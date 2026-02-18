using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using IranianMinerDetector.WinForms.Models;

namespace IranianMinerDetector.WinForms.Data
{
    public class DatabaseManager
    {
        private static DatabaseManager? _instance;
        private readonly string _connectionString;
        private readonly string _databasePath;

        public static DatabaseManager Instance => _instance ??= new DatabaseManager();

        private DatabaseManager()
        {
            var appData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "IranianMinerDetector");

            if (!Directory.Exists(appData))
                Directory.CreateDirectory(appData);

            _databasePath = Path.Combine(appData, "iranian_miner_detector.db");
            _connectionString = $"Data Source={_databasePath};Version=3;";
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            // Scan Records table
            using var command1 = connection.CreateCommand();
            command1.CommandText = @"
                CREATE TABLE IF NOT EXISTS ScanRecords (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    StartTime DATETIME NOT NULL,
                    EndTime DATETIME,
                    Province TEXT,
                    City TEXT,
                    ISP TEXT,
                    TotalIPs INTEGER NOT NULL,
                    ScannedIPs INTEGER NOT NULL,
                    OnlineHosts INTEGER NOT NULL,
                    MinersFound INTEGER NOT NULL,
                    Status TEXT NOT NULL,
                    Configuration TEXT
                )";
            command1.ExecuteNonQuery();

            // Host Records table
            using var command2 = connection.CreateCommand();
            command2.CommandText = @"
                CREATE TABLE IF NOT EXISTS HostRecords (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ScanId INTEGER NOT NULL,
                    IPAddress TEXT NOT NULL,
                    IsOnline INTEGER NOT NULL,
                    ResponseTimeMs INTEGER,
                    OpenPorts TEXT,
                    IsMinerDetected INTEGER NOT NULL,
                    ConfidenceScore REAL,
                    DetectedService TEXT,
                    Banner TEXT,
                    ISP TEXT,
                    Province TEXT,
                    City TEXT,
                    Latitude REAL,
                    Longitude REAL,
                    ScannedAt DATETIME NOT NULL,
                    FOREIGN KEY (ScanId) REFERENCES ScanRecords(Id)
                )";
            command2.ExecuteNonQuery();

            // Geolocation Cache table
            using var command3 = connection.CreateCommand();
            command3.CommandText = @"
                CREATE TABLE IF NOT EXISTS GeolocationCache (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    IPAddress TEXT NOT NULL UNIQUE,
                    Country TEXT,
                    Region TEXT,
                    City TEXT,
                    ISP TEXT,
                    Organization TEXT,
                    Latitude REAL,
                    Longitude REAL,
                    CachedAt DATETIME NOT NULL
                )";
            command3.ExecuteNonQuery();

            // Settings table
            using var command4 = connection.CreateCommand();
            command4.CommandText = @"
                CREATE TABLE IF NOT EXISTS Settings (
                    Key TEXT NOT NULL UNIQUE,
                    Value TEXT NOT NULL
                )";
            command4.ExecuteNonQuery();

            // Create indexes
            using var command5 = connection.CreateCommand();
            command5.CommandText = @"
                CREATE INDEX IF NOT EXISTS idx_host_records_scan_id ON HostRecords(ScanId);
                CREATE INDEX IF NOT EXISTS idx_host_records_ip ON HostRecords(IPAddress);
                CREATE INDEX IF NOT EXISTS idx_host_records_miner ON HostRecords(IsMinerDetected);
                CREATE INDEX IF NOT EXISTS idx_geo_ip ON GeolocationCache(IPAddress);
            ";
            command5.ExecuteNonQuery();
        }

        public int CreateScanRecord(ScanRecord record)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO ScanRecords 
                (StartTime, EndTime, Province, City, ISP, TotalIPs, ScannedIPs, 
                 OnlineHosts, MinersFound, Status, Configuration)
                VALUES (@StartTime, @EndTime, @Province, @City, @ISP, @TotalIPs, @ScannedIPs,
                        @OnlineHosts, @MinersFound, @Status, @Configuration);
                SELECT last_insert_rowid();";

