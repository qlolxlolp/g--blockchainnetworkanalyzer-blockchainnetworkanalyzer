#!/usr/bin/env python3
"""
Ilam Miner Detector - Main Entry Point

A security tool for detecting cryptocurrency mining operations in Ilam province, Iran.
Features real network scanning, geolocation, and interactive map visualization.

Usage:
    python main.py                          # Launch GUI
    python main.py --cli --cidr 192.168.1.0/24  # CLI mode
    python main.py --help                   # Show help
"""

import sys
import os
import argparse
import logging
import asyncio
from pathlib import Path
from datetime import datetime

# Add package to path
sys.path.insert(0, str(Path(__file__).parent))

from config_manager import get_config_manager
from database import get_db_manager
from network_scanner import get_network_scanner
from ip_manager import get_ip_manager
from geolocation import get_geolocation_service
from reporter import get_report_generator
from map_generator import get_map_generator


def setup_logging(log_file: str = None, log_level: str = "INFO"):
    """Setup logging configuration."""
    handlers = [logging.StreamHandler()]
    
    if log_file:
        Path(log_file).parent.mkdir(parents=True, exist_ok=True)
        handlers.append(logging.FileHandler(log_file))
    
    logging.basicConfig(
        level=getattr(logging, log_level.upper()),
        format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
        handlers=handlers
    )


def cli_scan(args):
    """Run scan in CLI mode."""
    config = get_config_manager().get()
    db = get_db_manager()
    scanner = get_network_scanner()
    ip_manager = get_ip_manager()
    
    logger = logging.getLogger(__name__)
    
    # Validate CIDR
    try:
        info = ip_manager.parse_cidr(args.cidr)
        logger.info(f"Scanning {info.total_hosts} hosts in {args.cidr}")
    except ValueError as e:
        logger.error(f"Invalid CIDR: {e}")
        return 1
    
    # Create scan record
    scan_name = args.name or f"CLI Scan {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}"
    scan_id = db.create_scan(scan_name, args.cidr, args.notes or "")
    logger.info(f"Created scan record: {scan_id}")
    
    # Update status
    db.update_scan_status(scan_id, "running")
    db.update_scan_stats(scan_id, total_hosts=info.total_hosts)
    
    # Run scan
    ports = config.miner_ports.all_ports
    if args.ports:
        ports = [int(p) for p in args.ports.split(',') if p.strip().isdigit()]
    
    logger.info(f"Scanning ports: {ports}")
    
    async def run_scan():
        results = await scanner.scan_range(
            ip_manager.generate_from_cidr(args.cidr),
            ports,
            progress_callback=lambda c, t, s: logger.info(f"Progress: {c}/{t} - {s}")
        )
        return results
    
    loop = asyncio.new_event_loop()
    asyncio.set_event_loop(loop)
    
    try:
        results = loop.run_until_complete(run_scan())
    finally:
        loop.close()
    
    # Process results
    import json
    responsive = 0
    miners = 0
    
    for result in results:
        if result.is_responsive:
            responsive += 1
        if result.is_miner_detected:
            miners += 1
            logger.warning(f"MINER DETECTED: {result.ip_address} ({result.miner_type}) - {result.confidence_score:.1f}% confidence")
        
        # Save to DB
        host_record = type('HostRecord', (), {
            'scan_id': scan_id,
            'ip_address': result.ip_address,
            'is_responsive': result.is_responsive,
            'ping_time_ms': result.ping_time_ms,
            'open_ports': json.dumps([p.port for p in result.open_ports]),
            'banner_info': json.dumps({p.port: p.banner for p in result.open_ports}),
            'is_miner_detected': result.is_miner_detected,
            'miner_type': result.miner_type,
            'confidence_score': result.confidence_score
        })
        db.add_host(host_record)
    
    # Update final stats
    db.update_scan_stats(scan_id, responsive_hosts=responsive, miners_detected=miners)
    db.update_scan_status(scan_id, "completed")
    
    logger.info(f"Scan complete: {responsive} responsive, {miners} miners detected")
    
    # Generate reports
    if args.export:
        reporter = get_report_generator()
        reports = reporter.generate_all_reports(scan_id)
        for fmt, path in reports.items():
            logger.info(f"Generated {fmt} report: {path}")
    
    # Generate map
    if args.map and miners > 0:
        try:
            geo_service = get_geolocation_service()
            map_gen = get_map_generator()
            
            miner_hosts = db.get_miner_hosts(scan_id)
            map_data = []
            
            for miner in miner_hosts:
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
            
            if map_data:
                timestamp = datetime.now().strftime(config.reporting.timestamp_format)
                map_path = Path(config.reporting.reports_dir) / f"map_cli_{scan_id}_{timestamp}.html"
                map_gen.create_summary_map(map_data, str(map_path))
                logger.info(f"Map saved to: {map_path}")
        except Exception as e:
            logger.error(f"Map generation failed: {e}")
    
    return 0 if miners == 0 else 1


