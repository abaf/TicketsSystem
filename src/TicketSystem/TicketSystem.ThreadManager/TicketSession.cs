using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketSystem.GlobalContext
{
    /// <summary>
    /// Session that indicates a ticket-buying behavior
    /// </summary>
    [Serializable]
    public class TicketSession
    {
        public long SessionID { get; set; }
        public string ClientIP { get; set; }
        public int ClientPort { get; set; }
        public long RequestId { get; set; }
    }
}
