#if true

using System;
using System.Collections.Generic;

namespace Saro.Terminal
{
    public static partial class Terminal
    {
        internal const string k_DEV_CONSOLE = "DEV_CONSOLE";

        public const float k_Version = 0.1f;

        internal static Shell Shell { get; private set; }
        internal static Console Console { get; private set; }

        static Terminal()
        {
            Initialize();
        }

        [System.Diagnostics.Conditional(Terminal.k_DEV_CONSOLE)]
        static void Initialize()
        {
            Shell = new Shell();
            Console = new Console();

            Log("<color=blue>[Terminal]</color> Initialize...");
        }

//#if UNITY_EDITOR
//        [UnityEditor.MenuItem("Terminal/Toggle")]
//        static void SetupTerminal()
//        {
//            var flag = UnityEditor.EditorUserBuildSettings.selectedBuildTargetGroup;
//            var str = UnityEditor.PlayerSettings.GetScriptingDefineSymbolsForGroup(flag);

//            var dst = new List<string>();
//            var symbols = str.Split(';');
//            var has = false;
//            foreach (var item in symbols)
//            {
//                if (item == k_DEV_CONSOLE)
//                {
//                    has = true;
//                    continue;
//                }
//                dst.Add(item);
//            }

//            if (!has)
//            {
//                dst.Add(Terminal.k_DEV_CONSOLE);
//            }

//            UnityEditor.PlayerSettings.SetScriptingDefineSymbolsForGroup(flag, dst.ToArray());

//            LogError(string.Join(";", dst));
//        }
//#endif

        #region API

        [System.Diagnostics.Conditional(Terminal.k_DEV_CONSOLE)]
        public static void AddCommandInstance(Type classType, object instance)
        {
            Shell?.AddCommandInstance(classType, instance);
        }

        [System.Diagnostics.Conditional(Terminal.k_DEV_CONSOLE)]
        public static void AddCommand(Type classType)
        {
            Shell?.AddCommand(classType);
        }

        [System.Diagnostics.Conditional(Terminal.k_DEV_CONSOLE)]
        public static void AddAllCommand()
        {
            Shell?.AddAllCommand();
        }

        [System.Diagnostics.Conditional(Terminal.k_DEV_CONSOLE)]
        public static void RegisterType(Type classType, TypeParser parser)
        {
            Shell?.RegisterType(classType, parser);
        }

        [System.Diagnostics.Conditional(Terminal.k_DEV_CONSOLE)]
        public static void SaveSettings()
        {
            Shell?.SaveSettings();
            Console?.SaveSettings();
        }

        [System.Diagnostics.Conditional(Terminal.k_DEV_CONSOLE)]
        public static void ClearLog()
        {
            Console?.ClearLog();
        }

        [System.Diagnostics.Conditional(Terminal.k_DEV_CONSOLE)]
        public static void FilterLog()
        {
            Console?.FilterLog();
        }

        [System.Diagnostics.Conditional(Terminal.k_DEV_CONSOLE)]
        public static void IsWarningEnable(bool value)
        {
            if (Console != null)
            {
                Console.IsWarningEnable = value;
            }
        }

        [System.Diagnostics.Conditional(Terminal.k_DEV_CONSOLE)]
        public static void IsInfoEnable(bool value)
        {
            if (Console != null)
            {
                Console.IsInfoEnable = value;
            }
        }

        [System.Diagnostics.Conditional(Terminal.k_DEV_CONSOLE)]
        public static void IsErrorEnable(bool value)
        {
            if (Console != null)
            {
                Console.IsErrorEnable = value;
            }
        }

        [System.Diagnostics.Conditional(Terminal.k_DEV_CONSOLE)]
        public static void IsCollapsedEnable(bool value)
        {
            if (Console != null)
            {
                Console.IsCollapsedEnable = value;
            }
        }

        // 替换自定义 logger
        internal static void Log(object msg)
        {
            UnityEngine.Debug.Log(msg);
        }

        // 替换自定义 logger
        internal static void LogWarning(object msg)
        {
            UnityEngine.Debug.LogWarning(msg);
        }

        // 替换自定义 logger
        internal static void LogError(object msg)
        {
            UnityEngine.Debug.LogError(msg);
        }

        #endregion
    }
}

#endif