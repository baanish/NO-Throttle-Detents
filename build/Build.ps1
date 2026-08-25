[CmdletBinding()]
param([string]$GameDir)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = [IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$dist = Join-Path $root 'dist'
$artifacts = Join-Path $root 'artifacts'
$cache = Join-Path $root '.cache'
$sdkVersion = '8.0.419'
$bepVersion = '5.4.23.5'
$bepAsset = "BepInEx_win_x64_$bepVersion.zip"
$bepSha256 = '82F9878551030F54657792C0740D9D51A09500EEAE1FBA21106B0C441E6732C4'
$bepLicenseSha256 = 'E6E534EF6F4347B6449407EE046A3D09CB0174C6F688C996AD0BED94B74B3933'
$lgplSha256 = '20E50FE7AAE3E56378EBF0417D9DE904F55A0E61E4DF315333E632A4D3555D95'
$pluginProject = Join-Path $root 'src\NuclearOptionDetents\NuclearOptionDetents.csproj'
$testsProject = Join-Path $root 'tests\NuclearOptionDetents.Tests\NuclearOptionDetents.Tests.csproj'
$pluginSource = Join-Path $root 'src\NuclearOptionDetents\Plugin.cs'
$configSource = Join-Path $root 'src\NuclearOptionDetents\Config\ModConfig.cs'
[xml]$projectXml = Get-Content $pluginProject
$version = [string]$projectXml.Project.PropertyGroup.Version
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw "Invalid plugin version '$version'." }
$pluginSourceText = [IO.File]::ReadAllText($pluginSource)
$pluginVersionMatch = [regex]::Match(
    $pluginSourceText,
    'public\s+const\s+string\s+PluginVersion\s*=\s*"(?<version>[^"]+)"')
if (-not $pluginVersionMatch.Success) { throw 'Plugin.PluginVersion declaration was not found.' }
$pluginVersion = $pluginVersionMatch.Groups['version'].Value
if ($pluginVersion -ne $version) {
    throw "Plugin version mismatch: Plugin.PluginVersion='$pluginVersion', project Version='$version'."
}
$debugLoggingMatch = [regex]::Match(
    [IO.File]::ReadAllText($configSource),
    '(?s)DebugLogging\s*=\s*config\.Bind\(.*?\"DebugLogging\"\s*,\s*(?<value>true|false)\s*,')
if (-not $debugLoggingMatch.Success) { throw 'DebugLogging config binding was not found.' }
if ($debugLoggingMatch.Groups['value'].Value -ne 'false') {
    throw 'DebugLogging source default must be false.'
}
$networkValidationMatch = [regex]::Match(
    [IO.File]::ReadAllText($configSource),
    '(?s)NetworkValidation\s*=\s*config\.Bind\(.*?"NetworkValidation"\s*,\s*(?<value>true|false)\s*,')
if (-not $networkValidationMatch.Success) { throw 'NetworkValidation config binding was not found.' }
if ($networkValidationMatch.Groups['value'].Value -ne 'false') {
    throw 'NetworkValidation source default must be false.'
}

function Invoke-DotNet([string[]]$Arguments) {
    & $script:dotnet @Arguments
    if ($LASTEXITCODE -ne 0) { throw "dotnet $($Arguments -join ' ') failed ($LASTEXITCODE)." }
}

function Get-DotNet {
    $installed = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($installed) {
        $versions = & $installed.Source --list-sdks 2>$null
        if ($versions -match "(?m)^$([regex]::Escape($sdkVersion))\s") { return $installed.Source }
    }
    $local = Join-Path $root '.dotnet\dotnet.exe'
    $localVersion = if (Test-Path $local -PathType Leaf) { & $local --version 2>$null } else { $null }
    if ($localVersion -ne $sdkVersion) {
        New-Item -ItemType Directory -Force -Path $cache | Out-Null
        $script = Join-Path $cache 'dotnet-install.ps1'
        if (-not (Test-Path $script -PathType Leaf)) {
            Invoke-WebRequest 'https://dot.net/v1/dotnet-install.ps1' -OutFile $script
        }
        New-Item -ItemType Directory -Force -Path (Split-Path $local) | Out-Null
        & pwsh -NoProfile -File $script -Version $sdkVersion -InstallDir (Split-Path $local) -NoPath
        if ($LASTEXITCODE -ne 0 -or -not (Test-Path $local -PathType Leaf)) { throw 'Unable to install the pinned .NET SDK.' }
    }
    return $local
}

