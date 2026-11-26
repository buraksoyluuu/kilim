using System;
using System.Collections;
using System.Linq;
using Kilim.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Kilim.Game
{
    /// <summary>
    /// 1 · Ana ekran (Spec 3 §1). Üst: coin, ilmek, ayarlar. Etkinlik kutucukları (Günlük, Yarış, Sezon, Kumbara).
    /// Orta: üzerinde çalışılan evin canlı görseli (onarıldıkça renklenir) ve Nine'nin sözü. Altta nefes alan
    /// "Bölüm N" butonu ve Harita / Ev / Mağaza.
    /// </summary>
    public sealed class HomeScreen : UiScreen
    {
        private Text _starterTimer, _raceTimer, _lives;

        public override void Build(RectTransform root)
        {
            var app = App;
            var p = app.Profile;
            var m = app.Meta;
            ClaimRaceIfEnded();

            var v = Ui.VStack(root, 22, 28).Stretch();
            Ui.Band(v);

            var top = Ui.HStack(v, 16, 0, TextAnchor.MiddleLeft, 96);
            top.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            _lives = LivesModal.Pill(top, app);
            Ui.CoinPill(top, app);
            Ui.IlmekPill(top, app);
            var gap = Ui.Rect(top, "Gap");
            Ui.Size(gap.gameObject, -1, 10, 1);
            IconButton(top, "settings", () => app.Nav.Show(new SettingsScreen()), 96);

            // Etkinlikler
            var row = Ui.HStack(v, 14, 0, TextAnchor.MiddleCenter, 168);
            bool dailyDone = m.Daily.StampedToday(p);
            Tile(row, "daily", "Günlük", dailyDone ? $"Seri {m.Daily.CurrentStreak(p)} ✓" : "Yeni desen!", () => app.Nav.Show(new DailyScreen()), !dailyDone);
            var race = Tile(row, "race", "Yarış", "", () => app.Nav.Show(new RaceScreen()), false);
            _raceTimer = race;
            bool passReady = false;
            for (int l = 1; l <= m.Pass.Level(p) && !passReady; l++) passReady = m.Pass.CanClaim(p, l, false) || (p.GoldPass && m.Pass.CanClaim(p, l, true));
            Tile(row, "pass", "Sezon", $"Seviye {m.Pass.Level(p)}", () => app.Nav.Show(new PassScreen()), passReady);
            bool piggyFull = m.Wallet.Piggy >= app.Config.Economy.PiggyCap;
            Tile(row, "piggy", "Kumbara", piggyFull ? "Dolu!" : Fmt.N(m.Wallet.Piggy), () => app.Nav.Show(new PiggyScreen()), piggyFull);

            if (m.Starter.IsVisible(p))
            {
                var offer = Ui.HStack(v, 16, 20, TextAnchor.MiddleLeft, 130);
                offer.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
                var obg = offer.gameObject.AddComponent<Image>();
                obg.color = Theme.WithAlpha(Theme.Gold, 0.3f);
                Ui.Round(obg, 1.2f);
                var txt = Ui.VStack(offer, 0, 0, TextAnchor.MiddleLeft);
                Ui.Size(txt.gameObject, -1, 100, 1);
                Ui.Label(txt, "Başlangıç paketi · 2.000 coin + 3'er güçlendirici", 30, Theme.Ink, TextAnchor.MiddleLeft, true);
                _starterTimer = Ui.Label(txt, "", 28, Theme.Muted, TextAnchor.MiddleLeft);
                Ui.Button(offer, app.Iap.LocalizedPrice(Skus.Starter), () => app.Buy(Skus.Starter, "home", () => app.GoHome()), Theme.Primary, Color.white, 96, 34, 220);
            }

            // Ev görseli (dokununca Ev ekranı)
            var house = Home.CurrentHouse(p);
            var next = Home.NextTask(p);
            HouseView.Card(v, app, house, () => app.Nav.Show(new DecorScreen()), flex: true);

            // Nine konuşma balonu
            string note = next != null ? next.NineNote : "Bütün evler pırıl pırıl oldu, ellerine sağlık.";
            NineSays(v, note);

            // Ana buton
            int lvl = p.CurrentLevel;
            if (app.Levels.Get(lvl) != null)
            {
                var play = Ui.Button(v, $"Bölüm {lvl}", () => app.StartLevel(lvl), Theme.Primary, Color.white, 200, 76);
                Fx.Breathe(play.transform, 0.025f, 1.8f);
            }
            else
            {
                var card = Ui.Card(v, 10, 28);
                Ui.Label(card, "Yeni desenler yakında", 48, Theme.Ink, TextAnchor.MiddleCenter, true);
                int replay = FirstNotPerfect();
                if (replay > 0) Ui.Button(card, $"Bölüm {replay}'i 3★ yap", () => app.StartLevel(replay), Theme.Primary, Color.white, 120, 40);
            }

            var bottom = Ui.HStack(v, 16, 0, TextAnchor.MiddleCenter, 150);
            bool canTask = next != null && m.Wallet.Ilmek >= Home.CostOf(next);
            NavTile(bottom, "map", "Harita", () => app.Nav.Show(new LevelMapScreen()), false);
            NavTile(bottom, "home", "Ev", () => app.Nav.Show(new DecorScreen()), canTask);
            NavTile(bottom, "shop", "Mağaza", () => app.Nav.Show(new ShopScreen()), false);
            Tick();
        }

        public static void NineSays(Transform parent, string note, float h = 150)
        {
            var nine = Ui.HStack(parent, 12, 0, TextAnchor.MiddleLeft, h);
            nine.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            var face = Ui.Icon(nine, Art.Nine, h);
            Fx.PopIn(face.transform, 0.4f, 0.15f);
            var bubble = Ui.Box(nine, "Bubble", Theme.Panel);
            if (Art.Bubble != null) Ui.Round(bubble, 1f, Art.Bubble);
            Ui.Soft(bubble, 6);
            Ui.Size(bubble.gameObject, -1, h - 10, 1);
            var t = Ui.Label(bubble.transform, $"\"{note}\"", 32, Theme.Ink, TextAnchor.MiddleLeft, false);
            ((RectTransform)t.transform).Stretch(44, 8, 24, 8);
            Fx.PopIn(bubble.transform, 0.4f, 0.25f);
        }

        private static Text Tile(Transform parent, string icon, string title, string sub, Action tap, bool attention)
        {
            var b = Ui.Button(parent, "", tap, attention ? Theme.WithAlpha(Theme.Gold, 0.9f) : Theme.Panel, Theme.Ink, 168, 28);
            var inner = Ui.VStack(b.transform, 0, 6, TextAnchor.MiddleCenter);
            inner.Stretch(4, 6, 4, 14);
            var ic = Ui.Icon(inner, Art.Icon(icon), 62, Theme.Primary);
            Ui.Size(ic.gameObject, 62, 62, 0);
            Ui.Label(inner, title, 28, Theme.Ink, TextAnchor.MiddleCenter, true, 34);
            var s = Ui.Label(inner, sub, 24, Theme.Muted, TextAnchor.MiddleCenter, false, 30);
            if (attention) Ui.Badge(b.transform, "!", Theme.Primary);
            return s;
        }

        private static void NavTile(Transform parent, string icon, string title, Action tap, bool attention)
        {
            var b = Ui.Button(parent, "", tap, Theme.Panel, Theme.Ink, 150, 30);
            var inner = Ui.HStack(b.transform, 14, 0, TextAnchor.MiddleCenter);
            inner.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            ((RectTransform)inner).Stretch(8, 4, 8, 14);
            var ic = Ui.Icon(inner, Art.Icon(icon), 70, Theme.Primary);
            Ui.Size(ic.gameObject, 70, 70, 0);
            var t = Ui.Label(inner, title, 36, Theme.Ink, TextAnchor.MiddleLeft, true);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            Ui.Size(t.gameObject, 190, 100, 0);
            if (attention) { Ui.Badge(b.transform, "!", Theme.Primary); Fx.Pulse(b.targetGraphic, Theme.Panel, Theme.WithAlpha(Theme.Gold, 0.8f), 1.2f); }
        }

        public static Button IconButton(Transform parent, string icon, Action tap, float size)
        {
            var b = Ui.Button(parent, "", tap, Theme.Panel, Theme.Ink, size, 30, size);
            var ic = Ui.Icon(b.transform, Art.Icon(icon), size * 0.55f, Theme.Ink);
            var rt = ic.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, 4);
            return b;
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
            if (App.Backend.Online)
            {
                // Sunucu otoritatif: sıra ve ödül sunucudan (UAT RACE-05..07)
                var week = m.Race.WeekId;
                App.Profile.RaceRewardClaimedWeek = week;
                App.Save();
                App.Backend.RaceClaim(week, (r, c) =>
                {
                    if (c <= 0) return;
                    App.Meta.Wallet.AddCoins(c);
                    if (r == 1) App.Profile.Cosmetics.Add($"frame-race-{week}");
                    App.Save();
                    App.Nav.Confirm("Tezgâh Yarışı bitti", $"{r}. oldun! +{Fmt.N(c)} coin", "Harika", null, null, null);
                });
                return;
            }
            var (rank, coins) = m.Race.ClaimIfEnded(App.Profile, m.Race.BuildGroup(App.Profile));
            App.Save();
            App.Analytics.Track("race_result", ("week", m.Race.WeekId), ("rank", rank), ("flags", App.Profile.RaceFlags));
            if (coins > 0) App.Nav.Confirm("Tezgâh Yarışı bitti", $"{rank}. oldun! +{Fmt.N(coins)} coin", "Harika", null, null, null);
        }

        public override void Tick()
        {
            var m = App.Meta;
            if (_starterTimer != null) _starterTimer.text = Loc.T($"Kalan {Fmt.Span(m.Starter.Remaining(App.Profile))}");
            if (_lives != null) LivesModal.PillText(_lives, App);
            if (_raceTimer != null) _raceTimer.text = Loc.T(m.Race.IsActive ? Fmt.Span(m.Race.Remaining) : "Cuma");
        }
    }

    /// <summary>
    /// Ev görselleri: her alanın bakımsız (eski) ve onarılmış (yeni) illüstrasyonu üst üste; onarılmış katmanın
    /// opaklığı alandaki tamamlanan görev oranıdır. Görev bitince yumuşakça bir sonraki orana geçer.
    /// </summary>
    public static class HouseView
    {
        /// <summary>Ana ekrandaki mekân kartı: sıradaki görevin alanı büyük, başlık ve ilerleme.</summary>
        public static void Card(Transform parent, App app, House house, Action tap, bool flex)
        {
            var p = app.Profile;
            var next = Home.NextTask(p);
            int area = next != null && next.House == house.Id ? Array.IndexOf(house.Areas, next.Area) : house.Areas.Length - 1;
            var frame = Ui.Box(parent, "HouseCard", Theme.Panel);
            Ui.Round(frame, 1.2f);
            Ui.Soft(frame, 10);
            if (flex) Ui.Size(frame.gameObject, -1, 420, -1, 1);
            else Ui.Size(frame.gameObject, -1, 520);
            var pic = Ui.Rect(frame.transform, "Pic").Stretch(14, 14, 14, 96);
            pic.gameObject.AddComponent<RectMask2D>();
            // Ana ekranda kasvetli görünmesin: onarılmış hâl ağırlıklı (bakımsız katman en fazla %45 görünür).
            Area(pic, house, area, Mathf.Max(0.55f, Home.DoneInArea(p, house, area) / 4f));
            var cap = Ui.HStack(frame.transform, 14, 0, TextAnchor.MiddleLeft);
            cap.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            var crt = (RectTransform)cap;
            crt.anchorMin = new Vector2(0, 0);
            crt.anchorMax = new Vector2(1, 0);
            crt.pivot = new Vector2(0.5f, 0);
            crt.sizeDelta = new Vector2(-40, 90);
            crt.anchoredPosition = new Vector2(0, 6);
            var title = Ui.Label(cap, house.Name, 40, Theme.Ink, TextAnchor.MiddleLeft, true);
            Ui.Size(title.gameObject, -1, 80, 1);
            var holder = Ui.Rect(cap, "Bar");
            Ui.Size(holder.gameObject, 240, 28, 0);
            Ui.Bar(holder, 28, Theme.Ok, Theme.Neutral)(Home.Progress(p, house));
            ((RectTransform)holder.GetChild(0)).Stretch();
            var pct = Ui.Label(cap, $"%{Mathf.RoundToInt(Home.Progress(p, house) * 100)}", 34, Theme.Ink, TextAnchor.MiddleRight, true);
            Ui.Size(pct.gameObject, 100, 80, 0);
            if (tap != null)
            {
                var b = frame.gameObject.AddComponent<Button>();
                b.targetGraphic = frame;
                frame.gameObject.AddComponent<PressFx>().Pressed = 0.97f;
                b.onClick.AddListener(() => { Audio.Click(); tap(); });
            }
        }

        /// <summary>Alan görseli (eski + yeni katman). Dönen: yeni katman (opaklığı animasyonla değiştirmek için).</summary>
        public static Image Area(RectTransform parent, House house, int area, float restored)
        {
            string key = house.AreaArt[Mathf.Clamp(area, 0, house.AreaArt.Length - 1)];
            var oldImg = Ui.Icon(parent, Art.Area(key, false), 0);
            oldImg.preserveAspect = false;
            oldImg.rectTransform.Stretch();
            var newImg = Ui.Icon(parent, Art.Area(key, true), 0);
            newImg.preserveAspect = false;
            newImg.rectTransform.Stretch();
            newImg.color = new Color(1, 1, 1, restored);
            if (oldImg.sprite == null) oldImg.color = Theme.Sand;
            return newImg;
        }
    }

    /// <summary>
    /// 5 · Ev: mekân seçici, alan sekmeleri, alanın canlı görseli ve o alanın görev kartları. Görev yapılınca görsel
    /// onarılmış hâline doğru renklenir; alan ve mekân tamamlanınca kutlama. Yeni mekân açılınca Nine tanıtır.
    /// </summary>
    public sealed class DecorScreen : UiScreen
    {
        private House _house;
        private int _area;

        public DecorScreen(House house = null, int area = -1) { _house = house; _area = area; }

        public override void Build(RectTransform root)
        {
            var app = App;
            var p = app.Profile;
            if (_house == null) _house = Home.CurrentHouse(p);
            var next = Home.NextTask(p);
            if (_area < 0) _area = next != null && next.House == _house.Id ? Array.IndexOf(_house.Areas, next.Area) : 0;

            var v = Ui.VStack(root, 0, 0).Stretch();
            var header = Ui.Header(v, _house.Name, app.GoHome);
            Ui.IlmekPill(header, app);

            var body = Ui.VStack(v, 16, 24);
            Ui.Size(body.gameObject, -1, -1, -1, 1);

            // Mekân seçici (açılmış olanlar)
            var unlocked = Home.Houses.Where(h => Home.IsUnlocked(p, h)).ToList();
            if (unlocked.Count > 1)
            {
                var hs = Ui.HStack(body, 12, 0, TextAnchor.MiddleCenter, 86);
                foreach (var h in unlocked)
                {
                    var hh = h;
                    Ui.Button(hs, Home.IsComplete(p, h) ? $"{h.Name} ✓" : h.Name, () => app.Nav.Show(new DecorScreen(hh)),
                        h == _house ? Theme.Gold : Theme.Panel, Theme.Ink, 86, 28);
                }
            }

            // Alan sekmeleri
            var tabs = Ui.HStack(body, 10, 0, TextAnchor.MiddleCenter, 96);
            for (int i = 0; i < _house.Areas.Length; i++)
            {
                int idx = i;
                int done = Home.DoneInArea(p, _house, i);
                Ui.Button(tabs, $"{_house.Areas[i]}\n{done}/4{(done == 4 ? " ✓" : "")}", () => app.Nav.Show(new DecorScreen(_house, idx)),
                    i == _area ? Theme.Primary : Theme.Panel, i == _area ? Color.white : Theme.Ink, 96, 24);
            }

            // Görsel
            var frame = Ui.Box(body, "Frame", Theme.Panel);
            Ui.Round(frame, 1.2f);
            Ui.Soft(frame, 10);
            Ui.Size(frame.gameObject, -1, 520);
            var pic = Ui.Rect(frame.transform, "Pic").Stretch(12, 12, 12, 12);
            pic.gameObject.AddComponent<RectMask2D>();
            var restoredLayer = HouseView.Area(pic, _house, _area, Home.DoneInArea(p, _house, _area) / 4f);

            // Görevler
            var list = Ui.Scroll(body, 14, 8);
            var tasks = _house.Tasks.Skip(_area * 4).Take(4).ToList();
            foreach (var t in tasks)
            {
                var task = t;
                bool done = p.HomeTasksDone.Contains(t.Id);
                var card = Ui.Card(list, 8, 24, done ? Theme.Cream : Theme.Panel);
                var row = Ui.HStack(card, 14, 0, TextAnchor.MiddleLeft, 80);
                row.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
                var title = Ui.Label(row, t.Title, 36, done ? Theme.Muted : Theme.Ink, TextAnchor.MiddleLeft, true);
                Ui.Size(title.gameObject, -1, 80, 1);
                if (done) { Ui.Label(row, "✓", 52, Theme.Ok, TextAnchor.MiddleCenter, true); }
                else
                {
                    Ui.Icon(row, Art.Ilmek, 52);
                    var c = Ui.Label(row, Home.CostOf(t).ToString(), 40, Theme.Ink, TextAnchor.MiddleLeft, true);
                    Ui.Size(c.gameObject, 50, 80, 0);
                }

                if (done && t.Styles != null)
                {
                    var styles = Ui.HStack(card, 8, 0, TextAnchor.MiddleCenter, 84);
                    foreach (var s in t.Styles)
                    {
                        var style = s;
                        bool sel = p.HomeStyles.TryGetValue(t.Id, out var cur) && cur == s;
                        Ui.Button(styles, s, () => { Home.ChangeStyle(p, task, style); app.Save(); app.Nav.Show(new DecorScreen(_house, _area)); },
                            sel ? Theme.Gold : Theme.Neutral, Theme.Ink, 84, 30);
                    }
                }
                else if (!done && task == next)
                {
                    if (Home.CanDo(p, task))
                    {
                        var b = Ui.Button(card, "Yap", () => DoTask(task, restoredLayer), Theme.Primary, Color.white, 104, 40);
                        Fx.Breathe(b.transform, 0.03f, 1.4f);
                    }
                    else
                    {
                        var r2 = Ui.HStack(card, 12, 0, TextAnchor.MiddleCenter, 100);
                        Ui.Label(r2, $"{Home.CostOf(task) - p.Wallet.Ilmek} ilmek daha", 32, Theme.Muted);
                        Ui.Button(r2, "Oyna ▶", () => app.StartLevel(p.CurrentLevel), Theme.Primary, Color.white, 100, 38);
                    }
                }
                else if (!done) Ui.Label(card, "Sırada bekliyor", 28, Theme.Muted, TextAnchor.MiddleLeft);
            }

            if (!p.HousesIntroduced.Contains(_house.Id) && Home.IsUnlocked(p, _house))
            {
                p.HousesIntroduced.Add(_house.Id);
                app.Save();
                App.Delay(0.35f, () => Intro(_house));
            }
        }

        private void Intro(House h)
        {
            var card = App.Nav.Modal();
            var pic = Ui.Rect(card, "Pic");
            Ui.Size(pic.gameObject, -1, 420);
            pic.gameObject.AddComponent<RectMask2D>();
            HouseView.Area(pic, h, 0, 1f);
            Ui.Label(card, h.Name, 60, Theme.Primary, TextAnchor.MiddleCenter, true);
            HomeScreen.NineSays(card, h.Intro, 140);
            Ui.Button(card, "Başlayalım", () => App.Nav.Close(card));
            Audio.Play("unlock");
        }

        private void DoTask(HomeTask t, Image restoredLayer)
        {
            var app = App;
            void Finish(string style)
            {
                var p = app.Profile;
                if (!Home.Do(p, t, style)) return;
                app.Analytics.Track("task_done", ("house", t.House), ("area", t.Area), ("task", t.Id), ("style", style ?? ""));
                app.Save();
                Audio.Play("unlock");
                Haptic.Medium();
                float from = restoredLayer != null ? restoredLayer.color.a : 0;
                float to = Home.DoneInArea(p, _house, _area) / 4f;
                Fx.To(restoredLayer, 1.1f, Ease.InOutSine, k => { if (restoredLayer != null) restoredLayer.color = new Color(1, 1, 1, Mathf.Lerp(from, to, k)); });
                Fx.Confetti(app.Nav.Overlay, new Vector2(0, 500), 34, 0.8f);
                bool areaDone = Home.DoneInArea(p, _house, _area) == 4;
                bool houseDone = Home.IsComplete(p, _house);
                app.Delay(1.2f, () =>
                {
                    app.Nav.Show(new DecorScreen(_house, _area));
                    if (houseDone)
                    {
                        int i = Home.Houses.IndexOf(_house);
                        var nextHouse = i + 1 < Home.Houses.Count ? Home.Houses[i + 1] : null;
                        app.Nav.Confirm($"{_house.Name} tamamlandı!", nextHouse != null ? $"Sırada: {nextHouse.Name}" : "Bütün evleri onardın. Nine seninle gurur duyuyor!",
                            nextHouse != null ? "Hadi gidelim" : "Harika", () => { if (nextHouse != null) app.Nav.Show(new DecorScreen(nextHouse)); }, null, null);
                        Fx.Confetti(app.Nav.Overlay, new Vector2(0, 200), 80, 1.2f);
                    }
                    else if (areaDone)
                        app.Nav.Confirm($"{_house.Areas[_area]} tamamlandı!", $"Nine: \"{t.NineNote}\"", "Sıradaki alan", () => app.Nav.Show(new DecorScreen(_house, Mathf.Min(_area + 1, 4))), null, null);
                    else
                        app.Nav.Toast($"Nine: \"{t.NineNote}\"", 3f);
                });
            }
            if (t.Styles == null) { Finish(null); return; }
            app.Nav.Choice("Stil seç", "Sonra ücretsiz değiştirebilirsin.", t.Styles.Select(s => (s, (Action)(() => Finish(s)))).ToArray());
        }

        public override bool OnBack() { App.GoHome(); return true; }
    }

    /// <summary>
    /// Bölüm haritası: bölgeler halinde tüm bölümler, her birinde kazanılan yıldızlar. Kazanılmış bölümler tekrar
    /// oynanabilir (3★ için); sıradaki bölüm nefes alır; kilitliler kilitli. Açılışta sıradaki bölüme kaydırır.
    /// </summary>
    public sealed class LevelMapScreen : UiScreen
    {
        public override void Build(RectTransform root)
        {
            var app = App;
            var p = app.Profile;
            var w = app.Meta.Wallet;
            var v = Ui.VStack(root, 0, 0).Stretch();
            Ui.Header(v, "Bölümler", app.GoHome, app);
            int totalStars = 0;
            for (int i = 1; i <= app.Levels.Count; i++) totalStars += w.BestStars(i);
            Ui.Label(v, $"★ {totalStars} / {app.Levels.Count * 3}", 38, Theme.Ink, TextAnchor.MiddleCenter, true, 64);

            var list = Ui.Scroll(v, 18, 28);
            var sr = list.parent.GetComponent<ScrollRect>();
            int regions = (app.Levels.Count + Levels.PerRegion - 1) / Levels.PerRegion;
            int currentRow = 0, row = 0;
            for (int r = 1; r <= regions; r++)
            {
                Ui.Label(list, $"Bölge {r}", 44, Theme.Ink, TextAnchor.MiddleLeft, true, 70);
                row++;
                RectTransform line = null;
                for (int k = 0; k < Levels.PerRegion; k++)
                {
                    int id = (r - 1) * Levels.PerRegion + k + 1;
                    var lvl = app.Levels.Get(id);
                    if (lvl == null) break;
                    if (k % 5 == 0) { line = Ui.HStack(list, 16, 0, TextAnchor.MiddleCenter, 190); row++; }
                    Node(line, id, lvl.Boss, w.BestStars(id), id < p.CurrentLevel, id == p.CurrentLevel);
                    if (id == p.CurrentLevel) currentRow = row;
                }
            }
            int rows = Mathf.Max(1, row);
            App.StartCoroutine(ScrollTo(sr, 1f - Mathf.Clamp01((currentRow - 2f) / rows)));
        }

        private static IEnumerator ScrollTo(ScrollRect sr, float pos)
        {
            yield return null;
            yield return null;
            if (sr == null) yield break;
            Canvas.ForceUpdateCanvases();
            float from = sr.verticalNormalizedPosition;
            Fx.To(sr, 0.6f, Ease.OutCubic, k => { if (sr != null) sr.verticalNormalizedPosition = Mathf.Lerp(from, pos, k); });
        }

        private void Node(Transform parent, int id, bool boss, int stars, bool won, bool current)
        {
            var app = App;
            bool open = won || current;
            var bg = current ? Theme.Primary : won ? Theme.Panel : Theme.Neutral;
            var b = Ui.Button(parent, "", () =>
            {
                if (open) app.StartLevel(id);
                else app.Nav.Toast($"Önce Bölüm {app.Profile.CurrentLevel}'i bitir.");
            }, bg, Theme.Ink, 180, 30);
            var inner = Ui.VStack(b.transform, 0, 4, TextAnchor.MiddleCenter);
            inner.Stretch(4, 8, 4, 14);
            if (open)
            {
                Ui.Label(inner, id.ToString(), 52, current ? Color.white : Theme.Ink, TextAnchor.MiddleCenter, true, 70);
                string s = "";
                for (int i = 0; i < 3; i++) s += i < stars ? "★" : "☆";
                Ui.Label(inner, current ? "Oyna" : s, 32, current ? Color.white : Theme.Gold, TextAnchor.MiddleCenter, true, 40);
            }
            else
            {
                var ic = Ui.Icon(inner, Art.Lock, 56, Theme.Muted);
                Ui.Size(ic.gameObject, 56, 56, 0);
                Ui.Label(inner, id.ToString(), 30, Theme.Muted, TextAnchor.MiddleCenter, true, 36);
            }
            if (boss) Ui.Badge(b.transform, "Usta", Theme.Gold);
            if (current) Fx.Breathe(b.transform, 0.05f, 1.2f);
        }

        public override bool OnBack() { App.GoHome(); return true; }
    }

    /// <summary>
    /// Can kutusu ve "Canların bitti" penceresi. Can her saat 1 yenilenir (3 saatte tam). Hemen oynamak için:
    /// coin ile doldur, satın al ya da reklam izleyip +1 can.
    /// </summary>
    public static class LivesModal
    {
        public static Text Pill(Transform parent, App app)
        {
            var t = Ui.Pill(parent, Art.Icon("heart"), "", 210, 86);
            var pill = t.transform.parent;
            foreach (var i in pill.GetComponentsInChildren<Image>()) if (i.sprite == Art.Icon("heart")) i.color = Theme.Primary;
            PillText(t, app);
            var b = pill.gameObject.AddComponent<Button>();
            b.targetGraphic = pill.GetComponent<Image>();
            pill.gameObject.AddComponent<PressFx>();
            b.onClick.AddListener(() => { Audio.Click(); Show(app, null); });
            return t;
        }

        public static void PillText(Text t, App app)
        {
            var l = app.Meta.Lives;
            int n = l.Count;
            bool full = n >= l.Config.Max;
            t.text = full ? $"{n}" : $"{n} · {Clock(l.NextIn)}";
            t.fontSize = full ? 40 : 28;
        }

        private static string Clock(System.TimeSpan s) =>
            s.TotalHours >= 1 ? $"{(int)s.TotalHours}:{s.Minutes:00}:{s.Seconds:00}" : $"{s.Minutes:00}:{s.Seconds:00}";

        public static void Show(App app, System.Action thenPlay)
        {
            var l = app.Meta.Lives;
            var card = app.Nav.Modal(dimTapCloses: true);
            var hearts = Ui.HStack(card, 14, 0, TextAnchor.MiddleCenter, 130);
            hearts.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            for (int i = 0; i < l.Config.Max; i++)
            {
                var h = Ui.Icon(hearts, Art.Icon("heart"), 110, i < l.Count ? Theme.Primary : Theme.Neutral);
                Ui.Size(h.gameObject, 110, 110, 0);
                Fx.PopIn(h.transform, 0.35f, 0.08f * i);
            }
            bool empty = l.Count == 0;
            Ui.Label(card, empty ? "Canların bitti" : $"{l.Count} canın var", 56, Theme.Ink, TextAnchor.MiddleCenter, true);
            var timer = Ui.Label(card, "", 36, Theme.Muted);
            void Tick()
            {
                if (timer == null) return;
                timer.text = l.IsFull ? Loc.T("Canların dolu. Kazandığın bölümde can harcanmaz.")
                    : Loc.T($"Canlar {Clock(l.FullIn)} sonra dolar");
                if (thenPlay != null && empty && l.CanPlay) { app.Nav.Close(card); thenPlay(); return; }
                app.Delay(1f, Tick);
            }
            Tick();
            if (!l.IsFull)
            {
                var w = app.Meta.Wallet;
                bool coinsOk = w.Coins >= l.Config.RefillCoins;
                void Done() { app.Save(); app.Nav.Close(card); Audio.Play("unlock"); if (thenPlay != null) thenPlay(); else app.Nav.Toast("Canlar doldu!"); }
                if (coinsOk)
                    Ui.Button(card, $"Hepsini doldur · ● {Fmt.N(l.Config.RefillCoins)}", () => { if (l.TryRefillWithCoins(w)) Done(); }, Theme.Primary, Color.white, 120, 40);
                Ui.Button(card, $"Hepsini doldur · {app.Iap.LocalizedPrice(Skus.Lives)}", () => app.Buy(Skus.Lives, "lives", Done),
                    coinsOk ? Theme.Panel : Theme.Primary, coinsOk ? Theme.Ink : Color.white, 120, 40);
                if (app.Ads.RewardedReady)
                    Ui.Button(card, "Reklam izle · +1 can", () => app.ShowRewarded("lives", () => { l.Add(1); Done(); }), Theme.Gold, Theme.Ink, 120, 38);
            }
            Ui.Button(card, "Tamam", () => app.Nav.Close(card), Theme.Neutral, Theme.Ink);
        }
    }
}
