using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iRailTracker.Model
{
    public class NearbyStation
    {
        public string name { get; set; }
        public string operational_status { get; set; }

        public class NearbyStations
        {
            public List<NearbyStation> places { get; set; }
        }
    }
}
