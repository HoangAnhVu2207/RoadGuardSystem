# RoadGuard Backend Official — source-verified documentation baseline

**CURRENT_VERIFIED — source baseline `cdeb1ca1037744d06995058af13d0582f6273c0a`, `huy-review`, 2026-10-13.** Source observation is not all-path runtime execution. Expected business behavior uses owner-confirmed decisions and assigned specs; implementation/test evidence has its own limits.

**Delivery:70files across12 numbered sections plus README/manifest;59requiredfiles and11provenance supplements.**

This set is independently readable current Backend documentation, not canonical business adoption of V2 or runtime E2E certification. D1–D4 keep expected decisions/current source separate,100% verified inventory scope, docs-only edits and local huy-review commit.

**Baseline:** cdeb1ca1037744d06995058af13d0582f6273c0a. **Runtime discovery:**47controllers/241operations/223paths/357production schemas. **EF:**75DbSets/196entitytypes/191tables/1940mapped columns. Source and historical SQL proof are distinguished from LIVE_SQL_VERIFIED NOT_RUN.

[Manifest and execution/source inventories](DOCUMENTATION_MANIFEST.md) · [Alignment report](11_Audit_and_Gaps/01_Documentation_Alignment_Report.md) · [Known gaps](11_Audit_and_Gaps/02_Known_Implementation_Gaps.md) · [Pending environments](11_Audit_and_Gaps/03_Pending_Environment_Verification.md).

## Reading routes

### 00_Overview — Purpose, scope, actors, implementation status

- [01_Backend_Project_Overview.md](00_Overview/01_Backend_Project_Overview.md)
- [02_Backend_Scope.md](00_Overview/02_Backend_Scope.md)
- [03_System_Actors_and_Responsibilities.md](00_Overview/03_System_Actors_and_Responsibilities.md)
- [04_Implementation_Status.md](00_Overview/04_Implementation_Status.md)

### 01_Architecture — Boundaries, full service/repository seams, workers/DI/config

- [01_Backend_Architecture.md](01_Architecture/01_Backend_Architecture.md)
- [02_Module_Boundaries.md](01_Architecture/02_Module_Boundaries.md)
- [03_Controller_Service_Repository_Flow.md](01_Architecture/03_Controller_Service_Repository_Flow.md)
- [04_Background_Workers_and_Outbox.md](01_Architecture/04_Background_Workers_and_Outbox.md)
- [05_Configuration_and_Dependency_Injection.md](01_Architecture/05_Configuration_and_Dependency_Injection.md)

### 02_Business_Workflows — End-to-end actors, state, read IDs/version, commands/evidence/recovery

- [01_Authentication_and_Accounts.md](02_Business_Workflows/01_Authentication_and_Accounts.md)
- [02_Staff_Membership_and_Project.md](02_Business_Workflows/02_Staff_Membership_and_Project.md)
- [03_Roads_Geometry_and_Pavement.md](02_Business_Workflows/03_Roads_Geometry_and_Pavement.md)
- [04_Reporter_and_IncidentCase.md](02_Business_Workflows/04_Reporter_and_IncidentCase.md)
- [05_Drone_Survey_and_AI_Processing.md](02_Business_Workflows/05_Drone_Survey_and_AI_Processing.md)
- [06_Field_Inspection_and_Measurements.md](02_Business_Workflows/06_Field_Inspection_and_Measurements.md)
- [07_Repair_Policy_and_Fast_Track.md](02_Business_Workflows/07_Repair_Policy_and_Fast_Track.md)
- [08_Repair_Execution_and_Acceptance.md](02_Business_Workflows/08_Repair_Execution_and_Acceptance.md)
- [09_Notifications_and_Reporting.md](02_Business_Workflows/09_Notifications_and_Reporting.md)
- [10_Retention_and_Export.md](02_Business_Workflows/10_Retention_and_Export.md)

### 03_API_Contracts — All operations/DTOs/errors/headers/version/idempotency and independent runtime OpenAPI

- [01_API_Conventions.md](03_API_Contracts/01_API_Conventions.md)
- [02_Endpoint_Catalog.md](03_API_Contracts/02_Endpoint_Catalog.md)
- [03_Request_Response_DTOs.md](03_API_Contracts/03_Request_Response_DTOs.md)
- [04_Error_Catalog.md](03_API_Contracts/04_Error_Catalog.md)
- [05_Concurrency_and_Idempotency.md](03_API_Contracts/05_Concurrency_and_Idempotency.md)
- [06_OpenAPI_Runtime_Alignment.md](03_API_Contracts/06_OpenAPI_Runtime_Alignment.md)
- [openapi.backend.yaml](03_API_Contracts/openapi.backend.yaml)

