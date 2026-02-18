# Iranian Miner Detector - WinForms Edition

A standalone Windows Forms desktop application for detecting cryptocurrency mining operations in Iranian networks.

## Features

### Core Functionality
- **Network Scanning**: TCP port scanning with configurable ports and timeouts
- **Mining Detection**: Identifies cryptocurrency mining operations using known mining ports
- **Geographic Selection**: Filter scans by Iranian province and city
- **ISP Filtering**: Target specific Iranian ISPs (TCI, Irancell, RighTel, Shatel, etc.)
- **Banner Grabbing**: Detects mining services and protocols

### Data & Visualization
- **Interactive Maps**: WebView2-powered Leaflet.js maps showing scan results
- **Heatmaps**: Visual representation of mining activity density
- **Results Table**: Detailed host information with mining status
- **Statistics**: Real-time progress and detection statistics

### Reporting
- **PDF Reports**: Professional PDF reports using QuestPDF
- **Excel Reports**: Detailed spreadsheets using ClosedXML
- **CSV Export**: Raw data export for further analysis

### Database
- **SQLite Storage**: Local database for scan history and results
- **Geolocation Cache**: Cached IP geolocation data
- **Settings Persistence**: Application settings saved to database

## System Requirements

- **OS**: Windows 10 or Windows 11
- **.NET Runtime**: 8.0 (included in self-contained build)
- **WebView2 Runtime**: Microsoft Edge WebView2 (usually pre-installed on Windows 10/11)
- **Memory**: 4GB RAM recommended
- **Disk Space**: 100MB for application, additional space for reports

## Installation

### Self-Contained Distribution
1. Download the self-contained build (IranianMinerDetector.WinForms.exe)
2. Run the executable directly - no installation required
3. First run will create the application data folder in AppData

### Development Build
```bash
# Build the project
dotnet build

# Publish self-contained executable
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

## Usage

### Quick Start
1. Launch the application
2. Select a province from the dropdown (e.g., Tehran)
3. Optionally select a city and ISP
4. Click "Start Scan" to begin scanning
5. View results in the Results tab
6. Check the Map tab for visual representation
7. Export reports using the File menu

### Advanced Configuration

#### Province/City Selection
- Choose from 31 Iranian provinces
- Cities automatically populate based on province selection
- Filter by ISP to target specific networks

#### IP Range Specification
- CIDR notation: `192.168.1.0/24`
- Range notation: `192.168.1.1-192.168.1.100`
- Leave empty for automatic generation based on selection

#### Port Configuration
Default mining ports:
- Bitcoin: 8332 (RPC), 8333 (P2P), 3333 (Stratum)
- Ethereum: 30303 (P2P), 8545 (RPC)
- Litecoin: 9332 (RPC), 9333 (P2P)
- Monero: 18081 (P2P)
- Stratum: 4028, 4444, 5050, 8888

Custom ports can be entered in the Ports field (comma-separated).

#### Performance Tuning
- **Timeout**: Connection timeout in milliseconds (default: 3000)
- **Concurrency**: Number of simultaneous scans (default: 100)
- Increase concurrency for faster scans on fast networks
- Decrease timeout for quicker failure detection

## File Structure

```
IranianMinerDetector.WinForms/
├── Forms/
│   └── MainForm.cs              # Main application window
├── Models/
│   ├── Province.cs              # Province and city models
│   ├── ISPInfo.cs               # ISP information and IP ranges
│   └── ScanModels.cs            # Scan and host record models
├── Data/
│   ├── IranianGeography.cs      # 31 Iranian provinces with cities
│   ├── IranianISPs.cs           # Iranian ISP data with IP ranges
│   └── DatabaseManager.cs       # SQLite database operations
├── Services/
│   ├── NetworkScanner.cs        # Network scanning implementation
│   ├── GeolocationService.cs    # IP geolocation lookup
│   ├── MapService.cs            # Interactive map generation
│   └── ReportService.cs         # PDF/Excel/CSV report generation
├── Program.cs                   # Application entry point
├── appsettings.json             # Configuration file
└── build.bat                    # Build script
```

## Data Files

### Iranian Geography
- 31 provinces with Persian and English names
- Cities with coordinates for each province
- Total: ~200+ cities

### Iranian ISPs
- TCI (Telecommunication Company of Iran)
- Irancell (MTN Iran)
- RighTel
- Shatel
- Pars Online
- HiWeb
- AsiaTech
- And more...

### Known Mining Ports
- Bitcoin, Ethereum, Litecoin, Monero
- Stratum protocol variants
- Configurable port lists

## Reports

### PDF Reports
- Scan summary with statistics
- Detected miners with details
- IP addresses, ports, and services
- Geolocation information
- Time-stamped for tracking

### Excel Reports
- Scan information sheet
- Detailed results table
- Statistics and calculations
- Formatted with colors highlighting miners

### CSV Reports
- Raw data export
- All fields included
- Compatible with spreadsheet applications

## Database

The application uses SQLite for local data storage:

**Tables**:
- `ScanRecords`: Scan operation history
- `HostRecords`: Scanned host information
- `GeolocationCache`: Cached geolocation data
- `Settings`: Application settings

**Location**: `%LOCALAPPDATA%\IranianMinerDetector\`

## Configuration

Edit `appsettings.json` to customize:

```json
{
  "Database": {
    "ConnectionString": "Data Source=iranian_miner_detector.db;Version=3;"
  },
  "Network": {
    "DefaultTimeoutMs": 3000,
    "MaxConcurrency": 100
  },
  "Geolocation": {
    "ApiUrl": "http://ip-api.com/json/{0}",
    "CacheDurationHours": 24
  }
}
```

## Troubleshooting

### WebView2 Issues
If maps don't display:
1. Install WebView2 Runtime: https://developer.microsoft.com/en-us/microsoft-edge/webview2/
2. Restart the application

### Network Scanning Issues
- Run as Administrator if scans fail
- Check Windows Firewall settings
- Verify network connectivity
- Increase timeout for slow networks

### Database Issues
- Delete `iranian_miner_detector.db` to reset
- Check file permissions in AppData
- Ensure sufficient disk space

## Security Considerations

- Network scanning may trigger antivirus alerts
- Add to antivirus exclusions if needed
- Requires appropriate network permissions
- Administrator privileges recommended for full functionality

## Development

### Building
```bash
dotnet build -c Release
```

### Publishing (Self-contained)
```bash
dotnet publish -c Release -r win-x64 --self-contained \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:EnableCompressionInSingleFile=true \
  -p:PublishReadyToRun=true
```

### Output Size
- Debug: ~50MB
- Release: ~40MB
- Self-contained: ~80-100MB (includes .NET runtime)

## License

Copyright © 2024 Iranian Network Security

## Credits

This application uses:
- **QuestPDF**: MIT License - PDF generation
- **ClosedXML**: MIT License - Excel generation
- **Leaflet.js**: BSD 2-Clause - Map visualization
- **System.Data.SQLite**: Public Domain - SQLite database

## Support

For issues, questions, or contributions, please refer to the project repository.

---

**Version**: 1.0.0
**Release Date**: 2024
**Platform**: Windows 10/11 (.NET 8)
