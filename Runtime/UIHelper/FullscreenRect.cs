using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace Transient.UI {
    public class FullscreenRect : MonoBehaviour {
        public void Start() {
            CheckFit();
        }

        public void CheckFit() {
            if (transform is RectTransform rect) {
                //TODO consider notch area
                var anchor = Vector2.one * 0.5f;
                rect.anchorMin = anchor;
                rect.anchorMax = anchor;
                var size = rect.sizeDelta;
                var ratioRect = size.x / size.y;
                var ratioActual = (float)Screen.width / Screen.height;
                if (ratioRect < ratioActual) {//screen is wider, stretch width
                    rect.sizeDelta = new Vector2(size.x * ratioActual / ratioRect, size.y);
                }
            }
        }
    }
}