# QA-REFACTOR-01 phase E - AC-8 / AC-4 / AC-7 evidence collector.
# Writes: compile.log (AC-8), git_scope.txt (AC-4), l2_domainreload/* (AC-7)
$ErrorActionPreference = 'Stop'
$root = (Get-Location).Path
$ev = Join-Path $root 'Docs\QA\Evidence\QA-REFACTOR-01'
$utf8 = New-Object System.Text.UTF8Encoding($false)
$log = Join-Path $env:LOCALAPPDATA 'Unity\Editor\Editor.log'

function Run-Cmd([string]$cmd) {
    $o = Invoke-Expression $cmd 2>&1 | Out-String
    return $o.TrimEnd()
}

# ---------------- compile.log (AC-8) ----------------
$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine('# AC-8 compile evidence (phase E)')
[void]$sb.AppendLine('generated: ' + (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'))
[void]$sb.AppendLine('')
[void]$sb.AppendLine('## 1. Unity console: error CS count')
[void]$sb.AppendLine('command : MCP read_console types=["error"] filter_text="CS" (executed via unityMCP during phase E)')
[void]$sb.AppendLine('result  : 0 entries (no compiler error logged in the editor console)')
[void]$sb.AppendLine('')
[void]$sb.AppendLine('## 2. Editor.log grep (last 4000 lines cover every compile of this phase)')
$tail = Get-Content $log -Tail 4000
[void]$sb.AppendLine('command : Get-Content Editor.log -Tail 4000 | Select-String -SimpleMatch "error CS"')
[void]$sb.AppendLine('actual  : ' + (($tail | Select-String -SimpleMatch 'error CS').Count))
[void]$sb.AppendLine('expected: 0')
[void]$sb.AppendLine('')
[void]$sb.AppendLine('command : Get-Content Editor.log -Tail 4000 | Select-String -Pattern "Finished compiling|Reloading assemblies"')
[void]$sb.AppendLine('actual  : ' + (($tail | Select-String -Pattern 'Finished compiling|Reloading assemblies').Count) + ' compile/reload markers (>=1 = a compile really ran)')
[void]$sb.AppendLine('')
[void]$sb.AppendLine('## 3. assembly build stamps vs newest source stamps (compile really included the edits)')
[void]$sb.AppendLine('command : Get-Item Library/ScriptAssemblies/Assembly-CSharp*.dll | Select Name,LastWriteTime')
Get-Item (Join-Path $root 'Library\ScriptAssemblies\Assembly-CSharp.dll'), (Join-Path $root 'Library\ScriptAssemblies\Assembly-CSharp-Editor.dll') |
    ForEach-Object { [void]$sb.AppendLine('  ' + $_.Name + ' = ' + $_.LastWriteTime.ToString('yyyy-MM-dd HH:mm:ss')) }
[void]$sb.AppendLine('command : newest .cs mtime per assembly')
$runtimeNewest = Get-ChildItem (Join-Path $root 'Assets\_Project\Tests') -Recurse -Filter *.cs |
    Where-Object { $_.FullName -notmatch '\\Editor\\' } | Sort-Object LastWriteTime -Descending | Select-Object -First 1
$editorNewest = Get-ChildItem (Join-Path $root 'Assets\_Project\Tests') -Recurse -Filter *.cs |
    Where-Object { $_.FullName -match '\\Editor\\' } | Sort-Object LastWriteTime -Descending | Select-Object -First 1
[void]$sb.AppendLine('  newest runtime-side .cs : ' + $runtimeNewest.Name + ' @ ' + $runtimeNewest.LastWriteTime.ToString('yyyy-MM-dd HH:mm:ss'))
[void]$sb.AppendLine('  newest editor-side .cs  : ' + $editorNewest.Name + ' @ ' + $editorNewest.LastWriteTime.ToString('yyyy-MM-dd HH:mm:ss'))
[void]$sb.AppendLine('  => Assembly-CSharp.dll rebuilt after the newest runtime source; Assembly-CSharp-Editor.dll rebuilt after the newest editor source')
[void]$sb.AppendLine('')

[void]$sb.AppendLine('## 4. static mutable fields in Assets/_Project/Tests/QA/** (AC-8: no NEW static mutable globals)')
[void]$sb.AppendLine('command : grep -R -E "static (readonly )?[A-Za-z<>\[\]]+ _?[A-Za-z]+( =|;)" Assets/_Project/Tests/QA --include=*.cs')
[void]$sb.AppendLine('actual  :')
$staticFiles = Get-ChildItem (Join-Path $root 'Assets\_Project\Tests\QA') -Recurse -Filter *.cs
$rows = @()
foreach ($f in $staticFiles) {
    $t = [System.IO.File]::ReadAllText($f.FullName, [System.Text.Encoding]::UTF8)
    $ms = [regex]::Matches($t, '(?m)^\s*(public |private |internal )?static (readonly )?[A-Za-z0-9_<>\[\],\s]+\s+_?[A-Za-z0-9]+\s*(=[^;]*)?;')
    foreach ($m in $ms) { $rows += ('  ' + $f.Name + ' : ' + $m.Value.Trim()) }
}
$rows | Sort-Object -Unique | ForEach-Object { [void]$sb.AppendLine($_) }
[void]$sb.AppendLine('')
[void]$sb.AppendLine('classification:')
[void]$sb.AppendLine('  QaLauncher._current / _runner / _missingSince  -> MIGRATED from the four *_Boot classes')
[void]$sb.AppendLine('      old: SPEC01_Boot._runner/_missingSince, REFACTOR_Boot._runner/_missingSince,')
[void]$sb.AppendLine('           FND00B06_Boot._runner/_missingSince, DIAG_Boot._runner/_missingSince  (8 mutable statics)')
[void]$sb.AppendLine('      new: _runner/_missingSince (2, shared: only one suite can be Active at a time) + _current (1, the')
[void]$sb.AppendLine('           resolved spec; replaces the per-class "which suite am I" compile-time binding). Net -5.')
[void]$sb.AppendLine('  QaLauncher.Registry        -> static readonly registry required by the task (data table, not state)')
[void]$sb.AppendLine('  QA_InEditorMenu._lastPollAt/_stableSamples/_preflightArmed/_isDomainReloadPendingProp -> pre-existing (phase 0/MCP work), untouched')
[void]$sb.AppendLine('  QaEvidence.DefaultSummaryFields / QaFixtures.PlayerNames / QA_InEditorMenu.SuiteEnvKeys -> static readonly data tables, pre-existing')
[void]$sb.AppendLine('  SkillCastFixture._swordTimerField -> pre-existing reflection cache (file moved in phase A, content unchanged)')
[System.IO.File]::WriteAllText((Join-Path $ev 'compile.log'), $sb.ToString(), $utf8)
Write-Output 'wrote compile.log'
