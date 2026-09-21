using System.Collections.Generic;
using MountainWardBarrier.Core;
using UnityEngine;
using UnityEngine.UI;

namespace MountainWardBarrier.Game
{
    /// <summary>
    /// 结算界面。把 BattleReport 摊开成一张可读的战报，并提示新解锁的成就。
    ///
    /// 这里刻意不做"再来一局要不要花灵气"之类的延伸规则 —— 结算只是把结果讲清楚，
    /// 玩家的下一步永远是三个明确的选择：再来一次、换难度、或者先去看看成就。
    /// </summary>
    public class ResultScreen : MonoBehaviour
    {
        private GameBootstrap _app;
        private BattleReport _report;

        public void Setup(GameBootstrap app, BattleReport report, List<int> freshAchievements)
        {
            _app = app;
            _report = report;
            UiKit.Stretch((RectTransform)transform);
            BuildUi(freshAchievements);
        }

        private void BuildUi(List<int> fresh)
        {
            Vector2 canvas = _app != null ? _app.CanvasSize
                : new Vector2(UiKit.RefWidth, UiKit.RefHeight);

            Image bg = UiKit.NewImage(transform, "Bg", SpriteLibrary.Get("bg_menu"), Color.white);
            UiKit.Stretch(bg.rectTransform);
            bg.preserveAspect = false;

            Image scrim = UiKit.NewImage(transform, "Scrim", null,
                new Color(0.02f, 0.03f, 0.06f, 0.82f));
            UiKit.Stretch(scrim.rectTransform);

            bool win = _report.victory;

            // --- 标题
            Text title = UiKit.NewLabel(transform, "Title", win ? "守山成功" : "山门失守",
                96, win ? Palette.Gold : Palette.TextBad, TextAnchor.MiddleCenter);
            title.fontStyle = FontStyle.Bold;
            UiKit.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -132f), new Vector2(960f, 120f));

