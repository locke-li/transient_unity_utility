using UnityEngine;
using Transient.SimpleContainer;
using Transient.DataAccess;
using Transient.UI;
using System;
using UnityEngine.UI;

namespace Transient {
    public class ViewportFocus {
        private Camera MainCamera;
        private AbstractCoordinateSystem CameraSystem;
        public Transform Focus;
        public Vector2 FocusOffset;
        private bool _focusOverride = false;
        private bool _focusOnce = false;
        private Action _afterFocus;
        private float _focusStep = 2f;

        public ViewportFocus Init() {
            MainCamera = ViewEnv.MainCamera;
            CameraSystem = ViewEnv.CameraSystem;
            return this;
        }

        public void InitWithDuration(Transform location, Vector2 offset, float duration, Action afterFocus = null) {
            Focus = location;
            _afterFocus = afterFocus;
            if (location == null) return;
            FocusOffset = offset - ViewEnv.OffsetFromRelativeY(location.position.y);
            var dist = Vector2.Distance(CameraSystem.WorldToSystemXY(Focus.position) + FocusOffset, CameraSystem.PositionXY);
            if (duration <= 0) duration = 1;
            _focusOnce = true;
            _focusStep = dist / duration;
        }

        public void InitWithStep(Transform location, Vector2 offset, bool once = false, float step = 0f, Action afterFocus = null) {
            Focus = location;
            _afterFocus = afterFocus;
            if (location == null) return;
            FocusOffset = offset - ViewEnv.OffsetFromRelativeY(location.position.y);
            _focusOnce = once;
            _focusStep = step <= 0 ? _focusStep : step;
        }

        public void Override(bool value_) {
            _focusOverride = value_;
        }

        public void Update(float deltaTime) {
            if (Focus != null && !_focusOverride) {
                var focus = CameraSystem.WorldToSystemXY(Focus.position) + FocusOffset;
                var dir = focus - CameraSystem.PositionXY;
                var dist = dir.magnitude;
                var move = _focusStep * deltaTime;
                if (move >= dist) {
                    CameraSystem.PositionXY = focus;
                    if (_focusOnce) {
                        Focus = null;
                    }
                    if (_afterFocus != null) {
                        _afterFocus();
                        _afterFocus = null;
                    }
                }
                else CameraSystem.PositionXY += _focusStep * deltaTime * dist * Mathf.Lerp(0.5f, 2f, dist) * dir;
                MainCamera.transform.position = CameraSystem.WorldPosition;
            }
        }
    }
}