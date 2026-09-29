# P1-09 — Compact agent context and review prompts

## Identity and assignment

- Owner/implementer/self-reviewer: Person 1 / Codex Implementer, current task; branch `anh`, 2026-09-19 (+07:00).
- Baseline: `ec9572507eb63b446ded06361dd7cf8d104f134c`.
- Request: apply the compact workflow proposed in this conversation. Trace: TE-01/10; dependency P1-07 workflow is present in this checkout.
- Status: Ready for review. Done authority: separate Codex Reviewer who did not author these artifacts.
- In scope/exclusive files: `AGENTS.md`, `.antigravity/AGENTS.md`, `docs/prompts/RoadGuard_Task_Workflow.md`, this log and only P1-09 entries in `planning/RoadGuard_Plan_Person_1.md`.
- Out of scope: production/tests/skill/config changes, weakening gates, accepting P1-08/P1-10, commits/integration/publication.
- Conflict warning: existing P1-08 untracked artifacts and plan edits are preserved. The owner's current bounded documentation request is handled alongside that frozen submission; P1-09 owns only its new plan section, with serialized writes. P1-10 is already recorded Done; its history is untouched.

## Acceptance contract and design

| AC | Observable result | Check |
|---|---|---|
| AC-01 | Compact B/C prompts reference canonical policy/evidence instead of repeating it; complete review packet stays in worklog. | Compare prompt lengths and inspect retained role/identity/gate links. |
| AC-02 | Read relevant sections once; summarize output with exit/counts while retaining diagnostic logs; no false pass from truncation. | Scenario self-review and documentation checks. |
| AC-03 | Stale handoff checks current acceptance/content before reopening; changed artifacts or new risk still require applicable review. | Done/changed/missing-evidence scenarios. |
| AC-04 | Independent review, required SQL/security/CI, evidence identity and Git restrictions remain intact; canonical rules match mirror. | Mirror, verifiers, links and scoped diff review. |

No business actor/state/audit/schema change. No runtime or behavior-changing script/config edit: negative-first runtime tests and build/format/SQL suites are N/A. Required checks: existing documentation, setup and planning verifiers; local links, mirror equality, whitespace and scenario self-review. Ready for review requires these checks, exact identity and self-review; Done additionally requires independent acceptance.

## Changes, evidence and self-review

- Files: both AGENTS files (three compact context/output/stale-handoff rules); shared workflow (shorter B/C); this new untracked log; only P1-09 entries in the Person 1 plan. Existing P1-08 edits preserved.
- AC-01: B/C sections reduced from 4,177 to 2,246 characters (46.2%; not a measured token/billing reduction). Full packet remains in worklog; canonical rules remain authoritative.
- AC-02/03/04: six manually inspected scenarios pass: unchanged Done reports historical acceptance; changed identity requires resubmission; missing/stale evidence cannot pass; failed/zero/skipped-required output cannot pass; fix rounds retain findings and rerun regressions; report-only makes no edits. These are prose scenario checks, not executable tests.
- Self-review: acceptance remains independent, all AC/diff coverage and SQL/security/CI gates remain required where applicable. Existing history is preserved; metadata writes are scoped/serialized; logs are local and secret-safe. No new credential or runtime authorization/state/audit/concurrency behavior. No open in-scope findings. Reviewer acceptance remains pending.

Commands run in Windows / PowerShell 7, 2026-09-19 (+07:00); local logs in `%TEMP%/RoadGuard-P1-09-{docs,setup,planning}.log`:

| Command/check | Exit/result |
|---|---|
| `pwsh -NoProfile -File tests/Documentation/Verify-P102Docs.ps1` | 0; documentation contracts passed. |
| `pwsh -NoProfile -File tests/Tooling/Verify-AntigravitySetup.ps1` | 0; setup/discovery/mirror passed; no live calls. |
| `pwsh -NoProfile -File tests/Documentation/Test-P203Planning.ps1` | 0; 9/9 planning scenarios passed. |
| Python mirror bytes/local Markdown links | Initial byte check failed on encoding/line endings only; decoded text matched. Mirror normalized to canonical bytes; final equality/link check passed. |
| `git diff --check` | 0; clean. |

Runtime build/unit/API/SQL/security suites and artificial RED are N/A: prose-only changes, no script/config/runtime implementation. This omission does not waive production gates. No commit/merge/push/deploy; index remains empty.

## Exact submission / review packet

Person 1 / anh / Ready for review; baseline in assignment. Three substantive source files below. Relevant untracked file: this log. Review metadata also includes only P1-09 plan entries; existing P1-08 files/entries are excluded. Source digest excludes mutable plan/worklog bookkeeping, which reviewer must still inspect. Hash raw bytes, sort paths lexically, join `<sha256><two spaces><path>` with LF/no final LF, SHA-256 UTF-8 payload.

| Source | SHA-256 |
|---|---|
| `.antigravity/AGENTS.md` | `1554b687f08188b2e10e135dedef976c638645427a81589d020237ac3fee7e69` |
| `AGENTS.md` | `1554b687f08188b2e10e135dedef976c638645427a81589d020237ac3fee7e69` |
| `docs/prompts/RoadGuard_Task_Workflow.md` | `def61b2c6ddafd59031a22a70af6e72551779fa3a73732956c455d484d1d815f` |

Aggregate: `daed134ebc358650a40f335de589f72cf3150378a9fb40770a5466e99a34108b`.

