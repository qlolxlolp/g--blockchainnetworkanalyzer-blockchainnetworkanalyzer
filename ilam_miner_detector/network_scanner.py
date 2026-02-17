"""
Network scanning module for Ilam Miner Detector.
Implements TCP port scanning, ping checks, and service detection.
"""

import asyncio
import socket
import struct
import time
import logging
from typing import List, Dict, Optional, Set, Callable, Any
from dataclasses import dataclass, field
from concurrent.futures import ThreadPoolExecutor
import ipaddress

from .config_manager import get_config_manager

logger = logging.getLogger(__name__)


@dataclass
class PortScanResult:
    """Result of scanning a single port."""
    port: int
    is_open: bool
    banner: str = ""
    service_name: str = ""
    response_time_ms: float = 0.0


@dataclass
class HostScanResult:
    """Complete scan result for a single host."""
    ip_address: str
    is_responsive: bool = False
    ping_time_ms: Optional[float] = None
    open_ports: List[PortScanResult] = field(default_factory=list)
    banners: Dict[int, str] = field(default_factory=dict)
    detected_services: List[str] = field(default_factory=list)
    is_miner_detected: bool = False
    miner_type: str = ""
    confidence_score: float = 0.0
    scan_error: str = ""
    timestamp: float = field(default_factory=time.time)


