using System;
using Kilim.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Kilim.Game
{
    /// <summary>3 · Kazandın: yıldızlar 0,3 sn arayla, desen %, coin; "coin ×2" (bölüm 5+); Devam → Ana (+ kurala göre tam ekran reklam).</summary>
    public sealed class WinScreen : UiScreen
    {
        private readonly LevelRun _run;
        private readonly WinReport _r;
        private Text _coins;
        private Button _double;

        public WinScreen(LevelRun run, WinReport report) { _run = run; _r = report; }

        public override void Build(RectTransform root)
        {
            var v = Ui.VStack(root, 28, 64, TextAnchor.MiddleCenter).Stretch();
            Ui.Label(v, _r.Stars == 3 ? "Muhteşem!" : _r.Stars == 2 ? "Çok güzel!" : "Tamamlandı!", 88, Theme.Primary, TextAnchor.MiddleCenter, true, 140);
            var stars = Ui.HStack(v, 24, 0, TextAnchor.MiddleCenter, 160);
            stars.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            for (int i = 0; i < 3; i++)
            {
                var s = Ui.Icon(stars, Art.Star, 160, Theme.Neutral);
                if (i < _r.Stars)
                {
                    int idx = i;
                    App.Delay(0.3f * (idx + 1), () =>
                    {
                        if (s == null) return;
                        s.color = Theme.Gold;
                        Audio.Play($"star{idx + 1}");
                    });
                }
            }
            Ui.Label(v, $"Desen %{Mathf.RoundToInt((float)_r.PatternPercent * 100)}", 48, Theme.Ink, TextAnchor.MiddleCenter, true);

            var card = Ui.Card(v);
            if (_r.IsDaily)
            {
                Ui.Label(card, _r.DailyCoins > 0 ? $"Günün deseni: +{_r.DailyCoins} coin ve damga" :
                    App.Meta.Daily.StampedToday(App.Profile) ? "Bugünün damgası zaten alındı." : "100 coin ve damga için 3★ gerekli.", 40);
            }
            else
            {
                _coins = Ui.Label(card, $"+{_r.Coins} coin", 52, Theme.Ink, TextAnchor.MiddleCenter, true);
                var cfg = App.Config.Economy;
                Ui.Label(card, $"{cfg.WinBase} + {cfg.WinPerExtraStar} × ({_r.Stars}★ − 1){(L.Boss ? $" + {cfg.BossBonus}" : "")} = {_r.Coins} coin", 32, Theme.Muted);
                if (_r.Ilmek > 0) Ui.Label(card, $"+{_r.Ilmek} ilmek", 40, Theme.Ink);
                if (_r.Piggy > 0) Ui.Label(card, $"Kumbaraya +{_r.Piggy}", 34, Theme.Muted);
                if (_r.RaceFlag) Ui.Label(card, "Tezgâh Yarışı: +1 bayrak", 34, Theme.Ok);
                if (_r.PassLevelAfter > _r.PassLevelBefore) Ui.Label(card, $"Sezon kartı seviye {_r.PassLevelAfter}!", 34, Theme.Ok);
            }

            if (!_r.IsDaily && _r.CanDoubleCoins && !_r.Doubled)
            {
                _double = Ui.Button(v, "Reklam izle · coin ×2", () =>
                    App.ShowRewarded("win_double", () =>
                    {
                        if (App.Meta.DoubleWinCoins(_run))
                        {
                            App.Save();
                            _coins.text = Loc.T($"+{_r.Coins * 2} coin");
                            Audio.Play("coin");
                            _double.gameObject.SetActive(false);
                        }
                    }), Theme.Gold, Theme.Ink);
            }
            Ui.Button(v, "Devam", () =>
            {
                if (_r.IsDaily) App.Nav.Show(new DailyScreen());
                else App.LeaveWinToHome(L.Id);
            });
        }

        private LevelData L => _run.Level;
        public override bool OnBack() { if (!_r.IsDaily) App.LeaveWinToHome(L.Id); else App.GoHome(); return true; }
    }

    /// <summary>
    /// 4 + 10 · Tezgâh doldu → devam merdiveni (Spec 2 §3). Etik: "Pes et" her zaman görünür ve eşit boyda.
    /// </summary>
    public static class FailFlow
    {
        public static void Show(LevelRun run, GameScreen game)
        {
            var app = App.I;
            var ladder = run.Ladder;
            var wallet = app.Meta.Wallet;
            int price = ladder.CurrentPrice;
            int step = ladder.ContinuesTaken + 1;
            app.Analytics.Track("continue_offer", ("level", run.Level.Id), ("step", step), ("price", price));

            var card = app.Nav.Modal();
            Ui.Label(card, "Tezgâh doldu", 60, Theme.Ink, TextAnchor.MiddleCenter, true);
            if (ladder.IsLastChance) Ui.Label(card, "Son şans", 36, Theme.Primary, TextAnchor.MiddleCenter, true);
            Ui.Label(card, $"Kalan {run.Session.RemainingPieces} parçanın hiçbiri boş yerlere sığmıyor. Devam etmek ister misin?", 38, Theme.Muted, TextAnchor.MiddleCenter, false, 120);
            Ui.Label(card, "+2 Tek İlmek ve +1 Değiştir eklenir, tahta olduğu gibi kalır.", 34, Theme.Ink, TextAnchor.MiddleCenter, false, 100);

            void Done(string how)
            {
                app.Analytics.Track("continue_result", ("level", run.Level.Id), ("step", step), ("price", price), ("result", how));
                app.Save();
                app.Nav.Close(card);
                game.Resume();
            }

            if (wallet.Coins >= price)
            {
                Ui.Button(card, $"Devam · ● {Fmt.N(price)}", () =>
                {
                    if (ladder.TryContinueWithCoins(wallet, run.Session)) Done("coin");
                });
            }
            else
            {
                string sku = Skus.SmallestPackCovering(price - wallet.Coins);
                Ui.Button(card, $"Devam · {app.Iap.LocalizedPrice(sku)}", () =>
                    app.Buy(sku, "continue", () =>
                    {
                        if (ladder.TryContinueWithCoins(wallet, run.Session)) Done("iap");
                    }));
                Ui.Label(card, $"Coin {Fmt.N(wallet.Coins)} / {Fmt.N(price)} · {Fmt.N(Skus.CoinsOf(sku))} coin paketi alınır, {Fmt.N(price)} harcanır", 30, Theme.Muted);
            }

            if (app.Config.ContinueAdEnabled && ladder.CanUseAd && app.Meta.Ads.CanShowRewarded(app.Profile))
            {
                Ui.Button(card, "Reklam izle · ücretsiz devam", () =>
                    app.ShowRewarded("continue", () =>
                    {
                        if (ladder.ContinueWithRewardedAd(wallet, run.Session)) { app.Meta.Ads.OnRewardedEarned(app.Profile); Done("ad"); }
                    }), Theme.Gold, Theme.Ink);
            }

            Ui.Button(card, "Pes et", () => { app.Nav.Close(card); GiveUpConfirm(run, game); }, Theme.Neutral, Theme.Ink);
        }

        /// <summary>10.3 "Bölümü yeniden başlat?" — ardışık 3. kayıpta ücretsiz 1 Geri Al hediyesi.</summary>
        private static void GiveUpConfirm(LevelRun run, GameScreen game)
        {
            var app = App.I;
            bool gift = app.Meta.WillGiftUndoOnGiveUp(run);
            app.Nav.Confirm("Bölümü yeniden başlat?", gift ? "Zor bir bölüm! Sana 1 Geri Al hediye ediyoruz." : null,
                "Yeniden başlat", () =>
                {
                    app.Meta.GiveUp(run);
                    app.Analytics.Track("continue_result", ("level", run.Level.Id), ("step", run.Ladder.ContinuesTaken + 1), ("price", run.Ladder.CurrentPrice), ("result", "giveup"));
                    app.Save();
                    app.StartLevel(run.Level.Id);
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
            Ui.Button(card, "Vazgeç", () => app.Nav.Close(card), Theme.Neutral, Theme.Ink);
        }
    }
}
