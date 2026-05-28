[Setup]
AppName=MimicRPC
AppVersion=0.3
AppPublisher=tadedav
DefaultDirName={autopf}\MimicRPC
DefaultGroupName=MimicRPC
OutputDir=Output
OutputBaseFilename=MimicRPC-Windows-Setup
Compression=lzma2/ultra64
SolidCompression=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Files]
Source: "..\publish\win\MimicRPC.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\MimicRPC"; Filename: "{app}\MimicRPC.exe"
Name: "{commondesktop}\MimicRPC"; Filename: "{app}\MimicRPC.exe"; Tasks: desktopicon

[Tasks]
Name: desktopicon; Description: "Create a desktop shortcut"; Flags: unchecked

[Run]
Filename: "{app}\MimicRPC.exe"; Description: "Launch MimicRPC"; Flags: nowait postinstall skipifsilent
