using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketSystem.LogHelper
{
    class LogUtil
    {
        public static LogLevel LogLevel { get; set; }

        private LogQueue LogQueue { get; set; }

        static LogUtil()
        {
            var config = new LogConfig();
            LogLevel = config.LogLevel;
        }

        public LogUtil()
        {
            LogQueue = LogQueue.GetInstance();
        }

        #region Info Log methods
        public LogData LogInfoMsg(string strFormat, params object[] args)
        {
            if (LogQueue.LogThread == null || LogUtil.LogLevel > LogLevel.Info) return null;

            return LogInfoMsg(LogType.DisplayAndLogFile, strFormat, args);
        }

        public LogData LogInfoMsg(LogType logType, string strFormat, params object[] args)
        {
            if (LogQueue.LogThread == null || LogUtil.LogLevel > LogLevel.Info) return null;

            return LogInfoMsg(logType, null, strFormat, args);
        }

        #endregion

        #region Error log method

        public LogData LogErrMsg(string strFormat, params object[] args)
        {
            return LogErrMsg(LogType.DisplayAndLogFile, strFormat, args);
        }

        public LogData LogErrMsg(LogType logType, string strFormat, params object[] args)
        {
            return LogErrMsg(logType, null, strFormat, args);
        }

        public LogData LogErrMsg(Exception error)
        {
            return LogErrMsg(LogType.DisplayAndLogFile, error, null, null);
        }

        public LogData LogErrMsg(Exception error, string strFormat, params object[] args)
        {
            return LogErrMsg(LogType.DisplayAndLogFile, error, strFormat, args);
        }

        public LogData LogErrMsg(LogType logType, Exception error, string strFormat, params object[] args)
        {
            LogData data = new LogData(logType, LogLevel.Error, error, strFormat, args);

            LogQueue.Enqueue(data);

            return data;
        }

        #endregion

        #region Debug log methods

        public LogData LogDebugMsg(string strFormat, params object[] args)
        {
            if (LogQueue.LogThread == null || LogUtil.LogLevel > LogLevel.Debug) return null;

            return LogDebugMsg(LogType.LogFileOnly, strFormat, args);
        }

        public LogData LogDebugMsg(LogType logType, string strFormat, params object[] args)
        {
            if (LogQueue.LogThread == null || LogUtil.LogLevel > LogLevel.Debug) return null;

            return LogDebugMsg(logType, null, strFormat, args);
        }
        #endregion

        #region Warning log methods

        public LogData LogWarnMsg(string strFormat, params object[] args)
        {
            if (LogQueue.LogThread == null || LogUtil.LogLevel > LogLevel.Warning) return null;

            return LogWarnMsg(LogType.DisplayAndLogFile, strFormat, args);
        }

        public LogData LogWarnMsg(LogType logType, string strFormat, params object[] args)
        {
            if (LogQueue.LogThread == null || LogUtil.LogLevel > LogLevel.Warning) return null;

            return LogWarnMsg(logType, null, strFormat, args);
        }

        public LogData LogWarnMsg(LogType logType, Exception error, string strFormat, params object[] args)
        {
            if (LogQueue.LogThread == null || LogUtil.LogLevel > LogLevel.Warning) return null;

            LogData data = new LogData(logType, LogLevel.Warning, error, strFormat, args);

            LogQueue.Enqueue(data);

            return data;
        }

        #endregion
    }
}
