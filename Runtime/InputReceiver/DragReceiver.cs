//
//  ScriptableDrag
//
//  author: liyingbo
//

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Transient.UI {
    public struct DragOffset {
        public Vector2 current;
        public Vector2 persist;
        private float xMin, xMax, yMin, yMax;
        public Vector2 Value => current + persist;

        public void Apply() {
            persist += current;
            current = Vector2.zero;
            if(persist.x < xMin) persist.x = xMin;
            else if(persist.x > xMax) persist.x = xMax;
            if(persist.y < yMin) persist.y = yMin;
            else if(persist.y > yMax) persist.y = yMax;
        }

        public void Limit(float xMin_, float xMax_, float yMin_, float yMax_) {
            xMin = xMin_;
            xMax = xMax_;
            yMin = yMin_;
            yMax = yMax_;
        }

        public void Reset() {
            current = Vector2.zero;
            persist = Vector2.zero;
        }
    }

    [DisallowMultipleComponent, ExecuteInEditMode]
    public class DragReceiver : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler {
        public Action<DragReceiver> WhenDragBegin { get; set; } = d => { };
        public Action<DragReceiver, Vector2, Vector2> WhenDrag { get; set; } = (d, v, p) => { };
        public Action<DragReceiver> WhenDragEnd { get; set; } = d => { };
        public bool interactable { get => _raycastAdapter.enabled; set => _raycastAdapter.enabled = value; }
        private IEventSystemRaycastAdapter _raycastAdapter;
        private Vector2 _start;
        public object CustomInfo { get; set; }

        private void OnEnable() {
            if(_raycastAdapter == null) {
                EventSystemRaycastAdapter.Check(transform, out _raycastAdapter);
            }
        }

        public void OnBeginDrag(PointerEventData eventData) {
            _start = eventData.position;
            WhenDragBegin(this);
        }

        public void OnDrag(PointerEventData eventData) {
            WhenDrag(this, _start - eventData.position, eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData) {
            WhenDragEnd(this);
        }
    }
}
