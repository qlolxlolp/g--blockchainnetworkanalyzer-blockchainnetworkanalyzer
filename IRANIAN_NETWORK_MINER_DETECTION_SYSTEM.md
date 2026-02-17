# Iranian Network Miner Detection System

## Overview

The **Iranian Network Miner Detection System** is a comprehensive, feature-rich network security auditing tool designed for authorized detection and analysis of cryptocurrency mining activities across Iranian networks. This expanded system provides full province/city coverage (all 31 Iranian provinces), ISP IP range management, advanced network analysis tools, and professional reporting capabilities.

## Key Features

### 1. Geographic Coverage
- **Full Province Coverage**: All 31 Iranian provinces with complete city listings
- **ISP Association**: Map which ISPs operate in which regions
- **Provincial IP Range Management**: Select and scan by province/city
- **Geographic Distribution Analytics**: Visualize miner distribution across Iran

### 2. ISP Management
- **Comprehensive ISP Database**: Major Iranian ISPs including:
  - Iran Telecommunication Company (TCI)
  - Irancell (MTN)
  - RighTel
  - Shatel
  - Pars Online
  - HiWeb
  - AsiaTech
- **IP Range Lookup**: Automatic IP-to-ISP identification
- **ISP Risk Scoring**: Identify high-risk ISPs based on detection statistics
- **RIPE Integration**: Import IP ranges from RIPE NCC database

### 3. Detection Engine
- **Configurable Detection Rules**:
  - Port combination rules
  - Banner signature matching
  - Behavior pattern analysis
  - Custom rule definitions
- **Miner Signatures**: Pre-configured signatures for common miners:
  - CGMiner
  - BFGMiner
  - Ethminer
  - XMRig
- **Confidence Scoring**: Multi-factor confidence calculation
- **False Positive Reduction**: Built-in heuristics

### 4. Network Scanning
- **Multiple Scan Modes**:
  - Single IP
  - IP Range
  - Random IP generation
  - Serial IP generation
  - Custom IP lists
- **Port Scanning**: TCP port scanning with configurable timeouts
- **Service Detection**: Automatic service identification
- **Blockchain Protocol Detection**: Bitcoin, Ethereum, Stratum
- **Fake IP Detection**: VPN/proxy detection capabilities

### 5. Analytics & Reporting
- **Dashboard Summary**: Real-time statistics and metrics
- **ISP Statistics**: Distribution by ISP
- **Geographic Distribution**: Province/city breakdown
- **Miner Trends**: Historical trend analysis
- **Risk Assessment**: Per-IP risk scoring
- **Anomaly Detection**: Automated anomaly identification

### 6. Professional Reporting
- **Multiple Export Formats**:
  - HTML (styled reports)
  - CSV
  - JSON
  - XML
  - Text
  - Nmap-compatible XML
  - Masscan-compatible JSON
- **Scheduled Reports**: Automated report generation
- **Email Notifications**: Automated email delivery
- **Webhook Integration**: External system integration

### 7. Scan Scheduling
- **Flexible Scheduling**:
  - Hourly
  - Daily
  - Weekly
  - Monthly
  - Custom cron expressions
- **Recurring Scans**: Automated repeat scans
- **Scan Comparison**: Before/after analysis
- **Notification System**: Email and webhook alerts

### 8. Data Management
- **Database Backup/Restore**: Full database management
- **Data Import/Export**: Support for multiple formats
- **Compressed Archives**: Package multiple scans
- **Nmap Integration**: Import Nmap scan results
- **Masscan Integration**: Import Masscan results

### 9. User Interface
- **Modern WPF Interface**: Material Design styling
- **Multi-Tab Layout**: Organized by function
- **Real-time Updates**: Live scan progress
- **Data Grids**: Sortable, filterable results
- **Province/City Selector**: Hierarchical selection

## Architecture

### Core Components

```
Core/
├── Models/
│   ├── IranianProvinces.cs       # Province/city data
│   ├── DetectionRule.cs          # Detection rule models
│   ├── ScheduledScan.cs          # Scheduling models
│   ├── Analytics.cs              # Analytics models
│   ├── ScanConfiguration.cs      # Scan settings
│   └── ScanResult.cs             # Result models
├── Services/
│   ├── DetectionEngine.cs        # Rule-based detection
│   ├── ReportingService.cs       # Report generation
│   ├── AnalyticsService.cs       # Statistical analysis
│   ├── SchedulerService.cs       # Scan scheduling
│   ├── DataExchangeService.cs    # Import/export
│   ├── EnhancedISPService.cs     # ISP management
│   ├── ISPService.cs             # Legacy ISP service
│   ├── GeolocationService.cs     # IP geolocation
│   └── ...
├── DatabaseManager.cs            # Database operations
├── NetworkScanner.cs             # Core scanning engine
├── BlockchainAnalyzer.cs         # Blockchain detection
└── ...

Views/
├── MainWindow.xaml               # Main application window
├── ProvinceSelectionWindow.xaml  # Province/city selector
├── MapWindow.xaml               # Geographic visualization
├── MinerTrackerWindow.xaml      # Miner tracking
└── ConsoleWindow.xaml           # Console output

Data/
└── iranian_isps.json            # ISP database
```

