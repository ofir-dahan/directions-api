namespace DirectionsApi.Models
{
    public class DirectionsResponse
    {
        public double TotalDistance { get; set; }
        public int PointCount { get; set; }
        public List<RoutePoint> RoutePoints { get; set; } = new List<RoutePoint>();
        public double StartLatitude { get; set; }
        public double StartLongitude { get; set; }
        public double EndLatitude { get; set; }
        public double EndLongitude { get; set; }
    }
}
