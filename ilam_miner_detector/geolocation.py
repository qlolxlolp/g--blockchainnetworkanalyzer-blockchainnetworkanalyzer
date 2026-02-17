"""
Geolocation service for Ilam Miner Detector.
Multi-provider IP geolocation with caching and rate limiting.
"""

import time
import json
import logging
import requests
from typing import Optional, Dict, Any
from dataclasses import dataclass
from datetime import datetime
from threading import Lock

from .config_manager import get_config_manager, IlamRegionConfig
from .database import get_db_manager, GeolocationCache

logger = logging.getLogger(__name__)


@dataclass
class GeolocationResult:
    """Result of geolocation lookup."""
    ip_address: str
    country: str = ""
    country_code: str = ""
    region: str = ""
    city: str = ""
    latitude: float = 0.0
    longitude: float = 0.0
    isp: str = ""
    org: str = ""
    is_in_ilam: bool = False
    success: bool = False
    error: str = ""
    
    def to_dict(self) -> Dict[str, Any]:
        return {
            "ip_address": self.ip_address,
            "country": self.country,
            "country_code": self.country_code,
            "region": self.region,
            "city": self.city,
            "latitude": self.latitude,
            "longitude": self.longitude,
            "isp": self.isp,
            "org": self.org,
            "is_in_ilam": self.is_in_ilam,
            "success": self.success,
            "error": self.error
        }


class RateLimiter:
    """Token bucket rate limiter for API calls."""
    
    def __init__(self, max_requests: int, time_window: int = 60):
        self.max_requests = max_requests
        self.time_window = time_window
        self.tokens = max_requests
        self.last_update = time.time()
        self.lock = Lock()
    
    def acquire(self) -> bool:
        """Try to acquire a token. Returns True if successful."""
        with self.lock:
            now = time.time()
            elapsed = now - self.last_update
            
            # Add tokens based on elapsed time
            self.tokens = min(
                self.max_requests,
                self.tokens + (elapsed * self.max_requests / self.time_window)
            )
            self.last_update = now
            
            if self.tokens >= 1:
                self.tokens -= 1
                return True
            return False
    
    def get_wait_time(self) -> float:
        """Get time to wait before next token is available."""
        with self.lock:
            if self.tokens >= 1:
                return 0.0
            return (1 - self.tokens) * self.time_window / self.max_requests


