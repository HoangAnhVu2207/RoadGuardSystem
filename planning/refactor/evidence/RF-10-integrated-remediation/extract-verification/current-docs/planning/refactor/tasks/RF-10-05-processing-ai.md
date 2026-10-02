# RF-10-05: Processing, AI callback and validation

> Current execution 2026-10-01: Anh alone owns R/C refactor on `anh`. F/G is unassigned for later development; proposed A/B text below is historical only. Future identity after a common baseline is Huy/A (`huy`), Anh/B (`anh`). See `../10-refactor-slices.md` and `../11-development-plan.md`.

- **Status/checkpoint:** Parent Partial. Local C01 reachable-state characterization Done on `anh` at dirty HEAD `2efc8a5`; see `planning/refactor/reports/RF-10-05-C01.md`. CG11 late-A fencing is unresolved because no production transition to `RetryableFailure` was found; adapter/protocol F/G remains unassigned and unimplemented.
- **Goal:** reconcile R14/CG11, processing jobs/attempts, callback, validation worker and BE/external-AI boundary under Accepted 44.
- **In scope:** BE manifest/outbox dispatch, retry/attempt fencing, result receipt/idempotency, model/source provenance and validation run. **Out of scope:** implementing external AI model/Web/Android, auto-approving defects, assuming Appendix B protocol is accepted.
- **Dependencies/decisions:** RF-10-03 immutable dataset, RF-10-04 verified files, RF-08 outbox baseline, RF-09; Q-RF02-06 provider contract/dispatcher operator and named external AI owner/ETA.
- **Read first:** R14, CG11, decision 44, prompt Appendix B/F as proposal, `ProcessingV2Controller`, Service/Persistence, `ProcessingJob`, `ValidationRunWorker`, outbox registration, `P231ProcessingPersistenceTests`.
- **Likely files:** processing Controller/Service/Repository/entities/config, outbox adapter/worker, AI versioned contract/fixtures, focused ApiTests/IntegrationTests; no external provider repo edits without its owner.
- **Contract/data/consumer effect:** current callback remains during characterization. Approved version needs manifest/result/receipt IDs and active-attempt semantics with AI provider; additive receipt/attempt columns and old-result handling require migration rehearsal. Preserve old results/provenance.
- **Steps:** isolate retry/late callback race -> record actual durable outcome -> define versioned provider protocol with owner -> implement BE adapter behind compatibility path -> validate duplicate/stale/failed provider handling -> hand off fixtures.
- **Verify:** isolated SQL active/latest attempt and replay/transaction tests, API JWT AI-client scope/wrong model/checksum tests, outbox crash/retry/receipt tests with fake provider, external provider contract tests only when available. No deployed-AI success claim from fake.
- **Done when:** approved protocol and adapter pass failure/retry/receipt gates, CG11 resolved or explicitly disproven with evidence, external ownership/consumer status documented; provider-dependent slice Partial if absent.
- **Recovery:** disable new dispatch route and retain queued/old result records; replay from outbox/receipt after repair; schema forward repair/backup rehearsal, never discard immutable AI/source evidence.

## Two-developer delivery supplement

- **Proposed owner:** B. Split dual-owner parent tasks into single-owner slices before edits. See [ownership plan](../03-two-developer-plan.md).
- **Pre-edit checkpoint:** record exact allowed files/symbols, read-only dependencies, shared-file reservation, baseline commit and preserved dirty changes. Record START / INTEGRATE / RELEASE gates for this slice; existing decision/data gates remain in force.
- **Independence:** use an agreed interface/version and fixture for consumer work; label test-double evidence separately. Do not claim parent Done before required real integration.
- **Self-review:** correctness pass plus ownership/consumer impact pass, safe in-scope autofix, focused verification and final diff review. Record unresolved findings.
- **Cross-owner impact:** create a note from [coordination template](../templates/coordination-note.md), recommend primary fixer and wait for the two developers to assign the overlapping change. Continue independent work; do not edit the other owner's files or duplicate their business logic.
- **Report:** [revised seven-section template](../templates/task-report.md); single-owner task may retain its existing report path, slices use `reports/<TASK-ID>-<SLICE>-<A|B>.md`. Record implementation status separately from delivery stage.
