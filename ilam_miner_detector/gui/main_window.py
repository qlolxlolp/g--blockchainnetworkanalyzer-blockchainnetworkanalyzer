"""
Main window for Ilam Miner Detector GUI.
"""

import os
import sys
import logging
from pathlib import Path
from datetime import datetime
from typing import Optional

from PyQt5.QtWidgets import (
    QMainWindow, QWidget, QVBoxLayout, QHBoxLayout,
    QPushButton, QTabWidget, QLabel, QMenuBar, QMenu,
    QAction, QFileDialog, QMessageBox, QSplitter,
    QStatusBar, QToolBar, QFrame, QGroupBox,
    QDialog, QDialogButtonBox, QTextBrowser, QComboBox
)
from PyQt5.QtCore import Qt, QThread, pyqtSignal
from PyQt5.QtGui import QIcon, QFont, QKeySequence

# Add parent to path for imports
sys.path.insert(0, str(Path(__file__).parent.parent.parent))

from ..config_manager import get_config_manager
from ..database import get_db_manager, ScanRecord
from ..worker import ScanWorker, GeolocationWorker, ReportWorker
from ..reporter import get_report_generator
from ..map_generator import get_map_generator
from .widgets import LogWidget, ResultsTableWidget, ScanConfigWidget, ProgressWidget


class AboutDialog(QDialog):
    """About dialog."""
    
    def __init__(self, parent=None):
        super().__init__(parent)
        self.setWindowTitle("About Ilam Miner Detector")
        self.setFixedSize(500, 400)
        
        layout = QVBoxLayout(self)
        
        text = QTextBrowser()
        text.setOpenExternalLinks(True)
        text.setHtml("""
        <h2>Ilam Miner Detector v1.0.0</h2>
        <p>A security tool for detecting cryptocurrency mining operations in Ilam province, Iran.</p>
        
        <h3>Features</h3>
        <ul>
            <li>Real network scanning with TCP port checks</li>
            <li>Geolocation with ip-api.com integration</li>
            <li>Interactive map visualization with Folium</li>
            <li>SQLite database for result storage</li>
            <li>JSON, CSV, and HTML report generation</li>
        </ul>
        
        <h3>Legal Notice</h3>
        <p>This tool is for <b>authorized security auditing only</b>. Users must have explicit 
        permission to scan target networks. Unauthorized scanning may violate laws and regulations.</p>
        
        <p style="color: #666; font-size: 11px; margin-top: 20px;">
        © 2024 Security Team. All rights reserved.
        </p>
        """)
        layout.addWidget(text)
        
        buttons = QDialogButtonBox(QDialogButtonBox.Ok)
        buttons.accepted.connect(self.accept)
        layout.addWidget(buttons)


