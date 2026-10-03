# HUY-01 Closure Final Implementation Plan

> **For agentic workers:** Execute the checklist in this session and keep each verification result in `planning/development/HUY-01.md`.

**Goal:** Close the HUY-01 capabilities that are implementable from the combined Huy/Ạnh handoff while leaving only explicitly unavailable environment or business gates.

**Architecture:** Preserve Huy's reviewed Reporter/domain behavior, selectively integrate the exact Anh multipart/session/schema dependencies, then bind only real HUY-01 readers and services through the existing composition root. Production mappings and migrations remain the source of SQL evidence; no fixture-only adapter is accepted.

**Tech Stack:** .NET 8, ASP.NET Core, EF Core SQL Server, xUnit, disposable SQL fixtures, Postman JSON validation.

**Spec:** `planning/development/HUY-01.md` and the closure prompt supplied for this turn.

## Global Constraints

- Work only on `huy-review`; preserve unrelated work and never reset, amend, force-push, or merge `anh-review`, `develop`, or `main`.
- Keep HUY-02 inspection/repair/offline work out of scope.
- Use exact source SHAs for Anh handoff; do not treat historical test claims as fresh evidence on the combined HEAD.
- Do not emit events or activate a route until its canonical contract and real producer/consumer acceptance are present.

### Task 1: Dependency and graph inventory

**Files:** `planning/development/HUY-01.md`; optional exact handoff files only after diff review.

- [ ] Record local/tracking/live HEAD and dirty paths.
- [ ] Verify common ancestors and inspect exact Anh commit/file diffs.
- [ ] Build an allowlist of HUY-01 dependencies and reject ANH-02-only files.

### Task 2: Session and multipart persistence

**Files:** `RoadGuardSystem.BusinessObjects/Identity/UserSession.cs`, `RoadGuardSystem.Repositories/Configurations/UserSessionConfiguration.cs`, additive migration/snapshot, multipart files from the exact handoff when required.

- [ ] Add a failing SQL/model regression for the missing session columns and transport constraint.
- [ ] Add the minimal additive migration with legacy backfill and compatibility checks.
- [ ] Import multipart recovery only when its dependency closure is proven and run its focused SQL/adapter tests.

### Task 3: HUY-01 production reader/binding closure

**Files:** Huy-owned reader/service interfaces, existing HUY-01 composition extensions, focused tests.

- [ ] Add failing tests for real persisted facts, current authorization, incomplete snapshots, canonical status/hash, and wrong-project rejection.
- [ ] Bind only implementations whose producer facts and contracts are present.
- [ ] Run unit, API, SQL, migration/model, and JSON/Postman validation; record unavailable external/storage gates.

### Task 4: Documentation, review, and delivery

**Files:** `planning/development/HUY-01.md` and canonical checkpoint docs touched by the combined diff.

- [ ] Update the finite closure ledger with source/binding/SQL/HTTP/network/external-review status and exact commands/counts.
- [ ] Perform two self-review passes and fix bounded findings.
- [ ] Commit once coherently, push `huy-review`, verify local/tracking/live SHAs and stop.
