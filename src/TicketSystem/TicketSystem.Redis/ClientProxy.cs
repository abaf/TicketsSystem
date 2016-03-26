using ServiceStack.Redis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TicketSystem.Redis
{
    public class ClientProxy
    {
        /// <summary>
		/// The key of the queue which stores the ticket buying request
		/// </summary>
		/// <param name="clientNo"></param>
		/// <returns></returns>
		internal static string GetResponseQueueKey()
        {
            var process = Process.GetCurrentProcess();
            return string.Format("QUEUE:RSP@{0}@{1}@{2}:{3}", System.Net.Dns.GetHostName(), process.ProcessName, process.Id);
        }

        /// <summary>
		/// The key of the response queue
		/// </summary>
		string RspQueueKey = "";

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

        public ClientProxy(RedisHost host)
        {
            if (host == null)
                throw new ArgumentNullException("host");

            this.Host = host;

            ExpireTimeout = 60; // default expire time of the queue

            //init redis client
            var client = new RedisClient(Host.Host, Host.Port, Host.Password, Host.SSL);
            client.ConnectTimeout = Timeouts.WaitConnection;
            client.SendTimeout = Timeouts.SocketSendTimeout;
            client.ReceiveTimeout = Timeouts.SocketReceiveTimeout;
            client.Db = Host.DB;
            RequestClient = client;

            RspQueueKey = GetResponseQueueKey();
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
                conn.Expire(this.RspQueueKey, ExpireTimeout);

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

                //启动一个接受应答的线程
                ResponseThread = new Thread((ThreadStart)delegate
                {
                    StopEvent.Reset();
                    ExitEvent.Reset();

                    //建立正常的连接前，先认为是有异常的（未准备好），避免发送了请求但是收不到应答
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

                            //应该是连接到Redis出异常了
                            if (DirectoryService.RedisLogHelper != null)
                                DirectoryService.RedisLogHelper.LogErrMsg(err, "队列连线{0}获取应答数据失败，原因：{1}", this.Host, err.Message);
                            else
                                break;

                            //抛出事件
                            try
                            {
                                if (ConnectionException != null)
                                    ConnectionException(this, err);
                            }
                            catch (Exception) { }

                            //有异常发生，稍等一会儿再尝试，避免异常时占用大量CPU
                            if (StopEvent == null || StopEvent.WaitOne(Timeouts.RedisOpt))
                                break;
                        }
                    }

                    //退出时设置，表示线程已经终止
                    ResponseThread = null;
                    ExitEvent.Set();
                });

                //启动线程
                ResponseThread.IsBackground = true;
                ResponseThread.Priority = ThreadPriority.AboveNormal;
                ResponseThread.Start();
            }
        }

        /// <summary>
        /// 停止处理
        /// </summary>
        /// <param name="timeout">超时时间设置</param>
        public void StopProcess(int timeout = 5000)
        {
            lock (this)
            {
                if (ResponseThread == null || !ResponseThread.IsAlive)
                    return;

                bool sameThread = ResponseThread.ManagedThreadId == Thread.CurrentThread.ManagedThreadId;

                //停止事件
                StopEvent.Set();

                //线程退出事件
                if (!sameThread && !ExitEvent.WaitOne(timeout))
                {
                    try
                    {
                        ResponseThread.Abort();
                    }
                    catch (Exception) { }
                    finally { ResponseThread = null; }
                }

                //如果需要应答
                if (NeedResponse)
                {
                    try
                    {
                        //如果应答队列中还有数据，则报警
                        if ((RequestClient as RedisClient).Exists(RspQueueKey) == 1
                            && (RequestClient as RedisClient).Type(RspQueueKey) == "list"
                            && (RequestClient as RedisClient).LLen(RspQueueKey) > 0
                            )
                            DirectoryService.RedisLogHelper.LogWarnMsg("应答队列{0}中存在未处理的应答数据，将被清空.", RspQueueKey);
                    }
                    catch (Exception)
                    {
                        //如果断网，或者RedisServer停止，此处忽略这个错误
                    }
                }
            }
        }

        /// <summary>
        /// 获取Server端的应答数据
        /// </summary>
        [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions]
        void FetchResponseProcess()
        {
            //一直占用一个连接
            using (var conn = new RedisClient(Host.Host, Host.Port, Host.Password, Host.SSL))
            {
                conn.ConnectTimeout = Timeouts.WaitConnection;
                conn.ReceiveTimeout = Timeouts.SocketReceiveTimeout;
                conn.SendTimeout = Timeouts.SocketSendTimeout;

                #region 测试网络连通性

                //尝试一下是否能够Ping通
                try
                {
                    //会连接网络
                    conn.Db = Host.DB;

                    if (!conn.Ping())
                        throw new Exception(string.Format("与队列服务器{0}的连线Ping失败", this.Host));
                }
                catch (Exception)
                {
                    StopEvent.WaitOne(Timeouts.WaitConnection);
                    throw;
                }

                //既然已经连接上了，尝试帮助把发送的连接对象也重连一下
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

                //Ping通了
                this.IsException = false;

                #endregion

                //错误恢复了
                this.IsException = false;

                //一直循环，直至接收到退出事件
                while (true)
                {
                    if (DirectoryService.DefaultInstance == null)
                        break;

                    //每隔一定的时间，需要修改一下有效期
                    RefreshQueue(conn);

                    #region 获取应答数据

                    //获取应答数据
                    byte[] response = null;

                    //每次查询应答数据的超时时间（不能太长，否则响应退出事件慢）
                    int timeout = Math.Min(conn.ReceiveTimeout, 5000);

                    byte[][] datas = null;

                    //每个客户端都有一个自己的应答队列（如果Redis服务器系统时间修改，可能导致永远Block）
                    datas = conn.BRPop(RspQueueKey, (int)(timeout / 1000));

                    //错误恢复了
                    this.IsException = false;

                    //超时
                    if (datas == null || datas.Length != 2)
                    {
                        if (StopEvent.WaitOne(0))
                            break;

                        continue;
                    }

                    response = datas[1];

                    #endregion

                    #region 解析应答数据

                    //解析应答数据
                    RedisPackage pkg = new RedisPackage();
                    pkg.FromBytes(response);

                    //检查客户端编号
                    if (this.RspQueueKey != pkg.ResponseKey)
                        throw new Exception("应答数据异常");

                    ResponseId = Math.Max(ResponseId, pkg.RequestId);

                    //检查应答时间
                    var rspTime = new DateTime(pkg.OccurTime);
                    //如果应答请求处理超过一定时间，报警处理
                    var elapsedMS = (DirectoryService.Now - rspTime).TotalMilliseconds;
                    if (elapsedMS > Timeouts.Queue)
                        DirectoryService.RedisLogHelper.LogPerformaceMsg("应答数据在队列中已经等待{0}毫秒.", elapsedMS);

                    try
                    {
                        Stopwatch sw = Stopwatch.StartNew();

                        //处理应答数据
                        OnResponse(pkg.Service, pkg.RequestId, pkg.Tag, pkg.Data);

                        if (sw.ElapsedMilliseconds > Timeouts.Response && DirectoryService.LogHelper != null)
                            DirectoryService.LogHelper.LogPerformaceMsg("应答处理时间过长, 耗时: {0} 毫秒", sw.ElapsedMilliseconds);
                    }
                    catch (ThreadAbortException) { throw; }
                    catch (Exception err)
                    {
                        if (DirectoryService.LogHelper != null)
                            DirectoryService.LogHelper.LogErrMsg(err, "处理应答数据失败");
                    }

                    #endregion
                }
            }
        }
    }
}
