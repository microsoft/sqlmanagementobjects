#This script is FOR TEST ONLY. It bypasses StrongName verification
#for SQL assemblies, since they are delay signed the installer
#will fail on non-signed builds unless we add the exceptions.


#List of SQL public keys
$publicKeys = @(
        "*,31bf3856ad364e35", 
        "*,36e4ce08b8ecfb17", 
        "*,89845dcd8080cc91", 
        "*,b03f5f7f11d50a3a", 
        "*,ae41e2615877eb90",
        "*,b77a5c561934e089")
 
$publicKeys | % {
    New-Item -Path "HKLM:\SOFTWARE\Microsoft\StrongName\Verification\$_"             -Force | Out-Null
    New-Item -Path "HKLM:\SOFTWARE\Wow6432Node\Microsoft\StrongName\Verification\$_" -Force | Out-Null
}