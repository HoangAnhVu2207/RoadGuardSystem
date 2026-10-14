# Document control and change history

**Phase1 only · owner-authorized execution/push on14/10/2026.** [Canonical entrance](../README.md).

## Authorization and allowed files

Owner message after successful Phase0 push instructs Codex to run Phase1 and push to `huy-review`, then wait for canonical review before full Backend/Web/Mobile/FastAPI rewrite. This authorizes governance/single entrance/precedence/glossary/client matrix/source registry/crosswalk/link checker. It does not authorize Phase2–5, implementation fixes, generated OpenAPI changes, migrations, lock updates or old-document deletion.

Writer: root Codex for this package. Shared production/code/contracts/tests/root instructions/old docs are unchanged. Pre-existing untracked five loose HHWS audit reports, Reporter Android audit and `docs/frontend/REPORTER-EXPO-HANDOFF.md` preserved and excluded from commit. No concurrent writer assigned to new canonical files.

Allowed additions: canonical README; governance01–05; actor/client matrix; tools/check_links.py. Proposed future section paths appear as non-clickable planned destinations only until their pages are written in separately authorized phases. No duplicate business schema/reference is copied into prose.

## Version and source control

| Date / phase | Authority / baseline | Scope / outcome |
|---|---|---|
|14/10/2026 Phase0 | Implementation6fdd98d; report commitccf0e70128b857d796a057b0aa30dc6bde4d20c9 | Six audit reports pushed; old/source preserved;0P0confirmed/5P1/7P2checkpoint;16PASS/1FAIL focused tests |
|14/10/2026 Phase1 | User instruction after Phase0; starting local/origin/huy-reviewccf0e70128b857d796a057b0aa30dc6bde4d20c9, fetch ahead/behind0/0 | Establish canonical governance/source registry/matrix/crosswalk/checker; old doc destinations not moved; waiting owner review before Phase2 |

Canonical pages use this baseline/date rather than embedding their own delivery SHA circularly. Final commit/push SHA and remote equality are reported in delivery. No force push/merge/rebase/reset. Commit content limited to allowed files. A record of source SHA is not proof of hosted CI or production deployment.

## Ownership and change policy

| Topic | Decision authority | Documentation/integration responsibility |
|---|---|---|
| Product/business/public compatibility/data policy | Owner | Preserve decision scope/qualifiers, ask only dependent missing decision |
| Technical direction/spec/external diff review | ChatGPT/owner workflow per root guidance | Not automatically performed by Codex self-review |
| Assigned backend source/shared writer | Huy coordinates shared source/spec reservations | Docs task does not edit implementation |
| Web/Mobile/AI consumer implementation | External teams, exact names/repos/ETA UNKNOWN | Consumer contracts/UX labeled proposals until source/runtime verified |
| New canonical Phase1 docs/checker | Root Codex | Two concise self-review passes and focused docs verification |

Existing old “ACTIVE/Official” labels are preserved as checkpoint metadata. Canonical crosswalk marks exact conflicts as stale/superseded; this does not blanket-deprecate all source-backed Backend Official pages or amend AGENTS/assigned workflow.

To update a page, record owner decision if behavior changes, current source/SHA/symbol, scope of runtime results and affected links. Generating a schema from code does not approve compatibility; updating a contract lock needs separate adoption review. Archive/delete requires owner-approved path/link migration; none performed in Phase1.

## Verification and review

Docs checker command from repository root:

```powershell
python docs/HoangHaiWarrantySystem-Official/tools/check_links.py
git diff --cached --check
```

Checker reads canonical Markdown only by default. It verifies inline/reference links and repo-local existence/Markdown-heading anchors after removing fenced code; it handles duplicate heading slugs. External URLs, HTML links, image resources, API access, deployed behavior and every GitHub renderer edge case are not verified. GitHub `#L` source-line fragments are treated as source-file targets rather than Markdown headings. Protected data/secrets are not read or sent to external services.

Meaningful negative controls verify a missing target and nonexistent heading are rejected; positive control verifies a valid canonical heading. Negative controls call the checker function on in-memory Markdown, with no file mutation. Production/runtime suites not run for this docs-only change; prior failures remain recorded in status registry.

Executed Phase1 verification: `check_links.py` reports **7Markdown pages /147local links /0errors**, with0external URLs tested. Temporary verification helper outside repository reports8positive/negative controls passing,0unpublished link targets,7pre-existing untracked files unchanged and1334source hashes unchanged. Crosswalk links all70Backend Official files. No production/runtime test suite rerun; prior Phase0 test/lock failures remain HISTORICAL scoped evidence. Whitespace and staged scope checks are required immediately before commit. Tools/tests do not silently write reports, repair old links, update dependency packages or normalize existing documents.

Self-review1: authority/client/scope/glossary, owner qualifiers vs source/runtime, no new wire/schema/data policy. Self-review2: one entrance/navigation/crosswalk paths, evidence status, writer/scope/old artifacts, link checker positive/negative controls and final staged diff. External review/owner acceptance of full canonical content is pending after delivery.

## Stop and next gate

Push this Phase1 package, then **STOP**. Owner reviews canonical governance before approving Phase2. Later Backend/workflow/API, Web/Mobile and FastAPI chapters remain NOT_STARTED; none are automatically authorized by Phase1 push.
