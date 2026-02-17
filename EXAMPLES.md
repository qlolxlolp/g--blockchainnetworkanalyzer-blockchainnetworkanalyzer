# Ilam Miner Detector - Usage Examples

## Example 1: Basic Local Network Scan

### Scenario
You want to check your home/office network for unauthorized miners.

### Configuration
```json
{
  "scan": {
    "timeout_ms": 2000,
    "max_concurrent": 50,
    "ping_enabled": true,
    "banner_grab_enabled": true
  }
}
```

### Steps
1. Launch application: `python main.py`
2. Enter target: `192.168.1.0/24`
3. Ports: `3333,4444,8332,8333,8545` (default)
4. Uncheck "Enable Geolocation" (faster for local)
5. Uncheck "Filter for Ilam Region"
6. Click "Start Scan"

### Expected Results
- Scan completes in 2-3 minutes
- Discovers active hosts
- Identifies any miners on typical ports
- No geolocation data (disabled)

### Sample Output
```
[INFO] Starting scan of 192.168.1.0/24
[INFO] Parsed 254 IP addresses
[INFO] Scanning 192.168.1.1...
[INFO] Host found: 192.168.1.1
[INFO] Scanning 192.168.1.100...
[SUCCESS] MINER DETECTED: 192.168.1.100 (stratum)
[INFO] Scan completed! Found 15 hosts
```

---

## Example 2: Ilam Region Miner Detection

### Scenario
You want to detect cryptocurrency miners specifically in Ilam province, Iran.

### Configuration
```json
{
  "geolocation": {
    "primary_provider": "ip-api",
    "rate_limit_per_minute": 45,
    "cache_enabled": true,
    "ilam_lat_min": 32.5,
    "ilam_lat_max": 33.5,
    "ilam_lon_min": 46.0,
    "ilam_lon_max": 47.5
  }
}
```

### Steps
1. Launch application
2. Enter target: `YOUR_PUBLIC_IP_RANGE` (e.g., `185.51.200.0/24`)
3. Keep default miner ports
4. **Check "Enable Geolocation"**
5. **Check "Filter for Ilam Region Only"**
6. Click "Start Scan"

### Expected Results
- Slower scan (~1.5s per IP due to geolocation)
- Only hosts in Ilam region are shown
- Map tab displays geographic distribution
- Results include city/region/coordinates

### Sample Output
```
[INFO] Starting scan of 185.51.200.0/24
[INFO] Geolocation lookup for 185.51.200.50...
[INFO] IP 185.51.200.50 is in Ilam region: (33.1234, 46.5678)
[SUCCESS] MINER DETECTED: 185.51.200.50 (stratum)
[INFO] Skipping 185.51.200.100 - outside Ilam region
```

---

## Example 3: Thorough Security Audit

### Scenario
Comprehensive audit of a company network for policy compliance.

### Configuration
```json
{
  "scan": {
    "timeout_ms": 5000,
    "max_concurrent": 30,
    "retry_count": 3,
    "ping_enabled": true,
    "banner_grab_enabled": true,
    "banner_timeout_ms": 3000
  },
  "miner_ports": {
    "stratum": [3333, 4444, 4028, 5555, 7777, 8888, 9999, 14433, 14444],
    "bitcoin": [8332, 8333, 18332, 18333],
    "ethereum": [8545, 8546, 30303, 30304],
    "generic": [8080, 8081, 3000, 9090, 6666]
  }
}
```

### Steps
1. Get written authorization from management
2. Schedule scan during maintenance window
3. Launch application
4. Enter target: `10.0.0.0/16` (example corporate network)
5. Use all miner ports (as configured above)
6. Enable all options:
   - ✓ ICMP Ping
   - ✓ Banner Grabbing
   - ✓ Geolocation
   - ✗ Filter Ilam Region (internal network)
7. Click "Start Scan"

### During Scan
- Monitor progress bar
- Review log tab for issues
- Watch for miner detections in real-time

### After Scan
1. Export JSON report for archival
2. Export CSV for spreadsheet analysis
3. Export HTML for management presentation
4. Document findings:
   - Total hosts scanned
   - Miners detected (authorized/unauthorized)
   - Recommended actions

