# Antigravity completion log - P2-04

## Assignment history

### Assignment 1 - prepared 2026-09-18T21:20:14+07:00

- Assignment preparation status: `Blocked before activation`. `P2-04` remains `Not started`; it is not added to the current-status table and implementation must not begin until the dependency gate below is satisfied.
- Task ID / title: `P2-04` / File metadata, immutable content/storage boundary, and local test store.
- Person / branch: Person 2 / `huy`.
- Implementer / self-reviewer: Antigravity for Person 2.
- Mandatory acceptance reviewer / Done authority: Codex.
- Baseline: clean local `huy` at `7e44f40ddd971b97609509158552d87c2a4dcaf8`; `huy`, `origin/huy`, local `anh`, and `origin/develop` resolve to this observed revision. No fetch or integration was performed.
- Objective: establish the canonical `File` metadata schema and a repository-owned, streaming, immutable content-store boundary with a confined local implementation so later handover, survey, measurement, repair, export, and evidence tasks can reference one accepted file identity without overwriting original bytes.
- Assignment authority and write check: the repository-owner request authorizes this task-scoped worklog and applicable P2-04 plan metadata only. `docs/worklogs` exists and is writable; `planning/RoadGuard_Plan_Person_2.md` is not read-only. This preparation creates only this worklog because the dependency blocker prevents activation and plan status progression.

## Dependency and baseline evidence

| Dependency / gate | Current-checkout evidence | Assessment |
|---|---|---|
| `P2-10` | The P2 plan requires `P2-10`. Its row is not `Done`, `docs/worklogs/P2-10-completion.md` is absent, and the checkout has no `Session`, `RefreshToken`, password/account log mappings, identity migration, or role seed. `RoadGuardDbContext` contains only P2-02 sets. The P1-00 `ApplicationUser`/`ApplicationRole` skeleton is not P2-10 acceptance evidence. | **Blocking.** `File.uploaded_by_user_id` cannot receive its canonical `User.id` FK or SQL proof before the accepted P2-10 schema is present in this checkout. |
| `P2-02` (transitive through P2-10) | The current table records `Done`; accepted audit/outbox/idempotency/concurrency code, migration, SQL tests, and worklog are present, including commit `e2454e6` in HEAD history. | Present, but does not bypass `P2-10`. |
| EF/SQL/storage foundation | `RoadGuardDbContext`, SQL Server/NetTopologySuite registration, migrations, isolated SQL fixture, and integration-test project are present. ADR 001 selects a local filesystem abstraction initially and leaves the production object-store provider unresolved. | Sufficient foundation after `P2-10` is accepted. |
| One active task per Person 2 | The current-status table has only `Done` rows and no `In Progress`, `Ready for review`, `Changes requested`, or `Blocked` P2 task. | Capacity is free, but P2-04 is not activated because its prerequisite must be assigned and completed first. |

**Blocker resolution and resume point:** the repository owner must assign `P2-10` as the next Person 2 task. After Antigravity implements/self-reviews it, Codex marks it `Done`, and its accepted artifacts are present in this checkout, re-run status/HEAD/dependency/file-ownership checks. If the `User` identity/FK shape matches this contract and no shared-hotspot owner is active, P2-04 may move from `Not started` to `In Progress`. Do not remove or weaken the dependency, create a placeholder User table, or implement P2-10 inside P2-04.

## Trace and source contract

- Plan trace: `US-03`, `US-06`, `DA01`, `KS09`; direct dependency `P2-10`.
- Data Dictionary (highest precedence): section 3.7 defines `File.id`, unique `storage_uri`, `original_name`, `mime_type`, `size_bytes`, SHA-256 `checksum`, nullable `uploaded_by_user_id -> User.id`, `uploaded_at`, and nullable `retention_until`. Section 6.4 requires private storage, server-side size/MIME/checksum checks, and immutable original content.
- ERD: `USER o|--o{ FILE`; the file metadata fields and later consumers are defined without a direct `project_id` on `File`.
- Domain Model: `File` is a repository-backed shared record; original evidence/file content is not updated in place. General `Evidence` is a separate later concern.
- Use Cases: DA01 provides the later project/handover context; KS09 uploads multiple files and tracks progress. P2-04 supplies only the shared persistence/storage primitive, not either end-to-end workflow.
- User Stories: US-03 requires traceable handover/project records; US-06 requires intact multi-file server storage. Device copy/queue behavior remains FE-owned.
- ADR 001: original files live behind an immutable external-store boundary; the backend computes/verifies SHA-256. The initial implementation is a local filesystem abstraction; selecting a production cloud provider is deferred.
- ADR 003: P2-04 is the early File/storage prerequisite; P2-20 and later consumers reuse it. P2-30 owns upload metadata/backend-validation work and P1-30 owns public upload APIs. File limits remain configuration, not hard-coded business thresholds.
- Specification interpretation: the Data Dictionary intentionally allows `uploaded_by_user_id = null` for system-originated content. The plan's negative "missing owner" case is interpreted as a user-attributed write with no valid current `User`, or a non-null dangling FK; it does not authorize changing canonical nullability. No material spec conflict or P2-04-specific unresolved ADR decision was found.

## Acceptance criteria

These IDs are stable for implementation, self-review, fix, and Codex acceptance rounds.

