using UnityEngine.Sprites;
using UnityEngine;
using UnityEngine.UI;
using Transient.SimpleContainer;

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
            if (mirrored == null) return;
            var offset = new Vector2(0, 0);
            var width = 0f;
            var height = rectTransform.rect.height * 0.5f;
            var center = _center;
            Vector4 uv, uvInner;
            Vector4 rect, border;
            if (middle != null && middle.texture == mirrored.texture) {
                width = middle.rect.width * 0.5f;
                offset = new Vector2(width, 0);
                uv = DataUtility.GetOuterUV(middle);
                uvInner = DataUtility.GetInnerUV(middle);
                rect = new Vector4(-width, -height, width, height);
                if (uv == uvInner) {
                    MeshUtility.AddSimple(vh, color, center, rect, uv);
                }
                else {
                    border = middle.border;
                    MeshUtility.AddSliced(vh, color, center, rect, uv, uvInner, border);
                }
            }
            width = rectTransform.rect.width * 0.5f - width;
            uv = DataUtility.GetOuterUV(mirrored);
            uvInner = DataUtility.GetInnerUV(mirrored);
            rect = new Vector4(-width, -height, 0, height);
            if (uv == uvInner) {
                center = _center - offset;
                MeshUtility.AddSimple(vh, color, center, rect, uv);
                center = _center + offset;
                rect.x = -rect.x;
                MeshUtility.AddSimple(vh, color, center, rect, uv);
            }
            else {
                border = mirrored.border;
                center = _center - offset;
                MeshUtility.AddSliced(vh, color, center, rect, uv, uvInner, border);
                center = _center + offset;
                rect.x = -rect.x;
                MeshUtility.AddSliced(vh, color, center, rect, uv, uvInner, border);
            }
        }
    }
}