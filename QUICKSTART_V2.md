# Quick Start Guide - Iranian Network Miner Detection System v2.0

Get started in 5 minutes! 🚀

---

## Step 1: Installation (2 minutes)

```bash
# Navigate to the project directory
cd /home/engine/project/ilam_miner_detector

# Install all dependencies
pip install -r requirements.txt

# Verify installation
python main_extended.py --version
```

**Expected Output:**
```
Iranian Network Miner Detection System v2.0.0
```

---

## Step 2: Your First Scan (1 minute)

### Option A: CLI Mode (Fastest)

```bash
# Scan your local network
python main_extended.py scan --cidr 192.168.1.0/24
```

### Option B: GUI Mode (Visual)

```bash
# Launch the GUI
python main_extended.py
```

Then:
1. Enter CIDR: `192.168.1.0/24`
2. Click "Start Network Scan"
3. Watch real-time progress

---

## Step 3: View Results (30 seconds)

### CLI Results

After the scan completes, you'll see:
```
Scan complete: 15 responsive, 2 miners detected
```

### GUI Results

1. Check the "Results" tab
2. View the "Analytics" dashboard
3. Explore the "Map" tab for geographic visualization

---

## Step 4: Generate Reports (30 seconds)

```bash
# Generate all report formats with analytics
python main_extended.py scan \
  --cidr 192.168.1.0/24 \
  --export \
  --map \
  --analytics
```

**Generated Files:**
- `reports/scan_X_json.json` - Structured data
- `reports/scan_X_csv.csv` - Spreadsheet format
- `reports/scan_X_html.html` - Web report
- `reports/scan_X_pdf.pdf` - Professional PDF
- `reports/scan_X_excel.xlsx` - Multi-sheet Excel
- `reports/map_cli_X_timestamp.html` - Interactive map
- `reports/charts/` - Analytics charts

---

## Step 5: Explore Features

### List Iranian Provinces

```bash
python main_extended.py provinces
```

### List ISPs

```bash
python main_extended.py isps
```

### View Statistics

```bash
python main_extended.py stats
```

---

## Common Use Cases

### Use Case 1: Scan Specific Network

```bash
# Scan a /24 network
python main_extended.py scan --cidr 10.0.0.0/24 --export
```

### Use Case 2: Scan with Custom Ports

```bash
# Scan specific ports only
python main_extended.py scan \
  --cidr 192.168.1.0/24 \
  --ports 3333,4444,8332 \
  --export
```

### Use Case 3: Generate Full Report

```bash
# Complete scan with all reports
python main_extended.py scan \
  --cidr 192.168.1.0/24 \
  --name "Daily Security Audit" \
  --export \
  --map \
  --analytics
```

### Use Case 4: Province-Based Analysis

```bash
# List all provinces
python main_extended.py provinces --detailed --show-cities

# Get city information (GUI feature)
# 1. Launch GUI: python main_extended.py
# 2. Select province from dropdown
# 3. Select city
# 4. Auto-fetch IP ranges
# 5. Start scan
```

### Use Case 5: ISP Risk Analysis

```bash
# List ISPs with risk scores
python main_extended.py isps --detailed

# Check ISP for specific IP (in code)
python -c "
from iran_isps import identify_isp
isp = identify_isp('91.98.0.1')
print(f'ISP: {isp.name}')
print(f'Risk Score: {isp.risk_score}')
"
```

---

## Configuration

### Basic Configuration

Edit `config/config_extended.json`:

```json
{
  "scan": {
    "timeout": 3,
    "concurrency": 50,
    "rate_limit_enabled": true
  },
  
  "detection_rules": {
    "enabled": true,
    "confidence_threshold": 0.5
  },
  
  "reporting": {
    "export_formats": ["json", "csv", "html", "pdf", "excel"],
    "include_charts": true
  }
}
```

### Custom Detection Rules

Create `config/detection_rules.yaml`:

```yaml
version: "1.0"
rules:
  - name: "my_custom_rule"
    description: "My custom detection rule"
    enabled: true
    priority: 90
    confidence_score: 0.85
    conditions:
      ports: [8080, 8081]
      banner_patterns: ["custom_signature"]
    actions: ["log", "alert"]
```

---

## Troubleshooting

### Problem: Import Errors

**Solution:**
```bash
pip install -r requirements.txt
```

### Problem: Permission Denied

**Solution:**
```bash
sudo python main_extended.py scan --cidr ...
```

### Problem: No Results Found

**Solution:**
- Verify CIDR range is correct
- Check firewall settings
- Ensure target hosts are online
- Increase timeout in config

### Problem: Map Not Generated

**Solution:**
- Check internet connectivity
- Verify geolocation API is accessible
- Check if IPs have geolocation data

---

## Advanced Usage

### Scheduled Scanning

```python
from scheduler import ScheduledScan, ScheduleFrequency, get_scheduler

# Create schedule
scheduler = get_scheduler()
scheduler.start()

scan_schedule = ScheduledScan(
    name="Daily Scan",
    cidr_range="192.168.1.0/24",
    frequency=ScheduleFrequency.DAILY,
    export_reports=True
)

def scan_callback(schedule):
    print(f"Running scheduled scan: {schedule.name}")
    # Your scan logic here

scheduler.add_schedule(scan_schedule, scan_callback)
```

### VPN Detection

```python
from vpn_detector import get_vpn_detector

detector = get_vpn_detector()
result = detector.check_ip("91.98.0.1")

print(f"VPN: {result.is_vpn}")
print(f"Proxy: {result.is_proxy}")
print(f"Confidence: {result.confidence}")
```

### Analytics

```python
from analytics import get_analytics_service

analytics = get_analytics_service()

# Generate charts
charts = analytics.generate_matplotlib_charts()
interactive = analytics.generate_plotly_charts()

# Export analytics
analytics.export_analytics_json("reports/analytics.json")
```

---

## Tips & Best Practices

1. **Start Small**: Test with small networks first
2. **Use Rate Limiting**: Be polite when scanning
3. **Cache Geolocation**: Reduce API calls
4. **Regular Backups**: Backup your database
5. **Monitor Logs**: Check `data/iranian_miner_detector.log`
6. **Update Rules**: Keep detection rules current
7. **Generate Reports**: Create regular reports for tracking
8. **Use VPN Detection**: Identify anonymized traffic
9. **Check ISP Risk**: Focus on high-risk ISPs
10. **Analyze Trends**: Use analytics dashboards

---

## Legal Compliance

⚠️ **IMPORTANT**: 

- Only scan networks you own or have written permission to scan
- Document all authorization before scanning
- Comply with Iranian cyberlaw
- All activities are logged for accountability

---

## Next Steps

1. ✅ Complete your first scan
2. ✅ Generate a full report
3. ✅ Explore the GUI features
4. ✅ Try province-based scanning
5. ✅ Review detection rules
6. ✅ Set up scheduled scans
7. ✅ Analyze trends with analytics
8. ✅ Customize detection rules
9. ✅ Integrate with your workflow
10. ✅ Contribute improvements

---

## Support

- **Documentation**: `IRANIAN_NETWORK_MINER_DETECTION_SYSTEM_V2.md`
- **Examples**: `ilam_miner_detector/examples.py`
- **Logs**: `data/iranian_miner_detector.log`
- **Database**: `data/iranian_miner_detector.db`

---

**Ready to detect miners? Let's get started!** 🚀

For comprehensive documentation, see the full guide: `IRANIAN_NETWORK_MINER_DETECTION_SYSTEM_V2.md`
