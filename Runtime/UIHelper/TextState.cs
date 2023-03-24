using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

namespace Transient.UI {
    public class TextState : MonoBehaviour, IOptionState {
        [Serializable]
        public struct State {
            public bool hide;
            public Color color;
            public Color outline;
        }

        public TextMeshProUGUI text;
        public State[] state;

        public string Text {
            get => text.text;
            set => text.text = value;
        }

        #if UNITY_EDITOR

        private void OnValidate() {
            if (UnityEditor.EditorApplication.isPlaying) return;
            if (state == null || state.Length == 0) {
                state = new State[2];
                state[1].color = new Color(0.4f, 0.4f, 0.4f, 1f);
            }
            text ??= GetComponent<TextMeshProUGUI>();
            if (text != null) {
                state[0].color = text.faceColor;
                state[0].outline = text.outlineColor;
            }
        }

        #endif

        void IOptionState.Select(int i_) => Setup(i_);
        public void Setup(int i_, string text_ = null)
        {
            var s = state[i_];
            text.gameObject.SetActive(!s.hide);
            if (s.hide) return;
            if (text != null) {
                text.faceColor = s.color;
                text.outlineColor = s.outline;
                if (text_ != null) Text = text_;
            }
        }

        public void Enabled(bool v_, string text_ = null) {
            var i = v_ ? 0 : 1;
            Setup(i, text_);
        }
    }
}