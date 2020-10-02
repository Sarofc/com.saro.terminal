using System;
using System.Collections.Generic;

namespace Saro.Terminal
{
    public static partial class Terminal
    {
        public const float k_Version = 0.1f;

        //static Terminal()
        //{
            
        //}

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

        #endregion
    }

}
