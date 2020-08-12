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

    public class MessageFade<M> : IMessageFade where M : struct, IMessageText {
        private readonly Queue<M> _message;
        private readonly Transform _parent;
        public Vector3 StartPos { get; set; } = new Vector3(0, -50, 0);
        public Vector3 EndPos { get; set; } = new Vector3(0, 550f, 0);
        public Vector3 Velocity { get; set; } = new Vector3(0, 240f, 0);
        private string Asset { get; set; }

        public MessageFade(Transform parent_) {
            _parent = parent_;
            _message = new Queue<M>(64, 4);
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
            var message = new M();
            message.Init(obj);
            message.Text = m;
            message.Color = c;
            _message.Enqueue(message);
        }

        public void Fade() {
            float start, end = 0f;
            for (int k = 0; k < _message.Count; ++k) {
                var l = _message.RawIndex(k);
                var transform = (RectTransform)_message.Data[l].Root;
                transform.localPosition += Velocity * Time.deltaTime;
                var height = transform.sizeDelta.y * 0.5f;
                start = transform.anchoredPosition.y - height;
                if (start < end) {
                    transform.anchoredPosition += new Vector2(0f, end - start);
                }
                end = transform.anchoredPosition.y + height;
                float p = (transform.localPosition.y - StartPos.y) / (EndPos.y - StartPos.y);
                var color = _message.Data[l].Color;
                color.a = 1.25f - p;
                _message.Data[l].Color = color;
            }
            while (_message.Count > 0 && _message.Peek().Root.localPosition.y >= EndPos.y) {
                var m = _message.Dequeue();
                AssetMapping.Default.Recycle(m.Root.gameObject);
                m.Recycle();
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