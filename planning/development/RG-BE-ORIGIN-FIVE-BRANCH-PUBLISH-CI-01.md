# Backend five-branch publication

## Assignment and scope

TARGET_CONFIRMED: Owner authorized Backend commit, non-force publication and CI repair
on `develop`, `anh`, `anh-review`, `huy`, `huy-review` on 2026-10-25.
Production release and FE source changes are outside this assignment.
Codex is the sole writer in the isolated publication checkout; the inherited workspace remains untouched.

## Source and preservation

- Original source: `huy-review`, `dd94aa697c411bb11d305973580055216f65ba73`.
- Selected Backend integration: `b98a26cab63289d10b29630537a3f7f327c4fc41`.
- Historical Anh merge parent: `5089c3267dcdf60645ab34f61b58a79e3cbb0cf6`.
- Original 909 dirty/untracked files, original refs, patches and immutable Candidate1–3
  artifacts are preserved privately. No reset, clean or force push is permitted.
- The merge retains both histories. Consolidated baseline and additive C1/B2 migrations
  remain active; superseded migration bytes remain in their original commits and private archives.

## Integration decisions

Retain current approved source for authority, A1 replay fencing, B2 assignments, C1 privacy,
optional-null serialization, PersistenceFacts boundaries and current reporting producers.
Resolve historical test assumptions against actual current readers without changing public contracts.
Keep historical documents explicitly historical; they grant no new data or publication authority.

Technical fixes include deployment override precedence, excluding developer-local configuration
from publish, owned seed identity validation, SQL lane identity reconciliation and isolated
queue fixtures for tests whose workers claim across projects. Preserve strict failed/skipped
TRX gates and the explicitly optional legacy upload smoke exception only.

## Verification and delivery

Run fresh build/unit/API/SQL, format, documentation, workflow negative tests, source preservation
and secret/dependency checks. Retain every failed execution. Record exact commands, timestamps,
source SHA-256 and raw TRX privately; no raw credentials or personal evidence enters Git.
Two final Codex reviews cover source/history/security, then CI/remote/evidence integrity.
Publication is complete only after all five exact remote SHAs and their own CI runs pass.
An external review is separate and remains PENDING until actually performed.

## Limits

Candidate3 remains immutable. The integrated source includes later architecture and CI fixes;
Git/CI success does not certify original Candidate3 deployment parity, full contract acceptance,
FE acceptance or production readiness. API030 remains NOT_VERIFIED; Backend H02–H05 remain OPEN.

## Retained local verification checkpoint

CURRENT_VERIFIED (merged tree, before publication): build exit 0; unit 1,117/1,117;
API 1,533 PASS / 0 FAIL / 1 explicitly optional legacy MinIO smoke skip, 1,534 total.
The owned MinIO export recovery test executed. Strict unit/API TRX gates passed.
Format, staged diff whitespace, documentation, source-model parity (EF 8.0.17),
workflow policy, dependency security and 119-class SQL lane discovery passed.
SQL execution and remote exact-SHA CI remain pending at this checkpoint.

HISTORICAL: retain the earlier merged API execution (1,519 PASS / 13 FAIL / 2 SKIP)
and focused failing runs. Fixes replaced obsolete missing-producer fixtures with explicit
fixtures, asserted current canonical reporting facts, checked project-scoped export
403 before authorized expiry 410, and isolated global worker queues. Those failed runs
are not relabeled as PASS. A later successful run is a separate execution identity.

HISTORICAL: first merged SQL lane 1 executed 203 tests, 201 PASS / 2 FAIL.
Both failures are imported export replay/concurrency fixtures without the project membership
required by current receipt authorization. Preserve that TRX; correct the fixture's authority
and rerun affected tests without weakening receipt, lease or atomicity assertions.
The H4 Normal/Supervisor and FastTrack/PM theory cases also require exact parameterized
identities in the SQL manifest. SQL gate remains open until all four lanes reconcile.

## Final local integration verification

CURRENT_VERIFIED: SQL lanes 203/203, 179/179, 217/217 and 208/208 PASS, zero skips.
The exact identity union is 807 executed cases. Export focused regression is 4/4 PASS.
The sandbox schema/rollback test now writes to an execution-owned temporary directory,
removing its dependency on a historical Windows handoff path. Its full lane was rerun.
Schema inventory refresh changes source/tool hashes only; model, snapshot and SQL facts agree.
Retained run checksums and relevant production/test source pins match the final tree.
Unit/API runs remain reusable: their production, shared and own test sources have not changed.
Format (full plus final changed fixtures), documentation and whitespace checks pass.

Self-review 1 (Codex): reviewed selected source, both histories and individual merge
resolutions, authority/privacy/receipt boundaries, baseline migration preservation,
developer configuration exclusion, disposable test isolation and secret scan.
Fixed the fixture, identity-manifest and portable-output findings above. Original dirty
source and failed evidence remain preserved. No new public contract decision is introduced.
Self-review 2 covers remote SHA, per-branch CI and final delivery; it remains pending
until those remote checks execute. Independent review and production release remain pending/not run.

## Actual remote CI repair: portable reporting export fonts

HISTORICAL: publication commit `05c55593a270d9877ded4e0d900657f0466faac5`
was pushed non-force to all five named branches. Develop run 40332829556 executed
1,529 API PASS / 4 FAIL / 1 optional SKIP; all SQL lanes and their union passed.
The retained raw TRX identifies `export_font_unavailable` in reporting exports.
Two HTTP fixtures selected Windows Arial directly despite CI already declaring
`RG_TEST_UNICODE_FONT` as DejaVu Sans on Linux. Both fixtures now use the explicit
licensed-font environment configuration, consistent with other export tests.
No renderer, public contract, security check or test assertion is relaxed.
The failed remote execution remains a distinct historical result. Focused and
full affected API verification plus every branch's new exact-SHA CI are required.

CURRENT_VERIFIED: focused export HTTP regression 4/4 PASS after correction;
changed-file format, documentation and diff checks PASS. Full local API rerun
was launched, but Docker subsequently timed out and `/containers/json` returned
HTTP 500; that execution is retained and is not accepted as PASS. Earlier full
local API evidence remains historical for unchanged tests. Fresh complete CI
on each repaired branch is mandatory; local infrastructure recovery is pending.
Self-review of this repair confirms exactly two test configuration substitutions,
no production changes and no assertion removal. Original 909 dirty files have zero drift.
