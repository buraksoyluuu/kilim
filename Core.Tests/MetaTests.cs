using System;
using System.Linq;
using static Kilim.Core.Tests.TestLevels;

namespace Kilim.Core.Tests
{
    public static class MetaTests
    {
        // Perşembe 2026-11-12 12:00 UTC, İstanbul +3
        private static ManualClock NewClock() => new ManualClock(new DateTime(2026, 11, 12, 12, 0, 0), TimeSpan.FromHours(3));

        private static Meta NewMeta(ManualClock clock = null, AgeBand age = AgeBand.Adult)
        {
            var p = new PlayerProfile { Age = age, FirstSessionDone = true, RaceNickname = "Safran Kuşu" };
            return new Meta(p, GameConfig.Default, clock ?? NewClock());
        }

        private static LevelRun WinLevel(Meta m, LevelData l)
        {
            var run = m.StartLevel(l);
            foreach (var step in l.Solution) PlaceCells(run.Session, SlotOf(run.Session, step.QueueIndex), step.Cells);
            Assert.Equal(SessionState.Won, run.Session.State);
            return run;
        }

        [Test("WIN-08")]
        public static void WinRewardsAppliedExactlyOnce()
        {
            var m = NewMeta();
            var run = WinLevel(m, Region1[0]);
            var r1 = m.CompleteWin(run);
            var r2 = m.CompleteWin(run);
            Assert.True(ReferenceEquals(r1, r2));
            Assert.Equal(540, m.Wallet.Coins);
            Assert.Equal(3, m.Wallet.Ilmek);
            Assert.Equal(20, m.Wallet.Piggy);
            Assert.Equal(1, m.Profile.SeasonSpools);
            Assert.Equal(2, m.Profile.CurrentLevel);
        }

        [Test("SAVE-01, SAVE-03")]
        public static void ProfileRoundTripsThroughJson()
        {
            var m = NewMeta();
            m.CompleteWin(WinLevel(m, Region1[0]));
            m.Profile.HomeStyles["x"] = "Uşak \"özel\"";
            m.Profile.DailyStamps.Add("2026-11-12");
            m.ApplyPurchase(Skus.NoAds, "tx-1", 499);
            var json = m.Profile.ToJson();
            var back = PlayerProfile.FromJson(json);
            Assert.Equal(json, back.ToJson(), "aynı JSON");
            Assert.Equal(540, back.Wallet.Coins);
            Assert.Equal(3, back.Wallet.BestStars(1));
            Assert.True(back.NoAds);
            Assert.True(PlayerProfile.FromJson("{bozuk") == null, "bozuk kayıt null");
        }

        [Test("AGE-02, AGE-03, AGE-04")]
        public static void AgeBands()
        {
            var today = new DateTime(2026, 11, 12);
            Assert.Equal(AgeBand.Blocked, AgePolicy.BandFor(2014, today));
            Assert.Equal(AgeBand.Teen, AgePolicy.BandFor(2012, today));
            Assert.Equal(AgeBand.Teen, AgePolicy.BandFor(2008, today));
            Assert.Equal(AgeBand.Adult, AgePolicy.BandFor(2007, today));
            Assert.Equal(AgeBand.Unknown, AgePolicy.BandFor(1900, today));
        }

        [Test("ADS-01..ADS-08")]
        public static void InterstitialRules()
        {
            var clock = NewClock();
            var m = NewMeta(clock);
            var p = m.Profile;
            Assert.Equal(InterstitialBlock.BeforeLevel, m.Ads.CanShowInterstitial(p, 11, false));
            Assert.Equal(InterstitialBlock.None, m.Ads.CanShowInterstitial(p, 12, false));
            Assert.Equal(InterstitialBlock.AfterFail, m.Ads.CanShowInterstitial(p, 12, true));
            m.Ads.OnInterstitialShown(p);
            m.Ads.OnLevelCompleted(p);
            clock.Advance(TimeSpan.FromSeconds(120));
            Assert.Equal(InterstitialBlock.LevelGap, m.Ads.CanShowInterstitial(p, 13, false));
            m.Ads.OnLevelCompleted(p);
            Assert.Equal(InterstitialBlock.None, m.Ads.CanShowInterstitial(p, 14, false));
            p.LastInterstitialMs = Data.ToMs(clock.UtcNow.AddSeconds(-30));
            Assert.Equal(InterstitialBlock.TimeGap, m.Ads.CanShowInterstitial(p, 14, false));

            p.FirstSessionDone = false;
            Assert.Equal(InterstitialBlock.FirstSession, m.Ads.CanShowInterstitial(p, 20, false));
            p.FirstSessionDone = true;
            p.LastPurchaseMs = Data.ToMs(clock.UtcNow.AddHours(-71));
            Assert.Equal(InterstitialBlock.RecentPurchase, m.Ads.CanShowInterstitial(p, 20, false));
            p.LastPurchaseMs = 0;
            p.NoAds = true;
            Assert.Equal(InterstitialBlock.NoAds, m.Ads.CanShowInterstitial(p, 20, false));
        }

