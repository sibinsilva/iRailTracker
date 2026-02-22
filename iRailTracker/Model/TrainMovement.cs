using System.Xml.Serialization;

namespace iRailTracker.Model;

[XmlRoot("ArrayOfObjTrainMovements", Namespace = "http://api.irishrail.ie/realtime/")]
public class TrainMovementsResponse
{
    [XmlElement("objTrainMovements")]
    public List<TrainMovement> Movements { get; set; } = [];
}

public class TrainMovement
{
    [XmlElement("TrainCode")]
    public string TrainCode { get; set; } = string.Empty;

    [XmlElement("LocationCode")]
    public string LocationCode { get; set; } = string.Empty;

    [XmlElement("LocationFullName")]
    public string LocationFullName { get; set; } = string.Empty;

    [XmlElement("LocationOrder")]
    public int LocationOrder { get; set; }

    [XmlElement("LocationType")]
    public string LocationType { get; set; } = string.Empty;

    [XmlElement("StopType")]
    public string StopType { get; set; } = string.Empty;
}
