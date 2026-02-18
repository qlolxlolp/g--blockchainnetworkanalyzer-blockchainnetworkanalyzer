using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using IranianMinerDetector.WinForms.Models;
using IranianMinerDetector.WinForms.Data;

namespace IranianMinerDetector.WinForms.Services
{
    public class NetworkScanner
    {
        private readonly CancellationTokenSource _cts = new();
        private readonly SemaphoreSlim _semaphore;
        private readonly DatabaseManager _db = DatabaseManager.Instance;

        public event EventHandler<ScanProgress>? ProgressUpdated;
        public event EventHandler<HostRecord>? HostFound;
        public event EventHandler<string>? LogMessage;

        private static readonly Dictionary<int, string> KnownMiningPorts = new()
        {
            { 8332, "Bitcoin RPC" },
            { 8333, "Bitcoin P2P" },
            { 3333, "Bitcoin Stratum" },
            { 4028, "Stratum Protocol" },
            { 4444, "Ethereum/Generic Miner" },
            { 30303, "Ethereum P2P" },
            { 8545, "Ethereum RPC" },
            { 18081, "Monero P2P" },
            { 9332, "Litecoin RPC" },
            { 9333, "Litecoin P2P" },
            { 5050, "Alternative Stratum" },
            { 8888, "Generic Mining Pool" }
        };

        public bool IsScanning => !_cts.IsCancellationRequested;

        public NetworkScanner(int maxConcurrency = 100)
        {
            _semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
        }

        public async Task<ScanRecord> StartScanAsync(ScanConfiguration config)
        {
            var record = new ScanRecord
            {
                StartTime = DateTime.Now,
                Province = config.Province,
                City = config.City,
                ISP = config.ISP,
                TotalIPs = 0,
                ScannedIPs = 0,
                OnlineHosts = 0,
                MinersFound = 0,
                Status = ScanStatus.NotStarted,
                Configuration = System.Text.Json.JsonSerializer.Serialize(config)
            };

            record.Id = _db.CreateScanRecord(record);
            record.Status = ScanStatus.InProgress;
            _db.UpdateScanRecord(record);

            Log($"Starting scan with configuration: {config.Province}, {config.City}, {config.ISP}");

            try
            {
                var ipAddresses = GenerateIPAddresses(config);
                record.TotalIPs = ipAddresses.Count;
                _db.UpdateScanRecord(record);

                Log($"Generated {ipAddresses.Count} IP addresses to scan");

                var tasks = ipAddresses.Select(ip => ScanHostAsync(ip, config, record.Id));
                var results = await Task.WhenAll(tasks);

                record.ScannedIPs = results.Length;
                record.OnlineHosts = results.Count(r => r.IsOnline);
                record.MinersFound = results.Count(r => r.IsMinerDetected);
                record.EndTime = DateTime.Now;
                record.Status = ScanStatus.Completed;

                _db.UpdateScanRecord(record);
                Log($"Scan completed: {record.OnlineHosts}/{record.TotalIPs} online, {record.MinersFound} miners found");
            }
            catch (OperationCanceledException)
            {
                record.EndTime = DateTime.Now;
                record.Status = ScanStatus.Cancelled;
                _db.UpdateScanRecord(record);
                Log("Scan cancelled by user");
            }
            catch (Exception ex)
            {
                record.EndTime = DateTime.Now;
                record.Status = ScanStatus.Error;
                _db.UpdateScanRecord(record);
                Log($"Scan error: {ex.Message}");
            }

            return record;
        }

        public void CancelScan()
        {
            _cts.Cancel();
            Log("Cancelling scan...");
        }

