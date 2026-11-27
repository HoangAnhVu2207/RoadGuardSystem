import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

SCRIPT = Path(__file__).with_name('release_manifest.py')
SHA = 'a' * 40
DIGEST = 'sha256:' + 'b' * 64
FIRST = '20261008171739_BaselineCurrentSchema'
SECOND = '20261126090000_ReporterRoutingMap'


class ReleaseManifestTests(unittest.TestCase):
    def generate(self, before=FIRST, after=FIRST + '\n' + SECOND, revision=SHA):
        with tempfile.TemporaryDirectory() as temporary:
            directory = Path(temporary)
            (directory / 'REVISION').write_text(revision)
            (directory / 'application.json').write_text(json.dumps({
                'Secret': 'must-never-appear',
                'AiRealWorker': {
                    'ReleasedModelVersionId': '05ca3d3b-0823-413a-89fc-4d8192b8ed27',
                    'CheckpointSha256': 'c' * 64,
                    'PreprocessingVersion': 'ffmpeg-pts-v1',
                    'ConfigVersion': 'ft95-cpu-v1',
                },
            }))
            receipt = directory / 'receipt.json'
            result = subprocess.run([
                sys.executable, str(SCRIPT), str(receipt), SHA, 'deploy',
                str(directory / 'REVISION'), str(directory / 'application.json'),
                DIGEST, DIGEST, DIGEST, DIGEST, 'true', 'c' * 64,
                '05ca3d3b-0823-413a-89fc-4d8192b8ed27', before, after,
            ], capture_output=True, text=True, timeout=10)
            return result, receipt.read_text() if receipt.exists() else ''

    def test_receipt_records_verified_identity_and_exact_migration_range(self):
        result, raw = self.generate()
        self.assertEqual(0, result.returncode, result.stderr)
        receipt = json.loads(raw)
        self.assertEqual(SHA, receipt['gitSha'])
        self.assertEqual('deploy', receipt['sourceBranch'])
        self.assertEqual({name: DIGEST for name in ('api', 'ai', 'minio', 'sql')}, receipt['imageIds'])
        self.assertEqual(SECOND, receipt['schemaVersion'])
        self.assertEqual([FIRST, SECOND], receipt['migrations'])
        self.assertEqual([SECOND], receipt['appliedMigrations'])
        self.assertEqual('05ca3d3b-0823-413a-89fc-4d8192b8ed27', receipt['aiModelVersionId'])
        self.assertEqual('c' * 64, receipt['modelSha256'])
        self.assertEqual('ffmpeg-pts-v1', receipt['preprocessingVersion'])
        self.assertEqual('ft95-cpu-v1', receipt['configVersion'])
        self.assertTrue(receipt['rgAiEnabled'])
        self.assertTrue(receipt['activatedAtUtc'].endswith('Z'))
        self.assertNotIn('must-never-appear', raw)

    def test_mismatched_revision_or_non_prefix_migrations_never_write_receipt(self):
        for kwargs in ({'revision': 'b' * 40}, {'before': SECOND}, {'after': FIRST + '\ninvalid'}):
            with self.subTest(kwargs=kwargs):
                result, raw = self.generate(**kwargs)
                self.assertNotEqual(0, result.returncode)
                self.assertEqual('', raw)
