"""Mode-specific repair acceptance through the assigned Crew's actual task."""
from datetime import datetime, timedelta, timezone

from demo_flow_api import now, require, stable


def confirm_coverage(p, key, obligation):
    path = p.root + "/road-coverage"
    current = p.api.read(p.sup, path)
    scope = next(s for s in current["scopes"] if s["obligationId"] == obligation)
    sources = current["sources"]
    handover = next(s for s in sources if s["kind"] == "HANDOVER" and s["id"] == p.f["handoverDocumentId"])
    warranty = next(s for s in sources if s["kind"] == "WARRANTY" and
                    s["id"] == p.f["roadWarrantyId"])
    start = datetime.now(timezone.utc) - timedelta(minutes=5)
    # These are the DEMO project's synthetic handover documents.
    p.post(key + "-coverage", p.sup, path, {
        "scopeObligationId": obligation, "expectedScopeHash": scope["scopeHash"],
        "handoverDocumentId": handover["id"], "handoverVersion": handover["version"],
        "sourceKind": "WARRANTY", "sourceId": warranty["id"], "sourceVersion": warranty["version"],
        "applicableFromUtc": start.isoformat(), "applicableToUtc": (start + timedelta(days=1)).isoformat(),
        "provenance": "TEST_ONLY", "reason": "DEMO coverage for training records"}, path)


