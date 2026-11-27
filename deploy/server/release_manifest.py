"""Write a release receipt from independently observed deployment facts."""
from datetime import datetime, timezone
import json
import os
from pathlib import Path
import re
import sys
from tempfile import NamedTemporaryFile


def checked(value, pattern):
    if not re.fullmatch(pattern, value):
        raise ValueError('Invalid release metadata')
    return value


def migrations(raw):
    values = [checked(line, r'[0-9]{14}_[A-Za-z0-9_]+') for line in raw.splitlines()]
    if sorted(set(values)) != values:
        raise ValueError('Migration history is not ordered and unique')
    return values


def migration_range(before, after):
    if after[:len(before)] != before:
        raise ValueError('Applied migration history is not a prefix')
    return after[len(before):]


def write_manifest(output, sha, branch, revision, config, api, ai, minio, sql,
                   enabled, model_hash, released_model, before, after):
    checked(sha, r'[0-9a-f]{40}')
    if Path(revision).read_text().strip() != sha:
        raise ValueError('Release SHA differs from REVISION')
    enabled = checked(enabled, r'true|false') == 'true'
    images = {name: checked(value, r'sha256:[0-9a-f]{64}')
              for name, value in (('api', api), ('minio', minio), ('sql', sql))}
    images['ai'] = checked(ai, r'sha256:[0-9a-f]{64}') if ai else None
    applied = migrations(after)
    if not applied or (enabled and not images['ai']):
        raise ValueError('Missing schema or real AI image')
    ai_settings = json.loads(Path(config).read_text()).get('AiRealWorker', {})
    receipt = {
        'gitSha': sha,
        'sourceBranch': checked(branch, r'[A-Za-z0-9._/-]+'),
        'imageIds': images,
        'schemaVersion': applied[-1],
        'migrations': applied,
        'appliedMigrations': migration_range(migrations(before), applied),
        'aiModelVersionId': None,
        'modelSha256': checked(model_hash, r'[0-9a-f]{64}') if model_hash else None,
        'preprocessingVersion': None,
        'configVersion': None,
        'rgAiEnabled': enabled,
        'activatedAtUtc': datetime.now(timezone.utc).isoformat(timespec='seconds').replace('+00:00', 'Z'),
    }
    if enabled:
        model = checked(ai_settings['ReleasedModelVersionId'].lower(),
                        r'[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}')
        if model != released_model.lower() or model_hash != ai_settings['CheckpointSha256'].lower():
            raise ValueError('Released model or checkpoint differs from application configuration')
        receipt.update(
            aiModelVersionId=model,
            preprocessingVersion=checked(ai_settings['PreprocessingVersion'], r'[A-Za-z0-9_.-]{1,128}'),
            configVersion=checked(ai_settings['ConfigVersion'], r'[A-Za-z0-9_.-]{1,128}'),
        )
    path = Path(output)
    stream = NamedTemporaryFile(mode='w', encoding='utf-8', dir=path.parent, delete=False)
    temporary = Path(stream.name)
    try:
        with stream:
            json.dump(receipt, stream, indent=2)
            stream.write('\n')
            stream.flush()
            os.fsync(stream.fileno())
        os.replace(temporary, path)
    finally:
        temporary.unlink(missing_ok=True)


if __name__ == '__main__':
    try:
        if sys.argv[1:2] == ['--check-migrations']:
            before, target, verified = map(migrations, sys.argv[2:])
            if migration_range(before, target) != verified:
                raise ValueError('Missing exact migration compatibility evidence')
        else:
            write_manifest(*sys.argv[1:])
    except (ValueError, KeyError, TypeError, AttributeError, OSError):
        sys.exit('Release receipt rejected. Verify deployment identity and metadata.')