class MainWindow(QMainWindow):
    """
    Main application window for Ilam Miner Detector.
    """
    
    def __init__(self):
        super().__init__()
        
        self.setWindowTitle("Ilam Miner Detector")
        self.setMinimumSize(1200, 800)
        
        # Initialize components
        self.config = get_config_manager().get()
        self.db = get_db_manager()
        
        # Workers
        self.scan_worker: Optional[ScanWorker] = None
        self.geo_worker: Optional[GeolocationWorker] = None
        self.report_worker: Optional[ReportWorker] = None
        
        # Current scan state
        self.current_scan_id: Optional[int] = None
        self.scanning = False
        
        self._setup_ui()
        self._setup_menu()
        self._setup_toolbar()
        self._setup_statusbar()
        self._setup_logging()
        
        self.log_widget.append_log("Ilam Miner Detector initialized", "INFO")
        self.log_widget.append_log(f"Database: {self.config.database.db_path}", "INFO")
    
    def _setup_ui(self):
        """Setup the main UI."""
        # Central widget
        central = QWidget()
        self.setCentralWidget(central)
        
        main_layout = QHBoxLayout(central)
        main_layout.setSpacing(10)
        
        # Main splitter
        splitter = QSplitter(Qt.Horizontal)
        main_layout.addWidget(splitter)
        
        # Left panel - Configuration
        left_panel = QWidget()
        left_layout = QVBoxLayout(left_panel)
        left_layout.setContentsMargins(10, 10, 10, 10)
        
        # Scan configuration widget
        self.scan_config = ScanConfigWidget()
        left_layout.addWidget(self.scan_config)
        
        # Control buttons
        buttons_group = QGroupBox("Controls")
        buttons_layout = QVBoxLayout(buttons_group)
        
        self.start_btn = QPushButton("▶ Start Scan")
        self.start_btn.setStyleSheet("""
            QPushButton {
                background-color: #4CAF50;
                color: white;
                font-weight: bold;
                padding: 10px;
            }
            QPushButton:hover { background-color: #45a049; }
            QPushButton:disabled { background-color: #cccccc; }
        """)
        self.start_btn.clicked.connect(self._start_scan)
        buttons_layout.addWidget(self.start_btn)
        
        self.stop_btn = QPushButton("⏹ Stop Scan")
        self.stop_btn.setStyleSheet("""
            QPushButton {
                background-color: #f44336;
                color: white;
                font-weight: bold;
                padding: 10px;
            }
            QPushButton:hover { background-color: #da190b; }
            QPushButton:disabled { background-color: #cccccc; }
        """)
        self.stop_btn.setEnabled(False)
        self.stop_btn.clicked.connect(self._stop_scan)
        buttons_layout.addWidget(self.stop_btn)
        
        self.export_btn = QPushButton("📊 Export Reports")
        self.export_btn.clicked.connect(self._export_reports)
        buttons_layout.addWidget(self.export_btn)
        
        left_layout.addWidget(buttons_group)
        
        # Progress widget
        self.progress_widget = ProgressWidget()
        left_layout.addWidget(self.progress_widget)
        
        left_layout.addStretch()
        
        splitter.addWidget(left_panel)
        
        # Right panel - Results and logs
        right_panel = QWidget()
        right_layout = QVBoxLayout(right_panel)
        right_layout.setContentsMargins(10, 10, 10, 10)
        
        # Tab widget
        self.tabs = QTabWidget()
        
        # Results tab
        results_widget = QWidget()
        results_layout = QVBoxLayout(results_widget)
        
        self.results_table = ResultsTableWidget()
        self.results_table.row_selected.connect(self._on_result_selected)
        results_layout.addWidget(self.results_table)
        
        # Results filter
        filter_layout = QHBoxLayout()
        filter_layout.addWidget(QLabel("Filter:"))
        self.filter_combo = QComboBox()
        self.filter_combo.addItems(["All Results", "Miners Only", "Online Only"])
        self.filter_combo.currentTextChanged.connect(self._on_filter_changed)
        filter_layout.addWidget(self.filter_combo)
        filter_layout.addStretch()
        results_layout.addLayout(filter_layout)
        
        self.tabs.addTab(results_widget, "Results")
        
        # Map tab
        self.map_widget = QTextBrowser()
        self.map_widget.setPlaceholderText("Map will be displayed here after scan completion.")
        self.tabs.addTab(self.map_widget, "Map")
        
        # Log tab
        self.log_widget = LogWidget()
        self.tabs.addTab(self.log_widget, "Log")
        
        right_layout.addWidget(self.tabs)
        
        splitter.addWidget(right_panel)
        
        # Set splitter proportions
        splitter.setSizes([350, 850])
    
    def _setup_menu(self):
        """Setup menu bar."""
        menubar = self.menuBar()
        
        # File menu
        file_menu = menubar.addMenu("&File")
        
        new_scan_action = QAction("&New Scan", self)
        new_scan_action.setShortcut(QKeySequence.New)
        new_scan_action.triggered.connect(self._new_scan)
        file_menu.addAction(new_scan_action)
        
        file_menu.addSeparator()
        
        export_action = QAction("&Export Reports...", self)
        export_action.triggered.connect(self._export_reports)
        file_menu.addAction(export_action)
        
        file_menu.addSeparator()
        
        exit_action = QAction("E&xit", self)
        exit_action.setShortcut(QKeySequence.Quit)
        exit_action.triggered.connect(self.close)
        file_menu.addAction(exit_action)
        
        # Tools menu
        tools_menu = menubar.addMenu("&Tools")
        
        view_db_action = QAction("&View Database Stats", self)
        view_db_action.triggered.connect(self._view_db_stats)
        tools_menu.addAction(view_db_action)
        
        clear_cache_action = QAction("&Clear Geolocation Cache", self)
        clear_cache_action.triggered.connect(self._clear_geo_cache)
        tools_menu.addAction(clear_cache_action)
        
        # Help menu
        help_menu = menubar.addMenu("&Help")
        
        about_action = QAction("&About", self)
        about_action.triggered.connect(self._show_about)
        help_menu.addAction(about_action)
    
    def _setup_toolbar(self):
        """Setup toolbar."""
        toolbar = QToolBar("Main Toolbar")
        self.addToolBar(toolbar)
        
        toolbar.addAction("New Scan", self._new_scan)
        toolbar.addSeparator()
        toolbar.addAction("Export", self._export_reports)
    
    def _setup_statusbar(self):
        """Setup status bar."""
        self.statusbar = QStatusBar()
        self.setStatusBar(self.statusbar)
        self.statusbar.showMessage("Ready")
    
    def _setup_logging(self):
        """Setup logging to GUI."""
        # Create custom handler
        class GuiLogHandler(logging.Handler):
            def __init__(self, widget):
                super().__init__()
                self.widget = widget
            
            def emit(self, record):
                msg = self.format(record)
                self.widget.append_log(msg, record.levelname)
        
        # Add handler to root logger
        handler = GuiLogHandler(self.log_widget)
        handler.setFormatter(logging.Formatter('%(message)s'))
        logging.getLogger().addHandler(handler)
    
    def _start_scan(self):
        """Start a new scan."""
        # Validate CIDR
        if not self.scan_config._validate_cidr():
            QMessageBox.warning(self, "Invalid Input", "Please enter a valid CIDR range.")
            return
        
        cidr = self.scan_config.get_cidr()
        
        # Confirm for large ranges
        from ..ip_manager import get_ip_manager
        ip_manager = get_ip_manager()
        try:
            info = ip_manager.parse_cidr(cidr)
            if info.total_hosts > 10000:
                reply = QMessageBox.question(
                    self, "Large Scan Range",
                    f"This will scan {info.total_hosts:,} hosts. Continue?",
                    QMessageBox.Yes | QMessageBox.No
                )
                if reply != QMessageBox.Yes:
                    return
        except:
            pass
        
        # Create scan record
        scan_name = f"Scan {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}"
        self.current_scan_id = self.db.create_scan(scan_name, cidr)
        
        # Clear previous results
        self.results_table.clear_results()
        self.progress_widget.reset()
        
        # Create and start worker
        self.scan_worker = ScanWorker(self.current_scan_id, cidr, scan_name, self)
        self.scan_worker.progress_updated.connect(self._on_scan_progress)
        self.scan_worker.host_scanned.connect(self._on_host_scanned)
        self.scan_worker.scan_completed.connect(self._on_scan_completed)
        self.scan_worker.scan_error.connect(self._on_scan_error)
        self.scan_worker.log_message.connect(self._log_message)
        
        self.scan_worker.start()
        
        # Update UI state
        self.scanning = True
        self.start_btn.setEnabled(False)
        self.stop_btn.setEnabled(True)
        self.statusbar.showMessage(f"Scanning {cidr}...")
        
        self._log_message(f"Started scan of {cidr}")
    
    def _stop_scan(self):
        """Stop the current scan."""
        if self.scan_worker and self.scan_worker.isRunning():
            self.scan_worker.cancel()
            self._log_message("Stopping scan...")
    
    def _on_scan_progress(self, current: int, total: int, status: str):
        """Handle scan progress update."""
        self.progress_widget.set_progress(current, total)
        self.progress_widget.set_status(status)
        self.statusbar.showMessage(f"Scanning: {current}/{total}")
    
    def _on_host_scanned(self, result):
        """Handle host scan result."""
        self.results_table.add_result(result)
        
        # Update stats
        responsive = sum(1 for i in range(self.results_table.rowCount()) 
                        if self.results_table.get_result(i) and 
                        self.results_table.get_result(i).is_responsive)
        miners = sum(1 for i in range(self.results_table.rowCount()) 
                    if self.results_table.get_result(i) and 
                    self.results_table.get_result(i).is_miner_detected)
        
        self.progress_widget.set_stats(responsive, miners)
    
    def _on_scan_completed(self, complete_info):
        """Handle scan completion."""
        self.scanning = False
        self.start_btn.setEnabled(True)
        self.stop_btn.setEnabled(False)
        
        if complete_info.success:
            self.statusbar.showMessage(
                f"Scan complete. Found {complete_info.miners_detected} potential miners."
            )
            self._log_message(
                f"Scan completed. Total: {complete_info.total_hosts}, "
                f"Responsive: {complete_info.responsive_hosts}, "
                f"Miners: {complete_info.miners_detected}"
            )
            
            # Auto-generate map
            self._generate_map()
            
            # Auto-export if enabled
            if self.config.reporting.auto_export:
                self._export_reports()
        else:
            self.statusbar.showMessage(f"Scan failed: {complete_info.error_message}")
            self._log_message(f"Scan failed: {complete_info.error_message}", "ERROR")
    
    def _on_scan_error(self, error_message: str):
        """Handle scan error."""
        QMessageBox.critical(self, "Scan Error", error_message)
    
    def _on_result_selected(self, row: int):
        """Handle result row selection."""
        result = self.results_table.get_result(row)
        if result:
            self._log_message(f"Selected: {result.ip_address}")
    
    def _on_filter_changed(self, filter_text: str):
        """Handle filter change."""
        if filter_text == "Miners Only":
            self.results_table.filter_miners_only(True)
        elif filter_text == "Online Only":
            # Show only responsive hosts
            for row in range(self.results_table.rowCount()):
                result = self.results_table.get_result(row)
                self.results_table.setRowHidden(row, result and not result.is_responsive)
        else:
            # Show all
            for row in range(self.results_table.rowCount()):
                self.results_table.setRowHidden(row, False)
    
    def _generate_map(self):
        """Generate map for current results."""
        if not self.current_scan_id:
            return
        
        try:
            # Get miner hosts
            miners = self.db.get_miner_hosts(self.current_scan_id)
            
            if not miners:
                self.map_widget.setText("No miners detected to display on map.")
                return
            
            # Get geolocation data
            from ..geolocation import get_geolocation_service
            geo_service = get_geolocation_service()
            
            map_data = []
            for miner in miners:
                geo = geo_service.lookup(miner.ip_address)
                if geo.success:
                    import json
                    try:
                        ports = json.loads(miner.open_ports) if miner.open_ports else []
                    except:
                        ports = []
                    
                    map_data.append({
                        'ip_address': miner.ip_address,
                        'latitude': geo.latitude,
                        'longitude': geo.longitude,
                        'confidence_score': miner.confidence_score,
                        'miner_type': miner.miner_type,
                        'city': geo.city,
                        'region': geo.region,
                        'country': geo.country,
                        'isp': geo.isp,
                        'open_ports': ports
                    })
            
            if map_data:
                map_gen = get_map_generator()
                map_path = self.config.reporting.reports_dir
                Path(map_path).mkdir(parents=True, exist_ok=True)
                
                timestamp = datetime.now().strftime(self.config.reporting.timestamp_format)
                output_path = Path(map_path) / f"map_scan_{self.current_scan_id}_{timestamp}.html"
                
                map_gen.create_summary_map(map_data, str(output_path))
                
                # Display map path
                self.map_widget.setText(f"Map generated: {output_path}")
                self._log_message(f"Map saved to {output_path}")
            else:
                self.map_widget.setText("Could not geolocate any miner addresses.")
                
        except Exception as e:
            self._log_message(f"Map generation failed: {e}", "ERROR")
            self.map_widget.setText(f"Map generation failed: {e}")
    
    def _export_reports(self):
        """Export scan reports."""
        if not self.current_scan_id:
            QMessageBox.warning(self, "No Scan", "No scan data to export.")
            return
        
        self.report_worker = ReportWorker(self.current_scan_id, parent=self)
        self.report_worker.report_generated.connect(self._on_report_generated)
        self.report_worker.report_error.connect(self._on_report_error)
        self.report_worker.log_message.connect(self._log_message)
        self.report_worker.start()
    
    def _on_report_generated(self, fmt: str, path: str):
        """Handle report generation."""
        self._log_message(f"Generated {fmt.upper()} report: {path}")
        self.statusbar.showMessage(f"Report saved: {path}")
    
    def _on_report_error(self, fmt: str, error: str):
        """Handle report error."""
        self._log_message(f"Failed to generate {fmt} report: {error}", "ERROR")
    
    def _new_scan(self):
        """Reset for new scan."""
        if self.scanning:
            reply = QMessageBox.question(
                self, "Scan in Progress",
                "A scan is currently running. Start a new one?",
                QMessageBox.Yes | QMessageBox.No
            )
            if reply != QMessageBox.Yes:
                return
            self._stop_scan()
        
        self.results_table.clear_results()
        self.progress_widget.reset()
        self.map_widget.clear()
        self.current_scan_id = None
        self.statusbar.showMessage("Ready for new scan")
    
    def _view_db_stats(self):
        """View database statistics."""
        stats = self.db.get_stats()
        msg = f"""Database Statistics:
        
Total Scans: {stats['total_scans']}
Total Hosts: {stats['total_hosts']}
Total Miners: {stats['total_miners']}
Geolocation Cache: {stats['geolocation_cache_entries']} entries
"""
        QMessageBox.information(self, "Database Statistics", msg)
    
    def _clear_geo_cache(self):
        """Clear geolocation cache."""
        from ..geolocation import get_geolocation_service
        
        geo_service = get_geolocation_service()
        removed = geo_service.clean_cache()
        self._log_message(f"Cleared {removed} expired geolocation cache entries")
        QMessageBox.information(self, "Cache Cleared", f"Removed {removed} expired entries.")
    
    def _show_about(self):
        """Show about dialog."""
        dialog = AboutDialog(self)
        dialog.exec_()
    
    def _log_message(self, message: str, level: str = "INFO"):
        """Log a message to the log widget."""
        self.log_widget.append_log(message, level)
    
    def closeEvent(self, event):
        """Handle window close event."""
        if self.scanning:
            reply = QMessageBox.question(
                self, "Scan in Progress",
                "A scan is currently running. Close anyway?",
                QMessageBox.Yes | QMessageBox.No
            )
            if reply == QMessageBox.No:
                event.ignore()
                return
            self._stop_scan()
        
        event.accept()


