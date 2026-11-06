# QA-REFACTOR-01 phase C: FND00B06 evidence shape diff (names / summary key order / line field order / BOM / exit code)
$ErrorActionPreference = 'Stop'
$root = (Get-Location).Path
$bDir = Join-Path $root 'Docs\QA\Evidence\QA-REFACTOR-01\FND00B06\before'
$aDir = Join-Path $root 'Docs\QA\Evidence\QA-REFACTOR-01\FND00B06\after'
$out  = Join-Path $root 'Docs\QA\Evidence\QA-REFACTOR-01\FND00B06\diff_EvidenceShape.txt'

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
[void]$sb.AppendLine('FND00B06 evidence shape diff (before vs after migration)')
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

# 2. summary key order (FND00B06: run first, qaItems right after task)
$bKeys = Get-SummaryKeys (Join-Path $bDir '06_Summary_run1.json')
$aKeys = Get-SummaryKeys (Join-Path $aDir '06_Summary_run1.json')
[void]$sb.AppendLine('=== SUMMARY KEY ORDER ===')
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

# 3. assertion line field order (FND00B06: run, qaItem, assertion, expected, actual, pass, status, kind, timestamp, data)
$bLineKeys = Get-Keys (Join-Path $bDir '06_Assertions.jsonl')
$aLineKeys = Get-Keys (Join-Path $aDir '06_Assertions.jsonl')
[void]$sb.AppendLine('=== ASSERTION LINE FIELD ORDER (first line) ===')
[void]$sb.AppendLine("BEFORE ($($bLineKeys.Count)): " + ($bLineKeys -join ', '))
[void]$sb.AppendLine("AFTER  ($($aLineKeys.Count)): " + ($aLineKeys -join ', '))
if (($bLineKeys -join '|') -eq ($aLineKeys -join '|')) { [void]$sb.AppendLine('ASSERTION LINE DIFF: (empty) IDENTICAL') }
else { [void]$sb.AppendLine('ASSERTION LINE DIFF: DIFFERENT') }
[void]$sb.AppendLine('')

# 4. runs index must NOT exist on either side (FND00B06 has no Runs index)
[void]$sb.AppendLine('=== RUNS INDEX ===')
$bRuns = @($bNames | Where-Object { $_ -like '*Runs*' })
$aRuns = @($aNames | Where-Object { $_ -like '*Runs*' })
[void]$sb.AppendLine("BEFORE runs files=$($bRuns.Count) AFTER runs files=$($aRuns.Count)")
if ($bRuns.Count -eq 0 -and $aRuns.Count -eq 0) { [void]$sb.AppendLine('RUNS INDEX DIFF: (empty) IDENTICAL (both sides generate none)') }
else { [void]$sb.AppendLine('RUNS INDEX DIFF: DIFFERENT') }
[void]$sb.AppendLine('')

# 5. encoding / line endings
[void]$sb.AppendLine('=== ENCODING ===')
$bomBad = 0
foreach ($n in $bNames) {
    $bp = Join-Path $bDir $n
    $ap = Join-Path $aDir $n
    $line = "{0}: beforeBOM={1} afterBOM={2} beforeCRLF={3} afterCRLF={4}" -f $n, (Has-Bom $bp), (Has-Bom $ap), (Has-CrLf $bp), (Has-CrLf $ap)
    if ((Has-Bom $bp) -or (Has-Bom $ap)) { $bomBad++ }
    [void]$sb.AppendLine($line)
}
if ($bomBad -eq 0) { [void]$sb.AppendLine('BOM DIFF: (empty) IDENTICAL (no BOM on either side)') }
else { [void]$sb.AppendLine("BOM DIFF: $bomBad file(s) carry a BOM") }
[void]$sb.AppendLine('')

# 6. ExitCode content (06_ExitCode_run1.txt must still exist and be "0")
[void]$sb.AppendLine('=== EXITCODE CONTENT ===')
$bExitPath = Join-Path $bDir '06_ExitCode_run1.txt'
$aExitPath = Join-Path $aDir '06_ExitCode_run1.txt'
[void]$sb.AppendLine("before exists=$(Test-Path $bExitPath) after exists=$(Test-Path $aExitPath)")
$bExit = [System.IO.File]::ReadAllText($bExitPath).Trim()
$aExit = [System.IO.File]::ReadAllText($aExitPath).Trim()
$exitVerdict = 'DIFFERENT'
if ($bExit -eq $aExit) { $exitVerdict = 'IDENTICAL' }
[void]$sb.AppendLine("before=[$bExit] after=[$aExit] $exitVerdict")

[System.IO.File]::WriteAllText($out, $sb.ToString(), (New-Object System.Text.UTF8Encoding($false)))
Write-Output "written: $out"
