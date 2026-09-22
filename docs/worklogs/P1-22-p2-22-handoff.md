# P1-22 / P2-22 Survey Planning Handoff

Date: 2026-09-22
Branch: `anh`

## Handoff note for Person 2

P1-22 required an authoritative road-section version for survey plan/request creation. The P2-22 persistence seam now carries `RoadSectionVersionId` for both create commands, validates that the version exists under the supplied road section/project scope, and persists nullable FK columns for compatibility with legacy rows.

Changed persistence-owned files:

- `RoadGuardSystem.Repositories/Implementations/Surveys/SurveyPlanCreationPersistenceRequest.cs`
- `RoadGuardSystem.Repositories/Implementations/Surveys/SurveyRequestCreationPersistenceRequest.cs`
- `RoadGuardSystem.Repositories/Implementations/Surveys/SurveyPlanningPersistenceService.cs`
- `RoadGuardSystem.Repositories/Configurations/SurveyPlanConfiguration.cs`
- `RoadGuardSystem.Repositories/Configurations/SurveyRequestConfiguration.cs`
- `RoadGuardSystem.Repositories/Migrations/20260922110000_P122SurveyPlanningRoadSectionVersionAnchor.cs`

The migration adds nullable `RoadSectionVersionId` columns and restrictive foreign keys. New P1-22 API commands require a non-empty version id; legacy rows remain readable until an owner-approved backfill policy is selected.

Verification evidence:

- P2-22 SQL integration test rejects an unrelated version id with `ScopeConflict`.
- P1-22 API test rejects an unrelated version id with `403 access_forbidden`.
- Migration is applied by fresh SQL Server/Testcontainers fixtures.

Follow-up decision for Huy: choose whether legacy nullable rows should be backfilled to each road section's current version and then made non-nullable in a later migration. No automatic backfill was added because historical plan/request intent is not recoverable from existing rows.
