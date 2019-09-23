using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Saro.Util;

namespace Saro.Console
{
    /*
     *  Warning :
     *  Don't support command overload !
     *  One command string only bind one command !
     *
     *  example :
     *  int                 - Command 1
     *  float               - Command 1.1
     *  bool                - Command false/False
     *  vector2             - Command (1,1)         // no ' '
     *  vector3             - Command 1,1,1         // '(' ')' is not necessary 
     *  string//gameobject  - Command actor
     */
    public static class ConsoleCommand
    {
        public class Command
        {
            private readonly MethodInfo method;
            private readonly Type[] paramsTypes;
            private readonly object instance;
            private readonly string methodSignature;

            public Command(MethodInfo method, Type[] paramsTypes, object instance, string methodSignature)
            {
                this.method = method;
                this.paramsTypes = paramsTypes;
                this.instance = instance;
                this.methodSignature = methodSignature;
            }

            public bool IsValid()
            {
                if (!method.IsStatic && instance.Equals(null))
                {
                    return false;
                }
                return true;
            }

            public Type[] ParamsTypes => paramsTypes;

            public bool IsParamsCountMatch(int count)
            {
                return paramsTypes.Length == count;
            }

            public void Execute(object[] objects)
            {
                method?.Invoke(instance, objects);
            }

            /// <summary>
            /// complete info, include command and methodinfo
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return methodSignature;
            }
        }

        private delegate bool TypeParse(string input, out object output);

        private static Dictionary<string, Command> m_commandLookup = null;

        private static Dictionary<Type, TypeParse> m_typeLookup = null;

        private static Queue<string> m_args = null;

        private static List<string> m_autoCompleteCache = null;
        private static int m_idxOfCommandCache = -1;

        private static readonly List<char> m_invalidChrsForCommandName = new List<char> { ' ', '-', '/', '\\', '\b', '\t', };

#if UNIT_TEST
        /// <summary>
        /// Warning : only use in test code!
        /// </summary>
        public static Dictionary<string, Command> CommandLookup => m_commandLookup;
#endif

        static ConsoleCommand()
        {
            m_commandLookup = new Dictionary<string, Command>(8);
            m_args = new Queue<string>(3);
            m_autoCompleteCache = new List<string>(8);

            m_typeLookup = new Dictionary<Type, TypeParse>()
            {
                {typeof(string), (string i, out object o)=>{ o = i; return true; } },
                {typeof(int), (string i,out object o)=> {var res = int.TryParse(i,out int v); o = v; return res; } },
                {typeof(float), (string i, out object  o)=>{var res = float.TryParse(i,out float v); o = v; return res; } },
                {typeof(bool), (string i,  out object o) => {var res = bool.TryParse(i,out bool v); o = v; return res; } },
                {typeof(UnityEngine.Vector2), ParseVector2 },
                {typeof(UnityEngine.Vector3), ParseVector3 },
                {typeof(UnityEngine.Vector4), ParseVector4 },
                {typeof(UnityEngine.GameObject), ParseGameobject },
                // Add more types here
            };

            AddAllCommandStatic();
        }

        #region Command

