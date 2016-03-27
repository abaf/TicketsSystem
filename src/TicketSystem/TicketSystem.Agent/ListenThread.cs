using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TicketSystem.GlobalContext;

namespace TicketSystem.Agent
{
    public class ListenThread
    {
        /// <summary>
		/// Listening thread
		/// </summary>
		Thread Thread = null;

        /// <summary>
        /// Listening the thread stop event
        /// </summary>
        ManualResetEvent StopEvent = new ManualResetEvent(false);

        /// <summary>
        /// Listening port number
        /// </summary>
        int ListenPort = 0;

        public ListenThread(int listenPort)
        {
            this.ListenPort = listenPort;

            Thread = new Thread((ThreadStart)ListenProcess);
        }

        [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions]
        void ListenProcess()
        {
            int timeout = 5000;

            try
            {
                while (!StopEvent.WaitOne(0))
                {
                    try
                    {
                        TcpListener listener = new TcpListener(new IPEndPoint(IPAddress.Any, this.ListenPort));

                        try
                        {
                            listener.Start(50);

                            ThreadContext.LogHelper.LogInfoMsg("Start to listen port：{0}", this.ListenPort);

                            listener.BeginAcceptTcpClient(AsyncAcceptClientCallback, listener);

                            while (!StopEvent.WaitOne(timeout))
                            {
                             
                            }
                        }
                        finally
                        {
                            listener.Stop();
                        }
                    }
                    catch (ThreadAbortException)
                    {
                        return;
                    }
                    catch (Exception err)
                    {
                        ThreadContext.LogHelper.LogErrMsg(err, "Listen thread get an exception, stopped!.");
                        StopEvent.WaitOne(timeout);
                    }
                }
            }
            catch (ThreadAbortException)
            {
            }
            catch (Exception err)
            {
                ThreadContext.LogHelper.LogErrMsg(err, "Listen thread get an exception, stopped.");
            }
        }
        [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions]
        void AsyncAcceptClientCallback(IAsyncResult ar)
        {
            TcpListener listener = ar.AsyncState as TcpListener;
            if (listener == null || StopEvent.WaitOne(0)) return;

            string sourceIp = null;

            try
            {
                var client = listener.EndAcceptTcpClient(ar);

                sourceIp = (client.Client.RemoteEndPoint as IPEndPoint).Address.ToString();

                client.ReceiveTimeout = 5000;
                client.SendTimeout = 5000;
             
                SslStream sslStream = new SslStream(client.GetStream(), false);
                var session = new SessionInfo(sslStream, client);
                AgentContext.SessionInfos[session.SessionID] = session;
                sslStream.BeginRead(session.RecvBuffer, 0, 4096, (AsyncCallback)ReceiveProcess.AsyncReceiveCallback, session);
            }
            catch (Exception err)
            {
                ThreadContext.LogHelper.LogErrMsg(err, "Accept thread exception!, SourceIp: {0}", sourceIp);
            }

            if (StopEvent.WaitOne(0)) return;

            listener.BeginAcceptTcpClient(AsyncAcceptClientCallback, listener);
        }

        public void Start()
        {
            if (Thread == null || Thread.IsAlive) return;

            StopEvent.Reset();
            Thread.Start();
        }

        public void Stop()
        {
            if (Thread == null || !Thread.IsAlive) return;

            try
            {
                StopEvent.Set();

                Thread.Abort();
            }
            catch (Exception) { }

            try
            {
                AgentContext.SessionInfos.Clear();
            }
            catch (Exception) { }
        }
    }
}
