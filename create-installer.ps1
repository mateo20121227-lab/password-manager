param(
    [string]$Configuration = 'Release',
    [string]$Runtime = 'win-x64',
    [string]$OutputDir = "$PSScriptRoot\installer"
)

$project = Join-Path $PSScriptRoot 'PasswordManager.csproj'
$publishRoot = Join-Path $PSScriptRoot 'publish'
$publishDir = Join-Path $publishRoot $Runtime
$installerExe = Join-Path $OutputDir 'PasswordManagerInstaller.exe'
$sedPath = Join-Path $OutputDir 'installer.sed'

Write-Host 'Cleaning previous output...'
Remove-Item -Recurse -Force $publishRoot, $OutputDir -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $publishDir -Force | Out-Null
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

$dotnetPath = "C:\Program Files\dotnet\dotnet.exe"
if (-not (Test-Path $dotnetPath)) {
    $command = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($command) {
        $dotnetPath = $command.Source
    }
}
if (-not $dotnetPath -or -not (Test-Path $dotnetPath)) {
    throw 'dotnet CLI not found. Please install .NET SDK or add dotnet to PATH.'
}

Write-Host 'Publishing application...'
& "$dotnetPath" publish $project -c $Configuration -r $Runtime -o $publishDir /p:PublishSingleFile=true /p:PublishTrimmed=false /p:SelfContained=true /p:UseAppHost=true

$exe = Join-Path $publishDir 'PasswordManager.exe'
if (-not (Test-Path $exe)) {
    throw "Publish failed: $exe not found"
}

Write-Host 'Publish complete, preparing installer files...'

$installerRelativeExe = 'PasswordManager.exe'
$installerPublishExe = Join-Path $publishDir $installerRelativeExe
$installerCopyExe = Join-Path $OutputDir $installerRelativeExe
Copy-Item -Path $installerPublishExe -Destination $installerCopyExe -Force

$tempPackageDir = Join-Path ([System.IO.Path]::GetTempPath()) 'PasswordManagerInstaller'
Remove-Item -Recurse -Force $tempPackageDir -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $tempPackageDir -Force | Out-Null
$installerTempExe = Join-Path $tempPackageDir $installerRelativeExe
Copy-Item -Path $installerPublishExe -Destination $installerTempExe -Force
$installerTempSede = Join-Path $tempPackageDir 'installer.sed'
$installerTempOutput = Join-Path $tempPackageDir 'PasswordManagerInstaller.exe'

$appIconSource = Join-Path $PSScriptRoot 'app.ico'
$appIconPackage = Join-Path $tempPackageDir 'app.ico'
if (Test-Path $appIconSource) {
    Copy-Item -Path $appIconSource -Destination $appIconPackage -Force
}

$installScript = @'
$installDir = Join-Path $env:LOCALAPPDATA 'Programs\Password Manager'
New-Item -ItemType Directory -Path $installDir -Force | Out-Null
Copy-Item -Path '.\PasswordManager.exe' -Destination (Join-Path $installDir 'PasswordManager.exe') -Force
if (Test-Path '.\app.ico') {
    Copy-Item -Path '.\app.ico' -Destination (Join-Path $installDir 'app.ico') -Force
}
$desktop = [Environment]::GetFolderPath('Desktop')
$shortcutPath = Join-Path $desktop 'Password Manager.lnk'
$wsh = New-Object -ComObject WScript.Shell
$shortcut = $wsh.CreateShortcut($shortcutPath)
$shortcut.TargetPath = Join-Path $installDir 'PasswordManager.exe'
if (Test-Path (Join-Path $installDir 'app.ico')) {
    $shortcut.IconLocation = Join-Path $installDir 'app.ico'
}
$shortcut.Save()
Write-Host "Installed Password Manager to $installDir"
Read-Host 'Press Enter to close this window'
'@

$installScriptPath = Join-Path $tempPackageDir 'install.ps1'
Set-Content -Path $installScriptPath -Value $installScript -Encoding UTF8

$installBat = @'
@echo off
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0install.ps1"
'@

$installBatPath = Join-Path $tempPackageDir 'install.bat'
Set-Content -Path $installBatPath -Value $installBat -Encoding ASCII

$sedContent = @"
[Version]
Class=IEXPRESS

[Options]
PackagePurpose=InstallApp
ShowInstallProgramWindow=1
HideExtractAnimation=0
UseLongFileName=1
InsideCompressed=1
CAB_FixedSize=0
RebootMode=I
InstallPrompt=Do you want to install Password Manager?
DisplayLicense=0
FinishMessage=Password Manager was installed successfully.
TargetName=PasswordManagerInstaller.exe
FriendlyName=Password Manager
InstallProgram=install.bat
PostInstallCmd=
UseSourceFileDir=1

[SourceFiles]
SourceFiles0=SourceFiles0

[SourceFiles0]
Path=.
File1=PasswordManager.exe
File2=install.bat
File3=install.ps1
File4=app.ico
"@

Set-Content -Path $installerTempSede -Value $sedContent -Encoding ASCII

Write-Host 'Building installer with IExpress...'
$iexpressPath = 'C:\Windows\System32\iexpress.exe'
if (-not (Test-Path $iexpressPath)) {
    $command = Get-Command iexpress.exe -ErrorAction SilentlyContinue
    if ($command) {
        $iexpressPath = $command.Source
    }
}
if (-not $iexpressPath -or -not (Test-Path $iexpressPath)) {
    throw 'IExpress not found on this machine.'
}

Push-Location $tempPackageDir
try {
    & "$iexpressPath" '/N' "$installerTempSede"
} finally {
    Pop-Location
}

if (-not (Test-Path $installerTempOutput)) {
    throw "Installer build failed at $installerTempOutput"
}

Move-Item -Path $installerTempOutput -Destination $installerExe -Force

if (-not (Test-Path $installerExe)) {
    throw "Installer build failed: $installerExe not found"
}

Write-Host "Installer created successfully:" $installerExe
