using System.Collections.Generic;
using System;
using Transient.Container;

namespace Transient {
    using Enumerator = IEnumerator<CoroutineState>;

    public enum CoroutineState {
        Init,
        Waiting,
        Executing,
        Done
    }

    internal struct CoroutineCache {
        public Enumerator enumerator;

        public CoroutineState Step() {
            if (enumerator.MoveNext()) {
                return enumerator.Current;
            }
            return CoroutineState.Done;
        }
    }

    public class GeneratorCoroutine {
        private List<CoroutineCache> CoroutineList { get; set; } = new List<CoroutineCache>(16);

        public GeneratorCoroutine(ActionList<float> updater) {
            updater.Add(Update, this);
        }

        public void Execute(Enumerator enumerator_) {
            CoroutineList.Add(new CoroutineCache() {
                enumerator = enumerator_,
            });
        }

        public void Execute(Func<Enumerator> routine_) {
            CoroutineList.Add(new CoroutineCache() {
                enumerator = routine_(),
            });
        }

        public void Execute<P>(Func<P, Enumerator> routine_, P parameter_) {
            CoroutineList.Add(new CoroutineCache() {
                enumerator = routine_(parameter_),
            });
        }

        private void Update(float deltaTime) {
            for (int r = 0; r < CoroutineList.Count; ++r) {
                var state = CoroutineList[r].Step();
                if (state == CoroutineState.Done) {
                    CoroutineList.OutOfOrderRemoveAt(r);
                    --r;
                }
            }
        }

        public void Clear() {
            CoroutineList.Clear();
        }
    }
}