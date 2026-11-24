using System;
using System.Collections.Generic;

namespace Kilim.Core
{
    /// <summary>Bir bölüm denemesi: oturum + devam merdiveni + deneme bilgisi.</summary>
    public sealed class LevelRun
    {
        public LevelData Level { get; internal set; }
        public GameSession Session { get; internal set; }
        public ContinueLadder Ladder { get; internal set; }
        public int Attempt { get; internal set; }
        public bool IsDaily { get; internal set; }
        public bool IsTutorial { get; internal set; }
        public DateTime StartedUtc { get; internal set; }
        public readonly List<string> BoostersUsed = new List<string>();
        public bool Completed { get; internal set; }
        public WinReport Report { get; internal set; }
    }

    public sealed class WinReport
    {
        public int Level;
        public int Stars;
        public double PatternPercent;
        public int Coins;
        public int Ilmek;
        public int Piggy;
        public bool RaceFlag;
        public int DailyCoins;
        public int PassLevelBefore, PassLevelAfter;
        public bool IsDaily;
        public bool CanDoubleCoins;
        public bool Doubled;
        public bool BoostersUnlockedNext;
        public int DurationSeconds;
    }

    public enum PurchaseOutcome { Granted, AlreadyProcessed, Unknown }

    /// <summary>
    /// Meta akış: bölüm başlat/kazan/pes et, satın alma teslimi ve iadesi. Tüm kurallar burada, tüm yazmalar profilde;
    /// çağıran her işlemden sonra profili kaydeder. Kazanma ödülü yalnızca bir kez uygulanır (UAT WIN-08).
    /// </summary>
    public sealed class Meta
    {
        public PlayerProfile Profile { get; }
        public GameConfig Config { get; }
        public IClock Clock { get; }
        public AdPolicy Ads { get; }
        public StarterOffer Starter { get; }
        public DailyPattern Daily { get; }
        public Race Race { get; }
        public SeasonPass Pass { get; }

        public Meta(PlayerProfile profile, GameConfig config, IClock clock)
        {
            Profile = profile;
            Config = config;
            Clock = clock;
            Ads = new AdPolicy(config, clock);
            Starter = new StarterOffer(config, clock);
            Daily = new DailyPattern(config, clock);
            Race = new Race(config, clock);
            Pass = new SeasonPass(clock);
            Pass.Roll(profile);
        }

        public Wallet Wallet => Profile.Wallet;

        public LevelRun StartLevel(LevelData level, bool isDaily = false)
        {
            if (!isDaily)
            {
                Profile.Attempts[level.Id] = Profile.AttemptsOf(level.Id) + 1;
                Wallet.UnlockBoostersUpTo(level.Id, Config.Economy);
            }
            var session = new GameSession(level) { ZenMode = isDaily };
            return new LevelRun
            {
                Level = level,
                Session = session,
                Ladder = new ContinueLadder(Config.Economy),
                Attempt = isDaily ? 1 : Profile.AttemptsOf(level.Id),
                IsDaily = isDaily,
                IsTutorial = !isDaily && level.Id <= Config.TutorialLastLevel,
                StartedUtc = Clock.UtcNow,
            };
        }

        /// <summary>Bölüm kazanıldı: coin, ilmek, kumbara, makara, bayrak, günlük, ilerleme. İkinci çağrı aynı raporu döner.</summary>
        public WinReport CompleteWin(LevelRun run)
        {
            if (run.Completed) return run.Report;
            if (run.Session.State != SessionState.Won) throw new InvalidOperationException("Bölüm kazanılmadı");
            int stars = run.Session.Stars;
            var r = new WinReport
            {
                Level = run.Level.Id,
                Stars = stars,
                PatternPercent = run.Session.PatternPercent,
                IsDaily = run.IsDaily,
                DurationSeconds = (int)(Clock.UtcNow - run.StartedUtc).TotalSeconds,
                PassLevelBefore = Pass.Level(Profile),
            };

            if (run.IsDaily)
            {
                r.DailyCoins = Daily.OnWin(Profile, stars);
            }
            else
            {
                var (coins, ilmek, piggy) = Wallet.ApplyWin(run.Level.Id, stars, run.Level.Boss, Config.Economy);
                r.Coins = coins;
                r.Ilmek = ilmek;
                r.Piggy = piggy;
                r.RaceFlag = Race.OnLevelWon(Profile, run.Attempt);
                Pass.OnLevelWon(Profile);
                Starter.OnLevelWon(Profile, run.Level.Id);
                Ads.OnLevelCompleted(Profile);
                r.CanDoubleCoins = run.Level.Id >= Config.WinDoubleFromLevel && Ads.CanShowRewarded(Profile);
                if (run.Level.Id >= Profile.CurrentLevel)
                {
                    Profile.CurrentLevel = run.Level.Id + 1;
                    Profile.MaxLevelReached = Math.Max(Profile.MaxLevelReached, Profile.CurrentLevel);
                }
                Profile.ConsecutiveFails = 0;
                foreach (var kv in Config.Economy.BoosterUnlockLevel)
                    if (kv.Value == Profile.CurrentLevel) r.BoostersUnlockedNext = true;
            }
            r.PassLevelAfter = Pass.Level(Profile);
            run.Completed = true;
            run.Report = r;
            return r;
        }

