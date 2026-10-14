#!/usr/bin/env python3
"""Validate Phase 3A C01 screen/API role and Web transport mappings.
Source of truth is the Phase 2C catalog; this validator never grants a role.
"""
from pathlib import Path
import re, json, sys
ROOT=Path(__file__).resolve().parents[4]
WEB=ROOT/'docs/HoangHaiWarrantySystem-Official/05_Web'
CAT=ROOT/'docs/HoangHaiWarrantySystem-Official/04_API-Contracts/02_Full-API-Catalog.md'

def fail(msg):
    print('FAIL:',msg); raise SystemExit(1)
text=CAT.read_text(encoding='utf-8')
entries={}
for m in re.finditer(r'^### (API-\d+)\n(.*?)(?=^### API-|\Z)',text,re.M|re.S):
    body=m.group(2); ident=m.group(1)
    route=re.search(r'\*\*`([^`]+)`\*\*',body)
    role=re.search(r'- \*\*Actor/role/scope:\*\* ([^\.\n]+)',body)
    auth=re.search(r'- \*\*Authentication/security declaration:\*\* (.*)',body)
    if not (route and role and auth): fail(f'{ident} missing catalog metadata')
    entries[ident]={'route':route.group(1),'role':role.group(1),'cookie':('Cookie eligibility metadata=true' in auth.group(1) or 'legacy selector=true' in auth.group(1))}
rows=[]; main=[]
for fn,prefix,actor in [('02_Supervisor-Screen-API-Matrix.md','SUP','Supervisor'),('03_PM-Screen-API-Matrix.md','PM','PM')]:
    s=(WEB/fn).read_text(encoding='utf-8')
    before=s.split('## C01 mapping adjudication',1)[0]
    for line in before.splitlines():
        m=re.match(r'\| \*\*('+prefix+r'-\d+)\*\*',line)
        if m:
            for n in re.findall(r'API-(\d+)',line): main.append((m.group(1),'API-'+n.zfill(3),actor))
    section=s.split('## C01 mapping adjudication',1)
    if len(section)!=2: fail(f'{fn} has no C01 table')
    for line in section[1].splitlines():
        m=re.match(r'\| (SUP-\d+|PM-\d+) \| \[(API-\d+)\].*?\| `([^`]+)` \| ([^|]+) \| ([^|]+) \| \*\*([A-Z_]+)\*\* \| (.*) \|',line)
        if m: rows.append((m.group(1),m.group(2),m.group(3).strip(),m.group(4).strip(),m.group(5).strip(),m.group(6),m.group(7).strip(),actor))
if len(set(x[0] for x in rows)) != 23: fail(f'expected 23 screens, got {len(set(x[0] for x in rows))}')
if len(rows) != len(set((x[0],x[1]) for x in rows)): fail('duplicate Screen/API adjudication')
errors=[]
for sid,ident,route,role,transport,status,reason,actor in rows:
    e=entries.get(ident)
    if not e: errors.append(f'{sid}/{ident}: unknown API'); continue
    expected='READY_BY_SOURCE' if actor in e['role'].split(', ') and e['cookie'] else ('TRANSPORT_BLOCKED' if actor in e['role'].split(', ') else 'WRONG_ROLE_REMOVED')
    if status != expected: errors.append(f'{sid}/{ident}: {status} expected {expected}')
    if route != e['route']: errors.append(f'{sid}/{ident}: route mismatch')
    if status=='TRANSPORT_BLOCKED' and 'Bearer' not in transport and 'transport' not in reason.lower(): errors.append(f'{sid}/{ident}: blocked without transport reason')
    if status=='WRONG_ROLE_REMOVED' and status=='READY_BY_SOURCE': errors.append(f'{sid}/{ident}: contradictory role status')
for sid,ident,actor in main:
    e=entries.get(ident)
    if not e: errors.append(f'{sid}/{ident}: unknown API in active matrix'); continue
    if actor not in e['role'].split(', '): errors.append(f'{sid}/{ident}: wrong-role API remains active')
if errors:
    for x in errors: print(x)
    fail(f'{len(errors)} validation errors')
from collections import Counter
c=Counter(x[5] for x in rows)
result={'screens':len(set(x[0] for x in rows)),'mappings':len(rows),'valid':c['READY_BY_SOURCE'],'wrong_role':c['WRONG_ROLE_REMOVED'],'transport_blocked':c['TRANSPORT_BLOCKED'],'source_uncertain':c['SOURCE_UNCERTAIN'],'active_mappings':len(main)}
print(json.dumps(result,ensure_ascii=False))
print('PASS: catalog IDs, actor authorization, cookie eligibility, route equality and active-matrix role safety')
