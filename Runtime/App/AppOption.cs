using Transient;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Text;

namespace Transient {
    using DataMap = Dictionary<string, string>;

    public class AppOption {
        public DataMap data = new DataMap();
        public string this[string key] => InfoString(key);

        public static (AppOption, string) Create(byte[] content) {
            return Deserialize(content, null);
        }

        public (bool, string) Parse(byte[] content) {
            var (option, reason) = Deserialize(content, this);
            return (reason == null, reason);
        }

        public static (AppOption, string) Deserialize(byte[] content, AppOption target) {
            var parser = InfoParser.Instance;
            target = target ?? new AppOption();
            foreach (var (k, v) in parser.Deserialize(content)) {
                target.data.Add(k, v); break;
            }
            return (target, null);
        }

        public byte[] Serialize() {
            var parser = InfoParser.Instance;
            IEnumerable<(string, string)> SerializeEach() {
                foreach (var (k, v) in data) {
                    yield return (k, v);
                }
            }
            return parser.Serialize(SerializeEach());
        }

        public void Reset() {
            data.Clear();
        }

        public void Set(string k, string v) {
            data[k] = v;
        }

        public string InfoString(string key) {
            if (data.TryGetValue(key, out var v)) return v;
            return null;
        }

        public bool InfoBool(string key) {
            if (data.TryGetValue(key, out var v)) return v == "true";
            return false;
        }

        public (bool, int) InfoInt(string key) {
            if (data.TryGetValue(key, out var v) && int.TryParse(v, out var vv)) return (true, vv);
            return (false, 0);
        }

        public (bool, float) InfoFloat(string key) {
            if (data.TryGetValue(key, out var v) && float.TryParse(v, out var vv)) return (true, vv);
            return (false, 0.0f);
        }
    }
}