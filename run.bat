@echo off
echo Building and Running ArtStudio (Debug)...
echo.

:: Build first
echo [1/2] Building project...
dotnet build --configuration Debug
if %ERRORLEVEL% NEQ 0 (
    echo ❌ Build failed!
    pause
    exit /b 1
)

:: Run the application
echo [2/2] Starting ArtStudio...
echo.
dotnet run --project src/ArtStudio.WPF --configuration Debug
