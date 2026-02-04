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
    /// Service for analyzing and identifying internet connection types and subscriber information
    /// </summary>
    public class InternetConnectionService
    {
        private readonly ILogger<InternetConnectionService> _logger;
        private readonly HttpClient _httpClient;
        private readonly ISPService _ispService;

        public InternetConnectionService()
        {
            _logger = App.LoggerFactory.CreateLogger<InternetConnectionService>();
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
            _ispService = new ISPService();
        }

        public async Task<InternetConnectionInfo> AnalyzeConnectionAsync(string ipAddress)
        {
            var connectionInfo = new InternetConnectionInfo
            {
                IPAddress = ipAddress,
                AnalyzedAt = DateTime.Now
            };

            try
            {
                // Get IP information from multiple sources
                var ipInfoTasks = new List<Task>
                {
                    AnalyzeIPInfoAsync(ipAddress, connectionInfo),
                    AnalyzeISPAsync(ipAddress, connectionInfo),
                    AnalyzeConnectionTypeAsync(ipAddress, connectionInfo),
                    AnalyzeSubscriberInfoAsync(ipAddress, connectionInfo)
                };

                await Task.WhenAll(ipInfoTasks);

                // Determine connection type
                connectionInfo.ConnectionType = DetermineConnectionType(connectionInfo);

                _logger.LogInformation($"Analyzed connection for {ipAddress}: {connectionInfo.ConnectionType}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error analyzing connection for {ipAddress}");
            }

            return connectionInfo;
        }

        private async Task AnalyzeIPInfoAsync(string ipAddress, InternetConnectionInfo info)
        {
            try
            {
                var url = $"https://ipinfo.io/{ipAddress}/json";
                var token = App.Configuration["Geolocation:IPInfoToken"] ?? "";
                if (!string.IsNullOrEmpty(token))
                {
                    url += $"?token={token}";
                }

                var response = await _httpClient.GetStringAsync(url);
                var data = JObject.Parse(response);

                info.ISP = data["org"]?.ToString();
                info.City = data["city"]?.ToString();
                info.Province = data["region"]?.ToString();
                info.Country = data["country"]?.ToString();
                info.ASN = data["org"]?.ToString();

                // Determine if it's residential or commercial
                var org = info.ISP?.ToLowerInvariant() ?? "";
                if (org.Contains("residential") || org.Contains("home") || org.Contains("household"))
                {
                    info.IsResidential = true;
                }
                else if (org.Contains("datacenter") || org.Contains("hosting") || org.Contains("server"))
                {
                    info.IsCommercial = true;
                    info.IsDatacenter = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to analyze IP info");
            }
        }

        private async Task AnalyzeISPAsync(string ipAddress, InternetConnectionInfo info)
        {
            try
            {
                var url = $"https://ip-api.com/json/{ipAddress}?fields=isp,org,as,asname,mobile,proxy,hosting";
                var response = await _httpClient.GetStringAsync(url);
                var data = JObject.Parse(response);

                info.ISP = data["isp"]?.ToString() ?? info.ISP;
                info.Organization = data["org"]?.ToString();
                info.ASN = data["as"]?.ToString();
                info.ASName = data["asname"]?.ToString();
                info.IsMobile = data["mobile"]?.ToObject<bool>() ?? false;
                info.IsProxy = data["proxy"]?.ToObject<bool>() ?? false;
                info.IsHosting = data["hosting"]?.ToObject<bool>() ?? false;

                // Detect Iranian ISPs
                var isp = info.ISP?.ToLowerInvariant() ?? "";
                if (isp.Contains("iran") || isp.Contains("mci") || isp.Contains("mtn"))
                {
                    info.ISPCountry = "Iran";
                    info.IsIranianISP = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to analyze ISP");
            }
        }

        private async Task AnalyzeConnectionTypeAsync(string ipAddress, InternetConnectionInfo info)
        {
            try
            {
                // Check connection type indicators
                var org = info.ISP?.ToLowerInvariant() ?? "";
                var asn = info.ASN?.ToLowerInvariant() ?? "";

                // Fiber indicators
                if (org.Contains("fiber") || org.Contains("ftth") || org.Contains("fttx") || 
                    org.Contains("فیبر") || org.Contains("فایبر"))
                {
                    info.HasFiberOptic = true;
                }

                // DSL/ADSL indicators
                if (org.Contains("dsl") || org.Contains("adsl") || org.Contains("vdsl"))
                {
                    info.HasDSL = true;
                }

                // Mobile indicators
                if (info.IsMobile || org.Contains("mobile") || org.Contains("cellular") ||
                    org.Contains("موبایل") || org.Contains("همراه"))
                {
                    info.IsMobileConnection = true;
                }

                // Satellite indicators
                if (org.Contains("satellite") || org.Contains("sat") || org.Contains("ماهواره"))
                {
                    info.IsSatellite = true;
                }

                // Fixed wireless
                if (org.Contains("wireless") && !info.IsMobileConnection)
                {
                    info.IsFixedWireless = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to analyze connection type");
            }
        }

        private async Task AnalyzeSubscriberInfoAsync(string ipAddress, InternetConnectionInfo info)
        {
            try
            {
                // This would require access to ISP databases or APIs
                // For Iranian ISPs, we can query internal databases
                
                var subscriberInfo = await GetIranianSubscriberInfoAsync(ipAddress);
                if (subscriberInfo != null)
                {
                    info.SubscriberInfo = subscriberInfo;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to analyze subscriber info");
            }
        }

        private async Task<SubscriberInfo> GetIranianSubscriberInfoAsync(string ipAddress)
        {
            // This would query Iranian ISP databases
            // For legal/compliance reasons, this should be done through official APIs
            
            await Task.CompletedTask;
            
            // Placeholder - in production, this would query actual ISP databases
            return new SubscriberInfo
            {
                // Information would be populated from ISP database queries
            };
        }

        private InternetConnectionType DetermineConnectionType(InternetConnectionInfo info)
        {
            if (info.IsMobileConnection)
            {
                return InternetConnectionType.Mobile;
            }
            else if (info.HasFiberOptic)
            {
                return InternetConnectionType.FiberOptic;
            }
            else if (info.HasDSL)
            {
                return InternetConnectionType.DSL;
            }
            else if (info.IsSatellite)
            {
                return InternetConnectionType.Satellite;
            }
            else if (info.IsFixedWireless)
            {
                return InternetConnectionType.FixedWireless;
            }
            else if (info.IsResidential)
            {
                return InternetConnectionType.Residential;
            }
            else if (info.IsCommercial)
            {
                return InternetConnectionType.Commercial;
            }

            return InternetConnectionType.Unknown;
        }
    }

    public class InternetConnectionInfo
    {
        public string IPAddress { get; set; }
        public string ISP { get; set; }
        public string Organization { get; set; }
        public string ASN { get; set; }
        public string ASName { get; set; }
        public string City { get; set; }
        public string Province { get; set; }
        public string Country { get; set; }
        public string ISPCountry { get; set; }
        public bool IsIranianISP { get; set; }

        // Connection Type Indicators
        public bool IsMobile { get; set; }
        public bool IsMobileConnection { get; set; }
        public bool HasFiberOptic { get; set; }
        public bool HasDSL { get; set; }
        public bool IsSatellite { get; set; }
        public bool IsFixedWireless { get; set; }
        public bool IsResidential { get; set; }
        public bool IsCommercial { get; set; }
        public bool IsDatacenter { get; set; }
        public bool IsHosting { get; set; }
        public bool IsProxy { get; set; }

        public InternetConnectionType ConnectionType { get; set; }
        public SubscriberInfo SubscriberInfo { get; set; }
        public DateTime AnalyzedAt { get; set; }
    }

    public enum InternetConnectionType
    {
        Unknown,
        Mobile,
        FiberOptic,
        DSL,
        Satellite,
        FixedWireless,
        Residential,
        Commercial,
        Datacenter
    }

    public class SubscriberInfo
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName { get; set; }
        public string NationalID { get; set; }
        public string PhoneNumber { get; set; }
        public string LandlineNumber { get; set; }
        public string MobileNumber { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string PostalCode { get; set; }
        public string Province { get; set; }
        public string City { get; set; }
        public string SubscriptionType { get; set; }
        public DateTime SubscriptionDate { get; set; }
        public string AccountNumber { get; set; }
        public bool IsActive { get; set; }
    }
}

