using System;
using System.Collections.Generic;
using System.Linq;

namespace Kilim.Core
{
    /// <summary>Günün Deseni (Spec 2 §4): herkese aynı, kayıpsız (zen), 3★'da 100 coin + damga, 7 gün seri = rozet.</summary>
    public sealed class DailyPattern
    {
        private readonly GameConfig _cfg;
        private readonly IClock _clock;
        public DailyPattern(GameConfig cfg, IClock clock) { _cfg = cfg; _clock = clock; }

        public string Today => _clock.LocalDate().ToString("yyyy-MM-dd");

        /// <summary>Bugünün bölümü: havuzdan tarihe göre deterministik seçim (sunucu gelene kadar herkes aynı).</summary>
        public LevelData PickLevel(IReadOnlyList<LevelData> pool)
        {
            int day = (int)(_clock.LocalDate() - new DateTime(2026, 1, 1)).TotalDays;
            int idx = ((day % pool.Count) + pool.Count) % pool.Count;
            return pool[idx];
        }

        public bool StampedToday(PlayerProfile p) => p.DailyStamps.Contains(Today);

        /// <summary>Günün deseni kazanıldı. Dönen değer: verilen coin (0 = ödül yok ya da bugün zaten alındı).</summary>
        public int OnWin(PlayerProfile p, int stars)
        {
            if (stars < 3 || StampedToday(p)) return 0;
            var today = Today;
            var yesterday = _clock.LocalDate().AddDays(-1).ToString("yyyy-MM-dd");
            p.DailyStreak = p.LastDailyDay == yesterday ? p.DailyStreak + 1 : 1;
            p.LastDailyDay = today;
            p.DailyStamps.Add(today);
            if (p.DailyStreak > 0 && p.DailyStreak % 7 == 0) p.StreakBadges.Add($"streak7-{today}");
            p.Wallet.AddCoins(_cfg.DailyPatternReward);
            return _cfg.DailyPatternReward;
        }

        public int CurrentStreak(PlayerProfile p)
        {
            var yesterday = _clock.LocalDate().AddDays(-1).ToString("yyyy-MM-dd");
            return p.LastDailyDay == Today || p.LastDailyDay == yesterday ? p.DailyStreak : 0;
        }

        /// <summary>Günlük sandık: günde 1 kez 50 coin; ödüllü reklamla ×2.</summary>
        public bool ChestAvailable(PlayerProfile p) => p.DailyChestDay != Today;

        public int OpenChest(PlayerProfile p, bool doubled)
        {
            if (!ChestAvailable(p)) return 0;
            p.DailyChestDay = Today;
            int coins = doubled ? 100 : 50;
            p.Wallet.AddCoins(coins);
            return coins;
        }
    }

    public sealed class RaceEntry
    {
        public string Name;
        public int Flags;
        public long ReachedMs;   // bu bayrak sayısına ulaşma zamanı (eşitlikte önce ulaşan üstte)
        public bool IsPlayer;
    }

    /// <summary>
    /// Tezgâh Yarışı (Spec 2 §4): Cuma 10:00 – Pazar 22:00 yerel; ilk denemede kazanılan bölüm = 1 bayrak;
    /// 30 kişilik grup; ödül 1.500/800/400 (+çerçeve 1.ye); 0 bayrak ödülsüz.
    /// Grup eşleşmesi sunucu işidir; sunucu yokken <see cref="BuildGroup"/> deterministik yerel rakipler üretir.
    /// </summary>
    public sealed class Race
    {
        public const int GroupSize = 30;
        public static readonly int[] Prizes = { 1500, 800, 400 };
        public static readonly string[] Nicknames =
        {
            "Safran Kuşu", "Gece Dokumacısı", "Mavi Boncuk", "Zeytin Dalı", "Sessiz İlmek", "Kırmızı Yün",
            "Nar Çiçeği", "Ay Işığı", "Lale Deseni", "Pamuk Bulut", "Çini Ustası", "Yayla Rüzgârı",
            "İnce Saçak", "Altın Mekik", "Kervan Yolu", "Göbek Motifi", "Bereket", "Kuş Gözü",
            "Elibelinde", "Koçboynuzu", "Hayat Ağacı", "Su Yolu", "Yıldız Göl", "Bukağı", "Muska",
            "Pıtrak", "Akrep Kuyruğu", "Saç Bağı", "Küpe Motifi", "Çengel", "Turna Teli", "Kervansaray",
        };

        private readonly GameConfig _cfg;
        private readonly IClock _clock;
        public Race(GameConfig cfg, IClock clock) { _cfg = cfg; _clock = clock; }

