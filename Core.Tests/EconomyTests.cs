using static Kilim.Core.Tests.TestLevels;

namespace Kilim.Core.Tests
{
    public static class EconomyTests
    {
        private static EconomyConfig Cfg => EconomyConfig.Default;

        private static GameSession FailedSession()
        {
            var s = new GameSession(ThreeByThree());
            s.Place(0, new Cell(0, 1));
            s.Place(2, new Cell(2, 0));
            Assert.Equal(SessionState.Failed, s.State);
            return s;
        }

        [Test("ECO-01")]
        public static void StartingBalance() => Assert.Equal(500, new Wallet(Cfg.StartingCoins).Coins);

        [Test("WIN-03")]
        public static void WinCoinsFormula()
        {
            Assert.Equal(20, Cfg.WinCoins(1, false));
            Assert.Equal(30, Cfg.WinCoins(2, false));
            Assert.Equal(40, Cfg.WinCoins(3, false));
            Assert.Equal(140, Cfg.WinCoins(3, true));
        }

        [Test("STAR-06")]
        public static void IlmekOnlyAddsImprovement()
        {
            var w = new Wallet(0);
            Assert.Equal(2, w.ApplyWin(5, 2, false, Cfg).ilmek);
            Assert.Equal(0, w.ApplyWin(5, 1, false, Cfg).ilmek, "daha kötü tekrar");
            Assert.Equal(1, w.ApplyWin(5, 3, false, Cfg).ilmek, "yalnızca fark");
            Assert.Equal(3, w.Ilmek);
            Assert.Equal(3, w.BestStars(5));
            Assert.Equal(30 + 20 + 40, w.Coins, "coin her kazanışta");
        }

        [Test("PIG-01, PIG-02")]
        public static void PiggyCapsAt2000()
        {
            var w = new Wallet(0);
            for (int i = 0; i < 120; i++) w.ApplyWin(i, 1, false, Cfg);
            Assert.Equal(2000, w.Piggy);
            Assert.Equal(2000, w.BreakPiggy());
            Assert.Equal(0, w.Piggy);
        }

        [Test("IAP-10")]
        public static void RefundNeverGoesNegative()
        {
            var w = new Wallet(500);
            w.Revoke(6000);
            Assert.Equal(0, w.Coins);
        }

        [Test("BST-08")]
        public static void BoostersUnlockWithThreeFree()
        {
            var w = new Wallet(0);
            w.UnlockBoostersUpTo(5, Cfg);
            Assert.False(w.IsUnlocked(Booster.Undo));
            w.UnlockBoostersUpTo(9, Cfg);
            Assert.Equal(3, w.BoosterCount(Booster.Undo));
            Assert.Equal(3, w.BoosterCount(Booster.Swap));
            Assert.False(w.IsUnlocked(Booster.SingleStitch));
            w.UnlockBoostersUpTo(12, Cfg);
            w.UnlockBoostersUpTo(13, Cfg);
            Assert.Equal(3, w.BoosterCount(Booster.SingleStitch), "ikinci kez verilmez");
        }

        [Test("BST-09")]
        public static void BoosterNotChargedWhenNotApplicable()
        {
            var w = new Wallet(1000);
            w.UnlockBoostersUpTo(12, Cfg);
            var svc = new BoosterService(w, Cfg);
            var s = new GameSession(ThreeByThree());
            Assert.Equal(BoosterUseResult.NotApplicable, svc.Use(s, Booster.Undo));
            Assert.Equal(BoosterUseResult.NotApplicable, svc.Use(s, Booster.Swap, 0), "kuyruk boş");
            Assert.Equal(3, w.BoosterCount(Booster.Undo));
            Assert.Equal(1000, w.Coins);
        }

        [Test("BST-06")]
        public static void BoosterBoughtWithCoinsWhenStockEmpty()
        {
            var w = new Wallet(700);
            w.UnlockBoostersUpTo(12, Cfg);
            var svc = new BoosterService(w, Cfg);
            var s = new GameSession(Legacy1);
            for (int i = 0; i < 3; i++) Assert.Equal(BoosterUseResult.Applied, svc.Use(s, Booster.Swap, 0));
            Assert.Equal(0, w.BoosterCount(Booster.Swap));
            Assert.Equal(BoosterUseResult.Applied, svc.Use(s, Booster.Swap, 0));
            Assert.Equal(300, w.Coins, "400 düştü");
            Assert.Equal(BoosterUseResult.NotEnoughCoins, svc.Use(s, Booster.Swap, 0));
            Assert.Equal(300, w.Coins);
        }

        [Test("FAIL-05, FAIL-06")]
        public static void ContinueLadderPrices()
        {
            var w = new Wallet(10000);
            var ladder = new ContinueLadder(Cfg);
            var s = FailedSession();
            Assert.Equal(900, ladder.CurrentPrice);
            Assert.True(ladder.TryContinueWithCoins(w, s));
            Assert.Equal(9100, w.Coins);
            Assert.Equal(SessionState.Playing, s.State);
            Assert.Equal(2, w.BoosterCount(Booster.SingleStitch));
            Assert.Equal(1, w.BoosterCount(Booster.Swap));
            Assert.Equal(1900, ladder.CurrentPrice);
            Assert.False(ladder.CanUseAd, "reklam yalnızca 1. basamakta");
        }

        [Test("BST-10")]
        public static void DoubleTapContinueChargesOnce()
        {
            var w = new Wallet(2000);
            var ladder = new ContinueLadder(Cfg);
            var s = FailedSession();
            Assert.True(ladder.TryContinueWithCoins(w, s));
            Assert.False(ladder.TryContinueWithCoins(w, s), "oturum artık kayıpta değil");
            Assert.Equal(1100, w.Coins);
        }

        [Test("FAIL-07, ADS-12")]
        public static void AdContinueOncePerLevel()
        {
            var w = new Wallet(0);
            var ladder = new ContinueLadder(Cfg);
            var s = FailedSession();
            Assert.True(ladder.CanUseAd, "reklam erken kapatıldıysa hak duruyor");
            Assert.True(ladder.ContinueWithRewardedAd(w, s));
            Assert.False(ladder.CanUseAd);
            Assert.False(ladder.TryContinueWithCoins(w, s), "oynanıyor");
        }

        [Test("FAIL-08")]
        public static void NotEnoughCoinsKeepsFailed()
        {
            var w = new Wallet(500);
            var ladder = new ContinueLadder(Cfg);
            var s = FailedSession();
            Assert.False(ladder.TryContinueWithCoins(w, s));
            Assert.Equal(500, w.Coins);
            Assert.Equal(SessionState.Failed, s.State);
        }
    }
}
