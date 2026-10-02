"""Package only RF-05 review artifacts, with relative paths and SHA-256."""

import hashlib
import json
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
RF05 = ROOT / "planning/refactor/rf05"
REPORTS = ROOT / "planning/refactor/reports"
ZIP = REPORTS / "RF-05-prepare-correction-3-handoff.zip"
MANIFEST = RF05 / "handoff-correction-3-manifest.json"


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    required = [
        ROOT / "planning/refactor/reports/RF-05.md",
        ROOT / "planning/refactor/reports/RF-05-prepare-correction.md",
        ROOT / "planning/refactor/reports/RF-05-prepare-correction-3.md",
        ROOT / "planning/refactor/tasks/RF-05-agent-tooling-adoption.md",
        ROOT / "planning/refactor/reports/RF-04-correction.md",
        ROOT / "planning/refactor/04-operation-crosswalk.json",
        ROOT / "planning/refactor/04-source-crosswalk.json",
        ROOT / "planning/refactor/templates/task-report.md",
        ROOT / "planning/refactor/templates/coordination-note.md",
    ]
    files = sorted([p for p in RF05.rglob("*") if p.is_file() and p != MANIFEST and p.name != "handoff-manifest.json" and
                    "__pycache__" not in p.parts and p.suffix != ".zip" and
                    p.name not in {"handoff-correction-manifest.json", "handoff-correction-3-manifest.json"}] + required)
    if len(files) != len(set(files)):
        raise ValueError("duplicate package file")
    rows = [{"path": p.relative_to(ROOT).as_posix(), "sha256": sha(p), "bytes": p.stat().st_size} for p in files]
    manifest = {"status": "RF05_PREPARE_CORRECTION_INACTIVE", "package_version": "rf05-prepare-correction-3", "files": rows,
                "verification": "planning/refactor/rf05/checks/staging-result.json",
                "active_changes": "NONE", "runtime_tests": "NOT_RUN"}
    MANIFEST.write_text(json.dumps(manifest, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    with zipfile.ZipFile(ZIP, "w", compression=zipfile.ZIP_DEFLATED) as archive:
        for path in [*files, MANIFEST]:
            archive.write(path, path.relative_to(ROOT).as_posix())
    with zipfile.ZipFile(ZIP) as archive:
        if archive.testzip() is not None:
            raise ValueError("ZIP integrity failure")
        for row in rows:
            if hashlib.sha256(archive.read(row["path"])).hexdigest() != row["sha256"]:
                raise ValueError(f"ZIP hash mismatch: {row['path']}")
        if any(name.endswith((".cs", ".sql", ".db")) for name in archive.namelist()):
            raise ValueError("production/data source included in ZIP")
    print(f"ZIP {ZIP.relative_to(ROOT).as_posix()} files={len(rows)+1} bytes={ZIP.stat().st_size} sha256={sha(ZIP)}")


if __name__ == "__main__":
    main()
