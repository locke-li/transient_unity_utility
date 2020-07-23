//#define SkipPerformance

using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Text;

namespace Transient {
    public static class Log {
        [Conditional("LogEnabled")]
        public static void Debug(
            string msg_,
            object arg0_ = null, object arg1_ = null, object arg2_ = null,
            [CallerMemberName]string member_ = "", [CallerFilePath]string filePath_ = "", [CallerLineNumber]int lineNumber_ = 0
            ) {
            LogStream.Default.Debug(msg_, arg0_, arg1_, arg2_, member_, filePath_, lineNumber_);
        }

        [Conditional("LogEnabled")]
        public static void Info(
            string msg_,
            object arg0_ = null, object arg1_ = null, object arg2_ = null,
            [CallerMemberName]string member_ = "", [CallerFilePath]string filePath_ = "", [CallerLineNumber]int lineNumber_ = 0
            ) {
            LogStream.Default.Info(msg_, arg0_, arg1_, arg2_, member_, filePath_, lineNumber_);
        }

        [Conditional("LogEnabled")]
        public static void Warning(
            string msg_,
            object arg0_ = null, object arg1_ = null, object arg2_ = null,
            [CallerMemberName]string member_ = "", [CallerFilePath]string filePath_ = "", [CallerLineNumber]int lineNumber_ = 0
            ) {
            LogStream.Default.Warning(msg_, arg0_, arg1_, arg2_, member_, filePath_, lineNumber_);
        }

        [Conditional("LogEnabled")]
        public static void Error(
            string msg_,
            object arg0_ = null, object arg1_ = null, object arg2_ = null,
            [CallerMemberName]string member_ = "", [CallerFilePath]string filePath_ = "", [CallerLineNumber]int lineNumber_ = 0
            ) {
            LogStream.Default.Error(msg_, arg0_, arg1_, arg2_, member_, filePath_, lineNumber_);
        }

        [Conditional("LogEnabled")]
        public static void Assert(
            bool condition_, string msg_,
            object arg0_ = null, object arg1_ = null, object arg2_ = null,
            [CallerMemberName]string member_ = "", [CallerFilePath]string filePath_ = "", [CallerLineNumber]int lineNumber_ = 0
            ) {
            LogStream.Default.Assert(condition_, msg_, arg0_, arg1_, arg2_, member_, filePath_, lineNumber_);
        }

        [Conditional("LogEnabled")]
        public static void Custom(
            int level_, string msg_,
            object arg0_ = null, object arg1_ = null, object arg2_ = null,
            [CallerMemberName]string member_ = "", [CallerFilePath]string filePath_ = "", [CallerLineNumber]int lineNumber_ = 0
            ) {
            LogStream.Default.Custom(level_, msg_, arg0_, arg1_, arg2_, member_, filePath_, lineNumber_);
        }
    }

    public sealed class LogStream {
        public static LogStream Default { get; private set; } = new LogStream();

        private readonly StringBuilder _buffer = new StringBuilder(1024);
        public const int debug = 0;
        public const int normal = 1;
        public const int warning = 2;
        public const int error = 3;
        public const int assert = 4;
        public const int custom = 5;
        public const byte sourceDirect = byte.MaxValue;
        public const byte sourceUnity = 9;
        public readonly bool[] skipStacktrace = new bool[] { true, true, true, false, false };
        public LogCache Cache { get; } = new LogCache();

        [Conditional("LogEnabled")]
        public void Debug(
            string msg_,
            object arg0_ = null, object arg1_ = null, object arg2_ = null,
            [CallerMemberName]string member_ = "", [CallerFilePath]string filePath_ = "", [CallerLineNumber]int lineNumber_ = 0
            ) {
#if !SkipPerformance
            Performance.RecordProfiler(nameof(LogStream));
#endif
            _buffer.Length = 0;
            _buffer.AppendFormat(msg_, arg0_, arg1_, arg2_);
            Cache.Log(_buffer.ToString(), skipStacktrace[debug] ? null : new StackTrace(1, true).ToString(), debug, new EntrySource(member_, filePath_, lineNumber_));
#if !SkipPerformance
            Performance.End(nameof(LogStream));
#endif
        }

