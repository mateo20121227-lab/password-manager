Write-Host 'DEBUG: starting IExpress'
$sed = 'C:\Users\mateo\AppData\Local\Temp\PasswordManagerInstaller\installer.sed'
Write-Host "DEBUG: sed path = $sed"
if (-not (Test-Path $sed)) {
    Write-Host 'DEBUG: SED missing'
    exit 1
}
& 'C:\Windows\System32\iexpress.exe' '/N' $sed
Write-Host 'DEBUG: after IExpress'
Write-Host 'DEBUG: exit code = ' $LASTEXITCODE
