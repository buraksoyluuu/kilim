using System;

namespace Kilim.Core
{
    public enum InterstitialBlock
    {
        None, NoAds, FirstSession, BeforeLevel, AfterFail, RecentPurchase, LevelGap, TimeGap, Tutorial, Blocked,
    }

    /// <summary>Reklam kuralları (Spec 2 §3, Spec 3 §3). Saf karar; SDK çağrısı Unity katmanında.</summary>
    public sealed class AdPolicy
    {
        private readonly GameConfig _cfg;
        private readonly IClock _clock;

        public AdPolicy(GameConfig cfg, IClock clock) { _cfg = cfg; _clock = clock; }

        /// <summary>Kazandın → Ana geçişinde tam ekran reklam gösterilebilir mi. Kayıptan sonra asla çağrılmaz.</summary>
        public InterstitialBlock CanShowInterstitial(PlayerProfile p, int levelJustWon, bool cameFromFail)
        {
            if (p.Age == AgeBand.Blocked || p.Age == AgeBand.Unknown) return InterstitialBlock.Blocked;
            if (cameFromFail) return InterstitialBlock.AfterFail;
            if (p.IsNoAdsActive(_clock.UtcNow)) return InterstitialBlock.NoAds;
            if (!p.FirstSessionDone) return InterstitialBlock.FirstSession;
            if (levelJustWon <= _cfg.TutorialLastLevel) return InterstitialBlock.Tutorial;
            if (levelJustWon < _cfg.InterFromLevel) return InterstitialBlock.BeforeLevel;
            if (p.LastPurchaseMs > 0 &&
                _clock.UtcNow - Data.FromMs(p.LastPurchaseMs) < TimeSpan.FromHours(_cfg.InterNoAfterPurchaseHours))
                return InterstitialBlock.RecentPurchase;
            int gap = p.Age == AgeBand.Teen ? Math.Max(_cfg.InterGapLevels, _cfg.InterTeenGapLevels) : _cfg.InterGapLevels;
            if (p.InterstitialCount > 0 && p.LevelsSinceInterstitial < gap) return InterstitialBlock.LevelGap;
            if (p.LastInterstitialMs > 0 &&
                _clock.UtcNow - Data.FromMs(p.LastInterstitialMs) < TimeSpan.FromSeconds(_cfg.InterGapSeconds))
                return InterstitialBlock.TimeGap;
            return InterstitialBlock.None;
        }

        /// <summary>Her kazanılan bölümden sonra çağrılır (reklam gösterilsin ya da gösterilmesin).</summary>
        public void OnLevelCompleted(PlayerProfile p) => p.LevelsSinceInterstitial++;

        public void OnInterstitialShown(PlayerProfile p)
        {
            p.LastInterstitialMs = Data.ToMs(_clock.UtcNow);
            p.LevelsSinceInterstitial = 0;
            p.InterstitialCount++;
        }

        /// <summary>3. tam ekran reklamdan sonra reklamsız paket bir kez önerilir.</summary>
        public bool ShouldOfferNoAds(PlayerProfile p) =>
            !p.NoAds && !p.NoAdsOfferShown && p.InterstitialCount >= _cfg.NoAdsOfferAfterInterstitials;

        // ---------- Ödüllü ----------

        public bool CanShowRewarded(PlayerProfile p)
        {
            if (p.Age == AgeBand.Blocked || p.Age == AgeBand.Unknown) return false;
            RollRewardedDay(p);
            if (p.RewardedToday >= _cfg.RewardedDailyCap) return false;
            if (p.LastRewardedMs > 0 &&
                _clock.UtcNow - Data.FromMs(p.LastRewardedMs) < TimeSpan.FromSeconds(_cfg.RewardedGapSeconds))
                return false;
            return true;
        }

        /// <summary>Yalnızca SDK "ödül kazanıldı" dediğinde çağrılır (UAT ADS-12).</summary>
        public void OnRewardedEarned(PlayerProfile p)
        {
            RollRewardedDay(p);
            p.RewardedToday++;
            p.LastRewardedMs = Data.ToMs(_clock.UtcNow);
        }

        private void RollRewardedDay(PlayerProfile p)
        {
            var day = _clock.LocalDate().ToString("yyyy-MM-dd");
            if (p.RewardedDay != day) { p.RewardedDay = day; p.RewardedToday = 0; }
        }

        /// <summary>Genç profilinde kişiselleştirilmiş reklam yok (tag_for_under_age_of_consent).</summary>
        public static bool PersonalizedAdsAllowed(PlayerProfile p, bool umpConsentPersonalized) =>
            p.Age == AgeBand.Adult && umpConsentPersonalized;
    }

    /// <summary>Başlangıç paketi (Spec 2 §3): bölüm 8 sonrası, 48 saat, bir kez. Sayaç gerçek süre (UAT OFFER-02).</summary>
    public sealed class StarterOffer
    {
        private readonly GameConfig _cfg;
        private readonly IClock _clock;
        public StarterOffer(GameConfig cfg, IClock clock) { _cfg = cfg; _clock = clock; }

        /// <summary>Bölüm kazanıldıktan sonra çağrılır; eşik geçildiyse sayaç başlar.</summary>
        public void OnLevelWon(PlayerProfile p, int level)
        {
            if (p.StarterOfferStartMs == 0 && !p.StarterBought && level >= _cfg.StarterPackLevel)
                p.StarterOfferStartMs = Data.ToMs(_clock.UtcNow);
        }

        public TimeSpan Remaining(PlayerProfile p)
        {
            if (p.StarterOfferStartMs == 0 || p.StarterBought) return TimeSpan.Zero;
            var end = Data.FromMs(p.StarterOfferStartMs) + TimeSpan.FromHours(_cfg.StarterPackHours);
            var left = end - _clock.UtcNow;
            return left > TimeSpan.Zero ? left : TimeSpan.Zero;
        }

        public bool IsVisible(PlayerProfile p) => Remaining(p) > TimeSpan.Zero;
    }
}
