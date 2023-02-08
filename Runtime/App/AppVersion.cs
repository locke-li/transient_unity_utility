using Transient;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Transient {
    public class AppVersion : IEqualityComparer<AppVersion>, IComparable<AppVersion> {
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

        public int Compare(int major_, int minor_, int patch_) {
            if (major != major_) return major > major_ ? 1 : -1;
            if (minor != minor_) return minor > minor_ ? 1 : -1;
            if (patch != patch_) return patch > patch_ ? 1 : -1;
            return 0;
        }
        public bool Above(int major_, int minor_, int patch_)
            => Compare(major_, minor_, patch_) >= 0;
        public bool Below(int major_, int minor_, int patch_)
            => Compare(major_, minor_, patch_) < 0;

        public virtual bool Equals(AppVersion a, AppVersion b) {
            if (ReferenceEquals(a, b)) return true;
            return a.major == b.major && a.minor == b.minor && a.patch == b.patch;
        }

        public override int GetHashCode() {
            return major << 16 + minor << 8 + patch;
        }
        public int GetHashCode(AppVersion obj) => obj.GetHashCode();

        public virtual int CompareTo(AppVersion b) {
            var hashA = GetHashCode();
            var hashB = b.GetHashCode();
            if (hashA == hashB) return 0;
            return hashA > hashB ? 1 : -1;
        }

        public override string ToString() {
            return version;
        }
    }
}