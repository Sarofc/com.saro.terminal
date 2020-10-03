using System;
using System.Collections.Generic;



namespace Saro.Terminal
{
    public static partial class Terminal
    {
        public const float k_Version = 0.1f;

        public static Shell Shell { get; private set; }
        public static Console Console { get; private set; }

        static Terminal()
        {
            Shell = new Shell();
            Console = new Console();
        }

        #region API

        public static void Log(object msg)
        {
            UnityEngine.Debug.Log(msg);
        }

        public static void LogWarning(object msg)
        {
            UnityEngine.Debug.LogWarning(msg);
        }

        public static void LogError(object msg)
        {
            UnityEngine.Debug.LogError(msg);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void LogAssertion(object msg)
        {
            UnityEngine.Debug.LogAssertion(msg);
        }

        public static void SaveSettings()
        {
            Shell?.SaveSettings();
            Console?.SaveSettings();
        }

        #endregion
    }

}
