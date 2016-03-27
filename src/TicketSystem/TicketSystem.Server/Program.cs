using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketSystem.Utils;
using TicketSystem.GlobalContext;
using TicketSystem.Core;
using TicketSystem.Retail;
using System.Diagnostics;
using System.IO;

namespace TicketSystem.Server
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
                InitalizeRailwayRoute();
                StartRedisServer();
                InitBussinessRequest();
                ServerReadyAndListenTheTerminateSignal();
            }
            catch (Exception ex)
            {
                ThreadContext.LogHelper.LogErrMsg(ex, "Exception caught in initializing ticket server");
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
                ThreadContext.LogHelper.LogWarnMsg("Ticket server quit successfully, ExitCode = " + exitCode.ToString());
                if (Environment.UserInteractive || Environment.OSVersion.Platform == PlatformID.Unix)
                    Environment.Exit(exitCode);

            }
            catch (Exception)
            { }
        }

        static void InitBussinessRequest()
        {
            ThreadContext.LogHelper.LogInfoMsg("Starting the ticket retail thread...");
            coreBussiness = new BussinessRequest(ThreadContext.GlobalConfig.MaxRedisClients);
            coreBussiness.Initialize();
            coreBussiness.StartWorking();
            ThreadContext.LogHelper.LogInfoMsg("Ticket retail thread is ready.");
        }

        static void StartRedisServer()
        {
            ThreadContext.LogHelper.LogInfoMsg("Starting Redis Server...");
            string name = "redis-server.exe";
            string commandLine = string.Format("--port {0}", ThreadContext.GlobalConfig.RedisConfig.Port);

            var path = System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
            var fileName = string.Format("{0}{1}", path, name);
            if (!File.Exists(fileName))
            {
                ThreadContext.LogHelper.LogWarnMsg("Could not find {0} in current directory, please start it mannually with port number {1}", name,
                                                    ThreadContext.GlobalConfig.RedisConfig.Port);
                return;
            }
            ProcessStartInfo pi = new ProcessStartInfo();
            pi.FileName = fileName;
            pi.Arguments = commandLine;
            Process.Start(pi);
            ThreadContext.LogHelper.LogInfoMsg("Redis Server Started, Port:{0}.", ThreadContext.GlobalConfig.RedisConfig.Port);
        }

        static void ServerReadyAndListenTheTerminateSignal()
        {
            ThreadContext.LogHelper.LogInfoMsg("###########################################");
            ThreadContext.LogHelper.LogInfoMsg(string.Format("     Ticket server started.          "));
            ThreadContext.LogHelper.LogInfoMsg("###########################################");

            //capture Ctrl + C and terminate the process
            ConsoleCancelEventHandler ctrlc = null;
            ctrlc = (sender, e) =>
            {
                Console.Title = "Ticket server is exiting...";
                ThreadContext.LogHelper.LogWarnMsg("Captured Ctrl + C, Ticket server will exited immediatelly！");

                Uninitialize();
            };
            Console.CancelKeyPress += ctrlc;

            while (true)
            {
                bool bQuit = false;

                ConsoleKeyInfo? cki = null;

                cki = Console.ReadKey(true);

                //调试使用Control+D进行停止Application
                bQuit = (cki.Value.Modifiers & ConsoleModifiers.Control) != 0 && cki.Value.Key == ConsoleKey.D;

                if (bQuit)
                {
                    Console.Title = "Ticket server is exiting...";
                    Console.CancelKeyPress -= ctrlc;
                    ThreadContext.LogHelper.LogWarnMsg("Captured Ctrl + C, Ticket server will exited immediatelly！");

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

            Console.Title = "Ticket Server - Press Ctrl + C to quit";
        }

        static void InitConfig()
        {
            ThreadContext.LogHelper.LogInfoMsg("Initializing config...");
            var redisHost = System.Configuration.ConfigurationSettings.AppSettings["RedisHost"].ToString();
            var redisPort = System.Configuration.ConfigurationSettings.AppSettings["RedisPort"].ToString();
            var maxClients = System.Configuration.ConfigurationSettings.AppSettings["MaxClients"].ToString();
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

        static void InitalizeRailwayRoute()
        {
            ThreadContext.LogHelper.LogInfoMsg("Initializing route and seats...");
            var route = new Route("G1001",
                               new string[]
                               { "Beijing","Shijiazhuang","Zhenzhou","Wuhan","Changsha","Guangzhou"},
                               800);
            ThreadContext.LogHelper.LogInfoMsg("Route: G1001, Seats:800, Stations : Beijing,Shijiazhuang,Zhenzhou,Wuhan,Changsha,Guangzhou");
            ThreadContext.Route = route;
            ThreadContext.LogHelper.LogInfoMsg("Initialize route and seats finished");
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
