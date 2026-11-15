@echo off
title Create Desktop Shortcut for SEQI
setlocal enabledelayedexpansion

set "APP_DIR=%~dp0"
if "%APP_DIR:~-1%"=="\" set "APP_DIR=%APP_DIR:~0,-1%"

set "TARGET_EXE=%APP_DIR%\SEQI.exe"
set "ICON_PATH=%APP_DIR%\app.ico"

echo =====================================================================
echo           Creating Desktop Shortcut for SEQI Application
echo =====================================================================
echo.

if not exist "%TARGET_EXE%" (
    echo [ERROR] Could not find %TARGET_EXE%
    pause
    exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
    "$wshell = New-Object -ComObject WScript.Shell; " ^
    "$desktop = [System.Environment]::GetFolderPath('Desktop'); " ^
    "$shortcutPath = Join-Path $desktop 'SEQI - School Quality System.lnk'; " ^
    "$shortcut = $wshell.CreateShortcut($shortcutPath); " ^
    "$shortcut.TargetPath = '%TARGET_EXE%'; " ^
    "$shortcut.WorkingDirectory = '%APP_DIR%'; " ^
    "$shortcut.IconLocation = '%ICON_PATH%,0'; " ^
    "$shortcut.Description = 'SEQI - Institutional Quality Assurance System'; " ^
    "$shortcut.Save(); " ^
    "Write-Host '[*] Shortcut successfully created on your Desktop: ' $shortcutPath"

echo.
echo =====================================================================
echo You can now double-click 'SEQI - School Quality System' on your
echo Windows Desktop to launch the app at any time!
echo =====================================================================
echo.
pause

