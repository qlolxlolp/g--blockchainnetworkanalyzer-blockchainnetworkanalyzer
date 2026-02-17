"""
Custom PyQt5 widgets for Ilam Miner Detector GUI.
"""

import logging
from typing import List, Optional, Callable

from PyQt5.QtWidgets import (
    QWidget, QVBoxLayout, QHBoxLayout, QLabel, QLineEdit,
    QPushButton, QSpinBox, QDoubleSpinBox, QCheckBox,
    QGroupBox, QTableWidget, QTableWidgetItem, QHeaderView,
    QTextEdit, QProgressBar, QComboBox, QFileDialog,
    QMessageBox, QSplitter, QFrame
)
from PyQt5.QtCore import Qt, pyqtSignal
from PyQt5.QtGui import QColor, QFont

from ..config_manager import get_config_manager


class LogWidget(QTextEdit):
    """
    Widget for displaying log messages with color coding.
    """
    
    def __init__(self, parent=None):
        super().__init__(parent)
        self.setReadOnly(True)
        self.setMaximumBlockCount(1000)  # Limit history
        
        # Set monospace font
        font = QFont("Consolas", 9)
        font.setStyleHint(QFont.Monospace)
        self.setFont(font)
        
        # Color scheme
        self.colors = {
            'DEBUG': '#808080',
            'INFO': '#000000',
            'WARNING': '#FF8C00',
            'ERROR': '#FF0000',
            'CRITICAL': '#8B0000'
        }
    
    def append_log(self, message: str, level: str = 'INFO'):
        """Append a log message with color coding."""
        color = self.colors.get(level, '#000000')
        timestamp = logging.time.strftime('%H:%M:%S') if hasattr(logging, 'time') else ''
        
        html = f'<span style="color: {color}">[{timestamp}] {message}</span>'
        self.append(html)
        
        # Auto-scroll to bottom
        scrollbar = self.verticalScrollBar()
        scrollbar.setValue(scrollbar.maximum())
    
    def clear_log(self):
        """Clear all log messages."""
        self.clear()


class ResultsTableWidget(QTableWidget):
    """
    Table widget for displaying scan results.
    """
    
    # Signal emitted when a row is double-clicked
    row_selected = pyqtSignal(int)  # row index
    
    def __init__(self, parent=None):
        super().__init__(parent)
        
        self.setColumnCount(7)
        self.setHorizontalHeaderLabels([
            'IP Address', 'Status', 'Ping', 'Open Ports', 
            'Miner', 'Type', 'Confidence'
        ])
        
        # Configure header
        header = self.horizontalHeader()
        header.setSectionResizeMode(QHeaderView.Stretch)
        header.setSectionResizeMode(0, QHeaderView.ResizeToContents)  # IP
        header.setSectionResizeMode(4, QHeaderView.ResizeToContents)  # Miner
        header.setSectionResizeMode(6, QHeaderView.ResizeToContents)  # Confidence
        
        self.setSelectionBehavior(QTableWidget.SelectRows)
        self.setSelectionMode(QTableWidget.SingleSelection)
        self.setAlternatingRowColors(True)
        
        # Connect signals
        self.cellDoubleClicked.connect(self._on_double_click)
        
        self._results = []
    
    def add_result(self, result):
        """Add a scan result to the table."""
        row = self.rowCount()
        self.insertRow(row)
        
        # Store result data
        self._results.append(result)
        
        # IP Address
        self.setItem(row, 0, QTableWidgetItem(result.ip_address))
        
        # Status
        status = "Online" if result.is_responsive else "Offline"
        status_item = QTableWidgetItem(status)
        status_item.setForeground(QColor('#008000') if result.is_responsive else QColor('#808080'))
        self.setItem(row, 1, status_item)
        
        # Ping
        ping_str = f"{result.ping_time_ms:.2f}ms" if result.ping_time_ms else "N/A"
        self.setItem(row, 2, QTableWidgetItem(ping_str))
        
        # Open Ports
        import json
        try:
            ports = json.loads(result.open_ports) if hasattr(result, 'open_ports') else []
            if isinstance(ports, list):
                ports_str = ', '.join(str(p) for p in ports[:3])
                if len(ports) > 3:
                    ports_str += f" (+{len(ports)-3})"
            else:
                ports_str = str(ports)
        except:
            ports_str = ""
        self.setItem(row, 3, QTableWidgetItem(ports_str))
        
        # Miner Detected
        miner_str = "Yes" if result.is_miner_detected else "No"
        miner_item = QTableWidgetItem(miner_str)
        if result.is_miner_detected:
            miner_item.setForeground(QColor('#FF0000'))
            miner_item.setFont(QFont("", weight=QFont.Bold))
        self.setItem(row, 4, miner_item)
        
        # Type
        miner_type = result.miner_type if result.is_miner_detected else ""
        self.setItem(row, 5, QTableWidgetItem(miner_type))
        
        # Confidence
        confidence = f"{result.confidence_score:.1f}%" if result.is_miner_detected else ""
        conf_item = QTableWidgetItem(confidence)
        if result.confidence_score >= 80:
            conf_item.setForeground(QColor('#FF0000'))
        elif result.confidence_score >= 50:
            conf_item.setForeground(QColor('#FF8C00'))
        self.setItem(row, 6, conf_item)
    
    def clear_results(self):
        """Clear all results."""
        self.setRowCount(0)
        self._results = []
    
    def get_result(self, row: int):
        """Get result data for a row."""
        if 0 <= row < len(self._results):
            return self._results[row]
        return None
    
    def filter_miners_only(self, show_only: bool):
        """Filter to show only miner detections."""
        for row in range(self.rowCount()):
            result = self._results[row] if row < len(self._results) else None
            if result:
                self.setRowHidden(row, show_only and not result.is_miner_detected)
    
    def _on_double_click(self, row: int, column: int):
        """Handle double click on row."""
        self.row_selected.emit(row)


