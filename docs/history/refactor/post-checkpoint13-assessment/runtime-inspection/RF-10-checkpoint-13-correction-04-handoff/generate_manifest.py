import os
import hashlib
import json
from pathlib import Path

def compute_sha256(filepath):
    """Compute SHA-256 hash of a file."""
    sha256 = hashlib.sha256()
    with open(filepath, 'rb') as f:
        for chunk in iter(lambda: f.read(8192), b''):
            sha256.update(chunk)
    return sha256.hexdigest()

def get_file_size(filepath):
    """Get file size in bytes."""
    return os.path.getsize(filepath)

def collect_files(base_dir):
    """Collect all files with their metadata."""
    files_data = {
        'tests': [],
        'fixtures': [],
        'production': [],
        'evidence': [],
        'reports': [],
        'documentation': []
    }

    for category in files_data.keys():
        category_path = Path(base_dir) / category
        if category_path.exists():
            for filepath in category_path.rglob('*'):
                if filepath.is_file():
                    rel_path = filepath.relative_to(base_dir)
                    file_info = {
                        'path': str(rel_path).replace('\\', '/'),
                        'size_bytes': get_file_size(filepath),
                        'sha256': compute_sha256(filepath)
                    }
                    files_data[category].append(file_info)

    return files_data

# Collect all files
base_dir = '.'
files_data = collect_files(base_dir)

# Build manifest structure
manifest = {
    'package': {
        'name': 'RF-10-checkpoint-13-correction-04-handoff',
        'version': 'correction-04',
        'date': '2026-10-01',
        'writer': 'BOX 3',
        'purpose': 'Close assertion gaps: postpone receipt snapshot timing, upload/survey scoped effect-sets'
    },
    'test_results': {
        'overall': {
            'total': 15,
            'passed': 15,
            'failed': 0,
            'pass_rate': '100%'
        },
        'boxes': [
            {
                'id': 'RF-10-08-C01',
                'name': 'IdempotencyPerCommandCharacterizationTests',
                'total': 10,
                'passed': 10,
                'failed': 0,
                'duration': '5s'
            },
            {
                'id': 'RF-10-09-C01',
                'name': 'Rf1009NotificationInboxCharacterizationTests',
                'total': 5,
                'passed': 5,
                'failed': 0,
                'duration': '2s',
                'note': 'Evidence reused from correction-03 - source unchanged'
            }
        ]
    },
    'build': {
        'warnings': 180,
        'errors': 0,
        'status': 'success'
    },
    'findings': [
        {
            'id': 'F-C13-01a',
            'status': 'FIXED',
            'description': 'Upload Create Replay - OperationId + OutcomeJson immutability + scoped effect-set',
            'coverage': 'correction-04',
            'open_issues': []
        },
        {
            'id': 'F-C13-01b',
            'status': 'FIXED',
            'description': 'SurveyPlan Create Replay - OperationId + OutcomeJson immutability + scoped effect-set',
            'coverage': 'correction-04',
            'open_issues': []
        },
        {
            'id': 'F-C13-01c',
            'status': 'VERIFIED',
            'description': 'Role Revocation Replay Authorization with new JWT',
            'coverage': 'correction-02',
            'open_issues': [
                'NOT_VERIFIED: original JWT behavior after role change (F-C13-01c-HISTORICAL)'
            ]
        },
        {
            'id': 'F-C13-02',
            'status': 'FIXED',
            'description': 'Postpone receipt snapshot timing + OperationId + OutcomeJson immutability',
            'coverage': 'correction-04',
            'open_issues': []
        },
        {
            'id': 'F-C13-03',
            'status': 'VERIFIED',
            'description': 'Consumer replay with candidate ID + fresh DbContext',
            'coverage': 'correction-02',
            'open_issues': []
        },
        {
            'id': 'F-C13-04',
            'status': 'VERIFIED',
            'description': 'MarkRead ReadAt/RowVersion + receipt immutability',
            'coverage': 'correction-02',
            'open_issues': []
        }
    ],
    'source_provenance': {
        'before_build': {
            'IdempotencyPerCommandCharacterizationTests.cs': '6f0bebf1bb2ad34abe8efce2b511556fdd4e901b118bc5e055fb57f2023aceb1',
            'Rf1009NotificationInboxCharacterizationTests.cs': 'f699530832800c4b247699e19a4fe56cdb88a0cf422966c07e5ce3bd339e986e'
        },
        'after_build': {
            'IdempotencyPerCommandCharacterizationTests.cs': 'a7bbae78744e69ca028c4f6e727637ae3872c0144537b5f3c94619b9e5c3789f',
            'Rf1009NotificationInboxCharacterizationTests.cs': 'f699530832800c4b247699e19a4fe56cdb88a0cf422966c07e5ce3bd339e986e'
        },
        'changes': [
            'IdempotencyPerCommandCharacterizationTests.cs: postpone receipt snapshot timing (lines 803-826, 860-883)',
            'IdempotencyPerCommandCharacterizationTests.cs: upload scoped effect-set count checks (lines 131-165)',
            'IdempotencyPerCommandCharacterizationTests.cs: survey plan scoped effect-set count checks (lines 245-275)'
        ]
    },
    'payload': files_data,
    'historical_limitations': [
        'NOT_VERIFIED: Original JWT behavior after role change (F-C13-01c-HISTORICAL)',
        'Survey checkpoint 08 inspection provenance',
        'Dispatcher/delivery workflow (not yet implemented)',
        'Before-build provenance for correction-02 (placeholder hashes)'
    ],
    'constraints_compliance': {
        'no_production_changes': True,
        'no_schema_changes': True,
        'no_contract_changes': True,
        'no_migration_changes': True,
        'no_ci_changes': True,
        'no_git_operations': True,
        'no_shared_database_writes': True,
        'isolated_fixtures_only': True,
        'historical_evidence_preserved': True
    },
    'checkpoint_status': {
        'checkpoint_13': 'COMPLETE',
        'rf_10_parent': 'PARTIAL'
    }
}

# Write manifest
with open('MANIFEST.json', 'w', encoding='utf-8') as f:
    json.dump(manifest, f, indent=2, ensure_ascii=False)

print('Manifest generated successfully')
print(f'Total files: {sum(len(files) for files in files_data.values())}')
for category, files in files_data.items():
    if files:
        print(f'  {category}: {len(files)} files')
