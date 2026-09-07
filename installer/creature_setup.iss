#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif

[Setup]
AppId={{D37E860E-A89E-4B07-B9B2-37146522E1A9}
AppName=Creature
AppVersion={#AppVersion}
AppPublisher=kunal-jangid
AppPublisherURL=https://github.com/kunal-jangid/Creature
AppSupportURL=https://github.com/kunal-jangid/Creature/issues
AppUpdatesURL=https://github.com/kunal-jangid/Creature/releases
DefaultDirName={autopf}\Creature
DefaultGroupName=Creature
AllowNoIcons=yes
OutputDir=..\dist
OutputBaseFilename=Creature-Setup-v{#AppVersion}
SetupIconFile=..\Assets\app.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
UninstallDisplayIcon={app}\Creature.exe

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "autostart"; Description: "Start Creature automatically when Windows starts"; GroupDescription: "Startup Options:"

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\Creature"; Filename: "{app}\Creature.exe"; IconFilename: "{app}\Assets\app.ico"
Name: "{autodesktop}\Creature"; Filename: "{app}\Creature.exe"; IconFilename: "{app}\Assets\app.ico"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "Creature"; ValueData: """{app}\Creature.exe"""; Flags: uninsdeletevalue; Tasks: autostart

[Run]
Filename: "{app}\Creature.exe"; Description: "{cm:LaunchProgram,Creature}"; Flags: nowait postinstall skipifsilent