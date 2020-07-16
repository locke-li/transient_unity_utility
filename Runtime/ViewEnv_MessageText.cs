using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Transient {
    public interface IMessageText {
        Transform Root { get; }
        string Text { get; set; }
        Color Color { get; set; }
        bool Init(GameObject obj);
    }

    public struct TextUGUI : IMessageText {
        public Transform Root { get; set; }
        public Text Content { get; set; }

        public string Text {
            get => Content.text;
            set => Content.text = value;
        }
        public Color Color {
            get => Content.color;
            set => Content.color = value;
        }

        public bool Init(GameObject obj) {
            Root = obj?.transform;
            Content = obj?.FindChecked<Text>("message");
            return Content != null;
        }
    }

    public struct TextTMPro : IMessageText {
        public Transform Root { get; set; }
        public TextMeshProUGUI Content { get; set; }

        public string Text {
            get => Content.text;
            set => Content.text = value;
        }
        public Color Color {
            get => Content.color;
            set => Content.color = value;
        }

        public bool Init(GameObject obj) {
            Root = obj?.transform;
            Content = obj?.FindChecked<TextMeshProUGUI>("message");
            return Content != null;
        }
    }
}
