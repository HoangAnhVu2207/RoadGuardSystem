"""Read-only guidance, discovery and document ownership guard."""

import argparse
import copy
import json
import re
import tempfile
import xml.etree.ElementTree as ET
from pathlib import Path


def discovered_guidance(root):
    paths = [root / "AGENTS.md", root / "agent.md", root / ".agents/README.md"]
    for name in ("AGENTS.md", "agent.md"):
        paths.extend(p for p in root.rglob(name) if "docs/history" not in p.relative_to(root).as_posix()
                     and ".git" not in p.parts)
    for pattern in (".agents/rules/*.md", ".agents/modules/*.md", ".agents/references/*.md",
                    ".agents/skills/**/SKILL.md", ".agent/**/*.md"):
        paths.extend(root.glob(pattern))
    return {p.relative_to(root).as_posix() for p in paths if p.is_file()}


def verify_inventory(root, manifest, staging):
    expected = "PROPOSED_INACTIVE" if staging else "ACTIVE_AFTER_OWNER_ACCEPTANCE"
    if manifest["status"] != expected:
        raise ValueError("guidance manifest is not accepted/active")
    if not staging and (not manifest.get("acceptance_source") or not manifest.get("accepted_at")):
        raise ValueError("missing owner acceptance metadata")
    paths = manifest["guidance_paths"]
    if len(paths) != len(set(paths)) or not paths or "AGENTS.md" not in paths:
        raise ValueError("invalid guidance path manifest")
    for rel in paths:
        if not (root / rel).is_file():
            raise ValueError(f"missing guidance {rel}")
    retired = set(manifest["retired_guidance"])
    for rel in retired:
        if (root / rel).exists():
            raise ValueError(f"old active guidance remains {rel}")
    skills = manifest.get("skills", [])
    if len({item["path"] for item in skills}) != len(skills):
        raise ValueError("duplicate declared skill")
    for item in skills:
        if item["path"] not in paths or not item["path"].startswith(".agents/skills/") or not item["path"].endswith("/SKILL.md"):
            raise ValueError(f"invalid skill declaration {item['path']}")
        if item.get("status") != expected:
            raise ValueError(f"skill status mismatch {item['path']}")
    declared_skills = {item["path"] for item in skills}
    if {p for p in paths if p.startswith(".agents/skills/")} != declared_skills:
        raise ValueError("guidance skill declaration mismatch")
    undisclosed = discovered_guidance(root) - set(paths) - retired
    if undisclosed:
        raise ValueError(f"undeclared discoverable guidance: {sorted(undisclosed)}")
    return paths