class ScanConfigWidget(QWidget):
    """
    Widget for configuring scan parameters.
    """
    
    # Signals
    config_changed = pyqtSignal()
    
    def __init__(self, parent=None):
        super().__init__(parent)
        self.config = get_config_manager().get()
        self._setup_ui()
    
    def _setup_ui(self):
        layout = QVBoxLayout(self)
        
        # CIDR Range Group
        cidr_group = QGroupBox("Target Range")
        cidr_layout = QVBoxLayout(cidr_group)
        
        cidr_input_layout = QHBoxLayout()
        cidr_input_layout.addWidget(QLabel("CIDR:"))
        self.cidr_input = QLineEdit()
        self.cidr_input.setPlaceholderText("e.g., 192.168.1.0/24")
        cidr_input_layout.addWidget(self.cidr_input)
        
        self.validate_btn = QPushButton("Validate")
        self.validate_btn.clicked.connect(self._validate_cidr)
        cidr_input_layout.addWidget(self.validate_btn)
        
        cidr_layout.addLayout(cidr_input_layout)
        
        self.cidr_info = QLabel("")
        self.cidr_info.setStyleSheet("color: #666; font-size: 11px;")
        cidr_layout.addWidget(self.cidr_info)
        
        layout.addWidget(cidr_group)
        
        # Scan Options Group
        options_group = QGroupBox("Scan Options")
        options_layout = QVBoxLayout(options_group)
        
        # Timeout
        timeout_layout = QHBoxLayout()
        timeout_layout.addWidget(QLabel("Timeout (s):"))
        self.timeout_spin = QDoubleSpinBox()
        self.timeout_spin.setRange(0.5, 30.0)
        self.timeout_spin.setValue(self.config.scan.timeout)
        self.timeout_spin.setSingleStep(0.5)
        timeout_layout.addWidget(self.timeout_spin)
        timeout_layout.addStretch()
        options_layout.addLayout(timeout_layout)
        
        # Concurrency
        concurrency_layout = QHBoxLayout()
        concurrency_layout.addWidget(QLabel("Concurrency:"))
        self.concurrency_spin = QSpinBox()
        self.concurrency_spin.setRange(1, 500)
        self.concurrency_spin.setValue(self.config.scan.concurrency)
        concurrency_layout.addWidget(self.concurrency_spin)
        concurrency_layout.addStretch()
        options_layout.addLayout(concurrency_layout)
        
        # Checkboxes
        self.ping_check = QCheckBox("Enable Ping Check")
        self.ping_check.setChecked(self.config.scan.enable_ping)
        options_layout.addWidget(self.ping_check)
        
        self.banner_check = QCheckBox("Enable Banner Grabbing")
        self.banner_check.setChecked(self.config.scan.enable_banner_grab)
        options_layout.addWidget(self.banner_check)
        
        layout.addWidget(options_group)
        
        # Port Selection Group
        ports_group = QGroupBox("Ports to Scan")
        ports_layout = QVBoxLayout(ports_group)
        
        self.port_preset = QComboBox()
        self.port_preset.addItems([
            "Common Mining Ports",
            "Stratum Only",
            "Bitcoin Only",
            "Ethereum Only",
            "Custom"
        ])
        self.port_preset.currentTextChanged.connect(self._on_port_preset_changed)
        ports_layout.addWidget(self.port_preset)
        
        self.ports_input = QLineEdit()
        self.ports_input.setPlaceholderText("e.g., 3333,4444,8332")
        ports_layout.addWidget(self.ports_input)
        
        self._on_port_preset_changed("Common Mining Ports")
        
        layout.addWidget(ports_group)
        layout.addStretch()
    
    def get_cidr(self) -> str:
        """Get entered CIDR range."""
        return self.cidr_input.text().strip()
    
    def get_ports(self) -> List[int]:
        """Get list of ports to scan."""
        ports_text = self.ports_input.text()
        ports = []
        for part in ports_text.split(','):
            part = part.strip()
            if part.isdigit():
                ports.append(int(part))
        return ports if ports else self.config.miner_ports.all_ports
    
    def get_timeout(self) -> float:
        """Get timeout value."""
        return self.timeout_spin.value()
    
    def get_concurrency(self) -> int:
        """Get concurrency value."""
        return self.concurrency_spin.value()
    
    def is_ping_enabled(self) -> bool:
        """Check if ping is enabled."""
        return self.ping_check.isChecked()
    
    def is_banner_enabled(self) -> bool:
        """Check if banner grabbing is enabled."""
        return self.banner_check.isChecked()
    
    def _validate_cidr(self):
        """Validate the CIDR input."""
        from ..ip_manager import get_ip_manager
        
        cidr = self.cidr_input.text().strip()
        if not cidr:
            self.cidr_info.setText("Please enter a CIDR range")
            self.cidr_info.setStyleSheet("color: #FF0000;")
            return False
        
        ip_manager = get_ip_manager()
        
        try:
            info = ip_manager.parse_cidr(cidr)
            estimate_sec, estimate_str = ip_manager.estimate_scan_time(
                cidr, 
                self.timeout_spin.value(),
                self.concurrency_spin.value()
            )
            
            info_text = f"Hosts: {info.total_hosts:,} | Range: {info.first_ip} - {info.last_ip} | Est. time: {estimate_str}"
            if info.is_private:
                info_text += " | (Private Network)"
            
            self.cidr_info.setText(info_text)
            self.cidr_info.setStyleSheet("color: #008000;")
            return True
            
        except ValueError as e:
            self.cidr_info.setText(f"Invalid CIDR: {e}")
            self.cidr_info.setStyleSheet("color: #FF0000;")
            return False
    
    def _on_port_preset_changed(self, preset: str):
        """Handle port preset selection."""
        presets = {
            "Common Mining Ports": self.config.miner_ports.all_ports,
            "Stratum Only": self.config.miner_ports.stratum_ports,
            "Bitcoin Only": self.config.miner_ports.bitcoin_ports,
            "Ethereum Only": self.config.miner_ports.ethereum_ports,
        }
        
        if preset in presets:
            ports = presets[preset]
            self.ports_input.setText(','.join(str(p) for p in ports))
            self.ports_input.setEnabled(False)
        else:
            self.ports_input.setEnabled(True)


