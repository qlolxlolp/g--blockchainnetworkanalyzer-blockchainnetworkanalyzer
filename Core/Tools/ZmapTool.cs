using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BlockchainNetworkAnalyzer.Core.Models;

namespace BlockchainNetworkAnalyzer.Core.Tools
{
    /// <summary>
    /// Zmap - Fast network scanner for internet-wide surveys
    /// </summary>
    public class ZmapTool : BaseScanTool
    {
        public override string Name => "Zmap";
        public override string Description => "Fast single-packet network scanner designed for internet-wide network surveys";

        protected override string FindExecutable()
        {
            var paths = new[]
            {
                @"C:\Program Files\Zmap\zmap.exe",
                @"C:\Zmap\zmap.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Zmap", "zmap.exe")
            };

            foreach (var path in paths)
            {
                if (File.Exists(path))
                    return path;
            }

            return "zmap.exe";
        }

        protected override string BuildArguments(ScanConfiguration config, Dictionary<string, object> parameters)
        {
            var args = new List<string>();

            // Target IP ranges
            if (config.SelectionMode == Models.IPSelectionMode.IPRange)
            {
                args.Add("-p");
                args.Add(string.Join(",", config.Ports.Distinct()));
                args.Add("-o");
                args.Add("-"); // Output to stdout
            }

            // Ports
            if (config.Ports.Any())
            {
                args.Add("-p");
                args.Add(string.Join(",", config.Ports.Distinct()));
            }

            // Rate
            if (parameters.ContainsKey("Rate"))
            {
                args.Add($"--rate={parameters["Rate"]}");
            }

            return string.Join(" ", args);
        }

        protected override List<IPResult> ParseOutput(string output, ScanConfiguration config)
        {
            var results = new List<IPResult>();
            
            if (string.IsNullOrEmpty(output))
                return results;

            // Zmap outputs IP addresses line by line
            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                var ip = line.Trim();
                if (System.Net.IPAddress.TryParse(ip, out _))
                {
                    results.Add(new IPResult
                    {
                        IPAddress = ip,
                        PortStatus = "Open"
                    });
                }
            }

            return results;
        }

        public override Dictionary<string, object> GetDefaultParameters()
        {
            return new Dictionary<string, object>
            {
                { "Rate", 100000 }
            };
        }
    }
}

