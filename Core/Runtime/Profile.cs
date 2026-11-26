using System;
using System.Collections.Generic;
using System.Linq;

namespace Kilim.Core
{
    public enum AgeBand { Unknown, Blocked, Teen, Adult }

    /// <summary>
    /// Oyuncunun kalıcı durumu (tek kayıt dosyası). Kazanma ödülü tek işlemde yazılır (UAT WIN-08):
    /// sonuç uygulanır → Save() çağrılır; ödül animasyonu yalnızca görseldir.
    /// </summary>
    public sealed class PlayerProfile
    {
        public const int Version = 1;

        public string PlayerId = Guid.NewGuid().ToString("N");
        public Wallet Wallet = new Wallet(EconomyConfig.Default.StartingCoins);

        // Yaş ve rıza (Spec 2 §5)
        public int BirthYear;
        public AgeBand Age = AgeBand.Unknown;
        public bool ConsentDone;
        public bool AnalyticsConsent;     // KVKK açık rıza, varsayılan kapalı
        public bool AttAsked;
        public bool UmpDone;

        // İlerleme
        public int CurrentLevel = 1;      // sıradaki oynanacak bölüm
        public int MaxLevelReached = 1;
        public readonly Dictionary<int, int> Attempts = new Dictionary<int, int>();
        public readonly HashSet<int> FreeUndoGiftGiven = new HashSet<int>();
        public int ConsecutiveFails;

        // Ayarlar
        public bool Music = true, Sfx = true, Haptics = true, LeftHanded, DailyNotification;
        /// <summary>Kolay mod: desen ipucu her bölümde tahtada (HintPolicy).</summary>
        public bool EasyMode;
        /// <summary>Can sayısı ve yenilenme sayacının başladığı an (UTC tick, 0 = dolu).</summary>
        public int Lives = 3;
        public long LivesSinceUtc;
        /// <summary>Tanıtım hikâyesi görüldü.</summary>
        public bool IntroSeen;
        /// <summary>Açılan mekânların giriş kartı gösterildi (House.Id).</summary>
        public readonly HashSet<string> HousesIntroduced = new HashSet<string>();
        public string Language = "tr";

        // Reklam ve satın alma
        public bool NoAds;
        public long NoAdsUntilMs;         // başlangıç paketi 24 saat reklamsız
        public long LastPurchaseMs;
        public long LastInterstitialMs;
        public int LevelsSinceInterstitial;
        public int InterstitialCount;
        public bool NoAdsOfferShown;
        public string RewardedDay = "";
        public int RewardedToday;
        public long LastRewardedMs;
        public bool FirstSessionDone;
        public readonly HashSet<string> OwnedNonConsumables = new HashSet<string>();
        public readonly HashSet<string> ProcessedTransactions = new HashSet<string>();
        public readonly Dictionary<string, int> GrantedCoinsBySku = new Dictionary<string, int>();
        public string SpendMonth = "";
        public int SpendMonthUsdCents;

        // Teklifler
        public long StarterOfferStartMs;
        public bool StarterBought;

        // Günlük desen
        public readonly HashSet<string> DailyStamps = new HashSet<string>();   // "yyyy-MM-dd"
        public int DailyStreak;
        public string LastDailyDay = "";
        public readonly HashSet<string> StreakBadges = new HashSet<string>();
        public string DailyChestDay = "";

        // Yarış
        public string RaceWeek = "";
        public int RaceFlags;
        public long RaceLastFlagMs;
        public string RaceNickname = "";
        public string RaceRewardClaimedWeek = "";

        // Sezon kartı
        public string SeasonId = "";
        public int SeasonSpools;
        public bool GoldPass;
        public readonly HashSet<int> PassClaimedFree = new HashSet<int>();
        public readonly HashSet<int> PassClaimedGold = new HashSet<int>();

        // Ev meta
        public readonly HashSet<string> HomeTasksDone = new HashSet<string>();
        public readonly Dictionary<string, string> HomeStyles = new Dictionary<string, string>();
        public readonly HashSet<string> Cosmetics = new HashSet<string>();

        public bool IsNoAdsActive(DateTime utcNow) => NoAds || Data.ToMs(utcNow) < NoAdsUntilMs;

        public int AttemptsOf(int level) => Attempts.TryGetValue(level, out var a) ? a : 0;

        // ---------- Kayıt ----------

