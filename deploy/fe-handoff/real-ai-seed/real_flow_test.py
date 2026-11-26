"""Regression checks for the real upload driver. Run with python -B real_flow_test.py."""
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

from real_flow import DRONE, submit
from real_flow_api import Api, FlowError, checked_asset, file_hash, operation, upload, valid_etag


class RealUploadTests(unittest.TestCase):
    def test_already_verified_file_replays_without_writing_parts(self):
        calls = []
        class Verified:
            def request(self, action, method, path, body=None, **kwargs):
                calls.append((method, path, kwargs.get("idem")))
                return {"fileId": "00000000-0000-4000-8000-000000000001",
                        "id": "00000000-0000-4000-8000-000000000002", "status": "VERIFIED"}, {}
        with tempfile.TemporaryDirectory() as folder:
            source = Path(folder, "example.srt")
            source.write_text("telemetry")
            digest = file_hash(source)
            args = (Verified(), "operator", "project", "task", source, digest, "TELEMETRY", "application/x-subrip")
            self.assertEqual(upload(*args), upload(*args))
            self.assertEqual([c[0] for c in calls], ["POST", "GET", "GET"] * 2)
            self.assertEqual(calls[0][2], calls[3][2])
            self.assertNotEqual(calls[0][2], operation("another-task", "TELEMETRY", digest))

    def test_closed_task_replay_uses_saved_files_and_original_etag(self):
        from uuid import uuid4

        task_id, project_id, actor, dataset_id, video_id, srt_id = [str(uuid4()) for _ in range(6)]
        scope = [{"routeVersionId": str(uuid4()), "segmentSetId": str(uuid4()), "segmentIds": [str(uuid4())],
                  "targetBand": "SURFACE"}]
        record = {"foundation": dict(scope[0], projectId=project_id), "taskId": task_id,
                  "pair": {"videoFileId": video_id, "telemetryFileId": srt_id}, "submissionEtag": '"original"'}
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            names = ["example.mp4", "example.srt"]
            for name in names:
                (root / name).write_text(name)
            files = {name: {"sha256": file_hash(root / name)} for name in names}
            expectations = self

            class Submitted:
                def request(self, action, method, path, body=None, **kwargs):
                    if path.endswith("/survey-tasks"):
                        return {"id": task_id}, {}
                    if path.endswith(task_id):
                        return {"id": task_id, "status": "SUBMITTED", "scope": scope, "projectId": project_id,
                                "operatorId": actor}, {"ETag": '"new"'}
                    if path.endswith("/eligible-drones?limit=100"):
                        return {"items": [{"id": DRONE}]}, {}
                    if path.endswith("/datasets"):
                        expectations.assertEqual(kwargs["etag"], '"original"')
                        return {"id": dataset_id}, {}
                    expectations.assertEqual(path, "/datasets/" + dataset_id)
                    return {"surveyTaskId": task_id, "projectId": project_id, "sourceFiles": [
                        {"fileId": file_id, "checksumSha256": files[name]["sha256"]}
                        for name, file_id in zip(names, [video_id, srt_id])]}, {}

            with patch("real_flow.upload") as upload_again, patch("builtins.print"):
                submit(Submitted(), "DKD", record, {"operator-kenh-dong": (actor, "operator"), "pm-kenh-dong": ("id", "pm")},
                       {"videoFile": names[0], "telemetryFile": names[1], "key": "clip",
                        "approximateRecordedAtUtc": "2026-11-20T00:00:00Z"}, files, root, lambda: None)
                upload_again.assert_not_called()
            self.assertEqual(record["datasetReplay"], "PASS")

    def test_rejects_wrong_media_bytes_before_seed(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            source = root / "media.mp4"
            source.write_bytes(b"original")
            digest = file_hash(source)
            source.write_bytes(b"replaced")
            with self.assertRaises(FlowError):
                checked_asset(root, source.name, digest)

    def test_signed_origin_and_etag_guards(self):
        api = Api("http://127.0.0.1:8080", "http://localhost:9900")
        for address in ["https://private.example/item?signature=hidden", "http://localhost:9901/item?signature=hidden"]:
            with patch("http.client.HTTPConnection") as connect:
                with self.assertRaisesRegex(FlowError, "^Unexpected signed storage origin$"):
                    api.put(address, b"")
                connect.assert_not_called()
        self.assertEqual(valid_etag({"ETag": '"opaque"'}), '"opaque"')
        with self.assertRaises(FlowError):
            valid_etag({})

if __name__ == "__main__":
    unittest.main()
