"""
IP address management for Ilam Miner Detector.
Handles CIDR notation, range generation, and filtering.
"""

import ipaddress
import socket
from typing import List, Iterator, Optional, Tuple, Set
from dataclasses import dataclass
import logging

logger = logging.getLogger(__name__)


@dataclass
class IPRangeInfo:
    """Information about an IP range."""
    network: ipaddress.IPv4Network
    total_hosts: int
    first_ip: ipaddress.IPv4Address
    last_ip: ipaddress.IPv4Address
    is_private: bool


class IPManager:
    """
    Manages IP addresses, CIDR ranges, and filtering.
    """
    
    # Common private IP ranges
    PRIVATE_NETWORKS = [
        ipaddress.IPv4Network("10.0.0.0/8"),
        ipaddress.IPv4Network("172.16.0.0/12"),
        ipaddress.IPv4Network("192.168.0.0/16"),
        ipaddress.IPv4Network("127.0.0.0/8"),
        ipaddress.IPv4Network("169.254.0.0/16"),  # Link-local
    ]
    
    def __init__(self):
        self._exclude_private = False
        self._excluded_networks: Set[ipaddress.IPv4Network] = set()
    
    def parse_cidr(self, cidr: str) -> IPRangeInfo:
        """
        Parse a CIDR notation string and return range information.
        
        Args:
            cidr: CIDR notation (e.g., "192.168.1.0/24")
            
        Returns:
            IPRangeInfo object with network details
            
        Raises:
            ValueError: If CIDR is invalid
        """
        try:
            network = ipaddress.IPv4Network(cidr, strict=False)
            hosts = list(network.hosts())
            
            return IPRangeInfo(
                network=network,
                total_hosts=len(hosts),
                first_ip=hosts[0] if hosts else network.network_address,
                last_ip=hosts[-1] if hosts else network.broadcast_address,
                is_private=self.is_private_ip(network.network_address)
            )
        except ValueError as e:
            raise ValueError(f"Invalid CIDR notation: {cidr}") from e
    
    def parse_ip_list(self, ip_list: str) -> List[ipaddress.IPv4Address]:
        """
        Parse a comma-separated list of IPs or CIDR ranges.
        
        Args:
            ip_list: Comma-separated IPs or CIDRs (e.g., "192.168.1.1,10.0.0.0/24")
            
        Returns:
            List of unique IP addresses
        """
        ips = set()
        for item in ip_list.split(','):
            item = item.strip()
            if not item:
                continue
                
            try:
                if '/' in item:
                    # CIDR notation
                    network = ipaddress.IPv4Network(item, strict=False)
                    for host in network.hosts():
                        if not self._should_exclude(host):
                            ips.add(host)
                else:
                    # Single IP
                    ip = ipaddress.IPv4Address(item)
                    if not self._should_exclude(ip):
                        ips.add(ip)
            except ValueError as e:
                logger.warning(f"Skipping invalid IP/CIDR: {item} - {e}")
                
        return sorted(list(ips))
    
    def generate_ip_range(self, start_ip: str, end_ip: str) -> Iterator[ipaddress.IPv4Address]:
        """
        Generate IP addresses from start to end (inclusive).
        
        Args:
            start_ip: Starting IP address
            end_ip: Ending IP address
            
        Yields:
            IPv4Address objects
        """
        try:
            start = ipaddress.IPv4Address(start_ip)
            end = ipaddress.IPv4Address(end_ip)
            
            if start > end:
                raise ValueError(f"Start IP {start_ip} must be <= end IP {end_ip}")
            
            current = int(start)
            end_int = int(end)
            
            while current <= end_int:
                ip = ipaddress.IPv4Address(current)
                if not self._should_exclude(ip):
                    yield ip
                current += 1
                
        except ValueError as e:
            raise ValueError(f"Invalid IP range: {start_ip} - {end_ip}") from e
    
    def generate_from_cidr(self, cidr: str) -> Iterator[ipaddress.IPv4Address]:
        """
        Generate all host IPs from a CIDR range.
        Memory-efficient generator for large ranges.
        
        Args:
            cidr: CIDR notation (e.g., "192.168.1.0/24")
            
        Yields:
            IPv4Address objects
        """
        try:
            network = ipaddress.IPv4Network(cidr, strict=False)
            for host in network.hosts():
                if not self._should_exclude(host):
                    yield host
        except ValueError as e:
            raise ValueError(f"Invalid CIDR: {cidr}") from e
    
    def is_private_ip(self, ip: ipaddress.IPv4Address) -> bool:
        """Check if an IP is in a private range."""
        return ip.is_private or ip.is_loopback or ip.is_link_local
    
    def is_in_ilam_region(self, latitude: float, longitude: float,
                          min_lat: float = 32.5, max_lat: float = 33.5,
                          min_lon: float = 46.0, max_lon: float = 47.5) -> bool:
        """
        Check if coordinates are within Ilam province bounds.
        
        Args:
            latitude: Latitude coordinate
            longitude: Longitude coordinate
            min_lat: Minimum latitude bound
            max_lat: Maximum latitude bound
            min_lon: Minimum longitude bound
            max_lon: Maximum longitude bound
            
        Returns:
            True if coordinates are within bounds
        """
        return (min_lat <= latitude <= max_lat and 
                min_lon <= longitude <= max_lon)
    
    def exclude_private(self, exclude: bool = True) -> 'IPManager':
        """Set whether to exclude private IPs from results."""
        self._exclude_private = exclude
        return self
    
    def add_excluded_network(self, cidr: str) -> 'IPManager':
        """Add a network to the exclusion list."""
        try:
            network = ipaddress.IPv4Network(cidr, strict=False)
            self._excluded_networks.add(network)
        except ValueError as e:
            logger.warning(f"Invalid excluded network: {cidr} - {e}")
        return self
    
    def _should_exclude(self, ip: ipaddress.IPv4Address) -> bool:
        """Check if an IP should be excluded based on current settings."""
        if self._exclude_private and self.is_private_ip(ip):
            return True
        
        for network in self._excluded_networks:
            if ip in network:
                return True
        
        return False
    
    def validate_ip(self, ip_str: str) -> bool:
        """Validate if a string is a valid IPv4 address."""
        try:
            ipaddress.IPv4Address(ip_str)
            return True
        except ValueError:
            return False
    
    def validate_cidr(self, cidr: str) -> bool:
        """Validate if a string is a valid CIDR notation."""
        try:
            ipaddress.IPv4Network(cidr, strict=False)
            return True
        except ValueError:
            return False
    
    def resolve_hostname(self, hostname: str) -> Optional[str]:
        """
        Resolve a hostname to an IP address.
        
        Args:
            hostname: Hostname to resolve
            
        Returns:
            IP address string or None if resolution fails
        """
        try:
            return socket.gethostbyname(hostname)
        except socket.gaierror:
            return None
    
    def get_network_info(self, cidr: str) -> dict:
        """
        Get detailed information about a network.
        
        Args:
            cidr: CIDR notation
            
        Returns:
            Dictionary with network information
        """
        network = ipaddress.IPv4Network(cidr, strict=False)
        hosts = list(network.hosts())
        
        return {
            "network": str(network),
            "netmask": str(network.netmask),
            "broadcast": str(network.broadcast_address),
            "first_host": str(hosts[0]) if hosts else None,
            "last_host": str(hosts[-1]) if hosts else None,
            "total_hosts": len(hosts),
            "is_private": network.is_private,
        }
    
    def estimate_scan_time(self, cidr: str, timeout_per_host: float = 3.0,
                          concurrency: int = 50) -> Tuple[int, str]:
        """
        Estimate scan time for a CIDR range.
        
        Args:
            cidr: CIDR notation
            timeout_per_host: Timeout per host in seconds
            concurrency: Number of concurrent scans
            
        Returns:
            Tuple of (estimated_seconds, human_readable_string)
        """
        info = self.parse_cidr(cidr)
        
        # Calculate batches
        batches = (info.total_hosts + concurrency - 1) // concurrency
        estimated_seconds = int(batches * timeout_per_host)
        
        # Format human readable
        if estimated_seconds < 60:
            time_str = f"{estimated_seconds} seconds"
        elif estimated_seconds < 3600:
            minutes = estimated_seconds // 60
            seconds = estimated_seconds % 60
            time_str = f"{minutes}m {seconds}s"
        else:
            hours = estimated_seconds // 3600
            minutes = (estimated_seconds % 3600) // 60
            time_str = f"{hours}h {minutes}m"
        
        return estimated_seconds, time_str


def get_ip_manager() -> IPManager:
    """Get IP manager instance."""
    return IPManager()
