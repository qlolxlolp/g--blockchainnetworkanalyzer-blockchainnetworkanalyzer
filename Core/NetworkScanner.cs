using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using BlockchainNetworkAnalyzer.Core.Models;
using static BlockchainNetworkAnalyzer.App;

namespace BlockchainNetworkAnalyzer.Core
{
    public class NetworkScanner
    {
        private readonly ILogger<NetworkScanner> _logger;
        private readonly SemaphoreSlim _semaphore;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly FakeIPDetector _fakeIPDetector;
        private readonly BlockchainAnalyzer _blockchainAnalyzer;

        public event EventHandler<ScanProgressEventArgs> ScanProgress;
        public event EventHandler<IPResult> IPResultFound;

        public NetworkScanner()
        {
            _logger = App.LoggerFactory.CreateLogger<NetworkScanner>();
            var maxConcurrent = int.Parse(App.Configuration["NetworkScanning:MaxConcurrentScans"] ?? "50");
            _semaphore = new SemaphoreSlim(maxConcurrent, maxConcurrent);
            _cancellationTokenSource = new CancellationTokenSource();
            _fakeIPDetector = new FakeIPDetector();
            _blockchainAnalyzer = new BlockchainAnalyzer();
        }

        public async Task<ScanResult> ScanAsync(ScanConfiguration config)
        {
            var scanResult = new ScanResult
            {
                ScanType = config.ScanName ?? "Network Scan",
                StartTime = DateTime.Now,
                Status = "Running"
            };

            try
            {
                DatabaseManager.LogAudit("Scan Started", details: $"Scan: {scanResult.ScanType}");

                // Generate IPs based on selection mode
                var ipManager = new IPManager();
                var ipsToScan = ipManager.GenerateIPs(config);
                scanResult.TotalIPs = ipsToScan.Count;

                // Get ports to scan
                var portsToScan = config.Ports.Any() 
                    ? config.Ports 
                    : GetDefaultBlockchainPorts();

                // Save scan to database
                var scanId = await SaveScanResultAsync(scanResult);

                // Scan each IP
                var tasks = new List<Task>();
                int scannedCount = 0;
                object lockObject = new object();

                foreach (var ip in ipsToScan)
                {
                    if (_cancellationTokenSource.Token.IsCancellationRequested)
                        break;

                    await _semaphore.WaitAsync(_cancellationTokenSource.Token);
                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            var hasOpenPort = await ScanIPAsync(ip, portsToScan, config, scanId);
                            
                            lock (lockObject)
                            {
                                scannedCount++;
                                scanResult.ScannedIPs = scannedCount;
                                if (hasOpenPort)
                                {
                                    scanResult.FoundHosts++;
                                }
                                OnScanProgress(new ScanProgressEventArgs
                                {
                                    Scanned = scannedCount,
                                    Total = scanResult.TotalIPs,
                                    Percentage = (scannedCount * 100.0) / scanResult.TotalIPs
                                });
                            }
                        }
                        finally
                        {
                            _semaphore.Release();
                        }
                    }, _cancellationTokenSource.Token));
                }

                await Task.WhenAll(tasks);

                scanResult.EndTime = DateTime.Now;
                scanResult.Status = "Completed";

                // Update scan result
                await UpdateScanResultAsync(scanId, scanResult);
                DatabaseManager.LogAudit("Scan Completed", details: $"Scan: {scanResult.ScanType}, Found: {scanResult.FoundHosts}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during network scan");
                scanResult.Status = "Error";
                scanResult.EndTime = DateTime.Now;
                throw;
            }

            return scanResult;
        }

        private async Task<bool> ScanIPAsync(string ipAddress, List<int> ports, ScanConfiguration config, long scanId)
        {
            try
            {
                // Basic connectivity check
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(ipAddress, config.Timeout);
                
                if (reply?.Status != IPStatus.Success)
                {
                    return false; // IP is not reachable
                }
                
                bool hasOpenPort = false;

                var ipResult = new IPResult
                {
                    ScanResultId = scanId,
                    IPAddress = ipAddress,
                    ResponseTime = (int)reply.RoundtripTime
                };

                // Fake IP detection
                if (config.EnableFakeIPDetection)
                {
                    var fakeDetection = await _fakeIPDetector.DetectFakeIPAsync(ipAddress);
                    ipResult.IsFakeIP = fakeDetection.IsFake;
                    ipResult.FakeIPReason = string.Join("; ", fakeDetection.Reasons);
                }

                // Port scanning
                if (config.EnablePortScanning)
                {
                    foreach (var port in ports)
                    {
                        var portResult = await ScanPortAsync(ipAddress, port, config.Timeout);
                        if (portResult.IsOpen)
                        {
                            hasOpenPort = true;
                            ipResult.Port = port;
                            ipResult.PortStatus = "Open";
                            
                            // Service detection
                            ipResult.Service = DetectService(port, portResult.Banner);
                            ipResult.Protocol = DetectProtocol(port);
                            break; // Found at least one open port
                        }
                    }
                }

                // Blockchain detection
                if (config.EnableBlockchainDetection && ipResult.Port.HasValue)
                {
                    var blockchainResult = await _blockchainAnalyzer.AnalyzeBlockchainAsync(
                        ipAddress, ipResult.Port.Value, config.Timeout);
                    
                    if (blockchainResult.IsBlockchain)
                    {
                        ipResult.BlockchainDetected = true;
                        ipResult.BlockchainType = blockchainResult.Type;
                    }
                }

                // Only save result if we found something or if fake IP was detected
                if (hasOpenPort || ipResult.IsFakeIP || ipResult.BlockchainDetected)
                {
                    await SaveIPResultAsync(ipResult);
                    OnIPResultFound(ipResult);
                }
                
                return hasOpenPort;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Error scanning IP {ipAddress}");
                return false;
            }
        }

        private async Task<PortScanResult> ScanPortAsync(string ipAddress, int port, int timeout)
        {
            var result = new PortScanResult { Port = port };

            try
            {
                using var client = new TcpClient();
                var connectTask = client.ConnectAsync(ipAddress, port);
                var timeoutTask = Task.Delay(timeout);

                var completedTask = await Task.WhenAny(connectTask, timeoutTask);

                if (completedTask == connectTask && client.Connected)
                {
                    result.IsOpen = true;
                    
                    // Try to get banner
                    try
                    {
                        using var stream = client.GetStream();
                        stream.ReadTimeout = 2000;
                        var buffer = new byte[1024];
                        var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                        if (bytesRead > 0)
                        {
                            result.Banner = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        }
                    }
                    catch
                    {
                        // Ignore banner read errors
                    }

                    client.Close();
                }
            }
            catch
            {
                result.IsOpen = false;
            }

            return result;
        }

        private string DetectService(int port, string banner)
        {
            // Service detection based on port and banner
            var serviceMap = new Dictionary<int, string>
            {
                { 8332, "Bitcoin RPC" },
                { 8333, "Bitcoin P2P" },
                { 8545, "Ethereum JSON-RPC" },
                { 30303, "Ethereum P2P" },
                { 4444, "Stratum" },
                { 3333, "Stratum" },
                { 4028, "Stratum" },
                { 7777, "Stratum" },
                { 9332, "Litecoin RPC" },
                { 9333, "Litecoin P2P" },
                { 14444, "Stratum SSL" },
                { 14433, "Stratum" }
            };

            if (serviceMap.ContainsKey(port))
                return serviceMap[port];

            // Try to detect from banner
            if (!string.IsNullOrEmpty(banner))
            {
                banner = banner.ToLowerInvariant();
                if (banner.Contains("stratum"))
                    return "Stratum Mining";
                if (banner.Contains("bitcoin"))
                    return "Bitcoin";
                if (banner.Contains("ethereum") || banner.Contains("eth"))
                    return "Ethereum";
            }

            return "Unknown";
        }

        private string DetectProtocol(int port)
        {
            var protocolMap = new Dictionary<int, string>
            {
                { 8332, "Bitcoin RPC" },
                { 8333, "Bitcoin P2P" },
                { 8545, "Ethereum JSON-RPC" },
                { 30303, "Ethereum P2P" },
                { 4444, "Stratum" },
                { 3333, "Stratum" },
                { 4028, "Stratum" }
            };

            return protocolMap.ContainsKey(port) ? protocolMap[port] : "TCP";
        }

        private List<int> GetDefaultBlockchainPorts()
        {
            var ports = App.Configuration.GetSection("Blockchain:DefaultPorts").Get<int[]>();
            return ports?.ToList() ?? new List<int> { 8332, 8333, 8545, 4444, 3333 };
        }

        private async Task<long> SaveScanResultAsync(ScanResult scanResult)
        {
            return await Task.Run(() =>
            {
                using var connection = DatabaseManager.GetConnection();
                connection.Open();
                var sql = @"INSERT INTO ScanResults 
                           (ScanType, StartTime, Status, TotalIPs, ScannedIPs, FoundHosts, Configuration) 
                           VALUES (@Type, @Start, @Status, @Total, @Scanned, @Found, @Config);
                           SELECT last_insert_rowid();";
                
                using var command = new SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue("@Type", scanResult.ScanType);
                command.Parameters.AddWithValue("@Start", scanResult.StartTime);
                command.Parameters.AddWithValue("@Status", scanResult.Status);
                command.Parameters.AddWithValue("@Total", scanResult.TotalIPs);
                command.Parameters.AddWithValue("@Scanned", scanResult.ScannedIPs);
                command.Parameters.AddWithValue("@Found", scanResult.FoundHosts);
                command.Parameters.AddWithValue("@Config", scanResult.Configuration ?? "");
                
                return Convert.ToInt64(command.ExecuteScalar());
            });
        }

        private async Task UpdateScanResultAsync(long scanId, ScanResult scanResult)
        {
            await Task.Run(() =>
            {
                using var connection = DatabaseManager.GetConnection();
                connection.Open();
                var sql = @"UPDATE ScanResults 
                           SET EndTime = @End, Status = @Status, ScannedIPs = @Scanned, FoundHosts = @Found 
                           WHERE Id = @Id";
                
                using var command = new SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue("@End", scanResult.EndTime);
                command.Parameters.AddWithValue("@Status", scanResult.Status);
                command.Parameters.AddWithValue("@Scanned", scanResult.ScannedIPs);
                command.Parameters.AddWithValue("@Found", scanResult.FoundHosts);
                command.Parameters.AddWithValue("@Id", scanId);
                
                command.ExecuteNonQuery();
            });
        }

        private async Task SaveIPResultAsync(IPResult ipResult)
        {
            await Task.Run(() =>
            {
                using var connection = DatabaseManager.GetConnection();
                connection.Open();
                var sql = @"INSERT INTO IPResults 
                           (ScanResultId, IPAddress, Port, PortStatus, Service, Protocol, ResponseTime, 
                            IsFakeIP, FakeIPReason, BlockchainDetected, BlockchainType, Geolocation, ISP, ASN) 
                           VALUES (@ScanId, @IP, @Port, @PortStatus, @Service, @Protocol, @ResponseTime, 
                                   @IsFake, @FakeReason, @Blockchain, @BlockchainType, @Geo, @ISP, @ASN)";
                
                using var command = new SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue("@ScanId", ipResult.ScanResultId);
                command.Parameters.AddWithValue("@IP", ipResult.IPAddress);
                command.Parameters.AddWithValue("@Port", (object)ipResult.Port ?? DBNull.Value);
                command.Parameters.AddWithValue("@PortStatus", ipResult.PortStatus ?? "");
                command.Parameters.AddWithValue("@Service", ipResult.Service ?? "");
                command.Parameters.AddWithValue("@Protocol", ipResult.Protocol ?? "");
                command.Parameters.AddWithValue("@ResponseTime", (object)ipResult.ResponseTime ?? DBNull.Value);
                command.Parameters.AddWithValue("@IsFake", ipResult.IsFakeIP ? 1 : 0);
                command.Parameters.AddWithValue("@FakeReason", ipResult.FakeIPReason ?? "");
                command.Parameters.AddWithValue("@Blockchain", ipResult.BlockchainDetected ? 1 : 0);
                command.Parameters.AddWithValue("@BlockchainType", ipResult.BlockchainType ?? "");
                command.Parameters.AddWithValue("@Geo", ipResult.Geolocation ?? "");
                command.Parameters.AddWithValue("@ISP", ipResult.ISP ?? "");
                command.Parameters.AddWithValue("@ASN", ipResult.ASN ?? "");
                
                command.ExecuteNonQuery();
            });
        }

        public void Cancel()
        {
            _cancellationTokenSource.Cancel();
        }

        protected virtual void OnScanProgress(ScanProgressEventArgs e)
        {
            ScanProgress?.Invoke(this, e);
        }

        protected virtual void OnIPResultFound(IPResult e)
        {
            IPResultFound?.Invoke(this, e);
        }
    }

    public class ScanProgressEventArgs : EventArgs
    {
        public int Scanned { get; set; }
        public int Total { get; set; }
        public double Percentage { get; set; }
    }

    public class PortScanResult
    {
        public int Port { get; set; }
        public bool IsOpen { get; set; }
        public string Banner { get; set; }
    }
}

