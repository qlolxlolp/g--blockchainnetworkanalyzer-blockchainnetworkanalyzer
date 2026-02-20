#!/bin/bash

echo "========================================"
echo "Iranian Miner Detector - Build Script"
echo "========================================"
echo ""

echo "Step 1: Restoring NuGet packages..."
dotnet restore IranianMinerDetector.WinForms.csproj
if [ $? -ne 0 ]; then
    echo "ERROR: Failed to restore packages"
    exit 1
fi

echo ""
echo "Step 2: Building the project..."
dotnet build IranianMinerDetector.WinForms.csproj --configuration Release
if [ $? -ne 0 ]; then
    echo "ERROR: Build failed"
    exit 1
fi

echo ""
echo "Step 3: Publishing as self-contained single-file executable..."
dotnet publish IranianMinerDetector.WinForms.csproj \
    --configuration Release \
    --runtime win-x64 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -p:EnableCompressionInSingleFile=true \
    -p:PublishReadyToRun=true \
    -p:PublishTrimmed=false \
    --output publish

if [ $? -ne 0 ]; then
    echo "ERROR: Publish failed"
    exit 1
fi

echo ""
echo "========================================"
echo "Build completed successfully!"
echo "========================================"
echo ""
echo "Output location: publish/IranianMinerDetector.WinForms.exe"
echo "File size:"
ls -lh publish/*.exe
echo ""
echo "You can distribute this single .exe file to any Windows 10/11 machine."
echo "No installation or .NET runtime required."
echo ""
