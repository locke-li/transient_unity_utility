using System.Collections;
using System.Collections.Generic;
using UnityEngine.Sprites;
using UnityEngine;
using UnityEngine.UI;

namespace Transient.UI {
    public class ImageMirrored : Graphic{
        //NOTE both sprites need to be on the same texture
        public Sprite mirrored;
        public Sprite middle;
        private Vector2 _center;
        public override Texture mainTexture => mirrored == null ? base.mainTexture : mirrored.texture;

#if UNITY_EDITOR
        protected override void OnValidate() {
            base.OnValidate();
            Bake();
        }
#endif

        private void Bake() {
            CheckCenter();
            SetVerticesDirty();
        }

        private void CheckCenter() {
            var pivot = rectTransform.pivot;
            var rect = rectTransform.rect;
            _center = new Vector2((0.5f - pivot.x) * rect.width, (0.5f - pivot.y) * rect.height);
        }

        protected override void OnRectTransformDimensionsChange() {
            base.OnRectTransformDimensionsChange();
            CheckCenter();
        }

        protected override void OnPopulateMesh(VertexHelper vh) {
            vh.Clear();
            //TODO extend direction
            //TODO sliced sprite
            if (mirrored == null) return;
            var offset = new Vector2(0, 0);
            var vOffset = 0;
            var width = 0f;
            var height = rectTransform.rect.height * 0.5f;
            Vector4 uv;
            if (middle != null && middle.texture == mirrored.texture) {
                width = middle.rect.width * 0.5f;
                offset = new Vector2(width, 0);
                uv = DataUtility.GetOuterUV(middle);
                vOffset = 4;
                vh.AddVert(_center + new Vector2(-width, -height), color, new Vector2(uv.x, uv.y));
                vh.AddVert(_center + new Vector2(-width, height), color, new Vector2(uv.x, uv.w));
                vh.AddVert(_center + new Vector2(width, height), color, new Vector2(uv.z, uv.w));
                vh.AddVert(_center + new Vector2(width, -height), color, new Vector2(uv.z, uv.y));
                vh.AddTriangle(0, 1, 2);
                vh.AddTriangle(0, 2, 3);
            }
            width = rectTransform.rect.width * 0.5f - width;
            uv = DataUtility.GetOuterUV(mirrored);
            var center = _center - offset;
            vh.AddVert(center + new Vector2(-width, -height), color, new Vector2(uv.x, uv.y));
            vh.AddVert(center + new Vector2(-width, height), color, new Vector2(uv.x, uv.w));
            vh.AddVert(center + new Vector2(0, height), color, new Vector2(uv.z, uv.w));
            vh.AddVert(center + new Vector2(0, -height), color, new Vector2(uv.z, uv.y));
            vh.AddTriangle(vOffset, vOffset + 1, vOffset + 2);
            vh.AddTriangle(vOffset, vOffset + 2, vOffset + 3);
            center = _center + offset;
            vh.AddVert(center + new Vector2(width, -height), color, new Vector2(uv.x, uv.y));
            vh.AddVert(center + new Vector2(width, height), color, new Vector2(uv.x, uv.w));
            vh.AddVert(center + new Vector2(0, height), color, new Vector2(uv.z, uv.w));
            vh.AddVert(center + new Vector2(0, -height), color, new Vector2(uv.z, uv.y));
            vh.AddTriangle(vOffset + 6, vOffset + 5, vOffset + 4);
            vh.AddTriangle(vOffset + 7, vOffset + 6, vOffset + 4);
        }
    }
}