# AC-7 L2 fault injection - domain reload during Play (result: NOT TRIGGERED / NOT EXECUTABLE)

Goal: while `Tools/QA/In-Editor/DIAG` is in Play, trigger one domain reload by a net-zero script edit
(add one comment line -> save -> wait for reload -> delete the line -> save), then observe the
watchdog / launcher teardown.

## What was actually attempted

| attempt | mechanism | injection time vs Play | observed |
|---|---|---|---|
| 1 | file watcher tails Editor.log for "playModeStateChanged=EnteredPlayMode", then appends `// L2-DOMAINRELOAD-PROBE` to DIAG_ProbeRunner.cs | injected while Play was running (run started 16:48:06.895, injection 16:48:05.938, first assertion 16:48:07.977) | run completed normally: pass=6 fail=1 exit=2, no domain reload during Play (Unity deferred the recompile until Play ended) -> editor_run7 InEditorResult code=2 reason=none |
| 2 | same watcher + Win32 AppActivate("Unity") attempt to force the editor to pick up the change | injected while Play was running (injection 16:55:09.786, run started 16:55:10.834) | identical: run completed normally (editor_run8, code=2), AppActivate returned False |
| 3 | same watcher + scripted `refresh_unity(compile=request)` in the same MCP batch | injection landed after the run had already finished (MCP round-trip latency ~20s per call) | run completed normally (editor_run9). Recompile happened after Play ended. |

## Why it cannot be triggered from here

Unity 2022.3 defers script recompilation while Play Mode is running (`Script Changes While Playing` =
"Recompile After Finished Playing", the editor default). Every injection was queued and applied only
after the DIAG run had already written its Summary and called `EditorApplication.isPlaying = false`.
A genuine mid-Play domain reload would require either changing that editor preference / ProjectSettings
(outside this task scope and owner-owned) or `execute_code` (forbidden by the task scope).

Per the dispatch: no fabrication. The three attempts above are recorded as "not triggered / not
executable", with the launcher behaving correctly (no false watchdog, no editor kill).

## Side observations that DO cover the watchdog contract

- Each of the 5 DIAG runs after the unified launcher (editor_run7..11) went through the new
  `QaLauncher.Boot` -> `EnteredPlayMode` -> `TryStartRunner` -> `Finished` -> `EnteredEditMode` ->
  `ExitWithCode` -> `QA_InEditorMenu.TryFinish` path: `[DIAG] batchmode exit 2` followed by
  `[QA][INEDITOR] done task=DIAG code=2`, i.e. TryFinish took over and the editor stayed alive.
- No `[DIAG][WATCHDOG]` line was emitted in any run, i.e. no false positive from the 10s/3s grace.
- The 8 AC-2 runs (all four suites x2) exercise the same teardown 8 more times with the same result.

## Files in this folder
- `l2_watch*.txt` : injection timestamps (wall clock) from the watcher processes
- `InEditorResult_editor_run{7,8,9}.txt` : the three attempt runs
- `console_excerpt.txt` : DIAG console excerpt from Editor.log
- `netzero_diff.txt` : proof the touched script is byte-identical to HEAD (blob sha match)
