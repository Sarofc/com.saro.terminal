using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Saro.Terminal
{
    /*
     *  Warning :
     *  Don't support command overload!
     *  One command string only bind one command!
     *
     *  example :
     *  int                 - CmdStr 1
     *  float               - CmdStr 1.1
     *  bool                - CmdStr false/True
     *  vector2             - CmdStr (1,1)         // no ' '
     *  vector3             - CmdStr 1,1,1         // '(' ')' is not necessary
     *  string//gameobject  - CmdStr actor
     */
    public static partial class Terminal
    {
        /// <summary>
        /// 处理用户命令
        /// </summary>
        public static class Shell
        {
            /// <summary>
            /// 参数解析委托
            /// </summary>
            /// <param name="input"></param>
            /// <param name="output"></param>
            /// <returns></returns>
            public delegate bool TypeParser(string input, out object output);

            // 命令map
            private static SortedDictionary<string, Command> s_CommandMap = null;

            // 参数解析函数map
            private static Dictionary<Type, TypeParser> s_TypeMap = null;

            // 解析后的参数队列，包括命令名称
            private static Queue<string> s_Args = null;

            // 自动补全缓存
            private static List<string> s_AutoCompleteCache = null;
            private static int s_IdxOfCommandCache = -1;

            // 命令历史记录
            private static LitRingBuffer<string> s_CommandHistory;
            private static int s_CommandIdx;

            /// <summary>
            /// 非法字符
            /// </summary>
            private static readonly List<char> s_InvalidChrsForCommandName = new List<char> { ' ', /*'-',*/ '/', '\\', '\b', '\t', };

            // Shell静态构造
            static Shell()
            {
                // 初始化各种容器
                s_CommandMap = new SortedDictionary<string, Command>();
                s_Args = new Queue<string>(4);
                s_AutoCompleteCache = new List<string>(8);
                s_TypeMap = new Dictionary<Type, TypeParser>();
                s_CommandHistory = new LitRingBuffer<string>(32);
                s_CommandIdx = s_CommandHistory.Length;

                // 注册内置参数类型解析
                RegisterType(typeof(string), ParseString);
                RegisterType(typeof(int), ParseInt);
                RegisterType(typeof(float), ParseFlot);
                RegisterType(typeof(bool), ParseBool);
                RegisterType(typeof(UnityEngine.Vector2), ParseVector2);
                RegisterType(typeof(UnityEngine.Vector3), ParseVector3);
                RegisterType(typeof(UnityEngine.Vector4), ParseVector4);
                RegisterType(typeof(UnityEngine.GameObject), ParseGameobject);


                // 注册内置命令
                AddCommandStatic(typeof(BuildInCommands));
            }

            #region Command

            /// <summary>
            /// 注册参数类型解析函数
            /// </summary>
            /// <param name="type"></param>
            /// <param name="fn"></param>
            public static void RegisterType(Type type, TypeParser fn)
            {
                if (s_TypeMap.ContainsKey(type))
                {
                    LogWarning("[Shell] Already contains this type: " + type);
                    return;
                }

                s_TypeMap.Add(type, fn);
            }

            /// <summary>
            /// 绑定实例命令
            /// </summary>
            /// <param name="classType"></param>
            /// <param name="instance"></param>
            public static void AddCommandInstance(Type classType, object instance)
            {
                if (instance == null)
                {
                    LogError("[Shell] Instance couldn't be null!");
                    return;
                }

                MethodInfo[] methods = classType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (MethodInfo method in methods)
                {
                    if (method != null)
                    {
                        CommandAttribute attribute = method.GetCustomAttribute<CommandAttribute>();
                        if (attribute != null)
                            InternalAddCommand(attribute.command, attribute.description, method, instance);
                    }
                }
            }

            /// <summary>
            /// 移除命令
            /// </summary>
            /// <param name="command">命令名称</param>
            public static void RemoveCommand(string command)
            {
                if (s_CommandMap.ContainsKey(command))
                {
                    s_CommandMap.Remove(command);
                }
            }

            /// <summary>
            /// 执行命令
            /// </summary>
            /// <param name="commandLine">包括命令名称以及参数</param>
            public static void ExecuteCommand(string commandLine)
            {
                // parse command
                // [0] is command
                // others are parameters
                Queue<string> args = ParseCommandLine(commandLine);
                if (args.Count <= 0)
                {
                    LogError("[Shell] Command shouln't be null");
                    return;
                }

                // 除非是空串，不管命令是否有效，先都记录下来
                PushCommandHistory(commandLine);

                string commandStr = args.Dequeue();

                if (!s_CommandMap.TryGetValue(commandStr, out Command command))
                {
                    LogError("[Shell] Can't find this command : " + commandStr);
                }
                else if (!command.IsValid())
                {
                    LogError("[Shell] This command is not valid : " + commandStr);
                }
                else
                {
                    if (!command.IsParamsCountMatch(args.Count))
                    {
                        LogError($"[Shell] {commandStr} : Parameters count mismatch, expected count : {command.ParamsTypes.Length}. Type : {string.Join<object>(",", command.ParamsTypes)}");
                        return;
                    }

                    object[] paramters = new object[command.ParamsTypes.Length];
                    for (int i = 0; i < command.ParamsTypes.Length; i++)
                    {
                        if (!s_TypeMap.TryGetValue(command.ParamsTypes[i], out TypeParser typeParse))
                        {
                            LogError("[Shell] This paramter type is unsupported : " + command.ParamsTypes[i].Name);
                            return;
                        }

                        if (typeParse?.Invoke(args.Peek(), out paramters[i]) == false)
                        {
                            LogError($"[Shell] Can't parse {args.Peek()} to type {command.ParamsTypes[i].Name}");
                        }

                        args.Dequeue();
                    }
                    // call method
                    command.Execute(paramters);
                }
            }

            /// <summary>
            /// 绑定Assembly-CSharp程序集里所有静态命令
            /// </summary>
            public static void AddAllCommandStatic()
            {
                IEnumerable<Type> types = ReflectionUtil.GetAllAssemblyTypes;
                foreach (Type type in types)
                {
                    AddCommandStatic(type);
                }
            }

            /// <summary>
            /// 绑定指定类里的静态命令
            /// </summary>
            /// <param name="classType"></param>
            public static void AddCommandStatic(Type classType)
            {
                MethodInfo[] methods = classType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (MethodInfo method in methods)
                {
                    if (method != null)
                    {
                        CommandAttribute attribute = method.GetCustomAttribute<CommandAttribute>();
                        if (attribute != null)
                            InternalAddCommand(attribute.command, attribute.description, method, null);
                    }
                }
            }

            /// <summary>
            /// 获取所有命令
            /// </summary>
            /// <returns></returns>
            internal static IEnumerable<Command> GetAllCommands()
            {
                return s_CommandMap.Values;
            }

            /// <summary>
            /// 尝试通过“命令名称”获取命令
            /// </summary>
            /// <param name="commandStr"></param>
            /// <returns></returns>
            internal static Command TryGetCommand(string commandStr)
            {
                if (s_CommandMap.TryGetValue(commandStr, out Command command))
                    return command;

                return null;
            }

            private static void InternalAddCommand(string command, string description, MethodInfo methodInfo, object instance)
            {
                // check command name
                foreach (char chr in command)
                {
                    if (s_InvalidChrsForCommandName.Contains(chr))
                    {
                        LogError("[Shell] invalid characters : " + string.Join(",", s_InvalidChrsForCommandName));
                        return;
                    }
                }

                // check parameters
                ParameterInfo[] parameters = methodInfo.GetParameters();
                if (parameters == null) parameters = new ParameterInfo[0];

                Type[] paramTypes = new Type[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    Type type = parameters[i].ParameterType;
                    if (s_TypeMap.ContainsKey(type))
                    {
                        paramTypes[i] = type;
                    }
                    else
                    {
                        // command is not valid, return
                        LogError("[Shell] Unsupported type : " + type);
                        return;
                    }
                }

                // parse method info
                var sb = StringBuilderCache.Acquire();
                sb.AppendFormat("<color=red>{0}</color>", command).Append("\t - ");

                if (!string.IsNullOrEmpty(description)) sb.Append(description).Append("\t -> ");

                sb//.Append(methodInfo.DeclaringType.ToString()).Append(".")
                  .AppendFormat("<color=yellow>{0}(</color>", methodInfo.Name);

                for (int i = 0; i < paramTypes.Length; i++)
                {
                    Type type = paramTypes[i];
                    sb.AppendFormat("<color=yellow>{0}</color>", type.Name);
                    if (i < paramTypes.Length - 1) sb.Append(",");
                }

                sb.Append("<color=yellow>)</color>").Append(" : ").Append(methodInfo.ReturnType.Name);

                // store to map
                s_CommandMap[command] = new Command(methodInfo, paramTypes, instance, StringBuilderCache.GetStringAndRelease(sb));
            }

            #endregion


            #region AutoComplete

            /// <summary>
            /// 命令自动补全
            /// </summary>
            /// <returns></returns>
            public static string AutoComplete()
            {
                if (s_AutoCompleteCache.Count == 0) return null;

                if (++s_IdxOfCommandCache >= s_AutoCompleteCache.Count) s_IdxOfCommandCache = 0;
                return s_AutoCompleteCache[s_IdxOfCommandCache];
            }

            /// <summary>
            /// 获取可能的命令，并缓存下来
            /// </summary>
            /// <param name="header"></param>
            public static void GetPossibleCommand(string header)
            {
                if (string.IsNullOrEmpty(header))
                {
                    throw new Exception("Header couldn't be Null or Empty");
                }

                s_IdxOfCommandCache = -1;
                s_AutoCompleteCache.Clear();

                foreach (string k in s_CommandMap.Keys)
                {
                    if (k.StartsWith(header))
                    {
                        s_AutoCompleteCache.Add(k);
                    }
                }

                // ---------------------------------------------
                // TEST
                //if (m_autoCompleteCache.Count == 0) return;
                //var sb = new StringBuilder();
                //foreach (var str in m_autoCompleteCache)
                //{
                //    sb.Append(str).Append('\t');
                //}
                //Log(sb.ToString());
                // ---------------------------------------------
            }

            #endregion

            #region CommandHistory

            public static string GetPrevCommand()
            {
                if (--s_CommandIdx < 0)
                {
                    s_CommandIdx = 0;
                }

                return s_CommandHistory[s_CommandIdx];
            }

            public static string GetNextCommand()
            {
                if (++s_CommandIdx >= s_CommandHistory.Length)
                {
                    s_CommandIdx = 0;
                    s_CommandIdx = s_CommandHistory.Length;

                    return string.Empty;
                }

                return s_CommandHistory[s_CommandIdx];
            }

            // 命令历史缓存
            private static void PushCommandHistory(string cmd)
            {
                s_CommandHistory.AddTail(cmd);
                s_CommandIdx = s_CommandHistory.Length;
            }

            #endregion

            #region Parse command

            private static Queue<string> ParseCommandLine(string input)
            {
                s_Args.Clear();
                if (input == null) return s_Args;
                input.Trim();
                if (input.Length == 0) return s_Args;

                string[] args = input.Split(' ');
                foreach (string arg in args)
                {
                    string tmp = arg.Trim();
                    if (string.IsNullOrEmpty(tmp)) continue;
                    s_Args.Enqueue(tmp);
                }

                return s_Args;
            }

            private static bool ParseString(string input, out object output)
            {
                output = input;
                return true;
            }

            private static bool ParseInt(string input, out object output)
            {
                var result = int.TryParse(input, out int value);
                output = value;
                return result;
            }

            private static bool ParseFlot(string input, out object output)
            {
                var result = float.TryParse(input, out float value);
                output = value;
                return result;
            }

            private static bool ParseBool(string input, out object output)
            {
                var result = bool.TryParse(input, out bool value);
                output = value;
                return result;
            }

            // (x,y)
            private static bool ParseVector2(string input, out object output)
            {
                output = UnityEngine.Vector2.zero;

                if (string.IsNullOrEmpty(input))
                {
                    return false;
                }

                string[] xy = input.Replace('(', ' ').Replace(')', ' ').Split(',');

                if (xy.Length == 2)
                {
                    string tmpX = xy[0].Trim();
                    if (string.IsNullOrEmpty(tmpX)) return false;

                    string tmpY = xy[1].Trim();
                    if (string.IsNullOrEmpty(tmpY)) return false;

                    if (!float.TryParse(tmpX, out float x)) return false;

                    if (!float.TryParse(tmpY, out float y)) return false;

                    output = new UnityEngine.Vector2(x, y);
                }

                return false;
            }

            // (x,y,z)
            private static bool ParseVector3(string input, out object output)
            {
                output = UnityEngine.Vector3.zero;

                if (string.IsNullOrEmpty(input))
                {
                    return false;
                }

                string[] xyz = input.Replace('(', ' ').Replace(')', ' ').Split(',');

                if (xyz.Length == 3)
                {
                    string tmpX = xyz[0].Trim();
                    if (string.IsNullOrEmpty(tmpX)) return false;

                    string tmpY = xyz[1].Trim();
                    if (string.IsNullOrEmpty(tmpY)) return false;

                    string tmpZ = xyz[2].Trim();
                    if (string.IsNullOrEmpty(tmpZ)) return false;

                    if (!float.TryParse(tmpX, out float x)) return false;

                    if (!float.TryParse(tmpY, out float y)) return false;

                    if (!float.TryParse(tmpZ, out float z)) return false;

                    output = new UnityEngine.Vector3(x, y, z);
                    return true;
                }

                return false;
            }

            private static bool ParseVector4(string input, out object output)
            {
                output = UnityEngine.Vector4.zero;

                if (string.IsNullOrEmpty(input))
                {
                    output = new UnityEngine.Vector4();
                    return false;
                }

                string[] xyzw = input.Replace('(', ' ').Replace(')', ' ').Split(',');

                if (xyzw.Length == 4)
                {
                    string tmpX = xyzw[0].Trim();
                    if (string.IsNullOrEmpty(tmpX)) return false;

                    string tmpY = xyzw[1].Trim();
                    if (string.IsNullOrEmpty(tmpY)) return false;

                    string tmpZ = xyzw[2].Trim();
                    if (string.IsNullOrEmpty(tmpZ)) return false;

                    string tmpW = xyzw[3].Trim();
                    if (string.IsNullOrEmpty(tmpZ)) return false;

                    if (!float.TryParse(tmpX, out float x)) return false;

                    if (!float.TryParse(tmpY, out float y)) return false;

                    if (!float.TryParse(tmpZ, out float z)) return false;

                    if (!float.TryParse(tmpW, out float w)) return false;

                    output = new UnityEngine.Vector4(x, y, z, w);
                    return true;
                }

                output = new UnityEngine.Vector4();
                return false;
            }

            private static bool ParseGameobject(string input, out object output)
            {
                output = UnityEngine.GameObject.Find(input);
                if (output != null) return true;
                else return false;
            }

            #endregion

        }
    }
}
