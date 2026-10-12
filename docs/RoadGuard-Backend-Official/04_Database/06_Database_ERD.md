# Database ERD by bounded context

**EF_LOGICAL_MODEL / MIGRATION_DERIVED_PHYSICAL_MODEL** at cdeb1ca. Every191table appears in a primary CLR namespace context; cross-context targets are retained. Edges are657fresh EF-relational constraints plus5current migration SQL-only constraints; multiple FK columns between the same tables are collapsed visually, complete662definitions are in relationship catalog. No owner-navigation pseudo-FKs or invented cardinalities. LIVE_SQL_VERIFIED NOT_RUN.

## Auditing

```mermaid
flowchart LR
  AuditLogs["AuditLogs"]
  Users["Users"]
  AuditLogs --> Users
```

Primary tables: `dbo.AuditLogs`

## Candidates

```mermaid
flowchart LR
  AIDetections["AIDetections"]
  CauseCategories["CauseCategories"]
  DefectTypes["DefectTypes"]
  Defects["Defects"]
  Projects["Projects"]
  Reports["Reports"]
  RoadSectionVersions["RoadSectionVersions"]
  RoadSegments["RoadSegments"]
  SourceDecisions["SourceDecisions"]
  Users["Users"]
  SourceDecisions --> AIDetections
  SourceDecisions --> CauseCategories
  SourceDecisions --> DefectTypes
  SourceDecisions --> Defects
  SourceDecisions --> Projects
  SourceDecisions --> Reports
  SourceDecisions --> RoadSectionVersions
  SourceDecisions --> RoadSegments
  SourceDecisions --> SourceDecisions
  SourceDecisions --> Users
```

Primary tables: `dbo.SourceDecisions`

## Cases

```mermaid
flowchart LR
  CaseConclusions["CaseConclusions"]
  CasePublications["CasePublications"]
  CaseReportLinkHistory["CaseReportLinkHistory"]
  IncidentCases["IncidentCases"]
  Projects["Projects"]
  RoadSegmentSets["RoadSegmentSets"]
  Users["Users"]
  CaseConclusions --> IncidentCases
  CasePublications --> IncidentCases
  CaseReportLinkHistory --> IncidentCases
  CaseReportLinkHistory --> Users
  IncidentCases --> IncidentCases
  IncidentCases --> Projects
  IncidentCases --> RoadSegmentSets
```

Primary tables: `dbo.CaseConclusions`, `dbo.CasePublications`, `dbo.CaseReportLinkHistory`, `dbo.IncidentCases`

## Catalogs

```mermaid
flowchart LR
  CauseCategories["CauseCategories"]
  DefectTypes["DefectTypes"]
  SeverityRuleVersions["SeverityRuleVersions"]
```

Primary tables: `dbo.CauseCategories`, `dbo.DefectTypes`, `dbo.SeverityRuleVersions`

## Clocks

```mermaid
flowchart LR
  BusinessDutyAppointments["BusinessDutyAppointments"]
  BusinessReceivingRequests["BusinessReceivingRequests"]
  DeadlineBreaches["DeadlineBreaches"]
  DeadlineClocks["DeadlineClocks"]
  DeadlineDutyAppointments["DeadlineDutyAppointments"]
  DeadlineExtensions["DeadlineExtensions"]
  Projects["Projects"]
  Users["Users"]
  BusinessDutyAppointments --> BusinessReceivingRequests
  BusinessDutyAppointments --> Users
  BusinessReceivingRequests --> DeadlineClocks
  BusinessReceivingRequests --> Projects
  BusinessReceivingRequests --> Users
  DeadlineBreaches --> DeadlineClocks
  DeadlineClocks --> Projects
  DeadlineClocks --> Users
  DeadlineDutyAppointments --> DeadlineClocks
  DeadlineDutyAppointments --> Users
  DeadlineExtensions --> DeadlineClocks
  DeadlineExtensions --> Users
```

Primary tables: `dbo.BusinessDutyAppointments`, `dbo.BusinessReceivingRequests`, `dbo.DeadlineBreaches`, `dbo.DeadlineClocks`, `dbo.DeadlineDutyAppointments`, `dbo.DeadlineExtensions`

## Defects

```mermaid
flowchart LR
  AIDetections["AIDetections"]
  CauseCategories["CauseCategories"]
  DefectTypes["DefectTypes"]
  DefectVerificationLogs["DefectVerificationLogs"]
  Defects["Defects"]
  FieldInspectionTasks["FieldInspectionTasks"]
  Projects["Projects"]
  RoadSectionVersions["RoadSectionVersions"]
  SeverityRuleVersions["SeverityRuleVersions"]
  Users["Users"]
  DefectVerificationLogs --> AIDetections
  DefectVerificationLogs --> Defects
  DefectVerificationLogs --> FieldInspectionTasks
  DefectVerificationLogs --> SeverityRuleVersions
  DefectVerificationLogs --> Users
  Defects --> AIDetections
  Defects --> CauseCategories
  Defects --> DefectTypes
  Defects --> Projects
  Defects --> RoadSectionVersions
```

Primary tables: `dbo.DefectVerificationLogs`, `dbo.Defects`

## Devices

```mermaid
flowchart LR
  DroneDevices["DroneDevices"]
```

Primary tables: `dbo.DroneDevices`

## Exports

```mermaid
flowchart LR
  Anh02ExportJobs["Anh02ExportJobs"]
  Anh02ExportSnapshotFiles["Anh02ExportSnapshotFiles"]
  Anh02ExportSnapshots["Anh02ExportSnapshots"]
  Anh02GeneratedArtifacts["Anh02GeneratedArtifacts"]
  Files["Files"]
  Projects["Projects"]
  Users["Users"]
  Anh02ExportJobs --> Anh02ExportSnapshots
  Anh02ExportJobs --> Anh02GeneratedArtifacts
  Anh02ExportJobs --> Projects
  Anh02ExportJobs --> Users
  Anh02ExportSnapshotFiles --> Anh02ExportSnapshots
  Anh02ExportSnapshotFiles --> Files
  Anh02ExportSnapshots --> Projects
  Anh02GeneratedArtifacts --> Anh02ExportJobs
  Anh02GeneratedArtifacts --> Anh02ExportSnapshots
  Anh02GeneratedArtifacts --> Files
```

Primary tables: `dbo.Anh02ExportJobs`, `dbo.Anh02ExportSnapshotFiles`, `dbo.Anh02ExportSnapshots`, `dbo.Anh02GeneratedArtifacts`

## Files

```mermaid
flowchart LR
  FileScopes["FileScopes"]
  Files["Files"]
  Projects["Projects"]
  UploadParts["UploadParts"]
  UploadSessions["UploadSessions"]
  Users["Users"]
  FileScopes --> Files
  FileScopes --> Projects
  FileScopes --> Users
  Files --> Users
  UploadParts --> UploadSessions
  UploadSessions --> Files
  UploadSessions --> Users
```

Primary tables: `dbo.FileScopes`, `dbo.Files`, `dbo.UploadParts`, `dbo.UploadSessions`

## Idempotency

```mermaid
flowchart LR
  IdempotencyRecords["IdempotencyRecords"]
```

Primary tables: `dbo.IdempotencyRecords`

## Identity

