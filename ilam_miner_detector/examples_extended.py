"""
Real-world Usage Examples for Iranian Network Miner Detection System
Demonstrates various use cases and API usage patterns.
"""

import asyncio
from datetime import datetime
from pathlib import Path

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
from analytics import get_analytics_service

from iran_geography import (
    get_all_province_names,
    get_cities_in_province,
    get_province_by_name,
    get_province_by_coordinates
)
from iran_isps import (
    get_all_isps,
    identify_isp,
    get_high_risk_isps,
    update_detection_count
)


# ============================================================================
# Example 1: Basic Network Scan
# ============================================================================

async def example_1_basic_scan():
    """Example 1: Basic network scan with CIDR range."""
    print("\n" + "="*60)
    print("Example 1: Basic Network Scan")
    print("="*60)
    
    # Initialize components
    scanner = get_network_scanner()
    ip_manager = get_ip_manager()
    
    # Define CIDR range
    cidr = "192.168.1.0/24"
    print(f"\nScanning CIDR: {cidr}")
    
    # Parse CIDR
    info = ip_manager.parse_cidr(cidr)
    print(f"Total hosts: {info.total_hosts}")
    
    # Generate IP addresses
    ip_list = list(ip_manager.generate_from_cidr(cidr))[:10]  # Limit to 10 for demo
    print(f"Scanning {len(ip_list)} hosts (demo limit)...")
    
    # Define ports to scan
    ports = [3333, 4444, 8332, 8333, 8545, 8080]
    
    # Run scan
    results = await scanner.scan_range(
        ip_list,
        ports,
        progress_callback=lambda c, t, s: print(f"  Progress: {c}/{t}")
    )
    
    # Print results
    print(f"\nResults: {len(results)} hosts scanned")
    for result in results:
        if result.is_miner_detected:
            print(f"  ⚠️  MINER: {result.ip_address} - {result.miner_type} ({result.confidence_score:.1f}%)")
        elif result.is_responsive:
            print(f"  ✓ Responsive: {result.ip_address}")


# ============================================================================
# Example 2: Province-Based Analysis
# ============================================================================

def example_2_province_analysis():
    """Example 2: Analyze miners by Iranian province."""
    print("\n" + "="*60)
    print("Example 2: Province-Based Analysis")
    print("="*60)
    
    # List all provinces
    print("\nAll Iranian Provinces (31):")
    provinces = get_all_province_names()
    for i, province in enumerate(provinces, 1):
        print(f"  {i:2d}. {province}")
    
    # Get detailed information for a province
    print("\nTehran Province Details:")
    tehran = get_province_by_name("Tehran")
    if tehran:
        print(f"  Persian Name: {tehran.name_persian}")
        print(f"  Code: {tehran.code}")
        print(f"  Coordinates: {tehran.latitude:.4f}, {tehran.longitude:.4f}")
        print(f"  Cities ({len(tehran.cities)}):")
        for city in tehran.cities[:5]:  # Show first 5
            print(f"    - {city.name} ({city.latitude:.4f}, {city.longitude:.4f})")
    
    # Get cities in a province
    print("\nCities in Isfahan Province:")
    isfahan_cities = get_cities_in_province("Isfahan")
    for city in isfahan_cities[:10]:  # Show first 10
        print(f"  - {city}")


# ============================================================================
# Example 3: ISP Risk Analysis
# ============================================================================

def example_3_isp_risk_analysis():
    """Example 3: Analyze ISP risk scores and statistics."""
    print("\n" + "="*60)
    print("Example 3: ISP Risk Analysis")
    print("="*60)
    
    # List all ISPs
    print("\nIranian ISPs:")
    isps = get_all_isps()
    
    for isp in isps:
        total_ips = sum(net.num_addresses for net in isp.networks)
        risk_level = "HIGH" if isp.risk_score >= 0.6 else "MEDIUM" if isp.risk_score >= 0.4 else "LOW"
        print(f"  {isp.name}")
        print(f"    Risk Score: {isp.risk_score:.2f} [{risk_level}]")
        print(f"    Total IPs: {total_ips:,}")
        print(f"    Detections: {isp.detection_count}")
        print()
    
    # Identify ISP for specific IP
    test_ips = ["91.98.0.1", "5.116.0.1", "2.176.0.1"]
    print("ISP Identification:")
    for ip in test_ips:
        isp = identify_isp(ip)
        if isp:
            print(f"  {ip} -> {isp.name}")


# ============================================================================
# Example 4: VPN/Proxy Detection
# ============================================================================

