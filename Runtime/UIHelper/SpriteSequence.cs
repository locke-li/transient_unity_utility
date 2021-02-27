using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Transient.UI {
    public class SpriteSequence : MonoBehaviour {
        public SpriteOption target;
        private bool loopSequence;
        private float interval;
        private float time;
        public int index;
        private bool animating;

#if UNITY_EDITOR
        internal void OnValidate() {
            target = GetComponent<SpriteOption>();
            if (target != null) {
                target.Select(index);
            }
        }
#endif

        public void Play(float interval_, bool loop_) {
            if (target == null
                || target.target == null
                || target.option.Length < 2)
                return;
            loopSequence = loop_;
            interval = interval_;
            time = 0;
            index = 0;
            target.Select(index);
            if (animating) return;
            animating = true;
            MainLoop.OnUpdate.Add(Step, this);
        }

        public void Stop() {
            MainLoop.OnUpdate.Remove(this);
            animating = false;
        }

        public void Step(float deltaTime_) {
            time += deltaTime_;
            if (time > interval) {
                time -= interval;
                ++index;
                if (index >= target.option.Length) {
                    if (loopSequence) index = 0;
                    else {
                        Stop();
                        return;
                    }
                }
                target.Select(index);
            }
        }
    }
}