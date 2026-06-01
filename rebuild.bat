@echo off
echo ========================================
echo Rebuilding project...
echo ========================================
echo.

echo [1/4] Cleaning project...
dotnet clean
if %errorlevel% neq 0 (
    echo ERROR: Clean failed!
    pause
    exit /b %errorlevel%
)
echo Clean completed!
echo.

echo [2/4] Removing bin and obj folders...
if exist bin rmdir /s /q bin 2>nul
if exist obj rmdir /s /q obj 2>nul
echo Folders removed!
echo.

echo [3/4] Restoring packages...
dotnet restore
if %errorlevel% neq 0 (
    echo ERROR: Restore failed!
    pause
    exit /b %errorlevel%
)
echo Packages restored!
echo.

echo [4/4] Building project...
dotnet build
if %errorlevel% neq 0 (
    echo ERROR: Build failed!
    pause
    exit /b %errorlevel%
)
echo Build completed successfully!
echo.

echo [5/5] Running application...
echo ========================================
echo.
dotnet run --project Shoes\Shoes.csproj

pause
