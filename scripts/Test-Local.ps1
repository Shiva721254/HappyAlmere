param([Parameter(Mandatory=$true)][string]$EnvironmentFile)
$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')
. "$PSScriptRoot/Load-LocalEnvironment.ps1" -EnvironmentFile $EnvironmentFile
dotnet test HappyAlmere.slnx --configuration Release -m:1 --logger trx
if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
