using System;
using System.Collections.Generic;
using MountainWardBarrier.Core;
using UnityEngine;
using UnityEngine.UI;

namespace MountainWardBarrier.Game
{
    /// <summary>
    /// 对局界面：HUD + 棋盘 + 输入 + 事件表现。
    ///
    /// 它本身不包含任何玩法规则 —— 所有规则都在 Core/BattleSimulation 里。
    /// 这一层的职责只有三件事：把状态画出来、把手指变成指令、把事件变成声音和特效。
    /// </summary>
    public class BattleScreen : MonoBehaviour
    {
        // 卡牌下标布局（三行）
        private const int CardTowerStart = 0;   // 0..4   五种阵法
        private const int CardSpiritStart = 5;  // 5..7   三种仙灵
        private const int CardTrapStart = 8;    // 8..10  三种符箓
        private const int CardSkillStart = 11;  // 11..13 三个终极技能
        private const int CardStartWave = 14;   // 开始 / 跳过备战
        private const int CardPause = 15;
        private const int CardSpeed = 16;
        private const int CardCount = 17;
        private const int CardsPerColumn = 6;

        private GameBootstrap _app;
        private BattleSimulation _sim;
        private BoardView _board;
        private RectTransform _bottomBar;

        private readonly List<CardUi> _cards = new List<CardUi>();

        // HUD
        private Text _spiritText;
        private Text _incomeText;
        private Text _waveText;
        private Text _phaseText;
        private Text _coreText;
        private Image _coreBarFill;
        private Text _previewText;
        private Text _phaseBanner;
        private Image _speedIcon;
        private Image _pauseIcon;
        private GameObject _bossRow;
        private Text _bossName;
        private Image _bossBarFill;
        private RectTransform _actionPanel;
        private Text _actionTitle;
        private Button _upgradeButton;
        private Text _upgradeLabel;
        private Button _sellButton;
        private Text _sellLabel;
        private FloatingToast _toast;

        // 状态
        private enum PlaceMode
        {
            None = 0,
            Tower = 1,
            Spirit = 2,
            Trap = 3
        }

        private PlaceMode _placeMode = PlaceMode.None;
        private TowerKind _placeTower;
        private SpiritKind _placeSpirit;
        private TrapKind _placeTrap;
        private int _selectedTowerId = -1;
        private float _endTimer;
        private bool _reported;

        private readonly Dictionary<string, float> _sfxCooldown = new Dictionary<string, float>();
        private float _damageTextTimer;
        private bool _quitArmed;

        private class CardUi
        {
            public int Index;
            public Image Bg;
            public Image Icon;
            public Image Cooldown;
            public Text Title;
            public Text Cost;
            public Image Selection;
        }

        // ============================================================ 初始化

        public void Setup(GameBootstrap app, Difficulty difficulty, int seed)
        {
            _app = app;
            _sim = new BattleSimulation(app.NewDatabase(), difficulty, seed);
            _sim.Begin();
            UiKit.Stretch((RectTransform)transform);
            BuildUi();
            SyncCards();
            RefreshHud();
            ShowBanner("第 1 波备战中 —— 布好阵就点「开始」", 2.6f);
        }

        private void BuildUi()
        {
            Vector2 canvas = _app.CanvasSize;
            float boardCell = _app.BoardCellSize;
            float boardW = boardCell * _sim.Map.Columns;
            float boardH = boardCell * _sim.Map.Rows;

            // 背景
            Image bg = UiKit.NewImage(transform, "Bg", SpriteLibrary.Get("bg_battle"), Color.white);
            UiKit.Stretch(bg.rectTransform);
            bg.preserveAspect = false;

            // 棋盘区域
            RectTransform boardArea = UiKit.Node("BoardArea", transform);
            boardArea.anchorMin = new Vector2(0.5f, 0.5f);
            boardArea.anchorMax = new Vector2(0.5f, 0.5f);
            boardArea.pivot = new Vector2(0.5f, 0.5f);
            float areaTop = canvas.y * 0.5f - GameBootstrap.TopBarHeight - 14f;
            float areaBottom = -canvas.y * 0.5f + GameBootstrap.BottomBarHeight + 14f;
            boardArea.anchoredPosition = new Vector2(0f, (areaTop + areaBottom) * 0.5f);
            boardArea.sizeDelta = new Vector2(boardW, boardH);

            _board = BoardView.Build(boardArea, _sim, boardW, boardH);
            BoardInputArea inputArea = boardArea.gameObject.AddComponent<BoardInputArea>();
            inputArea.OnTapLocal = OnBoardTapped;

            BuildTopBar(canvas);
            BuildBottomBar(canvas);
            BuildBossRow(canvas);
            BuildPhaseBanner(canvas);
            BuildActionPanel();
            BuildToast();

            _toast = FloatingToast.Create(transform, new Vector2(0f, -GameBootstrap.TopBarHeight - 30f),
                34, Palette.TextWarn);
        }

        /// <summary>
        /// 屏幕中上部的过场大字（"第 3 波 —— 妖魔来袭"之类）。
        /// 它只是一句话，不承载任何交互，所以整块都关掉 raycast。
        /// </summary>
        private void BuildPhaseBanner(Vector2 canvas)
        {
            _phaseBanner = UiKit.NewLabel(transform, "PhaseBanner", "", 58, Palette.Gold,
                TextAnchor.MiddleCenter);
            _phaseBanner.fontStyle = FontStyle.Bold;
            UiKit.Place(_phaseBanner.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -GameBootstrap.TopBarHeight - 150f),
                new Vector2(Mathf.Min(canvas.x - 60f, 900f), 96f));
            _phaseBanner.gameObject.SetActive(false);
        }

