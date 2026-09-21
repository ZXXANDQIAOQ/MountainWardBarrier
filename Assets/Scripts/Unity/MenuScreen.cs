using System;
using MountainWardBarrier.Core;
using UnityEngine;
using UnityEngine.UI;

namespace MountainWardBarrier.Game
{
    /// <summary>
    /// 主界面（山门）。
    ///
    /// 布局思路：上方是标题，中间是三张难度卡（点哪张就直接开哪一局），
    /// 下方一排图标按钮通往设置 / 成就 / 典籍 / 关于。
    /// 每张难度卡上会显示该难度的最佳纪录，让"再来一局"有个具体目标。
    /// </summary>
    public static class MenuScreen
    {
        public const string Version = "1.0.0";

        private static GameDatabase _db;

        /// <summary>菜单要用到难度文案，优先用运行期载入的那份配置，没有才现场兜一份。</summary>
        private static GameDatabase Db
        {
            get
            {
                GameBootstrap app = GameBootstrap.Instance;
                if (app != null && app.Db != null)
                {
                    return app.Db;
                }
                if (_db == null)
                {
                    _db = DefaultConfig.Build();
                }
                return _db;
            }
        }

        public static GameObject Build(RectTransform parent)
        {
            RectTransform root = UiKit.Node("MenuScreen", parent);
            UiKit.Stretch(root);
            GameBootstrap app = GameBootstrap.Instance;
            Vector2 canvas = app != null ? app.CanvasSize : new Vector2(UiKit.RefWidth, UiKit.RefHeight);

            BuildBackdrop(root);

            float y = -180f;
            BuildTitle(root, ref y);

            BuildDifficultyCards(root, y);
            BuildBottomActions(root, canvas);
            BuildStats(root, canvas);

            // 第一次进游戏的玩家直接拉去新手引导；引导做过一次就不再打扰。
            if (app != null && !SaveSystem.Data.tutorialCompleted)
            {
                app.ShowOverlay(Overlays.Tutorial(app));
            }

            return root.gameObject;
        }

        // ------------------------------------------------------------ 背景

        private static void BuildBackdrop(RectTransform root)
        {
            Image bg = UiKit.NewImage(root, "Bg", SpriteLibrary.Get("bg_menu"), Color.white);
            UiKit.Stretch(bg.rectTransform);
            bg.preserveAspect = false;

            // 底部的暗色压角，保证按钮和文字在任何屏幕比例下都读得清。
            Image shade = UiKit.NewImage(root, "BottomShade", SpriteLibrary.Get("ui_panel"),
                new Color(0.03f, 0.04f, 0.07f, 0.72f));
            RectTransform srt = shade.rectTransform;
            srt.anchorMin = new Vector2(0f, 0f);
            srt.anchorMax = new Vector2(1f, 0.62f);
            srt.pivot = new Vector2(0.5f, 0f);
            srt.offsetMin = Vector2.zero;
            srt.offsetMax = Vector2.zero;

            Image top = UiKit.NewImage(root, "TopShade", SpriteLibrary.Get("ui_panel"),
                new Color(0.02f, 0.03f, 0.06f, 0.55f));
            RectTransform trt = top.rectTransform;
            trt.anchorMin = new Vector2(0f, 1f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.pivot = new Vector2(0.5f, 1f);
            trt.offsetMin = new Vector2(0f, -420f);
            trt.offsetMax = Vector2.zero;
        }

        private static void BuildTitle(RectTransform root, ref float y)
        {
            Text title = UiKit.NewLabel(root, "Title", "仙侠 · 护山大阵", 96, Palette.Gold,
                TextAnchor.MiddleCenter);
            title.fontStyle = FontStyle.Bold;
            UiKit.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, y), new Vector2(960f, 120f));
            y -= 108f;

            Text sub = UiKit.NewLabel(root, "Subtitle", "布阵 · 御妖 · 守山门", 36,
                Palette.TextDim, TextAnchor.MiddleCenter);
            UiKit.Place(sub.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, y), new Vector2(860f, 54f));
            y -= 84f;

