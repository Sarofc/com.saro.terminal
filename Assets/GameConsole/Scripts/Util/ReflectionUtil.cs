using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Saro.Util
{
    public static class ReflectionUtil
    {
        private static IEnumerable<Type> m_AllAssemblyTypes;

        public static IEnumerable<Type> GetAllAssemblyTypes
        {
            get
            {
                if (m_AllAssemblyTypes == null)
                {
                    m_AllAssemblyTypes = Assembly.Load("Assembly-CSharp").GetTypes();
                }
                return m_AllAssemblyTypes;
            }
        }
    }
}
