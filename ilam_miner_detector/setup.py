#!/usr/bin/env python3
"""
Setup script for Ilam Miner Detector.
"""

from setuptools import setup, find_packages
from pathlib import Path

# Read README
readme_path = Path(__file__).parent / "README.md"
long_description = readme_path.read_text() if readme_path.exists() else ""

setup(
    name="ilam-miner-detector",
    version="1.0.0",
    author="Security Team",
    description="A security tool for detecting cryptocurrency mining operations in Ilam province, Iran",
    long_description=long_description,
    long_description_content_type="text/markdown",
    packages=find_packages(),
    install_requires=[
        "PyQt5>=5.15.0",
        "requests>=2.28.0",
        "folium>=0.14.0",
    ],
    python_requires=">=3.8",
    entry_points={
        "console_scripts": [
            "ilam-miner-detector=main:main",
        ],
        "gui_scripts": [
            "ilam-miner-detector-gui=gui.main_window:main",
        ],
    },
    classifiers=[
        "Development Status :: 4 - Beta",
        "Intended Audience :: System Administrators",
        "Topic :: Security",
        "Topic :: System :: Networking",
        "License :: OSI Approved :: MIT License",
        "Programming Language :: Python :: 3",
        "Programming Language :: Python :: 3.8",
        "Programming Language :: Python :: 3.9",
        "Programming Language :: Python :: 3.10",
        "Programming Language :: Python :: 3.11",
    ],
    keywords="security network scanner cryptocurrency miner detection",
    project_urls={
        "Source": "https://github.com/example/ilam-miner-detector",
        "Tracker": "https://github.com/example/ilam-miner-detector/issues",
    },
)
