"""
Report generation module for Ilam Miner Detector.
Creates JSON, HTML, and CSV reports from scan results.
"""

import json
import csv
import logging
from datetime import datetime
from pathlib import Path
from typing import List, Dict, Any, Optional
from dataclasses import asdict

from .database import ScanRecord, HostRecord, get_db_manager
from .map_generator import get_map_generator
from .config_manager import get_config_manager

logger = logging.getLogger(__name__)


class ReportGenerator:
    """
    Generates various report formats from scan data.
    """
    
    def __init__(self):
        self.config = get_config_manager().get()
        self.db = get_db_manager()
        self.reports_dir = Path(self.config.reporting.reports_dir)
        self.reports_dir.mkdir(parents=True, exist_ok=True)
    
    def generate_timestamp(self) -> str:
        """Generate timestamp string for filenames."""
        return datetime.now().strftime(self.config.reporting.timestamp_format)
    
    def export_json(self, scan_id: int, output_path: Optional[str] = None) -> str:
        """
        Export scan results to JSON.
        
        Args:
            scan_id: Scan ID to export
            output_path: Custom output path (optional)
            
        Returns:
            Path to exported file
        """
        scan = self.db.get_scan(scan_id)
        if not scan:
            raise ValueError(f"Scan {scan_id} not found")
        
        hosts = self.db.get_hosts_by_scan(scan_id)
        
        # Build report structure
        report = {
            "metadata": {
                "report_type": "Ilam Miner Detection Report",
                "generated_at": datetime.now().isoformat(),
                "version": "1.0.0"
            },
            "scan": scan.to_dict(),
            "hosts": [host.to_dict() for host in hosts],
            "summary": {
                "total_hosts": scan.total_hosts,
                "responsive_hosts": scan.responsive_hosts,
                "miners_detected": scan.miners_detected,
                "detection_rate": (scan.miners_detected / scan.total_hosts * 100) 
                                  if scan.total_hosts > 0 else 0
            }
        }
        
        # Determine output path
        if output_path is None:
            timestamp = self.generate_timestamp()
            output_path = self.reports_dir / f"scan_{scan_id}_{timestamp}.json"
        
        # Write JSON
        with open(output_path, 'w') as f:
            json.dump(report, f, indent=2, default=str)
        
        logger.info(f"JSON report saved to {output_path}")
        return str(output_path)
    
    def export_csv(self, scan_id: int, output_path: Optional[str] = None) -> str:
        """
        Export scan results to CSV.
        
        Args:
            scan_id: Scan ID to export
            output_path: Custom output path (optional)
            
        Returns:
            Path to exported file
        """
        scan = self.db.get_scan(scan_id)
        if not scan:
            raise ValueError(f"Scan {scan_id} not found")
        
        hosts = self.db.get_hosts_by_scan(scan_id)
        
        # Determine output path
        if output_path is None:
            timestamp = self.generate_timestamp()
            output_path = self.reports_dir / f"scan_{scan_id}_{timestamp}.csv"
        
        # Write CSV
        fieldnames = [
            'ip_address', 'is_responsive', 'ping_time_ms', 'open_ports',
            'is_miner_detected', 'miner_type', 'confidence_score', 'timestamp'
        ]
        
        with open(output_path, 'w', newline='') as f:
            writer = csv.DictWriter(f, fieldnames=fieldnames)
            writer.writeheader()
            
            for host in hosts:
                row = {
                    'ip_address': host.ip_address,
                    'is_responsive': host.is_responsive,
                    'ping_time_ms': host.ping_time_ms or '',
                    'open_ports': host.open_ports,
                    'is_miner_detected': host.is_miner_detected,
                    'miner_type': host.miner_type,
                    'confidence_score': host.confidence_score,
                    'timestamp': host.timestamp.isoformat() if host.timestamp else ''
                }
                writer.writerow(row)
        
        logger.info(f"CSV report saved to {output_path}")
        return str(output_path)
    
    def export_html(self, scan_id: int, output_path: Optional[str] = None,
                   include_map: bool = True) -> str:
        """
        Export scan results to HTML report.
        
        Args:
            scan_id: Scan ID to export
            output_path: Custom output path (optional)
            include_map: Whether to include interactive map
            
        Returns:
            Path to exported file
        """
        scan = self.db.get_scan(scan_id)
        if not scan:
            raise ValueError(f"Scan {scan_id} not found")
        
        hosts = self.db.get_hosts_by_scan(scan_id)
        miners = [h for h in hosts if h.is_miner_detected]
        
        # Determine output path
        if output_path is None:
            timestamp = self.generate_timestamp()
            output_path = self.reports_dir / f"scan_{scan_id}_{timestamp}.html"
        
        # Parse host data for display
        host_data = []
        for host in hosts:
            try:
                open_ports = json.loads(host.open_ports) if host.open_ports else []
            except json.JSONDecodeError:
                open_ports = []
            
            try:
                banner_info = json.loads(host.banner_info) if host.banner_info else {}
            except json.JSONDecodeError:
                banner_info = {}
            
            host_data.append({
                'ip': host.ip_address,
                'responsive': 'Yes' if host.is_responsive else 'No',
                'ping': f"{host.ping_time_ms:.2f}ms" if host.ping_time_ms else 'N/A',
                'ports': ', '.join(str(p) for p in open_ports[:5]),
                'miner': 'Yes' if host.is_miner_detected else 'No',
                'type': host.miner_type,
                'confidence': f"{host.confidence_score:.1f}%"
            })
        
        # Build HTML
        html_content = self._build_html_report(scan, host_data, miners, include_map)
        
        # Write HTML
        with open(output_path, 'w') as f:
            f.write(html_content)
        
        # Generate map if requested
        if include_map and miners:
            try:
                self._generate_map_for_report(miners, output_path)
            except Exception as e:
                logger.warning(f"Failed to generate map: {e}")
        
        logger.info(f"HTML report saved to {output_path}")
        return str(output_path)
    
    def generate_all_reports(self, scan_id: int) -> Dict[str, str]:
        """
        Generate all report formats for a scan.
        
        Args:
            scan_id: Scan ID
            
        Returns:
            Dictionary mapping format to file path
        """
        timestamp = self.generate_timestamp()
        reports = {}
        
        for fmt in self.config.reporting.export_formats:
            try:
                if fmt == 'json':
                    path = self.reports_dir / f"scan_{scan_id}_{timestamp}.json"
                    reports['json'] = self.export_json(scan_id, str(path))
                elif fmt == 'csv':
                    path = self.reports_dir / f"scan_{scan_id}_{timestamp}.csv"
                    reports['csv'] = self.export_csv(scan_id, str(path))
                elif fmt == 'html':
                    path = self.reports_dir / f"scan_{scan_id}_{timestamp}.html"
                    reports['html'] = self.export_html(
                        scan_id, str(path),
                        include_map=self.config.reporting.include_map
                    )
            except Exception as e:
                logger.error(f"Failed to generate {fmt} report: {e}")
                reports[fmt] = f"Error: {str(e)}"
        
        return reports
    
    def _build_html_report(self, scan: ScanRecord, host_data: List[Dict],
                          miners: List[HostRecord], include_map: bool) -> str:
        """Build HTML report content."""
        
        # Miner rows
        miner_rows = ""
        for host in miners:
            try:
                ports = json.loads(host.open_ports) if host.open_ports else []
            except json.JSONDecodeError:
                ports = []
            
            miner_rows += f"""
                <tr class="miner-row">
                    <td>{host.ip_address}</td>
                    <td>{host.miner_type}</td>
                    <td><span class="confidence high">{host.confidence_score:.1f}%</span></td>
                    <td>{', '.join(str(p) for p in ports[:3])}</td>
                </tr>
            """
        
        # All hosts rows
        host_rows = ""
        for data in host_data:
            row_class = "miner-row" if data['miner'] == 'Yes' else ""
            host_rows += f"""
                <tr class="{row_class}">
                    <td>{data['ip']}</td>
                    <td>{data['responsive']}</td>
                    <td>{data['ping']}</td>
                    <td>{data['ports']}</td>
                    <td>{data['miner']}</td>
                    <td>{data['type']}</td>
                    <td><span class="confidence {'high' if float(data['confidence'].rstrip('%')) >= 50 else 'low'}">{data['confidence']}</span></td>
                </tr>
            """
        
        html = f"""<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Ilam Miner Detection Report - {scan.scan_name}</title>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            margin: 0;
            padding: 20px;
            background-color: #f5f5f5;
        }}
        .container {{
            max-width: 1200px;
            margin: 0 auto;
            background: white;
            padding: 30px;
            border-radius: 8px;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
        }}
        h1 {{
            color: #d32f2f;
            border-bottom: 3px solid #d32f2f;
            padding-bottom: 10px;
        }}
        h2 {{
            color: #333;
            margin-top: 30px;
        }}
        .summary {{
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 20px;
            margin: 20px 0;
        }}
        .summary-card {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 20px;
            border-radius: 8px;
            text-align: center;
        }}
        .summary-card.alert {{
            background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%);
        }}
        .summary-card h3 {{
            margin: 0 0 10px 0;
            font-size: 14px;
            opacity: 0.9;
        }}
        .summary-card .value {{
            font-size: 32px;
            font-weight: bold;
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
            background-color: #f8f9fa;
            font-weight: 600;
            color: #333;
        }}
        tr:hover {{
            background-color: #f8f9fa;
        }}
        .miner-row {{
            background-color: #ffebee !important;
        }}
        .miner-row:hover {{
            background-color: #ffcdd2 !important;
        }}
        .confidence {{
            padding: 4px 8px;
            border-radius: 4px;
            font-weight: bold;
        }}
        .confidence.high {{
            background-color: #ffebee;
            color: #c62828;
        }}
        .confidence.medium {{
            background-color: #fff3e0;
            color: #ef6c00;
        }}
        .confidence.low {{
            background-color: #e3f2fd;
            color: #1565c0;
        }}
        .metadata {{
            background: #f8f9fa;
            padding: 15px;
            border-radius: 4px;
            font-size: 14px;
            color: #666;
        }}
        .alert-box {{
            background-color: #ffebee;
            border-left: 4px solid #d32f2f;
            padding: 15px;
            margin: 20px 0;
        }}
        .alert-box h3 {{
            margin: 0 0 10px 0;
            color: #d32f2f;
        }}
    </style>
</head>
<body>
    <div class="container">
        <h1>🚨 Ilam Miner Detection Report</h1>
        
        <div class="metadata">
            <strong>Scan Name:</strong> {scan.scan_name}<br>
            <strong>CIDR Range:</strong> {scan.cidr_range}<br>
            <strong>Start Time:</strong> {scan.start_time.strftime('%Y-%m-%d %H:%M:%S') if scan.start_time else 'N/A'}<br>
            <strong>Status:</strong> {scan.status.title()}<br>
            <strong>Generated:</strong> {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}
        </div>
        
        <div class="summary">
            <div class="summary-card">
                <h3>Total Hosts</h3>
                <div class="value">{scan.total_hosts}</div>
            </div>
            <div class="summary-card">
                <h3>Responsive</h3>
                <div class="value">{scan.responsive_hosts}</div>
            </div>
            <div class="summary-card alert">
                <h3>Miners Detected</h3>
                <div class="value">{scan.miners_detected}</div>
            </div>
            <div class="summary-card">
                <h3>Detection Rate</h3>
                <div class="value">{(scan.miners_detected / scan.total_hosts * 100):.1f}%</div>
            </div>
        </div>
        
        {f'''
        <div class="alert-box">
            <h3>⚠️ High Priority Alerts</h3>
            <p>{len(miners)} potential cryptocurrency mining operations detected in the scanned range.</p>
        </div>
        ''' if miners else ''}
        
        <h2>Detected Miners</h2>
        <table>
            <thead>
                <tr>
                    <th>IP Address</th>
                    <th>Type</th>
                    <th>Confidence</th>
                    <th>Open Ports</th>
                </tr>
            </thead>
            <tbody>
                {miner_rows if miner_rows else '<tr><td colspan="4" style="text-align:center;color:#999;">No miners detected</td></tr>'}
            </tbody>
        </table>
        
        <h2>All Hosts</h2>
        <table>
            <thead>
                <tr>
                    <th>IP Address</th>
                    <th>Responsive</th>
                    <th>Ping</th>
                    <th>Open Ports</th>
                    <th>Miner</th>
                    <th>Type</th>
                    <th>Confidence</th>
                </tr>
            </thead>
            <tbody>
                {host_rows if host_rows else '<tr><td colspan="7" style="text-align:center;color:#999;">No hosts scanned</td></tr>'}
            </tbody>
        </table>
        
        {f'<h2>Geographic Map</h2><p>An interactive map has been generated showing detection locations.</p>' if include_map and miners else ''}
        
        <div style="margin-top: 40px; padding-top: 20px; border-top: 1px solid #ddd; color: #999; font-size: 12px;">
            <p>Generated by Ilam Miner Detector v1.0.0</p>
            <p>This report is for authorized security auditing purposes only.</p>
        </div>
    </div>
</body>
</html>"""
        
        return html
    
    def _generate_map_for_report(self, miners: List[HostRecord], report_path: str) -> str:
        """Generate map for HTML report."""
        # Get geolocation data for miners
        geo_service = __import__('ilam_miner_detector.geolocation', fromlist=['get_geolocation_service']).get_geolocation_service()
        
        map_data = []
        for miner in miners:
            try:
                geo = geo_service.lookup(miner.ip_address)
                if geo.success:
                    map_data.append({
                        'ip_address': miner.ip_address,
                        'latitude': geo.latitude,
                        'longitude': geo.longitude,
                        'confidence_score': miner.confidence_score,
                        'miner_type': miner.miner_type,
                        'city': geo.city,
                        'region': geo.region,
                        'country': geo.country,
                        'isp': geo.isp,
                        'open_ports': json.loads(miner.open_ports) if miner.open_ports else []
                    })
            except Exception as e:
                logger.warning(f"Failed to geolocate {miner.ip_address}: {e}")
        
        if map_data:
            map_gen = get_map_generator()
            map_path = str(Path(report_path).with_suffix('.map.html'))
            return map_gen.create_summary_map(map_data, map_path)
        
        return ""


def get_report_generator() -> ReportGenerator:
    """Get report generator instance."""
    return ReportGenerator()
