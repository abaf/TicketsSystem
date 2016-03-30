using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace TicketSystem.Message
{
    [DataContract]
    [Serializable]
    public class BuyTicketResponse: Message
    {
        [DataMember]
        public long RequestId { get; set; }
        [DataMember]
        public string Route { get; set; }
        [DataMember]
        public string StartStation { get; set; }
        [DataMember]
        public string EndStation { get; set; }
        [DataMember]
        public DateTime OccurTime { get; set; }
        /// <summary>
        /// Actual tickets bought
        /// </summary>
        [DataMember]
        public int Tickets { get; set; }

        public override string ToString()
        {
            return string.Format("BuyTicketResponse:[RequestId:{0},Route:{1},StartStation:{2},EndStation:{3},Tickects:{4},Time:{5}]",
                RequestId, Route, StartStation, EndStation, Tickets, OccurTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
        }
    }
}
