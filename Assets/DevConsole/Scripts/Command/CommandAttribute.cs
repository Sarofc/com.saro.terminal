using System;

namespace Saro.Console
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public class CommandAttribute : Attribute
    {
        public string Command => m_Command;
        private readonly string m_Command;

        public string Description => m_Description;
        private readonly string m_Description;

        public CommandAttribute(string command, string description)
        {
            m_Command = command;
            m_Description = description;
        }
    }
}
