# HUY-02: existing inspection/inbox authority and inspection retention adapter

Status: ASSIGNED; overall PARTIAL. Assignment: Huy's finite autonomous implementation request, 2026-10-04. Writer: Huy/Codex LOCAL, `huy-review` only. Initial and implementation base: `5a6d4c3b1957c0142c4d4d83679c11066ca1cdb7` (parent `21cf8f7de6fea1bf419dadbfac39476d11474a07`); initial dirty/untracked paths: none. Initial live `huy-review` equals base; live `anh-review`: `5089c3267dcdf60645ab34f61b58a79e3cbb0cf6`. These are evidence, not reset/integration targets. HUY-01 closure is not reopened.

## Goal and sources

TARGET_CONFIRMED: the assigned prompt bounds owned work to I1/N1/R1/DOC. Product authority: `docs/product/confirmed-decisions.md` PR-32A/35A/36A/37/41A/42A/43A/44. `workflows.md` and `contracts/events/README.md` remain PROPOSED synthesis/questions, not adopted new routes/schema. Guidance: AGENTS, manifest (skills empty; retired RoadGuard skills not used), four active rules, module map, development README/template. Current implementation sources at base: InspectionTasksController → InspectionTaskQueryService → InspectionTaskReadRepository; NotificationsController → NotificationService → NotificationPersistenceService → IdempotencyOperationService; RetentionContracts/RetentionInventoryRepository/Huy01RetentionInventoryContributor; production inspection model/configurations/migrations and focused tests.

No newer HUY-02 spec or explicit shared ownership transfer found locally. Current code proves implementation, not business authority. No repair/sync/handover routes or tables are assigned.

## Finite gate ledger

| Gate | Existing implementation | Remaining work | Dependency | Acceptance | Category | Status |
|---|---|---|---|---|---|---|
| I1 | Crew task GET, assignment query, membership filter | None in assigned existing read scope | Real auth/migrated owned SQL | Current authority, cursor boundary, no business mutation | OWNED | IMPLEMENTED_VERIFIED |
| N1 | Recipient inbox GET/detail/read, rowversion receipt | None in owned handler/replay scope; shared auth media type below | Existing additive receipt guard | No denied payload/effect/receipt; exact replay/ETag; rollback/concurrency | OWNED | IMPLEMENTED_VERIFIED |
| R1 | HUY contributor always incomplete for repair | None in adapter/repository integration scope; S1 remains | Existing scoped DbContext/interface | All real session/measurement references, deterministic versions, no writes | OWNED | IMPLEMENTED_VERIFIED |
| DOC | Assignment and current sources | Commit/push verification recorded in delivery checkpoint | Focused results | Accurate finite status/limitations | OWNED | IMPLEMENTED_VERIFIED |
| S1 | Production composite registration | Anh registers additive contributor, retains HUY signal | Anh file slot/order and integration evidence | Actual production DI composition | SHARED_INTEGRATION_REQUIRED | PENDING_ANH |
| S2 | Cookie eligibility excludes inspection/inbox | Anh binds exact existing routes + adopted contracts/Postman | Anh file slot/order/canonical adoption evidence | Bearer precedence, no fallback, mixed actor rejection, cookie POST CSRF | SHARED_INTEGRATION_REQUIRED | PENDING_ANH |
| B1 | Existing task/session/measurement SQL; FIELD returns source_not_ready | Adopt mutation and FIELD producer semantics | D1 and D5 interface facts | Agreed actor/state/evidence/result contract | BLOCKED_BY_MISSING_BUSINESS_DECISION | BLOCKED |
| B2 | Repair inventory unavailable | Adopt repair item/package/attempt lifecycle | D2 | Agreed approval/rework/closure | BLOCKED_BY_MISSING_BUSINESS_DECISION | BLOCKED |
| B3 | PR-35A temporary safety requirement | Adopt eligibility/policy/material/method | D2/D4 | Temporary action never substitutes closure | BLOCKED_BY_MISSING_BUSINESS_DECISION | BLOCKED |
| B4 | Inbox consumer receipt is not fanout | Adopt dispatch/reminder producers | D4 | Occurrence/recipient/timezone/retry policy | BLOCKED_BY_MISSING_BUSINESS_DECISION | BLOCKED |
| B5 | No adopted sync intake | Adopt envelope/key/order/conflicts | D3 | Current reconnect authority, no local data loss | BLOCKED_BY_MISSING_BUSINESS_DECISION | BLOCKED |
| B6 | Construction HandoverDocument is unrelated | Adopt encrypted device handover | D3 | Supervisor grant, same-project PM, original actor, crypto/import/replay | BLOCKED_BY_MISSING_BUSINESS_DECISION | BLOCKED |
| X1 | No real external validation in this assignment | External consumer/provider/device/deployment checks | External owners/environment | Live interoperability evidence | EXTERNAL_ENVIRONMENT_ONLY | NOT_VERIFIED |

