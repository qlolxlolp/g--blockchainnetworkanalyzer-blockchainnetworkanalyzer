using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BlockchainNetworkAnalyzer.Core.Models;
using static BlockchainNetworkAnalyzer.App;

namespace BlockchainNetworkAnalyzer.Core.Services
{
    public class DetectionEngine
    {
        private readonly ILogger<DetectionEngine> _logger;
        private readonly List<DetectionRule> _rules;
        private readonly List<MinerSignature> _signatures;

        public DetectionEngine()
        {
            _logger = App.LoggerFactory.CreateLogger<DetectionEngine>();
            _rules = LoadDefaultRules();
            _signatures = LoadDefaultSignatures();
        }

        public async Task<DetectionResult> AnalyzeAsync(IPResult ipResult, ScanConfiguration config)
        {
            var result = new DetectionResult
            {
                IPAddress = ipResult.IPAddress,
                Port = ipResult.Port,
                ScanResultId = ipResult.ScanResultId,
                IPResultId = ipResult.Id
            };

            try
            {
                var scores = new List<double>();
                var matchedConditions = new List<string>();

                // Check against miner signatures
                var signatureResult = CheckMinerSignatures(ipResult);
                if (signatureResult.IsMatch)
                {
                    scores.Add(signatureResult.Confidence);
                    matchedConditions.Add($"Signature: {signatureResult.SignatureName}");
                    result.DetectionType = signatureResult.MinerType;
                }

                // Check against detection rules
                foreach (var rule in _rules.Where(r => r.IsActive))
                {
                    var ruleResult = await EvaluateRuleAsync(rule, ipResult);
                    if (ruleResult.IsMatch)
                    {
                        scores.Add(ruleResult.Confidence * rule.ConfidenceThreshold);
                        matchedConditions.Add($"Rule: {rule.Name}");
                        
                        if (string.IsNullOrEmpty(result.DetectionType))
                        {
                            result.DetectionType = rule.Name;
                        }
                    }
                }

                // Calculate final confidence
                if (scores.Any())
                {
                    result.IsDetected = true;
                    result.ConfidenceScore = Math.Min(scores.Average() * (1 + (scores.Count - 1) * 0.1), 1.0);
                    result.MatchedConditions = matchedConditions;
                    result.Details = string.Join("; ", matchedConditions);
                }

                // Save detection result
                await SaveDetectionResultAsync(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error analyzing IP {ipResult.IPAddress} for detection");
            }

            return result;
        }

        private SignatureMatchResult CheckMinerSignatures(IPResult ipResult)
        {
            var result = new SignatureMatchResult { IsMatch = false };

            foreach (var signature in _signatures.Where(s => s.IsActive))
            {
                double confidence = 0;
                var matches = new List<string>();

                // Check port match
                if (ipResult.Port.HasValue && signature.KnownPorts.Contains(ipResult.Port.Value))
                {
                    confidence += 0.3;
                    matches.Add($"Port {ipResult.Port}");
                }

                // Check service match
                if (!string.IsNullOrEmpty(ipResult.Service))
                {
                    var service = ipResult.Service.ToLowerInvariant();
                    if (signature.BannerSignatures.Any(s => service.Contains(s.ToLowerInvariant())))
                    {
                        confidence += 0.4;
                        matches.Add("Service signature");
                    }
                }

                // Check blockchain detection
                if (ipResult.BlockchainDetected)
                {
                    confidence += 0.3;
                    matches.Add("Blockchain protocol");
                }

                if (confidence >= 0.5)
                {
                    result.IsMatch = true;
                    result.Confidence = confidence;
                    result.SignatureName = signature.Name;
                    result.MinerType = signature.MinerType;
                    result.Matches = matches;
                    break;
                }
            }

            return result;
        }

        private async Task<RuleEvaluationResult> EvaluateRuleAsync(DetectionRule rule, IPResult ipResult)
        {
            var result = new RuleEvaluationResult { IsMatch = false };
            var conditionResults = new List<bool>();

            foreach (var condition in rule.Conditions.OrderBy(c => c.OrderIndex))
            {
                bool conditionMet = EvaluateCondition(condition, ipResult);
                conditionResults.Add(conditionMet);
            }

            // Evaluate based on logical operators (simplified - assumes AND for now)
            if (conditionResults.All(r => r))
            {
                result.IsMatch = true;
                result.Confidence = 0.8;
            }

            await Task.CompletedTask;
            return result;
        }

        private bool EvaluateCondition(RuleCondition condition, IPResult ipResult)
        {
            var propertyValue = GetPropertyValue(ipResult, condition.Property);
            if (propertyValue == null) return false;

            return condition.Operator.ToLowerInvariant() switch
            {
                "equals" => propertyValue.Equals(condition.Value, StringComparison.OrdinalIgnoreCase),
                "contains" => propertyValue.Contains(condition.Value, StringComparison.OrdinalIgnoreCase),
                "startswith" => propertyValue.StartsWith(condition.Value, StringComparison.OrdinalIgnoreCase),
                "endswith" => propertyValue.EndsWith(condition.Value, StringComparison.OrdinalIgnoreCase),
                "regex" => Regex.IsMatch(propertyValue, condition.Value),
                "greaterthan" => CompareNumeric(propertyValue, condition.Value) > 0,
                "lessthan" => CompareNumeric(propertyValue, condition.Value) < 0,
                "true" => bool.TryParse(propertyValue, out var boolValue) && boolValue,
                "false" => bool.TryParse(propertyValue, out var boolValue) && !boolValue,
                _ => false
            };
        }

        private string GetPropertyValue(IPResult ipResult, string propertyName)
        {
            return propertyName.ToLowerInvariant() switch
            {
                "ipaddress" => ipResult.IPAddress,
                "port" => ipResult.Port?.ToString() ?? "",
                "portstatus" => ipResult.PortStatus,
                "service" => ipResult.Service,
                "protocol" => ipResult.Protocol,
                "isfakeip" => ipResult.IsFakeIP.ToString(),
                "fakeipreason" => ipResult.FakeIPReason,
                "blockchaindetected" => ipResult.BlockchainDetected.ToString(),
                "blockchaintype" => ipResult.BlockchainType,
                "geolocation" => ipResult.Geolocation,
                "isp" => ipResult.ISP,
                "asn" => ipResult.ASN,
                _ => ""
            };
        }

        private int CompareNumeric(string value1, string value2)
        {
            if (int.TryParse(value1, out var num1) && int.TryParse(value2, out var num2))
            {
                return num1.CompareTo(num2);
            }
            return string.Compare(value1, value2, StringComparison.Ordinal);
        }

        private List<DetectionRule> LoadDefaultRules()
        {
            return new List<DetectionRule>
            {
                new DetectionRule
                {
                    Id = 1,
                    Name = "Stratum Mining Detection",
                    Description = "Detects Stratum mining protocol usage",
                    Type = RuleType.PortCombination,
                    Severity = RuleSeverity.High,
                    ConfidenceThreshold = 0.8,
                    Conditions = new List<RuleCondition>
                    {
                        new RuleCondition { Property = "Service", Operator = "contains", Value = "Stratum", OrderIndex = 1 },
                        new RuleCondition { Property = "Port", Operator = "equals", Value = "3333", OrderIndex = 2 }
                    }
                },
                new DetectionRule
                {
                    Id = 2,
                    Name = "Bitcoin RPC Exposure",
                    Description = "Detects exposed Bitcoin RPC interfaces",
                    Type = RuleType.PortCombination,
                    Severity = RuleSeverity.Medium,
                    ConfidenceThreshold = 0.7,
                    Conditions = new List<RuleCondition>
                    {
                        new RuleCondition { Property = "Port", Operator = "equals", Value = "8332", OrderIndex = 1 },
                        new RuleCondition { Property = "BlockchainDetected", Operator = "equals", Value = "True", OrderIndex = 2 }
                    }
                },
                new DetectionRule
                {
                    Id = 3,
                    Name = "Suspicious Mining Ports",
                    Description = "Detects common cryptocurrency mining ports",
                    Type = RuleType.PortCombination,
                    Severity = RuleSeverity.Medium,
                    ConfidenceThreshold = 0.6,
                    Conditions = new List<RuleCondition>
                    {
                        new RuleCondition { Property = "Port", Operator = "regex", Value = "^(3333|4444|4028|7777|14433|14444)$", OrderIndex = 1 }
                    }
                },
                new DetectionRule
                {
                    Id = 4,
                    Name = "Fake IP with Mining Activity",
                    Description = "Detects mining activity from known fake IPs",
                    Type = RuleType.BehaviorPattern,
                    Severity = RuleSeverity.Critical,
                    ConfidenceThreshold = 0.9,
                    Conditions = new List<RuleCondition>
                    {
                        new RuleCondition { Property = "IsFakeIP", Operator = "equals", Value = "True", OrderIndex = 1 },
                        new RuleCondition { Property = "BlockchainDetected", Operator = "equals", Value = "True", OrderIndex = 2 }
                    }
                }
            };
        }

        private List<MinerSignature> LoadDefaultSignatures()
        {
            return new List<MinerSignature>
            {
                new MinerSignature
                {
                    Id = 1,
                    Name = "CGMiner",
                    MinerType = "ASIC Mining",
                    KnownPorts = new List<int> { 4028, 3333 },
                    BannerSignatures = new List<string> { "cgminer", "pool" },
                    HashAlgorithms = new List<string> { "SHA-256", "Scrypt" },
                    Protocol = "Stratum"
                },
                new MinerSignature
                {
                    Id = 2,
                    Name = "BFGMiner",
                    MinerType = "GPU/ASIC Mining",
                    KnownPorts = new List<int> { 3333, 4444 },
                    BannerSignatures = new List<string> { "bfgminer", "stratum" },
                    HashAlgorithms = new List<string> { "SHA-256", "Scrypt" },
                    Protocol = "Stratum"
                },
                new MinerSignature
                {
                    Id = 3,
                    Name = "Ethminer",
                    MinerType = "GPU Mining",
                    KnownPorts = new List<int> { 8545, 30303 },
                    BannerSignatures = new List<string> { "ethminer", "ethereum" },
                    HashAlgorithms = new List<string> { "Ethash" },
                    Protocol = "Ethereum"
                },
                new MinerSignature
                {
                    Id = 4,
                    Name = "XMRig",
                    MinerType = "CPU/GPU Mining",
                    KnownPorts = new List<int> { 3333, 8080, 7777 },
                    BannerSignatures = new List<string> { "xmrig", "monero" },
                    HashAlgorithms = new List<string> { "RandomX", "CryptoNight" },
                    Protocol = "Stratum"
                }
            };
        }

        private async Task SaveDetectionResultAsync(DetectionResult result)
        {
            try
            {
                using var connection = DatabaseManager.GetConnection();
                connection.Open();

                var sql = @"INSERT INTO DetectionResults 
                           (RuleId, ScanResultId, IPResultId, IPAddress, Port, IsDetected, 
                            ConfidenceScore, DetectionType, Details, DetectedAt) 
                           VALUES (@RuleId, @ScanResultId, @IPResultId, @IP, @Port, @IsDetected, 
                                   @Confidence, @Type, @Details, @DetectedAt)";

                using var command = new System.Data.SQLite.SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue("@RuleId", result.RuleId);
                command.Parameters.AddWithValue("@ScanResultId", result.ScanResultId);
                command.Parameters.AddWithValue("@IPResultId", result.IPResultId);
                command.Parameters.AddWithValue("@IP", result.IPAddress);
                command.Parameters.AddWithValue("@Port", (object)result.Port ?? DBNull.Value);
                command.Parameters.AddWithValue("@IsDetected", result.IsDetected ? 1 : 0);
                command.Parameters.AddWithValue("@Confidence", result.ConfidenceScore);
                command.Parameters.AddWithValue("@Type", result.DetectionType);
                command.Parameters.AddWithValue("@Details", result.Details);
                command.Parameters.AddWithValue("@DetectedAt", result.DetectedAt);

                await Task.Run(() => command.ExecuteNonQuery());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save detection result");
            }
        }

        public List<DetectionRule> GetRules() => _rules;
        public List<MinerSignature> GetSignatures() => _signatures;

        public void AddRule(DetectionRule rule)
        {
            rule.Id = _rules.Max(r => r.Id) + 1;
            rule.CreatedAt = DateTime.UtcNow;
            _rules.Add(rule);
        }

        public void UpdateRule(DetectionRule rule)
        {
            var existing = _rules.FirstOrDefault(r => r.Id == rule.Id);
            if (existing != null)
            {
                var index = _rules.IndexOf(existing);
                rule.LastModified = DateTime.UtcNow;
                _rules[index] = rule;
            }
        }

        public void RemoveRule(long ruleId)
        {
            _rules.RemoveAll(r => r.Id == ruleId);
        }
    }

    public class SignatureMatchResult
    {
        public bool IsMatch { get; set; }
        public double Confidence { get; set; }
        public string SignatureName { get; set; } = string.Empty;
        public string MinerType { get; set; } = string.Empty;
        public List<string> Matches { get; set; } = new List<string>();
    }

    public class RuleEvaluationResult
    {
        public bool IsMatch { get; set; }
        public double Confidence { get; set; }
        public List<string> MatchedConditions { get; set; } = new List<string>();
    }
}
