@echo off
SET target=%~dp0

cd %target%/public
rd /s /q publish
del /s /q publish.rar

echo publish client ==============================================
cd %target%/client/client.service
dotnet publish -c release -f net6.0 -o %target%/public/publish/client/default						--self-contained false	-p:DebugType=none -p:DebugSymbols=false
dotnet publish -c release -f net6.0 -o %target%/public/publish/client/win-x64		-r win-x64		--self-contained false	-p:DebugType=none -p:DebugSymbols=false
dotnet publish -c release -f net6.0 -o %target%/public/publish/client/win-arm64		-r win-arm64	--self-contained false	-p:DebugType=none -p:DebugSymbols=false
dotnet publish -c release -f net6.0 -o %target%/public/publish/client/linux-x64		-r linux-x64	--self-contained false	-p:DebugType=none -p:DebugSymbols=false
dotnet publish -c release -f net6.0 -o %target%/public/publish/client/linux-arm64	-r linux-arm64	--self-contained false	-p:DebugType=none -p:DebugSymbols=false
dotnet publish -c release -f net6.0 -o %target%/public/publish/client/osx-x64		-r osx-x64		--self-contained false	-p:DebugType=none -p:DebugSymbols=false
dotnet publish -c release -f net6.0 -o %target%/public/publish/client/osx-arm64		-r osx-arm64	--self-contained false	-p:DebugType=none -p:DebugSymbols=false
echo publish client ============================================== OK

echo publish client tray =========================================
cd %target%/client/client.service.tray
dotnet publish -c release -o %target%/public/publish/client/win-x64/tray	-r win-x64		--self-contained false	-p:DebugType=none -p:DebugSymbols=false
dotnet publish -c release -o %target%/public/publish/client/win-arm64/tray	-r win-arm64	--self-contained false	-p:DebugType=none -p:DebugSymbols=false

echo publish client command ======================================
cd %target%/client/client.service.command
dotnet publish -c release -f net6.0 -o %target%/public/publish/client/default/command						--self-contained false	-p:DebugType=none -p:DebugSymbols=false
dotnet publish -c release -f net6.0 -o %target%/public/publish/client/win-x64/command		-r win-x64		--self-contained false	-p:DebugType=none -p:DebugSymbols=false
dotnet publish -c release -f net6.0 -o %target%/public/publish/client/win-arm64/command		-r win-arm64	--self-contained false	-p:DebugType=none -p:DebugSymbols=false
dotnet publish -c release -f net6.0 -o %target%/public/publish/client/linux-x64/command		-r linux-x64	--self-contained false	-p:DebugType=none -p:DebugSymbols=false
dotnet publish -c release -f net6.0 -o %target%/public/publish/client/linux-arm64/command	-r linux-arm64	--self-contained false	-p:DebugType=none -p:DebugSymbols=false
dotnet publish -c release -f net6.0 -o %target%/public/publish/client/osx-x64/command		-r osx-x64		--self-contained false	-p:DebugType=none -p:DebugSymbols=false
dotnet publish -c release -f net6.0 -o %target%/public/publish/client/osx-arm64/command		-r osx-arm64	--self-contained false	-p:DebugType=none -p:DebugSymbols=false

echo publish server ==============================================
cd %target%/server/server.service
dotnet publish -c release -f net6.0 -o %target%/public/publish/server/default						--self-contained false	-p:DebugType=none -p:DebugSymbols=false
dotnet publish -c release -f net6.0 -o %target%/public/publish/server/win-x64		-r win-x64		--self-contained false	-p:DebugType=none -p:DebugSymbols=false
dotnet publish -c release -f net6.0 -o %target%/public/publish/server/win-arm64		-r win-arm64	--self-contained false	-p:DebugType=none -p:DebugSymbols=false
dotnet publish -c release -f net6.0 -o %target%/public/publish/server/linux-x64		-r linux-x64	--self-contained false	-p:DebugType=none -p:DebugSymbols=false
dotnet publish -c release -f net6.0 -o %target%/public/publish/server/linux-arm64	-r linux-arm64	--self-contained false	-p:DebugType=none -p:DebugSymbols=false
dotnet publish -c release -f net6.0 -o %target%/public/publish/server/osx-x64		-r osx-x64		--self-contained false	-p:DebugType=none -p:DebugSymbols=false
dotnet publish -c release -f net6.0 -o %target%/public/publish/server/osx-arm64		-r osx-arm64	--self-contained false	-p:DebugType=none -p:DebugSymbols=false
echo publish server ============================================== OK

cd %target%/public
winrar a -r publish.rar ./publish

cd %target%
pause