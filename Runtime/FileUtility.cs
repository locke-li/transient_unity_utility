using System.Collections;
using System.Collections.Generic;
using System.IO;
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

        public static byte[] LoadBytesExtracted(string path, bool shouldUpdate, string sourceRoot, string targetRoot) {
            //only extract on Android
#if UNITY_ANDROID && !UNITY_EDITOR
            var target = Path.Combine(targetRoot, path);
            if (shouldUpdate && !DownloadTo(true, Path.Combine(sourceRoot, path), target)) {
                return null;
            }
#else
            var target = Path.Combine(sourceRoot, path);
#endif
            return File.ReadAllBytes(target);
        }

        public static byte[] LoadBytesIntegrated(string path) {
            Log.Info($"loading {path}");
            //only extract on Android
#if UNITY_ANDROID && !UNITY_EDITOR
            var request = new UnityWebRequest(path);
            var handler = new DownloadHandlerBuffer();
            request.downloadHandler = handler;
            request.SendWebRequest();
            while (!request.isDone) { }
            if (request.error != null) {
                Log.Error(request.error);
                return null;
            }
            while (!handler.isDone) {
                Thread.Sleep(100);
            }
            return handler.data;
#else
            return File.ReadAllBytes(path);
#endif
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
    }
}