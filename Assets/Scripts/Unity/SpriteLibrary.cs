using System.Collections.Generic;
using UnityEngine;

namespace MountainWardBarrier.Game
{
    /// <summary>
    /// 精灵图仓库。
    ///
    /// 三级兜底策略，保证「图挂了也照样能玩」：
    ///   1) Resources.Load&lt;Sprite&gt;              —— 正常路径
    ///   2) Resources.Load&lt;Texture2D&gt; → Sprite  —— 贴图导入设置被改成 Default 时用
    ///   3) 运行时画一个圆角方块                  —— 资源整个缺失时用
    /// </summary>
    public static class SpriteLibrary
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Sprite> Fallbacks = new Dictionary<string, Sprite>();

        private static readonly string[] Prefixes = new string[]
        {
            "Sprites/{0}",
            "Sprites/UI/{0}",
            "Sprites/Units/{0}",
            "Sprites/Terrain/{0}"
        };

        /// <summary>取精灵图。找不到时返回 null（不会抛异常）。</summary>
        public static Sprite Get(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }
            Sprite cached;
            if (Cache.TryGetValue(name, out cached))
            {
                return cached;
            }

            Sprite result = null;
            for (int i = 0; i < Prefixes.Length && result == null; i++)
            {
                string path = string.Format(Prefixes[i], name);
                result = Resources.Load<Sprite>(path);
                if (result == null)
                {
                    Texture2D tex = Resources.Load<Texture2D>(path);
                    if (tex != null)
                    {
                        result = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height),
                            new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                        result.name = name;
                    }
                }
            }

            if (result == null)
            {
                Debug.LogWarning("[SpriteLibrary] 找不到精灵图：" + name + "，将使用占位方块。");
            }
            Cache[name] = result;
            return result;
        }

        /// <summary>取精灵图，取不到就用指定颜色生成一个圆角方块顶上。</summary>
        public static Sprite GetOrFallback(string name, Color color)
        {
            Sprite s = Get(name);
            if (s != null)
            {
                return s;
            }
            string key = ColorKey(color);
            Sprite fb;
            if (Fallbacks.TryGetValue(key, out fb))
            {
                return fb;
            }
            fb = MakeRoundedSprite(color);
            Fallbacks[key] = fb;
            return fb;
        }

        private static string ColorKey(Color c)
        {
            return Mathf.RoundToInt(c.r * 255f) + "_" + Mathf.RoundToInt(c.g * 255f) + "_"
                + Mathf.RoundToInt(c.b * 255f) + "_" + Mathf.RoundToInt(c.a * 255f);
        }

        /// <summary>运行时生成一个 64x64 的圆角方块精灵，带描边。</summary>
        public static Sprite MakeRoundedSprite(Color color, int size = 64, int radius = 14)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            Color edge = new Color(Mathf.Clamp01(color.r * 0.35f), Mathf.Clamp01(color.g * 0.35f),
                Mathf.Clamp01(color.b * 0.35f), 1f);
            Color[] px = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(radius - x, x - (size - 1 - radius), 0f);
                    float dy = Mathf.Max(radius - y, y - (size - 1 - radius), 0f);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float inside = Mathf.Clamp01(radius - dist + 0.5f);
                    bool border = inside > 0f && (x < 3 || y < 3 || x >= size - 3 || y >= size - 3);
                    Color c = border ? edge : color;
                    c.a = color.a * inside;
                    px[y * size + x] = c;
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            Sprite sprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect);
            sprite.name = "fallback_" + ColorKey(color);
            return sprite;
        }

        /// <summary>九宫格面板精灵（ui_panel 之类）。有 border 才能用 Sliced 拉伸。</summary>
        public static bool HasBorder(Sprite s)
        {
            if (s == null)
            {
                return false;
            }
            Vector4 b = s.border;
            return b.x > 0.01f || b.y > 0.01f || b.z > 0.01f || b.w > 0.01f;
        }

        public static void ClearCache()
        {
            Cache.Clear();
            Fallbacks.Clear();
        }
    }
}
