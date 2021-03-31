using UnityEngine;
using UnityEngine.UI;

namespace Transient.UI {
    public class LayerDisableBlock : Layer {
        private ImageFlex block;
        private ImageFlex busy;
        public ButtonReceiver button;
        public int stack;

        public void Init(string path, Transform parent) {
            InitAsset(path, true);
            Asset.SetParent(parent, false);
            block = Asset.FindChecked<ImageFlex>("image_block");
            button = block.GetChecked<ButtonReceiver>();
            busy = Asset.FindChecked<ImageFlex>("image_busy");
            Clear();
        }

        public void Block(int value_, Color color_) {
            stack += value_;
            if (value_ > 0) {
                block.Hide(false);
                block.color = color_;
            }
            else if(stack <= 0) {
                stack = 0;
                block.Hide(true);
                busy.Hide(true);
                AnimateStop();
            }
        }

        public void Busy() {
            busy.Hide(false);
            Animate("busy_spin");
        }

        public void Clear() {
            stack = 0;
            Block(int.MinValue, Color.clear);
        }
    }
}