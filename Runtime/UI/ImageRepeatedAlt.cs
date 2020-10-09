﻿using UnityEngine.Sprites;
using UnityEngine;
using UnityEngine.UI;
using Transient.SimpleContainer;

namespace Transient.UI {
    public class ImageRepeatedAlt : Graphic {
        //NOTE must be on the same texture
        public Sprite sprite;
        public Sprite option = null;
        public float spacing;
        public int count;
        public int value;
        public override Texture mainTexture => sprite == null ? base.mainTexture : sprite.texture;

#if UNITY_EDITOR
        protected override void OnValidate() {
            count = Mathf.Max(count, 1);
            value = Mathf.Max(value, 0);
            base.OnValidate();
        }
#endif

        public void Refresh(int value_) {
            if (value == value_) return;
            value = value_;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh) {
            vh.Clear();
            if (sprite == null) return;
            var width = rectTransform.sizeDelta.x;
            var height = rectTransform.sizeDelta.y * 0.5f;
            var size = new Vector4(0, -height, width, height);
            var uv = DataUtility.GetOuterUV(sprite);
            width = rectTransform.sizeDelta.x + spacing;
            var basePos = TransformUtility.CheckOffsetBiased(rectTransform, spacing, value);
            int e = 0;
            for (; e < Mathf.Min(value, count); ++e) {
                var pos = basePos + new Vector2(e * width, 0f);
                MeshUtility.AddSimple(vh, Color.white, pos, size, uv);
            }
            if (option == null) return;
            uv = DataUtility.GetOuterUV(option);
            for (; e < count; ++e) {
                var pos = basePos + new Vector2(e * width, 0f);
                MeshUtility.AddSimple(vh, Color.white, pos, size, uv);
            }
        }
    }
}