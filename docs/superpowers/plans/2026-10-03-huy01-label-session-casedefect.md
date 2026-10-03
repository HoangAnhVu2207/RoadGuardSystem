# HUY-01 Label Session CaseDefectRead Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Deliver the Huy-owned TrainingLabel and UserSession source shapes and a scoped CaseDefectRead producer without activating shared schema or DI.

**Architecture:** Preserve existing domain aggregates, add explicit materialization/current-head semantics, and keep persistence-facing reader queries on the caller's scoped `RoadGuardDbContext`. Missing authoritative facts are reported as `MissingReasons`; no synthetic complete dossier is emitted.

**Tech Stack:** .NET 8, C#, EF Core SQL Server, xUnit, FluentAssertions.

**Spec:** `planning/development/HUY-01.md` sections 7-9.

## Global Constraints

- Do not modify DbContext, migrations/snapshot, shared DI, canonical contracts, or ANH-02 consumers.
- Keep `SOURCE READY / SCHEMA PENDING` for model-changing Session members.
- Tests must distinguish unit/materialization evidence from SQL/HTTP acceptance.

### Task 1: TrainingLabel source shape

**Files:** `RoadGuardSystem.BusinessObjects/Labels/TrainingLabel.cs`, `TrainingLabelRevision.cs`, focused label tests.

- [ ] Add explicit current revision number/id, immutable source/file facts, stable materialization ordered by revision, and a row-version member without changing existing policy behavior.
- [ ] Add tests for materialization order, current-head reset, and immutable revision facts.
- [ ] Run label unit tests and build.

### Task 2: UserSession shape

**Files:** `RoadGuardSystem.BusinessObjects/Identity/UserSession.cs`, new transport enum, session unit tests.

- [ ] Add typed transport and nullable `LastActivityAt`, plus active/touch semantics that never revive revoked/expired sessions or extend absolute expiry.
- [ ] Add tests for legacy/web/android values, idle expiry, and touch rejection.
- [ ] Run identity unit tests; record EF model drift as schema pending.

### Task 3: CaseDefectRead

**Files:** exact handoff DTO/interface boundary, Huy-owned reader implementation and tests.

- [ ] Use the caller context and current actor/project facts.
- [ ] Query persisted case/report/defect identities and current statuses; add explicit missing reasons for unavailable source/geometry/lifecycle facts.
- [ ] Add unit tests for privacy and incomplete snapshots; leave production binding untouched.

### Task 4: Documentation and verification

- [ ] Record initial state, source handoff, changed files, model impact, blockers, exact commands, and acceptance levels in `planning/development/HUY-01.md`.
- [ ] Run affected build/tests and `git diff --check`, self-review twice, commit and push `huy-review`.
