"""Read-only canonical Markdown target/heading checker; Python standard library only."""
from pathlib import Path
import argparse
import json
import re
import unicodedata
from urllib.parse import unquote, urlsplit
from functools import lru_cache


def markdown_content(text):
    return re.sub(r"(?ms)^\s*(`{3,}|~{3,})[^\n]*\n.*?^\s*\1\s*$", "", text)


def heading_ids(text):
    result = set()
    counts = {}
    for match in re.finditer(r"(?m)^\s{0,3}#{1,6}\s+(.+?)\s*#*\s*$", markdown_content(text)):
        title = re.sub(r"<[^>]+>", "", match.group(1)).replace("`", "").strip().lower()
        slug = "".join(c for c in title if c in " -_" or unicodedata.category(c)[0] in "LN")
        slug = re.sub(r"\s", "-", slug)
        occurrence = counts.get(slug, 0)
        counts[slug] = occurrence + 1
        result.add(slug if occurrence == 0 else f"{slug}-{occurrence}")
    return result


@lru_cache(maxsize=None)
def headings_at(destination):
    """One file read/heading parse per target in a checker invocation."""
    return heading_ids(destination.read_text(encoding="utf-8-sig"))


def audit_text(text, origin, repo):
    """Return errors and counts for links in one Markdown text; never write files."""
    visible = markdown_content(text)
    definitions = dict(re.findall(r"(?m)^\s{0,3}\[([^\]]+)\]:\s*<?([^\s>]+)>?", visible))
    definitions = {label.casefold(): target for label, target in definitions.items()}
    targets = re.findall(r"(?<!!)\[[^\]\n]+\]\(\s*(<[^>]+>|[^\s)]+)(?:\s+\"[^\"]*\")?\s*\)", visible)
    errors = []
    for label, ident in re.findall(r"(?<!!)\[([^\]\n]+)\]\[([^\]\n]*)\]", visible):
        target = definitions.get((ident or label).casefold())
        if target is None:
            errors.append(f"{origin.name}: undefined reference {(ident or label)!r}")
        else:
            targets.append(target)
    local = external = 0
    for target in targets:
        target = target.strip("<>")
        parsed = urlsplit(target)
        if parsed.scheme or parsed.netloc:
            external += 1
            continue
        local += 1
        pathpart = unquote(parsed.path)
        destination = origin if not pathpart else (repo / pathpart.lstrip("/") if pathpart.startswith("/") else origin.parent / pathpart)
        destination = destination.resolve()
        if not destination.is_relative_to(repo):
            errors.append(f"{origin.name}: target outside repository: {target}")
            continue
        if not destination.exists():
            errors.append(f"{origin.name}: missing target: {target}")
            continue
        if parsed.fragment and destination.suffix.lower() == ".md":
            fragment = unquote(parsed.fragment)
            if fragment not in headings_at(destination):
                errors.append(f"{origin.name}: missing heading: {target}")
    return errors, {"local_links": local, "external_links_not_checked": external}


def main():
    headings_at.cache_clear()
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--docs-dir", type=Path, default=Path(__file__).resolve().parents[1])
    args = parser.parse_args()
    repo = Path(__file__).resolve().parents[3]
    docs = args.docs_dir.resolve()
    if not docs.is_dir() or not docs.is_relative_to(repo):
        parser.error("docs-dir must be an existing directory inside this repository")
    errors = []
    totals = {"markdown_files": 0, "local_links": 0, "external_links_not_checked": 0}
    for origin in sorted(docs.rglob("*.md")):
        found, counts = audit_text(origin.read_text(encoding="utf-8-sig"), origin, repo)
        errors.extend(found)
        totals["markdown_files"] += 1
        for key, count in counts.items():
            totals[key] += count
    print(json.dumps({**totals, "errors": len(errors)}, ensure_ascii=False))
    for error in errors:
        print(error)
    return 1 if errors else 0


if __name__ == "__main__":
    raise SystemExit(main())