### Database Schema

The system uses SQLite with the following key tables:

- **ScanResults**: Scan metadata and results
- **IPResults**: Individual IP scan results
- **DetectionRules**: Configurable detection rules
- **DetectionResults**: Rule match results
- **ScheduledScans**: Scan schedules
- **ScheduleExecutions**: Schedule execution history
- **RiskAssessments**: IP risk scores
- **Anomalies**: Detected anomalies
- **ScanStatistics**: Aggregated statistics

## Legal Compliance

**IMPORTANT**: This tool is designed exclusively for:
- Authorized security auditing
- Network administration
- Legitimate cybersecurity research
- Compliance monitoring

### Requirements
- Written authorization from network owners
- Compliance with Iranian cyberlaw
- Proper documentation of all scan activities
- Audit logging of all operations

### Warnings
- Unauthorized scanning is illegal
- Tool includes prominent legal warnings
- Terms of service acceptance required
- All activities are logged for accountability

## Installation

### Prerequisites
- Windows 10/11 or Windows Server 2019/2022
- .NET 8.0 Runtime
- 4GB RAM minimum (8GB recommended)
- 500MB disk space

### Build Instructions

```bash
# Clone repository
git clone <repository-url>

# Navigate to project directory
cd BlockchainNetworkAnalyzer

# Build project
dotnet build --configuration Release

# Run application
dotnet run --configuration Release
```

### Publish Standalone

```bash
# Publish single-file executable
dotnet publish --configuration Release --self-contained true --runtime win-x64
```

## Usage

### Basic Scan

1. Launch the application
2. Go to "IP Configuration" tab
3. Select IP input mode (Single, Range, Random, etc.)
4. Configure ports and options
5. Click "Start Network Scan"

### Province-Based Scan

1. Click "Select Province" button
2. Choose province and city from the list
3. Fetch IP ranges automatically
4. Configure scan options
5. Start scan

### Setting Up Scheduled Scans

1. Go to "Scheduling" tab
2. Click "Add Schedule"
3. Configure scan parameters
4. Set frequency (Hourly, Daily, Weekly, etc.)
5. Configure notifications
6. Enable the schedule

### Managing Detection Rules

1. Go to "Detection Rules" tab
2. View existing rules
3. Click "Add New Rule" to create custom rules
4. Configure conditions and actions
5. Enable/disable rules as needed

### Exporting Reports

1. Complete a scan
2. Go to "Data Management" tab
3. Select export format (HTML, CSV, JSON, etc.)
4. Click "Export"
5. Choose save location

## Configuration

### appsettings.json

```json
{
  "Database": {
    "ConnectionString": "Data Source=Data\\blockchain_analyzer.db;Version=3;"
  },
  "NetworkScanning": {
    "MaxConcurrentScans": 50,
    "DefaultTimeout": 3000
  },
  "Blockchain": {
    "DefaultPorts": [3333, 4028, 4444, 7777, 8332, 8333, 14433, 14444]
  },
  "Detection": {
    "ConfidenceThreshold": 0.7,
    "EnableRulesEngine": true,
    "EnableSignatures": true
  }
}
```

## API Integration

The system can integrate with external APIs:

### RIPE NCC
- Automatic IP range import
- Iranian network data

### Geolocation Services
- IP geolocation
- Province/city identification

### Notification Services
- Email (SMTP)
- Webhook callbacks

## Performance Considerations

- **Large-Scale Scanning**: Province-level scanning requires careful memory management
- **Database**: SQLite suitable for small-medium deployments
- **PostgreSQL**: Consider for enterprise deployments
- **Concurrent Scans**: Configurable semaphore-based concurrency
- **Result Streaming**: Results processed in real-time

## Security Features

- **Audit Logging**: All actions logged
- **Database Encryption**: SQLite encryption support
- **Input Validation**: All user inputs validated
- **Rate Limiting**: API rate limiting
- **Access Control**: Role-based access (future)

## Troubleshooting

### Common Issues

1. **Database locked**: Ensure single instance running
2. **Permission denied**: Run as administrator
3. **Network timeout**: Adjust timeout settings
4. **Out of memory**: Reduce concurrent scans

### Logs

Logs are stored in:
- Application log: `Logs/app.log`
- Database audit: `AuditLog` table

## Contributing

Contributions are welcome! Please:
1. Fork the repository
2. Create a feature branch
3. Submit a pull request

## License

This project is licensed under the MIT License with additional terms for authorized use only.

## Acknowledgments

- Iranian provinces data: Official government sources
- ISP data: RIPE NCC database
- Icons: Material Design Icons
- UI Framework: MaterialDesignInXaml

## Support

For support and questions:
- Create an issue in the repository
- Contact the development team
- Review documentation

## Roadmap

### Future Enhancements
- [ ] REST API server
- [ ] Web-based interface
- [ ] Machine learning detection
- [ ] Advanced visualization
- [ ] Mobile app companion
- [ ] Farsi language support
- [ ] Integration with Iranian CERTs

---

**Disclaimer**: This tool is intended for authorized security auditing only. Users are responsible for ensuring compliance with all applicable laws and regulations. The developers assume no liability for misuse of this software.
