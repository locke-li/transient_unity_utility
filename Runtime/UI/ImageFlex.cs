using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Transient.UI {
    public class ImageFlex : Image {
        public bool hidden;

        public void Hide(bool value) {
            if (hidden == value) return;
            hidden = value;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh) {
            if (hidden) {
                vh.Clear();
                return;
            }
            base.OnPopulateMesh(vh);
        }
    }
}