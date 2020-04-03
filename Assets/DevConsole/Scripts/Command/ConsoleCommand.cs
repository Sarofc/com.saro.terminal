using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Saro.Console
{
    /*
     *  Warning :
     *  Don't support command overload!
     *  One command string only bind one command!
     *
     *  example :
     *  int                 - Command 1
     *  float               - Command 1.1
     *  bool                - Command false/True
     *  vector2             - Command (1,1)         // no ' '
     *  vector3             - Command 1,1,1         // '(' ')' is not necessary
     *  string//gameobject  - Command actor
     */
    public static class ConsoleCommand
    {
        public class Command
        {
            private readonly MethodInfo m_Method;
            private readonly Type[] m_ParamsTypes;
            private readonly object m_Instance;
            private readonly string m_MethodSignature;

            public Command(MethodInfo method, Type[] paramsTypes, object instance, string methodSignature)
            {
                this.m_Method = method;
                this.m_ParamsTypes = paramsTypes;
                this.m_Instance = instance;
                this.m_MethodSignature = methodSignature;
            }

            public bool IsValid()
            {
                if (!m_Method.IsStatic && m_Instance.Equals(null))
                {
                    return false;
                }
                return true;
            }

            public Type[] ParamsTypes => m_ParamsTypes;

            public bool IsParamsCountMatch(int count)
            {
                return m_ParamsTypes.Length == count;
            }

            public void Execute(object[] objects)
            {
                m_Method?.Invoke(m_Instance, objects);
            }

            /// <summary>
            /// complete info, include command and methodinfo
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return m_MethodSignature;
            }
        }

        private delegate bool TypeParse(string input, out object output);

        private static SortedDictionary<string, Command> s_CommandLookup = null;

        private static Dictionary<Type, TypeParse> s_TypeLookup = null;

        private static Queue<string> s_Args = null;

        private static List<string> s_AutoCompleteCache = null;
        private static int s_IdxOfCommandCache = -1;

        private static readonly List<char> s_InvalidChrsForCommandName = new List<char> { ' ', /*'-',*/ '/', '\\', '\b', '\t', };

#if UNIT_TEST
        /// <summary>
        /// Warning : only use in test code!
        /// </summary>
        public static Dictionary<string, Command> CommandLookup => m_commandLookup;
#endif

        static ConsoleCommand()
        {
            s_CommandLookup = new SortedDictionary<string, Command>();
            s_Args = new Queue<string>(4);
            s_AutoCompleteCache = new List<string>(8);

            s_TypeLookup = new Dictionary<Type, TypeParse>()
            {
                {typeof(string), (string i, out object o)=>{ o = i; return true; } },
                {typeof(int), (string i,out object o)=> {bool res = int.TryParse(i,out int v); o = v; return res; } },
                {typeof(float), (string i, out object  o)=>{bool res = float.TryParse(i,out float v); o = v; return res; } },
                {typeof(bool), (string i,  out object o) => {bool res = bool.TryParse(i,out bool v); o = v; return res; } },
                {typeof(UnityEngine.Vector2), ParseVector2 },
                {typeof(UnityEngine.Vector3), ParseVector3 },
                {typeof(UnityEngine.Vector4), ParseVector4 },
                {typeof(UnityEngine.GameObject), ParseGameobject },
                // Add more types here
            };

            AddCommandStatic(typeof(Commands));
        }

        #region Command

        public static void AddCommandInstance(Type classType, object instance)
        {
            if (instance == null)
            {
                UnityEngine.Debug.LogError("Instance couldn't be null!");
                return;
            }

            MethodInfo[] methods = classType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (MethodInfo method in methods)
            {
                if (method != null)
                {
                    CommandAttribute attribute = method.GetCustomAttribute<CommandAttribute>();
                    if (attribute != null)
                        InternalAddCommand(attribute.Command, attribute.Description, method, instance);
                }
            }
        }

        public static void RemoveCommand(string command)
        {
            if (s_CommandLookup.ContainsKey(command))
            {
                s_CommandLookup.Remove(command);
            }
        }

        public static void ExecuteCommand(string commandLine)
        {
            // parse command
            // [0] is command
            // other is parameter
            Queue<string> args = ParseCommandLine(commandLine);
            if (args.Count <= 0)
            {
                UnityEngine.Debug.LogError("Command shouln't be null");
                return;
            }

            string commandStr = args.Dequeue();

            if (!s_CommandLookup.TryGetValue(commandStr, out Command command))
            {
                UnityEngine.Debug.LogError("Can't find this command : " + commandStr);
            }
            else if (!command.IsValid())
            {
                UnityEngine.Debug.LogError("This command is not valid : " + commandStr);
            }
            else
            {
                if (!command.IsParamsCountMatch(args.Count))
                {
                    UnityEngine.Debug.LogErrorFormat("{0} : Parameters count mismatch, expected count : {1}. Type : {2}", commandStr, command.ParamsTypes.Length, string.Join<object>(",", command.ParamsTypes));
                    return;
                }

                object[] paramters = new object[command.ParamsTypes.Length];
                for (int i = 0; i < command.ParamsTypes.Length; i++)
                {
                    if (!s_TypeLookup.TryGetValue(command.ParamsTypes[i], out TypeParse typeParse))
                    {
                        UnityEngine.Debug.LogError("This paramter type is unsupported : " + command.ParamsTypes[i].Name);
                        return;
                    }

                    if (typeParse?.Invoke(args.Peek(), out paramters[i]) == false)
                    {
                        UnityEngine.Debug.LogErrorFormat("Can't parse {0} to type {1}", args.Peek(), command.ParamsTypes[i].Name);
                    }

                    args.Dequeue();
                }
                // call method
                command.Execute(paramters);
            }
        }

        public static void AddAllCommandStatic()
        {
            IEnumerable<Type> types = ReflectionUtil.GetAllAssemblyTypes;
            foreach (Type type in types)
            {
                AddCommandStatic(type);
            }
        }

        public static void AddCommandStatic(Type classType)
        {
            MethodInfo[] methods = classType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (MethodInfo method in methods)
            {
                if (method != null)
                {
                    CommandAttribute attribute = method.GetCustomAttribute<CommandAttribute>();
                    if (attribute != null)
                        InternalAddCommand(attribute.Command, attribute.Description, method, null);
                }
            }
        }

        private static void InternalAddCommand(string command, string description, MethodInfo methodInfo, object instance)
        {
            // check command name
            foreach (char chr in command)
            {
                if (s_InvalidChrsForCommandName.Contains(chr))
                {
                    UnityEngine.Debug.LogError("Command name counldn't contains this character : " + chr);
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
                if (s_TypeLookup.ContainsKey(type))
                {
                    paramTypes[i] = type;
                }
                else
                {
                    // command is not valid, return
                    UnityEngine.Debug.LogError("Unsupported type : " + type);
                    return;
                }
            }

            // parse method info
            StringBuilder sb = new StringBuilder(128);
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
            s_CommandLookup[command] = new Command(methodInfo, paramTypes, instance, sb.ToString()); ;
        }

        #endregion


        #region AutoComplete

        public static string AutoComplete()
        {
            if (s_AutoCompleteCache.Count == 0) return null;

            if (++s_IdxOfCommandCache >= s_AutoCompleteCache.Count) s_IdxOfCommandCache = 0;
            return s_AutoCompleteCache[s_IdxOfCommandCache];
        }

        public static void GetPossibleCommand(string header)
        {
            if (string.IsNullOrEmpty(header))
            {
                throw new Exception("Header couldn't be Null or Empty");
            }

            s_IdxOfCommandCache = -1;
            s_AutoCompleteCache.Clear();

            foreach (string k in s_CommandLookup.Keys)
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
            //UnityEngine.Debug.Log(sb.ToString());
            // ---------------------------------------------
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

        // (x,y)
        private static bool ParseVector2(string input, out object output)
        {
            if (string.IsNullOrEmpty(input))
            {
                output = new UnityEngine.Vector2();
                return false;
            }

            string[] xy = input.Replace('(', ' ').Replace(')', ' ').Split(',');
            float x = 0, y = 0;

            if (xy.Length >= 2)
            {
                string tmpX = xy[0].Trim();
                if (!string.IsNullOrEmpty(tmpX))
                {
                    float.TryParse(tmpX, out x);
                }

                string tmpY = xy[1].Trim();
                if (!string.IsNullOrEmpty(tmpY))
                {
                    float.TryParse(tmpY, out y);
                }

                output = new UnityEngine.Vector2(x, y);
                return true;
            }


            output = new UnityEngine.Vector2();
            return false;
        }

        // (x,y,z)
        private static bool ParseVector3(string input, out object output)
        {
            if (string.IsNullOrEmpty(input))
            {
                output = new UnityEngine.Vector3();
                return false;
            }

            string[] xyz = input.Replace('(', ' ').Replace(')', ' ').Split(',');
            float x = 0, y = 0, z = 0;

            if (xyz.Length >= 3)
            {
                string tmpX = xyz[0].Trim();
                if (!string.IsNullOrEmpty(tmpX))
                {
                    float.TryParse(tmpX, out x);
                }

                string tmpY = xyz[1].Trim();
                if (!string.IsNullOrEmpty(tmpY))
                {
                    float.TryParse(tmpY, out y);
                }

                string tmpZ = xyz[2].Trim();
                if (!string.IsNullOrEmpty(tmpZ))
                {
                    float.TryParse(tmpZ, out z);
                }

                output = new UnityEngine.Vector3(x, y, z);
                return true;
            }

            output = new UnityEngine.Vector3();
            return false;
        }

        private static bool ParseVector4(string input, out object output)
        {
            if (string.IsNullOrEmpty(input))
            {
                output = new UnityEngine.Vector4();
                return false;
            }

            string[] xyz = input.Replace('(', ' ').Replace(')', ' ').Split(',');
            float x = 0, y = 0, z = 0, w = 0;

            if (xyz.Length >= 4)
            {
                string tmpX = xyz[0].Trim();
                if (!string.IsNullOrEmpty(tmpX))
                {
                    float.TryParse(tmpX, out x);
                }

                string tmpY = xyz[1].Trim();
                if (!string.IsNullOrEmpty(tmpY))
                {
                    float.TryParse(tmpY, out y);
                }

                string tmpZ = xyz[2].Trim();
                if (!string.IsNullOrEmpty(tmpZ))
                {
                    float.TryParse(tmpZ, out z);
                }

                string tmpW = xyz[3].Trim();
                if (!string.IsNullOrEmpty(tmpW))
                {
                    float.TryParse(tmpW, out w);
                }

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

        internal static List<Command> GetAllCommands()
        {
            return s_CommandLookup.Values.ToList();
        }

        internal static Command TryGetCommand(string commandStr)
        {
            if (s_CommandLookup.TryGetValue(commandStr, out Command command))
                return command;

            return null;
        }
    }
}
