"""Apply proposed files only in a disposable external staging directory."""

import hashlib
import json
import os
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
RF05 = ROOT / "planning/refactor/rf05"


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def copy_one(source, stage):
    target = stage / source.relative_to(ROOT)
    target.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(source, target)


def run(args, cwd=None, env=None):
    result = subprocess.run(args, cwd=cwd or ROOT, env=env, text=True, capture_output=True)
    return {"command": " ".join(str(x) for x in args), "exit": result.returncode,
            "stdout": result.stdout[-3000:], "stderr": result.stderr[-3000:]}


def main():
    manifest = json.loads((RF05 / "target-manifest.json").read_text(encoding="utf-8"))
    protected = [ROOT / r.get("source", r["target"]) for r in manifest["files"] if r["source_sha256"]]
    protected += [ROOT / r["source"] for r in manifest["retire_after_acceptance"]]
    before = {p.relative_to(ROOT).as_posix(): sha(p) for p in protected}
    evidence = {"branch": manifest["branch"], "head": manifest["head"], "staging": "temporary directory outside worktree",
                "checks": [], "protected_count": len(before)}
    patches = [str(ROOT / entry["patch"]) for entry in manifest["files"] if entry.get("patch")]
    patch_check = run(["git", "apply", "--check", *patches])
    evidence["checks"].append(patch_check)
    if patch_check["exit"]:
        raise ValueError("patch applicability drift")
    with tempfile.TemporaryDirectory(prefix="roadguard-rf05-") as tmp:
        stage = Path(tmp)
        for rel in ("planning/refactor", "docs/diagram/V2", "docs/product", "docs/backend",
                    "docs/decisions", "docs/adr", "contracts", "RoadGuardSystem.API/Controllers",
                    "RoadGuardSystem.DTOs/Projects", "RoadGuardSystem.BusinessObjects/Common",
                    "RoadGuardSystem.BusinessObjects/Entities", "tests/RoadGuardSystem.ApiTests",
                    "tests/RoadGuardSystem.IntegrationTests", "tests/RoadGuardSystem.UnitTests"):
            src = ROOT / rel
            if src.is_dir():
                shutil.copytree(src, stage / rel, ignore=shutil.ignore_patterns("*.zip", "__pycache__", "*.pyc", "bin", "obj", "TestResults"),
                                dirs_exist_ok=True)
        for rel in ("docs/RoadGuard_Project_Scope.md", "docs/RoadGuard_Backend_Scope.md",
                    "docs/postman/RoadGuardSystem-V2.postman_collection.json",
                    "docs/postman/RoadGuard.local.postman_environment.json",
                    "Directory.Build.props", ".agents/mcp_config.json",
                    "RoadGuardSystem.API/Program.cs", "RoadGuardSystem.API/RoadGuardSystem.API.http",
                    "AGENTS.md", "tests/CI/Verify-CiWorkflow.ps1"):
            copy_one(ROOT / rel, stage)
        # Copy other fingerprinted inputs, then verify every overlay against its original hash.
        for cross in ("04-source-crosswalk.json", "04-operation-crosswalk.json"):
            data = json.loads((ROOT / "planning/refactor" / cross).read_text(encoding="utf-8"))
            for rel in data["input_sha256"]:
                copy_one(ROOT / rel, stage)
        for entry in manifest["files"]:
            if entry["source_sha256"]:
                copy_one(ROOT / entry.get("source", entry["target"]), stage)
        for entry in manifest["files"]:
            target = stage / entry["target"]
            if entry["source_sha256"] and sha(target) != entry["source_sha256"]:
                raise ValueError(f"patch source drift: {entry['target']}")
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(ROOT / entry["draft"], target)
            if sha(target) != entry["proposed_sha256"]:
                raise ValueError(f"patch target hash mismatch: {entry['target']}")
        # The old discoverable guides are absent in stage. Keep their bytes only in the real checkout.
        evidence["checks"].append({"name": "manifest and exact-source overlay", "result": "PASS",
                                   "files": len(manifest["files"]), "retired_in_stage": len(manifest["retire_after_acceptance"])})
        env = dict(os.environ, ROADGUARD_ROOT=str(stage), ROADGUARD_GIT_ROOT=str(ROOT))
        py = sys.executable
        checks = [
            [py, str(stage / "planning/refactor/tools/build_rf04_sources.py")],
            [py, str(stage / "planning/refactor/tools/verify_rf04.py")],
            [py, str(stage / "planning/refactor/tools/test_rf04.py")],
            [py, str(stage / "planning/refactor/rf05/checks/verify_repository_readiness.py"), "--root", str(stage)],
            [py, str(stage / "planning/refactor/rf05/checks/verify_guidance.py"),
             "--root", str(stage), "--source-root", str(ROOT), "--staging", "--self-test"],
            [py, str(stage / "planning/refactor/rf05/checks/test_readiness.py")],
            [py, str(stage / "planning/refactor/rf05/checks/test_guidance.py")],
            [py, str(stage / "planning/refactor/rf05/checks/test_repository_readiness.py")],
            [py, str(stage / "planning/refactor/rf05/checks/test_readiness_flow.py"),
             "--root", str(stage), "--source-root", str(ROOT)],
        ]
        for command in checks:
            outcome = run(command, cwd=stage, env=env)
            evidence["checks"].append(outcome)
            if outcome["exit"]:
                break
        if all(c.get("exit", 0) == 0 for c in evidence["checks"]):
            crosswalk = json.loads((stage / "planning/refactor/04-source-crosswalk.json").read_text(encoding="utf-8"))
            staged_rows = [row for group in ("fr", "br", "us", "pf", "nfr") for row in crosswalk[group]]
            index = json.loads((stage / "planning/refactor/04-decision-index.json").read_text(encoding="utf-8"))
            registry = json.loads((stage / "planning/refactor/04-readiness-decisions.json").read_text(encoding="utf-8"))
            if index["decisions"] or registry["decisions"] or any(row["readiness"]["state"] != "BLOCKED" for row in staged_rows):
                raise ValueError("actual staged data unexpectedly gained readiness")
            evidence["checks"].append({"name": "actual staged readiness remains blocked", "result": "PASS",
                                       "requirements": len(staged_rows), "ready": 0, "indexed_decisions": 0})
            registry_path = stage / "planning/refactor/04-readiness-decisions.json"
            original = registry_path.read_bytes()
            try:
                registry_path.write_text(json.dumps({"schema_version": 1, "decisions": {"FR-UNKNOWN": {"state": "READY"}}}), encoding="utf-8")
                negative = run([py, str(stage / "planning/refactor/rf05/checks/verify_repository_readiness.py"),
                                "--root", str(stage)], cwd=stage, env=env)
                if negative["exit"] == 0 or "Unknown readiness IDs" not in negative["stderr"]:
                    raise ValueError(f"repository readiness negative check failed: {negative}")
                evidence["checks"].append({"name": "real-data validator rejects unknown registry ID", "result": "PASS"})
            finally:
                registry_path.write_bytes(original)
            crosswalk_path = stage / "planning/refactor/04-source-crosswalk.json"
            original_crosswalk = crosswalk_path.read_bytes()
            try:
                stale = json.loads(original_crosswalk)
                stale["fr"][0]["readiness"]["reason"] = "stale-output-fixture"
                crosswalk_path.write_text(json.dumps(stale), encoding="utf-8")
                negative = run([py, str(stage / "planning/refactor/rf05/checks/verify_repository_readiness.py"),
                                "--root", str(stage)], cwd=stage, env=env)
                if negative["exit"] == 0 or "generated crosswalk stale" not in negative["stderr"]:
                    raise ValueError(f"stale-output negative check failed: {negative}")
                evidence["checks"].append({"name": "real-data validator rejects stale generated output", "result": "PASS"})
            finally:
                crosswalk_path.write_bytes(original_crosswalk)
        # The CI file is checked as YAML and by the existing security verifier in staging.
        if all(c.get("exit", 0) == 0 for c in evidence["checks"]):
            import yaml
            workflow = yaml.safe_load((stage / ".github/workflows/ci.yml").read_text(encoding="utf-8"))
            verify_steps = [step.get("run", "") for step in workflow["jobs"]["verify"]["steps"]]
            required = ("Verify-P102Docs.ps1", "Verify-AgentSetup.ps1", "test_readiness.py",
                        "test_guidance.py", "test_repository_readiness.py", "verify_repository_readiness.py")
            if not all(any(term in step for step in verify_steps) for term in required):
                raise ValueError("CI proposed guidance steps missing")
            evidence["checks"].append({"name": "CI YAML parse and retained verify steps", "result": "PASS"})
            if shutil.which("pwsh"):
                active_manifest_path = stage / ".agents/manifest.json"
                simulated = json.loads(active_manifest_path.read_text(encoding="utf-8"))
                simulated.update(status="ACTIVE_AFTER_OWNER_ACCEPTANCE",
                                 acceptance_source="STAGING_SIMULATION_ONLY", accepted_at="STAGING_SIMULATION_ONLY")
                active_manifest_path.write_text(json.dumps(simulated, indent=2) + "\n", encoding="utf-8")
                evidence["checks"].append(run(["pwsh", "-File", str(stage / "tests/Documentation/Verify-P102Docs.ps1"), "-SelfTestNegative"], cwd=stage))
                evidence["checks"].append(run(["pwsh", "-File", str(stage / "tests/Tooling/Verify-AgentSetup.ps1"), "-SelfTest"], cwd=stage))
                evidence["checks"].append(run(["pwsh", "-File", str(ROOT / "tests/CI/Verify-CiWorkflow.ps1"),
                                               "-RepoRoot", str(stage)], cwd=stage))
            else:
                evidence["checks"].append({"name": "CI PowerShell verifier", "result": "NOT RUN", "reason": "pwsh unavailable"})
    after = {p.relative_to(ROOT).as_posix(): sha(p) for p in protected}
    evidence["protected_unchanged"] = before == after
    evidence["status"] = "PASS" if evidence["protected_unchanged"] and all(
        c.get("exit", 0) == 0 and c.get("result", "PASS") != "FAIL" for c in evidence["checks"]) else "FAIL"
    (RF05 / "checks/staging-result.json").write_text(json.dumps(evidence, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(json.dumps({"status": evidence["status"], "checks": [(c.get("name") or c.get("command"), c.get("exit", c.get("result"))) for c in evidence["checks"]],
                      "protected_unchanged": evidence["protected_unchanged"]}, ensure_ascii=False))
    if evidence["status"] != "PASS":
        raise SystemExit(1)


if __name__ == "__main__":
    main()
