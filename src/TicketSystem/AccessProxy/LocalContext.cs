using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccessProxy
{
    class LocalContext
    {
        internal static ConcurrentDictionary<string, ITicketCallbackService> Clients = new ConcurrentDictionary<string, ITicketCallbackService>();
    }
}
