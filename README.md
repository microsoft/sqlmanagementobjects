<<<<<<< HEAD
# SQL Management Objects

SQL Management Objects, or SMO, provides objects and APIs to discover, modify, and script out SQL Server entities.

## Documentation

See https://docs.microsoft.com/sql/relational-databases/server-management-objects-smo/overview-smo

## Usage

SMO packages on nuget.org include:

### Microsoft.SqlServer.SqlManagementObjects

This package is the primary development SDK for SMO. It provides both NetFx and NetStandard binaries. Capabilities of the NetStandard binaries may be limited by that platform, such as the lack of WMI support.
Version suffixes for this package include "preview" and "msdata". The preview suffix indicates the package was built using System.Data and System.Data.SqlClient as its SQL client driver for NetFx.
The msdata suffix indicate the package uses Microsoft.Data.SqlClient as its SQL client driver for NetFx.
The binaries are strong named and Authenticode signed.

### Microsoft.SqlServer.SqlManagementObjects.Loc

This package has resource DLLs with localized strings corresponding to the DLLs in Microsoft.SqlServer.SqlManagementObjects.

### Microsoft.SqlServer.SqlManagementObjects.SSMS

This package has NetFx binaries that continue to use System.Data.SqlClient as their SQL client driver.
It is mainly intended for use by Sql Server Management Studio and Sql Server Data Tools until such time as those tools can upgrade to Microsoft.Data.SqlClient.
If you are building SSMS 18 extensions that depend on SMO, use this package instead of Microsoft.SqlServer.SqlManagementObjects.

## Microsoft.SqlServer.Management.SmoMetadataProvider

SmoMetadataProvider provides completion support for TSQL language services in Azure Data Studio and the Sql Server extension for VS Code.

## Microsoft.SqlServer.Management.SmoMetadataProvider.SSMS

SmoMetadataProvider.SSMS provides completion support for the TSQL language service in Sql Server Management Studio.

## Versioning

The major version for each SMO release corresponds with the highest Sql Server compatibility level that version of SMO supports.
For example, 140 means it supports SQL Server 2017 and below. Some features of SMO may require having a matching SQL Server version in order to work effectively, but most features are fully backward compatible.

## Dependents

SMO is a integral part of the SQL Server ecosystem. A broad set of client tools, engine components, and service components rely on it extensively. The set of SMO dependents includes:

- Azure Data Studio/Sql Tools Service
- Sql Server Management Studio
- Sql Server Integration Services (SSIS)
- Sql Powershell module
- Sql Data Sync service
- Polybase
- Azure Sql Database
- Microsoft Dynamics
- Sql Server SCOM Management Pack

## Contributing

### Types of contributions

