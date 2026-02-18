#!/usr/bin/env python3
"""
Iranian Network Miner Detection System - Extended Main Entry Point
Comprehensive security tool for detecting cryptocurrency mining across Iranian networks.

Features:
- Full Iranian geographic coverage (31 provinces)
- ISP IP range management
- Advanced network analysis
- VPN/Proxy detection
- YAML-based detection rules
- Scheduled scanning
- Analytics dashboard
- Professional reporting (PDF, Excel, HTML, JSON, CSV)

Usage:
    python main_extended.py                      # Launch GUI
    python main_extended.py --cli --cidr ...   # CLI mode
    python main_extended.py --help              # Show help
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

# Import all modules
from config_manager import get_config_manager
from database import get_db_manager
from network_scanner import get_network_scanner
from ip_manager import get_ip_manager
from geolocation import get_geolocation_service
from reporter import get_report_generator
from enhanced_reporter import get_enhanced_report_generator
from map_generator import get_map_generator
from vpn_detector import get_vpn_detector
from detection_rules import get_detection_rules_engine
from scheduler import get_scheduler
from analytics import get_analytics_service
from iran_geography import (
    get_all_province_names, 
    get_cities_in_province,
    get_province_by_coordinates
)
from iran_isps import (
    get_all_isps,
    identify_isp,
    get_isp_ranges
)


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


def print_banner():
    """Print application banner."""
    banner = """
