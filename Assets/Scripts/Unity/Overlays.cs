using System;
using System.Collections.Generic;
using MountainWardBarrier.Core;
using UnityEngine;
using UnityEngine.UI;

namespace MountainWardBarrier.Game
{
    /// <summary>
    /// 所有弹窗：设置、成就、典籍、关于、新手引导。
    ///
    /// 它们都是"非模态的全屏遮罩 + 一块居中面板"。之所以不写成 MonoBehaviour，
    /// 是因为这些面板没有自己的状态 —— 需要改的东西直接改 SaveSystem.Data 再存盘，
    /// 需要关就调 app.CloseOverlay()，面板本身只是当场搭出来的一堆控件。
    /// </summary>
    public static class Overlays
    {
        private static GameDatabase _db;

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

        // ============================================================ 面板骨架

        private class Frame
        {
            public GameObject Root;
            public RectTransform Panel;
            public RectTransform Content;
            public float Width;
        }

        /// <summary>搭一块"遮罩 + 面板 + 标题栏 + 关闭按钮"的骨架。</summary>
        private static Frame BuildFrame(GameBootstrap app, string name, string title,
            string subtitle, Vector2 size)
        {
            Frame f = new Frame();
            RectTransform root = app.NewOverlay(name);
            f.Root = root.gameObject;
            f.Width = size.x - 60f;

            Image panel = UiKit.NewPanel(root, "Panel", Palette.PanelBg, true);
            UiKit.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, size);
            f.Panel = panel.rectTransform;

            // 顶部留一条略亮的带子做标题区
            Image strip = UiKit.NewImage(panel.transform, "Header", SpriteLibrary.Get("ui_panel"),
                new Color(1f, 1f, 1f, 0.06f));
            RectTransform hrt = strip.rectTransform;
            hrt.anchorMin = new Vector2(0f, 1f);
            hrt.anchorMax = new Vector2(1f, 1f);
            hrt.pivot = new Vector2(0.5f, 1f);
            hrt.offsetMin = new Vector2(0f, -122f);
            hrt.offsetMax = Vector2.zero;
            if (SpriteLibrary.HasBorder(strip.sprite))
            {
                strip.type = Image.Type.Sliced;
            }

            Text t = UiKit.NewLabel(panel.transform, "Title", title, UiKit.FontH1, Palette.Gold,
                TextAnchor.MiddleLeft);
            UiKit.Place(t.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(36f, subtitle.Length > 0 ? -22f : -38f), new Vector2(size.x - 220f, 58f));

            if (subtitle.Length > 0)
            {
                Text s = UiKit.NewLabel(panel.transform, "Sub", subtitle, UiKit.FontSmall,
                    Palette.TextDim, TextAnchor.MiddleLeft);
                UiKit.Place(s.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(38f, -78f), new Vector2(size.x - 220f, 40f));
            }

            Button close = UiKit.NewIconButton(panel.transform, "Close", "ui_close", 72f,
                Palette.ButtonBg, delegate
                {
                    app.PlayClick();
                    app.CloseOverlay();
                });
            UiKit.Place(close.GetComponent<RectTransform>(), new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(72f, 72f));

            f.Content = UiKit.Node("Content", panel.transform);
            RectTransform crt = f.Content;
            crt.anchorMin = new Vector2(0f, 0f);
            crt.anchorMax = new Vector2(1f, 1f);
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.offsetMin = new Vector2(0f, 0f);
            crt.offsetMax = new Vector2(0f, -130f);
            return f;
        }

        /// <summary>在内容区里做一条「标签 + 控件」的设置行；返回行的 RectTransform。</summary>
        private static RectTransform SettingRow(RectTransform content, float top, float height,
            string label, string hint)
        {
            RectTransform row = UiKit.Node("Row_" + label, content);
            UiKit.Place(row, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -top), new Vector2(content.sizeDelta.x - 48f, height));

            Text l = UiKit.NewLabel(row, "Label", label, UiKit.FontBody, Palette.TextMain,
                TextAnchor.MiddleLeft);
            UiKit.Place(l.rectTransform, new Vector2(0f, hint.Length > 0 ? 0.62f : 0.5f),
                new Vector2(0f, 0.5f), new Vector2(20f, 0f), new Vector2(420f, 46f));

