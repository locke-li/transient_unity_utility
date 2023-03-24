using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

namespace Transient.UI {
    public interface IOptionState {
        void Select(int v_);
    }

    public class StateGroup : MonoBehaviour, IOptionState {
        public IOptionState[] state;

        public void Select(int i_) {
            foreach(var s in state) {
                s.Select(i_);
            }
        }

        public void Enabled(bool v_) {
            var i = v_ ? 0 : 1;
            Select(i);
        }
    }
}