"""Resolve current authority from sourced decisions while retaining historical origin."""

import hashlib
import json
import re

STATES = {"BLOCKED", "READY", "DEFERRED"}
GROUPS = ("fr", "br", "us", "pf", "nfr")
INDEX = "planning/refactor/04-decision-index.json"


def digest(value):
    return hashlib.sha256(value.encode("utf-8")).hexdigest()


def decision_index(root):
    data = json.loads((root / INDEX).read_text(encoding="utf-8"))
    if data.get("schema_version") != 1 or not isinstance(data.get("decisions"), dict):
        raise ValueError("Invalid decision index")
    return data["decisions"]


def decision_for(row, scope, root, index):
    ident = row["id"]
    decision_id = scope.get("decision_id")
    claim_ids = scope.get("claim_ids")
    if not decision_id or not scope.get("decision_source") or not scope.get("scope") or not scope.get("decision_claim") or not claim_ids:
        raise ValueError(f"{ident}: decision_id, decision_source, scope, decision_claim and claim_ids required")
    if not isinstance(claim_ids, list) or len(claim_ids) != len(set(claim_ids)):
        raise ValueError(f"{ident}: invalid claim_ids")
    known = {claim["id"]: claim["content"] for claim in row["claims"]}
    if not set(claim_ids) <= set(known):
        raise ValueError(f"{ident}: decision claim_ids unrelated to requirement")
    decision = index.get(decision_id)
    if not decision:
        raise ValueError(f"{ident}: decision unrelated to requirement or absent from index")
    if decision.get("status") != "Accepted":
        raise ValueError(f"{ident}: decision is not Accepted")
    source = decision.get("source")
    anchor = decision.get("anchor")
    if not source or not anchor or scope["decision_source"] != f"{source}#{anchor}":
        raise ValueError(f"{ident}: decision source/anchor mismatch")
    path = (root / source).resolve()
    if not path.is_relative_to(root.resolve()) or not path.is_file():
        raise ValueError(f"{ident}: missing decision source")
    body = path.read_text(encoding="utf-8")
    headings = [(m.start(), m.group(1)) for m in re.finditer(r"^## (.+)$", body, re.M)]
    section = None
    for number, (start, title) in enumerate(headings):
        slug = re.sub(r"[^\w -]", "", title.lower()).replace(" ", "-")
        if slug == anchor:
            section = body[start:headings[number + 1][0] if number + 1 < len(headings) else len(body)]
            break
    if section is None:
        raise ValueError(f"{ident}: decision anchor missing")
    status_line = decision.get("status_line")
    if (not status_line or status_line not in section.splitlines()
            or not status_line.startswith("Status: Accepted ")
            or not decision.get("confirmed_by") or not decision.get("confirmed_at")
            or decision["confirmed_by"] not in status_line or decision["confirmed_at"] not in status_line):
        raise ValueError(f"{ident}: decision source lacks matching Accepted status")
    lines = [line for line in section.splitlines() if line.startswith(f"| {decision_id} |")]
    if len(lines) != 1:
        raise ValueError(f"{ident}: decision row missing or duplicated")
    cells = [cell.strip() for cell in lines[0].strip("|").split("|")]
    if len(cells) < 3 or cells[1] != decision.get("content") or cells[1] != scope["decision_claim"]:
        raise ValueError(f"{ident}: decision claim does not match Accepted wording")
    approval = decision.get("requirements", {}).get(ident)
    if (not approval or approval.get("scope") != scope["scope"]
            or approval["scope"] not in cells[2] or ident not in cells[2]):
        raise ValueError(f"{ident}: decision scope unrelated to requirement")
    approved_claims = approval.get("claim_sha256", {})
    if set(claim_ids) != set(approved_claims):
        raise ValueError(f"{ident}: decision claim coverage mismatch")
    for claim_id in claim_ids:
        if approved_claims[claim_id] != digest(known[claim_id]):
            raise ValueError(f"{ident}: confirmed claim content changed: {claim_id}")
    return set(claim_ids)


