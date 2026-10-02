"""Build RF-04 draft operation crosswalk from checked-in source artifacts.

This script reads repository files and writes only planning/refactor outputs.
It deliberately reports static source matches, never runtime verification.
"""

from __future__ import annotations

import json
import re
import hashlib
import subprocess
from collections import Counter
from pathlib import Path

import yaml
from rf04_ownership import (CURRENT_CONTROLLER_TASK, OPERATION_CONTEXT_REASONS,
                           OPERATION_TASK_OVERRIDES, SOURCE_TASK, TASK_OWNER,
                           TASK_REQUIREMENTS)


ROOT = Path(__file__).resolve().parents[3]
PLAN = ROOT / "planning/refactor"
OPENAPI = "docs/diagram/V2/05_Technical/openapi.yaml"
POSTMAN = "docs/postman/RoadGuardSystem-V2.postman_collection.json"
INVENTORY = "planning/refactor/01-endpoint-inventory.md"
HTTP = "RoadGuardSystem.API/RoadGuardSystem.API.http"
METHODS = {"get", "post", "put", "patch", "delete", "options", "head"}
SEAMS = {
    "IA": ("IAuthService/AuthService", "IIdentityRepository/IdentityRepository"),
    "IO": ("IIdentityOnboardingService/IdentityOnboardingService", "IIdentityOnboardingRepository/IdentityOnboardingRepository"),
    "IP": ("IIdentityService/IdentityService", "IIdentityRepository/IdentityRepository"),
    "IV": ("IIdentityV2Service/IdentityV2Service", "IIdentityRepository + IIdentityV2Repository/IdentityRepository"),
    "PC": ("IProjectCreationService", "matching project repository"),
    "PU": ("IProjectUpdateService", "matching project repository"),
    "PMG": ("IPrimaryProjectManagerService", "matching project repository"),
    "PR": ("IRoadSectionVersionService", "matching road repository"),
    "PW": ("IWarrantyCreationService", "matching warranty repository"),
    "PWP": ("IProjectWorkPackageService", "matching work-package read repository"),
    "IQ": ("IInspectionTaskQueryService", "IInspectionTaskReadRepository"),
    "SP": ("ISurveyPlanningService", "ISurveyPlanningRepository/SurveyPlanningPersistenceService"),
    "SV": ("ISurveyV2Service", "ISurveyV2Repository/SurveyV2PersistenceService"),
    "UP": ("IUploadService", "IUploadRepository/UploadPersistenceService + IUploadObjectStorage"),
    "PV": ("IProcessingV2Service", "IProcessingV2Repository/ProcessingV2PersistenceService"),
    "NT": ("INotificationService", "INotificationRepository/NotificationPersistenceService"),
}
TAG_TASK = {
    "auth": ("RF-10-01", "A", "R01-R03"), "admin": ("RF-10-01", "A", "R01-R03"),
    "project": ("RF-10-02", "A", "R04"), "route": ("RF-10-02", "A", "R05"),
    "survey": ("RF-10-03", "B", "R06-R07"), "file": ("RF-10-04", "B", "R13"),
    "ai": ("RF-10-05", "B", "R14"), "report": ("RF-10-06", "A", "R08"),
    "case": ("RF-10-06", "A", "R08"), "defect": ("RF-10-06", "A", "R09"),
    "inspection": ("RF-10-07", "A", "R10-R12"), "repair": ("RF-10-07", "A", "R10-R12"),
    "policy": ("RF-10-07", "A", "R10-R12"), "plan": ("RF-10-07", "A", "R10-R12"),
    "emergency": ("RF-10-07", "A", "R12"), "sync": ("RF-10-08", "B", "R15-R16"),
    "notification": ("RF-10-09-A", "A", "R17"), "analytics": ("RF-10-09-B", "B", "R18"),
    "export": ("RF-10-09-B", "B", "R18"), "retention": ("RF-10-09-A", "A", "R18"),
    "catalog": ("RF-10-02", "A", "R04-R05"),
}
SPECIAL = {
    "submitDataset": ("CG06,CG10", "38"),
    "getDatasetCoverage": ("CG06", "Q-RF02-05"),
    "syncOperations": ("CG12", "42A,43A"),
}
TAG_GAPS = {"auth": "CG01-CG03", "admin": "CG02-CG03", "project": "CG05,CG16", "route": "CG05",
            "survey": "CG04,CG06", "file": "CG10,CG17", "ai": "CG11", "report": "CG07",
            "case": "CG07", "defect": "CG08", "inspection": "CG09", "repair": "CG09",
            "policy": "CG09", "plan": "CG09", "emergency": "CG09", "sync": "CG12",
            "notification": "CG13", "analytics": "CG14", "export": "CG14", "retention": "CG14",
            "catalog": "CG05"}
