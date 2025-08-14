using Microsoft.AspNetCore.Mvc;
using DirectionsApi.Models;
using DirectionsApi.Services;
using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.Annotations;

namespace DirectionsApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DirectionsController : ControllerBase
    {
        private readonly IDirectionsService _directionsService;
        private readonly ILogger<DirectionsController> _logger;

        public DirectionsController(IDirectionsService directionsService, ILogger<DirectionsController> logger)
        {
            _directionsService = directionsService;
            _logger = logger;
        }

        /// <summary>
        /// Get route directions between two points with configurable spacing between intermediate points
        /// Uses OpenRouteService for real road routing when API key is configured, otherwise falls back to OSRM or straight-line calculation
        /// </summary>
        /// <param name="startLat">Start latitude (-90 to 90)</param>
        /// <param name="startLng">Start longitude (-180 to 180)</param>
        /// <param name="endLat">End latitude (-90 to 90)</param>
        /// <param name="endLng">End longitude (-180 to 180)</param>
        /// <param name="spacing">Distance in meters between route points (default: 50, range: 1-1000)</param>
        /// <param name="routeType">Type of route: Cycling (default), Walking, or Driving</param>
        /// <returns>A list of route points with specified spacing following appropriate roads/paths</returns>
        [HttpGet("route")]
        [SwaggerOperation(
            Summary = "Get route directions with real road routing",
            Description = "Calculate route points between two coordinates following actual roads, bike paths, or walking routes"
        )]
        [SwaggerResponse(200, "Route calculated successfully", typeof(DirectionsResponse))]
        [SwaggerResponse(400, "Invalid input parameters", typeof(ValidationProblemDetails))]
        [ProducesResponseType(typeof(DirectionsResponse), 200)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        public async Task<ActionResult<DirectionsResponse>> GetDirections(
            [FromQuery, Required, Range(-90, 90, ErrorMessage = "Start latitude must be between -90 and 90")]
            [SwaggerParameter("Start latitude")] 
            double startLat = 30.774746, 
            
            [FromQuery, Required, Range(-180, 180, ErrorMessage = "Start longitude must be between -180 and 180")]
            [SwaggerParameter("Start longitude")] 
            double startLng = 35.276701,
            
            [FromQuery, Required, Range(-90, 90, ErrorMessage = "End latitude must be between -90 and 90")]
            [SwaggerParameter("End latitude")] 
            double endLat = 30.762593,
            [FromQuery, Required, Range(-180, 180, ErrorMessage = "End longitude must be between -180 and 180")]
            [SwaggerParameter("End longitude")] 
            double endLng = 35.278288,
            
            [FromQuery, Range(1, 1000, ErrorMessage = "Spacing must be between 1 and 1000 meters")]
            [SwaggerParameter("Distance in meters between route points")] 
            double spacing = 50.0,
            
            [FromQuery]
            [SwaggerParameter("Type of route")] 
            RouteType routeType = RouteType.Cycling)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var request = new DirectionsRequest
                {
                    StartLatitude = startLat,
                    StartLongitude = startLng,
                    EndLatitude = endLat,
                    EndLongitude = endLng,
                    SpacingMeters = spacing,
                    RouteType = routeType
                };

                _logger.LogInformation("Calculating {RouteType} route from ({StartLat}, {StartLng}) to ({EndLat}, {EndLng}) with {Spacing}m spacing", 
                    routeType, startLat, startLng, endLat, endLng, spacing);

                var result = await _directionsService.GetDirectionsAsync(request);

                _logger.LogInformation("Route calculated successfully with {PointCount} points over {TotalDistance:F2} meters", 
                    result.PointCount, result.TotalDistance);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating directions");
                return StatusCode(500, "An error occurred while calculating directions");
            }
        }
    }
}
