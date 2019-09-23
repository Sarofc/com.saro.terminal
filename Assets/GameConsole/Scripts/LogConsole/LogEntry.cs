using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

namespace Saro.Console
{
    public class LogEntry : IEquatable<LogEntry>
    {
        private const int HASH_NOT_CALCULATED = -623218;

        public string logString;
        public string stackTrace;

        public Sprite typeSprite;
        public int count;

        private string m_completeLog = null;
        private int m_hash = HASH_NOT_CALCULATED;

        public LogEntry(string logString, string stackTrace, Sprite typeSprite)
        {
            this.logString = logString;
            this.stackTrace = stackTrace;
            this.typeSprite = typeSprite;

            count = 1;
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
            if (m_hash == HASH_NOT_CALCULATED)
            {
                unchecked
                {
                    m_hash = 17;
                    m_hash = m_hash * m_hash * 23 + logString == null ? 0 : logString.GetHashCode();
                    m_hash = m_hash * m_hash * 23 + stackTrace == null ? 0 : stackTrace.GetHashCode();
                }
            }
            return m_hash;
        }

        public override string ToString()
        {
            if (m_completeLog == null)
            {
#if UNITY_EDITOR
                m_completeLog = string.Concat(logString, "\n", stackTrace);
#else
                m_completeLog = logString;
#endif

            }
            return m_completeLog;
        }

#if UNITY_EDITOR
        public void TraceScript()
        {
            Match regex = Regex.Match(stackTrace, @"\(at .*\.cs:[0-9]+\)$", RegexOptions.Multiline);
            if (regex.Success)
            {
                var line = stackTrace.Substring(regex.Index + 4, regex.Length - 5);
                var lineSeparator = line.IndexOf(':');

                UnityEditor.MonoScript script = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.MonoScript>(line.Substring(0, lineSeparator));
                if (script != null)
                {
                    UnityEditor.AssetDatabase.OpenAsset(script, int.Parse(line.Substring(lineSeparator + 1)));
                }
            }
        }
#endif
    }

    public struct QueuedLogEntry
    {
        public readonly string logString;
        public readonly string stackTrace;
        public readonly LogType logType;

        public QueuedLogEntry(string logString, string stackTrace, LogType logType)
        {
            this.logString = logString;
            this.stackTrace = stackTrace;
            this.logType = logType;
        }
    }
}