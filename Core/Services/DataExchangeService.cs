using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using BlockchainNetworkAnalyzer.Core.Models;
using static BlockchainNetworkAnalyzer.App;
using Newtonsoft.Json;

namespace BlockchainNetworkAnalyzer.Core.Services
{
    public class DataExchangeService
    {
        private readonly ILogger<DataExchangeService> _logger;
        private readonly string _exportDirectory;
        private readonly string _importDirectory;

        public DataExchangeService()
        {
            _logger = App.LoggerFactory.CreateLogger<DataExchangeService>();
            _exportDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Exports");
            _importDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Imports");
            Directory.CreateDirectory(_exportDirectory);
            Directory.CreateDirectory(_importDirectory);
        }

        #region Export Functions

        public async Task<string> ExportScanDataAsync(long scanId, ExportFormat format)
        {
            try
            {
                var scanResult = await GetScanResultWithDetailsAsync(scanId);
                if (scanResult == null)
                    throw new ArgumentException($"Scan with ID {scanId} not found");

                string filePath;
                switch (format)
                {
                    case ExportFormat.Json:
                        filePath = await ExportToJsonAsync(scanResult);
                        break;
                    case ExportFormat.Xml:
                        filePath = await ExportToXmlAsync(scanResult);
                        break;
                    case ExportFormat.Csv:
                        filePath = await ExportToCsvAsync(scanResult);
                        break;
                    case ExportFormat.Nmap:
                        filePath = await ExportToNmapAsync(scanResult);
                        break;
                    case ExportFormat.Masscan:
                        filePath = await ExportToMasscanAsync(scanResult);
                        break;
                    default:
                        throw new ArgumentException("Unsupported export format");
                }

                _logger.LogInformation($"Exported scan {scanId} to {format}: {filePath}");
                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error exporting scan {scanId}");
                throw;
            }
        }

        public async Task<string> ExportDatabaseBackupAsync()
        {
            try
            {
                var backupFileName = $"Backup_{DateTime.Now:yyyyMMdd_HHmmss}.db";
                var backupPath = Path.Combine(_exportDirectory, backupFileName);

                using var sourceConnection = DatabaseManager.GetConnection();
                sourceConnection.Open();

                // Create backup using SQLite backup API
                var backupConnection = new SQLiteConnection($"Data Source={backupPath};Version=3;");
                backupConnection.Open();
                sourceConnection.BackupDatabase(backupConnection, "main", "main", -1, null, 0);
                backupConnection.Close();

                _logger.LogInformation($"Database backup created: {backupPath}");
                return backupPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating database backup");
                throw;
            }
        }

        public async Task<string> ExportCompressedArchiveAsync(List<long> scanIds)
        {
            try
            {
                var archiveName = $"Export_Package_{DateTime.Now:yyyyMMdd_HHmmss}.zip";
                var archivePath = Path.Combine(_exportDirectory, archiveName);

                using (var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create))
                {
                    foreach (var scanId in scanIds)
                    {
                        var jsonPath = await ExportScanDataAsync(scanId, ExportFormat.Json);
                        var entryName = $"scan_{scanId}.json";
                        archive.CreateEntryFromFile(jsonPath, entryName);
                    }

                    // Add metadata
                    var metadata = new
                    {
                        ExportDate = DateTime.UtcNow,
                        ScanIds = scanIds,
                        Application = "Iranian Network Miner Detection System",
                        Version = "1.0"
                    };
                    var metadataJson = JsonConvert.SerializeObject(metadata, Formatting.Indented);
                    var metadataEntry = archive.CreateEntry("metadata.json");
                    using (var entryStream = metadataEntry.Open())
                    using (var streamWriter = new StreamWriter(entryStream))
                    {
                        await streamWriter.WriteAsync(metadataJson);
                    }
                }

                _logger.LogInformation($"Compressed archive created: {archivePath}");
                return archivePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating compressed archive");
                throw;
            }
        }

        #endregion

        #region Import Functions

