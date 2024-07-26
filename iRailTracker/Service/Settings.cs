using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iRailTracker.Service
{
    public class Settings
    {
        private string getAllStationsUrl;
        private string getNearbyStationUrl;
        private string getServiceByStationCodeURL;

        public string GetAllStationsUrl { get => getAllStationsUrl; set => getAllStationsUrl = value; }
        public string GetNearbyStationUrl { get => getNearbyStationUrl; set => getNearbyStationUrl = value; }
        public string GetServiceByStationCodeURL { get => getServiceByStationCodeURL; set => getServiceByStationCodeURL = value; }
    }
}
