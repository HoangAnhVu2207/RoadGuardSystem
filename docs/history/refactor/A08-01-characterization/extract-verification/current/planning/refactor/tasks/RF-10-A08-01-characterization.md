# RF-10-A08-01: AI callback identity characterization

Assigned by owner prompt 2026-10-02; sole writer Anh/Codex on dirty `anh`, HEAD `2efc8a5775f834c7f0fe37cc0ce703011649e1f1`. This bounded task belongs primarily to [RF-10-05](RF-10-05-processing-ai.md); A08-01 originated in the [RF-10-08 assessment](../reports/RF-10-08-assessment.md) of per-command receipts. It is neither R01 nor F01 implementation. Parent RF-10/overall/RF-11 remain Partial.

Scope: characterize current callback identity and fingerprint through existing HTTP endpoint and isolated Testcontainers SQL, document compatibility questions, correct inspection checkpoint reference. No production fix or shared fixture change. AI provider/real storage/deployed DB are excluded; A08-02/A09-01/A09-02/F/G and CG11/RF10-R07 remain separate.

Allowlist: new `tests/RoadGuardSystem.ApiTests/Processing/A0801CallbackIdentityCharacterizationTests.cs`; this task note; `../reports/RF-10-A08-01-characterization.md`; `../evidence/A08-01-characterization/**`; existing `../reports/RF-10-07-C01.md` and `../evidence/post-checkpoint13-assessment/findings-ledger.md`. All other source/configuration/tests/fixtures/contracts/schema/migrations/CI read-only. Real before bytes and baseline source hashes are captured by the evidence script before test/doc edits.

START: active manifest/task definitions/ledger and full callback path inspected; fixture owned SQL and current AI JWT scheme available. INTEGRATE: fresh focused build/test, exact receipt filter, snapshots before and after each claimed operation, two self-review passes, handoff extraction verification. RELEASE: reviewer acceptance pending; deployment/rollout not assigned.

Acceptance: valid first callback and replay; each of six independently changed identities using same key/hash versus fresh-key control; valid changed detections/checksum; separate project scope; upstream validation and authentication controls. Trace and runtime matrix in `../evidence/A08-01-characterization/scope-matrix.md`. Every non-reachable/unexecuted behavior retains NOT_VERIFIED.

Rollback: remove only this task's new test/docs/evidence and restore edited docs from captured before bytes after owner decision; no database or production recovery action. Stop at reviewable ZIP handoff.
