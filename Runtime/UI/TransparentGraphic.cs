using UnityEngine.UI;
using UnityEngine;

namespace Transient.UI {
    //NOTE not working with CanvasGroup.BlocksRaycasts
    public sealed class TransparentGraphic : Graphic, ICanvasRaycastFilter {
        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera) {
            return raycastTarget;
        }

        public override bool Raycast(Vector2 sp, Camera eventCamera) {
            return raycastTarget;
        }

        protected override void OnPopulateMesh(VertexHelper vh) {
            vh.Clear();
        }
    }
}