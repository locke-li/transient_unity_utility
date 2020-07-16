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

        private Color ColorByState(bool aState) => aState ? ToggleEnableColor : ToggleDisableColor;

        public void AddCommonShortcuts() {
            AddShortcut("Execute", null, () => SystemColor, closeConsole_ : false, checkInput_ : true);//to be filled
            AddShortcut("Continue", t => Time.timeScale = 1, () => SystemColor);
            AddShortcut("Log Enabled", t => LogEnabled = !LogEnabled, () => ColorByState(LogEnabled), closeConsole_ : false);
            AddShortcut("Clear Log", t => ClearLog(), closeConsole_ : false);
            AddShortcut("Snapshot", _ => TakeSnapshot());
            AddShortcut("Load Scene", name => SceneManager.LoadScene(name), checkInput_ : true);
        }

#if UNITY_EDITOR
        [MenuItem("Tools/Snapshot", priority = 1200)]
#endif
        public static void TakeSnapshot() {
            const string dir = "SnapShot";
            if (!Directory.Exists(dir)) {
                Directory.CreateDirectory(dir);
            }
            var time = DateTime.Now.ToString("yyyyMMddHHmmssffff");
            var path = $"{dir}/snapshot_{time}.png";
            var scale = Mathf.RoundToInt(1080f / Screen.height);
            ScreenCapture.CaptureScreenshot(path, scale);
            Debug.Log(path);
        }
    }
}
