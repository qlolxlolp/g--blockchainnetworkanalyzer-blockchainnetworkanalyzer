
@echo off
REM Complete Commercial Build Script
REM اسکریپت ساخت کامل نسخه تجاری

echo ============================================================
echo Blockchain Network Analyzer - Commercial Edition Builder
echo سازنده نسخه تجاری Blockchain Network Analyzer
echo ============================================================
echo.

cd /d "%~dp0"

REM Step 1: Clean all previous builds
echo [1/5] Cleaning previous builds...
echo [1/5] پاک‌سازی ساخت‌های قبلی...
if exist "bin" rmdir /s /q "bin"
if exist "obj" rmdir /s /q "obj"
if exist "Portable_Release" rmdir /s /q "Portable_Release"
if exist "Installer_Output" rmdir /s /q "Installer_Output"
echo      ✓ Cleanup completed
echo.

REM Step 2: Restore NuGet packages
echo [2/5] Restoring NuGet packages...
echo [2/5] بازیابی بسته‌های NuGet...
dotnet restore
if %ERRORLEVEL% neq 0 (
    echo      ✗ ERROR: Failed to restore packages
    pause
    exit /b 1
)
echo      ✓ Packages restored
echo.

REM Step 3: Build Release version
echo [3/5] Building Release configuration...
echo [3/5] ساخت نسخه Release...
dotnet build -c Release --no-restore
if %ERRORLEVEL% neq 0 (
    echo      ✗ ERROR: Build failed
    pause
    exit /b 1
)
echo      ✓ Build completed
echo.

REM Step 4: Create portable release
echo [4/5] Creating portable release...
echo [4/5] ایجاد نسخه قابل حمل...
call build_portable.bat
if %ERRORLEVEL% neq 0 (
    echo      ✗ ERROR: Portable release failed
    pause
    exit /b 1
)
echo      ✓ Portable release created
echo.

REM Step 5: Create installer
echo [5/5] Creating installer...
echo [5/5] ایجاد نصب‌کننده...
cd Installer
call build_installer.bat
if %ERRORLEVEL% neq 0 (
    echo      ✗ ERROR: Installer creation failed
    cd ..
    pause
    exit /b 1
)
cd ..
echo      ✓ Installer created
echo.

echo ============================================================
echo ✓ Commercial build completed successfully!
echo ✓ ساخت نسخه تجاری با موفقیت انجام شد!
echo ============================================================
echo.
echo Outputs / خروجی‌ها:
echo   - Portable: Portable_Release\BlockchainNetworkAnalyzer.exe
echo   - Installer: Installer_Output\Install_BlockchainNetworkAnalyzer.exe
echo.
echo You can now distribute these files!
echo می‌توانید این فایل‌ها را منتشر کنید!
echo.
pause
