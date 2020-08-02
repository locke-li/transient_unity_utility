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

        public void InitElastic(float value, float expand) => ViewEnv.PositionLimit.InitElastic(value, expand);
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
        public float ElasticFactor { get; set; }
        private float minExpand;
        private float diffX;
        private float diffY;
        private float diffXSign;
        private float diffYSign;
        private float borderX;
        private float borderY;

        public (float x, float y) Limit(float x, float y) {
            Unstable = false;
            if (ElasticFactor == 0) {
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
            diffX = Mathf.Min(x - borderX, Expand);
            diffY = Mathf.Min(y - borderY, Expand);
            diffXSign = Mathf.Sign(diffX);
            diffYSign = Mathf.Sign(diffY);
            diffX = Mathf.Min(diffX * diffXSign, Expand);
            diffY = Mathf.Min(diffY * diffYSign, Expand);
            borderX -= OffsetX;
            borderY -= OffsetY;
            return DiffLimit();
        }

        private (float x, float y) DiffLimit() {
            return (borderX + diffX * diffXSign, borderY + diffY * diffYSign);
        }

        public void InitElastic(float value, float expand) {
            expand = Mathf.Max(expand, 0f);
            Expand = expand;
            //TODO calculate when the gap is less than half pixel, i.e. visually identical
            minExpand = expand * 0.002f;
            value = Mathf.Max(value, 0f);
            ElasticFactor = Mathf.Pow(0.8f, value);
            Debug.Log(ElasticFactor);
        }

        public (float x, float y) ElsaticPull() {
            diffX *= ElasticFactor;
            diffY *= ElasticFactor;
            if (diffX <= minExpand && diffY <= minExpand) {
                Unstable = false;
                return (borderX, borderY);
            }
            return DiffLimit();
        }
    }
}