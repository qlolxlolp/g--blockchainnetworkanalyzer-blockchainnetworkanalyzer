using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using static BlockchainNetworkAnalyzer.App;

namespace BlockchainNetworkAnalyzer.Core
{
    public class BlockchainAnalyzer
    {
        private readonly ILogger<BlockchainAnalyzer> _logger;
        private readonly Dictionary<int, string> _blockchainPortMap;
        private readonly Dictionary<string, byte[]> _protocolSignatures;

        public BlockchainAnalyzer()
        {
            _logger = App.LoggerFactory.CreateLogger<BlockchainAnalyzer>();
            
            _blockchainPortMap = new Dictionary<int, string>
            {
                { 8332, "Bitcoin RPC" },
                { 8333, "Bitcoin P2P" },
                { 8545, "Ethereum JSON-RPC" },
                { 30303, "Ethereum P2P" },
                { 9332, "Litecoin RPC" },
                { 9333, "Litecoin P2P" },
                { 4444, "Stratum Mining" },
                { 3333, "Stratum Mining" },
                { 4028, "Stratum Mining" },
                { 7777, "Stratum Mining" },
                { 14433, "Stratum SSL" },
                { 14444, "Stratum SSL" },
                { 14455, "Stratum SSL" }
            };

            _protocolSignatures = new Dictionary<string, byte[]>
            {
                { "Bitcoin", new byte[] { 0xF9, 0xBE, 0xB4, 0xD9 } },
                { "Ethereum", Encoding.UTF8.GetBytes("eth") },
                { "Stratum", Encoding.UTF8.GetBytes("stratum") }
            };
        }

        public async Task<BlockchainAnalysisResult> AnalyzeBlockchainAsync(string ipAddress, int port, int timeout)
        {
            var result = new BlockchainAnalysisResult
            {
                IPAddress = ipAddress,
                Port = port,
                IsBlockchain = false
            };

            try
            {
                // Check if port is a known blockchain port
                if (_blockchainPortMap.ContainsKey(port))
                {
                    result.IsBlockchain = true;
                    result.Type = _blockchainPortMap[port];
                    result.Confidence = 0.7;
                }

                // Try to connect and analyze protocol
                var protocolResult = await AnalyzeProtocolAsync(ipAddress, port, timeout);
                if (protocolResult.Detected)
                {
                    result.IsBlockchain = true;
                    result.Type = protocolResult.BlockchainType;
                    result.Confidence = 0.9;
                    result.ProtocolDetails = protocolResult.Details;
                }

                // Additional analysis based on port and response
                if (result.IsBlockchain)
                {
                    result = await PerformDeepAnalysisAsync(ipAddress, port, timeout, result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Error analyzing blockchain for {ipAddress}:{port}");
            }

            return result;
        }

        private async Task<ProtocolAnalysisResult> AnalyzeProtocolAsync(string ipAddress, int port, int timeout)
        {
            var result = new ProtocolAnalysisResult();

            try
            {
                using var client = new TcpClient();
                var connectTask = client.ConnectAsync(ipAddress, port);
                var timeoutTask = Task.Delay(timeout);

                var completedTask = await Task.WhenAny(connectTask, timeoutTask);

                if (completedTask == connectTask && client.Connected)
                {
                    using var stream = client.GetStream();
                    stream.ReadTimeout = timeout;

                    // Try to read initial response
                    var buffer = new byte[4096];
                    var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

                    if (bytesRead > 0)
                    {
                        var response = Encoding.UTF8.GetString(buffer, 0, bytesRead).ToLowerInvariant();

                        // Check for Stratum protocol
                        if (response.Contains("stratum") || response.Contains("mining"))
                        {
                            result.Detected = true;
                            result.BlockchainType = "Stratum Mining Pool";
                            result.Details = ExtractStratumDetails(response);
                        }
                        // Check for Bitcoin RPC
                        else if (port == 8332 && response.Contains("bitcoin"))
                        {
                            result.Detected = true;
                            result.BlockchainType = "Bitcoin RPC";
                            result.Details = "Bitcoin JSON-RPC interface detected";
                        }
                        // Check for Ethereum JSON-RPC
                        else if (port == 8545 && (response.Contains("jsonrpc") || response.Contains("eth")))
                        {
                            result.Detected = true;
                            result.BlockchainType = "Ethereum JSON-RPC";
                            result.Details = "Ethereum JSON-RPC interface detected";
                        }
                        // Check protocol signatures
                        else
                        {
                            foreach (var signature in _protocolSignatures)
                            {
                                if (response.Contains(Encoding.UTF8.GetString(signature.Value).ToLowerInvariant()))
                                {
                                    result.Detected = true;
                                    result.BlockchainType = signature.Key;
                                    break;
                                }
                            }
                        }

                        // Check for common blockchain mining protocols
                        if (IsMiningProtocol(response))
                        {
                            result.Detected = true;
                            if (string.IsNullOrEmpty(result.BlockchainType))
                                result.BlockchainType = "Cryptocurrency Mining";
                            result.Details = "Mining protocol detected";
                        }
                    }

                    client.Close();
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, $"Protocol analysis failed for {ipAddress}:{port}");
            }

            return result;
        }

        private bool IsMiningProtocol(string response)
        {
            var miningKeywords = new[]
            {
                "mining.subscribe",
                "mining.authorize",
                "getwork",
                "getblocktemplate",
                "submitblock",
                "eth_submithashrate",
                "eth_submitwork",
                "eth_getwork"
            };

            foreach (var keyword in miningKeywords)
            {
                if (response.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private string ExtractStratumDetails(string response)
        {
            var details = new List<string>();

            if (response.Contains("version"))
                details.Add("Version negotiation detected");

            if (response.Contains("difficulty"))
                details.Add("Difficulty parameters present");

            if (response.Contains("extranonce"))
                details.Add("Extranonce support detected");

            return string.Join("; ", details);
        }

        private async Task<BlockchainAnalysisResult> PerformDeepAnalysisAsync(
            string ipAddress, int port, int timeout, BlockchainAnalysisResult initialResult)
        {
            // Perform additional analysis
            // This could include:
            // - Sending specific protocol messages
            // - Analyzing response patterns
            // - Checking for specific blockchain network features

            await Task.CompletedTask;
            return initialResult;
        }

        public List<int> GetBlockchainPorts()
        {
            var ports = App.Configuration.GetSection("Blockchain:DefaultPorts").Get<int[]>();
            return ports?.ToList() ?? new List<int>(_blockchainPortMap.Keys);
        }

        public bool IsBlockchainPort(int port)
        {
            return _blockchainPortMap.ContainsKey(port);
        }
    }

    public class BlockchainAnalysisResult
    {
        public string IPAddress { get; set; }
        public int Port { get; set; }
        public bool IsBlockchain { get; set; }
        public string Type { get; set; }
        public double Confidence { get; set; }
        public string ProtocolDetails { get; set; }
    }

    public class ProtocolAnalysisResult
    {
        public bool Detected { get; set; }
        public string BlockchainType { get; set; }
        public string Details { get; set; }
    }
}

