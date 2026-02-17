using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BlockchainNetworkAnalyzer.Core.Models;
using static BlockchainNetworkAnalyzer.App;

namespace BlockchainNetworkAnalyzer.Core.Services
{
    public class ReportingService
    {
        private readonly ILogger<ReportingService> _logger;
        private readonly string _reportsDirectory;

        public ReportingService()
        {
            _logger = App.LoggerFactory.CreateLogger<ReportingService>();
            _reportsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
            Directory.CreateDirectory(_reportsDirectory);
        }

        public async Task<string> GenerateHtmlReportAsync(long scanId, string reportTitle = null)
        {
            try
            {
                var scanResult = await GetScanResultAsync(scanId);
                if (scanResult == null)
                {
                    throw new ArgumentException($"Scan with ID {scanId} not found");
                }

                var ipResults = await GetIPResultsAsync(scanId);
                var detectionResults = await GetDetectionResultsAsync(scanId);

                var html = GenerateHtmlReportContent(scanResult, ipResults, detectionResults, reportTitle);

                var fileName = $"Report_Scan_{scanId}_{DateTime.Now:yyyyMMdd_HHmmss}.html";
                var filePath = Path.Combine(_reportsDirectory, fileName);

                await File.WriteAllTextAsync(filePath, html, Encoding.UTF8);
                _logger.LogInformation($"HTML report generated: {filePath}");

                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating HTML report for scan {scanId}");
                throw;
            }
        }

        public async Task<string> GenerateCsvReportAsync(long scanId)
        {
            try
            {
                var ipResults = await GetIPResultsAsync(scanId);

                var csv = new StringBuilder();
                csv.AppendLine("IPAddress,Port,PortStatus,Service,Protocol,BlockchainDetected,BlockchainType,IsFakeIP,FakeIPReason,ISP,Geolocation,ResponseTime,CreatedAt");

                foreach (var result in ipResults)
                {
                    csv.AppendLine($"{result.IPAddress},{result.Port},{result.PortStatus},{result.Service},{result.Protocol},{result.BlockchainDetected},{result.BlockchainType},{result.IsFakeIP},\"{result.FakeIPReason}\",{result.ISP},\"{result.Geolocation}\",{result.ResponseTime},{result.CreatedAt:yyyy-MM-dd HH:mm:ss}");
                }

                var fileName = $"Report_Scan_{scanId}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                var filePath = Path.Combine(_reportsDirectory, fileName);

                await File.WriteAllTextAsync(filePath, csv.ToString(), Encoding.UTF8);
                _logger.LogInformation($"CSV report generated: {filePath}");

                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating CSV report for scan {scanId}");
                throw;
            }
        }

        public async Task<string> GenerateJsonReportAsync(long scanId)
        {
            try
            {
                var scanResult = await GetScanResultAsync(scanId);
                var ipResults = await GetIPResultsAsync(scanId);

                var report = new
                {
                    Scan = scanResult,
                    Results = ipResults,
                    GeneratedAt = DateTime.UtcNow,
                    ReportVersion = "1.0"
                };

                var json = Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented);

                var fileName = $"Report_Scan_{scanId}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                var filePath = Path.Combine(_reportsDirectory, fileName);

                await File.WriteAllTextAsync(filePath, json, Encoding.UTF8);
                _logger.LogInformation($"JSON report generated: {filePath}");

                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating JSON report for scan {scanId}");
                throw;
            }
        }