            // 一根金色细线，替代花哨的装饰
            Image line = UiKit.NewImage(root, "Divider", SpriteLibrary.Get("ui_bar_fill"),
                new Color(0.95f, 0.78f, 0.38f, 0.55f));
            UiKit.Place(line.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, y), new Vector2(520f, 4f));
            y -= 60f;
        }

        // ------------------------------------------------------------ 难度卡

        private static void BuildDifficultyCards(RectTransform root, float topY)
        {
            SaveData save = SaveSystem.Data;
            const float cardW = 880f;
            const float cardH = 208f;
            const float gap = 26f;

            for (int i = 0; i < 3; i++)
            {
                Difficulty diff = (Difficulty)i;
                DifficultyConfig cfg = Db.GetDifficulty(diff);
                if (cfg == null)
                {
                    continue;
                }
                float y = topY - i * (cardH + gap);

                Button card = UiKit.NewButton(root, "Diff_" + diff, "", new Vector2(cardW, cardH),
                    1, Palette.ButtonBg, MakeStartAction(diff));
                RectTransform rt = card.GetComponent<RectTransform>();
                UiKit.Place(rt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, y), new Vector2(cardW, cardH));

                // 左侧：难度名 + 说明
                Text name = UiKit.NewLabel(rt, "Name", cfg.displayName, UiKit.FontTitle,
                    DifficultyColor(diff), TextAnchor.MiddleLeft);
                UiKit.Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(38f, -26f), new Vector2(420f, 82f));

                Text desc = UiKit.NewLabel(rt, "Desc", cfg.description, UiKit.FontSmall,
                    Palette.TextDim, TextAnchor.UpperLeft);
                UiKit.Place(desc.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(40f, -114f), new Vector2(520f, 80f));

                // 右侧：纪录
                int best = save.BestWave(diff);
                float bestTime = save.BestTime(diff);
                bool cleared = bestTime > 0f;
                string record = cleared
                    ? "已通关 " + FormatTime(bestTime) + "\n最高 第 " + best + " 波"
                    : (best > 0 ? "最高 第 " + best + " 波" : "尚未挑战");
                Text rec = UiKit.NewLabel(rt, "Record", record, UiKit.FontSmall,
                    cleared ? Palette.JadeLight : Palette.TextDim, TextAnchor.MiddleRight);
                UiKit.Place(rec.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                    new Vector2(-118f, -6f), new Vector2(300f, 100f));

                Image arrow = UiKit.NewIcon(rt, "Arrow", "ui_arrow_up", 56f,
                    cleared ? Palette.Gold : Palette.TextDim);
                UiKit.Place(arrow.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                    new Vector2(-36f, 0f), new Vector2(56f, 56f));
                arrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 90f);

                if (cleared)
                {
                    Image mark = UiKit.NewIcon(rt, "ClearMark", "ui_check", 44f, Palette.Jade);
                    UiKit.Place(mark.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f),
                        new Vector2(40f, 22f), new Vector2(44f, 44f));
                }
            }
        }

        private static Action MakeStartAction(Difficulty diff)
        {
            return delegate
            {
                GameBootstrap app = GameBootstrap.Instance;
                if (app == null)
                {
                    return;
                }
                app.PlayClick();
                SaveSystem.Data.preferredDifficulty = (int)diff;
                SaveSystem.Save();
                app.StartBattle(diff);
            };
        }

        public static Color DifficultyColor(Difficulty d)
        {
            switch (d)
            {
                case Difficulty.Easy: return Palette.JadeLight;
                case Difficulty.Hard: return Palette.TextBad;
                default: return Palette.Gold;
            }
        }

        // ------------------------------------------------------------ 底部入口

        private static void BuildBottomActions(RectTransform root, Vector2 canvas)
        {
            float y = 300f;
            float size = 116f;
            float gap = 40f;
            float total = size * 4f + gap * 3f;
            float startX = -total * 0.5f + size * 0.5f;

            AddAction(root, "BtnSettings", "ui_gear", "设置", size,
                new Vector2(startX, y), delegate (GameBootstrap app)
                {
                    app.ShowOverlay(Overlays.Settings(app));
                });

            AddAction(root, "BtnAchievement", "ui_trophy", "成就", size,
                new Vector2(startX + (size + gap), y), delegate (GameBootstrap app)
                {
                    app.ShowOverlay(Overlays.AchievementsPanel(app));
                });

            AddAction(root, "BtnCodex", "ui_book", "典籍", size,
                new Vector2(startX + (size + gap) * 2f, y), delegate (GameBootstrap app)
                {
                    app.ShowOverlay(Overlays.Codex(app));
                });

            AddAction(root, "BtnAbout", "ui_info", "关于", size,
                new Vector2(startX + (size + gap) * 3f, y), delegate (GameBootstrap app)
                {
                    app.ShowOverlay(Overlays.About(app));
                });
        }

        private static void AddAction(RectTransform root, string name, string sprite, string label,
            float size, Vector2 pos, Action<GameBootstrap> onClick)
        {
            Button btn = UiKit.NewIconButton(root, name, sprite, size, Palette.ButtonBg,
                delegate
                {
                    GameBootstrap app = GameBootstrap.Instance;
                    if (app == null)
                    {
                        return;
                    }
                    app.PlayClick();
                    onClick(app);
                });
            UiKit.Place(btn.GetComponent<RectTransform>(), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), pos, new Vector2(size, size));

            Text cap = UiKit.NewLabel(btn.transform, "Caption", label, UiKit.FontSmall,
                Palette.TextDim, TextAnchor.MiddleCenter);
            UiKit.Place(cap.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f),
                new Vector2(0f, -6f), new Vector2(size + 40f, 34f));
        }

        // ------------------------------------------------------------ 统计

        private static void BuildStats(RectTransform root, Vector2 canvas)
        {
            SaveData save = SaveSystem.Data;
            int unlocked = Achievements.UnlockedCount(save);
            bool allClear = save.BestTime(Difficulty.Easy) > 0f
                && save.BestTime(Difficulty.Normal) > 0f
                && save.BestTime(Difficulty.Hard) > 0f;

            string stats = string.Format(
                "对局 {0} · 胜 {1} · 败 {2} · 斩妖 {3}\n成就 {4}/{5} · 累计灵气 {6} · 时长 {7}",
                save.totalBattles, save.totalVictories, save.totalDefeats, save.totalKills,
                unlocked, Achievements.Count, Mathf.RoundToInt(save.totalSpiritEarned),
                FormatTime(save.totalPlayTime));

            Text text = UiKit.NewLabel(root, "Stats", stats, UiKit.FontSmall,
                allClear ? Palette.Gold : Palette.TextDim, TextAnchor.MiddleCenter);
            UiKit.Place(text.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 176f), new Vector2(canvas.x - 80f, 100f));

            if (allClear)
            {
                Text badge = UiKit.NewLabel(root, "AllClear", "三山皆平 · 山门永固", UiKit.FontBody,
                    Palette.GoldLight, TextAnchor.MiddleCenter);
                UiKit.Place(badge.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0f, 96f), new Vector2(canvas.x - 80f, 50f));
            }

            Text ver = UiKit.NewLabel(root, "Version", "v" + Version + " · 单机版", UiKit.FontSmall,
                new Color(0.62f, 0.66f, 0.72f, 0.85f), TextAnchor.MiddleCenter);
            UiKit.Place(ver.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 40f), new Vector2(600f, 40f));
        }

        // ------------------------------------------------------------ 小工具

        public static string FormatTime(float seconds)
        {
            if (seconds <= 0f)
            {
                return "--:--";
            }
            int total = Mathf.RoundToInt(seconds);
            int m = total / 60;
            int s = total % 60;
            if (m >= 60)
            {
                return (m / 60) + "时" + (m % 60) + "分";
            }
            return m.ToString("00") + ":" + s.ToString("00");
        }
    }
}
