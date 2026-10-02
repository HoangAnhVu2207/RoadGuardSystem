"""Fingerprint exact activation inputs and proposed outputs; never apply them."""

import hashlib
import json
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
RF05 = ROOT / "planning/refactor/rf05"


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def row(draft, target, action):
    current = ROOT / target
    return {"draft": draft.relative_to(ROOT).as_posix(), "target": target, "action": action,
            "source_sha256": sha(current) if current.is_file() else None,
            "proposed_sha256": sha(draft)}


def main():
    files = []
    drafts = RF05 / "drafts"
    for path in sorted(drafts.rglob("*.draft")):
        if path.name == "manifest.json.draft":
            continue
        rel = path.relative_to(drafts).as_posix()[:-6]
        if rel.startswith("agents/"):
            target = ".agents/" + rel[len("agents/"):]
        elif rel == "rf05_readiness.py":
            target = "planning/refactor/tools/rf05_readiness.py"
        elif rel == "readiness-decisions.json":
            target = "planning/refactor/04-readiness-decisions.json"
        elif rel == "decision-index.json":
            target = "planning/refactor/04-decision-index.json"
        else:
            target = rel
        files.append(row(path, target, "replace" if (ROOT / target).is_file() else "create"))
    patch_manifest = json.loads((RF05 / "patches/patch-manifest.json").read_text(encoding="utf-8"))
    files += patch_manifest["files"]
    retire = []
    for path in sorted((ROOT / ".agents").rglob("*")):
        if not path.is_file():
            continue
        rel = path.relative_to(ROOT).as_posix()
        if rel.startswith(".agents/skills/") or rel.startswith(".agents/references/") or rel == ".agents/rules/roadguard.md":
            retire.append({"source": rel, "action": "relocate_after_acceptance",
                           "target": "docs/history/agent-pre-rf05/" + rel[len(".agents/"):],
                           "source_sha256": sha(path)})
    manifest = {
        "package_version": "rf05-prepare-correction-3",
        "status": "PREPARED_INACTIVE",
        "branch": subprocess.check_output(["git", "branch", "--show-current"], cwd=ROOT, text=True).strip(),
        "head": subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=ROOT, text=True).strip(),
        "files": files, "retire_after_acceptance": retire,
        "keep_unchanged": [".agents/mcp_config.json", "docs/diagram/V2/09_Frontend/contracts/contract.lock.json",
                           "docs/postman/RoadGuardSystem-V2.postman_collection.json",
                           "docs/diagram/V2/ci/check_alignment.py",
                           "docs/diagram/V2/09_Frontend/contracts/check_contracts.py"],
    }
    (RF05 / "target-manifest.json").write_text(json.dumps(manifest, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    guidance = [r["target"] for r in files if r["target"] == "AGENTS.md" or r["target"].startswith(".agents/")]
    active = {"version": "rf05-proposed-3-correction", "status": "PROPOSED_INACTIVE",
              "acceptance_source": None, "accepted_at": None,
              "guidance_paths": guidance,
              "skills": [],
              "retired_guidance": [r["source"] for r in retire],
              "product_paths": ["docs/product/README.md", "docs/product/requirements.md",
                                "docs/product/confirmed-decisions.md", "docs/product/workflows.md"],
              "backend_paths": ["docs/backend/README.md", "docs/backend/persistence-and-operations.md"],
              "contract_paths": ["contracts/README.md", "contracts/events/README.md",
                                 "contracts/http/work-package.proposed.yaml"],
              "source_references": ["planning/refactor/01-endpoint-inventory.md",
                                    "planning/refactor/04-operation-crosswalk.json",
                                    "planning/refactor/04-source-crosswalk.json",
                                    "planning/refactor/templates/task-report.md",
                                    "planning/refactor/templates/coordination-note.md"]}
    (drafts / "agents/manifest.json.draft").write_text(json.dumps(active, indent=2) + "\n", encoding="utf-8")
    # Include the generated manifest itself in the reviewable target list.
    manifest["files"].append(row(drafts / "agents/manifest.json.draft", ".agents/manifest.json", "create"))
    (RF05 / "target-manifest.json").write_text(json.dumps(manifest, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(f"MANIFEST {len(manifest['files'])} proposed files, {len(retire)} old guidance files to relocate")


if __name__ == "__main__":
    main()
