using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Kilim.Core.Tests
{
    /// <summary>Test bölümleri: gerçek JSON + elle kurulan küçük tahtalar.</summary>
    public static class TestLevels
    {
        private static List<LevelData> _region1;

        private static List<LevelData> _all;

        /// <summary>Data/levels altındaki tüm bölgeler.</summary>
        public static List<LevelData> All
        {
            get
            {
                if (_all != null) return _all;
                _all = new List<LevelData>();
                var dir = Path.Combine(AppContext.BaseDirectory, "levels");
                foreach (var f in Directory.GetFiles(dir, "region*.json").OrderBy(f => f.Length).ThenBy(f => f))
                    _all.AddRange(LevelParser.ParseMany(File.ReadAllText(f)));
                return _all;
            }
        }

        public static List<LevelData> Region1
        {
            get
            {
                if (_region1 != null) return _region1;
                var path = Path.Combine(AppContext.BaseDirectory, "levels", "region1.json");
                _region1 = LevelParser.ParseMany(File.ReadAllText(path));
                return _region1;
            }
        }

        public static Shape S(params int[] rc)
        {
            var cells = new List<Cell>();
            for (int i = 0; i < rc.Length; i += 2) cells.Add(new Cell(rc[i], rc[i + 1]));
            return Shape.From(cells);
        }

        /// <summary>
        /// Elle bölüm: motif satırları ("." = blocked), kuyruk (şekil, renk). stars.2 = %60.
        /// </summary>
        public static LevelData Make(string[] motifRows, params (Shape shape, string color)[] queue)
        {
            int size = motifRows.Length;
            var blocked = new bool[size, size];
            var motif = new string[size, size];
            for (int r = 0; r < size; r++)
                for (int c = 0; c < size; c++)
                {
                    char ch = motifRows[r][c];
                    blocked[r, c] = ch == '.';
                    motif[r, c] = ch == '.' ? "" : ch.ToString();
                }
            var level = new LevelData
            {
                Id = 999, Region = 1, Size = size, Blocked = blocked, Motif = motif,
                Queue = queue.Select(q => new PieceDef(q.shape, q.color)).ToList(),
                Solution = new List<SolutionStep>(),
            };
            level.Star3Cells = level.PlayableCells;
            level.Star2Cells = (int)Math.Ceiling(level.PlayableCells * 0.6);
            return level;
        }

        /// <summary>3×3, kare + I3 + domino. Kayıp senaryosu için.</summary>
        public static LevelData ThreeByThree() => Make(
            new[] { "KKK", "KKK", "KKK" },
            (S(0, 0, 0, 1, 1, 0, 1, 1), "K"),
            (S(0, 0, 0, 1, 0, 2), "K"),
            (S(0, 0, 0, 1), "K"));

        /// <summary>Bir slottaki parçayı istenen hücre kümesine oturacak şekilde döndürüp yerleştirir.</summary>
        public static PlaceResult PlaceCells(GameSession s, int slot, IReadOnlyList<Cell> cells)
        {
            var target = Shape.From(cells);
            for (int i = 0; i < 4 && !s.Tray[slot].Shape.Equals(target); i++) s.Rotate(slot);
            if (!s.Tray[slot].Shape.Equals(target)) throw new AssertFailed($"slot {slot} şekli hedefe dönmüyor");
            var anchor = new Cell(cells.Min(c => c.Row), cells.Min(c => c.Col));
            return s.Place(slot, anchor);
        }

        public static int SlotOf(GameSession s, int queueIndex)
        {
            for (int i = 0; i < s.Tray.Count; i++)
                if (s.Tray[i] != null && s.Tray[i].QueueIndex == queueIndex) return i;
            throw new AssertFailed($"kuyruk {queueIndex} tezgâhta değil");
        }
    }
}
