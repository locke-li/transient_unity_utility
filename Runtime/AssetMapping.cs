//
//  Author : li.yingbo
//  Email : liveitbe@126.com
//
//
using UnityEngine;
using Transient.SimpleContainer;
using System;

namespace Transient.DataAccess {
    public sealed class AssetMapping : System.Collections.Generic.IEqualityComparer<AssetIdentifier> {
        const int ItemPerRecycle = 64;
        public Transform ActiveObjectRoot { get; set; }
        public Transform RecycleObjectRoot { get; set; }
        public static AssetMapping Default { get; private set; }
        public static AssetMapping View { get; private set; }
        public static (string, string) AssetEmpty = ("_internal_", "_empty_");

        readonly Dictionary<(string category, string id), AssetIdentifier> _pool;
        readonly Dictionary<object, AssetIdentifier> _activePool;//objects in the scene
        readonly Dictionary<AssetIdentifier, List<object>> _recyclePool;//recyclable(resetable) disabled objects

        private AssetMapping(int category_, int active_, int recycle_) {
            _pool = new Dictionary<(string, string), AssetIdentifier>(category_);
            _activePool = new Dictionary<object, AssetIdentifier>(active_, DefaultObjectEqualityComparer<object>.Default);
            _recyclePool = new Dictionary<AssetIdentifier, List<object>>(recycle_, this);
        }

        public static void Init() {
            Default = new AssetMapping(16, 512, 512);
            View = new AssetMapping(8, 32, 64);
            AssetAdapter.Redirect(null, null);
            Default.AddInternal(AssetEmpty, new GameObject());
        }

        public static void RootObject(Transform active, Transform recycle) {
            Default.ActiveObjectRoot = active;
            Default.RecycleObjectRoot = recycle;
            View.ActiveObjectRoot = active;
            View.RecycleObjectRoot = recycle;
        }

        public void AddInternal((string, string) AssetId, GameObject obj) {
            obj.hideFlags = HideFlags.HideAndDontSave;
            Default._pool.Add(AssetId, new AssetIdentifier(obj));
        }

        public T TakePersistent<T>(string c, string i) where T : UnityEngine.Object => UnityEngine.Object.Instantiate((T)AssetAdapter.Take(c, i, typeof(T)));
        public T TakeDirect<T>(string c, string i) => (T)AssetAdapter.Take(c, i, typeof(T));

        public T TakeAs<T>(string c, string i, bool ins = true) where T : Component => TakeActive(c, i, ins).GetChecked<T>();

        public GameObject TakeActive(string c, string i, bool ins = true) {
            var ret = Take<GameObject>(c, i, ins);
            ret.SetActive(true);
            return ret;
        }

        public GameObject TakeEmpty() => Take<GameObject>(AssetEmpty.Item1, AssetEmpty.Item2, true);

        public T Take<T>(string c, string i, bool ins = true) where T : class {
            Performance.RecordProfiler(nameof(Take));
            object retv;
            if(!_pool.TryGetValue((c, i), out var resi)) {
                Performance.RecordProfiler(nameof(AssetIdentifier));
                resi = new AssetIdentifier(c, i, typeof(T));
                resi.Load();//load for the first time
                _pool.Add((c, i), resi);//add to pool
                Performance.End(nameof(AssetIdentifier), true, $"first load:{c}_{i}");
            }
            if(!ins) {
                retv = resi.Mapped;
            }
            else if(!TryGetRecycle(resi, out retv)) {//look for reusable fails, load pool prototype
                retv = resi.Instantiate();
            }
            Log.Assert(retv != null, "load failed {0}_{1}", c, i);
            if(ins) ActivateObject(retv, resi);
            Performance.End(nameof(Take));
            return retv as T;
        }

        bool TryGetRecycle(AssetIdentifier resId, out object resObject) {
            if(_recyclePool.TryGetValue(resId, out var recyclables)) {
                if(recyclables.Count > 0) {
                    resObject = recyclables.Pop();
                    return resObject is object;
                }
            }
            resObject = null;
            return false;
        }