```mermaid
flowchart LR
  AccountStatusChangeLogs["AccountStatusChangeLogs"]
  PasswordRecoveryRequests["PasswordRecoveryRequests"]
  PasswordResetLogs["PasswordResetLogs"]
  Projects["Projects"]
  RefreshTokens["RefreshTokens"]
  ReporterRegistrationIntents["ReporterRegistrationIntents"]
  Roles["Roles"]
  Sessions["Sessions"]
  StaffInvitationProjects["StaffInvitationProjects"]
  StaffInvitations["StaffInvitations"]
  Users["Users"]
  AccountStatusChangeLogs --> Users
  PasswordRecoveryRequests --> Users
  PasswordResetLogs --> Users
  RefreshTokens --> Sessions
  ReporterRegistrationIntents --> Users
  Sessions --> Users
  StaffInvitationProjects --> Projects
  StaffInvitationProjects --> StaffInvitations
  StaffInvitations --> Users
  Users --> Roles
```

Primary tables: `dbo.AccountStatusChangeLogs`, `dbo.PasswordRecoveryRequests`, `dbo.PasswordResetLogs`, `dbo.RefreshTokens`, `dbo.ReporterRegistrationIntents`, `dbo.Roles`, `dbo.Sessions`, `dbo.StaffInvitationProjects`, `dbo.StaffInvitations`, `dbo.Users`

## Inspections

```mermaid
flowchart LR
  CrsProfileRevisions["CrsProfileRevisions"]
  Defects["Defects"]
  FieldInspectionAssignments["FieldInspectionAssignments"]
  FieldInspectionEvidenceLinks["FieldInspectionEvidenceLinks"]
  FieldInspectionEvidenceReuseDecisions["FieldInspectionEvidenceReuseDecisions"]
  FieldInspectionLocationProofs["FieldInspectionLocationProofs"]
  FieldInspectionOperationOrigins["FieldInspectionOperationOrigins"]
  FieldInspectionReviews["FieldInspectionReviews"]
  FieldInspectionSessions["FieldInspectionSessions"]
  FieldInspectionSubmissions["FieldInspectionSubmissions"]
  FieldInspectionTaskEvents["FieldInspectionTaskEvents"]
  FieldInspectionTasks["FieldInspectionTasks"]
  FieldTaskStartOrigins["FieldTaskStartOrigins"]
  Files["Files"]
  GeometryLocationImpactDecisions["GeometryLocationImpactDecisions"]
  GeometryLocationImpacts["GeometryLocationImpacts"]
  GeometryMapPublications["GeometryMapPublications"]
  GroundTruthMeasurements["GroundTruthMeasurements"]
  PavementLayoutRevisions["PavementLayoutRevisions"]
  Projects["Projects"]
  RepairItems["RepairItems"]
  RoadSectionVersions["RoadSectionVersions"]
  RoadSegmentSets["RoadSegmentSets"]
  Surveys["Surveys"]
  Users["Users"]
  FieldInspectionAssignments --> FieldInspectionTasks
  FieldInspectionAssignments --> Users
  FieldInspectionEvidenceLinks --> FieldInspectionAssignments
  FieldInspectionEvidenceLinks --> FieldInspectionSubmissions
  FieldInspectionEvidenceLinks --> FieldInspectionTasks
  FieldInspectionEvidenceLinks --> Files
  FieldInspectionEvidenceLinks --> Projects
  FieldInspectionEvidenceReuseDecisions --> FieldInspectionTasks
  FieldInspectionEvidenceReuseDecisions --> Files
  FieldInspectionEvidenceReuseDecisions --> Projects
  FieldInspectionEvidenceReuseDecisions --> Users
  FieldInspectionLocationProofs --> FieldInspectionSubmissions
  FieldInspectionLocationProofs --> FieldInspectionTasks
  FieldInspectionLocationProofs --> Projects
  FieldInspectionOperationOrigins --> FieldInspectionTasks
  FieldInspectionOperationOrigins --> Projects
  FieldInspectionOperationOrigins --> Users
  FieldInspectionReviews --> FieldInspectionSubmissions
  FieldInspectionReviews --> FieldInspectionTasks
  FieldInspectionReviews --> Projects
  FieldInspectionReviews --> Users
  FieldInspectionSessions --> FieldInspectionTasks
  FieldInspectionSessions --> Files
  FieldInspectionSessions --> Projects
  FieldInspectionSessions --> RoadSectionVersions
  FieldInspectionSessions --> Surveys
  FieldInspectionSessions --> Users
  FieldInspectionSubmissions --> FieldInspectionAssignments
  FieldInspectionSubmissions --> FieldInspectionOperationOrigins
  FieldInspectionSubmissions --> FieldInspectionSessions
  FieldInspectionSubmissions --> FieldInspectionSubmissions
  FieldInspectionSubmissions --> FieldInspectionTasks
  FieldInspectionSubmissions --> FieldTaskStartOrigins
  FieldInspectionSubmissions --> Projects
  FieldInspectionSubmissions --> Users
  FieldInspectionTaskEvents --> FieldInspectionAssignments
  FieldInspectionTaskEvents --> FieldInspectionTasks
  FieldInspectionTaskEvents --> GeometryLocationImpactDecisions
  FieldInspectionTaskEvents --> GeometryLocationImpacts
  FieldInspectionTaskEvents --> Projects
  FieldInspectionTaskEvents --> Users
  FieldInspectionTasks --> CrsProfileRevisions
  FieldInspectionTasks --> Defects
  FieldInspectionTasks --> GeometryMapPublications
  FieldInspectionTasks --> PavementLayoutRevisions
  FieldInspectionTasks --> Projects
  FieldInspectionTasks --> RepairItems
  FieldInspectionTasks --> RoadSectionVersions
  FieldInspectionTasks --> RoadSegmentSets
  FieldInspectionTasks --> Surveys
  FieldInspectionTasks --> Users
  FieldTaskStartOrigins --> CrsProfileRevisions
  FieldTaskStartOrigins --> FieldInspectionAssignments
  FieldTaskStartOrigins --> FieldInspectionOperationOrigins
  FieldTaskStartOrigins --> FieldInspectionTasks
  FieldTaskStartOrigins --> GeometryMapPublications
  FieldTaskStartOrigins --> PavementLayoutRevisions
  FieldTaskStartOrigins --> Projects
  FieldTaskStartOrigins --> RoadSectionVersions
  FieldTaskStartOrigins --> RoadSegmentSets
  FieldTaskStartOrigins --> Users
  GroundTruthMeasurements --> Defects
  GroundTruthMeasurements --> FieldInspectionSessions
  GroundTruthMeasurements --> Files
  GroundTruthMeasurements --> RoadSectionVersions
  GroundTruthMeasurements --> Surveys
```

Primary tables: `dbo.FieldInspectionAssignments`, `dbo.FieldInspectionEvidenceLinks`, `dbo.FieldInspectionEvidenceReuseDecisions`, `dbo.FieldInspectionLocationProofs`, `dbo.FieldInspectionOperationOrigins`, `dbo.FieldInspectionReviews`, `dbo.FieldInspectionSessions`, `dbo.FieldInspectionSubmissions`, `dbo.FieldInspectionTaskEvents`, `dbo.FieldInspectionTasks`, `dbo.FieldTaskStartOrigins`, `dbo.GroundTruthMeasurements`

## Messaging

