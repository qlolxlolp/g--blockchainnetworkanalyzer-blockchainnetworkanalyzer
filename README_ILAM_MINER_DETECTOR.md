# Project Overview

This repository contains two distinct applications:

## 1. BlockchainNetworkAnalyzer (C#/.NET/WPF)
A Windows desktop application for blockchain network analysis. See `README.md` and `README_COMMERCIAL.md` for details.

## 2. Ilam Miner Detector (Python/PyQt5) ⭐ NEW

A fully functional cryptocurrency miner detection tool built in Python with a modern PyQt5 GUI.

### 📁 Location
All Ilam Miner Detector files are located in the project root:

```
/
├── ilam_miner_detector/          # Main Python package
│   ├── __init__.py
│   ├── config_manager.py
│   ├── database.py
│   ├── ip_manager.py
│   ├── network_scanner.py
│   ├── geolocation.py
│   ├── map_generator.py
│   ├── reporter.py
│   ├── worker.py
│   └── gui/
│       ├── __init__.py
│       ├── main_window.py
│       └── widgets.py
│
├── config/                       # Configuration files
│   └── config.json
│
├── data/                         # Application data (auto-created)
│   ├── ilam_miner.db            # SQLite database
│   └── logs/                     # Log files
│
├── reports/                      # Exported reports (auto-created)
│
├── main.py                       # Application entry point
├── requirements.txt              # Python dependencies
├── setup.sh                      # Linux/macOS setup script
├── setup.bat                     # Windows setup script
├── test_components.py            # Unit tests
│
├── ILAM_MINER_DETECTOR_README.md # Full documentation
├── QUICKSTART.md                 # 5-minute setup guide
├── IMPLEMENTATION_SUMMARY.md     # Technical details
├── EXAMPLES.md                   # Usage examples
└── README_ILAM_MINER_DETECTOR.md # This file
```

### 🚀 Quick Start

#### Prerequisites
- Python 3.8 or higher
- pip (Python package manager)

#### Installation (5 minutes)

**Option 1: Automated Setup**

Linux/macOS:
```bash
./setup.sh
```

Windows:
```bash
setup.bat
```

**Option 2: Manual Setup**

1. Install dependencies:
```bash
pip install -r requirements.txt
```

2. Create configuration:
```bash
python main.py --create-config
```

3. Launch application:
```bash
python main.py
```

### 📚 Documentation

| Document | Description |
|----------|-------------|
| [ILAM_MINER_DETECTOR_README.md](ILAM_MINER_DETECTOR_README.md) | Complete documentation (11,000+ words) |
| [QUICKSTART.md](QUICKSTART.md) | 5-minute setup and first scan |
| [IMPLEMENTATION_SUMMARY.md](IMPLEMENTATION_SUMMARY.md) | Technical architecture and design |
| [EXAMPLES.md](EXAMPLES.md) | Real-world usage examples |

### ✨ Features

- ✅ **Real Network Scanning**: Actual TCP port scanning and ICMP ping
- ✅ **Geolocation**: Live IP geolocation via ip-api.com and ipinfo.io
- ✅ **CIDR Support**: Full CIDR notation parsing (e.g., 192.168.1.0/24)
- ✅ **Interactive Maps**: Folium-based geographic visualization
- ✅ **Multiple Export Formats**: JSON, CSV, HTML reports
- ✅ **Modern GUI**: PyQt5 desktop application with real-time updates
- ✅ **Database**: SQLite for persistent storage and caching

### 🎯 Use Cases

1. **Network Administrators**: Audit your network for unauthorized miners
2. **Security Researchers**: Detect mining activity in specific regions
3. **IT Teams**: Enforce cryptocurrency mining policies
4. **Penetration Testers**: Assess mining-related security risks

### ⚠️ Legal Notice

**WARNING**: This tool is for authorized security auditing only.

- ✅ Only scan networks you own or have written permission to scan
- ⚠️ Unauthorized network scanning may be illegal in your jurisdiction
- 🔒 Users are solely responsible for compliance with all laws

