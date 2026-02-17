#!/usr/bin/env python3
"""
Ilam Miner Detector - Main entry point

A network security tool for detecting cryptocurrency miners in Ilam province, Iran.

WARNING: Only use this tool on networks you have explicit authorization to scan.
Unauthorized network scanning may be illegal in your jurisdiction.
"""

import sys
import os
import logging
import argparse
from pathlib import Path
from PyQt5.QtWidgets import QApplication

# Add project root to path
sys.path.insert(0, str(Path(__file__).parent))

from ilam_miner_detector.config_manager import ConfigManager
from ilam_miner_detector.gui.main_window import MainWindow


def setup_logging(verbose: bool = False):
    """
    Configure logging.
    
    Args:
        verbose: Enable verbose logging
    """
    log_dir = Path("data/logs")
    log_dir.mkdir(parents=True, exist_ok=True)
    
    log_level = logging.DEBUG if verbose else logging.INFO
    
    # File handler
    file_handler = logging.FileHandler(
        log_dir / "ilam_miner_detector.log",
        encoding='utf-8'
    )
    file_handler.setLevel(logging.DEBUG)
    file_formatter = logging.Formatter(
        '%(asctime)s - %(name)s - %(levelname)s - %(message)s'
    )
    file_handler.setFormatter(file_formatter)
    
    # Console handler
    console_handler = logging.StreamHandler()
    console_handler.setLevel(log_level)
    console_formatter = logging.Formatter(
        '%(levelname)s: %(message)s'
    )
    console_handler.setFormatter(console_formatter)
    
    # Root logger
    root_logger = logging.getLogger()
    root_logger.setLevel(logging.DEBUG)
    root_logger.addHandler(file_handler)
    root_logger.addHandler(console_handler)


def main():
    """Main application entry point."""
    parser = argparse.ArgumentParser(
        description="Ilam Miner Detector - Network Security Tool",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  python main.py
  python main.py --config config/custom_config.json --verbose

WARNING: Only use this tool on networks you have explicit authorization to scan.
        """
    )
    
    parser.add_argument(
        '--config', '-c',
        type=str,
        default='config/config.json',
        help='Path to configuration file (default: config/config.json)'
    )
    
    parser.add_argument(
        '--verbose', '-v',
        action='store_true',
        help='Enable verbose logging'
    )
    
    parser.add_argument(
        '--create-config',
        action='store_true',
        help='Create default configuration file and exit'
    )
    
    args = parser.parse_args()
    
    # Setup logging
    setup_logging(args.verbose)
    logger = logging.getLogger(__name__)
    
    logger.info("=" * 60)
    logger.info("Ilam Miner Detector v1.0.0")
    logger.info("=" * 60)
    
    # Create default config if requested
    if args.create_config:
        config_path = Path(args.config)
        config_path.parent.mkdir(parents=True, exist_ok=True)
        
        config = ConfigManager()
        config.save(args.config)
        
        logger.info(f"Default configuration saved to {args.config}")
        print(f"✓ Configuration file created: {args.config}")
        print("  Edit this file to customize scan settings.")
        return 0
    
    # Load configuration
    try:
        config_path = Path(args.config)
        if not config_path.exists():
            logger.warning(f"Config file not found: {args.config}")
            logger.info("Using default configuration")
            logger.info(f"Tip: Run with --create-config to generate a config file")
        
        config = ConfigManager(args.config if config_path.exists() else None)
        logger.info(f"Configuration loaded")
        
    except Exception as e:
        logger.error(f"Failed to load configuration: {e}")
        return 1
    
    # Print warning
    print("\n" + "=" * 60)
    print("⚠️  WARNING: AUTHORIZED USE ONLY")
    print("=" * 60)
    print("This tool performs network scanning and should only be used on")
    print("networks you have explicit authorization to scan. Unauthorized")
    print("network scanning may be illegal in your jurisdiction.")
    print("=" * 60 + "\n")
    
    # Create and run Qt application
    try:
        app = QApplication(sys.argv)
        app.setApplicationName("Ilam Miner Detector")
        app.setOrganizationName("Security Research")
        
        # Create main window
        window = MainWindow(config)
        window.show()
        
        logger.info("Application started")
        
        # Run event loop
        exit_code = app.exec_()
        
        logger.info("Application closed")
        return exit_code
    
    except Exception as e:
        logger.error(f"Application error: {e}", exc_info=True)
        return 1


if __name__ == '__main__':
    sys.exit(main())
