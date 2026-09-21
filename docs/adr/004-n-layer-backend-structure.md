# ADR 004: Retain and Standardize the N-Layer Backend Structure

## Status

Accepted (2026-09-21)

- **Date:** 2026-09-21
- **Author / Owner:** Person 1 (`anh`)
- **Reviewer:** Person 2 (`huy`)
- **Approved by:** Team Lead, 2026-09-21
- **Trace:** P1-71B draft accepted by the Team Lead; P1-72 applies the approved follow-ups.

---

## Context

ADR 001 is Accepted and codifies the five-project dependency graph as `API -> Services -> Repositories; Repositories -> BusinessObjects and DTOs; DTOs -> BusinessObjects` (`docs/adr/001-backend-boundary.md`, lines 76-82). It assigns use-case orchestration and cross-aggregate policy to Services (lines 104-107), requires thin controllers, and forbids API access to Repositories or `DbContext` (lines 109-111). It therefore defines an N-layer backend boundary; it does not approve a Minimal API vertical-slice migration.

A different target was introduced by commit `5d95e90e` under task P1-70. `AGENTS.md`, lines 12-14, says that the target after a separately approved migration is Minimal API vertical slices with direct `RoadGuardDbContext`, while the current structure remains Controller/Service/Repository until then. The endpoint-delivery skill repeats that target at `.agents/skills/roadguard-endpoint-delivery/SKILL.md`, lines 43-45. The P1-70 planning record explicitly places controller/Minimal API conversion out of scope (`planning/RoadGuard_Plan_Person_1.md`, line 45). A repository search found no approved `architecture-migration` task in either plan or ADR 001-003. The migration is therefore mentioned by workflow files but is not planned or accepted by an ADR.

The current source remains N-layer:

- Five production projects were counted by enumerating the `RoadGuardSystem.API`, `RoadGuardSystem.BusinessObjects`, `RoadGuardSystem.DTOs`, `RoadGuardSystem.Repositories`, and `RoadGuardSystem.Services` project directories and their `.csproj` files.
- Six controllers were counted with `rg -l --glob '*.cs' '\[ApiController\]' RoadGuardSystem.API`.
- Eleven endpoint actions were counted with `rg -n --glob '*Controller.cs' '^\s*\[Http(?:Get|Post|Put|Patch|Delete)' RoadGuardSystem.API`.
- There are no Minimal API endpoint mappings in the production source reviewed for Phase 1.
- 275 public type declarations were counted with `rg` over production `.cs` files, excluding `bin`, `obj`, and `Migrations`, using the anchored declaration pattern `^\s*public\s+(?:(?:sealed|abstract|static|partial|readonly)\s+)*(?:class|record|struct|interface|enum)\b`. This is a textual declaration count, includes nested public types, and is not a count of files or runtime types.
- Phase 1 reviewed 166 production `.cs` files after excluding `bin`, `obj`, `TestResults`, `Migrations`, and `tests`.

Task P1-20 is `In Progress` in `planning/RoadGuard_Plan_Person_1.md`, line 36. Its remaining assign/reassign-primary-PM work overlaps Project Services, Repositories, DTOs, API, and tests. Structural movement in those areas would create avoidable conflicts before P1-20 is complete or explicitly frozen.

---

## Decision

RoadGuard will retain and standardize the existing N-layer flow:

`Controller -> IService -> IRepository`

The repository will not migrate production endpoints to Minimal API vertical slices. Standardization is incremental: new or touched code follows the naming and file rules below; unrelated legacy types are not mechanically split.

The Team Lead approved this decision and assigned both ownership scopes to the `anh` branch for P1-72.

### 1. Relationship to ADR 001

**Proposal:** ADR 001 remains Accepted and is amended by ADR 004. ADR 001 already defines the project boundaries and N-layer responsibilities; ADR 004 resolves the later workflow contradiction and adds incremental file/interface conventions without replacing the broader system-boundary decision.

- Confirmed by Team Lead on behalf of the P1-72 implementation scope.

### 2. Vertical-slice workflow text

**Proposal:** remove the unapproved Minimal API/direct-`DbContext` target from `AGENTS.md`, the discovery/rules surface, and `roadguard-endpoint-delivery`. Do not create an architecture-migration task unless both owners reject this ADR and explicitly choose that alternative.

