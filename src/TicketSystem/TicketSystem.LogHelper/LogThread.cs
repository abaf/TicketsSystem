using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TicketSystem.Utils;

namespace TicketSystem.LogHelper
{
    class LogThread
    {
        private LogConfig LogConfig = null;

        private LogQueue LogQueue = null;

        private AutoResetEvent NewDataEvent = null;

        private ManualResetEvent StopEvent = null;

		private Thread LoggingThread = null;

        public LogLevel ConsoleLogLevel { get; set; }

        private System.ConsoleColor Debug_ForegroundColor = System.ConsoleColor.White;
        private System.ConsoleColor Debug_BackgroundColor = System.ConsoleColor.Black;
        private System.ConsoleColor Info_ForegroundColor = System.ConsoleColor.White;
        private System.ConsoleColor Info_BackgroundColor = System.ConsoleColor.Black;
        private System.ConsoleColor Warning_ForegroundColor = System.ConsoleColor.White;
        private System.ConsoleColor Warning_BackgroundColor = System.ConsoleColor.Black;
        private System.ConsoleColor Error_ForegroundColor = System.ConsoleColor.White;
        private System.ConsoleColor Error_BackgroundColor = System.ConsoleColor.Black;

        private bool CanSetConsoleColor = true;

        private Dictionary<string, FileLogger> FileLoggers = new Dictionary<string, FileLogger>();

        public LogThread(LogQueue queue, AutoResetEvent dataEvent, ManualResetEvent StopEvent)
        {
            this.LogQueue = queue;
            this.NewDataEvent = dataEvent;
            this.StopEvent = StopEvent;

            CanSetConsoleColor = Environment.UserInteractive;

            LogConfig = new LogConfig();

            try
            {
                ConsoleLogLevel = LogConfig.LogLevel;
            }
            catch (Exception)
            {
                ConsoleLogLevel = LogLevel.None;
            }

            if (ConsoleLogLevel <= LogLevel.Info)
                ConsoleLogLevel = LogLevel.Info;

            #region ConsoleColor
            foreach (var col in LogConfig.ConsoleColor.Colors)
            {
                try
                {
                    if (col.Level.ToUpper().Equals("DEBUG"))
                    {
                        Debug_BackgroundColor = (System.ConsoleColor)Enum.Parse(typeof(System.ConsoleColor), col.BackgroundColor, true);
                        Debug_ForegroundColor = (System.ConsoleColor)Enum.Parse(typeof(System.ConsoleColor), col.ForegroundColor, true);
                    }
                    else if (col.Level.ToUpper().Equals("INFO"))
                    {
                        Info_BackgroundColor = (System.ConsoleColor)Enum.Parse(typeof(System.ConsoleColor), col.BackgroundColor, true);
                        Info_ForegroundColor = (System.ConsoleColor)Enum.Parse(typeof(System.ConsoleColor), col.ForegroundColor, true);
                    }
                    else if (col.Level.ToUpper().Equals("WARNING"))
                    {
                        Warning_BackgroundColor = (System.ConsoleColor)Enum.Parse(typeof(System.ConsoleColor), col.BackgroundColor, true);
                        Warning_ForegroundColor = (System.ConsoleColor)Enum.Parse(typeof(System.ConsoleColor), col.ForegroundColor, true);
                    }
                    else if (col.Level.ToUpper().Equals("ERROR"))
                    {
                        Error_BackgroundColor = (System.ConsoleColor)Enum.Parse(typeof(System.ConsoleColor), col.BackgroundColor, true);
                        Error_ForegroundColor = (System.ConsoleColor)Enum.Parse(typeof(System.ConsoleColor), col.ForegroundColor, true);
                    }
                }
                catch (Exception)
                {
                }
            }

            #endregion


            string LogPath = LogConfig.LogPath;
            if (string.IsNullOrWhiteSpace(LogPath))
                LogPath = Path.Combine(System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "Log");

            if (!Directory.Exists(LogPath))
                Directory.CreateDirectory(LogPath);


            LoggingThread = new Thread(new ThreadStart(StratProcess));
            LoggingThread.IsBackground = true;
            LoggingThread.Start();
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            lock (((ICollection)FileLoggers).SyncRoot)
            {
                foreach (var logger in this.FileLoggers.Values)
                {
                    logger.Close();
                }

                this.FileLoggers.Clear();
            }
        }

