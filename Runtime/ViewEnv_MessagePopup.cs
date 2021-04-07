using UnityEngine;
using Transient.DataAccess;
using Transient.UI;
using System;

namespace Transient {
    public interface IMessagePopup {
        void Create(string m, Action Confirm_, Action Cancel_, PopupOption option);
        void Clear(bool manual_ = false);
    }

    public struct PopupOption {
        public bool keepOpen;
        public bool blockIsCancel;
        public Action<RectTransform> Modify;
        public Transform sync;
    }

    public class MessagePopup<M> : IMessagePopup where M : IMessageText, new() {
        private GameObject _obj;
        private RectTransform _content;
        private Vector2 _defaultPosition;
        private M _message;
        private PopupOption _option;
        private ButtonReceiver _confirm;
        private ButtonReceiver _cancel;
        private Action _OnConfirm;
        private Action _OnCancel;
        private Vector2 _positionBoth;
        private Vector2 _positionSingle;

        public static MessagePopup<M> TryCreate(string asset_) {
            var key = "MessagePopup.Create";
            Performance.RecordProfiler(key);
            MessagePopup<M> ret;
            try {
                var obj = AssetMapping.View.TakePersistent<GameObject>(asset_);
                var trans = obj.transform;
                trans.SetParent(ViewEnv.CanvasOverlay, false);
                RectTransform content = trans.FindChecked<RectTransform>(nameof(content));
                var message = new M();
                message.Init(content.gameObject);
                ButtonReceiver confirm = content.FindChecked<ButtonReceiver>(nameof(confirm));
                ButtonReceiver cancel = content.TryFind<ButtonReceiver>(nameof(cancel));
                ButtonReceiver block = trans.FindChecked<ButtonReceiver>(nameof(block));
                var confirmPos = confirm.transform.localPosition;
                ret = new MessagePopup<M>() {
                    _obj = obj,
                    _content = content,
                    _defaultPosition = content.anchoredPosition,
                    _message = message,
                    _confirm = confirm,
                    _cancel = cancel,
                    _positionBoth = confirmPos,
                    _positionSingle = confirmPos,
                };
                confirm.WhenClick = b => {
                    ret._OnConfirm?.Invoke();
                    ret.Clear();
                };
                if(cancel != null) {
                    cancel.WhenClick = b => {
                        ret._OnCancel?.Invoke();
                        ret.Clear();
                    };
                    ret._positionSingle = (confirmPos + cancel.transform.localPosition) * 0.5f;
                }
                block.WhenClick = b => {
                    if (ret._option.blockIsCancel) {
                        ret._OnCancel?.Invoke();
                        ret.Clear();
                    }
                };
                obj.SetActive(false);
            }
            catch (Exception e) {
                Log.Warning($"{nameof(MessagePopup<M>)} create failed. {e.Message}");
                ret = null;
            }
            Performance.End(key);
            return ret;
        }

        public void Create(string m, Action Confirm_, Action Cancel_, PopupOption option_) {
            Performance.RecordProfiler(nameof(MessagePopup<M>));
            _OnConfirm = Confirm_;
            _OnCancel = Cancel_;
            if (Cancel_ == null) {
                UnityExtension.TrySetActive(_cancel, false);
                _confirm.transform.localPosition = _positionSingle;
            }
            else {
                UnityExtension.TrySetActive(_cancel, true);
                _confirm.transform.localPosition = _positionBoth;
            }
            _message.Text = string.IsNullOrEmpty(m) ? "<no message>" : m.Replace("\\n", "\n");
            option_.Modify?.Invoke(_content);
            option_.Modify = null;
            _obj.SetActive(true);
            if (option_.sync != null) {
                MainLoop.OnUpdate.Add(_ => Sync(option_.sync.position), this);
            }
            _option = option_;
            Performance.End(nameof(MessagePopup<M>));
        }

        public void Clear(bool manual_ = false) {
            if (!manual_ && _option.keepOpen) return;
            if (_option.sync != null) {
                MainLoop.OnUpdate.Remove(this);
            }
            _obj.SetActive(false);
            _content.anchoredPosition = _defaultPosition;
            _OnConfirm = null;
            _OnCancel = null;
        }

        public void Sync(Vector3 position) {
            _content.localPosition = ViewEnv.WorldToCanvasSpace(position);
        }
    }
}