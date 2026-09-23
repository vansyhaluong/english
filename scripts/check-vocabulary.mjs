// Run against a local development instance: node scripts/check-vocabulary.mjs [base-url]
import assert from 'node:assert/strict';
const base = process.argv[2] || 'http://127.0.0.1:5098';
const cases = [
    ['/vocabulary', 12, '14 bộ thẻ'],
    ['/vi/vocabulary', 12, '14 bộ thẻ'],
    ['/vocabulary?page=2', 2, 'sustainable'],
    ['/vocabulary?level=C2', 1, 'ubiquitous'],
    ['/vocabulary?search=hanh%20ly', 1, 'luggage'],
    ['/vocabulary?search=xyznotfound', 0, 'Chưa tìm thấy'],
    ['/vocabulary?page=999', 2, 'sustainable'],
    ['/vocabulary?page=-4&level=invalid', 12, '14 bộ thẻ'],
    ['/vocabulary?search=%3Cscript%3E', 0, '&lt;script&gt;']
];
for (const [path, count, expected] of cases) {
    const response = await fetch(base + path);
    const html = await response.text();
    assert.equal(response.status, 200, path);
    assert.equal((html.match(/class="vocab-deck"/g) || []).length, count, path);
    assert.ok(html.includes(expected), `${path}: expected ${expected}`);
    const ids = Array.from(html.matchAll(/\bid="([^"]+)"/g), m => m[1]);
    assert.equal(new Set(ids).size, ids.length, `Duplicate IDs: ${path}`);
    console.log('PASS', path);
}
for (const path of ['/', '/css/vocabulary.css', '/js/vocabulary.js', '/images/night.jpg']) {
    assert.equal((await fetch(base + path)).status, 200, path);
    console.log('PASS', path);
}
const home = await (await fetch(base + '/')).text();
assert.match(home, /href="\/vocabulary" class="showcase-link">\s*Mở kho từ vựng/);
console.log('PASS homepage vocabulary CTA');
