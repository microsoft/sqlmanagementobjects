# Introduction

This repo is the home of Sql Management Object development. It will produce NuGet packages for internal and external use.

## Getting Started

- Install Visual Studio 2022 or newer.
- Clone the repo
- Run [.scripts\DisableStrongName.ps1](./.scripts/DisableStrongName.ps1) as administrator
- Open a command prompt where the VS msbuild.exe is in PATH. Developer Command Prompt works well.
- Run init.cmd to install prerequisites, set variables used by tests and install the appropriate .NET SDK

## Test Server Access

In order to run tests against the default test servers for this repo you will need access to the ClientToolsInfra_TME subscription in the TME tenant.

Follow the steps outlined in [the wiki](https://msdata.visualstudio.com/SQLToolsAndLibraries/_wiki/wikis/SQLToolsAndLibraries.wiki/24457/Testing) to get access. Note that access can take a while to be approved, so get this done early!

## Build and Test

To open a project in Visual Studio:

1.Open a VS 2022 Developer Command Prompt
2.Initialize the environment. The environment variable shouldn't be needed if you joined a `tm-sqldb` myaccess group and have logged in to Windows using a PIN.

   ```cmd
    D:\smo> init.cmd
   ```

3.Ensure you're up to date:

```cmd

   D:\smo> git checkout main
   D:\smo> git pull

 ```

4.OPTIONAL: Execute 'clean' to remove anything left over: [NOTE: If you have uncommitted changes, please stash: git stash or create a local branch and push them before clean comman]

   ```cmd
   D:\smo> clean
   ```

5.Build SMO from the command line:

   ```cmd
   D:\smo> msbuild src\Microsoft\SqlServer\Management\Smo\Microsoft.SqlServer.Smo.csproj
   ```

   **Note:** when you run init.cmd, you should see a bunch of shortcuts that you can use to build and run
   tests without having to remember this particular line.

6.Remove the solution file:

   ```cmd
   D:\smo> del src\Microsoft\SqlServer\Management\Smo\Microsoft.SqlServer.Smo.sln
   ```

7.Recreate the solution:

   ```cmd
   D:\smo> slngen src\Microsoft\SqlServer\Management\Smo\Microsoft.SqlServer.Smo.csproj
   ```

   **ProTip:** slngen on the src\FunctionalTest\Smo\Microsoft.SqlServer.Test.Smo.csproj project will include
   the above SMO project as a dependency, so you can make changes there and run tests in the same project.

slngen will open Visual Studio with the newly created solution. If it opens an old VS version, consider changing the
default .sln in windows by opening `Choose default apps by file type` and finding the .sln extension,
clicking on the right side will allow you to choose a new default.

Functional tests are in src\FunctionalTest\Smo\Microsoft.SqlServer.Test.Smo.csproj
Tests will run automatically as part of the PR process.

To run tests locally against a subset of the test servers, create a copy of functionaltest.runsettings and edit this parameter, removing unneeded servers. Use your new runsettings file as input to vstest or visual studio. There are per-SQL-version runsettings [files](./src/FunctionalTest/Framework/) available.

```xml

<TestRunParameters>
    <Parameter name="SqlTestTargetServersFilter" value="Sql2008;Sql2008R2;Sql2012;Sql2014;Sql2016;Sql2017;AzureSterlingV12;AzureSterlingV12_SqlDW;Sqlv150;Sql2016Express;Sql2017Express;Sqlv150Express" />
</TestRunParameters>

```

To run tests locally against custom test servers, you can override the default list of test servers by defining the following env variable

``` cmd

SET TestPath=Y:\MyCustomListOfServer

```

and provide your own `ToolsConnectionInfo.xml` under that folder. The default file that the automation uses is [%BASEDIR%/src/FunctionalTest/Environment/ToolsConnectionInfo.xml](https://msdata.visualstudio.com/SQLToolsAndLibraries/_git/SqlManagementObjects?path=/src/FunctionalTest/Environment/ToolsConnectionInfo.xml) so you can just grab that and tweak it as needed.

To run tests in Azure Devops, push your private branch and queue this build and pick your branch as the target: [PR Verification ADO](https://msdata.visualstudio.com/SQLToolsAndLibraries/_build?definitionId=17944). This is the same pipeline that will run when you create a PR.

Baseline tests that fail locally will automatically copy your updated baseline files to their proper locations in the source tree when running locally (as long as you've run init.cmd first). If you use the PR Verification pipeline above to run your tests you can use the [SMO Commit Baseline changes](https://msdata.visualstudio.com/SQLToolsAndLibraries/_build?definitionId=52412) pipeline to copy over the updated baselines into your branch by queueing a new build of that pipeline against your branch and giving it the ID (from the URL) of the PR Verification pipeline.

### To Run tests locally against Fabric Workspace

To run tests locally against Fabric Workspace with dynamic create/drop database,  run the [Install-Fabric-Cli.ps1](./.scripts/Install-Fabric-Cli.ps1) script. See that script for instructions on usage.
This command will install the Fabric CLI and logs in to Fabric CLI interactively. It would open a shell window and browser to interactively authenticate.

``` cmd
D:\smo> powershell .scripts\Install-Fabric-Cli.ps1 -Interactive
```


``` cmd

``` cmd
D:\smo> powershell .scripts\Install-Fabric-Cli.ps1 -Interactive
```


``` cmd
### Running tests from the command line

The rtests doskey found in init.cmd can be modified to run individual tests from the command line. Put the name of the test to run in the /tests: parameter and this will execute just your test. 
The .trx files can be found in the "TestResults" folder that is created automatically. 
There are multiple locations to run tests from %BASEDIR%bin\debug\net472, %BASEDIR%bin\debug\net8.0 and %BASEDIR%bin\debug\net10.0 
To run baseline tests use microsoft.sqlserver.test.smointernal.dll instead of microsoft.sqlserver.test.smo.dll.

``` cmd

vstest.console.exe microsoft.sqlserver.test.smo.dll /logger:trx /Settings:%BASEDIR%src\FunctionalTest\Framework\functionaltest.runsettings /tests:Script_Filtered_Statistics

```
Also see [runtests.cmd](src/FunctionalTest/runtests.cmd)

### Building in SSMS against local SMO changes

During development, it's often useful to be able to build and test changes in SSMS at the same time you're making changes in SMO.

If the changes are binary compatible (that is - not adding any new classes/methods/properties/etc.) then follow the instructions in [Testing in SSMS](#testing-in-ssms) to copy over the updated SMO binaries to the SSMS installation.

But if changes in SSMS need to use some new class/method/property directly then you will need to update the SSMS repo to reference the local copy of SMO with your changes so that it can build against that instead of the published binaries.

To do this run the [Update-SSMS-Repo.ps1](./.scripts/Update-SSMS-Repo.ps1) script. See it for more information about what it does and how to run it.

### Testing in SSMS

To test private SMO binaries with an SSMS installation, run the [Update-SSMS.ps1](./.scripts/Update-SSMS.ps1) script. See that script for instructions on usage.

You'll also need to disable strong name verification on the SSMS machine by running [.scripts\DisableStrongName.ps1](./.scripts/DisableStrongName.ps1) as administrator.

If you decide to copy over the binaries manually, make sure you build with `/p:SignBuild=true` (alias `bssms`) to make sure your binaries have the right public key token.

### Build Issues

If you run into build issues try deleting the 'bin' folder and the 'src\obj' folder and rebuilding. The 'clean' doskey command can also be run to do part of this.

## Contribute

Read the [associated project-wide wiki](https://msdata.visualstudio.com/SqlServerManagementStudio/_wiki/wikis/SqlServerManagementStudio.wiki/3082/Welcome-to-SqlServerManagementStudio) for information on how the code gets built and packaged and consumed.

In-depth documentation for writing SMO code is in the new md-based [wiki](/docs/README.md).

There's also the [old OneNote for SMO development](https://microsoft.sharepoint.com/teams/sqldsdt/_layouts/OneNote.aspx?id=%2Fteams%2Fsqldsdt%2FShared%20Documents%2FClient%20Tools%2FTooling-MasterPlan%2FTooling%20Master%20Notebook&wd=target%28SMO.one%7C9BC69EC5-5FE8-42A1-A33F-31F1BEBA3779%2F%29)

If you find valuable content in the old OneNote which is missing from the Azure Devops-hosted wiki, please copy it.
Update [CHANGELOG.md](CHANGELOG.md) when adding new features or fixing user-facing bugs.
Eventually this ChangeLog will get pushed to github, and in the short term the relevant
sections will be included in the Documentation section of the nuget.org package.
We will group changes by nuget package version.

The PR verifier runs the tests against a set of servers (PRverifier.runsettings).
Before committing, be sure to run the entire test suite against all servers (i.e. using functionaltest.runsettings) locally.
Note, the _rtests_ alias (defined when _init.cmd_ is run) will run this.

### C# Style

- spaces, no tabs
- {} around all if bodies
- minimize "this." usage
- preserve whatever naming conventions exist in a given file, we aren't interested in renaming member variables from "m_member" to "member" as parts of bug fixes or other refactoring
- explicit private modifier
- use nameof operator instead of constants for property names. Please make this change in existing code near your change!
- use interpolated strings instead of string.Format

**When replacing a call to `String.Format(SmoApplication.DefaultCulture...)` or adding a new interpolated string, check the type of the arguments. Dates, times, and decimal values are rendered in culture-specific ways, so use `FormattableString.Invariant($"...")` when needed. Script generation in particular should always be culture-invariant.**

- new classes should use these naming conventions:

Identifier | Style | Example
--- | --- | ---
field | camel case | `private int memberName;`
property | Pascal case | `public int MyProperty { get; set; }`

### Updating nuget dependencies

This repo relies on the [CentralPackageVersions SDK](https://github.com/Microsoft/MSBuildSdks/tree/main/src/CentralPackageVersions) to enforce use of a single version of each nuget dependency. When adding new dependencies or updating the versions of current dependencies, update [Directory.Packages.Props](./Directory.Packages.props) to list the package and its version. Do not put `Version` attributes on `PackageReference` tags in individual projects.

### Dos and Don'ts

- Email ssmsdevteam _before_ starting to write code and propose your object model changes for review.
- Run all tests that have `Baseline` in their name and follow the instructions in the logs for any that fail.
- Refactor code to enable unit tests as you fix bugs or add features. We are trying to increase unit test code coverage during the build.
- Write unit tests.
- Use the constraint-based NUnit asserts for all tests.
- All code changes under src\Microsoft require an accompanying test change unless an existing test found the bug. Exceptions to this bar will be rare.
- Update the wiki!
- Test your changes in SSMS as well as verifying automated tests pass.
- Examine msbuild.binlog using the [structured log viewer](https://msbuildlog.com/). Ensure there are no "Double Writes". Typically a double write can be fixed by pinning [package versions](./Directory.Packages.props)
- Try to add a unit test that shows your new SMO objects are usable in DesignMode

### Potential Issues and Solutions

- if init fails because it cannot find msbuild from Developer Command Prompt check if there are extra entries in PATH and clear out unneeded entries and retry
- The GAC version builds after `171.41.0` do not include Microsoft.SqlServer.BatchParserClient.dll. If you want to run the tests against a GAC version you'll need to copy the DLL from an older build or from a SQL Server 2022 build drop or installation. That said, we haven't run the tests with a GAC build in quite some time. As SQL Server vbump ramps up we will address that gap by getting batchparserclient from a nuget package and running the tests in a pipeline.

### Resolving CDPx CredScan scanning issues

- CredScan errors will fail the CDPx build if any non-suppressed hard coded credentials are found.
- Check for the file CredScanSuppressions.json in .config folder that is used to to Suppress false positives and other inaccuracies
- For details on CredScan issues and warnings, navigate to CredScan-matches.xml. This file can be found in Build Artifacts under Static Analysis Results __Source Analysis_ -> SourceAnalysis -> CredScan
- User will need to get the hash key of the secret from CredScan-matches.xml
- A template is provided for the suppressions of false positives and from the CredScan-matches.xml add the hash keys to the file .config\CredScanSuppressions.json
- This Suppress all occurrences of a given secret within the specified InputPath.
- For more suppression scenarios, <https://strikecommunity.azurewebsites.net/articles/4127/suppression-scenarios.html>

**Note:** CredScan will be run in the Source Analysis stage of all Windows builds, and all issues will show as warnings and NOT fail the build. After a period of time, builds will start failing when CredScan finds credentials.
