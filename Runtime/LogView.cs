#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using static UnityEngine.GUILayout;
using static System.Math;

namespace Transient.Development {
    public sealed class LogView : EditorWindow {
        public class LogPref {
            public bool show;
            public Texture2D tex;
        }

        const float logLineHeight = 16;

        private static LogView window;
        private static int selected = -1;
        private static bool needRepaint = true;

        private Vector2 _previewScrollPos;
        private Vector2 _previewScrollPosBaseline;
        private Vector2 _detailScrollPos;
        private Vector2 _stacktraceScrollPos;
        private float _logPreviewWidth;
        private float _scrollStart;
        private float _scrollEnd;
        private float _scrollSize;
        private Rect _selectionRect;
        private bool _selectionChanged;
        private bool _snapToLast = true;
        private bool _snapToLastOnReceive;
        private LogStream _log;
        Dictionary<int, LogPref> _logPref;
        bool _showDirectLog = true;
        bool _showUnityLog = true;
        bool _fullLogPreview;
        string _filterStr;
        readonly List<string> _filterKeep = new List<string>(8);
        readonly List<string> _filterRemove = new List<string>(8);

        GUIStyle _styleLogPreview;
        GUIStyle _styleLogDetail;
        GUIStyle _styleToolbarButton;
        GUIStyle _styleSearchText;
        GUIStyle _styleSearchCancel;
        Texture2D _texLine;
        Texture2D _texSelectedBG;

        const float toolbarSize = 16;
        const float logTypeTexSize = 8;

        [MenuItem("Window/LogView",false, 90)]
        public static void Wake() {
            window = GetWindow<LogView>(nameof(LogView));
            window.Show();
        }

        private void Clear() {
            _log.Cache.Clear();
            selected = -1;
            _snapToLast = true;
            _previewScrollPos = _previewScrollPosBaseline = Vector2.zero;
        }

        private void LogReceived(LogEntry log) {
            needRepaint = true;
            _snapToLastOnReceive = true;
        }

        private void OnEnable() {
            _log = LogStream.Default;
            _log.Cache.LogReceived.Add(LogReceived, this);
            RefreshConfig();
        }

        private void OnDisable() {
            _log.Cache.LogReceived.Remove(this);
        }

        private Texture2D FillTexture(Color color) {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }

        private void RefreshStyle() {
            _styleLogPreview = new GUIStyle("label") {
                alignment = TextAnchor.UpperLeft
            };
            _styleLogDetail = new GUIStyle("label") {
                alignment = TextAnchor.UpperLeft,
                wordWrap = true
            };
            _styleToolbarButton = new GUIStyle("toolbarbutton");
            _styleSearchText = new GUIStyle("ToolbarSeachTextField");
            _styleSearchCancel = new GUIStyle("ToolbarSeachCancelButton");

            _texLine = FillTexture(new Color(0,0,0,1));
            _texSelectedBG = FillTexture(new Color(0.1f, 0.3f, 1f, 0.2f));
        }

        private void RefreshConfig() {
            _logPref = new Dictionary<int, LogPref>(16) {
                [LogStream.debug] = new LogPref() {
                    show = true,
                    tex = FillTexture(new Color(1f, 1f, 1f, 1f))
                },
                [LogStream.info] = new LogPref() {
                    show = true,
                    tex = FillTexture(new Color(0.2f, 0.7f, 0.1f, 1f))
                },
                [LogStream.warning] = new LogPref() {
                    show = true,
                    tex = FillTexture(new Color(1f, 0.7f, 0f, 1f))
                },
                [LogStream.error] = new LogPref() {
                    show = true,
                    tex = FillTexture(new Color(0.8f, 0.2f, 0.2f, 1f))
                },
                [LogStream.assert] = new LogPref() {
                    show = true,
                    tex = FillTexture(new Color(0.6f, 0.25f, 0.2f, 1f))
                },
                [LogStream.custom] = new LogPref() {
                    show = true,
                    tex = FillTexture(new Color(0.6f, 0.5f, 0.8f, 1f))
                },
                [Performance.logLevel] = new LogPref() {
                    show = true,
                    tex = FillTexture(new Color(0.3f, 0.6f, 0.8f, 1f))
                },
            };
            _showDirectLog = true;
            _showUnityLog = true;
            var str = EditorPrefs.GetString(nameof(LogView), null);
            if(!string.IsNullOrEmpty(str)) {
                try {
                    var sg = str.Split(',', '=');
                    int k = -1;
                    while(++k < sg.Length) {
                        if(sg[k] == "#") break;
                        _logPref[int.Parse(sg[k])].show = bool.Parse(sg[++k]);
                    }
                    _showDirectLog = bool.Parse(sg[++k]);
                    _showUnityLog = bool.Parse(sg[++k]);
                    _fullLogPreview = bool.Parse(sg[++k]);
                }
                catch(Exception e) { Debug.LogError(e.Message); }
            }
            else {
                SaveConfig();
            }
        }

