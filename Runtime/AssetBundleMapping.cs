//
//  Author : li.yingbo
//  Email : liveitbe@126.com
//
//
using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;
using System.Text;
using Transient.Container;

namespace Transient.DataAccess {
    [Serializable]
    public struct BundleInfo {
        public string name;
        public string hash;
        public string prefix;
        public string[] dep;
        public string[] asset;
    }

    [Serializable]
    public class AssetManifest {
        public List<BundleInfo> Info;
    }

    public sealed class AssetBundleMapping {
        public static AssetBundleMapping Default { get; private set; }

        private Dictionary<string, string> Alias2Name { get; set; }
        private Dictionary<string, BundleIdentifier> Asset2Bundle { get; set; } = new(512);
        private Dictionary<string, BundleIdentifier> BundlePool { get; set; } = new(128);
        private Dictionary<BundleIdentifier, float> ToRelease { get; set; } = new(64);
        private List<string> LoadingChain { get; set; } = new(8);
        private float ReleaseTimestamp = 0;
        public float ReleaseInterval { get; set; } = 1;
        public int ReleaseDelay { get; set; } = 30;
        public List<string> DataDir { get; private set; }
        public string DataExt { get; private set; }
        internal StringBuilder Builder { get; private set; } = new();

        public static void Init(List<string> path, string extension) {
            Default = new();
            Default.Setup(path, extension);
        }

        public static void Destroy() {
            Default?.Clear();
            Default = null;
            AssetBundle.UnloadAllAssetBundles(true);
            Resources.UnloadUnusedAssets();
        }

#if UNITY_EDITOR
        private static string KeyEnabled = $"{nameof(AssetBundleMapping)}.{nameof(Enabled)}";
        public static bool Enabled {
            get => UnityEditor.EditorPrefs.GetBool(KeyEnabled, false);
            set => UnityEditor.EditorPrefs.SetBool(KeyEnabled, value);
        }

        [ExtendableTool("AssetBundle", "Enable")]
        public static bool ToggleEnabled(bool? value) {
            if (value.HasValue) Enabled = value.Value;
            return Enabled;
        }
#else
        public static bool Enabled { get; set; } = true;
#endif

        public void Clear(bool unload = false) {
            if (unload && BundlePool != null) {
                foreach (var (_, identifier) in BundlePool) {
                    identifier.Release();
                }
            }
            Alias2Name = null;
            Asset2Bundle.Clear();
            BundlePool.Clear();
            ToRelease.Clear();
            MainLoop.OnUpdate.Remove(this);
        }

        public AssetBundleMapping Setup(List<string> path, string extension) {
            DataDir = path;
            DataExt = extension;
            return this;
        }

        public AssetManifest InitAssetList(string path_, string bundleAsset = null,
            bool byName = false, bool byAlias = false, bool byPath = true) {
            AssetManifest manifest = null;
            Builder.Length = 0;
            foreach (var dir in DataDir) {
                var path = Path.Combine(dir, path_);
                try {
                    string content;
                    if (bundleAsset != null) {
                        var byteContent = FileUtility.LoadBytes(path);
                        var bundle = AssetBundle.LoadFromMemory(byteContent);
                        var asset = bundle.LoadAsset<TextAsset>(bundleAsset);
                        content = Encoding.UTF8.GetString(asset.bytes);
                        bundle.Unload(true);
                    }
                    else {
                        content = File.ReadAllText(path);
                    }
                    manifest = JsonUtility.FromJson<AssetManifest>(content);
                }
                catch (Exception e) {
                    Builder.Append(path).Append("|").AppendLine(e.Message);
                }
                if (manifest != null) {
                    InitAssetList(manifest, byName, byAlias, byPath);
                    goto end;
                }
            }
            Log.Warn($"failed to load asset list:\n{Builder}");
        end:
            Builder.Length = 0;
            return manifest;
        }

        public void InitAssetList(AssetManifest assetManifest,
            bool byName = false, bool byAlias = false, bool byPath = true) {
            if (byAlias && Alias2Name == null) Alias2Name = new(512);
            MainLoop.OnUpdate.Remove(this);
            MainLoop.OnUpdate.Add(DelayedRelease, this);
            var skipAsset = !(byName || byAlias || byPath);
            foreach (var bundle in assetManifest.Info) {
                if (BundlePool.ContainsKey(bundle.name)) continue;
                var identifier = new BundleIdentifier() {
                    name = bundle.name,
                    hash = bundle.hash,
                    prefix = bundle.prefix,
                    dependency = bundle.dep,
                    asset = bundle.asset,
                };
                BundlePool.Add(bundle.name, identifier);
                if (skipAsset) continue;
                foreach (var path in bundle.asset) {
                    if (byPath) {
                        Asset2Bundle.Add(path, identifier);
                    }
                    if (byAlias || byName) {
                        var nameStart = path.LastIndexOf('/');
                        var nameEnd = path.LastIndexOf('.');
                        if (nameStart > 0 || nameEnd > 0) {//with any
                            nameStart = Mathf.Max(0, nameStart + 1);
                            nameEnd = nameEnd > 0 ? nameEnd : path.Length;
                            var name = path.Substring(nameStart, nameEnd - nameStart);
                            if (byAlias) {
                                var alias = path.Substring(0, nameEnd);
                                Asset2Bundle[alias] = identifier;
                                Alias2Name[alias] = name;
                                //Debug.Log($"{alias}|{name}");
                            }
                            if (byName) Asset2Bundle[name] = identifier;
                        }
                        else if(!byPath) {
                            Asset2Bundle[path] = identifier;
                        }
                    }
                }
            }
        }

