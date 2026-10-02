"""Apply the owner-accepted RF-05 package with exact-hash preflight and scoped recovery."""

import argparse
import hashlib
import json
import os
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
RF05 = ROOT / "planning/refactor/rf05"
MANIFEST = RF05 / "target-manifest.json"
ACCEPTED_SHA256 = "764bec3cac0450fe4219fa0e41168795efb0bc7120569c691354e587540c0448"
ACCEPTANCE_SOURCE = ("User message 2026-09-30 accepting rf05-prepare-correction-3, "
                     f"target manifest SHA-256 {ACCEPTED_SHA256}")
GENERATED = ["planning/refactor/04-source-crosswalk.json", "planning/refactor/04-source-crosswalk.md",
             "docs/product/historical-fr-br.md", "docs/product/historical-us.md",
             "docs/product/historical-pf.md", "docs/product/historical-nfr.md",
             "docs/product/confirmed-decisions.md"]


def sha(data):
    return hashlib.sha256(data).hexdigest()


def inside(relative):
    path = (ROOT / relative).resolve()
    if not path.is_relative_to(ROOT.resolve()):
        raise ValueError(f"Path outside repository: {relative}")
    return path


def check(manifest):
    if sha(MANIFEST.read_bytes()) != ACCEPTED_SHA256:
        raise ValueError("Accepted target manifest hash changed")
    if manifest["package_version"] != "rf05-prepare-correction-3" or len(manifest["files"]) != 16 or len(manifest["retire_after_acceptance"]) != 22:
        raise ValueError("Unexpected package version or target count")
    branch = subprocess.check_output(["git", "branch", "--show-current"], cwd=ROOT, text=True).strip()
    head = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=ROOT, text=True).strip()
    if branch != manifest["branch"] or head != manifest["head"]:
        raise ValueError(f"Branch/HEAD drift: {branch} {head}")
    stage = json.loads((RF05 / "checks/staging-result.json").read_text(encoding="utf-8"))
    if stage["status"] != "PASS" or not stage["protected_unchanged"]:
        raise ValueError("Prepared staging verification is not PASS")
    seen = set()
    for entry in manifest["files"]:
        target = inside(entry["target"])
        draft = inside(entry["draft"])
        if target in seen:
            raise ValueError(f"Duplicate target: {target}")
        seen.add(target)
        if not draft.is_file() or sha(draft.read_bytes()) != entry["proposed_sha256"]:
            raise ValueError(f"Draft hash drift: {entry['target']}")
        if entry["source_sha256"]:
            if not target.is_file() or sha(target.read_bytes()) != entry["source_sha256"]:
                raise ValueError(f"Source hash drift: {entry['target']}")
        elif target.exists():
            raise ValueError(f"Create target already exists: {entry['target']}")
    for entry in manifest["retire_after_acceptance"]:
        source, target = inside(entry["source"]), inside(entry["target"])
        if source in seen or target in seen or target.exists():
            raise ValueError(f"Retirement path conflict: {entry['source']}")
        seen.add(source)
        seen.add(target)
        if not source.is_file() or sha(source.read_bytes()) != entry["source_sha256"]:
            raise ValueError(f"Retirement source hash drift: {entry['source']}")
    unchanged = {name: sha(inside(name).read_bytes()) for name in manifest["keep_unchanged"]}
    return branch, head, unchanged


def run(command, env=None):
    result = subprocess.run(command, cwd=ROOT, env=env, capture_output=True, text=True)
    if result.returncode:
        raise ValueError(f"Check failed: {' '.join(command)}\n{result.stdout[-1200:]}\n{result.stderr[-1200:]}")
    return {"command": " ".join(command), "stdout": result.stdout.strip(), "stderr": result.stderr.strip()}


