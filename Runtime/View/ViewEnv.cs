using UnityEngine;
using Transient.SimpleContainer;
using Transient.DataAccess;
using Transient.UI;
using System;
using UnityEngine.UI;

namespace Transient {
    public struct ZoomSetting {
        public float max;
        public float min;
        public float range;
        public float rest;
        public float dragFactor;
        public float scrollStep;
        public bool spring;
        public float springStep;
        public float springTolerance;//TODO: make a smooth implementation
        public Action<Camera, AbstractCoordinateSystem, float> ZoomOverride;
    }

    public sealed class ViewEnv {
        public static ViewEnv Instance { get; private set; }
        public static Camera MainCamera { get; private set; }
        public static Camera UICamera { get; private set; }
        public static Canvas MainCanvas { get; private set; }
        private static CanvasScaler _canvasScaler;
        public static RectTransform CanvasContent { get; private set; }
        public static RectTransform CanvasOverlay { get; private set; }
        public static Vector2 SafeAreaPosOffset;

        public static float RatioXY { get; private set; }
        public static float RatioYX { get; private set; }
        public static float UnitPerPixel { get; private set; }
        public static float UIViewScale { get; private set; } = 1f;
        public static Vector2 CanvasSize { get; private set; }
        private static float _perspectiveProjectionFactor = 1f;
        private static int _screenWidth = 0;
        private static int _screenHeight = 0;
        private static float _screenHeightInverse;
        public static ActionList<int, int, float> OnScreenSizeChange { get; private set; }

        private static List<IMessagePopup> _popupMessage;
        private static List<IMessageFade> _fadeMessage;
        public static ClickVisual ClickVisual { get; set; }
        public static AbstractCoordinateSystem CameraSystem { get; private set; }

        public static ViewportFocus Focus { get; set; }

        private static DragReceiver _drag;
        private static bool _dragFocusOverride = false;
        private static bool _dragInertia;
        private static float _dragInertiaValue;
        private static Vector2 _dragInertiaDelta;
        private static float _dragInertiaThreshold = 0.05f;
        private static float _dragInertiaDamping = 0.85f;
        public static ActionList<Vector3, Vector3> OnDrag { get; private set; }

        public static ViewportLimit ViewportLimit { get; set; }
        public static PositionLimit PositionLimit { get; set; }

        public static ZoomSetting zoomSetting;
        private static bool _zoomMute;
        private static float _zoomCache;
        private static float _zoomTarget;
        private static float _zoomStart;
        private static float _zoomTransitTime;
        private static float _zoomTransitDuration;
        public static float Zoom { get; private set; }
        public static float ZoomPercent => (Zoom - zoomSetting.min) / zoomSetting.range;
        public static ActionList<float> OnZoom { get; private set; }
        public static Action<Camera, AbstractCoordinateSystem, float> ProcessZoom { get; set; }

