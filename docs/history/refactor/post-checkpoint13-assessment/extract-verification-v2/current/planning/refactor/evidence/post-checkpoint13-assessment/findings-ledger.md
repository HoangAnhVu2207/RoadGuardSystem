# Continuing findings and historical limitation ledger

2026-10-02. This ledger supersedes late checkpoint-13 documentation claims; old bytes/archives remain HISTORICAL. Assertion corrections accepted within the owner's stated bounded characterization do not close provenance/deployment/target-policy gaps. Checkpoint-13 docs correction READY FOR REVIEW, closure PENDING. RF-10/overall and RF-11 Partial. No new runtime tests or production fixes.

## IDs reused historically: explicit mapping

The same F-C13 strings were reused for different issues. Always qualify by artifact/version; this assessment assigns stable history/assessment keys below, rather than pretending one finding changed meaning.

| Printed ID | correction-01 findings matrix | correction-02 findings matrix | correction-03/04/finalization late meaning | Stable mapping / current handling |
|---|---|---|---|---|
| F-C13-01a | Upload initial PENDING vs expected ACTIVE | Upload status mismatch fixed | Upload create receipt/scoped count gap | H13-C01-01a/H13-C02-01a status; L13-UploadReplay late assertion, correction-04 reused. Distinct issues |
| F-C13-01b | Actor isolation needed valid membership | Actor isolation source inspected | Survey plan create receipt/scoped count gap | H13-ActorScope historical; L13-PlanReplay late. No equivalence |
| F-C13-01c | Revoked role replay expected201 but got403 | Updated to new JWT403 | New JWT authorization, old JWT NOT_VERIFIED | H13-NewJwt same topic; L13-OldJwt open limit. Atomic role change then re-login; not token revocation |
| F-C13-02 | Response stream disposal fixed, then invalid postpone date surfaced | Postpone date validation correction | Postpone receipt baseline timing/immutability | H13-Stream and H13-PostponeDate; L13-PostponeReceipt late. Do not rename the date issue as receipt issue |
| F-C13-03 | SurveyAssignmentData missing EF mapping | Seed switched to actual SurveyAssignment | Consumer replay fresh context/candidate ID | H13-AssignmentSeed historical; L13-ConsumerReplay correction-03 reused. Different issues |
| F-C13-04 | Inbox GET projection | Project scope guard source inspected | MarkRead first-success vs final chain state | H13-InboxProjection, H13-ProjectGuard and L13-MarkRead distinct. Late correction attributed to correction-03 |

Sources: checkpoint13-integration/correction-01-findings-matrix.md, correction-02-findings-matrix.md, correction-03-handoff/FINDINGS-MATRIX.md, correction-04-handoff/documentation/FINDINGS-MATRIX.md and finalization-handoff/documentation/FINDINGS-LEDGER.md. Preserved historical copies/reference archives; late apparent 6/6 closure does not close original IDs or historical evidence gaps globally.

## Accepted bounded assertions and precise limits

| Stable ID | Evidence / behavior | Status / limit |
|---|---|---|
| L13-UploadReplay | IdempotencyPerCommandCharacterizationTests.UploadCreate_SameKeySamePayload_ReplaysWithIdenticalOutcome; receipt response, actor/purpose session count and project/actor file scope | Accepted bounded assertion; correction-04 10/10 reused. No whole DB immutability |
| L13-PlanReplay | SurveyPlanCreate_SameKeySamePayload_ReplaysWithIdenticalOutcome; receipt and project+Planned plan count, plan-scoped rows | Accepted bounded assertion; correction-04 reused. Filtered scenario only |
| H13-NewJwt / L13-OldJwt | ProjectCreate_ActorRoleRevokedAfterCreate_ReplayReturnsStoredOutcome actually asserts403 after ChangeUserRoleAtomicAsync then AuthenticateAsync | New-token assertion accepted; old token validity/revocation NOT_VERIFIED and not a new-test blocker |
| L13-PostponeReceipt | SurveyPlanPostpone_StaleVersionThenReplay_ReturnsStoredPreconditionFailure; actor/null/SurveyPlanV2Postponed/key BEFORE replay baseline, OperationId/RequestFingerprint/OutcomeJson unchanged, count0/version3 preserved | Runtime receipt/state assertion accepted. Handler skipping SOURCE_INSPECTED from MapExisting before callback; callback version guard inside. Runtime equality alone cannot prove skip |
| L13-ConsumerReplay | OutboxConsumer_Replay_ReturnsDurableNotificationWithoutDuplicate; fresh context/different candidate, stored ID/candidate absent/one consumer+message receipt | Accepted correction-03 reused5/5; direct boundary, no automatic delivery |
| L13-MarkRead | MarkRead_RecipientVersionIdempotency_EnforcesConstraintsAndReplays; first-success snapshot vs snapshot after replay + conflict | Accepted correction-03 reused5/5; no individual snapshot after each operation; tests query actor/operation/key, source null project |

## Open findings and limitations

