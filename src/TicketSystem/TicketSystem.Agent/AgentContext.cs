using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketSystem.Agent
{
    internal class AgentContext
    {
        /// <summary>
		/// The session collections
		/// </summary>
		internal static readonly ConcurrentDictionary<UInt32, SessionInfo> SessionInfos = new ConcurrentDictionary<uint, SessionInfo>();

        internal static BussinessRequest BussinessRequest;
    }
}