### 04_Database — All entities/properties/relationships/constraints/migrations and per-context ERD

- [01_Database_Overview.md](04_Database/01_Database_Overview.md)
- [02_Entity_Data_Dictionary.md](04_Database/02_Entity_Data_Dictionary.md)
- [03_Entity_Relationships.md](04_Database/03_Entity_Relationships.md)
- [04_Persistence_Constraints.md](04_Database/04_Persistence_Constraints.md)
- [05_Migrations_and_Schema_Baseline.md](04_Database/05_Migrations_and_Schema_Baseline.md)
- [06_Database_ERD.md](04_Database/06_Database_ERD.md)

### 05_Security — Current auth/operation guards/project ownership/cookie/CSRF

- [01_Authentication.md](05_Security/01_Authentication.md)
- [02_Authorization_Matrix.md](05_Security/02_Authorization_Matrix.md)
- [03_Project_Scope_and_Ownership.md](05_Security/03_Project_Scope_and_Ownership.md)
- [04_Cookie_CSRF_and_Token_Security.md](05_Security/04_Cookie_CSRF_and_Token_Security.md)

### 06_Integrations — FE/BE/FastAPI and bytes/artifact boundaries only

- [01_BE_FE_Integration_Boundary.md](06_Integrations/01_BE_FE_Integration_Boundary.md)
- [02_BE_FastAPI_Integration_Boundary.md](06_Integrations/02_BE_FastAPI_Integration_Boundary.md)
- [03_File_and_Artifact_Exchange.md](06_Integrations/03_File_and_Artifact_Exchange.md)

### 07_Offline_Sync — Device/snapshot/signed commands/current authority/handover/recovery

- [01_Offline_Backend_Architecture.md](07_Offline_Sync/01_Offline_Backend_Architecture.md)
- [02_Snapshot_and_Device_Management.md](07_Offline_Sync/02_Snapshot_and_Device_Management.md)
- [03_Signed_Sync_and_Idempotency.md](07_Offline_Sync/03_Signed_Sync_and_Idempotency.md)
- [04_Conflict_Handover_and_Recovery.md](07_Offline_Sync/04_Conflict_Handover_and_Recovery.md)

### 08_Storage_Operations — Storage/retention/hold/config/deployment/recovery responsibilities

- [01_File_and_Object_Storage.md](08_Storage_Operations/01_File_and_Object_Storage.md)
- [02_Retention_and_Legal_Hold.md](08_Storage_Operations/02_Retention_and_Legal_Hold.md)
- [03_Deployment_Configuration.md](08_Storage_Operations/03_Deployment_Configuration.md)
- [04_Operational_Recovery.md](08_Storage_Operations/04_Operational_Recovery.md)

### 09_Testing — All source tests, exact executed outcomes and limitations

- [01_Backend_Test_Strategy.md](09_Testing/01_Backend_Test_Strategy.md)
- [02_API_Test_Matrix.md](09_Testing/02_API_Test_Matrix.md)
- [03_Database_Integration_Tests.md](09_Testing/03_Database_Integration_Tests.md)
- [04_Known_Test_Limitations.md](09_Testing/04_Known_Test_Limitations.md)

### 10_Traceability — Expected decisions↔implementation, all APIs↔source and database↔source

- [01_Requirement_to_Implementation_Matrix.md](10_Traceability/01_Requirement_to_Implementation_Matrix.md)
- [02_API_to_Source_Matrix.md](10_Traceability/02_API_to_Source_Matrix.md)
- [03_Database_to_Source_Matrix.md](10_Traceability/03_Database_to_Source_Matrix.md)

### 11_Audit_and_Gaps — Review report, gaps/confidence/owners and deferred acceptance

- [02_Known_Implementation_Gaps.md](11_Audit_and_Gaps/02_Known_Implementation_Gaps.md)
- [03_Pending_Environment_Verification.md](11_Audit_and_Gaps/03_Pending_Environment_Verification.md)
- [BE-DOC-02-SPEC.md](11_Audit_and_Gaps/BE-DOC-02-SPEC.md)

Evidence vocabulary: TARGET_CONFIRMED=owner decision; CURRENT_VERIFIED=observed source/executed check within stated scope; HISTORICAL=revalidated earlier result; UNKNOWN/NOT_RUN=no execution proof; PROPOSED=optional future action. No method/file/build alone proves full behavior. See the report for gate results and fresh91pass/1fail focused tests.
