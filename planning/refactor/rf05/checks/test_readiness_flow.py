"""Exercise the proposed generator and read-only validator on a disposable repository."""

import argparse
import hashlib
import json
import os
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path


def copy(source, target, relative):
    src = source / relative
    dst = target / relative
    dst.parent.mkdir(parents=True, exist_ok=True)
    if src.is_dir():
        shutil.copytree(src, dst, dirs_exist_ok=True)
    else:
        shutil.copy2(src, dst)


def run(root, script, expected=None):
    env = dict(os.environ, ROADGUARD_ROOT=str(root), ROADGUARD_GIT_ROOT=str(SOURCE))
    result = subprocess.run([sys.executable, str(root / script), *( ["--root", str(root)] if script.endswith("verify_repository_readiness.py") else [])],
                            cwd=root, env=env, capture_output=True, text=True)
    output = result.stdout + result.stderr
    if expected is None and result.returncode:
        raise AssertionError(f"{script} unexpectedly failed: {output[-1800:]}")
    if expected is not None and (result.returncode == 0 or expected not in output):
        raise AssertionError(f"{script} expected {expected!r}, got exit={result.returncode}: {output[-1800:]}")
    return output


def save(path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def row_for(root, ident):
    data = json.loads((root / "planning/refactor/04-source-crosswalk.json").read_text(encoding="utf-8"))
    return next(row for group in ("fr", "br", "us", "pf", "nfr") for row in data[group] if row["id"] == ident)


def check():
    with tempfile.TemporaryDirectory(prefix="roadguard-rf05-flow-") as tmp:
        root = Path(tmp)
        for rel in ("planning/refactor/tools", "planning/refactor/rf05/checks/verify_repository_readiness.py",
                    "planning/refactor/02-decision-register.md", "planning/refactor/03-agent-design.md",
                    "planning/refactor/00-tooling-dependencies.md", "planning/refactor/04-operation-crosswalk.json",
                    "planning/refactor/04-readiness-decisions.json", "planning/refactor/04-decision-index.json",
                    "docs/diagram/V2", "docs/adr", "docs/product/README.md", "docs/product/data-and-quality.md",
                    "docs/backend/README.md", "contracts/events/README.md", "docs/RoadGuard_Project_Scope.md",
                    "docs/RoadGuard_Backend_Scope.md", "docs/postman/RoadGuardSystem-V2.postman_collection.json",
                    "RoadGuardSystem.API/RoadGuardSystem.API.http", "AGENTS.md"):
            copy(STAGE, root, rel)
        (root / ".agents").mkdir(exist_ok=True)
        generator = "planning/refactor/tools/build_rf04_sources.py"
        validator = "planning/refactor/rf05/checks/verify_repository_readiness.py"
        run(root, generator)
        first = row_for(root, "FR-02")
        if len(first["claims"]) < 2 or first["readiness"]["state"] != "BLOCKED":
            raise AssertionError("fixture must start with at least two blocked historical claims")
        print(f"BASELINE {first['id']}: {len(first['claims'])} historical claims, BLOCKED")
        decision_path = root / "planning/refactor/fixture-owner-decision.md"
        decision_path.write_text("## New scoped decision 2026-10-01\n"
                                 "Status: Accepted by fixture-owner on 2026-10-01.\n\n"
                                 "| ID | Meaning | Scope |\n|---|---|---|\n"
                                 "| RF05-FIXTURE-NEW | Confirm FR-02 historical claims for scoped implementation. | FR-02 complete historical section |\n",
                                 encoding="utf-8")
        decision = {"status": "Accepted", "source": "planning/refactor/fixture-owner-decision.md",
                    "anchor": "new-scoped-decision-2026-10-01",
                    "status_line": "Status: Accepted by fixture-owner on 2026-10-01.",
                    "confirmed_by": "fixture-owner", "confirmed_at": "2026-10-01",
                    "content": "Confirm FR-02 historical claims for scoped implementation.",
                    "requirements": {"FR-02": {"scope": "FR-02 complete historical section",
                                                "claim_sha256": {c["id"]: hashlib.sha256(c["content"].encode("utf-8")).hexdigest()
                                                                 for c in first["claims"]}}}}
        scope = {"decision_id": "RF05-FIXTURE-NEW", "decision_source": f"{decision['source']}#{decision['anchor']}",
                 "decision_claim": decision["content"], "scope": "FR-02 complete historical section",
                 "claim_ids": [c["id"] for c in first["claims"]]}
        index_path = root / "planning/refactor/04-decision-index.json"
        registry_path = root / "planning/refactor/04-readiness-decisions.json"
        cross_path = root / "planning/refactor/04-source-crosswalk.json"
        index = {"schema_version": 1, "decisions": {"RF05-FIXTURE-NEW": decision}}
        registry = {"schema_version": 1, "decisions": {"FR-02": {"state": "READY", "approved_scopes": [scope],
                                                            "required_evidence": ["Planned scoped tests; not executed"]}}}
        save(index_path, index)
        save(registry_path, registry)
        run(root, generator)
        ready = row_for(root, "FR-02")
        assert ready["source_authority"] == "HISTORICAL_SOURCE_ONLY"
        assert ready["authority"] == "TARGET_CONFIRMED_BY_DECISION"
        assert ready["implementation_readiness"] == "READY"
        assert all(c["authority"] == "HISTORICAL_SOURCE_ONLY" and c["current_authority"] == "TARGET_CONFIRMED_BY_DECISION"
                   for c in ready["claims"])
        run(root, validator)
        print(f"ACCEPTED {ready['id']}: historical origin retained, confirmed authority, READY, real-data validator PASS")
        generated_before = cross_path.read_bytes()
        run(root, generator)
        assert cross_path.read_bytes() == generated_before
        run(root, validator)
        print("REGENERATION stable; decision content and READY retained")

        # Each negative mutates one input, checks a precise failure, and restores it.
        cases = []
        def rejected(name, path, value, command, error):
            original = path.read_bytes()
            try:
                save(path, value)
                run(root, command, expected=error)
                cases.append(name)
            finally:
                path.write_bytes(original)

        for state in ("Proposed", "Unknown"):
            changed = json.loads(index_path.read_text(encoding="utf-8"))
            changed["decisions"]["RF05-FIXTURE-NEW"]["status"] = state
            rejected(state, index_path, changed, generator, "decision is not Accepted")
        changed = json.loads(registry_path.read_text(encoding="utf-8"))
        changed["decisions"]["FR-02"]["approved_scopes"][0]["decision_source"] = "docs/product/historical-fr-br.md#fr-02"
        rejected("historical source", registry_path, changed, generator, "source/anchor mismatch")
        changed["decisions"]["FR-02"]["approved_scopes"][0]["decision_source"] = "planning/refactor/fixture-owner-decision.md#wrong"
        rejected("wrong anchor", registry_path, changed, generator, "source/anchor mismatch")
        source_backup = decision_path.read_bytes()
        try:
            decision_path.unlink()
            run(root, generator, expected="missing decision source")
            cases.append("missing decision source")
        finally:
            decision_path.write_bytes(source_backup)
        changed["decisions"]["FR-02"]["approved_scopes"][0]["decision_id"] = "UNRELATED"
        rejected("unrelated decision", registry_path, changed, generator, "unrelated to requirement")
        changed = json.loads(registry_path.read_text(encoding="utf-8"))
        changed["decisions"]["FR-02"]["approved_scopes"][0]["claim_ids"] = ["FR-99.C01"]
        rejected("unrelated claim", registry_path, changed, generator, "claim_ids unrelated")
        changed = json.loads(index_path.read_text(encoding="utf-8"))
        changed["decisions"]["RF05-FIXTURE-NEW"]["requirements"]["FR-02"]["scope"] = "unrecorded scope"
        rejected("scope absent from source", index_path, changed, generator, "scope unrelated")
        source_path = root / first["source"]
        source_original = source_path.read_bytes()
        try:
            source_path.write_text(source_path.read_text(encoding="utf-8").replace(first["claims"][0]["content"],
                                                                                    first["claims"][0]["content"] + " changed", 1), encoding="utf-8")
            run(root, generator, expected="confirmed claim content changed")
            cases.append("changed claim")
        finally:
            source_path.write_bytes(source_original)
        changed = json.loads(cross_path.read_text(encoding="utf-8"))
        row = next(r for r in changed["fr"] if r["id"] == "FR-02")
        row["authority"] = "HISTORICAL_SOURCE_ONLY"
        rejected("forged output", cross_path, changed, validator, "generated crosswalk stale: FR-02")
        changed = json.loads(registry_path.read_text(encoding="utf-8"))
        changed["decisions"]["FR-02"]["state"] = "BLOCKED"
        changed["decisions"]["FR-02"]["reason"] = "forged"
        changed["decisions"]["FR-02"]["checkpoint"] = "forged"
        rejected("forged registry", registry_path, changed, validator, "source fingerprint stale: planning/refactor/04-readiness-decisions.json")
        original_decision = decision_path.read_bytes()
        try:
            decision_path.write_text(decision_path.read_text(encoding="utf-8").replace("Confirm FR-02 historical", "Changed FR-02 historical"), encoding="utf-8")
            run(root, validator, expected="decision claim does not match")
            cases.append("stale output after decision change")
        finally:
            decision_path.write_bytes(original_decision)
        partial_index = json.loads(index_path.read_text(encoding="utf-8"))
        partial_registry = json.loads(registry_path.read_text(encoding="utf-8"))
        partial_claims = partial_index["decisions"]["RF05-FIXTURE-NEW"]["requirements"]["FR-02"]["claim_sha256"]
        for claim in first["claims"][1:]:
            partial_claims.pop(claim["id"])
        partial_registry["decisions"]["FR-02"] = {"state": "BLOCKED", "approved_scopes": [{**scope, "claim_ids": [first["claims"][0]["id"]]}],
                                                   "reason": "remaining claims unconfirmed", "checkpoint": "RF-10 START"}
        save(index_path, partial_index)
        save(registry_path, partial_registry)
        run(root, generator)
        partial = row_for(root, "FR-02")
        assert partial["authority"] == "PARTIALLY_CONFIRMED_BY_DECISION" and partial["implementation_readiness"] == "BLOCKED"
        assert partial["claims"][0]["current_authority"] == "TARGET_CONFIRMED_BY_DECISION"
        assert all(c["current_authority"] == "HISTORICAL_SOURCE_ONLY" for c in partial["claims"][1:])
        run(root, validator)
        print(f"PARTIAL {partial['id']}: one claim confirmed, {len(partial['claims'])-1} blocked, validator PASS")
        print(f"REJECTIONS {len(cases)}: {', '.join(cases)}")


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, required=True, help="Proposed staging repository")
    parser.add_argument("--source-root", type=Path, required=True, help="Real Git root for metadata only")
    args = parser.parse_args()
    STAGE = args.root.resolve()
    SOURCE = args.source_root.resolve()
    check()