        public string ToJson()
        {
            var d = new Dictionary<string, object>
            {
                { "v", Version }, { "id", PlayerId }, { "wallet", Wallet.Save() },
                { "birthYear", BirthYear }, { "age", Age.ToString() }, { "consentDone", ConsentDone },
                { "analytics", AnalyticsConsent }, { "attAsked", AttAsked }, { "umpDone", UmpDone },
                { "level", CurrentLevel }, { "maxLevel", MaxLevelReached },
                { "attempts", Attempts.ToDictionary(k => k.Key.ToString(), k => (object)k.Value) },
                { "undoGift", FreeUndoGiftGiven.Cast<object>().ToList() }, { "consecFails", ConsecutiveFails },
                { "music", Music }, { "sfx", Sfx }, { "haptics", Haptics }, { "leftHanded", LeftHanded },
                { "dailyNotif", DailyNotification }, { "lang", Language },
                { "easy", EasyMode }, { "lives", Lives }, { "livesSince", LivesSinceUtc }, { "intro", IntroSeen }, { "housesIntro", HousesIntroduced.Cast<object>().ToList() },
                { "noAds", NoAds }, { "noAdsUntil", NoAdsUntilMs }, { "lastPurchase", LastPurchaseMs },
                { "lastInter", LastInterstitialMs }, { "levelsSinceInter", LevelsSinceInterstitial },
                { "interCount", InterstitialCount }, { "noAdsOfferShown", NoAdsOfferShown },
                { "rewardedDay", RewardedDay }, { "rewardedToday", RewardedToday }, { "lastRewarded", LastRewardedMs },
                { "firstSessionDone", FirstSessionDone },
                { "owned", OwnedNonConsumables.Cast<object>().ToList() },
                { "tx", ProcessedTransactions.Cast<object>().ToList() },
                { "grantedBySku", GrantedCoinsBySku.ToDictionary(k => k.Key, k => (object)k.Value) },
                { "spendMonth", SpendMonth }, { "spendCents", SpendMonthUsdCents },
                { "starterStart", StarterOfferStartMs }, { "starterBought", StarterBought },
                { "dailyStamps", DailyStamps.Cast<object>().ToList() }, { "dailyStreak", DailyStreak },
                { "lastDaily", LastDailyDay }, { "badges", StreakBadges.Cast<object>().ToList() },
                { "chestDay", DailyChestDay },
                { "raceWeek", RaceWeek }, { "raceFlags", RaceFlags }, { "raceLastFlag", RaceLastFlagMs },
                { "nick", RaceNickname }, { "raceClaimed", RaceRewardClaimedWeek },
                { "season", SeasonId }, { "spools", SeasonSpools }, { "gold", GoldPass },
                { "passFree", PassClaimedFree.Cast<object>().ToList() },
                { "passGold", PassClaimedGold.Cast<object>().ToList() },
                { "homeDone", HomeTasksDone.Cast<object>().ToList() },
                { "homeStyles", HomeStyles.ToDictionary(k => k.Key, k => (object)k.Value) },
                { "cosmetics", Cosmetics.Cast<object>().ToList() },
            };
            return MiniJson.Serialize(d);
        }

