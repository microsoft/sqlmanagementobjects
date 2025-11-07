<#
.Synopsis
    Builds the SMO projects and then copies the DLLs & PDBs into the specified SSMS install location. You will need to run
    this as admin if the install location is a protected folder such as Program Files.

    **Make sure you've ran DisableStrongName.ps1 first as we need to delay-sign the assemblies locally for SSMS to load them.**
.Parameter SSMSPath
    The path to the installation of SSMS to update
.Switch NoBuild
    Whether to skip all build steps
.Example
    Update-SSMS.ps1 -SSMSPath "C:\Program Files\Microsoft SQL Server Management Studio 21\Preview" -NoBuild
#>

param(
    [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string]$SSMSPath,
    [switch]$NoBuild
)

# Set global error config value,
# so script will stop executing if there will be any error in any command
$ErrorActionPreference = "Stop";

if ($NoBuild.IsPresent -eq $false)
{
    dotnet build $PSScriptRoot\..\dirs.proj /p:SignBuild=true /p:EnableLocalization=false /p:TargetFramework=net472 /p:RestoreLockedMode=false /p:SkipUnitTests=true
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed!"
    }
}

# Names of the files (without the .dll filext) to update
$filesToUpdate =
    "Microsoft.SqlServer.ConnectionInfo",
    "Microsoft.SqlServer.Dmf.Common",
    "Microsoft.SqlServer.Dmf",
    "Microsoft.SqlServer.Management.Collector",
    "Microsoft.SqlServer.Management.CollectorEnum",
    "Microsoft.SqlServer.Management.HadrData",
    "Microsoft.SqlServer.Management.HadrModel",
    "Microsoft.SqlServer.Management.RegisteredServers",
    "Microsoft.SqlServer.Management.Sdk.Sfc",
    "Microsoft.SqlServer.Management.SmoMetadataProvider",
    "Microsoft.SqlServer.Management.SqlScriptPublish",
    "Microsoft.SqlServer.Management.XEvent",
    "Microsoft.SqlServer.Management.XEventDbScoped",
    "Microsoft.SqlServer.Management.XEventDbScopedEnum",
    "Microsoft.SqlServer.Management.XEventEnum",
    "Microsoft.SqlServer.PolicyEnum",
    "Microsoft.SqlServer.RegSvrEnum",
    "Microsoft.SqlServer.ServiceBrokerEnum",
    "Microsoft.SqlServer.Smo",
    "Microsoft.SqlServer.Smo.Notebook",
    "Microsoft.SqlServer.SmoExtended",
    "Microsoft.SqlServer.SqlClrProvider",
    "Microsoft.SqlServer.SqlEnum",
    "Microsoft.SqlServer.SqlWmiManagement"

foreach ($filename in $filesToUpdate) {
    $dllFilename = "$filename.dll"
    $pdbFilename = "$filename.pdb"
    $destFolder = [IO.Path]::Combine($SSMSPath, "Common7\IDE");
    $destPathDLL = [IO.Path]::Combine($destFolder, $dllFilename)
    Write-Host "Updating " $destPathDLL
    # First check to see if we already have a backup copy of the original DLL
    # If we don't then we move the current one to *.bak in case we want to revert
    # easily later
    $backupFilename = $dllFilename + ".bak"
    $backupPath = [IO.Path]::Combine($destFolder, $backupFilename)
    if (-not (Test-Path -Path $backupPath)) {
        Write-Host "Backing up " $dllFilename " to " $backupFilename
        Move-Item $destPathDLL $backupPath
    }
    # Now copy the locally built DLL and PDB to the SSMS folder
    $srcPathDLL = [IO.Path]::Combine($PSScriptRoot, "..\bin\Debug\net472\$dllFilename")
    $srcPathPDB = [IO.Path]::Combine($PSScriptRoot, "..\bin\Debug\net472\$pdbFilename")
    Write-Host "Copying " $srcPathDLL
    Copy-Item $srcPathDLL $destFolder
    Copy-Item $srcPathPDB $destFolder
}
