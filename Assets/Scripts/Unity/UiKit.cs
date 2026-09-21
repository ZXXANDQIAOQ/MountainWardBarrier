using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MountainWardBarrier.Game
{
    /// <summary>
    /// uGUI 搭建工具箱。
    ///
    /// 整个游戏的界面都是这里用代码搭出来的 —— 不依赖任何 prefab，
    /// 所以工程里没有脆弱的 .prefab / .unity 二进制引用，从 git 拉下来打开就能跑。
    /// </summary>
    public static class UiKit
    {
        public const float RefWidth = 1080f;
        public const float RefHeight = 1920f;

        public static readonly int FontTitle = 66;
        public static readonly int FontH1 = 46;
        public static readonly int FontH2 = 38;
        public static readonly int FontBody = 32;
        public static readonly int FontSmall = 26;

        private static Font _font;

        // ------------------------------------------------------------ 字体

        /// <summary>
        /// 取一个「能显示中文」的字体。
        ///
        /// Unity 内置的 Arial / LegacyRuntime 都不含中文字形，直接用会显示成方块，
        /// 所以这里优先向操作系统要字体：Windows 上会拿到微软雅黑，Android 上会拿到
        /// Noto Sans CJK（几乎所有国产/国际 ROM 都自带）。全都拿不到才退回内置字体。
        /// </summary>
        public static Font GetFont()
        {
            if (_font != null)
            {
                return _font;
            }
            string[] candidates = new string[]
            {
                "Microsoft YaHei", "微软雅黑", "Microsoft YaHei UI",
                "Noto Sans CJK SC", "Noto Sans SC", "Source Han Sans CN", "Source Han Sans SC",
                "Droid Sans Fallback", "PingFang SC", "Heiti SC", "Hiragino Sans GB",
                "WenQuanYi Micro Hei", "SimHei", "SimSun", "sans-serif"
            };
            try
            {
                _font = Font.CreateDynamicFontFromOSFont(candidates, 42);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[UiKit] 无法创建系统中文字体：" + e.Message);
            }
            if (_font == null)
            {
                _font = TryBuiltin("LegacyRuntime.ttf");
            }
            if (_font == null)
            {
                _font = TryBuiltin("Arial.ttf");
            }
            if (_font == null)
            {
                Debug.LogWarning("[UiKit] 没有任何可用字体，界面文字可能不显示。");
            }
            return _font;
        }

        private static Font TryBuiltin(string name)
        {
            try
            {
                return Resources.GetBuiltinResource<Font>(name);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>首次使用中文字体时预热一次，避免开局第一帧卡顿。</summary>
        public static void WarmUpFont()
        {
            Font f = GetFont();
            if (f != null)
            {
                f.RequestCharactersInTexture("仙侠护山大阵聚灵攻击困幻盾剑符丹雷冰爆灵气波次耐久布阵升级铲除",
                    42, FontStyle.Normal);
            }
        }

        // ------------------------------------------------------------ 节点

        public static RectTransform Node(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            rt.anchoredPosition3D = Vector3.zero;
            return rt;
        }

        /// <summary>铺满父节点，四边留出指定内缩。</summary>
        public static RectTransform Stretch(RectTransform rt, float left = 0f, float bottom = 0f,
            float right = 0f, float top = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        /// <summary>定尺寸、按锚点定位。</summary>
        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pivot,
            Vector2 anchoredPos, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            return rt;
        }

        /// <summary>横向铺满、贴在父节点顶部的条带。</summary>
        public static RectTransform TopStrip(RectTransform rt, float height)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(0f, -height);
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        /// <summary>横向铺满、贴在父节点底部的条带。</summary>
        public static RectTransform BottomStrip(RectTransform rt, float height)
        {
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = new Vector2(0f, height);
            return rt;
        }

        // ------------------------------------------------------------ 视觉元素

        public static Image NewImage(Transform parent, string name, Sprite sprite, Color color)
        {
            RectTransform rt = Node(name, parent);
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            img.type = Image.Type.Simple;
            return img;
        }

        /// <summary>九宫格面板：有 border 就用 Sliced（圆角不被拉伸变形），没有就退化成正片。</summary>
        public static Image NewPanel(Transform parent, string name, Color color, bool dark = false)
        {
            string spriteName = dark ? "ui_panel_dark" : "ui_panel";
            Sprite sprite = SpriteLibrary.Get(spriteName);
            Image img = NewImage(parent, name, sprite != null ? sprite
                : SpriteLibrary.MakeRoundedSprite(color, 64, 16), color);
            if (sprite != null && SpriteLibrary.HasBorder(sprite))
            {
                img.type = Image.Type.Sliced;
            }
            return img;
        }

        public static Image NewIcon(Transform parent, string name, string spriteName, float size, Color tint)
        {
            Image img = NewImage(parent, name,
                SpriteLibrary.GetOrFallback(spriteName, tint), tint);
            img.rectTransform.sizeDelta = new Vector2(size, size);
            img.preserveAspect = true;
            return img;
        }

        public static Image NewIconRect(Transform parent, string name, string spriteName,
            Vector2 size, Color tint)
        {
            Image img = NewImage(parent, name,
                SpriteLibrary.GetOrFallback(spriteName, tint), tint);
            img.rectTransform.sizeDelta = size;
            img.preserveAspect = true;
            return img;
        }

        public static Text NewLabel(Transform parent, string name, string text, int fontSize,
            Color color, TextAnchor anchor)
        {
            RectTransform rt = Node(name, parent);
            Text t = rt.gameObject.AddComponent<Text>();
            t.font = GetFont();
            t.text = text;
            t.fontSize = fontSize;
            t.color = color;
            t.alignment = anchor;
            t.supportRichText = true;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.lineSpacing = 1.15f;
            return t;
        }

        /// <summary>一行「图标 + 文字」的紧凑信息块，返回文字控件方便后续改内容。</summary>
        public static Text NewIconLabel(Transform parent, string name, string spriteName,
            string text, int fontSize, Color tint, float iconSize)
        {
            RectTransform row = Node(name, parent);
            row.sizeDelta = new Vector2(iconSize + 12f, iconSize);

            Image icon = NewIcon(row, "Icon", spriteName, iconSize, tint);
            Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0f, 0f), new Vector2(iconSize, iconSize));

            Text label = NewLabel(row, "Text", text, fontSize, Palette.TextMain, TextAnchor.MiddleLeft);
            RectTransform lrt = label.rectTransform;
            lrt.anchorMin = new Vector2(0f, 0f);
            lrt.anchorMax = new Vector2(1f, 1f);
            lrt.pivot = new Vector2(0f, 0.5f);
            lrt.offsetMin = new Vector2(iconSize + 10f, 0f);
            lrt.offsetMax = Vector2.zero;
            return label;
        }

        // ------------------------------------------------------------ 交互元素

        public static Button NewButton(Transform parent, string name, string label, Vector2 size,
            int fontSize, Color background, Action onClick)
        {
            Image bg = NewPanel(parent, name, background, true);
            bg.raycastTarget = true;
            RectTransform rt = bg.rectTransform;
            rt.sizeDelta = size;

            Button btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = bg;
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            cb.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            cb.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            cb.fadeDuration = 0.06f;
            btn.colors = cb;

            Text text = NewLabel(rt, "Label", label, fontSize, Palette.TextMain, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform, 10f, 4f, 10f, 4f);

            if (onClick != null)
            {
                btn.onClick.AddListener(delegate { onClick(); });
            }
            return btn;
        }

        /// <summary>图标按钮（方形，带底）。</summary>
        public static Button NewIconButton(Transform parent, string name, string spriteName,
            float size, Color background, Action onClick)
        {
            Button btn = NewButton(parent, name, "", new Vector2(size, size), 1, background, onClick);
            Image icon = NewIcon(btn.transform, "Icon", spriteName, size * 0.66f, Palette.TextMain);
            Place(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(size * 0.66f, size * 0.66f));
            return btn;
        }

        public static Slider NewSlider(Transform parent, string name, Vector2 size, float value,
            Action<float> onChange)
        {
            RectTransform rt = Node(name, parent);
            rt.sizeDelta = size;

            Image bg = NewImage(rt, "Background", SpriteLibrary.Get("ui_bar_bg"),
                new Color(1f, 1f, 1f, 0.85f));
            Place(bg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(size.x, size.y * 0.42f));
            bg.raycastTarget = true;

            RectTransform fillArea = Node("FillArea", rt);
            Place(fillArea, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(size.x - 12f, size.y * 0.42f));
            Image fill = NewImage(fillArea, "Fill", SpriteLibrary.Get("ui_bar_fill"), Palette.Jade);
            Stretch(fill.rectTransform);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillAmount = Mathf.Clamp01(value);

            Slider slider = rt.gameObject.AddComponent<Slider>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.fillRect = fill.rectTransform;
            slider.targetGraphic = bg;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = Mathf.Clamp01(value);
            slider.wholeNumbers = false;
            if (onChange != null)
            {
                slider.onValueChanged.AddListener(delegate (float v) { onChange(v); });
            }
            return slider;
        }

        /// <summary>可滚动列表。返回 ScrollRect，content 就是往里塞内容的父节点。</summary>
        public static ScrollRect NewScroll(Transform parent, string name, float spacing,
            RectOffset padding, out RectTransform content)
        {
            RectTransform rt = Node(name, parent);
            Stretch(rt);

            RectTransform viewport = Node("Viewport", rt);
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();

            content = Node("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 0f);

            VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = padding;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.UpperCenter;

            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            ScrollRect scroll = rt.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.elasticity = 0.12f;
            scroll.inertia = true;
            scroll.decelerationRate = 0.08f;
            scroll.scrollSensitivity = 42f;
            scroll.viewport.gameObject.AddComponent<EmptyGraphic>();
            return scroll;
        }

        /// <summary>给列表项做一个固定高度的"行"。</summary>
        public static RectTransform NewRow(Transform parent, string name, float height)
        {
            RectTransform rt = Node(name, parent);
            LayoutElement le = rt.gameObject.AddComponent<LayoutElement>();
            le.minHeight = height;
            le.preferredHeight = height;
            le.flexibleHeight = 0f;
            return rt;
        }

        /// <summary>水平排列容器。</summary>
        public static RectTransform NewRowHorizontal(Transform parent, string name, float height,
            float spacing, TextAnchor align)
        {
            RectTransform rt = Node(name, parent);
            HorizontalLayoutGroup layout = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = align;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            LayoutElement le = rt.gameObject.AddComponent<LayoutElement>();
            le.minHeight = height;
            le.preferredHeight = height;
            return rt;
        }

        // ------------------------------------------------------------ 全屏遮罩层

        public static RectTransform NewOverlayRoot(Transform parent, string name, Color scrim)
        {
            RectTransform rt = Node(name, parent);
            Stretch(rt);
            Image bg = NewImage(rt, "Scrim", null, scrim);
            Stretch(bg.rectTransform);
            bg.raycastTarget = true;
            return rt;
        }

        // ------------------------------------------------------------ 对象池

        /// <summary>
        /// 按 id 复用的 Image 池。
        ///
        /// 用法：
        ///     pool.Begin();
        ///     foreach (entity) { Image img = pool.Use(entity.id); ...摆位... }
        ///     pool.End();   // 回收这一帧没出现过的
        ///
        /// 这样即使妖魔中途死亡导致列表顺序变化，每个 id 对应的图形对象也不会串位。
        /// </summary>
        public class IdPool
        {
            private readonly Transform _parent;
            private readonly Dictionary<int, Image> _live = new Dictionary<int, Image>();
            private readonly HashSet<int> _seen = new HashSet<int>();
            private readonly Stack<Image> _free = new Stack<Image>();
            private readonly List<int> _stale = new List<int>();
            private readonly string _name;
            private readonly Sprite _defaultSprite;
            private readonly Color _defaultColor;

            public IdPool(Transform parent, string name, Sprite defaultSprite, Color defaultColor)
            {
                _parent = parent;
                _name = name;
                _defaultSprite = defaultSprite;
                _defaultColor = defaultColor;
            }

            public int LiveCount
            {
                get { return _live.Count; }
            }

            public void Begin()
            {
                _seen.Clear();
            }

            public Image Use(int id)
            {
                Image img;
                if (_live.TryGetValue(id, out img))
                {
                    _seen.Add(id);
                    if (!img.gameObject.activeSelf)
                    {
                        img.gameObject.SetActive(true);
                    }
                    return img;
                }
                if (_free.Count > 0)
                {
                    img = _free.Pop();
                    img.gameObject.SetActive(true);
                }
                else
                {
                    img = NewImage(_parent, _name + "_" + id, _defaultSprite, _defaultColor);
                    img.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                    img.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    img.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                }
                _live[id] = img;
                _seen.Add(id);
                return img;
            }

            public void End()
            {
                _stale.Clear();
                foreach (KeyValuePair<int, Image> kv in _live)
                {
                    if (!_seen.Contains(kv.Key))
                    {
                        _stale.Add(kv.Key);
                    }
                }
                for (int i = 0; i < _stale.Count; i++)
                {
                    Image img = _live[_stale[i]];
                    img.gameObject.SetActive(false);
                    _free.Push(img);
                    _live.Remove(_stale[i]);
                }
            }

            public void Clear()
            {
                foreach (KeyValuePair<int, Image> kv in _live)
                {
                    if (kv.Value != null)
                    {
                        UnityEngine.Object.Destroy(kv.Value.gameObject);
                    }
                }
                _live.Clear();
                while (_free.Count > 0)
                {
                    Image img = _free.Pop();
                    if (img != null)
                    {
                        UnityEngine.Object.Destroy(img.gameObject);
                    }
                }
            }
        }

        /// <summary>没有图形也能吃到触摸事件的透明块（ScrollRect 的 viewport 需要它）。</summary>
        public class EmptyGraphic : MaskableGraphic
        {
            protected override void OnPopulateMesh(VertexHelper vh)
            {
                vh.Clear();
            }
        }
    }
}
