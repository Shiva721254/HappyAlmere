param([Parameter(Mandatory=$true)][string]$EnvironmentFile)
$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')
. "$PSScriptRoot/Load-LocalEnvironment.ps1" -EnvironmentFile $EnvironmentFile
# Explicit child-process boundary: retain platform paths and only approved runtime configuration.
# New backup/restore/test credentials are denied automatically rather than added to a denylist.
$runtimeKeys = @(
    'PATH','HOME','USERPROFILE','APPDATA','LOCALAPPDATA','TEMP','TMP','TMPDIR','SYSTEMROOT',
    'WINDIR','COMSPEC','PATHEXT','PROGRAMFILES','PROGRAMFILES(X86)','PROGRAMDATA','SYSTEMDRIVE',
    'USER','USERNAME','USERDOMAIN','LANG','LC_ALL','TERM','OS','NUMBER_OF_PROCESSORS',
    'PROCESSOR_ARCHITECTURE','PSMODULEPATH','DOTNET_ROOT','DOTNET_ROOT_X64','DOTNET_CLI_HOME',
    'DOTNET_CLI_TELEMETRY_OPTOUT','NUGET_PACKAGES','SSL_CERT_FILE','SSL_CERT_DIR',
    'CONNECTIONSTRINGS__HAPPYCITY','CONNECTIONSTRINGS__HAPPYCITYIDENTITY','IDENTITYFOUNDATION__ENABLED'
)
foreach ($item in Get-ChildItem Env:) {
    if ($item.Name.ToUpperInvariant() -notin $runtimeKeys) {
        Remove-Item -LiteralPath ("Env:" + $item.Name)
    }
}
dotnet run --project HappyAlmere_V0.1_MVC/src/HappyCity.Web --configuration Release --no-build --launch-profile https
