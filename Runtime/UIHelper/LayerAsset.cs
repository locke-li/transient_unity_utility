using UnityEngine;
using System.Collections.Generic;
using Transient.DataAccess;

namespace Transient.UI {
    public class LayerAsset {
        public Transform Asset { get; set; }
        private Animation animation;

        public static LayerAsset Create(string path, bool active) => Create<LayerAsset>(path, active);
        public static T Create<T>(string path, bool active) where T : LayerAsset, new() {
            var layer = new T();
            layer.InitAsset(path, active);
            return layer;
        }

        public virtual bool Activate() {
            if (Asset == null) return false;
            Asset.gameObject.SetActive(true);
            return true;
        }

        public void InitAsset(string path, bool active) {
            var obj = AssetMapping.Default.TakePersistent<GameObject>(path);
            if (obj == null) return;
            Asset = obj.transform;
            animation = Asset.GetComponent<Animation>();
            if(!active) {
                obj.SetActive(false);
            }
        }

        public virtual void Close() {
            if (Asset == null) return;
            Asset.gameObject.SetActive(false);
        }

        public void Recycle() {
            if (Asset == null) return;
            GameObject.Destroy(Asset.gameObject);
            Asset = null;
            animation = null;
        }

        public float Animate(string name_) {
            AnimationState state;
            if (animation == null || (state = animation[name_]) == null) return 0;
            animation.Play(name_);
            //TODO when timeScale == 0
            return state.length / Time.timeScale;
        }

        public void AnimateStep(string name_, float percent_) {
            AnimationState state;
            if (animation == null || !animation.isPlaying || (state = animation[name_]) == null) return;
            state.normalizedTime = percent_;
        }

        public void AnimateStop() {
            if (animation == null || !animation.isPlaying) return;
            animation.Stop();
        }
    }
}