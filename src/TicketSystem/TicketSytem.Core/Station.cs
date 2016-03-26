using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketSytem.Core
{
    public class Station
    {
        /// <summary>
        /// The name of the station
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The index of the station in the route, starts from zero
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Indicates this station is departure station
        /// </summary>
        public bool IsDeparture { get; set; }

        /// <summary>
        /// Indicates this station is arrival station
        /// </summary>
        public bool IsArrival { get; set; }

        /// <summary>
        ///  Formatted the station name with start and end
        /// </summary>
        public string ActualName
        {
            get
            {
                if (Index == RouteConfig.Instance.StartStationIndex ||
                    Index == RouteConfig.Instance.EndStationIndex)
                {
                    return Name;
                }
                else
                {
                    if (IsDeparture)
                        return string.Format("{0}_Start", Name);
                    if(IsArrival)
                        return string.Format("{0}_End", Name);
                }
                return Name;
            }
        }
    }
}