def main():
    """Main entry point for GUI."""
    from PyQt5.QtWidgets import QApplication
    
    app = QApplication(sys.argv)
    app.setApplicationName("Ilam Miner Detector")
    app.setStyle('Fusion')
    
    # Apply dark palette
    from PyQt5.QtGui import QPalette, QColor
    palette = QPalette()
    palette.setColor(QPalette.Window, QColor(53, 53, 53))
    palette.setColor(QPalette.WindowText, Qt.white)
    palette.setColor(QPalette.Base, QColor(25, 25, 25))
    palette.setColor(QPalette.AlternateBase, QColor(53, 53, 53))
    palette.setColor(QPalette.ToolTipBase, Qt.white)
    palette.setColor(QPalette.ToolTipText, Qt.white)
    palette.setColor(QPalette.Text, Qt.white)
    palette.setColor(QPalette.Button, QColor(53, 53, 53))
    palette.setColor(QPalette.ButtonText, Qt.white)
    palette.setColor(QPalette.BrightText, Qt.red)
    palette.setColor(QPalette.Link, QColor(42, 130, 218))
    palette.setColor(QPalette.Highlight, QColor(42, 130, 218))
    palette.setColor(QPalette.HighlightedText, Qt.black)
    app.setPalette(palette)
    
    window = MainWindow()
    window.show()
    
    sys.exit(app.exec_())


if __name__ == "__main__":
    main()
