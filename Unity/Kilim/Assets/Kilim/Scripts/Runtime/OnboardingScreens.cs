using System;
using Kilim.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Kilim.Game
{
    /// <summary>8.1 Açılış: logo ≤ 3 sn; kayıt ve config App.Awake'te yüklendi. Yönlendirme buradan.</summary>
    public sealed class SplashScreen : UiScreen
    {
        public override void Build(RectTransform root)
        {
            var v = Ui.VStack(root, 24, 0, TextAnchor.MiddleCenter).Stretch();
            Ui.Label(v, "Kilim", 140, Theme.Primary, TextAnchor.MiddleCenter, true);
            Ui.Label(v, "desen desen, ilmek ilmek", 40, Theme.Muted);
            App.Delay(0.8f, Route);
        }

        public static void Route()
        {
            var app = App.I;
            var p = app.Profile;
            if (p.Age == AgeBand.Unknown) { app.Nav.Show(new AgeGateScreen()); return; }
            if (p.Age == AgeBand.Blocked) { app.Nav.Show(new BlockedScreen()); return; }
            if (!p.ConsentDone) { app.Nav.Show(new ConsentScreen()); return; }
            app.InitSdksAfterConsent(p.UmpDone);
            if (p.CurrentLevel == 1 && p.AttemptsOf(1) == 0) app.StartLevel(1);
            else app.GoHome();
        }
    }

    /// <summary>8.2 Yaş: doğum yılı listesi (1950–bugün), varsayılan seçim yok, seçimsiz "Devam" pasif.</summary>
    public sealed class AgeGateScreen : UiScreen
    {
        private int _selected;
        private Button _continue;
        private Button _selectedBtn;

        public override void Build(RectTransform root)
        {
            var v = Ui.VStack(root, 24, 64).Stretch();
            Ui.Spacer(v, 40);
            Ui.Label(v, "Hoş geldin!", 72, Theme.Ink, TextAnchor.MiddleCenter, true);
            Ui.Label(v, "Deneyimini yaşına göre ayarlamak için doğum yılını seç.\nBu bilgi yalnızca cihazında saklanır.", 38, Theme.Muted, TextAnchor.MiddleCenter, false, 130);

            var list = Ui.Scroll(v, 12, 24);
            int thisYear = App.Clock.UtcNow.Year;
            for (int y = thisYear; y >= AgePolicy.MinYear; y--)
            {
                int year = y;
                Button b = null;
                b = Ui.Button(list, year.ToString(), () => Select(year, b), Theme.Panel, Theme.Ink, 110, 48);
            }

            _continue = Ui.Button(v, "Devam", Submit);
            Ui.SetEnabled(_continue, false);
        }

        private void Select(int year, Button b)
        {
            if (_selectedBtn != null) _selectedBtn.targetGraphic.color = Theme.Panel;
            _selected = year;
            _selectedBtn = b;
            b.targetGraphic.color = Theme.Gold;
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

        /// <summary>Açık/kapalı anahtar (buton olarak).</summary>
        public static Button Toggle(Transform parent, string label, bool initial, Action<bool> changed)
        {
            bool state = initial;
            Button b = null;
            b = Ui.Button(parent, "", () =>
            {
                state = !state;
                Paint();
                changed(state);
            }, Theme.Neutral, Theme.Ink, 110, 40);
            void Paint()
            {
                Ui.ButtonLabel(b).text = $"{label}:  {(state ? "AÇIK" : "KAPALI")}";
                b.targetGraphic.color = state ? Theme.WithAlpha(Theme.Ok, 0.35f) : Theme.Neutral;
            }
            Paint();
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
