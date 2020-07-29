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
            Init(baseTransform, 0.5f * sprite.rect.size / sprite.pixelsPerUnit, sprite.rect.center);
        }

        public void Init(Transform baseTransform, Vector2 extent, Vector2 center) {
            ViewEnv.ViewportLimit = this;
            borderCenter = center;
            borderExtent = extent;
            limit = new PositionLimit();
            //assume base transform Y & rotation won't change
            limit.OffsetY = 0.5f * baseTransform.position.y / Mathf.Tan(baseTransform.rotation.eulerAngles.x * Mathf.Deg2Rad);
            ViewEnv.PositionLimit = limit;
            OnZoom(ViewEnv.Zoom);
        }

        void CalculateViewExtent(float size) {
            viewExtent = new Vector2(ViewEnv.RatioXY * size, size);
            limit.MinX = borderCenter.x - borderExtent.x + viewExtent.x;
            limit.MaxX = borderCenter.x + borderExtent.x - viewExtent.x;
            limit.MinY = borderCenter.y - borderExtent.y + viewExtent.y;
            limit.MaxY = borderCenter.y + borderExtent.y - viewExtent.y;
        }

        public void OnZoom(float zoom_) {
            CalculateViewExtent(zoom_);
            ViewEnv.TryLimitPosition();
        }
    }
}