        [Test("ADS-09")]
        public static void TeenInterstitialEveryThreeLevels()
        {
            var m = NewMeta(age: AgeBand.Teen);
            var p = m.Profile;
            m.Ads.OnInterstitialShown(p);
            m.Ads.OnLevelCompleted(p); m.Ads.OnLevelCompleted(p);
            ((ManualClock)m.Clock).Advance(TimeSpan.FromMinutes(5));
            Assert.Equal(InterstitialBlock.LevelGap, m.Ads.CanShowInterstitial(p, 20, false));
            m.Ads.OnLevelCompleted(p);
            Assert.Equal(InterstitialBlock.None, m.Ads.CanShowInterstitial(p, 21, false));
        }

        [Test("ADS-10, ADS-11")]
        public static void RewardedCapAndGap()
        {
            var clock = NewClock();
            var m = NewMeta(clock);
            for (int i = 0; i < 20; i++)
            {
                Assert.True(m.Ads.CanShowRewarded(m.Profile), $"#{i}");
                m.Ads.OnRewardedEarned(m.Profile);
                Assert.False(m.Ads.CanShowRewarded(m.Profile), "30 sn ara");
                clock.Advance(TimeSpan.FromSeconds(31));
            }
            Assert.False(m.Ads.CanShowRewarded(m.Profile), "günlük 20");
            clock.Advance(TimeSpan.FromDays(1));
            Assert.True(m.Ads.CanShowRewarded(m.Profile), "ertesi gün");
        }

        [Test("OFFER-01, OFFER-02")]
        public static void StarterOfferRealTimer()
        {
            var clock = NewClock();
            var m = NewMeta(clock);
            m.Starter.OnLevelWon(m.Profile, 7);
            Assert.False(m.Starter.IsVisible(m.Profile));
            m.Starter.OnLevelWon(m.Profile, 8);
            Assert.True(m.Starter.IsVisible(m.Profile));
            clock.Advance(TimeSpan.FromHours(47));
            Assert.Equal(1, (int)Math.Round(m.Starter.Remaining(m.Profile).TotalHours));
            clock.Advance(TimeSpan.FromHours(2));
            Assert.False(m.Starter.IsVisible(m.Profile));
            m.Starter.OnLevelWon(m.Profile, 9);
            Assert.False(m.Starter.IsVisible(m.Profile), "yeniden başlamaz");
        }

        [Test("IAP-04, IAP-11, IAP-10")]
        public static void PurchasesIdempotentAndRefundable()
        {
            var m = NewMeta();
            Assert.Equal(PurchaseOutcome.Granted, m.ApplyPurchase(Skus.Coins3, "t1", 499));
            Assert.Equal(PurchaseOutcome.AlreadyProcessed, m.ApplyPurchase(Skus.Coins3, "t1", 499));
            Assert.Equal(6500, m.Wallet.Coins);
            m.Wallet.TrySpend(6000);
            m.ApplyRefund(Skus.Coins3);
            Assert.Equal(0, m.Wallet.Coins, "negatife inmez");
            m.ApplyPurchase(Skus.NoAds, "t2");
            m.ApplyRefund(Skus.NoAds);
            Assert.False(m.Profile.NoAds);
        }

        [Test("OFFER-03, IAP-05")]
        public static void StarterPackContentsAndRestore()
        {
            var m = NewMeta();
            m.ApplyPurchase(Skus.Starter, "s1", 199);
            Assert.Equal(2500, m.Wallet.Coins);
            Assert.Equal(3, m.Wallet.BoosterCount(Booster.Swap));
            Assert.True(m.Profile.IsNoAdsActive(m.Clock.UtcNow));
            Assert.False(m.Profile.IsNoAdsActive(m.Clock.UtcNow.AddHours(25)), "24 saat");

            var fresh = NewMeta();
            fresh.ApplyPurchase(Skus.NoAds, "r1", isRestore: true);
            fresh.ApplyPurchase(Skus.Starter, "r2", isRestore: true);
            Assert.True(fresh.Profile.NoAds);
            Assert.Equal(500, fresh.Wallet.Coins, "geri yüklemede tüketilebilir içerik yok");
        }