- [ ] **P2-04-AC-01 - Dependency and ownership gate.** Before any production/test edit, prove accepted `P2-10` artifacts are `Done` and present in this checkout, including the canonical `User` table/key. Recheck that no active Person 2 task owns `RoadGuardDbContext`, the model snapshot, migrations, persistence DI, or integration-test infrastructure. No placeholder identity shape or cross-task implementation is permitted.
- [ ] **P2-04-AC-02 - Canonical File schema and migration.** Add one BusinessObjects entity and EF configuration/migration for the Data Dictionary fields with SQL Server types and bounds: `uniqueidentifier` ID; unique URI up to 2048; original name up to 255; MIME up to 120; non-negative 32-bit size; lowercase 64-character SHA-256; nullable uploader FK to the P2-10 User with restrictive delete behavior; UTC `datetimeoffset` upload time; nullable `date` retention. Add meaningful constraints/indexes and application validation. Apply on an empty database and prove downgrade/reapply or an equivalent documented recovery path without rewriting shared migrations.
- [ ] **P2-04-AC-03 - Immutable content identity.** Once committed, bytes and content-identity metadata (`storage_uri`, checksum, size, and observed MIME) cannot be overwritten in place through the production persistence/storage paths. Changed content creates a new file identity and URI. Do not add a general hard-delete API; future retention/legal-hold deletion remains P2-62. Isolated test cleanup may delete only its owned temporary root.
- [ ] **P2-04-AC-04 - Safe streaming local store.** Provide a repository-owned content-store interface and local test implementation that streams with bounded memory, honors cancellation, uses generated opaque object keys under an options-supplied root, writes via an isolated temporary object then atomic finalization where supported, and cleans incomplete temporary content. Reject null/unreadable streams, rooted paths, traversal, containment escape, invalid names, collisions, configured oversize content, and storage I/O failure without publishing a successful File record.
- [ ] **P2-04-AC-05 - Server-observed integrity.** Compute SHA-256 and byte count from the received stream; never trust client name, declared MIME, size, checksum, or storage URI as authoritative. Reject malformed or mismatched expected checksums and declared/observed MIME mismatches for supported test formats. Return only opaque storage identity and verified metadata. Full malware-provider integration and survey-quality confirmation remain later upload-validation work, but this boundary must not claim those checks passed.
- [ ] **P2-04-AC-06 - Owner, retry, race, and failure behavior.** A user-attributed write requires an existing P2-10 User; system-originated content may retain the canonical nullable uploader. Reuse P2-02 idempotency conventions: the same scoped key and same request/content fingerprint returns the same file identity without duplicate bytes/rows; changed payload conflicts; concurrent identical attempts converge to one committed effect. A DB failure after content staging or a storage failure before metadata commit leaves no reported success and has deterministic retry/cleanup evidence.
- [ ] **P2-04-AC-07 - Audit and downstream-safe contract.** Successful metadata creation appends a sanitized `FileStored` audit event through the accepted P2-02 transaction convention, with actor nullable only for a documented system source, correlation ID, file ID, verified size/checksum, and no raw bytes, credentials, machine-local path, or sensitive client metadata. Internal failure categories remain stable for later P1-30 mapping: `file_path_invalid`, `file_too_large`, `file_mime_mismatch`, `file_checksum_mismatch`, `file_owner_not_found`, `idempotency_key_conflict`, and `file_storage_unavailable`. P2-04 publishes no HTTP endpoint or ProblemDetails contract.
- [ ] **P2-04-AC-08 - SQL and component proof.** Negative-first component and real SQL Server tests cover every case listed below; positive tests prove immutable streamed round-trip, exact checksum/size/MIME metadata, uploader FK behavior, unique URI, idempotent replay, audit linkage, and migration lifecycle. EF InMemory, a zero-discovered filter, skipped SQL tests, or an unavailable container is not acceptance evidence.

Completing only a subset is not completion of P2-04. The task remains one parent slice and is `Done` only after all eight criteria pass.

## In scope

- Canonical File entity/property shape in `RoadGuardSystem.BusinessObjects` after P2-10 hands over identity shape.
- EF Core SQL Server mapping, constraints, uploader FK, new migration, model snapshot, DbContext set, and recovery note.
- Repository-owned storage abstraction, options validation, streaming SHA-256/size verification, immutable local test store, retry/cleanup behavior, and DI registration.
- Reuse of accepted P2-02 audit/idempotency/transaction primitives for File creation.
- P2-04-focused unit/component and SQL Server integration tests plus required repository gates.

## Explicitly out of scope

- P2-10 identity entities/mapping/migration/role seed or any placeholder replacement for them.
- Public upload-session/chunk/complete/status DTOs, Services, controllers, ProblemDetails, or OpenAPI (`P1-30`); upload-session metadata, `SurveyFile`, `SurveyDataVersion`, `QualityCheck`, supplementary rounds, and backend-validation work (`P2-30`).
- Project/ProjectMember/HandoverDocument (`P2-20`), field measurement links (`P2-40`), general Evidence/repair evidence (`P2-53`), export generation (`P2-61`), or retention/legal-hold deletion (`P1/P2-62`).
- Project authorization decisions; File has no project FK, and downstream resources must establish/recheck scope before exposing content.
- Production cloud object-store provider selection, SAS/presigned URL delivery, malware-engine selection/integration, mobile offline queue/local cleanup, AI processing, or server confirmation of a survey dataset.
- SDK/package upgrades, unrelated refactors, edits to accepted migrations, commit, merge, push, deployment, or publication.

## Preconditions and decisions

- Actor and project-scope rule: P2-04 is an internal persistence/storage component, not a public actor endpoint. A future authenticated user workflow passes its current server-side User ID; a documented backend worker/import may use null. File itself carries no project scope, so it never proves authorization. Later consumers derive the owning project from their server-side resource and enforce current membership before read/replay/download.
- State before / allowed state after: no file identity -> temporary private content -> verified immutable object plus File metadata and audit/idempotency outcome. Any validation, storage, SQL, or cancellation failure -> no reported committed identity and deterministic cleanup/retry state. There is no update-in-place transition for original content.
- Data/version/immutability rules: SHA-256 is lowercase hex; byte count and MIME are server-observed; UTC uses `DateTimeOffset`; options own root and file-size limits. Retention metadata does not authorize deletion and must remain compatible with later Warranty/legal-hold policy.
- Audit event and error categories: `FileStored` is append-only and sanitized. Stable internal categories are listed in AC-07; P1-30 owns later HTTP status/ProblemDetails publication and may map them without changing their meaning.
- Idempotency/concurrency behavior: scope by actor (nullable only for documented system operation), operation, and any later project context supplied by the caller; fingerprint immutable request metadata plus verified content hash. Same key/same payload replays the original result; changed payload conflicts; unique-key races are handled separately from optimistic-concurrency errors.
- Configuration/secrets: the local root and maximum size are validated options; no hard-coded production path, limit, credentials, public URI, or engineering threshold. Storage credentials/provider configuration are deferred with the production provider.
- Decisions required: none inside P2-04 after P2-10. If accepted P2-10 uses a User key/nullability incompatible with the Data Dictionary FK, stop and request an ownership/schema decision instead of adapting silently.

## Intended files and exclusive ownership

Person 2 owns the following paths only after activation:

- `RoadGuardSystem.BusinessObjects/Files/**` - File entity and entity-local validation; conceptual name remains Data Dictionary `File` even if a CLR name avoids `System.IO.File` ambiguity.
- `RoadGuardSystem.Repositories/Configurations/*File*Configuration.cs` - EF mapping.
- `RoadGuardSystem.Repositories/Storage/**` and `RoadGuardSystem.Repositories/Options/*FileStorage*` - storage interface, results/errors, options, and local implementation.
- `tests/RoadGuardSystem.IntegrationTests/Files/**` - P2-04 component/SQL/migration tests.
- `docs/worklogs/P2-04-completion.md` - assignment, implementation evidence, self-review, and Codex review history.

Shared hotspots reserved for P2-04 only while it is active:

- `RoadGuardSystem.Repositories/RoadGuardDbContext.cs`
- `RoadGuardSystem.Repositories/Extensions/RoadGuardPersistenceExtensions.cs`
- `RoadGuardSystem.Repositories/Migrations/<new P2-04 migration>.cs` and designer
- `RoadGuardSystem.Repositories/Migrations/RoadGuardDbContextModelSnapshot.cs`
- `RoadGuardSystem.Repositories/RoadGuardSystem.cRepositories.csproj` and `tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj` only if an already-approved dependency/reference is actually required; no package upgrade is authorized.
- `planning/RoadGuard_Plan_Person_2.md` status metadata for P2-04 at activation/handoff/review, serialized with Codex.

