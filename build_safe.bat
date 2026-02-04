@echo off
REM Safe build script that closes running processes first
REM اسکریپت build امن که ابتدا فرآیندهای در حال اجرا را می‌بندد

echo ========================================
echo Safe Build Script
echo اسکریپت Build امن
echo ========================================
echo.

cd /d "%~dp0"

echo Step 1: Closing running processes...
echo مرحله 1: بستن فرآیندهای در حال اجرا...
taskkill /F /IM BlockchainNetworkAnalyzer.exe >nul 2>&1
timeout /t 2 /nobreak >nul
echo Done.
echo.

echo Step 2: Cleaning previous build...
echo مرحله 2: پاک کردن build قبلی...
dotnet clean >nul 2>&1
echo Done.
echo.

echo Step 3: Building project...
echo مرحله 3: ساخت پروژه...
dotnet build -c Release -r win-x64

if %ERRORLEVEL% equ 0 (
    echo.
    echo ========================================
    echo Build completed successfully!
    echo ساخت با موفقیت انجام شد!
    echo ========================================
) else (
    echo.
    echo ========================================
    echo Build failed!
    echo ساخت ناموفق بود!
    echo ========================================
    pause
    exit /b 1
)

pause

