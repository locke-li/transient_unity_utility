﻿//
//  Author : li.yingbo
//  Email : liveitbe@126.com
//
//
using UnityEngine;
using System.Collections.Generic;
using System;
using Transient.Container;

namespace Transient.DataAccess {
    public sealed class AssetMapping {
        public Transform ActiveObjectRoot { get; set; }
        public Transform RecycleObjectRoot { get; set; }
        private static List<AssetMapping> _mapping;
        public static AssetMapping Default { get; private set; }
        public static AssetMapping View { get; private set; }
        public static string AssetEmpty { get; private set; } = "_empty_";
        internal static AssetMappingComparer _comparer;

        readonly Dictionary<(string, Type), AssetIdentifier> _pool;
        readonly Dictionary<object, AssetIdentifier> _activePool;//objects in the scene
        readonly Dictionary<AssetIdentifier, List<object>> _recyclePool;//recyclable(resetable) disabled objects

        private AssetMapping(int capacity_, int active_, int recycle_) {
            _pool = new(capacity_, _comparer);
            _activePool = new(active_);
            _recyclePool = new(recycle_, _comparer);
        }

        public static void Init() {
            Performance.RecordProfiler(nameof(AssetMapping));
            _mapping = new(4);
            _comparer = new();
            Default = Create(16, 512, 512);
            View = Create(8, 32, 64);
            Default.AddInternal(AssetEmpty, new GameObject());//TODO don't create
            Performance.End(nameof(AssetMapping));
        }

        public static AssetMapping Create(int capacity_, int active_, int recycle_) {
            var ret = new AssetMapping(capacity_, active_, recycle_);
            _mapping.Add(ret);
            return ret;
        }

        public static void Destroy() {
            AssetAdapter.PackSearch = null;
            if (_mapping == null) return;
            foreach(var m in _mapping) {
                m.Clear();
            }
            _mapping = null;
        }

        public static void RootObject(Transform active, Transform recycle) {
            foreach(var m in _mapping) {
                m.ActiveObjectRoot = active;
                m.RecycleObjectRoot = recycle;
            }
        }

        public void AddInternal(string id, GameObject obj) {
            obj.hideFlags = HideFlags.HideAndDontSave;
            _pool.Add((id, typeof(GameObject)), new AssetIdentifier(obj));
        }

        public T TakePersistent<T>(string id, bool try_ = false) where T : UnityEngine.Object
            => (T)TakePersistent(typeof(T), id, try_);
        public object TakePersistent(Type type_, string id, bool try_ = false) {
            var obj = (UnityEngine.Object)AssetAdapter.Take(id, type_, try_);
            if (obj == null) {
                Log.Warn($"failed to take persistent {id}");
                return null;
            }
            return UnityEngine.Object.Instantiate(obj);
        }

        public T TakeDirect<T>(string id, bool try_ = false)
            => (T)TakeDirect(typeof(T), id, try_);
        public object TakeDirect(Type type_, string id, bool try_ = false) {
            var ret = AssetAdapter.Take(id, type_, try_);
            if (!try_ && ret == null) {
                Log.Warn($"failed to directly take {id}");
            }
            return ret;
        }

        public T TakeAs<T>(string id, bool ins = true) where T : Component => TakeActive(id, ins).GetChecked<T>();

        public GameObject TakeActive(string id, bool ins = true) {
            var ret = Take<GameObject>(id, ins);
            ret?.SetActive(true);
            return ret;
        }

        public GameObject TakeEmpty() => Take<GameObject>(AssetEmpty, true);

