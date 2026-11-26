using System;
using System.Collections.Generic;
using System.Linq;

namespace Kilim.Core
{
    public enum NotifKind { LivesFull, Daily, Streak, RaceEnding, Piggy, Comeback3, Comeback7 }

    public sealed class PlannedNotification
    {
        public NotifKind Kind;
        public DateTime FireLocal;     // oyuncunun yerel saati
        public string Title, Body;     // Türkçe kaynak metin (Loc.T ile çevrilir)
        public string Deeplink;        // dokununca açılacak yer: home, daily, race, shop, level
        public int Id => (int)Kind + 1;
    }

    /// <summary>
    /// Yerel bildirim planı (sunucusuz). Uygulama arka plana geçerken hepsi silinip bu plan yeniden kurulur.
    /// Kurallar: sessiz saat (22–09 → ertesi 09:00'a kayar), günde en fazla N, 18 yaş altına satış içerikli bildirim yok,
    /// her tür uzaktan kapatılabilir (notif_off: "piggy,race" gibi).
    /// </summary>
    public static class NotificationPlanner
    {
        public static List<PlannedNotification> Plan(Meta m)
        {
            var c = m.Config.Notif;
            var res = new List<PlannedNotification>();
            if (!c.Enabled || !m.Profile.Notifications) return res;
            var now = m.Clock.UtcNow + m.Clock.LocalOffset;
            var p = m.Profile;
            var teen = p.Age == AgeBand.Teen;

            void Add(NotifKind k, DateTime at, string title, string body, string link)
            {
                if (c.Off.Contains(k)) return;
                at = Shift(at, c);
                if (at <= now.AddMinutes(1)) return;
                res.Add(new PlannedNotification { Kind = k, FireLocal = at, Title = title, Body = body, Deeplink = link });
            }

            // 1) Canlar doldu
            if (!m.Lives.IsFull) Add(NotifKind.LivesFull, now + m.Lives.FullIn, "Canların doldu!", "Nine tezgâhta seni bekliyor, bir kilim daha dokuyalım.", "level");

            // 2) Günün deseni (bugün oynadıysa yarın; saat: tercih edilen saat)
            var todayPlayed = p.LastDailyDay == m.Daily.Today;
            var dailyAt = now.Date.AddDays(todayPlayed ? 1 : 0).AddHours(c.DailyHour);
            if (dailyAt <= now) dailyAt = dailyAt.AddDays(1);
            Add(NotifKind.Daily, dailyAt, "Günün deseni hazır", "Bugünün kilim deseni seni bekliyor.", "daily");

            // 3) Seri bozulmasın: serisi var, bugün oynamadı → bu akşam
            var streak = m.Daily.CurrentStreak(p);
            if (streak >= 2 && !todayPlayed) Add(NotifKind.Streak, now.Date.AddHours(c.StreakHour), $"{streak} günlük serin bozulmasın!", "Günün desenini dokuyarak serini koru.", "daily");

            // 4) Yarış bitiyor: katıldıysa, bitime 3 saat kala
            if (m.Race.IsActive && m.Race.Flags(p) > 0) Add(NotifKind.RaceEnding, now + m.Race.Remaining - TimeSpan.FromHours(3), "Yarış bitmek üzere!", "Son saatler: bir bayrak daha kap, sıranı yükselt.", "race");

            // 5) Kumbara doldu (satış içerikli → 18 yaş altına yok)
            if (!teen && p.Wallet.Piggy >= m.Config.Economy.PiggyCap) Add(NotifKind.Piggy, now.AddDays(1).Date.AddHours(c.DailyHour + 6), "Kumbaran doldu", "Biriken altınlar kırılmayı bekliyor.", "shop");

            // 6) Geri dönüş
            Add(NotifKind.Comeback3, now.AddDays(3), "Avlunun çeşmesi seni bekliyor", "Nine'nin evi seni özledi, kaldığın yerden devam et.", "home");
            Add(NotifKind.Comeback7, now.AddDays(7), "Tezgâh tozlanmasın", "Yeni desenler ve ödüller seni bekliyor.", "home");

            // Günde en fazla N: aynı gün içinde öncelik sırası (enum sırası) korunur
            return res.OrderBy(n => n.FireLocal).GroupBy(n => n.FireLocal.Date)
                      .SelectMany(g => g.OrderBy(n => (int)n.Kind).Take(c.MaxPerDay).OrderBy(n => n.FireLocal)).ToList();
        }

        private static DateTime Shift(DateTime at, NotifConfig c)
        {
            if (at.Hour >= c.QuietFrom) return at.Date.AddDays(1).AddHours(c.QuietTo);
            if (at.Hour < c.QuietTo) return at.Date.AddHours(c.QuietTo);
            return at;
        }
    }

    public sealed class NotifConfig
    {
        public bool Enabled = true;
        public int MaxPerDay = 2, QuietFrom = 22, QuietTo = 9, DailyHour = 12, StreakHour = 20;
        public HashSet<NotifKind> Off = new HashSet<NotifKind>();
    }
}
