﻿
using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Text;

namespace Transient {
    public static class Log {
        [Conditional("LogEnabled")]
        public static void Debug(
            string msg_,
            [CallerMemberName]string member_ = "", [CallerFilePath]string filePath_ = "", [CallerLineNumber]int lineNumber_ = 0
            ) {
            LogStream.Default.Debug(msg_, member_, filePath_, lineNumber_);
        }

        [Conditional("LogEnabled")]
        public static void Info(
            string msg_,
            [CallerMemberName]string member_ = "", [CallerFilePath]string filePath_ = "", [CallerLineNumber]int lineNumber_ = 0
            ) {
            LogStream.Default.Info(msg_, member_, filePath_, lineNumber_);
        }

        [Conditional("LogEnabled")]
        public static void Warning(
            string msg_,
            [CallerMemberName]string member_ = "", [CallerFilePath]string filePath_ = "", [CallerLineNumber]int lineNumber_ = 0
            ) {
            LogStream.Default.Warning(msg_, member_, filePath_, lineNumber_);
        }

        [Conditional("LogEnabled")]
        public static void Error(
            string msg_,
            [CallerMemberName]string member_ = "", [CallerFilePath]string filePath_ = "", [CallerLineNumber]int lineNumber_ = 0
            ) {
            LogStream.Default.Error(msg_, member_, filePath_, lineNumber_);
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
            [CallerMemberName]string member_ = "", [CallerFilePath]string filePath_ = "", [CallerLineNumber]int lineNumber_ = 0
            ) {
            LogStream.Default.Custom(level_, msg_, member_, filePath_, lineNumber_);
        }
    }

    public sealed class LogStream {
        public static LogStream Default { get; private set; } = new LogStream();

        public const int debug = 0;
        public const int info = 1;
        public const int warning = 2;
        public const int error = 3;
        public const int assert = 4;
        public const int custom = 5;
        public const byte sourceDirect = byte.MaxValue;
        public const byte sourceUnity = 9;
        public readonly bool[] skipStacktrace = new bool[] { true, true, true, false, false };
        public LogCache Cache { get; } = new LogCache();

        [Conditional("LogEnabled")]
        internal void Debug(
            string msg_,
            object arg0_, object arg1_, object arg2_,
            [CallerMemberName]string member_ = "", [CallerFilePath]string filePath_ = "", [CallerLineNumber]int lineNumber_ = 0
            ) {
            Performance.RecordProfiler(nameof(LogStream));
            Cache.Log(msg_, skipStacktrace[debug] ? null : new StackTrace(2, true).ToString(), debug, new EntrySource(member_, filePath_, lineNumber_));
            Performance.End(nameof(LogStream));
        }

        [Conditional("LogEnabled")]
        internal void Info(
            string msg_,
            object arg0_, object arg1_, object arg2_,
            [CallerMemberName]string member_ = "", [CallerFilePath]string filePath_ = "", [CallerLineNumber]int lineNumber_ = 0
            ) {
            Performance.RecordProfiler(nameof(LogStream));
            Cache.Log(msg_, skipStacktrace[info] ? null : new StackTrace(2, true).ToString(), info, new EntrySource(member_, filePath_, lineNumber_));
            Performance.End(nameof(LogStream));
        }

        [Conditional("LogEnabled")]
        internal void Warning(
            string msg_,
            object arg0_, object arg1_, object arg2_,
            [CallerMemberName]string member_ = "", [CallerFilePath]string filePath_ = "", [CallerLineNumber]int lineNumber_ = 0
            ) {
            Performance.RecordProfiler(nameof(LogStream));
            Cache.Log(msg_, skipStacktrace[warning] ? null : new StackTrace(2, true).ToString(), warning, new EntrySource(member_, filePath_, lineNumber_));
            Performance.End(nameof(LogStream));
        }

        [Conditional("LogEnabled")]
        internal void Error(
            string msg_,
            object arg0_, object arg1_, object arg2_,
            [CallerMemberName]string member_ = "", [CallerFilePath]string filePath_ = "", [CallerLineNumber]int lineNumber_ = 0
            ) {
            Performance.RecordProfiler(nameof(LogStream));
            Cache.Log(msg_, skipStacktrace[error] ? null : new StackTrace(2, true).ToString(), error, new EntrySource(member_, filePath_, lineNumber_));
            Performance.End(nameof(LogStream));
        }

