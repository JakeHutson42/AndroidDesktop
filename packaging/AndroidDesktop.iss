#ifndef PublishDir
  #error PublishDir must point to a verified candidate publish directory
#endif
#ifndef OutputDir
  #define OutputDir "."
#endif
#define AppVersion "0.3.0"

[Setup]
AppId={{B6CDA357-735F-4AA8-A37B-4F19A81E9C54}
AppName=Android Desktop
AppVersion={#AppVersion}
AppPublisher=Android Desktop
DefaultDirName={localappdata}\Programs\AndroidDesktop
DefaultGroupName=Android Desktop
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.22000
WizardStyle=modern
Compression=lzma2
SolidCompression=yes
OutputDir={#OutputDir}
OutputBaseFilename=AndroidDesktop-{#AppVersion}-win-x64-candidate
UninstallDisplayIcon={app}\AndroidDesktop.exe
CloseApplications=yes
RestartApplications=no
InfoBeforeFile={#PublishDir}\HARDWARE_AND_SETUP.md

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Shortcuts:"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Android Desktop"; Filename: "{app}\AndroidDesktop.exe"
Name: "{group}\Hardware and Setup guide"; Filename: "{app}\HARDWARE_AND_SETUP.md"
Name: "{autodesktop}\Android Desktop"; Filename: "{app}\AndroidDesktop.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\AndroidDesktop.exe"; Description: "Open Android Desktop and check official prerequisites"; Flags: nowait postinstall skipifsilent

; No LocalAppData device/settings deletion. No SDK/Java redistribution or automatic license acceptance.
[Code]
procedure InitializeWizard();
begin
  WizardForm.WelcomeLabel2.Caption := 'Evaluation candidate: Android media/input/performance and clean-PC acceptance remain unverified.' + #13#10#13#10 +
    'The .NET desktop runtime and private gateway are included. Setup in the application checks official Android SDK/image, compatible Java, WebView2, acceleration and free storage. Review official download sizes and licenses before installation of those dependencies. Reopen Setup after cancellation or a virtualization reboot.' + #13#10#13#10 +
    'Upgrades and uninstall retain Android device data and desktop settings. Close the running Android session gracefully before upgrading.';
end;
