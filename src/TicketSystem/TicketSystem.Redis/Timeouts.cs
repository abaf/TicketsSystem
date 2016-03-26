using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketSystem.Redis
{
    /// <summary>
    /// All kinds of timeout
    /// </summary>
    public class Timeouts
    {
        /// <summary>
        /// The expire time of service
        /// </summary>
        public const Int32 ServiceTTL = 2 * 60 * 1000;

        public const Int32 SocketSendTimeout = 10 * 1000;

        public const Int32 SocketReceiveTimeout = 10 * 1000;

        public const Int32 WaitConnection = 5000;
    }
}
