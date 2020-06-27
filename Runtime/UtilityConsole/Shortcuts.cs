//
// debug utils console predefined shortcuts
//
// author: liyingbo
//

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
            AddShortcut("Load Scene", name => SceneManager.LoadScene(name), checkInput_ : true);
        }
    }
}
