"""
Geolocation service for IP address location lookup.
Supports multiple providers with rate limiting and caching.
"""

import asyncio
import aiohttp
import time
from typing import Optional, Dict, Any
from dataclasses import dataclass
import logging


@dataclass
class RateLimiter:
    """Token bucket rate limiter."""
    max_tokens: int
    refill_rate: float  # tokens per second
    
    def __post_init__(self):
        self.tokens = self.max_tokens
        self.last_refill = time.time()
        self._lock = asyncio.Lock()
    
    async def acquire(self):
        """Acquire a token, waiting if necessary."""
        async with self._lock:
            while self.tokens < 1:
                # Refill tokens
                now = time.time()
                elapsed = now - self.last_refill
                refill_amount = elapsed * self.refill_rate
                self.tokens = min(self.max_tokens, self.tokens + refill_amount)
                self.last_refill = now
                
                if self.tokens < 1:
                    # Wait until we can refill at least one token
                    wait_time = (1.0 - self.tokens) / self.refill_rate
                    await asyncio.sleep(wait_time)
            
            self.tokens -= 1


class GeolocationService:
    """Provides IP geolocation using multiple providers."""
    
    def __init__(self, 
                 rate_limit_per_minute: int = 45,
                 api_key: Optional[str] = None,
                 cache_provider: Optional[Any] = None):
        """
        Initialize geolocation service.
        
        Args:
            rate_limit_per_minute: Maximum requests per minute (default: 45 for ip-api.com)
            api_key: Optional API key for premium providers
            cache_provider: Optional cache object with get/set methods
        """
        self.api_key = api_key
        self.cache = cache_provider
        
        # Rate limiter: convert per-minute to per-second rate
        self.rate_limiter = RateLimiter(
            max_tokens=rate_limit_per_minute,
            refill_rate=rate_limit_per_minute / 60.0
        )
        
        self.logger = logging.getLogger(__name__)
        self._session = None
    
    async def _get_session(self) -> aiohttp.ClientSession:
        """Get or create aiohttp session."""
        if self._session is None or self._session.closed:
            self._session = aiohttp.ClientSession()
        return self._session
    
    async def close(self):
        """Close HTTP session."""
        if self._session and not self._session.closed:
            await self._session.close()
    
    async def lookup(self, ip: str) -> Optional[Dict[str, Any]]:
        """
        Lookup geolocation for an IP address.
        
        Args:
            ip: IP address to lookup
            
        Returns:
            Dictionary with geolocation data or None on failure
        """
        # Check cache first
        if self.cache:
            cached = self.cache.get_cached_geolocation(ip)
            if cached:
                self.logger.debug(f"Cache hit for {ip}")
                return cached
        
        # Rate limit
        await self.rate_limiter.acquire()
        
        # Try primary provider (ip-api.com)
        result = await self._lookup_ip_api(ip)
        
        # Try fallback if primary fails and API key is available
        if not result and self.api_key:
            result = await self._lookup_ipinfo(ip)
        
        # Cache result
        if result and self.cache:
            self.cache.cache_geolocation(ip, result, result.get('source', 'unknown'))
        
        return result
    
    async def _lookup_ip_api(self, ip: str) -> Optional[Dict[str, Any]]:
        """
        Lookup using ip-api.com (free, no API key required).
        
        Rate limit: 45 requests per minute
        
        Args:
            ip: IP address
            
        Returns:
            Geolocation dictionary or None
        """
        url = f"http://ip-api.com/json/{ip}"
        params = {
            'fields': 'status,message,country,countryCode,region,regionName,city,lat,lon,isp,org,as,query'
        }
        
        try:
            session = await self._get_session()
            async with session.get(url, params=params, timeout=10) as response:
                if response.status == 200:
                    data = await response.json()
                    
                    if data.get('status') == 'success':
                        data['source'] = 'ip-api'
                        self.logger.info(f"Geolocation found for {ip}: {data.get('city')}, {data.get('regionName')}, {data.get('country')}")
                        return data
                    else:
                        self.logger.warning(f"ip-api.com lookup failed for {ip}: {data.get('message')}")
                else:
                    self.logger.error(f"ip-api.com returned status {response.status} for {ip}")
        
        except asyncio.TimeoutError:
            self.logger.error(f"Timeout looking up {ip} on ip-api.com")
        except Exception as e:
            self.logger.error(f"Error looking up {ip} on ip-api.com: {e}")
        
        return None
    
    async def _lookup_ipinfo(self, ip: str) -> Optional[Dict[str, Any]]:
        """
        Lookup using ipinfo.io (requires API key for high volume).
        
        Args:
            ip: IP address
            
        Returns:
            Geolocation dictionary or None
        """
        url = f"https://ipinfo.io/{ip}/json"
        headers = {}
        
        if self.api_key:
            headers['Authorization'] = f'Bearer {self.api_key}'
        
        try:
            session = await self._get_session()
            async with session.get(url, headers=headers, timeout=10) as response:
                if response.status == 200:
                    data = await response.json()
                    
                    # Convert ipinfo format to ip-api format for consistency
                    loc = data.get('loc', '').split(',')
                    
                    normalized = {
                        'query': ip,
                        'country': data.get('country'),
                        'countryCode': data.get('country'),
                        'region': data.get('region'),
                        'regionName': data.get('region'),
                        'city': data.get('city'),
                        'lat': float(loc[0]) if len(loc) > 0 else None,
                        'lon': float(loc[1]) if len(loc) > 1 else None,
                        'isp': data.get('org'),
                        'org': data.get('org'),
                        'as': data.get('org'),
                        'source': 'ipinfo'
                    }
                    
                    self.logger.info(f"Geolocation found for {ip}: {normalized.get('city')}, {normalized.get('region')}, {normalized.get('country')}")
                    return normalized
                else:
                    self.logger.error(f"ipinfo.io returned status {response.status} for {ip}")
        
        except asyncio.TimeoutError:
            self.logger.error(f"Timeout looking up {ip} on ipinfo.io")
        except Exception as e:
            self.logger.error(f"Error looking up {ip} on ipinfo.io: {e}")
        
        return None
    
    def is_in_ilam_region(self, geo_data: Dict[str, Any],
                          lat_range: tuple = (32.5, 33.5),
                          lon_range: tuple = (46.0, 47.5)) -> bool:
        """
        Check if geolocation is within Ilam province bounds.
        
        Ilam province approximate coordinates:
        - Latitude: 32.5°N to 33.5°N
        - Longitude: 46.0°E to 47.5°E
        
        Args:
            geo_data: Geolocation dictionary
            lat_range: Latitude bounds (min, max)
            lon_range: Longitude bounds (min, max)
            
        Returns:
            True if within Ilam region
        """
        lat = geo_data.get('lat')
        lon = geo_data.get('lon')
        
        if lat is None or lon is None:
            return False
        
        in_region = (lat_range[0] <= lat <= lat_range[1] and 
                    lon_range[0] <= lon <= lon_range[1])
        
        if in_region:
            self.logger.info(f"IP {geo_data.get('query')} is in Ilam region: ({lat}, {lon})")
        
        return in_region
    
    async def batch_lookup(self, ips: list, filter_ilam: bool = False) -> Dict[str, Dict[str, Any]]:
        """
        Lookup multiple IPs with rate limiting.
        
        Args:
            ips: List of IP addresses
            filter_ilam: If True, only return IPs in Ilam region
            
        Returns:
            Dictionary mapping IP -> geolocation data
        """
        results = {}
        
        for ip in ips:
            geo_data = await self.lookup(ip)
            
            if geo_data:
                if filter_ilam:
                    if self.is_in_ilam_region(geo_data):
                        results[ip] = geo_data
                else:
                    results[ip] = geo_data
        
        return results