### Sample Report Structure
```
Security Audit Report
=====================
Date: 2024-02-16
Target: 10.0.0.0/16
Duration: 2h 15m

Summary:
- Total IPs scanned: 65,534
- Active hosts: 1,247
- Miners detected: 5

Findings:
1. 10.0.50.100 - Stratum miner (Ports: 3333, 4444)
   Status: UNAUTHORIZED - Employee workstation
   Action: Investigate and remove

2. 10.0.100.50 - Bitcoin node (Ports: 8332, 8333)
   Status: Authorized - IT testing environment
   Action: No action required

[... continue for all findings ...]
```

---

## Example 4: Single IP Deep Scan

### Scenario
You've received a tip about a specific IP and want to investigate.

### Steps
1. Launch application
2. Enter target: `185.51.200.100` (single IP)
3. Enter **all known miner ports**: 
   ```
   3333,4444,4028,5555,7777,8080,8081,8332,8333,8545,8546,8888,9090,9999,14433,14444,30303,30304
   ```
4. Settings:
   - Timeout: 5000ms (thorough)
   - Max Concurrent: 1 (single target)
   - Enable all options
5. Click "Start Scan"

### Expected Results
- Completes in ~30 seconds
- Checks all ports thoroughly
- Grabs banners from all open ports
- Provides geolocation data
- Identifies miner type if present

### Analysis
Review banner data in Results tab:
```
[Port 3333]: {"id":null,"result":["mining.notify","5f"],"error":null}
[Port 4444]: Connection refused
[Port 8332]: HTTP/1.1 401 Authorization Required
```

Banner analysis reveals:
- Port 3333: Stratum protocol response → **CONFIRMED MINER**
- Port 8332: Bitcoin RPC (requires auth) → **LIKELY BITCOIN NODE**

---

## Example 5: Custom Port Scan

### Scenario
Looking for custom mining software on non-standard ports.

### Configuration
Add custom ports to config:
```json
{
  "miner_ports": {
    "custom": [10000, 10001, 10002, 12345, 54321]
  }
}
```

### Steps
1. Launch application
2. Enter target: `192.168.100.0/24`
3. Enter custom ports: `10000,10001,10002,12345,54321`
4. Start scan

### Expected Results
- Scans non-standard ports
- Discovers services on unusual ports
- Banner grabbing may reveal mining software

---

## Example 6: Multiple Network Ranges

### Scenario
Scanning multiple non-contiguous networks.

### Approach 1: Sequential Scans
Scan each range separately:
1. Scan `192.168.1.0/24`
2. Export results
3. Scan `10.0.0.0/24`
4. Export results
5. Scan `172.16.0.0/24`
6. Export results
7. Combine reports

### Approach 2: Comma-Separated
Use comma-separated input:
```
192.168.1.1, 10.0.0.1, 172.16.0.1
```
(Only specific IPs from each range)

---

## Example 7: Batch Export Workflow

### Scenario
Regular weekly scans with automated reporting.

### Workflow
1. **Monday 9 AM**: Run scan
   - Target: `10.0.0.0/16`
   - Save as: `Weekly_Scan_YYYYMMDD`

2. **After scan completes**:
   - Export JSON → `reports/weekly_YYYYMMDD.json`
   - Export CSV → `reports/weekly_YYYYMMDD.csv`
   - Export HTML → `reports/weekly_YYYYMMDD.html`

3. **Analysis**:
   - Open CSV in Excel
   - Filter for `is_miner = 1`
   - Compare with previous week's results
   - Identify new miners

4. **Reporting**:
   - Email HTML report to security team
   - Archive JSON for historical analysis
   - Update tracking spreadsheet

---

## Example 8: Map-Focused Analysis

### Scenario
Geographic visualization of mining activity.

### Steps
1. Run scan with geolocation enabled
2. After completion, click "Map" tab
3. Interact with map:
   - Zoom to Ilam province
   - Click markers for details
   - Observe heatmap density
   - Identify clusters

### Map Features
- **Red clusters**: High concentration of Stratum miners
- **Blue scattered**: Individual Ethereum nodes
- **Heatmap**: Overall mining activity density

### Export
- Click "Export HTML"
- Share interactive map with team
- Embed in reports or dashboards

---

## Example 9: Troubleshooting Network Issues

