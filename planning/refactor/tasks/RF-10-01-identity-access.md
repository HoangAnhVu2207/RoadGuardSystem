# RF-10-01: Identity, session, profile and authorization

> Current execution 2026-10-01: Anh alone owns R/C refactor on `anh`. F/G is unassigned for later development; proposed A/B text below is historical only. Future identity after a common baseline is Huy/A (`huy`), Anh/B (`anh`). See `../10-refactor-slices.md` and `../11-development-plan.md`.

- **Status/checkpoint:** PARTIAL parent on 2026-10-01. `10-01-C01` current-behavior characterization is **Done locally** in `../10-identity-characterization-baseline.md` and `../reports/RF-10-01-C01.md` after new focused HTTP/owned-SQL 5/5 and selected existing 6/6 + 2/2 + 2/2. Profile/reset routes and bearer remain unchanged. R01 parser and R02/R03 identity internals RETAIN; F/G cookie/profile/reset/consumer transition awaits Q-RF02-02 and real client/deployed evidence. Branch `anh`, local HEAD `2efc8a5` plus preserved dirty tree; no change commit. Parent report `planning/refactor/reports/RF-10-01.md` remains a future aggregate checkpoint.
- **Goal:** reconcile R01-03, L01-04/L08/L15, CG01-03 and Accepted 36A/37 without silently choosing `/me` over `/profile` or one password-reset route.
- **In scope:** authentication/session/OTP, actor/profile/admin reset, role/project-scope checks; focused Service/Repository cleanup after characterization. **Out of scope:** breaking bearer clients, deleting routes, external Web/Android code, global error rename without approval.
- **Dependencies/decisions:** RF-09; Q-RF02-02 for cookie/bearer compatibility, PII shape and deprecation window; actual web/Android consumers and CSRF/CORS owner. 36A/37 accepted values are requirements, not test results.
- **Read first:** R01-03, CG01-03, L01-04/L08, decision rows 36A/37, ADR 002 history; `AuthController`, `ProfileController`, `MeController`, `UsersController`, `AdminUsersController`, Services/IdentityRepository, migrations/tests.
- **Likely files:** those controllers, identity DTOs/Services/Repositories, auth configuration/DI, focused ApiTests/IntegrationTests, contract/Postman/`.http` **only for approved versioned delta**.
- **Contract/data/consumer effect:** current JWT bearer and both profile/reset contracts preserved during structural phase. Approved cookie/mobile version needs parallel auth, CSRF, session TTL, rotation, error/PII compatibility; session schema/backfill only after explicit migration rehearsal. No password/secret in reports.
- **Steps:** capture login/refresh/OTP/profile/reset wire and SQL effects -> compare effective config with 37 -> extract identical internals -> decide versioned auth/profile contract -> implement approved slice with dual-client period -> retire old route only after consumer gate.
- **Verify:** isolated API login/OTP/expiry/refresh/replay, wrong actor/project and PII non-leak; SQL token rotation/race; current/target status/body/headers and Postman/FE contract checks. Build affected projects; record unrun external-client tests.
- **Done when:** accepted 36A/37 compliance or explicit Partial for missing provider/client evidence; all retained routes have characterized behavior; no privilege/PII regression; approved transition and rollback documented.
- **Recovery:** feature-flag/route revert to bearer/old routes while keeping sessions readable; additive schema forward repair/backup restore per RF-09, never revoke working clients by doc rename.

## Two-developer delivery supplement

- **Proposed owner:** A. Split dual-owner parent tasks into single-owner slices before edits. See [ownership plan](../03-two-developer-plan.md).
- **Pre-edit checkpoint:** record exact allowed files/symbols, read-only dependencies, shared-file reservation, baseline commit and preserved dirty changes. Record START / INTEGRATE / RELEASE gates for this slice; existing decision/data gates remain in force.
- **Independence:** use an agreed interface/version and fixture for consumer work; label test-double evidence separately. Do not claim parent Done before required real integration.
- **Self-review:** correctness pass plus ownership/consumer impact pass, safe in-scope autofix, focused verification and final diff review. Record unresolved findings.
- **Cross-owner impact:** create a note from [coordination template](../templates/coordination-note.md), recommend primary fixer and wait for the two developers to assign the overlapping change. Continue independent work; do not edit the other owner's files or duplicate their business logic.
- **Report:** [revised seven-section template](../templates/task-report.md); single-owner task may retain its existing report path, slices use `reports/<TASK-ID>-<SLICE>-<A|B>.md`. Record implementation status separately from delivery stage.
