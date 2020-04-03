using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Saro.Console
{
    public partial class Commands
    {
        [Command("description", "控制台信息")]
        public static void console_description()
        {
            StringBuilder sb = new StringBuilder(64);

            sb.AppendLine("名称 : Game CLI")
              .AppendLine($"版本: {DevConsole.k_Version}");

            //.AppendLine()
            //.AppendLine("一个运行时")

            Debug.Log(sb.ToString());
        }

        [Command("manual", "手册")]
        public static void user_manual(string commandStr)
        {
            ConsoleCommand.Command command = ConsoleCommand.TryGetCommand(commandStr);

            Debug.Log(command?.ToString());
        }

        [Command("all-commands", "打印所有命令")]
        public static void log_all_commands()
        {
            StringBuilder sb = new StringBuilder(128);
            foreach (ConsoleCommand.Command v in ConsoleCommand.GetAllCommands())
            {
                sb.AppendLine(v.ToString());
            }
            Debug.Log(sb.ToString());
        }

        [Command("save-log", "保存日志")]
        public static void save_log()
        {
            try
            {
                string path = Path.Combine(Application.persistentDataPath, DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss") + ".txt");
                File.WriteAllText(path, DevConsole.Get().GetLog());

                Debug.Log("Save log file to : " + path);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
        }
    }
}
