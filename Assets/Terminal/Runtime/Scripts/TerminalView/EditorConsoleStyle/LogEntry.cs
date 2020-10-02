using System;
using System.Text.RegularExpressions;
using UnityEngine;

//namespace Saro.Terminal
//{
//    public class LogEntry : IEquatable<LogEntry>
//    {
//        private const int k_HASH_NOT_CALCULATED = -623218;

//        public string logString;
//        public string stackTrace;

//        public Sprite typeSprite;

//        //public List<string> dateTimes = new List<string>(); // TODO store DateTime
//        public int count;

//        private string m_CompleteLog = null;
//        private int m_Hash = k_HASH_NOT_CALCULATED;

//        public LogEntry(string logString, string stackTrace, Sprite typeSprite)
//        {
//            this.logString = logString;
//            this.stackTrace = stackTrace;
//            this.typeSprite = typeSprite;

//            count = 1;
//        }

//        public bool Equals(LogEntry other)
//        {
//            return logString == other.logString &&
//                stackTrace == other.stackTrace;
//        }

//        // Ovirride hash function to use this as Key for Dictionary
//        // Credit: https://stackoverflow.com/a/19250516/2373034
//        public override int GetHashCode()
//        {
//            if (m_Hash == k_HASH_NOT_CALCULATED)
//            {
//                unchecked
//                {
//                    m_Hash = 17;
//                    m_Hash = m_Hash * m_Hash * 23 + logString == null ? 0 : logString.GetHashCode();
//                    m_Hash = m_Hash * m_Hash * 23 + stackTrace == null ? 0 : stackTrace.GetHashCode();
//                }
//            }
//            return m_Hash;
//        }

//        public string ToString(bool showStackTrace = false)
//        {
//            if (m_CompleteLog == null)
//            {
//#if UNITY_EDITOR
//                m_CompleteLog = string.Concat(logString, "\n", stackTrace);
//                //m_completeLog = showStackTrace ?
//                //    string.Concat("[", dateTimes[dateTimes.Count - 1], "] ", logString, "\n", stackTrace) :
//                //    string.Concat("[", dateTimes[dateTimes.Count - 1], "] ", logString);
//#else
//                m_CompleteLog = logString;
//                //m_CompleteLog = string.Concat("[", dateTimes[dateTimes.Count - 1], "] ", logString);
//#endif

//            }
//            return m_CompleteLog;
//        }

//#if UNITY_EDITOR
//        public void TraceScript()
//        {
//            Match regex = Regex.Match(stackTrace, @"\(at .*\.cs:[0-9]+\)$", RegexOptions.Multiline);
//            if (regex.Success)
//            {
//                string line = stackTrace.Substring(regex.Index + 4, regex.Length - 5);
//                int lineSeparator = line.IndexOf(':');

//                UnityEditor.MonoScript script = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.MonoScript>(line.Substring(0, lineSeparator));
//                if (script != null)
//                {
//                    UnityEditor.AssetDatabase.OpenAsset(script, int.Parse(line.Substring(lineSeparator + 1)));
//                }
//            }
//        }
//#endif
//    }

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
//}