        [Conditional("LogEnabled")]
        internal void Assert(
            bool condition_, string msg_,
            object arg0_, object arg1_, object arg2_,
            [CallerMemberName]string member_ = "", [CallerFilePath]string filePath_ = "", [CallerLineNumber]int lineNumber_ = 0
            ) {
            if(condition_) return;
            Performance.RecordProfiler(nameof(LogStream));
            var message = string.Format(msg_, arg0_, arg1_, arg2_);
            Cache.Log(message, skipStacktrace[assert] ? null : new StackTrace(2, true).ToString(), assert, new EntrySource(member_, filePath_, lineNumber_));
            Performance.End(nameof(LogStream));
            throw new Exception($"assert failed: {message}");
        }

        [Conditional("LogEnabled")]
        internal void Custom(
            int level_, string msg_,
            [CallerMemberName]string member_ = "", [CallerFilePath]string filePath_ = "", [CallerLineNumber]int lineNumber_ = 0
            ) {
            Performance.RecordProfiler(nameof(LogStream));
            Cache.Log(msg_, null, level_, new EntrySource(member_, filePath_, lineNumber_));
            Performance.End(nameof(LogStream));
        }
    }

    public struct LogEntry {
        public string content;
        public string stacktrace;
        public int level;
        public EntrySource source;
        public ushort count;
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

#if LogEnabled
    public sealed class LogCache {
        private const int EntryLimit = 10000;
        private readonly LogEntry[] logs = new LogEntry[EntryLimit];
        private int head = 0, last = -1, tail = 0;
        private LogEntry defaultLog = new LogEntry();
        public ActionList<LogEntry> LogReceived { get; } = new ActionList<LogEntry>(4);
        //Error/Assert/Warning/Log/Exception
        public static readonly int[] unityLogLevel = new int[] {
            LogStream.error, LogStream.assert, LogStream.warning, LogStream.debug, LogStream.error
        };

        internal LogCache() {
        }

        public void Log(string log_, string stacktrace_, int level_, EntrySource source_) {
            Performance.RecordProfiler(nameof(LogCache));
            //merge consecutive logs with the same content
            //NOTE source ignored
            if (last >= 0
                && logs[last].content == log_
                && logs[last].stacktrace == stacktrace_
                && logs[last].level == level_) {
                ++logs[last].count;
                return;
            }
            var log = logs[tail] = new LogEntry() {
                content = log_,
                stacktrace = stacktrace_,
                level = level_,
                source = source_,
            };
            last = ++last % logs.Length;
            tail = ++tail % logs.Length;
            if (!LogToUnity(log_, level_)) {
                LogReceived.Invoke(log);
            }
            Performance.End(nameof(LogCache));
        }

        //TODO use LogCache in UtilsConsole, then re-enable this condition
        //[Conditional("UNITY_EDITOR")]
        private bool LogToUnity(string log_, int level_) {
            if (level_ == unityLogLevel[0]) {//Error
                UnityEngine.Debug.LogError(log_);
                return true;
            }
            if (level_ == unityLogLevel[1]) {//Assert
                UnityEngine.Debug.LogAssertion(log_);
                return true;
            }
            if (level_ == unityLogLevel[2]) {//Warning
                UnityEngine.Debug.LogWarning(log_);
                return true;
            }
            if (level_ == unityLogLevel[3]) {//Log
                UnityEngine.Debug.Log(log_);
                return true;
            }
            if (level_ == unityLogLevel[4]) {//Exception
                UnityEngine.Debug.LogError(log_);
                return true;
            }
            return false;
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
            last = -1;
            tail = 0;
        }
    }
#else
    public class LogCache {
        private LogEntry defaultLog = new LogEntry();
        
        [Conditional("DEBUG")]
        public void Log(string log_, string stacktrace_, int level_, EntrySource source_) {}
        [Conditional("DEBUG")]
        public void ForEach(Action<int, LogEntry> Process) {}
        public int Offset(int n, int f) { return n+f; }
        public LogEntry EntryAt(int n) { return defaultLog; }
        public void Clear() {}
    }
#endif
}