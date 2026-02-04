using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using BlockchainNetworkAnalyzer.Core;
using static BlockchainNetworkAnalyzer.App;

namespace BlockchainNetworkAnalyzer.Core
{
    public class FakeIPDetector
    {
        private readonly ILogger<FakeIPDetector> _logger;
        private readonly HttpClient _httpClient;
        private readonly HashSet<string> _knownFakeIPRanges;
        private readonly SemaphoreSlim _semaphore;

        public FakeIPDetector()
        {
            _logger = App.LoggerFactory.CreateLogger<FakeIPDetector>();
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMilliseconds(
                    int.Parse(App.Configuration["IPDetection:ValidationTimeout"] ?? "5000"))
            };
            
            _semaphore = new SemaphoreSlim(
                int.Parse(App.Configuration["IPDetection:MaxValidationConcurrent"] ?? "10"),
                int.Parse(App.Configuration["IPDetection:MaxValidationConcurrent"] ?? "10"));

            _knownFakeIPRanges = LoadKnownFakeIPRanges();
            LoadFakeIPDatabase();
        }

        public async Task<FakeIPDetectionResult> DetectFakeIPAsync(string ipAddress)
        {
            var result = new FakeIPDetectionResult
            {
                IPAddress = ipAddress,
                IsFake = false,
                Confidence = 0.0,
                Reasons = new List<string>()
            };

            try
            {
                // Check against known fake IP ranges
                if (_knownFakeIPRanges.Any(range => IsIPInRange(ipAddress, range)))
                {
                    result.IsFake = true;
                    result.Confidence = 0.95;
                    result.Reasons.Add("IP is in known fake IP range database");
                    return result;
                }

                // Check database
                if (IsInFakeIPDatabase(ipAddress))
                {
                    result.IsFake = true;
                    result.Confidence = 0.90;
                    result.Reasons.Add("IP found in fake IP database");
                    return result;
                }

                // Validate using multiple services
                var validationTasks = new List<Task<IPValidationResponse>>();
                var services = App.Configuration.GetSection("IPDetection:ValidationServices").Get<string[]>();

                if (services != null && services.Length > 0)
                {
                    await _semaphore.WaitAsync();
                    try
                    {
                        foreach (var service in services)
                        {
                            validationTasks.Add(ValidateIPWithServiceAsync(ipAddress, service));
                        }

                        var responses = await Task.WhenAll(validationTasks);
                        AnalyzeValidationResponses(result, responses);
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                }

                // Additional heuristics
                ApplyHeuristics(result, ipAddress);

                // Save to database if fake
                if (result.IsFake && result.Confidence > 0.7)
                {
                    SaveToFakeIPDatabase(ipAddress, result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error detecting fake IP for {ipAddress}");
            }

            return result;
        }

        private async Task<IPValidationResponse> ValidateIPWithServiceAsync(string ip, string serviceUrl)
        {
            try
            {
                var url = serviceUrl.Replace("{ip}", ip);
                var response = await _httpClient.GetStringAsync(url);
                return JsonConvert.DeserializeObject<IPValidationResponse>(response);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to validate IP {ip} with service {serviceUrl}");
                return null;
            }
        }

        private void AnalyzeValidationResponses(FakeIPDetectionResult result, IPValidationResponse[] responses)
        {
            var validResponses = responses.Where(r => r != null).ToList();

            if (validResponses.Count == 0)
            {
                result.Reasons.Add("Could not validate IP - all services failed");
                result.Confidence += 0.1;
                return;
            }

            // Check for VPN/Proxy indicators
            var vpnCount = validResponses.Count(r => r.IsVPN == true || r.IsProxy == true);
            if (vpnCount > 0)
            {
                result.IsFake = true;
                result.Confidence += 0.4;
                result.Reasons.Add($"Detected as VPN/Proxy by {vpnCount} service(s)");
            }

            // Check for hosting/datacenter IPs (often used by IP changing tools)
            var hostingCount = validResponses.Count(r => 
                !string.IsNullOrEmpty(r.Hosting) && 
                (r.Hosting.Contains("hosting", StringComparison.OrdinalIgnoreCase) ||
                 r.Hosting.Contains("datacenter", StringComparison.OrdinalIgnoreCase)));
            if (hostingCount > 0)
            {
                result.Confidence += 0.2;
                result.Reasons.Add("IP appears to be from hosting/datacenter (common for IP changers)");
            }

            // Check for mismatched geolocation
            var locations = validResponses.Where(r => !string.IsNullOrEmpty(r.Country)).Select(r => r.Country).Distinct().ToList();
            if (locations.Count > 1)
            {
                result.IsFake = true;
                result.Confidence += 0.3;
                result.Reasons.Add($"Geolocation mismatch detected across services: {string.Join(", ", locations)}");
            }
        }

        private void ApplyHeuristics(FakeIPDetectionResult result, string ip)
        {
            // Check for common fake IP patterns
            if (IsCommonFakePattern(ip))
            {
                result.IsFake = true;
                result.Confidence += 0.3;
                result.Reasons.Add("IP matches common fake IP pattern");
            }

            // Check if IP is in known VPN/Proxy ranges
            if (IsVPNProxyRange(ip))
            {
                result.IsFake = true;
                result.Confidence += 0.4;
                result.Reasons.Add("IP is in known VPN/Proxy range");
            }
        }

        private bool IsCommonFakePattern(string ip)
        {
            // Common patterns used by fake IP tools
            var patterns = new[]
            {
                @"^10\.10\.10\.",  // Common fake range
                @"^192\.0\.2\.",   // TEST-NET
                @"^198\.51\.100\.", // TEST-NET-2
                @"^203\.0\.113\.",  // TEST-NET-3
            };

            return patterns.Any(pattern => Regex.IsMatch(ip, pattern));
        }

        private bool IsVPNProxyRange(string ip)
        {
            // استفاده از دیتابیس جهانی رایگان (IPHub):
            try
            {
                // API پابلیک IPHub (rate-limited and free tier):
                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("X-Key", "free_iphub_api_key_goes_here");
                var resp = client.GetAsync($"https://v2.api.iphub.info/ip/{ip}").Result;
                if(resp.IsSuccessStatusCode)
                {
                    var json = resp.Content.ReadAsStringAsync().Result;
                    var jobj = Newtonsoft.Json.Linq.JObject.Parse(json);
                    int block = jobj.Value<int>("block");
                    // block==1 => Hosting/VPN/Proxy
                    if(block==1) return true;
                }
            }
            catch(Exception ex) {
                _logger.LogDebug(ex, "IPHub query failed");
            }
            return false;
        }

        private bool IsIPInRange(string ip, string range)
        {
            if (!range.Contains("/"))
                return ip == range;

            var parts = range.Split('/');
            if (parts.Length != 2)
                return false;

            var networkIP = IPAddress.Parse(parts[0]);
            var prefixLength = int.Parse(parts[1]);

            var targetIP = IPAddress.Parse(ip);
            return IsIPInSubnet(targetIP, networkIP, prefixLength);
        }

        private bool IsIPInSubnet(IPAddress address, IPAddress subnet, int prefixLength)
        {
            var addressBytes = address.GetAddressBytes();
            var subnetBytes = subnet.GetAddressBytes();

            if (addressBytes.Length != subnetBytes.Length)
                return false;

            var bytes = prefixLength / 8;
            var bits = prefixLength % 8;

            for (int i = 0; i < bytes; i++)
            {
                if (addressBytes[i] != subnetBytes[i])
                    return false;
            }

            if (bits > 0)
            {
                var mask = (byte)(0xFF << (8 - bits));
                return (addressBytes[bytes] & mask) == (subnetBytes[bytes] & mask);
            }

            return true;
        }

        private HashSet<string> LoadKnownFakeIPRanges()
        {
            var ranges = new HashSet<string>
            {
                // Common fake IP ranges used by IP changing tools
                "10.10.10.0/24",
                "192.0.2.0/24",
                "198.51.100.0/24",
                "203.0.113.0/24"
            };

            // Load from database/file
            try
            {
                LoadFakeIPDatabase();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load fake IP database");
            }

            return ranges;
        }

        private void LoadFakeIPDatabase()
        {
            // Load from SQLite database
            try
            {
                using var connection = DatabaseManager.GetConnection();
                connection.Open();
                var sql = "SELECT IPAddress, IPRange FROM FakeIPDatabase";
                using var command = new SQLiteCommand(sql, connection);
                using var reader = command.ExecuteReader();
                
                while (reader.Read())
                {
                    var ip = reader["IPAddress"]?.ToString();
                    var range = reader["IPRange"]?.ToString();
                    
                    if (!string.IsNullOrEmpty(ip))
                        _knownFakeIPRanges.Add(ip);
                    if (!string.IsNullOrEmpty(range))
                        _knownFakeIPRanges.Add(range);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load fake IP database");
            }
        }

        private bool IsInFakeIPDatabase(string ipAddress)
        {
            try
            {
                using var connection = DatabaseManager.GetConnection();
                connection.Open();
                var sql = "SELECT COUNT(*) FROM FakeIPDatabase WHERE IPAddress = @IP OR IPRange LIKE @IPRange";
                using var command = new SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue("@IP", ipAddress);
                command.Parameters.AddWithValue("@IPRange", $"%{ipAddress}%");
                
                var count = Convert.ToInt64(command.ExecuteScalar());
                return count > 0;
            }
            catch
            {
                return false;
            }
        }

        private void SaveToFakeIPDatabase(string ipAddress, FakeIPDetectionResult result)
        {
            try
            {
                using var connection = DatabaseManager.GetConnection();
                connection.Open();
                var sql = @"INSERT OR REPLACE INTO FakeIPDatabase 
                           (IPAddress, Source, DetectionMethod, ConfidenceLevel) 
                           VALUES (@IP, 'Auto-Detection', @Method, @Confidence)";
                using var command = new SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue("@IP", ipAddress);
                command.Parameters.AddWithValue("@Method", string.Join("; ", result.Reasons));
                command.Parameters.AddWithValue("@Confidence", result.Confidence);
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to save fake IP to database: {ipAddress}");
            }
        }
    }

    public class FakeIPDetectionResult
    {
        public string IPAddress { get; set; }
        public bool IsFake { get; set; }
        public double Confidence { get; set; }
        public List<string> Reasons { get; set; } = new List<string>();
    }

    public class IPValidationResponse
    {
        [JsonProperty("ip")]
        public string IP { get; set; }
        
        [JsonProperty("country")]
        public string Country { get; set; }
        
        [JsonProperty("city")]
        public string City { get; set; }
        
        [JsonProperty("org")]
        public string Organization { get; set; }
        
        [JsonProperty("hosting")]
        public string Hosting { get; set; }
        
        [JsonProperty("vpn")]
        public bool? IsVPN { get; set; }
        
        [JsonProperty("proxy")]
        public bool? IsProxy { get; set; }
    }
}

