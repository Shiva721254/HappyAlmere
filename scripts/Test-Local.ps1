$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')
. "$PSScriptRoot/Load-LocalEnvironment.ps1"
dotnet test HappyAlmere.slnx --configuration Release -m:1 --logger trx
if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
