"""Literal ASP.NET attribute route discovery for documentation, not MVC execution.

Fail closed on unresolvable templates; action-only routes and absolute overrides
are valid. Constraints are normalized only for method/path comparison.
"""
import re

HTTP = re.compile(r'\bHttp(Get|Post|Put|Patch|Delete|Head|Options)(?:\(\s*"([^"\n]*)"\s*\))?(?=\s*[,\]])')
ACTION = re.compile(r'\bpublic\s+(?:async\s+)?[\w<>?,.\[\] ]+?\s+(\w+)\s*\(')


def normalize(route):
    route = route.replace('{version:apiVersion}', '1')
    return re.sub(r'\{(\w+):[^}]+\}', r'{\1}', '/' + route.strip('/'))


def discover(text, filename='Controller.cs'):
    # Preserve line numbers while excluding comments containing fake attributes.
    text = re.sub(r'/\*.*?\*/|//[^\n]*', lambda m: '\n' * m[0].count('\n'), text, flags=re.S)
    declaration = re.search(r'public\s+(?:sealed\s+|abstract\s+)?class\s+(\w+)', text)
    if not declaration:
        raise ValueError(f'{filename}: controller declaration unresolved')
    header, body = text[:declaration.start()], text[declaration.end():]
    bases = re.findall(r'\bRoute\(\s*"([^"]*)"\s*\)', header) or [None]
    result = []
    for match in HTTP.finditer(body):
        action = ACTION.search(body, match.end())
        if action is None:
            raise ValueError(f'{filename}: HTTP attribute without action')
        suffix = match[2]
        for base in bases:
            if suffix is None and base is None:
                raise ValueError(f'{filename}.{action[1]}: no literal route template')
            route = suffix or ''
            if route.startswith('~/'):
                route = route[2:]
            elif not route.startswith('/') and base is not None:
                route = base.rstrip('/') + '/' + route
            route = route.replace('[controller]', declaration[1].removesuffix('Controller')).replace('[action]', action[1])
            if '[' in route or ']' in route:
                raise ValueError(f'{filename}: unsupported route token {route}')
            line = text[:declaration.end() + action.start()].count('\n') + 1
            result.append({'method': match[1].upper(), 'path': normalize(route),
                           'controller': declaration[1], 'action': action[1], 'line': line})
    # Unsupported HTTP expression must not silently disappear from the inventory.
    if len(list(re.finditer(r'\bHttp(?:Get|Post|Put|Patch|Delete|Head|Options)\b', body))) != len(list(HTTP.finditer(body))):
        raise ValueError(f'{filename}: nonliteral HTTP template')
    return result