- Confirmed by Team Lead on behalf of the P1-72 implementation scope.

### 3. Interface and file conventions

**Proposal:** every concrete persistence/read-model class injected into a Service must have a repository interface owned at the existing layer boundary. New code uses one public type per file. New DTOs, entities, and interfaces always use one public type per file. Existing multi-type files remain unchanged until their owning feature is modified; there is no repository-wide mechanical split.

- Confirmed by Team Lead on behalf of the P1-72 implementation scope.

### 4. Ownership

**Proposal:** `huy` owns production project-reference changes, repository moves, `RoadGuardDbContext`, EF mappings, migrations, and SQL-backed architecture enforcement. `anh` owns Services/API/DTO organization and non-schema dependency tests. Any change spanning both sets is split into separately approved tasks with an explicit shared-hotspot sequence.

- Confirmed by Team Lead on behalf of the P1-72 implementation scope.

### 5. P1-20 and schema freeze

**Proposal:** P1-20 must be completed or explicitly frozen before structural batches touch its files. Structure-only refactors must not change database schema, migrations, model snapshot, or namespaces of entities, enums, or `RoadGuardDbContext`.

- Confirmed by Team Lead on behalf of the P1-72 implementation scope.

---

## Layer Rules

These rules reflect the actual `.csproj` graph, not a desired dependency inversion that the checkout does not currently implement.

| Layer | May contain | Must not contain |
|---|---|---|
| `BusinessObjects` | Entities, value objects, fixed enums, invariants of the entity itself, argument checks, value normalization, factory methods, and pure calculations from only that object's state. The existing `Microsoft.Extensions.Identity.Stores` and NetTopologySuite references remain the ADR 001 exceptions. | EF Core, HTTP/API contracts, I/O, current-user or configuration access, direct clock/random reads, multi-step orchestration, decisions requiring another entity or database query, and `*Options`/`*Settings` classes. |
| `DTOs` | Request/response DTOs, query/filter input, pagination/result shapes, validation attributes, and shared domain enums where ADR 001 permits the reference. | EF entities, persistence or HTTP-context dependencies, workflow decisions, mutable business calculations, direct clock/random reads, options/settings, or hidden input normalization. |
| `Repositories` | Repository interfaces and implementations, EF Core/SQL Server, mappings, migrations, file storage, transactions, retry/idempotency, rowversion/concurrency, outbox, durable consumer receipts, and persistence backstops. | Authorization or project-scope policy, business state-machine decisions, business calculations, HTTP status/error codes, DTO/`HttpContext`/`IActionResult` coupling, notification delivery, or `IQueryable` exposed to Services. |
| `Services` | `IService` contracts and implementations, use-case orchestration, cross-aggregate policy, current-user-independent business decisions, mapping to DTOs, and coordination through repository interfaces. | `RoadGuardDbContext`, EF Core/`DbSet` queries, `HttpContext`, `IActionResult`, route concerns, or HTTP status-code decisions. |
| `API` | Controllers, route/versioning metadata, authentication/authorization composition, request binding, DTO response mapping, ProblemDetails/stable error-code mapping, middleware, OpenAPI, and DI composition. | Direct repository/`DbContext` access, business calculations or state transitions, persistence orchestration, and returning EF entities. |

Repository durability mechanisms are valid even when they coordinate several writes atomically. The boundary is the kind of decision: transaction, idempotency, rowversion, outbox, and persistence validation remain in Repositories; authorization, workflow status, cross-aggregate business rules, business calculations, and HTTP error semantics do not.

---

## Alternatives Considered

### A. Minimal API vertical slices with direct `RoadGuardDbContext`

**Advantages:** fewer files per new endpoint, less interface/mapping ceremony, and potentially lower prompt context for an isolated endpoint.

**Disadvantages:** contradicts ADR 001's accepted API/Services/Repositories boundary; none of the 11 current endpoint actions uses this structure; no approved migration task exists; and a mixed architecture during P1-20 would increase ownership and review ambiguity. It would also require rewriting architecture tests and workflow documents before production conversion.

**Outcome:** not selected. Revisit only through a separately measured and approved ADR.

### B. Retain and incrementally standardize N-layer

**Advantages:** matches all 11 current endpoint actions and the accepted project graph; preserves P1-20 work; keeps SQL ownership with `huy`; and addresses the seven concrete Service injections found without requiring a blanket repository rewrite.

