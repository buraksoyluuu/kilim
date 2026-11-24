using System;
using System.Collections.Generic;

namespace Kilim.Core
{
    public enum Booster { Undo, Swap, SingleStitch }

    /// <summary>Ekonomi sabitleri (Spec 2 §2–§3). Canlıda remote config ile ezilebilir.</summary>
    public sealed class EconomyConfig
    {
        public int StartingCoins = 500;
        public int WinBase = 20;
        public int WinPerExtraStar = 10;
        public int BossBonus = 100;
        public int PiggyPerWin = 20;
        public int PiggyCap = 2000;
        public int FreeBoostersOnUnlock = 3;
        public int[] ContinueLadder = { 900, 1900, 2900 };
        public int ContinueGrantSingleStitch = 2;
        public int ContinueGrantSwap = 1;

        public readonly Dictionary<Booster, int> BoosterPrice = new Dictionary<Booster, int>
        {
            { Booster.Undo, 300 }, { Booster.Swap, 400 }, { Booster.SingleStitch, 500 },
        };

        public readonly Dictionary<Booster, int> BoosterUnlockLevel = new Dictionary<Booster, int>
        {
            { Booster.Undo, 6 }, { Booster.Swap, 9 }, { Booster.SingleStitch, 12 },
        };

        public static EconomyConfig Default => new EconomyConfig();

        public int WinCoins(int stars, bool boss)
        {
            if (stars < 1 || stars > 3) throw new ArgumentOutOfRangeException(nameof(stars));
            return WinBase + WinPerExtraStar * (stars - 1) + (boss ? BossBonus : 0);
        }

        /// <summary>n. devam teklifi (0 tabanlı) fiyatı; merdiven bitince son basamak tekrarlanır.</summary>
        public int ContinuePrice(int continueIndex) =>
            ContinueLadder[Math.Min(continueIndex, ContinueLadder.Length - 1)];
    }

    /// <summary>
    /// Oyuncu cüzdanı: coin, ilmek (yıldız), güçlendirici stokları, kumbara.
    /// Kural: bakiye asla negatif olmaz (iade dahil, UAT IAP-10).
    /// </summary>
    public sealed class Wallet
    {
        private readonly Dictionary<Booster, int> _boosters = new Dictionary<Booster, int>
        {
            { Booster.Undo, 0 }, { Booster.Swap, 0 }, { Booster.SingleStitch, 0 },
        };
        private readonly HashSet<Booster> _unlocked = new HashSet<Booster>();
        private readonly Dictionary<int, int> _bestStars = new Dictionary<int, int>();

        public int Coins { get; private set; }
        public int Ilmek { get; private set; }
        public int Piggy { get; private set; }

        public event Action Changed;

        public Wallet(int startingCoins) { Coins = startingCoins; }

        public int BoosterCount(Booster b) => _boosters[b];
        public bool IsUnlocked(Booster b) => _unlocked.Contains(b);
        public int BestStars(int levelId) => _bestStars.TryGetValue(levelId, out var s) ? s : 0;

        public void AddCoins(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            Coins += amount;
            Changed?.Invoke();
        }

        public bool TrySpend(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (Coins < amount) return false;
            Coins -= amount;
            Changed?.Invoke();
            return true;
        }

        /// <summary>İade edilen coin'i düşer; bakiye 0'ın altına inmez (UAT IAP-10).</summary>
        public void Revoke(int amount)
        {
            Coins = Math.Max(0, Coins - Math.Max(0, amount));
            Changed?.Invoke();
        }

        public void AddBooster(Booster b, int n)
        {
            if (n < 0) throw new ArgumentOutOfRangeException(nameof(n));
            _boosters[b] += n;
            Changed?.Invoke();
        }

        public bool TryConsumeBooster(Booster b)
        {
            if (_boosters[b] <= 0) return false;
            _boosters[b]--;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Bölüm kazanılınca çağrılır; güçlendirici açılışlarını uygular.</summary>
        public void UnlockBoostersUpTo(int levelReached, EconomyConfig cfg)
        {
            foreach (var kv in cfg.BoosterUnlockLevel)
                if (levelReached >= kv.Value && _unlocked.Add(kv.Key))
                    _boosters[kv.Key] += cfg.FreeBoostersOnUnlock;
            Changed?.Invoke();
        }

        /// <summary>
        /// Bölüm kazancı. Coin her kazanışta verilir; ilmek yalnızca en iyi yıldız farkı kadar eklenir (Spec 1 §4).
        /// Dönen değer: (coin, eklenen ilmek, kumbaraya eklenen).
        /// </summary>
        public (int coins, int ilmek, int piggy) ApplyWin(int levelId, int stars, bool boss, EconomyConfig cfg)
        {
            int coins = cfg.WinCoins(stars, boss);
            int prev = BestStars(levelId);
            int ilmek = Math.Max(0, stars - prev);
            if (stars > prev) _bestStars[levelId] = stars;
            int piggyAdd = Math.Max(0, Math.Min(cfg.PiggyPerWin, cfg.PiggyCap - Piggy));
            Coins += coins;
            Ilmek += ilmek;
            Piggy += piggyAdd;
            Changed?.Invoke();
            return (coins, ilmek, piggyAdd);
        }

        /// <summary>Kumbara kırıldı (IAP onayı sonrası): içerik coin'e geçer.</summary>
        public int BreakPiggy()
        {
            int v = Piggy;
            Coins += v;
            Piggy = 0;
            Changed?.Invoke();
            return v;
        }
    }

    /// <summary>
    /// Bir bölümdeki devam merdiveni (Spec 2 §3). Reklamla devam bölüm başına 1 kez ve yalnızca 1. basamakta.
    /// Reklam ödülü gelmezse hak harcanmaz (UAT ADS-12).
    /// </summary>
    public sealed class ContinueLadder
    {
        private readonly EconomyConfig _cfg;
        public int ContinuesTaken { get; private set; }
        public bool AdContinueUsed { get; private set; }

        public ContinueLadder(EconomyConfig cfg) { _cfg = cfg; }

        public int CurrentPrice => _cfg.ContinuePrice(ContinuesTaken);
        public bool IsLastChance => ContinuesTaken >= _cfg.ContinueLadder.Length - 1;
        public bool CanUseAd => !AdContinueUsed && ContinuesTaken == 0;

        public bool TryContinueWithCoins(Wallet w, GameSession s)
        {
            if (s.State != SessionState.Failed) return false;
            if (!w.TrySpend(CurrentPrice)) return false;
            Grant(w, s);
            return true;
        }

        /// <summary>Reklam SDK'sı "ödül kazanıldı" dediğinde çağrılır; erken kapatmada çağrılmaz.</summary>
        public bool ContinueWithRewardedAd(Wallet w, GameSession s)
        {
            if (s.State != SessionState.Failed || !CanUseAd) return false;
            AdContinueUsed = true;
            Grant(w, s);
            return true;
        }

        private void Grant(Wallet w, GameSession s)
        {
            w.AddBooster(Booster.SingleStitch, _cfg.ContinueGrantSingleStitch);
            w.AddBooster(Booster.Swap, _cfg.ContinueGrantSwap);
            ContinuesTaken++;
            s.ContinueAfterFail();
        }
    }
}
