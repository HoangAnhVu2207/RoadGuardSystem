"""Read-only Phase 5 static audit. Requires existing PyYAML; never changes runtime.

Compares literal MVC routes, stored OpenAPI, catalog, crosswalk and active mobile
matrix API columns. Does not execute middleware, SQL, native code or E2E tests.
"""
from pathlib import Path
import json
import re
import sys
import yaml
import contextlib
import io
import runpy

ROOT = Path(__file__).resolve().parents[3]
DOCS = ROOT / 'docs/HoangHaiWarrantySystem-Official'


def read(path):
    return path.read_text(encoding='utf-8-sig')


def normalize(route):
    route = route.replace('{version:apiVersion}', '1').replace('[controller]', '')
    return re.sub(r'\{(\w+):[^}]+\}', r'{\1}', '/' + route.strip('/'))


def entries():
    text = read(DOCS / '04_API-Contracts/02_Full-API-Catalog.md')
    result = {}
    for match in re.finditer(r'^### (API-\d+)\n(.*?)(?=^### API-|\Z)', text, re.M | re.S):
        body = match[2]
        route = re.search(r'\*\*`(GET|POST|PUT|PATCH|DELETE|HEAD|OPTIONS) ([^`]+)`\*\*', body)
        role = re.search(r'Actor/role/scope:\*\* ([^.\n]+)', body)
        result[match[1]] = {'method': route[1], 'path': route[2], 'role': role[1], 'body': body}
    return result


def api_ids(text):
    """Expand shorthand API-080/087 and bounded ranges before extracting IDs."""
    text = re.sub(r'API-(\d{3})[–—-](\d{3})',
                  lambda m: ' '.join(f'API-{i:03}' for i in range(int(m[1]), int(m[2]) + 1)), text)
    text = re.sub(r'(API-\d{3})((?:/\d{3})+)',
                  lambda m: m[1] + ' ' + m[2].replace('/', 'API-'), text)
    return re.findall(r'API-\d{3}', text)


def mobile_errors(catalog, rows):
    errors = []
    for sid, actor, column, ident in rows:
        entry = catalog.get(ident)
        if entry is None:
            errors.append(f'{sid}: missing {ident}')
            continue
        if actor not in entry['role'].split(', ') and entry['role'] != 'Anonymous/credential':
            errors.append(f'{sid}: {actor} cannot call {ident}')
        if column == 'read' and entry['method'] != 'GET':
            errors.append(f'{sid}: {ident} {entry["method"]} is not an initial GET')
        if column == 'command' and entry['method'] == 'GET':
            errors.append(f'{sid}: {ident} GET is not a command')
    return errors


