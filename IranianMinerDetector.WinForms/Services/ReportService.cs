using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using IranianMinerDetector.WinForms.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace IranianMinerDetector.WinForms.Services
{
    public class ReportService
    {
        private readonly DatabaseManager _db = DatabaseManager.Instance;
        private readonly string _reportsDir;

        public ReportService()
        {
            _reportsDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "IranianMinerDetector",
                "Reports");

            if (!Directory.Exists(_reportsDir))
                Directory.CreateDirectory(_reportsDir);
        }

        public async Task<string> GeneratePDFReportAsync(int scanId)
        {
            var scan = _db.GetAllScanRecords().FirstOrDefault(s => s.Id == scanId);
            if (scan == null)
                throw new ArgumentException($"Scan {scanId} not found");

            var hosts = _db.GetHostsByScanId(scanId);
            var miners = hosts.Where(h => h.IsMinerDetected).ToList();

            var fileName = $"Scan_{scanId}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            var filePath = Path.Combine(_reportsDir, fileName);

            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Calibri));

                    page.Header().Element(ComposeHeader);
                    page.Content().Element(ComposeContent);
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
                });
            });

            await Task.Run(() => document.GeneratePdf(filePath));

            return filePath;
        }

        public string GenerateExcelReport(int scanId)
        {
            var scan = _db.GetAllScanRecords().FirstOrDefault(s => s.Id == scanId);
            if (scan == null)
                throw new ArgumentException($"Scan {scanId} not found");

            var hosts = _db.GetHostsByScanId(scanId);

            var fileName = $"Scan_{scanId}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            var filePath = Path.Combine(_reportsDir, fileName);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Scan Results");

            // Title
            worksheet.Cell("B1").Value = "Iranian Miner Detector - Scan Report";
            worksheet.Cell("B1").Style.Font.Bold = true;
            worksheet.Cell("B1").Style.Font.FontSize = 16;

            // Scan information
            int row = 3;
            worksheet.Cell($"A{row}").Value = "Scan ID:";
            worksheet.Cell($"B{row}").Value = scan.Id;
            row++;
            worksheet.Cell($"A{row}").Value = "Start Time:";
            worksheet.Cell($"B{row}").Value = scan.StartTime;
            row++;
            worksheet.Cell($"A{row}").Value = "End Time:";
            worksheet.Cell($"B{row}").Value = scan.EndTime?.ToString() ?? "N/A";
            row++;
            worksheet.Cell($"A{row}").Value = "Province:";
            worksheet.Cell($"B{row}").Value = scan.Province ?? "N/A";
            row++;
            worksheet.Cell($"A{row}").Value = "City:";
            worksheet.Cell($"B{row}").Value = scan.City ?? "N/A";
            row++;
            worksheet.Cell($"A{row}").Value = "ISP:";
            worksheet.Cell($"B{row}").Value = scan.ISP ?? "N/A";
            row++;
            worksheet.Cell($"A{row}").Value = "Total IPs:";
            worksheet.Cell($"B{row}").Value = scan.TotalIPs;
            row++;
            worksheet.Cell($"A{row}").Value = "Online Hosts:";
            worksheet.Cell($"B{row}").Value = scan.OnlineHosts;
            row++;
            worksheet.Cell($"A{row}").Value = "Miners Found:";
            worksheet.Cell($"B{row}").Value = scan.MinersFound;

            // Statistics section
            row += 2;
            worksheet.Cell($"A{row}").Value = "Statistics";
            worksheet.Cell($"A{row}").Style.Font.Bold = true;
            row++;
            worksheet.Cell($"A{row}").Value = "Success Rate:";
            worksheet.Cell($"B{row}").Value = scan.TotalIPs > 0 
                ? $"{(double)scan.OnlineHosts / scan.TotalIPs:P2}" 
                : "N/A";
            row++;
            worksheet.Cell($"A{row}").Value = "Miner Detection Rate:";
            worksheet.Cell($"B{row}").Value = scan.OnlineHosts > 0 
                ? $"{(double)scan.MinersFound / scan.OnlineHosts:P2}" 
                : "N/A";

            // Hosts table
            row += 2;
            worksheet.Cell($"A{row}").Value = "Scan Results";
            worksheet.Cell($"A{row}").Style.Font.Bold = true;
            row++;

            // Headers
            var headers = new[] { "IP Address", "Status", "Response Time (ms)", "Open Ports", 
                                 "Miner Detected", "Confidence", "Service", "ISP", "Location", "Scanned At" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(row, i + 1).Value = headers[i];
                worksheet.Cell(row, i + 1).Style.Font.Bold = true;
                worksheet.Cell(row, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
            }
            row++;

            // Data
            foreach (var host in hosts)
            {
                worksheet.Cell(row, 1).Value = host.IPAddress;
                worksheet.Cell(row, 2).Value = host.IsOnline ? "Online" : "Offline";
                worksheet.Cell(row, 3).Value = host.ResponseTimeMs?.ToString() ?? "N/A";
                worksheet.Cell(row, 4).Value = string.Join(", ", host.OpenPorts);
                worksheet.Cell(row, 5).Value = host.IsMinerDetected ? "Yes" : "No";
                worksheet.Cell(row, 6).Value = $"{host.ConfidenceScore:P2}";
                worksheet.Cell(row, 7).Value = host.DetectedService ?? "N/A";
                worksheet.Cell(row, 8).Value = host.ISP ?? "N/A";
                worksheet.Cell(row, 9).Value = $"{host.City}, {host.Province}".Trim(',', ' ');
                worksheet.Cell(row, 10).Value = host.ScannedAt.ToString("yyyy-MM-dd HH:mm:ss");

                // Highlight miners
                if (host.IsMinerDetected)
                {
                    worksheet.Range(row, 1, row, headers.Length).Style.Fill.BackgroundColor = XLColor.LightPink;
                }

                row++;
            }

            worksheet.Columns().AdjustToContents();
            workbook.SaveAs(filePath);

            return filePath;
        }

        public string GenerateCSVReport(int scanId)
        {
            var hosts = _db.GetHostsByScanId(scanId);
            var fileName = $"Scan_{scanId}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            var filePath = Path.Combine(_reportsDir, fileName);

            using var writer = new StreamWriter(filePath);
            
            // Header
            writer.WriteLine("IP Address,Status,Response Time (ms),Open Ports,Miner Detected,Confidence,Service,ISP,Province,City,Latitude,Longitude,Scanned At");

            // Data
            foreach (var host in hosts)
            {
                var line = $"{host.IPAddress}," +
                          $"{(host.IsOnline ? "Online" : "Offline")}," +
                          $"{host.ResponseTimeMs?.ToString() ?? ""}," +
                          $"\"{string.Join(", ", host.OpenPorts)}\"," +
                          $"{(host.IsMinerDetected ? "Yes" : "No")}," +
                          $"{host.ConfidenceScore}," +
                          $"\"{host.DetectedService ?? ""}\"," +
                          $"\"{host.ISP ?? ""}\"," +
                          $"\"{host.Province ?? ""}\"," +
                          $"\"{host.City ?? ""}\"," +
                          $"{host.Latitude?.ToString() ?? ""}," +
                          $"{host.Longitude?.ToString() ?? ""}," +
                          $"\"{host.ScannedAt:yyyy-MM-dd HH:mm:ss}\"";
                writer.WriteLine(line);
            }

            return filePath;
        }

        public string GenerateHTMLReport(int scanId)
        {
            var scan = _db.GetAllScanRecords().FirstOrDefault(s => s.Id == scanId);
            if (scan == null)
                throw new ArgumentException($"Scan {scanId} not found");

            var hosts = _db.GetHostsByScanId(scanId);
            var miners = hosts.Where(h => h.IsMinerDetected).ToList();

            var fileName = $"Scan_{scanId}_{DateTime.Now:yyyyMMdd_HHmmss}.html";
            var filePath = Path.Combine(_reportsDir, fileName);

            var html = GenerateHTMLContent(scan, hosts, miners);
            File.WriteAllText(filePath, html);

            return filePath;
        }

        private string GenerateHTMLContent(ScanRecord scan, List<HostRecord> hosts, List<HostRecord> miners)
        {
            var sb = new System.Text.StringBuilder();

            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang='fa' dir='rtl'>");
            sb.AppendLine("<head>");
            sb.AppendLine("    <meta charset='UTF-8'>");
            sb.AppendLine("    <meta name='viewport' content='width=device-width, initial-scale=1.0'>");
            sb.AppendLine("    <title>Iranian Miner Detector - Scan Report</title>");
            sb.AppendLine("    <link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css' />");
            sb.AppendLine("    <script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>");
            sb.AppendLine("    <style>");
            sb.AppendLine("        * { margin: 0; padding: 0; box-sizing: border-box; }");
            sb.AppendLine("        body { font-family: Tahoma, Arial, sans-serif; background: #f5f5f5; padding: 20px; }");
            sb.AppendLine("        .container { max-width: 1400px; margin: 0 auto; background: white; padding: 20px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }");
            sb.AppendLine("        .header { text-align: center; margin-bottom: 30px; border-bottom: 3px solid #e74c3c; padding-bottom: 20px; }");
            sb.AppendLine("        .header h1 { color: #e74c3c; font-size: 28px; margin-bottom: 10px; }");
            sb.AppendLine("        .header p { color: #666; font-size: 14px; }");
            sb.AppendLine("        .summary { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 15px; margin-bottom: 30px; }");
            sb.AppendLine("        .stat-card { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 20px; border-radius: 10px; text-align: center; }");
            sb.AppendLine("        .stat-card.miner { background: linear-gradient(135deg, #e74c3c 0%, #c0392b 100%); }");
            sb.AppendLine("        .stat-card h3 { font-size: 32px; margin-bottom: 5px; }");
            sb.AppendLine("        .stat-card p { font-size: 14px; opacity: 0.9; }");
            sb.AppendLine("        .section { margin-bottom: 30px; }");
            sb.AppendLine("        .section h2 { color: #2c3e50; margin-bottom: 15px; padding-bottom: 10px; border-bottom: 2px solid #3498db; }");
            sb.AppendLine("        .info-grid { display: grid; grid-template-columns: repeat(2, 1fr); gap: 10px; margin-bottom: 20px; }");
            sb.AppendLine("        .info-item { padding: 10px; background: #ecf0f1; border-radius: 5px; }");
            sb.AppendLine("        .info-item label { font-weight: bold; color: #2c3e50; display: block; margin-bottom: 5px; }");
            sb.AppendLine("        table { width: 100%; border-collapse: collapse; margin-top: 15px; }");
            sb.AppendLine("        th { background: #3498db; color: white; padding: 12px; text-align: right; }");
            sb.AppendLine("        td { padding: 10px; border-bottom: 1px solid #ddd; }");
            sb.AppendLine("        tr:hover { background: #f5f5f5; }");
            sb.AppendLine("        tr.miner { background: #ffebee; }");
            sb.AppendLine("        .status-online { color: #27ae60; font-weight: bold; }");
            sb.AppendLine("        .status-offline { color: #e74c3c; }");
            sb.AppendLine("        .miner-badge { background: #e74c3c; color: white; padding: 3px 8px; border-radius: 3px; font-size: 12px; }");
            sb.AppendLine("        #map { height: 500px; margin-top: 20px; border-radius: 10px; border: 2px solid #ddd; }");
            sb.AppendLine("        .footer { text-align: center; margin-top: 30px; padding-top: 20px; border-top: 1px solid #ddd; color: #666; font-size: 12px; }");
            sb.AppendLine("        @media print { body { background: white; } .container { box-shadow: none; } }");
            sb.AppendLine("    </style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("    <div class='container'>");

            // Header
            sb.AppendLine("        <div class='header'>");
            sb.AppendLine("            <h1>⛏️ Iranian Miner Detector - Scan Report</h1>");
            sb.AppendLine("            <p>گزارش اسکن شبکه تشخیص ماینر</p>");
            sb.AppendLine($"            <p>Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");
            sb.AppendLine("        </div>");

            // Summary
            sb.AppendLine("        <div class='summary'>");
            sb.AppendLine($"            <div class='stat-card'><h3>{scan.TotalIPs}</h3><p>Total IPs</p></div>");
            sb.AppendLine($"            <div class='stat-card'><h3>{scan.OnlineHosts}</h3><p>Online Hosts</p></div>");
            sb.AppendLine($"            <div class='stat-card miner'><h3>{scan.MinersFound}</h3><p>Miners Found</p></div>");
            var successRate = scan.TotalIPs > 0 ? (scan.OnlineHosts * 100.0 / scan.TotalIPs) : 0;
            sb.AppendLine($"            <div class='stat-card'><h3>{successRate:F1}%</h3><p>Success Rate</p></div>");
            sb.AppendLine("        </div>");

            // Scan Information
            sb.AppendLine("        <div class='section'>");
            sb.AppendLine("            <h2>📊 Scan Information</h2>");
            sb.AppendLine("            <div class='info-grid'>");
            sb.AppendLine($"                <div class='info-item'><label>Scan ID</label>{scan.Id}</div>");
            sb.AppendLine($"                <div class='info-item'><label>Start Time</label>{scan.StartTime:yyyy-MM-dd HH:mm:ss}</div>");
            sb.AppendLine($"                <div class='info-item'><label>End Time</label>{(scan.EndTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "In Progress")}</div>");
            sb.AppendLine($"                <div class='info-item'><label>Status</label>{scan.Status}</div>");
            sb.AppendLine($"                <div class='info-item'><label>Province</label>{scan.Province ?? "All"}</div>");
            sb.AppendLine($"                <div class='info-item'><label>City</label>{scan.City ?? "All"}</div>");
            sb.AppendLine($"                <div class='info-item'><label>ISP</label>{scan.ISP ?? "All"}</div>");
            sb.AppendLine($"                <div class='info-item'><label>Configuration</label><small>{System.Web.HttpUtility.HtmlEncode(scan.Configuration ?? "")}</small></div>");
            sb.AppendLine("            </div>");
            sb.AppendLine("        </div>");

            // Map
            var hostsWithLocation = hosts.Where(h => h.Latitude.HasValue && h.Longitude.HasValue).ToList();
            if (hostsWithLocation.Any())
            {
                sb.AppendLine("        <div class='section'>");
                sb.AppendLine("            <h2>🗺️ Geographic Distribution</h2>");
                sb.AppendLine("            <div id='map'></div>");
                sb.AppendLine("        </div>");
            }

            // Results Table
            sb.AppendLine("        <div class='section'>");
            sb.AppendLine("            <h2>📋 Scan Results</h2>");
            sb.AppendLine("            <table>");
            sb.AppendLine("                <thead>");
            sb.AppendLine("                    <tr>");
            sb.AppendLine("                        <th>IP Address</th>");
            sb.AppendLine("                        <th>Status</th>");
            sb.AppendLine("                        <th>Response (ms)</th>");
            sb.AppendLine("                        <th>Open Ports</th>");
            sb.AppendLine("                        <th>Miner</th>");
            sb.AppendLine("                        <th>Confidence</th>");
            sb.AppendLine("                        <th>Service</th>");
            sb.AppendLine("                        <th>ISP</th>");
            sb.AppendLine("                        <th>Location</th>");
            sb.AppendLine("                    </tr>");
            sb.AppendLine("                </thead>");
            sb.AppendLine("                <tbody>");

            foreach (var host in hosts)
            {
                var rowClass = host.IsMinerDetected ? "class='miner'" : "";
                var statusClass = host.IsOnline ? "status-online" : "status-offline";
                var minerBadge = host.IsMinerDetected ? "<span class='miner-badge'>⛏️ MINER</span>" : "";

                sb.AppendLine($"                    <tr {rowClass}>");
                sb.AppendLine($"                        <td>{host.IPAddress}</td>");
                sb.AppendLine($"                        <td class='{statusClass}'>{(host.IsOnline ? "Online" : "Offline")}</td>");
                sb.AppendLine($"                        <td>{host.ResponseTimeMs?.ToString() ?? "N/A"}</td>");
                sb.AppendLine($"                        <td>{string.Join(", ", host.OpenPorts)}</td>");
                sb.AppendLine($"                        <td>{minerBadge}</td>");
                sb.AppendLine($"                        <td>{host.ConfidenceScore:P2}</td>");
                sb.AppendLine($"                        <td>{host.DetectedService ?? "N/A"}</td>");
                sb.AppendLine($"                        <td>{host.ISP ?? "N/A"}</td>");
                sb.AppendLine($"                        <td>{host.City}, {host.Province}".Trim(',', ' '));
                sb.AppendLine("                    </tr>");
            }

            sb.AppendLine("                </tbody>");
            sb.AppendLine("            </table>");
            sb.AppendLine("        </div>");

            // Footer
            sb.AppendLine("        <div class='footer'>");
            sb.AppendLine("            <p>© 2024 Iranian Network Security - Iranian Miner Detector v1.0.0</p>");
            sb.AppendLine("            <p>This report was generated automatically by the Iranian Miner Detector software.</p>");
            sb.AppendLine("        </div>");

            sb.AppendLine("    </div>");

            // Map script
            if (hostsWithLocation.Any())
            {
                var centerLat = hostsWithLocation.Average(h => h.Latitude ?? 32.4279);
                var centerLon = hostsWithLocation.Average(h => h.Longitude ?? 53.6880);

                sb.AppendLine("    <script>");
                sb.AppendLine($"        var map = L.map('map').setView([{centerLat}, {centerLon}], 6);");
                sb.AppendLine("        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {");
                sb.AppendLine("            attribution: '© OpenStreetMap contributors'");
                sb.AppendLine("        }).addTo(map);");

                foreach (var host in hostsWithLocation)
                {
                    var lat = host.Latitude ?? 0;
                    var lon = host.Longitude ?? 0;
                    var color = host.IsMinerDetected ? "red" : "blue";
                    var popup = System.Web.HttpUtility.JavaScriptStringEncode(
                        $"IP: {host.IPAddress}<br/>" +
                        $"Status: {(host.IsOnline ? "Online" : "Offline")}<br/>" +
                        $"Ports: {string.Join(", ", host.OpenPorts)}<br/>" +
                        (host.IsMinerDetected ? "<strong>⛏️ MINER DETECTED</strong><br/>" : "") +
                        $"Service: {host.DetectedService ?? "N/A"}<br/>" +
                        $"ISP: {host.ISP ?? "N/A"}<br/>" +
                        $"Location: {host.City}, {host.Province}"
                    );

                    sb.AppendLine($"        L.marker([{lat}, {lon}]).addTo(map).bindPopup('{popup}');");

                    if (host.IsMinerDetected)
                    {
                        sb.AppendLine($"        L.circle([{lat}, {lon}], {{color: '{color}', fillColor: '{color}', fillOpacity: 0.3, radius: 50000}}).addTo(map);");
                    }
                }

                sb.AppendLine("    </script>");
            }

            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }

        public string GetReportsDirectory() => _reportsDir;

        private void ComposeHeader(IContainer container)
        {
            container.Column(column =>
            {
                column.Item().AlignCenter().Text("Iranian Miner Detector Report")
                    .FontSize(20).Bold().FontColor(Colors.Red.Darken2);

                column.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                column.Item().Text(text =>
                {
                    text.Span("Generated: ").Bold();
                    text.Span($"{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                });

                column.Item().Text(text =>
                {
                    text.Span("Report Type: ").Bold();
                    text.Span("Network Scan Results");
                });
            });
        }

        private void ComposeContent(IContainer container)
        {
            container.PaddingVertical(40).Column(column =>
            {
                column.Spacing(20);

                column.Item().Element(c => ComposeSummary(c));
                column.Item().Element(c => ComposeResultsTable(c));
            });
        }

        private void ComposeSummary(IContainer container)
        {
            var scans = _db.GetAllScanRecords();
            var lastScan = scans.FirstOrDefault();

            if (lastScan == null) return;

            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(150);
                    columns.RelativeColumn();
                });

                table.Header(header =>
                {
                    header.Cell().ColumnSpan(2).PaddingBottom(10)
                        .Text("Scan Summary").FontSize(14).Bold();
                });

                // Add summary rows
                table.Cell().Text("Scan ID:");
                table.Cell().Text(lastScan.Id.ToString());

                table.Cell().Text("Start Time:");
                table.Cell().Text(lastScan.StartTime.ToString("yyyy-MM-dd HH:mm:ss"));

                table.Cell().Text("End Time:");
                table.Cell().Text(lastScan.EndTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "In Progress");

                table.Cell().Text("Total IPs:");
                table.Cell().Text(lastScan.TotalIPs.ToString());

                table.Cell().Text("Online Hosts:");
                table.Cell().Text(lastScan.OnlineHosts.ToString());

                table.Cell().Text("Miners Found:");
                table.Cell().Text(lastScan.MinersFound.ToString());
            });
        }

        private void ComposeResultsTable(IContainer container)
        {
            var scans = _db.GetAllScanRecords();
            var lastScan = scans.FirstOrDefault();

            if (lastScan == null) return;

            var hosts = _db.GetHostsByScanId(lastScan.Id);
            var miners = hosts.Where(h => h.IsMinerDetected).Take(20).ToList();

            if (!miners.Any())
            {
                container.Text("No miners detected in this scan.").FontSize(12);
                return;
            }

            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(100);
                    columns.ConstantColumn(80);
                    columns.RelativeColumn();
                    columns.ConstantColumn(60);
                });

                table.Header(header =>
                {
                    header.Cell().Padding(5).Text("IP Address").FontSize(10).Bold();
                    header.Cell().Padding(5).Text("Ports").FontSize(10).Bold();
                    header.Cell().Padding(5).Text("Service").FontSize(10).Bold();
                    header.Cell().Padding(5).Text("Confidence").FontSize(10).Bold();
                });

                foreach (var miner in miners)
                {
                    table.Cell().Padding(5).Text(miner.IPAddress).FontSize(9);
                    table.Cell().Padding(5).Text(string.Join(", ", miner.OpenPorts)).FontSize(9);
                    table.Cell().Padding(5).Text(miner.DetectedService ?? "N/A").FontSize(9);
                    table.Cell().Padding(5).Text($"{miner.ConfidenceScore:P0}").FontSize(9);
                }
            });
        }
    }
}
