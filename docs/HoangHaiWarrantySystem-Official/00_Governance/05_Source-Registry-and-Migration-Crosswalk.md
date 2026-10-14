# Source registry and migration crosswalk

**Reference-first Phase1 canonical registry; existing files preserved.** [Canonical entrance](../README.md), [full Phase0 file-level crosswalk](../../audit/HHWS-DOCS-REBUILD-01/04_DOCS_MIGRATION_MATRIX.md).

## Import and deprecation notices

Canonical governance is active as the new entrance for this rebuild. Detailed legacy chapters remain source references while later phases are unapproved. Only exact stale claims are superseded: Reporter primary Web, Kotlin-only Mobile stack and old product naming in product prose. Technical RoadGuard identifiers and `/auth/android/*` wire stay intact.

Backend Official is not blanket deprecated:70source-reference files remain available. Phase0 hash revalidation/source discovery supports reading its implementation locators; its embedded runtime claims retain their date/SHA and are not Phase1 test results. Historical V2/RF/FE draft assertions remain comparison material; local contract adoption must be explicit.

No source file moved/deleted, no contract lock changed and no old README content overwritten. Deprecation notices live here with exact scope rather than replacing old historical content. The proposed per-file destination list is a review candidate, not a script to mass-migrate all3498documents or vendored/cache files.

## Current source entrances

| Existing source | Why retained | Canonical use / status |
|---|---|---|
| [Confirmed decisions](../../product/confirmed-decisions.md) | Owner32–44 with original qualifiers | Decision source; apply latest H1/client scope supersession only where conflicting |
| [HUY final assignment](../../../planning/development/HUY-FINAL-INTEGRATION.md) | Latest R/ND01/activation and evidence; shared ownership | Business source with newest bounded checkpoints; old embedded evidence historical |
| [Product overview](../../product/README.md), [requirements](../../product/requirements.md), [workflows](../../product/workflows.md) | Derived current guides | Exact ReporterWeb primary-client line superseded by canonical matrix; don't infer source bug from stale wording |
| [Backend Official README](../../RoadGuard-Backend-Official/README.md) | Current architecture/data/workflow/API source locators | SOURCE reference; runtime/deployment acceptance separate |
| [Backend index](../../backend/README.md) | Module/operations summary | Source locator; duplicate policy should link to eventual canonical workflows |
| [Contract transition index](../../../contracts/README.md), [Huy local contract](../../../contracts/http/huy-final.local-contract.md), [Anh local contract](../../../contracts/http/anh01.local-contract.md) | Wire/adoption boundaries | Local assigned scopes vs inactive drafts; no automatic canonical adoption |
| [V2 project material](../../diagram/V2/README.md), [FE proposed guide](../../diagram/V2/09_Frontend/01_FE_Scope_Implementation_Guide.md), [AI comparison material](../../diagram/V2/AI_Integration/README.md) | Historical/proposed docs retain trace and reconciliation banners | Kotlin/ReporterWeb/candidate physical-dimension wording must be compared to new authority; no draft endpoint copied as runtime |
| [Phase0 executive pack](../../audit/HHWS-DOCS-REBUILD-01/00_EXECUTIVE_SUMMARY.md), [full source inventory](../../audit/HHWS-DOCS-REBUILD-01/01_SOURCE_AND_DOCS_INVENTORY.md), [full API inventory](../../audit/HHWS-DOCS-REBUILD-01/02_FULL_API_INVENTORY.md) |241operations+health, full DTO/EF/service/repo/DI evidence and scoped verification | Detailed source registry; counts aren't an empirical model benchmark or deployed acceptance |
| [Phase0 gaps](../../audit/HHWS-DOCS-REBUILD-01/03_BUSINESS_AND_PLATFORM_CONFLICTS.md), [future decisions](../../audit/HHWS-DOCS-REBUILD-01/05_OPEN_DECISIONS_AND_GAPS.md) | JEV/authority/protocol/data choices | Open gates preserved; previous Phase0 “Phase1 not authorized” status is historical after latest owner instruction |

`docs/frontend/REPORTER-EXPO-HANDOFF.md` and `docs/audit/RG-REPORTER-ANDROID-AUDIT-01.md` exist locally as pre-existing untracked inputs. They are preserved, excluded from this commit and not linked as published GitHub artifacts. Reporter scope/evidence needed for review is in the committed Phase0 pack. A literal external Project Docs package and phiếu FA26SE219 were not supplied; V2/FE_R3/BA_R3 are comparison candidates, not proof those inputs were read.