        [Test("PIG-03")]
        public static void PiggyBreakPurchase()
        {
            var m = NewMeta();
            for (int i = 0; i < 100; i++) m.Wallet.ApplyWin(1000 + i, 1, false, m.Config.Economy);
            int before = m.Wallet.Coins;
            m.ApplyPurchase(Skus.Piggy, "p1", 299);
            Assert.Equal(before + 2000, m.Wallet.Coins);
            Assert.Equal(0, m.Wallet.Piggy);
        }

        [Test("DAILY-01..DAILY-06")]
        public static void DailyPatternRewardOncePerDay()
        {
            var clock = NewClock();
            var m = NewMeta(clock);
            Assert.Equal(0, m.Daily.OnWin(m.Profile, 2), "3★ değilse ödül yok");
            Assert.Equal(100, m.Daily.OnWin(m.Profile, 3));
            Assert.Equal(0, m.Daily.OnWin(m.Profile, 3), "aynı gün ikinci kez yok");
            for (int d = 1; d < 7; d++) { clock.Advance(TimeSpan.FromDays(1)); m.Daily.OnWin(m.Profile, 3); }
            Assert.Equal(7, m.Profile.DailyStreak);
            Assert.Equal(1, m.Profile.StreakBadges.Count);
            clock.Advance(TimeSpan.FromDays(2));
            Assert.Equal(0, m.Daily.CurrentStreak(m.Profile), "seri kırıldı");
            var pick1 = m.Daily.PickLevel(Region1);
            Assert.True(ReferenceEquals(pick1, m.Daily.PickLevel(Region1)), "aynı gün aynı desen");
        }

        [Test("DAILY-02")]
        public static void ZenModeAutoUndoesInsteadOfFail()
        {
            var s = new GameSession(ThreeByThree()) { ZenMode = true };
            int undone = 0;
            s.AutoUndone += _ => undone++;
            s.Place(0, new Cell(0, 1));
            s.Place(2, new Cell(2, 0));
            Assert.Equal(SessionState.Playing, s.State);
            Assert.Equal(1, undone);
            Assert.Equal(4, s.Board.FilledCount, "son yerleşim geri alındı");
            Assert.True(s.Tray[2] != null, "domino tezgâha döndü");
        }

        [Test("RACE-01..RACE-07")]
        public static void RaceWindowFlagsAndPrizes()
        {
            var clock = new ManualClock(new DateTime(2026, 11, 13, 6, 0, 0), TimeSpan.FromHours(3)); // Cuma 09:00 yerel
            var m = NewMeta(clock);
            Assert.False(m.Race.IsActive, "Cuma 10:00 öncesi");
            clock.Advance(TimeSpan.FromHours(2));
            Assert.True(m.Race.IsActive);
            Assert.True(m.Race.OnLevelWon(m.Profile, 1));
            Assert.False(m.Race.OnLevelWon(m.Profile, 2), "2. deneme bayrak yok");
            Assert.Equal(1, m.Race.Flags(m.Profile));

            var group = m.Race.BuildGroup(m.Profile);
            Assert.Equal(Race.GroupSize, group.Count);
            Assert.Equal(1, group.Count(g => g.IsPlayer));

            clock.Advance(TimeSpan.FromDays(3)); // Pazartesi
            Assert.False(m.Race.IsActive);
            var ranking = Race.Rank(new[]
            {
                new RaceEntry { Name = "A", Flags = 5, ReachedMs = 200 },
                new RaceEntry { Name = "B", Flags = 5, ReachedMs = 100 },
            });
            Assert.Equal("B", ranking[0].Name, "eşitlikte önce ulaşan");
        }

        [Test("RACE-07")]
        public static void RaceZeroFlagsNoPrize()
        {
            var clock = new ManualClock(new DateTime(2026, 11, 13, 9, 0, 0), TimeSpan.FromHours(3));
            var m = NewMeta(clock);
            m.Profile.RaceWeek = m.Race.WeekId;
            clock.Advance(TimeSpan.FromDays(2.6));
            var group = m.Race.BuildGroup(m.Profile);
            Assert.Equal((0, 0), m.Race.ClaimIfEnded(m.Profile, group));
        }

