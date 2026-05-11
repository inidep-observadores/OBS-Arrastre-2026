@echo off
echo ======================================================
echo Generando Instalador para Control de mareas
echo ======================================================

echo 1. Publicando aplicacion (Auto-contenida)...
dotnet publish OBSArrastre2026.App\OBSArrastre2026.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:PublishReadyToRun=true -o setup\publish

echo 2. Compilando instalador con NSIS...
:: Intentar encontrar makensis en rutas comunes y en la version portable indicada
set MAKENSIS="makensis.exe"
if exist "D:\Portables\PortableApps\NSISPortable\App\NSIS\Bin\makensis.exe" set MAKENSIS="D:\Portables\PortableApps\NSISPortable\App\NSIS\Bin\makensis.exe"
if exist "C:\Program Files (x86)\NSIS\makensis.exe" set MAKENSIS="C:\Program Files (x86)\NSIS\makensis.exe"
if exist "C:\Program Files\NSIS\makensis.exe" set MAKENSIS="C:\Program Files\NSIS\makensis.exe"

%MAKENSIS% setup\installer.nsi

if %ERRORLEVEL% EQU 0 (
    echo ======================================================
    echo Instalador generado exitosamente en setup\ControlDeMareas_Setup.exe
    echo ======================================================
) else (
    echo ======================================================
    echo ERROR: No se pudo generar el instalador. 
    echo Asegurate de tener NSIS instalado (http://nsis.sourceforge.net)
    echo ======================================================
)
pause
