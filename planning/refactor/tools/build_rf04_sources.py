"""Transfer historical requirement content with explicit proposed owners.

Writes draft documentation and planning crosswalks only. Source text remains
historical evidence unless a separate owner decision confirms the claim.
"""

from __future__ import annotations

import hashlib
import html
import json
import os
from rf05_readiness import apply_readiness
import re
import subprocess
from pathlib import Path

from rf04_ownership import (CG_TASK, DECISION_TASK, L_TASK, R_TASK,
                           SOURCE_COORDINATION, SOURCE_TASK, TASK_OWNER, TASK_REQUIREMENTS)

ROOT = Path(os.environ["ROADGUARD_ROOT"]).resolve() if os.environ.get("ROADGUARD_ROOT") else Path(__file__).resolve().parents[3]
PLAN = ROOT / "planning/refactor"
PRODUCT = ROOT / "docs/product"
SOURCES = {
    "fr": ("docs/diagram/V2/02_Requirements/01_FRD_SRS.md", r"^### (FR-\d+)\s+[—-]\s+(.+)$", "historical-fr-br.md"),
    "br": ("docs/diagram/V2/02_Requirements/02_Business_Rules.md", r"^### (BR-\d+)\s+[—-]\s+(.+)$", "historical-fr-br.md"),
    "us": ("docs/diagram/V2/02_Requirements/05_User_Stories_Acceptance_Criteria.md", r"^### (US-\d+)\s+[—-]\s+(.+)$", "historical-us.md"),
    "pf": ("docs/diagram/V2/02_Requirements/03_To_Be_Process.md", r"^## \d+\. (PF-\d+)\s+[—-]\s+(.+)$", "historical-pf.md"),
}
DECISIONS = "32A 33A 34A 35A 36A 37 38 39A 40 41A 42A 43A 44".split()
CONFIRMED_RELATED = {
    "BR-10": "PR-32A", "FR-16": "PR-32A", "US-35": "PR-32A",
    "BR-30": "PR-33A", "FR-12": "PR-33A", "US-22": "PR-33A",
    "US-10": "PR-34A", "BR-46": "PR-35A", "FR-37": "PR-35A", "US-41": "PR-35A",
    "FR-01": "PR-36A/37", "US-01": "PR-36A/37", "US-27": "PR-37",
    "FR-27": "PR-38", "US-06": "PR-38", "FR-08": "PR-39A", "US-32": "PR-39A",
    "NFR-06": "PR-40", "NFR-08": "PR-40", "BR-45": "PR-41A", "US-19": "PR-41A",
    "US-02": "PR-42A/43A", "PF-08": "PR-42A/43A", "BR-39": "PR-44", "US-26": "PR-44",
}
ADR_TRANSITION = {
    "ADR-001": "docs/decisions/D-ARCH-proposed.md",
    "ADR-002": "docs/decisions/D-AUTH-proposed.md",
    "ADR-003": "docs/decisions/D-ASYNC-proposed.md",
    "ADR-004": "docs/decisions/D-ARCH-proposed.md",
    "ADR-005": "docs/decisions/D-WORKFLOW-proposed.md",
    "ADR-006": "docs/decisions/D-DELIVERY-proposed.md",
}


def git(*args):
    return subprocess.check_output(["git", *args], cwd=Path(os.environ.get("ROADGUARD_GIT_ROOT", ROOT)), text=True).strip()


