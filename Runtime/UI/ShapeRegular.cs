using UnityEngine;
using UnityEngine.UI;

namespace Transient.UI {
    public sealed class ShapeRegular : Graphic {
        public enum Shape {
            Custom = 0,
            Triangle = 3,
            Diamond = 4,
            Pentagon = 5,
            Hexagon = 6,
        }

        private Vector2 _center;
        private Vector2[] _baked;
        public Shape shape = Shape.Custom;
        public float scale = 1;
        public float rotation = 0;

        protected override void OnEnable() {
            base.OnEnable();
            Scale(scale);
            Bake();
        }

        protected override void OnValidate() {
            base.OnValidate();
            Scale(scale);
            Bake();
        }

        private void Bake() {
            if(shape == Shape.Custom) return;
            BakeCustom((int)shape, rotation);
        }

        public void Bake(int s, float r) {
            shape = (Shape)s;
            BakeCustom(s, r);
        }

        private void BakeCustom(int s, float r) {
            CheckCenter();
            rotation = r;
            _baked = new Vector2[s];
            float seg = (float)System.Math.PI * 2f / s;
            float rot = rotation * Mathf.Deg2Rad;
            for(int t = 0;t < s;++t) {
                _baked[t] = new Vector2((float)System.Math.Cos(seg * t + rot), (float)System.Math.Sin(seg * t + rot));
            }
            SetVerticesDirty();
        }

        private void CheckCenter() {
            var pivot = rectTransform.pivot;
            var rect = rectTransform.rect;
            _center = new Vector2((0.5f-pivot.x)*rect.width, (0.5f-pivot.y)*rect.height);
        }

        protected override void OnRectTransformDimensionsChange() {
            base.OnRectTransformDimensionsChange();
            var size = rectTransform.sizeDelta;
            Scale((size.x < size.y ? size.x : size.y) * 0.5f);
            CheckCenter();
        }

        public void Scale(float v_) {
            scale = v_;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh) {
            vh.Clear();
            if(_baked == null || _baked.Length < 3) return;
            vh.AddVert(_center, color, new Vector2(0, scale));
            var uv = new Vector2(1, scale);
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