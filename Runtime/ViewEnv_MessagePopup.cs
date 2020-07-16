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

        public static MessagePopup<M> TryCreate(string asset_, Transform parent_) {
            try {
                var obj = AssetMapping.View.TakePersistent<GameObject>(null, asset_);
                var trans = obj.transform;
                var message = new M();
                message.Init(obj);
                trans.SetParent(parent_, false);
                ButtonReceiver confirm = trans.FindChecked<ButtonReceiver>(nameof(confirm));
                ButtonReceiver cancel = trans.FindChecked<ButtonReceiver>(nameof(cancel));
                var ret = new MessagePopup<M>() {
                    _obj = obj,
                    _message = message,
                    _confirm = confirm,
                    _cancel = cancel
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
            //TODO use auto layout or use configurable value
            if (Cancel_ == null) {
                _cancel.gameObject.SetActive(false);
                _confirm.transform.localPosition = new Vector3(0, -80f, 0);
            }
            else {
                _cancel.gameObject.SetActive(true);
                _confirm.transform.localPosition = new Vector3(-160f, -80f, 0);
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