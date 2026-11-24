using System;
using System.Collections;
using Kilim.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Kilim.Game
{
    /// <summary>
    /// Uygulama kökü. Boş bir sahnede Play'e basınca kendini kurar (sahne/prefab gerekmez).
    /// Akış: Açılış → Yaş → Rıza → (Öğretici) Ana. Spec 3 §1.
    /// </summary>
    public sealed class App : MonoBehaviour
    {
        public static App I { get; private set; }

        public Meta Meta { get; private set; }
        public PlayerProfile Profile => Meta.Profile;
        public GameConfig Config { get; private set; }
        public TrustedClock Clock { get; private set; }
        public Levels Levels { get; private set; }
        public Navigator Nav { get; private set; }
        public Analytics Analytics { get; private set; }
        public IAdService Ads { get; private set; }
        public IIapService Iap { get; private set; }
        public IConsentService Consent { get; private set; }
        public ISaveStore Store { get; private set; }
        public Backend Backend { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (I != null) return;
            var go = new GameObject("Kilim.App");
            DontDestroyOnLoad(go);
            go.AddComponent<App>();
        }

        private void Awake()
        {
            I = this;
            Application.targetFrameRate = 60;
            Audio.Init(gameObject);
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            BuildCanvas();

            Config = GameConfig.Default;
            var remote = Resources.Load<TextAsset>("remote_config_default");
            if (remote != null) Config.Apply(remote.text);   // gerçek: Remote Config, 500 ms zaman aşımı
            Clock = new TrustedClock(new SystemClock());       // gerçek: sunucu zamanıyla Sync()
            Levels = new Levels();
            Levels.LoadAll();
            Store = new FileSaveStore();
            LoadProfile();
            Analytics = new Analytics(new DebugAnalyticsSink(), () => Meta?.Profile);
#if KILIM_ADMOB
            Ads = new AdMobAdService();
            Consent = new UmpConsentService();
#else
            Ads = new MockAdService(this);
            Consent = new MockConsentService(this);
#endif
#if KILIM_UNITY_IAP
            Iap = new UnityIapService();
#else
            Iap = new MockIapService(this);
#endif
            Iap.Initialize(OnDeferredPurchase);
            Backend = new Backend(this);
        }

        private void Start()
        {
            Analytics.Track("app_start", ("version", Application.version));
            Nav.Show(new SplashScreen());
            StartCoroutine(Backend.Startup());
        }

        /// <summary>Bulut kaydı yereldekinden ilerideyse profil değiştirilir; açık ekran yeniden kurulur.</summary>
        public void ReplaceProfile(PlayerProfile p)
        {
            p.FirstSessionDone = Profile.FirstSessionDone;
            Meta = new Meta(p, Config, Clock);
            ApplySettings();
            Save();
            if (Nav.Current is HomeScreen) GoHome();
        }

        private void LoadProfile()
        {
            var json = Store.Load();
            var profile = json != null ? PlayerProfile.FromJson(json) : null;
            if (json != null && profile == null) Debug.LogError("[Kilim] Kayıt bozuk; yeni profil başlatıldı");
            if (profile != null) profile.FirstSessionDone = true; // ikinci açılıştan itibaren (ilk oturumda tam ekran reklam yok)
            if (profile == null) profile = new PlayerProfile { Language = Loc.SystemDefault() };
            Meta = new Meta(profile, Config, Clock);
            ApplySettings();
        }

        public void Save()
        {
            Store.Save(Profile.ToJson());
            Backend?.CloudSave();
        }

        public void ApplySettings()
        {
            Audio.Sfx = Profile.Sfx;
            Audio.Haptics = Profile.Haptics;
            Audio.SetMusic(Profile.Music);
            Loc.Set(Profile.Language);
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && Meta != null) { Save(); Backend?.CloudSave(force: true); }
            else if (!paused && Backend != null && Backend.Online) StartCoroutine(Backend.SyncRefunds());
        }

        private void OnApplicationQuit() { if (Meta != null) { Profile.FirstSessionDone = true; Save(); Backend?.CloudSave(force: true); } }

        // ---------------------------------------------------------------
        // Kanvas
        // ---------------------------------------------------------------

        private void BuildCanvas()
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0f;

            if (FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                es.transform.SetParent(transform, false);
            }

            var bg = Ui.Panel(canvasGo.transform, "Background", Color.white);
            var bgImg = bg.GetComponent<Image>();
            bgImg.sprite = Art.Pattern;
            bgImg.type = Image.Type.Tiled;
            if (bgImg.sprite == null) bgImg.color = Theme.Bg;
            var safe = Ui.Rect(canvasGo.transform, "SafeArea").Stretch();
            safe.gameObject.AddComponent<SafeArea>();
            Nav = new Navigator(this, safe, (RectTransform)canvasGo.transform);
            bg.SetAsFirstSibling();
        }

        // ---------------------------------------------------------------
        // Yardımcılar
        // ---------------------------------------------------------------

        public void Delay(float seconds, Action a) => StartCoroutine(DelayCo(seconds, a));

        private static IEnumerator DelayCo(float s, Action a)
        {
            yield return new WaitForSecondsRealtime(s);
            a?.Invoke();
        }

        /// <summary>Rıza tamamlanınca SDK'ları başlatır (UAT PRV-02: rızadan önce başlamaz).</summary>
        public void InitSdksAfterConsent(bool umpPersonalized)
        {
            bool personalized = AdPolicy.PersonalizedAdsAllowed(Profile, umpPersonalized);
            Ads.Initialize(personalized, Profile.Age != AgeBand.Adult);
        }

        public void GoHome() => Nav.Show(new HomeScreen());

        public void StartLevel(int levelId)
        {
            var level = Levels.Get(levelId);
            if (level == null) { Nav.Show(new HomeScreen()); return; }
            var run = Meta.StartLevel(level);
            Save();
            Analytics.Track("level_start", ("level", level.Id), ("attempt", run.Attempt), ("region", level.Region));
            Nav.Show(new GameScreen(run));
        }

        public void StartDaily()
        {
            var level = Meta.Daily.PickLevel(Levels.Region(1));
            var run = Meta.StartLevel(level, isDaily: true);
            Nav.Show(new GameScreen(run));
        }

        /// <summary>Ödüllü reklam: kural kontrolü + SDK. onEarned yalnızca ödül kazanılınca.</summary>
        public void ShowRewarded(string placement, Action onEarned, Action onNotEarned = null)
        {
            if (!Meta.Ads.CanShowRewarded(Profile) || !Ads.RewardedReady)
            {
                Nav.Toast("Şu an reklam yok, birazdan tekrar dene.");
                onNotEarned?.Invoke();
                return;
            }
            Ads.ShowRewarded(placement, earned =>
            {
                Analytics.Track("ad_rewarded", ("placement", placement), ("result", earned ? "earned" : "closed"));
                if (earned) { onEarned?.Invoke(); }
                else { Nav.Toast("Reklam tamamlanmadı, ödül verilmedi."); onNotEarned?.Invoke(); }
                Save();
            });
        }

        /// <summary>Kazandın → Ana geçişi; kural uygunsa tam ekran reklam (UAT ADS-01..09).</summary>
        public void LeaveWinToHome(int levelWon)
        {
            var block = Meta.Ads.CanShowInterstitial(Profile, levelWon, cameFromFail: false);
            if (block == InterstitialBlock.None)
            {
                Ads.ShowInterstitial("win_to_home", () =>
                {
                    Meta.Ads.OnInterstitialShown(Profile);
                    Analytics.Track("ad_interstitial", ("placement", "win_to_home"), ("result", "shown"));
                    Save();
                    GoHome();
                    if (Meta.Ads.ShouldOfferNoAds(Profile))
                    {
                        Profile.NoAdsOfferShown = true;
                        Save();
                        Nav.Confirm("Reklamsız oyna", "Tam ekran reklamları kalıcı olarak kapat. Ödüllü reklamlar isteğe bağlı kalır.",
                            $"Reklamsız · {Iap.LocalizedPrice(Skus.NoAds)}", () => Buy(Skus.NoAds, "noads_offer"), "Şimdi değil", null);
                    }
                });
            }
            else GoHome();
        }

        // ---------------------------------------------------------------
        // Satın alma
        // ---------------------------------------------------------------

        /// <summary>Genç profilinde ek "gerçek para" onayı (Spec 2 §5), sonra mağaza.</summary>
        public void Buy(string sku, string source, Action onGranted = null)
        {
            Analytics.Track("iap_view", ("sku", sku), ("source", source));
            if (Profile.Age == AgeBand.Teen)
            {
                Nav.Confirm("Bu satın alma gerçek para", $"{Iap.LocalizedPrice(sku)} ödenecek. Emin misin?",
                    "Evet, satın al", () => DoBuy(sku, source, onGranted), "Vazgeç", null);
            }
            else DoBuy(sku, source, onGranted);
        }

        private bool _purchaseInFlight;

        private void DoBuy(string sku, string source, Action onGranted)
        {
            if (_purchaseInFlight) return; // çift dokunma (UAT IAP-11)
            _purchaseInFlight = true;
            Iap.Purchase(sku, (status, tx) =>
            {
                _purchaseInFlight = false;
                Analytics.Track("iap_result", ("sku", sku), ("source", source), ("result", status.ToString()));
                switch (status)
                {
                    case IapStatus.Success:
                        Grant(sku, tx);
                        onGranted?.Invoke();
                        break;
                    case IapStatus.Pending:
                        Nav.Toast("Ödeme onay bekliyor. Onaylanınca içerik eklenecek.");
                        break;
                    case IapStatus.Failed:
                        Nav.Toast("Satın alma tamamlanamadı. Ücret alınmadı.");
                        break;
                }
            });
        }

        private void OnDeferredPurchase(string sku, string tx)
        {
            Grant(sku, tx);
            Nav.Toast("Bekleyen ödeme onaylandı, içerik eklendi.");
        }

        /// <summary>Sunucu doğrulaması (backend varsa) → teslim. Doğrulanamayan makbuz teslim edilmez.</summary>
        private void Grant(string sku, string tx)
        {
            Backend.VerifyPurchase(sku, tx, (valid, granted) =>
            {
                if (!valid) { Nav.Toast("Satın alma doğrulanamadı. Destek ile iletişime geç."); return; }
                if (!granted) { Nav.Toast("Bu satın alma zaten teslim edilmiş."); return; }
                GrantLocal(sku, tx);
            });
        }

        private void GrantLocal(string sku, string tx)
        {
            int cents = PriceCents(sku);
            if (Meta.ApplyPurchase(sku, tx, cents) == PurchaseOutcome.Granted)
            {
                Save();
                Audio.Play("coin");
                Nav.Toast("Satın alma tamamlandı.");
                if (Meta.TeenSpendNoticeDue)
                    Nav.Confirm("Bilgilendirme", "Bu ay 100 $ üzerinde harcama yaptın. Harcamalarını bir yetişkinle konuşmanı öneririz.", "Tamam", null, null, null);
            }
        }

        public void RestorePurchases()
        {
            Iap.Restore(list =>
            {
                foreach (var (sku, tx) in list) Meta.ApplyPurchase(sku, tx, 0, isRestore: true);
                Save();
                Nav.Toast(list.Count > 0 ? "Satın alımlar geri yüklendi." : "Geri yüklenecek satın alım bulunamadı.");
            });
        }

        private static int PriceCents(string sku)
        {
            var s = Skus.UsdPrice(sku).Replace(" $", "").Replace(",", ".");
            return double.TryParse(s, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var d) ? (int)Math.Round(d * 100) : 0;
        }

        /// <summary>Ayarlar › Verilerimi sil (UAT PRV-07): yerel kayıt silinir, sunucuya silme isteği, ilk açılış akışı.</summary>
        public void DeleteAllData() => StartCoroutine(DeleteAllDataCo());

        private IEnumerator DeleteAllDataCo()
        {
            yield return Backend.DeleteAccount();
            Store.Delete();
            Meta = new Meta(new PlayerProfile { Language = Loc.SystemDefault() }, Config, Clock);
            ApplySettings();
            Nav.Show(new SplashScreen());
        }
    }

    /// <summary>Ekranı güvenli alana (çentik, ana çubuk) sığdırır.</summary>
    public sealed class SafeArea : MonoBehaviour
    {
        private Rect _last;
        private void Update()
        {
            var sa = Screen.safeArea;
            if (sa == _last || Screen.width == 0) return;
            _last = sa;
            var rt = (RectTransform)transform;
            rt.anchorMin = new Vector2(sa.xMin / Screen.width, sa.yMin / Screen.height);
            rt.anchorMax = new Vector2(sa.xMax / Screen.width, sa.yMax / Screen.height);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