def resolved_authority(row, root, index):
    readiness = row["readiness"]
    state = readiness["state"]
    if state not in STATES:
        raise ValueError(f"{row['id']}: invalid readiness state {state}")
    approved = readiness.get("approved_scopes", [])
    if not isinstance(approved, list):
        raise ValueError(f"{row['id']}: invalid approved_scopes")
    covered = set()
    for scope in approved:
        claims = decision_for(row, scope, root, index)
        if covered & claims:
            raise ValueError(f"{row['id']}: duplicate approved claim")
        covered |= claims
    all_claims = {claim["id"] for claim in row["claims"]}
    if state == "READY":
        evidence = readiness.get("required_evidence")
        if not approved or not isinstance(evidence, list) or not evidence or any(
                not isinstance(item, str) or not item.strip() for item in evidence):
            raise ValueError(f"{row['id']}: READY needs approved_scopes and planned required_evidence")
        if covered != all_claims:
            raise ValueError(f"{row['id']}: partial decision cannot make entire requirement READY")
        if readiness.get("reason") or readiness.get("checkpoint"):
            raise ValueError(f"{row['id']}: READY cannot retain a blocker")
    elif not readiness.get("reason") or not readiness.get("checkpoint"):
        raise ValueError(f"{row['id']}: {state} needs reason and checkpoint")
    authority = ("TARGET_CONFIRMED_BY_DECISION" if covered == all_claims and approved
                 else "PARTIALLY_CONFIRMED_BY_DECISION" if covered
                 else row["source_authority"])
    return authority, covered


def validate_readiness(row, root):
    if row.get("source_authority") not in {"HISTORICAL_SOURCE_ONLY", "MIXED_CONFIRMED_REFERENCE_AND_HISTORICAL_DETAIL"}:
        raise ValueError(f"{row['id']}: historical source authority missing")
    authority, covered = resolved_authority(row, root, decision_index(root))
    if row["authority"] != authority or row["implementation_readiness"] != row["readiness"]["state"]:
        raise ValueError(f"{row['id']}: generated authority/readiness mismatch")
    for claim in row["claims"]:
        expected = "TARGET_CONFIRMED_BY_DECISION" if claim["id"] in covered else "HISTORICAL_SOURCE_ONLY"
        if claim.get("authority") != "HISTORICAL_SOURCE_ONLY" or claim.get("current_authority") != expected:
            raise ValueError(f"{row['id']}: generated claim authority mismatch")
    return row


def apply_readiness(rows, root, registry_path, previous_path):
    registry = json.loads(registry_path.read_text(encoding="utf-8"))
    if registry.get("schema_version") != 1 or not isinstance(registry.get("decisions"), dict):
        raise ValueError("Invalid readiness registry")
    overrides = registry["decisions"]
    old = {}
    if previous_path.is_file():
        previous = json.loads(previous_path.read_text(encoding="utf-8"))
        old = {r["id"]: r for group in GROUPS for r in previous[group]}
    by_id = {r["id"]: r for r in rows}
    if set(overrides) - set(by_id):
        raise ValueError(f"Unknown readiness IDs: {sorted(set(overrides) - set(by_id))}")
    index = decision_index(root)
    for row in rows:
        ident = row["id"]
        prior = old.get(ident, {}).get("readiness")
        override = overrides.get(ident)
        if prior and (prior["state"] in {"READY", "DEFERRED"} or prior.get("approved_scopes")) and override is None:
            raise ValueError(f"{ident}: recorded readiness missing from registry; preserve or explicitly reverse it")
        row["source_authority"] = row["authority"]
        row["readiness"] = override if override is not None else {
            "state": "BLOCKED", "reason": row["missing_decision"], "checkpoint": row["blocked_checkpoint"]}
        row["authority"], covered = resolved_authority(row, root, index)
        for claim in row["claims"]:
            claim["current_authority"] = "TARGET_CONFIRMED_BY_DECISION" if claim["id"] in covered else "HISTORICAL_SOURCE_ONLY"
        row["implementation_readiness"] = row["readiness"]["state"]
        validate_readiness(row, root)
    return rows
