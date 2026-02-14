@echo off
echo Building HomeCenter Backup Manager (Release)...
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false

echo.
echo Build complete!
echo.
echo Executable location:
echo bin\Release\net8.0-windows\win-x64\publish\HomeCenterBackup.exe
echo.
pause
