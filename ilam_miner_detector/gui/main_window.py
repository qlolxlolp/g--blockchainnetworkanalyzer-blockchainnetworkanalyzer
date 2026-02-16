"""
Main window for Ilam Miner Detector application.
"""

from PyQt5.QtWidgets import (QMainWindow, QWidget, QVBoxLayout, QHBoxLayout,
                             QTabWidget, QProgressBar, QLabel, QPushButton,
                             QMessageBox, QFileDialog, QSplitter)
from PyQt5.QtCore import Qt, QUrl
from PyQt5.QtWebEngineWidgets import QWebEngineView
import sys
import os
from datetime import datetime
import logging

from .widgets import ScanConfigWidget, ResultsTableWidget, LogWidget
from ..config_manager import ConfigManager
from ..database import Database
from ..ip_manager import IPManager
from ..network_scanner import NetworkScanner
from ..geolocation import GeolocationService
from ..map_generator import MapGenerator
from ..reporter import Reporter
from ..worker import ScanWorker


class MainWindow(QMainWindow):
    """Main application window."""
    
    def __init__(self, config: ConfigManager):
        super().__init__()
        self.config = config
        self.current_worker = None
        self.current_scan_id = None
        self.discovered_hosts = []
        
        # Initialize services
        self.database = Database(self.config.database.path)
        self.geolocation_service = GeolocationService(
            rate_limit_per_minute=self.config.geolocation.rate_limit_per_minute,
            api_key=self.config.geolocation.api_key,
            cache_provider=self.database
        )
        self.map_generator = MapGenerator()
        self.reporter = Reporter()
        
        self.logger = logging.getLogger(__name__)
        
        self.init_ui()
        self.setWindowTitle("Ilam Miner Detector v1.0.0")
        self.setGeometry(100, 100, 1400, 900)
    
    def init_ui(self):
        """Initialize user interface."""
        # Central widget
        central = QWidget()
        self.setCentralWidget(central)
        
        # Main layout
        main_layout = QVBoxLayout()
        central.setLayout(main_layout)
        
        # Title
        title = QLabel("🔍 Ilam Miner Detector")
        title.setStyleSheet("""
            font-size: 24px;
            font-weight: bold;
            color: #2c3e50;
            padding: 10px;
        """)
        main_layout.addWidget(title)
        
        # Create splitter for left panel and right content
        splitter = QSplitter(Qt.Horizontal)
        
        # Left panel: Scan configuration
        self.config_widget = ScanConfigWidget(self.config.miner_ports.all_ports())
        self.config_widget.scan_requested.connect(self.on_scan_requested)
        splitter.addWidget(self.config_widget)
        
        # Right panel: Tabs
        right_widget = QWidget()
        right_layout = QVBoxLayout()
        right_widget.setLayout(right_layout)
        
        # Progress bar
        self.progress_bar = QProgressBar()
        self.progress_bar.setVisible(False)
        right_layout.addWidget(self.progress_bar)
        
        # Progress label
        self.progress_label = QLabel("")
        self.progress_label.setVisible(False)
        right_layout.addWidget(self.progress_label)
        
        # Tab widget
        self.tabs = QTabWidget()
        
        # Results tab
        self.results_table = ResultsTableWidget()
        self.tabs.addTab(self.results_table, "Results")
        
        # Map tab
        self.map_view = QWebEngineView()
        self.tabs.addTab(self.map_view, "Map")
        
        # Log tab
        self.log_widget = LogWidget()
        self.tabs.addTab(self.log_widget, "Log")
        
        right_layout.addWidget(self.tabs)
        
        # Control buttons
        button_layout = QHBoxLayout()
        
        self.stop_button = QPushButton("Stop Scan")
        self.stop_button.clicked.connect(self.on_stop_scan)
        self.stop_button.setEnabled(False)
        self.stop_button.setStyleSheet("""
            QPushButton {
                background-color: #f44336;
                color: white;
                font-weight: bold;
                padding: 8px;
                border: none;
                border-radius: 5px;
            }
            QPushButton:hover {
                background-color: #da190b;
            }
        """)
        button_layout.addWidget(self.stop_button)
        
        self.export_json_button = QPushButton("Export JSON")
        self.export_json_button.clicked.connect(self.on_export_json)
        button_layout.addWidget(self.export_json_button)
        
        self.export_csv_button = QPushButton("Export CSV")
        self.export_csv_button.clicked.connect(self.on_export_csv)
        button_layout.addWidget(self.export_csv_button)
        
        self.export_html_button = QPushButton("Export HTML")
        self.export_html_button.clicked.connect(self.on_export_html)
        button_layout.addWidget(self.export_html_button)
        
        button_layout.addStretch()
        right_layout.addLayout(button_layout)
        
        splitter.addWidget(right_widget)
        splitter.setStretchFactor(0, 1)
        splitter.setStretchFactor(1, 3)
        
        main_layout.addWidget(splitter)
        
        # Status bar
        self.statusBar().showMessage("Ready")
    
    def on_scan_requested(self, config: dict):
        """Handle scan request from config widget."""
        self.logger.info(f"Scan requested with config: {config}")
        self.log_widget.log_info(f"Starting scan of {config['target']}")
        
        # Validate and parse IP input
        try:
            ip_generator = IPManager.parse_input(config['target'])
            ip_list = list(ip_generator)
            
            if not ip_list:
                QMessageBox.warning(self, "Invalid Input", "No valid IP addresses found in target range.")
                return
            
            self.log_widget.log_info(f"Parsed {len(ip_list)} IP addresses")
        
        except ValueError as e:
            QMessageBox.critical(self, "Invalid Input", f"Error parsing IP range: {str(e)}")
            return
        
        # Create database scan record
        scan_name = f"Scan_{datetime.now().strftime('%Y%m%d_%H%M%S')}"
        self.current_scan_id = self.database.create_scan(
            scan_name=scan_name,
            target_range=config['target'],
            config=config
        )
        
        # Clear previous results
        self.discovered_hosts = []
        self.results_table.clear_results()
        
        # Create scanner
        scanner = NetworkScanner(
            timeout=config['timeout'] / 1000.0,
            max_concurrent=config['max_concurrent'],
            enable_ping=config['enable_ping'],
            enable_banner_grab=config['enable_banner'],
            banner_timeout=2.0
        )
        
        # Create and start worker
        self.current_worker = ScanWorker(
            scanner=scanner,
            ip_generator=iter(ip_list),
            ports=config['ports'],
            scan_id=self.current_scan_id,
            database=self.database,
            geolocation_service=self.geolocation_service if config['enable_geolocation'] else None,
            filter_ilam=config['filter_ilam']
        )
        
        # Connect signals
        self.current_worker.progress_updated.connect(self.on_progress_updated)
        self.current_worker.host_discovered.connect(self.on_host_discovered)
        self.current_worker.scan_completed.connect(self.on_scan_completed)
        self.current_worker.error_occurred.connect(self.on_error_occurred)
        
        # Update UI state
        self.config_widget.set_scan_running(True)
        self.stop_button.setEnabled(True)
        self.progress_bar.setVisible(True)
        self.progress_bar.setValue(0)
        self.progress_label.setVisible(True)
        self.statusBar().showMessage("Scan in progress...")
        
        # Start worker
        self.current_worker.start()
    
    def on_stop_scan(self):
        """Handle stop scan request."""
        if self.current_worker and self.current_worker.isRunning():
            self.log_widget.log_warning("Stopping scan...")
            self.current_worker.cancel()
            self.current_worker.wait()
            self.statusBar().showMessage("Scan stopped by user")
    
    def on_progress_updated(self, scanned: int, total: int, message: str):
        """Handle progress update from worker."""
        progress = int((scanned / total) * 100) if total > 0 else 0
        self.progress_bar.setValue(progress)
        self.progress_label.setText(f"Progress: {scanned}/{total} IPs scanned ({progress}%)")
    
    def on_host_discovered(self, host_data: dict):
        """Handle host discovery from worker."""
        self.discovered_hosts.append(host_data)
        self.results_table.add_host(host_data)
        
        if host_data.get('is_miner'):
            self.log_widget.log_success(
                f"MINER DETECTED: {host_data['ip_address']} ({host_data.get('miner_type', 'Unknown')})"
            )
        else:
            self.log_widget.log_info(f"Host found: {host_data['ip_address']}")
    
    def on_scan_completed(self, success: bool, message: str):
        """Handle scan completion."""
        # Update database
        if self.current_scan_id:
            self.database.complete_scan(
                self.current_scan_id,
                status='completed' if success else 'cancelled'
            )
        
        # Update UI state
        self.config_widget.set_scan_running(False)
        self.stop_button.setEnabled(False)
        self.progress_bar.setVisible(False)
        self.progress_label.setVisible(False)
        
        if success:
            self.log_widget.log_success(f"Scan completed! Found {len(self.discovered_hosts)} hosts")
            self.statusBar().showMessage(f"Scan completed - {len(self.discovered_hosts)} hosts discovered")
            
            # Generate map if we have geolocated hosts
            self.update_map()
        else:
            self.log_widget.log_warning(message)
            self.statusBar().showMessage(message)
    
    def on_error_occurred(self, error_message: str):
        """Handle error from worker."""
        self.log_widget.log_error(error_message)
        QMessageBox.critical(self, "Scan Error", error_message)
    
    def update_map(self):
        """Update map view with discovered hosts."""
        hosts_with_geo = [h for h in self.discovered_hosts if h.get('latitude') and h.get('longitude')]
        
        if not hosts_with_geo:
            self.log_widget.log_warning("No geolocated hosts to display on map")
            return
        
        try:
            # Generate map HTML
            map_obj = self.map_generator.create_map(hosts_with_geo)
            
            # Save to temp file
            import tempfile
            temp_file = tempfile.NamedTemporaryFile(mode='w', suffix='.html', delete=False)
            map_obj.save(temp_file.name)
            temp_file.close()
            
            # Load in web view
            self.map_view.setUrl(QUrl.fromLocalFile(temp_file.name))
            
            self.log_widget.log_info(f"Map generated with {len(hosts_with_geo)} locations")
        
        except Exception as e:
            self.log_widget.log_error(f"Failed to generate map: {str(e)}")
            self.logger.error(f"Map generation error: {e}", exc_info=True)
    
    def on_export_json(self):
        """Export results to JSON."""
        if not self.current_scan_id:
            QMessageBox.warning(self, "No Data", "No scan data to export.")
            return
        
        scan_data = self.database.get_scan(self.current_scan_id)
        hosts = self.database.get_scan_hosts(self.current_scan_id)
        
        try:
            filepath = self.reporter.generate_json_report(scan_data, hosts)
            self.log_widget.log_success(f"JSON report exported to {filepath}")
            QMessageBox.information(self, "Export Complete", f"Report saved to:\n{filepath}")
        except Exception as e:
            self.log_widget.log_error(f"Export failed: {str(e)}")
            QMessageBox.critical(self, "Export Error", str(e))
    
    def on_export_csv(self):
        """Export results to CSV."""
        if not self.current_scan_id:
            QMessageBox.warning(self, "No Data", "No scan data to export.")
            return
        
        hosts = self.database.get_scan_hosts(self.current_scan_id)
        
        try:
            filepath = self.reporter.generate_csv_report(hosts)
            self.log_widget.log_success(f"CSV report exported to {filepath}")
            QMessageBox.information(self, "Export Complete", f"Report saved to:\n{filepath}")
        except Exception as e:
            self.log_widget.log_error(f"Export failed: {str(e)}")
            QMessageBox.critical(self, "Export Error", str(e))
    
    def on_export_html(self):
        """Export results to HTML with embedded map."""
        if not self.current_scan_id:
            QMessageBox.warning(self, "No Data", "No scan data to export.")
            return
        
        scan_data = self.database.get_scan(self.current_scan_id)
        hosts = self.database.get_scan_hosts(self.current_scan_id)
        
        try:
            # Generate map first
            hosts_with_geo = [h for h in hosts if h.get('latitude') and h.get('longitude')]
            map_path = None
            
            if hosts_with_geo:
                import tempfile
                temp_map = tempfile.NamedTemporaryFile(mode='w', suffix='.html', delete=False)
                self.map_generator.save_map(
                    self.map_generator.create_map(hosts_with_geo),
                    temp_map.name
                )
                map_path = temp_map.name
            
            # Generate HTML report
            filepath = self.reporter.generate_html_report(scan_data, hosts, map_html_path=map_path)
            self.log_widget.log_success(f"HTML report exported to {filepath}")
            QMessageBox.information(self, "Export Complete", f"Report saved to:\n{filepath}")
        
        except Exception as e:
            self.log_widget.log_error(f"Export failed: {str(e)}")
            QMessageBox.critical(self, "Export Error", str(e))
    
    def closeEvent(self, event):
        """Handle window close event."""
        # Stop any running scan
        if self.current_worker and self.current_worker.isRunning():
            reply = QMessageBox.question(
                self,
                "Scan in Progress",
                "A scan is currently running. Are you sure you want to quit?",
                QMessageBox.Yes | QMessageBox.No,
                QMessageBox.No
            )
            
            if reply == QMessageBox.Yes:
                self.current_worker.cancel()
                self.current_worker.wait()
            else:
                event.ignore()
                return
        
        # Close database
        self.database.close()
        
        # Close geolocation service
        import asyncio
        try:
            loop = asyncio.get_event_loop()
            if loop.is_running():
                loop.create_task(self.geolocation_service.close())
            else:
                loop.run_until_complete(self.geolocation_service.close())
        except:
            pass
        
        event.accept()
