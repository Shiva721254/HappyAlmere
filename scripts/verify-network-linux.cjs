const assert = require('node:assert/strict');
const { execFileSync } = require('node:child_process');
const fs = require('node:fs');
const os = require('node:os');
const net = require('node:net');

(async () => {
    const [suite, ...values] = process.argv.slice(2);
    assert.match(suite, /^(responsive|functional|rate)$/);
    const ports = values.map(Number);
    assert.equal(ports.length, 2);
    const rows = execFileSync('ss', ['-H', '-ltnp'], { encoding: 'utf8' }).trim().split('\n').map(line => {
        const endpoint = line.trim().split(/\s+/)[3];
        const match = endpoint?.match(/^(.*):(\d+)$/);
        return { address: match?.[1].replace(/^\[|\]$/g, ''), port: Number(match?.[2]),
            owners: [...line.matchAll(/pid=(\d+)/g)].map(value => value[1]) };
    });
    const selected = rows.filter(row => ports.includes(row.port));
    for (const port of ports) assert(selected.some(row => row.port === port), `Missing listener ${port}`);
    const owners = new Set(selected.flatMap(row => row.owners));
    assert(owners.size > 0, 'Socket ownership must be visible to verify every application listener.');
    const owned = rows.filter(row => row.owners.some(owner => owners.has(owner)));
    assert(owned.every(row => ['127.0.0.1', '::1'].includes(row.address)), 'Non-loopback application listener');
    const addresses = [...new Set(Object.values(os.networkInterfaces()).flat()
        .filter(value => value.family === 'IPv4' && !value.internal).map(value => value.address))];
    assert(addresses.length > 0, 'No local non-loopback IPv4 interface available');
    let rejected = 0;
    for (const host of addresses) for (const port of ports) {
        const connected = await new Promise(resolve => {
            const socket = net.connect({ host, port });
            socket.setTimeout(1500);
            socket.once('connect', () => { socket.destroy(); resolve(true); });
            socket.once('error', () => { socket.destroy(); resolve(false); });
            socket.once('timeout', () => { socket.destroy(); resolve(false); });
        });
        assert.equal(connected, false, 'Application reachable through a non-loopback local interface');
        rejected++;
    }
    const result = { suite, result: 'PASS', platform: 'linux',
        listeners: owned.map(row => ({ LocalAddress: row.address, LocalPort: row.port })),
        nonLoopbackInterfacesProbed: addresses.length, nonLoopbackConnectionsRejected: rejected,
        limitation: 'Local process socket inventory and IPv4 interface probes, not public perimeter certification.' };
    fs.mkdirSync('artifacts/r02/browser', { recursive: true });
    fs.writeFileSync(`artifacts/r02/browser/${suite}-network.json`, JSON.stringify(result, null, 2));
    console.log(JSON.stringify(result, null, 2));
})().catch(error => { console.error(error); process.exitCode = 1; });
