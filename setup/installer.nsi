; NSIS script para el instalador de Control de mareas
; Basado en el sistema de publicación de .NET 10

!define APP_NAME "ControlDeMareas"
!define APP_DISPLAY "Control de mareas"
!define APP_PUBLISHER "INIDEP - Observadores a Bordo"
!define APP_VERSION "1.0.0"
!define INSTALLER_OUT "ControlDeMareas_Setup.exe"
!define INSTALL_DIR "$PROGRAMFILES64\Control de mareas"
!define APP_ICON "..\OBSArrastre2026.App\assets\icons\app_icon.ico"
!define PUBLISH_DIR "publish"

SetCompressor lzma
SetCompressorDictSize 32
Name "${APP_DISPLAY}"
OutFile "${INSTALLER_OUT}"
InstallDir "${INSTALL_DIR}"
Icon "${APP_ICON}"
UninstallIcon "${APP_ICON}"
RequestExecutionLevel admin

; --- Interfaz de Usuario ---
!include "MUI2.nsh"

!define MUI_ABORTWARNING
!define MUI_ICON "${APP_ICON}"
!define MUI_UNICON "${APP_ICON}"

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_LANGUAGE "Spanish"

; --- Proceso de Instalación ---
Section "Instalar ${APP_DISPLAY}"
  SetOutPath "$INSTDIR"
  
  ; Copiar todos los archivos de la carpeta de publicación
  File /r "${PUBLISH_DIR}\*"

  ; Registro del desinstalador y panel de control
  WriteUninstaller "$INSTDIR\Uninstall.exe"
  
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME}" "DisplayName" "${APP_DISPLAY}"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME}" "DisplayVersion" "${APP_VERSION}"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME}" "Publisher" "${APP_PUBLISHER}"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME}" "DisplayIcon" "$INSTDIR\${APP_NAME}.exe"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME}" "InstallLocation" "$INSTDIR"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME}" "UninstallString" '"$INSTDIR\Uninstall.exe"'
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME}" "NoModify" 1
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME}" "NoRepair" 1

  ; Accesos directos
  SetOutPath "$INSTDIR"
  CreateDirectory "$SMPROGRAMS\${APP_DISPLAY}"
  CreateShortCut "$SMPROGRAMS\${APP_DISPLAY}\${APP_DISPLAY}.lnk" "$INSTDIR\${APP_NAME}.exe" "" "$INSTDIR\${APP_NAME}.exe"
  CreateShortCut "$SMPROGRAMS\${APP_DISPLAY}\Desinstalar.lnk" "$INSTDIR\Uninstall.exe" "" "$INSTDIR\Uninstall.exe"
  CreateShortCut "$DESKTOP\${APP_DISPLAY}.lnk" "$INSTDIR\${APP_NAME}.exe" "" "$INSTDIR\${APP_NAME}.exe"
SectionEnd

; --- Proceso de Desinstalación ---
Section "Uninstall"
  ; Eliminar archivos
  Delete "$INSTDIR\Uninstall.exe"
  RMDir /r "$INSTDIR"

  ; Eliminar accesos directos
  Delete "$SMPROGRAMS\${APP_DISPLAY}\${APP_DISPLAY}.lnk"
  Delete "$SMPROGRAMS\${APP_DISPLAY}\Desinstalar.lnk"
  Delete "$DESKTOP\${APP_DISPLAY}.lnk"
  RMDir "$SMPROGRAMS\${APP_DISPLAY}"

  ; Eliminar claves del registro
  DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME}"
SectionEnd
