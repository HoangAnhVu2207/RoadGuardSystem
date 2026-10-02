"""Conservative, read-only RF-09 OpenAPI and SQL-inventory change guard.

Requires PyYAML. Exit 0 means no detected breaking/unknown change in supported
fields, not a general backward-compatibility guarantee. Exit 2 requires review.
"""

import argparse
import json
import sys
from pathlib import Path

import yaml


METHODS = {"get", "put", "post", "delete", "patch", "head", "options", "trace"}
def read_mapping(path, yaml_input=False):
    data = (yaml.safe_load if yaml_input else json.loads)(Path(path).read_text(encoding="utf-8-sig"))
    if not isinstance(data, dict):
        raise ValueError(f"{path}: root must be an object")
    return data


def change(changes, kind, name, before=None, after=None):
    changes.append({"classification": kind, "path": name, "before": before, "after": after})


def compare_schema(old, new, path, changes):
    if not isinstance(old, dict) or not isinstance(new, dict):
        if old != new:
            change(changes, "REVIEW_REQUIRED", path, old, new)
        return
    for key in sorted(set(old) | set(new)):
        a, b = old.get(key), new.get(key)
        if a == b:
            continue
        name = f"{path}.{key}"
        if key == "properties" and isinstance(a, dict) and isinstance(b, dict):
            for prop in sorted(set(a) | set(b)):
                if prop not in a:
                    change(changes, "ADDITIVE", f"{name}.{prop}", None, b[prop])
                elif prop not in b:
                    change(changes, "POTENTIALLY_BREAKING", f"{name}.{prop}", a[prop], None)
                else:
                    compare_schema(a[prop], b[prop], f"{name}.{prop}", changes)
        elif key == "items":
            compare_schema(a, b, name, changes)
        elif key == "required" and isinstance(a, list) and isinstance(b, list):
            if set(b) - set(a):
                change(changes, "POTENTIALLY_BREAKING", name, a, b)
            elif set(a) - set(b):
                change(changes, "ADDITIVE", name, a, b)
        elif key == "enum" and isinstance(a, list) and isinstance(b, list):
            kind = "POTENTIALLY_BREAKING" if set(a) - set(b) else "ADDITIVE"
            change(changes, kind, name, a, b)
        elif key in {"minimum", "minLength", "minItems"} and isinstance(a, (int, float)) and isinstance(b, (int, float)):
            change(changes, "POTENTIALLY_BREAKING" if b > a else "ADDITIVE", name, a, b)
        elif key in {"maximum", "maxLength", "maxItems"} and isinstance(a, (int, float)) and isinstance(b, (int, float)):
            change(changes, "POTENTIALLY_BREAKING" if b < a else "ADDITIVE", name, a, b)
        elif key in {"nullable", "type", "format", "$ref", "pattern", "additionalProperties",
                     "exclusiveMinimum", "exclusiveMaximum"}:
            change(changes, "POTENTIALLY_BREAKING", name, a, b)
        else:
            change(changes, "REVIEW_REQUIRED", name, a, b)