        [Conditional("LogEnabled")]
        public void Info(
            string msg_,
            object arg0_ = null, object arg1_ = null, object arg2_ = null,
            [CallerMemberName]string member_ = "", [CallerFilePath]string filePath_ = "", [CallerLineNumber]int lineNumber_ = 0
            ) {
#if !SkipPerformance
            Performance.RecordProfiler(nameof(LogStream));
#endif
            _buffer.Length = 0;
            _buffer.AppendFormat(msg_, arg0_, arg1_, arg2_);
            Cache.Log(_buffer.ToString(), skipStacktrace[normal] ? null : new StackTrace(1, true).ToString(), normal, new EntrySource(member_, filePath_, lineNumber_));
#if !SkipPerformance
            Performance.End(nameof(LogStream));
#endif
        }

        [Conditional("LogEnabled")]
        public void Warning(
            string msg_,
            object arg0_ = null, object arg1_ = null, object arg2_ = null,
            [CallerMemberName]string member_ = "", [CallerFilePath]string filePath_ = "", [CallerLineNumber]int lineNumber_ = 0
            ) {
#if !SkipPerformance
            Performance.RecordProfiler(nameof(LogStream));
#endif
            _buffer.Length = 0;
            _buffer.AppendFormat(msg_, arg0_, arg1_, arg2_);
            Cache.Log(_buffer.ToString(), skipStacktrace[warning] ? null : new StackTrace(1, true).ToString(), warning, new EntrySource(member_, filePath_, lineNumber_));
#if !SkipPerformance
            Performance.End(nameof(LogStream));
#endif
        }

        [Conditional("LogEnabled")]
        public void Error(
            string msg_,
            object arg0_ = null, object arg1_ = null, object arg2_ = null,
            [CallerMemberName]string member_ = "", [CallerFilePath]string filePath_ = "", [CallerLineNumber]int lineNumber_ = 0
            ) {
#if !SkipPerformance
            Performance.RecordProfiler(nameof(LogStream));
#endif
            _buffer.Length = 0;
            _buffer.AppendFormat(msg_, arg0_, arg1_, arg2_);
            Cache.Log(_buffer.ToString(), skipStacktrace[error] ? null : new StackTrace(1, true).ToString(), error, new EntrySource(member_, filePath_, lineNumber_));
#if !SkipPerformance
            Performance.End(nameof(LogStream));
#endif
        }

        [Conditional("LogEnabled")]
        public void Assert(
            bool condition_, string msg_,
            object arg0_ = null, object arg1_ = null, object arg2_ = null,
            [CallerMemberName]string member_ = "", [CallerFilePath]string filePath_ = "", [CallerLineNumber]int lineNumber_ = 0
            ) {
            if(condition_) return;
#if !SkipPerformance
            Performance.RecordProfiler(nameof(LogStream));
#endif
            _buffer.Length = 0;
            _buffer.AppendFormat(msg_, arg0_, arg1_, arg2_);
            Cache.Log(_buffer.ToString(), skipStacktrace[assert] ? null : new StackTrace(1, true).ToString(), assert, new EntrySource(member_, filePath_, lineNumber_));
#if !SkipPerformance
            Performance.End(nameof(LogStream));
#endif
        }

        [Conditional("LogEnabled")]
        public void Custom(
            int level_, string msg_,
            object arg0_ = null, object arg1_ = null, object arg2_ = null,
            [CallerMemberName]string member_ = "", [CallerFilePath]string filePath_ = "", [CallerLineNumber]int lineNumber_ = 0
            ) {
#if !SkipPerformance
            Performance.RecordProfiler(nameof(LogStream));
#endif
            _buffer.Length = 0;
            _buffer.AppendFormat(msg_, arg0_, arg1_, arg2_);
            Cache.Log(_buffer.ToString(), null, level_, new EntrySource(member_, filePath_, lineNumber_));
#if !SkipPerformance
            Performance.End(nameof(LogStream));
#endif
        }
    }

    public struct LogEntry {
        public string content;
        public string stacktrace;
        public int level;
        public EntrySource source;
    }