Conflict warning: `P2-10` must first own and complete identity entity/mapping, `RoadGuardDbContext`, migrations/snapshot, DI, and SQL test hotspots. P2-04 must not edit them concurrently. After P2-04 is accepted, P2-20/P2-30/P2-40/P2-53 and other consumers may add FKs/use the boundary without remapping or mutating the accepted File shape. Any required schema reopening needs an explicit conflict decision.

## Required negative-first checks

Antigravity must add and run these negative/edge tests before implementation, observe the intended behavioral RED (not a compile/setup failure), then add positive contracts before production code:

| Check | Layer | Expected result |
|---|---|---|
| Null/unreadable/empty stream; malformed original name or MIME | Component | Rejected with stable validation category; no temp/final object, File row, idempotency outcome, or success audit. Empty content may be accepted only if a later explicit product rule is recorded; absent that rule it is rejected. |
| Rooted path, `..` traversal, separator tricks, canonical/symlink containment escape | Component | `file_path_invalid`; nothing is written outside the isolated configured root. |
| Declared size mismatch, configured limit exceeded while streaming, cancellation mid-stream | Component | `file_too_large` or cancellation; bounded memory and incomplete temp cleanup proven. |
| Malformed/wrong checksum or content changed under expected hash | Component/integration | `file_checksum_mismatch`; no committed identity/effect. |
| Declared/observed MIME mismatch for supported fixture types | Component | `file_mime_mismatch`; client declaration is not treated as proof. |
| Unknown non-null uploader or user-attributed request without a valid User | SQL/component | FK or `file_owner_not_found`; system-null owner remains an explicit separate positive case. |
| Same retry key with changed metadata/content; concurrent same-key writers | SQL/component | `idempotency_key_conflict` for changed payload; identical race converges to one row/object/audit effect. |
| Duplicate storage URI / case-sensitive checksum violations / negative or overflow size | SQL Server | Database constraints reject the invalid row. |
| Attempt to overwrite committed bytes or content-identity metadata | Component/SQL | Rejected; original content and row stay unchanged. |
| Storage exception or SQL failure after staging | Component/SQL | `file_storage_unavailable` or persistence failure; no reported success, deterministic cleanup/retry, no orphan accepted as committed. |
| Update/delete through generic persistence path | SQL/component | Content identity cannot be updated; no general deletion path bypasses future retention/legal-hold rules. |

## Required positive checks

| Check | Layer | Expected result |
|---|---|---|
| Small streamed file round-trip | Component + SQL Server | Server-computed size/SHA-256/observed MIME match independent expected values; bytes round-trip without whole-file buffering; File row and sanitized audit are persisted. |
| User-attributed file | SQL Server | Accepted P2-10 User FK round-trips and restrictive delete behavior preserves referenced history. |
| Documented system-originated file | SQL Server | Nullable uploader round-trips with a system audit source and correlation, without pretending to be an authenticated user. |
| Identical retry and concurrent replay | Component + SQL Server | Same file identity/result returned; exactly one final object, metadata row, idempotency outcome, and required audit effect. |
| Migration lifecycle | SQL Server | Empty apply, schema/constraint/index inspection, downgrade/reapply (or documented equivalent), and prior P2-02/P2-10 migrations remain intact. |

## Required commands and evidence

Run and record exact exit code, environment, timestamp, discovered/passed/failed/skipped counts, and relevant container/SQL identity without secrets:

```powershell
dotnet restore RoadGuardSystem.slnx
dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --filter "TaskId=P2-04"
dotnet build RoadGuardSystem.slnx --no-restore --no-incremental
dotnet format RoadGuardSystem.slnx --verify-no-changes --no-restore
dotnet test RoadGuardSystem.slnx --no-build
git diff --check
```

Also run the existing migration consistency/security checks applicable after P2-10 and inspect the generated migration/model snapshot. Real SQL Server/Testcontainers execution is mandatory. Record unavailability as a blocker, not a pass. Do not run unrelated API tests merely to inflate evidence; API behavior is out of scope, while the full solution test gate remains required for regression coverage.

## Ready for review and Done gates

- Ready for review: dependency gate is satisfied; plan row was moved to `In Progress` only when implementation began; all ACs are implemented; negative RED, positive contract, and final GREEN chronology is recorded; real SQL/migration and required solution gates pass with non-zero test discovery and no unexplained skips; files/diff identity are complete; Antigravity self-reviews authorization/scope, immutability, retry/idempotency, races, audit/redaction, failure cleanup, migration recovery, and shared-hotspot conflicts; all in-scope findings are fixed. Antigravity then records `Ready for review`, stops editing submitted artifacts, and yields review/status sections to Codex.
- Done: Codex reviews the exact submitted commit or working-tree diff including relevant untracked files; verifies P2-10 integration, all eight ACs, required SQL/component/solution checks, Antigravity self-review, migration/recovery evidence, no unresolved mandatory finding, and conflict resolution; appends acceptance evidence and alone updates the P2 plan row to `Done`. Done does not authorize merge, push, deployment, publication, or starting another task.

## Preparation commands and result

These are assignment-preparation checks only, not implementation or acceptance evidence.

| Command / inspection | Result | Time |
|---|---|---|
| `git status --short --branch`; `git rev-parse HEAD`; recent `git log` | Clean `huy`; HEAD `7e44f40ddd971b97609509158552d87c2a4dcaf8`; P2-02 accepted artifacts in history. | 2026-09-18 +07:00 |
| Read root `AGENTS.md`, both Person plans, P2-04/P2-10 rows, worklog template, delivery skill and stack/negative-first references | Ownership, dependency, evidence, review, and no-production-edit rules confirmed. | 2026-09-18 +07:00 |
| Inspect P2-10 status/log/entities/DbContext/migrations/seed | Required dependency absent and not accepted in this checkout; blocker confirmed. | 2026-09-18 +07:00 |
| Read Data Dictionary -> ERD/Domain Model -> Use Cases -> User Stories, then ADR 001/002/003 | File schema, immutability, trace, local-store boundary, identity FK, downstream ownership, and deferred provider decision confirmed. | 2026-09-18 +07:00 |
| Inspect `global.json`, project files, DbContext, persistence DI, existing migrations and SQL-test infrastructure | net8.0/EF Core 8.0.17 baseline and shared hotspots confirmed; no upgrade authorized. | 2026-09-18 +07:00 |
| Check `docs/worklogs` and P2 plan attributes | Worklog directory exists; plan is not read-only. Only this worklog was selected for the blocked preparation. | 2026-09-18 +07:00 |
| `pwsh -NoProfile -File tests/Documentation/Verify-P102Docs.ps1` | Exit 0; current specification/plan trace and dependency contracts pass. | 2026-09-18 +07:00 |
| `pwsh -NoProfile -File tests/Tooling/Verify-AntigravitySetup.ps1` | Exit 0; workspace rules, skill discovery/references, and MCP configuration pass. | 2026-09-18 +07:00 |
| `git diff --check`; `git status --short --branch` | Exit 0; no tracked whitespace error; the only worktree change is this untracked P2-04 worklog. | 2026-09-18 +07:00 |

