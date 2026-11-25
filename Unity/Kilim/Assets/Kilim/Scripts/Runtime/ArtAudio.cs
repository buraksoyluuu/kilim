using System.Collections.Generic;
using UnityEngine;

namespace Kilim.Game
{
    /// <summary>
    /// Görseller: Resources/art/*.png (Tools/gen_assets.py üretir). Dokular varsayılan içe aktarımla yüklenir,
    /// sprite çalışma anında kesilir (9-dilim kenarlarıyla) → .meta ayarı gerekmez.
    /// Final illüstrasyonlar geldiğinde aynı adlarla değiştirmek yeterli.
    /// </summary>
    public static class Art
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite Get(string name, float border = 0)
        {
            if (Cache.TryGetValue(name, out var s)) return s;
            var tex = Resources.Load<Texture2D>("art/" + name);
            if (tex == null) { Cache[name] = null; return null; }
            s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(border, border, border, border));
            Cache[name] = s;
            return s;
        }

        public static Sprite Tile => Get("tile", 40);
        public static Sprite TileFlat => Get("tile_flat", 40);
        public static Sprite Pill => Get("pill", 52);
        public static Sprite Star => Get("star");
        public static Sprite Coin => Get("coin");
        public static Sprite Ilmek => Get("ilmek");
        public static Sprite Lock => Get("lock");
        public static Sprite Nine => Get("nine");
        public static Sprite Pattern => Get("pattern_bg");
        public static Sprite Band => Get("band");
        public static Sprite Bubble => Get("bubble", 48);

        /// <summary>Arayüz simgesi (beyaz, çalışma anında renklendirilir): Resources/art/icon_{name}.png.</summary>
        public static Sprite Icon(string name) => Get("icon_" + name);

        /// <summary>Ev alanı illüstrasyonu: eski (bakımsız) ya da yeni (onarılmış) hâli.</summary>
        public static Sprite Area(string key, bool restored) => Get($"area_{key}_{(restored ? "new" : "old")}");

        /// <summary>Renk körlüğü için her motif renginin şekli (UAT A11Y-01).</summary>
        public static Sprite Motif(string code)
        {
            switch (code)
            {
                case "K": return Get("motif_circle");
                case "L": return Get("motif_square");
                case "S": return Get("motif_triangle");
                case "Z": return Get("motif_diamond");
                case "M": return Get("motif_cross");
                default: return null;
            }
        }
    }

    /// <summary>
    /// Ses: Resources/audio/*.wav (Tools/gen_assets.py; telli çalgı + çan sentezi, telifsiz).
    /// Müzik ve efekt ayrı kaynak; Ayarlar'daki anahtarlar anında uygulanır.
    /// </summary>
    public static class Audio
    {
        public static bool Sfx = true;
        public static bool Haptics = true;

        private static AudioSource _sfx, _music;
        private static readonly Dictionary<string, AudioClip> Clips = new Dictionary<string, AudioClip>();

        public static void Init(GameObject host)
        {
            _sfx = host.AddComponent<AudioSource>();
            _sfx.playOnAwake = false;
            _music = host.AddComponent<AudioSource>();
            _music.playOnAwake = false;
            _music.loop = true;
            _music.volume = 0.35f;
        }

        private static AudioClip Clip(string name)
        {
            if (!Clips.TryGetValue(name, out var c)) Clips[name] = c = Resources.Load<AudioClip>("audio/" + name);
            return c;
        }

        public static void Play(string name, float volume = 1f)
        {
            if (!Sfx || _sfx == null) return;
            var c = Clip(name);
            if (c != null) _sfx.PlayOneShot(c, volume);
        }

        public static void SetMusic(bool on)
        {
            if (_music == null) return;
            if (on)
            {
                if (_music.clip == null) _music.clip = Clip("music_loop");
                if (_music.clip != null && !_music.isPlaying) _music.Play();
            }
            else _music.Stop();
        }

        public static void Click() => Play("click", 0.6f);

        /// <summary>Yerleşim sesi (titreşim Haptic sınıfında, kısa ve hafif).</summary>
        public static void Place(bool matched) => Play(matched ? "match" : "place");
    }
}
