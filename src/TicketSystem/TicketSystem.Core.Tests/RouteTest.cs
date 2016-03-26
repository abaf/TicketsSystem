using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TicketSystem.Core;
namespace TicketSystem.Core.Tests
{
    [TestClass]
    public class RouteTest
    {
        [TestMethod]
        public void IsRouteSeatsEqual()
        {
            Route route = CreateRoute();
            Assert.AreEqual(route.Seats, 800);
        }

        [TestMethod]
        public void IsRouteConfigIntialized()
        {
            var route = CreateRoute();
            Assert.IsNotNull(RouteConfig.Instance);
            Assert.AreEqual(RouteConfig.Instance.StartStationIndex, 0);
            Assert.AreEqual(RouteConfig.Instance.EndStationIndex, 9);
        }



        private Route CreateRoute()
        {
            Route route = new Route("G1001",
                                    new string[]
                                    { "Beijing","Shijiazhuang","Zhenzhou","Wuhan","Changsha","Guangzhou"},
                                    800);
            return route;
        }
    }
}
