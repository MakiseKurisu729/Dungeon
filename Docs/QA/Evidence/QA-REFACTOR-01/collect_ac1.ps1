$ErrorActionPreference = 'Stop'
$utf8 = New-Object System.Text.UTF8Encoding($false)
$root = (Get-Location).Path
$ev = Join-Path $root 'Docs\QA\Evidence\QA-REFACTOR-01'
$out = Join-Path $ev 'final\ac1_recheck.txt'

$suites = @(
    @{ Name = 'SPEC01'; Old = 'SPEC01'; OldFile = 'SPEC01_Assertions.jsonl'; NewFile = 'SPEC01_Assertions.jsonl' },
    @{ Name = 'REFACTOR'; Old = 'REFACTOR'; OldFile = 'REFACTOR_Assertions.jsonl'; NewFile = 'REFACTOR_Assertions.jsonl' },
    @{ Name = 'FND00B06'; Old = 'FND00B06'; OldFile = '06_Assertions.jsonl'; NewFile = '06_Assertions.jsonl' },
    @{ Name = 'DIAG'; Old = 'DIAG'; OldFile = 'DIAG_Assertions.jsonl'; NewFile = 'DIAG_Assertions.jsonl' }
)

function Tuples([string]$path) {
    $rows = @()
    foreach ($l in (Get-Content $path -Encoding UTF8)) {
        $qi = [regex]::Match($l, '"qaItem":"([^"]*)"').Groups[1].Value
        $an = [regex]::Match($l, '"assertion":"([^"]*)"').Groups[1].Value
        $st = [regex]::Match($l, '"status":"([^"]*)"').Groups[1].Value
        $pa = [regex]::Match($l, '"pass":(true|false)').Groups[1].Value
        $ex = ''
        $m = [regex]::Match($l, '"expected":(.*?),"actual":')
        if ($m.Success) { $ex = $m.Groups[1].Value }
        $rows += ($qi + ' | ' + $an + ' | ' + $st + ' | pass=' + $pa)
    }
    return $rows
}

function Expecteds([string]$path) {
    $rows = @()
    foreach ($l in (Get-Content $path -Encoding UTF8)) {
        $qi = [regex]::Match($l, '"qaItem":"([^"]*)"').Groups[1].Value
        $m = [regex]::Match($l, '"expected":(.*?),"actual":')
        $ex = ''
        if ($m.Success) { $ex = $m.Groups[1].Value }
        $rows += ($qi + ' => ' + $ex)
    }
    return $rows
}

$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine('# AC-1 phase-E recheck: phase-A/D "after" run vs phase-E final run1')
[void]$sb.AppendLine('# Compares (qaItem, assertion, status, pass) tuple multisets and the expected literal per row.')
[void]$sb.AppendLine('generated: ' + (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'))
[void]$sb.AppendLine('')

foreach ($s in $suites) {
    $oldPath = Join-Path $ev ((Join-Path $s.Old 'after') + '\' + $s.OldFile)
    $newPath = Join-Path $ev ('final\run1\' + $s.Name + '\' + $s.NewFile)
    [void]$sb.AppendLine('## ' + $s.Name)
    [void]$sb.AppendLine('old: ' + $oldPath.Replace($root + '\', ''))
    [void]$sb.AppendLine('new: ' + $newPath.Replace($root + '\', ''))
    if (-not (Test-Path $oldPath) -or -not (Test-Path $newPath)) { [void]$sb.AppendLine('MISSING FILE -> skipped'); [void]$sb.AppendLine(''); continue }
    $o = Tuples $oldPath
    $n = Tuples $newPath
    [void]$sb.AppendLine('rows old=' + $o.Count + ' new=' + $n.Count)
    $os = ($o | Sort-Object) -join "`n"
    $ns = ($n | Sort-Object) -join "`n"
    [void]$sb.AppendLine('tuple multiset: ' + $(if ($os -eq $ns) { 'IDENTICAL' } else { 'DIFF' }))
    if ($os -ne $ns) {
        [void]$sb.AppendLine('old sorted:')
        ($o | Sort-Object) | ForEach-Object { [void]$sb.AppendLine('  - ' + $_) }
        [void]$sb.AppendLine('new sorted:')
        ($n | Sort-Object) | ForEach-Object { [void]$sb.AppendLine('  + ' + $_) }
    }
    $oe = Expecteds $oldPath
    $ne = Expecteds $newPath
    $oeS = ($oe | Sort-Object) -join "`n"
    $neS = ($ne | Sort-Object) -join "`n"
    [void]$sb.AppendLine('expected literals: ' + $(if ($oeS -eq $neS) { 'IDENTICAL' } else { 'DIFF' }))
    if ($oeS -ne $neS) {
        (Compare-Object $oe $ne) | ForEach-Object { [void]$sb.AppendLine('  ' + $_.SideIndicator + ' ' + $_.InputObject) }
    }
    [void]$sb.AppendLine('')
}

[System.IO.File]::WriteAllText($out, $sb.ToString(), $utf8)
Write-Output ('wrote ' + $out)
Write-Output ((Get-Content $out -Encoding UTF8 | Select-String -Pattern 'tuple multiset|expected literals|rows old').Line -join ' | ')