## Behavior and data

I1 preserves GET `/api/v1/me/inspection-tasks?cursor=&limit=` (50 default, 1..100); current active Crew, active assignment and current matching-role membership/date interval. Auth 401, non-Crew 403, invalid input/cursor 400; inaccessible rows filtered after query. Projection: id/projectId/single defectIds/MEASURE_ONLY/crewId/null policyVersionId/uppercase enum (including NEWASSIGNED)/base64 rowversion. Cursor remains base64 UTC ticks|GUID ordered DueAt/Id; hidden rows may yield short/empty pages with advancing cursor. No filled-page or snapshot promise, inspection mutation APIs or FIELD readiness bypass.

N1 preserves GET notifications (25 default, max100; cursor/items/nextCursor/asOf), own detail + quoted base64 ETag, POST `{id}/read` with Idempotency-Key/If-Match and no body. DTO id/message/resourceId/read/occurredAt/version; 200 success/exact replay, 428 missing header validation_error, 400 invalid input, 404 notification_not_found, 409 duplicate_request, 412 notification_concurrency_conflict. Transaction-time principal denial maps 401 auth_unauthorized/problem+json with current correlation convention. Request-time shared auth keeps its existing denial semantics.

Handler and receipt guard use fresh same-context user → role → notification UPDLOCK/HOLDLOCK reads in engine-owned transaction. Active user/role, no must-change restriction, authenticated role snapshot where supplied, recipient relation required. Existing positional service/repository overloads remain available; additive overload carries role from controller. Missing/foreign recipient ordinary commands retain stored not-found semantics; receipt guard denial is never stored. Missing notification cannot replay a successful payload. Current-principal checks precede If-Match/conflict; exact success replay does not revalidate old If-Match. First ReadAt, saved rowversion/body, receipt commit atomically. No nested transaction, audit/outbox/notification producer added. Session revoke/expiry validation is request-boundary only, not claimed transaction-safe. Inbox project-revocation policy remains undecided; recipient-only visibility preserved.

R1 uses only actual `FieldInspectionSessions.EvidenceFileId` and `GroundTruthMeasurements.EvidenceFileId`; project derives from session. Separate Name HUY02_INSPECTION. Reference facts retain session/measurement/purpose/task/defect/survey/road-version provenance including draft/research/immutable history; no FileScope requirement. Stable kind + SHA256 JSON persisted fact versions, sorted references/files; unresolved scope is incomplete, never empty-complete. Covered-table empty is not complete repair inventory. Same scoped DbContext/caller transaction, no writes or deletion/hold worker. No entity/configuration/migration/snapshot changes.

## Ownership and exact shared handoff (D5)

Owned files: notification controller/service/repository/interfaces/status enums, inspection/notification module tests, new `Repositories/Implementations/Retention/Huy02InspectionRetentionContributor.cs`, this spec. Global fixtures, generic idempotency/outbox/auth, DbContext/model/configurations/migrations/snapshot, Program/shared roots/options, canonical contracts and integrated Postman are reserved to **Anh**. HUY-01 transfer does not apply.

