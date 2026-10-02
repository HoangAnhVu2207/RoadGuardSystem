# Two-pass self-review record

2026-10-02, sole writer. Peer review PENDING, no delegated or simulated peer approval. Source implementation review and documentation/package verification are separate from reused runtime evidence.

## Pass 1: correctness and acceptance scope

Reviewed current primitive and all 25 caller sites, upstream service/controller/DI paths, notification queries/receipts/lease entities, and specialized validation worker. Compared matrices to actual fingerprint bytes, nullable receipt scope, outcome envelopes and failed-status remap, order of role/input/facts/replay/version, exception rollback, storage compensation and uncertain commit recovery. Reviewed current two C01 test files against reused assertion claims.

Corrections: project Create probes replay before facts on a miss; V2 Task fingerprint is JSON-serialized pipe string, not anonymous object; 14 source files/13 classes; MarkRead final snapshot occurs after replay+conflict; stored equality does not instrument handler skips; notification read missing headers428/default25/descending less-than cursor/correlation/payload/delay fixed; no blanket dispatcher absence because ValidationRunWorker uses direct outbox. P07 complete-production-provenance claim removed; survey plan evidence no longer asserts task reassignment. No arbitrary3+ extraction threshold.

Classify A08-01/02 and A09-01/02 as SOURCE_INSPECTED risks/inferences awaiting separate behavior tasks, not verified deployed defects. Every recommendation points to matrix path/symbol/behavior and future gate. RETAIN does not close F/G or create task acceptance.

## Pass 2: coordination, preservation and package

Checked allowlist against actual edits and 1000 baseline fingerprints; preserve all existing source/test/fixture/schema/contract/CI data and dirty work. No branch/Git mutation, runtime rerun, database/provider operation, refactor abstraction or migration. Existing archives/staging docs remain unmodified; explicit ID/version mapping prevents reused finding strings from erasing original meaning. Survey checkpoint08, inspection01 and inspection02 limits remain separate; failed evidence indexed and retained.

Reviewed all before/current content diffs, report seven sections, source path existence, current Markdown references, script syntax and package path/manifest rules. Script generates hashes from actual bytes, includes script/inventory/summary and nested historical manifests, excludes only root manifest. Extract verification uses independent extracted bytes, entry duplicates and unlisted checks, plus read-only preservation. Final numerical outcomes are in generated checks/static-checks.json, current-state.json and external archive-verification.json; review is limited to those static checks, no new runtime PASS.

Documentation in-scope corrections delivered for review. Historical runtime limitations remain open. Reviewer approval/integration/provider execution NOT VERIFIED. No claim of refactor complete locally.
