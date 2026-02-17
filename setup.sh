#!/bin/bash
# Ilam Miner Detector - Setup Script

set -e

echo "==========================================="
echo "Ilam Miner Detector - Setup"
echo "==========================================="
echo ""

# Check Python version
echo "[1/5] Checking Python version..."
PYTHON_CMD=""

if command -v python3 &> /dev/null; then
    PYTHON_CMD="python3"
elif command -v python &> /dev/null; then
    PYTHON_CMD="python"
else
    echo "❌ Error: Python is not installed"
    echo "   Please install Python 3.8 or higher"
    exit 1
fi

PYTHON_VERSION=$($PYTHON_CMD --version 2>&1 | awk '{print $2}')
echo "✓ Found Python $PYTHON_VERSION"

# Check pip
echo ""
echo "[2/5] Checking pip..."
if ! $PYTHON_CMD -m pip --version &> /dev/null; then
    echo "❌ Error: pip is not installed"
    echo "   Please install pip for Python 3"
    exit 1
fi
echo "✓ pip is installed"

# Install dependencies
echo ""
echo "[3/5] Installing dependencies..."
echo "   This may take a few minutes..."
$PYTHON_CMD -m pip install -r requirements.txt --quiet

if [ $? -eq 0 ]; then
    echo "✓ Dependencies installed successfully"
else
    echo "❌ Error: Failed to install dependencies"
    echo "   Try manually: pip install -r requirements.txt"
    exit 1
fi

# Create directories
echo ""
echo "[4/5] Creating directories..."
mkdir -p data/logs
mkdir -p reports
mkdir -p config
echo "✓ Directories created"

# Create default configuration
echo ""
echo "[5/5] Creating default configuration..."
if [ ! -f "config/config.json" ]; then
    $PYTHON_CMD main.py --create-config
    echo "✓ Configuration created: config/config.json"
else
    echo "⚠ Configuration already exists: config/config.json"
    echo "  Delete it first if you want to recreate defaults"
fi

# Summary
echo ""
echo "==========================================="
echo "✅ Setup Complete!"
echo "==========================================="
echo ""
echo "To start the application:"
echo "  $PYTHON_CMD main.py"
echo ""
echo "For help:"
echo "  $PYTHON_CMD main.py --help"
echo ""
echo "Quick start guide: QUICKSTART.md"
echo "Full documentation: ILAM_MINER_DETECTOR_README.md"
echo ""
echo "⚠️  WARNING: Only scan networks you have"
echo "   explicit authorization to scan!"
echo ""
