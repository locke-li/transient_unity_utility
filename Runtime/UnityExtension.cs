//
// Unity built-in type extension methods
// locating info should redirect to the actual call site
//
// author: liyingbo
//

using System.Runtime.CompilerServices;
using Transient;

namespace UnityEngine {
    public static class UnityExtension {
        public static T GameObjectFindChecked<T>(
            string path_,
            [CallerMemberName]string member_ = "",
            [CallerFilePath]string file_ = "",
            [CallerLineNumber]int line_ = 0
            ) where T : Component {
            var go = GameObject.Find(path_);
            Log.Assert(
                go != null, "GameObject {0} not found!",
                path_, null, null,
                member_, file_, line_
                );
            T ret = go.GetComponent<T>();
            Log.Assert(
                ret != null, "Component {0} not found on {1}",
                typeof(T), path_, null,
                member_, file_, line_
                );
            return ret;
        }

        public static T GameObjectTryFind<T>(
            string path_,
            [CallerMemberName] string member_ = "",
            [CallerFilePath] string file_ = "",
            [CallerLineNumber] int line_ = 0
            ) where T : Component {
            var go = GameObject.Find(path_);
            if (go is null) {
                return null;
            }
            return go.GetComponent<T>();
        }

        private static string LogNotFound => "Path: <{0}> on {1} not found!";

        public static Transform FindChecked(
            this Transform transform_, string path_,
            [CallerMemberName]string member_ = "",
            [CallerFilePath]string file_ = "",
            [CallerLineNumber]int line_ = 0
            ) {
            Log.Assert(
                transform_ != null, "{0} called on null object",
                nameof(FindChecked), null, null,
                member_, file_, line_
                );
            Transform ret = transform_.Find(path_);
            Log.Assert(
                ret != null, LogNotFound,
                path_, transform_.name, null,
                member_, file_, line_
                );
            return ret;
        }

        public static Transform FindChecked(
            this GameObject gameObject_, string path_,
            [CallerMemberName]string member_ = "",
            [CallerFilePath]string file_ = "",
            [CallerLineNumber]int line_ = 0
            ) {
            Log.Assert(
                gameObject_ != null, "{0} called on null object",
                nameof(FindChecked), null, null,
                member_, file_, line_
                );
            Transform ret = gameObject_.transform.Find(path_);
            Log.Assert(
                ret != null, LogNotFound,
                path_, gameObject_.name, null,
                member_, file_, line_
                );
            return ret;
        }

        public static T AddChecked<T>(
            this Transform transform_, string path_,
            [CallerMemberName]string member_ = "",
            [CallerFilePath]string file_ = "",
            [CallerLineNumber]int line_ = 0
            ) where T : Component {
            Log.Assert(
                transform_ != null, "{0} called on null object",
                nameof(AddChecked), null, null,
                member_, file_, line_
                );
            Transform t = FindChecked(transform_, path_);
            Log.Assert(
                t != null, LogNotFound,
                path_, transform_.name, null,
                member_, file_, line_
                );
            T ret = AddChecked<T>(t, member_, file_, line_);
            return ret;
        }

        public static T AddChecked<T>(
            this Transform transform_,
            [CallerMemberName]string member_ = "",
            [CallerFilePath]string file_ = "",
            [CallerLineNumber]int line_ = 0
            ) where T : Component {
            Log.Assert(
                transform_ != null, "{0} called on null object",
                nameof(AddChecked), null, null,
                member_, file_, line_
                );
            T ret = transform_.GetComponent<T>();
            return ret ?? transform_.gameObject.AddComponent<T>();
        }

        public static T GetChecked<T>(
            this Transform transform_,
            [CallerMemberName]string member_ = "",
            [CallerFilePath]string file_ = "",
            [CallerLineNumber]int line_ = 0
            ) where T : Component {
            Log.Assert(
                transform_ != null, "{0} called on null object",
                nameof(GetChecked), null, null,
                member_, file_, line_
                );
            T ret = transform_.GetComponent<T>();
            if(ret == null) {
                Log.Warning(
                    "Expected component of type {0} not found on {1}!",
                    typeof(T), transform_.name, null,
                    member_, file_, line_
                    );
                ret = transform_.gameObject.AddComponent<T>();
            }
            return ret;
        }

        public static T GetChecked<T>(
            this GameObject obj_,
            [CallerMemberName]string member_ = "",
            [CallerFilePath]string file_ = "",
            [CallerLineNumber]int line_ = 0
            ) where T : Component {
            return GetChecked<T>(obj_?.transform, member_, file_, line_);
        }

        public static T GetChecked<T>(
            this Component obj_,
            [CallerMemberName] string member_ = "",
            [CallerFilePath] string file_ = "",
            [CallerLineNumber] int line_ = 0
            ) where T : Component {
            return GetChecked<T>(obj_?.transform, member_, file_, line_);
        }

        public static T FindChecked<T>(
            this Transform transform_, string path_,
            [CallerMemberName]string member_ = "",
            [CallerFilePath]string file_ = "",
            [CallerLineNumber]int line_ = 0
            ) where T : Component {
            Transform t = FindChecked(transform_, path_);
            Log.Assert(t != null, LogNotFound, path_, transform_.name, null, member_, file_, line_);
            return GetChecked<T>(t, member_, file_, line_);
        }

        public static T FindChecked<T>(
            this GameObject obj_, string path_,
            [CallerMemberName]string member_ = "",
            [CallerFilePath]string file_ = "",
            [CallerLineNumber]int line_ = 0
            ) where T : Component {
            return FindChecked<T>(obj_?.transform, path_, member_, file_, line_);
        }

        public static T FindChecked<T>(
            this Component obj_, string path_,
            [CallerMemberName] string member_ = "",
            [CallerFilePath] string file_ = "",
            [CallerLineNumber] int line_ = 0
            ) where T : Component {
            return FindChecked<T>(obj_?.transform, path_, member_, file_, line_);
        }

        public static Transform AddChild(
            this Transform transform_, string name_, Transform child_ = null,
            [CallerMemberName]string member_ = "",
            [CallerFilePath]string file_ = "",
            [CallerLineNumber]int line_ = 0
            ) {
            Log.Assert(
                transform_ != null, "{0} called on null object",
                nameof(AddChild), null, null,
                member_, file_, line_
                );
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

        public static RectTransform AddChildRect(
            this Transform transform_, string name_, RectTransform child_ = null,
            [CallerMemberName]string member_ = "",
            [CallerFilePath]string file_ = "",
            [CallerLineNumber]int line_ = 0
            )
        {
            Log.Assert(
                transform_ != null, "{0} called on null object",
                nameof(AddChildRect), null, null,
                member_, file_, line_
                );
            if (child_ == null)
            {
                child_ = new GameObject(name_).AddComponent<RectTransform>();
            }
            else
            {
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
    }
}
