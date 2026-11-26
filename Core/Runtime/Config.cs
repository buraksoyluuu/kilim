using System.Collections.Generic;

namespace Kilim.Core
{
    /// <summary>
    /// Remote config (Spec 3 §3). Varsayılanlar spec değerleridir; uzaktan gelen JSON yalnızca verilen anahtarları ezer.
    /// </summary>
    public sealed class GameConfig
    {
        public EconomyConfig Economy { get; } = new EconomyConfig();
        public LivesConfig Lives { get; } = new LivesConfig();

        public bool ContinueAdEnabled = true;
        public int InterFromLevel = 12;
        public int InterGapLevels = 2;
        public int InterGapSeconds = 90;
        public int InterTeenGapLevels = 3;
        public int InterNoAfterPurchaseHours = 72;
        public int RewardedDailyCap = 20;
        public int RewardedGapSeconds = 30;
        public int StarterPackLevel = 8;
        public int StarterPackHours = 48;
        public int NoAdsOfferAfterInterstitials = 3;
        public int WinDoubleFromLevel = 5;
        public int TutorialLastLevel = 4;
        public bool RaceEnabled = true;
        public string SeasonId = "s7-fas";
        public int DailyPatternReward = 100;
        public int TeenMonthlySpendNoticeUsd = 100;

        public static GameConfig Default => new GameConfig();

        /// <summary>Uzak JSON'u uygular. Bilinmeyen anahtarlar yok sayılır; hatalı JSON varsayılanları bozmaz.</summary>
        public void Apply(string json)
        {
            Dictionary<string, object> d;
            try { d = MiniJson.Parse(json) as Dictionary<string, object>; }
            catch (System.FormatException) { return; }
            if (d == null) return;

            var lv = Lives;
            lv.Max = Data.Int(d, "lives_max", lv.Max);
            lv.RefillHours = Data.Double(d, "lives_refill_hours", lv.RefillHours);
            lv.RefillCoins = Data.Int(d, "lives_refill_coins", lv.RefillCoins);

            var e = Economy;
            e.WinBase = Data.Int(d, "win_base", e.WinBase);
            e.WinPerExtraStar = Data.Int(d, "win_star_bonus", e.WinPerExtraStar);
            e.BossBonus = Data.Int(d, "boss_bonus", e.BossBonus);
            e.ContinueLadder = Data.IntArray(d, "continue_prices", e.ContinueLadder);
            e.PiggyPerWin = Data.Int(d, "piggy_per_win", e.PiggyPerWin);
            e.PiggyCap = Data.Int(d, "piggy_cap", e.PiggyCap);
            var bp = Data.Dict(d, "booster_prices");
            e.BoosterPrice[Booster.Undo] = Data.Int(bp, "undo", e.BoosterPrice[Booster.Undo]);
            e.BoosterPrice[Booster.Swap] = Data.Int(bp, "swap", e.BoosterPrice[Booster.Swap]);
            e.BoosterPrice[Booster.SingleStitch] = Data.Int(bp, "single", e.BoosterPrice[Booster.SingleStitch]);

            ContinueAdEnabled = Data.Bool(d, "continue_ad_enabled", ContinueAdEnabled);
            InterFromLevel = Data.Int(d, "inter_from_level", InterFromLevel);
            InterGapLevels = Data.Int(d, "inter_gap_levels", InterGapLevels);
            InterGapSeconds = Data.Int(d, "inter_gap_s", InterGapSeconds);
            InterTeenGapLevels = Data.Int(d, "inter_teen_gap_levels", InterTeenGapLevels);
            RewardedDailyCap = Data.Int(d, "rewarded_daily_cap", RewardedDailyCap);
            StarterPackLevel = Data.Int(d, "starter_pack_level", StarterPackLevel);
            RaceEnabled = Data.Bool(d, "race_enabled", RaceEnabled);
            SeasonId = Data.Str(d, "season_id", SeasonId);
        }
    }

    /// <summary>Mağaza ürünleri (Spec 2 §3). Fiyatlar mağazadan yerel para birimiyle gelir; buradaki USD yedek gösterimdir.</summary>
    public static class Skus
    {
        public const string Coins1 = "kilim.coins.1000";
        public const string Coins2 = "kilim.coins.2500";
        public const string Coins3 = "kilim.coins.6000";
        public const string Coins4 = "kilim.coins.14000";
        public const string Coins5 = "kilim.coins.32000";
        public const string NoAds = "kilim.noads";
        public const string Starter = "kilim.starter";
        public const string Piggy = "kilim.piggy";
        public const string GoldPass = "kilim.goldpass";
        /// <summary>Canları hemen doldur (tüketilebilir).</summary>
        public const string Lives = "kilim.lives";

        public static readonly (string sku, int coins, string usd, string badge)[] CoinPacks =
        {
            (Coins1, 1000, "0,99 $", null),
            (Coins2, 2500, "2,49 $", null),
            (Coins3, 6000, "4,99 $", "En popüler"),
            (Coins4, 14000, "9,99 $", null),
            (Coins5, 32000, "19,99 $", "En avantajlı"),
        };

        public static bool IsConsumable(string sku) => sku != NoAds && sku != Starter && sku != GoldPass;

        public static string UsdPrice(string sku)
        {
            foreach (var p in CoinPacks) if (p.sku == sku) return p.usd;
            switch (sku)
            {
                case NoAds: return "4,99 $";
                case Starter: return "1,99 $";
                case Piggy: return "2,99 $";
                case GoldPass: return "4,99 $";
                case Lives: return "0,99 $";
                default: return "";
            }
        }

        /// <summary>Coin yetmediğinde gösterilecek en küçük uygun coin paketi (Spec 2 §3).</summary>
        public static string SmallestPackCovering(int missingCoins)
        {
            foreach (var p in CoinPacks) if (p.coins >= missingCoins) return p.sku;
            return Coins5;
        }

        public static int CoinsOf(string sku)
        {
            foreach (var p in CoinPacks) if (p.sku == sku) return p.coins;
            return 0;
        }
    }
}
