using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketSystem.LogHelper
{
    public class LogConfig
    {
        public LogLevel LogLevel { get; set; }
        public string LogPath { get; set; }
        public LogMonitor LogMonitor { get; set; }
        public string LogEncoding { get; set; }
        public LogType LogType { get; set; }
        public ConsoleColor ConsoleColor { get; set; }

        /// <summary>
        /// max file size, in MB
        /// </summary>
        public int MaxLogFileSize { get; set; }

        public LogConfig()
        {
            LogLevel = LogLevel.Info;
            LogPath = Path.Combine(System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "Log");
            LogEncoding = "UTF8";
            LogType = LogType.DisplayAndLogFile;
            MaxLogFileSize = 20;

            ConsoleColor = new ConsoleColor();
            ConsoleColor.Colors = new List<LogColor>();
            ConsoleColor.Colors.Add(new LogColor() { Level = "Debug", BackgroundColor = "Black", ForegroundColor = "Gray" });
            ConsoleColor.Colors.Add(new LogColor() { Level = "Info", BackgroundColor = "Black", ForegroundColor = "DarkGreen" });
            ConsoleColor.Colors.Add(new LogColor() { Level = "Warning", BackgroundColor = "Yellow", ForegroundColor = "Magenta" });
            ConsoleColor.Colors.Add(new LogColor() { Level = "Debug", BackgroundColor = "Yellow", ForegroundColor = "Red" });
        }
    }

    public class ConsoleColor
    {
        public List<LogColor> Colors { get; set; }
    }

    public class LogColor
    {
        public string Level { get; set; }

        public string BackgroundColor { get; set; }
        public string ForegroundColor { get; set; }
    }
}
