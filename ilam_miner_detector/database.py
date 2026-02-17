"""
Database management for Ilam Miner Detector.
SQLite operations with connection pooling and ORM-style models.
"""

import sqlite3
import json
import logging
from datetime import datetime, timedelta
from dataclasses import dataclass, asdict
from typing import List, Optional, Dict, Any, Tuple
from pathlib import Path
from contextlib import contextmanager
import threading

from .config_manager import get_config_manager

logger = logging.getLogger(__name__)


@dataclass
class ScanRecord:
    """Represents a scan operation record."""
    id: Optional[int] = None
    scan_name: str = ""
    cidr_range: str = ""
    start_time: Optional[datetime] = None
    end_time: Optional[datetime] = None
    total_hosts: int = 0
    responsive_hosts: int = 0
    miners_detected: int = 0
    status: str = "pending"  # pending, running, completed, cancelled, error
    notes: str = ""
    
    def to_dict(self) -> Dict[str, Any]:
        result = asdict(self)
        if self.start_time:
            result['start_time'] = self.start_time.isoformat()
        if self.end_time:
            result['end_time'] = self.end_time.isoformat()
        return result


@dataclass
class HostRecord:
    """Represents a scanned host record."""
    id: Optional[int] = None
    scan_id: int = 0
    ip_address: str = ""
    is_responsive: bool = False
    ping_time_ms: Optional[float] = None
    open_ports: str = ""  # JSON array
    banner_info: str = ""  # JSON object
    is_miner_detected: bool = False
    miner_type: str = ""
    confidence_score: float = 0.0
    timestamp: Optional[datetime] = None
    
    def to_dict(self) -> Dict[str, Any]:
        result = asdict(self)
        if self.timestamp:
            result['timestamp'] = self.timestamp.isoformat()
        return result


@dataclass
class GeolocationCache:
    """Cached geolocation data for an IP."""
    id: Optional[int] = None
    ip_address: str = ""
    country: str = ""
    country_code: str = ""
    region: str = ""
    city: str = ""
    latitude: float = 0.0
    longitude: float = 0.0
    isp: str = ""
    org: str = ""
    cached_at: Optional[datetime] = None
    ttl_hours: int = 24
    
    @property
    def is_expired(self) -> bool:
        if not self.cached_at:
            return True
        expiry = self.cached_at + timedelta(hours=self.ttl_hours)
        return datetime.now() > expiry
    
    def to_dict(self) -> Dict[str, Any]:
        result = asdict(self)
        if self.cached_at:
            result['cached_at'] = self.cached_at.isoformat()
        return result