TAG_DECISIONS = {"auth": "36A,37", "route": "39A", "file": "38", "ai": "44",
                 "defect": "33A,34A", "emergency": "35A", "sync": "42A,43A",
                 "analytics": "40", "retention": "41A"}


def raw_path(path: str) -> str:
    path = path.split("?", 1)[0]
    path = re.sub(r"^\{\{[^}]+\}\}(?=/)", "", path)
    path = re.sub(r"\{\{([^}]+)\}\}", r"{\1}", path)
    path = re.sub(r"^https?://[^/]+", "", path)
    return "/" + path.strip("/")


def version(path: str) -> str:
    match = re.match(r"^/api/v(\{version(?::[^}]+)?\}|\d+)(?=/|$)", raw_path(path))
    return match.group(1) if match else "UNSPECIFIED"


def norm(path: str) -> str:
    path = raw_path(path)
    path = re.sub(r"^/api/v(?:\{version(?::[^}]+)?\}|\d+)(?=/|$)", "", path)
    path = re.sub(r"\{([^}:]+)(?::[^}]+)?\}", r"{\1}", path)
    return "/" + path.strip("/")


def shape(path: str) -> str:
    return re.sub(r"\{[^}]+\}", "{}", norm(path)).lower()


def compatible_versions(a, b, supported=()):
    left, right = version(a), version(b)
    if supported and right.isdigit() and right not in supported:
        return False
    return left == right or "UNSPECIFIED" in (left, right) or left.startswith("{version") or right.startswith("{version")


def git(*args):
    return subprocess.check_output(["git", *args], cwd=ROOT, text=True).strip()


def fingerprint(paths):
    return {p: hashlib.sha256((ROOT / p).read_bytes()).hexdigest() for p in sorted(set(paths))}


def postman_items(items, folder=""):
    for item in items:
        name = "/".join(filter(None, (folder, item.get("name", ""))))
        if "item" in item:
            yield from postman_items(item["item"], name)
        elif "request" in item:
            req = item["request"]
            url = req.get("url", {})
            raw = url.get("raw", "") if isinstance(url, dict) else url
            if isinstance(url, dict) and url.get("path"):
                raw = "/" + "/".join(str(v) for v in url["path"])
            yield {"name": name, "method": req.get("method", ""), "route": raw_path(raw)}


def parse_inventory():
    rows = []
    for line in (ROOT / INVENTORY).read_text(encoding="utf-8").splitlines():
        match = re.match(r"^\| (\d+) \| ([A-Z]+) `([^`]+)` \| `([^`]+)` \| ([^|]+) \| ([^|]+) \| ([^|]+) \| ([^|]+) \|", line)
        if not match:
            continue
        number, method, route, action, auth, dto, seam, test = match.groups()
        controller = action.split(".")[0]
        task = CURRENT_CONTROLLER_TASK[controller]
        request, response = (s.strip() for s in dto.split("->", 1))
        service, repository = SEAMS[seam.strip()]
        rows.append({"id": f"C{int(number):03}", "method": method, "route": route,
                     "controller_action": action, "controller_source": f"RoadGuardSystem.API/Controllers/{action.split('.')[0]}.cs",
                     "auth": auth.strip(), "request": request,
                     "response": response, "service": service, "repository": repository,
                     "test_source": test.strip().replace(" / PM", "").replace(" / -", ""),
                     "module": task, "requirements": TASK_REQUIREMENTS[task], "rf_task": task,
                     "proposed_owner": TASK_OWNER[task], "seam": seam.strip(),
                     "ownership_reason": f"{controller} use case; {TASK_REQUIREMENTS[task]}"})
    return rows