def document_map():
    explicit = {
        "docs/RoadGuard_Project_Scope.md": "docs/product/README.md",
        "docs/RoadGuard_Backend_Scope.md": "docs/backend/README.md",
        "docs/postman/RoadGuardSystem-V2.postman_collection.json": "planning/refactor/04-operation-crosswalk.json",
        "RoadGuardSystem.API/RoadGuardSystem.API.http": "planning/refactor/04-operation-crosswalk.json",
        "AGENTS.md": "planning/refactor/03-agent-design.md",
        ".agents/": "planning/refactor/03-agent-design.md",
    }
    result = []
    for path in sorted((ROOT / "docs/diagram/V2").rglob("*")):
        if not path.is_file():
            continue
        rel = path.relative_to(ROOT).as_posix()
        if "/02_Requirements/" in rel or "/01_Overview/" in rel or "/04_UI_UX/" in rel:
            dest = "docs/product/README.md"
        elif "/03_Data/" in rel:
            dest = "docs/product/data-and-quality.md"
        elif "/AI_Integration/" in rel or "/09_Frontend/" in rel:
            dest = "contracts/events/README.md"
        elif rel.endswith("openapi.yaml"):
            dest = "planning/refactor/04-operation-crosswalk.json"
        elif "/ci/" in rel:
            dest = "planning/refactor/00-tooling-dependencies.md"
        else:
            dest = "docs/backend/README.md"
        explicit[rel] = dest
    for src, dest in sorted(explicit.items()):
        assert (ROOT / src).exists() and (ROOT / dest).exists(), (src, dest)
        result.append({"source": src, "destination": dest, "disposition": "PRESERVE_HISTORICAL_SOURCE",
                       "reason": "Claim/consumer/tooling review required before RF-11 retirement",
                       "rf_task": "RF-11"})
    return result


def sections(path, pattern, destination):
    lines = (ROOT / path).read_text(encoding="utf-8").splitlines()
    starts = [(i, re.match(pattern, line)) for i, line in enumerate(lines)]
    starts = [(i, m) for i, m in starts if m]
    rows = []
    for n, (start, match) in enumerate(starts):
        end = starts[n + 1][0] if n + 1 < len(starts) else len(lines)
        # The last section stops at the next higher-level chapter or amendment.
        for i in range(start + 1, end):
            if re.match(r"^## (?:\d+\.|V2)", lines[i]):
                end = i
                break
        body = "\n".join(lines[start + 1:end]).strip()
        ident = match.group(1)
        task = SOURCE_TASK.get(ident)
        if task is None:
            raise ValueError(f"Unmapped source ID {ident}")
        claims = []
        paragraphs = re.split(r"\n\s*\n|\n(?=\d+\. \*\*)|\n(?=- \*\*(?:Đầu vào|Hành vi|Kiểm chứng|US-\d+-AC-))", body)
        for number, paragraph in enumerate(paragraphs, 1):
            paragraph = paragraph.strip()
            if paragraph:
                claims.append({"id": f"{ident}.C{number:02}", "content": paragraph,
                               "authority": "HISTORICAL_SOURCE_ONLY"})
        if not claims:
            raise ValueError(f"No source content for {ident}")
        legacy_status = re.search(r"\*\*Trạng thái:\*\*\s*([^.]*)", body)
        related = CONFIRMED_RELATED.get(ident)
        rows.append({"id": ident, "title": match.group(2).strip(), "source": path,
                     "line": start + 1, "source_heading": lines[start],
                     "destination": f"docs/product/{destination}#{ident.lower()}",
                     "module_requirement": TASK_REQUIREMENTS[task],
                     "content_status": "MIGRATED_FULL_SECTION", "authority": "MIXED_CONFIRMED_REFERENCE_AND_HISTORICAL_DETAIL" if related else "HISTORICAL_SOURCE_ONLY",
                     "confirmed_reference": related,
                     "legacy_status": legacy_status.group(1).strip() if legacy_status else "UNSPECIFIED",
                     "implementation_readiness": "BLOCKED_DETAIL_REVIEW" if related else "BLOCKED_AUTHORITY_REVIEW",
                     "disposition": "PRESERVE_AND_REVIEW", "reason": "Content transferred, historical authority not automatically accepted",
                     "rf_task": task, "proposed_owner": TASK_OWNER[task],
                     "coordination_tasks": SOURCE_COORDINATION.get(ident, []),
                     "missing_decision": f"Review {ident} details against {related or 'owner source'} before {task} implements this claim",
                     "blocked_checkpoint": f"{task} START for unconfirmed details; RF-11 retirement",
                     "claims": claims, "content": body})
    return rows


