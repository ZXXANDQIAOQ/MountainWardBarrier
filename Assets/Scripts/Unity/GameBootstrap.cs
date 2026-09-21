using System;
using System.Collections.Generic;
using MountainWardBarrier.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MountainWardBarrier.Game
{
    /// <summary>
    /// 游戏入口。整个工程只有这一个 MonoBehaviour 需要挂在场景里 ——
    /// 而且实际上连挂都不需要：它带 [RuntimeInitializeOnLoadMethod]，
    /// 所以哪怕在空场景里直接按 Play，游戏也会自己起来。
    ///
    /// 场景里没有 Canvas、没有 prefab、没有对资源 GUID 的引用，
    /// 所有界面都是代码在现场搭的，从 git 拉下来就能跑。
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        public const float TopBarHeight = 236f;
        public const float BottomBarHeight = 500f;
        public const float BoardMargin = 18f;

        public static GameBootstrap Instance;

        public Canvas Canvas;
        public RectTransform ScreenRoot;
        public RectTransform OverlayRoot;
        public AudioLibrary Audio;

        /// <summary>当前生效的数值配置（由 Resources/Config/game_config.json 载入）。</summary>
        public GameDatabase Db { get; private set; }

        private string _configJson;
        private GameObject _screen;
        private GameObject _overlay;
        private Vector2 _canvasSize = new Vector2(UiKit.RefWidth, UiKit.RefHeight);

        public Vector2 CanvasSize
        {
            get { return _canvasSize; }
        }

        public float BoardAreaHeight
        {
            get { return Mathf.Max(200f, _canvasSize.y - TopBarHeight - BottomBarHeight); }
        }

        public float BoardCellSize
        {
            get
            {
                float byWidth = (_canvasSize.x - BoardMargin * 2f) / 6f;
                float byHeight = (BoardAreaHeight - BoardMargin * 1.5f) / 9f;
                return Mathf.Max(40f, Mathf.Min(byWidth, byHeight));
            }
        }

        // ------------------------------------------------------------ 启动

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBoot()
        {
            if (Instance != null)
            {
                return;
            }
            GameObject go = new GameObject("~仙侠护山大阵");
            go.AddComponent<GameBootstrap>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.SystemSetting;

            _canvasSize = ComputeCanvasSize();

            EnsureCamera();
            EnsureEventSystem();
            BuildCanvas();
            UiKit.WarmUpFont();

            Db = NewDatabase();

            Audio = AudioLibrary.Create(transform);
            SaveSystem.Load();
            Audio.ApplySettings(SaveSystem.Data.musicEnabled, SaveSystem.Data.sfxEnabled,
                SaveSystem.Data.musicVolume, SaveSystem.Data.sfxVolume);

            // 帧率显示挂在 Canvas 下、两个界面根节点的后面，这样永远画在最上层。
            FpsCounter.Create(Canvas.transform);
            FpsCounter.SetVisible(SaveSystem.Data.showFps);

            ShowMenu();
        }

        /// <summary>
        /// 从 Resources/Config/game_config.json 载入数值表，每次返回一份**新的**实例。
        ///
        /// 对局里会拿到自己那一份，改不了菜单/典籍用的那份；JSON 缺失或写坏了就退回内置默认值，
        /// 所以哪怕玩家把配置文件改烂了，游戏依然能开。
        /// </summary>
        public GameDatabase NewDatabase()
        {
            if (_configJson == null)
            {
                try
                {
                    TextAsset asset = Resources.Load<TextAsset>("Config/game_config");
                    _configJson = asset != null ? asset.text : "";
                    if (asset == null)
                    {
                        Debug.LogWarning("[GameBootstrap] 未找到 Config/game_config.json，使用内置默认数值。");
                    }
                }
                catch (Exception e)
                {
                    _configJson = "";
                    Debug.LogWarning("[GameBootstrap] 读取配置失败，使用内置默认数值：" + e.Message);
                }
            }

            GameDatabase db = ConfigLoader.Load(_configJson);
            for (int i = 0; i < ConfigLoader.LastWarnings.Count; i++)
            {
                Debug.LogWarning("[配置] " + ConfigLoader.LastWarnings[i]);
            }
            return db;
        }

        /// <summary>
        /// 算出 CanvasScaler 缩放之后的"参考分辨率尺寸"。
        /// 有了它就能在 Awake 里立刻算出棋盘格子大小，不用等一帧布局。
        /// </summary>
        public static Vector2 ComputeCanvasSize()
        {
            float sw = Screen.width;
            float sh = Screen.height;
            if (sw <= 1f || sh <= 1f)
            {
                return new Vector2(UiKit.RefWidth, UiKit.RefHeight);
            }
            const float match = 0.5f;
            float logW = Mathf.Log(sw / UiKit.RefWidth, 2f);
            float logH = Mathf.Log(sh / UiKit.RefHeight, 2f);
            float scale = Mathf.Pow(2f, match * logW + (1f - match) * logH);
            if (scale <= 0.0001f)
            {
                scale = 1f;
            }
            return new Vector2(sw / scale, sh / scale);
        }

        private void EnsureCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
                go.transform.position = new Vector3(0f, 0f, -10f);
            }
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.Ink;
            cam.cullingMask = 0; // 全部界面都由 Canvas 负责，相机不需要渲染任何东西
            if (cam.GetComponent<AudioListener>() == null
                && UnityEngine.Object.FindObjectOfType<AudioListener>() == null)
            {
                cam.gameObject.AddComponent<AudioListener>();
            }
        }

        /// <summary>
        /// 保证场景里有 EventSystem。
        /// 使用反射挑选输入模块：装了新版 Input System 就用它的模块，否则用旧的 StandaloneInputModule，
        /// 这样无论工程的 Active Input Handling 设成哪种都不会点不动。
        /// </summary>
        private void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindObjectOfType<EventSystem>() != null)
            {
                return;
            }
            GameObject go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            Type moduleType = null;
            try
            {
                moduleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            }
            catch (Exception)
            {
                moduleType = null;
            }
            if (moduleType != null)
            {
                go.AddComponent(moduleType);
            }
            else
            {
                go.AddComponent<StandaloneInputModule>();
            }
        }

        private void BuildCanvas()
        {
            GameObject canvasGo = new GameObject("UI Canvas");
            canvasGo.transform.SetParent(transform, false);

            Canvas = canvasGo.AddComponent<Canvas>();
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Canvas.pixelPerfect = false;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(UiKit.RefWidth, UiKit.RefHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;

            canvasGo.AddComponent<GraphicRaycaster>();

            RectTransform root = (RectTransform)canvasGo.transform;
            ScreenRoot = UiKit.Node("ScreenRoot", root);
            UiKit.Stretch(ScreenRoot);
            OverlayRoot = UiKit.Node("OverlayRoot", root);
            UiKit.Stretch(OverlayRoot);
        }

        // ------------------------------------------------------------ 界面切换

        private void SetScreen(GameObject go)
        {
            if (_screen != null)
            {
                Destroy(_screen);
                _screen = null;
            }
            _screen = go;
            if (go != null)
            {
                go.transform.SetParent(ScreenRoot, false);
            }
        }

        public void ShowMenu()
        {
            SaveSystem.Data.Normalize();
            SetScreen(MenuScreen.Build(ScreenRoot));
            if (Audio != null)
            {
                Audio.PlayMusic("bgm_menu");
            }
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
        }

        public void StartBattle(Difficulty difficulty)
        {
            int seed = UnityEngine.Random.Range(1, int.MaxValue);
            StartBattle(difficulty, seed);
        }

        public void StartBattle(Difficulty difficulty, int seed)
        {
            CloseOverlay();
            GameObject go = UiKit.Node("BattleScreen", ScreenRoot).gameObject;
            BattleScreen screen = go.AddComponent<BattleScreen>();
            screen.Setup(this, difficulty, seed);
            _screen = go;
            if (Audio != null)
            {
                Audio.PlayMusic("bgm_battle");
            }
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        public void ShowResult(BattleReport report, List<int> freshAchievements)
        {
            CloseOverlay();
            GameObject go = UiKit.Node("ResultScreen", ScreenRoot).gameObject;
            ResultScreen screen = go.AddComponent<ResultScreen>();
            screen.Setup(this, report, freshAchievements);
            _screen = go;
            if (Audio != null)
            {
                Audio.StopMusic();
                Audio.PlaySfx(report != null && report.victory ? "sfx_victory" : "sfx_defeat");
            }
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
        }

        /// <summary>打开一个覆盖层（弹窗）。同一时间只允许一个。</summary>
        public void ShowOverlay(GameObject go)
        {
            CloseOverlay();
            _overlay = go;
            if (go != null)
            {
                go.transform.SetParent(OverlayRoot, false);
            }
        }

        public void CloseOverlay()
        {
            if (_overlay != null)
            {
                Destroy(_overlay);
                _overlay = null;
            }
        }

        public bool HasOverlay
        {
            get { return _overlay != null; }
        }

        public RectTransform NewOverlay(string name)
        {
            return UiKit.NewOverlayRoot(OverlayRoot, name, Palette.Scrim);
        }

        // ------------------------------------------------------------ 通用小工具

        public void PlayClick()
        {
            if (Audio != null)
            {
                Audio.PlaySfx("sfx_click", 0.7f, 0.05f);
            }
        }

        public void ApplyAudioSettings()
        {
            SaveData data = SaveSystem.Data;
            if (Audio != null)
            {
                Audio.ApplySettings(data.musicEnabled, data.sfxEnabled,
                    data.musicVolume, data.sfxVolume);
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                SaveSystem.Save();
            }
        }

        /// <summary>
        /// 安卓返回键 / 键盘 Esc。
        ///
        /// 对局界面的返回键由 BattleScreen 自己处理（它更清楚当前该退哪一层），
        /// 所以这里只在"非对局界面"上把打开的弹窗关掉，避免两边同时响应。
        /// </summary>
        private void Update()
        {
            if (_screen == null || _overlay == null)
            {
                return;
            }
            if (_screen.name == "BattleScreen")
            {
                return;
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CloseOverlay();
            }
        }

        private void OnApplicationQuit()
        {
            SaveSystem.Save();
        }
    }
}
