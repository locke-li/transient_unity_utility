using UnityEngine.UI;
using UnityEngine;

namespace Transient.UI {
    //NOTE not working with CanvasGroup.BlocksRaycasts
    public sealed class TransparentCircle : Graphic {
        [Range(0, 1)]
        public float diameter = 1;
#pragma warning disable 0649
        [SerializeField]
        private float radiusSqr;
#pragma warning restore 0649

#if UNITY_EDITOR
        protected override void OnValidate() {
            var size = rectTransform.rect;
            radiusSqr = diameter * 0.5f * Mathf.Min(size.width, size.height);
            radiusSqr *= radiusSqr;
        }
#endif

        public override bool Raycast(Vector2 sp, Camera eventCamera) {
            if (!raycastTarget) return false;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, sp, eventCamera, out var point);
            var diff = rectTransform.rect.center - point;
            return diff.sqrMagnitude <= radiusSqr;
        }

        protected override void OnPopulateMesh(VertexHelper vh) {
            vh.Clear();
        }
    }
}