"""Read-only check of the active readiness registry and generated RF-04 crosswalk."""

import argparse
import copy
import hashlib
import json
import os
import sys
from pathlib import Path

GROUPS = ("fr", "br", "us", "pf", "nfr")


def compare_requirement_ids(recorded, fresh, mapped):
    ids = [row["id"] for row in recorded]
    source_ids = [row["id"] for row in fresh]
    if len(ids) != len(set(ids)) or len(source_ids) != len(set(source_ids)):
        raise ValueError("duplicate requirement ID")
    if set(ids) != set(source_ids) or set(ids) != set(mapped):
        raise ValueError(f"requirement mapping drift: missing={sorted(set(source_ids)-set(ids))}, stale={sorted(set(ids)-set(source_ids))}, unmapped={sorted(set(source_ids)-set(mapped))}")
    return len(ids) - 148


def verify(root):
    plan = root / "planning/refactor"
    required = [plan / "tools/rf05_readiness.py", plan / "tools/build_rf04_sources.py",
                plan / "04-readiness-decisions.json", plan / "04-source-crosswalk.json"]
    missing = [str(path.relative_to(root)) for path in required if not path.is_file()]
    if missing:
        raise ValueError(f"active readiness inputs missing: {missing}")
    sys.path.insert(0, str(plan / "tools"))
    from build_rf04_sources import SOURCES, SOURCE_TASK, nfr_rows, sections
    from rf05_readiness import apply_readiness

    cross = json.loads(required[3].read_text(encoding="utf-8"))
    recorded = [row for group in GROUPS for row in cross[group]]
    fresh = [row for spec in SOURCES.values() for row in sections(*spec)] + nfr_rows()
    delta = compare_requirement_ids(recorded, fresh, SOURCE_TASK)
    expected = apply_readiness(copy.deepcopy(fresh), root, required[2], required[3])
    if cross["input_sha256"].get("planning/refactor/tools/build_rf04_sources.py") != hashlib.sha256(required[1].read_bytes()).hexdigest():
        raise ValueError("generator fingerprint stale")
    for rel, expected_hash in cross["input_sha256"].items():
        path = root / rel
        if not path.is_file() or hashlib.sha256(path.read_bytes()).hexdigest() != expected_hash:
            raise ValueError(f"source fingerprint stale: {rel}")
    by_id = {row["id"]: row for row in recorded}
    for row in expected:
        actual = by_id[row["id"]]
        if actual != row:
            raise ValueError(f"generated crosswalk stale: {row['id']}")
        destination, _ = row["destination"].split("#", 1)
        page = (root / destination).read_text(encoding="utf-8")
        marker = f"Readiness: `{row['implementation_readiness']}`."
        section = page.split(f"### {row['id']} - ", 1)
        if len(section) != 2:
            raise ValueError(f"generated product page stale: {row['id']}")
        excerpt = section[1].split("\n### ", 1)[0]
        if (marker not in excerpt or f"Historical authority: `{row['source_authority']}`" not in excerpt
                or f"current authority: `{row['authority']}`" not in excerpt):
            raise ValueError(f"generated product page stale: {row['id']}")
        for claim in row["claims"]:
            if f"**{claim['id']}** (origin `{claim['authority']}`, current `{claim['current_authority']}`)" not in excerpt:
                raise ValueError(f"generated product page stale: {row['id']} / {claim['id']}")
    # All comparisons above are read-only. No generator is executed against the checkout.
    return len(recorded), delta, len(json.loads(required[2].read_text(encoding="utf-8"))["decisions"])


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, required=True)
    args = parser.parse_args()
    os.environ["ROADGUARD_ROOT"] = str(args.root.resolve())
    count, delta, decisions = verify(args.root.resolve())
    print(f"PASS repository readiness: {count} mapped requirements (historical delta {delta:+d}), {decisions} registry decisions; read-only")


if __name__ == "__main__":
    main()
