# RF-01 — module assessment and staged refactor plan

## Identity

- Branch: `anh`
- Surveyed HEAD: `2efc8a5775f834c7f0fe37cc0ce703011649e1f1`
- Working tree: dirty; see `RF-00-baseline.md`.
- Status: `CURRENT_VERIFIED` assessment plus `PROPOSAL`; no implementation authorized by this report.

## Module map

| Module | Current evidence | Refactor concern | Proposed slice/order | Gate |
|---|---|---|---|---|
| API host | 17 controllers, middleware, versioning, Swagger, health and workers in `RoadGuardSystem.API/Program.cs` | route/error/auth behavior is a shared compatibility surface | A1 inventory endpoints and error envelopes; A2 isolate composition by concern; A3 controller cleanup only with contract fixtures | API characterization + route/problem-details checks |
| Identity/auth | Auth, reporter onboarding, sessions, refresh/password recovery in API/Services/Repositories and identity migrations | security-sensitive state, token/session concurrency, email/OTP side effects | B1 contract inventory; B2 service/persistence seams; B3 migration/data rehearsal | API auth suite + SQL concurrency + no-secret logs |
| Projects/membership/warranty | project, road section/version, membership, handover/warranty entities and controllers | scope/effective dates and authorization are cross-module | C1 authority matrix; C2 read/write seams; C3 controlled move | authorization API tests + SQL fixtures |
| Survey/planning | SurveyV2, planning controllers and large survey persistence implementation; current dirty split is concurrent work | concurrency, scope and immutable datasets; active files are shared | D1 freeze current diff; D2 characterization; D3 split by use case; D4 data/SQL verification | focused service/API + SQL Server scope/concurrency |
| Files/uploads/storage | upload sessions, parts, immutable files, MinIO/AWS options and worker | external storage and retry/idempotency | E1 storage contract; E2 adapter seams; E3 offline/retry tests | isolated storage smoke + persistence tests |
| Inspection/defects/repair | defects, field inspection, measurements, repair-related workflows | human authority and state transitions must not be inferred from names | F1 state/actor matrix; F2 vertical slices; F3 refactor | API state tests + SQL invariants |
| Processing/AI/validation | processing jobs, attempts, AI results, validation runs | external AI boundary, provenance, late results and async lifecycle | G1 async contract; G2 adapter/persistence split; G3 replay/failure tests | API + SQL + external adapter fake |
| Notifications/audit/outbox | notification, audit, outbox and security logging cross-cutting entities | ordering, retention, PII/sensitive data and delivery semantics | H1 event taxonomy; H2 delivery adapters; H3 operational verification | persistence and redaction tests |
| Seeder/fixtures | separate Seeder project and Postman fixture account docs | fixture ownership may drift from contracts | I1 fixture manifest; I2 isolated deterministic seed; I3 environment checks | clean test DB only |
| Docs/contracts/agent guidance | `docs/`, `planning/V2/`, `.agents/`, `AGENTS.md`, ADRs and OpenAPI/Postman | multiple authority systems and historical/current mixing | J1 adopt new authority; J2 canonical indexes; J3 retire old docs after replacement | link/consistency validators |

## Sequencing rules

1. Documentation authority and evidence taxonomy are approved first.
2. Characterization tests and contract snapshots precede structural moves.
3. Shared persistence and error/middleware hotspots have one writer and are sequenced after their consumers are mapped.
4. Each slice must preserve externally observed routes, status codes, error codes, state transitions and durable facts unless an explicit product decision changes them.
5. No migration is edited/applied as part of refactor planning. Any schema delta becomes a separately approved change with rehearsal and rollback evidence.
6. Dirty concurrent work is frozen or integrated by its owner before touching the same files.

## Candidate work packages

- RF-A Foundation: evidence registry, documentation authority, module inventory.
- RF-B Contracts: endpoint catalogue, stable error model, actor/scope matrix, state transition catalogue.
- RF-C Persistence seams: DbContext/configuration inventory, repository interfaces, transaction/idempotency/concurrency contracts.
- RF-D Identity and project slices.
- RF-E Survey/files/processing slices.
- RF-F Inspection/repair/notification slices.
- RF-G Data migration and release rehearsal (only after explicit schema approval).
- RF-H Legacy retirement and final verification.

## Risks and unknowns

- Existing V2 documents contain target-only entities and role/workflow claims; source comparison is required per operation.
- Current dirty survey changes make Survey a reserved hotspot.
- Migration count and model breadth make a mechanical namespace/folder move high risk.
- Runtime API smoke and SQL evidence need environment-specific confirmation; this survey did not mutate or run shared environments.
