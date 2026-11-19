const { chromium } = require('playwright');
const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
const https = require('node:https');
const base = 'https://localhost:7243';
const output = path.resolve('artifacts/r02/browser');
fs.mkdirSync(output, { recursive: true });
const results = { timestamp: new Date().toISOString(), mode: process.argv[2] || 'functional', checks: [], layouts: [], timings: [] };
function check(name, actual, expected) {
    const passed = actual === expected;
    results.checks.push({ name, actual, expected, status: passed ? 'PASS' : 'FAIL' });
}
(async () => {
    const browser = await chromium.launch({ headless: true });
    try {
        results.browser = browser.version();
        const context = await browser.newContext({ ignoreHTTPSErrors: false });
        const page = await context.newPage();
        const errors = [];
        page.on('pageerror', e => errors.push(e.message));
        if (results.mode === 'rate') {
            const statuses = [];
            for (let i = 0; i < 120; i++) statuses.push((await context.request.get(base)).status());
            check('First 120 discovery requests succeed', statuses.every(s => s === 200), true);
            const second = await browser.newContext({ ignoreHTTPSErrors: false });
            const blocked = await second.request.get(base);
            check('Separate context on same transport peer shares budget', blocked.status(), 429);
            results.rateHeaders = blocked.headers();
            check('429 supplies bounded Retry-After', Number(blocked.headers()['retry-after']) >= 1 && Number(blocked.headers()['retry-after']) <= 60, true);
            check('Privacy remains available', (await second.request.get(base + '/Home/Privacy')).status(), 200);
            check('429 CSP present', !!blocked.headers()['content-security-policy'], true);
            await second.close();
        } else {
            const routes = ['/', '/Home/Business', '/Home/Privacy', '/?q=does-not-exist'];
            for (const width of [320,375,430,768,1024,1440,1920]) {
                await page.setViewportSize({ width, height: 900 });
                for (const route of routes) {
                    const response = await page.goto(base + route);
                    check(width + ' ' + route + ' status', response.status(), 200);
                    const geometry = await page.evaluate(() => ({
                        width: window.innerWidth, scrollWidth: document.documentElement.scrollWidth,
                        overflowing: [...document.querySelectorAll('main *,header *,footer *')]
                            .filter(e => { const r=e.getBoundingClientRect(); return r.width && (r.right>innerWidth+1 || r.left < -1); })
                            .map(e => e.tagName + '.' + e.className).slice(0,10)
                    }));
                    results.layouts.push({ route, ...geometry });
                    check(width + ' ' + route + ' no overflow', geometry.scrollWidth <= width, true);
                    if (route === '/') {
                        check(width + ' cards', await page.locator('.card').count(), 6);
                        check(width + ' stylesheet loaded', await page.locator('body').evaluate(e => getComputedStyle(e).margin), '0px');
                        await page.screenshot({ path: path.join(output, 'home-' + width + '.png'), fullPage: true });
                        await page.keyboard.press('Tab');
                        check(width + ' skip link focused', await page.locator('.skip').evaluate(e => e === document.activeElement), true);
                        await page.keyboard.press('Enter');
                        check(width + ' skip target', new URL(page.url()).hash, '#main');
                    }
                }
                await page.goto(base + '/?category=' + 'W'.repeat(30));
                const scrollWidth = await page.evaluate(() => document.documentElement.scrollWidth);
                results.layouts.push({ route: 'valid-boundary-category-30W', width, scrollWidth });
                check(width + ' boundary category no overflow', scrollWidth <= width, true);
                if (scrollWidth > width) await page.screenshot({path: path.join(output, 'overflow-' + width + '.png'), fullPage: true});
            }
            await page.goto(base);
            const links = await page.locator('a[href]').evaluateAll(elements =>
                [...new Set(elements.map(e=>e.getAttribute('href')))].filter(h=>!h.startsWith('#')));
            for (const href of links) {
                check('Navigation ' + href, (await page.goto(new URL(href, base).href)).status(), 200);
            }
            results.navigationDestinations = links;
            await page.goto(base);
            await page.getByLabel('Wat zoek je vandaag?').fill('familie');
            await page.getByRole('button', { name: 'Zoeken' }).click();
            check('Form search family result', await page.locator('.card').count(), 1);
            check('Form search expected title', await page.locator('.card h3').innerText(), 'Een familiedag plannen');
            await page.goto(base + '/?category=eten');
            await page.getByLabel('Wat zoek je vandaag?').fill('lunch');
            await page.getByRole('button', { name: 'Zoeken' }).click();
            check('Form retains category', new URL(page.url()).searchParams.get('category'), 'eten');
            check('Combined filter result', await page.locator('.card').count(), 1);

            const xss = '\"><img src=x onerror=alert(1)>';
            await page.goto(base + '/?q=' + encodeURIComponent(xss) + '&category=' + encodeURIComponent('<b>test</b>'));
            check('Search payload round-trips as data', await page.locator('#q').inputValue(), xss);
            check('No injected image', await page.locator('img').count(), 0);
            check('Category markup encoded', await page.locator('main b').count(), 0);
            for (const [query, expected] of [
                ['q='+'x'.repeat(100),200], ['q='+'x'.repeat(101),400],
                ['category='+'x'.repeat(30),200],['category='+'x'.repeat(31),400],
                ['q=%25',200],['q=%00',400],['category=unknown',200]
            ]) {
                const response = await context.request.get(base + '/?' + query);
                check('Input ' + query.slice(0,20) + ' length ' + query.length, response.status(), expected);
            }
            for (const [route, expected] of [['/',200],['/css/site.css',200],['/not-found',404],['/Home/Error',500],['/.env',404],['/appsettings.json',404],['/test/throw',404],['/test/unsafe',404],['/test/token',404]]) {
                const response = await context.request.get(base + route);
                check('HTTP route ' + route, response.status(), expected);
                const headers = response.headers();
                (results.headers ||= {})[route] = headers;
                for (const key of ['content-security-policy','x-content-type-options','x-frame-options','referrer-policy','permissions-policy','x-robots-tag'])
                    check(route + ' header ' + key, !!headers[key], true);
                check(route + ' no server banner', !!headers.server, false);
            }
            const redirect = await context.request.get('http://localhost:5243/?q=familie', {maxRedirects:0});
            check('Live HTTP redirect', redirect.status(), 307);
            check('Live redirect destination', redirect.headers().location, base + '/?q=familie');
            // Validate the actual TLS server (localhost), then test the HTTP Host allowlist.
            // Playwright derives TLS identity from an overridden Host header, preventing this probe.
            const badHost = await new Promise((resolve, reject) => {
                const request = https.get(base, {
                    servername: 'localhost', headers: { Host: 'untrusted.example' }, timeout: 10000
                }, response => {
                    response.resume();
                    response.on('end', () => resolve({ status: response.statusCode, headers: response.headers }));
                });
                request.on('error', reject);
                request.on('timeout', () => request.destroy(new Error('Host validation probe timed out')));
            });
            check('Live host rejection', badHost.status, 400);
            results.badHostHeaders = badHost.headers;
            check('POST to read-only index rejected', (await context.request.post(base, {data:'probe'})).status(), 405);
            check('No browser JS errors', errors.length, 0);

            await page.setViewportSize({width:375,height:900});
            const cdp = await context.newCDPSession(page);
            await cdp.send('Network.enable');
            await cdp.send('Network.setCacheDisabled', {cacheDisabled:true});
            for (let i=0;i<5;i++) {
                await page.goto(base);
                const navigation = await page.evaluate(() => {
                    const n=performance.getEntriesByType('navigation')[0];
                    return {ttfbMs:n.responseStart-n.requestStart,loadMs:n.loadEventEnd-n.startTime,
                        domContentLoadedMs:n.domContentLoadedEventEnd-n.startTime,
                        htmlBytes:n.encodedBodySize,resources:performance.getEntriesByType('resource').map(r=>({name:new URL(r.name).pathname,bytes:r.encodedBodySize}))};
                });
                results.timings.push(navigation);
            }
            const latency = [];
            for(let i=0;i<50;i++) {
                const start=performance.now();
                const response=await context.request.get(base+'/Home/Privacy');
                assert.equal(response.status(),200);
                latency.push(performance.now()-start);
            }
            latency.sort((a,b)=>a-b);
            results.privacyLatency = {samples:50, medianMs:latency[24],p95Ms:latency[47],minMs:latency[0],maxMs:latency[49]};
        }
    } finally { await browser.close(); }
    results.summary = {passed:results.checks.filter(c=>c.status==='PASS').length,failed:results.checks.filter(c=>c.status==='FAIL').length};
    fs.writeFileSync(path.join(output, results.mode + '-results.json'),JSON.stringify(results,null,2));
    console.log(JSON.stringify({summary:results.summary,failures:results.checks.filter(c=>c.status==='FAIL'),timings:results.timings,privacyLatency:results.privacyLatency},null,2));
    if(results.summary.failed) process.exitCode=1;
})().catch(error=>{console.error(error);process.exitCode=2;});
