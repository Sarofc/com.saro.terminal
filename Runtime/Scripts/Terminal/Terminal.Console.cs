using System;
using System.Collections.Generic;


namespace Saro.Terminal
{
    // TODO 
    // 设置持久化
    // 时间戳

    public partial class Terminal
    {
        #region Console

        /// <summary>
        /// 日志条目
        /// </summary>
        public class LogEntry : IEquatable<LogEntry>
        {
            private const int k_HASH_NOT_CALCULATED = -623218;
            private int m_Hash;

            public string logString;
            public string stackTrace;
            public UnityEngine.LogType logType;

            public int count;

            private static Stack<LogEntry> m_Pool = new Stack<LogEntry>();

            public static LogEntry Create(string logString, string stackTrace, UnityEngine.LogType logType)
            {
                if (m_Pool.Count > 0)
                {
                    var entry = m_Pool.Pop();
                    entry.logString = logString;
                    entry.stackTrace = stackTrace;
                    entry.logType = logType;
                    entry.count = 1;

                    return entry;
                }
                else
                {
                    return new LogEntry(logString, stackTrace, logType);
                }
            }

            public static void Release(LogEntry entry)
            {
                if (entry == null)
                {
                    LogError("entry is null. can't release.");
                    return;
                }

                entry.logString = null;
                entry.stackTrace = null;
                entry.logType = 0;

                entry.m_Hash = k_HASH_NOT_CALCULATED;
                entry.count = 1;
            }

            private LogEntry(string logString, string stackTrace, UnityEngine.LogType logType)
            {
                this.logString = logString;
                this.stackTrace = stackTrace.Remove(stackTrace.Length - 1, 1);
                this.logType = logType;

                this.count = 1;

                m_Hash = k_HASH_NOT_CALCULATED;
            }

            public override string ToString()
            {
#if UNITY_EDITOR
                return logString + "\n" + stackTrace;
#else
                return logString;
#endif
            }

            public string ToString(bool full)
            {
#if UNITY_EDITOR

                if (full)
                    return logString + "\n" + stackTrace;
                else
#endif
                    return logString;
            }

            [System.Diagnostics.Conditional("UNITY_EDITOR")]
            public void TraceScript()
            {
                var regex = System.Text.RegularExpressions.Regex.Match(stackTrace, @"\(at .*\.cs:[0-9]+\)$", System.Text.RegularExpressions.RegexOptions.Multiline);
                if (regex.Success)
                {
                    string line = stackTrace.Substring(regex.Index + 4, regex.Length - 5);
                    int lineSeparator = line.IndexOf(':');

                    UnityEditor.MonoScript script = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.MonoScript>(line.Substring(0, lineSeparator));
                    if (script != null)
                    {
                        UnityEditor.AssetDatabase.OpenAsset(script, int.Parse(line.Substring(lineSeparator + 1)));
                    }
                }
            }

            public bool Equals(LogEntry other)
            {
                return logString == other.logString &&
                    stackTrace == other.stackTrace;
            }

            // Ovirride hash function to use this as Key for Dictionary
            // Credit: https://stackoverflow.com/a/19250516/2373034
            public override int GetHashCode()
            {
                if (m_Hash == k_HASH_NOT_CALCULATED)
                {
                    unchecked
                    {
                        m_Hash = 17;
                        m_Hash = m_Hash * m_Hash * 23 + logString == null ? 0 : logString.GetHashCode();
                        m_Hash = m_Hash * m_Hash * 23 + stackTrace == null ? 0 : stackTrace.GetHashCode();
                    }
                }
                return m_Hash;
            }

        }

        /// <summary>
        /// 相同信息是否折叠
        /// </summary>
        public static bool IsCollapsed
        {
            get
            {
                return HasLogFlag(ELogTypeFlag.Collapsed);
            }
            set
            {
                if (value)
                {
                    SetLogFlag(ELogTypeFlag.Collapsed);
                }
                else
                {
                    UnsetLogFlag(ELogTypeFlag.Collapsed);
                }
            }
        }

        /// <summary>
        /// 显示log
        /// </summary>
        public static bool IsLog
        {
            get
            {
                return HasLogFlag(ELogTypeFlag.Log);
            }
            set
            {
                if (value)
                {
                    SetLogFlag(ELogTypeFlag.Log);
                }
                else
                {
                    UnsetLogFlag(ELogTypeFlag.Log);
                }
            }
        }

