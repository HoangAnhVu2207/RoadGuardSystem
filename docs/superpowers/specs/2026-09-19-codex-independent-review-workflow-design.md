# Codex Implementation and Independent Review Workflow Design

Date: 2026-09-19  
Task: P1-07  
Owner: Repository owner / Person 1 tooling  
Status: Owner authorized implementation including Lean TDD refinement on 2026-09-19; independent acceptance pending.

## Purpose

Replace the prospective Antigravity implementation role with a Codex implementation role. A different Codex task performs mandatory acceptance so that the implementer cannot approve its own work. After every initial implementation or fix round, the implementer returns a complete review packet and a ready-to-run reviewer prompt.

The change also hardens repository ignore rules so common local secrets, deployment-user settings, test artifacts, packages and database backups are not accidentally staged. Shared source, agent rules, CI configuration and safe development configuration remain visible to Git.

## Role Model

### Codex Implementer

The Codex Implementer may assign and execute one authorized plan task for the applicable Person. It records the acceptance contract before production edits, follows negative-first testing, implements, runs required checks, performs task-owner self-review and fixes its own findings. It may move the task only to `Ready for review` or `Blocked`; it cannot record acceptance or `Done`.

When an independent reviewer returns `Changes requested`, the same implementation task resumes as `In Progress`, fixes only the stable finding IDs and agreed scope, reruns affected gates and emits a new review packet.

### Independent Codex Reviewer

The reviewer runs in a separate Codex task/session and must not have authored the submitted artifacts. It reconstructs scope from the repository, task worklog and exact submitted revision/diff instead of relying on conversational memory. It may inspect and run checks, record findings and update task-scoped review/status metadata. It must not fix production code or tests during acceptance.

Only this reviewer can record `Done`. A new implementation change after acceptance invalidates acceptance for the affected behavior and requires another independent review.

## Workflow

1. Prepare or validate the task assignment in the existing Person plan and worklog.
2. Codex Implementer changes status to `In Progress`, declares exact files and performs negative-first implementation.
3. Codex Implementer runs required checks and self-reviews authorization, transitions, immutability/versioning, idempotency, concurrency, audit, tests, secrets and conflicts as applicable.
4. Codex Implementer records evidence, changes status to `Ready for review`, freezes submitted artifacts and returns the review packet.
5. The repository owner starts a separate Codex task using the packet's reviewer prompt.
6. Independent Codex Reviewer returns `Changes requested`, `Blocked` or `Done` and records the review round where authorized.
7. For requested changes, the original implementer resumes, fixes stable finding IDs and repeats steps 3-6.

No workflow step authorizes merge, rebase, cherry-pick, push, deployment or starting another business task.

## Review Packet Contract

Every implementation and fix handoff contains:

- task ID, Person, assigned branch and current status;
- starting baseline and exact submitted commit or working-tree diff identity, including relevant untracked files;
- acceptance criteria and trace coverage;
- complete changed-file list and ownership/conflict result;
- commands, exit codes, environment, timestamps, test counts and failed/skipped counts;
- negative-first chronology and positive-path evidence;
- implementer self-review findings and resolutions;
- open gaps, blockers, residual risks and unexecuted environments;
- stable reviewer finding IDs addressed in a fix round;
- worklog path; and
- a ready-to-run prompt instructing a separate Codex task to use the appropriate RoadGuard review skills and review the exact artifact without implementing fixes.

A packet missing artifact identity or required check evidence cannot be accepted merely because the summary says tests passed.

## Repository Policy Synchronization

Prospective policy is updated consistently in canonical `AGENTS.md`, its compatibility mirror, both Person plans, the shared workflow prompt, completion template, maintained delivery skill and review skills. Historical P1-06 and earlier evidence remains unchanged as history even when it names Antigravity as the implementer used at that time.

Legacy paths such as `Antigravity_Completion_Log_Template.md` and `.antigravity/skills/.../antigravity-handoff.md` remain in place to avoid breaking links. Their maintained content becomes tool-neutral or explicitly describes the new Codex roles.

Verifier changes distinguish current policy from historical prose. Existing executable checks cover role consistency, links and planning behavior. Scenario review rejects same-session self-acceptance, missing independence and incomplete packets; text checks cannot prove session independence. Historical logs are not rewritten.

## Git Ignore Design

Retain current rules for IDE state, .NET outputs, `TestResults`, coverage, `.env*`, private keys, local databases and logs. Add targeted rules for:

- `appsettings.Local.json` and case-equivalent multi-environment local overrides;
- ASP.NET user-secret folders or local secret directories located inside the repository;
- private HTTP-client environment files;
- user-specific publish profiles/settings, while allowing intentionally shared publish definitions to be force-added or explicitly unignored later;
- stray `*.trx`, `*.nupkg` and `*.snupkg` outputs;
- SQL Server backup/export/dump artifacts such as `*.bak` and `*.bacpac`;
- repository-root publish/coverage-report output directories; and
- user-specific IDE settings not already covered.

Do not ignore `.agents/`, `.antigravity/`, `.github/`, `.env.example`, `RoadGuardSystem.API/Properties/launchSettings.json`, `RoadGuardSystem.API/appsettings.Development.json`, migrations, SQL source scripts or documentation. Ignore rules do not untrack an already tracked file and do not replace pre-commit status/diff/secret inspection.

## Verification

### Approved Lean TDD refinement

Follow the canonical P1-07 migration and proportional verification sections in AGENTS.md. Keep historical Done evidence and review in-flight submissions unchanged; transfer the next implementation/fix round after writer handoff. New tasks use Codex Implementer and a separate Codex Reviewer.

Use a small negative/positive/implementation cycle, narrow tests while editing, affected projects after a slice, and submission checks once per covered content/environment state. Production submission still requires restore, non-incremental build, format and affected tests. Full suite applies to shared architecture/DI/schema/packages/security/cross-project changes and explicit task/CI/integration requirements. Review reruns new regressions/high-risk checks and validates remaining evidence, rerunning when covered inputs/environment or trust change. Required SQL/security/CI and develop integration gates remain intact.

One worklog stores compact evidence. Prompts link to it. No extra assignment session or repeated approval for routine choices is required. Keep the existing two plans; execution steps belong in the P1-07 worklog. A generic command-dispatch script is deferred in favor of the shared prompt's explicit command recipe.

Documentation/tooling verification will include:

- existing documentation verifier in normal and negative modes;
- Antigravity/Codex setup verifier and root/mirror equality;
- skill frontmatter and local-link validation;
- stale prospective-role scan with historical-path exclusions;
- scenario checks for implementation, fix, blocked and independent-review handoffs;
- a `git check-ignore --no-index` matrix proving sensitive/local examples are ignored;
- negative visibility checks proving required shared configuration is not ignored;
- tracked-file inventory/secret-pattern inspection;
- `git diff --check`, explicit-path diff review and clean staging inspection.

Runtime build and product tests are not evidence for prose/ignore behavior and are N/A unless implementation unexpectedly touches runtime artifacts.

## Acceptance Boundaries

P1-07 is complete only after a separate Codex task verifies the exact submitted artifacts and records the acceptance round. Local implementation success is `Ready for review`, not `Done`. No commit or remote publication is part of this request.