        [Test("PASS-01..PASS-04")]
        public static void SeasonPassProgressAndRollover()
        {
            var clock = NewClock();
            var m = NewMeta(clock);
            for (int i = 0; i < 25; i++) m.Pass.OnLevelWon(m.Profile);
            Assert.Equal(2, m.Pass.Level(m.Profile));
            Assert.Equal(5, m.Pass.SpoolsInLevel(m.Profile));
            Assert.True(m.Pass.CanClaim(m.Profile, 1, false));
            Assert.False(m.Pass.CanClaim(m.Profile, 3, false));
            Assert.False(m.Pass.CanClaim(m.Profile, 1, true), "altın yol kapalı");
            m.Pass.Claim(m.Profile, 1, false);
            Assert.False(m.Pass.CanClaim(m.Profile, 1, false), "ikinci kez yok");
            m.ApplyPurchase(Skus.GoldPass, "g1", 499);
            Assert.True(m.Pass.CanClaim(m.Profile, 1, true), "geçmiş altın ödüller");
            clock.Advance(TimeSpan.FromDays(SeasonPass.SeasonDays));
            m.Pass.Roll(m.Profile);
            Assert.Equal(0, m.Pass.Level(m.Profile));
            Assert.False(m.Profile.GoldPass);
        }

        [Test("META-01..META-05")]
        public static void HomeTasksCostAndOrder()
        {
            Assert.Equal(40, Home.TotalCost);
            Assert.Equal(20, Home.Region1.Count);
            var m = NewMeta();
            var first = Home.NextTask(m.Profile);
            Assert.False(Home.Do(m.Profile, first), "ilmek yok");
            m.CompleteWin(WinLevel(m, Region1[0]));  // +3 ilmek
            Assert.True(Home.Do(m.Profile, first));
            var second = Home.NextTask(m.Profile);
            Assert.True(Home.Do(m.Profile, second, "Hereke"));
            Assert.Equal("Hereke", m.Profile.HomeStyles[second.Id]);
            Assert.True(Home.ChangeStyle(m.Profile, second, "Yörük"));
            Assert.Equal(0, m.Wallet.Ilmek);
        }

        [Test("META-06")]
        public static void HousesChainAfterFirstIsDone()
        {
            Assert.True(Home.Houses.Count >= 4, "en az 4 mekân");
            foreach (var h in Home.Houses)
            {
                Assert.Equal(20, h.Tasks.Count, h.Name);
                Assert.Equal(5, h.Areas.Length, h.Name);
                Assert.Equal(5, h.AreaArt.Length, h.Name);
            }
            var ids = new System.Collections.Generic.HashSet<string>();
            foreach (var t in Home.All) Assert.True(ids.Add(t.Id), "tekil görev kimliği " + t.Id);
            var p = new PlayerProfile();
            Assert.False(Home.IsUnlocked(p, Home.Houses[1]));
            foreach (var t in Home.Houses[0].Tasks) p.HomeTasksDone.Add(t.Id);
            Assert.True(Home.IsUnlocked(p, Home.Houses[1]));
            Assert.Equal(Home.Houses[1], Home.CurrentHouse(p));
            Assert.Equal(Home.Houses[1].Tasks[0], Home.NextTask(p));
        }

        [Test("LVL-07")]
        public static void ReplayDoesNotAdvanceAndPaysHalf()
        {
            var m = NewMeta();
            var r1 = m.CompleteWin(WinLevel(m, Region1[0]));
            Assert.False(r1.IsReplay);
            Assert.Equal(2, m.Profile.CurrentLevel);
            int ilmek = m.Wallet.Ilmek, piggy = m.Wallet.Piggy;
            var r2 = m.CompleteWin(WinLevel(m, Region1[0]));
            Assert.True(r2.IsReplay);
            Assert.Equal(r1.Coins / 2, r2.Coins);
            Assert.Equal(2, m.Profile.CurrentLevel, "ilerleme değişmez");
            Assert.Equal(ilmek, m.Wallet.Ilmek, "yeni yıldız yoksa ilmek yok");
            Assert.Equal(piggy, m.Wallet.Piggy, "tekrarda kumbara yok");
            Assert.Equal(3, r2.PreviousBest);
        }

