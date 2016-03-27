using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketSystem.GlobalContext
{
    public class GlobalConfig
    {
        public int MaxRedisClients { get; set; }

        public RedisConfig RedisConfig { get; set; }
    }
}
