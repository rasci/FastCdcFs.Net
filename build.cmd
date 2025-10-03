@echo off
setlocal

set VERSION=1.0.0-alpha02

REM test before packing

dotnet test || exit /b 1

REM packing

dotnet pack -c Release -o publish FastCdcFs.Net -p:Version=%VERSION% || exit /b 1
dotnet pack -c Release -o publish FastCdcFs.Net.Shell -p:Version=%VERSION% || exit /b 1

endlocal