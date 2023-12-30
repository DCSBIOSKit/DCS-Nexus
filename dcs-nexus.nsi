!include "LogicLib.nsh"

; Define the name of your installer
Outfile "DCS-NexusInstaller.exe"

; Define the installation directory
InstallDir "$PROGRAMFILES\DCS-Nexus"

; Request application privileges for Windows Vista and newer
RequestExecutionLevel admin

; Default section start
Section "DCS-Nexus Installation" SecInstall
    ;Call CheckDotNet

    ; Set the output path to the installation directory
    SetOutPath $INSTDIR

    ; Copy all files from the specified directory
    File /r ".\bin\Release\net8.0-windows\win-x64\publish\*.*"

    ; Create a shortcut in the Start menu
    CreateDirectory "$SMPROGRAMS\DCS-Nexus"
    CreateShortCut "$SMPROGRAMS\DCS-Nexus\DCS-Nexus.lnk" "$INSTDIR\DCS-Nexus.exe"

    ; Create uninstaller
    WriteUninstaller "$INSTDIR\Uninstall.exe"

SectionEnd

; Uninstaller section
Section "Uninstall"

    ; Remove the Start menu shortcut
    Delete "$SMPROGRAMS\DCS-Nexus\DCS-Nexus.lnk"
    RMDir "$SMPROGRAMS\DCS-Nexus"

    ; Remove installed files
    Delete "$INSTDIR\*.*"

    ; Remove the installation directory
    RMDir "$INSTDIR"

    ; Remove the uninstaller itself
    Delete "$INSTDIR\Uninstall.exe"

SectionEnd

!define DOTNET_REG_KEY "SOFTWARE\WOW6432Node\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App"
!define DOTNET_REG_VALUE "8.0.0"

Function CheckDotNet
    ClearErrors
    ReadRegDWORD $0 HKLM "${DOTNET_REG_KEY}" "${DOTNET_REG_VALUE}"
    IfErrors notFound
    IntOp $0 $0 & 0x0000FFFF ; Extract the lower 16 bits if the version is encoded
    StrCmp $0 1 versionFound notFound

    versionFound:
    ;MessageBox MB_ICONINFORMATION ".NET Version is installed."
    Goto done

    notFound:
    MessageBox MB_ICONINFORMATION ".NET Version is not installed."
    Call DownloadAndInstallDotNet

    done:
FunctionEnd

!define DOTNET_INSTALLER_URL "https://download.visualstudio.microsoft.com/download/pr/b280d97f-25a9-4ab7-8a12-8291aa3af117/a37ed0e68f51fcd973e9f6cb4f40b1a7/windowsdesktop-runtime-8.0.0-win-x64.exe"
;!define DOTNET_INSTALLER_URL "https://download.visualstudio.microsoft.com/download/pr/89d3660b-d344-47c5-a1cd-d8343a3f3779/9f55af82923dab7e3dce912f5c5b9d60/aspnetcore-runtime-8.0.0-win-x64.exe"
!define DOTNET_INSTALLER_PATH "$TEMP\dotnet_installer.exe"

Function DownloadAndInstallDotNet
    ; Download .NET 8.0 installer
    nsisdl::download /TIMEOUT=30000 "${DOTNET_INSTALLER_URL}" "${DOTNET_INSTALLER_PATH}"
    Pop $0 ; Get the return value
    StrCmp $0 "success" +3
        MessageBox MB_ICONSTOP "Download failed: $0"
        Abort

    ; Execute the .NET 8.0 installer silently
    ExecWait '"${DOTNET_INSTALLER_PATH}" /install /quiet /norestart' $0
    StrCmp $0 0 +2
        MessageBox MB_ICONSTOP "Installation failed."

    ; Clean up the installer
    Delete "${DOTNET_INSTALLER_PATH}"
FunctionEnd
