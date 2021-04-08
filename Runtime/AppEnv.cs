using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Transient {
    public static class AppEnv {
        public static string keyLanguage = nameof(keyLanguage);
        public static SystemLanguage Language {
            get => (SystemLanguage)PlayerPrefs.GetInt(keyLanguage, (int)Application.systemLanguage);
            set => PlayerPrefs.SetInt(keyLanguage, (int)value);
        }
        public static string Channel { get; set; }
        public static string Version { get; set; }
        public static string VersionPackage { get; set; }
        public static string VersionIdentifier { get; set; }
    }
}