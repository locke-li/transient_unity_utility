#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Linq;

namespace Transient {
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class ExtendableToolAttribute : Attribute {
        public string Name { get; }
        public string Group { get; }
        public int Priority { get; }
        public bool IsToggle { get; set; }

        public ExtendableToolAttribute(string name = null, string group = null, int priority = 0) {
            Name = name;
            Group = group;
            Priority = priority;
        }

        public void CheckType(MethodInfo method) {
            if (method.ReturnParameter.ParameterType == typeof(bool)) {
                var input = method.GetParameters();
                IsToggle = input.Length > 0 && input[0].ParameterType == typeof(bool?);
            }
        }
    }

    class ExtendableToolPortal : EditorWindow {
        const string DisplayName = "Tool Portal";
        private static (MethodInfo method, ExtendableToolAttribute attr)[] Entry { get; set; }
        Vector2 entryScrollPos;
        public static int ButtonWidth => 50;
        public static int ButtonHeight => 30;
        private static GUIStyle styleButton;
        private static GUIStyle styleBox;
        private UnityEngine.Object activeObject;
        private GameObject activeGameObject;
        private float groupPadding = 8;
        private float contentPadding = 10;

        [MenuItem("Window/"+ DisplayName)]
        private static void Open() {
            RefreshEntry();
            RefreshStyle();
            GetWindow<ExtendableToolPortal>(DisplayName);
        }

        private static void RefreshEntry() {
            var assemblyEnumerable = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.FullName.StartsWith("Unity") && !a.FullName.StartsWith("System") && !a.FullName.StartsWith("Mono"))
                .Where(a => !a.FullName.StartsWith("mscorlib") && !a.FullName.StartsWith("netstandard"));
            //foreach (var asm in assemblyEnumerable) Debug.Log($"{asm.FullName} {asm.GetName().Name}");
            var methodWithAttr = assemblyEnumerable
                .SelectMany(assem => assem.GetTypes())
                .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                .Select(m => (method: m, attr: m.GetCustomAttribute<ExtendableToolAttribute>()))
                .Where(pair => pair.attr != null)
                .OrderBy(pair => pair.attr.Group)
                .ThenBy(pair => pair.attr.Priority);
            Entry = methodWithAttr.ToArray();
            foreach (var (method, attr) in Entry) {
                attr.CheckType(method);
            }
        }

        private static void RefreshStyle() {
            styleButton = new GUIStyle("button") {
                fontSize = 11,
                wordWrap = true,
            };
            styleBox = new GUIStyle("box") {
                fontSize = 12,
                wordWrap = false,
            };
        }

        private void Update() {
            bool repaint = false;
            if (activeObject != Selection.activeObject) {
                activeObject = Selection.activeObject;
                repaint = true;
            }
            if (activeGameObject != Selection.activeGameObject) {
                activeGameObject = Selection.activeGameObject;
                repaint = true;
            }
            if (repaint) {
                Repaint();
            }
        }

        private void OnGUI() {
            Toolbar();
            EditorInfo();
            EntryList();
        }

        private void Toolbar() {
            using (new GUILayout.HorizontalScope()) {
                if (GUILayout.Button("Entry") || Entry == null) RefreshEntry();
                if (GUILayout.Button("Style") || styleButton == null) RefreshStyle();
            }
        }

        private void EditorInfo() {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Active Object", Selection.activeObject, typeof(UnityEngine.Object), true);
            EditorGUILayout.ObjectField("Active GameObject", Selection.activeGameObject, typeof(UnityEngine.Object), true);
            EditorGUI.EndDisabledGroup();
        }

        private void EntryList() {
            entryScrollPos = GUILayout.BeginScrollView(entryScrollPos);
            GUILayout.BeginHorizontal();
            string group = null;
            var padding = contentPadding * 2;
            var lineWidth = padding;
            var widthLimit = position.width;
            var guiOption = new GUILayoutOption[2];
            var parameters = new object[1];
            foreach (var (method, attr) in Entry) {
                if (attr.Group != group) {
                    group = attr.Group;
                    GUILayout.EndHorizontal();
                    GUILayout.Space(groupPadding);
                    GUILayout.Label(group, styleBox);
                    GUILayout.Space(2);
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(contentPadding);
                    lineWidth = padding;
                }
                var name = attr.Name ?? method.Name;
                var content = new GUIContent(name);
                var size = styleButton.CalcSize(content).x + 2;
                if (lineWidth + size + contentPadding > widthLimit) {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(contentPadding);
                    lineWidth = padding;
                }
                lineWidth += size;
                guiOption[0] = GUILayout.Width(size);
                guiOption[1] =  GUILayout.MinHeight(ButtonHeight);
                if (attr.IsToggle) {
                    parameters[0] = null;
                    var toggleValue = (bool)method.Invoke(null, parameters);
                    var toggleValueEdit = GUILayout.Toggle(toggleValue, content, styleButton, guiOption);
                    if (toggleValue != toggleValueEdit) {
                        parameters[0] = toggleValueEdit;
                        method.Invoke(null, parameters);
                    }
                }
                else {
                    if (GUILayout.Button(content, styleButton, guiOption)) {
                        method.Invoke(null, null);
                        return;
                    }
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();
        }
    }
}
#endif