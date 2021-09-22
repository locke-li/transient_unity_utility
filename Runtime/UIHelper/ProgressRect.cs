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
        public float value;

        public void Init(Image image, float min_ = -1) {
            obj = image.rectTransform;
            min = min_ < 0 ? DataUtility.GetMinSize(image.sprite).x : min_;
            flex = obj.sizeDelta.x - min;
            if (flex <= 0) {
                Log.Warning("flex size too small");
            }
        }

        public void Resize(float percent) {
            value = percent;
            var current = obj.sizeDelta;
            obj.sizeDelta = new Vector2(min + flex * percent, current.y);
        }

        public Vector2 Calculate(float percent) {
            value = percent;
            var current = obj.sizeDelta;
            return new Vector2(min + flex * percent, current.y);
        }

        public void Resize(float a, float b) => Resize(a / b);
    }
}