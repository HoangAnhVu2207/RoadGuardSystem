"""Seed and verify two real MP4/SRT pairs on the dedicated local Docker handoff."""
import argparse
import re
import subprocess
from pathlib import Path

from real_flow_api import Api, FlowError, checked_asset, identifier, login, operation, read_json, require, save, upload, valid_etag
from real_flow_foundation import PROJECTS, foundation
from real_flow_verify import verify

CATALOG = {
    "ALLIGATOR_CRACK": ("Alligator crack", "Interconnected cracking with an alligator pattern."),
    "LONGITUDINAL_CRACK": ("Longitudinal crack", "Cracking along the road direction."),
    "OBLIQUE_CRACK": ("Oblique crack", "Cracking at an oblique angle to the road direction."),
    "POTHOLE": ("Pothole", "Localized loss of pavement material forming a hole."),
    "REPAIR_PATCH": ("Existing repair patch", "Previously repaired area, classified separately from new defects."),
    "TRANSVERSE_CRACK": ("Transverse crack", "Cracking across the road direction."),
}
DRONE = "5f1e9338-3f89-48f0-9d90-097b4ced4e62"


def catalog(api, token):
    route = "/catalog/defect-types/admin"
    rows, _ = api.request("defect catalog", "GET", route, token=token)
    current = {r["code"] for r in rows if r["isActive"]}
    for code, (name, description) in CATALOG.items():
        if code not in current:
            api.request(code, "POST", route, {"code": code, "name": name, "description": description,
                        "isActive": True, "reason": "Configure the six released FT95 detection classes."},
                        token=token, idem=operation("catalog", code), statuses=(200, 201))
    rows, _ = api.request("verify catalog", "GET", route, token=token)
    require(CATALOG.keys() <= {r["code"] for r in rows if r["isActive"]}, "Missing active FT95 class")


