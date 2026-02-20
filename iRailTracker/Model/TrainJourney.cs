using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iRailTracker.Model
{
    public class TrainJourney
    {
        public string Origin { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public string CurrentStatus { get; set; } = string.Empty;
        public string DueIn { get; set; } = string.Empty;
        public string ExpectedArrival { get; set; } = string.Empty;
        public string Late { get; set; } = string.Empty;
    }
}
