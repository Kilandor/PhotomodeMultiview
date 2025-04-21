using UnityEngine;
using UnityEngine.EventSystems;

namespace PhotomodeMultiview
{
    public class RightClickResizer : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private RectTransform rectTransform;
        private bool resizing = false;
        private Vector2 lastMousePos;
        public DroneWindowUI window;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if(window.locked)
            {
                return;
            }

            if (eventData.button == PointerEventData.InputButton.Right)
            {
                resizing = true;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out lastMousePos);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (window.locked)
            {
                return;
            }

            if (resizing && eventData.button == PointerEventData.InputButton.Right)
            {
                Vector2 currentMousePos;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out currentMousePos);
                Vector2 delta = currentMousePos - lastMousePos;

                // Widen and heighten based on drag delta
                rectTransform.sizeDelta += new Vector2(delta.x, -delta.y);

                // Clamp to minimum size
                rectTransform.sizeDelta = new Vector2(
                    Mathf.Max(100, rectTransform.sizeDelta.x),
                    Mathf.Max(100, rectTransform.sizeDelta.y)
                );

                lastMousePos = currentMousePos;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (window.locked)
            {
                return;
            }

            if (eventData.button == PointerEventData.InputButton.Right)
            {
                resizing = false;
            }
        }
    }
}