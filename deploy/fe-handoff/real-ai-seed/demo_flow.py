"""Reporter, Supervisor, PM and Crew fixtures on the dedicated local stack."""
import argparse
import hashlib
import json
from datetime import datetime, timedelta, timezone
from pathlib import Path
from urllib.parse import quote

from demo_flow_api import Api, Rejected, now, require, save, stable
from demo_flow_repairs import seed_repair

PROJECTS = {"DKD": ("Duong Kenh Dong", "kenh-dong", "1123-part-00.mp4"),
            "DS2": ("Duong so 2", "duong-so-2", "11234.mp4")}
REASON = "DEMO local workflow exercise"


def frames(media, clip, work):
    result = {}
    for stage, second in (("before", 1), ("after", 3)):
        target = work / (Path(clip).stem + "-" + stage + ".jpg")
        if not target.exists():
            import cv2
            cap = cv2.VideoCapture(str(media / clip))
            try:
                cap.set(cv2.CAP_PROP_POS_MSEC, 1000 * second)
                ok, frame = cap.read()
                require(ok, "Could not read named fixture video")
                ratio = min(1, 800 / frame.shape[1])
                frame = cv2.resize(frame, None, fx=ratio, fy=ratio)
                # Visible provenance when FE downloads a simulated before/after.
                cv2.putText(frame, "DEMO workflow - " + stage, (16, 34),
                            cv2.FONT_HERSHEY_SIMPLEX, 0.8, (255, 255, 255), 2)
                target.parent.mkdir(parents=True, exist_ok=True)
                require(cv2.imwrite(str(target), frame), "Could not encode DEMO JPEG")
            finally:
                cap.release()
        result[stage] = target.read_bytes()
    return result