def parse_code_routes():
    result = []
    for file in sorted((ROOT / "RoadGuardSystem.API/Controllers").glob("*Controller.cs")):
        content = file.read_text(encoding="utf-8").splitlines()
        bases = [m.group(1) for s in content for m in re.finditer(r'\[Route\("([^"]+)"\)\]', s)]
        if len(bases) != 1:
            raise ValueError(f"Unsupported controller route declaration: {file}: {bases}")
        base = bases[0]
        api_versions = [m.group(1).split(".")[0] for s in content for m in re.finditer(r'\[ApiVersion\("([^"]+)"\)\]', s)]
        if version(base).startswith("{version") and not api_versions:
            raise ValueError(f"No ApiVersion declaration for versioned route: {file}")
        for index, line in enumerate(content):
            http = re.search(r'\[Http(Get|Post|Put|Patch|Delete)(?:\("([^"]*)"\))?\]', line)
            if not http:
                if re.search(r"\[Http\w+", line):
                    raise ValueError(f"Unsupported HTTP attribute: {file}:{index+1}: {line.strip()}")
                continue
            signature = next((s for s in content[index + 1:] if re.search(r"public\s+(?:async\s+)?(?:Task|IActionResult)", s)), "")
            action = re.search(r"\b(\w+)\s*\(", signature)
            assert action, (file, index + 1)
            route = raw_path(base.rstrip("/") + "/" + (http.group(2) or "").lstrip("/"))
            result.append({"method": http.group(1).upper(), "raw_route": route,
                           "route_prefix": raw_path(base), "version": version(route),
                           "supported_versions": api_versions,
                           "normalized_route": norm(route),
                           "controller_action": file.stem + "." + action.group(1),
                           "source": file.relative_to(ROOT).as_posix(), "line": index + 1})
    return result


def schema_refs(content):
    refs = []
    if not isinstance(content, dict):
        return refs
    for value in content.values():
        if isinstance(value, dict):
            schema = value.get("schema", {})
            if isinstance(schema, dict):
                refs.append(schema.get("$ref", "INLINE_SCHEMA"))
    return refs