## Implementation evidence

- Files changed by Antigravity: Not started.
- Migration/recovery evidence: Not started.
- Negative-first RED evidence: Not started.
- Positive-contract/GREEN evidence: Not started.
- Antigravity self-review: Not started.
- Assignment-preparation file change: added `docs/worklogs/P2-04-completion.md`; no plan, production, test, migration, integration, or publication change.
- Current implementation status: `Blocked before activation`; P2-04 remains `Not started`.
- Exact next action: assign and complete `P2-10`; then revalidate this assignment and begin P2-04 only after Codex acceptance and artifact integration.

## Codex acceptance review - append one section per round

No acceptance round exists. Assignment preparation and dependency inspection are not implementation evidence.

## Assignment 2 - current-checkout recheck 2026-09-19T15:00:42+07:00

- Preparation only; `P2-04` remains `Not started`. This section supersedes the old dependency/status snapshot, not its historical evidence or the stable AC-01 through AC-08 above. No implementation, review verdict, commit, or publication is claimed.
- Task / Person / branch: `P2-04` / Person 2 / `huy`; Codex Implementer owns implementation and self-review, and a separate Codex Reviewer task/session owns acceptance and `Done`.
- Baseline: `ec9572507eb63b446ded06361dd7cf8d104f134c`. Working tree also contains an unrelated modified `planning/RoadGuard_Plan_Person_1.md` (end-of-line normalization, no content diff) and this already-existing untracked P2-04 worklog. Preserve both; this preparation edits only this worklog. Its pre-edit SHA-256 was `0414932965A882298C2D1899056B282839826BAF6CA0C638A9F8F5AEC8F9D370`.
- Direct dependency `P2-10` is now `Done` in the Person 2 plan and present at HEAD: `ApplicationUser : IdentityUser<Guid>`, `Users` table and DbContext set, identity/security migrations, and P2-10 worklog are tracked. P2-02 audit/idempotency and SQL foundation also remain present. Historical Assignment 1's missing-P2-10 blocker no longer describes this checkout.
- Activation gate: the same plan still records `P2-01` as `In Progress` for hosted-CI compatibility. Even though earlier conversation reported successful hosted runs, that is not a plan-status change or independent acceptance. Under the one-unfinished-task-per-Person rule, do not move P2-04 to `In Progress` or edit its production/tests until P2-01 has an accepted `Done` record and the hotspot/checkout check is repeated. No owner/product decision is needed merely to record this gate.

### Assignment contract (unchanged scope)

| AC | Trace | Observable outcome / evidence required at implementation handoff |
|---|---|---|
| AC-01 | `P2-10`, P2-02 | Dependency artifacts and exclusive shared-hotspot ownership proven against the implementation checkout. |
| AC-02 | `US-03`, `DA01`; Data Dictionary 3.7 | Canonical File metadata, nullable `uploaded_by_user_id -> Users.id`, new SQL mapping/migration, uniqueness/constraints, downgrade or recovery proof. |
| AC-03 | `US-03`, `US-06`, `DA01`, `KS09` | Stored original bytes and content identity never updated in place; changed content gets new identity. |
| AC-04 | `US-06`, `KS09` | Private, configured, contained local test store streams safely; rejects traversal, oversize, cancellation and failed writes without success. |
| AC-05 | `US-06`, `KS09`; Data Dictionary 6.4 | Server-observed byte count, SHA-256 and supported MIME; rejects malformed/mismatched declarations. |
| AC-06 | `US-06`, `KS09`; P2-02 | Valid current uploader or documented null system source; same scoped retry converges, changed retry conflicts, races/failures leave no false success. |
| AC-07 | `US-03`, `US-06`; P2-02 | Sanitized `FileStored` audit and stable internal failure categories; no public HTTP contract. |
| AC-08 | `US-03`, `US-06`, `DA01`, `KS09` | Negative-first component and real SQL Server tests, positive immutable round-trip, migration lifecycle, owner FK, audit, concurrency and failure cleanup. |

- In scope: Person 2 File entity shape, EF SQL Server mapping/new migration/DbContext, repository storage boundary with validated options and streamed local implementation, reuse of P2-02 transaction/audit/idempotency primitives, component/SQL tests, and migration recovery note. Work in small negative/edge RED -> positive contract -> GREEN slices.
- Out of scope: P2-01 CI fixes, P2-10 identity changes, P2-20 project/handover, P2-30 upload/session/validation jobs, P1-30 DTO/Services/API, later Evidence/retention, project membership decisions, production cloud provider, malware integration, SDK upgrades and unrelated refactors. File has no project FK; downstream consumers must authorize project-scoped access. No Git integration/publication is authorized by this assignment.
- Exclusive P2-04 paths after activation: `RoadGuardSystem.BusinessObjects/Files/**`, `RoadGuardSystem.Repositories/Configurations/*File*Configuration.cs`, `RoadGuardSystem.Repositories/Storage/**`, `RoadGuardSystem.Repositories/Options/*FileStorage*`, `tests/RoadGuardSystem.IntegrationTests/Files/**`, and this worklog. Shared hotspots for P2-04 only while active: `RoadGuardDbContext.cs`, persistence DI, a newly generated migration/designer and model snapshot, and `planning/RoadGuard_Plan_Person_2.md` task status. Existing `RoadGuardSystem.Repositories/RoadGuardSystem.cRepositories.csproj` and integration-test project file require a separate hotspot check if a reference is actually needed; no upgrade is assigned.
- Conflict warning: P2-01 / Person 2 currently occupies the sole active task slot (CI workflow/verifier/evidence, no claimed File overlap). P1-10's accepted temporary repository ownership is released per the P2 plan; the dirty P1 plan file remains Person 1's work and is excluded. Sequence: finish/accept P2-01, recheck P1/P2 ownership, then activate P2-04; P2-20/P2-30 consume the accepted File shape afterward. Stop and request an ownership/schema decision only if actual shared-file overlap or incompatible User FK appears.
- Required checks on submitted content: targeted negative-first tests and positive contract tests; real SQL Server File mapping, constraints/FK, migration upgrade/recovery, retry/race/audit and storage failure tests; `dotnet restore RoadGuardSystem.slnx`, `dotnet build RoadGuardSystem.slnx --no-restore --no-incremental`, `dotnet format RoadGuardSystem.slnx --verify-no-changes --no-restore`, affected integration tests with nonzero discovery/no required skips, full solution tests for the shared schema/cross-project change, applicable migration/security verifiers, and `git diff --check`. Record command, exit, counts, time, environment and exact artifact identity once. An unavailable SQL/container or hosted-required gate is a gap, never a pass. Public API and wrong-project endpoint tests are N/A to this internal, projectless File primitive; later consumers own them.
- Ready for review gate: all AC-01..08 evidenced, negative-first chronology and migration recovery recorded, accepted dependency/current ownership confirmed, required checks green, Codex Implementer self-review complete, and submitted artifacts frozen with a compact review packet. Done gate: a separate Codex Reviewer verifies the exact artifacts, ACs, tests, dependency, self-review and finding/conflict closure, then records the verdict and only then updates the P2 plan row. Neither gate implies merge/push.
- Preparation verification only: status/HEAD, plan rows, P2-10 tracked artifacts, Data Dictionary/ERD/Domain Model, use cases/stories, ADR 001/003 and existing worklog inspected. No runtime/SQL tests were run for this documentation-only recheck.
- Preparation checks at 2026-09-19 +07:00: `pwsh -NoProfile -File tests/Documentation/Verify-P102Docs.ps1` exit 0 (documentation contracts passed); `pwsh -NoProfile -File tests/Tooling/Verify-AntigravitySetup.ps1` exit 0 (workspace/discovery references passed); `git diff --check` exit 0 (tracked paths only, does not inspect this untracked worklog). `git status --short --branch` still reports only the pre-existing P1 plan modification and this P2-04 worklog. These are documentation/preparation checks, not P2-04 runtime or acceptance gates.

