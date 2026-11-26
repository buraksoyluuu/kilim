// AdMob + UMP + ATT adaptörü (KILIM_ADMOB). Kimlikler: BuildScript.ConfigureAds / Resources/ads.json.
#if KILIM_ADMOB
using System;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

namespace Kilim.Game
{
    /// <summary>AdMob (mediation). Test ID'leri Google'ın herkese açık test birimleridir; yayında değiştir.</summary>
    public sealed class AdMobAdService : IAdService
    {
// Birim kimlikleri build anında Resources/ads.json'a yazılır (BuildScript.ConfigureAds, CI secret'ları);
        // yoksa Google'ın herkese açık test birimleri kullanılır.
        private static string RewardedId => Unit("rewarded",
#if UNITY_ANDROID
            "ca-app-pub-3940256099942544/5224354917");
#else
            "ca-app-pub-3940256099942544/1712485313");
#endif
        private static string InterstitialId => Unit("interstitial",
#if UNITY_ANDROID
            "ca-app-pub-3940256099942544/1033173712");
#else
            "ca-app-pub-3940256099942544/4411468910");
#endif

        /// <summary>Beta/test döneminde gerçek birim yerine Google test birimi (uzaktan ayar ads_test_mode=1).
        /// Kendi gerçek reklamına tıklamak AdMob politikasına aykırı; yayına kadar açık kalır.</summary>
        public static Func<bool> TestMode = () => false;

        private static string Unit(string kind, string test)
        {
            if (TestMode()) return test;
            var t = Resources.Load<TextAsset>("ads");
#if UNITY_ANDROID
            var key = "android_" + kind;
#else
            var key = "ios_" + kind;
#endif
            var m = t == null ? null : System.Text.RegularExpressions.Regex.Match(t.text, "\"" + key + "\"\\s*:\\s*\"([^\"]+)\"");
            return m != null && m.Success ? m.Groups[1].Value : test;
        }
        private RewardedAd _rewarded;
        private InterstitialAd _inter;
        private bool _personalized;

        public void Initialize(bool personalized, bool underAge)
        {
            _personalized = personalized && !underAge;
#pragma warning disable 618
            MobileAds.RaiseAdEventsOnUnityMainThread = true;   // geri çağrılar UI'ye dokunuyor
#pragma warning restore 618
            MobileAds.SetRequestConfiguration(new RequestConfiguration
            {
                TagForUnderAgeOfConsent = underAge ? TagForUnderAgeOfConsent.True : TagForUnderAgeOfConsent.False,
            });
            MobileAds.Initialize(_ => { LoadRewarded(); LoadInterstitial(); });
        }

        private AdRequest Request()
        {
            var r = new AdRequest();
            if (!_personalized) r.Extras.Add("npa", "1");
            return r;
        }

        private void LoadRewarded() => RewardedAd.Load(RewardedId, Request(), (ad, err) => { if (err == null) _rewarded = ad; });
        private void LoadInterstitial() => InterstitialAd.Load(InterstitialId, Request(), (ad, err) => { if (err == null) _inter = ad; });

        public bool RewardedReady => _rewarded != null && _rewarded.CanShowAd();

        public void ShowRewarded(string placement, Action<bool> done)
        {
            if (!RewardedReady) { done(false); LoadRewarded(); return; }
            bool earned = false;
            var ad = _rewarded;
            _rewarded = null;
            ad.OnAdFullScreenContentClosed += () => { done(earned); ad.Destroy(); LoadRewarded(); };
            ad.OnAdFullScreenContentFailed += _ => { done(false); ad.Destroy(); LoadRewarded(); };
            ad.Show(_ => earned = true);
        }

        public void ShowInterstitial(string placement, Action done)
        {
            if (_inter == null || !_inter.CanShowAd()) { done(); LoadInterstitial(); return; }
            var ad = _inter;
            _inter = null;
            ad.OnAdFullScreenContentClosed += () => { done(); ad.Destroy(); LoadInterstitial(); };
            ad.OnAdFullScreenContentFailed += _ => { done(); ad.Destroy(); LoadInterstitial(); };
            ad.Show();
        }
    }

    /// <summary>Google UMP (IAB TCF v2.2) + iOS ATT.</summary>
    public sealed class UmpConsentService : IConsentService
    {
        private bool _required;

        public UmpConsentService()
        {
            ConsentInformation.Update(new ConsentRequestParameters(), err =>
            {
                _required = err == null && ConsentInformation.ConsentStatus == ConsentStatus.Required;
            });
        }

        public bool UmpRequired => _required;

        public void ShowUmpForm(Action<bool> personalizedAllowed) =>
            ConsentForm.LoadAndShowConsentFormIfRequired(err =>
                personalizedAllowed(err == null && ConsentInformation.CanRequestAds()));

#if UNITY_IOS
        public bool AttSupported => true;
        public void RequestAtt(Action<bool> granted)
        {
            // Unity iOS 14 Advertising Support paketi: Unity.Advertisement.IosSupport
            Unity.Advertisement.IosSupport.ATTrackingStatusBinding.RequestAuthorizationTracking(status =>
                granted(status == (int)Unity.Advertisement.IosSupport.ATTrackingStatusBinding.AuthorizationTrackingStatus.AUTHORIZED));
        }
#else
        public bool AttSupported => false;
        public void RequestAtt(Action<bool> granted) => granted(false);
#endif
    }
}
#endif
