using Transient;
using UnityEngine;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using InfoMap = Transient.SimpleContainer.Dictionary<string, string>;

namespace Transient {
    public class AppVersion : IEqualityComparer<AppVersion>, IComparable<AppVersion> {
        public static Func<byte[], AppVersion, (AppVersion, string)> Parser;
        public static char seperator = ':';

        public string version;
        public int major;
        public int minor;
        public int patch;
        public string Revision => InfoChecked(nameof(Revision));
        public string Channel => InfoChecked(nameof(Channel));
        public string Env => InfoChecked(nameof(Env));
        public string DataVersion => InfoChecked(nameof(DataVersion));
        public string AssetVersion => InfoChecked(nameof(AssetVersion));
        public InfoMap info = new InfoMap(4);

        public static (AppVersion, string) TryCreate(byte[] content) {
            var (version, reason) = Parser?.Invoke(content, null) ?? Deserialize(content, null);
            if (reason != null) return (null, reason);
            return (version, string.Empty);
        }

        public (bool, string) Parse(byte[] content) {
            var (version, reason) = Parser?.Invoke(content, this) ?? Deserialize(content, this);
            if (reason != null) return (false, reason);
            return (true, string.Empty);
        }

        public static (AppVersion, string) Deserialize(byte[] content, AppVersion target) {
            target = target ?? new AppVersion();
            using var reader = new StreamReader(new MemoryStream(content));
            while (!reader.EndOfStream) {
                var l = reader.ReadLine();
                if (!l.Contains(seperator)) continue;
                var seg = l.Split(seperator, ' ', StringSplitOptions.RemoveEmptyEntries);
                if (seg.Length < 2) continue;
                var v = seg[0];
                switch (v) {
                    case nameof(version): target.Reset(v); break;
                    default: target.info.Add(v, seg[1]); break;
                }
            }
            if (target.version == null) return (null, "invalid version");
            return (target, string.Empty);
        }

        public byte[] Serialize() {
            var builder = new StringBuilder();
            builder.Append(nameof(version)).Append(seperator).Append(version).AppendLine();
            foreach (var (k, v) in info) {
                builder.Append(k).Append(seperator).Append(v).AppendLine();
            }
            return Encoding.UTF8.GetBytes(builder.ToString());//TODO optimize
        }

        public (bool, string) Reset(string version_) {
            version = version_;
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

        public void Reset(int major_, int minor_, int patch_) {
            major = major_;
            minor = minor_;
            patch = patch_;
            version = $"{major_}.{minor_}.{patch_}";
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