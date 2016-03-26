using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketSystem.Message
{
    public class BuyTicketResponse: Message
    {
        public string Route { get; set; }
        public string StartStation { get; set; }
        public string EndStation { get; set; }
        /// <summary>
        /// Actual tickets bought
        /// </summary>
        public int Tickets { get; set; }
    }
}