class NetworkScanner:
    """
    Network scanner with TCP port scanning and service detection.
    Uses asyncio for efficient concurrent operations.
    """
    
    # Known service signatures for banner analysis
    SERVICE_SIGNATURES = {
        "stratum": [b"mining.subscribe", b"mining.authorize", b"stratum"],
        "bitcoin_rpc": [b"jsonrpc", b"bitcoin", b"bitcoind"],
        "ethereum": [b"ethereum", b"geth", b"parity"],
        "http": [b"HTTP/1.", b"html", b"Server:"],
        "ssh": [b"SSH-2.0", b"OpenSSH"],
        "telnet": [b"telnet", b"login:"],
    }
    
    # Port to service mapping
    PORT_SERVICES = {
        3333: "stratum",
        4444: "stratum",
        4028: "stratum",
        7777: "stratum",
        14433: "stratum",
        14444: "stratum",
        8332: "bitcoin_rpc",
        8333: "bitcoin",
        8545: "ethereum_rpc",
        30303: "ethereum_p2p",
        8080: "http_proxy",
        8081: "http_alt",
        22: "ssh",
        23: "telnet",
        80: "http",
        443: "https",
    }
    
    def __init__(self):
        self.config = get_config_manager().get()
        self._cancelled = False
        self._progress_callback: Optional[Callable[[int, int, str], None]] = None
        self._result_callback: Optional[Callable[[HostScanResult], None]] = None
        
    def set_progress_callback(self, callback: Callable[[int, int, str], None]) -> None:
        """Set callback for progress updates (current, total, status)."""
        self._progress_callback = callback
        
    def set_result_callback(self, callback: Callable[[HostScanResult], None]) -> None:
        """Set callback for scan results."""
        self._result_callback = callback
        
    def cancel(self) -> None:
        """Cancel ongoing scan."""
        self._cancelled = True
        logger.info("Scan cancellation requested")
    
    async def scan_host(self, ip: str, ports: List[int]) -> HostScanResult:
        """
        Scan a single host for open ports and service detection.
        
        Args:
            ip: IP address to scan
            ports: List of ports to check
            
        Returns:
            HostScanResult with scan data
        """
        result = HostScanResult(ip_address=ip)
        
        try:
            # Ping check first (if enabled)
            if self.config.scan.enable_ping:
                ping_time = await self._ping_host(ip)
                if ping_time is not None:
                    result.is_responsive = True
                    result.ping_time_ms = ping_time
            
            # TCP port scan
            open_ports = await self._scan_ports(ip, ports)
            result.open_ports = open_ports
            
            # Analyze for mining services
            self._analyze_miner_detection(result)
            
        except Exception as e:
            result.scan_error = str(e)
            logger.error(f"Error scanning {ip}: {e}")
        
        return result
    
    async def scan_range(self, ip_generator, ports: List[int],
                        progress_callback: Optional[Callable] = None) -> List[HostScanResult]:
        """
        Scan a range of IPs with controlled concurrency.
        
        Args:
            ip_generator: Generator yielding IP addresses
            ports: List of ports to scan
            progress_callback: Optional callback for progress
            
        Returns:
            List of HostScanResult
        """
        self._cancelled = False
        self._progress_callback = progress_callback or self._progress_callback
        
        results = []
        ip_list = list(ip_generator)
        total = len(ip_list)
        completed = 0
        
        # Create semaphore for concurrency control
        semaphore = asyncio.Semaphore(self.config.scan.concurrency)
        
        async def scan_with_semaphore(ip: str) -> HostScanResult:
            async with semaphore:
                if self._cancelled:
                    return HostScanResult(ip_address=ip, scan_error="Cancelled")
                return await self.scan_host(ip, ports)
        
        # Create tasks
        tasks = [scan_with_semaphore(str(ip)) for ip in ip_list]
        
        # Process results as they complete
        for coro in asyncio.as_completed(tasks):
            if self._cancelled:
                break
                
            result = await coro
            results.append(result)
            completed += 1
            
            if self._result_callback:
                self._result_callback(result)
            
            if self._progress_callback:
                self._progress_callback(completed, total, f"Scanned {result.ip_address}")
        
        return results
    
    async def _ping_host(self, ip: str) -> Optional[float]:
        """
        Perform ICMP ping to host.
        Returns round-trip time in ms or None if unreachable.
        """
        try:
            # Use asyncio subprocess for ping
            ping_cmd = ["ping", "-c", "1", "-W", str(int(self.config.scan.ping_timeout)), ip]
            
            proc = await asyncio.create_subprocess_exec(
                *ping_cmd,
                stdout=asyncio.subprocess.PIPE,
                stderr=asyncio.subprocess.DEVNULL
            )
            
            try:
                stdout, _ = await asyncio.wait_for(
                    proc.communicate(),
                    timeout=self.config.scan.ping_timeout + 1
                )
            except asyncio.TimeoutError:
                proc.kill()
                return None
            
            if proc.returncode == 0:
                # Parse time from ping output
                output = stdout.decode('utf-8', errors='ignore')
                for line in output.split('\n'):
                    if 'time=' in line:
                        try:
                            time_part = line.split('time=')[1].split()[0]
                            return float(time_part.replace('ms', ''))
                        except (IndexError, ValueError):
                            pass
                return 0.0  # Success but couldn't parse time
            
            return None
            
        except Exception as e:
            logger.debug(f"Ping failed for {ip}: {e}")
            return None
    
    async def _scan_ports(self, ip: str, ports: List[int]) -> List[PortScanResult]:
        """Scan multiple ports on a host."""
        tasks = [self._check_port(ip, port) for port in ports]
        results = await asyncio.gather(*tasks, return_exceptions=True)
        
        port_results = []
        for port, result in zip(ports, results):
            if isinstance(result, Exception):
                logger.debug(f"Port scan error for {ip}:{port}: {result}")
                continue
            if result.is_open:
                port_results.append(result)
        
        return port_results
    
    async def _check_port(self, ip: str, port: int) -> PortScanResult:
        """Check if a single port is open and grab banner."""
        result = PortScanResult(port=port, is_open=False)
        start_time = time.time()
        
        try:
            # Try to connect
            reader, writer = await asyncio.wait_for(
                asyncio.open_connection(ip, port),
                timeout=self.config.scan.tcp_timeout
            )
            
            result.is_open = True
            
            # Try to grab banner if enabled
            if self.config.scan.enable_banner_grab:
                banner = await self._grab_banner(reader, writer, port)
                result.banner = banner
                result.service_name = self._detect_service(port, banner)
            else:
                # Use port mapping
                result.service_name = self.PORT_SERVICES.get(port, "unknown")
            
            writer.close()
            try:
                await writer.wait_closed()
            except Exception:
                pass
            
            result.response_time_ms = (time.time() - start_time) * 1000
            
        except asyncio.TimeoutError:
            pass
        except ConnectionRefusedError:
            pass
        except OSError:
            pass
        except Exception as e:
            logger.debug(f"Error checking {ip}:{port}: {e}")
        
        return result
    
    async def _grab_banner(self, reader: asyncio.StreamReader,
                          writer: asyncio.StreamWriter, port: int) -> str:
        """Attempt to grab service banner."""
        banner = ""
        
        try:
            # Send appropriate probe based on port
            probe = self._get_probe_for_port(port)
            if probe:
                writer.write(probe)
                await writer.drain()
            
            # Read response
            data = await asyncio.wait_for(
                reader.read(1024),
                timeout=self.config.scan.banner_timeout
            )
            
            # Try to decode as text
            try:
                banner = data.decode('utf-8', errors='ignore').strip()
            except UnicodeDecodeError:
                banner = data.hex()[:100]  # Hex representation
            
        except asyncio.TimeoutError:
            pass
        except Exception as e:
            logger.debug(f"Banner grab error: {e}")
        
        return banner[:500]  # Limit length
    
    def _get_probe_for_port(self, port: int) -> bytes:
        """Get appropriate probe bytes for a port."""
        probes = {
            80: b"HEAD / HTTP/1.0\r\n\r\n",
            443: b"",  # SSL handshake needed
            22: b"",  # SSH sends banner first
            3333: b'{"id": 1, "method": "mining.subscribe", "params": []}\n',
            8332: b'{"jsonrpc":"1.0","id":"1","method":"getinfo","params":[]}\n',
            8545: b'{"jsonrpc":"2.0","method":"eth_protocolVersion","params":[],"id":1}\n',
        }
        return probes.get(port, b"")
    
    def _detect_service(self, port: int, banner: str) -> str:
        """Detect service type from port and banner."""
        # First check port mapping
        if port in self.PORT_SERVICES:
            return self.PORT_SERVICES[port]
        
        # Check banner signatures
        banner_lower = banner.lower().encode()
        for service, signatures in self.SERVICE_SIGNATURES.items():
            for sig in signatures:
                if sig.lower() in banner_lower:
                    return service
        
        return "unknown"
    
    def _analyze_miner_detection(self, result: HostScanResult) -> None:
        """Analyze scan results to detect potential mining activity."""
        if not result.open_ports:
            return
        
        miner_ports = set(self.config.miner_ports.all_ports)
        open_port_numbers = {p.port for p in result.open_ports}
        
        # Check for known mining ports
        mining_ports_found = open_port_numbers & miner_ports
        
        if mining_ports_found:
            result.is_miner_detected = True
            
            # Determine miner type
            miner_types = []
            if mining_ports_found & set(self.config.miner_ports.stratum_ports):
                miner_types.append("Stratum")
            if mining_ports_found & set(self.config.miner_ports.bitcoin_ports):
                miner_types.append("Bitcoin")
            if mining_ports_found & set(self.config.miner_ports.ethereum_ports):
                miner_types.append("Ethereum")
            
            result.miner_type = "/".join(miner_types) if miner_types else "Unknown"
            
            # Calculate confidence score (0-100)
            confidence = min(100, len(mining_ports_found) * 25 + 25)
            
            # Check banners for stronger evidence
            for port_result in result.open_ports:
                if port_result.port in miner_ports and port_result.banner:
                    confidence = min(100, confidence + 20)
                    if any(sig in port_result.banner.lower() for sig in 
                          ['stratum', 'mining', 'bitcoin', 'ethereum']):
                        confidence = min(100, confidence + 15)
            
            result.confidence_score = confidence


def get_network_scanner() -> NetworkScanner:
    """Get network scanner instance."""
    return NetworkScanner()
