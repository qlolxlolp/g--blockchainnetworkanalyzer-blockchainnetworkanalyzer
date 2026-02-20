# Quick Start Guide - Iranian Miner Detector

Get started with Iranian Miner Detector in 5 minutes!

## System Requirements

- **Windows 10** (version 1809 or later) or **Windows 11**
- **2GB RAM** minimum (4GB recommended)
- **200MB free disk space**
- **Internet connection** (for geolocation and map tiles)

## Installation

### Option 1: Standalone Executable (Recommended)

1. Download `IranianMinerDetector.WinForms.exe`
2. Place it anywhere on your computer
3. Double-click to run - **No installation required!**

### Option 2: Installer

1. Download `IranianMinerDetector-Setup.exe`
2. Double-click to run installer
3. Follow the installation wizard
4. Launch from desktop shortcut or Start menu

## First Launch

When you first run the application:

1. **WebView2 Initialization** - The map feature requires Microsoft Edge WebView2. If not installed, you'll see a prompt to download it.
2. **Database Creation** - The application automatically creates a local database for storing scan results.
3. **Ready to Scan** - You're now ready to start scanning!

## Basic Usage

### Perform Your First Scan

**Step 1: Select Target Area**
```
1. Click on "استان (Province)" dropdown
2. Select an Iranian province (e.g., تهران - Tehran)
3. Optionally select a city
```

**Step 2: Configure Scan Parameters**
```
1. ISP: Leave on "All ISPs" or select a specific provider
2. IP Range: Leave empty (auto-generated) or enter custom range (e.g., 192.168.1.0/24)
3. Ports: Use default ports or customize (e.g., 8332,8333,3333,4028)
4. Timeout: Default 3000ms is suitable for most networks
5. Concurrency: Default 100 is recommended
```

**Step 3: Start Scanning**
```
1. Click "شروع اسکن (Start Scan)" button (green)
2. Watch the progress bar and statistics
3. Click "توقف (Stop)" button (red) to cancel if needed
```

**Step 4: View Results**
```
1. Results Tab: See detailed table of scanned hosts
2. Map Tab: View geographic distribution of miners
3. Log Tab: Review scan log with color-coded messages
4. History Tab: Access previous scans (double-click to reload)
```

### Understanding Results

| Column | Description |
|--------|-------------|
| IP Address | The scanned IP address |
| Status | Online (green) or Offline (red) |
| Response (ms) | Network response time |
| Open Ports | Ports that responded to scan |
| Miner | ⛏️ MINER badge if miner detected |
| Confidence | Detection confidence (0-100%) |
| Service | Detected mining service |
| ISP | Internet service provider |
| Location | Geographic location |

## Exporting Reports

### PDF Report
1. Click **Tools** → **خروجی PDF**
2. Wait for report generation
3. PDF opens automatically in default viewer

### Excel Report
1. Click **Tools** → **خروجی Excel**
2. Excel file opens automatically

### CSV Report
1. Click **Tools** → **خروجی CSV**
2. CSV file opens in default application (e.g., Excel)

### HTML Report
1. Click **Tools** → **خروجی HTML** (if available)
2. HTML report opens in web browser
3. Includes embedded interactive map!

## Configuration

### Access Settings

1. Click **Tools** → **تنظیمات (Settings)**
2. Configure the following:

#### Geolocation API Settings
- **Provider**: Choose from ip-api.com, ipinfo.io, or ipgeolocation.io
- **API Key**: Enter optional API key if required

#### Default Scan Settings
- **Timeout**: Default connection timeout (500-10000ms)
- **Concurrency**: Number of simultaneous connections (10-500)
- **Default Ports**: Comma-separated list of ports
- **Banner Grab**: Enable/disable service banner detection
- **Geolocation**: Enable/disable IP location lookup

### Clear Cache

If you want to clear the geolocation cache:
1. Click **Tools** → **تنظیمات (Settings)**
2. Click **پاک کردن کش (Clear Cache)**
3. Confirm the action

## Common Tasks

### Scan a Specific IP Range

1. Enter CIDR notation in **IP Range** field
   - Example: `192.168.1.0/24` (all IPs from 192.168.1.0 to 192.168.1.255)
   - Example: `10.0.0.0/8` (large range - be careful!)
   - Example: `172.16.0.1-172.16.0.50` (specific range)

### Focus on Specific Mining Protocol

1. Edit **Ports** field
2. Enter only the ports for your target:
   - Bitcoin: `8332,8333,3333,4028`
   - Ethereum: `30303,8545,4444`
   - Monero: `18081`
   - Litecoin: `9332,9333`

### Scan Faster (Less Accurate)

1. Increase **Timeout** to 1000ms
2. Increase **Concurrency** to 200-300
3. Disable **Banner Grab** in settings
4. Disable **Geolocation** in settings

### Scan Slower (More Accurate)

