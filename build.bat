@echo off
echo ===================================================
echo   Building BeamMP-GUI...
echo ===================================================
"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe" "AppSource\BeamMP-GUI.csproj" /p:Configuration=Release /v:minimal
if %ERRORLEVEL% EQU 0 (
    echo.
    echo ===================================================
    echo   BUILD SUCCESSFUL!
    echo   Output: BeamMP-GUI.exe
    echo ===================================================
) else (
    echo.
    echo ===================================================
    echo   BUILD FAILED! Please check error output above.
    echo ===================================================
)
pause
