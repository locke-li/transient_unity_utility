using UnityEngine.Sprites;
using UnityEngine;
using UnityEngine.UI;

namespace Transient.UI {
    public class Image3Split : Graphic{
        //NOTE both sprites need to be on the same texture
        public Sprite left;
        public Sprite middle;
        public Sprite right;
        private Vector2 _center;
        public override Texture mainTexture => middle == null ? base.mainTexture : middle.texture;

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
            var offset = new Vector2(0, 0);
            var width = 0f;
            var height = rectTransform.rect.height * 0.5f;
            var center = _center;
            Vector4 uv, uvInner;
            Vector4 rect;
            if (middle != null) {
                width = middle.rect.width * 0.5f;
                offset = new Vector2(width, 0);
                uv = DataUtility.GetOuterUV(middle);
                uvInner = DataUtility.GetInnerUV(middle);
                rect = new Vector4(-width, -height, width, height);
                if (uv == uvInner) {
                    MeshUtility.AddSimple(vh, color, center, rect, uv);
                }
                else {
                    MeshUtility.AddSliced(vh, color, center, rect, uv, uvInner, middle.border);
                }
            }
            width = rectTransform.rect.width * 0.5f - width;
            uv = DataUtility.GetOuterUV(left);
            uvInner = DataUtility.GetInnerUV(left);
            rect = new Vector4(-width, -height, 0, height);
            center = _center - offset;
            if (uv == uvInner) {
                MeshUtility.AddSimple(vh, color, center, rect, uv);
            }
            else {
                MeshUtility.AddSliced(vh, color, center, rect, uv, uvInner, left.border);
            }
            uv = DataUtility.GetOuterUV(right);
            uvInner = DataUtility.GetInnerUV(right);
            rect = new Vector4(0, -height, width, height);
            center = _center + offset;
            if (uv == uvInner) {
                MeshUtility.AddSimple(vh, color, center, rect, uv);
            }
            else {
                MeshUtility.AddSliced(vh, color, center, rect, uv, uvInner, right.border);
            }
        }
    }
}