        /// <summary>Kaydı okur. Bozuk kayıt null döner (çağıran yeni profil başlatır ve hatayı loglar).</summary>
        public static PlayerProfile FromJson(string json)
        {
            Dictionary<string, object> d;
            try { d = MiniJson.Parse(json) as Dictionary<string, object>; }
            catch (FormatException) { return null; }
            if (d == null) return null;

            var p = new PlayerProfile
            {
                PlayerId = Data.Str(d, "id", Guid.NewGuid().ToString("N")),
                Wallet = Wallet.Load(Data.Dict(d, "wallet")),
                BirthYear = Data.Int(d, "birthYear"),
                Age = Enum.TryParse<AgeBand>(Data.Str(d, "age", "Unknown"), out var a) ? a : AgeBand.Unknown,
                ConsentDone = Data.Bool(d, "consentDone"),
                AnalyticsConsent = Data.Bool(d, "analytics"),
                AttAsked = Data.Bool(d, "attAsked"),
                UmpDone = Data.Bool(d, "umpDone"),
                CurrentLevel = Math.Max(1, Data.Int(d, "level", 1)),
                MaxLevelReached = Math.Max(1, Data.Int(d, "maxLevel", 1)),
                ConsecutiveFails = Data.Int(d, "consecFails"),
                Music = Data.Bool(d, "music", true), Sfx = Data.Bool(d, "sfx", true),
                Haptics = Data.Bool(d, "haptics", true), LeftHanded = Data.Bool(d, "leftHanded"),
                DailyNotification = Data.Bool(d, "dailyNotif"), Language = Data.Str(d, "lang", "tr"),
                EasyMode = Data.Bool(d, "easy"), Lives = Data.Int(d, "lives", 3), LivesSinceUtc = Data.Long(d, "livesSince"), IntroSeen = Data.Bool(d, "intro"),
                NoAds = Data.Bool(d, "noAds"), NoAdsUntilMs = Data.Long(d, "noAdsUntil"),
                LastPurchaseMs = Data.Long(d, "lastPurchase"), LastInterstitialMs = Data.Long(d, "lastInter"),
                LevelsSinceInterstitial = Data.Int(d, "levelsSinceInter"), InterstitialCount = Data.Int(d, "interCount"),
                NoAdsOfferShown = Data.Bool(d, "noAdsOfferShown"),
                RewardedDay = Data.Str(d, "rewardedDay", ""), RewardedToday = Data.Int(d, "rewardedToday"),
                LastRewardedMs = Data.Long(d, "lastRewarded"), FirstSessionDone = Data.Bool(d, "firstSessionDone"),
                SpendMonth = Data.Str(d, "spendMonth", ""), SpendMonthUsdCents = Data.Int(d, "spendCents"),
                StarterOfferStartMs = Data.Long(d, "starterStart"), StarterBought = Data.Bool(d, "starterBought"),
                DailyStreak = Data.Int(d, "dailyStreak"), LastDailyDay = Data.Str(d, "lastDaily", ""),
                DailyChestDay = Data.Str(d, "chestDay", ""),
                RaceWeek = Data.Str(d, "raceWeek", ""), RaceFlags = Data.Int(d, "raceFlags"),
                RaceLastFlagMs = Data.Long(d, "raceLastFlag"), RaceNickname = Data.Str(d, "nick", ""),
                RaceRewardClaimedWeek = Data.Str(d, "raceClaimed", ""),
                SeasonId = Data.Str(d, "season", ""), SeasonSpools = Data.Int(d, "spools"), GoldPass = Data.Bool(d, "gold"),
            };
            foreach (var kv in Data.Dict(d, "attempts")) p.Attempts[int.Parse(kv.Key)] = Convert.ToInt32(kv.Value);
            foreach (var kv in Data.Dict(d, "grantedBySku")) p.GrantedCoinsBySku[kv.Key] = Convert.ToInt32(kv.Value);
            foreach (var kv in Data.Dict(d, "homeStyles")) p.HomeStyles[kv.Key] = (string)kv.Value;
            AddInts(p.FreeUndoGiftGiven, Data.List(d, "undoGift"));
            AddInts(p.PassClaimedFree, Data.List(d, "passFree"));
            AddInts(p.PassClaimedGold, Data.List(d, "passGold"));
            AddStrs(p.OwnedNonConsumables, Data.List(d, "owned"));
            AddStrs(p.ProcessedTransactions, Data.List(d, "tx"));
            AddStrs(p.DailyStamps, Data.List(d, "dailyStamps"));
            AddStrs(p.StreakBadges, Data.List(d, "badges"));
            AddStrs(p.HomeTasksDone, Data.List(d, "homeDone"));
            AddStrs(p.Cosmetics, Data.List(d, "cosmetics"));
            AddStrs(p.HousesIntroduced, Data.List(d, "housesIntro"));
            return p;
        }

        private static void AddInts(HashSet<int> set, List<object> l) { foreach (var o in l) set.Add(Convert.ToInt32(o)); }
        private static void AddStrs(HashSet<string> set, List<object> l) { foreach (var o in l) if (o is string s) set.Add(s); }

        /// <summary>
        /// Bulut çakışması (UAT SAVE-03): daha ileri bölümdeki kayıt seçilir; coin kaybolmasın diye
        /// coin, satın alınan kalıcı ürünler ve işlenmiş işlemler birleştirilir.
        /// </summary>
        public static PlayerProfile Merge(PlayerProfile a, PlayerProfile b)
        {
            var main = a.MaxLevelReached >= b.MaxLevelReached ? a : b;
            var other = ReferenceEquals(main, a) ? b : a;
            if (other.Wallet.Coins > main.Wallet.Coins)
                main.Wallet.AddCoins(other.Wallet.Coins - main.Wallet.Coins);
            foreach (var s in other.OwnedNonConsumables) main.OwnedNonConsumables.Add(s);
            foreach (var t in other.ProcessedTransactions) main.ProcessedTransactions.Add(t);
            main.NoAds |= other.NoAds;
            main.GoldPass |= other.GoldPass && other.SeasonId == main.SeasonId;
            return main;
        }
    }

    /// <summary>Yaş kapısı (Spec 2 §5, Spec 3 8.2).</summary>
    public static class AgePolicy
    {
        public const int MinYear = 1950;

        public static AgeBand BandFor(int birthYear, DateTime today)
        {
            if (birthYear < MinYear || birthYear > today.Year) return AgeBand.Unknown;
            // Yalnızca yıl soruluyor: yaş = bu yıl − doğum yılı (gün bilinmediği için muhafazakâr: −1).
            int age = today.Year - birthYear - 1;
            if (age < 13) return AgeBand.Blocked;
            if (age < 18) return AgeBand.Teen;
            return AgeBand.Adult;
        }

        public static string AnalyticsBand(AgeBand b) =>
            b == AgeBand.Teen ? "13_17" : b == AgeBand.Adult ? "18p" : "blocked";
    }
}
