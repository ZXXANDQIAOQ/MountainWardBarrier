// ============================================================================
//  Unity API 桩（UnityStubs）
// ============================================================================
//
//  这是给"没有安装 Unity 的机器"用的编译自检桩：
//  tools/UnityCheck 把 Assets/Scripts/Core、Assets/Scripts/Unity、Assets/Editor
//  连同这个文件一起编译，于是在没有 Unity 的环境里也能发现语法错误、拼错的成员名、
//  参数个数不对之类的问题。
//
//  它**不是** UnityEngine 的替代品：方法体全是空的，也不能运行。
//  只保证签名与 Unity 2022 LTS 一致。
//
//  维护约定：只补游戏代码真正用到的东西。新增 API 用法时，先在这里补签名，
//  再跑 tools/UnityCheck 验证 —— 这也是它能持续生效的原因。
// ============================================================================

using System;
using System.Collections.Generic;

namespace UnityEngine
{
    // ---------------------------------------------------------------- 基础数学类型

    public struct Vector2
    {
        public float x;
        public float y;

        public Vector2(float x, float y) { this.x = x; this.y = y; }

        public static Vector2 zero { get { return new Vector2(0f, 0f); } }
        public static Vector2 one { get { return new Vector2(1f, 1f); } }
        public static Vector2 up { get { return new Vector2(0f, 1f); } }
        public static Vector2 right { get { return new Vector2(1f, 0f); } }

        public float magnitude { get { return (float)Math.Sqrt(x * x + y * y); } }
        public float sqrMagnitude { get { return x * x + y * y; } }
        public Vector2 normalized { get { return this; } }

        public void Set(float nx, float ny) { x = nx; y = ny; }

        public static float Distance(Vector2 a, Vector2 b)
        {
            float dx = a.x - b.x;
            float dy = a.y - b.y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        public static Vector2 Lerp(Vector2 a, Vector2 b, float t)
        {
            t = t < 0f ? 0f : (t > 1f ? 1f : t);
            return new Vector2(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t);
        }

        public static Vector2 operator +(Vector2 a, Vector2 b) { return new Vector2(a.x + b.x, a.y + b.y); }
        public static Vector2 operator -(Vector2 a, Vector2 b) { return new Vector2(a.x - b.x, a.y - b.y); }
        public static Vector2 operator -(Vector2 a) { return new Vector2(-a.x, -a.y); }
        public static Vector2 operator *(Vector2 a, float s) { return new Vector2(a.x * s, a.y * s); }
        public static Vector2 operator *(float s, Vector2 a) { return new Vector2(a.x * s, a.y * s); }
        public static Vector2 operator /(Vector2 a, float s) { return new Vector2(a.x / s, a.y / s); }
        public static bool operator ==(Vector2 a, Vector2 b) { return a.x == b.x && a.y == b.y; }
        public static bool operator !=(Vector2 a, Vector2 b) { return !(a == b); }
        public override bool Equals(object o) { return o is Vector2 && this == (Vector2)o; }
        public override int GetHashCode() { return x.GetHashCode() ^ (y.GetHashCode() << 2); }
        public override string ToString() { return "(" + x + ", " + y + ")"; }

        public static implicit operator Vector2(Vector3 v) { return new Vector2(v.x, v.y); }
    }

    public struct Vector3
    {
        public float x;
        public float y;
        public float z;

        public Vector3(float x, float y) { this.x = x; this.y = y; this.z = 0f; }
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }

        public static Vector3 zero { get { return new Vector3(0f, 0f, 0f); } }
        public static Vector3 one { get { return new Vector3(1f, 1f, 1f); } }

        public static Vector3 operator +(Vector3 a, Vector3 b) { return new Vector3(a.x + b.x, a.y + b.y, a.z + b.z); }
        public static Vector3 operator -(Vector3 a, Vector3 b) { return new Vector3(a.x - b.x, a.y - b.y, a.z - b.z); }
        public static Vector3 operator *(Vector3 a, float s) { return new Vector3(a.x * s, a.y * s, a.z * s); }
        public static bool operator ==(Vector3 a, Vector3 b) { return a.x == b.x && a.y == b.y && a.z == b.z; }
        public static bool operator !=(Vector3 a, Vector3 b) { return !(a == b); }
        public override bool Equals(object o) { return o is Vector3 && this == (Vector3)o; }
        public override int GetHashCode() { return x.GetHashCode() ^ (y.GetHashCode() << 2) ^ (z.GetHashCode() >> 2); }
        public override string ToString() { return "(" + x + ", " + y + ", " + z + ")"; }

        public static implicit operator Vector3(Vector2 v) { return new Vector3(v.x, v.y, 0f); }
    }

    public struct Vector4
    {
        public float x, y, z, w;
        public Vector4(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
    }

    public struct Quaternion
    {
        public float x, y, z, w;
        public static Quaternion identity { get { return new Quaternion(); } }
        public static Quaternion Euler(float x, float y, float z) { return new Quaternion(); }
        public static Quaternion Euler(Vector3 euler) { return new Quaternion(); }
        public static Quaternion AngleAxis(float angle, Vector3 axis) { return new Quaternion(); }
    }

    public struct Color
    {
        public float r, g, b, a;

        public Color(float r, float g, float b) { this.r = r; this.g = g; this.b = b; this.a = 1f; }
        public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; }

        public static Color white { get { return new Color(1f, 1f, 1f, 1f); } }
        public static Color black { get { return new Color(0f, 0f, 0f, 1f); } }
        public static Color clear { get { return new Color(0f, 0f, 0f, 0f); } }
        public static Color red { get { return new Color(1f, 0f, 0f, 1f); } }
        public static Color green { get { return new Color(0f, 1f, 0f, 1f); } }
        public static Color blue { get { return new Color(0f, 0f, 1f, 1f); } }
        public static Color yellow { get { return new Color(1f, 0.92f, 0.016f, 1f); } }
        public static Color gray { get { return new Color(0.5f, 0.5f, 0.5f, 1f); } }
        public static Color magenta { get { return new Color(1f, 0f, 1f, 1f); } }
        public static Color cyan { get { return new Color(0f, 1f, 1f, 1f); } }

