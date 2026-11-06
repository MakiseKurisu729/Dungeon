# 生成 QA-REFACTOR-01 阶段 A 的 SPEC01 断言三重集比对（只读）
# 用法：在仓库根目录执行  powershell -File Docs/QA/Evidence/QA-REFACTOR-01/SPEC01/compare.ps1
$ErrorActionPreference = 'Stop'
$root = (Get-Location).Path
$before = Join-Path $root 'Docs\QA\Evidence\QA-REFACTOR-01\SPEC01\before\SPEC01_Assertions.jsonl'
$after  = Join-Path $root 'Docs\QA\Evidence\QA-REFACTOR-01\SPEC01\after\SPEC01_Assertions.jsonl'
$out    = Join-Path $root 'Docs\QA\Evidence\QA-REFACTOR-01\SPEC01\diff_AssertionIds.txt'

function Get-Triples([string]$path) {
    $rows = @()
    foreach ($line in [System.IO.File]::ReadAllLines($path, [System.Text.Encoding]::UTF8)) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $qa = [regex]::Match($line, '"qaItem":"((?:[^"\\]|\\.)*)"').Groups[1].Value
        $as = [regex]::Match($line, '"assertion":"((?:[^"\\]|\\.)*)"').Groups[1].Value
        $st = [regex]::Match($line, '"status":"((?:[^"\\]|\\.)*)"').Groups[1].Value
        $pa = [regex]::Match($line, '"pass":(true|false)').Groups[1].Value
        $rows += [pscustomobject]@{ Line = "$qa | $as | $st"; Pass = $pa }
    }
    return $rows
}

$b = Get-Triples $before
$a = Get-Triples $after
$bs = ($b | ForEach-Object { $_.Line } | Sort-Object)
$as = ($a | ForEach-Object { $_.Line } | Sort-Object)

$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine('SPEC01 assertion triple diff - (qaItem, assertion, status) sorted')
[void]$sb.AppendLine("BEFORE: $before")
[void]$sb.AppendLine("AFTER : $after")
[void]$sb.AppendLine('')
[void]$sb.AppendLine("BEFORE count=$($b.Count) pass=$(($b | Where-Object Pass -eq 'true').Count) fail=$(($b | Where-Object Pass -eq 'false').Count)")
[void]$sb.AppendLine("AFTER  count=$($a.Count) pass=$(($a | Where-Object Pass -eq 'true').Count) fail=$(($a | Where-Object Pass -eq 'false').Count)")
[void]$sb.AppendLine('')
[void]$sb.AppendLine('=== BEFORE ===')
foreach ($l in $bs) { [void]$sb.AppendLine($l) }
[void]$sb.AppendLine('')
[void]$sb.AppendLine('=== AFTER ===')
foreach ($l in $as) { [void]$sb.AppendLine($l) }
[void]$sb.AppendLine('')
[void]$sb.AppendLine('=== DIFF ===')
$diff = Compare-Object -ReferenceObject $bs -DifferenceObject $as
if ($null -eq $diff) {
    [void]$sb.AppendLine('(empty)')
    [void]$sb.AppendLine('RESULT: IDENTICAL')
} else {
    foreach ($d in $diff) { [void]$sb.AppendLine("$($d.SideIndicator) $($d.InputObject)") }
    [void]$sb.AppendLine("RESULT: DIFFERENT ($($diff.Count) rows)")
}

[System.IO.File]::WriteAllText($out, $sb.ToString(), (New-Object System.Text.UTF8Encoding($false)))
Write-Output "written: $out"
Write-Output "before=$($b.Count) after=$($a.Count) diffRows=$(if ($null -eq $diff) { 0 } else { $diff.Count })"