1. Increase **Timeout** to 5000ms
2. Decrease **Concurrency** to 20-50
3. Enable **Banner Grab** in settings
4. Enable **Geolocation** in settings

### View Previous Scan Results

1. Click **History** tab
2. Double-click on any scan row
3. Results load in **Results** and **Map** tabs

### Share Scan Results

1. Complete your scan
2. Export to PDF or Excel
3. Share the file with colleagues
4. They can view it without the application

## Tips and Best Practices

### For Best Performance
- Start with smaller IP ranges
- Use appropriate timeout values
- Don't set concurrency too high (causes network issues)
- Close other network-intensive applications

### For Best Accuracy
- Use higher timeout values
- Enable banner grabbing
- Enable geolocation
- Lower concurrency
- Scan during low network traffic

### For Security Audits
- Get proper authorization before scanning
- Document all scans and results
- Use VPN if scanning external networks
- Keep scan logs for compliance
- Follow organizational policies

### For Research/Analysis
- Export results to CSV for data analysis
- Use Excel pivot tables for statistics
- Generate PDF reports for documentation
- Use map for geographic analysis
- Compare multiple scans over time

## Troubleshooting

### Map Not Displaying

**Problem:** Map tab is blank or shows error

**Solutions:**
1. Ensure WebView2 is installed
2. Check internet connection (maps need online tiles)
3. Try refreshing the page (right-click → Reload)

### Scan is Very Slow

**Problem:** Scan takes a very long time

**Solutions:**
1. Reduce the IP range
2. Decrease timeout value
3. Increase concurrency
4. Disable geolocation in settings
5. Check your network connection

### "Access Denied" Errors

**Problem:** Getting access denied errors

**Solutions:**
1. Run application as Administrator
2. Check Windows Firewall settings
3. Ensure you have permission to scan target network
4. Some networks block ICMP pings

### Application Won't Start

**Problem:** Application doesn't launch

**Solutions:**
1. Check Windows SmartScreen - "Run anyway"
2. Ensure .NET 8 is installed (for non-self-contained builds)
3. Check Windows Event Viewer for errors
4. Try running from different location

### Results Show All Offline

**Problem:** All hosts show as offline

**Solutions:**
1. Verify IP range is correct
2. Check network connectivity
3. Increase timeout value
4. Test with known online IP (e.g., 8.8.8.8)
5. Check if target network blocks ICMP

## Keyboard Shortcuts

- **Ctrl+S**: Save current scan
- **Ctrl+O**: Open previous scan (from history)
- **Ctrl+E**: Export to Excel
- **Ctrl+P**: Export to PDF
- **F5**: Refresh data grids
- **Escape**: Stop current scan
- **F1**: Open this help

## Getting Help

### Built-in Help
- Click **Help** → **درباره (About)** for version info
- Check tooltips by hovering over controls

### Online Resources
- [Video Tutorials](https://youtube.com/playlist)
- [User Manual](https://docs.iranian-network-security.local)
- [FAQ](https://faq.iranian-network-security.local)

### Support
- **Email**: support@iranian-network-security.local
- **Issues**: [GitHub Issues](https://github.com/iranian-network-security/issues)
- **Community Forum**: [Link to forum]

## Advanced Features

### Using Command Line (Future)
```bash
# Scan specific range
IranianMinerDetector.WinForms.exe --scan 192.168.1.0/24 --ports 8332,8333

# Export results
IranianMinerDetector.WinForms.exe --export-pdf --scan-id 123
```

### Scheduled Scans (Future)
- Configure automatic scans at specific times
- Set up recurring scans for monitoring
- Email notifications on miner detection

### Custom Scripts (Future)
- Python API for automation
- PowerShell integration
- Plugin system for custom detection

## Legal and Ethical Use

⚠️ **IMPORTANT**:

1. **Always get permission** before scanning networks you don't own
2. **Follow local laws** and regulations
3. **Respect privacy** of network users
4. **Use responsibly** for legitimate security purposes only
5. **Don't scan** unauthorized networks
6. **Report findings** appropriately

## What's Next?

1. **Explore the Interface** - Try all tabs and menus
2. **Read the Full Manual** - Available online
3. **Watch Tutorials** - Video guides available
4. **Join Community** - Connect with other users
5. **Provide Feedback** - Help us improve the application

## Keyboard Shortcuts Reference

| Shortcut | Action |
|----------|--------|
| Ctrl+S | Save scan |
| Ctrl+O | Open scan |
| Ctrl+E | Export Excel |
| Ctrl+P | Export PDF |
| Ctrl+C | Copy selection |
| Ctrl+V | Paste |
| Ctrl+A | Select all |
| F5 | Refresh |
| Escape | Stop scan |
| F1 | Help |

---

**Version:** 1.0.0
**Last Updated:** 2024
**Questions?** Contact support@iranian-network-security.local