        [Test("LVL-08")]
        public static void HintsFadeWithLevel()
        {
            Assert.Equal(HintLevel.Full, HintPolicy.For(1, false, false));
            Assert.Equal(HintLevel.Glyph, HintPolicy.For(HintPolicy.GlyphFrom, false, false));
            Assert.Equal(HintLevel.None, HintPolicy.For(HintPolicy.NoneFrom, false, false));
            Assert.Equal(HintLevel.Full, HintPolicy.For(100, true, false), "kolay mod");
            Assert.Equal(HintLevel.Full, HintPolicy.For(100, false, true), "günün deseni");
        }

        [Test("SAVE-05")]
        public static void NewProfileFieldsRoundTrip()
        {
            var p = new PlayerProfile { EasyMode = true, IntroSeen = true };
            p.HousesIntroduced.Add("bag");
            var q = PlayerProfile.FromJson(p.ToJson());
            Assert.True(q.EasyMode);
            Assert.True(q.IntroSeen);
            Assert.True(q.HousesIntroduced.Contains("bag"));
        }

        [Test("NOTIF-01")]
        public static void NotificationPlanRespectsQuietHoursCapAndConfig()
        {
            var clock = new ManualClock(new DateTime(2026, 11, 12, 18, 30, 0), TimeSpan.FromHours(3)); // yerel 21:30
            var m = NewMeta(clock);
            m.GiveUp(m.StartLevel(Region1[11]));                      // 1 can gitti → 3 saat sonra (00:30) dolar
            var plan = NotificationPlanner.Plan(m);
            var lives = plan.Find(n => n.Kind == NotifKind.LivesFull);
            Assert.True(lives != null && lives.FireLocal == new DateTime(2026, 11, 13, 9, 0, 0), "sessiz saat → 09:00");
            foreach (var g in plan.GroupBy(n => n.FireLocal.Date)) Assert.True(g.Count() <= 2, "günde en fazla 2");
            Assert.True(plan.All(n => n.FireLocal.Hour >= 9 && n.FireLocal.Hour < 22), "gece bildirim yok");
            m.Config.Apply("{\"notif_off\": \"LivesFull,Comeback3\"}");
            Assert.True(NotificationPlanner.Plan(m).All(n => n.Kind != NotifKind.LivesFull && n.Kind != NotifKind.Comeback3), "uzaktan kapatma");
            m.Profile.Notifications = false;
            Assert.Equal(0, NotificationPlanner.Plan(m).Count, "oyuncu kapattı");
        }

        [Test("HINT-01")]
        public static void CoinHintGivesSolutionMoveAndCharges()
        {
            var m = NewMeta();
            var run = m.StartLevel(Region1[11]);
            int before = m.Wallet.Coins;
            var mv = m.BuyHint(run);
            Assert.True(mv != null, "ipucu var");
            Assert.Equal(before - m.Config.HintCoins, m.Wallet.Coins, "coin düştü");
            var anchorPiece = run.Session.Tray[mv.Slot];
            Assert.True(anchorPiece != null && mv.Cells.Count == anchorPiece.Shape.Cells.Count, "parça boyu tutuyor");
        }

        [Test("STUCK-01")]
        public static void EarlyLevelsAndEasyModeAutoUndoInsteadOfFail()
        {
            var m = NewMeta();
            Assert.True(m.StartLevel(Region1[1]).Session.AutoUndoOnStuck, "öğrenme bölümü");
            Assert.True(!m.StartLevel(Region1[11]).Session.AutoUndoOnStuck, "normal bölüm");
            m.Profile.EasyMode = true;
            Assert.True(m.StartLevel(Region1[11]).Session.AutoUndoOnStuck, "kolay mod");
        }

        [Test("LIFE-01")]
        public static void LivesConsumeOnStartRefundOnWinRegenHourly()
        {
            var m = NewMeta();
            var clock = (ManualClock)m.Clock;
            Assert.Equal(3, m.Lives.Count);
            m.GiveUp(m.StartLevel(Region1[0]));
            Assert.Equal(3, m.Lives.Count, "ilk 10 bölümde kayıp can götürmez");
            var run = m.StartLevel(Region1[11]);
            Assert.Equal(2, m.Lives.Count, "başlarken 1 can");
            m.GiveUp(run);
            Assert.Equal(2, m.Lives.Count, "pes etmek ek can yakmaz");
            m.StartLevel(Region1[11]); m.StartLevel(Region1[11]);
            Assert.Equal(0, m.Lives.Count);
            Assert.False(m.Lives.CanPlay);
            Assert.True(m.Lives.FullIn.TotalMinutes > 179 && m.Lives.FullIn.TotalMinutes <= 180, "3 saatte dolar");
            clock.Advance(System.TimeSpan.FromHours(2));
            Assert.Equal(0, m.Lives.Count, "süre dolmadan can yok");
            clock.Advance(System.TimeSpan.FromHours(1.01));
            Assert.Equal(3, m.Lives.Count, "3 saatte hepsi birden");
            m.Config.Apply("{\"lives_refill_hours\": 5}");
            Assert.Equal(5.0, m.Config.Lives.RefillHours, "uzaktan ayar");
            m.CompleteWin(WinLevel(m, Region1[11]));
            Assert.Equal(3, m.Lives.Count, "kazanınca iade");
        }

