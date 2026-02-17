using System;
using System.Collections.Generic;

namespace BlockchainNetworkAnalyzer.Core.Models
{
    public enum RuleType
    {
        PortCombination,
        BannerSignature,
        BehaviorPattern,
        TrafficAnalysis,
        Custom
    }

    public enum RuleSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    public class DetectionRule
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public RuleType Type { get; set; }
        public RuleSeverity Severity { get; set; }
        public double ConfidenceThreshold { get; set; } = 0.7;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastModified { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public List<RuleCondition> Conditions { get; set; } = new List<RuleCondition>();
        public List<RuleAction> Actions { get; set; } = new List<RuleAction>();
    }

    public class RuleCondition
    {
        public long Id { get; set; }
        public long RuleId { get; set; }
        public string Property { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public int OrderIndex { get; set; }
        public string LogicalOperator { get; set; } = "AND";
    }

    public class RuleAction
    {
        public long Id { get; set; }
        public long RuleId { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public string ActionValue { get; set; } = string.Empty;
        public int OrderIndex { get; set; }
    }

    public class DetectionResult
    {
        public long Id { get; set; }
        public long RuleId { get; set; }
        public long ScanResultId { get; set; }
        public long IPResultId { get; set; }
        public string IPAddress { get; set; } = string.Empty;
        public int? Port { get; set; }
        public bool IsDetected { get; set; }
        public double ConfidenceScore { get; set; }
        public string DetectionType { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public List<string> MatchedConditions { get; set; } = new List<string>();
        public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    }

    public class MinerSignature
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string MinerType { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public List<int> KnownPorts { get; set; } = new List<int>();
        public List<string> BannerSignatures { get; set; } = new List<string>();
        public List<string> HashAlgorithms { get; set; } = new List<string>();
        public string Protocol { get; set; } = string.Empty;
        public double ConfidenceWeight { get; set; } = 1.0;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
