using System;
using System.Collections.Generic;
using System.Linq;

namespace Kilim.Core
{
    public enum SessionState { Playing, Won, Failed }

    /// <summary>Tezgâhtaki bir parça. Kuyruk parçası ya da Tek İlmek (QueueIndex = -1).</summary>
    public sealed class TrayPiece
    {
        public int QueueIndex { get; }
        public Shape BaseShape { get; }
        public Shape Shape { get; internal set; }
        public string Color { get; }
        public bool IsSingleStitch => QueueIndex < 0;
        /// <summary>Kaç kez döndürüldü (0–3); analytics ve geri al için.</summary>
        public int Rotation { get; internal set; }

        internal TrayPiece(int queueIndex, Shape shape, string color)
        {
            QueueIndex = queueIndex;
            BaseShape = shape;
            Shape = shape;
            Color = color;
        }
    }

    public readonly struct PlaceResult
    {
        public readonly bool Success;
        public readonly IReadOnlyList<Cell> Cells;
        public readonly int MatchedCells;
        public PlaceResult(bool success, IReadOnlyList<Cell> cells, int matched)
        { Success = success; Cells = cells; MatchedCells = matched; }
        public static PlaceResult Fail => new PlaceResult(false, Array.Empty<Cell>(), 0);
    }

    /// <summary>
    /// Bir bölüm oturumu: tezgâh, kuyruk, yerleştirme, kazanma/kaybetme, yıldız ve güçlendirici mekanikleri.
    /// Coin/fiyat bilgisi burada yok (Economy). Saf C#; Unity'den bağımsız ve deterministik.
    /// Kurallar: Spec 1 §2–§5, Spec 2 §3 (devam).
    /// </summary>
    public sealed class GameSession
    {
        public const int TraySlots = 4;
        /// <summary>Tek İlmek parçaları için ek yuva sayısı (devam paketi +2 Tek İlmek verir).</summary>
        public const int BonusSlots = 2;

        private readonly LevelData _level;
        private readonly TrayPiece[] _tray = new TrayPiece[TraySlots + BonusSlots];
        private readonly List<int> _pending = new List<int>();          // tezgâha henüz gelmemiş kuyruk indeksleri
        private readonly Stack<Move> _history = new Stack<Move>();
        private int _nextOwnerId;
        private bool _lastActionWasPlacement;

        public LevelData Level => _level;
        public Board Board { get; }
        public SessionState State { get; private set; } = SessionState.Playing;
        public IReadOnlyList<TrayPiece> Tray => _tray;
        public int RemainingInQueue => _pending.Count;
        /// <summary>Tezgâh üstünde yazan "Kalan N": kuyruktaki + tezgâhtaki kuyruk parçaları.</summary>
        public int RemainingPieces => _pending.Count + _tray.Count(p => p != null && !p.IsSingleStitch);
        public int PlacementCount { get; private set; }
        public int FailCount { get; private set; }
        public bool UsedSingleStitch { get; private set; }

        /// <summary>Durum değişince (Won/Failed/Playing) tetiklenir.</summary>
        public event Action<SessionState> StateChanged;

        /// <summary>Zen modu (Günün Deseni): tezgâh dolarsa son yerleşim kendiliğinden geri alınır, kayıp yok.</summary>
        public bool ZenMode { get; set; }
        /// <summary>Tıkanınca kaybetmek yerine son parça geri alınır (öğrenme bölümleri, Kolay mod).</summary>
        public bool AutoUndoOnStuck { get; set; }

        /// <summary>Zen modunda otomatik geri alma olunca tetiklenir (geri alınan hücreler).</summary>
        public event Action<IReadOnlyList<Cell>> AutoUndone;

        public GameSession(LevelData level)
        {
            _level = level ?? throw new ArgumentNullException(nameof(level));
            Board = new Board(level);
            for (int i = 0; i < level.Queue.Count; i++) _pending.Add(i);
            for (int s = 0; s < TraySlots; s++) _tray[s] = DrawNext();
            EvaluateFail();
        }

        // ---------- Sorgular ----------

        public double PatternPercent => Board.PlayableCells == 0 ? 0 : (double)Board.MatchCount / Board.PlayableCells;

        /// <summary>Spec 1 §4: 1★ bitir · 2★ eşleşen ≥ stars.2 · 3★ eşleşen = tüm hücreler.</summary>
        public int Stars
        {
            get
            {
                if (State != SessionState.Won) return 0;
                if (Board.MatchCount >= _level.Star3Cells) return 3;
                if (Board.MatchCount >= _level.Star2Cells) return 2;
                return 1;
            }
        }

        // Güçlendiriciler "Tezgâh doldu" durumunda da kullanılabilir (kurtarma): başarılı olursa oyun kaldığı yerden sürer.
        private bool Live => State != SessionState.Won;
        public bool CanUndo => Live && _lastActionWasPlacement && _history.Count > 0;
        public bool CanSwap(int slot) => Live && _pending.Count > 0 && IsQueueSlot(slot) && _tray[slot] != null;
        public bool CanAddSingleStitch => Live && FirstFreeBonusSlot() >= 0 && !Board.IsFull;

        /// <summary>Kayıp anında en az bir güçlendirici durumu kurtarabilir mi (Geri Al, Değiştir ya da Tek İlmek).</summary>
        public bool HasRescue => State == SessionState.Failed &&
                                 (CanUndo || CanAddSingleStitch || Enumerable.Range(0, TraySlots).Any(CanSwap));

        // ---------- Oyuncu eylemleri ----------

