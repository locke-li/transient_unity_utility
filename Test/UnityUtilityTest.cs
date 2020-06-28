using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Transient;
using Transient.Development;

namespace Tests
{
    public class UnityUtilityTest
    {
        [Test]
        public void UnityUtilityTestSimplePasses()
        {
            MainLoop.Init();
            UtilsConsole.Init();

            Log.Debug("debug test");
            Log.Error("error test");

            Timer.Execute(() => Log.Debug("timer test")).WithDelay(2f);
        }

        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        [UnityTest]
        public IEnumerator UnityUtilityTestWithEnumeratorPasses()
        {
            // Use the Assert class to test conditions.
            // Use yield to skip a frame.
            yield return null;
        }
    }
}
