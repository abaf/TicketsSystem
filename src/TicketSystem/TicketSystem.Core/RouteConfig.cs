using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketSystem.Core
{
    public class RouteConfig
    {
        private static RouteConfig _instance;

        public static RouteConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new RouteConfig();
                }
                return _instance;
            }
        }

        public int StartStationIndex { get; set; }

        public int EndStationIndex { get; set; }
    }
}
