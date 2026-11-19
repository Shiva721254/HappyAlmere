param([Parameter(Mandatory=$true)][string]$EnvironmentFile)
$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')
. "$PSScriptRoot/Load-LocalEnvironment.ps1" -EnvironmentFile $EnvironmentFile
# The website inherits only the read-only connection, never migration credentials.
Remove-Item Env:HAPPYCITY_MIGRATION_CONNECTION -ErrorAction SilentlyContinue
Remove-Item Env:POSTGRES_PASSWORD -ErrorAction SilentlyContinue
Remove-Item Env:HAPPY_APP_PASSWORD -ErrorAction SilentlyContinue
dotnet run --project HappyAlmere_V0.1_MVC/src/HappyCity.Web --configuration Release --no-build --launch-profile https
