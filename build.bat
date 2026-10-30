@echo off
echo ===================================================
echo   Building BeamMP-GUI and Installer...
echo ===================================================
"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe" "AppSource\BeamMP-GUI.csproj" /p:Configuration=Release /v:minimal
if %ERRORLEVEL% EQU 0 (
    copy /Y "BeamMP-GUI.exe" "BeamMP-GUI-Installer.exe" >nul
    echo.
    echo ===================================================
    echo   BUILD SUCCESSFUL!
    echo   Output:
    echo     - BeamMP-GUI.exe
    echo     - BeamMP-GUI-Installer.exe
    echo ===================================================
) else (
    echo.
    echo ===================================================
    echo   BUILD FAILED! Please check error output above.
    echo ===================================================
)
pause
