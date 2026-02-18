# Iranian Network Miner Detection System v2.0

**Comprehensive, Feature-Rich Network Security Auditing Tool for Iran**

---

## 🚀 Overview

The Iranian Network Miner Detection System v2.0 is a major expansion of the original Ilam Miner Detector, now providing:

- **Full Iranian Geographic Coverage**: All 31 provinces with complete city listings
- **ISP IP Range Management**: Major Iranian ISPs with risk scoring
- **Advanced Network Analysis**: Multi-layered detection with confidence scoring
- **VPN/Proxy Detection**: Identify anonymization services
- **YAML-Based Detection Rules**: Flexible, extensible rule system
- **Scheduled Scanning**: Automated scans with APScheduler
- **Analytics Dashboard**: Charts, trends, and statistical insights
- **Professional Reporting**: PDF, Excel, HTML, JSON, CSV exports
- **Interactive Maps**: Folium-based geographic visualization with heatmaps

---

## ✨ Key Features

### 1. Geographic Coverage

**All 31 Iranian Provinces:**
- Tehran, Alborz, Isfahan, Fars, Khorasan Razavi
- East/West Azerbaijan, Kermanshah, Khuzestan, Kerman
- Yazd, Hormozgan, Hamadan, Gilan, Mazandaran
- Golestan, Kohgiluyeh and Boyer-Ahmad, Bushehr
- Zanjan, Semnan, Sistan and Baluchestan, Kurdistan
- Markazi, Chaharmahal and Bakhtiari, Qazvin, Ilam
- Lorestan, South/North Khorasan, Ardabil, Qom

**Each Province Includes:**
- Persian and English names
- Province code
- Central coordinates
- Complete list of major cities with coordinates

### 2. ISP Management

**Major Iranian ISPs:**
- Iran Telecommunication Company (TCI)
- Irancell (MTN Iran)
- RighTel
- Shatel
- Pars Online
- HiWeb
- AsiaTech
- Fanava
- Sefroyek Parvaz

**ISP Features:**
- IP range database with CIDR notation
- Automatic IP-to-ISP identification
- Risk scoring based on detection statistics
- Dynamic risk score updates
- High-risk ISP identification

### 3. Detection Engine

**Configurable Detection Rules (YAML):**
```yaml
rules:
  - name: "stratum_pool_detection"
    description: "Detect Stratum mining pool connections"
    enabled: true
    priority: 90
    confidence_score: 0.85
    tags: ["stratum", "mining_pool", "high_confidence"]
    conditions:
      ports: [3333, 4444, 4028, 7777, 14433, 14444]
      banner_patterns: ["mining.subscribe", "stratum"]
    actions: ["log", "alert", "database_record"]
```

**Pre-Configured Signatures:**
- CGMiner
- BFGMiner
- Ethminer
- XMRig
- Claymore
- PhoenixMiner

**Confidence Scoring:**
- High (80-100%): Multiple mining ports + banner match
- Medium (50-79%): Mining ports + partial evidence
- Low (20-49%): Single mining port only

### 4. Network Scanning

**Scan Modes:**
- Single IP
- IP Range (CIDR)
- Random IP generation
- Serial IP generation
- Custom IP lists
- Province-based scanning

**Features:**
- TCP port scanning with configurable timeouts
- ICMP ping checks
- Service fingerprinting
- Banner grabbing
- Multi-threading with asyncio
- Rate limiting and polite scanning
- Configurable concurrency

### 5. VPN/Proxy Detection

**Detection Methods:**
- Known VPN/proxy provider database
- ASN-based detection
- Hosting provider identification
- Residential proxy patterns
- External service verification (ip-api.com, ipinfo.io)

**Detection Types:**
- VPN detection
- Proxy detection
- Tor exit nodes
- Hosting/Cloud services
- Mobile networks

### 6. Geolocation

**Features:**
- Multi-provider support (ip-api.com, ipinfo.io)
- Rate limiting and caching
- Iranian province identification
- City-level accuracy
- ISP information
- TTL-based cache expiration

**Iranian Province Matching:**
- Coordinate-based province identification
- Configurable tolerance for border regions
- Province code mapping

### 7. Scan Scheduling

**Scheduler Features:**
- APScheduler integration
- Cron expressions support
- Interval-based scheduling
- One-time schedules
- Job persistence with SQLAlchemy
- Multiple job executors
- Event-based logging

**Schedule Frequencies:**
- Once
- Hourly
- Daily
- Weekly
- Monthly
- Custom (cron expression)

### 8. Analytics & Reporting

**Analytics Features:**
- Scan trends over time
- Detection rate analysis
- Geographic distribution
- ISP statistics
- Miner type distribution
- Confidence score analysis
- Top vulnerable regions
- High-risk ISPs

