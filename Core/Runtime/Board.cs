using System.Collections.Generic;

namespace Kilim.Core
{
    /// <summary>Tahta (dokuma tezgâhı ızgarası). Yalnızca durum; kurallar GameSession'da.</summary>
    public sealed class Board
    {
        public const int Empty = -1;

        private readonly LevelData _level;
        private readonly int[,] _owner;     // yerleşen parçanın kimliği, boşsa Empty
        private readonly string[,] _color;  // yerleşen parçanın rengi

        public int Size => _level.Size;
        public int FilledCount { get; private set; }
        public int MatchCount { get; private set; }
        public int PlayableCells { get; }
        public bool IsFull => FilledCount == PlayableCells;

        public Board(LevelData level)
        {
            _level = level;
            _owner = new int[level.Size, level.Size];
            _color = new string[level.Size, level.Size];
            for (int r = 0; r < level.Size; r++)
                for (int c = 0; c < level.Size; c++)
                    _owner[r, c] = Empty;
            PlayableCells = level.PlayableCells;
        }

        public bool IsPlayable(int r, int c) => _level.IsPlayable(r, c);
        public bool IsFree(int r, int c) => _level.IsPlayable(r, c) && _owner[r, c] == Empty;
        public int OwnerAt(int r, int c) => _owner[r, c];
        public string ColorAt(int r, int c) => _color[r, c];
        public bool IsMatchAt(int r, int c) => _owner[r, c] != Empty && _level.Matches(r, c, _color[r, c]);

        /// <summary>Şeklin sol-üstü <paramref name="anchor"/>'a gelecek şekilde yerleşebilir mi.</summary>
        public bool CanPlace(Shape shape, Cell anchor)
        {
            foreach (var c in shape.Cells)
                if (!IsFree(anchor.Row + c.Row, anchor.Col + c.Col)) return false;
            return true;
        }

        public List<Cell> Place(Shape shape, Cell anchor, string color, int ownerId)
        {
            var cells = new List<Cell>(shape.Count);
            foreach (var c in shape.Cells)
            {
                var t = anchor.Offset(c);
                _owner[t.Row, t.Col] = ownerId;
                _color[t.Row, t.Col] = color;
                FilledCount++;
                if (_level.Matches(t.Row, t.Col, color)) MatchCount++;
                cells.Add(t);
            }
            return cells;
        }

        public void Remove(IEnumerable<Cell> cells)
        {
            foreach (var t in cells)
            {
                if (_owner[t.Row, t.Col] == Empty) continue;
                if (_level.Matches(t.Row, t.Col, _color[t.Row, t.Col])) MatchCount--;
                _owner[t.Row, t.Col] = Empty;
                _color[t.Row, t.Col] = null;
                FilledCount--;
            }
        }

        /// <summary>Şekil bu döndürmesiyle tahtada herhangi bir yere sığıyor mu.</summary>
        public bool FitsAnywhere(Shape shape)
        {
            for (int r = 0; r + shape.Height <= Size; r++)
                for (int c = 0; c + shape.Width <= Size; c++)
                    if (CanPlace(shape, new Cell(r, c))) return true;
            return false;
        }

        /// <summary>Şeklin herhangi bir döndürmesi bir yere sığıyor mu (kayıp kontrolü, Spec 1 §4).</summary>
        public bool FitsAnyRotation(Shape shape)
        {
            foreach (var rot in shape.DistinctRotations())
                if (FitsAnywhere(rot)) return true;
            return false;
        }

        /// <summary>
        /// Mıknatıs (Spec 1 §3): hedef geçerliyse kendisi; değilse 1 hücre (8 komşu) içindeki en yakın
        /// geçerli konum; eşitlikte yukarı, sonra sol. Yoksa null.
        /// </summary>
        public Cell? SnapAnchor(Shape shape, Cell target)
        {
            if (CanPlace(shape, target)) return target;
            Cell? best = null;
            int bestDist = int.MaxValue;
            for (int dr = -1; dr <= 1; dr++)
                for (int dc = -1; dc <= 1; dc++)
                {
                    if (dr == 0 && dc == 0) continue;
                    var cand = new Cell(target.Row + dr, target.Col + dc);
                    if (!CanPlace(shape, cand)) continue;
                    int dist = dr * dr + dc * dc;
                    if (dist < bestDist || (dist == bestDist && cand.CompareTo(best.Value) < 0))
                    {
                        best = cand;
                        bestDist = dist;
                    }
                }
            return best;
        }
    }
}
