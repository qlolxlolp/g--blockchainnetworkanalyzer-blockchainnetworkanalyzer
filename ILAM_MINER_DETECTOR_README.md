# Ilam Miner Detector

A fully functional network security tool for detecting cryptocurrency miners in Ilam province, Iran. Built with Python and PyQt5, featuring real network scanning, geolocation, and comprehensive reporting.

## ⚠️ Legal and Ethical Notice

**WARNING**: This tool is designed for **authorized security auditing only**. 

- Only scan networks you own or have explicit written permission to scan
- Unauthorized network scanning may be **illegal** in your jurisdiction
- Users are solely responsible for compliance with all applicable laws
- This tool is for **educational and legitimate security research purposes**

## 🎯 Features

### Real Network Scanning
- **TCP Port Scanning**: Async port scanning with configurable timeouts
- **ICMP Ping**: Host reachability detection
- **Service Detection**: Identify services by port and banner analysis
- **Banner Grabbing**: Extract service information from open ports
- **Miner Detection**: Heuristic and signature-based cryptocurrency miner identification

### Geolocation
- **IP Geolocation**: Lookup IP locations using ip-api.com and ipinfo.io
- **Regional Filtering**: Filter results for Ilam province (configurable coordinates)
- **Rate Limiting**: Built-in token bucket rate limiter (45 req/min)
- **Caching**: SQLite-based geolocation cache to minimize API calls

### Visualization
- **Interactive Maps**: Folium-based HTML maps with marker clustering
- **Heatmaps**: Density visualization of detected miners
- **Color Coding**: Miners categorized by type (Stratum, Bitcoin, Ethereum, etc.)

### Reporting
- **JSON Export**: Structured data with full scan metadata
- **CSV Export**: Spreadsheet-compatible format
- **HTML Reports**: Professional reports with embedded maps

### Modern GUI
- **PyQt5 Interface**: Native desktop application with responsive design
- **Real-time Updates**: Live progress tracking and result streaming
- **Tabbed Interface**: Organized view for results, maps, and logs
- **Customizable Scans**: Flexible configuration for target ranges and ports

## 🚀 Quick Start

### Installation

1. **Clone or extract the repository**

2. **Install Python 3.8+** (if not already installed)
   ```bash
   # Check Python version
   python3 --version
   ```

3. **Install dependencies**
   ```bash
   pip install -r requirements.txt
   ```

   **Note**: On some systems you may need to install PyQt5 system packages:
   ```bash
   # Ubuntu/Debian
   sudo apt-get install python3-pyqt5 python3-pyqt5.qtwebengine
   
   # Fedora
   sudo dnf install python3-qt5 python3-qt5-webengine
   
   # macOS (with Homebrew)
   brew install pyqt5
   ```

4. **Create default configuration**
   ```bash
   python main.py --create-config
   ```

### Running the Application

```bash
# Launch GUI
python main.py

# With verbose logging
python main.py --verbose

# With custom config
python main.py --config /path/to/config.json
```

### Basic Usage

1. **Enter Target Range**
   - Single IP: `192.168.1.100`
   - CIDR notation: `192.168.1.0/24`
   - IP range: `192.168.1.1-192.168.1.254`
   - Comma-separated: `192.168.1.1, 192.168.1.5, 192.168.1.10`

2. **Configure Scan Options**
   - Ports: Comma-separated list (default includes common miner ports)
   - Timeout: Connection timeout in milliseconds
   - Max Concurrent: Number of parallel scan operations
   - Enable/disable: Ping, banner grabbing, geolocation, Ilam filtering

3. **Start Scan**
   - Click "Start Scan"
   - Monitor progress in real-time
   - View discovered hosts in the Results tab
   - Explore geographic distribution in the Map tab

4. **Export Results**
   - JSON: Full structured data
   - CSV: Spreadsheet format
   - HTML: Professional report with embedded map

## 📋 Configuration

Edit `config/config.json` to customize settings:

### Scan Configuration
```json
{
  "scan": {
    "timeout_ms": 3000,          // Connection timeout
    "max_concurrent": 50,         // Parallel operations
    "retry_count": 2,             // Connection retries
    "ping_enabled": true,         // ICMP ping before port scan
    "banner_grab_enabled": true,  // Extract service banners
    "banner_timeout_ms": 2000     // Banner read timeout
  }
}
```

### Geolocation Configuration
```json
{
  "geolocation": {
    "primary_provider": "ip-api",    // Free provider
    "fallback_provider": "ipinfo",   // Requires API key
    "api_key": null,                 // Optional ipinfo.io key
    "rate_limit_per_minute": 45,     // API rate limit
    "cache_enabled": true,           // Cache lookups
    "ilam_lat_min": 32.5,           // Ilam province bounds
    "ilam_lat_max": 33.5,
    "ilam_lon_min": 46.0,
    "ilam_lon_max": 47.5
  }
}
```

### Miner Port Configuration
```json
{
  "miner_ports": {
    "stratum": [3333, 4444, 4028, 7777, 14433, 14444, 5555, 8888, 9999],
    "bitcoin": [8332, 8333, 18332, 18333],
    "ethereum": [8545, 8546, 30303, 30304],
    "generic": [8080, 8081, 3000, 9090]
  }
}
```

## 🏗️ Architecture

### Project Structure
```
ilam_miner_detector/
├── __init__.py               # Package initialization
├── config_manager.py         # Configuration handling
├── database.py               # SQLite operations
├── ip_manager.py             # IP parsing and validation
├── network_scanner.py        # TCP/ICMP scanning engine
├── geolocation.py            # IP geolocation service
├── map_generator.py          # Folium map generation
├── reporter.py               # Report export (JSON/CSV/HTML)
├── worker.py                 # QThread worker for async scanning
└── gui/
    ├── __init__.py
    ├── main_window.py        # Main application window
    └── widgets.py            # Custom PyQt5 widgets

config/
└── config.json               # Configuration file

data/
├── ilam_miner.db            # SQLite database
└── logs/                     # Application logs

reports/                      # Exported reports

main.py                       # Application entry point
requirements.txt              # Python dependencies
```

### Database Schema

**scans** table:
- Scan metadata: name, target range, timestamps, status
- Configuration snapshot
- Statistics: total IPs, scanned IPs, detected miners

**hosts** table:
- Discovered host information
- IP address, hostname, reachability
- Open ports, detected services
- Miner classification and type
- Banner data

**geolocation_cache** table:
- Cached IP geolocation data
- Country, region, city, coordinates
- ISP and organization info
- Cache timestamp and source

### Key Components

#### NetworkScanner
- Async TCP port scanning using `asyncio`
- ICMP ping via subprocess
- Service detection by port and banner signatures
- Miner heuristics based on port combinations

#### GeolocationService
- Rate-limited API calls (token bucket algorithm)
- Multi-provider support (ip-api.com, ipinfo.io)
- SQLite caching layer
- Regional boundary filtering

#### IPManager
- Smart parsing of CIDR, ranges, lists
- IP validation and private range detection
- Memory-efficient generation using iterators

#### ScanWorker
- QThread-based async scanning
- Signal-based GUI communication
- Graceful cancellation support
- Progress tracking and error handling

## 🔍 Miner Detection Methods

### Port-Based Detection
Common cryptocurrency miner ports:
- **Stratum**: 3333, 4444, 4028, 5555, 7777, 8888, 9999, 14433, 14444
- **Bitcoin**: 8332 (RPC), 8333 (P2P), 18332-18333 (Testnet)
- **Ethereum**: 8545-8546 (RPC), 30303-30304 (P2P)

### Banner Signature Detection
Keyword matching in service banners:
- Stratum: `stratum`, `mining.subscribe`, `mining.authorize`
- Bitcoin: `Bitcoin`, `Satoshi`, `getwork`
- Ethereum: `eth_`, `geth`, `parity`
- Monero: `monero`, `cryptonight`, `xmr-`

### Heuristic Detection
- Port combination analysis
- Service behavior patterns
- Multiple open mining ports

## 📊 Reports