        /// <summary>Kazandın "coin ×2" (ödüllü reklam izlendi). Bölüm başına bir kez.</summary>
        public bool DoubleWinCoins(LevelRun run)
        {
            var r = run.Report;
            if (r == null || r.Doubled || !r.CanDoubleCoins) return false;
            r.Doubled = true;
            Wallet.AddCoins(r.Coins);
            Ads.OnRewardedEarned(Profile);
            return true;
        }

        /// <summary>
        /// Pes et onaylandı. Ardışık 3. kayıpta bölüm başına bir kez ücretsiz 1 Geri Al hediye edilir (Spec 3 §1).
        /// Dönen değer: hediye verildi mi.
        /// </summary>
        public bool GiveUp(LevelRun run)
        {
            if (run.IsDaily) return false;
            Profile.ConsecutiveFails++;
            if (Profile.ConsecutiveFails >= 3 && !Profile.FreeUndoGiftGiven.Contains(run.Level.Id)
                && Wallet.IsUnlocked(Booster.Undo))
            {
                Profile.FreeUndoGiftGiven.Add(run.Level.Id);
                Wallet.AddBooster(Booster.Undo, 1);
                return true;
            }
            return false;
        }

        public bool WillGiftUndoOnGiveUp(LevelRun run) =>
            !run.IsDaily && Profile.ConsecutiveFails + 1 >= 3 && !Profile.FreeUndoGiftGiven.Contains(run.Level.Id)
            && Wallet.IsUnlocked(Booster.Undo);

        // ---------- Satın alma ----------

        /// <summary>
        /// Mağaza onayladıktan (ve sunucu doğruladıktan) sonra içeriği verir. Aynı işlem ikinci kez gelirse
        /// hiçbir şey vermez (UAT IAP-04, IAP-11). Kalıcı ürünler geri yüklemede de buradan geçer.
        /// </summary>
        public PurchaseOutcome ApplyPurchase(string sku, string transactionId, int priceUsdCents = 0, bool isRestore = false)
        {
            if (!string.IsNullOrEmpty(transactionId) && !Profile.ProcessedTransactions.Add(transactionId))
                return PurchaseOutcome.AlreadyProcessed;

            int coins = Skus.CoinsOf(sku);
            switch (sku)
            {
                case Skus.NoAds:
                    Profile.NoAds = true;
                    Profile.OwnedNonConsumables.Add(sku);
                    break;
                case Skus.Starter:
                    if (Profile.OwnedNonConsumables.Contains(sku) && isRestore) return PurchaseOutcome.AlreadyProcessed;
                    Profile.StarterBought = true;
                    Profile.OwnedNonConsumables.Add(sku);
                    if (!isRestore)
                    {
                        coins = 2000;
                        foreach (Booster b in Enum.GetValues(typeof(Booster))) Wallet.AddBooster(b, 3);
                        Profile.NoAdsUntilMs = Data.ToMs(Clock.UtcNow.AddHours(24));
                    }
                    break;
                case Skus.Piggy:
                    coins = Wallet.BreakPiggy();
                    Profile.GrantedCoinsBySku[sku] = coins;
                    coins = 0; // BreakPiggy zaten ekledi
                    break;
                case Skus.GoldPass:
                    Pass.UnlockGold(Profile);
                    break;
                default:
                    if (coins == 0) return PurchaseOutcome.Unknown;
                    break;
            }
            if (coins > 0)
            {
                Wallet.AddCoins(coins);
                Profile.GrantedCoinsBySku[sku] = coins;
            }
            if (!isRestore)
            {
                Profile.LastPurchaseMs = Data.ToMs(Clock.UtcNow);
                TrackSpend(priceUsdCents);
            }
            return PurchaseOutcome.Granted;
        }

        /// <summary>İade/chargeback (UAT IAP-10): kalıcı ürün kaldırılır, coin düşülür (bakiye 0'ın altına inmez).</summary>
        public void ApplyRefund(string sku)
        {
            switch (sku)
            {
                case Skus.NoAds: Profile.NoAds = false; Profile.OwnedNonConsumables.Remove(sku); break;
                case Skus.GoldPass: Profile.GoldPass = false; break;
                case Skus.Starter:
                    Profile.OwnedNonConsumables.Remove(sku);
                    Wallet.Revoke(2000);
                    break;
                default:
                    Wallet.Revoke(Profile.GrantedCoinsBySku.TryGetValue(sku, out var c) ? c : Skus.CoinsOf(sku));
                    break;
            }
        }

        /// <summary>Genç profilinde ayda 100 $ üstü harcamada bilgilendirme kartı (Spec 2 §7).</summary>
        public bool TeenSpendNoticeDue { get; private set; }

        private void TrackSpend(int cents)
        {
            var month = Clock.LocalDate().ToString("yyyy-MM");
            if (Profile.SpendMonth != month) { Profile.SpendMonth = month; Profile.SpendMonthUsdCents = 0; }
            int before = Profile.SpendMonthUsdCents;
            Profile.SpendMonthUsdCents += Math.Max(0, cents);
            int limit = Config.TeenMonthlySpendNoticeUsd * 100;
            TeenSpendNoticeDue = Profile.Age == AgeBand.Teen && before <= limit && Profile.SpendMonthUsdCents > limit;
        }
    }
}
