using System;

namespace Saro.Console
{
    [System.AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    sealed class CommandAttribute : Attribute
    {
        private readonly string m_command;
        private readonly string m_description;

        public CommandAttribute(string command, string description)
        {
            m_command = command;
            m_description = description;
        }

        public string Command => m_command;
        public string Description => m_description;
    }
}
