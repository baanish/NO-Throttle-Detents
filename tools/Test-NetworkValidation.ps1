[CmdletBinding()]
param(
    [string]$LogPath,
    [int]$Owner = -1,
    [ValidateSet('local', 'remote', 'any')]
    [string]$Scope = 'remote',
    [switch]$SelfTest
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Convert-NodRecord([string]$Line) {
    $marker = 'NOD-NET|'
    $start = $Line.IndexOf($marker, [StringComparison]::Ordinal)
    if ($start -lt 0) { return $null }
    $fields = @{}
    foreach ($part in $Line.Substring($start).Split('|')) {
        $separator = $part.IndexOf('=')
        if ($separator -gt 0) {
            $fields[$part.Substring(0, $separator)] = $part.Substring($separator + 1)
        }
    }
    if ($fields.v -ne '1' -or -not $fields.event) { return $null }
    [PSCustomObject]@{ Fields = $fields; Raw = $Line }
}

function Get-Field($Record, [string]$Name, [string]$Default = '') {
    if ($Record.Fields.ContainsKey($Name)) { return [string]$Record.Fields[$Name] }
    return $Default
}

function Get-Number($Record, [string]$Name) {
    $text = Get-Field $Record $Name 'na'
    $value = 0.0
    if ($text -eq 'na' -or -not [double]::TryParse(
        $text,
        [Globalization.NumberStyles]::Float,
        [Globalization.CultureInfo]::InvariantCulture,
        [ref]$value)) { return $null }
    return $value
}

function New-Check([string]$Name, [string]$Status, [string]$Detail) {
    [PSCustomObject]@{ Name = $Name; Status = $Status; Detail = $Detail }
}

function Test-ClosedAtIdle($Record, $Attach) {
    $airbrakes = [int](Get-Field $Attach 'airbrakes' '0')
    $splitSurfaces = [int](Get-Field $Attach 'splitSurfaces' '0')
    if ($airbrakes -gt 0) {
        $active = Get-Field $Record 'airbrakeActive' 'na'
        $open = Get-Number $Record 'airbrakeOpen'
        return $active -eq '0' -and $null -ne $open -and $open -le 0.001
    }
    if ($splitSurfaces -gt 0) {
        $split = Get-Number $Record 'split'
        return $null -ne $split -and $split -le 0.001
    }
    return $null
}

function Test-OpenedAfterIdle($Record, $Attach) {
    $airbrakes = [int](Get-Field $Attach 'airbrakes' '0')
    $splitSurfaces = [int](Get-Field $Attach 'splitSurfaces' '0')
    if ($airbrakes -gt 0) {
        $open = Get-Number $Record 'airbrakeOpen'
        return (Get-Field $Record 'airbrakeActive' '0') -eq '1' -or ($null -ne $open -and $open -gt 0.001)
    }
    if ($splitSurfaces -gt 0) {
        $split = Get-Number $Record 'split'
        return $null -ne $split -and $split -gt 0.001
    }
    return $null
}

function Test-IdleSequence($Segment) {
    $samples = @($Segment.Samples)
    $holdIndexes = @(
        for ($index = 0; $index -lt $samples.Count; $index++) {
            $throttle = Get-Number $samples[$index] 'throttle'
            if ((Get-Field $samples[$index] 'idleHeld') -eq '1' -or
                ($null -ne $throttle -and $throttle -gt 0.00002 -and $throttle -le 0.0003)) {
                $index
            }
        }
    )
    if ($holdIndexes.Count -lt 2) {
        return New-Check 'idle/airbrake' 'INCONCLUSIVE' 'fewer than two idle hold samples'
    }
    foreach ($index in $holdIndexes) {
        $closed = Test-ClosedAtIdle $samples[$index] $Segment.Attach
        if ($null -eq $closed) {
            return New-Check 'idle/airbrake' 'INCONCLUSIVE' 'aircraft has no observable airbrake path'
        }
        if (-not $closed) {
            return New-Check 'idle/airbrake' 'FAIL' 'airbrake opened during the idle hold'
        }
    }

    $crossingIndex = -1
    for ($index = $holdIndexes[-1] + 1; $index -lt $samples.Count; $index++) {
        $throttle = Get-Number $samples[$index] 'throttle'
        if ($null -ne $throttle -and $throttle -le 0.00002) {
            $crossingIndex = $index
            break
        }
    }
    if ($crossingIndex -lt 0) {
        return New-Check 'idle/airbrake' 'INCONCLUSIVE' 'hold was seen but no later zero-throttle crossing was recorded'
    }
    for ($index = $crossingIndex; $index -lt $samples.Count; $index++) {
        if (Test-OpenedAfterIdle $samples[$index] $Segment.Attach) {
            return New-Check 'idle/airbrake' 'PASS' 'closed during hold and opened after crossing'
        }
    }
    return New-Check 'idle/airbrake' 'FAIL' 'airbrake did not open after the recorded crossing'
}

function Test-AfterburnerSequence($Segment) {
    $samples = @($Segment.Samples)
    if (-not @($samples | Where-Object { $null -ne (Get-Number $_ 'ab') }).Count) {
        return New-Check 'MIL/afterburner' 'INCONCLUSIVE' 'aircraft has no observable afterburner'
    }
    $holdIndexes = @(
        for ($index = 0; $index -lt $samples.Count; $index++) {
            $throttle = Get-Number $samples[$index] 'throttle'
            if ((Get-Field $samples[$index] 'abHeld') -eq '1' -or
                ($null -ne $throttle -and $throttle -ge 0.8997 -and $throttle -lt 0.9)) {
                $index
            }
        }
    )
    if ($holdIndexes.Count -lt 2) {
        return New-Check 'MIL/afterburner' 'INCONCLUSIVE' 'fewer than two full-dry hold samples'
    }
    foreach ($index in $holdIndexes) {
        if ((Get-Number $samples[$index] 'ab') -gt 0.001) {
            return New-Check 'MIL/afterburner' 'FAIL' 'afterburner activated during the full-dry hold'
        }
    }

    $crossingIndex = -1
    for ($index = $holdIndexes[-1] + 1; $index -lt $samples.Count; $index++) {
        $throttle = Get-Number $samples[$index] 'throttle'
        if ($null -ne $throttle -and $throttle -ge 0.9001) {
            $crossingIndex = $index
            break
        }
    }
    if ($crossingIndex -lt 0) {
        return New-Check 'MIL/afterburner' 'INCONCLUSIVE' 'hold was seen but no later afterburner crossing was recorded'
    }
    for ($index = $crossingIndex; $index -lt $samples.Count; $index++) {
        $amount = Get-Number $samples[$index] 'ab'
        if ($null -ne $amount -and $amount -gt 0.001) {
            return New-Check 'MIL/afterburner' 'PASS' 'off during hold and active after crossing'
        }
    }
    return New-Check 'MIL/afterburner' 'FAIL' 'afterburner did not activate after the recorded crossing'
}

function Merge-SequenceChecks([object[]]$Checks, [string]$Name) {
    $failure = @($Checks | Where-Object Status -eq 'FAIL' | Select-Object -First 1)
    if ($failure.Count) { return $failure[0] }
    $pass = @($Checks | Where-Object Status -eq 'PASS' | Select-Object -First 1)
    if ($pass.Count) { return $pass[0] }
    $detail = @($Checks | ForEach-Object Detail | Sort-Object -Unique) -join '; '
    return New-Check $Name 'INCONCLUSIVE' $detail
}

function Invoke-NetworkAnalysis([string[]]$Lines, [int]$RequestedOwner, [string]$RequestedScope) {
    $nodLines = @($Lines | Where-Object { $_ -like '*NOD-NET|*' })
    $records = @($nodLines | ForEach-Object { Convert-NodRecord $_ } | Where-Object { $null -ne $_ })
    $samples = @($records | Where-Object { (Get-Field $_ 'event') -eq 'sample' })
    if ($RequestedScope -ne 'any') {
        $samples = @($samples | Where-Object { (Get-Field $_ 'scope') -eq $RequestedScope })
    }

    $owners = @($samples | ForEach-Object { [int](Get-Field $_ 'owner' '-1') } | Sort-Object -Unique)
    $availableRemoteOwners = @($records |
        Where-Object { (Get-Field $_ 'event') -eq 'owners' } |
        ForEach-Object { Get-Field $_ 'remote' 'none' } |
        Where-Object { $_ -ne 'none' } |
        ForEach-Object { $_.Split(',') } |
        ForEach-Object { [int]$_ } |
        Sort-Object -Unique)
    $targetOwner = $RequestedOwner
    if ($targetOwner -lt 0 -and $owners.Count -eq 1) { $targetOwner = $owners[0] }
    if ($targetOwner -lt 0 -or ($RequestedOwner -lt 0 -and $owners.Count -ne 1)) {
        if ($RequestedScope -eq 'remote' -and $owners.Count -eq 0 -and $availableRemoteOwners.Count) {
            return ,(New-Check 'target selection' 'INCONCLUSIVE' "set NetworkValidationOwner and recapture; available remote owners: $($availableRemoteOwners -join ', ')")
        }
        return ,(New-Check 'target selection' 'INCONCLUSIVE' "choose -Owner from: $($owners -join ', ')")
    }

    $targetRecords = @($records | Where-Object { (Get-Field $_ 'owner' '-1') -eq [string]$targetOwner })
    $segments = [Collections.Generic.List[object]]::new()
    $active = $null
    $identityError = ''
    foreach ($record in $targetRecords) {
        $event = Get-Field $record 'event'
        $scope = Get-Field $record 'scope'
        $aircraft = Get-Field $record 'aircraft'
        if ($event -eq 'attach') {
            if ($null -ne $active) { $identityError = 'attach occurred before the previous detach'; break }
            $active = [PSCustomObject]@{
                Attach = $record
                Scope = $scope
                Aircraft = $aircraft
                Samples = [Collections.Generic.List[object]]::new()
            }
        } elseif ($event -eq 'sample') {
            if ($null -eq $active) { $identityError = 'sample occurred outside an attach segment'; break }
            if ($active.Scope -ne $scope -or $active.Aircraft -ne $aircraft) {
                $identityError = 'aircraft or scope changed without a detach'
                break
            }
            $active.Samples.Add($record)
        } elseif ($event -eq 'detach') {
            if ($null -eq $active) { $identityError = 'detach occurred without an attach'; break }
            if ($active.Scope -ne $scope -or $active.Aircraft -ne $aircraft) {
                $identityError = 'detach did not match its attach segment'
                break
            }
            $segments.Add($active)
            $active = $null
        }
    }
    if ($null -ne $active) { $segments.Add($active) }
    $relevantSegments = @($segments | Where-Object {
        ($RequestedScope -eq 'any' -or $_.Scope -eq $RequestedScope) -and $_.Samples.Count -gt 0
    })
    $targetSamples = @($relevantSegments | ForEach-Object { @($_.Samples) })
    $checks = [Collections.Generic.List[object]]::new()
    if ($nodLines.Count -ne $records.Count) {
        $checks.Add((New-Check 'identity stability' 'FAIL' 'malformed or unsupported NOD-NET record'))
    } elseif ($identityError) {
        $checks.Add((New-Check 'identity stability' 'FAIL' $identityError))
    } elseif ($relevantSegments.Count -eq 0) {
        $checks.Add((New-Check 'identity stability' 'INCONCLUSIVE' "owner $targetOwner has no complete attach/sample evidence"))
    } else {
        $checks.Add((New-Check 'identity stability' 'PASS' "owner=$targetOwner has $($relevantSegments.Count) clean attach segment(s)"))
    }

    if ($targetSamples.Count -eq 0) {
        $checks.Add((New-Check 'throttle observation' 'INCONCLUSIVE' 'no target samples'))
    } elseif ($RequestedScope -eq 'remote' -and @($targetSamples | Where-Object { (Get-Field $_ 'scope') -ne 'remote' }).Count) {
        $checks.Add((New-Check 'throttle observation' 'FAIL' 'target was not consistently remote'))
    } elseif (@($targetSamples | Where-Object { $null -ne (Get-Number $_ 'throttle') }).Count -ne $targetSamples.Count) {
        $checks.Add((New-Check 'throttle observation' 'FAIL' 'target has unreadable throttle samples'))
    } else {
        $checks.Add((New-Check 'throttle observation' 'PASS' "$($targetSamples.Count) readable samples"))
    }

    if ($relevantSegments.Count -eq 0) {
        $checks.Add((New-Check 'idle/airbrake' 'INCONCLUSIVE' 'missing attach capabilities'))
        $checks.Add((New-Check 'MIL/afterburner' 'INCONCLUSIVE' 'missing attach capabilities'))
        return $checks
    }
    $checks.Add((Merge-SequenceChecks @($relevantSegments | ForEach-Object { Test-IdleSequence $_ }) 'idle/airbrake'))
    $checks.Add((Merge-SequenceChecks @($relevantSegments | ForEach-Object { Test-AfterburnerSequence $_ }) 'MIL/afterburner'))
    return $checks
}

function Invoke-SelfTest {
    $pass = @(
        'NOD-NET|v=1|event=owners|local=30|remote=3,4|selectedRemote=3',
        'NOD-NET|v=1|event=attach|scope=local|owner=30|aircraft=VTOLTrainer1|airbrakes=1|splitSurfaces=0|nozzles=3',
        'NOD-NET|v=1|event=sample|scope=local|owner=30|aircraft=VTOLTrainer1|throttle=0.500000|airbrakeActive=0|airbrakeOpen=0.000000|split=na|ab=0.000000|idleHeld=0|abHeld=0',
        'NOD-NET|v=1|event=attach|scope=remote|owner=3|aircraft=Multirole1|airbrakes=0|splitSurfaces=2|nozzles=2',
        'NOD-NET|v=1|event=sample|scope=remote|owner=3|aircraft=Multirole1|throttle=0.000000|airbrakeActive=na|airbrakeOpen=na|split=2.000000|ab=0.000000|idleHeld=na|abHeld=na',
        'NOD-NET|v=1|event=sample|scope=remote|owner=3|aircraft=Multirole1|throttle=0.000100|airbrakeActive=na|airbrakeOpen=na|split=0.000000|ab=0.000000|idleHeld=na|abHeld=na',
        'NOD-NET|v=1|event=sample|scope=remote|owner=3|aircraft=Multirole1|throttle=0.000100|airbrakeActive=na|airbrakeOpen=na|split=0.000000|ab=0.000000|idleHeld=na|abHeld=na',
        'NOD-NET|v=1|event=sample|scope=remote|owner=3|aircraft=Multirole1|throttle=0.000000|airbrakeActive=na|airbrakeOpen=na|split=2.000000|ab=0.000000|idleHeld=na|abHeld=na',
        'NOD-NET|v=1|event=sample|scope=remote|owner=3|aircraft=Multirole1|throttle=0.899900|airbrakeActive=na|airbrakeOpen=na|split=0.000000|ab=0.000000|idleHeld=na|abHeld=na',
        'NOD-NET|v=1|event=sample|scope=remote|owner=3|aircraft=Multirole1|throttle=0.899900|airbrakeActive=na|airbrakeOpen=na|split=0.000000|ab=0.000000|idleHeld=na|abHeld=na',
        'NOD-NET|v=1|event=sample|scope=remote|owner=3|aircraft=Multirole1|throttle=0.910000|airbrakeActive=na|airbrakeOpen=na|split=0.000000|ab=0.200000|idleHeld=na|abHeld=na',
        'NOD-NET|v=1|event=attach|scope=remote|owner=4|aircraft=trainer|airbrakes=1|splitSurfaces=0|nozzles=0',
        'NOD-NET|v=1|event=sample|scope=remote|owner=4|aircraft=trainer|throttle=0.500000|airbrakeActive=0|airbrakeOpen=0.000000|split=na|ab=na|idleHeld=na|abHeld=na')
    $checks = @(Invoke-NetworkAnalysis $pass 3 'remote')
    if (@($checks | Where-Object Status -ne 'PASS').Count) { throw 'PASS fixture did not pass.' }

    $fail = $pass.Clone()
    $fail[5] = $fail[5] -replace 'split=0.000000', 'split=2.000000'
    $checks = @(Invoke-NetworkAnalysis $fail 3 'remote')
    if (-not @($checks | Where-Object { $_.Name -eq 'idle/airbrake' -and $_.Status -eq 'FAIL' }).Count) {
        throw 'FAIL fixture was not rejected.'
    }

    $cleanChange = @($pass +
        'NOD-NET|v=1|event=detach|scope=remote|owner=3|aircraft=Multirole1' +
        'NOD-NET|v=1|event=attach|scope=remote|owner=3|aircraft=StrikeFighter|airbrakes=1|splitSurfaces=0|nozzles=1' +
        'NOD-NET|v=1|event=sample|scope=remote|owner=3|aircraft=StrikeFighter|throttle=0.500000|airbrakeActive=0|airbrakeOpen=0.000000|split=na|ab=0.000000|idleHeld=na|abHeld=na')
    $checks = @(Invoke-NetworkAnalysis $cleanChange 3 'remote')
    if (-not @($checks | Where-Object { $_.Name -eq 'identity stability' -and $_.Status -eq 'PASS' }).Count) {
        throw 'Clean aircraft change was rejected.'
    }

    $localOnly = @(
        'NOD-NET|v=1|event=owners|local=30|remote=3,4|selectedRemote=-1',
        'NOD-NET|v=1|event=attach|scope=local|owner=30|aircraft=VTOLTrainer1|airbrakes=1|splitSurfaces=0|nozzles=3',
        'NOD-NET|v=1|event=sample|scope=local|owner=30|aircraft=VTOLTrainer1|throttle=0.500000|airbrakeActive=0|airbrakeOpen=0.000000|split=na|ab=0.000000|idleHeld=0|abHeld=0')
    $checks = @(Invoke-NetworkAnalysis $localOnly -1 'remote')
    if (-not @($checks | Where-Object {
        $_.Name -eq 'target selection' -and
        $_.Status -eq 'INCONCLUSIVE' -and
        $_.Detail -eq 'set NetworkValidationOwner and recapture; available remote owners: 3, 4'
    }).Count) {
        throw 'Local-only fixture did not report the available remote owners.'
    }
    Write-Output 'Network validation analyzer self-test passed.'
}

if ($SelfTest) {
    Invoke-SelfTest
    exit 0
}
if ([string]::IsNullOrWhiteSpace($LogPath)) { throw '-LogPath is required unless -SelfTest is used.' }
$resolvedLog = (Resolve-Path -LiteralPath $LogPath -ErrorAction Stop).Path
$checks = @(Invoke-NetworkAnalysis (Get-Content -LiteralPath $resolvedLog) $Owner $Scope)
foreach ($check in $checks) {
    Write-Output "$($check.Status) $($check.Name) - $($check.Detail)"
}
if (@($checks | Where-Object Status -eq 'FAIL').Count) { exit 1 }
if (@($checks | Where-Object Status -eq 'INCONCLUSIVE').Count) { exit 2 }
Write-Output 'PASS overall'
exit 0