        private void SaveConfig() {
            var sb = new StringBuilder();
            foreach(var kv in _logPref) {
                sb.Append(kv.Key);
                sb.Append("=");
                sb.Append(kv.Value.show);
                sb.Append(",");
            }
            sb.Append("#,");
            sb.Append(_showDirectLog).Append(",");
            sb.Append(_showUnityLog).Append(",");
            sb.Append(_fullLogPreview);
            EditorPrefs.SetString(nameof(LogView), sb.ToString());
        }

        private void OnGUI() {
            const float scrollbarSize = 16;
            needRepaint = false;
            if (_texLine == null) {
                RefreshStyle();
            }
            if(_logPref == null) {
                RefreshConfig();
            }
            _logPreviewWidth = position.width - logTypeTexSize - 4;
            if (_scrollSize >= _scrollEnd) _logPreviewWidth -= scrollbarSize;
            BeginVertical();
            BeginHorizontal(Height(toolbarSize));
            Toolbar();
            EndHorizontal();
            Space(2);
            _scrollSize = 0;
            _previewScrollPos = BeginScrollView(_previewScrollPos, MaxHeight(4000));
            _log.Cache.ForEach(PreviewLog);
            EndScrollView();
            if (_previewScrollPos.y != _previewScrollPosBaseline.y && _scrollSize > 0 && !_snapToLastOnReceive) {
                var scrollEnd = _scrollEnd + _previewScrollPos.y - _previewScrollPosBaseline.y;
                _snapToLast = scrollEnd >= _scrollSize;
                //Debug.Log($"{_snapToLast} {scrollEnd} {_scrollSize} {_previewScrollPos.y} {_previewScrollPosBaseline.y}");
                _previewScrollPosBaseline = _previewScrollPos;
            }
            if (Event.current.type == EventType.Repaint) CheckScrollReposition(GUILayoutUtility.GetLastRect());
            Space(2);
            var currentLog = _log.Cache.EntryAt(selected);
            _stacktraceScrollPos = BeginScrollView(_stacktraceScrollPos, MaxWidth(position.width), MinHeight(50), MaxHeight(2000));
            var height = _styleLogDetail.CalcHeight(new GUIContent(currentLog.stacktrace), _logPreviewWidth);
            EditorGUILayout.SelectableLabel(currentLog.stacktrace, _styleLogDetail, Height(height));
            EndScrollView();
            var lineRect = GUILayoutUtility.GetLastRect();
            lineRect.y -= 2;
            lineRect.height = 0.8f;
            GUI.DrawTexture(lineRect, _texLine);
            BeginHorizontal();
            Label(currentLog.source.file == null ? string.Empty : $"{currentLog.source.file}:{currentLog.source.line}|{currentLog.source.member}");
            Space(4);
            FlexibleSpace();
            Filter();
            EndHorizontal();
            Space(3);
            EndVertical();
            CheckInput();
        }

