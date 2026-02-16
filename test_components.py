#!/usr/bin/env python3
"""
Test script for Ilam Miner Detector components.
Tests functionality without requiring GUI or external dependencies.
"""

import sys
import os
sys.path.insert(0, os.path.dirname(__file__))

def test_config_manager():
    """Test configuration management."""
    print("Testing ConfigManager...")
    from ilam_miner_detector.config_manager import ConfigManager
    
    config = ConfigManager()
    
    assert config.scan.timeout_ms == 3000
    assert config.geolocation.primary_provider == "ip-api"
    assert len(config.miner_ports.all_ports()) > 0
    
    print("  ✓ ConfigManager working")

def test_ip_manager():
    """Test IP address management."""
    print("Testing IPManager...")
    from ilam_miner_detector.ip_manager import IPManager
    
    # Test CIDR parsing
    ips = list(IPManager.parse_cidr("192.168.1.0/30"))
    assert len(ips) == 2  # .1 and .2 (excluding network and broadcast)
    
    # Test range parsing
    ips = list(IPManager.parse_range("10.0.0.1", "10.0.0.5"))
    assert len(ips) == 5
    
    # Test validation
    assert IPManager.is_valid("192.168.1.1")
    assert not IPManager.is_valid("999.999.999.999")
    
    # Test private IP detection
    assert IPManager.is_private("192.168.1.1")
    assert IPManager.is_private("10.0.0.1")
    assert not IPManager.is_private("8.8.8.8")
    
    # Test smart parsing
    ips = list(IPManager.parse_input("192.168.1.0/30"))
    assert len(ips) == 2
    
    print("  ✓ IPManager working")

def test_database():
    """Test database operations."""
    print("Testing Database...")
    from ilam_miner_detector.database import Database
    import tempfile
    
    # Use temp database
    temp_db = tempfile.NamedTemporaryFile(suffix='.db', delete=False)
    temp_db.close()
    
    db = Database(temp_db.name)
    
    # Create scan
    scan_id = db.create_scan("Test Scan", "192.168.1.0/24", {"test": "config"})
    assert scan_id > 0
    
    # Add host
    host_id = db.add_host(
        scan_id=scan_id,
        ip_address="192.168.1.100",
        is_reachable=True,
        open_ports=[3333, 4444],
        is_miner=True,
        miner_type="stratum"
    )
    assert host_id > 0
    
    # Retrieve scan
    scan = db.get_scan(scan_id)
    assert scan is not None
    assert scan['scan_name'] == "Test Scan"
    
    # Retrieve hosts
    hosts = db.get_scan_hosts(scan_id)
    assert len(hosts) == 1
    assert hosts[0]['ip_address'] == "192.168.1.100"
    
    # Cache geolocation
    db.cache_geolocation(
        "192.168.1.100",
        {
            'country': 'Iran',
            'countryCode': 'IR',
            'regionName': 'Ilam',
            'city': 'Ilam',
            'lat': 33.6374,
            'lon': 46.4227,
            'isp': 'Test ISP'
        },
        'test'
    )
    
    # Retrieve cached geo
    cached = db.get_cached_geolocation("192.168.1.100")
    assert cached is not None
    assert cached['country'] == 'Iran'
    
    db.close()
    
    # Cleanup
    os.unlink(temp_db.name)
    
    print("  ✓ Database working")

def test_network_scanner_basic():
    """Test network scanner basic functionality."""
    print("Testing NetworkScanner (basic)...")
    from ilam_miner_detector.network_scanner import NetworkScanner
    
    scanner = NetworkScanner(timeout=1.0, max_concurrent=10)
    
    # Test service identification
    service = scanner._identify_service(3333)
    assert 'Stratum' in service
    
    service = scanner._identify_service(8332)
    assert 'Bitcoin' in service
    
    # Test miner heuristic
    is_miner, miner_type = scanner._detect_miner_heuristic([3333, 4444])
    assert is_miner
    assert miner_type == 'stratum'
    
    is_miner, miner_type = scanner._detect_miner_heuristic([80, 443])
    assert not is_miner
    
    print("  ✓ NetworkScanner basic functions working")

def test_map_generator():
    """Test map generation."""
    print("Testing MapGenerator...")
    from ilam_miner_detector.map_generator import MapGenerator
    
    generator = MapGenerator()
    
    # Test marker color
    color = generator._get_marker_color('stratum')
    assert color == 'red'
    
    color = generator._get_marker_color('bitcoin')
    assert color == 'orange'
    
    # Test map creation (basic)
    hosts = [
        {
            'ip_address': '192.168.1.100',
            'latitude': 33.6374,
            'longitude': 46.4227,
            'city': 'Ilam',
            'region': 'Ilam',
            'miner_type': 'stratum',
            'open_ports': '[3333, 4444]'
        }
    ]
    
    map_obj = generator.create_map(hosts, show_heatmap=False, show_clusters=False)
    assert map_obj is not None
    
    print("  ✓ MapGenerator working")

def test_reporter():
    """Test report generation."""
    print("Testing Reporter...")
    from ilam_miner_detector.reporter import Reporter
    import tempfile
    import json
    
    temp_dir = tempfile.mkdtemp()
    reporter = Reporter(temp_dir)
    
    scan_data = {
        'scan_name': 'Test Scan',
        'target_range': '192.168.1.0/24',
        'started_at': '2024-02-16T10:00:00',
        'completed_at': '2024-02-16T10:05:00',
        'scanned_ips': 10,
        'detected_miners': 2
    }
    
    hosts = [
        {
            'ip_address': '192.168.1.100',
            'hostname': 'miner-01',
            'open_ports': '[3333, 4444]',
            'is_miner': 1,
            'miner_type': 'stratum',
            'city': 'Ilam',
            'region': 'Ilam',
            'country': 'Iran',
            'latitude': 33.6374,
            'longitude': 46.4227
        }
    ]
    
    # Test JSON report
    json_path = reporter.generate_json_report(scan_data, hosts, 'test.json')
    assert os.path.exists(json_path)
    
    with open(json_path, 'r') as f:
        data = json.load(f)
        assert data['scan_metadata']['scan_name'] == 'Test Scan'
        assert len(data['hosts']) == 1
    
    # Test CSV report
    csv_path = reporter.generate_csv_report(hosts, 'test.csv')
    assert os.path.exists(csv_path)
    
    # Test HTML report
    html_path = reporter.generate_html_report(scan_data, hosts, filename='test.html')
    assert os.path.exists(html_path)
    
    # Cleanup
    import shutil
    shutil.rmtree(temp_dir)
    
    print("  ✓ Reporter working")

def main():
    """Run all tests."""
    print("=" * 50)
    print("Ilam Miner Detector - Component Tests")
    print("=" * 50)
    print()
    
    tests = [
        test_config_manager,
        test_ip_manager,
        test_database,
        test_network_scanner_basic,
        test_map_generator,
        test_reporter,
    ]
    
    failed = []
    
    for test in tests:
        try:
            test()
        except Exception as e:
            print(f"  ✗ {test.__name__} FAILED: {str(e)}")
            failed.append((test.__name__, str(e)))
    
    print()
    print("=" * 50)
    
    if not failed:
        print("✅ All tests passed!")
        print("=" * 50)
        return 0
    else:
        print(f"❌ {len(failed)} test(s) failed:")
        for name, error in failed:
            print(f"  - {name}: {error}")
        print("=" * 50)
        return 1

if __name__ == '__main__':
    sys.exit(main())
