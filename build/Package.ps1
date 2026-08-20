[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$RootDir,
    [Parameter(Mandatory)][string]$PluginDll,
    [Parameter(Mandatory)][string]$BepInExRoot,
    [Parameter(Mandatory)][string]$BepInExMitLicense,
    [Parameter(Mandatory)][string]$LgplLicense,
    [Parameter(Mandatory)][ValidatePattern('^\d+\.\d+\.\d+$')][string]$Version
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem

$root = [IO.Path]::GetFullPath($RootDir)
$stage = Join-Path $root 'artifacts\package-staging'
$nommStage = Join-Path $stage 'nomm'
$pluginStage = Join-Path $stage 'plugin-only'
$standaloneStage = Join-Path $stage 'standalone'
$dist = Join-Path $root 'dist'
$nommZip = Join-Path $dist "NuclearOptionDetents-v$Version-nomm.zip"
$pluginZip = Join-Path $dist "NuclearOptionDetents-v$Version-plugin-only.zip"
$standaloneZip = Join-Path $dist "NuclearOptionDetents-v$Version-standalone-fresh-install-win-x64.zip"
$nommManifest = Join-Path $dist 'NuclearOptionDetents.nomnom.json'

function Copy-Versioned([string]$Source, [string]$Destination) {
    $text = [IO.File]::ReadAllText($Source)
    if ($text -notlike '*@VERSION@*') { throw "Version token missing from $Source" }
    [IO.File]::WriteAllText($Destination, $text.Replace('@VERSION@', $Version), [Text.UTF8Encoding]::new($false))
}

function Add-Plugin([string]$Destination, [bool]$Config) {
    $pluginDir = Join-Path $Destination 'BepInEx\plugins\NuclearOptionDetents'
    New-Item -ItemType Directory -Force -Path $pluginDir | Out-Null
    if ($Config) {
        $configDir = Join-Path $Destination 'BepInEx\config'
        New-Item -ItemType Directory -Force -Path $configDir | Out-Null
        Copy-Item (Join-Path $root 'packaging\com.aanish.nuclearoption.detents.cfg') $configDir
    }
    Copy-Item $PluginDll (Join-Path $pluginDir 'NuclearOptionDetents.dll')
    Copy-Versioned (Join-Path $root 'packaging\PLUGIN-README.txt') (Join-Path $pluginDir 'README.txt')
    Copy-Item (Join-Path $root 'LICENSE') (Join-Path $pluginDir 'LICENSE.txt')
}

function Add-NommPlugin([string]$Destination) {
    Copy-Item $PluginDll (Join-Path $Destination 'NuclearOptionDetents.dll')
    Copy-Versioned (Join-Path $root 'packaging\NOMM-README.txt') (Join-Path $Destination 'README.txt')
    Copy-Item (Join-Path $root 'LICENSE') (Join-Path $Destination 'LICENSE.txt')
}

function New-Zip([string]$Source, [string]$Destination) {
    if (Test-Path $Destination) { Remove-Item $Destination -Force }
    $archive = [IO.Compression.ZipFile]::Open($Destination, [IO.Compression.ZipArchiveMode]::Create)
    try {
        $base = [IO.Path]::GetFullPath($Source).TrimEnd('\','/') + '\'
        $stableTimestamp = [DateTimeOffset]::new(2000, 1, 1, 0, 0, 0, [TimeSpan]::Zero)
        foreach ($file in Get-ChildItem -LiteralPath $Source -Recurse -File | Sort-Object FullName) {
            $entryName = $file.FullName.Substring($base.Length).Replace('\','/')
            $entry = $archive.CreateEntry($entryName, [IO.Compression.CompressionLevel]::Optimal)
            $entry.LastWriteTime = $stableTimestamp
            $input = [IO.File]::OpenRead($file.FullName)
            try { $output = $entry.Open(); try { $input.CopyTo($output) } finally { $output.Dispose() } }
            finally { $input.Dispose() }
        }
    }
    finally { $archive.Dispose() }
}

if (-not (Test-Path $PluginDll -PathType Leaf)) { throw "Plugin DLL not found: $PluginDll" }
if (-not (Test-Path (Join-Path $BepInExRoot 'BepInEx\core\BepInEx.dll') -PathType Leaf)) { throw "BepInExRoot is incomplete: $BepInExRoot" }
foreach ($license in $BepInExMitLicense, $LgplLicense) { if (-not (Test-Path $license -PathType Leaf)) { throw "Missing license: $license" } }

if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
New-Item -ItemType Directory -Force -Path $nommStage, $pluginStage, $standaloneStage, $dist | Out-Null
Add-NommPlugin $nommStage
Add-Plugin $pluginStage $false
Get-ChildItem $BepInExRoot -Force | Copy-Item -Destination $standaloneStage -Recurse -Force
Add-Plugin $standaloneStage $true
$standaloneConfig = Join-Path $standaloneStage 'BepInEx\config'
Copy-Item (Join-Path $root 'packaging\BepInEx.cfg') $standaloneConfig -Force
Copy-Versioned (Join-Path $root 'packaging\README-FIRST.txt') (Join-Path $standaloneStage 'README-FIRST.txt')
Copy-Item (Join-Path $root 'THIRD_PARTY_NOTICES.md') $standaloneStage
$licenses = Join-Path $standaloneStage 'licenses'
New-Item -ItemType Directory -Force -Path $licenses | Out-Null
Copy-Item $BepInExMitLicense (Join-Path $licenses 'BepInEx-MIT.txt')
Copy-Item $LgplLicense (Join-Path $licenses 'BepInEx-LGPL-2.1.txt')
New-Zip $nommStage $nommZip
New-Zip $pluginStage $pluginZip
New-Zip $standaloneStage $standaloneZip
& (Join-Path $PSScriptRoot 'Validate-Packages.ps1') -NommZip $nommZip -PluginOnlyZip $pluginZip -StandaloneZip $standaloneZip

$manifestText = [IO.File]::ReadAllText((Join-Path $root 'packaging\NOMNOM-MANIFEST.template.json'))
$nommHash = (Get-FileHash $nommZip -Algorithm SHA256).Hash.ToLowerInvariant()
$manifestText = $manifestText.Replace('@VERSION@', $Version).Replace('@SHA256@', $nommHash)
if ($manifestText -match '@[A-Z0-9_]+@') { throw 'NOMNOM manifest still contains an unresolved token.' }
[IO.File]::WriteAllText($nommManifest, $manifestText, [Text.UTF8Encoding]::new($false))

$checksumPath = Join-Path $dist 'SHA256SUMS.txt'
$checksumLines = @(
    Get-ChildItem -LiteralPath $dist -Filter '*.zip' |
        Sort-Object Name |
        ForEach-Object { "$((Get-FileHash $_.FullName -Algorithm SHA256).Hash)  $($_.Name)" }
)
[IO.File]::WriteAllLines($checksumPath, [string[]]$checksumLines, [Text.Encoding]::ASCII)
Write-Output "Created $pluginZip"
Write-Output "Created $standaloneZip"
Write-Output "Created $nommZip"
Write-Output "Created $nommManifest"
Write-Output "Created $checksumPath"
