# Build and Distribution Guide

## Building the Application

### Prerequisites

**For Development:**
- .NET 8 SDK
- Visual Studio 2022 or VS Code
- Windows 10/11

**For Running:**
- Windows 10/11
- WebView2 Runtime (usually pre-installed on Windows 10/11)
- OR self-contained build (includes .NET runtime)

### Build Options

#### Option 1: Debug Build (Development)
```bash
cd IranianMinerDetector.WinForms
dotnet build
```
**Output:** `bin\Debug\net8.0-windows\`
**Size:** ~50MB
**Requires:** .NET 8 Runtime on target machine

#### Option 2: Release Build (Production)
```bash
cd IranianMinerDetector.WinForms
dotnet build -c Release
```
**Output:** `bin\Release\net8.0-windows\`
**Size:** ~40MB
**Requires:** .NET 8 Runtime on target machine

#### Option 3: Self-Contained Build (Recommended for Distribution)
```bash
cd IranianMinerDetector.WinForms
dotnet publish -c Release -r win-x64 --self-contained ^
  -p:PublishSingleFile=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:EnableCompressionInSingleFile=true ^
  -p:PublishReadyToRun=true
```
**Output:** `bin\Release\net8.0-windows\win-x64\publish\`
**Size:** ~80-100MB
**Requires:** Nothing (includes .NET runtime)

**Using PowerShell:**
```powershell
cd IranianMinerDetector.WinForms
dotnet publish -c Release -r win-x64 --self-contained `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true `
  -p:PublishReadyToRun=true
```

#### Option 4: Using Build Script
```bash
cd IranianMinerDetector.WinForms
build.bat
```

### Build Script Details

The `build.bat` script:
1. Cleans previous build artifacts
2. Runs dotnet publish with recommended settings
3. Reports success or failure
4. Pauses for review

### What Gets Built?

**Self-Contained Build Produces:**
- `IranianMinerDetector.WinForms.exe` - Main executable (~80-100MB)
- `appsettings.json` - Configuration file

**Additional Files to Include:**
- `README.md` - Documentation
- `QUICKSTART.md` - Quick start guide
- `app.ico` - Application icon (optional, embedded in exe)

## Distribution Package

### Create Distribution Folder

```bash
mkdir Distribution
cd Distribution

# Copy files
copy ..\bin\Release\net8.0-windows\win-x64\publish\IranianMinerDetector.WinForms.exe .
copy ..\appsettings.json .
copy ..\README.md .
copy ..\QUICKSTART.md .
```

### Directory Structure for Distribution

```
IranianMinerDetector-v1.0/
├── IranianMinerDetector.WinForms.exe
├── appsettings.json
├── README.md
└── QUICKSTART.md
```

### Compression (Optional)

```bash
# Create ZIP archive
powershell Compress-Archive -Path * -DestinationPath IranianMinerDetector-v1.0.zip
```

## Installation for End Users

### Option 1: Portable (No Installation)

1. Download the distribution package
2. Extract to any folder
3. Run `IranianMinerDetector.WinForms.exe`
4. Application creates AppData folder automatically

### Option 2: Installer (Using Inno Setup)

Create `setup.iss`:

```ini
[Setup]
AppName=Iranian Miner Detector
AppVersion=1.0.0
DefaultDirName={commonpf}\IranianMinerDetector
DefaultGroupName=Iranian Miner Detector
OutputBaseFilename=IranianMinerDetector-Setup
Compression=lzma
SolidCompression=yes

[Files]
Source: "IranianMinerDetector.WinForms.exe"; DestDir: "{app}"
Source: "appsettings.json"; DestDir: "{app}"
Source: "README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "QUICKSTART.md"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Iranian Miner Detector"; Filename: "{app}\IranianMinerDetector.WinForms.exe"
Name: "{commondesktop}\Iranian Miner Detector"; Filename: "{app}\IranianMinerDetector.WinForms.exe"

[Run]
Filename: "{app}\IranianMinerDetector.WinForms.exe"; Description: "Launch application"; Flags: nowait postinstall skipifsilent
```

Build installer:
```bash
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" setup.iss
```

### Option 3: MSIX Packaging (Windows Store)

Create `Package.appxmanifest` or use Visual Studio's packaging tools.

## Deployment Scenarios

### Scenario 1: Single User / Portable

**Distribution:**
- Upload ZIP file to file sharing service
- Share download link
- User extracts and runs

**Pros:**
- No installation required
- No admin privileges needed
- Easy to test and distribute

**Cons:**
- Larger file size
- User must extract manually

### Scenario 2: Corporate Deployment

**Distribution:**
- Create MSI installer using WiX Toolset
- Deploy via Group Policy or SCCM
- Install to Program Files

**Pros:**
- Standard deployment process
- Easy updates
- Professional appearance

**Cons:**
- Requires installer tooling
- Admin privileges needed

### Scenario 3: Web Distribution

**Distribution:**
- Host on website
- Provide download link
- Include version checking

**Pros:**
- Easy to update
- Reach wide audience
- Can implement auto-updates

**Cons:**
- Requires web hosting
- Bandwidth costs

## Version Management

### Version Numbers

Update `IranianMinerDetector.WinForms.csproj`:

```xml
<Version>1.0.1</Version>
<AssemblyVersion>1.0.1.0</AssemblyVersion>
<FileVersion>1.0.1.0</FileVersion>
```

### Changelog

Maintain `CHANGELOG.md`:

```markdown
# Changelog

