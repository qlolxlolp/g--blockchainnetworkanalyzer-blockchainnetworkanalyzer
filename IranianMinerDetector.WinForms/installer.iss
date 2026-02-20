; Iranian Miner Detector - Inno Setup Installer Script
; Run this with Inno Setup Compiler (ISCC.exe) to create an installer

[Setup]
; Basic application information
AppName=Iranian Miner Detector
AppVersion=1.0.0
AppPublisher=Iranian Network Security
AppPublisherURL=https://github.com/iranian-network-security
AppSupportURL=https://github.com/iranian-network-security/issues
AppUpdatesURL=https://github.com/iranian-network-security/releases
DefaultDirName={pf}\IranianMinerDetector
DefaultGroupName=Iranian Miner Detector
AllowNoIcons=yes
OutputDir=installer_output
OutputBaseFilename=IranianMinerDetector-Setup
Compression=lzma2/max
SolidCompression=yes
; Require Windows 10 or later
MinVersion=10.0
; Admin privileges not required
PrivilegesRequired=lowest
; Show license agreement
LicenseFile=LICENSE.txt
; Display additional icons
UninstallDisplayIcon={app}\IranianMinerDetector.WinForms.exe

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "persian"; MessagesFile: "compiler:Languages\Persian.isl"

[Tasks]
; Create desktop shortcut
Name: "desktopicon"; Description: "Create a &desktop icon"; GroupDescription: "Additional icons:"
; Create quick launch shortcut
Name: "quicklaunchicon"; Description: "Create a &quick launch icon"; GroupDescription: "Additional icons:"

[Files]
; Main executable (assuming it's in publish directory)
Source: "publish\IranianMinerDetector.WinForms.exe"; DestDir: "{app}"; Flags: ignoreversion
; Include documentation if available
Source: "README.md"; DestDir: "{app}"; Flags: ignoreversion; DestName: "README.txt"
Source: "LICENSE.txt"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
; Program menu icons
Name: "{group}\Iranian Miner Detector"; Filename: "{app}\IranianMinerDetector.WinForms.exe"
Name: "{group}\Uninstall Iranian Miner Detector"; Filename: "{uninstallexe}"
; Desktop shortcut
Name: "{commondesktop}\Iranian Miner Detector"; Filename: "{app}\IranianMinerDetector.WinForms.exe"; Tasks: desktopicon
; Quick launch shortcut
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\Iranian Miner Detector"; Filename: "{app}\IranianMinerDetector.WinForms.exe"; Tasks: quicklaunchicon

[Run]
; Run the application after installation
Filename: "{app}\IranianMinerDetector.WinForms.exe"; Description: "Launch Iranian Miner Detector"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Remove application data directory if user chooses
Type: filesandordirs; Name: "{localappdata}\IranianMinerDetector"

; Custom pages for better user experience
[Types]
Name: "full"; Description: "Full installation"
Name: "compact"; Description: "Compact installation"
Name: "custom"; Description: "Custom installation"; Flags: iscustom

[Components]
Name: "main"; Description: "Main Application"; Types: full compact custom; Flags: fixed
Name: "docs"; Description: "Documentation"; Types: full

[Dirs]
; Create application data directory
Name: "{localappdata}\IranianMinerDetector"

; Code section for custom actions
[Code]
// Function to check if WebView2 is installed
function IsWebView2Installed: Boolean;
var
  regVersion: Cardinal;
begin
  Result := RegQueryDWordValue(HKLM, 'SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}', 'pv', regVersion);
  if not Result then
    Result := RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}', 'pv', regVersion);
end;

// Show warning if WebView2 is not installed
function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if CurPageID = wpReady then
  begin
    if not IsWebView2Installed then
    begin
      if MsgBox('WebView2 Runtime is required for the map feature to work properly.' + #13#10 +
                'Do you want to download it now?' + #13#10 +
                'The application will still work without it, but maps will not be available.',
                mbConfirmation, MB_YESNO) = IDYES then
      begin
        ShellExec('open', 'https://go.microsoft.com/fwlink/p/?LinkId=2124703', '', '', SW_SHOW, ewNoWait, 0);
      end;
    end;
  end;
end;

// Initialize setup
function InitializeSetup: Boolean;
var
  ResultCode: Integer;
begin
  Result := True;
  // Check if .NET 8 is installed (not needed for self-contained, but good practice)
  // For self-contained builds, we don't need to check for .NET runtime
end;

// After installation
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    // Create any necessary directories
    if not DirExists(ExpandConstant('{localappdata}\IranianMinerDetector')) then
      CreateDir(ExpandConstant('{localappdata}\IranianMinerDetector'));
  end;
end;