function Get-VerifiedDownload([string]$Uri, [string]$Destination, [string]$Sha256) {
    New-Item -ItemType Directory -Force -Path (Split-Path $Destination) | Out-Null
    if (Test-Path $Destination -PathType Leaf) {
        if ((Get-FileHash $Destination -Algorithm SHA256).Hash -ne $Sha256) { Remove-Item $Destination -Force }
    }
    if (-not (Test-Path $Destination -PathType Leaf)) { Invoke-WebRequest $Uri -OutFile $Destination }
    $actual = (Get-FileHash $Destination -Algorithm SHA256).Hash
    if ($actual -ne $Sha256) { throw "SHA-256 mismatch for $Destination (expected $Sha256, got $actual)." }
    return $Destination
}

function Get-BepInEx {
    $folder = Join-Path $cache "bepinex-$bepVersion"
    $archive = Join-Path $folder $bepAsset
    $uri = "https://github.com/BepInEx/BepInEx/releases/download/v$bepVersion/$bepAsset"
    [void](Get-VerifiedDownload $uri $archive $bepSha256)
    $rootPath = Join-Path $folder 'extracted'
    $core = Join-Path $rootPath 'BepInEx\core\BepInEx.dll'
    if (-not (Test-Path $core -PathType Leaf)) {
        if (Test-Path $rootPath) { Remove-Item $rootPath -Recurse -Force }
        Expand-Archive $archive $rootPath -Force
    }
    if (-not (Test-Path $core -PathType Leaf)) { throw 'Pinned BepInEx archive did not contain BepInEx.dll.' }
    return $rootPath
}

$resolvedGame = (& (Join-Path $PSScriptRoot 'Find-NuclearOption.ps1') -GameDir $GameDir | Select-Object -Last 1).Trim()
if (-not $resolvedGame) { throw 'Nuclear Option was not found.' }
$resolvedGame = [IO.Path]::GetFullPath($resolvedGame)
$script:dotnet = Get-DotNet
$bepRoot = Get-BepInEx
Write-Host "Game: $resolvedGame"
Write-Host ".NET: $(& $script:dotnet --version)"

foreach ($path in $dist, $artifacts) {
    if (Test-Path $path) { Remove-Item $path -Recurse -Force }
}
New-Item -ItemType Directory -Force -Path $dist, $artifacts | Out-Null

Invoke-DotNet @('restore', $testsProject, '--nologo')
Invoke-DotNet @('restore', $pluginProject, '--nologo')
Invoke-DotNet @('run', '--project', $testsProject, '--configuration', 'Release', '--no-restore')
& (Join-Path $root 'tools\Test-NetworkValidation.ps1') -SelfTest
if ($LASTEXITCODE -ne 0) { throw 'Network validation analyzer self-test failed.' }

$pluginOutput = Join-Path $artifacts 'plugin'
New-Item -ItemType Directory -Force -Path $pluginOutput | Out-Null
Invoke-DotNet @('build', $pluginProject, '--configuration', 'Release', '--no-restore', '--nologo', '--output', $pluginOutput,
    "/p:GameDir=$resolvedGame", "/p:BepInExRoot=$bepRoot")
$pluginDll = Join-Path $pluginOutput 'NuclearOptionDetents.dll'
if (-not (Test-Path $pluginDll -PathType Leaf)) { throw 'Release build did not produce NuclearOptionDetents.dll.' }

$bepLicense = Join-Path $cache "BepInEx-LICENSE-v$bepVersion.txt"
$lgpl = Join-Path $cache 'LGPL-2.1.txt'
[void](Get-VerifiedDownload 'https://raw.githubusercontent.com/BepInEx/BepInEx/v5.4.23.5/LICENSE' $bepLicense $bepLicenseSha256)
[void](Get-VerifiedDownload 'https://www.gnu.org/licenses/old-licenses/lgpl-2.1.txt' $lgpl $lgplSha256)
& (Join-Path $PSScriptRoot 'Package.ps1') -RootDir $root -PluginDll $pluginDll -BepInExRoot $bepRoot `
    -BepInExMitLicense $bepLicense -LgplLicense $lgpl -Version $version
if ($LASTEXITCODE -ne 0) { throw 'Packaging failed.' }

Write-Host 'Build complete.'
Get-ChildItem $dist -Filter '*.zip' | ForEach-Object {
    Write-Host "$($_.Name)  $((Get-FileHash $_.FullName -Algorithm SHA256).Hash)"
}