class ProgressWidget(QWidget):
    """
    Widget for displaying scan progress.
    """
    
    def __init__(self, parent=None):
        super().__init__(parent)
        self._setup_ui()
    
    def _setup_ui(self):
        layout = QVBoxLayout(self)
        
        # Progress bar
        self.progress_bar = QProgressBar()
        self.progress_bar.setRange(0, 100)
        self.progress_bar.setValue(0)
        self.progress_bar.setTextVisible(True)
        layout.addWidget(self.progress_bar)
        
        # Status labels
        status_layout = QHBoxLayout()
        
        self.status_label = QLabel("Ready")
        status_layout.addWidget(self.status_label)
        
        status_layout.addStretch()
        
        self.stats_label = QLabel("")
        status_layout.addWidget(self.stats_label)
        
        layout.addLayout(status_layout)
        
        # Separator
        line = QFrame()
        line.setFrameShape(QFrame.HLine)
        line.setStyleSheet("color: #ccc;")
        layout.addWidget(line)
    
    def set_progress(self, current: int, total: int):
        """Update progress bar."""
        if total > 0:
            percentage = int((current / total) * 100)
            self.progress_bar.setValue(percentage)
            self.progress_bar.setFormat(f"{current}/{total} ({percentage}%)")
        else:
            self.progress_bar.setValue(0)
            self.progress_bar.setFormat("Ready")
    
    def set_status(self, status: str):
        """Update status text."""
        self.status_label.setText(status)
    
    def set_stats(self, responsive: int, miners: int):
        """Update statistics display."""
        self.stats_label.setText(f"Responsive: {responsive} | Miners: {miners}")
    
    def reset(self):
        """Reset to initial state."""
        self.progress_bar.setValue(0)
        self.progress_bar.setFormat("Ready")
        self.status_label.setText("Ready")
        self.stats_label.setText("")
