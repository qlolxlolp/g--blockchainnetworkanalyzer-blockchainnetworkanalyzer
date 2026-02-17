using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BlockchainNetworkAnalyzer.Core.Models;
using static BlockchainNetworkAnalyzer.App;

namespace BlockchainNetworkAnalyzer.Core.Services
{
    public class AnalyticsService
    {
        private readonly ILogger<AnalyticsService> _logger;

        public AnalyticsService()
        {
            _logger = App.LoggerFactory.CreateLogger<AnalyticsService>();
        }

        public async Task<DashboardSummary> GetDashboardSummaryAsync()
        {
            var summary = new DashboardSummary();

            try
            {
                using var connection = DatabaseManager.GetConnection();
                connection.Open();

                // Total scans
                summary.TotalScans = await GetTotalScansAsync(connection);
                
                // Total miners detected
                summary.TotalMinersDetected = await GetTotalMinersDetectedAsync(connection);
                
                // Scheduled scans
                summary.ScheduledScans = await GetScheduledScanCountAsync(connection);
                
                // Top miner types
                summary.TopMinerTypes = await GetTopMinerTypesAsync(connection, 5);
                
                // Top provinces
                summary.TopProvinces = await GetTopProvincesAsync(connection, 5);
                
                // High risk hosts
                summary.HighRiskHosts = await GetHighRiskHostsAsync(connection, 10);
                
                // Recent anomalies
                summary.RecentAnomalies = await GetRecentAnomaliesAsync(connection, 10);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard summary");
            }

            return summary;
        }

