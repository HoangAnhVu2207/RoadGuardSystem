"""Read-only RF-04 semantic, source and structural contract checks."""

from __future__ import annotations

import hashlib
import html
import json
import os
from rf05_readiness import validate_readiness
import re
from pathlib import Path

import yaml

from build_rf04_crosswalk import compatible_versions, norm, raw_path, shape, version
from build_rf04_sources import ADR_TRANSITION, CONFIRMED_RELATED, SOURCES, sections
from rf04_ownership import (CG_TASK, DECISION_TASK, L_TASK, R_TASK,
                           CURRENT_CONTROLLER_TASK, OPERATION_CONTEXT_REASONS,
                           OPERATION_TASK_OVERRIDES, SOURCE_TASK, TASK_OWNER)

ROOT = Path(os.environ["ROADGUARD_ROOT"]).resolve() if os.environ.get("ROADGUARD_ROOT") else Path(__file__).resolve().parents[3]
PLAN = ROOT / "planning/refactor"
DOC_FILES = [
    *sorted((ROOT / "docs/product").glob("*.md")),
    *sorted((ROOT / "docs/backend").glob("*.md")),
    *sorted((ROOT / "docs/decisions").glob("*.md")),
    *sorted((ROOT / "contracts").rglob("*.md")),
    *sorted(PLAN.glob("04-*.md")),
    *sorted((PLAN / "coordination").glob("RF-04-*.md")),
    *sorted((PLAN / "reports").glob("RF-04*.md")),
    PLAN / "README.md",
]


def check_links(path):
    content = path.read_text(encoding="utf-8")
    missing = []
    for target in re.findall(r"(?<!!)\[[^]]+\]\(([^)]+)\)", content):
        path_part, _, anchor = target.partition("#")
        if target.startswith(("http://", "https://", "mailto:")):
            continue
        destination = (path.parent / path_part).resolve() if path_part else path
        if not destination.exists():
            missing.append(target)
        elif anchor and destination.suffix.lower() == ".md":
            headings = re.findall(r"^#{1,6}\s+(.+)$", destination.read_text(encoding="utf-8"), re.M)
            slugs = {re.sub(r"[^\w -]", "", h.lower()).replace(" ", "-") for h in headings}
            explicit_anchors = set(re.findall(r'<a id="([^"]+)"', destination.read_text(encoding="utf-8")))
            if anchor.lower() not in slugs | explicit_anchors:
                missing.append(target)
    return missing


def verify_fingerprint(data):
    assert data["input_sha256"], "No input fingerprint"
    for path, digest in data["input_sha256"].items():
        assert hashlib.sha256((ROOT / path).read_bytes()).hexdigest() == digest, f"Stale generated input: {path}"


