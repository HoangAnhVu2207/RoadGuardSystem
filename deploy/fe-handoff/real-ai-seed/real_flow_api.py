"""HTTP and multipart helpers for a disposable loopback RoadGuard stack."""
import hashlib
import http.client
import json
import re
import time
from pathlib import Path
from urllib.parse import urlsplit
from uuid import NAMESPACE_URL, UUID, uuid4, uuid5


class FlowError(RuntimeError):
    pass


def require(condition, message):
    if not condition:
        raise FlowError(message)


def identifier(value):
    return str(UUID(value))


def operation(*parts):
    return str(uuid5(NAMESPACE_URL, "roadguard:real-ai-fe:v1:" + ":".join(parts)))


def read_json(path):
    return json.loads(Path(path).read_text(encoding="utf-8-sig"))


def save(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    draft = path.with_name(path.name + ".tmp")
    draft.write_text(json.dumps(value, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    draft.replace(path)


def file_hash(path):
    with path.open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()


def checked_asset(root, name, digest):
    path = (root / name).resolve()
    require(path.is_relative_to(root.resolve()) and file_hash(path) == digest, "Asset checksum mismatch: " + name)
    return path


def valid_etag(headers):
    tag = headers.get("ETag", "")
    require(len(tag) > 2 and tag[0] == tag[-1] == '"', "Missing ETag")
    return tag


def loopback(origin):
    url = urlsplit(origin)
    require(url.scheme == "http" and url.hostname in ("localhost", "127.0.0.1", "::1")
            and url.port and not (url.username or url.password or url.query or url.fragment)
            and url.path in ("", "/"), "A local HTTP origin and port are required")
    return url


class Api:
    def __init__(self, endpoint, storage):
        self.url = loopback(endpoint)
        self.storage = loopback(storage)
        self.status_counts = {}
        self.sessions = {}

    def remember(self, key, response):
        require(isinstance(response["expiresIn"], (float, int)) and response["expiresIn"] > 0, "Invalid session lifetime")
        self.sessions[key] = (response["accessToken"], response["refreshToken"],
                              time.monotonic() + response["expiresIn"], identifier(response["user"]["id"]))

    def bearer(self, key):
        access, refresh, expiry, actor_id = self.sessions[key]
        if time.monotonic() < expiry - 45:
            return access
        refreshed, _ = self.request(key + " refresh", "POST", "/auth/refresh", {"refreshToken": refresh}, idem=str(uuid4()))
        require(identifier(refreshed["user"]["id"]) == actor_id, "Session identity changed on refresh")
        self.remember(key, refreshed)
        return refreshed["accessToken"]

    def logout(self):
        for key in list(self.sessions):
            self.request(key + " logout", "POST", "/auth/logout", token=key, idem=str(uuid4()), statuses=(204, 401))
            del self.sessions[key]

    def request(self, action, method, path, body=None, *, token=None, idem=None, etag=None, statuses=(200,)):
        headers = {"Accept": "application/json"}
        if token:
            headers["Authorization"] = "Bearer " + self.bearer(token)
        if idem:
            headers["Idempotency-Key"] = idem
        if etag:
            headers["If-Match"] = etag
        payload = None
        if body is not None:
            headers["Content-Type"] = "application/json"
            payload = json.dumps(body, allow_nan=False).encode()
        conn = http.client.HTTPConnection(self.url.hostname, self.url.port, timeout=90)
        try:
            conn.request(method, "/api/v1" + path, body=payload, headers=headers)
            res = conn.getresponse()
            data = res.read()
            answer = json.loads(data) if data and "json" in res.headers.get("Content-Type", "") else None
            self.status_counts[str(res.status)] = self.status_counts.get(str(res.status), 0) + 1
            if res.status not in statuses:
                code = answer.get("code") if isinstance(answer, dict) else None
                safe = code if isinstance(code, str) and re.fullmatch(r"[A-Za-z0-9_]{1,80}", code) else "unknown"
                raise FlowError(f"{action} HTTP {res.status} ({safe})")
            return answer, res.headers
        finally:
            conn.close()

    def put(self, url, data):
        signed = urlsplit(url)
        require(signed.scheme == "http" and signed.hostname in {"localhost", "127.0.0.1", "::1"}
                and signed.port == self.storage.port and signed.query and not (signed.username or signed.password
                or signed.fragment), "Unexpected signed storage origin")
        conn = http.client.HTTPConnection(self.storage.hostname, self.storage.port, timeout=120)
        try:
            conn.request("PUT", signed.path + "?" + signed.query, data, {"Host": signed.netloc})
            response = conn.getresponse()
            response.read()
            require(response.status == 200, f"Storage PUT HTTP {response.status}")
            return valid_etag(response.headers)
        finally:
            conn.close()


def upload(api, token, project_id, target, source, checksum, purpose, media_type):
    size = source.stat().st_size
    label = purpose + " " + source.name
    item, _ = api.request(label, "POST", "/uploads", {
        "projectId": project_id, "targetId": target, "purpose": purpose, "fileName": source.name,
        "mediaType": media_type, "sizeBytes": size, "checksumSha256": checksum,
    }, token=token, idem=operation(target, purpose, checksum), statuses=(201, 200))
    base = "/uploads/" + identifier(item["id"])
    file_id = identifier(item["fileId"])
    current, headers = api.request(label, "GET", base, token=token)
    if current["status"] not in ("VERIFIED", "VERIFYING"):
        part_size = current["partSizeBytes"]
        require(0 < part_size <= 128 * 1024 ** 2 and (size + part_size - 1) // part_size <= 10000,
                "Invalid multipart size")
        parts = []
        with source.open("rb") as stream:
            while data := stream.read(part_size):
                number = len(parts) + 1
                signed, _ = api.request(label, "POST", base + "/part-urls", {"partNumbers": [number]},
                                        token=token, idem=str(uuid4()))
                part, = signed["parts"]
                require(part["partNumber"] == number, "Issued part number mismatch")
                parts.append({"partNumber": number, "eTag": api.put(part["url"], data)})
        _, headers = api.request(label, "GET", base, token=token)
        api.request(label, "POST", base + "/complete", {"parts": parts, "checksumSha256": checksum},
                    token=token, idem=operation(base, "complete"), etag=valid_etag(headers), statuses=(202,))
    until = time.monotonic() + 240
    while True:
        current, _ = api.request(label, "GET", base, token=token)
        require(identifier(current["fileId"]) == file_id, "File identity changed")
        if current["status"] == "VERIFIED":
            return file_id
        require(current["status"] == "VERIFYING" and time.monotonic() < until, label + " verification incomplete")
        time.sleep(2)


def login(api, credentials):
    roles = {1: "SUPERVISOR", 2: "PM", 3: "OPERATOR", 4: "CREW", 5: "REPORTER"}
    actors = {}
    for account in credentials:
        name = account["key"]
        session, _ = api.request(name + " login", "POST", "/auth/login",
                                 {"email": account["email"], "password": account["password"]})
        require(session["user"]["role"] == roles[account["role"]] and not session["mustChangePassword"],
                name + " role mismatch")
        api.remember(name, session)
        actors[name] = (identifier(session["user"]["id"]), name)
    require(len(actors) == 14, "Expected fourteen distinct local actor keys")
    return actors
