"""Production HTTP demo producer, restricted to a local isolated API.

No SQL connection, reset, fake VERIFIED flag, or direct baseline seed.
Tokens are read only from environment variables and never written to state.
"""
import argparse
import hashlib
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
    return manifest


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
        label = "upload:" + filename
        known = self.state.get(label)
        if known and known.get("verified"):
            file = self.call(role, "GET", "files/" + known["fileId"])
            if file["status"] != "VERIFIED":
                raise RuntimeError("Previously verified fixture no longer verified")
            return known["fileId"]
        path = ASSETS / filename
        content = path.read_bytes()
        checksum = hashlib.sha256(content).hexdigest()
        mime = {".pdf": "application/pdf", ".mp4": "video/mp4", ".srt": "application/x-subrip"}[path.suffix]
        if not known:
            session = self.call(role, "POST", "uploads", {"purpose": purpose, "projectId": project_id,
                "targetId": target_id, "fileName": filename, "mediaType": mime,
                "sizeBytes": len(content), "checksumSha256": checksum}, label)
            known = {"id": session["id"], "fileId": session["fileId"]}
            self.state[label] = known
            self.save()
        session = self.call(role, "GET", "uploads/" + known["id"])
        if session["status"] not in ("VERIFYING", "VERIFIED", "COMPLETED"):
            part_size = session["partSizeBytes"]
            count = (len(content) + part_size - 1) // part_size
            urls = self.call(role, "POST", "uploads/" + known["id"] + "/part-urls",
                {"partNumbers": list(range(1, count + 1))}, label + ":urls:" + str(uuid.uuid4()))
            parts = []
            for part in urls["parts"]:
                number = part["partNumber"]
                data = content[(number - 1) * part_size:number * part_size]
                signed = urllib.request.Request(part["url"], data=data, method="PUT")
                try:
                    with urllib.request.urlopen(signed, timeout=60) as response:
                        etag = response.headers.get("ETag")
                        if not etag:
                            raise RuntimeError("Object store PUT missing ETag")
                        parts.append({"partNumber": number, "eTag": etag.strip('"')})
                except (urllib.error.URLError, OSError):
                    raise RuntimeError("Object store PUT unavailable; signed URL omitted") from None
            session = self.call(role, "GET", "uploads/" + known["id"])
            self.call(role, "POST", "uploads/" + known["id"] + "/complete",
                {"parts": parts, "checksumSha256": checksum}, label + ":complete", session["version"])
        for _ in range(60):
            file = self.call(role, "GET", "files/" + known["fileId"])
            if file["status"] == "VERIFIED":
                known["verified"] = True
                self.save()
                return known["fileId"]
            if file["status"] == "FAILED":
                raise RuntimeError("Fixture verification failed: " + filename)
            time.sleep(1)
        raise RuntimeError("Verification still pending; rerun against the same state to resume")

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
    parser.add_argument("--phase", choices=("prepare", "survey", "all"), default="prepare")
    parser.add_argument("--pm-id")
    parser.add_argument("--operator-id")
    parser.add_argument("--device-id")
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