def verify_operation(cross):
    assert cross["status"] == "DRAFT_RF04_NOT_ACTIVE"
    verify_fingerprint(cross)
    current, draft = cross["current"], cross["draft"]
    assert len({r["id"] for r in current}) == len(current)
    assert len({r["id"] for r in draft}) == len(draft)
    assert len({r["operation_id"] for r in draft}) == len(draft)
    assert len({(r["method"], r["raw_route"]) for r in draft}) == len(draft)
    assert len({(r["method"], r["raw_route"], r["controller_action"]) for r in current}) == len(current)
    assert cross["counts"]["actions"] == len({r["controller_action"] for r in current if "Controller." in r["controller_action"]})
    assert cross["counts"]["current_routes_including_health"] == len(current)
    assert cross["counts"]["draft_operations"] == len(draft)
    assert cross["counts"]["controllers"] == len({r["controller_source"] for r in current if "Controller." in r["controller_action"]})
    by_current = {r["id"]: r for r in current}
    by_draft = {r["operation_id"]: r for r in draft}
    for row in current:
        assert row["normalized_route"] == norm(row["raw_route"])
        assert row["version"] == version(row["raw_route"])
        assert row["route_prefix"] is not None and row["normalization_rule"]
        if "Controller." in row["controller_action"]:
            controller = row["controller_action"].split(".")[0]
            assert row["rf_task"] == CURRENT_CONTROLLER_TASK[controller], row["id"]
        for operation in row["draft_operation_ids"]:
            assert row["id"] in by_draft[operation]["current_matches"]
    for row in draft:
        assert row["contract_status"] == "PROPOSED_DRAFT"
        assert row["normalized_route"] == norm(row["raw_route"])
        assert row["version"] == version(row["raw_route"])
        assert row["rf_task"] in TASK_OWNER and row["proposed_owner"] == TASK_OWNER[row["rf_task"]]
        if row["operation_id"] in OPERATION_TASK_OVERRIDES:
            assert row["rf_task"] == OPERATION_TASK_OVERRIDES[row["operation_id"]]
        assert {link["id"] for link in row["requirement_links"]} == set(row["source_requirements"])
        for link in row["requirement_links"]:
            assert link["source_primary_task"] == SOURCE_TASK[link["id"]]
            assert link["operation_primary_task"] == row["rf_task"]
            expected_role = "PRIMARY" if SOURCE_TASK[link["id"]] == row["rf_task"] else "CONTEXT"
            assert link["role"] == expected_role
            if expected_role == "CONTEXT":
                assert link["reason"] == OPERATION_CONTEXT_REASONS[row["operation_id"]]
        assert row["match_limit"] and row["normalization_rule"]
        matches = [by_current[item] for item in row["current_matches"]]
        assert all(row["operation_id"] in item["draft_operation_ids"] for item in matches)
        if matches:
            assert all(item["method"] == row["method"] and compatible_versions(item["raw_route"], row["raw_route"], item.get("supported_versions", [])) for item in matches)
            if len(matches) > 1:
                assert row["match_kind"] == "ambiguous"
            elif raw_path(matches[0]["raw_route"]) == row["raw_route"]:
                assert row["match_kind"] == "exact_raw"
            elif norm(matches[0]["raw_route"]) == norm(row["raw_route"]):
                assert row["match_kind"] == "normalized"
            else:
                assert shape(matches[0]["raw_route"]) == shape(row["raw_route"])
                assert row["match_kind"] == "parameter_alias"
            changed = sorted({item["rf_task"] for item in matches if item["rf_task"] != row["rf_task"]})
            if changed:
                transition = row["transition_owner"]
                assert transition and transition["to_task"] == row["rf_task"] and transition["reason"]
                assert set(changed) <= set(transition["from_tasks"])
            else:
                assert row["transition_owner"] is None
        else:
            assert row["match_kind"] == "none"
    assert sum(bool(r["current_matches"]) for r in draft) == cross["counts"]["draft_source_matches"]
    assert sum(bool(r["draft_operation_ids"]) for r in current) == cross["counts"]["current_draft_matches"]
    return {k: cross["counts"][k] - cross["historical_baseline"][k]
            for k in ("actions", "current_routes_including_health", "draft_operations")}


