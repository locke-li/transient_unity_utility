using Transient;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Transient {
    public class AppVersion {
        public string version;
        public int major;
        public int minor;
        public int patch;
        public string Revision => InfoChecked(nameof(Revision));
        public string Channel => InfoChecked(nameof(Channel));
        public string Env => InfoChecked(nameof(Env));
        public string DataVersion => InfoChecked(nameof(DataVersion));
        public string AssetVersion => InfoChecked(nameof(AssetVersion));
        public Dictionary<string, string> info;

        public static (AppVersion, string) Parse(string str) {
            AppVersion version = null;
            try {
                version = JsonUtility.FromJson<AppVersion>(str);
            }
            catch (Exception e) {
                return (null, e.Message);
            }
            var (valid, reason) = version.Init();
            if (!valid) return (null, reason);
            return (version, string.Empty);
        }

        private (bool, string) Init() {
            var seg = version.Split('.');
            if (seg.Length != 3) return (false, "invalid version format");
            if (int.TryParse(seg[0], out var majorT)
                || int.TryParse(seg[0], out var minorT)
                || int.TryParse(seg[0], out var patchT))
                return (false, "invalid numeric format");
            major = majorT;
            minor = minorT;
            patch = patchT;
            return (true, string.Empty);
        }

        public string InfoChecked(string key) {
            if (info.TryGetValue(key, out var v)) return v;
            return null;
        }
    }
}