        /// <summary>İçinde bulunulan ya da en son yarışın (başlangıç, bitiş) yerel zamanı.</summary>
        public (DateTime start, DateTime end) CurrentWindow()
        {
            var now = _clock.LocalNow();
            int sinceFriday = ((int)now.DayOfWeek - (int)DayOfWeek.Friday + 7) % 7;
            var start = now.Date.AddDays(-sinceFriday).AddHours(10);
            if (start > now) start = start.AddDays(-7);
            return (start, start.AddDays(2).AddHours(12)); // Pazar 22:00
        }

        public bool IsActive
        {
            get
            {
                if (!_cfg.RaceEnabled) return false;
                var (s, e) = CurrentWindow();
                var now = _clock.LocalNow();
                return now >= s && now < e;
            }
        }

        public string WeekId => CurrentWindow().start.ToString("yyyy-MM-dd");

        public TimeSpan Remaining
        {
            get
            {
                var (s, e) = CurrentWindow();
                var now = _clock.LocalNow();
                if (now < s) return TimeSpan.Zero;
                return e > now ? e - now : TimeSpan.Zero;
            }
        }

        private void Roll(PlayerProfile p)
        {
            if (p.RaceWeek != WeekId) { p.RaceWeek = WeekId; p.RaceFlags = 0; p.RaceLastFlagMs = 0; }
        }

        /// <summary>Normal bölüm kazanıldı. İlk denemedeyse ve yarış aktifse +1 bayrak.</summary>
        public bool OnLevelWon(PlayerProfile p, int attempt)
        {
            if (!IsActive || attempt != 1 || string.IsNullOrEmpty(p.RaceNickname)) return false;
            Roll(p);
            p.RaceFlags++;
            p.RaceLastFlagMs = Data.ToMs(_clock.UtcNow);
            return true;
        }

        public int Flags(PlayerProfile p) => p.RaceWeek == WeekId ? p.RaceFlags : 0;

        /// <summary>Sunucu yokken yerel grup: haftaya ve oyuncuya göre sabit rakipler; bayraklar zamanla artar.</summary>
        public List<RaceEntry> BuildGroup(PlayerProfile p)
        {
            var (s, e) = CurrentWindow();
            double progress = Math.Max(0, Math.Min(1, (_clock.LocalNow() - s).TotalSeconds / (e - s).TotalSeconds));
            var rng = new Random((WeekId + p.PlayerId).GetHashCode());
            var names = Nicknames.Where(n => n != p.RaceNickname).OrderBy(_ => rng.Next()).Take(GroupSize - 1).ToList();
            var list = new List<RaceEntry>();
            long startMs = Data.ToMs(s - _clock.LocalOffset);
            foreach (var n in names)
            {
                int target = rng.Next(2, 45);
                int flags = (int)Math.Round(target * progress);
                list.Add(new RaceEntry { Name = n, Flags = flags, ReachedMs = startMs + rng.Next(1, 100000) * 1000L });
            }
            list.Add(new RaceEntry { Name = p.RaceNickname, Flags = Flags(p), ReachedMs = p.RaceLastFlagMs, IsPlayer = true });
            return Rank(list);
        }

        /// <summary>Sıralama: bayrak çok olan üstte; eşitlikte o sayıya önce ulaşan üstte (UAT RACE-06).</summary>
        public static List<RaceEntry> Rank(IEnumerable<RaceEntry> entries) =>
            entries.OrderByDescending(x => x.Flags).ThenBy(x => x.ReachedMs).ToList();

        /// <summary>Yarış bittiyse ve ödül alınmadıysa ödülü verir. 0 bayrak ödülsüz (UAT RACE-07).</summary>
        public (int rank, int coins) ClaimIfEnded(PlayerProfile p, List<RaceEntry> finalRanking)
        {
            if (IsActive || p.RaceWeek != WeekId || p.RaceRewardClaimedWeek == WeekId) return (0, 0);
            p.RaceRewardClaimedWeek = WeekId;
            if (p.RaceFlags == 0) return (0, 0);
            int rank = finalRanking.FindIndex(x => x.IsPlayer) + 1;
            if (rank < 1 || rank > Prizes.Length) return (rank, 0);
            p.Wallet.AddCoins(Prizes[rank - 1]);
            if (rank == 1) p.Cosmetics.Add($"frame-race-{WeekId}");
            return (rank, Prizes[rank - 1]);
        }
    }