def seed_repair(p, key, defect, crew, mode, finalize, policy, await_approval=False):
    from demo_flow import REASON, sample
    api, pm = p.api, p.pm
    base = p.root + "/repair-packages"
    source = p.root + "/defects/" + defect
    scope = {"physicalRoadId": p.f["roadSectionId"], "routeVersionId": p.route, "segmentSetId": p.segset,
             "fromMeters": 1, "toMeters": 2, "offsetFromMeters": 0, "offsetToMeters": 1}
    package = p.post(key + "-package", pm, base, {
        "defectId": defect, "defectVersion": api.etag(pm, source).strip('"'), "obligations": [{
            "kind": "FORMAL_REPAIR", "mandatory": True, "scope": scope, "reason": REASON}], "reason": REASON}, source)
    pkgpath = base + "/" + package["id"]
    obligation = package["unresolvedMandatoryObligationIds"][0]
    item = p.cmd(key + "-propose", pm, pkgpath + "/items", {"obligationId": obligation, "mode": mode,
                 "repairPlan": "DEMO surface patching exercise", "checklistVersion": "DEMO-depth-v1",
                 "reason": REASON}, '"' + package["version"] + '"')
    itempath = pkgpath + "/items/" + item["id"]
    if await_approval:
        require(mode == "NORMAL", "Supervisor pre-approval applies to Normal repair")
        pending = api.read(p.sup, itempath)
        require(pending.get("currentBinding") is None, "Pending repair already has a Crew")
        return {"packageId": package["id"], "itemId": item["id"], "state": pending["state"]}
    if mode == "NORMAL":
        p.post(key + "-approve", p.sup, itempath + "/approve", {"reason": REASON}, itempath)
        stale = api.denial(p.sup, itempath + "/approve", (409,), method="POST", body={"reason": REASON},
                           headers={"Idempotency-Key": stable(p.key + key + "-stale"), "If-Match": '"' + item["version"] + '"'})
    else:
        stale = None
        p.cmd(key + "-recipient", pm, itempath + "/notification-supervisor",
              {"supervisorId": api.ids[p.sup]}, method="PUT", extra={"If-None-Match": "*"})
    p.post(key + "-assign", pm, itempath + "/assign", {
        "task": p.task_input(defect, crew, "POST_REPAIR"), "reason": REASON,
        "policyRevisionId": policy if mode == "FAST_TRACK" else None}, itempath)
    bound = api.read(pm, itempath)["currentBinding"]
    task = bound["taskId"]
    field = p.root + "/field-inspection-tasks/" + task
    execpath = itempath + "/tasks/" + task
    safety_path = itempath + "/temporary-safety-obligations"
    safety = None
    if mode == "NORMAL":
        safety = p.post(key + "-safety", pm, safety_path, {"scope": scope, "mandatory": False,
                    "reason": "DEMO temporary barrier during works"}, safety_path)
    started = p.start_task(key, field, crew)
    before = p.capture(key, crew, task, "BEFORE", "before")
    assessment = p.post(key + "-assessment", crew, execpath + "/assessment", {
        "originId": stable(p.key + key + "-assessment"), "fieldFirstStartId": started,
        "measurements": [sample(2)], "evidence": [before], "locationProof": p.location()}, itempath, pm)
    row = {"packageId": package["id"], "itemId": item["id"], "mode": mode, "taskId": task,
           "crew": crew, "assignmentId": bound["assignmentId"], "policyRevisionId": bound.get("policyRevisionId"),
           "safetyObligationId": safety["items"][0]["safetyObligationId"] if safety else None, "assessmentId": assessment["id"],
           "beforeFileId": before["fileId"], "staleApprovalHttp": stale, "wrongPmStatus": api.denial(p.other_pm, itempath)}
    if mode == "FAST_TRACK":
        confirm_coverage(p, key, obligation)
        eligibility = api.read(pm, itempath + "/eligibility")
        row["eligibility"] = {k: eligibility[k] for k in ("eligible", "missingReasons", "policyState", "reason", "sourceMapping", "unknownSources")
                              if k in eligibility}
    start_body = {"originId": stable(p.key + key + "-execution"), "fieldFirstStartId": started,
                  "claimedAt": now(), "assessmentId": assessment["id"]}
    if mode == "FAST_TRACK" and eligibility.get("missingReasons"):
        # Exercise the real execution guard, preserving the BE's genuine authority result.
        try:
            p.post(key + "-execution", crew, execpath + "/execution-start", start_body, itempath, pm)
        except RuntimeError as error:
            if getattr(error, "status", None) != 409:
                raise
            row.update(status="NOT_RUN", state=api.read(pm, itempath)["state"], reason=str(error))
            return row
        raise ValueError("FastTrack authority changed during execution")
    execution = p.post(key + "-execution", crew, execpath + "/execution-start", start_body, itempath, pm)
    finish = p.post(key + "-finish", crew, execpath + "/execution-finish", {
        "originId": stable(p.key + key + "-finish"), "executionStartId": execution["id"], "claimedAt": now()}, itempath, pm)
    after = p.capture(key, crew, task, "AFTER", "after")
    submission = p.post(key + "-claim", crew, field + "/submissions", {
        "originId": stable(p.key + key + "-claim"), "startOriginId": started, "captureType": "REPAIR_CLAIM",
        "repaired": True, "measurements": [sample(0)], "evidence": [after], "locationProof": p.location()}, field)
    attempt = p.post(key + "-attempt", crew, execpath + "/attempts", {
        "fieldSubmissionId": submission["id"], "expectedContentHash": submission["contentHash"],
        "executionFinishId": finish["id"]}, itempath, pm)
    review = p.post(key + "-review", pm, execpath + "/attempts/reviews", {
        "fieldSubmissionId": submission["id"], "decision": "ACCEPT", "reason": REASON}, itempath)
    if finalize:
        p.post(key + "-final", p.sup if mode == "NORMAL" else pm,
               itempath + "/final-confirmation", {"reason": REASON}, itempath)
    current = api.read(pm, itempath)
    crew_view = api.read(crew, field)["repairContext"]
    require(crew_view["attempt"]["attemptId"] == attempt["attemptId"]
            and crew_view["attempt"]["review"]["decision"] == "ACCEPT", "Crew repair lineage mismatch")
    require(bool(crew_view.get("finalDecision")) == finalize, "Repair final decision readback mismatch")
    row.update(status="PASS", state=current["state"], afterFileId=after["fileId"], attemptId=attempt["attemptId"],
               submissionId=submission["id"], reviewId=review["reviewId"],
               finalDecisionId=current.get("effectiveDecisionId"),
               finalActor=(p.sup if mode == "NORMAL" else pm) if finalize else None)
    return row
