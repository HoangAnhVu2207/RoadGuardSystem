"""Fail closed on current documentation metadata or raw source-pin drift."""
import argparse
import json
from pathlib import Path
import yaml
from sync_current_docs import ROOT, BE, digest, VERBS
from route_inventory import discover


def audit(mapping, spec, hashes):
    errors=[]
    pairs=set()
    for row in mapping['operations']:
        key=row['method'],row['path']
        if key in pairs: errors.append('duplicate pair '+str(key))
        pairs.add(key)
        path=ROOT/row['source']
        if not path.is_file() or digest(path)!=row['sha256']: errors.append('source pin '+row['id'])
        elif not any((x['method'],x['path'],x['action'],x['line'])==(row['method'],row['path'],row['action'],row['line']) for x in discover(path.read_text(encoding='utf-8-sig'))): errors.append('source locator '+row['id'])
        op=spec['paths'].get(row['path'],{}).get(row['method'].lower())
        if op is None: errors.append('missing operation '+row['id']); continue
        for field,default in [('parameters',[]),('responses',{}),('security',[])]:
            if row[field]!=op.get(field,default): errors.append(field+' mismatch '+row['id'])
        if row['request']!=op.get('requestBody'): errors.append('request mismatch '+row['id'])
        for evidence in row.get('sourceEvidence',[]):
            p=ROOT/evidence['path']
            if not p.is_file() or digest(p)!=evidence['sha256']: errors.append('dependency source pin '+row['id'])
    expected={(m.upper(),p) for p,v in spec['paths'].items() for m in v if m in VERBS}
    if pairs!=expected: errors.append('method/path coverage mismatch')
    for path,sha in hashes['files'].items():
        if not (ROOT/path).is_file() or digest(ROOT/path)!=sha: errors.append('source drift '+path)
    return errors


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--capture',type=Path,required=True)
    args=parser.parse_args()
    spec=yaml.safe_load((BE/'03_API_Contracts/openapi.backend.yaml').read_text(encoding='utf-8-sig'))
    raw=json.loads((args.capture/'runtime-openapi.json').read_text(encoding='utf-8'))
    mapping=json.loads((BE/'10_Traceability/api-source-map.json').read_text(encoding='utf-8'))
    hashes=json.loads((BE/'10_Traceability/current-source-hashes.json').read_text(encoding='utf-8'))
    errors=audit(mapping,spec,hashes)
    if spec!=raw: errors.append('source-built Swagger/document snapshot mismatch')
    print(json.dumps({'operations':len(mapping['operations']),'hashes':len(hashes['files']),'errors':errors},indent=2))
    return bool(errors)


if __name__=='__main__': raise SystemExit(main())
