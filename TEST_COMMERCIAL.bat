
@echo off
REM Commercial Version Testing Script
REM اسکریپت تست نسخه تجاری

echo ============================================================
echo Commercial Version Testing
echo تست نسخه تجاری
echo ============================================================
echo.

cd /d "%~dp0"

echo [TEST 1/5] Checking build configuration...
echo [تست 1/5] بررسی پیکربندی ساخت...

if not exist "BlockchainNetworkAnalyzer.csproj" (
    echo ✗ ERROR: Project file not found
    pause
    exit /b 1
)
echo ✓ Project file OK
echo.

echo [TEST 2/5] Checking required files...
echo [تست 2/5] بررسی فایل‌های مورد نیاز...

set FILES_OK=1
if not exist "appsettings.json" set FILES_OK=0
if not exist "app.ico" set FILES_OK=0
if not exist "config.default.json" set FILES_OK=0

if %FILES_OK%==0 (
    echo ✗ ERROR: Some required files are missing
    pause
    exit /b 1
)
echo ✓ Required files OK
echo.

echo [TEST 3/5] Testing build...
echo [تست 3/5] تست ساخت...

dotnet build -c Release --no-restore > nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo ✗ ERROR: Build test failed
    echo Running detailed build...
    dotnet build -c Release
    pause
    exit /b 1
)
echo ✓ Build test passed
echo.

echo [TEST 4/5] Checking portable build capability...
echo [تست 4/5] بررسی قابلیت ساخت portable...

if not exist "build_portable.bat" (
    echo ✗ ERROR: build_portable.bat not found
    pause
    exit /b 1
)
echo ✓ Portable build script OK
echo.

echo [TEST 5/5] Checking installer capability...
echo [تست 5/5] بررسی قابلیت installer...

if not exist "Installer\installer.iss" (
    echo ✗ WARNING: Installer script not found
    echo   This is optional but recommended
)
echo ✓ Installer check complete
echo.

echo ============================================================
echo ✓ All tests passed!
echo ✓ تمام تست‌ها موفق بود!
echo ============================================================
echo.
echo Ready to build commercial version!
echo آماده برای ساخت نسخه تجاری!
echo.
echo Run BUILD_COMMERCIAL.bat to create the commercial release
echo برای ساخت نسخه تجاری، BUILD_COMMERCIAL.bat را اجرا کنید
echo.
pause
