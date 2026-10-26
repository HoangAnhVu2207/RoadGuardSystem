# Demo entity and table coverage — closeout

CURRENT_VERIFIED owned isolated RC1 after API-only repair scenario extension: 193 SQL tables including migration history; 192 application tables; 63 populated application tables. Counts are observations, not complete workflow acceptance.

Real production API creates A/B geometry, survey/video/dataset, six Mock runs/six detections, actual MinIO evidence, two Reporter/Case/candidate Defects, Normal AwaitingApproval and FastTrack ReadyToAssign proposals, exports and retention evaluations. Approval/execution/Verified/publication states are not fabricated. Normal proposal creates its genuine approval duty; FastTrack creates no Supervisor duty. PublicPublications remains TEST_ONLY empty. See FE-CLOSEOUT-WORKFLOW-ACCEPTANCE.md for separately executed lifecycle tests.

| SQL table | Rows | Boundary |
|---|---:|---|
| dbo.__EFMigrationsHistory | 3 | POPULATED bounded scenario/bookkeeping |
| dbo.AccountStatusChangeLogs | 0 | EMPTY; no fabricated state |
| dbo.AIDetections | 6 | POPULATED bounded scenario/bookkeeping |
| dbo.AIModelVersions | 1 | POPULATED bounded scenario/bookkeeping |
| dbo.Anh02AiDetectionProvenance | 6 | POPULATED bounded scenario/bookkeeping |
| dbo.Anh02AiManifestFiles | 6 | POPULATED bounded scenario/bookkeeping |
| dbo.Anh02AiMockRuns | 6 | POPULATED bounded scenario/bookkeeping |
| dbo.Anh02AiResultProvenance | 6 | POPULATED bounded scenario/bookkeeping |
| dbo.Anh02ExportJobs | 2 | POPULATED bounded scenario/bookkeeping |
| dbo.Anh02ExportSnapshotFiles | 0 | EMPTY; no fabricated state |
| dbo.Anh02ExportSnapshots | 2 | POPULATED bounded scenario/bookkeeping |
| dbo.Anh02GeneratedArtifacts | 2 | POPULATED bounded scenario/bookkeeping |
| dbo.AuditLogs | 54 | POPULATED bounded scenario/bookkeeping |
| dbo.BaselineCurrentPointers | 0 | EMPTY; no fabricated state |
| dbo.BaselineSelectionItems | 0 | EMPTY; no fabricated state |
| dbo.BaselineSelections | 0 | EMPTY; no fabricated state |
| dbo.BusinessDutyAppointments | 0 | EMPTY; no fabricated state |
| dbo.BusinessReceivingRequests | 0 | EMPTY; no fabricated state |
| dbo.CandidateSourceHeads | 2 | POPULATED bounded scenario/bookkeeping |
| dbo.CaseConclusionDefects | 0 | EMPTY; no fabricated state |
| dbo.CaseConclusionEvidence | 0 | EMPTY; no fabricated state |
| dbo.CaseConclusions | 0 | EMPTY; no fabricated state |
| dbo.CasePublicationDefects | 0 | EMPTY; no fabricated state |
| dbo.CasePublicationEvidence | 0 | EMPTY; no fabricated state |
| dbo.CasePublicationRecipients | 0 | EMPTY; no fabricated state |
| dbo.CasePublications | 0 | EMPTY; no fabricated state |
| dbo.CaseReportLinkHistory | 0 | EMPTY; no fabricated state |
| dbo.CaseReportLinkHistoryReports | 0 | EMPTY; no fabricated state |
| dbo.CaseReportLinks | 2 | POPULATED bounded scenario/bookkeeping |
| dbo.CauseCategories | 0 | EMPTY; no fabricated state |
| dbo.ConsumerEffectReceipts | 0 | EMPTY; no fabricated state |
| dbo.CrsProfileRevisions | 0 | EMPTY; no fabricated state |
| dbo.DatasetAssessmentItems | 0 | EMPTY; no fabricated state |
| dbo.DatasetAssessments | 0 | EMPTY; no fabricated state |
| dbo.DeadlineBreaches | 0 | EMPTY; no fabricated state |
| dbo.DeadlineClocks | 1 | POPULATED bounded scenario/bookkeeping |
| dbo.DeadlineDutyAppointments | 0 | EMPTY; no fabricated state |
| dbo.DeadlineExtensions | 0 | EMPTY; no fabricated state |
| dbo.Defects | 2 | POPULATED bounded scenario/bookkeeping |
| dbo.DefectSourceLinks | 2 | POPULATED bounded scenario/bookkeeping |
| dbo.DefectStatisticsSources | 0 | EMPTY; no fabricated state |
| dbo.DefectTypes | 2 | POPULATED bounded scenario/bookkeeping |
| dbo.DefectVerificationLogs | 0 | EMPTY; no fabricated state |
| dbo.DerivedMeasurements | 0 | EMPTY; no fabricated state |
| dbo.DroneDevices | 1 | POPULATED bounded scenario/bookkeeping |
| dbo.FieldInspectionAssignments | 0 | EMPTY; no fabricated state |
| dbo.FieldInspectionEvidenceLinks | 0 | EMPTY; no fabricated state |
| dbo.FieldInspectionEvidenceReuseDecisions | 0 | EMPTY; no fabricated state |
| dbo.FieldInspectionLocationProofs | 0 | EMPTY; no fabricated state |
| dbo.FieldInspectionOperationOrigins | 0 | EMPTY; no fabricated state |
| dbo.FieldInspectionReviews | 0 | EMPTY; no fabricated state |
| dbo.FieldInspectionSessions | 0 | EMPTY; no fabricated state |
| dbo.FieldInspectionSubmissions | 0 | EMPTY; no fabricated state |
| dbo.FieldInspectionTaskEvents | 0 | EMPTY; no fabricated state |
| dbo.FieldInspectionTasks | 0 | EMPTY; no fabricated state |
| dbo.FieldTaskStartOrigins | 0 | EMPTY; no fabricated state |
| dbo.Files | 11 | POPULATED bounded scenario/bookkeeping |
| dbo.FileScopes | 3 | POPULATED bounded scenario/bookkeeping |
| dbo.Flights | 0 | EMPTY; no fabricated state |
| dbo.GeometryLocationImpactDecisions | 0 | EMPTY; no fabricated state |
| dbo.GeometryLocationImpacts | 0 | EMPTY; no fabricated state |
| dbo.GeometryMapPublications | 0 | EMPTY; no fabricated state |
| dbo.GroundTruthMeasurements | 0 | EMPTY; no fabricated state |
| dbo.H6NotificationAudits | 0 | EMPTY; no fabricated state |
| dbo.H6NotificationCalendar | 1 | POPULATED bounded scenario/bookkeeping |
| dbo.H6NotificationDeliveries | 1 | POPULATED bounded scenario/bookkeeping |
| dbo.H6NotificationDeliveryAttempts | 1 | POPULATED bounded scenario/bookkeeping |
| dbo.H6NotificationEventReceipts | 1 | POPULATED bounded scenario/bookkeeping |
| dbo.H6NotificationOccurrences | 1 | POPULATED bounded scenario/bookkeeping |
| dbo.H6NotificationScopes | 0 | EMPTY; no fabricated state |
| dbo.HandoverDocuments | 0 | EMPTY; no fabricated state |
| dbo.IdempotencyRecords | 49 | POPULATED bounded scenario/bookkeeping |
| dbo.IncidentCases | 2 | POPULATED bounded scenario/bookkeeping |
| dbo.LD06ActionEvidence | 0 | EMPTY; no fabricated state |
| dbo.LD06LifecycleActions | 0 | EMPTY; no fabricated state |
| dbo.MeasurementValidationSamples | 0 | EMPTY; no fabricated state |
| dbo.NativeRouteVersionFacts | 0 | EMPTY; no fabricated state |
| dbo.Notifications | 0 | EMPTY; no fabricated state |
| dbo.ObligationResponsibilities | 0 | EMPTY; no fabricated state |
| dbo.OfflineAdmittedFileReferences | 0 | EMPTY; no fabricated state |
| dbo.OfflineDeviceRegistrations | 0 | EMPTY; no fabricated state |
| dbo.OfflineDeviceRevocations | 0 | EMPTY; no fabricated state |
| dbo.OfflineEncryptedCaptureArtifacts | 0 | EMPTY; no fabricated state |
| dbo.OfflineEncryptedPackages | 0 | EMPTY; no fabricated state |
| dbo.OfflineEvidenceCaptureReferences | 0 | EMPTY; no fabricated state |
| dbo.OfflineHandoverGrantRevocations | 0 | EMPTY; no fabricated state |
| dbo.OfflineHandoverGrants | 0 | EMPTY; no fabricated state |
| dbo.OfflineOperationAdmissions | 0 | EMPTY; no fabricated state |
| dbo.OfflineOperationBindings | 0 | EMPTY; no fabricated state |
| dbo.OfflineOperationResults | 0 | EMPTY; no fabricated state |
| dbo.OfflineOriginTimeVerifications | 0 | EMPTY; no fabricated state |
| dbo.OfflinePackageFileReferences | 0 | EMPTY; no fabricated state |
| dbo.OfflineSyncBatches | 0 | EMPTY; no fabricated state |
| dbo.OfflineTaskSnapshots | 0 | EMPTY; no fabricated state |
| dbo.OutboxMessages | 1 | POPULATED bounded scenario/bookkeeping |
| dbo.PasswordRecoveryRequests | 0 | EMPTY; no fabricated state |
| dbo.PasswordResetLogs | 0 | EMPTY; no fabricated state |
| dbo.PavementLayoutRevisions | 0 | EMPTY; no fabricated state |
| dbo.PavementSourceFileReferences | 0 | EMPTY; no fabricated state |
| dbo.ProcessingAttempts | 6 | POPULATED bounded scenario/bookkeeping |
| dbo.ProcessingBlocks | 2 | POPULATED bounded scenario/bookkeeping |
| dbo.ProcessingJobs | 6 | POPULATED bounded scenario/bookkeeping |
| dbo.ProjectLifecycleHistory | 0 | EMPTY; no fabricated state |
| dbo.ProjectMembers | 8 | POPULATED bounded scenario/bookkeeping |
| dbo.Projects | 2 | POPULATED bounded scenario/bookkeeping |
| dbo.PublicPublications | 0 | EMPTY; no fabricated state |
| dbo.QualityChecks | 0 | EMPTY; no fabricated state |
| dbo.RefreshTokens | 45 | POPULATED bounded scenario/bookkeeping |
| dbo.RepairActualScopes | 2 | POPULATED bounded scenario/bookkeeping |
| dbo.RepairAssessmentEvidence | 0 | EMPTY; no fabricated state |
| dbo.RepairAssessmentMeasurements | 0 | EMPTY; no fabricated state |
| dbo.RepairAttemptEvidence | 0 | EMPTY; no fabricated state |
| dbo.RepairAttemptReviews | 0 | EMPTY; no fabricated state |
| dbo.RepairAttempts | 0 | EMPTY; no fabricated state |
| dbo.RepairAttemptSubmissionLinks | 0 | EMPTY; no fabricated state |
| dbo.RepairCorrectionEvidence | 0 | EMPTY; no fabricated state |
| dbo.RepairDangerAcknowledgements | 0 | EMPTY; no fabricated state |
| dbo.RepairDangerWarnings | 0 | EMPTY; no fabricated state |
| dbo.RepairDecisions | 0 | EMPTY; no fabricated state |
| dbo.RepairEligibilityAssessments | 0 | EMPTY; no fabricated state |
| dbo.RepairEligibilityHandoverSources | 0 | EMPTY; no fabricated state |
| dbo.RepairEligibilityWarrantySources | 0 | EMPTY; no fabricated state |
| dbo.RepairExecutionAuthorizations | 0 | EMPTY; no fabricated state |
| dbo.RepairExecutionFinishes | 0 | EMPTY; no fabricated state |
| dbo.RepairExecutionStarts | 0 | EMPTY; no fabricated state |
| dbo.RepairFieldTaskBindings | 0 | EMPTY; no fabricated state |
| dbo.RepairItemLifecycleEvents | 2 | POPULATED bounded scenario/bookkeeping |
| dbo.RepairItems | 2 | POPULATED bounded scenario/bookkeeping |
| dbo.RepairMeasurementAssessments | 0 | EMPTY; no fabricated state |
| dbo.RepairNormalSuccessors | 0 | EMPTY; no fabricated state |
| dbo.RepairObligationResolutionEvents | 0 | EMPTY; no fabricated state |
| dbo.RepairObligations | 2 | POPULATED bounded scenario/bookkeeping |
| dbo.RepairPackages | 2 | POPULATED bounded scenario/bookkeeping |
| dbo.RepairPolicyDraftChanges | 0 | EMPTY; no fabricated state |
| dbo.RepairPolicyDrafts | 0 | EMPTY; no fabricated state |
| dbo.RepairPolicyMeasurementRules | 0 | EMPTY; no fabricated state |
| dbo.RepairPolicyRevisions | 0 | EMPTY; no fabricated state |
| dbo.RepairPolicyRevocations | 0 | EMPTY; no fabricated state |
| dbo.RepairReviewRequests | 0 | EMPTY; no fabricated state |
| dbo.RepairSafetyActionSources | 0 | EMPTY; no fabricated state |
| dbo.RepairSafetyCheckEvidence | 0 | EMPTY; no fabricated state |
| dbo.RepairSafetyChecks | 0 | EMPTY; no fabricated state |
| dbo.RepairSafetyMonitoring | 0 | EMPTY; no fabricated state |
| dbo.RepairSafetyResponsibilityTransfers | 0 | EMPTY; no fabricated state |
| dbo.RepairTemporarySafetyMeasures | 0 | EMPTY; no fabricated state |
| dbo.RepairWorkHandovers | 0 | EMPTY; no fabricated state |
| dbo.ReporterRegistrationIntents | 0 | EMPTY; no fabricated state |
| dbo.ReportOriginalEvidence | 2 | POPULATED bounded scenario/bookkeeping |
| dbo.Reports | 2 | POPULATED bounded scenario/bookkeeping |
| dbo.ReportSupplementEvidence | 0 | EMPTY; no fabricated state |
| dbo.ReportSupplements | 0 | EMPTY; no fabricated state |
| dbo.RetentionBasisHeads | 0 | EMPTY; no fabricated state |
| dbo.RetentionBasisRevisions | 0 | EMPTY; no fabricated state |
| dbo.RetentionEvaluationItems | 2 | POPULATED bounded scenario/bookkeeping |
| dbo.RetentionEvaluations | 2 | POPULATED bounded scenario/bookkeeping |
| dbo.RetentionHoldHistories | 0 | EMPTY; no fabricated state |
| dbo.RetentionHolds | 0 | EMPTY; no fabricated state |
| dbo.RoadCoverageMappings | 0 | EMPTY; no fabricated state |
| dbo.RoadGeometryDrafts | 2 | POPULATED bounded scenario/bookkeeping |
| dbo.RoadGeometryMetadata | 2 | POPULATED bounded scenario/bookkeeping |
| dbo.RoadRouteSystems | 0 | EMPTY; no fabricated state |
| dbo.RoadSections | 4 | POPULATED bounded scenario/bookkeeping |
| dbo.RoadSectionVersions | 4 | POPULATED bounded scenario/bookkeeping |
| dbo.RoadSegments | 4 | POPULATED bounded scenario/bookkeeping |
| dbo.RoadSegmentSets | 2 | POPULATED bounded scenario/bookkeeping |
| dbo.Roles | 5 | POPULATED bounded scenario/bookkeeping |
| dbo.Sessions | 45 | POPULATED bounded scenario/bookkeeping |
| dbo.SeverityRuleVersions | 0 | EMPTY; no fabricated state |
| dbo.SourceDecisions | 2 | POPULATED bounded scenario/bookkeeping |
| dbo.StaffInvitationProjects | 0 | EMPTY; no fabricated state |
| dbo.StaffInvitations | 0 | EMPTY; no fabricated state |
| dbo.SupplementarySurveyRequests | 0 | EMPTY; no fabricated state |
| dbo.SurveyAssignments | 2 | POPULATED bounded scenario/bookkeeping |
| dbo.SurveyDataVersions | 2 | POPULATED bounded scenario/bookkeeping |
| dbo.SurveyFiles | 2 | POPULATED bounded scenario/bookkeeping |
| dbo.SurveyPlanPostponements | 0 | EMPTY; no fabricated state |
| dbo.SurveyPlans | 2 | POPULATED bounded scenario/bookkeeping |
| dbo.SurveyPlanScopes | 2 | POPULATED bounded scenario/bookkeeping |
| dbo.SurveyRequests | 2 | POPULATED bounded scenario/bookkeeping |
| dbo.SurveyRequestScopes | 2 | POPULATED bounded scenario/bookkeeping |
| dbo.Surveys | 2 | POPULATED bounded scenario/bookkeeping |
| dbo.TrainingLabelReviews | 0 | EMPTY; no fabricated state |
| dbo.TrainingLabelRevisions | 0 | EMPTY; no fabricated state |
| dbo.TrainingLabels | 0 | EMPTY; no fabricated state |
| dbo.UploadMultipartSweeps | 3 | POPULATED bounded scenario/bookkeeping |
| dbo.UploadParts | 3 | POPULATED bounded scenario/bookkeeping |
| dbo.UploadSessions | 3 | POPULATED bounded scenario/bookkeeping |
| dbo.Users | 11 | POPULATED bounded scenario/bookkeeping |
| dbo.ValidationRuns | 0 | EMPTY; no fabricated state |
| dbo.Warranties | 0 | EMPTY; no fabricated state |
| dbo.WeeklyReviewDigestDuties | 0 | EMPTY; no fabricated state |
| dbo.WeeklyReviewDigests | 0 | EMPTY; no fabricated state |
| dbo.WeeklyReviewRecoveryPeriods | 0 | EMPTY; no fabricated state |
