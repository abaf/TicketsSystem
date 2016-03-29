using Demo.Contracts;
using Demo.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Text;
using System.Threading.Tasks;

namespace Demo.Hosting
{
    class Program
    {
        static void Main(string[] args)
        {
            using (ServiceHost host = new ServiceHost(typeof(CalculatorService)))
            {
                host.AddServiceEndpoint(typeof(ICalculator), new NetTcpBinding(), "net.tcp://127.0.0.1:8989/calculatorservice");
                if (host.Description.Behaviors.Find<ServiceMetadataBehavior>() == null)
                {
                    ServiceMetadataBehavior behavior = new ServiceMetadataBehavior();
                    host.Description.Behaviors.Add(behavior);
                }

                host.Opened += (s, e) =>
                {
                    Console.WriteLine("CalculaorService Started, Press any key to exit!");
                };

                host.Open();
                Console.Read();
            }
        }
    }
}