CURRENT_VERIFIED: desktop chat inventory exposes no identifiable Anh implementation chat to coordinate with. No shared slot/order/adoption evidence is available; no message sent to an unrelated chat. Handoff here is ready for Anh; S gates alone wait. Proposed integration order: Huy owned code/test checkpoint → Anh review interface facts → Anh reserves/writes S1/S2 → focused production composition/cookie tests → canonical adoption/Postman update preserving IDs. No automatic merge of Anh tip.

S1 exact delta: in shared production registration `RoadGuardSystem.Repositories/Extensions/Huy01ReporterPersistenceExtensions.cs`, `AddHuy01ReporterPersistence` currently registers HUY at lines 31–32 and real composite at 33–34. Anh reserves this composition slot (or a named shared composition successor) and adds `services.TryAddEnumerable(ServiceDescriptor.Scoped<IRetentionInventoryContributor, Huy02InspectionRetentionContributor>());` without removing existing registration. No Huy module registration extension is needed for this one additive descriptor. Keep Name=HUY/HUY_REPAIR_REFERENCE_UNAVAILABLE. Tests manually compose real repository and both contributors; this is repository integration, not production DI evidence.

S2 exact symbol: `WebCookieConfiguration.IsCookieEligiblePath`. Add only `/api/v1/me/inspection-tasks`, `/api/v1/notifications`, `/api/v1/notifications/{guid}`, `/api/v1/notifications/{guid}/read` (normal route trailing slash behavior to be checked by Anh), preserving Authorization precedence/invalid bearer rejection/mixed actor rejection/CSRF/expiry/idle/activity. No broad substring path match or auth redesign. Cookie tests run only after that symbol changes. Bearer remains supported; Web binding pending. Canonical contract/Postman route adoption must be evidenced separately, not inferred from an old V2 file.

CURRENT_VERIFIED S2 compatibility finding: shared `JwtBearerConfiguration.WriteChallengeAsync` sets problem+json then calls `WriteAsJsonAsync`, which overwrites it to application/json. Real revoked-session HTTP reproduced this. Transaction-time Huy denial returns 401/auth_unauthorized/application/problem+json and is verified. Request-boundary revoke/expiry returns 401/auth_session_revoked with existing shared application/json; this does **not** meet target problem media type. Anh must reserve/correct the shared challenge writer/content-type call as part of S2 and rerun exact bearer/cookie checks. No shared-auth change was made here; do not promote the boundary tests' status/code assertions to media-type compliance.

D5 also blocks B1/B2/B3/B5/B6 where dependent: Crew evidence admission needs agreed inspection/repair purpose/scope, ownership/verified-file/version interface from Anh; Reporter-private resolver cannot supply it. FIELD producer/result consumer facts and version need adoption. No new evidence admission/producer is invented.

## Missing business decisions (one batch)

Asked once asynchronously in this chat on 2026-10-04; no response is approval. D1: inspection/FIELD purpose, actor, task/survey/state/evidence/result contract; existing task requires Defect/Survey/RoadVersion, DefectVerification session task/survey/inspector, SQL defect Open, immutable submitted history. General Reporter/no-survey/Verified-repair measurement cannot use fake IDs or arbitrary Completed session.

D2: repair item/package/attempt lifecycle, approval/rework/closure; Fast Track eligibility/policy/thresholds/materials/methods. PR-35A accepts limited PM temporary safety + Supervisor notification only.

D3: sync envelope/device/key/ordering/atomicity/cursor/stale conflict; encrypted handover grant after actor revocation, crypto/manifest/import/replay. Preserve PR-37 local data on expiry, PR-43A downloaded geometry/task, PR-42A Supervisor-approved accessible-device export/same-project receiving PM/original actor/no org recovery key.

D4: notification occurrence/revision, recipients, weekly timezone/window, retries/poison/delivery and project-revoked inbox history. Current `(messageId,notification-inbox)` receipt is not fanout; recipient/source/event uniqueness lacks occurrence; do not lease unsupported events or invent email/SMS/push.