def nfr_rows():
    fr = "docs/diagram/V2/02_Requirements/01_FRD_SRS.md"
    detail = "docs/diagram/V2/02_Requirements/07_Non_Functional_Requirements.md"
    detailed_lines = (ROOT / detail).read_text(encoding="utf-8").splitlines()
    rows = []
    for i, line in enumerate((ROOT / fr).read_text(encoding="utf-8").splitlines(), 1):
        match = re.match(r"^\| (NFR-\d+) \| ([^|]+) \| ([^|]+) \|", line)
        if not match:
            continue
        ident = match.group(1)
        task = SOURCE_TASK[ident]
        number = ident.split("-")[1]
        detail_match = next(((j, l) for j, l in enumerate(detailed_lines, 1)
                             if re.match(rf"^\| {number} \|", l)), None)
        if detail_match is None:
            raise ValueError(f"Missing detailed NFR {ident}")
        detailed = detail_match[1]
        related = CONFIRMED_RELATED.get(ident)
        rows.append({"id": ident, "title": match.group(2).strip(), "source": fr,
                     "line": i, "detail_source": detail, "detail_line": detail_match[0],
                     "source_heading": line,
                     "destination": f"docs/product/historical-nfr.md#{ident.lower()}",
                     "module_requirement": TASK_REQUIREMENTS[task], "content_status": "MIGRATED_FULL_SECTION",
                     "authority": "MIXED_CONFIRMED_REFERENCE_AND_HISTORICAL_DETAIL" if related else "HISTORICAL_SOURCE_ONLY",
                     "confirmed_reference": related,
                     "implementation_readiness": "BLOCKED_DETAIL_REVIEW" if related else "BLOCKED_AUTHORITY_REVIEW",
                     "disposition": "PRESERVE_AND_REVIEW", "reason": "Detailed review document is not an owner acceptance",
                     "rf_task": task, "proposed_owner": TASK_OWNER[task],
                     "coordination_tasks": SOURCE_COORDINATION.get(ident, []),
                     "missing_decision": f"Review {ident} criterion against {related or 'owner source'} before {task} release",
                     "blocked_checkpoint": f"{task} acceptance/release; RF-11 retirement",
                     "claims": [{"id": f"{ident}.C01", "content": line, "authority": "HISTORICAL_SOURCE_ONLY"},
                                {"id": f"{ident}.C02", "content": detailed, "authority": "HISTORICAL_SOURCE_ONLY"}],
                     "content": line + "\n" + detailed})
    return rows


def write_product(rows, name, title):
    output = [f"# {title} (RF-04 historical transfer)", "",
              "These are source contents, not newly Accepted rules. The per-item source status is retained as evidence; current authority and implementation readiness are separate. Owner review is required before each named module uses unconfirmed detail. Accepted 32-44 are in [requirements](requirements.md).", ""]
    for row in rows:
        output += [f"<a id=\"{row['id'].lower()}\"></a>", "", f"### {row['id']} - {row['title']}", "",
                   f"Source: `{row['source']}:{row['line']}`; primary `{row['rf_task']}` / proposed {row['proposed_owner']}; coordination {', '.join(row['coordination_tasks']) or 'none'}. Historical authority: `{row['source_authority']}`; current authority: `{row['authority']}`; confirmed overlap `{row.get('confirmed_reference') or 'none'}`; source label: `{row.get('legacy_status', 'UNSPECIFIED')}`. Readiness: `{row['implementation_readiness']}`.", "",
                   "Transferred source text and acceptance conditions:", ""]
        for claim in row["claims"]:
            output += [f"**{claim['id']}** (origin `{claim['authority']}`, current `{claim['current_authority']}`):", "",
                       "<pre>" + html.escape(claim["content"]) + "</pre>", ""]
        for approved in row["readiness"].get("approved_scopes", []):
            output += [f"Confirmed implementation scope ({approved['decision_id']} / {', '.join(approved['claim_ids'])}): {html.escape(approved['scope'])}. Historical excerpt text remains unconfirmed outside this scope.", ""]
        if row["readiness"]["state"] == "READY":
            output += [f"Ready for scoped implementation. Planned checks (not executed proof): {'; '.join(row['readiness']['required_evidence'])}.", ""]
        else:
            output += [f"Review gate: {row['readiness'].get('reason', row['missing_decision'])}. Checkpoint: {row['readiness'].get('checkpoint', row['blocked_checkpoint'])}.", ""]
    (PRODUCT / name).write_text("\n".join(output), encoding="utf-8")


