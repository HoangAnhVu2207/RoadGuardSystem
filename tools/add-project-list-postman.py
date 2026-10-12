"""Add list-only requests to the existing local manual pack, preserving old requests/environments."""
import copy
import hashlib
import json
import subprocess
from pathlib import Path
import yaml

root = Path(r'C:\Users\HoangAnhVu\Downloads\testing')
collection_path = root / 'RoadGuard-ANH-Manual.postman_collection.json'
collection = json.loads(collection_path.read_text(encoding='utf-8-sig'))
old_items = copy.deepcopy(collection['item'])
folder_name = 'GET Projects — Danh sách dự án'
assert not any(f['name'] == folder_name for f in old_items), 'Folder already exists; preserve user changes.'
scope_note = '''API mới LOCAL-GET-PROJECTS (không chiếm SWG ID trong inventory116). Backend mới cần chạy tại baseUrl đã chọn.
Không body. Login H00 của role tương ứng trước, giữ environment/token đang dùng.
Supervisor phải thấy mọi dự án (kể cả CLOSED); PM chỉ membership ACTIVE, đúng role, có hiệu lực.
Operator/Crew cần đồng thời membership hợp lệ và assignment hiện tại cho chính user, task không bị hủy/rejected/reassigned-away; completed vẫn thấy nếu membership còn hiệu lực.
Trước kiểm scope, ghi project nào của actor phải có/không có theo phân công thực tế. 200/items=[] có thể đúng nếu chưa có assignment; HTTP200 riêng lẻ không chứng minh scope đủ.
Response chỉ items/nextCursor/asOf và 6 field cơ bản của mỗi project; không gọi work-package Operator/Crew.
Cursor lấy nextCursor của response trước, URL-encode; giữ filters khi tiếp tục. Summary chỉ status/số items, không token/name/ID cá nhân.'''

cases = [
    ('Danh sách dự án — Supervisor', 'supervisorToken', '', 200),
    ('Danh sách dự án — PM', 'pmToken', '', 200),
    ('Danh sách dự án — Operator', 'operatorToken', '', 200),
    ('Danh sách dự án — Crew', 'crewToken', '', 200),
    ('Danh sách dự án — Reporter bị từ chối', 'reporterToken', '', 403),
    ('Không đăng nhập', None, '', 401),
    ('Tìm kiếm và phân trang', 'supervisorToken', '?search=TEST_ONLY&status=ACTIVE&limit=1', 200),
    ('Query sai — limit vượt 100', 'supervisorToken', '?limit=101', 400),
]
requests = []
for seq, (name, token, query, expected) in enumerate(cases, 1):
    description = scope_note
    if token == 'reporterToken':
        description += '\nPrerequisite riêng: reporterToken lấy từ login Reporter thật rồi thêm vào environment đang dùng. Không có token thì nhận401, chưa test được403.'
    code = f'''const expected = {expected};
pm.test('HTTP ' + expected, () => pm.expect(pm.response.code).to.eql(expected));
let body = null;
try {{ body = pm.response.json(); }} catch (_) {{}}
if (expected === 200 && pm.response.code === 200) {{
    pm.test('Envelope items/nextCursor/asOf', () => {{
        pm.expect(Object.keys(body).sort()).to.eql(['asOf','items','nextCursor'].sort());
        pm.expect(Array.isArray(body.items)).to.eql(true);
        pm.expect(body.nextCursor === null || typeof body.nextCursor === 'string').to.eql(true);
        pm.expect(Number.isNaN(Date.parse(body.asOf))).to.eql(false);
    }});
    pm.test('Only basic project fields; no duplicate IDs', () => {{
        const fields = ['projectId','projectCode','name','status','primaryProjectManagerUserId','primaryProjectManagerName'].sort();
        for (const item of body.items) {{
            pm.expect(Object.keys(item).sort()).to.eql(fields);
            pm.expect(['PLANNING','ACTIVE','CLOSED']).to.include(item.status);
            pm.expect(typeof item.projectId).to.eql('string');
            pm.expect(typeof item.projectCode).to.eql('string');
            pm.expect(typeof item.name).to.eql('string');
            pm.expect(item.primaryProjectManagerUserId === null || typeof item.primaryProjectManagerUserId === 'string').to.eql(true);
            pm.expect(item.primaryProjectManagerName === null || typeof item.primaryProjectManagerName === 'string').to.eql(true);
        }}
        pm.expect(new Set(body.items.map(x => x.projectId)).size).to.eql(body.items.length);
    }});
}} else if (pm.response.code === expected) {{
    pm.test('Problem convention', () => {{
        pm.expect(pm.response.headers.get('Content-Type')).to.include('application/problem+json');
        pm.expect(typeof body.code).to.eql('string');
        pm.expect(typeof body.correlationId).to.eql('string');
    }});
}}
console.log('ROADGUARD_PROJECT_LIST_SUMMARY ' + JSON.stringify({{http:pm.response.code,itemCount:Array.isArray(body?.items)?body.items.length:null,scope:'NEEDS_REAL_FIXTURE_VERIFICATION'}}));'''
    request = {'method': 'GET', 'auth': {'type': 'bearer', 'bearer': [{'key': 'token', 'value': '{{' + token + '}}', 'type': 'string'}]} if token else {'type': 'noauth'},
               'header': [{'key': 'Accept', 'value': 'application/json'}], 'url': '{{baseUrl}}/api/v1/projects' + query, 'description': description}
    requests.append({'name': name, 'request': request, 'protocolProfileBehavior': {'disableCookies': True}, 'event': [{'listen': 'test', 'script': {'type': 'text/javascript', 'exec': code.splitlines()}}]})