        [Test("LIFE-02")]
        public static void LivesRefillAndClockRollback()
        {
            var m = NewMeta();
            var clock = (ManualClock)m.Clock;
            m.StartLevel(Region1[0]); m.StartLevel(Region1[0]); m.StartLevel(Region1[0]);
            m.Wallet.AddCoins(1000);
            Assert.True(m.Lives.TryRefillWithCoins(m.Wallet));
            Assert.Equal(3, m.Lives.Count);
            m.StartLevel(Region1[0]);
            clock.Advance(System.TimeSpan.FromHours(-10)); // saat geri alındı
            Assert.Equal(2, m.Lives.Count, "geri saat can vermez");
            Assert.Equal(PurchaseOutcome.Granted, m.ApplyPurchase(Skus.Lives, "tx-l1"));
            Assert.Equal(3, m.Lives.Count);
            var p = PlayerProfile.FromJson(m.Profile.ToJson());
            Assert.Equal(3, p.Lives);
        }

        [Test("FAIL-09")]
        public static void ThirdConsecutiveGiveUpGiftsUndoOnce()
        {
            var m = NewMeta();
            var l = Region1[6]; // bölüm 7 → Geri Al açık
            var run = m.StartLevel(l);
            Assert.False(m.GiveUp(run));
            run = m.StartLevel(l);
            Assert.False(m.GiveUp(run));
            run = m.StartLevel(l);
            int before = m.Wallet.BoosterCount(Booster.Undo);
            Assert.True(m.WillGiftUndoOnGiveUp(run));
            Assert.True(m.GiveUp(run));
            Assert.Equal(before + 1, m.Wallet.BoosterCount(Booster.Undo));
            run = m.StartLevel(l);
            Assert.False(m.GiveUp(run), "bölüm başına 1 kez");
            Assert.Equal(4, m.Profile.AttemptsOf(7));
        }

        [Test("CFG-01..CFG-04")]
        public static void RemoteConfigOverridesOnlyGivenKeys()
        {
            var c = GameConfig.Default;
            c.Apply("{\"continue_prices\":[700,1500,2500],\"inter_from_level\":20,\"booster_prices\":{\"undo\":250}}");
            Assert.Equal(700, c.Economy.ContinuePrice(0));
            Assert.Equal(20, c.InterFromLevel);
            Assert.Equal(250, c.Economy.BoosterPrice[Booster.Undo]);
            Assert.Equal(400, c.Economy.BoosterPrice[Booster.Swap]);
            c.Apply("bozuk json");
            Assert.Equal(20, c.InterFromLevel, "bozuk config değeri bozmaz");
        }

        [Test("INT-04")]
        public static void TrustedClockIgnoresDeviceClockChange()
        {
            var device = NewClock();
            var trusted = new TrustedClock(device);
            trusted.Sync(new DateTime(2026, 11, 12, 12, 0, 0));
            device.UtcNow = device.UtcNow.AddDays(1); // oyuncu saati ileri aldı… ama fark sabit kalır
            Assert.Equal(new DateTime(2026, 11, 13, 12, 0, 0), trusted.UtcNow);
            trusted.Sync(new DateTime(2026, 11, 12, 12, 0, 5)); // tekrar senkron → gerçek zaman
            Assert.Equal(new DateTime(2026, 11, 12, 12, 0, 5), trusted.UtcNow);
        }

        [Test("SAVE-03")]
        public static void CloudMergeKeepsFurthestProgressAndPurchases()
        {
            var a = new PlayerProfile { MaxLevelReached = 10 };
            var b = new PlayerProfile { MaxLevelReached = 6, NoAds = true };
            b.Wallet.AddCoins(1000);
            var merged = PlayerProfile.Merge(a, b);
            Assert.Equal(10, merged.MaxLevelReached);
            Assert.True(merged.NoAds);
            Assert.Equal(1500, merged.Wallet.Coins);
        }
    }
}