def verify_source(source):
    assert source["status"] == "DRAFT_RF04_NOT_ACTIVE"
    verify_fingerprint(source)
    rows = [row for key in ("fr", "br", "us", "pf", "nfr") for row in source[key]]
    assert {r["id"] for r in rows} == set(SOURCE_TASK)
    assert len(rows) == len({r["id"] for r in rows})
    source_sections = {row["id"]: row for key, spec in SOURCES.items() for row in sections(*spec)}
    for row in rows:
        assert row["rf_task"] == SOURCE_TASK[row["id"]]
        assert row["proposed_owner"] == TASK_OWNER[row["rf_task"]]
        assert row["content_status"] == "MIGRATED_FULL_SECTION" and row["claims"]
        validate_readiness(row, ROOT)
        assert row["implementation_readiness"] == row["readiness"]["state"]
        registry = json.loads((PLAN / "04-readiness-decisions.json").read_text(encoding="utf-8"))["decisions"]
        if row["id"] in registry:
            assert row["readiness"] == registry[row["id"]]
        assert (ROOT / row["source"]).read_text(encoding="utf-8").splitlines()[row["line"]-1] == row["source_heading"]
        dest_path, anchor = row["destination"].split("#", 1)
        destination = (ROOT / dest_path).read_text(encoding="utf-8")
        assert f"### {row['id']} " in destination and anchor == row["id"].lower()
        for claim in row["claims"]:
            assert claim["content"].strip() and html.escape(claim["content"]) in destination, claim["id"]
        if row["id"] in source_sections:
            assert row["content"] == source_sections[row["id"]]["content"], row["id"]
        related = CONFIRMED_RELATED.get(row["id"])
        assert row.get("confirmed_reference") == related
        if related:
            assert row["source_authority"].startswith("MIXED_")
        else:
            assert row["source_authority"] == "HISTORICAL_SOURCE_ONLY"
    assert {r["id"] for r in source["adr"]} == set(ADR_TRANSITION)
    assert all(r["destination"] == ADR_TRANSITION[r["id"]] for r in source["adr"])
    assert {r["id"] for r in source["decisions"]} == set("32A 33A 34A 35A 36A 37 38 39A 40 41A 42A 43A 44".split())
    product = (ROOT / "docs/product/confirmed-decisions.md").read_text(encoding="utf-8")
    decision_source = (PLAN / "02-decision-register.md").read_text(encoding="utf-8")
    for row in source["decisions"]:
        assert row["authority"] == "TARGET_CONFIRMED_2026-09-30"
        assert row["rf_task"] == DECISION_TASK[row["id"]]
        line = next(line for line in decision_source.splitlines() if line.startswith(f"| {row['id']} |"))
        assert line in product, f"Decision content drift: {row['id']}"
    assert "RF-10-07" == next(r for r in source["br"] if r["id"] == "BR-46")["rf_task"]
    for group, mapping in (("R", R_TASK), ("CG", CG_TASK), ("L", L_TASK)):
        assert {r["id"] for r in source[group]} == set(mapping)
        for row in source[group]:
            assert row["rf_task"] == mapping[row["id"]]
            assert row["proposed_owner"] == TASK_OWNER[row["rf_task"]]
            assert row["id"] in (ROOT / row["source"]).read_text(encoding="utf-8")
    return {key: len(source[key]) for key in ("fr", "br", "us", "pf", "nfr", "adr", "decisions", "documents")}


def resolve_ref(spec, reference):
    assert reference.startswith("#/"), reference
    node = spec
    for part in reference[2:].split("/"):
        node = node[part.replace("~1", "/").replace("~0", "~")]
    return node


