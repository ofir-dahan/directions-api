using System.ComponentModel.DataAnnotations;

namespace DirectionsApi.Models
{
    public class DirectionsRequest
    {
        [Required]
        [Range(-90, 90, ErrorMessage = "Start latitude must be between -90 and 90")]
        public double StartLatitude { get; set; }

        [Required]
        [Range(-180, 180, ErrorMessage = "Start longitude must be between -180 and 180")]
        public double StartLongitude { get; set; }

        [Required]
        [Range(-90, 90, ErrorMessage = "End latitude must be between -90 and 90")]
        public double EndLatitude { get; set; }

        [Required]
        [Range(-180, 180, ErrorMessage = "End longitude must be between -180 and 180")]
        public double EndLongitude { get; set; }

        [Range(1, 1000, ErrorMessage = "Spacing must be between 1 and 1000 meters")]
        public double SpacingMeters { get; set; } = 50.0;

        public RouteType RouteType { get; set; } = RouteType.Cycling;
    }
}
