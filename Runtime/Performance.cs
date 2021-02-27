using System.Diagnostics;
using System.Runtime.CompilerServices;
using Transient.SimpleContainer;
using UnityEngine.Profiling;
using UnityEngine;

namespace Transient {
    public static class Performance {
        [System.Flags]
        public enum Method : byte {
            Log = 1,
            Profiler = 1<<1,
            ProfilerThread = 1<<2 | Profiler,

            NoStartLog = 1<<7,

            _LogExceptStart = Log | NoStartLog,
            _All = Log | Profiler
        }

        public struct Session {
            public Stopwatch sw;
            public Method method;
            public byte markDepth;
        }

        public const int logLevel = 16;
        private static Dictionary<string, Session> _record;

        static Performance() {
            Init();
        }

        public static void ExtendHeapMB(int mb_) {
            Performance.Record(nameof(ExtendHeapMB));
            var o = new object[mb_];
            var unit = 1024 * 1024;
            for (int b = 0; b < mb_; ++b) {
                o[b] = new byte[unit];
            }
            Performance.End(nameof(ExtendHeapMB));
            o = null;
            System.GC.Collect(0);
        }

        [Conditional("PerformanceRecord")]
        public static void Init() {
            _record = new Dictionary<string, Session>(4);
        }

        [Conditional("PerformanceRecord")]
        public static void Record(
            string key_, Method method_ = Method._All,
            [CallerMemberName]string member_ = "", [CallerFilePath]string filePath_ = "", [CallerLineNumber]int lineNumber_ = 0
        ) {
            if(!_record.TryGetValue(key_, out var s)) {
                _record[key_] = s = new Session() { sw = new Stopwatch(), method = method_ };
            }
            if((method_ & Method.Profiler) != 0) {
                if(method_ == Method.ProfilerThread) Profiler.BeginThreadProfiling(nameof(Performance), key_);
                Profiler.BeginSample(key_);
            }
            if((method_ & Method.Log) != 0) {
                s.sw.Restart();
                if((method_ & Method.NoStartLog) == 0) {
                    Log.Custom(logLevel, $"perf[{key_}|Start]|@{Time.realtimeSinceStartup}",
                        member_, filePath_, lineNumber_
                        );
                }
            }
        }

        [Conditional("PerformanceRecord")]
        public static void RecordLog(
            string key_,
            [CallerMemberName]string member_ = "", [CallerFilePath]string filePath_ = "", [CallerLineNumber]int lineNumber_ = 0
        ) {
            if(!_record.TryGetValue(key_, out var s)) {
                _record[key_] = s = new Session() { sw = new Stopwatch(), method = Method.Log };
            }
            s.sw.Restart();
            Log.Custom(logLevel, $"perf[{key_}|Start]|@{Time.realtimeSinceStartup}",
                member_, filePath_, lineNumber_
            );
        }

        [Conditional("PerformanceRecord")]
        public static void RecordProfiler(
            string key_,
            [CallerMemberName]string member_ = "", [CallerFilePath]string filePath_ = "", [CallerLineNumber]int lineNumber_ = 0
        ) {
            if(!_record.TryGetValue(key_, out var _)) {
                _record[key_] = new Session() { method = Method.Profiler };
            }
            Profiler.BeginSample(key_);
        }

        [Conditional("PerformanceRecord")]
        public static void RecordProfilerThread(
            string key_,
            [CallerMemberName]string member_ = "", [CallerFilePath]string filePath_ = "", [CallerLineNumber]int lineNumber_ = 0
        ) {
            if(!_record.TryGetValue(key_, out var s)) {
                _record[key_] = s = new Session() { method = Method.ProfilerThread };
            }
            Profiler.BeginThreadProfiling(nameof(Performance), key_);
            Profiler.BeginSample(key_);
        }

        [Conditional("PerformanceRecord")]
        public static void Mark(
            string key_, string mark_,
            [CallerMemberName]string member_ = "", [CallerFilePath]string filePath_ = "", [CallerLineNumber]int lineNumber_ = 0
        ) {
            ref var s = ref _record.ValueRef(key_);
            if((s.method & Method.Log) != 0) {
                Log.Custom(logLevel, $"perf[{key_}/{mark_}]{s.sw.ElapsedMilliseconds}ms|{s.sw.ElapsedTicks}ticks",
                    member_, filePath_, lineNumber_
                    );
            }
            if((s.method & Method.Profiler) != 0) {
                if(s.markDepth > 0) Profiler.EndSample();
                else ++s.markDepth;
                Profiler.BeginSample(mark_);
            }
        }

        [Conditional("PerformanceRecord")]
        public static void End(
            string key_, bool keep_ = true, string msg_ = nameof(End),
            [CallerMemberName]string member_ = "", [CallerFilePath]string filePath_ = "", [CallerLineNumber]int lineNumber_ = 0
        ) {
            ref var s = ref _record.ValueRef(key_);
            if((s.method & Method.Log) != 0) {
                s.sw.Stop();
                Log.Custom(logLevel, $"perf[{key_}|{msg_}]{s.sw.ElapsedMilliseconds}ms|{s.sw.ElapsedTicks}ticks",
                    member_, filePath_, lineNumber_
                    );
            }
            if((s.method & Method.Profiler) != 0) {
                for(int r=0;r<s.markDepth;++r) {
                    Profiler.EndSample();
                }
                s.markDepth = 0;
                Profiler.EndSample();
                if(s.method == Method.ProfilerThread) Profiler.EndThreadProfiling();
            }
            if(!keep_) _record.Remove(key_);
        }
    }
}
