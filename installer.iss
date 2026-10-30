#define MyAppName "BeamMP-GUI"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Alen Trošt"
#define MyAppURL "https://github.com/alentrost/BeamMP-GUI"
#define MyAppExeName "BeamMP-GUI.exe"

[Setup]
AppId={{D37F8A2A-72C5-4A91-8D6F-9C1D735B1342}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={code:GetDefaultServerDir}
DisableProgramGroupPage=yes
OutputDir=.
OutputBaseFilename=BeamMP-GUI-Installer
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
UninstallDisplayIcon={app}\{#MyAppExeName}
SetupIconFile=AppSource\icon.ico

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"
Name: "startmenuicon"; Description: "Create a Start Menu shortcut"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: startmenuicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
function GetDefaultServerDir(Param: String): String;
var
  DocsPath: String;
  OneDriveDocs: String;
begin
  DocsPath := ExpandConstant('{userdocs}\BeamMP Server');
  OneDriveDocs := ExpandConstant('{userprofile}\OneDrive\Documents\BeamMP Server');

  if FileExists(OneDriveDocs + '\BeamMP-Server.exe') then
    Result := OneDriveDocs
  else if FileExists(DocsPath + '\BeamMP-Server.exe') then
    Result := DocsPath
  else if DirExists(OneDriveDocs) then
    Result := OneDriveDocs
  else if DirExists(DocsPath) then
    Result := DocsPath
  else
    Result := DocsPath;
end;
