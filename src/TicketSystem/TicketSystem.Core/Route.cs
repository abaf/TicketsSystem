using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketSystem.Core
{
    public class Route
    {
        private string[] routeStations;
        private int seats;
        private int[] seatsVacancy;

        public string Name { get; set; }

        /// <summary>
        /// The available seats for every station
        /// </summary>
        public int[] SeatsVacancy
        {
            get { return seatsVacancy; }
        }

        /// <summary>
        /// Stations of this route
        /// </summary>
        public List<Station> Stations { get; private set; }

        public int Seats { get { return seats; } }

        /// <summary>
        /// The constructor of route
        /// </summary>
        /// <param name="name">The name of the route</param>
        /// <param name="routeStations">Stations of the route in a string collection</param>
        /// <param name="seats">Total seats of the route</param>
        public Route(string name, string[] routeStations, int seats)
        {
            Name = name;
            this.routeStations = routeStations;
            this.seats = seats;

            Initialize();
        }

        private void Initialize()
        {
            InitializeSeats();

            InitializeRouteConfig();

            InitializeStations();
        }

        private void InitializeStations()
        {
            Stations = new List<Station>();
            var stationIndex = 0;
            for (int i = 0; i < routeStations.Length; i++)
            {
                var routeStation = routeStations[i];
                if (i == 0)
                {
                    //start staion
                    Station station = new Station
                    {
                        Name = routeStation,
                        IsArrival = false,
                        IsDeparture = true,
                        Index = stationIndex
                    };
                    Stations.Add(station);
                }
                else if (i == routeStation.Length - 1)
                {
                    //end station
                    Station station = new Station
                    {
                        Name = routeStation,
                        IsArrival = true,
                        IsDeparture = false,
                        Index = stationIndex
                    };
                    Stations.Add(station);
                }
                else
                {
                    //middle stations
                    Station arrivalStation  = new Station
                    {
                        Name = routeStation,
                        IsArrival = true,
                        IsDeparture = false,
                        Index = stationIndex
                    };
                    Stations.Add(arrivalStation);

                    stationIndex++;
                    Station departureStation = new Station
                    {
                        Name = routeStation,
                        IsArrival = true,
                        IsDeparture = false,
                        Index = stationIndex
                    };
                    Stations.Add(departureStation);
                }
                stationIndex++;
            }
        }

        private void InitializeRouteConfig()
        {
            if (seatsVacancy == null)
                return;
            //Init config
            RouteConfig.Instance.StartStationIndex = 0;
            RouteConfig.Instance.EndStationIndex = seatsVacancy.Length - 1;
        }

        private void InitializeSeats()
        {
            //besides the start station and end station, other stations will both have a depart and arrival station in memory
            int numberOfStations = (routeStations.Length - 2) * 2 + 2;
            seatsVacancy = new int[numberOfStations];
            for (int i = 0; i < numberOfStations; i++)
            {
                seatsVacancy[i] = seats;
            }
        }
    }
}