        public static void Init(ActionList<float> updater, Camera main_, Camera ui_, Canvas canvas_) {
            Performance.RecordProfiler(nameof(ViewEnv));
            Instance = new ViewEnv();
            OnScreenSizeChange = new ActionList<int, int, float>(4, nameof(OnScreenSizeChange));
            OnDrag = new ActionList<Vector3, Vector3>(8, nameof(OnDrag));
            OnZoom = new ActionList<float>(8, nameof(OnZoom));
            MainCamera = main_;
            //NOTE don't support overlay canvas, not quite controllable performance wise
            UICamera = ui_;
            MainCanvas = canvas_;
            _perspectiveProjectionFactor = (float)Math.Tan(MainCamera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            CheckCanvas();
            CheckScreenSize();
            InitViewport();
            if (CameraSystem == null) ResetCoordinateSystem();
            else CameraSystem.SyncPosition(main_.transform);
            CanvasContent = MainCanvas.transform.AddChildRect("content");
            CanvasOverlay = MainCanvas.transform.AddChildRect("overlay");
            updater.Add(Instance.Update, Instance);
            Performance.End(nameof(ViewEnv));
        }

        public static void Clear() {
            OnDrag.Clear();
            OnZoom.Clear();
            _popupMessage?.Clear();
            _fadeMessage?.Clear();
            MainCanvas.DestroyChildren();
        }

        public static void ViewVisible(bool value_) {
            MainCamera.enabled = value_;
        }

        public static void AddMessage(IMessagePopup popup) {
            _popupMessage = _popupMessage ?? new List<IMessagePopup>(2);
            _popupMessage.Add(popup);
        }
        public static void AddMessage(IMessageFade fade) {
            _fadeMessage = _fadeMessage ?? new List<IMessageFade>(2);
            _fadeMessage.Add(fade);
        }

        public static void Message(string m, Action Confirm_, Action Cancel_, PopupOption option_ = new PopupOption(), int index_ = 0)
            => _popupMessage[index_].Create(m, Confirm_, Cancel_, option_);
        public static void CloseMessage(int index_ = 0) => _popupMessage[index_].Clear(true);

        public static void MessageModify(Action<RectTransform> Modify_, int index_ = 0) => _fadeMessage[index_].ModifyMessage = Modify_;
        public static void Message(string m, int index_ = 0) => _fadeMessage[index_].Create(m, Color.clear);
        public static void Message(string m, Color color, int index_ = 0) => _fadeMessage[index_].Create(m, color);
        public static void ClearMessage(int index_ = 0) => _fadeMessage[index_].Clear();

        public static void MoveToSystemPos(Vector2 pos) {
            CameraSystem.X = pos.x;
            CameraSystem.Y = pos.y;
            MainCamera.transform.position = CameraSystem.WorldPosition;
        }

        public static void MoveToSystemPos(Vector2 pos, float z) {
            //TODO temporary
            pos -= OffsetFromRelativeY(MainCamera.transform.position.y - z);
            CameraSystem.X = pos.x;
            CameraSystem.Y = pos.y;
            CameraSystem.Z = z;
            MainCamera.transform.position = CameraSystem.WorldPosition;
        }

        public static void MoveToWorldPos(Vector3 pos) {
            var y = MainCamera.transform.position.y;
            CameraSystem.WorldPosition = pos;
            MainCamera.transform.position = pos;
            if (y != pos.y) {
                ViewportLimit?.CalculateOffset();
            }
        }

        public static void TryLimitPosition() {
            if (PositionLimit is null) return;
            (CameraSystem.X, CameraSystem.Y) = PositionLimit.Limit(CameraSystem.X, CameraSystem.Y);
            MainCamera.transform.position = CameraSystem.WorldPosition;
        }

        private static void CheckScreenSize() {
            if (_screenWidth == Screen.width && _screenHeight == Screen.height)
                return;
            RatioXY = (float)Screen.width / Screen.height;
            RatioYX = 1f / RatioXY;
            _screenWidth = Screen.width;
            _screenHeight = Screen.height;
            _screenHeightInverse = 1f / Screen.height;
            UIViewScale = UICamera.orthographicSize * _screenHeightInverse * 2f;
            UnitPerPixel = Zoom * _screenHeightInverse * 2f;
            CheckCanvas();
            OnScreenSizeChange.Invoke(_screenWidth, _screenHeight, RatioXY);
        }

        private static void CheckCanvas() {
            _canvasScaler = MainCanvas.GetComponent<CanvasScaler>();
            if (_canvasScaler != null) {
                CanvasSize = _canvasScaler.referenceResolution;
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
            CameraSystem.SyncPosition(MainCamera?.transform);
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
            CameraSystem.SyncPosition(MainCamera?.transform);
        }

        public static void ResetCoordinateSystem() {
            if (!(CameraSystem is WorldCoordinateSystem)) {
                CameraSystem = new WorldCoordinateSystem();
            }
            //these axis values won't actually be used
            CameraSystem.AxisX = Vector3.right;
            CameraSystem.AxisY = Vector3.up;
            CameraSystem.AxisZ = Vector3.forward;
            CameraSystem.SyncPosition(MainCamera?.transform);
        }

        public static void FitSafeArea(float l_, float r_, float t_, float b_) {
            Vector2 size;
            if (l_ < 0 && r_ < 0) {
                var rect = Screen.safeArea;
                var pos = rect.position;
                size = rect.size;
                l_ = pos.x;
                r_ = Screen.width - size.x - l_;
                t_ = pos.y;
                b_ = Screen.height - size.y - t_;
                Log.Debug($"SafeArea|Screen.safeArea: {rect}");
            }
            SafeAreaPosOffset = new Vector2(l_ - r_, b_ - t_) * 0.5f;
            size = new Vector2(-l_ - r_, -t_ - b_);
            Log.Debug($"SafeArea|input: ({l_},{r_},{t_},{b_}) {MainCanvas.scaleFactor} output:{SafeAreaPosOffset} {size}");
            CanvasContent.anchoredPosition = SafeAreaPosOffset;
            CanvasOverlay.anchoredPosition = SafeAreaPosOffset;
            CanvasContent.sizeDelta = size;
            CanvasOverlay.sizeDelta = size;
        }

        public static void InitViewportControl(DragReceiver drag_) {
            _drag = drag_;
            _drag.WhenDragBegin = d => {
                Focus?.Override(_dragFocusOverride);
                _dragInertiaValue = 0;
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
            _drag.WhenDragEnd = (d, delta) => {
                Focus?.Override(false);
                _dragInertiaValue = delta.magnitude;
                _dragInertiaDelta = delta / _dragInertiaValue;
                CameraSystem.WorldPosition = MainCamera.transform.position;
            };
            _drag.WhenPinch = (d, start, distance) => {
                if (!_zoomMute) return;
                var diff = (distance - start) * zoomSetting.dragFactor * zoomSetting.range * _screenHeightInverse;
                TargetZoom(_zoomTarget - diff);
            };
            ViewportControl(true);
        }

        public static void ViewportControl(bool v) {
            _drag.enabled = v;
        }

        public static void InitDragInertia(float damping_, float threshold_) {
            _dragInertia = damping_ > 0;
            _dragInertiaDamping = damping_;
            _dragInertiaThreshold = threshold_;
        }

        public static void InitZoom(ZoomSetting setting_) {
            zoomSetting = setting_;
            zoomSetting.range = zoomSetting.max - zoomSetting.min;
#if UNITY_EDITOR_WIN
            zoomSetting.scrollStep *= 4f;
#endif
            if (MainCamera.orthographic) {
                ProcessZoom = setting_.ZoomOverride ?? ZoomOrthographic;
            }
            else {
                ProcessZoom = setting_.ZoomOverride ?? ZoomPerspective;
                MainCamera.farClipPlane = setting_.max + 1;
            }
            ZoomTo(setting_.rest);
        }

        //mute input induced zoom
        public static void MuteZoom(bool value_) {
            _zoomMute = value_;
        }

        public static void CacheZoom() => _zoomCache = Zoom;
        public static void RestoreZoom(float duration_) {
            if (_zoomCache <= 0) return;
            ZoomTo(_zoomCache, duration_);
            _zoomCache = 0;
        }

        private static void ZoomValue(float v_) {
            ProcessZoom?.Invoke(MainCamera, CameraSystem, v_);
            Zoom = v_;
            OnZoom.Invoke(v_);
            ViewportLimit?.OnZoom(v_);
        }

        public static void ZoomOrthographic(Camera camera_, AbstractCoordinateSystem _, float v_) {
            camera_.orthographicSize = v_;
            UpdateViewHeight(v_);
        }

        public static void ZoomPerspective(Camera camera_, AbstractCoordinateSystem system_, float v_) {
            system_.Z = -v_ * _perspectiveProjectionFactor;
            camera_.transform.position = system_.WorldPosition;
            //TODO UnitPerPixel at a designated plane (z)
        }

        private static float CalculateUnitPerPixel(float v_) => v_ * 2 * _screenHeightInverse;
        public static void UpdateViewHeight(float v_) => UnitPerPixel = CalculateUnitPerPixel(v_);

        public static float TargetZoom(float v) {
            return _zoomTarget = v > zoomSetting.max ? zoomSetting.max : v < zoomSetting.min ? zoomSetting.min : v;
        }

        public static void ZoomTo(float v, float duration = 0) {
            _zoomStart = Zoom;
            v = TargetZoom(v);
            _zoomTransitDuration = duration;
            _zoomTransitTime = duration;
            if (duration == 0) ZoomValue(v);
        }

        public static Vector2 ViewportOffsetToWorld(Vector2 viewOffset, float zoom) {
            var unitPerPixel = CalculateUnitPerPixel(zoom);
            return new Vector2(
                -viewOffset.x * _screenWidth * unitPerPixel,
                -viewOffset.y * _screenHeight * unitPerPixel
            );
        }
        public static Vector2 ViewportOffsetToWorld(Vector2 viewOffset)
            => ViewportOffsetToWorld(viewOffset, _zoomTarget);

        public static Vector2 ViewportOffsetToSystem(Vector2 viewOffset, float zoom) {
            return CameraSystem.WorldToSystemXY(ViewportOffsetToWorld(viewOffset, zoom));
        }
        public static Vector2 ViewportOffsetToSystem(Vector2 viewOffset)
            => ViewportOffsetToSystem(viewOffset, _zoomTarget);

        public static Vector2 WorldToCanvasSpace(Vector3 position) {
            //TODO persist screen offset
            return ((Vector2)MainCamera.WorldToScreenPoint(position) - new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)) / MainCanvas.scaleFactor - SafeAreaPosOffset;
        }

        public static Vector2 ScreenToCanvasSpace(Vector2 position) {
            //TODO persist screen offset
            return (position - new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)) / MainCanvas.scaleFactor - SafeAreaPosOffset;
        }

        public static Vector2 UILocalToCanvasSpace(Vector3 position) {
            //TODO persist screen offset
            return ((Vector2)UICamera.WorldToScreenPoint(position) - new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)) / MainCanvas.scaleFactor - SafeAreaPosOffset;
        }

