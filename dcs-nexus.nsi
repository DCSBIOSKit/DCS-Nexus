; Define the name of your installer
Outfile "DCS-NexusInstaller.exe"

; Define the installation directory
InstallDir "$PROGRAMFILES\DCS-Nexus"

; Request application privileges for Windows Vista and newer
RequestExecutionLevel admin

; Default section start
Section "DCS-Nexus Installation" SecInstall

    ; Set the output path to the installation directory
    SetOutPath $INSTDIR

    ; Copy all files from the specified directory
    File /r ".\bin\Release\net7.0-windows\win-x64\publish\*.*"

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