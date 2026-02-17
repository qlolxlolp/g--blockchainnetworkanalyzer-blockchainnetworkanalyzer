#!/usr/bin/env python3
"""
Example usage of Ilam Miner Detector modules.
Demonstrates programmatic usage without GUI.
"""

import asyncio
import logging
from datetime import datetime

# Setup logging
logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(message)s')
logger = logging.getLogger(__name__)


def example_ip_manager():
    """Example: IP and CIDR management."""
    from ip_manager import get_ip_manager
    
    ip_manager = get_ip_manager()
    
    # Parse CIDR
    cidr = "192.168.1.0/24"
    info = ip_manager.parse_cidr(cidr)
    print(f"CIDR: {cidr}")
    print(f"  Total hosts: {info.total_hosts}")
    print(f"  Range: {info.first_ip} - {info.last_ip}")
    print(f"  Is private: {info.is_private}")
    
    # Generate IPs
    print("\nFirst 5 IPs:")
    for i, ip in enumerate(ip_manager.generate_from_cidr(cidr)):
        if i >= 5:
            break
        print(f"  {ip}")
    
    # Estimate scan time
    estimate_sec, estimate_str = ip_manager.estimate_scan_time(cidr, 3.0, 50)
    print(f"\nEstimated scan time: {estimate_str}")


def example_geolocation():
    """Example: IP geolocation lookup."""
    from geolocation import get_geolocation_service
    
    geo_service = get_geolocation_service()
    
    # Lookup IP
    test_ips = ["8.8.8.8", "1.1.1.1"]
    
    for ip in test_ips:
        result = geo_service.lookup(ip)
        if result.success:
            print(f"\n{ip}:")
            print(f"  Location: {result.city}, {result.region}, {result.country}")
            print(f"  Coordinates: {result.latitude}, {result.longitude}")
            print(f"  ISP: {result.isp}")
            print(f"  In Ilam Region: {result.is_in_ilam}")
        else:
            print(f"\n{ip}: Failed - {result.error}")


def example_database():
    """Example: Database operations."""
    from database import get_db_manager, ScanRecord, HostRecord
    
    db = get_db_manager()
    
    # Create a scan
    scan_id = db.create_scan("Example Scan", "192.168.1.0/24", "Test scan")
    print(f"Created scan: {scan_id}")
    
    # Add a host
    host = HostRecord(
        scan_id=scan_id,
        ip_address="192.168.1.1",
        is_responsive=True,
        ping_time_ms=1.5,
        open_ports='[80, 443]',
        is_miner_detected=True,
        miner_type="Stratum",
        confidence_score=85.0
    )
    host_id = db.add_host(host)
    print(f"Added host: {host_id}")
    
    # Get stats
    stats = db.get_stats()
    print(f"\nDatabase stats: {stats}")
    
    # Get miners
    miners = db.get_miner_hosts(scan_id)
    print(f"Miners found: {len(miners)}")


async def example_network_scan():
    """Example: Network scanning (async)."""
    from network_scanner import get_network_scanner
    from ip_manager import get_ip_manager
    
    scanner = get_network_scanner()
    ip_manager = get_ip_manager()
    
    # Scan a single host
    print("\nScanning localhost...")
    result = await scanner.scan_host("127.0.0.1", [80, 443, 8080])
    
    print(f"IP: {result.ip_address}")
    print(f"Responsive: {result.is_responsive}")
    print(f"Open ports: {[p.port for p in result.open_ports]}")
    print(f"Miner detected: {result.is_miner_detected}")


def example_reports():
    """Example: Report generation."""
    from reporter import get_report_generator
    from database import get_db_manager
    
    db = get_db_manager()
    reporter = get_report_generator()
    
    # Get latest scan
    scans = db.get_all_scans(limit=1)
    if scans:
        scan_id = scans[0].id
        print(f"\nGenerating reports for scan {scan_id}...")
        
        try:
            json_path = reporter.export_json(scan_id)
            print(f"JSON: {json_path}")
            
            csv_path = reporter.export_csv(scan_id)
            print(f"CSV: {csv_path}")
        except Exception as e:
            print(f"Report generation failed: {e}")
    else:
        print("No scans available for reporting")


def example_map():
    """Example: Map generation."""
    from map_generator import get_map_generator, MapMarker
    
    map_gen = get_map_generator()
    
    # Create map with markers
    m = map_gen.create_map()
    map_gen.add_ilam_boundary(m)
    
    markers = [
        MapMarker(33.0, 46.5, "Test 1", "Example miner detection", "red"),
        MapMarker(33.1, 46.6, "Test 2", "Another detection", "orange"),
    ]
    map_gen.add_markers(markers, map_obj=m)
    
    # Save
    output_path = "reports/example_map.html"
    map_gen.save_map(output_path, m)
    print(f"\nMap saved to: {output_path}")


def run_all_examples():
    """Run all examples."""
    print("=" * 60)
    print("Ilam Miner Detector - Usage Examples")
    print("=" * 60)
    
    print("\n1. IP Manager Example")
    print("-" * 40)
    example_ip_manager()
    
    print("\n2. Geolocation Example")
    print("-" * 40)
    example_geolocation()
    
    print("\n3. Database Example")
    print("-" * 40)
    example_database()
    
    print("\n4. Network Scan Example")
    print("-" * 40)
    asyncio.run(example_network_scan())
    
    print("\n5. Reports Example")
    print("-" * 40)
    example_reports()
    
    print("\n6. Map Generation Example")
    print("-" * 40)
    example_map()
    
    print("\n" + "=" * 60)
    print("Examples completed!")
    print("=" * 60)


if __name__ == "__main__":
    run_all_examples()
