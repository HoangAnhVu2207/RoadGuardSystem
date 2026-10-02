# RF-10 Refactor Checkpoint 10 Handoff

- Branch `anh`; base HEAD `2efc8a5775f834c7f0fe37cc0ce703011649e1f1`; no commit made. Existing dirty work was preserved.
- Scope is the `10-03-C02` correction after checkpoint 09. Only the characterization test and planning/report files changed; no production, contract, schema, migration, CI or shared database change was made.
- F1: stale reassign has an immediate fresh SQL snapshot before replay; task identity/project/status/version, assignment IDs/operators and audit IDs are unchanged.
- F2: old replay reads the actual request row and project request-id set, plus assignment/audit IDs.
- F3: old create, V2 create, stale reassign and reassign replay query the real `IdempotencyRecords` mechanism by actor/project/operation/key. Stale receipt creation is recorded as observed, not generalized.
- F4: V2 create replay asserts the stored response projection: task ID/project, original operator A, `NEW_ASSIGNED`, and original version; SQL still shows the task reassigned to operator B.
- F5: authorization wording distinguishes assigned operator GET 200, Reporter GET 403 and manager A wrong-project POST 403; no POST claim is used as wrong-project GET evidence.
- Outbox: the C02 `SurveyV2Controller` -> `SurveyV2Service` -> `SurveyV2PersistenceService.MutateTaskAsync` chain has no enqueue. `SurveyAssignmentPersistenceService` is a separate path with outbox behavior and is not runtime evidence for C02.
- Final build: API test project, exit 0, 0 errors and 0 warnings observed in that run. Final focused test: 1 executed, 1 passed, 0 failed, 0 skipped.
- Final evidence uses `rf1003-c02-checkpoint10-build-console-final.txt`, `rf1003-c02-checkpoint10-test-console-final4.txt`, `rf1003-c02-checkpoint10-test-final4.trx`, run metadata, and before/after source hashes.
- Checkpoint 08's missing pre-edit snapshot remains a historical limitation and is not reconstructed. Checkpoint 09 ZIP is retained unchanged with SHA-256 `CB31A12BCA172C334BE2ED7B38924619BCC7AF1AE38D5C0C0BD3469B05DC8427`.
- The two failed checkpoint-10 runs are included in correction-01 with their original TRX, raw console and exit-1 metadata; no failure artifact was recreated.
- Patch applicability remains `NOT_VERIFIED` against an isolated historical baseline. `10-06-C01` has not started.