**Charts & Visualizations:**
- **Matplotlib Charts**: Static PNG exports
  - Miner distribution by province (bar)
  - Scan trends over time (line)
  - Detection rate trends (line)
  - Miner types distribution (pie)
  - Confidence distribution (bar)

- **Plotly Charts**: Interactive HTML exports
  - All Matplotlib charts in interactive format
  - Hover information
  - Zoom and pan capabilities
  - Export options

**Export Formats:**
- **JSON**: Structured data for automation
- **CSV**: Spreadsheet-compatible
- **HTML**: Styled web reports
- **PDF**: Professional printable reports (ReportLab)
- **Excel**: Multi-sheet workbooks (openpyxl)
  - Summary sheet
  - Detected Miners sheet
  - All Hosts sheet
  - Analytics sheet

### 9. Database

**SQLite Database with Tables:**
- `ScanResults`: Scan metadata and results
- `IPResults`: Individual IP scan results
- `GeolocationCache`: Cached geolocation data
- `ScheduledScans`: Scan schedules
- `ScheduleExecutions`: Execution history
- `AuditLog`: Audit trail for all operations

**Features:**
- Connection pooling
- Thread-safe operations
- Automatic backup
- Data retention policies
- Transaction support

### 10. User Interface

**PyQt5 GUI Features:**
- Modern Material Design styling
- Province/city selector
- Real-time scan progress
- Interactive map display
- Results table with filtering
- Analytics dashboard
- Report generation buttons
- Schedule management

**CLI Features:**
- Comprehensive command-line interface
- Batch processing support
- Scriptable automation
- Multiple output formats

---

## 📦 Installation

### Prerequisites

- Python 3.8 or higher
- pip (Python package manager)

### Quick Install

```bash
# Navigate to ilam_miner_detector directory
cd ilam_miner_detector

# Install all dependencies
pip install -r requirements.txt

# Run the application
python main_extended.py
```

### Manual Installation

```bash
# Install required packages
pip install PyQt5 PyQtWebEngine aiohttp requests folium
pip install APScheduler reportlab openpyxl Pillow
pip install matplotlib plotly pandas PyYAML python-dateutil

# Optional: For advanced scanning
pip install scapy  # Requires root privileges
```

### Verify Installation

```bash
# Check version
python main_extended.py --version

# List provinces
python main_extended.py provinces

# List ISPs
python main_extended.py isps

# Show help
python main_extended.py --help
```

---

## 🎯 Usage

### GUI Mode

```bash
python main_extended.py
```

**GUI Workflow:**
1. Select IP input mode (Single, Range, Province-based)
2. Configure scan options (timeout, concurrency, ports)
3. Select province/city if needed
4. Click "Start Network Scan"
5. View real-time progress in Results tab
6. Check Analytics dashboard for insights
7. Generate reports in desired format
8. View interactive map

### CLI Mode - Basic Scan

```bash
# Scan a local network
python main_extended.py scan --cidr 192.168.1.0/24

# Scan with all reports
python main_extended.py scan --cidr 10.0.0.0/24 --export --map --analytics
```

### CLI Mode - Province-Based Scan

```bash
# List available provinces
python main_extended.py provinces

# List cities in a province
python main_extended.py provinces --detailed --show-cities

# Scan with province filter (implemented in GUI, requires custom script for CLI)
```

### CLI Mode - ISP Information

```bash
# List all ISPs
python main_extended.py isps

# Show detailed ISP information
python main_extended.py isps --detailed --show-ranges
```

### CLI Mode - Analytics

```bash
# View database statistics
python main_extended.py stats
```

---

## 🔧 Configuration

### Configuration File

Edit `config/config_extended.json`:

```json
{
  "scan": {
    "timeout": 3,
    "concurrency": 50,
    "enable_banner_grab": true,
    "rate_limit_enabled": true,
    "polite_scanning": true
  },
  
  "detection_rules": {
    "enabled": true,
    "config_file": "config/detection_rules.yaml",
    "confidence_threshold": 0.5
  },
  
  "reporting": {
    "export_formats": ["json", "csv", "html", "pdf", "excel"],
    "include_charts": true,
    "include_map": true
  },
  
  "analytics": {
    "enabled": true,
    "trend_analysis_days": 30,
    "generate_charts": true
  },
  
  "scheduler": {
    "enabled": true,
    "database_path": "data/scheduler.db"
  }
}
```

### Detection Rules

Edit `config/detection_rules.yaml` to customize detection rules:

```yaml
version: "1.0"
rules:
  - name: "custom_miner_detection"
    description: "Custom rule for specific miner"
    enabled: true
    priority: 95
    confidence_score: 0.90
    conditions:
      ports: [8080, 8081]
      banner_patterns: ["custom_signature"]
    actions: ["log", "alert", "database_record"]
```

