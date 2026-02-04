using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Win32;
using BlockchainNetworkAnalyzer.Core.Models;

namespace BlockchainNetworkAnalyzer.Core.Tools
{
    /// <summary>
    /// Nmap network scanning tool integration
    /// </summary>
    public class NmapTool : BaseScanTool
    {
        public override string Name => "Nmap";
        public override string Description => "Network Mapper - Advanced network discovery and security auditing tool";

        protected override string FindExecutable()
        {
            // Check common installation paths
            var paths = new[]
            {
                @"C:\Program Files (x86)\Nmap\nmap.exe",
                @"C:\Program Files\Nmap\nmap.exe",
                @"C:\Nmap\nmap.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Nmap", "nmap.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Nmap", "nmap.exe")
            };

            foreach (var path in paths)
            {
                if (File.Exists(path))
                    return path;
            }

            // Check PATH environment variable
            var pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (!string.IsNullOrEmpty(pathEnv))
            {
                foreach (var dir in pathEnv.Split(Path.PathSeparator))
                {
                    var fullPath = Path.Combine(dir, "nmap.exe");
                    if (File.Exists(fullPath))
                        return fullPath;
                }
            }

            return "nmap.exe"; // Fallback to PATH
        }

        protected override string BuildArguments(ScanConfiguration config, Dictionary<string, object> parameters)
        {
            var args = new List<string>();

            // IP addresses/ranges
            var ipManager = new IPManager();
            var ips = ipManager.GenerateIPs(config);
            if (ips.Count == 1)
            {
                args.Add(ips[0]);
            }
            else
            {
                args.Add(string.Join(" ", ips));
            }

            // Port scanning
            if (config.EnablePortScanning && config.Ports.Any())
            {
                var ports = string.Join(",", config.Ports.Distinct());
                args.Add($"-p {ports}");
            }
            else if (config.EnablePortScanning)
            {
                args.Add("-p-"); // Scan all ports
            }

            // Scan options
            args.Add("-sS"); // TCP SYN scan
            args.Add("-T4"); // Aggressive timing
            args.Add("--open"); // Only show open ports
            args.Add("-Pn"); // Skip host discovery
            args.Add("--host-timeout"); args.Add($"{config.Timeout / 1000}s");
            
            // Service detection
            if (parameters.ContainsKey("ServiceDetection") && (bool)parameters["ServiceDetection"])
            {
                args.Add("-sV"); // Version detection
            }

            // OS detection
            if (parameters.ContainsKey("OSDetection") && (bool)parameters["OSDetection"])
            {
                args.Add("-O");
            }

            // Script scanning
            if (parameters.ContainsKey("ScriptScan") && (bool)parameters["ScriptScan"])
            {
                args.Add("-sC");
            }

            // Output format
            args.Add("--stats-every"); args.Add("5s");
            args.Add("-v"); // Verbose

            return string.Join(" ", args);
        }

        protected override List<IPResult> ParseOutput(string output, ScanConfiguration config)
        {
            var results = new List<IPResult>();
            
            if (string.IsNullOrEmpty(output))
                return results;

            // Parse Nmap XML or text output
            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            IPResult currentResult = null;

            foreach (var line in lines)
            {
                // Match: Nmap scan report for 192.168.1.1
                var ipMatch = Regex.Match(line, @"Nmap scan report for (?:.*\()?([0-9.]+)");
                if (ipMatch.Success)
                {
                    if (currentResult != null)
                    {
                        results.Add(currentResult);
                    }
                    currentResult = new IPResult
                    {
                        IPAddress = ipMatch.Groups[1].Value,
                        ScanResultId = 0
                    };
                    continue;
                }

                if (currentResult == null) continue;

                // Match: PORT STATE SERVICE VERSION
                // 8332/tcp open  bitcoin
                var portMatch = Regex.Match(line, @"(\d+)/(\w+)\s+(\w+)\s+(.+)");
                if (portMatch.Success)
                {
                    var port = int.Parse(portMatch.Groups[1].Value);
                    var state = portMatch.Groups[3].Value;
                    
                    if (state == "open")
                    {
                        currentResult.Port = port;
                        currentResult.PortStatus = "Open";
                        
                        var serviceInfo = portMatch.Groups[4].Value;
                        var parts = serviceInfo.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length > 0)
                        {
                            currentResult.Service = parts[0];
                            if (parts.Length > 1)
                            {
                                currentResult.Protocol = parts[1];
                            }
                        }
                    }
                }

                // Match version info
                var versionMatch = Regex.Match(line, @"Service Info: (.+)");
                if (versionMatch.Success)
                {
                    currentResult.Service = versionMatch.Groups[1].Value;
                }
            }

            if (currentResult != null)
            {
                results.Add(currentResult);
            }

            return results;
        }

        public override Dictionary<string, object> GetDefaultParameters()
        {
            return new Dictionary<string, object>
            {
                { "ServiceDetection", true },
                { "OSDetection", false },
                { "ScriptScan", false },
                { "ScanSpeed", "T4" },
                { "ScanType", "SYN" }
            };
        }

        public override async Task<bool> CheckInstallationAsync()
        {
            var installed = await base.CheckInstallationAsync();
            if (installed)
            {
                // Test execution
                try
                {
                    var result = await ExecuteAsync("--version");
                    installed = result.Success && result.Output.Contains("Nmap");
                }
                catch
                {
                    installed = false;
                }
            }
            return installed;
        }
    }
}