## Phase1 actual destinations vs future candidates

| Source topic | Actual canonical page now | Future content gate |
|---|---|---|
| Authority/status/scope glossary | [Authority](01_Authority-and-Source-of-Truth.md), [decisions](02_Product-Decisions-and-Scope.md), [status](03_Implementation-and-Verification-Status.md) | Governance review; no business source adoption by title alone |
| Actor/client/permission | [Actor matrix](../01_Product-and-Roles/02_Actor-Client-Permission-Matrix.md) | Exact role/resource operation details retained in Phase0 source; detailed UX later |
| Document precedence/change/mapping | [Control](04_Document-Control-and-Change-History.md), this registry | Owner review before Phase2 imports or archive changes |
| Backend/workflow/data/API | Current Backend Official and Phase0 locators | Phase2 source-backed writing NOT_STARTED |
| Web and Expo consumer handoff | Matrix/status and existing sources | Phase3 screens/API/native feasibility NOT_STARTED |
| FastAPI/provider/model handoff | Existing AI boundary and Phase0 gaps | Phase4 cookbook/protocol proposal NOT_STARTED |
| Testing/ops/migration QA/archive | Current evidence and old-source pointers | Phase5 migration validation NOT_STARTED; keep original paths |

## Backend Official source-to-canonical crosswalk

The file-level registry below gives a current source link and future topic destination for each70existing Backend Official files. Future paths are code notation, not links to nonexistent pages. Generated OpenAPI/JSON/CSV remain references, not manually copied schema. Phase0's full3498file crosswalk covers other available docs/history/planning/artifacts with candidate/disposition labels.

