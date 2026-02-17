using System;
using System.Collections.Generic;

namespace BlockchainNetworkAnalyzer.Core.Models
{
    public enum ScheduleFrequency
    {
        Once,
        Hourly,
        Daily,
        Weekly,
        Monthly,
        Custom
    }

    public enum ScheduleStatus
    {
        Pending,
        Running,
        Completed,
        Failed,
        Cancelled,
        Paused
    }

    public class ScheduledScan
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ScheduleFrequency Frequency { get; set; }
        public string CronExpression { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public TimeSpan? StartTime { get; set; }
        public ScheduleStatus Status { get; set; } = ScheduleStatus.Pending;
        public bool IsEnabled { get; set; } = true;
        public ScanConfiguration Configuration { get; set; } = new ScanConfiguration();
        public string TargetProvince { get; set; } = string.Empty;
        public List<string> TargetCities { get; set; } = new List<string>();
        public List<string> TargetISPs { get; set; } = new List<string>();
        public NotificationSettings Notifications { get; set; } = new NotificationSettings();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastRun { get; set; }
        public DateTime? NextRun { get; set; }
        public int RunCount { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class NotificationSettings
    {
        public bool EmailEnabled { get; set; }
        public string EmailAddress { get; set; } = string.Empty;
        public bool WebhookEnabled { get; set; }
        public string WebhookUrl { get; set; } = string.Empty;
        public bool OnCompletion { get; set; } = true;
        public bool OnDetection { get; set; } = true;
        public bool OnFailure { get; set; } = true;
        public bool IncludeReport { get; set; } = true;
    }

    public class ScheduleExecution
    {
        public long Id { get; set; }
        public long ScheduledScanId { get; set; }
        public long? ScanResultId { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public ScheduleStatus Status { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public int HostsFound { get; set; }
        public int MinersDetected { get; set; }
        public bool NotificationSent { get; set; }
    }

    public class ScanComparison
    {
        public long Id { get; set; }
        public long BaseScanId { get; set; }
        public long ComparisonScanId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int NewHosts { get; set; }
        public int RemovedHosts { get; set; }
        public int ChangedHosts { get; set; }
        public int NewMiners { get; set; }
        public int RemovedMiners { get; set; }
        public List<HostDifference> Differences { get; set; } = new List<HostDifference>();
    }

    public class HostDifference
    {
        public long Id { get; set; }
        public long ComparisonId { get; set; }
        public string IPAddress { get; set; } = string.Empty;
        public DifferenceType Type { get; set; }
        public string PropertyName { get; set; } = string.Empty;
        public string OldValue { get; set; } = string.Empty;
        public string NewValue { get; set; } = string.Empty;
    }

    public enum DifferenceType
    {
        Added,
        Removed,
        Changed
    }
}
