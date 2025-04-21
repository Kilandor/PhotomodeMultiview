using UnityEngine;
using UnityEngine.EventSystems;

namespace PhotomodeMultiview
{
    public class DraggableWindowUI : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        private RectTransform rectTransform;
        private Vector2 dragOffset;
        public DroneWindowUI window;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if(window.locked)
            {
                return;
            }

            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform, eventData.position, eventData.pressEventCamera, out dragOffset);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if(window.locked)
            {
                return;
            }

            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            Vector2 localMousePos;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform.parent as RectTransform, eventData.position, eventData.pressEventCamera, out localMousePos))
            {
                rectTransform.localPosition = localMousePos - dragOffset;
            }
        }

    }
}
