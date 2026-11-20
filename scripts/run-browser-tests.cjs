// Windows local and Linux PostgreSQL CI harness. Trust is configured explicitly before running this script.
const { spawn, spawnSync } = require('node:child_process');
const fs = require('node:fs');
const path = require('node:path');
const net = require('node:net');
const { setTimeout: delay } = require('node:timers/promises');

const output = path.resolve('artifacts/r02/browser');
const project = 'HappyAlmere_V0.1_MVC/src/HappyCity.Web';
fs.mkdirSync(output, { recursive: true });

function runtimeEnvironment(source) {
    const keys = new Set([
        'PATH', 'HOME', 'USERPROFILE', 'APPDATA', 'LOCALAPPDATA', 'TEMP', 'TMP', 'TMPDIR',
        'SYSTEMROOT', 'WINDIR', 'COMSPEC', 'PATHEXT', 'PROGRAMFILES', 'PROGRAMFILES(X86)',
        'PROGRAMDATA', 'SYSTEMDRIVE', 'USER', 'USERNAME', 'USERDOMAIN', 'LANG', 'LC_ALL',
        'TERM', 'OS', 'NUMBER_OF_PROCESSORS', 'PROCESSOR_ARCHITECTURE', 'PSMODULEPATH',
        'DOTNET_ROOT', 'DOTNET_ROOT_X64', 'DOTNET_CLI_HOME', 'DOTNET_CLI_TELEMETRY_OPTOUT',
        'NUGET_PACKAGES', 'SSL_CERT_FILE', 'SSL_CERT_DIR', 'PLAYWRIGHT_BROWSERS_PATH',
        'CONNECTIONSTRINGS__HAPPYCITY', 'CONNECTIONSTRINGS__HAPPYCITYIDENTITY',
        'IDENTITYFOUNDATION__ENABLED'
    ]);
    return Object.fromEntries(Object.entries(source).filter(([key]) => keys.has(key.toUpperCase())));
}

function run(command, args, env = runtimeEnvironment(process.env)) {
    const result = spawnSync(command, args, { stdio: 'inherit', env, windowsHide: true });
    if (result.error) throw result.error;
    if (result.status !== 0) throw new Error(`${command} exited ${result.status}`);
}

async function isListening(port, host = '127.0.0.1') {
    return new Promise(resolve => {
        const socket = net.connect({ host, port });
        socket.setTimeout(300);
        socket.once('connect', () => { socket.destroy(); resolve(true); });
        socket.once('error', () => { socket.destroy(); resolve(false); });
        socket.once('timeout', () => { socket.destroy(); resolve(false); });
    });
}

async function waitForPort(port, server) {
    for (let attempt = 0; attempt < 100; attempt++) {
        if (server.exitCode !== null) throw new Error(`Server exited ${server.exitCode}`);
        const ready = await isListening(port);
        if (ready) return;
        await delay(200);
    }
    throw new Error(`Server did not start on loopback port ${port}`);
}

async function suite(name, development, script, args = []) {
    const ports = development ? [7143, 5143] : [7243, 5243];
    // Refuse to reuse an existing listener: each suite needs its own fresh rate budget.
    for (const port of ports) {
        if (await isListening(port) || await isListening(port, '::1'))
            throw new Error(`Port ${port} already has a listener; stop it before running tests.`);
    }
    const env = {
        ...runtimeEnvironment(process.env),
        ASPNETCORE_ENVIRONMENT: development ? 'Development' : 'Production',
        HA_BASE_URL: `https://localhost:${ports[0]}`,
        NODE_EXTRA_CA_CERTS: path.join(output, 'localhost-public.crt')
    };
    const serverArgs = ['run', '--project', project, '-c', 'Release', '--no-build'];
    if (development) serverArgs.push('--launch-profile', 'https');
    else serverArgs.push('--no-launch-profile', '--', '--urls',
        `https://localhost:${ports[0]};http://localhost:${ports[1]}`);
    const log = fs.openSync(path.join(output, `${name}-server.log`), 'w');
    const server = spawn('dotnet', serverArgs, { env, stdio: ['ignore', log, log], windowsHide: true, detached: process.platform !== 'win32' });
    fs.closeSync(log);
    let spawnError;
    server.on('error', error => { spawnError = error; });
    try {
        await waitForPort(ports[0], server);
        if (spawnError) throw spawnError;
        if (process.platform === 'win32')
            run('powershell.exe', ['-NoProfile', '-File', 'scripts/verify-network.ps1',
                '-HttpsPort', String(ports[0]), '-HttpPort', String(ports[1]), '-Suite', name], env);
        else run(process.execPath, ['scripts/verify-network-linux.cjs', name, ...ports.map(String)], env);
        run(process.execPath, [script, ...args], env);
    } finally {
        if (server.pid && server.exitCode === null) {
            // Terminate only the process tree created by this harness.
            if (process.platform === 'win32')
                spawnSync('taskkill.exe', ['/PID', String(server.pid), '/T', '/F'],
                    { stdio: 'ignore', windowsHide: true });
            else process.kill(-server.pid, 'SIGTERM');
            for (let attempt = 0; attempt < 50; attempt++) {
                if (!await isListening(ports[0]) && !await isListening(ports[1])) break;
                if (attempt === 49) throw new Error('Owned test server did not stop.');
                await delay(100);
            }
        }
    }
}

module.exports = { runtimeEnvironment };
if (require.main === module) (async () => {
    if (!['win32', 'linux'].includes(process.platform)) throw new Error('Supported hosts: Windows and Linux.');
    run('dotnet', ['dev-certs', 'https', '--check', ...(process.platform === 'win32' ? ['--trust'] : [])]);
    // Linux CI provisions OS/NSS trust; both Node HTTPS and Chromium still validate TLS.
    // Without a password/no-password switch this exports the public certificate only.
    run('dotnet', ['dev-certs', 'https', '--export-path', path.join(output, 'localhost-public.crt'), '--format', 'PEM']);
    await suite('responsive', true, 'scripts/verify-browser.cjs');
    await suite('functional', false, 'scripts/verify-security.cjs', ['functional']);
    await suite('rate', false, 'scripts/verify-security.cjs', ['rate']);
    console.log('PASS: responsive, production security, rate limiting and actual network bindings.');
})().catch(error => { console.error(error); process.exitCode = 1; });
