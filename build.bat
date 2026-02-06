@echo off
echo ========================================
echo Stick Fight Ultimate - Build Script
echo ========================================
echo.

REM Check if we're in the right directory
if not exist "StickFightUltimate.cs" (
    echo Error: StickFightUltimate.cs not found!
    pause
    exit /b 1
)

REM Check for .NET SDK
echo Checking for .NET SDK...
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo Error: .NET SDK not found!
    echo.
    echo Please install .NET Framework 3.5 SDK or higher from:
    echo https://dotnet.microsoft.com/download/dotnet
    echo.
    pause
    exit /b 1
)

echo Found .NET SDK
echo.

REM Clean previous builds
echo Cleaning previous builds...
if exist "bin" rmdir /s /q "bin" >nul 2>&1
if exist "obj" rmdir /s /q "obj" >nul 2>&1

echo.
echo Building Stick Fight Ultimate...
echo ==================

REM Build the project
dotnet build StickFightCheats_NET35.csproj --configuration Release --verbosity minimal

if %errorlevel% equ 0 (
    echo.
    echo ==================
    echo BUILD SUCCESSFUL!
    echo ==================
    echo.
    echo YOUR MOD DLL IS READY!
    echo Location: bin\Release\net35\StickFightUltimate.dll
    
    if exist "bin\Release\net35\StickFightUltimate.dll" (
        echo.
        echo DLL file size: 
        for %%A in ("bin\Release\net35\StickFightUltimate.dll") do echo   %%~zA bytes
        echo.
        
        echo ========================================
        echo Stick Fight Ultimate Ready
        echo ========================================
        echo.
        echo This version is compatible with:
        echo - .NET Framework 3.5
        echo - MelonLoader
        echo - Stick Fight: The Game
        echo.
        echo Features:
        echo - God Mode (Complete Invincibility)
        echo - Infinite Ammo (Never Run Out)
        echo - No Cooldown (Attack Without Delays)
        echo - Fly Mode (Flight Controls)
        echo - Click Teleport (Right-click Teleport)
        echo - No Clip (Walk Through Walls)
        echo - Instant Win (Auto-win Rounds)
        echo - Kill All Enemies (Eliminate All)
        echo - Weapon Spawning (Spawn Weapons)
        echo.
        echo Controls:
        echo - F1: Toggle Menu
        echo - F2: Toggle God Mode
        echo - Right-click: Teleport (when enabled)
        echo.
        
        set /p copy="Copy to game directory? (y/n): "
        if /i "%copy%"=="y" (
            echo.
            echo Enter your game directory path:
            echo Example: C:\Program Files (x86)\Steam\steamapps\common\StickFightTheGame
            set /p gamepath="Game path: "
            
            if exist "%gamepath%" (
                if not exist "%gamepath%\Mods" mkdir "%gamepath%\Mods"
                
                copy "bin\Release\net35\StickFightUltimate.dll" "%gamepath%\Mods\" >nul 2>&1
                if !errorlevel! (
                    echo.
                    echo ========================================
                    echo INSTALLATION COMPLETE
                    echo ========================================
                    echo.
                    echo YOUR MOD IS INSTALLED AT:
                    echo %gamepath%\Mods\StickFightUltimate.dll
                    echo.
                    echo 1. Launch game with MelonLoader
                    echo 2. Press F1 in-game for cheat menu
                    echo 3. Use F2 for god mode toggle
                    echo.
                ) else (
                    echo.
                    echo Could not copy to Mods folder.
                    echo Manually copy from: bin\Release\net35\StickFightUltimate.dll
                    echo To: [Game Folder]\Mods\
                )
            ) else (
                echo Game directory not found.
                echo Manually copy from: bin\Release\net35\StickFightUltimate.dll
                echo To: [Game Folder]\Mods\
            )
        )
    )
    
) else (
    echo.
    echo ==================
    echo BUILD FAILED!
    echo ==================
    echo.
    echo Check the error messages above.
    echo Common issues:
    echo 1. Missing .NET Framework 3.5 SDK
    echo 2. Wrong MelonLoader version
    echo 3. Missing dependencies
    echo.
)

echo.
pause
