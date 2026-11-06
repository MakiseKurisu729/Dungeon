# QA-REFACTOR-01 phase D: DIAG evidence shape diff (names / summary key order / line field order / no Runs index / BOM)
$ErrorActionPreference = 'Stop'
$root = (Get-Location).Path
$bDir = Join-Path $root 'Docs\QA\Evidence\QA-REFACTOR-01\DIAG\before'
$aDir = Join-Path $root 'Docs\QA\Evidence\QA-REFACTOR-01\DIAG\after'
$out  = Join-Path $root 'Docs\QA\Evidence\QA-REFACTOR-01\DIAG\diff_EvidenceShape.txt'

function Get-Keys([string]$path) {
    $line = [System.IO.File]::ReadAllLines($path, [System.Text.Encoding]::UTF8) | Where-Object { $_.Trim().Length -gt 0 } | Select-Object -First 1
    $ms = [regex]::Matches($line, '"([A-Za-z0-9_]+)":')
    return ($ms | ForEach-Object { $_.Groups[1].Value })
}

function Get-SummaryKeys([string]$path) {
    $keys = @()
    foreach ($line in [System.IO.File]::ReadAllLines($path, [System.Text.Encoding]::UTF8)) {
        $m = [regex]::Match($line, '^\s{2}"([A-Za-z0-9_]+)":')
        if ($m.Success) { $keys += $m.Groups[1].Value }
    }
    return $keys
}

function Has-Bom([string]$path) {
    $bytes = [System.IO.File]::ReadAllBytes($path)
    if ($bytes.Length -lt 3) { return $false }
    return ($bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF)
}

function Has-CrLf([string]$path) {
    $text = [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)
    return $text.Contains("`r`n")
}

$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine('DIAG evidence shape diff (before vs after migration)')
[void]$sb.AppendLine("BEFORE dir: $bDir")
[void]$sb.AppendLine("AFTER  dir: $aDir")
[void]$sb.AppendLine('')

# 1. file name set
$bNames = (Get-ChildItem $bDir -File | Select-Object -ExpandProperty Name | Sort-Object)
$aNames = (Get-ChildItem $aDir -File | Select-Object -ExpandProperty Name | Sort-Object)
[void]$sb.AppendLine('=== FILE NAMES ===')
[void]$sb.AppendLine("BEFORE ($($bNames.Count)): " + ($bNames -join ', '))
[void]$sb.AppendLine("AFTER  ($($aNames.Count)): " + ($aNames -join ', '))
$nameDiff = Compare-Object -ReferenceObject $bNames -DifferenceObject $aNames
if ($null -eq $nameDiff) { [void]$sb.AppendLine('FILE NAME DIFF: (empty) IDENTICAL') }
else {
    foreach ($d in $nameDiff) { [void]$sb.AppendLine("FILE NAME DIFF: $($d.SideIndicator) $($d.InputObject)") }
}
[void]$sb.AppendLine('')

# 2. no Runs index (DIAG contract: RunIndexFileName = null)
[void]$sb.AppendLine('=== RUNS INDEX (must be absent) ===')
$bRuns = @(Get-ChildItem $bDir -File | Where-Object { $_.Name -like '*Runs*' })
$aRuns = @(Get-ChildItem $aDir -File | Where-Object { $_.Name -like '*Runs*' })
[void]$sb.AppendLine("BEFORE runs-index files=$($bRuns.Count)  AFTER runs-index files=$($aRuns.Count)")
if ($bRuns.Count -eq 0 -and $aRuns.Count -eq 0) { [void]$sb.AppendLine('RUNS INDEX: (absent both sides) IDENTICAL') }
else { [void]$sb.AppendLine('RUNS INDEX: PRESENT - DIFFERENT') }
[void]$sb.AppendLine('')

