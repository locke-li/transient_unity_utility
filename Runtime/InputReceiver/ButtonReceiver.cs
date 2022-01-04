using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine;
using Transient.Audio;
using System;

namespace Transient.UI {
    [DisallowMultipleComponent, ExecuteInEditMode]
    public sealed class ButtonReceiver : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler,
        IBeginDragHandler, ISubmitHandler, IEventSystemHandler {
        private static float LastResponseTime = 0;
        public static float ResponseInterval = 0.1f;
        public static float PressIntervalMax = 1.6f;

        public int id { get; set; }
        public object CustomInfo { get; set; }
        public Image image;
        public string audioEvent;

        public Action<ButtonReceiver> WhenClick { get; set; } = b => { };
        public Action<ButtonReceiver> WhenClickDown { get; set; } = b => { };
        public Action<ButtonReceiver> WhenClickUp { get; set; } = b => { };
        public Func<ButtonReceiver, bool> WhenDrag { get; set; } = b => false;
        public Action<ButtonReceiver, bool> WhenHover { get; set; } = (b, c) => { };
        public Action<ButtonReceiver> WhenLongPressed { get; set; }

        public float LongPressInterval { get; set; } = 0.5f;
        private static readonly ActionList<float> StepI = new ActionList<float>(2);
        public bool interactable { get => _raycastAdapter.enabled; set => _raycastAdapter.enabled = value; }
        internal IEventSystemRaycastAdapter _raycastAdapter;
        private float _pressTime;
        private Action<float> LongPressStep;
        public bool CancelClickOnce { get; set; } = false;

        private void OnEnable() {
            if(_raycastAdapter == null) {
                var v = EventSystemRaycastAdapter.Check(transform, out _raycastAdapter);
                image = image ?? v as Image;
            }
            if (LongPressStep == null) LongPressStep = CheckLongPress;
        }

        //TODO call this from somewhere, for long press check
        //TODO null token is used
        internal static void RegisterStep(ActionList<float> StepInvoker_) => StepInvoker_.Add(dt => StepI.Invoke(dt), null);

        public ButtonReceiver Id(int id_) {
            id = id_;
            return this;
        }

        private void CheckLongPress(float dt) {
            if(Time.realtimeSinceStartup - _pressTime >= LongPressInterval) {
                WhenLongPressed(this);
                CancelClick();
            }
        }

        public bool InvalidInput(PointerEventData eventData)
            => eventData.button != PointerEventData.InputButton.Left || Input.touchCount > 1;

        public void Click() {
            WhenClick(this);
        }

        public void CancelClick() {
            StepI.Remove(this);
            CancelClickOnce = true;
        }

        public void OnPointerClick(PointerEventData eventData) {
            if (CancelClickOnce || InvalidInput(eventData)) {
                CancelClickOnce = false;
                return;
            }
            if (Time.realtimeSinceStartup - LastResponseTime < ResponseInterval
             || Time.realtimeSinceStartup - _pressTime > PressIntervalMax) {
                return;
            }
            LastResponseTime = Time.realtimeSinceStartup;
            //Log.Debug("button click");
            if(!string.IsNullOrEmpty(audioEvent)) SimpleAudio.Instance?.Event(audioEvent);
            WhenClick(this);
        }

        public void OnPointerDown(PointerEventData eventData) {
            CancelClickOnce = false;
            if (InvalidInput(eventData))
                return;
            _pressTime = Time.realtimeSinceStartup;
            if(WhenLongPressed != null) {//start tracking long press
                StepI.Add(LongPressStep, this);
            }
            WhenClickDown(this);
        }

        public void OnPointerUp(PointerEventData eventData) {
            if (InvalidInput(eventData))
                return;
            StepI.Remove(this);
            WhenClickUp(this);
        }

        public void OnPointerEnter(PointerEventData eventData) => WhenHover(this, true);

        //this is called when enter play mode, why?
        public void OnPointerExit(PointerEventData eventData) {
            CancelClick();
            WhenHover(this, false);
        }

        public void OnBeginDrag(PointerEventData eventData) {
            if (!WhenDrag(this)) {
                CancelClick();
            }
        }

        public void OnSubmit(BaseEventData eventData) => WhenClick(this);
    }
}