| Existing source file | Canonical topic candidate | Current import status |
|---|---|---|
| [00_Overview/01_Backend_Project_Overview.md](../../RoadGuard-Backend-Official/00_Overview/01_Backend_Project_Overview.md) | `01_Product-and-Roles/; current actor matrix already exists` | Reference only; detailed rewrite not started |
| [00_Overview/02_Backend_Scope.md](../../RoadGuard-Backend-Official/00_Overview/02_Backend_Scope.md) | `01_Product-and-Roles/; current actor matrix already exists` | Reference only; detailed rewrite not started |
| [00_Overview/03_System_Actors_and_Responsibilities.md](../../RoadGuard-Backend-Official/00_Overview/03_System_Actors_and_Responsibilities.md) | `01_Product-and-Roles/; current actor matrix already exists` | Reference only; detailed rewrite not started |
| [00_Overview/04_Implementation_Status.md](../../RoadGuard-Backend-Official/00_Overview/04_Implementation_Status.md) | `01_Product-and-Roles/; current actor matrix already exists` | Reference only; detailed rewrite not started |
| [01_Architecture/01_Backend_Architecture.md](../../RoadGuard-Backend-Official/01_Architecture/01_Backend_Architecture.md) | `03_Backend-and-Data/01_ASPNET-Architecture.md` | Reference only; detailed rewrite not started |
| [01_Architecture/02_Module_Boundaries.md](../../RoadGuard-Backend-Official/01_Architecture/02_Module_Boundaries.md) | `03_Backend-and-Data/01_ASPNET-Architecture.md` | Reference only; detailed rewrite not started |
| [01_Architecture/03_Controller_Service_Repository_Flow.md](../../RoadGuard-Backend-Official/01_Architecture/03_Controller_Service_Repository_Flow.md) | `03_Backend-and-Data/01_ASPNET-Architecture.md` | Reference only; detailed rewrite not started |
| [01_Architecture/04_Background_Workers_and_Outbox.md](../../RoadGuard-Backend-Official/01_Architecture/04_Background_Workers_and_Outbox.md) | `03_Backend-and-Data/01_ASPNET-Architecture.md` | Reference only; detailed rewrite not started |
| [01_Architecture/05_Configuration_and_Dependency_Injection.md](../../RoadGuard-Backend-Official/01_Architecture/05_Configuration_and_Dependency_Injection.md) | `03_Backend-and-Data/01_ASPNET-Architecture.md` | Reference only; detailed rewrite not started |
| [02_Business_Workflows/01_Authentication_and_Accounts.md](../../RoadGuard-Backend-Official/02_Business_Workflows/01_Authentication_and_Accounts.md) | `02_Business-Workflows/ (one canonical workflow definition)` | Reference only; detailed rewrite not started |
| [02_Business_Workflows/02_Staff_Membership_and_Project.md](../../RoadGuard-Backend-Official/02_Business_Workflows/02_Staff_Membership_and_Project.md) | `02_Business-Workflows/ (one canonical workflow definition)` | Reference only; detailed rewrite not started |
| [02_Business_Workflows/03_Roads_Geometry_and_Pavement.md](../../RoadGuard-Backend-Official/02_Business_Workflows/03_Roads_Geometry_and_Pavement.md) | `02_Business-Workflows/ (one canonical workflow definition)` | Reference only; detailed rewrite not started |
| [02_Business_Workflows/04_Reporter_and_IncidentCase.md](../../RoadGuard-Backend-Official/02_Business_Workflows/04_Reporter_and_IncidentCase.md) | `02_Business-Workflows/ (one canonical workflow definition)` | Reference only; detailed rewrite not started |
| [02_Business_Workflows/05_Drone_Survey_and_AI_Processing.md](../../RoadGuard-Backend-Official/02_Business_Workflows/05_Drone_Survey_and_AI_Processing.md) | `02_Business-Workflows/ (one canonical workflow definition)` | Reference only; detailed rewrite not started |
| [02_Business_Workflows/06_Field_Inspection_and_Measurements.md](../../RoadGuard-Backend-Official/02_Business_Workflows/06_Field_Inspection_and_Measurements.md) | `02_Business-Workflows/ (one canonical workflow definition)` | Reference only; detailed rewrite not started |
| [02_Business_Workflows/07_Repair_Policy_and_Fast_Track.md](../../RoadGuard-Backend-Official/02_Business_Workflows/07_Repair_Policy_and_Fast_Track.md) | `02_Business-Workflows/ (one canonical workflow definition)` | Reference only; detailed rewrite not started |
| [02_Business_Workflows/08_Repair_Execution_and_Acceptance.md](../../RoadGuard-Backend-Official/02_Business_Workflows/08_Repair_Execution_and_Acceptance.md) | `02_Business-Workflows/ (one canonical workflow definition)` | Reference only; detailed rewrite not started |
| [02_Business_Workflows/09_Notifications_and_Reporting.md](../../RoadGuard-Backend-Official/02_Business_Workflows/09_Notifications_and_Reporting.md) | `02_Business-Workflows/ (one canonical workflow definition)` | Reference only; detailed rewrite not started |
| [02_Business_Workflows/10_Retention_and_Export.md](../../RoadGuard-Backend-Official/02_Business_Workflows/10_Retention_and_Export.md) | `02_Business-Workflows/ (one canonical workflow definition)` | Reference only; detailed rewrite not started |
| [03_API_Contracts/01_API_Conventions.md](../../RoadGuard-Backend-Official/03_API_Contracts/01_API_Conventions.md) | `04_API-Contracts/ (source-backed wire inventory/schema references)` | Reference only; detailed rewrite not started |
| [03_API_Contracts/02_Endpoint_Catalog.md](../../RoadGuard-Backend-Official/03_API_Contracts/02_Endpoint_Catalog.md) | `04_API-Contracts/ (source-backed wire inventory/schema references)` | Reference only; detailed rewrite not started |
| [03_API_Contracts/03_Request_Response_DTOs.md](../../RoadGuard-Backend-Official/03_API_Contracts/03_Request_Response_DTOs.md) | `04_API-Contracts/ (source-backed wire inventory/schema references)` | Reference only; detailed rewrite not started |
| [03_API_Contracts/04_Error_Catalog.md](../../RoadGuard-Backend-Official/03_API_Contracts/04_Error_Catalog.md) | `04_API-Contracts/ (source-backed wire inventory/schema references)` | Reference only; detailed rewrite not started |
| [03_API_Contracts/05_Concurrency_and_Idempotency.md](../../RoadGuard-Backend-Official/03_API_Contracts/05_Concurrency_and_Idempotency.md) | `04_API-Contracts/ (source-backed wire inventory/schema references)` | Reference only; detailed rewrite not started |
| [03_API_Contracts/06_OpenAPI_Runtime_Alignment.md](../../RoadGuard-Backend-Official/03_API_Contracts/06_OpenAPI_Runtime_Alignment.md) | `04_API-Contracts/ (source-backed wire inventory/schema references)` | Reference only; detailed rewrite not started |
| [03_API_Contracts/openapi.backend.yaml](../../RoadGuard-Backend-Official/03_API_Contracts/openapi.backend.yaml) | `04_API-Contracts/ (source-backed wire inventory/schema references)` | Generated/source evidence reference; no manual schema import/adoption |
| [04_Database/01_Database_Overview.md](../../RoadGuard-Backend-Official/04_Database/01_Database_Overview.md) | `03_Backend-and-Data/02_Domain-and-Data-Model.md` | Reference only; detailed rewrite not started |
| [04_Database/02_Entity_Data_Dictionary.md](../../RoadGuard-Backend-Official/04_Database/02_Entity_Data_Dictionary.md) | `03_Backend-and-Data/02_Domain-and-Data-Model.md` | Reference only; detailed rewrite not started |
| [04_Database/03_Entity_Relationships.md](../../RoadGuard-Backend-Official/04_Database/03_Entity_Relationships.md) | `03_Backend-and-Data/02_Domain-and-Data-Model.md` | Reference only; detailed rewrite not started |
| [04_Database/04_Persistence_Constraints.md](../../RoadGuard-Backend-Official/04_Database/04_Persistence_Constraints.md) | `03_Backend-and-Data/02_Domain-and-Data-Model.md` | Reference only; detailed rewrite not started |
| [04_Database/05_Migrations_and_Schema_Baseline.md](../../RoadGuard-Backend-Official/04_Database/05_Migrations_and_Schema_Baseline.md) | `03_Backend-and-Data/02_Domain-and-Data-Model.md` | Reference only; detailed rewrite not started |
| [04_Database/06_Database_ERD.md](../../RoadGuard-Backend-Official/04_Database/06_Database_ERD.md) | `03_Backend-and-Data/02_Domain-and-Data-Model.md` | Reference only; detailed rewrite not started |
| [04_Database/ef-model.json](../../RoadGuard-Backend-Official/04_Database/ef-model.json) | `03_Backend-and-Data/02_Domain-and-Data-Model.md` | Generated/source evidence reference; no manual schema import/adoption |
| [05_Security/01_Authentication.md](../../RoadGuard-Backend-Official/05_Security/01_Authentication.md) | `03_Backend-and-Data/03_Auth-RBAC-and-Security.md` | Reference only; detailed rewrite not started |
| [05_Security/02_Authorization_Matrix.md](../../RoadGuard-Backend-Official/05_Security/02_Authorization_Matrix.md) | `03_Backend-and-Data/03_Auth-RBAC-and-Security.md` | Reference only; detailed rewrite not started |
| [05_Security/03_Project_Scope_and_Ownership.md](../../RoadGuard-Backend-Official/05_Security/03_Project_Scope_and_Ownership.md) | `03_Backend-and-Data/03_Auth-RBAC-and-Security.md` | Reference only; detailed rewrite not started |
| [05_Security/04_Cookie_CSRF_and_Token_Security.md](../../RoadGuard-Backend-Official/05_Security/04_Cookie_CSRF_and_Token_Security.md) | `03_Backend-and-Data/03_Auth-RBAC-and-Security.md` | Reference only; detailed rewrite not started |
| [06_Integrations/01_BE_FE_Integration_Boundary.md](../../RoadGuard-Backend-Official/06_Integrations/01_BE_FE_Integration_Boundary.md) | `05_Web/ + 06_Mobile-Expo/ + 07_AI-FastAPI/` | Reference only; detailed rewrite not started |
| [06_Integrations/02_BE_FastAPI_Integration_Boundary.md](../../RoadGuard-Backend-Official/06_Integrations/02_BE_FastAPI_Integration_Boundary.md) | `05_Web/ + 06_Mobile-Expo/ + 07_AI-FastAPI/` | Reference only; detailed rewrite not started |
| [06_Integrations/03_File_and_Artifact_Exchange.md](../../RoadGuard-Backend-Official/06_Integrations/03_File_and_Artifact_Exchange.md) | `05_Web/ + 06_Mobile-Expo/ + 07_AI-FastAPI/` | Reference only; detailed rewrite not started |
| [07_Offline_Sync/01_Offline_Backend_Architecture.md](../../RoadGuard-Backend-Official/07_Offline_Sync/01_Offline_Backend_Architecture.md) | `02_Business-Workflows/07_Offline-Handover-and-Recovery.md + 06_Mobile-Expo/` | Reference only; detailed rewrite not started |
| [07_Offline_Sync/02_Snapshot_and_Device_Management.md](../../RoadGuard-Backend-Official/07_Offline_Sync/02_Snapshot_and_Device_Management.md) | `02_Business-Workflows/07_Offline-Handover-and-Recovery.md + 06_Mobile-Expo/` | Reference only; detailed rewrite not started |
| [07_Offline_Sync/03_Signed_Sync_and_Idempotency.md](../../RoadGuard-Backend-Official/07_Offline_Sync/03_Signed_Sync_and_Idempotency.md) | `02_Business-Workflows/07_Offline-Handover-and-Recovery.md + 06_Mobile-Expo/` | Reference only; detailed rewrite not started |
| [07_Offline_Sync/04_Conflict_Handover_and_Recovery.md](../../RoadGuard-Backend-Official/07_Offline_Sync/04_Conflict_Handover_and_Recovery.md) | `02_Business-Workflows/07_Offline-Handover-and-Recovery.md + 06_Mobile-Expo/` | Reference only; detailed rewrite not started |
| [08_Storage_Operations/01_File_and_Object_Storage.md](../../RoadGuard-Backend-Official/08_Storage_Operations/01_File_and_Object_Storage.md) | `08_Testing-and-Operations/03_Deployment-SQL-MinIO-and-AI.md` | Reference only; detailed rewrite not started |
| [08_Storage_Operations/02_Retention_and_Legal_Hold.md](../../RoadGuard-Backend-Official/08_Storage_Operations/02_Retention_and_Legal_Hold.md) | `08_Testing-and-Operations/03_Deployment-SQL-MinIO-and-AI.md` | Reference only; detailed rewrite not started |
| [08_Storage_Operations/03_Deployment_Configuration.md](../../RoadGuard-Backend-Official/08_Storage_Operations/03_Deployment_Configuration.md) | `08_Testing-and-Operations/03_Deployment-SQL-MinIO-and-AI.md` | Reference only; detailed rewrite not started |
| [08_Storage_Operations/04_Operational_Recovery.md](../../RoadGuard-Backend-Official/08_Storage_Operations/04_Operational_Recovery.md) | `08_Testing-and-Operations/03_Deployment-SQL-MinIO-and-AI.md` | Reference only; detailed rewrite not started |
| [09_Testing/01_Backend_Test_Strategy.md](../../RoadGuard-Backend-Official/09_Testing/01_Backend_Test_Strategy.md) | `08_Testing-and-Operations/01_Test-Matrix-and-Evidence.md` | Reference only; detailed rewrite not started |
| [09_Testing/02_API_Test_Matrix.md](../../RoadGuard-Backend-Official/09_Testing/02_API_Test_Matrix.md) | `08_Testing-and-Operations/01_Test-Matrix-and-Evidence.md` | Reference only; detailed rewrite not started |
| [09_Testing/03_Database_Integration_Tests.md](../../RoadGuard-Backend-Official/09_Testing/03_Database_Integration_Tests.md) | `08_Testing-and-Operations/01_Test-Matrix-and-Evidence.md` | Reference only; detailed rewrite not started |
| [09_Testing/04_Known_Test_Limitations.md](../../RoadGuard-Backend-Official/09_Testing/04_Known_Test_Limitations.md) | `08_Testing-and-Operations/01_Test-Matrix-and-Evidence.md` | Reference only; detailed rewrite not started |
| [09_Testing/api-test-references.json](../../RoadGuard-Backend-Official/09_Testing/api-test-references.json) | `08_Testing-and-Operations/01_Test-Matrix-and-Evidence.md` | Generated/source evidence reference; no manual schema import/adoption |
| [09_Testing/executed-tests.json](../../RoadGuard-Backend-Official/09_Testing/executed-tests.json) | `08_Testing-and-Operations/01_Test-Matrix-and-Evidence.md` | Generated/source evidence reference; no manual schema import/adoption |
| [10_Traceability/01_Requirement_to_Implementation_Matrix.md](../../RoadGuard-Backend-Official/10_Traceability/01_Requirement_to_Implementation_Matrix.md) | `04_API-Contracts/ source-index references; preserve machine evidence` | Reference only; detailed rewrite not started |
| [10_Traceability/02_API_to_Source_Matrix.md](../../RoadGuard-Backend-Official/10_Traceability/02_API_to_Source_Matrix.md) | `04_API-Contracts/ source-index references; preserve machine evidence` | Reference only; detailed rewrite not started |
| [10_Traceability/03_Database_to_Source_Matrix.md](../../RoadGuard-Backend-Official/10_Traceability/03_Database_to_Source_Matrix.md) | `04_API-Contracts/ source-index references; preserve machine evidence` | Reference only; detailed rewrite not started |
| [10_Traceability/api-method-evidence.json](../../RoadGuard-Backend-Official/10_Traceability/api-method-evidence.json) | `04_API-Contracts/ source-index references; preserve machine evidence` | Generated/source evidence reference; no manual schema import/adoption |
| [10_Traceability/api-source-map.json](../../RoadGuard-Backend-Official/10_Traceability/api-source-map.json) | `04_API-Contracts/ source-index references; preserve machine evidence` | Generated/source evidence reference; no manual schema import/adoption |
| [10_Traceability/backend-assets.csv](../../RoadGuard-Backend-Official/10_Traceability/backend-assets.csv) | `04_API-Contracts/ source-index references; preserve machine evidence` | Generated/source evidence reference; no manual schema import/adoption |
| [10_Traceability/source-files.csv](../../RoadGuard-Backend-Official/10_Traceability/source-files.csv) | `04_API-Contracts/ source-index references; preserve machine evidence` | Generated/source evidence reference; no manual schema import/adoption |
| [10_Traceability/source-inventory.json](../../RoadGuard-Backend-Official/10_Traceability/source-inventory.json) | `04_API-Contracts/ source-index references; preserve machine evidence` | Generated/source evidence reference; no manual schema import/adoption |
| [11_Audit_and_Gaps/01_Documentation_Alignment_Report.md](../../RoadGuard-Backend-Official/11_Audit_and_Gaps/01_Documentation_Alignment_Report.md) | `08_Testing-and-Operations/04_Open-Gaps-and-Risks.md` | Reference only; detailed rewrite not started |
| [11_Audit_and_Gaps/02_Known_Implementation_Gaps.md](../../RoadGuard-Backend-Official/11_Audit_and_Gaps/02_Known_Implementation_Gaps.md) | `08_Testing-and-Operations/04_Open-Gaps-and-Risks.md` | Reference only; detailed rewrite not started |
| [11_Audit_and_Gaps/03_Pending_Environment_Verification.md](../../RoadGuard-Backend-Official/11_Audit_and_Gaps/03_Pending_Environment_Verification.md) | `08_Testing-and-Operations/04_Open-Gaps-and-Risks.md` | Reference only; detailed rewrite not started |
| [11_Audit_and_Gaps/BE-DOC-02-SPEC.md](../../RoadGuard-Backend-Official/11_Audit_and_Gaps/BE-DOC-02-SPEC.md) | `08_Testing-and-Operations/04_Open-Gaps-and-Risks.md` | Reference only; detailed rewrite not started |
| [11_Audit_and_Gaps/gaps.json](../../RoadGuard-Backend-Official/11_Audit_and_Gaps/gaps.json) | `08_Testing-and-Operations/04_Open-Gaps-and-Risks.md` | Generated/source evidence reference; no manual schema import/adoption |
| [11_Audit_and_Gaps/validation-evidence.json](../../RoadGuard-Backend-Official/11_Audit_and_Gaps/validation-evidence.json) | `08_Testing-and-Operations/04_Open-Gaps-and-Risks.md` | Generated/source evidence reference; no manual schema import/adoption |
| [DOCUMENTATION_MANIFEST.md](../../RoadGuard-Backend-Official/DOCUMENTATION_MANIFEST.md) | `00_Governance/source registry + canonical entrance; retain historical manifest` | Reference only; detailed rewrite not started |
| [README.md](../../RoadGuard-Backend-Official/README.md) | `00_Governance/source registry + canonical entrance; retain historical manifest` | Reference only; detailed rewrite not started |

All70source files retain original paths/content. The full Phase0 crosswalk and current registry are the import locators; no archival or deprecation of all backend source guides is implied.
