using UnityEngine;
using Transient.DataAccess;
using Transient.SimpleContainer;
using TMPro;
using UnityEngine.UI;

namespace Transient {
    public interface IMessageFade {
        void Create(string m, Color c);
        void Fade(float deltaTime);
        void Clear();
    }

    public class MessageFade<M> : IMessageFade where M : struct, IMessageText {
        private readonly Queue<M> _message;
        private Vector3 _startPos;
        private string Asset { get; set; }
        private Color _templateColor;
        public Func<int, float, float, float> AnimateFade { get; set; }
        public Func<M, bool> RecycleCheck { get; set; }

        public MessageFade() {
            _message = new Queue<M>(64, 4);
        }

        public static MessageFade<M> TryCreate(string asset_) {
            var key = "MessageFade.Create";
            Performance.RecordProfiler(key);
            var obj = AssetMapping.View.TakeActive(asset_);
            var message = new M();
            if(!message.Init(obj)) {
                Log.Warning($"{nameof(MessageFade<M>)} create failed.");
                AssetMapping.View.Recycle(obj);
                return null;
            }
            AssetMapping.View.Recycle(obj);
            Performance.End(key);
            return new MessageFade<M>() {
                Asset = asset_,
                _templateColor = message.Color,
            };
        }

        public void Create(string m, Color c) {
            Performance.RecordProfiler(nameof(MessageFade<M>));
            var obj = AssetMapping.View.TakeActive(Asset);
            obj.transform.SetParent(ViewEnv.CanvasOverlay, false);
            var message = new M();
            message.Init(obj);
            message.Text = m;
            message.Color = c == Color.clear ? _templateColor : c;
            message.Root.localPosition = _startPos;
            _message.Enqueue(message);
            Performance.End(nameof(MessageFade<M>), true);
        }

        private float CheckOverlap(RectTransform transform, float end_) {
            var height = transform.sizeDelta.y * 0.5f;
            var start_ = transform.anchoredPosition.y - height;
            if (start_ < end_) {
                transform.anchoredPosition += new Vector2(0f, end_ - start_);
            }
            end_ = transform.anchoredPosition.y + height;
            return end_;
        }

        public MessageFade<M> FadeLinearUp() {
            return FadeLinearMove(
                new Vector3(0, -50, 0),
                new Vector3(0, 550f, 0),
                new Vector3(0, 240f, 0),
                2
            );
        }

        public MessageFade<M> FadeLinearMove(Vector3 start_, Vector3 end_, Vector3 velocity_, float delay_) {
            _startPos = start_;
            AnimateFade = (k_, deltaTime_, elementEnd_) => {
                k_ = _message.RawIndex(k_);
                var data = _message.Data[k_];
                var transform = (RectTransform)data.Root;
                if (data.Time < delay_) {
                    _message.Data[k_].Time = data.Time + deltaTime_;
                    return CheckOverlap(transform, elementEnd_);
                }
                transform.localPosition += velocity_ * deltaTime_;
                var p = (transform.localPosition.y - start_.y) / (end_.y - start_.y);
                var c = data.Color;
                c.a = 1.25f - p;
                data.Color = c;
                return CheckOverlap(transform, elementEnd_);
            };
            RecycleCheck = m => m.Root.localPosition.y >= end_.y;
            return this;
        }

        public void Fade(float deltaTime_) {
            var end = float.MinValue;
            for (int k = _message.Count - 1; k >= 0; --k) {
                end = AnimateFade(k, deltaTime_, end);
            }
            while (_message.Count > 0 && RecycleCheck(_message.Peek())) {
                var m = _message.Dequeue();
                AssetMapping.View.Recycle(m.Root.gameObject);
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