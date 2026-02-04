using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BlockchainNetworkAnalyzer.Core.Models;

namespace BlockchainNetworkAnalyzer.Core.Tools
{
    /// <summary>
    /// Netstat - Built-in Windows network statistics tool
    /// </summary>
    public class NetstatTool : BaseScanTool
    {
        public override string Name => "Netstat";
        public override string Description => "Windows built-in network statistics and connection monitoring tool";

        protected override string FindExecutable()
        {
            // Netstat is built into Windows
            var system32 = Environment.GetFolderPath(Environment.SpecialFolder.System);
            return Path.Combine(system32, "netstat.exe");
        }

        public override bool IsInstalled => File.Exists(ExecutablePath);

        protected override string BuildArguments(ScanConfiguration config, Dictionary<string, object> parameters)
        {
            var args = new List<string>();

            // Show all connections
            args.Add("-an");

            // Show process IDs
            args.Add("-o");

            // Show numerical addresses
            args.Add("-n");

            return string.Join(" ", args);
        }

        protected override List<IPResult> ParseOutput(string output, ScanConfiguration config)
        {
            var results = new List<IPResult>();
            var seenConnections = new HashSet<string>();
            
            if (string.IsNullOrEmpty(output))
                return results;

            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            
            // Blockchain ports
            var blockchainPorts = config.Ports.Any() ? config.Ports : new List<int> { 8332, 8333, 8545, 30303 };

            foreach (var line in lines)
            {
                // Match: TCP 192.168.1.1:8332 192.168.1.2:54321 ESTABLISHED
                var match = Regex.Match(line, 
                    @"(TCP|UDP)\s+([0-9.]+):(\d+)\s+([0-9.]+):(\d+)\s+(\w+)");
                
                if (match.Success)
                {
                    var protocol = match.Groups[1].Value;
                    var localIP = match.Groups[2].Value;
                    var localPort = int.Parse(match.Groups[3].Value);
                    var remoteIP = match.Groups[4].Value;
                    var remotePort = int.Parse(match.Groups[5].Value);
                    var state = match.Groups[6].Value;

                    // Check if port is a blockchain port
                    if (blockchainPorts.Contains(localPort) || blockchainPorts.Contains(remotePort))
                    {
                        var connectionKey = $"{remoteIP}:{remotePort}";
                        if (!seenConnections.Contains(connectionKey))
                        {
                            results.Add(new IPResult
                            {
                                IPAddress = remoteIP,
                                Port = remotePort,
                                PortStatus = state,
                                Protocol = protocol,
                                BlockchainDetected = true
                            });
                            seenConnections.Add(connectionKey);
                        }
                    }
                }
            }

            return results;
        }

        public override Dictionary<string, object> GetDefaultParameters()
        {
            return new Dictionary<string, object>
            {
                { "ShowProcesses", true },
                { "ShowNumerical", true }
            };
        }
    }
}

