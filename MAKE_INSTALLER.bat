@echo off
REM Quick script to build installer from portable release
REM اسکریپت سریع برای ساخت نصب‌کننده از نسخه قابل حمل

echo ========================================
echo Blockchain Network Analyzer - Installer Builder
echo ساخت‌کننده نصب‌کننده
echo ========================================
echo.

cd /d "%~dp0"

REM Check if portable release exists
if not exist "Portable_Release\BlockchainNetworkAnalyzer.exe" (
    echo Portable release not found. Building it first...
    echo نسخه قابل حمل پیدا نشد. در حال ساخت...
    echo.
    call build_portable.bat
    if %ERRORLEVEL% neq 0 (
        echo ERROR: Failed to build portable release
        pause
        exit /b 1
    )
    echo.
)

echo Portable release ready - OK
echo.

REM Build installer
cd Installer
call build_installer.bat

cd ..

echo.
echo ========================================
echo Done! Check Installer_Output folder
echo انجام شد! پوشه Installer_Output را بررسی کنید
echo ========================================
pause

