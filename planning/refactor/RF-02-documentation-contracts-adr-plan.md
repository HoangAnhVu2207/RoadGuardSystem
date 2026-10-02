# RF-02 — replacement documentation, contracts, ADR and planning system

> Preliminary proposal retained for traceability. `03-documentation-design.md` supersedes this layout for RF-03 planning, using the owner-requested `docs/product/`, `docs/backend/`, `contracts/`, `docs/decisions/`, `planning/`, `AGENTS.md` and `.agents/` paths. No replacement path is active yet.

## Identity

- Branch: `anh`
- Surveyed HEAD: `2efc8a5775f834c7f0fe37cc0ce703011649e1f1`
- Working tree: dirty; no old docs deleted or altered.

## Proposed authority model

`docs/` becomes the product and architecture source; `planning/` becomes execution evidence; `agent-guidance/` (proposed new top-level name) becomes operating guidance after owner acceptance. Old `AGENTS.md`, `.agent/`, `.agents/`, and legacy docs remain readable history until a separate retirement decision. They do not govern this program unless the owner explicitly accepts the new system.

Every statement carries one of `CURRENT_VERIFIED`, `TARGET_CONFIRMED`, `PROPOSAL`, `UNKNOWN`, or `HISTORICAL`. A document must name its source paths, revision, date, owner and supersession status.

## Whole-project document set

- `docs/project/README.md`: map, authority, glossary and decision history.
- `docs/project/scope.md`: in/out scope and explicit exclusions.
- `docs/project/domain-map.md`: modules, actors and lifecycle overview.
- `docs/project/requirements/`: FR/BR/UC/AC with stable IDs.
- `docs/project/data/`: canonical dictionary, ERD and provenance rules.
- `docs/project/contracts/`: public API, events, storage and offline contracts.
- `docs/project/adr/`: accepted decisions only; proposals live separately.
- `docs/project/verification/`: evidence policy and release gates.

## Backend document set

- `docs/backend/README.md`: backend map and dependency rules.
- `docs/backend/architecture.md`: runtime composition and layer boundaries.
- `docs/backend/modules/<module>.md`: source-to-contract assessment per module.
- `docs/backend/contracts/openapi.yaml`: canonical API contract after reconciliation.
- `docs/backend/contracts/errors.md`, `authorization.md`, `idempotency.md`, `concurrency.md`.
- `docs/backend/persistence/`: model/configuration/migration chain and SQL verification.
- `docs/backend/operations/`: configuration, storage, workers, observability and rollback.

## ADR design

ADR IDs are monotonic and immutable. Each ADR contains context, decision, alternatives, consequences, affected contracts/modules, evidence links, status (`PROPOSED`/`ACCEPTED`/`SUPERSEDED`), owner and review date. A code or migration change links to an accepted ADR or explicitly records that no ADR applies. The owner reconfirmed the full Appendix D interpretation of 32-44 on 2026-09-30, so those exact requirements may be cited as Accepted; original 2026-09-28 questions/options remain unavailable, and no extra terms may be inferred from numeric labels.

## Contract design

Use one canonical operation record per endpoint: operation ID, route/method, actor and scope, input/validation, response/status, stable errors, state transition, persistence/idempotency/concurrency, audit/sensitive-data rules, and verification evidence. Generate or validate OpenAPI/Postman from this record only after reconciliation. Distinguish `implemented`, `documented`, `verified`, and `proposed`; do not use test pass as a substitute for contract approval.

## Planning design

Each refactor work package has: objective, owner, base revision, scope, dependencies/hotspots, source evidence, proposed delta, acceptance criteria, verification commands, side effects, rollback, open decisions, and append-only history. Status values are `PLANNED`, `IN_PROGRESS`, `BLOCKED`, `PARTIAL`, `DONE`, `REOPENED`. Evidence fields are separate from status.

## Adoption sequence

1. Owner accepts authority boundary and taxonomy.
2. Create canonical indexes and cross-links without deleting old docs.
3. Reconcile one module's contract against source/tests/runtime.
4. Mark old document supersession explicitly.
5. Retire/archive old docs only after no live links or ownership references remain.
