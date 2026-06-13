[Setup]
AppName=SmartGPA
AppVersion=1.0
DefaultDirName={autopf}\SmartGPA
DefaultGroupName=SmartGPA
OutputBaseFilename=SmartGPASetup
Compression=lzma2
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; IMPORTANT: Run 'dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true' before compiling this script
Source: "..\bin\Release\net8.0-windows\win-x64\publish\GpaCalculatorApp.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\SmartGPA"; Filename: "{app}\GpaCalculatorApp.exe"
Name: "{commondesktop}\SmartGPA"; Filename: "{app}\GpaCalculatorApp.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\GpaCalculatorApp.exe"; Description: "{cm:LaunchProgram,SmartGPA}"; Flags: nowait postinstall skipifsilent
