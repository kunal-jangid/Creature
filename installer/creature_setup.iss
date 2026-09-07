#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif

[Setup]
AppId={{D37E860E-A89E-4B07-B9B2-37146522E1A9}
AppName=Desktop Pets
AppVersion={#AppVersion}
AppPublisher=Desktop Pets
AppPublisherURL=https://github.com/kunal-jangid/Creature
AppSupportURL=https://github.com/kunal-jangid/Creature/issues
AppUpdatesURL=https://github.com/kunal-jangid/Creature/releases
DefaultDirName={autopf}\Desktop Pets
DefaultGroupName=Desktop Pets
AllowNoIcons=yes
OutputDir=..\dist
OutputBaseFilename=DesktopPets-Setup-v{#AppVersion}
SetupIconFile=..\Assets\app.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
UninstallDisplayIcon={app}\DesktopPets.exe
CloseApplications=yes
CloseApplicationsFilter=*.exe

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"
Name: "autostart"; Description: "Start Desktop Pets automatically when Windows starts"; GroupDescription: "Startup Options:"

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\Desktop Pets"; Filename: "{app}\DesktopPets.exe"; WorkingDir: "{app}"; IconFilename: "{app}\Assets\app.ico"
Name: "{autodesktop}\Desktop Pets"; Filename: "{app}\DesktopPets.exe"; WorkingDir: "{app}"; IconFilename: "{app}\Assets\app.ico"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "Desktop Pets"; ValueData: """{app}\DesktopPets.exe"""; Flags: uninsdeletevalue; Tasks: autostart

[Run]
Filename: "{app}\DesktopPets.exe"; Description: "{cm:LaunchProgram,Desktop Pets}"; Flags: nowait postinstall skipifsilent

[Code]
procedure KillProcess(const ProcessName: String);
var
  ErrorCode: Integer;
begin
  ShellExec('open', 'taskkill.exe', '/f /im "' + ProcessName + '"', '', SW_HIDE, ewWaitUntilTerminated, ErrorCode);
end;

function InitializeSetup(): Boolean;
begin
  KillProcess('DesktopPets.exe');
  KillProcess('Desktop Pets.exe');
  KillProcess('Creature.exe');
  Result := True;
end;

function InitializeUninstall(): Boolean;
begin
  KillProcess('DesktopPets.exe');
  KillProcess('Desktop Pets.exe');
  KillProcess('Creature.exe');
  Result := True;
end;