//
//  draggable
//
//  author: liyingbo
//

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Transient {
    public class Draggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler {
        public bool canvasOffset = false;
        private Canvas _canvas;
        private Selectable _control;

        private void Start() {
            _control = GetComponent<Selectable>();
            _canvas = GetComponentInParent<Canvas>();
            enabled = _control != null && _canvas != null;
        }

        public void OnBeginDrag(PointerEventData eventData) {
            _control.enabled = false;
        }

        public void OnDrag(PointerEventData eventData) {
            //screen position from event data (2019.3.14)
            var x = Mathf.Clamp(eventData.position.x, 0, Screen.width);
            var y = Mathf.Clamp(eventData.position.y, 0, Screen.height);
            var screenPos = canvasOffset ? new Vector2(x - Screen.width * 0.5f, y - Screen.height * 0.5f) : new Vector2(x, y);
            ((RectTransform)transform).anchoredPosition = screenPos / _canvas.scaleFactor;
        }

        public void OnEndDrag(PointerEventData eventData) {
            _control.enabled = true;
        }
    }
}
