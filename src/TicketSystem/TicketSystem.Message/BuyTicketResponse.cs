using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketSystem.Message
{
    [Serializable]
    public class BuyTicketResponse: Message
    {
        public long RequestId { get; set; }
        public string Route { get; set; }
        public string StartStation { get; set; }
        public string EndStation { get; set; }
        public DateTime OccurTime { get; set; }
        /// <summary>
        /// Actual tickets bought
        /// </summary>
        public int Tickets { get; set; }

        public override string ToString()
        {
            return string.Format("BuyTicketRequest:{RequestId:{0},Route:{1},StartStation:{2},EndStation:{3},Tickects:{4},Time:{5}}",
                RequestId, Route, StartStation, EndStation, Tickets, OccurTime.ToString("YYYYMMDDhhmmss.fff"));
        }
    }
}
