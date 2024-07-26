using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iRailTracker.Model
{
    public class TrainJourney
    {
        public string Origin { get; set; }
        public string Destination { get; set; }
        public string CurrentStatus { get; set; }
        public string DueIn { get; set; }
        public string ExpectedArrival { get; set; }
        public string Late { get; set; }
    }
}
