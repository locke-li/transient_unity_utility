
using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Text;

namespace Transient {
    public static class Log {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Init(int capacity_)
            => LogStream.Default.Cache.Init(capacity_);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Debug(
            string msg_, string stack_ = "", string source_ = null,
            int stackDepth_ = 0, [CallerMemberName] string member_ = "", [CallerFilePath] string filePath_ = "", [CallerLineNumber] int lineNumber_ = 0)
            => LogStream.Default.Message(LogStream.debug, msg_, stack_, source_, stackDepth_ + 1, member_, filePath_, lineNumber_);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Info(
            string msg_, string stack_ = "", string source_ = null,
            int stackDepth_ = 0, [CallerMemberName] string member_ = "", [CallerFilePath] string filePath_ = "", [CallerLineNumber] int lineNumber_ = 0)
            => LogStream.Default.Message(LogStream.info, msg_, stack_, source_, stackDepth_ + 1, member_, filePath_, lineNumber_);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Warn(
            string msg_, string stack_ = null, string source_ = null,
            int stackDepth_ = 0, [CallerMemberName] string member_ = "", [CallerFilePath] string filePath_ = "", [CallerLineNumber] int lineNumber_ = 0)
            => LogStream.Default.Message(LogStream.warning, msg_, stack_, source_, stackDepth_ + 1, member_, filePath_, lineNumber_);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WarnIf(
            bool condition_, string msg_,
            object arg0_ = null, object arg1_ = null, object arg2_ = null, object arg3_ = null,
            string stack_ = null, string source_ = null,
            int stackDepth_ = 0, [CallerMemberName] string member_ = "", [CallerFilePath] string filePath_ = "", [CallerLineNumber] int lineNumber_ = 0)
            => LogStream.Default.MessageIf(condition_, LogStream.warning, msg_, arg0_, arg1_, arg2_, arg3_, stack_, source_, stackDepth_ + 1, member_, filePath_, lineNumber_);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Error(
            string msg_, string stack_ = null, string source_ = null,
            int stackDepth_ = 0, [CallerMemberName] string member_ = "", [CallerFilePath] string filePath_ = "", [CallerLineNumber] int lineNumber_ = 0)
            => LogStream.Default.Message(LogStream.error, msg_, stack_, source_, stackDepth_ + 1, member_, filePath_, lineNumber_);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ErrorIf(
            bool condition_, string msg_,
            object arg0_ = null, object arg1_ = null, object arg2_ = null, object arg3_ = null,
            string stack_ = null, string source_ = null,
            int stackDepth_ = 0, [CallerMemberName] string member_ = "", [CallerFilePath] string filePath_ = "", [CallerLineNumber] int lineNumber_ = 0)
            => LogStream.Default.MessageIf(condition_, LogStream.error, msg_, arg0_, arg1_, arg2_, arg3_, stack_, source_, stackDepth_ + 1, member_, filePath_, lineNumber_);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Assert(
            bool condition_, string msg_,
            object arg0_ = null, object arg1_ = null, object arg2_ = null, object arg3_ = null,
            string stack_ = null, string source_ = null,
            int stackDepth_ = 0, [CallerMemberName] string member_ = "", [CallerFilePath] string filePath_ = "", [CallerLineNumber] int lineNumber_ = 0)
            => LogStream.Default.Assert(condition_, msg_, arg0_, arg1_, arg2_, arg3_, stack_, source_, stackDepth_ + 1, member_, filePath_, lineNumber_);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Custom(
            int level_, string msg_, string stack_ = null, string source_ = null,
            int stackDepth_ = 0, [CallerMemberName] string member_ = "", [CallerFilePath] string filePath_ = "", [CallerLineNumber] int lineNumber_ = 0)
            => LogStream.Default.Message(level_, msg_, stack_, source_, stackDepth_ + 1, member_, filePath_, lineNumber_);
    }

    public sealed class LogStream {
        public static LogStream Default { get; private set; } = new LogStream();

        public const int debug = 0;
        public const int info = 1;
        public const int warning = 2;
        public const int error = 3;
        public const int assert = 4;
        public const int custom = 5;
        public LogCache Cache { get; set; } = new LogCache();

        public void Message(
            int level_, string msg_, string stack_ = "", string source_ = null, 
            int stackDepth_ = 0, [CallerMemberName] string member_ = "", [CallerFilePath] string filePath_ = "", [CallerLineNumber] int lineNumber_ = 0
            ) {
            Performance.RecordProfiler(nameof(LogStream));
            Cache.Log(msg_, stack_ ?? new StackTrace(stackDepth_ + 1).ToString(), level_, source_, new EntrySite(member_, filePath_, lineNumber_));
            Performance.End(nameof(LogStream));
        }

        public void MessageIf(
            bool condition_, int level_, string msg_,
            object arg0_, object arg1_, object arg2_, object arg3_,
            string stack_ = null, string source_ = null, 
            int stackDepth_ = 0, [CallerMemberName] string member_ = "", [CallerFilePath] string filePath_ = "", [CallerLineNumber] int lineNumber_ = 0
            ) {
            if (condition_) return;
            Performance.RecordProfiler(nameof(LogStream));
            var message = string.Format(msg_, arg0_, arg1_, arg2_, arg3_);
            Cache.Log(message, stack_ ?? new StackTrace(stackDepth_ + 1).ToString(), assert, source_, new EntrySite(member_, filePath_, lineNumber_));
            Performance.End(nameof(LogStream));
        }

