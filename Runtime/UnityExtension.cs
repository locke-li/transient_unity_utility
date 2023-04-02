//
// Unity built-in type extension methods
// locating info should redirect to the actual call site
//
// author: liyingbo
//

using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using Transient;

namespace UnityEngine {
    public static class UnityExtension {
        public static Component GameObjectFindChecked(Type type_, string path_,
            [CallerMemberName]string member_ = "", [CallerFilePath]string file_ = "", [CallerLineNumber]int line_ = 0
            ) {
            var go = GameObject.Find(path_);
            Log.Assert(go != null)?.Message($"GameObject {path_} not found!",
                stackDepth_:1, member_:member_, filePath_:file_, lineNumber_:line_);
            var ret = go.GetComponent(type_);
            Log.Assert(ret != null)?.Message($"Component {type_} not found on {path_}",
                stackDepth_:1, member_: member_, filePath_: file_, lineNumber_: line_);
            return ret;
        }

        public static T GameObjectFindChecked<T>(string path_,
            [CallerMemberName]string member_ = "", [CallerFilePath]string file_ = "", [CallerLineNumber]int line_ = 0
            ) where T : Component
            => (T)GameObjectFindChecked(typeof(T), path_, member_, file_, line_);

        public static Component GameObjectTryFind(string path_, Type type_) {
            var go = GameObject.Find(path_);
            if (go is null) {
                return null;
            }
            return go.GetComponent(type_);
        }

        public static T GameObjectTryFind<T>(string path_) where T : Component
            => (T)GameObjectTryFind(path_, typeof(T));

        public static Transform FindChecked(this Component comp_, string path_,
            [CallerMemberName]string member_ = "", [CallerFilePath]string file_ = "", [CallerLineNumber]int line_ = 0
            ) {
            Log.Assert(comp_ != null)?.Message($"{nameof(FindChecked)} called on null object",
                stackDepth_:1, member_: member_, filePath_: file_, lineNumber_: line_);
            var ret = comp_.transform.Find(path_);
            Log.Assert(ret != null)?.Message($"path {path_} on {comp_.name} not found",
                stackDepth_:1, member_: member_, filePath_: file_, lineNumber_: line_);
            return ret;
        }

        public static Transform FindChecked(this GameObject obj_, string path_,
            [CallerMemberName]string member_ = "", [CallerFilePath]string file_ = "", [CallerLineNumber]int line_ = 0
            ) {
            Log.Assert(obj_ != null)?.Message($"{nameof(FindChecked)} called on null object",
                stackDepth_:1, member_: member_, filePath_: file_, lineNumber_: line_);
            var ret = obj_.transform.Find(path_);
            Log.Assert(ret != null)?.Message($"path {path_} on {obj_.name} not found",
                stackDepth_:1, member_:member_, filePath_:file_, lineNumber_:line_);
            return ret;
        }

        public static Component FindChecked(this Component comp_, Type type_, string path_, 
            [CallerMemberName]string member_ = "", [CallerFilePath]string file_ = "", [CallerLineNumber]int line_ = 0
            ) {
            var t = FindChecked(comp_, path_);
            Log.Assert(t != null)?.Message($"path {path_} on {comp_.name} not found",
                stackDepth_:1, member_:member_, filePath_:file_, lineNumber_:line_);
            return GetChecked(t, type_, member_, file_, line_);
        }

        public static Component TryFind(this Component comp_, Type type_, string path_) {
            var t = comp_.transform.Find(path_);
            if (t == null) return null;
            return t.GetComponent(type_);
        }

        public static T TryFind<T>(this Component comp_, string path_) where T : Component {
            return (T)TryFind(comp_, typeof(T), path_);
        }

        public static Component FindChecked(this GameObject obj_, Type type_, string path_, 
            [CallerMemberName]string member_ = "", [CallerFilePath]string file_ = "", [CallerLineNumber]int line_ = 0
            ) {
            var t = FindChecked(obj_, path_);
            Log.Assert(t != null)?.Message($"path {path_} on {obj_.name} not found",
                stackDepth_:1, member_:member_, filePath_:file_, lineNumber_:line_);
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
            Log.Assert(comp_ != null)?.Message($"{nameof(AddChecked)} called on null object",
                stackDepth_:1, member_:member_, filePath_:file_, lineNumber_:line_);
            var t = path_ == null ? comp_.transform : FindChecked(comp_, path_);
            Log.Assert(t != null)?.Message($"path {path_} on {comp_.name} not found",
                stackDepth_:1, member_:member_, filePath_:file_, lineNumber_:line_);
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
            Log.Assert(comp_ != null)?.Message($"{nameof(GetChecked)} called on null object",
                stackDepth_:1, member_:member_, filePath_:file_, lineNumber_:line_);
            var ret = comp_.GetComponent(type_);
            if(ret == null) {
                Log.Warn($"Expected component of type {type_} not found on {comp_.name}!",
                    member_:member_, filePath_:file_, lineNumber_:line_);
                ret = comp_.gameObject.AddComponent(type_);
            }
            return ret;
        }

        public static Component GetChecked(this GameObject obj_, Type type_,
            [CallerMemberName] string member_ = "", [CallerFilePath] string file_ = "", [CallerLineNumber] int line_ = 0
            ) {
            Log.Assert(obj_ != null)?.Message($"{nameof(GetChecked)} called on null object",
                stackDepth_:1, member_: member_, filePath_: file_, lineNumber_: line_);
            var ret = obj_.GetComponent(type_);
            if (ret == null) {
                Log.Warn($"Expected component of type {type_} not found on {obj_.name}!",
                    member_: member_, filePath_: file_, lineNumber_: line_);
                ret = obj_.AddComponent(type_);
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
            return (T)GetChecked(obj_, typeof(T), member_, file_, line_);
        }

        public static Transform AddChild(this Transform transform_, string name_, Transform child_ = null,
            [CallerMemberName]string member_ = "", [CallerFilePath]string file_ = "", [CallerLineNumber]int line_ = 0
            ) {
            Log.Assert(transform_ != null)?.Message($"{nameof(AddChild)} called on null object",
                stackDepth_:1, member_: member_, filePath_: file_, lineNumber_: line_);
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
            Log.Assert(transform_ != null)?.Message($"{nameof(AddChildRect)} called on null object",
                stackDepth_:1, member_: member_, filePath_: file_, lineNumber_: line_);
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

        public static void GetChildren(this Transform transform_, List<Transform> cache_) {
            cache_.Clear();
            for(int k = 0; k < transform_.childCount; ++k) {
                cache_.Add(transform_.GetChild(k));
            }
        }

        public static void DestroyChildren(this Component comp_) {
            var transform = comp_.transform;
            for(int k = 0; k < transform.childCount; ++k) {
                GameObject.Destroy(transform.GetChild(k).gameObject);
            }
        }

        public static Transform Duplicate(this Transform transform_) {
            var obj = GameObject.Instantiate<GameObject>(transform_.gameObject);
            obj.transform.SetParent(transform_.parent, false);
            return obj.transform;
        }

        public static RectTransform Duplicate(this RectTransform transform_) {
            var obj = GameObject.Instantiate<GameObject>(transform_.gameObject);
            obj.transform.SetParent(transform_.parent, false);
            return (RectTransform)obj.transform;
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
