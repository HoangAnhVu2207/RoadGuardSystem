# Antigravity completion log — P1-00

## Identity and scope

- **Task ID/title:** P1-00 / Wave 0 — Executable Foundation
- **Owner / reviewer:** Person 1 (Antigravity) / Person 2 (Independent Reviewer)
- **Date / branch or commit:** 2026-09-17 / `anh` / HEAD `da3a1aab343425a17886601a567cda6c3898ce7b` (post-PR #1 merge)
- **Trace:** TE-01 (foundation/infrastructure task with no business use-case)
- **Status:** Approved / Done

### In-scope behavior
- Verify and document production project dependency graph
- Enforce boundary rules: `BusinessObjects` has no EF Core, DTOs, Services, Repositories, or API dependencies; `DTOs` has no EF Core or HTTP dependencies; `API` has no direct Repositories dependency
- Create `Directory.Build.props` (Nullable enable, ImplicitUsings enable, Deterministic true, NetAnalyzers enabled)
- Create `global.json` (pin SDK 10.0.401 with `latestPatch`)
- Add root `AGENTS.md` for standard tool and agent discovery (F7)
- Bootstrap Git with protected-flow documentation for `main` -> `develop` and personal branches `anh`/`huy`
- Fix empty `Program.cs` (CS5001 baseline error) with minimal ASP.NET Core startup + `public partial class Program {}`
- Clean up production package references (F1, F2):
  - `BusinessObjects`: removed `Microsoft.AspNetCore.Identity.EntityFrameworkCore`; added `Microsoft.Extensions.Identity.Stores` (v8.0.17, zero EF Core dependencies)
  - `DTOs`: removed `Microsoft.EntityFrameworkCore.SqlServer`, `Design`, `Tools`, `Microsoft.AspNetCore.Http`, `Configuration`
  - `Repositories`: added `Microsoft.EntityFrameworkCore` (v8.0.17) directly for `PagedList.cs`
- Fix all 4 compiler warnings (F4):
  - `EnumExtensions.cs`: nullable-annotated `FieldInfo?` and `DescriptionAttribute?`
  - `ApiResult.cs`: added `required` modifier to `SuccessResponse<T>.Data` and `Message`
- Unit tests (`tests/RoadGuardSystem.UnitTests`):
  - Reusable dependency checker (`DependencyGraphChecker.cs`) inspecting both `ProjectReference` and `PackageReference`
  - In-memory negative fixture tests for all forbidden edges (including BO -> DTOs) and forbidden packages
  - Actual production architecture tests enforcing package and reference boundaries
  - BusinessObjects assembly and `EnumExtensions` smoke tests
- API startup tests (`tests/RoadGuardSystem.ApiTests`):
  - `WebApplicationFactory<Program>` smoke test
  - Middleware pipeline verification
- Run non-incremental build: 0 warnings, 0 errors
- Run `dotnet format --verify-no-changes --no-restore`: clean (exit code 0)
- Run all tests: 28 passed, 0 failed, 0 skipped

### Explicitly out of scope
- Business controllers, endpoints, JWT/Identity configuration, `DbContext`, migrations, entities
- ProblemDetails, API versioning, correlation middleware, health checks, OpenAPI schema customization (owned by P1-01)
- ADR documents (owned by P1-02)
- SQL Server/NetTopologySuite integration and migrations (owned by P2-00)
- GitHub remote creation, pushing, hosted branch-protection settings, and rewriting published history

---

## Status of Review Findings (F1–F8)

### F1 (Blocker) — Architecture test false green & BusinessObjects EF dependency
- **Root cause:** `DependencyGraphChecker` previously checked only `ProjectReference` elements and omitted `BusinessObjects -> DTOs`. Furthermore, `RoadGuardSystem.aBusinessObjects.csproj` referenced `Microsoft.AspNetCore.Identity.EntityFrameworkCore`.
- **Resolution:**
  1. Added `BusinessObjects -> DTOs` to `ForbiddenProjectReferenceEdges` and added fixture test `Negative_CheckerDetects_BusinessObjectsDependsOnDTOs`.
  2. Extended `DependencyGraphChecker` with `FindForbiddenPackages(projectPath, forbiddenPackagePrefixes)` and added negative fixture tests `Negative_CheckerDetects_ForbiddenPackage_InBusinessObjects` and `Negative_CheckerDetects_ForbiddenPackage_InDTOs`.
  3. Replaced `Microsoft.AspNetCore.Identity.EntityFrameworkCore` in `BusinessObjects` with `Microsoft.Extensions.Identity.Stores` (v8.0.17). This provides `IdentityUser<Guid>` and `IdentityRole<Guid>` without any EF Core packages or EF runtime dependencies (verified via `project.assets.json`).
  4. Added production architecture tests `BusinessObjects_HasNoForbiddenPackages` and `DTOs_HasNoForbiddenPackages`.
- **Evidence:** See Red-First / Green section below for reproduced failure and pass.

### F2 (High) — DTO and persistence dependencies in wrong projects
- **Root cause:** `RoadGuardSystem.bDTOs.csproj` referenced EF Core SqlServer, Design, Tools, Http 2.3.0, and Configuration packages, none of which are used in DTO source code. `RoadGuardSystem.cRepositories.csproj` relied on transitive EF Core from DTOs for `PagedList.cs`.
- **Resolution:**
  1. Removed all EF Core, HTTP, and Configuration packages from `RoadGuardSystem.bDTOs.csproj`. DTOs now contains only the `ProjectReference` to `BusinessObjects`.
  2. Added `Microsoft.EntityFrameworkCore` (v8.0.17) directly to `RoadGuardSystem.cRepositories.csproj`. Documented in comments that SQL Server, Design, and NetTopologySuite are deferred to P2-00 when `DbContext` is introduced.
  3. Added architecture tests enforcing that DTOs cannot reference EF Core, HTTP, or persistence packages.

### F3 (High) — Diff review reproducibility — Closed (Approved)
- **Root cause:** The original workspace had no `.git` repository or immutable baseline.
- **Resolution & Review Evidence:**
  - With repository-owner approval, Git was initialized on `main` (baseline `9293545`) and local branches `develop`, `anh`, and `huy` were created.
  - Person 2 / Independent Reviewer conducted a full diff review comparing baseline `develop`/`c48be5d` against reviewed HEAD `da3a1aab343425a17886601a567cda6c3898ce7b` (following PR #1 integration of branch `huy`).
  - Result: All architecture boundaries, package constraints, warning fixes, and testing criteria are verified with zero actionable code findings. F3 is officially closed.

### F4 (Medium) — Clean-build evidence understates warnings
- **Root cause:** `EnumExtensions.cs` had two CS8600 warnings; `ApiResult.cs` had two CS8618 warnings. Incremental builds had masked the EnumExtensions warnings.
- **Resolution:**
  1. In `EnumExtensions.cs`, typed local variables as `FieldInfo?` and `DescriptionAttribute?` with null-safe access.
  2. In `ApiResult.cs`, added `required` modifier to `SuccessResponse<T>.Data` and `Message`.
  3. Verified via non-incremental build: `dotnet build RoadGuardSystem.slnx --no-restore --no-incremental` produces **0 warnings, 0 errors**.

### F5 (Medium) — SDK policy for net8.0 solution — Accepted Non-Blocking Residual Risk
- **Current state:**
  - Solution projects target `net8.0`.
  - Machine environment has .NET SDK `10.0.401` and runtime `8.0.31` installed.
  - `global.json` pins SDK `10.0.401` with `rollForward: "latestPatch"` to maintain exact SDK reproducibility across the team.
  - The repository owner temporarily accepts this toolchain for the existing target without authorizing a target-framework change.
- **Disposition (Accepted Non-Blocking Follow-up):**
  - This finding is not declared closed/completed because formal ADR and CI proof artifacts are still pending.
  - However, per repository owner decision, it is accepted as a non-blocking residual risk for P1-00 sign-off.
  - Rationale for reclassification: Requiring ADR and CI proof to complete before P1-00 creates an unresolvable circular dependency, as both P1-02 (`docs/adr/001-backend-boundary.md`) and P2-01 (CI proof) depend on the P1-00 foundation being approved first.
  - Mandatory follow-up tracking remains strictly assigned to P1-02 (ADR documentation) and P2-01 (CI reproduction and proof).

### F6 (Medium) — Negative-First evidence clarification
- **Resolution:**
  - Separated fixture unit tests (testing the verification tool itself) from domain/production red-first test runs.
  - Recorded actual failing run for `BusinessObjects_HasNoForbiddenPackages` (caught EF Core Identity package) and `DTOs_HasNoForbiddenPackages` (caught Http package) before fixing project references.
  - Documented transition from RED to GREEN with exact command output.

### F7 (Medium) — Repository instruction file discoverability
- **Resolution:**
  - Created root `AGENTS.md` with identical content to `.antigravity/AGENTS.md`. Standard tools (`rg`, CI scripts, IDE agents) can now discover the file at the repository root.

### F8 (Low) — Scope boundary with P1-01 Swagger
- **Resolution:**
  - Clarified the comment in `ApiStartupTests.cs`: Swagger bootstrap in P1-00 is solely the baseline necessary for `WebApplicationFactory` startup pipeline validation. No equivalent clarification was added to `Program.cs`.
  - Full OpenAPI configuration, API versioning, ProblemDetails, and endpoint metadata belong exclusively to P1-01.

---

## Review-Fix Round 2 (2026-09-17) — Close Architecture Gate False Greens

### Finding 1 (High) — BuildProductionGraph silently dropped unmapped ProjectReferences
- **Root cause:** `DependencyGraphTests.cs:81-84` used `.Where(r => assemblyToLogical.ContainsKey(r))` which silently filtered out any `ProjectReference` not present in `assemblyToLogical`. If `BusinessObjects` referenced a newly introduced infrastructure project, the gate remained green (false green).
- **Resolution:**
  1. Extracted reusable `DependencyGraphChecker.BuildGraph(projects, assemblyToLogical, referenceReader)` where unmapped references are preserved with their raw names instead of being omitted.
  2. In `FindForbiddenEdges`, enforced two explicit gates:
     - Domain boundary: `BusinessObjects` must not depend on ANY project reference (must have zero project references).
     - Architecture boundary: All project references across all projects must belong to recognized architecture layers (`BusinessObjects`, `DTOs`, `Repositories`, `Services`, `API`). Unmapped references trigger an explicit violation naming the source project and unmapped reference.
  3. Added negative fixture test `ProductionGraphPolicy_Rejects_Unmapped_ProjectReference_From_BusinessObjects`.

### Finding 2 (High) — ForbiddenBusinessObjectsPackagePrefixes missed transport and JWT packages
- **Root cause:** `DependencyGraphChecker.cs:133-137` only checked for EF Core prefixes (`Microsoft.EntityFrameworkCore`, `Microsoft.AspNetCore.Identity.EntityFrameworkCore`). Repository rules prohibit transport dependencies in `BusinessObjects`; `Microsoft.AspNetCore.Http` and JWT packages were not detected.
- **Resolution:**
  1. Expanded `ForbiddenBusinessObjectsPackagePrefixes` to include `Microsoft.AspNetCore`, `System.IdentityModel`, `Microsoft.IdentityModel`, and `System.Net.Http`.
  2. Established `AllowedBusinessObjectsPackages` containing strictly `Microsoft.Extensions.Identity.Stores`.
  3. Enforced the explicit allow-list in production test `BusinessObjects_HasNoForbiddenPackages`.
  4. Added negative fixture tests `Checker_Detects_Direct_Http_TransportPackage_In_BusinessObjects` and `Checker_Detects_Direct_Jwt_Package_In_BusinessObjects`.

### Finding 3 (High) — Direct package check failed to verify resolved transitive EF Core dependencies
- **Root cause:** `DependencyGraphChecker.ReadPackageReferences` only read direct `PackageReference` elements from `.csproj`. It did not verify whether the resolved package graph contained EF Core transitively.
- **Resolution:**
  1. Implemented `ParseResolvedPackages(string assetsJsonContent)` and `ReadResolvedPackages(string projectDirectoryOrAssetsPath)` using `System.Text.Json` to parse `targets` and `libraries` from `project.assets.json`.
  2. If `project.assets.json` does not exist, `ReadResolvedPackages` fails fast with `FileNotFoundException` and the actionable message: `"Run 'dotnet restore' first to generate resolved dependency assets."`
  3. Added `ForbiddenTransitiveBusinessObjectsPackagePrefixes = ["Microsoft.EntityFrameworkCore"]`.
  4. Added negative fixture test `Checker_Detects_Transitive_EFCore_In_ResolvedPackages` and `ReadResolvedPackages_ThrowsFileNotFound_WhenAssetsMissing`.
  5. Added production test `BusinessObjects_ResolvedPackageGraph_HasNoForbiddenDependencies`.

---

## Review-Fix Round 3 (2026-09-17) — Preserve Spatial Integration and Branch Scope (Historical: Commit 04cf314)

> [!NOTE]
> The assertions and evidence in this section reflect the historical state of commit `04cf314` on branch `anh` prior to merging branch `huy` via PR #1. At that commit snapshot, `docs/worklogs/P2-00-completion.md` was removed to maintain zero net diff into `develop`. Following the subsequent PR #1 merge (`da3a1aa`), P2-00 artifacts (including `docs/worklogs/P2-00-completion.md`) are now legitimately integrated into `anh` from branch `huy`.

### Finding 1 (High) — Branch scope polluted with P2-00 artifact (Historical snapshot at 04cf314)
- **Root cause:** Branch `anh` included commit `5b16560` which added `docs/worklogs/P2-00-completion.md`. That artifact belongs to P2-00 (Person 2 / branch `huy`), which maintains a separate version. Leaving it on `anh` pollutes the P1-00 merge diff into `develop` and causes an add/add merge conflict.
- **Resolution (at commit 04cf314):**
  - Removed `docs/worklogs/P2-00-completion.md` from branch `anh` via explicit cleanup commit `04cf314` without history rewrite (no rebase/reset/revert).
  - Verified `git diff develop..HEAD -- docs/worklogs/P2-00-completion.md` was completely empty at that snapshot (zero net merge diff into `develop`).
  - `git diff origin/anh..HEAD` recorded the deletion of the prematurely tracked file on `anh`.
  - Note: Following PR #1 (`da3a1aa`), `docs/worklogs/P2-00-completion.md` is now legitimately present on `anh` as an integrated P2-00 artifact from branch `huy`.

### Finding 2 (High) — AllowedBusinessObjectsPackages false-fails on P2-00 NetTopologySuite
- **Root cause:** Round 2 established a strict `AllowedBusinessObjectsPackages` allow-list containing solely `Microsoft.Extensions.Identity.Stores`. P2-00 legitimately introduces `NetTopologySuite` into `BusinessObjects` for spatial domain primitives and invariants. Without updating the allow-list, merging `anh` and `huy` would trigger an architecture test false-failure.
- **Resolution:**
  - Added negative fixture test `BusinessObjects_Allows_IdentityStores_And_NetTopologySuite` proving that `NetTopologySuite` was initially rejected by the allow-list (RED).
  - Added `NetTopologySuite` to `AllowedBusinessObjectsPackages` in `DependencyGraphChecker.cs` with explicit documentation attributing ownership to P2-00 for spatial domain invariants and geometry models.
  - Kept all forbidden package checks (EF Core, ASP.NET Core transport, JWT packages) and transitive EF Core gates strictly enforced.
  - Verified all 33 unit tests and 2 API tests turn GREEN.

---

## Final Re-review & Post-Merge Integration (HEAD da3a1aa)

Following Round 3, Pull Request #1 merged branch `huy` into `anh` (merge commit `da3a1aab343425a17886601a567cda6c3898ce7b`), integrating P2-00 spatial persistence work into branch `anh`.

### Integrated State
1. **PR #1 Integration:** P2-00 persistence foundation, spatial invariants, and `RoadGuardSystem.IntegrationTests` are now cleanly merged into branch `anh`.
2. **P2-00 Artifacts:** `docs/worklogs/P2-00-completion.md` is now legitimately present in the repository from branch `huy`. It is no longer an artifact to be deleted or cleaned up on `anh`.
3. **Solution Projects:** `RoadGuardSystem.slnx` now includes 8 projects, incorporating `tests/RoadGuardSystem.IntegrationTests`.
4. **Independent Verification:** Person 2 / Independent Reviewer inspected HEAD `da3a1aa` against baseline `develop`/`c48be5d` with zero new code findings.

### Independent Verification Evidence (HEAD da3a1aa)

| # | Command | Result | Details |
|---|---|---|---|
| 1 | `dotnet restore RoadGuardSystem.slnx` | PASS | All 8 projects restored successfully |
| 2 | `dotnet build RoadGuardSystem.slnx --no-restore --no-incremental` | PASS | **0 Warning(s), 0 Error(s)** |
| 3 | `dotnet test tests/RoadGuardSystem.UnitTests --filter "TaskId=P1-00" --no-build` | PASS | **Passed: 33**, Failed: 0, Skipped: 0 |
| 4 | `dotnet test tests/RoadGuardSystem.ApiTests --filter "TaskId=P1-00" --no-build` | PASS | **Passed: 2**, Failed: 0, Skipped: 0 |
| 5 | `dotnet test RoadGuardSystem.slnx --no-build` | PASS | **Passed: 78**, Failed: 0, Skipped: 0 (Unit: 33, Api: 2, Integration: 43) |
| 6 | `dotnet format RoadGuardSystem.slnx --verify-no-changes --no-restore` | PASS | Clean formatting verified across all 8 projects |
| 7 | `git diff --check develop..HEAD` | PASS | Clean whitespace and line endings verified |
| 8 | `git status --short` | PASS | Working tree clean |

---

## Files Changed and Current Integrated State (HEAD da3a1aa)

### P1-00-Owned Files
| Change | File | Purpose |
|---|---|---|
| Added | `.gitignore` | Excludes IDE state, build/test output, secrets, certificates, local databases, and logs |
| Added | `.gitattributes` | Normalizes text line endings and preserves intentional Markdown hard breaks |
| Added | `AGENTS.md` | Root rules file matching `.antigravity/AGENTS.md`, including branch workflow and agent Git command policy |
| Modified | `.antigravity/AGENTS.md` | Keeps hidden agent rules aligned with the root Git policy |
| Added | `global.json` | Pins SDK to 10.0.401 with `latestPatch` (F5) |
| Added | `Directory.Build.props` | Solution-level Nullable, deterministic analyzers, and warnings-as-errors gate; stale warning rationale removed in re-review |
| Modified | `RoadGuardSystem.BusinessObjects/RoadGuardSystem.aBusinessObjects.csproj` | Replaced EF Identity with `Microsoft.Extensions.Identity.Stores` and assigned EF Identity persistence to P2-10 (F1) |
| Modified | `RoadGuardSystem.BusinessObjects/Commons/EnumExtensions.cs` | Fixed 2x CS8600 nullable warnings (F4) |
| Modified | `RoadGuardSystem.DTOs/RoadGuardSystem.bDTOs.csproj` | Removed EF Core, HTTP, and Config packages (F2) |
| Modified | `RoadGuardSystem.Repositories/RoadGuardSystem.cRepositories.csproj` | Added direct EF Core package reference for PagedList.cs (F2) |
| Modified | `RoadGuardSystem.Repositories/Commons/ApiResult.cs` | Fixed 2x CS8618 warnings using `required` modifier (F4) |
| Modified | `RoadGuardSystem.API/Program.cs` | Added minimal ASP.NET Core startup and `public partial class Program {}` (F8) |
| Modified | `RoadGuardSystem.slnx` | Registered P1-00 test projects in solution |
| Modified | `tests/RoadGuardSystem.UnitTests/Architecture/DependencyGraphChecker.cs` | Preserves unmapped ProjectReferences; enforces BusinessObjects zero-reference and unmapped project rules; expands forbidden prefixes to transport/JWT; parses resolved packages from project.assets.json; includes NetTopologySuite in allow-list for P2-00 |
| Modified | `tests/RoadGuardSystem.UnitTests/Architecture/DependencyGraphTests.cs` | Added negative tests for unmapped ProjectReferences, direct transport packages, transitive EF Core, missing assets, and NetTopologySuite allow-list; updated BuildProductionGraph; added transitive EF production test |
| Added | `tests/RoadGuardSystem.UnitTests/Smoke/BusinessObjectsSmokeTests.cs` | Smoke tests for BusinessObjects assembly |
| Added | `tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj` | xUnit + WebApplicationFactory API test project |
| Added | `tests/RoadGuardSystem.ApiTests/Startup/ApiStartupTests.cs` | API startup & pipeline smoke tests |
| Updated | `docs/worklogs/P1-00-completion.md` | Documented P1-00 foundation, review rounds, post-PR #1 integration evidence, and final approval |

### P2-00 Files Integrated via PR #1 (Merge commit da3a1aa)
> [!NOTE]
> The following files were introduced from branch `huy` through PR #1. They are owned by Person 2 (P2-00); P1-00 does not claim ownership of these artifacts.

| Change | File | Ownership / Purpose |
|---|---|---|
| Modified | `RoadGuardSystem.BusinessObjects/RoadGuardSystem.aBusinessObjects.csproj` | P2-00: Added `NetTopologySuite` for spatial domain primitives |
| Added | `RoadGuardSystem.BusinessObjects/Spatial/SpatialConstants.cs` | P2-00: Spatial coordinate constants and SRID definitions |
| Added | `RoadGuardSystem.BusinessObjects/Spatial/SpatialValidation.cs` | P2-00: Domain validation logic for spatial geometries |
| Modified | `RoadGuardSystem.Repositories/RoadGuardSystem.cRepositories.csproj` | P2-00: Added `Microsoft.EntityFrameworkCore.SqlServer` and `NetTopologySuite` |
| Added | `RoadGuardSystem.Repositories/RoadGuardDbContext.cs` | P2-00: Initial DbContext with spatial extension wiring |
| Added | `RoadGuardSystem.Repositories/Extensions/RoadGuardPersistenceExtensions.cs` | P2-00: Service collection registration for SQL Server persistence |
| Added | `RoadGuardSystem.Repositories/Options/RoadGuardDatabaseOptions.cs` | P2-00: Strongly-typed database options |
| Added | `RoadGuardSystem.Repositories/Options/RoadGuardDatabaseOptionsValidator.cs` | P2-00: Data annotations & custom validation for database configuration |
| Modified | `RoadGuardSystem.slnx` | P2-00: Registered `RoadGuardSystem.IntegrationTests` project |
| Added | `docs/worklogs/P2-00-completion.md` | P2-00: Completion worklog for SQL Server spatial persistence foundation |
| Added | `tests/RoadGuardSystem.UnitTests/Spatial/SpatialInvariantTests.cs` | P2-00: Unit tests for spatial domain invariants |
| Added | `tests/RoadGuardSystem.IntegrationTests/*` (10 files) | P2-00: SQL Server and NetTopologySuite integration test suite |
---

## Negative-First Evidence

### 0. Review-correction checks (2026-09-16)

Before the review corrections, a PowerShell assertion command checked the Identity ownership comment, stale warning rationale, P1-00 status, F3/F5 status, and the global "all resolved" claim. It exited `1`; all six assertions reported `FAIL`. After the corrections, the same assertions plus the solution-wide warnings-as-errors assertion exited `0`; all seven reported `PASS`.

### 1. Injected Fixture Tests (Verifying checker capability)
The architecture tests evaluate synthetic project graphs to prove that violations are accurately trapped:
- `BusinessObjects -> Services` (fixture) => DETECTED
- `BusinessObjects -> Repositories` (fixture) => DETECTED
- `BusinessObjects -> API` (fixture) => DETECTED
- `BusinessObjects -> DTOs` (fixture) => DETECTED (Added for F1)
- `DTOs -> Services` (fixture) => DETECTED
- `DTOs -> API` (fixture) => DETECTED
- `Repositories -> Services` (fixture) => DETECTED
- `Repositories -> API` (fixture) => DETECTED
- `API -> Repositories` direct edge (fixture) => DETECTED
- Circular dependency A -> B -> A (fixture) => DETECTED
- Forbidden package in BusinessObjects (fixture) => DETECTED (Added for F1)
- Forbidden package in DTOs (fixture) => DETECTED (Added for F2)
- Clean graph fixture => NO VIOLATIONS

### 2. Actual Production Red-to-Green Evidence (F1 & F2)
Prior to removing forbidden packages from production project files, the new production package tests were executed:
- **Command:** `dotnet test tests/RoadGuardSystem.UnitTests --filter "TaskId=P1-00" --no-build`
- **Observed RED failure:**
  ```
  Failed BusinessObjects_HasNoForbiddenPackages
    Expected violations to be empty, but found:
    - RoadGuardSystem.aBusinessObjects references forbidden package 'Microsoft.AspNetCore.Identity.EntityFrameworkCore'
  Failed DTOs_HasNoForbiddenPackages
    Expected violations to be empty, but found:
    - RoadGuardSystem.bDTOs references forbidden package 'Microsoft.AspNetCore.Http'
  Total: 26, Failed: 2, Passed: 24
  ```
- **Implementation:**
  - Removed `Microsoft.AspNetCore.Identity.EntityFrameworkCore` from `BusinessObjects`, added `Microsoft.Extensions.Identity.Stores`.
  - Removed `Microsoft.AspNetCore.Http` and EF packages from `DTOs`.
- **Observed GREEN pass:**
  ```
  Passed! - Failed: 0, Passed: 26, Skipped: 0, Total: 26 - RoadGuardSystem.UnitTests.dll (net8.0)
  ```

### 3. Review-Fix Round 2 Red-to-Green Evidence (2026-09-17)
Prior to implementing fixes for Findings 1, 2, and 3, five negative fixture tests were executed against the initial checker logic:
- **Command:** `dotnet test tests/RoadGuardSystem.UnitTests --filter "TaskId=P1-00"`
- **Exit Code:** 1
- **Observed RED failure:**
  ```
  Failed Production graph policy rejects unmapped ProjectReference from BusinessObjects
    Expected violations not to be empty because an unmapped ProjectReference from BusinessObjects must be rejected by production graph policy.
  Failed Checker detects direct HTTP transport package in BusinessObjects fixture
    Expected violations not to be empty because transport packages such as Microsoft.AspNetCore.Http are forbidden in BusinessObjects.
  Failed Checker detects direct JWT package in BusinessObjects fixture
    Expected violations not to be empty because JWT packages are forbidden in BusinessObjects.
  Failed Checker detects transitive EF Core in resolved package graph fixture
    Expected violations not to be empty because transitive EF Core package in resolved graph must be detected.
  Failed ReadResolvedPackages fails with actionable message when restore assets are missing
    Expected a <System.IO.FileNotFoundException> to be thrown, but no exception was thrown.
  Total: 31, Failed: 5, Passed: 26
  ```
- **Implementation:**
  - `DependencyGraphChecker.BuildGraph`: maps known projects to logical names and preserves unmapped project references as raw identifiers instead of silently dropping them.
  - `DependencyGraphChecker.FindForbiddenEdges`: enforces domain boundary (BusinessObjects has zero project references) and architecture boundary (all project references across all projects must belong to recognized architecture layers).
  - `DependencyGraphChecker.ForbiddenBusinessObjectsPackagePrefixes`: expanded to include `Microsoft.AspNetCore`, `System.IdentityModel`, `Microsoft.IdentityModel`, and `System.Net.Http`. Added `AllowedBusinessObjectsPackages` explicit allow-list.
  - `DependencyGraphChecker.ParseResolvedPackages` & `ReadResolvedPackages`: parsed `project.assets.json` to detect resolved transitive EF Core packages; fails fast with `FileNotFoundException` and actionable `dotnet restore` instruction if assets are missing.
  - `DependencyGraphTests.cs`: updated `BuildProductionGraph` to use `BuildGraph`; updated `BusinessObjects_HasNoForbiddenPackages` with allow-list check; added production test `BusinessObjects_ResolvedPackageGraph_HasNoForbiddenDependencies`.
- **Observed GREEN pass:**
  ```
  Passed! - Failed: 0, Passed: 32, Skipped: 0, Total: 32 - RoadGuardSystem.UnitTests.dll (net8.0)
  ```

### 4. Review-Fix Round 3 Red-to-Green Evidence (2026-09-17)
Prior to modifying `AllowedBusinessObjectsPackages`, the new spatial package allow-list test was executed:
- **Command:** `dotnet test tests/RoadGuardSystem.UnitTests --filter "DisplayName=BusinessObjects allows both Identity Stores and NetTopologySuite spatial package"`
- **Exit Code:** 1
- **Observed RED failure:**
  ```
  Failed BusinessObjects allows both Identity Stores and NetTopologySuite spatial package [50 ms]
    Error Message:
     Expected allViolations to be empty because BusinessObjects allows Microsoft.Extensions.Identity.Stores (P1-00) and NetTopologySuite (P2-00) for spatial domain invariants, but found at least one item {"PACKAGE VIOLATION in BusinessObjects: 'NetTopologySuite' is not in the explicit allow-list"}.
  ```
- **Implementation:**
  - Updated `AllowedBusinessObjectsPackages` in `DependencyGraphChecker.cs` to include `"NetTopologySuite"` with an explicit ownership comment attributing it to P2-00 for spatial domain invariants and geometry calculations.
  - Retained strict forbidden prefixes (`Microsoft.EntityFrameworkCore`, `Microsoft.AspNetCore`, `System.IdentityModel`, `Microsoft.IdentityModel`, `System.Net.Http`) and transitive EF Core validation without relaxing any boundaries.
  - Removed `docs/worklogs/P2-00-completion.md` from branch `anh` to eliminate artifact pollution and prevent add/add merge conflicts with branch `huy`.
- **Observed GREEN pass:**
  ```
  Passed! - Failed: 0, Passed: 33, Skipped: 0, Total: 33 - RoadGuardSystem.UnitTests.dll (net8.0)
  ```

---

## Positive Evidence (Round 3 Historical Snapshot — Commit 04cf314)

All 8 gate commands executed cleanly:

| # | Command | Exit Code | Result | Timestamp (UTC+7) |
|---|---|---:|---|---|
| 1 | `dotnet restore RoadGuardSystem.slnx` | 0 | All projects up-to-date for restore | 2026-09-17 00:40 |
| 2 | `dotnet build RoadGuardSystem.slnx --no-restore --no-incremental` | 0 | **0 Warning(s), 0 Error(s)** | 2026-09-17 00:40 |
| 3 | `dotnet test tests/RoadGuardSystem.UnitTests --filter "TaskId=P1-00" --no-build` | 0 | **Passed: 33**, Failed: 0, Skipped: 0 | 2026-09-17 00:41 |
| 4 | `dotnet test tests/RoadGuardSystem.ApiTests --filter "TaskId=P1-00" --no-build` | 0 | **Passed: 2**, Failed: 0, Skipped: 0 | 2026-09-17 00:41 |
| 5 | `dotnet format RoadGuardSystem.slnx --verify-no-changes --no-restore` | 0 | Clean formatting verified | 2026-09-17 00:41 |
| 6 | `dotnet test RoadGuardSystem.slnx --no-build` | 0 | **Passed: 35**, Failed: 0, Skipped: 0 (Unit: 33, Api: 2) | 2026-09-17 00:41 |
| 7 | `git diff --check` | 0 | Clean whitespace verified | 2026-09-17 00:41 |
| 8 | `git diff --name-status origin/anh..HEAD` | 0 | Clean scope: M for checker/tests/completion, D for P2-00 artifact | 2026-09-17 00:42 |

### Git bootstrap evidence (Baseline)

| Command/check | Exit Code | Result | Timestamp (UTC+7) |
|---|---:|---|---|
| Pre-change policy assertions | 1 (expected) | Git repository, `.gitignore`, and agent Git policy all absent | 2026-09-16 21:02 |
| `git init -b main` | 0 | Empty repository initialized with `main` as the initial branch | 2026-09-16 21:05 |
| Local Git configuration | 0 | `pull.ff=only`, `fetch.prune=true`, `core.autocrlf=false`, `core.safecrlf=warn` | 2026-09-16 21:05 |
| Ignore/candidate validation | 0 | 46 candidates; no `.vs`, `bin`, `obj`, secret, certificate, database, or log path | 2026-09-16 21:05 |
| First `git diff --cached --check` | nonzero (expected) | Detected intentional Markdown hard breaks and two real whitespace-only `.csproj` lines | 2026-09-16 21:06 |
| Final `git diff --cached --check` | 0 | Clean after Markdown attribute and `.csproj` whitespace correction | 2026-09-16 21:06 |
| Restore/build/format/test gate | 0 | Build 0 warnings/errors; format clean; unit 26/26; API 2/2; total 28/28 | 2026-09-16 21:07 |
| `git commit -m "P1-00: establish executable foundation and Git workflow"` | 0 | Root commit `9293545`, 46 files | 2026-09-16 21:08 |
| Create `develop`, `anh`, and `huy` from `main` | 0 | All four local branches initially reference `9293545` | 2026-09-16 21:08 |

---

## Production Dependency Graph (Verified)

### Project References:
```
API (RoadGuardSystem.eAPI)
  └── Services (RoadGuardSystem.dServices)
        └── Repositories (RoadGuardSystem.cRepositories)
              └── DTOs (RoadGuardSystem.bDTOs)
                    └── BusinessObjects (RoadGuardSystem.aBusinessObjects)
                          └── (zero project references)

UnitTests ──> BusinessObjects (smoke tests)
ApiTests  ──> API (WebApplicationFactory<Program>)
```

### Package Reference Boundaries:
- `BusinessObjects`: Pure domain abstractions (`Microsoft.Extensions.Identity.Stores` for `IdentityUser<Guid>` / `IdentityRole<Guid>`, and `NetTopologySuite` for P2-00 spatial geometry/geography domain primitives). Zero EF Core dependencies (direct or transitive). Zero transport dependencies. Enforced via explicit allow-list and `project.assets.json` inspection.
- `DTOs`: Pure contracts. Zero EF Core, HTTP, or persistence dependencies.
- `Repositories`: `Microsoft.EntityFrameworkCore` (v8.0.17) for `PagedList.cs`. SQL Server and NetTopologySuite to be added in P2-00.

---

## Person 2 Re-review Checklist

- [x] F1: `BusinessObjects -> DTOs` forbidden rule added and tested
- [x] F1: `Microsoft.AspNetCore.Identity.EntityFrameworkCore` removed from `BusinessObjects`
- [x] F1: Production architecture test verifies no EF packages in `BusinessObjects`
- [x] F2: EF Core, HTTP, and Configuration packages removed from `DTOs`
- [x] F2: `Microsoft.EntityFrameworkCore` referenced directly by `Repositories`
- [x] F2: Architecture test verifies no EF or HTTP packages in `DTOs`
- [x] F3: Person 2 / independent diff review completed against baseline `develop`/`c48be5d` and reviewed HEAD `da3a1aa`; zero actionable code findings
- [x] F4: CS8600 (EnumExtensions) and CS8618 (ApiResult) fixed; non-incremental build has 0 warnings
- [x] F5: Accepted non-blocking residual risk (repository owner decision); mandatory follow-up assigned to P1-02 (ADR) and P2-01 (CI proof)
- [x] F6: Red-to-green test run documented with actual observed failure
- [x] F7: Root `AGENTS.md` created and identical to `.antigravity/AGENTS.md`
- [x] F8: Swagger bootstrap scope clarified
- [x] Finding 1 (Round 2): Unmapped `ProjectReference` from `BusinessObjects` or other layers preserved and rejected
- [x] Finding 2 (Round 2): Direct transport and JWT packages forbidden in `BusinessObjects`; explicit allow-list enforced
- [x] Finding 3 (Round 2): Resolved package graph (`project.assets.json`) verified free of transitive EF Core dependencies
- [x] Finding 1 (Round 3): `docs/worklogs/P2-00-completion.md` removed from pre-PR #1 `anh` commit; post-PR #1 integrated cleanly from `huy`
- [x] Finding 2 (Round 3): `NetTopologySuite` included in `AllowedBusinessObjectsPackages` with P2-00 spatial ownership comment; verified red-to-green
- [x] Round 3 gate: All 8 commands pass with 0 errors and 0 warnings (35/35 tests passed)
- [x] Post-merge integration gate (HEAD da3a1aa): All 8 projects build cleanly, 78/78 tests passed, format verified, git diff check verified

### F5 Disposition (Repository Owner Decision)
- **Status:** Accepted non-blocking residual risk.
- **Rationale:** Temporarily accepts SDK 10.0.401 for building the existing `net8.0` target. Reclassified from blocking to accepted non-blocking follow-up to eliminate the circular dependency (both P1-02 ADR and P2-01 CI proof require P1-00 foundation to be approved first).
- **Follow-up tracking:**
  - P1-02 must document the SDK/runtime baseline decision in `docs/adr/001-backend-boundary.md`.
  - P2-01 must reproduce and verify the pinned toolchain in the GitHub Actions CI workflow.

---

## Review handoff

- **Known gaps:** None blocking P1-00. F5 follow-up tracked under P1-02 and P2-01.
- **Residual risks:** SDK 10.0.401 / net8.0 toolchain combination accepted as non-blocking residual risk by repository owner decision; formal ADR and CI proof to follow under P1-02/P2-01.
- **Reviewer findings and resolution:**
  - Round 2 architecture false greens (Findings 1–3) resolved with red-to-green test evidence.
  - Round 3 spatial integration (Finding 2) resolved with NetTopologySuite allow-list and P2-00 attribution.
  - Post-PR #1 integration: Branch `huy` merged into `anh` at `da3a1aa`; P2-00 worklog and tests cleanly integrated; full solution passes (78/78 tests).
  - Independent diff review completed by Person 2 with zero actionable code findings. F3 closed.
  - F5 dispositioned as accepted non-blocking residual risk to avoid circular dependencies.
- **Exact next action:** Repository owner integrates `anh` into `develop` according to integration gate rules.
- **Final status:** Approved / Done
