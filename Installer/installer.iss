; Inno Setup Script for Blockchain Network Analyzer
; اسکریپت Inno Setup برای Blockchain Network Analyzer
; Created for portable to installed conversion
; ایجاد شده برای تبدیل از حالت قابل حمل به نصب شده

#define MyAppName "Blockchain Network Analyzer Premium"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Enterprise Solutions"
#define MyAppURL "https://www.enterprise-solutions.com"
#define MyAppDescription "Professional Enterprise Blockchain Network Analysis and IP Management System"
#define MyAppExeName "BlockchainNetworkAnalyzer.exe"
#define MyAppId "{{A1B2C3D4-E5F6-4A5B-8C9D-0E1F2A3B4C5D}}"

[Setup]
; NOTE: The value of AppId uniquely identifies this application. Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={#MyAppId}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
LicenseFile=
InfoBeforeFile=
InfoAfterFile=
OutputDir=..\Installer_Output
OutputBaseFilename=Install_BlockchainNetworkAnalyzer
SetupIconFile=
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 6.1; Check: not IsAdminInstallMode

[Files]
Source: "..\Portable_Release\BlockchainNetworkAnalyzer.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\Portable_Release\appsettings.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\Portable_Release\Config\*"; DestDir: "{app}\Config"; Flags: ignoreversion recursesubdirs createallsubdirs
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: quicklaunchicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Registry]
; Create registry entries for file associations and uninstall info
Root: HKLM; Subkey: "Software\{#MyAppPublisher}\{#MyAppName}"; ValueType: string; ValueName: "InstallPath"; ValueData: "{app}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\Uninstall\{#MyAppId}"; ValueType: string; ValueName: "DisplayName"; ValueData: "{#MyAppName}"; Flags: uninsdeletevalue
Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\Uninstall\{#MyAppId}"; ValueType: string; ValueName: "DisplayVersion"; ValueData: "{#MyAppVersion}"; Flags: uninsdeletevalue
Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\Uninstall\{#MyAppId}"; ValueType: string; ValueName: "Publisher"; ValueData: "{#MyAppPublisher}"; Flags: uninsdeletevalue
Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\Uninstall\{#MyAppId}"; ValueType: string; ValueName: "UninstallString"; ValueData: """{uninstallexe}"""; Flags: uninsdeletevalue

[UninstallDelete]
Type: filesandordirs; Name: "{app}\Data"
Type: filesandordirs; Name: "{app}\Logs"
Type: filesandordirs; Name: "{app}\Reports"
Type: filesandordirs; Name: "{app}\Exports"

[Code]
procedure InitializeWizard;
begin
  WizardForm.LicenseAcceptedRadio.Checked := True;
end;

function InitializeSetup(): Boolean;
begin
  Result := True;
  // Check if .NET 8.0 Runtime is installed (not needed for self-contained, but good to check)
  // Since we're using self-contained, we don't need this check
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    // Create necessary directories
    CreateDir(ExpandConstant('{app}\Data'));
    CreateDir(ExpandConstant('{app}\Data\Backups'));
    CreateDir(ExpandConstant('{app}\Logs'));
    CreateDir(ExpandConstant('{app}\Reports'));
    CreateDir(ExpandConstant('{app}\Exports'));
  end;
end;