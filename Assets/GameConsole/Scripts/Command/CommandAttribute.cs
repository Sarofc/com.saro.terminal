using System;

namespace Saro.Console
{
    [System.AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    sealed class CommandAttribute : Attribute
    {
        public string Command => m_command;
        private readonly string m_command;

        public string Description => m_description;
        private readonly string m_description;

        public CommandAttribute(string command, string description)
        {
            m_command = command;
            m_description = description;
        }
    }
}
