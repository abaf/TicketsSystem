using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TicketSystem.GlobalContext;
using TicketSystem.LogHelper;

namespace TicketSystem.Agent
{
    class SessionInfo
    {
		internal static uint RecvBufferLength = 4096;
        internal readonly byte[] RecvBuffer = new byte[RecvBufferLength];
        internal uint RecvLength = 0;
        internal readonly MemoryStream RecvDataStream = new MemoryStream();

        internal DateTime LastSentTime;
        internal bool Closed = false;

        internal TcpClient Client = null;
        internal SslStream SslStream = null;

        internal uint SessionID = 0;
        internal TicketSession TicketSession;
        internal DateTime CreateTime;

        internal Stopwatch LastValidTime;
        private Socket Socket;

        internal SessionInfo(SslStream stream, TcpClient client)
        {
            this.SslStream = stream;
            this.Client = client;
            this.RecvLength = 0;
            this.Socket = client.Client;
            this.SessionID = (uint)this.Socket.GetHashCode();
            this.CreateTime = this.LastSentTime = DateTime.Now;

            this.TicketSession = new TicketSession();
            this.TicketSession.SessionID = this.SessionID;
            this.TicketSession.ClientIP = (client.Client.RemoteEndPoint as IPEndPoint).Address.ToString();
            this.TicketSession.ClientPort = (client.Client.LocalEndPoint as IPEndPoint).Port;
        }

        /// <summary>
        /// Is session valid
        /// </summary>
        /// <returns></returns>
        internal bool IsValid()
        {
            if (this.TicketSession == null)
                return false;

            if (this.Client == null || this.SslStream == null || !this.Client.Connected)
                return false;

            return true;
        }

        internal void ManualClose()
        {
            try
            {
                if (this.SslStream != null)
                    this.SslStream.Close();

                if (this.Client != null)
                    this.Client.Close();
            }
            finally
            {
                this.SslStream = null;
                this.Client = null;
            }
        }

        internal bool IsManualClosed()
        {
            return this.Client == null || this.SslStream == null;
        }

        internal void AddSendData(byte[] data)
        {
            if (data == null || Closed || !this.IsValid()) return;

            lock (this)
            {
                Task t = new Task(() => SendData(data));
                t.Start();
            }
        }

        void SendData(byte[] data)
        {
            try
            {
                uint len = (uint)data.Length;
                SslStream.Write(data, 0, (int)len);
                SslStream.Flush();
            }
            catch (Exception err)
            {
                if (!this.IsManualClosed())
                {
                    if (err is IOException)
                        ThreadContext.LogHelper.LogWarnMsg(LogType.LogFileOnly, "Send data to client error. {0}", this.TicketSession);
                    else
                        ThreadContext.LogHelper.LogErrMsg(err, "Send data to client error. {0}", this.TicketSession);
                }

                this.ManualClose();
            }
        }
    }
}
