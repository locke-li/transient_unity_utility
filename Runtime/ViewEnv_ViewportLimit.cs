using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Transient {
    public class ViewportLimit {
        private Vector2 viewExtent;
        private Vector2 borderCenter;
        private Vector2 borderExtent;
        private PositionLimit limit;

        public void Init(Transform baseTransform, SpriteRenderer border) {
            var sprite = border.sprite;
            var extent = 0.5f * sprite.rect.size / sprite.pixelsPerUnit;
            var offset = ViewEnv.CameraSystem.WorldToSystemXY(border.transform.position);
            Init(baseTransform, extent, offset + sprite.rect.center / sprite.pixelsPerUnit - extent);
        }

        public void Init(Transform baseTransform, Vector2 extent, Vector2 center) {
            //assume base transform Y & rotation X won't change
            Init(baseTransform.position.y, baseTransform.rotation.eulerAngles.x, extent, center);
        }

        public void Init(float height, float rotationAngle, Vector2 extent, Vector2 center) {
            ViewEnv.ViewportLimit = this;
            borderCenter = center;
            borderExtent = extent;
            limit = new PositionLimit();
            limit.OffsetY = 0.5f * height / Mathf.Tan(rotationAngle * Mathf.Deg2Rad);
            ViewEnv.PositionLimit = limit;
            OnZoom(ViewEnv.Zoom);
        }

        void CalculateViewExtent(float size) {
            viewExtent = new Vector2(ViewEnv.RatioXY * size, size);
            limit.MinX = borderCenter.x - borderExtent.x + viewExtent.x;
            limit.MaxX = borderCenter.x + borderExtent.x - viewExtent.x;
            limit.MinY = borderCenter.y - borderExtent.y + viewExtent.y;
            limit.MaxY = borderCenter.y + borderExtent.y - viewExtent.y;
            //Debug.Log($"{limit.MinX} {limit.MaxX} {limit.MinY} {limit.MaxY}");
        }

        public void OnZoom(float zoom_) {
            CalculateViewExtent(zoom_);
            ViewEnv.TryLimitPosition();
        }
    }

    public class PositionLimit {
        public float MinX { get; set; }
        public float MaxX { get; set; }
        public float OffsetX { get; set; }
        public float MinY { get; set; }
        public float MaxY { get; set; }
        public float OffsetY { get; set; }
        public float Elasticity { get; set; }

        public (float x, float y) Limit(float x, float y) {
            return (
                Mathf.Min(Mathf.Max(x + OffsetX, MinX), MaxX) - OffsetX,
                Mathf.Min(Mathf.Max(y + OffsetY, MinY), MaxY) - OffsetY
            );
        }
    }
}