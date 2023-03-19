using UnityEngine;
using UnityEngine.UI;
using System;

namespace Transient.UI {
    public class ImageState : MonoBehaviour {
        [Serializable]
        public struct State {
            public bool enabled;
            public Sprite sprite;
            public Color color;
        }

        public Image image;
        public State[] state;

        #if UNITY_EDITOR

        private void OnValidate() {
            if (UnityEditor.EditorApplication.isPlaying) return;
            if (state == null || state.Length == 0) {
                state = new State[2];
                state[0].enabled = true;
                state[1].color = new Color(0.4f, 0.4f, 0.4f, 1f);
                state[1].enabled = false;
            }
            image = image ?? transform.GetComponent<Image>();
            if (image != null) {
                state[0].enabled = image.enabled;
                state[0].sprite = image.sprite;
                state[0].color = image.color;
            }
        }

        #endif

        public void Setup(int i_) {
            var s = state[i_];
            image.enabled = s.enabled;
            if (!s.enabled) return;
            image.sprite = s.sprite;
            image.color = s.color;
        }

        public void Enabled(bool v_) {
            var i = v_ ? 0 : 1;
            Setup(i);
        }
    }
}