<#
.SYNOPSIS
    Starts (or stops) an isolated local Docker preview of HappyAlmere on http://localhost:8088.
.DESCRIPTION
    Boots an isolated PostgreSQL 18 (VerifyFull TLS) + non-root .NET 10 web stack using ephemeral
    secrets stored outside Git (artifacts/local-preview/preview.env), applies all EF Core migrations
    (including HA-005-A Dutch Full-Text Search), and optionally loads the 30 curated Almere R1 draft
    listings (infrastructure/deploy/seed-almere-r1-draft.sql). Never touches preserved happyalmere-v02.
.PARAMETER WithAlmereSeed
    Loads the 30 curated Almere candidate listings (R03-01..R03-30) into the isolated preview database.
.PARAMETER Port
    Loopback port on 127.0.0.1 (default: 8088).
.PARAMETER Stop
    Stops and removes ONLY the isolated happyalmere-preview containers, volumes, and network.
.EXAMPLE
    .\scripts\Start-LocalPreview.ps1 -WithAlmereSeed
.EXAMPLE
    .\scripts\Start-LocalPreview.ps1 -Stop
#>
[CmdletBinding()]
param(
    [switch]$WithAlmereSeed,
    [ValidateRange(1024, 65535)]
    [int]$Port = 8088,
    [switch]$Stop
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$deployDir = Join-Path $repoRoot 'infrastructure/deploy'
$composeFile = Join-Path $deployDir 'compose.preview.yaml'
$previewDir = Join-Path $repoRoot 'artifacts/local-preview'
$envFile = Join-Path $previewDir 'preview.env'
$migrationFile = Join-Path $previewDir 'migrations.sql'
$projectName = 'happyalmere-preview'
$pgContainer = 'happyalmere-preview-postgres-1'

function New-RandomHex {
    $bytes = New-Object byte[] 32
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try { $rng.GetBytes($bytes) } finally { $rng.Dispose() }
    return ([System.BitConverter]::ToString($bytes) -replace '-', '').ToLowerInvariant()
}

function Assert-ExitZero {
    param([Parameter(Mandatory)][string]$Context)
    if ($LASTEXITCODE -ne 0) { throw "$Context exited with code $LASTEXITCODE." }
}

Push-Location $repoRoot
try {
    if ($Stop) {
        Write-Host "Stopping and removing isolated preview stack ($projectName)..." -ForegroundColor Cyan
        if (Test-Path $envFile) {
            & docker compose -p $projectName --env-file $envFile -f $composeFile down -v
        }
        else {
            & docker compose -p $projectName -f $composeFile down -v
        }
        if (Test-Path $envFile) { Remove-Item $envFile -Force }
        Write-Host "Preview stack stopped cleanly. Preserved happyalmere-v02 untouched." -ForegroundColor Green
        return
    }

    $null = (& docker info --format '{{.ServerVersion}}' 2>&1)
    Assert-ExitZero 'docker info (is Docker Desktop running?)'

    New-Item -ItemType Directory -Path $previewDir -Force | Out-Null

    # Reuse existing ephemeral preview secrets if container/volume already exists; otherwise create fresh ones.
    $secretsMap = @{}
    if (Test-Path $envFile) {
        foreach ($line in Get-Content $envFile) {
            if ($line -match '^([A-Z0-9_]+)=(.*)$') {
                $secretsMap[$matches[1]] = $matches[2]
            }
        }
    }
    if (-not $secretsMap.ContainsKey('POSTGRES_ADMIN_SECRET')) {
        $secretsMap['POSTGRES_ADMIN_SECRET'] = New-RandomHex
        $secretsMap['POSTGRES_APP_SECRET'] = New-RandomHex
        $secretsMap['SCHEMA_MIGRATOR_SECRET'] = New-RandomHex
        $secretsMap['BACKUP_ROLE_SECRET'] = New-RandomHex
    }
    $secretsMap['HAPPYCITY_PREVIEW_PORT'] = "$Port"
    $secretsMap['HAPPYCITY_RUNTIME_CONNECTION'] = "Host=postgres;Port=5432;Database=happyalmere;Username=happy_app;Password=$($secretsMap['POSTGRES_APP_SECRET']);SSL Mode=VerifyFull;Root Certificate=/var/lib/postgresql-tls/server.crt"

    $envLines = @(
        "HAPPYCITY_PREVIEW_PORT=$($secretsMap['HAPPYCITY_PREVIEW_PORT'])"
        "POSTGRES_ADMIN_SECRET=$($secretsMap['POSTGRES_ADMIN_SECRET'])"
        "POSTGRES_APP_SECRET=$($secretsMap['POSTGRES_APP_SECRET'])"
        "SCHEMA_MIGRATOR_SECRET=$($secretsMap['SCHEMA_MIGRATOR_SECRET'])"
        "BACKUP_ROLE_SECRET=$($secretsMap['BACKUP_ROLE_SECRET'])"
        "HAPPYCITY_RUNTIME_CONNECTION=$($secretsMap['HAPPYCITY_RUNTIME_CONNECTION'])"
    )
    [System.IO.File]::WriteAllText($envFile, ($envLines -join "`n") + "`n", (New-Object System.Text.UTF8Encoding($false)))

    Write-Host "1/4 Generating idempotent EF Core migration script..." -ForegroundColor Cyan
    & dotnet restore HappyAlmere_V0.1_MVC/src/HappyCity.Infrastructure/HappyCity.Infrastructure.csproj | Out-Null
    Assert-ExitZero 'dotnet restore HappyCity.Infrastructure'
    $env:HAPPYCITY_MIGRATION_CONNECTION = 'Host=127.0.0.1;Port=5432;Database=happyalmere;Username=happy_schema_migrator;SSL Mode=VerifyFull'
    & dotnet ef migrations script --idempotent `
        --project HappyAlmere_V0.1_MVC/src/HappyCity.Infrastructure `
        --context HappyCityDbContext `
        --output $migrationFile | Out-Null
    Assert-ExitZero 'dotnet ef migrations script --idempotent'

    Write-Host "2/4 Starting isolated PostgreSQL 18 (VerifyFull TLS)..." -ForegroundColor Cyan
    & docker compose -p $projectName --env-file $envFile -f $composeFile up -d --wait postgres
    Assert-ExitZero 'docker compose up postgres'

    Write-Host "3/4 Bootstrapping roles, applying migrations (including HA-005-A Dutch FTS) & grants..." -ForegroundColor Cyan
    $bootstrapSql = @"
DO `$`$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'happy_owner') THEN
    CREATE ROLE happy_owner NOLOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
  END IF;
END `$`$;
SELECT format('CREATE ROLE happy_schema_migrator LOGIN PASSWORD %L NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS', :'migrator_secret')
WHERE NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'happy_schema_migrator') \gexec
SELECT format('CREATE ROLE happy_backup LOGIN PASSWORD %L NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS', :'backup_secret')
WHERE NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'happy_backup') \gexec
GRANT happy_owner TO happy_schema_migrator;
REVOKE ALL ON DATABASE happyalmere FROM PUBLIC;
GRANT CONNECT ON DATABASE happyalmere TO happy_owner, happy_schema_migrator, happy_app, happy_backup;
GRANT CREATE ON DATABASE happyalmere TO happy_owner;
REVOKE CREATE ON SCHEMA public FROM PUBLIC;
GRANT USAGE, CREATE ON SCHEMA public TO happy_owner;
GRANT USAGE ON SCHEMA public TO happy_app, happy_backup;
ALTER DEFAULT PRIVILEGES FOR ROLE happy_owner IN SCHEMA public REVOKE ALL ON TABLES FROM happy_app, happy_backup;
"@
    $bootstrapSql | & docker exec -i $pgContainer psql -X -v ON_ERROR_STOP=1 -U happy_migrator -d happyalmere `
        --set="migrator_secret=$($secretsMap['SCHEMA_MIGRATOR_SECRET'])" `
        --set="backup_secret=$($secretsMap['BACKUP_ROLE_SECRET'])" | Out-Null
    Assert-ExitZero 'bootstrap database roles'

    $migText = [System.IO.File]::ReadAllText($migrationFile)
    $prevPgPass = $env:PGPASSWORD
    try {
        $env:PGPASSWORD = $secretsMap['SCHEMA_MIGRATOR_SECRET']
        $migText | & docker exec -i -e PGPASSWORD -e 'PGOPTIONS=-c role=happy_owner' $pgContainer `
            psql -X -v ON_ERROR_STOP=1 `
            --dbname='host=localhost dbname=happyalmere user=happy_schema_migrator sslmode=verify-full sslrootcert=/var/lib/postgresql-tls/server.crt' | Out-Null
        Assert-ExitZero 'apply idempotent EF migrations'
    }
    finally {
        $env:PGPASSWORD = $prevPgPass
    }

    $grantSql = @"
GRANT USAGE ON SCHEMA catalog, discovery TO happy_app, happy_backup;
GRANT SELECT ON catalog.cities, catalog.categories, catalog.category_translations,
  catalog.city_domains, catalog.districts, catalog.locations, discovery.discovery_entries
  TO happy_app, happy_backup;
GRANT SELECT ON public."__EFMigrationsHistory" TO happy_backup;
REVOKE ALL ON public."__EFMigrationsHistory" FROM happy_app;
REVOKE EXECUTE ON ALL FUNCTIONS IN SCHEMA catalog FROM PUBLIC;
ALTER DEFAULT PRIVILEGES FOR ROLE happy_owner IN SCHEMA catalog, discovery REVOKE ALL ON TABLES FROM happy_app, happy_backup;
"@
    $grantSql | & docker exec -i $pgContainer psql -X -v ON_ERROR_STOP=1 -U happy_migrator -d happyalmere | Out-Null
    Assert-ExitZero 'apply least-privilege grants'

    if ($WithAlmereSeed) {
        Write-Host "    Loading 30 curated Almere R1 draft listings (seed-almere-r1-draft.sql)..." -ForegroundColor Yellow
        $seedSql = [System.IO.File]::ReadAllText((Join-Path $deployDir 'seed-almere-r1-draft.sql'))
        $seedSql | & docker exec -i $pgContainer psql -X -v ON_ERROR_STOP=1 -U happy_migrator -d happyalmere | Out-Null
        Assert-ExitZero 'load 30 Almere draft listings'
    }

    Write-Host "4/4 Building and starting non-root .NET 10 web container on http://localhost:$Port ..." -ForegroundColor Cyan
    & docker compose -p $projectName --env-file $envFile -f $composeFile up -d --build web
    Assert-ExitZero 'docker compose up web'

    $url = "http://localhost:$Port"
    $healthy = $false
    for ($i = 0; $i -lt 25; $i++) {
        $null = (& curl.exe -fsS -H 'Host: localhost' "http://127.0.0.1:$Port/healthz" 2>$null)
        if ($LASTEXITCODE -eq 0) { $healthy = $true; break }
        Start-Sleep -Seconds 1
    }
    if (-not $healthy) { throw "Web preview did not become healthy on $url/healthz." }

    $count = (& docker exec $pgContainer psql -X -qAt -U happy_migrator -d happyalmere -c 'SELECT count(*) FROM discovery.discovery_entries WHERE is_published;').Trim()
    Write-Host ""
    Write-Host "=== HappyAlmere Local Preview READY ===" -ForegroundColor Green
    Write-Host "URL:             $url" -ForegroundColor Green
    Write-Host "Published items: $count" -ForegroundColor Green
    Write-Host "Try Dutch FTS:   $url/?q=wandelen   |   $url/?q=kringloop   |   $url/?category=eten" -ForegroundColor Cyan
    Write-Host "To stop cleanly: .\scripts\Start-LocalPreview.ps1 -Stop" -ForegroundColor DarkGray
}
finally {
    Pop-Location
}
