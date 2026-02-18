@echo off
echo Building Iranian Miner Detector - WinForms Edition...
echo.

REM Clean previous build
if exist "bin" rmdir /s /q "bin"
if exist "obj" rmdir /s /q "obj"

REM Build self-contained executable
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:PublishReadyToRun=true

if %ERRORLEVEL% EQU 0 (
    echo.
    echo Build successful!
    echo Output: bin\Release\net8.0-windows\win-x64\publish\
    echo.
    echo To distribute the application, copy the following files:
    echo   - IranianMinerDetector.WinForms.exe
    echo   - appsettings.json
    echo.
    pause
) else (
    echo.
    echo Build failed!
    pause
)
