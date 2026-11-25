using System;
using Kilim.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Kilim.Game
{
    /// <summary>8.1 Açılış: kilim deseni açılır, logo zıplar (≤ 1,6 sn); kayıt ve config App.Awake'te yüklendi.</summary>
    public sealed class SplashScreen : UiScreen
    {
        public override void Build(RectTransform root)
        {
            var v = Ui.VStack(root, 18, 0, TextAnchor.MiddleCenter).Stretch();
            var logo = Ui.Icon(v, Art.Get("logo"), 420);
            if (logo.sprite == null) logo.gameObject.SetActive(false);
            Fx.PopIn(logo.transform, 0.6f, 0.05f);
            var t = Ui.Label(v, "Kilim", 150, Theme.Primary, TextAnchor.MiddleCenter, true, 190);
            Fx.PopIn(t.transform, 0.55f, 0.25f);
            var sub = Ui.Label(v, "desen desen, ilmek ilmek", 42, Theme.Muted, TextAnchor.MiddleCenter, false, 60);
            var g = sub.gameObject.AddComponent<CanvasGroup>();
            Fx.Fade(g, 0, 1, 0.5f, 0.6f);
            App.Delay(1.6f, Route);
        }

        public static void Route()
        {
            var app = App.I;
            var p = app.Profile;
            if (p.Age == AgeBand.Unknown) { app.Nav.Show(new AgeGateScreen()); return; }
            if (p.Age == AgeBand.Blocked) { app.Nav.Show(new BlockedScreen()); return; }
            if (!p.ConsentDone) { app.Nav.Show(new ConsentScreen()); return; }
            app.InitSdksAfterConsent(p.UmpDone);
            bool fresh = p.CurrentLevel == 1 && p.AttemptsOf(1) == 0;
            if (!p.IntroSeen)
            {
                app.Nav.Show(new IntroScreen(() => { if (fresh) app.StartLevel(1); else app.GoHome(); }));
                return;
            }
            if (fresh) app.StartLevel(1);
            else app.GoHome();
        }
    }

    /// <summary>
    /// Tanıtım hikâyesi (ilk açılış, Ayarlar'dan yeniden izlenebilir): Nine, köydeki eski ev ve dokuma tezgâhı.
    /// 4 sayfa, kaydırmalı geçiş; "Geç" her sayfada.
    /// </summary>
    public sealed class IntroScreen : UiScreen
    {
        private static readonly (string art, string title, string body)[] Pages =
        {
            ("intro_1", "Nine seni çağırıyor", "Köydeki eski ev yıllardır bekliyor. Nine'nin dokuma tezgâhı tozlanmış, avlunun çeşmesi kurumuş."),
            ("intro_2", "İlmek ilmek dokuyacaksın", "Renkli parçaları tezgâha yerleştir. Tezgâhı tamamen doldurunca kilim biter."),
            ("intro_3", "Desen yıldız getirir", "Parçanın rengi desendeki izle eşleşirse altın çerçeve parlar. Tam desen = 3 yıldız."),
            ("intro_4", "Evi birlikte onaralım", "Her yıldız bir ilmek kazandırır. İlmeklerle evi, bağ evini, yaylayı… yeniden canlandır."),
        };

        private readonly Action _done;
        private int _page;
        private RectTransform _pageRoot;
        private RectTransform _dots;
        private Text _nextLabel;

        public IntroScreen(Action done) { _done = done; }

        public override void Build(RectTransform root)
        {
            var v = Ui.VStack(root, 24, 56).Stretch();
            var top = Ui.HStack(v, 0, 0, TextAnchor.MiddleRight, 90);
            top.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            Ui.Button(top, "Geç", Finish, Theme.Neutral, Theme.Ink, 84, 32, 180);
            _pageRoot = Ui.Rect(v, "Page");
            Ui.Size(_pageRoot.gameObject, -1, -1, -1, 1);
            _dots = Ui.HStack(v, 18, 0, TextAnchor.MiddleCenter, 40);
            _dots.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            var next = Ui.Button(v, "İleri", Next, Theme.Primary, Color.white, 150, 52);
            _nextLabel = Ui.ButtonLabel(next);
            Render(0);
        }

        private void Render(int dir)
        {
            Ui.Clear(_pageRoot);
            var (art, title, body) = Pages[_page];
            var page = Ui.VStack(_pageRoot, 28, 0, TextAnchor.MiddleCenter).Stretch();
            var pic = Ui.Box(page, "Art", Theme.Sand);
            Ui.Size(pic.gameObject, -1, 860);
            Ui.Soft(pic, 10);
            var img = Ui.Icon(pic.transform, Art.Get(art), 0);
            img.preserveAspect = true;
            img.rectTransform.Stretch(16, 16, 16, 16);
            if (img.sprite == null) { img.sprite = Art.Nine; }
            Ui.Label(page, title, 64, Theme.Primary, TextAnchor.MiddleCenter, true, 90);
            Ui.Label(page, body, 40, Theme.Ink, TextAnchor.MiddleCenter, false, 200);
            if (dir != 0)
            {
                var rt = (RectTransform)page;
                Fx.To(rt, 0.35f, Ease.OutCubic, k => { if (rt != null) { float x = (1 - k) * 260 * dir; rt.offsetMin = new Vector2(x, 0); rt.offsetMax = new Vector2(x, 0); } });
            }
            var g = page.gameObject.AddComponent<CanvasGroup>();
            Fx.Fade(g, 0, 1, 0.3f);
            Fx.PopIn(pic.transform, 0.45f);

            Ui.Clear(_dots);
            for (int i = 0; i < Pages.Length; i++)
            {
                var d = Ui.Box(_dots, "Dot", i == _page ? Theme.Primary : Theme.Neutral, i == _page ? 56 : 24, 24);
                Ui.Round(d, 1f, Art.Pill);
            }
            _nextLabel.text = Loc.T(_page == Pages.Length - 1 ? "Başla" : "İleri");
        }

        private void Next()
        {
            if (_page >= Pages.Length - 1) { Finish(); return; }
            _page++;
            Render(1);
        }

        private void Finish()
        {
            App.Profile.IntroSeen = true;
            App.Save();
            _done?.Invoke();
        }

        public override bool OnBack() { if (_page > 0) { _page--; Render(-1); } return true; }
    }

    /// <summary>
    /// 8.2 Yaş: doğum yılı iki adımda seçilir (önce on yıl, sonra yıl). Varsayılan seçim yok; seçimsiz "Devam" pasif.
    /// Uzun liste kaydırma yok, her şey tek ekranda.
    /// </summary>
    public sealed class AgeGateScreen : UiScreen
    {
        private int _selected;
        private int _decade;
        private Button _continue;
        private RectTransform _years;
        private Text _display;

        public override void Build(RectTransform root)
        {
            var v = Ui.VStack(root, 26, 56).Stretch();
            Ui.Spacer(v, 30);
            var hi = Ui.Label(v, "Hoş geldin!", 80, Theme.Primary, TextAnchor.MiddleCenter, true, 110);
            Fx.PopIn(hi.transform, 0.5f);
            Ui.Label(v, "Deneyimini yaşına göre ayarlamak için doğum yılını seç. Bu bilgi yalnızca cihazında saklanır.", 36, Theme.Muted, TextAnchor.MiddleCenter, false, 120);

            var shown = Ui.Card(v, 0, 18, Theme.Cream);
            _display = Ui.Label(shown, "— — — —", 96, Theme.Ink, TextAnchor.MiddleCenter, true, 130);

            Ui.Label(v, "On yıl", 34, Theme.Ink, TextAnchor.MiddleLeft, true, 46);
            int thisYear = App.Clock.UtcNow.Year;
            int firstDecade = AgePolicy.MinYear / 10 * 10;
            RectTransform line = null;
            int n = 0;
            for (int d = thisYear / 10 * 10; d >= firstDecade; d -= 10, n++)
            {
                if (n % 4 == 0) line = Ui.HStack(v, 12, 0, TextAnchor.MiddleCenter, 100);
                int dec = d;
                Ui.Button(line, $"{dec}'ler", () => PickDecade(dec), Theme.Panel, Theme.Ink, 100, 36);
            }
            while (n % 4 != 0) { Ui.Spacer(line, 100); n++; }

            Ui.Label(v, "Yıl", 34, Theme.Ink, TextAnchor.MiddleLeft, true, 46);
            _years = Ui.VStack(v, 12, 0);
            Ui.Size(_years.gameObject, -1, 224);
            Ui.Spacer(v);
            _continue = Ui.Button(v, "Devam", Submit);
            Ui.SetEnabled(_continue, false);
        }

        private void PickDecade(int dec)
        {
            _decade = dec;
            Ui.Clear(_years);
            int thisYear = App.Clock.UtcNow.Year;
            RectTransform line = null;
            for (int i = 0; i < 10; i++)
            {
                if (i % 5 == 0) line = Ui.HStack(_years, 12, 0, TextAnchor.MiddleCenter, 100);
                int y = dec + i;
                bool valid = y >= AgePolicy.MinYear && y <= thisYear;
                var b = Ui.Button(line, y.ToString(), () => Pick(y), y == _selected ? Theme.Gold : Theme.Panel, Theme.Ink, 100, 36);
                if (!valid) Ui.SetEnabled(b, false);
                Fx.PopIn(b.transform, 0.3f, i * 0.025f);
            }
        }

        private void Pick(int year)
        {
            _selected = year;
            _display.text = year.ToString();
            Fx.Pop(_display.transform, 1.15f);
            PickDecade(_decade);
            Ui.SetEnabled(_continue, true);
        }

        private void Submit()
        {
            if (_selected == 0) return;
            var p = App.Profile;
            p.BirthYear = _selected;
            p.Age = AgePolicy.BandFor(_selected, App.Clock.LocalDate());
            App.Analytics.Track("age_gate", ("band", AgePolicy.AnalyticsBand(p.Age)));
            App.Save();
            SplashScreen.Route();
        }
    }

    /// <summary>13 yaş altı engel (UAT AGE-02). Değişiklik yalnızca Destek ile.</summary>
    public sealed class BlockedScreen : UiScreen
    {
        public override void Build(RectTransform root)
        {
            var v = Ui.VStack(root, 32, 80, TextAnchor.MiddleCenter).Stretch();
            Ui.Label(v, "Bu oyun 13 yaş ve üzeri içindir", 60, Theme.Ink, TextAnchor.MiddleCenter, true, 200);
            Ui.Label(v, "Çocuklar için hazırladığımız Minik Yollar, Pofuduk ve Minik Kasaba oyunlarına göz atabilirsin.", 40, Theme.Muted, TextAnchor.MiddleCenter, false, 200);
            Ui.Button(v, "Minik oyunlarımız", () => Application.OpenURL("https://example.com/minik"), Theme.Neutral, Theme.Ink);
            Ui.Label(v, "Yanlış seçim yaptıysan: Destek", 32, Theme.Muted);
        }
    }

    /// <summary>8.3 Rıza: KVKK aydınlatma + analitik anahtarı (varsayılan kapalı). Ardından UMP (AB/UK) ve ATT ön ekranı (iOS yetişkin).</summary>
    public sealed class ConsentScreen : UiScreen
    {
        private bool _analytics;

        public override void Build(RectTransform root)
        {
            var v = Ui.VStack(root, 28, 64).Stretch();
            Ui.Spacer(v, 40);
            var shield = Ui.Icon(v, Art.Icon("shield"), 150, Theme.Primary);
            Fx.PopIn(shield.transform, 0.45f);
            Ui.Label(v, "Gizlilik ve rıza", 64, Theme.Ink, TextAnchor.MiddleCenter, true);
            var card = Ui.Card(v);
            Ui.Label(card, "Zorunlu", 40, Theme.Ink, TextAnchor.MiddleLeft, true);
            Ui.Label(card, "Oyun ilerlemen ve satın alımların cihazında ve bulut kaydında saklanır.", 36, Theme.Muted, TextAnchor.MiddleLeft, false, 110);
            Ui.Label(card, "İsteğe bağlı", 40, Theme.Ink, TextAnchor.MiddleLeft, true);
            Ui.Label(card, "Oyunu geliştirmek için anonim kullanım verisi (analitik).", 36, Theme.Muted, TextAnchor.MiddleLeft, false, 110);
            Toggle(card, "Analitiğe izin ver", false, on => _analytics = on);
            Ui.Button(v, "KVKK aydınlatma metnini oku", () => KvkkText(App), Theme.Neutral, Theme.Ink);
            Ui.Spacer(v);
            Ui.Label(v, "Hiçbir seçim oyunu kısıtlamaz.", 34, Theme.Muted);
            Ui.Button(v, "Devam", Submit);
        }

        public static void KvkkText(App app)
        {
            app.Nav.Confirm("Aydınlatma metni",
                "Veri sorumlusu: [Şirket]. İşlenen veriler: oyun ilerlemesi, satın alma kayıtları, (rıza verirsen) anonim kullanım verisi. Amaç: oyunu sunmak ve geliştirmek. Haklarını Ayarlar › Verilerimi sil ve destek e-postası üzerinden kullanabilirsin.",
                "Kapat", null, null, null);
        }

        /// <summary>Açık/kapalı anahtar: solda etiket, sağda kayan düğme (iOS/Android tarzı).</summary>
        public static Button Toggle(Transform parent, string label, bool initial, Action<bool> changed)
        {
            bool state = initial;
            Button b = null;
            Image track = null;
            RectTransform krt = null;
            b = Ui.Button(parent, "", () =>
            {
                state = !state;
                Paint(true);
                changed(state);
            }, Theme.Panel, Theme.Ink, 110, 36);
            var text = Ui.ButtonLabel(b);
            text.alignment = TextAnchor.MiddleLeft;
            ((RectTransform)text.transform).Stretch(32, 0, 170, 6);
            text.text = Loc.T(label);
            track = Ui.Box(b.transform, "Track", Theme.Neutral);
            Ui.Round(track, 1f, Art.Pill);
            track.raycastTarget = false;
            var trt = track.rectTransform;
            trt.anchorMin = trt.anchorMax = new Vector2(1, 0.5f);
            trt.pivot = new Vector2(1, 0.5f);
            trt.sizeDelta = new Vector2(120, 64);
            trt.anchoredPosition = new Vector2(-28, 3);
            var knob = Ui.Box(track.transform, "Knob", Color.white);
            Ui.Round(knob, 1f, Art.Pill);
            Ui.Soft(knob, 3);
            knob.raycastTarget = false;
            krt = knob.rectTransform;
            krt.anchorMin = krt.anchorMax = new Vector2(0, 0.5f);
            krt.pivot = new Vector2(0.5f, 0.5f);
            krt.sizeDelta = new Vector2(52, 52);
            void Paint(bool anim)
            {
                track.color = state ? Theme.Ok : Theme.Neutral;
                float x = state ? 88 : 32;
                if (anim) { float from = krt.anchoredPosition.x; Fx.To(krt, 0.2f, Ease.OutBack, k => krt.anchoredPosition = new Vector2(Mathf.LerpUnclamped(from, x, k), 0)); }
                else krt.anchoredPosition = new Vector2(x, 0);
            }
            Paint(false);
            return b;
        }

        private void Submit()
        {
            var app = App;
            var p = app.Profile;
            p.AnalyticsConsent = _analytics;
            app.Analytics.Track("consent", ("kvkk_analytics", _analytics));

            void Finish(bool umpPersonalized)
            {
                p.ConsentDone = true;
                app.Save();
                SplashScreen.Route();
            }

            void AfterUmp(bool personalized)
            {
                p.UmpDone = personalized;
                if (p.Age == AgeBand.Adult && app.Consent.AttSupported && !p.AttAsked)
                {
                    // ATT ön ekranı: kendi açıklamamız, sonra OS izni (Spec 2 §5). Gençte ATT istenmez.
                    app.Nav.Confirm("Reklam deneyimin", "Sana daha alakalı reklamlar gösterebilmemiz için bir sonraki adımda izin istenecek. Reddetmek hiçbir özelliği kapatmaz.",
                        "Devam", () => app.Consent.RequestAtt(_ => { p.AttAsked = true; Finish(personalized); }), null, null);
                }
                else Finish(personalized);
            }

            if (p.Age == AgeBand.Adult && app.Consent.UmpRequired) app.Consent.ShowUmpForm(AfterUmp);
            else AfterUmp(p.Age == AgeBand.Adult);
        }
    }
}
