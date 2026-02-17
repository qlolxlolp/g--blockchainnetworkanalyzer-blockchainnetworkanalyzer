"""
Custom PyQt5 widgets for Ilam Miner Detector GUI.
"""

from PyQt5.QtWidgets import (QWidget, QVBoxLayout, QHBoxLayout, QGroupBox,
                             QLabel, QLineEdit, QPushButton, QSpinBox,
                             QCheckBox, QTableWidget, QTableWidgetItem,
                             QTextEdit, QHeaderView, QAbstractItemView)
from PyQt5.QtCore import Qt, pyqtSignal
from PyQt5.QtGui import QColor
import json


class ScanConfigWidget(QWidget):
    """Widget for configuring scan parameters."""
    
    scan_requested = pyqtSignal(dict)  # Emits config dictionary
    
    def __init__(self, default_ports=None):
        super().__init__()
        self.default_ports = default_ports or [3333, 4444, 8332, 8333, 8545]
        self.init_ui()
    
    def init_ui(self):
        """Initialize UI components."""
        layout = QVBoxLayout()
        
        # Target configuration
        target_group = QGroupBox("Target Configuration")
        target_layout = QVBoxLayout()
        
        target_layout.addWidget(QLabel("IP Range (CIDR, range, or comma-separated):"))
        self.target_input = QLineEdit()
        self.target_input.setPlaceholderText("e.g., 192.168.1.0/24 or 10.0.0.1-10.0.0.254")
        target_layout.addWidget(self.target_input)
        
        target_layout.addWidget(QLabel("Ports to scan (comma-separated):"))
        self.ports_input = QLineEdit()
        self.ports_input.setText(','.join(map(str, self.default_ports)))
        target_layout.addWidget(self.ports_input)
        
        target_group.setLayout(target_layout)
        layout.addWidget(target_group)
        
        # Scan options
        options_group = QGroupBox("Scan Options")
        options_layout = QVBoxLayout()
        
        # Timeout
        timeout_layout = QHBoxLayout()
        timeout_layout.addWidget(QLabel("Timeout (ms):"))
        self.timeout_spin = QSpinBox()
        self.timeout_spin.setRange(500, 10000)
        self.timeout_spin.setValue(3000)
        self.timeout_spin.setSingleStep(500)
        timeout_layout.addWidget(self.timeout_spin)
        timeout_layout.addStretch()
        options_layout.addLayout(timeout_layout)
        
        # Max concurrent
        concurrent_layout = QHBoxLayout()
        concurrent_layout.addWidget(QLabel("Max Concurrent:"))
        self.concurrent_spin = QSpinBox()
        self.concurrent_spin.setRange(1, 200)
        self.concurrent_spin.setValue(50)
        self.concurrent_spin.setSingleStep(10)
        concurrent_layout.addWidget(self.concurrent_spin)
        concurrent_layout.addStretch()
        options_layout.addLayout(concurrent_layout)
        
        # Checkboxes
        self.ping_check = QCheckBox("Enable ICMP Ping")
        self.ping_check.setChecked(True)
        options_layout.addWidget(self.ping_check)
        
        self.banner_check = QCheckBox("Enable Banner Grabbing")
        self.banner_check.setChecked(True)
        options_layout.addWidget(self.banner_check)
        
        self.geolocation_check = QCheckBox("Enable Geolocation Lookup")
        self.geolocation_check.setChecked(True)
        options_layout.addWidget(self.geolocation_check)
        
        self.filter_ilam_check = QCheckBox("Filter for Ilam Region Only")
        self.filter_ilam_check.setChecked(True)
        options_layout.addWidget(self.filter_ilam_check)
        
        options_group.setLayout(options_layout)
        layout.addWidget(options_group)
        
        # Start button
        self.start_button = QPushButton("Start Scan")
        self.start_button.clicked.connect(self.on_start_scan)
        self.start_button.setStyleSheet("""
            QPushButton {
                background-color: #4CAF50;
                color: white;
                font-size: 14px;
                font-weight: bold;
                padding: 10px;
                border: none;
                border-radius: 5px;
            }
            QPushButton:hover {
                background-color: #45a049;
            }
            QPushButton:disabled {
                background-color: #cccccc;
            }
        """)
        layout.addWidget(self.start_button)
        
        layout.addStretch()
        self.setLayout(layout)
    
    def on_start_scan(self):
        """Validate and emit scan configuration."""
        target = self.target_input.text().strip()
        if not target:
            return
        
        ports_text = self.ports_input.text().strip()
        try:
            ports = [int(p.strip()) for p in ports_text.split(',') if p.strip()]
        except ValueError:
            return
        
        config = {
            'target': target,
            'ports': ports,
            'timeout': self.timeout_spin.value(),
            'max_concurrent': self.concurrent_spin.value(),
            'enable_ping': self.ping_check.isChecked(),
            'enable_banner': self.banner_check.isChecked(),
            'enable_geolocation': self.geolocation_check.isChecked(),
            'filter_ilam': self.filter_ilam_check.isChecked()
        }
        
        self.scan_requested.emit(config)
    
    def set_scan_running(self, running: bool):
        """Enable/disable controls during scan."""
        self.start_button.setEnabled(not running)
        self.target_input.setEnabled(not running)
        self.ports_input.setEnabled(not running)


