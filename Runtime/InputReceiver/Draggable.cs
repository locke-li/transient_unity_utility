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
        private Canvas _canvas;
        private Vector2 _resolution;
        private Selectable _control;

        private void Start() {
            _control = GetComponent<Selectable>();
            _canvas = GetComponentInParent<Canvas>();
            var scaler = _canvas.GetComponent<CanvasScaler>();
            _resolution = scaler.referenceResolution;
            enabled = _control != null && _canvas != null;
        }

        public void OnBeginDrag(PointerEventData eventData) {
            _control.enabled = false;
        }

        public void OnDrag(PointerEventData eventData) {
            var x = Mathf.Clamp(eventData.position.x, 0, Screen.width);
            var y = Mathf.Clamp(eventData.position.y, 0, Screen.height);
            var pos = new Vector2(x, y);
            if (_canvas.worldCamera != null) {
                pos = _canvas.worldCamera.ScreenToViewportPoint(pos);
                pos.x = (pos.x - 0.5f) * _resolution.x;
                pos.y = (pos.y - 0.5f) * _resolution.y;
                transform.localPosition = pos;
            }
            else {
                transform.position = pos;
            }
        }

        public void OnEndDrag(PointerEventData eventData) {
            _control.enabled = true;
        }
    }
}
