using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketSystem.Core
{
    public class SoldTicket
    {
        public string Route { get; set; }
        public string StartStation { get; set; }
        public string EndStation { get; set; }

        public int TicketsSold { get; set; }
    }
}
