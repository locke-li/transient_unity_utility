using UnityEngine.Sprites;
using UnityEngine;
using UnityEngine.UI;

namespace Transient.UI {
    public class ImageRepeated : MaskableGraphic {
        public Sprite sprite;
        public Vector2 direction = new Vector2(1, 0);
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
            var rect = rectTransform.rect;
            var rectSize = new Vector2(rect.width, rect.height);
            var size = new Vector4(0, 0, rectSize.x, rectSize.y);
            var uv = DataUtility.GetOuterUV(sprite);
            var pivotSize = Vector2.Dot(rectSize, direction) + spacing;
            var countInteger = Mathf.FloorToInt(count);
            var countFract = count - countInteger;
            var basePos = TransformUtility.CheckOffsetBiased(rectTransform, direction, spacing, count);
            for (int e = 0; e < countInteger; ++e) {
                var pos = basePos + e * pivotSize * direction;
                MeshUtility.AddSimple(vh, Color.white, pos, size, uv);
            }
            if (countFract > 0) {
                var posLast = basePos + countInteger * pivotSize * direction;
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