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
        public Action<DragReceiver, Vector2> WhenDragEnd { get; set; } = (d, v) => { };
        public Action<DragReceiver> WhenPinchBegin { get; set; } = d => { };
        public Action<DragReceiver, float, float> WhenPinch { get; set; } = (d, b, v) => { };
        public Action<DragReceiver> WhenPinchEnd { get; set; } = d => { };
        public bool interactable { get => _raycastAdapter.enabled; set => _raycastAdapter.enabled = value; }
        internal IEventSystemRaycastAdapter _raycastAdapter;
        private Vector2 _start;
        private float _distance;
        private int touchCount;
        private int fingerId0;
        private int fingerId1;
        public object CustomInfo { get; set; }

        private void OnEnable() {
            if(_raycastAdapter == null) {
                EventSystemRaycastAdapter.Check(transform, out _raycastAdapter);
            }
        }

        private bool MatchingTouch(int id) => fingerId0 == id;

        private bool MatchingTouch2(int id0, int id1) => (fingerId0 == id0 && fingerId1 == id1)
                                                    || (fingerId0 == id1 && fingerId1 == id0);

        public void OnBeginDrag(PointerEventData eventData) {
            //Log.Debug($"begin drag {Input.touchCount} {touchCount}");
            if (!Input.touchSupported) {
                _start = eventData.position;
                WhenDragBegin(this);
                return;
            }
            //try end current op
            if (touchCount != Input.touchCount) {
                if (touchCount == 2) {
                    WhenPinchEnd(this);
                }
                else if (touchCount == 1) {
                    WhenDragEnd(this, Vector2.zero);
                }
                touchCount = Input.touchCount;
            }
            if (touchCount == 2) {
                var touch0 = Input.GetTouch(0);
                var touch1 = Input.GetTouch(1);
                fingerId0 = touch0.fingerId;
                fingerId1 = touch1.fingerId;
                //Log.Debug($"finger {fingerId0},{fingerId1}");
                _distance = (touch0.position - touch1.position).magnitude;
                WhenPinchBegin(this);
            }
            else if (touchCount == 1) {
                var touch = Input.GetTouch(0);
                fingerId0 = touch.fingerId;
                //Log.Debug($"finger {fingerId0}");
                _start = eventData.position;
                WhenDragBegin(this);
            }
        }

        public void OnDrag(PointerEventData eventData) {
            if (!Input.touchSupported) {
                WhenDrag(this, _start - eventData.position, eventData.position);
                return;
            }
            if (Input.touchCount != touchCount) {
                return;
            }
            if (touchCount == 2) {
                var touch0 = Input.GetTouch(0);
                var touch1 = Input.GetTouch(1);
                if (!MatchingTouch2(touch0.fingerId, touch1.fingerId)) {
                    return;
                }
                var distance = (touch0.position - touch1.position).magnitude;
                WhenPinch(this, _distance, distance);
                _distance = distance;
            }
            else if (touchCount == 1) {
                var touch = Input.GetTouch(0);
                if (!MatchingTouch(touch.fingerId)) {
                    return;
                }
                WhenDrag(this, _start - eventData.position, eventData.position);
            }
        }

        public void OnEndDrag(PointerEventData eventData) {
            //Log.Debug("end drag");
            if (!Input.touchSupported) {
                WhenDragEnd(this, eventData.delta);
                return;
            }
            if (touchCount == 2) {
                WhenPinchEnd(this);
            }
            else if (touchCount == 1) {
                WhenDragEnd(this, eventData.delta);
            }
            touchCount = 0;
        }
    }
}