        public static Color Lerp(Color a, Color b, float t)
        {
            t = t < 0f ? 0f : (t > 1f ? 1f : t);
            return new Color(a.r + (b.r - a.r) * t, a.g + (b.g - a.g) * t,
                a.b + (b.b - a.b) * t, a.a + (b.a - a.a) * t);
        }

        public static Color operator *(Color c, float s) { return new Color(c.r * s, c.g * s, c.b * s, c.a * s); }
        public static Color operator *(Color a, Color b) { return new Color(a.r * b.r, a.g * b.g, a.b * b.b, a.a * b.a); }
        public static Color operator +(Color a, Color b) { return new Color(a.r + b.r, a.g + b.g, a.b + b.b, a.a + b.a); }
        public static Color operator -(Color a, Color b) { return new Color(a.r - b.r, a.g - b.g, a.b - b.b, a.a - b.a); }

        public override string ToString() { return "RGBA(" + r + ", " + g + ", " + b + ", " + a + ")"; }

        public static implicit operator Color(Color32 c32)
        {
            return new Color(c32.r / 255f, c32.g / 255f, c32.b / 255f, c32.a / 255f);
        }
    }

    public struct Color32
    {
        public byte r, g, b, a;
        public Color32(byte r, byte g, byte b, byte a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static implicit operator Color32(Color c)
        {
            return new Color32((byte)(c.r * 255f), (byte)(c.g * 255f), (byte)(c.b * 255f), (byte)(c.a * 255f));
        }
    }

    public struct Rect
    {
        public float x, y, width, height;
        public Rect(float x, float y, float width, float height)
        {
            this.x = x; this.y = y; this.width = width; this.height = height;
        }
        public float xMin { get { return x; } }
        public float yMin { get { return y; } }
        public float xMax { get { return x + width; } }
        public float yMax { get { return y + height; } }
        public Vector2 position { get { return new Vector2(x, y); } }
        public Vector2 size { get { return new Vector2(width, height); } }
        public Vector2 center { get { return new Vector2(x + width * 0.5f, y + height * 0.5f); } }
    }

    public class RectOffset
    {
        public int left, right, top, bottom;
        public RectOffset() { }
        public RectOffset(int left, int right, int top, int bottom)
        {
            this.left = left; this.right = right; this.top = top; this.bottom = bottom;
        }
        public int horizontal { get { return left + right; } }
        public int vertical { get { return top + bottom; } }
    }

    public static class Mathf
    {
        public const float PI = 3.14159274f;
        public const float Infinity = float.PositiveInfinity;
        public const float Epsilon = 1.401298E-45f;
        public const float Deg2Rad = 0.0174532924f;
        public const float Rad2Deg = 57.29578f;

        public static float Abs(float v) { return Math.Abs(v); }
        public static int Abs(int v) { return Math.Abs(v); }
        public static float Max(float a, float b) { return Math.Max(a, b); }
        public static float Max(float a, float b, float c) { return Math.Max(Math.Max(a, b), c); }
        public static float Max(params float[] values)
        {
            if (values == null || values.Length == 0) { return 0f; }
            float m = values[0];
            for (int i = 1; i < values.Length; i++) { if (values[i] > m) { m = values[i]; } }
            return m;
        }
        public static int Max(int a, int b) { return Math.Max(a, b); }
        public static int Max(int a, int b, int c) { return Math.Max(Math.Max(a, b), c); }
        public static int Max(params int[] values)
        {
            if (values == null || values.Length == 0) { return 0; }
            int m = values[0];
            for (int i = 1; i < values.Length; i++) { if (values[i] > m) { m = values[i]; } }
            return m;
        }
        public static float Min(float a, float b) { return Math.Min(a, b); }
        public static float Min(float a, float b, float c) { return Math.Min(Math.Min(a, b), c); }
        public static float Min(params float[] values)
        {
            if (values == null || values.Length == 0) { return 0f; }
            float m = values[0];
            for (int i = 1; i < values.Length; i++) { if (values[i] < m) { m = values[i]; } }
            return m;
        }
        public static int Min(int a, int b) { return Math.Min(a, b); }
        public static int Min(int a, int b, int c) { return Math.Min(Math.Min(a, b), c); }
        public static float Sqrt(float v) { return (float)Math.Sqrt(v); }
        public static float Pow(float a, float b) { return (float)Math.Pow(a, b); }
        public static float Sin(float v) { return (float)Math.Sin(v); }
        public static float Cos(float v) { return (float)Math.Cos(v); }
        public static float Tan(float v) { return (float)Math.Tan(v); }
        public static float Atan2(float y, float x) { return (float)Math.Atan2(y, x); }
        public static float Log(float v) { return (float)Math.Log(v); }
        public static float Log(float v, float b) { return (float)Math.Log(v, b); }
        public static float Log10(float v) { return (float)Math.Log10(v); }
        public static float Exp(float v) { return (float)Math.Exp(v); }
        public static float Sign(float v) { return v < 0f ? -1f : 1f; }

        /// <summary>把 t 折返到 [0, length) 区间内，来回往复。</summary>
        public static float PingPong(float t, float length)
        {
            if (length <= 0f) { return 0f; }
            t = Repeat(t, length * 2f);
            return length - Math.Abs(t - length);
        }

        public static float DeltaAngle(float current, float target) { return target - current; }
        public static float InverseLerp(float a, float b, float value)
        {
            if (a == b) { return 0f; }
            return Clamp01((value - a) / (b - a));
        }

        public static float Clamp(float v, float min, float max)
        {
            return v < min ? min : (v > max ? max : v);
        }

        public static int Clamp(int v, int min, int max)
        {
            return v < min ? min : (v > max ? max : v);
        }

        public static float Clamp01(float v) { return Clamp(v, 0f, 1f); }

        public static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * Clamp01(t);
        }

        public static float LerpUnclamped(float a, float b, float t) { return a + (b - a) * t; }

        public static float MoveTowards(float current, float target, float maxDelta)
        {
            if (Math.Abs(target - current) <= maxDelta)
            {
                return target;
            }
            return current + Sign(target - current) * maxDelta;
        }

        public static float SmoothStep(float from, float to, float t)
        {
            t = Clamp01(t);
            t = -2f * t * t * t + 3f * t * t;
            return to * t + from * (1f - t);
        }