- Please open issues related to bugs or other deficiencies in SMO on the [Issues](https://github.com/microsoft/sqlmanagementobjects/issues) feed of this repo
- Include SMO version where the issue was found
- Include as much of the source code to reproduce the issue as possible
- Ask for sample code for areas where you find the docs lacking
- If you are a SMO application developer, we welcome contributions to the [wiki](https://github.com/microsoft/sqlmanagementobjects/wiki) or even source code samples to illustrate effective ways to use SMO in applications.

### Stuff our attorney added

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
=======
# Introduction

This repo is the home of Sql Management Object development. It will produce NuGet packages for internal and external use.

## Getting Started

- Install Visual Studio 2019 or newer.
- Clone the repo
- Run SmoBuild\DisableStrongName.ps1 as administrator
- Also install the Azure Devops credential provider from [https://github.com/Microsoft/artifacts-credprovider](https://github.com/Microsoft/artifacts-credprovider)
- Open a command prompt where the VS msbuild.exe is in PATH. Developer Command Prompt works well.
- Use init.cmd to set variables used by tests and install the appropriate .net SDK

## Build and Test

Our tests require access to an Azure Key Vault. 
If you do not log in to your dev machine using a smart card or HELLO PIN, you may need to set an environment variable that the tests use as an AKV connection string.  Get the value of the [secret](https://ms.portal.azure.com/#@microsoft.onmicrosoft.com/asset/Microsoft_Azure_KeyVault/Secret/https://sqltoolssecretstore.vault.azure.net/secrets/SmoTestSpn-connection-string).
Create the following 3 environment variables based on the entries in that value:

 - AZURE_CLIENT_ID
 - AZURE_TENANT_ID
 - AZURE_CLIENT_SECRET

You may not need to set these variables if you log in to Visual Studio using your domain credentials which have membership in one of the `tm-sqldb` myaccess groups. See [DefaultAzureCredential docs](https://docs.microsoft.com/dotnet/api/azure.identity.defaultazurecredential?view=azure-dotnet) for more information.

To open a project in Visual Studio:

1.Open a VS 2019 Developer Command Prompt
2.Initialize the environment. The environment variable shouldn't be needed if you joined a `tm-sqldb` myaccess group and have logged in to Windows using a PIN.

   ```cmd
    D:\smo> rem If you have AZ CLI installed uncomment the next line
    D:\smo> rem az keyvault secret show --id https://sqltoolssecretstore.vault.azure.net/secrets/SmoTestSpn-connection-string
    d:\smo> rem If you have Az powershell module use powershell or pwsh as appropriate:
    d:\smo> PowerShell -ExecutionPolicy ByPass -Command "&{ Connect-AzAccount -SubscriptionName ClientToolsInfra_670062; Get-AzKeyVaultSecret -VaultName sqltoolssecretstore -Name SmoTestSpn-connection-string -AsPlainText }"
    D:\smo> rem set AzureServicesAuthConnectionString=<paste secret value from prior step>
    D:\smo> init.cmd

   ```

3.Ensure you're up to date:

```cmd

   D:\smo> git checkout master
   D:\smo> git pull

 ```

4.OPTIONAL: Execute 'clean' to remove anything left over: [NOTE: If you have uncommited changes, please stash: git stash or create a local branch and push them before clean comman]

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

slngen will open VS 2019 with the newly created solution. If it opens an older version, consider changing the
default .sln in windows by opening `Choose default apps by file type` and finding the .sln extension,
clicking on the right side will allow you to choose a new default.

Functional tests are in src\FunctionalTest\Smo\Microsoft.SqlServer.Test.Smo.csproj
Tests will run automatically as part of the PR process.

To run tests locally against a subset of the test servers, create a copy of functionaltest.runsettings and edit this parameter, removing unneeded servers. Use your new runsettings file as input to vstest or visual studio.

```xml

<TestRunParameters>
    <Parameter name="SqlTestTargetServersFilter" value="Sql2008;Sql2008R2;Sql2012;Sql2014;Sql2016;Sql2017;AzureSterlingV12;AzureSterlingV12_SqlDW;Sqlv150;Sql2016Express;Sql2017Express;Sqlv150Express" />
  </TestRunParameters>

```

To run tests in Azure Devops, push your private branch and queue this build and pick your branch as the target: https://msdata.visualstudio.com/SQLToolsAndLibraries/_build?definitionId=7429
You can also just create a PR and the PR verification build will test your branch against sql2017, sql2019, sql2022, Azure DB, and Azure DW.

Baseline tests that fail locally will include instructions on how to copy your updated baseline files to their proper locations in the source tree. If you decide to rely on the Azure Devops build to regenerate baselines, you will need to download the regenerated baselines from the test attachments and manually update them.

To test private SMO binaries with an SSMS installation, build with SignBuild=true to make sure your binaries have the right public key token. You'll need to disable strong name verification on the SSMS machine.

### Running tests from the command line

The rtests doskey found in init.cmd can be modified to run individual tests from the command line. Put the name of the test to run in the /tests: parameter and this will execute just your test. The .trx files can be found in the "TestResults" folder that is created automatically. There are two locations to run tests from %BASEDIR%target\distrib\debug\net462 and %BASEDIR%target\distrib\debug\netcoreapp2.2. To run baseline tests use microsoft.sqlserver.test.smointernal.dll instead of microsoft.sqlserver.test.smo.dll.

``` cmd

vstest.console.exe microsoft.sqlserver.test.smo.dll /logger:trx /Settings:%BASEDIR%src\FunctionalTest\Framework\functionaltest.runsettings /tests:Script_Filtered_Statistics

```

### Build Issues

If you run into build issues try deleting the 'target\distrib' folder and the 'src\obj' folder and rebuilding. The 'clean' doskey command can also be run to do part of this.

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

This repo relies on the [CentralPackageVersions SDK](https://github.com/Microsoft/MSBuildSdks/tree/main/src/CentralPackageVersions) to enforce use of a single version of each nuget dependency. When adding new dependencies or updating the versions of current dependencies, update [Packages.Props](/.Packages.Props) to list the package and its version. Do not put `Version` attributes on `PackageReference` tags in individual projects.

### Dos and Don'ts

- Email ssmsdevteam _before_ starting to write code and propose your object model changes for review.
- Run all tests that have `Baseline` in their name and follow the instructions in the logs for any that fail.
- Refactor code to enable unit tests as you fix bugs or add features. We are trying to increase unit test code coverage during the build.
- Write unit tests.
- Use the constraint-based NUnit asserts for all tests.
- All code changes under src\Microsoft require an accompanying test change unless an existing test found the bug. Exceptions to this bar will be rare.
- Update the wiki!
- Test your changes in SSMS as well as verifying automated tests pass.
- Examine msbuild.binlog using the [structured log viewer](https://msbuildlog.com/). Ensure there are no "Double Writes". If the same target file is being overwritten by multiple versions during the build, you likely have to adjust the set of nuget packages explicitly referenced by each project to ensure only a single version is being used. For example, every project explicitly references `Newtonsoft.json` because there are multiple versions of that package listed as dependencies of other packages we use.

### Potential Issues and Solutions

- if init fails because it cannot find msbuild from Developer Command Prompt check if there are extra entries in PATH and clear out unneeded entries and retry

### Resolving CDPx CredScan scanning issues

- CredScan errors will fail the CDPx build if any non-suppressed hard coded credentials are found.
- Check for the file CredScanSuppressions.json in .config folder that is used to to Suppress false positives and other inaccuracies
- For details on CredScan issues and warnings, navigate to CredScan-matches.xml. This file can be found in Build Artifacts under Static Analysis Results __Source Analysis_ -> SourceAnalysis -> CredScan
- User will need to get the hash key of the secret from CredScan-matches.xml
- A template is provided for the suppressions of false positives and from the CredScan-matches.xml add the hash keys to the file .config\CredScanSuppressions.json
- This Suppress all occurrences of a given secret within the specified InputPath.
- For more suppression scenarios, <https://strikecommunity.azurewebsites.net/articles/4127/suppression-scenarios.html>

**Note:** CredScan will be run in the Source Analysis stage of all Windows builds, and all issues will show as warnings and NOT fail the build. After a period of time, builds will start failing when CredScan finds credentials.
>>>>>>> ddbd3e643d30a121128531089231d45893eb60f1
