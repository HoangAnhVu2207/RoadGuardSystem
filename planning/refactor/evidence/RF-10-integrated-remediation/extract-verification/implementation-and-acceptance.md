# Implementation summary and acceptance matrix

## Implemented change

`RoadGuardSystem.Repositories/Implementations/Processing/ProcessingV2PersistenceService.cs`, `CreateValidationAsync`: replay maps to `Replayed` only for stored success; stored `Conflict` and `InvalidInput` remain unchanged. This is additive control-flow correction with no schema, wire, receipt-key, or migration change.

## Future A08-01 implementation scope after approval

`ProcessingV2PersistenceService.ReceiveResultAsync` (canonical v2 bytes and legacy branch), `IdempotencyRecord`/configuration only if version persistence is approved, provider adapter contract/caller owned by its external owner, and focused HTTP+SQL tests. Preserve characterization test as pre-fix evidence; add corrected tests alongside it.

## Acceptance matrix

| Case | Setup | HTTP expectation | Receipt/effects | Evidence |
|---|---|---|---|---|
| New exact retry | same route/payload/key | same 200 body/ETag | one success receipt; no new rows | HTTP + fresh SQL snapshots |
| Each identity field changed with same key | valid request, one of Job/Attempt/Manifest/Model/Mode/File changed | 409 duplicate | no business effect; rejection receipt only if policy says so | six independent cases |
| Detections/checksum changed | valid structure, same key | 409 duplicate | no extra detection/job/outbox | SQL snapshot + receipt |
| Project isolation | same key/fingerprint, other project | independent 200 | separate scoped receipt/effect | two-project SQL snapshot |
| Legacy success retry | stored legacy receipt | exact stored outcome | no duplicate effect | compatibility integration test |
| Legacy rejection retry | stored legacy rejection receipt | same original 409/422 | no effect | HTTP + receipt outcome JSON |
| Legacy identity mismatch/unknown | same legacy key/fingerprint, changed identity | explicit conflict or migration error | no effect | decision-dependent test |
| Concurrent same key | two requests race | winner outcome; loser replay/conflict | one unique receipt/effect | SQL concurrency test |
| A08-02 failure replay | unreleased model or invalid pairs, same key twice | same original 409/422 | one rejection receipt, no run/outbox | focused HTTP + SQL test |
| A09 lease cap | crash after final acquire | owner policy | no stuck ambiguous row | blocked until policy + SQL crash test |
| A09 fencing | stale worker after reacquire | stale completion rejected | only current generation changes row | blocked until token/schema decision |

The existing A08-01 characterization remains unchanged and is explicitly pre-fix evidence.
