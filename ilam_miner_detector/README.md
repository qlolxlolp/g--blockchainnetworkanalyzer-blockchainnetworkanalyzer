# Ilam Miner Detector

A fully functional, real network security tool for detecting cryptocurrency mining operations in Ilam province, Iran. Built with Python and PyQt5.

## Features

- **Real Network Scanning**: TCP port scanning with actual socket connections
- **Geolocation**: IP geolocation using ip-api.com with rate limiting and caching
- **Interactive Maps**: Folium-based map visualization with heatmaps and markers
- **Database Storage**: SQLite for scan results and geolocation cache
- **Report Generation**: Export to JSON, CSV, and HTML formats
- **PyQt5 GUI**: Professional graphical interface with real-time updates
- **CLI Mode**: Command-line interface for scripting and automation

## Installation

### Requirements

- Python 3.8 or higher
- PyQt5
- requests
- folium

### Setup

```bash
# Clone or navigate to the project
cd ilam_miner_detector

# Install dependencies
pip install -r requirements.txt

# Run the application
python main.py
```

## Usage

### GUI Mode

Launch the graphical interface:

```bash
python main.py
```

1. Enter a CIDR range (e.g., `192.168.1.0/24`)
2. Configure scan options (timeout, concurrency, ports)
3. Click "Start Scan"
4. View results in real-time
5. Export reports when complete

### CLI Mode

Scan a network range:

```bash
python main.py scan --cidr 192.168.1.0/24 --export --map
```

Geolocate IP addresses:

```bash
python main.py geolocate --ips 8.8.8.8,1.1.1.1
```

View database statistics:

```bash
python main.py stats
```

## Configuration

Edit `config/config.json` to customize:

- **Scan Settings**: Timeouts, concurrency, port lists
- **Geolocation**: API providers, rate limits, cache TTL
- **Ilam Region**: Geographic bounds for filtering
- **Database**: Storage path and connection settings
- **Reporting**: Export formats and directories

### Default Ports Scanned

- **Stratum Mining**: 3333, 4444, 4028, 7777, 14433, 14444
- **Bitcoin**: 8332 (RPC), 8333 (P2P)
- **Ethereum**: 8545 (RPC), 30303 (P2P)
- **Generic**: 8080, 8081

## Architecture

```
ilam_miner_detector/
├── __init__.py           # Package initialization
├── main.py               # Entry point
├── config_manager.py     # Configuration handling
├── database.py           # SQLite operations
├── ip_manager.py         # CIDR and IP utilities
├── network_scanner.py    # TCP/ICMP scanning
├── geolocation.py        # IP geolocation service
├── map_generator.py      # Folium map creation
├── reporter.py           # Report export
├── worker.py             # QThread workers
├── gui/
│   ├── __init__.py
│   ├── main_window.py    # Main GUI window
│   └── widgets.py        # Custom PyQt5 widgets
├── config/
│   └── config.json       # Default configuration
├── data/                 # SQLite database
└── reports/              # Generated reports
```

## Detection Methodology

The tool uses multiple indicators to detect potential mining operations:

1. **Port Analysis**: Checks for known mining service ports
2. **Banner Grabbing**: Analyzes service responses for mining signatures
3. **Geolocation**: Verifies if detected hosts are in Ilam province
4. **Confidence Scoring**: Weighted scoring based on multiple factors

### Confidence Levels

- **High (80-100%)**: Multiple mining ports open with matching banners
- **Medium (50-79%)**: Mining ports open with some banner evidence
- **Low (20-49%)**: Single mining port open without banner confirmation

## Legal Notice

**This tool is for authorized security auditing only.**

Users must have explicit permission to scan target networks. Unauthorized scanning may violate:

- Computer Fraud and Abuse Act (CFAA) - United States
- Computer Misuse Act - United Kingdom
- Similar legislation in other jurisdictions

## Rate Limiting

The geolocation service respects API limits:

- **ip-api.com**: 45 requests per minute (free tier)
- **ipinfo.io**: Requires API token for higher limits

Results are cached to minimize API calls.

## Troubleshooting

### Permission Denied

TCP scanning may require root/administrator privileges on some systems:

```bash
sudo python main.py
```

### No Results

- Verify the CIDR range is correct
- Check firewall settings
- Ensure target hosts are online

### Map Not Generated

- Requires geolocation API to return coordinates
- Some IPs may not have geolocation data
- Check internet connectivity

## Development

### Running Tests

```bash
python -m pytest tests/
```

### Building Executable

```bash
pyinstaller --onefile --windowed main.py
```

## License

This project is for educational and authorized security auditing purposes.

## Contributing

Contributions are welcome. Please ensure:

1. Code follows existing style patterns
2. All tests pass
3. Documentation is updated
4. No malicious functionality is added

## Contact

For issues or questions, please open an issue on the project repository.

---

**Disclaimer**: This tool is intended for legitimate security research and authorized auditing only. Misuse of this tool is prohibited.
