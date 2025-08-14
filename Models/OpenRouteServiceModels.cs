namespace DirectionsApi.Models
{
    public class OpenRouteServiceResponse
    {
        public List<OpenRouteServiceRoute> Routes { get; set; } = new List<OpenRouteServiceRoute>();
    }

    public class OpenRouteServiceRoute
    {
        public OpenRouteServiceGeometry Geometry { get; set; } = new OpenRouteServiceGeometry();
        public OpenRouteServiceSummary Summary { get; set; } = new OpenRouteServiceSummary();
    }

    public class OpenRouteServiceGeometry
    {
        public List<List<double>> Coordinates { get; set; } = new List<List<double>>();
    }

    public class OpenRouteServiceSummary
    {
        public double Distance { get; set; }
        public double Duration { get; set; }
    }
}
