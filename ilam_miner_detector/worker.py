"""
Worker thread module for Ilam Miner Detector.
Handles background scanning operations with PyQt5 signals.
"""

import asyncio
import logging
from typing import List, Optional, Callable
from dataclasses import dataclass

from PyQt5.QtCore import QThread, pyqtSignal

from .network_scanner import NetworkScanner, HostScanResult
from .database import get_db_manager, ScanRecord, HostRecord
from .ip_manager import get_ip_manager
from .config_manager import get_config_manager

logger = logging.getLogger(__name__)


@dataclass
class ScanProgress:
    """Progress information for a scan."""
    current: int
    total: int
    status: str
    current_ip: str = ""


@dataclass
class ScanComplete:
    """Scan completion information."""
    scan_id: int
    total_hosts: int
    responsive_hosts: int
    miners_detected: int
    success: bool
    error_message: str = ""


class ScanWorker(QThread):
    """
    QThread worker for performing network scans in background.
    Emits signals for progress updates and results.
    """
    
    # Signals
    progress_updated = pyqtSignal(int, int, str)  # current, total, status
    host_scanned = pyqtSignal(object)  # HostScanResult
    scan_completed = pyqtSignal(object)  # ScanComplete
    scan_error = pyqtSignal(str)  # Error message
    log_message = pyqtSignal(str)  # Log message for display
    
    def __init__(self, scan_id: int, cidr_range: str, 
                 scan_name: str = "", parent=None):
        """
        Initialize scan worker.
        
        Args:
            scan_id: Database scan ID
            cidr_range: CIDR range to scan
            scan_name: Name for the scan
            parent: Parent QObject
        """
        super().__init__(parent)
        
        self.scan_id = scan_id
        self.cidr_range = cidr_range
        self.scan_name = scan_name
        self.config = get_config_manager().get()
        self.db = get_db_manager()
        self.scanner = NetworkScanner()
        self.ip_manager = get_ip_manager()
        
        self._cancelled = False
        self._responsive_count = 0
        self._miner_count = 0
        
    def cancel(self):
        """Request scan cancellation."""
        self._cancelled = True
        self.scanner.cancel()
        self.log_message.emit("Cancellation requested...")
        
    def run(self):
        """Main worker thread execution."""
        try:
            # Update scan status
            self.db.update_scan_status(self.scan_id, "running")
            self.log_message.emit(f"Starting scan of {self.cidr_range}")
            
            # Parse CIDR and get IP list
            try:
                ip_info = self.ip_manager.parse_cidr(self.cidr_range)
                total_hosts = ip_info.total_hosts
                self.log_message.emit(f"Total hosts to scan: {total_hosts}")
            except ValueError as e:
                raise ValueError(f"Invalid CIDR range: {e}")
            
            # Update total hosts in DB
            self.db.update_scan_stats(self.scan_id, total_hosts=total_hosts)
            
            # Generate IP iterator
            ip_generator = self.ip_manager.generate_from_cidr(self.cidr_range)
            
            # Get ports to scan
            ports = self.config.miner_ports.all_ports
            self.log_message.emit(f"Scanning ports: {ports}")
            
            # Run async scan
            loop = asyncio.new_event_loop()
            asyncio.set_event_loop(loop)
            
            try:
                results = loop.run_until_complete(
                    self.scanner.scan_range(
                        ip_generator,
                        ports,
                        progress_callback=self._on_progress
                    )
                )
            finally:
                loop.close()
            
            # Check if cancelled
            if self._cancelled:
                self.db.update_scan_status(self.scan_id, "cancelled")
                self.scan_completed.emit(ScanComplete(
                    scan_id=self.scan_id,
                    total_hosts=total_hosts,
                    responsive_hosts=self._responsive_count,
                    miners_detected=self._miner_count,
                    success=False,
                    error_message="Scan was cancelled by user"
                ))
                return
            
            # Process and save results
            self._process_results(results)
            
            # Update final stats
            self.db.update_scan_stats(
                self.scan_id,
                responsive_hosts=self._responsive_count,
                miners_detected=self._miner_count
            )
            self.db.update_scan_status(self.scan_id, "completed")
            
            # Emit completion
            self.scan_completed.emit(ScanComplete(
                scan_id=self.scan_id,
                total_hosts=total_hosts,
                responsive_hosts=self._responsive_count,
                miners_detected=self._miner_count,
                success=True
            ))
            
            self.log_message.emit(f"Scan completed. Found {self._miner_count} potential miners.")
            
        except Exception as e:
            logger.exception("Scan failed")
            error_msg = str(e)
            self.scan_error.emit(error_msg)
            self.db.update_scan_status(self.scan_id, "error")
            self.scan_completed.emit(ScanComplete(
                scan_id=self.scan_id,
                total_hosts=0,
                responsive_hosts=self._responsive_count,
                miners_detected=self._miner_count,
                success=False,
                error_message=error_msg
            ))
    
    def _on_progress(self, current: int, total: int, status: str):
        """Handle progress updates from scanner."""
        if self._cancelled:
            return
        self.progress_updated.emit(current, total, status)
    
    def _process_results(self, results: List[HostScanResult]):
        """Process and save scan results to database."""
        import json
        
        for result in results:
            if self._cancelled:
                break
            
            # Count statistics
            if result.is_responsive:
                self._responsive_count += 1
            if result.is_miner_detected:
                self._miner_count += 1
            
            # Create host record
            host_record = HostRecord(
                scan_id=self.scan_id,
                ip_address=result.ip_address,
                is_responsive=result.is_responsive,
                ping_time_ms=result.ping_time_ms,
                open_ports=json.dumps([p.port for p in result.open_ports]),
                banner_info=json.dumps({p.port: p.banner for p in result.open_ports}),
                is_miner_detected=result.is_miner_detected,
                miner_type=result.miner_type,
                confidence_score=result.confidence_score
            )
            
            # Save to database
            try:
                self.db.add_host(host_record)
            except Exception as e:
                logger.error(f"Failed to save host {result.ip_address}: {e}")
            
            # Emit signal for real-time display
            self.host_scanned.emit(result)


