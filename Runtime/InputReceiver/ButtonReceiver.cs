using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine;

namespace Transient.UI {
    [DisallowMultipleComponent, ExecuteInEditMode]
    public sealed class ButtonReceiver : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler,
        IBeginDragHandler, ISubmitHandler, IEventSystemHandler {
        public int id { get; set; }
        public object CustomInfo { get; set; }
        public Image image;

        public Action<ButtonReceiver> WhenClick { get; set; } = b => { };
        public Action<ButtonReceiver> WhenClickDown { get; set; } = b => { };
        public Action<ButtonReceiver> WhenClickUp { get; set; } = b => { };
        public Action<ButtonReceiver, bool> WhenHover { get; set; } = (b, c) => { };
        public Action<ButtonReceiver> WhenLongPressed { get; set; }

        public float LongPressInterval { get; set; } = 0.5f;
        private static readonly ActionList<float> StepI = new ActionList<float>(2);
        public bool interactable { get => _raycastAdapter.enabled; set => _raycastAdapter.enabled = value; }
        private IEventSystemRaycastAdapter _raycastAdapter;
        private float _pressTime;

        public bool CancelClickOnDrag { get; set; } = false;
        private bool _drag;

        private void OnEnable() {
            if(_raycastAdapter == null) {
                var v = EventSystemRaycastAdapter.Check(transform, out _raycastAdapter);
                image = image ?? v as Image;
            }
        }

        //TODO call this from somewhere, for long press check
        //TODO null token is used
        internal static void RegisterStep(ActionList<float> StepInvoker_) => StepInvoker_.Add(dt => StepI.Invoke(dt), null);

        public ButtonReceiver Id(int id_) {
            id = id_;
            return this;
        }

        private void LongPressStep(float dt) {
            _pressTime += dt;
            if(_pressTime >= LongPressInterval) {
                WhenLongPressed(this);
                StepI.Remove(this);
            }
        }

        public void CancelClick() {
            if(EventSystem.current?.currentSelectedGameObject == gameObject) {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        public bool InvalidInput(PointerEventData eventData)
            => eventData.button != PointerEventData.InputButton.Left || Input.touchCount > 1;

        public void OnPointerClick(PointerEventData eventData) {
            if (InvalidInput(eventData))
                return;
            if (_drag && CancelClickOnDrag) {
                _drag = false;
                return;
            }
            WhenClick(this);
        }

        public void OnPointerDown(PointerEventData eventData) {
            if (InvalidInput(eventData))
                return;
            EventSystem.current.SetSelectedGameObject(gameObject, eventData);
            if(WhenLongPressed != null) {//start tracking long press
                _pressTime = 0;
                StepI.Add(LongPressStep, this);
            }
            WhenClickDown(this);
        }

        public void OnPointerUp(PointerEventData eventData) {
            if (InvalidInput(eventData))
                return;
            EventSystem.current.SetSelectedGameObject(null);
            StepI.Remove(this);
            WhenClickUp(this);
        }

        public void OnPointerEnter(PointerEventData eventData) => WhenHover(this, true);

        //this is called when enter play mode, why?
        public void OnPointerExit(PointerEventData eventData) {
            StepI.Remove(this);
            WhenHover(this, false);
        }

        public void OnBeginDrag(PointerEventData eventData) {
            _drag = true;
        }

        public void OnSubmit(BaseEventData eventData) => WhenClick(this);
    }
}