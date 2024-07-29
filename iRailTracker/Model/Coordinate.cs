namespace iRailTracker.Model
{
    public class Coordinate
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        // Constructor
        public Coordinate(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }
    }
}
