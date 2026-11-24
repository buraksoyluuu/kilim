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
        public static readonly Color Dim = new Color(0, 0, 0, 0.55f);

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
        private static Font _font;
        public static Font Font => _font != null ? _font : (_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

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

        public static Image Box(Transform parent, string name, Color color, float w = -1, float h = -1)
        {
            var rt = Rect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            if (w >= 0 || h >= 0) Size(rt.gameObject, w, h);
            return img;
        }

        public static Text Label(Transform parent, string text, int size = 40, Color? color = null,
                                 TextAnchor align = TextAnchor.MiddleCenter, bool bold = false, float h = -1)
        {
            var rt = Rect(parent, "Text");
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = color ?? Theme.Ink;
            t.alignment = align;
            t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            // h > 0: sabit yükseklik; aksi halde Text kendi tercih edilen yüksekliğini verir (çok satırlı metin taşmaz).
            if (h > 0) Size(rt.gameObject, -1, h);
            return t;
        }

        public static Button Button(Transform parent, string text, Action onClick, Color? bg = null,
                                    Color? fg = null, float h = 130, int fontSize = 44, float w = -1)
        {
            var img = Box(parent, "Button:" + text, bg ?? Theme.Primary, w, h);
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var label = Label(img.transform, text, fontSize, fg ?? Color.white, TextAnchor.MiddleCenter, true);
            ((RectTransform)label.transform).Stretch(16, 0, 16, 0);
            btn.onClick.AddListener(() => { Audio.Click(); onClick?.Invoke(); });
            return btn;
        }

        public static Text ButtonLabel(Button b) => b.GetComponentInChildren<Text>();

        public static void SetEnabled(Button b, bool on)
        {
            b.interactable = on;
            var c = b.targetGraphic.color;
            b.targetGraphic.color = new Color(c.r, c.g, c.b, on ? 1f : 0.4f);
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
            if (h >= 0) Size(rt.gameObject, -1, h);
            return rt;
        }

        public static RectTransform Card(Transform parent, float spacing = 16, int pad = 32, Color? bg = null)
        {
            var rt = VStack(parent, spacing, pad, TextAnchor.UpperCenter, "Card");
            var img = rt.gameObject.AddComponent<Image>();
            img.color = bg ?? Theme.Panel;
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
            var bar = HStack(parent, 16, 24, TextAnchor.MiddleLeft, 150, "Header");
            var g = bar.GetComponent<HorizontalLayoutGroup>();
            g.childForceExpandWidth = false;
            if (onBack != null) Button(bar, "‹", onBack, Theme.Neutral, Theme.Ink, 100, 64, 100);
            var t = Label(bar, title, 52, Theme.Ink, TextAnchor.MiddleLeft, true);
            Size(t.gameObject, -1, 100, 1);
            if (app != null) CoinPill(bar, app);
            return bar;
        }

        public static Text CoinPill(Transform parent, App app)
        {
            var pill = Box(parent, "Coins", Theme.Neutral, 260, 90);
            var t = Label(pill.transform, "", 40, Theme.Ink, TextAnchor.MiddleCenter, true);
            ((RectTransform)t.transform).Stretch();
            void Refresh() { if (t != null) t.text = $"● {Fmt.N(app.Meta.Wallet.Coins)}"; }
            Refresh();
            app.Meta.Wallet.Changed += Refresh;
            pill.gameObject.AddComponent<OnDestroyHook>().Action = () => app.Meta.Wallet.Changed -= Refresh;
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
            var f = Box(back.transform, "Fill", fill);
            var frt = (RectTransform)f.transform;
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = new Vector2(0, 1);
            frt.offsetMin = Vector2.zero;
            frt.offsetMax = Vector2.zero;
            return v => { if (frt != null) frt.anchorMax = new Vector2(Mathf.Clamp01(v), 1); };
        }
    }

    public sealed class OnDestroyHook : MonoBehaviour
    {
        public Action Action;
        private void OnDestroy() => Action?.Invoke();
    }

    public static class Fmt
    {
        private static readonly System.Globalization.CultureInfo Tr = new System.Globalization.CultureInfo("tr-TR");
        public static string N(int n) => n.ToString("N0", Tr);

        public static string Span(TimeSpan t)
        {
            if (t.TotalDays >= 1) return $"{(int)t.TotalDays}g {t.Hours}s";
            if (t.TotalHours >= 1) return $"{(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00}";
            return $"{t.Minutes:00}:{t.Seconds:00}";
        }
    }
}
