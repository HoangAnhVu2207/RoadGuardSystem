# Web B completion — assigned 2026-11-25

Owner assignment: attached BE request dated 2026-11-25, extending the accepted
Web B decisions. Base: 71d8ae85612fcc11b2b96227cc87981da383e2d5. Writer: Huy.
Worktree D:/RG-web-b-contracts, branch codex/web-b-completion. This worktree was
clean. Main checkout's seven modified and nine untracked paths remain reserved.

Scope: B1 supported CRS/distance and explicit uncertainty readback; B12 minimal
current-safety-PM resolution workflow with immutable provenance, evidence,
concurrency and receipt; crash139 evidence and capture tooling; related CI fixes,
regressions, migration/catalog/Seeder, OpenAPI and FE handoff. No FE changes,
production guard bypass, package upgrade, shared seed/reset or remote deploy.

TARGET_CONFIRMED: matching remains advisory, explicit source confirmation and
pinned geometry; no snapping, synthetic accuracy or custom datum assumptions.
Current PM of the safety obligation may resolve against recorded conditions and
scoped verified evidence; formal responsibility/final does not resolve safety.
Source references: WEB-B-READ-CONTRACTS.md, owner-web-b-decisions-20261125.md,
RepairSafetyMonitoring, RepairObligation, CandidateMatchingService and CRS engines.

CURRENT_VERIFIED baseline CI: both branch runs at 71d8ae8 failed the same three
API tests (1705 PASS / 3 FAIL / 1 allowed MinIO smoke SKIP): stale source excerpts,
description length, stale PM safety read denial. Other seven jobs passed. Preserve
exact-wire/source checks and skip whitelist. Downloaded both real TRX/logs outside
Git. Do not attribute these failures to SQL or runner without evidence.

Verification: isolated HTTP/SQL, actual private storage where fixture supports it;
sequential full API and SQL lanes if local resources allow. Record each run and
revision, keep failures and reruns distinct. Two self-review passes before commit.
Normal push huy-review then fast-forward develop; fetch/ancestry guards, no force.
Local update from final source with backup/restore rehearsal before migration,
bootstrap off and old image/volumes retained. Health is not business acceptance.

## Self-review evidence (continuing verification)

Pass 1: resolution evidence initially lost nested fields through the audit
sanitizer. Added the exact nested provenance allowlist and fresh-session checks
for file/link/uploader/hash, rather than trusting the POST response. Resolution
touches measure and package versions; obligation has its own rowversion. Separate
nullable audit FK avoids misusing a formal RepairDecision for safety. Old checks
remain immutable and current safety responsibility is independent of formal.

Pass 2: traced selection, Crew active writes, safety notification/deadline readers,
package conflict reads and project lifecycle to include the new resolved head.
Added retention references to resolution evidence without changing deletion
policy. Kept current source-file/version/hash checks distinct from metadata read.
Migration has an explicit populated Down fence and no backfill. A failed historical
downgrade can already have removed a newer empty migration, so the migration test
reapplies latest before asserting no pending migration. No workflow/gate/SQL test
identity change. External review remains pending, not a claimed peer review.
