@echo off
SET target=%~dp0

cd %target%/public
rd /s /q publish
del /s /q publish.rar

echo publish client ==============================================
cd %target%/client/client.service
dotnet publish -c release -f net6.0 -o %target%/public/publish/client/default						--self-contained false	-p:DebugType=none -p:DebugSymbols=false
echo publish client ============================================== OK
echo publish server ==============================================
cd %target%/server/server.service
dotnet publish -c release -f net6.0 -o %target%/public/publish/server/default						--self-contained false	-p:DebugType=none -p:DebugSymbols=false
echo publish server ============================================== OK

cd %target%/public
winrar a -r publish.rar ./publish

cd %target%
pause