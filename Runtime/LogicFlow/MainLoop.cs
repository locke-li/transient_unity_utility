//
// Unity主循环=Update的接口
//
// author: liyingbo
//

using UnityEngine;

namespace Transient {
    public sealed class MainLoop : MonoBehaviour {
        public static MainLoop Instance { get; private set; }
        public static ActionList<float> OnUpdate { get; private set; }
        public static GeneratorCoroutine Coroutine { get; private set; }

        public static void Init() {
            Performance.RecordProfiler(nameof(MainLoop));
            var go = new GameObject("MainLoop");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<MainLoop>();
            OnUpdate = new ActionList<float>(16);
            Log.Info("MainLoop initialized");
            Performance.End(nameof(MainLoop));
        }

        public static void InitCoroutine() {
            Log.Assert(Instance != null, "MainLoop is not initialized yet!");
            Coroutine = new GeneratorCoroutine(OnUpdate);
        }

        private void Update() {
            var deltaTime = Time.deltaTime;
            OnUpdate.Invoke(deltaTime);
        }

        public static void Clear() {
            OnUpdate?.Clear();
            //TODO stop & clear coroutine
        }
    }
}
