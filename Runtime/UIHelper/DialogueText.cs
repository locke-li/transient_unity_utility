using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using System.Text;
using Transient;

namespace Transient.UI {
    public class DialogueText : MonoBehaviour {
        public TextMeshProUGUI line;
        public string text;
        public float speed = 20;//char per second
        public float linePause;//second
        private float interval;
        private int index;
        private readonly StringBuilder builder = new StringBuilder();
        private Action<float> AnimateUpdateDelegate;
        private Action WhenStop;

        private void Awake() {
            AnimateUpdateDelegate = AnimateUpdate;
        }

        public void Animate(string text_, Action WhenStop_, string prepend_ = null, float delay_ = 0, bool appendLine_ = false) {
            builder.Clear();
            builder.Append(prepend_);
            if (appendLine_) {
                builder.AppendLine();
                delay_ += speed * linePause;
            }
            text = text_;
            WhenStop = WhenStop_;
            interval = delay_;
            index = -1;
            //TODO prevent multiple animate
            MainLoop.OnUpdate.Add(AnimateUpdateDelegate, this);
        }
        public void AnimateAppend(string text_, Action WhenStop_, float delay_ = 0, bool appendLine_ = false)
            => Animate(text_, WhenStop_, text, delay_, appendLine_);

        public void AnimateUpdate(float deltaTime_) {
            interval -= speed * deltaTime_;
            if (interval <= 0) {
                interval += 1;
                if (index >= text.Length - 1) {
                    Stop();
                    return;
                }
                builder.Append(text[++index]);
                var lastChar = text[index];
                if (lastChar == '\n' || lastChar == '\r') {
                    interval += speed * linePause;
                }
                line.text = builder.ToString();
            }
        }

        public bool Stop() {
            line.text = builder.ToString();
            builder.Clear();
            MainLoop.OnUpdate.Remove(this);
            var ret = index >= text.Length - 1;
            index = text.Length;
            WhenStop?.Invoke();
            return ret;
        }
    }
}