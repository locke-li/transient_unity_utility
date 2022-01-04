using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System;

namespace Transient {
    public class SceneControl {
        public void LoadScene(string name, LoadSceneMode mode) {
            SceneManager.LoadScene(name, mode);
        }

        public AsyncOperation LoadSceneAsync(string name, LoadSceneMode mode, Action OnLoaded) {
            var asyncOp = SceneManager.LoadSceneAsync(name, mode);
            MainLoop.Coroutine.Execute(SceneLoading(asyncOp, OnLoaded));
            return asyncOp;
        }

        private static IEnumerator<CoroutineState> SceneLoading(AsyncOperation asyncOp, Action OnLoaded) {
            while (!asyncOp.isDone) {
                yield return CoroutineState.Executing;
            }
            //wait one frame
            yield return CoroutineState.Waiting;
            OnLoaded?.Invoke();
        }
    }
}