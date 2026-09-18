# Negative-First test workflow

Use this sequence for Antigravity implementing/fixing observable behavior. Review/diagnosis does not implement fixes; Codex task acceptance may record task review/status under AGENTS. Explicit report-only reviews remain read-only. Human prose: verify references, consistency and discovery, not exact wording. Scripts/configurations: test actual failure modes.

## 1. Negative and edge cases

Choose relevant cases and name expected state/error:

- malformed/empty input, unsupported enum/unit/SRID, invalid JSON and boundary/oversize values;
- wrong role/project, suspended/revoked session and forged/stale claims;
- invalid transition, missing prerequisites, stale version or immutable update;
- repeated key/message, changed payload under the same key and racing workers;
- missing file, failed checksum, timeout/disconnect and retryable versus permanent failure;
- legal hold, deletion-scope drift, wrong geometry version or target count.

Run the negative test before the fix and confirm the intended behavioral failure. Compilation/setup errors or unavailable SQL Server are not intended RED. Record irrelevant cases with reasons; HTTP-200-only checks are insufficient.

Assert no unintended domain/file/outbox effects for rejection. Assert rejection/security audit where policy requires it; do not impose a blanket ban on audit evidence for rejected actions.

## 2. Positive contracts

Add the smallest accepted path and a representative flow. Assert DTO/error contract, state, version, audit and deduplicated effects as applicable. Research expected metrics use hand-calculated fixtures, not the implementation under test.

## 3. Implement the assigned slice

Entity-local invariants live in BusinessObjects; orchestration/cross-aggregate decisions in Services. Controllers and EF mappings do not own workflow policy. Preserve scope, immutable content/source versions and exclusive ownership.

## 4. Verify and self-review

Run narrow tests, affected suites and required format/build checks. Fix the first real failure and rerun affected checks; do not weaken requirements or discard another person's changes for green. Record command/exit code, chronology, RED/GREEN, skipped checks and environment limits.

| Layer | Observable proof |
|---|---|
| Domain/service | Invalid/accepted transitions, current authorization, cross-aggregate gates, audit/retry/version behavior |
| Persistence | SQL constraints, rollback, spatial/concurrency and migration recovery |
| API | Accepted request plus relevant forbidden/invalid transition; stable errors and DTO shape |
| Worker | Duplicate delivery, stale lease/result, cancellation/retry without duplicate effects |
| Tool/config | Invalid input/discovery/connection fails clearly; valid config operates as documented |
| Prose/skill | Resolvable references and correct scenario decisions; no invented failure/runtime claim |

Antigravity owner self-review is mandatory and ends at Ready for review. Mandatory Codex acceptance then verifies the exact submission, required checks and findings before Codex alone marks Done. Fix rounds retain finding IDs and acceptance scope; Antigravity never substitutes self-review for Codex acceptance. Follow the [handoff contract](antigravity-handoff.md).
