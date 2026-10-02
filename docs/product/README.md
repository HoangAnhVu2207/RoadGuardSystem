# RoadGuard product draft: not active

**RF-04 draft, 2026-09-30.** This directory does not replace `docs/RoadGuard_Project_Scope.md`, V2 requirements or active contracts. Branch `anh`, local source HEAD `2efc8a5775f834c7f0fe37cc0ce703011649e1f1`; the checkout includes pre-existing survey and planning edits. Read [RF-04 report](../../planning/refactor/reports/RF-04.md) before promoting any statement.

## Scope and authority

RoadGuard records project/road context, field survey evidence, Reporter input, defect review, measurement, repair and handover. The backend manages identity, durable evidence and orchestration. Web, Android and AI implementation are delivered by other teams under **TARGET_CONFIRMED decision 44**; backend still owns its adapters, contracts, fixtures and integration. This scope comes from [decision 44](../../planning/refactor/02-decision-register.md) and historical [project scope](../RoadGuard_Project_Scope.md), not from a deployed end-to-end proof.

`confirmed-decisions.md` transfers the complete 32-44 interpretation from the source [decision register](../../planning/refactor/02-decision-register.md). `requirements.md` summarizes product rule ownership and acceptance intent; `workflows.md` records source-linked conceptual transitions; `data-and-quality.md` defines data meaning and NFR evidence. Historical FR/BR, US, PF and NFR full text lives in the four `historical-*.md` pages with per-item review gates. Wire methods and schemas live only in `contracts/` after separate approval. `docs/backend/` describes implementation facts and proposed technical boundaries. None of these draft pages silently elevates older material to Accepted.

Evidence labels used throughout: **CURRENT_VERIFIED** (checked source or command, with limits), **TARGET_CONFIRMED** (owner reconfirmed requirement), **PROPOSED** (design needing approval), **UNKNOWN** (missing authority/evidence), **HISTORICAL** (older source). An Accepted requirement does not show implementation or test success.

## Actor boundary

| Actor | Draft responsibility and authority | Source/status |
|---|---|
| Supervisor | Project creation/oversight, road confirmation, receives temporary safety notice; final mixed-case authority remains to reconcile with workflow source. | HISTORICAL `03_To_Be_Process.md` PF-01..; TARGET_CONFIRMED 35A notice. |
| Project Manager (PM) | Project-scoped planning, candidate and label review, survey/repair decisions; cannot turn a proposed threshold into policy. | TARGET_CONFIRMED 32A, 33A, 34A, 35A; detailed roles HISTORICAL. |
| Repair Crew | Measure and perform assigned allowed work; offline authority rechecked at sync. | HISTORICAL FR-17..23; TARGET_CONFIRMED 42A/43A pilot scope. |
| Drone Operator | Receives survey scope and submits source video/SRT; drone flight control stays outside backend. | HISTORICAL FR-26..30; TARGET_CONFIRMED 44 boundary. |
| Reporter | Registers with OTP, reports evidence and sees only an approved privacy projection of own reports. | CURRENT_VERIFIED registration source; privacy projection UNKNOWN (Q-RF02-07). |
| AI service | Supplies candidate results/provenance; cannot approve PM labels or merge defects. | TARGET_CONFIRMED 34A/44; protocol UNKNOWN (Q-RF02-06). |

The draft [operation crosswalk](../../planning/refactor/04-operation-crosswalk.md) separates 57 observed controller actions and 133 proposed OpenAPI operations. It is not a promise to implement all operations or retire any current route.
