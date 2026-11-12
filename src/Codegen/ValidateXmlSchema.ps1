<#
.SYNOPSIS
    Validates XML files against an XSD schema file.

.DESCRIPTION
    This script validates all XML files in a specified folder (including subfolders) against an XSD schema.
    The script compiles the schema and validates each XML file, reporting any validation errors or exceptions.

.PARAMETER XmlFilesFolder
    The folder path containing XML files to validate. The script will recursively search for files matching 
    the Include pattern in this folder and all subfolders.

.PARAMETER SchemaFile
    The path to the XSD schema file used for validation. The script will exit with an error if this file 
    does not exist.

.PARAMETER Include
    File pattern to match when searching for XML files. Defaults to "*.xml" to match all XML files.
    Can be customized to validate specific file patterns (e.g., "EnumObject_*.xml").

.EXAMPLE
    .\ValidateXmlSchema.ps1 -XmlFilesFolder "C:\Project\xml" -SchemaFile "C:\Project\schema.xsd"
    
    Validates all *.xml files in C:\Project\xml and subfolders against schema.xsd

.EXAMPLE
    .\ValidateXmlSchema.ps1 -XmlFilesFolder ".\xml" -SchemaFile ".\EnumObject.xsd" -Include "*.xml"
    
    Validates all XML files matching *.xml pattern against EnumObject.xsd

.NOTES
    Exit Codes:
    - 0: All files validated successfully
    - 1: Validation errors found or script error (schema not found, no XML files, etc.)
    
    MSBuild Integration:
    Error messages use the format "ValidateXmlSchema.ps1: error VSX001: message" which MSBuild automatically
    recognizes and includes in the build error summary. See https://learn.microsoft.com/en-us/visualstudio/msbuild/exec-task
#>
param(
    [Parameter(Mandatory=$true)]
    [string]$XmlFilesFolder,
    
    [Parameter(Mandatory=$true)]
    [string]$SchemaFile,
    
    [Parameter(Mandatory=$false)]
    [string]$Include = "*.xml"
)

# Validate schema file exists
if (-not (Test-Path $SchemaFile)) {
    Write-Host "ERROR: Schema file not found: $SchemaFile" -ForegroundColor Red
    exit 1
}

# Load schema
$schemaSet = New-Object System.Xml.Schema.XmlSchemaSet
$schemaSet.Add($null, $SchemaFile) | Out-Null
$schemaSet.Compile()

# Get XML files to validate
$xmlFiles = Get-ChildItem -Path $XmlFilesFolder -Include $Include -File -Recurse

if ($xmlFiles.Count -eq 0) {
    Write-Host "ERROR: No XML files found in: $XmlFilesFolder" -ForegroundColor Red
    exit 1
}

Write-Host "Validating $($xmlFiles.Count) XML files in folder: $XmlFilesFolder against schema: $SchemaFile" -ForegroundColor Cyan
Write-Host ""

$validationErrors = @()
$validatedCount = 0

foreach ($xmlFile in $xmlFiles) {    
    try {
        $xmlDoc = New-Object System.Xml.XmlDocument
        $xmlDoc.Schemas = $schemaSet

        $fileErrors = @()
        $validationEventHandler = {
            param($sender, $e)
            $script:fileErrors += "$($e.Severity): $($e.Message) at line $($e.Exception.LineNumber), position $($e.Exception.LinePosition)"
        }
        
        $xmlDoc.Load($xmlFile.FullName)
        $xmlDoc.Validate($validationEventHandler)
        
        if ($fileErrors.Count -gt 0) {
            $validationErrors += "File: $($xmlFile.FullName)"
            $validationErrors += $fileErrors
            $validationErrors += ""
        }
        else {
            $validatedCount++
        }
    }
    catch {
        $validationErrors += "File: $($xmlFile.FullName)"
        $validationErrors += "ERROR: $($_.Exception.Message)"
        $validationErrors += ""
    }
}

Write-Host ""

if ($validationErrors.Count -gt 0) {
    # In order for the messages to show up in the build summary we prefix the important messages with 
    # "ValidateXmlSchema.ps1: error VSX001: " so that msbuild recognizes it as
    # an error. Otherwise users will have to dig up through the logs to find the actual errors.
    # See IgnoreStandardErrorWarningFormat in https://learn.microsoft.com/en-us/visualstudio/msbuild/exec-task?view=visualstudio
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "XML Schema Validation Failed" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host ""
    $validationErrors | ForEach-Object { Write-Host "ValidateXmlSchema.ps1: error VSX001: $_" -ForegroundColor Red }
    Write-Host ""
    Write-Host "ValidateXmlSchema.ps1: error VSX001: Fix the above errors and try again. If the schema is incorrect update $SchemaFile."
    Write-Host "Summary: $validatedCount of $($xmlFiles.Count) files validated successfully" -ForegroundColor Red
    exit 1
}
else {
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "All XML files validated successfully!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "Total files validated: $validatedCount" -ForegroundColor Green
    exit 0
}
