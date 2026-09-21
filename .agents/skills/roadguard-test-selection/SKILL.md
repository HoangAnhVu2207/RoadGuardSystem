---
name: roadguard-test-selection
description: Use when selecting the sufficient RoadGuard build and test breadth from changed paths and behavior risk.
---

# RoadGuard Test Selection

Read the approved scope card, exact changed paths, ADR 001, ADR 004, and valid unchanged evidence before selecting commands.

## Rules

- Build once, then test with `--no-build --nologo -v q`.
- Choose exactly one breadth: focused, affected-project, or full-solution.
- Routine local commands do not collect coverage; coverage is CI/release-only unless an approved scope explicitly measures it.
- Zero discovered tests, a missing required project, or skipped required tests is a failed gate.
- Stop after two failed fix attempts for the same problem and report the shortest diagnostic.

## Selection

| Changed area | Build | Test breadth |
|---|---|---|
| Documentation/tooling | None | Relevant static verifier only |
| Entity invariant or pure calculation | Owning project | Focused UnitTests |
| Service policy or controller endpoint | Changed production project | Focused API or UnitTests; run API smoke for endpoints |
| SQL mapping, transaction, outbox, rowversion, migration | Repositories | Focused IntegrationTests on SQL Server |
| Shared API/Service/Repository runtime behavior | Owning production project | Affected test-project suite |
| Project references, CI, release | Solution | Full solution only when explicitly approved |

## Commands

```powershell
dotnet build <project.csproj> -nologo -v q -clp:ErrorsOnly
dotnet test <test-project.csproj> --no-build --nologo -v q --filter "FullyQualifiedName~<Feature>"
dotnet test <test-project.csproj> --no-build --nologo -v q
dotnet test RoadGuardSystem.slnx --no-build --nologo -v q
```