## Implementation activation - 2026-09-19

- Status: `In Progress`; Codex Implementer for Person 2 on `huy`, baseline `ec9572507eb63b446ded06361dd7cf8d104f134c`. The earlier blocked assignment and recheck remain historical; stable AC-01 through AC-08 above govern this implementation.
- Current-checkout gate: P2-01 and P2-10 are `Done` in the Person 2 plan; the accepted P2-10 `Users`/identity migrations, P2-02 audit/idempotency schema, DbContext and SQL fixtures are present in HEAD. No other unfinished Person 2 task owns the shared hotspots; the Person 1 P1-10 temporary Repository ownership was released after acceptance/integration. Recheck if files move during implementation.
- Exclusive paths and shared hotspots: those declared in `Intended files and exclusive ownership` above are reserved for P2-04 until handoff. Existing P1 plan modification, P2-01 review/status metadata and all other unrelated changes are preserved and excluded from P2-04 implementation. Conflict warning is currently resolved by sequencing; no concurrent edits to P1-owned files are authorized.
- Actor/preconditions/transition/scope/audit: internal storage caller supplies a current persisted User for an attributed write, or an explicit system source for null uploader; no public HTTP actor or project authorization is inferred from File. Valid input moves temporary private bytes to a verified immutable object plus File row, idempotency outcome and sanitized `FileStored` audit; failure/cancellation yields no reported committed identity, with cleanup/retry. Later consumers must recheck current membership and file ownership through their own project entity.
- Negative-first slices: (1) SQL mapping/immutability and canonical uploader FK, (2) private streamed store with path/size/checksum/MIME/incomplete-write failures, (3) atomic File/audit/idempotency with owner/race/rollback, then positives for each slice. Execute targeted tests first and record real RED/GREEN outputs; submit only after required SQL, migration recovery, solution and self-review gates. No acceptance, commit, merge or push is implied by activation.

## Codex Implementer submission - 2026-09-19T16:19:30+07:00

- Status: `Ready for review`; Person 2 / branch `huy`; baseline HEAD `ec9572507eb63b446ded06361dd7cf8d104f134c`. Codex Implementer now freezes the submitted production/tests and yields P2-04 review/status sections to an independent Codex Reviewer.
- Dependencies: P2-01, P2-02 and P2-10 remain `Done`; canonical `ApplicationUser : IdentityUser<Guid>`, P2-02 audit/idempotency services and all predecessor migrations were exercised by the current migration lifecycle and full solution gate.
- Implementation identity: 21 scoped implementation/test files, sorted by the list below. SHA-256 of UTF-8 lines `path<TAB>lowercase-file-sha256`, joined with LF and no trailing LF, is `d19c323ba8016d55c02ad87e24635e18ea9e14583b6f590ab00deb5e773e1f83`. Worklog and P2 plan status metadata are intentionally outside this non-self-referential implementation manifest. The unrelated P1 plan line-ending change and P2-01 accepted review/status metadata are excluded.

### Files changed

- `RoadGuardSystem.BusinessObjects/Files/StoredFile.cs`
- `RoadGuardSystem.Repositories/Configurations/StoredFileConfiguration.cs`
- `RoadGuardSystem.Repositories/Extensions/RoadGuardPersistenceExtensions.cs`
- `RoadGuardSystem.Repositories/Files/FileRepository.cs`, `FileStoreResult.cs`, `IFileRepository.cs`, `StoreFileRequest.cs`
- `RoadGuardSystem.Repositories/Migrations/20260919085118_AddImmutableFileStorageBoundary.cs`, its designer, and `RoadGuardDbContextModelSnapshot.cs`
- `RoadGuardSystem.Repositories/Options/FileStorageOptions.cs`
- `RoadGuardSystem.Repositories/RoadGuardDbContext.cs`
- `RoadGuardSystem.Repositories/Storage/FileStorageErrorCodes.cs`, `FileStorageException.cs`, `IFileContentStore.cs`, `LocalFileContentStore.cs`, `StoredContent.cs`
- `tests/RoadGuardSystem.IntegrationTests/Files/FileRepositoryBoundaryTests.cs`, `FileRepositorySqlTests.cs`, `FileSchemaContractTests.cs`, `LocalFileContentStoreTests.cs`

### AC coverage and observable result

| AC | Result / evidence |
|---|---|
| AC-01 | Accepted P2-10 User schema and released P1-10 Repository hotspots verified in checkout before activation; no other unfinished P2 task. |
| AC-02 | `Files` mapping/migration has canonical fields/types, unique URI, nullable restrictive User FK, size/checksum CHECKs, UTC timestamp/date retention, application factory validation and no model drift. |
| AC-03 | Factory has private setters; DbContext rejects update/delete; SQL trigger rejects raw UPDATE/DELETE; local store uses generated opaque keys and never overwrites a collision. |
| AC-04 | 64 KiB pooled streaming buffer, cancellation, absolute configured root, temp file plus same-root atomic move, traversal/reparse-point containment, oversize/collision/I/O and partial-temp cleanup tests. No public hard-delete method is exposed. |
| AC-05 | Server computes byte count and lowercase SHA-256; signature-sniffs PDF/PNG/JPEG/MP4 and strict UTF-8 text/SRT, otherwise octet-stream; checksum/size/MIME mismatches fail with stable codes. |
| AC-06 | Unknown/missing attributed owner has no effect; explicit system source permits null owner; P2-02 scoped idempotency returns replay or changed-payload conflict; a real two-DbContext race converges to one row/object/audit; injected SQL failure rolls back DB and removes object. |
| AC-07 | Atomic handler appends one `FileStored` audit with actor/system source, correlation, ID, verified size/checksum allow-list only; no bytes, local root, credentials or client metadata. Stable storage/retry error codes are repository-only; no HTTP controller/ProblemDetails added. |
| AC-08 | Final P2-04 filter passed 30/30, 0 skipped on real SQL Server; migration apply/downgrade/reapply, constraints/FK/trigger, round-trip, retry/race/audit/failure behavior and component edges are covered. |

