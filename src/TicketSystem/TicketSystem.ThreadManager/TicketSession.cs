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

        public override string ToString()
        {
            return string.Format("SessionID:{0}, ClientIP:{1},ClientPort:{2},RequestID:{3}", SessionID, ClientIP, ClientPort, RequestId);
        }
    }
}
