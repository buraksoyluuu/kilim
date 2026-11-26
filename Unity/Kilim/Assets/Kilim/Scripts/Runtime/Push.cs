// Yerel bildirimler (com.unity.mobile.notifications, birleşik API). Plan Core'da: NotificationPlanner.
// Arka plana geçerken tüm planlı bildirimler silinip yeniden kurulur; açılışta bildirim kaynaklı derin bağlantı okunur.
using System;
using System.Collections;
using Kilim.Core;
using UnityEngine;
#if KILIM_NOTIF
using Unity.Notifications;
#endif

namespace Kilim.Game
{
    public static class Push
    {
        private static bool _init;

        public static void Init()
        {
#if KILIM_NOTIF
            if (_init) return;
            var args = NotificationCenterArgs.Default;
            args.AndroidChannelId = "kilim";
            args.AndroidChannelName = "Kilim";
            args.AndroidChannelDescription = Loc.T("Can, günlük desen ve yarış hatırlatmaları");
            NotificationCenter.Initialize(args);
            _init = true;
#endif
        }

        /// <summary>Sistem izni (Android 13+, iOS). İlk kazanılan bölümden sonra bir kez sorulur.</summary>
        public static IEnumerator RequestPermission(Action<bool> done)
        {
#if KILIM_NOTIF
            Init();
            var r = NotificationCenter.RequestPermission();
            if (r.Status == NotificationsPermissionStatus.RequestPending) yield return r;
            done?.Invoke(r.Status == NotificationsPermissionStatus.Granted);
#else
            done?.Invoke(false);
            yield break;
#endif
        }

        public static void Reschedule(Meta m)
        {
#if KILIM_NOTIF
            if (m == null) return;
            Init();
            NotificationCenter.CancelAllScheduledNotifications();
            var gameNow = m.Clock.UtcNow + m.Clock.LocalOffset;
            foreach (var n in NotificationPlanner.Plan(m))
            {
                var when = DateTime.Now + (n.FireLocal - gameNow);   // oyun yerel saati → cihaz saati
                NotificationCenter.ScheduleNotification(new Notification
                {
                    Identifier = n.Id,
                    Title = Loc.T(n.Title),
                    Text = Loc.T(n.Body),
                    Data = n.Deeplink,
                }, new NotificationDateTimeSchedule(when));
            }
#endif
        }

        public static void ClearDelivered()
        {
#if KILIM_NOTIF
            if (!_init) return;
            NotificationCenter.CancelAllDeliveredNotifications();
#endif
        }

        /// <summary>Uygulama bildirime dokunularak açıldıysa derin bağlantıyı (home/daily/race/shop/level) verir.</summary>
        public static IEnumerator ConsumeOpened(Action<string> open)
        {
#if KILIM_NOTIF
            Init();
            var q = NotificationCenter.QueryLastRespondedNotification();
            yield return q;
            if (q.State == QueryLastRespondedNotificationState.HaveRespondedNotification) open?.Invoke(q.Notification.Data);
#else
            yield break;
#endif
        }
    }
}
