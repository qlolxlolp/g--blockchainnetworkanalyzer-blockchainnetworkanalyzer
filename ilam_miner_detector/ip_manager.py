"""
IP address management and generation utilities.
Handles CIDR notation, IP range expansion, and validation.
"""

import ipaddress
import random
from typing import Iterator, List, Tuple, Optional


class IPManager:
    """Manages IP address generation and validation for network scanning."""
    
    @staticmethod
    def parse_cidr(cidr: str) -> Iterator[str]:
        """
        Parse CIDR notation and generate all IP addresses in range.
        
        Args:
            cidr: CIDR notation string (e.g., '192.168.1.0/24')
            
        Yields:
            IP address strings
            
        Raises:
            ValueError: If CIDR notation is invalid
        """
        try:
            network = ipaddress.ip_network(cidr, strict=False)
            for ip in network.hosts():
                yield str(ip)
        except ValueError as e:
            raise ValueError(f"Invalid CIDR notation '{cidr}': {e}")
    
    @staticmethod
    def parse_range(start_ip: str, end_ip: str) -> Iterator[str]:
        """
        Generate IP addresses between start and end (inclusive).
        
        Args:
            start_ip: Starting IP address
            end_ip: Ending IP address
            
        Yields:
            IP address strings
            
        Raises:
            ValueError: If IP addresses are invalid or end < start
        """
        try:
            start = ipaddress.ip_address(start_ip)
            end = ipaddress.ip_address(end_ip)
            
            if end < start:
                raise ValueError("End IP must be greater than or equal to start IP")
            
            current = int(start)
            end_int = int(end)
            
            while current <= end_int:
                yield str(ipaddress.ip_address(current))
                current += 1
                
        except ValueError as e:
            raise ValueError(f"Invalid IP range '{start_ip}' to '{end_ip}': {e}")
    
    @staticmethod
    def parse_list(ip_list: List[str]) -> Iterator[str]:
        """
        Validate and yield IPs from a list.
        
        Args:
            ip_list: List of IP address strings
            
        Yields:
            Valid IP address strings
            
        Raises:
            ValueError: If any IP is invalid
        """
        for ip_str in ip_list:
            try:
                # Validate IP
                ipaddress.ip_address(ip_str.strip())
                yield ip_str.strip()
            except ValueError as e:
                raise ValueError(f"Invalid IP address '{ip_str}': {e}")
    
    @staticmethod
    def is_private(ip: str) -> bool:
        """
        Check if IP address is in private range.
        
        Args:
            ip: IP address string
            
        Returns:
            True if IP is private (RFC 1918, loopback, link-local, etc.)
        """
        try:
            ip_obj = ipaddress.ip_address(ip)
            return ip_obj.is_private or ip_obj.is_loopback or ip_obj.is_link_local
        except ValueError:
            return False
    
    @staticmethod
    def is_valid(ip: str) -> bool:
        """
        Validate IP address format.
        
        Args:
            ip: IP address string
            
        Returns:
            True if valid IP address
        """
        try:
            ipaddress.ip_address(ip)
            return True
        except ValueError:
            return False
    
    @staticmethod
    def count_ips_in_cidr(cidr: str) -> int:
        """
        Count total IPs in CIDR range (excluding network/broadcast).
        
        Args:
            cidr: CIDR notation string
            
        Returns:
            Number of host IPs
        """
        try:
            network = ipaddress.ip_network(cidr, strict=False)
            return network.num_addresses - 2  # Exclude network and broadcast
        except ValueError:
            return 0
    
    @staticmethod
    def count_ips_in_range(start_ip: str, end_ip: str) -> int:
        """
        Count IPs between start and end (inclusive).
        
        Args:
            start_ip: Starting IP
            end_ip: Ending IP
            
        Returns:
            Number of IPs in range
        """
        try:
            start = int(ipaddress.ip_address(start_ip))
            end = int(ipaddress.ip_address(end_ip))
            return max(0, end - start + 1)
        except ValueError:
            return 0
    
    @staticmethod
    def generate_random_ips(count: int, exclude_private: bool = True) -> List[str]:
        """
        Generate random public IP addresses.
        
        Args:
            count: Number of IPs to generate
            exclude_private: If True, exclude private IP ranges
            
        Returns:
            List of random IP addresses
        """
        ips = []
        attempts = 0
        max_attempts = count * 10  # Prevent infinite loop
        
        while len(ips) < count and attempts < max_attempts:
            ip = f"{random.randint(1, 254)}.{random.randint(0, 255)}.{random.randint(0, 255)}.{random.randint(1, 254)}"
            
            if exclude_private and IPManager.is_private(ip):
                attempts += 1
                continue
            
            if ip not in ips:
                ips.append(ip)
            
            attempts += 1
        
        return ips
    
    @staticmethod
    def filter_ilam_region(ip: str, geo_data: dict, 
                          lat_range: Tuple[float, float] = (32.5, 33.5),
                          lon_range: Tuple[float, float] = (46.0, 47.5)) -> bool:
        """
        Check if IP's geolocation is within Ilam province bounds.
        
        Args:
            ip: IP address
            geo_data: Geolocation data dictionary with 'lat' and 'lon' keys
            lat_range: Latitude range tuple (min, max)
            lon_range: Longitude range tuple (min, max)
            
        Returns:
            True if IP is within Ilam region bounds
        """
        lat = geo_data.get('lat') or geo_data.get('latitude')
        lon = geo_data.get('lon') or geo_data.get('longitude')
        
        if lat is None or lon is None:
            return False
        
        return (lat_range[0] <= lat <= lat_range[1] and 
                lon_range[0] <= lon <= lon_range[1])
    
    @staticmethod
    def parse_input(ip_input: str) -> Iterator[str]:
        """
        Smart parser for various IP input formats.
        
        Supports:
        - Single IP: '192.168.1.1'
        - CIDR: '192.168.1.0/24'
        - Range: '192.168.1.1-192.168.1.254'
        - Comma-separated: '192.168.1.1, 10.0.0.1'
        
        Args:
            ip_input: IP address input string
            
        Yields:
            IP address strings
            
        Raises:
            ValueError: If input format is invalid
        """
        ip_input = ip_input.strip()
        
        # Check for comma-separated list
        if ',' in ip_input:
            ips = [ip.strip() for ip in ip_input.split(',')]
            yield from IPManager.parse_list(ips)
        
        # Check for range notation
        elif '-' in ip_input and '/' not in ip_input:
            parts = ip_input.split('-')
            if len(parts) != 2:
                raise ValueError("Range format must be 'START_IP-END_IP'")
            start_ip, end_ip = parts[0].strip(), parts[1].strip()
            yield from IPManager.parse_range(start_ip, end_ip)
        
        # Check for CIDR notation
        elif '/' in ip_input:
            yield from IPManager.parse_cidr(ip_input)
        
        # Single IP
        else:
            if not IPManager.is_valid(ip_input):
                raise ValueError(f"Invalid IP address: {ip_input}")
            yield ip_input