def apply(manifest, branch, head, unchanged):
    saved = {name: inside(name).read_bytes() for name in GENERATED}
    saved.update({entry["target"]: inside(entry["target"]).read_bytes()
                  for entry in manifest["files"] if entry["source_sha256"]})
    moved = []
    created = []
    checks = []
    try:
        for entry in manifest["retire_after_acceptance"]:
            source, target = inside(entry["source"]), inside(entry["target"])
            target.parent.mkdir(parents=True, exist_ok=True)
            source.rename(target)
            moved.append(entry)
        for entry in manifest["files"]:
            target = inside(entry["target"])
            target.parent.mkdir(parents=True, exist_ok=True)
            data = inside(entry["draft"]).read_bytes()
            if entry["target"] == ".agents/manifest.json":
                active = json.loads(data)
                active.update(status="ACTIVE_AFTER_OWNER_ACCEPTANCE",
                              accepted_at="2026-09-30", acceptance_source=ACCEPTANCE_SOURCE)
                data = (json.dumps(active, ensure_ascii=False, indent=2) + "\n").encode("utf-8")
            target.write_bytes(data)
            if not entry["source_sha256"]:
                created.append(entry["target"])
            if entry["target"] != ".agents/manifest.json" and sha(target.read_bytes()) != entry["proposed_sha256"]:
                raise ValueError(f"Applied target hash mismatch: {entry['target']}")
        env = dict(os.environ, ROADGUARD_ROOT=str(ROOT), ROADGUARD_GIT_ROOT=str(ROOT))
        checks.append(run([sys.executable, "planning/refactor/tools/build_rf04_sources.py"], env))
        for command in ([sys.executable, "planning/refactor/tools/verify_rf04.py"],
                        [sys.executable, "planning/refactor/tools/test_rf04.py"],
                        [sys.executable, "planning/refactor/rf05/checks/test_readiness.py"],
                        [sys.executable, "planning/refactor/rf05/checks/test_guidance.py"],
                        [sys.executable, "planning/refactor/rf05/checks/test_repository_readiness.py"],
                        [sys.executable, "planning/refactor/rf05/checks/verify_repository_readiness.py", "--root", str(ROOT)],
                        [sys.executable, "planning/refactor/rf05/checks/verify_guidance.py", "--root", str(ROOT), "--mode", "all", "--self-test"],
                        [sys.executable, "planning/refactor/rf05/checks/test_readiness_flow.py", "--root", str(ROOT), "--source-root", str(ROOT)],
                        ["pwsh", "-File", "tests/Documentation/Verify-P102Docs.ps1", "-SelfTestNegative"],
                        ["pwsh", "-File", "tests/Tooling/Verify-AgentSetup.ps1", "-SelfTest"],
                        ["pwsh", "-File", "tests/CI/Verify-CiWorkflow.ps1", "-RepoRoot", str(ROOT)]):
            checks.append(run(command, env))
        if any(sha(inside(name).read_bytes()) != digest for name, digest in unchanged.items()):
            raise ValueError("Keep-unchanged guard changed")
        for entry in moved:
            if inside(entry["source"]).exists() or sha(inside(entry["target"]).read_bytes()) != entry["source_sha256"]:
                raise ValueError(f"Retirement verification failed: {entry['source']}")
        result = {"status": "PASS", "branch": branch, "head": head,
                  "accepted_manifest_sha256": ACCEPTED_SHA256, "acceptance_source": ACCEPTANCE_SOURCE,
                  "target_count": len(manifest["files"]), "retired_count": len(moved),
                  "active_manifest_sha256": sha(inside(".agents/manifest.json").read_bytes()),
                  "unchanged_sha256": unchanged,
                  "target_sha256": {entry["target"]: sha(inside(entry["target"]).read_bytes()) for entry in manifest["files"]},
                  "checks": checks, "github_ci": "NOT_RUN", "runtime_tests": "NOT_RUN"}
        (RF05 / "activation-result.json").write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        print(f"ACTIVATED {len(manifest['files'])} targets; retired {len(moved)} files; {len(checks)} checks PASS")
    except Exception:
        for name, data in saved.items():
            target = inside(name)
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(data)
        for name in created:
            inside(name).unlink(missing_ok=True)
        for entry in reversed(moved):
            source, target = inside(entry["source"]), inside(entry["target"])
            source.parent.mkdir(parents=True, exist_ok=True)
            target.rename(source)
        raise


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--apply", action="store_true", help="Apply accepted package after preflight")
    args = parser.parse_args()
    manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
    branch, head, unchanged = check(manifest)
    print(f"PREFLIGHT PASS: {branch} {head}, 16 targets, 22 retirement files, accepted manifest hash")
    if args.apply:
        apply(manifest, branch, head, unchanged)


if __name__ == "__main__":
    main()
