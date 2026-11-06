# QA-REFACTOR-01 phase C: FND00B06 `expected` literal comparison (per assertion, char by char)
# `expected` 是规格描述，跨轮不应变化；actual/timestamp/run/data 不参与比对。
$ErrorActionPreference = 'Stop'
$root = (Get-Location).Path
$before = Join-Path $root 'Docs\QA\Evidence\QA-REFACTOR-01\FND00B06\before\06_Assertions.jsonl'
$after  = Join-Path $root 'Docs\QA\Evidence\QA-REFACTOR-01\FND00B06\after\06_Assertions.jsonl'
$out    = Join-Path $root 'Docs\QA\Evidence\QA-REFACTOR-01\FND00B06\diff_Expected.txt'

function Get-Expected([string]$path) {
    $map = [ordered]@{}
    foreach ($line in [System.IO.File]::ReadAllLines($path, [System.Text.Encoding]::UTF8)) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $qa = [regex]::Match($line, '"qaItem":"((?:[^"\\]|\\.)*)"').Groups[1].Value
        $as = [regex]::Match($line, '"assertion":"((?:[^"\\]|\\.)*)"').Groups[1].Value
        $m = [regex]::Match($line, '"expected":(.*?),"actual":')
        $map["$qa | $as"] = $m.Groups[1].Value
    }
    return $map
}

$b = Get-Expected $before
$a = Get-Expected $after

$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine('FND00B06 `expected` literal comparison (before vs after migration)')
[void]$sb.AppendLine("BEFORE: $before")
[void]$sb.AppendLine("AFTER : $after")
[void]$sb.AppendLine('')
[void]$sb.AppendLine("BEFORE assertions=$($b.Count)  AFTER assertions=$($a.Count)")
[void]$sb.AppendLine('')
[void]$sb.AppendLine('=== PER ASSERTION ===')

$diffs = 0
$onlyB = @($b.Keys | Where-Object { -not $a.Contains($_) })
$onlyA = @($a.Keys | Where-Object { -not $b.Contains($_) })
foreach ($k in $onlyB) { $diffs++; [void]$sb.AppendLine("MISSING-IN-AFTER: $k") }
foreach ($k in $onlyA) { $diffs++; [void]$sb.AppendLine("MISSING-IN-BEFORE: $k") }

foreach ($k in $b.Keys) {
    if (-not $a.Contains($k)) { continue }
    $bv = $b[$k]
    $av = $a[$k]
    if ([string]::Equals($bv, $av, [System.StringComparison]::Ordinal)) {
        [void]$sb.AppendLine("IDENTICAL  $k  len=$($bv.Length)")
    } else {
        $diffs++
        [void]$sb.AppendLine("DIFFERENT  $k")
        [void]$sb.AppendLine("  before: $bv")
        [void]$sb.AppendLine("  after : $av")
    }
}

[void]$sb.AppendLine('')
[void]$sb.AppendLine('=== RESULT ===')
if ($diffs -eq 0 -and $b.Count -eq $a.Count -and $b.Count -gt 0) {
    [void]$sb.AppendLine("IDENTICAL ($($b.Count) assertions, expected literals char-for-char equal)")
} else {
    [void]$sb.AppendLine("DIFFERENT ($diffs differences)")
}

[System.IO.File]::WriteAllText($out, $sb.ToString(), (New-Object System.Text.UTF8Encoding($false)))
Write-Output "written: $out"
Write-Output "assertions=$($b.Count) diffs=$diffs"
