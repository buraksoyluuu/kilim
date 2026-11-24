namespace Kilim.Core
{
    public enum BoosterUseResult { Applied, NotApplicable, Locked, NotEnoughCoins }

    /// <summary>
    /// Güçlendirici kullanımı: önce uygulanabilirlik kontrol edilir, sonra stoktan (yoksa coin'le) düşülür.
    /// Uygulanamayan güçlendirici için hiçbir şey harcanmaz.
    /// </summary>
    public sealed class BoosterService
    {
        private readonly Wallet _wallet;
        private readonly EconomyConfig _cfg;

        public BoosterService(Wallet wallet, EconomyConfig cfg) { _wallet = wallet; _cfg = cfg; }

        public bool IsApplicable(GameSession s, Booster b, int slot = -1)
        {
            switch (b)
            {
                case Booster.Undo: return s.CanUndo;
                case Booster.Swap: return s.CanSwap(slot);
                case Booster.SingleStitch: return s.CanAddSingleStitch;
                default: return false;
            }
        }

        public BoosterUseResult Use(GameSession s, Booster b, int slot = -1)
        {
            // Kilitliyken de stoktaki (ör. devam paketinden gelen) güçlendirici kullanılabilir; coin'le alım açılış ister.
            if (!_wallet.IsUnlocked(b) && _wallet.BoosterCount(b) == 0) return BoosterUseResult.Locked;
            if (!IsApplicable(s, b, slot)) return BoosterUseResult.NotApplicable;
            if (!_wallet.TryConsumeBooster(b) && !_wallet.TrySpend(_cfg.BoosterPrice[b]))
                return BoosterUseResult.NotEnoughCoins;

            bool ok;
            switch (b)
            {
                case Booster.Undo: ok = s.Undo(); break;
                case Booster.Swap: ok = s.Swap(slot); break;
                default: ok = s.AddSingleStitch(); break;
            }
            // IsApplicable doğrulandığı için ok her zaman true olmalı; değilse iade et.
            if (!ok) { _wallet.AddBooster(b, 1); return BoosterUseResult.NotApplicable; }
            return BoosterUseResult.Applied;
        }
    }
}