def submit(api, code, record, users, source, files, assets, checkpoint):
    project = record["foundation"]
    project_id = project["projectId"]
    scope = [{k: project[k] for k in ("routeVersionId", "segmentSetId", "segmentIds")}]
    scope[0]["targetBand"] = "SURFACE"
    suffix = PROJECTS[code][1]
    operator_id, operator = users["operator-" + suffix]
    task, _ = api.request(code + " create task", "POST", f"/projects/{project_id}/survey-tasks",
                          {"scope": scope, "surveyType": "BASELINE", "operatorId": operator_id},
                          token=users["pm-" + suffix][1], idem=operation(project_id, source["key"], "task"),
                          statuses=(200, 201))
    task_id = identifier(task["id"])
    path = "/survey-tasks/" + task_id
    task, headers = api.request(code + " operator task", "GET", path, token=operator)
    require(task["projectId"] == project_id and task["operatorId"] == operator_id, "Survey task scope mismatch")
    if task["status"] not in ("ACCEPTED", "IN_PROGRESS", "SUBMITTED", "COMPLETED"):
        api.request(code + " accept", "POST", path + "/accept", token=operator, etag=valid_etag(headers),
                    idem=operation(task_id, "accept"))
    devices, _ = api.request(code + " eligible drones", "GET", path + "/eligible-drones?limit=100", token=operator)
    require(any(r["id"] == DRONE for r in devices["items"]), "Expected local drone is ineligible")
    # The BE closes upload creation when a task is submitted. Resume from the pair
    # checkpoint saved before submission, then verify both IDs and hashes on the dataset.
    closed = task["status"] in ("SUBMITTED", "COMPLETED")
    if closed:
        require(record.get("taskId") == task_id and "pair" in record and "submissionEtag" in record,
                "Submitted survey requires its original receipt to replay the paired dataset")
    pair = {"timeOffsetMilliseconds": 0}
    for field, key, purpose, content in (("videoFile", "videoFileId", "SURVEY_VIDEO", "video/mp4"),
                                         ("telemetryFile", "telemetryFileId", "TELEMETRY", "application/x-subrip")):
        name = source[field]
        digest = files[name]["sha256"]
        local = checked_asset(assets, name, digest)
        pair[key] = identifier(record["pair"][key]) if closed else upload(
            api, operator, project_id, task_id, local, digest, purpose, content)
        record[field + "Sha256"] = digest
    task, headers = api.request(code + " ready task", "GET", path, token=operator)
    require(record.get("taskId", task_id) == task_id, "Survey identity changed")
    record.update(taskId=task_id, pair=pair)
    record.setdefault("submissionEtag", valid_etag(headers))
    checkpoint()
    payload = {"videoFileIds": [pair["videoFileId"]], "telemetryFileIds": [pair["telemetryFileId"]], "pairs": [pair],
               "scope": task["scope"], "deviceId": DRONE, "recordedAt": source["approximateRecordedAtUtc"]}
    for _ in range(2):
        data, _ = api.request(code + " dataset", "POST", path + "/datasets", payload, token=operator,
                              idem=operation(task_id, "dataset"), etag=record["submissionEtag"], statuses=(201,))
        dataset_id = identifier(data["id"])
        require(record.get("datasetId", dataset_id) == dataset_id, "Dataset identity changed on replay")
        record["datasetId"] = dataset_id
    detail, _ = api.request(code + " dataset readback", "GET", "/datasets/" + dataset_id, token=operator)
    require(detail["projectId"] == project_id and detail["surveyTaskId"] == task_id, "Dataset scope mismatch")
    sources = {r["fileId"]: r["checksumSha256"] for r in detail["sourceFiles"]}
    require(sources.get(pair["videoFileId"]) == record["videoFileSha256"] and
            sources.get(pair["telemetryFileId"]) == record["telemetryFileSha256"], "Dataset input checksum mismatch")
    record["datasetReplay"] = "PASS"
    checkpoint()
    print(code, "dataset submitted and replayed", flush=True)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--api", default="http://127.0.0.1:28081")
    parser.add_argument("--storage", default="http://127.0.0.1:29001")
    parser.add_argument("--credentials", type=Path, required=True)
    parser.add_argument("--assets", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--compose-project", default="rg-real-fe-check")
    parser.add_argument("--phase", choices=("all", "foundation", "submit", "verify"), default="all")
    args = parser.parse_args()
    require(args.output.resolve() != args.credentials.resolve(), "Receipt and credentials must have different paths")
    api = Api(args.api, args.storage)
    require(re.fullmatch(r"[a-z0-9][a-z0-9-]{0,62}", args.compose_project), "Invalid Compose project")
    port = subprocess.check_output(["docker", "--context", "desktop-linux", "port", args.compose_project + "-api-1", "8080/tcp"],
                                   text=True).strip().splitlines()
    require(f"127.0.0.1:{api.url.port}" in port, "API must belong to the selected local Compose project")
    manifest = read_json(Path(__file__).with_name("manifest.json"))
    for name, info in manifest["files"].items():
        require(checked_asset(args.assets, name, info["sha256"]).stat().st_size == info["bytes"], "Asset size mismatch")
    receipt = read_json(args.output) if args.output.exists() else {
        "api": args.api, "profile": manifest["profile"], "provenance": "REAL_AI", "projects": {}}
    require(receipt["api"] == args.api and receipt["profile"] == manifest["profile"], "Receipt belongs to another stack")
    checkpoint = lambda: save(args.output, receipt)
    receipt["status"] = "IN_PROGRESS"
    receipt.pop("error", None)
    checkpoint()
    try:
        users = login(api, read_json(args.credentials))
        catalog(api, users["supervisor-a"][1])
        for code, (_, _, source_key, _) in PROJECTS.items():
            record = receipt["projects"].setdefault(code, {})
            if args.phase in ("all", "foundation"):
                project, = [p for p in manifest["projects"] if p["key"] == source_key]
                facts = foundation(api, code, project, manifest["files"], args.assets, users)
                require(facts == record.get("foundation", facts), "Project foundation identity changed")
                record["foundation"] = facts
                checkpoint()
                print(code, "foundation verified", flush=True)
        if args.phase in ("all", "submit"):
            for code, (_, _, source_key, _) in PROJECTS.items():
                source, = [s for s in manifest["inputs"] if s["projectKey"] == source_key]
                submit(api, code, receipt["projects"][code], users, source, manifest["files"], args.assets, checkpoint)
        if args.phase in ("all", "verify"):
            for code in PROJECTS:
                verify(api, users, code, receipt["projects"][code], args.compose_project, checkpoint)
            receipt["status"] = "PASS"
    except Exception as error:
        receipt["status"] = "FAIL"
        receipt["error"] = str(error) if isinstance(error, FlowError) else type(error).__name__
        raise
    finally:
        try:
            api.logout()
        finally:
            receipt["httpStatusCounts"] = api.status_counts
            checkpoint()


if __name__ == "__main__":
    try:
        main()
    except (FlowError, OSError, KeyError, ValueError, TypeError) as exc:
        raise SystemExit(str(exc) if isinstance(exc, FlowError) else "Local flow failed: " + type(exc).__name__) from None
