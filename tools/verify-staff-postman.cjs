// Static and fixture-only validation; no network, credentials or real contacts.
const fs = require('node:fs');
const vm = require('node:vm');
const assert = require('node:assert/strict');
const file = process.argv[2] || 'docs/postman/RoadGuard-Staff-Membership-Read.postman_collection.json';
const collection = JSON.parse(fs.readFileSync(file, 'utf8'));
const requests = collection.item[0].item;
assert.equal(requests.length, 19);
assert(collection.variable.every(v => v.value === ''));
for (const item of requests) {
  assert.equal(item.request.method, 'GET');
  assert(!item.request.body);
  for (const event of item.event || []) new vm.Script(event.script.exec.join('\n'));
}
const select = requests.find(x => x.name.startsWith('03'));
const primary = requests.find(x => x.name.startsWith('08'));
const values = new Map();
const environment = { get: key => values.get(key), set: (key, value) => values.set(key, value), unset: key => values.delete(key) };
function execute(item, body, code = 200) {
  const pm = {
    environment,
    response: { code, json: () => body },
    test: (_name, callback) => { try { callback(); } catch { /* Postman records assertion failure and continues scripts. */ } },
    expect: value => ({ to: { eql: expected => assert.equal(value, expected), be: { an: type => assert.equal(type, Array.isArray(value) ? 'array' : typeof value) } } })
  };
  for (const event of item.event.filter(e => e.listen === 'test')) vm.runInNewContext(event.script.exec.join('\n'), { pm });
}
execute(select, { items: [{ userId: 'fixture-old-pm' }, { userId: 'fixture-next-pm' }] });
assert.equal(values.get('selectedPmUserId'), 'fixture-old-pm');
execute(primary, { primaryProjectManager: { userId: 'fixture-old-pm', membershipVersion: 'fixture-head' } });
assert.equal(values.get('selectedPmUserId'), 'fixture-next-pm');
assert.equal(values.get('currentPrimaryMembershipVersion'), 'fixture-head');
execute(select, { items: [] });
assert.equal(values.get('selectedPmUserId'), '');
assert.equal(values.get('selectedStaffUserId'), '');
values.set('selectedPmUserId', 'stale'); values.set('selectedStaffUserId', 'stale');
execute(select, {}, 403);
assert.equal(values.get('selectedPmUserId'), '');
assert.equal(values.get('selectedStaffUserId'), '');
execute(primary, { primaryProjectManager: null });
assert.equal(values.get('currentPrimaryMembershipVersion'), '');
values.set('currentPrimaryMembershipVersion', 'stale');
execute(primary, {}, 401);
assert.equal(values.get('currentPrimaryMembershipVersion'), '');
console.log('PASS: 19 GET requests, blank variables, script syntax, current-PM exclusion, empty/error selection clearing, null/error head clearing. Fixture-only; no Postman/network execution.');
