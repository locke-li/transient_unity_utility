using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using System.Text;
using Transient;

namespace Transient.UI {
    public class FocusArea : Graphic, ICanvasRaycastFilter {
        public RectTransform sync;
        public Rect focusRect;
        public bool circle;
        [Range(0.02f, 0.5f)]
        public float radius;
        [Range(0.01f, 0.25f)]
        public float fade;
        private bool hideVisual;//but blocks raycast
        public bool isMask;//ignore focus area, always blocks raycast

#if UNITY_EDITOR
        public Vector2 syncPadding;

        protected override void OnValidate() {
            SetVerticesDirty();
            if (circle) {
                material.color = color;
                material.SetVector("_Gradient", new Vector4(radius, fade, 0, 0));
            }
            if (sync != null) {
                focusRect = SyncRect(sync);
            }
        }
#endif

        private Rect SyncRect(RectTransform transform_) {
            var rect = transform_.rect;
#if UNITY_EDITOR
            rect.center += syncPadding * 0.5f;
            rect.size -= syncPadding;
#endif
            var pos = transform_.TransformPoint(rect.center);
            rect.center = transform.parent.InverseTransformPoint(pos);
            return rect;
        }

        public void Move(Rect rect_) {
            if (circle && rect_.size.x != rect_.size.y) {
                var r = Mathf.Max(rect_.size.x, rect_.size.y);
                var center = rect_.center;
                rect_.size = new Vector2(r, r);
                rect_.center = center;
            }
            if (sync != null) {
                sync.anchoredPosition = rect_.center;
                sync.sizeDelta = rect_.size;
            }
            focusRect = rect_;
            SetVerticesDirty();
        }

        public void Move(Vector2 center_, Vector2 size_)
            => Move(new Rect(center_ - size_ * 0.5f, size_));

        public void Move(RectTransform transform_) {
            Move(SyncRect(transform_));
        }

        public void Hide(bool value) {
            if (hideVisual == value) return;
            hideVisual = value;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh) {
            vh.Clear();
            if (hideVisual) return;
            var rect = rectTransform.rect;
            vh.AddVert(new Vector3(rect.xMin, rect.yMin), color, Vector2.zero);
            vh.AddVert(new Vector3(rect.xMax, rect.yMin), color, Vector2.zero);
            vh.AddVert(new Vector3(rect.xMax, rect.yMax), color, Vector2.zero);
            vh.AddVert(new Vector3(rect.xMin, rect.yMax), color, Vector2.zero);
            vh.AddVert(new Vector3(focusRect.xMin, focusRect.yMin), color, Vector2.zero);
            vh.AddVert(new Vector3(focusRect.xMax, focusRect.yMin), color, Vector2.zero);
            vh.AddVert(new Vector3(focusRect.xMax, focusRect.yMax), color, Vector2.zero);
            vh.AddVert(new Vector3(focusRect.xMin, focusRect.yMax), color, Vector2.zero);
            vh.AddTriangle(0, 4, 5);
            vh.AddTriangle(0, 5, 1);
            vh.AddTriangle(1, 5, 6);
            vh.AddTriangle(1, 6, 2);
            vh.AddTriangle(2, 6, 7);
            vh.AddTriangle(2, 7, 3);
            vh.AddTriangle(3, 7, 4);
            vh.AddTriangle(3, 4, 0);
            if (circle) {
                //8,9,10,11
                vh.AddVert(new Vector3(focusRect.xMin, focusRect.yMin), color, Vector2.zero);
                vh.AddVert(new Vector3(focusRect.xMax, focusRect.yMin), color, new Vector2(1, 0));
                vh.AddVert(new Vector3(focusRect.xMax, focusRect.yMax), color, new Vector2(1, 1));
                vh.AddVert(new Vector3(focusRect.xMin, focusRect.yMax), color, new Vector2(0, 1));
                vh.AddTriangle(8, 10, 9);
                vh.AddTriangle(8, 11, 10);
            }
        }

        public virtual bool IsRaycastLocationValid (Vector2 screenPoint, Camera eventCamera) {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out var val)) {
                return false;
            }
            if (hideVisual || isMask) return true;
            if (circle) {
                var dist = Vector2.Distance(val, focusRect.center);
                return dist > radius * focusRect.width;
            }
            else {
                return !focusRect.Contains(val);
            }
        }
    }
}