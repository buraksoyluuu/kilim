using System.Collections.Generic;
using System.Linq;

namespace Kilim.Core
{
    /// <summary>İpucu: hangi tepsi yuvasındaki parça hangi hücrelere.</summary>
    public sealed class NextMove
    {
        public int Slot;
        public IReadOnlyList<Cell> Cells;
    }

    /// <summary>Tahtadaki desen ipucunun gücü.</summary>
    public enum HintLevel
    {
        /// <summary>Boş hücre motif rengiyle boyalı + şekil işareti (öğrenme bölümleri, Kolay mod).</summary>
        Full,
        /// <summary>Yalnızca soluk şekil işareti; renk desen kartından okunur.</summary>
        Glyph,
        /// <summary>Tahta boş; desen yalnızca üstteki desen kartında görünür (ezberle ve dokuy).</summary>
        None,
    }

    /// <summary>
    /// Zorluk eğrisi (oyuncu geri bildirimi: ipuçları tahtada kalınca yerleştirme mekanikleşiyor).
    /// İpucu kademeli azalır; Kolay mod ve Günün Deseni her zaman tam ipucu gösterir. Yıldız kuralı değişmez.
    /// </summary>
    public static class HintPolicy
    {
        public const int GlyphFrom = 16;
        public const int NoneFrom = 31;

        public static HintLevel For(int levelId, bool easyMode, bool daily)
        {
            if (easyMode || daily || levelId < GlyphFrom) return HintLevel.Full;
            return levelId < NoneFrom ? HintLevel.Glyph : HintLevel.None;
        }

        /// <summary>Çözümden, tepside duran ve hücreleri hâlâ boş olan ilk adım (oyuncu saptıysa null → geri al önerilir).</summary>
        public static NextMove NextMove(LevelData level, GameSession s)
        {
            if (level.Solution == null) return null;
            foreach (var step in level.Solution)
            {
                int slot = -1;
                for (int i = 0; i < GameSession.TraySlots; i++)
                    if (s.Tray[i] != null && s.Tray[i].QueueIndex == step.QueueIndex) { slot = i; break; }
                if (slot < 0) continue;
                if (step.Cells.All(c => s.Board.OwnerAt(c.Row, c.Col) == Board.Empty)) return new NextMove { Slot = slot, Cells = step.Cells };
            }
            return null;
        }
    }
}