collection['item'].append({'name': folder_name, 'description': scope_note, 'item': requests})
assert collection['item'][:-1] == old_items
collection_path.write_text(json.dumps(collection, indent=2, ensure_ascii=False) + '\n', encoding='utf-8')

ui = json.loads(Path(r'C:\Users\HoangAnhVu\AppData\Roaming\bruno\ui-state-snapshot.json').read_text(encoding='utf-8'))
native_collection = next(Path(c['pathname']) for c in ui['collections'] if 'RoadGuard ANH Manual' in c.get('pathname', ''))
destinations = [native_collection / 'GET Projects', root / 'GET-Projects-Bruno-Requests']
for destination in destinations:
    assert not destination.exists(), 'Existing native folder preserved; manual merge needed.'
    destination.mkdir()
    (destination / 'folder.yml').write_text(yaml.safe_dump({'info': {'name': folder_name, 'type': 'folder', 'seq': 9}, 'request': {'auth': 'inherit'}}, allow_unicode=True, sort_keys=False), encoding='utf-8')
    for seq, item in enumerate(requests, 1):
        request = item['request']
        code = '\n'.join(item['event'][0]['script']['exec'])
        code = code.replace('pm.test(', 'test(').replace('pm.expect(', 'expect(').replace('pm.response.json()', 'res.getBody()').replace('pm.response.code', 'res.getStatus()').replace("pm.response.headers.get(", 'res.getHeader(')
        token = cases[seq-1][1]
        native = {'info': {'name': item['name'], 'type': 'http', 'seq': seq},
                  'http': {'method': 'GET', 'url': request['url'], 'headers': [{'name': 'Accept', 'value': 'application/json'}],
                           'auth': {'type': 'bearer', 'token': '{{' + token + '}}'} if token else {'type': 'none'}},
                  'runtime': {'scripts': [{'type': 'after-response', 'code': code}]}, 'docs': request['description']}
        if not token:
            del native['http']['auth']
        (destination / f'{seq:02d}-projects.yml').write_text(yaml.safe_dump(native, allow_unicode=True, sort_keys=False), encoding='utf-8')

