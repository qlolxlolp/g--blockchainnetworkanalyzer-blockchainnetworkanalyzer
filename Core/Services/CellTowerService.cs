using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Logging;
using static BlockchainNetworkAnalyzer.App;

namespace BlockchainNetworkAnalyzer.Core.Services
{
    /// <summary>
    /// Service for retrieving cell tower and cellular network information
    /// </summary>
    public class CellTowerService
    {
        private readonly ILogger<CellTowerService> _logger;
        private readonly HttpClient _httpClient;

        public CellTowerService()
        {
            _logger = App.LoggerFactory.CreateLogger<CellTowerService>();
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        public async Task<List<CellTowerInfo>> GetCellTowersAsync(double latitude, double longitude, int radiusKm = 5)
        {
            var cellTowers = new List<CellTowerInfo>();

            try
            {
                // Query multiple sources
                var tasks = new List<Task<List<CellTowerInfo>>>
                {
                    GetCellTowersFromOpenCellID(latitude, longitude, radiusKm),
                    GetCellTowersFromCellMapper(latitude, longitude, radiusKm),
                    GetCellTowersFromMozillaLocationService(latitude, longitude, radiusKm)
                };

                var results = await Task.WhenAll(tasks);

                foreach (var result in results)
                {
                    cellTowers.AddRange(result);
                }

                // Remove duplicates and sort by distance
                cellTowers = cellTowers
                    .GroupBy(t => new { t.MCC, t.MNC, t.LAC, t.CellID })
                    .Select(g => g.OrderBy(t => t.Distance).First())
                    .OrderBy(t => t.Distance)
                    .ToList();

                _logger.LogInformation($"Retrieved {cellTowers.Count} cell towers near {latitude}, {longitude}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving cell towers");
            }

            return cellTowers;
        }

        private async Task<List<CellTowerInfo>> GetCellTowersFromOpenCellID(double lat, double lng, int radiusKm)
        {
            var towers = new List<CellTowerInfo>();

            try
            {
                var apiKey = Configuration["CellTower:OpenCellIDAPIKey"] ?? "";
                var url = $"https://us1.unwiredlabs.com/v2/process.php?token={apiKey}&mcc=432&mnc=-1&lac=-1&cellid=-1&signal=-85&radio=GSM&address=1&lat={lat}&lon={lng}&range={radiusKm * 1000}";
                
                var response = await _httpClient.GetStringAsync(url);
                var data = JObject.Parse(response);

                if (data["status"]?.ToString() == "ok" && data["cells"] != null)
                {
                    foreach (var cell in data["cells"])
                    {
                        towers.Add(new CellTowerInfo
                        {
                            MCC = cell["mcc"]?.ToString(),
                            MNC = cell["mnc"]?.ToString(),
                            LAC = cell["lac"]?.ToString(),
                            CellID = cell["cid"]?.ToString(),
                            Latitude = double.Parse(cell["lat"]?.ToString() ?? "0"),
                            Longitude = double.Parse(cell["lon"]?.ToString() ?? "0"),
                            Range = int.Parse(cell["range"]?.ToString() ?? "0"),
                            RadioType = cell["radio"]?.ToString(),
                            Source = "OpenCellID",
                            Distance = CalculateDistance(lat, lng, 
                                double.Parse(cell["lat"]?.ToString() ?? "0"),
                                double.Parse(cell["lon"]?.ToString() ?? "0"))
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get cell towers from OpenCellID");
            }

            return towers;
        }

        private async Task<List<CellTowerInfo>> GetCellTowersFromCellMapper(double lat, double lng, int radiusKm)
        {
            var towers = new List<CellTowerInfo>();

            try
            {
                // CellMapper API (unofficial)
                var url = $"https://www.cellmapper.net/api/search?mcc=432&lat={lat}&lon={lng}&radius={radiusKm}";
                
                var response = await _httpClient.GetStringAsync(url);
                var data = JArray.Parse(response);

                foreach (var cell in data)
                {
                    towers.Add(new CellTowerInfo
                    {
                        MCC = "432", // Iran
                        MNC = cell["mnc"]?.ToString(),
                        LAC = cell["tac"]?.ToString(),
                        CellID = cell["cid"]?.ToString(),
                        Latitude = double.Parse(cell["latitude"]?.ToString() ?? "0"),
                        Longitude = double.Parse(cell["longitude"]?.ToString() ?? "0"),
                        RadioType = cell["radio"]?.ToString(),
                        Source = "CellMapper",
                        Distance = CalculateDistance(lat, lng,
                            double.Parse(cell["latitude"]?.ToString() ?? "0"),
                            double.Parse(cell["longitude"]?.ToString() ?? "0"))
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get cell towers from CellMapper");
            }

            return towers;
        }

        private async Task<List<CellTowerInfo>> GetCellTowersFromMozillaLocationService(double lat, double lng, int radiusKm)
        {
            var towers = new List<CellTowerInfo>();

            try
            {
                // Mozilla Location Service
                var url = $"https://location.services.mozilla.com/v1/search?key=test&q={lat},{lng}&wifi=0";
                
                var response = await _httpClient.GetStringAsync(url);
                var data = JObject.Parse(response);

                if (data["cells"] != null)
                {
                    foreach (var cell in data["cells"])
                    {
                        towers.Add(new CellTowerInfo
                        {
                            MCC = cell["mcc"]?.ToString(),
                            MNC = cell["mnc"]?.ToString(),
                            LAC = cell["lac"]?.ToString(),
                            CellID = cell["cid"]?.ToString(),
                            Latitude = lat,
                            Longitude = lng,
                            Source = "Mozilla Location Service",
                            Distance = 0
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get cell towers from Mozilla Location Service");
            }

            return towers;
        }

        public async Task<CellTowerInfo> GetClosestCellTowerAsync(double latitude, double longitude)
        {
            var towers = await GetCellTowersAsync(latitude, longitude, 10);
            return towers.OrderBy(t => t.Distance).FirstOrDefault();
        }

        public async Task<List<CellTowerInfo>> GetRecentCellTowersAsync(string ipAddress)
        {
            // Get cell towers that were recently used by this IP
            // This would require historical data or API access
            var towers = new List<CellTowerInfo>();
            
            // Implementation would query database or API for historical cell tower data
            await Task.CompletedTask;
            
            return towers;
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; // Earth radius in km
            var dLat = (lat2 - lat1) * Math.PI / 180;
            var dLon = (lon2 - lon1) * Math.PI / 180;
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }
    }

    public class CellTowerInfo
    {
        public string MCC { get; set; } // Mobile Country Code (432 for Iran)
        public string MNC { get; set; } // Mobile Network Code
        public string LAC { get; set; } // Location Area Code
        public string CellID { get; set; } // Cell ID
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int Range { get; set; } // Range in meters
        public string RadioType { get; set; } // GSM, LTE, 5G, etc.
        public string Source { get; set; }
        public double Distance { get; set; } // Distance from query point in km
        public string Operator { get; set; } // MCI, Irancell, Rightel
        public DateTime LastSeen { get; set; }
        public int SignalStrength { get; set; } // dBm
    }
}

