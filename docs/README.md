# RoadGuard documentation map

## Current sources (RF-11, 2026-10-02)

- [Product index](product/README.md) and [confirmed 32-44 decisions](product/confirmed-decisions.md): owner-confirmed requirements; wire/schema adoption is separate.
- [Backend index](backend/README.md) and [current data inventory](backend/data/README.md): observed implementation and verification limits.
- [Contract index](../contracts/README.md): a contract is active only with explicit status and adoption evidence.
- [Decision index](decisions/README.md): confirmed decisions, proposed successors and historical ADR transitions.
- [Refactor checkpoint](../planning/refactor/README.md) and [handoff baseline](../planning/refactor/11-handoff-baseline.md): current task status and branch readiness.
- [Agent manifest](../.agents/manifest.json): the accepted repository guidance inventory.
- [Historical refactor packages](history/refactor/README.md): archived snapshots, findings and runtime evidence; embedded guidance and packaging instructions are inactive.
- [Retired agent guidance](history/agent-pre-rf05/), [old design](history/v1-design/), [worklogs](history/worklogs/) and [historical Superpowers material](history/superpowers/).

The following sections are HISTORICAL design context. Their former source-priority hierarchy and target descriptions do not override current confirmed decisions, contract adoption status or observed implementation. Retained ADR/design files are review sources, not new retirement or implementation authorization.

## Historical documentation map

This folder contains both accepted architecture decisions and target product design. The target incident/segment/AI design is being synchronized with the existing backend documentation; a target statement does not mean the corresponding API, database migration, role seed, worker, or external AI deployment already exists.

## Scope, hotfix and technology notes

- [Backend in-scope/out-of-scope](RoadGuard_Backend_Scope.md)
- [Whole-project in-scope/out-of-scope](RoadGuard_Project_Scope.md)
- [Workflow hotfix plan](history/RoadGuard_Workflow_Hotfix_Plan_v1.md)
- [C# and platform technology research](research/RoadGuard_CSharp_Technology_Research.md)
- [Change 1 Record](history/Change_1_Record.md)

The hotfix plan reopens only the deltas caused by the new Reporter, IncidentCase, route/segment, edge-coverage and external-AI requirements. Historical `Done` rows and their worklogs remain evidence of the behavior accepted at that time; they are not edited to claim that the hotfix is already released.

## Source priority

1. Product decisions recorded in the latest approved design conversation and the two target design documents below.
2. Accepted ADRs for architecture, authentication, and backend/external boundaries.
3. Domain model, Data Dictionary, ERD, use cases, and user stories, which must agree with the target design and preserve historical identifiers where possible.
4. Architecture, API error, setup, prompt, plan, and task documents.
5. Completion logs and worklogs are historical evidence. They retain the scope, role set, and behavior that were true when the work was completed; they are not rewritten retroactively to claim that the new design was already implemented.

## Current target design

- [Incident, Reporter and segment design](history/v1-design/RoadGuard_Incident_Segment_Design_v1.md)
- [AI, segment bands and edge coverage design](history/v1-design/RoadGuard_AI_Segment_Edge_Design_v1.md)
- [Use case specification](history/v1-design/Dac_ta_UseCase_v2.md)
- [User stories and acceptance criteria](history/v1-design/User_Stories_Acceptance_Criteria_v2.md)
- [Domain model](history/v1-design/RoadGuard_Domain_Model_v1.md)
- [Data Dictionary](history/v1-design/RoadGuard_Data_Dictionary_v1.md)
- [ERD](history/v1-design/RoadGuard_ERD_v1.md)

The target design adds the `REPORTER` role (citizen or investor representative) with Gmail self-registration and OTP verification, individual photo coordinates, an `IncidentReport`/`IncidentCase` workflow, versioned configurable segment sets, `SURFACE`/`LEFT_EDGE`/`RIGHT_EDGE` coverage, and an external AI adapter. PM repair input is limited to a general repair method summary; financial data, construction phases, materials, quantities, and detailed execution remain outside scope.

The incident case lifecycle is `New -> Assigned -> Open -> Fixed -> Retest -> Verified -> Closed`. Reporter-facing progress is a separate public event timeline: receiving, accepted, verifying, defect found or no defect with a PM reason, repair progress, and repaired with PM-reviewed after-repair evidence. `Defect.VERIFIED` remains the pre-repair domain decision; `IncidentCase.VERIFIED` means the repair retest passed.

## Architecture decisions

- [ADR 001 — backend boundaries](adr/001-backend-boundary.md)
- [ADR 002 — authentication and authorization](adr/002-authentication.md)
- [ADR 003 — backend acceptance and external AI boundary](adr/003-backend-delivery-and-ai-boundary.md)
- [ADR 004 — N-layer backend structure](adr/004-n-layer-backend-structure.md)
- [ADR 005 — product workflow synchronization](adr/005-product-workflow-synchronization.md)

The accepted runtime architecture remains `Controller -> IService -> IRepository`. The external AI service is behind a versioned adapter and asynchronous job contract. Raw video, aircraft GPS, projected route station, camera footprint, and defect location are separate facts; the AI produces candidate detections and quality/coverage results, while PM/Supervisor decisions remain authoritative.

## Verification of this documentation set

Documentation changes use link, Markdown fence, placeholder, and diff checks. They do not claim runtime behavior. Before implementing the target, reconcile the target-only entities and role with source code, migrations, API contracts, and seeded permissions; then run the affected build and tests for each implementation slice.
