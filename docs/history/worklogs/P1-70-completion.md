# P1-70 - Lightweight Codex workflow reset

Status: Done
Owner/branch: Person 1 / `anh`
Approved: repository owner, 2026-09-20, option 1 (tooling first; production migration later)
Baseline: `f26055b`; preserve the existing P1-11 working-tree changes.

## Scope

In scope: rebuild root agent rules and one endpoint-delivery skill; remove tracked `.antigravity` compatibility files; align both plan policy sections and shared prompts; add the two requested compiler properties; replace obsolete agent verifier assumptions; keep every historical `Done` task and worklog unchanged.

Out of scope: production endpoint conversion, Minimal API wiring, direct `DbContext` use in API, entity/schema/migration/package changes, P1-11 or P2-01 implementation, Git integration, commit, push or deployment.

Exclusive paths: `AGENTS.md`, `.agents/**`, `.antigravity/**`, `Directory.Build.props`, `docs/prompts/RoadGuard_Task_Workflow.md`, `docs/diagram/RoadGuard_Task_Log_Template.md`, `tests/Tooling/Verify-AgentSetup.ps1`, retirement of `tests/Tooling/Verify-AntigravitySetup.ps1`, prospective workflow sections in both plans, and this worklog.

Conflict note: `planning/RoadGuard_Plan_Person_1.md` and `docs/worklogs/P1-11-completion.md` already contain owner changes. P1-70 preserves them and edits only its own row plus prospective policy. P2-01 owns CI behavior, not these files.

## Four slices

1. Record P1-70 scope and transition without changing historical `Done` evidence.
2. Replace root rules and the old skill suite; retire `.antigravity`.
3. Align prompts, plans and compiler properties with scope-first endpoint delivery.
4. Update verifiers; run link/discovery/planning/whitespace checks and a quiet API-project build.

## Verification record

No artificial failing-test phase is required for documentation/tooling work under the owner-approved workflow.

| Command | Exit | Result |
|---|---:|---|
| skill creator `quick_validate.py` | 0 | New `roadguard-endpoint-delivery` skill is valid. |
| `pwsh -NoProfile -File tests/Tooling/Verify-AgentSetup.ps1` | 0 | Compact rules, one rebuilt skill, retired `.antigravity`, plans, prompt, compiler properties and MCP config pass. |
| `pwsh -NoProfile -File tests/Tooling/Verify-AgentSetup.ps1 -SelfTest` | 0 | Injected verifier failure is detected. |
| `pwsh -NoProfile -File tests/Documentation/Verify-P102Docs.ps1` | 0 | Existing domain/docs/dependency contracts and historical evidence pass. |
| `pwsh -NoProfile -File tests/Documentation/Test-P203Planning.ps1` | 0 | Nine planning regression scenarios pass. |
| `git diff --check` | 0 | No whitespace error; Git reports only expected LF-to-CRLF notices for two PowerShell files. |
| `dotnet build RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj -nologo -v q -clp:ErrorsOnly` | 1 | Recommended analyzers expose existing CA1805, then CA1512/CA1000/CA1861 in BusinessObjects, DTOs, Repositories and a shared migration. |

## Blocker and resume point

Two configuration attempts did not make the build green without touching production. The remaining safe choices are: (A) keep the requested global `Recommended` mode and demote the four known baseline rule IDs `CA1805;CA1512;CA1000;CA1861` from errors to warnings, or (B) expand scope to refactor production APIs and an already shared migration. Option A is recommended because it preserves the phase-1 no-production boundary; all other Recommended diagnostics remain errors. Resume by recording the owner's choice, setting P1-70 to `In Progress`, applying that bounded decision and rerunning the build plus all passing checks above.

Owner decision, 2026-09-20: approved option A with the reply `Dong y`. `CA1805`, `CA1512`, `CA1000` and `CA1861` remain visible warnings; they alone are excluded from warnings-as-errors. No production or migration file is changed by this decision.

Owner synchronization decision, 2026-09-20: approved test-project-only baseline warnings `CA1305`, `CA1707`, `CA1822`, `CA1854` and `CA2219` after the full test build exposed 299 existing diagnostics (289 were xUnit-style names under CA1707). The condition applies only when `MSBuildProjectName` ends with `Tests`; production projects retain warnings-as-errors for these rules.

## Completion

- API-project build rerun: exit 0, 13 approved baseline warnings, 0 errors.
- New skill validation: pass.
- Agent setup verifier and injected-failure self-test: pass.
- Existing documentation/dependency verifier: pass.
- Planning regression suite: 9/9 scenarios pass.
- Managed files: all at or below 500 lines; `AGENTS.md` is 32 lines; one skill directory remains; `.antigravity` is absent.
- `.http` smoke: N/A because no endpoint or runtime behavior changed.
- Full runtime tests before integration: `dotnet test RoadGuardSystem.slnx --no-build -nologo -v q` passed 346/346 with zero failures/skips (Unit 124, Integration 166, API 56). The preceding build-enabled run compiled the current analyzer configuration successfully.
- Git at task completion evidence time: no files staged. The owner subsequently authorized commit, merge and push for `anh`/`huy`; `main` remains explicitly out of scope.
