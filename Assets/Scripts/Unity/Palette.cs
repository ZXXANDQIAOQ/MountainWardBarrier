using UnityEngine;

namespace MountainWardBarrier.Game
{
    /// <summary>
    /// 全局配色。与 tools/gen_sprites.py 里那套调色板保持一致，
    /// 这样程序生成的图标和代码写的 UI 是同一套视觉语言。
    /// </summary>
    public static class Palette
    {
        public static readonly Color Ink = Hex(0x10121A);
        public static readonly Color InkSoft = Hex(0x2A3040);

        public static readonly Color Jade = Hex(0x60D6B2);
        public static readonly Color JadeDark = Hex(0x1A645E);
        public static readonly Color JadeLight = Hex(0xBAF4DC);

        public static readonly Color Gold = Hex(0xF2C660);
        public static readonly Color GoldDark = Hex(0xA06E1E);
        public static readonly Color GoldLight = Hex(0xFFEAAE);

        public static readonly Color Cinnabar = Hex(0xCE3E3A);
        public static readonly Color CinnabarDark = Hex(0x801F28);

        public static readonly Color Purple = Hex(0x986EE4);
        public static readonly Color Ice = Hex(0x8ADAF8);
        public static readonly Color Bone = Hex(0xEEE8D6);
        public static readonly Color Stone = Hex(0x7E8694);
        public static readonly Color Demon = Hex(0xB23E54);

        public static readonly Color TextMain = Hex(0xEEE8D6);
        public static readonly Color TextDim = Hex(0x9AA6B4);
        public static readonly Color TextWarn = Hex(0xFFB454);
        public static readonly Color TextBad = Hex(0xFF7A6A);

        /// <summary>灵气不足 / 危险提示。</summary>
        public static readonly Color Danger = Hex(0xE05252);
        /// <summary>按钮底。</summary>
        public static readonly Color ButtonBg = new Color(0.11f, 0.16f, 0.22f, 0.92f);
        public static readonly Color PanelBg = new Color(0.09f, 0.12f, 0.17f, 0.94f);
        public static readonly Color Scrim = new Color(0f, 0f, 0f, 0.72f);

        public static Color Hex(int rgb)
        {
            float r = ((rgb >> 16) & 0xFF) / 255f;
            float g = ((rgb >> 8) & 0xFF) / 255f;
            float b = (rgb & 0xFF) / 255f;
            return new Color(r, g, b, 1f);
        }

        public static Color WithAlpha(Color c, float a)
        {
            return new Color(c.r, c.g, c.b, a);
        }

        /// <summary>按血量比例给妖魔染色：满血接近本色，残血偏红。</summary>
        public static Color EnemyTint(float hpRatio)
        {
            float t = 1f - Mathf.Clamp01(hpRatio);
            Color hurt = new Color(1f, 0.55f, 0.5f, 1f);
            return Color.Lerp(Color.white, hurt, t * 0.55f);
        }

        public static Color TowerTint(float hpRatio)
        {
            float t = 1f - Mathf.Clamp01(hpRatio);
            Color hurt = new Color(1f, 0.6f, 0.55f, 1f);
            return Color.Lerp(Color.white, hurt, t * 0.5f);
        }
    }
}
