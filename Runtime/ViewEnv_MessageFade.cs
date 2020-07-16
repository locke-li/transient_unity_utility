using UnityEngine;
using Transient.DataAccess;
using Transient.SimpleContainer;
using TMPro;
using UnityEngine.UI;

namespace Transient {
    public interface IMessageFade {
        void Create(string m, Color c);
        void Fade();
        void Clear();
    }

    public class MessageFade<M> : IMessageFade where M : IMessageText, new() {
        private readonly List<M> _message;
        private readonly Transform _parent;
        public Vector3 StartPos { get; set; } = new Vector3(0, -50, 0);
        public Vector3 EndPos { get; set; } = new Vector3(0, 550f, 0);
        public Vector3 Velocity { get; set; } = new Vector3(0, 240f, 0);
        private string Asset { get; set; }

        public MessageFade(Transform parent_) {
            _parent = parent_;
            _message = new List<M>(64);
        }

        public static MessageFade<M> TryCreate(string asset_, Transform parent_) {
            var obj = AssetMapping.View.Take<GameObject>(null, asset_, false);
            var message = new M();
            if(!message.Init(obj)) {
                Log.Warning($"{nameof(MessageFade<M>)} create failed.");
                return null;
            }
            return new MessageFade<M>(parent_) {
                Asset = asset_
            };
        }

        public void Create(string m, Color c) {
            var obj = AssetMapping.View.Take<GameObject>(null, Asset, true);
            obj.transform.SetParent(_parent, false);
            obj.transform.localPosition = StartPos;
            obj.gameObject.SetActive(true);
            var message = new M();
            message.Init(obj);
            message.Text = m;
            message.Color = c;
            _message.Add(message);
        }

        public void Fade() {
            for (int k = 0; k < _message.Count; ++k) {
                var m = _message[k];
                m.Root.localPosition += Velocity * Time.deltaTime;
                float p = (m.Root.localPosition.y - StartPos.y) / (EndPos.y - StartPos.y);
                if (p >= 1) {
                    _message.OutOfOrderRemoveAt(k);
                    AssetMapping.View.Recycle(m.Root.gameObject);
                    --k;
                    continue;
                }
                var color = m.Color;
                color.a = 1 - p + 0.25f;
                m.Color = color;
            }
        }

        public void Clear() {
            foreach (var m in _message) {
                AssetMapping.View.Recycle(m.Root.gameObject);
            }
            _message.Clear();
        }
    }
}