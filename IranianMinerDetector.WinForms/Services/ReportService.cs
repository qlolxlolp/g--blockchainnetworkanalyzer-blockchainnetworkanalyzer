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