╔══════════════════════════════════════════════════════════════════╗
║                                                                  ║
║      🇮🇷 Iranian Network Miner Detection System v2.0 🇮🇷           ║
║                                                                  ║
║      Comprehensive Security Auditing Tool for Iran                 ║
║                                                                  ║
║      • Full Geographic Coverage (31 Provinces)                     ║
║      • ISP Range Management                                        ║
║      • Advanced Network Analysis                                   ║
║      • VPN/Proxy Detection                                         ║
║      • YAML-Based Detection Rules                                  ║
║      • Scheduled Scanning                                         ║
║      • Analytics Dashboard                                         ║
║      • Professional Reports (PDF, Excel, HTML)                    ║
║                                                                  ║
║      For AUTHORIZED security auditing only                        ║
║                                                                  ║
╚══════════════════════════════════════════════════════════════════╝
"""
    print(banner)


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
    vpn_detected = 0
    
    # Get VPN detector
    vpn_detector = get_vpn_detector()
    
    # Get detection rules engine
    rules_engine = get_detection_rules_engine()
    
    for result in results:
        if result.is_responsive:
            responsive += 1
        
        # Apply detection rules
        rule_matches = rules_engine.evaluate(result)
        overall_confidence = rules_engine.calculate_overall_confidence(rule_matches)
        
        # Update result with rule-based confidence
        result.confidence_score = max(result.confidence_score, overall_confidence)
        
        if result.is_miner_detected or overall_confidence >= 0.5:
            miners += 1
            logger.warning(f"MINER DETECTED: {result.ip_address} ({result.miner_type}) - {result.confidence_score:.1f}% confidence")
        
        # Check for VPN/Proxy
        vpn_result = vpn_detector.check_ip(result.ip_address)
        if vpn_result.is_vpn or vpn_result.is_proxy:
            vpn_detected += 1
            logger.info(f"VPN/Proxy detected: {result.ip_address}")
        
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
    
    logger.info(f"Scan complete: {responsive} responsive, {miners} miners detected, {vpn_detected} VPN/proxy")
    
    # Generate reports
    if args.export:
        reporter = get_report_generator()
        enhanced_reporter = get_enhanced_report_generator()
        reports = {}
        
        # Generate basic reports
        for fmt in config.reporting.export_formats:
            try:
                if fmt == 'json':
                    reports['json'] = reporter.export_json(scan_id)
                elif fmt == 'csv':
                    reports['csv'] = reporter.export_csv(scan_id)
                elif fmt == 'html':
                    reports['html'] = reporter.export_html(scan_id)
                elif fmt == 'pdf':
                    reports['pdf'] = enhanced_reporter.export_pdf(scan_id)
                elif fmt == 'excel':
                    reports['excel'] = enhanced_reporter.export_excel(scan_id)
            except Exception as e:
                logger.error(f"Failed to generate {fmt} report: {e}")
        
        for fmt, path in reports.items():
            logger.info(f"Generated {fmt} report: {path}")
    
    # Generate analytics
    if args.analytics:
        analytics_service = get_analytics_service()
        analytics_service.clear_cache()
        
        # Generate charts
        if config.analytics.generate_charts:
            matplotlib_charts = analytics_service.generate_matplotlib_charts()
            plotly_charts = analytics_service.generate_plotly_charts()
            logger.info(f"Generated {len(matplotlib_charts)} matplotlib charts")
            logger.info(f"Generated {len(plotly_charts)} plotly charts")
        
        # Export analytics JSON
        analytics_path = analytics_service.export_analytics_json()
        logger.info(f"Exported analytics to {analytics_path}")
    
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
                    # Determine province
                    province = get_province_by_coordinates(geo.latitude, geo.longitude)
                    province_name = province.name if province else "Unknown"
                    
                    # Identify ISP
                    isp = identify_isp(miner.ip_address)
                    isp_name = isp.name if isp else geo.isp
                    
                    # Check VPN/Proxy
                    vpn_result = vpn_detector.check_ip(miner.ip_address)
                    
                    map_data.append({
                        'ip_address': miner.ip_address,
                        'latitude': geo.latitude,
                        'longitude': geo.longitude,
                        'confidence_score': miner.confidence_score,
                        'miner_type': miner.miner_type,
                        'city': geo.city,
                        'region': geo.region,
                        'province': province_name,
                        'country': geo.country,
                        'isp': isp_name,
                        'open_ports': json.loads(miner.open_ports) if miner.open_ports else [],
                        'is_vpn': vpn_result.is_vpn,
                        'is_proxy': vpn_result.is_proxy
                    })
            
            if map_data:
                timestamp = datetime.now().strftime(config.reporting.timestamp_format)
                map_path = Path(config.reporting.reports_dir) / f"map_cli_{scan_id}_{timestamp}.html"
                map_gen.create_summary_map(map_data, str(map_path))
                logger.info(f"Map saved to: {map_path}")
        except Exception as e:
            logger.error(f"Map generation failed: {e}")
    
    return 0 if miners == 0 else 1


def cli_list_provinces(args):
    """List all Iranian provinces."""
    provinces = get_all_province_names()
    
    print("\nIranian Provinces (31):")
    print("=" * 50)
    for i, province in enumerate(provinces, 1):
        print(f"{i:2d}. {province}")
    
    if args.detailed:
        print("\nDetailed Information:")
        print("=" * 50)
        from iran_geography import get_province_by_name
        for province_name in provinces:
            province = get_province_by_name(province_name)
            if province:
                print(f"\n{province.name} ({province.name_persian})")
                print(f"  Code: {province.code}")
                print(f"  Coordinates: {province.latitude:.4f}, {province.longitude:.4f}")
                print(f"  Cities: {len(province.cities)}")
                if args.show_cities:
                    for city in province.cities:
                        print(f"    - {city.name}")
    
    return 0


def cli_list_isps(args):
    """List all Iranian ISPs."""
    isps = get_all_isps()
    
    print("\nIranian ISPs:")
    print("=" * 70)
    print(f"{'ISP Name':<40} {'ASN':<15} {'Risk Score':<10}")
    print("-" * 70)
    
    for isp in isps:
        total_ips = sum(net.num_addresses for net in isp.networks)
        print(f"{isp.name:<40} {isp.asn:<15} {isp.risk_score:<10.2f}")
    
    if args.detailed:
        print("\nDetailed Information:")
        print("=" * 70)
        for isp in isps:
            total_ips = sum(net.num_addresses for net in isp.networks)
            print(f"\n{isp.name}")
            print(f"  Persian: {isp.name_persian}")
            print(f"  ASN: {isp.asn}")
            print(f"  Risk Score: {isp.risk_score:.2f}")
            print(f"  IP Ranges: {len(isp.ip_ranges)}")
            print(f"  Total IPs: {total_ips:,}")
            if args.show_ranges:
                print(f"  Sample Ranges:")
                for cidr in isp.ip_ranges[:5]:
                    print(f"    - {cidr}")
    
    return 0


def cli_stats(args):
    """Show database statistics."""
    db = get_db_manager()
    stats = db.get_stats()
    
    print("\nDatabase Statistics:")
    print("=" * 50)
    print(f"Total Scans: {stats['total_scans']}")
    print(f"Total Hosts: {stats['total_hosts']}")
    print(f"Total Miners: {stats['total_miners']}")
    print(f"Geolocation Cache: {stats['geolocation_cache_entries']} entries")
    
    # Show analytics if available
    try:
        analytics_service = get_analytics_service()
        analytics = analytics_service.get_analytics_data()
        
        print("\nAnalytics Summary:")
        print("=" * 50)
        print(f"Miners by Province: {len(analytics.miners_by_province)} provinces")
        print(f"Miners by ISP: {len(analytics.miners_by_isp)} ISPs")
        print(f"Miners by Type: {len(analytics.miners_by_type)} types")
        
        if analytics.top_vulnerable_regions:
            print(f"\nTop Vulnerable Region: {analytics.top_vulnerable_regions[0][0]} ({analytics.top_vulnerable_regions[0][1]} miners)")
        
        if analytics.high_risk_isps:
            print(f"Top Risk ISP: {analytics.high_risk_isps[0][0]} ({analytics.high_risk_isps[0][1]} miners)")
    except Exception as e:
        logger.error(f"Failed to load analytics: {e}")
    
    return 0


def main():
    """Main entry point."""
    parser = argparse.ArgumentParser(
        description="Iranian Network Miner Detection System - Comprehensive security auditing tool",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  %(prog)s                                    # Launch GUI
  %(prog)s scan --cidr 192.168.1.0/24        # Scan network
  %(prog)s scan --cidr 10.0.0.0/24 --export --map --analytics  # Full scan with reports
  %(prog)s provinces                          # List provinces
  %(prog)s isps                               # List ISPs
  %(prog)s stats                              # Show statistics

Legal Notice:
  This tool is for authorized security auditing only. Users must have
  explicit permission to scan target networks.
        """
    )
    
    parser.add_argument('--version', action='version', version='%(prog)s 2.0.0')
    parser.add_argument('--config', default='config/config_extended.json', 
                       help='Config file path (default: config/config_extended.json)')
    parser.add_argument('--log-level', default='INFO', 
                       choices=['DEBUG', 'INFO', 'WARNING', 'ERROR'])
    parser.add_argument('--log-file', help='Log file path')
    parser.add_argument('--no-banner', action='store_true', help='Don't display banner')
    
    subparsers = parser.add_subparsers(dest='command', help='Available commands')
    
    # Scan command
    scan_parser = subparsers.add_parser('scan', help='Run network scan')
    scan_parser.add_argument('--cidr', required=True, help='CIDR range to scan')
    scan_parser.add_argument('--name', help='Scan name')
    scan_parser.add_argument('--notes', help='Scan notes')
    scan_parser.add_argument('--ports', help='Comma-separated ports (default: mining ports)')
    scan_parser.add_argument('--export', action='store_true', help='Export all report formats')
    scan_parser.add_argument('--map', action='store_true', help='Generate interactive map')
    scan_parser.add_argument('--analytics', action='store_true', help='Generate analytics and charts')
    
    # Provinces command
    provinces_parser = subparsers.add_parser('provinces', help='List Iranian provinces')
    provinces_parser.add_argument('--detailed', action='store_true', help='Show detailed information')
    provinces_parser.add_argument('--show-cities', action='store_true', help='Show cities in each province')
    
    # ISPs command
    isps_parser = subparsers.add_parser('isps', help='List Iranian ISPs')
    isps_parser.add_argument('--detailed', action='store_true', help='Show detailed information')
    isps_parser.add_argument('--show-ranges', action='store_true', help='Show IP ranges')
    
    # Stats command
    stats_parser = subparsers.add_parser('stats', help='Show database statistics')
    
    # GUI flag
    parser.add_argument('--gui', action='store_true', help='Launch GUI (default if no command)')
    
    args = parser.parse_args()
    
    # Print banner
    if not args.no_banner:
        print_banner()
    
    # Setup logging
    setup_logging(args.log_file, args.log_level)
    
    # Load config
    try:
        get_config_manager(args.config).load()
    except Exception as e:
        print(f"Warning: Failed to load config: {e}")
    
    # Route to appropriate handler
    if args.command == 'scan':
        return cli_scan(args)
    elif args.command == 'provinces':
        return cli_list_provinces(args)
    elif args.command == 'isps':
        return cli_list_isps(args)
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
