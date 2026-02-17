"""
Database operations for Ilam Miner Detector.
Handles SQLite storage for scans, hosts, and geolocation cache.
"""

import sqlite3
import threading
from pathlib import Path
from typing import Optional, List, Dict, Any
from datetime import datetime
from contextlib import contextmanager
import json


class Database:
    """SQLite database manager with connection pooling."""
    
    SCHEMA_VERSION = 1
    
    SCHEMA_SQL = """
    CREATE TABLE IF NOT EXISTS schema_version (
        version INTEGER PRIMARY KEY,
        applied_at TEXT NOT NULL
    );
    
    CREATE TABLE IF NOT EXISTS scans (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        scan_name TEXT NOT NULL,
        started_at TEXT NOT NULL,
        completed_at TEXT,
        status TEXT NOT NULL,
        target_range TEXT NOT NULL,
        total_ips INTEGER DEFAULT 0,
        scanned_ips INTEGER DEFAULT 0,
        detected_miners INTEGER DEFAULT 0,
        config_json TEXT
    );
    
    CREATE TABLE IF NOT EXISTS hosts (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        scan_id INTEGER NOT NULL,
        ip_address TEXT NOT NULL,
        hostname TEXT,
        is_reachable INTEGER DEFAULT 0,
        open_ports TEXT,
        detected_services TEXT,
        is_miner INTEGER DEFAULT 0,
        miner_type TEXT,
        banner_info TEXT,
        discovered_at TEXT NOT NULL,
        FOREIGN KEY (scan_id) REFERENCES scans(id) ON DELETE CASCADE
    );
    
    CREATE TABLE IF NOT EXISTS geolocation_cache (
        ip_address TEXT PRIMARY KEY,
        country TEXT,
        country_code TEXT,
        region TEXT,
        region_code TEXT,
        city TEXT,
        latitude REAL,
        longitude REAL,
        isp TEXT,
        org TEXT,
        as_number TEXT,
        cached_at TEXT NOT NULL,
        source TEXT NOT NULL
    );
    
    CREATE INDEX IF NOT EXISTS idx_hosts_scan_id ON hosts(scan_id);
    CREATE INDEX IF NOT EXISTS idx_hosts_ip_address ON hosts(ip_address);
    CREATE INDEX IF NOT EXISTS idx_hosts_is_miner ON hosts(is_miner);
    CREATE INDEX IF NOT EXISTS idx_scans_started_at ON scans(started_at);
    CREATE INDEX IF NOT EXISTS idx_geolocation_region ON geolocation_cache(region);
    """
    
    def __init__(self, db_path: str):
        """
        Initialize database connection.
        
        Args:
            db_path: Path to SQLite database file
        """
        self.db_path = db_path
        self._local = threading.local()
        self._ensure_database()
    
    def _ensure_database(self):
        """Create database file and schema if they don't exist."""
        Path(self.db_path).parent.mkdir(parents=True, exist_ok=True)
        
        with self._get_connection() as conn:
            cursor = conn.cursor()
            
            # Execute schema
            cursor.executescript(self.SCHEMA_SQL)
            
            # Check/update schema version
            cursor.execute("SELECT version FROM schema_version ORDER BY version DESC LIMIT 1")
            row = cursor.fetchone()
            current_version = row[0] if row else 0
            
            if current_version < self.SCHEMA_VERSION:
                cursor.execute(
                    "INSERT INTO schema_version (version, applied_at) VALUES (?, ?)",
                    (self.SCHEMA_VERSION, datetime.utcnow().isoformat())
                )
            
            conn.commit()
    
    @contextmanager
    def _get_connection(self):
        """Get thread-local database connection."""
        if not hasattr(self._local, 'conn'):
            self._local.conn = sqlite3.connect(self.db_path, check_same_thread=False)
            self._local.conn.row_factory = sqlite3.Row
            # Enable WAL mode for better concurrent access
            self._local.conn.execute("PRAGMA journal_mode=WAL")
        
        yield self._local.conn
    
    def create_scan(self, scan_name: str, target_range: str, config: Dict[str, Any]) -> int:
        """
        Create a new scan record.
        
        Args:
            scan_name: Human-readable scan name
            target_range: CIDR or IP range being scanned
            config: Scan configuration dictionary
            
        Returns:
            Scan ID
        """
        with self._get_connection() as conn:
            cursor = conn.cursor()
            cursor.execute(
                """INSERT INTO scans (scan_name, started_at, status, target_range, config_json)
                   VALUES (?, ?, ?, ?, ?)""",
                (scan_name, datetime.utcnow().isoformat(), 'running', target_range, json.dumps(config))
            )
            conn.commit()
            return cursor.lastrowid
    
    def update_scan_progress(self, scan_id: int, scanned_ips: int, detected_miners: int):
        """Update scan progress counters."""
        with self._get_connection() as conn:
            cursor = conn.cursor()
            cursor.execute(
                """UPDATE scans SET scanned_ips = ?, detected_miners = ? WHERE id = ?""",
                (scanned_ips, detected_miners, scan_id)
            )
            conn.commit()
    
    def complete_scan(self, scan_id: int, status: str = 'completed'):
        """Mark scan as completed."""
        with self._get_connection() as conn:
            cursor = conn.cursor()
            cursor.execute(
                """UPDATE scans SET status = ?, completed_at = ? WHERE id = ?""",
                (status, datetime.utcnow().isoformat(), scan_id)
            )
            conn.commit()
    
    def add_host(self, scan_id: int, ip_address: str, hostname: Optional[str] = None,
                 is_reachable: bool = False, open_ports: Optional[List[int]] = None,
                 detected_services: Optional[Dict[str, str]] = None, is_miner: bool = False,
                 miner_type: Optional[str] = None, banner_info: Optional[str] = None) -> int:
        """
        Add a discovered host to the database.
        
        Args:
            scan_id: Parent scan ID
            ip_address: Host IP address
            hostname: Resolved hostname (optional)
            is_reachable: Whether host responded to ping
            open_ports: List of open TCP ports
            detected_services: Dictionary of port -> service name
            is_miner: Whether host is identified as a miner
            miner_type: Type of miner detected (e.g., 'stratum', 'bitcoin')
            banner_info: Raw banner data grabbed from service
            
        Returns:
            Host record ID
        """
        with self._get_connection() as conn:
            cursor = conn.cursor()
            cursor.execute(
                """INSERT INTO hosts 
                   (scan_id, ip_address, hostname, is_reachable, open_ports, detected_services,
                    is_miner, miner_type, banner_info, discovered_at)
                   VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)""",
                (
                    scan_id,
                    ip_address,
                    hostname,
                    int(is_reachable),
                    json.dumps(open_ports or []),
                    json.dumps(detected_services or {}),
                    int(is_miner),
                    miner_type,
                    banner_info,
                    datetime.utcnow().isoformat()
                )
            )
            conn.commit()
            return cursor.lastrowid
    
    def cache_geolocation(self, ip_address: str, geo_data: Dict[str, Any], source: str):
        """
        Cache geolocation data for an IP address.
        
        Args:
            ip_address: Target IP
            geo_data: Geolocation information dictionary
            source: Provider name (e.g., 'ip-api', 'ipinfo')
        """
        with self._get_connection() as conn:
            cursor = conn.cursor()
            cursor.execute(
                """INSERT OR REPLACE INTO geolocation_cache
                   (ip_address, country, country_code, region, region_code, city,
                    latitude, longitude, isp, org, as_number, cached_at, source)
                   VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)""",
                (
                    ip_address,
                    geo_data.get('country'),
                    geo_data.get('countryCode'),
                    geo_data.get('regionName') or geo_data.get('region'),
                    geo_data.get('region'),
                    geo_data.get('city'),
                    geo_data.get('lat'),
                    geo_data.get('lon'),
                    geo_data.get('isp'),
                    geo_data.get('org'),
                    geo_data.get('as'),
                    datetime.utcnow().isoformat(),
                    source
                )
            )
            conn.commit()
    
    def get_cached_geolocation(self, ip_address: str) -> Optional[Dict[str, Any]]:
        """Retrieve cached geolocation data for an IP."""
        with self._get_connection() as conn:
            cursor = conn.cursor()
            cursor.execute(
                "SELECT * FROM geolocation_cache WHERE ip_address = ?",
                (ip_address,)
            )
            row = cursor.fetchone()
            return dict(row) if row else None
    
    def get_scan(self, scan_id: int) -> Optional[Dict[str, Any]]:
        """Retrieve scan record by ID."""
        with self._get_connection() as conn:
            cursor = conn.cursor()
            cursor.execute("SELECT * FROM scans WHERE id = ?", (scan_id,))
            row = cursor.fetchone()
            return dict(row) if row else None
    
    def get_scan_hosts(self, scan_id: int, miners_only: bool = False) -> List[Dict[str, Any]]:
        """
        Retrieve all hosts discovered in a scan.
        
        Args:
            scan_id: Scan ID
            miners_only: If True, return only hosts identified as miners
            
        Returns:
            List of host dictionaries
        """
        with self._get_connection() as conn:
            cursor = conn.cursor()
            if miners_only:
                cursor.execute(
                    "SELECT * FROM hosts WHERE scan_id = ? AND is_miner = 1 ORDER BY discovered_at",
                    (scan_id,)
                )
            else:
                cursor.execute(
                    "SELECT * FROM hosts WHERE scan_id = ? ORDER BY discovered_at",
                    (scan_id,)
                )
            return [dict(row) for row in cursor.fetchall()]
    
    def get_recent_scans(self, limit: int = 20) -> List[Dict[str, Any]]:
        """Retrieve most recent scans."""
        with self._get_connection() as conn:
            cursor = conn.cursor()
            cursor.execute(
                "SELECT * FROM scans ORDER BY started_at DESC LIMIT ?",
                (limit,)
            )
            return [dict(row) for row in cursor.fetchall()]
    
    def close(self):
        """Close database connection."""
        if hasattr(self._local, 'conn'):
            self._local.conn.close()
            delattr(self._local, 'conn')