        //TODO should be in CoordinateSystem
        public static Vector2 OffsetFromRelativeY(float height) {
            var relative = MainCamera.transform.position.y - height;
            var rotationAngle = MainCamera.transform.rotation.eulerAngles.x;
            var value = Mathf.Tan(rotationAngle * Mathf.Deg2Rad);
            return value == 0 ? Vector2.zero : new Vector2(0f, 0.5f * relative / value );
        }

        public static bool VisibleInCanvas(RectTransform obj) {
            var pos = obj.anchoredPosition;
            return pos.x > 0 && pos.x < CanvasSize.x && pos.y > 0 && pos.y < CanvasSize.y;
        }

        public static bool VisibleInView(Vector3 pos) {
            pos = MainCamera.WorldToViewportPoint(pos);
            return pos.x > 0 && pos.x < 1 && pos.y > 0 && pos.y < 1;
        }
        public static Vector3 GetLimitWorldToCanvasPosition(Vector3 pos) {
            var canvasPos = WorldToCanvasSpace(pos);
            if (VisibleInView(pos)) {
                return new Vector3(canvasPos.x, canvasPos.y, 0);
            }
            else {
                var viewOffset = MainCamera.WorldToViewportPoint(pos);
                var realX = viewOffset.x >= 0 && viewOffset.x <= 1 ? canvasPos.x : viewOffset.x / Math.Abs(viewOffset.x) * Screen.width * 0.5f / MainCanvas.scaleFactor;
                var realY = viewOffset.y >= 0 && viewOffset.y <= 1 ? canvasPos.y : viewOffset.y / Math.Abs(viewOffset.y) * Screen.height * 0.5f / MainCanvas.scaleFactor;
                viewOffset = new Vector2(viewOffset.x - 0.5f, viewOffset.y - 0.5f);
                float z_rotation_angle;
                if (Math.Abs(viewOffset.y) >= Math.Abs(viewOffset.x)) {
                    z_rotation_angle = viewOffset.y < 0 ? 0 : 180;
                }
                else {
                    z_rotation_angle = viewOffset.x < 0 ? 270 : 90;
                }
                // 0:Down | 90:Right | 180:Up | 270:Left
                return new Vector3(realX, realY, z_rotation_angle);
            }
        }

