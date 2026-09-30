@echo off

REM
REM Setup the developer environment
REM **This script is not run on build machines**
REM
REM Use this script to setup tools which make local development better
REM
set BASEDIR=%~dp0%
if /I "%BASEDIR:~43,1%" NEQ "" (
  if /I "%1" NEQ "/FORCE" (
    echo Currently BASEDIR equals ^"%BASEDIR%^"
    echo Warning: MSBuild failure likely due to MAX_PATH Length errors when packages are installed
    echo Warning: Please use a shorter base path such as C:\src\
    echo run 'init.cmd /force' to bypass this warning
    goto :EOF
  )
)


REM Setup common doskey shortcuts
doskey root=pushd %BASEDIR%$*
doskey src=pushd %BASEDIR%src\$*
doskey out=pushd %BASEDIR%bin\$*
doskey bin=pushd %BASEDIR%bin\$*
doskey ci=pushd %BASEDIR%src\Microsoft\SqlServer\Management\ConnectionInfo
doskey prod=pushd %BASEDIR%src\Microsoft\SqlServer\Management\$*
doskey sfc=pushd %BASEDIR%src\Microsoft\SqlServer\Management\Sdk\sfc
doskey smo=pushd %BASEDIR%src\Microsoft\SqlServer\Management\Smo
doskey smoenum=pushd %BASEDIR%src\Microsoft\SqlServer\Management\SqlEnum\$*

doskey tst=pushd %BASEDIR%src\FunctionalTest\Smo\$*
doskey slngen19=slngen -vs "C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\Common7\IDE\devenv.exe" $*

REM == Common build commands:
doskey msb=msbuild $*
doskey msbnolock=msbuild /p:RestoreLockedMode=false $*
doskey bsmo=msbuild %BASEDIR%dirs.proj
doskey bnr=msbuild /p:BuildProjectReferences=false $*

REM == Common test command:
doskey rtests=pushd %BASEDIR%bin\debug\net472$Tvstest.console.exe microsoft.sqlserver.test.smo.dll /logger:trx /TestCaseFilter:"(TestCategory != Staging)" /Settings:%BASEDIR%src\FunctionalTest\Framework\functionaltest.runsettings $*
doskey netcoretests=pushd %BASEDIR%bin\debug\net8.0$Tvstest.console.exe microsoft.sqlserver.test.smo.dll /logger:trx /TestCaseFilter:"(TestCategory != Staging)" /Settings:%BASEDIR%src\FunctionalTest\Framework\functionaltest.runsettings $*

title git %BASEDIR%

REM Read the required SDK version from global.json so this script never drifts from it
for /f "usebackq tokens=2 delims=:, " %%V in (`findstr /r /c:"\"version\"" %BASEDIR%global.json`) do set DOTNET_SDK_VERSION=%%~V
set DOTNET_SDK_VERSION=%DOTNET_SDK_VERSION:"=%
dotnet --list-sdks | findstr /b /c:"%DOTNET_SDK_VERSION% " >nul
IF ERRORLEVEL 1 (
  echo .NET SDK %DOTNET_SDK_VERSION% ^(required by global.json^) was not found.
  echo Install it from https://dotnet.microsoft.com/download/dotnet/%DOTNET_SDK_VERSION:~0,-4%
)

dotnet tool install --global Microsoft.VisualStudio.SlnGen.Tool
dotnet tool install --global Microsoft.SqlPackage

set NUGET_EXE=%BASEDIR%Build\Local\Nuget\nuget.exe
IF NOT EXIST %NUGET_EXE% (
  echo nuget.exe not found, downloading from https://dist.nuget.org/win-x86-commandline/latest/nuget.exe
  IF NOT EXIST %BASEDIR%Build\Local\Nuget mkdir %BASEDIR%Build\Local\Nuget
  powershell -NoProfile -ExecutionPolicy Bypass -Command "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; Invoke-WebRequest -Uri 'https://dist.nuget.org/win-x86-commandline/latest/nuget.exe' -OutFile '%NUGET_EXE%'"
  IF NOT EXIST %NUGET_EXE% (
    echo Failed to download nuget.exe. Please download it manually from https://dist.nuget.org/win-x86-commandline/latest/nuget.exe and place it at %NUGET_EXE%
    goto :EOF
  )
)
IF NOT EXIST %BASEDIR%packages\StrawberryPerl.5.28.0.1\bin\perl.exe (
  %NUGET_EXE% install StrawberryPerl -Version 5.28.0.1
)

echo.
echo.
echo To build for SMO development and to run tests (alias: bsmo):
echo    msbuild %BASEDIR%dirs.proj
echo.
echo To open solution with SMO (code+test):
echo    slngen src\FunctionalTest\Smo\Microsoft.SqlServer.Test.Smo.csproj
echo    REM == Then use "Test | Configure Run Settings | Select Solution Wide Runsettings File"
echo    REM == and point it to one of the .runsettings under %BASEDIR%src\FunctionalTest\Framework
echo    REM == Select "Test | Test Explorer" and you are ready to run tests!
echo.
echo To run tests (alias: rtests):
echo    pushd %BASEDIR%bin\debug\net472
echo    REM == If you want to trim down the list of servers, use something like this:
echo    REM == SET SqlTestTargetServersFilter=Sql2017;Sqlv150
echo    REM == See %BASEDIR%bin\Debug\net472\ToolsConnectionInfo.xml
echo    REM == for all the friendly names available.
echo    REM == Or pick one of the runsettings files under %BASEDIR%src\FunctionalTest\Framework
echo    vstest.console.exe /TestCaseFilter:"(TestCategory != Staging)" /logger:trx /Settings:%BASEDIR%src\FunctionalTest\Framework\functionaltest.runsettings
echo.
echo To run tests for netcore (alias: netcoretests)
echo    pushd %BASEDIR%bin\debug\net8.0
echo    REM == If you want to trim down the list of servers, use something like this:
echo    REM == SET SqlTestTargetServersFilter=Sql2017;Sqlv150
echo    REM == See %BASEDIR%bin\Debug\net472\ToolsConnectionInfo.xml
echo    REM == for all the friendly names available.
echo    vstest.console.exe /TestCaseFilter:"(TestCategory != Staging)" /logger:trx /Settings:%BASEDIR%src\FunctionalTest\Framework\functionaltest.runsettings
echo.
echo To run functional tests:
echo     REM == Build SMO
echo     bsmo
echo.
echo     REM == See content of runtests.cmd on how to filter the list of backends and tests
echo     REM == otherwise it's going to take a while to run everything...
echo     SET SqlTestTargetServersFilter=Sqlv160
echo     "%BASEDIR%src\FunctionalTest\runtests.cmd"
echo.
echo     REM ** To run fabric tests, you need to have fab cli installed and authenticated
echo     REM ** See %BASEDIR%.scripts\Install-Fabric-Cli.ps1 for details
echo     REM ** fab auth login
exit /b %errorlevel%

