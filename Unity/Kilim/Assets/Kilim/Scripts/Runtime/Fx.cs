using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Kilim.Game
{
    public enum Ease { Linear, OutQuad, OutCubic, OutBack, InOutSine, OutElastic, InQuad }

    /// <summary>
    /// Hafif tween motoru (paket bağımlılığı yok). Her tween bir sahibe (UnityEngine.Object) bağlıdır; sahibi yok
    /// edilince sessizce düşer. Aynı anahtarla yeni tween başlarsa eskisi iptal edilir. Zaman ölçeksiz (duraklatmadan etkilenmez).
    /// </summary>
    public sealed class Fx : MonoBehaviour
    {
        private sealed class Tw
        {
            public UnityEngine.Object Owner;
            public string Key;
            public float T, Dur, Delay;
            public Ease Ease;
            public Action<float> Step;
            public Action Done;
        }

        private static Fx _i;
        private readonly List<Tw> _list = new List<Tw>();
        private readonly List<Tw> _add = new List<Tw>();

        private static Fx I
        {
            get
            {
                if (_i == null && App.I != null) _i = App.I.gameObject.AddComponent<Fx>();
                return _i;
            }
        }

        /// <summary>Genel tween: step(0..1 eased). key boş değilse aynı sahip+anahtardaki önceki tween iptal edilir.</summary>
        public static void To(UnityEngine.Object owner, float dur, Ease ease, Action<float> step, Action done = null, float delay = 0, string key = null)
        {
            var fx = I;
            if (fx == null || owner == null) { step?.Invoke(1); done?.Invoke(); return; }
            if (key != null)
            {
                foreach (var t in fx._list) if (t.Owner == owner && t.Key == key) t.Owner = null;
                foreach (var t in fx._add) if (t.Owner == owner && t.Key == key) t.Owner = null;
            }
            fx._add.Add(new Tw { Owner = owner, Key = key, Dur = Mathf.Max(0.0001f, dur), Delay = delay, Ease = ease, Step = step, Done = done });
        }

        public static void Kill(UnityEngine.Object owner)
        {
            if (_i == null) return;
            foreach (var t in _i._list) if (t.Owner == owner) t.Owner = null;
            foreach (var t in _i._add) if (t.Owner == owner) t.Owner = null;
        }

        private void Update()
        {
            if (_add.Count > 0) { _list.AddRange(_add); _add.Clear(); }
            float dt = Time.unscaledDeltaTime;
            for (int i = _list.Count - 1; i >= 0; i--)
            {
                var t = _list[i];
                if (t.Owner == null) { _list.RemoveAt(i); continue; }
                if (t.Delay > 0) { t.Delay -= dt; continue; }
                t.T += dt;
                float k = Mathf.Clamp01(t.T / t.Dur);
                try { t.Step?.Invoke(Apply(t.Ease, k)); }
                catch (Exception e) { Debug.LogWarning(e); t.Owner = null; continue; }
                if (k >= 1)
                {
                    _list.RemoveAt(i);
                    try { t.Done?.Invoke(); } catch (Exception e) { Debug.LogWarning(e); }
                }
            }
        }

        public static float Apply(Ease e, float x)
        {
            switch (e)
            {
                case Ease.OutQuad: return 1 - (1 - x) * (1 - x);
                case Ease.InQuad: return x * x;
                case Ease.OutCubic: { float a = 1 - x; return 1 - a * a * a; }
                case Ease.OutBack: { const float c1 = 1.70158f, c3 = c1 + 1; float a = x - 1; return 1 + c3 * a * a * a + c1 * a * a; }
                case Ease.InOutSine: return -(Mathf.Cos(Mathf.PI * x) - 1) / 2;
                case Ease.OutElastic:
                    if (x <= 0 || x >= 1) return x;
                    return Mathf.Pow(2, -10 * x) * Mathf.Sin((x * 10 - 0.75f) * (2 * Mathf.PI / 3)) + 1;
                default: return x;
            }
        }

        // ---------------- Hazır efektler ----------------

        public static void Scale(Transform t, float from, float to, float dur, Ease ease = Ease.OutBack, float delay = 0, Action done = null)
        {
            if (t == null) return;
            if (delay > 0) t.localScale = new Vector3(from, from, 1);
            To(t, dur, ease, k => { float s = from + (to - from) * k; t.localScale = new Vector3(s, s, 1); }, done, delay, "scale");
        }

        /// <summary>Kısa "zıplama": büyüyüp yerine oturur.</summary>
        public static void Pop(Transform t, float peak = 1.18f, float dur = 0.32f, float delay = 0)
        {
            if (t == null) return;
            To(t, dur, Ease.Linear, k =>
            {
                float s = k < 0.4f ? 1 + (peak - 1) * Fx.Apply(Ease.OutQuad, k / 0.4f) : peak + (1 - peak) * Fx.Apply(Ease.OutBack, (k - 0.4f) / 0.6f);
                t.localScale = new Vector3(s, s, 1);
            }, () => t.localScale = Vector3.one, delay, "scale");
        }

        /// <summary>Belirip büyüyerek gelme (0 → 1, OutBack).</summary>
        public static void PopIn(Transform t, float dur = 0.35f, float delay = 0)
        {
            if (t == null) return;
            t.localScale = new Vector3(0.001f, 0.001f, 1);
            Scale(t, 0.001f, 1f, dur, Ease.OutBack, delay);
        }

        /// <summary>Yatay titreme (geçersiz hamle).</summary>
        public static void Shake(RectTransform rt, float amp = 18, float dur = 0.35f)
        {
            if (rt == null) return;
            var start = rt.anchoredPosition;
            To(rt, dur, Ease.Linear, k =>
            {
                float d = Mathf.Sin(k * Mathf.PI * 6) * amp * (1 - k);
                rt.anchoredPosition = new Vector2(start.x + d, start.y);
            }, () => rt.anchoredPosition = start, 0, "shake");
        }

        public static void Move(RectTransform rt, Vector2 from, Vector2 to, float dur, Ease ease = Ease.OutCubic, float delay = 0, Action done = null)
        {
            if (rt == null) return;
            rt.anchoredPosition = from;
            To(rt, dur, ease, k => rt.anchoredPosition = Vector2.LerpUnclamped(from, to, k), done, delay, "move");
        }

        public static void Fade(CanvasGroup g, float from, float to, float dur, float delay = 0, Action done = null)
        {
            if (g == null) return;
            g.alpha = from;
            To(g, dur, Ease.OutQuad, k => g.alpha = from + (to - from) * k, done, delay, "fade");
        }

        public static void Tint(Graphic g, Color from, Color to, float dur, Ease ease = Ease.OutQuad, float delay = 0)
        {
            if (g == null) return;
            To(g, dur, ease, k => g.color = Color.Lerp(from, to, k), null, delay, "tint");
        }

        /// <summary>Sayacı yukarı say (coin, yüzde).</summary>
        public static void Count(Text t, int from, int to, float dur, Func<int, string> fmt, float delay = 0, Action<int> tick = null)
        {
            if (t == null) return;
            int last = int.MinValue;
            t.text = fmt(from);
            To(t, dur, Ease.OutCubic, k =>
            {
                int v = Mathf.RoundToInt(from + (to - from) * k);
                t.text = fmt(v);
                if (v != last) { last = v; tick?.Invoke(v); }
            }, null, delay, "count");
        }

        /// <summary>Sürekli "nefes alma" (ana buton). Sahibi yok olunca durur.</summary>
        public static void Breathe(Transform t, float amp = 0.035f, float period = 1.6f)
        {
            if (t == null) return;
            void Loop()
            {
                if (t == null) return;
                To(t, period, Ease.Linear, k => { float s = 1 + amp * Mathf.Sin(k * Mathf.PI * 2); t.localScale = new Vector3(s, s, 1); }, Loop, 0, "breathe");
            }
            Loop();
        }

        /// <summary>Yanıp sönen vurgu (Değiştir modu, sıradaki görev).</summary>
        public static void Pulse(Graphic g, Color a, Color b, float period = 0.9f)
        {
            if (g == null) return;
            void Loop()
            {
                if (g == null) return;
                To(g, period, Ease.Linear, k => g.color = Color.Lerp(a, b, (1 - Mathf.Cos(k * Mathf.PI * 2)) / 2), Loop, 0, "pulse");
            }
            Loop();
        }

        /// <summary>Yükselip kaybolan yazı ("+3", "Desen!").</summary>
        public static void FloatText(Transform layer, Vector2 anchoredPos, string text, Color color, int size = 56)
        {
            if (layer == null) return;
            var t = Ui.Label(layer, text, size, color, TextAnchor.MiddleCenter, true);
            var rt = (RectTransform)t.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(600, 100);
            var sh = t.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0, 0, 0, 0.35f);
            sh.effectDistance = new Vector2(0, -4);
            var g = t.gameObject.AddComponent<CanvasGroup>();
            Move(rt, anchoredPos, anchoredPos + new Vector2(0, 140), 0.9f, Ease.OutCubic);
            Scale(rt, 0.6f, 1f, 0.3f, Ease.OutBack);
            Fade(g, 1, 0, 0.35f, 0.6f, () => { if (t != null) Destroy(t.gameObject); });
        }

        /// <summary>Konfeti: renkli küçük kilim karoları yukarı saçılıp düşer.</summary>
        public static void Confetti(RectTransform layer, Vector2 origin, int count = 46, float power = 1f)
        {
            if (layer == null) return;
            string[] colors = { "K", "L", "S", "Z", "M" };
            for (int i = 0; i < count; i++)
            {
                var img = Ui.Box(layer, "Confetti", Theme.Motif(colors[i % colors.Length]));
                img.raycastTarget = false;
                var rt = (RectTransform)img.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                float size = UnityEngine.Random.Range(18f, 34f);
                rt.sizeDelta = new Vector2(size, size * UnityEngine.Random.Range(0.6f, 1.2f));
                float ang = UnityEngine.Random.Range(-Mathf.PI * 0.95f, -Mathf.PI * 0.05f) * -1; // yukarı yarım daire
                float speed = UnityEngine.Random.Range(900f, 1700f) * power;
                var v = new Vector2(Mathf.Cos(ang) * speed, Mathf.Sin(ang) * speed);
                float spin = UnityEngine.Random.Range(-720f, 720f);
                float life = UnityEngine.Random.Range(1.3f, 1.9f);
                var start = origin;
                To(rt, life, Ease.Linear, k =>
                {
                    float tt = k * life;
                    var p = start + new Vector2(v.x * tt, v.y * tt - 0.5f * 2600f * tt * tt);
                    rt.anchoredPosition = p;
                    rt.localRotation = Quaternion.Euler(0, 0, spin * tt);
                    var c = img.color; c.a = k < 0.75f ? 1 : 1 - (k - 0.75f) / 0.25f; img.color = c;
                }, () => { if (rt != null) Destroy(rt.gameObject); }, i * 0.004f);
            }
        }
    }

    /// <summary>
    /// Buton basma hissi: basılınca küçülür ve hafif kararır, bırakınca esneyerek geri gelir; hafif titreşim.
    /// Ui.Button her butona ekler.
    /// </summary>
    public sealed class PressFx : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public float Pressed = 0.93f;
        private bool _down;
        private Button _btn;

        private void Awake() => _btn = GetComponent<Button>();

        public void OnPointerDown(PointerEventData e)
        {
            if (_btn != null && !_btn.interactable) return;
            _down = true;
            Fx.Scale(transform, transform.localScale.x, Pressed, 0.08f, Ease.OutQuad);
            Haptic.Tick();
        }

        public void OnPointerUp(PointerEventData e) => Release();
        public void OnPointerExit(PointerEventData e) => Release();

        private void Release()
        {
            if (!_down) return;
            _down = false;
            Fx.Scale(transform, transform.localScale.x, 1f, 0.28f, Ease.OutBack);
        }

        private void OnDisable() { _down = false; transform.localScale = Vector3.one; }
    }

    /// <summary>
    /// Dokunsal geri bildirim. Android: kısa titreşim (ms); iOS: Unity yalnızca uzun titreşim verir, bu yüzden
    /// iOS'ta yalnızca güçlü anlarda (kazanma) kullanılır. Ayarlar › Titreşim kapalıysa hiçbir şey yapmaz.
    /// </summary>
    public static class Haptic
    {
        public static void Tick() => Vibrate(12);
        public static void Light() => Vibrate(22);
        public static void Medium() => Vibrate(40);

        public static void Success()
        {
            if (!Audio.Haptics) return;
#if UNITY_IOS && !UNITY_EDITOR
            Handheld.Vibrate();
#else
            Vibrate(70);
#endif
        }

        /// <summary>Android VIBRATE iznini derleme çıktısında tutar (Unity izni Handheld.Vibrate çağrısından ekler).</summary>
        private static void KeepVibratePermission() => Handheld.Vibrate();

        private static void Vibrate(long ms)
        {
            if (!Audio.Haptics) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var vib = activity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
                {
                    vib?.Call("vibrate", ms);
                }
            }
            catch (Exception) { /* titreşim yoksa sessiz */ }
#endif
        }
    }
}
