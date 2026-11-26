"""Regression checks for long-running demo API replays."""
import unittest
from tempfile import TemporaryDirectory
from unittest.mock import Mock

from demo_flow_api import Api


class DenialTests(unittest.TestCase):
    def test_forbidden_check_renews_expired_session(self):
        with TemporaryDirectory() as journal:
            api = Api("http://127.0.0.1:8080", 9900, [], journal)
            api.tokens["pm"] = "expired"
            api.http.request = Mock(side_effect=[
                Mock(status_code=401),
                Mock(status_code=403, **{"json.return_value": {"code": "forbidden"}}),
            ])

            def login(actor):
                api.tokens[actor] = "renewed"

            api.login = Mock(side_effect=login)
            self.assertEqual(api.denial("pm", "/projects/example"), 403)
            api.login.assert_called_once_with("pm")
            sent = api.http.request.call_args_list
            self.assertEqual([call.kwargs["headers"]["Authorization"] for call in sent],
                             ["Bearer expired", "Bearer renewed"])

    def test_expected_unauthorized_keeps_original_session(self):
        with TemporaryDirectory() as journal:
            api = Api("http://127.0.0.1:8080", 9900, [], journal)
            api.tokens["pm"] = "expired"
            api.http.request = Mock(return_value=Mock(
                status_code=401, **{"json.return_value": {"code": "unauthorized"}}))
            api.login = Mock()

            self.assertEqual(api.denial("pm", "/projects/example", expected=(401,)), 401)
            api.login.assert_not_called()


if __name__ == "__main__":
    unittest.main()