        void ActivateObject(object resObject, AssetIdentifier resId) {
            Performance.RecordProfiler(nameof(ActivateObject));
            _activePool.Add(resObject, resId);
            GameObject rObj;
            if((rObj = resObject as GameObject) is object) {
                rObj.transform.SetParent(ActiveObjectRoot, false);
                //rObj.SetActive(true);
            }
            Performance.End(nameof(ActivateObject));
        }

        public void Recycle(object resObj_, GameObject go_) {
            go_.SetActive(false);
            go_.transform.SetParent(RecycleObjectRoot, false);
            Recycle(resObj_);
        }

        public void Recycle(GameObject resObj_) {
            resObj_.SetActive(false);
            resObj_.transform.SetParent(RecycleObjectRoot, false);
            Recycle((object)resObj_);
        }

        public void Recycle(object resObj_) {
            Performance.RecordProfiler(nameof(Recycle));
            var registeredActiveObj = _activePool.TryGetValue(resObj_, out var resId);
            if (!registeredActiveObj) {
                Log.Error($"recycling un-registered object {resObj_}");
                //properly? handle un-registered object
                if (resObj_ is GameObject obj) {
                    GameObject.Destroy(obj);
                }
            }
            _activePool.Remove(resObj_);
            List<object> recyclables;
            if(!_recyclePool.ContainsKey(resId)) {
                recyclables = new List<object>(ItemPerRecycle, 32);//TODO optimize/move external
                recyclables.Push(resObj_);
                _recyclePool.Add(resId, recyclables);
            }
            else {
                _recyclePool.TryGetValue(resId, out recyclables);
                Log.Assert(recyclables != null, "empty entry in recycle pool");
                recyclables.Push(resObj_);
            }
            Performance.End(nameof(Recycle));
        }

        public void Clear() {
            List<object> stack;
            object o;
            foreach(var resi in _recyclePool) {
                stack = resi.Value;
                while(stack.Count > 0) {
                    o = stack.Pop();
                    if(o is GameObject) GameObject.Destroy((GameObject)o);
                }
            }
            _recyclePool.Clear();
            foreach(var assetId in _activePool) {
                o = assetId.Key;
                if(o is GameObject) GameObject.Destroy((GameObject)o);
            }
            _activePool.Clear();
        }

        #region IEqualityComparer<AssetIdentifier>

        public bool Equals(AssetIdentifier a, AssetIdentifier b) => a.category == b.category && a.id == b.id;

        public int GetHashCode(AssetIdentifier i) => i.category.GetHashCode()^i.id.GetHashCode();

        #endregion
    }

    public static class AssetAdapter {
        public static string PackedDir { get; private set; } = "packed";
        public static string StagingDir { get; private set; } = "resources_staging";
        public static string DeployPath { get; private set; }
        public static string PackedPath { get; private set; }

        #if UNITY_EDITOR
        public static string PackedPathFromAssets { get; private set; }
        public static string StagingPathFromAssets { get; private set; }
        private static readonly System.Collections.Generic.Dictionary<Type, string> TypeToExtension = new System.Collections.Generic.Dictionary<Type, string>() {
            { typeof(GameObject), "prefab" },
            { typeof(Material), "mat" },
            { typeof(Sprite), "png" },
        };
        #endif

        public struct TypeCoalescing {
            public Func<string, object, object> Mapping;
            public Type targetType;
            public Func<string, object, object> Inst;
        }
        private static readonly Dictionary<string, TypeCoalescing> TypeCoalescingByCategory = new Dictionary<string, TypeCoalescing>(16, StringComparer.Ordinal);

        public static void Redirect(string stagingDir, string packedDir) {
            PackedDir = packedDir??PackedDir;
            StagingDir = stagingDir??StagingDir;
            DeployPath = $"{Application.persistentDataPath}/{PackedDir}/";
            PackedPath = $"{Application.streamingAssetsPath}/{PackedDir}/";
            #if UNITY_EDITOR
            PackedPathFromAssets = $"Assets/StreamingAssets/{PackedDir}/";
            StagingPathFromAssets = $"Assets/{StagingDir}/";
            #endif
        }