### Scenario
Network admin reports slow network, suspects mining.

### Investigation Process

1. **Broad Scan**:
   - Target: Entire subnet
   - Ports: All miner ports
   - Goal: Find any miners

2. **Analysis**:
   - Review open ports column
   - Check for unusual port combinations
   - Examine hostnames for clues

3. **Deep Dive**:
   - Target: Suspicious IPs individually
   - Enable banner grabbing
   - Document exact services

4. **Verification**:
   - Physical inspection if possible
   - Check system logs
   - Interview users

5. **Remediation**:
   - Block miner ports at firewall
   - Remove unauthorized software
   - Update acceptable use policy

---

## Example 10: API Rate Limit Management

### Scenario
Scanning large network without hitting geolocation rate limits.

### Strategy 1: Caching
```json
{
  "geolocation": {
    "cache_enabled": true
  }
}
```
- First scan: Full geolocation lookups
- Subsequent scans: Use cached data

### Strategy 2: Batching
- Scan 40 IPs
- Wait 60 seconds
- Scan next 40 IPs
- Repeat

### Strategy 3: Premium API
```json
{
  "geolocation": {
    "fallback_provider": "ipinfo",
    "api_key": "YOUR_IPINFO_API_KEY"
  }
}
```
- Fallback to ipinfo.io (higher limits)
- Requires paid API key

---

## Command-Line Tips

### Verbose Logging
```bash
python main.py --verbose
```
See detailed debug information.

### Custom Config
```bash
python main.py --config /path/to/custom/config.json
```
Use different configuration.

### Create Config Template
```bash
python main.py --create-config
```
Generate default config file.

---

## Common Patterns

### Pattern 1: Daily Automated Scan
```bash
#!/bin/bash
# daily_scan.sh

DATE=$(date +%Y%m%d)
python main.py --config config/automated.json

# Wait for manual export or implement auto-export
# Then move reports
mv reports/*.json reports/archive/
mv reports/*.csv reports/archive/
mv reports/*.html reports/archive/
```

### Pattern 2: Multi-Stage Scan
1. **Stage 1**: Quick ping sweep (no ports)
2. **Stage 2**: Port scan on reachable hosts only
3. **Stage 3**: Deep dive on hosts with miner ports
4. **Stage 4**: Geolocation lookup on confirmed miners

### Pattern 3: Incident Response
1. **Alert received**: Suspicious network activity
2. **Quick scan**: Target specific subnet
3. **Identify culprit**: Check miner detections
4. **Immediate action**: Block/isolate host
5. **Full report**: Generate for incident log

---

## Best Practices

1. **Always test on local network first**
2. **Start with small ranges** (/27 or /28)
3. **Enable verbose logging** for first scans
4. **Save scan results** before closing application
5. **Review log tab** for errors or issues
6. **Use appropriate timeouts** for network type:
   - Local: 1000-2000ms
   - WAN: 3000-5000ms
7. **Respect rate limits** (geolocation)
8. **Document all scans** (date, target, findings)
9. **Get authorization** before scanning
10. **Handle data securely** (reports may contain sensitive info)

---

## Error Recovery

### Scan Interrupted
- Results in database are preserved
- Can review partial results
- Re-run scan if needed

### Database Locked
```bash
# Close all instances
pkill -f "python main.py"

# Remove lock files
rm data/ilam_miner.db-wal
rm data/ilam_miner.db-shm
```

### Rate Limit Hit
- Wait 60 seconds
- Enable caching
- Reduce scan speed
- Consider premium API

---

## Performance Optimization

### Fast Scan (Local Network)
```json
{
  "timeout_ms": 1000,
  "max_concurrent": 100,
  "ping_enabled": false,
  "banner_grab_enabled": false,
  "geolocation": false
}
```

### Balanced Scan
```json
{
  "timeout_ms": 3000,
  "max_concurrent": 50,
  "ping_enabled": true,
  "banner_grab_enabled": true
}
```

### Thorough Scan
```json
{
  "timeout_ms": 5000,
  "max_concurrent": 20,
  "ping_enabled": true,
  "banner_grab_enabled": true,
  "banner_timeout_ms": 3000
}
```

---

**Remember**: This tool is for authorized security auditing only. Always obtain proper authorization before scanning networks.