        private void Toolbar() {
            const float toolbarButtonSize = 60;
 		    if(Button("Test", _styleToolbarButton, Width(toolbarButtonSize))) {
				var sourceUnity = new EntrySource() { logger = LogStream.sourceUnity };
				var sourceDirect = new EntrySource() { logger = LogStream.sourceDirect };
				_log.Cache.Log(nameof(LogStream.debug), new System.Diagnostics.StackTrace(true).ToString(), LogStream.debug, sourceDirect);
				_log.Cache.Log(nameof(LogStream.debug), null, LogStream.debug, sourceDirect);
				_log.Cache.Log(nameof(LogStream.debug), null, LogStream.debug, sourceUnity);
				_log.Cache.Log("1\n2\n3\n4\n", null, LogStream.debug, sourceDirect);
				_log.Cache.Log(nameof(LogStream.info), new System.Diagnostics.StackTrace(true).ToString(), LogStream.info, sourceDirect);
				_log.Cache.Log("1\n2\n3\n4\n", null, LogStream.debug, sourceDirect);
                _log.Cache.Log(nameof(LogStream.warning), "warning\n1\n2\n3\n4", LogStream.warning, sourceDirect);
				_log.Cache.Log(nameof(LogStream.error), "error\n1\n2\n3\n4", LogStream.error, sourceDirect);
				_log.Cache.Log(nameof(LogStream.assert), "assert\n1\n2\n3\n4", LogStream.assert, sourceDirect);
				_log.Cache.Log(nameof(LogStream.error), "error long stack \n1\n2\n3\n4\n5\n6\n7\n8\n9\n10\n11\n12\n13\n14\n15\n16", LogStream.error, sourceDirect);
				_log.Cache.Log(nameof(LogStream.custom), null, LogStream.custom, sourceDirect);
			}
			if(Button("Refresh", _styleToolbarButton, Width(toolbarButtonSize))) {
				RefreshStyle();
				RefreshConfig();
			}
			if(Button("Clear", _styleToolbarButton, Width(toolbarButtonSize))) {
				Clear();
			}
            Label(string.Empty, _styleToolbarButton, Width(toolbarButtonSize));
            if (Button("Copy", _styleToolbarButton, Width(toolbarButtonSize * 0.6f)) && selected > 0) {
                EditorGUIUtility.systemCopyBuffer = _log.Cache.EntryAt(selected).content;
            }
            Label(string.Empty, _styleToolbarButton, MaxWidth(2000));
            EditorGUI.BeginChangeCheck();
			_showDirectLog = Toggle(_showDirectLog, "D", _styleToolbarButton, Width(toolbarSize));
			_showUnityLog = Toggle(_showUnityLog, "U", _styleToolbarButton, Width(toolbarSize));
            Label(string.Empty, _styleToolbarButton, Width(8));
			for(int k = 0;k <= LogStream.custom;++k) {
				LogSwitch(_logPref[k]);
			}
            Label(string.Empty, _styleToolbarButton, Width(8));
			foreach(var pair in _logPref) {
				if(pair.Key <= LogStream.custom) continue;
				LogSwitch(pair.Value);
			}
            Label(string.Empty, _styleToolbarButton, Width(8));
			_fullLogPreview = Toggle(_fullLogPreview, "F", _styleToolbarButton, Width(toolbarSize));
			if(EditorGUI.EndChangeCheck()) {
				SaveConfig();
			}
        }

        private void LogSwitch(LogPref style_) {
            style_.show = Toggle(style_.show, string.Empty, _styleToolbarButton, Width(toolbarSize));
            if (Event.current.type == EventType.Repaint) {
                var toolbar = GUILayoutUtility.GetLastRect();
                toolbar.x += 1;
                toolbar.y += 2;
                toolbar.width -= 1;
                toolbar.height = 4;
                GUI.DrawTexture(toolbar, style_.tex);
                toolbar.y += 4;
                toolbar.height = 1;
                GUI.DrawTexture(toolbar, _texLine);
            }
        }

