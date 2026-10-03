"""Production HTTP demo producer, restricted to a local isolated API.

No SQL connection, reset, fake VERIFIED flag, or direct baseline seed.
Tokens are read only from environment variables and never written to state.
"""
import argparse
import hashlib
import http.client
import json
import os
from pathlib import Path
import time
import urllib.error
import urllib.parse
import urllib.request
import uuid

ROOT = Path(__file__).resolve().parents[3]
ASSETS = ROOT / "docs/demo/anh01"
FACTS = json.loads((ASSETS / "facts.json").read_text(encoding="utf-8"))
NAMESPACE = uuid.UUID("54e1b264-c5a2-4a8d-963a-c7bb2a3d7d01")


def operation(name):
    return str(uuid.uuid5(NAMESPACE, FACTS["code"] + ":" + name))


def validate_assets():
    manifest = json.loads((ASSETS / "asset-manifest.json").read_text(encoding="utf-8"))
    for item in manifest["files"]:
        content = (ASSETS / item["fileName"]).read_bytes()
        if len(content) != item["sizeBytes"] or hashlib.sha256(content).hexdigest() != item["checksumSha256"]:
            raise ValueError("Fixture checksum mismatch: " + item["fileName"])
    points = FACTS["coordinates"]
    length = sum(((b["x"] - a["x"]) ** 2 + (b["y"] - a["y"]) ** 2) ** .5 for a, b in zip(points, points[1:]))
    if length != FACTS["lengthMeters"] or FACTS["boundariesMeters"] != [0, 100, 200]:
        raise ValueError("Demo facts length/segments disagree")
    ai_assets = ROOT / "contracts/ai/fixtures/anh02"
    ai_manifest = json.loads((ai_assets / "asset-manifest.json").read_text(encoding="utf-8"))
    for key in ("video", "frame"):
        item = ai_manifest[key]
        content = (ai_assets / item["name"]).read_bytes()  # Fixed tiny repository fixtures only.
        if len(content) != item["sizeBytes"] or hashlib.sha256(content).hexdigest() != item["sha256"]:
            raise ValueError("AI synthetic fixture checksum mismatch")
    return manifest


def put_part(path, signed_url, offset, size, mime):
    """Only disposable loopback storage; send at most 64 KiB per buffer."""
    uri = urllib.parse.urlsplit(signed_url)
    if uri.scheme not in ("http", "https") or uri.hostname not in ("localhost", "127.0.0.1", "::1") or uri.username or uri.password:
        raise ValueError("Demo signed PUT must target owned loopback storage; URL omitted")
    connection = (http.client.HTTPSConnection if uri.scheme == "https" else http.client.HTTPConnection)(uri.hostname, uri.port, timeout=120)
    try:
        connection.putrequest("PUT", uri.path + ("?" + uri.query if uri.query else ""))
        connection.putheader("Content-Type", mime)
        connection.putheader("Content-Length", str(size))
        connection.endheaders()
        with path.open("rb") as source:
            source.seek(offset)
            remaining = size
            while remaining:
                chunk = source.read(min(65536, remaining))
                if not chunk:
                    raise RuntimeError("Source changed while uploading")
                connection.send(chunk)
                remaining -= len(chunk)
        response = connection.getresponse()
        etag = response.getheader("ETag")
        if not 200 <= response.status < 300 or not etag:
            raise RuntimeError("Object store PUT failed or missing ETag; URL omitted")
        return etag.strip('"')
    except (OSError, http.client.HTTPException):
        raise RuntimeError("Object store PUT unavailable; signed URL omitted") from None
    finally:
        connection.close()


