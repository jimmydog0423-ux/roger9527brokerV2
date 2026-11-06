using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SSD
{
    /// <summary>點一下就執行。比 Button 元件單純，不會有預設的顏色變化。</summary>
    public class Clickable : MonoBehaviour, IPointerClickHandler
    {
        public Action onClick;

        public void OnPointerClick(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left) return;
            if (e.dragging) return;
            if (onClick != null) onClick();
        }
    }

    /// <summary>
    /// 滑鼠移上去（或手指按住）時通知，離開或放開時再通知一次。
    /// 用來在按下出價之前先看成功率。
    /// </summary>
    public class Hover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public Action<bool> onHover;

        public void OnPointerEnter(PointerEventData e) { if (onHover != null) onHover(true); }
        public void OnPointerExit(PointerEventData e) { if (onHover != null) onHover(false); }
        public void OnPointerDown(PointerEventData e) { if (onHover != null) onHover(true); }
        public void OnPointerUp(PointerEventData e) { }
    }

    /// <summary>可以拖曳也可以點擊的物件（資料倉庫的格子）。</summary>
    public class DragSource : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        public Action<PointerEventData> onBegin;
        public Action<PointerEventData> onDrag;
        public Action<PointerEventData> onEnd;
        public Action onClick;
        bool dragged;

        public void OnPointerDown(PointerEventData e)
        {
            dragged = false;
        }

        public void OnBeginDrag(PointerEventData e)
        {
            dragged = true;
            if (onBegin != null) onBegin(e);
        }

        public void OnDrag(PointerEventData e)
        {
            if (onDrag != null) onDrag(e);
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (onEnd != null) onEnd(e);
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left) return;
            if (e.dragging || dragged)
            {
                dragged = false;
                return;
            }
            if (onClick != null) onClick();
        }
    }
}