            if (hint.Length > 0)
            {
                Text h = UiKit.NewLabel(row, "Hint", hint, UiKit.FontSmall, Palette.TextDim,
                    TextAnchor.MiddleLeft);
                UiKit.Place(h.rectTransform, new Vector2(0f, 0.18f), new Vector2(0f, 0.5f),
                    new Vector2(22f, 0f), new Vector2(520f, 36f));
            }
            return row;
        }

        /// <summary>开关按钮：点一下翻转，颜色跟着状态走。</summary>
        private static Button Switch(RectTransform row, bool value, Action<bool> onToggle)
        {
            bool state = value;
            Button btn = null;
            btn = UiKit.NewButton(row, "Switch", value ? "开" : "关", new Vector2(150f, 66f),
                UiKit.FontBody, value ? Palette.JadeDark : Palette.ButtonBg, delegate
                {
                    state = !state;
                    GameBootstrap app = GameBootstrap.Instance;
                    if (app != null)
                    {
                        app.PlayClick();
                    }
                    Text label = btn.transform.Find("Label").GetComponent<Text>();
                    Image bg = btn.GetComponent<Image>();
                    label.text = state ? "开" : "关";
                    bg.color = state ? Palette.JadeDark : Palette.ButtonBg;
                    onToggle(state);
                });
            UiKit.Place(btn.GetComponent<RectTransform>(), new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f), new Vector2(-20f, 0f), new Vector2(150f, 66f));
            return btn;
        }

        // ============================================================ 设置

        public static GameObject Settings(GameBootstrap app)
        {
            Frame f = BuildFrame(app, "SettingsPanel", "设置", "改动会立刻生效并写进本地存档",
                new Vector2(940f, 1180f));
            SaveData data = SaveSystem.Data;
            float top = 20f;
            const float rowH = 148f;

            // --- 背景音乐
            RectTransform music = SettingRow(f.Content, top, rowH, "背景音乐", "山门里的古筝与环境音");
            Switch(music, data.musicEnabled, delegate (bool on)
            {
                data.musicEnabled = on;
                SaveSystem.Save();
                app.ApplyAudioSettings();
            });
            Slider musicSlider = UiKit.NewSlider(music, "Slider", new Vector2(300f, 60f),
                data.musicVolume, delegate (float v)
                {
                    data.musicVolume = v;
                    if (AudioLibrary.Instance != null)
                    {
                        AudioLibrary.Instance.MusicVolume = v;
                    }
                });
            UiKit.Place(musicSlider.GetComponent<RectTransform>(), new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f), new Vector2(-190f, 0f), new Vector2(300f, 60f));
            top += rowH;

            // --- 音效
            RectTransform sfx = SettingRow(f.Content, top, rowH, "音效", "施法、命中、妖魔嘶吼");
            Switch(sfx, data.sfxEnabled, delegate (bool on)
            {
                data.sfxEnabled = on;
                SaveSystem.Save();
                app.ApplyAudioSettings();
                if (on)
                {
                    app.Audio.PlaySfx("sfx_click");
                }
            });
            Slider sfxSlider = UiKit.NewSlider(sfx, "Slider", new Vector2(300f, 60f),
                data.sfxVolume, delegate (float v)
                {
                    data.sfxVolume = v;
                    if (AudioLibrary.Instance != null)
                    {
                        AudioLibrary.Instance.SfxVolume = v;
                    }
                });
            UiKit.Place(sfxSlider.GetComponent<RectTransform>(), new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f), new Vector2(-190f, 0f), new Vector2(300f, 60f));
            top += rowH;

            // --- 伤害飘字
            RectTransform dmg = SettingRow(f.Content, top, rowH, "伤害飘字",
                "关掉后画面更干净，适合小屏机型");
            Switch(dmg, data.damageNumbers, delegate (bool on)
            {
                data.damageNumbers = on;
                SaveSystem.Save();
            });
            top += rowH;

            // --- 帧率
            RectTransform fps = SettingRow(f.Content, top, rowH, "显示帧率", "左上角常驻显示实时帧率");
            Switch(fps, data.showFps, delegate (bool on)
            {
                data.showFps = on;
                SaveSystem.Save();
                FpsCounter.SetVisible(on);
            });
            top += rowH;

            // --- 重看引导
            RectTransform guide = SettingRow(f.Content, top, rowH, "新手引导",
                "随时可以再看一遍布阵流程");
            Button replay = UiKit.NewButton(guide, "Replay", "重看引导", new Vector2(230f, 66f),
                UiKit.FontSmall, Palette.ButtonBg, delegate
                {
                    app.PlayClick();
                    app.ShowOverlay(Tutorial(app));
                });
            UiKit.Place(replay.GetComponent<RectTransform>(), new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f), new Vector2(-20f, 0f), new Vector2(230f, 66f));
            top += rowH + 10f;

            // --- 危险操作
            RectTransform wipe = SettingRow(f.Content, top, rowH, "抹除山门记录",
                "清空全部战绩、成就与设置，不可撤销");
            Button wipeBtn = UiKit.NewButton(wipe, "Wipe", "抹除", new Vector2(230f, 66f),
                UiKit.FontSmall, Palette.CinnabarDark, delegate
                {
                    app.PlayClick();
                    app.ShowOverlay(ConfirmWipe(app));
                });
            UiKit.Place(wipeBtn.GetComponent<RectTransform>(), new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f), new Vector2(-20f, 0f), new Vector2(230f, 66f));
            top += rowH + 20f;

            Text tip = UiKit.NewLabel(f.Content, "Tip",
                "本作完全单机：没有任何广告、内购与联网请求。\n存档只写在本机，卸载即清除。",
                UiKit.FontSmall, Palette.TextDim, TextAnchor.UpperLeft);
            UiKit.Place(tip.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -top), new Vector2(f.Width, 100f));

            return f.Root;
        }

        private static GameObject ConfirmWipe(GameBootstrap app)
        {
            Frame f = BuildFrame(app, "ConfirmWipe", "确认抹除？",
                "这一步之后什么都回不来了", new Vector2(840f, 560f));

            Text warn = UiKit.NewLabel(f.Content, "Warn",
                "将清空：\n· 各难度最高波次与最快通关记录\n· 全部成就解锁\n· 对局场次与击杀统计\n· 音量、难度偏好等设置",
                UiKit.FontBody, Palette.TextWarn, TextAnchor.UpperLeft);
            UiKit.Place(warn.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -30f), new Vector2(f.Width - 60f, 320f));

            Button no = UiKit.NewButton(f.Content, "No", "我再想想", new Vector2(340f, 92f),
                UiKit.FontH2, Palette.ButtonBg, delegate
                {
                    app.PlayClick();
                    app.ShowOverlay(Settings(app));
                });
            UiKit.Place(no.GetComponent<RectTransform>(), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(-180f, 40f), new Vector2(340f, 92f));

            Button yes = UiKit.NewButton(f.Content, "Yes", "确认抹除", new Vector2(340f, 92f),
                UiKit.FontH2, Palette.CinnabarDark, delegate
                {
                    app.PlayClick();
                    SaveSystem.ResetAll();
                    app.ApplyAudioSettings();
                    FpsCounter.SetVisible(SaveSystem.Data.showFps);
                    app.ShowMenu();
                });
            UiKit.Place(yes.GetComponent<RectTransform>(), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(180f, 40f), new Vector2(340f, 92f));

            return f.Root;
        }

        // ============================================================ 成就

        public static GameObject AchievementsPanel(GameBootstrap app)
        {
            SaveData save = SaveSystem.Data;
            int unlocked = Achievements.UnlockedCount(save);
            Frame f = BuildFrame(app, "AchievementPanel", "成就",
                "已解锁 " + unlocked + " / " + Achievements.Count, new Vector2(940f, 1400f));

            RectTransform content;
            UiKit.NewScroll(f.Content, "List", 14f, new RectOffset(24, 24, 12, 24), out content);

            for (int i = 0; i < Achievements.Count; i++)
            {
                AchievementDef def = Achievements.All[i];
                bool got = save.IsUnlocked(i);

                RectTransform row = UiKit.NewRow(content, "Ach_" + i, 132f);
                Image bg = UiKit.NewPanel(row, "Bg",
                    got ? new Color(0.14f, 0.23f, 0.21f, 0.95f) : new Color(0.1f, 0.12f, 0.16f, 0.9f),
                    true);
                UiKit.Stretch(bg.rectTransform);

                Image icon = UiKit.NewIcon(row, "Icon", got ? "ui_star" : "ui_lock", 64f,
                    got ? Palette.GoldLight : Palette.TextDim);
                UiKit.Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(26f, 0f), new Vector2(64f, 64f));

                Text name = UiKit.NewLabel(row, "Name", def.name, UiKit.FontBody,
                    got ? Palette.Gold : Palette.TextDim, TextAnchor.LowerLeft);
                UiKit.Place(name.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(106f, 8f), new Vector2(600f, 52f));

                Text desc = UiKit.NewLabel(row, "Desc", def.description, UiKit.FontSmall,
                    Palette.TextDim, TextAnchor.UpperLeft);
                UiKit.Place(desc.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(108f, -34f), new Vector2(640f, 52f));

                if (got)
                {
                    Image mark = UiKit.NewIcon(row, "Mark", "ui_check", 48f, Palette.Jade);
                    UiKit.Place(mark.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                        new Vector2(-26f, 0f), new Vector2(48f, 48f));
                }
            }

            return f.Root;
        }

        // ============================================================ 典籍

        public static GameObject Codex(GameBootstrap app)
        {
            Frame f = BuildFrame(app, "CodexPanel", "典籍",
                "阵、灵、符、法与妖魔的底细", new Vector2(960f, 1460f));

            RectTransform content;
            UiKit.NewScroll(f.Content, "List", 10f, new RectOffset(24, 24, 12, 28), out content);

            Section(content, "丙·终极技", "冷却结束后随时可放，是翻盘的关键");
            for (int i = 0; i < Db.skills.Count; i++)
            {
                SkillConfig c = Db.skills[i];
                string detail = string.Format("耗灵气 {0} · 冷却 {1}s", c.cost, c.cooldown.ToString("F0"));
                if (c.damage > 0f)
                {
                    detail += " · 伤害 " + c.damage.ToString("F0");
                }
                if (c.duration > 0f)
                {
                    detail += " · 持续 " + c.duration.ToString("F0") + "s";
                }
                if (c.coreRepairPercent > 0f)
                {
                    detail += " · 修复核心 " + (c.coreRepairPercent * 100f).ToString("F0") + "%";
                }
                Entry(content, c.sprite, c.displayName, c.description, detail, Palette.Gold);
            }

            Section(content, "甲·阵法", "只能布在空地上，是输出的骨架");
            for (int i = 0; i < Db.towers.Count; i++)
            {
                TowerConfig c = Db.towers[i];
                string detail = "造价 " + c.cost + " · 满级 Lv." + c.maxLevel;
                if (c.damage > 0f)
                {
                    detail += " · 伤害 " + c.damage.ToString("F0") + " / " + c.attackInterval.ToString("F2") + "s";
                }
                if (c.range > 0f)
                {
                    detail += " · 射程 " + c.range.ToString("F1") + " 格";
                }
                if (c.spiritPerSecond > 0f)
                {
                    detail += " · 产灵气 " + c.spiritPerSecond.ToString("F1") + "/s";
                }
                if (c.slowFactor > 0f && c.slowFactor < 1f)
                {
                    detail += " · 减速至 " + (c.slowFactor * 100f).ToString("F0") + "%";
                }
                if (c.dodgeChance > 0f)
                {
                    detail += " · 闪避 " + (c.dodgeChance * 100f).ToString("F0") + "%";
                }
                if (c.shieldPool > 0f)
                {
                    detail += " · 护盾 " + c.shieldPool.ToString("F0");
                }
                Entry(content, c.sprite, c.displayName, c.description, detail, Palette.JadeLight);
            }

            Section(content, "乙·仙灵", "召唤后驻守道路，会自己找目标打");
            for (int i = 0; i < Db.spirits.Count; i++)
            {
                SpiritConfig c = Db.spirits[i];
                string detail = string.Format("耗灵气 {0} · 存在 {1}s · 生命 {2}",
                    c.cost, c.duration.ToString("F0"), c.hp.ToString("F0"));
                if (c.damage > 0f)
                {
                    detail += " · 伤害 " + c.damage.ToString("F0");
                }
                if (c.coreRepairPerSecond > 0f)
                {
                    detail += " · 每秒修复核心 " + c.coreRepairPerSecond.ToString("F1");
                }
                Entry(content, c.sprite, c.displayName, c.description, detail, Palette.Purple);
            }

            Section(content, "丁·符箓", "埋在路面上，妖魔踩到才触发");
            for (int i = 0; i < Db.traps.Count; i++)
            {
                TrapConfig c = Db.traps[i];
                string detail = string.Format("耗灵气 {0} · 伤害 {1} · 范围 {2} 格",
                    c.cost, c.damage.ToString("F0"), c.radius.ToString("F1"));
                if (c.slowFactor > 0f && c.slowFactor < 1f)
                {
                    detail += " · 减速 " + (c.slowFactor * 100f).ToString("F0") + "% "
                        + c.slowDuration.ToString("F0") + "s";
                }
                Entry(content, c.sprite, c.displayName, "", detail, Palette.Ice);
            }

            Section(content, "戊·妖魔", "看清它们的能力，再决定怎么布阵");
            for (int i = 0; i < Db.enemies.Count; i++)
            {
                EnemyConfig c = Db.enemies[i];
                string detail = string.Format("生命 {0} · 护甲 {1}% · 速度 {2} · 赏金 {3}",
                    c.hp.ToString("F0"), (c.armor * 100f).ToString("F0"), c.speed.ToString("F1"),
                    c.reward);
                List<string> tags = new List<string>();
                if (c.shieldAmount > 0f)
                {
                    tags.Add("护盾 " + c.shieldAmount.ToString("F0"));
                }
                if (c.frenzyThreshold > 0f)
                {
                    tags.Add("残血狂暴");
                }
                if (c.attackDamage > 0f)
                {
                    tags.Add("会拆阵 " + c.attackDamage.ToString("F0"));
                }
                if (tags.Count > 0)
                {
                    detail += "\n" + string.Join(" · ", tags.ToArray());
                }
                Entry(content, c.sprite, c.displayName, "", detail, Palette.Cinnabar);
            }

            Section(content, "己·难度", "三个难度的波数与基准倍率");
            for (int i = 0; i < Db.difficulties.Count; i++)
            {
                DifficultyConfig c = Db.difficulties[i];
                string detail = string.Format("共 {0} 波 · 妖魔生命 ×{1} · 灵气获取 ×{2} · 核心耐久 {3}",
                    c.waveCount, c.enemyHpMultiplier.ToString("F2"),
                    c.spiritGainMultiplier.ToString("F2"), c.coreHp.ToString("F0"));
                Entry(content, "ui_wave", c.displayName, c.description, detail,
                    MenuScreen.DifficultyColor(c.difficulty));
            }

            return f.Root;
        }

        private static void Section(RectTransform content, string title, string hint)
        {
            RectTransform row = UiKit.NewRow(content, "Section_" + title, 96f);
            Text t = UiKit.NewLabel(row, "Title", title, UiKit.FontH2, Palette.Gold,
                TextAnchor.LowerLeft);
            UiKit.Place(t.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(10f, 10f), new Vector2(600f, 48f));

            Text h = UiKit.NewLabel(row, "Hint", hint, UiKit.FontSmall, Palette.TextDim,
                TextAnchor.UpperLeft);
            UiKit.Place(h.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(12f, -26f), new Vector2(760f, 40f));
        }

        private static void Entry(RectTransform content, string sprite, string name, string desc,
            string detail, Color accent)
        {
            bool hasDesc = !string.IsNullOrEmpty(desc);
            float h = hasDesc ? 168f : 138f;
            RectTransform row = UiKit.NewRow(content, "Entry_" + name, h);

            Image bg = UiKit.NewPanel(row, "Bg", new Color(0.1f, 0.13f, 0.18f, 0.9f), true);
            UiKit.Stretch(bg.rectTransform);

            Image icon = UiKit.NewIcon(row, "Icon", sprite, 76f, Color.white);
            UiKit.Place(icon.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(22f, -14f), new Vector2(76f, 76f));

            Text n = UiKit.NewLabel(row, "Name", name, UiKit.FontH2, accent, TextAnchor.LowerLeft);
            UiKit.Place(n.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(112f, -48f), new Vector2(520f, 50f));

            Text d = UiKit.NewLabel(row, "Detail", detail, UiKit.FontSmall, Palette.TextDim,
                TextAnchor.UpperLeft);
            UiKit.Place(d.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(114f, -58f), new Vector2(700f, 66f));

            if (hasDesc)
            {
                Text s = UiKit.NewLabel(row, "Desc", desc, UiKit.FontSmall, Palette.TextMain,
                    TextAnchor.UpperLeft);
                UiKit.Place(s.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f),
                    new Vector2(24f, 14f), new Vector2(820f, 48f));
            }
        }

        // ============================================================ 关于

        public static GameObject About(GameBootstrap app)
        {
            Frame f = BuildFrame(app, "AboutPanel", "关于", "v" + MenuScreen.Version,
                new Vector2(920f, 1220f));

            Text body = UiKit.NewLabel(f.Content, "Body",
                "<b>仙侠 · 护山大阵</b>\n" +
                "单机塔防。妖魔沿两条山路直扑山门，你需要在有限的地块上摆出阵法、召唤仙灵、"
                + "埋下符箓，在每一波之间抢时间扩张阵线。\n\n" +
                "<b>玩法要点</b>\n" +
                "· 阵法只能布在空地上，产灵气、打伤害、减速、闪避、代挡各有分工。\n" +
                "· 仙灵与符箓要放在道路上，仙灵会自己迎击，符箓踩到才触发。\n" +
                "· 每 5 波出现 BOSS 波，魔君会带护盾并拆掉挡路的阵法。\n" +
                "· 山门耐久归零即失守；撑满全部波次即守山成功。\n\n" +
                "<b>技术说明</b>\n" +
                "· 全部界面与棋盘由代码在运行时搭建，工程里没有 prefab 与场景二进制依赖。\n" +
                "· 全部美术与音效都是程序化生成的（Python 脚本见 tools/），不含任何第三方素材。\n" +
                "· 玩法逻辑位于 Assets/Scripts/Core，不依赖 UnityEngine，可脱离 Unity 单独编译与测试。\n\n" +
                "<b>承诺</b>\n" +
                "无广告、无内购、无账号、无联网请求。存档只写在本机。",
                UiKit.FontSmall, Palette.TextMain, TextAnchor.UpperLeft);
            UiKit.Place(body.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -24f), new Vector2(f.Width - 40f, 900f));

            Text footer = UiKit.NewLabel(f.Content, "Footer",
                "开源项目 · 欢迎按需自取与修改", UiKit.FontSmall, Palette.TextDim,
                TextAnchor.MiddleCenter);
            UiKit.Place(footer.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 26f), new Vector2(f.Width, 40f));

            return f.Root;
        }

        // ============================================================ 新手引导

        private class TutorialPage
        {
            public string Title;
            public string Body;
            public string Sprite;
        }

        private static readonly TutorialPage[] Pages = new TutorialPage[]
        {
            new TutorialPage
            {
                Title = "一、守住山门",
                Sprite = "ui_core",
                Body = "妖魔会沿着两条山路一路摸到山门，山门耐久归零就算失守。\n\n"
                    + "撑满全部波次就是守山成功。顶部可以看到剩余耐久、当前波次和下一波的阵容。"
            },
            new TutorialPage
            {
                Title = "二、布下阵法",
                Sprite = "tower_spirit_gather",
                Body = "先点下方的阵法卡，再点棋盘上的空地；或者直接把卡拖到棋盘上松手。\n\n"
                    + "聚灵阵产灵气，攻击法阵打伤害，困阵减速，幻阵让妖魔打空，护盾阵替周围阵法挨打。\n\n"
                    + "灵气是所有布置的本钱，起手先铺两三个聚灵阵通常最稳。"
            },
            new TutorialPage
            {
                Title = "三、仙灵与符箓",
                Sprite = "spirit_sword",
                Body = "仙灵和符箓要布置在妖魔走的那条路上。\n\n"
                    + "剑灵近身缠斗，符灵远程投符，丹灵会持续修复山门耐久；它们都有存在时间，会自行消失。\n\n"
                    + "符箓埋下去看不见，妖魔踩到才炸：雷符单体高伤，冰符减速，爆符范围伤害。"
            },
            new TutorialPage
            {
                Title = "四、终极技与节奏",
                Sprite = "skill_thunder",
                Body = "天雷咒、冰封、灵雨三个终极技各有冷却，危急时直接点卡片施放。\n\n"
                    + "每波结束会进入备战期，想抢节奏就点「开始」提前开战；右上角可以暂停和二倍速。\n\n"
                    + "每 5 波是 BOSS 波，魔君带护盾还拆阵，遇到它记得把阵线拉开。"
            }
        };

        public static GameObject Tutorial(GameBootstrap app)
        {
            GameObject root = app.NewOverlay("TutorialOverlay").gameObject;
            RectTransform rt = (RectTransform)root.transform;

            int page = 0;

            Image panel = UiKit.NewPanel(rt, "Panel", Palette.PanelBg, true);
            UiKit.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(920f, 1240f));
            RectTransform prt = panel.rectTransform;

            Image art = UiKit.NewIcon(prt, "Art", Pages[0].Sprite, 240f, Color.white);
            UiKit.Place(art.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -40f), new Vector2(240f, 240f));

            Text title = UiKit.NewLabel(prt, "Title", Pages[0].Title, UiKit.FontH1, Palette.Gold,
                TextAnchor.MiddleCenter);
            UiKit.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -300f), new Vector2(820f, 70f));

            Text body = UiKit.NewLabel(prt, "Body", Pages[0].Body, UiKit.FontBody,
                Palette.TextMain, TextAnchor.UpperCenter);
            UiKit.Place(body.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -380f), new Vector2(800f, 480f));

            // 页码圆点
            Image[] dots = new Image[Pages.Length];
            float dotsW = Pages.Length * 34f;
            for (int i = 0; i < Pages.Length; i++)
            {
                dots[i] = UiKit.NewImage(prt, "Dot_" + i, SpriteLibrary.GetOrFallback("ui_star",
                    Palette.Gold), Palette.TextDim);
                UiKit.Place(dots[i].rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(-dotsW * 0.5f + 17f + i * 34f, 226f), new Vector2(20f, 20f));
            }

            Button next = UiKit.NewButton(prt, "Next", "下一步", new Vector2(440f, 104f),
                UiKit.FontH1, Palette.JadeDark, null);
            UiKit.Place(next.GetComponent<RectTransform>(), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 96f), new Vector2(440f, 104f));
            Text nextLabel = next.transform.Find("Label").GetComponent<Text>();

            Button skip = UiKit.NewButton(prt, "Skip", "跳过", new Vector2(240f, 74f),
                UiKit.FontBody, Palette.ButtonBg, null);
            UiKit.Place(skip.GetComponent<RectTransform>(), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(240f, 74f));

            Action render = null;
            Action finish = delegate
            {
                app.PlayClick();
                SaveSystem.Data.tutorialCompleted = true;
                SaveSystem.Save();
                app.CloseOverlay();
            };

            render = delegate
            {
                TutorialPage p = Pages[page];
                art.sprite = SpriteLibrary.GetOrFallback(p.Sprite, Palette.Jade);
                title.text = p.Title;
                body.text = p.Body;
                for (int i = 0; i < dots.Length; i++)
                {
                    dots[i].color = i == page ? Palette.Gold : Palette.TextDim;
                }
                bool last = page >= Pages.Length - 1;
                nextLabel.text = last ? "开始守山" : "下一步";
                skip.gameObject.SetActive(!last);
            };

            next.onClick.AddListener(delegate
            {
                if (page >= Pages.Length - 1)
                {
                    finish();
                    return;
                }
                app.PlayClick();
                page++;
                render();
            });
            skip.onClick.AddListener(delegate { finish(); });

            render();
            return root;
        }
    }
}
