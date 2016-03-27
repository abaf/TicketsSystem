using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Text;
using System.Threading.Tasks;
using TicketSystem.GlobalContext;
using TicketSystem.LogHelper;

namespace TicketSystem.Agent
{
    class ReceiveProcess
    {
		[System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions]
        internal static void AsyncReceiveCallback(IAsyncResult ar)
        {
            SessionInfo session = null;

            try
            {
                session = ar.AsyncState as SessionInfo;
                if (session == null) return;

                SslStream sslStream = session.SslStream;
                if (sslStream == null)
                {
                    ThreadContext.LogHelper.LogWarnMsg(LogType.LogFileOnly, "Disconnected. {0}", session.TicketSession);
                    session.ManualClose();
                    return;
                }

                int recvLength = sslStream.EndRead(ar);

                if (recvLength <= 0)
                {
                    ThreadContext.LogHelper.LogWarnMsg(LogType.LogFileOnly, "Disconnected. {0}", session.TicketSession);
                    session.ManualClose();
                    return;
                }

                if (!session.IsValid())
                {
                    session.ManualClose();
                    return;
                }

                if (recvLength > 0)
                {
                    session.RecvLength += (uint)recvLength;

                    session.RecvDataStream.Write(session.RecvBuffer, 0, recvLength);
                    sslStream.BeginRead(session.RecvBuffer, (int)session.RecvLength, 4096, (AsyncCallback)AsyncReceiveCallback, session);
                }
                else
                {
                    AgentContext.BussinessRequest.OnClientMessageReceived(session);
                    //start receive next process
                    session.RecvLength = 0;
                    session.RecvDataStream.Seek(0, SeekOrigin.Begin);
                    sslStream.BeginRead(session.RecvBuffer, 0, 4096, (AsyncCallback)AsyncReceiveCallback, session);
                }
                
            }
            catch (Exception err)
            {
                if (!session.IsManualClosed())
                {
                    if (err is IOException)
                        ThreadContext.LogHelper.LogWarnMsg(LogType.LogFileOnly, "Receive error. {0}", session.TicketSession);
                    else
                        ThreadContext.LogHelper.LogErrMsg(err, "Receive error. {0}", session.TicketSession);
                }

                session.ManualClose();
            }
        }
    }
}
