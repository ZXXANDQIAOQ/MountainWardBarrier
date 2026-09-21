using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MountainWardBarrier.Game
{
    /// <summary>
    /// 底部卡牌。支持「点一下选中」和「按住拖到棋盘上松手即布置」两种操作 ——
    /// 需求文档里要求的"拖拽操作"就是走这条路径。
    /// </summary>
    public class CardButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        /// <summary>拖动多少像素才算"拖"，低于这个值当成点击。</summary>
        private const float DragThreshold = 18f;

        public int Index;
        public Action<int> OnTap;
        public Action<int, Vector2, bool> OnDragMoving;
        public Action<int, Vector2, bool> OnDragRelease;

        private bool _pressed;
        private bool _dragging;
        private Vector2 _pressPos;

        public bool IsDragging
        {
            get { return _dragging; }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _pressed = true;
            _dragging = false;
            _pressPos = eventData.position;
            if (OnTap != null)
            {
                OnTap(Index);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_pressed)
            {
                return;
            }
            if (!_dragging && Vector2.Distance(eventData.position, _pressPos) > DragThreshold)
            {
                _dragging = true;
            }
            if (OnDragMoving != null)
            {
                OnDragMoving(Index, eventData.position, _dragging);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_pressed)
            {
                return;
            }
            _pressed = false;
            if (OnDragRelease != null)
            {
                OnDragRelease(Index, eventData.position, _dragging);
            }
            _dragging = false;
        }
    }

    /// <summary>棋盘的点击接收器：把屏幕坐标换算成格子坐标后抛给上层。</summary>
    public class BoardInputArea : MonoBehaviour, IPointerClickHandler
    {
        public Action<Vector2> OnTapLocal;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (OnTapLocal == null)
            {
                return;
            }
            RectTransform rect = (RectTransform)transform;
            Vector2 local;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rect, eventData.position, eventData.pressEventCamera, out local))
            {
                OnTapLocal(local);
            }
        }
    }

    /// <summary>通用的小弹窗（帧率显示、通用确认框等）。</summary>
    public class FloatingToast : MonoBehaviour
    {
        private Text _label;
        private float _life;
        private float _duration;
        private Color _baseColor;

        public static FloatingToast Create(Transform parent, Vector2 anchoredPos, int fontSize,
            Color color)
        {
            RectTransform rt = UiKit.Node("Toast", parent);
            UiKit.Place(rt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), anchoredPos,
                new Vector2(700f, 60f));
            FloatingToast toast = rt.gameObject.AddComponent<FloatingToast>();
            toast._label = UiKit.NewLabel(rt, "Text", "", fontSize, color, TextAnchor.MiddleCenter);
            UiKit.Stretch(toast._label.rectTransform);
            toast._baseColor = color;
            toast.gameObject.SetActive(false);
            return toast;
        }

        public void Show(string text, float duration)
        {
            if (_label == null)
            {
                return;
            }
            _label.text = text;
            _life = duration;
            _duration = duration;
            _label.color = _baseColor;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (_life <= 0f)
            {
                return;
            }
            _life -= Time.unscaledDeltaTime;
            float t = _duration > 0f ? 1f - Mathf.Clamp01(_life / _duration) : 1f;
            Color c = _baseColor;
            c.a = _baseColor.a * Mathf.Clamp01(1f - t * t);
            if (_label != null)
            {
                _label.color = c;
            }
            if (_life <= 0f)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
