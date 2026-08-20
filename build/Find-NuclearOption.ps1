[CmdletBinding()]
param([string]$GameDir)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Add-Candidate([System.Collections.Generic.List[string]]$List, [string]$Path) {
    if ([string]::IsNullOrWhiteSpace($Path)) { return }
    try { $full = [IO.Path]::GetFullPath($Path.Trim().Trim('"')) } catch { return }
    if (-not $List.Contains($full)) { [void]$List.Add($full) }
}

function Test-Game([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Container)) { return $null }
    $exe = Join-Path $Path 'NuclearOption.exe'
    $assembly = Join-Path $Path 'NuclearOption_Data\Managed\Assembly-CSharp.dll'
    if ((Test-Path -LiteralPath $exe -PathType Leaf) -and
        (Test-Path -LiteralPath $assembly -PathType Leaf)) {
        return [IO.Path]::GetFullPath($Path)
    }
    return $null
}

function Resolve-ExplicitGame([string]$Override, [string]$Source) {
    if ([string]::IsNullOrWhiteSpace($Override)) { return $null }
    try { $full = [IO.Path]::GetFullPath($Override.Trim().Trim('"')) }
    catch { throw "Invalid $Source override '$Override'." }
    $valid = Test-Game $full
    if (-not $valid) {
        throw "$Source override '$full' is not a valid Nuclear Option install."
    }
    return $valid
}

$explicitGame = Resolve-ExplicitGame $GameDir '-GameDir'
if (-not $explicitGame) {
    $explicitGame = Resolve-ExplicitGame ([Environment]::GetEnvironmentVariable('NUCLEAR_OPTION_DIR')) 'NUCLEAR_OPTION_DIR'
}
if ($explicitGame) {
    Write-Output $explicitGame
    return
}

$candidates = [System.Collections.Generic.List[string]]::new()

$steamRoots = [System.Collections.Generic.List[string]]::new()
foreach ($name in 'SteamPath', 'STEAM_PATH', 'STEAM_DIR') {
    Add-Candidate $steamRoots ([Environment]::GetEnvironmentVariable($name))
}
foreach ($key in 'HKCU:\Software\Valve\Steam', 'HKLM:\SOFTWARE\Valve\Steam', 'HKLM:\SOFTWARE\WOW6432Node\Valve\Steam') {
    foreach ($name in 'SteamPath', 'InstallPath') {
        try { Add-Candidate $steamRoots ([string](Get-ItemProperty -LiteralPath $key -Name $name -ErrorAction Stop).$name) } catch { }
    }
}
foreach ($base in [Environment]::GetEnvironmentVariable('ProgramFiles(x86)'),
                 [Environment]::GetEnvironmentVariable('ProgramFiles'),
                 [Environment]::GetEnvironmentVariable('LOCALAPPDATA')) {
    if ($base) { Add-Candidate $steamRoots (Join-Path $base 'Steam') }
}

# Read only the library path lines we need. This intentionally avoids a full
# Valve KeyValues parser; an explicit -GameDir remains the reliable override.
foreach ($steam in $steamRoots) {
    Add-Candidate $candidates (Join-Path $steam 'steamapps\common\Nuclear Option')
    $vdf = Join-Path $steam 'steamapps\libraryfolders.vdf'
    if (Test-Path -LiteralPath $vdf -PathType Leaf) {
        foreach ($line in Get-Content -LiteralPath $vdf) {
            if ($line -match '"path"\s+"(?<path>(?:\\.|[^"\\])+)"') {
                $library = $Matches.path -replace '\\\\', '\'
                Add-Candidate $candidates (Join-Path $library 'steamapps\common\Nuclear Option')
            }
        }
    }
}

foreach ($candidate in $candidates) {
    $valid = Test-Game $candidate
    if ($valid) { Write-Output $valid; return }
}

throw "Unable to find Nuclear Option. Pass -GameDir or set NUCLEAR_OPTION_DIR. Checked: $($candidates -join ', ')"
