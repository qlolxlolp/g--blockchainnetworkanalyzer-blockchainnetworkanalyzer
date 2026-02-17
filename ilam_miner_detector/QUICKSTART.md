# Ilam Miner Detector - Quick Start Guide

## Installation

```bash
# Navigate to project directory
cd ilam_miner_detector

# Install dependencies
pip install -r requirements.txt
```

## Launch GUI

```bash
python main.py
```

## CLI Usage

### Scan a Network

```bash
# Basic scan
python main.py scan --cidr 192.168.1.0/24

# With export and map
python main.py scan --cidr 10.0.0.0/24 --export --map

# Custom ports
python main.py scan --cidr 192.168.1.0/24 --ports 3333,4444,8332
```

### Geolocate IPs

```bash
python main.py geolocate --ips 8.8.8.8,1.1.1.1
```

### View Stats

```bash
python main.py stats
```

## Configuration

Edit `config/config.json` to customize:

- Scan timeouts and concurrency
- Port lists
- Geolocation settings
- Database path
- Report formats

## Important Notes

1. **Rate Limiting**: ip-api.com allows 45 requests/minute. Large scans will automatically pause.

2. **Permissions**: TCP scanning may require root/admin privileges:
   ```bash
   sudo python main.py
   ```

3. **Legal**: Only scan networks you own or have explicit permission to scan.

4. **Results**: Found in `reports/` directory (JSON, CSV, HTML, and interactive maps)

## Example Output

```
Scanning 192.168.1.0/24...
Progress: 50/256 (19%)
Progress: 100/256 (39%)
⚠️ MINER DETECTED: 192.168.1.105 (Stratum) - 85.0% confidence
Progress: 256/256 (100%)
Scan complete: 42 responsive, 1 miners detected
Reports generated:
  JSON: reports/scan_1_20240217_143052.json
  CSV: reports/scan_1_20240217_143052.csv
  HTML: reports/scan_1_20240217_143052.html
  Map: reports/map_scan_1_20240217_143052.html
```
