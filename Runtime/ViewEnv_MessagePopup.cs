using UnityEngine;
using Transient.DataAccess;
using Transient.UI;
using System;

namespace Transient {
    public interface IMessagePopup {
        void Create(string m, Action Confirm_, Action Cancel_);
    }

    public class MessagePopup<M> : IMessagePopup where M : IMessageText, new() {
        private GameObject _obj;
        private M _message;
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
                Transform content = trans.FindChecked<Transform>(nameof(content));
                var message = new M();
                message.Init(content.gameObject);
                ButtonReceiver confirm = content.FindChecked<ButtonReceiver>(nameof(confirm));
                ButtonReceiver cancel = content.FindChecked<ButtonReceiver>(nameof(cancel));
                var confirmPos = confirm.transform.localPosition;
                var cancelPos = cancel.transform.localPosition;
                var ret = new MessagePopup<M>() {
                    _obj = obj,
                    _message = message,
                    _confirm = confirm,
                    _cancel = cancel,
                    _positionBoth = confirmPos,
                    _positionSingle = (confirmPos + cancelPos) * 0.5f,
                };
                confirm.WhenClick = b => {
                    ret._OnConfirm();
                    ret.Clear();
                };
                cancel.WhenClick = b => {
                    ret._OnCancel();
                    ret.Clear();
                };
                obj.SetActive(false);
                return ret;
            }
            catch (Exception e) {
                Log.Warning($"{nameof(MessagePopup<M>)} create failed. {e.Message}");
            }
            return null;
        }

        public void Create(string m, Action Confirm_, Action Cancel_) {
            _OnConfirm = Confirm_;
            _OnCancel = Cancel_;
            if (Cancel_ == null) {
                _cancel.gameObject.SetActive(false);
                _confirm.transform.localPosition = _positionSingle;
            }
            else {
                _cancel.gameObject.SetActive(true);
                _confirm.transform.localPosition = _positionBoth;
            }
            _message.Text = m.Replace("\\n", "\n");
            _obj.SetActive(true);
        }

        public void Clear() {
            _obj.SetActive(false);
            _OnConfirm = null;
            _OnCancel = null;
        }
    }
}