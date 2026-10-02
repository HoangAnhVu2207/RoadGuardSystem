# Current relational ERD

CURRENT_VERIFIED from [isolated SQL inventory](current-schema.inventory.json). Each Mermaid entity displays the exact `dbo.Table` name; `dbo_Table` is its internal Mermaid identifier. Table headings in the [dictionary](current-data-dictionary.md) give exact names. Solid relationships below exist in `sys.foreign_keys`. No edge is inferred from a property name.

## Module overview

```mermaid
flowchart LR
  identity["identity (10 tables)"]
  project_road["project-road (8 tables)"]
  survey["survey (13 tables)"]
  files["files (4 tables)"]
  processing["processing (5 tables)"]
  defect_inspection["defect-inspection (12 tables)"]
  messaging["messaging (5 tables)"]
  defect_inspection --> files
  defect_inspection --> identity
  defect_inspection --> processing
  defect_inspection --> project_road
  defect_inspection --> survey
  files --> identity
  files --> project_road
  identity --> project_road
  messaging --> identity
  processing --> identity
  processing --> survey
  project_road --> files
  project_road --> identity
  survey --> files
  survey --> identity
  survey --> project_road
```

Arrows indicate child-module FK dependency on principal module, not transaction or service ownership. Cross-module FK details are in the per-table dictionary.

## identity

```mermaid
erDiagram
  dbo_Users["dbo.Users"] {
    uniqueidentifier Id PK
    varchar RoleCode FK
  }
  dbo_Roles["dbo.Roles"] {
    varchar Code PK
  }
  dbo_Sessions["dbo.Sessions"] {
    uniqueidentifier Id PK
    uniqueidentifier UserId FK
  }
  dbo_RefreshTokens["dbo.RefreshTokens"] {
    uniqueidentifier Id PK
    uniqueidentifier SessionId FK
  }
  dbo_PasswordResetLogs["dbo.PasswordResetLogs"] {
    uniqueidentifier Id PK
    uniqueidentifier TargetUserId FK
    uniqueidentifier PerformedByUserId FK
  }
  dbo_AccountStatusChangeLogs["dbo.AccountStatusChangeLogs"] {
    uniqueidentifier Id PK
    uniqueidentifier TargetUserId FK
    uniqueidentifier ChangedByUserId FK
  }
  dbo_PasswordRecoveryRequests["dbo.PasswordRecoveryRequests"] {
    uniqueidentifier Id PK
    uniqueidentifier TargetUserId FK
  }
  dbo_ReporterRegistrationIntents["dbo.ReporterRegistrationIntents"] {
    uniqueidentifier Id PK
    uniqueidentifier UserId FK
  }
  dbo_StaffInvitations["dbo.StaffInvitations"] {
    uniqueidentifier Id PK
    uniqueidentifier CreatedByUserId FK
  }
  dbo_StaffInvitationProjects["dbo.StaffInvitationProjects"] {
    uniqueidentifier InvitationId PK
    uniqueidentifier ProjectId PK
  }
  dbo_Users o|--o{ dbo_AccountStatusChangeLogs : FK_AccountStatusChangeLogs_Users_ChangedByUserId
  dbo_Users ||--o{ dbo_AccountStatusChangeLogs : FK_AccountStatusChangeLogs_Users_TargetUserId
  dbo_Users o|--o{ dbo_PasswordRecoveryRequests : FK_PasswordRecoveryRequests_Users_TargetUserId
  dbo_Users o|--o{ dbo_PasswordResetLogs : FK_PasswordResetLogs_Users_PerformedByUserId
  dbo_Users ||--o{ dbo_PasswordResetLogs : FK_PasswordResetLogs_Users_TargetUserId
  dbo_Sessions ||--o{ dbo_RefreshTokens : FK_RefreshTokens_Sessions_SessionId
  dbo_Users o|--o{ dbo_ReporterRegistrationIntents : FK_ReporterRegistrationIntents_Users_UserId
  dbo_Users ||--o{ dbo_Sessions : FK_Sessions_Users_UserId
  dbo_StaffInvitations ||--o{ dbo_StaffInvitationProjects : FK_StaffInvitationProjects_StaffInvitations_InvitationId
  dbo_Users ||--o{ dbo_StaffInvitations : FK_StaffInvitations_Users_CreatedByUserId
  dbo_Roles ||--o{ dbo_Users : FK_Users_Roles_RoleCode
```

