"""Verify API-visible real inference and a PM decision against durable detection IDs."""
import re
import subprocess
import time

from real_flow_api import identifier, operation, require
from real_flow_foundation import PROJECTS


def read_worker_facts(compose, job, attempt):
    # The public job exposes the first detection only. Enumerate IDs read-only and resolve through the BE.
    command = ["docker", "--context", "desktop-linux", "exec", "-i", compose + "-sql-1", "sh", "-c",
               'export SQLCMDPASSWORD="$(cat /handoff/sql-password)"; exec /opt/mssql-tools/bin/sqlcmd -b -S localhost -U sa -h -1 -W']
    def query(sql):
        result = subprocess.run(command, input="SET NOCOUNT ON\n" + sql + "\nGO\n", capture_output=True, text=True)
        require(result.returncode == 0, "Local SQL detection readback failed")
        return [s.strip() for s in result.stdout.splitlines() if s.strip()]
    names = query("SELECT name FROM sys.databases WHERE name LIKE 'RoadGuard[_]FE[_]%'")
    require(len(names) == 1 and re.fullmatch(r"RoadGuard_FE_[0-9a-f]{32}", names[0]), "Ambiguous local database")
    ids = query(f"SELECT Id FROM [{names[0]}].dbo.AIDetections WHERE ProcessingJobId='{identifier(job)}' ORDER BY Id")
    db = "[" + names[0] + "].dbo."
    raw = query(f"SELECT a.FileId, a.Sha256, j.ModelVersionId FROM {db}AiWorkerArtifacts a "
                f"INNER JOIN {db}AiVideoWorkItems w ON a.WorkItemId = w.Id "
                f"INNER JOIN {db}ProcessingJobs j ON j.Id = w.ProcessingJobId "
                f"WHERE j.Id='{identifier(job)}' AND a.AttemptId='{identifier(attempt)}' AND a.Kind='RAW_RESULT'")
    require(len(raw) == 1, "Missing durable real inference result")
    file_id, digest, model_id = raw[0].split()
    require(re.fullmatch(r"[0-9a-f]{64}", digest) and identifier(model_id) == "72f84201-d033-4000-8000-000000000002",
            "Unexpected durable model or result checksum")
    return [identifier(i) for i in ids], {"fileId": identifier(file_id), "sha256": digest, "modelVersionId": identifier(model_id)}