### RED / GREEN chronology

| Slice | RED observed | GREEN evidence |
|---|---|---|
| Schema | 2/2 failed because no `Files` entity/CHECKs; 2/2 factory/immutability tests failed because `Create`/guard were absent. | Schema/factory/DbContext tests pass; generated migration and real SQL tests pass. |
| Storage containment | Boundary test failed because options/store types were absent; symlink test then proved a valid-looking object key could open a target outside root. | Traversal and reparse-point tests pass; final component suite includes cleanup, size, checksum, MIME, cancellation, collision and bounded-read checks. |
| SQL immutability | Raw SQL UPDATE succeeded before the new migration trigger. | UPDATE and DELETE both rejected by `TR_Files_Immutable`; lifecycle proves trigger disappears/reappears with migration. |
| Entity validation | Path/MIME/null-checksum theory failed 3/3 (invalid values admitted or wrong exception). | Factory validation theory passes 3/3. |
| Stable stream error | Null stream returned untyped `ArgumentException`. | Null/disposed streams return `file_content_invalid`. |
| Survey formats | Valid MP4 and SRT each failed with MIME mismatch before their byte detectors existed. | Server-observed `video/mp4` (`ftyp`) and strict UTF-8 `text/plain` tests pass. |

One initial test attempt produced a compile-only collection-expression error and one EF runtime-metadata test used the read-optimized model; both test-harness issues were corrected before behavioral RED was recorded and are not counted as RED evidence.

### Verification evidence

- Environment: Windows 11, PowerShell 7.6.5, pinned .NET SDK `10.0.401`, Docker `29.4.2`, SQL Server image `mcr.microsoft.com/mssql/server:2019-CU18-ubuntu-20.04`, EF tool `8.0.17` for migration generation/model checks.
- `dotnet restore RoadGuardSystem.slnx`: exit 0; 9-project solution restored/current.
- `dotnet build RoadGuardSystem.slnx --no-restore --no-incremental`: final exit 0; 9 projects, 0 warnings, 0 errors.
- `dotnet format RoadGuardSystem.slnx --verify-no-changes --no-restore --verbosity minimal`: final exit 0.
- `dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --no-build --filter "TaskId=P2-04"`: final exit 0; 30 passed, 0 failed, 0 skipped, real SQL Server.
- First unconfigured `dotnet test RoadGuardSystem.slnx --no-build` attempt: exit 1; Unit 124/124 and API 56/56 passed, Integration 55 passed/139 failed because parallel Testcontainers exhausted the local SQL environment (containers exited 255 and one instance failed CLR/spatial initialization). This is retained as environment failure, not a product RED or pass.
- Final controlled single-SQL `dotnet test RoadGuardSystem.slnx --no-build`: exit 0; Unit 124/124, API 56/56, Integration 196/196, total 376/376, 0 skipped; unique container removed in `finally` and no P2-04 gate container remains.
- `pwsh -NoProfile -File tests/Security/Verify-DependencySecurity.ps1`: exit 0; no High/Critical vulnerable dependency. No package/project file changed afterward, so this evidence remains content-valid.
- P1-02 docs, P2-03 planning (9/9) and Antigravity/Codex discovery verifiers: exit 0. `git diff --check`: exit 0; unrelated P1 line-ending warning only.
- `dotnet-ef migrations has-pending-model-changes` using 8.0.17: exit 0, no model drift. Migration script from `20260918185738_EnforceSecurityLogSafeCodes` contains `Files`, checksum CHECK and immutable trigger. Real SQL lifecycle test applies predecessor, applies P2-04, downgrades to predecessor, and reapplies; recovery is to migrate down to that predecessor, which drops the new table/trigger, then reapply after resolving the cause. No shared migration was edited.

### Database, config and operations impact

- Adds migration `20260919085118_AddImmutableFileStorageBoundary`; no seed change and no data backfill. Downgrade drops only the new `Files` table and its trigger, so production downgrade would discard P2-04 data and requires backup/explicit operational approval.
- Host configuration must supply `FileStorage__RootPath` as an absolute private path and `FileStorage__MaximumSizeBytes` in the range 1..`Int32.MaxValue`. No machine path, credential or business threshold was committed. Production cloud provider, malware scanning and retention/legal-hold deletion remain assigned later.
- No API/OpenAPI/DTO/Services behavior changed. The repository returns opaque storage identity plus verified metadata; downstream project authorization and public upload sessions remain P1-30/P2-30.

### Codex Implementer self-review

- Authorization/scope: internal primitive only; File has no project FK and conveys no authorization. Existing User is required for attributed writes; null actor requires explicit system origin. No endpoint trusts claims.
- State/immutability/versioning: content identity is create-only at entity, DbContext, SQL trigger and storage collision layers. No delete boundary is public; retention is metadata only.
- Idempotency/concurrency/failure: fingerprint uses actor/system identity, normalized metadata, verified bytes and retention; same payload replays, changed payload conflicts, parallel same-key calls converge. Storage and DB failures do not report success; SQL failure compensation is verified.
- Audit/secrets: one sanitized audit/idempotency effect commits with File metadata; snapshots allow only size/checksum. Source, correlation and actor semantics are tested. Secret scan found no production credential/path exposure.
- Tests/gaps: all AC risk categories are covered without skips. MIME sniffing is deliberately limited to PDF/PNG/JPEG/MP4/strict UTF-8 text and generic octet-stream; P2-30 owns deeper media validation, completeness, malware and server confirmation. No open in-scope finding, schema/product decision or ownership conflict remains.
- Finding IDs addressed: none; no prior P2-04 acceptance round exists.

### Independent reviewer prompt

```text
Ban la Codex Reviewer doc lap cho RoadGuard P2-04, Person 2, branch huy.
Baseline: ec9572507eb63b446ded06361dd7cf8d104f134c. Submission: working-tree implementation manifest d19c323ba8016d55c02ad87e24635e18ea9e14583b6f590ab00deb5e773e1f83.
Worklog: docs/worklogs/P2-04-completion.md.

Phien nay phai la task/session rieng, khong phai phien da author artifact. Doc AGENTS.md, dung roadguard-review va roadguard-review-p2. Review AC-01..AC-08, dung 21-file manifest trong worklog; bao gom relevant untracked files nhung loai tru P1 plan line-ending change va P2-01 review/status metadata. Khong sua production code/tests. Doi chieu RED/GREEN, real SQL migration apply/down/reapply, immutable trigger, containment/symlink, streamed checksum/MIME, owner/system, idempotency race, audit/redaction, rollback cleanup, 30/30 targeted va controlled full 376/376. Phan biet rerun voi inspected evidence; khong rerun gate con content/environment khop neu khong co risk moi. Neu co finding, ghi ID/severity/file-line/trigger-impact/AC/closure. Neu tat ca gate dat, append review round va chi reviewer moi cap nhat P2-04 thanh Done. Khong commit/merge/push/deploy.
```