            command.Parameters.AddWithValue("@StartTime", record.StartTime);
            command.Parameters.AddWithValue("@EndTime", (object?)record.EndTime ?? DBNull.Value);
            command.Parameters.AddWithValue("@Province", (object?)record.Province ?? DBNull.Value);
            command.Parameters.AddWithValue("@City", (object?)record.City ?? DBNull.Value);
            command.Parameters.AddWithValue("@ISP", (object?)record.ISP ?? DBNull.Value);
            command.Parameters.AddWithValue("@TotalIPs", record.TotalIPs);
            command.Parameters.AddWithValue("@ScannedIPs", record.ScannedIPs);
            command.Parameters.AddWithValue("@OnlineHosts", record.OnlineHosts);
            command.Parameters.AddWithValue("@MinersFound", record.MinersFound);
            command.Parameters.AddWithValue("@Status", record.Status.ToString());
            command.Parameters.AddWithValue("@Configuration", (object?)record.Configuration ?? DBNull.Value);

            return Convert.ToInt32(command.ExecuteScalar());
        }

        public void UpdateScanRecord(ScanRecord record)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE ScanRecords 
                SET EndTime = @EndTime, ScannedIPs = @ScannedIPs, 
                    OnlineHosts = @OnlineHosts, MinersFound = @MinersFound, Status = @Status
                WHERE Id = @Id";

            command.Parameters.AddWithValue("@EndTime", (object?)record.EndTime ?? DBNull.Value);
            command.Parameters.AddWithValue("@ScannedIPs", record.ScannedIPs);
            command.Parameters.AddWithValue("@OnlineHosts", record.OnlineHosts);
            command.Parameters.AddWithValue("@MinersFound", record.MinersFound);
            command.Parameters.AddWithValue("@Status", record.Status.ToString());
            command.Parameters.AddWithValue("@Id", record.Id);

            command.ExecuteNonQuery();
        }

        public int CreateHostRecord(HostRecord record)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO HostRecords 
                (ScanId, IPAddress, IsOnline, ResponseTimeMs, OpenPorts, 
                 IsMinerDetected, ConfidenceScore, DetectedService, Banner,
                 ISP, Province, City, Latitude, Longitude, ScannedAt)
                VALUES (@ScanId, @IPAddress, @IsOnline, @ResponseTimeMs, @OpenPorts,
                        @IsMinerDetected, @ConfidenceScore, @DetectedService, @Banner,
                        @ISP, @Province, @City, @Latitude, @Longitude, @ScannedAt);
                SELECT last_insert_rowid();";

            command.Parameters.AddWithValue("@ScanId", record.ScanId);
            command.Parameters.AddWithValue("@IPAddress", record.IPAddress);
            command.Parameters.AddWithValue("@IsOnline", record.IsOnline ? 1 : 0);
            command.Parameters.AddWithValue("@ResponseTimeMs", (object?)record.ResponseTimeMs ?? DBNull.Value);
            command.Parameters.AddWithValue("@OpenPorts", string.Join(",", record.OpenPorts));
            command.Parameters.AddWithValue("@IsMinerDetected", record.IsMinerDetected ? 1 : 0);
            command.Parameters.AddWithValue("@ConfidenceScore", record.ConfidenceScore);
            command.Parameters.AddWithValue("@DetectedService", (object?)record.DetectedService ?? DBNull.Value);
            command.Parameters.AddWithValue("@Banner", (object?)record.Banner ?? DBNull.Value);
            command.Parameters.AddWithValue("@ISP", (object?)record.ISP ?? DBNull.Value);
            command.Parameters.AddWithValue("@Province", (object?)record.Province ?? DBNull.Value);
            command.Parameters.AddWithValue("@City", (object?)record.City ?? DBNull.Value);
            command.Parameters.AddWithValue("@Latitude", (object?)record.Latitude ?? DBNull.Value);
            command.Parameters.AddWithValue("@Longitude", (object?)record.Longitude ?? DBNull.Value);
            command.Parameters.AddWithValue("@ScannedAt", record.ScannedAt);

            return Convert.ToInt32(command.ExecuteScalar());
        }

        public List<ScanRecord> GetAllScanRecords()
        {
            var records = new List<ScanRecord>();

            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT * FROM ScanRecords 
                ORDER BY StartTime DESC 
                LIMIT 100";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                records.Add(new ScanRecord
                {
                    Id = reader.GetInt32("Id"),
                    StartTime = reader.GetDateTime("StartTime"),
                    EndTime = reader["EndTime"] as DateTime?,
                    Province = reader["Province"] as string,
                    City = reader["City"] as string,
                    ISP = reader["ISP"] as string,
                    TotalIPs = reader.GetInt32("TotalIPs"),
                    ScannedIPs = reader.GetInt32("ScannedIPs"),
                    OnlineHosts = reader.GetInt32("OnlineHosts"),
                    MinersFound = reader.GetInt32("MinersFound"),
                    Status = Enum.Parse<ScanStatus>(reader.GetString("Status")),
                    Configuration = reader["Configuration"] as string
                });
            }

            return records;
        }

        public List<HostRecord> GetHostsByScanId(int scanId)
        {
            var records = new List<HostRecord>();

            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT * FROM HostRecords 
                WHERE ScanId = @ScanId
                ORDER BY IsMinerDetected DESC, IPAddress";

            command.Parameters.AddWithValue("@ScanId", scanId);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                records.Add(new HostRecord
                {
                    Id = reader.GetInt32("Id"),
                    ScanId = reader.GetInt32("ScanId"),
                    IPAddress = reader.GetString("IPAddress"),
                    IsOnline = reader.GetInt32("IsOnline") == 1,
                    ResponseTimeMs = reader["ResponseTimeMs"] as int?,
                    OpenPorts = reader["OpenPorts"] as string != null
                        ? reader.GetString("OpenPorts").Split(',').Select(int.Parse).ToList()
                        : new List<int>(),
                    IsMinerDetected = reader.GetInt32("IsMinerDetected") == 1,
                    ConfidenceScore = reader.GetDouble("ConfidenceScore"),
                    DetectedService = reader["DetectedService"] as string,
                    Banner = reader["Banner"] as string,
                    ISP = reader["ISP"] as string,
                    Province = reader["Province"] as string,
                    City = reader["City"] as string,
                    Latitude = reader["Latitude"] as double?,
                    Longitude = reader["Longitude"] as double?,
                    ScannedAt = reader.GetDateTime("ScannedAt")
                });
            }

            return records;
        }

        public GeolocationData? GetCachedGeolocation(string ipAddress)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT * FROM GeolocationCache 
                WHERE IPAddress = @IPAddress
                AND CachedAt > datetime('now', '-24 hours')";

            command.Parameters.AddWithValue("@IPAddress", ipAddress);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new GeolocationData
                {
                    IPAddress = reader.GetString("IPAddress"),
                    Country = reader["Country"] as string,
                    Region = reader["Region"] as string,
                    City = reader["City"] as string,
                    ISP = reader["ISP"] as string,
                    Organization = reader["Organization"] as string,
                    Latitude = reader["Latitude"] as double?,
                    Longitude = reader["Longitude"] as double?,
                    CachedAt = reader.GetDateTime("CachedAt")
                };
            }

            return null;
        }

        public void CacheGeolocation(GeolocationData data)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT OR REPLACE INTO GeolocationCache
                (IPAddress, Country, Region, City, ISP, Organization, Latitude, Longitude, CachedAt)
                VALUES (@IPAddress, @Country, @Region, @City, @ISP, @Organization, 
                        @Latitude, @Longitude, @CachedAt)";

            command.Parameters.AddWithValue("@IPAddress", data.IPAddress);
            command.Parameters.AddWithValue("@Country", (object?)data.Country ?? DBNull.Value);
            command.Parameters.AddWithValue("@Region", (object?)data.Region ?? DBNull.Value);
            command.Parameters.AddWithValue("@City", (object?)data.City ?? DBNull.Value);
            command.Parameters.AddWithValue("@ISP", (object?)data.ISP ?? DBNull.Value);
            command.Parameters.AddWithValue("@Organization", (object?)data.Organization ?? DBNull.Value);
            command.Parameters.AddWithValue("@Latitude", (object?)data.Latitude ?? DBNull.Value);
            command.Parameters.AddWithValue("@Longitude", (object?)data.Longitude ?? DBNull.Value);
            command.Parameters.AddWithValue("@CachedAt", data.CachedAt);

            command.ExecuteNonQuery();
        }

        public void SaveSetting(string key, string value)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT OR REPLACE INTO Settings (Key, Value)
                VALUES (@Key, @Value)";

            command.Parameters.AddWithValue("@Key", key);
            command.Parameters.AddWithValue("@Value", value);

            command.ExecuteNonQuery();
        }

        public string? GetSetting(string key, string defaultValue = "")
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT Value FROM Settings WHERE Key = @Key";
            command.Parameters.AddWithValue("@Key", key);

            var result = command.ExecuteScalar();
            return result as string ?? defaultValue;
        }

        public string GetDatabasePath() => _databasePath;
    }
}
