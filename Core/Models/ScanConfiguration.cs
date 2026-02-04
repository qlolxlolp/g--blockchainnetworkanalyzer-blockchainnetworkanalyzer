using System.Collections.Generic;
using BlockchainNetworkAnalyzer.Core.Models;

namespace BlockchainNetworkAnalyzer.Core.Models
{
    public class ScanConfiguration
    {
        public IPSelectionMode SelectionMode { get; set; }
        public string StartIP { get; set; } = string.Empty;
        public string EndIP { get; set; } = string.Empty;
        public List<string> CustomIPs { get; set; } = new List<string>();
        public int RandomIPCount { get; set; } = 100;
        public List<int> Ports { get; set; } = new List<int>();
        public int Timeout { get; set; } = 3000;
        public int MaxConcurrent { get; set; } = 50;
        public bool EnableFakeIPDetection { get; set; } = true;
        public bool EnableBlockchainDetection { get; set; } = true;
        public bool EnablePortScanning { get; set; } = true;
        public string ScanName { get; set; } = string.Empty;
    }
}

