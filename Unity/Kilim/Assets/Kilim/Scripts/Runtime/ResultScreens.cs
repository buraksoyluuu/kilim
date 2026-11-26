using System;
using Kilim.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Kilim.Game
{
    /// <summary>
    /// 3 · Kazandın: başlık zıplar, yıldızlar sırayla uçarak oturur (3★'da konfeti), desen yüzdesi ve coin sayarak artar,
    /// ilmek/açılan şeyler kart olarak görünür. Butonlar: Sonraki bölüm (birincil), Tekrar oyna, Ana menü.
    /// </summary>
    public sealed class WinScreen : UiScreen
    {
        private readonly LevelRun _run;
        private readonly WinReport _r;
        private Text _coins;
        private Button _double;
        private int _coinsShown;

        public WinScreen(LevelRun run, WinReport report) { _run = run; _r = report; }

        private LevelData L => _run.Level;

        public override void Build(RectTransform root)
        {
            var app = App;
            var v = Ui.VStack(root, 22, 56, TextAnchor.MiddleCenter).Stretch();
            Ui.Spacer(v);
            bool record = _r.IsReplay && _r.Stars > _r.PreviousBest;
            string title = record ? "Yeni rekor!" : _r.Stars == 3 ? "Muhteşem!" : _r.Stars == 2 ? "Çok güzel!" : "Tamamlandı!";
            var t = Ui.Label(v, title, 96, Theme.Primary, TextAnchor.MiddleCenter, true, 130);
            Fx.PopIn(t.transform, 0.5f);
            Ui.Label(v, _r.IsDaily ? "Günün Deseni" : $"Bölüm {L.Id}{(L.Boss ? " · Usta" : "")}", 40, Theme.Muted, TextAnchor.MiddleCenter, true, 56);

            // Yıldızlar: ortadaki biraz büyük ve yukarıda (klasik podyum).
            var stars = Ui.HStack(v, 18, 0, TextAnchor.MiddleCenter, 230);
            stars.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            for (int i = 0; i < 3; i++)
            {
                float size = i == 1 ? 200 : 160;
                var holder = Ui.Rect(stars, "StarHolder");
                Ui.Size(holder.gameObject, size, 230, 0);
                var bgStar = Ui.Icon(holder, Art.Star, size, Theme.Neutral);
                Place(bgStar.rectTransform, size, i == 1 ? 20 : -10);
                if (i >= _r.Stars) continue;
                var s = Ui.Icon(holder, Art.Star, size, Theme.Gold);
                Place(s.rectTransform, size, i == 1 ? 20 : -10);
                s.transform.localScale = new Vector3(0.001f, 0.001f, 1);
                int idx = i;
                float delay = 0.45f + 0.35f * idx;
                Fx.To(s, 0.45f, Ease.OutBack, k =>
                {
                    float sc = Mathf.LerpUnclamped(2.2f, 1f, k);
                    s.transform.localScale = new Vector3(sc, sc, 1);
                    s.transform.localRotation = Quaternion.Euler(0, 0, (1 - k) * -40);
                    var c = s.color; c.a = Mathf.Clamp01(k * 2); s.color = c;
                }, () =>
                {
                    Audio.Play($"star{idx + 1}");
                    Haptic.Light();
                    Fx.Pop(holder, 1.12f, 0.25f);
                    if (idx == 2) Fx.Confetti(App.Nav.Overlay, new Vector2(0, 300), 70, 1.1f);
                }, delay);
            }

            var pct = Ui.Label(v, "Desen %0", 48, Theme.Ink, TextAnchor.MiddleCenter, true, 64);
            int target = Mathf.RoundToInt((float)_r.PatternPercent * 100);
            Fx.Count(pct, 0, target, 1.0f, n => Loc.T($"Desen %{n}"), 0.3f);

            var card = Ui.Card(v, 14, 36, Theme.Cream);
            if (_r.IsDaily)
            {
                Ui.Label(card, _r.DailyCoins > 0 ? $"Günün deseni: +{_r.DailyCoins} coin ve damga" :
                    App.Meta.Daily.StampedToday(App.Profile) ? "Bugünün damgası zaten alındı." : "100 coin ve damga için 3★ gerekli.", 40);
            }
            else
            {
                var row = Ui.HStack(card, 40, 0, TextAnchor.MiddleCenter, 96);
                row.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
                _coins = Reward(row, Art.Coin, "+0");
                _coinsShown = _r.Coins;
                Fx.Count(_coins, 0, _r.Coins, 0.9f, n => $"+{n}", 1.2f, n => { if (n % 5 == 0) Audio.Play("coin", 0.25f); });
                if (_r.Ilmek > 0)
                {
                    var il = Reward(row, Art.Ilmek, $"+{_r.Ilmek}");
                    Fx.PopIn(il.transform.parent, 0.4f, 1.6f);
                }
                if (_r.IsReplay) Ui.Label(card, "Tekrar oynamada coin yarı, ilmek yalnızca yeni yıldız için.", 28, Theme.Muted);
                if (_r.Piggy > 0) Ui.Label(card, $"Kumbaraya +{_r.Piggy}", 32, Theme.Muted);
                if (_r.RaceFlag) Ui.Label(card, "Tezgâh Yarışı: +1 bayrak", 32, Theme.Ok);
                if (_r.PassLevelAfter > _r.PassLevelBefore)
                {
                    var nb = Ui.Button(card, $"YENİ ÖDÜL · Sezon kartı seviye {_r.PassLevelAfter} ›", () => app.Nav.Show(new PassScreen()), Theme.Ok, Color.white, 70, 30);
                    Fx.Pulse(nb.targetGraphic, Theme.Ok, Theme.WithAlpha(Theme.Gold, 0.9f), 1.2f);
                }
                Fx.PopIn(card, 0.4f, 1.0f);

                if (_r.UnlocksNext.HasValue) Unlock(v, Art.Icon(_r.UnlocksNext.Value == Booster.Undo ? "undo" : _r.UnlocksNext.Value == Booster.Swap ? "swap" : "stitch"),
                    $"Yeni güçlendirici: {GameScreen.BoosterName(_r.UnlocksNext.Value)}", "Sıradaki bölümde 3 tane hediye!", null);
                if (_r.TaskAffordable)
                {
                    var task = Home.NextTask(App.Profile);
                    if (task != null) Unlock(v, Art.Icon("home"), "Evde yeni görev yapılabilir!", task.Title, () => App.Nav.Show(new DecorScreen()));
                }
            }

            if (!_r.IsDaily && _r.CanDoubleCoins && !_r.Doubled && app.Ads.RewardedReady)
            {
                _double = Ui.Button(v, "Reklam izle · coin ×2", () =>
                    app.ShowRewarded("win_double", () =>
                    {
                        if (App.Meta.DoubleWinCoins(_run))
                        {
                            App.Save();
                            Fx.Count(_coins, _coinsShown, _r.Coins * 2, 0.6f, n => $"+{n}");
                            Fx.Pop(_coins.transform.parent, 1.2f);
                            Audio.Play("coin");
                            _double.gameObject.SetActive(false);
                        }
                    }), Theme.Gold, Theme.Ink, 120, 40);
            }

            Ui.Spacer(v);
            if (_r.IsDaily)
            {
                Ui.Button(v, "Devam", () => App.Nav.Show(new DailyScreen()));
                return;
            }
            bool hasNext = app.Levels.Get(app.Profile.CurrentLevel) != null;
            int next = _r.IsReplay ? app.Profile.CurrentLevel : L.Id + 1;
            if (app.Levels.Get(next) != null)
            {
                var play = Ui.Button(v, _r.IsReplay ? $"Bölüm {next}'e dön" : $"Bölüm {next} ▶", () => app.LeaveWin(L.Id, () => app.StartLevel(next)), Theme.Primary, Color.white, 150, 52);
                Fx.PopIn(play.transform, 0.4f, 1.3f);
                App.Delay(1.8f, () => { if (play != null) Fx.Breathe(play.transform, 0.03f, 1.5f); });
            }
            var row2 = Ui.HStack(v, 16, 0, TextAnchor.MiddleCenter, 120);
            Ui.Button(row2, "Tekrar oyna", () => app.LeaveWin(L.Id, () => app.StartLevel(L.Id)), Theme.Panel, Theme.Ink, 120, 38);
            Ui.Button(row2, "Ana menü", () => app.LeaveWin(L.Id, app.GoHome), Theme.Panel, Theme.Ink, 120, 38);
            if (!hasNext && !_r.IsReplay) Ui.Label(v, "Yeni desenler yakında!", 32, Theme.Muted);
        }

        private static void Place(RectTransform rt, float size, float y)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = new Vector2(0, y);
        }

        private static Text Reward(Transform row, Sprite icon, string text)
        {
            var g = Ui.HStack(row, 12, 0, TextAnchor.MiddleCenter, 96);
            g.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            Ui.Icon(g, icon, 80);
            var t = Ui.Label(g, text, 60, Theme.Ink, TextAnchor.MiddleLeft, true);
            Ui.Size(t.gameObject, 200, 96, 0);
            return t;
        }

        /// <summary>"Açıldı" kartı: simge + başlık + alt metin; isteğe bağlı dokunma eylemi.</summary>
        private void Unlock(Transform parent, Sprite icon, string title, string sub, Action tap)
        {
            var row = Ui.HStack(parent, 20, 24, TextAnchor.MiddleLeft, 150);
            row.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            var bg = row.gameObject.AddComponent<Image>();
            bg.color = Theme.WithAlpha(Theme.Gold, 0.28f);
            Ui.Round(bg, 1.2f);
            Ui.Icon(row, icon, 100, Theme.Primary);
            var txt = Ui.VStack(row, 2, 0, TextAnchor.MiddleLeft);
            Ui.Size(txt.gameObject, -1, 110, 1);
            Ui.Label(txt, title, 38, Theme.Ink, TextAnchor.MiddleLeft, true);
            Ui.Label(txt, sub, 30, Theme.Muted, TextAnchor.MiddleLeft);
            if (tap != null)
            {
                var b = row.gameObject.AddComponent<Button>();
                b.targetGraphic = bg;
                row.gameObject.AddComponent<PressFx>();
                b.onClick.AddListener(() => { Audio.Click(); App.LeaveWin(L.Id, tap); });
                Ui.Label(row, "›", 64, Theme.Ink, TextAnchor.MiddleCenter, true);
            }
            Fx.PopIn(row, 0.4f, 2.0f);
        }

        public override bool OnBack() { if (!_r.IsDaily) App.LeaveWin(L.Id, App.GoHome); else App.GoHome(); return true; }
    }

    /// <summary>
    /// 4 + 10 · Tezgâh doldu. Önce kurtarma (elindeki güçlendiricilerle, internet gerekmez), sonra devam merdiveni
    /// (Spec 2 §3: coin / satın alma / ödüllü reklam), en altta her zaman Yeniden başla ve Ana menü.
    /// Çevrimdışıyken reklam seçeneği gösterilmez; isteğe bağlı bölümler hata verse bile çıkış butonları kurulur.
    /// </summary>
    public static class FailFlow
    {
        public static void Show(LevelRun run, GameScreen game)
        {
            var app = App.I;
            var s = run.Session;
            if (s.State != SessionState.Failed) return;
            var ladder = run.Ladder;
            var wallet = app.Meta.Wallet;
            var cfg = app.Config.Economy;
            int price = ladder.CurrentPrice;
            int step = ladder.ContinuesTaken + 1;
            app.Analytics.Track("continue_offer", ("level", run.Level.Id), ("step", step), ("price", price));

            var card = app.Nav.Modal();
            var icon = Ui.Icon(card, Art.Icon("stuck"), 130, Theme.Primary);
            Fx.PopIn(icon.transform, 0.4f, 0.05f);
            Ui.Label(card, "Tezgâh doldu", 60, Theme.Ink, TextAnchor.MiddleCenter, true);
            if (ladder.IsLastChance) Ui.Label(card, "Son şans", 36, Theme.Primary, TextAnchor.MiddleCenter, true);
            int holes = s.Board.PlayableCells - s.Board.FilledCount;
            Ui.Label(card, s.RemainingPieces > 0
                ? $"Kalan {s.RemainingPieces} parçanın hiçbiri boş yerlere sığmıyor."
                : $"Parçalar bitti ama {holes} boş hücre kaldı.", 36, Theme.Muted);

            void Close() => app.Nav.Close(card);

            // 1) Kurtarma: elindeki (ya da coin'le alınabilen) güçlendiriciler — çevrimdışı çalışır.
            try
            {
                var boosters = new BoosterService(wallet, cfg);
                var rescue = Ui.HStack(card, 14, 0, TextAnchor.MiddleCenter, 170, "Rescue");
                int shown = 0;
                foreach (Booster b in new[] { Booster.SingleStitch, Booster.Undo, Booster.Swap })
                {
                    bool usable = wallet.IsUnlocked(b) || wallet.BoosterCount(b) > 0;
                    bool applicable = b == Booster.Swap ? s.RemainingInQueue > 0 && FirstSlot(s) >= 0 : boosters.IsApplicable(s, b);
                    bool affordable = wallet.BoosterCount(b) > 0 || wallet.Coins >= cfg.BoosterPrice[b];
                    if (!usable || !applicable || !affordable) continue;
                    var bb = b;
                    string cost = wallet.BoosterCount(b) > 0 ? $"×{wallet.BoosterCount(b)}" : $"● {cfg.BoosterPrice[b]}";
                    var btn = Ui.Button(rescue, "", () => { Close(); game.Rescue(bb); }, Theme.Panel, Theme.Ink, 170, 30);
                    var inner = Ui.VStack(btn.transform, 0, 6, TextAnchor.MiddleCenter);
                    inner.Stretch(6, 6, 6, 14);
                    var ic = Ui.Icon(inner, Art.Icon(b == Booster.Undo ? "undo" : b == Booster.Swap ? "swap" : "stitch"), 60, Theme.Ink);
                    Ui.Size(ic.gameObject, 60, 60, 0);
                    Ui.Label(inner, GameScreen.BoosterName(b), 28, Theme.Ink, TextAnchor.MiddleCenter, true, 34);
                    Ui.Label(inner, cost, 26, Theme.Muted, TextAnchor.MiddleCenter, true, 30);
                    shown++;
                }
                if (shown == 0) UnityEngine.Object.Destroy(rescue.gameObject);
                else Ui.Label(card, "Güçlendiriciyle kurtar, kaldığın yerden devam et.", 30, Theme.Muted);
            }
            catch (Exception e) { Debug.LogWarning(e); }

            // 2) Devam paketi (merdiven): +2 Tek İlmek ve +1 Değiştir. İlk 10 bölümde ücretli devam gösterilmez.
            bool early = run.Level.Id <= 10;
            if (!early)
            try
            {
                void Done(string how)
                {
                    app.Analytics.Track("continue_result", ("level", run.Level.Id), ("step", step), ("price", price), ("result", how));
                    app.Save();
                    Close();
                    game.Resume();
                }

                Ui.Label(card, "Devam paketi: +2 Tek İlmek ve +1 Değiştir", 32, Theme.Ink, TextAnchor.MiddleCenter, true);
                if (wallet.Coins >= price)
                {
                    Ui.Button(card, $"Devam · ● {Fmt.N(price)}", () =>
                    {
                        if (ladder.TryContinueWithCoins(wallet, run.Session)) Done("coin");
                    }, Theme.Primary, Color.white, 120, 42);
                }
                else
                {
                    string sku = Skus.SmallestPackCovering(price - wallet.Coins);
                    Ui.Button(card, $"Devam · {app.Iap.LocalizedPrice(sku)}", () =>
                        app.Buy(sku, "continue", () =>
                        {
                            if (ladder.TryContinueWithCoins(wallet, run.Session)) Done("iap");
                        }), Theme.Primary, Color.white, 120, 42);
                    Ui.Label(card, $"Coin {Fmt.N(wallet.Coins)} / {Fmt.N(price)} · {Fmt.N(Skus.CoinsOf(sku))} coin paketi alınır, {Fmt.N(price)} harcanır", 26, Theme.Muted);
                }

                bool adPossible = app.Config.ContinueAdEnabled && ladder.CanUseAd && app.Meta.Ads.CanShowRewarded(app.Profile);
                if (adPossible && app.Ads.RewardedReady)
                {
                    Ui.Button(card, "Reklam izle · ücretsiz devam", () =>
                        app.ShowRewarded("continue", () =>
                        {
                            if (ladder.ContinueWithRewardedAd(wallet, run.Session)) { app.Meta.Ads.OnRewardedEarned(app.Profile); Done("ad"); }
                        }), Theme.Gold, Theme.Ink, 120, 40);
                }
                else if (adPossible && !app.Backend.Online)
                    Ui.Label(card, "İnternet yok: reklamla devam şu an kullanılamıyor.", 28, Theme.Muted);
            }
            catch (Exception e) { Debug.LogWarning(e); }

            // 3) Her zaman: yeniden başla ve ana menü (etik kural: eşit boy, her durumda görünür).
            if (early)
                Ui.Button(card, "Tekrar dene", () => { Close(); app.Meta.GiveUp(run); app.Save(); app.StartLevel(run.Level.Id); }, Theme.Primary, Color.white, 130, 46);
            var exits = Ui.HStack(card, 16, 0, TextAnchor.MiddleCenter, 120);
            if (!early) Ui.Button(exits, "Yeniden başla", () => { Close(); GiveUpConfirm(run, game, restart: true); }, Theme.Neutral, Theme.Ink, 120, 38);
            Ui.Button(exits, "Ana menü", () => { Close(); GiveUpConfirm(run, game, restart: false); }, Theme.Neutral, Theme.Ink, 120, 38);
        }

        private static int FirstSlot(GameSession s)
        {
            for (int i = 0; i < GameSession.TraySlots; i++) if (s.Tray[i] != null) return i;
            return -1;
        }

        /// <summary>10.3 "Bölümü bırak?" — ardışık 3. kayıpta ücretsiz 1 Geri Al hediyesi.</summary>
        private static void GiveUpConfirm(LevelRun run, GameScreen game, bool restart)
        {
            var app = App.I;
            bool gift = app.Meta.WillGiftUndoOnGiveUp(run);
            app.Nav.Confirm(restart ? "Bölümü yeniden başlat?" : "Ana menüye dön?", gift ? "Zor bir bölüm! Sana 1 Geri Al hediye ediyoruz." : "Bu denemedeki ilerleme kaybolur.",
                restart ? "Yeniden başlat" : "Ana menü", () =>
                {
                    app.Meta.GiveUp(run);
                    app.Analytics.Track("continue_result", ("level", run.Level.Id), ("step", run.Ladder.ContinuesTaken + 1), ("price", run.Ladder.CurrentPrice), ("result", "giveup"));
                    app.Save();
                    if (restart) app.StartLevel(run.Level.Id);
                    else app.GoHome();
                },
                "Geri dön", () => Show(run, game));
        }
    }

    /// <summary>Coin yetmediğinde küçük paket seçici (oyundan çıkmadan).</summary>
    public static class CoinPacks
    {
        public static void Show(App app, int missing, Action after)
        {
            var card = app.Nav.Modal(dimTapCloses: true);
            Ui.Icon(card, Art.Coin, 120);
            Ui.Label(card, "Coin yetmiyor", 56, Theme.Ink, TextAnchor.MiddleCenter, true);
            if (missing > 0) Ui.Label(card, $"{Fmt.N(missing)} coin daha gerekli.", 36, Theme.Muted);
            foreach (var p in Skus.CoinPacks)
            {
                var sku = p.sku;
                if (p.coins < missing && sku != Skus.Coins5) continue;
                Ui.Button(card, $"{Fmt.N(p.coins)} coin · {app.Iap.LocalizedPrice(sku)}", () =>
                {
                    app.Nav.Close(card);
                    app.Buy(sku, "booster", after);
                }, Theme.Panel, Theme.Ink, 120, 40);
            }
            Ui.Button(card, "Vazgeç", () => { app.Nav.Close(card); after?.Invoke(); }, Theme.Neutral, Theme.Ink);
        }
    }
}
