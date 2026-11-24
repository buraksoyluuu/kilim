using System.Collections.Generic;
using System.Linq;

namespace Kilim.Core
{
    public sealed class HomeTask
    {
        public string Id;
        public string Area;
        public string Title;
        public int Cost;
        public string[] Styles;   // null = stil seçimi yok
        public string NineNote;
    }

    /// <summary>
    /// Ev restorasyonu (Spec 2 §1): bölge başına 5 alan × 4 görev, maliyet 1–3 ilmek, toplam 40.
    /// Görevler sırayla açılır (alan içinde ve alanlar arasında); yeni bölümler görevden bağımsız oynanır.
    /// </summary>
    public static class Home
    {
        public static readonly string[] Areas = { "Oturma odası", "Mutfak", "Avlu", "Tezgâh odası", "Bahçe" };

        public static readonly List<HomeTask> Region1 = new List<HomeTask>
        {
            T("r1-oda-1", 0, "Pencereyi çini çerçeveyle yenile", 1, null, "Bu pencereden bütün köy görünürdü."),
            T("r1-oda-2", 0, "Pencereye perde as", 2, new[] { "Uşak", "Hereke", "Yörük" }, "Pencereye bir perde yakışır, kızım."),
            T("r1-oda-3", 0, "Sedire kilim yastık koy", 2, null, "Dedenin en sevdiği köşeydi."),
            T("r1-oda-4", 0, "Nine'nin sandığını onar", 3, null, "Çeyizim hâlâ içinde duruyor."),
            T("r1-mut-1", 1, "Ocak taşlarını temizle", 1, null, "Tarhana kokusu buradan yayılırdı."),
            T("r1-mut-2", 1, "Bakır kapları as", 2, null, "Her biri bir düğünden kaldı."),
            T("r1-mut-3", 1, "Sofra bezini ser", 2, new[] { "Çiçekli", "Yanışlı", "Düz keten" }, "Sofra kalabalık olsun."),
            T("r1-mut-4", 1, "Kiler rafını onar", 2, null, "Kışlık burada saklanırdı."),
            T("r1-avl-1", 2, "Avlu taşlarını döşe", 2, null, "Bayramlarda burada halay çekilirdi."),
            T("r1-avl-2", 2, "Çeşmeyi akıt", 2, null, "Suyun sesi iyi gelir."),
            T("r1-avl-3", 2, "Asma çardağı kur", 2, null, "Gölgesinde yün eğirirdik."),
            T("r1-avl-4", 2, "Saksılara sardunya dik", 1, new[] { "Kırmızı", "Pembe", "Beyaz" }, "Sardunyasız ev olmaz."),
            T("r1-tez-1", 3, "Dokuma tezgâhını tamir et", 3, null, "İlk kilimimi bu tezgâhta dokudum."),
            T("r1-tez-2", 3, "Yün çilelerini as", 2, null, "Kökboya ile boyardık."),
            T("r1-tez-3", 3, "Duvara eski kilimi as", 2, new[] { "Kapadokya", "Kars", "Milas" }, "Bu kilim annemden kaldı."),
            T("r1-tez-4", 3, "Lambayı yak", 1, null, "Geceleri de dokurduk."),
            T("r1-bah-1", 4, "Bahçe kapısını boya", 2, new[] { "Mavi", "Yeşil", "Doğal" }, "Kapı mavi olursa nazar değmez."),
            T("r1-bah-2", 4, "Ceviz ağacının dibini temizle", 2, null, "Bu ceviz senden yaşlı."),
            T("r1-bah-3", 4, "Kuyuyu onar", 3, null, "Suyu buz gibidir."),
            T("r1-bah-4", 4, "Bahçeye sedir kur", 3, null, "Akşam çayı burada içilir."),
        };

        private static HomeTask T(string id, int area, string title, int cost, string[] styles, string note) =>
            new HomeTask { Id = id, Area = Areas[area], Title = title, Cost = cost, Styles = styles, NineNote = note };

        public static int TotalCost => Region1.Sum(t => t.Cost);

        /// <summary>Sıradaki görev (ilk yapılmamış). null = bölge evi tamamlandı.</summary>
        public static HomeTask NextTask(PlayerProfile p) => Region1.FirstOrDefault(t => !p.HomeTasksDone.Contains(t.Id));

        public static int DoneInArea(PlayerProfile p, string area) =>
            Region1.Count(t => t.Area == area && p.HomeTasksDone.Contains(t.Id));

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
