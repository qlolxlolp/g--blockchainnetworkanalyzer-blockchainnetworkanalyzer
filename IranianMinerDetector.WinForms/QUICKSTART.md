# Quick Start Guide - Iranian Miner Detector (WinForms)

## Installation

1. Download the self-contained executable (`IranianMinerDetector.WinForms.exe`)
2. Place it in any folder on your Windows PC
3. Double-click to run - no installation needed!

## Basic Usage

### Step 1: Select Target Location
- Choose a Province from the dropdown (e.g., "Tehran")
- Optionally select a specific City
- Optionally select an ISP (e.g., "TCI" or "Irancell")

### Step 2: Configure Scan
- **IP Range**: Leave empty for automatic or enter:
  - CIDR: `192.168.1.0/24`
  - Range: `192.168.1.1-192.168.1.100`
- **Ports**: Default mining ports pre-configured
- **Timeout**: 3000ms (adjust for slow networks)
- **Concurrency**: 100 simultaneous scans

### Step 3: Start Scanning
1. Click "Start Scan" (green button)
2. Watch progress in the status panel
3. Results appear in the Results tab in real-time
4. Miners are highlighted in red

### Step 4: View Results
- **Results Tab**: Table of all scanned hosts
- **Map Tab**: Interactive map showing detected miners
- **Log Tab**: Detailed scan log with timestamps
- **History Tab**: Previous scan results

### Step 5: Export Reports
- Go to File menu → Export format
- Choose PDF, Excel, or CSV
- Reports open automatically after generation

## Shortcuts

- **Start Scan**: Click green button or press F5
- **Stop Scan**: Click red button or press ESC
- **Export PDF**: File → Export PDF
- **View Map**: Click Map tab
- **View History**: Double-click scan in History tab

## Tips

### Faster Scans
- Increase "Concurrency" (up to 500)
- Decrease "Timeout" (as low as 500ms)
- Select specific ISP instead of all ISPs

### Better Detection
- Use default mining ports
- Enable "Banner Grab" in settings
- Check detected miners in Results tab

### Troubleshooting
- **Scan fails?** Run as Administrator
- **Maps not showing?** Install WebView2 Runtime
- **Slow scans?** Reduce concurrency or increase timeout
- **No miners found?** Try different provinces/ISPs

## Common Scenarios

### Scan Home Network
1. IP Range: `192.168.1.0/24`
2. Ports: Default
3. Click Start

### Scan Specific ISP
1. Province: Any or specific
2. ISP: Select from dropdown (e.g., "Irancell")
3. Click Start

### Scan for Bitcoin Miners
1. Ports: `8332,8333,3333`
2. Concurrency: 50
3. Click Start

### Quick Test
1. IP Range: `127.0.0.1-127.0.0.10`
2. Click Start (tests localhost)

## Understanding Results

### Status Column
- **Online**: Host responded to ping
- **Offline**: Host didn't respond

### Miner Column
- **Yes**: Mining operation detected
- **No**: No mining activity found

### Confidence Score
- **80-100%**: High confidence
- **50-79%**: Medium confidence
- **Below 50%**: Low confidence (may be false positive)

### Service Column
- Shows detected service name (e.g., "Bitcoin RPC")
- "Mining Operation Detected" for miners

## File Locations

- **Application**: Where you placed the .exe
- **Database**: `%LOCALAPPDATA%\IranianMinerDetector\`
- **Reports**: Desktop\IranianMinerDetector\Reports\`
- **Maps**: `%LOCALAPPDATA%\IranianMinerDetector\Maps\`

## Keyboard Shortcuts

- **F5**: Start Scan
- **ESC**: Stop Scan
- **Ctrl+E**: Export Report
- **Ctrl+H**: Show History
- **Ctrl+M**: Show Map
- **Alt+F4**: Exit

## Support

For help:
1. Check the main README.md
2. Review Troubleshooting section
3. Check application logs in Log tab

## Safety Notes

- Always run antivirus scans on downloaded files
- Network scanning may trigger security alerts
- Use responsibly and legally
- Only scan networks you own or have permission to scan

---

**Ready to detect miners? Click Start Scan!** 🛡️