class Project:
    def __init__(self, api, key, foundation, images):
        self.api, self.key, self.f, self.images = api, key, foundation, images
        self.name, role, _ = PROJECTS[key]
        self.pm, self.operator = "pm-" + role, "operator-" + role
        self.crews = ["crew-" + role + "-" + str(i) for i in (1, 2)]
        self.reporters = ["reporter-" + role + "-" + str(i) for i in (1, 2)]
        self.sup = "supervisor-" + ("a" if key == "DKD" else "b")
        self.other_pm = "pm-" + ("duong-so-2" if key == "DKD" else "kenh-dong")
        self.pid = foundation["projectId"]
        self.root = "/projects/" + self.pid
        self.route, self.segset = foundation["routeVersionId"], foundation["segmentSetId"]
        self.segment = foundation["segmentIds"][0]
        self.due = (datetime.now(timezone.utc) + timedelta(days=7)).isoformat()
        fact = api.read(self.pm, self.root + "/work-package")
        require(fact["name"] == self.name, "Wrong DEMO project")
        geometry = api.call(self.pm, self.root + "/geometry-package?routeVersionId=" + self.route
                            + "&segmentSetId=" + self.segset)
        self.geometry_version = geometry["etag"].strip('"')
        self.crs = geometry["body"]["route"]["crsProfileRevisionId"]
        self.evidence = {}

    def cmd(self, key, actor, path, body, version=None, **options):
        return self.api.mutation(self.key + ":" + key, actor, path, body, version, **options)["body"]

    def post(self, key, actor, path, body, resource, reader=None):
        return self.cmd(key, actor, path, body, self.api.etag(reader or actor, resource))

    def report(self, key, reporter):
        if reporter not in self.evidence:
            file, _ = self.api.photo(self.key + "-reporter-photo-" + reporter, reporter,
                                     self.pid, None, "REPORTER", self.images["before"])
            self.evidence[reporter] = file
        file = self.evidence[reporter]
        version = self.api.read(reporter, "/reporter-evidence/files/" + file)["version"]
        report = self.cmd(key, reporter, "/reports", {
            "description": "DEMO " + self.name + " " + key,
            "evidence": [{"fileId": file, "fileVersion": version, "locationSource": "UNKNOWN"}]},
            replay=key == "waiting-01")
        path = "/reports/" + report["id"]
        own = self.api.read(reporter, path)
        require(own["id"] == report["id"] and own["description"].endswith(key), "Reporter readback mismatch")
        match = None
        for query in ("/cases?pageSize=100", "/cases?pageSize=100&projectId=" + self.pid):
            page = query
            while page and match is None:
                records = self.api.read(self.sup, page)
                match = next((r for r in records["items"] if report["id"] in r["reportIds"]), None)
                cursor = records.get("nextCursor")
                page = query + "&cursor=" + quote(cursor, safe="") if cursor else None
        require(match is not None, "Missing case for " + key)
        return {"logicalKey": key, "reporter": reporter, "reportId": report["id"],
                "caseId": match["id"], "caseStatus": match["status"], "locationSource": "UNKNOWN",
                "evidenceIds": report["evidenceIds"], "fileId": file}

    def triage(self, row):
        path = "/cases/" + row["caseId"]
        self.post(row["logicalKey"] + "-triage", self.sup, path + "/triage", {
            "projectId": self.pid, "verificationMethod": "EXISTING_EVIDENCE", "reason": REASON,
            "routeVersionId": self.route, "segmentSetId": self.segset,
            "geometryVersion": self.geometry_version}, path)
        current = self.api.read(self.sup, path)
        require(current["projectId"] == self.pid, "Case assigned to wrong project")
        row.update(caseStatus=current["status"], triageActor=self.sup)

    def open_defect(self, row):
        key = row["logicalKey"]
        self.triage(row)
        source = self.api.read(self.pm, self.root + "/candidates?sourceKind=REPORT&sourceId=" + row["reportId"])
        decision = self.cmd(key + "-candidate", self.pm, self.root + "/candidate-decisions", {
            "sourceKind": "REPORT", "sourceId": row["reportId"], "sourceVersion": source["source"]["version"],
            "geometryVersion": source["geometryVersion"], "decision": "KEEP_NEW", "reason": REASON,
            "classification": {"defectTypeCode": "POTHOLE", "severity": "LOW", "roadSectionVersionId": self.route}})
        return decision["defectId"]

    def verify_defect(self, row, defect):
        key = row["logicalKey"]
        path = self.root + "/defects/" + defect
        self.post(key + "-assess", self.pm, path + "/assessments", {
            "defectTypeCode": "POTHOLE", "severity": "LOW", "evidenceIds": row["evidenceIds"],
            "reason": REASON}, path)
        self.post(key + "-verify", self.pm, path + "/verification-decisions", {
            "decision": "CONFIRM", "verificationMethod": "EXISTING_EVIDENCE",
            "evidenceIds": row["evidenceIds"], "reason": REASON}, path)
        state = self.api.read(self.pm, path)["status"]
        require(state.upper() == "VERIFIED", "Defect verification readback mismatch")
        row.update(defectId=defect, defectStatus=state, verificationActor=self.pm,
                   wrongPmStatus=self.api.denial(self.other_pm, path))
        return defect

    def task_input(self, defect, crew, purpose):
        return {"defectId": defect, "defectVersion": self.api.etag(self.pm, self.root + "/defects/" + defect).strip('"'),
                "sourceKind": "REPORTER", "routeVersionId": self.route, "segmentSetId": self.segset,
                "crsProfileRevisionId": self.crs, "purpose": purpose, "requiredMeasurementType": 1, "measurementScope": "{}",
                "instructions": REASON, "assignedToUserId": self.api.ids[crew], "dueAt": self.due}

    def start_task(self, key, path, crew):
        self.post(key + "-accept", crew, path + "/accept", {"reason": REASON}, path)
        self.post(key + "-start", crew, path + "/start",
                  {"originId": stable(self.key + key + "-first-start"), "claimedAt": now()}, path)
        return self.api.read(crew, path + "/start-origin")["id"]

    def capture(self, key, crew, task, purpose, stage):
        file, digest = self.api.photo(self.key + "-" + key + "-" + purpose, crew, self.pid,
                                      task, purpose, self.images[stage])
        evidence = {"captureOriginId": stable(self.key + key + purpose), "fileId": file,
                    "purpose": purpose, "checksumSha256": digest, "mediaType": "image/jpeg", "capturedAt": now()}
        if purpose == "AFTER":
            evidence["attemptChecklist"] = "POST_REPAIR_CAPTURE"
        return evidence

    def measurement(self, key, defect, crew, decision):
        base = self.root + "/field-inspection-tasks"
        task = self.cmd(key + "-task", self.pm, base, self.task_input(defect, crew, "PRE_MEASUREMENT"))["id"]
        path = base + "/" + task
        started = self.start_task(key, path, crew)
        image = self.capture(key, crew, task, "MEASUREMENT", "before")
        data = self.post(key + "-submission", crew, path + "/submissions", {
            "originId": stable(self.key + key + "-submission"), "startOriginId": started,
            "captureType": "MEASUREMENT", "measurements": [sample(2)], "evidence": [image],
            "locationProof": self.location()}, path)
        submitted = self.api.read(self.pm, path + "/submissions/" + data["id"])
        require(submitted["readiness"] == "READY", "Measurement missing required evidence")
        if decision is None:
            return {"taskId": task, "crew": crew, "status": self.api.read(self.pm, path)["status"],
                    "submissionId": data["id"], "fileId": image["fileId"], "review": None}
        review = self.post(key + "-review", self.pm, path + "/reviews", {
            "submissionId": data["id"], "decision": decision, "reason": REASON}, path)
        current = self.api.read(crew, path)
        require(review["decision"] == decision, "Measurement review mismatch")
        return {"taskId": task, "crew": crew, "status": current["status"], "review": decision,
                "submissionId": data["id"], "reviewId": review["id"], "fileId": image["fileId"],
                "wrongPmStatus": self.api.denial(self.other_pm, path)}

    def location(self):
        return {"kind": "POSITION_CHECKLIST", "checklist": "ROUTE_CHAINAGE_MARKINGS_CONFIRMED",
                "observedRouteVersionId": self.route,
                "observedChainageMeters": 1}


