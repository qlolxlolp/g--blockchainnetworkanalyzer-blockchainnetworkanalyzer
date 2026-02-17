using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static BlockchainNetworkAnalyzer.App;

namespace BlockchainNetworkAnalyzer.Core.Services
{
    /// <summary>
    /// Enhanced ISP Service with comprehensive Iranian ISP data management
    /// </summary>
    public class EnhancedISPService
    {
        private readonly ILogger<EnhancedISPService> _logger;
        private readonly string _ispDataPath;
        private List<IranianISP> _cachedISPs;

        public EnhancedISPService()
        {
            _logger = App.LoggerFactory.CreateLogger<EnhancedISPService>();
            _ispDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "iranian_isps.json");
            LoadISPData();
        }

        private void LoadISPData()
        {
            try
            {
                if (File.Exists(_ispDataPath))
                {
                    var json = File.ReadAllText(_ispDataPath);
                    var data = JsonConvert.DeserializeObject<IranianISPData>(json);
                    _cachedISPs = data?.ISPs ?? new List<IranianISP>();
                    _logger.LogInformation($"Loaded {_cachedISPs.Count} Iranian ISPs from database");
                }
                else
                {
                    _cachedISPs = GetDefaultISPs();
                    _logger.LogWarning("ISP data file not found, using default ISP list");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading ISP data");
                _cachedISPs = GetDefaultISPs();
            }
        }

        public List<IranianISP> GetAllISPs()
        {
            return _cachedISPs;
        }

        public IranianISP GetISPByName(string name)
        {
            return _cachedISPs.FirstOrDefault(isp => 
                isp.Name.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                isp.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
        }

        public IranianISP GetISPByASN(string asn)
        {
            return _cachedISPs.FirstOrDefault(isp => 
                isp.ASN?.Equals(asn, StringComparison.OrdinalIgnoreCase) == true);
        }

        public List<IranianISP> GetISPsByProvince(string province)
        {
            return _cachedISPs.Where(isp => 
                isp.Coverage.Any(c => c.Contains(province, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        public List<IranianISP> GetISPsByType(string type)
        {
            return _cachedISPs.Where(isp => 
                isp.Type?.Equals(type, StringComparison.OrdinalIgnoreCase) == true)
                .ToList();
        }

        public IranianISP LookupIP(string ipAddress)
        {
            if (!IPAddress.TryParse(ipAddress, out _))
                return null;

            foreach (var isp in _cachedISPs)
            {
                foreach (var cidr in isp.IPRanges)
                {
                    if (IsIPInRange(ipAddress, cidr))
                    {
                        return isp;
                    }
                }
            }

            return null;
        }

        public List<string> GetAllIPRanges()
        {
            return _cachedISPs.SelectMany(isp => isp.IPRanges).ToList();
        }

        public List<string> GetIPRangesByISP(string ispName)
        {
            var isp = GetISPByName(ispName);
            return isp?.IPRanges ?? new List<string>();
        }

        public List<string> GetIPRangesByProvince(string province)
        {
            var isps = GetISPsByProvince(province);
            return isps.SelectMany(isp => isp.IPRanges).ToList();
        }

        public async Task<bool> AddISPAsync(IranianISP isp)
        {
            try
            {
                if (_cachedISPs.Any(i => i.Name.Equals(isp.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogWarning($"ISP {isp.Name} already exists");
                    return false;
                }

                _cachedISPs.Add(isp);
                await SaveISPDataAsync();
                _logger.LogInformation($"Added ISP: {isp.Name}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding ISP {isp.Name}");
                return false;
            }
        }

        public async Task<bool> UpdateISPAsync(IranianISP isp)
        {
            try
            {
                var existing = _cachedISPs.FirstOrDefault(i => i.Name.Equals(isp.Name, StringComparison.OrdinalIgnoreCase));
                if (existing == null)
                {
                    _logger.LogWarning($"ISP {isp.Name} not found for update");
                    return false;
                }

                var index = _cachedISPs.IndexOf(existing);
                _cachedISPs[index] = isp;
                await SaveISPDataAsync();
                _logger.LogInformation($"Updated ISP: {isp.Name}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating ISP {isp.Name}");
                return false;
            }
        }

        public async Task<bool> RemoveISPAsync(string name)
        {
            try
            {
                var existing = _cachedISPs.FirstOrDefault(i => i.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (existing == null)
                {
                    return false;
                }

                _cachedISPs.Remove(existing);
                await SaveISPDataAsync();
                _logger.LogInformation($"Removed ISP: {name}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error removing ISP {name}");
                return false;
            }
        }

        public async Task ImportFromRIPEAsync()
        {
            try
            {
                _logger.LogInformation("Importing IP ranges from RIPE NCC database");
                
                using var client = new System.Net.Http.HttpClient();
                client.Timeout = TimeSpan.FromSeconds(30);
                
                var response = await client.GetStringAsync("https://stat.ripe.net/data/country-resource-list/data.json?resource=IR");
                var data = JObject.Parse(response);

                var ipRanges = new List<string>();
                
                if (data["data"]?["resources"]?["ipv4"] != null)
                {
                    foreach (var range in data["data"]["resources"]["ipv4"])
                    {
                        ipRanges.Add(range.ToString());
                    }
                }

                _logger.LogInformation($"Imported {ipRanges.Count} IP ranges from RIPE");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing from RIPE");
            }
        }

        public ISPStatistics GetISPStatistics(string ispName)
        {
            var isp = GetISPByName(ispName);
            if (isp == null) return null;

            return new ISPStatistics
            {
                ISPName = isp.Name,
                ASN = isp.ASN,
                TotalIPRanges = isp.IPRanges.Count,
                EstimatedIPs = isp.IPRanges.Sum(CalculateIPsInRange),
                Coverage = isp.Coverage.Count
            };
        }

        public List<ISPStatistics> GetAllISPStatistics()
        {
            return _cachedISPs.Select(isp => GetISPStatistics(isp.Name)).ToList();
        }

        private async Task SaveISPDataAsync()
        {
            try
            {
                var data = new IranianISPData
                {
                    ISPs = _cachedISPs,
                    Metadata = new ISPMetadata
                    {
                        LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                        Source = "User Updated",
                        Description = "Iranian ISP IP Range Database for Network Miner Detection System"
                    }
                };

                var json = JsonConvert.SerializeObject(data, Formatting.Indented);
                await File.WriteAllTextAsync(_ispDataPath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving ISP data");
            }
        }

        private bool IsIPInRange(string ipAddress, string cidr)
        {
            try
            {
                var parts = cidr.Split('/');
                if (parts.Length != 2) return false;

                var baseIP = parts[0];
                var prefix = int.Parse(parts[1]);

                var ip = IPAddress.Parse(ipAddress).GetAddressBytes();
                var baseIpBytes = IPAddress.Parse(baseIP).GetAddressBytes();

                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(ip);
                    Array.Reverse(baseIpBytes);
                }

                var ipInt = BitConverter.ToUInt32(ip, 0);
                var baseInt = BitConverter.ToUInt32(baseIpBytes, 0);

                var mask = (uint)(0xFFFFFFFF << (32 - prefix));

                return (ipInt & mask) == (baseInt & mask);
            }
            catch
            {
                return false;
            }
        }

        private int CalculateIPsInRange(string cidr)
        {
            try
            {
                var parts = cidr.Split('/');
                if (parts.Length != 2) return 0;

                var prefix = int.Parse(parts[1]);
                return (int)Math.Pow(2, 32 - prefix);
            }
            catch
            {
                return 0;
            }
        }

        private List<IranianISP> GetDefaultISPs()
        {
            return new List<IranianISP>
            {
                new IranianISP
                {
                    Name = "Iran Telecommunication Company",
                    ASN = "AS58224",
                    Type = "Government",
                    Website = "www.tci.ir",
                    Coverage = new List<string> { "All Provinces" },
                    IPRanges = new List<string> { "2.176.0.0/12", "5.52.0.0/14", "31.56.0.0/14" }
                },
                new IranianISP
                {
                    Name = "Irancell",
                    ASN = "AS44244",
                    Type = "Mobile",
                    Website = "www.irancell.ir",
                    Coverage = new List<string> { "All Provinces" },
                    IPRanges = new List<string> { "5.1.8.0/21", "37.254.0.0/16", "91.98.0.0/16" }
                },
                new IranianISP
                {
                    Name = "RighTel",
                    ASN = "AS57218",
                    Type = "Mobile",
                    Website = "www.rightel.ir",
                    Coverage = new List<string> { "Tehran", "Isfahan", "Mashhad", "Shiraz", "Tabriz" },
                    IPRanges = new List<string> { "5.22.192.0/21", "79.127.0.0/17", "188.229.0.0/17" }
                },
                new IranianISP
                {
                    Name = "Shatel",
                    ASN = "AS31549",
                    Type = "DSL/FTTH",
                    Website = "www.shatel.ir",
                    Coverage = new List<string> { "Tehran", "Isfahan", "Mashhad", "Shiraz", "Tabriz", "Karaj", "Ahvaz" },
                    IPRanges = new List<string> { "85.185.0.0/16", "80.191.0.0/16", "85.198.0.0/16" }
                },
                new IranianISP
                {
                    Name = "Pars Online",
                    ASN = "AS16322",
                    Type = "ISP/Datacenter",
                    Website = "www.parsonline.com",
                    Coverage = new List<string> { "Tehran", "Isfahan", "Shiraz", "Tabriz" },
                    IPRanges = new List<string> { "46.100.0.0/16", "46.143.0.0/16", "77.104.64.0/18" }
                }
            };
        }
    }

    public class IranianISP
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("asn")]
        public string ASN { get; set; }

        [JsonProperty("website")]
        public string Website { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("coverage")]
        public List<string> Coverage { get; set; } = new List<string>();

        [JsonProperty("ipRanges")]
        public List<string> IPRanges { get; set; } = new List<string>();
    }

    public class IranianISPData
    {
        [JsonProperty("isps")]
        public List<IranianISP> ISPs { get; set; }

        [JsonProperty("metadata")]
        public ISPMetadata Metadata { get; set; }
    }

    public class ISPMetadata
    {
        [JsonProperty("lastUpdated")]
        public string LastUpdated { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
    }

    public class ISPStatistics
    {
        public string ISPName { get; set; }
        public string ASN { get; set; }
        public int TotalIPRanges { get; set; }
        public int EstimatedIPs { get; set; }
        public int Coverage { get; set; }
    }
}
