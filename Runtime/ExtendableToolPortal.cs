#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Linq;

namespace Transient {
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class ExtendableToolAttribute : Attribute {
        public string Name { get;  }
        public string Group { get;  }
        public int Priority { get;  }
        public bool IsToggle { get; }

        public ExtendableToolAttribute(string name = null, string group = null, int priority = 0, bool toggle = false) {
            Name = name;
            Group = group;
            Priority = priority;
            IsToggle = toggle;
        }
    }

    class ExtendableToolPortal : EditorWindow {
        const string DisplayName = "Tool Portal";
        private static (MethodInfo method, ExtendableToolAttribute attr)[] Entry { get; set; }
        Vector2 entryScrollPos;
        public static int ButtonWidth => 50;
        public static int ButtonHeight => 30;
        private GUIStyle styleButton;
        private UnityEngine.Object activeObject;
        private GameObject activeGameObject;

        [MenuItem("Window/"+ DisplayName)]
        private static void Open() {
            RefreshEntry();
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
        }

        private void RefreshStyle() {
            styleButton = new GUIStyle("button") {
                fontSize = 11,
                wordWrap = true,
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
            if (Entry == null) {
                RefreshEntry();
            }
            if (styleButton == null) {
                RefreshStyle();
            }
            EditorInfo();
            EntryList();
        }

        private void Toolbar() {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Entry")) RefreshEntry();
            if (GUILayout.Button("Style")) RefreshStyle();
            GUILayout.EndHorizontal();
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
            const float padding = 4;
            float lineWidth = 0;
            var widthLimit = position.width - 2;
            foreach (var (method, attr) in Entry) {
                if (attr.Group != group) {
                    group = attr.Group;
                    GUILayout.EndHorizontal();
                    GUILayout.Label(group);
                    GUILayout.BeginHorizontal();
                    lineWidth = 0;
                }
                var name = attr.Name ?? method.Name;
                var size = styleButton.CalcSize(new GUIContent(name));
                if (lineWidth + size.x > widthLimit) {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    lineWidth = 0;
                }
                lineWidth += size.x + padding;
                if (GUILayout.Button(name, styleButton, GUILayout.Width(size.x), GUILayout.MinHeight(ButtonHeight))) {
                    method.Invoke(null, null);
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();
        }
    }
}
#endif