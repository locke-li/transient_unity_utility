using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace Transient
{
    public class SceneControl
    {
        private Func<AsyncOperation, IEnumerator<CoroutineState>> _SceneLoading = SceneLoading;

        public void LoadSceneAsync(string name, LoadSceneMode mode, Action OnLoaded)
        {
            var asyncOp = SceneManager.LoadSceneAsync(name, mode);
            MainLoop.Coroutine.Execute(_SceneLoading, asyncOp, OnLoaded);
        }

        private static IEnumerator<CoroutineState> SceneLoading(AsyncOperation asyncOp)
        {
            while (!asyncOp.isDone)
            {
                yield return CoroutineState.Executing;
            }
            //wait one frame
            yield return CoroutineState.Waiting;
        }
    }
}