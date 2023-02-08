using Transient;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Transient {
    public class AppOption {
        public Dictionary<string, string> data;
        public string this[string key] => InfoString(key);

        public static (AppOption, string) Parse(string str) {
            var option = new AppOption();
            try {
                JsonUtility.FromJsonOverwrite(str, option);
            }
            catch (Exception e) {
                return (option, e.Message);
            }
            return (option, string.Empty);
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