<#
.SYNOPSIS
    Runs the HappyAlmere CI verification pipeline locally using Docker (zero GitHub Actions minutes).
.DESCRIPTION
    Executes the same build, static source/dependency scan, isolated PostgreSQL 18 recovery/migration/rollback,
    Identity isolation, .NET test suite, production container build/smoke test, and optional Playwright browser
    checks locally on your machine. Safe for both Public and Private repositories.
.PARAMETER Quick
    Fast inner-loop mode: skips heavy multi-container PostgreSQL migration rehearsals and runs Debug/Release
    builds, static/vulnerability scans, non-Postgres unit tests, and the production Docker image smoke test.
.PARAMETER IncludeBrowser
    Also runs npm ci --ignore-scripts, npm audit --audit-level=high, and npm run test:browser (Playwright).
.PARAMETER SkipContainer
    Skips building and smoke-testing infrastructure/deploy/Dockerfile.
.EXAMPLE
    .\scripts\Run-LocalCi.ps1 -Quick
.EXAMPLE
    .\scripts\Run-LocalCi.ps1
.EXAMPLE
    .\scripts\Run-LocalCi.ps1 -IncludeBrowser
#>
[CmdletBinding()]
param(
    [switch]$Quick,
    [switch]$IncludeBrowser,
    [switch]$SkipContainer,
    [switch]$SkipHistoryScan
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
function Resolve-PythonCommand {
    foreach ($candidate in @('python', 'python3')) {
        if (Get-Command $candidate -ErrorAction SilentlyContinue) {
            $ver = (& $candidate --version 2>&1)
            if ($LASTEXITCODE -eq 0 -and $ver -match 'Python 3\.') {
                return $candidate
            }
        }
    }
    throw 'A working Python 3 interpreter is required (python or python3).'
}
$pythonCmd = Resolve-PythonCommand
$smokeContainer = 'happyalmere-local-ci-smoke'
$smokeImage = 'happyalmere-web:local-ci'
$startedSourcePostgres = $false
$results = [System.Collections.Generic.List[object]]::new()
$totalTimer = [System.Diagnostics.Stopwatch]::StartNew()
$overallStatus = 'PASS'

function Invoke-Stage {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][scriptblock]$Action
    )
    Write-Host ""
    Write-Host "=== [LOCAL CI] $Name ===" -ForegroundColor Cyan
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        & $Action
        $sw.Stop()
        $sec = [math]::Round($sw.Elapsed.TotalSeconds, 2)
        $results.Add([pscustomobject]@{ stage = $Name; status = 'PASS'; seconds = $sec })
        Write-Host "PASS: $Name (${sec}s)" -ForegroundColor Green
    }
    catch {
        $sw.Stop()
        $sec = [math]::Round($sw.Elapsed.TotalSeconds, 2)
        $results.Add([pscustomobject]@{ stage = $Name; status = 'FAIL'; seconds = $sec; error = $_.Exception.Message })
        Write-Host "FAIL: $Name (${sec}s) - $($_.Exception.Message)" -ForegroundColor Red
        throw
    }
}

function Assert-ExitZero {
    param([Parameter(Mandatory)][string]$Context)
    if ($LASTEXITCODE -ne 0) {
        throw "$Context exited with code $LASTEXITCODE."
    }
}

