using DirectionsApi.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.ComponentModel;

namespace DirectionsApi.Services
{
    public class DirectionsService : IDirectionsService
    {
        private readonly HttpClient _httpClient;
        private readonly RoutingServiceOptions _options;
        private readonly ILogger<DirectionsService> _logger;

        public DirectionsService(HttpClient httpClient, IOptions<RoutingServiceOptions> options, ILogger<DirectionsService> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<DirectionsResponse> GetDirectionsAsync(DirectionsRequest request)
        {
            try
            {
                // Try OpenRouteService first if API key is configured
                if (!string.IsNullOrEmpty(_options.OpenRouteServiceApiKey))
                {
                    var routeData = await GetRouteFromOpenRouteService(request);
                    if (routeData?.Routes?.FirstOrDefault()?.Geometry?.Coordinates != null && routeData.Routes.Any())
                    {
                        return ProcessRouteData(routeData.Routes.First(), request);
                    }
                }

                // Fallback to OSRM (free, no API key required)
                _logger.LogInformation("Using OSRM for road-based routing (no API key required)");
                var osrmRouteData = await GetRouteFromOSRM(request);
                
                if (osrmRouteData != null)
                {
                    return ProcessOSRMRouteData(osrmRouteData, request);
                }

                // Final fallback to straight-line calculation
                _logger.LogWarning("All routing services failed, using straight-line calculation");
                return GetStraightLineDirections(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting directions from routing services, falling back to straight-line calculation");
                return GetStraightLineDirections(request);
            }
        }

        public DirectionsResponse GetDirections(DirectionsRequest request)
        {
            // For backward compatibility, provide synchronous version
            return GetDirectionsAsync(request).GetAwaiter().GetResult();
        }

        private async Task<OpenRouteServiceResponse?> GetRouteFromOpenRouteService(DirectionsRequest request)
        {
            var profile = GetRouteProfile(request.RouteType);
            
            var requestBody = new
            {
                coordinates = new double[][]
                {
                    new double[] { request.StartLongitude, request.StartLatitude },
                    new double[] { request.EndLongitude, request.EndLatitude }
                },
                profile = profile,
                format = "json",
                instructions = false,
                geometry = true
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", _options.OpenRouteServiceApiKey);

            var url = $"{_options.BaseUrl}/v2/directions/{profile}";
            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("OpenRouteService API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<OpenRouteServiceResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        private async Task<OSRMResponse?> GetRouteFromOSRM(DirectionsRequest request)
        {
            try
            {
                // Map our route types to OSRM profiles
                var profile = request.RouteType switch
                {
                    RouteType.Driving => "driving",
                    RouteType.Walking => "walking",
                    RouteType.Cycling => "cycling",
                    _ => "cycling"
                };

                // OSRM uses lon,lat format
                var url = $"http://router.project-osrm.org/route/v1/{profile}/{request.StartLongitude},{request.StartLatitude};{request.EndLongitude},{request.EndLatitude}?overview=full&geometries=geojson";

                _httpClient.DefaultRequestHeaders.Clear();
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("OSRM API error: {StatusCode}", response.StatusCode);
                    return null;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<OSRMResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling OSRM API");
                return null;
            }
        }

        private DirectionsResponse ProcessOSRMRouteData(OSRMResponse osrmResponse, DirectionsRequest request)
        {
            var route = osrmResponse.Routes?.FirstOrDefault();
            if (route?.Geometry?.Coordinates == null)
            {
                throw new InvalidOperationException("Invalid OSRM route data");
            }

            var coordinates = route.Geometry.Coordinates;
            var totalDistance = route.Distance; // Distance in meters

            var routePoints = new List<RoutePoint>();
            var currentDistance = 0.0;
            var sequenceNumber = 0;

            // Add start point
            if (coordinates.Length > 0)
            {
                var startCoord = coordinates[0];
                routePoints.Add(new RoutePoint
                {
                    Latitude = startCoord[1], // OSRM uses [lon, lat]
                    Longitude = startCoord[0],
                    DistanceFromStart = 0,
                    SequenceNumber = sequenceNumber++
                });
            }

            // Process the route to create points with specified spacing
            for (int i = 1; i < coordinates.Length; i++)
            {
                var prevCoord = coordinates[i - 1];
                var currentCoord = coordinates[i];
                
                var segmentDistance = CalculateDistance(prevCoord[1], prevCoord[0], currentCoord[1], currentCoord[0]);
                
                // If this segment would put us past the next spacing interval, interpolate
                while (currentDistance + segmentDistance >= sequenceNumber * request.SpacingMeters && sequenceNumber * request.SpacingMeters <= totalDistance)
                {
                    var targetDistance = sequenceNumber * request.SpacingMeters;
                    var remainingInSegment = targetDistance - currentDistance;
                    var fraction = remainingInSegment / segmentDistance;
                    
                    var interpolatedPoint = InterpolatePoint(prevCoord[1], prevCoord[0], currentCoord[1], currentCoord[0], fraction);
                    
                    routePoints.Add(new RoutePoint
                    {
                        Latitude = interpolatedPoint.Lat,
                        Longitude = interpolatedPoint.Lng,
                        DistanceFromStart = targetDistance,
                        SequenceNumber = sequenceNumber++
                    });
                }
                
                currentDistance += segmentDistance;
            }

            // Add the final point if it's not already added
            var lastCoord = coordinates.Last();
            if (routePoints.Count == 0 || routePoints.Last().DistanceFromStart < totalDistance - 1) // Allow 1m tolerance
            {
                routePoints.Add(new RoutePoint
                {
                    Latitude = lastCoord[1],
                    Longitude = lastCoord[0],
                    DistanceFromStart = totalDistance,
                    SequenceNumber = routePoints.Count
                });
            }

            return new DirectionsResponse
            {
                TotalDistance = totalDistance,
                PointCount = routePoints.Count,
                RoutePoints = routePoints,
                StartLatitude = request.StartLatitude,
                StartLongitude = request.StartLongitude,
                EndLatitude = request.EndLatitude,
                EndLongitude = request.EndLongitude
            };
        }

        private DirectionsResponse ProcessRouteData(OpenRouteServiceRoute route, DirectionsRequest request)
        {
            var coordinates = route.Geometry.Coordinates;
            var totalDistance = route.Summary.Distance; // Distance in meters

            var routePoints = new List<RoutePoint>();
            var currentDistance = 0.0;
            var sequenceNumber = 0;

            // Add the first point
            if (coordinates.Any())
            {
                var firstCoord = coordinates.First();
                routePoints.Add(new RoutePoint
                {
                    Latitude = firstCoord[1], // OpenRouteService returns [longitude, latitude]
                    Longitude = firstCoord[0],
                    DistanceFromStart = 0,
                    SequenceNumber = sequenceNumber++
                });
            }

            // Process the route to create points with approximately 50m spacing
            for (int i = 1; i < coordinates.Count; i++)
            {
                var prevCoord = coordinates[i - 1];
                var currentCoord = coordinates[i];
                
                var segmentDistance = CalculateDistance(prevCoord[1], prevCoord[0], currentCoord[1], currentCoord[0]);
                
                // If this segment would put us past the next spacing interval, interpolate
                while (currentDistance + segmentDistance >= sequenceNumber * request.SpacingMeters && sequenceNumber * request.SpacingMeters <= totalDistance)
                {
                    var targetDistance = sequenceNumber * request.SpacingMeters;
                    var remainingInSegment = targetDistance - currentDistance;
                    var fraction = remainingInSegment / segmentDistance;
                    
                    var interpolatedPoint = InterpolatePoint(prevCoord[1], prevCoord[0], currentCoord[1], currentCoord[0], fraction);
                    
                    routePoints.Add(new RoutePoint
                    {
                        Latitude = interpolatedPoint.Lat,
                        Longitude = interpolatedPoint.Lng,
                        DistanceFromStart = targetDistance,
                        SequenceNumber = sequenceNumber++
                    });
                }
                
                currentDistance += segmentDistance;
            }

            // Add the final point if it's not already added
            var lastCoord = coordinates.Last();
            if (routePoints.Last().DistanceFromStart < totalDistance - 1) // Allow 1m tolerance
            {
                routePoints.Add(new RoutePoint
                {
                    Latitude = lastCoord[1],
                    Longitude = lastCoord[0],
                    DistanceFromStart = totalDistance,
                    SequenceNumber = routePoints.Count
                });
            }

            return new DirectionsResponse
            {
                TotalDistance = totalDistance,
                PointCount = routePoints.Count,
                RoutePoints = routePoints,
                StartLatitude = request.StartLatitude,
                StartLongitude = request.StartLongitude,
                EndLatitude = request.EndLatitude,
                EndLongitude = request.EndLongitude
            };
        }

        private DirectionsResponse GetStraightLineDirections(DirectionsRequest request)
        {
            var startPoint = new { Lat = request.StartLatitude, Lng = request.StartLongitude };
            var endPoint = new { Lat = request.EndLatitude, Lng = request.EndLongitude };

            // Calculate total distance between start and end points
            var totalDistance = CalculateDistance(startPoint.Lat, startPoint.Lng, endPoint.Lat, endPoint.Lng);
            
            // Calculate number of points needed (including start and end)
            var numberOfIntervals = (int)Math.Ceiling(totalDistance / request.SpacingMeters);
            var actualInterval = totalDistance / numberOfIntervals;

            var routePoints = new List<RoutePoint>();

            // Add start point
            routePoints.Add(new RoutePoint
            {
                Latitude = startPoint.Lat,
                Longitude = startPoint.Lng,
                DistanceFromStart = 0,
                SequenceNumber = 0
            });

            // Calculate intermediate points
            for (int i = 1; i < numberOfIntervals; i++)
            {
                var fraction = (double)i / numberOfIntervals;
                var intermediatePoint = InterpolatePoint(startPoint.Lat, startPoint.Lng, endPoint.Lat, endPoint.Lng, fraction);
                
                routePoints.Add(new RoutePoint
                {
                    Latitude = intermediatePoint.Lat,
                    Longitude = intermediatePoint.Lng,
                    DistanceFromStart = i * actualInterval,
                    SequenceNumber = i
                });
            }

            // Add end point
            routePoints.Add(new RoutePoint
            {
                Latitude = endPoint.Lat,
                Longitude = endPoint.Lng,
                DistanceFromStart = totalDistance,
                SequenceNumber = numberOfIntervals
            });

            return new DirectionsResponse
            {
                TotalDistance = totalDistance,
                PointCount = routePoints.Count,
                RoutePoints = routePoints,
                StartLatitude = request.StartLatitude,
                StartLongitude = request.StartLongitude,
                EndLatitude = request.EndLatitude,
                EndLongitude = request.EndLongitude
            };
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            // Haversine formula to calculate distance between two points on Earth
            const double EarthRadiusKm = 6371.0;
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var distance = EarthRadiusKm * c * 1000; // Convert to meters

            return distance;
        }

        private (double Lat, double Lng) InterpolatePoint(double lat1, double lon1, double lat2, double lon2, double fraction)
        {
            // Linear interpolation between two geographic points
            var lat = lat1 + (lat2 - lat1) * fraction;
            var lon = lon1 + (lon2 - lon1) * fraction;

            return (lat, lon);
        }

        private double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }

        private string GetRouteProfile(RouteType routeType)
        {
            var fieldInfo = routeType.GetType().GetField(routeType.ToString());
            var attribute = fieldInfo?.GetCustomAttributes(typeof(DescriptionAttribute), false)
                                    .FirstOrDefault() as DescriptionAttribute;
            return attribute?.Description ?? "cycling-regular";
        }
    }
}
