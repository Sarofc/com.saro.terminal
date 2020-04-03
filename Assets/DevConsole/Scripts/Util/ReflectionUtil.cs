using System;
using System.Collections.Generic;
using System.Reflection;

namespace Saro.Console
{
    public static class ReflectionUtil
    {
        private static IEnumerable<Type> s_AllAssemblyTypes;

        public static IEnumerable<Type> GetAllAssemblyTypes
        {
            get
            {
                if (s_AllAssemblyTypes == null)
                {
                    s_AllAssemblyTypes = Assembly.Load("Assembly-CSharp").GetTypes();
                }
                return s_AllAssemblyTypes;
            }
        }
    }
}
