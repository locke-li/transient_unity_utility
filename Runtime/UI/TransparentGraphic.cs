using UnityEngine.UI;
using UnityEngine;

namespace Transient.UI {
    //NOTE not working with CanvasGroup.BlocksRaycasts
    public sealed class TransparentGraphic : Graphic {
        public override bool Raycast(Vector2 sp, Camera eventCamera) {
            return true;
        }

        protected override void OnPopulateMesh(VertexHelper vh) {
            vh.Clear();
        }
    }
}