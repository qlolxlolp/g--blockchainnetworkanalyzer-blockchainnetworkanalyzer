# Ilam Miner Detector - Quick Start Guide

## 5-Minute Setup

### Step 1: Install Dependencies
```bash
pip install -r requirements.txt
```

### Step 2: Create Configuration
```bash
python main.py --create-config
```

### Step 3: Launch Application
```bash
python main.py
```

## First Scan - Safe Testing

### Test on Local Network (Recommended)
1. Launch the application
2. Enter your local IP range: `192.168.1.0/24` (or your subnet)
3. Keep default ports: `3333,4444,8332,8333,8545`
4. **Uncheck "Enable Geolocation"** for faster local testing
5. **Uncheck "Filter for Ilam Region"**
6. Click "Start Scan"

### Understanding Results

#### Results Tab
- **Green rows**: Regular hosts with open ports
- **Red/pink rows**: Detected miners
- **Columns**:
  - IP Address: Target host
  - Hostname: Resolved DNS name
  - Open Ports: Detected services
  - Miner Type: stratum, bitcoin, ethereum, etc.
  - Location: City, Region, Country (if geolocation enabled)

#### Map Tab
- Interactive map with color-coded markers
- Red: Stratum miners
- Orange: Bitcoin nodes
- Blue: Ethereum nodes
- Purple: Monero miners
- Click markers for details

#### Log Tab
- Real-time scan progress
- [INFO]: Normal operations
- [SUCCESS]: Miner detected
- [WARN]: Issues or skipped hosts
- [ERROR]: Failures

## Example Scans

### 1. Single IP
```
Target: 192.168.1.100
```

### 2. IP Range
```
Target: 192.168.1.1-192.168.1.50
```

### 3. CIDR Notation
```
Target: 10.0.0.0/24
```
(Scans 10.0.0.1 through 10.0.0.254)

### 4. Multiple IPs
```
Target: 192.168.1.10, 192.168.1.20, 192.168.1.30
```

### 5. Custom Ports
```
Target: 192.168.1.0/24
Ports: 22,80,443,3389
```
(Scan for SSH, HTTP, HTTPS, RDP instead of miners)

## Common Miner Ports

- **3333**: Stratum mining (most common)
- **4444**: Alternative Stratum port
- **8332**: Bitcoin Core RPC
- **8333**: Bitcoin P2P network
- **8545**: Ethereum JSON-RPC
- **30303**: Ethereum P2P network

## Export Results

After scan completes:

### JSON Export
- Full structured data
- Importable into other tools
- Includes all metadata

### CSV Export
- Open in Excel/Google Sheets
- Easy filtering and sorting
- Suitable for reports

### HTML Export
- Professional presentation
- Embedded interactive map
- Shareable via web browser

## Performance Tuning

### Fast Scan (Local Network)
```json
{
  "timeout_ms": 1000,
  "max_concurrent": 100,
  "ping_enabled": false
}
```

### Thorough Scan (Internet)
```json
{
  "timeout_ms": 5000,
  "max_concurrent": 20,
  "ping_enabled": true,
  "banner_grab_enabled": true
}
```

### Geolocation Enabled
```json
{
  "timeout_ms": 3000,
  "max_concurrent": 40
}
```
(Rate limited to ~45 lookups/minute by ip-api.com)

## Troubleshooting Quick Fixes

### "No module named PyQt5"
```bash
pip install PyQt5 PyQtWebEngine
```
Or on Ubuntu/Debian:
```bash
sudo apt-get install python3-pyqt5 python3-pyqt5.qtwebengine
```

### "Permission denied" on ping
Option 1: Run with sudo
```bash
sudo python main.py
```

Option 2: Disable ping
- Uncheck "Enable ICMP Ping" in GUI
- Or edit config.json: `"ping_enabled": false`

### Scan is slow
- Decrease timeout: 1000-2000ms
- Increase max_concurrent: 75-100
- Disable geolocation for local scans
- Scan smaller ranges (/27 or /28 instead of /24)

### "Rate limit exceeded"
- Geolocation limited to 45 requests/minute
- Enable caching (default)
- Wait 60 seconds before retrying
- Get ipinfo.io API key for fallback

### Application won't start
Check logs:
```bash
cat data/logs/ilam_miner_detector.log
```

Run with verbose mode:
```bash
python main.py --verbose
```

## Safety Tips

✅ **DO**:
- Test on your own network first
- Get written authorization before scanning external networks
- Start with small ranges (/27 or /28)
- Use conservative timeout and concurrency settings
- Check local laws regarding network scanning

❌ **DON'T**:
- Scan networks you don't own or have permission to scan
- Use aggressive settings on production networks
- Ignore warnings from network administrators
- Assume open ports = malicious activity (false positives exist)

## What is a "Miner"?

This tool detects cryptocurrency mining software by:
1. **Port scanning**: Checking for common miner ports (3333, 4444, 8332, etc.)
2. **Banner analysis**: Reading service responses for miner keywords
3. **Heuristics**: Detecting suspicious port combinations

### Legitimate Uses
- Your own mining rigs
- Bitcoin/Ethereum full nodes
- Development environments
- Mining pool servers

### Potentially Unauthorized
- Unexpected miners on company networks
- Hidden miners on compromised systems
- Cryptojacking malware

**Always investigate before taking action.**

## Next Steps

1. ✅ Complete a test scan on local network
2. ✅ Review results in all three tabs
3. ✅ Export a report (try HTML for full experience)
4. ✅ Customize configuration for your needs
5. ✅ Read full documentation (ILAM_MINER_DETECTOR_README.md)

## Getting Help

- Review full README: `ILAM_MINER_DETECTOR_README.md`
- Check logs: `data/logs/ilam_miner_detector.log`
- Verify configuration: `config/config.json`
- Run verbose mode: `python main.py --verbose`

---

**Happy scanning! Remember: Always get authorization first.** 🔐