        public static float Repeat(float t, float length) { return t - (float)Math.Floor(t / length) * length; }

        public static int FloorToInt(float v) { return (int)Math.Floor(v); }
        public static int CeilToInt(float v) { return (int)Math.Ceiling(v); }
        public static int RoundToInt(float v) { return (int)Math.Round(v, MidpointRounding.AwayFromZero); }
        public static float Floor(float v) { return (float)Math.Floor(v); }
        public static float Ceil(float v) { return (float)Math.Ceiling(v); }
        public static float Round(float v) { return (float)Math.Round(v, MidpointRounding.AwayFromZero); }
        public static bool Approximately(float a, float b) { return Math.Abs(a - b) < 1e-6f; }
    }

    // ---------------------------------------------------------------- 对象体系

    public class Object
    {
        public string name { get; set; }
        public HideFlags hideFlags { get; set; }

        public int GetInstanceID() { return 0; }
        public override string ToString() { return name; }

        public static void Destroy(Object obj) { }
        public static void Destroy(Object obj, float t) { }
        public static void DestroyImmediate(Object obj) { }
        public static void DontDestroyOnLoad(Object obj) { }
        public static T FindObjectOfType<T>() where T : Object { return null; }
        public static T[] FindObjectsOfType<T>() where T : Object { return new T[0]; }
        public static Object Instantiate(Object original) { return null; }

        public static bool operator ==(Object a, Object b) { return ReferenceEquals(a, b); }
        public static bool operator !=(Object a, Object b) { return !ReferenceEquals(a, b); }
        public static implicit operator bool(Object o) { return !ReferenceEquals(o, null); }

        public override bool Equals(object other) { return ReferenceEquals(this, other); }
        public override int GetHashCode() { return base.GetHashCode(); }
    }

    public enum HideFlags
    {
        None = 0, HideInHierarchy = 1, HideInInspector = 2, DontSaveInEditor = 4,
        NotEditable = 8, DontSaveInBuild = 16, DontUnloadUnusedAsset = 32, DontSave = 52, HideAndDontSave = 61
    }

    public enum Space { World = 0, Self = 1 }

    public class Component : Object
    {
        public GameObject gameObject { get { return null; } }
        public Transform transform { get { return null; } }

        public T GetComponent<T>() { return default(T); }
        public Component GetComponent(Type type) { return null; }
        public T GetComponentInChildren<T>() { return default(T); }
        public T GetComponentInParent<T>() { return default(T); }
        public T[] GetComponentsInChildren<T>() { return new T[0]; }
        public bool CompareTag(string tag) { return false; }
        public string tag { get; set; }
    }

    public class Behaviour : Component
    {
        public bool enabled { get; set; }
        public bool isActiveAndEnabled { get { return true; } }
    }

    public class MonoBehaviour : Behaviour
    {
        public Coroutine StartCoroutine(System.Collections.IEnumerator routine) { return null; }
        public void StopCoroutine(Coroutine routine) { }
        public void StopAllCoroutines() { }
        public void Invoke(string methodName, float time) { }
        public void CancelInvoke() { }
        public static void print(object message) { }
    }

    public class Coroutine { }
    public class YieldInstruction { }
    public class WaitForSeconds : YieldInstruction { public WaitForSeconds(float seconds) { } }
    public class WaitForEndOfFrame : YieldInstruction { }
    public class WaitForSecondsRealtime : CustomYieldInstruction
    {
        public WaitForSecondsRealtime(float seconds) { }
        public override bool keepWaiting { get { return false; } }
    }
    public abstract class CustomYieldInstruction : YieldInstruction
    {
        public abstract bool keepWaiting { get; }
    }

    public class ScriptableObject : Object { }

    public class Transform : Component, IEnumerable<Transform>
    {
        public Transform parent { get; set; }
        public Transform root { get { return null; } }
        public int childCount { get { return 0; } }
        public Vector3 position { get; set; }
        public Vector3 localPosition { get; set; }
        public Vector3 localScale { get; set; }
        public Quaternion rotation { get; set; }
        public Quaternion localRotation { get; set; }
        public Vector3 eulerAngles { get; set; }
        public Vector3 localEulerAngles { get; set; }

        public void SetParent(Transform p) { }
        public void SetParent(Transform p, bool worldPositionStays) { }
        public Transform GetChild(int index) { return null; }
        public Transform Find(string n) { return null; }
        public void SetAsFirstSibling() { }
        public void SetAsLastSibling() { }
        public void SetSiblingIndex(int index) { }
        public int GetSiblingIndex() { return 0; }
        public void DetachChildren() { }
        public IEnumerator<Transform> GetEnumerator() { yield break; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { yield break; }
    }

    public class RectTransform : Transform
    {
        public Vector2 anchorMin { get; set; }
        public Vector2 anchorMax { get; set; }
        public Vector2 pivot { get; set; }
        public Vector2 offsetMin { get; set; }
        public Vector2 offsetMax { get; set; }
        public Vector2 anchoredPosition { get; set; }
        public Vector3 anchoredPosition3D { get; set; }
        public Vector2 sizeDelta { get; set; }
        public Rect rect { get { return new Rect(0f, 0f, 0f, 0f); } }
        public void SetInsetAndSizeFromParentEdge(Edge edge, float inset, float size) { }
        public void GetWorldCorners(Vector3[] fourCornersArray) { }
        public enum Edge { Left, Right, Top, Bottom }
    }

    public enum PrimitiveType { Sphere, Capsule, Cylinder, Cube, Plane, Quad }

    public sealed class GameObject : Object
    {
        public GameObject() { }
        public GameObject(string n) { name = n; }
        public GameObject(string n, params Type[] components) { name = n; }

        public Transform transform { get { return null; } }
        public bool activeSelf { get { return true; } }
        public bool activeInHierarchy { get { return true; } }
        public int layer { get; set; }
        public string tag { get; set; }
        public SceneManagement.Scene scene { get { return default(SceneManagement.Scene); } }

