using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Logging;
using static BlockchainNetworkAnalyzer.App;

namespace BlockchainNetworkAnalyzer.Core.Services
{
    /// <summary>
    /// Comprehensive geolocation service using multiple technologies and data sources
    /// </summary>
    public class GeolocationService
    {
        private readonly ILogger<GeolocationService> _logger;
        private readonly HttpClient _httpClient;
        private readonly List<IGeolocationProvider> _providers;

        public GeolocationService()
        {
            _logger = App.LoggerFactory.CreateLogger<GeolocationService>();
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");

            // Initialize multiple providers for accuracy
            _providers = new List<IGeolocationProvider>
            {
                new IPGeolocationProvider(_httpClient),
                new MaxMindProvider(_httpClient),
                new IPAPIProvider(_httpClient),
                new IPInfoProvider(_httpClient),
                new OpenCageProvider(_httpClient)
            };
        }

        public async Task<GeolocationResult> GetLocationAsync(string ipAddress)
        {
            var result = new GeolocationResult
            {
                IPAddress = ipAddress,
                Sources = new List<GeolocationSource>()
            };

            try
            {
                // Query multiple providers in parallel for accuracy
                var tasks = _providers.Select(p => p.GetLocationAsync(ipAddress)).ToList();
                var results = await Task.WhenAll(tasks);

                // Aggregate results from multiple sources
                foreach (var providerResult in results.Where(r => r != null && r.Success))
                {
                    result.Sources.Add(new GeolocationSource
                    {
                        Provider = providerResult.ProviderName,
                        Latitude = providerResult.Latitude,
                        Longitude = providerResult.Longitude,
                        Accuracy = providerResult.Accuracy,
                        Confidence = providerResult.Confidence
                    });
                }

                // Calculate weighted average for best accuracy
                result = CalculateWeightedAverage(result);

                _logger.LogInformation($"Retrieved location for {ipAddress}: {result.Latitude}, {result.Longitude}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting location for {ipAddress}");
            }

            return result;
        }

        private GeolocationResult CalculateWeightedAverage(GeolocationResult result)
        {
            if (result.Sources.Count == 0)
                return result;

            // Weight sources by accuracy and confidence
            double totalWeight = 0;
            double weightedLat = 0;
            double weightedLng = 0;

            foreach (var source in result.Sources)
            {
                var weight = source.Accuracy * source.Confidence;
                weightedLat += source.Latitude * weight;
                weightedLng += source.Longitude * weight;
                totalWeight += weight;
            }

            if (totalWeight > 0)
            {
                result.Latitude = weightedLat / totalWeight;
                result.Longitude = weightedLng / totalWeight;
                result.Confidence = result.Sources.Average(s => s.Confidence);
                result.Accuracy = result.Sources.Average(s => s.Accuracy);
            }
            else
            {
                result.Latitude = result.Sources.Average(s => s.Latitude);
                result.Longitude = result.Sources.Average(s => s.Longitude);
            }

            return result;
        }