        private void BuildTopBar(Vector2 canvas)
        {
            Image bar = UiKit.NewPanel(transform, "TopBar", Palette.PanelBg, true);
            RectTransform rt = bar.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(0f, -GameBootstrap.TopBarHeight);
            rt.offsetMax = Vector2.zero;

            // --- 灵气
            Image spiritIcon = UiKit.NewIcon(rt, "SpiritIcon", "ui_spirit", 68f, Color.white);
            UiKit.Place(spiritIcon.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(28f, -16f), new Vector2(68f, 68f));
            _spiritText = UiKit.NewLabel(rt, "SpiritValue", "0", UiKit.FontH1, Palette.JadeLight,
                TextAnchor.MiddleLeft);
            UiKit.Place(_spiritText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(104f, -12f), new Vector2(300f, 52f));
            _incomeText = UiKit.NewLabel(rt, "Income", "", UiKit.FontSmall, Palette.TextDim,
                TextAnchor.MiddleLeft);
            UiKit.Place(_incomeText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(104f, -60f), new Vector2(320f, 40f));

            // --- 波次
            _waveText = UiKit.NewLabel(rt, "Wave", "", UiKit.FontH2, Palette.TextMain,
                TextAnchor.MiddleCenter);
            UiKit.Place(_waveText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -14f), new Vector2(420f, 52f));
            _phaseText = UiKit.NewLabel(rt, "Phase", "", UiKit.FontBody, Palette.TextWarn,
                TextAnchor.MiddleCenter);
            UiKit.Place(_phaseText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -64f), new Vector2(420f, 46f));

            // --- 核心耐久
            Image coreIcon = UiKit.NewIcon(rt, "CoreIcon", "ui_core", 60f, Color.white);
            UiKit.Place(coreIcon.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(28f, -100f), new Vector2(60f, 60f));
            Image coreBarBg = UiKit.NewImage(rt, "CoreBarBg", SpriteLibrary.Get("ui_bar_bg"),
                new Color(0.08f, 0.1f, 0.14f, 0.9f));
            UiKit.Place(coreBarBg.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(96f, -116f), new Vector2(360f, 28f));
            _coreBarFill = UiKit.NewImage(coreBarBg.transform, "Fill",
                SpriteLibrary.Get("ui_bar_fill"), Palette.Jade);
            RectTransform cf = _coreBarFill.rectTransform;
            cf.anchorMin = new Vector2(0f, 0.5f);
            cf.anchorMax = new Vector2(0f, 0.5f);
            cf.pivot = new Vector2(0f, 0.5f);
            cf.anchoredPosition = Vector2.zero;
            cf.sizeDelta = new Vector2(360f, 28f);
            _coreBarFill.type = Image.Type.Filled;
            _coreBarFill.fillMethod = Image.FillMethod.Horizontal;
            _coreText = UiKit.NewLabel(rt, "CoreValue", "", UiKit.FontSmall, Palette.TextMain,
                TextAnchor.MiddleLeft);
            UiKit.Place(_coreText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(96f, -144f), new Vector2(360f, 40f));

            // --- 下一波预告
            _previewText = UiKit.NewLabel(rt, "Preview", "", UiKit.FontSmall, Palette.TextDim,
                TextAnchor.UpperRight);
            UiKit.Place(_previewText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-28f, -14f), new Vector2(420f, 150f));

            // --- 暂停 / 倍速
            Button pause = UiKit.NewIconButton(rt, "PauseBtn", "ui_pause", 78f, Palette.ButtonBg,
                OnPauseClicked);
            UiKit.Place(pause.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-28f, 18f), new Vector2(78f, 78f));
            _pauseIcon = pause.transform.Find("Icon").GetComponent<Image>();

            Button speed = UiKit.NewIconButton(rt, "SpeedBtn", "ui_speed1", 78f, Palette.ButtonBg,
                OnSpeedClicked);
            UiKit.Place(speed.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-116f, 18f), new Vector2(78f, 78f));
            _speedIcon = speed.transform.Find("Icon").GetComponent<Image>();
        }

        private void BuildBossRow(Vector2 canvas)
        {
            Image panel = UiKit.NewPanel(transform, "BossRow", new Color(0.32f, 0.08f, 0.1f, 0.92f),
                true);
            RectTransform rt = panel.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -GameBootstrap.TopBarHeight - 6f);
            rt.sizeDelta = new Vector2(Mathf.Min(canvas.x - 40f, 720f), 78f);
            _bossRow = panel.gameObject;

            _bossName = UiKit.NewLabel(rt, "Name", "", UiKit.FontBody, Palette.TextMain,
                TextAnchor.MiddleLeft);
            UiKit.Place(_bossName.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(16f, 0f), new Vector2(360f, 60f));

            Image barBg = UiKit.NewImage(rt, "BarBg", SpriteLibrary.Get("ui_bar_bg"),
                new Color(0.06f, 0.07f, 0.1f, 0.95f));
            UiKit.Place(barBg.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-16f, 0f), new Vector2(360f, 26f));
            _bossBarFill = UiKit.NewImage(barBg.transform, "Fill", SpriteLibrary.Get("ui_bar_fill"),
                Palette.Cinnabar);
            RectTransform bf = _bossBarFill.rectTransform;
            bf.anchorMin = new Vector2(0f, 0.5f);
            bf.anchorMax = new Vector2(0f, 0.5f);
            bf.pivot = new Vector2(0f, 0.5f);
            bf.anchoredPosition = Vector2.zero;
            bf.sizeDelta = new Vector2(360f, 26f);
            _bossBarFill.type = Image.Type.Filled;
            _bossBarFill.fillMethod = Image.FillMethod.Horizontal;

            _bossRow.SetActive(false);
        }

        private void BuildBottomBar(Vector2 canvas)
        {
            Image bar = UiKit.NewPanel(transform, "BottomBar", new Color(0.07f, 0.1f, 0.14f, 0.96f),
                true);
            _bottomBar = bar.rectTransform;
            _bottomBar.anchorMin = new Vector2(0f, 0f);
            _bottomBar.anchorMax = new Vector2(1f, 0f);
            _bottomBar.pivot = new Vector2(0.5f, 0f);
            _bottomBar.offsetMin = Vector2.zero;
            _bottomBar.offsetMax = new Vector2(0f, GameBootstrap.BottomBarHeight);

            float width = canvas.x;
            const float sidePad = 14f;
            const float spacing = 8f;
            const float rowHeight = 152f;
            const float rowBottom0 = 10f;

            // 行的排列顺序（自下而上）：技能/控制 → 仙灵+符箓 → 阵法
            BuildRow(width, sidePad, spacing, rowHeight, rowBottom0, CardSkillStart, 6);
            BuildRow(width, sidePad, spacing, rowHeight, rowBottom0 + rowHeight + spacing,
                CardSpiritStart, 6);
            BuildRow(width, sidePad, spacing, rowHeight, rowBottom0 + (rowHeight + spacing) * 2f,
                CardTowerStart, 5);
        }

        private void BuildRow(float canvasWidth, float sidePad, float spacing, float rowHeight,
            float bottom, int firstIndex, int count)
        {
            float usable = canvasWidth - sidePad * 2f - spacing * (count - 1);
            float cardW = usable / count;
            for (int i = 0; i < count; i++)
            {
                int index = firstIndex + i;
                float x = sidePad + i * (cardW + spacing);
                RectTransform card = UiKit.Node("Card_" + index, _bottomBar);
                card.anchorMin = new Vector2(0f, 0f);
                card.anchorMax = new Vector2(0f, 0f);
                card.pivot = new Vector2(0f, 0f);
                card.anchoredPosition = new Vector2(x, bottom);
                card.sizeDelta = new Vector2(cardW, rowHeight);
                _cards.Add(BuildCardVisual(card, index, cardW, rowHeight));
            }
        }

        private CardUi BuildCardVisual(RectTransform parent, int index, float w, float h)
        {
            CardUi ui = new CardUi();
            ui.Index = index;

            Image bg = UiKit.NewPanel(parent, "Bg", Palette.ButtonBg, true);
            UiKit.Stretch(bg.rectTransform);
            bg.raycastTarget = true;
            ui.Bg = bg;

            ui.Selection = UiKit.NewImage(parent, "Selection", SpriteLibrary.Get("fx_ring"),
                new Color(0.6f, 1f, 0.85f, 0.55f));
            UiKit.Stretch(ui.Selection.rectTransform);
            ui.Selection.gameObject.SetActive(false);

            ui.Icon = UiKit.NewImage(parent, "Icon", null, Color.white);
            UiKit.Place(ui.Icon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -10f), new Vector2(h * 0.52f, h * 0.52f));
            ui.Icon.preserveAspect = true;

            ui.Title = UiKit.NewLabel(parent, "Title", "", UiKit.FontSmall, Palette.TextMain,
                TextAnchor.MiddleCenter);
            UiKit.Place(ui.Title.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, h * 0.28f), new Vector2(w, 34f));

            ui.Cost = UiKit.NewLabel(parent, "Cost", "", UiKit.FontSmall, Palette.JadeLight,
                TextAnchor.MiddleCenter);
            UiKit.Place(ui.Cost.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 4f), new Vector2(w, 34f));

            ui.Cooldown = UiKit.NewImage(parent, "Cooldown", SpriteLibrary.Get("ui_bar_bg"),
                new Color(0.02f, 0.03f, 0.05f, 0.72f));
            UiKit.Stretch(ui.Cooldown.rectTransform);
            ui.Cooldown.type = Image.Type.Filled;
            ui.Cooldown.fillMethod = Image.FillMethod.Vertical;
            ui.Cooldown.fillOrigin = 1;
            ui.Cooldown.fillAmount = 0f;
            ui.Cooldown.gameObject.SetActive(false);

            CardButton button = parent.gameObject.AddComponent<CardButton>();
            button.Index = index;
            button.OnTap = OnCardTapped;
            button.OnDragMoving = OnCardDrag;
            button.OnDragRelease = OnCardDragRelease;
            return ui;
        }

        private void BuildActionPanel()
        {
            Image panel = UiKit.NewPanel(transform, "ActionPanel", new Color(0.1f, 0.14f, 0.2f, 0.97f),
                true);
            _actionPanel = panel.rectTransform;
            _actionPanel.anchorMin = new Vector2(0.5f, 0.5f);
            _actionPanel.anchorMax = new Vector2(0.5f, 0.5f);
            _actionPanel.pivot = new Vector2(0.5f, 0f);
            _actionPanel.sizeDelta = new Vector2(360f, 200f);
            panel.gameObject.SetActive(false);

            _actionTitle = UiKit.NewLabel(_actionPanel, "Title", "", UiKit.FontBody,
                Palette.TextMain, TextAnchor.MiddleCenter);
            UiKit.Place(_actionTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -8f), new Vector2(340f, 46f));

            _upgradeButton = UiKit.NewButton(_actionPanel, "Upgrade", "升级", new Vector2(160f, 66f),
                UiKit.FontBody, Palette.JadeDark, OnUpgradeClicked);
            UiKit.Place(_upgradeButton.GetComponent<RectTransform>(), new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(14f, 48f), new Vector2(160f, 66f));
            _upgradeLabel = _upgradeButton.transform.Find("Label").GetComponent<Text>();
            Image upIcon = UiKit.NewIcon(_upgradeButton.transform, "Icon", "ui_upgrade", 34f,
                Palette.TextMain);
            UiKit.Place(upIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(14f, 0f), new Vector2(34f, 34f));

            _sellButton = UiKit.NewButton(_actionPanel, "Sell", "铲除", new Vector2(160f, 66f),
                UiKit.FontBody, Palette.CinnabarDark, OnSellClicked);
            UiKit.Place(_sellButton.GetComponent<RectTransform>(), new Vector2(1f, 0f),
                new Vector2(1f, 0f), new Vector2(-14f, 48f), new Vector2(160f, 66f));
            _sellLabel = _sellButton.transform.Find("Label").GetComponent<Text>();
            Image sellIcon = UiKit.NewIcon(_sellButton.transform, "Icon", "ui_sell", 34f,
                Palette.TextMain);
            UiKit.Place(sellIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(14f, 0f), new Vector2(34f, 34f));

            Button close = UiKit.NewIconButton(_actionPanel, "Close", "ui_close", 52f,
                Palette.ButtonBg, CloseActionPanel);
            UiKit.Place(close.GetComponent<RectTransform>(), new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(-6f, -6f), new Vector2(52f, 52f));
        }

        private void BuildToast()
        {
            // 提示条在 BuildUi 末尾创建（需要 transform 已就绪），这里留空占位。
        }

        // ============================================================ 输入

        private void OnCardTapped(int index)
        {
            if (_sim == null || _sim.Finished)
            {
                return;
            }
            PlayClick();

            if (index >= CardTowerStart && index < CardTowerStart + 5)
            {
                TogglePlaceMode(PlaceMode.Tower, (TowerKind)(index - CardTowerStart));
                return;
            }
            if (index >= CardSpiritStart && index < CardSpiritStart + 3)
            {
                TogglePlaceMode(PlaceMode.Spirit, (SpiritKind)(index - CardSpiritStart));
                return;
            }
            if (index >= CardTrapStart && index < CardTrapStart + 3)
            {
                TogglePlaceMode(PlaceMode.Trap, (TrapKind)(index - CardTrapStart));
                return;
            }
            if (index >= CardSkillStart && index < CardSkillStart + 3)
            {
                SkillKind kind = (SkillKind)(index - CardSkillStart);
                ClearPlaceMode();
                ActionResult r = _sim.CastSkill(kind);
                if (!r.ok)
                {
                    Toast(r.Message(), Palette.TextBad);
                    PlaySfx("sfx_deny");
                }
                return;
            }
            if (index == CardStartWave)
            {
                if (_sim.Phase == BattlePhase.Preparing)
                {
                    _sim.LaunchWave();
                }
                return;
            }
            if (index == CardPause)
            {
                OnPauseClicked();
                return;
            }
            if (index == CardSpeed)
            {
                OnSpeedClicked();
            }
        }

        /// <summary>
        /// 切换/取消当前的布置目标。
        ///
        /// 三种"要摆下去的东西"各写一个重载，而不是收一个 int 再转回来 ——
        /// 枚举和整数在 C# 里不会自动互转，收了 int 反而要在每个调用点写 (int) 强制转换，
        /// 那样既啰嗦又容易把阵法序号和仙灵序号弄混。
        /// </summary>
        private void TogglePlaceMode(PlaceMode mode, TowerKind kind)
        {
            if (mode == PlaceMode.Tower && _placeMode == PlaceMode.Tower && _placeTower == kind)
            {
                ClearPlaceMode();
                return;
            }
            EnterPlaceMode(mode);
            _placeTower = kind;
        }

        private void TogglePlaceMode(PlaceMode mode, SpiritKind kind)
        {
            if (mode == PlaceMode.Spirit && _placeMode == PlaceMode.Spirit && _placeSpirit == kind)
            {
                ClearPlaceMode();
                return;
            }
            EnterPlaceMode(mode);
            _placeSpirit = kind;
        }

        private void TogglePlaceMode(PlaceMode mode, TrapKind kind)
        {
            if (mode == PlaceMode.Trap && _placeMode == PlaceMode.Trap && _placeTrap == kind)
            {
                ClearPlaceMode();
                return;
            }
            EnterPlaceMode(mode);
            _placeTrap = kind;
        }

        private void EnterPlaceMode(PlaceMode mode)
        {
            _selectedTowerId = -1;
            HideActionPanel();
            _placeMode = mode;
            SyncCards();
        }

        private void ClearPlaceMode()
        {
            _placeMode = PlaceMode.None;
            _board.HideGhost();
            SyncCards();
        }

        private void OnCardDrag(int index, Vector2 screenPos, bool dragging)
        {
            if (!dragging || _placeMode == PlaceMode.None)
            {
                return;
            }
            GridPos cell;
            bool inside = ScreenToCell(screenPos, out cell);
            if (!inside)
            {
                _board.HideGhost();
                return;
            }
            UpdateGhost(cell);
        }

        private void OnCardDragRelease(int index, Vector2 screenPos, bool dragging)
        {
            if (!dragging)
            {
                return;
            }
            GridPos cell;
            if (!ScreenToCell(screenPos, out cell))
            {
                _board.HideGhost();
                return;
            }
            if (TryPlaceAt(cell))
            {
                ClearPlaceMode();
            }
            else
            {
                _board.HideGhost();
                SyncCards();
            }
        }

        private bool ScreenToCell(Vector2 screenPos, out GridPos cell)
        {
            cell = new GridPos(0, 0);
            if (_board == null)
            {
                return false;
            }
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _board.Root, screenPos, null, out local))
            {
                return false;
            }
            return _board.LocalToCell(local, out cell);
        }

        private void OnBoardTapped(Vector2 local)
        {
            if (_sim == null || _sim.Finished)
            {
                return;
            }
            GridPos cell;
            if (!_board.LocalToCell(local, out cell))
            {
                return;
            }

            if (_placeMode != PlaceMode.None)
            {
                if (TryPlaceAt(cell))
                {
                    if (SaveSystem.Data != null)
                    {
                        // 连续布置更顺手：保留当前选中，方便在附近接着放
                    }
                }
                else
                {
                    SyncCards();
                }
                return;
            }

            TowerUnit tower = _sim.GetTowerAt(cell);
            if (tower != null)
            {
                _selectedTowerId = tower.id;
                ShowActionPanel(tower);
                return;
            }
            _selectedTowerId = -1;
            HideActionPanel();
        }

        private void UpdateGhost(GridPos cell)
        {
            string sprite = null;
            bool valid = false;
            bool onPath = _sim.Map.IsPath(cell.x, cell.y) && !_sim.Map.IsCore(cell.x, cell.y);

            if (_placeMode == PlaceMode.Tower)
            {
                TowerConfig cfg = _sim.Db.GetTower(_placeTower);
                sprite = cfg != null ? cfg.sprite : null;
                valid = _sim.Map.IsBuildable(cell.x, cell.y) && !_sim.IsCellOccupied(cell)
                    && _sim.Spirit >= (cfg != null ? cfg.cost : 0);
            }
            else if (_placeMode == PlaceMode.Spirit)
            {
                SpiritConfig cfg = _sim.Db.GetSpirit(_placeSpirit);
                sprite = cfg != null ? cfg.sprite : null;
                valid = onPath && !_sim.IsCellOccupied(cell) && _sim.Spirit >= (cfg != null ? cfg.cost : 0);
            }
            else if (_placeMode == PlaceMode.Trap)
            {
                TrapConfig cfg = _sim.Db.GetTrap(_placeTrap);
                sprite = cfg != null ? cfg.sprite : null;
                valid = onPath && !_sim.IsCellOccupied(cell) && _sim.Spirit >= (cfg != null ? cfg.cost : 0);
            }
            _board.ShowGhost(cell, sprite, valid, 0f);
        }

        private bool TryPlaceAt(GridPos cell)
        {
            ActionResult result;
            if (_placeMode == PlaceMode.Tower)
            {
                result = _sim.TryBuildTower(cell, _placeTower);
            }
            else if (_placeMode == PlaceMode.Spirit)
            {
                result = _sim.TrySummonSpirit(cell, _placeSpirit);
            }
            else if (_placeMode == PlaceMode.Trap)
            {
                result = _sim.TryPlaceTrap(cell, _placeTrap);
            }
            else
            {
                return false;
            }

            if (!result.ok)
            {
                Toast(result.Message(), Palette.TextBad);
                PlaySfx("sfx_deny", 0.6f);
                return false;
            }
            _board.HideGhost();
            RefreshHud();
            return true;
        }

        private void OnUpgradeClicked()
        {
            if (_selectedTowerId < 0)
            {
                return;
            }
            ActionResult r = _sim.TryUpgradeTower(_selectedTowerId);
            if (!r.ok)
            {
                Toast(r.Message(), Palette.TextBad);
                PlaySfx("sfx_deny", 0.6f);
                return;
            }
            TowerUnit tower = FindTower(_selectedTowerId);
            if (tower != null)
            {
                ShowActionPanel(tower);
            }
            RefreshHud();
        }

        private void OnSellClicked()
        {
            if (_selectedTowerId < 0)
            {
                return;
            }
            ActionResult r = _sim.TrySellTower(_selectedTowerId);
            if (!r.ok)
            {
                Toast(r.Message(), Palette.TextBad);
                return;
            }
            HideActionPanel();
            _selectedTowerId = -1;
            RefreshHud();
        }

        private TowerUnit FindTower(int id)
        {
            for (int i = 0; i < _sim.Towers.Count; i++)
            {
                if (_sim.Towers[i].id == id)
                {
                    return _sim.Towers[i];
                }
            }
            return null;
        }

        private void ShowActionPanel(TowerUnit tower)
        {
            if (_actionPanel == null || tower == null)
            {
                return;
            }
            _actionPanel.gameObject.SetActive(true);
            _actionTitle.text = tower.config.displayName + "  Lv." + tower.level;

            int upgradeCost = tower.NextUpgradeCost;
            if (upgradeCost < 0)
            {
                _upgradeLabel.text = "已满级";
                _upgradeButton.interactable = false;
            }
            else
            {
                _upgradeLabel.text = "升级 " + upgradeCost;
                _upgradeButton.interactable = _sim.Spirit >= upgradeCost;
            }
            _sellLabel.text = "铲除 +" + tower.RefundValue;

            // 把面板摆到阵法旁边
            Vector2 local = _board.CellToLocal(tower.cell);
            Vector2 boardPos = _board.Root.anchoredPosition;
            float halfH = _board.Root.sizeDelta.y * 0.5f;
            float topY = boardPos.y + halfH;
            float x = Mathf.Clamp(boardPos.x + local.x, -_app.CanvasSize.x * 0.5f + 200f,
                _app.CanvasSize.x * 0.5f - 200f);
            float y = Mathf.Clamp(boardPos.y + local.y + _board.CellSize * 0.6f,
                -_app.CanvasSize.y * 0.5f + GameBootstrap.BottomBarHeight + 20f,
                topY - 210f);
            _actionPanel.anchoredPosition = new Vector2(x, y);
        }

        private void CloseActionPanel()
        {
            _selectedTowerId = -1;
            HideActionPanel();
        }

        private void HideActionPanel()
        {
            if (_actionPanel != null)
            {
                _actionPanel.gameObject.SetActive(false);
            }
        }

        private void OnPauseClicked()
        {
            PlayClick();
            if (_sim == null || _sim.Finished)
            {
                return;
            }
            _sim.Paused = !_sim.Paused;
            if (_pauseIcon != null)
            {
                _pauseIcon.sprite = SpriteLibrary.GetOrFallback(
                    _sim.Paused ? "ui_play" : "ui_pause", Palette.TextMain);
            }
            if (_sim.Paused && _app != null)
            {
                _app.ShowOverlay(BuildPauseOverlay());
            }
            else if (_app != null)
            {
                _app.CloseOverlay();
            }
        }

        private void OnSpeedClicked()
        {
            PlayClick();
            if (_sim == null)
            {
                return;
            }
            _sim.GameSpeed = _sim.GameSpeed >= 1.99f ? 1f : 2f;
            if (_speedIcon != null)
            {
                _speedIcon.sprite = SpriteLibrary.GetOrFallback(
                    _sim.GameSpeed > 1.5f ? "ui_speed2" : "ui_speed1", Palette.TextMain);
            }
        }

        // ============================================================ 主循环

        private void Update()
        {
            if (_sim == null || _app == null)
            {
                return;
            }
            HandleBackKey();

            float dt = Time.unscaledDeltaTime;
            if (!_sim.Paused && !_sim.Finished)
            {
                _sim.Tick(dt);
            }
            ConsumeEvents();

            float visualDt = _sim.Paused ? 0f : dt;
            _board.Sync(visualDt, SaveSystem.Data.damageNumbers);
            _board.SetCoreAlarm(_sim.CoreHpRatio);

            RefreshHud();
            UpdateEndFlow(dt);
        }

        private void HandleBackKey()
        {
            if (!Input.GetKeyDown(KeyCode.Escape))
            {
                return;
            }
            if (_app.HasOverlay)
            {
                _app.CloseOverlay();
                _sim.Paused = false;
                if (_pauseIcon != null)
                {
                    _pauseIcon.sprite = SpriteLibrary.GetOrFallback("ui_pause", Palette.TextMain);
                }
                return;
            }
            if (_placeMode != PlaceMode.None)
            {
                ClearPlaceMode();
                return;
            }
            if (_actionPanel != null && _actionPanel.gameObject.activeSelf)
            {
                CloseActionPanel();
                return;
            }
            if (_quitArmed)
            {
                _app.ShowMenu();
                return;
            }
            _quitArmed = true;
            Toast("再按一次返回主界面", Palette.TextWarn);
        }

        private void UpdateEndFlow(float dt)
        {
            if (!_sim.Finished || _reported)
            {
                return;
            }
            _endTimer += dt;
            if (_endTimer < 1.5f)
            {
                return;
            }
            _reported = true;

            BattleReport report = BattleReport.FromSimulation(_sim);

            // 先和旧纪录比一比，再并入存档 —— 并入之后旧纪录就看不到了。
            int prevBestWave = SaveSystem.Data.BestWave(report.difficulty);
            float prevBestTime = SaveSystem.Data.BestTime(report.difficulty);
            report.newRecord = report.waveReached > prevBestWave
                || (report.victory && (prevBestTime <= 0f || report.duration < prevBestTime));

            List<int> fresh = SaveSystem.CommitReport(report);
            SaveSystem.FlushTelemetry(_sim.Telemetry, report);
            _app.ShowResult(report, fresh);
        }

        // ============================================================ 事件表现

        private void ConsumeEvents()
        {
            List<SimEvent> events = _sim.Events;
            if (events.Count == 0)
            {
                return;
            }
            for (int i = 0; i < events.Count; i++)
            {
                HandleEvent(events[i]);
            }
            _sim.ClearEvents();
        }

        private void HandleEvent(SimEvent e)
        {
            // 事件里的坐标是"格子空间"的连续坐标（Float2），棋盘视图负责换算成 uGUI 的本地坐标。
            Float2 world = new Float2(e.x, e.y);
            switch (e.type)
            {
                case SimEventType.EnemySpawned:
                    {
                        if (e.amount >= (float)EnemyKind.Lieutenant)
                        {
                            _board.SpawnFx("fx_ring", _board.WorldToLocal(world), 30f, 90f, 0.4f,
                                Palette.Purple, false);
                            PlaySfx("sfx_enemy_spawn", 0.5f, 0.1f);
                        }
                        break;
                    }
                case SimEventType.EnemyDamaged:
                    if (e.amount >= 25f && _damageTextTimer <= 0f)
                    {
                        _damageTextTimer = 0.05f;
                        _board.SpawnDamageText(_board.WorldToLocal(world), e.amount, Palette.GoldLight);
                    }
                    PlaySfx("sfx_hit", 0.35f, 0.16f);
                    break;
                case SimEventType.EnemyDied:
                    _board.SpawnFx("fx_impact", _board.WorldToLocal(world), 40f, 110f, 0.32f,
                        Palette.TextMain, true);
                    PlaySfx("sfx_enemy_die", 0.7f, 0.1f);
                    if (e.amount > 0f)
                    {
                        _board.SpawnDamageText(_board.WorldToLocal(world), e.amount, Palette.Jade);
                    }
                    break;
                case SimEventType.EnemySplit:
                    _board.SpawnFx("fx_ring", _board.WorldToLocal(world), 30f, 120f, 0.35f,
                        Palette.Demon, false);
                    PlaySfx("sfx_split", 0.6f, 0.08f);
                    break;
                case SimEventType.EnemyReachedCore:
                case SimEventType.CoreDamaged:
                    _board.Shake(16f);
                    _board.SpawnFx("fx_impact", _board.WorldToLocal(world), 60f, 180f, 0.42f,
                        Palette.Cinnabar, true);
                    PlaySfx("sfx_core_hit", 0.9f, 0.05f);
                    break;
                case SimEventType.TowerBuilt:
                    _board.SpawnFx("fx_ring", _board.WorldToLocal(world), 30f, 130f, 0.4f,
                        Palette.Jade, false);
                    PlaySfx("sfx_place", 0.9f, 0.06f);
                    break;
                case SimEventType.TowerUpgraded:
                    _board.SpawnFx("fx_ring", _board.WorldToLocal(world), 40f, 170f, 0.5f,
                        Palette.Gold, true);
                    PlaySfx("sfx_upgrade", 0.9f, 0.05f);
                    break;
                case SimEventType.TowerSold:
                    _board.SpawnFx("fx_slash", _board.WorldToLocal(world), 60f, 120f, 0.3f,
                        Palette.TextDim, true);
                    PlaySfx("sfx_sell", 0.8f, 0.05f);
                    break;
                case SimEventType.TowerDestroyed:
                    _board.SpawnFx("fx_impact", _board.WorldToLocal(world), 50f, 150f, 0.45f,
                        Palette.Cinnabar, true);
                    _board.Shake(9f);
                    Toast("阵法被妖魔摧毁了！", Palette.TextBad);
                    PlaySfx("sfx_core_hit", 0.5f, 0.1f);
                    break;
                case SimEventType.TowerDamaged:
                    if (e.amount <= 0f)
                    {
                        _board.SpawnDamageText(_board.WorldToLocal(world), 0f, Palette.Ice);
                    }
                    break;
                case SimEventType.SpiritSummoned:
                    _board.SpawnFx("fx_shield", _board.WorldToLocal(world), 50f, 150f, 0.5f,
                        Palette.JadeLight, true);
                    PlaySfx("sfx_summon", 0.85f, 0.05f);
                    break;
                case SimEventType.SpiritDied:
                case SimEventType.SpiritExpired:
                    _board.SpawnFx("fx_impact", _board.WorldToLocal(world), 36f, 90f, 0.3f,
                        Palette.JadeLight, false);
                    break;
                case SimEventType.EnemyAttackDodged:
                    _board.SpawnDamageText(_board.WorldToLocal(world), 0f, Palette.Ice);
                    break;
                case SimEventType.TrapPlaced:
                    PlaySfx("sfx_click", 0.6f, 0.08f);
                    break;
                case SimEventType.TrapTriggered:
                    {
                        float kind = e.amount;
                        string fx = kind < 0.5f ? "fx_bolt" : (kind < 1.5f ? "fx_frost" : "fx_impact");
                        Color tint = kind < 0.5f ? Palette.GoldLight
                            : (kind < 1.5f ? Palette.Ice : Palette.Cinnabar);
                        _board.SpawnFx(fx, _board.WorldToLocal(world), 60f, 220f, 0.45f, tint, true);
                        PlaySfx("sfx_trap", 0.9f, 0.05f);
                        _board.Shake(8f);
                        break;
                    }
                case SimEventType.ProjectileFired:
                    PlaySfx("sfx_shoot", 0.30f, 0.2f);
                    break;
                case SimEventType.ProjectileHit:
                    _board.SpawnFx("fx_bolt", _board.WorldToLocal(world), 22f, 54f, 0.22f,
                        Palette.GoldLight, false);
                    break;
                case SimEventType.SkillCast:
                    {
                        SkillKind kind = (SkillKind)Mathf.RoundToInt(e.amount);
                        if (kind == SkillKind.HeavenlyThunder)
                        {
                            _board.Shake(22f);
                            PlaySfx("sfx_thunder", 1f, 0f);
                            for (int k = 0; k < 8; k++)
                            {
                                Vector2 p = new Vector2(
                                    UnityEngine.Random.Range(-_board.Root.sizeDelta.x * 0.45f,
                                        _board.Root.sizeDelta.x * 0.45f),
                                    UnityEngine.Random.Range(-_board.Root.sizeDelta.y * 0.45f,
                                        _board.Root.sizeDelta.y * 0.45f));
                                _board.SpawnFx("fx_bolt", p, 40f, 200f, 0.4f, Palette.GoldLight, false);
                            }
                        }
                        else if (kind == SkillKind.FrostSeal)
                        {
                            PlaySfx("sfx_freeze", 1f, 0f);
                            _board.SpawnFx("fx_ring", Vector2.zero,
                                _board.Root.sizeDelta.y * 0.4f, _board.Root.sizeDelta.y * 1.9f,
                                0.7f, Palette.Ice, true);
                        }
                        else
                        {
                            PlaySfx("sfx_summon", 1f, 0f);
                            _board.SpawnFx("fx_shield", Vector2.zero,
                                _board.Root.sizeDelta.y * 0.3f, _board.Root.sizeDelta.y * 1.4f,
                                0.8f, Palette.JadeLight, true);
                        }
                        break;
                    }
                case SimEventType.SpiritGained:
                    if (e.amount >= 1f)
                    {
                        _board.SpawnDamageText(_board.WorldToLocal(world), e.amount, Palette.Jade);
                    }
                    break;
                case SimEventType.WaveStarted:
                    // 注意：该事件走 Make(type, int a, int b) 重载，数量在 b 上，amount 恒为 0。
                    ShowBanner("第 " + e.a + " 波 —— 妖魔来袭！ ×" + e.b, 2.2f);
                    PlaySfx("sfx_wave_start", 0.95f, 0f);
                    break;
                case SimEventType.WaveCleared:
                    // 同上：清波奖励在 b 上。
                    ShowBanner(e.b > 0 ? "本波已清 · 灵气 +" + e.b : "本波已清", 2.0f);
                    PlaySfx("sfx_unlock", 0.6f, 0.05f);
                    break;
                case SimEventType.PrepPhaseStarted:
                    ShowBanner("备战中 · 点「开始」立即开战", 2.0f);
                    break;
                case SimEventType.Victory:
                    ShowBanner("守山成功！", 3f);
                    break;
                case SimEventType.Defeat:
                    ShowBanner("山门已破……", 3f);
                    break;
            }
        }

        private void PlaySfx(string name)
        {
            PlaySfx(name, 1f, 0.05f);
        }

        private void PlaySfx(string name, float volume)
        {
            PlaySfx(name, volume, 0.05f);
        }

        /// <summary>同一音效在极短时间内被触发太多次会很吵，这里做个节流。</summary>
        private void PlaySfx(string name, float volume, float jitter)
        {
            if (_app == null || _app.Audio == null)
            {
                return;
            }
            float now = Time.unscaledTime;
            float next;
            if (_sfxCooldown.TryGetValue(name, out next) && now < next)
            {
                return;
            }
            float minGap = 0.04f;
            if (name == "sfx_hit")
            {
                minGap = 0.07f;
            }
            else if (name == "sfx_shoot")
            {
                minGap = 0.08f;
            }
            else if (name == "sfx_core_hit" || name == "sfx_enemy_die")
            {
                minGap = 0.09f;
            }
            _sfxCooldown[name] = now + minGap;
            _app.Audio.PlaySfx(name, volume, jitter);
        }

        private void PlayClick()
        {
            if (_app != null)
            {
                _app.PlayClick();
            }
        }

        private void Toast(string text, Color color)
        {
            if (_toast != null)
            {
                _toast.Show(text, 1.4f);
            }
        }

        private void ShowBanner(string text, float duration)
        {
            if (_phaseBanner == null)
            {
                return;
            }
            _phaseBanner.text = text;
            _phaseBanner.gameObject.SetActive(true);
            _bannerLife = duration;
            _bannerDuration = duration;
        }

        private float _bannerLife;
        private float _bannerDuration;

        // ============================================================ HUD 刷新

        private void RefreshHud()
        {
            if (_sim == null)
            {
                return;
            }
            _damageTextTimer -= Time.unscaledDeltaTime;
            if (_bannerLife > 0f)
            {
                _bannerLife -= Time.unscaledDeltaTime;
                float t = _bannerDuration > 0f ? 1f - Mathf.Clamp01(_bannerLife / _bannerDuration) : 1f;
                Color c = Palette.TextMain;
                c.a = Mathf.Clamp01(1.6f - t * 2.2f);
                if (_phaseBanner != null)
                {
                    _phaseBanner.color = c;
                    if (_bannerLife <= 0f)
                    {
                        _phaseBanner.gameObject.SetActive(false);
                    }
                }
            }

            if (_spiritText != null)
            {
                _spiritText.text = Mathf.FloorToInt(_sim.Spirit).ToString();
            }
            if (_incomeText != null)
            {
                _incomeText.text = "+" + _sim.SpiritIncomePerSecond.ToString("F1") + " /秒";
            }
            if (_waveText != null)
            {
                _waveText.text = "第 " + _sim.CurrentWaveNumber + " / " + _sim.TotalWaves + " 波";
            }
            if (_phaseText != null)
            {
                _phaseText.text = PhaseLabel();
            }
            if (_coreText != null)
            {
                _coreText.text = Mathf.CeilToInt(_sim.CoreHp) + " / " + Mathf.RoundToInt(_sim.CoreMaxHp);
            }
            if (_coreBarFill != null)
            {
                _coreBarFill.fillAmount = _sim.CoreHpRatio;
                _coreBarFill.color = _sim.CoreHpRatio > 0.5f ? Palette.Jade
                    : (_sim.CoreHpRatio > 0.25f ? Palette.Gold : Palette.Danger);
            }
            if (_previewText != null)
            {
                _previewText.text = PreviewLabel();
            }

            UpdateBossRow();
            UpdateActionPanelState();
            SyncCards();
        }

        private string PhaseLabel()
        {
            if (_sim.Finished)
            {
                return _sim.Victory ? "守山成功" : "山门失守";
            }
            if (_sim.Phase == BattlePhase.Preparing)
            {
                return "备战 " + Mathf.CeilToInt(_sim.PrepTimer) + " 秒";
            }
            return "交战中 · 剩余 " + _sim.WaveEnemiesRemaining + " 只";
        }

        private string PreviewLabel()
        {
            WaveConfig next = _sim.Phase == BattlePhase.Preparing ? _sim.UpcomingWave : null;
            if (next == null)
            {
                if (_sim.Phase == BattlePhase.Fighting)
                {
                    return "本波剩余 " + _sim.WaveEnemiesRemaining + " 只";
                }
                return "";
            }
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("下一波：");
            for (int i = 0; i < next.groups.Count; i++)
            {
                WaveGroup g = next.groups[i];
                EnemyConfig ec = _sim.Db.GetEnemy(g.kind);
                if (i > 0)
                {
                    sb.Append(" + ");
                }
                sb.Append(ec != null ? ec.displayName : g.kind.ToString());
                sb.Append(" ×");
                sb.Append(g.count);
            }
            sb.Append("\n共 ");
            sb.Append(next.TotalEnemies);
            sb.Append(" 只 · 血量 ×");
            sb.Append(next.hpMultiplier.ToString("F2"));
            return sb.ToString();
        }

        private void UpdateBossRow()
        {
            EnemyUnit boss = null;
            for (int i = 0; i < _sim.Enemies.Count; i++)
            {
                EnemyUnit e = _sim.Enemies[i];
                if (e.alive && e.kind == EnemyKind.Boss)
                {
                    if (boss == null || e.hp > boss.hp)
                    {
                        boss = e;
                    }
                }
            }
            if (_bossRow == null)
            {
                return;
            }
            if (boss == null)
            {
                if (_bossRow.activeSelf)
                {
                    _bossRow.SetActive(false);
                }
                return;
            }
            if (!_bossRow.activeSelf)
            {
                _bossRow.SetActive(true);
            }
            if (_bossName != null)
            {
                _bossName.text = boss.config.displayName + "　" + Mathf.CeilToInt(boss.hp);
            }
            if (_bossBarFill != null)
            {
                _bossBarFill.fillAmount = boss.HpRatio;
            }
        }

        private void UpdateActionPanelState()
        {
            if (_actionPanel == null || !_actionPanel.gameObject.activeSelf || _selectedTowerId < 0)
            {
                return;
            }
            TowerUnit tower = FindTower(_selectedTowerId);
            if (tower == null)
            {
                HideActionPanel();
                return;
            }
            int cost = tower.NextUpgradeCost;
            _upgradeButton.interactable = cost >= 0 && _sim.Spirit >= cost;
        }

        private void SyncCards()
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                CardUi ui = _cards[i];
                string title;
                string costText;
                string sprite;
                int cost = 0;
                bool selected = false;
                float cooldown = 0f;
                bool available = true;

                if (ui.Index < CardSpiritStart)
                {
                    TowerKind kind = (TowerKind)(ui.Index - CardTowerStart);
                    TowerConfig cfg = _sim.Db.GetTower(kind);
                    title = cfg != null ? cfg.displayName : "?";
                    sprite = cfg != null ? cfg.sprite : null;
                    cost = cfg != null ? cfg.cost : 0;
                    selected = _placeMode == PlaceMode.Tower && _placeTower == kind;
                    costText = cost.ToString();
                }
                else if (ui.Index < CardTrapStart)
                {
                    SpiritKind kind = (SpiritKind)(ui.Index - CardSpiritStart);
                    SpiritConfig cfg = _sim.Db.GetSpirit(kind);
                    title = cfg != null ? cfg.displayName : "?";
                    sprite = cfg != null ? cfg.sprite : null;
                    cost = cfg != null ? cfg.cost : 0;
                    selected = _placeMode == PlaceMode.Spirit && _placeSpirit == kind;
                    costText = cost.ToString();
                }
                else if (ui.Index < CardSkillStart)
                {
                    TrapKind kind = (TrapKind)(ui.Index - CardTrapStart);
                    TrapConfig cfg = _sim.Db.GetTrap(kind);
                    title = cfg != null ? cfg.displayName : "?";
                    sprite = cfg != null ? cfg.sprite : null;
                    cost = cfg != null ? cfg.cost : 0;
                    selected = _placeMode == PlaceMode.Trap && _placeTrap == kind;
                    costText = cost.ToString();
                }
                else if (ui.Index < CardStartWave)
                {
                    SkillKind kind = (SkillKind)(ui.Index - CardSkillStart);
                    SkillConfig cfg = _sim.Db.GetSkill(kind);
                    title = cfg != null ? cfg.displayName : "?";
                    sprite = cfg != null ? cfg.sprite : null;
                    cost = cfg != null ? cfg.cost : 0;
                    cooldown = _sim.GetSkillCooldownRatio(kind);
                    available = cooldown <= 0.001f;
                    costText = available ? cost.ToString()
                        : Mathf.CeilToInt(_sim.GetSkillCooldown(kind)) + "s";
                }
                else if (ui.Index == CardStartWave)
                {
                    bool prep = _sim.Phase == BattlePhase.Preparing;
                    title = prep ? "开始" : "进行中";
                    sprite = prep ? "ui_play" : "ui_wave";
                    costText = prep ? "跳过备战" : "剩余 " + _sim.WaveEnemiesRemaining;
                    available = prep;
                    ui.Cooldown.gameObject.SetActive(false);
                }
                else if (ui.Index == CardPause)
                {
                    title = _sim.Paused ? "继续" : "暂停";
                    sprite = _sim.Paused ? "ui_play" : "ui_pause";
                    costText = _sim.Paused ? "已暂停" : "暂停对局";
                    ui.Cooldown.gameObject.SetActive(false);
                }
                else
                {
                    title = _sim.GameSpeed > 1.5f ? "二倍速" : "一倍速";
                    sprite = _sim.GameSpeed > 1.5f ? "ui_speed2" : "ui_speed1";
                    costText = "点按切换";
                    ui.Cooldown.gameObject.SetActive(false);
                }

                ui.Title.text = title;
                ui.Cost.text = costText;
                ui.Icon.sprite = SpriteLibrary.GetOrFallback(sprite, Palette.Jade);
                ui.Selection.gameObject.SetActive(selected);

                bool affordable = _sim.Spirit >= cost && available;
                ui.Bg.color = selected
                    ? new Color(0.18f, 0.34f, 0.32f, 0.98f)
                    : (affordable ? Palette.ButtonBg : new Color(0.09f, 0.1f, 0.13f, 0.9f));
                Color label = affordable ? Palette.TextMain : Palette.TextDim;
                ui.Title.color = label;
                ui.Cost.color = affordable ? Palette.JadeLight : Palette.Danger;
                ui.Icon.color = affordable ? Color.white : new Color(0.62f, 0.64f, 0.68f, 1f);

                if (ui.Index >= CardSkillStart && ui.Index < CardStartWave)
                {
                    if (cooldown > 0.001f)
                    {
                        ui.Cooldown.gameObject.SetActive(true);
                        ui.Cooldown.fillAmount = cooldown;
                    }
                    else
                    {
                        ui.Cooldown.gameObject.SetActive(false);
                    }
                }
            }
        }

        // ============================================================ 覆盖层

        private GameObject BuildPauseOverlay()
        {
            RectTransform overlay = _app.NewOverlay("PauseOverlay");
            Image panel = UiKit.NewPanel(overlay, "Panel", Palette.PanelBg, true);
            UiKit.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(680f, 560f));

            Text title = UiKit.NewLabel(panel.transform, "Title", "对局暂停", UiKit.FontTitle,
                Palette.Gold, TextAnchor.MiddleCenter);
            UiKit.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -40f), new Vector2(600f, 90f));

            Text hint = UiKit.NewLabel(panel.transform, "Hint",
                "暂停期间可以慢慢想清楚该补哪个位置。\n阵法与仙灵的负面状态也会一起停住。",
                UiKit.FontBody, Palette.TextDim, TextAnchor.UpperCenter);
            UiKit.Place(hint.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -150f), new Vector2(580f, 130f));

            Button resume = UiKit.NewButton(panel.transform, "Resume", "继续对局",
                new Vector2(420f, 96f), UiKit.FontH2, Palette.JadeDark, delegate
                {
                    PlayClick();
                    _sim.Paused = false;
                    _app.CloseOverlay();
                    if (_pauseIcon != null)
                    {
                        _pauseIcon.sprite = SpriteLibrary.GetOrFallback("ui_pause", Palette.TextMain);
                    }
                });
            UiKit.Place(resume.GetComponent<RectTransform>(), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 210f), new Vector2(420f, 96f));

            Button restart = UiKit.NewButton(panel.transform, "Restart", "重开这一局",
                new Vector2(420f, 88f), UiKit.FontBody, Palette.ButtonBg, delegate
                {
                    PlayClick();
                    _app.CloseOverlay();
                    _app.StartBattle(_sim.Difficulty);
                });
            UiKit.Place(restart.GetComponent<RectTransform>(), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 110f), new Vector2(420f, 88f));

            Button quit = UiKit.NewButton(panel.transform, "Quit", "放弃并返回山门",
                new Vector2(420f, 88f), UiKit.FontBody, Palette.CinnabarDark, delegate
                {
                    PlayClick();
                    _app.CloseOverlay();
                    _app.ShowMenu();
                });
            UiKit.Place(quit.GetComponent<RectTransform>(), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(420f, 88f));

            return overlay.gameObject;
        }
    }
}