        private void StratProcess()
        {
            OnLogData(new LogData(LogType.DisplayAndLogFile, LogLevel.Info, "Log thread started"));

            while (true)
            {
                try
                {
                    ProcessDataInQueue();

                    int index = AutoResetEvent.WaitAny(new WaitHandle[2] { StopEvent, NewDataEvent }, 1000, true);
                    if (index == 0)
                    {
                        ProcessDataInQueue();

                        OnLogData(new LogData(LogType.DisplayAndLogFile, LogLevel.Info, "Log thread exited"));
                        OnLogData(null); 

                        Dispose();
                        break;
                    }
                }
                catch (Exception err)
                {
                    Trace.WriteLine(err.Message);
                }
            }
        }

        private void ProcessDataInQueue()
        {
            LogData data = null;

            if (LogQueue.Count == 0)
            { 
                return;
            }

            while (LogQueue.Count > 0)
            {
                data = LogQueue.Dequeue();
                if (data == null) break;
                try
                {
                    OnLogData(data);
                }
                catch (Exception err)
                {
                    Trace.WriteLine(string.Format("Error：{0}", err.Message));
                }
            }
        }

        private static LogLevel LastedLogLevel = LogLevel.None; 
        private static DateTime LastedLogTime = DateTime.Now; 
        private static StringBuilder ConsoleLogCache = new StringBuilder();


        private void OnLogData(LogData data)
        {
            if (ConsoleLogCache.Length > 0 && (DateTime.Now - LastedLogTime).TotalMilliseconds >= 200)
            {
                #region output screen cache

                if (Environment.UserInteractive)
                {
                    string logLine = ConsoleLogCache.ToString();

                    if (Environment.UserInteractive)
                    {
                        SetConsoleColor(LastedLogLevel);

                        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                            WindowsConsoleHelper.Write(logLine);
                        else
                            Console.Write(logLine);
                    }
                }

                #endregion

                ConsoleLogCache.Clear();
                LastedLogTime = DateTime.Now;
            }

            if (data == null)
                return;

            string strLogMsg = null;

            if (data.LogType != LogType.LogFileOnly
                && data.LogLevel >= ConsoleLogLevel
                && Environment.UserInteractive)
            {
                strLogMsg = data.ToLogString();

                {
                    if (ConsoleLogCache.Length > 0
                        && (LastedLogLevel != data.LogLevel
                          || (DateTime.Now - LastedLogTime).TotalMilliseconds >= 200
                          || ConsoleLogCache.Length >= 32 * 1024)
                        )
                    {
                        string logLine = ConsoleLogCache.ToString();

                        if (Environment.UserInteractive)
                        {
                            SetConsoleColor(LastedLogLevel);

                            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                                WindowsConsoleHelper.Write(logLine);
                            else
                                Console.Write(logLine);
                        }
                        //clean up
                        ConsoleLogCache.Clear();
                        LastedLogTime = DateTime.Now;
                    }
                }
                ConsoleLogCache.AppendLine(strLogMsg);
                LastedLogLevel = data.LogLevel;
            }

            if (data.LogType == LogType.DisplayOnly)
                return;

            if (string.IsNullOrEmpty(strLogMsg))
                strLogMsg = data.ToLogString();

            WriteLogData("Default", data, strLogMsg + "\r\n");
            if (data.LogLevel == LogLevel.Error || data.LogLevel == LogLevel.Warning)
            {
                WriteLogData("Error", data, strLogMsg + "\r\n");
            }
        }

        private void WriteLogData(string suffix, LogData data, string msg = null)
        {
            if (msg == null)
                msg = data.ToLogString();

            if (!this.FileLoggers.ContainsKey(suffix))
            {
                lock (((ICollection)FileLoggers).SyncRoot)
                {
                    FileLoggers[suffix] = new FileLogger(LogConfig, suffix);
                    FileLoggers[suffix].Open();
                }
            }

            var logger = this.FileLoggers[suffix];
            logger.Write(msg);
        }

        private void SetConsoleColor(LogLevel logLevel)
        {
            if (!CanSetConsoleColor) return;

            try
            {
                if (logLevel == LogLevel.Debug)
                {
                    Console.BackgroundColor = Debug_BackgroundColor;
                    Console.ForegroundColor = Debug_ForegroundColor;
                }
                else if (logLevel == LogLevel.Info)
                {
                    Console.BackgroundColor = Info_BackgroundColor;
                    Console.ForegroundColor = Info_ForegroundColor;
                }
                else if (logLevel == LogLevel.Warning)
                {
                    Console.BackgroundColor = Warning_BackgroundColor;
                    Console.ForegroundColor = Warning_ForegroundColor;
                }
                else if (logLevel == LogLevel.Error)
                {
                    Console.BackgroundColor = Error_BackgroundColor;
                    Console.ForegroundColor = Error_ForegroundColor;
                }
            }
            catch (Exception)
            {
                CanSetConsoleColor = false;
            }
        }

    }
}
