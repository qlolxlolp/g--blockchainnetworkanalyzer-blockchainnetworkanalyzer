@echo off
echo Running BlockchainNetworkAnalyzer with error logging...
echo.

cd /d "%~dp0bin\Release\net8.0-windows"

echo Current directory: %CD%
echo.

echo Checking for appsettings.json...
if exist appsettings.json (
    echo ✓ appsettings.json found
) else (
    echo ✗ appsettings.json NOT FOUND!
    pause
    exit /b 1
)

echo.
echo Starting application...
echo If the application doesn't start, check for error messages.
echo.

BlockchainNetworkAnalyzer.exe

echo.
echo Application exited with code %ERRORLEVEL%
pause

