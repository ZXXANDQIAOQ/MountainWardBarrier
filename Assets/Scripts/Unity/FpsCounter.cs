using UnityEngine;
using UnityEngine.UI;

namespace MountainWardBarrier.Game
{
    /// <summary>
    /// 左上角帧率显示（设置里可以开关）。
    ///
    /// 它挂在 GameBootstrap 下面，随存档里的开关显隐。
    /// 只在打开时才做统计和取样，关着的时候几乎不花时间。
    /// </summary>
    public class FpsCounter : MonoBehaviour
    {
        public static FpsCounter Instance;

        private Text _label;
        private float _accum;
        private int _frames;
        private float _worst;
        private float _nextSample;

        public static FpsCounter Create(Transform parent)
        {
            if (Instance != null)
            {
                return Instance;
            }
            RectTransform rt = UiKit.Node("FpsCounter", parent);
            // 摆在左下角：这块地方在三个界面里都是空的，不会挡住任何有效信息。
            UiKit.Place(rt, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(14f, 10f), new Vector2(280f, 38f));

            FpsCounter counter = rt.gameObject.AddComponent<FpsCounter>();
            counter._label = UiKit.NewLabel(rt, "Text", "", UiKit.FontSmall,
                new Color(0.75f, 0.95f, 0.85f, 0.9f), TextAnchor.MiddleLeft);
            UiKit.Stretch(counter._label.rectTransform);
            Instance = counter;
            return counter;
        }

        private void Start()
        {
            SetVisible(SaveSystem.Data != null && SaveSystem.Data.showFps);
        }

        public static void SetVisible(bool visible)
        {
            if (Instance == null)
            {
                return;
            }
            Instance.gameObject.SetActive(visible);
            Instance._accum = 0f;
            Instance._frames = 0;
            Instance._worst = 0f;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (dt <= 0f)
            {
                return;
            }
            _accum += dt;
            _frames++;
            if (dt > _worst)
            {
                _worst = dt;
            }
            // 每 0.5 秒刷新一次文本，避免数字抖动得看不清
            if (_accum < 0.5f || Time.unscaledTime < _nextSample)
            {
                return;
            }
            _nextSample = Time.unscaledTime + 0.5f;
            float avg = _frames / _accum;
            _label.text = string.Format("{0:F0} FPS  低 {1:F0}", avg, 1f / Mathf.Max(_worst, 0.0001f));
            _accum = 0f;
            _frames = 0;
            _worst = 0f;
        }
    }
}
