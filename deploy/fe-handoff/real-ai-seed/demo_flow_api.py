"""Loopback-only DEMO API client with per-operation crash recovery."""
import hashlib
import json
import re
import time
import uuid
from datetime import datetime, timezone
from pathlib import Path
from urllib.parse import urlsplit

import requests


def now():
    return datetime.now(timezone.utc).isoformat()


def save(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    tmp = path.with_suffix(".tmp")
    tmp.write_text(json.dumps(value, ensure_ascii=False, indent=2), encoding="utf-8")
    tmp.replace(path)


def stable(value):
    return str(uuid.uuid5(uuid.NAMESPACE_URL, "roadguard-api-demo-v1/" + value))


def require(condition, message):
    if not condition:
        raise ValueError(message)


class Rejected(RuntimeError):
    def __init__(self, method, path, status, code):
        self.status = status
        super().__init__(f"{method} {path.split('?')[0]} HTTP {status} {code}")


class Api:
    def __init__(self, origin, storage_port, credentials, journal):
        target = urlsplit(origin)
        require(target.hostname in {"localhost", "127.0.0.1"} and target.path in {"", "/"}
                and target.scheme == "http" and target.port, "Local BE origin required")
        self.origin = origin.rstrip("/") + "/api/v1"
        self.storage_port = storage_port
        self.http = requests.Session()
        self.http.trust_env = False
        self.users = {user["key"]: user for user in credentials}
        self.tokens = {}
        self.ids = {}
        self.journal = Path(journal)
        self.http_counts = {}
        self.replay_path = self.journal / "replay-families.json"
        self.replays = json.loads(self.replay_path.read_text()) if self.replay_path.exists() else {}

    def call(self, actor, path, method="GET", body=None, headers=None, refresh=True):
        require(path.startswith("/") and not path.startswith("//"), "Expected BE resource path")
        auth = {"Authorization": "Bearer " + self.tokens[actor]} if actor else {}
        r = self.http.request(method, self.origin + path, json=body, headers={**auth, **(headers or {})},
                              timeout=45, allow_redirects=False)
        code = str(r.status_code)
        self.http_counts[code] = self.http_counts.get(code, 0) + 1
        if r.status_code == 401 and refresh and actor:
            self.login(actor)
            return self.call(actor, path, method, body, headers, False)
        if not 200 <= r.status_code < 300:
            try:
                problem = r.json()
                label = str(problem.get("code", "request_rejected"))
                if not re.fullmatch(r"[a-zA-Z0-9_.-]{1,100}", label):
                    label = "request_rejected"
            except (ValueError, AttributeError):
                label = "request_rejected"
            raise Rejected(method, path, r.status_code, label)
        return {"body": r.json() if r.content else None, "etag": r.headers.get("ETag")}

    def login(self, key):
        user = self.users[key]
        result = self.call(None, "/auth/android/login", "POST",
                           {field: user[field] for field in ("email", "password")})["body"]
        self.ids[key] = result["user"]["id"]
        self.tokens[key] = result["accessToken"]

    def read(self, actor, path):
        return self.call(actor, path)["body"]

    def etag(self, actor, path):
        reply = self.call(actor, path)
        return reply["etag"] or '"' + reply["body"]["version"] + '"'

    def mutation(self, key, actor, path, body, version=None, method="POST", extra=None, replay=False):
        checkpoint = self.journal / (stable(key) + ".json")
        if checkpoint.exists():
            rec = json.loads(checkpoint.read_text(encoding="utf-8"))
            require((rec["actor"], rec["path"], rec["method"]) == (actor, path, method),
                    "Journal scope changed for " + key)
            if "result" in rec and not replay:
                return rec["result"]
        else:
            headers = {"Idempotency-Key": stable(key), **(extra or {})}
            if version:
                headers["If-Match"] = version
            rec = dict(key=key, actor=actor, path=path, method=method, body=body, headers=headers)
            save(checkpoint, rec)
        try:
            result = self.call(actor, path, method, rec["body"], rec["headers"])
        except Rejected as error:
            # A conclusive 4xx with no earlier success can be retried with corrected input.
            if error.status in {400, 403, 404, 409, 412, 422, 428} and "result" not in rec:
                checkpoint.unlink()
            raise
        family = method + " " + re.sub(r"[0-9a-fA-F]{8}(?:-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12}", "{id}", path)
        if family not in self.replays:
            try:
                second = self.call(actor, path, method, rec["body"], rec["headers"])
                initial = result["body"] or {}
                repeated = second["body"] or {}
                fields = [name for name in ("id", "attemptId", "reviewId", "taskId", "version")
                          if name in initial]
                same = all(repeated.get(name) == initial[name] for name in fields)
                self.replays[family] = {"logicalKey": key, "status": "PASS" if same else "FAIL",
                                        "comparedFields": fields}
            except Rejected as error:
                self.replays[family] = {"logicalKey": key, "status": "FAIL", "reason": str(error)}
            save(self.replay_path, self.replays)
        prior = rec.get("result", {}).get("body") or {}
        if prior.get("id"):
            require(result["body"]["id"] == prior["id"], "Changed ID on API replay")
        rec["result"] = result
        save(checkpoint, rec)
        return result

    def denial(self, actor, path, expected=(403, 404), **kwargs):
        try:
            self.call(actor, path, refresh=401 not in expected, **kwargs)
        except Rejected as error:
            require(error.status in expected, str(error))
            return error.status
        raise ValueError("Unexpected authorization or version success at " + path)

    def photo(self, key, actor, project, task, purpose, image):
        digest = hashlib.sha256(image).hexdigest()
        reporter = purpose == "REPORTER"
        base = "/reporter-evidence/uploads" if reporter else "/uploads"
        body = {"fileName": "DEMO-" + purpose + ".jpg", "mediaType": "image/jpeg",
                "sizeBytes": len(image), "checksumSha256": digest}
        if not reporter:
            body.update(projectId=project, targetId=task, purpose=purpose)
        upload = self.mutation(key, actor, base, body)["body"]
        path = base + "/" + upload["id"]
        state = self.read(actor, path)
        if state["status"] not in {"VERIFIED", "VERIFYING", "PENDING_VERIFICATION"}:
            completed = self.journal / (stable(key + "-complete") + ".json")
            if completed.exists():
                self.mutation(key + "-complete", actor, path + "/complete", None)
            else:
                parts = self.call(actor, path + "/part-urls", "POST", {"partNumbers": [1]},
                                  {"Idempotency-Key": str(uuid.uuid4())})["body"]["parts"]
                url = parts[0]["url"]
                location = urlsplit(url)
                require(location.scheme == "http" and location.hostname in {"localhost", "127.0.0.1"}
                        and location.port == self.storage_port and not location.username,
                        "Unexpected signed storage origin")
                try:
                    uploaded = self.http.put(url, data=image, timeout=40, allow_redirects=False)
                except requests.RequestException:
                    raise RuntimeError("DEMO image storage request failed") from None
                require(uploaded.status_code == 200, f"DEMO image storage PUT HTTP {uploaded.status_code}")
                self.mutation(key + "-complete", actor, path + "/complete", {
                    "parts": [{"partNumber": 1, "eTag": uploaded.headers["ETag"]}],
                    "checksumSha256": digest}, self.etag(actor, path))
        for _ in range(180):
            state = self.read(actor, path)
            if state["status"] == "VERIFIED":
                return upload["fileId"], digest
            require(state["status"] not in {"FAILED", "REJECTED", "CANCELLED"}, "DEMO upload failed")
            time.sleep(1)
        raise RuntimeError("DEMO verification timed out")