def compare_openapi(old, new, changes):
    for doc in (old, new):
        if not isinstance(doc.get("paths"), dict) or not isinstance(doc.get("components", {}).get("schemas", {}), dict):
            raise ValueError("OpenAPI paths/components.schemas must be objects")
    def operations(doc):
        return {(method.upper(), path): body for path, item in doc["paths"].items()
                if isinstance(item, dict) for method, body in item.items() if method in METHODS}
    before, after = operations(old), operations(new)
    for key in sorted(set(before) | set(after)):
        label = f"operations.{key[0]} {key[1]}"
        if key not in before:
            change(changes, "ADDITIVE", label)
            continue
        if key not in after:
            change(changes, "POTENTIALLY_BREAKING", label)
            continue
        a, b = before[key], after[key]
        if not isinstance(a, dict) or not isinstance(b, dict):
            raise ValueError(f"{label}: operation must be an object")
        for field in ("parameters", "requestBody", "security"):
            if a.get(field) != b.get(field):
                change(changes, "REVIEW_REQUIRED", f"{label}.{field}", a.get(field), b.get(field))
        ra, rb = a.get("responses", {}), b.get("responses", {})
        if not isinstance(ra, dict) or not isinstance(rb, dict):
            raise ValueError(f"{label}: responses must be objects")
        for status in sorted(set(ra) | set(rb)):
            status_label = f"{label}.responses.{status}"
            if status not in ra:
                change(changes, "ADDITIVE", status_label)
            elif status not in rb:
                change(changes, "POTENTIALLY_BREAKING", status_label)
            else:
                ca, cb = ra[status].get("content", {}), rb[status].get("content", {})
                for media in sorted(set(ca) | set(cb)):
                    media_label = f"{status_label}.content.{media}"
                    if media not in ca:
                        change(changes, "ADDITIVE", media_label)
                    elif media not in cb:
                        change(changes, "POTENTIALLY_BREAKING", media_label)
                    elif ca[media] != cb[media]:
                        change(changes, "REVIEW_REQUIRED", media_label, ca[media], cb[media])
                if ra[status].get("headers") != rb[status].get("headers"):
                    change(changes, "REVIEW_REQUIRED", f"{status_label}.headers")
    sa, sb = old.get("components", {}).get("schemas", {}), new.get("components", {}).get("schemas", {})
    for name in sorted(set(sa) | set(sb)):
        label = f"schemas.{name}"
        if name not in sa:
            change(changes, "ADDITIVE", label)
        elif name not in sb:
            change(changes, "POTENTIALLY_BREAKING", label)
        else:
            compare_schema(sa[name], sb[name], label, changes)


def compare_sql(old, new, tables, changes):
    for doc in (old, new):
        if not isinstance(doc.get("sql"), dict):
            raise ValueError("SQL inventory must contain sql object")
    for group in ("tables", "columns", "keys", "foreignKeys", "indexes", "checks", "triggers"):
        def rows(doc):
            items = doc["sql"].get(group)
            if not isinstance(items, list):
                raise ValueError(f"SQL inventory sql.{group} must be an array")
            selected = [row for row in items if row.get("table") in tables]
            indexed = {json.dumps((row.get("schema"), row.get("table"), row.get("name"),
                                   row.get("column"), row.get("ordinal")), sort_keys=True): row
                       for row in selected}
            if len(indexed) != len(selected):
                raise ValueError(f"SQL inventory sql.{group} has duplicate object keys")
            return indexed
        a, b = rows(old), rows(new)
        for key in sorted(set(a) | set(b)):
            label = f"sql.{group}.{key}"
            if key not in a:
                row = b[key]
                kind = "ADDITIVE" if group == "columns" and row.get("nullable") is True else "REVIEW_REQUIRED"
                change(changes, kind, label, None, row)
            elif key not in b:
                change(changes, "POTENTIALLY_BREAKING", label, a[key], None)
            elif a[key] != b[key]:
                change(changes, "POTENTIALLY_BREAKING" if group == "columns" else "REVIEW_REQUIRED", label, a[key], b[key])


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--old-openapi", required=True)
    parser.add_argument("--new-openapi", required=True)
    parser.add_argument("--old-schema", required=True)
    parser.add_argument("--new-schema", required=True)
    parser.add_argument("--tables", required=True, help="comma-separated SQL table names")
    args = parser.parse_args()
    try:
        changes = []
        compare_openapi(read_mapping(args.old_openapi, True), read_mapping(args.new_openapi, True), changes)
        tables = set(filter(None, args.tables.split(",")))
        if not tables:
            raise ValueError("--tables must name at least one table")
        compare_sql(read_mapping(args.old_schema), read_mapping(args.new_schema), tables, changes)
        print(json.dumps({"changes": changes, "tableScope": sorted(tables)}, ensure_ascii=False, indent=2))
        return 2 if any(item["classification"] in {"POTENTIALLY_BREAKING", "REVIEW_REQUIRED"} for item in changes) else 0
    except (OSError, ValueError, yaml.YAMLError, TypeError, AttributeError) as exc:
        print(f"RF09_GUARD_INVALID_INPUT: {exc}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    sys.exit(main())
