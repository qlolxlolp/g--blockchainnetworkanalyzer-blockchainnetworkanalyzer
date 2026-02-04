using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BlockchainNetworkAnalyzer.Core.Models;

namespace BlockchainNetworkAnalyzer.Core.Tools
{
    /// <summary>
    /// Masscan - Fast port scanner
    /// </summary>
    public class MasscanTool : BaseScanTool
    {
        public override string Name => "Masscan";
        public override string Description => "Fast port scanner - can scan the entire internet in minutes";

        protected override string FindExecutable()
        {
            var paths = new[]
            {
                @"C:\Program Files\Masscan\masscan.exe",
                @"C:\Program Files (x86)\Masscan\masscan.exe",
                @"C:\Masscan\masscan.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Masscan", "masscan.exe")
            };

            foreach (var path in paths)
            {
                if (File.Exists(path))
                    return path;
            }

            return "masscan.exe";
        }

        protected override string BuildArguments(ScanConfiguration config, Dictionary<string, object> parameters)
        {
            var args = new List<string>();

            // IP ranges
            var ipManager = new IPManager();
            var ips = ipManager.GenerateIPs(config);
            
            if (config.SelectionMode == Models.IPSelectionMode.IPRange)
            {
                args.Add(config.StartIP);
                args.Add("-p");
                args.Add(string.Join(",", config.Ports.Distinct()));
            }
            else
            {
                args.Add(string.Join(" ", ips));
                args.Add("-p");
                args.Add(string.Join(",", config.Ports.Distinct()));
            }

            // Rate
            if (parameters.ContainsKey("Rate"))
            {
                args.Add($"--rate={parameters["Rate"]}");
            }
            else
            {
                args.Add("--rate=10000"); // Default 10k packets/second
            }

            // Ports
            args.Add("-p");
            args.Add(string.Join(",", config.Ports.Distinct()));

            // Output format
            args.Add("-oJ");
            args.Add("-"); // Output to stdout

            return string.Join(" ", args);
        }

        protected override List<IPResult> ParseOutput(string output, ScanConfiguration config)
        {
            var results = new List<IPResult>();
            
            if (string.IsNullOrEmpty(output))
                return results;

            // Masscan outputs JSON format
            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                if (line.Trim().StartsWith("{"))
                {
                    // Parse JSON output
                    // {"ip": "192.168.1.1", "timestamp": "1234567890", "ports": [{"port": 8332, "proto": "tcp", "status": "open"}]}
                    var ipMatch = Regex.Match(line, @"""ip""\s*:\s*""([^""]+)""");
                    var portMatch = Regex.Match(line, @"""port""\s*:\s*(\d+)");
                    
                    if (ipMatch.Success && portMatch.Success)
                    {
                        results.Add(new IPResult
                        {
                            IPAddress = ipMatch.Groups[1].Value,
                            Port = int.Parse(portMatch.Groups[1].Value),
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
                { "Rate", 10000 },
                { "Wait", 10 }
            };
        }
    }
}

