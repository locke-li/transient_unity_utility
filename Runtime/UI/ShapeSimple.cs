using UnityEngine;
using UnityEngine.UI;

namespace Transient.UI {
    public sealed class ShapeSimple : Graphic {
        public enum Shape {
            Custom = 0,
            Cross = 128,
            Rectangle = 129,
        }

        private Vector2 _center;
        private Vector2[] _baked;
        public Shape shape = Shape.Custom;
        public Vector2 scale = Vector2.one;
        public float rotation = 0;
        public float featureSize0 = 0;

        protected override void OnEnable() {
            base.OnEnable();
            Scale(scale);
            Bake();
        }
#if UNITY_EDITOR
        protected override void OnValidate() {
            base.OnValidate();
            Scale(scale);
            Bake();
        }
#endif

        private void Bake() {
            switch(shape) {
                case Shape.Cross:
                    BakeCross(featureSize0, rotation);
                    break;
                case Shape.Rectangle:
                    BakeRectangle();
                    break;
            }
        }

        public void BakeCross(float size_, float r_) {
            CheckCenter();
            rotation = r_;
            float sizeX = size_ / scale.x, sizeY = size_ / scale.y;
            _baked = new Vector2[] {
                new Vector2(-sizeX, 1),
                new Vector2(-sizeX, sizeY),
                new Vector2(-1, sizeY),
                new Vector2(-1, -sizeY),
                new Vector2(-sizeX, -sizeY),
                new Vector2(-sizeX, -sizeY),
                new Vector2(-sizeX, -1),
                new Vector2(sizeX, -1),
                new Vector2(sizeX, -sizeY),
                new Vector2(1, -sizeY),
                new Vector2(1, sizeY),
                new Vector2(sizeX, sizeY),
                new Vector2(sizeX, 1),
            };
            SetVerticesDirty();
        }

        public void BakeRectangle() {
            CheckCenter();
            _baked = new Vector2[] {
                new Vector2(-1, 1),
                new Vector2(1, 1),
                new Vector2(1, -1),
                new Vector2(-1, -1),
            };
            SetVerticesDirty();
        }

        private void CheckCenter() {
            var pivot = rectTransform.pivot;
            var rect = rectTransform.rect;
            _center = new Vector2((0.5f-pivot.x)*rect.width, (0.5f-pivot.y)*rect.height);
        }

        protected override void OnRectTransformDimensionsChange() {
            base.OnRectTransformDimensionsChange();
            Scale(rectTransform.rect.size * 0.5f);
            CheckCenter();
        }

        public void Scale(Vector2 v_) {
            scale = v_;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh) {
            vh.Clear();
            if(_baked == null || _baked.Length < 3) return;
            var l = scale.magnitude;
            vh.AddVert(_center, color, new Vector2(0, l));
            var uv = new Vector2(0, l);
            vh.AddVert(_center + _baked[0] * scale, color, uv);
            int r = 1;
            for(;r < _baked.Length;++r) {
                vh.AddVert(_center + _baked[r] * scale, color, uv);
                vh.AddTriangle(0, r+1, r);
            }
            vh.AddTriangle(0, 1, r);
        }
    }
}