```mermaid
flowchart LR
  ConsumerEffectReceipts["ConsumerEffectReceipts"]
  DeadlineClocks["DeadlineClocks"]
  H6NotificationAudits["H6NotificationAudits"]
  H6NotificationCalendar["H6NotificationCalendar"]
  H6NotificationDeliveries["H6NotificationDeliveries"]
  H6NotificationDeliveryAttempts["H6NotificationDeliveryAttempts"]
  H6NotificationEventReceipts["H6NotificationEventReceipts"]
  H6NotificationOccurrences["H6NotificationOccurrences"]
  H6NotificationScopes["H6NotificationScopes"]
  Notifications["Notifications"]
  OutboxMessages["OutboxMessages"]
  Projects["Projects"]
  Users["Users"]
  WeeklyReviewDigestDuties["WeeklyReviewDigestDuties"]
  WeeklyReviewDigests["WeeklyReviewDigests"]
  WeeklyReviewRecoveryPeriods["WeeklyReviewRecoveryPeriods"]
  ConsumerEffectReceipts --> OutboxMessages
  H6NotificationAudits --> H6NotificationAudits
  H6NotificationAudits --> Notifications
  H6NotificationAudits --> OutboxMessages
  H6NotificationAudits --> Projects
  H6NotificationCalendar --> DeadlineClocks
  H6NotificationCalendar --> OutboxMessages
  H6NotificationDeliveries --> H6NotificationOccurrences
  H6NotificationDeliveries --> Notifications
  H6NotificationDeliveries --> Users
  H6NotificationDeliveryAttempts --> H6NotificationDeliveries
  H6NotificationEventReceipts --> H6NotificationOccurrences
  H6NotificationEventReceipts --> OutboxMessages
  H6NotificationOccurrences --> OutboxMessages
  H6NotificationOccurrences --> Projects
  H6NotificationScopes --> H6NotificationAudits
  H6NotificationScopes --> H6NotificationOccurrences
  H6NotificationScopes --> Notifications
  H6NotificationScopes --> Projects
  Notifications --> Users
  WeeklyReviewDigestDuties --> DeadlineClocks
  WeeklyReviewDigestDuties --> WeeklyReviewDigests
  WeeklyReviewDigests --> Projects
  WeeklyReviewDigests --> Users
  WeeklyReviewDigests --> WeeklyReviewRecoveryPeriods
  WeeklyReviewRecoveryPeriods --> Projects
```

Primary tables: `dbo.ConsumerEffectReceipts`, `dbo.H6NotificationAudits`, `dbo.H6NotificationCalendar`, `dbo.H6NotificationDeliveries`, `dbo.H6NotificationDeliveryAttempts`, `dbo.H6NotificationEventReceipts`, `dbo.H6NotificationOccurrences`, `dbo.H6NotificationScopes`, `dbo.Notifications`, `dbo.OutboxMessages`, `dbo.WeeklyReviewDigestDuties`, `dbo.WeeklyReviewDigests`, `dbo.WeeklyReviewRecoveryPeriods`

## Models

```mermaid
flowchart LR
  AIDetections["AIDetections"]
  CandidateSourceHeads["CandidateSourceHeads"]
  CaseConclusionDefects["CaseConclusionDefects"]
  CaseConclusionEvidence["CaseConclusionEvidence"]
  CaseConclusions["CaseConclusions"]
  CasePublicationDefects["CasePublicationDefects"]
  CasePublicationEvidence["CasePublicationEvidence"]
  CasePublicationRecipients["CasePublicationRecipients"]
  CasePublications["CasePublications"]
  CaseReportLinkHistory["CaseReportLinkHistory"]
  CaseReportLinkHistoryReports["CaseReportLinkHistoryReports"]
  CaseReportLinks["CaseReportLinks"]
  DefectSourceLinks["DefectSourceLinks"]
  DefectTypes["DefectTypes"]
  Defects["Defects"]
  Files["Files"]
  IncidentCases["IncidentCases"]
  Projects["Projects"]
  ReportOriginalEvidence["ReportOriginalEvidence"]
  ReportSupplementEvidence["ReportSupplementEvidence"]
  Reports["Reports"]
  SourceDecisions["SourceDecisions"]
  TrainingLabelReviews["TrainingLabelReviews"]
  TrainingLabelRevisions["TrainingLabelRevisions"]
  TrainingLabels["TrainingLabels"]
  UploadMultipartSweeps["UploadMultipartSweeps"]
  UploadSessions["UploadSessions"]
  Users["Users"]
  CandidateSourceHeads --> Projects
  CandidateSourceHeads --> SourceDecisions
  CaseConclusionDefects --> CaseConclusions
  CaseConclusionDefects --> Defects
  CaseConclusionEvidence --> CaseConclusions
  CaseConclusionEvidence --> ReportOriginalEvidence
  CaseConclusionEvidence --> ReportSupplementEvidence
  CaseConclusionEvidence --> Reports
  CasePublicationDefects --> CasePublications
  CasePublicationDefects --> Defects
  CasePublicationEvidence --> CasePublicationRecipients
  CasePublicationEvidence --> ReportOriginalEvidence
  CasePublicationEvidence --> ReportSupplementEvidence
  CasePublicationEvidence --> Reports
  CasePublicationRecipients --> CasePublications
  CasePublicationRecipients --> Reports
  CaseReportLinkHistoryReports --> CaseReportLinkHistory
  CaseReportLinkHistoryReports --> Reports
  CaseReportLinks --> IncidentCases
  CaseReportLinks --> Reports
  DefectSourceLinks --> AIDetections
  DefectSourceLinks --> Defects
  DefectSourceLinks --> Projects
  DefectSourceLinks --> Reports
  DefectSourceLinks --> SourceDecisions
  TrainingLabelReviews --> TrainingLabelRevisions
  TrainingLabelReviews --> Users
  TrainingLabelRevisions --> DefectTypes
  TrainingLabelRevisions --> Files
  TrainingLabelRevisions --> TrainingLabels
  TrainingLabels --> AIDetections
  TrainingLabels --> Projects
  TrainingLabels --> Reports
  UploadMultipartSweeps --> UploadSessions
```

Primary tables: `dbo.CandidateSourceHeads`, `dbo.CaseConclusionDefects`, `dbo.CaseConclusionEvidence`, `dbo.CasePublicationDefects`, `dbo.CasePublicationEvidence`, `dbo.CasePublicationRecipients`, `dbo.CaseReportLinkHistoryReports`, `dbo.CaseReportLinks`, `dbo.DefectSourceLinks`, `dbo.TrainingLabelReviews`, `dbo.TrainingLabelRevisions`, `dbo.TrainingLabels`, `dbo.UploadMultipartSweeps`

## Offline