state_path = root / 'TEST-STATE.json'
state = json.loads(state_path.read_text(encoding='utf-8-sig'))
old_operations = copy.deepcopy(state['operations'])
assert len(old_operations) == 116
assert not any(o.get('operation_id') == 'LOCAL-GET-PROJECTS' for o in state.get('added_scope', []))
state.setdefault('added_scope', []).append({'operation_id': 'LOCAL-GET-PROJECTS', 'method': 'GET', 'route': '/api/v1/projects',
    'request_readiness': 'PREPARED', 'runtime_status': 'NOT_TESTED', 'manual_runtime_status': 'NOT_TESTED',
    'evidence_note': 'Added scope separate from original116. SQL-backed automated tests recorded in planning/development/GET-PROJECTS.md; manual runner not executed.',
    'request_names': [x[0] for x in cases]})
state['revision'] += 1
assert state['operations'] == old_operations
state_path.write_text(json.dumps(state, indent=2, ensure_ascii=False) + '\n', encoding='utf-8')
(root / 'GET-PROJECTS-START.md').write_text('''# Test danh sách dự án

Bruno: collection **RoadGuard ANH Manual — v1.0.0** đã được thêm folder **GET Projects — Danh sách dự án**. Nếu chưa xuất hiện, mở lại collection. Giữ environment hiện tại; chọn baseUrl backend mới. Chạy login Supervisor H00 rồi mở **Danh sách dự án — Supervisor** và bấm Send. Không cần body/projectId/operationId và không cần import lại environment.

Postman: import lại duy nhất `RoadGuard-ANH-Manual.postman_collection.json` trong thư mục testing, chọn environment đang dùng. Mở **GET Projects — Danh sách dự án → Danh sách dự án — Supervisor**. Token dùng các biến đã có supervisorToken/pmToken/operatorToken/crewToken. Chỉ case Reporter cần thêm reporterToken từ login Reporter thật; để trống chưa test được403.

Endpoint: GET `{{baseUrl}}/api/v1/projects`. Query tùy chọn: search theo code/name, status=PLANNING/ACTIVE/CLOSED, limit=1..100 (mặc định20), cursor từ nextCursor response trước (URL encode, giữ filters). Response `{items,nextCursor,asOf}`, không totalCount/page. Case tìm kiếm/phân trang đã hiển thị trực tiếp query `?search=TEST_ONLY&status=ACTIVE&limit=1`.

Supervisor thấy tất cả, kể cả CLOSED; PM thấy membership ACTIVE đúng role/còn hiệu lực. Operator/Crew cần thêm assignment hiện tại cho chính user, task hợp lệ (completed vẫn thấy); cancelled/rejected/assignment cũ không cấp quyền. Reporter403, noauth401, query sai400. Không có scope hợp lệ trả200/items=[]. Một200 không chứng minh scope đủ: đối chiếu project phải thấy/không thấy với phân công thật. Không gọi work-package Crew/Operator để kiểm list.

Gửi lại status và items đã che thông tin dự án nhạy cảm. Không gửi token/environment/password. Operation mới LOCAL-GET-PROJECTS nằm ngoài inventory116, chưa đánh dấu manual runtime PASS.
''', encoding='utf-8')
for destination in destinations:
    for path in destination.glob('*.yml'):
        d = yaml.safe_load(path.read_text(encoding='utf-8'))
        for script in d.get('runtime', {}).get('scripts', []):
            subprocess.run(['node', '-e', 'new (require("vm").Script)(process.argv[1]);', script['code']], check=True, capture_output=True)
for item in requests:
    code = '\n'.join(item['event'][0]['script']['exec'])
    subprocess.run(['node', '-e', 'new (require("vm").Script)(process.argv[1]);', code], check=True, capture_output=True)
print(json.dumps({'requests_added': len(requests), 'old_requests_preserved': True, 'original_operation_count': len(state['operations']),
                  'environments_modified': False, 'native_folder': str(destinations[0]), 'manual_runtime': 'NOT_TESTED'}, ensure_ascii=False))
