using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace iRailTracker.Model
{
    [XmlRoot("ArrayOfObjStationData", Namespace = "http://api.irishrail.ie/realtime/")]
    public class TrainService
    {
        [XmlElement("objStationData")]
        public List<StationData> ObjStationData { get; set; }
    }

    public class StationData
    {
        [XmlElement("Servertime")]
        public string Servertime { get; set; }

        [XmlElement("Traincode")]
        public string Traincode { get; set; }

        [XmlElement("Stationfullname")]
        public string Stationfullname { get; set; }

        [XmlElement("Stationcode")]
        public string Stationcode { get; set; }

        [XmlElement("Querytime")]
        public string Querytime { get; set; }

        [XmlElement("Traindate")]
        public string Traindate { get; set; }

        [XmlElement("Origin")]
        public string Origin { get; set; }

        [XmlElement("Destination")]
        public string Destination { get; set; }

        [XmlElement("Origintime")]
        public string Origintime { get; set; }

        [XmlElement("Destinationtime")]
        public string Destinationtime { get; set; }

        [XmlElement("Status")]
        public string Status { get; set; }

        [XmlElement("Lastlocation")]
        public string Lastlocation { get; set; }

        [XmlElement("Duein")]
        public int Duein { get; set; }

        [XmlElement("Late")]
        public int Late { get; set; }

        [XmlElement("Exparrival")]
        public string Exparrival { get; set; }

        [XmlElement("Expdepart")]
        public string Expdepart { get; set; }

        [XmlElement("Scharrival")]
        public string Scharrival { get; set; }

        [XmlElement("Schdepart")]
        public string Schdepart { get; set; }

        [XmlElement("Direction")]
        public string Direction { get; set; }

        [XmlElement("Traintype")]
        public string Traintype { get; set; }

        [XmlElement("Locationtype")]
        public string Locationtype { get; set; }
    }
}
