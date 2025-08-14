namespace DirectionsApi.Models
{
    public class RoutePoint
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double DistanceFromStart { get; set; }
        public int SequenceNumber { get; set; }
        public string Location => $"{Latitude},{Longitude}";
    }
}
