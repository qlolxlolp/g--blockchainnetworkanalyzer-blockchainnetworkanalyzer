using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BlockchainNetworkAnalyzer.Core.Models;

namespace BlockchainNetworkAnalyzer.Core.Tools
{
    /// <summary>
    /// Wireshark/TShark network packet analysis tool integration
    /// </summary>
    public class WiresharkTool : BaseScanTool
    {
        public override string Name => "Wireshark/TShark";
        public override string Description => "Network protocol analyzer using TShark (command-line version of Wireshark)";

        protected override string FindExecutable()
        {
            // TShark is the command-line version
            var paths = new[]
            {
                @"C:\Program Files\Wireshark\tshark.exe",
                @"C:\Program Files (x86)\Wireshark\tshark.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Wireshark", "tshark.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Wireshark", "tshark.exe")
            };

            foreach (var path in paths)
            {
                if (File.Exists(path))
                    return path;
            }

            return "tshark.exe";
        }

        protected override string BuildArguments(ScanConfiguration config, Dictionary<string, object> parameters)
        {
            var args = new List<string>();

            // Capture interface
            if (parameters.ContainsKey("Interface"))
            {
                args.Add("-i");
                args.Add(parameters["Interface"].ToString());
            }
            else
            {
                args.Add("-i"); args.Add("any"); // Capture on all interfaces
            }

            // Filter
            var ipManager = new IPManager();
            var ips = ipManager.GenerateIPs(config);
            
            if (ips.Count > 0)
            {
                var filter = string.Join(" or ", ips.Select(ip => $"ip.addr == {ip}"));
                
                // Add port filter
                if (config.Ports.Any())
                {
                    var ports = string.Join(" or ", config.Ports.Select(p => $"tcp.port == {p}"));
                    filter = $"({filter}) and ({ports})";
                }

                args.Add("-f");
                args.Add($"\"{filter}\"");
            }

            // Capture duration
            if (parameters.ContainsKey("Duration"))
            {
                args.Add("-a");
                args.Add($"duration:{(int)parameters["Duration"]}");
            }
            else
            {
                args.Add("-a"); args.Add("duration:60"); // Default 60 seconds
            }

            // Output format
            args.Add("-T");
            args.Add("fields");
            
            var fields = new[] { "-e", "ip.src", "-e", "ip.dst", "-e", "tcp.port", "-e", "frame.protocols" };
            args.AddRange(fields);

            // Display filter for blockchain protocols
            args.Add("-Y");
            args.Add("\"tcp.port == 8332 || tcp.port == 8333 || tcp.port == 8545 || tcp.port == 30303\"");

            return string.Join(" ", args);
        }

        protected override List<IPResult> ParseOutput(string output, ScanConfiguration config)
        {
            var results = new List<IPResult>();
            
            if (string.IsNullOrEmpty(output))
                return results;

            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var seenIPs = new HashSet<string>();

            foreach (var line in lines)
            {
                var parts = line.Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 4) continue;

                var srcIP = parts[0].Trim();
                var dstIP = parts[1].Trim();
                var port = parts[2].Trim();
                var protocols = parts[3].Trim().ToLowerInvariant();

                // Check if blockchain related
                if (protocols.Contains("bitcoin") || protocols.Contains("ethereum") || 
                    protocols.Contains("stratum") || protocols.Contains("mining"))
                {
                    if (!seenIPs.Contains(srcIP))
                    {
                        results.Add(new IPResult
                        {
                            IPAddress = srcIP,
                            Port = int.TryParse(port, out var p) ? p : null,
                            BlockchainDetected = true,
                            Protocol = protocols
                        });
                        seenIPs.Add(srcIP);
                    }
                }
            }

            return results;
        }

        public override Dictionary<string, object> GetDefaultParameters()
        {
            return new Dictionary<string, object>
            {
                { "Interface", "any" },
                { "Duration", 60 },
                { "PacketCount", 1000 },
                { "OutputFormat", "fields" }
            };
        }
    }
}

