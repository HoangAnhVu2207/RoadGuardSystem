# RoadGuard V2 Skill And Governance Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make `$roadguard-endpoint-delivery` reliably deliver one approved V2 endpoint through the existing N-layer architecture with vertical task ownership and risk-proportionate verification.

**Architecture:** `AGENTS.md` remains the canonical repository policy, a new ADR records V2 vertical ownership, and three project skills separate endpoint orchestration, persistence correctness, and test selection. Planning task boilerplate and documentation validators are aligned without changing backend source or product contracts.

**Tech Stack:** Markdown/YAML project skills, Python 3 documentation validators, ASP.NET Core repository conventions, JSON task manifest, OpenAPI YAML.

**Spec:** `docs/superpowers/specs/2026-09-27-roadguard-v2-skill-governance-design.md`

## Global Constraints

- Preserve `Controller -> IService -> IRepository` and ADR 001/004 layer boundaries.
- V2 task owners own the full approved endpoint slice, including directly required persistence and migration work.
- Serialize shared DbContext, mapping, migration snapshot, DI, error-contract, and OpenAPI edits to one writer.
- Preserve unrelated working-tree changes and historical Done evidence.
- Do not change backend C# source, packages, database state, generated migrations, task contracts, task IDs, gates, dependencies, or owners.
- Do not commit, push, merge, rebase, stash, or change branches without separate owner approval.

---

### Task 1: Record V2 Ownership Governance

**Files:**
- Create: `docs/adr/006-v2-endpoint-ownership-and-persistence-coordination.md`
- Modify: `docs/adr/001-backend-boundary.md`
- Modify: `docs/adr/002-authentication.md`
- Modify: `docs/adr/003-backend-delivery-and-ai-boundary.md`
- Modify: `docs/adr/004-n-layer-backend-structure.md`
- Modify: `docs/adr/005-product-workflow-synchronization.md`

**Interfaces:**
- Consumes: owner-approved design and current accepted ADR history.
- Produces: one authoritative ownership decision referenced by all earlier ADRs.

- [x] **Step 1: Add ADR 006**

Document vertical task ownership, unchanged N-layer boundaries, shared-hotspot reservation, Person 2 migration/SQL coordination, source precedence, and transition from legacy plans to V2 task files.

- [x] **Step 2: Amend ADR 001 and ADR 004**

Add explicit supersession notes so architectural layer ownership is not interpreted as exclusive person ownership. Preserve their accepted architecture rationale and history.

- [x] **Step 3: Amend ADR 002, ADR 003, and ADR 005**

Replace stale fixed-task ownership statements with V2 task ownership, distinguish proposed V2 auth error codes from current compatibility behavior, route scheduling to V2 task files, and keep ADR 005 Proposed.

- [x] **Step 4: Verify ADR consistency**

Run:

```powershell
rg -n "P1-|P2-|exclusive|độc quyền|exactly two|hai kế hoạch|ownership|Owner" docs/adr
git diff --check -- docs/adr
```

Expected: historical task references remain clearly historical; active ownership references ADR 006; no whitespace errors.

### Task 2: Install And Align The Three Skills

**Files:**
- Replace/merge: `.agents/skills/roadguard-endpoint-delivery/SKILL.md`
- Create/replace: `.agents/skills/roadguard-endpoint-delivery/references/source-map.md`
- Create/replace: `.agents/skills/roadguard-endpoint-delivery/references/product-gates.md`
- Modify: `.agents/skills/roadguard-endpoint-delivery/agents/openai.yaml`
- Create/replace: `.agents/skills/roadguard-endpoint-delivery/assets/icon.svg`
- Create: `.agents/skills/roadguard-persistence/SKILL.md`
- Create: `.agents/skills/roadguard-persistence/references/risk-cases.md`
- Create: `.agents/skills/roadguard-persistence/agents/openai.yaml`
- Create: `.agents/skills/roadguard-persistence/assets/icon.svg`
- Replace/merge: `.agents/skills/roadguard-test-selection/SKILL.md`
- Create: `.agents/skills/roadguard-test-selection/references/commands.md`
- Modify: `.agents/skills/roadguard-test-selection/agents/openai.yaml`
- Create/replace: `.agents/skills/roadguard-test-selection/assets/icon.svg`

**Interfaces:**
- Consumes: supplied skill packages and ADR 006 ownership model.
- Produces: endpoint orchestrator plus persistence and verification specialists with valid UI metadata.