Cross-module enforced FK (full composite columns and delete behavior: [dictionary](current-data-dictionary.md)):

- dbo.StaffInvitationProjects -> dbo.Projects via FK_StaffInvitationProjects_Projects_ProjectId.

## project-road

```mermaid
erDiagram
  dbo_Projects["dbo.Projects"] {
    uniqueidentifier Id PK
  }
  dbo_ProjectMembers["dbo.ProjectMembers"] {
    uniqueidentifier Id PK
    uniqueidentifier ProjectId FK
    uniqueidentifier UserId FK
    varchar RoleCode FK
  }
  dbo_RoadSections["dbo.RoadSections"] {
    uniqueidentifier Id PK
    uniqueidentifier ProjectId FK
  }
  dbo_RoadSectionVersions["dbo.RoadSectionVersions"] {
    uniqueidentifier Id PK
    uniqueidentifier RoadSectionId FK
  }
  dbo_RoadSegmentSets["dbo.RoadSegmentSets"] {
    uniqueidentifier Id PK
    uniqueidentifier RoadSectionVersionId FK
  }
  dbo_RoadSegments["dbo.RoadSegments"] {
    uniqueidentifier Id PK
    uniqueidentifier SegmentSetId FK
    uniqueidentifier RoadSectionVersionId FK
  }
  dbo_Warranties["dbo.Warranties"] {
    uniqueidentifier Id PK
    uniqueidentifier ProjectId FK
    uniqueidentifier RoadSectionId FK
    uniqueidentifier HandoverDocumentId FK
    uniqueidentifier SourceDocumentId FK
  }
  dbo_HandoverDocuments["dbo.HandoverDocuments"] {
    uniqueidentifier Id PK
    uniqueidentifier ProjectId FK
    uniqueidentifier AcceptedByUserId FK
    uniqueidentifier FileId FK
  }
  dbo_Projects ||--o{ dbo_HandoverDocuments : FK_HandoverDocuments_Projects_ProjectId
  dbo_Projects ||--o{ dbo_ProjectMembers : FK_ProjectMembers_Projects_ProjectId
  dbo_Projects ||--o{ dbo_RoadSections : FK_RoadSections_Projects_ProjectId
  dbo_RoadSections ||--o{ dbo_RoadSectionVersions : FK_RoadSectionVersions_RoadSections_RoadSectionId
  dbo_RoadSectionVersions ||--o{ dbo_RoadSegments : FK_RoadSegments_RoadSectionVersions_RoadSectionVersionId
  dbo_RoadSegmentSets ||--o{ dbo_RoadSegments : FK_RoadSegments_RoadSegmentSets_SegmentSetId
  dbo_RoadSectionVersions ||--o{ dbo_RoadSegmentSets : FK_RoadSegmentSets_RoadSectionVersions_RoadSectionVersionId
  dbo_HandoverDocuments o|--o{ dbo_Warranties : FK_Warranties_HandoverDocuments_HandoverDocumentId
  dbo_Projects ||--o{ dbo_Warranties : FK_Warranties_Projects_ProjectId
  dbo_RoadSections o|--o{ dbo_Warranties : FK_Warranties_RoadSections_RoadSectionId
```

Cross-module enforced FK (full composite columns and delete behavior: [dictionary](current-data-dictionary.md)):

- dbo.HandoverDocuments -> dbo.Files via FK_HandoverDocuments_Files_FileId.
- dbo.HandoverDocuments -> dbo.Users via FK_HandoverDocuments_Users_AcceptedByUserId.
- dbo.ProjectMembers -> dbo.Roles via FK_ProjectMembers_Roles_RoleCode.
- dbo.ProjectMembers -> dbo.Users via FK_ProjectMembers_Users_UserId.
- dbo.Warranties -> dbo.Files via FK_Warranties_Files_SourceDocumentId.

## survey

