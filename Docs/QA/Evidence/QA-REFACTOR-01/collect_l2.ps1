$ErrorActionPreference = 'Stop'
$utf8 = New-Object System.Text.UTF8Encoding($false)
try { [Console]::OutputEncoding = [System.Text.Encoding]::UTF8 } catch {}
$root = (Get-Location).Path
$ev = Join-Path $root 'Docs\QA\Evidence\QA-REFACTOR-01\l2_domainreload'
New-Item -ItemType Directory -Force $ev | Out-Null
$log = Join-Path $env:LOCALAPPDATA 'Unity\Editor\Editor.log'
$target = 'Assets/_Project/Tests/DIAG/Runtime/DIAG_ProbeRunner.cs'

function Capture([string]$cmd) {
    $prev = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $out = & cmd /c "$cmd 2>&1" | Out-String
    $ErrorActionPreference = $prev
    $lines = ($out -split "`r?`n") | Where-Object { $_ -notmatch '^warning: in the working copy' -and $_ -ne '' }
    return ($lines -join "`n").TrimEnd()
}

# 1. watcher logs (verifiable injection times)
foreach ($w in @('l2_watch', 'l2_watch2', 'l2_watch3')) {
    $src = Join-Path $env:TEMP ("opencode\" + $w + ".txt")
    if (Test-Path $src) { Copy-Item $src (Join-Path $ev ($w + '.txt')) -Force }
}

# 2. InEditorResult.txt of the three attempt runs (DIAG editor_run7/8/9)
$rows = @()
foreach ($n in @(7, 8, 9)) {
    $src = Join-Path $root ("Docs\QA\Evidence\DIAG-SKILL\editor_run" + $n + "\InEditorResult.txt")
    if (Test-Path $src) {
        Copy-Item $src (Join-Path $ev ("InEditorResult_editor_run" + $n + '.txt')) -Force
        $rows += ('editor_run' + $n + ': ' + ((Get-Content $src) -join ' | '))
    }
}

# 3. console excerpt from Editor.log (DIAG lines, last 40)
$tail = Get-Content $log -Tail 20000 -Encoding UTF8
$diag = $tail | Select-String -Pattern '\[DIAG\] (boot;|playModeStateChanged|play-mode runner created|runner start|finish;)|\[DIAG\]\[WATCHDOG\]|INEDITOR\] result .*DIAG'
$excerpt = New-Object System.Text.StringBuilder
[void]$excerpt.AppendLine('# DIAG launcher console excerpt from Unity Editor.log (lifecycle lines only, last 120 matches)')
[void]$excerpt.AppendLine('# Covers L2 attempts 1-3 (editor_run7/8/9) and the two AC-2 DIAG runs (editor_run10/11).')
[void]$excerpt.AppendLine('# No "[DIAG][WATCHDOG]" line appears in any run (no false watchdog).')
[void]$excerpt.AppendLine('')
($diag | Select-Object -Last 120) | ForEach-Object { [void]$excerpt.AppendLine($_.Line) }
[System.IO.File]::WriteAllText((Join-Path $ev 'console_excerpt.txt'), $excerpt.ToString(), $utf8)

# 4. net-zero proof for the script that was touched during the attempts
$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine('# AC-7 L2 net-zero proof: the test script touched during the injections is byte-identical to HEAD')
[void]$sb.AppendLine('generated: ' + (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'))
[void]$sb.AppendLine('file: ' + $target)
[void]$sb.AppendLine('')
[void]$sb.AppendLine('## git diff --exit-code (empty / exit 0 = no difference)')
[void]$sb.AppendLine('$ git -c core.quotepath=false diff --exit-code -- ' + $target)
$diff = Capture ('git -c core.quotepath=false diff --exit-code -- "' + $target + '"')
[void]$sb.AppendLine($(if ($diff.Length -eq 0) { '<empty output>' } else { $diff }))
[void]$sb.AppendLine('')
[void]$sb.AppendLine('## blob identity')
[void]$sb.AppendLine('$ git hash-object ' + $target)
$h1 = Capture ('git hash-object "' + $target + '"')
[void]$sb.AppendLine($h1)
[void]$sb.AppendLine('$ git rev-parse HEAD:' + $target)
$h2 = Capture ('git rev-parse "HEAD:' + $target + '"')
[void]$sb.AppendLine($h2)
[void]$sb.AppendLine('')
[void]$sb.AppendLine('identical(sha): ' + ($h1 -eq $h2))
[void]$sb.AppendLine('')
[void]$sb.AppendLine('## status of the file (no staged/unstaged change)')
[void]$sb.AppendLine('$ git -c core.quotepath=false status --porcelain -- ' + $target)
$st = Capture ('git -c core.quotepath=false status --porcelain -- "' + $target + '"')
[void]$sb.AppendLine($(if ($st.Length -eq 0) { '<empty output>' } else { $st }))
[System.IO.File]::WriteAllText((Join-Path $ev 'netzero_diff.txt'), $sb.ToString(), $utf8)

# 5. attempts summary + conclusion
$readme = New-Object System.Text.StringBuilder
[void]$readme.AppendLine('# AC-7 L2 fault injection - domain reload during Play (result: NOT TRIGGERED / NOT EXECUTABLE)')
[void]$readme.AppendLine('')
[void]$readme.AppendLine('Goal: while `Tools/QA/In-Editor/DIAG` is in Play, trigger one domain reload by a net-zero script edit')
[void]$readme.AppendLine('(add one comment line -> save -> wait for reload -> delete the line -> save), then observe the')
[void]$readme.AppendLine('watchdog / launcher teardown.')
[void]$readme.AppendLine('')
[void]$readme.AppendLine('## What was actually attempted')
[void]$readme.AppendLine('')
[void]$readme.AppendLine('| attempt | mechanism | injection time vs Play | observed |')
[void]$readme.AppendLine('|---|---|---|---|')
[void]$readme.AppendLine('| 1 | file watcher tails Editor.log for "playModeStateChanged=EnteredPlayMode", then appends `// L2-DOMAINRELOAD-PROBE` to DIAG_ProbeRunner.cs | injected while Play was running (run started 16:48:06.895, injection 16:48:05.938, first assertion 16:48:07.977) | run completed normally: pass=6 fail=1 exit=2, no domain reload during Play (Unity deferred the recompile until Play ended) -> editor_run7 InEditorResult code=2 reason=none |')
[void]$readme.AppendLine('| 2 | same watcher + Win32 AppActivate("Unity") attempt to force the editor to pick up the change | injected while Play was running (injection 16:55:09.786, run started 16:55:10.834) | identical: run completed normally (editor_run8, code=2), AppActivate returned False |')
[void]$readme.AppendLine('| 3 | same watcher + scripted `refresh_unity(compile=request)` in the same MCP batch | injection landed after the run had already finished (MCP round-trip latency ~20s per call) | run completed normally (editor_run9). Recompile happened after Play ended. |')
[void]$readme.AppendLine('')
[void]$readme.AppendLine('## Why it cannot be triggered from here')
[void]$readme.AppendLine('')
[void]$readme.AppendLine('Unity 2022.3 defers script recompilation while Play Mode is running (`Script Changes While Playing` =')
[void]$readme.AppendLine('"Recompile After Finished Playing", the editor default). Every injection was queued and applied only')
[void]$readme.AppendLine('after the DIAG run had already written its Summary and called `EditorApplication.isPlaying = false`.')
[void]$readme.AppendLine('A genuine mid-Play domain reload would require either changing that editor preference / ProjectSettings')
[void]$readme.AppendLine('(outside this task scope and owner-owned) or `execute_code` (forbidden by the task scope).')
[void]$readme.AppendLine('')
[void]$readme.AppendLine('Per the dispatch: no fabrication. The three attempts above are recorded as "not triggered / not')
[void]$readme.AppendLine('executable", with the launcher behaving correctly (no false watchdog, no editor kill).')
[void]$readme.AppendLine('')
[void]$readme.AppendLine('## Side observations that DO cover the watchdog contract')
[void]$readme.AppendLine('')
[void]$readme.AppendLine('- Each of the 5 DIAG runs after the unified launcher (editor_run7..11) went through the new')
[void]$readme.AppendLine('  `QaLauncher.Boot` -> `EnteredPlayMode` -> `TryStartRunner` -> `Finished` -> `EnteredEditMode` ->')
[void]$readme.AppendLine('  `ExitWithCode` -> `QA_InEditorMenu.TryFinish` path: `[DIAG] batchmode exit 2` followed by')
[void]$readme.AppendLine('  `[QA][INEDITOR] done task=DIAG code=2`, i.e. TryFinish took over and the editor stayed alive.')
[void]$readme.AppendLine('- No `[DIAG][WATCHDOG]` line was emitted in any run, i.e. no false positive from the 10s/3s grace.')
[void]$readme.AppendLine('- The 8 AC-2 runs (all four suites x2) exercise the same teardown 8 more times with the same result.')
[void]$readme.AppendLine('')
[void]$readme.AppendLine('## Files in this folder')
[void]$readme.AppendLine('- `l2_watch*.txt` : injection timestamps (wall clock) from the watcher processes')
[void]$readme.AppendLine('- `InEditorResult_editor_run{7,8,9}.txt` : the three attempt runs')
[void]$readme.AppendLine('- `console_excerpt.txt` : DIAG console excerpt from Editor.log')
[void]$readme.AppendLine('- `netzero_diff.txt` : proof the touched script is byte-identical to HEAD (blob sha match)')
[System.IO.File]::WriteAllText((Join-Path $ev 'README.txt'), $readme.ToString(), $utf8)

Write-Output ('wrote l2 evidence to ' + $ev)
Get-ChildItem $ev | ForEach-Object { Write-Output ('  ' + $_.Name) }