### JSON Report
```json
{
  "scan_metadata": {
    "scan_name": "Scan_20240216_143022",
    "target_range": "192.168.1.0/24",
    "started_at": "2024-02-16T14:30:22",
    "completed_at": "2024-02-16T14:35:45",
    "status": "completed"
  },
  "hosts": [
    {
      "ip_address": "192.168.1.100",
      "hostname": "miner-001.local",
      "open_ports": [3333, 4444],
      "is_miner": true,
      "miner_type": "stratum",
      "city": "Ilam",
      "latitude": 33.6374,
      "longitude": 46.4227
    }
  ],
  "summary": {
    "total_hosts_scanned": 254,
    "miners_detected": 5,
    "scan_duration": "5m 23s"
  }
}
```

### CSV Report
Columns: IP Address, Hostname, Open Ports, Miner Type, Location, ISP, Coordinates

### HTML Report
Professional report with:
- Scan statistics dashboard
- Embedded interactive map
- Detailed host table
- Timestamp and metadata

## 🛠️ Advanced Usage

### Custom Port Scanning
```python
# Edit config/config.json or use GUI
{
  "miner_ports": {
    "custom": [1234, 5678, 9999]
  }
}
```

### Scanning Large Networks
For /16 or larger networks:
1. Increase timeout and max_concurrent
2. Disable geolocation to speed up scanning
3. Use filters to reduce result set
4. Consider scanning in batches

### API Key Configuration
For high-volume scans, get an API key from ipinfo.io:
```json
{
  "geolocation": {
    "api_key": "your_ipinfo_api_key_here"
  }
}
```

### Custom Ilam Region Bounds
Adjust coordinates for different regions:
```json
{
  "geolocation": {
    "ilam_lat_min": 32.0,
    "ilam_lat_max": 34.0,
    "ilam_lon_min": 45.5,
    "ilam_lon_max": 48.0
  }
}
```

## 🐛 Troubleshooting

### Import Errors
```bash
# Install all dependencies
pip install -r requirements.txt

# If PyQt5 fails, try system packages
sudo apt-get install python3-pyqt5
```

### Permission Errors
Some systems require elevated privileges for ICMP ping:
```bash
# Linux/macOS
sudo python main.py

# Or disable ping in config
{
  "scan": {
    "ping_enabled": false
  }
}
```

### Rate Limiting
If you hit ip-api.com rate limits (45/min):
1. Enable caching (default)
2. Reduce scan speed
3. Get ipinfo.io API key for fallback

### Database Locked
If database is locked, ensure only one instance is running:
```bash
# Kill existing processes
pkill -f "python main.py"

# Remove lock files
rm data/ilam_miner.db-wal data/ilam_miner.db-shm
```

## 📝 Logging

Logs are saved to `data/logs/ilam_miner_detector.log`

Enable verbose logging:
```bash
python main.py --verbose
```

Log levels:
- INFO: General operations
- WARNING: Non-critical issues
- ERROR: Failures and exceptions
- DEBUG: Detailed diagnostic info (verbose mode)

## 🔒 Security Considerations

1. **Authorization**: Always obtain written permission before scanning
2. **Rate Limiting**: Respect API rate limits to avoid service disruption
3. **Data Privacy**: Scan results may contain sensitive network information
4. **Network Impact**: Aggressive scanning may trigger IDS/IPS systems
5. **Legal Compliance**: Consult local laws regarding network scanning

## 🤝 Contributing

This is a security research tool. Contributions should focus on:
- Improving detection accuracy
- Adding new miner signatures
- Performance optimizations
- Bug fixes and stability

## 📄 License

This tool is provided for educational and authorized security research purposes only.
Users are responsible for compliance with all applicable laws and regulations.

## 🙏 Acknowledgments

- **ip-api.com**: Free IP geolocation service
- **ipinfo.io**: Alternative geolocation provider
- **Folium**: Python mapping library
- **PyQt5**: Cross-platform GUI framework

## 📧 Support

For issues, questions, or contributions:
1. Check existing documentation
2. Review troubleshooting section
3. Examine log files for errors
4. Ensure you have proper authorization for scanning

---

**Remember**: With great power comes great responsibility. Use this tool ethically and legally.
