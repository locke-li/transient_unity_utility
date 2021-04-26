using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Transient {
    public static partial class AppEnv {
        public static string keyLanguage = nameof(keyLanguage);
        public static SystemLanguage Language {
            get => (SystemLanguage)PlayerPrefs.GetInt(keyLanguage, (int)Application.systemLanguage);
            set => PlayerPrefs.SetInt(keyLanguage, (int)value);
        }
        public static string LanguageCode => Language2Code(Language);

        public static string Channel { get; set; }
        public static string Version { get; set; }
        public static string VersionPackage { get; set; }
        public static string VersionIdentifier { get; set; }
    }
}

namespace Transient {
    public static partial class AppEnv {
        public static string Language2Code(SystemLanguage lang) {
            return lang switch {
                SystemLanguage.Afrikaans => "aa",
                SystemLanguage.Arabic => "ar",
                SystemLanguage.Basque => "eu",
                SystemLanguage.Belarusian => "be",
                SystemLanguage.Bulgarian => "bg",
                SystemLanguage.Catalan => "ca",
                SystemLanguage.Chinese => "zh",
                SystemLanguage.Czech => "cs",
                SystemLanguage.Danish => "da",
                SystemLanguage.Dutch => "nl",
                SystemLanguage.English => "en",
                SystemLanguage.Estonian => "et",
                SystemLanguage.Faroese => "fo",
                SystemLanguage.Finnish => "fi",
                SystemLanguage.French => "fr",
                SystemLanguage.German => "de",
                SystemLanguage.Greek => "el",
                SystemLanguage.Hebrew => "he",
                SystemLanguage.Icelandic => "is",
                SystemLanguage.Indonesian => "id",
                SystemLanguage.Italian => "it",
                SystemLanguage.Japanese => "ja",
                SystemLanguage.Korean => "ko",
                SystemLanguage.Latvian => "lv",
                SystemLanguage.Lithuanian => "lt",
                SystemLanguage.Norwegian => "nn",
                SystemLanguage.Polish => "pl",
                SystemLanguage.Portuguese => "pt",
                SystemLanguage.Romanian => "ro",
                SystemLanguage.Russian => "ru",
                SystemLanguage.SerboCroatian => "hr",//?Croatian
                SystemLanguage.Slovak => "sk",
                SystemLanguage.Slovenian => "sl",
                SystemLanguage.Spanish => "es",
                SystemLanguage.Swedish => "sv",
                SystemLanguage.Thai => "th",
                SystemLanguage.Turkish => "tr",
                SystemLanguage.Ukrainian => "uk",
                SystemLanguage.Vietnamese => "vi",
                SystemLanguage.ChineseSimplified => "cn",//locale zh_CN, non ISO 639-1
                SystemLanguage.ChineseTraditional => "tw",//locale zh_TW, non ISO 639-1
                SystemLanguage.Unknown => "?",
                _ => "!",
            };
        }
    }
}