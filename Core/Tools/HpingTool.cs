using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BlockchainNetworkAnalyzer.Core.Models;

namespace BlockchainNetworkAnalyzer.Core.Tools
{
    /// <summary>
    /// Hping3 - Network tool for packet crafting and testing
    /// </summary>
    public class HpingTool : BaseScanTool
    {
        public override string Name => "Hping3";
        public override string Description => "Network tool for packet crafting, testing, and firewall testing";

        protected override string FindExecutable()
        {
            var paths = new[]
            {
                @"C:\Program Files\Hping\hping3.exe",
                @"C:\Hping\hping3.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Hping", "hping3.exe")
            };

            foreach (var path in paths)
            {
                if (File.Exists(path))
                    return path;
            }

            return "hping3.exe";
        }

        protected override string BuildArguments(ScanConfiguration config, Dictionary<string, object> parameters)
        {
            var args = new List<string>();

            // Scan mode
            args.Add("-S"); // SYN scan
            args.Add("-p"); // Port
            
            // Ports
            if (config.Ports.Any())
            {
                args.Add(string.Join(",", config.Ports.Distinct()));
            }

            // Target IP
            if (config.SelectionMode == Models.IPSelectionMode.SingleIP)
            {
                args.Add(config.StartIP);
            }

            // Count
            args.Add("-c");
            args.Add("1");

            // Timeout
            args.Add("--timeout");
            args.Add((config.Timeout / 1000).ToString());

            return string.Join(" ", args);
        }

        protected override List<IPResult> ParseOutput(string output, ScanConfiguration config)
        {
            var results = new List<IPResult>();
            
            if (string.IsNullOrEmpty(output))
                return results;

            // Parse hping output
            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                // Match: len=46 ip=192.168.1.1 flags=SA
                var match = Regex.Match(line, @"ip=([0-9.]+).*flags=([A-Z]+)");
                if (match.Success)
                {
                    var ip = match.Groups[1].Value;
                    var flags = match.Groups[2].Value;
                    
                    if (flags.Contains("SA") || flags.Contains("A"))
                    {
                        results.Add(new IPResult
                        {
                            IPAddress = ip,
                            PortStatus = "Open"
                        });
                    }
                }
            }

            return results;
        }

        public override Dictionary<string, object> GetDefaultParameters()
        {
            return new Dictionary<string, object>
            {
                { "ScanType", "SYN" },
                { "PacketCount", 1 }
            };
        }
    }
}

