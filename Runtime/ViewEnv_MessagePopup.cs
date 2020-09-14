using UnityEngine;
using Transient.DataAccess;
using Transient.UI;
using System;

namespace Transient {
    public interface IMessagePopup {
        void Create(string m, Action Confirm_, Action Cancel_, bool blockIsCancel, Action<RectTransform> Modify_);
        void Sync(Vector3 position);
    }

    public class MessagePopup<M> : IMessagePopup where M : IMessageText, new() {
        private GameObject _obj;
        private RectTransform _content;
        private Vector2 _defaultPosition;
        private M _message;
        private bool _blockIsCancel = false;
        private ButtonReceiver _confirm;
        private ButtonReceiver _cancel;
        private Action _OnConfirm;
        private Action _OnCancel;
        private Vector2 _positionBoth;
        private Vector2 _positionSingle;

        public static MessagePopup<M> TryCreate(string asset_, Transform parent_) {
            try {
                var obj = AssetMapping.View.TakePersistent<GameObject>(null, asset_);
                var trans = obj.transform;
                trans.SetParent(parent_, false);
                RectTransform content = trans.FindChecked<RectTransform>(nameof(content));
                var message = new M();
                message.Init(content.gameObject);
                ButtonReceiver confirm = content.FindChecked<ButtonReceiver>(nameof(confirm));
                ButtonReceiver cancel = content.TryFind<ButtonReceiver>(nameof(cancel));
                ButtonReceiver block = trans.FindChecked<ButtonReceiver>(nameof(block));
                var confirmPos = confirm.transform.localPosition;
                var ret = new MessagePopup<M>() {
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
                    ret._OnConfirm();
                    ret.Clear();
                };
                if(cancel != null) {
                    cancel.WhenClick = b => {
                        ret._OnCancel();
                        ret.Clear();
                    };
                    ret._positionSingle = (confirmPos + cancel.transform.localPosition) * 0.5f;
                }
                block.WhenClick = b => {
                    if (ret._blockIsCancel) {
                        ret._OnCancel();
                        ret.Clear();
                    }
                };
                
                obj.SetActive(false);
                return ret;
            }
            catch (Exception e) {
                Log.Warning($"{nameof(MessagePopup<M>)} create failed. {e.Message}");
            }
            return null;
        }

        public void Create(string m, Action Confirm_, Action Cancel_, bool blockIsCancel, Action<RectTransform> Modify_) {
            Performance.RecordProfiler(nameof(MessagePopup<M>));
            _OnConfirm = Confirm_;
            _OnCancel = Cancel_;
            _blockIsCancel = blockIsCancel;
            if (Cancel_ == null) {
                UnityExtension.TrySetActive(_cancel, false);
                _confirm.transform.localPosition = _positionSingle;
            }
            else {
                UnityExtension.TrySetActive(_cancel, true);
                _confirm.transform.localPosition = _positionBoth;
            }
            _message.Text = m.Replace("\\n", "\n");
            if (Modify_ != null) {
                Modify_(_content);
            }
            _obj.SetActive(true);
            Performance.End(nameof(MessagePopup<M>));
        }

        public void Clear() {
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