class GeolocationWorker(QThread):
    """
    QThread worker for geolocation lookups.
    """
    
    # Signals
    progress_updated = pyqtSignal(int, int)  # current, total
    ip_geolocated = pyqtSignal(str, object)  # ip, geolocation_result
    geo_completed = pyqtSignal(int, int)  # success_count, failed_count
    log_message = pyqtSignal(str)
    
    def __init__(self, ip_addresses: List[str], parent=None):
        """
        Initialize geolocation worker.
        
        Args:
            ip_addresses: List of IPs to geolocate
            parent: Parent QObject
        """
        super().__init__(parent)
        self.ip_addresses = ip_addresses
        self._cancelled = False
        
    def cancel(self):
        """Cancel geolocation."""
        self._cancelled = True
        
    def run(self):
        """Main worker execution."""
        from .geolocation import get_geolocation_service
        
        geo_service = get_geolocation_service()
        total = len(self.ip_addresses)
        success_count = 0
        failed_count = 0
        
        self.log_message.emit(f"Starting geolocation for {total} IP addresses")
        
        for i, ip in enumerate(self.ip_addresses):
            if self._cancelled:
                self.log_message.emit("Geolocation cancelled")
                break
            
            try:
                result = geo_service.lookup(ip)
                if result.success:
                    success_count += 1
                else:
                    failed_count += 1
                
                self.ip_geolocated.emit(ip, result)
                self.progress_updated.emit(i + 1, total)
                
            except Exception as e:
                logger.error(f"Geolocation error for {ip}: {e}")
                failed_count += 1
        
        self.geo_completed.emit(success_count, failed_count)
        self.log_message.emit(f"Geolocation complete. Success: {success_count}, Failed: {failed_count}")


class ReportWorker(QThread):
    """
    QThread worker for generating reports.
    """
    
    # Signals
    report_generated = pyqtSignal(str, str)  # format, file_path
    report_error = pyqtSignal(str, str)  # format, error_message
    all_reports_complete = pyqtSignal(dict)  # format -> path mapping
    log_message = pyqtSignal(str)
    
    def __init__(self, scan_id: int, formats: List[str] = None, parent=None):
        """
        Initialize report worker.
        
        Args:
            scan_id: Scan ID to generate report for
            formats: List of formats ('json', 'csv', 'html')
            parent: Parent QObject
        """
        super().__init__(parent)
        self.scan_id = scan_id
        self.formats = formats or ['json', 'csv', 'html']
        
    def run(self):
        """Main worker execution."""
        from .reporter import get_report_generator
        
        reporter = get_report_generator()
        results = {}
        
        self.log_message.emit(f"Generating reports for scan {self.scan_id}")
        
        for fmt in self.formats:
            try:
                if fmt == 'json':
                    path = reporter.export_json(self.scan_id)
                elif fmt == 'csv':
                    path = reporter.export_csv(self.scan_id)
                elif fmt == 'html':
                    path = reporter.export_html(self.scan_id)
                else:
                    self.report_error.emit(fmt, f"Unknown format: {fmt}")
                    continue
                
                results[fmt] = path
                self.report_generated.emit(fmt, path)
                self.log_message.emit(f"Generated {fmt.upper()} report: {path}")
                
            except Exception as e:
                error_msg = str(e)
                self.report_error.emit(fmt, error_msg)
                self.log_message.emit(f"Failed to generate {fmt} report: {error_msg}")
        
        self.all_reports_complete.emit(results)


def create_scan_worker(scan_id: int, cidr_range: str, 
                       scan_name: str = "", parent=None) -> ScanWorker:
    """Factory function to create a scan worker."""
    return ScanWorker(scan_id, cidr_range, scan_name, parent)
