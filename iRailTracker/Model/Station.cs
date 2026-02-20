using System.Collections.Generic;
using System.Xml.Serialization;

namespace iRailTracker.Model
{
    [XmlRoot("ArrayOfObjStation", Namespace = "http://api.irishrail.ie/realtime/")]
    public class StationCollection
    {
        [XmlElement("objStation")]
        public List<Station> Stations { get; set; } = new List<Station>();
    }

    public class Station
    {
        [XmlElement("StationDesc")]
        public string StationDesc { get; set; } = string.Empty;

        [XmlElement("StationAlias")]
        public string StationAlias { get; set; } = string.Empty;

        [XmlElement("StationLatitude")]
        public double StationLatitude { get; set; }

        [XmlElement("StationLongitude")]
        public double StationLongitude { get; set; }

        [XmlElement("StationCode")]
        public string StationCode { get; set; } = string.Empty;

        [XmlElement("StationId")]
        public int StationId { get; set; }
    }
}
