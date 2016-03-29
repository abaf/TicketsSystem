using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketSystem.GlobalContext;
using TicketSystem.Message;
using TicketSystem.Redis;
using TicketSystem.Utils;

namespace TicketClient
{
    class BussinessRequest
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

        internal void SendRequest(BuyTicketRequest request)
        {
            Task requestTask = new Task(() => SendRequestImpl(request));
            requestTask.Start();
        }

        private void SendRequestImpl(BuyTicketRequest request)
        {
            try
            {
                var client = GetClientProxy();
                ThreadContext.LogHelper.LogInfoMsg("Send request to redis, detail:[{0}]", request.ToString());
                //Adjust the request's occur time
                request.OccurTime = DateTime.Now;
                var requestData = BinarySerialize.Serialize<BuyTicketRequest>(request);
                var session = new TicketSession { SessionID = System.Guid.NewGuid().GetHashCode(), RequestId = request.RequestId };
                var sessionData = BinarySerialize.Serialize<TicketSession>(session);
                ClientContext.Requests[(int)request.RequestId] = request;
                client.SendRequest(ThreadContext.RequestQueueName, request.RequestId, sessionData, requestData);
            }
            catch (Exception ex)
            {
                ThreadContext.LogHelper.LogErrMsg(ex, "error sending request to redis, detail:{0}", request.ToString());
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
                ThreadContext.LogHelper.LogInfoMsg("Get the response from redis,requestId:{0}", package.RequestId);
                var response = BinarySerialize.Deserialize<BuyTicketResponse>(package.Data);
                BuyTicketRequest request;
                if (ClientContext.Requests.TryRemove((int)response.RequestId, out request))
                {
                    lock (this)
                    {
                        var time = (response.OccurTime - request.OccurTime).TotalMilliseconds;
                        var tickets = response.Tickets;

                        TicketStat.TotalTickets += tickets;
                        TicketStat.TotalTimesInMilliseconds += time;
                        if (tickets > 0)
                            ThreadContext.LogHelper.LogInfoMsg("Buy {0} tickets in {1}ms, detail:{2}", tickets, time, response.ToString());
                        else
                            ThreadContext.LogHelper.LogWarnMsg("Spend {0}ms but get no tickets available, detail:{1}", time, response.ToString());

                        ThreadContext.LogHelper.LogInfoMsg("Buy {0} tickets in total, and time spend {1}s", TicketStat.TotalTickets, TicketStat.TotalTimesInMilliseconds / 1000);
                    }
                }
                ThreadContext.LogHelper.LogInfoMsg("Buy tickets finished,detail:[{0}]", response.ToString());
            }
            catch (Exception ex)
            {
                ThreadContext.LogHelper.LogErrMsg(ex, "Error get the response,requestId:{0}", package.RequestId);
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
