; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define MyAppName "SanteDB Web Portal Host"
#define MyAppPublisher "SanteDB Community"
#define MyAppURL "http://santesuite.org"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{89E5D303-441E-4742-970A-C2C240C89AFF}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
;AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={pf64}\SanteSuite\SanteDB\WWW
DisableProgramGroupPage=no
LicenseFile=.\License.rtf
OutputDir=.\dist
OutputBaseFilename=santedb-www-{#MyAppVersion}
Compression=bzip
SolidCompression=yes
DefaultGroupName={#MyAppName}
WizardStyle=modern
;SignedUninstaller=yes
;SignTool=default
SignedUninstaller=yes
SignTool=default /a /n $qFyfe Software$q /d $q{#MyAppName}$q $f

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: ".\bin\Release\*.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: ".\bin\Release\Applets\*.pak"; DestDir: "{app}\Applets"; Flags: ignoreversion
Source: ".\bin\Release\santedb-www.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: ".\bin\Release\santedb-www.exe.config"; DestDir: "{app}"; Flags: ignoreversion
Source: ".\installsupp\vcredist_x86.exe"; DestDir: "{tmp}"; Flags: dontcopy;
Source: ".\installsupp\netfx.exe"; DestDir: "{tmp}"; Flags: dontcopy;

[Icons]
Filename: "http://127.0.0.1:9200"; Name: "{group}\SanteDB\SanteDB Web Portal"; IconFilename: "{app}\santedb-www.exe"
Filename: "{app}\santedb-www.exe"; Parameters: "--restart"; Name: "{group}\SanteDB\Restart SanteDB Portal";  IconFilename: "{app}\santedb-www.exe"
Filename: "http://127.0.0.1:9200"; Name: "{commondesktop}\SanteDB Web Portal"; IconFilename: "{app}\santedb-www.exe"

[Run]
Filename: "{app}\santedb-www.exe";StatusMsg: "Installing Services..."; Parameters: "--install"; Description: "Installing Service"; Flags: runhidden
Filename: "net.exe";StatusMsg: "Starting Services..."; Parameters: "start sdb-www-default"; Description: "Start Gateway Service"; Flags: runhidden
Filename: "http://127.0.0.1:9200"; Description: "Configure the SanteDB Web Portal"; Flags: postinstall shellexec

[UninstallRun]
Filename: "net.exe";StatusMsg: "Stopping Services..."; Parameters: "stop sdb-www-default"; Flags: runhidden
Filename: "{app}\santedb-www.exe";StatusMsg: "Removing Configuration Data...";  Parameters: "--reset"; Flags: runhidden
Filename: "{app}\santedb-www.exe";StatusMsg: "Removing Services..."; Parameters: "--uninstall"; Flags: runhidden

[Code]
function PrepareToInstall(var needsRestart:Boolean): String;
var
  hWnd: Integer;
  ResultCode : integer;
  uninstallString : string;
begin
    EnableFsRedirection(true);
    WizardForm.PreparingLabel.Visible := True;
    WizardForm.PreparingLabel.Caption := 'Installing Visual C++ Redistributable';
    ExtractTemporaryFile('vcredist_x86.exe');
    Exec(ExpandConstant('{tmp}\vcredist_x86.exe'), '/install /passive /norestart', '', SW_SHOW, ewWaitUntilTerminated, ResultCode);
    WizardForm.PreparingLabel.Caption := 'Installing Microsoft .NET Framework 4.8';
     ExtractTemporaryFile('netfx.exe');
    Exec(ExpandConstant('{tmp}\netfx.exe'), '/q', '', SW_SHOW, ewWaitUntilTerminated, ResultCode);
end;
