@echo off
SET target=%~dp0

echo publish client ==============================================
cd %target%/client/client.service
dotnet publish -c release  -f net6.0 -o ./public/publish/default -p:DebugType=none -p:DebugSymbols=false
echo publish win-x64----------------------------------------------
dotnet publish -c release -r win-x64 --self-contained false  -f net6.0 -o ./public/publish/win-x64 -p:DebugType=none -p:DebugSymbols=false
dotnet publish -c release -r win-x64 --self-contained true -f net6.0 -o ./public/publish-single/win-x64 -p:DebugType=none -p:DebugSymbols=false  -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true
echo publish linux-x64--------------------------------------------
dotnet publish -c release -r linux-x64 --self-contained false  -f net6.0 -o ./public/publish/linux-x64 -p:DebugType=none -p:DebugSymbols=false
dotnet publish -c release -r linux-x64 --self-contained true -f net6.0 -o ./public/publish-single/linux-x64 -p:DebugType=none -p:DebugSymbols=false  -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true
echo publish client ============================================== OK


cd %target%/client/client.service.tray
dotnet publish -c release -r win-x64 --self-contained false -o %target%/client/client.service/public/publish/win-x64/tray -p:DebugType=none -p:DebugSymbols=false
rem dotnet publish -c release -r win-x64 --self-contained true -o %target%/client/client.service/public/publish-single/win-x64/tray -p:DebugType=none -p:DebugSymbols=false  -p:PublishSingleFile=true  -p:IncludeNativeLibrariesForSelfExtract=true


echo publish server ==============================================
cd %target%/server/server.service
dotnet publish -c release  -f net6.0 -o ./public/publish/default -p:DebugType=none -p:DebugSymbols=false
echo publish win-x64----------------------------------------------
dotnet publish -c release -r win-x64 --self-contained false -f net6.0 -o ./public/publish/win-x64 -p:DebugType=none -p:DebugSymbols=false
dotnet publish -c release -r win-x64 --self-contained true -f net6.0 -o ./public/publish-single/win-x64 -p:DebugType=none -p:DebugSymbols=false  -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true
echo publish linux-x64--------------------------------------------
dotnet publish -c release -r linux-x64 --self-contained false -f net6.0 -o ./public/publish/linux-x64 -p:DebugType=none -p:DebugSymbols=false
dotnet publish -c release -r linux-x64 --self-contained true -f net6.0 -o ./public/publish-single/linux-x64 -p:DebugType=none -p:DebugSymbols=false  -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true
echo publish server ============================================== OK


pause