        private void Update(float deltaTime) {
            CheckScreenSize();
            if (_fadeMessage != null) {
                foreach(var m in _fadeMessage) {
                    m.Fade(deltaTime);
                }
            }
            Focus?.Update(deltaTime);
            if (_dragInertia && _dragInertiaValue > _dragInertiaThreshold) {
                _dragInertiaValue *= _dragInertiaDamping;
                var inertia = _dragInertiaValue * UnitPerPixel * _dragInertiaDelta;
                var targetX = CameraSystem.X - inertia.x;
                var targetY = CameraSystem.Y - inertia.y;
                if (PositionLimit != null) {
                    (targetX, targetY) = PositionLimit.Limit(targetX, targetY);
                }
                MoveToSystemPos(new Vector2(targetX, targetY));
            }
            if (PositionLimit != null && PositionLimit.Unstable && !Input.anyKey) {
                (CameraSystem.X, CameraSystem.Y) = PositionLimit.ElasticPull();
                MainCamera.transform.position = CameraSystem.WorldPosition;
            }
            if (_zoomTarget != Zoom) {
                if (_zoomTransitTime > 0) {
                    _zoomTransitTime -= deltaTime;
                    ZoomValue(Mathf.SmoothStep(_zoomTarget, _zoomStart, _zoomTransitTime / _zoomTransitDuration));
                }
                else {
                    ZoomValue(_zoomTarget);
                }
            }
            Vector2 mp = Input.mousePosition;
            if (mp.x < 0 || mp.y < 0 || mp.x > Screen.width || mp.y > Screen.height) return;
            if (Input.GetMouseButtonDown(0)) {
                ClickVisual?.EmitAt(UICamera.ScreenToWorldPoint(mp), UIViewScale);
            }
            if (!_zoomMute && Input.mouseScrollDelta.y != 0) {
                _zoomTransitTime = 0;
                TargetZoom(_zoomTarget - Input.mouseScrollDelta.y * zoomSetting.scrollStep);
            }
        }
    }
}