        /// <summary>Tezgâhtaki parçaya dokunuş: saat yönünde 90°.</summary>
        public bool Rotate(int slot)
        {
            if (State != SessionState.Playing || !ValidSlot(slot) || _tray[slot] == null) return false;
            var p = _tray[slot];
            p.Shape = p.Shape.RotateCW();
            p.Rotation = (p.Rotation + 1) % 4;
            return true;
        }

        public bool CanPlace(int slot, Cell anchor) =>
            State == SessionState.Playing && ValidSlot(slot) && _tray[slot] != null && Board.CanPlace(_tray[slot].Shape, anchor);

        /// <summary>Parçayı sol-üstü anchor'a gelecek şekilde yerleştirir. Geçersizse hiçbir şey değişmez.</summary>
        public PlaceResult Place(int slot, Cell anchor)
        {
            if (!CanPlace(slot, anchor)) return PlaceResult.Fail;
            var piece = _tray[slot];
            int matchedBefore = Board.MatchCount;
            var cells = Board.Place(piece.Shape, anchor, piece.Color, _nextOwnerId++);

            _tray[slot] = null;
            TrayPiece refill = null;
            if (IsQueueSlot(slot))
            {
                refill = DrawNext();
                _tray[slot] = refill;
            }

            _history.Push(new Move(slot, piece, cells, refill));
            _lastActionWasPlacement = true;
            PlacementCount++;
            if (piece.IsSingleStitch) UsedSingleStitch = true;

            if (Board.IsFull) SetState(SessionState.Won);
            else EvaluateFail();

            return new PlaceResult(true, cells, Board.MatchCount - matchedBefore);
        }

        /// <summary>Geri Al (Spec 1 §5): son yerleşimi geri alır; parça eski yuvasına, kuyruk eski haline döner.
        /// Yalnızca son eylem yerleştirme ise kullanılabilir (Değiştir/Tek İlmek sonrası pasif).</summary>
        public bool Undo()
        {
            if (!CanUndo) return false;
            var m = _history.Pop();
            Board.Remove(m.Cells);
            if (m.Refill != null)
            {
                _pending.Insert(0, m.Refill.QueueIndex);
            }
            // Parça yerleştiği andaki döndürmesiyle geri gelir.
            _tray[m.Slot] = m.Piece;
            PlacementCount--;
            _lastActionWasPlacement = _history.Count > 0;
            if (m.Piece.IsSingleStitch) UsedSingleStitch = _history.Any(h => h.Piece.IsSingleStitch);
            Revive();
            return true;
        }

        /// <summary>Değiştir (Spec 1 §5): parçayı kuyruğun sonuna gönderir, yuvaya sıradaki gelir. Kuyruk boşsa pasif.</summary>
        public bool Swap(int slot)
        {
            if (!CanSwap(slot)) return false;
            var p = _tray[slot];
            _pending.Add(p.QueueIndex);
            _tray[slot] = DrawNext();
            _lastActionWasPlacement = false;
            Revive();
            EvaluateFail();
            return true;
        }

        /// <summary>Tek İlmek (Spec 1 §5): tezgâha 1 hücrelik, motifle eşleşmeyen ek parça koyar.</summary>
        public bool AddSingleStitch()
        {
            int s = FirstFreeBonusSlot();
            if (!CanAddSingleStitch) return false;
            _tray[s] = new TrayPiece(-1, Shape.Single(), "");
            _lastActionWasPlacement = false;
            Revive();
            EvaluateFail();
            return true;
        }

        /// <summary>
        /// Devam kabul edildi (Spec 2 §3). Oturum yeniden oynanır hale gelir; ödül (2 Tek İlmek + 1 Değiştir)
        /// cüzdana Economy tarafından eklenir. Kayıp kontrolü bir sonraki yerleşimde tekrar çalışır.
        /// </summary>
        public bool ContinueAfterFail()
        {
            if (State != SessionState.Failed) return false;
            SetState(SessionState.Playing);
            return true;
        }

        // ---------- İç ----------

        /// <summary>Kurtarma: kayıptayken güçlendirici uygulandıysa oyun sürer (kayıp kontrolü yeniden yapılır).</summary>
        private void Revive()
        {
            if (State == SessionState.Failed) SetState(SessionState.Playing);
        }

        private TrayPiece DrawNext()
        {
            if (_pending.Count == 0) return null;
            int qi = _pending[0];
            _pending.RemoveAt(0);
            var def = _level.Queue[qi];
            return new TrayPiece(qi, def.Shape, def.Color);
        }

        private void EvaluateFail()
        {
            if (State != SessionState.Playing || Board.IsFull) return;
            foreach (var p in _tray)
                if (p != null && Board.FitsAnyRotation(p.Shape)) return;
            if ((ZenMode || AutoUndoOnStuck) && _history.Count > 0)
            {
                var m = _history.Peek();
                _lastActionWasPlacement = true;
                Undo();
                AutoUndone?.Invoke(m.Cells);
                return;
            }
            FailCount++;
            SetState(SessionState.Failed);
        }

        private void SetState(SessionState s)
        {
            if (State == s) return;
            State = s;
            StateChanged?.Invoke(s);
        }

        private static bool ValidSlot(int slot) => slot >= 0 && slot < TraySlots + BonusSlots;
        private static bool IsQueueSlot(int slot) => slot >= 0 && slot < TraySlots;

        private int FirstFreeBonusSlot()
        {
            for (int s = TraySlots; s < TraySlots + BonusSlots; s++) if (_tray[s] == null) return s;
            return -1;
        }

        private sealed class Move
        {
            public readonly int Slot;
            public readonly TrayPiece Piece;
            public readonly List<Cell> Cells;
            public readonly TrayPiece Refill;
            public Move(int slot, TrayPiece piece, List<Cell> cells, TrayPiece refill)
            { Slot = slot; Piece = piece; Cells = cells; Refill = refill; }
        }
    }
}