        private void PreviewLog(int r_, LogEntry entry_) {
            var pref = _logPref[entry_.level];
            if (!pref.show
                || (!_showUnityLog && (entry_.source.logger == LogStream.sourceUnity))
                || (!_showDirectLog && (entry_.source.logger == LogStream.sourceDirect))
                || (_filterKeep.Count > 0 && _filterKeep.All(s=>!entry_.content.Contains(s)))
                || (_filterRemove.Count > 0 && _filterRemove.Any(s=>entry_.content.Contains(s)))
                )
                return;
            float size = _fullLogPreview || selected == r_
                ? Max(_styleLogPreview.CalcHeight(new GUIContent(entry_.content), _logPreviewWidth), logLineHeight)
                : logLineHeight;
            var logStart = _scrollSize;
            _scrollSize += size;
            Space(size);
            if(_scrollSize < _scrollStart || logStart > _scrollEnd) {
                goto end;
            }
            var rect = new Rect(logTypeTexSize+1, logStart, position.width - logTypeTexSize-1, size);
            if (selected == r_) {
                GUI.DrawTexture(rect, _texSelectedBG);
            }
            if(GUI.Button(rect, entry_.content, _styleLogPreview)) {
                Select(r_);
            }
            rect.x = 0;
            rect.width = logTypeTexSize;
            if(pref.tex == null) {
                RefreshConfig();
            }
            GUI.DrawTexture(rect, pref.tex);
            rect.x += rect.width;
            rect.width = 1;
            GUI.DrawTexture(rect, _texLine);
            end:
            if(selected == r_ && _selectionChanged && Event.current.type == EventType.Repaint) {
                _selectionRect = GUILayoutUtility.GetLastRect();
            }
        }

        private void Filter() {
            string filterStr = TextField(_filterStr, _styleSearchText, Width(200));
            if(filterStr != _filterStr) {
                _filterStr = filterStr;
                _filterKeep.Clear();
                _filterRemove.Clear();
                if(!string.IsNullOrEmpty(_filterStr)) {
                    string[] filter = _filterStr.Split(' ');
                    for(int r = 0;r < filter.Length;++r) {
                        filterStr = filter[r];
                        if(filterStr == "-")
                            continue;
                        else if(filterStr[0] == '-')
                            _filterRemove.Add(filterStr.Substring(1));
                        else
                            _filterKeep.Add(filterStr);
                    }
                }
            }
            if(Button(string.Empty, _styleSearchCancel)) {
                _filterStr = null;
                _filterKeep.Clear();
                _filterRemove.Clear();
                GUI.FocusControl(null);
            }
        }

        private void CheckScrollReposition(Rect scrollRect) {
			var scrollStart = _previewScrollPos.y;
			var scrollEnd = _scrollStart + scrollRect.height;
			bool repaint = false;
			if(scrollStart != _scrollStart || scrollEnd != _scrollEnd) {
				_scrollStart = scrollStart;
				_scrollEnd = scrollEnd;
				repaint = true;
			}
            if (_snapToLastOnReceive && _snapToLast) {
                _previewScrollPos.y = _scrollSize - scrollRect.height;
                repaint = true;
            }
            _snapToLastOnReceive = false;
            if(_selectionChanged) {
				_selectionChanged = false;
				if(_selectionRect.y < _previewScrollPos.y || _selectionRect.height > scrollRect.height) {
					_previewScrollPos.y = _selectionRect.y;
				}
				else if(_selectionRect.y + _selectionRect.height > _previewScrollPos.y + scrollRect.height) {
					_previewScrollPos.y = _selectionRect.y + _selectionRect.height - scrollRect.height;
				}
				repaint = true;
			}
			if(repaint) {
				Repaint();
			}
        }

        private void Select(int r_) {
            GUI.FocusControl(null);
            selected = r_;
            _selectionChanged = true;
        }

        private void CheckInput() {
            if(Event.current.type == EventType.KeyDown) {
                switch(Event.current.keyCode) {
                    case KeyCode.DownArrow:
                        Select(_log.Cache.Offset(selected, 1));
                        Repaint();
                        break;
                    case KeyCode.UpArrow:
                        Select(_log.Cache.Offset(selected, -1));
                        Repaint();
                        break;
                    case KeyCode.LeftArrow:
                        selected = -1;
                        Repaint();
                        break;
                }
            }
        }

        private void OnInspectorUpdate() {
            if (focusedWindow != this && needRepaint) {
                Repaint();
                needRepaint = false;
            }
        }
    }
}
#endif