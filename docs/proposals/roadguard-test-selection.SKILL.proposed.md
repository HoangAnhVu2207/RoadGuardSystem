---
name: roadguard-test-selection
description: Use when selecting the sufficient RoadGuard build and test breadth from changed paths and behavior risk.
---

# RoadGuard test selection (proposed)

Status: Proposed. Target: `.agents/skills/roadguard-test-selection/SKILL.md`. This draft does not become an active skill until both owners approve ADR 004 and task A0.

## Inputs

Before selecting commands, read:

1. The approved scope card and assigned planning row.
2. `git status --short --branch` and the exact changed paths.
3. The dependency direction from ADR 001 and the accepted form of ADR 004.
4. Existing valid evidence whose source, configuration, dependencies, selection, and environment have not changed.

## Global rules

- Build once, then test with `--no-build --nologo -v q`.
- Choose exactly one test breadth: focused, affected-project, or full-solution.
- Do not run narrower breadths first when a broader breadth is already required.
- Routine local runs do not collect coverage. Coverage belongs to CI/release unless the approved scope explicitly requests it.
- A test run with zero discovered tests, a missing required project, or skipped required tests fails the gate.
- Reuse passing evidence until an edit invalidates it.
- After a change, rerun only the checks invalidated by that change.
- If the same failure survives two fix attempts, stop and report the command plus the shortest diagnostic.

## Selection matrix

| Changed area | Build | One test breadth | Additional proof |
|---|---|---|---|
| Documentation/proposal only | None | None | Relevant docs/link/diff verifier and `git diff --check` |
| `BusinessObjects` invariant or pure calculation | BusinessObjects or UnitTests project | Focused UnitTests | Include boundary and invalid-input cases |
| DTO shape/validation | DTOs or API project | Focused UnitTests or ApiTests, based on consumer | Confirm serialized contract when public |
| One Service use case | Services project | Focused UnitTests; use focused ApiTests when HTTP contract is affected | Characterize authorization/idempotency before refactor |
| One controller/endpoint | API project | Focused ApiTests | Run matching `Http/*.http` request against real host |
| One repository query without SQL-specific behavior | Repositories project | Focused IntegrationTests | Assert projection and scope; no EF entity leak |
| SQL constraint, mapping, rowversion, transaction, outbox, migration | Repositories/IntegrationTests project | Focused IntegrationTests on SQL Server | Migration lifecycle only when schema/migration changes |
| Shared API middleware/auth/ProblemDetails/DI | API project | Affected-project ApiTests | Real unauthenticated/authenticated smoke as applicable |
| Shared Services or repository runtime behavior | Owning production project | Affected-project suite for the one affected test project | State why multiple features can be affected |
| Project references, common architecture rules, release/integration gate | Solution | Full solution | Explicit owner or release/integration scope required |
| CI workflow only | None locally unless workflow verifier requires it | CI verifier selected by scope | Do not claim hosted CI proof from local YAML checks |

## Command templates

Build the changed project once:

```powershell
dotnet build <project.csproj> -nologo -v q -clp:ErrorsOnly
```

Focused breadth:

```powershell
dotnet test <test-project.csproj> --no-build --nologo -v q --filter "FullyQualifiedName~<FeatureOrTestClass>"
```

Affected-project breadth:

```powershell
dotnet test <test-project.csproj> --no-build --nologo -v q
```

Full-solution breadth, only for an approved integration/release gate:

```powershell
dotnet test RoadGuardSystem.slnx --no-build --nologo -v q
```

TRX for timing or failure evidence, when the scope asks for it:

```powershell
dotnet test <test-project.csproj> --no-build --nologo -v q --logger "trx;LogFileName=<name>.trx" --results-directory TestResults/<scope>
```

Do not add `--collect:"XPlat Code Coverage"` to routine local commands.

## Baseline evidence for future S1-S5 updates

P1-71A on 2026-09-21 measured the second local run without coverage:

| Project | Warm build | Test result | Wall time |
|---|---:|---:|---:|
| UnitTests | 1.730 s | 133 passed, 0 failed, 0 skipped | 1.392 s |
| IntegrationTests | 5.294 s | 258 passed, 1 failed, 0 skipped | 134.727 s |
| ApiTests | 6.177 s | 75 passed, 0 failed, 0 skipped | 11.355 s |

The Integration failure reproduced twice: `P2-23: unknown commit reassignment returns the durable replay`, expected `Replayed` but found `Executed`. This proposal does not treat the Integration suite as passing. Container counts were not verified because Docker Desktop returned no retained events during the measurement; future S2 acceptance must use reliable live telemetry.

## Completion report

Report:

- changed paths and selected mapping row;
- build command and result;
- the single selected breadth and why it is sufficient;
- discovered, passed, failed, and skipped counts;
- coverage state;
- reused evidence and invalidated checks;
- SQL/container environment when relevant;
- remaining risk and any unverified claim.
