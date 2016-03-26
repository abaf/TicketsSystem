using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TicketSystem.LogHelper
{
    public class LogQueue
    {
        private static object SyncRoot = new object();
        private AutoResetEvent NewDataEvent;
        private ManualResetEvent StopEvent;
        internal static LogThread LogThread = null;
        private static LogQueue _Instance = null;
        private static Queue<LogData> _Queue = null;

        /// <summary>
        /// Number of data in queue
        /// </summary>
        public int Count
        {
            get
            {
                return _Queue == null ? 0 : _Queue.Count;
            }
        }

        /// <summary>
        /// private constructor
        /// </summary>
        private LogQueue()
        {
            NewDataEvent = new AutoResetEvent(false);
            StopEvent = new ManualResetEvent(false);
            _Queue = new Queue<LogData>();
            LogThread = new LogThread(this, NewDataEvent, StopEvent);
        }
        public void StartProcess()
        {
        }

        public void StopProcess()
        {
            StopEvent.Set();

            //sleep to switch thread
            Thread.Sleep(0);

            LogThread = null;
        }

        public static LogQueue GetInstance()
        {
            if (_Instance != null)
                return _Instance;

            lock (SyncRoot)
            {
                if (_Instance == null)
                    _Instance = new LogQueue();
            }

            return _Instance;
        }

        internal void Enqueue(LogData item)
        {
            lock (((ICollection)_Queue).SyncRoot)
                       _Queue.Enqueue(item);
            NewDataEvent.Set();
        }

        internal LogData Dequeue()
        {
            LogData data = null;
            lock (((ICollection)_Queue).SyncRoot)
            {
                try
                {
                    if (_Queue.Count > 0)
                        data = _Queue.Dequeue();
                }
                catch (InvalidOperationException)
                {
                    data = null;
                }
            }
            return data;
        }
    }
}
