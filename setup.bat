@echo off
REM Ilam Miner Detector - Windows Setup Script

echo ===========================================
echo Ilam Miner Detector - Setup
echo ===========================================
echo.

REM Check Python
echo [1/5] Checking Python version...
python --version >nul 2>&1
if errorlevel 1 (
    echo Error: Python is not installed
    echo Please install Python 3.8 or higher from python.org
    pause
    exit /b 1
)

for /f "tokens=2" %%i in ('python --version 2^>^&1') do set PYTHON_VERSION=%%i
echo Found Python %PYTHON_VERSION%

REM Check pip
echo.
echo [2/5] Checking pip...
python -m pip --version >nul 2>&1
if errorlevel 1 (
    echo Error: pip is not installed
    pause
    exit /b 1
)
echo pip is installed

REM Install dependencies
echo.
echo [3/5] Installing dependencies...
echo This may take a few minutes...
python -m pip install -r requirements.txt --quiet
if errorlevel 1 (
    echo Error: Failed to install dependencies
    echo Try manually: pip install -r requirements.txt
    pause
    exit /b 1
)
echo Dependencies installed successfully

REM Create directories
echo.
echo [4/5] Creating directories...
if not exist "data\logs" mkdir data\logs
if not exist "reports" mkdir reports
if not exist "config" mkdir config
echo Directories created

REM Create configuration
echo.
echo [5/5] Creating default configuration...
if not exist "config\config.json" (
    python main.py --create-config
    echo Configuration created: config\config.json
) else (
    echo Configuration already exists: config\config.json
    echo Delete it first if you want to recreate defaults
)

REM Summary
echo.
echo ===========================================
echo Setup Complete!
echo ===========================================
echo.
echo To start the application:
echo   python main.py
echo.
echo For help:
echo   python main.py --help
echo.
echo Quick start guide: QUICKSTART.md
echo Full documentation: ILAM_MINER_DETECTOR_README.md
echo.
echo WARNING: Only scan networks you have
echo explicit authorization to scan!
echo.
pause
