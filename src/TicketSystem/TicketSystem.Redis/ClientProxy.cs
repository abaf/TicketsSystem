using ServiceStack.Redis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TicketSystem.GlobalContext;
namespace TicketSystem.Redis
{
    /// <summary>
    /// The proxy to send/get data from redis server
    /// </summary>
    public class ClientProxy
    {
        /// <summary>
		/// The key of the response queue
		/// </summary>
		string QueueKey = "";

        /// <summary>
        /// Redis client
        /// </summary>
        internal IRedisClient RequestClient { get; private set; }

        DateTime LastActiveTime;

        /// <summary>
        /// 接收应答数据的线程
        /// </summary>
        Thread ResponseThread = null;

        /// <summary>
        /// 停止事件
        /// </summary>
        ManualResetEvent StopEvent = new ManualResetEvent(false);

        /// <summary>
        /// 退出事件
        /// </summary>
        ManualResetEvent ExitEvent = new ManualResetEvent(false);

        private Action<RedisPackage> Callback;

        #region Properties

        /// <summary>
        /// The redis host of current channel
        /// </summary>
        public RedisHost Host { get; private set; }

        /// <summary>
        /// Is redis connection in normal status
        /// </summary>
        public bool IsValid
        {
            get
            {
                return !IsDisposed && 
                       !StopEvent.WaitOne(0) && 
                       !IsException && 
                        IsWaitingResponse && 
                        (DateTime.Now - LastActiveTime).TotalMilliseconds <= Timeouts.ServiceTTL;
            }
        }

        public bool IsWaitingResponse { get { return ResponseThread != null && ResponseThread.IsAlive; } }

        public bool IsException { get; private set; }

        public bool IsBusy { get; private set; }

        public bool IsDisposed { get; private set; }

        /// <summary>
        /// The expire time of the queue in second
        /// </summary>
        public int ExpireTimeout { get; set; }

        #endregion

        public ClientProxy(RedisHost host, string watchingQueue, Action<RedisPackage> callback)
        {
            if (host == null)
                throw new ArgumentNullException("host");

            this.Host = host;
            this.Callback = callback;
            ExpireTimeout = 60; // default expire time of the queue

            //init redis client
            var client = new RedisClient(Host.Host, Host.Port, Host.Password, Host.SSL);
            client.ConnectTimeout = Timeouts.WaitConnection;
            client.SendTimeout = Timeouts.SocketSendTimeout;
            client.ReceiveTimeout = Timeouts.SocketReceiveTimeout;
            client.Db = Host.DB;
            RequestClient = client;

            QueueKey = watchingQueue;
            LastActiveTime = DateTime.Now - TimeSpan.FromSeconds(ExpireTimeout);
        }

        /// <summary>
		/// Refresh the queue to prevent it expires
		/// </summary>
		void RefreshQueue(RedisClient conn)
        {
            if (conn == null) return;

            //need to refresh the queue continuously
            double interval = (DateTime.Now - LastActiveTime).TotalSeconds;
            if (interval < 0 || interval >= ExpireTimeout / 2)
            {
                //reset the expire time
                conn.Expire(this.QueueKey, ExpireTimeout);

                LastActiveTime = DateTime.Now;
            }
        }