class DemoApi:
    def __init__(self, base_url, state_path):
        self.base = base_url.rstrip("/") + "/api/v1/"
        self.state_path = state_path
        self.state = json.loads(state_path.read_text()) if state_path.exists() else {}
        if self.state.get("baseUrl", base_url) != base_url:
            raise ValueError("State belongs to a different API")
        self.state["baseUrl"] = base_url

    def save(self):
        self.state_path.parent.mkdir(parents=True, exist_ok=True)
        self.state_path.write_text(json.dumps(self.state, indent=2) + "\n", encoding="utf-8")

    def call(self, role, method, path, body=None, key=None, version=None):
        token = os.environ.get("ANH01_" + role.upper() + "_TOKEN")
        if not token:
            raise ValueError("Missing environment variable ANH01_" + role.upper() + "_TOKEN")
        if key:
            commands = self.state.setdefault("commands", {})
            identity = {"role": role, "method": method, "path": path, "body": body}
            original = commands.get(key)
            if original:
                if original["identity"] != identity:
                    raise ValueError("Command key already belongs to different payload/scope; use a separate demo state")
                version = original["version"]
            else:
                commands[key] = {"identity": identity, "version": version}
                self.save()  # Persist before transport; even an acknowledgement loss must resume exactly.
        headers = {"Authorization": "Bearer " + token, "Accept": "application/json"}
        if key:
            headers["Idempotency-Key"] = operation(key)
        if version:
            headers["If-Match"] = '"' + version.strip('"') + '"'
        data = None if body is None else json.dumps(body).encode()
        if data is not None:
            headers["Content-Type"] = "application/json"
        request = urllib.request.Request(self.base + path, data=data, headers=headers, method=method)
        try:
            with urllib.request.urlopen(request, timeout=60) as response:
                self.last_version = (response.headers.get("ETag") or "").strip('"')
                content = response.read()
                return json.loads(content) if content else None
        except urllib.error.HTTPError as error:
            # Never include request headers, bearer values, or signed URLs in diagnostics.
            try:
                problem = json.loads(error.read())
                code = problem.get("code", problem.get("title", "unknown"))
            except ValueError:
                code = "non-json-error"
            raise RuntimeError(f"{method} {path}: HTTP {error.code}, {code}") from None

    def upload(self, role, filename, purpose, project_id, target_id=None):
        return self.upload_path(role, ASSETS / filename, purpose, project_id, target_id)

    def upload_path(self, role, path, purpose, project_id, target_id=None, exact_size=None):
        path = Path(path)
        filename = path.name
        size = path.stat().st_size
        if exact_size is not None and size != exact_size:
            raise ValueError("Large acceptance requires exactly 8,589,934,592 actual bytes")
        digest = hashlib.sha256()
        with path.open("rb") as source:
            while chunk := source.read(65536):
                digest.update(chunk)
        checksum = digest.hexdigest()
        mime = {".pdf": "application/pdf", ".mp4": "video/mp4", ".srt": "application/x-subrip", ".png": "image/png", ".jpg": "image/jpeg"}[path.suffix.lower()]
        identity = {"sizeBytes": size, "checksumSha256": checksum, "purpose": purpose, "projectId": project_id, "targetId": target_id}
        prefix = "reporter-evidence/" if purpose == "REPORT_PHOTO" else ""
        label = "upload:" + filename
        known = self.state.get(label)
        if known and known.get("identity", identity) != identity:
            raise ValueError("State belongs to different file bytes or scope; use a separate demo state")
        if known and known.get("verified"):
            file = self.call(role, "GET", prefix + "files/" + known["fileId"])
            if file["status"] != "VERIFIED" or file["sizeBytes"] != size or file["checksumSha256"] != checksum:
                raise RuntimeError("Previously verified fixture no longer verified")
            return known["fileId"]
        if not known:
            body = {"fileName": filename, "mediaType": mime, "sizeBytes": size, "checksumSha256": checksum}
            if not prefix:
                body.update({"purpose": purpose, "projectId": project_id, "targetId": target_id})
            session = self.call(role, "POST", prefix + "uploads", body, label)
            known = {"id": session["id"], "fileId": session["fileId"], "identity": identity}
            self.state[label] = known
            self.save()
        session = self.call(role, "GET", prefix + "uploads/" + known["id"])
        if "completeRequest" in known:
            # The prior acknowledgement may have been lost. Replay the original
            # payload/If-Match before inspecting the new session version.
            self.call(role, "POST", prefix + "uploads/" + known["id"] + "/complete", known["completeRequest"], label + ":complete", known["completeVersion"])
        elif session["status"] not in ("VERIFYING", "VERIFIED", "COMPLETED"):
            part_size = session["partSizeBytes"]
            if part_size <= 0:
                raise ValueError("Invalid server part size")
            count = (size + part_size - 1) // part_size
            parts = []
            # Refresh URLs in small batches instead of assuming every URL is
            # still valid at the end of an 8 GiB transfer.
            for start in range(1, count + 1, 64):
                numbers = list(range(start, min(start + 64, count + 1)))
                urls = self.call(role, "POST", prefix + "uploads/" + known["id"] + "/part-urls", {"partNumbers": numbers}, label + ":urls:" + str(uuid.uuid4()))
                if sorted(p["partNumber"] for p in urls["parts"]) != numbers:
                    raise ValueError("Missing/duplicate signed part numbers")
                for part in urls["parts"]:
                    number = part["partNumber"]
                    etag = put_part(path, part["url"], (number - 1) * part_size, min(part_size, size - (number - 1) * part_size), mime)
                    parts.append({"partNumber": number, "eTag": etag})
            if sorted(p["partNumber"] for p in parts) != list(range(1, count + 1)):
                raise ValueError("Missing/duplicate signed part numbers")
            session = self.call(role, "GET", prefix + "uploads/" + known["id"])
            known.update({"completeRequest": {"parts": parts, "checksumSha256": checksum}, "completeVersion": session["version"]})
            self.save()
            self.call(role, "POST", prefix + "uploads/" + known["id"] + "/complete",
                known["completeRequest"], label + ":complete", known["completeVersion"])
        # A full 8 GiB verifier read can legitimately outlast the tiny-fixture polling window.
        deadline = time.monotonic() + (1800 if exact_size == 8589934592 else 60)
        while time.monotonic() < deadline:
            file = self.call(role, "GET", prefix + "files/" + known["fileId"])
            if file["status"] == "VERIFIED":
                if file["sizeBytes"] != size or file.get("checksumSha256", file.get("checksum")) != checksum:
                    raise RuntimeError("Verified metadata differs from actual source bytes")
                known["verified"] = True
                self.save()
                return known["fileId"]
            if file["status"] == "FAILED":
                raise RuntimeError("Fixture verification failed: " + filename)
            time.sleep(1)
        raise RuntimeError("Verification still pending; rerun against the same state to resume")

    def download(self, role, path, output, expected_sha256=None, expected_size=None):
        token = os.environ.get("ANH01_" + role.upper() + "_TOKEN")
        if not token:
            raise ValueError("Missing bearer environment variable for download")
        request = urllib.request.Request(self.base + path, headers={"Authorization": "Bearer " + token})
        digest, size = hashlib.sha256(), 0
        output = Path(output)
        output.parent.mkdir(parents=True, exist_ok=True)
        try:
            with urllib.request.urlopen(request, timeout=120) as response, output.open("wb") as target:
                while chunk := response.read(65536):
                    size += len(chunk)
                    digest.update(chunk)
                    target.write(chunk)
        except (urllib.error.URLError, OSError):
            raise RuntimeError("Protected download failed; partial local output may remain") from None
        if expected_size is not None and size != expected_size or expected_sha256 is not None and digest.hexdigest() != expected_sha256:
            raise RuntimeError("Downloaded bytes/checksum mismatch")
        return {"sizeBytes": size, "sha256": digest.hexdigest()}

    def wait(self, role, path, completed="SUCCEEDED"):
        for _ in range(60):
            view = self.call(role, "GET", path)
            if view["status"] == completed:
                return view
            if view["status"] == "FAILED":
                raise RuntimeError("Worker failed: " + view.get("errorCode", "unknown"))
            time.sleep(1)
        raise RuntimeError("Worker still pending; rerun with the same state")

    def anh02(self, operator_id, device_id, model_id):
        pid = self.state["projectId"]
        route, segment_set = self.state["route"], self.state["segmentSet"]
        scope = {"routeVersionId": route["routeVersionId"], "segmentSetId": segment_set["id"], "segmentIds": [segment_set["segments"][0]["id"]], "targetBand": "SURFACE"}
        plan = self.call("pm", "POST", f"projects/{pid}/survey-plans", {"scope": [scope], "plannedAt": FACTS["plannedAt"], "surveyType": "PERIODIC"}, "ai-demo-plan")
        task = self.call("pm", "POST", f"projects/{pid}/survey-tasks", {"planId": plan["id"], "scope": [scope], "surveyType": "PERIODIC", "operatorId": operator_id, "dueAt": FACTS["dueAt"], "accessPoint": None}, "ai-demo-task")
        accepted = self.call("operator", "POST", f"survey-tasks/{task['id']}/accept", key="ai-demo-accept", version=task["version"])
        video = self.upload_path("operator", ROOT / "contracts/ai/fixtures/anh02/synthetic-road-v1.mp4", "SURVEY_VIDEO", pid, task["id"])
        dataset = self.call("operator", "POST", f"survey-tasks/{task['id']}/datasets", {"videoFileIds": [video], "telemetryFileIds": [], "recordedAt": FACTS["plannedAt"], "deviceId": device_id, "scope": [scope], "pairs": [{"videoFileId": video, "telemetryFileId": None, "timeOffsetMilliseconds": 0}]}, "ai-demo-dataset", accepted["version"])
        if "aiDemoRequest" not in self.state:
            geometry = self.call("pm", "GET", f"projects/{pid}/geometry-package?routeVersionId={scope['routeVersionId']}&segmentSetId={scope['segmentSetId']}")
            if geometry["route"]["metadataStatus"] != "COMPLETE" or not self.last_version:
                raise RuntimeError("Geometry incomplete; no guessed CRS or geometry version")
            self.state["aiDemoRequest"] = {"datasetId": dataset["id"], "scope": scope, "modelVersionId": model_id, "preprocessingVersion": "demo-preprocess.v1", "configVersion": "demo-config.v1", "fixtureVersion": "synthetic-road-v1", "stage": "VIDEO_ANALYSIS", "expectedGeometryVersion": self.last_version}
            self.save()
        run = self.call("pm", "POST", f"projects/{pid}/ai-mock-runs", self.state["aiDemoRequest"], "ai-demo-run")
        self.wait("pm", f"projects/{pid}/ai-mock-runs/{run['id']}")
        result = self.call("pm", "GET", f"projects/{pid}/ai-mock-runs/{run['id']}/result")
        if result["mode"] != "MOCK":
            raise RuntimeError("Unexpected mock provenance")
        photo = self.upload_path("reporter", ROOT / "contracts/ai/fixtures/anh02/synthetic-road-v1-frame-1.png", "REPORT_PHOTO", None)
        file = self.call("reporter", "GET", "reporter-evidence/files/" + photo)
        report_body = {"description": "SYNTHETIC DEMO - not field evidence", "evidence": [{"fileId": photo, "fileVersion": file["version"], "locationSource": "UNKNOWN"}]}
        report = self.call("reporter", "POST", "reports", report_body, "demo-reporter-intake")
        replay = self.call("reporter", "POST", "reports", report_body, "demo-reporter-intake")
        if report["id"] != replay["id"]:
            raise RuntimeError("Intake replay changed report identity")
        self.state["anh02Summary"] = self.call("pm", "GET", f"projects/{pid}/reports/summary")
        for format in ("PDF", "ZIP"):
            export = self.call("pm", "POST", f"projects/{pid}/exports", {"kind": "DOSSIER", "format": format}, "demo-export-" + format)
            self.wait("pm", f"projects/{pid}/exports/{export['id']}")
            manifest = self.call("pm", "GET", f"projects/{pid}/exports/{export['id']}/manifest")
            self.state["demoExport" + format] = {"id": export["id"], "manifest": manifest, "download": self.download("pm", f"projects/{pid}/exports/{export['id']}/content", self.state_path.parent / ("anh02-demo." + format.lower()))}
        hold = self.call("supervisor", "POST", "retention/holds", {"scopeType": "PROJECT", "scopeId": pid, "reason": "SYNTHETIC DEMO - independent hold"}, "demo-project-hold")
        evaluation = self.call("pm", "POST", f"projects/{pid}/retention/evaluations", {}, "demo-retention-evaluate")
        self.state["anh02Evaluation"] = self.wait("pm", f"projects/{pid}/retention/evaluations/{evaluation['id']}", completed="COMPLETE")
        self.state["anh02Demo"] = {"mode": "MOCK_VERIFIED", "reportId": report["id"], "holdId": hold["id"], "note": "Intake remains UNASSIGNED/private; no Huy triage, approval or complete inventory fabricated; matching pending"}
        self.save()
        print("SYNTHETIC ANH-02 flow executed; intake remains private/unassigned; Huy readers pending")

    def prepare(self, pm_id):
        project = self.call("supervisor", "POST", "projects", {"code": FACTS["code"], "name": FACTS["name"],
            "primaryPmId": pm_id, "handoverDate": FACTS["handoverDate"], "warrantyEndDate": FACTS["warrantyEndDate"],
            "handoverFileIds": [], "description": "DEMO FICTIONAL - NOT LEGAL EVIDENCE",
            "engineeringUtmSrid": FACTS["engineeringSrid"], "operationId": operation("project")}, "project")
        pid = project["id"]
        self.state["projectId"] = pid
        self.save()
        document = self.upload("supervisor", "dossier-demo.pdf", "DOCUMENT", pid)
        self.call("supervisor", "POST", f"projects/{pid}/warranties", {"roadSectionId": None,
            "handoverDocumentId": project["handoverDocumentId"], "handoverDate": FACTS["handoverDate"],
            "warrantyStartDate": FACTS["warrantyStartDate"], "warrantyEndDate": FACTS["warrantyEndDate"],
            "retainedValue": None, "scope": "PROJECT", "terms": "DEMO-R01 fictional 200m route; DEMO only, no legal validity",
            "sourceDocumentId": document, "status": "ACTIVE", "operationId": operation("warranty")}, "warranty")
        prefix = f"projects/{pid}/road-geometry-drafts"
        draft = self.call("pm", "POST", prefix, {"sourceKind": "COORDINATES", "sourceCrs": FACTS["engineeringSrid"],
            "stationOriginMeters": FACTS["stationOriginMeters"], "changeReason": "DEMO initial route",
            "coordinates": FACTS["coordinates"], "sourceFileId": None, "trackIndex": None, "trackSegmentIndex": None,
            "widthProfile": FACTS["widthProfile"], "surveyWidthMeters": FACTS["surveyWidthMeters"],
            "roadCode": FACTS["roadCode"], "roadName": "DEMO fictional road"}, "draft")
        route = self.call("supervisor", "POST", prefix + "/" + draft["id"] + "/confirm",
            {"expectedCurrentVersionId": None, "effectiveFrom": "2026-09-01T00:00:00+07:00", "reason": "DEMO confirm"},
            "confirm", draft["version"])
        sets = f"projects/{pid}/road-sections/{route['roadSectionId']}/versions/{route['routeVersionId']}/segment-sets"
        segment_set = self.call("pm", "POST", sets, {"targetLengthMeters": 100, "remainderMode": "KEEP",
            "boundariesMeters": FACTS["boundariesMeters"]}, "segment-set")
        published = self.call("pm", "POST", sets + "/" + segment_set["id"] + "/publish",
            {"expectedPublishedSetId": None, "reason": "DEMO publish two segments"}, "publish", segment_set["version"])
        self.state.update({"route": route, "segmentSet": published})
        self.save()
        print("DEMO project/geometry prepared; projectId=" + pid)

    def survey(self, operator_id, device_id):
        pid = self.state["projectId"]
        route, segment_set = self.state["route"], self.state["segmentSet"]
        segments = {s["sequence"]: s["id"] for s in segment_set["segments"]}
        scopes = [{"routeVersionId": route["routeVersionId"], "segmentSetId": segment_set["id"],
            "segmentIds": [segments[1], segments[2]], "targetBand": "SURFACE"},
            {"routeVersionId": route["routeVersionId"], "segmentSetId": segment_set["id"],
            "segmentIds": [segments[1]], "targetBand": "RIGHT_EDGE"}]
        plan = self.call("pm", "POST", f"projects/{pid}/survey-plans",
            {"scope": scopes, "plannedAt": FACTS["plannedAt"], "surveyType": "BASELINE"}, "plan")
        task = self.call("pm", "POST", f"projects/{pid}/survey-tasks", {"planId": plan["id"],
            "scope": scopes, "surveyType": "BASELINE", "operatorId": operator_id, "dueAt": FACTS["dueAt"], "accessPoint": None}, "task")
        accepted = self.call("operator", "POST", "survey-tasks/" + task["id"] + "/accept", key="accept", version=task["version"])
        video = self.upload("operator", "survey-synthetic.mp4", "SURVEY_VIDEO", pid, task["id"])
        telemetry = self.upload("operator", "survey-synthetic.srt", "TELEMETRY", pid, task["id"])
        dataset = self.call("operator", "POST", "survey-tasks/" + task["id"] + "/datasets",
            {"videoFileIds": [video], "telemetryFileIds": [telemetry], "recordedAt": FACTS["plannedAt"],
             "deviceId": device_id, "scope": scopes, "pairs": [{"videoFileId": video, "telemetryFileId": telemetry, "timeOffsetMilliseconds": 0}]}, "dataset", accepted["version"])
        items = []
        for scenario in FACTS["assessmentScenarios"]:
            item = {k: v for k, v in scenario.items() if k not in ("segmentSequence", "fromMilliseconds", "toMilliseconds")}
            item.update({"routeVersionId": route["routeVersionId"], "segmentSetId": segment_set["id"],
                "segmentId": segments[scenario["segmentSequence"]], "evidence": []})
            if "fromMilliseconds" in scenario:
                item["evidence"] = [{"fileId": video, "fromMilliseconds": scenario["fromMilliseconds"], "toMilliseconds": scenario["toMilliseconds"]}]
            items.append(item)
        assessment = self.call("pm", "POST", "datasets/" + dataset["id"] + "/assessments",
            {"methodVersion": "pm-evidence-review.v1", "items": items}, "assessment", dataset["version"])
        passed = {k: items[0][k] for k in ("routeVersionId", "segmentSetId", "segmentId", "targetBand")}
        passed.update({"datasetId": dataset["id"], "assessmentId": assessment["id"], "expectedBaselineSelectionId": None})
        baseline = self.call("pm", "POST", f"projects/{pid}/baseline-selections",
            {"items": [passed], "reason": "DEMO partial baseline: S01 SURFACE only"}, "baseline")
        parent = self.call("pm", "GET", "survey-tasks/" + task["id"])
        missing = [{"routeVersionId": route["routeVersionId"], "segmentSetId": segment_set["id"],
            "segmentIds": [segments[1]], "targetBand": "RIGHT_EDGE"},
            {"routeVersionId": route["routeVersionId"], "segmentSetId": segment_set["id"],
            "segmentIds": [segments[2]], "targetBand": "SURFACE"}]
        supplement = self.call("pm", "POST", "survey-tasks/" + task["id"] + "/supplements",
            {"scope": missing, "reason": "DEMO missing right edge / obstructed segment2", "operatorId": operator_id}, "supplement", parent["version"])
        coverage = self.call("pm", "GET", "datasets/" + dataset["id"] + "/coverage")
        self.state.update({"datasetId": dataset["id"], "assessmentId": assessment["id"], "baseline": baseline,
            "supplement": supplement, "coverage": coverage, "evidence": "BE HTTP workflow executed against supplied local API"})
        self.save()
        print("DEMO partial baseline and supplement created through production APIs")


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--validate-only", action="store_true")
    parser.add_argument("--base-url", default="http://localhost:5000")
    parser.add_argument("--isolated-local", action="store_true", help="Explicitly attest local API uses an isolated demo DB/object-store")
    parser.add_argument("--phase", choices=("prepare", "survey", "all", "anh02", "large"), default="prepare")
    parser.add_argument("--pm-id")
    parser.add_argument("--operator-id")
    parser.add_argument("--device-id")
    parser.add_argument("--model-version-id", help="Existing RELEASED model; never provisioned by this script")
    parser.add_argument("--project-id")
    parser.add_argument("--task-id")
    parser.add_argument("--video-file", type=Path)
    parser.add_argument("--download-file", type=Path, help="New local output for actual 8 GiB download")
    parser.add_argument("--state", type=Path, default=Path(os.environ.get("TEMP", ".")) / "anh01-demo-state.json")
    args = parser.parse_args()
    validate_assets()
    if args.validate_only:
        print("DEMO facts and all asset hashes validated; no API/DB accessed")
    else:
        uri = urllib.parse.urlparse(args.base_url)
        if uri.scheme not in ("http", "https") or uri.hostname not in ("localhost", "127.0.0.1", "::1") or uri.path not in ("", "/") or uri.username or uri.password:
            parser.error("Demo writes require a loopback API URL without credentials or path")
        if not args.isolated_local:
            parser.error("--isolated-local required: localhost alone does not prove the backend DB is isolated")
        api = DemoApi(args.base_url.rstrip("/"), args.state)
        if args.phase in ("prepare", "all"):
            if not args.pm_id:
                parser.error("--pm-id required")
            api.prepare(str(uuid.UUID(args.pm_id)))
        if args.phase in ("survey", "all"):
            if not args.operator_id or not args.device_id:
                parser.error("--operator-id and --device-id required; current project membership and ACTIVE registry are prerequisites")
            api.survey(str(uuid.UUID(args.operator_id)), str(uuid.UUID(args.device_id)))
        if args.phase == "anh02":
            if not args.operator_id or not args.device_id or not args.model_version_id:
                parser.error("--operator-id, --device-id and --model-version-id required; Reporter token and enabled Development/Test workers are prerequisites")
            api.anh02(str(uuid.UUID(args.operator_id)), str(uuid.UUID(args.device_id)), str(uuid.UUID(args.model_version_id)))
        if args.phase == "large":
            if not args.project_id or not args.task_id or not args.video_file or not args.download_file:
                parser.error("--project-id, --task-id, --video-file and --download-file required; existing accepted task/current Operator")
            if args.download_file.exists() or args.video_file.resolve() == args.download_file.resolve():
                parser.error("Download output must be a new file, distinct from the actual source")
            file_id = api.upload_path("operator", args.video_file, "SURVEY_VIDEO", str(uuid.UUID(args.project_id)), str(uuid.UUID(args.task_id)), exact_size=8589934592)
            metadata = api.call("operator", "GET", "files/" + file_id)
            api.state["largeDownload"] = api.download("operator", "files/" + file_id + "/content", args.download_file, metadata["checksumSha256"], 8589934592)
            api.save()
            print("Actual 8 GiB VERIFIED upload and protected download hash matched; bounded 64 KiB buffers")
