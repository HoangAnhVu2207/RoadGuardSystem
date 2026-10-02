# RF-00 — reproducible baseline and survey boundary

## Identity

- **Branch:** $branch
- **HEAD:** $head
- **HEAD record:** $(git log -1 --format='%h %ci %s')
- **Survey timestamp:** $ts
- **Working-tree status at survey start:**
`
M RoadGuardSystem.Repositories/Implementations/Surveys/SurveyV2PersistenceService.cs
 M planning/CROSS_OWNER_HANDOFFS.md
 M planning/V2/Execution/ANH-02-project-survey.md
 M planning/V2/Governance/README.md
 M tests/RoadGuardSystem.IntegrationTests/Surveys/P2V2SurveyScopeConcurrencyTests.cs
?? RoadGuardSystem.Repositories/Implementations/Surveys/SurveyV2PersistenceService.Dataset.cs
?? planning/V2/Governance/COV-BASELINE-Q11-CONTRACT.md
`

The existing dirty files belong to concurrent work and remain untouched. The two untracked files are also preserved. Reports in this folder are new survey artifacts and are not evidence that those changes are complete.

## Scope card

- **Goal:** assess the current system and produce a controlled, module-by-module refactor and documentation replacement plan.
- **In scope:** repository inventory, dependency and runtime boundaries, evidence classification, refactor sequencing, contract/ADR/planning design, proposed agent guidance.
- **Out of scope:** production edits, schema or migration edits/application, active API contract edits, CI changes, package/framework upgrades, data mutation, deletion of legacy docs, push/merge/reset/clean.
- **Shared hotspots:** RoadGuardDbContext, EF configurations and migrations/snapshot, API middleware/error envelope, OpenAPI/Postman, DI registration, seed fixtures, project references, and all files already modified in the initial status.
- **Side effects:** documentation files under planning/refactor/ only; no package, migration, database, runtime, or external-service side effect.

## Current verified inventory

- Solution projects: API, BusinessObjects, DTOs, Repositories, Services, three test projects, and Seeder (g --files -g '*.csproj').
- Target/runtime: project files target 
et8.0; global.json selects SDK 10.0.401 with latest-patch roll-forward; Directory.Build.props enables nullable, analyzers, deterministic builds, and warnings-as-errors.
- Layer references observed in project files: API → Services → Repositories → DTOs/BusinessObjects; DTOs → BusinessObjects; Seeder → Repositories. Test projects reference the layers they exercise.
- Approximate source size from Get-ChildItem -Recurse -Filter '*.cs': API 43, BusinessObjects 69, DTOs 98, Repositories 319, Services 99, tests 143, Seeder 7. Counts include current checkout files and are planning indicators, not complexity measures.
- API has 17 controllers. Routes use versioned pi/v{version:apiVersion} patterns plus unversioned /health (g -n '\[Route|\[Http...' RoadGuardSystem.API/Controllers).
- RoadGuardDbContext exposes identity, audit/outbox/idempotency, files/uploads, projects/roads/warranty, surveys/flights, defects/inspection, processing/validation entities; 38 non-designer migration files were counted in RoadGuardSystem.Repositories/Migrations.
- Repositories and Services currently have populated Interfaces/<Domain>/ and Implementations/<Domain>/ trees. API is organized by host concerns (Controllers, Extensions, Middlewares, workers, auth/authorization). BusinessObjects and DTOs are domain-foldered.
- Runtime composition is in RoadGuardSystem.API/Program.cs: controllers, API platform services, seeding, optional upload verification worker, exception/status middleware, auth, health checks, Swagger in development, and controller mapping.
- Tests include architecture/dependency-graph tests, unit tests, SQL Server integration tests and API tests using WebApplicationFactory/Testcontainers evidence where configured. A test pass is not treated as business correctness without contract/requirement evidence.

## Evidence sources and commands

- Git identity/status: git status --short --branch, git rev-parse HEAD, git log -1 --format='%h %ci %s', git diff --stat.
- Source tree: g --files, g -l 'class .*Controller' RoadGuardSystem.API, g -n 'class RoadGuardDbContext|DbSet<' RoadGuardSystem.Repositories/RoadGuardDbContext.cs.
- Dependency graph: g -n '<ProjectReference|<PackageReference|<TargetFramework' **/*.csproj.
- Current docs/index: docs/README.md, docs/RoadGuard_Project_Scope.md, docs/RoadGuard_Backend_Scope.md, docs/architecture/backend-structure.md, docs/adr/*.md, docs/diagram/V2/**, docs/postman/**, planning/V2/**.
- Runtime composition: RoadGuardSystem.API/Program.cs and API/repository/service extension classes.

## Important current-vs-target distinction

The repository contains extensive V2 design, contracts, Postman examples and historical completion records. They document intended or previously accepted behavior, but do not prove every endpoint/entity/role is implemented correctly today. The next phase must reconcile each module against source, migrations, tests, and runtime evidence before structural movement.
