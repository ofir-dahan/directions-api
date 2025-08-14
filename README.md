# Directions API

A .NET C# RESTful API with Swagger documentation that calculates route points between two geographic coordinates with configurable spacing. Supports real road-based routing for Walking, Cycling, and Driving.

## 🌟 Features

- **🗺️ Real Road Routing**: Uses OpenRouteService and OSRM for actual street directions
- **🚴‍♂️ Multiple Route Types**: Walking, Cycling, and Driving routes
- **⚙️ Configurable Spacing**: Customizable distance between route points (1-1000 meters)
- **📍 Location Field**: Each point includes "latitude,longitude" format
- **📚 Swagger Documentation**: Interactive API documentation
- **🔧 Input Validation**: Validates all coordinate and parameter ranges
- **☁️ Azure Ready**: Includes GitHub Actions for automatic deployment
- **Multiple endpoints**: Support for both POST with JSON body and GET with query parameters
- **CORS enabled**: Allows cross-origin requests for web applications

## API Endpoints

### POST /api/directions/route

Calculate directions using a JSON request body.

**Request Body:**
```json
{
  "startLatitude": 32.0853,
  "startLongitude": 34.7818,
  "endLatitude": 32.0861,
  "endLongitude": 34.7831
}
```

**Response:**
```json
{
  "totalDistance": 150.5,
  "pointCount": 4,
  "routePoints": [
    {
      "latitude": 32.0853,
      "longitude": 34.7818,
      "distanceFromStart": 0,
      "sequenceNumber": 0
    },
    {
      "latitude": 32.08556,
      "longitude": 34.78232,
      "distanceFromStart": 50.17,
      "sequenceNumber": 1
    }
  ],
  "startLatitude": 32.0853,
  "startLongitude": 34.7818,
  "endLatitude": 32.0861,
  "endLongitude": 34.7831
}
```

### GET /api/directions/route

Calculate directions using query parameters.

**Query Parameters:**
- `startLat`: Start latitude (-90 to 90)
- `startLng`: Start longitude (-180 to 180)
- `endLat`: End latitude (-90 to 90)
- `endLng`: End longitude (-180 to 180)

**Example:**
```
GET /api/directions/route?startLat=32.0853&startLng=34.7818&endLat=32.0861&endLng=34.7831
```

## Getting Started

### Prerequisites

- .NET 9.0 SDK or later
- Visual Studio Code (recommended) or Visual Studio

### Running the Application

1. **Clone or navigate to the project directory**
   ```bash
   cd /path/to/directions
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Build the project**
   ```bash
   dotnet build DirectionsApi.csproj
   ```

4. **Run the application**
   ```bash
   dotnet run --project DirectionsApi.csproj
   ```

5. **Access the API**
   - Application: http://localhost:5244
   - Swagger UI: http://localhost:5244 (automatically redirects to Swagger)

### Using VS Code Tasks

If you're using VS Code, you can use the pre-configured task:

1. Open the project in VS Code
2. Press `Ctrl+Shift+P` (or `Cmd+Shift+P` on Mac)
3. Type "Tasks: Run Task" and select it
4. Choose "Launch DirectionsApi"

## Project Structure

```
/
├── Controllers/
│   └── DirectionsController.cs    # API controller with endpoints
├── Models/
│   ├── DirectionsRequest.cs       # Request model for API
│   ├── DirectionsResponse.cs      # Response model for API
│   └── RoutePoint.cs             # Model for individual route points
├── Services/
│   ├── IDirectionsService.cs     # Service interface
│   └── DirectionsService.cs      # Business logic for route calculation
├── Properties/
│   └── launchSettings.json       # Development settings
├── DirectionsApi.csproj          # Project file
├── Program.cs                    # Application entry point and configuration
└── README.md                     # This file
```

## Algorithm Details

The API uses a simplified approach for calculating route points:

1. **Distance Calculation**: Uses the Haversine formula to calculate the great-circle distance between two points on Earth
2. **Point Interpolation**: Uses linear interpolation between coordinates (suitable for short distances)
3. **Spacing Logic**: Calculates the number of intervals needed to achieve approximately 50-meter spacing

**Note**: For longer distances (>10km), a more sophisticated great-circle interpolation would provide more accurate results.

## Example Usage

### Using curl

```bash
# POST request
curl -X POST "http://localhost:5244/api/directions/route" \
  -H "Content-Type: application/json" \
  -d '{
    "startLatitude": 32.0853,
    "startLongitude": 34.7818,
    "endLatitude": 32.0861,
    "endLongitude": 34.7831
  }'

# GET request
curl "http://localhost:5244/api/directions/route?startLat=32.0853&startLng=34.7818&endLat=32.0861&endLng=34.7831"
```

### Using JavaScript fetch

```javascript
// POST request
const response = await fetch('http://localhost:5244/api/directions/route', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
  },
  body: JSON.stringify({
    startLatitude: 32.0853,
    startLongitude: 34.7818,
    endLatitude: 32.0861,
    endLongitude: 34.7831
  })
});

const directions = await response.json();
console.log(directions);
```

## Dependencies

- **ASP.NET Core**: Web framework
- **Swashbuckle.AspNetCore**: Swagger/OpenAPI documentation
- **Microsoft.AspNetCore.OpenApi**: OpenAPI support

## Development

### Adding Features

1. Models are in the `Models/` directory
2. Business logic should be added to `Services/DirectionsService.cs`
3. API endpoints are defined in `Controllers/DirectionsController.cs`

### Configuration

The application can be configured through:
- `appsettings.json`: General application settings (includes OpenRouteService API key)
- `appsettings.Development.json`: Development-specific settings
- `Properties/launchSettings.json`: Launch profiles

## 🚀 Deployment

### Deploy to Azure

This project includes GitHub Actions for automatic deployment to Azure App Service:

1. **Create Azure App Service**:
   ```bash
   # Create resource group
   az group create --name directionsapi-rg --location "West Europe"
   
   # Create App Service plan
   az appservice plan create --name directionsapi-plan --resource-group directionsapi-rg --sku B1 --is-linux
   
   # Create Web App
   az webapp create --resource-group directionsapi-rg --plan directionsapi-plan --name directionsapi --runtime "DOTNETCORE:9.0"
   ```

2. **Get Publish Profile**:
   ```bash
   az webapp deployment list-publishing-profiles --name directionsapi --resource-group directionsapi-rg --xml
   ```

3. **Add GitHub Secret**:
   - Go to your GitHub repository → Settings → Secrets and variables → Actions
   - Add new secret: `AZURE_WEBAPP_PUBLISH_PROFILE`
   - Paste the XML content from step 2

4. **Push to GitHub**:
   ```bash
   git add .
   git commit -m "Initial commit"
   git push origin main
   ```

The GitHub Action will automatically deploy your app to Azure on every push to the main branch.

### Environment Variables

For production deployment, set these environment variables in Azure:

- `RoutingService__OpenRouteServiceApiKey`: Your OpenRouteService API key
- `RoutingService__BaseUrl`: https://api.openrouteservice.org

## 📋 API Documentation

Once deployed, your API documentation will be available at:
- **Local**: http://localhost:5244
- **Azure**: https://your-app-name.azurewebsites.net

## License

This project is created as a sample application for demonstration purposes.