    public sealed class PassReward
    {
        public int Coins;
        public Booster? Booster;
        public int BoosterCount;
        public string Cosmetic;
        public string Label;
    }

    /// <summary>Sezon kartı (Spec 2 §3, KumbaraPass 11.2): 28 gün, 30 seviye, kazanılan bölüm = 1 makara, 10 makara = 1 seviye.</summary>
    public sealed class SeasonPass
    {
        public const int Levels = 30;
        public const int SpoolsPerLevel = 10;
        public const int SeasonDays = 28;
        public static readonly DateTime Anchor = new DateTime(2026, 10, 27);   // Sezon 7 başlangıcı

        private readonly IClock _clock;
        public SeasonPass(IClock clock) { _clock = clock; }

        public int SeasonNumber => 7 + (int)Math.Floor((_clock.LocalDate() - Anchor).TotalDays / SeasonDays);
        public string SeasonKey => $"s{SeasonNumber}";
        public TimeSpan Remaining => Anchor.AddDays((SeasonNumber - 6) * SeasonDays) - _clock.LocalNow();

        /// <summary>Yeni sezona geçildiyse sıfırlar; alınmamış ödüller kaybolur (UAT PASS-04).</summary>
        public void Roll(PlayerProfile p)
        {
            if (p.SeasonId == SeasonKey) return;
            p.SeasonId = SeasonKey;
            p.SeasonSpools = 0;
            p.GoldPass = false;
            p.PassClaimedFree.Clear();
            p.PassClaimedGold.Clear();
        }

        public int Level(PlayerProfile p) => Math.Min(Levels, p.SeasonSpools / SpoolsPerLevel);
        public int SpoolsInLevel(PlayerProfile p) => Level(p) >= Levels ? SpoolsPerLevel : p.SeasonSpools % SpoolsPerLevel;

        public void OnLevelWon(PlayerProfile p)
        {
            Roll(p);
            if (Level(p) < Levels) p.SeasonSpools++;
        }

        public static PassReward FreeReward(int level)
        {
            if (level % 10 == 0) return new PassReward { Coins = 250, Label = "Sandık: 250 coin" };
            switch (level % 3)
            {
                case 0: return new PassReward { Coins = 100 + level * 5, Label = $"{100 + level * 5} coin" };
                case 1: return new PassReward { Booster = Kilim.Core.Booster.Swap, BoosterCount = 1, Label = "Değiştir ×1" };
                default: return new PassReward { Booster = Kilim.Core.Booster.Undo, BoosterCount = 1, Label = "Geri Al ×1" };
            }
        }

        public static PassReward GoldReward(int level)
        {
            if (level == 10) return new PassReward { Cosmetic = "frame-fas", Label = "Çerçeve: Fas" };
            if (level == 20) return new PassReward { Cosmetic = "pamuk-fes", Label = "Pamuk: Fes" };
            if (level == 30) return new PassReward { Cosmetic = "pamuk-kaftan", Coins = 1000, Label = "Pamuk: Kaftan + 1.000 coin" };
            switch (level % 3)
            {
                case 0: return new PassReward { Coins = 300 + level * 10, Label = $"{300 + level * 10} coin" };
                case 1: return new PassReward { Booster = Kilim.Core.Booster.Undo, BoosterCount = 2, Label = "Geri Al ×2" };
                default: return new PassReward { Booster = Kilim.Core.Booster.SingleStitch, BoosterCount = 1, Label = "Tek İlmek ×1" };
            }
        }

        public bool CanClaim(PlayerProfile p, int level, bool gold) =>
            level >= 1 && level <= Level(p) && (gold ? p.GoldPass && !p.PassClaimedGold.Contains(level)
                                                     : !p.PassClaimedFree.Contains(level));

        public PassReward Claim(PlayerProfile p, int level, bool gold)
        {
            if (!CanClaim(p, level, gold)) return null;
            var r = gold ? GoldReward(level) : FreeReward(level);
            (gold ? p.PassClaimedGold : p.PassClaimedFree).Add(level);
            if (r.Coins > 0) p.Wallet.AddCoins(r.Coins);
            if (r.Booster.HasValue) p.Wallet.AddBooster(r.Booster.Value, r.BoosterCount);
            if (r.Cosmetic != null) p.Cosmetics.Add(r.Cosmetic);
            return r;
        }

        /// <summary>Altın yol açılınca geçmiş seviyelerin altın ödülleri alınabilir hale gelir (talep edilerek).</summary>
        public void UnlockGold(PlayerProfile p) { Roll(p); p.GoldPass = true; }
    }
}
