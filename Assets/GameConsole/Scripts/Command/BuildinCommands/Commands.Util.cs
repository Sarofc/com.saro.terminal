using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using UnityEngine;

namespace Saro.Console
{
    public partial class Commands
    {
        [Command("description", "控制台信息")]
        public static void console_description()
        {
            var sb = new StringBuilder(64);

            sb.AppendLine("名称 : Game CLI")
              .AppendLine($"版本: {LogConsole.Version}");

            //.AppendLine()
            //.AppendLine("一个运行时")

            Debug.Log(sb.ToString());
        }

        [Command("manual", "手册")]
        public static void user_manual(string commandStr)
        {
            var command = ConsoleCommand.TryGetCommand(commandStr);

            Debug.Log(command?.ToString());
        }

        [Command("all-commands", "打印所有命令")]
        public static void log_all_commands()
        {
            var sb = new StringBuilder(128);
            foreach (var v in ConsoleCommand.GetAllCommands())
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
                var path = Path.Combine(Application.persistentDataPath, DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss") + ".txt");
                File.WriteAllText(path, LogConsole.Instance.GetLog());

                Debug.Log("Save log file to : " + path);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
        }
    }
}
