using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using Microsoft.Extensions.Logging;
using static BlockchainNetworkAnalyzer.App;

namespace BlockchainNetworkAnalyzer.Core
{
    public static class DatabaseManager
    {
        private static string _connectionString;
        private static ILogger _logger;

        static DatabaseManager()
        {
            _connectionString = App.Configuration["Database:ConnectionString"] ?? 
                              "Data Source=Data\\blockchain_analyzer.db;Version=3;";
            _logger = App.LoggerFactory?.CreateLogger(typeof(DatabaseManager));
        }

        public static void Initialize()
        {
            try
            {
                var dbPath = ExtractDbPath(_connectionString);
                var dbDirectory = Path.GetDirectoryName(dbPath);

                if (!string.IsNullOrEmpty(dbDirectory) && !Directory.Exists(dbDirectory))
                {
                    Directory.CreateDirectory(dbDirectory);
                }

                using var connection = new SQLiteConnection(_connectionString);
                connection.Open();

                CreateTables(connection);
                _logger?.LogInformation("Database initialized successfully.");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to initialize database.");
                throw;
            }
        }

        private static string ExtractDbPath(string connectionString)
        {
            var parts = connectionString.Split(';');
            foreach (var part in parts)
            {
                if (part.Trim().StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
                {
                    return part.Substring("Data Source=".Length).Trim();
                }
            }
            return "Data\\blockchain_analyzer.db";
        }

        private static void CreateTables(SQLiteConnection connection)
        {
            // Scan Results Table
            var scanResultsTable = @"
                CREATE TABLE IF NOT EXISTS ScanResults (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ScanType TEXT NOT NULL,
                    StartTime DATETIME NOT NULL,
                    EndTime DATETIME,
                    Status TEXT NOT NULL,
                    TotalIPs INTEGER DEFAULT 0,
                    ScannedIPs INTEGER DEFAULT 0,
                    FoundHosts INTEGER DEFAULT 0,
                    Configuration TEXT,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                )";

            // IP Results Table
            var ipResultsTable = @"
                CREATE TABLE IF NOT EXISTS IPResults (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ScanResultId INTEGER NOT NULL,
                    IPAddress TEXT NOT NULL,
                    Port INTEGER,
                    PortStatus TEXT,
                    Service TEXT,
                    Protocol TEXT,
                    ResponseTime INTEGER,
                    IsFakeIP INTEGER DEFAULT 0,
                    FakeIPReason TEXT,
                    BlockchainDetected INTEGER DEFAULT 0,
                    BlockchainType TEXT,
                    Geolocation TEXT,
                    ISP TEXT,
                    ASN TEXT,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (ScanResultId) REFERENCES ScanResults(Id) ON DELETE CASCADE
                )";

            // Fake IP Database
            var fakeIPTable = @"
                CREATE TABLE IF NOT EXISTS FakeIPDatabase (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    IPAddress TEXT NOT NULL UNIQUE,
                    IPRange TEXT,
                    Source TEXT,
                    DetectionMethod TEXT,
                    ConfidenceLevel REAL,
                    LastUpdated DATETIME DEFAULT CURRENT_TIMESTAMP
                )";

            // Audit Log Table
            var auditLogTable = @"
                CREATE TABLE IF NOT EXISTS AuditLog (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Action TEXT NOT NULL,
                    User TEXT,
                    Details TEXT,
                    IPAddress TEXT,
                    Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP
                )";

            // IP Ranges Table
            var ipRangesTable = @"
                CREATE TABLE IF NOT EXISTS IPRanges (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Province TEXT NOT NULL,
                    City TEXT,
                    StartIP TEXT NOT NULL,
                    EndIP TEXT NOT NULL,
                    CIDR TEXT,
                    Mask TEXT,
                    ISP TEXT NOT NULL,
                    Source TEXT,
                    IsIPv6 INTEGER DEFAULT 0,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                )";

            // Tool Logs Table
            var toolLogsTable = @"
                CREATE TABLE IF NOT EXISTS ToolLogs (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ToolName TEXT NOT NULL,
                    Command TEXT,
                    Output TEXT,
                    ErrorOutput TEXT,
                    ExitCode INTEGER,
                    ExecutionTime INTEGER,
                    StartedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    CompletedAt DATETIME
                )";

            // Geolocation Data Table
            var geolocationTable = @"
                CREATE TABLE IF NOT EXISTS GeolocationData (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    IPAddress TEXT NOT NULL,
                    Latitude REAL NOT NULL,
                    Longitude REAL NOT NULL,
                    Accuracy REAL,
                    Confidence REAL,
                    Address TEXT,
                    Street TEXT,
                    HouseNumber TEXT,
                    City TEXT,
                    Province TEXT,
                    PostalCode TEXT,
                    Country TEXT,
                    Source TEXT,
                    RetrievedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                )";

            // Cell Tower Data Table
            var cellTowerTable = @"
                CREATE TABLE IF NOT EXISTS CellTowerData (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    IPAddress TEXT,
                    MCC TEXT,
                    MNC TEXT,
                    LAC TEXT,
                    CellID TEXT,
                    Latitude REAL,
                    Longitude REAL,
                    Range INTEGER,
                    RadioType TEXT,
                    Operator TEXT,
                    SignalStrength INTEGER,
                    LastSeen DATETIME,
                    Distance REAL,
                    Source TEXT,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                )";

            // Internet Connection Info Table
            var internetConnectionTable = @"
                CREATE TABLE IF NOT EXISTS InternetConnectionInfo (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    IPAddress TEXT NOT NULL UNIQUE,
                    ISP TEXT,
                    Organization TEXT,
                    ASN TEXT,
                    ASName TEXT,
                    ConnectionType TEXT,
                    IsMobile INTEGER DEFAULT 0,
                    HasFiberOptic INTEGER DEFAULT 0,
                    HasDSL INTEGER DEFAULT 0,
                    IsSatellite INTEGER DEFAULT 0,
                    IsResidential INTEGER DEFAULT 0,
                    IsCommercial INTEGER DEFAULT 0,
                    City TEXT,
                    Province TEXT,
                    Country TEXT,
                    AnalyzedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                )";

            // Subscriber Info Table
            var subscriberInfoTable = @"
                CREATE TABLE IF NOT EXISTS SubscriberInfo (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    IPAddress TEXT NOT NULL,
                    FirstName TEXT,
                    LastName TEXT,
                    FullName TEXT,
                    NationalID TEXT,
                    PhoneNumber TEXT,
                    LandlineNumber TEXT,
                    MobileNumber TEXT,
                    Email TEXT,
                    Address TEXT,
                    PostalCode TEXT,
                    Province TEXT,
                    City TEXT,
                    SubscriptionType TEXT,
                    SubscriptionDate DATETIME,
                    AccountNumber TEXT,
                    IsActive INTEGER DEFAULT 1,
                    RetrievedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                )";

            // Map Markers Table
            var mapMarkersTable = @"
                CREATE TABLE IF NOT EXISTS MapMarkers (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    IPAddress TEXT NOT NULL,
                    MarkerType TEXT NOT NULL,
                    Latitude REAL NOT NULL,
                    Longitude REAL NOT NULL,
                    Title TEXT,
                    Description TEXT,
                    IconUrl TEXT,
                    Color TEXT,
                    IsVisible INTEGER DEFAULT 1,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                )";

            ExecuteNonQuery(connection, scanResultsTable);
            ExecuteNonQuery(connection, ipResultsTable);
            ExecuteNonQuery(connection, fakeIPTable);
            ExecuteNonQuery(connection, auditLogTable);
            ExecuteNonQuery(connection, ipRangesTable);
            ExecuteNonQuery(connection, toolLogsTable);
            ExecuteNonQuery(connection, geolocationTable);
            ExecuteNonQuery(connection, cellTowerTable);
            ExecuteNonQuery(connection, internetConnectionTable);
            ExecuteNonQuery(connection, subscriberInfoTable);
            ExecuteNonQuery(connection, mapMarkersTable);

            // Create indexes
            ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS idx_IPResults_ScanResultId ON IPResults(ScanResultId)");
            ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS idx_IPResults_IPAddress ON IPResults(IPAddress)");
            ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS idx_FakeIPDatabase_IPAddress ON FakeIPDatabase(IPAddress)");
            ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS idx_IPRanges_Province ON IPRanges(Province)");
            ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS idx_IPRanges_City ON IPRanges(City)");
            ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS idx_IPRanges_ISP ON IPRanges(ISP)");
            ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS idx_ToolLogs_ToolName ON ToolLogs(ToolName)");
            ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS idx_ToolLogs_StartedAt ON ToolLogs(StartedAt)");
            ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS idx_GeolocationData_IPAddress ON GeolocationData(IPAddress)");
            ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS idx_GeolocationData_LatLng ON GeolocationData(Latitude, Longitude)");
            ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS idx_CellTowerData_IPAddress ON CellTowerData(IPAddress)");
            ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS idx_InternetConnectionInfo_IPAddress ON InternetConnectionInfo(IPAddress)");
            ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS idx_SubscriberInfo_IPAddress ON SubscriberInfo(IPAddress)");
            ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS idx_SubscriberInfo_Province ON SubscriberInfo(Province)");
            ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS idx_MapMarkers_IPAddress ON MapMarkers(IPAddress)");
            ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS idx_MapMarkers_LatLng ON MapMarkers(Latitude, Longitude)");
        }

        private static void ExecuteNonQuery(SQLiteConnection connection, string sql)
        {
            using var command = new SQLiteCommand(sql, connection);
            command.ExecuteNonQuery();
        }

        public static SQLiteConnection GetConnection()
        {
            return new SQLiteConnection(_connectionString);
        }

        public static void LogAudit(string action, string user = null, string details = null, string ipAddress = null)
        {
            try
            {
                using var connection = GetConnection();
                connection.Open();
                var sql = @"INSERT INTO AuditLog (Action, User, Details, IPAddress) 
                           VALUES (@Action, @User, @Details, @IPAddress)";
                using var command = new SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue("@Action", action);
                command.Parameters.AddWithValue("@User", user ?? Environment.UserName);
                command.Parameters.AddWithValue("@Details", details ?? "");
                command.Parameters.AddWithValue("@IPAddress", ipAddress ?? "");
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to log audit entry.");
            }
        }
    }
}