def sample(value):
    return {"sampleId": "DEMO-depth", "type": "DepressionDepth", "value": value, "state": "KNOWN",
            "dimension": "LENGTH", "unit": "mm", "locationReason": "DEMO field exercise",
            "instrument": "DEMO depth gauge", "method": "DEMO fixture measurement"}

def surveys(p):
    results = []
    scope = [{"routeVersionId": p.route, "segmentSetId": p.segset, "segmentIds": [p.segment], "targetBand": "SURFACE"}]
    for i in range(9):
        key = "survey-" + str(i + 1)
        due = (datetime.now(timezone.utc) + timedelta(days=-2 if i == 7 else 6)).isoformat()
        created = p.cmd(key, p.pm, p.root + "/survey-tasks", {"scope": scope, "operatorId": p.api.ids[p.operator],
                              "surveyType": "BASELINE", "dueAt": due})
        task = created["id"]
        path = "/survey-tasks/" + task
        if i in (1, 3, 6):
            p.post(key + "-accept", p.operator, path + "/accept", None, path)
        if i in (2, 4):
            p.post(key + "-decline", p.operator, path + "/decline", {"reason": REASON}, path, p.pm)
        if i == 5:
            p.post(key + "-cancel", p.pm, path + "/cancel", {"reason": REASON}, path)
        row = p.api.read(p.pm, path)
        require(row["projectId"] == p.pid and row["operatorId"] == p.api.ids[p.operator], "Survey scope mismatch")
        result = {"taskId": task, "logicalKey": key, "operator": p.operator, "status": row["status"],
                  "dueAt": row.get("dueAt"), "wrongPmStatus": p.api.denial(p.other_pm, path)}
        if i == 3:
            upload = p.cmd(key + "-open-upload", p.operator, "/uploads", {
                "purpose": "SURVEY_VIDEO", "projectId": p.pid, "targetId": task,
                "fileName": "DEMO-pending.mp4", "mediaType": "video/mp4",
                "sizeBytes": 1024, "checksumSha256": hashlib.sha256(bytes(1024)).hexdigest()})
            result.update(uploadId=upload["id"], uploadStatus=p.api.read(p.operator, "/uploads/" + upload["id"])["status"])
        results.append(result)
    return results


