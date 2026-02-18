"""
VPN and Proxy Detection Module.
Identifies VPN, proxy, and anonymization services for security auditing.
"""

import re
import logging
from typing import List, Dict, Optional, Set
from dataclasses import dataclass
from ipaddress import ip_address, ip_network
import requests

logger = logging.getLogger(__name__)


@dataclass
class VPN检测结果:
    """Result of VPN/proxy detection."""
    ip_address: str
    is_vpn: bool
    is_proxy: bool
    is_tor: bool
    is_hosting: bool
    is_mobile: bool
    vpn_type: str = ""
    proxy_type: str = ""
    confidence: float = 0.0
    details: Dict[str, str] = None
    
    def __post_init__(self):
        if self.details is None:
            self.details = {}


class VPNDetector:
    """
    Detects VPN, proxy, and anonymization services.
    Uses multiple heuristics and external services for detection.
    """
    
    # Known VPN/proxy provider ASNs
    KNOWN_VPN_ASNS = {
        # NordVPN
        "AS44950",
        # ExpressVPN
        "AS13876",
        # Surfshark
        "AS13335",
        # CyberGhost
        "AS56589",
        # Private Internet Access
        "AS11776",
        # Mullvad
        "AS52019",
        # ProtonVPN
        "AS52019",
        # PureVPN
        "AS15169",
        # HideMyAss
        "AS41692",
        # IPVanish
        "AS46475",
        # VyprVPN
        "AS13536",
    }
    
    # Known VPN/proxy provider IP ranges (sample)
    KNOWN_VPN_RANGES = [
        "185.108.80.0/22",  # NordVPN
        "185.162.228.0/22",  # NordVPN
        "185.213.84.0/22",  # NordVPN
        "89.40.116.0/22",   # CyberGhost
        "37.120.128.0/17",  # Mullvad
    ]
    
    # Common VPN port patterns
    VPN_PORTS = {
        "openvpn": [1194, 443],
        "wireguard": [51820],
        "pptp": [1723],
        "l2tp": [500, 1701],
        "sstp": [443],
        "ikev2": [500, 4500],
    }
    
    # Tor exit node patterns
    TOR_EXIT_PATTERNS = [
        r"tor\d+\.exit",
        r"exitnode\.torproject\.org",
        r"tor-exit",
    ]
    
    # Hosting/Cloud provider ASN patterns
    HOSTING_ASNS = {
        "AS16509",  # AWS
        "AS15169",  # Google Cloud
        "AS8075",   # Microsoft Azure
        "AS14061",  # DigitalOcean
        "AS20473",  # Choopa
        "AS16276",  # OVH
    }
    
    def __init__(self):
        self._cache: Dict[str, VPN检测结果] = {}
        self._known_vpn_networks = self._load_vpn_networks()
        self._known_hosting_networks = self._load_hosting_networks()
    
    def _load_vpn_networks(self) -> List[IPv4Network]:
        """Load known VPN/proxy network ranges."""
        networks = []
        for cidr in self.KNOWN_VPN_RANGES:
            try:
                networks.append(ip_network(cidr, strict=False))
            except ValueError:
                pass
        return networks
    
    def _load_hosting_networks(self) -> List[IPv4Network]:
        """Load known hosting provider network ranges."""
        # Common cloud provider ranges
        networks = [
            ip_network("3.0.0.0/8"),      # AWS
            ip_network("52.0.0.0/8"),     # AWS
            ip_network("54.0.0.0/8"),     # AWS
            ip_network("35.0.0.0/8"),     # Google
            ip_network("104.0.0.0/8"),    # Cloud
            ip_network("172.64.0.0/13"),  # Cloudflare
            ip_network("104.16.0.0/13"),  # Cloudflare
        ]
        return networks
    
    def check_ip(self, ip_address_str: str, use_cache: bool = True) -> VPN检测结果:
        """
        Check if an IP is a VPN, proxy, or anonymization service.
        
        Args:
            ip_address_str: IP address to check
            use_cache: Whether to use cached results
            
        Returns:
            VPN检测结果 with detection information
        """
        if use_cache and ip_address_str in self._cache:
            return self._cache[ip_address_str]
        
        try:
            ip = ip_address(ip_address_str)
        except ValueError:
            return VPN检测结果(
                ip_address=ip_address_str,
                is_vpn=False,
                is_proxy=False,
                is_tor=False,
                is_hosting=False,
                is_mobile=False,
                confidence=0.0,
                details={"error": "Invalid IP address"}
            )
        
        result = VPN检测结果(
            ip_address=ip_address_str,
            is_vpn=False,
            is_proxy=False,
            is_tor=False,
            is_hosting=False,
            is_mobile=False,
            confidence=0.0,
            details={}
        )
        
        # Check against known VPN ranges
        is_vpn_range = False
        for network in self._known_vpn_networks:
            if ip in network:
                is_vpn_range = True
                result.is_vpn = True
                result.vpn_type = "known_range"
                result.confidence = 0.95
                result.details["match_reason"] = "Matched known VPN range"
                break
        
        # Check against hosting ranges
        if not is_vpn_range:
            for network in self._known_hosting_networks:
                if ip in network:
                    result.is_hosting = True
                    result.confidence = max(result.confidence, 0.7)
                    result.details["hosting"] = "Matched hosting provider range"
                    break
        
        # Check for residential proxy patterns
        is_residential_proxy = self._check_residential_proxy(ip)
        if is_residential_proxy:
            result.is_proxy = True
            result.proxy_type = "residential"
            result.confidence = max(result.confidence, 0.6)
            result.details["proxy_type"] = "Residential proxy pattern detected"
        
        # Check mobile patterns
        is_mobile = self._check_mobile_proxy(ip)
        if is_mobile:
            result.is_mobile = True
            result.confidence = max(result.confidence, 0.5)
            result.details["mobile"] = "Mobile network detected"
        
        # Try external service detection (if available)
        self._check_external_services(ip, result)
        
        # Cache result
        if use_cache:
            self._cache[ip_address_str] = result
        
        return result
    
    def _check_residential_proxy(self, ip) -> bool:
        """
        Check if IP matches residential proxy patterns.
        This is heuristic-based and not 100% accurate.
        """
        # Check for residential ISP ranges (Iranian)
        iran_isps = self._load_iran_isp_networks()
        for network in iran_isps:
            if ip in network:
                return False  # Known legitimate ISP
        
        # Check for datacenter IP patterns
        # Datacenters often have IP addresses in specific ranges
        first_octet = int(str(ip).split('.')[0])
        if first_octet in [104, 107, 52, 34, 35]:
            # Common cloud/hosting first octets
            return True
        
        return False
    
    def _check_mobile_proxy(self, ip) -> bool:
        """Check if IP is from mobile network."""
        # This is a simplified check
        # In production, use ASN lookup to identify mobile carriers
        mobile_asn_patterns = [
            "AS197207",  # Irancell (MTN)
            "AS57218",   # RighTel
            # Add more mobile ASNs as needed
        ]
        # Would need actual ASN lookup here
        return False
    
    def _load_iran_isp_networks(self) -> List[IPv4Network]:
        """Load Iranian ISP networks for exclusion."""
        # Import from iran_isps module
        try:
            from .iran_isps import get_all_isps
            networks = []
            for isp in get_all_isps():
                networks.extend(isp.networks)
            return networks
        except ImportError:
            logger.warning("Could not load Iranian ISP data")
            return []
    
    def _check_external_services(self, ip, result: VPN检测结果):
        """
        Check external services for VPN/proxy detection.
        This makes API calls to services like ip-api.com, ipinfo.io, etc.
        """
        try:
            # Try ip-api.com (free, rate-limited)
            url = f"http://ip-api.com/json/{ip}"
            response = requests.get(url, timeout=5)
            if response.status_code == 200:
                data = response.json()
                if data.get("status") == "success":
                    # Check ISP/organization for VPN indicators
                    org = data.get("org", "").lower()
                    isp = data.get("isp", "").lower()
                    
                    vpn_keywords = ["vpn", "proxy", "tor", "anonymous", "datacenter", 
                                   "hosting", "cloud", "vps"]
                    
                    combined_text = org + " " + isp
                    for keyword in vpn_keywords:
                        if keyword in combined_text:
                            result.is_vpn = result.is_vpn or keyword in ["vpn", "tor"]
                            result.is_proxy = result.is_proxy or keyword in ["proxy", "tor", "anonymous"]
                            result.confidence = max(result.confidence, 0.8)
                            result.details["external_match"] = f"Matched keyword: {keyword}"
                            break
                    
                    # Check AS number
                    as_number = data.get("as", "")
                    if as_number:
                        asn = as_number.split()[0]
                        if asn in self.KNOWN_VPN_ASNS:
                            result.is_vpn = True
                            result.vpn_type = "known_asn"
                            result.confidence = 0.95
                            result.details["matched_asn"] = asn
                        elif asn in self.HOSTING_ASNS:
                            result.is_hosting = True
                            result.confidence = max(result.confidence, 0.7)
                            result.details["hosting_asn"] = asn
                    
                    # Check country
                    country = data.get("countryCode", "")
                    if country in ["KY", "BZ", "PA", "VG"]:
                        # Common VPN server locations
                        result.is_vpn = result.is_vpn or True
                        result.confidence = max(result.confidence, 0.6)
                        result.details["vpn_location"] = country
        
        except Exception as e:
            logger.debug(f"External service check failed: {e}")
    
    def check_batch(self, ip_addresses: List[str]) -> Dict[str, VPN检测结果]:
        """
        Check multiple IP addresses for VPN/proxy status.
        
        Args:
            ip_addresses: List of IP addresses to check
            
        Returns:
            Dictionary mapping IP addresses to detection results
        """
        results = {}
        for ip in ip_addresses:
            results[ip] = self.check_ip(ip)
        return results
    
    def get_vpn_statistics(self, scan_results: List[VPN检测结果]) -> Dict[str, int]:
        """
        Get statistics from a batch of detection results.
        
        Args:
            scan_results: List of VPN检测结果 objects
            
        Returns:
            Dictionary with statistics
        """
        stats = {
            "total": len(scan_results),
            "vpn_detected": 0,
            "proxy_detected": 0,
            "tor_detected": 0,
            "hosting_detected": 0,
            "mobile_detected": 0,
            "high_confidence": 0,
            "medium_confidence": 0,
            "low_confidence": 0,
        }
        
        for result in scan_results:
            if result.is_vpn:
                stats["vpn_detected"] += 1
            if result.is_proxy:
                stats["proxy_detected"] += 1
            if result.is_tor:
                stats["tor_detected"] += 1
            if result.is_hosting:
                stats["hosting_detected"] += 1
            if result.is_mobile:
                stats["mobile_detected"] += 1
            
            if result.confidence >= 0.7:
                stats["high_confidence"] += 1
            elif result.confidence >= 0.4:
                stats["medium_confidence"] += 1
            else:
                stats["low_confidence"] += 1
        
        return stats
    
    def clear_cache(self):
        """Clear the detection cache."""
        self._cache.clear()


# Singleton instance
_detector_instance: Optional[VPNDetector] = None


def get_vpn_detector() -> VPNDetector:
    """Get singleton VPN detector instance."""
    global _detector_instance
    if _detector_instance is None:
        _detector_instance = VPNDetector()
    return _detector_instance