class ResultsTableWidget(QTableWidget):
    """Table widget for displaying scan results."""
    
    def __init__(self):
        super().__init__()
        self.init_ui()
    
    def init_ui(self):
        """Initialize table structure."""
        self.setColumnCount(7)
        self.setHorizontalHeaderLabels([
            'IP Address', 'Hostname', 'Open Ports', 'Miner Type',
            'Location', 'ISP', 'Coordinates'
        ])
        
        # Configure table
        self.setSelectionBehavior(QAbstractItemView.SelectRows)
        self.setSelectionMode(QAbstractItemView.SingleSelection)
        self.setEditTriggers(QAbstractItemView.NoEditTriggers)
        self.setSortingEnabled(True)
        
        # Column widths
        header = self.horizontalHeader()
        header.setStretchLastSection(True)
        for i in range(self.columnCount()):
            header.setSectionResizeMode(i, QHeaderView.Interactive)
        
        self.setAlternatingRowColors(True)
    
    def add_host(self, host_data: dict):
        """
        Add a discovered host to the table.
        
        Args:
            host_data: Dictionary with host information
        """
        row = self.rowCount()
        self.insertRow(row)
        
        # IP Address
        ip_item = QTableWidgetItem(host_data.get('ip_address', ''))
        self.setItem(row, 0, ip_item)
        
        # Hostname
        hostname_item = QTableWidgetItem(host_data.get('hostname') or 'N/A')
        self.setItem(row, 1, hostname_item)
        
        # Open Ports
        ports = host_data.get('open_ports', [])
        ports_text = ', '.join(map(str, ports))
        ports_item = QTableWidgetItem(ports_text)
        self.setItem(row, 2, ports_item)
        
        # Miner Type
        miner_type = host_data.get('miner_type', '')
        is_miner = host_data.get('is_miner', False)
        miner_item = QTableWidgetItem(miner_type or 'N/A')
        
        if is_miner:
            miner_item.setBackground(QColor(255, 200, 200))
            miner_item.setForeground(QColor(150, 0, 0))
        
        self.setItem(row, 3, miner_item)
        
        # Location
        city = host_data.get('city', '')
        region = host_data.get('region', '')
        country = host_data.get('country', '')
        location = f"{city}, {region}, {country}" if city else 'N/A'
        location_item = QTableWidgetItem(location)
        self.setItem(row, 4, location_item)
        
        # ISP
        isp_item = QTableWidgetItem(host_data.get('isp') or 'N/A')
        self.setItem(row, 5, isp_item)
        
        # Coordinates
        lat = host_data.get('latitude')
        lon = host_data.get('longitude')
        coords = f"{lat:.4f}, {lon:.4f}" if lat and lon else 'N/A'
        coords_item = QTableWidgetItem(coords)
        self.setItem(row, 6, coords_item)
        
        # Highlight miner rows
        if is_miner:
            for col in range(self.columnCount()):
                item = self.item(row, col)
                if item:
                    item.setBackground(QColor(255, 235, 235))
    
    def clear_results(self):
        """Clear all results from table."""
        self.setRowCount(0)
    
    def get_all_hosts(self):
        """Get all host data from table."""
        hosts = []
        for row in range(self.rowCount()):
            host = {
                'ip_address': self.item(row, 0).text(),
                'hostname': self.item(row, 1).text(),
                'open_ports': self.item(row, 2).text(),
                'miner_type': self.item(row, 3).text(),
                'location': self.item(row, 4).text(),
                'isp': self.item(row, 5).text(),
                'coordinates': self.item(row, 6).text()
            }
            hosts.append(host)
        return hosts


class LogWidget(QTextEdit):
    """Widget for displaying log messages."""
    
    def __init__(self):
        super().__init__()
        self.setReadOnly(True)
        self.setMaximumHeight(150)
        self.setStyleSheet("""
            QTextEdit {
                background-color: #2b2b2b;
                color: #00ff00;
                font-family: 'Courier New', monospace;
                font-size: 10px;
            }
        """)
    
    def log_info(self, message: str):
        """Log info message."""
        self.append(f"<span style='color: #00ff00;'>[INFO] {message}</span>")
    
    def log_warning(self, message: str):
        """Log warning message."""
        self.append(f"<span style='color: #ffaa00;'>[WARN] {message}</span>")
    
    def log_error(self, message: str):
        """Log error message."""
        self.append(f"<span style='color: #ff0000;'>[ERROR] {message}</span>")
    
    def log_success(self, message: str):
        """Log success message."""
        self.append(f"<span style='color: #00ffff;'>[SUCCESS] {message}</span>")
