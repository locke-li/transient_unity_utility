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
        public Action<DragReceiver> WhenPinchBegin { get; set; } = d => { };
        public Action<DragReceiver, float, float> WhenPinch { get; set; } = (d, b, v) => { };
        public Action<DragReceiver> WhenPinchEnd { get; set; } = d => { };
        public bool interactable { get => _raycastAdapter.enabled; set => _raycastAdapter.enabled = value; }
        private IEventSystemRaycastAdapter _raycastAdapter;
        private Vector2 _start;
        private float _distance;
        private int touchCount;
        public object CustomInfo { get; set; }

        private void OnEnable() {
            if(_raycastAdapter == null) {
                EventSystemRaycastAdapter.Check(transform, out _raycastAdapter);
            }
        }

        public void OnBeginDrag(PointerEventData eventData) {
            touchCount = Input.touchCount;
            if (touchCount == 2) {
                var finger0 = Input.GetTouch(0);
                var finger1 = Input.GetTouch(1);
                _distance = (finger0.position - finger1.position).magnitude;
                WhenPinchBegin(this);
            }
            else {
                _start = eventData.position;
                WhenDragBegin(this);
            }
        }

        public void OnDrag(PointerEventData eventData) {
            if (Input.touchCount != touchCount) {
                return;
            }
            if (touchCount == 2) {
                var finger0 = Input.GetTouch(0);
                var finger1 = Input.GetTouch(1);
                var distance = (finger0.position - finger1.position).magnitude;
                WhenPinch(this, _distance, distance);
                _distance = distance;
            }
            else {
                WhenDrag(this, _start - eventData.position, eventData.position);
            }
        }

        public void OnEndDrag(PointerEventData eventData) {
            if (touchCount == 2) {
                WhenPinchEnd(this);
            }
            else {
                WhenDragEnd(this);
            }
        }
    }
}
