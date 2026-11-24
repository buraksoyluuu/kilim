using System;
using System.Collections.Generic;
using System.IO;
using Kilim.Core;
using UnityEngine;

namespace Kilim.Game
{
    // =====================================================================
    // Kayıt
    // =====================================================================

    public interface ISaveStore
    {
        string Load();
        void Save(string json);
        void Delete();
    }

    /// <summary>Atomik dosya kaydı: önce .tmp yazılır, sonra yer değiştirilir (yarıda kesilen yazma kaydı bozmaz).</summary>
    public sealed class FileSaveStore : ISaveStore
    {
        private readonly string _path;
        public FileSaveStore(string fileName = "kilim_save.json")
        {
            _path = Path.Combine(Application.persistentDataPath, fileName);
        }

        public string Load()
        {
            try { return File.Exists(_path) ? File.ReadAllText(_path) : null; }
            catch (Exception e) { Debug.LogError($"[Kilim] Kayıt okunamadı: {e.Message}"); return null; }
        }

        public void Save(string json)
        {
            var tmp = _path + ".tmp";
            try
            {
                File.WriteAllText(tmp, json);
                if (File.Exists(_path)) File.Delete(_path);
                File.Move(tmp, _path);
            }
            catch (Exception e) { Debug.LogError($"[Kilim] Kayıt yazılamadı: {e.Message}"); }
        }

        public void Delete()
        {
            try { if (File.Exists(_path)) File.Delete(_path); }
            catch (Exception e) { Debug.LogError($"[Kilim] Kayıt silinemedi: {e.Message}"); }
        }
    }

    // =====================================================================
    // Reklam
    // =====================================================================

    public interface IAdService
    {
        /// <summary>SDK'yı rıza sonrası başlatır. Genç profilinde kişiselleştirilmemiş istek (UAT PRV-05).</summary>
        void Initialize(bool personalized, bool childDirectedOrUnderAge);
        bool RewardedReady { get; }
        /// <summary>done(true) yalnızca ödül kazanıldığında; erken kapatma/yükleme hatası = false (UAT ADS-12).</summary>
        void ShowRewarded(string placement, Action<bool> done);
        void ShowInterstitial(string placement, Action done);
    }

    /// <summary>Editör/test reklamı: gerçek SDK yerine test penceresi açar. Gerçek ağ: AdMobAdService (KILIM_ADMOB).</summary>
    public sealed class MockAdService : IAdService
    {
        private readonly App _app;
        public bool Initialized { get; private set; }
        public bool Personalized { get; private set; }
        public MockAdService(App app) { _app = app; }

        public void Initialize(bool personalized, bool underAge)
        {
            Initialized = true;
            Personalized = personalized && !underAge;
            Debug.Log($"[Kilim][Ads] init personalized={Personalized} underAge={underAge}");
        }

        public bool RewardedReady => Initialized;

        public void ShowRewarded(string placement, Action<bool> done)
        {
            if (!Initialized) { done(false); return; }
            _app.Nav.Confirm("Test reklamı (ödüllü)", $"Yerleşim: {placement}\nGerçek cihazda burada reklam oynar.",
                "Sonuna kadar izle", () => done(true), "Erken kapat", () => done(false));
        }

        public void ShowInterstitial(string placement, Action done)
        {
            if (!Initialized) { done(); return; }
            _app.Nav.Confirm("Test reklamı (tam ekran)", $"Yerleşim: {placement}", "Kapat", done, null, null);
        }
    }

    // =====================================================================
    // Satın alma
    // =====================================================================

    public enum IapStatus { Success, Cancelled, Pending, Failed }

    public interface IIapService
    {
        void Initialize(Action<string, string> onDeferredSuccess);
        string LocalizedPrice(string sku);
        /// <summary>done(status, transactionId). Bekleyen ödeme (Android ertelenmiş) sonradan onDeferredSuccess ile gelir.</summary>
        void Purchase(string sku, Action<IapStatus, string> done);
        void Restore(Action<List<(string sku, string tx)>> done);
    }

    /// <summary>Editör/test mağazası. Gerçek mağaza: UnityIapService (KILIM_UNITY_IAP) + sunucu makbuz doğrulama.</summary>
    public sealed class MockIapService : IIapService
    {
        private readonly App _app;
        private Action<string, string> _deferred;
        private readonly List<(string, string)> _owned = new List<(string, string)>();
        public MockIapService(App app) { _app = app; }

        public void Initialize(Action<string, string> onDeferredSuccess) => _deferred = onDeferredSuccess;

        public string LocalizedPrice(string sku) => Skus.UsdPrice(sku);