```mermaid
erDiagram
  dbo_SurveyPlans["dbo.SurveyPlans"] {
    uniqueidentifier Id PK
    uniqueidentifier ProjectId FK
    uniqueidentifier RoadSectionId FK
    uniqueidentifier RoadSectionVersionId FK
  }
  dbo_SurveyPlanScopes["dbo.SurveyPlanScopes"] {
    uniqueidentifier Id PK
    uniqueidentifier SurveyPlanId FK
    uniqueidentifier RouteSectionVersionId FK
  }
  dbo_SurveyPlanPostponements["dbo.SurveyPlanPostponements"] {
    uniqueidentifier Id PK
    uniqueidentifier SurveyPlanId FK
  }
  dbo_SurveyRequests["dbo.SurveyRequests"] {
    uniqueidentifier Id PK
    uniqueidentifier ProjectId FK
    uniqueidentifier RoadSectionId FK
    uniqueidentifier SurveyPlanId FK
    uniqueidentifier RequestedByUserId FK
    uniqueidentifier RoadSectionVersionId FK
  }
  dbo_SurveyRequestScopes["dbo.SurveyRequestScopes"] {
    uniqueidentifier Id PK
    uniqueidentifier SurveyRequestId FK
    uniqueidentifier RouteSectionVersionId FK
  }
  dbo_Surveys["dbo.Surveys"] {
    uniqueidentifier Id PK
    uniqueidentifier SurveyRequestId FK
    uniqueidentifier ProjectId FK
    uniqueidentifier RoadSectionVersionId FK
    uniqueidentifier BaselineConfirmedByUserId FK
  }
  dbo_SurveyAssignments["dbo.SurveyAssignments"] {
    uniqueidentifier Id PK
    uniqueidentifier SurveyRequestId FK
    uniqueidentifier OperatorUserId FK
    uniqueidentifier AssignedByUserId FK
  }
  dbo_Flights["dbo.Flights"] {
    uniqueidentifier Id PK
    uniqueidentifier SurveyId FK
    uniqueidentifier DroneDeviceId FK
    uniqueidentifier OperatorUserId FK
  }
  dbo_SurveyFiles["dbo.SurveyFiles"] {
    uniqueidentifier Id PK
    uniqueidentifier SurveyId FK
    uniqueidentifier FlightId FK
    uniqueidentifier FileId FK
  }
  dbo_SurveyDataVersions["dbo.SurveyDataVersions"] {
    uniqueidentifier Id PK
    uniqueidentifier SurveyId FK
  }
  dbo_QualityChecks["dbo.QualityChecks"] {
    uniqueidentifier Id PK
    uniqueidentifier SurveyFileId FK
    uniqueidentifier SurveyDataVersionId FK
    uniqueidentifier InitiatedByUserId FK
  }
  dbo_SupplementarySurveyRequests["dbo.SupplementarySurveyRequests"] {
    uniqueidentifier Id PK
    uniqueidentifier SurveyId FK
    uniqueidentifier SurveyRequestId FK
    uniqueidentifier RequestedByUserId FK
    uniqueidentifier ApprovedByUserId FK
  }
  dbo_DroneDevices["dbo.DroneDevices"] {
    uniqueidentifier Id PK
  }
  dbo_DroneDevices o|--o{ dbo_Flights : FK_Flights_DroneDevices_DroneDeviceId
  dbo_Surveys ||--o{ dbo_Flights : FK_Flights_Surveys_SurveyId
  dbo_SurveyDataVersions o|--o{ dbo_QualityChecks : FK_QualityChecks_SurveyDataVersions_SurveyDataVersionId
  dbo_SurveyFiles o|--o{ dbo_QualityChecks : FK_QualityChecks_SurveyFiles_SurveyFileId
  dbo_SurveyRequests o|--o{ dbo_SupplementarySurveyRequests : FK_SupplementarySurveyRequests_SurveyRequests_SurveyRequestId
  dbo_Surveys ||--o{ dbo_SupplementarySurveyRequests : FK_SupplementarySurveyRequests_Surveys_SurveyId
  dbo_SurveyRequests ||--o{ dbo_SurveyAssignments : FK_SurveyAssignments_SurveyRequests_SurveyRequestId
  dbo_Surveys ||--o{ dbo_SurveyDataVersions : FK_SurveyDataVersions_Surveys_SurveyId
  dbo_Flights o|--o{ dbo_SurveyFiles : FK_SurveyFiles_Flights_FlightId
  dbo_Surveys ||--o{ dbo_SurveyFiles : FK_SurveyFiles_Surveys_SurveyId
  dbo_SurveyPlans ||--o{ dbo_SurveyPlanPostponements : FK_SurveyPlanPostponements_SurveyPlans_SurveyPlanId
  dbo_SurveyPlans ||--o{ dbo_SurveyPlanScopes : FK_SurveyPlanScopes_SurveyPlans_SurveyPlanId
  dbo_SurveyPlans o|--o{ dbo_SurveyRequests : FK_SurveyRequests_SurveyPlans_SurveyPlanId
  dbo_SurveyRequests ||--o{ dbo_SurveyRequestScopes : FK_SurveyRequestScopes_SurveyRequests_SurveyRequestId
  dbo_SurveyRequests o|--o{ dbo_Surveys : FK_Surveys_SurveyRequests_SurveyRequestId
```

