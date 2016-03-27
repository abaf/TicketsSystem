using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketSystem.LogHelper
{
    public class FileLogger
    {
        public const int MAX_FILE_SIZE = 30 * 1024 * 1024; //30MB
        private string LogRootPath { get; set; }
        private FileStream fLogFileStream = null;
        private string LogFileName = string.Empty;
        private Encoding MsgEncoding = Encoding.GetEncoding("UTF-8");
        private int MaxFileSize = MAX_FILE_SIZE;

        private string moduleName;
        public FileLogger(LogConfig config, string moduleName)
        {
            this.moduleName = moduleName;
            #region Encoding
            try
            {
                MsgEncoding = Encoding.GetEncoding(config.LogEncoding);
            }
            catch (Exception)
            {
                MsgEncoding = Encoding.GetEncoding("UTF-8");
            }

            #endregion

            #region log Path

            LogRootPath = config.LogPath;
            if (string.IsNullOrWhiteSpace(LogRootPath))
                LogRootPath = Path.Combine(System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "Log");

            if (!Directory.Exists(LogRootPath))
                Directory.CreateDirectory(LogRootPath);

            #endregion

            MaxFileSize = config.MaxLogFileSize * 1024 * 1024;
        }

        public void Open()
        {
            string fileNamePattern = String.Format("{0}.{1}*.log", moduleName, DateTime.Now.ToString("yyyyMMdd"));

            // get max number of today's log
            int maxLogFileSeqNo = 0;
            string[] files = Directory.GetFiles(LogRootPath, string.Format("*{0}.{1}*.*", moduleName, DateTime.Now.ToString("yyyyMMdd")), SearchOption.TopDirectoryOnly);
            foreach (var filename in files)
            {
                var ymd = DateTime.Now.ToString("yyyyMMdd");
                int sindex = filename.LastIndexOf(ymd) + ymd.Length;
                int eindex = filename.LastIndexOf(".");
                string snum = filename.Substring(sindex, eindex - sindex);
                if (!string.IsNullOrWhiteSpace(snum))
                {
                    int num = 0;
                    if (Int32.TryParse(snum, out num) && num > maxLogFileSeqNo)
                        maxLogFileSeqNo = num;
                }
            }

            string logFileName = null;
            int tryOpenCount = 1000;
            while (tryOpenCount-- > 0)
            {
                logFileName = Path.Combine(LogRootPath, String.Format("{0}.{1}.{2}.log", moduleName, DateTime.Now.ToString("yyyyMMdd"), maxLogFileSeqNo));

                FileInfo info = new FileInfo(logFileName);
                if (info.Exists && info.Length >= MaxFileSize)
                {
                    maxLogFileSeqNo++;
                    logFileName = Path.Combine(LogRootPath, String.Format("{0}.{1}.{2}.log", moduleName, DateTime.Now.ToString("yyyyMMdd"), maxLogFileSeqNo));

                }

                if (OpenLoggerFile(logFileName))
                    break;

                maxLogFileSeqNo++;
            }
        }

        public void Write(string strMsg)
        {
            if (string.IsNullOrWhiteSpace(strMsg) || fLogFileStream == null) return;

            byte[] info = MsgEncoding.GetBytes(strMsg);
            fLogFileStream.Write(info, 0, info.Length);

            fLogFileStream.Flush();
        }
        public void Close()
        {
            if (fLogFileStream != null)
                fLogFileStream.Flush();

            if (fLogFileStream != null)
                fLogFileStream.Close();

            if (fLogFileStream != null)
                fLogFileStream.Dispose();
        }


        private bool OpenLoggerFile(string filename)
        {
            try
            {
                if (!File.Exists(filename))
                {
                    FileInfo fi = new FileInfo(filename);
                    using (fi.Create())
                    { }
                    fi.CreationTime = DateTime.Now;
                }

                fLogFileStream = new FileStream(filename, FileMode.Append, FileAccess.Write, FileShare.Read);

                string msg = String.Format("========================== {0} ========================\r\n", DateTime.Now.ToString());
                if (fLogFileStream.Position > 0)
                {
                    msg = "\r\n" + msg;
                }
                Write(msg);

                LogFileName = Path.GetFileNameWithoutExtension(filename);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