```mermaid
flowchart LR
  FieldInspectionAssignments["FieldInspectionAssignments"]
  FieldInspectionOperationOrigins["FieldInspectionOperationOrigins"]
  FieldInspectionTasks["FieldInspectionTasks"]
  Files["Files"]
  OfflineAdmittedFileReferences["OfflineAdmittedFileReferences"]
  OfflineDeviceRegistrations["OfflineDeviceRegistrations"]
  OfflineDeviceRevocations["OfflineDeviceRevocations"]
  OfflineEncryptedCaptureArtifacts["OfflineEncryptedCaptureArtifacts"]
  OfflineEncryptedPackages["OfflineEncryptedPackages"]
  OfflineEvidenceCaptureReferences["OfflineEvidenceCaptureReferences"]
  OfflineHandoverGrantRevocations["OfflineHandoverGrantRevocations"]
  OfflineHandoverGrants["OfflineHandoverGrants"]
  OfflineOperationAdmissions["OfflineOperationAdmissions"]
  OfflineOperationBindings["OfflineOperationBindings"]
  OfflineOperationResults["OfflineOperationResults"]
  OfflineOriginTimeVerifications["OfflineOriginTimeVerifications"]
  OfflinePackageFileReferences["OfflinePackageFileReferences"]
  OfflineSyncBatches["OfflineSyncBatches"]
  OfflineTaskSnapshots["OfflineTaskSnapshots"]
  Projects["Projects"]
  RepairItems["RepairItems"]
  UploadSessions["UploadSessions"]
  Users["Users"]
  OfflineAdmittedFileReferences --> FieldInspectionTasks
  OfflineAdmittedFileReferences --> Files
  OfflineAdmittedFileReferences --> OfflineOperationAdmissions
  OfflineAdmittedFileReferences --> OfflineOperationBindings
  OfflineAdmittedFileReferences --> Projects
  OfflineAdmittedFileReferences --> Users
  OfflineDeviceRegistrations --> Projects
  OfflineDeviceRegistrations --> Users
  OfflineDeviceRevocations --> OfflineDeviceRegistrations
  OfflineDeviceRevocations --> Projects
  OfflineDeviceRevocations --> Users
  OfflineEncryptedCaptureArtifacts --> FieldInspectionTasks
  OfflineEncryptedCaptureArtifacts --> OfflineDeviceRegistrations
  OfflineEncryptedCaptureArtifacts --> OfflineEncryptedPackages
  OfflineEncryptedCaptureArtifacts --> Projects
  OfflineEncryptedCaptureArtifacts --> Users
  OfflineEncryptedPackages --> OfflineDeviceRegistrations
  OfflineEncryptedPackages --> Projects
  OfflineEncryptedPackages --> Users
  OfflineEvidenceCaptureReferences --> FieldInspectionTasks
  OfflineEvidenceCaptureReferences --> Files
  OfflineEvidenceCaptureReferences --> OfflineHandoverGrants
  OfflineEvidenceCaptureReferences --> OfflineOperationAdmissions
  OfflineEvidenceCaptureReferences --> OfflineOperationBindings
  OfflineEvidenceCaptureReferences --> Projects
  OfflineEvidenceCaptureReferences --> UploadSessions
  OfflineEvidenceCaptureReferences --> Users
  OfflineHandoverGrantRevocations --> OfflineHandoverGrants
  OfflineHandoverGrantRevocations --> Projects
  OfflineHandoverGrantRevocations --> Users
  OfflineHandoverGrants --> OfflineDeviceRegistrations
  OfflineHandoverGrants --> OfflineEncryptedPackages
  OfflineHandoverGrants --> Projects
  OfflineHandoverGrants --> Users
  OfflineOperationAdmissions --> OfflineHandoverGrants
  OfflineOperationAdmissions --> OfflineOperationBindings
  OfflineOperationAdmissions --> OfflineSyncBatches
  OfflineOperationAdmissions --> Projects
  OfflineOperationAdmissions --> Users
  OfflineOperationBindings --> FieldInspectionAssignments
  OfflineOperationBindings --> FieldInspectionTasks
  OfflineOperationBindings --> OfflineDeviceRegistrations
  OfflineOperationBindings --> OfflineTaskSnapshots
  OfflineOperationBindings --> Projects
  OfflineOperationBindings --> RepairItems
  OfflineOperationBindings --> Users
  OfflineOperationResults --> OfflineOperationAdmissions
  OfflineOperationResults --> OfflineSyncBatches
  OfflineOperationResults --> Projects
  OfflineOriginTimeVerifications --> FieldInspectionOperationOrigins
  OfflineOriginTimeVerifications --> OfflineOperationBindings
  OfflineOriginTimeVerifications --> Projects
  OfflineOriginTimeVerifications --> Users
  OfflinePackageFileReferences --> Files
  OfflinePackageFileReferences --> OfflineEncryptedPackages
  OfflinePackageFileReferences --> Projects
  OfflineSyncBatches --> OfflineDeviceRegistrations
  OfflineSyncBatches --> OfflineEncryptedPackages
  OfflineSyncBatches --> OfflineHandoverGrants
  OfflineSyncBatches --> Projects
  OfflineSyncBatches --> Users
  OfflineTaskSnapshots --> FieldInspectionAssignments
  OfflineTaskSnapshots --> FieldInspectionTasks
  OfflineTaskSnapshots --> OfflineDeviceRegistrations
  OfflineTaskSnapshots --> Projects
  OfflineTaskSnapshots --> Users
```

Primary tables: `dbo.OfflineAdmittedFileReferences`, `dbo.OfflineDeviceRegistrations`, `dbo.OfflineDeviceRevocations`, `dbo.OfflineEncryptedCaptureArtifacts`, `dbo.OfflineEncryptedPackages`, `dbo.OfflineEvidenceCaptureReferences`, `dbo.OfflineHandoverGrantRevocations`, `dbo.OfflineHandoverGrants`, `dbo.OfflineOperationAdmissions`, `dbo.OfflineOperationBindings`, `dbo.OfflineOperationResults`, `dbo.OfflineOriginTimeVerifications`, `dbo.OfflinePackageFileReferences`, `dbo.OfflineSyncBatches`, `dbo.OfflineTaskSnapshots`

## Processing

```mermaid
flowchart LR
  AIDetections["AIDetections"]
  AIModelVersions["AIModelVersions"]
  Anh02AiDetectionProvenance["Anh02AiDetectionProvenance"]
  Anh02AiManifestFiles["Anh02AiManifestFiles"]
  Anh02AiMockRuns["Anh02AiMockRuns"]
  Anh02AiResultProvenance["Anh02AiResultProvenance"]
  DefectTypes["DefectTypes"]
  DerivedMeasurements["DerivedMeasurements"]
  Files["Files"]
  GroundTruthMeasurements["GroundTruthMeasurements"]
  MeasurementValidationSamples["MeasurementValidationSamples"]
  ProcessingAttempts["ProcessingAttempts"]
  ProcessingBlocks["ProcessingBlocks"]
  ProcessingJobs["ProcessingJobs"]
  Projects["Projects"]
  RoadSectionVersions["RoadSectionVersions"]
  RoadSegmentSets["RoadSegmentSets"]
  RoadSegments["RoadSegments"]
  SurveyDataVersions["SurveyDataVersions"]
  Users["Users"]
  ValidationRuns["ValidationRuns"]
  AIDetections --> AIModelVersions
  AIDetections --> DefectTypes
  AIDetections --> ProcessingJobs
  AIDetections --> RoadSectionVersions
  AIModelVersions --> Users
  Anh02AiDetectionProvenance --> AIDetections
  Anh02AiDetectionProvenance --> Anh02AiManifestFiles
  Anh02AiDetectionProvenance --> Anh02AiMockRuns
  Anh02AiDetectionProvenance --> Anh02AiResultProvenance
  Anh02AiDetectionProvenance --> Files
  Anh02AiDetectionProvenance --> RoadSegments
  Anh02AiManifestFiles --> Anh02AiMockRuns
  Anh02AiManifestFiles --> Files
  Anh02AiMockRuns --> AIModelVersions
  Anh02AiMockRuns --> Anh02AiMockRuns
  Anh02AiMockRuns --> Anh02AiResultProvenance
  Anh02AiMockRuns --> ProcessingAttempts
  Anh02AiMockRuns --> ProcessingJobs
  Anh02AiMockRuns --> Projects
  Anh02AiMockRuns --> RoadSegmentSets
  Anh02AiMockRuns --> SurveyDataVersions
  Anh02AiMockRuns --> Users
  Anh02AiResultProvenance --> Anh02AiMockRuns
  Anh02AiResultProvenance --> ProcessingAttempts
  Anh02AiResultProvenance --> ProcessingJobs
  DerivedMeasurements --> RoadSectionVersions
  DerivedMeasurements --> SurveyDataVersions
  MeasurementValidationSamples --> DerivedMeasurements
  MeasurementValidationSamples --> GroundTruthMeasurements
  MeasurementValidationSamples --> ValidationRuns
  ProcessingAttempts --> ProcessingJobs
  ProcessingBlocks --> SurveyDataVersions
  ProcessingJobs --> AIModelVersions
  ProcessingJobs --> ProcessingBlocks
  ValidationRuns --> AIModelVersions
```