def write_confirmed_decisions():
    source = (PLAN / "02-decision-register.md").read_text(encoding="utf-8")
    lines = []
    for line in source.splitlines():
        match = re.match(r"^\| (32A|33A|34A|35A|36A|37|38|39A|40|41A|42A|43A|44) \| (.+) \| (.+) \|$", line)
        if match and match.group(1) not in {item[0] for item in lines}:
            lines.append(match.groups())
    if [row[0] for row in lines] != DECISIONS:
        raise ValueError("Confirmed decision source order/content changed; review before generation")
    page = ["# Confirmed owner decisions 32-44 (RF-04 draft transfer)", "",
            "The owner reconfirmed these complete interpretations on 2026-09-30. Original 2026-09-28 question/option wording remains unavailable. Accepted here means requirement authority only: implementation, HTTP contract and tests remain separately unverified. Source: `planning/refactor/02-decision-register.md`, first confirmed table.", "",
            "| ID | Confirmed interpretation | Consequence/qualification |", "|---|---|---|"]
    page += [f"| {ident} | {meaning} | {consequence} |" for ident, meaning, consequence in lines]
    page += ["", "No recall@5, AI timeout, orphan window, or response-cache duration is accepted by this table.", ""]
    (PRODUCT / "confirmed-decisions.md").write_text("\n".join(page), encoding="utf-8")
    return lines


