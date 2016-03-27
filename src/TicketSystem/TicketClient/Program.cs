using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketSystem.GlobalContext;
using TicketSystem.Message;
using TicketSystem.Utils;

namespace TicketClient
{
    class Program
    {
        static BussinessRequest coreBussiness;
        static void Main(string[] args)
        {
            Initialize();
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
        }

        public static void Initialize()
        {
            try
            {
                InitConsole();
                InitConfig();
                InitBussinessRequest();
                InitFinished();
                StartBuyTickets();
                ServerReadyAndListenTheTerminateSignal();
            }
            catch (Exception ex)
            {
                ThreadContext.LogHelper.LogErrMsg(ex, "Exception caught in initializing ticket client");
                System.Threading.Thread.Sleep(3000);
                Uninitialize();
            }
        }

        static void Uninitialize(int exitCode = 0)
        {
            try
            {
                ThreadContext.LogHelper.LogWarnMsg("Exiting...");

                Console.Title = "Ticket server is exiting...";

                if (coreBussiness != null)
                    coreBussiness.StopWorking();

                System.Threading.Thread.Sleep(2000);
                ThreadContext.LogHelper.UnInitialize();

                ThreadContext.LogHelper.LogInfoMsg("Ticket server quit successfully, ExitCode = " + exitCode.ToString());
                if (Environment.UserInteractive || Environment.OSVersion.Platform == PlatformID.Unix)
                    Environment.Exit(exitCode);

            }
            catch (Exception)
            { }
        }

        static void InitBussinessRequest()
        {
            ThreadContext.LogHelper.LogInfoMsg("Starting the ticket client thread...");
            coreBussiness = new BussinessRequest(ThreadContext.GlobalConfig.MaxRedisClients);
            coreBussiness.Initialize();
            coreBussiness.StartWorking();
            ThreadContext.LogHelper.LogInfoMsg("Ticket client thread is ready.");
        }


        static void InitFinished()
        {
            ThreadContext.LogHelper.LogInfoMsg("###########################################");
            ThreadContext.LogHelper.LogInfoMsg(string.Format("     Ticket client started.          "));
            ThreadContext.LogHelper.LogInfoMsg("###########################################");
        }

        static void ServerReadyAndListenTheTerminateSignal()
        {

            //capture Ctrl + C and terminate the process
            ConsoleCancelEventHandler ctrlc = null;
            ctrlc = (sender, e) =>
            {
                Console.Title = "Ticket client is exiting...";
                ThreadContext.LogHelper.LogWarnMsg("Captured Ctrl + C, Ticket client will exited immediatelly！");

                Uninitialize();
            };
            Console.CancelKeyPress += ctrlc;

            while (true)
            {
                bool bQuit = false;

                ConsoleKeyInfo? cki = null;

                cki = Console.ReadKey(true);

                bQuit = (cki.Value.Modifiers & ConsoleModifiers.Control) != 0 && cki.Value.Key == ConsoleKey.D;

                if (bQuit)
                {
                    Console.Title = "Ticket client is exiting...";
                    Console.CancelKeyPress -= ctrlc;
                    ThreadContext.LogHelper.LogWarnMsg("Captured Ctrl + C, Ticket client will exited immediatelly！");

                    Uninitialize();
                    break;
                }
            }
        }
        static void InitConsole()
        {
            try
            {
                //set the console window
                Console.SetWindowSize(120, 30);
                Console.SetBufferSize(120, 1000);
                Console.BackgroundColor = ConsoleColor.Black;
                ConsoleHelper.DisableCloseMenu();
            }
            catch (Exception)
            {
            }

            Console.Title = "Ticket client - Press Ctrl + C to quit";
        }

        static void InitConfig()
        {
            ThreadContext.LogHelper.LogInfoMsg("Initializing config...");
            var redisHost = System.Configuration.ConfigurationSettings.AppSettings["RedisHost"].ToString();
            var redisPort = System.Configuration.ConfigurationSettings.AppSettings["RedisPort"].ToString();
            var maxClients = System.Configuration.ConfigurationSettings.AppSettings["MaxRedisClients"].ToString();
            int port = -1;
            if (!Int32.TryParse(redisPort, out port))
            {
                ThreadContext.LogHelper.LogErrMsg("Invalid redis port number, please check the app.config");
                throw new Exception("Invalid redis port number");
            }
            int maxClientsNum = -1;
            if (!Int32.TryParse(maxClients, out maxClientsNum))
            {
                ThreadContext.LogHelper.LogErrMsg("Invalid maxClients, please check the app.config");
                throw new Exception("Invalid maxClients");
            }
            ThreadContext.GlobalConfig.RedisConfig = new RedisConfig { Host = redisHost, Port = port };
            ThreadContext.GlobalConfig.MaxRedisClients = maxClientsNum;
            ThreadContext.LogHelper.LogInfoMsg("Initialize config finished.");
        }

        static void StartBuyTickets()
        {
            Task buyTask = new Task(() =>
            {
                for (int i = 0; i < 1000; i++)
                {
                    var request = CreateRequest();
                    coreBussiness.SendRequest(request);
                }
            });
            buyTask.Start();
        }

        static BuyTicketRequest CreateRequest()
        {
            BuyTicketRequest request = new BuyTicketRequest
            {
                RequestId = GenerateRequestId(),
                StartStation = TicketService.GetStartStation(),
                Route = "G1001",
                OccurTime = DateTime.Now,
                Tickets = TicketService.GetTickets()
            };
            request.EndStation = TicketService.GetEndStation(request.StartStation);
            return request;
        }

        static int GenerateRequestId()
        {
            var requestId = System.Guid.NewGuid().GetHashCode();
            if (requestId > 0)
                return requestId;
            return 0 - requestId;
        }

        [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions]
        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ThreadContext.LogHelper.LogErrMsg(e.ExceptionObject as Exception, "CurrentDomain_UnhandledException");

            if (e.IsTerminating)
            {
                ThreadContext.LogHelper.LogErrMsg("Ticket server crashed！");

                System.Threading.ManualResetEvent evtExit = new System.Threading.ManualResetEvent(false);
                System.Threading.Thread exitThread = new System.Threading.Thread((System.Threading.ThreadStart)delegate
                {
                    try
                    {
                        Uninitialize(-1);

                        evtExit.Set();
                    }
                    catch (Exception) { }
                });
                exitThread.Start();

                evtExit.WaitOne(10000);

                Environment.Exit(-1);
            }
        }
    }
}