| ID / severity | Path + symbol or artifact / evidence type | Impact and exact remaining gate |
|---|---|---|
| A08-01 / High, source risk | RoadGuardSystem.Repositories/Implementations/Processing/ProcessingV2PersistenceService.cs: ReceiveResultAsync hashes DetectionsJson+Checksum only; scope actor null/project/operation/key | Different job/attempt/manifest/model/mode/raw file within same project/key and identical hash can reach MapExisting before callback validation. SOURCE_INSPECTED inference, NOT runtime-tested/exploit/deployed claim. Proposed RF-10-05 callback identity follow-up: agree fingerprint/version/key compatibility, trace provider key uniqueness, isolated replay with changed identifying field; no production fix here |
| A08-02 / Medium, source behavior risk | Same file CreateValidationAsync maps ANY primitive Replayed to ProcessingJobPersistenceStatus.Replayed; service maps Replayed, controller CreateValidationRun accepts only non-null Job | Stored model-release Conflict/null can first map HTTP409, then replay Replayed/null reaches controller fallback422. SOURCE_INSPECTED path inference, NOT fresh HTTP proof. Proposed separate validation replay outcome task; decide retain rejection status, characterize failed-outcome replay before fix |
| A09-01 / Medium, source risk | RoadGuardSystem.Repositories/Implementations/Messaging/OutboxWorkRepository.cs: TryLeaseNextAsync excludes attempt >= cap; OutboxMessage.AcquireLease increments; only ScheduleRetry sets DeadLetter | Crash after final lease leaves Leased at cap; expiry alone does not make it eligible or DeadLetter through these methods. No scheduled caller/deployed stuck-row claim. Proposed lease exhaustion recovery task: agree crash terminal policy and isolate SQL crash/restart/cap assertions |
| A09-02 / Medium, source risk | RoadGuardSystem.BusinessObjects/Messaging/OutboxMessage.cs: EnsureLeaseOwner checks status/string owner, no expiry or lease token; CompleteAsync/RetryAsync accept owner string | Same owner reference reused after reacquisition cannot distinguish prior lease generation; expired owner can complete before reacquire. Deployment owner uniqueness and desired expiry semantics UNKNOWN. Proposed fencing policy/lease generation follow-up, characterize old/new worker ownership; no implementation |
| P13-C04-Linkage / provenance | correction-04-tests-before.sha256 6f0bebf1... vs after a7bbae78...; missing assembly hash | Before-build-to-test/source-to-binary linkage NOT_VERIFIED; archive payload matches after bytes only. Preserve permanently for historical run; no reconstruction/rerun to erase |
| P13-C02-Hashes / provenance | checkpoint-13 correction-02 source snapshot history contains placeholder before hashes | Historical limitation retained; later passing TRX cannot repair original chain |
| P03-C08-Snapshot / provenance | reports/RF-10-03-C02.md checkpoint08 correction | Pre-first-edit before-snapshots absent. Separate from inspection provenance; later checkpoint10 proof does not retroactively supply bytes |
| P07-C01-Linkage / provenance | RF-10-07-C01 report, checkpoint11 correction01 metadata mismatch | Inspection correction01 source-to-binary NOT_VERIFIED. Separate from survey checkpoint08 |
| P07-C02-Coverage / provenance | Inspection checkpoint12 correction02 test+two fixtures hashes and assembly | No production fingerprint, approximate build end; not reproducible/full production source chain |
| D13-NotificationDelivery / reachability | notification-matrix.md and checks/outbox-reachability.json | Notification consumer/work repo registration but no caller in searched production scope; specialized validation worker exists. Actual scheduler/provider/deployed backlog/timing UNKNOWN/NOT_VERIFIED |
| D13-DirectProof / coverage | correction03 five focused C01 cases | Producer and consumer separate direct repository tests, not automatic end-to-end chain; no lease/retry fresh runtime; malformed cursor/performance/external clients unverified |
| H13-PackageCoverage / delivery | Prior finalization archive actual31 files, old manifest27 entries; self manifest excluded and generator/inventory/summary unlisted | Old archive remains unchanged/HISTORICAL. New package lists every payload including scripts/inventory/summary/old nested manifests, excludes only root MANIFEST.json. Fresh extract checks decide new package validity |
| H07-AssertionLimits / coverage | Inspection baseline/report | Only asserted success-GET fields; denied/filtered GET/audit/outbox immutability, exact version decode, trigger/SRID rejection runtime, cursor/concurrency/external/deployed remain NOT_VERIFIED |
| H03-Policy / domain findings | RF-10-03-C02 F01-F07; survey coexistence C01 F04-F06 | Old/V2 shared rows/projection/errors/assignment and stored create-time replay remain current evidence, not target policy. Q-RF02-03/05 and consumers gate F/backfill; no closure from checkpoint13 |
| H10-Parent / workflow | RF-10-refactor report, slices/checklist and RF-11 stage | Remaining R/C final aggregate checks and F/G external/decision/release limits retained. Q-RF02-04/07 and retention Q-RF02-08 not opened; RF-10/overall/RF-11 never Done |

Earlier production findings (including upload overflow RF10-R03/CG17 and AI late-attempt RF10-R07/CG11) retain their IDs/status in full current parent report supplied as reference; this assessment does not reassign them or infer closure. Historical failed TRX/build logs are kept in repository and mapped by generated checks/historical-evidence-index.json. No evidence deletion or reclassification of failure as success.

## Documentation correction closure versus review

All ten requested docs/package corrections have artifacts here: full Phase A before/current docs; divergent C04 before/after+missing assembly; consumer/MarkRead correction03 attribution; final-chain MarkRead wording; separate SOURCE_INSPECTED skip-handler; version-qualified finding mapping; scoped dispatcher search with actual validation worker; three separate survey/inspection limitations; preserved failed evidence/index; complete generated new manifest. These corrections are delivered, not reviewer-approved checkpoint closure. No new implementation task is created.
