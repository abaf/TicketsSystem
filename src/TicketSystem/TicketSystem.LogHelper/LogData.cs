using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketSystem.LogHelper
{
    public class LogData
    {
        public LogLevel LogLevel { get; internal set; }

        private LogType _LogType = LogType.DisplayAndLogFile;
 
        public LogType LogType
        {
            get { return _LogType; }
            internal set { _LogType = value; }
        }

        public string LogMsg { get; internal set; }

        public string OriLogMsg { get; internal set; }
        public DateTime LogTime { get; internal set; }
        public int ThreadId { get; internal set; }
        public Exception LogException { get; internal set; }
        public string LogFormat { get; private set; }
        public object[] LogFormatArgs { get; private set; }

        public LogData(LogType logType, LogLevel level, string strFormat, params object[] args)
        {
            this.LogLevel = level;
            this.LogType = logType;
            this.LogTime = DateTime.Now;
            this.LogMsg = "";
            this.ThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;

            this.LogFormat = strFormat;
            this.LogFormatArgs = args;

            PrepareLogString();
        }

        public LogData(LogType logType, LogLevel level, Exception error, string strFormat, params object[] args)
        {
            this.LogLevel = level;
            this.LogType = logType;
            this.LogTime = DateTime.Now;
            this.LogMsg = string.Empty;
            this.ThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;

            this.LogFormat = strFormat;
            this.LogFormatArgs = args;
            this.LogException = error;

            this.PrepareLogString();
        }

        private void PrepareLogString()
        {
            if (!string.IsNullOrWhiteSpace(this.LogMsg))
                return;

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("{0:D4}-{1:D2}-{2:D2} {3:D2}:{4:D2}:{5:D2}.{6:D3}",
                                    this.LogTime.Year,
                                    this.LogTime.Month,
                                    this.LogTime.Day,
                                    this.LogTime.Hour,
                                    this.LogTime.Minute,
                                    this.LogTime.Second,
                                    this.LogTime.Millisecond);

            sb.AppendFormat("|Level={2}|ThreadID={3}|", LogLevel, this.ThreadId);

            if (!string.IsNullOrWhiteSpace(this.LogFormat))
            {
                try
                {
                    if (this.LogFormatArgs == null || this.LogFormatArgs.Length == 0)
                        sb.Append(this.LogFormat);
                    else
                        sb.AppendFormat(this.LogFormat, this.LogFormatArgs);
                }
                catch (Exception err)
                {
                    sb.Append(this.LogFormat).Append(err.Message);
                }
            }

            if (this.LogException != null)
            {
                sb.AppendFormat("{0},{1}", this.LogException.Message, this.LogException.StackTrace);

                if (this.LogException.Data != null && this.LogException.Data.Count > 0)
                {
                    sb.Append(",Data");

                    foreach (System.Collections.DictionaryEntry data in this.LogException.Data)
                        sb.AppendFormat(" [Key:{0},Value:{1}]", data.Key, data.Value);
                }
            }

            this.LogMsg = sb.ToString();
        }

        public string ToLogString()
        {
            if (!string.IsNullOrWhiteSpace(this.OriLogMsg))
                return this.OriLogMsg;

            if (!string.IsNullOrWhiteSpace(this.LogMsg))
                return this.LogMsg;

            this.PrepareLogString();

            return this.LogMsg;
        }

    }

    public enum LogLevel
    {
        Debug = 1,
        Info = 2,
        Warning = 4,
        Error = 8,
        None = 16
    }

    /// <summary>
    /// Log type
    /// </summary>
    public enum LogType
    {
        /// <summary>
        /// Display and log file
        /// </summary>
        DisplayAndLogFile = 1,

        /// <summary>
        /// Only display to console
        /// </summary>
        DisplayOnly = 2,

        /// <summary>
        /// Only log to local file
        /// </summary>
        LogFileOnly = 3
    }
}