def verify(api, users, code, record, compose, checkpoint, *, suffix=None, decide=True):
    project = record["foundation"]
    project_id = project["projectId"]
    suffix = suffix or PROJECTS[code][1]
    pm = users["pm-" + suffix][1]
    supervisor = users["supervisor-a"][1]
    forbidden = {"wrongPm": users["pm-" + ("duong-so-2" if suffix == "kenh-dong" else "kenh-dong")][1],
                 "crew": users["crew-" + suffix + "-1"][1], "reporter": users["reporter-" + suffix + "-1"][1]}
    root = "/projects/" + project_id
    path = root + "/ai-videos?datasetId=" + identifier(record["datasetId"])
    api.request(code + " anonymous read", "GET", path, statuses=(401,))
    for role, token in forbidden.items():
        api.request(code + " " + role, "GET", path, token=token, statuses=(403,))
    api.request(code + " supervisor read", "GET", path, token=supervisor)
    end = time.monotonic() + 1800
    previous = None
    while True:
        rows, _ = api.request(code + " real jobs", "GET", path, token=pm)
        items = rows["items"]
        superseded = record.get("supersededJob")
        if superseded:
            require(superseded in items and superseded["state"] == "TerminalFailed" and
                    superseded["sourceVideoFileId"] == record["pair"]["videoFileId"], "Superseded job changed")
            items = [item for item in items if item != superseded]
        require(len(items) <= 1 and not rows["hasMore"], "Duplicate current real jobs")
        if items:
            work, = items
            require(work["sourceVideoFileId"] == record["pair"]["videoFileId"], "Unexpected inference source")
            if work["state"] != previous:
                print(code, "real worker", work["state"], flush=True)
                previous = work["state"]
            if work["state"] == "Ingested":
                break
            require(work["state"] in ("Pending", "Admitted", "Running", "ArtifactsReady"),
                    code + " inference failed: " + str(work.get("errorCode")))
        require(time.monotonic() < end, code + " inference timeout")
        time.sleep(5)
    job_id = identifier(work["jobId"])
    job, _ = api.request(code + " job readback", "GET", "/processing-jobs/" + job_id, token=pm)
    require(job["id"] == job_id and job["status"] == "SUCCEEDED", "Ingested work has no successful job")
    telemetry, _ = api.request(code + " telemetry", "GET", root + "/ai-videos/" + identifier(work["workId"]) + "/telemetry", token=pm)
    correction = record.get("telemetryCorrection")
    expected_source = "PAIRED_SRT"
    if correction:
        expected_source = "EXTERNAL_SRT"
        require(telemetry["headRevisionId"] == correction["revisionId"]
                and len(telemetry["intervals"]) == correction["unchangedGpsFixes"]
                and telemetry["intervals"][-1]["endMs"] == correction["videoDurationMs"], "Telemetry correction mismatch")
    require(telemetry["attemptId"] == work["currentAttemptId"] and telemetry["sourceVideoFileId"] == work["sourceVideoFileId"]
            and telemetry["positionMeaning"] == "DRONE_REFERENCE_NOT_GROUND_LOCATION"
            and telemetry["measuredTelemetryStatus"] == expected_source, "Measured telemetry readback mismatch")
    ids, raw = read_worker_facts(compose, job_id, work["currentAttemptId"])
    require((job["resultId"] in ids) if ids else job["resultId"] is None, "Job result does not match detection readback")
    candidates = []
    active_decisions = {}
    review = None
    second = None
    for detection in ids:
        candidate_path = root + "/ai-candidates/" + detection
        item, _ = api.request(code + " PM candidate", "GET", candidate_path, token=pm)
        require(item["mode"] == "REAL" and item["jobId"] == job_id and item["projectId"] == project_id
                and item["datasetVersionId"] == record["datasetId"] and item["detectionId"] == detection
                and item["modelVersionId"] == "72f84201-d033-4000-8000-000000000002"
                and item["sourceVideoFileId"] == work["sourceVideoFileId"] and item["attemptId"] == work["currentAttemptId"]
                and item["positionStatus"] == "UNKNOWN" and re.fullmatch(r"[0-9a-f]{64}", item["frameSha256"])
                and item["resultHash"] == raw["sha256"], "Candidate provenance mismatch")
        if item.get("disposition"):
            active_decisions[detection] = identifier(item["disposition"]["decisionId"])
        candidates.append({k: item[k] for k in ("detectionId", "modelVersionId", "resultId", "resultHash",
                          "typeCode", "positionStatus", "frameFileId", "frameSha256", "timestampMs")})
        if not decide:
            candidates[-1]["confidence"] = item.get("confidence")
        if review is None and item["typeCode"] != "REPAIR_PATCH":
            review = item
        elif second is None:
            second = item
    evidence = {"workId": identifier(work["workId"]), "jobId": job_id, "attemptId": identifier(work["currentAttemptId"]),
                "attemptNumber": job["attemptNumber"], "firstResultId": job["resultId"], "rawResult": raw,
                "telemetryStatus": telemetry["measuredTelemetryStatus"], "positionMeaning": telemetry["positionMeaning"],
                "measuredIntervals": len(telemetry["intervals"]), "detectionCount": len(ids), "candidates": candidates}
    require(record.get("inference", evidence) == evidence, "Real model results changed on replay")
    record["inference"] = evidence
    record["videoAuthorization"] = {"anonymous": 401, "wrongPm": 403, "crew": 403, "reporter": 403,
                                    "ownPm": 200, "supervisor": 200}
    checkpoint()
    if not decide:
        return
    if review is None:
        record["pmReview"] = {"status": "NOT_RUN", "reason": "No non-repair candidate was observed"}
    else:
        candidate_path = root + "/ai-candidates/" + review["detectionId"]
        for role, token in forbidden.items():
            api.request(code + " " + role + " candidate read", "GET", candidate_path, token=token, statuses=(403,))
        api.request(code + " supervisor candidate read", "GET", candidate_path, token=supervisor)
        decision_path = root + "/candidate-decisions"
        body = {"sourceKind": "AI_DETECTION", "sourceId": review["detectionId"], "sourceVersion": review["sourceVersion"],
                "geometryVersion": review["geometryVersion"], "decision": "KEEP_NEW", "classification": {
                    "defectTypeCode": review["typeCode"], "severity": "LOW", "roadSectionVersionId": project["routeVersionId"]},
                "reason": "DEMO manual PM review of paired telemetry. Ground defect position is unverified."}
        for role, token in forbidden.items():
            api.request(code + " " + role + " decision", "POST", decision_path, body, token=token,
                        idem=operation(project_id, review["detectionId"], role, "negative-decision"), statuses=(403,))
        decision, _ = api.request(code + " PM keep", "POST", decision_path, body, token=pm,
                                  idem=operation(project_id, review["detectionId"], "keep"), statuses=(200, 201))
        defect_id = identifier(decision["defectId"])
        api.request(code + " project defect", "GET", root + "/defects/" + defect_id, token=pm)
        record["pmReview"] = {"status": "PASS", "decisionId": identifier(decision["id"]), "defectId": defect_id,
                              "sourceDetectionId": review["detectionId"], "decision": "KEEP_NEW"}
    if second is not None:
        path = root + "/candidate-decisions"
        payload = {"sourceKind": "AI_DETECTION", "sourceId": second["detectionId"],
                   "sourceVersion": second["sourceVersion"], "geometryVersion": second["geometryVersion"],
                   "decision": "REJECT", "reason": "DEMO PM rejects a separate proposal during local workflow verification."}
        stale = dict(payload, sourceVersion="stale-demo-version")
        api.request(code + " reject stale source", "POST", path, stale, token=pm,
                    idem=operation(project_id, second["detectionId"], "stale-reject"), statuses=(409,))
        rejection, _ = api.request(code + " PM reject", "POST", path, payload, token=pm,
                                   idem=operation(project_id, second["detectionId"], "reject"), statuses=(200, 201))
        rejection_id = identifier(rejection["id"])
        saved, _ = api.request(code + " rejected decision", "GET", path + "/" + rejection_id, token=pm)
        require(saved["decision"] == "REJECT" and saved.get("defectId") is None and saved["sourceId"] == second["detectionId"],
                "Rejected candidate produced unexpected effects")
        record["pmReject"] = {"status": "PASS", "decisionId": rejection_id, "defectId": None,
                              "sourceDetectionId": second["detectionId"], "staleSourceHttp": 409}
    else:
        record["pmReject"] = {"status": "NOT_RUN", "reason": "No separate candidate was observed"}
    for decision in (record["pmReview"], record["pmReject"]):
        if decision["status"] == "PASS":
            source_id = decision["sourceDetectionId"]
            refreshed, _ = api.request(code + " current disposition", "GET", root + "/ai-candidates/" + source_id, token=pm)
            current_id = identifier(refreshed["disposition"]["decisionId"])
            require(current_id == decision["decisionId"], "Candidate head differs from its recorded PM decision")
            active_decisions[source_id] = current_id
    record["reviewCounts"] = {"reviewed": len(active_decisions), "pending": len(ids) - len(active_decisions)}
    checkpoint()
    print(code, "verified", len(ids), "detections", flush=True)
