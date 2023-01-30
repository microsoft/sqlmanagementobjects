param (
    [switch] $Clean = $false,
    [switch] $Initialize = $false
)

if ($Clean)
{
    if (Test-Path $PSScriptRoot\target)
    {
        Write-Output "Removing $PSScriptRoot\target"
        Remove-Item -Force -Recurse $PSScriptRoot\target
    }
    Get-ChildItem -Directory -Recurse $PSScriptRoot\obj | ForEach-Object {Write-Output "Removing $_"; Remove-Item -Force -Recurse $_}
}

if ($Initialize)
{
    . $PSScriptRoot\installcredprovider.ps1
}