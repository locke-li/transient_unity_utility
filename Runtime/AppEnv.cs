using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Transient {
    public static class AppEnv {
        public static SystemLanguage Language {
            get => (SystemLanguage)PlayerPrefs.GetInt(nameof(Language), (int)Application.systemLanguage);
            set => PlayerPrefs.SetInt(nameof(Language), (int)value);
        }
        public static string Channel { get; set; }
        public static string Version { get; set; }
    }
}