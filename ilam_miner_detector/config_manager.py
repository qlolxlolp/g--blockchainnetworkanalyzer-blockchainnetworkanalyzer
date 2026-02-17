"""
Configuration management for Ilam Miner Detector.
Provides JSON-based configuration with schema validation.
"""

import json
import os
from dataclasses import dataclass, field, asdict
from typing import Dict, List, Optional, Any
from pathlib import Path


@dataclass
class ScanConfig:
    """Scanning configuration settings."""
    timeout: int = 3
    concurrency: int = 50
    ping_timeout: float = 1.0
    tcp_timeout: float = 3.0
    max_retries: int = 2
    retry_delay: float = 1.0
    enable_ping: bool = True
    enable_banner_grab: bool = True
    banner_timeout: float = 5.0


@dataclass
class GeolocationConfig:
    """Geolocation service configuration."""
    primary_provider: str = "ip-api.com"
    fallback_provider: str = "ipinfo.io"
    ipinfo_token: str = ""
    rate_limit_per_minute: int = 45
    cache_enabled: bool = True
    cache_ttl_hours: int = 24
    request_timeout: int = 10


@dataclass
class IlamRegionConfig:
    """Ilam province region boundaries for filtering."""
    min_latitude: float = 32.5
    max_latitude: float = 33.5
    min_longitude: float = 46.0
    max_longitude: float = 47.5
    province_name: str = "Ilam"
    country_code: str = "IR"


@dataclass
class MinerPortsConfig:
    """Common cryptocurrency mining ports to scan."""
    stratum_ports: List[int] = field(default_factory=lambda: [3333, 4444, 4028, 7777, 14433, 14444])
    bitcoin_ports: List[int] = field(default_factory=lambda: [8332, 8333])
    ethereum_ports: List[int] = field(default_factory=lambda: [8545, 30303])
    generic_ports: List[int] = field(default_factory=lambda: [8080, 8081])
    
    @property
    def all_ports(self) -> List[int]:
        """Get all ports combined and sorted."""
        all_p = set(self.stratum_ports + self.bitcoin_ports + 
                   self.ethereum_ports + self.generic_ports)
        return sorted(list(all_p))


@dataclass
class DatabaseConfig:
    """Database configuration."""
    db_path: str = "data/ilam_miner_detector.db"
    connection_timeout: int = 30
    max_connections: int = 10


@dataclass
class ReportingConfig:
    """Reporting and export configuration."""
    reports_dir: str = "reports"
    auto_export: bool = True
    export_formats: List[str] = field(default_factory=lambda: ["json", "html"])
    include_map: bool = True
    timestamp_format: str = "%Y%m%d_%H%M%S"


@dataclass
class AppConfig:
    """Main application configuration container."""
    scan: ScanConfig = field(default_factory=ScanConfig)
    geolocation: GeolocationConfig = field(default_factory=GeolocationConfig)
    ilam_region: IlamRegionConfig = field(default_factory=IlamRegionConfig)
    miner_ports: MinerPortsConfig = field(default_factory=MinerPortsConfig)
    database: DatabaseConfig = field(default_factory=DatabaseConfig)
    reporting: ReportingConfig = field(default_factory=ReportingConfig)
    log_level: str = "INFO"
    log_file: str = "data/ilam_miner_detector.log"


class ConfigManager:
    """
    Manages application configuration with load/save capabilities.
    """
    
    DEFAULT_CONFIG = {
        "scan": {
            "timeout": 3,
            "concurrency": 50,
            "ping_timeout": 1.0,
            "tcp_timeout": 3.0,
            "max_retries": 2,
            "retry_delay": 1.0,
            "enable_ping": True,
            "enable_banner_grab": True,
            "banner_timeout": 5.0
        },
        "geolocation": {
            "primary_provider": "ip-api.com",
            "fallback_provider": "ipinfo.io",
            "ipinfo_token": "",
            "rate_limit_per_minute": 45,
            "cache_enabled": True,
            "cache_ttl_hours": 24,
            "request_timeout": 10
        },
        "ilam_region": {
            "min_latitude": 32.5,
            "max_latitude": 33.5,
            "min_longitude": 46.0,
            "max_longitude": 47.5,
            "province_name": "Ilam",
            "country_code": "IR"
        },
        "miner_ports": {
            "stratum_ports": [3333, 4444, 4028, 7777, 14433, 14444],
            "bitcoin_ports": [8332, 8333],
            "ethereum_ports": [8545, 30303],
            "generic_ports": [8080, 8081]
        },
        "database": {
            "db_path": "data/ilam_miner_detector.db",
            "connection_timeout": 30,
            "max_connections": 10
        },
        "reporting": {
            "reports_dir": "reports",
            "auto_export": True,
            "export_formats": ["json", "html"],
            "include_map": True,
            "timestamp_format": "%Y%m%d_%H%M%S"
        },
        "log_level": "INFO",
        "log_file": "data/ilam_miner_detector.log"
    }
    
    def __init__(self, config_path: str = "config/config.json"):
        self.config_path = Path(config_path)
        self._config: Optional[AppConfig] = None
        
    def load(self) -> AppConfig:
        """Load configuration from file or create default."""
        if self._config is not None:
            return self._config
            
        if self.config_path.exists():
            try:
                with open(self.config_path, 'r') as f:
                    data = json.load(f)
                self._config = self._dict_to_config(data)
            except (json.JSONDecodeError, KeyError, TypeError) as e:
                print(f"Warning: Failed to load config: {e}. Using defaults.")
                self._config = self._create_default_config()
                self.save()
        else:
            self._config = self._create_default_config()
            self.save()
            
        return self._config
    
    def save(self) -> None:
        """Save current configuration to file."""
        if self._config is None:
            self._config = self._create_default_config()
            
        self.config_path.parent.mkdir(parents=True, exist_ok=True)
        
        data = self._config_to_dict(self._config)
        with open(self.config_path, 'w') as f:
            json.dump(data, f, indent=2)
    
    def get(self) -> AppConfig:
        """Get current configuration."""
        if self._config is None:
            return self.load()
        return self._config
    
    def _create_default_config(self) -> AppConfig:
        """Create default configuration."""
        return AppConfig()
    
    def _dict_to_config(self, data: Dict[str, Any]) -> AppConfig:
        """Convert dictionary to AppConfig."""
        return AppConfig(
            scan=ScanConfig(**data.get("scan", {})),
            geolocation=GeolocationConfig(**data.get("geolocation", {})),
            ilam_region=IlamRegionConfig(**data.get("ilam_region", {})),
            miner_ports=MinerPortsConfig(**data.get("miner_ports", {})),
            database=DatabaseConfig(**data.get("database", {})),
            reporting=ReportingConfig(**data.get("reporting", {})),
            log_level=data.get("log_level", "INFO"),
            log_file=data.get("log_file", "data/ilam_miner_detector.log")
        )
    
    def _config_to_dict(self, config: AppConfig) -> Dict[str, Any]:
        """Convert AppConfig to dictionary."""
        return {
            "scan": asdict(config.scan),
            "geolocation": asdict(config.geolocation),
            "ilam_region": asdict(config.ilam_region),
            "miner_ports": asdict(config.miner_ports),
            "database": asdict(config.database),
            "reporting": asdict(config.reporting),
            "log_level": config.log_level,
            "log_file": config.log_file
        }


# Global config manager instance
_config_manager: Optional[ConfigManager] = None


def get_config_manager(config_path: str = "config/config.json") -> ConfigManager:
    """Get or create global config manager instance."""
    global _config_manager
    if _config_manager is None:
        _config_manager = ConfigManager(config_path)
    return _config_manager
