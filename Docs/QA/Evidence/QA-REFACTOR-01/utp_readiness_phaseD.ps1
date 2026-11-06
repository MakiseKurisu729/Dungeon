# QA-REFACTOR-01 phase D - AC-10 (UTP readiness) static checks, appended to utp_readiness.txt
# Each check prints: command -> actual -> expected -> result
$ErrorActionPreference = 'Stop'
$root = (Get-Location).Path
$tests = Join-Path $root 'Assets\_Project\Tests'
$out = Join-Path $root 'Docs\QA\Evidence\QA-REFACTOR-01\utp_readiness.txt'
$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine('')
[void]$sb.AppendLine('## PHASE D (DIAG suite migration) - appended ' + (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'))
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

# --- D1. DIAG Assert*(IQaSink sink) count ---
$diagRunner = Join-Path $tests 'DIAG\Runtime\DIAG_ProbeRunner.cs'
$diagText = [System.IO.File]::ReadAllText($diagRunner, [System.Text.Encoding]::UTF8)
$diagSinkCount = Count-Regex $diagText 'Assert\w+\(IQaSink sink'
Add-Check 'D1. Assert*(IQaSink sink) in DIAG_ProbeRunner.cs' 'grep -E "Assert\w+\(IQaSink sink" Assets/_Project/Tests/DIAG/Runtime/DIAG_ProbeRunner.cs' $diagSinkCount 7
[void]$sb.AppendLine('   methods: ' + ((([regex]::Matches($diagText, 'IEnumerator\s+(Assert\w+)\s*\(IQaSink sink')) | ForEach-Object { $_.Groups[1].Value }) -join ', '))
[void]$sb.AppendLine('')

# --- D2. forbidden tokens inside every Assert* body ---
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

$bodies = Get-MethodBodies $diagText
$bad = 0
$oldSinkUsers = @()
foreach ($k in ($bodies.Keys | Sort-Object)) {
    $bad += Count-Regex $bodies[$k] 'File\.|Directory\.|SessionState|EditorApplication|QaLauncher|_Boot'
    if ((Count-Regex $bodies[$k] '_ev\.') -gt 0) { $oldSinkUsers += $k }
    if ((Count-Regex $bodies[$k] 'sink\.Record|sink\.BlockedAssertion|sink\.InfraFailure') -eq 0) { $oldSinkUsers += ($k + '(no-sink-call)') }
}
Add-Check 'D2. forbidden tokens inside DIAG Assert* bodies' 'per-method brace scan: grep -E "File\.|Directory\.|SessionState|EditorApplication|QaLauncher|_Boot" in every Assert* body' $bad 0
[void]$sb.AppendLine('   methods scanned (' + $bodies.Count + '): ' + (($bodies.Keys | Sort-Object) -join ', '))
[void]$sb.AppendLine('   still reading runner field `_ev` / not calling sink (phase D target = none): ' + $(if ($oldSinkUsers.Count -eq 0) { '(none)' } else { ($oldSinkUsers -join ', ') }))
[void]$sb.AppendLine('')

# --- D3. DIAG_Evidence is a thin adapter ---
$diagEv = Join-Path $tests 'DIAG\Runtime\DIAG_Evidence.cs'
$diagEvText = [System.IO.File]::ReadAllText($diagEv, [System.Text.Encoding]::UTF8)
$diagEvLines = ($diagEvText -split "`r?`n").Count
if ($diagEvText.EndsWith("`n")) { $diagEvLines-- }
Add-Check 'D3a. DIAG_Evidence.cs line count (adapter limit <= 40, measured)' 'wc -l Assets/_Project/Tests/DIAG/Runtime/DIAG_Evidence.cs' $diagEvLines 34
Add-Check 'D3b. class DIAG_Evidence : QaEvidence' 'grep -E "class DIAG_Evidence\s*:\s*QaEvidence" Assets/_Project/Tests/DIAG/Runtime/DIAG_Evidence.cs' (Count-Regex $diagEvText 'class\s+DIAG_Evidence\s*:\s*QaEvidence') 1
$evIo = Count-Regex $diagEvText 'File\.|Directory\.|SessionState|EditorApplication|QaLauncher'
Add-Check 'D3c. no I/O / boot tokens inside DIAG_Evidence.cs (adapter is pure spec assembly)' 'grep -E "File\.|Directory\.|SessionState|EditorApplication|QaLauncher" Assets/_Project/Tests/DIAG/Runtime/DIAG_Evidence.cs' $evIo 0
Add-Check 'D3d. DIAG_Evidence.cs declares no local WriteSummary/Record/BuildLine implementation' 'grep -E "void WriteSummary|void Record|string BuildLine" Assets/_Project/Tests/DIAG/Runtime/DIAG_Evidence.cs' (Count-Regex $diagEvText 'void\s+WriteSummary|void\s+Record\(|string\s+BuildLine') 0

# --- D4. Qa layer invariants ---
$sinkText = [System.IO.File]::ReadAllText((Join-Path $tests 'QA\Runtime\QaSink.cs'), [System.Text.Encoding]::UTF8)
Add-Check 'D4a. QaSink.cs forbidden tokens (unchanged)' 'grep -E "File|Directory|SessionState|EditorApplication|QaLauncher" Assets/_Project/Tests/QA/Runtime/QaSink.cs' (Count-Regex $sinkText 'File|Directory|SessionState|EditorApplication|QaLauncher') 0
$evCore = [System.IO.File]::ReadAllText((Join-Path $tests 'QA\Runtime\QaEvidence.cs'), [System.Text.Encoding]::UTF8)
Add-Check 'D4b. QaEvidence still declares IQaSink (unchanged)' 'grep -E "class QaEvidence\s*:\s*IQaSink" Assets/_Project/Tests/QA/Runtime/QaEvidence.cs' (Count-Regex $evCore 'class\s+QaEvidence\s*:\s*IQaSink') 1
$docsLit = 0
Get-ChildItem (Join-Path $tests 'DIAG') -Recurse -Filter *.cs | ForEach-Object {
    $docsLit += Count-Regex ([System.IO.File]::ReadAllText($_.FullName, [System.Text.Encoding]::UTF8)) '"Docs", "QA", "Evidence"'
}
Add-Check 'D4c. "Docs","QA","Evidence" literal inside DIAG/** (adapter must not hardcode the root)' 'grep -R "\"Docs\", \"QA\", \"Evidence\"" Assets/_Project/Tests/DIAG --include=*.cs' $docsLit 0

# --- D5. per-runner sink-assert tally (all four suites after phase D) ---
$runners = @(
    @{ Path = 'SPEC01\Runtime\SPEC01_ProbeRunner.cs'; Expected = 1 },
    @{ Path = 'REFACTOR\Runtime\REFACTOR_ProbeRunner.cs'; Expected = 16 },
    @{ Path = 'FND00B\Runtime\FND00B06_ProbeRunner.cs'; Expected = 14 },
    @{ Path = 'DIAG\Runtime\DIAG_ProbeRunner.cs'; Expected = 7 }
)
foreach ($r in $runners) {
    $t = [System.IO.File]::ReadAllText((Join-Path $tests $r.Path), [System.Text.Encoding]::UTF8)
    Add-Check ('D5. Assert*(IQaSink sink) in ' + ($r.Path -replace '\\', '/')) ('grep -E "Assert\w+\(IQaSink sink" Assets/_Project/Tests/' + ($r.Path -replace '\\', '/')) (Count-Regex $t 'Assert\w+\(IQaSink sink') $r.Expected
}

# --- D6. DIAG boot / git-head dedup (DIAG_Boot intentionally kept until phase E) ---
$diagBatch = [System.IO.File]::ReadAllText((Join-Path $tests 'DIAG\Editor\DIAG_BatchAcceptance.cs'), [System.Text.Encoding]::UTF8)
Add-Check 'D6a. local GitHead removed from DIAG_BatchAcceptance.cs (uses QaScene.GetHead)' 'grep -E "static string GitHead|GitHead\(\)" Assets/_Project/Tests/DIAG/Editor/DIAG_BatchAcceptance.cs' (Count-Regex $diagBatch 'static\s+string\s+GitHead|GitHead\(\)') 0
Add-Check 'D6b. QaScene.GetHead call sites in DIAG_BatchAcceptance.cs' 'grep -E "QaScene.GetHead" Assets/_Project/Tests/DIAG/Editor/DIAG_BatchAcceptance.cs' (Count-Regex $diagBatch 'QaScene\.GetHead') 1
Add-Check 'D6c. class DIAG_Boot kept for phase E (must still exist)' 'grep -E "class DIAG_Boot" Assets/_Project/Tests/DIAG/Editor/DIAG_BatchAcceptance.cs' (Count-Regex $diagBatch 'class\s+DIAG_Boot') 1
Add-Check 'D6d. local Dict/V3/GetHead removed from DIAG_ProbeRunner.cs' 'grep -E "Dictionary<string, object> Dict|Dictionary<string, object> V3|static string GetHead" Assets/_Project/Tests/DIAG/Runtime/DIAG_ProbeRunner.cs' (Count-Regex $diagText 'Dictionary<string,\s*object>\s+(Dict|V3)\s*\(|static\s+string\s+GetHead') 0

[void]$sb.AppendLine('=== PHASE D OVERALL ===')
[void]$sb.AppendLine($(if ($script:allOk) { 'ALL APPLICABLE CHECKS PASS' } else { 'SOME CHECKS FAIL (see above)' }))

[System.IO.File]::AppendAllText($out, $sb.ToString(), (New-Object System.Text.UTF8Encoding($false)))
Write-Output ('appended: ' + $out)
Write-Output ('phase D overall: ' + $script:allOk)
