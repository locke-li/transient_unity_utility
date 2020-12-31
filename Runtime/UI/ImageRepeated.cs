using UnityEngine.Sprites;
using UnityEngine;
using UnityEngine.UI;
using Transient.SimpleContainer;

namespace Transient.UI {
    public class ImageRepeated : MaskableGraphic {
        public Sprite sprite;
        public float spacing;
        public float count;
        public override Texture mainTexture => sprite == null ? base.mainTexture : sprite.texture;

#if UNITY_EDITOR
        protected override void OnValidate() {
            count = Mathf.Max(count, 0);
            base.OnValidate();
        }
#endif

        public void Refresh(float count_) {
            if (Mathf.Abs(count - count_) < 0.01f) return;
            count = count_;
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
            var basePos = TransformUtility.CheckOffsetBiased(rectTransform, spacing, count);
            var countInteger = Mathf.FloorToInt(count);
            for (int e = 0; e < countInteger; ++e) {
                var pos = basePos + new Vector2(e * width, 0f);
                MeshUtility.AddSimple(vh, Color.white, pos, size, uv);
            }
            var countFract = count - countInteger;
            if (countFract > 0) {
                var posLast = basePos + new Vector2(countInteger * width, 0f);
#if SCALED_FRACTIONAL
                posLast.x += width * countFract * 0.5f;
                MeshUtility.AddSimple(vh, Color.white, posLast, size * countFract, uv);
#else //clipped factional
                size.z *= countFract;
                uv.z = uv.x + (uv.z - uv.x) * countFract;
                MeshUtility.AddSimple(vh, Color.white, posLast, size, uv);
#endif
            }
        }
    }
}