        private bool ReadyInit(BundleIdentifier identifier) {
            if (LoadingChain.Contains(identifier.name)) return true;
            LoadingChain.Push(identifier.name);
            AssetBundle raw = null;
            if (identifier.dependency != null) {
                foreach (var dep in identifier.dependency) {
                    if (!BundlePool.TryGetValue(dep, out var identifierDep) || !Ready(identifierDep)) {
                        if (Builder.Length == 0) Builder.AppendLine($"bundle identifier {dep} not found");
                        Builder.Append("dependency fail:").AppendLine(dep);
                        goto result;
                    }
                }
            }
            var file = identifier.name + DataExt;
            foreach (var dir in DataDir) {
                var path = Path.Combine(dir, file);
                var byteContent = FileUtility.LoadBytes(path);
                if (byteContent != null) {
                    raw = AssetBundle.LoadFromMemory(byteContent);
                    if (raw != null) break;
                }
                if (raw == null) {
                    Builder.AppendLine(path);
                }
            }
        result:
            LoadingChain.Pop();
            var valid = identifier.Reset(raw);
            if (!valid) {
                Log.Warn($"failed to find bundle {identifier.name}:\n{Builder}");
            }
            return valid;
        }

        private bool Ready(BundleIdentifier identifier) {
            identifier.RefChange(1);
            var raw = identifier.Raw;
            var valid = identifier.valid;
            if (raw != null || !valid) return valid;
            Builder.Length = 0;
            valid = ReadyInit(identifier);
            Builder.Length = 0;
            Log.Info($"{identifier.name} ready = {valid}");
            return valid;
        }

        public BundleIdentifier BundleByAny(string key) {
            return BundleByName(key) ?? BundleByAsset(key);
        }
        public BundleIdentifier BundleByName(string name) {
            if(BundlePool.TryGetValue(name, out var identifier) && Ready(identifier)) {
                return identifier;
            }
            return null;
        }
        public BundleIdentifier BundleByAsset(string alias) {
            if(Asset2Bundle.TryGetValue(alias, out var identifier) && Ready(identifier)) {
                return identifier;
            }
            return null;
        }

        //take by path = with extension/directory
        public T TakeByPath<T>(string path) where T : UnityEngine.Object {
            var identifier = BundleByAsset(path);
            return identifier?.Raw.LoadAsset<T>($"{identifier.prefix}{path}");
        }
        public object TakeByPath(string path, Type type) {
            var identifier = BundleByAsset(path);
            return identifier?.Raw.LoadAsset($"{identifier.prefix}{path}", type);
        }

        //take by name = without extension/directory
        public T TakeByName<T>(string name) where T : UnityEngine.Object
            => BundleByAsset(name)?.Raw.LoadAsset<T>(name);
        public object TakeByName(string name, Type type)
            => BundleByAsset(name)?.Raw.LoadAsset(name, type);

        //take by alias = without extension/with directory
        public T TakeByAlias<T>(string alias) where T : UnityEngine.Object
            => BundleByAsset(alias)?.Raw.LoadAsset<T>(Alias2Name[alias]);
        public object TakeByAlias(string alias, Type type)
            => BundleByAsset(alias)?.Raw.LoadAsset(Alias2Name[alias], type);

        public T Take<T>(string alias, string name) where T : UnityEngine.Object
            => BundleByAsset(alias)?.Raw.LoadAsset<T>(name);
        public object Take(string alias, string name, Type type)
            => BundleByAsset(alias)?.Raw.LoadAsset(name, type);

        public T TakeFrom<T>(string bundle, string asset) where T : UnityEngine.Object
            => (T)TakeFrom(bundle, asset, typeof(T));
        public object TakeFrom(string bundle, string asset, Type type) {
            if (!BundlePool.TryGetValue(bundle, out var identifier)) {
                identifier = new() { name = bundle };
                BundlePool.Add(bundle, identifier);
            }
            if (!Ready(identifier)) {
                return null;
            }
            return identifier.Raw.LoadAsset(asset, type);
        }

        private void DelayedRelease(float _) {
            var time = Time.realtimeSinceStartup;
            if (time - ReleaseTimestamp < ReleaseInterval) return;
            ReleaseTimestamp = time;
            foreach(var (identifier, timestamp) in ToRelease) {
                if (!identifier.WillRelease) {
                    ToRelease.Remove(identifier);
                }
                else if (time - timestamp > ReleaseDelay) {
                    identifier.Release();
                }
            }
        }

        public bool Release(BundleIdentifier identifier) {
            identifier.RefChange(-1);
            if (identifier.WillRelease && !ToRelease.ContainsKey(identifier)) {
                ToRelease.Add(identifier, Time.realtimeSinceStartup);
            }
            return true;
        }
        public void ReleaseBundle(string bundle) {
            if(BundlePool.TryGetValue(bundle, out var identifier)) {
                Release(identifier);
            }
        }
        public void Release(string alias) {
            if(Asset2Bundle.TryGetValue(alias, out var identifier)) {
                Release(identifier);
            }
        }
    }

    public class BundleIdentifier {
        public string name;
        public string hash;
        public string prefix;
        public string[] dependency;
        public string[] asset;
        public AssetBundle Raw { get; private set; }
        public bool valid = true;
        internal int RefCount { get; private set; }
        internal bool WillRelease => RefCount <= 0 && Raw != null;

        internal bool Reset(AssetBundle raw_) {
            Raw = raw_;
            valid = raw_ != null;
            return valid;
        }

        internal int RefChange(int value_) {
            //Log.Debug($"ref change {name} {RefCount} {value_}");
            RefCount += value_;
            return RefCount;
        }

        internal void Release() {
            Log.Debug($"release bundle {name}");
            Raw?.Unload(false);
            valid = true;
            Raw = null;
        }
    }
}