        public async Task<List<ScanStatistics>> GetScanStatisticsAsync(DateTime startDate, DateTime endDate, string province = null, string isp = null)
        {
            var stats = new List<ScanStatistics>();

            try
            {
                using var connection = DatabaseManager.GetConnection();
                connection.Open();

                var sql = @"
                    SELECT 
                        DATE(sr.StartTime) as Date,
                        COUNT(*) as TotalScans,
                        SUM(sr.TotalIPs) as TotalIPs,
                        SUM(sr.FoundHosts) as HostsFound,
                        AVG(CAST((julianday(sr.EndTime) - julianday(sr.StartTime)) * 24 * 60 * 60 AS INTEGER)) as AvgDuration
                    FROM ScanResults sr
                    WHERE DATE(sr.StartTime) BETWEEN @StartDate AND @EndDate
                    AND sr.Status = 'Completed'
                    GROUP BY DATE(sr.StartTime)
                    ORDER BY Date DESC";

                using var command = new SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue("@StartDate", startDate.ToString("yyyy-MM-dd"));
                command.Parameters.AddWithValue("@EndDate", endDate.ToString("yyyy-MM-dd"));

                using var reader = await Task.Run(() => command.ExecuteReader());
                while (reader.Read())
                {
                    stats.Add(new ScanStatistics
                    {
                        Date = reader.GetDateTime(0),
                        TotalScans = reader.GetInt32(1),
                        TotalIPsScanned = reader.GetInt32(2),
                        TotalHostsFound = reader.GetInt32(3),
                        AverageScanTime = reader.IsDBNull(4) ? 0 : reader.GetDouble(4),
                        Province = province ?? "All",
                        ISP = isp ?? "All"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting scan statistics");
            }

            return stats;
        }

        public async Task<List<MinerTrend>> GetMinerTrendsAsync(DateTime startDate, DateTime endDate, string interval = "day")
        {
            var trends = new List<MinerTrend>();

            try
            {
                using var connection = DatabaseManager.GetConnection();
                connection.Open();

                var sql = @"
                    SELECT 
                        DATE(ip.CreatedAt) as Date,
                        ip.BlockchainType,
                        COUNT(*) as Count
                    FROM IPResults ip
                    JOIN ScanResults sr ON ip.ScanResultId = sr.Id
                    WHERE ip.BlockchainDetected = 1
                    AND DATE(ip.CreatedAt) BETWEEN @StartDate AND @EndDate
                    GROUP BY DATE(ip.CreatedAt), ip.BlockchainType
                    ORDER BY Date DESC";

                using var command = new SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue("@StartDate", startDate.ToString("yyyy-MM-dd"));
                command.Parameters.AddWithValue("@EndDate", endDate.ToString("yyyy-MM-dd"));

                using var reader = await Task.Run(() => command.ExecuteReader());
                while (reader.Read())
                {
                    trends.Add(new MinerTrend
                    {
                        Date = reader.GetDateTime(0),
                        MinerType = reader.GetString(1),
                        Count = reader.GetInt32(2)
                    });
                }

                // Calculate growth rates
                CalculateGrowthRates(trends);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting miner trends");
            }

            return trends;
        }

        public async Task<List<GeographicDistribution>> GetGeographicDistributionAsync()
        {
            var distribution = new List<GeographicDistribution>();

            try
            {
                using var connection = DatabaseManager.GetConnection();
                connection.Open();

                var sql = @"
                    SELECT 
                        ip.Geolocation,
                        COUNT(*) as TotalMiners,
                        COUNT(DISTINCT ip.IPAddress) as ActiveMiners
                    FROM IPResults ip
                    WHERE ip.BlockchainDetected = 1
                    AND ip.Geolocation IS NOT NULL
                    GROUP BY ip.Geolocation
                    ORDER BY TotalMiners DESC";

                using var command = new SQLiteCommand(sql, connection);
                using var reader = await Task.Run(() => command.ExecuteReader());
                while (reader.Read())
                {
                    distribution.Add(new GeographicDistribution
                    {
                        Province = reader.GetString(0),
                        TotalMiners = reader.GetInt32(1),
                        ActiveMiners = reader.GetInt32(2),
                        LastUpdated = DateTime.Now
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting geographic distribution");
            }

            return distribution;
        }

        public async Task<List<ISPStatistics>> GetISPStatisticsAsync()
        {
            var stats = new List<ISPStatistics>();

            try
            {
                using var connection = DatabaseManager.GetConnection();
                connection.Open();

                var sql = @"
                    SELECT 
                        ip.ISP,
                        ip.ASN,
                        COUNT(*) as TotalMiners,
                        COUNT(DISTINCT ip.IPAddress) as UniqueIPs
                    FROM IPResults ip
                    WHERE ip.BlockchainDetected = 1
                    AND ip.ISP IS NOT NULL
                    GROUP BY ip.ISP, ip.ASN
                    ORDER BY TotalMiners DESC";

                using var command = new SQLiteCommand(sql, connection);
                using var reader = await Task.Run(() => command.ExecuteReader());
                while (reader.Read())
                {
                    var ispName = reader.IsDBNull(0) ? "Unknown" : reader.GetString(0);
                    var totalMiners = reader.GetInt32(2);
                    
                    stats.Add(new ISPStatistics
                    {
                        ISPName = ispName,
                        ASN = reader.IsDBNull(1) ? "" : reader.GetString(1),
                        TotalMiners = totalMiners,
                        UniqueIPs = reader.GetInt32(3),
                        RiskScore = CalculateISPRiskScore(totalMiners),
                        LastUpdated = DateTime.Now
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ISP statistics");
            }

            return stats;
        }

        public async Task<RiskAssessment> AssessRiskAsync(string ipAddress)
        {
            var assessment = new RiskAssessment { IPAddress = ipAddress };

            try
            {
                using var connection = DatabaseManager.GetConnection();
                connection.Open();

                // Get IP details
                var sql = @"
                    SELECT 
                        ip.*,
                        sr.StartTime as LastSeen
                    FROM IPResults ip
                    JOIN ScanResults sr ON ip.ScanResultId = sr.Id
                    WHERE ip.IPAddress = @IP
                    ORDER BY sr.StartTime DESC
                    LIMIT 1";

                using var command = new SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue("@IP", ipAddress);

                using var reader = await Task.Run(() => command.ExecuteReader());
                if (reader.Read())
                {
                    var riskFactors = new List<string>();
                    double riskScore = 0;

                    // Check for fake IP
                    if (reader.GetInt32(8) == 1)
                    {
                        riskScore += 30;
                        riskFactors.Add("Known fake IP/VPN");
                    }

                    // Check for blockchain activity
                    if (reader.GetInt32(10) == 1)
                    {
                        riskScore += 20;
                        riskFactors.Add("Cryptocurrency mining detected");
                    }

                    // Check for suspicious ports
                    var port = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);
                    if (IsSuspiciousPort(port))
                    {
                        riskScore += 15;
                        riskFactors.Add($"Suspicious port activity ({port})");
                    }

                    assessment.RiskScore = Math.Min(riskScore, 100);
                    assessment.Level = GetRiskLevel(assessment.RiskScore);
                    assessment.RiskFactors = riskFactors;
                    assessment.ISP = reader.IsDBNull(13) ? "" : reader.GetString(13);
                    assessment.Province = reader.IsDBNull(12) ? "" : reader.GetString(12);
                    assessment.LastSeen = reader.IsDBNull(16) ? null : reader.GetDateTime(16);
                }

                // Save assessment
                await SaveRiskAssessmentAsync(assessment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error assessing risk for {ipAddress}");
            }

            return assessment;
        }

        public async Task<List<AnomalyDetection>> DetectAnomaliesAsync(DateTime lookbackPeriod)
        {
            var anomalies = new List<AnomalyDetection>();

            try
            {
                // Detect sudden miner appearances
                var suddenMiners = await DetectSuddenMinerAppearancesAsync(lookbackPeriod);
                anomalies.AddRange(suddenMiners);

                // Detect geographic anomalies
                var geoAnomalies = await DetectGeographicAnomaliesAsync(lookbackPeriod);
                anomalies.AddRange(geoAnomalies);

                // Save anomalies
                foreach (var anomaly in anomalies)
                {
                    await SaveAnomalyAsync(anomaly);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting anomalies");
            }

            return anomalies;
        }

        public async Task<ScanComparison> CompareScansAsync(long baseScanId, long comparisonScanId)
        {
            var comparison = new ScanComparison
            {
                BaseScanId = baseScanId,
                ComparisonScanId = comparisonScanId
            };

            try
            {
                using var connection = DatabaseManager.GetConnection();
                connection.Open();

                var baseHosts = await GetScanHostsAsync(connection, baseScanId);
                var compareHosts = await GetScanHostsAsync(connection, comparisonScanId);

                var baseSet = new HashSet<string>(baseHosts.Select(h => h.IPAddress));
                var compareSet = new HashSet<string>(compareHosts.Select(h => h.IPAddress));

                comparison.NewHosts = compareSet.Except(baseSet).Count();
                comparison.RemovedHosts = baseSet.Except(compareSet).Count();

                // Find changed hosts
                var commonHosts = baseSet.Intersect(compareSet);
                foreach (var ip in commonHosts)
                {
                    var baseHost = baseHosts.First(h => h.IPAddress == ip);
                    var compareHost = compareHosts.First(h => h.IPAddress == ip);

                    if (baseHost.BlockchainDetected != compareHost.BlockchainDetected ||
                        baseHost.IsFakeIP != compareHost.IsFakeIP)
                    {
                        comparison.ChangedHosts++;
                        comparison.Differences.Add(new HostDifference
                        {
                            IPAddress = ip,
                            Type = DifferenceType.Changed,
                            PropertyName = "Status",
                            OldValue = $"Blockchain: {baseHost.BlockchainDetected}, FakeIP: {baseHost.IsFakeIP}",
                            NewValue = $"Blockchain: {compareHost.BlockchainDetected}, FakeIP: {compareHost.IsFakeIP}"
                        });
                    }
                }

                // New miners
                comparison.NewMiners = compareHosts.Count(h => h.BlockchainDetected && !baseSet.Contains(h.IPAddress));
                
                // Removed miners
                comparison.RemovedMiners = baseHosts.Count(h => h.BlockchainDetected && !compareSet.Contains(h.IPAddress));

                await SaveScanComparisonAsync(comparison);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error comparing scans");
            }

            return comparison;
        }

        private async Task<int> GetTotalScansAsync(SQLiteConnection connection)
        {
            using var command = new SQLiteCommand("SELECT COUNT(*) FROM ScanResults", connection);
            return Convert.ToInt32(await Task.Run(() => command.ExecuteScalar()));
        }

        private async Task<int> GetTotalMinersDetectedAsync(SQLiteConnection connection)
        {
            using var command = new SQLiteCommand("SELECT COUNT(*) FROM IPResults WHERE BlockchainDetected = 1", connection);
            return Convert.ToInt32(await Task.Run(() => command.ExecuteScalar()));
        }

        private async Task<int> GetScheduledScanCountAsync(SQLiteConnection connection)
        {
            using var command = new SQLiteCommand("SELECT COUNT(*) FROM ScheduledScans WHERE IsEnabled = 1", connection);
            return Convert.ToInt32(await Task.Run(() => command.ExecuteScalar()));
        }

        private async Task<List<MinerTrend>> GetTopMinerTypesAsync(SQLiteConnection connection, int limit)
        {
            var trends = new List<MinerTrend>();
            var sql = $@"
                SELECT BlockchainType, COUNT(*) as Count 
                FROM IPResults 
                WHERE BlockchainDetected = 1 
                GROUP BY BlockchainType 
                ORDER BY Count DESC 
                LIMIT {limit}";

            using var command = new SQLiteCommand(sql, connection);
            using var reader = await Task.Run(() => command.ExecuteReader());
            while (reader.Read())
            {
                trends.Add(new MinerTrend
                {
                    MinerType = reader.GetString(0),
                    Count = reader.GetInt32(1)
                });
            }
            return trends;
        }

        private async Task<List<GeographicDistribution>> GetTopProvincesAsync(SQLiteConnection connection, int limit)
        {
            var provinces = new List<GeographicDistribution>();
            // Simplified - would join with geolocation data
            await Task.CompletedTask;
            return provinces;
        }

        private async Task<List<RiskAssessment>> GetHighRiskHostsAsync(SQLiteConnection connection, int limit)
        {
            var hosts = new List<RiskAssessment>();
            var sql = $@"
                SELECT IPAddress, RiskScore, Level, Province, ISP 
                FROM RiskAssessments 
                WHERE Level IN ('High', 'Critical')
                ORDER BY RiskScore DESC 
                LIMIT {limit}";

            using var command = new SQLiteCommand(sql, connection);
            using var reader = await Task.Run(() => command.ExecuteReader());
            while (reader.Read())
            {
                hosts.Add(new RiskAssessment
                {
                    IPAddress = reader.GetString(0),
                    RiskScore = reader.GetDouble(1),
                    Level = (RiskLevel)Enum.Parse(typeof(RiskLevel), reader.GetString(2)),
                    Province = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    ISP = reader.IsDBNull(4) ? "" : reader.GetString(4)
                });
            }
            return hosts;
        }

        private async Task<List<AnomalyDetection>> GetRecentAnomaliesAsync(SQLiteConnection connection, int limit)
        {
            var anomalies = new List<AnomalyDetection>();
            var sql = $@"
                SELECT * FROM Anomalies 
                WHERE IsResolved = 0 
                ORDER BY DetectedAt DESC 
                LIMIT {limit}";

            using var command = new SQLiteCommand(sql, connection);
            using var reader = await Task.Run(() => command.ExecuteReader());
            while (reader.Read())
            {
                anomalies.Add(new AnomalyDetection
                {
                    Id = reader.GetInt64(0),
                    DetectedAt = reader.GetDateTime(1),
                    Type = (AnomalyType)Enum.Parse(typeof(AnomalyType), reader.GetString(2)),
                    Description = reader.GetString(3),
                    IPAddress = reader.GetString(4),
                    SeverityScore = reader.GetDouble(5),
                    IsResolved = reader.GetInt32(6) == 1
                });
            }
            return anomalies;
        }

        private void CalculateGrowthRates(List<MinerTrend> trends)
        {
            var grouped = trends.GroupBy(t => t.MinerType);
            foreach (var group in grouped)
            {
                var ordered = group.OrderBy(t => t.Date).ToList();
                for (int i = 1; i < ordered.Count; i++)
                {
                    var current = ordered[i];
                    var previous = ordered[i - 1];
                    if (previous.Count > 0)
                    {
                        current.GrowthRate = ((double)(current.Count - previous.Count) / previous.Count) * 100;
                    }
                }
            }
        }

        private double CalculateISPRiskScore(int totalMiners)
        {
            if (totalMiners >= 100) return 100;
            if (totalMiners >= 50) return 75;
            if (totalMiners >= 20) return 50;
            if (totalMiners >= 10) return 25;
            return Math.Max(totalMiners * 2, 0);
        }

        private RiskLevel GetRiskLevel(double score)
        {
            return score switch
            {
                >= 80 => RiskLevel.Critical,
                >= 60 => RiskLevel.High,
                >= 40 => RiskLevel.Medium,
                >= 20 => RiskLevel.Low,
                _ => RiskLevel.None
            };
        }

        private bool IsSuspiciousPort(int port)
        {
            var suspiciousPorts = new[] { 3333, 4444, 4028, 7777, 8332, 8545, 14433, 14444 };
            return suspiciousPorts.Contains(port);
        }

        private async Task<List<IPResult>> GetScanHostsAsync(SQLiteConnection connection, long scanId)
        {
            var hosts = new List<IPResult>();
            using var command = new SQLiteCommand("SELECT * FROM IPResults WHERE ScanResultId = @ScanId", connection);
            command.Parameters.AddWithValue("@ScanId", scanId);
            using var reader = await Task.Run(() => command.ExecuteReader());
            while (reader.Read())
            {
                hosts.Add(new IPResult
                {
                    Id = reader.GetInt64(0),
                    IPAddress = reader.GetString(2),
                    Port = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                    BlockchainDetected = reader.GetInt32(10) == 1,
                    IsFakeIP = reader.GetInt32(8) == 1
                });
            }
            return hosts;
        }

        private async Task SaveRiskAssessmentAsync(RiskAssessment assessment)
        {
            try
            {
                using var connection = DatabaseManager.GetConnection();
                connection.Open();

                var sql = @"
                    INSERT OR REPLACE INTO RiskAssessments 
                    (IPAddress, RiskScore, Level, RiskFactors, Province, City, ISP, AssessedAt, LastSeen)
                    VALUES (@IP, @Score, @Level, @Factors, @Province, @City, @ISP, @AssessedAt, @LastSeen)";

                using var command = new SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue("@IP", assessment.IPAddress);
                command.Parameters.AddWithValue("@Score", assessment.RiskScore);
                command.Parameters.AddWithValue("@Level", assessment.Level.ToString());
                command.Parameters.AddWithValue("@Factors", string.Join(",", assessment.RiskFactors));
                command.Parameters.AddWithValue("@Province", assessment.Province ?? "");
                command.Parameters.AddWithValue("@City", assessment.City ?? "");
                command.Parameters.AddWithValue("@ISP", assessment.ISP ?? "");
                command.Parameters.AddWithValue("@AssessedAt", assessment.AssessedAt);
                command.Parameters.AddWithValue("@LastSeen", (object)assessment.LastSeen ?? DBNull.Value);

                await Task.Run(() => command.ExecuteNonQuery());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving risk assessment");
            }
        }

        private async Task SaveAnomalyAsync(AnomalyDetection anomaly)
        {
            try
            {
                using var connection = DatabaseManager.GetConnection();
                connection.Open();

                var sql = @"
                    INSERT INTO Anomalies 
                    (DetectedAt, Type, Description, IPAddress, SeverityScore, IsResolved)
                    VALUES (@DetectedAt, @Type, @Description, @IP, @Severity, 0)";

                using var command = new SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue("@DetectedAt", anomaly.DetectedAt);
                command.Parameters.AddWithValue("@Type", anomaly.Type.ToString());
                command.Parameters.AddWithValue("@Description", anomaly.Description);
                command.Parameters.AddWithValue("@IP", anomaly.IPAddress);
                command.Parameters.AddWithValue("@Severity", anomaly.SeverityScore);

                await Task.Run(() => command.ExecuteNonQuery());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving anomaly");
            }
        }

        private async Task SaveScanComparisonAsync(ScanComparison comparison)
        {
            try
            {
                using var connection = DatabaseManager.GetConnection();
                connection.Open();

                var sql = @"
                    INSERT INTO ScanComparisons 
                    (BaseScanId, ComparisonScanId, CreatedAt, NewHosts, RemovedHosts, ChangedHosts, NewMiners, RemovedMiners)
                    VALUES (@Base, @Compare, @Created, @New, @Removed, @Changed, @NewMiners, @RemovedMiners)";

                using var command = new SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue("@Base", comparison.BaseScanId);
                command.Parameters.AddWithValue("@Compare", comparison.ComparisonScanId);
                command.Parameters.AddWithValue("@Created", comparison.CreatedAt);
                command.Parameters.AddWithValue("@New", comparison.NewHosts);
                command.Parameters.AddWithValue("@Removed", comparison.RemovedHosts);
                command.Parameters.AddWithValue("@Changed", comparison.ChangedHosts);
                command.Parameters.AddWithValue("@NewMiners", comparison.NewMiners);
                command.Parameters.AddWithValue("@RemovedMiners", comparison.RemovedMiners);

                var id = Convert.ToInt64(await Task.Run(() => command.ExecuteScalar()));
                comparison.Id = id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving scan comparison");
            }
        }

        private async Task<List<AnomalyDetection>> DetectSuddenMinerAppearancesAsync(DateTime lookbackPeriod)
        {
            var anomalies = new List<AnomalyDetection>();
            // Implementation would compare recent activity with historical baseline
            await Task.CompletedTask;
            return anomalies;
        }

        private async Task<List<AnomalyDetection>> DetectGeographicAnomaliesAsync(DateTime lookbackPeriod)
        {
            var anomalies = new List<AnomalyDetection>();
            // Implementation would detect unusual geographic patterns
            await Task.CompletedTask;
            return anomalies;
        }
    }
}
