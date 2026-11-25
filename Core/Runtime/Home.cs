using System.Collections.Generic;
using System.Linq;

namespace Kilim.Core
{
    public sealed class HomeTask
    {
        public string Id;
        public string House;
        public string Area;
        public string Title;
        public int Cost;
        public string[] Styles;   // null = stil seçimi yok
        public string NineNote;
    }

    /// <summary>Bir restorasyon mekânı: 5 alan × 4 görev. Alanların görseli <see cref="AreaArt"/> anahtarıyla çizilir.</summary>
    public sealed class House
    {
        public string Id;
        public string Name;
        /// <summary>Mekân açılınca Nine'nin söylediği giriş cümlesi.</summary>
        public string Intro;
        public string[] Areas;
        /// <summary>Alan başına illüstrasyon anahtarı (Resources/art/area_{key}_old|new).</summary>
        public string[] AreaArt;
        public List<HomeTask> Tasks;
        public int TotalCost => Tasks.Sum(t => t.Cost);
    }

    /// <summary>
    /// Ev restorasyonu (Spec 2 §1): her mekân 5 alan × 4 görev, maliyet 1–3 ilmek. Görevler sırayla açılır
    /// (alan içinde, alanlar ve mekânlar arasında); bir mekân bitince sıradaki mekâna geçilir.
    /// Bölümler görevden bağımsız oynanır.
    /// </summary>
    public static class Home
    {
        public static readonly List<House> Houses = new List<House>
        {
            H("nine", "Nine'nin Evi", "Bu ev seni bekliyordu, kızım. Hadi el ele verelim.",
                new[] { "Oturma odası", "Mutfak", "Avlu", "Tezgâh odası", "Bahçe" },
                new[] { "oda", "mutfak", "avlu", "tezgah", "bahce" },
                new[]
                {
                    T("r1-oda-1", "Pencereyi çini çerçeveyle yenile", 1, null, "Bu pencereden bütün köy görünürdü."),
                    T("r1-oda-2", "Pencereye perde as", 2, new[] { "Uşak", "Hereke", "Yörük" }, "Pencereye bir perde yakışır, kızım."),
                    T("r1-oda-3", "Sedire kilim yastık koy", 2, null, "Dedenin en sevdiği köşeydi."),
                    T("r1-oda-4", "Nine'nin sandığını onar", 3, null, "Çeyizim hâlâ içinde duruyor."),
                    T("r1-mut-1", "Ocak taşlarını temizle", 1, null, "Tarhana kokusu buradan yayılırdı."),
                    T("r1-mut-2", "Bakır kapları as", 2, null, "Her biri bir düğünden kaldı."),
                    T("r1-mut-3", "Sofra bezini ser", 2, new[] { "Çiçekli", "Yanışlı", "Düz keten" }, "Sofra kalabalık olsun."),
                    T("r1-mut-4", "Kiler rafını onar", 2, null, "Kışlık burada saklanırdı."),
                    T("r1-avl-1", "Avlu taşlarını döşe", 2, null, "Bayramlarda burada halay çekilirdi."),
                    T("r1-avl-2", "Çeşmeyi akıt", 2, null, "Suyun sesi iyi gelir."),
                    T("r1-avl-3", "Asma çardağı kur", 2, null, "Gölgesinde yün eğirirdik."),
                    T("r1-avl-4", "Saksılara sardunya dik", 1, new[] { "Kırmızı", "Pembe", "Beyaz" }, "Sardunyasız ev olmaz."),
                    T("r1-tez-1", "Dokuma tezgâhını tamir et", 3, null, "İlk kilimimi bu tezgâhta dokudum."),
                    T("r1-tez-2", "Yün çilelerini as", 2, null, "Kökboya ile boyardık."),
                    T("r1-tez-3", "Duvara eski kilimi as", 2, new[] { "Kapadokya", "Kars", "Milas" }, "Bu kilim annemden kaldı."),
                    T("r1-tez-4", "Lambayı yak", 1, null, "Geceleri de dokurduk."),
                    T("r1-bah-1", "Bahçe kapısını boya", 2, new[] { "Mavi", "Yeşil", "Doğal" }, "Kapı mavi olursa nazar değmez."),
                    T("r1-bah-2", "Ceviz ağacının dibini temizle", 2, null, "Bu ceviz senden yaşlı."),
                    T("r1-bah-3", "Kuyuyu onar", 3, null, "Suyu buz gibidir."),
                    T("r1-bah-4", "Bahçeye sedir kur", 3, null, "Akşam çayı burada içilir."),
                }),
            H("bag", "Bağ Evi", "Dedenin bağ evi... Üzümler yine dallarında olsun.",
                new[] { "Bağ yolu", "Asma bağı", "Pekmez evi", "Seki", "Ambar" },
                new[] { "yol", "bag", "pekmez", "seki", "ambar" },
                new[]
                {
                    T("r2-yol-1", "Patikayı temizle", 1, null, "Bu yoldan eşekle inerdik."),
                    T("r2-yol-2", "Taş duvarı ör", 2, null, "Her taşı deden taşıdı."),
                    T("r2-yol-3", "Bağ kapısını as", 2, new[] { "Ahşap", "Demir", "Örgü" }, "Kapı açık dursun, misafir eksik olmasın."),
                    T("r2-yol-4", "Yol boyuna kavak dik", 2, null, "Kavaklar rüzgârı sever."),
                    T("r2-bag-1", "Asmaları buda", 2, null, "Budanmayan asma ağlar."),
                    T("r2-bag-2", "Herekleri çak", 2, null, "Asma yaslanacak dal ister."),
                    T("r2-bag-3", "Salkımları topla", 3, null, "İlk salkım komşuya gider."),
                    T("r2-bag-4", "Korkuluk kur", 1, new[] { "Şapkalı", "Fesli", "Yemenili" }, "Kuşlar da nasibini alır ama."),
                    T("r2-pek-1", "Şıra teknesini onar", 2, null, "Üzüm burada ayakla ezilirdi."),
                    T("r2-pek-2", "Kazanları parlat", 2, null, "Pekmez kazanda sabır ister."),
                    T("r2-pek-3", "Ocağı yak", 1, null, "Ateşi ağır olsun."),
                    T("r2-pek-4", "Pekmez küplerini diz", 3, null, "Kışın en tatlı ilacı."),
                    T("r2-sek-1", "Sekiyi sıva", 2, null, "Yazın burada yatardık."),
                    T("r2-sek-2", "Minder ser", 2, new[] { "Kilim", "Cicim", "Zili" }, "Yıldızları sayardık buradan."),
                    T("r2-sek-3", "Gölgelik kur", 2, null, "Öğle sıcağı çok yakar."),
                    T("r2-sek-4", "Semaveri getir", 1, null, "Çaysız sohbet olmaz."),
                    T("r2-amb-1", "Ambar kapısını onar", 2, null, "Kilidi dedenin cebindeydi."),
                    T("r2-amb-2", "Çuvalları diz", 2, null, "Buğday, nohut, mercimek..."),
                    T("r2-amb-3", "Damı onar", 3, null, "Yağmur girmesin içeri."),
                    T("r2-amb-4", "Nazarlık as", 1, new[] { "Mavi", "Kırmızı", "Boncuk" }, "Göz değmesin bu berekete."),
                }),
            H("yayla", "Yayla Evi", "Yaylaya çıkma vakti! Serin sular, yeşil çayırlar...",
                new[] { "Oba", "Ahır", "Ocak başı", "Çayır", "Kaynak" },
                new[] { "oba", "ahir", "ocak", "cayir", "kaynak" },
                new[]
                {
                    T("r3-oba-1", "Çadırı kur", 2, null, "Kara çadır, ana kucağı gibidir."),
                    T("r3-oba-2", "Keçeleri ser", 2, new[] { "Kırmızı", "Mavi", "Doğal" }, "Keçe soğuğu geçirmez."),
                    T("r3-oba-3", "Çadır kilimini as", 3, null, "Bu desen göçün haritasıdır."),
                    T("r3-oba-4", "Beşiği as", 1, null, "Annen bu beşikte büyüdü."),
                    T("r3-ahi-1", "Ahır kapısını onar", 2, null, "Hayvanlar da yuva ister."),
                    T("r3-ahi-2", "Yemlikleri doldur", 1, null, "Önce hayvan doyar."),
                    T("r3-ahi-3", "Samanlığı topla", 2, null, "Kış uzun, saman çok gerek."),
                    T("r3-ahi-4", "Koyunlara çan tak", 2, new[] { "Pirinç", "Bakır", "Demir" }, "Çan sesinden sürüyü bulurduk."),
                    T("r3-oca-1", "Ocak taşlarını diz", 2, null, "Sacda yufka pişer."),
                    T("r3-oca-2", "Sacı as", 1, null, "Yufkanın inceliği ustalıktır."),
                    T("r3-oca-3", "Yayık kur", 3, null, "Tereyağı yayıkta olur."),
                    T("r3-oca-4", "Kazanı kaynat", 2, null, "Süt kaynasın, peynir mayalansın."),
                    T("r3-cay-1", "Çitleri çek", 2, null, "Çayır herkesin, ama çit bizim."),
                    T("r3-cay-2", "Kır çiçeklerini topla", 1, new[] { "Gelincik", "Papatya", "Kekik" }, "Kekik kokusu yaylanın kokusudur."),
                    T("r3-cay-3", "Salıncak kur", 2, null, "Bayram salıncağı!"),
                    T("r3-cay-4", "Bayrak dik", 2, null, "Oba buradan görünsün."),
                    T("r3-kay-1", "Kaynağı aç", 3, null, "Buz gibi akar, diş sızlatır."),
                    T("r3-kay-2", "Yalak yap", 2, null, "Hayvanlar da su içsin."),
                    T("r3-kay-3", "Taş köprü kur", 2, null, "Dereyi atlamak kolay olmaz."),
                    T("r3-kay-4", "Dilek ağacına bez bağla", 1, new[] { "Kırmızı", "Beyaz", "Yeşil" }, "Dileğin kabul olsun."),
                }),
            H("konak", "Kasaba Konağı", "Kasabadaki eski konak... Dokuduğun kilimler burada sergilenecek.",
                new[] { "Hayat", "Başoda", "Selamlık", "Çarşı kapısı", "Havuz başı" },
                new[] { "hayat", "basoda", "selamlik", "kapi", "havuz" },
                new[]
                {
                    T("r4-hay-1", "Hayatı süpür", 1, null, "Konağın kalbi burasıdır."),
                    T("r4-hay-2", "Merdiveni onar", 2, null, "Gıcırtısı çocukluğumuzdu."),
                    T("r4-hay-3", "Sütunları boya", 2, new[] { "Beyaz", "Aşı boyası", "Mavi" }, "Ahşap boyanınca canlanır."),
                    T("r4-hay-4", "Kuş kafesi as", 2, null, "Kanarya sabah türkü söyler."),
                    T("r4-bas-1", "Tavan göbeğini onar", 3, null, "Usta bunu bir yılda oydu."),
                    T("r4-bas-2", "Yüklüğü düzelt", 2, null, "Yataklar gündüz burada uyur."),
                    T("r4-bas-3", "Büyük kilimi ser", 2, new[] { "Bergama", "Konya", "Yahyalı" }, "Odanın ruhu kilimindedir."),
                    T("r4-bas-4", "Ocak davlumbazını parlat", 1, null, "Kış gecelerinin sohbeti..."),
                    T("r4-sel-1", "Sedirleri kaplat", 2, null, "Misafir başköşeye oturur."),
                    T("r4-sel-2", "Lambaları as", 1, null, "Gaz lambasının ışığı yumuşaktır."),
                    T("r4-sel-3", "Kahve takımını diz", 2, new[] { "Fincan", "Zarf", "Cezve" }, "Bir fincan kahvenin kırk yıl hatırı var."),
                    T("r4-sel-4", "Duvar halısını as", 3, null, "Bu halı konağın şahididir."),
                    T("r4-kap-1", "Kapı tokmağını tak", 1, null, "Tokmak sesinden misafiri tanırdık."),
                    T("r4-kap-2", "Kemerli kapıyı onar", 3, null, "Kemer sağlamsa ev sağlamdır."),
                    T("r4-kap-3", "Tabela as", 2, new[] { "Kilim Evi", "Dokuma", "Nine'nin" }, "Herkes bilsin, kilim burada!"),
                    T("r4-kap-4", "Fenerleri yak", 2, null, "Akşam kapı aydınlık olsun."),
                    T("r4-hav-1", "Havuzu temizle", 2, null, "Su durgun olmasın."),
                    T("r4-hav-2", "Fıskiyeyi aç", 2, null, "Şırıltısı insanı dinlendirir."),
                    T("r4-hav-3", "Gül fidanı dik", 2, new[] { "Kırmızı", "Pembe", "Sarı" }, "Gül kokusu konağa yakışır."),
                    T("r4-hav-4", "Kilim sergisini aç", 3, null, "Elinin emeği, gözünün nuru. Aferin kızım!"),
                }),
        };

