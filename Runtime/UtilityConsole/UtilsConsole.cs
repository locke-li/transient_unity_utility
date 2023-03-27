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
            public bool enabled { get => tr.gameObject.activeSelf; set => tr.gameObject.SetActive(value); }
            public Transform tr;
            public Text text;
            public Text textCount;
            public Button bg;
            public string msg;
            public string stack;
            public int level;
            public int count;
        }

        public static UtilsConsole Instance { get; private set; }

        private string[] _fpsNumbers;

        private float _fpsFactor;
        private GameObject _content;
        private Button _templateShortcut;
        private RectTransform _templateSlider;
        private RectTransform _templateWatch;
        private Button _entry;
        private Text _fps;
        private float[] _fpsLast;
        private int _last = -1;
        private Queue<Log> _logList;
        private int _logCount = 0;
        private Log _lastLog;
        private const int LOG_LIMIT = 99;
        private RectTransform _logTemplate;
        private ScrollRect _logScroll;
        private readonly Color[] _logColors = new Color[] {
            Color.white,//debug = 0
            new Color(0.1f, 0.8f, 0.1f),//info = 1
            new Color(1f, 0.8f, 0f),//warning = 2
            new Color(1, 0.2f, 0.2f),//error = 3
            new Color(1f, 0.6f, 0.6f),//assert = 4
            new Color(0.55f, 0.65f, 1f),//custom = 5
        };
        private static Canvas _before;
        private ScrollRect _logDetailScroll;
        private Text _logDetailText;
        private Text _logDetailStackText;
        private InputField _params;
        private float _time;
        private float _watchRefreshTime;
        private Dictionary<string, Button> _shortcutButton;
        private Dictionary<string, Slider> _valueSlider;
        private List<(string name, Text text, Func<string>)> _watchList;
        private CanvasGroup _contentGroup;

        public static bool LogEnabled;
        private const int FPS_NUMBER_LIMIT = 140;

#if UNITY_EDITOR
        private const string EnabledMenuPath = "Tools/Misc: ConsoleEnabled";
        public static bool ConsoleEnabled {
            get => EditorPrefs.GetBool(nameof(ConsoleEnabled), true);
            private set => EditorPrefs.SetBool(nameof(ConsoleEnabled), value);
        }

        [MenuItem(EnabledMenuPath, false, 14010)]
        public static void ToggleEnabled() {
            ConsoleEnabled = !ConsoleEnabled;
        }

        [MenuItem(EnabledMenuPath, true)]
        public static bool ValidateEnabled() {
            Menu.SetChecked(EnabledMenuPath, ConsoleEnabled);
            return true;
        }

        [ExtendableTool("Console", "Enable")]
        public static bool ToggleEnabled(bool? value) {
            if (!value.HasValue) {
                return ConsoleEnabled;
            }
            ConsoleEnabled = value.Value;
            return true;
        }
