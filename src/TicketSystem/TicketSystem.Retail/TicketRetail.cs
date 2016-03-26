using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketSystem.ThreadManager;
using TicketSystem.Core;
namespace TicketSystem.Retail
{
    public class TicketRetail
    {
        public static object synctRoot = new object();

        /// <summary>
        /// The core logic for buying tickets
        /// </summary>
        /// <param name="startStation"></param>
        /// <param name="endStation"></param>
        /// <param name="tickets"></param>
        /// <returns></returns>
        public SoldTicket BuyTicket(string startStation, string endStation, int tickets)
        {
            lock (synctRoot)
            {
                SoldTicket soldTicket = new SoldTicket
                {
                    Route = ThreadContext.Route.Name,
                    StartStation = startStation,
                    EndStation = endStation
                };

                var startStations = ThreadContext.Route.Stations.Where(p => p.Name == startStation)
                                                .ToList();
                var endStations = ThreadContext.Route.Stations.Where(p => p.Name == endStation)
                                                              .ToList();
                var startIndex = startStations.Count == 1 ? startStations.FirstOrDefault().Index :
                                                          startStations.LastOrDefault().Index;
                var endIndex = endStations.FirstOrDefault().Index;
                int length = endIndex - startIndex + 1;
                int ticketsAvailable = tickets;
                var preSeats = ThreadContext.Route.SeatsVacancy.Skip(startIndex)
                                                               .Take(length);
                var minSeatsAvailable = preSeats.Min();
                if (minSeatsAvailable <= 0)
                {
                    return soldTicket;
                }
                else if (minSeatsAvailable < tickets)
                {
                    ticketsAvailable = minSeatsAvailable;
                }
                soldTicket.TicketsSold = ticketsAvailable;
                for (int i = startIndex; i <= endIndex; i++)
                {
                    ThreadContext.Route.SeatsVacancy[i] = ThreadContext.Route.SeatsVacancy[i] - ticketsAvailable;
                }
                return soldTicket;
            }
        }
    }
}
