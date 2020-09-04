//
// Unity built-in type extension methods
// locating info should redirect to the actual call site
//
// author: liyingbo
//

using System;
using System.Runtime.CompilerServices;
using Transient;

namespace UnityEngine {
    public static class UnityExtension {
        public static T GameObjectFindChecked<T>(string path_,
            [CallerMemberName]string member_ = "", [CallerFilePath]string file_ = "", [CallerLineNumber]int line_ = 0
            ) where T : Component {
            var go = GameObject.Find(path_);
            Log.Assert(go != null, "GameObject {0} not found!",
                arg0_:path_, member_:member_, filePath_:file_, lineNumber_:line_);
            T ret = go.GetComponent<T>();
            Log.Assert(ret != null, "Component {0} not found on {1}",
                arg0_:typeof(T), arg1_:path_, member_: member_, filePath_: file_, lineNumber_: line_);
            return ret;
        }

        public static T GameObjectTryFind<T>(string path_) where T : Component {
            var go = GameObject.Find(path_);
            if (go is null) {
                return null;
            }
            return go.GetComponent<T>();
        }

        private static string LogNotFound => "Path: <{0}> on {1} not found!";

        public static Transform FindChecked(this Component comp_, string path_,
            [CallerMemberName]string member_ = "", [CallerFilePath]string file_ = "", [CallerLineNumber]int line_ = 0
            ) {
            Log.Assert(comp_ != null, "{0} called on null object",
                arg0_:nameof(FindChecked), member_: member_, filePath_: file_, lineNumber_: line_);
            var ret = comp_.transform.Find(path_);
            Log.Assert(ret != null, LogNotFound,
                arg0_:path_, arg1_:comp_.name, member_: member_, filePath_: file_, lineNumber_: line_);
            return ret;
        }

        public static Transform FindChecked(this GameObject gameObject_, string path_,
            [CallerMemberName]string member_ = "", [CallerFilePath]string file_ = "", [CallerLineNumber]int line_ = 0
            ) {
            Log.Assert(gameObject_ != null, "{0} called on null object",
                arg0_:nameof(FindChecked), member_: member_, filePath_: file_, lineNumber_: line_);
            var ret = gameObject_.transform.Find(path_);
            Log.Assert(ret != null, LogNotFound,
                arg0_:path_, arg1_:gameObject_.name, member_: member_, filePath_: file_, lineNumber_: line_);
            return ret;
        }

        public static Component FindChecked(this Component comp_, Type type_, string path_, 
            [CallerMemberName]string member_ = "", [CallerFilePath]string file_ = "", [CallerLineNumber]int line_ = 0
            ) {
            var t = FindChecked(comp_, path_);
            Log.Assert(t != null, LogNotFound, path_, comp_.name, null, member_, file_, line_);
            return GetChecked(t, type_, member_, file_, line_);
        }

        public static Component FindChecked(this GameObject obj_, Type type_, string path_, 
            [CallerMemberName]string member_ = "", [CallerFilePath]string file_ = "", [CallerLineNumber]int line_ = 0
            ) {
            var t = FindChecked(obj_, path_);
            Log.Assert(t != null, LogNotFound, path_, obj_.name, null, member_, file_, line_);
            return GetChecked(t, type_, member_, file_, line_);
        }

        public static T FindChecked<T>(this Component comp_, string path_,
            [CallerMemberName]string member_ = "", [CallerFilePath]string file_ = "", [CallerLineNumber]int line_ = 0
            ) where T : Component
            => (T)FindChecked(comp_, typeof(T), path_, member_, file_, line_);

        public static T FindChecked<T>(this GameObject obj_, string path_,
            [CallerMemberName]string member_ = "", [CallerFilePath]string file_ = "", [CallerLineNumber]int line_ = 0
            ) where T : Component
            => (T)FindChecked(obj_, typeof(T), path_, member_, file_, line_);

        public static Component AddChecked(this Component comp_, Type type_, string path_ = null,
            [CallerMemberName]string member_ = "", [CallerFilePath]string file_ = "", [CallerLineNumber]int line_ = 0
            ) {
            Log.Assert(comp_ != null, "{0} called on null object",
                arg0_:nameof(AddChecked), member_: member_, filePath_: file_, lineNumber_: line_);
            var t = path_ == null ? comp_.transform : FindChecked(comp_, path_);
            Log.Assert(t != null, LogNotFound,
                arg0_:path_, arg1_: comp_.name, member_: member_, filePath_: file_, lineNumber_: line_);
            var ret = t.GetComponent(type_);
            return ret ?? t.gameObject.AddComponent(type_);
        }

