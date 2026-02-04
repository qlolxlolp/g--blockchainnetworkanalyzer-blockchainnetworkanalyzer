@echo off
REM Build script for creating installer from portable release
REM اسکریپت ساخت نصب‌کننده از نسخه قابل حمل

echo ========================================
echo Building Installer from Portable Release
echo ساخت نصب‌کننده از نسخه قابل حمل
echo ========================================
echo.

cd /d "%~dp0"

REM Check if portable release exists
if not exist "..\Portable_Release\BlockchainNetworkAnalyzer.exe" (
    echo ERROR: Portable release not found!
    echo Please run build_portable.bat first to create the portable version.
    echo.
    echo خطا: نسخه قابل حمل پیدا نشد!
    echo لطفاً ابتدا build_portable.bat را اجرا کنید.
    pause
    exit /b 1
)

echo Portable release found - OK
echo.

REM Check for Inno Setup
set INNO_SETUP_PATH=
if exist "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" (
    set INNO_SETUP_PATH=C:\Program Files (x86)\Inno Setup 6\ISCC.exe
) else if exist "C:\Program Files\Inno Setup 6\ISCC.exe" (
    set INNO_SETUP_PATH=C:\Program Files\Inno Setup 6\ISCC.exe
) else if exist "%LOCALAPPDATA%\Programs\Inno Setup\ISCC.exe" (
    set INNO_SETUP_PATH=%LOCALAPPDATA%\Programs\Inno Setup\ISCC.exe
)

if "%INNO_SETUP_PATH%"=="" (
    echo.
    echo ========================================
    echo WARNING: Inno Setup not found!
    echo ========================================
    echo.
    echo Please install Inno Setup from:
    echo https://jrsoftware.org/isdl.php
    echo.
    echo Or download portable version and extract to:
    echo %CD%\InnoSetup\
    echo.
    echo ========================================
    echo.
    echo لطفاً Inno Setup را نصب کنید از:
    echo https://jrsoftware.org/isdl.php
    echo.
    pause
    exit /b 1
)

echo Inno Setup found: %INNO_SETUP_PATH%
echo.

REM Create output directory
if not exist "..\Installer_Output" mkdir "..\Installer_Output"

echo Compiling installer...
"%INNO_SETUP_PATH%" "installer.iss"

if %ERRORLEVEL% equ 0 (
    echo.
    echo ========================================
    echo Installer created successfully!
    echo نصب‌کننده با موفقیت ایجاد شد!
    echo ========================================
    echo.
    echo Output: ..\Installer_Output\Install_BlockchainNetworkAnalyzer.exe
    echo.
) else (
    echo.
    echo ERROR: Failed to create installer
    echo خطا: ایجاد نصب‌کننده ناموفق بود
    echo.
    pause
    exit /b 1
)

pause

