// Unity API'sinin oyunda kullanılan alt kümesi için sahte (stub) tanımlar.
// Amaç: Unity Editor olmadan CI'da Kilim.Game kodunu tip kontrolünden geçirmek. Çalışma zamanı davranışı yoktur.
#pragma warning disable CS0067, CS0649, CS0414, CS1998
using System;
using System.Collections;

namespace UnityEngine
{
    public class Object
    {
        public string name;
        public static void Destroy(Object o) { }
        public static void DontDestroyOnLoad(Object o) { }
        public static T FindObjectOfType<T>() where T : Object => null;
        public static implicit operator bool(Object o) => o != null;
    }
    public class Component : Object
    {
        public GameObject gameObject => null;
        public Transform transform => null;
        public T GetComponent<T>() => default;
        public T GetComponentInChildren<T>() => default;
    }
    public class Behaviour : Component { public bool enabled; }
    public class MonoBehaviour : Behaviour
    {
        public Coroutine StartCoroutine(IEnumerator r) => null;
    }
    public sealed class Coroutine { }
    public class YieldInstruction { }
    public sealed class WaitForSecondsRealtime : CustomYieldInstruction { public WaitForSecondsRealtime(float s) { } public override bool keepWaiting => false; }
    public abstract class CustomYieldInstruction : IEnumerator
    {
        public abstract bool keepWaiting { get; }
        public object Current => null; public bool MoveNext() => keepWaiting; public void Reset() { }
    }
    public sealed class GameObject : Object
    {
        public GameObject(string name, params Type[] components) { }
        public Transform transform => null;
        public T AddComponent<T>() where T : Component => default;
        public T GetComponent<T>() => default;
        public void SetActive(bool v) { }
    }
    public class Transform : Component, IEnumerable
    {
        public Transform parent => null;
        public int childCount => 0;
        public Vector3 localScale;
        public Transform GetChild(int i) => null;
        public void SetParent(Transform p, bool worldPositionStays) { }
        public void SetAsFirstSibling() { }
        public void SetAsLastSibling() { }
        public void SetSiblingIndex(int i) { }
        public int GetSiblingIndex() => 0;
        public IEnumerator GetEnumerator() => null;
    }
    public sealed class RectTransform : Transform
    {
        public Vector2 anchorMin, anchorMax, offsetMin, offsetMax, sizeDelta, anchoredPosition, pivot;
        public Rect rect => default;
    }
    public struct Rect
    {
        public Rect(float x, float y, float w, float h) { xMin = x; yMin = y; xMax = x + w; yMax = y + h; }
        public float xMin, yMin, xMax, yMax;
        public Vector2 size => default;
        public static bool operator ==(Rect a, Rect b) => true;
        public static bool operator !=(Rect a, Rect b) => false;
        public override bool Equals(object o) => true;
        public override int GetHashCode() => 0;
    }
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero => default; public static Vector2 one => default;
        public static Vector2 operator +(Vector2 a, Vector2 b) => default;
        public static Vector2 operator *(Vector2 a, float b) => default;
    }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 one => default;
    }
    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a = 1) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white => default; public static Color black => default; public static Color clear => default;
        public static Color Lerp(Color a, Color b, float t) => default;
    }
    public static class ColorUtility { public static bool TryParseHtmlString(string s, out Color c) { c = default; return true; } }
    public static class Mathf
    {
        public static float Clamp01(float v) => v; public static float Min(float a, float b) => a; public static int Min(int a, int b) => a;
        public static float Max(float a, float b) => a; public static int Max(int a, int b) => a; public static int RoundToInt(float f) => 0;
        public static float Clamp(float v, float a, float b) => v;
    }
    public static class Debug
    {
        public static void Log(object o) { } public static void LogWarning(object o) { } public static void LogError(object o) { }
    }
    public enum SystemLanguage { Turkish, English, Unknown }
    public struct Vector4 { public Vector4(float x, float y, float z, float w) { } }
    public class Texture : Object { public int width => 0; public int height => 0; }
    public sealed class Texture2D : Texture { }
    public enum SpriteMeshType { FullRect, Tight }
    public sealed class Sprite : Object
    {
        public static Sprite Create(Texture2D t, Rect r, Vector2 pivot, float ppu, uint extrude, SpriteMeshType m, Vector4 border) => null;
    }
    public sealed class AudioClip : Object { }
    public sealed class AudioSource : Behaviour
    {
        public bool playOnAwake, loop; public float volume; public AudioClip clip; public bool isPlaying => false;
        public void Play() { } public void Stop() { } public void PlayOneShot(AudioClip c, float v) { }
    }
    public static class Application
    {
        public static SystemLanguage systemLanguage => SystemLanguage.Turkish; public static bool isEditor => true;
        public static string persistentDataPath => ""; public static string version => "";
        public static int targetFrameRate; public static void OpenURL(string u) { }
    }
    public static class Screen
    {
        public static int width => 0; public static int height => 0; public static Rect safeArea => default; public static int sleepTimeout;
    }
    public static class SleepTimeout { public const int NeverSleep = -1; }
    public static class Handheld { public static void Vibrate() { } }
    public static class Input { public static bool GetKeyDown(KeyCode k) => false; }
    public enum KeyCode { Escape }
    public class TextAsset : Object { public string text => ""; }
    public class Font : Object { }
    public static class Resources
    {
        public static T Load<T>(string path) where T : Object => null;
        public static T GetBuiltinResource<T>(string path) where T : Object => null;
    }
    public enum RenderMode { ScreenSpaceOverlay, ScreenSpaceCamera, WorldSpace }
    public sealed class Canvas : Behaviour { public RenderMode renderMode; public int sortingOrder; }
    public sealed class Camera : Behaviour { }
    public static class RectTransformUtility
    {
        public static bool ScreenPointToLocalPointInRectangle(RectTransform r, Vector2 p, Camera c, out Vector2 local) { local = default; return true; }
    }
    public enum TextAnchor { UpperLeft, UpperCenter, UpperRight, MiddleLeft, MiddleCenter, MiddleRight, LowerLeft, LowerCenter, LowerRight }
    public enum FontStyle { Normal, Bold, Italic, BoldAndItalic }
    public enum HorizontalWrapMode { Wrap, Overflow }
    public enum VerticalWrapMode { Truncate, Overflow }
    public sealed class RectOffset { public RectOffset(int l, int r, int t, int b) { } }
    public enum RuntimeInitializeLoadType { AfterSceneLoad, BeforeSceneLoad }
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class RuntimeInitializeOnLoadMethodAttribute : Attribute { public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t) { } }
}

