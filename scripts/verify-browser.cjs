// Uses an existing Playwright installation; no browser/runtime dependency is added to the app.
// Run: node scripts/verify-browser.cjs <path-to-playwright-or-playwright-core>
const { chromium } = require(process.argv[2] || 'playwright');
const assert = require('node:assert/strict');
const fs = require('node:fs');

(async () => {
    const browser = await chromium.launch({ headless: true });
    try {
        // Certificate validation remains enabled.
        const context = await browser.newContext();
        const page = await context.newPage();
        const failures = [];
        page.on('pageerror', error => failures.push(error.message));
        page.on('console', message => {
            if (message.type() === 'error' && !message.text().includes('favicon.ico'))
                failures.push(message.text());
        });
        const base = process.env.HA_BASE_URL || 'https://localhost:7143';
        const layouts = [];
        for (const width of [320, 375, 430, 768, 1024, 1440, 1920]) {
            await page.setViewportSize({ width, height: 900 });
            const response = await page.goto(base);
            assert.equal(response.status(), 200);
            assert.equal(await page.locator('.card').count(), 6);
            assert.equal(await page.evaluate(() =>
                document.documentElement.scrollWidth <= window.innerWidth), true, 'Overflow at ' + width);
            await page.keyboard.press('Tab');
            assert.equal(await page.locator('.skip').evaluate(element => element === document.activeElement), true);
            assert.equal(await page.locator('.skip').evaluate(element => element.getBoundingClientRect().left >= 0), true);
            fs.mkdirSync('artifacts', { recursive: true });
            await page.screenshot({ path: 'artifacts/home-' + width + '.png', fullPage: true });
            for (const route of ['/', '/Home/Business', '/Home/Privacy', '/?q=does-not-exist', '/?category=' + 'W'.repeat(30)]) {
                assert.equal((await page.goto(base + route)).status(), 200);
                const geometry = await page.evaluate(() => ({ viewport: innerWidth, document: document.documentElement.scrollWidth }));
                layouts.push({ route, ...geometry });
                assert.equal(geometry.document <= geometry.viewport, true, `Overflow at ${width}: ${route}`);
                if (route.includes('category='))
                    await page.screenshot({ path: `artifacts/boundary-${width}.png`, fullPage: true });
            }
            await page.goto(base);
            await page.getByRole('link', { name: 'Ontdekken', exact: true }).click();
            assert.equal(await page.locator('.card').count(), 2);
            await page.getByLabel('Wat zoek je vandaag?').fill('familie');
            await page.getByRole('button', { name: 'Zoeken' }).click();
            assert.equal(await page.locator('.card').count(), 1);
            await page.goto(base);

        }
        const links = await page.locator('a[href]').evaluateAll(elements =>
            [...new Set(elements.map(element => element.getAttribute('href')))].filter(href => !href.startsWith('#')));
        for (const href of links) {
            const response = await page.goto(new URL(href, base).href);
            assert.equal(response.status(), 200, href);
        }
        await page.goto(base);
        await page.getByLabel('Wat zoek je vandaag?').fill('familie');
        await page.getByRole('button', { name: 'Zoeken' }).click();
        assert.equal(await page.locator('.card').count(), 1);
        assert.match(await page.locator('.card').innerText(), /familiedag/);
        await page.getByLabel('Wat zoek je vandaag?').fill('does-not-exist');
        await page.getByRole('button', { name: 'Zoeken' }).click();
        assert.equal(await page.locator('.card').count(), 0);
        assert.equal(await page.getByText('Geen resultaten. Probeer een andere zoekterm.').count(), 1);
        await page.goto(base + '/?category=eten');
        await page.getByLabel('Wat zoek je vandaag?').fill('lunch');
        await page.getByRole('button', { name: 'Zoeken' }).click();
        assert.equal(new URL(page.url()).searchParams.get('category'), 'eten');
        assert.equal(await page.locator('.card').count(), 1);
        assert.deepEqual(failures, []);
        fs.writeFileSync('artifacts/responsive-results.json', JSON.stringify({ layouts, browser: browser.version(), result: 'PASS' }, null, 2));
        console.log(JSON.stringify({ result: 'PASS', widths: [320,375,430,768,1024,1440,1920], links: links.length,
            checks: ['trusted HTTPS', 'no horizontal overflow', 'keyboard skip link', 'navigation',
                'search', 'empty state', 'category retained', 'no browser errors'] }, null, 2));
    } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
