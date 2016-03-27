using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketSystem.Message;

namespace TicketClient
{
    class ClientContext
    {
        internal static readonly ConcurrentDictionary<Int32, BuyTicketRequest> Requests = new ConcurrentDictionary<Int32, BuyTicketRequest>();
    }
}
