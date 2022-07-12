@echo off
SET target=%~dp0

cd %target%/public
rd /s /q publish
del /s /q publish.rar

echo publish client ==============================================
cd %target%/client/client.service
dotnet publish -c release -f net6.0 -o %target%/public/publish/client/default			-p:DebugType=none -p:DebugSymbols=false
echo publish win-x64----------------------------------------------
dotnet publish -c release -f net6.0 -o %target%/public/publish/client/win-x64			-r win-x64 --self-contained false  -p:DebugType=none -p:DebugSymbols=false
dotnet publish -c release -f net6.0 -o %target%/public/publish/client/win-x64-single	-r win-x64 --self-contained true  -p:DebugType=none -p:DebugSymbols=false  -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true
echo publish linux-x64--------------------------------------------
dotnet publish -c release -f net6.0 -o %target%/public/publish/client/linux-x64			-r linux-x64 --self-contained false -p:DebugType=none -p:DebugSymbols=false
dotnet publish -c release -f net6.0 -o %target%/public/publish/client/linux-x64-single	-r linux-x64 --self-contained true  -p:DebugType=none -p:DebugSymbols=false  -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true
echo publish client ============================================== OK

echo publish client tray =========================================
cd %target%/client/client.service.tray
dotnet publish -c release -o %target%/public/publish/client/default/tray -r win-x64 --self-contained false  -p:DebugType=none -p:DebugSymbols=false
dotnet publish -c release -o %target%/public/publish/client/win-x64/tray -r win-x64 --self-contained false -p:DebugType=none -p:DebugSymbols=false

echo publish client command ======================================
cd %target%/client/client.service.command
dotnet publish -c release -f net6.0 -o %target%/public/publish/client/default/command			-r win-x64 --self-contained false -p:DebugType=none -p:DebugSymbols=false
dotnet publish -c release -f net6.0 -o %target%/public/publish/client/win-x64/command			-r win-x64 --self-contained false -p:DebugType=none -p:DebugSymbols=false
dotnet publish -c release -f net6.0 -o %target%/public/publish/client/win-x64-single/command	-r win-x64 --self-contained true  -p:DebugType=none -p:DebugSymbols=false  -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true
dotnet publish -c release -f net6.0 -o %target%/public/publish/client/linux-x64/command			-r linux-x64 --self-contained false -p:DebugType=none -p:DebugSymbols=false
dotnet publish -c release -f net6.0 -o %target%/public/publish/client/linux-x64-single/command	-r linux-x64 --self-contained true  -p:DebugType=none -p:DebugSymbols=false  -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true


echo publish server ==============================================
cd %target%/server/server.service
dotnet publish -c release -f net6.0 -o %target%/public/publish/server/default			-p:DebugType=none -p:DebugSymbols=false
echo publish win-x64----------------------------------------------
dotnet publish -c release -f net6.0 -o %target%/public/publish/server/win-x64			-r win-x64 --self-contained false  -p:DebugType=none -p:DebugSymbols=false
dotnet publish -c release -f net6.0 -o %target%/public/publish/server/win-x64-single	-r win-x64 --self-contained true  -p:DebugType=none -p:DebugSymbols=false  -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true
echo publish linux-x64--------------------------------------------
dotnet publish -c release -f net6.0 -o %target%/public/publish/server/linux-x64			-r linux-x64 --self-contained false  -p:DebugType=none -p:DebugSymbols=false
dotnet publish -c release -f net6.0 -o %target%/public/publish/server/linux-x64-single	-r linux-x64 --self-contained true -p:DebugType=none -p:DebugSymbols=false  -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true
echo publish server ============================================== OK

cd %target%/public
winrar a -r publish.rar ./publish
pause