        public void SetActive(bool value) { }
        public T AddComponent<T>() where T : Component { return null; }
        public Component AddComponent(Type componentType) { return null; }
        public T GetComponent<T>() { return default(T); }
        public Component GetComponent(Type type) { return null; }
        public T GetComponentInChildren<T>() { return default(T); }
        public T[] GetComponentsInChildren<T>() { return new T[0]; }
        public bool CompareTag(string t) { return false; }
        public static GameObject Find(string n) { return null; }
        public static GameObject CreatePrimitive(PrimitiveType type) { return null; }
    }

    // ---------------------------------------------------------------- 资源

    public class Texture : Object
    {
        public int width { get; set; }
        public int height { get; set; }
        public FilterMode filterMode { get; set; }
        public TextureWrapMode wrapMode { get; set; }
    }

    public class Texture2D : Texture
    {
        public Texture2D(int width, int height) { this.width = width; this.height = height; }
        public Texture2D(int width, int height, TextureFormat format, bool mipChain)
        {
            this.width = width; this.height = height;
        }
        public TextureFormat format { get; set; }
        public void SetPixels(Color[] colors) { }
        public void SetPixels32(Color32[] colors) { }
        public void SetPixel(int x, int y, Color color) { }
        public Color GetPixel(int x, int y) { return Color.clear; }
        public void Apply() { }
        public void Apply(bool updateMipmaps) { }
    }

    public enum TextureFormat
    {
        Alpha8 = 1, ARGB4444 = 2, RGB24 = 3, RGBA32 = 4, ARGB32 = 5,
        RGB565 = 7, R16 = 9, DXT1 = 10, DXT5 = 12, RGBA4444 = 13,
        BGRA32 = 14, RHalf = 15, RGHalf = 16, RGBAHalf = 17,
        RFloat = 18, RGFloat = 19, RGBAFloat = 20
    }

    public enum FilterMode { Point = 0, Bilinear = 1, Trilinear = 2 }
    public enum TextureWrapMode { Repeat = 0, Clamp = 1, Mirror = 2, MirrorOnce = 3 }
    public enum SpriteMeshType { FullRect = 0, Tight = 1 }

    public class Sprite : Object
    {
        public Vector4 border { get; set; }
        public Rect rect { get { return new Rect(0f, 0f, 0f, 0f); } }
        public Vector2 pivot { get { return new Vector2(0.5f, 0.5f); } }
        public Texture2D texture { get { return null; } }
        public float pixelsPerUnit { get { return 100f; } }
        public Rect textureRect { get { return new Rect(0f, 0f, 0f, 0f); } }

        public static Sprite Create(Texture2D texture, Rect rect, Vector2 pivot) { return null; }
        public static Sprite Create(Texture2D texture, Rect rect, Vector2 pivot, float pixelsPerUnit) { return null; }
        public static Sprite Create(Texture2D texture, Rect rect, Vector2 pivot, float pixelsPerUnit,
            uint extrude, SpriteMeshType meshType) { return null; }
        public static Sprite Create(Texture2D texture, Rect rect, Vector2 pivot, float pixelsPerUnit,
            uint extrude, SpriteMeshType meshType, Vector4 border) { return null; }
    }

    public class TextAsset : Object
    {
        public string text { get { return ""; } }
        public byte[] bytes { get { return new byte[0]; } }
    }

    public class Font : Object
    {
        public int fontSize { get { return 0; } }
        public string[] fontNames { get { return new string[0]; } }
        public bool dynamic { get { return true; } }

        public static Font CreateDynamicFontFromOSFont(string fontname, int size) { return null; }
        public static Font CreateDynamicFontFromOSFont(string[] fontnames, int size) { return null; }
        public static string[] GetOSInstalledFontNames() { return new string[0]; }
        public void RequestCharactersInTexture(string characters) { }
        public void RequestCharactersInTexture(string characters, int size) { }
        public void RequestCharactersInTexture(string characters, int size, FontStyle style) { }
        public bool HasCharacter(char c) { return false; }
    }

    public enum FontStyle { Normal = 0, Bold = 1, Italic = 2, BoldAndItalic = 3 }

    public enum TextAnchor
    {
        UpperLeft = 0, UpperCenter = 1, UpperRight = 2,
        MiddleLeft = 3, MiddleCenter = 4, MiddleRight = 5,
        LowerLeft = 6, LowerCenter = 7, LowerRight = 8
    }

    public enum HorizontalWrapMode { Wrap = 0, Overflow = 1 }
    public enum VerticalWrapMode { Truncate = 0, Overflow = 1 }

    public static class Resources
    {
        public static T Load<T>(string path) where T : Object { return null; }
        public static Object Load(string path) { return null; }
        public static Object Load(string path, Type systemTypeInstance) { return null; }
        public static T[] LoadAll<T>(string path) where T : Object { return new T[0]; }
        public static T GetBuiltinResource<T>(string path) where T : Object { return null; }
        public static void UnloadUnusedAssets() { }
    }

    // ---------------------------------------------------------------- 音频

    public class AudioClip : Object
    {
        public float length { get { return 0f; } }
        public int samples { get { return 0; } }
        public int channels { get { return 0; } }
        public int frequency { get { return 0; } }

        public static AudioClip Create(string name, int lengthSamples, int channels, int frequency,
            bool stream) { return null; }
        public bool SetData(float[] data, int offsetSamples) { return true; }
        public bool GetData(float[] data, int offsetSamples) { return true; }
    }

    public class AudioSource : Behaviour
    {
        public AudioClip clip { get; set; }
        public float volume { get; set; }
        public float pitch { get; set; }
        public bool loop { get; set; }
        public bool mute { get; set; }
        public bool playOnAwake { get; set; }
        public float spatialBlend { get; set; }
        public float time { get; set; }
        public int timeSamples { get; set; }
        public bool isPlaying { get { return false; } }

        public void Play() { }
        public void Play(ulong delay) { }
        public void Stop() { }
        public void Pause() { }
        public void UnPause() { }
        public void PlayOneShot(AudioClip clip) { }
        public void PlayOneShot(AudioClip clip, float volumeScale) { }
    }

    public class AudioListener : Behaviour { }
    public class AudioSourceSettings { }
    public enum AudioClipLoadType { DecompressOnLoad = 0, CompressedInMemory = 1, Streaming = 2 }
    public enum AudioCompressionFormat
    {
        PCM = 0, Vorbis = 1, MP3 = 2, AAC = 4, HEVAG = 8, XMA = 16, AT9 = 32, ADPCM = 256
    }

