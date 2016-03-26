using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketSystem.Message
{
    [Serializable]
    public class BuyTicketRequest : Message
    {
        public string Route { get; set; }
        public string StartStation { get; set; }
        public string EndStation { get; set; }
        /// <summary>
        /// The tickets wanted
        /// </summary>
        public int Tickets { get; set; }
    }
}
