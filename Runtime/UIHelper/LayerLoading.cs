using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Transient.UI {
    public class LayerLoading : Layer {
        private ProgressRect progress;
        private float start;
        private float range;
        public float Value => progress.value;
        public TextMeshProUGUI Version { get; private set; }

        public void Init(string path, Transform parent, string progress_, float progressMin_, string version_) {
            if(Asset == null) InitAsset(path, true);
            Asset.SetParent(parent, false);
            progress.Init(Asset.FindChecked<Image>(progress_), progressMin_);
            Version = Asset.FindChecked<TextMeshProUGUI>(version_);
        }

        public override bool Activate() {
            Reset();
            return base.Activate();
        }

        public void Reset() {
            start = 0;
            range = 0;
            progress.Resize(0);
        }

        public void Target(float value_) {
            Log.Assert(value_ > start, "invalid progress target {0} < {1}", value_, start);
            if (value_ > 1) {
                Log.Warn($"progress exceeding 1 {value_}");
                value_ = 1;
            }
            start += range;
            range = value_ - start;
        }

        public void Progress(float value_) {
            value_ = start + range * value_;
#if DEBUG
            if (value_ < progress.value) {
                Log.Warn($"progress rolling back {value_} {progress.value}");
            }
#endif
            progress.Resize(value_);
        }
    }
}