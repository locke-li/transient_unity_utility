using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;

namespace Transient {
    public static class FileUtility {
        private static bool DownloadTo(bool isLocal, string path, string target) {
            var prefix = isLocal ?
#if UNITY_EDITOR || UNITY_IOS
                "file://"
#else
                string.Empty
#endif
                : string.Empty;
            var source = prefix + path;
            var request = new UnityWebRequest(source);
            Log.Info($"extracting {request.url}");
            var handler = new DownloadHandlerFile(target);
            request.downloadHandler = handler;
            request.SendWebRequest();
            while (!request.isDone) { }
            if (request.error != null) {
                Log.Error(request.error);
                return false;
            }
            while (!handler.isDone) {
                Thread.Sleep(100);
            }
            return true;
        }

        public static byte[] LoadBytes(string path) {
#if DEBUG
            Log.Info($"loading {path}");
#endif
            //only extract on Android
            if (!File.Exists(path)) {
#if UNITY_ANDROID && !UNITY_EDITOR
                var request = new UnityWebRequest(path);
                var handler = new DownloadHandlerBuffer();
                request.downloadHandler = handler;
                request.SendWebRequest();
                while (!request.isDone) { }
                if (request.error != null) {
                    return null;
                }
                while (!handler.isDone) {
                    Thread.Sleep(100);
                }
                return handler.data;
#else
                return null;
#endif
            }
            return File.ReadAllBytes(path);
        }

        public static string TryReadText(string path) {
            if (!File.Exists(path)) return null;
            return File.ReadAllText(path);
        }

        public static byte[] TryReadBytes(string path) {
            if (!File.Exists(path)) return null;
            return File.ReadAllBytes(path);
        }

        public static void WriteTextWithBackup(string path, string content) {
            var temp = path + "_temp";
            if (File.Exists(path)) {
                File.WriteAllText(temp, content);
                File.Replace(temp, path, path + "_bak");
            }
            else {
                File.WriteAllText(path, content);
            }
        }

        public static void WriteBytesWithBackup(string path, byte[] content) {
            var temp = path + "_temp";
            if (File.Exists(path)) {
                File.WriteAllBytes(temp, content);
                File.Replace(temp, path, path + "_bak");
            }
            else {
                File.WriteAllBytes(path, content);
            }
        }

        public static void Copy(string from, string to, StringBuilder builder, bool overwrite = false) {
            builder.AppendLine($"copy: {from} -> {to}");
            File.Copy(from, to, overwrite);
        }

        public static void Copy(FileInfo from, string to, StringBuilder builder, bool overwrite = false) {
            builder.AppendLine($"copy: {from.Name} -> {to}");
            from.CopyTo(to, overwrite);
        }

        public static void DeleteDirectory(string path, params string[] filter) {
            if (!Directory.Exists(path)) return;
            foreach (var sub in Directory.EnumerateDirectories(path)) {
                Directory.Delete(sub, true);
            }
            foreach (var file in Directory.EnumerateFiles(path)) {
                if (!filter.Any(v => file.EndsWith(v))) {
                    File.Delete(file);
                }
            }
        }

        public static void CopyDirectory(string source, string destination, params string[] patternList) {
            var dir = new DirectoryInfo(source);
            Directory.CreateDirectory(destination);
            foreach (var pattern in patternList) {
                foreach (var file in dir.EnumerateFiles(pattern)) {
                    File.Copy(Path.Combine(source, file.Name), Path.Combine(destination, file.Name));
                }
            }
            foreach (var sub in dir.EnumerateDirectories()) {
                CopyDirectory(sub.FullName, Path.Combine(destination, sub.Name), patternList);
            }
        }

        public static void ClearDirectory(string dir) {
            if (Directory.Exists(dir)) {
                Directory.Delete(dir, true);
            }
            Directory.CreateDirectory(dir);
        }
    }
}