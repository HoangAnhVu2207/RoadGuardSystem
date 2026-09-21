# RoadGuard discovery

Use the canonical repository rules at `../../AGENTS.md`.
Use accepted ADR 004 with ADR 001 for backend layer decisions.
For endpoint planning, implementation, smoke verification, selective tests, or bug fixes, load `../skills/roadguard-endpoint-delivery/SKILL.md`.
For selecting test breadth, load `../skills/roadguard-test-selection/SKILL.md`.
Repository rules and accepted ADRs win over third-party skill defaults.

`RoadGuardSystem.BusinessObjects` is entity-only: domain entity files, `Common/Enums.cs`, and enum extension files in `Common/Extensions/`. Keep entity-local invariants there; put options, validators, sanitizers, spatial/persistence helpers, infrastructure constants, interfaces, EF, HTTP, configuration, clocks, and random generators in the owning Service or Repository folder.

`RoadGuardSystem.DTOs` is contract-only: one public request/response DTO per file, with data properties/records and validation attributes only. Do not put business/computed logic, data access, clocks/random generation, or Repository/Service/EF/HTTP references in DTOs; mapping belongs to Services.

`RoadGuardSystem.Repositories` owns DbContext, EF query/write, mappings, transactions, idempotency, rowversion/concurrency, outbox, storage, and persistence backstops. Services inject its concrete persistence/read models only through interfaces. Repositories return facts and never decide authorization, scope, role/status workflow, calculations, or HTTP/stable error codes. ADR 001 allows its DTO project reference, but Repository `.cs` source must never reference `RoadGuardSystem.DTOs`; keep the architecture test green.

`RoadGuardSystem.Services` owns use-case orchestration, business/state rules, authorization/scope decisions, stable business result codes, and DTO mapping. Controllers call `IService`; Services call persistence/read models through repository interfaces. Services must never reference `HttpContext`, MVC/controller result types, EF Core, or `RoadGuardDbContext`; keep the service architecture test green.

Use the verified direct project-reference matrix: `BusinessObjects -> (none)`, `DTOs -> BusinessObjects`, `Repositories -> BusinessObjects, DTOs` (ADR 001 exception; source DTO use remains forbidden), `Services -> Repositories`, `API -> Services`. Do not add a direct reference merely because a type is transitively available.

Before creating a source file, use `rg --files` in the target layer, identify the closest existing type, and include the exact path in approved scope. Repository/Service technical concerns use root-level sibling folders (`Extensions`, `Options`, `Factories`, `Generators`, `Configurations`, `Concurrency`, `Transactions`, `Storage`, `Seeding`, `Migrations`); feature seams use the existing `Interfaces/<Domain>/` and `Implementations/<Domain>/` layout. Do not create catch-all `Helpers`, `Utils`, `Common`, `Shared`, `Models`, `Managers`, another interface/implementation tree, root-level source files, parallel domains, speculative files, duplicate compatibility types, or empty directories. Remove empty legacy folders in the same approved relocation scope.
