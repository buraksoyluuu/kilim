using System;

namespace Kilim.Core
{
    public sealed class LivesConfig
    {
        public int Max = 3;
        /// <summary>Bir canın yenilenme süresi. 3 can × 60 dk = hepsi 3 saatte dolar.</summary>
        public int RegenMinutes = 60;
        /// <summary>Canları coin ile hemen doldurma bedeli.</summary>
        public int RefillCoins = 900;
    }

    /// <summary>
    /// Can sistemi (Candy Crush mantığı): bölüme başlamak 1 can harcar, kazanınca can geri verilir. Böylece
    /// kaybetmek, pes etmek ya da oyundan çıkmak 1 cana mal olur (uygulamayı kapatıp kaçmak da). Can 0 ise
    /// bölüm başlamaz; her RegenMinutes'ta 1 can yenilenir. Hemen oynamak için: coin, satın alma ya da ödüllü reklam (+1).
    /// Günün Deseni can harcamaz. Saat geri alınırsa sayaç baştan başlar (hile koruması; saat sunucudan doğrulanır).
    /// </summary>
    public sealed class Lives
    {
        private readonly PlayerProfile _p;
        private readonly IClock _clock;
        public LivesConfig Config { get; }

        public Lives(PlayerProfile p, IClock clock, LivesConfig cfg)
        {
            _p = p;
            _clock = clock;
            Config = cfg;
        }

        private DateTime Now => _clock.UtcNow;
        private TimeSpan Regen => TimeSpan.FromMinutes(Config.RegenMinutes);

        /// <summary>Zamanla yenilenen canları uygular.</summary>
        private void Tick()
        {
            if (_p.Lives >= Config.Max) { _p.Lives = Config.Max; _p.LivesSinceUtc = 0; return; }
            var now = Now;
            if (_p.LivesSinceUtc <= 0 || _p.LivesSinceUtc > now.Ticks) { _p.LivesSinceUtc = now.Ticks; return; }
            var since = new DateTime(_p.LivesSinceUtc, DateTimeKind.Utc);
            int gained = (int)((now - since).Ticks / Regen.Ticks);
            if (gained <= 0) return;
            _p.Lives = Math.Min(Config.Max, _p.Lives + gained);
            _p.LivesSinceUtc = _p.Lives >= Config.Max ? 0 : since.AddTicks(Regen.Ticks * gained).Ticks;
        }

        public int Count { get { Tick(); return _p.Lives; } }
        public bool IsFull => Count >= Config.Max;
        public bool CanPlay => Count > 0;

        /// <summary>Sıradaki canın gelmesine kalan süre (dolu ise sıfır).</summary>
        public TimeSpan NextIn
        {
            get
            {
                Tick();
                if (_p.Lives >= Config.Max) return TimeSpan.Zero;
                var left = new DateTime(_p.LivesSinceUtc, DateTimeKind.Utc) + Regen - Now;
                return left < TimeSpan.Zero ? TimeSpan.Zero : left;
            }
        }

        /// <summary>Hepsinin dolmasına kalan süre.</summary>
        public TimeSpan FullIn
        {
            get
            {
                int missing = Config.Max - Count;
                return missing <= 0 ? TimeSpan.Zero : NextIn + TimeSpan.FromTicks(Regen.Ticks * (missing - 1));
            }
        }

        /// <summary>Bölüm başı: 1 can harcar. Can yoksa false.</summary>
        public bool TryConsume()
        {
            Tick();
            if (_p.Lives <= 0) return false;
            if (_p.Lives >= Config.Max) _p.LivesSinceUtc = Now.Ticks; // yenilenme sayacı ilk eksilmede başlar
            _p.Lives--;
            return true;
        }

        /// <summary>Kazanınca harcanan can iade edilir.</summary>
        public void Refund() => Add(1);

        public void Add(int n)
        {
            Tick();
            _p.Lives = Math.Min(Config.Max, _p.Lives + n);
            if (_p.Lives >= Config.Max) _p.LivesSinceUtc = 0;
        }

        public void Refill() { _p.Lives = Config.Max; _p.LivesSinceUtc = 0; }

        public bool TryRefillWithCoins(Wallet w)
        {
            if (IsFull || !w.TrySpend(Config.RefillCoins)) return false;
            Refill();
            return true;
        }
    }
}
