//
// Unity主循环=Update的接口
//
// author: liyingbo
//

using UnityEngine;

namespace Transient
{
    public sealed class MainLoop : MonoBehaviour
    {
        public static MainLoop Instance { get; private set; }

        public static void Init()
        {
            var go = new GameObject("MainLoop");
            DontDestroyOnLoad(go);
            go.AddComponent<MainLoop>();
        }

        private void Awake()
        {
            Instance = this;
            Debug.Log("MainLoop initialized");
        }

        public ActionList<float> OnUpdate { get; private set; } = new ActionList<float>(16);

        public event Action OnAppExit = () => { };

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            OnUpdate.Invoke(deltaTime);
        }

        private void OnApplicationQuit()
        {
            OnAppExit();
        }
    }
}
