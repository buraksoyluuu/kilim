using System;
using UnityEngine;
using UnityEngine.UI;

namespace Kilim.Game
{
    /// <summary>Renk paleti ve motif işaretleri (renk körlüğü için her rengin bir şekli var, UAT A11Y-01).</summary>
    public static class Theme
    {
        public static readonly Color Bg = Hex("F6EFE3");
        public static readonly Color Panel = Hex("FFFFFF");
        public static readonly Color Ink = Hex("2B2420");
        public static readonly Color Muted = Hex("8A7F74");
        public static readonly Color Primary = Hex("B23A2E");
        public static readonly Color PrimaryDark = Hex("8E2C22");
        public static readonly Color Gold = Hex("D9A441");
        public static readonly Color Ok = Hex("4E7D4A");
        public static readonly Color Neutral = Hex("E8DFD1");
        public static readonly Color BoardBg = Hex("5B4636");
        public static readonly Color CellEmpty = Hex("EADFCB");
        public static readonly Color Dim = new Color(0.12f, 0.08f, 0.06f, 0.62f);
        public static readonly Color Cream = Hex("FFF8EC");
        public static readonly Color Sand = Hex("F1E4CC");

        /// <summary>Rengin koyu tonu (buton gölgesi, basılı hâl).</summary>
        public static Color Darker(Color c, float k = 0.72f) => new Color(c.r * k, c.g * k, c.b * k, c.a);

        public static Color Motif(string code)
        {
            switch (code)
            {
                case "K": return Hex("B23A2E");   // kırmızı
                case "L": return Hex("26385F");   // lacivert
                case "S": return Hex("D9A23A");   // safran
                case "Z": return Hex("6B7A2E");   // zeytin
                case "M": return Hex("6E3A5E");   // mürdüm
                default: return Hex("9C948A");    // Tek İlmek / bilinmeyen
            }
        }

        public static string Glyph(string code)
        {
            switch (code)
            {
                case "K": return "●";
                case "L": return "■";
                case "S": return "▲";
                case "Z": return "◆";
                case "M": return "✚";
                default: return "·";
            }
        }

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }

