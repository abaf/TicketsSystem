using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketClient
{
    public class TicketService
    {
        static int MaxTickets = 5;
        static string[] stations = new string[] { "Beijing", "Shijiazhuang", "Zhenzhou", "Wuhan", "Changsha", "Guangzhou" };

        public static string GetStartStation()
        {
            Random rnd = new Random(System.Guid.NewGuid().GetHashCode());
            var startIndex = rnd.Next(4);
            return stations[startIndex];
        }

        public static string GetEndStation(string startStation)
        {
            int startIndex = -1;
            for (int i = 0; i < stations.Length; i++)
            {
                startIndex = i;
                if (stations[i] == startStation)
                    break;
            }

            Random rnd = new Random(System.Guid.NewGuid().GetHashCode());
            var endIndex = rnd.Next(5);
            while (endIndex == 0 || endIndex <= startIndex)
                endIndex = rnd.Next(5);
            return stations[endIndex];
        }

        public static int GetTickets()
        {
            Random rnd = new Random(System.Guid.NewGuid().GetHashCode());
            var tickets = rnd.Next(MaxTickets);
            while (tickets == 0)
                tickets = rnd.Next(MaxTickets);
            return tickets;
        }
    }
}
