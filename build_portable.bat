@echo off
REM Build script for creating a self-contained, portable executable
REM اسکریپت ساخت فایل اجرایی یکپارچه و قابل حمل

echo ========================================
echo Building Portable Self-Contained Executable
echo ساخت فایل اجرایی یکپارچه و قابل حمل
echo ========================================
echo.

cd /d "%~dp0"

echo Cleaning previous builds...
if exist "bin\Release\net8.0-windows\win-x64\publish" (
    rmdir /s /q "bin\Release\net8.0-windows\win-x64\publish"
)
if exist "Portable_Release" (
    rmdir /s /q "Portable_Release"
)
echo.

echo Restoring NuGet packages...
dotnet restore
if %ERRORLEVEL% neq 0 (
    echo ERROR: Failed to restore packages
    pause
    exit /b 1
)
echo.

echo Publishing self-contained single-file executable...
dotnet publish -c Release ^
    -r win-x64 ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -p:EnableCompressionInSingleFile=true ^
    -p:IncludeAllContentForSelfExtract=true ^
    -p:DebugType=None ^
    -p:DebugSymbols=false ^
    -p:PublishReadyToRun=true ^
    -p:PublishTrimmed=false ^
    -p:RuntimeFrameworkVersion=8.0.0 ^
    -p:Version=1.0.0 ^
    -p:AssemblyVersion=1.0.0.0 ^
    -p:FileVersion=1.0.0.0 ^
    --no-restore ^
    -o "bin\Release\net8.0-windows\win-x64\publish"

if %ERRORLEVEL% neq 0 (
    echo ERROR: Failed to publish
    pause
    exit /b 1
)
echo.

echo Creating Portable_Release directory...
mkdir "Portable_Release"
mkdir "Portable_Release\Config"
mkdir "Portable_Release\Data"
mkdir "Portable_Release\Logs"
echo.

echo Copying executable...
copy /Y "bin\Release\net8.0-windows\win-x64\publish\BlockchainNetworkAnalyzer.exe" "Portable_Release\"
if %ERRORLEVEL% neq 0 (
    echo ERROR: Failed to copy executable
    pause
    exit /b 1
)
echo.

echo Copying configuration files...
copy /Y "appsettings.json" "Portable_Release\"
copy /Y "appsettings.json" "Portable_Release\Config\"
if exist "app.manifest" (
    copy /Y "app.manifest" "Portable_Release\"
)
echo.

echo Creating README...
(
echo Blockchain Network Analyzer - Portable Edition
echo ==============================================
echo.
echo This is a self-contained, portable executable.
echo No installation or .NET runtime required!
echo.
echo USAGE / استفاده:
echo   1. Double-click BlockchainNetworkAnalyzer.exe
echo   2. The application will create necessary folders automatically
echo.
echo FILES / فایل‌ها:
echo   - BlockchainNetworkAnalyzer.exe: Main executable
echo   - appsettings.json: Configuration file
echo   - Config\: Configuration directory
echo   - Data\: Database and data files (auto-created)
echo   - Logs\: Log files (auto-created)
echo.
echo REQUIREMENTS / نیازمندی‌ها:
echo   - Windows 10/11 (64-bit)
echo   - No additional software required!
echo.
echo NOTE / توجه:
echo   - Administrator rights may be required for network scanning
echo   - First run may take a few seconds to extract files
echo.
) > "Portable_Release\README.txt"
echo.

echo ========================================
echo Build completed successfully!
echo ساخت با موفقیت انجام شد!
echo ========================================
echo.
echo Output location: Portable_Release\
echo Executable: Portable_Release\BlockchainNetworkAnalyzer.exe
echo.
echo You can now copy the entire Portable_Release folder to any Windows PC
echo and run BlockchainNetworkAnalyzer.exe directly!
echo.
pause