            DifficultyConfig dcfg = _app != null && _app.Db != null
                ? _app.Db.GetDifficulty(_report.difficulty) : null;
            string subText = (dcfg != null ? dcfg.displayName : "?") + " 难度 · 第 "
                + _report.waveReached + " / " + _report.totalWaves + " 波";
            if (win)
            {
                subText += " · 用时 " + MenuScreen.FormatTime(_report.duration);
            }
            Text sub = UiKit.NewLabel(transform, "Sub", subText, UiKit.FontH2,
                Palette.TextMain, TextAnchor.MiddleCenter);
            UiKit.Place(sub.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -240f), new Vector2(canvas.x - 80f, 56f));

            if (_report.newRecord)
            {
                Image badge = UiKit.NewPanel(transform, "NewRecord",
                    new Color(0.42f, 0.3f, 0.06f, 0.95f), true);
                UiKit.Place(badge.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -306f), new Vector2(340f, 62f));
                Text bt = UiKit.NewLabel(badge.transform, "Text", "刷新纪录！", UiKit.FontBody,
                    Palette.GoldLight, TextAnchor.MiddleCenter);
                UiKit.Stretch(bt.rectTransform, 8f, 4f, 8f, 4f);
            }

            // --- 数据面板
            RectTransform panel = BuildPanel(canvas);
            FillStats(panel);

            // --- 新成就
            float buttonsTop = 300f;
            if (fresh != null && fresh.Count > 0)
            {
                BuildFreshAchievements(fresh, buttonsTop + 40f);
            }

            // --- 按钮组
            BuildButtons(buttonsTop);
        }

        private RectTransform BuildPanel(Vector2 canvas)
        {
            Image panel = UiKit.NewPanel(transform, "Panel", Palette.PanelBg, true);
            RectTransform rt = panel.rectTransform;
            float w = Mathf.Min(canvas.x - 90f, 940f);
            float h = 800f;
            UiKit.Place(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 60f), new Vector2(w, h));
            return rt;
        }

        private void FillStats(RectTransform panel)
        {
            RectTransform content = UiKit.Node("Rows", panel);
            UiKit.Place(content, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -26f), new Vector2(panel.sizeDelta.x - 60f, 700f));

            float y = 0f;
            const float rowH = 96f;

            AddRow(content, ref y, "ui_wave", "推进波次",
                _report.waveReached + " / " + _report.totalWaves,
                _report.victory ? Palette.JadeLight : Palette.TextWarn, rowH);

            AddRow(content, ref y, "ui_core", "山门耐久",
                Mathf.RoundToInt(_report.coreHpLeft) + " / " + Mathf.RoundToInt(_report.coreHpMax)
                + "  (" + Mathf.RoundToInt(_report.CoreHpRatio * 100f) + "%)",
                _report.CoreHpRatio > 0.5f ? Palette.JadeLight : Palette.TextBad, rowH);

            AddRow(content, ref y, "ui_skull", "斩妖 / 漏怪",
                _report.kills + " / " + _report.leaked,
                _report.leaked == 0 ? Palette.JadeLight : Palette.TextWarn, rowH);

            AddRow(content, ref y, "ui_upgrade", "构筑",
                "阵法 " + _report.towersBuilt + "（铲除 " + _report.towersSold + "）· 仙灵 "
                + _report.spiritsSummoned + " · 符箓 " + _report.trapsPlaced,
                Palette.TextMain, rowH);

            AddRow(content, ref y, "ui_spirit", "灵气收支",
                "+" + Mathf.RoundToInt(_report.spiritEarned) + " / -"
                + Mathf.RoundToInt(_report.spiritSpent),
                Palette.JadeLight, rowH);

            AddRow(content, ref y, "fx_bolt", "总伤害",
                Mathf.RoundToInt(_report.damageDealt).ToString(),
                Palette.GoldLight, rowH);

            AddRow(content, ref y, "ui_star", "终极技",
                _report.skillCasts + " 次（天雷 " + _report.thunderCasts + " 次）",
                Palette.TextMain, rowH);

            AddRow(content, ref y, "ui_clock", "耗时",
                MenuScreen.FormatTime(_report.duration), Palette.TextMain, rowH);
        }

        private void AddRow(RectTransform parent, ref float y, string sprite, string label,
            string value, Color valueColor, float rowH)
        {
            RectTransform row = UiKit.Node("Row_" + label, parent);
            UiKit.Place(row, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -y), new Vector2(parent.sizeDelta.x, rowH));
            y += rowH;

            // 交替底纹，让长表格不串行
            int index = Mathf.RoundToInt(y / rowH);
            if (index % 2 == 1)
            {
                Image stripe = UiKit.NewImage(row, "Stripe", SpriteLibrary.Get("ui_panel"),
                    new Color(1f, 1f, 1f, 0.045f));
                UiKit.Stretch(stripe.rectTransform);
                if (SpriteLibrary.HasBorder(stripe.sprite))
                {
                    stripe.type = Image.Type.Sliced;
                }
            }

            Image icon = UiKit.NewIcon(row, "Icon", sprite, 52f, Palette.TextDim);
            UiKit.Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(8f, 0f), new Vector2(52f, 52f));

            Text l = UiKit.NewLabel(row, "Label", label, UiKit.FontBody, Palette.TextDim,
                TextAnchor.MiddleLeft);
            UiKit.Place(l.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(76f, 0f), new Vector2(280f, rowH - 10f));

            Text v = UiKit.NewLabel(row, "Value", value, UiKit.FontBody, valueColor,
                TextAnchor.MiddleRight);
            UiKit.Place(v.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-10f, 0f), new Vector2(parent.sizeDelta.x - 380f, rowH - 10f));
        }

        private void BuildFreshAchievements(List<int> fresh, float topY)
        {
            Image panel = UiKit.NewPanel(transform, "FreshPanel",
                new Color(0.13f, 0.22f, 0.2f, 0.96f), true);
            RectTransform rt = panel.rectTransform;
            float h = 92f + fresh.Count * 70f;
            UiKit.Place(rt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, topY), new Vector2(Mathf.Min(UiKit.RefWidth - 90f, 940f), h));

            Text head = UiKit.NewLabel(rt, "Head", "新解锁成就", UiKit.FontH2, Palette.Gold,
                TextAnchor.MiddleCenter);
            UiKit.Place(head.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -8f), new Vector2(860f, 48f));

            float y = 62f;
            for (int i = 0; i < fresh.Count; i++)
            {
                int index = fresh[i];
                if (index < 0 || index >= Achievements.Count)
                {
                    continue;
                }
                AchievementDef def = Achievements.All[index];
                RectTransform row = UiKit.Node("Fresh_" + index, rt);
                UiKit.Place(row, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -y), new Vector2(880f, 64f));
                y += 70f;

                Image star = UiKit.NewIcon(row, "Star", "ui_star", 44f, Palette.GoldLight);
                UiKit.Place(star.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(6f, 0f), new Vector2(44f, 44f));

                Text t = UiKit.NewLabel(row, "Text",
                    "<b>" + def.name + "</b>　" + def.description, UiKit.FontSmall,
                    Palette.TextMain, TextAnchor.MiddleLeft);
                UiKit.Place(t.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(62f, 0f), new Vector2(800f, 60f));
            }
        }

        private void BuildButtons(float topY)
        {
            float y = topY;
            Button again = UiKit.NewButton(transform, "Again",
                _report.victory ? "再守一局" : "再来一次", new Vector2(560f, 108f),
                UiKit.FontH1, Palette.JadeDark, delegate
                {
                    Click();
                    _app.StartBattle(_report.difficulty);
                });
            UiKit.Place(again.GetComponent<RectTransform>(), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, y), new Vector2(560f, 108f));
            y -= 118f;

            Button menu = UiKit.NewButton(transform, "Menu", "返回山门", new Vector2(560f, 96f),
                UiKit.FontBody, Palette.ButtonBg, delegate
                {
                    Click();
                    _app.ShowMenu();
                });
            UiKit.Place(menu.GetComponent<RectTransform>(), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, y), new Vector2(560f, 96f));
            y -= 106f;

            Button ach = UiKit.NewButton(transform, "Achievements", "成就一览",
                new Vector2(560f, 96f), UiKit.FontBody, Palette.ButtonBg, delegate
                {
                    Click();
                    _app.ShowOverlay(Overlays.AchievementsPanel(_app));
                });
            UiKit.Place(ach.GetComponent<RectTransform>(), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, y), new Vector2(560f, 96f));
        }

        private void Click()
        {
            if (_app != null)
            {
                _app.PlayClick();
            }
        }
    }
}