Primary tables: `dbo.AIDetections`, `dbo.AIModelVersions`, `dbo.Anh02AiDetectionProvenance`, `dbo.Anh02AiManifestFiles`, `dbo.Anh02AiMockRuns`, `dbo.Anh02AiResultProvenance`, `dbo.DerivedMeasurements`, `dbo.MeasurementValidationSamples`, `dbo.ProcessingAttempts`, `dbo.ProcessingBlocks`, `dbo.ProcessingJobs`, `dbo.ValidationRuns`

## Projects

```mermaid
flowchart LR
  CrsProfileRevisions["CrsProfileRevisions"]
  Defects["Defects"]
  Files["Files"]
  GeometryLocationImpactDecisions["GeometryLocationImpactDecisions"]
  GeometryLocationImpacts["GeometryLocationImpacts"]
  GeometryMapPublications["GeometryMapPublications"]
  HandoverDocuments["HandoverDocuments"]
  LD06ActionEvidence["LD06ActionEvidence"]
  LD06LifecycleActions["LD06LifecycleActions"]
  NativeRouteVersionFacts["NativeRouteVersionFacts"]
  ObligationResponsibilities["ObligationResponsibilities"]
  PavementLayoutRevisions["PavementLayoutRevisions"]
  PavementSourceFileReferences["PavementSourceFileReferences"]
  ProjectLifecycleHistory["ProjectLifecycleHistory"]
  ProjectMembers["ProjectMembers"]
  Projects["Projects"]
  RepairDecisions["RepairDecisions"]
  RepairObligations["RepairObligations"]
  RoadGeometryDrafts["RoadGeometryDrafts"]
  RoadGeometryMetadata["RoadGeometryMetadata"]
  RoadRouteSystems["RoadRouteSystems"]
  RoadSectionVersions["RoadSectionVersions"]
  RoadSections["RoadSections"]
  RoadSegmentSets["RoadSegmentSets"]
  RoadSegments["RoadSegments"]
  Roles["Roles"]
  Users["Users"]
  CrsProfileRevisions --> Projects
  CrsProfileRevisions --> Users
  GeometryLocationImpactDecisions --> GeometryLocationImpacts
  GeometryLocationImpactDecisions --> Users
  GeometryLocationImpacts --> Projects
  GeometryLocationImpacts --> RoadSectionVersions
  GeometryLocationImpacts --> Users
  GeometryMapPublications --> CrsProfileRevisions
  GeometryMapPublications --> PavementLayoutRevisions
  GeometryMapPublications --> Projects
  GeometryMapPublications --> RoadSectionVersions
  GeometryMapPublications --> RoadSegmentSets
  GeometryMapPublications --> Users
  HandoverDocuments --> Files
  HandoverDocuments --> Projects
  HandoverDocuments --> Users
  LD06ActionEvidence --> Files
  LD06ActionEvidence --> LD06LifecycleActions
  LD06LifecycleActions --> Defects
  LD06LifecycleActions --> LD06LifecycleActions
  LD06LifecycleActions --> Projects
  LD06LifecycleActions --> RepairDecisions
  LD06LifecycleActions --> RepairObligations
  LD06LifecycleActions --> Users
  NativeRouteVersionFacts --> CrsProfileRevisions
  NativeRouteVersionFacts --> RoadRouteSystems
  NativeRouteVersionFacts --> RoadSectionVersions
  ObligationResponsibilities --> LD06LifecycleActions
  ObligationResponsibilities --> Projects
  ObligationResponsibilities --> RepairObligations
  PavementLayoutRevisions --> CrsProfileRevisions
  PavementLayoutRevisions --> PavementLayoutRevisions
  PavementLayoutRevisions --> Projects
  PavementLayoutRevisions --> RoadSectionVersions
  PavementLayoutRevisions --> RoadSegmentSets
  PavementLayoutRevisions --> Users
  PavementSourceFileReferences --> Files
  PavementSourceFileReferences --> PavementLayoutRevisions
  ProjectLifecycleHistory --> Defects
  ProjectLifecycleHistory --> ProjectLifecycleHistory
  ProjectLifecycleHistory --> Projects
  ProjectLifecycleHistory --> RepairObligations
  ProjectLifecycleHistory --> Users
  ProjectMembers --> Projects
  ProjectMembers --> Roles
  ProjectMembers --> Users
  RoadGeometryDrafts --> Projects
  RoadGeometryDrafts --> RoadSections
  RoadGeometryMetadata --> RoadGeometryDrafts
  RoadGeometryMetadata --> RoadSectionVersions
  RoadRouteSystems --> Projects
  RoadSectionVersions --> CrsProfileRevisions
  RoadSectionVersions --> RoadSections
  RoadSections --> Projects
  RoadSegmentSets --> RoadSectionVersions
  RoadSegments --> RoadSectionVersions
  RoadSegments --> RoadSegmentSets
```

Primary tables: `dbo.CrsProfileRevisions`, `dbo.GeometryLocationImpactDecisions`, `dbo.GeometryLocationImpacts`, `dbo.GeometryMapPublications`, `dbo.HandoverDocuments`, `dbo.LD06ActionEvidence`, `dbo.LD06LifecycleActions`, `dbo.NativeRouteVersionFacts`, `dbo.ObligationResponsibilities`, `dbo.PavementLayoutRevisions`, `dbo.PavementSourceFileReferences`, `dbo.ProjectLifecycleHistory`, `dbo.ProjectMembers`, `dbo.Projects`, `dbo.RoadGeometryDrafts`, `dbo.RoadGeometryMetadata`, `dbo.RoadRouteSystems`, `dbo.RoadSectionVersions`, `dbo.RoadSections`, `dbo.RoadSegmentSets`, `dbo.RoadSegments`

## Repairs

