using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketSystem.Message
{
    [Serializable]
    public class TicketRequest<T> where T : Message
    {
        public int MyProperty { get; set; }
    }
}
