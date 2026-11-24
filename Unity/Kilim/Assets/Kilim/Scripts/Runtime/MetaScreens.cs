using System;
using System.Linq;
using Kilim.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Kilim.Game
{
    /// <summary>1 · Ana ekran (Spec 3 §1).</summary>
    public sealed class HomeScreen : UiScreen
    {
        private Text _starterTimer, _raceTimer;

        public override void Build(RectTransform root)
        {
            var app = App;
            var p = app.Profile;
            var m = app.Meta;
            ClaimRaceIfEnded();

            var v = Ui.VStack(root, 24, 32).Stretch();

            // Üst: coin, ilmek, ayarlar
            var top = Ui.HStack(v, 16, 0, TextAnchor.MiddleCenter, 100);
            Ui.CoinPill(top, app);
            Ui.Label(top, $"✦ {m.Wallet.Ilmek} ilmek", 40, Theme.Ink, TextAnchor.MiddleCenter, true);
            Ui.Button(top, "Ayarlar", () => app.Nav.Show(new SettingsScreen()), Theme.Neutral, Theme.Ink, 100, 36);

            // Sol/sağ kısayollar
            var row = Ui.HStack(v, 16, 0, TextAnchor.MiddleCenter, 170);
            var piggyFull = m.Wallet.Piggy >= app.Config.Economy.PiggyCap;
            Ui.Button(row, $"Kumbara\n{Fmt.N(m.Wallet.Piggy)}{(piggyFull ? " · dolu!" : "")}", () => app.Nav.Show(new PiggyScreen()),
                piggyFull ? Theme.Gold : Theme.Panel, Theme.Ink, 170, 34);
            Ui.Button(row, $"Sezon\nSeviye {m.Pass.Level(p)}", () => app.Nav.Show(new PassScreen()), Theme.Panel, Theme.Ink, 170, 34);
            Ui.Button(row, $"Günlük{(m.Daily.StampedToday(p) ? " ✓" : " •")}\nSeri {m.Daily.CurrentStreak(p)}", () => app.Nav.Show(new DailyScreen()), Theme.Panel, Theme.Ink, 170, 34);
            var race = Ui.Button(row, "Yarış", () => app.Nav.Show(new RaceScreen()), Theme.Panel, Theme.Ink, 170, 34);
            _raceTimer = Ui.ButtonLabel(race);

            // Başlangıç paketi
            if (m.Starter.IsVisible(p))
            {
                var offer = Ui.Card(v, 8, 24, Theme.WithAlpha(Theme.Gold, 0.25f));
                Ui.Label(offer, "Başlangıç paketi · bir kez", 38, Theme.Ink, TextAnchor.MiddleCenter, true);
                _starterTimer = Ui.Label(offer, "", 34, Theme.Muted);
                Ui.Button(offer, $"2.000 coin + 3'er güçlendirici + 24 sa reklamsız · {app.Iap.LocalizedPrice(Skus.Starter)}",
                    () => app.Buy(Skus.Starter, "home", () => app.GoHome()), Theme.Primary, Color.white, 110, 32);
            }

            Ui.Spacer(v);
            var next = Home.NextTask(p);
            Ui.Label(v, next != null ? $"Nine: \"{next.NineNote}\"" : "Nine: \"Ev pırıl pırıl oldu, ellerine sağlık.\"", 38, Theme.Muted, TextAnchor.MiddleCenter, false, 110);

            // Ana buton
            int lvl = p.CurrentLevel;
            if (app.Levels.Get(lvl) != null)
            {
                Ui.Button(v, $"Bölüm {lvl}", () => app.StartLevel(lvl), Theme.Primary, Color.white, 220, 80);
            }
            else
            {
                // İçerik sonu (UAT LVL-05)
                var card = Ui.Card(v);
                Ui.Label(card, "Yeni desenler yakında", 52, Theme.Ink, TextAnchor.MiddleCenter, true);
                Ui.Label(card, "Bu arada Günün Deseni ve Tezgâh Yarışı açık.", 36, Theme.Muted);
                int replay = FirstNotPerfect();
                if (replay > 0) Ui.Button(card, $"Bölüm {replay}'i 3★ yap", () => app.StartLevel(replay));
            }
            Ui.Spacer(v);

            var bottom = Ui.HStack(v, 16, 0, TextAnchor.MiddleCenter, 150);
            bool canTask = next != null && m.Wallet.Ilmek >= next.Cost;
            Ui.Button(bottom, canTask ? "Ev •" : "Ev", () => app.Nav.Show(new DecorScreen()), canTask ? Theme.Gold : Theme.Panel, Theme.Ink, 150, 44);
            Ui.Button(bottom, "Mağaza", () => app.Nav.Show(new ShopScreen()), Theme.Panel, Theme.Ink, 150, 44);
            Tick();
        }

        private int FirstNotPerfect()
        {
            for (int i = 1; i <= App.Levels.Count; i++) if (App.Meta.Wallet.BestStars(i) < 3) return i;
            return 0;
        }

        private void ClaimRaceIfEnded()
        {
            var m = App.Meta;
            if (m.Race.IsActive || App.Profile.RaceWeek != m.Race.WeekId || App.Profile.RaceRewardClaimedWeek == m.Race.WeekId) return;
            var (rank, coins) = m.Race.ClaimIfEnded(App.Profile, m.Race.BuildGroup(App.Profile));
            App.Save();
            App.Analytics.Track("race_result", ("week", m.Race.WeekId), ("rank", rank), ("flags", App.Profile.RaceFlags));
            if (coins > 0) App.Nav.Confirm("Tezgâh Yarışı bitti", $"{rank}. oldun! +{Fmt.N(coins)} coin", "Harika", null, null, null);
        }

        public override void Tick()
        {
            var m = App.Meta;
            if (_starterTimer != null) _starterTimer.text = $"Kalan {Fmt.Span(m.Starter.Remaining(App.Profile))}";
            if (_raceTimer != null) _raceTimer.text = m.Race.IsActive ? $"Yarış\n{Fmt.Span(m.Race.Remaining)}" : "Yarış\nCuma";
        }
    }

    /// <summary>6 · Mağaza: başlangıç paketi → reklamsız → coin paketleri → kumbara; geri yükle altta.</summary>
    public sealed class ShopScreen : UiScreen
    {
        public override void Build(RectTransform root)
        {
            var app = App;
            var v = Ui.VStack(root, 0, 0).Stretch();
            Ui.Header(v, "Mağaza", app.GoHome, app);
            var list = Ui.Scroll(v);
            if (app.Meta.Starter.IsVisible(app.Profile))
                Item(list, "Başlangıç paketi · bir kez", "2.000 coin + 3'er güçlendirici + 24 sa reklamsız", Skus.Starter);
            if (!app.Profile.NoAds)
                Item(list, "Reklamsız", "Tam ekran reklamlar kapanır; ödüllü reklamlar isteğe bağlı kalır.", Skus.NoAds);
            foreach (var p in Skus.CoinPacks)
                Item(list, $"{Fmt.N(p.coins)} coin", p.badge, p.sku);
            Ui.Button(list, "Kumbara", () => app.Nav.Show(new PiggyScreen()), Theme.Neutral, Theme.Ink);
            Ui.Button(list, "Satın alımları geri yükle", app.RestorePurchases, Theme.Neutral, Theme.Ink);
            Ui.Label(list, "Kullanım koşulları · Gizlilik", 30, Theme.Muted);
            Ui.Label(list, "Ganimet kutusu ve rastgele ödüllü satın alma yoktur.", 28, Theme.Muted);
        }

        private void Item(Transform parent, string title, string sub, string sku)
        {
            var card = Ui.Card(parent, 10, 28);
            Ui.Label(card, title, 44, Theme.Ink, TextAnchor.MiddleLeft, true);
            if (!string.IsNullOrEmpty(sub)) Ui.Label(card, sub, 32, Theme.Muted, TextAnchor.MiddleLeft, false, 90);
            Ui.Button(card, App.Iap.LocalizedPrice(sku), () => App.Buy(sku, "shop", () => App.Nav.Show(new ShopScreen())), Theme.Primary, Color.white, 110, 40);
        }

        public override bool OnBack() { App.GoHome(); return true; }
    }

    /// <summary>11.1 Kumbara: "Kır 2,99 $" yalnızca doluyken; değilse "X coin daha".</summary>
    public sealed class PiggyScreen : UiScreen
    {
        public override void Build(RectTransform root)
        {
            var app = App;
            var w = app.Meta.Wallet;
            int cap = app.Config.Economy.PiggyCap;
            var v = Ui.VStack(root, 28, 0).Stretch();
            Ui.Header(v, "Kumbara", app.GoHome, app);
            var c = Ui.Card(v, 24, 48);
            Ui.Label(c, $"{Fmt.N(w.Piggy)} / {Fmt.N(cap)}", 64, Theme.Ink, TextAnchor.MiddleCenter, true);
            Ui.Bar(c, 40, Theme.Gold, Theme.Neutral)((float)w.Piggy / cap);
            Ui.Label(c, $"Kazandığın her bölüm kumbaraya {app.Config.Economy.PiggyPerWin} coin atar. Dolunca {app.Iap.LocalizedPrice(Skus.Piggy)} ile kırıp hepsini alabilirsin.", 34, Theme.Muted, TextAnchor.MiddleCenter, false, 120);
            if (w.Piggy >= cap)
                Ui.Button(c, $"Kır · {app.Iap.LocalizedPrice(Skus.Piggy)}", () => app.Buy(Skus.Piggy, "piggy", () => app.Nav.Show(new PiggyScreen())));
            else
            {
                var b = Ui.Button(c, $"{Fmt.N(cap - w.Piggy)} coin daha", null, Theme.Neutral, Theme.Ink);
                Ui.SetEnabled(b, false);
            }
            App.Analytics.Track("piggy_state", ("coins", w.Piggy));
        }

        public override bool OnBack() { App.GoHome(); return true; }
    }

    /// <summary>11.2 Sezon kartı: 30 seviye, ücretsiz/altın satırlar, kalan gün; altın yol geçmiş ödülleri de verir.</summary>
    public sealed class PassScreen : UiScreen
    {
        public override void Build(RectTransform root)
        {
            var app = App;
            var p = app.Profile;
            var pass = app.Meta.Pass;
            pass.Roll(p);
            var v = Ui.VStack(root, 0, 0).Stretch();
            Ui.Header(v, $"Sezon {pass.SeasonNumber} · Fas", app.GoHome, app);
            var info = Ui.Card(v, 12, 28);
            Ui.Label(info, $"Seviye {pass.Level(p)} / {SeasonPass.Levels} · Makara {pass.SpoolsInLevel(p)}/{SeasonPass.SpoolsPerLevel} · {Fmt.Span(pass.Remaining)} kaldı", 34, Theme.Ink);
            Ui.Bar(info, 24, Theme.Gold, Theme.Neutral)((float)pass.SpoolsInLevel(p) / SeasonPass.SpoolsPerLevel);
            Ui.Label(info, "Her kazanılan bölüm 1 makara, 10 makara 1 seviye. Sezon bitince alınmamış ödüller kaybolur.", 28, Theme.Muted, TextAnchor.MiddleCenter, false, 80);
            if (!p.GoldPass)
                Ui.Button(info, $"Altın yolu aç · {app.Iap.LocalizedPrice(Skus.GoldPass)}", () => app.Buy(Skus.GoldPass, "pass", () => app.Nav.Show(new PassScreen())), Theme.Gold, Theme.Ink, 110, 38);

            var list = Ui.Scroll(v, 12, 24);
            for (int lvl = 1; lvl <= SeasonPass.Levels; lvl++)
            {
                int L = lvl;
                var row = Ui.HStack(list, 12, 0, TextAnchor.MiddleCenter, 120);
                var n = Ui.Label(row, L.ToString(), 40, L <= pass.Level(p) ? Theme.Ink : Theme.Muted, TextAnchor.MiddleCenter, true);
                Ui.Size(n.gameObject, 80, 120, 0);
                RewardCell(row, L, false);
                RewardCell(row, L, true);
            }
            app.Analytics.Track("pass_level", ("season", pass.SeasonKey), ("level", pass.Level(p)), ("gold", p.GoldPass));
        }

        private void RewardCell(Transform row, int lvl, bool gold)
        {
            var app = App;
            var p = app.Profile;
            var pass = app.Meta.Pass;
            var r = gold ? SeasonPass.GoldReward(lvl) : SeasonPass.FreeReward(lvl);
            bool claimed = (gold ? p.PassClaimedGold : p.PassClaimedFree).Contains(lvl);
            bool can = pass.CanClaim(p, lvl, gold);
            string prefix = gold ? "Altın: " : "";
            var b = Ui.Button(row, claimed ? $"{prefix}{r.Label} ✓" : $"{prefix}{r.Label}", () =>
            {
                if (pass.Claim(p, lvl, gold) != null) { app.Save(); app.Nav.Show(new PassScreen()); }
            }, gold ? Theme.WithAlpha(Theme.Gold, 0.5f) : Theme.Panel, Theme.Ink, 110, 30);
            Ui.SetEnabled(b, can);
        }

        public override bool OnBack() { App.GoHome(); return true; }
    }

    /// <summary>12.1 Günün Deseni: ay takvimi, damgalı günler, seri; günlük sandık (ödüllü reklamla ×2).</summary>
    public sealed class DailyScreen : UiScreen
    {
        public override void Build(RectTransform root)
        {
            var app = App;
            var p = app.Profile;
            var d = app.Meta.Daily;
            var v = Ui.VStack(root, 20, 0).Stretch();
            Ui.Header(v, "Günün Deseni", app.GoHome, app);
            var body = Ui.VStack(v, 20, 32);
            Ui.Label(body, "Herkese aynı desen. Süre yok, kaybetmek yok: tezgâh dolarsa son parça kendiliğinden geri alınır.", 34, Theme.Muted, TextAnchor.MiddleCenter, false, 110);
            var today = app.Clock.LocalDate();
            Ui.Label(body, $"{today:MMMM yyyy} · Seri: {d.CurrentStreak(p)} gün", 40, Theme.Ink, TextAnchor.MiddleCenter, true);

            var first = new DateTime(today.Year, today.Month, 1);
            int days = DateTime.DaysInMonth(today.Year, today.Month);
            int lead = ((int)first.DayOfWeek + 6) % 7; // Pazartesi başlangıç
            var grid = Ui.VStack(body, 8);
            RectTransform week = null;
            for (int i = 0; i < lead + days; i++)
            {
                if (i % 7 == 0) week = Ui.HStack(grid, 8, 0, TextAnchor.MiddleCenter, 100);
                if (i < lead) { Ui.Spacer(week, 100); continue; }
                int day = i - lead + 1;
                var date = new DateTime(today.Year, today.Month, day).ToString("yyyy-MM-dd");
                bool stamped = p.DailyStamps.Contains(date);
                var cell = Ui.Box(week, "Day", stamped ? Theme.Gold : day == today.Day ? Theme.Neutral : Theme.Panel, -1, 100);
                var t = Ui.Label(cell.transform, stamped ? $"{day}\n✦" : day.ToString(), 30, Theme.Ink);
                ((RectTransform)t.transform).Stretch();
            }
            for (int i = (lead + days) % 7; i != 0 && i < 7; i++) Ui.Spacer(week, 100);

            Ui.Label(body, "Ödül: 3★'da 100 coin + damga · 7 gün seri = rozet", 32, Theme.Muted);
            Ui.Button(body, d.StampedToday(p) ? "Bugünün desenini yeniden doku" : "Bugünün desenini doku", app.StartDaily);

            if (d.ChestAvailable(p))
            {
                var chest = Ui.HStack(body, 16, 0, TextAnchor.MiddleCenter, 120);
                Ui.Button(chest, "Günlük sandık: 50", () => { d.OpenChest(p, false); app.Save(); app.Nav.Show(new DailyScreen()); }, Theme.Panel, Theme.Ink, 120, 36);
                Ui.Button(chest, "Reklam izle · ×2", () => app.ShowRewarded("daily_chest", () =>
                {
                    d.OpenChest(p, true);
                    app.Meta.Ads.OnRewardedEarned(p);
                    app.Save();
                    app.Nav.Show(new DailyScreen());
                }), Theme.Gold, Theme.Ink, 120, 36);
            }
            else Ui.Label(body, "Sandık yarın yine burada.", 32, Theme.Muted);
        }

        public override bool OnBack() { App.GoHome(); return true; }
    }

    /// <summary>12.2 Tezgâh Yarışı: 30 kişilik sıralama, kendi satırın vurgulu, kalan süre; takma ad listeden.</summary>
    public sealed class RaceScreen : UiScreen
    {
        private Text _timer;

        public override void Build(RectTransform root)
        {
            var app = App;
            var p = app.Profile;
            var race = app.Meta.Race;
            var v = Ui.VStack(root, 16, 0).Stretch();
            Ui.Header(v, "Tezgâh Yarışı", app.GoHome, app);
            var info = Ui.Card(v, 10, 28);
            Ui.Label(info, "İlk denemede kazandığın her bölüm 1 bayrak. 30 kişilik grubunda ilk 3'e ödül.", 32, Theme.Muted, TextAnchor.MiddleCenter, false, 90);
            Ui.Label(info, "1. 1.500 coin + çerçeve · 2. 800 · 3. 400", 34, Theme.Ink, TextAnchor.MiddleCenter, true);
            _timer = Ui.Label(info, "", 34, Theme.Ink);

            if (!race.IsActive)
            {
                Ui.Label(v, "Yarış Cuma 10:00'da başlar, Pazar 22:00'de biter.", 38, Theme.Muted, TextAnchor.MiddleCenter, false, 120);
                return;
            }
            if (string.IsNullOrEmpty(p.RaceNickname))
            {
                Ui.Label(v, "Takma adını seç", 44, Theme.Ink, TextAnchor.MiddleCenter, true);
                var list = Ui.Scroll(v, 10, 24);
                foreach (var n in Race.Nicknames)
                {
                    var name = n;
                    Ui.Button(list, name, () => { p.RaceNickname = name; app.Save(); app.Nav.Show(new RaceScreen()); }, Theme.Panel, Theme.Ink, 100, 38);
                }
                return;
            }
            var group = race.BuildGroup(p);
            var rows = Ui.Scroll(v, 8, 24);
            for (int i = 0; i < group.Count; i++)
            {
                var e = group[i];
                var row = Ui.HStack(rows, 12, 16, TextAnchor.MiddleLeft, 90);
                row.gameObject.AddComponent<Image>().color = e.IsPlayer ? Theme.WithAlpha(Theme.Gold, 0.45f) : Theme.Panel;
                Ui.Label(row, $"{i + 1}", 36, Theme.Ink, TextAnchor.MiddleLeft, true);
                Ui.Label(row, e.IsPlayer ? $"{e.Name} (sen)" : e.Name, 36, Theme.Ink, TextAnchor.MiddleLeft, e.IsPlayer);
                Ui.Label(row, $"⚑ {e.Flags}", 36, Theme.Ink, TextAnchor.MiddleRight, true);
            }
            Ui.Label(v, "Grup eşleşmesi sunucuda; çevrimdışı oynanan bölümler bağlantı gelince sayılır.", 26, Theme.Muted);
            Tick();
        }

        public override void Tick()
        {
            if (_timer != null) _timer.text = App.Meta.Race.IsActive ? $"Kalan {Fmt.Span(App.Meta.Race.Remaining)}" : "";
        }

        public override bool OnBack() { App.GoHome(); return true; }
    }

    /// <summary>5 · Ev: alan listesi + görev kartları; ilmek yeterse "Yap", stil seçimi varsa 3 seçenek.</summary>
    public sealed class DecorScreen : UiScreen
    {
        public override void Build(RectTransform root)
        {
            var app = App;
            var p = app.Profile;
            var v = Ui.VStack(root, 0, 0).Stretch();
            Ui.Header(v, "Nine'nin Evi", app.GoHome);
            Ui.Label(v, $"✦ {p.Wallet.Ilmek} ilmek", 40, Theme.Ink, TextAnchor.MiddleCenter, true, 70);
            var list = Ui.Scroll(v, 16, 24);
            var next = Home.NextTask(p);
            foreach (var area in Home.Areas)
            {
                Ui.Label(list, $"{area} · {Home.DoneInArea(p, area)}/4", 44, Theme.Ink, TextAnchor.MiddleLeft, true);
                foreach (var t in Home.Region1.Where(x => x.Area == area))
                {
                    var task = t;
                    var card = Ui.Card(list, 8, 24);
                    bool done = p.HomeTasksDone.Contains(t.Id);
                    Ui.Label(card, t.Title, 38, done ? Theme.Muted : Theme.Ink, TextAnchor.MiddleLeft, true);
                    if (done)
                    {
                        if (t.Styles != null)
                        {
                            var styles = Ui.HStack(card, 8, 0, TextAnchor.MiddleCenter, 90);
                            foreach (var s in t.Styles)
                            {
                                var style = s;
                                bool sel = p.HomeStyles.TryGetValue(t.Id, out var cur) && cur == s;
                                Ui.Button(styles, s, () => { Home.ChangeStyle(p, task, style); app.Save(); app.Nav.Show(new DecorScreen()); },
                                    sel ? Theme.Gold : Theme.Neutral, Theme.Ink, 90, 32);
                            }
                        }
                        else Ui.Label(card, "Tamam ✓", 32, Theme.Ok, TextAnchor.MiddleLeft);
                    }
                    else if (task == next)
                    {
                        if (Home.CanDo(p, task))
                            Ui.Button(card, $"{task.Cost} ilmek · Yap", () => DoTask(task), Theme.Primary, Color.white, 100, 38);
                        else
                        {
                            var row = Ui.HStack(card, 12, 0, TextAnchor.MiddleCenter, 100);
                            Ui.Label(row, $"{task.Cost - p.Wallet.Ilmek} ilmek daha", 34, Theme.Muted);
                            Ui.Button(row, "Oyna", () => app.StartLevel(p.CurrentLevel), Theme.Primary, Color.white, 100, 38);
                        }
                    }
                    else Ui.Label(card, $"{task.Cost} ilmek · sırada bekliyor", 30, Theme.Muted, TextAnchor.MiddleLeft);
                }
            }
        }

        private void DoTask(HomeTask t)
        {
            var app = App;
            void Finish(string style)
            {
                if (!Home.Do(app.Profile, t, style)) return;
                app.Analytics.Track("task_done", ("region", 1), ("area", t.Area), ("task", t.Id), ("style", style ?? ""));
                app.Save();
                app.Nav.Show(new DecorScreen());
                app.Nav.Confirm(t.Title, $"Nine: \"{t.NineNote}\"", "Güzel olmuş", null, null, null);
            }
            if (t.Styles == null) { Finish(null); return; }
            app.Nav.Choice("Stil seç", "Sonra ücretsiz değiştirebilirsin.", t.Styles.Select(s => (s, (Action)(() => Finish(s)))).ToArray());
        }

        public override bool OnBack() { App.GoHome(); return true; }
    }

    /// <summary>7 · Ayarlar.</summary>
    public sealed class SettingsScreen : UiScreen
    {
        public override void Build(RectTransform root)
        {
            var app = App;
            var p = app.Profile;
            var v = Ui.VStack(root, 0, 0).Stretch();
            Ui.Header(v, "Ayarlar", app.GoHome);
            var list = Ui.Scroll(v, 14, 32);
            ConsentScreen.Toggle(list, "Müzik", p.Music, on => { p.Music = on; app.Save(); });
            ConsentScreen.Toggle(list, "Efektler", p.Sfx, on => { p.Sfx = on; app.ApplySettings(); app.Save(); });
            ConsentScreen.Toggle(list, "Titreşim", p.Haptics, on => { p.Haptics = on; app.ApplySettings(); app.Save(); });
            ConsentScreen.Toggle(list, "Sol el modu", p.LeftHanded, on => { p.LeftHanded = on; app.Save(); });
            ConsentScreen.Toggle(list, "Günlük desen bildirimi", p.DailyNotification, on => { p.DailyNotification = on; app.Save(); });
            Ui.Button(list, "Dil: Türkçe", () => app.Nav.Toast("Diğer diller yakında."), Theme.Neutral, Theme.Ink, 110, 40);
            Ui.Button(list, "Bulut kayıt (Game Center / Play Games)", () => app.Nav.Toast("Bulut kayıt bu sürümde test modunda."), Theme.Neutral, Theme.Ink, 110, 36);
            Ui.Button(list, "Satın alımları geri yükle", app.RestorePurchases, Theme.Neutral, Theme.Ink, 110, 40);
            Ui.Button(list, "Destek", () => Application.OpenURL("mailto:destek@example.com?subject=Kilim%20" + p.PlayerId.Substring(0, 8)), Theme.Neutral, Theme.Ink, 110, 40);
            Ui.Label(list, "Gizlilik", 40, Theme.Ink, TextAnchor.MiddleLeft, true);
            ConsentScreen.Toggle(list, "Analitik (KVKK açık rıza)", p.AnalyticsConsent, on =>
            {
                p.AnalyticsConsent = on;
                app.Analytics.Track("consent", ("kvkk_analytics", on));
                app.Save();
            });
            if (p.Age == AgeBand.Adult && app.Consent.UmpRequired)
                Ui.Button(list, "Reklam tercihleri (UMP)", () => app.Consent.ShowUmpForm(on => { p.UmpDone = on; app.InitSdksAfterConsent(on); app.Save(); }), Theme.Neutral, Theme.Ink, 110, 40);
            Ui.Button(list, "Aydınlatma metni", () => ConsentScreen.KvkkText(app), Theme.Neutral, Theme.Ink, 110, 40);
            Ui.Button(list, "Verilerimi sil", () => app.Nav.Confirm("Verilerin silinsin mi?",
                "İlerlemen, coin'lerin ve ayarların bu cihazdan ve sunucudan (30 gün içinde) silinir. Satın alımlar mağazadan geri yüklenebilir.",
                "Sil", app.DeleteAllData, "Vazgeç", null), Theme.WithAlpha(Theme.Primary, 0.2f), Theme.Primary, 110, 40);
            Ui.Label(list, $"Sürüm {Application.version} · Oyuncu ID: {p.PlayerId.Substring(0, 4).ToUpper()}…{p.PlayerId.Substring(p.PlayerId.Length - 2).ToUpper()}", 30, Theme.Muted);
        }

        public override bool OnBack() { App.GoHome(); return true; }
    }
}
