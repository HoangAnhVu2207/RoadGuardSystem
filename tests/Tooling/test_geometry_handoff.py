"""Finite script contract checks. No network, seed, credentials or shared data."""
import importlib.util
import pathlib
import types
import unittest

SPEC = importlib.util.spec_from_file_location(
    "demo_ai", pathlib.Path(__file__).resolve().parents[2] / "deploy/fe-handoff/demo_ai.py")
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)


class GeometryHandoffTests(unittest.TestCase):
    def run_workflow(self, status="SUBMITTED", content="locked", saved=False, uncertain=False):
        calls = []
        journal = types.SimpleNamespace(data={"steps": {"ai-confirm-a": True} if saved else {}})
        if uncertain:
            journal.data["steps"]["input-ai-confirm-a"] = {"body": {}, "headers": {"Idempotency-Key": "same-key", "If-Match": "saved-draft-etag"}}

        def command(key, path, body, actor, etag=None):
            calls.append((key, path, body, actor, etag))
            return (201, {"submittedContentVersion": "locked", "routeVersionId": "route"}, {"ETag": "submit-etag"})

        def request(path, **kwargs):
            calls.append(("read", path, kwargs))
            return (200, {"draftId": "draft", "status": status, "submittedContentVersion": content}, {"ETag": "current-draft-etag"})

        result = MODULE.confirm_geometry(journal, {"supervisor": "test-session"}, request, command,
                                         lambda reply: reply[2]["ETag"], "/api/v1/projects/project",
                                         (201, {"id": "draft"}, {"ETag": "create-draft-etag"}), "a")
        return calls, result

    def test_submit_then_readback_then_supervisor_confirm_uses_current_draft_etag(self):
        calls, result = self.run_workflow()
        self.assertEqual(["submit-a", "read", "confirm-a"], [call[0] for call in calls])
        self.assertEqual(("pm-a", "create-draft-etag"), calls[0][3:])
        self.assertEqual(("supervisor", "current-draft-etag"), calls[2][3:])
        self.assertEqual("route", result["routeVersionId"])

    def test_changed_round_or_state_is_not_confirmed(self):
        for status, content in (("DRAFT", "locked"), ("SUBMITTED", "changed")):
            with self.subTest(status=status, content=content), self.assertRaisesRegex(RuntimeError, "submission changed"):
                self.run_workflow(status, content)

    def test_completed_journal_does_not_submit_a_new_round(self):
        calls, _ = self.run_workflow(saved=True)
        self.assertEqual(["confirm-a"], [call[0] for call in calls])
        # The real command helper's Journal.once returns the existing confirmation receipt.

    def test_confirmed_readback_only_retries_when_original_command_was_saved(self):
        with self.assertRaisesRegex(RuntimeError, "submission changed"):
            self.run_workflow(status="CONFIRMED")
        calls, _ = self.run_workflow(status="CONFIRMED", uncertain=True)
        self.assertEqual("confirm-a", calls[-1][0])
        # initialize.command ignores the newly observed token and reuses saved headers/body.


if __name__ == "__main__":
    unittest.main()
