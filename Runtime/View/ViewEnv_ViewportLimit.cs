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
        public bool offsetByCameraY;

        public void Init(SpriteRenderer border) {
            var sprite = border.sprite;
            var extent = 0.5f * sprite.rect.size / sprite.pixelsPerUnit;
            var offset = ViewEnv.CameraSystem.WorldToSystemXY(border.transform.position);
            Init(extent, offset + sprite.rect.center / sprite.pixelsPerUnit - extent);
        }

        //TODO need to compensate view extent
        public void InitWithCorner(Vector3 posTopLeft, Vector3 posBottomRight) {
            var system = ViewEnv.CameraSystem;
            var vTL = system.WorldToSystemXY(posTopLeft);
            var vBR = system.WorldToSystemXY(posBottomRight);
            var center = new Vector2(0.5f * (vTL.x + vBR.x), 0.5f * (vTL.y + vBR.y)) + ViewEnv.OffsetFromRelativeY(0);
            var extent = new Vector2(0.5f * Mathf.Abs(vTL.x - vBR.x), 0.5f * Mathf.Abs(vTL.y - vBR.y));
            Init(extent, center);
        }

        public void Init(Vector2 extent, Vector2 center) {
            ViewEnv.ViewportLimit = this;
            limit = new PositionLimit();
            borderCenter = center;
            borderExtent = extent;
            ViewEnv.PositionLimit = limit;
            OnZoom(ViewEnv.Zoom);
        }

        public void ApplyOffsetY() {
            offsetByCameraY = true;
            CalculateOffset();
        }

        internal void CalculateOffset() {
            if (offsetByCameraY) limit.OffsetY = ViewEnv.OffsetFromRelativeY(0).y;
        }

        void CalculateViewExtent() {
            var size = ViewEnv.UnitPerPixel * Screen.height * 0.5f;
            viewExtent = new Vector2(ViewEnv.RatioXY * size, size);
            limit.MinX = borderCenter.x - borderExtent.x + viewExtent.x;
            limit.MaxX = borderCenter.x + borderExtent.x - viewExtent.x;
            limit.MinY = borderCenter.y - borderExtent.y + viewExtent.y;
            limit.MaxY = borderCenter.y + borderExtent.y - viewExtent.y;
            limit.CenterX = (limit.MinX + limit.MaxX) * 0.5f;
            limit.CenterY = (limit.MinY + limit.MaxY) * 0.5f;
            limit.CenterOnX = viewExtent.x >= borderExtent.x;
            limit.CenterOnY = viewExtent.y >= borderExtent.y;
            //Debug.Log($"{viewExtent} {limit.MinX} {limit.MaxX} {limit.MinY} {limit.MaxY}");
        }

        void CalculateViewExpand() {
            //TODO need to improve
            //(1 - ViewEnv.ZoomPercent) * limitExpand;
            limit.Expand = limitExpand;
        }

        public void OnZoom(float _) {
            CalculateViewExtent();
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
        public float CenterX { get; set; }
        public float OffsetX { get; set; }
        public bool CenterOnX { get; set; }
        public float MinY { get; set; }
        public float MaxY { get; set; }
        public float CenterY { get; set; }
        public float OffsetY { get; set; }
        public bool CenterOnY { get; set; }
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
                return Composite(
                    Mathf.Min(Mathf.Max(x + OffsetX, MinX), MaxX) - OffsetX,
                    Mathf.Min(Mathf.Max(y + OffsetY, MinY), MaxY) - OffsetY);
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
            return Composite(
                borderX + extendX * diffXSign, 
                borderY + extendY * diffYSign);
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
                return Composite(borderX, borderY);
            }
            return DiffLimit();
        }

        private (float, float) Composite(float x, float y) {
            if (CenterOnX) x = CenterX;
            if (CenterOnY) y = CenterY;
            return (x, y);
        }
    }
}