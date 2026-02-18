using System;
using System.Net.Http;
using System.Threading.Tasks;
using IranianMinerDetector.WinForms.Data;
using IranianMinerDetector.WinForms.Models;
using Newtonsoft.Json;

namespace IranianMinerDetector.WinForms.Services
{
    public class GeolocationService
    {
        private readonly HttpClient _httpClient;
        private readonly DatabaseManager _db = DatabaseManager.Instance;
        private const string API_URL = "http://ip-api.com/json/{0}";

        public GeolocationService()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(5)
            };
        }

        public async Task<GeolocationData?> LookupIPAsync(string ipAddress)
        {
            // Check cache first
            var cached = _db.GetCachedGeolocation(ipAddress);
            if (cached != null)
            {
                return cached;
            }

            try
            {
                // Check if IP is Iranian from local database
                var isp = IranianISPs.IdentifyISP(ipAddress);
                if (isp != null)
                {
                    var geoData = new GeolocationData
                    {
                        IPAddress = ipAddress,
                        Country = "Iran",
                        ISP = isp.Name,
                        CachedAt = DateTime.Now
                    };

                    // Cache it
                    _db.CacheGeolocation(geoData);
                    return geoData;
                }

                // Make API request (rate limited)
                await Task.Delay(1000); // Respect rate limiting

                var url = string.Format(API_URL, ipAddress);
                var response = await _httpClient.GetAsync(url);
                var json = await response.Content.ReadAsStringAsync();

                dynamic? data = JsonConvert.DeserializeObject(json);

                if (data != null && data.status == "success")
                {
                    geoData = new GeolocationData
                    {
                        IPAddress = ipAddress,
                        Country = data.country,
                        Region = data.regionName,
                        City = data.city,
                        ISP = data.isp,
                        Organization = data.org,
                        Latitude = data.lat,
                        Longitude = data.lon,
                        CachedAt = DateTime.Now
                    };

                    _db.CacheGeolocation(geoData);
                    return geoData;
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<string?> GetISPAsync(string ipAddress)
        {
            var geo = await LookupIPAsync(ipAddress);
            return geo?.ISP;
        }

        public async Task<(double? lat, double? lon)> GetCoordinatesAsync(string ipAddress)
        {
            var geo = await LookupIPAsync(ipAddress);
            return (geo?.Latitude, geo?.Longitude);
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
