using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketSystem.Agent
{
    internal class AgentConfig
    {
        public string RedisHost { get; set; }
        public int RedisPort { get; set; }
        public int AgentPort { get; set; }
    }
}