def run():
    errors = []
    cat = entries()
    spec = yaml.safe_load(read(ROOT / 'docs/RoadGuard-Backend-Official/03_API_Contracts/openapi.backend.yaml'))
    operations = {(method.upper(), path) for path, value in spec['paths'].items()
                  for method in value if method.lower() in {'get', 'post', 'put', 'patch', 'delete', 'head', 'options'}}
    controllers = sorted((ROOT / 'RoadGuardSystem.API/Controllers').glob('*.cs'))
    source = []
    for file in controllers:
        text = read(file)
        header, body = text.split('public sealed class', 1) if 'public sealed class' in text else text.split('public class', 1)
        base = re.search(r'\bRoute\("([^"]+)"\)', header)
        if not base:
            errors.append(f'unsupported controller route {file.name}')
            continue
        for match in re.finditer(r'\bHttp(Get|Post|Put|Patch|Delete|Head|Options)(?:\("([^"]*)"\))?(?=\s*[,\]])', body):
            suffix = match[2] or ''
            route = suffix[2:] if suffix.startswith('~/') else suffix if suffix.startswith('/') else base[1].rstrip('/') + '/' + suffix
            source.append((match[1].upper(), normalize(route)))
    source_set = set(source)
    catalog_set = {(e['method'], e['path']) for e in cat.values()}
    for label, left, right in [('source/OpenAPI', source_set, operations), ('catalog/OpenAPI', catalog_set, operations)]:
        if left != right:
            errors.append(f'{label}: left-only={sorted(left-right)} right-only={sorted(right-left)}')
    if len(source) != len(source_set):
        errors.append('duplicate source method/path')
    crosswalk = read(DOCS / '04_API-Contracts/09_Consumer-API-Crosswalk.md')
    crosswalk_count = 0
    for line in crosswalk.splitlines():
        m = re.match(r'\| \[(API-\d+)\].*?\| `(\w+) ([^`]+)` \| [^|]+ \| ([^|]+) \|', line)
        if not m:
            continue
        crosswalk_count += 1
        e = cat.get(m[1])
        if not e or (m[2], m[3], m[4].strip()) != (e['method'], e['path'], e['role']):
            errors.append(f'crosswalk mismatch {m[1]}')
    if crosswalk_count != len(cat):
        errors.append('crosswalk cardinality mismatch')
    rows = []
    counts = {}
    for folder, filename, prefix, actor in [
        ('Reporter', '01_Reporter-Screen-API-Matrix.md', 'RPT', 'Reporter'),
        ('Operator', '01_Operator-Screen-API-Matrix.md', 'O', 'Operator'),
        ('Crew', '01_Crew-Screen-API-Matrix.md', 'C', 'Crew')]:
        count = 0
        for line in read(DOCS / '06_Mobile-Expo' / folder / filename).splitlines():
            if not re.match(r'\| ' + prefix + r'-\d+ ', line):
                continue
            cols = line.split('|')
            sid = cols[1].strip().split()[0]
            count += 1
            read_col, command_col = (3, 4) if folder == 'Reporter' else (2, 3)
            for column, cell in [('read', cols[read_col]), ('command', cols[command_col])]:
                rows.extend((sid, actor, column, ident) for ident in api_ids(cell))
                for inline in re.finditer(r'(API-\d{3})\s+`(GET|POST|PUT|PATCH|DELETE) ([^`]+)`', cell):
                    entry = cat.get(inline[1])
                    if entry and (inline[2], inline[3]) != (entry['method'], entry['path']):
                        errors.append(f'{sid}: inline route contradicts catalog for {inline[1]}')
        counts[folder] = count
    errors.extend(mobile_errors(cat, rows))
    # Reuse the source-backed Web selector adjudicator for all catalog operations.
    # Role membership here remains catalog metadata; resource authority needs the
    # repository/service evidence audit and actual negative HTTP/SQL tests.
    diagnostics = io.StringIO()
    try:
        with contextlib.redirect_stdout(diagnostics):
            web = runpy.run_path(str(DOCS / '05_Web/tools/validate_phase3a_c01.py'))
            for ident, entry in web['entries'].items():
                web['source_transport'](ident, entry)
    except SystemExit as ex:
        errors.append(f'Web source/catalog transport validation failed: exit {ex.code}: {diagnostics.getvalue().strip()}')
    schema_ids = set(spec['components']['schemas'])
    schemas = set(re.findall(r'^## Schema (\S+)$', read(DOCS / '04_API-Contracts/03_DTO-and-Schema-Reference.md'), re.M))
    if schemas != schema_ids:
        errors.append(f'schema reference mismatch: {sorted(schemas ^ schema_ids)}')
    # Validate source locators and line ranges, including plain Markdown source links.
    source_links = 0
    for file in DOCS.rglob('*.md'):
        for target, line in re.findall(r'\]\(([^)]+\.cs)#L(\d+)\)', read(file)):
            destination = (file.parent / target).resolve()
            source_links += 1
            if not destination.is_file() or not 1 <= int(line) <= len(read(destination).splitlines()):
                errors.append(f'{file.relative_to(DOCS)}: invalid source line {target}#L{line}')
    # Pin critical offline wire invariants; this is static drift detection, not crypto E2E.
    wire = {
        'RoadGuardSystem.Services/Implementations/Offline/OfflineWorkflowEngine.cs':
            ['JsonSerializerDefaults.Web', 'OriginId.ToString("N")', 'StringComparer.Ordinal'],
        'RoadGuardSystem.BusinessObjects/Offline/OfflinePackageAuthentication.cs':
            ['roadguard.h5.direct-sync.v1\\0', 'IeeeP1363FixedFieldConcatenation', 'hash.AppendData(ClaimPurpose)'],
        'RoadGuardSystem.BusinessObjects/Offline/OfflineDeviceKeys.cs':
            ['ExportSubjectPublicKeyInfo', 'IeeeP1363FixedFieldConcatenation']}
    for path, invariants in wire.items():
        content = read(ROOT / path)
        for invariant in invariants:
            if invariant not in content:
                errors.append(f'offline invariant missing: {path}: {invariant}')
    # Anchor naming is separately checked by the canonical Markdown checker.
    unknown_ids = []
    for file in DOCS.rglob('*.md'):
        for ident in set(re.findall(r'API-\d{3}', read(file))):
            if ident not in cat:
                unknown_ids.append(f'{file.relative_to(DOCS)}: {ident}')
    errors.extend(unknown_ids)
    print(json.dumps({'controllers': len(controllers), 'source_operations': len(source),
                      'source_paths': len({p for _, p in source}), 'catalog_operations': len(cat),
                      'crosswalk_operations': crosswalk_count, 'swagger_schemas': len(schema_ids),
                      'mobile_screens': counts, 'mobile_api_column_references': len(rows),
                      'mobile_unique_apis': len({r[3] for r in rows}),
                      'source_line_links_checked': source_links, 'offline_invariants': sum(map(len, wire.values())),
                      'errors': errors}, indent=2))
    return 1 if errors else 0


if __name__ == '__main__':
    if '--self-test' in sys.argv:
        sample = {'API-001': {'role': 'PM', 'method': 'POST'}}
        assert mobile_errors(sample, [('C-01', 'Crew', 'command', 'API-001')])
        assert mobile_errors(sample, [('C-01', 'PM', 'read', 'API-001')])
        assert mobile_errors(sample, [('C-01', 'PM', 'command', 'API-999')])
        assert not mobile_errors(sample, [('P-01', 'PM', 'command', 'API-001')])
        assert api_ids('API-080/087/094 API-113–115') == ['API-080', 'API-087', 'API-094', 'API-113', 'API-114', 'API-115']
        print('PASS: negative role, wrong method, missing ID; valid command; shorthand/range regression')
    else:
        sys.exit(run())
