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

    def test_redirects_are_refused_instead_of_forwarding_credentials(self):
        module = self.load()
        self.assertIsNone(module.NoRedirect().redirect_request(None, None, 307, 'redirect', {}, 'https://other'))


if __name__ == '__main__':
    unittest.main()