## Codex acceptance review - Round 1 - 2026-09-19T16:36:23+07:00

- Reviewer: independent Codex Reviewer task `/root/p204_independent_acceptance`; this task/session did not author the submitted implementation or tests.
- Reviewed artifacts: branch `huy`, HEAD/baseline `ec9572507eb63b446ded06361dd7cf8d104f134c`, no staged changes, and the exact 21-file working-tree implementation/test manifest. The reviewer recomputed `d19c323ba8016d55c02ad87e24635e18ea9e14583b6f590ab00deb5e773e1f83`, matching the submitted identity. The unrelated modified Person 1 plan and P2-01 review/status metadata were excluded and preserved.
- Metadata handoff: the submission freezes production/tests and explicitly yields this P2-04 review section and P2-04 plan status to the independent reviewer. No active ownership conflict blocks this review write.

### Findings and disposition

- **F-01 - Open - [P1] Parent directory reparse points escape the configured storage root.** Owner: Codex Implementer for Person 2 / P2-04. Location: `RoadGuardSystem.Repositories/Storage/LocalFileContentStore.cs:40-49`, `:141`, and `:267-298`. Trigger: make the configured `objects` directory a directory symbolic link/junction to a same-volume directory outside the configured root, then store an otherwise valid file. `ResolveContainedPath` checks only the lexical path prefix; `StoreAsync` follows the parent link during `File.Move`, while `ResolveObjectPath` checks the final file's reparse attribute but not the parent chain. An isolated Windows probe reproduced `LEXICAL_CONTAINMENT=True`, `OUTSIDE_WRITE=True`, and `FINAL_REPARSE_POINT=False`. Impact: the local store can publish bytes outside its configured private root and later read or compensating-delete them through the same escaped parent, violating the containment/security boundary. Violated criteria: P2-04-AC-04 and the canonical/symlink containment case required by P2-04-AC-08; Data Dictionary 6.4 private storage boundary. Closure: reject or safely contain reparse points for the configured root and all storage parent components (`objects` and `.tmp`) across store/read/cleanup, add a regression that places a directory link/junction in the parent chain and proves no outside write/read/delete, then rerun the targeted real-SQL P2-04 gate and invalidated submission gates. Preserve the opaque-key and no-public-delete contracts.

No additional in-scope code finding was identified in the reviewed entity, mapping, migration/snapshot, repository/idempotency/audit, MIME/checksum/size, SQL immutability, or public-boundary paths. Deeper upload-session/media completeness, malware, project authorization and HTTP behavior remain assigned to P2-30/P1-30 and were not expanded into this review.

### AC coverage and verification

| AC | Reviewer assessment |
|---|---|
| AC-01 | Pass by inspection: P2-01/P2-02/P2-10 are `Done`, dependency artifacts exist at HEAD, and the prior P1 repository hotspot is released. |
| AC-02 | Pass by code/migration/snapshot inspection and the rerun SQL filter: canonical fields, nullable restrictive User FK, indexes/CHECKs and migration lifecycle are present. |
| AC-03 | Pass by inspection and rerun tests: entity/DbContext/SQL trigger prevent update/delete and collision does not overwrite an existing object. |
| AC-04 | **Fail: F-01.** Lexical containment and final-file reparse checks do not stop a reparse point in the root/parent chain. |
| AC-05 | Pass for this slice by code/test inspection and rerun tests: bounded streaming computes server byte count/SHA-256 and detects the declared PDF/PNG/JPEG/MP4/strict UTF-8 text categories; deeper format validation remains out of scope. |
| AC-06 | Pass by inspected two-context race, owner/system, conflict/replay and injected SQL rollback coverage, rerun against real SQL Server. |
| AC-07 | Pass by inspection and rerun SQL evidence: File/audit/idempotency persist together, snapshots are allow-listed, and no HTTP surface was added. |
| AC-08 | **Fail through F-01:** the 30 tests pass, but the required parent-chain symlink containment regression is absent and the isolated probe demonstrates the unguarded behavior. |

Reviewer-rerun checks on Windows 11, PowerShell 7.6.5, .NET SDK `10.0.401`, Docker `29.4.2`; `ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING` was unset, so SQL tests used owned Testcontainers resources rather than a shared database:

- `dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --filter "TaskId=P2-04" --logger "console;verbosity=minimal"`: exit 0; 30 passed, 0 failed, 0 skipped; duration 18 s. The command restored/built the current projects before running.
- Isolated PowerShell directory-link containment probe mirroring `ResolveContainedPath` plus `File.Move`: exit 0; lexical containment `True`, outside write `True`, final object reparse flag `False`. All GUID-named temporary probe paths were removed in `finally`; no repository or retained storage data was touched.
- `git diff --check`: exit 0; only the pre-existing CRLF warnings for the model snapshot and unrelated Person 1 plan were emitted.
- Inspected, not rerun: the submission's matching-manifest restore, non-incremental build, format, controlled full solution 376/376 with zero skips, dependency-security, model-drift, migration-script and documentation/tooling evidence. Those results remain content/environment-matched, but they cannot close F-01 or substitute for its missing regression.
- RED/GREEN chronology was inspected for the recorded schema, final-file symlink, SQL immutability, validation, stream-error and media slices. It contains no parent-directory junction/reparse RED/GREEN evidence; current passing tests cannot reconstruct that missing case.

### Verdict and resume point

- Verdict: **Changes requested**. P2-04 is not `Done` and is not eligible for integration because F-01 leaves AC-04 and AC-08 open. No dependency, product decision or ownership conflict otherwise blocks a bounded fix.
- Resume point: Codex Implementer changes P2-04 back to `In Progress`, fixes F-01 without broadening into P2-30/P1-30, adds negative-first parent-chain reparse regressions for store/read/cleanup, refreshes the 21-file-or-successor manifest and invalidated verification evidence, completes self-review, then resubmits `Ready for review` with F-01 marked fixed awaiting independent verification.

## F-01 fix round - resumed 2026-09-19

- Status: `In Progress`; scope is limited to stable finding F-01 for P2-04 AC-04/AC-08. Baseline remains `ec9572507eb63b446ded06361dd7cf8d104f134c`; Round 1 and all prior evidence remain preserved.
- Verified root cause: `ResolveContainedPath` proves only lexical prefix containment. A directory reparse point at configured root, `objects`, or `.tmp` is followed by `FileStream`/`File.Move`; the final object itself need not carry `ReparsePoint`, so the existing final-file check cannot detect the escape. Store, read and internal compensating cleanup share this parent chain.
- Fix contract: negative-first regressions must replace `objects` and `.tmp` with links to an isolated outside directory and prove no outside write/read/delete. Production must reject reparse points on the configured root and both storage parents before every file-system operation, retain opaque keys/no-public-delete, and return `file_path_invalid`. No P2-30/P1-30 behavior or other finding is in scope.

