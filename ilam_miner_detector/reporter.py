"""
Report generation for scan results.
Supports JSON, CSV, and HTML formats.
"""

import json
import csv
from pathlib import Path
from typing import List, Dict, Any
from datetime import datetime
import logging


class Reporter:
    """Generates reports from scan results in various formats."""
    
    def __init__(self, output_dir: str = "reports"):
        """
        Initialize reporter.
        
        Args:
            output_dir: Directory for output files
        """
        self.output_dir = Path(output_dir)
        self.output_dir.mkdir(parents=True, exist_ok=True)
        self.logger = logging.getLogger(__name__)
    
    def generate_json_report(self, 
                            scan_data: Dict[str, Any],
                            hosts: List[Dict[str, Any]],
                            filename: str = None) -> str:
        """
        Generate JSON report.
        
        Args:
            scan_data: Scan metadata dictionary
            hosts: List of discovered hosts
            filename: Output filename (auto-generated if None)
            
        Returns:
            Path to generated file
        """
        if filename is None:
            timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
            filename = f"scan_report_{timestamp}.json"
        
        filepath = self.output_dir / filename
        
        report = {
            'scan_metadata': scan_data,
            'hosts': hosts,
            'summary': {
                'total_hosts_scanned': scan_data.get('scanned_ips', 0),
                'miners_detected': scan_data.get('detected_miners', 0),
                'scan_duration': self._calculate_duration(
                    scan_data.get('started_at'),
                    scan_data.get('completed_at')
                )
            }
        }
        
        with open(filepath, 'w', encoding='utf-8') as f:
            json.dump(report, f, indent=2, ensure_ascii=False)
        
        self.logger.info(f"JSON report saved to {filepath}")
        return str(filepath)
    
    def generate_csv_report(self,
                           hosts: List[Dict[str, Any]],
                           filename: str = None) -> str:
        """
        Generate CSV report.
        
        Args:
            hosts: List of discovered hosts
            filename: Output filename (auto-generated if None)
            
        Returns:
            Path to generated file
        """
        if filename is None:
            timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
            filename = f"scan_report_{timestamp}.csv"
        
        filepath = self.output_dir / filename
        
        if not hosts:
            self.logger.warning("No hosts to export to CSV")
            return str(filepath)
        
        # Define CSV columns
        fieldnames = [
            'ip_address', 'hostname', 'is_reachable', 'open_ports',
            'is_miner', 'miner_type', 'city', 'region', 'country',
            'latitude', 'longitude', 'isp', 'discovered_at'
        ]
        
        with open(filepath, 'w', newline='', encoding='utf-8') as f:
            writer = csv.DictWriter(f, fieldnames=fieldnames)
            writer.writeheader()
            
            for host in hosts:
                # Parse JSON fields
                open_ports = host.get('open_ports', '[]')
                if isinstance(open_ports, str):
                    try:
                        open_ports = json.loads(open_ports)
                    except:
                        open_ports = []
                
                row = {
                    'ip_address': host.get('ip_address', ''),
                    'hostname': host.get('hostname', ''),
                    'is_reachable': host.get('is_reachable', 0),
                    'open_ports': ','.join(map(str, open_ports)),
                    'is_miner': host.get('is_miner', 0),
                    'miner_type': host.get('miner_type', ''),
                    'city': host.get('city', ''),
                    'region': host.get('region', ''),
                    'country': host.get('country', ''),
                    'latitude': host.get('latitude', ''),
                    'longitude': host.get('longitude', ''),
                    'isp': host.get('isp', ''),
                    'discovered_at': host.get('discovered_at', '')
                }
                
                writer.writerow(row)
        
        self.logger.info(f"CSV report saved to {filepath}")
        return str(filepath)
    
    def generate_html_report(self,
                            scan_data: Dict[str, Any],
                            hosts: List[Dict[str, Any]],
                            map_html_path: str = None,
                            filename: str = None) -> str:
        """
        Generate HTML report with embedded map.
        
        Args:
            scan_data: Scan metadata dictionary
            hosts: List of discovered hosts
            map_html_path: Path to Folium map HTML file
            filename: Output filename (auto-generated if None)
            
        Returns:
            Path to generated file
        """
        if filename is None:
            timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
            filename = f"scan_report_{timestamp}.html"
        
        filepath = self.output_dir / filename
        
        # Read map HTML if provided
        map_html = ""
        if map_html_path and Path(map_html_path).exists():
            with open(map_html_path, 'r', encoding='utf-8') as f:
                map_html = f.read()
        
        # Generate host table HTML
        host_rows = ""
        for host in hosts:
            open_ports = host.get('open_ports', '[]')
            if isinstance(open_ports, str):
                try:
                    open_ports = json.loads(open_ports)
                except:
                    open_ports = []
            
            miner_badge = ""
            if host.get('is_miner'):
                miner_badge = f'<span style="background: #f44336; color: white; padding: 2px 8px; border-radius: 3px;">MINER: {host.get("miner_type", "Unknown")}</span>'
            
            host_rows += f"""
            <tr>
                <td>{host.get('ip_address', '')}</td>
                <td>{host.get('hostname', 'N/A')}</td>
                <td>{', '.join(map(str, open_ports))}</td>
                <td>{miner_badge}</td>
                <td>{host.get('city', '')}, {host.get('region', '')}</td>
                <td>{host.get('latitude', '')}, {host.get('longitude', '')}</td>
            </tr>
            """
        
        # Calculate statistics
        total_scanned = scan_data.get('scanned_ips', 0)
        miners_found = scan_data.get('detected_miners', 0)
        duration = self._calculate_duration(
            scan_data.get('started_at'),
            scan_data.get('completed_at')
        )
        
        # Generate HTML
        html = f"""
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Ilam Miner Detector - Scan Report</title>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            margin: 0;
            padding: 20px;
            background: #f5f5f5;
        }}
        .container {{
            max-width: 1400px;
            margin: 0 auto;
            background: white;
            padding: 30px;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
            border-radius: 8px;
        }}
        h1 {{
            color: #2c3e50;
            border-bottom: 3px solid #3498db;
            padding-bottom: 10px;
        }}
        .stats {{
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 20px;
            margin: 30px 0;
        }}
        .stat-card {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 20px;
            border-radius: 8px;
            text-align: center;
        }}
        .stat-card.danger {{
            background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%);
        }}
        .stat-card h3 {{
            margin: 0;
            font-size: 36px;
        }}
        .stat-card p {{
            margin: 5px 0 0 0;
            opacity: 0.9;
        }}
        table {{
            width: 100%;
            border-collapse: collapse;
            margin: 20px 0;
        }}
        th, td {{
            padding: 12px;
            text-align: left;
            border-bottom: 1px solid #ddd;
        }}
        th {{
            background: #3498db;
            color: white;
            font-weight: 600;
        }}
        tr:hover {{
            background: #f5f5f5;
        }}
        .map-container {{
            margin: 30px 0;
            border: 2px solid #ddd;
            border-radius: 8px;
            overflow: hidden;
        }}
        .footer {{
            margin-top: 30px;
            padding-top: 20px;
            border-top: 1px solid #ddd;
            color: #7f8c8d;
            text-align: center;
        }}
    </style>
</head>
<body>
    <div class="container">
        <h1>🔍 Ilam Miner Detector - Scan Report</h1>
        
        <div class="stats">
            <div class="stat-card">
                <h3>{total_scanned}</h3>
                <p>IPs Scanned</p>
            </div>
            <div class="stat-card danger">
                <h3>{miners_found}</h3>
                <p>Miners Detected</p>
            </div>
            <div class="stat-card">
                <h3>{duration}</h3>
                <p>Scan Duration</p>
            </div>
        </div>
        
        <h2>Scan Details</h2>
        <table>
            <tr><th>Parameter</th><th>Value</th></tr>
            <tr><td>Scan Name</td><td>{scan_data.get('scan_name', 'N/A')}</td></tr>
            <tr><td>Target Range</td><td>{scan_data.get('target_range', 'N/A')}</td></tr>
            <tr><td>Started At</td><td>{scan_data.get('started_at', 'N/A')}</td></tr>
            <tr><td>Completed At</td><td>{scan_data.get('completed_at', 'N/A')}</td></tr>
            <tr><td>Status</td><td>{scan_data.get('status', 'N/A')}</td></tr>
        </table>
        
        {f'<h2>Geographic Distribution</h2><div class="map-container">{map_html}</div>' if map_html else ''}
        
        <h2>Discovered Hosts</h2>
        <table>
            <thead>
                <tr>
                    <th>IP Address</th>
                    <th>Hostname</th>
                    <th>Open Ports</th>
                    <th>Miner Status</th>
                    <th>Location</th>
                    <th>Coordinates</th>
                </tr>
            </thead>
            <tbody>
                {host_rows}
            </tbody>
        </table>
        
        <div class="footer">
            <p>Generated by Ilam Miner Detector v1.0.0 on {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}</p>
            <p><strong>Warning:</strong> This tool is for authorized security auditing only.</p>
        </div>
    </div>
</body>
</html>
"""
        
        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(html)
        
        self.logger.info(f"HTML report saved to {filepath}")
        return str(filepath)
    
    def _calculate_duration(self, start_time: str, end_time: str) -> str:
        """Calculate duration between start and end timestamps."""
        try:
            if not start_time or not end_time:
                return "N/A"
            
            start = datetime.fromisoformat(start_time)
            end = datetime.fromisoformat(end_time)
            delta = end - start
            
            hours, remainder = divmod(delta.seconds, 3600)
            minutes, seconds = divmod(remainder, 60)
            
            if hours > 0:
                return f"{hours}h {minutes}m {seconds}s"
            elif minutes > 0:
                return f"{minutes}m {seconds}s"
            else:
                return f"{seconds}s"
        except:
            return "N/A"