class GeolocationService:
    """
    Multi-provider geolocation service with caching and rate limiting.
    Primary: ip-api.com (free, 45 req/min limit)
    Fallback: ipinfo.io (requires token)
    """
    
    def __init__(self):
        self.config = get_config_manager().get()
        self.db = get_db_manager()
        self.ilam_config = self.config.ilam_region
        
        # Rate limiter for ip-api.com (45 requests per minute)
        self.rate_limiter = RateLimiter(
            self.config.geolocation.rate_limit_per_minute,
            time_window=60
        )
        
        self.session = requests.Session()
        self.session.headers.update({
            'User-Agent': 'IlamMinerDetector/1.0 (Security Research Tool)'
        })
    
    def lookup(self, ip_address: str, use_cache: bool = True) -> GeolocationResult:
        """
        Lookup geolocation for an IP address.
        
        Args:
            ip_address: IP to lookup
            use_cache: Whether to use cached results
            
        Returns:
            GeolocationResult with location data
        """
        result = GeolocationResult(ip_address=ip_address)
        
        # Validate IP
        if ip_address in ['127.0.0.1', 'localhost', '0.0.0.0']:
            result.error = "Cannot geolocate local IP"
            return result
        
        # Check cache first
        if use_cache and self.config.geolocation.cache_enabled:
            cached = self.db.get_geolocation(ip_address)
            if cached:
                result = self._cache_to_result(cached)
                result.success = True
                logger.debug(f"Cache hit for {ip_address}")
                return result
        
        # Rate limit check
        if not self.rate_limiter.acquire():
            wait_time = self.rate_limiter.get_wait_time()
            logger.warning(f"Rate limit hit, waiting {wait_time:.1f}s")
            time.sleep(wait_time)
        
        # Try primary provider
        try:
            result = self._lookup_ipapi(ip_address)
            if result.success:
                self._cache_result(result)
                return result
        except Exception as e:
            logger.warning(f"ip-api.com failed for {ip_address}: {e}")
        
        # Try fallback if available
        if self.config.geolocation.ipinfo_token:
            try:
                result = self._lookup_ipinfo(ip_address)
                if result.success:
                    self._cache_result(result)
                    return result
            except Exception as e:
                logger.warning(f"ipinfo.io failed for {ip_address}: {e}")
        
        result.error = "All geolocation providers failed"
        return result
    
    def lookup_batch(self, ip_addresses: list, progress_callback=None) -> Dict[str, GeolocationResult]:
        """
        Lookup geolocation for multiple IPs with rate limiting.
        
        Args:
            ip_addresses: List of IP addresses
            progress_callback: Optional callback(current, total)
            
        Returns:
            Dictionary mapping IP to GeolocationResult
        """
        results = {}
        total = len(ip_addresses)
        
        for i, ip in enumerate(ip_addresses):
            results[ip] = self.lookup(ip)
            
            if progress_callback:
                progress_callback(i + 1, total)
            
            # Small delay to respect rate limits
            if (i + 1) % self.config.geolocation.rate_limit_per_minute == 0:
                logger.info("Rate limit approached, pausing for 60 seconds...")
                time.sleep(60)
        
        return results
    
    def _lookup_ipapi(self, ip_address: str) -> GeolocationResult:
        """Lookup using ip-api.com (free, no API key)."""
        result = GeolocationResult(ip_address=ip_address)
        
        url = f"http://ip-api.com/json/{ip_address}"
        
        try:
            response = self.session.get(
                url,
                timeout=self.config.geolocation.request_timeout
            )
            response.raise_for_status()
            data = response.json()
            
            if data.get('status') == 'success':
                result.country = data.get('country', '')
                result.country_code = data.get('countryCode', '')
                result.region = data.get('regionName', '')
                result.city = data.get('city', '')
                result.latitude = data.get('lat', 0.0)
                result.longitude = data.get('lon', 0.0)
                result.isp = data.get('isp', '')
                result.org = data.get('org', '')
                result.success = True
                result.is_in_ilam = self._check_ilam_region(result.latitude, result.longitude)
            else:
                result.error = data.get('message', 'Unknown error')
                
        except requests.RequestException as e:
            result.error = f"Request failed: {str(e)}"
        
        return result
    
    def _lookup_ipinfo(self, ip_address: str) -> GeolocationResult:
        """Lookup using ipinfo.io (requires token)."""
        result = GeolocationResult(ip_address=ip_address)
        
        token = self.config.geolocation.ipinfo_token
        if not token:
            result.error = "No ipinfo.io token configured"
            return result
        
        url = f"https://ipinfo.io/{ip_address}/json"
        headers = {"Authorization": f"Bearer {token}"}
        
        try:
            response = self.session.get(
                url,
                headers=headers,
                timeout=self.config.geolocation.request_timeout
            )
            response.raise_for_status()
            data = response.json()
            
            result.country = data.get('country', '')
            result.country_code = data.get('country', '')
            result.region = data.get('region', '')
            result.city = data.get('city', '')
            result.isp = data.get('org', '')
            result.org = data.get('org', '')
            
            # Parse loc "lat,lon"
            loc = data.get('loc', '').split(',')
            if len(loc) == 2:
                result.latitude = float(loc[0])
                result.longitude = float(loc[1])
                result.is_in_ilam = self._check_ilam_region(result.latitude, result.longitude)
            
            result.success = True
            
        except requests.RequestException as e:
            result.error = f"Request failed: {str(e)}"
        except (KeyError, ValueError) as e:
            result.error = f"Parse error: {str(e)}"
        
        return result
    
    def _check_ilam_region(self, latitude: float, longitude: float) -> bool:
        """Check if coordinates are within Ilam province bounds."""
        return (self.ilam_config.min_latitude <= latitude <= self.ilam_config.max_latitude and
                self.ilam_config.min_longitude <= longitude <= self.ilam_config.max_longitude)
    
    def _cache_result(self, result: GeolocationResult) -> None:
        """Save result to cache."""
        if not self.config.geolocation.cache_enabled:
            return
        
        cache_entry = GeolocationCache(
            ip_address=result.ip_address,
            country=result.country,
            country_code=result.country_code,
            region=result.region,
            city=result.city,
            latitude=result.latitude,
            longitude=result.longitude,
            isp=result.isp,
            org=result.org,
            ttl_hours=self.config.geolocation.cache_ttl_hours
        )
        
        try:
            self.db.save_geolocation(cache_entry)
        except Exception as e:
            logger.warning(f"Failed to cache geolocation: {e}")
    
    def _cache_to_result(self, cache: GeolocationCache) -> GeolocationResult:
        """Convert cache entry to result."""
        return GeolocationResult(
            ip_address=cache.ip_address,
            country=cache.country,
            country_code=cache.country_code,
            region=cache.region,
            city=cache.city,
            latitude=cache.latitude,
            longitude=cache.longitude,
            isp=cache.isp,
            org=cache.org,
            is_in_ilam=self._check_ilam_region(cache.latitude, cache.longitude),
            success=True
        )
    
    def clean_cache(self) -> int:
        """Clean expired cache entries."""
        return self.db.clean_expired_geolocation()
    
    def get_cache_stats(self) -> Dict[str, Any]:
        """Get cache statistics."""
        db_stats = self.db.get_stats()
        return {
            "cache_entries": db_stats.get("geolocation_cache_entries", 0),
            "rate_limit_tokens": self.rate_limiter.tokens
        }


def get_geolocation_service() -> GeolocationService:
    """Get geolocation service instance."""
    return GeolocationService()
