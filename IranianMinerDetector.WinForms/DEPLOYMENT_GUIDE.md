# Deployment Guide - Iranian Miner Detector

This guide provides step-by-step instructions for building, packaging, and distributing the Iranian Miner Detector WinForms application.

## Prerequisites

### Development Environment
- **Windows 10/11** with latest updates
- **.NET 8 SDK** ([Download](https://dotnet.microsoft.com/download/dotnet/8.0))
- **Visual Studio 2022** (optional) or **Visual Studio Code**
- **Git** (for version control)

### Distribution Tools (Optional)
- **Inno Setup** ([Download](https://jrsoftware.org/isinfo.php)) - For creating Windows installer
- **Code Signing Certificate** - For signing the executable (recommended)

## Building the Application

### Step 1: Clone/Download Source Code

```bash
git clone <repository-url>
cd IranianMinerDetector.WinForms
```

### Step 2: Restore Dependencies

```powershell
dotnet restore
```

### Step 3: Build for Development

```powershell
dotnet build --configuration Release
```

### Step 4: Publish as Self-Contained Executable

#### Option A: Using Build Script (Windows)

```cmd
publish.bat
```

#### Option B: Using Build Script (Linux/Mac)

```bash
chmod +x publish.sh
./publish.sh
```

#### Option C: Manual Command

```powershell
dotnet publish IranianMinerDetector.WinForms.csproj `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:PublishReadyToRun=true `
    -p:PublishTrimmed=false `
    --output publish
```

### Expected Output

After publishing, you'll find:

```
publish/
└── IranianMinerDetector.WinForms.exe  (~50-100MB)
```

## Distribution Methods

### Method 1: Direct File Distribution (Simplest)

Simply distribute the single `IranianMinerDetector.WinForms.exe` file.

**Pros:**
- No installation required
- User can run immediately
- No admin privileges needed

**Cons:**
- No desktop shortcut created automatically
- No entry in "Add/Remove Programs"

**Instructions for Users:**
1. Download `IranianMinerDetector.WinForms.exe`
2. Double-click to run
3. No installation required

### Method 2: ZIP Archive with Documentation

Package the executable with documentation files.

**Files to Include:**
```
IranianMinerDetector.zip
├── IranianMinerDetector.WinForms.exe
├── README.txt
├── LICENSE.txt
└── QUICKSTART.txt
```

**Creation:**
```powershell
Compress-Archive -Path publish\*.exe, README.txt, LICENSE.txt -DestinationPath IranianMinerDetector.zip
```

### Method 3: Inno Setup Installer (Professional)

Create a professional Windows installer with the provided `installer.iss` file.

**Prerequisites:**
- Install [Inno Setup](https://jrsoftware.org/isinfo.php)

**Steps:**

1. Place `IranianMinerDetector.WinForms.exe` in the `publish/` directory
2. Ensure `README.md` and `LICENSE.txt` are present
3. Open `installer.iss` in Inno Setup Compiler
4. Click **Build** → **Compile**

**Output:**
```
installer_output/
└── IranianMinerDetector-Setup.exe  (~50-100MB)
```

**User Experience:**
- Professional installer wizard
- Desktop shortcut creation
- Start menu entry
- Uninstaller included
- Automatic WebView2 detection

### Method 4: Code-Signed Distribution (Enterprise)

For enterprise deployment or public distribution, code signing is recommended.

**Prerequisites:**
- Code Signing Certificate from a trusted CA (e.g., DigiCert, Sectigo)

**Signing the Executable:**

```powershell
# Sign the executable
signtool sign `
    /f certificate.pfx `
    /p password `
    /t http://timestamp.digicert.com `
    /fd sha256 `
    publish\IranianMinerDetector.WinForms.exe

# Verify signature
signtool verify /pa publish\IranianMinerDetector.WinForms.exe
```

**Benefits:**
- No "Unknown Publisher" warning
- Increased user trust
- Passes Windows SmartScreen more easily
- Required for some enterprise policies

## Distribution Channels

### Internal Distribution

For organization-internal use:

1. **Network Share**
   ```
   \\fileserver\software\IranianMinerDetector\
   ```

2. **Internal Software Portal**
   - Upload to company software catalog
   - Include version information
   - Document prerequisites

3. **Group Policy Deployment**
   ```powershell
   # Create a GPO to deploy to specific computers
   # Computer Configuration → Policies → Software Installation
   ```

### Public Distribution

For public release:

1. **GitHub Releases**
   ```bash
   # Create a release with tagged version
   gh release create v1.0.0 \
       publish\IranianMinerDetector.WinForms.exe \
       README.md \
       LICENSE.txt \
       --title "Version 1.0.0" \
       --notes "Initial stable release"
   ```

2. **Official Website**
   - Download page with direct links
   - Include checksums for verification
   - Provide system requirements

3. **Software Repositories**
   - Submit to [Microsoft Store](https://partner.microsoft.com/dashboard)
   - List on [Softpedia](https://www.softpedia.com/)
   - Upload to alternative download sites

### Enterprise Distribution

For enterprise customers:

1. **Microsoft Intune**
   ```xml
   <!-- IntuneWin32App configuration -->
   <InstallCommand>IranianMinerDetector-Setup.exe /SILENT /NORESTART</InstallCommand>
   <UninstallCommand>"C:\Program Files\IranianMinerDetector\unins000.exe" /SILENT</UninstallCommand>
   ```

2. **SCCM/Configuration Manager**
   - Create application package
   - Deploy to device collections
   - Set deployment type to "Required"

3. **MSI Wrapper (Alternative)**
   - Convert EXE to MSI using tools like [MSI Wrapper](https://www.exemsi.com/)
   - Easier for some deployment systems

## Versioning and Updates

### Semantic Versioning

Follow semantic versioning: `MAJOR.MINOR.PATCH`

- **MAJOR**: Breaking changes
- **MINOR**: New features
- **PATCH**: Bug fixes

Example: `1.0.0`, `1.1.0`, `1.1.1`

### Auto-Update Mechanism (Future Enhancement)

Implement automatic updates:

1. **Check for Updates** on startup
2. **Download Update** to temp directory
3. **Verify Checksum**
4. **Replace Files** on next restart
5. **Run Migration Scripts** if needed

### Update Distribution

```bash
# Create a versioned release
git tag v1.0.1
git push origin v1.0.1

# Build and publish
./publish.sh

# Create release notes
echo "Bug fixes and improvements" > release-notes.txt
```

## Testing Before Distribution

### Pre-Deployment Checklist

- [ ] Build completes without errors
- [ ] Application launches successfully on clean Windows 10 machine
- [ ] All features work as expected
- [ ] WebView2 maps display correctly
- [ ] Database operations function properly
- [ ] Export formats generate correctly
- [ ] Settings persist across sessions
- [ ] No memory leaks during extended use
- [ ] UI scales correctly on different DPI settings
- [ ] Application works without admin privileges

### Compatibility Testing

Test on:

- **Windows 10** (versions 1809, 1909, 2004, 21H1, 21H2, 22H2)
- **Windows 11** (21H2, 22H2)
- **Different DPI settings** (100%, 125%, 150%, 200%)
- **Various screen resolutions** (1366x768, 1920x1080, 2560x1440, 4K)

### Security Testing

- [ ] Scan for vulnerabilities with static analysis tools
- [ ] Test with [Application Verifier](https://docs.microsoft.com/en-us/windows/win32/debugger/application-verifier)
- [ ] Verify no hardcoded credentials or API keys
- [ ] Check for insecure deserialization
- [ ] Validate input sanitization

## Documentation Package

Include these files with your distribution:

### User Documentation
- **README.txt** - Quick start guide
- **USER_MANUAL.pdf** - Comprehensive user manual
- **FAQ.txt** - Frequently asked questions
- **TROUBLESHOOTING.txt** - Common issues and solutions

### Developer Documentation
- **API_DOCUMENTATION.md** - For plugin developers
- **CONTRIBUTING.md** - Contribution guidelines
- **CHANGELOG.md** - Version history

### Legal Documents
- **LICENSE.txt** - End user license agreement
- **PRIVACY.txt** - Privacy policy
- **THIRD_PARTY.txt** - Third-party software acknowledgments

## Support and Maintenance

### Support Channels

1. **Email Support**
   - support@yourdomain.com
   - Response time: 24-48 hours

2. **Issue Tracking**
   - GitHub Issues
   - Jira (for enterprise)

3. **Documentation**
   - Online wiki
   - Video tutorials
   - FAQ section

### Maintenance Schedule

- **Weekly**: Monitor issue tracker
- **Monthly**: Review and merge pull requests
- **Quarterly**: Security audit and updates
- **Annually**: Major version planning

### Bug Fix Process

1. User reports bug
2. Team triages issue
3. Developer creates fix
4. QA testing
5. Code review
6. Merge to main branch
7. Create patch release
8. Notify users

## Post-Deployment

### Monitoring

Track the following metrics:

- **Download Count**
- **Installation Success Rate**
- **Crash Reports** (implement error reporting)
- **Feature Usage Analytics** (with user consent)
- **Support Ticket Volume**

### User Feedback Collection

- **In-App Feedback Form**
- **Post-Installation Survey**
- **Net Promoter Score (NPS)**
- **User Interviews**

### Continuous Improvement

1. Analyze usage data
2. Identify pain points
3. Prioritize features
4. Implement improvements
5. Test thoroughly
6. Release updates

## Troubleshooting Common Issues

### Issue: Windows Defender Blocks Download

**Solution:** Add to allowed apps or submit to SmartScreen

### Issue: WebView2 Not Available

**Solution:** Include WebView2 installer or direct users to download

### Issue: High CPU Usage During Scanning

**Solution:** Adjust default concurrency settings

### Issue: Database Corruption

**Solution:** Implement database backup and recovery

## Security Considerations

### Distribution Security

- **Sign all binaries** with code signing certificate
- **Verify checksums** before distribution
- **Use HTTPS** for all downloads
- **Implement authenticity checks** in application

### Application Security

- **Secure API keys** - Never hardcode in source
- **Encrypt sensitive data** in database
- **Validate all user inputs**
- **Implement rate limiting** for API calls
- **Regular security audits**

## Backup and Recovery

### Data Backup

Automatically backup:
- Application settings
- User preferences
- Custom configurations

### Recovery Plan

1. **Database Recovery**
   - Implement auto-backup
   - Provide restore function
   - Document recovery procedure

2. **Settings Recovery**
   - Export settings to file
   - Import from backup
   - Reset to defaults option

## Compliance

### Iranian Regulations

Ensure compliance with:
- **Computer Crimes Law**
- **Data Protection Laws**
- **Cybersecurity Regulations**

### International Compliance

If distributing internationally:
- **GDPR** (EU)
- **CCPA** (California)
- **PDPA** (Singapore)

## Cost Estimation

### Development Costs

- Development: $X,XXX
- Testing: $XXX
- Documentation: $XXX
- Total: $X,XXX

### Distribution Costs

- Code Signing Certificate: $XXX/year
- Hosting: $XXX/year
- CDN (if needed): $XXX/month
- Total: $XXX/year

### Support Costs

- Support staff: $X,XXX/month
- Tools: $XXX/month
- Total: $X,XXX/month

## Conclusion

This deployment guide covers the complete process from building to distributing the Iranian Miner Detector application. Choose the distribution method that best fits your needs and target audience.

For questions or assistance, contact the development team.

---

**Document Version:** 1.0.0
**Last Updated:** 2024
**Maintained by:** Iranian Network Security
