# PowerShell script to build portable release and then create installer
# اسکریپت PowerShell برای ساخت نسخه قابل حمل و سپس ایجاد نصب‌کننده

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Building Portable Release and Installer" -ForegroundColor Cyan
Write-Host "ساخت نسخه قابل حمل و نصب‌کننده" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptPath

# Step 1: Build portable release
Write-Host "Step 1: Building portable release..." -ForegroundColor Yellow
Write-Host "مرحله 1: ساخت نسخه قابل حمل..." -ForegroundColor Yellow
Write-Host ""

$portableScript = Join-Path $scriptPath "..\create_portable_package.ps1"
if (Test-Path $portableScript) {
    & $portableScript
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Failed to build portable release" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "Portable script not found, checking if portable release exists..." -ForegroundColor Yellow
    $portableExe = Join-Path $scriptPath "..\Portable_Release\BlockchainNetworkAnalyzer.exe"
    if (-not (Test-Path $portableExe)) {
        Write-Host "ERROR: Portable release not found and cannot be built!" -ForegroundColor Red
        Write-Host "Please run build_portable.bat or create_portable_package.ps1 first" -ForegroundColor Yellow
        exit 1
    }
    Write-Host "Portable release found - OK" -ForegroundColor Green
}
Write-Host ""

# Step 2: Check for Inno Setup
Write-Host "Step 2: Checking for Inno Setup..." -ForegroundColor Yellow
Write-Host "مرحله 2: بررسی Inno Setup..." -ForegroundColor Yellow
Write-Host ""

$innoSetupPaths = @(
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe",
    "$env:LOCALAPPDATA\Programs\Inno Setup\ISCC.exe",
    "$scriptPath\InnoSetup\ISCC.exe"
)

$innoSetupPath = $null
foreach ($path in $innoSetupPaths) {
    if (Test-Path $path) {
        $innoSetupPath = $path
        break
    }
}

if ($null -eq $innoSetupPath) {
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "WARNING: Inno Setup not found!" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please install Inno Setup from:" -ForegroundColor Yellow
    Write-Host "https://jrsoftware.org/isdl.php" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Or download portable version and extract to:" -ForegroundColor Yellow
    Write-Host "$scriptPath\InnoSetup\" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "لطفاً Inno Setup را نصب کنید" -ForegroundColor Yellow
    Write-Host ""
    exit 1
}

Write-Host "Inno Setup found: $innoSetupPath" -ForegroundColor Green
Write-Host ""

# Step 3: Create output directory
Write-Host "Step 3: Creating output directory..." -ForegroundColor Yellow
Write-Host "مرحله 3: ایجاد پوشه خروجی..." -ForegroundColor Yellow
Write-Host ""

$outputDir = Join-Path $scriptPath "..\Installer_Output"
if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
}
Write-Host ""

# Step 4: Compile installer
Write-Host "Step 4: Compiling installer..." -ForegroundColor Yellow
Write-Host "مرحله 4: کامپایل نصب‌کننده..." -ForegroundColor Yellow
Write-Host ""

$issFile = Join-Path $scriptPath "installer.iss"
& $innoSetupPath $issFile

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "Installer created successfully!" -ForegroundColor Green
    Write-Host "نصب‌کننده با موفقیت ایجاد شد!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    
    $installerFile = Join-Path $outputDir "Install_BlockchainNetworkAnalyzer.exe"
    if (Test-Path $installerFile) {
        $fileSize = (Get-Item $installerFile).Length / 1MB
        Write-Host "Installer location: $installerFile" -ForegroundColor Cyan
        Write-Host "Installer size: $([math]::Round($fileSize, 2)) MB" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "You can now distribute this installer!" -ForegroundColor Green
        Write-Host "می‌توانید این نصب‌کننده را منتشر کنید!" -ForegroundColor Green
    }
} else {
    Write-Host ""
    Write-Host "ERROR: Failed to create installer" -ForegroundColor Red
    Write-Host "خطا: ایجاد نصب‌کننده ناموفق بود" -ForegroundColor Red
    exit 1
}

Write-Host ""

