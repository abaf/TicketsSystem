using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketSystem.Core;
using TicketSystem.LogHelper;
namespace TicketSystem.GlobalContext
{
    public class ThreadContext
    {
        private static object SyncRoot = new object();
        public static Route Route { get; set; }

        private static LogUtil _LogHelper;

        public static LogUtil LogHelper
        {
            get
            {
                if (_LogHelper != null)
                    return _LogHelper;

                lock (SyncRoot)
                {
                    if (_LogHelper == null)
                        _LogHelper = new LogUtil();
                }

                return _LogHelper;
            }
        }


        private static GlobalConfig _GlobalConfig;
        public static GlobalConfig GlobalConfig
        {
            get
            {
                if (_GlobalConfig != null)
                    return _GlobalConfig;

                lock (SyncRoot)
                {
                    if (_GlobalConfig == null)
                        _GlobalConfig = new GlobalConfig();
                }

                return _GlobalConfig;
            }
        }

        public static string RequestQueueName = "Tickets:REQ";
        public static string ResponseQueueName = "Tickets:RSP";
    }
}
