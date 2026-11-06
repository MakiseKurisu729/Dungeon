# QA-REFACTOR-01 phase D: DIAG expected-field diff (literal spec must not drift, compared char by char)
# NOTE: DIAG reuses qaItem="DIAG-SKILL" for the first 4 rows, so rows are keyed by (qaItem, assertion).
$ErrorActionPreference = 'Stop'
$root = (Get-Location).Path
$before = Join-Path $root 'Docs\QA\Evidence\QA-REFACTOR-01\DIAG\before\DIAG_Assertions.jsonl'
$after  = Join-Path $root 'Docs\QA\Evidence\QA-REFACTOR-01\DIAG\after\DIAG_Assertions.jsonl'
$out    = Join-Path $root 'Docs\QA\Evidence\QA-REFACTOR-01\DIAG\diff_Expected.txt'

function Extract-JsonValue([string]$line, [string]$key) {
    $i = $line.IndexOf($key)
    if ($i -lt 0) { return '<missing>' }
    $start = $i + $key.Length
    $c = $line[$start]
    if ($c -eq '"') {
        $j = $start + 1
        while ($j -lt $line.Length) {
            if ($line[$j] -eq '\') { $j += 2; continue }
            if ($line[$j] -eq '"') { break }
            $j++
        }
        return $line.Substring($start, $j - $start + 1)
    }
    if ($c -eq '{' -or $c -eq '[') {
        $open = $c
        $close = if ($c -eq '{') { '}' } else { ']' }
        $depth = 0
        $inStr = $false
        $j = $start
        while ($j -lt $line.Length) {
            $ch = $line[$j]
            if ($inStr) {
                if ($ch -eq '\') { $j += 2; continue }
                if ($ch -eq '"') { $inStr = $false }
            } else {
                if ($ch -eq '"') { $inStr = $true }
                elseif ($ch -eq $open) { $depth++ }
                elseif ($ch -eq $close) { $depth--; if ($depth -eq 0) { break } }
            }
            $j++
        }
        return $line.Substring($start, $j - $start + 1)
    }
    $j = $start
    while ($j -lt $line.Length -and $line[$j] -ne ',') { $j++ }
    return $line.Substring($start, $j - $start)
}

function Get-Expected([string]$path) {
    $rows = [ordered]@{}
    foreach ($line in [System.IO.File]::ReadAllLines($path, [System.Text.Encoding]::UTF8)) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $qa = [regex]::Match($line, '"qaItem":"((?:[^"\\]|\\.)*)"').Groups[1].Value
        $as = [regex]::Match($line, '"assertion":"((?:[^"\\]|\\.)*)"').Groups[1].Value
        $rows["$qa :: $as"] = Extract-JsonValue $line '"expected":'
    }
    return $rows
}

$b = Get-Expected $before
$a = Get-Expected $after
$keys = ($b.Keys + $a.Keys | Sort-Object -Unique)

$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine('# DIAG expected-field diff (literal spec must not drift)  [QA-REFACTOR-01 AC-1]')
[void]$sb.AppendLine("BEFORE: $before")
[void]$sb.AppendLine("AFTER : $after")
[void]$sb.AppendLine("BEFORE count=$($b.Count)  AFTER count=$($a.Count)")
[void]$sb.AppendLine('')
[void]$sb.AppendLine('=== BEFORE ===')
foreach ($k in $keys) { [void]$sb.AppendLine("$k :: $($b[$k])") }
[void]$sb.AppendLine('')
[void]$sb.AppendLine('=== AFTER ===')
foreach ($k in $keys) { [void]$sb.AppendLine("$k :: $($a[$k])") }
[void]$sb.AppendLine('')

$diffs = 0
[void]$sb.AppendLine('=== DIFF (char-by-char) ===')
foreach ($k in $keys) {
    $x = [string]$b[$k]
    $y = [string]$a[$k]
    if ($x -ceq $y) { continue }
    $diffs++
    [void]$sb.AppendLine("MISMATCH $k")
    [void]$sb.AppendLine("  before($($x.Length)): $x")
    [void]$sb.AppendLine("  after ($($y.Length)): $y")
    $limit = [Math]::Min($x.Length, $y.Length)
    for ($i = 0; $i -lt $limit; $i++) {
        if ($x[$i] -cne $y[$i]) {
            [void]$sb.AppendLine("  first difference at index $i : before=[$($x[$i])] after=[$($y[$i])]")
            break
        }
    }
}
if ($diffs -eq 0) {
    [void]$sb.AppendLine('(empty)')
    [void]$sb.AppendLine("RESULT: IDENTICAL ($($keys.Count) assertions, char-by-char)")
} else {
    [void]$sb.AppendLine("RESULT: DIFFERENT ($diffs of $($keys.Count))")
}

[System.IO.File]::WriteAllText($out, $sb.ToString(), (New-Object System.Text.UTF8Encoding($false)))
Write-Output "written: $out"
Write-Output "assertions=$($keys.Count) mismatches=$diffs"
