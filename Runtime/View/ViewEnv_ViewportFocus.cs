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
        private Func<float, float> _damping;
        public static Func<float, float> DampingDefaultDelegate;
        public static Func<float, float> DampingNoneDelegate;

        public ViewportFocus Init() {
            DampingDefaultDelegate = DampingDefaultDelegate ?? DampingDefault;
            DampingNoneDelegate = DampingNoneDelegate ?? DampingNone;
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
            _damping = DampingNoneDelegate;
            //Debug.Log($"focus|D {Focus.name} offset {offset}");
        }

        public void InitWithStep(Transform location, Vector2 offset, bool once = false, float step = 0f, Action afterFocus = null) {
            Focus = location;
            _afterFocus = afterFocus;
            if (location == null) return;
            FocusOffset = offset - ViewEnv.OffsetFromRelativeY(location.position.y);
            _focusOnce = once;
            _focusStep = step <= 0 ? _focusStep : step;
            _damping = once ? DampingNoneDelegate : DampingDefaultDelegate;
            //Debug.Log($"focus|S {Focus.name} offset {offset}");
        }

        private static float DampingDefault(float dist)
            => dist * Mathf.Lerp(0.5f, 2f, dist);
        private static float DampingNone(float dist)
            => 1;

        public void Damping(Func<float, float> v_) => _damping = v_;

        public void Override(bool value_) {
            _focusOverride = value_;
        }

        public void Update(float deltaTime) {
            if (Focus != null && !_focusOverride) {
                var focus = CameraSystem.WorldToSystemXY(Focus.position) + FocusOffset;
                var dir = focus - CameraSystem.PositionXY;
                var dist = dir.magnitude;
                var move = _focusStep * deltaTime;
                //Debug.Log($"focus update {move} {dist} {focus} {CameraSystem.PositionXY} {dir}");
                if (move >= dist) {
                    CameraSystem.PositionXY = focus;
                    if (_focusOnce) {
                        Focus = null;
                    }
                }
                else {
                    dir /= dist;
                    CameraSystem.PositionXY += move * _damping(dist) * dir;
                }
                MainCamera.transform.position = CameraSystem.WorldPosition;
                if (Focus == null && _afterFocus != null) {
                    _afterFocus();
                    _afterFocus = null;
                }
            }
        }
    }
}