#define MyAppName "BeamMP-GUI"
#define MyAppVersion "2.0.0"
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
AppendDefaultDirName=no
DirExistsWarning=no
DisableProgramGroupPage=yes
OutputDir=.
OutputBaseFilename=BeamMP-GUI-Installer
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
UninstallDisplayIcon={app}\icon.ico
SetupIconFile=AppSource\icon.ico

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"
Name: "startmenuicon"; Description: "Create a Start Menu shortcut"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "AppSource\icon.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\icon.ico"; Tasks: desktopicon
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\icon.ico"; Tasks: startmenuicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
function GetDefaultServerDir(Param: String): String;
var
  DocsPath: String;
  UserProf: String;
  OneDriveDocs: String;
begin
  DocsPath := ExpandConstant('{userdocs}\BeamMP Server');
  UserProf := GetEnv('USERPROFILE');
  if UserProf <> '' then
    OneDriveDocs := UserProf + '\OneDrive\Documents\BeamMP Server'
  else
    OneDriveDocs := '';

  if (OneDriveDocs <> '') and FileExists(OneDriveDocs + '\BeamMP-Server.exe') then
    Result := OneDriveDocs
  else if FileExists(DocsPath + '\BeamMP-Server.exe') then
    Result := DocsPath
  else if (OneDriveDocs <> '') and DirExists(OneDriveDocs) then
    Result := OneDriveDocs
  else if DirExists(DocsPath) then
    Result := DocsPath
  else
    Result := DocsPath;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
var
  SelectedDir: String;
  ServerExe: String;
begin
  Result := True;
  if CurPageID = wpSelectDir then
  begin
    SelectedDir := WizardDirValue;
    ServerExe := AddBackslash(SelectedDir) + 'BeamMP-Server.exe';
    if not FileExists(ServerExe) then
    begin
      MsgBox('BeamMP-Server.exe was not found in the selected folder:' + #13#10#13#10 +
             SelectedDir + #13#10#13#10 +
             'Please select the folder where BeamMP-Server.exe is located.',
             mbCriticalError, MB_OK);
      Result := False;
    end;
  end;
end;
