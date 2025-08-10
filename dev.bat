@echo off
echo 🛠️  ArtStudio Development Helper
echo.
echo Choose an option:
echo [1] Run in Debug mode (recommended for development)
echo [2] Run in Release mode
echo [3] Run tests
echo [4] Clean and rebuild
echo [5] Open in VS Code
echo [6] Exit
echo.

set /p choice=Enter your choice (1-6): 

if "%choice%"=="1" (
    echo.
    echo 🔧 Running in Debug mode...
    dotnet run --project src/ArtStudio.WPF --configuration Debug
) else if "%choice%"=="2" (
    echo.
    echo 🚀 Running in Release mode...
    dotnet run --project src/ArtStudio.WPF --configuration Release
) else if "%choice%"=="3" (
    echo.
    echo 🧪 Running tests...
    dotnet test --configuration Debug --verbosity normal
) else if "%choice%"=="4" (
    echo.
    echo 🧹 Cleaning and rebuilding...
    dotnet clean
    dotnet build --configuration Debug
) else if "%choice%"=="5" (
    echo.
    echo 💻 Opening in VS Code...
    code .
) else if "%choice%"=="6" (
    echo.
    echo 👋 Goodbye!
    exit /b 0
) else (
    echo.
    echo ❌ Invalid choice. Please run the script again.
    pause
    exit /b 1
)

echo.
pause