        public static void CategoryTypeCoalescing(string category_, Type type_, Func<string, object, object> mapping_, Func<string, object, object> inst_) {
            TypeCoalescingByCategory.Add(category_, new TypeCoalescing {
                Mapping = mapping_,
                targetType = type_,
                Inst = inst_
            });
        }

        public static bool TryGetTypeCoalescing(string category_, out TypeCoalescing t_) => 
            TypeCoalescingByCategory.TryGetValue(category_, out t_);

        public static Func<string, Type, object> ExtendedSearch { get; set; } = (p, t) => {
            Debug.LogWarning($"{nameof(ExtendedSearch)} unavailable. requested:{p} ({t})");
            return null;
        };

        public static object Take(string category_, string id_, Type type_) {
            var path = string.IsNullOrEmpty(category_) ? id_ : $"{category_}_{id_}";
            var ret = SearchPacked(path, type_)??Resources.Load(path, type_)??ExtendedSearch?.Invoke(path, type_);
            return ret;
        }

        private static object SearchPacked(string path_, Type type_) {
#if UNITY_EDITOR
            //TODO Texture2D extension?
            if (!TypeToExtension.TryGetValue(type_, out var ext)) {
                return null;
            }
            return UnityEditor.AssetDatabase.LoadAssetAtPath($"{PackedPathFromAssets}{path_}.{ext}", type_)
                    ??UnityEditor.AssetDatabase.LoadAssetAtPath($"{StagingPathFromAssets}{path_}.{ext}", type_);
            #else
            //TODO
            return null;
            #endif
        }
    }

    public struct AssetIdentifier {
        //non-null
        public readonly string category;
        //non-null
        public readonly string id;
        public object Mapped { get; private set; }
        public object Raw { get; private set; }
        private readonly Type _type;
        Func<string, object, object> InstantiateI;

        public object Instantiate() => InstantiateI(category, Mapped);

        public AssetIdentifier(UnityEngine.Object obj) {
            category = string.Empty;
            id = string.Empty;
            _type = obj.GetType();
            Raw = Mapped = obj;
            InstantiateI = InstantiateUnityObject;
        }

        public AssetIdentifier(string category_, string id_, Type type_) {
            category = category_?? string.Empty;
            id = id_;
            _type = type_;
            Mapped = null;
            Raw = null;
            InstantiateI = null;
        }

        internal void Load() {
            if(AssetAdapter.TryGetTypeCoalescing(category, out var lt)) {
                InstantiateI = lt.Inst;
                Raw = AssetAdapter.Take(category, id, lt.targetType);
                if(Raw != null) Mapped = lt.Mapping(category, Raw);
            }
            else {
                InstantiateI = InstantiateUnityObject;
                Raw = AssetAdapter.Take(category, id, _type);
                Mapped = Raw;
            }
        }

        internal void Preload(object o_) {
            Mapped = o_;
            InstantiateI = InstantiateUnityObject;
        }

        private static readonly Func<string, object, object> InstantiateUnityObject = (c, o) => {
            object retv = null;
            retv = GameObject.Instantiate((UnityEngine.Object)o);
            return retv;
        };
    }

    public sealed class AutoRecycler {
        const int AutoRecycleCap = 256;
        readonly AssetMapping _mapping;
        readonly List<ParticleSystem> _pool;

        public AutoRecycler(AssetMapping mapping_) {
            _mapping = mapping_;
            _pool = new List<ParticleSystem>(AutoRecycleCap);
        }

        public void AutoRecycle(ParticleSystem ps) => _pool.Add(ps);

        public void CheckRecycle(float _) {
            for(int y = 0;y < _pool.Count;++y) {
                if(!_pool[y].IsAlive(false)) {//assume the root particle system survive longest, for performance
                    _mapping.Recycle(_pool[y].gameObject);
                    _pool.RemoveAt(y);
                    --y;
                }
            }
        }

        public void Clear() => _pool.Clear();
    }
}
