; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!
#define public Dependency_NoExampleSetup
#include "CodeDependencies.iss"

#define MyAppName "MSU Scripter"
#define MyAppPublisher "MattEqualsCoder"
#define MyAppURL "https://github.com/MattEqualsCoder"
#define MyAppExeName "MSUScripter.exe"
#define MyAppVersion GetStringFileInfo("..\MSUScripter\bin\Release\net9.0\win-x64\publish\" + MyAppExeName, "ProductVersion")

[Setup]
; NOTE: The value of AppId uniquely identifies this application. Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{F2AE703A-0B27-4B64-AF3D-D5D36571FA93}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\MSUScripter
DisableProgramGroupPage=yes
; Remove the following line to run in administrative install mode (install for all users.)
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
OutputBaseFilename=MSUScripterSetupWin_{#MyAppVersion}
Compression=lzma
SolidCompression=yes
WizardStyle=modern


[Code]
function InitializeSetup: Boolean;
begin
  Dependency_AddDotNet90Desktop;
  Dependency_AddDotNet90Asp;
  Result := True;
end;

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "netcorecheck.exe"; Flags: dontcopy noencryption
Source: "netcorecheck_x64.exe"; Flags: dontcopy noencryption
Source: "..\MSUScripter\bin\Release\net9.0\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Check: Is64BitInstallMode;
Source: "..\MSUScripter\bin\Release\net9.0\win-x86\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Check: "not Is64BitInstallMode";
; Source: "..\ConfigRepo\resources\snes\*"; Excludes: "*.txt,*.bps"; DestDir: "{localappdata}\MSUScripter\Configs\snes"; Flags: ignoreversion recursesubdirs createallsubdirs
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

