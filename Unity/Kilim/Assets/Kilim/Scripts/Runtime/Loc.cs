using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Kilim.Game
{
    /// <summary>
    /// Yerelleştirme. Kaynak dil Türkçe; anahtar = Türkçe metnin kendisi. Değişken içeren metinler {0},{1}… kalıbıyla
    /// eşleşir ve yakalanan parçalar da (ör. "Değiştir") ayrıca çevrilir. Eksik çeviri editörde bir kez uyarı verir.
    /// Yeni dil: <see cref="Tables"/>'a sözlük ekle, Ayarlar'daki dil listesine kodu ekle.
    /// </summary>
    public static class Loc
    {
        public static string Lang { get; private set; } = "tr";
        public static CultureInfo Culture { get; private set; } = new CultureInfo("tr-TR");
        public static readonly string[] Supported = { "tr", "en" };

        private static readonly Dictionary<string, Dictionary<string, string>> Tables = new Dictionary<string, Dictionary<string, string>>
        {
            { "en", En.Table },
        };

        private static readonly Dictionary<string, List<(Regex rx, string target)>> Templates = new Dictionary<string, List<(Regex, string)>>();
        private static readonly HashSet<string> Missing = new HashSet<string>();
        private static readonly Regex Placeholder = new Regex(@"\{\d+\}");
        private static readonly Regex Numberish = new Regex(@"^[\d\s.,:%+=\-−×/★✦●⚑…()]*$");

        public static string SystemDefault() => Application.systemLanguage == SystemLanguage.Turkish ? "tr" : "en";

        public static void Set(string lang)
        {
            Lang = System.Array.IndexOf(Supported, lang) >= 0 ? lang : "en";
            Culture = new CultureInfo(Lang == "tr" ? "tr-TR" : "en-US");
            if (Lang != "tr" && !Templates.ContainsKey(Lang)) BuildTemplates(Lang);
        }

        private static void BuildTemplates(string lang)
        {
            var list = new List<(Regex, string)>();
            var keys = new List<string>();
            foreach (var k in Tables[lang].Keys) if (Placeholder.IsMatch(k)) keys.Add(k);
            keys.Sort((a, b) => Placeholder.Replace(b, "").Length.CompareTo(Placeholder.Replace(a, "").Length)); // en özgül önce
            foreach (var k in keys)
            {
                var pattern = "^" + Placeholder.Replace(Regex.Escape(k).Replace(@"\{", "{"), "(.+?)") + "$";
                pattern = pattern.Replace(@"\n", "\n");
                list.Add((new Regex(pattern, RegexOptions.Singleline), Tables[lang][k]));
            }
            Templates[lang] = list;
        }

        public static string T(string s)
        {
            if (Lang == "tr" || string.IsNullOrEmpty(s) || Numberish.IsMatch(s)) return s;
            var table = Tables[Lang];
            if (table.TryGetValue(s, out var exact)) return exact;
            foreach (var (rx, target) in Templates[Lang])
            {
                var m = rx.Match(s);
                if (!m.Success) continue;
                var args = new object[m.Groups.Count - 1];
                for (int i = 1; i < m.Groups.Count; i++) args[i - 1] = T(m.Groups[i].Value);
                return string.Format(target, args);
            }
            if (Application.isEditor && Missing.Add(s)) Debug.LogWarning($"[Kilim][Loc] Çeviri yok ({Lang}): {s}");
            return s;
        }

        public static string F(string template, params object[] args) => T(string.Format(template, args));
    }

    /// <summary>İngilizce tablo. Anahtarlar koddaki Türkçe metinlerle birebir aynı olmalı.</summary>
    public static class En
    {
        public static readonly Dictionary<string, string> Table = new Dictionary<string, string>
        {
            // Genel
            // v0.5: his, harita, ev, tanıtım
            { "Kilim tamamlandı!", "Kilim complete!" }, { "Tekrar dene", "Try again" },
            { "Canların bitti", "Out of lives" }, { "{0} canın var", "You have {0} lives" }, { "Canlar doldu!", "Lives refilled!" },
            { "Canların dolu. Kazandığın bölümde can harcanmaz.", "Lives are full. Winning a level doesn't cost a life." },
            { "Canlar {0} sonra dolar", "Lives refill in {0}" }, { "Hepsini doldur · ● {0}", "Refill all · ● {0}" },
            { "Hepsini doldur · {0}", "Refill all · {0}" }, { "Reklam izle · +1 can", "Watch ad · +1 life" },
            { "Kalan {0} parça", "{0} pieces left" }, { "Parça kalmadı", "No pieces left" }, { "Desen kartı", "Pattern card" },
            { "Güzel!", "Nice!" }, { "Harika!", "Great!" }, { "Usta işi!", "Masterful!" }, { "Yeni rekor!", "New record!" },
            { "Tahtada artık ipucu yok. Parçaları bu desene göre yerleştir; eşleşen her hücre yıldıza yaklaştırır.", "No more hints on the board. Place pieces following this pattern; every matching cell gets you closer to a star." },
            { "Tahtada yalnızca şekiller var. Renkleri bu karttan oku.", "Only shapes on the board now. Read the colours from this card." },
            { "Desen kartına dokun: hedef deseni gör.", "Tap the pattern card to see the target." },
            { "Değiştirmek istediğin parçaya dokun.", "Tap the piece you want to swap." }, { "Tek İlmek değiştirilemez.", "A single stitch can't be swapped." },
            { "{0} {1}. bölümde açılır.", "{0} unlocks at level {1}." }, { "Bölüm {0}'da", "At level {0}" }, { "Bölüm {0}'de", "At level {0}" },
            { "Bölüm haritası", "Level map" }, { "Ana menü", "Main menu" }, { "Yeniden başla", "Restart" }, { "Bölümler", "Levels" },
            { "Bölge {0}", "Region {0}" }, { "Önce Bölüm {0}'i bitir.", "Finish level {0} first." }, { "Usta", "Master" },
            { "Usta dokumacı", "Master weaver" }, { "Ezbere dokuma", "Weaving by heart" },
            { "Tahtada artık renkler yok, yalnızca şekiller. Renkleri sağ üstteki desen kartından oku.", "No colours on the board now, only shapes. Read the colours from the pattern card at the top right." },
            { "Bundan sonra tahta boş. Desen kartına bak, deseni aklında tut ve doku!\n(Ayarlar › Kolay mod ipuçlarını geri getirir.)", "From now on the board is empty. Look at the pattern card, remember it and weave!\n(Settings › Easy mode brings hints back.)" },
            { "Yeni: {0}", "New: {0}" }, { "Son yerleşimi geri alır. Tezgâh dolduğunda da kurtarır.", "Undoes your last placement. Also rescues you when the loom is full." },
            { "Seçtiğin parçayı kuyruğun sonuna gönderir, yerine sıradaki gelir.", "Sends the chosen piece to the end of the queue and brings the next one." },
            { "Tezgâha 1 hücrelik ek parça koyar: kalan boşlukları kapatır (desen yüzdesini düşürür).", "Adds a 1-cell piece to the tray to fill leftover holes (lowers the pattern percentage)." },
            { "Parçalar bitti ama {0} boş hücre kaldı.", "Out of pieces but {0} empty cells are left." },
            { "Kalan {0} parçanın hiçbiri boş yerlere sığmıyor.", "None of the {0} remaining pieces fit." },
            { "Güçlendiriciyle kurtar, kaldığın yerden devam et.", "Rescue with a booster and carry on." },
            { "Devam paketi: +2 Tek İlmek ve +1 Değiştir", "Continue pack: +2 Single Stitch and +1 Swap" },
            { "İnternet yok: reklamla devam şu an kullanılamıyor.", "No internet: continuing with an ad isn't available right now." },
            { "Bölümü yeniden başlat?", "Restart the level?" }, { "Ana menüye dön?", "Back to the main menu?" },
            { "Bu denemedeki ilerleme kaybolur.", "Progress in this attempt will be lost." }, { "Yeniden başlat", "Restart" },
            { "Desen %{0}", "Pattern {0}%" }, { "Bölüm {0} ▶", "Level {0} ▶" }, { "Bölüm {0}'e dön", "Back to level {0}" },
            { "Tekrar oyna", "Play again" }, { "Yeni desenler yakında!", "New patterns coming soon!" },
            { "Tekrar oynamada coin yarı, ilmek yalnızca yeni yıldız için.", "Replays pay half coins; knots only for new stars." },
            { "Yeni güçlendirici: {0}", "New booster: {0}" }, { "Sıradaki bölümde 3 tane hediye!", "3 free in the next level!" },
            { "Evde yeni görev yapılabilir!", "A new house task is ready!" },
            { "Günlük", "Daily" }, { "Yarış", "Race" }, { "Sezon", "Season" }, { "Kumbara", "Piggy bank" }, { "Dolu!", "Full!" },
            { "Yeni desen!", "New pattern!" }, { "Seri {0} ✓", "Streak {0} ✓" }, { "Seviye {0}", "Tier {0}" }, { "Cuma", "Friday" },
            { "Harita", "Map" }, { "Ev", "Home" }, { "Mağaza", "Shop" }, { "Yap", "Do it" }, { "Oyna ▶", "Play ▶" },
            { "Başlangıç paketi · 2.000 coin + 3'er güçlendirici", "Starter pack · 2,000 coins + 3 of each booster" },
            { "Bütün evler pırıl pırıl oldu, ellerine sağlık.", "Every house is spotless — well done, dear." },
            { "{0} ilmek daha", "{0} more knots" }, { "Sırada bekliyor", "Waiting its turn" }, { "Başlayalım", "Let's begin" },
            { "{0} tamamlandı!", "{0} complete!" }, { "Sırada: {0}", "Next: {0}" }, { "Hadi gidelim", "Let's go" }, { "Sıradaki alan", "Next area" },
            { "Bütün evleri onardın. Nine seninle gurur duyuyor!", "You've restored every house. Grandma is so proud of you!" },
            { "Nine seni çağırıyor", "Grandma is calling" }, { "İlmek ilmek dokuyacaksın", "Weave knot by knot" },
            { "Desen yıldız getirir", "Patterns earn stars" }, { "Evi birlikte onaralım", "Let's restore the house together" },
            { "Köydeki eski ev yıllardır bekliyor. Nine'nin dokuma tezgâhı tozlanmış, avlunun çeşmesi kurumuş.", "The old village house has waited for years. Grandma's loom is dusty and the courtyard fountain has run dry." },
            { "Renkli parçaları tezgâha yerleştir. Tezgâhı tamamen doldurunca kilim biter.", "Place the coloured pieces on the loom. Fill it completely to finish the kilim." },
            { "Parçanın rengi desendeki izle eşleşirse altın çerçeve parlar. Tam desen = 3 yıldız.", "When a piece matches the pattern a gold frame shines. Full pattern = 3 stars." },
            { "Her yıldız bir ilmek kazandırır. İlmeklerle evi, bağ evini, yaylayı… yeniden canlandır.", "Every star earns a knot. Use knots to bring the house, the vineyard, the highlands… back to life." },
            { "Geç", "Skip" }, { "İleri", "Next" }, { "Başla", "Start" }, { "On yıl", "Decade" }, { "Yıl", "Year" }, { "{0}'ler", "{0}s" },
            { "Deneyimini yaşına göre ayarlamak için doğum yılını seç. Bu bilgi yalnızca cihazında saklanır.", "Select your birth year so we can tailor the experience. It's stored only on your device." },
            { "Kolay mod (ipucu hep tahtada, tıkanınca son hamle geri alınır)", "Easy mode (hints always on the board, last move undone when stuck)" }, { "Hikâyeyi yeniden izle", "Watch the story again" },
            { "Nine'nin Evi", "Grandma's House" }, { "Bağ Evi", "Vineyard House" }, { "Yayla Evi", "Highland House" }, { "Kasaba Konağı", "Town Mansion" },
            { "Devam", "Continue" }, { "Tamam", "OK" }, { "Kapat", "Close" }, { "Vazgeç", "Cancel" }, { "Sil", "Delete" },
            { "Harika", "Great" }, { "Anladım", "Got it" }, { "Oyna", "Play" }, { "İptal", "Cancel" }, { "Geri dön", "Go back" },
            { "Şimdi değil", "Not now" }, { "AÇIK", "ON" }, { "KAPALI", "OFF" }, { "{0}:  {1}", "{0}:  {1}" },
            { "Kilim", "Kilim" }, { "desen desen, ilmek ilmek", "pattern by pattern, knot by knot" },
            { "Kalan {0}", "{0} left" }, { "{0} coin", "{0} coins" }, { "+{0} coin", "+{0} coins" }, { "● {0}", "● {0}" },
            { "{0} coin daha", "{0} more coins" }, { "{0} coin daha gerekli.", "You need {0} more coins." },
            { "{0} coin · {1}", "{0} coins · {1}" }, { "Coin yetmiyor", "Not enough coins" },
            { "Bölüm {0}", "Level {0}" }, { "Bölüm {0} · Usta", "Level {0} · Master" }, { "Günün Deseni", "Pattern of the Day" },

            // Açılış, yaş, rıza
            { "Hoş geldin!", "Welcome!" },
            { "Deneyimini yaşına göre ayarlamak için doğum yılını seç.\nBu bilgi yalnızca cihazında saklanır.", "Select your birth year so we can tailor the experience to your age.\nThis stays on your device." },
            { "Bu oyun 13 yaş ve üzeri içindir", "This game is for ages 13 and up" },
            { "Çocuklar için hazırladığımız Minik Yollar, Pofuduk ve Minik Kasaba oyunlarına göz atabilirsin.", "Check out our games for kids: Minik Yollar, Pofuduk and Minik Kasaba." },
            { "Minik oyunlarımız", "Our kids' games" }, { "Yanlış seçim yaptıysan: Destek", "Picked the wrong year? Contact Support" },
            { "Gizlilik ve rıza", "Privacy & consent" }, { "Zorunlu", "Required" }, { "İsteğe bağlı", "Optional" },
            { "Oyun ilerlemen ve satın alımların cihazında ve bulut kaydında saklanır.", "Your progress and purchases are stored on your device and in cloud save." },
            { "Oyunu geliştirmek için anonim kullanım verisi (analitik).", "Anonymous usage data to improve the game (analytics)." },
            { "Analitiğe izin ver", "Allow analytics" }, { "KVKK aydınlatma metnini oku", "Read the privacy notice" },
            { "Hiçbir seçim oyunu kısıtlamaz.", "None of these choices limit the game." }, { "Aydınlatma metni", "Privacy notice" },
            { "Veri sorumlusu: [Şirket]. İşlenen veriler: oyun ilerlemesi, satın alma kayıtları, (rıza verirsen) anonim kullanım verisi. Amaç: oyunu sunmak ve geliştirmek. Haklarını Ayarlar › Verilerimi sil ve destek e-postası üzerinden kullanabilirsin.",
              "Data controller: [Company]. Data processed: game progress, purchase records and (with your consent) anonymous usage data. Purpose: to provide and improve the game. Exercise your rights via Settings › Delete my data or the support email." },
            { "Reklam deneyimin", "Your ad experience" },
            { "Sana daha alakalı reklamlar gösterebilmemiz için bir sonraki adımda izin istenecek. Reddetmek hiçbir özelliği kapatmaz.", "Next, you'll be asked for permission so we can show you more relevant ads. Declining doesn't turn off any feature." },
            { "Reklam tercihleri (UMP test)", "Ad preferences (UMP test)" }, { "Kişiselleştirilmiş reklamlara izin?", "Allow personalised ads?" },
            { "İzin ver", "Allow" }, { "Reddet", "Decline" },

            // Oyun
            { "Tezgâh doldu", "No room on the loom" }, { "Tezgâh doldu, son parça geri alındı.", "No room left, so your last piece was undone." },
            { "2★", "2★" }, { "3★", "3★" }, { "Önce gösterilen hamleyi yap.", "Make the highlighted move first." },
            { "Geri Al", "Undo" }, { "Değiştir", "Swap" }, { "Tek İlmek", "Single Knot" },
            { "{0}\nBölüm {1}", "{0}\nLevel {1}" }, { "{0}\n● {1}", "{0}\n● {1}" }, { "{0}\n×{1}", "{0}\n×{1}" },
            { "Değiştirilecek parçaya dokun.", "Tap the piece to swap." }, { "Geri alınacak yerleşim yok.", "Nothing to undo." },
            { "Şu an kullanılamaz.", "Can't use this right now." }, { "Duraklatıldı", "Paused" },
            { "Günün deseni kaydedilmez, istediğin zaman yeniden başlayabilirsin.", "Today's pattern isn't saved mid-way; restart any time." },
                        { "Nine: \"Lacivert parçayı aynı renkli izin üstüne koy.\"", "Granny: \"Place the navy piece on the matching outline.\"" },
            { "Nine: \"Bu parçayı aynı renkli izin üstüne koy.\"", "Granny: \"Place this piece on the matching outline.\"" },
            { "Rengi motifle eşleşen parça desen yıldızı kazandırır.", "Pieces matching the motif colour earn pattern stars." },
            { "Parçaya dokun: saat yönünde döner.", "Tap a piece to rotate it clockwise." }, { "Süre yok, kaybetmek yok.", "No timer, no losing." },
            { "{0}\n3 tane hediye!", "{0}\n3 free to start!" },
            { "Son yerleşimi geri alır.", "Undoes your last placement." }, { "Seçtiğin parçayı kuyruğun sonuna gönderir.", "Sends the chosen piece to the back of the queue." },
            { "Tezgâha 1 hücrelik ek parça koyar (desen yüzdesini düşürür).", "Adds a 1-cell piece to the loom (lowers your pattern score)." },

            // Sonuç
            { "Muhteşem!", "Magnificent!" }, { "Çok güzel!", "Lovely!" }, { "Tamamlandı!", "Complete!" },             { "Günün deseni: +{0} coin ve damga", "Pattern of the Day: +{0} coins and a stamp" },
            { "Bugünün damgası zaten alındı.", "You've already earned today's stamp." },
            { "100 coin ve damga için 3★ gerekli.", "Get 3★ for 100 coins and a stamp." },
            { "+{0} ilmek", "+{0} knots" }, { "Kumbaraya +{0}", "Piggy bank +{0}" }, { "Tezgâh Yarışı: +1 bayrak", "Loom Race: +1 flag" },
            { "Sezon kartı seviye {0}!", "Season pass level {0}!" }, { "Reklam izle · coin ×2", "Watch ad · 2× coins" },
            { "Son şans", "Last chance" },
            { "Kalan {0} parçanın hiçbiri boş yerlere sığmıyor. Devam etmek ister misin?", "None of the {0} remaining pieces fit. Want to continue?" },
            { "+2 Tek İlmek ve +1 Değiştir eklenir, tahta olduğu gibi kalır.", "You'll get +2 Single Knots and +1 Swap; the board stays as it is." },
            { "Devam · ● {0}", "Continue · ● {0}" }, { "Devam · {0}", "Continue · {0}" },
            { "Coin {0} / {1} · {2} coin paketi alınır, {3} harcanır", "Coins {0} / {1} · buys the {2}-coin pack and spends {3}" },
            { "Reklam izle · ücretsiz devam", "Watch ad · continue free" }, { "Pes et", "Give up" },
            { "Zor bir bölüm! Sana 1 Geri Al hediye ediyoruz.", "Tough one! Here's a free Undo." },

            // Reklam / satın alma
            { "Şu an reklam yok, birazdan tekrar dene.", "No ad available right now. Try again shortly." },
            { "Reklam tamamlanmadı, ödül verilmedi.", "The ad wasn't finished, so no reward was given." },
            { "Reklamsız oyna", "Play ad-free" },
            { "Tam ekran reklamları kalıcı olarak kapat. Ödüllü reklamlar isteğe bağlı kalır.", "Remove full-screen ads for good. Rewarded ads stay optional." },
            { "Reklamsız · {0}", "Ad-free · {0}" }, { "Bu satın alma gerçek para", "This purchase uses real money" },
            { "{0} ödenecek. Emin misin?", "You'll be charged {0}. Are you sure?" }, { "Evet, satın al", "Yes, buy" },
            { "Ödeme onay bekliyor. Onaylanınca içerik eklenecek.", "Payment pending. Your items will be added once it's approved." },
            { "Satın alma tamamlanamadı. Ücret alınmadı.", "Purchase couldn't be completed. You weren't charged." },
            { "Bekleyen ödeme onaylandı, içerik eklendi.", "Pending payment approved, items added." },
            { "Satın alma tamamlandı.", "Purchase complete." },
            { "Satın alma doğrulanamadı. Destek ile iletişime geç.", "We couldn't verify this purchase. Please contact Support." },
            { "Bu satın alma zaten teslim edilmiş.", "This purchase has already been delivered." }, { "Bilgilendirme", "Heads up" },
            { "Bu ay 100 $ üzerinde harcama yaptın. Harcamalarını bir yetişkinle konuşmanı öneririz.", "You've spent over $100 this month. We suggest talking about your spending with an adult." },
            { "Satın alımlar geri yüklendi.", "Purchases restored." }, { "Geri yüklenecek satın alım bulunamadı.", "No purchases to restore." },
            { "Test reklamı (ödüllü)", "Test ad (rewarded)" }, { "Test reklamı (tam ekran)", "Test ad (interstitial)" },
            { "Yerleşim: {0}\nGerçek cihazda burada reklam oynar.", "Placement: {0}\nA real ad plays here on device." },
            { "Yerleşim: {0}", "Placement: {0}" }, { "Sonuna kadar izle", "Watch to the end" }, { "Erken kapat", "Close early" },
            { "Test satın alma", "Test purchase" }, { "Satın al", "Buy" }, { "Bekleyen ödeme (5 sn)", "Pending payment (5 s)" },

            // Ana ekran ve meta
            { "✦ {0} ilmek", "✦ {0} knots" }, { "Ayarlar", "Settings" },             { "Kumbara\n{0}", "Piggy bank\n{0}" }, { "Kumbara\n{0} · dolu!", "Piggy bank\n{0} · full!" },
            { "Sezon\nSeviye {0}", "Season\nLevel {0}" }, { "Günlük ✓\nSeri {0}", "Daily ✓\nStreak {0}" }, { "Günlük •\nSeri {0}", "Daily •\nStreak {0}" },
            { "Yarış\n{0}", "Race\n{0}" }, { "Yarış\nCuma", "Race\nFriday" },
            { "Başlangıç paketi · bir kez", "Starter pack · one time" },
            { "2.000 coin + 3'er güçlendirici + 24 sa reklamsız · {0}", "2,000 coins + 3 of each booster + 24 h ad-free · {0}" },
            { "2.000 coin + 3'er güçlendirici + 24 sa reklamsız", "2,000 coins + 3 of each booster + 24 h ad-free" },
            { "Nine: \"{0}\"", "Granny: \"{0}\"" }, { "Ev pırıl pırıl oldu, ellerine sağlık.", "The house is spotless. Bless your hands." },
            { "Yeni desenler yakında", "New patterns coming soon" },
            { "Bu arada Günün Deseni ve Tezgâh Yarışı açık.", "Meanwhile, Pattern of the Day and the Loom Race are open." },
            { "Bölüm {0}'i 3★ yap", "Get 3★ on level {0}" }, { "Ev •", "Home •" },             { "Tezgâh Yarışı bitti", "Loom Race finished" }, { "{0}. oldun! +{1} coin", "You placed #{0}! +{1} coins" },
            { "Reklamsız", "Ad-free" }, { "Tam ekran reklamlar kapanır; ödüllü reklamlar isteğe bağlı kalır.", "Full-screen ads turn off; rewarded ads stay optional." },
            { "En popüler", "Most popular" }, { "En avantajlı", "Best value" },
            { "Satın alımları geri yükle", "Restore purchases" }, { "Kullanım koşulları · Gizlilik", "Terms of use · Privacy" },
            { "Ganimet kutusu ve rastgele ödüllü satın alma yoktur.", "No loot boxes or randomised paid rewards." },
            { "Kazandığın her bölüm kumbaraya {0} coin atar. Dolunca {1} ile kırıp hepsini alabilirsin.", "Every level you win drops {0} coins in. When it's full, break it for {1} and keep them all." },
            { "Kır · {0}", "Break · {0}" }, { "Sezon {0} · Fas", "Season {0} · Morocco" },
            { "Seviye {0} / {1} · Makara {2}/{3} · {4} kaldı", "Level {0} / {1} · Spools {2}/{3} · {4} left" },
            { "Her kazanılan bölüm 1 makara, 10 makara 1 seviye. Sezon bitince alınmamış ödüller kaybolur.", "Each level won = 1 spool, 10 spools = 1 tier. Unclaimed rewards expire at season end." },
            { "Altın yolu aç · {0}", "Unlock gold track · {0}" }, { "Altın: {0}", "Gold: {0}" }, { "Altın: {0} ✓", "Gold: {0} ✓" }, { "{0} ✓", "{0} ✓" },
            { "Herkese aynı desen. Süre yok, kaybetmek yok: tezgâh dolarsa son parça kendiliğinden geri alınır.", "Same pattern for everyone. No timer, no losing: if the loom fills up, your last piece is undone." },
            { "{0} · Seri: {1} gün", "{0} · Streak: {1} days" }, { "Ödül: 3★'da 100 coin + damga · 7 gün seri = rozet", "Reward: 100 coins + a stamp for 3★ · 7-day streak = badge" },
            { "Bugünün desenini yeniden doku", "Weave today's pattern again" }, { "Bugünün desenini doku", "Weave today's pattern" },
            { "Günlük sandık: 50", "Daily chest: 50" }, { "Reklam izle · ×2", "Watch ad · ×2" }, { "Sandık yarın yine burada.", "The chest is back tomorrow." },
            { "Tezgâh Yarışı", "Loom Race" },
            { "İlk denemede kazandığın her bölüm 1 bayrak. 30 kişilik grubunda ilk 3'e ödül.", "Every level won on the first try = 1 flag. Top 3 in your group of 30 win prizes." },
            { "1. 1.500 coin + çerçeve · 2. 800 · 3. 400", "1st 1,500 coins + frame · 2nd 800 · 3rd 400" },
            { "Yarış Cuma 10:00'da başlar, Pazar 22:00'de biter.", "The race starts Friday 10:00 and ends Sunday 22:00." },
            { "Takma adını seç", "Choose your nickname" }, { "{0} (sen)", "{0} (you)" },
            { "Grup eşleşmesi sunucuda; çevrimdışı oynanan bölümler bağlantı gelince sayılır.", "Groups are matched on the server; levels played offline count once you reconnect." },
            { "{0} · {1}/4", "{0} · {1}/4" }, { "Tamam ✓", "Done ✓" },
            { "{0} ilmek · Yap", "{0} knots · Do it" }, { "{0} ilmek · sırada bekliyor", "{0} knots · up next" },
            { "Güzel olmuş", "Looks lovely" }, { "Stil seç", "Choose a style" }, { "Sonra ücretsiz değiştirebilirsin.", "You can change it later for free." },

            // Ayarlar
            { "Bildirimler", "Notifications" }, { "İpucu", "Hint" }, { "Dokun → döner ↻", "Tap → rotate ↻" }, { "Tezgâh tıkandı, son parçayı geri aldık. Parçaya dokunup döndürmeyi dene!", "The loom got stuck, we undid your last piece. Tap a piece to rotate it!" }, { "İpucu için {0} coin gerekli.", "You need {0} coins for a hint." },
            { "Önce bir hamleyi geri al, sonra ipucu iste.", "Undo a move first, then ask for a hint." }, { "Parlayan parçayı işaretli yere koy (gerekirse döndür).", "Place the glowing piece on the marked spot (rotate if needed)." }, { "Can, günlük desen ve yarış hatırlatmaları", "Lives, daily pattern and race reminders" },
            { "Canların doldu!", "Your lives are full!" }, { "Nine tezgâhta seni bekliyor, bir kilim daha dokuyalım.", "Grandma is waiting at the loom — let's weave another kilim." },
            { "Günün deseni hazır", "Today's pattern is ready" }, { "Bugünün kilim deseni seni bekliyor.", "Today's kilim pattern is waiting for you." },
            { "{0} günlük serin bozulmasın!", "Don't break your {0}-day streak!" }, { "Günün desenini dokuyarak serini koru.", "Weave today's pattern to keep your streak." },
            { "Yarış bitmek üzere!", "The race is ending soon!" }, { "Son saatler: bir bayrak daha kap, sıranı yükselt.", "Final hours: grab another flag and climb the ranks." },
            { "Kumbaran doldu", "Your piggy bank is full" }, { "Biriken altınlar kırılmayı bekliyor.", "Your saved coins are ready to collect." },
            { "Avlunun çeşmesi seni bekliyor", "The courtyard fountain misses you" }, { "Nine'nin evi seni özledi, kaldığın yerden devam et.", "Grandma's house misses you — pick up where you left off." },
            { "Tezgâh tozlanmasın", "Don't let the loom gather dust" }, { "Yeni desenler ve ödüller seni bekliyor.", "New patterns and rewards are waiting." },
            { "Müzik", "Music" }, { "Efektler", "Sound effects" }, { "Titreşim", "Vibration" }, { "Sol el modu", "Left-handed mode" },
            { "Günlük desen bildirimi", "Daily pattern reminder" }, { "Dil: Türkçe", "Language: Turkish" }, { "Dil: English", "Language: English" },
            { "Bulut kayıt (Game Center / Play Games)", "Cloud save (Game Center / Play Games)" },
            { "Bulut kayıt bu sürümde test modunda.", "Cloud save is in test mode in this build." }, { "Destek", "Support" }, { "Gizlilik", "Privacy" },
            { "Analitik (KVKK açık rıza)", "Analytics (explicit consent)" }, { "Reklam gizlilik seçenekleri", "Ad privacy options" }, { "Gizlilik politikası", "Privacy policy" },
            { "Verilerimi sil", "Delete my data" }, { "Verilerin silinsin mi?", "Delete your data?" },
            { "İlerlemen, coin'lerin ve ayarların bu cihazdan ve sunucudan (30 gün içinde) silinir. Satın alımlar mağazadan geri yüklenebilir.",
              "Your progress, coins and settings will be deleted from this device and our servers (within 30 days). Purchases can be restored from the store." },
            { "Sürüm {0} · Oyuncu ID: {1}…{2}", "Version {0} · Player ID: {1}…{2}" },

            // Ev görevleri (Core.Home)
            { "Oturma odası", "Living room" }, { "Mutfak", "Kitchen" }, { "Avlu", "Courtyard" }, { "Tezgâh odası", "Loom room" }, { "Bahçe", "Garden" },
            { "Pencereyi çini çerçeveyle yenile", "Frame the window with tiles" }, { "Pencereye perde as", "Hang curtains" },
            { "Sedire kilim yastık koy", "Add kilim cushions to the divan" }, { "Nine'nin sandığını onar", "Repair Granny's chest" },
            { "Ocak taşlarını temizle", "Clean the hearth stones" }, { "Bakır kapları as", "Hang the copper pots" },
            { "Sofra bezini ser", "Lay the tablecloth" }, { "Kiler rafını onar", "Fix the pantry shelf" },
            { "Avlu taşlarını döşe", "Pave the courtyard" }, { "Çeşmeyi akıt", "Get the fountain flowing" },
            { "Asma çardağı kur", "Build the vine arbour" }, { "Saksılara sardunya dik", "Plant geraniums" },
            { "Dokuma tezgâhını tamir et", "Repair the loom" }, { "Yün çilelerini as", "Hang the wool skeins" },
            { "Duvara eski kilimi as", "Hang the old kilim" }, { "Lambayı yak", "Light the lamp" },
            { "Bahçe kapısını boya", "Paint the garden gate" }, { "Ceviz ağacının dibini temizle", "Tidy under the walnut tree" },
            { "Kuyuyu onar", "Fix the well" }, { "Bahçeye sedir kur", "Set up a garden bench" },
            { "Bu pencereden bütün köy görünürdü.", "You could see the whole village from this window." },
            { "Pencereye bir perde yakışır, kızım.", "A curtain would suit this window, dear." },
            { "Dedenin en sevdiği köşeydi.", "This was Grandpa's favourite corner." }, { "Çeyizim hâlâ içinde duruyor.", "My dowry is still inside." },
            { "Tarhana kokusu buradan yayılırdı.", "The smell of tarhana soup came from here." }, { "Her biri bir düğünden kaldı.", "Each one is from a wedding." },
            { "Sofra kalabalık olsun.", "May the table always be full." }, { "Kışlık burada saklanırdı.", "We kept winter stores here." },
            { "Bayramlarda burada halay çekilirdi.", "We danced the halay here on holidays." }, { "Suyun sesi iyi gelir.", "The sound of water does you good." },
            { "Gölgesinde yün eğirirdik.", "We spun wool in its shade." }, { "Sardunyasız ev olmaz.", "A home needs geraniums." },
            { "İlk kilimimi bu tezgâhta dokudum.", "I wove my first kilim on this loom." }, { "Kökboya ile boyardık.", "We dyed them with madder root." },
            { "Bu kilim annemden kaldı.", "This kilim was my mother's." }, { "Geceleri de dokurduk.", "We wove at night too." },
            { "Kapı mavi olursa nazar değmez.", "A blue gate keeps the evil eye away." }, { "Bu ceviz senden yaşlı.", "This walnut tree is older than you." },
            { "Suyu buz gibidir.", "Its water is ice-cold." }, { "Akşam çayı burada içilir.", "Evening tea is had here." },
            { "Çiçekli", "Floral" }, { "Yanışlı", "Yanış motif" }, { "Düz keten", "Plain linen" }, { "Kırmızı", "Red" }, { "Pembe", "Pink" },
            { "Beyaz", "White" }, { "Mavi", "Blue" }, { "Yeşil", "Green" }, { "Doğal", "Natural" },

            // Sezon ödülleri (Core.SeasonPass)
            { "Sandık: 250 coin", "Chest: 250 coins" }, { "Değiştir ×1", "Swap ×1" }, { "Geri Al ×1", "Undo ×1" }, { "Geri Al ×2", "Undo ×2" },
            { "Tek İlmek ×1", "Single Knot ×1" }, { "Çerçeve: Fas", "Frame: Morocco" }, { "Pamuk: Fes", "Pamuk: Fez" },
            { "Pamuk: Kaftan + 1.000 coin", "Pamuk: Kaftan + 1,000 coins" },
        };
    }
}
