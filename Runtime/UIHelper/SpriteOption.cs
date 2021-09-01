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
        [Min(0)][SerializeField]
        internal int preview;

        internal void OnValidate() {
            var init = target == null;
            target = GetComponent<Image>();
            if (init) {
                option = new Sprite[] {
                    target.sprite
                };
            }
            if (target == null || option == null) return;
            preview = Mathf.Min(preview, option.Length - 1);
            Select(preview);
        }
#endif

        public void Select(int index) {
            if (target == null) return;
            SelectFor(index, target);
        }

        public void SelectWithState(int index, bool interactable) {
            if (target == null) return;
            SelectFor(index, target);
            target.raycastTarget = interactable;
        }

        public void SelectFor(int index, Image image) {
            image.sprite = option[index];
            if (nativeSize) image.SetNativeSize();
        }
    }
}