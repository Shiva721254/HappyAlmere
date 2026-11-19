# Dot-source from repository scripts. Never print connection strings.
$environmentFile = Join-Path $PSScriptRoot '../.env'
if (!(Test-Path $environmentFile)) { throw 'Run scripts/Initialize-LocalDatabase.ps1 first.' }
foreach ($line in [IO.File]::ReadAllLines($environmentFile)) {
    if ([string]::IsNullOrWhiteSpace($line) -or $line.StartsWith('#')) { continue }
    $parts = $line.Split('=', 2)
    if ($parts.Count -ne 2) { throw 'Invalid local environment file.' }
    [Environment]::SetEnvironmentVariable($parts[0], $parts[1], 'Process')
    if ($env:GITHUB_ACTIONS -eq 'true') { Write-Output "::add-mask::$($parts[1])" }
}
