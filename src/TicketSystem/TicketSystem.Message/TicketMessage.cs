using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketSystem.Message
{
    /// <summary>
    /// A request & response message wrapper
    /// </summary>
    /// <typeparam name="T">The type of the message, might be request message or response message</typeparam>
    [Serializable]
    public class TicketMessage<T> where T : Message
    {
        /// <summary>
        /// Unique, generate from GUID, and request& response shared same value
        /// </summary>
        public int RequestId { get; set; }

        public DateTime RequestTime { get; set; }

        public T Message { get; set; }
    }
}