namespace UnityEngine.Events
{
    public sealed class UnityEvent { public void AddListener(Action a) { } public void RemoveAllListeners() { } }
}

namespace UnityEngine.EventSystems
{
    public class UIBehaviour : MonoBehaviour { }
    public sealed class EventSystem : UIBehaviour { }
    public sealed class StandaloneInputModule : UIBehaviour { }
    public class BaseEventData { }
    public sealed class PointerEventData : BaseEventData { public Vector2 position; public int pointerId; }
    public interface IPointerClickHandler { void OnPointerClick(PointerEventData e); }
    public interface IBeginDragHandler { void OnBeginDrag(PointerEventData e); }
    public interface IDragHandler { void OnDrag(PointerEventData e); }
    public interface IEndDragHandler { void OnEndDrag(PointerEventData e); }
}

namespace UnityEngine.UI
{
    using UnityEngine.EventSystems;
    public abstract class Graphic : UIBehaviour { public Color color; public bool raycastTarget; }
    public abstract class MaskableGraphic : Graphic { }
    public sealed class Image : MaskableGraphic
    {
        public enum Type { Simple, Sliced, Tiled, Filled }
        public Sprite sprite; public Type type; public float pixelsPerUnitMultiplier; public bool preserveAspect;
    }
    public sealed class Text : MaskableGraphic
    {
        public string text; public Font font; public int fontSize; public TextAnchor alignment; public FontStyle fontStyle;
        public HorizontalWrapMode horizontalOverflow; public VerticalWrapMode verticalOverflow;
    }
    public class Selectable : UIBehaviour { public bool interactable; public Graphic targetGraphic; }
    public sealed class Button : Selectable { public UnityEngine.Events.UnityEvent onClick = new UnityEngine.Events.UnityEvent(); }
    public sealed class CanvasScaler : UIBehaviour
    {
        public enum ScaleMode { ConstantPixelSize, ScaleWithScreenSize, ConstantPhysicalSize }
        public ScaleMode uiScaleMode; public Vector2 referenceResolution; public float matchWidthOrHeight;
    }
    public sealed class GraphicRaycaster : UIBehaviour { }
    public abstract class LayoutGroup : UIBehaviour { public RectOffset padding; public TextAnchor childAlignment; }
    public abstract class HorizontalOrVerticalLayoutGroup : LayoutGroup
    {
        public float spacing; public bool childControlWidth, childControlHeight, childForceExpandWidth, childForceExpandHeight, reverseArrangement;
    }
    public sealed class VerticalLayoutGroup : HorizontalOrVerticalLayoutGroup { }
    public sealed class HorizontalLayoutGroup : HorizontalOrVerticalLayoutGroup { }
    public sealed class LayoutElement : UIBehaviour { public float preferredWidth, preferredHeight, flexibleWidth, flexibleHeight; }
    public sealed class ContentSizeFitter : UIBehaviour
    {
        public enum FitMode { Unconstrained, MinSize, PreferredSize }
        public FitMode verticalFit, horizontalFit;
    }
    public sealed class RectMask2D : UIBehaviour { }
    public sealed class ScrollRect : UIBehaviour
    {
        public enum MovementType { Unrestricted, Elastic, Clamped }
        public bool horizontal, vertical; public MovementType movementType; public RectTransform content, viewport;
    }
    public abstract class BaseMeshEffect : UIBehaviour { }
    public class Shadow : BaseMeshEffect { public Color effectColor; public Vector2 effectDistance; }
    public sealed class Outline : Shadow { }
}