        /// <summary>
        /// 显示warning
        /// </summary>
        public static bool IsWarning
        {
            get
            {
                return HasLogFlag(ELogTypeFlag.Warning);
            }
            set
            {
                if (value)
                {
                    SetLogFlag(ELogTypeFlag.Warning);
                }
                else
                {
                    UnsetLogFlag(ELogTypeFlag.Warning);
                }
            }
        }

        /// <summary>
        /// 显示error
        /// </summary>
        public static bool IsError
        {
            get
            {
                return HasLogFlag(ELogTypeFlag.Error);
            }
            set
            {
                if (value)
                {
                    SetLogFlag(ELogTypeFlag.Error);
                }
                else
                {
                    UnsetLogFlag(ELogTypeFlag.Error);
                }
            }
        }

        /// <summary>
        /// 日志flag
        /// </summary>
        [System.Flags]
        public enum ELogTypeFlag : byte
        {
            None = 0,
            Error = 1 << 1,
            Warning = 1 << 2,
            Log = 1 << 3,

            Collapsed = 1 << 4,

            Debug = Error | Warning | Log,

            All = Collapsed | Debug,
        }

        private static ELogTypeFlag s_LogFlag = ELogTypeFlag.All;

        /// <summary>
        /// 接收unity log，view需要监听
        /// </summary>
        public static event Action<bool, int, UnityEngine.LogType> LogMessageReceived;
        /// <summary>
        /// 折叠日志条目，每条都是唯一的，重复的不再添加进来
        /// </summary>
        public static IReadOnlyList<LogEntry> CollapsedLogEntries => s_CollapsedLogEntries;
        /// <summary>
        /// 需要显示的日志条目索引，数据源 
        /// <see cref="CollapsedLogEntries"/>
        /// </summary>
        public static IReadOnlyList<int> LogEntryIndicesToShow => s_LogEntryIndicesToShow;

        // store unique logentry
        private static List<LogEntry> s_CollapsedLogEntries;
        /// <summary>
        /// logentry to (index, timestamp). see 
        /// <see cref="s_CollapsedLogEntries"/>
        /// </summary>
        private static Dictionary<LogEntry, int> s_CollapsedLogEntriesMap;
        // uncollapsed list index
        private static List<int> s_UnCollapsedLogEntryIndices;
        // logentry index to show
        private static List<int> s_LogEntryIndicesToShow;

        // TODO timestamp
        //private static List<string> s_Timestamps;
        //private static List<int> s_CollapsedTimestampsIndices;

        private static void InitializeConfig()
        {
            if (s_CollapsedLogEntries == null) s_CollapsedLogEntries = new List<LogEntry>();
            if (s_CollapsedLogEntriesMap == null) s_CollapsedLogEntriesMap = new Dictionary<LogEntry, int>();
            if (s_UnCollapsedLogEntryIndices == null) s_UnCollapsedLogEntryIndices = new List<int>();
            if (s_LogEntryIndicesToShow == null) s_LogEntryIndicesToShow = new List<int>();
            //if (s_Timestamps == null) s_Timestamps = new List<string>();
            //if (s_CollapsedTimestampsIndices == null) s_CollapsedTimestampsIndices = new List<int>();
        }

        public static void ClearLog()
        {
            for (int i = 0; i < s_CollapsedLogEntries.Count; i++)
            {
                var entry = s_CollapsedLogEntries[i];
                LogEntry.Release(entry);
            }

            s_CollapsedLogEntries.Clear();
            s_CollapsedLogEntriesMap.Clear();
            s_UnCollapsedLogEntryIndices.Clear();
            s_LogEntryIndicesToShow.Clear();

            //s_Timestamps.Clear();
            //s_CollapsedTimestampsIndices.Clear();
        }

        //[System.Diagnostics.Conditional("ENABLE_TERMINAL_PRINT_LOG")]
        public static void AddUnityLogListener()
        {
            InitializeConfig();

            UnityEngine.Application.logMessageReceived -= Application_logMessageReceived;
            UnityEngine.Application.logMessageReceived += Application_logMessageReceived;
        }

        //[System.Diagnostics.Conditional("ENABLE_TERMINAL_PRINT_LOG")]
        public static void RemoveUnityLogListener()
        {
            UnityEngine.Application.logMessageReceived += Application_logMessageReceived;

            ClearLog();
        }

