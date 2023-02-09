using Transient;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Text;
using DataMap = Transient.SimpleContainer.Dictionary<string, string>;

namespace Transient {
    public class AppOption {
        public static Func<byte[], AppOption, (AppOption, string)> Parser;
        public static char seperator = ':';

        public DataMap data = new DataMap(4);
        public string this[string key] => InfoString(key);

        public static (AppOption, string) Create(byte[] content) {
            return Deserialize(content, null);
        }

        public (bool, string) Parse(byte[] content) {
            var (option, reason) = Parser?.Invoke(content, this) ?? Deserialize(content, this);
            if (reason != null) return (false, reason);
            return (true, string.Empty);
        }

        public static (AppOption, string) Deserialize(byte[] content, AppOption target) {
            target = target ?? new AppOption();
            using var reader = new StreamReader(new MemoryStream(content));
            while (!reader.EndOfStream) {
                var l = reader.ReadLine();
                if (!l.Contains(seperator)) continue;
                var seg = l.Split(seperator, ' ', StringSplitOptions.RemoveEmptyEntries);
                if (seg.Length < 2) continue;
                target.data.Add(seg[0], seg[1]); break;
            }
            return (target, string.Empty);
        }

        public byte[] Serialize() {
            var builder = new StringBuilder();
            foreach (var (k, v) in data) {
                builder.Append(k).Append(seperator).Append(v).AppendLine();
            }
            return Encoding.UTF8.GetBytes(builder.ToString());//TODO optimize
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