### F-01 fix evidence and resubmission - 2026-09-19T16:50:08+07:00

- Status: `Ready for review`; F-01 is **Fixed awaiting independent verification**. Submitted production/tests are frozen again after this section; only review/status metadata is yielded to the independent reviewer.
- Root-cause correction: `LocalFileContentStore` rejects a configured root that is already a reparse point, validates root/`objects`/`.tmp` immediately after directory creation, before stream write, again before atomic `File.Move`, and before object read or compensating cleanup. Temporary cleanup also revalidates the parent chain, so it cannot follow a directory link introduced after construction. All reparse rejections use `file_path_invalid`; missing directories remain `file_storage_unavailable`.
- Negative-first regression: four new tests replaced root, `objects`, or `.tmp` with real Windows directory symbolic links to isolated outside targets. Before the fix all four failed because no exception was thrown; the object-parent cases demonstrated store/read/cleanup followed the link. After the fix, all four reject with `file_path_invalid`, outside write directories remain empty, and the outside read/cleanup file remains present. The pre-existing final-file symlink regression also remains green, making the focused symbolic-link filter 5/5.
- Affected checks: `LocalFileContentStoreTests` exit 0, 18/18; P2-04 filter exit 0, 34/34, 0 skipped on real SQL Server; final non-incremental solution build exit 0 (9 projects, 0 warnings/errors); format verification exit 0; final controlled single-SQL full solution exit 0, Unit 124/124 + API 56/56 + Integration 200/200 = 380/380, 0 skipped. The unique SQL container was removed in `finally`.
- Evidence reuse: package/project/mapping/migration/model files did not change in this fix round, so the accepted security scan, migration lifecycle/script, model-drift and documentation/tooling evidence from the submission remains content-valid. `git diff --check` exit 0 with only the existing CRLF warnings for the snapshot and unrelated P1 plan.
- Updated implementation identity: same sorted 21-file path list and hashing algorithm recorded in the first submission; manifest is now `61f4ddbb3194233af547b3b02e886cf31323b132372a28ddc71a6761b1d28322`. Changed implementation hashes are `LocalFileContentStore.cs = 8e4bb6206d522ce9858c535d3fd3af301fd4d771b927cfc89d07d2ed9f74947c` and `LocalFileContentStoreTests.cs = 952762b75cdfd49893d8552f9633bc19a3c62f918ad268e21c319028894dcca4`; the other 19 manifest files are unchanged from `d19c323b...1f83`.
- Self-review: F-01 scope only. Store/read/internal cleanup all invoke the same parent-directory guard; no public delete or HTTP contract was added, opaque key/checksum/MIME behavior is unchanged, and no project/auth/schema/migration/audit/idempotency behavior was modified. No new finding or ownership conflict observed.
- Round 2 reviewer input: baseline `ec9572507eb63b446ded06361dd7cf8d104f134c`, submission manifest `61f4ddbb3194233af547b3b02e886cf31323b132372a28ddc71a6761b1d28322`. Verify F-01 closure using real directory links for root, `.tmp`, and `objects` across store/read/cleanup; rerun the focused regression and affected P2-04 gate, inspect the final full-gate evidence, then preserve F-01 ID and record `Verified` or return a bounded finding. Do not broaden into P2-30/P1-30 or modify implementation.

## Codex acceptance review - Round 2 - 2026-09-19T16:55:57+07:00

- Reviewer: independent Codex Reviewer task `/root/p204_independent_acceptance`; this same reviewer opened F-01 in Round 1 and did not author the fix.
- Reviewed artifacts: branch `huy`, HEAD/baseline `ec9572507eb63b446ded06361dd7cf8d104f134c`, no staged changes, and the frozen 21-file working-tree manifest. The reviewer recomputed `61f4ddbb3194233af547b3b02e886cf31323b132372a28ddc71a6761b1d28322`, matching the resubmission. The only changed implementation/test hashes since Round 1 are the submitted `LocalFileContentStore.cs` and `LocalFileContentStoreTests.cs`; unrelated Person 1 plan and P2-01 metadata remain excluded and preserved.
- Metadata handoff remains explicit and no active dependency, ownership or product-decision conflict blocks acceptance.

### Finding disposition

- **F-01 - Verified - [P1] Parent directory reparse points escape the configured storage root.** `LocalFileContentStore` now applies the shared directory guard to the configured root, `objects`, and `.tmp` after creation, before temporary write, before final move, and before object resolution used by read/internal cleanup. Real Windows directory-link regressions cover a linked configured root, linked `.tmp` store, linked `objects` store, and linked `objects` read/cleanup; the outside write targets remain empty and the outside read/cleanup target remains present. The prior final-file symlink regression also remains green. Closure conditions from Round 1 are satisfied without adding a public delete or changing opaque object keys.
- No new in-scope finding or verification gap was identified.

### Reviewer verification and AC closure

- `dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --filter "FullyQualifiedName~LocalFileContentStoreTests&FullyQualifiedName~SymbolicLink" --logger "console;verbosity=minimal"`: exit 0; 5 passed, 0 failed, 0 skipped. This built the current source and ran real directory symbolic links; duration 37 ms.
- `ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING` was confirmed unset, so the affected SQL gate used owned Testcontainers resources rather than a live/shared database.
- `dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --no-build --no-restore --filter "TaskId=P2-04" --logger "console;verbosity=minimal"`: exit 0; 34 passed, 0 failed, 0 skipped; duration 17 s on real SQL Server.
- `dotnet build RoadGuardSystem.slnx --no-restore --no-incremental`: exit 0; 9 projects, 0 warnings, 0 errors.
- `dotnet format RoadGuardSystem.slnx --verify-no-changes --no-restore --verbosity minimal`: exit 0.
- `git diff --check`: exit 0; only the pre-existing CRLF warnings for the model snapshot and unrelated Person 1 plan were emitted.
- Inspected, not rerun: matching-manifest controlled full solution evidence Unit 124/124 + API 56/56 + Integration 200/200 = 380/380 with zero skips and owned-container cleanup; unchanged package/schema/model content keeps the prior security, migration lifecycle/script, model-drift and documentation/tooling evidence valid.
- Negative-first chronology inspected: the four parent-link regressions failed 4/4 for the demonstrated Round 1 cause before implementation, then the focused link suite passed 5/5 after the fix. This closes the missing AC-08 regression evidence.
- AC-04 and AC-08 are now Pass. AC-01 through AC-03 and AC-05 through AC-07 retain the verified Round 1 assessments because their 19 manifest files and covered behavior did not change; the fresh 34/34 affected gate remained green.

### Verdict

- Verdict: **Done**. F-01 is Verified, AC-01 through AC-08, dependencies, required evidence, implementer self-review and conflict checks pass for manifest `61f4ddbb...8322`.
- Acceptance is bound to this reviewed working-tree content. `Done` does not mean committed, merged, pushed or deployed; none of those actions was performed by this review.