        public void Assert(
            bool condition_, string msg_,
            object arg0_, object arg1_, object arg2_, object arg3_,
            string stack_ = null, string source_ = null,
            int stackDepth_ = 0, [CallerMemberName] string member_ = "", [CallerFilePath] string filePath_ = "", [CallerLineNumber] int lineNumber_ = 0
            ) {
            if (condition_) return;
            Performance.RecordProfiler(nameof(LogStream));
            var message = string.Format(msg_, arg0_, arg1_, arg2_, arg3_);
            Cache.Log(message, stack_ ?? new StackTrace(stackDepth_ + 1).ToString(), assert, source_, new EntrySite(member_, filePath_, lineNumber_));
            Performance.End(nameof(LogStream));
            throw new Exception($"assert failed: {message}");
        }
    }

    public struct LogEntry {
        public string content;
        public string stacktrace;
        public int level;
        public EntrySite site;
        public string source;
        public ushort count;
    }

    public struct EntrySite {
        public string member;
        public string file;
        public int line;

        public EntrySite(string member_, string file_, int line_) {
            member = member_;
            file = file_;
            line = line_;
        }
    }

    public sealed class LogCache {
        private LogEntry[] logs;
        private int head = 0;
        private int tail = 0;
        public static string sourceUnity = "unity";
        public ActionList<LogEntry> LogReceived { get; private set; }

        public LogCache() {

        }

        public void Init(int capacity_, bool forced = false) {
            if (!forced && logs != null) return;
            logs = new LogEntry[capacity_];
            head = 0;
            tail = 0;
            LogReceived = new ActionList<LogEntry>(4);
            var unityLogLevel = new int[] {
                LogStream.error, LogStream.assert, LogStream.warning, LogStream.debug, LogStream.error
            };
            UnityEngine.Application.logMessageReceived += (m_, s_, t_) => {
                var level = unityLogLevel[(int)t_];
                if (m_ == logs[LastLogIndex()].content) return;
                Log(m_, s_, level, sourceUnity, new EntrySite());
            };
        }

        private int LastLogIndex() {
            return (tail + logs.Length - 1) % logs.Length;
        }

        public void Log(string log_, string stacktrace_, int level_, string source_, EntrySite site_) {
#if UNITY_EDITOR
            if (logs == null) return;
#endif
            Performance.RecordProfiler(nameof(LogCache));
            if (string.IsNullOrEmpty(log_)) {
                log_ = "<null log>";
                stacktrace_ = new StackTrace(2).ToString();
                level_ = LogStream.error;
            }
            //merge consecutive logs with the same content
            //NOTE source ignored
            var last = LastLogIndex();
            if (logs[last].content == log_
                && logs[last].stacktrace == stacktrace_
                && logs[last].level == level_) {
                ++logs[last].count;
                return;
            }
            var log = new LogEntry() {
                content = log_,
                stacktrace = stacktrace_,
                level = level_,
                source = source_,
                site = site_,
            };
            logs[tail] = log;
            tail = ++tail % logs.Length;
            if (source_ != sourceUnity) {
                LogToUnity(log_, level_);
            }
            LogReceived.Invoke(log);
            Performance.End(nameof(LogCache));
        }

        private bool LogToUnity(string log_, int level_) {
            if (level_ == LogStream.error) {//Error
                UnityEngine.Debug.LogError(log_);
                return true;
            }
            if (level_ == LogStream.assert) {//Assert
                UnityEngine.Debug.LogAssertion(log_);
                return true;
            }
            if (level_ == LogStream.warning) {//Warning
                UnityEngine.Debug.LogWarning(log_);
                return true;
            }
            if (level_ == LogStream.debug) {//Log
                UnityEngine.Debug.Log(log_);
                return true;
            }
            return false;
        }

        public void ForEach(Action<int, LogEntry> Process) {
            if (logs == null) return;
            ForEach(Process, logs.Length);
        }
        public void ForEach(Action<int, LogEntry> Process, int max_) {
            int tailV = tail, headV = head;
            if (tailV < headV) {
                for (int r = headV; r < max_; ++r) {
                    Process(r, logs[r]);
                }
                for (int r = 0; r < tailV - max_ + headV; ++r) {
                    Process(r, logs[r]);
                }
            }
            else {
                max_ = headV + max_;
                max_ = max_ > tailV ? tailV : max_;
                for (int r = headV; r < max_; ++r) {
                    Process(r, logs[r]);
                }
            }
        }

        public int Offset(int n, int f) {
            if (head == tail) {
                return -1;
            }
            n += f;
            if (tail < head) {
                if (n >= tail) n = tail - 1;
                else if (tail < head) n = head;
            }
            else {
                if (n < head) n = head;
                else if (n >= tail) n = tail - 1;
            }
            return n;
        }

        public LogEntry EntryAt(int n) {
            if (n < 0 || n > logs.Length)
                return default;
            return logs[n];
        }

        //NOTE don't clear while iterating
        public void Clear() {
            LogReceived.Clear();
            Array.Clear(logs, 0, logs.Length);
            head = 0;
            tail = 0;
        }
    }
}