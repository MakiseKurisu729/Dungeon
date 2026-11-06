# QA-REFACTOR-01 phase E - AC-6 dedup counts (scope: Tests/QA + four guard suites)
# Scope excludes Tests/FND00B/FND00B04*, FND00B05*, Tests/FND02A/** (historical suites, out of scope).
$ErrorActionPreference = 'Stop'
$root = (Get-Location).Path
$tests = Join-Path $root 'Assets\_Project\Tests'
$out = Join-Path $root 'Docs\QA\Evidence\QA-REFACTOR-01\dedup_counts.txt'
$sb = New-Object System.Text.StringBuilder

[void]$sb.AppendLine('# QA-REFACTOR-01 phase E - AC-6 dedup counts (grep command + actual + expected)')
[void]$sb.AppendLine('generated: ' + (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'))
[void]$sb.AppendLine('scope   : Assets/_Project/Tests/QA/** + SPEC01/** + REFACTOR/** + DIAG/** + FND00B/FND00B06*')
[void]$sb.AppendLine('excluded: Tests/FND00B/FND00B04* , FND00B05* , Tests/FND02A/** (out of scope per TASK AC-6)')
[void]$sb.AppendLine('')

$files = @()
foreach ($d in @('QA', 'SPEC01', 'REFACTOR', 'DIAG')) {
    $files += Get-ChildItem (Join-Path $tests $d) -Recurse -Filter *.cs | ForEach-Object { $_.FullName }
}
$files += Get-ChildItem (Join-Path $tests 'FND00B') -Filter 'FND00B06*.cs' -Recurse | ForEach-Object { $_.FullName }
$files = $files | Sort-Object -Unique
[void]$sb.AppendLine('files in scope: ' + $files.Count)
foreach ($f in $files) { [void]$sb.AppendLine('  ' + ($f.Replace($root + '\', '') -replace '\\', '/')) }
[void]$sb.AppendLine('')

$script:allOk = $true
function Add-Check([string]$name, [string]$command, $actual, $expected) {
    [void]$sb.AppendLine('## ' + $name)
    [void]$sb.AppendLine('command : ' + $command)
    [void]$sb.AppendLine('actual  : ' + $actual)
    [void]$sb.AppendLine('expected: ' + $expected)
    $ok = ("$actual" -eq "$expected")
    if ($ok) { [void]$sb.AppendLine('result  : PASS') } else { [void]$sb.AppendLine('result  : FAIL'); $script:allOk = $false }
    [void]$sb.AppendLine('')
}

function Total([string]$pattern) {
    $n = 0
    $hits = @()
    foreach ($f in $files) {
        $t = [System.IO.File]::ReadAllText($f, [System.Text.Encoding]::UTF8)
        $m = [regex]::Matches($t, $pattern)
        if ($m.Count -gt 0) {
            $n += $m.Count
            $hits += (($f.Replace($root + '\', '') -replace '\\', '/') + ' x' + $m.Count)
        }
    }
    return @{ Count = $n; Hits = $hits }
}

# 1. *_Boot launcher classes must be gone (regex uses \b so SPEC01_BootHook is NOT a match)
$r = Total 'class\s+\w+_Boot\b'
Add-Check '1. class *_Boot (four launcher classes removed)' 'grep -R -E "class \w+_Boot\b" <scope>' $r.Count 0
$raw = Total 'class\s+\w*_Boot\w*'
[void]$sb.AppendLine('   note: looser pattern "class .*_Boot" also matches the runtime hook(s): ' + (($raw.Hits | Where-Object { $_ -notmatch 'QaLauncher' }) -join ', '))
[void]$sb.AppendLine('')

# 2. legacy per-suite JSON serializers must be gone
$r = Total 'class\s+\w+_Json\b'
Add-Check '2. class *_Json (replaced by QaJson)' 'grep -R -E "class \w+_Json\b" <scope>' $r.Count 0

# 3-4. single definitions of shared fixtures
$r = Total 'class\s+PlayerProbe\b'
Add-Check '3. class PlayerProbe (single definition in QaFixtures)' 'grep -R -E "class PlayerProbe" <scope>' $r.Count 1
[void]$sb.AppendLine('   where: ' + ($r.Hits -join ', '))
[void]$sb.AppendLine('')
$r = Total 'class\s+HitCollector\b'
Add-Check '4. class HitCollector (single definition in QaFixtures)' 'grep -R -E "class HitCollector" <scope>' $r.Count 1
[void]$sb.AppendLine('   where: ' + ($r.Hits -join ', '))
[void]$sb.AppendLine('')

# 5-7. single definitions of shared helpers
$r = Total 'static\s+Dictionary<string,\s*object>\s+Dict\s*\('
Add-Check '5. static Dictionary<string, object> Dict( definition' 'grep -R -E "static Dictionary<string, object> Dict\(" <scope>' $r.Count 1
[void]$sb.AppendLine('   where: ' + ($r.Hits -join ', '))
[void]$sb.AppendLine('')
$r = Total 'IEnumerator\s+WaitState\s*\('
Add-Check '6. IEnumerator WaitState( definition' 'grep -R -E "IEnumerator WaitState\(" <scope>' $r.Count 1
[void]$sb.AppendLine('   where: ' + ($r.Hits -join ', '))
[void]$sb.AppendLine('')
$r = Total 'static\s+string\s+GetHead\s*\('
Add-Check '7. static string GetHead( definition' 'grep -R -E "static string GetHead\(" <scope>' $r.Count 1
[void]$sb.AppendLine('   where: ' + ($r.Hits -join ', '))
[void]$sb.AppendLine('')

# 8. evidence root literal only in QaEvidence.cs and QA_InEditorMenu.cs
$litPattern = '"Docs", "QA", "Evidence"'
$litTotal = Total $litPattern
Add-Check '8a. "Docs", "QA", "Evidence" occurrences in scope' 'grep -R -F """Docs"", ""QA"", ""Evidence""" <scope>' $litTotal.Count 2
[void]$sb.AppendLine('   where: ' + ($litTotal.Hits -join ', '))
[void]$sb.AppendLine('')
$bad = @()
foreach ($h in $litTotal.Hits) {
    if ($h -notmatch '(QaEvidence\.cs|QA_InEditorMenu\.cs)') { $bad += $h }
}
Add-Check '8b. same literal outside QaEvidence.cs / QA_InEditorMenu.cs' 'grep -R -F literal <scope> | grep -v -E "QaEvidence.cs|QA_InEditorMenu.cs"' $bad.Count 0

# 9. evidence adapters are thin (<= 40 lines, no File./Directory.)
$adapters = @(
    'SPEC01\Runtime\SPEC01_Evidence.cs',
    'REFACTOR\Runtime\REFACTOR_Evidence.cs',
    'FND00B\Runtime\FND00B06_Evidence.cs',
    'DIAG\Runtime\DIAG_Evidence.cs'
)
$idx = 0
foreach ($a in $adapters) {
    $idx++
    $p = Join-Path $tests $a
    $t = [System.IO.File]::ReadAllText($p, [System.Text.Encoding]::UTF8)
    $lines = ($t -split "`r?`n").Count
    if ($t.EndsWith("`n")) { $lines-- }
    $io = ([regex]::Matches($t, 'File\.|Directory\.')).Count
    Add-Check ('9.' + $idx + 'a. ' + ($a -replace '\\', '/') + ' line count (<= 40)') ('wc -l Assets/_Project/Tests/' + ($a -replace '\\', '/') + '  # measured ' + $lines) ($lines -le 40) $true
    Add-Check ('9.' + $idx + 'b. ' + ($a -replace '\\', '/') + ' I/O tokens (File./Directory.)') ('grep -E "File\.|Directory\." Assets/_Project/Tests/' + ($a -replace '\\', '/')) $io 0
}

# 10. QaLauncher / QaLaunchSpec exist exactly once (phase E new files)
$r = Total 'class\s+QaLauncher\b'
Add-Check '10a. class QaLauncher (single definition)' 'grep -R -E "class QaLauncher\b" <scope>' $r.Count 1
$r = Total 'class\s+QaLaunchSpec\b'
Add-Check '10b. class QaLaunchSpec (single definition)' 'grep -R -E "class QaLaunchSpec\b" <scope>' $r.Count 1

# 11. line counts (reference only, NOT a pass criterion)
$after = 0
foreach ($f in $files) { $after += ([System.IO.File]::ReadAllLines($f)).Count }
[void]$sb.AppendLine('## 11. line counts (reference only; NOT a pass criterion per AC-6)')
[void]$sb.AppendLine('command : sum of line counts over scope files')
[void]$sb.AppendLine('before  : 8209 lines (phase A baseline, recorded in TASK AC revision)')
[void]$sb.AppendLine('phaseD  : 8929 lines (recorded at end of phase D)')
[void]$sb.AppendLine('after   : ' + $after + ' lines (phase E; QaLauncher merged 4 boot classes)')
[void]$sb.AppendLine('result  : INFO')
[void]$sb.AppendLine('')

[void]$sb.AppendLine('=== OVERALL ===')
[void]$sb.AppendLine($(if ($script:allOk) { 'ALL CHECKS PASS' } else { 'SOME CHECKS FAIL (see above)' }))
[void]$sb.AppendLine('')

[System.IO.File]::WriteAllText($out, $sb.ToString(), (New-Object System.Text.UTF8Encoding($false)))
Write-Output ('wrote: ' + $out)
Write-Output ('overall: ' + $script:allOk)
Write-Output ('lines after: ' + $after)
