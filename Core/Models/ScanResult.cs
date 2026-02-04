using System;
using System.Collections.Generic;

namespace BlockchainNetworkAnalyzer.Core.Models
{
    public class ScanResult
    {
        public long Id { get; set; }
        public string ScanType { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public int TotalIPs { get; set; }
        public int ScannedIPs { get; set; }
        public int FoundHosts { get; set; }
        public string Configuration { get; set; } = string.Empty;
        public List<IPResult> IPResults { get; set; } = new List<IPResult>();
    }

    public class IPResult
    {
        public long Id { get; set; }
        public long ScanResultId { get; set; }
        public string IPAddress { get; set; } = string.Empty;
        public int? Port { get; set; }
        public string PortStatus { get; set; } = string.Empty;
        public string Service { get; set; } = string.Empty;
        public string Protocol { get; set; } = string.Empty;
        public int? ResponseTime { get; set; }
        public bool IsFakeIP { get; set; }
        public string FakeIPReason { get; set; } = string.Empty;
        public bool BlockchainDetected { get; set; }
        public string BlockchainType { get; set; } = string.Empty;
        public string Geolocation { get; set; } = string.Empty;
        public string ISP { get; set; } = string.Empty;
        public string ASN { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

