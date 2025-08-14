using System.ComponentModel;

namespace DirectionsApi.Models
{
    public enum RouteType
    {
        [Description("cycling-regular")]
        Cycling = 0,
        
        [Description("foot-walking")]
        Walking = 1,
        
        [Description("driving-car")]
        Driving = 2
    }
}
