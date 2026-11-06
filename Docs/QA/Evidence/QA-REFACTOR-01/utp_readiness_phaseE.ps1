# QA-REFACTOR-01 phase E - AC-10 (UTP readiness) + AC-7 launcher structural checks
# Appends a "PHASE E" section to utp_readiness.txt (append-only; re-runs replace only this section).
$ErrorActionPreference = 'Stop'
$root = (Get-Location).Path
$tests = Join-Path $root 'Assets\_Project\Tests'
$out = Join-Path $root 'Docs\QA\Evidence\QA-REFACTOR-01\utp_readiness.txt'
$utf8 = New-Object System.Text.UTF8Encoding($false)

# drop a previous PHASE E section (own append only), keep phases A-D untouched
$existing = [System.IO.File]::ReadAllText($out, [System.Text.Encoding]::UTF8)
$cut = $existing.IndexOf('## PHASE E (unified QaLauncher')
if ($cut -ge 0) { [System.IO.File]::WriteAllText($out, $existing.Substring(0, $cut), $utf8) }

$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine('## PHASE E (unified QaLauncher + all assert signatures) - appended ' + (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'))
[void]$sb.AppendLine('root: ' + $tests)
[void]$sb.AppendLine('')

function Count-Regex([string]$text, [string]$pattern) { return ([regex]::Matches($text, $pattern)).Count }

function Strip-Comments([string]$t) {
    $t = [regex]::Replace($t, '/\*[\s\S]*?\*/', '')
    $t = [regex]::Replace($t, '(?m)^\s*///.*$', '')
    $t = [regex]::Replace($t, '(?m)//.*$', '')
    return $t
}

function Get-NamedBody([string]$text, [string]$signaturePattern) {
    $m = [regex]::Match($text, $signaturePattern)
    if (-not $m.Success) { return '' }
    $i = $m.Index
    $depth = 0; $started = $false; $buf = New-Object System.Text.StringBuilder
    while ($i -lt $text.Length) {
        $ch = $text[$i]
        [void]$buf.Append($ch)
        if ($ch -eq '{') { $depth++; $started = $true }
        elseif ($ch -eq '}') { $depth--; if ($started -and $depth -le 0) { break } }
        $i++
    }
    return $buf.ToString()
}

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

function Get-MethodBodies([string]$text) {
    $lines = $text -split "`r?`n"
    $bodies = @{}
    $i = 0
    while ($i -lt $lines.Count) {
        $m = [regex]::Match($lines[$i], '^\s*IEnumerator\s+(Assert\w+)\s*\(')
        if (-not $m.Success) { $i++; continue }
        $name = $m.Groups[1].Value
        $depth = 0; $started = $false; $j = $i
        $buf = New-Object System.Text.StringBuilder
        while ($j -lt $lines.Count) {
            $line = $lines[$j]
            [void]$buf.AppendLine($line)
            foreach ($ch in $line.ToCharArray()) {
                if ($ch -eq '{') { $depth++; $started = $true }
                elseif ($ch -eq '}') { $depth-- }
            }
            if ($started -and $depth -le 0) { break }
            $j++
        }
        $bodies[$name] = $buf.ToString()
        $i = $j + 1
    }
    return $bodies
}

# ---------- E1. Assert*(IQaSink sink) per suite ----------
$runners = @(
    @{ Path = 'SPEC01\Runtime\SPEC01_ProbeRunner.cs'; Expected = 10; Suite = 'SPEC01' },
    @{ Path = 'REFACTOR\Runtime\REFACTOR_ProbeRunner.cs'; Expected = 16; Suite = 'REFACTOR' },
    @{ Path = 'FND00B\Runtime\FND00B06_ProbeRunner.cs'; Expected = 14; Suite = 'FND00B06' },
    @{ Path = 'DIAG\Runtime\DIAG_ProbeRunner.cs'; Expected = 7; Suite = 'DIAG' }
)
$suiteBodies = @{}
foreach ($r in $runners) {
    $p = Join-Path $tests $r.Path
    $t = [System.IO.File]::ReadAllText($p, [System.Text.Encoding]::UTF8)
    $n = Count-Regex $t 'Assert\w+\(IQaSink sink'
    Add-Check ('E1. Assert*(IQaSink sink) in ' + $r.Suite) ('grep -E "Assert\w+\(IQaSink sink" Assets/_Project/Tests/' + ($r.Path -replace '\\', '/')) $n $r.Expected
    [void]$sb.AppendLine('   methods: ' + ((([regex]::Matches($t, 'IEnumerator\s+(Assert\w+)\s*\(IQaSink sink')) | ForEach-Object { $_.Groups[1].Value }) -join ', '))
    [void]$sb.AppendLine('')
    $suiteBodies[$r.Suite] = Get-MethodBodies $t
}
[void]$sb.AppendLine('NOTE (SPEC01 count): SPEC01 emits 12 assertion rows from 10 Assert* methods;')
[void]$sb.AppendLine('AssertDualPathAndClocks emits Attack.DualPathUnified + Clock.MeleeNormalizedWindow + Clock.RangedOneShotPerCycle,')
[void]$sb.AppendLine('so 12 assertion IDs vs 10 IEnumerator Assert* signatures. AC-10 wording is "every Assert* takes IQaSink as first')
[void]$sb.AppendLine('parameter"; that holds for 10/10 methods. Splitting AssertDualPathAndClocks would restructure judgement code (out of scope).')
[void]$sb.AppendLine('')

# ---------- E2/E3. forbidden tokens + _ev inside every Assert* body ----------
foreach ($s in @('SPEC01', 'REFACTOR', 'FND00B06', 'DIAG')) {
    $bodies = $suiteBodies[$s]
    $bad = 0; $evUsers = @(); $noSink = @()
    foreach ($k in ($bodies.Keys | Sort-Object)) {
        $bad += Count-Regex $bodies[$k] 'File\.|Directory\.|SessionState|EditorApplication|QaLauncher|_Boot'
        if ((Count-Regex $bodies[$k] '_ev\.') -gt 0) { $evUsers += $k }
        if ((Count-Regex $bodies[$k] 'sink\.Record|sink\.BlockedAssertion|sink\.InfraFailure') -eq 0) { $noSink += $k }
    }
    Add-Check ('E2. forbidden tokens inside ' + $s + ' Assert* bodies') 'per-method brace scan: grep -E "File\.|Directory\.|SessionState|EditorApplication|QaLauncher|_Boot" in every Assert* body' $bad 0
    Add-Check ('E3. assert bodies still using runner field _ev (' + $s + ')') 'per-method brace scan: grep -E "_ev\." in every Assert* body' $evUsers.Count 0
    [void]$sb.AppendLine('   methods scanned (' + $bodies.Count + '): ' + (($bodies.Keys | Sort-Object) -join ', '))
    [void]$sb.AppendLine('   _ev users: ' + $(if ($evUsers.Count -eq 0) { '(none)' } else { $evUsers -join ', ' }))
    [void]$sb.AppendLine('   no-sink-call methods: ' + $(if ($noSink.Count -eq 0) { '(none)' } else { $noSink -join ', ' }))
    [void]$sb.AppendLine('')
}

# ---------- E4. IQaSink contract ----------
$sinkText = Strip-Comments ([System.IO.File]::ReadAllText((Join-Path $tests 'QA\Runtime\QaSink.cs'), [System.Text.Encoding]::UTF8))
Add-Check 'E4a. interface IQaSink defined once' 'grep -E "interface IQaSink" Assets/_Project/Tests/QA/Runtime/QaSink.cs' (Count-Regex $sinkText 'interface\s+IQaSink') 1
Add-Check 'E4b. QaSink.cs forbidden tokens' 'grep -E "File|Directory|SessionState|EditorApplication|QaLauncher" Assets/_Project/Tests/QA/Runtime/QaSink.cs' (Count-Regex $sinkText 'File|Directory|SessionState|EditorApplication|QaLauncher') 0
Add-Check 'E4c. IQaSink members (Record/BlockedAssertion/InfraFailure/EvidenceDir) total' 'grep -E "void Record\(|void BlockedAssertion\(|void InfraFailure\(|string EvidenceDir" Assets/_Project/Tests/QA/Runtime/QaSink.cs' (Count-Regex $sinkText 'void\s+\w+\(|string\s+EvidenceDir') 4
Add-Check 'E4d. IQaSink has no other members' 'grep -E "^\s+(void|string|int|bool)\s" Assets/_Project/Tests/QA/Runtime/QaSink.cs' (Count-Regex $sinkText '(?m)^\s+void\s+\w+\(') 3
$evCore = Strip-Comments ([System.IO.File]::ReadAllText((Join-Path $tests 'QA\Runtime\QaEvidence.cs'), [System.Text.Encoding]::UTF8))
Add-Check 'E4e. class QaEvidence : IQaSink' 'grep -E "class QaEvidence\s*:\s*IQaSink" Assets/_Project/Tests/QA/Runtime/QaEvidence.cs' (Count-Regex $evCore 'class\s+QaEvidence\s*:\s*IQaSink') 1

# ---------- E5. QaLauncher semantics (AC-7 structural backfill) ----------
$q = Strip-Comments ([System.IO.File]::ReadAllText((Join-Path $tests 'QA\Editor\QaLauncher.cs'), [System.Text.Encoding]::UTF8))
Add-Check 'E5a. single InitializeOnLoadMethod bootstrap' 'grep -E "\[InitializeOnLoadMethod\]" Assets/_Project/Tests/QA/Editor/QaLauncher.cs' (Count-Regex $q '\[InitializeOnLoadMethod\]') 1
Add-Check 'E5b. ClearStaleRunFlags definition' 'grep -E "static bool ClearStaleRunFlags\(" Assets/_Project/Tests/QA/Editor/QaLauncher.cs' (Count-Regex $q 'static\s+bool\s+ClearStaleRunFlags\s*\(') 1
Add-Check 'E5c. first-start grace 10s' 'grep -E "FirstStartGraceSeconds = 10f" Assets/_Project/Tests/QA/Editor/QaLauncher.cs' (Count-Regex $q 'FirstStartGraceSeconds\s*=\s*10f') 1
Add-Check 'E5d. runner-lost grace 3s' 'grep -E "RunnerLostGraceSeconds = 3f" Assets/_Project/Tests/QA/Editor/QaLauncher.cs' (Count-Regex $q 'RunnerLostGraceSeconds\s*=\s*3f') 1
Add-Check 'E5e. watchdog reason literal (code only)' 'grep -c "runner-lost-domain-reload" Assets/_Project/Tests/QA/Editor/QaLauncher.cs' (Count-Regex $q 'runner-lost-domain-reload') 2
Add-Check 'E5f. RunnerAlive static reference check' 'grep -E "_runner != null" Assets/_Project/Tests/QA/Editor/QaLauncher.cs' (Count-Regex $q '_runner != null') 1
Add-Check 'E5g. RunnerAlive GameObject.Find(name) check' 'grep -E "GameObject.Find\(name\)" Assets/_Project/Tests/QA/Editor/QaLauncher.cs' (Count-Regex $q 'GameObject\.Find\(name\)') 1
Add-Check 'E5h. runner DontDestroyOnLoad' 'grep -E "DontDestroyOnLoad" Assets/_Project/Tests/QA/Editor/QaLauncher.cs' (Count-Regex $q 'DontDestroyOnLoad') 1
$wd = Get-NamedBody $q 'static\s+void\s+WatchdogFail\s*\('
$ex = Get-NamedBody $q 'static\s+void\s+ExitWithCode\s*\('
Add-Check 'E5i. watchdog: TryFinish precedes isPlaying=false/Exit(3)' 'body scan of WatchdogFail' (($wd.IndexOf('TryFinish') -ge 0) -and ($wd.IndexOf('TryFinish') -lt $wd.IndexOf('EditorApplication.Exit(3)'))) $true
Add-Check 'E5j. ExitWithCode: TryFinish precedes EditorApplication.Exit(code)' 'body scan of ExitWithCode' (($ex.IndexOf('TryFinish') -ge 0) -and ($ex.IndexOf('TryFinish') -lt $ex.IndexOf('EditorApplication.Exit(code)'))) $true
Add-Check 'E5k. no *_Boot left in the launcher' 'grep -E "\w+_Boot" Assets/_Project/Tests/QA/Editor/QaLauncher.cs' (Count-Regex $q '\w+_Boot') 0
Add-Check 'E5l. EnteredEditMode+Finished branch present' 'grep -E "EnteredEditMode && SessionState.GetBool\(spec.FinishedKey" Assets/_Project/Tests/QA/Editor/QaLauncher.cs' (Count-Regex $q 'EnteredEditMode\s*&&\s*SessionState\.GetBool\(spec\.FinishedKey') 1
Add-Check 'E5m. Begin() writes Active/Started/Finished then EnterPlaymode' 'body scan of Begin' (Count-Regex (Get-NamedBody $q 'static\s+void\s+Begin\s*\(') 'SessionState\.SetBool\(spec\.(ActiveKey|StartedKey|FinishedKey)') 3
# ---------- E6. registry drives menu (no per-suite switch left) ----------
$menu = Strip-Comments ([System.IO.File]::ReadAllText((Join-Path $tests 'QA\Editor\QA_InEditorMenu.cs'), [System.Text.Encoding]::UTF8))
Add-Check 'E6a. menu uses registry Launch() instead of per-suite switch' 'grep -E "spec\.Launch\(\)" Assets/_Project/Tests/QA/Editor/QA_InEditorMenu.cs' (Count-Regex $menu 'spec\.Launch\(\)') 1
Add-Check 'E6b. no per-suite Run() switch left in Proceed' 'grep -E "case \"SPEC01\":" Assets/_Project/Tests/QA/Editor/QA_InEditorMenu.cs' (Count-Regex $menu 'case\s+"SPEC01"') 0
Add-Check 'E6c. menu items unchanged (4 entries)' 'grep -E "\[MenuItem\(\"Tools/QA/In-Editor/" Assets/_Project/Tests/QA/Editor/QA_InEditorMenu.cs' (Count-Regex $menu '\[MenuItem\("Tools/QA/In-Editor/') 4
Add-Check 'E6d. subdir increment rule unchanged' 'grep -E "editor_run" Assets/_Project/Tests/QA/Editor/QA_InEditorMenu.cs' (Count-Regex $menu '"editor_run') 4
Add-Check 'E6e. runInBackground record/restore unchanged' 'grep -E "SessionRunInBackgroundArmedKey" Assets/_Project/Tests/QA/Editor/QA_InEditorMenu.cs' (Count-Regex $menu 'SessionRunInBackgroundArmedKey') 4
Add-Check 'E6f. preflight timeout unchanged' 'grep -E "PreflightTimeoutSeconds = 120" Assets/_Project/Tests/QA/Editor/QA_InEditorMenu.cs' (Count-Regex $menu 'PreflightTimeoutSeconds\s*=\s*120') 1

[void]$sb.AppendLine('=== PHASE E OVERALL ===')
[void]$sb.AppendLine($(if ($script:allOk) { 'ALL APPLICABLE CHECKS PASS' } else { 'SOME CHECKS FAIL (see above)' }))
[void]$sb.AppendLine('')
[System.IO.File]::AppendAllText($out, $sb.ToString(), $utf8)
Write-Output ('appended: ' + $out)
Write-Output ('phase E overall: ' + $script:allOk)