Push-Location $repoRoot
try {
    Invoke-Stage 'Preflight (Docker, .NET, Python)' {
        $null = Get-Command dotnet -ErrorAction Stop
        $null = Get-Command docker -ErrorAction Stop
        $dockerVer = (& docker info --format '{{.ServerVersion}}' 2>&1)
        Assert-ExitZero 'docker info (is Docker Desktop running?)'
        Write-Host "Docker Engine: $dockerVer | Python: $pythonCmd | .NET: $(& dotnet --version)"
    }

    Invoke-Stage 'Restore & Build (Debug + Release)' {
        & dotnet restore HappyAlmere.slnx
        Assert-ExitZero 'dotnet restore'
        & dotnet build HappyAlmere.slnx -c Debug --no-restore
        Assert-ExitZero 'dotnet build Debug'
        & dotnet build HappyAlmere.slnx -c Release --no-restore
        Assert-ExitZero 'dotnet build Release'
    }

    Invoke-Stage 'Static Source, Dependency & Vulnerability Scan' {
        if ($Quick -or $SkipHistoryScan) {
            # Fast working-tree check (<1s on Windows): dependency graph, JSON validity, and current-file secret scan.
            $fastScan = @'
import json, pathlib, re, subprocess, sys, xml.etree.ElementTree as ET
root = pathlib.Path.cwd()
files = subprocess.check_output(['git', '-C', str(root), 'ls-files', '--cached', '--others', '--exclude-standard'], text=True, encoding='utf-8').splitlines()
graph = {pathlib.Path(f).stem: [pathlib.Path(r.attrib['Include']).stem for r in ET.parse(root / f).getroot().iter('ProjectReference')] for f in files if f.endswith('.csproj')}
expected = {'HappyCity.Domain': [], 'HappyCity.Application': ['HappyCity.Domain'], 'HappyCity.Infrastructure': ['HappyCity.Application', 'HappyCity.Domain'], 'HappyCity.Web': ['HappyCity.Application', 'HappyCity.Infrastructure'], 'HappyCity.FoundationTests': ['HappyCity.Web']}
assert graph == expected, f'Project graph mismatch: {graph}'
text_ext = {'.cs', '.csproj', '.cshtml', '.json', '.props', '.slnx', '.md', '.yml', '.css', '.cjs', '.py', '.log', '.txt', '.ps1', '.sql'}
patterns = [
    re.compile(r'-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----'),
    re.compile(r'\b(?:gh[pousr]_[A-Za-z0-9]{30,}|github_pat_[A-Za-z0-9_]{40,})\b'),
    re.compile(r'\bAKIA[A-Z0-9]{16}\b'),
    re.compile(r"""(?i)(?:password|pwd|clientsecret|api[_-]?key|access[_-]?token)\s*[:=]\s*["'][^"']{6,}["']"""),
]
findings = []
for f in files:
    p = root / f
    if not p.is_file(): continue
    if f.endswith('.json'): json.loads(p.read_text(encoding='utf-8-sig'))
    if p.suffix in text_ext:
        txt = p.read_text(encoding='utf-8-sig', errors='replace')
        for rx in patterns:
            if rx.search(txt): findings.append(f)
assert not findings, f'Secret pattern found in: {findings}'
print(f'Fast source scan PASS ({len(files)} tracked/untracked files, 0 secret findings)')
'@
            $fastScan | & $pythonCmd -
            Assert-ExitZero 'fast working-tree source scan'
        }
        else {
            & $pythonCmd scripts/verify-source.py | Out-Null
            Assert-ExitZero 'scripts/verify-source.py'
        }
        $vulnOut = (& dotnet list HappyAlmere.slnx package --vulnerable --include-transitive | Out-String)
        Assert-ExitZero 'dotnet list vulnerable packages'
        if ($vulnOut -match 'has the following vulnerable packages') {
            Write-Host $vulnOut
            throw 'Vulnerable NuGet package detected.'
        }
    }

    if ($Quick) {
        Invoke-Stage 'Unit Tests (Non-Postgres Fast Suite)' {
            $env:IdentityFoundation__Enabled = 'false'
            & dotnet test HappyAlmere.slnx -c Release --no-build --filter 'Category!=Postgres&Category!=CityRecovery&FullyQualifiedName!~IdentityArchitectureTests.Enabled_services_resolve'
            Assert-ExitZero 'dotnet test quick unit suite'
        }
    }
    else {
        Invoke-Stage 'Ensure Preserved Baseline PostgreSQL Container' {
            $pgId = (& docker ps -aq --filter 'label=com.docker.compose.project=happyalmere-v02' --filter 'label=com.docker.compose.service=postgres' | Select-Object -First 1)
            if ([string]::IsNullOrWhiteSpace($pgId)) {
                Write-Host 'No preserved happyalmere-v02 postgres container found; initializing baseline via Initialize-LocalDatabase.ps1...'
                & (Join-Path $repoRoot 'scripts/Initialize-LocalDatabase.ps1')
                Assert-ExitZero 'Initialize-LocalDatabase.ps1'
                $startedSourcePostgres = $true
            }
            else {
                $running = (& docker inspect -f '{{.State.Running}}' $pgId).Trim()
                if ($running -ne 'true') {
                    & docker start $pgId | Out-Null
                    Assert-ExitZero 'docker start baseline postgres'
                    $startedSourcePostgres = $true
                    Start-Sleep -Seconds 3
                }
            }
        }

        $gateEvidence = $null
        Invoke-Stage 'Isolated PostgreSQL 18 Backup & Restore Gate' {
            & $pythonCmd scripts/verify-database-recovery.py
            Assert-ExitZero 'scripts/verify-database-recovery.py'
            $gateEvidence = (Get-ChildItem artifacts/ha002a/*/evidence.json | Sort-Object FullName | Select-Object -Last 1).FullName
        }

        $forwardEnv = $null
        Invoke-Stage 'Forward Migration, Rollback, Dutch FTS & Privilege Gate' {
            & $pythonCmd scripts/verify-forward-migration.py --gate $gateEvidence
            Assert-ExitZero 'scripts/verify-forward-migration.py'
            $forwardEnv = (Get-ChildItem artifacts/forward/*/rehearsal.env | Sort-Object FullName | Select-Object -Last 1).FullName
        }

        Invoke-Stage 'Regression Suite (Identity Disabled)' {
            . (Join-Path $repoRoot 'scripts/Load-LocalEnvironment.ps1') -EnvironmentFile $forwardEnv
            $env:IdentityFoundation__Enabled = 'false'
            $env:HAPPYCITY_EVIDENCE_DIRECTORY = Join-Path $repoRoot 'artifacts/local-ci/identity-disabled-tests'
            & dotnet test HappyAlmere.slnx -c Release --no-build -m:1 --filter 'FullyQualifiedName!~HappyCity.FoundationTests.Identity'
            Assert-ExitZero 'Identity-disabled regression suite'
        }

        $identityEnv = $null
        Invoke-Stage 'Isolated Identity Contract, Privileges & Coordinated Recovery' {
            & $pythonCmd scripts/verify-identity-foundation.py
            Assert-ExitZero 'scripts/verify-identity-foundation.py'
            $identityEnv = (Get-ChildItem artifacts/identity/*/tests.env | Sort-Object FullName | Select-Object -Last 1).FullName
        }

        Invoke-Stage 'Full Migrated PostgreSQL 18, Dutch FTS, City & Identity Test Suite' {
            $env:HAPPYCITY_EVIDENCE_DIRECTORY = Join-Path $repoRoot 'artifacts/local-ci/forward-tests'
            & (Join-Path $repoRoot 'scripts/Test-Local.ps1') -EnvironmentFile $identityEnv
            Assert-ExitZero 'scripts/Test-Local.ps1'
        }

        if ($IncludeBrowser) {
            Invoke-Stage 'Playwright HTTPS Browser, Responsive & Security Suite' {
                & npm ci --ignore-scripts
                Assert-ExitZero 'npm ci'
                & npm audit --audit-level=high
                Assert-ExitZero 'npm audit'
                . (Join-Path $repoRoot 'scripts/Load-LocalEnvironment.ps1') -EnvironmentFile $identityEnv
                & npm run test:browser
                Assert-ExitZero 'npm run test:browser'
            }
        }
    }

    if (-not $SkipContainer) {
        Invoke-Stage 'Production Container Build & Live /healthz Smoke Test' {
            & docker build -f infrastructure/deploy/Dockerfile -t $smokeImage .
            Assert-ExitZero 'docker build production image'

            & docker rm -f $smokeContainer 2>$null | Out-Null
            $testDbConn = 'Host=127.0.0.1;Port=5432;Database=happyalmere;Username=happy_app;SSL Mode=VerifyFull;Root Certificate=/etc/ssl/certs/ca-certificates.crt'
            & docker run -d --name $smokeContainer -p 127.0.0.1:18080:8080 `
                -e ASPNETCORE_ENVIRONMENT=Production `
                -e AllowedHosts=localhost `
                -e IdentityFoundation__Enabled=false `
                -e "ConnectionStrings__HappyCity=$testDbConn" `
                $smokeImage | Out-Null
            Assert-ExitZero 'docker run smoke container'

            $probeScript = @'
import http.client, time, sys
for _ in range(25):
    try:
        conn = http.client.HTTPConnection("127.0.0.1", 18080, timeout=2)
        conn.request("GET", "/healthz", headers={"Host": "localhost"})
        resp = conn.getresponse()
        body = resp.read().decode("utf-8", errors="replace")
        headers = {k.lower(): v for k, v in resp.getheaders()}
        conn.close()
        if resp.status == 200:
            break
    except Exception:
        time.sleep(1)
else:
    print("Smoke container did not become healthy on /healthz", file=sys.stderr)
    sys.exit(1)

assert resp.status == 200, f"Expected 200, got {resp.status}"
assert '{"status":"ok"}' in body, f"Unexpected body: {body}"
assert headers.get("x-content-type-options", "").lower() == "nosniff", "Missing nosniff header"
assert "content-security-policy" in headers, "Missing CSP header"

conn = http.client.HTTPConnection("127.0.0.1", 18080, timeout=2)
conn.request("GET", "/healthz", headers={"Host": "evil.example"})
bad = conn.getresponse()
bad.read()
conn.close()
assert bad.status == 400, f"Expected 400 for evil.example Host header, got {bad.status}"
print("Container smoke probe PASS: /healthz=200 + headers + evil.example=400")
'@
            $probeScript | & $pythonCmd -
            Assert-ExitZero 'production container smoke probe'
        }
    }
}
catch {
    $overallStatus = 'FAIL'
    throw
}
finally {
    $totalTimer.Stop()
    & docker rm -f $smokeContainer 2>$null | Out-Null

    foreach ($pattern in @('artifacts/forward/*/evidence.json', 'artifacts/identity/*/evidence.json')) {
        foreach ($file in Get-ChildItem $pattern -ErrorAction SilentlyContinue) {
            try {
                $ev = Get-Content $file.FullName -Raw | ConvertFrom-Json
                if ($ev.PSObject.Properties['targetContainers'] -and $ev.targetContainers) {
                    foreach ($c in $ev.targetContainers) {
                        & docker stop $c 2>$null | Out-Null
                    }
                }
            }
            catch { }
        }
    }

    if ($startedSourcePostgres) {
        $pgId = (& docker ps -q --filter 'label=com.docker.compose.project=happyalmere-v02' --filter 'label=com.docker.compose.service=postgres' | Select-Object -First 1)
        if (-not [string]::IsNullOrWhiteSpace($pgId)) {
            & docker stop $pgId 2>$null | Out-Null
        }
    }

    $summaryDir = Join-Path $repoRoot 'artifacts/local-ci'
    New-Item -ItemType Directory -Path $summaryDir -Force | Out-Null
    $summary = [pscustomobject]@{
        status       = $overallStatus
        mode         = if ($Quick) { 'quick' } elseif ($IncludeBrowser) { 'full+browser' } else { 'full' }
        totalSeconds = [math]::Round($totalTimer.Elapsed.TotalSeconds, 2)
        completedUtc = (Get-Date).ToUniversalTime().ToString('o')
        stages       = $results
    }
    $summaryPath = Join-Path $summaryDir 'summary.json'
    $summary | ConvertTo-Json -Depth 5 | Set-Content -Path $summaryPath -Encoding utf8
    Write-Host ""
    Write-Host "=== [LOCAL CI] $overallStatus in $($summary.totalSeconds)s | Summary: $summaryPath ===" -ForegroundColor $(if ($overallStatus -eq 'PASS') { 'Green' } else { 'Red' })
    Pop-Location
}
