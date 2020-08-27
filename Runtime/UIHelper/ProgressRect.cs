using System.Collections;
using System.Collections.Generic;
using Transient;
using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace Transient.UI {
    public struct ProgressRect {
        public RectTransform obj;
        public float min;
        public float flex;

        public void Init(Image image) {
            obj = image.rectTransform;
            var size = DataUtility.GetMinSize(image.sprite);
            min = size.x;
            flex = obj.sizeDelta.x - min;
            //Log.Debug($"{min} {flex}");
        }

        public void Resize(float percent) {
            var current = obj.sizeDelta;
            obj.sizeDelta = new Vector2(min + flex * percent, current.y);
        }

        public void Resize(float a, float b) {
            var current = obj.sizeDelta;
            obj.sizeDelta = new Vector2(min + flex * a / b, current.y);
        }
    }
}