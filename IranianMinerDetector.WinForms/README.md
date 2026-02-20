# Iranian Miner Detector - WinForms Edition

A standalone Windows Forms desktop application for detecting cryptocurrency mining operations on Iranian networks.

## Features

### Core Functionality
- **Network TCP Port Scanning** - Configurable port scanning (default: 3333, 4028, 4444, 8332, 8333, 30303, 8545, 18081, 9332, 9333)
- **Miner Detection** - Protocol-based detection of Bitcoin, Ethereum, Monero, Litecoin, and other cryptocurrency miners
- **Multi-threaded Scanning** - Concurrent scanning with configurable concurrency levels
- **Real-time Progress** - Live progress updates and statistics

### Iranian Network Support
- **31 Iranian Provinces** - Complete coverage with Persian names
- **City Selection** - All major cities for each province
- **ISP Detection** - Support for TCI, Irancell, RighTel, Shatel, Pars Online, and other Iranian ISPs
- **IP Range Management** - CIDR-based IP range generation and validation

### Data Management
- **SQLite Database** - Local storage of scan history and results
- **Scan History** - View and reload previous scans
- **Results Export** - PDF, Excel, CSV, and HTML formats
- **Geolocation** - IP-based location detection with caching

### Visualization
- **Interactive Maps** - WebView2 + Leaflet.js for geographic visualization
- **Heatmaps** - Density-based visualization of miner distribution
- **Data Grid** - Sortable, filterable results with color-coded rows

### User Interface
- **Right-to-Left Layout** - Full Persian/RTL support
- **Tabbed Interface** - Organized views for Scan, Results, Map, Logs, and History
- **Professional Design** - Clean, modern Windows Forms interface
- **Status Notifications** - Real-time logging with color-coded messages

## Requirements