        private static House H(string id, string name, string intro, string[] areas, string[] art, HomeTask[] tasks)
        {
            var house = new House { Id = id, Name = name, Intro = intro, Areas = areas, AreaArt = art, Tasks = new List<HomeTask>() };
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i].House = id;
                tasks[i].Area = areas[i / 4];
                house.Tasks.Add(tasks[i]);
            }
            return house;
        }

        private static HomeTask T(string id, string title, int cost, string[] styles, string note) =>
            new HomeTask { Id = id, Title = title, Cost = cost, Styles = styles, NineNote = note };

        /// <summary>İlk mekân (geriye uyum).</summary>
        public static List<HomeTask> Region1 => Houses[0].Tasks;
        public static string[] Areas => Houses[0].Areas;
        public static int TotalCost => Houses[0].TotalCost;

        public static IEnumerable<HomeTask> All => Houses.SelectMany(h => h.Tasks);

        /// <summary>Sıradaki görev (ilk yapılmamış). null = tüm mekânlar tamamlandı.</summary>
        public static HomeTask NextTask(PlayerProfile p) => All.FirstOrDefault(t => !p.HomeTasksDone.Contains(t.Id));

        public static House HouseOf(HomeTask t) => Houses.First(h => h.Id == t.House);

        /// <summary>Üzerinde çalışılan mekân (sıradaki görevin mekânı; hepsi bittiyse sonuncusu).</summary>
        public static House CurrentHouse(PlayerProfile p)
        {
            var next = NextTask(p);
            return next != null ? HouseOf(next) : Houses[Houses.Count - 1];
        }

        public static bool IsUnlocked(PlayerProfile p, House h)
        {
            int idx = Houses.IndexOf(h);
            return idx == 0 || Houses[idx - 1].Tasks.All(t => p.HomeTasksDone.Contains(t.Id));
        }

        public static bool IsComplete(PlayerProfile p, House h) => h.Tasks.All(t => p.HomeTasksDone.Contains(t.Id));

        public static int DoneInArea(PlayerProfile p, string area) =>
            All.Count(t => t.Area == area && p.HomeTasksDone.Contains(t.Id));

        public static int DoneInArea(PlayerProfile p, House h, int areaIndex) =>
            h.Tasks.Skip(areaIndex * 4).Take(4).Count(t => p.HomeTasksDone.Contains(t.Id));

        public static float Progress(PlayerProfile p, House h) =>
            (float)h.Tasks.Count(t => p.HomeTasksDone.Contains(t.Id)) / h.Tasks.Count;

        public static bool CanDo(PlayerProfile p, HomeTask t) =>
            NextTask(p) == t && p.Wallet.Ilmek >= t.Cost;

        /// <summary>Görevi yapar; stil seçimi varsa ilk stil varsayılan (sonra ücretsiz değiştirilebilir).</summary>
        public static bool Do(PlayerProfile p, HomeTask t, string style = null)
        {
            if (!CanDo(p, t) || !p.Wallet.TrySpendIlmek(t.Cost)) return false;
            p.HomeTasksDone.Add(t.Id);
            if (t.Styles != null) p.HomeStyles[t.Id] = style != null && t.Styles.Contains(style) ? style : t.Styles[0];
            return true;
        }

        public static bool ChangeStyle(PlayerProfile p, HomeTask t, string style)
        {
            if (!p.HomeTasksDone.Contains(t.Id) || t.Styles == null || !t.Styles.Contains(style)) return false;
            p.HomeStyles[t.Id] = style;
            return true;
        }
    }
}
