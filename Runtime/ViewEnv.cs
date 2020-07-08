using UnityEngine;
using Transient.DataAccess;
using Transient.UI;
using System;

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
        private static float _perspectiveProjectionFactor = 1f;
        private static int _screenWidth = 0;
        private static int _screenHeight = 0;
        private static float _screenHeightInverse;
        public static MessagePopup PopupMessage { get; set; }
        public static MessageFade FadeMessage { get; set; }
        public static ClickVisual ClickVisual { get; set; }
        public static Transform Focus { get; set; } = null;
        public static Vector2 FocusOffset { get; set; }
        private static bool _manualFocus = false;
        private static float FocusStep { get; set; } = 2f;
        private static ICoordinateSystem CameraSystem;

        private static DragReceiver _drag;
        public static ActionList<Vector3, Vector3> OnDrag { get; } = new ActionList<Vector3, Vector3>(8, nameof(OnDrag));

        private static float _zoom;
        private static ZoomSetting _zoomSetting;
        private static float _zoomTarget;
        public static ActionList<float> OnZoom { get; } = new ActionList<float>(8, nameof(OnZoom));

        public static void Init(Camera main_, Camera ui_, Canvas canvas_) {
            MainCamera = main_;
            //TODO support for overlay canvas
            UICamera = ui_;
            MainCanvas = canvas_;
            _perspectiveProjectionFactor = (float)Math.Tan(MainCamera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            CheckScreenSize();
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

        private static void InitViewport() {
            //TODO what about perspective camera?
            _zoom = MainCamera.orthographicSize;
            UnitPerPixel = _zoom * 2 * _screenHeightInverse;
        }

        public static void InitCoodinateSystem(Vector3 axisX, Vector3 axisY, Vector3 axisZ) {
            if (!(CameraSystem is FlexibleCoordinateSystem)) {
                CameraSystem = new FlexibleCoordinateSystem() {
                    AxisX = axisX.normalized,
                    AxisY = axisY.normalized,
                    AxisZ = axisZ.normalized,
                };
            }
            else {
                CameraSystem.AxisX = axisX.normalized;
                CameraSystem.AxisY = axisY.normalized;
                CameraSystem.AxisZ = axisZ.normalized;
            }
            CameraSystem.WorldPosition = MainCamera.transform.position;
        }

        public static void ResetCoordinateSystem() {
            if (!(CameraSystem is WorldCoordinateSystem)) {
                CameraSystem = new WorldCoordinateSystem() {
                    //these values won't actually be used
                    AxisX = Vector3.right,
                    AxisY = Vector3.up,
                    AxisZ = Vector3.forward,
                };
            }
            CameraSystem.WorldPosition = MainCamera.transform.position;
        }

        public static void InitDrag(DragReceiver drag_) {
            _drag = drag_;
            //_drag.transform.SetParent(MainCanvas.transform, false);
            _drag.WhenDragBegin = d => {
                _manualFocus = true;
                CameraSystem.WorldPosition = MainCamera.transform.position;
            };
            _drag.WhenDrag = (d, offset, pos) => {
                MainCamera.transform.position = CameraSystem.SystemToWorld(
                    CameraSystem.X + offset.x * UnitPerPixel,
                    CameraSystem.Y + offset.y * UnitPerPixel,
                    CameraSystem.Z);
                OnDrag.Invoke(offset, CameraSystem.PositionXY);
            };
            _drag.WhenDragEnd = d => {
                _manualFocus = false;
                CameraSystem.WorldPosition = MainCamera.transform.position;
            };
        }

        public static void ViewportControl(bool t) {
            _drag.interactable = t;
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
            _zoom = v_;
            UnitPerPixel = v_ * 2 * _screenHeightInverse;
            OnZoom.Invoke(v_);
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

        private static void CheckScreenSize() {
            if(_screenWidth == Screen.width || _screenHeight == Screen.height)
                return;
            RatioXY = (float)Screen.width / Screen.height;
            RatioYX = 1f / RatioXY;
            _screenWidth = Screen.width;
            _screenHeight = Screen.height;
            _screenHeightInverse = 1f / Screen.height;
            UIViewScale = UICamera.orthographicSize * _screenHeightInverse * 2f;
            UnitPerPixel = _zoom * _screenHeightInverse * 2f;
        }

        private void FixedUpdate() {
            CheckScreenSize();
        }

        private void Update() {
            FadeMessage?.Fade();
            if(Focus != null && !_manualFocus) {
                var focus = CameraSystem.WorldToSystemXY(Focus.position) + FocusOffset;
                var dir = focus - CameraSystem.PositionXY;
                if(dir.sqrMagnitude < 0.025f) CameraSystem.PositionXY = focus;
                else CameraSystem.PositionXY += dir * FocusStep * Time.deltaTime;
                MainCamera.transform.position = CameraSystem.WorldPosition;
            }
            Vector2 mp = Input.mousePosition;
            if(mp.x < 0 || mp.y < 0 || mp.x > Screen.width || mp.y > Screen.height) return;
            if(Input.GetMouseButtonDown(0)) {
                ClickVisual?.EmitAt(UICamera.ScreenToWorldPoint(mp), UIViewScale);
            }
            if(Input.mouseScrollDelta.y != 0) {
                TargetZoom(_zoomTarget - Input.mouseScrollDelta.y * _zoomSetting.scrollStep);
            }
            //TODO touch zoom
            if(_zoomTarget != _zoom) {
                if (_zoomSetting.spring) {
                    if (_zoomTarget > _zoom) {
                        _zoom = Mathf.Min(_zoomTarget, _zoom + _zoomSetting.springStep);
                    }
                    else {
                        _zoom = Mathf.Max(_zoomTarget, _zoom - _zoomSetting.springStep);
                    }
                }
                else {
                    _zoom = _zoomTarget;
                }
                ZoomValue(_zoom);
            }
        }
    }
}