        public async Task<long> ImportScanDataAsync(string filePath, ImportFormat format)
        {
            try
            {
                if (!File.Exists(filePath))
                    throw new FileNotFoundException("Import file not found", filePath);

                long scanId;
                switch (format)
                {
                    case ImportFormat.Json:
                        scanId = await ImportFromJsonAsync(filePath);
                        break;
                    case ImportFormat.Xml:
                        scanId = await ImportFromXmlAsync(filePath);
                        break;
                    case ImportFormat.Csv:
                        scanId = await ImportFromCsvAsync(filePath);
                        break;
                    case ImportFormat.Nmap:
                        scanId = await ImportFromNmapAsync(filePath);
                        break;
                    default:
                        throw new ArgumentException("Unsupported import format");
                }

                _logger.LogInformation($"Imported scan from {filePath}, new ID: {scanId}");
                return scanId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error importing from {filePath}");
                throw;
            }
        }

        public async Task<long> RestoreDatabaseBackupAsync(string backupPath)
        {
            try
            {
                if (!File.Exists(backupPath))
                    throw new FileNotFoundException("Backup file not found", backupPath);

                // Create safety backup of current database
                var safetyBackup = Path.Combine(_exportDirectory, $"SafetyBackup_{DateTime.Now:yyyyMMdd_HHmmss}.db");
                await ExportDatabaseBackupAsync();

                // Get current database path
                var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "blockchain_analyzer.db");

                // Restore from backup
                File.Copy(backupPath, dbPath, true);

                _logger.LogInformation($"Database restored from: {backupPath}");
                return 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring database backup");
                throw;
            }
        }

        public async Task<List<ImportResult>> BulkImportAsync(string directoryPath, ImportFormat format)
        {
            var results = new List<ImportResult>();

            try
            {
                if (!Directory.Exists(directoryPath))
                    throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");

                var extension = format switch
                {
                    ImportFormat.Json => "*.json",
                    ImportFormat.Xml => "*.xml",
                    ImportFormat.Csv => "*.csv",
                    _ => "*.*"
                };

                var files = Directory.GetFiles(directoryPath, extension);

                foreach (var file in files)
                {
                    try
                    {
                        var scanId = await ImportScanDataAsync(file, format);
                        results.Add(new ImportResult
                        {
                            FilePath = file,
                            Success = true,
                            ScanId = scanId
                        });
                    }
                    catch (Exception ex)
                    {
                        results.Add(new ImportResult
                        {
                            FilePath = file,
                            Success = false,
                            ErrorMessage = ex.Message
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk import");
                throw;
            }

            return results;
        }

        #endregion

        #region Private Export Methods

        private async Task<string> ExportToJsonAsync(ScanResult scanResult)
        {
            var fileName = $"Export_Scan_{scanResult.Id}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            var filePath = Path.Combine(_exportDirectory, fileName);

            var export = new ScanExport
            {
                Scan = scanResult,
                ExportMetadata = new ExportMetadata
                {
                    ExportedAt = DateTime.UtcNow,
                    ExportedBy = Environment.UserName,
                    Application = "Iranian Network Miner Detection System",
                    Version = "1.0"
                }
            };

            var json = JsonConvert.SerializeObject(export, Formatting.Indented);
            await File.WriteAllTextAsync(filePath, json, Encoding.UTF8);

            return filePath;
        }

        private async Task<string> ExportToXmlAsync(ScanResult scanResult)
        {
            var fileName = $"Export_Scan_{scanResult.Id}_{DateTime.Now:yyyyMMdd_HHmmss}.xml";
            var filePath = Path.Combine(_exportDirectory, fileName);

            var doc = new XDocument(
                new XElement("ScanExport",
                    new XElement("Metadata",
                        new XElement("ExportedAt", DateTime.UtcNow),
                        new XElement("ExportedBy", Environment.UserName),
                        new XElement("Application", "Iranian Network Miner Detection System")
                    ),
                    new XElement("Scan",
                        new XAttribute("Id", scanResult.Id),
                        new XElement("ScanType", scanResult.ScanType),
                        new XElement("StartTime", scanResult.StartTime),
                        new XElement("EndTime", scanResult.EndTime),
                        new XElement("Status", scanResult.Status),
                        new XElement("TotalIPs", scanResult.TotalIPs),
                        new XElement("ScannedIPs", scanResult.ScannedIPs),
                        new XElement("FoundHosts", scanResult.FoundHosts),
                        new XElement("Results",
                            scanResult.IPResults.Select(r =>
                                new XElement("IPResult",
                                    new XElement("IPAddress", r.IPAddress),
                                    new XElement("Port", r.Port),
                                    new XElement("PortStatus", r.PortStatus),
                                    new XElement("Service", r.Service),
                                    new XElement("BlockchainDetected", r.BlockchainDetected),
                                    new XElement("BlockchainType", r.BlockchainType),
                                    new XElement("IsFakeIP", r.IsFakeIP),
                                    new XElement("ISP", r.ISP)
                                )
                            )
                        )
                    )
                )
            );

            await Task.Run(() => doc.Save(filePath));
            return filePath;
        }

        private async Task<string> ExportToCsvAsync(ScanResult scanResult)
        {
            var fileName = $"Export_Scan_{scanResult.Id}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            var filePath = Path.Combine(_exportDirectory, fileName);

            var csv = new StringBuilder();
            csv.AppendLine("IPAddress,Port,PortStatus,Service,Protocol,BlockchainDetected,BlockchainType,IsFakeIP,FakeIPReason,ISP,Geolocation,ResponseTime,CreatedAt");

            foreach (var result in scanResult.IPResults)
            {
                csv.AppendLine($"{result.IPAddress},{result.Port},{result.PortStatus},{result.Service},{result.Protocol},{result.BlockchainDetected},{result.BlockchainType},{result.IsFakeIP},\"{result.FakeIPReason}\",{result.ISP},\"{result.Geolocation}\",{result.ResponseTime},{result.CreatedAt:yyyy-MM-dd HH:mm:ss}");
            }

            await File.WriteAllTextAsync(filePath, csv.ToString(), Encoding.UTF8);
            return filePath;
        }

        private async Task<string> ExportToNmapAsync(ScanResult scanResult)
        {
            var fileName = $"Export_Scan_{scanResult.Id}_{DateTime.Now:yyyyMMdd_HHmmss}.xml";
            var filePath = Path.Combine(_exportDirectory, fileName);

            var doc = new XDocument(
                new XElement("nmaprun",
                    new XAttribute("scanner", "IranianNetworkMinerDetector"),
                    new XAttribute("start", scanResult.StartTime.ToUnixTimeSeconds()),
                    new XAttribute("version", "1.0"),
                    new XElement("host",
                        scanResult.IPResults.Select(r =>
                            new XElement("address",
                                new XAttribute("addr", r.IPAddress),
                                new XAttribute("addrtype", "ipv4")
                            )
                        )
                    )
                )
            );

            await Task.Run(() => doc.Save(filePath));
            return filePath;
        }

        private async Task<string> ExportToMasscanAsync(ScanResult scanResult)
        {
            var fileName = $"Export_Scan_{scanResult.Id}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            var filePath = Path.Combine(_exportDirectory, fileName);

            var masscanResults = scanResult.IPResults.Select(r => new
            {
                ip = r.IPAddress,
                ports = new[] { new { port = r.Port, proto = r.Protocol?.ToLower() ?? "tcp", status = r.PortStatus?.ToLower() ?? "open" } }
            });

            var json = JsonConvert.SerializeObject(masscanResults, Formatting.Indented);
            await File.WriteAllTextAsync(filePath, json, Encoding.UTF8);
            return filePath;
        }

        #endregion

        #region Private Import Methods

        private async Task<long> ImportFromJsonAsync(string filePath)
        {
            var json = await File.ReadAllTextAsync(filePath);
            var export = JsonConvert.DeserializeObject<ScanExport>(json);

            if (export?.Scan == null)
                throw new InvalidDataException("Invalid JSON format");

            return await ImportScanResultAsync(export.Scan);
        }

        private async Task<long> ImportFromXmlAsync(string filePath)
        {
            var doc = await Task.Run(() => XDocument.Load(filePath));
            var scanElement = doc.Root?.Element("Scan");

            if (scanElement == null)
                throw new InvalidDataException("Invalid XML format");

            var scanResult = new ScanResult
            {
                ScanType = scanElement.Element("ScanType")?.Value ?? "Imported",
                StartTime = DateTime.Parse(scanElement.Element("StartTime")?.Value ?? DateTime.Now.ToString()),
                EndTime = scanElement.Element("EndTime") != null ? DateTime.Parse(scanElement.Element("EndTime").Value) : null,
                Status = scanElement.Element("Status")?.Value ?? "Completed",
                TotalIPs = int.Parse(scanElement.Element("TotalIPs")?.Value ?? "0"),
                ScannedIPs = int.Parse(scanElement.Element("ScannedIPs")?.Value ?? "0"),
                FoundHosts = int.Parse(scanElement.Element("FoundHosts")?.Value ?? "0"),
                IPResults = scanElement.Element("Results")?.Elements("IPResult").Select(e => new IPResult
                {
                    IPAddress = e.Element("IPAddress")?.Value ?? "",
                    Port = int.TryParse(e.Element("Port")?.Value, out var port) ? port : null,
                    PortStatus = e.Element("PortStatus")?.Value ?? "",
                    Service = e.Element("Service")?.Value ?? "",
                    BlockchainDetected = bool.TryParse(e.Element("BlockchainDetected")?.Value, out var bd) && bd,
                    BlockchainType = e.Element("BlockchainType")?.Value ?? "",
                    IsFakeIP = bool.TryParse(e.Element("IsFakeIP")?.Value, out var fake) && fake,
                    ISP = e.Element("ISP")?.Value ?? ""
                }).ToList() ?? new List<IPResult>()
            };

            return await ImportScanResultAsync(scanResult);
        }

        private async Task<long> ImportFromCsvAsync(string filePath)
        {
            var lines = await File.ReadAllLinesAsync(filePath);
            var results = new List<IPResult>();

            // Skip header
            for (int i = 1; i < lines.Length; i++)
            {
                var parts = ParseCsvLine(lines[i]);
                if (parts.Length >= 10)
                {
                    results.Add(new IPResult
                    {
                        IPAddress = parts[0],
                        Port = int.TryParse(parts[1], out var port) ? port : null,
                        PortStatus = parts[2],
                        Service = parts[3],
                        Protocol = parts[4],
                        BlockchainDetected = bool.TryParse(parts[5], out var bd) && bd,
                        BlockchainType = parts[6],
                        IsFakeIP = bool.TryParse(parts[7], out var fake) && fake,
                        FakeIPReason = parts[8],
                        ISP = parts[9],
                        Geolocation = parts.Length > 10 ? parts[10] : ""
                    });
                }
            }

            var scanResult = new ScanResult
            {
                ScanType = "Imported from CSV",
                StartTime = DateTime.Now,
                EndTime = DateTime.Now,
                Status = "Completed",
                TotalIPs = results.Count,
                ScannedIPs = results.Count,
                FoundHosts = results.Count(r => !string.IsNullOrEmpty(r.PortStatus)),
                IPResults = results
            };

            return await ImportScanResultAsync(scanResult);
        }

        private async Task<long> ImportFromNmapAsync(string filePath)
        {
            var doc = await Task.Run(() => XDocument.Load(filePath));
            var results = new List<IPResult>();

            foreach (var host in doc.Descendants("host"))
            {
                var address = host.Element("address")?.Attribute("addr")?.Value ?? "";
                var ports = host.Element("ports")?.Elements("port");

                if (ports != null)
                {
                    foreach (var port in ports)
                    {
                        var portId = int.Parse(port.Attribute("portid")?.Value ?? "0");
                        var state = port.Element("state")?.Attribute("state")?.Value ?? "";
                        var service = port.Element("service")?.Attribute("name")?.Value ?? "";

                        results.Add(new IPResult
                        {
                            IPAddress = address,
                            Port = portId,
                            PortStatus = state == "open" ? "Open" : "Closed",
                            Service = service
                        });
                    }
                }
            }

            var scanResult = new ScanResult
            {
                ScanType = "Imported from Nmap",
                StartTime = DateTime.Now,
                EndTime = DateTime.Now,
                Status = "Completed",
                TotalIPs = results.Select(r => r.IPAddress).Distinct().Count(),
                ScannedIPs = results.Count,
                FoundHosts = results.Count(r => r.PortStatus == "Open"),
                IPResults = results
            };

            return await ImportScanResultAsync(scanResult);
        }

        private async Task<long> ImportScanResultAsync(ScanResult scanResult)
        {
            using var connection = DatabaseManager.GetConnection();
            connection.Open();

            using var transaction = connection.BeginTransaction();
            try
            {
                // Insert scan result
                var scanSql = @"
                    INSERT INTO ScanResults (ScanType, StartTime, EndTime, Status, TotalIPs, ScannedIPs, FoundHosts, Configuration)
                    VALUES (@Type, @Start, @End, @Status, @Total, @Scanned, @Found, @Config);
                    SELECT last_insert_rowid();";

                using var scanCmd = new SQLiteCommand(scanSql, connection, transaction);
                scanCmd.Parameters.AddWithValue("@Type", scanResult.ScanType);
                scanCmd.Parameters.AddWithValue("@Start", scanResult.StartTime);
                scanCmd.Parameters.AddWithValue("@End", (object)scanResult.EndTime ?? DBNull.Value);
                scanCmd.Parameters.AddWithValue("@Status", scanResult.Status);
                scanCmd.Parameters.AddWithValue("@Total", scanResult.TotalIPs);
                scanCmd.Parameters.AddWithValue("@Scanned", scanResult.ScannedIPs);
                scanCmd.Parameters.AddWithValue("@Found", scanResult.FoundHosts);
                scanCmd.Parameters.AddWithValue("@Config", scanResult.Configuration ?? "");

                var scanId = Convert.ToInt64(await scanCmd.ExecuteScalarAsync());

                // Insert IP results
                var ipSql = @"
                    INSERT INTO IPResults 
                    (ScanResultId, IPAddress, Port, PortStatus, Service, Protocol, ResponseTime,
                     IsFakeIP, FakeIPReason, BlockchainDetected, BlockchainType, Geolocation, ISP, ASN, CreatedAt)
                    VALUES 
                    (@ScanId, @IP, @Port, @PortStatus, @Service, @Protocol, @ResponseTime,
                     @IsFakeIP, @FakeReason, @Blockchain, @BlockchainType, @Geo, @ISP, @ASN, @CreatedAt)";

                foreach (var result in scanResult.IPResults)
                {
                    using var ipCmd = new SQLiteCommand(ipSql, connection, transaction);
                    ipCmd.Parameters.AddWithValue("@ScanId", scanId);
                    ipCmd.Parameters.AddWithValue("@IP", result.IPAddress);
                    ipCmd.Parameters.AddWithValue("@Port", (object)result.Port ?? DBNull.Value);
                    ipCmd.Parameters.AddWithValue("@PortStatus", result.PortStatus ?? "");
                    ipCmd.Parameters.AddWithValue("@Service", result.Service ?? "");
                    ipCmd.Parameters.AddWithValue("@Protocol", result.Protocol ?? "");
                    ipCmd.Parameters.AddWithValue("@ResponseTime", (object)result.ResponseTime ?? DBNull.Value);
                    ipCmd.Parameters.AddWithValue("@IsFakeIP", result.IsFakeIP ? 1 : 0);
                    ipCmd.Parameters.AddWithValue("@FakeReason", result.FakeIPReason ?? "");
                    ipCmd.Parameters.AddWithValue("@Blockchain", result.BlockchainDetected ? 1 : 0);
                    ipCmd.Parameters.AddWithValue("@BlockchainType", result.BlockchainType ?? "");
                    ipCmd.Parameters.AddWithValue("@Geo", result.Geolocation ?? "");
                    ipCmd.Parameters.AddWithValue("@ISP", result.ISP ?? "");
                    ipCmd.Parameters.AddWithValue("@ASN", result.ASN ?? "");
                    ipCmd.Parameters.AddWithValue("@CreatedAt", result.CreatedAt);

                    await ipCmd.ExecuteNonQueryAsync();
                }

                transaction.Commit();
                return scanId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        #endregion

        #region Helper Methods

        private async Task<ScanResult> GetScanResultWithDetailsAsync(long scanId)
        {
            using var connection = DatabaseManager.GetConnection();
            connection.Open();

            // Get scan result
            var scanSql = "SELECT * FROM ScanResults WHERE Id = @Id";
            using var scanCmd = new SQLiteCommand(scanSql, connection);
            scanCmd.Parameters.AddWithValue("@Id", scanId);

            ScanResult scanResult = null;
            using (var reader = await scanCmd.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    scanResult = new ScanResult
                    {
                        Id = reader.GetInt64(0),
                        ScanType = reader.GetString(1),
                        StartTime = reader.GetDateTime(2),
                        EndTime = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                        Status = reader.GetString(4),
                        TotalIPs = reader.GetInt32(5),
                        ScannedIPs = reader.GetInt32(6),
                        FoundHosts = reader.GetInt32(7),
                        Configuration = reader.IsDBNull(8) ? "" : reader.GetString(8),
                        IPResults = new List<IPResult>()
                    };
                }
            }

            if (scanResult == null) return null;

            // Get IP results
            var ipSql = "SELECT * FROM IPResults WHERE ScanResultId = @ScanId";
            using var ipCmd = new SQLiteCommand(ipSql, connection);
            ipCmd.Parameters.AddWithValue("@ScanId", scanId);

            using var ipReader = await ipCmd.ExecuteReaderAsync();
            while (await ipReader.ReadAsync())
            {
                scanResult.IPResults.Add(new IPResult
                {
                    Id = ipReader.GetInt64(0),
                    ScanResultId = ipReader.GetInt64(1),
                    IPAddress = ipReader.GetString(2),
                    Port = ipReader.IsDBNull(3) ? null : ipReader.GetInt32(3),
                    PortStatus = ipReader.IsDBNull(4) ? "" : ipReader.GetString(4),
                    Service = ipReader.IsDBNull(5) ? "" : ipReader.GetString(5),
                    Protocol = ipReader.IsDBNull(6) ? "" : ipReader.GetString(6),
                    ResponseTime = ipReader.IsDBNull(7) ? null : ipReader.GetInt32(7),
                    IsFakeIP = ipReader.GetInt32(8) == 1,
                    FakeIPReason = ipReader.IsDBNull(9) ? "" : ipReader.GetString(9),
                    BlockchainDetected = ipReader.GetInt32(10) == 1,
                    BlockchainType = ipReader.IsDBNull(11) ? "" : ipReader.GetString(11),
                    Geolocation = ipReader.IsDBNull(12) ? "" : ipReader.GetString(12),
                    ISP = ipReader.IsDBNull(13) ? "" : ipReader.GetString(13),
                    ASN = ipReader.IsDBNull(14) ? "" : ipReader.GetString(14),
                    CreatedAt = ipReader.GetDateTime(15)
                });
            }

            return scanResult;
        }

        private string[] ParseCsvLine(string line)
        {
            var result = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            result.Add(current.ToString());
            return result.ToArray();
        }

        #endregion

        public string GetExportDirectory() => _exportDirectory;
        public string GetImportDirectory() => _importDirectory;
    }

    public enum ExportFormat
    {
        Json,
        Xml,
        Csv,
        Nmap,
        Masscan
    }

    public enum ImportFormat
    {
        Json,
        Xml,
        Csv,
        Nmap
    }

    public class ScanExport
    {
        public ScanResult Scan { get; set; }
        public ExportMetadata ExportMetadata { get; set; }
    }

    public class ExportMetadata
    {
        public DateTime ExportedAt { get; set; }
        public string ExportedBy { get; set; }
        public string Application { get; set; }
        public string Version { get; set; }
    }

    public class ImportResult
    {
        public string FilePath { get; set; }
        public bool Success { get; set; }
        public long ScanId { get; set; }
        public string ErrorMessage { get; set; }
    }

    public static class DateTimeExtensions
    {
        public static long ToUnixTimeSeconds(this DateTime dateTime)
        {
            return new DateTimeOffset(dateTime).ToUnixTimeSeconds();
        }
    }
}