        public static void AddCommandInstance(Type classType, object instance)
        {
            if (instance == null)
            {
                UnityEngine.Debug.LogError("Instance couldn't be null!");
                return;
            }

            var methods = classType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var method in methods)
            {
                if (method != null)
                {
                    var attribute = method.GetCustomAttribute(typeof(CommandAttribute)) as CommandAttribute;
                    if (attribute != null)
                        InternalAddCommand(attribute.Command, attribute.Description, method, instance);
                }
            }
        }

        public static void RemoveCommand(string command)
        {
            if (m_commandLookup.ContainsKey(command))
            {
                m_commandLookup.Remove(command);
            }
        }

        public static void ExecuteCommand(string commandLine)
        {
            // parse command
            // [0] is command
            // other is parameter
            var args = ParseCommandLine(commandLine);
            if (args.Count <= 0)
            {
                UnityEngine.Debug.LogError("Command shouln't be null");
                return;
            }

            var commandStr = args.Dequeue();

            if (!m_commandLookup.TryGetValue(commandStr, out Command command))
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

                var paramters = new object[command.ParamsTypes.Length];
                for (int i = 0; i < command.ParamsTypes.Length; i++)
                {
                    if (!m_typeLookup.TryGetValue(command.ParamsTypes[i], out TypeParse typeParse))
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

        private static void AddAllCommandStatic()
        {
            var types = ReflectionUtil.GetAllAssemblyTypes;
            foreach (var type in types)
            {
                AddCommandStatic(type);
            }
        }

        private static void AddCommandStatic(Type classType)
        {
            var methods = classType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var method in methods)
            {
                if (method != null)
                {
                    var attribute = method.GetCustomAttribute(typeof(CommandAttribute)) as CommandAttribute;
                    if (attribute != null)
                        InternalAddCommand(attribute.Command, attribute.Description, method, null);
                }
            }
        }

        private static void InternalAddCommand(string command, string description, MethodInfo methodInfo, object instance)
        {
            // check command name
            foreach (var chr in command)
            {
                if (m_invalidChrsForCommandName.Contains(chr))
                {
                    UnityEngine.Debug.LogError("Command name counldn't contains this character : " + chr);
                    return;
                }
            }

            // check parameters
            var parameters = methodInfo.GetParameters();
            if (parameters == null) parameters = new ParameterInfo[0];

            var paramTypes = new Type[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                var type = parameters[i].ParameterType;
                if (m_typeLookup.ContainsKey(type))
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
            var sb = new StringBuilder(128);
            sb.Append("-").AppendFormat("<color=red>{0}</color>", command).Append(" : ");

            if (!string.IsNullOrEmpty(description)) sb.Append(description).Append(" -> ");

            sb.Append(methodInfo.DeclaringType.ToString()).Append(".").AppendFormat("<color=yellow>{0}(</color>", methodInfo.Name);

            for (int i = 0; i < paramTypes.Length; i++)
            {
                var type = paramTypes[i];
                sb.AppendFormat("<color=yellow>{0}</color>", type.Name);
                if (i < paramTypes.Length - 1) sb.Append(",");
            }

            sb.Append("<color=yellow>)</color>").Append(" : ").Append(methodInfo.ReturnType.Name);

            // store to map
            m_commandLookup[command] = new Command(methodInfo, paramTypes, instance, sb.ToString()); ;
        }

        #endregion


        #region AutoComplete

        public static string AutoComplete()
        {
            if (m_autoCompleteCache.Count == 0) return null;

            if (++m_idxOfCommandCache >= m_autoCompleteCache.Count) m_idxOfCommandCache = 0;
            return m_autoCompleteCache[m_idxOfCommandCache];
        }

        public static void GetPossibleCommand(string header)
        {
            if (string.IsNullOrEmpty(header))
            {
                throw new Exception("Header couldn't be Null or Empty");
            }

            m_idxOfCommandCache = -1;
            m_autoCompleteCache.Clear();

            foreach (var k in m_commandLookup.Keys)
            {
                if (k.StartsWith(header))
                {
                    m_autoCompleteCache.Add(k);
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
            m_args.Clear();
            if (input == null) return m_args;
            input.Trim();
            if (input.Length == 0) return m_args;

            var args = input.Split(' ');
            foreach (var arg in args)
            {
                var tmp = arg.Trim();
                if (string.IsNullOrEmpty(tmp)) continue;
                m_args.Enqueue(tmp);
            }

            return m_args;
        }

        // (x,y)
        private static bool ParseVector2(string input, out object output)
        {
            if (string.IsNullOrEmpty(input))
            {
                output = new UnityEngine.Vector2();
                return false;
            }

            var xy = input.Replace('(', ' ').Replace(')', ' ').Split(',');
            float x = 0, y = 0;

            if (xy.Length >= 2)
            {
                var tmpX = xy[0].Trim();
                if (!string.IsNullOrEmpty(tmpX))
                {
                    float.TryParse(tmpX, out x);
                }

                var tmpY = xy[1].Trim();
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

            var xyz = input.Replace('(', ' ').Replace(')', ' ').Split(',');
            float x = 0, y = 0, z = 0;

            if (xyz.Length >= 3)
            {
                var tmpX = xyz[0].Trim();
                if (!string.IsNullOrEmpty(tmpX))
                {
                    float.TryParse(tmpX, out x);
                }

                var tmpY = xyz[1].Trim();
                if (!string.IsNullOrEmpty(tmpY))
                {
                    float.TryParse(tmpY, out y);
                }

                var tmpZ = xyz[2].Trim();
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

            var xyz = input.Replace('(', ' ').Replace(')', ' ').Split(',');
            float x = 0, y = 0, z = 0, w = 0;

            if (xyz.Length >= 4)
            {
                var tmpX = xyz[0].Trim();
                if (!string.IsNullOrEmpty(tmpX))
                {
                    float.TryParse(tmpX, out x);
                }

                var tmpY = xyz[1].Trim();
                if (!string.IsNullOrEmpty(tmpY))
                {
                    float.TryParse(tmpY, out y);
                }

                var tmpZ = xyz[2].Trim();
                if (!string.IsNullOrEmpty(tmpZ))
                {
                    float.TryParse(tmpZ, out z);
                }

                var tmpW = xyz[3].Trim();
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


        [Command("help", "Show all commands")]
        private static void LogAllCommand()
        {
            var sb = new StringBuilder(128);
            foreach (var v in m_commandLookup.Values)
            {
                sb.AppendLine(v.ToString());
            }
            UnityEngine.Debug.Log(sb.ToString());
        }


    }
}
