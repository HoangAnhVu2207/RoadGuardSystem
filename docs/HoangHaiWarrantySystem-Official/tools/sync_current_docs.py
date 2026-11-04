"""Explicit documentation refresh from a disconnected, source-built capture.

Does not alter the runtime/handoff contract. Canonical IDs come from the retained
FE catalog; unknown pairs and changed existing ID pairs fail closed.
"""
from pathlib import Path
import argparse
import hashlib
import json
import re
import subprocess
import yaml
from route_inventory import discover

ROOT = Path(__file__).resolve().parents[3]
BE = ROOT / 'docs/RoadGuard-Backend-Official'
PROJECT = ROOT / 'docs/HoangHaiWarrantySystem-Official'
VERBS = {'get','post','put','patch','delete','head','options'}


def load(p): return json.loads(p.read_text(encoding='utf-8-sig'))
def digest(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def write(p, s): p.write_text(s.rstrip() + '\n', encoding='utf-8', newline='\n')
def compact(v): return json.dumps(v, ensure_ascii=False, separators=(',', ':')).replace('|', '&#124;')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--capture', type=Path, required=True)
    args = parser.parse_args()
    spec = load(args.capture / 'runtime-openapi.json')
    head = subprocess.check_output(['git','rev-parse','HEAD'], cwd=ROOT, text=True).strip()
    canonical = load(ROOT / 'deploy/fe-handoff/FE-API-CATALOG.json')['operations']
    ids = {(o['method'],o['path']): o['operationId'] for o in canonical}
    assert len(ids) == len(canonical) == 260
    source = {}
    for f in sorted((ROOT / 'RoadGuardSystem.API/Controllers').glob('*.cs')):
        text = f.read_text(encoding='utf-8-sig')
        for row in discover(text, f.name):
            row.update(source=f.relative_to(ROOT).as_posix(), sha256=digest(f))
            key = row['method'], row['path']
            assert key not in source, key
            source[key] = row
    pairs = {(m.upper(),p) for p,v in spec['paths'].items() for m in v if m in VERBS}
    assert pairs == set(source)
    assert set(ids) - pairs == {('GET','/ready')}
    oldtext = (PROJECT/'04_API-Contracts/02_Full-API-Catalog.md').read_text(encoding='utf-8-sig')
    old = {}
    for m in re.finditer(r'^### ((?:API-\d+|ADD-\d+))\n(.*?)(?=^### (?:API-|ADD-)|\Z)', oldtext, re.M|re.S):
        role = re.search(r'Actor/role/scope:\*\* ([^.\n]+)', m[2])
        route = re.search(r'\*\*`(GET|POST|PUT|PATCH|DELETE|HEAD|OPTIONS) ([^`]+)`\*\*',m[2])
        if role and route:
            assert ids[(route[1],route[2])] == m[1]
            old[m[1]] = role[1]
    manual = {(o['method'],o['route']):o for o in load(ROOT/'tests/RoadGuardSystem.ApiTests/Contracts/swagger-source-expectations.json')}
    banner = f'**CURRENT_VERIFIED — documentation/source discovery, 2026-10-27; BE `{head}` (`huy-review`).**\n\n259 controller operations / 229 paths / 50 controllers. Canonical catalog retains 260 IDs: API-001–API-241 and ADD-01–ADD-19; ADD-19 is conditional operational `/ready`. Source-built Swagger excludes operational minimal endpoints. No runtime/consumer/full-contract acceptance is granted by this inventory.\n'
    rows=[]
    for key, ident in ids.items():
        if key not in source: continue
        row = dict(source[key], id=ident)
        op = spec['paths'][key[1]][key[0].lower()]
        text = (ROOT/row['source']).read_text(encoding='utf-8-sig')
        decl = re.search(r'public (?:sealed )?class', text)
        before = '\n'.join(text.splitlines()[:row['line']-1])
        metadata = 'WebCookieEligible' in text[:decl.start()] or 'WebCookieEligible' in before.rsplit('}',1)[-1]
        # Preserve the selector semantics, including the restricted defect family.
        p=key[1]
        legacy = p == '/api/v1/me' or any(p == x or p.startswith(x+'/') for x in ('/api/v1/profile','/api/v1/reports','/api/v1/cases','/api/v1/auth/change-password')) or any(x in p for x in ('/candidate-decisions','/candidates','/labels'))
        if p.startswith('/api/v1/notifications'): legacy=key[0]=='GET' or (key[0]=='POST' and p.endswith('/read'))
        if p=='/api/v1/me/inspection-tasks': legacy=key[0]=='GET'
        if '/defects' in p:
            legacy=bool(re.fullmatch(r'/api/v1/projects/\{projectId\}/defects(?:/\{defectId\}(?:/(?:assessments|verification-decisions))?)?',p))
        security = op.get('security',[])
        role=old.get(ident,'')
        if row['controller']=='DiscoveryCollectionsController':
            role='Supervisor, PM, Operator' if row['action'] in ('SurveyTasks','ProcessingJobs') else 'Supervisor, PM, Crew' if row['action'] in ('OfflineDevices','OfflinePackages','OfflineGrants') else 'Supervisor, PM'
        elif row['controller']=='NotificationSupervisorController': role='PM'
        elif row['controller']=='PublicPublicationsController': role='Anonymous/credential' if row['action']=='Read' else 'PM'
        elif ident=='ADD-12': role='Supervisor, PM'
        assert role, ident
        row.update(role=role,cookieAttribute=metadata,cookieLegacy=legacy,
                   authentication='Cookie eligibility metadata='+str(metadata).lower()+', legacy selector='+str(legacy).lower()+'; authentication alternatives and CSRF are action-specific in OpenAPI and source',
                   parameters=op.get('parameters',[]),request=op.get('requestBody'),responses=op.get('responses',{}),security=security,
                   traceStatus='CURRENT_VERIFIED_STATIC; runtime NOT_RUN in this docs task')
        row['sourceEvidence'] = []
        policy=manual.get(key)
        row['sourcePolicy'] = (policy or {}).get('role', 'Follow linked current resource guard; no additional authority inferred')
        for evidence in policy.get('sources',[]) if policy else []:
            p=ROOT/evidence['file']; actual=p.read_text(encoding='utf-8-sig').replace('\r\n','\n')
            excerpt=evidence['evidence'].replace('\r\n','\n')
            assert excerpt in actual, (ident,evidence['file'],evidence['symbol'])
            row['sourceEvidence'].append({'path':evidence['file'],'symbol':evidence['symbol'],
                'line':actual[:actual.index(excerpt)].count('\n')+1,'sha256':digest(p)})
        rows.append(row)
    rows.sort(key=lambda x: list(ids.values()).index(x['id']))
    def detail(row, backend=False):
        ident=row['id']; op=spec['paths'][row['path']][row['method'].lower()]
        loc=f"[{row['controller']}.{row['action']}](../../../{row['source']}#L{row['line']})"
        title=f"## {ident} — {row['method']} {row['path']}" if backend else f"### {ident}\n\n**`{row['method']} {row['path']}`**"
        seams='; '.join(f"[{e['symbol']}](../../../{e['path']}#L{e['line']})" for e in row['sourceEvidence'][:6]) or 'Use linked current controller and integration notes; no transitive proof inferred'
        return title+'\n\n'+f"- **Actor/role/scope:** {row['role']}. Current resource and per-row authority apply; role lists do not grant every transition.\n- **Source policy qualifier:** {row['sourcePolicy']}.\n- **Controller/source/SHA:** {loc}; `{row['sha256']}`.\n- **Current source seams:** {seams}. Full source pins in normalized source map; static evidence is not executed coverage.\n- **Authentication/security declaration:** {row['authentication']}.\n- **Current operation description:** {op.get('description','Controller declaration; follow service/repository for source admission and durable effects.')}\n- **Parameters (including auth/concurrency headers):** `{compact(row['parameters'])}`\n- **Request:** `{compact(row['request'])}`\n- **Responses/status/media/headers/errors:** `{compact(row['responses'])}`\n- **Security alternatives:** `{compact(row['security'])}`\n- **Acceptance:** CURRENT_VERIFIED static metadata only; API030 NOT_VERIFIED; external/native/FE/production acceptance PENDING. Existing historical tests are not automatically current acceptance.\n"
    operational='''### ADD-19

**`GET /ready`**

- **Actor/role/scope:** Operational. Only mapped when Handoff:Enabled=true.
- Source: [HandoffBootstrap.MapReadiness](../../../RoadGuardSystem.API/Extensions/HandoffBootstrap.cs). SQL connectivity, private bucket and test-mail checks: 200 with status/database/privateStorage/testMail; dependency error 503. No current deployment claim from this source check. Not a controller API and not emitted in this Swagger capture.
'''
    write(PROJECT/'04_API-Contracts/02_Full-API-Catalog.md','# Full Backend API catalog — 260 canonical operations\n\n'+banner+'\n'+ '\n'.join(detail(r) for r in rows)+'\n'+operational)
    write(BE/'03_API_Contracts/02_Endpoint_Catalog.md','# Endpoint catalog\n\n'+banner+'\n'+'\n'.join(detail(r,True) for r in rows)+'\n'+operational.replace('### ADD','## ADD'))
    write(BE/'03_API_Contracts/openapi.backend.yaml',yaml.safe_dump(spec, allow_unicode=True,sort_keys=False))
    write(BE/'10_Traceability/api-source-map.json',json.dumps({'baseline':head,'evidence':'CURRENT_VERIFIED_STATIC','operations':rows},indent=2,ensure_ascii=False))
    matrix=['# API to source matrix\n',banner,'| ID | Method/path | Actor | Source | SHA256 |','|---|---|---|---|---|']
    for r in rows: matrix.append(f"| {r['id']} | `{r['method']} {r['path']}` | {r['role']} | [{r['controller']}.{r['action']}](../../../{r['source']}#L{r['line']}) | `{r['sha256']}` |")
    matrix.append('\nADD-19: [conditional operational readiness](../../../RoadGuardSystem.API/Extensions/HandoffBootstrap.cs). Static discovery does not prove transitive call-path coverage.')
    write(BE/'10_Traceability/02_API_to_Source_Matrix.md','\n'.join(matrix))
    cross=['# Consumer API crosswalk\n',banner,'| API | Method/path | Client | Actor | Guard |','|---|---|---|---|---|']
    for r in rows: cross.append(f"| [{r['id']}](02_Full-API-Catalog.md#{r['id'].lower()}) | `{r['method']} {r['path']}` | Source locator; approved screen mapping required | {r['role']} | Current controller/service/resource guard, no blanket UI authority |")
    cross.append('| [ADD-19](02_Full-API-Catalog.md#add-19) | `GET /ready` | Operational | Operational | Conditional Handoff:Enabled only |')
    write(PROJECT/'04_API-Contracts/09_Consumer-API-Crosswalk.md','\n'.join(cross))
    schemas=['# DTO and schema reference\n',banner, 'Exact Swagger facets below; absent nullable is not nullable=true. Implementation annotations and manual source-backed operation filters can add constraints; no accepted runtime examples are invented.\n']
    for name,schema in sorted(spec['components']['schemas'].items()):
        schemas += ['## Schema '+name,'```json',json.dumps(schema,indent=2,ensure_ascii=False),'```','']
    write(PROJECT/'04_API-Contracts/03_DTO-and-Schema-Reference.md','\n'.join(schemas))
    # Full raw hashes, not Git blob IDs. Exclude build products and generated docs
    # to avoid a self-hash cycle. Historical Roslyn inventory remains historical.
    files={p.relative_to(ROOT).as_posix():digest(p) for top in ['RoadGuardSystem.API','RoadGuardSystem.Services','RoadGuardSystem.Repositories','RoadGuardSystem.DTOs','RoadGuardSystem.BusinessObjects','tests'] for p in (ROOT/top).rglob('*.cs') if not {'obj','bin'}.intersection(p.parts)}
    write(BE/'10_Traceability/current-source-hashes.json',json.dumps({'baseline':head,'files':files},indent=2))
    summary={'head':head,'controllers':len({r['controller'] for r in rows}),'controllerOperations':len(rows),'paths':len(spec['paths']),'canonicalOperations':len(ids),'schemas':len(spec['components']['schemas']),'sourceFiles':len(files),'openApiSha256':digest(BE/'03_API_Contracts/openapi.backend.yaml'),'runtimeCaptureSha256':digest(args.capture/'runtime-openapi.json'),'idsPreserved':True}
    write(BE/'10_Traceability/current-discovery.json',json.dumps(summary,indent=2))
    print(json.dumps(summary))


if __name__=='__main__': main()
