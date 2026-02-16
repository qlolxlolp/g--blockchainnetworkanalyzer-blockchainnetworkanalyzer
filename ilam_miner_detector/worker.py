"""
Worker thread for executing scans without blocking the GUI.
"""

import asyncio
from PyQt5.QtCore import QThread, pyqtSignal
from typing import List
import logging


class ScanWorker(QThread):
    """Worker thread for network scanning operations."""
    
    # Signals for communication with GUI
    progress_updated = pyqtSignal(int, int, str)  # (scanned, total, message)
    host_discovered = pyqtSignal(dict)  # host data
    scan_completed = pyqtSignal(bool, str)  # (success, message)
    error_occurred = pyqtSignal(str)  # error message
    
    def __init__(self, 
                 scanner,
                 ip_generator,
                 ports: List[int],
                 scan_id: int,
                 database,
                 geolocation_service=None,
                 filter_ilam: bool = True):
        """
        Initialize scan worker.
        
        Args:
            scanner: NetworkScanner instance
            ip_generator: Iterator yielding IP addresses
            ports: List of ports to scan
            scan_id: Database scan ID
            database: Database instance
            geolocation_service: Optional GeolocationService instance
            filter_ilam: If True, only report hosts in Ilam region
        """
        super().__init__()
        self.scanner = scanner
        self.ip_generator = ip_generator
        self.ports = ports
        self.scan_id = scan_id
        self.database = database
        self.geolocation_service = geolocation_service
        self.filter_ilam = filter_ilam
        
        self._cancelled = False
        self.logger = logging.getLogger(__name__)
    
    def cancel(self):
        """Request cancellation of scan."""
        self._cancelled = True
        self.scanner.cancel()
        self.logger.info("Scan cancellation requested")
    
    def run(self):
        """Execute scan in worker thread."""
        try:
            # Create new event loop for this thread
            loop = asyncio.new_event_loop()
            asyncio.set_event_loop(loop)
            
            # Run async scan
            loop.run_until_complete(self._run_scan())
            
            loop.close()
            
            if not self._cancelled:
                self.scan_completed.emit(True, "Scan completed successfully")
            else:
                self.scan_completed.emit(False, "Scan cancelled by user")
        
        except Exception as e:
            self.logger.error(f"Scan worker error: {e}", exc_info=True)
            self.error_occurred.emit(f"Scan error: {str(e)}")
            self.scan_completed.emit(False, f"Scan failed: {str(e)}")
    
    async def _run_scan(self):
        """Async scan execution."""
        ip_list = list(self.ip_generator)
        total_ips = len(ip_list)
        scanned_count = 0
        miners_found = 0
        
        self.logger.info(f"Starting scan of {total_ips} IPs")
        
        for ip in ip_list:
            if self._cancelled:
                break
            
            try:
                # Scan host
                result = await self.scanner.scan_host(
                    ip,
                    self.ports,
                    progress_callback=lambda msg: self.logger.debug(msg)
                )
                
                scanned_count += 1
                
                # Update progress
                self.progress_updated.emit(
                    scanned_count,
                    total_ips,
                    f"Scanning {ip}..."
                )
                
                # Skip if not reachable or no open ports
                if not result.is_reachable or not result.open_ports:
                    continue
                
                # Geolocation lookup if service is available
                geo_data = None
                if self.geolocation_service:
                    try:
                        geo_data = await self.geolocation_service.lookup(ip)
                        
                        # Filter by Ilam region if requested
                        if self.filter_ilam and geo_data:
                            if not self.geolocation_service.is_in_ilam_region(geo_data):
                                self.logger.info(f"Skipping {ip} - outside Ilam region")
                                continue
                    
                    except Exception as e:
                        self.logger.warning(f"Geolocation failed for {ip}: {e}")
                
                # Save to database
                host_id = self.database.add_host(
                    scan_id=self.scan_id,
                    ip_address=ip,
                    hostname=result.hostname,
                    is_reachable=result.is_reachable,
                    open_ports=result.open_ports,
                    detected_services=result.services,
                    is_miner=result.is_miner,
                    miner_type=result.miner_type,
                    banner_info=result.banner_data
                )
                
                if result.is_miner:
                    miners_found += 1
                
                # Build host data for GUI
                host_data = {
                    'id': host_id,
                    'ip_address': ip,
                    'hostname': result.hostname,
                    'is_reachable': result.is_reachable,
                    'open_ports': result.open_ports,
                    'is_miner': result.is_miner,
                    'miner_type': result.miner_type,
                    'banner_info': result.banner_data
                }
                
                # Add geolocation data if available
                if geo_data:
                    host_data.update({
                        'city': geo_data.get('city'),
                        'region': geo_data.get('regionName') or geo_data.get('region'),
                        'country': geo_data.get('country'),
                        'latitude': geo_data.get('lat'),
                        'longitude': geo_data.get('lon'),
                        'isp': geo_data.get('isp')
                    })
                
                # Emit to GUI
                self.host_discovered.emit(host_data)
                
                # Update database progress
                self.database.update_scan_progress(self.scan_id, scanned_count, miners_found)
            
            except Exception as e:
                self.logger.error(f"Error scanning {ip}: {e}", exc_info=True)
                continue
        
        # Final database update
        self.database.update_scan_progress(self.scan_id, scanned_count, miners_found)
        
        self.logger.info(f"Scan completed: {scanned_count}/{total_ips} IPs scanned, {miners_found} miners found")