def verify_pilot(spec):
    assert spec["openapi"].startswith("3.1.") and spec["info"]["version"].endswith("draft")
    assert spec["x-contract-status"] == "PROPOSED"
    paths = spec["paths"]
    assert len(paths) == 1 and "/api/v1/projects/{projectId}/work-package" in paths
    op = paths["/api/v1/projects/{projectId}/work-package"]["get"]
    assert op["x-implementation-status"].endswith("NOT_HTTP_VERIFIED")
    assert set(op["responses"]) == {"200", "401", "403", "404"}
    assert op["security"][0]["currentBearerSource"] == []
    assert any(p["name"] == "projectId" and p["in"] == "path" and p["required"] for p in op["parameters"])
    assert any(p["name"] == "X-Correlation-ID" and p["in"] == "header" for p in op["parameters"])
    assert "X-Correlation-ID" in op["responses"]["200"]["headers"]
    def visit(node):
        if isinstance(node, dict):
            if "$ref" in node:
                assert len(node) == 1, "Reference siblings require deliberate semantics"
                resolve_ref(spec, node["$ref"])
            for value in node.values():
                visit(value)
        elif isinstance(node, list):
            for value in node:
                visit(value)
    visit(spec)
    schemas = spec["components"]["schemas"]
    expected = {
        "ProjectWorkPackageResponse": "ProjectWorkPackageResponseDto.cs",
        "RoadSectionWorkPackage": "RoadSectionWorkPackageDto.cs",
        "WarrantyWorkPackage": "WarrantyWorkPackageDto.cs",
    }
    for schema_name, file_name in expected.items():
        dto = (ROOT / "RoadGuardSystem.DTOs/Projects" / file_name).read_text(encoding="utf-8")
        body = dto.split("record ", 1)[1].split("(", 1)[1].split(");", 1)[0]
        fields = re.findall(r"\b(?:Guid|int|decimal|string|DateOnly|DateTimeOffset|IReadOnlyList<[^>]+>)(\?)?\s+(\w+)", body)
        expected_names = {name[0].lower() + name[1:] for _, name in fields}
        required = {name[0].lower() + name[1:] for nullable, name in fields if not nullable}
        schema = schemas[schema_name]
        assert set(schema["properties"]) == expected_names, (schema_name, expected_names - set(schema["properties"]))
        assert set(schema["required"]) == required, (schema_name, required - set(schema["required"]))
    root = schemas["ProjectWorkPackageResponse"]["properties"]
    assert root["roadSections"]["items"]["$ref"] == "#/components/schemas/RoadSectionWorkPackage"
    assert root["warranties"]["items"]["$ref"] == "#/components/schemas/WarrantyWorkPackage"
    assert schemas["RoadSectionWorkPackage"]["properties"]["effectiveFrom"]["format"] == "date-time"
    assert schemas["ProjectWorkPackageResponse"]["properties"]["rowVersion"]["format"] == "byte"
    role_source = (ROOT / "RoadGuardSystem.BusinessObjects/Common/Extensions/UserRoleCodeExtensions.cs").read_text(encoding="utf-8")
    roles = re.findall(r'public const string \w+DbCode = "([^"]+)"', role_source)
    assert set(root["accessRole"]["enum"]) == set(roles)
    enum_source = (ROOT / "RoadGuardSystem.BusinessObjects/Common/Enums.cs").read_text(encoding="utf-8")
    for enum_name, schema_name, property_name in [
        ("ProjectStatus", "ProjectWorkPackageResponse", "status"),
        ("WarrantyStatus", "WarrantyWorkPackage", "status")]:
        enum_body = re.search(rf"enum {enum_name} : byte\s*\{{([^}}]+)\}}", enum_source).group(1)
        values = re.findall(r"\b(\w+)\s*=\s*\d+", enum_body)
        assert set(schemas[schema_name]["properties"][property_name]["enum"]) == {v.upper() for v in values}
    scope_source = (ROOT / "RoadGuardSystem.BusinessObjects/Common/Extensions/WarrantyScopeExtensions.cs").read_text(encoding="utf-8")
    scope_values = re.findall(r'WarrantyScope\.\w+ => "([^"]+)"', scope_source)
    assert set(schemas["WarrantyWorkPackage"]["properties"]["scope"]["enum"]) == set(scope_values)
    return len(schemas)


def main():
    operation = json.loads((PLAN / "04-operation-crosswalk.json").read_text(encoding="utf-8"))
    source = json.loads((PLAN / "04-source-crosswalk.json").read_text(encoding="utf-8"))
    delta = verify_operation(operation)
    counts = verify_source(source)
    schema_count = verify_pilot(yaml.safe_load((ROOT / "contracts/http/work-package.proposed.yaml").read_text(encoding="utf-8")))
    missing = [(p.relative_to(ROOT).as_posix(), link) for p in DOC_FILES for link in check_links(p)]
    assert not missing, missing
    for path in DOC_FILES:
        if path.name.startswith("historical-"):
            continue  # Source excerpts preserve their original whitespace inside <pre>.
        assert not any(line.rstrip() != line for line in path.read_text(encoding="utf-8").splitlines()), path
    print(f"PASS static: {operation['counts']}; delta from 57/58/133={delta}; historical content={counts}; pilot schemas={schema_count}; Markdown={len(DOC_FILES)}")
    print("NOT RUN: full formal OpenAPI validator, HTTP/API/SQL, provider/Android consumers, FE lock")


if __name__ == "__main__":
    main()