def example_4_vpn_detection():
    """Example 4: Detect VPN and proxy services."""
    print("\n" + "="*60)
    print("Example 4: VPN/Proxy Detection")
    print("="*60)
    
    detector = get_vpn_detector()
    
    # Test IPs (mix of residential and potential VPN)
    test_ips = [
        "91.98.0.1",      # Iranian ISP
        "8.8.8.8",        # Google DNS
        "1.1.1.1",        # Cloudflare
        "185.108.80.1",   # Known VPN range
    ]
    
    print("\nVPN/Proxy Detection Results:")
    for ip in test_ips:
        result = detector.check_ip(ip)
        print(f"\n  IP: {ip}")
        print(f"    VPN: {result.is_vpn}")
        print(f"    Proxy: {result.is_proxy}")
        print(f"    Hosting: {result.is_hosting}")
        print(f"    Confidence: {result.confidence:.2f}")
        if result.details:
            for key, value in result.details.items():
                print(f"    {key}: {value}")


# ============================================================================
# Example 5: Detection Rules Engine
# ============================================================================

def example_5_detection_rules():
    """Example 5: Use detection rules engine."""
    print("\n" + "="*60)
    print("Example 5: Detection Rules Engine")
    print("="*60)
    
    # Get detection rules engine
    engine = get_detection_rules_engine()
    
    # Get enabled rules
    enabled_rules = engine.get_enabled_rules()
    print(f"\nEnabled Detection Rules: {len(enabled_rules)}")
    for rule in enabled_rules[:5]:  # Show first 5
        print(f"\n  Rule: {rule.name}")
        print(f"    Description: {rule.description}")
        print(f"    Priority: {rule.priority}")
        print(f"    Confidence: {rule.confidence_score}")
        print(f"    Tags: {', '.join(rule.tags)}")
    
    # Get rules by tag
    print("\nHigh Confidence Rules:")
    high_conf_rules = engine.get_rules_by_tag("high_confidence")
    for rule in high_conf_rules:
        print(f"  - {rule.name}")


# ============================================================================
# Example 6: Analytics and Reporting
# ============================================================================

def example_6_analytics_reporting():
    """Example 6: Generate analytics and reports."""
    print("\n" + "="*60)
    print("Example 6: Analytics and Reporting")
    print("="*60)
    
    analytics = get_analytics_service()
    
    # Get analytics data
    print("\nFetching analytics data...")
    data = analytics.get_analytics_data()
    
    print(f"\nStatistics:")
    print(f"  Total Scans: {data.total_scans}")
    print(f"  Total Hosts: {data.total_hosts}")
    print(f"  Total Miners: {data.total_miners}")
    
    # Detection rate
    detection_rate = 0
    if data.total_hosts > 0:
        detection_rate = (data.total_miners / data.total_hosts) * 100
    print(f"  Detection Rate: {detection_rate:.2f}%")
    
    # Top regions
    print(f"\nTop 5 Vulnerable Regions:")
    for i, (region, count) in enumerate(data.top_vulnerable_regions[:5], 1):
        print(f"  {i}. {region}: {count} miners")
    
    # Top ISPs
    print(f"\nTop 5 High-Risk ISPs:")
    for i, (isp, count) in enumerate(data.high_risk_isps[:5], 1):
        print(f"  {i}. {isp}: {count} miners")
    
    # Miner types
    print(f"\nMiner Types:")
    for miner_type, count in data.miners_by_type.items():
        print(f"  - {miner_type}: {count}")
    
    # Confidence distribution
    print(f"\nConfidence Distribution:")
    for level, count in data.confidence_distribution.items():
        print(f"  - {level}: {count}")


# ============================================================================
# Example 7: Geographic Analysis
# ============================================================================

def example_7_geographic_analysis():
    """Example 7: Geographic analysis with province matching."""
    print("\n" + "="*60)
    print("Example 7: Geographic Analysis")
    print("="*60)
    
    # Test coordinates
    test_locations = [
        (35.6892, 51.3890, "Tehran City"),
        (32.6546, 51.6678, "Isfahan City"),
        (29.5918, 52.5837, "Shiraz City"),
        (36.3172, 59.5628, "Mashhad City"),
    ]
    
    print("\nProvince Matching from Coordinates:")
    for lat, lon, location_name in test_locations:
        province = get_province_by_coordinates(lat, lon)
        if province:
            print(f"\n  {location_name}")
            print(f"    Coordinates: {lat:.4f}, {lon:.4f}")
            print(f"    Matched Province: {province.name} ({province.name_persian})")
            print(f"    Province Code: {province.code}")
        else:
            print(f"\n  {location_name}: No province match")


# ============================================================================
# Example 8: Comprehensive Workflow
# ============================================================================