AC coverage, files, commands, counts, N/A reasons and self-review are above. Addressed finding IDs: none (new task). No blockers; token savings are unmeasured. Frozen at Ready for review; independent reviewer alone can accept.

Ready-to-run prompt C:

```text
Review độc lập P1-09, Person 1, branch anh theo Prompt C trong
docs/prompts/RoadGuard_Task_Workflow.md và AGENTS.md.
Baseline: ec9572507eb63b446ded06361dd7cf8d104f134c.
Submission source SHA-256: daed134ebc358650a40f335de589f72cf3150378a9fb40770a5466e99a34108b.
Packet: docs/worklogs/P1-09-completion.md, phần Exact submission.
Review 3 source files, P1-09 plan entries và worklog untracked; loại trừ P1-08.
Không sửa implementation hoặc commit/merge/push/deploy.
```

Packet timestamp: 2026-09-19T14:28:16.779581+07:00.

## Independent Codex acceptance — round 1

- Reviewer: independent Codex Reviewer session `/root/review_p109`, which did not author the submitted artifacts. Review time: 2026-09-19, 14:29–14:33 +07:00. Exclusive review/status handoff confirmed; shared-plan writes serialized with the P1-08 reviewer.
- Reviewed checkout: `anh`, HEAD/baseline `ec9572507eb63b446ded06361dd7cf8d104f134c`, empty index. Reviewed all three source diffs, this relevant untracked worklog and only P1-09 plan entries. P1-08 untracked skills/packet and its plan entries were excluded; P1-10 was not reopened.
- Identity verified independently: all three individual source hashes above matched raw bytes; aggregate `daed134ebc358650a40f335de589f72cf3150378a9fb40770a5466e99a34108b` matched. Pre-review worklog SHA-256: `401f8c1df1734e61c4245f59f029a58f66b314969c60940d642892b2c8631aac`; pre-review shared plan SHA-256: `761af1ee04e91e3273c42e0fb5bf9d86712b9c1acb306e1fe029fe7f1524306d`. These latter hashes identify inspected metadata, not frozen source; this acceptance bookkeeping is excluded from source identity.
- Dependency: P1-07 is Done in this checkout, with independent acceptance evidence and its policy, review contract and workflow artifacts present. Inspected implementer assignment, self-review, checks, scope/conflict declaration and prose-only N/A rationale; no missing required deliverable or ownership decision.

| AC | Independent conclusion |
|---|---|
| AC-01 | Verified B/C sections from `## B.` to `## Chọn` shrink from 4,177 to 2,246 normalized-text characters (46.2%). Canonical policy references and full worklog packet requirements remain. No measured token/cost claim. |
| AC-02 | Verified relevant-range reading and evidence links still require every AC/diff/finding; output compaction preserves diagnostic inspection and explicitly rejects missing, truncated, zero-discovery or skipped-required results as proof. |
| AC-03 | Verified unchanged accepted content can report historical acceptance; changed identity requires submission identification/resubmission, while new risk or stale/untrustworthy environment evidence requires affected checks under AGENTS. |
| AC-04 | Verified non-author independent acceptance, full required SQL/security/CI proof, stable finding IDs, report-only restrictions, exact identity, scoped status authority and Git restrictions remain. Canonical and compatibility mirror bytes are identical. |

Fresh checks ran on Windows / PowerShell 7.6.5 in `D:\Project BE\RoadGuardSystem` during the review window; full verifier outputs are secret-safe local files `%TEMP%/RoadGuard-P1-09-review-{docs,setup,planning}.log`.

| Rerun command/check | Exit/result |
|---|---|
| `pwsh -NoProfile -File tests/Documentation/Verify-P102Docs.ps1` | 0; all documentation contracts and dependency checks passed. |
| `pwsh -NoProfile -File tests/Tooling/Verify-AntigravitySetup.ps1` | 0; configuration, mirror, discovery/reference checks passed; no live calls. |
| `pwsh -NoProfile -File tests/Documentation/Test-P203Planning.ps1` | 0; 9 passed, 0 failed, 0 skipped; isolated temporary fixtures. |
| Python SHA-256/mirror/section-length/local-link checks | 0; three hashes and aggregate matched, byte equality passed, count reproduced, 7/7 local Markdown links resolved across reviewed sources/log/plan. |
| `git diff --check` | 0; no whitespace errors. |

Independent manual edge scenarios, not executable tests: matching Done preserves history; changed identity cannot reuse old acceptance; same content with new risk cannot bypass applicable checks; missing/stale evidence blocks acceptance; failed/zero/skipped-required checks cannot pass through summarized output; fix rounds preserve IDs and require regression/closure review; same-author acceptance is prohibited; explicit report-only permits no metadata edits. All eight retain the required outcome through the compact prompts and referenced canonical contract.

Findings: none. Verification gaps/blockers: none within P1-09. Optional follow-ups: none required. Runtime build/unit/API/SQL suites and behavioral RED remain N/A because this task changes prose only; live MCP, hosted CI execution, empirical token savings and actual future agent compliance are not claimed.

Verdict: **Done**. AC-01 through AC-04, dependency, exact content identity, required documentation checks, self-review and conflict gates pass. Only P1-09 review/status metadata is updated; prior submission evidence is preserved. This is local acceptance and does not imply commit, merge, push, deployment or publication.
