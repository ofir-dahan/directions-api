namespace DirectionsApi.Models
{
    public class RoutingServiceOptions
    {
        public const string SectionName = "RoutingService";
        
        public string OpenRouteServiceApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://api.openrouteservice.org";
    }
}
