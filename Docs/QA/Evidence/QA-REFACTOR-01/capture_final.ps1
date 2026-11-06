param([string]$Suite, [string]$RunLabel)
$ErrorActionPreference = 'Stop'
$roots = @{ 'SPEC01' = 'SPEC-01'; 'REFACTOR' = 'REFACTOR-M4'; 'FND00B06' = 'FND-00B-06'; 'DIAG' = 'DIAG-SKILL' }
$root = Join-Path "D:\unity\Dungeon\Docs\QA\Evidence" $roots[$Suite]
$latest = Get-ChildItem $root -Directory -Filter 'editor_run*' |
    Sort-Object { [int]($_.Name -replace 'editor_run', '') } | Select-Object -Last 1
if ($latest -eq $null) { Write-Output 'NO editor_run* DIR'; exit 1 }

$dest = Join-Path (Join-Path "D:\unity\Dungeon\Docs\QA\Evidence\QA-REFACTOR-01\final" $RunLabel) $Suite
New-Item -ItemType Directory -Force $dest | Out-Null
Copy-Item (Join-Path $latest.FullName '*') $dest -Force

$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine('# AC-2 snapshot: suite=' + $Suite + ' run=' + $RunLabel + ' source=' + $latest.Name)
[void]$sb.AppendLine('captured: ' + (Get-Date -Format 'yyyy-MM-dd HH:mm:ss.fff'))
[void]$sb.AppendLine('files   : ' + ((Get-ChildItem $dest | Select-Object -ExpandProperty Name) -join ', '))
[void]$sb.AppendLine('')

$jsonl = Get-ChildItem $dest -Filter '*_Assertions.jsonl' | Select-Object -First 1
if ($jsonl -ne $null) {
    $lines = Get-Content $jsonl.FullName
    $st = @{}
    foreach ($l in $lines) {
        $m = [regex]::Match($l, '"status":"([^"]+)"')
        if ($m.Success) { $k = $m.Groups[1].Value; if (-not $st.ContainsKey($k)) { $st[$k] = 0 }; $st[$k]++ }
    }
    [void]$sb.AppendLine('assertionFile: ' + $jsonl.Name)
    [void]$sb.AppendLine('lineCount    : ' + $lines.Count)
    foreach ($k in ($st.Keys | Sort-Object)) { [void]$sb.AppendLine('status ' + $k + ' : ' + $st[$k]) }
    $ids = @()
    foreach ($l in $lines) { $m = [regex]::Match($l, '"qaItem":"([^"]+)"'); if ($m.Success) { $ids += $m.Groups[1].Value } }
    [void]$sb.AppendLine('qaItems      : ' + (($ids | Sort-Object) -join ', '))
    [void]$sb.AppendLine('')
}
$sum = Get-ChildItem $dest -Filter '*Summary*.json' | Select-Object -First 1
if ($sum -ne $null) {
    $o = Get-Content $sum.FullName -Raw -Encoding UTF8 | ConvertFrom-Json
    [void]$sb.AppendLine('summary      : ' + $sum.Name)
    foreach ($p in $o.PSObject.Properties) {
        [void]$sb.AppendLine('  ' + $p.Name + '=' + ($p.Value | Out-String).Trim())
    }
    [void]$sb.AppendLine('')
}
$res = Join-Path $dest 'InEditorResult.txt'
if (Test-Path $res) {
    [void]$sb.AppendLine('InEditorResult.txt:')
    Get-Content $res | ForEach-Object { [void]$sb.AppendLine('  ' + $_) }
}
[System.IO.File]::WriteAllText((Join-Path $dest 'counts.txt'), $sb.ToString(), (New-Object System.Text.UTF8Encoding($false)))
Write-Output ('source: ' + $latest.Name)
Write-Output ('dest  : ' + $dest)
Write-Output (Get-Content (Join-Path $dest 'counts.txt') -Raw)
