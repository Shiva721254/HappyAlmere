param()
$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')
if (!(Test-Path .env)) {
    $random = [Security.Cryptography.RandomNumberGenerator]::Create()
    $bytes = New-Object byte[] 32
    $random.GetBytes($bytes)
    $adminPassword = [Convert]::ToBase64String($bytes)
    $random.GetBytes($bytes)
    $appPassword = [Convert]::ToBase64String($bytes)
    $random.Dispose()
    $certificate = (Join-Path (Get-Location) 'artifacts/postgres.crt').Replace('\', '/')
    $common = "Host=localhost;Port=55432;Database=happyalmere;SSL Mode=VerifyFull;Root Certificate=$certificate"
    $lines = @(
        "POSTGRES_PASSWORD=$adminPassword"
        "HAPPY_APP_PASSWORD=$appPassword"
        "ConnectionStrings__HappyCity=$common;Username=happy_app;Password=$appPassword"
        "HAPPYCITY_MIGRATION_CONNECTION=$common;Username=happy_migrator;Password=$adminPassword"
    )
    [IO.File]::WriteAllLines((Join-Path (Get-Location) '.env'), $lines)
}
. "$PSScriptRoot/Load-LocalEnvironment.ps1"
# Debugger configuration receives only the least-privilege runtime connection.
[IO.File]::WriteAllText((Join-Path (Get-Location) '.env.runtime'),
    "ConnectionStrings__HappyCity=$env:ConnectionStrings__HappyCity" + [Environment]::NewLine)
New-Item -ItemType Directory -Force artifacts | Out-Null
docker compose up -d --wait postgres
if ($LASTEXITCODE -ne 0) { throw 'PostgreSQL startup failed.' }
docker compose cp postgres:/var/lib/postgresql-tls/server.crt artifacts/postgres.crt
if ($LASTEXITCODE -ne 0) { throw 'Could not copy the public development certificate.' }
dotnet tool restore
if ($LASTEXITCODE -ne 0) { throw 'EF tooling restore failed.' }
dotnet ef database update 20261119000108_InitialDiscovery --project HappyAlmere_V0.1_MVC/src/HappyCity.Infrastructure --configuration Release
if ($LASTEXITCODE -ne 0) { throw 'Migration failed.' }
Write-Output 'Preserved baseline is ready. Run the recovery and isolated forward rehearsal before application tests.'
