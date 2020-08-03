using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Transient {
    public class ViewportLimit {
        private Vector2 viewExtent;
        private Vector2 borderCenter;
        private Vector2 borderExtent;
        private PositionLimit limit;
        private float limitExpand;

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

        void CalculateViewExpand() {
            limit.Expand = (1 - ViewEnv.ZoomPercent) * limitExpand;
        }

        public void OnZoom(float zoom_) {
            CalculateViewExtent(zoom_);
            ViewEnv.TryLimitPosition();
        }

        public void InitElastic(float elasticity, float expand) {
            limitExpand = expand;
            ViewEnv.PositionLimit.InitElastic(elasticity, 0f);
            CalculateViewExpand();
        }
    }

    public class PositionLimit {
        public float MinX { get; set; }
        public float MaxX { get; set; }
        public float OffsetX { get; set; }
        public float MinY { get; set; }
        public float MaxY { get; set; }
        public float OffsetY { get; set; }
        public float Expand { get; set; }
        public bool Unstable { get; set; }
        public float Elasticity { get; set; }
        private float diffX;
        private float diffY;
        private float diffXSign;
        private float diffYSign;
        private float borderX;
        private float borderY;

        public (float x, float y) Limit(float x, float y) {
            Unstable = false;
            if (Expand == 0 || Elasticity == 1) {
                return (
                    Mathf.Min(Mathf.Max(x + OffsetX, MinX), MaxX) - OffsetX,
                    Mathf.Min(Mathf.Max(y + OffsetY, MinY), MaxY) - OffsetY
                );
            }
            return LimitElastic(x, y);
        }

        public (float x, float y) LimitElastic(float x, float y) {
            x += OffsetX;
            y += OffsetY;
            if (x < MinX) {
                borderX = MinX;
                diffX = x - MinX;
            }
            else if (x > MaxX) { borderX = MaxX; }
            else { borderX = x; }
            if (y < MinY) { borderY = MinY; }
            else if (y > MaxY) { borderY = MaxY; }
            else { borderY = y; }
            if (borderX == x && borderY == y) {
                return (x - OffsetX, y - OffsetY);
            }
            Unstable = true;
            diffX = x - borderX;
            diffY = y - borderY;
            diffXSign = Mathf.Sign(diffX);
            diffYSign = Mathf.Sign(diffY);
            diffX = Mathf.Min(diffX * diffXSign, Expand);
            diffY = Mathf.Min(diffY * diffYSign, Expand);
            borderX -= OffsetX;
            borderY -= OffsetY;
            return DiffLimit();
        }

        private (float x, float y) DiffLimit() {
            var extendX = ElasticForceFactor(diffX) * Expand;
            var extendY = ElasticForceFactor(diffY) * Expand;
            //Debug.Log($"{diffX} {diffY} {extendX} {extendY}");
            return (borderX + extendX * diffXSign, borderY + extendY * diffYSign);
        }

        private float ElasticForceFactor(float diff) {
            var percent = diff / Expand;
            return Elasticity * 0.9f * Mathf.Pow(percent, 1.5f - percent);
        }

        public void InitElastic(float elasticity, float expand) {
            expand = Mathf.Max(expand, 0f);
            Expand = expand;
            Elasticity = Mathf.Clamp01(1 / (1 + elasticity));
            //Debug.Log(Elasticity);
        }

        public (float x, float y) ElasticPull() {
            var pull = (1 - Elasticity) * Expand;
            diffX = Mathf.Max(diffX - 0.02f - pull * ElasticForceFactor(diffX), 0f);
            diffY = Mathf.Max(diffY - 0.02f - pull * ElasticForceFactor(diffY), 0f);
            if (diffX <= 0 && diffY <= 0) {
                Unstable = false;
                return (borderX, borderY);
            }
            return DiffLimit();
        }
    }
}