---

## 📊 Reports

### PDF Report

Professional, printable reports with:
- Scan metadata
- Summary statistics
- Detected miners table
- All hosts table
- Color-coded confidence levels

### Excel Workbook

Multi-sheet Excel file with:
- Summary sheet: Overview and key metrics
- Detected Miners sheet: Detailed miner information
- All Hosts sheet: Complete scan results
- Analytics sheet: Statistics and distributions

### HTML Report

Web-based reports with:
- Interactive styling
- Color-coded tables
- Summary cards
- Embedded map support

### Charts

**Matplotlib (PNG):**
- High-resolution static charts
- Publication-ready quality
- DPI configurable

**Plotly (HTML):**
- Interactive visualizations
- Hover information
- Zoom and pan
- Export options

---

## 🗺️ Geographic Features

### Province Selection

```python
from ilam_geography import get_all_province_names, get_cities_in_province

# List all provinces
provinces = get_all_province_names()

# Get cities in a province
cities = get_cities_in_province("Tehran")
```

### ISP Identification

```python
from iran_isps import identify_isp, get_isp_ranges

# Identify ISP for an IP
isp = identify_isp("91.98.0.1")

# Get IP ranges for an ISP
ranges = get_isp_ranges("Iran Telecommunication Company (TCI)")
```

### Province Matching

```python
from iran_geography import get_province_by_coordinates

# Find province from coordinates
province = get_province_by_coordinates(35.6892, 51.3890)  # Tehran
```

---

## 📅 Scheduling

### Create a Schedule

```python
from scheduler import ScheduledScan, ScheduleFrequency
from scheduler import get_scheduler

scheduler = get_scheduler()
scheduler.start()

# Define schedule
scan_schedule = ScheduledScan(
    name="Daily Network Scan",
    cidr_range="192.168.1.0/24",
    frequency=ScheduleFrequency.DAILY,
    ports=[3333, 4444, 8332, 8333],
    export_reports=True
)

# Define callback
def run_scheduled_scan(schedule):
    # Your scan logic here
    pass

# Add schedule
scheduler.add_schedule(scan_schedule, run_scheduled_scan)
```

---

## 🔒 Legal & Ethical Use

**IMPORTANT:** This tool is designed exclusively for:

- ✅ Authorized security auditing
- ✅ Network administration
- ✅ Legitimate cybersecurity research
- ✅ Compliance monitoring

**Requirements:**
- Written authorization from network owners
- Compliance with Iranian cyberlaw
- Proper documentation of all scan activities
- Audit logging of all operations

**Prohibited:**
- ❌ Unauthorized network scanning
- ❌ Scanning networks you don't own
- ❌ Using without proper authorization
- ❌ Violating local laws and regulations

---

## 🛠️ Troubleshooting

### Permission Denied

TCP scanning may require elevated privileges:
```bash
sudo python main_extended.py
```

### Missing Dependencies

Install missing packages:
```bash
pip install -r requirements.txt
```

### Database Locked

Ensure only one instance is running:
```bash
ps aux | grep python
# Kill other instances
```

### Map Generation Issues

Check internet connectivity for geolocation services.

---

## 📈 Performance

### Expected Performance

- **Small network (/28)**: 10-30 seconds
- **Medium network (/24)**: 2-5 minutes
- **Large network (/16)**: Hours (use batching)

### Optimization Tips

- Reduce concurrency for stability
- Increase timeout for slow networks
- Use province-based filtering
- Enable rate limiting for politeness
- Use geolocation caching

---

## 🤝 Contributing

Contributions are welcome! Please ensure:

1. Code follows existing style patterns
2. All features are legal and ethical
3. Documentation is updated
4. No malicious functionality is added

---

## 📝 License

This project is for educational and authorized security auditing purposes only.

---

## 📧 Support

For issues or questions:
1. Read the documentation
2. Check examples
3. Review logs: `data/iranian_miner_detector.log`
4. Create an issue on the project repository

---

## 🎉 Acknowledgments

- Iranian provinces data: Official government sources
- ISP data: RIPE NCC database
- Geolocation: ip-api.com, ipinfo.io
- UI Framework: PyQt5, Material Design
- Charts: Matplotlib, Plotly
- Reporting: ReportLab, openpyxl
- Scheduling: APScheduler

---

**Version**: 2.0.0  
**Last Updated**: 2024  
**Status**: Production Ready 🚀

---

**⚠️ DISCLAIMER**: This tool is intended for legitimate security research and authorized auditing only. Misuse of this tool is prohibited. Users are solely responsible for ensuring compliance with all applicable laws and regulations.