## Verification and reviews

Initial API build exit 0 (no warning on incremental baseline). Red SQL run: `dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --filter FullyQualifiedName~Huy02NotificationAuthorityTests --nologo --logger "trx;LogFileName=huy02-notification-red.trx"`: exit 1; 2 executed/2 failed/0 passed/0 skipped, expected inactive recipient incorrectly Success/Replayed. Owned Testcontainers SQL/production migrations verified, no shared database.

Intermediate build caught CA1068 for appended cancellation parameter; fixed by additive overloads preserving original signatures. Fresh affected API build exit 0; pre-existing analyzer warnings present on recompilation. Final commands/counts, both actual self-reviews and checkpoint to be recorded after implementation. External review PENDING. No external/deployment claim.

### Fresh executed verification (2026-10-04)

All SQL tests use owned disposable Testcontainers SQL Server and production migrations/mapping (Authentication fixture uses IMigrator; Identity fixture Database.Migrate). Docker server available, .NET SDK 10.0.401 running net8 projects. No InMemory/SQLite/EnsureCreated production substitute, shared DB/seeder writes, external storage/device/provider calls, package upgrade or schema drift introduced. `CallerTransactionSeesAddedReferencesAndRollbackRemovesThem` executes `Database.HasPendingModelChanges()==false` on migrated production context. No model/configuration/migration/snapshot files changed.

Commands below use `dotnet test <project> --filter <filter> --nologo --logger "trx;LogFileName=<artifact>"`, except guard suite additionally `--no-build`. Project aliases are U=`tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj`, A=`tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj`, I=`tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj`. Artifacts are ignored local `<project-directory>/TestResults/*.trx`; no secrets/raw SQL logs committed. Filters abbreviated below always mean `FullyQualifiedName~<name>` combined with `|`.

| Project / filter | TRX artifact | dotnet exit | Executed / pass / fail / skip | Interpretation |
|---|---|---|---|---|
| I / Huy02NotificationAuthorityTests | huy02-notification-red | 1 | 2 / 0 / 2 / 0 | Expected red: suspended actor could write/replay |
| I / Huy02NotificationAuthorityTests (initial two cases) | huy02-notification-green | 0 | 2 / 2 / 0 / 0 | Original defect corrected |
| A / Huy02InspectionRetentionTests (initial eight cases) | huy02-retention-red | 1 | 8 / 0 / 8 / 0 | Expected red: contributor absent; SQL seeds valid |
| I / Huy02NotificationAuthorityTests\|P207NotificationPersistenceTests\|P240FieldInspectionMeasurementSchemaTests | huy02-integration-final | 1 | 27 / 26 / 1 / 0 | Same-key test wrongly selected a single non-null result; both success and replay correctly have bodies |
| A / Inspections\|Notifications | huy02-api-final | 1 | 42 / 38 / 4 / 0 | Membership test clock accidentally shifted JWT issuance into future; fixed test DI seam |
| U / Inspections\|Notifications | huy02-unit | 0 | 4 / 4 / 0 / 0 | Existing service/query tests |
| I / Huy02NotificationAuthorityTests.ConcurrentSameKeyOrCompetingCommandHasSingleReadEffect | huy02-concurrency-fixed | 0 | 2 / 2 / 0 / 0 | Corrected assertion; same-key and competing stale command verified |
| A / Huy02MembershipChangedAfterLoginFiltersTask | huy02-membership-fixed | 0 | 4 / 4 / 0 / 0 | Real membership repository/guard; controlled clock only at membership seam |
| A / Huy02InspectionRetentionTests\|SessionRevocationOrExpiryRejectsCommittedReplayAtRequestBoundary | huy02-retention-session-final | 1 | 11 / 9 / 2 / 0 | Retention all 9 pass; expiry seed violated date constraint, shared challenge media type differs |
| I / ReceiptAccessGuardSqlTests (`--no-build`) | huy02-engine-guard | 0 | 32 / 32 / 0 / 0 | Reused engine paths: ordinary/conflict/retry/duplicate/ack/recovery/cancellation/locks |
| A / SessionRevocationOrExpiryRejectsCommittedReplayAtRequestBoundary | huy02-session-fixed | 1 | 2 / 0 / 2 / 0 | Test setup attempted write-once IssuedAt; expected code was missing auth_ prefix; corrected without bypass |
| A / SessionRevocationOrExpiryRejectsCommittedReplayAtRequestBoundary | huy02-session-final | 0 | 2 / 2 / 0 / 0 | ExpiresAt=IssuedAt+1 tick respects constraints; revoke/expiry 401, original receipt unchanged; media gap still S2 |
| A / Huy02SameDueTimeHiddenBoundaryAdvancesWithoutLeaksOrBusinessWrites | huy02-inspection-session-snapshot | 0 | 1 / 1 / 0 / 0 | Self-review added actual auth-session snapshot; no business/session mutation |