        public T Take<T>(string id, bool ins = true, bool try_ = false) where T : class
            => Take(typeof(T), id, ins, try_) as T;
        public object Take(Type type_, string id, bool ins = true, bool try_ = false) {
            Performance.RecordProfiler(nameof(Take));
            object retv;
            var key = (id, type_);
            if (!_pool.TryGetValue(key, out var resi)) {
                Performance.RecordProfiler(nameof(AssetIdentifier));
                resi = new AssetIdentifier(id, type_);
                resi.Load(try_);//load for the first time
                _pool.Add(key, resi);//add to pool
                Performance.End(nameof(AssetIdentifier), true, $"first load:{id}");
            }
            if (!ins) {
                retv = resi.Mapped;
            }
            else if (!TryGetRecycle(resi, out retv)) {//look for reusable fails, load pool prototype
                retv = resi.Instantiate();
            }
            if (retv == null) {
                return null;
            }
            if (ins) ActivateObject(retv, resi);
            Performance.End(nameof(Take));
            return retv;
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

        private void Recycle(GameObject go_) {
            go_.SetActive(false);
            #if UNITY_EDITOR
            //to keep hierarchy clean
            go_.transform.SetParent(RecycleObjectRoot, false);
            #else
            if (go_.transform.parent != null) {
                go_.transform.SetParent(null, false);
            }
            #endif
        }

        public void Recycle(object resObj_, GameObject go_) {
            Recycle(go_);
            Recycle(resObj_);
        }

        public bool Recycle(object resObj_) {
            Performance.RecordProfiler(nameof(Recycle));
            if (resObj_ == null) {
                Log.Warn("trying to recycle null object");
                return false;
            }
            var recognized = _activePool.TryGetValue(resObj_, out var resId);
            if (resObj_ is GameObject obj) {
                if (recognized) {
                    Recycle(obj);
                }
                else {
                    Log.Error($"recycling un-registered object {resObj_} {obj.transform.parent?.name}");
                    GameObject.Destroy(obj);
                    goto end;
                }
            }
            else if(!recognized) {
                Log.Error($"recycling un-registered object {resObj_}");
                goto end;
            }
            _activePool.Remove(resObj_);
            List<object> recyclables;
            if (!_recyclePool.ContainsKey(resId)) {
                recyclables = new List<object>(64);
                recyclables.Push(resObj_);
                _recyclePool.Add(resId, recyclables);
            }
            else {
                _recyclePool.TryGetValue(resId, out recyclables);
                Log.Assert(recyclables != null)?.Message("empty entry in recycle pool");
                recyclables.Push(resObj_);
            }
        end:
            Performance.End(nameof(Recycle));
            return recognized;
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
            GameObject.Destroy(ActiveObjectRoot?.gameObject);
            GameObject.Destroy(RecycleObjectRoot?.gameObject);
        }
    }

    internal sealed class AssetMappingComparer :
        IEqualityComparer<AssetIdentifier>,
        IEqualityComparer<(string, Type)>
        {
        public bool Equals(AssetIdentifier a, AssetIdentifier b) => a.id == b.id;
        public int GetHashCode(AssetIdentifier i) => i.id.GetHashCode();

        public bool Equals((string, Type) a, (string, Type) b) => a == b;
        public int GetHashCode((string, Type) i) => i.GetHashCode();
    }

    public static class AssetAdapter {
#if UNITY_EDITOR
        public static readonly Dictionary<Type, string[]> TypeToExtension = new() {
            { typeof(GameObject), new string[] { "prefab" } },
            { typeof(Material), new string[] { "mat" } },
            { typeof(Sprite), new string[] { "png", "jpg", "tga", "psd" } },
            { typeof(AudioClip), new string[] { "mp3", "ogg", "wav" } },
            { typeof(UnityEngine.Audio.AudioMixer), new string[] { "mixer" } },
        };
#endif

        public struct IdExtract {
            public string id;
            public string ext;
            public string sub;
        }

        public struct TypeCoalescing {
            public Func<object, object> Mapping;
            public Type targetType;
            public Func<object, object> Inst;
        }
        private static readonly Dictionary<string, TypeCoalescing> TypeCoalescingByCategory = new Dictionary<string, TypeCoalescing>(16, StringComparer.Ordinal);
        public static Func<string, string> Id2Category { get; set; } = id => id;

