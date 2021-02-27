//
// debug utils console predefined shortcuts
//
// author: liyingbo
//

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.IO;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Transient.Development {
    public partial class UtilsConsole : MonoBehaviour {
        public static Color SystemColor { get; private set; } = new Color(0.95f, 01f, 0.6f);
        public static Color ToggleEnableColor { get; private set; } = new Color(0.7f, 1f, 0.7f);
        public static Color ToggleDisableColor { get; private set; } = new Color(1f, 0.7f, 0.7f);

        public static Color DefaultColorToggle(bool aState) => aState ? ToggleEnableColor : ToggleDisableColor;

        public static void AddCommonShortcuts() {
            AddShortcut("Execute", null, () => SystemColor, closeConsole_ : false, checkInput_ : true);//to be filled
            AddShortcut("Continue", t => Time.timeScale = 1, () => SystemColor);
            AddShortcut("Log Enabled", t => LogEnabled = !LogEnabled, () => DefaultColorToggle(LogEnabled), closeConsole_ : false);
            AddShortcut("Clear Log", t => Instance.ClearLog(), closeConsole_ : false);
            AddShortcut("Snapshot", t => TakeSnapshot(int.TryParse(t, out var v) ? v : 1080));
            AddShortcut("Load Scene", name => SceneManager.LoadScene(name), checkInput_ : true);
            AddSlider("time scale", 0, -1, 1, WhenValueChange_: TimeScale);
            AddSlider("alpha", 1.0f, 0.2f, 1, WhenValueChange_: v => { Instance._contentGroup.alpha = v; return v.ToString("F1"); });
        }

#if UNITY_EDITOR
        [MenuItem("Tools/Snapshot", priority = 1200)]
        public static void TakeSnapshotDefault() {
            TakeSnapshot(1080);
        }
#endif
        public static void TakeSnapshot(int height) {
            const string dir = "SnapShot";
            if (!Directory.Exists(dir)) {
                Directory.CreateDirectory(dir);
            }
            var time = DateTime.Now.ToString("yyyyMMddHHmmssffff");
            var path = $"{dir}/snapshot_{time}.png";
            var scale = Mathf.CeilToInt((float)height / Screen.height);
            ScreenCapture.CaptureScreenshot(path, scale);
            Debug.Log(path);
        }

        private static string TimeScale(float v) {
            if (Mathf.Abs(v) < 0.08f) {
                v = 1;
            }
            else if (v < 0) {
                v += 1;
            }
            else {
                v = Mathf.Max(1, v * 10);
            }
            Time.timeScale = v;
            return $"{v:F2}";
        }
    }
}
