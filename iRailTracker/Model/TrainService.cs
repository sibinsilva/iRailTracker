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
        public List<StationData> ObjStationData { get; set; } = new List<StationData>();
    }

    public class StationData
    {
        [XmlElement("Servertime")]
        public string Servertime { get; set; } = string.Empty;

        [XmlElement("Traincode")]
        public string Traincode { get; set; } = string.Empty;

        [XmlElement("Stationfullname")]
        public string Stationfullname { get; set; } = string.Empty;

        [XmlElement("Stationcode")]
        public string Stationcode { get; set; } = string.Empty;

        [XmlElement("Querytime")]
        public string Querytime { get; set; } = string.Empty;

        [XmlElement("Traindate")]
        public string Traindate { get; set; } = string.Empty;

        [XmlElement("Origin")]
        public string Origin { get; set; } = string.Empty;

        [XmlElement("Destination")]
        public string Destination { get; set; } = string.Empty;

        [XmlElement("Origintime")]
        public string Origintime { get; set; } = string.Empty;

        [XmlElement("Destinationtime")]
        public string Destinationtime { get; set; } = string.Empty;

        [XmlElement("Status")]
        public string Status { get; set; } = string.Empty;

        [XmlElement("Lastlocation")]
        public string Lastlocation { get; set; } = string.Empty;

        [XmlElement("Duein")]
        public int Duein { get; set; }

        [XmlElement("Late")]
        public int Late { get; set; }

        [XmlElement("Exparrival")]
        public string Exparrival { get; set; } = string.Empty;

        [XmlElement("Expdepart")]
        public string Expdepart { get; set; } = string.Empty;

        [XmlElement("Scharrival")]
        public string Scharrival { get; set; } = string.Empty;

        [XmlElement("Schdepart")]
        public string Schdepart { get; set; } = string.Empty;

        [XmlElement("Direction")]
        public string Direction { get; set; } = string.Empty;

        [XmlElement("Traintype")]
        public string Traintype { get; set; } = string.Empty;

        [XmlElement("Locationtype")]
        public string Locationtype { get; set; } = string.Empty;
    }
}
