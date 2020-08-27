using System;
using UnityEngine;
using UnityEngine.UI;

namespace Transient.UI {
    public sealed class ShapeGradient : Graphic {
        public float[] _value;
        public Color[] _color;
        public float this[int t] { get => _value[t];  set { _value[t] = value; SetVerticesDirty(); } }
        public Vector2 scale = Vector2.one;

        protected override void OnRectTransformDimensionsChange() {
            base.OnRectTransformDimensionsChange();
            Scale(rectTransform.rect.size * 0.5f);
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
            var center = TransformUtility.CheckCenter(rectTransform);
            for (int r = 0; r < _value.Length-1; ++r) {
                v += _value[r];
                float n = v + _value[r + 1];
                var c = _color[r];
                vh.AddVert(center + new Vector2(-scale.x + width*v, scale.y), c, Vector2.one);
                vh.AddVert(center + new Vector2(-scale.x + width*n, scale.y), c, Vector2.one);
                vh.AddVert(center + new Vector2(-scale.x + width*n, -scale.y), c, Vector2.one);
                vh.AddVert(center + new Vector2(-scale.x + width*v, -scale.y), c, Vector2.one);
                int k = r * 4;
                vh.AddTriangle(k,k+1,k+2);
                vh.AddTriangle(k,k+2,k+3);
            }
        }
    }
}
