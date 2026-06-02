<#
.Synopsis
    This script upgrades pip, installs Fabric-CLI  python package, and logs in to Fabric-CLI, needed to run tests against Fabric SQL Database with dynamic create/drop.

    **Make sure you've Python installed first.**
.Switch Interactive
    Interactive login to Fabric-CLI
.Parameter ClientId
    The Azure Client ID used for non-interactive authentication. Defaults to the AZURE_CLIENT_ID environment variable if not provided.

.Parameter TenantId
    The Azure Tenant ID used for non-interactive authentication. Defaults to the AZURE_TENANT_ID environment variable if not provided.

.Parameter IdToken
    The Azure ID Token used for federated authentication. Defaults to the AZURE_ID_TOKEN environment variable if not provided.

.Example
    Install-Fabric-Cli.ps1 -Interactive
    Runs the script with interactive login for Fabric-CLI authentication.

.Example
    Install-Fabric-Cli.ps1 -ClientId "your-client-id" -TenantId "your-tenant-id" -IdToken "your-id-token"
    Runs the script with non-interactive federated authentication using the provided ClientId, TenantId, and IdToken.
#>
param (
    [switch] $Interactive,
    [string] $ClientId = $env:AZURE_CLIENT_ID, # Default to environment variable if not provided
    [string] $TenantId = $env:AZURE_TENANT_ID, # Default to environment variable if not provided
    [string] $IdToken = $env:AZURE_ID_TOKEN # Default to environment variable if not provided
)

# Set global error config value,
# so script will stop executing if there will be any error in any command
$ErrorActionPreference = "Stop";

# Upgrade pip
try {
    python -m pip install --upgrade pip
} catch {
    Write-Error "Failed to upgrade pip. Ensure Python is installed and accessible."
    exit 1
}
# Install ms-fabric-cli
try {
    python -m pip install ms-fabric-cli==1.5.0
} catch {
    Write-Error "Failed to install ms-fabric-cli. Check Python and pip configuration."
    exit 1
}

# Configure Fabric CLI - e.g. https://microsoft.github.io/fabric-cli/examples/files/azure-pipeline.yml
try {
   fab config set encryption_fallback_enabled true
} catch {
    Write-Error "Failed to set Fabric CLI configuration. Check if the config key is valid and the CLI is properly installed."
    exit 1
}

# Authenticate Fabric CLI

if ($Interactive.IsPresent) {
    Write-Host "Using interactive authentication..."
    fab auth login
} else {
   if (-not $ClientId) {
       Write-Error "ClientId is required for non-interactive authentication. Ensure the parameter or environment variable AZURE_CLIENT_ID is set."
       exit 1
   }
   if (-not $TenantId) {
       Write-Error "TenantId is required for non-interactive authentication. Ensure the parameter or environment variable AZURE_TENANT_ID is set."
       exit 1
   }

   Write-Host "Using federated authentication to login to prod..."
   fab auth login -u $ClientId --tenant $TenantId --federated-token $IdToken

   Write-Host "Logging in to daily environment. Not sure it is needed, but some tests use daily environment."
   $env:FAB_API_ENDPOINT_FABRIC = "dailyapi.fabric.microsoft.com"
   fab auth login -u $ClientId --tenant $TenantId --federated-token $IdToken
   $env:FAB_API_ENDPOINT_FABRIC = ""
}

# Test Fabric CLI
fab ls
