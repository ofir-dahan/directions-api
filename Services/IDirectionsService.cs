using DirectionsApi.Models;

namespace DirectionsApi.Services
{
    public interface IDirectionsService
    {
        Task<DirectionsResponse> GetDirectionsAsync(DirectionsRequest request);
        DirectionsResponse GetDirections(DirectionsRequest request); // Keep for backward compatibility
    }
}
