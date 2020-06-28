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
        private static DragReceiver _drag;
        public static MessagePopup PopupMessage { get; set; }
        public static MessageFade FadeMessage { get; set; }
        public static ClickVisual ClickVisual { get; set; }
        public static Transform Focus { get; set; } = null;
        public static Vector2 FocusOffset { get; set; }
        private static bool _manualFocus = false;
        private static float FocusStep { get; set; } = 2f;
        private static Vector2 _campos;
        private static float _camz = -5f;

        private static float _zoom;
        private static ZoomSetting _zoomSetting;
        private static float _zoomTarget;
        private static ActionList<float> OnZoom { get; } = new ActionList<float>(8, nameof(OnZoom));

        public static void Init(Camera main_, Camera ui_, Canvas canvas_) {
            MainCamera = main_;
            UICamera = ui_;
            MainCanvas = canvas_;
            _perspectiveProjectionFactor = (float)Math.Tan(MainCamera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            CheckScreenSize();
            InitViewport(MainCanvas.transform);
            CanvasContent = MainCanvas.transform.AddChildRect("content");
        }

        public static void Message(string m, Action Confirm_, Action Cancel_) => PopupMessage?.Create(m, Confirm_, Cancel_);

        public static void Message(string m) => FadeMessage?.Create(m, Color.white);
        public static void Message(string m, Color color) => FadeMessage?.Create(m, color);

        public static void ClearMessage() => FadeMessage?.Clear();

        public static void MoveTo(Vector3 pos) {
            _campos = pos;
            MainCamera.transform.position = new Vector3(_campos.x, _campos.y, _camz + pos.z);
        }

        private static void InitViewport(Transform canvas_) {
            _drag = AssetMapping.Default.TakePersistent<GameObject>(null, "view_drag").GetChecked<DragReceiver>();
            _drag.transform.SetParent(canvas_, false);
            _drag.WhenDragBegin = d => {
                _manualFocus = true;
                _campos = MainCamera.transform.position;
            };
            _drag.WhenDrag = (d, offset, pos) => {
                MainCamera.transform.position = new Vector3(
                    _campos.x + offset.x * UnitPerPixel,
                    _campos.y + offset.y * UnitPerPixel,
                    _camz
                );
            };
            _drag.WhenDragEnd = d => {
                _manualFocus = false;
                _campos = MainCamera.transform.position;
            };
            _campos = MainCamera.transform.position;
            _camz = MainCamera.transform.position.z;
            _zoom = MainCamera.orthographicSize;
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

        public static void ZoomSync(Action<float> sync_, object token_) {
            OnZoom.Add(sync_, token_);
            sync_(_zoom);
        }

        public static void ZoomUnsync(object token_) {
            OnZoom.Remove(token_);
        }

        private static void ZoomValue(float v_) {
            if (MainCamera.orthographic)
                MainCamera.orthographicSize = v_;
            else {
                _camz = -v_ * _perspectiveProjectionFactor;
                var p = MainCamera.transform.position;
                MainCamera.transform.position = new Vector3(p.x, p.y, _camz);
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
                Vector2 focus = (Vector2)Focus.position + FocusOffset;
                Vector2 dir = focus - _campos;
                if(dir.sqrMagnitude < 0.025f) _campos = focus;
                else _campos += dir * FocusStep * Time.deltaTime;
                MainCamera.transform.position = new Vector3(_campos.x, _campos.y, _camz);
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
                if(_zoomTarget > _zoom) {
                    _zoom += _zoomSetting.springStep;
                    if(_zoomTarget < _zoom)
                        _zoom = _zoomTarget;
                }
                else {
                    _zoom -= _zoomSetting.springStep;
                    if(_zoomTarget > _zoom)
                        _zoom = _zoomTarget;
                }
                ZoomValue(_zoom);
            }
        }
    }
}