#endif

        public static void Init(Canvas before) {
            if (Instance != null) {
                GameObject.Destroy(Instance.gameObject);
            }
            var go = Resources.Load<GameObject>(nameof(UtilsConsole));
            if(go == null)
                return;
            go = Instantiate(go);
            DontDestroyOnLoad(go);
            _before = before;
            Instance = go.AddComponent<UtilsConsole>();
            Instance.Init();
        }

        private void Init() {
            const float FPS_SEGMENT = 0.5f;
            _fpsLast = new float[4];
            _last = -1;
            _fpsFactor = 1 / (_fpsLast.Length * FPS_SEGMENT);
            _logList = new(LOG_LIMIT+1);
            var canvas = transform.FindChecked<Canvas>("canvas");
            if (_before != null) {
                canvas.worldCamera = _before.worldCamera;
                canvas.sortingOrder = _before.sortingOrder + 1000;
                CopyScalerSetting(_before, canvas);
            }
            _content = canvas.FindChecked("content").gameObject;
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
            var scrollContent = _content.FindChecked<ScrollRect>("shortcuts").content;
            _templateShortcut = scrollContent.FindChecked<Button>("layout_shortcut/shortcut");
            _templateShortcut.gameObject.SetActive(false);
            _templateSlider = scrollContent.FindChecked<RectTransform>("layout_slider/slider");
            _templateSlider.gameObject.SetActive(false);
            _templateWatch = scrollContent.FindChecked<RectTransform>("layout_watch/watch");
            _templateWatch.gameObject.SetActive(false);
            _shortcutButton = new(32);
            _valueSlider = new(32);
            _watchList = new(32);
            _fps = _entry.transform.FindChecked<Text>("fps");
            _params = _content.FindChecked<InputField>("params");
        }

        public void CopyScalerSetting(Canvas from, Canvas to) {
            var refScaler = _before.GetComponent<CanvasScaler>();
            if (refScaler == null) return;
            var scaler = to.GetChecked<CanvasScaler>();
            scaler.referencePixelsPerUnit = refScaler.referencePixelsPerUnit;
            scaler.referenceResolution = refScaler.referenceResolution;
            scaler.matchWidthOrHeight = refScaler.matchWidthOrHeight;
            scaler.screenMatchMode = refScaler.screenMatchMode;
            scaler.uiScaleMode = refScaler.uiScaleMode;
        }

        private void InitLogs() {
            _logScroll = _content.FindChecked<ScrollRect>("logs");
            _logTemplate = _logScroll.content.FindChecked<RectTransform>("log");
            _logDetailScroll = _content.FindChecked<ScrollRect>("logdetail");
            _logDetailText = _logDetailScroll.content.GetChild(0).GetComponent<Text>();
            _logDetailStackText = _logDetailScroll.content.GetChild(1).GetComponent<Text>();
            _logCount = 0;
            LogStream.Default.Cache.LogReceived.Add(e_ => LogReceived(e_.content, e_.stacktrace, Mathf.Min(LogStream.custom, e_.level)), this);
        }

        private void LogReceived(string message_, string stacktrace_, int level_) {
            if(!LogEnabled && level_ <= LogStream.info)
                return;
            if (_lastLog != null
                && _lastLog.msg == message_
                && _lastLog.stack == stacktrace_
                && _lastLog.level == level_) {
                ++_lastLog.count;
                _lastLog.textCount.text = _lastLog.count.ToString();
                return;
            }
            Log cLog = null;
            if(_logList.Count >= LOG_LIMIT || (_logList.Count > 0 && !_logList.Peek().enabled)) {
                cLog = _logList.Dequeue();
            }
            else {
                //TODO delay instantiation
                Transform tr = null;
                if(_logList.Count == 0)
                    tr = _logTemplate;
                else
                    tr = Instantiate(_logTemplate.gameObject).transform;
                cLog = new Log {
                    tr = tr,
                    text = tr.FindChecked<Text>("msg"),
                    textCount = tr.FindChecked<Text>("count"),
                    bg = tr.GetComponent<Button>()
                };
                tr.SetParent(_logTemplate.transform.parent, false);
            }
            _lastLog = cLog;
            cLog.tr.gameObject.SetActive(true);
            cLog.tr.SetAsLastSibling();
            cLog.msg = message_;
            cLog.text.text = cLog.msg;
            cLog.stack = stacktrace_;
            cLog.level = level_;
            cLog.count = 1;
            cLog.textCount.text = string.Empty;
            cLog.bg.onClick.RemoveAllListeners();
            cLog.bg.onClick.AddListener(() => Select(cLog));
            cLog.bg.image.color = _logColors[level_];
            _logList.Enqueue(cLog);
            ++_logCount;
            _logScroll.content.sizeDelta = new Vector2(0, _logCount * _logTemplate.sizeDelta.y);
            if(!_content.activeSelf)
                _logScroll.normalizedPosition = Vector2.zero;
            if(level_ >= LogStream.warning && level_ <= LogStream.assert) {
                //Time.timeScale = 0;
                //Toggle(true);
                //Select(cLog);
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
            if (!_content.gameObject.activeSelf) {
                return;
            }
            const float WatchRefreshInterval = 0.2f;
            _watchRefreshTime += Time.deltaTime;
            if (_watchRefreshTime >= WatchRefreshInterval) {
                _watchRefreshTime -= WatchRefreshInterval;
                foreach (var (_, text, FetchValue) in _watchList) {
                    text.text = FetchValue();
                }
            }
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
            _contentGroup.alpha = Mathf.Max(0.5f, _contentGroup.alpha);
        }

        #region shortcut

        public static void ColorByState(Button aButton, bool aState) {
            ColorByState(aButton, aState, ToggleEnableColor, ToggleDisableColor);
        }

        public static void ColorByState(Button aButton, bool aState, Color aColor, Color aTarget) {
            aButton.targetGraphic.color = aState ? aColor : aTarget;
        }

        public static Button AddShortcut(string text_, Action<string> action_, Func<Color> color_ = null, bool closeConsole_ = true, bool checkInput_ = false) {
            if (Instance == null) {
                return null;
            }
            //Debug.Log($"addshortcut {aText} {aAction}");
            action_ = action_??(s => Debug.LogWarning($"empty shortcut:{text_}!"));
            color_ = color_??(() => Color.white);
            Button button = AddShortcutUnity(text_);
            button.onClick.AddListener(new UnityAction(() => {
                string p = Instance._params.text;
                //mParams.text = string.Empty;
                if(!(checkInput_ && string.IsNullOrEmpty(p)))
                    try {
                        Debug.Log($"shortcut:{text_}");
                        action_(p);
                        button.targetGraphic.color = color_();
                    }
                    catch(Exception e) {
                        Debug.LogException(e);
                    }
                else
                    Debug.LogWarning($"debug shortcut {text_} need parameters");
                if(closeConsole_)
                    Instance.Toggle(false);
            }));
            button.targetGraphic.color = color_();
            return button;
        }

        public static Button AddToggleShortcut(string aText, Action<string, Button> aAction, bool aToggleDefaultState, bool aCloseConsole = true, int? aSiblingIndex = null) {
            if (Instance == null) {
                return null;
            }
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
            if (aSiblingIndex != null) {
                button.transform.SetSiblingIndex((int)aSiblingIndex);
            }
            ColorByState(button, aToggleDefaultState);
            return button;
        }

        public static void RemoveShortcut(string text_) {
            //Debug.Log($"removeshortcut {aText}");
            if(Instance._shortcutButton.TryGetValue(text_, out var go)) {
                go.gameObject.SetActive(false);
            }
        }

        public static void RemoveAllShortcut() {
            var buttons = Instance._shortcutButton;
            foreach (var shortcut in buttons) {
                shortcut.Value.onClick.RemoveAllListeners();
                shortcut.Value.onClick.Invoke();
                DestroyImmediate(shortcut.Value.gameObject);
            }
            buttons.Clear();
        }

        private static Button AddShortcutUnity(string text_) {
            var buttons = Instance._shortcutButton;
            if (!buttons.TryGetValue(text_, out var s)) {
                var template = Instance._templateShortcut;
                s = Instantiate(template.gameObject).GetComponent<Button>();
                s.transform.SetParent(template.transform.parent, false);
                s.gameObject.SetActive(true);
                buttons.Add(text_, s);
            }
            else {
                s.onClick.RemoveAllListeners();
            }
            s.transform.FindChecked<Text>("text").text = text_;
            return s;
        }

        #endregion shortcut

        #region value

        public static void AddSlider(string name_,
            float value_, float min_, float max_,
            bool integer_ = false, Func<float, string> WhenValueChange_ = null) {
            if (Instance == null) {
                return;
            }
            var sliders = Instance._valueSlider;
            if (!sliders.TryGetValue(name_, out var s)) {
                var template = Instance._templateSlider;
                var obj = Instantiate(template.gameObject);
                obj.transform.SetParent(template.transform.parent, false);
                obj.SetActive(true);
                s = obj.FindChecked<Slider>("slider");
                sliders.Add(name_, s);
            }
            else {
                s.onValueChanged.RemoveAllListeners();
            }
            s.minValue = min_;
            s.maxValue = max_;
            s.wholeNumbers = integer_;
            s.FindChecked<Text>("title").text = name_;
            var value = s.FindChecked<Text>("value");
            s.value = value_;
            UnityAction<float> eventValueChange = v => {
                if (WhenValueChange_ != null) {
                    value.text = WhenValueChange_(v);
                }
                else {
                    value.text = v.ToString();
                }
            };
            s.onValueChanged.AddListener(eventValueChange);
            eventValueChange(value_);
        }

        public static void AddWatch(string name_, Func<string> FetchValue) {
            if (Instance == null) {
                return;
            }
            var watchList = Instance._watchList;
            for(int i = 0; i < watchList.Count; ++i) {
                var (name, text, _) = watchList[i];
                if (name_ == name) {
                    watchList[i] = (name, text, FetchValue);
                    return;
                }
            }
            var template = Instance._templateWatch;
            var obj = Instantiate(template.gameObject);
            obj.transform.SetParent(template.transform.parent, false);
            obj.SetActive(true);
            obj.FindChecked<Text>("title").text = name_;
            var value = obj.FindChecked<Text>("value");
            value.text = FetchValue();
            watchList.Add((name_, value, FetchValue));
        }

        #endregion value

        private void OnDestroy() {
            if (Instance != null) {
                LogStream.Default.Cache.LogReceived.Remove(this);
            }
            foreach(var shortcut in _shortcutButton) {
                shortcut.Value.onClick.RemoveAllListeners();
            }
            _valueSlider.Clear();
            _watchList.Clear();
        }
    }
}