### Development
- **.NET 8 SDK** - [Download from Microsoft](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Windows 10/11** (for building and running)
- **Visual Studio 2022** or **Visual Studio Code** (optional)

### Runtime (End Users)
- **Windows 10 or 11** - Any edition
- **.NET 8 Runtime** - Not required for self-contained builds
- **WebView2 Runtime** - Included with Windows 10/11 or available from Microsoft

## Building the Application

### Prerequisites
Install .NET 8 SDK:
```powershell
winget install Microsoft.DotNet.SDK.8
```

### Restore Dependencies
```powershell
cd IranianMinerDetector.WinForms
dotnet restore
```

### Build for Development
```powershell
dotnet build --configuration Release
```

### Publish as Self-Contained Executable

#### Windows (PowerShell/Command Prompt)
```cmd
publish.bat
```

#### Linux/Mac (Cross-compile for Windows)
```bash
chmod +x publish.sh
./publish.sh
```

### Manual Publish Command
```powershell
dotnet publish IranianMinerDetector.WinForms.csproj `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:PublishReadyToRun=true `
    --output publish
```

### Output
After publishing, you'll find:
- `publish/IranianMinerDetector.WinForms.exe` - Single self-contained executable (~50-100MB)
- No additional files required for distribution

## Using the Application

### First Run
1. Double-click `IranianMinerDetector.WinForms.exe`
2. The application will create a local database in:
   - `%LocalAppData%\IranianMinerDetector\iranian_miner_detector.db`
3. WebView2 runtime will initialize automatically

### Performing a Scan

1. **Select Target Area**
   - Choose a Province (استان) from the dropdown
   - Select a City (شهر) if desired
   - Choose an ISP for targeted scanning

2. **Configure IP Range**
   - Enter a custom IP range (CIDR format: `192.168.1.0/24`)
   - Or leave empty to generate from province/city selection

3. **Set Scan Parameters**
   - Ports: Comma-separated list of ports to scan
   - Timeout: Connection timeout in milliseconds (default: 3000ms)
   - Concurrency: Number of simultaneous connections (default: 100)

4. **Start Scanning**
   - Click "شروع اسکن (Start Scan)"
   - Monitor progress in the progress bar and status label
   - View live results in the Results tab

5. **Review Results**
   - **Results Tab**: Detailed table of scanned hosts
   - **Map Tab**: Interactive geographic visualization
   - **Logs Tab**: Color-coded scan log
   - **History Tab**: Previous scans (double-click to reload)

6. **Export Reports**
   - **PDF**: Professional formatted report
   - **Excel**: Data spreadsheet with all details
   - **CSV**: Simple data export
   - **HTML**: Web-ready report with embedded map

### Settings

Access Settings from Tools → Settings (تنظیمات):

- **API Configuration**: Geolocation API provider and key
- **Default Parameters**: Timeout, concurrency, ports
- **Preferences**: Banner grab, geolocation options
- **Cache Management**: Clear geolocation cache

## Distribution

### As Single Executable
The self-contained build produces a single `.exe` file that includes:
- .NET runtime
- All dependencies
- Native libraries

Simply distribute `IranianMinerDetector.WinForms.exe` to users.

### System Requirements for End Users
- Windows 10 (version 1809 or later) or Windows 11
- 2GB RAM minimum (4GB recommended)
- 200MB free disk space

### Optional: Create Installer
Use [Inno Setup](https://jrsoftware.org/isinfo.php) to create a professional installer:

```iss
[Setup]
AppName=Iranian Miner Detector
AppVersion=1.0.0
DefaultDirName={pf}\IranianMinerDetector
DefaultGroupName=Iranian Miner Detector
OutputBaseFilename=IranianMinerDetectorSetup

[Files]
Source: "publish\IranianMinerDetector.WinForms.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Iranian Miner Detector"; Filename: "{app}\IranianMinerDetector.WinForms.exe"
Name: "{commondesktop}\Iranian Miner Detector"; Filename: "{app}\IranianMinerDetector.WinForms.exe"
```

## Project Structure

```
IranianMinerDetector.WinForms/
├── Models/                 # Data models
│   ├── ScanModels.cs       # ScanRecord, HostRecord, ScanConfiguration
│   ├── Province.cs         # Province and City data
│   └── ISPInfo.cs          # ISP information and IP ranges
├── Data/                   # Data access layer
│   ├── DatabaseManager.cs  # SQLite database operations
│   ├── IranianGeography.cs # 31 Iranian provinces data
│   └── IranianISPs.cs      # Iranian ISP IP ranges
├── Services/               # Business logic
│   ├── NetworkScanner.cs   # Network scanning and miner detection
│   ├── GeolocationService.cs # IP geolocation
│   ├── MapService.cs       # Map generation (Leaflet.js)
│   └── ReportService.cs    # PDF/Excel/CSV/HTML report generation
├── Forms/                  # UI forms
│   ├── MainForm.cs         # Main application window
│   ├── SettingsForm.cs     # Settings dialog
│   ├── AboutForm.cs        # About dialog
│   └── ReportViewerForm.cs # Report viewer
├── Resources/              # Application resources
│   └── app.ico            # Application icon
├── Program.cs              # Entry point
├── appsettings.json       # Configuration
├── app.ico                # Application icon
├── publish.bat            # Windows build script
├── publish.sh             # Linux/Mac build script
└── README.md              # This file
```

## Configuration

### Database Location
```
%LocalAppData%\IranianMinerDetector\iranian_miner_detector.db
```

### Settings File
```
%LocalAppData%\IranianMinerDetector\settings.json
```

### Reports Location
```
%Desktop%\IranianMinerDetector\Reports\
```

### Map Files
```
%LocalAppData%\IranianMinerDetector\Maps\
```

## Known Mining Ports

| Port  | Protocol/Service        | Coin Type |
|-------|------------------------|-----------|
| 8332  | Bitcoin RPC            | Bitcoin   |
| 8333  | Bitcoin P2P            | Bitcoin   |
| 3333  | Stratum Protocol       | Bitcoin   |
| 4028  | Stratum Protocol       | Multiple  |
| 4444  | Ethereum/Generic Miner | Ethereum  |
| 30303 | Ethereum P2P           | Ethereum  |
| 8545  | Ethereum RPC           | Ethereum  |
| 18081 | Monero P2P             | Monero    |
| 9332  | Litecoin RPC           | Litecoin  |
| 9333  | Litecoin P2P           | Litecoin  |
| 5050  | Alternative Stratum    | Multiple  |
| 8888  | Generic Mining Pool    | Multiple  |

## Troubleshooting

### WebView2 Issues
If the map doesn't display:
1. Ensure WebView2 runtime is installed: `https://go.microsoft.com/fwlink/p/?LinkId=2124703`
2. Check Windows Update for the latest version

### Database Errors
1. Ensure write permissions to `%LocalAppData%\IranianMinerDetector\`
2. Delete the database file to recreate it

### Network Scanning Issues
1. Run as Administrator for better network access
2. Reduce concurrency if experiencing timeouts
3. Check Windows Firewall settings

## Legal and Ethical Use

This tool is designed for authorized network security auditing and should only be used on networks you own or have explicit permission to scan.

**Important:**
- Always obtain proper authorization before scanning
- Respect privacy and data protection laws
- Use results only for legitimate security purposes
- Do not use for unauthorized access or surveillance

## License

Copyright © 2024 Iranian Network Security

## Support

For issues, questions, or feature requests, please contact the development team.

## Version History

### Version 1.0.0 (2024)
- Initial release
- Full Iranian province and city support
- Multi-protocol miner detection
- Interactive maps with Leaflet.js
- Multiple export formats (PDF, Excel, CSV, HTML)
- SQLite database storage
- Self-contained executable for easy distribution
