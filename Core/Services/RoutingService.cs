using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Logging;
using static BlockchainNetworkAnalyzer.App;

namespace BlockchainNetworkAnalyzer.Core.Services
{
    /// <summary>
    /// Routing service for navigation and directions
    /// </summary>
    public class RoutingService
    {
        private readonly ILogger<RoutingService> _logger;
        private readonly HttpClient _httpClient;

        public RoutingService()
        {
            _logger = App.LoggerFactory.CreateLogger<RoutingService>();
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        public async Task<RouteInfo> GetRouteAsync(double startLat, double startLng, double endLat, double endLng, RouteProfile profile = RouteProfile.Driving)
        {
            var routeInfo = new RouteInfo
            {
                StartLatitude = startLat,
                StartLongitude = startLng,
                EndLatitude = endLat,
                EndLongitude = endLng,
                Profile = profile
            };

            try
            {
                // Try multiple routing services
                RouteInfo result = null;

                // Try Google Maps first
                result = await GetRouteFromGoogleMaps(startLat, startLng, endLat, endLng, profile);
                if (result != null && result.Success)
                {
                    routeInfo = result;
                    routeInfo.Source = "Google Maps";
                    return routeInfo;
                }

                // Try OpenRouteService
                result = await GetRouteFromOpenRouteService(startLat, startLng, endLat, endLng, profile);
                if (result != null && result.Success)
                {
                    routeInfo = result;
                    routeInfo.Source = "OpenRouteService";
                    return routeInfo;
                }

                // Try Mapbox
                result = await GetRouteFromMapbox(startLat, startLng, endLat, endLng, profile);
                if (result != null && result.Success)
                {
                    routeInfo = result;
                    routeInfo.Source = "Mapbox";
                    return routeInfo;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting route");
            }

            return routeInfo;
        }

        public async Task<RouteInfo> GetRouteToAddressAsync(double startLat, double startLng, string address)
        {
            // First geocode the address, then get route
            var geocodingService = new GeolocationService();
            var addressLocation = await geocodingService.GetAddressAsync(startLat, startLng); // Reverse geocode
            
            // For now, return basic route info
            // In production, would geocode the target address first
            return await GetRouteAsync(startLat, startLng, addressLocation.Latitude, addressLocation.Longitude);
        }

        private async Task<RouteInfo> GetRouteFromGoogleMaps(double startLat, double startLng, double endLat, double endLng, RouteProfile profile)
        {
            try
            {
                var apiKey = App.Configuration["Routing:GoogleMapsAPIKey"] ?? "";
                if (string.IsNullOrEmpty(apiKey))
                    return null;

                var mode = profile switch
                {
                    RouteProfile.Driving => "driving",
                    RouteProfile.Walking => "walking",
                    RouteProfile.Cycling => "bicycling",
                    RouteProfile.Transit => "transit",
                    _ => "driving"
                };

                var url = $"https://maps.googleapis.com/maps/api/directions/json?" +
                         $"origin={startLat},{startLng}&" +
                         $"destination={endLat},{endLng}&" +
                         $"mode={mode}&" +
                         $"key={apiKey}&" +
                         $"language=fa";

                var response = await _httpClient.GetStringAsync(url);
                var data = JObject.Parse(response);

                if (data["status"]?.ToString() == "OK" && data["routes"]?.First != null)
                {
                    var route = data["routes"][0];
                    var legs = route["legs"][0];

                    var routeInfo = new RouteInfo
                    {
                        Success = true,
                        Distance = legs["distance"]?["value"]?.ToObject<double>() ?? 0,
                        DistanceText = legs["distance"]?["text"]?.ToString(),
                        Duration = legs["duration"]?["value"]?.ToObject<double>() ?? 0,
                        DurationText = legs["duration"]?["text"]?.ToString(),
                        StartAddress = legs["start_address"]?.ToString(),
                        EndAddress = legs["end_address"]?.ToString()
                    };

                    // Extract polyline
                    var overviewPolyline = route["overview_polyline"]?["points"]?.ToString();
                    routeInfo.Polyline = overviewPolyline;

                    // Extract steps
                    routeInfo.Steps = new List<RouteStep>();
                    if (legs["steps"] != null)
                    {
                        foreach (var step in legs["steps"])
                        {
                            routeInfo.Steps.Add(new RouteStep
                            {
                                Distance = step["distance"]?["value"]?.ToObject<double>() ?? 0,
                                DistanceText = step["distance"]?["text"]?.ToString(),
                                Duration = step["duration"]?["value"]?.ToObject<double>() ?? 0,
                                DurationText = step["duration"]?["text"]?.ToString(),
                                Instruction = step["html_instructions"]?.ToString(),
                                StartLatitude = step["start_location"]?["lat"]?.ToObject<double>() ?? 0,
                                StartLongitude = step["start_location"]?["lng"]?.ToObject<double>() ?? 0,
                                EndLatitude = step["end_location"]?["lat"]?.ToObject<double>() ?? 0,
                                EndLongitude = step["end_location"]?["lng"]?.ToObject<double>() ?? 0
                            });
                        }
                    }

                    return routeInfo;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get route from Google Maps");
            }

            return null;
        }

        private async Task<RouteInfo> GetRouteFromOpenRouteService(double startLat, double startLng, double endLat, double endLng, RouteProfile profile)
        {
            try
            {
                var apiKey = App.Configuration["Routing:OpenRouteServiceAPIKey"] ?? "";
                if (string.IsNullOrEmpty(apiKey))
                    return null;

                var profileStr = profile switch
                {
                    RouteProfile.Driving => "driving-car",
                    RouteProfile.Walking => "foot-walking",
                    RouteProfile.Cycling => "cycling-regular",
                    _ => "driving-car"
                };

                var url = $"https://api.openrouteservice.org/v2/directions/{profileStr}?" +
                         $"api_key={apiKey}&" +
                         $"start={startLng},{startLat}&" +
                         $"end={endLng},{endLat}";

                var response = await _httpClient.GetStringAsync(url);
                var data = JObject.Parse(response);

                if (data["features"]?.First != null)
                {
                    var feature = data["features"][0];
                    var properties = feature["properties"];
                    var geometry = feature["geometry"];

                    return new RouteInfo
                    {
                        Success = true,
                        Distance = properties["segments"]?[0]?["distance"]?.ToObject<double>() ?? 0,
                        Duration = properties["segments"]?[0]?["duration"]?.ToObject<double>() ?? 0,
                        Polyline = geometry["coordinates"]?.ToString()
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get route from OpenRouteService");
            }

            return null;
        }

        private async Task<RouteInfo> GetRouteFromMapbox(double startLat, double startLng, double endLat, double endLng, RouteProfile profile)
        {
            try
            {
                var apiKey = App.Configuration["Routing:MapboxAPIKey"] ?? "";
                if (string.IsNullOrEmpty(apiKey))
                    return null;

                var profileStr = profile switch
                {
                    RouteProfile.Driving => "driving",
                    RouteProfile.Walking => "walking",
                    RouteProfile.Cycling => "cycling",
                    _ => "driving"
                };

                var url = $"https://api.mapbox.com/directions/v5/mapbox/{profileStr}/{startLng},{startLat};{endLng},{endLat}?" +
                         $"access_token={apiKey}&" +
                         $"geometries=geojson&" +
                         $"overview=full";

                var response = await _httpClient.GetStringAsync(url);
                var data = JObject.Parse(response);

                if (data["code"]?.ToString() == "Ok" && data["routes"]?.First != null)
                {
                    var route = data["routes"][0];

                    return new RouteInfo
                    {
                        Success = true,
                        Distance = route["distance"]?.ToObject<double>() ?? 0,
                        Duration = route["duration"]?.ToObject<double>() ?? 0,
                        Polyline = route["geometry"]?.ToString()
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get route from Mapbox");
            }

            return null;
        }
    }

    public enum RouteProfile
    {
        Driving,
        Walking,
        Cycling,
        Transit
    }

    public class RouteInfo
    {
        public bool Success { get; set; }
        public double StartLatitude { get; set; }
        public double StartLongitude { get; set; }
        public double EndLatitude { get; set; }
        public double EndLongitude { get; set; }
        public double Distance { get; set; } // in meters
        public string DistanceText { get; set; }
        public double Duration { get; set; } // in seconds
        public string DurationText { get; set; }
        public string StartAddress { get; set; }
        public string EndAddress { get; set; }
        public string Polyline { get; set; }
        public List<RouteStep> Steps { get; set; } = new List<RouteStep>();
        public RouteProfile Profile { get; set; }
        public string Source { get; set; }
    }

    public class RouteStep
    {
        public double Distance { get; set; }
        public string DistanceText { get; set; }
        public double Duration { get; set; }
        public string DurationText { get; set; }
        public string Instruction { get; set; }
        public double StartLatitude { get; set; }
        public double StartLongitude { get; set; }
        public double EndLatitude { get; set; }
        public double EndLongitude { get; set; }
    }
}

