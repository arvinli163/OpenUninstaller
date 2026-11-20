#define AppName "OpenUninstaller"
#define AppVersion "0.1.0"
[Setup]
AppId={{2EED5C93-0AC2-4B3B-8F29-0B87E8B15D81}
AppName={#AppName}
AppVersion={#AppVersion}
DefaultDirName={autopf}\OpenUninstaller
DefaultGroupName={#AppName}
OutputDir=..\artifacts\setup
OutputBaseFilename=OpenUninstaller-Setup-{#AppVersion}
Compression=lzma2
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=lowest
WizardStyle=modern
[Files]
Source: "..\artifacts\installer\OpenUninstaller.exe"; DestDir: "{app}"; Flags: ignoreversion
[Icons]
Name: "{group}\OpenUninstaller"; Filename: "{app}\OpenUninstaller.exe"
