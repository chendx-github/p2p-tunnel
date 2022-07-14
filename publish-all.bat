@echo off
SET target=%~dp0

cd %target%/public
rd /s /q publish-all
del /s /q publish-all.rar

echo publish client ==============================================
cd %target%/client/client.service
dotnet publish -c release -f net6.0 -o %target%/public/publish-all/client/default			-p:DebugType=none -p:DebugSymbols=false
echo publish win-----------------------------------------------
dotnet publish -c release -f net6.0 -o %target%/public/publish-all/client/win-x64			-r win-x64		--self-contained false  -p:DebugType=none -p:DebugSymbols=false
dotnet publish -c release -f net6.0 -o %target%/public/publish-all/client/win-x64-single	-r win-x64		--self-contained true  -p:DebugType=none -p:DebugSymbols=false  -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true
dotnet publish -c release -f net6.0 -o %target%/public/publish-all/client/win-arm64			-r win-arm64	--self-contained false  -p:DebugType=none -p:DebugSymbols=false
dotnet publish -c release -f net6.0 -o %target%/public/publish-all/client/win-arm64-single	-r win-arm64	--self-contained true  -p:DebugType=none -p:DebugSymbols=false  -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true
echo publish linux--------------------------------------------
dotnet publish -c release -f net6.0 -o %target%/public/publish-all/client/linux-x64				-r linux-x64	--self-contained false -p:DebugType=none -p:DebugSymbols=false
dotnet publish -c release -f net6.0 -o %target%/public/publish-all/client/linux-x64-single		-r linux-x64	--self-contained true  -p:DebugType=none -p:DebugSymbols=false  -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true
dotnet publish -c release -f net6.0 -o %target%/public/publish-all/client/linux-arm64			-r linux-arm64	--self-contained false -p:DebugType=none -p:DebugSymbols=false
dotnet publish -c release -f net6.0 -o %target%/public/publish-all/client/linux-arm64-single	-r linux-arm64	--self-contained true  -p:DebugType=none -p:DebugSymbols=false  -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true
echo publish osx--------------------------------------------
dotnet publish -c release -f net6.0 -o %target%/public/publish-all/client/osx-x64			-r osx-x64		--self-contained false -p:DebugType=none -p:DebugSymbols=false
dotnet publish -c release -f net6.0 -o %target%/public/publish-all/client/osx-x64-single	-r osx-x64		--self-contained true  -p:DebugType=none -p:DebugSymbols=false  -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true
dotnet publish -c release -f net6.0 -o %target%/public/publish-all/client/osx-arm64			-r osx-arm64	--self-contained false -p:DebugType=none -p:DebugSymbols=false
dotnet publish -c release -f net6.0 -o %target%/public/publish-all/client/osx-arm64-single	-r osx-arm64	--self-contained true  -p:DebugType=none -p:DebugSymbols=false  -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true
echo publish client ============================================== OK

echo publish client tray =========================================
cd %target%/client/client.service.tray
dotnet publish -c release -o %target%/public/publish-all/client/default/tray  -p:DebugType=none -p:DebugSymbols=false
dotnet publish -c release -o %target%/public/publish-all/client/win-x64/tray			-r win-x64		--self-contained false -p:DebugType=none -p:DebugSymbols=false
dotnet publish -c release -o %target%/public/publish-all/client/win-x64-single/tray		-r win-x64		--self-contained false -p:DebugType=none -p:DebugSymbols=false
dotnet publish -c release -o %target%/public/publish-all/client/win-arm64/tray			-r win-arm64	--self-contained false -p:DebugType=none -p:DebugSymbols=false
dotnet publish -c release -o %target%/public/publish-all/client/win-arm64-single/tray	-r win-arm64	--self-contained false -p:DebugType=none -p:DebugSymbols=false