```mermaid
flowchart LR
  CrsProfileRevisions["CrsProfileRevisions"]
  DeadlineClocks["DeadlineClocks"]
  Defects["Defects"]
  FieldInspectionAssignments["FieldInspectionAssignments"]
  FieldInspectionEvidenceReuseDecisions["FieldInspectionEvidenceReuseDecisions"]
  FieldInspectionOperationOrigins["FieldInspectionOperationOrigins"]
  FieldInspectionSessions["FieldInspectionSessions"]
  FieldInspectionSubmissions["FieldInspectionSubmissions"]
  FieldInspectionTaskEvents["FieldInspectionTaskEvents"]
  FieldInspectionTasks["FieldInspectionTasks"]
  FieldTaskStartOrigins["FieldTaskStartOrigins"]
  Files["Files"]
  GeometryMapPublications["GeometryMapPublications"]
  GroundTruthMeasurements["GroundTruthMeasurements"]
  HandoverDocuments["HandoverDocuments"]
  PavementLayoutRevisions["PavementLayoutRevisions"]
  Projects["Projects"]
  RepairActualScopes["RepairActualScopes"]
  RepairAssessmentEvidence["RepairAssessmentEvidence"]
  RepairAssessmentMeasurements["RepairAssessmentMeasurements"]
  RepairAttemptEvidence["RepairAttemptEvidence"]
  RepairAttemptReviews["RepairAttemptReviews"]
  RepairAttemptSubmissionLinks["RepairAttemptSubmissionLinks"]
  RepairAttempts["RepairAttempts"]
  RepairCorrectionEvidence["RepairCorrectionEvidence"]
  RepairDangerAcknowledgements["RepairDangerAcknowledgements"]
  RepairDangerWarnings["RepairDangerWarnings"]
  RepairDecisions["RepairDecisions"]
  RepairEligibilityAssessments["RepairEligibilityAssessments"]
  RepairEligibilityHandoverSources["RepairEligibilityHandoverSources"]
  RepairEligibilityWarrantySources["RepairEligibilityWarrantySources"]
  RepairExecutionAuthorizations["RepairExecutionAuthorizations"]
  RepairExecutionFinishes["RepairExecutionFinishes"]
  RepairExecutionStarts["RepairExecutionStarts"]
  RepairFieldTaskBindings["RepairFieldTaskBindings"]
  RepairItemLifecycleEvents["RepairItemLifecycleEvents"]
  RepairItems["RepairItems"]
  RepairMeasurementAssessments["RepairMeasurementAssessments"]
  RepairNormalSuccessors["RepairNormalSuccessors"]
  RepairObligationResolutionEvents["RepairObligationResolutionEvents"]
  RepairObligations["RepairObligations"]
  RepairPackages["RepairPackages"]
  RepairPolicyDraftChanges["RepairPolicyDraftChanges"]
  RepairPolicyDrafts["RepairPolicyDrafts"]
  RepairPolicyMeasurementRules["RepairPolicyMeasurementRules"]
  RepairPolicyRevisions["RepairPolicyRevisions"]
  RepairPolicyRevocations["RepairPolicyRevocations"]
  RepairReviewRequests["RepairReviewRequests"]
  RepairSafetyActionSources["RepairSafetyActionSources"]
  RepairSafetyCheckEvidence["RepairSafetyCheckEvidence"]
  RepairSafetyChecks["RepairSafetyChecks"]
  RepairSafetyMonitoring["RepairSafetyMonitoring"]
  RepairSafetyResponsibilityTransfers["RepairSafetyResponsibilityTransfers"]
  RepairTemporarySafetyMeasures["RepairTemporarySafetyMeasures"]
  RepairWorkHandovers["RepairWorkHandovers"]
  RoadCoverageMappings["RoadCoverageMappings"]
  RoadSectionVersions["RoadSectionVersions"]
  RoadSections["RoadSections"]
  RoadSegmentSets["RoadSegmentSets"]
  Users["Users"]
  Warranties["Warranties"]
  RepairActualScopes --> RepairObligations
  RepairActualScopes --> RoadSections
  RepairAssessmentEvidence --> FieldInspectionEvidenceReuseDecisions
  RepairAssessmentEvidence --> Files
  RepairAssessmentEvidence --> RepairMeasurementAssessments
  RepairAssessmentEvidence --> Users
  RepairAssessmentMeasurements --> GroundTruthMeasurements
  RepairAssessmentMeasurements --> RepairMeasurementAssessments
  RepairAttemptEvidence --> Files
  RepairAttemptEvidence --> RepairAttempts
  RepairAttemptReviews --> FieldInspectionSubmissions
  RepairAttemptReviews --> Projects
  RepairAttemptReviews --> RepairAttemptSubmissionLinks
  RepairAttemptReviews --> RepairAttempts
  RepairAttemptReviews --> RepairFieldTaskBindings
  RepairAttemptReviews --> RepairItems
  RepairAttemptReviews --> Users
  RepairAttemptSubmissionLinks --> DeadlineClocks
  RepairAttemptSubmissionLinks --> FieldInspectionSubmissions
  RepairAttemptSubmissionLinks --> Projects
  RepairAttemptSubmissionLinks --> RepairAttemptSubmissionLinks
  RepairAttemptSubmissionLinks --> RepairAttempts
  RepairAttemptSubmissionLinks --> RepairExecutionFinishes
  RepairAttemptSubmissionLinks --> RepairFieldTaskBindings
  RepairAttemptSubmissionLinks --> RepairItems
  RepairAttempts --> Defects
  RepairAttempts --> FieldInspectionAssignments
  RepairAttempts --> FieldInspectionTasks
  RepairAttempts --> Projects
  RepairAttempts --> RepairItems
  RepairAttempts --> RepairObligations
  RepairAttempts --> Users
  RepairCorrectionEvidence --> Files
  RepairCorrectionEvidence --> RepairDecisions
  RepairDangerAcknowledgements --> RepairDangerWarnings
  RepairDangerAcknowledgements --> RepairSafetyMonitoring
  RepairDangerAcknowledgements --> Users
  RepairDangerWarnings --> RepairSafetyMonitoring
  RepairDangerWarnings --> Users
  RepairDecisions --> Defects
  RepairDecisions --> RepairDecisions
  RepairDecisions --> RepairItems
  RepairDecisions --> RepairObligations
  RepairDecisions --> Users
  RepairEligibilityAssessments --> Projects
  RepairEligibilityAssessments --> RepairFieldTaskBindings
  RepairEligibilityAssessments --> RepairItems
  RepairEligibilityAssessments --> RepairMeasurementAssessments
  RepairEligibilityAssessments --> RepairPolicyRevisions
  RepairEligibilityAssessments --> RoadSections
  RepairEligibilityHandoverSources --> Files
  RepairEligibilityHandoverSources --> HandoverDocuments
  RepairEligibilityHandoverSources --> RepairEligibilityAssessments
  RepairEligibilityWarrantySources --> Files
  RepairEligibilityWarrantySources --> RepairEligibilityAssessments
  RepairEligibilityWarrantySources --> Warranties
  RepairExecutionAuthorizations --> Defects
  RepairExecutionAuthorizations --> FieldInspectionAssignments
  RepairExecutionAuthorizations --> FieldInspectionTasks
  RepairExecutionAuthorizations --> Projects
  RepairExecutionAuthorizations --> RepairPolicyRevisions
  RepairExecutionAuthorizations --> Users
  RepairExecutionFinishes --> FieldInspectionOperationOrigins
  RepairExecutionFinishes --> Projects
  RepairExecutionFinishes --> RepairExecutionStarts
  RepairExecutionFinishes --> RepairFieldTaskBindings
  RepairExecutionFinishes --> RepairItems
  RepairExecutionFinishes --> Users
  RepairExecutionStarts --> FieldInspectionOperationOrigins
  RepairExecutionStarts --> FieldTaskStartOrigins
  RepairExecutionStarts --> Projects
  RepairExecutionStarts --> RepairEligibilityAssessments
  RepairExecutionStarts --> RepairFieldTaskBindings
  RepairExecutionStarts --> RepairItems
  RepairExecutionStarts --> RepairMeasurementAssessments
  RepairExecutionStarts --> Users
  RepairFieldTaskBindings --> CrsProfileRevisions
  RepairFieldTaskBindings --> Defects
  RepairFieldTaskBindings --> FieldInspectionAssignments
  RepairFieldTaskBindings --> FieldInspectionTasks
  RepairFieldTaskBindings --> GeometryMapPublications
  RepairFieldTaskBindings --> PavementLayoutRevisions
  RepairFieldTaskBindings --> Projects
  RepairFieldTaskBindings --> RepairExecutionAuthorizations
  RepairFieldTaskBindings --> RepairItems
  RepairFieldTaskBindings --> RepairObligations
  RepairFieldTaskBindings --> RepairPolicyRevisions
  RepairFieldTaskBindings --> RoadSectionVersions
  RepairFieldTaskBindings --> RoadSegmentSets
  RepairFieldTaskBindings --> Users
  RepairItemLifecycleEvents --> Defects
  RepairItemLifecycleEvents --> FieldInspectionSubmissions
  RepairItemLifecycleEvents --> Projects
  RepairItemLifecycleEvents --> RepairAttemptReviews
  RepairItemLifecycleEvents --> RepairAttempts
  RepairItemLifecycleEvents --> RepairDecisions
  RepairItemLifecycleEvents --> RepairFieldTaskBindings
  RepairItemLifecycleEvents --> RepairItems
  RepairItemLifecycleEvents --> RepairObligations
  RepairItemLifecycleEvents --> Users
  RepairItems --> Defects
  RepairItems --> FieldInspectionSubmissions
  RepairItems --> Projects
  RepairItems --> RepairAttemptReviews
  RepairItems --> RepairAttemptSubmissionLinks
  RepairItems --> RepairAttempts
  RepairItems --> RepairDecisions
  RepairItems --> RepairExecutionFinishes
  RepairItems --> RepairExecutionStarts
  RepairItems --> RepairFieldTaskBindings
  RepairItems --> RepairItems
  RepairItems --> RepairMeasurementAssessments
  RepairItems --> RepairObligations
  RepairItems --> RepairPackages
  RepairItems --> Users
  RepairMeasurementAssessments --> FieldInspectionAssignments
  RepairMeasurementAssessments --> FieldInspectionOperationOrigins
  RepairMeasurementAssessments --> FieldInspectionSessions
  RepairMeasurementAssessments --> FieldInspectionSubmissions
  RepairMeasurementAssessments --> FieldInspectionTasks
  RepairMeasurementAssessments --> FieldTaskStartOrigins
  RepairMeasurementAssessments --> Projects
  RepairMeasurementAssessments --> RepairFieldTaskBindings
  RepairMeasurementAssessments --> RepairItems
  RepairMeasurementAssessments --> Users
  RepairNormalSuccessors --> FieldInspectionTaskEvents
  RepairNormalSuccessors --> Projects
  RepairNormalSuccessors --> RepairDecisions
  RepairNormalSuccessors --> RepairItemLifecycleEvents
  RepairNormalSuccessors --> RepairItems
  RepairNormalSuccessors --> RepairMeasurementAssessments
  RepairNormalSuccessors --> RepairObligations
  RepairNormalSuccessors --> Users
  RepairObligationResolutionEvents --> RepairDecisions
  RepairObligationResolutionEvents --> RepairObligations
  RepairObligations --> Defects
  RepairObligations --> FieldTaskStartOrigins
  RepairObligations --> Projects
  RepairObligations --> RepairDecisions
  RepairObligations --> RepairItems
  RepairObligations --> RepairPackages
  RepairPackages --> Defects
  RepairPackages --> Projects
  RepairPolicyDraftChanges --> RepairPolicyDrafts
  RepairPolicyDraftChanges --> Users
  RepairPolicyDrafts --> Projects
  RepairPolicyDrafts --> RepairPolicyDraftChanges
  RepairPolicyDrafts --> RepairPolicyRevisions
  RepairPolicyMeasurementRules --> RepairPolicyRevisions
  RepairPolicyRevisions --> Projects
  RepairPolicyRevisions --> Users
  RepairPolicyRevocations --> RepairPolicyRevisions
  RepairPolicyRevocations --> Users
  RepairReviewRequests --> Projects
  RepairReviewRequests --> RepairDecisions
  RepairReviewRequests --> RepairItems
  RepairReviewRequests --> Users
  RepairSafetyActionSources --> Projects
  RepairSafetyActionSources --> RepairFieldTaskBindings
  RepairSafetyActionSources --> RepairItems
  RepairSafetyActionSources --> RepairTemporarySafetyMeasures
  RepairSafetyActionSources --> Users
  RepairSafetyCheckEvidence --> Files
  RepairSafetyCheckEvidence --> RepairSafetyChecks
  RepairSafetyCheckEvidence --> Users
  RepairSafetyChecks --> RepairSafetyMonitoring
  RepairSafetyChecks --> RepairTemporarySafetyMeasures
  RepairSafetyChecks --> Users
  RepairSafetyMonitoring --> RepairObligations
  RepairSafetyMonitoring --> RepairSafetyChecks
  RepairSafetyMonitoring --> RepairTemporarySafetyMeasures
  RepairSafetyResponsibilityTransfers --> RepairTemporarySafetyMeasures
  RepairSafetyResponsibilityTransfers --> Users
  RepairTemporarySafetyMeasures --> Defects
  RepairTemporarySafetyMeasures --> Projects
  RepairTemporarySafetyMeasures --> RepairObligations
  RepairTemporarySafetyMeasures --> RepairSafetyResponsibilityTransfers
  RepairTemporarySafetyMeasures --> Users
  RepairWorkHandovers --> RepairItems
  RepairWorkHandovers --> Users
  RoadCoverageMappings --> Files
  RoadCoverageMappings --> HandoverDocuments
  RoadCoverageMappings --> Projects
  RoadCoverageMappings --> RepairObligations
  RoadCoverageMappings --> RoadCoverageMappings
  RoadCoverageMappings --> RoadSections
  RoadCoverageMappings --> Users
```