        /// <summary>
        /// log filter. see
        /// <see cref="ELogTypeFlag"/>
        /// </summary>
        public static void FilterLog()
        {
            s_LogEntryIndicesToShow.Clear();

            if (HasLogFlag(ELogTypeFlag.Collapsed))
            {
                for (int i = 0; i < s_CollapsedLogEntries.Count; i++)
                {
                    var entry = s_CollapsedLogEntries[i];
                    if (HasLogFlag(ELogTypeFlag.Log) && entry.logType == UnityEngine.LogType.Log ||
                        HasLogFlag(ELogTypeFlag.Warning) && entry.logType == UnityEngine.LogType.Warning ||
                        HasLogFlag(ELogTypeFlag.Error) && entry.logType == UnityEngine.LogType.Error)
                    {
                        s_LogEntryIndicesToShow.Add(i);
                    }
                }
            }
            else
            {
                for (int i = 0; i < s_UnCollapsedLogEntryIndices.Count; i++)
                {
                    var entry = s_CollapsedLogEntries[s_UnCollapsedLogEntryIndices[i]];
                    if (HasLogFlag(ELogTypeFlag.Log) && entry.logType == UnityEngine.LogType.Log ||
                        HasLogFlag(ELogTypeFlag.Warning) && entry.logType == UnityEngine.LogType.Warning ||
                        HasLogFlag(ELogTypeFlag.Error) && entry.logType == UnityEngine.LogType.Error)
                    {
                        s_LogEntryIndicesToShow.Add(s_UnCollapsedLogEntryIndices[i]);
                    }
                }
            }
        }

        /// <summary>
        /// get log string
        /// <code>TODO: maybe chinese character cause error</code>
        /// </summary>
        /// <returns></returns>
        public static string GetLog()
        {
            int strLen = 100; // in case
            int newLineLen = Environment.NewLine.Length;

            for (int i = 0; i < s_UnCollapsedLogEntryIndices.Count; i++)
            {
                var entry = s_CollapsedLogEntries[s_UnCollapsedLogEntryIndices[i]];
                strLen += entry.logString.Length + entry.stackTrace.Length + newLineLen * 3;
            }

            var sb = StringBuilderCache.Acquire(strLen);
            for (int i = 0; i < s_UnCollapsedLogEntryIndices.Count; i++)
            {
                var entry = s_CollapsedLogEntries[s_UnCollapsedLogEntryIndices[i]];
                sb.AppendLine(entry.logString).AppendLine(entry.stackTrace);

                sb.AppendLine();
            }

            return StringBuilderCache.GetStringAndRelease(sb);
        }

        /// <summary>
        /// receive unity log message
        /// </summary>
        /// <param name="logString"></param>
        /// <param name="stackTrace"></param>
        /// <param name="logType"></param>
        private static void Application_logMessageReceived(string logString, string stackTrace, UnityEngine.LogType logType)
        {
            var entry = LogEntry.Create(logString, stackTrace, logType);
            var has = s_CollapsedLogEntriesMap.TryGetValue(entry, out int index);

            //s_Timestamps.Add($"[{DateTime.Now.Ticks}]");

            if (has)
            {
                s_CollapsedLogEntries[index].count++;
                LogEntry.Release(entry);

                //s_CollapsedTimestampsIndices[index] = s_Timestamps.Count - 1;
            }
            else
            {
                index = s_CollapsedLogEntries.Count;
                s_CollapsedLogEntries.Add(entry);

                s_CollapsedLogEntriesMap[entry] = index;

                //s_CollapsedTimestampsIndices.Add(index);
            }

            if (!(HasLogFlag(ELogTypeFlag.Collapsed) && has) && HasLogFlag(ELogTypeFlag.Error) || HasLogFlag(ELogTypeFlag.Warning) || HasLogFlag(ELogTypeFlag.Log))
            {
                s_LogEntryIndicesToShow.Add(index);
            }

            s_UnCollapsedLogEntryIndices.Add(index);

            LogMessageReceived?.Invoke(has, index, logType);
        }

        private static void SetLogFlag(ELogTypeFlag type)
        {
            s_LogFlag |= type;
        }

        private static void UnsetLogFlag(ELogTypeFlag type)
        {
            s_LogFlag &= ~type;
        }

        private static bool HasLogFlag(ELogTypeFlag type)
        {
            return (s_LogFlag & type) != 0;
        }

        #endregion
    }
}