async def example_8_comprehensive_workflow():
    """Example 8: Complete workflow from scan to reporting."""
    print("\n" + "="*60)
    print("Example 8: Comprehensive Workflow")
    print("="*60)
    
    # Initialize all components
    config = get_config_manager().get()
    db = get_db_manager()
    scanner = get_network_scanner()
    ip_manager = get_ip_manager()
    geo_service = get_geolocation_service()
    vpn_detector = get_vpn_detector()
    rules_engine = get_detection_rules_engine()
    reporter = get_report_generator()
    enhanced_reporter = get_enhanced_report_generator()
    map_gen = get_map_generator()
    analytics = get_analytics_service()
    
    # Step 1: Define scan
    cidr = "192.168.1.0/28"  # Small range for demo
    print(f"\nStep 1: Define scan for {cidr}")
    
    # Step 2: Create scan record
    scan_name = f"Comprehensive Scan {datetime.now().strftime('%Y-%m-%d %H:%M')}"
    scan_id = db.create_scan(scan_name, cidr, "Demo comprehensive workflow")
    print(f"Step 2: Created scan record (ID: {scan_id})")
    
    # Step 3: Run scan
    print("Step 3: Running scan...")
    ip_list = list(ip_manager.generate_from_cidr(cidr))
    ports = config.miner_ports.all_ports[:5]  # Use first 5 ports for demo
    
    results = await scanner.scan_range(
        ip_list,
        ports,
        progress_callback=lambda c, t: print(f"  Scanning: {c}/{t}")
    )
    
    print(f"  Scan complete: {len(results)} hosts")
    
    # Step 4: Apply detection rules
    print("\nStep 4: Applying detection rules...")
    for result in results:
        rule_matches = rules_engine.evaluate(result)
        overall_confidence = rules_engine.calculate_overall_confidence(rule_matches)
        result.confidence_score = max(result.confidence_score, overall_confidence)
        print(f"  {result.ip_address}: {result.confidence_score:.1f}% confidence")
    
    # Step 5: Check for VPN/Proxy
    print("\nStep 5: VPN/Proxy detection...")
    for result in results:
        vpn_result = vpn_detector.check_ip(result.ip_address)
        if vpn_result.is_vpn or vpn_result.is_proxy:
            print(f"  {result.ip_address}: VPN/Proxy detected")
    
    # Step 6: Geolocation
    print("\nStep 6: Geolocation lookup...")
    for result in results:
        if result.is_responsive:
            geo = geo_service.lookup(result.ip_address)
            if geo.success:
                province = get_province_by_coordinates(geo.latitude, geo.longitude)
                province_name = province.name if province else "Unknown"
                print(f"  {result.ip_address}: {geo.city}, {province_name}")
    
    # Step 7: Generate reports
    print("\nStep 7: Generating reports...")
    
    try:
        json_path = reporter.export_json(scan_id)
        print(f"  ✓ JSON: {json_path}")
    except Exception as e:
        print(f"  ✗ JSON: {e}")
    
    try:
        pdf_path = enhanced_reporter.export_pdf(scan_id)
        print(f"  ✓ PDF: {pdf_path}")
    except Exception as e:
        print(f"  ✗ PDF: {e}")
    
    try:
        excel_path = enhanced_reporter.export_excel(scan_id)
        print(f"  ✓ Excel: {excel_path}")
    except Exception as e:
        print(f"  ✗ Excel: {e}")
    
    # Step 8: Generate analytics
    print("\nStep 8: Analytics...")
    analytics.clear_cache()
    try:
        analytics_path = analytics.export_analytics_json()
        print(f"  ✓ Analytics: {analytics_path}")
    except Exception as e:
        print(f"  ✗ Analytics: {e}")
    
    print("\nWorkflow complete!")


# ============================================================================
# Main: Run All Examples
# ============================================================================

async def main():
    """Run all examples."""
    print("\n" + "╔" + "="*58 + "╗")
    print("║" + " "*10 + "Iranian Network Miner Detection System" + " "*10 + "║")
    print("║" + " "*20 + "Usage Examples" + " "*25 + "║")
    print("╚" + "="*58 + "╝")
    
    # Run examples
    await example_1_basic_scan()
    example_2_province_analysis()
    example_3_isp_risk_analysis()
    example_4_vpn_detection()
    example_5_detection_rules()
    example_6_analytics_reporting()
    example_7_geographic_analysis()
    await example_8_comprehensive_workflow()
    
    print("\n" + "="*60)
    print("All examples completed!")
    print("="*60)


if __name__ == "__main__":
    asyncio.run(main())