    // ---------------------------------------------------------------- 相机与屏幕

    public class Camera : Behaviour
    {
        public static Camera main { get { return null; } }
        public static Camera current { get { return null; } }
        public bool orthographic { get; set; }
        public float orthographicSize { get; set; }
        public float fieldOfView { get; set; }
        public float nearClipPlane { get; set; }
        public float farClipPlane { get; set; }
        public CameraClearFlags clearFlags { get; set; }
        public Color backgroundColor { get; set; }
        public int cullingMask { get; set; }
        public int depth { get; set; }
        public Rect rect { get; set; }

        public Vector3 WorldToScreenPoint(Vector3 position) { return Vector3.zero; }
        public Vector3 ScreenToWorldPoint(Vector3 position) { return Vector3.zero; }
    }

    public enum CameraClearFlags
    {
        Skybox = 1, Color = 2, SolidColor = 2, Depth = 3, Nothing = 4
    }

    public static class Screen
    {
        public static int width { get { return 1080; } }
        public static int height { get { return 1920; } }
        public static float dpi { get { return 400f; } }
        public static int sleepTimeout { get; set; }
        public static bool fullScreen { get; set; }
        public static void SetResolution(int width, int height, bool fullscreen) { }
    }

    public static class SleepTimeout
    {
        public const int NeverSleep = -1;
        public const int SystemSetting = -2;
    }

    public static class Application
    {
        public static int targetFrameRate { get; set; }
        public static string persistentDataPath { get { return ""; } }
        public static string dataPath { get { return ""; } }
        public static string streamingAssetsPath { get { return ""; } }
        public static string temporaryCachePath { get { return ""; } }
        public static string version { get { return "1.0.0"; } }
        public static string productName { get { return ""; } }
        public static string companyName { get { return ""; } }
        public static bool isPlaying { get { return true; } }
        public static bool isMobilePlatform { get { return true; } }
        public static RuntimePlatform platform { get { return RuntimePlatform.Android; } }
        public static void Quit() { }
    }

    public enum RuntimePlatform
    {
        OSXEditor = 0, OSXPlayer = 1, WindowsPlayer = 2, WindowsEditor = 7,
        IPhonePlayer = 8, Android = 11, LinuxPlayer = 13, LinuxEditor = 16, WebGLPlayer = 17
    }

    public static class QualitySettings
    {
        public static int vSyncCount { get; set; }
        public static int antiAliasing { get; set; }
        public static int GetQualityLevel() { return 0; }
        public static void SetQualityLevel(int index) { }
    }

    public static class PlayerPrefs
    {
        public static string GetString(string key, string defaultValue) { return defaultValue; }
        public static void SetString(string key, string value) { }
        public static int GetInt(string key, int defaultValue) { return defaultValue; }
        public static void SetInt(string key, int value) { }
        public static float GetFloat(string key, float defaultValue) { return defaultValue; }
        public static void SetFloat(string key, float value) { }
        public static bool HasKey(string key) { return false; }
        public static void DeleteKey(string key) { }
        public static void DeleteAll() { }
        public static void Save() { }
    }

    // ---------------------------------------------------------------- 时间与输入

    public static class Time
    {
        public static float deltaTime { get { return 0.016f; } }
        public static float unscaledDeltaTime { get { return 0.016f; } }
        public static float time { get { return 0f; } }
        public static float unscaledTime { get { return 0f; } }
        public static float fixedDeltaTime { get { return 0.02f; } }
        public static float timeScale { get; set; }
        public static int frameCount { get { return 0; } }
        public static float realtimeSinceStartup { get { return 0f; } }
    }

    public static class Input
    {
        public static bool GetKeyDown(KeyCode key) { return false; }
        public static bool GetKeyUp(KeyCode key) { return false; }
        public static bool GetKey(KeyCode key) { return false; }
        public static bool GetMouseButtonDown(int button) { return false; }
        public static bool GetMouseButtonUp(int button) { return false; }
        public static bool GetMouseButton(int button) { return false; }
        public static Vector3 mousePosition { get { return Vector3.zero; } }
        public static int touchCount { get { return 0; } }
        public static Touch GetTouch(int index) { return default(Touch); }
        public static bool anyKeyDown { get { return false; } }
    }

    public struct Touch
    {
        public int fingerId { get { return 0; } }
        public Vector2 position { get { return Vector2.zero; } }
        public Vector2 deltaPosition { get { return Vector2.zero; } }
        public TouchPhase phase { get { return TouchPhase.Ended; } }
    }

    public enum TouchPhase { Began = 0, Moved = 1, Stationary = 2, Ended = 3, Canceled = 4 }

    public enum KeyCode
    {
        None = 0, Backspace = 8, Tab = 9, Return = 13, Escape = 27, Space = 32,
        Delete = 127,
        UpArrow = 273, DownArrow = 274, RightArrow = 275, LeftArrow = 276,
        A = 97, B = 98, C = 99, D = 100, E = 101, F = 102, G = 103, H = 104, I = 105,
        J = 106, K = 107, L = 108, M = 109, N = 110, O = 111, P = 112, Q = 113, R = 114,
        S = 115, T = 116, U = 117, V = 118, W = 119, X = 120, Y = 121, Z = 122,
        Alpha0 = 48, Alpha1 = 49, Alpha2 = 50, Alpha3 = 51, Alpha4 = 52,
        Alpha5 = 53, Alpha6 = 54, Alpha7 = 55, Alpha8 = 56, Alpha9 = 57,
        F1 = 282, F2 = 283, F3 = 284, F4 = 285, F5 = 286, F6 = 287,
        LeftShift = 304, RightShift = 303, LeftControl = 306, RightControl = 305,
        LeftAlt = 308, RightAlt = 307,
        Mouse0 = 323, Mouse1 = 324, Mouse2 = 325
    }

    public static class Random
    {
        public static float value { get { return 0.5f; } }
        public static int Range(int minInclusive, int maxExclusive) { return minInclusive; }
        public static float Range(float minInclusive, float maxInclusive) { return minInclusive; }
        public static void InitState(int seed) { }
        public static Vector2 insideUnitCircle { get { return Vector2.zero; } }
    }

