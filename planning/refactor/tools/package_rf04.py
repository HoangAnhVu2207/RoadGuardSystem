"""Build a source-free, repository-relative RF-04 review ZIP and manifest."""

from __future__ import annotations

import hashlib
import json
import subprocess
import sys
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
PLAN = ROOT / "planning/refactor"
MANIFEST = PLAN / "04-handoff-manifest.json"
ZIP = PLAN / "reports/RF-04-correction-handoff.zip"


def run(*args):
    result = subprocess.run(args, cwd=ROOT, text=True, capture_output=True, check=False)
    if result.returncode:
        raise RuntimeError(f"{' '.join(args)} failed: {result.stdout}\n{result.stderr}")
    return (result.stdout + result.stderr).strip()


def files():
    paths = []
    for directory in ("docs/product", "docs/backend", "docs/decisions", "contracts"):
        paths.extend(p for p in (ROOT / directory).rglob("*") if p.is_file())
    paths.extend(p for p in PLAN.glob("04-*") if p.is_file() and p != MANIFEST)
    paths.extend(p for p in (PLAN / "tools").glob("*.py") if p.is_file())
    paths.extend(p for p in (PLAN / "coordination").glob("RF-04-*.md") if p.is_file())
    paths.extend([PLAN / "reports/RF-04.md", PLAN / "reports/RF-04-correction.md",
                  PLAN / "tasks/RF-04-documentation-contract-crosswalk.md", PLAN / "README.md"])
    return sorted(set(paths))


def main():
    checks = {
        "verify_rf04": run(sys.executable, "planning/refactor/tools/verify_rf04.py"),
        "test_rf04": run(sys.executable, "planning/refactor/tools/test_rf04.py"),
        "git_diff_check": run("git", "diff", "--check") or "exit 0",
    }
    included = files()
    manifest = {
        "status": "DRAFT_RF04_NOT_ACTIVE",
        "branch": run("git", "branch", "--show-current"),
        "head": run("git", "rev-parse", "HEAD"),
        "dirty": bool(run("git", "status", "--porcelain")),
        "files": [{"path": p.relative_to(ROOT).as_posix(),
                   "sha256": hashlib.sha256(p.read_bytes()).hexdigest()} for p in included],
        "read_only_pilot_evidence_sha256": {
            name: hashlib.sha256((ROOT / name).read_bytes()).hexdigest() for name in [
                "RoadGuardSystem.API/Controllers/ProjectWorkPackagesController.cs",
                "RoadGuardSystem.DTOs/Projects/ProjectWorkPackageResponseDto.cs",
                "RoadGuardSystem.DTOs/Projects/RoadSectionWorkPackageDto.cs",
                "RoadGuardSystem.DTOs/Projects/WarrantyWorkPackageDto.cs",
                "RoadGuardSystem.Services/Implementations/Projects/ProjectWorkPackageService.cs",
                "RoadGuardSystem.BusinessObjects/Common/Enums.cs",
                "RoadGuardSystem.BusinessObjects/Common/Extensions/UserRoleCodeExtensions.cs",
                "RoadGuardSystem.BusinessObjects/Common/Extensions/WarrantyScopeExtensions.cs",
            ]},
        "verification": checks,
        "not_run": ["API/SQL", "full formal OpenAPI validator", "FE lock", "external provider/Android", "peer review"],
        "scope": "RF-04 inactive drafts, generators, validators, coordination and reports only",
    }
    MANIFEST.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    with zipfile.ZipFile(ZIP, "w", compression=zipfile.ZIP_DEFLATED) as archive:
        for path in included + [MANIFEST]:
            archive.write(path, path.relative_to(ROOT).as_posix())
    with zipfile.ZipFile(ZIP) as archive:
        names = archive.namelist()
        assert len(names) == len(set(names)) and len(names) == len(included) + 1
        assert all(not name.endswith((".cs", ".sql", ".db")) for name in names)
        for item in manifest["files"]:
            assert hashlib.sha256(archive.read(item["path"])).hexdigest() == item["sha256"]
    print(f"PASS: {len(names)} relative entries; {ZIP.relative_to(ROOT).as_posix()}; {ZIP.stat().st_size} bytes")


if __name__ == "__main__":
    main()
