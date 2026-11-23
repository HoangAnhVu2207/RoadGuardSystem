import importlib.util
import contextlib
import io
import json
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location('server_setup', Path(__file__).with_name('setup.py'))
setup = importlib.util.module_from_spec(spec)
spec.loader.exec_module(setup)


class SetupTests(unittest.TestCase):
    def test_invalid_terminal_encoding_is_retried_without_logging_raw_input(self):
        for secret in (False, True):
            output = io.StringIO()
            invalid = UnicodeDecodeError('utf-8', b'private\xffinput', 7, 8, 'invalid continuation byte')
            with self.subTest(secret=secret), patch.object(setup.getpass, 'getpass', side_effect=[invalid, 'valid-value']) as password_reader, \
                 patch('builtins.input', side_effect=[invalid, 'valid-value']) as text_reader, contextlib.redirect_stderr(output):
                self.assertEqual('valid-value', setup.read_terminal('Value: ', secret=secret))
                self.assertEqual(2, (password_reader if secret else text_reader).call_count)
                self.assertNotIn('private', output.getvalue())
                self.assertNotIn('valid-value', output.getvalue())
                self.assertIn('UTF-8', output.getvalue())

    @unittest.skipUnless(sys.platform == 'linux', 'Linux PTY required')
    def test_real_terminal_recovers_after_invalid_utf8_line(self):
        import os
        import pty
        import select
        import termios
        import time
        master, slave = pty.openpty()
        attributes = termios.tcgetattr(slave)
        attributes[3] &= ~termios.ECHO
        termios.tcsetattr(slave, termios.TCSANOW, attributes)
        process = subprocess.Popen([sys.executable, '-u', '-c',
                                    "import setup; assert setup.read_terminal('Value: ') == 'Owner'; print('RECOVERED')"],
                                   cwd=Path(__file__).parent, stdin=slave, stdout=slave, stderr=slave,
                                   env={**os.environ, 'PYTHONIOENCODING': 'utf-8:strict'})
        os.close(slave)
        def receive(marker):
            result = b''
            deadline = time.monotonic() + 10
            while marker not in result and time.monotonic() < deadline:
                if select.select([master], [], [], 0.1)[0]:
                    result += os.read(master, 4096)
            self.assertIn(marker, result)
            return result
        try:
            receive(b'Value: ')
            os.write(master, b'\xff\n')
            retry = receive(b'Value: ')
            self.assertIn(b'UTF-8', retry)
            self.assertNotIn(b'Traceback', retry)
            os.write(master, b'Owner\n')
            receive(b'RECOVERED')
            self.assertEqual(0, process.wait(timeout=10))
        finally:
            if process.poll() is None:
                process.kill()
                process.wait()
            os.close(master)

    @unittest.skipUnless(shutil.which('openssl'), 'OpenSSL required for real certificate generation')
    def test_wizard_generates_verified_certificates_and_refuses_second_run(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            config, data, ca = root / 'config', root / 'data', root / 'public-ca.crt'
            output = io.StringIO()
            password = 'LongPrivatePassword1!'
            with patch.object(setup, 'CONFIG', config), patch.object(setup, 'DATA', data), \
                 patch.object(setup, 'PUBLIC_CA', ca), patch.object(setup.os, 'geteuid', return_value=0, create=True), \
                 patch.object(setup.os, 'chown', create=True), patch('sys.argv', ['setup.py', '--host', '192.168.1.11']), \
                 patch('builtins.input', side_effect=['owner@example.com', 'Owner']), \
                 patch.object(setup.getpass, 'getpass', return_value=password), contextlib.redirect_stdout(output):
                setup.main()
                original = (config / 'application.json').read_bytes()
                with self.assertRaises(ValueError):
                    setup.main()
                self.assertEqual(original, (config / 'application.json').read_bytes())
            runtime = json.loads(original)
            self.assertNotIn(password, str(runtime))
            self.assertNotIn(password, output.getvalue())
            self.assertEqual(ca.read_bytes(), (config / 'tls/ca.crt').read_bytes())
            self.assertTrue((config / 'tls/server.pfx').stat().st_size > 0)
            if setup.os.name == 'posix':
                self.assertEqual(0o600, (config / 'tls/ca.key').stat().st_mode & 0o777)
                self.assertEqual(0o700, data.stat().st_mode & 0o777)

    def test_rejects_non_lan_and_command_injection(self):
        for host in ['8.8.8.8', '127.0.0.1', '0.0.0.0', '192.168.1.11;touch /tmp/bad', 'example.com']:
            with self.subTest(host=host), self.assertRaises(ValueError):
                setup.validate_host(host)

    def test_refuses_overwrite_and_preserves_existing_secret(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            config = root / 'config'
            config.mkdir()
            secret = config / 'application.json'
            secret.write_text('existing-secret')
            with self.assertRaises(ValueError):
                setup.require_empty_target(config, root / 'data')
            self.assertEqual('existing-secret', secret.read_text())

    def test_runtime_cannot_read_sa_or_initial_credentials_and_keys_are_not_reused(self):
        app, maintenance, sa, storage, pfx = setup.configuration('192.168.1.11', 'owner@example.com', 'Owner', 'LongPrivatePassword1!')
        app_text = str(app)
        self.assertNotIn(sa, app_text)
        self.assertNotIn('LongPrivatePassword1!', app_text)
        self.assertNotIn('RuntimePassword', app['ServerDeployment'])
        self.assertNotIn('InitialSupervisor', app['ServerDeployment'])
        self.assertEqual(app['Jwt'], maintenance['Jwt'])
        self.assertEqual(app['PasswordChangeFingerprint'], maintenance['PasswordChangeFingerprint'])
        self.assertIn('TrustServerCertificate=false', app['RoadGuardDatabase']['ConnectionString'])
        self.assertFalse(app['Anh02']['MockEnabled'])
        other = setup.configuration('192.168.1.11', 'owner@example.com', 'Owner', 'LongPrivatePassword1!')[0]
        self.assertNotEqual(app['Jwt'], other['Jwt'])
        self.assertEqual(3, len(set((sa, storage, pfx))))

    def test_rejects_invalid_initial_account_before_creating_files(self):
        for email, name, password in [('bad-address', 'Owner', 'LongPrivatePassword1!'),
                                      ('owner@example.com', '', 'LongPrivatePassword1!'),
                                      ('owner@example.com', 'Owner', 'weak'),
                                      ('owner@example.com', 'Owner', 'a' * 129 + 'A1!')]:
            with self.subTest(email=email, name=name), self.assertRaises(ValueError):
                setup.configuration('192.168.1.11', email, name, password)


if __name__ == '__main__':
    unittest.main()