**Disadvantages:** retains mapping/interface ceremony and more files per feature. AI work must load contracts across layers, so naming and one-public-type rules need consistent enforcement.

**Outcome:** selected, subject to both owner confirmations.

### C. Full N-layer normalization with one public type per file for all existing types

**Advantages:** maximum mechanical consistency, easier source lookup, and smaller individual files.

**Disadvantages:** the measured source contains 275 public declarations across 166 production files, so a blanket split would create broad rename/move churn with little behavioral value. It would overlap P1-20, risk string-based assembly/type assumptions in tests and CI, and consume substantial review and AI context without fixing the Phase 1 K3/K4 findings by itself.

**Outcome:** rejected as a blanket migration. Apply the convention only to new or otherwise modified code.

---

## Consequences

### Positive

- One accepted architectural direction replaces the current ADR/workflow contradiction.
- Existing endpoint behavior and project boundaries remain stable.
- Services gain explicit persistence seams where concrete classes are currently injected.
- Incremental file normalization limits conflicts and reduces unnecessary token use compared with a repository-wide split.

### Trade-offs and Risks

- N-layer features require more named contracts and mapping than a compact vertical slice.
- P1-20 remains a sequencing constraint for Project/Warranty structural work.
- Moving types or changing project references can invalidate architecture tests and tests/CI that inspect assembly or project names as strings.
- Any entity, enum, or `RoadGuardDbContext` namespace change can affect the EF model snapshot even without an intended schema change; those namespaces are frozen for structure-only batches.
- Phase 1 found misplaced business decisions that require behavior-preserving feature tasks and characterization tests. A folder move alone does not resolve them.

### AI Context and Token Cost

- Alternative A is cheapest for a single new endpoint after migration, but the migration itself has high one-time context and verification cost.
- Alternative B has moderate recurring cost because Controller, Service, interface, implementation, DTO, and tests may need to be read together; stable naming and one-type conventions constrain that cost.
- Alternative C has the highest near-term cost because hundreds of declarations and references would churn while behavior remains unchanged.

---

## Enforcement

`DependencyGraphTests` should enforce only rules observable from assemblies or project/package references:

- the five-project reference graph;
- no EF Core runtime dependency in `BusinessObjects` or `DTOs`;
- no API reference from lower layers;
- no Repositories/`DbContext` reference from API;
- no API/HTTP dependency from Services;
- no DTO dependency from BusinessObjects.

The following remain review rules unless a focused analyzer is separately approved:

- one public type per file;
- repository interface required for concrete persistence/read-model collaborators injected into Services;
- no business decisions in Repositories;
- no hidden normalization or business calculations in DTOs;
- entity methods use only their own state and caller-supplied values;
- legacy multi-type files are split only when touched.

---

## Revisit Triggers

Reopen this decision only when measured evidence satisfies an owner-defined trigger. Owners must fill the thresholds before using a trigger:

- Median files changed per endpoint exceeds: ______
- Median implementation/review time per endpoint exceeds: ______
- N-layer mapping defects per release exceed: ______
- AI prompt/context cost per endpoint exceeds: ______
- Number of endpoints suitable for a vertical-slice pilot reaches: ______
- A pilot demonstrates at least ______ improvement without weakening authorization, idempotency, SQL, or ownership checks.

---

## Follow-up Tasks

This ADR performs none of these follow-ups:

1. A0: apply owner-approved updates to `AGENTS.md`, `.agents/rules/roadguard.md`, and `roadguard-endpoint-delivery`; add `roadguard-test-selection`.
2. Structure batches: sequence project references, repository interfaces/moves, DTO/entity file normalization, DI, and architecture tests only after P1-20 is complete or frozen.
3. Separate behavior-preserving tasks for each Phase 1 `must` finding, each with characterization tests before movement.
4. Test-speed tasks S1-S6, selected independently from measured baseline evidence.
5. Amend ADR 001 with a link to ADR 004 only after ADR 004 is Accepted.

---

## Compliance and Verification

- ADR 004 amends ADR 001's layer conventions; ADR 001 remains Accepted.
- P1-72 is authorized to apply the implementation and tooling follow-ups without a schema or migration change.
- Runtime behavior remains subject to the focused and suite-level verification handed to Huy; this acceptance is not runtime-test evidence.
