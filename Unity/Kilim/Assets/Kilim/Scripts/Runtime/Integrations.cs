// Gerçek SDK adaptörleri. Varsayılan derlemede kapalıdır (test servisleri çalışır).
// Açmak için: paketi kur → Player Settings › Scripting Define Symbols'a ekle:
//   KILIM_UNITY_IAP  → com.unity.purchasing 4.12+
//   KILIM_ADMOB      → Google Mobile Ads Unity Plugin 9.x (UMP dahil) + AdMob mediation adaptörleri
// Bu dosyadaki kod SDK'lar kurulmadan derlenmez; CI tip kontrolü yalnızca define'sız yolu doğrular.

#if KILIM_UNITY_IAP
using System;
using System.Collections.Generic;
using Kilim.Core;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

namespace Kilim.Game
{
    /// <summary>
    /// Unity IAP (StoreKit 2 / Play Billing). Makbuz sunucuda doğrulanmalı: ProcessPurchase'ta Pending dönüp
    /// doğrulama sonrası ConfirmPendingPurchase çağırmak üretim için önerilir (bkz. docs/integration.md).
    /// </summary>
    public sealed class UnityIapService : IIapService, IDetailedStoreListener
    {
        private IStoreController _store;
        private IExtensionProvider _ext;
        private Action<IapStatus, string> _inFlight;
        private Action<string, string> _deferred;
        private readonly IIapService _fallback;   // mağaza ürünü henüz tanımlı değilse (beta) test mağazası

        public UnityIapService(IIapService fallback = null) { _fallback = fallback; }

        private bool Live(string sku)
        {
            var p = _store?.products.WithID(sku);
            return p != null && p.availableToPurchase;
        }

        public void Initialize(Action<string, string> onDeferredSuccess)
        {
            _deferred = onDeferredSuccess;
            var b = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
            foreach (var p in Skus.CoinPacks) b.AddProduct(p.sku, ProductType.Consumable);
            b.AddProduct(Skus.Piggy, ProductType.Consumable);
            b.AddProduct(Skus.GoldPass, ProductType.Consumable);   // sezonluk: her sezon yeniden alınır
            b.AddProduct(Skus.NoAds, ProductType.NonConsumable);
            b.AddProduct(Skus.Starter, ProductType.NonConsumable);
            b.AddProduct(Skus.Lives, ProductType.Consumable);
            _fallback?.Initialize(onDeferredSuccess);
            UnityPurchasing.Initialize(this, b);
        }

        public string LocalizedPrice(string sku)
        {
            return Live(sku) ? _store.products.WithID(sku).metadata.localizedPriceString : Skus.UsdPrice(sku);
        }

        public void Purchase(string sku, Action<IapStatus, string> done)
        {
            if (!Live(sku)) { if (_fallback != null) _fallback.Purchase(sku, done); else done(IapStatus.Failed, null); return; }
            _inFlight = done;
            _store.InitiatePurchase(sku);
        }

        public void Restore(Action<List<(string sku, string tx)>> done)
        {
            var owned = new List<(string, string)>();
            void Collect()
            {
                if (_store != null)
                    foreach (var p in _store.products.all)
                        if (p.definition.type == ProductType.NonConsumable && p.hasReceipt) owned.Add((p.definition.id, p.transactionID));
                done(owned);
            }
#if UNITY_IOS
            _ext.GetExtension<IAppleExtensions>().RestoreTransactions((ok, msg) => Collect());
#else
            Collect(); // Google Play: sahip olunan ürünler başlatmada otomatik gelir
#endif
        }

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            _store = controller;
            _ext = extensions;
        }

        public void OnInitializeFailed(InitializationFailureReason error) => Debug.LogError($"[Kilim][IAP] init: {error}");
        public void OnInitializeFailed(InitializationFailureReason error, string message) => Debug.LogError($"[Kilim][IAP] init: {error} {message}");

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            var id = args.purchasedProduct.definition.id;
            var tx = args.purchasedProduct.transactionID;
            if (_inFlight != null) { var cb = _inFlight; _inFlight = null; cb(IapStatus.Success, tx); }
            else _deferred?.Invoke(id, tx);   // ertelenmiş ödeme ya da yarıda kalan işlem (UAT IAP-03)
            return PurchaseProcessingResult.Complete;
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureDescription d) => Fail(d.reason);
        public void OnPurchaseFailed(Product product, PurchaseFailureReason reason) => Fail(reason);

        private void Fail(PurchaseFailureReason reason)
        {
            var cb = _inFlight;
            _inFlight = null;
            cb?.Invoke(reason == PurchaseFailureReason.UserCancelled ? IapStatus.Cancelled : IapStatus.Failed, null);
        }
    }
}
#endif

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
#if UNITY_ANDROID
        private const string RewardedId = "ca-app-pub-3940256099942544/5224354917";
        private const string InterstitialId = "ca-app-pub-3940256099942544/1033173712";
#else
        private const string RewardedId = "ca-app-pub-3940256099942544/1712485313";
        private const string InterstitialId = "ca-app-pub-3940256099942544/4411468910";
#endif
        private RewardedAd _rewarded;
        private InterstitialAd _inter;
        private bool _personalized;

        public void Initialize(bool personalized, bool underAge)
        {
            _personalized = personalized && !underAge;
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