        private List<string> GenerateIPAddresses(ScanConfiguration config)
        {
            var ips = new List<string>();

            if (!string.IsNullOrEmpty(config.IPRange))
            {
                // Parse CIDR or IP range
                try
                {
                    var parts = config.IPRange.Split('-');
                    if (parts.Length == 2)
                    {
                        // Range format: 192.168.1.1-192.168.1.100
                        var start = IPAddress.Parse(parts[0].Trim());
                        var end = IPAddress.Parse(parts[1].Trim());
                        ips.AddRange(GenerateIPRange(start, end));
                    }
                    else
                    {
                        // CIDR format: 192.168.1.0/24
                        var cidrParts = config.IPRange.Split('/');
                        if (cidrParts.Length == 2)
                        {
                            var baseIP = IPAddress.Parse(cidrParts[0]);
                            var prefixLength = int.Parse(cidrParts[1]);
                            ips.AddRange(GenerateCIDRRange(baseIP, prefixLength));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log($"Error parsing IP range: {ex.Message}");
                }
            }
            else if (!string.IsNullOrEmpty(config.ISP))
            {
                // Use ISP IP ranges
                var isp = IranianISPs.GetISPByName(config.ISP);
                if (isp != null)
                {
                    foreach (var range in isp.IPRanges.Take(5)) // Limit ranges for demo
                    {
                        try
                        {
                            var cidrParts = range.Split('/');
                            if (cidrParts.Length == 2)
                            {
                                var baseIP = IPAddress.Parse(cidrParts[0]);
                                var prefixLength = int.Parse(cidrParts[1]);
                                // Limit to smaller subnets for demo
                                if (prefixLength >= 24)
                                {
                                    ips.AddRange(GenerateCIDRRange(baseIP, prefixLength));
                                }
                            }
                        }
                        catch
                        {
                            // Skip invalid ranges
                        }
                    }
                }
            }
            else if (!string.IsNullOrEmpty(config.Province))
            {
                // Generate sample IPs based on province (simulated)
                var province = IranianGeography.GetProvinceByName(config.Province);
                if (province != null)
                {
                    // Generate some example IPs for the province
                    var baseIPBytes = new byte[] { 185, (byte)(province.Code % 256), 0, 0 };
                    var baseIP = new IPAddress(baseIPBytes);
                    ips.AddRange(GenerateCIDRRange(baseIP, 24));
                }
            }

            return ips.Take(1000).ToList(); // Limit to 1000 IPs for demo
        }

        private List<string> GenerateIPRange(IPAddress start, IPAddress end)
        {
            var ips = new List<string>();
            var startBytes = start.GetAddressBytes();
            var endBytes = end.GetAddressBytes();

            // Simple implementation for /24 or smaller ranges
            for (var i = 0; i < 256; i++)
            {
                startBytes[3] = (byte)i;
                var ip = new IPAddress(startBytes);
                ips.Add(ip.ToString());
                if (ip.Equals(end)) break;
            }

            return ips;
        }

        private List<string> GenerateCIDRRange(IPAddress baseIP, int prefixLength)
        {
            var ips = new List<string>();
            var bytes = baseIP.GetAddressBytes();

            if (prefixLength < 24)
            {
                // Limit to /24 subnets for demo
                var count = Math.Min(256, 1 << (32 - prefixLength));
                for (var i = 0; i < Math.Min(count, 256); i++)
                {
                    bytes[2] = (byte)i;
                    for (var j = 0; j < 256; j++)
                    {
                        bytes[3] = (byte)j;
                        ips.Add(new IPAddress(bytes).ToString());
                    }
                }
            }
            else if (prefixLength == 24)
            {
                for (var i = 0; i < 256; i++)
                {
                    bytes[3] = (byte)i;
                    ips.Add(new IPAddress(bytes).ToString());
                }
            }
            else
            {
                for (var i = 0; i < (1 << (32 - prefixLength)); i++)
                {
                    bytes[3] = (byte)i;
                    ips.Add(new IPAddress(bytes).ToString());
                }
            }

            return ips;
        }

        private async Task<HostRecord> ScanHostAsync(string ipAddress, ScanConfiguration config, int scanId)
        {
            await _semaphore.WaitAsync(_cts.Token);

            try
            {
                var record = new HostRecord
                {
                    ScanId = scanId,
                    IPAddress = ipAddress,
                    ScannedAt = DateTime.Now
                };

                // Ping check
                var ping = new Ping();
                var reply = await ping.SendPingAsync(ipAddress, config.TimeoutMs);

                record.IsOnline = reply.Status == IPStatus.Success;
                record.ResponseTimeMs = record.IsOnline ? (int)reply.RoundtripTime : null;

                if (record.IsOnline)
                {
                    // Port scan
                    var openPorts = new List<int>();
                    var portsToScan = config.CheckMiningPortsOnly
                        ? KnownMiningPorts.Keys.ToList()
                        : config.Ports;

                    foreach (var port in portsToScan)
                    {
                        if (await CheckPortAsync(ipAddress, port, config.TimeoutMs))
                        {
                            openPorts.Add(port);
                        }
                    }

                    record.OpenPorts = openPorts;

                    // Detect mining
                    if (openPorts.Any(p => KnownMiningPorts.ContainsKey(p)))
                    {
                        record.IsMinerDetected = true;
                        record.ConfidenceScore = 0.8;
                        record.DetectedService = "Mining Operation Detected";

                        if (config.PerformBannerGrab)
                        {
                            foreach (var port in openPorts.Where(p => KnownMiningPorts.ContainsKey(p)))
                            {
                                var banner = await GrabBannerAsync(ipAddress, port, config.TimeoutMs);
                                if (!string.IsNullOrEmpty(banner))
                                {
                                    record.Banner = banner;
                                    break;
                                }
                            }
                        }
                    }

                    // Geolocation lookup
                    if (config.UseGeolocation)
                    {
                        var geo = _db.GetCachedGeolocation(ipAddress);
                        if (geo != null)
                        {
                            record.ISP = geo.ISP;
                            record.Province = geo.Region;
                            record.City = geo.City;
                            record.Latitude = geo.Latitude;
                            record.Longitude = geo.Longitude;
                        }
                        else
                        {
                            // Identify ISP from local database
                            var isp = IranianISPs.IdentifyISP(ipAddress);
                            if (isp != null)
                            {
                                record.ISP = isp.Name;
                            }
                        }
                    }

                    // Save to database
                    _db.CreateHostRecord(record);

                    HostFound?.Invoke(this, record);
                    Log($"{ipAddress}: Online, Ports: {string.Join(",", openPorts)}, Miner: {record.IsMinerDetected}");
                }

                return record;
            }
            catch (Exception ex)
            {
                Log($"Error scanning {ipAddress}: {ex.Message}");
                return new HostRecord
                {
                    ScanId = scanId,
                    IPAddress = ipAddress,
                    IsOnline = false,
                    ScannedAt = DateTime.Now
                };
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task<bool> CheckPortAsync(string ipAddress, int port, int timeoutMs)
        {
            try
            {
                using var client = new TcpClient();
                var connectTask = client.ConnectAsync(ipAddress, port);
                var timeoutTask = Task.Delay(timeoutMs);

                var completedTask = await Task.WhenAny(connectTask, timeoutTask);

                if (completedTask == connectTask)
                {
                    client.Close();
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private async Task<string?> GrabBannerAsync(string ipAddress, int port, int timeoutMs)
        {
            try
            {
                using var client = new TcpClient();
                var connectTask = client.ConnectAsync(ipAddress, port);
                var timeoutTask = Task.Delay(timeoutMs);

                var completedTask = await Task.WhenAny(connectTask, timeoutTask);

                if (completedTask == connectTask)
                {
                    var stream = client.GetStream();
                    client.ReceiveTimeout = timeoutMs;

                    var buffer = new byte[1024];
                    var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    
                    if (bytesRead > 0)
                    {
                        return Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private void Log(string message)
        {
            LogMessage?.Invoke(this, message);
        }

        public void Dispose()
        {
            _cts.Dispose();
            _semaphore.Dispose();
        }
    }
}
