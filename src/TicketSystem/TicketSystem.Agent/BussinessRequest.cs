using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketSystem.Utils;
using TicketSystem.Message;
using TicketSystem.GlobalContext;
using TicketSystem.Redis;
using System.Diagnostics;

namespace TicketSystem.Agent
{
    /// <summary>
    /// 
    /// </summary>
    internal class BussinessRequest
    {
        private int RedisClientNum;
        private RedisHost Host;
        private List<ClientProxy> Clients;

        internal BussinessRequest(int redisClientNum = 3)
        {
            RedisClientNum = Math.Min(10, Math.Max(1, redisClientNum));

            Clients = new List<ClientProxy>(RedisClientNum);
            var hostString = string.Format("{0}:{1}", ThreadContext.GlobalConfig.RedisConfig.Host,
                ThreadContext.GlobalConfig.RedisConfig.Port);
            Host = new RedisHost(hostString);
        }

        internal void Initialize()
        {
            for (int i = 0; i < RedisClientNum; i++)
            {
                ClientProxy client = new ClientProxy(Host, ThreadContext.ResponseQueueName, OnResponse);
                Clients.Add(client);
            }
        }

        private void OnResponse(RedisPackage package)
        {
            var processRspTask = new Task(() => ProcessResponseImpl(package));
            processRspTask.Start();
        }

        private void ProcessResponseImpl(RedisPackage package)
        {
            try
            {
                ThreadContext.LogHelper.LogInfoMsg("send response to client,requestId:{0}", package.RequestId);
                var ticketSession = BinarySerialize.Deserialize<TicketSession>(package.SessionData);
                SessionInfo session;
                if (AgentContext.SessionInfos.TryGetValue((uint)ticketSession.SessionID, out session))
                {
                    session.AddSendData(package.Data); // set the response data to client
                }
                ThreadContext.LogHelper.LogInfoMsg("send response to client finished,requestId:{0}", package.RequestId);
            }
            catch (Exception ex)
            {
                ThreadContext.LogHelper.LogErrMsg(ex, "Error sending response to client,requestId:{0}", package.RequestId);
            }
        }

        internal void OnClientMessageReceived(SessionInfo session)
        {
            var processReqTask = new Task(() => ClientMessageReceivedImpl(session));
            processReqTask.Start();
        }

        private void ClientMessageReceivedImpl(SessionInfo session)
        {
            try
            {
                var data = session.RecvDataStream.ToArray();
                var requestMessage = BinarySerialize.Deserialize<BuyTicketRequest>(data);
                session.TicketSession.RequestId = requestMessage.RequestId;
                session.TicketSession.SessionID = session.SessionID;

                ThreadContext.LogHelper.LogInfoMsg("Received request:{0}", requestMessage.ToString());
                var sessionData = BinarySerialize.Serialize<TicketSession>(session.TicketSession);
                // send to redis
                ThreadContext.LogHelper.LogInfoMsg("send request to redis,request:{0}", requestMessage.ToString());
                var client = GetClientProxy();
                if (client != null)
                {
                    client.SendRequest(ThreadContext.RequestQueueName, requestMessage.RequestId, sessionData, data);
                }
                ThreadContext.LogHelper.LogInfoMsg("send request to redis finished,request:{0}", requestMessage.ToString());

            }
            catch (Exception ex)
            {
                ThreadContext.LogHelper.LogErrMsg(ex, "Error sending request to redis,sessionID:{0}", session.SessionID);
            }
        }

        private ClientProxy GetClientProxy(int timeout = 5000)
        {
            ClientProxy client = null;
            Stopwatch sw = Stopwatch.StartNew();
            while (client == null && timeout - sw.ElapsedMilliseconds > 0)
            {
                client = Clients.FirstOrDefault(x => x.IsBusy == false);
            }

            if (client == null)
            {
                ThreadContext.LogHelper.LogErrMsg("Can't get client proxy");
                throw new Exception("Can't get client proxy");
            }
            return client;
        }

        internal void StartWorking()
        {
            Parallel.ForEach<ClientProxy>(Clients,
                                          (client) =>
                                          {
                                              client.StartProcess();
                                          });
        }

        internal void StopWorking()
        {
            Parallel.ForEach<ClientProxy>(Clients,
                                          (client) =>
                                          {
                                              client.StopProcess();
                                              client.Dispose();
                                          });
        }
    }
}