def verify(root, source_root, mode="all", staging=False, manifest_override=None):
    manifest = manifest_override or json.loads((root / ".agents/manifest.json").read_text(encoding="utf-8"))
    paths = verify_inventory(root, manifest, staging)
    for rel in paths:
        path = root / rel
        content = path.read_text(encoding="utf-8")
        if len(content.splitlines()) > 500:
            raise ValueError(f"oversized guidance {rel}")
        for target in re.findall(r"\[[^\]]+\]\(([^)]+)\)", content):
            dest = target.split("#", 1)[0]
            if not dest or dest.startswith(("http:", "https:", "mailto:")):
                continue
            if not (path.parent / dest).exists():
                raise ValueError(f"broken target link {rel} -> {target}")
        prefixes = ("planning/", "docs/", "contracts/", "tests/", ".agents/", "RoadGuardSystem.")
        for token in re.findall(r"`([^`]+)`", content):
            if token.startswith(prefixes) and "/" in token and "*" not in token and "<" not in token:
                referenced = token.split("#", 1)[0]
                if not (root / referenced).exists() and not (source_root / referenced).exists():
                    raise ValueError(f"broken routed path {rel} -> {token}")
    for rel in manifest["product_paths"] + manifest["backend_paths"] + manifest["contract_paths"]:
        if not (root / rel).is_file():
            raise ValueError(f"missing routed document {rel}")
    source = json.loads((source_root / "planning/refactor/04-source-crosswalk.json").read_text(encoding="utf-8"))
    for item in source["documents"] + source["adr"]:
        if not (source_root / item["source"]).exists() or not (root / item["destination"]).is_file():
            raise ValueError(f"broken historical transition {item['source']} -> {item['destination']}")
    product_decisions = (root / "docs/product/confirmed-decisions.md").read_text(encoding="utf-8")
    decision_source = (source_root / "planning/refactor/02-decision-register.md").read_text(encoding="utf-8")
    for item in source["decisions"]:
        line = next((line for line in decision_source.splitlines() if line.startswith(f"| {item['id']} |")), None)
        if not line or line not in product_decisions:
            raise ValueError(f"confirmed decision wording drift: {item['id']}")
    collection = json.loads((source_root / "docs/postman/RoadGuardSystem-V2.postman_collection.json").read_text(encoding="utf-8"))
    environment = json.loads((source_root / "docs/postman/RoadGuard.local.postman_environment.json").read_text(encoding="utf-8"))
    if collection["info"]["schema"] != "https://schema.getpostman.com/json/collection/v2.1.0/collection.json":
        raise ValueError("Postman collection format drift")
    if environment["name"] != "RoadGuard.local":
        raise ValueError("Postman environment name drift")
    config = json.loads((source_root / ".agents/mcp_config.json").read_text(encoding="utf-8"))
    if not {"microsoft-learn", "context7"} <= set(config["mcpServers"]):
        raise ValueError("MCP config drift")
    props = ET.parse(source_root / "Directory.Build.props").getroot().find("PropertyGroup")
    for name, allowed in {"Nullable": {"enable"}, "TreatWarningsAsErrors": {"true"},
                          "AnalysisMode": {"Recommended"}, "EnforceCodeStyleInBuild": {"true"}}.items():
        if props.findtext(name) not in allowed:
            raise ValueError(f"compiler property drift: {name}")
    if mode == "all":
        mapping = (root / ".agents/modules/README.md").read_text(encoding="utf-8")
        cross = json.loads((source_root / "planning/refactor/04-operation-crosswalk.json").read_text(encoding="utf-8"))
        for token in ("C027", "listMyInspectionTasks", "BR-46", "RF-10-07", "RF-10-09-A"):
            if token not in mapping:
                raise ValueError(f"module map missing {token}")
        ids = [r["id"] for k in ("fr", "br", "us", "pf", "nfr") for r in source[k]]
        if len(ids) != len(set(ids)) or any(not r.get("rf_task") for k in ("fr", "br", "us", "pf", "nfr") for r in source[k]):
            raise ValueError("requirement ID duplicate or unmapped")
        inspection = next(r for r in cross["current"] if r["id"] == "C027")
        if inspection["rf_task"] != "RF-10-07":
            raise ValueError("C027 ownership drift")
        br = next(r for r in source["br"] if r["id"] == "BR-46")
        if br["rf_task"] != "RF-10-07" or "RF-10-09-A" not in br["coordination_tasks"]:
            raise ValueError("BR-46 ownership drift")
        for source_rel in manifest["source_references"]:
            if not (source_root / source_rel).exists():
                raise ValueError(f"missing source reference {source_rel}")
        print(f"Requirement count {len(ids)} (historical baseline 148; delta {len(ids)-148:+d})")
    return len(paths)


def self_test(root, source_root, mode, staging):
    manifest = json.loads((root / ".agents/manifest.json").read_text(encoding="utf-8"))
    verify(root, source_root, mode, staging, manifest)
    cases = []
    missing = copy.deepcopy(manifest)
    missing["guidance_paths"].append(".agents/rules/missing-negative-fixture.md")
    cases.append((root, missing, staging, "missing guidance"))
    metadata = copy.deepcopy(manifest)
    metadata.update(status="ACTIVE_AFTER_OWNER_ACCEPTANCE", acceptance_source=None, accepted_at=None)
    cases.append((root, metadata, False, "missing owner acceptance metadata"))
    with tempfile.TemporaryDirectory(prefix="rf05-guidance-negative-") as tmp:
        fixture = Path(tmp)
        for rel in manifest["guidance_paths"]:
            target = fixture / rel
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes((root / rel).read_bytes())
        old = manifest["retired_guidance"][0]
        target = fixture / old
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_text("historical fixture", encoding="utf-8")
        cases.append((fixture, manifest, staging, "old active guidance remains"))
        for case_root, candidate, case_staging, expected in cases:
            try:
                verify(case_root, source_root, mode, case_staging, candidate)
            except ValueError as exc:
                if expected not in str(exc):
                    raise AssertionError(f"wrong negative failure: expected {expected}, got {exc}") from exc
            else:
                raise AssertionError(f"negative case missed: {expected}")
            print(f"PASS negative {expected}")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, required=True)
    parser.add_argument("--source-root", type=Path)
    parser.add_argument("--mode", choices=("all", "docs"), default="all")
    parser.add_argument("--self-test", action="store_true")
    parser.add_argument("--self-test-negative", action="store_true")
    parser.add_argument("--staging", action="store_true")
    args = parser.parse_args()
    source_root = args.source_root or args.root
    count = verify(args.root, source_root, args.mode, args.staging)
    print(f"PASS guidance {count} paths, {args.mode} mode; staging={args.staging}; runtime NOT RUN")
    if args.self_test or args.self_test_negative:
        self_test(args.root, source_root, args.mode, args.staging)


if __name__ == "__main__":
    main()
