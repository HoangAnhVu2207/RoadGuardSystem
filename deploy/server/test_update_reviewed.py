"""Exercise orchestration in a disposable directory with no Docker/server access."""
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest


@unittest.skipUnless(os.name == 'posix' and shutil.which('bash'), 'Requires POSIX bash')
class ReviewedUpdateTests(unittest.TestCase):
    def exercise(self, fail_backup=False, active_mismatch=False, target='b' * 40):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            old, new = 'a' * 40, 'b' * 40
            config = root / 'etc' / 'roadguard'
            config.mkdir(parents=True)
            (config / 'active-release').write_text('c' * 40 if active_mismatch else old)
            for sha in (old, new):
                directory = root / 'releases' / sha / 'deploy' / 'server'
                directory.mkdir(parents=True)
                (directory.parents[1] / 'REVISION').write_text(sha)
                (directory / 'manage.sh').write_text(
                    f'#!/bin/bash\necho "manage {sha[0]} $*" >> "$TRACE"\n'
                    'if [[ "$1" = backup* ]] && [ "${FAIL_BACKUP:-0}" = 1 ]; then exit 9; fi\n'
                    f'if [ "$1" = activate ]; then echo {new} > {config}/active-release; fi\n')
            (root / 'current').symlink_to(root / 'releases' / old)
            common = f'''RG_REPOSITORY={root}/repository.git
RG_RELEASES={root}/releases
RG_CONFIG={config}
RG_DATA={root}
require_root() {{ :; }}
extract_release() {{ echo "extract $1" >> "$TRACE"; }}
prepare_ai_assets() {{ echo "prepare_ai_assets" >> "$TRACE"; }}
ai_configured() {{ return 1; }}
compose() {{ echo "compose $*" >> "$TRACE"; }}
git() {{ echo "git $*" >> "$TRACE"; return 0; }}
hostname() {{ echo roadguardsystem; }}
# Deterministic resource fixture: never depend on CI runner free RAM/disk.
awk() {{ echo 8388608; }}
du() {{ printf '1024\\tfixture\\n'; }}
df() {{ printf 'Avail\\n21474836480\\n'; }}
'''
            source = Path(__file__).with_name('update-reviewed.sh')
            self.assertTrue(source.is_file(), 'Missing SHA-pinned deployment entrypoint')
            script = source.read_text().replace('/opt/roadguard', str(root)).replace(
                '/run/lock/roadguard-server.lock', str(root / 'lock'))
            runner = root / 'update.sh'
            runner.write_text(script)
            for sha in (old, new):
                directory = root / 'releases' / sha / 'deploy/server'
                (directory / 'common.sh').write_text(common)
            (root / 'releases' / new / 'deploy/server/update-reviewed.sh').write_text(script)
            trace = root / 'trace'
            result = subprocess.run(['bash', str(runner), old, target], capture_output=True, text=True,
                                    env={**os.environ, 'TRACE': str(trace), 'FAIL_BACKUP': str(int(fail_backup))}, timeout=15)
            return result, trace.read_text() if trace.exists() else ''

    def test_exact_target_is_built_then_backed_up_then_activated(self):
        result, trace = self.exercise()
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertIn('extract ' + 'b' * 40, trace)
        backup = 'manage b backup-for-update ' + 'a' * 40
        self.assertLess(trace.index('compose build'), trace.index(backup))
        self.assertLess(trace.index(backup), trace.index('manage b activate'))
        self.assertEqual(1, trace.count('manage b backup'))

    def test_backup_failure_never_activates(self):
        result, trace = self.exercise(fail_backup=True)
        self.assertNotEqual(0, result.returncode)
        self.assertIn('manage b backup-for-update', trace)
        self.assertNotIn(' activate', trace)

    def test_active_release_mismatch_stops_before_fetch_or_build(self):
        result, trace = self.exercise(active_mismatch=True)
        self.assertNotEqual(0, result.returncode)
        self.assertEqual('', trace)

    def test_non_sha_is_rejected_before_any_operation(self):
        result, trace = self.exercise(target='deploy')
        self.assertNotEqual(0, result.returncode)
        self.assertEqual('', trace)

    def test_deployment_backup_records_active_release_and_does_not_restart_services(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            old, new = 'a' * 40, 'b' * 40
            config = root / 'etc' / 'roadguard'
            config.mkdir(parents=True)
            (config / 'active-release').write_text(old)
            (root / 'releases' / old).mkdir(parents=True)
            (root / 'current').symlink_to(root / 'releases' / old)
            candidate = root / 'releases' / new
            scripts = candidate / 'deploy/server'
            scripts.mkdir(parents=True)
            (candidate / 'REVISION').write_text(new)
            data = root / 'srv/roadguard'
            for name in ('sql', 'backups', 'objects', 'files', 'keys', 'ai'):
                (data / name).mkdir(parents=True)
            stamp = '20261124T010000Z'
            trace = root / 'trace'
            (scripts / 'common.sh').write_text(f'''RG_RELEASES={root}/releases
RG_CONFIG={config}
RG_DATA={data}
require_root() {{ :; }}
ai_configured() {{ return 0; }}
date() {{ echo {stamp}; }}
compose() {{
    echo "$*" >> {trace}
    if [ "$1" = exec ]; then printf 'SQL backup fixture' > "$RG_DATA/sql/roadguard-{stamp}.bak"; fi
}}
''')
            source = Path(__file__).with_name('manage.sh').read_text()
            script = scripts / 'manage.sh'
            script.write_text(source.replace('/opt/roadguard', str(root))
                              .replace('/run/lock/roadguard-server.lock', str(root / 'lock'))
                              .replace('tar -C / ', f'tar -C {root} '))
            result = subprocess.run(['bash', str(script), 'backup-for-update', old],
                                    capture_output=True, text=True, timeout=15)
            self.assertEqual(0, result.returncode, result.stderr)
            snapshot = data / 'backups' / stamp
            self.assertEqual(old, (snapshot / 'RELEASE').read_text().strip())
            self.assertTrue((snapshot / 'SHA256SUMS').is_file())
            operations = trace.read_text().splitlines()
            self.assertEqual(['stop', 'exec'], [command.split()[0] for command in operations])


if __name__ == '__main__':
    unittest.main()
