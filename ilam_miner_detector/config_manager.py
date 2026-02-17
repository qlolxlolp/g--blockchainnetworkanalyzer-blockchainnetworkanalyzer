"""
Configuration management for Ilam Miner Detector.
Handles loading, validation, and access to configuration settings.
"""

import json
import os
from pathlib import Path
from typing import Dict, Any, Optional
from dataclasses import dataclass, asdict


@dataclass
class ScanConfig:
    """Scan operation configuration."""
    timeout_ms: int = 3000
    max_concurrent: int = 50
    retry_count: int = 2
    ping_enabled: bool = True
    banner_grab_enabled: bool = True
    banner_timeout_ms: int = 2000


@dataclass
class GeolocationConfig:
    """Geolocation service configuration."""
    primary_provider: str = "ip-api"
    fallback_provider: Optional[str] = "ipinfo"
    api_key: Optional[str] = None
    rate_limit_per_minute: int = 45
    cache_enabled: bool = True
    ilam_lat_min: float = 32.5
    ilam_lat_max: float = 33.5
    ilam_lon_min: float = 46.0
    ilam_lon_max: float = 47.5


@dataclass
class DatabaseConfig:
    """Database configuration."""
    path: str = "data/ilam_miner.db"
    connection_pool_size: int = 5
    enable_wal: bool = True


@dataclass
class MinerPorts:
    """Known cryptocurrency miner ports."""
    stratum: list = None
    bitcoin: list = None
    ethereum: list = None
    generic: list = None
    
    def __post_init__(self):
        if self.stratum is None:
            self.stratum = [3333, 4444, 4028, 7777, 14433, 14444, 5555, 8888, 9999]
        if self.bitcoin is None:
            self.bitcoin = [8332, 8333, 18332, 18333]
        if self.ethereum is None:
            self.ethereum = [8545, 8546, 30303, 30304]
        if self.generic is None:
            self.generic = [8080, 8081, 3000, 9090]
    
    def all_ports(self) -> list:
        """Return all configured miner ports."""
        return sorted(set(self.stratum + self.bitcoin + self.ethereum + self.generic))


class ConfigManager:
    """Manages application configuration from JSON files."""
    
    DEFAULT_CONFIG = {
        "scan": {
            "timeout_ms": 3000,
            "max_concurrent": 50,
            "retry_count": 2,
            "ping_enabled": True,
            "banner_grab_enabled": True,
            "banner_timeout_ms": 2000
        },
        "geolocation": {
            "primary_provider": "ip-api",
            "fallback_provider": "ipinfo",
            "api_key": None,
            "rate_limit_per_minute": 45,
            "cache_enabled": True,
            "ilam_lat_min": 32.5,
            "ilam_lat_max": 33.5,
            "ilam_lon_min": 46.0,
            "ilam_lon_max": 47.5
        },
        "database": {
            "path": "data/ilam_miner.db",
            "connection_pool_size": 5,
            "enable_wal": True
        },
        "miner_ports": {
            "stratum": [3333, 4444, 4028, 7777, 14433, 14444, 5555, 8888, 9999],
            "bitcoin": [8332, 8333, 18332, 18333],
            "ethereum": [8545, 8546, 30303, 30304],
            "generic": [8080, 8081, 3000, 9090]
        }
    }
    
    def __init__(self, config_path: Optional[str] = None):
        """
        Initialize configuration manager.
        
        Args:
            config_path: Path to configuration JSON file. If None, uses default config.
        """
        self.config_path = config_path
        self._config_data = self._load_config()
        
        self.scan = ScanConfig(**self._config_data.get("scan", {}))
        self.geolocation = GeolocationConfig(**self._config_data.get("geolocation", {}))
        self.database = DatabaseConfig(**self._config_data.get("database", {}))
        self.miner_ports = MinerPorts(**self._config_data.get("miner_ports", {}))
    
    def _load_config(self) -> Dict[str, Any]:
        """Load configuration from file or use defaults."""
        if self.config_path and os.path.exists(self.config_path):
            try:
                with open(self.config_path, 'r', encoding='utf-8') as f:
                    config = json.load(f)
                    # Merge with defaults to ensure all keys exist
                    return self._merge_configs(self.DEFAULT_CONFIG, config)
            except Exception as e:
                print(f"Warning: Failed to load config from {self.config_path}: {e}")
                print("Using default configuration")
        
        return self.DEFAULT_CONFIG.copy()
    
    def _merge_configs(self, default: Dict, override: Dict) -> Dict:
        """Recursively merge override config into default config."""
        result = default.copy()
        for key, value in override.items():
            if key in result and isinstance(result[key], dict) and isinstance(value, dict):
                result[key] = self._merge_configs(result[key], value)
            else:
                result[key] = value
        return result
    
    def save(self, path: Optional[str] = None):
        """
        Save current configuration to JSON file.
        
        Args:
            path: Target file path. If None, uses original config_path.
        """
        target_path = path or self.config_path
        if not target_path:
            raise ValueError("No config path specified for saving")
        
        config_data = {
            "scan": asdict(self.scan),
            "geolocation": asdict(self.geolocation),
            "database": asdict(self.database),
            "miner_ports": {
                "stratum": self.miner_ports.stratum,
                "bitcoin": self.miner_ports.bitcoin,
                "ethereum": self.miner_ports.ethereum,
                "generic": self.miner_ports.generic
            }
        }
        
        Path(target_path).parent.mkdir(parents=True, exist_ok=True)
        with open(target_path, 'w', encoding='utf-8') as f:
            json.dump(config_data, f, indent=2)
    
    def get_raw_config(self) -> Dict[str, Any]:
        """Get raw configuration dictionary."""
        return self._config_data
