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

        private readonly Func<bool> _fallbackAllowed;   // uzaktan ayar iap_test_store (yayında kapalı)

        public UnityIapService(IIapService fallback = null, Func<bool> fallbackAllowed = null)
        {
            _fallback = fallback;
            _fallbackAllowed = fallbackAllowed ?? (() => false);
        }

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
            if (!Live(sku)) { if (_fallback != null && _fallbackAllowed()) _fallback.Purchase(sku, done); else done(IapStatus.Failed, null); return; }
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