Cross-module enforced FK (full composite columns and delete behavior: [dictionary](current-data-dictionary.md)):

- dbo.Flights -> dbo.Users via FK_Flights_Users_OperatorUserId.
- dbo.QualityChecks -> dbo.Users via FK_QualityChecks_Users_InitiatedByUserId.
- dbo.SupplementarySurveyRequests -> dbo.Users via FK_SupplementarySurveyRequests_Users_ApprovedByUserId.
- dbo.SupplementarySurveyRequests -> dbo.Users via FK_SupplementarySurveyRequests_Users_RequestedByUserId.
- dbo.SurveyAssignments -> dbo.Users via FK_SurveyAssignments_Users_AssignedByUserId.
- dbo.SurveyAssignments -> dbo.Users via FK_SurveyAssignments_Users_OperatorUserId.
- dbo.SurveyFiles -> dbo.Files via FK_SurveyFiles_Files_FileId.
- dbo.SurveyPlans -> dbo.Projects via FK_SurveyPlans_Projects_ProjectId.
- dbo.SurveyPlans -> dbo.RoadSections via FK_SurveyPlans_RoadSections_RoadSectionId.
- dbo.SurveyPlans -> dbo.RoadSectionVersions via FK_SurveyPlans_RoadSectionVersions_RoadSectionVersionId.
- dbo.SurveyPlanScopes -> dbo.RoadSectionVersions via FK_SurveyPlanScopes_RoadSectionVersions_RouteSectionVersionId.
- dbo.SurveyRequests -> dbo.Projects via FK_SurveyRequests_Projects_ProjectId.
- dbo.SurveyRequests -> dbo.RoadSections via FK_SurveyRequests_RoadSections_RoadSectionId.
- dbo.SurveyRequests -> dbo.RoadSectionVersions via FK_SurveyRequests_RoadSectionVersions_RoadSectionVersionId.
- dbo.SurveyRequests -> dbo.Users via FK_SurveyRequests_Users_RequestedByUserId.
- dbo.SurveyRequestScopes -> dbo.RoadSectionVersions via FK_SurveyRequestScopes_RoadSectionVersions_RouteSectionVersionId.
- dbo.Surveys -> dbo.Projects via FK_Surveys_Projects_ProjectId.
- dbo.Surveys -> dbo.RoadSectionVersions via FK_Surveys_RoadSectionVersions_RoadSectionVersionId.
- dbo.Surveys -> dbo.Users via FK_Surveys_Users_BaselineConfirmedByUserId.

## files

```mermaid
erDiagram
  dbo_Files["dbo.Files"] {
    uniqueidentifier Id PK
    uniqueidentifier UploadedByUserId FK
  }
  dbo_FileScopes["dbo.FileScopes"] {
    uniqueidentifier Id PK
    uniqueidentifier FileId FK
    uniqueidentifier ProjectId FK
    uniqueidentifier OwnerUserId FK
  }
  dbo_UploadSessions["dbo.UploadSessions"] {
    uniqueidentifier Id PK
    uniqueidentifier FileId FK
    uniqueidentifier OwnerUserId FK
  }
  dbo_UploadParts["dbo.UploadParts"] {
    uniqueidentifier Id PK
    uniqueidentifier UploadSessionId FK
  }
  dbo_Files ||--o| dbo_FileScopes : FK_FileScopes_Files_FileId
  dbo_UploadSessions ||--o{ dbo_UploadParts : FK_UploadParts_UploadSessions_UploadSessionId
  dbo_Files ||--o{ dbo_UploadSessions : FK_UploadSessions_Files_FileId
```

Cross-module enforced FK (full composite columns and delete behavior: [dictionary](current-data-dictionary.md)):

