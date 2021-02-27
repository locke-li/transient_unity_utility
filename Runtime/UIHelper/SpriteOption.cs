using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Transient.UI {
    public class SpriteOption : MonoBehaviour {
        public Sprite[] option;
        public Image target;
        public bool nativeSize;

#if UNITY_EDITOR
        internal void OnValidate() {
            target = GetComponent<Image>();
        }
#endif

        public void Select(int index) {
            if (target == null) return;
            SelectFor(index, target);
        }

        public void SelectFor(int index, Image image) {
            image.sprite = option[index];
            if (nativeSize) image.SetNativeSize();
        }
    }
}