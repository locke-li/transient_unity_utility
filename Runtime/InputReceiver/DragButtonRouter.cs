using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine;
using Transient.Audio;

namespace Transient.UI {
    [DisallowMultipleComponent, ExecuteInEditMode]
    public sealed class DragButtonRouter : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler,
        ISubmitHandler, IEventSystemHandler {
        public bool interactable { get => _raycastAdapter.enabled; set => _raycastAdapter.enabled = value; }
        internal IEventSystemRaycastAdapter _raycastAdapter;
        public DragReceiver drag;
        public ButtonReceiver button;

        private void OnEnable() {
            if(drag == null || button == null) {
                enabled = false;
                return;
            }
            if(_raycastAdapter == null) {
                EventSystemRaycastAdapter.Check(transform, out _raycastAdapter);
            }
            drag._raycastAdapter = _raycastAdapter;
            button._raycastAdapter = _raycastAdapter;
        }

        public void OnPointerClick(PointerEventData eventData) {
            if(button.enabled) button.OnPointerClick(eventData);
        }

        public void OnPointerDown(PointerEventData eventData) {
            if(button.enabled) button.OnPointerDown(eventData);
        }

        public void OnPointerUp(PointerEventData eventData) {
            if(button.enabled) button.OnPointerUp(eventData);
        }

        public void OnPointerEnter(PointerEventData eventData) {
            if(button.enabled) button.OnPointerEnter(eventData);
        }

        public void OnPointerExit(PointerEventData eventData) {
            if(button.enabled) button.OnPointerExit(eventData);
        }

        public void OnBeginDrag(PointerEventData eventData) {
            if(drag.enabled) drag.OnBeginDrag(eventData);
            if(button.enabled) button.OnBeginDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData) {
            if(drag.enabled) drag.OnDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData) {
            if(drag.enabled) drag.OnEndDrag(eventData);
        }

        public void OnSubmit(BaseEventData eventData) {
            if(button.enabled) button.OnSubmit(eventData);
        }
    }
}