# 3. summary key order (fixed file name DIAG_Summary.json, no run suffix)
$bKeys = Get-SummaryKeys (Join-Path $bDir 'DIAG_Summary.json')
$aKeys = Get-SummaryKeys (Join-Path $aDir 'DIAG_Summary.json')
[void]$sb.AppendLine('=== SUMMARY KEY ORDER (DIAG_Summary.json) ===')
[void]$sb.AppendLine("BEFORE ($($bKeys.Count)): " + ($bKeys -join ', '))
[void]$sb.AppendLine("AFTER  ($($aKeys.Count)): " + ($aKeys -join ', '))
if (($bKeys -join '|') -eq ($aKeys -join '|')) { [void]$sb.AppendLine('SUMMARY KEY DIFF: (empty) IDENTICAL (order-sensitive)') }
else {
    for ($i = 0; $i -lt [Math]::Max($bKeys.Count, $aKeys.Count); $i++) {
        $bv = if ($i -lt $bKeys.Count) { $bKeys[$i] } else { '<none>' }
        $av = if ($i -lt $aKeys.Count) { $aKeys[$i] } else { '<none>' }
        $mark = if ($bv -eq $av) { ' ' } else { '!' }
        [void]$sb.AppendLine("$mark [$i] before=$bv after=$av")
    }
}
[void]$sb.AppendLine('')

# 4. assertion line field order
$bLineKeys = Get-Keys (Join-Path $bDir 'DIAG_Assertions.jsonl')
$aLineKeys = Get-Keys (Join-Path $aDir 'DIAG_Assertions.jsonl')
[void]$sb.AppendLine('=== ASSERTION LINE FIELD ORDER (first line) ===')
[void]$sb.AppendLine("BEFORE ($($bLineKeys.Count)): " + ($bLineKeys -join ', '))
[void]$sb.AppendLine("AFTER  ($($aLineKeys.Count)): " + ($aLineKeys -join ', '))
if (($bLineKeys -join '|') -eq ($aLineKeys -join '|')) { [void]$sb.AppendLine('ASSERTION LINE DIFF: (empty) IDENTICAL') }
else { [void]$sb.AppendLine('ASSERTION LINE DIFF: DIFFERENT') }
[void]$sb.AppendLine('')

# 5. encoding / line endings
[void]$sb.AppendLine('=== ENCODING ===')
foreach ($n in $bNames) {
    $bp = Join-Path $bDir $n
    $ap = Join-Path $aDir $n
    $line = "{0}: beforeBOM={1} afterBOM={2} beforeCRLF={3} afterCRLF={4}" -f $n, (Has-Bom $bp), (Has-Bom $ap), (Has-CrLf $bp), (Has-CrLf $ap)
    [void]$sb.AppendLine($line)
}
[void]$sb.AppendLine('')

# 6. ExitCode content
[void]$sb.AppendLine('=== EXITCODE CONTENT ===')
$bExit = [System.IO.File]::ReadAllText((Join-Path $bDir 'DIAG_ExitCode.txt')).Trim()
$aExit = [System.IO.File]::ReadAllText((Join-Path $aDir 'DIAG_ExitCode.txt')).Trim()
$exitVerdict = 'DIFFERENT'
if ($bExit -eq $aExit) { $exitVerdict = 'IDENTICAL' }
[void]$sb.AppendLine("before=$bExit after=$aExit $exitVerdict")
[void]$sb.AppendLine('')

# 7. summary values that are spec constants (task / mode / run / scene / conclusion / exitReason / counts)
[void]$sb.AppendLine('=== SUMMARY CONSTANT VALUES ===')
function Get-SummaryValue([string]$path, [string]$key) {
    $line = [System.IO.File]::ReadAllLines($path, [System.Text.Encoding]::UTF8) | Where-Object { $_ -match "^\s{2}\`"$key\`":" } | Select-Object -First 1
    if ($null -eq $line) { return '<missing>' }
    return ($line.Substring($line.IndexOf(':') + 1).Trim().TrimEnd(','))
}
foreach ($k in @('task', 'mode', 'run', 'unityVersion', 'scene', 'pass', 'fail', 'blocked', 'infraFail', 'observed', 'conclusion', 'exitReason')) {
    $bv = Get-SummaryValue (Join-Path $bDir 'DIAG_Summary.json') $k
    $av = Get-SummaryValue (Join-Path $aDir 'DIAG_Summary.json') $k
    $verdict = if ($bv -ceq $av) { 'IDENTICAL' } else { 'DIFFERENT' }
    [void]$sb.AppendLine("${k}: $verdict")
}

[System.IO.File]::WriteAllText($out, $sb.ToString(), (New-Object System.Text.UTF8Encoding($false)))
Write-Output "written: $out"