        public static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);
    }

    /// <summary>Koddan uGUI kurma yardımcıları. Tüm ekranlar bunlarla kurulur; sahne/prefab gerekmez.</summary>
    public static class Ui
    {
        private static Font _font, _bold;
        /// <summary>Kilim Sans (Poppins + semboller, Resources/fonts). Yoksa Unity'nin varsayılanı.</summary>
        public static Font Font => _font != null ? _font : (_font = Resources.Load<Font>("fonts/KilimSans-Medium") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
        public static Font Bold => _bold != null ? _bold : (_bold = Resources.Load<Font>("fonts/KilimSans-Bold") ?? Font);

        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static RectTransform Stretch(this RectTransform rt, float l = 0, float t = 0, float r = 0, float b = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(l, b);
            rt.offsetMax = new Vector2(-r, -t);
            return rt;
        }

        public static RectTransform Panel(Transform parent, string name, Color color)
        {
            var rt = Rect(parent, name).Stretch();
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return rt;
        }

        /// <summary>Yuvarlatılmış kilim karosu (9-dilim). mult büyüdükçe köşe küçülür.</summary>
        public static Image Round(Image img, float mult = 1.5f, Sprite sprite = null)
        {
            img.sprite = sprite ?? Art.Tile;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = mult;
            return img;
        }

        /// <summary>Kilim kenar bandı (ekran üstü süsü).</summary>
        public static void Band(Transform parent, float h = 44)
        {
            var img = Box(parent, "Band", Color.white, -1, h);
            img.sprite = Art.Band;
            img.type = Image.Type.Tiled;
            img.pixelsPerUnitMultiplier = 64f / h;
            img.raycastTarget = false;
        }

        public static Image Icon(Transform parent, Sprite s, float size, Color? tint = null)
        {
            var rt = Rect(parent, "Icon");
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = s;
            img.color = tint ?? Color.white;
            img.preserveAspect = true;
            img.raycastTarget = false;
            Size(rt.gameObject, size, size, 0);
            return img;
        }

        public static Image Box(Transform parent, string name, Color color, float w = -1, float h = -1)
        {
            var rt = Rect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            Round(img);
            if (w >= 0 || h >= 0) Size(rt.gameObject, w, h);
            return img;
        }

        public static Text Label(Transform parent, string text, int size = 40, Color? color = null,
                                 TextAnchor align = TextAnchor.MiddleCenter, bool bold = false, float h = -1)
        {
            var rt = Rect(parent, "Text");
            var t = rt.gameObject.AddComponent<Text>();
            var boldFont = Bold;
            t.font = bold ? boldFont : Font;
            t.text = Loc.T(text);
            // Küçük metinler telefonda okunmuyordu (oyuncu videosu): 40 altı boyutlar %12 büyütülür.
            t.fontSize = size < 40 ? Mathf.RoundToInt(size * 1.12f) : size;
            t.color = color ?? Theme.Ink;
            t.alignment = align;
            // Kalın font dosyası varsa sentetik kalın kullanılmaz (daha temiz kenar).
            t.fontStyle = bold && boldFont == Font ? FontStyle.Bold : FontStyle.Normal;
            t.lineSpacing = 0.92f;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            // h > 0: sabit yükseklik; aksi halde Text kendi tercih edilen yüksekliğini verir (çok satırlı metin taşmaz).
            if (h > 0) Size(rt.gameObject, -1, h);
            return t;
        }

        /// <summary>
        /// Kabartmalı buton: alt kenarda koyu gölge (3B his), basınca küçülür (PressFx), tıklama sesi + hafif titreşim.
        /// </summary>
        public static Button Button(Transform parent, string text, Action onClick, Color? bg = null,
                                    Color? fg = null, float h = 130, int fontSize = 44, float w = -1)
        {
            var color = bg ?? Theme.Primary;
            var img = Box(parent, "Button:" + text, color, w, h);
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.pressedColor = new Color(0.88f, 0.88f, 0.88f, 1);
            colors.highlightedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(1, 1, 1, 0.55f);
            colors.fadeDuration = 0.06f;
            btn.colors = colors;
            Depth(img, color);
            img.gameObject.AddComponent<PressFx>();
            var label = Label(img.transform, text, fontSize, fg ?? Color.white, TextAnchor.MiddleCenter, true);
            ((RectTransform)label.transform).Stretch(16, 0, 16, 6);
            btn.onClick.AddListener(() => { Audio.Click(); onClick?.Invoke(); });
            return btn;
        }

        /// <summary>Alt kenar gölgesi: rengin koyu tonu 8 px aşağıda (kabartma hissi).</summary>
        public static Shadow Depth(Graphic g, Color baseColor, float dist = 9)
        {
            var sh = g.gameObject.AddComponent<Shadow>();
            sh.effectColor = baseColor.a < 0.5f ? new Color(0, 0, 0, 0.08f) : Theme.WithAlpha(Theme.Darker(baseColor, 0.7f), 0.9f);
            sh.effectDistance = new Vector2(0, -dist);
            return sh;
        }

        /// <summary>Yumuşak kart gölgesi.</summary>
        public static void Soft(Graphic g, float dist = 10)
        {
            var sh = g.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0.35f, 0.22f, 0.12f, 0.16f);
            sh.effectDistance = new Vector2(0, -dist);
        }

        /// <summary>Simge + metin hap (coin, ilmek, kalan parça).</summary>
        public static Text Pill(Transform parent, Sprite icon, string text, float w = 230, float h = 86, Color? bg = null)
        {
            var pill = Box(parent, "Pill", bg ?? Theme.Panel, w, h);
            Round(pill, 1f, Art.Pill);
            Soft(pill, 6);
            var ic = Icon(pill.transform, icon, h - 22);
            var irt = (RectTransform)ic.transform;
            irt.anchorMin = irt.anchorMax = new Vector2(0, 0.5f);
            irt.pivot = new Vector2(0, 0.5f);
            irt.sizeDelta = new Vector2(h - 22, h - 22);
            irt.anchoredPosition = new Vector2(10, 0);
            var t = Label(pill.transform, text, (int)(h * 0.46f), Theme.Ink, TextAnchor.MiddleCenter, true);
            ((RectTransform)t.transform).Stretch(h - 8, 0, 14, 0);
            return t;
        }

        /// <summary>Küçük sayaç rozeti (sağ üst köşe).</summary>
        public static Text Badge(Transform parent, string text, Color? bg = null)
        {
            var b = Box(parent, "Badge", bg ?? Theme.Primary);
            Round(b, 1f, Art.Pill);
            var rt = (RectTransform)b.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.75f, 0.75f);
            rt.sizeDelta = new Vector2(70, 52);
            b.raycastTarget = false;
            var t = Label(b.transform, text, 30, Color.white, TextAnchor.MiddleCenter, true);
            ((RectTransform)t.transform).Stretch();
            return t;
        }

        public static Text ButtonLabel(Button b) => b.GetComponentInChildren<Text>();

        public static void SetEnabled(Button b, bool on)
        {
            b.interactable = on;
            var c = b.targetGraphic.color;
            b.targetGraphic.color = new Color(c.r, c.g, c.b, on ? 1f : 0.45f);
            var label = b.GetComponentInChildren<Text>();
            if (label != null) { var lc = label.color; label.color = new Color(lc.r, lc.g, lc.b, on ? 1f : 0.6f); }
        }

        public static LayoutElement Size(GameObject go, float w, float h, float flexW = -1, float flexH = -1)
        {
            var le = go.GetComponent<LayoutElement>();
            if (le == null) le = go.AddComponent<LayoutElement>();
            if (w >= 0) le.preferredWidth = w;
            if (h >= 0) le.preferredHeight = h;
            if (flexW >= 0) le.flexibleWidth = flexW;
            if (flexH >= 0) le.flexibleHeight = flexH;
            return le;
        }

        public static RectTransform VStack(Transform parent, float spacing = 24, int pad = 0,
                                           TextAnchor align = TextAnchor.UpperCenter, string name = "VStack")
        {
            var rt = Rect(parent, name);
            var g = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            g.spacing = spacing;
            g.padding = new RectOffset(pad, pad, pad, pad);
            g.childAlignment = align;
            g.childControlWidth = true;
            g.childControlHeight = true;
            g.childForceExpandWidth = true;
            g.childForceExpandHeight = false;
            return rt;
        }

        public static RectTransform HStack(Transform parent, float spacing = 24, int pad = 0,
                                           TextAnchor align = TextAnchor.MiddleCenter, float h = -1, string name = "HStack")
        {
            var rt = Rect(parent, name);
            var g = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            g.spacing = spacing;
            g.padding = new RectOffset(pad, pad, pad, pad);
            g.childAlignment = align;
            g.childControlWidth = true;
            g.childControlHeight = true;
            g.childForceExpandWidth = true;
            g.childForceExpandHeight = true;
            // Sabit yükseklikli satır esnemez: HorizontalLayoutGroup forceExpand yüzünden kendini esnek bildirir,
            // dikey yığında boş alanı paylaşıp uzuyordu (başlıkta dev beyaz sütunlar, uzun noktalar).
            if (h >= 0) Size(rt.gameObject, -1, h, -1, 0);
            return rt;
        }

        public static RectTransform Card(Transform parent, float spacing = 16, int pad = 32, Color? bg = null)
        {
            var rt = VStack(parent, spacing, pad, TextAnchor.UpperCenter, "Card");
            var img = rt.gameObject.AddComponent<Image>();
            img.color = bg ?? Theme.Panel;
            Round(img, 1.2f);
            Soft(img);
            return rt;
        }

        /// <summary>Dikey kaydırılabilir liste; içerik VStack döner.</summary>
        public static RectTransform Scroll(Transform parent, float spacing = 24, int pad = 32)
        {
            var view = Rect(parent, "Scroll");
            Size(view.gameObject, -1, -1, 1, 1);
            view.gameObject.AddComponent<RectMask2D>();
            var sr = view.gameObject.AddComponent<ScrollRect>();
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped;
            var content = VStack(view, spacing, pad, TextAnchor.UpperCenter, "Content");
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            var fit = content.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.content = content;
            sr.viewport = view;
            return content;
        }

        /// <summary>Üst başlık çubuğu: geri butonu + başlık + sağda coin.</summary>
        public static RectTransform Header(Transform parent, string title, Action onBack, App app = null)
        {
            Band(parent);
            var bar = HStack(parent, 16, 24, TextAnchor.MiddleLeft, 150, "Header");
            var g = bar.GetComponent<HorizontalLayoutGroup>();
            g.childForceExpandWidth = false;
            if (onBack != null) Button(bar, "‹", onBack, Theme.Panel, Theme.Ink, 100, 72, 100);
            var t = Label(bar, title, 52, Theme.Ink, TextAnchor.MiddleLeft, true);
            Size(t.gameObject, -1, 100, 1);
            if (app != null) CoinPill(bar, app);
            return bar;
        }

        public static Text CoinPill(Transform parent, App app)
        {
            var t = Pill(parent, Art.Coin, Fmt.N(app.Meta.Wallet.Coins), 260, 86);
            int shown = app.Meta.Wallet.Coins;
            void Refresh()
            {
                if (t == null) return;
                int now = app.Meta.Wallet.Coins;
                if (now == shown) return;
                Fx.Count(t, shown, now, 0.6f, Fmt.N);
                Fx.Pop(t.transform.parent, 1.08f);
                shown = now;
            }
            app.Meta.Wallet.Changed += Refresh;
            t.gameObject.AddComponent<OnDestroyHook>().Action = () => app.Meta.Wallet.Changed -= Refresh;
            return t;
        }

        public static Text IlmekPill(Transform parent, App app)
        {
            var t = Pill(parent, Art.Ilmek, app.Meta.Wallet.Ilmek.ToString(), 190, 86);
            int shown = app.Meta.Wallet.Ilmek;
            void Refresh()
            {
                if (t == null) return;
                int now = app.Meta.Wallet.Ilmek;
                if (now == shown) return;
                Fx.Count(t, shown, now, 0.5f, v => v.ToString());
                Fx.Pop(t.transform.parent, 1.1f);
                shown = now;
            }
            app.Meta.Wallet.Changed += Refresh;
            t.gameObject.AddComponent<OnDestroyHook>().Action = () => app.Meta.Wallet.Changed -= Refresh;
            return t;
        }

        public static void Spacer(Transform parent, float h = -1, float flex = 1)
        {
            var rt = Rect(parent, "Spacer");
            Size(rt.gameObject, -1, h >= 0 ? h : 0, -1, h >= 0 ? -1 : flex);
        }

        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(t.GetChild(i).gameObject);
        }

        /// <summary>İlerleme çubuğu; Set(0..1).</summary>
        public static Action<float> Bar(Transform parent, float h, Color fill, Color bg)
        {
            var back = Box(parent, "Bar", bg, -1, h);
            Round(back, 1f, Art.Pill);
            var f = Box(back.transform, "Fill", fill);
            Round(f, 1f, Art.Pill);
            var frt = (RectTransform)f.transform;
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = new Vector2(0, 1);
            frt.offsetMin = Vector2.zero;
            frt.offsetMax = Vector2.zero;
            float shown = 0;
            return v =>
            {
                if (frt == null) return;
                float from = shown, to = Mathf.Clamp01(v);
                shown = to;
                // İnce doluluk: hücre kadar kısa çubuk görünmesin diye en az yükseklik kadar genişlik.
                Fx.To(frt, 0.45f, Ease.OutCubic, k => frt.anchorMax = new Vector2(Mathf.Lerp(from, to, k), 1), null, 0, "bar");
            };
        }
    }

    public sealed class OnDestroyHook : MonoBehaviour
    {
        public Action Action;
        private void OnDestroy() => Action?.Invoke();
    }

    public static class Fmt
    {
        public static string N(int n) => n.ToString("N0", Loc.Culture);

        public static string Span(TimeSpan t)
        {
            bool tr = Loc.Lang == "tr";
            if (t.TotalDays >= 1) return tr ? $"{(int)t.TotalDays}g {t.Hours}s" : $"{(int)t.TotalDays}d {t.Hours}h";
            if (t.TotalHours >= 1) return $"{(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00}";
            return $"{t.Minutes:00}:{t.Seconds:00}";
        }
    }
}
