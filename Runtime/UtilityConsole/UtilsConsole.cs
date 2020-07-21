//
// debug utils console
//
// author: liyingbo
//

//#define LOG_TEST

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Transient.Development {
    public partial class UtilsConsole : MonoBehaviour {
        private class Log {
            public bool enabled { get { return tr.gameObject.activeSelf; } set { tr.gameObject.SetActive(value); } }
            public Transform tr;
            public Text text;
            public Button bg;
            public string msg;
            public string stack;
            public LogType type;
        }

        public static UtilsConsole Instance { get; private set; }

        private string[] _fpsNumbers;

        private float _fpsFactor;
        private GameObject _content;
        private Button _template;
        private Button _entry;
        private Text _fps;
        private float[] _fpsLast;
        private int _last = -1;
        private Queue<Log> _logList;
        private int _logCount = 0;
        private Log _lastLog;
        private const int LOG_LIMIT = 999;
        private RectTransform _logTemplate;
        private ScrollRect _logScroll;
        private readonly Color[] _logColors = new Color[] {
            new Color(1, 0.2f, 0.2f),//Error = 0
            new Color(0.75f, 0.75f, 0.75f),//Assert = 1
            new Color(1f, 0.8f, 0f),//Warning = 2
            Color.white,//Log = 3
            new Color(0.9f, 0.5f, 0.5f)//Exception = 4
        };
        private ScrollRect _logDetailScroll;
        private Text _logDetailText;
        private Text _logDetailStackText;
        private Slider _speedup;
        private Slider _speeddown;
        private Slider _contentAlpha;
        private InputField _params;
        private float _time;
        private Dictionary<string, Button> _shortcutButton;
        private CanvasGroup _contentGroup;

        public static bool LogEnabled;
        private const int FPS_NUMBER_LIMIT = 140;

#if UNITY_EDITOR
        public static bool ConsoleEnabled {
            get {
                return EditorPrefs.GetBool(nameof(ConsoleEnabled), true);
            }
            private set {
                EditorPrefs.SetBool(nameof(ConsoleEnabled), value);
            }
        }
        private const string EnabledMenuPath = "Tools/Misc: ConsoleEnabled";

        [MenuItem(EnabledMenuPath, false, 14010)]
        public static void ToggleEnabled() {
            ConsoleEnabled = !ConsoleEnabled;
        }

        [MenuItem(EnabledMenuPath, true)]
        public static bool ValidateEnabled() {
            Menu.SetChecked(EnabledMenuPath, ConsoleEnabled);
            return true;
        }
#endif

        [System.Diagnostics.Conditional("DEBUG")]
        public static void Init() {
            var go = Resources.Load<GameObject>(nameof(UtilsConsole));
            if(go == null)
                return;
            go = Instantiate(go);
            DontDestroyOnLoad(go);
            go.AddComponent<UtilsConsole>();
        }

        private void Awake() {
            const float FPS_SEGMENT = 0.5f;
            const float TIME_MAX_VALUE = 5;
            const float TIME_MIN_VALUE = 0;
            Instance = this;
            _shortcutButton = new Dictionary<string, Button>(32);
            _fpsLast = new float[4];
            _last = -1;
            _fpsFactor = 1 / (_fpsLast.Length * FPS_SEGMENT);
            _logList = new Queue<Log>(LOG_LIMIT+1);
            var canvas = transform.Find("canvas");
            _content = canvas.Find("content").gameObject;
            _content.SetActive(false);
            InitLogs();
            _fpsNumbers = new string[FPS_NUMBER_LIMIT];
            for(int n = 0;n < FPS_NUMBER_LIMIT;++n) {
                _fpsNumbers[n] = $"{n * FPS_SEGMENT:F1}";
            }
            //TODO check if event system exists
            _entry = canvas.FindChecked<Button>("entry");
            _entry.onClick.AddListener(ToggleAction);
            _entry.gameObject.SetActive(true);
            _contentGroup = _content.GetComponent<CanvasGroup>();
            _template = _content.FindChecked<ScrollRect>("shortcuts").content.FindChecked<Button>("shortcut");
            _template.gameObject.SetActive(false);
            _fps = _entry.transform.FindChecked<Text>("fps");
            _speedup = _content.FindChecked<Slider>("speedup");
            _speeddown = _content.FindChecked<Slider>("speeddown");
            _speedup.value = Time.timeScale;
            _speedup.minValue = 1;
            _speedup.maxValue = TIME_MAX_VALUE;
            _speedup.onValueChanged.AddListener(v => { Time.timeScale = v; _speeddown.value = _speeddown.maxValue; });
            _speeddown.value = Time.timeScale;
            _speeddown.maxValue = 1;
            _speeddown.minValue = TIME_MIN_VALUE;
            _speeddown.onValueChanged.AddListener(v => { Time.timeScale = v; _speedup.value = _speedup.minValue; });
            _contentAlpha = _content.FindChecked<Slider>("content_alpha");
            _contentAlpha.minValue = 0.05f;
            _contentAlpha.maxValue = 1;
            _contentAlpha.onValueChanged.AddListener(v => _contentGroup.alpha = v);
            _params = _content.FindChecked<InputField>("params");
        }

        private void InitLogs() {
            _logScroll = _content.FindChecked<ScrollRect>("logs");
            _logTemplate = _logScroll.content.FindChecked<RectTransform>("log");
            _logDetailScroll = _content.FindChecked<ScrollRect>("logdetail");
            _logDetailText = _logDetailScroll.content.GetChild(0).GetComponent<Text>();
            _logDetailStackText = _logDetailScroll.content.GetChild(1).GetComponent<Text>();
            _logCount = 0;
            LogEnabled =
#if UNITY_EDITOR
            true;
#else
            false;
#endif
            Application.logMessageReceived += Instance.LogReceived;
            LogTest();
        }

        [System.Diagnostics.Conditional("LOG_TEST")]
        private void LogTest() {
            LogReceived("test error", "test stack", LogType.Error);
            LogReceived("test assert", "test stack", LogType.Assert);
            LogReceived("test warning", "test stack", LogType.Warning);
            LogReceived("test log", "test stack", LogType.Log);
            LogReceived("test exception", "test stack", LogType.Exception);
        }

        private void LogReceived(string condition_, string stacktrace_, LogType type_) {
            if(!LogEnabled && type_ == LogType.Log)
                return;
            Log cLog = null;
            if(_logList.Count >= LOG_LIMIT || (_logList.Count > 0 && !_logList.Peek().enabled)) {
                cLog = _logList.Dequeue();
            }
            else {
                Transform tr = null;
                if(_logList.Count == 0)
                    tr = _logTemplate;
                else
                    tr = Instantiate(_logTemplate.gameObject).transform;
                cLog = new Log {
                    tr = tr,
                    text = tr.FindChecked<Text>("msg"),
                    bg = tr.GetComponent<Button>()
                };
                tr.SetParent(_logTemplate.transform.parent, false);
            }
            _lastLog = cLog;
            cLog.tr.gameObject.SetActive(true);
            cLog.tr.SetAsLastSibling();
            cLog.msg = condition_;
            cLog.text.text = cLog.msg;
            cLog.stack = stacktrace_;
            cLog.type = type_;
            cLog.bg.onClick.RemoveAllListeners();
            cLog.bg.onClick.AddListener(() => Select(cLog));
            cLog.bg.image.color = _logColors[(int)type_];
            _logList.Enqueue(cLog);
            ++_logCount;
            _logScroll.content.sizeDelta = new Vector2(0, _logCount * _logTemplate.sizeDelta.y);
            if(!_content.activeSelf)
                _logScroll.normalizedPosition = Vector2.zero;
            if(type_ != LogType.Warning && type_ != LogType.Log) {
                //Time.timeScale = 0;
                //Toggle(true);
                Select(cLog);
            }
        }

        public void ClearLog() {
            foreach(var log in _logList) {
                log.enabled = false;
            }
            _logCount = 0;
            _logScroll.content.sizeDelta = new Vector2(0, 0);
        }

        public void FocusLast() {
            if(_lastLog == null)
                return;
            Select(_lastLog);
        }

        private void Select(Log log_) {
            _logDetailText.text = log_.msg;
            _logDetailStackText.text = log_.stack;
            _logDetailStackText.transform.SetAsLastSibling();
            _logDetailScroll.normalizedPosition = Vector2.one;
        }

        private void Update() {
            if(_time == 0) {
                _time = Time.realtimeSinceStartup;
                return;
            }
            float udt = Time.realtimeSinceStartup - _time;
            _time = Time.realtimeSinceStartup;
            ++_last;
            if(_last >= _fpsLast.Length)
                _last = 0;
            _fpsLast[_last] = 1f / udt;
            float sum = 0;
            foreach(float v in _fpsLast) {
                sum += v;
            }
            int fpsIndex = Mathf.FloorToInt(sum * _fpsFactor);
            if(fpsIndex >= FPS_NUMBER_LIMIT)
                fpsIndex = FPS_NUMBER_LIMIT - 1;
            else if(fpsIndex < 0)
                fpsIndex = 0;
            _fps.text = _fpsNumbers[fpsIndex];
        }

        private void ToggleAction() {
            Toggle();
        }

        public void Toggle(bool? value_ = null) {
            if(value_ == null)
                value_ = !_content.gameObject.activeSelf;
            if(_content == null)
                return;
            _content.gameObject.SetActive(value_.Value);
            _speedup.value = Time.timeScale;
            _speeddown.value = Time.timeScale;
            _contentGroup.alpha = Mathf.Max(0.5f, _contentGroup.alpha);
            _contentAlpha.value = _contentGroup.alpha;
        }

        public static void ColorByState(Button aButton, bool aState) {
            ColorByState(aButton, aState, ToggleEnableColor, ToggleDisableColor);
        }

        public static void ColorByState(Button aButton, bool aState, Color aColor, Color aTarget) {
            aButton.targetGraphic.color = aState ? aColor : aTarget;
        }

        public Button AddShortcut(string text_, Action<string> action_, Func<Color> color_ = null, bool closeConsole_ = true, bool checkInput_ = false) {
            //Debug.Log($"addshortcut {aText} {aAction}");
            action_ = action_??(s => Debug.LogWarning($"empty shortcut:{text_}!"));
            color_ = color_??(() => Color.white);
            Button button = AddShortcutUnity(text_);
            button.onClick.AddListener(new UnityAction(() => {
                string p = _params.text;
                //mParams.text = string.Empty;
                if(!(checkInput_ && string.IsNullOrEmpty(p)))
                    try {
                        action_(p);
                        button.targetGraphic.color = color_();
                    }
                    catch(System.Exception e) {
                        Debug.LogException(e);
                    }
                else
                    Debug.LogWarning($"debug shortcut {text_} need parameters");
                if(closeConsole_)
                    Toggle(false);
            }));
            button.targetGraphic.color = color_();
            return button;
        }

        public Button AddToggleShortcut(string aText, Action<string, Button> aAction, bool aToggleDefaultState, bool aCloseConsole = true, int? aSiblineIndex = null) {
            if (Instance == null)
                return null;
            if (aAction == null) {
                aAction = (s, b) => Debug.LogWarning($"empty shortcut:{aText}!");
            }

            Button button = AddShortcutUnity(aText);
            button.onClick.AddListener(new UnityAction(() => {
                aToggleDefaultState = !aToggleDefaultState;
                ColorByState(button, aToggleDefaultState);
                aAction.Invoke(aToggleDefaultState.ToString(), button);
                if (aCloseConsole) {
                    Instance.Toggle(false);
                }
            }));
            if (aSiblineIndex != null) {
                button.transform.SetSiblingIndex((int)aSiblineIndex);
            }
            ColorByState(button, aToggleDefaultState);
            return button;
        }

        public void RemoveShortcut(string text_) {
            //Debug.Log($"removeshortcut {aText}");
            if(_shortcutButton.TryGetValue(text_, out var go)) {
                go.gameObject.SetActive(false);
            }
        }

        public void RemoveAllShortcut() {
            foreach(var shortcut in _shortcutButton) {
                shortcut.Value.onClick.RemoveAllListeners();
                shortcut.Value.onClick.Invoke();
                DestroyImmediate(shortcut.Value.gameObject);
            }
            _shortcutButton.Clear();
        }

        private Button AddShortcutUnity(string text_) {
            if (!_shortcutButton.TryGetValue(text_, out Button s)) {
                s = Instantiate(_template.gameObject).GetComponent<Button>();
                s.transform.SetParent(_template.transform.parent, false);
                _shortcutButton.Add(text_, s);
            }
            else {
                s.onClick.RemoveAllListeners();
            }
            s.transform.FindChecked<Text>("text").text = text_;
            s.gameObject.SetActive(true);
            return s;
        }

        private void OnDestroy() {
            Application.logMessageReceived -= Instance.LogReceived;
            foreach(var shortcut in _shortcutButton) {
                shortcut.Value.onClick.RemoveAllListeners();
            }
        }
    }
}