### 🛠️ Technology Stack

- **Language**: Python 3.8+
- **GUI**: PyQt5 5.15.x
- **Networking**: asyncio, aiohttp, socket
- **Database**: SQLite3
- **Mapping**: Folium
- **Async**: asyncio with QThread workers

### 📊 Statistics

- **Lines of Code**: 3,500+
- **Modules**: 12 Python files
- **Documentation**: 11,000+ words
- **Test Coverage**: 240 lines of unit tests

### 🔍 What It Detects

**Cryptocurrency Miner Types:**
- Stratum mining pools (ports 3333, 4444, etc.)
- Bitcoin nodes (ports 8332, 8333)
- Ethereum nodes (ports 8545, 30303)
- Monero miners
- Custom mining software

**Detection Methods:**
1. Port scanning (common miner ports)
2. Banner analysis (protocol signatures)
3. Service fingerprinting
4. Heuristic detection (port combinations)

### 🗺️ Regional Focus: Ilam Province

This tool includes specific functionality for filtering results to Ilam province, Iran:

- **Coordinates**: 32.5-33.5°N, 46.0-47.5°E
- **Configurable bounds** in config.json
- **Automatic filtering** based on IP geolocation

### 📈 Performance

- **Small network (/28)**: 10-30 seconds
- **Medium network (/24)**: 2-5 minutes
- **Large network (/16)**: Hours (use batching)

**Configurable parameters:**
- Timeout (1000-5000ms)
- Concurrency (1-200 simultaneous connections)
- Retry attempts
- Rate limiting

### 🧪 Testing

Run unit tests:
```bash
python test_components.py
```

Tests cover:
- Configuration loading
- IP parsing and validation
- Database operations
- Scanner functionality
- Report generation
- Map creation

### 🤝 Support

1. **Read the documentation**: Start with `QUICKSTART.md`
2. **Check examples**: See `EXAMPLES.md` for real scenarios
3. **Review logs**: Check `data/logs/ilam_miner_detector.log`
4. **Run tests**: `python test_components.py`

### 📝 License

This tool is provided for educational and authorized security research purposes only.

### 🙏 Acknowledgments

- **ip-api.com**: Free IP geolocation service
- **ipinfo.io**: Premium geolocation provider
- **Folium**: Python mapping library
- **PyQt5**: Cross-platform GUI framework
- **Python community**: Amazing ecosystem

---

## Getting Started Now

**First time?** Start here:

1. Read: [QUICKSTART.md](QUICKSTART.md) (5 minutes)
2. Run: `./setup.sh` or `setup.bat`
3. Launch: `python main.py`
4. Scan: Try `192.168.1.0/24` (your local network)
5. Explore: View Results, Map, and Log tabs
6. Export: Generate reports in JSON/CSV/HTML

**Need help?** Check [ILAM_MINER_DETECTOR_README.md](ILAM_MINER_DETECTOR_README.md) for complete documentation.

---

## Repository Structure

```
BlockchainNetworkAnalyzer/           # Original C#/.NET application
├── Core/                            # C# business logic
├── Views/                           # WPF windows
├── Resources/                       # Assets
└── [... C# project files ...]

ilam_miner_detector/                 # NEW: Python application
├── config_manager.py                # Configuration handling
├── database.py                      # SQLite operations
├── network_scanner.py               # Port scanning engine
├── geolocation.py                   # IP geolocation
├── gui/                             # PyQt5 GUI
└── [... Python modules ...]

config/                              # Python app configuration
data/                                # Python app data
reports/                             # Python app reports

main.py                              # Python app entry point
requirements.txt                     # Python dependencies
setup.sh / setup.bat                 # Setup scripts

[Documentation files]                # README, guides, examples
```

---

**Ready to detect miners? Let's get started!** 🚀

For the C#/.NET application, see the original `README.md`.
For the Python application, continue reading the Ilam Miner Detector docs.