- dbo.Files -> dbo.Users via FK_Files_Users_UploadedByUserId.
- dbo.FileScopes -> dbo.Projects via FK_FileScopes_Projects_ProjectId.
- dbo.FileScopes -> dbo.Users via FK_FileScopes_Users_OwnerUserId.
- dbo.UploadSessions -> dbo.Users via FK_UploadSessions_Users_OwnerUserId.

## processing

```mermaid
erDiagram
  dbo_ProcessingBlocks["dbo.ProcessingBlocks"] {
    uniqueidentifier Id PK
    uniqueidentifier SurveyDataVersionId FK
  }
  dbo_ProcessingJobs["dbo.ProcessingJobs"] {
    uniqueidentifier Id PK
    uniqueidentifier ProcessingBlockId FK
    uniqueidentifier ModelVersionId FK
  }
  dbo_ProcessingAttempts["dbo.ProcessingAttempts"] {
    uniqueidentifier Id PK
    uniqueidentifier ProcessingJobId FK
  }
  dbo_AIModelVersions["dbo.AIModelVersions"] {
    uniqueidentifier Id PK
    uniqueidentifier ReleasedByUserId FK
  }
  dbo_ValidationRuns["dbo.ValidationRuns"] {
    uniqueidentifier Id PK
    uniqueidentifier ModelVersionId FK
  }
  dbo_ProcessingJobs ||--o{ dbo_ProcessingAttempts : FK_ProcessingAttempts_ProcessingJobs_ProcessingJobId
  dbo_AIModelVersions ||--o{ dbo_ProcessingJobs : FK_ProcessingJobs_AIModelVersions_ModelVersionId
  dbo_ProcessingBlocks ||--o{ dbo_ProcessingJobs : FK_ProcessingJobs_ProcessingBlocks_ProcessingBlockId
  dbo_AIModelVersions ||--o{ dbo_ValidationRuns : FK_ValidationRuns_AIModelVersions_ModelVersionId
```

Cross-module enforced FK (full composite columns and delete behavior: [dictionary](current-data-dictionary.md)):

- dbo.AIModelVersions -> dbo.Users via FK_AIModelVersions_Users_ReleasedByUserId.
- dbo.ProcessingBlocks -> dbo.SurveyDataVersions via FK_ProcessingBlocks_SurveyDataVersions_SurveyDataVersionId.

## defect-inspection