def main():
    inventory = parse_inventory()
    code = parse_code_routes()
    code_keys = Counter((r["method"], r["normalized_route"], r["controller_action"]) for r in code)
    inventory_keys = Counter((r["method"], norm(r["route"]), r["controller_action"]) for r in inventory)
    stale_inventory = list((inventory_keys - code_keys).elements())
    if stale_inventory:
        raise ValueError(f"RF-01 inventory routes absent from current source: {stale_inventory}")
    added_code_routes = list((code_keys - inventory_keys).elements())
    for method, path, action in added_code_routes:
        base = next((r for r in inventory if r["controller_action"] == action), None)
        if base is None:
            task = CURRENT_CONTROLLER_TASK[action.split(".")[0]]
            base = {"method": method, "controller_action": action,
                    "controller_source": f"RoadGuardSystem.API/Controllers/{action.split('.')[0]}.cs",
                    "auth": "SOURCE_REVIEW_REQUIRED", "request": "SOURCE_REVIEW_REQUIRED",
                    "response": "SOURCE_REVIEW_REQUIRED", "service": "SOURCE_REVIEW_REQUIRED",
                    "repository": "SOURCE_REVIEW_REQUIRED", "test_source": "NOT_IN_RF01",
                    "module": task, "requirements": TASK_REQUIREMENTS[task],
                    "rf_task": task, "proposed_owner": TASK_OWNER[task],
                    "ownership_reason": f"new source action {action}"}
        inventory.append({**base, "id": f"C{len(inventory)+1:03}", "method": method,
                          "route": path, "source_inventory_status": "NEW_SOURCE_ROUTE_NOT_IN_RF01"})
    for row in inventory:
        code_routes = [r for r in code if (r["method"], r["normalized_route"], r["controller_action"]) ==
                       (row["method"], norm(row["route"]), row["controller_action"])]
        if len(code_routes) != 1:
            raise ValueError(f"Unresolved code route for {row['id']}: {code_routes}")
        row["code_raw_route"] = code_routes[0]["raw_route"]
        row["code_supported_versions"] = code_routes[0]["supported_versions"]
    api = yaml.safe_load((ROOT / OPENAPI).read_text(encoding="utf-8"))
    requests = list(postman_items(json.loads((ROOT / POSTMAN).read_text(encoding="utf-8"))["item"]))
    http_text = (ROOT / HTTP).read_text(encoding="utf-8")
    operations = []
    for path, methods in api["paths"].items():
        for method, spec in methods.items():
            if method.lower() not in METHODS:
                continue
            operations.append((method.upper(), raw_path(path), spec))
    ids = [spec.get("operationId") for _, _, spec in operations]
    assert len(ids) == len(set(ids)) and all(ids), "Draft operation IDs absent or duplicated"
    assert len({(m, p) for m, p, _ in operations}) == len(operations), "Duplicate draft route"

    used_current = set()
    target_rows = []
    for method, route, spec in operations:
        exact = [r for r in inventory if r["method"] == method and raw_path(r["code_raw_route"]) == route]
        normalized = [r for r in inventory if r["method"] == method and norm(r["code_raw_route"]) == norm(route)
                      and compatible_versions(r["code_raw_route"], route, r["code_supported_versions"])]
        structural = [r for r in inventory if r["method"] == method and shape(r["code_raw_route"]) == shape(route)
                      and compatible_versions(r["code_raw_route"], route, r["code_supported_versions"])]
        matches = exact or normalized or structural
        for r in matches:
            used_current.add(r["id"])
        tag = (spec.get("tags") or ["unknown"])[0]
        if tag not in TAG_TASK:
            raise ValueError(f"Unmapped draft tag {tag!r}: {spec['operationId']}")
        task, owner, requirement = TAG_TASK[tag]
        fr = spec.get("x-fr") or []
        if isinstance(fr, str):
            fr = [fr]
        operation_id = spec["operationId"]
        if operation_id in OPERATION_TASK_OVERRIDES:
            task = OPERATION_TASK_OVERRIDES[operation_id]
            owner = TASK_OWNER[task]
            requirement = TASK_REQUIREMENTS[task]
        requirement_links = []
        for identifier in fr:
            source_task = SOURCE_TASK.get(identifier)
            if source_task is None:
                raise ValueError(f"Draft operation {operation_id} has unmapped x-fr {identifier}")
            relation = "PRIMARY" if source_task == task else "CONTEXT"
            reason = "Operation and requirement share primary task" if relation == "PRIMARY" else OPERATION_CONTEXT_REASONS.get(operation_id)
            if not reason:
                raise ValueError(f"Explain cross-module requirement link {operation_id} -> {identifier}")
            requirement_links.append({"id": identifier, "source_primary_task": source_task,
                                      "operation_primary_task": task, "role": relation, "reason": reason})
        gap, decision = SPECIAL.get(operation_id, (TAG_GAPS.get(tag, "UNKNOWN"), TAG_DECISIONS.get(tag, "UNKNOWN")))
        request_schema = schema_refs((spec.get("requestBody") or {}).get("content", {}))
        response_schemas = {str(status): schema_refs(value.get("content", {})) if isinstance(value, dict) else []
                            for status, value in (spec.get("responses") or {}).items()}
        postman = [r["name"] for r in requests if r["method"] == method and shape(r["route"]) == shape(route)
                   and compatible_versions(r["route"], route)]
        http_match = any(shape(route) == shape(m.group(2)) and method == m.group(1).upper()
                         for m in re.finditer(r"(?m)^\s*(GET|POST|PUT|PATCH|DELETE)\s+(\S+)", http_text))
        impl = "SOURCE_MATCH_UNVERIFIED" if len(matches) == 1 else ("AMBIGUOUS_SOURCE_MATCH" if matches else "NO_SOURCE_ACTION_FOUND")
        kind = ("ambiguous" if len(matches) > 1 else "exact_raw" if exact else
                "normalized" if normalized else "parameter_alias" if structural else "none")
        transition = None
        if matches and any(r["rf_task"] != task for r in matches):
            transition = {"from_tasks": sorted({r["rf_task"] for r in matches}), "to_task": task,
                          "owner": owner, "reason": "Draft target ownership differs from current implementation; compare business responsibility before migration"}
        target_rows.append({"id": f"D{len(target_rows)+1:03}", "method": method, "route": norm(route),
                            "raw_route": route, "route_prefix": "", "version": version(route),
                            "normalized_route": norm(route),
                            "normalization_rule": "strip api/v version prefix; strip parameter constraints; retain version separately",
                            "operation_id": operation_id, "contract_source": OPENAPI,
                            "contract_status": "PROPOSED_DRAFT", "implementation_status": impl,
                            "current_matches": [r["id"] for r in matches],
                            "controller_action": [r["controller_action"] for r in matches],
                            "controller_source": [r["controller_source"] for r in matches],
                            "request_dto": [r["request"] for r in matches] if matches else request_schema or ["NO_REQUEST_BODY_IN_DRAFT"],
                            "response_dto": [r["response"] for r in matches] if matches else [ref for refs in response_schemas.values() for ref in refs] or ["NO_RESPONSE_SCHEMA_IN_DRAFT"],
                            "draft_request_schemas": request_schema,
                            "draft_response_schemas_by_status": response_schemas,
                            "service": [r["service"] for r in matches] if matches else ["NOT_IMPLEMENTED_IN_CHECKED_SOURCE"],
                            "repository": [r["repository"] for r in matches] if matches else ["NOT_IMPLEMENTED_IN_CHECKED_SOURCE"],
                            "source_requirements": fr or [requirement], "requirement_links": requirement_links,
                            "module_requirements": requirement,
                            "approval_status": "OPERATION_NOT_APPROVED; related requirement direction only" if decision != "UNKNOWN" else "UNKNOWN_OPERATION_AUTHORITY",
                            "decision_source": decision, "test_source": [r["test_source"] for r in matches] if matches else [],
                            "target_roles": spec.get("x-roles", []), "target_permission_policy": spec.get("x-permission-policy", "UNKNOWN"),
                            "postman_examples": postman, "http_example_found": http_match,
                            "repo_consumers": ["Postman"] * bool(postman) + (["API.http"] if http_match else []),
                            "web_consumer": "UNKNOWN_OUTSIDE_REPO", "android_consumer": "UNKNOWN_OUTSIDE_REPO",
                            "ai_consumer": "UNKNOWN_OUTSIDE_REPO", "external_consumer": "UNKNOWN",
                            "gap": gap if matches else "NO_SOURCE_ACTION_FOUND; " + gap,
                            "disposition": "COMPARE_WIRE_AND_KEEP_CURRENT" if matches else "DRAFT_TARGET_ONLY",
                            "rf_task": task, "proposed_owner": owner,
                            "ownership_reason": (f"operationId override {operation_id}; {requirement}" if operation_id in OPERATION_TASK_OVERRIDES
                                                 else f"draft tag {tag}; {requirement}"),
                            "transition_owner": transition,
                            "match_kind": kind,
                            "match_limit": "method/path candidate only; DTO/auth/header/status/behavior not equivalent"})

    current_rows = []
    for row in inventory:
        code_routes = [r for r in code if r["controller_action"] == row["controller_action"]
                       and r["method"] == row["method"] and r["normalized_route"] == norm(row["route"])]
        if len(code_routes) != 1:
            raise ValueError(f"Inventory row cannot identify one code route: {row['id']}: {code_routes}")
        route_meta = code_routes[0]
        drafts = [d for d in target_rows if row["id"] in d["current_matches"]]
        postman = [r["name"] for r in requests if r["method"] == row["method"] and shape(r["route"]) == shape(row["route"])]
        current_rows.append({**row, "raw_route": route_meta["raw_route"],
                             "route_prefix": route_meta["route_prefix"], "version": route_meta["version"],
                             "supported_versions": route_meta["supported_versions"],
                             "normalized_route": route_meta["normalized_route"],
                             "normalization_rule": "strip api/v version prefix; strip parameter constraints; retain version separately",
                             "source_line": route_meta["line"],
                             "contract_source": "controller source + DTO + API.http/Postman (source-level)",
                             "contract_status": "CURRENT_SOURCE_OBSERVED; runtime wire not verified",
                             "implementation_status": "SOURCE_ACTION_PRESENT_UNVERIFIED",
                             "approval_status": "UNKNOWN except cited Accepted 32-44 requirements",
                             "draft_operation_ids": [d["operation_id"] for d in drafts],
                             "postman_examples": postman, "http_example_found": any(shape(row["route"]) == shape(m.group(2)) and row["method"] == m.group(1).upper()
                                                                                 for m in re.finditer(r"(?m)^\s*(GET|POST|PUT|PATCH|DELETE)\s+(\S+)", http_text)),
                             "web_consumer": "UNKNOWN_OUTSIDE_REPO", "android_consumer": "UNKNOWN_OUTSIDE_REPO",
                             "ai_consumer": "UNKNOWN_OUTSIDE_REPO", "external_consumer": "UNKNOWN",
                             "disposition": "COMPARE_WIRE_AND_KEEP_CURRENT" if drafts else "CURRENT_ONLY_PRESERVE_PENDING_CONSUMER_CHECK",
                             "gap": "see RF-02 module gap; external consumer unverified"})
    program = (ROOT / "RoadGuardSystem.API/Program.cs").read_text(encoding="utf-8")
    health = re.findall(r'(?m)app\.MapHealthChecks\("([^"]+)"\)', program)
    if not health:
        raise ValueError("No MapHealthChecks route found in Program.cs")
    for health_path in health:
        current_rows.append({"id": f"C{len(current_rows)+1:03}", "method": "GET", "route": raw_path(health_path),
                         "raw_route": raw_path(health_path), "route_prefix": "", "version": "UNSPECIFIED",
                         "normalized_route": norm(health_path), "normalization_rule": "none",
                         "controller_action": "Program.MapHealthChecks",
                         "controller_source": "RoadGuardSystem.API/Program.cs",
                         "auth": "Anon", "request": "none", "response": "health response", "service": "N/A", "repository": "N/A",
                         "test_source": "RF-01 static preflight", "module": "platform", "requirements": "R18", "rf_task": "RF-06",
                         "proposed_owner": "A", "contract_source": "RoadGuardSystem.API/Program.cs", "contract_status": "CURRENT_SOURCE_OBSERVED",
                         "implementation_status": "SOURCE_ACTION_PRESENT_UNVERIFIED", "approval_status": "UNKNOWN",
                         "draft_operation_ids": [], "postman_examples": [r["name"] for r in requests if r["method"] == "GET" and r["route"] == "/health"],
                         "http_example_found": False, "web_consumer": "UNKNOWN_OUTSIDE_REPO", "android_consumer": "UNKNOWN_OUTSIDE_REPO",
                         "ai_consumer": "UNKNOWN_OUTSIDE_REPO", "external_consumer": "UNKNOWN", "disposition": "CURRENT_ONLY_PRESERVE_PENDING_CONSUMER_CHECK",
                         "gap": "no draft path; operational consumer unverified"})

    for row in inventory:
        row.pop("code_raw_route", None)
        row.pop("code_supported_versions", None)
    inputs = [INVENTORY, OPENAPI, POSTMAN, HTTP, "RoadGuardSystem.API/Program.cs",
              "planning/refactor/tools/rf04_ownership.py",
              "planning/refactor/tools/build_rf04_crosswalk.py"] + [r["source"] for r in code]
    payload = {"status": "DRAFT_RF04_NOT_ACTIVE", "branch": git("branch", "--show-current"), "head": git("rev-parse", "HEAD"),
               "dirty": bool(git("status", "--porcelain")), "input_sha256": fingerprint(inputs),
               "sources": {"current": INVENTORY, "draft": OPENAPI, "postman": POSTMAN, "http": HTTP},
               "counts": {"controllers": len({r["source"] for r in code}),
                          "actions": len({r["controller_action"] for r in code}),
                          "controller_routes": len(code), "current_routes_including_health": len(current_rows),
                          "draft_operations": len(target_rows), "unique_operation_ids": len(set(ids)),
                          "draft_source_matches": sum(bool(r["current_matches"]) for r in target_rows),
                          "current_draft_matches": len(used_current), "postman_requests": len(requests)},
               "historical_baseline": {"actions": 57, "current_routes_including_health": 58, "draft_operations": 133},
               "inventory_delta": {"added_source_routes": added_code_routes, "stale_inventory_routes": stale_inventory},
               "current": current_rows, "draft": target_rows}
    (PLAN / "04-operation-crosswalk.json").write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

    md = ["# RF-04: Operation crosswalk (draft)", "",
          "Generated from checked-in controllers, RF-01 inventory, parsed V2 OpenAPI, Postman and API.http by `tools/build_rf04_crosswalk.py`.",
          "`CURRENT_SOURCE_OBSERVED` is static source evidence; `PROPOSED_DRAFT` is not an active contract. JSON carries DTO, Service/Repository, test, Postman and consumer fields for every row.",
          "", f"- Source: `{payload['branch']}` at `{payload['head']}`; dirty={payload['dirty']}; SHA-256 fingerprints in JSON.",
          f"- Current: {payload['counts']['actions']} controller actions, {len(code)} route attributes, {len(health)} health route(s); {payload['counts']['controllers']} controllers.",
          f"- Draft: {len(target_rows)} parsed operations and {len(set(ids))} unique operation IDs.",
          f"- Delta from historical 57/58/133: actions {payload['counts']['actions']-57:+}, current routes {len(current_rows)-58:+}, draft operations {len(target_rows)-133:+}.",
          "- These are two sets of rows linked by route candidates, not a count of distinct implemented APIs. Matching method/path does not establish DTO, authorization, headers, status or behavior equivalence.",
          "- Current controller raw template, route prefix, declared API version, normalized path and normalization rule are in JSON. A raw exact match requires identical path text; prefix/constraint removal is `normalized`; parameter-name equivalence is `parameter_alias`; multiple candidates are `ambiguous`. Explicit different API major versions are incompatible with controller declarations.",
          "- `x-fr` is a historical trace hint. Each draft row has requirement links marked PRIMARY or CONTEXT with an explicit cross-module reason; the operation has one proposed task owner. No link proves requirement approval.",
          f"- Draft operations matching current source: {payload['counts']['draft_source_matches']}; current actions with draft match: {len(used_current)}.",
          f"- Postman requests parsed: {len(requests)}. Presence is a static example only; external web/Android/AI consumers are unknown.",
          "", "## Current source to draft", "",
          "| ID | Method and path | Controller/action | Draft operation | Source disposition | Test / Postman | Task/owner |", "|---|---|---|---|---|---|---|"]
    for r in current_rows:
        md.append(f"| {r['id']} | {r['method']} `{r['route']}` | `{r['controller_action']}` | {', '.join(r['draft_operation_ids']) or 'none'} | {r['disposition']} | {r['test_source']}; PM {len(r['postman_examples'])} | {r['rf_task']}/{r['proposed_owner']} |")
    md += ["", "## Draft contract to current source", "",
           "| ID | Method and path | operationId | Current action | Match / disposition | FR source | Task/owner |",
           "|---|---|---|---|---|---|---|"]
    for r in target_rows:
        fr_links = ", ".join(link["id"] + ("*" if link["role"] == "CONTEXT" else "") for link in r["requirement_links"])
        md.append(f"| {r['id']} | {r['method']} `{r['route']}` | `{r['operation_id']}` | {', '.join(r['controller_action']) or 'none'} | {r['match_kind']}; {r['disposition']} | {fr_links} | {r['rf_task']}/{r['proposed_owner']} |")
    md += ["", "## Reconciliation limits", "",
           "- `*` after an FR marks CONTEXT_ONLY: the FR's primary module differs from the operation owner. JSON records the named source task and reason. No current/draft primary owner transition was detected in this snapshot; the field is mandatory if one appears later.",
           "- A shape/parameter-alias match requires manual route parameter and wire review. A draft-only operation is a proposed target, not a missing accepted requirement by itself.",
           "- Test file labels come from RF-01 group inventory; they are not per-operation passing tests. RF-00 API tests failed before HTTP assertions.",
           "- The current inventory is verified against controller route attributes at generation time; `Program.MapHealthChecks` is separately counted. No deployed consumer or runtime response was checked.",
           "- Requirement approval is per decision in `02-decision-register.md`, not inferred from an OpenAPI `x-fr` or operation name. `UNKNOWN` remains explicit in JSON.", ""]
    (PLAN / "04-operation-crosswalk.md").write_text("\n".join(md), encoding="utf-8")
    print(json.dumps(payload["counts"], indent=2))
    print("draft only", sum(not r["current_matches"] for r in target_rows), "current only", sum(not r["draft_operation_ids"] for r in current_rows))


if __name__ == "__main__":
    main()