def cli_geolocate(args):
    """Geolocate IP addresses."""
    geo_service = get_geolocation_service()
    logger = logging.getLogger(__name__)
    
    ips = args.ips.split(',') if args.ips else []
    if args.file:
        with open(args.file) as f:
            ips.extend(line.strip() for line in f if line.strip())
    
    logger.info(f"Geolocating {len(ips)} IP addresses...")
    
    for ip in ips:
        result = geo_service.lookup(ip)
        if result.success:
            print(f"{ip}:")
            print(f"  Location: {result.city}, {result.region}, {result.country}")
            print(f"  Coordinates: {result.latitude}, {result.longitude}")
            print(f"  ISP: {result.isp}")
            print(f"  In Ilam Region: {result.is_in_ilam}")
        else:
            print(f"{ip}: Failed - {result.error}")
    
    return 0


def cli_stats(args):
    """Show database statistics."""
    db = get_db_manager()
    stats = db.get_stats()
    
    print("Database Statistics:")
    print(f"  Total Scans: {stats['total_scans']}")
    print(f"  Total Hosts: {stats['total_hosts']}")
    print(f"  Total Miners: {stats['total_miners']}")
    print(f"  Geolocation Cache: {stats['geolocation_cache_entries']} entries")
    
    return 0


def main():
    """Main entry point."""
    parser = argparse.ArgumentParser(
        description="Ilam Miner Detector - Cryptocurrency mining detection tool",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  %(prog)s                                    # Launch GUI
  %(prog)s --cli --cidr 192.168.1.0/24        # Scan network
  %(prog)s --cli --cidr 10.0.0.0/24 --export  # Scan and export reports
  %(prog)s --geo --ips 8.8.8.8,1.1.1.1        # Geolocate IPs
  %(prog)s --stats                            # Show database stats

Legal Notice:
  This tool is for authorized security auditing only. Users must have
  explicit permission to scan target networks.
        """
    )
    
    parser.add_argument('--version', action='version', version='%(prog)s 1.0.0')
    parser.add_argument('--config', default='config/config.json', help='Config file path')
    parser.add_argument('--log-level', default='INFO', choices=['DEBUG', 'INFO', 'WARNING', 'ERROR'])
    parser.add_argument('--log-file', help='Log file path')
    
    subparsers = parser.add_subparsers(dest='command')
    
    # Scan command
    scan_parser = subparsers.add_parser('scan', help='Run network scan')
    scan_parser.add_argument('--cidr', required=True, help='CIDR range to scan')
    scan_parser.add_argument('--name', help='Scan name')
    scan_parser.add_argument('--notes', help='Scan notes')
    scan_parser.add_argument('--ports', help='Comma-separated ports (default: mining ports)')
    scan_parser.add_argument('--export', action='store_true', help='Export reports')
    scan_parser.add_argument('--map', action='store_true', help='Generate map')
    
    # Geolocate command
    geo_parser = subparsers.add_parser('geolocate', help='Geolocate IP addresses')
    geo_parser.add_argument('--ips', help='Comma-separated IP addresses')
    geo_parser.add_argument('--file', help='File with IP addresses (one per line)')
    
    # Stats command
    stats_parser = subparsers.add_parser('stats', help='Show database statistics')
    
    # GUI flag
    parser.add_argument('--gui', action='store_true', help='Launch GUI (default if no command)')
    
    args = parser.parse_args()
    
    # Setup logging
    setup_logging(args.log_file, args.log_level)
    
    # Load config
    get_config_manager(args.config).load()
    
    # Route to appropriate handler
    if args.command == 'scan':
        return cli_scan(args)
    elif args.command == 'geolocate':
        return cli_geolocate(args)
    elif args.command == 'stats':
        return cli_stats(args)
    else:
        # Launch GUI by default
        try:
            from gui.main_window import main as gui_main
            gui_main()
            return 0
        except ImportError as e:
            print(f"GUI dependencies not available: {e}")
            print("Install with: pip install PyQt5")
            return 1


if __name__ == "__main__":
    sys.exit(main())