```mermaid
erDiagram
  dbo_DefectTypes["dbo.DefectTypes"] {
    varchar Code PK
  }
  dbo_CauseCategories["dbo.CauseCategories"] {
    varchar Code PK
  }
  dbo_SeverityRuleVersions["dbo.SeverityRuleVersions"] {
    uniqueidentifier Id PK
  }
  dbo_Defects["dbo.Defects"] {
    uniqueidentifier Id PK
    varchar DefectTypeCode FK
    varchar CauseCategoryCode FK
    uniqueidentifier ProjectId FK
    uniqueidentifier RoadSectionVersionId FK
    uniqueidentifier SourceAIDetectionId FK
  }
  dbo_AIDetections["dbo.AIDetections"] {
    uniqueidentifier Id PK
    uniqueidentifier ProcessingJobId FK
    uniqueidentifier ModelVersionId FK
    uniqueidentifier RoadSectionVersionId FK
    varchar DefectTypeCode FK
  }
  dbo_DefectVerificationLogs["dbo.DefectVerificationLogs"] {
    uniqueidentifier Id PK
    uniqueidentifier DefectId FK
    uniqueidentifier AIDetectionId FK
    uniqueidentifier SeverityRuleVersionId FK
    uniqueidentifier FieldInspectionTaskId FK
    uniqueidentifier VerifiedByUserId FK
  }
  dbo_FieldInspectionTasks["dbo.FieldInspectionTasks"] {
    uniqueidentifier Id PK
    uniqueidentifier ProjectId FK
    uniqueidentifier DefectId FK
    uniqueidentifier SurveyId FK
    uniqueidentifier RoadSectionVersionId FK
    uniqueidentifier AssignedByUserId FK
    uniqueidentifier ReviewedByUserId FK
  }
  dbo_FieldInspectionAssignments["dbo.FieldInspectionAssignments"] {
    uniqueidentifier Id PK
    uniqueidentifier FieldInspectionTaskId FK
    uniqueidentifier AssignedToUserId FK
    uniqueidentifier AssignedByUserId FK
  }
  dbo_FieldInspectionSessions["dbo.FieldInspectionSessions"] {
    uniqueidentifier Id PK
    uniqueidentifier FieldInspectionTaskId FK
    uniqueidentifier ProjectId FK
    uniqueidentifier RoadSectionVersionId FK
    uniqueidentifier SurveyId FK
    uniqueidentifier InspectorUserId FK
    uniqueidentifier EvidenceFileId FK
  }
  dbo_GroundTruthMeasurements["dbo.GroundTruthMeasurements"] {
    uniqueidentifier Id PK
    uniqueidentifier FieldInspectionSessionId FK
    uniqueidentifier RoadSectionVersionId FK
    uniqueidentifier SurveyId FK
    uniqueidentifier DefectId FK
    uniqueidentifier EvidenceFileId FK
  }
  dbo_DerivedMeasurements["dbo.DerivedMeasurements"] {
    uniqueidentifier Id PK
    uniqueidentifier SurveyDataVersionId FK
    uniqueidentifier RoadSectionVersionId FK
  }
  dbo_MeasurementValidationSamples["dbo.MeasurementValidationSamples"] {
    uniqueidentifier Id PK
    uniqueidentifier ValidationRunId FK
    uniqueidentifier GroundTruthMeasurementId FK
    uniqueidentifier DerivedMeasurementId FK
  }
  dbo_DefectTypes o|--o{ dbo_AIDetections : FK_AIDetections_DefectTypes_DefectTypeCode
  dbo_AIDetections o|--o{ dbo_Defects : FK_Defects_AIDetections_SourceAIDetectionId
  dbo_CauseCategories o|--o{ dbo_Defects : FK_Defects_CauseCategories_CauseCategoryCode
  dbo_DefectTypes ||--o{ dbo_Defects : FK_Defects_DefectTypes_DefectTypeCode
  dbo_AIDetections o|--o{ dbo_DefectVerificationLogs : FK_DefectVerificationLogs_AIDetections_AIDetectionId
  dbo_Defects o|--o{ dbo_DefectVerificationLogs : FK_DefectVerificationLogs_Defects_DefectId
  dbo_FieldInspectionTasks o|--o{ dbo_DefectVerificationLogs : FK_DefectVerificationLogs_FieldInspectionTasks_FieldInspectionTaskId
  dbo_SeverityRuleVersions o|--o{ dbo_DefectVerificationLogs : FK_DefectVerificationLogs_SeverityRuleVersions_SeverityRuleVersionId
  dbo_FieldInspectionTasks ||--o{ dbo_FieldInspectionAssignments : FK_FieldInspectionAssignments_FieldInspectionTasks_FieldInspectionTaskId
  dbo_FieldInspectionTasks o|--o{ dbo_FieldInspectionSessions : FK_FieldInspectionSessions_FieldInspectionTasks_FieldInspectionTaskId
  dbo_Defects ||--o{ dbo_FieldInspectionTasks : FK_FieldInspectionTasks_Defects_DefectId
  dbo_Defects o|--o{ dbo_GroundTruthMeasurements : FK_GroundTruthMeasurements_Defects_DefectId
  dbo_FieldInspectionSessions ||--o{ dbo_GroundTruthMeasurements : FK_GroundTruthMeasurements_FieldInspectionSessions_FieldInspectionSessionId
  dbo_DerivedMeasurements ||--o{ dbo_MeasurementValidationSamples : FK_MeasurementValidationSamples_DerivedMeasurements_DerivedMeasurementId
  dbo_GroundTruthMeasurements ||--o{ dbo_MeasurementValidationSamples : FK_MeasurementValidationSamples_GroundTruthMeasurements_GroundTruthMeasurementId
```

Cross-module enforced FK (full composite columns and delete behavior: [dictionary](current-data-dictionary.md)):

