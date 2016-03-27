using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketSystem.Redis;
using TicketSystem.GlobalContext;
using TicketSystem.Message;
using TicketSystem.Utils;
using System.Diagnostics;

namespace TicketSystem.Retail
{
    /// <summary>
    /// ticket bussiness
    /// </summary>
    public class BussinessRequest
    {
        private int RedisClientNum;
        private RedisHost Host;
        private List<ClientProxy> Clients;
        public BussinessRequest(int redisClientNum = 3)
        {
            RedisClientNum = Math.Min(10, Math.Max(1, redisClientNum));

            Clients = new List<ClientProxy>(RedisClientNum);
            var hostString = string.Format("{0}:{1}", ThreadContext.GlobalConfig.RedisConfig.Host,
                ThreadContext.GlobalConfig.RedisConfig.Port);
            Host = new RedisHost(hostString);
        }

        public void Initialize()
        {
            for (int i = 0; i < RedisClientNum; i++)
            {
                ClientProxy client = new ClientProxy(Host, ThreadContext.RequestQueueName, OnResponse);
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
                var requestMessage = BinarySerialize.Deserialize<BuyTicketRequest>(package.Data);
                ThreadContext.LogHelper.LogInfoMsg("Start process ticket request, {0}", requestMessage.ToString());
                var responseMessage = new BuyTicketResponse
                {
                    RequestId = requestMessage.RequestId,
                    Route = requestMessage.Route,
                    StartStation = requestMessage.StartStation,
                    EndStation = requestMessage.EndStation,
                    OccurTime = DateTime.Now
                };
                var retailer = new TicketRetail();
                var soldTicket = retailer.BuyTicket(requestMessage);
                responseMessage.Tickets = soldTicket.TicketsSold;
                ThreadContext.LogHelper.LogInfoMsg("Process finished, request, {0}", requestMessage.ToString());

                Task sendResponseTask = new Task(() => SendResponseToRedis(responseMessage, package.SessionData));
                sendResponseTask.Start();
            }
            catch (Exception ex)
            {
                ThreadContext.LogHelper.LogErrMsg(ex, "Error process requestId:{0}", package.RequestId);
            }
        }

        private void SendResponseToRedis(BuyTicketResponse response, byte[] sessionData)
        {
            try
            {
                ThreadContext.LogHelper.LogInfoMsg("Start send ticket response, {0}", response.ToString());
                ClientProxy client = GetClientProxy();
                var packageData = BinarySerialize.Serialize<BuyTicketResponse>(response);
                client.SendRequest(ThreadContext.ResponseQueueName, response.RequestId, sessionData, packageData);
                ThreadContext.LogHelper.LogInfoMsg("send ticket response finished, response, {0}", response.ToString());
            }
            catch (Exception ex)
            {
                ThreadContext.LogHelper.LogErrMsg(ex, "Error send response:{0}", response.ToString());
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

        public void StartWorking()
        {
            Parallel.ForEach<ClientProxy>(Clients,
                                          (client) =>
                                                    {
                                                        client.StartProcess();
                                                    });
        }

        public void StopWorking()
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