Primary tables: `dbo.RepairActualScopes`, `dbo.RepairAssessmentEvidence`, `dbo.RepairAssessmentMeasurements`, `dbo.RepairAttemptEvidence`, `dbo.RepairAttemptReviews`, `dbo.RepairAttemptSubmissionLinks`, `dbo.RepairAttempts`, `dbo.RepairCorrectionEvidence`, `dbo.RepairDangerAcknowledgements`, `dbo.RepairDangerWarnings`, `dbo.RepairDecisions`, `dbo.RepairEligibilityAssessments`, `dbo.RepairEligibilityHandoverSources`, `dbo.RepairEligibilityWarrantySources`, `dbo.RepairExecutionAuthorizations`, `dbo.RepairExecutionFinishes`, `dbo.RepairExecutionStarts`, `dbo.RepairFieldTaskBindings`, `dbo.RepairItemLifecycleEvents`, `dbo.RepairItems`, `dbo.RepairMeasurementAssessments`, `dbo.RepairNormalSuccessors`, `dbo.RepairObligationResolutionEvents`, `dbo.RepairObligations`, `dbo.RepairPackages`, `dbo.RepairPolicyDraftChanges`, `dbo.RepairPolicyDrafts`, `dbo.RepairPolicyMeasurementRules`, `dbo.RepairPolicyRevisions`, `dbo.RepairPolicyRevocations`, `dbo.RepairReviewRequests`, `dbo.RepairSafetyActionSources`, `dbo.RepairSafetyCheckEvidence`, `dbo.RepairSafetyChecks`, `dbo.RepairSafetyMonitoring`, `dbo.RepairSafetyResponsibilityTransfers`, `dbo.RepairTemporarySafetyMeasures`, `dbo.RepairWorkHandovers`, `dbo.RoadCoverageMappings`