        public async Task<string> GenerateTxtReportAsync(long scanId)
        {
            try
            {
                var scanResult = await GetScanResultAsync(scanId);
                var ipResults = await GetIPResultsAsync(scanId);

                var txt = new StringBuilder();
                txt.AppendLine("=".PadRight(80, '='));
                txt.AppendLine("IRANIAN NETWORK MINER DETECTION SYSTEM - SCAN REPORT");
                txt.AppendLine("=".PadRight(80, '='));
                txt.AppendLine();
                txt.AppendLine($"Scan ID: {scanResult.Id}");
                txt.AppendLine($"Scan Type: {scanResult.ScanType}");
                txt.AppendLine($"Start Time: {scanResult.StartTime:yyyy-MM-dd HH:mm:ss}");
                txt.AppendLine($"End Time: {scanResult.EndTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A"}");
                txt.AppendLine($"Status: {scanResult.Status}");
                txt.AppendLine($"Total IPs: {scanResult.TotalIPs}");
                txt.AppendLine($"Scanned IPs: {scanResult.ScannedIPs}");
                txt.AppendLine($"Hosts Found: {scanResult.FoundHosts}");
                txt.AppendLine();
                txt.AppendLine("-".PadRight(80, '-'));
                txt.AppendLine("DETAILED RESULTS");
                txt.AppendLine("-".PadRight(80, '-'));
                txt.AppendLine();

                foreach (var result in ipResults)
                {
                    txt.AppendLine($"IP: {result.IPAddress}");
                    txt.AppendLine($"  Port: {result.Port} ({result.PortStatus})");
                    txt.AppendLine($"  Service: {result.Service}");
                    txt.AppendLine($"  Protocol: {result.Protocol}");
                    txt.AppendLine($"  Blockchain: {(result.BlockchainDetected ? $"Yes ({result.BlockchainType})" : "No")}");
                    txt.AppendLine($"  Fake IP: {(result.IsFakeIP ? $"Yes ({result.FakeIPReason})" : "No")}");
                    txt.AppendLine($"  ISP: {result.ISP}");
                    txt.AppendLine($"  Response Time: {result.ResponseTime} ms");
                    txt.AppendLine();
                }

                txt.AppendLine("=".PadRight(80, '='));
                txt.AppendLine($"Report Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                txt.AppendLine("=".PadRight(80, '='));

                var fileName = $"Report_Scan_{scanId}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                var filePath = Path.Combine(_reportsDirectory, fileName);

                await File.WriteAllTextAsync(filePath, txt.ToString(), Encoding.UTF8);
                _logger.LogInformation($"TXT report generated: {filePath}");

                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating TXT report for scan {scanId}");
                throw;
            }
        }

        public async Task<string> GenerateSummaryReportAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var stats = await GetScanStatisticsAsync(startDate, endDate);

                var html = GenerateSummaryHtmlContent(stats, startDate, endDate);

                var fileName = $"SummaryReport_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.html";
                var filePath = Path.Combine(_reportsDirectory, fileName);

                await File.WriteAllTextAsync(filePath, html, Encoding.UTF8);
                _logger.LogInformation($"Summary report generated: {filePath}");

                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating summary report");
                throw;
            }
        }

        private string GenerateHtmlReportContent(ScanResult scan, List<IPResult> results, List<DetectionResult> detections, string title)
        {
            var html = new StringBuilder();
            
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html dir=\"rtl\" lang=\"fa\">");
            html.AppendLine("<head>");
            html.AppendLine("<meta charset=\"UTF-8\">");
            html.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            html.AppendLine($"<title>{title ?? "Scan Report"}</title>");
            html.AppendLine("<style>");
            html.AppendLine(@"
                body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 0; padding: 20px; background: #f5f5f5; }
                .container { max-width: 1400px; margin: 0 auto; background: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }
                .header { text-align: center; border-bottom: 3px solid #2196F3; padding-bottom: 20px; margin-bottom: 30px; }
                .header h1 { color: #1976D2; margin: 0; font-size: 28px; }
                .header h2 { color: #666; margin: 10px 0 0 0; font-size: 18px; font-weight: normal; }
                .stats-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 20px; margin-bottom: 30px; }
                .stat-card { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 20px; border-radius: 10px; text-align: center; }
                .stat-card h3 { margin: 0 0 10px 0; font-size: 14px; opacity: 0.9; }
                .stat-card .value { font-size: 32px; font-weight: bold; }
                .section { margin-bottom: 30px; }
                .section h3 { color: #333; border-bottom: 2px solid #e0e0e0; padding-bottom: 10px; }
                table { width: 100%; border-collapse: collapse; margin-top: 20px; }
                th, td { padding: 12px; text-align: right; border-bottom: 1px solid #e0e0e0; }
                th { background: #f8f9fa; font-weight: 600; color: #555; }
                tr:hover { background: #f8f9fa; }
                .badge { padding: 4px 12px; border-radius: 20px; font-size: 12px; font-weight: 500; }
                .badge-success { background: #4CAF50; color: white; }
                .badge-warning { background: #FF9800; color: white; }
                .badge-danger { background: #f44336; color: white; }
                .badge-info { background: #2196F3; color: white; }
                .footer { text-align: center; margin-top: 40px; padding-top: 20px; border-top: 1px solid #e0e0e0; color: #999; font-size: 12px; }
            ");
            html.AppendLine("</style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            html.AppendLine("<div class=\"container\">");
            
            // Header
            html.AppendLine("<div class=\"header\">");
            html.AppendLine("<h1>🛡️ Iranian Network Miner Detection System</h1>");
            html.AppendLine($"<h2>{title ?? $"Scan Report - {scan.ScanType}"}</h2>");
            html.AppendLine("</div>");
            
            // Stats
            html.AppendLine("<div class=\"stats-grid\">");
            html.AppendLine("<div class=\"stat-card\">");
            html.AppendLine("<h3>Total IPs</h3>");
            html.AppendLine($"<div class=\"value\">{scan.TotalIPs}</div>");
            html.AppendLine("</div>");
            html.AppendLine("<div class=\"stat-card\">");
            html.AppendLine("<h3>Hosts Found</h3>");
            html.AppendLine($"<div class=\"value\">{scan.FoundHosts}</div>");
            html.AppendLine("</div>");
            html.AppendLine("<div class=\"stat-card\">");
            html.AppendLine("<h3>Blockchain Detected</h3>");
            html.AppendLine($"<div class=\"value\">{results.Count(r => r.BlockchainDetected)}</div>");
            html.AppendLine("</div>");
            html.AppendLine("<div class=\"stat-card\">");
            html.AppendLine("<h3>Fake IPs</h3>");
            html.AppendLine($"<div class=\"value\">{results.Count(r => r.IsFakeIP)}</div>");
            html.AppendLine("</div>");
            html.AppendLine("</div>");
            
            // Scan Info
            html.AppendLine("<div class=\"section\">");
            html.AppendLine("<h3>📋 Scan Information</h3>");
            html.AppendLine("<table>");
            html.AppendLine("<tr><th>Property</th><th>Value</th></tr>");
            html.AppendLine($"<tr><td>Scan ID</td><td>{scan.Id}</td></tr>");
            html.AppendLine($"<tr><td>Scan Type</td><td>{scan.ScanType}</td></tr>");
            html.AppendLine($"<tr><td>Start Time</td><td>{scan.StartTime:yyyy-MM-dd HH:mm:ss}</td></tr>");
            html.AppendLine($"<tr><td>End Time</td><td>{scan.EndTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A"}</td></tr>");
            html.AppendLine($"<tr><td>Status</td><td><span class=\"badge badge-{(scan.Status == "Completed" ? "success" : "warning")}\">{scan.Status}</span></td></tr>");
            html.AppendLine($"<tr><td>Duration</td><td>{(scan.EndTime.HasValue ? (scan.EndTime.Value - scan.StartTime).ToString("hh\\:mm\\:ss") : "N/A")}</td></tr>");
            html.AppendLine("</table>");
            html.AppendLine("</div>");
            
            // Results Table
            if (results.Any())
            {
                html.AppendLine("<div class=\"section\">");
                html.AppendLine("<h3>🔍 Detailed Results</h3>");
                html.AppendLine("<table>");
                html.AppendLine("<thead>");
                html.AppendLine("<tr>");
                html.AppendLine("<th>IP Address</th>");
                html.AppendLine("<th>Port</th>");
                html.AppendLine("<th>Service</th>");
                html.AppendLine("<th>Blockchain</th>");
                html.AppendLine("<th>Fake IP</th>");
                html.AppendLine("<th>ISP</th>");
                html.AppendLine("<th>Response</th>");
                html.AppendLine("</tr>");
                html.AppendLine("</thead>");
                html.AppendLine("<tbody>");
                
                foreach (var result in results)
                {
                    html.AppendLine("<tr>");
                    html.AppendLine($"<td>{result.IPAddress}</td>");
                    html.AppendLine($"<td>{result.Port}</td>");
                    html.AppendLine($"<td>{result.Service}</td>");
                    html.AppendLine($"<td>{(result.BlockchainDetected ? $"<span class=\"badge badge-success\">{result.BlockchainType}</span>" : "-")}</td>");
                    html.AppendLine($"<td>{(result.IsFakeIP ? $"<span class=\"badge badge-danger\">Yes</span>" : "-")}</td>");
                    html.AppendLine($"<td>{result.ISP}</td>");
                    html.AppendLine($"<td>{result.ResponseTime}ms</td>");
                    html.AppendLine("</tr>");
                }
                
                html.AppendLine("</tbody>");
                html.AppendLine("</table>");
                html.AppendLine("</div>");
            }
            
            // Footer
            html.AppendLine("<div class=\"footer\">");
            html.AppendLine($"<p>Generated by Iranian Network Miner Detection System on {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");
            html.AppendLine("<p>⚠️ This report is for authorized security auditing purposes only.</p>");
            html.AppendLine("</div>");
            
            html.AppendLine("</div>");
            html.AppendLine("</body>");
            html.AppendLine("</html>");
            
            return html.ToString();
        }

        private string GenerateSummaryHtmlContent(List<ScanStatistics> stats, DateTime startDate, DateTime endDate)
        {
            var html = new StringBuilder();
            
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html dir=\"rtl\" lang=\"fa\">");
            html.AppendLine("<head>");
            html.AppendLine("<meta charset=\"UTF-8\">");
            html.AppendLine($"<title>Summary Report {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}</title>");
            html.AppendLine("<style>");
            html.AppendLine(@"
                body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 0; padding: 20px; background: #f5f5f5; }
                .container { max-width: 1200px; margin: 0 auto; background: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }
                .header { text-align: center; border-bottom: 3px solid #2196F3; padding-bottom: 20px; margin-bottom: 30px; }
                .header h1 { color: #1976D2; margin: 0; }
                .summary-stats { display: grid; grid-template-columns: repeat(4, 1fr); gap: 20px; margin: 30px 0; }
                .stat-box { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 20px; border-radius: 10px; text-align: center; }
                .stat-box h3 { margin: 0 0 10px 0; font-size: 14px; opacity: 0.9; }
                .stat-box .value { font-size: 28px; font-weight: bold; }
            ");
            html.AppendLine("</style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            html.AppendLine("<div class=\"container\">");
            html.AppendLine("<div class=\"header\">");
            html.AppendLine("<h1>📊 Summary Report</h1>");
            html.AppendLine($"<p>{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}</p>");
            html.AppendLine("</div>");
            
            var totalScans = stats.Sum(s => s.TotalScans);
            var totalIPs = stats.Sum(s => s.TotalIPsScanned);
            var totalHosts = stats.Sum(s => s.TotalHostsFound);
            var totalMiners = stats.Sum(s => s.TotalMinersDetected);
            
            html.AppendLine("<div class=\"summary-stats\">");
            html.AppendLine("<div class=\"stat-box\">");
            html.AppendLine("<h3>Total Scans</h3>");
            html.AppendLine($"<div class=\"value\">{totalScans}</div>");
            html.AppendLine("</div>");
            html.AppendLine("<div class=\"stat-box\">");
            html.AppendLine("<h3>IPs Scanned</h3>");
            html.AppendLine($"<div class=\"value\">{totalIPs:N0}</div>");
            html.AppendLine("</div>");
            html.AppendLine("<div class=\"stat-box\">");
            html.AppendLine("<h3>Hosts Found</h3>");
            html.AppendLine($"<div class=\"value\">{totalHosts:N0}</div>");
            html.AppendLine("</div>");
            html.AppendLine("<div class=\"stat-box\">");
            html.AppendLine("<h3>Miners Detected</h3>");
            html.AppendLine($"<div class=\"value\">{totalMiners:N0}</div>");
            html.AppendLine("</div>");
            html.AppendLine("</div>");
            
            html.AppendLine("</div>");
            html.AppendLine("</body>");
            html.AppendLine("</html>");
            
            return html.ToString();
        }

        private async Task<ScanResult> GetScanResultAsync(long scanId)
        {
            using var connection = DatabaseManager.GetConnection();
            connection.Open();

            var sql = "SELECT * FROM ScanResults WHERE Id = @Id";
            using var command = new SQLiteCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", scanId);

            using var reader = await Task.Run(() => command.ExecuteReader());
            if (reader.Read())
            {
                return new ScanResult
                {
                    Id = reader.GetInt64(0),
                    ScanType = reader.GetString(1),
                    StartTime = reader.GetDateTime(2),
                    EndTime = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                    Status = reader.GetString(4),
                    TotalIPs = reader.GetInt32(5),
                    ScannedIPs = reader.GetInt32(6),
                    FoundHosts = reader.GetInt32(7),
                    Configuration = reader.IsDBNull(8) ? "" : reader.GetString(8)
                };
            }

            return null;
        }

        private async Task<List<IPResult>> GetIPResultsAsync(long scanId)
        {
            var results = new List<IPResult>();

            using var connection = DatabaseManager.GetConnection();
            connection.Open();

            var sql = "SELECT * FROM IPResults WHERE ScanResultId = @ScanId ORDER BY Id";
            using var command = new SQLiteCommand(sql, connection);
            command.Parameters.AddWithValue("@ScanId", scanId);

            using var reader = await Task.Run(() => command.ExecuteReader());
            while (reader.Read())
            {
                results.Add(new IPResult
                {
                    Id = reader.GetInt64(0),
                    ScanResultId = reader.GetInt64(1),
                    IPAddress = reader.GetString(2),
                    Port = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                    PortStatus = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    Service = reader.IsDBNull(5) ? "" : reader.GetString(5),
                    Protocol = reader.IsDBNull(6) ? "" : reader.GetString(6),
                    ResponseTime = reader.IsDBNull(7) ? null : reader.GetInt32(7),
                    IsFakeIP = reader.GetInt32(8) == 1,
                    FakeIPReason = reader.IsDBNull(9) ? "" : reader.GetString(9),
                    BlockchainDetected = reader.GetInt32(10) == 1,
                    BlockchainType = reader.IsDBNull(11) ? "" : reader.GetString(11),
                    Geolocation = reader.IsDBNull(12) ? "" : reader.GetString(12),
                    ISP = reader.IsDBNull(13) ? "" : reader.GetString(13),
                    ASN = reader.IsDBNull(14) ? "" : reader.GetString(14),
                    CreatedAt = reader.GetDateTime(15)
                });
            }

            return results;
        }

        private async Task<List<DetectionResult>> GetDetectionResultsAsync(long scanId)
        {
            var results = new List<DetectionResult>();
            // Implementation would query DetectionResults table
            await Task.CompletedTask;
            return results;
        }

        private async Task<List<ScanStatistics>> GetScanStatisticsAsync(DateTime startDate, DateTime endDate)
        {
            var stats = new List<ScanStatistics>();
            // Implementation would aggregate scan data
            await Task.CompletedTask;
            return stats;
        }

        public string GetReportsDirectory() => _reportsDirectory;

        public List<string> GetAvailableReports()
        {
            var directory = new DirectoryInfo(_reportsDirectory);
            return directory.GetFiles("*.*")
                .OrderByDescending(f => f.CreationTime)
                .Select(f => f.FullName)
                .ToList();
        }
    }
}
