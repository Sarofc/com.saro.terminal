using System;
using System.IO;
using UnityEngine;

namespace Saro.Terminal
{
    public partial class BuildInCommands
    {
        [Command("core.terminal_info", "控制台信息")]
        public static void terminal_info()
        {
            var sb = StringBuilderCache.Acquire();

            sb.Append(typeof(Terminal).Name)
              .Append("-v")
              .Append(Terminal.k_Version);

            Terminal.Log(StringBuilderCache.GetStringAndRelease(sb));
        }

        //[Command("core.manual", "手册")]
        //public static void user_manual(string commandStr)
        //{
        //    Command command = Terminal.Shell.TryGetCommand(commandStr);

        //    Terminal.Log(command?.ToString());
        //}

        [Command("core.all_commands", "所有命令")]
        public static void all_commands()
        {
            var sb = StringBuilderCache.Acquire();
            foreach (Command v in Terminal.Shell.GetAllCommands())
            {
                sb.AppendLine(v.ToString());
            }

            Terminal.Log(StringBuilderCache.GetStringAndRelease(sb));
        }

        [Command("core.save_log", "保存日志")]
        public static void save_log()
        {
            try
            {
                string path = Path.Combine(Application.persistentDataPath, DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss") + ".txt");
                File.WriteAllText(path, Terminal.Console.GetLog());

                Terminal.Log("Save log file to : " + path);
            }
            catch (Exception e)
            {
                Terminal.LogError(e.Message);
            }
        }

        [Command("core.clear_cmd_histories", "清理命令历史")]
        public static void clear_cmd_histories()
        {
            Terminal.Shell.ClearCommandHistory();
        }

        [Command("core.clear_log", "清除所有日志")]
        public static void clear_log()
        {
            Terminal.Console.ClearLog();
        }
    }
}
