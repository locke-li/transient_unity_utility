using System;
using UnityEngine;
using UnityEngine.UI;

namespace Transient.UI {
    public sealed class ShapeGradient : Graphic {
        private Vector2 _center;
        public float[] _value;
        public Color[] _color;
        public float this[int t] { get => _value[t];  set { _value[t] = value; SetVerticesDirty(); } }
        public Vector2 scale = Vector2.one;

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
            CheckCenter();
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

        public void ControlPoint(Color[] color_) {
            _color = color_;
            Array.Resize(ref _value, color_.Length+1);
        }

        protected override void OnPopulateMesh(VertexHelper vh) {
            vh.Clear();
            if(_value.Length < 1) return;
            float width = scale.x * 2;
            float v = 0;
            for (int r = 0; r < _value.Length-1; ++r) {
                v += _value[r];
                float n = v + _value[r + 1];
                var c = _color[r];
                vh.AddVert(_center + new Vector2(-scale.x + width*v, scale.y), c, Vector2.one);
                vh.AddVert(_center + new Vector2(-scale.x + width*n, scale.y), c, Vector2.one);
                vh.AddVert(_center + new Vector2(-scale.x + width*n, -scale.y), c, Vector2.one);
                vh.AddVert(_center + new Vector2(-scale.x + width*v, -scale.y), c, Vector2.one);
                int k = r * 4;
                vh.AddTriangle(k,k+1,k+2);
                vh.AddTriangle(k,k+2,k+3);
            }
        }
    }
}