class DatabaseManager:
    """
    Manages SQLite database connections and operations.
    Thread-safe singleton pattern with connection pooling.
    """
    
    _instance: Optional['DatabaseManager'] = None
    _lock = threading.Lock()
    
    def __new__(cls, db_path: Optional[str] = None) -> 'DatabaseManager':
        if cls._instance is None:
            with cls._lock:
                if cls._instance is None:
                    cls._instance = super().__new__(cls)
                    cls._instance._initialized = False
        return cls._instance
    
    def __init__(self, db_path: Optional[str] = None):
        if self._initialized:
            return
            
        config = get_config_manager().get()
        self.db_path = db_path or config.database.db_path
        self._local = threading.local()
        
        # Ensure data directory exists
        Path(self.db_path).parent.mkdir(parents=True, exist_ok=True)
        
        # Initialize database
        self._init_database()
        self._initialized = True
        logger.info(f"Database initialized at {self.db_path}")
    
    def _get_connection(self) -> sqlite3.Connection:
        """Get thread-local database connection."""
        if not hasattr(self._local, 'connection') or self._local.connection is None:
            self._local.connection = sqlite3.connect(
                self.db_path,
                timeout=get_config_manager().get().database.connection_timeout,
                check_same_thread=False
            )
            self._local.connection.row_factory = sqlite3.Row
        return self._local.connection
    
    @contextmanager
    def _get_cursor(self):
        """Context manager for database cursor."""
        conn = self._get_connection()
        cursor = conn.cursor()
        try:
            yield cursor
            conn.commit()
        except Exception as e:
            conn.rollback()
            raise e
    
    def _init_database(self) -> None:
        """Initialize database schema."""
        with self._get_cursor() as cursor:
            # Scans table
            cursor.execute("""
                CREATE TABLE IF NOT EXISTS scans (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    scan_name TEXT NOT NULL,
                    cidr_range TEXT NOT NULL,
                    start_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    end_time TIMESTAMP,
                    total_hosts INTEGER DEFAULT 0,
                    responsive_hosts INTEGER DEFAULT 0,
                    miners_detected INTEGER DEFAULT 0,
                    status TEXT DEFAULT 'pending',
                    notes TEXT DEFAULT ''
                )
            """)
            
            # Hosts table
            cursor.execute("""
                CREATE TABLE IF NOT EXISTS hosts (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    scan_id INTEGER NOT NULL,
                    ip_address TEXT NOT NULL,
                    is_responsive BOOLEAN DEFAULT 0,
                    ping_time_ms REAL,
                    open_ports TEXT DEFAULT '[]',
                    banner_info TEXT DEFAULT '{}',
                    is_miner_detected BOOLEAN DEFAULT 0,
                    miner_type TEXT DEFAULT '',
                    confidence_score REAL DEFAULT 0.0,
                    timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (scan_id) REFERENCES scans(id) ON DELETE CASCADE
                )
            """)
            
            # Geolocation cache table
            cursor.execute("""
                CREATE TABLE IF NOT EXISTS geolocation_cache (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ip_address TEXT UNIQUE NOT NULL,
                    country TEXT DEFAULT '',
                    country_code TEXT DEFAULT '',
                    region TEXT DEFAULT '',
                    city TEXT DEFAULT '',
                    latitude REAL DEFAULT 0.0,
                    longitude REAL DEFAULT 0.0,
                    isp TEXT DEFAULT '',
                    org TEXT DEFAULT '',
                    cached_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    ttl_hours INTEGER DEFAULT 24
                )
            """)
            
            # Create indexes
            cursor.execute("CREATE INDEX IF NOT EXISTS idx_hosts_scan_id ON hosts(scan_id)")
            cursor.execute("CREATE INDEX IF NOT EXISTS idx_hosts_ip ON hosts(ip_address)")
            cursor.execute("CREATE INDEX IF NOT EXISTS idx_hosts_miner ON hosts(is_miner_detected)")
            cursor.execute("CREATE INDEX IF NOT EXISTS idx_geo_ip ON geolocation_cache(ip_address)")
            cursor.execute("CREATE INDEX IF NOT EXISTS idx_scans_time ON scans(start_time)")
    
    # Scan operations
    def create_scan(self, scan_name: str, cidr_range: str, notes: str = "") -> int:
        """Create a new scan record and return its ID."""
        with self._get_cursor() as cursor:
            cursor.execute(
                """INSERT INTO scans (scan_name, cidr_range, status, notes) 
                   VALUES (?, ?, 'pending', ?)""",
                (scan_name, cidr_range, notes)
            )
            return cursor.lastrowid
    
    def update_scan_status(self, scan_id: int, status: str) -> None:
        """Update scan status."""
        with self._get_cursor() as cursor:
            if status in ['completed', 'cancelled', 'error']:
                cursor.execute(
                    "UPDATE scans SET status = ?, end_time = CURRENT_TIMESTAMP WHERE id = ?",
                    (status, scan_id)
                )
            else:
                cursor.execute(
                    "UPDATE scans SET status = ? WHERE id = ?",
                    (status, scan_id)
                )
    
    def update_scan_stats(self, scan_id: int, total_hosts: int = None,
                         responsive_hosts: int = None, miners_detected: int = None) -> None:
        """Update scan statistics."""
        with self._get_cursor() as cursor:
            updates = []
            params = []
            if total_hosts is not None:
                updates.append("total_hosts = ?")
                params.append(total_hosts)
            if responsive_hosts is not None:
                updates.append("responsive_hosts = ?")
                params.append(responsive_hosts)
            if miners_detected is not None:
                updates.append("miners_detected = ?")
                params.append(miners_detected)
            
            if updates:
                query = f"UPDATE scans SET {', '.join(updates)} WHERE id = ?"
                params.append(scan_id)
                cursor.execute(query, params)
    
    def get_scan(self, scan_id: int) -> Optional[ScanRecord]:
        """Get scan record by ID."""
        with self._get_cursor() as cursor:
            cursor.execute("SELECT * FROM scans WHERE id = ?", (scan_id,))
            row = cursor.fetchone()
            if row:
                return self._row_to_scan_record(row)
            return None
    
    def get_all_scans(self, limit: int = 100) -> List[ScanRecord]:
        """Get all scan records."""
        with self._get_cursor() as cursor:
            cursor.execute(
                "SELECT * FROM scans ORDER BY start_time DESC LIMIT ?",
                (limit,)
            )
            return [self._row_to_scan_record(row) for row in cursor.fetchall()]
    
    # Host operations
    def add_host(self, host: HostRecord) -> int:
        """Add a host record."""
        with self._get_cursor() as cursor:
            cursor.execute(
                """INSERT INTO hosts 
                   (scan_id, ip_address, is_responsive, ping_time_ms, open_ports,
                    banner_info, is_miner_detected, miner_type, confidence_score)
                   VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)""",
                (host.scan_id, host.ip_address, host.is_responsive,
                 host.ping_time_ms, host.open_ports, host.banner_info,
                 host.is_miner_detected, host.miner_type, host.confidence_score)
            )
            return cursor.lastrowid
    
    def get_hosts_by_scan(self, scan_id: int, miners_only: bool = False) -> List[HostRecord]:
        """Get hosts for a scan."""
        with self._get_cursor() as cursor:
            if miners_only:
                cursor.execute(
                    "SELECT * FROM hosts WHERE scan_id = ? AND is_miner_detected = 1",
                    (scan_id,)
                )
            else:
                cursor.execute(
                    "SELECT * FROM hosts WHERE scan_id = ?",
                    (scan_id,)
                )
            return [self._row_to_host_record(row) for row in cursor.fetchall()]
    
    def get_miner_hosts(self, scan_id: Optional[int] = None) -> List[HostRecord]:
        """Get all hosts detected as miners."""
        with self._get_cursor() as cursor:
            if scan_id:
                cursor.execute(
                    """SELECT h.* FROM hosts h 
                       WHERE h.scan_id = ? AND h.is_miner_detected = 1""",
                    (scan_id,)
                )
            else:
                cursor.execute(
                    "SELECT * FROM hosts WHERE is_miner_detected = 1"
                )
            return [self._row_to_host_record(row) for row in cursor.fetchall()]
    
    # Geolocation cache operations
    def get_geolocation(self, ip_address: str) -> Optional[GeolocationCache]:
        """Get cached geolocation for IP."""
        with self._get_cursor() as cursor:
            cursor.execute(
                "SELECT * FROM geolocation_cache WHERE ip_address = ?",
                (ip_address,)
            )
            row = cursor.fetchone()
            if row:
                geo = self._row_to_geolocation_cache(row)
                if not geo.is_expired:
                    return geo
            return None
    
    def save_geolocation(self, geo: GeolocationCache) -> None:
        """Save or update geolocation cache."""
        with self._get_cursor() as cursor:
            cursor.execute(
                """INSERT OR REPLACE INTO geolocation_cache 
                   (ip_address, country, country_code, region, city, latitude, 
                    longitude, isp, org, cached_at, ttl_hours)
                   VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, CURRENT_TIMESTAMP, ?)""",
                (geo.ip_address, geo.country, geo.country_code, geo.region,
                 geo.city, geo.latitude, geo.longitude, geo.isp, geo.org, geo.ttl_hours)
            )
    
    def clean_expired_geolocation(self) -> int:
        """Remove expired geolocation entries. Returns count removed."""
        with self._get_cursor() as cursor:
            cursor.execute(
                """DELETE FROM geolocation_cache 
                   WHERE datetime(cached_at, '+' || ttl_hours || ' hours') < datetime('now')"""
            )
            return cursor.rowcount
    
    # Utility methods
    def delete_scan(self, scan_id: int) -> None:
        """Delete a scan and all associated hosts."""
        with self._get_cursor() as cursor:
            cursor.execute("DELETE FROM scans WHERE id = ?", (scan_id,))
    
    def get_stats(self) -> Dict[str, Any]:
        """Get database statistics."""
        with self._get_cursor() as cursor:
            cursor.execute("SELECT COUNT(*) FROM scans")
            total_scans = cursor.fetchone()[0]
            
            cursor.execute("SELECT COUNT(*) FROM hosts")
            total_hosts = cursor.fetchone()[0]
            
            cursor.execute("SELECT COUNT(*) FROM hosts WHERE is_miner_detected = 1")
            total_miners = cursor.fetchone()[0]
            
            cursor.execute("SELECT COUNT(*) FROM geolocation_cache")
            cache_entries = cursor.fetchone()[0]
            
            return {
                "total_scans": total_scans,
                "total_hosts": total_hosts,
                "total_miners": total_miners,
                "geolocation_cache_entries": cache_entries
            }
    
    # Row converters
    def _row_to_scan_record(self, row: sqlite3.Row) -> ScanRecord:
        return ScanRecord(
            id=row['id'],
            scan_name=row['scan_name'],
            cidr_range=row['cidr_range'],
            start_time=datetime.fromisoformat(row['start_time']) if row['start_time'] else None,
            end_time=datetime.fromisoformat(row['end_time']) if row['end_time'] else None,
            total_hosts=row['total_hosts'],
            responsive_hosts=row['responsive_hosts'],
            miners_detected=row['miners_detected'],
            status=row['status'],
            notes=row['notes']
        )
    
    def _row_to_host_record(self, row: sqlite3.Row) -> HostRecord:
        return HostRecord(
            id=row['id'],
            scan_id=row['scan_id'],
            ip_address=row['ip_address'],
            is_responsive=bool(row['is_responsive']),
            ping_time_ms=row['ping_time_ms'],
            open_ports=row['open_ports'],
            banner_info=row['banner_info'],
            is_miner_detected=bool(row['is_miner_detected']),
            miner_type=row['miner_type'],
            confidence_score=row['confidence_score'],
            timestamp=datetime.fromisoformat(row['timestamp']) if row['timestamp'] else None
        )
    
    def _row_to_geolocation_cache(self, row: sqlite3.Row) -> GeolocationCache:
        return GeolocationCache(
            id=row['id'],
            ip_address=row['ip_address'],
            country=row['country'],
            country_code=row['country_code'],
            region=row['region'],
            city=row['city'],
            latitude=row['latitude'],
            longitude=row['longitude'],
            isp=row['isp'],
            org=row['org'],
            cached_at=datetime.fromisoformat(row['cached_at']) if row['cached_at'] else None,
            ttl_hours=row['ttl_hours']
        )


def get_db_manager() -> DatabaseManager:
    """Get database manager instance."""
    return DatabaseManager()
