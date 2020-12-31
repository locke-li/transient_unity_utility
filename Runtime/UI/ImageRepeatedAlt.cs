using UnityEngine.Sprites;
using UnityEngine;
using UnityEngine.UI;

namespace Transient.UI {
    public class ImageRepeatedAlt : Graphic {
        //NOTE must be on the same texture
        public Sprite sprite;
        public Sprite option = null;
        public Vector2 direction = new Vector2(1, 0);
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
            var rect = rectTransform.rect;
            var rectSize = new Vector2(rect.width, rect.height);
            var size = new Vector4(0, 0, rectSize.x, rectSize.y);
            var uv = DataUtility.GetOuterUV(sprite);
            var pivotSize = Vector2.Dot(rectSize, direction) + spacing;
            var basePos = TransformUtility.CheckOffsetBiased(rectTransform, direction, spacing, count);
            int e = 0;
            for (; e < Mathf.Min(value, count); ++e) {
                var pos = basePos + e * pivotSize * direction;
                MeshUtility.AddSimple(vh, Color.white, pos, size, uv);
            }
            if (option == null) return;
            uv = DataUtility.GetOuterUV(option);
            for (; e < count; ++e) {
                var pos = basePos + e * pivotSize * direction;
                MeshUtility.AddSimple(vh, Color.white, pos, size, uv);
            }
        }
    }
}