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
doskey out=pushd %BASEDIR%target\distrib\$*
doskey prod=pushd %BASEDIR%src\Microsoft\SqlServer\Management\$*
doskey sfc=pushd %BASEDIR%src\Microsoft\SqlServer\Management\Sdk\sfc
doskey smo=pushd %BASEDIR%src\Microsoft\SqlServer\Management\Smo
doskey smoenum=pushd %BASEDIR%src\Microsoft\SqlServer\Management\SqlEnum\$*
doskey clean=powershell.exe -ExecutionPolicy Unrestricted -File "%BASEDIR%init.ps1" -Clean
doskey tst=pushd %BASEDIR%src\FunctionalTest\Smo\$*
doskey tsti=pushd %BASEDIR%src\FunctionalTest\SmoInternal\$*
doskey slngen19=slngen -vs "C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\Common7\IDE\devenv.exe" $*

REM == Common build commands:
doskey msb=msbuild /p:EnableLocalization=false $*
doskey msbnolock=msbuild /p:EnableLocalization=false /p:RestoreLockedMode=false $*
doskey msbupdatelocks=msbuild /p:EnableLocalization=false /p:RestoreForceEvaluate=true $*
doskey bssms18=msbuild /p:SignBuild=true /p:EnableLocalization=false /p:MicrosoftDataBuild=false %BASEDIR%dirs.proj
doskey bssms=msbuild /p:SignBuild=true /p:EnableLocalization=false /p:MicrosoftDataBuild=true %BASEDIR%dirs.proj
doskey bsmo=msbuild /p:SignBuild=false /p:EnableLocalization=false %BASEDIR%dirs.proj
doskey bpkgs=pushd %BASEDIR%src\PackageBuild$Tbuildpublicsmonuget.cmd$Tbuildssmssmonuget.cmd$Tbuildgacnuget.cmd

REM == Common test command:
doskey rtests=pushd %BASEDIR%target\distrib\debug\net462$Tvstest.console.exe microsoft.sqlserver.test.smo.dll /logger:trx /TestCaseFilter:"(TestCategory != Staging)" /Settings:%BASEDIR%src\FunctionalTest\Framework\functionaltest.runsettings $*
doskey netcoretests=pushd %BASEDIR%target\distrib\debug\netcoreapp3.1$Tvstest.console.exe microsoft.sqlserver.test.smo.dll /logger:trx /TestCaseFilter:"(TestCategory != Staging)" /Settings:%BASEDIR%src\FunctionalTest\Framework\functionaltest.runsettings $*

title git %BASEDIR%

REM Migration to PowerShell environment
powershell.exe -ExecutionPolicy Unrestricted -File "%BASEDIR%init.ps1" -Initialize
if "%errorlevel%" neq "0" (
    echo Failed to setup local dev build environment correctly
)

dotnet tool install --global Microsoft.VisualStudio.SlnGen.Tool

echo.
echo.
echo Assembly Signing is OFF by default. To enable:
echo    SET SignBuild=true or msbuild /p:SignBuild=true ...
echo LOC building is ON by default. To disable:
echo    SET EnableLocalization=false or pass /p:EnableLocalization=false
echo.
echo To build for SSMS 18.x development enable signing and disable mds (alias: bssms18):
echo    msbuild /p:SignBuild=true /p:EnableLocalization=false /p:MicrosoftDataBuild=false %BASEDIR%dirs.proj
echo.
echo To build for SSMS 19.x+ development enable signing and enable mds (alias: bssms):
echo    msbuild /p:SignBuild=true /p:EnableLocalization=false /p:MicrosoftDataBuild=true %BASEDIR%dirs.proj
echo.
echo To build for SMO development and to run tests (alias: bsmo):
echo    msbuild /p:SignBuild=false /p:EnableLocalization=false %BASEDIR%dirs.proj
echo.
echo To build and create NuGet packages (alias: bpkgs):
echo    pushd %BASEDIR%src\PackageBuild
echo    buildpublicsmonuget.cmd^&^&buildssmssmonuget.cmd^&^&buildgacnuget.cmd
echo.
echo To open solution with SMO (code+test):
echo    slngen src\FunctionalTest\Smo\Microsoft.SqlServer.Test.Smo.csproj
echo    REM == Then use "Test | Configure Run Settings | Select Solution Wide Runsettings File"
echo    REM == and point it to one of the .runsettings under %BASEDIR%src\FunctionalTest\Framework
echo    REM == Select "Test | Test Explorer" and you are ready to run tests!
echo.
echo To run tests (alias: rtests):
echo    pushd %BASEDIR%target\distrib\debug\net462
echo    REM == If you want to trim down the list of servers, use something like this:
echo    REM == SET SqlTestTargetServersFilter=Sql2017;Sqlv150
echo    REM == See %BASEDIR%src\FunctionalTest\Framework\ConnectionInfo.xml 
echo    REM == for all the friendly names available.
echo    vstest.console.exe microsoft.sqlserver.test.smo.dll /TestCaseFilter:"(TestCategory != Staging)" /logger:trx /Settings:%BASEDIR%src\FunctionalTest\Framework\functionaltest.runsettings
echo.
echo To run tests for netcore (alias: netcoretests)
echo    pushd %BASEDIR%target\distrib\debug\netcoreapp3.1
echo    REM == If you want to trim down the list of servers, use something like this:
echo    REM == SET SqlTestTargetServersFilter=Sql2017;Sqlv150
echo    REM == See %BASEDIR%src\FunctionalTest\Framework\ConnectionInfo.xml 
echo    REM == for all the friendly names available.
echo    vstest.console.exe microsoft.sqlserver.test.smo.dll /TestCaseFilter:"(TestCategory != Staging)" /logger:trx /Settings:%BASEDIR%src\FunctionalTest\Framework\functionaltest.runsettings
exit /b %errorlevel%

