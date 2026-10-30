@echo off
echo ===================================================
echo   Building BeamMP-GUI and Installer...
echo ===================================================
"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe" "AppSource\BeamMP-GUI.csproj" /p:Configuration=Release /v:minimal
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo BUILD FAILED!
    pause
    exit /b %ERRORLEVEL%
)

if exist "%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe" (
    "%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe" "installer.iss" /Q
) else if exist "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" (
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" "installer.iss" /Q
)

echo.
echo ===================================================
echo   BUILD SUCCESSFUL!
echo   Output:
echo     - BeamMP-GUI.exe
echo     - BeamMP-GUI-Installer.exe
echo ===================================================
pause
