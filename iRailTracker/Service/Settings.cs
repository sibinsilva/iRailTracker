using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iRailTracker.Service
{
    public class Settings
    {
        private string getAllStationsUrl = string.Empty;
        private string getNearbyStationUrl = string.Empty;
        private string getServiceByStationCodeURL = string.Empty;
        private string getTrainMovementsUrl = string.Empty;

        public string GetAllStationsUrl { get => getAllStationsUrl; set => getAllStationsUrl = value; }
        public string GetNearbyStationUrl { get => getNearbyStationUrl; set => getNearbyStationUrl = value; }
        public string GetServiceByStationCodeURL { get => getServiceByStationCodeURL; set => getServiceByStationCodeURL = value; }
        public string GetTrainMovementsUrl { get => getTrainMovementsUrl; set => getTrainMovementsUrl = value; }
    }
}