## Reporting

```mermaid
flowchart LR
  DefectStatisticsSources["DefectStatisticsSources"]
  Defects["Defects"]
  Projects["Projects"]
  RepairObligations["RepairObligations"]
  RoadSectionVersions["RoadSectionVersions"]
  RoadSections["RoadSections"]
  Users["Users"]
  DefectStatisticsSources --> DefectStatisticsSources
  DefectStatisticsSources --> Defects
  DefectStatisticsSources --> Projects
  DefectStatisticsSources --> RepairObligations
  DefectStatisticsSources --> RoadSectionVersions
  DefectStatisticsSources --> RoadSections
  DefectStatisticsSources --> Users
```

Primary tables: `dbo.DefectStatisticsSources`

## Reports

```mermaid
flowchart LR
  Files["Files"]
  ReportOriginalEvidence["ReportOriginalEvidence"]
  ReportSupplementEvidence["ReportSupplementEvidence"]
  ReportSupplements["ReportSupplements"]
  Reports["Reports"]
  Users["Users"]
  ReportOriginalEvidence --> Files
  ReportOriginalEvidence --> Reports
  ReportOriginalEvidence --> Users
  ReportSupplementEvidence --> Files
  ReportSupplementEvidence --> ReportSupplements
  ReportSupplementEvidence --> Reports
  ReportSupplementEvidence --> Users
  ReportSupplements --> Reports
  Reports --> Users
```

Primary tables: `dbo.ReportOriginalEvidence`, `dbo.ReportSupplementEvidence`, `dbo.ReportSupplements`, `dbo.Reports`

## Retention

```mermaid
flowchart LR
  Files["Files"]
  Projects["Projects"]
  RetentionBasisHeads["RetentionBasisHeads"]
  RetentionBasisRevisions["RetentionBasisRevisions"]
  RetentionEvaluationItems["RetentionEvaluationItems"]
  RetentionEvaluations["RetentionEvaluations"]
  RetentionHoldHistories["RetentionHoldHistories"]
  RetentionHolds["RetentionHolds"]
  Users["Users"]
  RetentionBasisHeads --> Files
  RetentionBasisHeads --> RetentionBasisRevisions
  RetentionBasisRevisions --> Files
  RetentionBasisRevisions --> RetentionBasisRevisions
  RetentionBasisRevisions --> Users
  RetentionEvaluationItems --> Files
  RetentionEvaluationItems --> RetentionEvaluations
  RetentionEvaluations --> Projects
  RetentionEvaluations --> Users
  RetentionHoldHistories --> RetentionHolds
  RetentionHoldHistories --> Users
  RetentionHolds --> Users
```

Primary tables: `dbo.RetentionBasisHeads`, `dbo.RetentionBasisRevisions`, `dbo.RetentionEvaluationItems`, `dbo.RetentionEvaluations`, `dbo.RetentionHoldHistories`, `dbo.RetentionHolds`

## Surveys

```mermaid
flowchart LR
  BaselineCurrentPointers["BaselineCurrentPointers"]
  BaselineSelectionItems["BaselineSelectionItems"]
  BaselineSelections["BaselineSelections"]
  DatasetAssessmentItems["DatasetAssessmentItems"]
  DatasetAssessments["DatasetAssessments"]
  DroneDevices["DroneDevices"]
  Files["Files"]
  Flights["Flights"]
  Projects["Projects"]
  QualityChecks["QualityChecks"]
  RoadSectionVersions["RoadSectionVersions"]
  RoadSections["RoadSections"]
  RoadSegments["RoadSegments"]
  SupplementarySurveyRequests["SupplementarySurveyRequests"]
  SurveyAssignments["SurveyAssignments"]
  SurveyDataVersions["SurveyDataVersions"]
  SurveyFiles["SurveyFiles"]
  SurveyPlanPostponements["SurveyPlanPostponements"]
  SurveyPlanScopes["SurveyPlanScopes"]
  SurveyPlans["SurveyPlans"]
  SurveyRequestScopes["SurveyRequestScopes"]
  SurveyRequests["SurveyRequests"]
  Surveys["Surveys"]
  Users["Users"]
  BaselineCurrentPointers --> BaselineSelectionItems
  BaselineCurrentPointers --> Projects
  BaselineSelectionItems --> BaselineSelections
  BaselineSelectionItems --> DatasetAssessments
  BaselineSelectionItems --> SurveyDataVersions
  BaselineSelections --> Projects
  BaselineSelections --> Users
  DatasetAssessmentItems --> DatasetAssessments
  DatasetAssessmentItems --> RoadSegments
  DatasetAssessments --> SurveyDataVersions
  DatasetAssessments --> Users
  Flights --> DroneDevices
  Flights --> Surveys
  Flights --> Users
  QualityChecks --> SurveyDataVersions
  QualityChecks --> SurveyFiles
  QualityChecks --> Users
  SupplementarySurveyRequests --> SurveyRequests
  SupplementarySurveyRequests --> Surveys
  SupplementarySurveyRequests --> Users
  SurveyAssignments --> SurveyRequests
  SurveyAssignments --> Users
  SurveyDataVersions --> Surveys
  SurveyFiles --> Files
  SurveyFiles --> Flights
  SurveyFiles --> Surveys
  SurveyPlanPostponements --> SurveyPlans
  SurveyPlanScopes --> RoadSectionVersions
  SurveyPlanScopes --> SurveyPlans
  SurveyPlans --> Projects
  SurveyPlans --> RoadSectionVersions
  SurveyPlans --> RoadSections
  SurveyRequestScopes --> RoadSectionVersions
  SurveyRequestScopes --> SurveyRequests
  SurveyRequests --> Projects
  SurveyRequests --> RoadSectionVersions
  SurveyRequests --> RoadSections
  SurveyRequests --> SupplementarySurveyRequests
  SurveyRequests --> SurveyPlans
  SurveyRequests --> SurveyRequests
  SurveyRequests --> Users
  Surveys --> Projects
  Surveys --> RoadSectionVersions
  Surveys --> SurveyRequests
  Surveys --> Users
```

Primary tables: `dbo.BaselineCurrentPointers`, `dbo.BaselineSelectionItems`, `dbo.BaselineSelections`, `dbo.DatasetAssessmentItems`, `dbo.DatasetAssessments`, `dbo.Flights`, `dbo.QualityChecks`, `dbo.SupplementarySurveyRequests`, `dbo.SurveyAssignments`, `dbo.SurveyDataVersions`, `dbo.SurveyFiles`, `dbo.SurveyPlanPostponements`, `dbo.SurveyPlanScopes`, `dbo.SurveyPlans`, `dbo.SurveyRequestScopes`, `dbo.SurveyRequests`, `dbo.Surveys`

## Warranties

```mermaid
flowchart LR
  Files["Files"]
  HandoverDocuments["HandoverDocuments"]
  Projects["Projects"]
  RoadSections["RoadSections"]
  Warranties["Warranties"]
  Warranties --> Files
  Warranties --> HandoverDocuments
  Warranties --> Projects
  Warranties --> RoadSections
```

Primary tables: `dbo.Warranties`
