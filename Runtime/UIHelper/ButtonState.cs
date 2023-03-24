using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

namespace Transient.UI {
    public class ButtonState : MonoBehaviour, IOptionState {
        [Serializable]
        public struct State {
            public bool hide;
            public bool enabled;
            public Sprite sprite;
        }

        public ButtonReceiver button;
        public Image image;
        public TextState text;
        public State[] state;

        public Action<ButtonReceiver> WhenClick => button.WhenClick;
        public string Text {
            get => text.Text;
            set => text.Text = value;
        }

        #if UNITY_EDITOR

        private void OnValidate() {
            if (UnityEditor.EditorApplication.isPlaying) return;
            if (state == null || state.Length == 0) {
                state = new State[2];
                state[0].enabled = true;
                state[1].enabled = false;
            }
            button = button ?? GetComponent<ButtonReceiver>();
            if (button != null) {
                image = button.image;
                state[0].sprite = image.sprite;
            }
            text = text ?? GetComponentInChildren<TextState>()
                ?? transform.parent.FindChecked<TextState>(name.Replace("Btn", "Text"));
        }

        #endif

        void IOptionState.Select(int i_) => Setup(i_);
        public void Setup(int i_, string text_ = null)
        {
            var s = state[i_];
            button.gameObject.SetActive(!s.hide);
            text?.gameObject.SetActive(!s.hide);
            if (s.hide) return;
            button.interactable = s.enabled;
            image.sprite = s.sprite;
            text?.Setup(i_, text_);
        }

        public void Enabled(bool v_, string text_ = null) {
            var i = v_ ? 0 : 1;
            Setup(i, text_);
        }
    }
}