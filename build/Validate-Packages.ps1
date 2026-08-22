[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$NommZip,
    [Parameter(Mandatory)][string]$PluginOnlyZip,
    [Parameter(Mandatory)][string]$StandaloneZip
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem

function Read-Entry([System.IO.Compression.ZipArchive]$Archive, [string]$Name) {
    $entry = $Archive.GetEntry($Name)
    if (-not $entry) { throw "Package is missing '$Name'." }
    $reader = [IO.StreamReader]::new($entry.Open(), [Text.Encoding]::UTF8, $true)
    try { return $reader.ReadToEnd() } finally { $reader.Dispose() }
}

function Assert-Package([string]$Path, [ValidateSet('Nomm','PluginOnly','Standalone')][string]$Kind) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { throw "Package does not exist: $Path" }
    $archive = [IO.Compression.ZipFile]::OpenRead([IO.Path]::GetFullPath($Path))
    try {
        $names = @($archive.Entries | Where-Object Name | ForEach-Object FullName)
        if (-not $names) { throw "$Kind package is empty." }
        $pluginRoot = if ($Kind -eq 'Nomm') { '' } else { 'BepInEx/plugins/NuclearOptionDetents/' }
        if ($names -notcontains "${pluginRoot}NuclearOptionDetents.dll") {
            throw "$Kind package is missing the plugin DLL."
        }
        foreach ($required in "${pluginRoot}README.txt", "${pluginRoot}LICENSE.txt") {
            if ($names -notcontains $required) { throw "$Kind package is missing '$required'." }
        }
        $versionedEntries = @("${pluginRoot}README.txt")
        if ($Kind -eq 'Standalone') { $versionedEntries += 'README-FIRST.txt' }
        foreach ($versionedEntry in $versionedEntries) {
            if ((Read-Entry $archive $versionedEntry) -match '@VERSION@') {
                throw "$Kind package still contains an @VERSION@ token in '$versionedEntry'."
            }
        }
        if ($Kind -eq 'Nomm') {
            if ($names.Count -ne 3 -or @($names | Where-Object { $_ -like '*/*' }).Count) {
                throw 'NOMM package must contain only the flat DLL, README, and license.'
            }
        }
        elseif ($Kind -eq 'PluginOnly') {
            if ($names -contains 'BepInEx/config/com.baanish.nuclearoption.detents.cfg' -or
                @($names | Where-Object { $_ -like 'BepInEx/core/*' -or $_ -in 'winhttp.dll','doorstop_config.ini','.doorstop_version' }).Count) {
                throw 'Plugin-only package contains standalone files.'
            }
        }
        else {
            foreach ($required in '.doorstop_version','doorstop_config.ini','winhttp.dll','README-FIRST.txt',
                                  'BepInEx/core/BepInEx.dll','BepInEx/config/BepInEx.cfg',
                                  'BepInEx/config/com.baanish.nuclearoption.detents.cfg',
                                  'licenses/BepInEx-LGPL-2.1.txt','licenses/BepInEx-MIT.txt','THIRD_PARTY_NOTICES.md') {
                if ($names -notcontains $required) { throw "Standalone package is missing '$required'." }
            }
            $config = Read-Entry $archive 'BepInEx/config/com.baanish.nuclearoption.detents.cfg'
            if ($config -notmatch '(?m)^\s*DebugLogging\s*=\s*false\s*(?:;.*)?$') {
                throw 'Standalone detents config must keep DebugLogging=false.'
            }
            $bepConfig = Read-Entry $archive 'BepInEx/config/BepInEx.cfg'
            if ($bepConfig -notmatch '(?m)^\s*HideManagerGameObject\s*=\s*true\s*(?:;.*)?$') {
                throw 'Standalone BepInEx.cfg must keep HideManagerGameObject=true.'
            }
        }
        if (@($names | Where-Object { $_ -match '(^|/)Assembly-CSharp.*\.dll$|(^|/)UnityEngine.*\.dll$|(^|/)Unity\.TextMeshPro\.dll$|(^|/)Rewired_Core\.dll$' })) {
            throw "$Kind package contains game assemblies."
        }
        [pscustomobject]@{ Kind = $Kind; Files = $names.Count; Bytes = (Get-Item -LiteralPath $Path).Length; Result = 'PASS' }
    }
    finally { $archive.Dispose() }
}

Assert-Package $PluginOnlyZip PluginOnly
Assert-Package $StandaloneZip Standalone
Assert-Package $NommZip Nomm
