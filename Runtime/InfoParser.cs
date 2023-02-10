using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Transient;
using UnityEngine;
using static Transient.Performance;

namespace Transient {
    public class InfoParser {
        private static InfoParser _instance;
        public static InfoParser Instance {
            get => _instance = _instance ?? new InfoParser();
            set => _instance = value;
        }
        public char seperator = ':';

        public virtual IEnumerable<(string, string)> Deserialize(byte[] content) {
            if (content == null || content.Length == 0) yield break;
            using var reader = new StreamReader(new MemoryStream(content));
            while (!reader.EndOfStream) {
                var l = reader.ReadLine();
                if (!l.Contains(seperator)) continue;
                var seg = l.Split(seperator, ' ', StringSplitOptions.RemoveEmptyEntries);
                if (seg.Length < 2) continue;
                yield return (seg[0], seg[1]);
            }
        }

        public virtual byte[] Serialize(IEnumerable<(string, string)> info) {
            var builder = new StringBuilder();
            foreach (var (k, v) in info) {
                builder.Append(k).Append(seperator).Append(v).AppendLine();
            }
            return Encoding.UTF8.GetBytes(builder.ToString());//TODO optimize
        }
    }
}