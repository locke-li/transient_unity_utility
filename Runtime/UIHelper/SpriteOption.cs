using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Transient.UI {
    public class SpriteOption : MonoBehaviour {
        public Sprite[] option;
        public Image target;

#if UNITY_EDITOR
        public void OnValidate() {
            target = GetComponent<Image>();
        }
#endif

        public void Select(int index) {
            if (target == null) return;
            target.sprite = option[index];
        }

        public void SelectFor(int index, Image image) {
            image.sprite = option[index];
        }
    }
}