## [1.0.1] - 2024-02-18
### Added
- Feature X
- Feature Y

### Fixed
- Bug fix

## [1.0.0] - 2024-02-01
### Initial Release
- All core features
```

## Testing Before Distribution

### Pre-Distribution Checklist

- [ ] Build succeeds without errors
- [ ] Application launches successfully
- [ ] All features work as expected
- [ ] Network scanning works
- [ ] Map generation works
- [ ] Report generation works
- [ ] Database creation works
- [ ] Right-to-left layout displays correctly
- [ ] No memory leaks during extended use
- [ ] Appropriate error handling

### Test on Clean Machine

1. Copy only executable to clean Windows machine
2. Run application
3. Test all features
4. Verify database creation
5. Verify map generation
6. Test report exports

## Signing (Optional)

### Code Signing

If you have a code signing certificate:

```bash
signtool sign /f certificate.pfx /p password ^
  /t http://timestamp.digicert.com ^
  /fd sha256 ^
  IranianMinerDetector.WinForms.exe
```

### Benefits of Signing
- Reduced antivirus false positives
- Professional appearance
- Better trust from users

## Troubleshooting Builds

### Common Build Issues

**"WebView2 not found"**
- Install WebView2 Runtime
- Or remove WebView2 references

**"SQLite not found"**
- Ensure System.Data.SQLite package installed
- Restore NuGet packages: `dotnet restore`

**"Publish failed"**
- Ensure .NET 8 SDK installed
- Clear NuGet cache: `dotnet nuget locals all --clear`
- Rebuild: `dotnet clean && dotnet build`

### Common Runtime Issues

**"Application won't start"**
- Verify .NET 8 runtime installed (for non-self-contained)
- Check Windows version compatibility
- Run as Administrator

**"Maps don't display"**
- Install WebView2 Runtime
- Check internet connectivity

**"Database errors"**
- Check write permissions to AppData
- Ensure sufficient disk space
- Delete corrupted database file

## Performance Optimization

### Build-Time Optimizations

**Faster builds:**
```bash
dotnet build -c Release -p:PublishReadyToRun=false
```

**Smaller size:**
```bash
dotnet publish -p:PublishTrimmed=true
```

**Balance:**
```bash
dotnet publish -c Release -r win-x64 --self-contained ^
  -p:PublishSingleFile=true ^
  -p:IncludeNativeLibrariesForSelfExtract=false
```

### Runtime Optimizations

**Edit appsettings.json:**
```json
{
  "Network": {
    "MaxConcurrency": 100
  },
  "Geolocation": {
    "CacheDurationHours": 24
  }
}
```

## Support and Updates

### Providing Support

Include in README:
- Version information
- System requirements
- Troubleshooting section
- Contact information

### Update Mechanism

**Manual:**
1. User downloads new version
2. Replaces executable
3. Settings preserved in AppData

**Automatic (future):**
- Implement update checker
- Download and install updates
- Preserve database and settings

## Legal Considerations

### Distribution

- Ensure compliance with open-source licenses
- Include license file in distribution
- Attribute third-party libraries

### Usage

- Network scanning may be regulated
- Include disclaimer in documentation
- Recommend legal use only

---

## Quick Distribution Steps

```bash
# 1. Build self-contained version
cd IranianMinerDetector.WinForms
dotnet publish -c Release -r win-x64 --self-contained ^
  -p:PublishSingleFile=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:EnableCompressionInSingleFile=true ^
  -p:PublishReadyToRun=true

# 2. Create distribution folder
mkdir ../Distribution-v1.0
copy bin\Release\net8.0-windows\win-x64\publish\IranianMinerDetector.WinForms.exe ..\Distribution-v1.0\
copy appsettings.json ..\Distribution-v1.0\
copy README.md ..\Distribution-v1.0\
copy QUICKSTART.md ..\Distribution-v1.0\

# 3. Create archive (optional)
cd ../Distribution-v1.0
powershell Compress-Archive -Path * -DestinationPath IranianMinerDetector-v1.0.zip

# 4. Ready to distribute!
# The zip file contains everything needed to run the application
```

**Success!** Your application is ready for distribution.

---

**Document Version:** 1.0
**Last Updated:** February 2024
**Application Version:** 1.0.0