        public static void CategoryTypeCoalescing(string category_, Type type_, Func<object, object> mapping_, Func<object, object> inst_) {
            TypeCoalescingByCategory.Add(category_, new TypeCoalescing {
                Mapping = mapping_,
                targetType = type_,
                Inst = inst_
            });
        }

        public static bool TryGetTypeCoalescing(string id, out TypeCoalescing t_) =>
            TypeCoalescingByCategory.TryGetValue(Id2Category(id), out t_);

        public static Func<IdExtract, Type, bool, object> PackSearch { get; set; }
        public static Func<IdExtract, Type, bool, object> ExtendedSearch { get; set; }
        public static Func<string, IdExtract> ExtractId { get; set; } = id_ => {
            //format: [name].<ext>#<sub>
            string sub = null;
            string ext = null;
            var sep = id_.LastIndexOf("#");
            var dot = id_.LastIndexOf(".");
            if (dot > 0 && sep > 0) {
                ext = id_.Substring(dot + 1, sep - dot);
                sub = id_.Substring(sep + 1);
                id_ = id_.Substring(0, dot);
            }
            else if (sep > 0) {
                sub = id_.Substring(sep + 1);
                id_ = id_.Substring(0, sep);
            }
            else if (dot > 0) {
                ext = id_.Substring(dot + 1);
                id_ = id_.Substring(0, dot);
            }
            return new IdExtract() { id = id_, sub = sub, ext = ext };
        };

        public static object Take(string id_, Type type_, bool try_) {
            var id = ExtractId(id_);
            //try AssetBundles
            var ret = PackSearch?.Invoke(id, type_, try_);
            if (ret != null) return ret;
            //try Resources
            if (id.sub != null) {
                var batch = Resources.LoadAll(id.id, type_);
                foreach (var r in batch) {
                    if (r.name == id.sub) return r;
                }
            }
            else {
                ret = Resources.Load(id.id, type_);
                if (ret != null) return ret;
            }
            //try extra/fail-safe
            return ExtendedSearch?.Invoke(id, type_, try_);
        }
    }

    public struct AssetIdentifier {
        //non-null
        public readonly string id;
        public object Mapped { get; private set; }
        public object Raw { get; private set; }
        public readonly Type type;
        Func<object, object> InstantiateI;

        public object Instantiate() => InstantiateI(Mapped);

        public AssetIdentifier(UnityEngine.Object obj) {
            id = string.Empty;
            type = obj.GetType();
            Raw = Mapped = obj;
            InstantiateI = InstantiateUnityObject;
        }

        public AssetIdentifier(string id_, Type type_) {
            id = id_;
            type = type_;
            Mapped = null;
            Raw = null;
            InstantiateI = null;
        }

        internal bool Load(bool try_) {
            if (AssetAdapter.TryGetTypeCoalescing(id, out var lt)) {
                InstantiateI = lt.Inst;
                Raw = AssetAdapter.Take(id, lt.targetType, try_);
                if (Raw != null) Mapped = lt.Mapping(Raw);
            }
            else {
                InstantiateI = InstantiateUnityObject;
                Raw = AssetAdapter.Take(id, type, try_);
                Mapped = Raw;
            }
            if (Mapped == null) {
                InstantiateI = InstantiateEmpty;
                if (!try_) Log.Warn($"failed to load {id} {type}");
                return false;
            }
            return true;
        }

        internal void Preload(object o_) {
            Mapped = o_;
            InstantiateI = InstantiateUnityObject;
        }

        private static readonly Func<object, object> InstantiateUnityObject =
            o => GameObject.Instantiate((UnityEngine.Object)o);

        private static readonly Func<object, object> InstantiateEmpty = o => null;
    }

    public sealed class AutoRecycler {
        const int AutoRecycleCap = 256;
        readonly AssetMapping _mapping;
        readonly List<ParticleSystem> _pool;

        public AutoRecycler(AssetMapping mapping_) {
            _mapping = mapping_;
            _pool = new(AutoRecycleCap);
        }

        public void RegisterWithMainLoop() {
            MainLoop.OnUpdate.Add(CheckRecycle, this);
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
