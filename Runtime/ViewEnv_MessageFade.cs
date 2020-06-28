using UnityEngine;
using Transient.DataAccess;
using Label = TMPro.TextMeshProUGUI;
using Transient.SimpleContainer;

namespace Transient {
    public class MessageFade {
        private readonly List<Label> _message;
        private readonly Transform _parent;
        public Vector3 StartPos { get; set; } = new Vector3(0, -50, 0);
        public Vector3 EndPos { get; set; } = new Vector3(0, 550f, 0);
        public Vector3 Velocity { get; set; } = new Vector3(0, 240f, 0);
        private string Asset { get; set; }

        public MessageFade(Transform parent_) {
            _parent = parent_;
            _message = new List<Label>(64);
        }

        public static MessageFade TryCreate(string asset_, Transform parent_) {
            var obj = AssetMapping.View.Take<GameObject>(null, asset_, false);
            var message = obj.GetChecked<Label>("message");
            if(obj == null || message == null) {
                Log.Warning($"{nameof(MessageFade)} create failed.");
                return null;
            }
            return new MessageFade(parent_) {
                Asset = asset_
            };
        }

        public void Create(string m, Color c) {
            var obj = AssetMapping.View.Take<GameObject>(null, Asset, true);
            var message = obj.GetChecked<Label>("message");
            message.text = m;
            message.color = c;
            obj.transform.SetParent(_parent, false);
            obj.transform.localPosition = StartPos;
            obj.gameObject.SetActive(true);
            if (!_message.Contains(message)) {
                _message.Add(message);
            }
        }

        public void Fade() {
            float p;
            foreach (var m in _message) {
                if (!m.gameObject.activeSelf) continue;//TODO: micro-optimize with sorting
                m.transform.localPosition += Velocity * Time.deltaTime;
                p = (m.transform.localPosition.y - StartPos.y) / (EndPos.y - StartPos.y);
                if (p >= 1) AssetMapping.View.Recycle(m.gameObject);
                m.color = new Color(m.color.r, m.color.g, m.color.b, 1 - p + 0.25f);
            }
        }

        public void Clear() {
            foreach (var m in _message) {
                if(m.gameObject.activeSelf) AssetMapping.View.Recycle(m.gameObject);
            }
            _message.Clear();
        }
    }
}