Early compilation-only attempts executed zero tests: invalid MeasurementType.Depth corrected to DepressionDepth; nullable Defect.ProjectId test filter corrected; retention helper CA1859 corrected to concrete type. Intermediate cancellation-parameter CA1068 build failed and was corrected with additive overloads. Some early shell log-filter wrappers overwrote dotnet's exit status with rg's; failures above are taken from actual dotnet logs/TRX, never called PASS from that wrapper. Later/final wrappers capture the dotnet exit before filtering output.

Final `dotnet build RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj --nologo`: exit 0, 0 warnings/0 errors on incremental final build. Full affected recompilation earlier showed existing repository/test analyzer warnings; not a claim that the entire repo is warning-free. `git diff --check`: exit 0.

Latest evidence by unique test identity (replaced only invalidated tests; no blanket rerun): **108 executed distinct cases, 108 passed, 0 unresolved failures, 0 skipped** = unit 4 + API 45 + integration 27 + shared guard 32. Across all retained TRX attempts: 139 executions, 120 pass/19 fail/0 skip; 10 were intentional red, 9 intermediate development/test failures now resolved (shared media gap remains explicitly S2). These attempt totals are not additional acceptance cases. No independent required validation is environment-blocked. NOT RUN: S1 production registration, S2 cookie binding/auth media-type compliance/integrated contract/Postman, B1–B6 unadopted flows, X1 external/deployment; no synthetic test count assigned to those gates.

### Actual self-review passes

1. Contract/auth/transaction/replay/concurrency/privacy: inspected final notification diff and same-context locking; existing overload/key normalization/fingerprint/operation/DTO/status/ETag preserved. SQL/HTTP verify stale/current authority, exact receipt body, first ReadAt, concurrent same-key/competing command, read+receipt rollback, cancellation, no duplicate inbox/outbox. Replay guard wired to shared engine overload on every engine recovery path (32 reused tests); no engine rewrite or cached authority. Review strengthened inspection auth-session snapshot, corrected test clock/assertion/expiry setup, and identified shared challenge media type for S2. No remaining direct owned finding; session safety remains request-boundary only.
2. Ownership/producer-consumer/compatibility/docs/final diff: verified all 15 paths are owned; shared auth/DI/DbContext/config/model/migration/snapshot/contracts/Postman untouched. Corrected S1 handoff from guessed Program slot to observed repository composition extension. R1 real consumer sees both FKs, projects without FileScope, same-file multiple sessions/projects, draft/research/immutable verification history, deterministic versions, incomplete unresolved provenance, caller transaction/rollback/no writes. HUY repair signal retained. Inspection production passed characterization and needed no rewrite. B/S/X finite ledger and one-batch decisions remain explicit; no invented producer/route/schema/backlog. These are self-reviews, not peer/external approval.

### Changed files and final flow handoff

