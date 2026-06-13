; FiveTogether Installer Script for Inno Setup
; Downloads: https://jrsoftware.org/isdl.php
;
; This script creates a single setup.exe that:
; 1. Installs the FiveTogether app
; 2. Checks for ViGEmBus driver and installs if missing
; 3. Checks for HidHide driver and installs if missing
; 4. Creates a desktop shortcut
;
; HOW TO BUILD:
; 1. Build the app: dotnet publish -c Release -r win-x64 --self-contained
; 2. Download ViGEmBusSetup_x64.msi from: https://github.com/nefarius/ViGEmBus/releases
; 3. Download HidHide_1.x.x_x64.exe from: https://github.com/nefarius/HidHide/releases
; 4. Place both in the installer/drivers/ folder
; 5. Open this .iss file in Inno Setup Compiler and click Build

[Setup]
AppName=FiveTogether
AppVersion=1.0.0
AppPublisher=FiveTogether
AppPublisherURL=https://github.com/fivetogether
DefaultDirName={autopf}\FiveTogether
DefaultGroupName=FiveTogether
OutputDir=..\output
OutputBaseFilename=FiveTogether_Setup
Compression=lzma2
SolidCompression=yes
PrivilegesRequired=admin
SetupIconFile=assets\icon.ico
UninstallDisplayIcon={app}\FiveTogether.exe
WizardStyle=modern
DisableProgramGroupPage=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: checkedonce

[Files]
; Main application files (from dotnet publish output)
Source: "..\src\FiveTogether\bin\Release\net8.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

; Bundled driver installers
Source: "drivers\ViGEmBusSetup_x64.msi"; DestDir: "{tmp}"; Flags: deleteafterinstall
Source: "drivers\HidHide_1.4.2_x64.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall

[Icons]
Name: "{group}\FiveTogether"; Filename: "{app}\FiveTogether.exe"
Name: "{autodesktop}\FiveTogether"; Filename: "{app}\FiveTogether.exe"; Tasks: desktopicon

[Run]
; Install ViGEmBus driver (silently, only if not already installed)
Filename: "msiexec.exe"; Parameters: "/i ""{tmp}\ViGEmBusSetup_x64.msi"" /quiet /norestart"; \
  StatusMsg: "Installing ViGEmBus driver..."; \
  Flags: runhidden waituntilterminated; \
  Check: NeedsViGEmBus

; Install HidHide driver (silently, only if not already installed)  
Filename: "{tmp}\HidHide_1.4.2_x64.exe"; Parameters: "/silent /norestart"; \
  StatusMsg: "Installing HidHide driver..."; \
  Flags: runhidden waituntilterminated; \
  Check: NeedsHidHide

; Launch the app after installation (optional)
Filename: "{app}\FiveTogether.exe"; Description: "Launch FiveTogether"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; Note: We do NOT uninstall the drivers — they might be used by other apps (DS4Windows, etc.)

[Code]
// Check if ViGEmBus is installed by looking for its service
function NeedsViGEmBus: Boolean;
var
  ResultCode: Integer;
begin
  // Check if ViGEmBus service exists
  Result := not RegKeyExists(HKEY_LOCAL_MACHINE, 'SYSTEM\CurrentControlSet\Services\ViGEmBus');
end;

// Check if HidHide is installed by looking for its service
function NeedsHidHide: Boolean;
var
  ResultCode: Integer;
begin
  // Check if HidHide service exists
  Result := not RegKeyExists(HKEY_LOCAL_MACHINE, 'SYSTEM\CurrentControlSet\Services\HidHide');
end;