    public struct EntrySource {
        public byte logger;
        public string member;
        public string file;
        public int line;

        public EntrySource(string member_, string file_, int line_, byte logger_ = LogStream.sourceDirect) {
            logger = logger_;
            member = member_;
            file = file_;
            line = line_;
        }
    }

#if DEBUG
    public sealed class LogCache {
        private const int EntryLimit = 10000;
        private readonly LogEntry[] logs = new LogEntry[EntryLimit];
        private int head = 0, tail = 0;
        private LogEntry defaultLog = new LogEntry();
        public ActionList<LogEntry> LogReceived { get; } = new ActionList<LogEntry>(4);
        //Error/Assert/Warning/Log/Exception
        private static readonly int[] unityLogLevel = new int[] {
            LogStream.error, LogStream.assert, LogStream.warning, LogStream.debug, LogStream.error
        };

        internal LogCache() {
            
        }

        public void Log(string log_, string stacktrace_, int level_, EntrySource source_) {
#if !SkipPerformance
            Performance.RecordProfiler(nameof(LogCache));
#endif
            var log = logs[tail] = new LogEntry() {
                content = log_,
                stacktrace = stacktrace_,
                level = level_,
                source = source_,
            };
            tail = ++tail % logs.Length;
            LogReceived.Invoke(log);
            LogToUnity(log_, level_);
#if !SkipPerformance
            Performance.End(nameof(LogCache));
#endif
        }

        private void UnityLog(string condition_, string stacktrace_, UnityEngine.LogType type_) {
            Log(condition_, stacktrace_, unityLogLevel[(int)type_], 
                new EntrySource() {
                    logger = LogStream.sourceUnity,
                }
            );
        }

        [Conditional("UNITY_EDITOR")]
        private void LogToUnity(string log_, int level_) {
            if (level_ == unityLogLevel[0]) {//Error
                UnityEngine.Debug.LogError(log_);
                return;
            }
            if (level_ == unityLogLevel[1]) {//Assert
                UnityEngine.Debug.LogAssertion(log_);
                return;
            }
            if (level_ == unityLogLevel[2]) {//Warning
                UnityEngine.Debug.LogWarning(log_);
                return;
            }
            if (level_ == unityLogLevel[3]) {//Log
                UnityEngine.Debug.Log(log_);
                return;
            }
            if (level_ == unityLogLevel[4]) {//Exception
                UnityEngine.Debug.LogError(log_);
                return;
            }
            //unknown
            UnityEngine.Debug.Log(log_);
        }

        public void ForEach(Action<int, LogEntry> Process, int max_ = EntryLimit) {
            int tailV = tail, headV = head;
            if(tailV < headV) {
                for(int r = headV;r < max_;++r) {
                    Process(r, logs[r]);
                }
                for(int r = 0;r < tailV - max_ + headV;++r) {
                    Process(r, logs[r]);
                }
            }
            else {
                max_ = headV + max_;
                max_ = max_ > tailV ? tailV : max_;
                for(int r = headV;r < max_;++r) {
                    Process(r, logs[r]);
                }
            }
        }

        public int Offset(int n, int f) {
            if(head == tail) {
                return -1;
            }
            n += f;
            if(tail < head) {
                if(n >= tail) n = tail-1;
                else if(tail < head) n = head;
            }
            else {
                if(n < head) n = head;
                else if(n >= tail) n = tail-1;
            }
            return n;
        }

        public LogEntry EntryAt(int n) {
            if (n < 0 || n > logs.Length)
                return defaultLog;
            return logs[n];
        }

        //assume no clearing while iterating
        public void Clear() {
            Array.Clear(logs, 0, logs.Length);
            head = 0;
            tail = 0;
        }
    }
#else
    public class LogCache {
        private LogEntry defaultLog = new LogEntry();
        
        [Conditional("DEBUG")]
        public void Log(string aLog, int aLevel, EntrySource source_) {}
        [Conditional("DEBUG")]
        public void ForEach(ActionAlt<int, LogEntry> Process) {}
        public int Offset(int n, int f) { return n+f; }
        public LogEntry EntryAt(int n) { return defaultLog; }
        public void Clear() {}
    }
#endif
}