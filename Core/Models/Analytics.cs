using System;
using System.Collections.Generic;

namespace BlockchainNetworkAnalyzer.Core.Models
{
    public class ScanStatistics
    {
        public long Id { get; set; }
        public DateTime Date { get; set; }
        public int TotalScans { get; set; }
        public int TotalIPsScanned { get; set; }
        public int TotalHostsFound { get; set; }
        public int TotalMinersDetected { get; set; }
        public int TotalFakeIPs { get; set; }
        public double AverageScanTime { get; set; }
        public double SuccessRate { get; set; }
        public string Province { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string ISP { get; set; } = string.Empty;
    }

    public class MinerTrend
    {
        public long Id { get; set; }
        public DateTime Date { get; set; }
        public string MinerType { get; set; } = string.Empty;
        public int Count { get; set; }
        public double GrowthRate { get; set; }
        public string Province { get; set; } = string.Empty;
        public string ISP { get; set; } = string.Empty;
    }

    public class GeographicDistribution
    {
        public long Id { get; set; }
        public string Province { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public int TotalMiners { get; set; }
        public int ActiveMiners { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime LastUpdated { get; set; }
        public List<string> CommonISPs { get; set; } = new List<string>();
        public List<string> CommonMinerTypes { get; set; } = new List<string>();
    }

    public class ISPStatistics
    {
        public long Id { get; set; }
        public string ISPName { get; set; } = string.Empty;
        public string ASN { get; set; } = string.Empty;
        public int TotalMiners { get; set; }
        public int UniqueIPs { get; set; }
        public double RiskScore { get; set; }
        public DateTime LastUpdated { get; set; }
        public List<string> CommonProvinces { get; set; } = new List<string>();
        public List<string> CommonMinerTypes { get; set; } = new List<string>();
    }

    public class RiskAssessment
    {
        public long Id { get; set; }
        public string IPAddress { get; set; } = string.Empty;
        public double RiskScore { get; set; }
        public RiskLevel Level { get; set; }
        public List<string> RiskFactors { get; set; } = new List<string>();
        public string Province { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string ISP { get; set; } = string.Empty;
        public DateTime AssessedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastSeen { get; set; }
    }

    public enum RiskLevel
    {
        None,
        Low,
        Medium,
        High,
        Critical
    }

    public class AnomalyDetection
    {
        public long Id { get; set; }
        public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
        public AnomalyType Type { get; set; }
        public string Description { get; set; } = string.Empty;
        public string IPAddress { get; set; } = string.Empty;
        public double SeverityScore { get; set; }
        public bool IsResolved { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string Resolution { get; set; } = string.Empty;
    }

    public enum AnomalyType
    {
        SuddenMinerAppearance,
        UnusualTrafficPattern,
        SuspiciousPortActivity,
        FakeIPCluster,
        GeographicAnomaly,
        TemporalAnomaly
    }

    public class DashboardSummary
    {
        public int TotalScans { get; set; }
        public int TotalMinersDetected { get; set; }
        public int ActiveAlerts { get; set; }
        public int ScheduledScans { get; set; }
        public List<MinerTrend> TopMinerTypes { get; set; } = new List<MinerTrend>();
        public List<GeographicDistribution> TopProvinces { get; set; } = new List<GeographicDistribution>();
        public List<RiskAssessment> HighRiskHosts { get; set; } = new List<RiskAssessment>();
        public List<AnomalyDetection> RecentAnomalies { get; set; } = new List<AnomalyDetection>();
    }
}