- dbo.AIDetections -> dbo.AIModelVersions via FK_AIDetections_AIModelVersions_ModelVersionId.
- dbo.AIDetections -> dbo.ProcessingJobs via FK_AIDetections_ProcessingJobs_ProcessingJobId.
- dbo.AIDetections -> dbo.RoadSectionVersions via FK_AIDetections_RoadSectionVersions_RoadSectionVersionId.
- dbo.Defects -> dbo.Projects via FK_Defects_Projects_ProjectId.
- dbo.Defects -> dbo.RoadSectionVersions via FK_Defects_RoadSectionVersions_RoadSectionVersionId.
- dbo.DefectVerificationLogs -> dbo.Users via FK_DefectVerificationLogs_Users_VerifiedByUserId.
- dbo.DerivedMeasurements -> dbo.RoadSectionVersions via FK_DerivedMeasurements_RoadSectionVersions_RoadSectionVersionId.
- dbo.DerivedMeasurements -> dbo.SurveyDataVersions via FK_DerivedMeasurements_SurveyDataVersions_SurveyDataVersionId.
- dbo.FieldInspectionAssignments -> dbo.Users via FK_FieldInspectionAssignments_Users_AssignedByUserId.
- dbo.FieldInspectionAssignments -> dbo.Users via FK_FieldInspectionAssignments_Users_AssignedToUserId.
- dbo.FieldInspectionSessions -> dbo.Files via FK_FieldInspectionSessions_Files_EvidenceFileId.
- dbo.FieldInspectionSessions -> dbo.Projects via FK_FieldInspectionSessions_Projects_ProjectId.
- dbo.FieldInspectionSessions -> dbo.RoadSectionVersions via FK_FieldInspectionSessions_RoadSectionVersions_RoadSectionVersionId.
- dbo.FieldInspectionSessions -> dbo.Surveys via FK_FieldInspectionSessions_Surveys_SurveyId.
- dbo.FieldInspectionSessions -> dbo.Users via FK_FieldInspectionSessions_Users_InspectorUserId.
- dbo.FieldInspectionTasks -> dbo.Projects via FK_FieldInspectionTasks_Projects_ProjectId.
- dbo.FieldInspectionTasks -> dbo.RoadSectionVersions via FK_FieldInspectionTasks_RoadSectionVersions_RoadSectionVersionId.
- dbo.FieldInspectionTasks -> dbo.Surveys via FK_FieldInspectionTasks_Surveys_SurveyId.
- dbo.FieldInspectionTasks -> dbo.Users via FK_FieldInspectionTasks_Users_AssignedByUserId.
- dbo.FieldInspectionTasks -> dbo.Users via FK_FieldInspectionTasks_Users_ReviewedByUserId.
- dbo.GroundTruthMeasurements -> dbo.Files via FK_GroundTruthMeasurements_Files_EvidenceFileId.
- dbo.GroundTruthMeasurements -> dbo.RoadSectionVersions via FK_GroundTruthMeasurements_RoadSectionVersions_RoadSectionVersionId.
- dbo.GroundTruthMeasurements -> dbo.Surveys via FK_GroundTruthMeasurements_Surveys_SurveyId.
- dbo.MeasurementValidationSamples -> dbo.ValidationRuns via FK_MeasurementValidationSamples_ValidationRuns_ValidationRunId.

## messaging

```mermaid
erDiagram
  dbo_AuditLogs["dbo.AuditLogs"] {
    uniqueidentifier Id PK
    uniqueidentifier ActorUserId FK
  }
  dbo_IdempotencyRecords["dbo.IdempotencyRecords"] {
    uniqueidentifier Id PK
  }
  dbo_OutboxMessages["dbo.OutboxMessages"] {
    uniqueidentifier Id PK
  }
  dbo_ConsumerEffectReceipts["dbo.ConsumerEffectReceipts"] {
    uniqueidentifier Id PK
    uniqueidentifier MessageId FK
  }
  dbo_Notifications["dbo.Notifications"] {
    uniqueidentifier Id PK
    uniqueidentifier RecipientUserId FK
  }
  dbo_OutboxMessages ||--o{ dbo_ConsumerEffectReceipts : FK_ConsumerEffectReceipts_OutboxMessages_MessageId
```

Cross-module enforced FK (full composite columns and delete behavior: [dictionary](current-data-dictionary.md)):

- dbo.AuditLogs -> dbo.Users via FK_AuditLogs_Users_ActorUserId.
- dbo.Notifications -> dbo.Users via FK_Notifications_Users_RecipientUserId.

Logical application links without a SQL FK are intentionally omitted; inspect service/persistence code before drawing any such edge. On an edge principal-to-child, `||` means the child FK is required and `o|` means nullable; `o{` means zero-to-many child rows and `o|` zero-to-one when an exact unique FK index exists. Composite columns and delete behavior are in the dictionary.
