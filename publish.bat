@echo off
echo 🚀 Publishing ArtStudio for Windows Distribution...
echo.

:: Clean previous builds
echo [1/4] Cleaning previous builds...
if exist "publish" rmdir /s /q "publish"
dotnet clean --configuration Release

:: Restore dependencies
echo [2/4] Restoring dependencies...
dotnet restore

:: Build in Release mode
echo [3/4] Building in Release mode...
dotnet build --configuration Release --no-restore
if %ERRORLEVEL% NEQ 0 (
    echo ❌ Build failed!
    pause
    exit /b 1
)

:: Publish
echo [4/4] Publishing application...
dotnet publish src/ArtStudio.WPF/ArtStudio.WPF.csproj --configuration Release --output ./publish --self-contained true --runtime win-x64 -p:PublishSingleFile=true

if %ERRORLEVEL% NEQ 0 (
    echo ❌ Publish failed!
    pause
    exit /b 1
)

echo.
echo ✅ Published successfully to: ./publish
echo 📁 Executable: ./publish/ArtStudio.exe
echo 💡 Tip: Use VS Code tasks or CI/CD for automated builds
pause
