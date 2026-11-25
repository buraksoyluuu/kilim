namespace Kilim.Core
{
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
        public const int GlyphFrom = 9;
        public const int NoneFrom = 25;

        public static HintLevel For(int levelId, bool easyMode, bool daily)
        {
            if (easyMode || daily || levelId < GlyphFrom) return HintLevel.Full;
            return levelId < NoneFrom ? HintLevel.Glyph : HintLevel.None;
        }
    }
}
