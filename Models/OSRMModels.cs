using System.Text.Json.Serialization;

namespace DirectionsApi.Models
{
    public class OSRMResponse
    {
        [JsonPropertyName("routes")]
        public OSRMRoute[]? Routes { get; set; }

        [JsonPropertyName("code")]
        public string? Code { get; set; }
    }

    public class OSRMRoute
    {
        [JsonPropertyName("geometry")]
        public OSRMGeometry? Geometry { get; set; }

        [JsonPropertyName("distance")]
        public double Distance { get; set; }

        [JsonPropertyName("duration")]
        public double Duration { get; set; }
    }

    public class OSRMGeometry
    {
        [JsonPropertyName("coordinates")]
        public double[][]? Coordinates { get; set; }
    }
}
