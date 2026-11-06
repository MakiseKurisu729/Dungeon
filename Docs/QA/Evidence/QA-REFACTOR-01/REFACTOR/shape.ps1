# QA-REFACTOR-01 phase B: REFACTOR evidence shape diff (names / summary key order / line field order / BOM)
$ErrorActionPreference = 'Stop'
$root = (Get-Location).Path
$bDir = Join-Path $root 'Docs\QA\Evidence\QA-REFACTOR-01\REFACTOR\before'
$aDir = Join-Path $root 'Docs\QA\Evidence\QA-REFACTOR-01\REFACTOR\after'
$out  = Join-Path $root 'Docs\QA\Evidence\QA-REFACTOR-01\REFACTOR\diff_EvidenceShape.txt'

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
[void]$sb.AppendLine('REFACTOR evidence shape diff (before vs after migration)')
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

# 2. summary key order
$bKeys = Get-SummaryKeys (Join-Path $bDir 'REFACTOR_Summary_run1.json')
$aKeys = Get-SummaryKeys (Join-Path $aDir 'REFACTOR_Summary_run1.json')
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

# 3. assertion line field order (REFACTOR keeps the extra milestone after run)
$bLineKeys = Get-Keys (Join-Path $bDir 'REFACTOR_Assertions.jsonl')
$aLineKeys = Get-Keys (Join-Path $aDir 'REFACTOR_Assertions.jsonl')
[void]$sb.AppendLine('=== ASSERTION LINE FIELD ORDER (first line) ===')
[void]$sb.AppendLine("BEFORE ($($bLineKeys.Count)): " + ($bLineKeys -join ', '))
[void]$sb.AppendLine("AFTER  ($($aLineKeys.Count)): " + ($aLineKeys -join ', '))
if (($bLineKeys -join '|') -eq ($aLineKeys -join '|')) { [void]$sb.AppendLine('ASSERTION LINE DIFF: (empty) IDENTICAL (milestone preserved after run)') }
else { [void]$sb.AppendLine('ASSERTION LINE DIFF: DIFFERENT') }
[void]$sb.AppendLine('')

# 4. runs index key order
$bRunKeys = Get-Keys (Join-Path $bDir 'REFACTOR_Runs.jsonl')
$aRunKeys = Get-Keys (Join-Path $aDir 'REFACTOR_Runs.jsonl')
[void]$sb.AppendLine('=== RUNS INDEX FIELD ORDER (first line) ===')
[void]$sb.AppendLine("BEFORE ($($bRunKeys.Count)): " + ($bRunKeys -join ', '))
[void]$sb.AppendLine("AFTER  ($($aRunKeys.Count)): " + ($aRunKeys -join ', '))
if (($bRunKeys -join '|') -eq ($aRunKeys -join '|')) { [void]$sb.AppendLine('RUNS INDEX DIFF: (empty) IDENTICAL') }
else { [void]$sb.AppendLine('RUNS INDEX DIFF: DIFFERENT') }
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
$bExit = [System.IO.File]::ReadAllText((Join-Path $bDir 'REFACTOR_ExitCode_run1.txt')).Trim()
$aExit = [System.IO.File]::ReadAllText((Join-Path $aDir 'REFACTOR_ExitCode_run1.txt')).Trim()
$exitVerdict = 'DIFFERENT'
if ($bExit -eq $aExit) { $exitVerdict = 'IDENTICAL' }
[void]$sb.AppendLine("before=$bExit after=$aExit $exitVerdict")

[System.IO.File]::WriteAllText($out, $sb.ToString(), (New-Object System.Text.UTF8Encoding($false)))
Write-Output "written: $out"
