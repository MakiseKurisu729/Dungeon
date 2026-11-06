# QA-REFACTOR-01 phase C - AC-10 (UTP readiness) static checks, appended to utp_readiness.txt
# Each check prints: command -> actual -> expected -> result
$ErrorActionPreference = 'Stop'
$root = (Get-Location).Path
$tests = Join-Path $root 'Assets\_Project\Tests'
$out = Join-Path $root 'Docs\QA\Evidence\QA-REFACTOR-01\utp_readiness.txt'
$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine('')
[void]$sb.AppendLine('## PHASE C (FND00B06 suite migration) - appended ' + (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'))
[void]$sb.AppendLine('root: ' + $tests)
[void]$sb.AppendLine('')

function Count-Regex([string]$text, [string]$pattern) {
    return ([regex]::Matches($text, $pattern)).Count
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
function Add-Info([string]$name, [string]$command, $actual, [string]$note) {
    [void]$sb.AppendLine('## ' + $name)
    [void]$sb.AppendLine('command : ' + $command)
    [void]$sb.AppendLine('actual  : ' + $actual)
    [void]$sb.AppendLine('result  : INFO - ' + $note)
    [void]$sb.AppendLine('')
}

# --- C1. FND00B06 Assert*(IQaSink sink) count ---
$fndRunner = Join-Path $tests 'FND00B\Runtime\FND00B06_ProbeRunner.cs'
$fndText = [System.IO.File]::ReadAllText($fndRunner, [System.Text.Encoding]::UTF8)
$fndSinkCount = Count-Regex $fndText 'Assert\w+\(IQaSink sink'
Add-Check 'C1. Assert*(IQaSink sink) in FND00B06_ProbeRunner.cs (>= 14 = 7 item-4 + 7 item-6)' 'grep -E "Assert\w+\(IQaSink sink" Assets/_Project/Tests/FND00B/FND00B06_ProbeRunner.cs' $fndSinkCount 14
[void]$sb.AppendLine('   methods: ' + ((([regex]::Matches($fndText, 'IEnumerator\s+(Assert\w+)\s*\(IQaSink sink')) | ForEach-Object { $_.Groups[1].Value }) -join ', '))
[void]$sb.AppendLine('')

# --- C2. forbidden tokens inside every Assert* body (FND00B06) ---
function Get-MethodBodies([string]$text) {
    $lines = $text -split "`r?`n"
    $bodies = @{}
    $i = 0
    while ($i -lt $lines.Count) {
        $m = [regex]::Match($lines[$i], '^\s*IEnumerator\s+(Assert\w+)\s*\(')
        if (-not $m.Success) { $i++; continue }
        $name = $m.Groups[1].Value
        $depth = 0
        $started = $false
        $j = $i
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

$bodies = Get-MethodBodies $fndText
$bad = 0
$oldSinkUsers = @()
foreach ($k in ($bodies.Keys | Sort-Object)) {
    $bad += Count-Regex $bodies[$k] 'File\.|Directory\.|SessionState|EditorApplication|QaLauncher|_Boot'
    if ((Count-Regex $bodies[$k] '_ev\.') -gt 0) { $oldSinkUsers += $k }
    if ((Count-Regex $bodies[$k] 'sink\.Record|sink\.BlockedAssertion|sink\.InfraFailure') -eq 0) { $oldSinkUsers += ($k + '(no-sink-call)') }
}
Add-Check 'C2. forbidden tokens inside FND00B06 Assert* bodies' 'per-method brace scan: grep -E "File\.|Directory\.|SessionState|EditorApplication|QaLauncher|_Boot" in every Assert* body' $bad 0
[void]$sb.AppendLine('   methods scanned (' + $bodies.Count + '): ' + (($bodies.Keys | Sort-Object) -join ', '))
[void]$sb.AppendLine('   still reading runner field `_ev` / not calling sink (phase C target = none): ' + $(if ($oldSinkUsers.Count -eq 0) { '(none)' } else { ($oldSinkUsers -join ', ') }))
[void]$sb.AppendLine('')

# --- C3. FND00B06_Evidence is a thin adapter ---
$fndEv = Join-Path $tests 'FND00B\Runtime\FND00B06_Evidence.cs'
$fndEvText = [System.IO.File]::ReadAllText($fndEv, [System.Text.Encoding]::UTF8)
$fndEvLines = ($fndEvText -split "`r?`n").Count
if ($fndEvText.EndsWith("`n")) { $fndEvLines-- }
Add-Check 'C3a. FND00B06_Evidence.cs line count (adapter limit <= 40, measured)' 'wc -l Assets/_Project/Tests/FND00B/Runtime/FND00B06_Evidence.cs' $fndEvLines 33
Add-Check 'C3b. class FND00B06_Evidence : QaEvidence' 'grep -E "class FND00B06_Evidence\s*:\s*QaEvidence" Assets/_Project/Tests/FND00B/Runtime/FND00B06_Evidence.cs' (Count-Regex $fndEvText 'class\s+FND00B06_Evidence\s*:\s*QaEvidence') 1
$jsonLeft = 0
Get-ChildItem $tests -Recurse -Filter *.cs | ForEach-Object {
    $jsonLeft += Count-Regex ([System.IO.File]::ReadAllText($_.FullName, [System.Text.Encoding]::UTF8)) 'class\s+FND00B06_Json\b'
}
Add-Check 'C3c. class FND00B06_Json removed (whole Tests tree)' 'grep -R "class FND00B06_Json" Assets/_Project/Tests --include=*.cs' $jsonLeft 0
$evIo = Count-Regex $fndEvText 'File\.|Directory\.|SessionState|EditorApplication|QaLauncher'
Add-Check 'C3d. no I/O / boot tokens inside FND00B06_Evidence.cs (adapter is pure spec assembly)' 'grep -E "File\.|Directory\.|SessionState|EditorApplication|QaLauncher" Assets/_Project/Tests/FND00B/Runtime/FND00B06_Evidence.cs' $evIo 0

# --- C4. Qa layer invariants unchanged by phase C ---
$sinkText = [System.IO.File]::ReadAllText((Join-Path $tests 'QA\Runtime\QaSink.cs'), [System.Text.Encoding]::UTF8)
Add-Check 'C4a. QaSink.cs forbidden tokens (unchanged)' 'grep -E "File|Directory|SessionState|EditorApplication|QaLauncher" Assets/_Project/Tests/QA/Runtime/QaSink.cs' (Count-Regex $sinkText 'File|Directory|SessionState|EditorApplication|QaLauncher') 0
$evCore = [System.IO.File]::ReadAllText((Join-Path $tests 'QA\Runtime\QaEvidence.cs'), [System.Text.Encoding]::UTF8)
Add-Check 'C4b. QaEvidence still declares IQaSink (unchanged)' 'grep -E "class QaEvidence\s*:\s*IQaSink" Assets/_Project/Tests/QA/Runtime/QaEvidence.cs' (Count-Regex $evCore 'class\s+QaEvidence\s*:\s*IQaSink') 1
Add-Check 'C4c. "Docs","QA","Evidence" literal inside the phase-C scope = QaEvidence.cs only' 'grep -R "\"Docs\", \"QA\", \"Evidence\"" Assets/_Project/Tests/QA --include=*.cs' ((Get-ChildItem (Join-Path $tests 'QA') -Recurse -Filter *.cs | ForEach-Object { Count-Regex ([System.IO.File]::ReadAllText($_.FullName, [System.Text.Encoding]::UTF8)) '"Docs", "QA", "Evidence"' } | Measure-Object -Sum).Sum) 2
Add-Check 'C4d. "Docs","QA","Evidence" literal inside FND00B06_Evidence.cs (adapter must not hardcode the root)' 'grep -E "\"Docs\", \"QA\", \"Evidence\"" Assets/_Project/Tests/FND00B/Runtime/FND00B06_Evidence.cs' (Count-Regex $fndEvText '"Docs", "QA", "Evidence"') 0

# --- C5. per-runner sink-assert tally (phase E expectations) ---
$runners = @(
    @{ Path = 'SPEC01\Runtime\SPEC01_ProbeRunner.cs'; Expected = '1' },
    @{ Path = 'REFACTOR\Runtime\REFACTOR_ProbeRunner.cs'; Expected = '16' },
    @{ Path = 'FND00B\Runtime\FND00B06_ProbeRunner.cs'; Expected = '14' },
    @{ Path = 'DIAG\Runtime\DIAG_ProbeRunner.cs'; Expected = '0' }
)
foreach ($r in $runners) {
    $t = [System.IO.File]::ReadAllText((Join-Path $tests $r.Path), [System.Text.Encoding]::UTF8)
    Add-Info ('C5. Assert*(IQaSink sink) in ' + ($r.Path -replace '\\', '/')) ('grep -E "Assert\w+\(IQaSink sink" Assets/_Project/Tests/' + ($r.Path -replace '\\', '/')) (Count-Regex $t 'Assert\w+\(IQaSink sink') ('phase C expectation = ' + $r.Expected + ' [DIAG migrates in phase D]')
}

[void]$sb.AppendLine('=== PHASE C OVERALL ===')
[void]$sb.AppendLine($(if ($script:allOk) { 'ALL APPLICABLE CHECKS PASS' } else { 'SOME CHECKS FAIL (see above)' }))

[System.IO.File]::AppendAllText($out, $sb.ToString(), (New-Object System.Text.UTF8Encoding($false)))
Write-Output ('appended: ' + $out)
Write-Output ('phase C overall: ' + $script:allOk)