    public static class Debug
    {
        public static void Log(object message) { }
        public static void Log(object message, Object context) { }
        public static void LogWarning(object message) { }
        public static void LogWarning(object message, Object context) { }
        public static void LogError(object message) { }
        public static void LogError(object message, Object context) { }
        public static void LogException(Exception exception) { }
        public static void Assert(bool condition) { }
        public static void DrawLine(Vector3 start, Vector3 end, Color color) { }
    }

    // ---------------------------------------------------------------- 特性

    [AttributeUsage(AttributeTargets.Field)]
    public class SerializeField : Attribute { }

    [AttributeUsage(AttributeTargets.Field)]
    public class HeaderAttribute : PropertyAttribute
    {
        public HeaderAttribute(string header) { }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class RangeAttribute : PropertyAttribute
    {
        public RangeAttribute(float min, float max) { }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class TooltipAttribute : PropertyAttribute
    {
        public TooltipAttribute(string tooltip) { }
    }

    public abstract class PropertyAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Class)]
    public class RequireComponent : Attribute
    {
        public RequireComponent(Type requiredComponent) { }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class DisallowMultipleComponent : Attribute { }

    [AttributeUsage(AttributeTargets.Class)]
    public class ExecuteInEditMode : Attribute { }

    [AttributeUsage(AttributeTargets.Class)]
    public class AddComponentMenu : Attribute
    {
        public AddComponentMenu(string menuName) { }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class RuntimeInitializeOnLoadMethodAttribute : Attribute
    {
        public RuntimeInitializeOnLoadMethodAttribute() { }
        public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType loadType) { }
    }

    public enum RuntimeInitializeLoadType
    {
        AfterSceneLoad = 0, BeforeSceneLoad = 1, AfterAssembliesLoaded = 2,
        BeforeSplashScreen = 3, SubsystemRegistration = 4
    }

    public static class RectTransformUtility
    {
        public static bool ScreenPointToLocalPointInRectangle(RectTransform rect, Vector2 screenPoint,
            Camera cam, out Vector2 localPoint)
        {
            localPoint = Vector2.zero;
            return true;
        }

        public static bool ScreenPointToWorldPointInRectangle(RectTransform rect, Vector2 screenPoint,
            Camera cam, out Vector3 worldPoint)
        {
            worldPoint = Vector3.zero;
            return true;
        }

        public static bool RectangleContainsScreenPoint(RectTransform rect, Vector2 screenPoint) { return false; }
        public static bool RectangleContainsScreenPoint(RectTransform rect, Vector2 screenPoint, Camera cam) { return false; }
    }

    public enum ColorSpace { Uninitialized = -1, Gamma = 0, Linear = 1 }
}

namespace UnityEngine.Events
{
    public delegate void UnityAction();
    public delegate void UnityAction<T0>(T0 arg0);
    public delegate void UnityAction<T0, T1>(T0 arg0, T1 arg1);

    public class UnityEvent
    {
        public void AddListener(UnityAction call) { }
        public void RemoveListener(UnityAction call) { }
        public void RemoveAllListeners() { }
        public void Invoke() { }
    }

    public class UnityEvent<T0>
    {
        public void AddListener(UnityAction<T0> call) { }
        public void RemoveListener(UnityAction<T0> call) { }
        public void RemoveAllListeners() { }
        public void Invoke(T0 arg0) { }
    }

    public class UnityEvent<T0, T1>
    {
        public void AddListener(UnityAction<T0, T1> call) { }
        public void RemoveAllListeners() { }
        public void Invoke(T0 arg0, T1 arg1) { }
    }
}

namespace UnityEngine.SceneManagement
{
    public struct Scene
    {
        public string name { get { return ""; } }
        public string path { get { return ""; } }
        public int buildIndex { get { return 0; } }
        public bool isLoaded { get { return true; } }
        public bool IsValid() { return true; }
        public static Scene GetActiveScene() { return default(Scene); }
    }

    public static class SceneManager
    {
        public static Scene GetActiveScene() { return default(Scene); }
        public static void LoadScene(string sceneName) { }
        public static void LoadScene(int sceneBuildIndex) { }
    }
}

namespace UnityEngine.UI
{
    using UnityEngine.Events;
    using UnityEngine.EventSystems;

    public enum RenderMode { ScreenSpaceOverlay = 0, ScreenSpaceCamera = 1, WorldSpace = 2 }

    public class Canvas : Behaviour
    {
        public RenderMode renderMode { get; set; }
        public bool pixelPerfect { get; set; }
        public int sortingOrder { get; set; }
        public string sortingLayerName { get; set; }
        public Camera worldCamera { get; set; }
        public float planeDistance { get; set; }
        public bool overrideSorting { get; set; }
        public bool isRootCanvas { get { return true; } }
        public float scaleFactor { get { return 1f; } }
        public Canvas rootCanvas { get { return null; } }
    }

    public class CanvasRenderer : Component
    {
        public void SetAlpha(float alpha) { }
        public float GetAlpha() { return 1f; }
    }

    public abstract class Graphic : UIBehaviour
    {
        public Color color { get; set; }
        public bool raycastTarget { get; set; }
        public RectTransform rectTransform { get { return null; } }
        public Canvas canvas { get { return null; } }
        public CanvasRenderer canvasRenderer { get { return null; } }
        public Material material { get; set; }
        public virtual void SetAllDirty() { }
        public virtual void SetVerticesDirty() { }
        public virtual void SetLayoutDirty() { }
        protected virtual void OnPopulateMesh(VertexHelper vh) { }
    }

    public abstract class MaskableGraphic : Graphic
    {
        public bool maskable { get; set; }
    }

    public class VertexHelper
    {
        public void Clear() { }
        public int currentVertCount { get { return 0; } }
        public void AddVert(Vector3 position, Color32 color, Vector2 uv0) { }
        public void AddTriangle(int idx0, int idx1, int idx2) { }
        public void AddUIVertexQuad(UIVertex[] verts) { }
        public void FillMesh(Mesh mesh) { }
    }

    public struct UIVertex
    {
        public Vector3 position;
        public Color32 color;
        public Vector2 uv0;
    }

