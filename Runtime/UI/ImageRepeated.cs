using UnityEngine.Sprites;
using UnityEngine;
using UnityEngine.UI;
using Transient.SimpleContainer;

namespace Transient.UI {
    public class ImageRepeated : Graphic {
        public Sprite sprite;
        public float spacing;
        public int count;
        public override Texture mainTexture => sprite == null ? base.mainTexture : sprite.texture;

#if UNITY_EDITOR
        protected override void OnValidate() {
            count = Mathf.Max(count, 1);
            base.OnValidate();
        }
#endif

        public void Refresh(int count_) {
            if (count == count_) return;
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
            for (int e = 0; e < count; ++e) {
                var pos = basePos + new Vector2(e * width, 0f);
                MeshUtility.AddSimple(vh, Color.white, pos, size, uv);
            }
        }
    }
}