        /// <summary>
        /// Check the connection is ok
        /// </summary>
        public bool Ping()
        {
            try
            {
                using (var client = new RedisClient(Host.Host, Host.Port, Host.Password, Host.SSL))
                {
                    client.ConnectTimeout = Timeouts.WaitConnection;
                    client.SendTimeout = Timeouts.SocketSendTimeout;
                    client.ReceiveTimeout = Timeouts.SocketReceiveTimeout;
                    client.Db = Host.DB;

                    return client.Ping();
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Start listen the redis
        /// </summary>
        public void StartProcess()
        {
            lock (this)
            {
                if ((ResponseThread != null && ResponseThread.IsAlive))
                    return;

                //start a thread to listen the redis
                ResponseThread = new Thread((ThreadStart)delegate
                {
                    StopEvent.Reset();
                    ExitEvent.Reset();

                    this.IsException = true;

                    while (true)
                    {
                        if (StopEvent.WaitOne(0))
                            break;

                        try
                        {
                            FetchResponseProcess();
                        }
                        catch (ThreadAbortException)
                        {
                            break;
                        }
                        catch (Exception err)
                        {
                            this.IsException = true;

                            ThreadContext.LogHelper.LogErrMsg("Error get response data from queue {0}, reason:{1}", Host, err.Message);
                            //If there is exception caught, then wait a while to fetch again
                            if (StopEvent == null || StopEvent.WaitOne(Timeouts.RedisOption))
                                break;
                        }
                    }

                    ResponseThread = null;
                    ExitEvent.Set();
                });

                //start listen thread
                ResponseThread.IsBackground = true;
                ResponseThread.Priority = ThreadPriority.AboveNormal;
                ResponseThread.Start();
            }
        }

        public void StopProcess(int timeout = 5000)
        {
            lock (this)
            {
                if (ResponseThread == null || !ResponseThread.IsAlive)
                    return;

                bool sameThread = ResponseThread.ManagedThreadId == Thread.CurrentThread.ManagedThreadId;

                StopEvent.Set();

                if (!sameThread && !ExitEvent.WaitOne(timeout))
                {
                    try
                    {
                        ResponseThread.Abort();
                    }
                    catch (Exception) { }
                    finally { ResponseThread = null; }
                }

                try
                {
                    //If the queue still have data, the popup a warning
                    if ((RequestClient as RedisClient).Exists(QueueKey) == 1
                        && (RequestClient as RedisClient).Type(QueueKey) == "list"
                        && (RequestClient as RedisClient).LLen(QueueKey) > 0 )
                        ThreadContext.LogHelper.LogWarnMsg("The queue {0} still have data, and will be clean up!", QueueKey);
                }
                catch (Exception)
                {
                    //disconnection or redis server is down
                }
            }
        }

        [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions]
        void FetchResponseProcess()
        {
            using (var conn = new RedisClient(Host.Host, Host.Port, Host.Password, Host.SSL))
            {
                conn.ConnectTimeout = Timeouts.WaitConnection;
                conn.ReceiveTimeout = Timeouts.SocketReceiveTimeout;
                conn.SendTimeout = Timeouts.SocketSendTimeout;

                #region Test the connectivity

                try
                {
                    conn.Db = Host.DB;

                    if (!conn.Ping())
                        throw new Exception(string.Format("Failed to Ping the redis server {0}", this.Host));
                }
                catch (Exception)
                {
                    StopEvent.WaitOne(Timeouts.WaitConnection);
                    throw;
                }

                //Try to ping the request client object
                if (RequestClient != null)
                {
                    lock (RequestClient)
                    {
                        try
                        {
                            this.IsBusy = true;
                            (RequestClient as RedisClient).Ping();
                        }
                        catch (Exception) { }
                        finally { this.IsBusy = false; }
                    }
                }

                //Ping is ok
                this.IsException = false;

                #endregion

                this.IsException = false;

                while (true)
                {
                    RefreshQueue(conn);

                    #region get response data

                    byte[] response = null;

                    //the timeout for query the response data
                    int timeout = Math.Min(conn.ReceiveTimeout, 5000);

                    byte[][] datas = null;

                    datas = conn.BRPop(QueueKey, (int)(timeout / 1000));

                    this.IsException = false;

                    if (datas == null || datas.Length != 2)
                    {
                        if (StopEvent.WaitOne(0))
                            break;

                        continue;
                    }

                    // response: {"queuename","value"}
                    response = datas[1];

                    #endregion

                    #region parse response data

                    RedisPackage pkg = new RedisPackage();
                    pkg.FromBytes(response);

                    try
                    {
                        Stopwatch sw = Stopwatch.StartNew();

                        //process response data
                        if (Callback != null)
                        {
                            Callback(pkg);
                        }
                    }
                    catch (ThreadAbortException) { throw; }
                    catch (Exception err)
                    {
                        ThreadContext.LogHelper.LogErrMsg(err, "RequestID={0}|Error process response data", pkg.RequestId);
                    }

                    #endregion
                }
            }
        }

        /// <summary>
        /// Send data request
        /// </summary>
        /// <param name="service">Queue name</param>
        /// <param name="requestId"></param>
        /// <param name="request"></param>
        /// <param name="timeout"></param>
        [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions]
        public void SendRequest(string service, long requestId, byte[] sessionData, byte[] request, Int32 timeout = 1000)
        {
            if (timeout < 0)
                throw new Exception("Invalid timeout value");

            //check the response thread
            if (ResponseThread == null || !ResponseThread.IsAlive || ExitEvent.WaitOne(0))
                throw new Exception("Response thread not started or exited");

            //serialization
            byte[] buffer = new RedisPackage
            {
                Service = service,
                RequestId = requestId,
                OccurTime = DateTime.Now.Ticks,
                Data = request,
                SessionData = sessionData
            }.ToBytes();

            try
            {
                #region send request

                lock (RequestClient)
                {
                    if (this.IsException || StopEvent.WaitOne(0))
                        throw new Exception("Request not sent!");

                    try
                    {
                        this.IsBusy = true;

                        if ((RequestClient as RedisClient).LPush(service, buffer) == 0)
                            throw new Exception(string.Format("Error sending request to redis, RequestID: {0}", requestId));

                        this.IsException = false;
                    }
                    catch (Exception err)
                    {
                        this.IsException = true;
                        ThreadContext.LogHelper.LogErrMsg(err, "Send request to {0}, queue {1} failed，reason:{2}\r\n", this.Host, service, err.Message);
                        throw;
                    }
                    finally
                    {
                        this.IsBusy = false;
                    }
                }

                #endregion
            }
            finally
            { }
        }

        public void Dispose()
        {
            lock (this)
            {
                if (IsDisposed) return;

                IsDisposed = true;

                StopProcess();

                if (RequestClient != null)
                {
                    try
                    {
                        lock (RequestClient)
                        {
                            RequestClient.Dispose();
                        }
                    }
                    catch (Exception)
                    {
                    }
                    finally
                    {
                        RequestClient = null;
                    }
                }

                StopEvent.Dispose();
                ExitEvent.Dispose();
            }
        }
    }
}
