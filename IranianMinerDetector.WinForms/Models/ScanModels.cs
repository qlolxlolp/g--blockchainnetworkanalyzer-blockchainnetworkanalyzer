using System;
using System.Collections.Generic;

namespace IranianMinerDetector.WinForms.Models
{
    public class ScanRecord
    {
        public int Id { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? Province { get; set; }
        public string? City { get; set; }
        public string? ISP { get; set; }
        public int TotalIPs { get; set; }
        public int ScannedIPs { get; set; }
        public int OnlineHosts { get; set; }
        public int MinersFound { get; set; }
        public ScanStatus Status { get; set; }
        public string? Configuration { get; set; }
    }

    public enum ScanStatus
    {
        NotStarted,
        InProgress,
        Completed,
        Cancelled,
        Error
    }

    public class HostRecord
    {
        public int Id { get; set; }
        public int ScanId { get; set; }
        public string IPAddress { get; set; } = string.Empty;
        public bool IsOnline { get; set; }
        public int? ResponseTimeMs { get; set; }
        public List<int> OpenPorts { get; set; } = new List<int>();
        public bool IsMinerDetected { get; set; }
        public double ConfidenceScore { get; set; }
        public string? DetectedService { get; set; }
        public string? Banner { get; set; }
        public string? ISP { get; set; }
        public string? Province { get; set; }
        public string? City { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public DateTime ScannedAt { get; set; }
    }

    public class GeolocationData
    {
        public string IPAddress { get; set; } = string.Empty;
        public string? Country { get; set; }
        public string? Region { get; set; }
        public string? City { get; set; }
        public string? ISP { get; set; }
        public string? Organization { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public DateTime CachedAt { get; set; }
    }

    public class ScanConfiguration
    {
        public string? Province { get; set; }
        public string? City { get; set; }
        public string? ISP { get; set; }
        public string? IPRange { get; set; }
        public List<int> Ports { get; set; } = new List<int> { 8332, 8333, 30303, 3333, 4028, 4444, 18081, 8545, 9332, 9333 };
        public int TimeoutMs { get; set; } = 3000;
        public int MaxConcurrency { get; set; } = 100;
        public bool CheckMiningPortsOnly { get; set; } = false;
        public bool PerformBannerGrab { get; set; } = true;
        public bool UseGeolocation { get; set; } = true;
    }

    public class ScanProgress
    {
        public int CurrentIP { get; set; }
        public int TotalIPs { get; set; }
        public int OnlineHosts { get; set; }
        public int MinersFound { get; set; }
        public string CurrentStatus { get; set; } = string.Empty;
        public double PercentComplete => TotalIPs > 0 ? (double)CurrentIP / TotalIPs * 100 : 0;
    }
}
