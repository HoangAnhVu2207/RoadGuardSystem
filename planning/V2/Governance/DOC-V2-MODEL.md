# DOC-V2-MODEL

## Task metadata

- Owner/branch: `anh` / `anh`
- deliveryStatus: `DONE`
- contractStatus: `PROPOSED_DELTA`
- implementationStatus: `PARTIAL`
- verificationStatus: `DOCS_PASS`
- sourceCheckpoint: base `49c7eae`; DbContext/config/migrations read 2026-09-28

## Source evidence

| Source | Heading / ID | Evidence used | Revision/checkpoint |
|---|---|---|---|
| V2 Data Dictionary | §§3, 9; DD-C01..20 | Target concepts, relationships and invariants | 28/09 working tree |
| V2 decision register | D02-D15, D20-D22, 32A-43A | Business authority and remaining technical gates | `V2-ALIGN-2026-09-28` |
| `RoadGuardDbContext.cs` | DbSet inventory/ApplyConfigurationsFromAssembly | Current EF model entry points | base `49c7eae` |
| `BusinessObjects/*` | entity classes read for User/Project/Road/Survey/Inspection/Defect/Processing/Warranty/File | Current entity facts | base `49c7eae` |
| `Repositories/Configurations/*` | matching entity configurations | Current mapping evidence | base `49c7eae` |
| `Repositories/Migrations/*` | latest migration `20260921125553_AddP230FlightSurveyFileSchema` | Current migration history | base `49c7eae` |
| PF/SQ/FR | PF-03..08, SQ-01..06, FR state guidance | Logical transitions | 28/09 working tree |

## Outputs

- `docs/diagram/V2/03_Data/02_ERD_V2.md`
- `docs/diagram/V2/03_Data/03_Domain_Model_V2.md`
- `docs/diagram/V2/03_Data/04_Data_Model_Code_Map.md`
- `docs/diagram/V2/05_Technical/07_State_Machines_V2.md`
- `docs/diagram/V2/05_Technical/08_Component_Layer_V2.md`
- Updated DD/index/README and delivery manifest.

## Acceptance criteria

- Current EF/entity/config/migration evidence is separated from target/proposed concepts.
- ERD/state transitions include actor, scope, evidence and remaining gate semantics.
- Policy framework, ReportDefectLink, RepairAttempt, manifest/artifact/receipt, sync/handover/rescue, curing/traffic and legal hold are not falsely claimed as current runtime entities.
- No migration, entity rename, enum renumber, database or seed change.

## Delivery status

`DONE`

## Completion history

### 2026-09-28 - DONE

- Scope/result: produced logical V2 ERD, domain/state model, current-to-target code map and component boundary; updated DD stale decision wording and delivery/index links.
- AC: current `DbSet`/configuration/migration evidence is mapped; target gaps are explicitly `PROPOSED_DELTA`; state machines separate defect verification, repair attempt, curing/traffic release and acceptance; no migration generated.
- Verification: manifest recomputed and checked; `validate_package.py` structural checks PASS; `check_contracts.py` hash unchanged; `test_contract_guard.py` PASS; `git diff --check` PASS.
- Side effects: docs/manifest only; no C# source, package, migration, DB, seed or external service action; uncommitted.
- Unverified: live SQL schema/application, Mermaid rendering, provider/geometry behavior, target concepts and runtime transitions.
