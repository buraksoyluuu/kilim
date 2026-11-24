using System;
using System.Collections.Generic;
using System.Globalization;

namespace Kilim.Core
{
    /// <summary>Kayıt/config sözlüklerinden güvenli okuma yardımcıları. Eksik anahtar = varsayılan.</summary>
    public static class Data
    {
        private static readonly Dictionary<string, object> EmptyDict = new Dictionary<string, object>();
        private static readonly List<object> EmptyList = new List<object>();

        public static int Int(Dictionary<string, object> d, string k, int def = 0) =>
            d != null && d.TryGetValue(k, out var v) && v != null ? Convert.ToInt32(v, CultureInfo.InvariantCulture) : def;

        public static long Long(Dictionary<string, object> d, string k, long def = 0) =>
            d != null && d.TryGetValue(k, out var v) && v != null ? Convert.ToInt64(v, CultureInfo.InvariantCulture) : def;

        public static double Double(Dictionary<string, object> d, string k, double def = 0) =>
            d != null && d.TryGetValue(k, out var v) && v != null ? Convert.ToDouble(v, CultureInfo.InvariantCulture) : def;

        public static bool Bool(Dictionary<string, object> d, string k, bool def = false) =>
            d != null && d.TryGetValue(k, out var v) && v is bool b ? b : def;

        public static string Str(Dictionary<string, object> d, string k, string def = null) =>
            d != null && d.TryGetValue(k, out var v) && v is string s ? s : def;

        public static Dictionary<string, object> Dict(Dictionary<string, object> d, string k) =>
            d != null && d.TryGetValue(k, out var v) && v is Dictionary<string, object> r ? r : EmptyDict;

        public static List<object> List(Dictionary<string, object> d, string k) =>
            d != null && d.TryGetValue(k, out var v) && v is List<object> r ? r : EmptyList;

        public static int[] IntArray(Dictionary<string, object> d, string k, int[] def)
        {
            var l = List(d, k);
            if (l.Count == 0) return def;
            var a = new int[l.Count];
            for (int i = 0; i < l.Count; i++) a[i] = Convert.ToInt32(l[i], CultureInfo.InvariantCulture);
            return a;
        }

        /// <summary>UTC zaman damgası (Unix ms) ↔ DateTime.</summary>
        public static long ToMs(DateTime utc) => new DateTimeOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc)).ToUnixTimeMilliseconds();
        public static DateTime FromMs(long ms) => DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime;
    }

    /// <summary>Saat soyutlaması. Oyun içinde sunucu zamanı ile beslenir (UAT DAILY-06, OFFER-02).</summary>
    public interface IClock
    {
        DateTime UtcNow { get; }
        /// <summary>Oyuncunun yerel saat dilimi farkı (günlük/yarış yerel saate göre).</summary>
        TimeSpan LocalOffset { get; }
    }

    public sealed class SystemClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
        public TimeSpan LocalOffset => TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow);
    }

    /// <summary>Sunucu zamanı alınınca cihaz saatine göre fark tutar; cihaz saati ileri/geri alınsa da etkilenmez.</summary>
    public sealed class TrustedClock : IClock
    {
        private readonly IClock _device;
        private TimeSpan _serverDelta;
        public bool Synced { get; private set; }

        public TrustedClock(IClock device) { _device = device; }

        public void Sync(DateTime serverUtc)
        {
            _serverDelta = serverUtc - _device.UtcNow;
            Synced = true;
        }

        public DateTime UtcNow => _device.UtcNow + _serverDelta;
        public TimeSpan LocalOffset => _device.LocalOffset;
    }

    public sealed class ManualClock : IClock
    {
        public DateTime UtcNow { get; set; }
        public TimeSpan LocalOffset { get; set; }
        public ManualClock(DateTime utc, TimeSpan offset = default) { UtcNow = utc; LocalOffset = offset; }
        public void Advance(TimeSpan t) => UtcNow += t;
    }

    public static class ClockExt
    {
        public static DateTime LocalNow(this IClock c) => c.UtcNow + c.LocalOffset;
        public static DateTime LocalDate(this IClock c) => c.LocalNow().Date;
    }
}
