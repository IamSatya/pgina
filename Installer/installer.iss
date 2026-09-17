#define MyAppName "pGina"
#define MyAppVersion "3.2.5.0"
#define MyAppPublisher "pGina Team"
#define MyAppURL "https://github.com/IamSatya/pgina"
#define MyAppExeName "pGina.Configuration.exe"

[Setup]
AppID={{3D8D0F0D-7DBF-400C-9C44-00BD21986138}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} v{#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=true
LicenseFile=..\LICENSE
OutputDir=Output
OutputBaseFilename=pGinaSetup-{#MyAppVersion}
SetupIconFile=..\pGina\src\Configuration\Resources\pginaicon_redcircle.ico
Compression=lzma2/Max
SolidCompression=true
AppCopyright=pGina Team
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesInstallIn64BitMode=x64
MinVersion=6.1
WizardStyle=modern

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Core application files
Source: "..\pGina\src\bin\pGina.Configuration.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\pGina\src\bin\pGina.Service.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\pGina\src\bin\pGina.InstallUtil.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\pGina\src\bin\pGina.CredentialProviderRegistration.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\pGina\src\bin\*.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\pGina\src\bin\*.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\pGina\src\bin\log4net.xml"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist

; Native Credential Provider DLL (x64 and Win32)
Source: "..\pGina\src\bin\x64\pGinaCredentialProvider.dll"; DestDir: "{app}\x64"; Flags: ignoreversion
Source: "..\pGina\src\bin\Win32\pGinaCredentialProvider.dll"; DestDir: "{app}\Win32"; Flags: ignoreversion
; Place active bitness in root of {app} for direct discovery
Source: "..\pGina\src\bin\x64\pGinaCredentialProvider.dll"; DestDir: "{app}"; Check: Is64BitInstallMode; Flags: ignoreversion
Source: "..\pGina\src\bin\Win32\pGinaCredentialProvider.dll"; DestDir: "{app}"; Check: not Is64BitInstallMode; Flags: ignoreversion

; Core Plugins
Source: "..\Plugins\Core\bin\*.dll"; DestDir: "{app}\Plugins\Core"; Flags: ignoreversion recursesubdirs createallsubdirs

; Contrib Plugins
Source: "..\Plugins\Contrib\bin\*.dll"; DestDir: "{app}\Plugins\Contrib"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName} Configuration"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{commondesktop}\{#MyAppName} Configuration"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
; Run InstallUtil post-install to register service, set permissions, and register Credential Provider
Filename: "{app}\pGina.InstallUtil.exe"; Parameters: "post-install"; StatusMsg: "Installing service, registering Credential Provider, and configuring permissions..."; WorkingDir: "{app}"; Flags: runhidden
; Optionally launch Configuration tool after installation
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent runascurrentuser

[UninstallRun]
; Run InstallUtil post-uninstall to unregister Credential Provider and stop/remove service
Filename: "{app}\pGina.InstallUtil.exe"; Parameters: "post-uninstall"; StatusMsg: "Unregistering Credential Provider and removing service..."; WorkingDir: "{app}"; Flags: runhidden

[Code]
procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
begin
  if CurStep = ssInstall then
  begin
    // Attempt to stop existing pGina service before file copying
    Exec('net.exe', 'stop pGina', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  end;
end;
