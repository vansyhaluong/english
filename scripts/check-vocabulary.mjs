// P2 acceptance uses SQL/HTTP fixtures, not the old mock catalog.
// Set ConnectionStrings__DefaultConnection to English_P2_Test and start the test app on port 5112.
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
const baseUrl = process.argv[2];
if (!baseUrl || !process.env.ConnectionStrings__DefaultConnection) {
    console.error('Usage: node scripts/check-vocabulary.mjs <test-app-base-url> (test DB environment override required)');
    process.exit(2);
}
const result = spawnSync('powershell', ['-NoProfile', '-File', fileURLToPath(new URL('./check-p2.ps1', import.meta.url)), '-BaseUrl', baseUrl], { stdio: 'inherit', env: process.env });
if (result.error) console.error(result.error.message);
process.exit(result.status ?? 2);