echo publish client command ======================================
cd %target%/client/client.service.command
dotnet publish -c release -f net6.0 -o %target%/public/publish-all/client/default/command	-p:DebugType=none -p:DebugSymbols=false
dotnet publish -c release -f net6.0 -o %target%/public/publish-all/client/win-x64/command				-r win-x64		--self-contained false -p:DebugType=none -p:DebugSymbols=false
dotnet publish -c release -f net6.0 -o %target%/public/publish-all/client/win-x64-single/command		-r win-x64		--self-contained true  -p:DebugType=none -p:DebugSymbols=false  -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true
dotnet publish -c release -f net6.0 -o %target%/public/publish-all/client/win-arm64/command				-r win-arm64	--self-contained false -p:DebugType=none -p:DebugSymbols=false
dotnet publish -c release -f net6.0 -o %target%/public/publish-all/client/win-arm64-single/command		-r win-arm64	--self-contained true  -p:DebugType=none -p:DebugSymbols=false  -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true
dotnet publish -c release -f net6.0 -o %target%/public/publish-all/client/linux-x64/command				-r linux-x64	--self-contained false -p:DebugType=none -p:DebugSymbols=false
dotnet publish -c release -f net6.0 -o %target%/public/publish-all/client/linux-x64-single/command		-r linux-x64	--self-contained true  -p:DebugType=none -p:DebugSymbols=false  -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true
dotnet publish -c release -f net6.0 -o %target%/public/publish-all/client/linux-arm64/command			-r linux-arm64	--self-contained false -p:DebugType=none -p:DebugSymbols=false
dotnet publish -c release -f net6.0 -o %target%/public/publish-all/client/linux-arm64-single/command	-r linux-arm64	--self-contained true  -p:DebugType=none -p:DebugSymbols=false  -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true
dotnet publish -c release -f net6.0 -o %target%/public/publish-all/client/osx-x64/command				-r osx-x64		--self-contained false -p:DebugType=none -p:DebugSymbols=false
dotnet publish -c release -f net6.0 -o %target%/public/publish-all/client/osx-x64-single/command		-r osx-x64		--self-contained true  -p:DebugType=none -p:DebugSymbols=false  -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true
dotnet publish -c release -f net6.0 -o %target%/public/publish-all/client/osx-arm64/command				-r osx-arm64	--self-contained false -p:DebugType=none -p:DebugSymbols=false
dotnet publish -c release -f net6.0 -o %target%/public/publish-all/client/osx-arm64-single/command		-r osx-arm64	--self-contained true  -p:DebugType=none -p:DebugSymbols=false  -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true



echo publish server ==============================================
cd %target%/server/server.service
dotnet publish -c release -f net6.0 -o %target%/public/publish-all/server/default	-p:DebugType=none -p:DebugSymbols=false
echo publish win----------------------------------------------
dotnet publish -c release -f net6.0 -o %target%/public/publish-all/server/win-x64			-r win-x64		--self-contained false  -p:DebugType=none -p:DebugSymbols=false
dotnet publish -c release -f net6.0 -o %target%/public/publish-all/server/win-x64-single	-r win-x64		--self-contained true  -p:DebugType=none -p:DebugSymbols=false  -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true
dotnet publish -c release -f net6.0 -o %target%/public/publish-all/server/win-arm64			-r win-arm64	--self-contained false  -p:DebugType=none -p:DebugSymbols=false
dotnet publish -c release -f net6.0 -o %target%/public/publish-all/server/win-arm64-single	-r win-arm64	--self-contained true  -p:DebugType=none -p:DebugSymbols=false  -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true
echo publish linux--------------------------------------------
dotnet publish -c release -f net6.0 -o %target%/public/publish-all/server/linux-x64				-r linux-x64	--self-contained false  -p:DebugType=none -p:DebugSymbols=false
dotnet publish -c release -f net6.0 -o %target%/public/publish-all/server/linux-x64-single		-r linux-x64	--self-contained true -p:DebugType=none -p:DebugSymbols=false  -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true
dotnet publish -c release -f net6.0 -o %target%/public/publish-all/server/linux-arm64			-r linux-arm64	--self-contained false  -p:DebugType=none -p:DebugSymbols=false
dotnet publish -c release -f net6.0 -o %target%/public/publish-all/server/linux-arm64-single	-r linux-arm64	--self-contained true -p:DebugType=none -p:DebugSymbols=false  -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true
echo publish osx--------------------------------------------
dotnet publish -c release -f net6.0 -o %target%/public/publish-all/server/osx-x64			-r osx-x64		--self-contained false  -p:DebugType=none -p:DebugSymbols=false
dotnet publish -c release -f net6.0 -o %target%/public/publish-all/server/osx-x64-single	-r osx-x64		--self-contained true -p:DebugType=none -p:DebugSymbols=false  -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true
dotnet publish -c release -f net6.0 -o %target%/public/publish-all/server/osx-arm64			-r osx-arm64	--self-contained false  -p:DebugType=none -p:DebugSymbols=false
dotnet publish -c release -f net6.0 -o %target%/public/publish-all/server/osx-arm64-single	-r osx-arm64	--self-contained true -p:DebugType=none -p:DebugSymbols=false  -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true

echo publish server ============================================== OK

cd %target%/public
winrar a -r publish-all.rar ./publish-all

cd %target%
pause