        public async Task<AddressInfo> GetAddressAsync(double latitude, double longitude)
        {
            var addressInfo = new AddressInfo
            {
                Latitude = latitude,
                Longitude = longitude
            };

            try
            {
                // Reverse geocoding from multiple sources
                var tasks = new List<Task<AddressInfo>>
                {
                    GetAddressFromOpenCage(latitude, longitude),
                    GetAddressFromNominatim(latitude, longitude),
                    GetAddressFromGoogle(latitude, longitude)
                };

                var results = await Task.WhenAll(tasks);
                
                // Use the most complete result
                var bestResult = results
                    .Where(r => r != null && !string.IsNullOrEmpty(r.FullAddress))
                    .OrderByDescending(r => GetAddressCompleteness(r))
                    .FirstOrDefault();

                if (bestResult != null)
                {
                    addressInfo = bestResult;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting address for {latitude}, {longitude}");
            }

            return addressInfo;
        }

        private async Task<AddressInfo> GetAddressFromOpenCage(double lat, double lng)
        {
            try
            {
                // OpenCage Geocoding API
                var apiKey = App.Configuration["Geolocation:OpenCageAPIKey"] ?? "";
                var url = $"https://api.opencagedata.com/geocode/v1/json?q={lat}+{lng}&key={apiKey}&language=fa";
                var response = await _httpClient.GetStringAsync(url);
                var data = JObject.Parse(response);

                if (data["results"]?.First != null)
                {
                    var result = data["results"][0];
                    var components = result["components"];

                    return new AddressInfo
                    {
                        Latitude = lat,
                        Longitude = lng,
                        FullAddress = result["formatted"]?.ToString(),
                        Street = components?["road"]?.ToString(),
                        HouseNumber = components?["house_number"]?.ToString(),
                        City = components?["city"]?.ToString() ?? components?["town"]?.ToString(),
                        Province = components?["state"]?.ToString(),
                        PostalCode = components?["postcode"]?.ToString(),
                        Country = components?["country"]?.ToString(),
                        Source = "OpenCage"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get address from OpenCage");
            }

            return null;
        }

        private async Task<AddressInfo> GetAddressFromNominatim(double lat, double lng)
        {
            try
            {
                // OpenStreetMap Nominatim (free, no API key required)
                var url = $"https://nominatim.openstreetmap.org/reverse?format=json&lat={lat}&lon={lng}&addressdetails=1&accept-language=fa";
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "BlockchainNetworkAnalyzer/1.0");
                
                var response = await _httpClient.GetStringAsync(url);
                var data = JObject.Parse(response);
                var address = data["address"];

                if (address != null)
                {
                    return new AddressInfo
                    {
                        Latitude = lat,
                        Longitude = lng,
                        FullAddress = data["display_name"]?.ToString(),
                        Street = address["road"]?.ToString(),
                        HouseNumber = address["house_number"]?.ToString(),
                        City = address["city"]?.ToString() ?? address["town"]?.ToString(),
                        Province = address["state"]?.ToString(),
                        PostalCode = address["postcode"]?.ToString(),
                        Country = address["country"]?.ToString(),
                        Source = "Nominatim"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get address from Nominatim");
            }

            return null;
        }

        private async Task<AddressInfo> GetAddressFromGoogle(double lat, double lng)
        {
            try
            {
                var apiKey = App.Configuration["Geolocation:GoogleMapsAPIKey"] ?? "";
                if (string.IsNullOrEmpty(apiKey))
                    return null;

                var url = $"https://maps.googleapis.com/maps/api/geocode/json?latlng={lat},{lng}&key={apiKey}&language=fa";
                var response = await _httpClient.GetStringAsync(url);
                var data = JObject.Parse(response);

                if (data["results"]?.First != null)
                {
                    var result = data["results"][0];
                    var addressComponents = result["address_components"];

                    var addressInfo = new AddressInfo
                    {
                        Latitude = lat,
                        Longitude = lng,
                        FullAddress = result["formatted_address"]?.ToString(),
                        Source = "Google Maps"
                    };

                    foreach (var component in addressComponents)
                    {
                        var types = component["types"].ToObject<List<string>>();
                        var longName = component["long_name"]?.ToString();
                        var shortName = component["short_name"]?.ToString();

                        if (types.Contains("street_number"))
                            addressInfo.HouseNumber = longName;
                        else if (types.Contains("route"))
                            addressInfo.Street = longName;
                        else if (types.Contains("locality") || types.Contains("sublocality"))
                            addressInfo.City = longName;
                        else if (types.Contains("administrative_area_level_1"))
                            addressInfo.Province = longName;
                        else if (types.Contains("postal_code"))
                            addressInfo.PostalCode = longName;
                        else if (types.Contains("country"))
                            addressInfo.Country = longName;
                    }

                    return addressInfo;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get address from Google");
            }

            return null;
        }

        private int GetAddressCompleteness(AddressInfo address)
        {
            int score = 0;
            if (!string.IsNullOrEmpty(address.FullAddress)) score += 3;
            if (!string.IsNullOrEmpty(address.Street)) score += 2;
            if (!string.IsNullOrEmpty(address.HouseNumber)) score += 1;
            if (!string.IsNullOrEmpty(address.City)) score += 1;
            if (!string.IsNullOrEmpty(address.Province)) score += 1;
            if (!string.IsNullOrEmpty(address.PostalCode)) score += 1;
            return score;
        }
    }

    // Interface for geolocation providers
    public interface IGeolocationProvider
    {
        string ProviderName { get; }
        Task<GeolocationProviderResult> GetLocationAsync(string ipAddress);
    }

    // Provider implementations
    public class IPGeolocationProvider : IGeolocationProvider
    {
        private readonly HttpClient _httpClient;
        public string ProviderName => "IPGeolocation.io";

        public IPGeolocationProvider(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<GeolocationProviderResult> GetLocationAsync(string ipAddress)
        {
            try
            {
                var apiKey = App.Configuration["Geolocation:IPGeolocationAPIKey"] ?? "";
                var url = $"https://api.ipgeolocation.io/ipgeo?ip={ipAddress}&apiKey={apiKey}";
                var response = await _httpClient.GetStringAsync(url);
                var data = JObject.Parse(response);

                return new GeolocationProviderResult
                {
                    Success = true,
                    ProviderName = ProviderName,
                    Latitude = double.Parse(data["latitude"]?.ToString() ?? "0"),
                    Longitude = double.Parse(data["longitude"]?.ToString() ?? "0"),
                    Accuracy = 100, // meters
                    Confidence = 0.85
                };
            }
            catch
            {
                return new GeolocationProviderResult { Success = false };
            }
        }
    }

    public class MaxMindProvider : IGeolocationProvider
    {
        private readonly HttpClient _httpClient;
        public string ProviderName => "MaxMind GeoIP2";

        public MaxMindProvider(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<GeolocationProviderResult> GetLocationAsync(string ipAddress)
        {
            // MaxMind implementation would go here
            await Task.CompletedTask;
            return new GeolocationProviderResult { Success = false };
        }
    }

    public class IPAPIProvider : IGeolocationProvider
    {
        private readonly HttpClient _httpClient;
        public string ProviderName => "IP-API";

        public IPAPIProvider(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<GeolocationProviderResult> GetLocationAsync(string ipAddress)
        {
            try
            {
                var url = $"http://ip-api.com/json/{ipAddress}?fields=status,lat,lon";
                var response = await _httpClient.GetStringAsync(url);
                var data = JObject.Parse(response);

                if (data["status"]?.ToString() == "success")
                {
                    return new GeolocationProviderResult
                    {
                        Success = true,
                        ProviderName = ProviderName,
                        Latitude = double.Parse(data["lat"]?.ToString() ?? "0"),
                        Longitude = double.Parse(data["lon"]?.ToString() ?? "0"),
                        Accuracy = 500,
                        Confidence = 0.80
                    };
                }
            }
            catch { }

            return new GeolocationProviderResult { Success = false };
        }
    }

    public class IPInfoProvider : IGeolocationProvider
    {
        private readonly HttpClient _httpClient;
        public string ProviderName => "IPInfo.io";

        public IPInfoProvider(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<GeolocationProviderResult> GetLocationAsync(string ipAddress)
        {
            try
            {
                var token = App.Configuration["Geolocation:IPInfoToken"] ?? "";
                var url = $"https://ipinfo.io/{ipAddress}/json?token={token}";
                var response = await _httpClient.GetStringAsync(url);
                var data = JObject.Parse(response);

                if (data["loc"] != null)
                {
                    var loc = data["loc"].ToString().Split(',');
                    return new GeolocationProviderResult
                    {
                        Success = true,
                        ProviderName = ProviderName,
                        Latitude = double.Parse(loc[0]),
                        Longitude = double.Parse(loc[1]),
                        Accuracy = 1000,
                        Confidence = 0.75
                    };
                }
            }
            catch { }

            return new GeolocationProviderResult { Success = false };
        }
    }

    public class OpenCageProvider : IGeolocationProvider
    {
        private readonly HttpClient _httpClient;
        public string ProviderName => "OpenCage";

        public OpenCageProvider(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<GeolocationProviderResult> GetLocationAsync(string ipAddress)
        {
            // Implementation using OpenCage
            await Task.CompletedTask;
            return new GeolocationProviderResult { Success = false };
        }
    }

    // Result models
    public class GeolocationResult
    {
        public string IPAddress { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Accuracy { get; set; } // in meters
        public double Confidence { get; set; } // 0-1
        public List<GeolocationSource> Sources { get; set; } = new List<GeolocationSource>();
        public DateTime RetrievedAt { get; set; } = DateTime.Now;
    }

    public class GeolocationSource
    {
        public string Provider { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Accuracy { get; set; }
        public double Confidence { get; set; }
    }

    public class GeolocationProviderResult
    {
        public bool Success { get; set; }
        public string ProviderName { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Accuracy { get; set; }
        public double Confidence { get; set; }
    }

    public class AddressInfo
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string FullAddress { get; set; }
        public string Street { get; set; }
        public string HouseNumber { get; set; }
        public string City { get; set; }
        public string Province { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
        public string Source { get; set; }
    }
}

