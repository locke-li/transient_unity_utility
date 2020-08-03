using UnityEngine;
using Transient.DataAccess;
using Transient.UI;
using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Transient {
    public struct ZoomSetting {
        public float max;
        public float min;
        public float rest;
        public float scrollStep;
        public bool spring;
        public float springStep;
        public float springTolerance;//TODO: make a smooth implementation
    }

    public sealed class ViewEnv : MonoBehaviour {
        public static Camera MainCamera { get; private set; }
        public static Camera UICamera { get; private set; }
        public static Canvas MainCanvas { get; private set; }
        public static RectTransform CanvasContent { get; private set; }

        public static float RatioXY { get; private set; }
        public static float RatioYX { get; private set; }
        public static float UnitPerPixel { get; private set; }
        public static float UIViewScale { get; private set; } = 1f;
        public static Vector2 CanvasSize { get; private set; }
        private static float _perspectiveProjectionFactor = 1f;
        private static int _screenWidth = 0;
        private static int _screenHeight = 0;
        private static float _screenHeightInverse;
        public static IMessagePopup PopupMessage { get; set; }
        public static IMessageFade FadeMessage { get; set; }
        public static ClickVisual ClickVisual { get; set; }
        public static Transform Focus { get; set; } = null;
        public static Vector2 FocusOffset { get; set; }
        private static bool _manualFocus = false;
        private static float FocusStep { get; set; } = 2f;
        public static AbstractCoordinateSystem CameraSystem { get; private set; }

        private static DragReceiver _drag;
        public static ActionList<Vector3, Vector3> OnDrag { get; } = new ActionList<Vector3, Vector3>(8, nameof(OnDrag));

        public static ViewportLimit ViewportLimit { get; set; }
        public static PositionLimit PositionLimit { get; set; }

        private static ZoomSetting _zoomSetting;
        private static float _zoomTarget;
        public static float Zoom { get; private set; }
        public static float ZoomPercent => (Zoom - _zoomSetting.min) / (_zoomSetting.max - _zoomSetting.min);
        public static float ZoomRatio => _zoomSetting.max / _zoomSetting.min;
        public static ActionList<float> OnZoom { get; } = new ActionList<float>(8, nameof(OnZoom));

        public static void Init(Camera main_, Camera ui_, Canvas canvas_) {
            MainCamera = main_;
            //TODO support for overlay canvas
            UICamera = ui_;
            MainCanvas = canvas_;
            _perspectiveProjectionFactor = (float)Math.Tan(MainCamera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            CheckScreenSize();
            CheckCanvas();
            InitViewport();
            ResetCoordinateSystem();
            CanvasContent = MainCanvas.transform.AddChildRect("content");
        }

        public static void Message(string m, Action Confirm_, Action Cancel_) => PopupMessage?.Create(m, Confirm_, Cancel_);

        public static void Message(string m) => FadeMessage?.Create(m, Color.white);
        public static void Message(string m, Color color) => FadeMessage?.Create(m, color);

        public static void ClearMessage() => FadeMessage?.Clear();

        public static void MoveTo(Vector2 pos) {
            CameraSystem.X = pos.x;
            CameraSystem.Y = pos.y;
            MainCamera.transform.position = CameraSystem.WorldPosition;
        }

        public static void TryLimitPosition() {
            if (PositionLimit is null) return;
            (CameraSystem.X, CameraSystem.Y) = PositionLimit.Limit(CameraSystem.X, CameraSystem.Y);
            MainCamera.transform.position = CameraSystem.WorldPosition;
        }

        private static void CheckScreenSize() {
            if (_screenWidth == Screen.width || _screenHeight == Screen.height)
                return;
            RatioXY = (float)Screen.width / Screen.height;
            RatioYX = 1f / RatioXY;
            _screenWidth = Screen.width;
            _screenHeight = Screen.height;
            _screenHeightInverse = 1f / Screen.height;
            UIViewScale = UICamera.orthographicSize * _screenHeightInverse * 2f;
            UnitPerPixel = Zoom * _screenHeightInverse * 2f;
        }

        private static void CheckCanvas() {
            var scaler = MainCanvas.GetComponent<CanvasScaler>();
            if (scaler != null) {
                CanvasSize = scaler.referenceResolution;
            }
            else {
                CanvasSize = new Vector2(Screen.width, Screen.height);
            }
        }

        private static void InitViewport() {
            //TODO what about perspective camera?
            Zoom = MainCamera.orthographicSize;
            UnitPerPixel = Zoom * 2 * _screenHeightInverse;
        }

        public static void InitCoordinateSystem(Vector3 axisX, Vector3 axisY, Vector3 axisZ) {
            if (!(CameraSystem is FlexibleCoordinateSystem)) {
                CameraSystem = new FlexibleCoordinateSystem();
            }
            CameraSystem.AxisX = axisX.normalized;
            CameraSystem.AxisY = axisY.normalized;
            CameraSystem.AxisZ = axisZ.normalized;
            CameraSystem.WorldPosition = MainCamera.transform.position;
        }
        public static void InitCoordinateSystem(Transform local) => InitCoordinateSystem(local.right, local.up, local.forward);

        public static void InitCoordinateSystem(Vector3 axisX, Vector3 axisY, Vector3 axisZ, float scaleX, float scaleY, float scaleZ) {
            if (!(CameraSystem is ScaledCoordinateSystem)) {
                CameraSystem = new ScaledCoordinateSystem();
            }
            var system = (ScaledCoordinateSystem)CameraSystem;
            CameraSystem.AxisX = axisX.normalized;
            CameraSystem.AxisY = axisY.normalized;
            CameraSystem.AxisZ = axisZ.normalized;
            system.ScaleX = scaleX;
            system.ScaleY = scaleY;
            system.ScaleZ = scaleZ;
            CameraSystem.WorldPosition = MainCamera.transform.position;
        }

        public static void ResetCoordinateSystem() {
            if (!(CameraSystem is WorldCoordinateSystem)) {
                CameraSystem = new WorldCoordinateSystem();
            }
            //these axis values won't actually be used
            CameraSystem.AxisX = Vector3.right;
            CameraSystem.AxisY = Vector3.up;
            CameraSystem.AxisZ = Vector3.forward;
            CameraSystem.WorldPosition = MainCamera.transform.position;
        }

        public static void InitViewportControl(DragReceiver drag_) {
            _drag = drag_;
            _drag.WhenDragBegin = d => {
                _manualFocus = true;
                CameraSystem.WorldPosition = MainCamera.transform.position;
            };
            _drag.WhenDrag = (d, offset, pos) => {
                var targetX = CameraSystem.X + offset.x * UnitPerPixel;
                var targetY = CameraSystem.Y + offset.y * UnitPerPixel;
                if (PositionLimit != null) {
                    (targetX, targetY) = PositionLimit.Limit(targetX, targetY);
                }
                MainCamera.transform.position = CameraSystem.SystemToWorld(
                    targetX,
                    targetY,
                    CameraSystem.Z);
                OnDrag.Invoke(offset, new Vector3(targetX, targetY, CameraSystem.Z));
            };
            _drag.WhenDragEnd = d => {
                _manualFocus = false;
                CameraSystem.WorldPosition = MainCamera.transform.position;
            };
            _drag.WhenPinch = (d, start, distance) => {
                TargetZoom(_zoomTarget * start / distance);
            };
            ViewportControl(true);
        }

        public static void ViewportControl(bool v) {
            _drag.interactable = v;
        }

        public static void InitZoom(ZoomSetting setting_) {
            _zoomSetting = setting_;
            if (!MainCamera.orthographic) {
                MainCamera.farClipPlane = setting_.max + 1;
            }
            ZoomTo(setting_.rest);
        }

        private static void ZoomValue(float v_) {
            if (MainCamera.orthographic)
                MainCamera.orthographicSize = v_;
            else {
                MainCamera.transform.position = CameraSystem.SystemToWorld(
                    CameraSystem.X,
                    CameraSystem.Y,
                    -v_ * _perspectiveProjectionFactor);
            }
            Zoom = v_;
            UnitPerPixel = v_ * 2 * _screenHeightInverse;
            OnZoom.Invoke(v_);
            ViewportLimit?.OnZoom(v_);
        }

        public static bool SpringZoomSwitch() {
            return _zoomSetting.spring = !_zoomSetting.spring;
        }

        public static float TargetZoom(float v) {
            return _zoomTarget = v > _zoomSetting.max ? _zoomSetting.max : v < _zoomSetting.min ? _zoomSetting.min : v;
        }

        public static void ZoomTo(float v) {
            ZoomValue(TargetZoom(v));
        }

        public static Vector3 WorldToCanvasSpace(Vector3 position) {
            //TODO persist screen offset
            return (MainCamera.WorldToScreenPoint(position) - new Vector3(Screen.width * 0.5f, Screen.height * 0.5f)) / MainCanvas.scaleFactor;
        }

        public static bool VisibleInCanvas(RectTransform obj) {
            var pos = obj.anchoredPosition;
            return pos.x > 0 && pos.x < CanvasSize.x && pos.y > 0 && pos.y < CanvasSize.y;
        }

        private void FixedUpdate() {
            CheckScreenSize();
        }

        private void Update() {
            FadeMessage?.Fade();
            if (Focus != null && !_manualFocus) {
                var focus = CameraSystem.WorldToSystemXY(Focus.position) + FocusOffset;
                var dir = focus - CameraSystem.PositionXY;
                if (dir.sqrMagnitude < 0.025f) CameraSystem.PositionXY = focus;
                else CameraSystem.PositionXY += dir * FocusStep * Time.deltaTime;
                MainCamera.transform.position = CameraSystem.WorldPosition;
            }
            if (PositionLimit != null && PositionLimit.Unstable && !Input.anyKey) {
                (CameraSystem.X, CameraSystem.Y) = PositionLimit.ElasticPull();
                MainCamera.transform.position = CameraSystem.WorldPosition;
            }
            Vector2 mp = Input.mousePosition;
            if (mp.x < 0 || mp.y < 0 || mp.x > Screen.width || mp.y > Screen.height) return;
            if (Input.GetMouseButtonDown(0)) {
                ClickVisual?.EmitAt(UICamera.ScreenToWorldPoint(mp), UIViewScale);
            }
            if (Input.mouseScrollDelta.y != 0) {
                TargetZoom(_zoomTarget - Input.mouseScrollDelta.y * _zoomSetting.scrollStep);
            }
            //TODO touch zoom
            if (_zoomTarget != Zoom) {
                if (_zoomSetting.spring) {
                    if (_zoomTarget > Zoom) {
                        Zoom = Mathf.Min(_zoomTarget, Zoom + _zoomSetting.springStep);
                    }
                    else {
                        Zoom = Mathf.Max(_zoomTarget, Zoom - _zoomSetting.springStep);
                    }
                }
                else {
                    Zoom = _zoomTarget;
                }
                ZoomValue(Zoom);
            }
        }
    }
}