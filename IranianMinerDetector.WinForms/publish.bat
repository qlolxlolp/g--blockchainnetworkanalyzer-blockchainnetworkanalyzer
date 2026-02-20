@echo off
echo ========================================
echo Iranian Miner Detector - Build Script
echo ========================================
echo.

echo Step 1: Restoring NuGet packages...
dotnet restore IranianMinerDetector.WinForms.csproj
if %errorlevel% neq 0 (
    echo ERROR: Failed to restore packages
    pause
    exit /b 1
)

echo.
echo Step 2: Building the project...
dotnet build IranianMinerDetector.WinForms.csproj --configuration Release
if %errorlevel% neq 0 (
    echo ERROR: Build failed
    pause
    exit /b 1
)

echo.
echo Step 3: Publishing as self-contained single-file executable...
dotnet publish IranianMinerDetector.WinForms.csproj ^
    --configuration Release ^
    --runtime win-x64 ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -p:EnableCompressionInSingleFile=true ^
    -p:PublishReadyToRun=true ^
    -p:PublishTrimmed=false ^
    --output publish

if %errorlevel% neq 0 (
    echo ERROR: Publish failed
    pause
    exit /b 1
)

echo.
echo ========================================
echo Build completed successfully!
echo ========================================
echo.
echo Output location: publish\IranianMinerDetector.WinForms.exe
echo File size:
dir publish\*.exe | find "IranianMinerDetector.WinForms.exe"
echo.
echo You can distribute this single .exe file to any Windows 10/11 machine.
echo No installation or .NET runtime required.
echo.
pause
