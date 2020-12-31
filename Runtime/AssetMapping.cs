//
//  Author : li.yingbo
//  Email : liveitbe@126.com
//
//
using UnityEngine;
using Transient.SimpleContainer;
using System;
using Generic = System.Collections.Generic;

namespace Transient.DataAccess {
    public sealed class AssetMapping {
        const int ItemPerRecycle = 64;
        const int ItemPerRecycleIncrement = 32;
        public Transform ActiveObjectRoot { get; set; }
        public Transform RecycleObjectRoot { get; set; }
        public static AssetMapping Default { get; private set; }
        public static AssetMapping View { get; private set; }
        public static (string, string) AssetEmpty = ("_internal_", "_empty_");
        internal static AssetMappingComparer _comparer;

        readonly Dictionary<(string, string), AssetIdentifier> _pool;
        readonly Dictionary<object, AssetIdentifier> _activePool;//objects in the scene
        readonly Dictionary<AssetIdentifier, List<object>> _recyclePool;//recyclable(resetable) disabled objects

        private AssetMapping(int category_, int active_, int recycle_) {
            _pool = new Dictionary<(string, string), AssetIdentifier>(category_, _comparer);
            _activePool = new Dictionary<object, AssetIdentifier>(active_);
            _recyclePool = new Dictionary<AssetIdentifier, List<object>>(recycle_, _comparer);
        }

        public static void Init() {
            _comparer = new AssetMappingComparer();
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
        public T TakeDirect<T>(string c, string i, string ext_ = null) => (T)AssetAdapter.Take(c, i, typeof(T), ext_);

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
            if (!_pool.TryGetValue((c, i), out var resi)) {
                Performance.RecordProfiler(nameof(AssetIdentifier));
                resi = new AssetIdentifier(c, i, typeof(T));
                resi.Load();//load for the first time
                _pool.Add((c, i), resi);//add to pool
                Performance.End(nameof(AssetIdentifier), true, $"first load:{c}_{i}");
            }
            if (!ins) {
                retv = resi.Mapped;
            }
            else if (!TryGetRecycle(resi, out retv)) {//look for reusable fails, load pool prototype
                retv = resi.Instantiate();
            }
            if (retv == null) {
                Log.Warning($"load failed {c}_{i}");
                return null;
            }
            if (ins) ActivateObject(retv, resi);
            Performance.End(nameof(Take));
            return retv as T;
        }

        bool TryGetRecycle(AssetIdentifier resId, out object resObject) {
            if (_recyclePool.TryGetValue(resId, out var recyclables)) {
                if (recyclables.Count > 0) {
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
            if (resObject is GameObject rObj) {
                rObj.transform.SetParent(ActiveObjectRoot, false);
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
            if (!_recyclePool.ContainsKey(resId)) {
                recyclables = new List<object>(ItemPerRecycle, ItemPerRecycleIncrement);
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
            foreach (var (_, stack) in _recyclePool) {
                while (stack.Count > 0) {
                    if (stack.Pop() is GameObject obj) GameObject.Destroy(obj);
                }
            }
            _recyclePool.Clear();
            foreach (var (key, _) in _activePool) {
                if (key is GameObject obj) GameObject.Destroy(obj);
            }
            _activePool.Clear();
        }
    }

    internal sealed class AssetMappingComparer :
        Generic.IEqualityComparer<AssetIdentifier>,
        Generic.IEqualityComparer<(string, string)>
        {
        public bool Equals(AssetIdentifier a, AssetIdentifier b) => a.category == b.category && a.id == b.id;
        public int GetHashCode(AssetIdentifier i) => i.category.GetHashCode() ^ i.id.GetHashCode();

        public bool Equals((string, string) a, (string, string) b) => a.Item1 == b.Item1 && a.Item2 == b.Item2;
        public int GetHashCode((string, string) i) => i.GetHashCode();
    }

    public static class AssetAdapter {
        public static string PackedDir { get; private set; } = "packed";
        public static string StagingDir { get; private set; } = "resources_staging";
        public static string DeployPath { get; private set; }
        public static string PackedPath { get; private set; }

#if UNITY_EDITOR
        public static string PackedPathFromAssets { get; private set; }
        public static string StagingPathFromAssets { get; private set; }
        private static readonly Generic.Dictionary<Type, string> TypeToExtension = new Generic.Dictionary<Type, string>() {
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
            PackedDir = packedDir ?? PackedDir;
            StagingDir = stagingDir ?? StagingDir;
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

        public static object Take(string category_, string id_, Type type_, string ext_ = null) {
            var path = string.IsNullOrEmpty(category_) ? id_ : $"{category_}_{id_}";
            var ret = SearchPacked(path, type_, ext_) ?? Resources.Load(path, type_) ?? ExtendedSearch?.Invoke(path, type_);
            return ret;
        }

        private static object SearchPacked(string path_, Type type_, string ext_) {
#if UNITY_EDITOR
            if (ext_ == null && !TypeToExtension.TryGetValue(type_, out ext_)) {
                return null;
            }
            var file = $"{path_}.{ext_}";
            return UnityEditor.AssetDatabase.LoadAssetAtPath($"{PackedPathFromAssets}{file}", type_)
                    ?? UnityEditor.AssetDatabase.LoadAssetAtPath($"{StagingPathFromAssets}{file}", type_)
                    ?? UnityEditor.AssetDatabase.LoadAssetAtPath($"Assets/{file}", type_);
#endif
            //TODO
            return null;

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
            category = category_ ?? string.Empty;
            id = id_;
            _type = type_;
            Mapped = null;
            Raw = null;
            InstantiateI = null;
        }

        internal void Load() {
            if (AssetAdapter.TryGetTypeCoalescing(category, out var lt)) {
                InstantiateI = lt.Inst;
                Raw = AssetAdapter.Take(category, id, lt.targetType);
                if (Raw != null) Mapped = lt.Mapping(category, Raw);
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
            for (int y = 0; y < _pool.Count; ++y) {
                if (!_pool[y].IsAlive(false)) {//assume the root particle system survive longest, for performance
                    _mapping.Recycle(_pool[y].gameObject);
                    _pool.RemoveAt(y);
                    --y;
                }
            }
        }

        public void Clear() => _pool.Clear();
    }
}
