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
doskey prod=pushd %BASEDIR%src\Microsoft\SqlServer\Management\$*
doskey sfc=pushd %BASEDIR%src\Microsoft\SqlServer\Management\Sdk\sfc
doskey smo=pushd %BASEDIR%src\Microsoft\SqlServer\Management\Smo
doskey smoenum=pushd %BASEDIR%src\Microsoft\SqlServer\Management\SqlEnum\$*
doskey tst=pushd %BASEDIR%src\FunctionalTest\Smo\$*
doskey slngen19=slngen -vs "C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\Common7\IDE\devenv.exe" $*

REM == Common test command:
doskey rtests=pushd %BASEDIR%bin\debug\net472$Tvstest.console.exe microsoft.sqlserver.test.smo /logger:trx /TestCaseFilter:"(TestCategory != Staging)" /Settings:%BASEDIR%src\FunctionalTest\Framework\functionaltest.runsettings $*
doskey netcoretests=pushd %BASEDIR%bin\debug\net8.0$Tvstest.console.exe microsoft.sqlserver.test.smo /logger:trx /TestCaseFilter:"(TestCategory != Staging)" /Settings:%BASEDIR%src\FunctionalTest\Framework\functionaltest.runsettings $*

title git %BASEDIR%

dotnet tool install --global Microsoft.VisualStudio.SlnGen.Tool
IF NOT EXIST %BASEDIR%packages\StrawberryPerl.5.28.0.1\bin\perl.exe (
  %BASEDIR%Build\Local\Nuget\nuget.exe install StrawberryPerl -Version 5.28.0.1
)

echo.
echo To open solution with SMO (code+test):
echo    slngen src\FunctionalTest\Smo\Microsoft.SqlServer.Test.Smo.csproj
echo    REM == Then use "Test | Configure Run Settings | Select Solution Wide Runsettings File"
echo    REM == and point it to one of the .runsettings under %BASEDIR%src\FunctionalTest\Framework
echo    REM == You'll need to edit connection strings in src/functionaltest/framework/toolsconnectioninfo.xml.
echo    REM == Select "Test | Test Explorer" and you are ready to run tests!
echo.
echo To run tests (alias: rtests):
echo    pushd %BASEDIR%bin\debug\net472
echo    REM == If you want to trim down the list of servers, use something like this:
echo    REM == SET SqlTestTargetServersFilter=Sql2017;Sqlv150
echo    REM == See %BASEDIR%bin\Debug\net472\ToolsConnectionInfo.xml
echo    REM == for all the friendly names available.
echo    REM == You'll need to edit connection strings in src/functionaltest/framework/toolsconnectioninfo.xml.
echo    vstest.console.exe microsoft.sqlserver.test.smo.dll /TestCaseFilter:"(TestCategory != Staging)" /logger:trx /Settings:%BASEDIR%src\FunctionalTest\Framework\functionaltest.runsettings
echo.
echo To run tests for netcore (alias: netcoretests)
echo    pushd %BASEDIR%bin\debug\net6.0
echo    REM == If you want to trim down the list of servers, use something like this:
echo    REM == SET SqlTestTargetServersFilter=Sql2017;Sqlv150
echo    REM == See %BASEDIR%bin\Debug\net472\ToolsConnectionInfo.xml
echo    REM == for all the friendly names available.
echo    REM == You'll need to edit connection strings in src/functionaltest/framework/toolsconnectioninfo.xml.
echo    vstest.console.exe microsoft.sqlserver.test.smo.dll /TestCaseFilter:"(TestCategory != Staging)" /logger:trx /Settings:%BASEDIR%src\FunctionalTest\Framework\functionaltest.runsettings
echo.

