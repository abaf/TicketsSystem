using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketSystem.Message
{
    [Serializable]
    public class TicketMessage : Message
    {
        public string StartStation { get; set; }
        public string EndStation { get; set; }
        public int Tickets { get; set; }
    }
}
