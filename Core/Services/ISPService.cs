using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Logging;
using BlockchainNetworkAnalyzer.Core;
using static BlockchainNetworkAnalyzer.App;

namespace BlockchainNetworkAnalyzer.Core.Services
{
    /// <summary>
    /// Service to fetch ISP and IP range information for Iranian provinces and cities
    /// </summary>
    public class ISPService
    {
        private readonly ILogger<ISPService> _logger;
        private readonly HttpClient _httpClient;
        private readonly Dictionary<string, List<IPRangeInfo>> _cache = new Dictionary<string, List<IPRangeInfo>>();

        public ISPService()
        {
            _logger = App.LoggerFactory.CreateLogger<ISPService>();
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
        }

        public async Task<List<IPRangeInfo>> GetIPRangesAsync(string province, string city = null)
        {
            var cacheKey = $"{province}_{city ?? "All"}";
            
            if (_cache.ContainsKey(cacheKey))
            {
                _logger.LogInformation($"Returning cached IP ranges for {cacheKey}");
                return _cache[cacheKey];
            }

            var ipRanges = new List<IPRangeInfo>();

            try
            {
                // Get IP ranges from multiple sources
                var tasks = new List<Task<List<IPRangeInfo>>>
                {
                    GetIPRangesFromRIPE(province, city),
                    GetIPRangesFromAPNIC(province, city),
                    GetIPRangesFromWhois(province, city),
                    GetIPRangesFromISPData(province, city)
                };

                var results = await Task.WhenAll(tasks);
                foreach (var result in results)
                {
                    ipRanges.AddRange(result);
                }

                // Remove duplicates
                ipRanges = ipRanges
                    .GroupBy(r => $"{r.StartIP}-{r.EndIP}")
                    .Select(g => g.First())
                    .ToList();

                _cache[cacheKey] = ipRanges;
                await SaveToDatabase(province, city, ipRanges);

                _logger.LogInformation($"Retrieved {ipRanges.Count} IP ranges for {cacheKey}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching IP ranges for {province}, {city}");
            }

            return ipRanges;
        }