        public static T AddChecked<T>(this Component comp_, string path_ = null,
            [CallerMemberName]string member_ = "", [CallerFilePath]string file_ = "", [CallerLineNumber]int line_ = 0
            ) where T : Component {
            return (T)AddChecked(comp_, typeof(T), path_, member_, file_, line_);
        }

        public static T AddChecked<T>(this GameObject obj_, string path_ = null,
            [CallerMemberName]string member_ = "", [CallerFilePath]string file_ = "", [CallerLineNumber]int line_ = 0
            ) where T : Component {
            return (T)AddChecked(obj_.transform, typeof(T), path_, member_, file_, line_);
        }

        public static Component GetChecked(this Component comp_, Type type_,
            [CallerMemberName]string member_ = "", [CallerFilePath]string file_ = "", [CallerLineNumber]int line_ = 0
            )  {
            Log.Assert(comp_ != null, "{0} called on null object",
                arg0_:nameof(GetChecked), member_: member_, filePath_: file_, lineNumber_: line_);
            var ret = comp_.GetComponent(type_);
            if(ret == null) {
                Log.Warning("Expected component of type {0} not found on {1}!",
                    arg0_:type_, arg1_: comp_.name, member_: member_, filePath_: file_, lineNumber_: line_);
                ret = comp_.gameObject.AddComponent(type_);
            }
            return ret;
        }

        public static T GetChecked<T>(this Component comp_,
            [CallerMemberName]string member_ = "", [CallerFilePath]string file_ = "", [CallerLineNumber]int line_ = 0
            ) where T : Component {
            return (T)GetChecked(comp_, typeof(T), member_, file_, line_);
        }

        public static T GetChecked<T>(this GameObject obj_,
            [CallerMemberName]string member_ = "", [CallerFilePath]string file_ = "", [CallerLineNumber]int line_ = 0
            ) where T : Component {
            return (T)GetChecked(obj_?.transform, typeof(T), member_, file_, line_);
        }

        public static Transform AddChild(this Transform transform_, string name_, Transform child_ = null,
            [CallerMemberName]string member_ = "", [CallerFilePath]string file_ = "", [CallerLineNumber]int line_ = 0
            ) {
            Log.Assert(transform_ != null, "{0} called on null object",
                arg0_:nameof(AddChild), member_: member_, filePath_: file_, lineNumber_: line_);
            if (child_ == null) {
                child_ = new GameObject(name_).transform;
            }
            else {
                child_.name = name_;
            }
            child_.SetParent(transform_, false);
            child_.localPosition = Vector3.zero;
            child_.rotation = Quaternion.identity;
            //child_.localScale = Vector3.one;
            return child_;
        }

        public static RectTransform AddChildRect(this Transform transform_, string name_, RectTransform child_ = null,
            [CallerMemberName]string member_ = "", [CallerFilePath]string file_ = "", [CallerLineNumber]int line_ = 0
            ) {
            Log.Assert(transform_ != null, "{0} called on null object",
                arg0_:nameof(AddChildRect), member_: member_, filePath_: file_, lineNumber_: line_);
            if (child_ == null) {
                child_ = new GameObject(name_).AddComponent<RectTransform>();
            }
            else {
                child_.name = name_;
            }
            //assume layer = UI
            child_.gameObject.layer = LayerMask.NameToLayer("UI");
            child_.SetParent(transform_, false);
            child_.localPosition = Vector3.zero;
            child_.rotation = Quaternion.identity;
            child_.anchorMin = new Vector2(0, 0);
            child_.anchorMax = new Vector2(1, 1);
            child_.sizeDelta = new Vector2(0, 0);
            child_.pivot = new Vector2(0.5f, 0.5f);
            return child_;
        }

        public static bool OrientationEqual(Quaternion a, Quaternion b) {
            var dot = 1 - Mathf.Abs(Quaternion.Dot(a, b));
            return dot < 1e-4f;
        }

        public static void TrySetActive(Component comp_, bool value_) {
            if(comp_ == null) return;
            comp_.gameObject.SetActive(value_);
        }

        public static void TrySetActive(GameObject obj_, bool value_) {
            if(obj_ == null) return;
            obj_.SetActive(value_);
        }
    }
}
