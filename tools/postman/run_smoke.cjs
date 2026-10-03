// Reuse the canonical collection without logging tokens, signed URLs or bodies.
const fs = require('node:fs');
const path = require('node:path');
const { spawnSync } = require('node:child_process');
const root = path.resolve(__dirname, '../..');
const args = process.argv.slice(2);
const environmentPath = args[0];
if (!environmentPath) throw new Error('Usage: node run_smoke.cjs PRIVATE_ENVIRONMENT.json [folder names...]');
const environment = JSON.parse(fs.readFileSync(environmentPath, 'utf8').replace(/^\uFEFF/, ''));
const values = Object.fromEntries(environment.values.filter(v => v.enabled !== false).map(v => [v.key, v.value]));
if (values.expectedSqlInstance !== '.\\HANHNAV' || values.expectedDatabase !== 'RoadGuardPostmanTest')
  throw new Error('Disposable environment target mismatch');
const url = new URL(values.baseUrl);
if (!['localhost', '127.0.0.1'].includes(url.hostname)) throw new Error('This smoke runner is local only');
const guard = spawnSync('dotnet', [path.join(root, 'tools/RoadGuardSystem.Seeder/bin/Debug/net8.0/RoadGuardSystem.Seeder.dll'),
  '--postman-disposable', '--verify-only'], { env: process.env, encoding: 'utf8' });
if (guard.status !== 0) throw new Error('Live SQL target verification failed; no HTTP sent');
const collection = JSON.parse(fs.readFileSync(path.join(root, 'docs/postman/RoadGuardSystem-V2.postman_collection.json'), 'utf8').replace(/^\uFEFF/, ''));
const requested = args.slice(1).filter(a => a.startsWith('--request=')).map(a => a.slice(10));
const folders = args.slice(1).filter(a => !a.startsWith('--request='));
if (folders.length === 0) folders.push('00 - Preflight');
for (const folder of folders) if (!collection.item.some(item => item.name === folder)) throw new Error('Unknown collection folder: ' + folder);
collection.item = collection.item.filter(item => folders.includes(item.name));
if (requested.length) {
  const found = new Set();
  const select = items => items.flatMap(item => {
    if (item.item) { const children = select(item.item); return children.length ? [{ ...item, item: children }] : []; }
    if (!requested.includes(item.name)) return [];
    found.add(item.name); return [item];
  });
  collection.item = select(collection.item);
  if (requested.some(name => !found.has(name))) throw new Error('Unknown requested leaf in selected folders');
}
let newman;
try { newman = require('newman'); }
catch { throw new Error('Use existing Newman via NODE_PATH or a locally installed package; do not auto-install'); }
newman.run({ collection, environment, reporters: [], timeoutRequest: 30000 }, (error, result) => {
  if (error) { console.error('Newman runner failed before completion'); process.exitCode = 1; return; }
  const report = { runner: 'newman', version: require('newman/package.json').version,
    folders, requested, requests: result.run.stats.requests, assertions: result.run.stats.assertions,
    failures: result.run.failures.map(f => ({ name: f.source?.name, assertion: f.error?.test || f.error?.name })),
    statuses: result.run.executions.map(e => ({ name: e.item.name, status: e.response?.code })) };
  console.log(JSON.stringify(report, null, 2));
  if (report.requests.total === 0 || report.failures.length) process.exitCode = 1;
});