    public class Mesh : Object
    {
        public Vector3[] vertices { get; set; }
        public int[] triangles { get; set; }
        public Vector2[] uv { get; set; }
        public Color[] colors { get; set; }
        public void Clear() { }
    }

    public class Material : Object { }

    public class Image : MaskableGraphic
    {
        public enum Type { Simple = 0, Sliced = 1, Tiled = 2, Filled = 3 }
        public enum FillMethod { Horizontal = 0, Vertical = 1, Radial90 = 2, Radial180 = 3, Radial360 = 4 }
        public enum OriginHorizontal { Left = 0, Right = 1 }
        public enum OriginVertical { Bottom = 0, Top = 1 }

        public Sprite sprite { get; set; }
        public Sprite overrideSprite { get; set; }
        public Type type { get; set; }
        public bool preserveAspect { get; set; }
        public bool fillCenter { get; set; }
        public FillMethod fillMethod { get; set; }
        public float fillAmount { get; set; }
        public bool fillClockwise { get; set; }
        public int fillOrigin { get; set; }
        public float pixelsPerUnitMultiplier { get; set; }
        public float alphaHitTestMinimumThreshold { get; set; }
    }

    public class RawImage : MaskableGraphic
    {
        public Texture texture { get; set; }
        public Rect uvRect { get; set; }
    }

    public class Text : MaskableGraphic
    {
        public string text { get; set; }
        public Font font { get; set; }
        public int fontSize { get; set; }
        public FontStyle fontStyle { get; set; }
        public TextAnchor alignment { get; set; }
        public bool supportRichText { get; set; }
        public bool resizeTextForBestFit { get; set; }
        public int resizeTextMinSize { get; set; }
        public int resizeTextMaxSize { get; set; }
        public HorizontalWrapMode horizontalOverflow { get; set; }
        public VerticalWrapMode verticalOverflow { get; set; }
        public float lineSpacing { get; set; }
        public bool alignByGeometry { get; set; }
        public TextGenerator cachedTextGenerator { get { return null; } }
        public float preferredWidth { get { return 0f; } }
        public float preferredHeight { get { return 0f; } }
    }

    public class TextGenerator { }

    public struct ColorBlock
    {
        public Color normalColor;
        public Color highlightedColor;
        public Color pressedColor;
        public Color selectedColor;
        public Color disabledColor;
        public float colorMultiplier;
        public float fadeDuration;
    }

    public struct SpriteState
    {
        public Sprite highlightedSprite;
        public Sprite pressedSprite;
        public Sprite disabledSprite;
    }

    public struct Navigation
    {
        public enum Mode { None = 0, Horizontal = 1, Vertical = 2, Automatic = 3, Explicit = 4 }
        public Mode mode { get; set; }
    }

    public class Selectable : UIBehaviour
    {
        public bool interactable { get; set; }
        public Graphic targetGraphic { get; set; }
        public ColorBlock colors { get; set; }
        public SpriteState spriteState { get; set; }
        public Navigation navigation { get; set; }
    }

    public class Button : Selectable
    {
        public class ButtonClickedEvent : UnityEvent { }
        public ButtonClickedEvent onClick { get { return _onClick; } }
        private readonly ButtonClickedEvent _onClick = new ButtonClickedEvent();
        public void Select() { }
    }

    public class Slider : Selectable
    {
        public enum Direction { LeftToRight = 0, RightToLeft = 1, BottomToTop = 2, TopToBottom = 3 }
        public class SliderEvent : UnityEvent<float> { }

        public float value { get; set; }
        public float minValue { get; set; }
        public float maxValue { get; set; }
        public float normalizedValue { get; set; }
        public bool wholeNumbers { get; set; }
        public Direction direction { get; set; }
        public RectTransform fillRect { get; set; }
        public RectTransform handleRect { get; set; }
        public SliderEvent onValueChanged { get { return _onValueChanged; } }
        private readonly SliderEvent _onValueChanged = new SliderEvent();
        public void SetValueWithoutNotify(float input) { }
    }

    public class Toggle : Selectable
    {
        public class ToggleEvent : UnityEvent<bool> { }
        public bool isOn { get; set; }
        public Graphic graphic { get; set; }
        public ToggleEvent onValueChanged { get { return _onValueChanged; } }
        private readonly ToggleEvent _onValueChanged = new ToggleEvent();
    }

    public class Scrollbar : Selectable
    {
        public class ScrollEvent : UnityEvent<float> { }
        public float value { get; set; }
        public float size { get; set; }
        public float direction { get; set; }
        public ScrollEvent onValueChanged { get { return _onValueChanged; } }
        private readonly ScrollEvent _onValueChanged = new ScrollEvent();
    }

    public class ScrollRect : UIBehaviour
    {
        public enum MovementType { Unrestricted = 0, Elastic = 1, Clamped = 2 }
        public class ScrollRectEvent : UnityEvent<Vector2> { }

        public RectTransform content { get; set; }
        public RectTransform viewport { get; set; }
        public bool horizontal { get; set; }
        public bool vertical { get; set; }
        public MovementType movementType { get; set; }
        public float elasticity { get; set; }
        public bool inertia { get; set; }
        public float decelerationRate { get; set; }
        public float scrollSensitivity { get; set; }
        public Vector2 normalizedPosition { get; set; }
        public float horizontalNormalizedPosition { get; set; }
        public float verticalNormalizedPosition { get; set; }
        public Scrollbar horizontalScrollbar { get; set; }
        public Scrollbar verticalScrollbar { get; set; }
        public ScrollRectEvent onValueChanged { get { return _onValueChanged; } }
        private readonly ScrollRectEvent _onValueChanged = new ScrollRectEvent();
    }

    public class Mask : UIBehaviour
    {
        public bool showMaskGraphic { get; set; }
    }

    public class RectMask2D : UIBehaviour { }

    public class LayoutElement : UIBehaviour
    {
        public bool ignoreLayout { get; set; }
        public float minWidth { get; set; }
        public float minHeight { get; set; }
        public float preferredWidth { get; set; }
        public float preferredHeight { get; set; }
        public float flexibleWidth { get; set; }
        public float flexibleHeight { get; set; }
        public int layoutPriority { get; set; }
    }

