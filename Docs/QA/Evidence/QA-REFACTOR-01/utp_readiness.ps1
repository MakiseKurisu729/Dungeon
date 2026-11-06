# QA-REFACTOR-01 phase A - AC-10 (UTP readiness) static checks
# Each check prints: command -> actual -> expected -> result
$ErrorActionPreference = 'Stop'
$root = (Get-Location).Path
$tests = Join-Path $root 'Assets\_Project\Tests'
$out = Join-Path $root 'Docs\QA\Evidence\QA-REFACTOR-01\utp_readiness.txt'
$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine('# QA-REFACTOR-01 phase A - AC-10 UTP readiness static checks')
[void]$sb.AppendLine('generatedAt: ' + (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'))
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
    [void]$sb.AppendLine('result  : N/A - ' + $note)
    [void]$sb.AppendLine('')
}

# --- 1. interface IQaSink definitions ---
$sinkFile = Join-Path $tests 'QA\Runtime\QaSink.cs'
$sinkText = [System.IO.File]::ReadAllText($sinkFile, [System.Text.Encoding]::UTF8)
$declCount = 0
Get-ChildItem $tests -Recurse -Filter *.cs | ForEach-Object {
    $declCount += Count-Regex ([System.IO.File]::ReadAllText($_.FullName, [System.Text.Encoding]::UTF8)) 'interface\s+IQaSink\b'
}
Add-Check '1. interface IQaSink definition count' 'grep -R "interface\s+IQaSink\b" Assets/_Project/Tests --include=*.cs' $declCount 1

# --- 2. QaSink.cs forbidden tokens ---
Add-Check '2. QaSink.cs forbidden tokens' 'grep -E "File|Directory|SessionState|EditorApplication|QaLauncher" Assets/_Project/Tests/QA/Runtime/QaSink.cs' (Count-Regex $sinkText 'File|Directory|SessionState|EditorApplication|QaLauncher') 0

# --- 2b. QaSink.cs member list ---
$members = [regex]::Matches($sinkText, '^\s{4}(void\s+\w+|string\s+EvidenceDir)', 'Multiline') | ForEach-Object { $_.Groups[1].Value.Trim() }
Add-Info '2b. IQaSink member list' 'grep -E "^\s{4}(void|string)" Assets/_Project/Tests/QA/Runtime/QaSink.cs' ($members -join ' ; ') 'expected exactly: void Record ; void BlockedAssertion ; void InfraFailure ; string EvidenceDir'

# --- 3. QaEvidence implements IQaSink ---
$evText = [System.IO.File]::ReadAllText((Join-Path $tests 'QA\Runtime\QaEvidence.cs'), [System.Text.Encoding]::UTF8)
Add-Check '3. QaEvidence declares IQaSink' 'grep -E "class QaEvidence\s*:\s*IQaSink" Assets/_Project/Tests/QA/Runtime/QaEvidence.cs' (Count-Regex $evText 'class\s+QaEvidence\s*:\s*IQaSink') 1

# --- 4. Assert\w+\(IQaSink sink per ProbeRunner (phase A: SPEC01 >= 1, others 0 until their phase) ---
$runners = @(
    @{ Path = 'SPEC01\Runtime\SPEC01_ProbeRunner.cs'; Expected = '1' },
    @{ Path = 'REFACTOR\Runtime\REFACTOR_ProbeRunner.cs'; Expected = '0' },
    @{ Path = 'DIAG\Runtime\DIAG_ProbeRunner.cs'; Expected = '0' },
    @{ Path = 'FND00B\Runtime\FND00B06_ProbeRunner.cs'; Expected = '0' }
)
$totalSinkAsserts = 0
foreach ($r in $runners) {
    $t = [System.IO.File]::ReadAllText((Join-Path $tests $r.Path), [System.Text.Encoding]::UTF8)
    $n = Count-Regex $t 'Assert\w+\(IQaSink sink'
    $totalSinkAsserts += $n
    Add-Check ('4. Assert*(IQaSink sink) in ' + ($r.Path -replace '\\', '/')) ('grep -E "Assert\w+\(IQaSink sink" Assets/_Project/Tests/' + ($r.Path -replace '\\', '/')) $n $r.Expected
}
Add-Check '4b. phase A total Assert*(IQaSink sink) >= 1' 'sum of the four counts above' $totalSinkAsserts 1

# --- 5. forbidden tokens inside Assert* method bodies ---
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

foreach ($r in $runners) {
    $t = [System.IO.File]::ReadAllText((Join-Path $tests $r.Path), [System.Text.Encoding]::UTF8)
    $bodies = Get-MethodBodies $t
    $rel = $r.Path -replace '\\', '/'
    if ($bodies.Count -eq 0) {
        Add-Info ('5. forbidden tokens inside Assert* bodies of ' + $rel) ('per-method brace scan of Assets/_Project/Tests/' + $rel) 'no Assert* method found' 'this runner has no Assert*-named method yet (its assertions live inline); phase C must extract/rename them before this check can apply'
        continue
    }
    $bad = 0
    $oldSinkUsers = @()
    foreach ($k in ($bodies.Keys | Sort-Object)) {
        $bad += Count-Regex $bodies[$k] 'File\.|Directory\.|SessionState|EditorApplication|QaLauncher|_Boot'
        if ((Count-Regex $bodies[$k] '_ev\.') -gt 0) { $oldSinkUsers += $k }
    }
    Add-Check ('5. forbidden tokens inside Assert* bodies of ' + $rel) ('per-method brace scan: grep -E "File\.|Directory\.|SessionState|EditorApplication|QaLauncher|_Boot" in every Assert* body of Assets/_Project/Tests/' + $rel) $bad 0
    [void]$sb.AppendLine('   methods scanned (' + $bodies.Count + '): ' + (($bodies.Keys | Sort-Object) -join ', '))
    if ($oldSinkUsers.Count -gt 0) {
        [void]$sb.AppendLine('   still reading runner field `_ev` (phase A migration pending; see REPORT section "still-writing-files / still-boot-dependent asserts"): ' + ($oldSinkUsers -join ', '))
    }
    [void]$sb.AppendLine('')
}

[void]$sb.AppendLine('=== OVERALL ===')
[void]$sb.AppendLine($(if ($script:allOk) { 'ALL APPLICABLE CHECKS PASS' } else { 'SOME CHECKS FAIL (see above)' }))

[System.IO.File]::WriteAllText($out, $sb.ToString(), (New-Object System.Text.UTF8Encoding($false)))
Write-Output ('written: ' + $out)
Write-Output ('overall: ' + $script:allOk)
