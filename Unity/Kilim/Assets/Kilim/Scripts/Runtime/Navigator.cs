using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Kilim.Game
{
    /// <summary>Ekran temel sınıfı. Build kökün içine kurar; Tick her karede (sayaçlar için).</summary>
    public abstract class UiScreen
    {
        protected App App => App.I;
        public RectTransform Root { get; internal set; }
        public abstract void Build(RectTransform root);
        public virtual void Tick() { }
        public virtual void OnClose() { }
        /// <summary>Android geri tuşu. true = ele alındı.</summary>
        public virtual bool OnBack() => false;
    }

    /// <summary>Tek aktif ekran + üstünde modallar + toast. Ekran değişince eskisi yok edilir.</summary>
    public sealed class Navigator
    {
        private readonly App _app;
        private readonly RectTransform _screenLayer;
        private readonly RectTransform _modalLayer;
        private readonly RectTransform _toastLayer;
        private readonly RectTransform _overlay;
        private readonly List<RectTransform> _modals = new List<RectTransform>();
        public UiScreen Current { get; private set; }

        /// <summary>Sürüklenen parça gibi üst katman görselleri için (ekranın üstünde, modalın altında).</summary>
        public RectTransform Overlay => _overlay;

        public Navigator(App app, RectTransform safeRoot, RectTransform canvasRoot)
        {
            _app = app;
            _screenLayer = Ui.Rect(safeRoot, "Screens").Stretch();
            _overlay = Ui.Rect(canvasRoot, "Overlay").Stretch();
            _modalLayer = Ui.Rect(canvasRoot, "Modals").Stretch();
            _toastLayer = Ui.Rect(canvasRoot, "Toasts").Stretch();
            app.gameObject.AddComponent<NavigatorTicker>().Nav = this;
        }

        public bool HasModal => _modals.Count > 0;

        public void Show(UiScreen s)
        {
            CloseAllModals();
            if (Current != null)
            {
                Current.OnClose();
                UnityEngine.Object.Destroy(Current.Root.gameObject);
            }
            Ui.Clear(_overlay);
            var root = Ui.Rect(_screenLayer, s.GetType().Name).Stretch();
            s.Root = root;
            Current = s;
            s.Build(root);
        }

        internal void Tick()
        {
            Current?.Tick();
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (HasModal) return; // modal kendi butonlarıyla kapanır
                Current?.OnBack();
            }
        }

        /// <summary>Boş modal kartı açar; içeriği çağıran doldurur. Dönen kart kökü (VStack).</summary>
        public RectTransform Modal(bool dimTapCloses = false)
        {
            var dim = Ui.Panel(_modalLayer, "Modal", Theme.Dim);
            _modals.Add(dim);
            if (dimTapCloses)
            {
                // Ayrı kardeş "yakalayıcı": karta tıklama üst nesneye kabarıp modalı kapatmasın.
                var catcher = Ui.Box(dim, "Catcher", new Color(0, 0, 0, 0));
                ((RectTransform)catcher.transform).Stretch();
                catcher.gameObject.AddComponent<Button>().onClick.AddListener(() => Close(dim));
            }
            var card = Ui.Card(dim, 28, 48);
            card.anchorMin = new Vector2(0.06f, 0.5f);
            card.anchorMax = new Vector2(0.94f, 0.5f);
            card.pivot = new Vector2(0.5f, 0.5f);
            card.anchoredPosition = Vector2.zero;
            var fit = card.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return card;
        }

        public void Close(RectTransform modalOrCard)
        {
            var t = modalOrCard;
            while (t != null && !_modals.Contains(t)) t = t.parent as RectTransform;
            if (t == null) return;
            _modals.Remove(t);
            UnityEngine.Object.Destroy(t.gameObject);
        }

        public void CloseAllModals()
        {
            foreach (var m in _modals) if (m != null) UnityEngine.Object.Destroy(m.gameObject);
            _modals.Clear();
        }

        /// <summary>
        /// İki seçenekli onay. Etik kural (Spec 2 §7): ikinci buton (vazgeç/pes et) birinciyle eşit boyda.
        /// </summary>
        public void Confirm(string title, string body, string yes, Action onYes, string no, Action onNo)
        {
            var card = Modal();
            Ui.Label(card, title, 52, Theme.Ink, TextAnchor.MiddleCenter, true);
            if (!string.IsNullOrEmpty(body)) Ui.Label(card, body, 38, Theme.Muted, TextAnchor.MiddleCenter, false, -1);
            Ui.Button(card, yes, () => { Close(card); onYes?.Invoke(); });
            if (no != null) Ui.Button(card, no, () => { Close(card); onNo?.Invoke(); }, Theme.Neutral, Theme.Ink);
        }

        public void Choice(string title, string body, (string label, Action action)[] options)
        {
            var card = Modal();
            Ui.Label(card, title, 52, Theme.Ink, TextAnchor.MiddleCenter, true);
            if (!string.IsNullOrEmpty(body)) Ui.Label(card, body, 38, Theme.Muted);
            for (int i = 0; i < options.Length; i++)
            {
                var (label, action) = options[i];
                Ui.Button(card, label, () => { Close(card); action?.Invoke(); },
                    i == 0 ? Theme.Primary : Theme.Neutral, i == 0 ? Color.white : Theme.Ink);
            }
        }

        public void Toast(string text, float seconds = 2.2f)
        {
            var box = Ui.Box(_toastLayer, "Toast", new Color(0.17f, 0.14f, 0.12f, 0.92f));
            var rt = (RectTransform)box.transform;
            rt.anchorMin = new Vector2(0.08f, 0.1f);
            rt.anchorMax = new Vector2(0.92f, 0.1f);
            rt.sizeDelta = new Vector2(0, 130);
            box.raycastTarget = false;
            var t = Ui.Label(box.transform, text, 36, Color.white);
            ((RectTransform)t.transform).Stretch(24, 0, 24, 0);
            _app.Delay(seconds, () => { if (box != null) UnityEngine.Object.Destroy(box.gameObject); });
        }
    }

    public sealed class NavigatorTicker : MonoBehaviour
    {
        public Navigator Nav;
        private void Update() => Nav?.Tick();
    }
}
