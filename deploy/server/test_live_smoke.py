import importlib.util
from pathlib import Path
import unittest


class LiveSmokeGuardTests(unittest.TestCase):
    def load(self):
        path = Path(__file__).with_name('live-smoke.py')
        self.assertTrue(path.is_file(), 'Missing bounded live HTTP smoke client')
        spec = importlib.util.spec_from_file_location('live_smoke', path)
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        return module

    def test_target_rejects_external_hosts_credentials_and_http(self):
        module = self.load()
        for target in ['http://192.168.1.11:8443', 'https://other:8443',
                       'https://user:secret@192.168.1.11:8443', 'https://192.168.1.11:8443/path']:
            with self.subTest(target=target), self.assertRaises(ValueError):
                module.validate_target(target)
        module.validate_target('https://192.168.1.11:8443')

    def test_failure_messages_never_contain_response_body(self):
        module = self.load()
        with self.assertRaisesRegex(RuntimeError, '^login: expected HTTP 200, received 401$'):
            module.expect('login', (401, {'password': 'private', 'token': 'private'}), 200)

    def test_password_change_failure_reports_only_known_category(self):
        module = self.load()
        with self.assertRaisesRegex(RuntimeError, 'new password rejected by policy') as error:
            module.expect('required password change', (400, {
                'title': 'Password policy rejected', 'password': 'private', 'token': 'private'}), 204)
        self.assertNotIn('private', str(error.exception))
        with self.assertRaisesRegex(RuntimeError, 'request validation failed') as error:
            module.expect('required password change', (400, {
                'title': 'Bad Request', 'password': 'private'}), 204)
        self.assertNotIn('private', str(error.exception))

    def test_redirects_are_refused_instead_of_forwarding_credentials(self):
        module = self.load()
        self.assertIsNone(module.NoRedirect().redirect_request(None, None, 307, 'redirect', {}, 'https://other'))

    @unittest.skipUnless(__import__('os').name == 'posix', 'PTY requires Linux')
    def test_secret_reader_accepts_ascii_and_rejects_invalid_terminal_bytes(self):
        import concurrent.futures
        import os
        import pty
        import termios
        import threading
        import tty
        from unittest.mock import patch

        module = self.load()
        for typed, expected in [(b'Example123456789!\r', 'Example123456789!'),
                                (b'Example123456789\x08!\r', 'Example12345678!'),
                                (b'bad\xff\r', None)]:
            with self.subTest(typed=typed):
                master, slave = pty.openpty()
                original = termios.tcgetattr(slave)
                ready = threading.Event()
                setraw = tty.setraw
                def signal_raw(fd):
                    setraw(fd)
                    ready.set()
                try:
                    with patch.object(tty, 'setraw', side_effect=signal_raw), concurrent.futures.ThreadPoolExecutor(max_workers=1) as pool:
                        result = pool.submit(module.read_secret, 'Password: ', slave)
                        self.assertEqual(os.read(master, 10), b'Password: ')
                        self.assertTrue(ready.wait(2))
                        os.write(master, typed)
                        if expected is None:
                            with self.assertRaisesRegex(RuntimeError, 'non-ASCII'):
                                result.result(timeout=2)
                        else:
                            self.assertEqual(result.result(timeout=2), expected)
                        self.assertEqual(termios.tcgetattr(slave), original)
                        self.assertEqual(os.read(master, 2), b'\r\n')
                finally:
                    os.close(master)
                    os.close(slave)


if __name__ == '__main__':
    unittest.main()
