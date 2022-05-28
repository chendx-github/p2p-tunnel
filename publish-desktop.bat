@echo off
SET target=%~dp0

cd %target%/client/client.service
dotnet publish -c release -f net6.0 -o d:/free/p2p-tunnel/ -p:DebugType=none -p:DebugSymbols=false
cd %target%/client/client.service.tray
dotnet publish -c release -o d:/free/p2p-tunnel/tray -p:DebugType=none -p:DebugSymbols=false

pause