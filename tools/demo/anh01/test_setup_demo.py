"""Focused tooling tests; local byte transport is NOT MinIO/8 GiB acceptance."""
import hashlib
import http.server
import tempfile
import threading
import unittest
from pathlib import Path
from unittest.mock import patch
import setup_demo as demo


class DemoTransportTests(unittest.TestCase):
    def test_command_resume_replays_original_version_after_ack_loss(self):
        observed = []

        class Command(http.server.BaseHTTPRequestHandler):
            def do_POST(self):
                observed.append((self.headers["If-Match"], self.rfile.read(int(self.headers["Content-Length"]))))
                if len(observed) == 1:
                    self.connection.close()  # Server received the command; acknowledgement is lost.
                    return
                self.send_response(200)
                self.send_header("Content-Length", "2")
                self.end_headers()
                self.wfile.write(b"{}")

            def log_message(self, *args):
                pass

        server = http.server.ThreadingHTTPServer(("127.0.0.1", 0), Command)
        thread = threading.Thread(target=server.serve_forever, daemon=True)
        thread.start()
        try:
            with tempfile.TemporaryDirectory() as directory, patch.dict(demo.os.environ, {"ANH01_PM_TOKEN": "fixture-only"}):
                state = Path(directory) / "state.json"
                base = f"http://127.0.0.1:{server.server_port}"
                with self.assertRaises((OSError, demo.http.client.HTTPException)):
                    demo.DemoApi(base, state).call("pm", "POST", "task/supplements", {"reason": "original"}, "supplement", "v1")
                resumed = demo.DemoApi(base, state)
                resumed.call("pm", "POST", "task/supplements", {"reason": "original"}, "supplement", "v2")
                self.assertEqual(observed[0], observed[1])
                with self.assertRaises(ValueError):
                    resumed.call("pm", "POST", "task/supplements", {"reason": "changed"}, "supplement", "v2")
        finally:
            server.shutdown()
            server.server_close()
            thread.join(timeout=5)

    def test_put_streams_exact_slice_in_bounded_buffers(self):
        observed = {}

        class Sink(http.server.BaseHTTPRequestHandler):
            def do_PUT(self):
                remaining = int(self.headers["Content-Length"])
                digest = hashlib.sha256()
                while remaining:
                    chunk = self.rfile.read(min(remaining, 65536))
                    digest.update(chunk)
                    remaining -= len(chunk)
                observed["hash"] = digest.hexdigest()
                self.send_response(200)
                self.send_header("ETag", '"fixture-part"')
                self.send_header("Content-Length", "0")
                self.end_headers()

            def log_message(self, *args):
                pass  # Never log signed URL query strings.

        server = http.server.ThreadingHTTPServer(("127.0.0.1", 0), Sink)
        thread = threading.Thread(target=server.serve_forever, daemon=True)
        thread.start()
        try:
            with tempfile.TemporaryDirectory() as directory:
                path = Path(directory) / "synthetic.bin"
                path.write_bytes(bytes(range(256)) * 1024)
                sends = []
                real_send = demo.http.client.HTTPConnection.send

                def track(connection, content):
                    sends.append(len(content))
                    return real_send(connection, content)

                with patch.object(demo.http.client.HTTPConnection, "send", track):
                    etag = demo.put_part(path, f"http://127.0.0.1:{server.server_port}/part?fixture-only=1", 13, 180000, "application/octet-stream")
                self.assertEqual("fixture-part", etag)
                self.assertLessEqual(max(sends), 65536)
                self.assertEqual(hashlib.sha256(path.read_bytes()[13:180013]).hexdigest(), observed["hash"])
        finally:
            server.shutdown()
            server.server_close()
            thread.join(timeout=5)

    def test_external_signed_url_is_rejected_before_transport(self):
        with self.assertRaises(ValueError):
            demo.put_part(Path("unused"), "https://storage.example/part?secret=fixture", 0, 1, "video/mp4")

    def test_small_file_cannot_be_reported_as_eight_gib_acceptance(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "small.mp4"
            path.write_bytes(b"synthetic")
            api = demo.DemoApi("http://localhost:5000", Path(directory) / "state.json")
            with self.assertRaises(ValueError):
                api.upload_path("operator", path, "SURVEY_VIDEO", "project", "task", exact_size=8589934592)

    def test_complete_ack_loss_reuses_original_body_and_version(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "fixture.mp4"
            path.write_bytes(b"synthetic")
            api = demo.DemoApi("http://localhost:5000", Path(directory) / "state.json")
            body = {"parts": [{"partNumber": 1, "eTag": "fixture"}], "checksumSha256": hashlib.sha256(path.read_bytes()).hexdigest()}
            api.state["upload:fixture.mp4"] = {"id": "session", "fileId": "file", "completeRequest": body, "completeVersion": "original-v1"}
            calls = []

            def call(role, method, resource, request=None, key=None, version=None):
                calls.append((method, resource, request, version))
                if resource == "files/file":
                    return {"status": "VERIFIED", "sizeBytes": 9, "checksumSha256": body["checksumSha256"]}
                return {"status": "VERIFYING", "version": "current-v2"}

            api.call = call
            self.assertEqual("file", api.upload_path("operator", path, "SURVEY_VIDEO", "project", "task"))
            posts = [c for c in calls if c[0] == "POST"]
            self.assertEqual(1, len(posts))
            self.assertEqual(body, posts[0][2])
            self.assertEqual("original-v1", posts[0][3])


if __name__ == "__main__":
    unittest.main()