def scenario(p, out, write):
    api = p.api
    for i in range(1, 11):
        row = p.report(f"waiting-{i:02}", p.reporters[i % 2])
        row["wrongReporterStatus"] = api.denial(p.reporters[(i + 1) % 2], "/reports/" + row["reportId"])
        out["waitingReports"].append(row)
    write()
    for extra, outcome in (("no-defect", "NO_DEFECT"), ("needs-evidence", "NEEDS_EVIDENCE")):
        row = p.report(extra, p.reporters[0])
        p.triage(row)
        path = "/cases/" + row["caseId"]
        p.post(extra + "-conclusion", p.pm, path + "/conclusions", {
            "outcome": outcome, "defectIds": [], "evidenceIds": row["evidenceIds"], "reason": REASON}, path)
        row["caseStatus"] = api.read(p.pm, path)["status"]
        row["outcome"] = outcome
        out["otherReports"].append(row)
    policy_path = p.root + "/repair-policies/drafts"
    policy = p.cmd("policy", p.pm, policy_path, {"defectTypeCode": "POTHOLE", "checklistVersion": "DEMO-depth-v1",
                   "measurements": [{"code": "DepressionDepth", "unit": "mm", "minimum": 0, "maximum": 3}],
                   "stopConditions": [], "reason": "DEMO fixture rule from LD07RoadCoverageSqlTests"})
    published = p.cmd("publish-policy", p.pm, policy_path + "/" + policy["id"] + "/publish",
                      {"reason": REASON}, '"' + policy["version"] + '"')
    for i in range(1, 11):
        key = f"lineage-{i:02}"
        crew = p.crews[(i - 1) % 2]
        row = p.report(key, p.reporters[(i - 1) % 2])
        defect = p.open_defect(row)
        decision = ("CONFIRM", "NO_DEFECT", "SUPPLEMENT")[(i - 1) % 3]
        field_source = defect
        if i == 1:
            # The offline exercise needs a retained open source through later imports.
            origin = p.report("offline-field-source", p.reporters[0])
            field_source = p.open_defect(origin)
            out["offlineSource"] = dict(origin, defectId=field_source)
        measurement = p.measurement("measurement-" + key, field_source, crew, decision)
        out["measurements"].append(dict(measurement, defectId=field_source))
        p.verify_defect(row, defect)
        if i == 1:
            path = "/cases/" + row["caseId"]
            p.post(key + "-conclude", p.pm, path + "/conclusions", {
                "outcome": "CONFIRMED", "defectIds": [defect], "evidenceIds": row["evidenceIds"], "reason": REASON}, path)
            published_report = p.post(key + "-publish", p.pm, path + "/publications", {
                "reportIds": [row["reportId"]], "defectIds": [defect], "evidenceIds": [],
                "summary": "DEMO confirmed pavement defect"}, path)
            row.update(publicationId=published_report["id"], caseStatus=api.read(p.pm, path)["status"])
            tasks = p.root + "/field-inspection-tasks"
            offline = p.cmd("offline-task", p.pm, tasks, p.task_input(field_source, crew, "PRE_MEASUREMENT"))["id"]
            field = tasks + "/" + offline
            p.post("offline-accept", crew, field + "/accept", {"reason": REASON}, field)
            require(api.read(crew, field)["id"] == offline, "Missing offline assigned task")
            out.update(offlineTaskId=offline, offlineCrew=crew, pm=p.pm, supervisor=p.sup)
            write()
        out["defects"].append(row)
        mode = "NORMAL" if i % 2 else "FAST_TRACK"
        repair = seed_repair(p, "repair-" + key, defect, crew, mode, i <= 5, published["id"])
        out["repairs"].append(repair)
        write()
        print(f"{p.key} {key} {mode} {repair['state']}", flush=True)
    requests_path = p.root + "/receiving-requests"
    for measurement in out["measurements"]:
        if measurement["review"] != "SUPPLEMENT":
            continue
        crew = measurement["crew"]
        duties = api.read(crew, requests_path + "?scopeId=" + measurement["taskId"])["items"]
        duty = next(d for d in duties if d["sourceId"] == measurement["reviewId"])
        path = requests_path + "/" + duty["id"]
        p.post("duty-" + duty["id"], crew, path + "/ack", {"claimedDeviceAt": now()}, path)
        current = api.read(crew, path)
        require(current["acknowledgedBy"] == api.ids[crew], "Missing Crew acknowledgment")
        measurement["receivingRequestId"] = duty["id"]
        measurement["acknowledgmentId"] = current["acknowledgmentId"]
    out["surveys"] = surveys(p)
    for crew in p.crews:
        inbox = api.read(crew, "/notifications?limit=100")["items"]
        if inbox:
            # Choose a stable item across replays, allowing more inbox deliveries later.
            select = p.api.journal / (stable(p.key + crew + "-inbox-choice") + ".json")
            if not select.exists():
                save(select, inbox[0]["id"])
            nid = json.loads(select.read_text(encoding="utf-8"))
            path = "/notifications/" + nid
            p.post(crew + "-mark-read", crew, path + "/read", None, path)
            out["notifications"].append({"actor": crew, "readId": nid, "read": api.read(crew, path)["read"],
                                         "unreadIds": [n["id"] for n in inbox if not n["read"] and n["id"] != nid]})
    write()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--work", type=Path, required=True)
    parser.add_argument("--media", type=Path, required=True, help="Directory containing the approved MP4s")
    parser.add_argument("--origin", default="http://127.0.0.1:28081")
    parser.add_argument("--storage-port", type=int, default=29001)
    parser.add_argument("--project", choices=PROJECTS)
    args = parser.parse_args()
    work = args.work
    foundations = json.loads((work / "real-flow.json").read_text(encoding="utf-8-sig"))["projects"]
    creds = json.loads((work / "private/demo-accounts.json").read_text(encoding="utf-8-sig"))
    api = Api(args.origin, args.storage_port, creds, work / "private/demo-journal")
    catalog = json.loads(Path(__file__).with_name("accounts.json").read_text(encoding="utf-8"))["Actors"]
    require(set(api.users) == {a["Key"] for a in catalog}, "Canonical credential catalog mismatch")
    for user in catalog:
        key = user["Key"]
        api.login(key)
        require(api.ids[key] == user["Id"], "Unexpected canonical actor ID")
    output = work / "demo-flow.json"
    receipt = json.loads(output.read_text(encoding="utf-8")) if output.exists() else {"projects": {}}
    receipt.update(provenance="DEMO", actorCount=len(api.ids), status="IN_PROGRESS")
    for key in ([args.project] if args.project else PROJECTS):
        f = foundations[key]["foundation"]
        images = frames(args.media, PROJECTS[key][2], work / "demo-images")
        p = Project(api, key, f, images)
        out = {"projectId": p.pid, "name": p.name, "photoSha256": {k: hashlib.sha256(v).hexdigest() for k, v in images.items()},
               "waitingReports": [], "otherReports": [], "defects": [], "repairs": [], "measurements": [],
               "surveys": [], "notifications": []}
        receipt["projects"][key] = out
        scenario(p, out, lambda: save(output, receipt))
        out["status"] = "PASS_WITH_DOCUMENTED_GAPS"
    receipt.update(httpCounts=api.http_counts, replayByMutationFamily=api.replays, status="PARTIAL", observedAt=now(),
                   gaps=[
                       {"status":"NOT_RUN","route":"/projects/{project}/defects/{defect}/verification-decisions",
                        "reason":"Only the project PM may verify a defect. Supervisor triages the source Case."},
                       {"status":"NOT_RUN","route":"/projects/{project}/repair-packages/{package}/items/{item}/tasks/{task}/execution-start",
                        "reason":"DEMO handover/warranty coverage cannot authorize FastTrack execution; real project handover and valid coverage evidence are required."},
                       {"status":"NOT_RUN","route":"/projects/{project}/field-inspection-tasks/{task}/reviews",
                        "reason":"SUPPLEMENT retains the task for rework. NO_DEFECT is the supported negative conclusion."},
                       {"status":"NOT_RUN","route":"/processing-jobs","reason":"A failed inference and a retry are not induced on the two approved successful clips."}])
    save(output, receipt)
    print("DEMO role workflows recorded", flush=True)


if __name__ == "__main__":
    main()