def main():
    groups = {name: sections(*spec) for name, spec in SOURCES.items()}
    groups["nfr"] = nfr_rows()
    ids = {row["id"] for group in groups.values() for row in group}
    if ids != set(SOURCE_TASK):
        raise ValueError({"unmapped": sorted(ids - set(SOURCE_TASK)), "missing": sorted(set(SOURCE_TASK) - ids)})
    apply_readiness([row for group in groups.values() for row in group], ROOT, PLAN / "04-readiness-decisions.json", PLAN / "04-source-crosswalk.json")
    write_product(groups["fr"] + groups["br"], "historical-fr-br.md", "Functional and business rules")
    write_product(groups["us"], "historical-us.md", "User stories and acceptance criteria")
    write_product(groups["pf"], "historical-pf.md", "Process flows")
    write_product(groups["nfr"], "historical-nfr.md", "Nonfunctional requirements")
    write_confirmed_decisions()
    adr = []
    for path in sorted((ROOT / "docs/adr").glob("[0-9][0-9][0-9]-*.md")):
        ident = f"ADR-{path.name[:3]}"
        if ident not in ADR_TRANSITION:
            raise ValueError(f"Unmapped ADR {ident}")
        adr.append({"id": ident, "source": path.relative_to(ROOT).as_posix(),
                    "destination": ADR_TRANSITION[ident], "disposition": "HISTORICAL_TRANSITION_PROPOSED",
                    "rf_task": "RF-05/RF-11"})
    decisions = [{"id": ident, "source": "planning/refactor/02-decision-register.md",
                  "destination": "docs/product/confirmed-decisions.md", "authority": "TARGET_CONFIRMED_2026-09-30",
                  "disposition": "KEEP_CONFIRMED_REQUIREMENT", "rf_task": DECISION_TASK[ident],
                  "proposed_owner": TASK_OWNER[DECISION_TASK[ident]]} for ident in DECISIONS]
    coverage = {prefix: [{"id": ident, "source": source, "destination": destination,
                          "disposition": "MAPPED_TO_IMPLEMENTATION_TASK", "rf_task": task,
                          "proposed_owner": TASK_OWNER[task]}
                         for ident, task in mapping.items()]
                for prefix, mapping, source, destination in [
                    ("R", R_TASK, "planning/refactor/02-requirements-traceability.md", "docs/product/requirements.md"),
                    ("CG", CG_TASK, "planning/refactor/02-contract-gaps.md", "contracts/README.md"),
                    ("L", L_TASK, "planning/refactor/01-legacy-register.md", "planning/refactor/03-master-plan.md")
                ]}
    source_paths = [spec[0] for spec in SOURCES.values()] + ["docs/diagram/V2/02_Requirements/07_Non_Functional_Requirements.md",
                   "planning/refactor/tools/rf04_ownership.py", "planning/refactor/tools/build_rf04_sources.py",
                   "planning/refactor/02-decision-register.md", "planning/refactor/04-decision-index.json", "planning/refactor/04-readiness-decisions.json", "planning/refactor/tools/rf05_readiness.py"]
    data = {"status": "DRAFT_RF04_NOT_ACTIVE", "branch": git("branch", "--show-current"),
            "head": git("rev-parse", "HEAD"), "dirty": bool(git("status", "--porcelain")),
            "input_sha256": {p: hashlib.sha256((ROOT / p).read_bytes()).hexdigest() for p in sorted(set(source_paths))},
            "documents": document_map(), **groups, "adr": adr, "decisions": decisions, **coverage}
    (PLAN / "04-source-crosswalk.json").write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    md = ["# RF-04 historical content crosswalk", "",
          "**Inactive draft.** Every FR/BR/US/PF/NFR row carries the transferred source text and acceptance conditions in the linked product page. Historical status is not owner acceptance. Before implementing a module, review its unconfirmed details; RF-11 checks retirement only.", "",
          f"Branch `{data['branch']}` at `{data['head']}`; dirty={data['dirty']}; input SHA-256 in JSON.", "",
          "| ID | Source | New content | Authority/readiness | Primary / coordination |", "|---|---|---|---|---|"]
    for group in ("fr", "br", "us", "pf", "nfr"):
        for row in groups[group]:
            md.append(f"| {row['id']} | `{row['source']}:{row['line']}` | [{row['title']}](../../{row['destination']}) | {row['authority']} / {row['implementation_readiness']} | {row['rf_task']} / {', '.join(row['coordination_tasks']) or '-'} |")
    md += ["", "## Document paths", "", "| Old source | Draft destination | Retirement gate |", "|---|---|---|"]
    for row in data["documents"]:
        md.append(f"| `{row['source']}` | `{row['destination']}` | {row['disposition']} |")
    md += ["", "## ADR and confirmed decisions", "", "| ID | Source | Destination | Status |", "|---|---|---|---|"]
    for row in adr + decisions:
        md.append(f"| {row['id']} | `{row['source']}` | `{row['destination']}` | {row['disposition']}; {row['rf_task']} |")
    md += ["", "## RF-02 requirement/gap and RF-01 legacy checkpoint owners", "",
           "| ID | Evidence source | Primary proposed task/owner |", "|---|---|---|"]
    for prefix in ("R", "CG", "L"):
        for row in coverage[prefix]:
            md.append(f"| {row['id']} | `{row['source']}` | {row['rf_task']}/{row['proposed_owner']} |")
    md += ["", "Content migration is complete as historical evidence. Authority confirmation and module-specific acceptance remain open where no owner source exists; neither title nor former CHỐT label proves current implementation correctness.", ""]
    (PLAN / "04-source-crosswalk.md").write_text("\n".join(md), encoding="utf-8")
    print(" ".join(f"{key.upper()} {len(groups[key])}" for key in ("fr", "br", "us", "pf", "nfr")),
          "ADR", len(adr), "decisions", len(decisions), "documents", len(data["documents"]))


if __name__ == "__main__":
    main()
