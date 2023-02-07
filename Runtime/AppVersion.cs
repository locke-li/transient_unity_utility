using Transient;
using System.Collections.Generic;

namespace Transient {
    public class AppVersion {
        public string versionRaw;
        public int major;
        public int minor;
        public int patch;
        public string revision;
        public string channel;
        public string env;
        public List<string> info;

        public (bool, string) ParseExternal(string path) {
            var content = FileUtility.TryReadBytes(path);
            if (content == null) return (false, "read failed");
            var str = System.Text.Encoding.UTF8.GetString(content);
            return Parse(str);
        }

        public (bool, string) ParseInternal(string path) {
            var content = FileUtility.LoadBytes(path);
            if (content == null) return (false, "read failed");
            var str = System.Text.Encoding.UTF8.GetString(content);
            return Parse(str);
        }

        public (bool, string) Parse(string str) {
            if (string.IsNullOrEmpty(str)) return (false, "empty version string");
            var seg = str.Split(new char[] {'\n', '\r'});
            if (seg.Length < 1) return (false, $"invalid format: {str}");
            str = seg[0];
            var seg1 = str.Split(new char[] {'.', '_'});
            if (seg1.Length < 3) return (false, $"invalid format: {str}");
            if (!int.TryParse(seg1[0], out var majorT)
                || !int.TryParse(seg1[1], out var minorT)
                || !int.TryParse(seg1[2], out var patchT)) return (false, $"numeric format invalid: {str}");
            versionRaw = str;
            major = majorT;
            minor = minorT;
            patch = patchT;
            revision = seg.Length > 1 ? seg[1] : string.Empty;
            channel = seg.Length > 2 ? seg[2] : string.Empty;
            env = seg.Length > 3 ? seg[3] : string.Empty;
            if (info != null) info.Clear();
            else info = new List<string>();
            if (seg.Length > 4) {
                for(var i = 4; i < seg.Length; ++i) {
                    info.Add(seg[i]);
                }
            }
            return (true, string.Empty);
        }
    }
}