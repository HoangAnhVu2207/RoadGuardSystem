# COV-BASELINE-Q11-CONTRACT - Coverage and per-band baseline decision

- Owner/branch: Anh / anh (contract coordination); business decision: project owner; API consumer: Huy
- deliveryStatus: TODO
- contractStatus: PROPOSED_DELTA
- implementationStatus: NOT_ENABLED
- verificationStatus: NOT_RUN
- dependencyType: business-contract-and-schema
- sourceCheckpoint: base HEAD 2efc8a5775f834c7f0fe37cc0ce703011649e1f1; canonical OpenAPI SHA-256 ADA7F48F522C0C0DBDACE21F483224A00FC4C76264A9A61D12F3E2211ECCE665

## Goal

Resolve Q11 and approve a physical per-band coverage/baseline model before ANH-02 or HUY-02 claims `getDatasetCoverage` or `confirmBaseline` as implemented. This task records decisions and a migration-ready contract; it does not itself authorize a migration or deployment.

## Source evidence

| Label | Source / heading or ID | Invariant or gap |
|---|---|---|
| TARGET_DOCUMENTED | `docs/diagram/V2/02_Requirements/01_FRD_SRS.md` / FR-26..30; `02_Business_Rules.md` / BR-40, BR-41; `05_User_Stories_Acceptance_Criteria.md` / US-04-AC-02, US-25-AC-02/03 | Baseline is segment/band specific; position, quality and coverage are separate; missing evidence is UNKNOWN. |
| TARGET_DOCUMENTED | `docs/diagram/V2/03_Data/01_Data_Dictionary.md` / 3.3a, 9.6; `02_ERD_V2.md` / Target V2 modules | `SurveyCoverageRequirement`, `SurveyCoverageResult`, `SurveyVideoInterval` and `SegmentBaseline` are target concepts, not current mapped entities. |
| PROPOSED_DELTA | `docs/diagram/V2/05_Technical/openapi.yaml` / `getDatasetCoverage`, `confirmBaseline`, `CoverageResultPage`, `BaselineConfirm` | Coverage GET is proposed; baseline POST is conditional on Q11. |
| CURRENT_VERIFIED | `RoadGuardSystem.Repositories/RoadGuardDbContext.cs`, `Migrations/RoadGuardDbContextModelSnapshot.cs`, `Implementations/Surveys/SurveyV2PersistenceService.Dataset.cs`; ANH-02 SQL checkpoint at base `2efc8a5` | Current persistence stores verified source files and dataset scope, but lacks per-band coverage/baseline rows and method evidence. |

## Decisions required

1. Q11: specify versioned input evidence and assessment method for aircraft position, image/data quality and observed surface coverage independently, including thresholds, missing-data/UNKNOWN rules, time alignment and segment/band boundaries. Confirm who may override or review an assessment and what evidence is retained.
2. Define the authoritative per-band requirement/result/baseline records, keys, foreign keys, uniqueness, append-only/versioning, supersession, actor/time and concurrency semantics. Decide how legacy `Survey.is_baseline_confirmed` is read or migrated without copying it to every band.
3. Approve an explicit implementation scope for entity/configuration/migration/snapshot, SQL fixture and any API contract change. Person 2 coordinates migration order; applying a migration to a live database requires separate approval.
4. Huy must align `getDatasetCoverage` and `confirmBaseline` with the approved facts and stable error/status mapping. Upload `VERIFIED` or dataset submission alone must never imply coverage `SUFFICIENT` or a baseline.

## Acceptance gate

- Owner records the concrete Q11 values/method version and approves the schema/compatibility contract; current versus proposed behavior is explicit.
- ANH-02 persistence and HUY-02 API scopes can be reopened with exact files, migration effects and SQL/HTTP verification requirements.
- Until then, ANH-02 and HUY-02 remain `PARTIAL`; no threshold, baseline row or migration is inferred from this task's creation.

## Completion history

- 2026-09-30 13:28 +07:00 - TODO: Owner chose a separate Q11/schema task. This checkpoint records the decision questions only; no contract value, migration, database or API change is approved or verified.
