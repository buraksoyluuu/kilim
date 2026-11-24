using System;
using System.Collections.Generic;
using System.Linq;

namespace Kilim.Core
{
    /// <summary>Izgara hücresi. Koordinat (satır, sütun); (0,0) sol üst. JSON'daki [r,c] ile birebir.</summary>
    public readonly struct Cell : IEquatable<Cell>, IComparable<Cell>
    {
        public readonly int Row;
        public readonly int Col;

        public Cell(int row, int col) { Row = row; Col = col; }

        public Cell Offset(Cell by) => new Cell(Row + by.Row, Col + by.Col);
        public bool Equals(Cell other) => Row == other.Row && Col == other.Col;
        public override bool Equals(object obj) => obj is Cell c && Equals(c);
        public override int GetHashCode() => (Row * 397) ^ Col;
        public int CompareTo(Cell other) => Row != other.Row ? Row.CompareTo(other.Row) : Col.CompareTo(other.Col);
        public override string ToString() => $"({Row},{Col})";
        public static bool operator ==(Cell a, Cell b) => a.Equals(b);
        public static bool operator !=(Cell a, Cell b) => !a.Equals(b);
    }

    /// <summary>
    /// Poliomino şekli. Her zaman normalize: en küçük satır/sütun 0, hücreler sıralı.
    /// Döndürme kuralı çözücüyle aynıdır: (r,c) → (c,−r), sonra normalize = saat yönünde 90°.
    /// </summary>
    public sealed class Shape : IEquatable<Shape>
    {
        private readonly Cell[] _cells;
        public IReadOnlyList<Cell> Cells => _cells;
        public int Height { get; }
        public int Width { get; }
        public int Count => _cells.Length;

        private Shape(Cell[] normalizedSorted)
        {
            _cells = normalizedSorted;
            Height = _cells.Max(c => c.Row) + 1;
            Width = _cells.Max(c => c.Col) + 1;
        }

        public static Shape From(IEnumerable<Cell> cells)
        {
            var list = cells.ToList();
            if (list.Count == 0) throw new ArgumentException("Şekil boş olamaz");
            int mr = list.Min(c => c.Row), mc = list.Min(c => c.Col);
            var norm = list.Select(c => new Cell(c.Row - mr, c.Col - mc)).Distinct().ToArray();
            if (norm.Length != list.Count) throw new ArgumentException("Şekilde tekrarlanan hücre var");
            Array.Sort(norm);
            return new Shape(norm);
        }

        public static Shape Single() => From(new[] { new Cell(0, 0) });

        public Shape RotateCW() => From(_cells.Select(c => new Cell(c.Col, -c.Row)));

        /// <summary>Birbirinden farklı döndürmeler (1, 2 veya 4 adet), çözücüdeki sırayla.</summary>
        public IReadOnlyList<Shape> DistinctRotations()
        {
            var res = new List<Shape>();
            var cur = this;
            for (int i = 0; i < 4; i++)
            {
                if (!res.Contains(cur)) res.Add(cur);
                cur = cur.RotateCW();
            }
            return res;
        }

        public bool Equals(Shape other) => other != null && _cells.SequenceEqual(other._cells);
        public override bool Equals(object obj) => Equals(obj as Shape);
        public override int GetHashCode()
        {
            unchecked
            {
                int h = 17;
                foreach (var c in _cells) h = h * 31 + c.GetHashCode();
                return h;
            }
        }
        public override string ToString() => string.Join("", _cells.Select(c => c.ToString()));
    }
}
