using System;
using System.Collections.Generic;
using System.Linq;

namespace Kilim.Core
{
    public sealed class PieceDef
    {
        public Shape Shape { get; }
        public string Color { get; }
        public PieceDef(Shape shape, string color) { Shape = shape; Color = color ?? ""; }
    }

    public sealed class SolutionStep
    {
        public int QueueIndex { get; }
        public IReadOnlyList<Cell> Cells { get; }
        public SolutionStep(int queueIndex, IReadOnlyList<Cell> cells) { QueueIndex = queueIndex; Cells = cells; }
    }

    /// <summary>Bölüm verisi. Şema: Spec 1 §6.</summary>
    public sealed class LevelData
    {
        public const int MinPieceCells = 2;
        public const int MaxPieceCells = 6;

        public int Id { get; set; }
        public int Region { get; set; }
        public int Size { get; set; }
        public bool Boss { get; set; }
        public bool Tutorial { get; set; }
        public double PlayerWinRate { get; set; }
        public bool PerfectPossible { get; set; }
        /// <summary>2★ için gereken eşleşen hücre sayısı (stars.2).</summary>
        public int Star2Cells { get; set; }
        /// <summary>3★ için gereken eşleşen hücre sayısı (stars.3 = oynanabilir hücre).</summary>
        public int Star3Cells { get; set; }
        public bool[,] Blocked { get; set; }
        /// <summary>Hücrenin motif rengi. "" = blocked. Bölge 2'nin çift motifi "K|L" biçimindedir.</summary>
        public string[,] Motif { get; set; }
        public IReadOnlyList<PieceDef> Queue { get; set; }
        public IReadOnlyList<SolutionStep> Solution { get; set; }

        public int PlayableCells
        {
            get
            {
                int n = 0;
                for (int r = 0; r < Size; r++)
                    for (int c = 0; c < Size; c++)
                        if (!Blocked[r, c]) n++;
                return n;
            }
        }

        public bool InBounds(int r, int c) => r >= 0 && c >= 0 && r < Size && c < Size;
        public bool IsPlayable(int r, int c) => InBounds(r, c) && !Blocked[r, c];

        /// <summary>Parça rengi hücrenin motifiyle eşleşiyor mu (çift motif "K|L" desteklenir).</summary>
        public bool Matches(int r, int c, string color)
        {
            if (string.IsNullOrEmpty(color) || !IsPlayable(r, c)) return false;
            var m = Motif[r, c];
            if (m == color) return true;
            return m.IndexOf('|') >= 0 && m.Split('|').Contains(color);
        }

        /// <summary>Bölüm verisinin iç tutarlılığını kontrol eder; hata listesi döner (boş = geçerli).</summary>
        public List<string> Validate()
        {
            var errors = new List<string>();
            if (Size < 3 || Size > 10) errors.Add($"size {Size} aralık dışı");
            int pieceCells = Queue.Sum(p => p.Shape.Count);
            if (pieceCells != PlayableCells) errors.Add($"parça hücresi {pieceCells} ≠ oynanabilir {PlayableCells}");
            if (Star3Cells != PlayableCells) errors.Add($"stars.3 {Star3Cells} ≠ oynanabilir {PlayableCells}");
            if (Star2Cells <= 0 || Star2Cells > Star3Cells) errors.Add($"stars.2 {Star2Cells} geçersiz");
            foreach (var p in Queue)
                // Spec 1 §2 "2–5" diyordu; bölüm 11, 12, 15'te 2×3 dikdörtgen (6) var → kural 2–6 olarak güncellendi.
                if (p.Shape.Count < MinPieceCells || p.Shape.Count > MaxPieceCells) errors.Add($"parça boyu {p.Shape.Count} geçersiz");
            for (int r = 0; r < Size; r++)
                for (int c = 0; c < Size; c++)
                    if (Blocked[r, c] != (Motif[r, c] == "")) errors.Add($"motif/blocked uyuşmuyor {r},{c}");
            return errors;
        }
    }

    public static class LevelParser
    {
        /// <summary>Tek bölüm nesnesi ya da bölüm dizisi içeren JSON'u okur.</summary>
        public static List<LevelData> ParseMany(string json)
        {
            var root = MiniJson.Parse(json);
            if (root is List<object> arr) return arr.Select(o => FromObject((Dictionary<string, object>)o)).ToList();
            if (root is Dictionary<string, object> obj)
            {
                if (obj.TryGetValue("levels", out var lv) && lv is List<object> la)
                    return la.Select(o => FromObject((Dictionary<string, object>)o)).ToList();
                return new List<LevelData> { FromObject(obj) };
            }
            throw new FormatException("Bölüm JSON'u dizi ya da nesne olmalı");
        }

        public static LevelData FromObject(Dictionary<string, object> o)
        {
            int size = Int(o, "size");
            var blocked = new bool[size, size];
            if (o.TryGetValue("blocked", out var b) && b is List<object> bl)
                foreach (var cell in bl) { var c = ToCell(cell); blocked[c.Row, c.Col] = true; }

            var motif = new string[size, size];
            var mrows = (List<object>)o["motif"];
            if (mrows.Count != size) throw new FormatException("motif satır sayısı size ile uyuşmuyor");
            for (int r = 0; r < size; r++)
            {
                var row = (List<object>)mrows[r];
                if (row.Count != size) throw new FormatException("motif sütun sayısı size ile uyuşmuyor");
                for (int c = 0; c < size; c++) motif[r, c] = (string)row[c] ?? "";
            }

            var queue = ((List<object>)o["queue"]).Select(q =>
            {
                var qd = (Dictionary<string, object>)q;
                var shape = Shape.From(((List<object>)qd["shape"]).Select(ToCell));
                return new PieceDef(shape, (string)qd["color"]);
            }).ToList();

            var solution = new List<SolutionStep>();
            if (o.TryGetValue("solution", out var s) && s is List<object> sl)
                foreach (var step in sl)
                {
                    var st = (List<object>)step;
                    solution.Add(new SolutionStep(Convert.ToInt32(st[0]), ((List<object>)st[1]).Select(ToCell).ToList()));
                }

            var stars = o.TryGetValue("stars", out var so) ? (Dictionary<string, object>)so : new Dictionary<string, object>();
            var level = new LevelData
            {
                Id = Int(o, "id"),
                Region = o.ContainsKey("region") ? Int(o, "region") : 1,
                Size = size,
                Blocked = blocked,
                Motif = motif,
                Queue = queue,
                Solution = solution,
                Boss = o.TryGetValue("boss", out var bo) && bo is bool bb && bb,
                Tutorial = o.TryGetValue("tutorial", out var to) && to is bool tb && tb,
                PerfectPossible = o.TryGetValue("perfectPossible", out var pp) && pp is bool pb && pb,
                PlayerWinRate = o.TryGetValue("playerWinRate", out var wr) && wr is double wd ? wd : 0,
            };
            level.Star3Cells = stars.ContainsKey("3") ? Convert.ToInt32(stars["3"]) : level.PlayableCells;
            level.Star2Cells = stars.ContainsKey("2") ? Convert.ToInt32(stars["2"]) : (int)Math.Ceiling(level.PlayableCells * 0.6);
            return level;
        }

        private static int Int(Dictionary<string, object> o, string k) => Convert.ToInt32(o[k]);
        private static Cell ToCell(object o)
        {
            var l = (List<object>)o;
            return new Cell(Convert.ToInt32(l[0]), Convert.ToInt32(l[1]));
        }
    }
}
