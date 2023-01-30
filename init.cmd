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

REM == Common test command:
doskey rtests=pushd %BASEDIR%target\distrib\debug\net462$Tvstest.console.exe microsoft.sqlserver.test.smo.dll /logger:trx /TestCaseFilter:"(TestCategory != Staging)" /Settings:%BASEDIR%src\FunctionalTest\Framework\functionaltest.runsettings $*
doskey netcoretests=pushd %BASEDIR%target\distrib\debug\netcoreapp3.1$Tvstest.console.exe microsoft.sqlserver.test.smo.dll /logger:trx /TestCaseFilter:"(TestCategory != Staging)" /Settings:%BASEDIR%src\FunctionalTest\Framework\functionaltest.runsettings $*

title git %BASEDIR%

set CommitIdForCurrentBuild=%ComputerName%

dotnet tool install --global Microsoft.VisualStudio.SlnGen.Tool

echo To open solution with SMO (code+test):
echo    slngen src\FunctionalTest\Smo\Microsoft.SqlServer.Test.Smo.csproj
echo    REM == Then use "Test | Configure Run Settings | Select Solution Wide Runsettings File"
echo    REM == and point it to one of the .runsettings under %BASEDIR%src\FunctionalTest\Framework
echo    REM == Select "Test | Test Explorer" and you are ready to run tests!
echo    pushd %BASEDIR%target\distrib\debug\net462
echo    REM == If you want to trim down the list of servers, use something like this:
echo    REM == SET SqlTestTargetServersFilter=Sql2017;Sqlv150
echo    REM == See %BASEDIR%src\FunctionalTest\Framework\ConnectionInfo.xml 
echo    REM == for all the friendly names available.
echo    REM == You'll need to edit connection strings in src/functionaltest/framework/toolsconnectioninfo.xml.
echo    vstest.console.exe microsoft.sqlserver.test.smo.dll /TestCaseFilter:"(TestCategory != Staging)" /logger:trx /Settings:%BASEDIR%src\FunctionalTest\Framework\functionaltest.runsettings
echo.
echo To run tests for netcore (alias: netcoretests)
echo    pushd %BASEDIR%target\distrib\debug\netcoreapp3.1
echo    REM == If you want to trim down the list of servers, use something like this:
echo    REM == SET SqlTestTargetServersFilter=Sql2017;Sqlv150
echo    REM == See %BASEDIR%src\FunctionalTest\Framework\ConnectionInfo.xml 
echo    REM == for all the friendly names available.
echo    REM == You'll need to edit connection strings in src/functionaltest/framework/toolsconnectioninfo.xml.
echo    vstest.console.exe microsoft.sqlserver.test.smo.dll /TestCaseFilter:"(TestCategory != Staging)" /logger:trx /Settings:%BASEDIR%src\FunctionalTest\Framework\functionaltest.runsettings

