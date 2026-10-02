#!/usr/bin/env python3
"""
Generate manifest for RF-10 Checkpoint 13 Finalization Handoff
Computes accurate file sizes and SHA-256 hashes from actual file bytes
"""

import json
import hashlib
from pathlib import Path
from datetime import datetime

def compute_sha256(filepath):
    """Compute SHA-256 hash of a file"""
    sha256 = hashlib.sha256()
    with open(filepath, 'rb') as f:
        for chunk in iter(lambda: f.read(8192), b''):
            sha256.update(chunk)
    return sha256.hexdigest()

def get_file_info(filepath):
    """Get file info: path, size, hash"""
    return {
        "path": str(filepath),
        "size_bytes": filepath.stat().st_size,
        "sha256": compute_sha256(filepath)
    }

def main():
    base_dir = Path('.')

    # Package metadata
    manifest = {
        "package": {
            "name": "RF-10-checkpoint-13-finalization-handoff",
            "version": "finalization",
            "date": datetime.now().strftime("%Y-%m-%d"),
            "writer": "BOX 3",
            "purpose": "Complete checkpoint 13 documentation with findings ledger, provenance report, and full evidence base"
        },
        "test_results": {
            "box1": {
                "id": "RF-10-08-C01",
                "name": "IdempotencyPerCommandCharacterizationTests",
                "total": 10,
                "passed": 10,
                "failed": 0,
                "duration": "5s",
                "evidence_source": "correction-04"
            },
            "box2": {
                "id": "RF-10-09-C01",
                "name": "Rf1009NotificationInboxCharacterizationTests",
                "total": 5,
                "passed": 5,
                "failed": 0,
                "duration": "2s",
                "evidence_source": "correction-03 (reused)"
            },
            "overall": {
                "total": 15,
                "passed": 15,
                "failed": 0,
                "pass_rate": "100%"
            }
        },
        "build": {
            "warnings": 180,
            "errors": 0,
            "status": "success"
        },
        "findings": [
            {
                "id": "F-C13-01a",
                "status": "FIXED",
                "description": "Upload Create Replay - Receipt + Scoped effect-set",
                "coverage": "correction-04",
                "evidence": "BOX 1 correction-04",
                "open_issues": []
            },
            {
                "id": "F-C13-01b",
                "status": "FIXED",
                "description": "SurveyPlan Create Replay - Receipt + Scoped effect-set",
                "coverage": "correction-04",
                "evidence": "BOX 1 correction-04",
                "open_issues": []
            },
            {
                "id": "F-C13-01c",
                "status": "FIXED",
                "description": "Role Revocation Replay Authorization with new JWT",
                "coverage": "correction-02",
                "evidence": "BOX 1 correction-02",
                "open_issues": [
                    "NOT_VERIFIED: original JWT behavior after role change (F-C13-01c-HISTORICAL)"
                ]
            },
            {
                "id": "F-C13-02",
                "status": "FIXED",
                "description": "Postpone Receipt Snapshot Timing + Immutability",
                "coverage": "correction-04",
                "evidence": "BOX 1 correction-04",
                "open_issues": []
            },
            {
                "id": "F-C13-03",
                "status": "FIXED",
                "description": "Consumer replay with candidate ID + fresh DbContext",
                "coverage": "correction-03",
                "evidence": "BOX 2 correction-03",
                "open_issues": []
            },
            {
                "id": "F-C13-04",
                "status": "FIXED",
                "description": "MarkRead ReadAt/RowVersion + State chain immutability",
                "coverage": "correction-03",
                "evidence": "BOX 2 correction-03",
                "open_issues": []
            }
        ],
        "payload": {}
    }

    # Collect test files
    tests_dir = base_dir / 'tests'
    if tests_dir.exists():
        manifest["payload"]["tests"] = [
            get_file_info(f) for f in sorted(tests_dir.glob('*.cs'))
        ]

    # Collect fixture files
    fixtures_dir = base_dir / 'fixtures'
    if fixtures_dir.exists():
        manifest["payload"]["fixtures"] = [
            get_file_info(f) for f in sorted(fixtures_dir.glob('*.cs'))
        ]

    # Collect production files
    production_dir = base_dir / 'production'
    if production_dir.exists():
        manifest["payload"]["production"] = [
            get_file_info(f) for f in sorted(production_dir.glob('*.cs'))
        ]

    # Collect evidence files
    evidence_dir = base_dir / 'evidence'
    if evidence_dir.exists():
        manifest["payload"]["evidence"] = [
            get_file_info(f) for f in sorted(evidence_dir.glob('*'))
            if f.is_file()
        ]

    # Collect source snapshots
    snapshots_dir = base_dir / 'source-snapshots'
    if snapshots_dir.exists():
        manifest["payload"]["source_snapshots"] = [
            get_file_info(f) for f in sorted(snapshots_dir.glob('*.sha256'))
        ]

    # Collect reports
    reports_dir = base_dir / 'reports'
    if reports_dir.exists():
        manifest["payload"]["reports"] = [
            get_file_info(f) for f in sorted(reports_dir.glob('*.md'))
        ]

    # Collect documentation
    docs_dir = base_dir / 'documentation'
    if docs_dir.exists():
        manifest["payload"]["documentation"] = [
            get_file_info(f) for f in sorted(docs_dir.glob('*.md'))
        ]

    # Source provenance
    manifest["source_provenance"] = {
        "box1": {
            "before_build": "6f0bebf1bb2ad34abe8efce2b511556fdd4e901b118bc5e055fb57f2023aceb1",
            "after_build": "a7bbae78744e69ca028c4f6e727637ae3872c0144537b5f3c94619b9e5c3789f",
            "changes": "correction-04 (postpone snapshot timing, upload/survey scoped counts)",
            "limitation": "before-build-to-test linkage not confirmed"
        },
        "box2": {
            "consistent_hash": "f699530832800c4b247699e19a4fe56cdb88a0cf422966c07e5ce3bd339e986e",
            "status": "unchanged from correction-03",
            "evidence_reuse": "TRX from correction-03 run"
        }
    }

    # Historical limitations
    manifest["historical_limitations"] = [
        "NOT_VERIFIED: Original JWT behavior after role change (F-C13-01c-HISTORICAL)",
        "Survey checkpoint 08 inspection provenance",
        "Dispatcher/delivery workflow (not yet implemented)",
        "Before-build provenance for correction-02 (placeholder hashes)",
        "Before-build-to-test linkage for correction-04 BOX 1",
        "Assembly hashes not captured in correction-04"
    ]

    # Constraints compliance
    manifest["constraints_compliance"] = {
        "no_production_changes": True,
        "no_schema_changes": True,
        "no_contract_changes": True,
        "no_migration_changes": True,
        "no_ci_changes": True,
        "no_git_operations": True,
        "no_shared_database_writes": True,
        "isolated_fixtures_only": True,
        "historical_evidence_preserved": True
    }

    # Checkpoint status
    manifest["checkpoint_status"] = {
        "checkpoint_13": "COMPLETE",
        "rf_10_parent": "PARTIAL",
        "findings_closed": "6/6",
        "not_verified_items": 6,
        "pass_rate": "100%"
    }

    # Write manifest
    with open('MANIFEST.json', 'w', encoding='utf-8') as f:
        json.dump(manifest, f, indent=2, ensure_ascii=False)

    print("Manifest generated successfully")

    # Count total payload files
    total_files = sum(len(files) for files in manifest["payload"].values())
    print(f"Total payload files: {total_files}")

    # Calculate total size
    total_size = sum(
        f["size_bytes"]
        for category in manifest["payload"].values()
        for f in category
    )
    print(f"Total payload size: {total_size:,} bytes ({total_size / 1024:.1f} KB)")

if __name__ == "__main__":
    main()
