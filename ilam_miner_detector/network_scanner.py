"""
Network scanning engine for detecting cryptocurrency miners.
Performs TCP port scanning, service detection, and banner grabbing.
"""

import asyncio
import socket
import subprocess
import platform
from typing import List, Dict, Optional, Callable
from dataclasses import dataclass
import logging
import re


@dataclass
class ScanResult:
    """Result of scanning a single host."""
    ip_address: str
    is_reachable: bool = False
    hostname: Optional[str] = None
    open_ports: List[int] = None
    services: Dict[int, str] = None
    is_miner: bool = False
    miner_type: Optional[str] = None
    banner_data: Optional[str] = None
    error: Optional[str] = None
    
    def __post_init__(self):
        if self.open_ports is None:
            self.open_ports = []
        if self.services is None:
            self.services = {}


class NetworkScanner:
    """Asynchronous network scanner for miner detection."""
    
    # Known miner service signatures
    MINER_SIGNATURES = {
        'stratum': [
            b'stratum',
            b'mining.subscribe',
            b'mining.authorize',
            b'mining.notify',
            b'eth_submitLogin',
            b'eth_getWork'
        ],
        'bitcoin': [
            b'Bitcoin',
            b'Satoshi',
            b'getwork',
            b'getblocktemplate'
        ],
        'ethereum': [
            b'eth_',
            b'net_version',
            b'web3_clientVersion',
            b'geth',
            b'parity'
        ],
        'monero': [
            b'monero',
            b'cryptonight',
            b'xmr-',
            b'monerod'
        ]
    }
    
    def __init__(self, 
                 timeout: float = 3.0,
                 max_concurrent: int = 50,
                 enable_ping: bool = True,
                 enable_banner_grab: bool = True,
                 banner_timeout: float = 2.0):
        """
        Initialize network scanner.
        
        Args:
            timeout: Socket connection timeout in seconds
            max_concurrent: Maximum concurrent scan operations
            enable_ping: Enable ICMP ping before port scanning
            enable_banner_grab: Enable TCP banner grabbing
            banner_timeout: Banner grab timeout in seconds
        """
        self.timeout = timeout
        self.max_concurrent = max_concurrent
        self.enable_ping = enable_ping
        self.enable_banner_grab = enable_banner_grab
        self.banner_timeout = banner_timeout
        
        self._semaphore = asyncio.Semaphore(max_concurrent)
        self._cancelled = False
        
        self.logger = logging.getLogger(__name__)
    
    def cancel(self):
        """Cancel ongoing scan operations."""
        self._cancelled = True
    
    async def scan_host(self, ip: str, ports: List[int], 
                       progress_callback: Optional[Callable[[str], None]] = None) -> ScanResult:
        """
        Scan a single host for open ports and miner services.
        
        Args:
            ip: Target IP address
            ports: List of ports to scan
            progress_callback: Optional callback for progress updates
            
        Returns:
            ScanResult object
        """
        async with self._semaphore:
            if self._cancelled:
                return ScanResult(ip_address=ip, error="Scan cancelled")
            
            result = ScanResult(ip_address=ip)
            
            # Step 1: Ping check (if enabled)
            if self.enable_ping:
                result.is_reachable = await self._ping_host(ip)
                if not result.is_reachable:
                    if progress_callback:
                        progress_callback(f"{ip}: Not reachable")
                    return result
            else:
                result.is_reachable = True
            
            # Step 2: Hostname resolution
            try:
                result.hostname = await asyncio.wait_for(
                    self._resolve_hostname(ip),
                    timeout=self.timeout
                )
            except asyncio.TimeoutError:
                pass
            
            # Step 3: Port scanning
            result.open_ports = await self._scan_ports(ip, ports)
            
            if not result.open_ports:
                if progress_callback:
                    progress_callback(f"{ip}: No open ports")
                return result
            
            # Step 4: Service detection and banner grabbing
            for port in result.open_ports:
                service = self._identify_service(port)
                result.services[port] = service
                
                if self.enable_banner_grab:
                    banner = await self._grab_banner(ip, port)
                    if banner:
                        if result.banner_data:
                            result.banner_data += f"\n[Port {port}]: {banner}"
                        else:
                            result.banner_data = f"[Port {port}]: {banner}"
                        
                        # Check for miner signatures
                        miner_type = self._detect_miner_from_banner(banner)
                        if miner_type:
                            result.is_miner = True
                            result.miner_type = miner_type
            
            # Step 5: Heuristic miner detection based on port combinations
            if not result.is_miner:
                result.is_miner, result.miner_type = self._detect_miner_heuristic(result.open_ports)
            
            if progress_callback:
                if result.is_miner:
                    progress_callback(f"{ip}: MINER DETECTED ({result.miner_type}) - Ports: {result.open_ports}")
                else:
                    progress_callback(f"{ip}: Open ports: {result.open_ports}")
            
            return result
    
    async def _ping_host(self, ip: str) -> bool:
        """
        Check if host responds to ICMP ping.
        
        Args:
            ip: Target IP address
            
        Returns:
            True if host is reachable
        """
        try:
            param = '-n' if platform.system().lower() == 'windows' else '-c'
            command = ['ping', param, '1', '-w' if platform.system().lower() == 'windows' else '-W', 
                      str(int(self.timeout * 1000) if platform.system().lower() == 'windows' else int(self.timeout)), 
                      ip]
            
            process = await asyncio.create_subprocess_exec(
                *command,
                stdout=asyncio.subprocess.DEVNULL,
                stderr=asyncio.subprocess.DEVNULL
            )
            
            await asyncio.wait_for(process.wait(), timeout=self.timeout + 1)
            return process.returncode == 0
            
        except (asyncio.TimeoutError, Exception) as e:
            self.logger.debug(f"Ping failed for {ip}: {e}")
            return False
    
    async def _resolve_hostname(self, ip: str) -> Optional[str]:
        """Resolve IP to hostname."""
        try:
            loop = asyncio.get_event_loop()
            hostname = await loop.run_in_executor(None, socket.gethostbyaddr, ip)
            return hostname[0]
        except Exception:
            return None
    
    async def _scan_ports(self, ip: str, ports: List[int]) -> List[int]:
        """
        Scan multiple ports on a host.
        
        Args:
            ip: Target IP
            ports: List of ports to check
            
        Returns:
            List of open ports
        """
        tasks = [self._check_port(ip, port) for port in ports]
        results = await asyncio.gather(*tasks, return_exceptions=True)
        
        open_ports = []
        for port, result in zip(ports, results):
            if result is True:
                open_ports.append(port)
        
        return sorted(open_ports)
    
    async def _check_port(self, ip: str, port: int) -> bool:
        """
        Check if a single port is open.
        
        Args:
            ip: Target IP
            port: Target port
            
        Returns:
            True if port is open
        """
        try:
            conn = asyncio.open_connection(ip, port)
            reader, writer = await asyncio.wait_for(conn, timeout=self.timeout)
            writer.close()
            await writer.wait_closed()
            return True
        except (asyncio.TimeoutError, ConnectionRefusedError, OSError):
            return False
        except Exception as e:
            self.logger.debug(f"Error checking {ip}:{port}: {e}")
            return False
    
    async def _grab_banner(self, ip: str, port: int) -> Optional[str]:
        """
        Grab service banner from a port.
        
        Args:
            ip: Target IP
            port: Target port
            
        Returns:
            Banner string or None
        """
        try:
            conn = asyncio.open_connection(ip, port)
            reader, writer = await asyncio.wait_for(conn, timeout=self.timeout)
            
            # Some services send banner immediately
            banner = await asyncio.wait_for(
                reader.read(1024),
                timeout=self.banner_timeout
            )
            
            # For some services, we need to send a probe
            if not banner:
                # Try stratum probe
                writer.write(b'{"id": 1, "method": "mining.subscribe", "params": []}\n')
                await writer.drain()
                banner = await asyncio.wait_for(
                    reader.read(1024),
                    timeout=self.banner_timeout
                )
            
            writer.close()
            await writer.wait_closed()
            
            if banner:
                return banner.decode('utf-8', errors='ignore').strip()
            
        except Exception as e:
            self.logger.debug(f"Banner grab failed for {ip}:{port}: {e}")
        
        return None
    
    def _identify_service(self, port: int) -> str:
        """
        Identify common service by port number.
        
        Args:
            port: Port number
            
        Returns:
            Service name
        """
        services = {
            3333: 'Stratum Mining',
            4444: 'Stratum Mining',
            4028: 'CGMiner API',
            5555: 'Stratum Mining',
            7777: 'Stratum Mining',
            8080: 'HTTP Proxy/Mining',
            8081: 'HTTP Proxy/Mining',
            8332: 'Bitcoin RPC',
            8333: 'Bitcoin P2P',
            8545: 'Ethereum RPC',
            8888: 'Stratum Mining',
            9090: 'Mining Dashboard',
            9999: 'Stratum Mining',
            14433: 'Stratum Mining (SSL)',
            14444: 'Stratum Mining (SSL)',
            18332: 'Bitcoin Testnet RPC',
            18333: 'Bitcoin Testnet P2P',
            30303: 'Ethereum P2P',
        }
        
        return services.get(port, f'Unknown Service (Port {port})')
    
    def _detect_miner_from_banner(self, banner: str) -> Optional[str]:
        """
        Detect miner type from banner data.
        
        Args:
            banner: Banner string
            
        Returns:
            Miner type or None
        """
        banner_lower = banner.lower().encode('utf-8')
        
        for miner_type, signatures in self.MINER_SIGNATURES.items():
            for signature in signatures:
                if signature.lower() in banner_lower:
                    return miner_type
        
        return None
    
    def _detect_miner_heuristic(self, open_ports: List[int]) -> tuple:
        """
        Detect miner based on port combination heuristics.
        
        Args:
            open_ports: List of open ports
            
        Returns:
            Tuple of (is_miner, miner_type)
        """
        stratum_ports = {3333, 4444, 4028, 5555, 7777, 8888, 9999, 14433, 14444}
        bitcoin_ports = {8332, 8333, 18332, 18333}
        ethereum_ports = {8545, 8546, 30303, 30304}
        
        open_set = set(open_ports)
        
        # Check for Stratum mining
        if open_set & stratum_ports:
            return (True, 'stratum')
        
        # Check for Bitcoin
        if open_set & bitcoin_ports:
            return (True, 'bitcoin')
        
        # Check for Ethereum
        if open_set & ethereum_ports:
            return (True, 'ethereum')
        
        return (False, None)