    public class LayoutGroup : UnityEngine.EventSystems.UIBehaviour
    {
        public RectOffset padding { get; set; }
        public TextAnchor childAlignment { get; set; }
    }

    public class HorizontalOrVerticalLayoutGroup : LayoutGroup
    {
        public float spacing { get; set; }
        public bool childControlWidth { get; set; }
        public bool childControlHeight { get; set; }
        public bool childForceExpandWidth { get; set; }
        public bool childForceExpandHeight { get; set; }
        public bool childScaleWidth { get; set; }
        public bool childScaleHeight { get; set; }
        public bool reverseArrangement { get; set; }
    }

    public class HorizontalLayoutGroup : HorizontalOrVerticalLayoutGroup { }

    public class VerticalLayoutGroup : HorizontalOrVerticalLayoutGroup { }

    public class GridLayoutGroup : LayoutGroup
    {
        public Vector2 cellSize { get; set; }
        public Vector2 gridSpacing { get; set; }
    }

    public class ContentSizeFitter : UnityEngine.EventSystems.UIBehaviour
    {
        public enum FitMode { Unconstrained = 0, MinSize = 1, PreferredSize = 2 }
        public FitMode horizontalFit { get; set; }
        public FitMode verticalFit { get; set; }
    }

    public class AspectRatioFitter : UnityEngine.EventSystems.UIBehaviour
    {
        public enum AspectMode { None = 0, WidthControlsHeight = 1, HeightControlsWidth = 2, FitInParent = 3, EnvelopeParent = 4 }
        public AspectMode aspectMode { get; set; }
        public float aspectRatio { get; set; }
    }

    public class CanvasScaler : UnityEngine.EventSystems.UIBehaviour
    {
        public enum ScaleMode { ConstantPixelSize = 0, ScaleWithScreenSize = 1, ConstantPhysicalSize = 2 }
        public enum ScreenMatchMode { MatchWidthOrHeight = 0, Expand = 1, Shrink = 2 }

        public ScaleMode uiScaleMode { get; set; }
        public Vector2 referenceResolution { get; set; }
        public ScreenMatchMode screenMatchMode { get; set; }
        public float matchWidthOrHeight { get; set; }
        public float referencePixelsPerUnit { get; set; }
        public float scaleFactor { get; set; }
    }

    public class GraphicRaycaster : UnityEngine.EventSystems.UIBehaviour
    {
        public bool ignoreReversedGraphics { get; set; }
        public int blockingObjects { get; set; }
    }
}

namespace UnityEngine.EventSystems
{
    public abstract class UIBehaviour : MonoBehaviour { }

    public class BaseEventData
    {
        public GameObject selectedObject { get; set; }
        public bool used { get; set; }
        public void Use() { }
    }

    public class PointerEventData : BaseEventData
    {
        public enum InputButton { Left = 0, Right = 1, Middle = 2 }
        public Vector2 position { get; set; }
        public Vector2 delta { get; set; }
        public Vector2 pressPosition { get; set; }
        public Camera pressEventCamera { get; set; }
        public Camera enterEventCamera { get; set; }
        public InputButton button { get; set; }
        public int clickCount { get; set; }
        public GameObject pointerEnter { get; set; }
        public GameObject pointerPress { get; set; }
        public bool dragging { get; set; }
        public int pointerId { get; set; }
        public bool IsPointerMoving() { return false; }
        public bool IsScrolling() { return false; }
    }

    public class AxisEventData : BaseEventData
    {
        public Vector2 moveVector { get; set; }
    }

    public interface IEventSystemHandler { }
    public interface IPointerEnterHandler : IEventSystemHandler { void OnPointerEnter(PointerEventData eventData); }
    public interface IPointerExitHandler : IEventSystemHandler { void OnPointerExit(PointerEventData eventData); }
    public interface IPointerDownHandler : IEventSystemHandler { void OnPointerDown(PointerEventData eventData); }
    public interface IPointerUpHandler : IEventSystemHandler { void OnPointerUp(PointerEventData eventData); }
    public interface IPointerClickHandler : IEventSystemHandler { void OnPointerClick(PointerEventData eventData); }
    public interface IBeginDragHandler : IEventSystemHandler { void OnBeginDrag(PointerEventData eventData); }
    public interface IDragHandler : IEventSystemHandler { void OnDrag(PointerEventData eventData); }
    public interface IEndDragHandler : IEventSystemHandler { void OnEndDrag(PointerEventData eventData); }
    public interface IDropHandler : IEventSystemHandler { void OnDrop(PointerEventData eventData); }
    public interface IScrollHandler : IEventSystemHandler { void OnScroll(PointerEventData eventData); }
    public interface ISelectHandler : IEventSystemHandler { void OnSelect(BaseEventData eventData); }
    public interface IDeselectHandler : IEventSystemHandler { void OnDeselect(BaseEventData eventData); }
    public interface ISubmitHandler : IEventSystemHandler { void OnSubmit(BaseEventData eventData); }
    public interface ICancelHandler : IEventSystemHandler { void OnCancel(BaseEventData eventData); }
    public interface IMoveHandler : IEventSystemHandler { void OnMove(AxisEventData eventData); }

    public class EventSystem : UIBehaviour
    {
        public static EventSystem current { get { return null; } }

        public void SetSelectedGameObject(GameObject selected) { }
        public void SetSelectedGameObject(GameObject selected, BaseEventData pointer) { }
        public GameObject currentSelectedGameObject { get { return null; } }
        public bool IsPointerOverGameObject() { return false; }
        public bool IsPointerOverGameObject(int pointerId) { return false; }
        public void RaycastAll(PointerEventData eventData, System.Collections.Generic.List<RaycastResult> raycastResults) { }
    }

    public struct RaycastResult
    {
        public GameObject gameObject;
        public float distance;
        public int depth;
    }

    public class BaseInputModule : UIBehaviour
    {
        public virtual void Process() { }
    }

    public class PointerInputModule : BaseInputModule { }

    public class StandaloneInputModule : PointerInputModule
    {
        public string horizontalAxis { get; set; }
        public string verticalAxis { get; set; }
        public string submitButton { get; set; }
        public string cancelButton { get; set; }
        public bool forceModuleActive { get; set; }
    }
}
