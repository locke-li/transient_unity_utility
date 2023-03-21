using UnityEngine;
using Transient.DataAccess;
using System.Collections.Generic;
using System;

namespace Transient {
    public interface IMessageFade {
        Action<RectTransform> ModifyMessage { get; set; }
        void Create(string m, Color c);
        void Fade(float deltaTime);
        void Clear();
    }

    public class MessageFade<M> : IMessageFade where M : struct, IMessageText {
        public class MessageText {
            public M text;
            public float time;
        }
        private readonly Queue<MessageText> _message = new(64);
        private Vector3 _startPos;
        private string Asset { get; set; }
        private Color _templateColor;
        public Func<MessageText, float, float, float> AnimateFade { get; set; }
        public Func<MessageText, bool> RecycleCheck { get; set; }
        public Action<RectTransform> ModifyMessage { get; set; }

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
            ModifyMessage?.Invoke((RectTransform)obj.transform);
            var message = new M();
            message.Init(obj);
            message.Text = m;
            message.Color = c == Color.clear ? _templateColor : c;
            message.Root.localPosition = _startPos;
            _message.Enqueue(new MessageText() { text = message });
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
            AnimateFade = (data_, deltaTime_, elementEnd_) => {
                var transform = (RectTransform)data_.text.Root;
                data_.time += deltaTime_;
                if (data_.time < delay_) {
                    return CheckOverlap(transform, elementEnd_);
                }
                transform.localPosition += velocity_ * deltaTime_;
                var p = (transform.localPosition.y - start_.y) / (end_.y - start_.y);
                var c = data_.text.Color;
                c.a = 1.25f - p;
                data_.text.Color = c;
                return CheckOverlap(transform, elementEnd_);
            };
            RecycleCheck = m => m.text.Root.localPosition.y >= end_.y;
            return this;
        }

        public void Fade(float deltaTime_) {
            var end = float.MinValue;
            foreach(var m in _message) {
                end = AnimateFade(m, deltaTime_, end);
            }
            while (_message.Count > 0 && RecycleCheck(_message.Peek())) {
                var m = _message.Dequeue().text;
                AssetMapping.View.Recycle(m.Root.gameObject);
            }
        }

        public void Clear() {
            foreach (var m in _message) {
                AssetMapping.View.Recycle(m.text.Root.gameObject);
            }
            _message.Clear();
        }
    }
}