        private async Task<List<IPRangeInfo>> GetIPRangesFromRIPE(string province, string city)
        {
            var ranges = new List<IPRangeInfo>();
            
            try
            {
                // RIPE NCC database for Iranian networks
                var url = "https://stat.ripe.net/data/country-resource-list/data.json?resource=IR";
                var response = await _httpClient.GetStringAsync(url);
                var data = JObject.Parse(response);

                if (data["data"]?["resources"]?["ipv4"] != null)
                {
                    foreach (var ipRange in data["data"]["resources"]["ipv4"])
                    {
                        ranges.Add(ParseIPRange(ipRange.ToString(), "RIPE", province, city));
                    }
                }

                if (data["data"]?["resources"]?["ipv6"] != null)
                {
                    foreach (var ipRange in data["data"]["resources"]["ipv6"])
                    {
                        ranges.Add(ParseIPRange(ipRange.ToString(), "RIPE", province, city, isIPv6: true));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch from RIPE");
            }

            return ranges;
        }

        private async Task<List<IPRangeInfo>> GetIPRangesFromAPNIC(string province, string city)
        {
            var ranges = new List<IPRangeInfo>();
            
            try
            {
                // APNIC database for Asian-Pacific networks
                var url = "https://ftp.apnic.net/stats/apnic/delegated-apnic-extended-latest";
                var response = await _httpClient.GetStringAsync(url);
                var lines = response.Split('\n');

                foreach (var line in lines)
                {
                    if (line.Contains("|IR|") && (line.Contains("ipv4") || line.Contains("ipv6")))
                    {
                        var parts = line.Split('|');
                        if (parts.Length >= 5)
                        {
                            var ip = parts[3];
                            var count = parts[4];
                            
                            if (int.TryParse(count, out var ipCount))
                            {
                                ranges.Add(new IPRangeInfo
                                {
                                    StartIP = ip,
                                    EndIP = CalculateEndIP(ip, ipCount),
                                    CIDR = $"{ip}/{GetCIDRFromCount(ipCount)}",
                                    ISP = parts[5] ?? "Unknown",
                                    Source = "APNIC",
                                    Province = province,
                                    City = city,
                                    IsIPv6 = line.Contains("ipv6")
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch from APNIC");
            }

            return ranges;
        }

        private async Task<List<IPRangeInfo>> GetIPRangesFromWhois(string province, string city)
        {
            var ranges = new List<IPRangeInfo>();
            
            try
            {
                // Use whois APIs for Iranian ISPs
                var isps = GetIranianISPs();
                
                foreach (var isp in isps)
                {
                    try
                    {
                        var url = $"https://whoisapi.com/api/v1/range?org={isp}";
                        var response = await _httpClient.GetStringAsync(url);
                        var data = JObject.Parse(response);

                        if (data["ranges"] != null)
                        {
                            foreach (var range in data["ranges"])
                            {
                                ranges.Add(new IPRangeInfo
                                {
                                    StartIP = range["start"]?.ToString(),
                                    EndIP = range["end"]?.ToString(),
                                    CIDR = range["cidr"]?.ToString(),
                                    ISP = isp,
                                    Source = "WhoisAPI",
                                    Province = province,
                                    City = city
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, $"Failed to fetch ranges for ISP: {isp}");
                    }
                    
                    await Task.Delay(500); // Rate limiting
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch from Whois");
            }

            return ranges;
        }

        private async Task<List<IPRangeInfo>> GetIPRangesFromISPData(string province, string city)
        {
            var ranges = new List<IPRangeInfo>();
            
            try
            {
                // Iranian ISP-specific data sources
                var ispRanges = GetKnownIranianISPRanges();
                
                foreach (var ispRange in ispRanges)
                {
                    ranges.Add(new IPRangeInfo
                    {
                        StartIP = ispRange.StartIP,
                        EndIP = ispRange.EndIP,
                        CIDR = ispRange.CIDR,
                        ISP = ispRange.ISP,
                        Source = "Known Database",
                        Province = province,
                        City = city,
                        Mask = ispRange.Mask
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch from ISP data");
            }

            return ranges;
        }

        private IPRangeInfo ParseIPRange(string range, string source, string province, string city, bool isIPv6 = false)
        {
            // Parse CIDR notation: 192.168.1.0/24
            var parts = range.Split('/');
            var startIP = parts[0];
            var cidr = int.Parse(parts[1]);

            return new IPRangeInfo
            {
                StartIP = startIP,
                EndIP = CalculateEndIPFromCIDR(startIP, cidr),
                CIDR = range,
                Mask = CalculateSubnetMask(cidr),
                ISP = "Unknown",
                Source = source,
                Province = province,
                City = city,
                IsIPv6 = isIPv6
            };
        }

        private string CalculateEndIP(string startIP, int count)
        {
            // Simplified calculation - in production, use proper IP math
            var ip = System.Net.IPAddress.Parse(startIP);
            var bytes = ip.GetAddressBytes().ToList();
            bytes.Reverse();
            var total = BitConverter.ToUInt32(bytes.ToArray(), 0) + (uint)count;
            var endBytes = BitConverter.GetBytes(total).ToList();
            endBytes.Reverse();
            return new System.Net.IPAddress(endBytes.ToArray()).ToString();
        }

        private string CalculateEndIPFromCIDR(string startIP, int cidr)
        {
            var ip = System.Net.IPAddress.Parse(startIP);
            var bytes = ip.GetAddressBytes().ToList();
            bytes.Reverse();
            var hostBits = 32 - cidr;
            var hosts = (uint)Math.Pow(2, hostBits) - 1;
            var total = BitConverter.ToUInt32(bytes.ToArray(), 0) + hosts;
            var endBytes = BitConverter.GetBytes(total).ToList();
            endBytes.Reverse();
            return new System.Net.IPAddress(endBytes.ToArray()).ToString();
        }

        private int GetCIDRFromCount(int count)
        {
            return 32 - (int)Math.Log(count, 2);
        }

        private string CalculateSubnetMask(int cidr)
        {
            var mask = (uint)(0xFFFFFFFF << (32 - cidr));
            var bytes = BitConverter.GetBytes(mask).ToList();
            bytes.Reverse();
            return new System.Net.IPAddress(bytes.ToArray()).ToString();
        }

        private List<string> GetIranianISPs()
        {
            return new List<string>
            {
                "Iran Telecommunication Company",
                "Mobile Telecommunication Company of Iran",
                "Irancell",
                "Rightel",
                "Taliya",
                "Shatel",
                "ParsOnline",
                "AsiaTech",
                "HiWeb",
                "Irancell",
                "MTN Irancell",
                "RighTel",
                "Taliya Telecom",
                "Shatel Network",
                "Pars Data Communications",
                "Asia Technology Development",
                "HiWEB Telecommunications"
            };
        }

        private List<IPRangeInfo> GetKnownIranianISPRanges()
        {
            var ranges = new List<IPRangeInfo>();
            try
            {
                // بارگذاری داده‌ی رسمی از RIPE برای کل ایران
                using (var client = new HttpClient())
                {
                    var ripeResult = client.GetStringAsync("https://stat.ripe.net/data/country-resource-list/data.json?resource=IR").Result;
                    var json = Newtonsoft.Json.Linq.JObject.Parse(ripeResult);
                    foreach (var ipRange in json["data"]?["resources"]?["ipv4"] ?? new JArray())
                    {
                        ranges.Add(new IPRangeInfo
                            {
                                StartIP = ipRange.ToString().Split('/')[0],
                                CIDR = ipRange.ToString(),
                                EndIP = CalculateEndIPFromCIDR(ipRange.ToString().Split('/')[0], int.Parse(ipRange.ToString().Split('/')[1])),
                                Mask = CalculateSubnetMask(int.Parse(ipRange.ToString().Split('/')[1])),
                                ISP = "RIPE",
                                Source = "RIPE.Net",
                                Province = null,
                                City = null,
                                IsIPv6 = false
                            });
                    }
                    foreach (var ipRange in json["data"]?["resources"]?["ipv6"] ?? new JArray())
                    {
                        ranges.Add(new IPRangeInfo
                            {
                                StartIP = ipRange.ToString().Split('/')[0],
                                CIDR = ipRange.ToString(),
                                EndIP = "IPv6-EndUnkown",
                                Mask = null,
                                ISP = "RIPE",
                                Source = "RIPE.Net",
                                Province = null,
                                City = null,
                                IsIPv6 = true
                            });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch IP ranges from RIPE");
            }
            return ranges;
        }

        private async Task SaveToDatabase(string province, string city, List<IPRangeInfo> ranges)
        {
            try
            {
                using var connection = DatabaseManager.GetConnection();
                connection.Open();

                var sql = @"INSERT OR REPLACE INTO IPRanges 
                           (Province, City, StartIP, EndIP, CIDR, Mask, ISP, Source, IsIPv6, CreatedAt) 
                           VALUES (@Province, @City, @StartIP, @EndIP, @CIDR, @Mask, @ISP, @Source, @IsIPv6, @CreatedAt)";

                using var command = new System.Data.SQLite.SQLiteCommand(sql, connection);
                
                foreach (var range in ranges)
                {
                    command.Parameters.Clear();
                    command.Parameters.AddWithValue("@Province", province ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@City", city ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@StartIP", range.StartIP ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@EndIP", range.EndIP ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@CIDR", range.CIDR ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Mask", range.Mask ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@ISP", range.ISP ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Source", range.Source ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@IsIPv6", range.IsIPv6 ? 1 : 0);
                    command.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                    
                    await Task.Run(() => command.ExecuteNonQuery());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save IP ranges to database");
            }
        }
    }

    public class IPRangeInfo
    {
        public string StartIP { get; set; }
        public string EndIP { get; set; }
        public string CIDR { get; set; }
        public string Mask { get; set; }
        public string ISP { get; set; }
        public string Source { get; set; }
        public string Province { get; set; }
        public string City { get; set; }
        public bool IsIPv6 { get; set; }
    }
}

