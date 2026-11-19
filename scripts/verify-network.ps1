param(
    [int]$HttpsPort = 7143,
    [int]$HttpPort = 5143,
    [string]$Suite = 'development'
)
$ErrorActionPreference = 'Stop'
$ports = @($HttpsPort, $HttpPort)
$listeners = @(Get-NetTCPConnection -State Listen | Where-Object { $_.LocalPort -in $ports })
if ($listeners.Count -eq 0) { throw 'No test server listeners found.' }
foreach ($port in $ports) {
    if (-not ($listeners | Where-Object { $_.LocalPort -eq $port })) { throw "Missing listener on $port" }
}
$ownerIds = @($listeners.OwningProcess | Select-Object -Unique)
$allOwned = @(Get-NetTCPConnection -State Listen | Where-Object { $_.OwningProcess -in $ownerIds })
foreach ($listener in $allOwned) {
    if (-not [Net.IPAddress]::IsLoopback([Net.IPAddress]::Parse($listener.LocalAddress))) {
        throw 'Test application has a non-loopback listener.'
    }
}

# Probe this machine's non-loopback IPv4 interfaces, without contacting any external host.
$addresses = @(Get-NetIPAddress -AddressFamily IPv4 | Where-Object {
    $_.AddressState -eq 'Preferred' -and -not [Net.IPAddress]::IsLoopback([Net.IPAddress]::Parse($_.IPAddress))
})
if ($addresses.Count -eq 0) { throw 'No non-loopback interface available; boundary probe cannot be claimed.' }
$rejected = 0
foreach ($address in $addresses) {
    foreach ($port in $ports) {
        $client = [Net.Sockets.TcpClient]::new()
        try {
            $connect = $client.ConnectAsync($address.IPAddress, $port)
            try { $null = $connect.Wait(1500) } catch [AggregateException] { }
            if ($client.Connected) { throw 'Application is reachable through a non-loopback interface.' }
            $rejected++
        } finally { $client.Dispose() }
    }
}
$result = [ordered]@{
    suite = $Suite
    result = 'PASS'
    listeners = @($allOwned | Select-Object LocalAddress, LocalPort)
    nonLoopbackInterfacesProbed = $addresses.Count
    nonLoopbackConnectionsRejected = $rejected
    limitation = 'Local interface probes and process socket inventory; not an external firewall or public deployment test.'
}
$directory = Join-Path $PSScriptRoot '../artifacts/r02/browser'
New-Item -ItemType Directory -Force $directory | Out-Null
$result | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $directory "$Suite-network.json") -Encoding UTF8
$result | ConvertTo-Json -Depth 5
