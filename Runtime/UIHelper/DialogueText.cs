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

        public void Animate(string text_, Action WhenStop_, bool append_) {
            text = text_;
            WhenStop = WhenStop_;
            if (!append_) builder.Clear();
            interval = 0;
            index = -1;
            //TODO prevent multiple animate
            MainLoop.OnUpdate.Add(AnimateUpdateDelegate, this);
        }

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
            line.text = text;
            builder.Clear();
            MainLoop.OnUpdate.Remove(this);
            WhenStop?.Invoke();
            var ret = index >= text.Length - 1;
            index = text.Length;
            return ret;
        }
    }
}