        public void Purchase(string sku, Action<IapStatus, string> done)
        {
            var tx = "test-" + Guid.NewGuid().ToString("N").Substring(0, 12);
            _app.Nav.Choice("Test satın alma", $"{sku}\n{LocalizedPrice(sku)}", new (string, Action)[]
            {
                ("Satın al", () =>
                {
                    if (!Skus.IsConsumable(sku)) _owned.Add((sku, tx));
                    done(IapStatus.Success, tx);
                }),
                ("Bekleyen ödeme (5 sn)", () =>
                {
                    done(IapStatus.Pending, null);
                    _app.Delay(5f, () => _deferred?.Invoke(sku, tx));
                }),
                ("İptal", () => done(IapStatus.Cancelled, null)),
            });
        }

        public void Restore(Action<List<(string sku, string tx)>> done) => done(new List<(string, string)>(_owned));
    }

    // =====================================================================
    // Analitik (rıza kapısı)
    // =====================================================================

    public interface IAnalyticsSink { void Send(string name, Dictionary<string, object> props); }

    public sealed class DebugAnalyticsSink : IAnalyticsSink
    {
        public void Send(string name, Dictionary<string, object> props) =>
            Debug.Log($"[Kilim][Analytics] {name} {MiniJson.Serialize(props)}");
    }

    /// <summary>
    /// KVKK açık rızası yoksa yalnızca zorunlu teknik olaylar, anonim gönderilir (Spec 2 §5). Rıza verilmeden
    /// SDK başlamaz (UAT PRV-02). Olay adları Spec 3 §2.
    /// </summary>
    public sealed class Analytics
    {
        private static readonly HashSet<string> Technical = new HashSet<string> { "app_start", "crash", "iap_result", "age_gate" };
        private readonly IAnalyticsSink _sink;
        private readonly Func<PlayerProfile> _profile;

        public Analytics(IAnalyticsSink sink, Func<PlayerProfile> profile) { _sink = sink; _profile = profile; }

        public void Track(string name, params (string k, object v)[] props)
        {
            var p = _profile();
            bool consent = p != null && p.ConsentDone && p.AnalyticsConsent;
            if (!consent && !Technical.Contains(name)) return;
            var d = new Dictionary<string, object>();
            foreach (var (k, v) in props) d[k] = v;
            if (consent && p != null) d["pid"] = p.PlayerId;
            _sink.Send(name, d);
        }
    }

    // =====================================================================
    // Rıza (UMP / ATT)
    // =====================================================================

    public interface IConsentService
    {
        /// <summary>AB/İngiltere'de UMP formu gerekir mi (cihaz bölgesi / UMP isteği).</summary>
        bool UmpRequired { get; }
        void ShowUmpForm(Action<bool> personalizedAllowed);
        bool AttSupported { get; }
        void RequestAtt(Action<bool> granted);
    }

    public sealed class MockConsentService : IConsentService
    {
        private readonly App _app;
        public MockConsentService(App app) { _app = app; }

        public bool UmpRequired => false; // gerçek: UMP ConsentInformation.Update sonucu
        public void ShowUmpForm(Action<bool> done) =>
            _app.Nav.Confirm("Reklam tercihleri (UMP test)", "Kişiselleştirilmiş reklamlara izin?", "İzin ver",
                () => done(true), "Reddet", () => done(false));

#if UNITY_IOS
        public bool AttSupported => true;
#else
        public bool AttSupported => false;
#endif
        public void RequestAtt(Action<bool> granted) => granted(false); // gerçek: ATTrackingStatusBinding
    }

    // =====================================================================
    // Ses / titreşim
    // =====================================================================

    public static class Audio
    {
        public static bool Sfx = true;
        public static bool Haptics = true;

        public static void Click() { }

        public static void Place()
        {
#if UNITY_ANDROID || UNITY_IOS
            if (Haptics) Handheld.Vibrate();
#endif
        }
    }

    // =====================================================================
    // Bölümler
    // =====================================================================

    /// <summary>Bölümler: Resources/levels/region{n}.json (Addressables'a geçiş Spec 1 §6 — aynı arayüz).</summary>
    public sealed class Levels
    {
        public const int PerRegion = 20;
        private readonly Dictionary<int, List<LevelData>> _regions = new Dictionary<int, List<LevelData>>();

        public int Count { get; private set; }

        public void LoadAll()
        {
            for (int r = 1; r <= 10; r++)
            {
                var asset = Resources.Load<TextAsset>($"levels/region{r}");
                if (asset == null) break;
                var list = LevelParser.ParseMany(asset.text);
                foreach (var l in list)
                {
                    var errs = l.Validate();
                    if (errs.Count > 0) Debug.LogError($"[Kilim] Bölüm {l.Id} geçersiz: {string.Join("; ", errs)}");
                }
                _regions[r] = list;
                Count += list.Count;
            }
            Debug.Log($"[Kilim] {Count} bölüm yüklendi");
        }

        public LevelData Get(int id)
        {
            int region = (id - 1) / PerRegion + 1;
            return _regions.TryGetValue(region, out var list) ? list.Find(l => l.Id == id) : null;
        }

        public IReadOnlyList<LevelData> Region(int r) => _regions.TryGetValue(r, out var l) ? l : new List<LevelData>();
    }
}
