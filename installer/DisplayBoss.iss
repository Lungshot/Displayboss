[Setup]
AppId={{B8E3F2A1-5C4D-4E6F-9A7B-1D2E3F4A5B6C}
AppName=DisplayBoss
AppVersion=1.0.0
AppPublisher=Lungshot
AppPublisherURL=https://github.com/Lungshot/Displayboss
DefaultDirName={autopf}\DisplayBoss
DefaultGroupName=DisplayBoss
UninstallDisplayIcon={app}\DisplayBoss.exe
OutputDir=..\publish
OutputBaseFilename=DisplayBoss-1.0.0-Setup
SetupIconFile=..\Dlogo.ico
Compression=lzma2
SolidCompression=yes
PrivilegesRequired=lowest
ArchitecturesInstallIn64BitMode=x64compatible
WizardStyle=modern
DisableProgramGroupPage=yes
CloseApplications=force
; Uncomment and set path when you have a code signing certificate:
; SignTool=signtool sign /f "$path_to_cert.pfx" /p $password /tr http://timestamp.digicert.com /td sha256 /fd sha256 $f

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "..\publish\final\DisplayBoss.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\publish\final\DisplayBoss.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\publish\final\DisplayBoss.deps.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\publish\final\DisplayBoss.runtimeconfig.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\publish\final\DisplayBoss.Core.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\publish\final\displayboss-cli.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\publish\final\displayboss-cli.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\publish\final\displayboss-cli.deps.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\publish\final\displayboss-cli.runtimeconfig.json"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\DisplayBoss"; Filename: "{app}\DisplayBoss.exe"; IconFilename: "{app}\DisplayBoss.exe"
Name: "{autodesktop}\DisplayBoss"; Filename: "{app}\DisplayBoss.exe"; Tasks: desktopicon
Name: "{userstartup}\DisplayBoss"; Filename: "{app}\DisplayBoss.exe"; Tasks: startupicon

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional shortcuts:"
Name: "startupicon"; Description: "Start DisplayBoss with &Windows"; GroupDescription: "Startup:"; Flags: checkedonce
Name: "addtopath"; Description: "Add CLI tool to system &PATH"; GroupDescription: "CLI:"

[Run]
Filename: "{tmp}\windowsdesktop-runtime-8.0-win-x64.exe"; Parameters: "/install /quiet /norestart"; StatusMsg: "Installing .NET 8.0 Desktop Runtime..."; Flags: waituntilterminated; Check: not IsDotNet8DesktopInstalled
Filename: "{app}\DisplayBoss.exe"; Description: "Launch DisplayBoss"; Flags: nowait postinstall skipifsilent

[Registry]
Root: HKCU; Subkey: "Environment"; ValueType: string; ValueName: "Path"; ValueData: "{olddata};{app}"; Tasks: addtopath; Check: NeedsAddPath(ExpandConstant('{app}'))

[Code]
var
  DownloadPage: TDownloadWizardPage;
  DotNetNeeded: Boolean;

function IsDotNet8DesktopInstalled: Boolean;
var
  ResultCode: Integer;
begin
  // Check via dotnet command for any 8.0.x WindowsDesktop runtime
  if Exec(ExpandConstant('{cmd}'), '/c dotnet --list-runtimes 2>nul | findstr /C:"Microsoft.WindowsDesktop.App 8.0" >nul', '',
    SW_HIDE, ewWaitUntilTerminated, ResultCode) then
    Result := (ResultCode = 0)
  else
    Result := False;
end;

function OnDownloadProgress(const Url, FileName: String; const Progress, ProgressMax: Int64): Boolean;
begin
  if Progress = ProgressMax then
    Log(Format('Downloaded %s (%d bytes)', [FileName, Progress]));
  Result := True;
end;

procedure InitializeWizard;
begin
  DotNetNeeded := not IsDotNet8DesktopInstalled;

  DownloadPage := CreateDownloadPage(
    SetupMessage(msgWizardPreparing),
    SetupMessage(msgPreparingDesc),
    @OnDownloadProgress);
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;

  if (CurPageID = wpReady) and DotNetNeeded then
  begin
    DownloadPage.Clear;
    // aka.ms redirect always points to latest 8.0.x runtime
    DownloadPage.Add(
      'https://aka.ms/dotnet/8.0/windowsdesktop-runtime-win-x64.exe',
      'windowsdesktop-runtime-8.0-win-x64.exe', '');
    DownloadPage.Show;
    try
      try
        DownloadPage.Download;
      except
        if DownloadPage.AbortedByUser then
          Log('Download aborted by user.')
        else
          SuppressibleMsgBox(AddPeriod(GetExceptionMessage),
            mbCriticalError, MB_OK, IDOK);
        Result := False;
      end;
    finally
      DownloadPage.Hide;
    end;
  end;
end;

function NeedsAddPath(Param: string): boolean;
var
  OrigPath: string;
begin
  if not RegQueryStringValue(HKEY_CURRENT_USER, 'Environment', 'Path', OrigPath) then
  begin
    Result := True;
    exit;
  end;
  Result := Pos(';' + Param + ';', ';' + OrigPath + ';') = 0;
end;

[UninstallDelete]
Type: files; Name: "{app}\*"
Type: dirifempty; Name: "{app}"
