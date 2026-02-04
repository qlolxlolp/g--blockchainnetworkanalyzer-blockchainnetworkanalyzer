# PowerShell script to create portable package
# اسکریپت PowerShell برای ایجاد بسته قابل حمل

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Creating Portable Package" -ForegroundColor Cyan
Write-Host "ایجاد بسته قابل حمل" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptPath

# Clean previous builds
Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
if (Test-Path "Portable_Release") {
    Remove-Item -Recurse -Force "Portable_Release"
}
Write-Host ""

# Create directories
Write-Host "Creating directories..." -ForegroundColor Yellow
New-Item -ItemType Directory -Path "Portable_Release" -Force | Out-Null
New-Item -ItemType Directory -Path "Portable_Release\Config" -Force | Out-Null
Write-Host "✓ Directories created" -ForegroundColor Green
Write-Host ""

# Publish self-contained executable
Write-Host "Publishing self-contained executable..." -ForegroundColor Yellow
Write-Host "This may take a few minutes..." -ForegroundColor Gray

dotnet publish -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:IncludeAllContentForSelfExtract=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -p:PublishReadyToRun=true `
    -p:PublishTrimmed=false `
    -o "bin\Release\net8.0-windows\win-x64\publish"

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to publish" -ForegroundColor Red
    exit 1
}
Write-Host "✓ Publishing completed" -ForegroundColor Green
Write-Host ""

# Copy files
Write-Host "Copying files..." -ForegroundColor Yellow
Copy-Item "bin\Release\net8.0-windows\win-x64\publish\BlockchainNetworkAnalyzer.exe" "Portable_Release\" -Force
Copy-Item "appsettings.json" "Portable_Release\" -Force
Copy-Item "appsettings.json" "Portable_Release\Config\" -Force

if (Test-Path "app.manifest") {
    Copy-Item "app.manifest" "Portable_Release\" -Force
}
Write-Host "✓ Files copied" -ForegroundColor Green
Write-Host ""

# Get file size
$exePath = "Portable_Release\BlockchainNetworkAnalyzer.exe"
if (Test-Path $exePath) {
    $fileSize = (Get-Item $exePath).Length / 1MB
    Write-Host "Executable size: $([math]::Round($fileSize, 2)) MB" -ForegroundColor Cyan
    Write-Host ""
}

Write-Host "========================================" -ForegroundColor Green
Write-Host "Package created successfully!" -ForegroundColor Green
Write-Host "بسته با موفقیت ایجاد شد!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Location: $((Get-Location).Path)\Portable_Release" -ForegroundColor Yellow
Write-Host ""
Write-Host "You can now:" -ForegroundColor Cyan
Write-Host "  1. Copy the entire 'Portable_Release' folder to any Windows PC" -ForegroundColor White
Write-Host "  2. Double-click BlockchainNetworkAnalyzer.exe to run" -ForegroundColor White
Write-Host "  3. No installation required!" -ForegroundColor White
Write-Host ""