| Owned file | Change |
|---|---|
| RoadGuardSystem.API/Controllers/NotificationsController.cs | Carry authenticated role; map transaction denial to current 401 problem convention |
| RoadGuardSystem.Repositories/Implementations/Messaging/NotificationPersistenceService.cs | Handler and guarded receipt authority, fresh ordered SQL locks |
| RoadGuardSystem.Repositories/Interfaces/Messaging/INotificationRepository.cs | Additive role overload; original positional API retained |
| RoadGuardSystem.Repositories/Interfaces/Messaging/NotificationMarkReadPersistenceStatus.cs | Add Unauthorized result |
| RoadGuardSystem.Services/Implementations/Messaging/NotificationService.cs | Pass role snapshot, map unauthorized |
| RoadGuardSystem.Services/Interfaces/Messaging/INotificationService.cs | Additive role overload |
| RoadGuardSystem.Services/Interfaces/Messaging/NotificationServiceStatus.cs | Add Unauthorized result |
| RoadGuardSystem.Repositories/Implementations/Retention/Huy02InspectionRetentionContributor.cs | New read-only two-FK provenance inventory |
| tests/RoadGuardSystem.ApiTests/Inspections/V2P1063InspectionTaskListTests.cs | Current membership/auth/assignment/cursor/no-mutation risks |
| tests/RoadGuardSystem.ApiTests/Inspections/Huy02InspectionRetentionTests.cs | Real migrated SQL contributor/composite integration |
| tests/RoadGuardSystem.ApiTests/Notifications/Huy02NotificationApiTests.cs | Real JWT boundary, post-auth authority loss, exact replay and errors |
| tests/RoadGuardSystem.IntegrationTests/Notifications/Huy02NotificationAuthorityTests.cs | Principal/relation/replay/concurrency/rollback/cancellation SQL |
| tests/RoadGuardSystem.IntegrationTests/Notifications/P207NotificationPersistenceTests.cs | Wrong-recipient test now uses an existing active outsider; missing principal separately denied |
| tests/RoadGuardSystem.UnitTests/Notifications/NotificationServiceTests.cs | Test stub implements additive interface overload |
| planning/development/HUY-02.md | Assigned spec/evidence/finite gates/shared handoff/reviews |

| Flow | Status | Implementation | Fresh evidence | Exact blocker |
|---|---|---|---|---|
| Inspection | PARTIAL; I1/R1 verified | Existing read preserved; retention adapter supplied | API inspection/auth/pagination/non-mutation + 9 SQL retention cases/model parity | B1 D1/D5 mutation/FIELD; S1 registration/S2 cookie binding |
| Repair | BLOCKED | No new route/schema; incomplete repair safety retained | Composite still HUY_REPAIR_REFERENCE_UNAVAILABLE | B2 D2/D5 lifecycle/evidence admission |
| Fast Track | BLOCKED | PR-35A requirement preserved; no evaluator/temporary action route invented | Source/decision inspection only, no runtime claim | B3 D2/D4/D5 policy/material/method/event facts |
| Notification | PARTIAL; N1 verified | Existing bearer inbox/read + transaction/replay authority | HTTP exact body/ETag/denials; SQL concurrency/rollback; engine guard paths | B4 D4 producers; S2 cookies/shared JWT challenge media type/canonical integration |
| Offline | BLOCKED | No sync intake invented; no local data deletion | Source/decision inspection only | B5 D3/D5 envelope/order/partiality/conflict/current admission |
| Handover | BLOCKED | Existing construction dossier not reused as encrypted device handover | Source/decision inspection only | B6 D3/D5 grant/crypto/manifest/import/replay |

BE verified under executed isolated conditions. Test-only post-auth authority mutation is deterministic fault injection around real HTTP auth/service/repository/SQL, not an external consumer. No AI/Android/MinIO/provider/device/deployment verification claimed. Business decisions D1–D4 still await one batch; D5 is interface/integration evidence under confirmed Anh ownership, not an ownership question. External review **PENDING**. Independent implementable owned code remaining=0; independent required validation remaining=0; additional independent run needed=0. Overall **PARTIAL**, not full HUY-02 DONE. No HUY-03/new self-assigned run; stop after verified normal push.