- [x] **Step 1: Install source package resources**

Copy the supplied reference and asset files into the matching project skill directories. Do not retain obsolete duplicate instructions.

- [x] **Step 2: Rewrite endpoint orchestration**

Require task-path resolution, source comparison, scope/contract approval, V2 ownership, current-vs-proposed delta handling, persistence/test skill routing, N-layer boundaries, HTTP example, and evidence-based completion.

- [x] **Step 3: Align persistence and test selection**

Preserve SQL durability rules and fresh test-binary requirements. Remove exclusive Person 2 persistence ownership while keeping migration serialization and SQL coordination.

- [x] **Step 4: Validate each skill**

Run:

```powershell
python C:/Users/HoangAnhVu/.codex/skills/.system/skill-creator/scripts/quick_validate.py .agents/skills/roadguard-endpoint-delivery
python C:/Users/HoangAnhVu/.codex/skills/.system/skill-creator/scripts/quick_validate.py .agents/skills/roadguard-persistence
python C:/Users/HoangAnhVu/.codex/skills/.system/skill-creator/scripts/quick_validate.py .agents/skills/roadguard-test-selection
```

Expected: `Skill is valid!` three times.

### Task 3: Align Repository Rules And Planning V2

**Files:**
- Modify: `AGENTS.md`
- Modify: `.agents/rules/roadguard.md`
- Modify: `planning/V2/README.md`
- Modify: every task path listed by `planning/V2/task_manifest.json` (133 files under `planning/V2/Person_1/` and `planning/V2/Person_2/`)

**Interfaces:**
- Consumes: ADR 006 and the three installed skill entrypoints.
- Produces: one consistent invocation path from a task file to project rules and skills.

- [x] **Step 1: Rewrite canonical ownership and workflow rules**

Update `AGENTS.md` for V2 task paths, vertical ownership, shared reservations, migration coordination, all three skills, contract-delta gates, and sufficient verification. Keep layer and safety rules intact.

- [x] **Step 2: Reduce the discovery rule to routing guidance**

Point endpoint work to `roadguard-endpoint-delivery`, persistence changes to `roadguard-persistence`, tests to `roadguard-test-selection`, and canonical policy to `AGENTS.md`/ADR 006.

- [x] **Step 3: Update the V2 README**

Replace the old Person 1/Person 2 persistence split and two-skill inventory with vertical endpoint ownership, shared-hotspot sequencing, and three-skill usage.

- [x] **Step 4: Mechanically update all task boilerplates**

For each manifest-listed file, replace the ownership sentence and two-skill instruction only. Preserve route, operationId, status, contract, gates, dependencies, test cases, and HTTP examples byte-for-byte outside those boilerplate lines.

- [x] **Step 5: Verify manifest/task invariants**

Check 133 unique task IDs and operation IDs, P1 71/P2 62, existing files, known dependencies, exact OpenAPI mapping, and one occurrence of each new boilerplate sentence per task.

### Task 4: Make Documentation Verification Portable And Revalidate

**Files:**
- Modify: `docs/diagram/V2/09_Frontend/contracts/validate_package.py`
- Modify: `docs/diagram/V2/ci/test_contract_guard.py`
- Modify: `planning/V2/VALIDATION.md`

**Interfaces:**
- Consumes: UTF-8 repository documents and existing contract lock.
- Produces: Windows-locale-independent documentation checks and current validation evidence.

- [x] **Step 1: Make text reads explicitly UTF-8**

Add `encoding='utf-8'` to repository text reads in both scripts without changing validation semantics.

- [x] **Step 2: Run contract and structural checks without environment overrides**

Run:

```powershell
python docs/diagram/V2/09_Frontend/contracts/check_contracts.py
python docs/diagram/V2/09_Frontend/contracts/validate_package.py
python docs/diagram/V2/ci/test_contract_guard.py
```

Expected: contract hash PASS, structural checks PASS, and all guard self-tests PASS without `PYTHONUTF8`.

- [x] **Step 3: Update validation evidence**

Record the governance/skill checks separately from backend tests. State explicitly that no backend build, SQL, HTTP, device, or UAT execution occurred.

- [x] **Step 4: Final diff verification**

Run:

```powershell
git diff --check
git status --short --branch
```

Inspect only scoped paths and report unrelated pre-existing changes separately. Do not commit.
