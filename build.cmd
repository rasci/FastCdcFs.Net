@echo off
setlocal

set VERSION=1.0.0-alpha01

dotnet pack -c Release -o publish FastCdcFs.Net.Reader -p:Version=%VERSION% || exit /b 1
dotnet pack -c Release -o publish FastCdcFs.Net.Writer -p:Version=%VERSION% || exit /b 1
dotnet pack -c Release -o publish FastCdcFs.Net.Shell -p:Version=%VERSION% || exit /b 1

endlocal