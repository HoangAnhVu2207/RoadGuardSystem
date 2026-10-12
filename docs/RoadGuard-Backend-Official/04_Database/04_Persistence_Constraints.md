# Persistence constraints and trigger catalog

**CURRENT_VERIFIED — source baseline `cdeb1ca1037744d06995058af13d0582f6273c0a`, `huy-review`, 2026-10-13.** Source observation is not all-path runtime execution. Expected business behavior uses owner-confirmed decisions and assigned specs; implementation/test evidence has its own limits.

Current EF key/index/check metadata is fully listed. SQL-only constraints/triggers below come from current migration source and historical fixture inventory, revalidated by unchanged input hashes; not live SQL execution. Business authorization/state/evidence rules must also run in services/repositories; a FK does not grant permission.

| Table | Kind | Name | Definition | Entity |
|---|---|---|---|---|
| dbo.AccountStatusChangeLogs | KEY | PK_AccountStatusChangeLogs | {"name": "PK_AccountStatusChangeLogs", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Identity.AccountStatusChangeLog |
| dbo.AccountStatusChangeLogs | INDEX | IX_AccountStatusChangeLogs_ChangedByUserId | {"name": "IX_AccountStatusChangeLogs_ChangedByUserId", "columns": ["ChangedByUserId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Identity.AccountStatusChangeLog |
| dbo.AccountStatusChangeLogs | INDEX | IX_AccountStatusChangeLogs_OccurredAt | {"name": "IX_AccountStatusChangeLogs_OccurredAt", "columns": ["OccurredAt"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Identity.AccountStatusChangeLog |
| dbo.AccountStatusChangeLogs | INDEX | IX_AccountStatusChangeLogs_TargetUserId | {"name": "IX_AccountStatusChangeLogs_TargetUserId", "columns": ["TargetUserId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Identity.AccountStatusChangeLog |
| dbo.AccountStatusChangeLogs | CHECK | CK_AccountStatusChangeLogs_FromStatus | {"name": "CK_AccountStatusChangeLogs_FromStatus", "sql": "[FromStatus] IN (1, 2, 3)"} | RoadGuardSystem.BusinessObjects.Identity.AccountStatusChangeLog |
| dbo.AccountStatusChangeLogs | CHECK | CK_AccountStatusChangeLogs_FromToStatus_Diff | {"name": "CK_AccountStatusChangeLogs_FromToStatus_Diff", "sql": "[FromStatus] <> [ToStatus]"} | RoadGuardSystem.BusinessObjects.Identity.AccountStatusChangeLog |
| dbo.AccountStatusChangeLogs | CHECK | CK_AccountStatusChangeLogs_Reason_SafeCode | {"name": "CK_AccountStatusChangeLogs_Reason_SafeCode", "sql": "[Reason] IN ('ADMINISTRATOR_INITIATED', 'SELF_SERVICE_ACCOUNT_RECOVERY', 'REGISTRATION_APPROVED', 'SAFETY_POLICY_VIOLATION', 'NO_STATUS_CHANGE', 'ADMINISTRATIVE_LOCK', 'SECURITY_INCIDENT', 'ACCOUNT_REACTIVATED')"} | RoadGuardSystem.BusinessObjects.Identity.AccountStatusChangeLog |
| dbo.AccountStatusChangeLogs | CHECK | CK_AccountStatusChangeLogs_Source_SafeCode | {"name": "CK_AccountStatusChangeLogs_Source_SafeCode", "sql": "[Source] IN ('ADMIN_API', 'SELF_SERVICE', 'IDENTITY_SERVICE', 'COMPLIANCE_REVIEW', 'SYSTEM')"} | RoadGuardSystem.BusinessObjects.Identity.AccountStatusChangeLog |
| dbo.AccountStatusChangeLogs | CHECK | CK_AccountStatusChangeLogs_ToStatus | {"name": "CK_AccountStatusChangeLogs_ToStatus", "sql": "[ToStatus] IN (1, 2, 3)"} | RoadGuardSystem.BusinessObjects.Identity.AccountStatusChangeLog |
| dbo.AIDetections | KEY | PK_AIDetections | {"name": "PK_AIDetections", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Processing.AIDetection |
| dbo.AIDetections | INDEX | IX_AIDetections_DefectTypeCode | {"name": "IX_AIDetections_DefectTypeCode", "columns": ["DefectTypeCode"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Processing.AIDetection |
| dbo.AIDetections | INDEX | IX_AIDetections_ModelVersionId | {"name": "IX_AIDetections_ModelVersionId", "columns": ["ModelVersionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Processing.AIDetection |
| dbo.AIDetections | INDEX | IX_AIDetections_ProcessingJobId | {"name": "IX_AIDetections_ProcessingJobId", "columns": ["ProcessingJobId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Processing.AIDetection |
| dbo.AIDetections | INDEX | IX_AIDetections_RoadSectionVersionId | {"name": "IX_AIDetections_RoadSectionVersionId", "columns": ["RoadSectionVersionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Processing.AIDetection |
| dbo.AIDetections | CHECK | CK_AIDetections_Confidence | {"name": "CK_AIDetections_Confidence", "sql": "[Confidence] >= 0 AND [Confidence] <= 1"} | RoadGuardSystem.BusinessObjects.Processing.AIDetection |
| dbo.AIDetections | CHECK | CK_AIDetections_EstimatedDimensions | {"name": "CK_AIDetections_EstimatedDimensions", "sql": "([EstimatedWidth] IS NULL OR [EstimatedWidth] >= 0) AND ([EstimatedLength] IS NULL OR [EstimatedLength] >= 0)"} | RoadGuardSystem.BusinessObjects.Processing.AIDetection |
| dbo.AIDetections | CHECK | CK_AIDetections_RawPayload_Json | {"name": "CK_AIDetections_RawPayload_Json", "sql": "ISJSON([RawPayload]) = 1"} | RoadGuardSystem.BusinessObjects.Processing.AIDetection |
| dbo.AIModelVersions | KEY | PK_AIModelVersions | {"name": "PK_AIModelVersions", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Processing.AIModelVersion |
| dbo.AIModelVersions | INDEX | IX_AIModelVersions_ModelName | {"name": "IX_AIModelVersions_ModelName", "columns": ["ModelName"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Processing.AIModelVersion |
| dbo.AIModelVersions | INDEX | IX_AIModelVersions_ReleasedByUserId | {"name": "IX_AIModelVersions_ReleasedByUserId", "columns": ["ReleasedByUserId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Processing.AIModelVersion |
| dbo.AIModelVersions | INDEX | UX_AIModelVersions_VersionLabel | {"name": "UX_AIModelVersions_VersionLabel", "columns": ["VersionLabel"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Processing.AIModelVersion |
| dbo.AIModelVersions | CHECK | CK_AIModelVersions_Metrics_Json | {"name": "CK_AIModelVersions_Metrics_Json", "sql": "[Metrics] IS NULL OR ISJSON([Metrics]) = 1"} | RoadGuardSystem.BusinessObjects.Processing.AIModelVersion |
| dbo.AIModelVersions | CHECK | CK_AIModelVersions_OperatingThresholds_Json | {"name": "CK_AIModelVersions_OperatingThresholds_Json", "sql": "[OperatingThresholds] IS NULL OR ISJSON([OperatingThresholds]) = 1"} | RoadGuardSystem.BusinessObjects.Processing.AIModelVersion |
| dbo.AIModelVersions | CHECK | CK_AIModelVersions_ReleaseMetadata | {"name": "CK_AIModelVersions_ReleaseMetadata", "sql": "([ReleasedAt] IS NULL AND [ReleasedByUserId] IS NULL) OR ([ReleasedAt] IS NOT NULL AND [ReleasedByUserId] IS NOT NULL)"} | RoadGuardSystem.BusinessObjects.Processing.AIModelVersion |
| dbo.AIModelVersions | CHECK | CK_AIModelVersions_Status | {"name": "CK_AIModelVersions_Status", "sql": "[Status] IN (1, 2, 3)"} | RoadGuardSystem.BusinessObjects.Processing.AIModelVersion |
| dbo.Anh02AiDetectionProvenance | KEY | PK_Anh02AiDetectionProvenance | {"name": "PK_Anh02AiDetectionProvenance", "columns": ["DetectionId"], "primary": true} | RoadGuardSystem.BusinessObjects.Processing.AiDetectionProvenance |
| dbo.Anh02AiDetectionProvenance | INDEX | IX_Anh02AiDetectionProvenance_FrameFileId | {"name": "IX_Anh02AiDetectionProvenance_FrameFileId", "columns": ["FrameFileId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Processing.AiDetectionProvenance |
| dbo.Anh02AiDetectionProvenance | INDEX | IX_Anh02AiDetectionProvenance_SegmentId | {"name": "IX_Anh02AiDetectionProvenance_SegmentId", "columns": ["SegmentId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Processing.AiDetectionProvenance |
| dbo.Anh02AiDetectionProvenance | INDEX | IX_Anh02AiDetectionProvenance_SourceVideoFileId | {"name": "IX_Anh02AiDetectionProvenance_SourceVideoFileId", "columns": ["SourceVideoFileId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Processing.AiDetectionProvenance |
| dbo.Anh02AiDetectionProvenance | INDEX | IX_Anh02AiDetectionProvenance_ResultId_RunId | {"name": "IX_Anh02AiDetectionProvenance_ResultId_RunId", "columns": ["ResultId", "RunId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Processing.AiDetectionProvenance |
| dbo.Anh02AiDetectionProvenance | INDEX | IX_Anh02AiDetectionProvenance_RunId_SourceVideoFileId_SourceVideoFileVersion | {"name": "IX_Anh02AiDetectionProvenance_RunId_SourceVideoFileId_SourceVideoFileVersion", "columns": ["RunId", "SourceVideoFileId", "SourceVideoFileVersion"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Processing.AiDetectionProvenance |
| dbo.Anh02AiDetectionProvenance | CHECK | CK_Anh02AiDetection_Box | {"name": "CK_Anh02AiDetection_Box", "sql": "[BoxX]>=0 AND [BoxY]>=0 AND [BoxWidth]>0 AND [BoxHeight]>0 AND [BoxX]+[BoxWidth]<=1 AND [BoxY]+[BoxHeight]<=1"} | RoadGuardSystem.BusinessObjects.Processing.AiDetectionProvenance |
| dbo.Anh02AiDetectionProvenance | CHECK | CK_Anh02AiDetection_Time | {"name": "CK_Anh02AiDetection_Time", "sql": "[TimestampMilliseconds]>=0 AND [SourceDurationMilliseconds]>0 AND [TimestampMilliseconds]<[SourceDurationMilliseconds]"} | RoadGuardSystem.BusinessObjects.Processing.AiDetectionProvenance |
| dbo.Anh02AiManifestFiles | KEY | PK_Anh02AiManifestFiles | {"name": "PK_Anh02AiManifestFiles", "columns": ["RunId", "FileId"], "primary": true} | RoadGuardSystem.BusinessObjects.Processing.AiManifestFileReference |
| dbo.Anh02AiManifestFiles | KEY | AK_Anh02AiManifestFiles_RunId_FileId_FileVersion | {"name": "AK_Anh02AiManifestFiles_RunId_FileId_FileVersion", "columns": ["RunId", "FileId", "FileVersion"], "primary": false} | RoadGuardSystem.BusinessObjects.Processing.AiManifestFileReference |
| dbo.Anh02AiManifestFiles | INDEX | IX_Anh02AiManifestFiles_FileId | {"name": "IX_Anh02AiManifestFiles_FileId", "columns": ["FileId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Processing.AiManifestFileReference |
| dbo.Anh02AiMockRuns | KEY | PK_Anh02AiMockRuns | {"name": "PK_Anh02AiMockRuns", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Processing.AiMockRun |
| dbo.Anh02AiMockRuns | INDEX | IX_Anh02AiMockRuns_AnalysisRunId | {"name": "IX_Anh02AiMockRuns_AnalysisRunId", "columns": ["AnalysisRunId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Processing.AiMockRun |
| dbo.Anh02AiMockRuns | INDEX | IX_Anh02AiMockRuns_AttemptId | {"name": "IX_Anh02AiMockRuns_AttemptId", "columns": ["AttemptId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Processing.AiMockRun |
| dbo.Anh02AiMockRuns | INDEX | IX_Anh02AiMockRuns_CreatedBy | {"name": "IX_Anh02AiMockRuns_CreatedBy", "columns": ["CreatedBy"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Processing.AiMockRun |
| dbo.Anh02AiMockRuns | INDEX | IX_Anh02AiMockRuns_DatasetVersionId | {"name": "IX_Anh02AiMockRuns_DatasetVersionId", "columns": ["DatasetVersionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Processing.AiMockRun |
| dbo.Anh02AiMockRuns | INDEX | IX_Anh02AiMockRuns_ModelVersionId | {"name": "IX_Anh02AiMockRuns_ModelVersionId", "columns": ["ModelVersionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Processing.AiMockRun |
| dbo.Anh02AiMockRuns | INDEX | IX_Anh02AiMockRuns_ProcessingJobId | {"name": "IX_Anh02AiMockRuns_ProcessingJobId", "columns": ["ProcessingJobId"], "unique": true, "filter": "[Stage]='VIDEO_ANALYSIS'"} | RoadGuardSystem.BusinessObjects.Processing.AiMockRun |
| dbo.Anh02AiMockRuns | INDEX | IX_Anh02AiMockRuns_ProjectId | {"name": "IX_Anh02AiMockRuns_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Processing.AiMockRun |
| dbo.Anh02AiMockRuns | INDEX | IX_Anh02AiMockRuns_ResultId_Id | {"name": "IX_Anh02AiMockRuns_ResultId_Id", "columns": ["ResultId", "Id"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Processing.AiMockRun |
| dbo.Anh02AiMockRuns | INDEX | IX_Anh02AiMockRuns_SegmentSetId_RouteVersionId | {"name": "IX_Anh02AiMockRuns_SegmentSetId_RouteVersionId", "columns": ["SegmentSetId", "RouteVersionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Processing.AiMockRun |
| dbo.Anh02AiMockRuns | INDEX | IX_Anh02AiMockRuns_Status_LeaseUntil | {"name": "IX_Anh02AiMockRuns_Status_LeaseUntil", "columns": ["Status", "LeaseUntil"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Processing.AiMockRun |
| dbo.Anh02AiMockRuns | CHECK | CK_Anh02AiMockRuns_Completion | {"name": "CK_Anh02AiMockRuns_Completion", "sql": "([Status]='SUCCEEDED' AND [ResultId] IS NOT NULL AND [CompletedAt] IS NOT NULL) OR ([Status]<>'SUCCEEDED' AND [ResultId] IS NULL)"} | RoadGuardSystem.BusinessObjects.Processing.AiMockRun |
| dbo.Anh02AiMockRuns | CHECK | CK_Anh02AiMockRuns_Manifest | {"name": "CK_Anh02AiMockRuns_Manifest", "sql": "ISJSON([CanonicalManifest])=1"} | RoadGuardSystem.BusinessObjects.Processing.AiMockRun |
| dbo.Anh02AiMockRuns | CHECK | CK_Anh02AiMockRuns_Stage | {"name": "CK_Anh02AiMockRuns_Stage", "sql": "[Stage] IN ('VIDEO_ANALYSIS','DUPLICATE_MATCHING')"} | RoadGuardSystem.BusinessObjects.Processing.AiMockRun |
| dbo.Anh02AiMockRuns | CHECK | CK_Anh02AiMockRuns_Status | {"name": "CK_Anh02AiMockRuns_Status", "sql": "[Status] IN ('QUEUED','RUNNING','SUCCEEDED','FAILED')"} | RoadGuardSystem.BusinessObjects.Processing.AiMockRun |
| dbo.Anh02AiResultProvenance | KEY | PK_Anh02AiResultProvenance | {"name": "PK_Anh02AiResultProvenance", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Processing.AiResultProvenance |
| dbo.Anh02AiResultProvenance | KEY | AK_Anh02AiResultProvenance_Id_RunId | {"name": "AK_Anh02AiResultProvenance_Id_RunId", "columns": ["Id", "RunId"], "primary": false} | RoadGuardSystem.BusinessObjects.Processing.AiResultProvenance |
| dbo.Anh02AiResultProvenance | INDEX | IX_Anh02AiResultProvenance_AttemptId | {"name": "IX_Anh02AiResultProvenance_AttemptId", "columns": ["AttemptId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Processing.AiResultProvenance |
| dbo.Anh02AiResultProvenance | INDEX | IX_Anh02AiResultProvenance_ProcessingJobId | {"name": "IX_Anh02AiResultProvenance_ProcessingJobId", "columns": ["ProcessingJobId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Processing.AiResultProvenance |
| dbo.Anh02AiResultProvenance | INDEX | IX_Anh02AiResultProvenance_RunId | {"name": "IX_Anh02AiResultProvenance_RunId", "columns": ["RunId"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Processing.AiResultProvenance |
| dbo.Anh02AiResultProvenance | CHECK | CK_Anh02AiResult_Json | {"name": "CK_Anh02AiResult_Json", "sql": "ISJSON([CanonicalResult])=1"} | RoadGuardSystem.BusinessObjects.Processing.AiResultProvenance |
| dbo.Anh02ExportJobs | KEY | PK_Anh02ExportJobs | {"name": "PK_Anh02ExportJobs", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Exports.ExportJob |
| dbo.Anh02ExportJobs | INDEX | IX_Anh02ExportJobs_ArtifactId | {"name": "IX_Anh02ExportJobs_ArtifactId", "columns": ["ArtifactId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Exports.ExportJob |
| dbo.Anh02ExportJobs | INDEX | IX_Anh02ExportJobs_ProjectId | {"name": "IX_Anh02ExportJobs_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Exports.ExportJob |
| dbo.Anh02ExportJobs | INDEX | IX_Anh02ExportJobs_RequestedBy | {"name": "IX_Anh02ExportJobs_RequestedBy", "columns": ["RequestedBy"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Exports.ExportJob |
| dbo.Anh02ExportJobs | INDEX | IX_Anh02ExportJobs_SnapshotId | {"name": "IX_Anh02ExportJobs_SnapshotId", "columns": ["SnapshotId"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Exports.ExportJob |
| dbo.Anh02ExportJobs | INDEX | IX_Anh02ExportJobs_Status_NextAttemptAt_LeaseUntil | {"name": "IX_Anh02ExportJobs_Status_NextAttemptAt_LeaseUntil", "columns": ["Status", "NextAttemptAt", "LeaseUntil"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Exports.ExportJob |
| dbo.Anh02ExportJobs | CHECK | CK_Anh02ExportJob_Expiry | {"name": "CK_Anh02ExportJob_Expiry", "sql": "([Status] <> 'SUCCEEDED' AND [ExpiresAt] IS NULL) OR ([Status]='SUCCEEDED' AND [ArtifactId] IS NOT NULL AND [CompletedAt] IS NOT NULL AND [ExpiresAt]=DATEADD(day,30,[CompletedAt]))"} | RoadGuardSystem.BusinessObjects.Exports.ExportJob |
| dbo.Anh02ExportJobs | CHECK | CK_Anh02ExportJob_Format | {"name": "CK_Anh02ExportJob_Format", "sql": "[Format] IN ('PDF','ZIP') AND ([Kind] <> 'TRAINING' OR [Format]='ZIP')"} | RoadGuardSystem.BusinessObjects.Exports.ExportJob |
| dbo.Anh02ExportJobs | CHECK | CK_Anh02ExportJob_Kind | {"name": "CK_Anh02ExportJob_Kind", "sql": "[Kind] IN ('DOSSIER','TRAINING')"} | RoadGuardSystem.BusinessObjects.Exports.ExportJob |
| dbo.Anh02ExportJobs | CHECK | CK_Anh02ExportJob_State | {"name": "CK_Anh02ExportJob_State", "sql": "[Status] IN ('QUEUED','RUNNING','SUCCEEDED','FAILED')"} | RoadGuardSystem.BusinessObjects.Exports.ExportJob |
| dbo.Anh02ExportSnapshotFiles | KEY | PK_Anh02ExportSnapshotFiles | {"name": "PK_Anh02ExportSnapshotFiles", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Exports.ExportSnapshotFile |
| dbo.Anh02ExportSnapshotFiles | INDEX | IX_Anh02ExportSnapshotFiles_FileId | {"name": "IX_Anh02ExportSnapshotFiles_FileId", "columns": ["FileId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Exports.ExportSnapshotFile |
| dbo.Anh02ExportSnapshotFiles | INDEX | IX_Anh02ExportSnapshotFiles_SnapshotId_FileId | {"name": "IX_Anh02ExportSnapshotFiles_SnapshotId_FileId", "columns": ["SnapshotId", "FileId"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Exports.ExportSnapshotFile |
| dbo.Anh02ExportSnapshotFiles | CHECK | CK_Anh02ExportSnapshotFile_Size | {"name": "CK_Anh02ExportSnapshotFile_Size", "sql": "[SizeBytes]>0"} | RoadGuardSystem.BusinessObjects.Exports.ExportSnapshotFile |
| dbo.Anh02ExportSnapshots | KEY | PK_Anh02ExportSnapshots | {"name": "PK_Anh02ExportSnapshots", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Exports.ExportSnapshot |
| dbo.Anh02ExportSnapshots | INDEX | IX_Anh02ExportSnapshots_ProjectId | {"name": "IX_Anh02ExportSnapshots_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Exports.ExportSnapshot |
| dbo.Anh02GeneratedArtifacts | KEY | PK_Anh02GeneratedArtifacts | {"name": "PK_Anh02GeneratedArtifacts", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Exports.GeneratedArtifact |
| dbo.Anh02GeneratedArtifacts | INDEX | IX_Anh02GeneratedArtifacts_ExportJobId | {"name": "IX_Anh02GeneratedArtifacts_ExportJobId", "columns": ["ExportJobId"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Exports.GeneratedArtifact |
| dbo.Anh02GeneratedArtifacts | INDEX | IX_Anh02GeneratedArtifacts_FileId | {"name": "IX_Anh02GeneratedArtifacts_FileId", "columns": ["FileId"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Exports.GeneratedArtifact |
| dbo.Anh02GeneratedArtifacts | INDEX | IX_Anh02GeneratedArtifacts_SnapshotId | {"name": "IX_Anh02GeneratedArtifacts_SnapshotId", "columns": ["SnapshotId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Exports.GeneratedArtifact |
| dbo.AuditLogs | KEY | PK_AuditLogs | {"name": "PK_AuditLogs", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Auditing.AuditLog |
| dbo.AuditLogs | INDEX | IX_AuditLogs_ActorUserId | {"name": "IX_AuditLogs_ActorUserId", "columns": ["ActorUserId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Auditing.AuditLog |
| dbo.AuditLogs | INDEX | IX_AuditLogs_CorrelationId | {"name": "IX_AuditLogs_CorrelationId", "columns": ["CorrelationId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Auditing.AuditLog |
| dbo.AuditLogs | INDEX | IX_AuditLogs_OccurredAtUtc | {"name": "IX_AuditLogs_OccurredAtUtc", "columns": ["OccurredAtUtc"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Auditing.AuditLog |
| dbo.AuditLogs | INDEX | IX_AuditLogs_Entity | {"name": "IX_AuditLogs_Entity", "columns": ["EntityType", "EntityId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Auditing.AuditLog |
| dbo.AuditLogs | CHECK | CK_AuditLogs_AfterSnapshot_Json | {"name": "CK_AuditLogs_AfterSnapshot_Json", "sql": "[AfterSnapshot] IS NULL OR ISJSON([AfterSnapshot]) = 1"} | RoadGuardSystem.BusinessObjects.Auditing.AuditLog |
| dbo.AuditLogs | CHECK | CK_AuditLogs_BeforeSnapshot_Json | {"name": "CK_AuditLogs_BeforeSnapshot_Json", "sql": "[BeforeSnapshot] IS NULL OR ISJSON([BeforeSnapshot]) = 1"} | RoadGuardSystem.BusinessObjects.Auditing.AuditLog |
| dbo.BaselineCurrentPointers | KEY | PK_BaselineCurrentPointers | {"name": "PK_BaselineCurrentPointers", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Surveys.BaselineCurrentPointer |
| dbo.BaselineCurrentPointers | INDEX | IX_BaselineCurrentPointers_SelectionId | {"name": "IX_BaselineCurrentPointers_SelectionId", "columns": ["SelectionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Surveys.BaselineCurrentPointer |
| dbo.BaselineCurrentPointers | INDEX | IX_BaselineCurrentPointers_ProjectId_RouteVersionId_SegmentSetId_SegmentId_TargetBand | {"name": "IX_BaselineCurrentPointers_ProjectId_RouteVersionId_SegmentSetId_SegmentId_TargetBand", "columns": ["ProjectId", "RouteVersionId", "SegmentSetId", "SegmentId", "TargetBand"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Surveys.BaselineCurrentPointer |
| dbo.BaselineSelectionItems | KEY | PK_BaselineSelectionItems | {"name": "PK_BaselineSelectionItems", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Surveys.BaselineSelectionItem |
| dbo.BaselineSelectionItems | INDEX | IX_BaselineSelectionItems_AssessmentId | {"name": "IX_BaselineSelectionItems_AssessmentId", "columns": ["AssessmentId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Surveys.BaselineSelectionItem |
| dbo.BaselineSelectionItems | INDEX | IX_BaselineSelectionItems_DatasetId | {"name": "IX_BaselineSelectionItems_DatasetId", "columns": ["DatasetId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Surveys.BaselineSelectionItem |
| dbo.BaselineSelectionItems | INDEX | IX_BaselineSelectionItems_BaselineSelectionId_RouteVersionId_SegmentSetId_SegmentId_TargetBand | {"name": "IX_BaselineSelectionItems_BaselineSelectionId_RouteVersionId_SegmentSetId_SegmentId_TargetBand", "columns": ["BaselineSelectionId", "RouteVersionId", "SegmentSetId", "SegmentId", "TargetBand"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Surveys.BaselineSelectionItem |
| dbo.BaselineSelections | KEY | PK_BaselineSelections | {"name": "PK_BaselineSelections", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Surveys.BaselineSelection |
| dbo.BaselineSelections | INDEX | IX_BaselineSelections_ProjectId | {"name": "IX_BaselineSelections_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Surveys.BaselineSelection |
| dbo.BaselineSelections | INDEX | IX_BaselineSelections_SelectedBy | {"name": "IX_BaselineSelections_SelectedBy", "columns": ["SelectedBy"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Surveys.BaselineSelection |
| dbo.BusinessDutyAppointments | KEY | PK_BusinessDutyAppointments | {"name": "PK_BusinessDutyAppointments", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Clocks.BusinessDutyAppointment |
| dbo.BusinessDutyAppointments | INDEX | IX_BusinessDutyAppointments_CurrentActorId | {"name": "IX_BusinessDutyAppointments_CurrentActorId", "columns": ["CurrentActorId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Clocks.BusinessDutyAppointment |
| dbo.BusinessDutyAppointments | INDEX | IX_BusinessDutyAppointments_DecisionActorId | {"name": "IX_BusinessDutyAppointments_DecisionActorId", "columns": ["DecisionActorId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Clocks.BusinessDutyAppointment |
| dbo.BusinessDutyAppointments | INDEX | IX_BusinessDutyAppointments_RequestId | {"name": "IX_BusinessDutyAppointments_RequestId", "columns": ["RequestId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Clocks.BusinessDutyAppointment |
| dbo.BusinessReceivingRequests | KEY | PK_BusinessReceivingRequests | {"name": "PK_BusinessReceivingRequests", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Clocks.BusinessReceivingRequest |
| dbo.BusinessReceivingRequests | INDEX | IX_BusinessReceivingRequests_ClockId | {"name": "IX_BusinessReceivingRequests_ClockId", "columns": ["ClockId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Clocks.BusinessReceivingRequest |
| dbo.BusinessReceivingRequests | INDEX | IX_BusinessReceivingRequests_ResponsibleActorId | {"name": "IX_BusinessReceivingRequests_ResponsibleActorId", "columns": ["ResponsibleActorId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Clocks.BusinessReceivingRequest |
| dbo.BusinessReceivingRequests | INDEX | IX_BusinessReceivingRequests_ProjectId_ScopeId_CompletedAt | {"name": "IX_BusinessReceivingRequests_ProjectId_ScopeId_CompletedAt", "columns": ["ProjectId", "ScopeId", "CompletedAt"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Clocks.BusinessReceivingRequest |
| dbo.BusinessReceivingRequests | INDEX | IX_BusinessReceivingRequests_SourceKind_SourceId_Kind | {"name": "IX_BusinessReceivingRequests_SourceKind_SourceId_Kind", "columns": ["SourceKind", "SourceId", "Kind"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Clocks.BusinessReceivingRequest |
| dbo.BusinessReceivingRequests | CHECK | CK_BusinessReceivingRequests_Ack | {"name": "CK_BusinessReceivingRequests_Ack", "sql": "([AcknowledgedAt] IS NULL AND [AcknowledgmentId] IS NULL AND [AcknowledgedBy] IS NULL AND [ClockId] IS NULL AND [ClaimedDeviceAt] IS NULL) OR ([AcknowledgedAt] IS NOT NULL AND [AcknowledgmentId] IS NOT NULL AND [AcknowledgedBy] IS NOT NULL AND [ClockId] IS NOT NULL AND [AcknowledgedAt]>=[RequestedAt])"} | RoadGuardSystem.BusinessObjects.Clocks.BusinessReceivingRequest |
| dbo.CandidateSourceHeads | KEY | PK_CandidateSourceHeads | {"name": "PK_CandidateSourceHeads", "columns": ["SourceKind", "SourceId"], "primary": true} | RoadGuardSystem.Repositories.Models.Huy01.HuyCandidateSourceHead |
| dbo.CandidateSourceHeads | INDEX | IX_CandidateSourceHeads_ProjectId | {"name": "IX_CandidateSourceHeads_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.Repositories.Models.Huy01.HuyCandidateSourceHead |
| dbo.CandidateSourceHeads | INDEX | IX_CandidateSourceHeads_DecisionId_SourceKind_SourceId_ProjectId | {"name": "IX_CandidateSourceHeads_DecisionId_SourceKind_SourceId_ProjectId", "columns": ["DecisionId", "SourceKind", "SourceId", "ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.Repositories.Models.Huy01.HuyCandidateSourceHead |
| dbo.CaseConclusionDefects | KEY | PK_CaseConclusionDefects | {"name": "PK_CaseConclusionDefects", "columns": ["ConclusionId", "DefectId"], "primary": true} | RoadGuardSystem.Repositories.Models.Huy01.HuyConclusionDefect |
| dbo.CaseConclusionDefects | INDEX | IX_CaseConclusionDefects_DefectId | {"name": "IX_CaseConclusionDefects_DefectId", "columns": ["DefectId"], "unique": false, "filter": null} | RoadGuardSystem.Repositories.Models.Huy01.HuyConclusionDefect |
| dbo.CaseConclusionEvidence | KEY | PK_CaseConclusionEvidence | {"name": "PK_CaseConclusionEvidence", "columns": ["ConclusionId", "EvidenceId"], "primary": true} | RoadGuardSystem.Repositories.Models.Huy01.HuyConclusionEvidence |
| dbo.CaseConclusionEvidence | INDEX | IX_CaseConclusionEvidence_SourceReportId | {"name": "IX_CaseConclusionEvidence_SourceReportId", "columns": ["SourceReportId"], "unique": false, "filter": null} | RoadGuardSystem.Repositories.Models.Huy01.HuyConclusionEvidence |
| dbo.CaseConclusionEvidence | CHECK | CK_CaseConclusionEvidence_TypedEvidence | {"name": "CK_CaseConclusionEvidence_TypedEvidence", "sql": "([OriginalEvidenceId] IS NOT NULL AND [OriginalEvidenceId]=[EvidenceId] AND [SupplementEvidenceId] IS NULL) OR ([SupplementEvidenceId] IS NOT NULL AND [SupplementEvidenceId]=[EvidenceId] AND [OriginalEvidenceId] IS NULL)"} | RoadGuardSystem.Repositories.Models.Huy01.HuyConclusionEvidence |
| dbo.CaseConclusions | KEY | PK_CaseConclusions | {"name": "PK_CaseConclusions", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Cases.CaseConclusion |
| dbo.CaseConclusions | INDEX | IX_CaseConclusions_CaseId | {"name": "IX_CaseConclusions_CaseId", "columns": ["CaseId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Cases.CaseConclusion |
| dbo.CasePublicationDefects | KEY | PK_CasePublicationDefects | {"name": "PK_CasePublicationDefects", "columns": ["PublicationId", "DefectId"], "primary": true} | RoadGuardSystem.Repositories.Models.Huy01.HuyPublicationDefect |
| dbo.CasePublicationDefects | INDEX | IX_CasePublicationDefects_DefectId | {"name": "IX_CasePublicationDefects_DefectId", "columns": ["DefectId"], "unique": false, "filter": null} | RoadGuardSystem.Repositories.Models.Huy01.HuyPublicationDefect |
| dbo.CasePublicationEvidence | KEY | PK_CasePublicationEvidence | {"name": "PK_CasePublicationEvidence", "columns": ["PublicationId", "RecipientReportId", "EvidenceId"], "primary": true} | RoadGuardSystem.Repositories.Models.Huy01.HuyPublicationEvidence |
| dbo.CasePublicationEvidence | INDEX | IX_CasePublicationEvidence_SourceReportId | {"name": "IX_CasePublicationEvidence_SourceReportId", "columns": ["SourceReportId"], "unique": false, "filter": null} | RoadGuardSystem.Repositories.Models.Huy01.HuyPublicationEvidence |
| dbo.CasePublicationEvidence | CHECK | CK_CasePublicationEvidence_TypedEvidence | {"name": "CK_CasePublicationEvidence_TypedEvidence", "sql": "([OriginalEvidenceId] IS NOT NULL AND [OriginalEvidenceId]=[EvidenceId] AND [SupplementEvidenceId] IS NULL) OR ([SupplementEvidenceId] IS NOT NULL AND [SupplementEvidenceId]=[EvidenceId] AND [OriginalEvidenceId] IS NULL)"} | RoadGuardSystem.Repositories.Models.Huy01.HuyPublicationEvidence |
| dbo.CasePublicationRecipients | KEY | PK_CasePublicationRecipients | {"name": "PK_CasePublicationRecipients", "columns": ["PublicationId", "ReportId"], "primary": true} | RoadGuardSystem.Repositories.Models.Huy01.HuyPublicationRecipient |
| dbo.CasePublicationRecipients | INDEX | IX_CasePublicationRecipients_ReportId | {"name": "IX_CasePublicationRecipients_ReportId", "columns": ["ReportId"], "unique": false, "filter": null} | RoadGuardSystem.Repositories.Models.Huy01.HuyPublicationRecipient |
| dbo.CasePublications | KEY | PK_CasePublications | {"name": "PK_CasePublications", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Cases.CasePublication |
| dbo.CasePublications | INDEX | IX_CasePublications_CaseId | {"name": "IX_CasePublications_CaseId", "columns": ["CaseId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Cases.CasePublication |
| dbo.CaseReportLinkHistory | KEY | PK_CaseReportLinkHistory | {"name": "PK_CaseReportLinkHistory", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Cases.CaseReportLinkHistory |
| dbo.CaseReportLinkHistory | INDEX | IX_CaseReportLinkHistory_ActorUserId | {"name": "IX_CaseReportLinkHistory_ActorUserId", "columns": ["ActorUserId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Cases.CaseReportLinkHistory |
| dbo.CaseReportLinkHistory | INDEX | IX_CaseReportLinkHistory_FromCaseId | {"name": "IX_CaseReportLinkHistory_FromCaseId", "columns": ["FromCaseId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Cases.CaseReportLinkHistory |
| dbo.CaseReportLinkHistory | INDEX | IX_CaseReportLinkHistory_ToCaseId | {"name": "IX_CaseReportLinkHistory_ToCaseId", "columns": ["ToCaseId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Cases.CaseReportLinkHistory |
| dbo.CaseReportLinkHistoryReports | KEY | PK_CaseReportLinkHistoryReports | {"name": "PK_CaseReportLinkHistoryReports", "columns": ["HistoryId", "ReportId"], "primary": true} | RoadGuardSystem.Repositories.Models.Huy01.HuyLinkHistoryReport |
| dbo.CaseReportLinkHistoryReports | INDEX | IX_CaseReportLinkHistoryReports_ReportId | {"name": "IX_CaseReportLinkHistoryReports_ReportId", "columns": ["ReportId"], "unique": false, "filter": null} | RoadGuardSystem.Repositories.Models.Huy01.HuyLinkHistoryReport |
| dbo.CaseReportLinks | KEY | PK_CaseReportLinks | {"name": "PK_CaseReportLinks", "columns": ["Id"], "primary": true} | RoadGuardSystem.Repositories.Models.Huy01.HuyCaseReportLink |
| dbo.CaseReportLinks | INDEX | IX_CaseReportLinks_ReportId | {"name": "IX_CaseReportLinks_ReportId", "columns": ["ReportId"], "unique": true, "filter": "[EndedAt] IS NULL"} | RoadGuardSystem.Repositories.Models.Huy01.HuyCaseReportLink |
| dbo.CaseReportLinks | INDEX | IX_CaseReportLinks_CaseId_ReportId | {"name": "IX_CaseReportLinks_CaseId_ReportId", "columns": ["CaseId", "ReportId"], "unique": false, "filter": null} | RoadGuardSystem.Repositories.Models.Huy01.HuyCaseReportLink |
| dbo.CaseReportLinks | CHECK | CK_CaseReportLinks_Times | {"name": "CK_CaseReportLinks_Times", "sql": "[EndedAt] IS NULL OR [EndedAt] >= [StartedAt]"} | RoadGuardSystem.Repositories.Models.Huy01.HuyCaseReportLink |
| dbo.CauseCategories | KEY | PK_CauseCategories | {"name": "PK_CauseCategories", "columns": ["Code"], "primary": true} | RoadGuardSystem.BusinessObjects.Catalogs.CauseCategory |
| dbo.ConsumerEffectReceipts | KEY | PK_ConsumerEffectReceipts | {"name": "PK_ConsumerEffectReceipts", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Messaging.ConsumerEffectReceipt |
| dbo.ConsumerEffectReceipts | INDEX | UX_ConsumerEffectReceipts_MessageConsumer | {"name": "UX_ConsumerEffectReceipts_MessageConsumer", "columns": ["MessageId", "ConsumerName"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Messaging.ConsumerEffectReceipt |
| dbo.CrsProfileRevisions | KEY | PK_CrsProfileRevisions | {"name": "PK_CrsProfileRevisions", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Projects.CrsProfileRevision |
| dbo.CrsProfileRevisions | KEY | AK_CrsProfileRevisions_Id_ProjectId | {"name": "AK_CrsProfileRevisions_Id_ProjectId", "columns": ["Id", "ProjectId"], "primary": false} | RoadGuardSystem.BusinessObjects.Projects.CrsProfileRevision |
| dbo.CrsProfileRevisions | INDEX | IX_CrsProfileRevisions_CreatedBy | {"name": "IX_CrsProfileRevisions_CreatedBy", "columns": ["CreatedBy"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.CrsProfileRevision |
| dbo.CrsProfileRevisions | INDEX | IX_CrsProfileRevisions_ProjectId_Code_Revision | {"name": "IX_CrsProfileRevisions_ProjectId_Code_Revision", "columns": ["ProjectId", "Code", "Revision"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.CrsProfileRevision |
| dbo.CrsProfileRevisions | CHECK | CK_CrsProfileRevision | {"name": "CK_CrsProfileRevision", "sql": "[Revision]>0 AND [SourceSrid]>=0 AND [Status] IN ('CANDIDATE','VERIFIED') AND ISJSON([PayloadJson])=1"} | RoadGuardSystem.BusinessObjects.Projects.CrsProfileRevision |
| dbo.DatasetAssessmentItems | KEY | PK_DatasetAssessmentItems | {"name": "PK_DatasetAssessmentItems", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Surveys.DatasetAssessmentItem |
| dbo.DatasetAssessmentItems | INDEX | IX_DatasetAssessmentItems_SegmentId | {"name": "IX_DatasetAssessmentItems_SegmentId", "columns": ["SegmentId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Surveys.DatasetAssessmentItem |
| dbo.DatasetAssessmentItems | INDEX | IX_DatasetAssessmentItems_AssessmentId_RouteVersionId_SegmentSetId_SegmentId_TargetBand | {"name": "IX_DatasetAssessmentItems_AssessmentId_RouteVersionId_SegmentSetId_SegmentId_TargetBand", "columns": ["AssessmentId", "RouteVersionId", "SegmentSetId", "SegmentId", "TargetBand"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Surveys.DatasetAssessmentItem |
| dbo.DatasetAssessmentItems | CHECK | CK_Assessment_Status | {"name": "CK_Assessment_Status", "sql": "[PositionStatus] IN ('PASS','FAIL','UNKNOWN') AND [QualityStatus] IN ('PASS','FAIL','UNKNOWN') AND [CoverageStatus] IN ('PASS','FAIL','UNKNOWN')"} | RoadGuardSystem.BusinessObjects.Surveys.DatasetAssessmentItem |
| dbo.DatasetAssessments | KEY | PK_DatasetAssessments | {"name": "PK_DatasetAssessments", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Surveys.DatasetAssessment |
| dbo.DatasetAssessments | INDEX | IX_DatasetAssessments_ReviewedBy | {"name": "IX_DatasetAssessments_ReviewedBy", "columns": ["ReviewedBy"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Surveys.DatasetAssessment |
| dbo.DatasetAssessments | INDEX | IX_DatasetAssessments_DatasetId_ReviewedAt | {"name": "IX_DatasetAssessments_DatasetId_ReviewedAt", "columns": ["DatasetId", "ReviewedAt"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Surveys.DatasetAssessment |
| dbo.DeadlineBreaches | KEY | PK_DeadlineBreaches | {"name": "PK_DeadlineBreaches", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Clocks.DeadlineBreach |
| dbo.DeadlineBreaches | INDEX | IX_DeadlineBreaches_ClockId_DueAt | {"name": "IX_DeadlineBreaches_ClockId_DueAt", "columns": ["ClockId", "DueAt"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Clocks.DeadlineBreach |
| dbo.DeadlineBreaches | CHECK | CK_DeadlineBreaches_Times | {"name": "CK_DeadlineBreaches_Times", "sql": "[ObservedAt]>=[DueAt]"} | RoadGuardSystem.BusinessObjects.Clocks.DeadlineBreach |
| dbo.DeadlineClocks | KEY | PK_DeadlineClocks | {"name": "PK_DeadlineClocks", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Clocks.DeadlineClock |
| dbo.DeadlineClocks | INDEX | IX_DeadlineClocks_AcknowledgedByUserId | {"name": "IX_DeadlineClocks_AcknowledgedByUserId", "columns": ["AcknowledgedByUserId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Clocks.DeadlineClock |
| dbo.DeadlineClocks | INDEX | IX_DeadlineClocks_AppointedActorId | {"name": "IX_DeadlineClocks_AppointedActorId", "columns": ["AppointedActorId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Clocks.DeadlineClock |
| dbo.DeadlineClocks | INDEX | IX_DeadlineClocks_OriginEventId | {"name": "IX_DeadlineClocks_OriginEventId", "columns": ["OriginEventId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Clocks.DeadlineClock |
| dbo.DeadlineClocks | INDEX | IX_DeadlineClocks_Kind_TargetId | {"name": "IX_DeadlineClocks_Kind_TargetId", "columns": ["Kind", "TargetId"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Clocks.DeadlineClock |
| dbo.DeadlineClocks | INDEX | IX_DeadlineClocks_ProjectId_CompletedAt_CurrentDueAt | {"name": "IX_DeadlineClocks_ProjectId_CompletedAt_CurrentDueAt", "columns": ["ProjectId", "CompletedAt", "CurrentDueAt"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Clocks.DeadlineClock |
| dbo.DeadlineClocks | CHECK | CK_DeadlineClocks_Acknowledgment | {"name": "CK_DeadlineClocks_Acknowledgment", "sql": "([AcknowledgedAt] IS NULL AND [AcknowledgedByUserId] IS NULL AND [AcknowledgmentEventId] IS NULL) OR ([Kind]=9 AND [AcknowledgedAt] IS NOT NULL AND [AcknowledgedAt]>=[OriginAt] AND [AcknowledgedByUserId] IS NOT NULL AND [AcknowledgmentEventId] IS NOT NULL AND [CompletedAt] IS NOT NULL AND [CompletedAt]=[AcknowledgedAt])"} | RoadGuardSystem.BusinessObjects.Clocks.DeadlineClock |
| dbo.DeadlineClocks | CHECK | CK_DeadlineClocks_Kind | {"name": "CK_DeadlineClocks_Kind", "sql": "[Kind] BETWEEN 1 AND 10"} | RoadGuardSystem.BusinessObjects.Clocks.DeadlineClock |
| dbo.DeadlineClocks | CHECK | CK_DeadlineClocks_Times | {"name": "CK_DeadlineClocks_Times", "sql": "([OriginalDueAt]>[OriginAt] OR ([Kind]=10 AND [OriginalDueAt]=[OriginAt])) AND [CurrentDueAt]>=[OriginalDueAt] AND ([CompletedAt] IS NULL OR [CompletedAt]>=[OriginAt])"} | RoadGuardSystem.BusinessObjects.Clocks.DeadlineClock |
| dbo.DeadlineDutyAppointments | KEY | PK_DeadlineDutyAppointments | {"name": "PK_DeadlineDutyAppointments", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Clocks.DeadlineDutyAppointment |
| dbo.DeadlineDutyAppointments | INDEX | IX_DeadlineDutyAppointments_ClockId | {"name": "IX_DeadlineDutyAppointments_ClockId", "columns": ["ClockId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Clocks.DeadlineDutyAppointment |
| dbo.DeadlineDutyAppointments | INDEX | IX_DeadlineDutyAppointments_CurrentActorId | {"name": "IX_DeadlineDutyAppointments_CurrentActorId", "columns": ["CurrentActorId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Clocks.DeadlineDutyAppointment |
| dbo.DeadlineDutyAppointments | INDEX | IX_DeadlineDutyAppointments_DecisionActorId | {"name": "IX_DeadlineDutyAppointments_DecisionActorId", "columns": ["DecisionActorId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Clocks.DeadlineDutyAppointment |
| dbo.DeadlineExtensions | KEY | PK_DeadlineExtensions | {"name": "PK_DeadlineExtensions", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Clocks.DeadlineExtension |
| dbo.DeadlineExtensions | INDEX | IX_DeadlineExtensions_ActorUserId | {"name": "IX_DeadlineExtensions_ActorUserId", "columns": ["ActorUserId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Clocks.DeadlineExtension |
| dbo.DeadlineExtensions | INDEX | IX_DeadlineExtensions_ClockId_NewDueAt | {"name": "IX_DeadlineExtensions_ClockId_NewDueAt", "columns": ["ClockId", "NewDueAt"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Clocks.DeadlineExtension |
| dbo.DeadlineExtensions | CHECK | CK_DeadlineExtensions_Times | {"name": "CK_DeadlineExtensions_Times", "sql": "[NewDueAt]>[PreviousDueAt] AND [NewDueAt]>[OccurredAt]"} | RoadGuardSystem.BusinessObjects.Clocks.DeadlineExtension |
| dbo.Defects | KEY | PK_Defects | {"name": "PK_Defects", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Defects.Defect |
| dbo.Defects | INDEX | IX_Defects_CauseCategoryCode | {"name": "IX_Defects_CauseCategoryCode", "columns": ["CauseCategoryCode"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Defects.Defect |
| dbo.Defects | INDEX | IX_Defects_DefectTypeCode | {"name": "IX_Defects_DefectTypeCode", "columns": ["DefectTypeCode"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Defects.Defect |
| dbo.Defects | INDEX | IX_Defects_ProjectId | {"name": "IX_Defects_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Defects.Defect |
| dbo.Defects | INDEX | IX_Defects_RoadSectionVersionId | {"name": "IX_Defects_RoadSectionVersionId", "columns": ["RoadSectionVersionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Defects.Defect |
| dbo.Defects | INDEX | UX_Defects_SourceAIDetectionId | {"name": "UX_Defects_SourceAIDetectionId", "columns": ["SourceAIDetectionId"], "unique": true, "filter": "[SourceAIDetectionId] IS NOT NULL"} | RoadGuardSystem.BusinessObjects.Defects.Defect |
| dbo.Defects | CHECK | CK_Defects_Severity | {"name": "CK_Defects_Severity", "sql": "[Severity] IS NULL OR [Severity] IN (0, 1, 2, 3, 4)"} | RoadGuardSystem.BusinessObjects.Defects.Defect |
| dbo.Defects | CHECK | CK_Defects_Status | {"name": "CK_Defects_Status", "sql": "[Status] IS NULL OR [Status] IN (0, 1, 2, 3, 4, 5)"} | RoadGuardSystem.BusinessObjects.Defects.Defect |
| dbo.DefectSourceLinks | KEY | PK_DefectSourceLinks | {"name": "PK_DefectSourceLinks", "columns": ["Id"], "primary": true} | RoadGuardSystem.Repositories.Models.Huy01.HuyDefectSourceLink |
| dbo.DefectSourceLinks | INDEX | IX_DefectSourceLinks_AIDetectionSourceId | {"name": "IX_DefectSourceLinks_AIDetectionSourceId", "columns": ["AIDetectionSourceId"], "unique": false, "filter": null} | RoadGuardSystem.Repositories.Models.Huy01.HuyDefectSourceLink |
| dbo.DefectSourceLinks | INDEX | IX_DefectSourceLinks_DecisionId | {"name": "IX_DefectSourceLinks_DecisionId", "columns": ["DecisionId"], "unique": true, "filter": null} | RoadGuardSystem.Repositories.Models.Huy01.HuyDefectSourceLink |
| dbo.DefectSourceLinks | INDEX | IX_DefectSourceLinks_DefectId | {"name": "IX_DefectSourceLinks_DefectId", "columns": ["DefectId"], "unique": false, "filter": null} | RoadGuardSystem.Repositories.Models.Huy01.HuyDefectSourceLink |
| dbo.DefectSourceLinks | INDEX | IX_DefectSourceLinks_ProjectId | {"name": "IX_DefectSourceLinks_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.Repositories.Models.Huy01.HuyDefectSourceLink |
| dbo.DefectSourceLinks | INDEX | IX_DefectSourceLinks_ReportSourceId | {"name": "IX_DefectSourceLinks_ReportSourceId", "columns": ["ReportSourceId"], "unique": false, "filter": null} | RoadGuardSystem.Repositories.Models.Huy01.HuyDefectSourceLink |
| dbo.DefectSourceLinks | INDEX | IX_DefectSourceLinks_SourceKind_SourceId | {"name": "IX_DefectSourceLinks_SourceKind_SourceId", "columns": ["SourceKind", "SourceId"], "unique": true, "filter": "[EndedAt] IS NULL"} | RoadGuardSystem.Repositories.Models.Huy01.HuyDefectSourceLink |
| dbo.DefectSourceLinks | INDEX | IX_DefectSourceLinks_DecisionId_SourceKind_SourceId_ProjectId | {"name": "IX_DefectSourceLinks_DecisionId_SourceKind_SourceId_ProjectId", "columns": ["DecisionId", "SourceKind", "SourceId", "ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.Repositories.Models.Huy01.HuyDefectSourceLink |
| dbo.DefectSourceLinks | CHECK | CK_DefectSourceLinks_TypedSource | {"name": "CK_DefectSourceLinks_TypedSource", "sql": "([SourceKind]=1 AND [ReportSourceId]=[SourceId] AND [AIDetectionSourceId] IS NULL) OR ([SourceKind]=2 AND [AIDetectionSourceId]=[SourceId] AND [ReportSourceId] IS NULL)"} | RoadGuardSystem.Repositories.Models.Huy01.HuyDefectSourceLink |
| dbo.DefectStatisticsSources | KEY | PK_DefectStatisticsSources | {"name": "PK_DefectStatisticsSources", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Reporting.DefectStatisticsSource |
| dbo.DefectStatisticsSources | INDEX | IX_DefectStatisticsSources_ActorId | {"name": "IX_DefectStatisticsSources_ActorId", "columns": ["ActorId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Reporting.DefectStatisticsSource |
| dbo.DefectStatisticsSources | INDEX | IX_DefectStatisticsSources_DefectId | {"name": "IX_DefectStatisticsSources_DefectId", "columns": ["DefectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Reporting.DefectStatisticsSource |
| dbo.DefectStatisticsSources | INDEX | IX_DefectStatisticsSources_ObligationId | {"name": "IX_DefectStatisticsSources_ObligationId", "columns": ["ObligationId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Reporting.DefectStatisticsSource |
| dbo.DefectStatisticsSources | INDEX | IX_DefectStatisticsSources_RoadSectionId | {"name": "IX_DefectStatisticsSources_RoadSectionId", "columns": ["RoadSectionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Reporting.DefectStatisticsSource |
| dbo.DefectStatisticsSources | INDEX | IX_DefectStatisticsSources_RouteVersionId | {"name": "IX_DefectStatisticsSources_RouteVersionId", "columns": ["RouteVersionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Reporting.DefectStatisticsSource |
| dbo.DefectStatisticsSources | INDEX | IX_DefectStatisticsSources_SupersedesId | {"name": "IX_DefectStatisticsSources_SupersedesId", "columns": ["SupersedesId"], "unique": true, "filter": "[SupersedesId] IS NOT NULL"} | RoadGuardSystem.BusinessObjects.Reporting.DefectStatisticsSource |
| dbo.DefectStatisticsSources | INDEX | IX_DefectStatisticsSources_ProjectId_SharedPartId | {"name": "IX_DefectStatisticsSources_ProjectId_SharedPartId", "columns": ["ProjectId", "SharedPartId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Reporting.DefectStatisticsSource |
| dbo.DefectStatisticsSources | CHECK | CK_DefectStatisticsSources_Bounds | {"name": "CK_DefectStatisticsSources_Bounds", "sql": "[From]>=0 AND [To]>[From] AND [OffsetTo]>[OffsetFrom]"} | RoadGuardSystem.BusinessObjects.Reporting.DefectStatisticsSource |
| dbo.DefectStatisticsSources | CHECK | CK_DefectStatisticsSources_Facts | {"name": "CK_DefectStatisticsSources_Facts", "sql": "ISJSON([SegmentIdsJson])=1 AND ISJSON([QuantitiesJson])=1 AND ISJSON([SourceFactsJson])=1 AND [Provenance] COLLATE Latin1_General_100_BIN2 IN ('REAL_SOURCE','TEST_ONLY')"} | RoadGuardSystem.BusinessObjects.Reporting.DefectStatisticsSource |
| dbo.DefectTypes | KEY | PK_DefectTypes | {"name": "PK_DefectTypes", "columns": ["Code"], "primary": true} | RoadGuardSystem.BusinessObjects.Catalogs.DefectType |
| dbo.DefectVerificationLogs | KEY | PK_DefectVerificationLogs | {"name": "PK_DefectVerificationLogs", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Defects.DefectVerificationLog |
| dbo.DefectVerificationLogs | INDEX | IX_DefectVerificationLogs_AIDetectionId | {"name": "IX_DefectVerificationLogs_AIDetectionId", "columns": ["AIDetectionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Defects.DefectVerificationLog |
| dbo.DefectVerificationLogs | INDEX | IX_DefectVerificationLogs_DefectId | {"name": "IX_DefectVerificationLogs_DefectId", "columns": ["DefectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Defects.DefectVerificationLog |
| dbo.DefectVerificationLogs | INDEX | IX_DefectVerificationLogs_FieldInspectionTaskId | {"name": "IX_DefectVerificationLogs_FieldInspectionTaskId", "columns": ["FieldInspectionTaskId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Defects.DefectVerificationLog |
| dbo.DefectVerificationLogs | INDEX | IX_DefectVerificationLogs_SeverityRuleVersionId | {"name": "IX_DefectVerificationLogs_SeverityRuleVersionId", "columns": ["SeverityRuleVersionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Defects.DefectVerificationLog |
| dbo.DefectVerificationLogs | INDEX | IX_DefectVerificationLogs_VerifiedByUserId | {"name": "IX_DefectVerificationLogs_VerifiedByUserId", "columns": ["VerifiedByUserId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Defects.DefectVerificationLog |
| dbo.DefectVerificationLogs | CHECK | CK_DefectVerificationLogs_Action | {"name": "CK_DefectVerificationLogs_Action", "sql": "[Action] IN (1, 2, 3, 4, 5)"} | RoadGuardSystem.BusinessObjects.Defects.DefectVerificationLog |
| dbo.DefectVerificationLogs | CHECK | CK_DefectVerificationLogs_AfterSnapshot_Json | {"name": "CK_DefectVerificationLogs_AfterSnapshot_Json", "sql": "[AfterSnapshot] IS NULL OR ISJSON([AfterSnapshot]) = 1"} | RoadGuardSystem.BusinessObjects.Defects.DefectVerificationLog |
| dbo.DefectVerificationLogs | CHECK | CK_DefectVerificationLogs_BeforeSnapshot_Json | {"name": "CK_DefectVerificationLogs_BeforeSnapshot_Json", "sql": "[BeforeSnapshot] IS NULL OR ISJSON([BeforeSnapshot]) = 1"} | RoadGuardSystem.BusinessObjects.Defects.DefectVerificationLog |
| dbo.DefectVerificationLogs | CHECK | CK_DefectVerificationLogs_ExactlyOneTarget | {"name": "CK_DefectVerificationLogs_ExactlyOneTarget", "sql": "([DefectId] IS NOT NULL AND [AIDetectionId] IS NULL) OR ([DefectId] IS NULL AND [AIDetectionId] IS NOT NULL)"} | RoadGuardSystem.BusinessObjects.Defects.DefectVerificationLog |
| dbo.DerivedMeasurements | KEY | PK_DerivedMeasurements | {"name": "PK_DerivedMeasurements", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Processing.DerivedMeasurement |
| dbo.DerivedMeasurements | INDEX | IX_DerivedMeasurements_RoadSectionVersionId | {"name": "IX_DerivedMeasurements_RoadSectionVersionId", "columns": ["RoadSectionVersionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Processing.DerivedMeasurement |
| dbo.DerivedMeasurements | INDEX | IX_DerivedMeasurements_DataSampleTypeStatus | {"name": "IX_DerivedMeasurements_DataSampleTypeStatus", "columns": ["SurveyDataVersionId", "SampleId", "MeasurementType", "Status"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Processing.DerivedMeasurement |
| dbo.DerivedMeasurements | CHECK | CK_DerivedMeasurements_MeasurementType | {"name": "CK_DerivedMeasurements_MeasurementType", "sql": "[MeasurementType] IN (1, 2, 3)"} | RoadGuardSystem.BusinessObjects.Processing.DerivedMeasurement |
| dbo.DerivedMeasurements | CHECK | CK_DerivedMeasurements_SourceType | {"name": "CK_DerivedMeasurements_SourceType", "sql": "[SourceType] IN (1, 2, 3, 4)"} | RoadGuardSystem.BusinessObjects.Processing.DerivedMeasurement |
| dbo.DerivedMeasurements | CHECK | CK_DerivedMeasurements_Status | {"name": "CK_DerivedMeasurements_Status", "sql": "[Status] IN (1, 2, 3)"} | RoadGuardSystem.BusinessObjects.Processing.DerivedMeasurement |
| dbo.DerivedMeasurements | CHECK | CK_DerivedMeasurements_UncertaintyEstimate | {"name": "CK_DerivedMeasurements_UncertaintyEstimate", "sql": "[UncertaintyEstimate] IS NULL OR [UncertaintyEstimate] >= 0"} | RoadGuardSystem.BusinessObjects.Processing.DerivedMeasurement |
| dbo.DerivedMeasurements | CHECK | CK_DerivedMeasurements_Unit | {"name": "CK_DerivedMeasurements_Unit", "sql": "LOWER([Unit]) IN ('mm', 'cm', 'm')"} | RoadGuardSystem.BusinessObjects.Processing.DerivedMeasurement |
| dbo.DerivedMeasurements | CHECK | CK_DerivedMeasurements_Value | {"name": "CK_DerivedMeasurements_Value", "sql": "[Value] >= 0"} | RoadGuardSystem.BusinessObjects.Processing.DerivedMeasurement |
| dbo.DroneDevices | KEY | PK_DroneDevices | {"name": "PK_DroneDevices", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Devices.DroneDevice |
| dbo.DroneDevices | INDEX | UX_DroneDevices_SerialNo | {"name": "UX_DroneDevices_SerialNo", "columns": ["SerialNo"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Devices.DroneDevice |
| dbo.DroneDevices | CHECK | CK_DroneDevices_Status | {"name": "CK_DroneDevices_Status", "sql": "[Status] IN (1, 2, 3)"} | RoadGuardSystem.BusinessObjects.Devices.DroneDevice |
| dbo.FieldInspectionAssignments | KEY | PK_FieldInspectionAssignments | {"name": "PK_FieldInspectionAssignments", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionAssignment |
| dbo.FieldInspectionAssignments | INDEX | IX_FieldInspectionAssignments_AssignedByUserId | {"name": "IX_FieldInspectionAssignments_AssignedByUserId", "columns": ["AssignedByUserId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionAssignment |
| dbo.FieldInspectionAssignments | INDEX | IX_FieldInspectionAssignments_AssignedToUserId | {"name": "IX_FieldInspectionAssignments_AssignedToUserId", "columns": ["AssignedToUserId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionAssignment |
| dbo.FieldInspectionAssignments | INDEX | UX_FieldInspectionAssignments_ActiveTask | {"name": "UX_FieldInspectionAssignments_ActiveTask", "columns": ["FieldInspectionTaskId"], "unique": true, "filter": "[Status] = 1"} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionAssignment |
| dbo.FieldInspectionAssignments | CHECK | CK_FieldInspectionAssignments_State | {"name": "CK_FieldInspectionAssignments_State", "sql": "([Status] = 1 AND [EndedAt] IS NULL AND [Reason] IS NULL) OR ([Status] IN (2, 3) AND [EndedAt] IS NOT NULL AND LEN(LTRIM(RTRIM([Reason]))) > 0)"} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionAssignment |
| dbo.FieldInspectionAssignments | CHECK | CK_FieldInspectionAssignments_Status | {"name": "CK_FieldInspectionAssignments_Status", "sql": "[Status] IN (1, 2, 3)"} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionAssignment |
| dbo.FieldInspectionAssignments | CHECK | CK_FieldInspectionAssignments_TimestampOrder | {"name": "CK_FieldInspectionAssignments_TimestampOrder", "sql": "[EndedAt] IS NULL OR [EndedAt] >= [AssignedAt]"} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionAssignment |
| dbo.FieldInspectionEvidenceLinks | KEY | PK_FieldInspectionEvidenceLinks | {"name": "PK_FieldInspectionEvidenceLinks", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionEvidenceLink |
| dbo.FieldInspectionEvidenceLinks | INDEX | IX_FieldInspectionEvidenceLinks_AssignmentId | {"name": "IX_FieldInspectionEvidenceLinks_AssignmentId", "columns": ["AssignmentId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionEvidenceLink |
| dbo.FieldInspectionEvidenceLinks | INDEX | IX_FieldInspectionEvidenceLinks_FileId | {"name": "IX_FieldInspectionEvidenceLinks_FileId", "columns": ["FileId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionEvidenceLink |
| dbo.FieldInspectionEvidenceLinks | INDEX | IX_FieldInspectionEvidenceLinks_ProjectId | {"name": "IX_FieldInspectionEvidenceLinks_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionEvidenceLink |
| dbo.FieldInspectionEvidenceLinks | INDEX | IX_FieldInspectionEvidenceLinks_TaskId | {"name": "IX_FieldInspectionEvidenceLinks_TaskId", "columns": ["TaskId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionEvidenceLink |
| dbo.FieldInspectionEvidenceLinks | INDEX | IX_FieldInspectionEvidenceLinks_SubmissionId_CaptureOriginId | {"name": "IX_FieldInspectionEvidenceLinks_SubmissionId_CaptureOriginId", "columns": ["SubmissionId", "CaptureOriginId"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionEvidenceLink |
| dbo.FieldInspectionEvidenceReuseDecisions | KEY | PK_FieldInspectionEvidenceReuseDecisions | {"name": "PK_FieldInspectionEvidenceReuseDecisions", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionEvidenceReuseDecision |
| dbo.FieldInspectionEvidenceReuseDecisions | INDEX | IX_FieldInspectionEvidenceReuseDecisions_ActorId | {"name": "IX_FieldInspectionEvidenceReuseDecisions_ActorId", "columns": ["ActorId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionEvidenceReuseDecision |
| dbo.FieldInspectionEvidenceReuseDecisions | INDEX | IX_FieldInspectionEvidenceReuseDecisions_FileId | {"name": "IX_FieldInspectionEvidenceReuseDecisions_FileId", "columns": ["FileId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionEvidenceReuseDecision |
| dbo.FieldInspectionEvidenceReuseDecisions | INDEX | IX_FieldInspectionEvidenceReuseDecisions_ProjectId | {"name": "IX_FieldInspectionEvidenceReuseDecisions_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionEvidenceReuseDecision |
| dbo.FieldInspectionEvidenceReuseDecisions | INDEX | IX_FieldInspectionEvidenceReuseDecisions_TaskId | {"name": "IX_FieldInspectionEvidenceReuseDecisions_TaskId", "columns": ["TaskId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionEvidenceReuseDecision |
| dbo.FieldInspectionLocationProofs | KEY | PK_FieldInspectionLocationProofs | {"name": "PK_FieldInspectionLocationProofs", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionLocationProof |
| dbo.FieldInspectionLocationProofs | INDEX | IX_FieldInspectionLocationProofs_ProjectId | {"name": "IX_FieldInspectionLocationProofs_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionLocationProof |
| dbo.FieldInspectionLocationProofs | INDEX | IX_FieldInspectionLocationProofs_SubmissionId | {"name": "IX_FieldInspectionLocationProofs_SubmissionId", "columns": ["SubmissionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionLocationProof |
| dbo.FieldInspectionLocationProofs | INDEX | IX_FieldInspectionLocationProofs_TaskId | {"name": "IX_FieldInspectionLocationProofs_TaskId", "columns": ["TaskId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionLocationProof |
| dbo.FieldInspectionOperationOrigins | KEY | PK_FieldInspectionOperationOrigins | {"name": "PK_FieldInspectionOperationOrigins", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionOperationOrigin |
| dbo.FieldInspectionOperationOrigins | INDEX | IX_FieldInspectionOperationOrigins_OriginalActorId | {"name": "IX_FieldInspectionOperationOrigins_OriginalActorId", "columns": ["OriginalActorId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionOperationOrigin |
| dbo.FieldInspectionOperationOrigins | INDEX | IX_FieldInspectionOperationOrigins_TaskId | {"name": "IX_FieldInspectionOperationOrigins_TaskId", "columns": ["TaskId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionOperationOrigin |
| dbo.FieldInspectionOperationOrigins | INDEX | IX_FieldInspectionOperationOrigins_ProjectId_OriginId | {"name": "IX_FieldInspectionOperationOrigins_ProjectId_OriginId", "columns": ["ProjectId", "OriginId"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionOperationOrigin |
| dbo.FieldInspectionOperationOrigins | CHECK | CK_FieldInspectionOperationOrigins_Kind | {"name": "CK_FieldInspectionOperationOrigins_Kind", "sql": "[Kind] IN ('FIELD_START','FIELD_SUBMISSION','FIELD_ACCEPT','REPAIR_ASSESSMENT','REPAIR_EXECUTION_START','REPAIR_EXECUTION_FINISH') AND [SchemaVersion]=1"} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionOperationOrigin |
| dbo.FieldInspectionReviews | KEY | PK_FieldInspectionReviews | {"name": "PK_FieldInspectionReviews", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionReview |
| dbo.FieldInspectionReviews | INDEX | IX_FieldInspectionReviews_ActorId | {"name": "IX_FieldInspectionReviews_ActorId", "columns": ["ActorId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionReview |
| dbo.FieldInspectionReviews | INDEX | IX_FieldInspectionReviews_ProjectId | {"name": "IX_FieldInspectionReviews_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionReview |
| dbo.FieldInspectionReviews | INDEX | IX_FieldInspectionReviews_SubmissionId | {"name": "IX_FieldInspectionReviews_SubmissionId", "columns": ["SubmissionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionReview |
| dbo.FieldInspectionReviews | INDEX | IX_FieldInspectionReviews_TaskId | {"name": "IX_FieldInspectionReviews_TaskId", "columns": ["TaskId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionReview |
| dbo.FieldInspectionSessions | KEY | PK_FieldInspectionSessions | {"name": "PK_FieldInspectionSessions", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionSession |
| dbo.FieldInspectionSessions | INDEX | IX_FieldInspectionSessions_EvidenceFileId | {"name": "IX_FieldInspectionSessions_EvidenceFileId", "columns": ["EvidenceFileId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionSession |
| dbo.FieldInspectionSessions | INDEX | IX_FieldInspectionSessions_FieldInspectionTaskId | {"name": "IX_FieldInspectionSessions_FieldInspectionTaskId", "columns": ["FieldInspectionTaskId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionSession |
| dbo.FieldInspectionSessions | INDEX | IX_FieldInspectionSessions_InspectorUserId | {"name": "IX_FieldInspectionSessions_InspectorUserId", "columns": ["InspectorUserId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionSession |
| dbo.FieldInspectionSessions | INDEX | IX_FieldInspectionSessions_RoadSectionVersionId | {"name": "IX_FieldInspectionSessions_RoadSectionVersionId", "columns": ["RoadSectionVersionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionSession |
| dbo.FieldInspectionSessions | INDEX | UX_FieldInspectionSessions_SessionCode | {"name": "UX_FieldInspectionSessions_SessionCode", "columns": ["SessionCode"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionSession |
| dbo.FieldInspectionSessions | INDEX | IX_FieldInspectionSessions_SurveyId | {"name": "IX_FieldInspectionSessions_SurveyId", "columns": ["SurveyId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionSession |
| dbo.FieldInspectionSessions | INDEX | IX_FieldInspectionSessions_ProjectVersion | {"name": "IX_FieldInspectionSessions_ProjectVersion", "columns": ["ProjectId", "RoadSectionVersionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionSession |
| dbo.FieldInspectionSessions | CHECK | CK_FieldInspectionSessions_Purpose | {"name": "CK_FieldInspectionSessions_Purpose", "sql": "[Purpose] IN (1, 2, 3, 4, 5)"} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionSession |
| dbo.FieldInspectionSessions | CHECK | CK_FieldInspectionSessions_PurposeScope | {"name": "CK_FieldInspectionSessions_PurposeScope", "sql": "([Purpose] = 1 AND [FieldInspectionTaskId] IS NOT NULL AND [SurveyId] IS NOT NULL AND [InspectorUserId] IS NOT NULL) OR ([Purpose] = 2 AND [FieldInspectionTaskId] IS NULL) OR ([Purpose] IN (3,4,5) AND [FieldInspectionTaskId] IS NOT NULL AND [InspectorUserId] IS NOT NULL)"} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionSession |
| dbo.FieldInspectionSessions | CHECK | CK_FieldInspectionSessions_Status | {"name": "CK_FieldInspectionSessions_Status", "sql": "[Status] IN (1, 2, 3, 4)"} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionSession |
| dbo.FieldInspectionSubmissions | KEY | PK_FieldInspectionSubmissions | {"name": "PK_FieldInspectionSubmissions", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionSubmission |
| dbo.FieldInspectionSubmissions | INDEX | IX_FieldInspectionSubmissions_AssignmentId | {"name": "IX_FieldInspectionSubmissions_AssignmentId", "columns": ["AssignmentId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionSubmission |
| dbo.FieldInspectionSubmissions | INDEX | IX_FieldInspectionSubmissions_OperationOriginId | {"name": "IX_FieldInspectionSubmissions_OperationOriginId", "columns": ["OperationOriginId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionSubmission |
| dbo.FieldInspectionSubmissions | INDEX | IX_FieldInspectionSubmissions_OriginalActorId | {"name": "IX_FieldInspectionSubmissions_OriginalActorId", "columns": ["OriginalActorId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionSubmission |
| dbo.FieldInspectionSubmissions | INDEX | IX_FieldInspectionSubmissions_ParentId | {"name": "IX_FieldInspectionSubmissions_ParentId", "columns": ["ParentId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionSubmission |
| dbo.FieldInspectionSubmissions | INDEX | UX_FieldInspectionSubmissions_Session | {"name": "UX_FieldInspectionSubmissions_Session", "columns": ["SessionId"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionSubmission |
| dbo.FieldInspectionSubmissions | INDEX | IX_FieldInspectionSubmissions_StartOriginId | {"name": "IX_FieldInspectionSubmissions_StartOriginId", "columns": ["StartOriginId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionSubmission |
| dbo.FieldInspectionSubmissions | INDEX | UX_FieldInspectionSubmissions_TaskRoot | {"name": "UX_FieldInspectionSubmissions_TaskRoot", "columns": ["TaskId"], "unique": true, "filter": "[Revision] = 1"} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionSubmission |
| dbo.FieldInspectionSubmissions | INDEX | IX_FieldInspectionSubmissions_ProjectId_OriginId | {"name": "IX_FieldInspectionSubmissions_ProjectId_OriginId", "columns": ["ProjectId", "OriginId"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionSubmission |
| dbo.FieldInspectionSubmissions | INDEX | IX_FieldInspectionSubmissions_RootId_Revision | {"name": "IX_FieldInspectionSubmissions_RootId_Revision", "columns": ["RootId", "Revision"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionSubmission |
| dbo.FieldInspectionTaskEvents | KEY | PK_FieldInspectionTaskEvents | {"name": "PK_FieldInspectionTaskEvents", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionTaskEvent |
| dbo.FieldInspectionTaskEvents | INDEX | IX_FieldInspectionTaskEvents_ActorId | {"name": "IX_FieldInspectionTaskEvents_ActorId", "columns": ["ActorId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionTaskEvent |
| dbo.FieldInspectionTaskEvents | INDEX | IX_FieldInspectionTaskEvents_AssignmentId | {"name": "IX_FieldInspectionTaskEvents_AssignmentId", "columns": ["AssignmentId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionTaskEvent |
| dbo.FieldInspectionTaskEvents | INDEX | IX_FieldInspectionTaskEvents_LocationImpactDecisionId | {"name": "IX_FieldInspectionTaskEvents_LocationImpactDecisionId", "columns": ["LocationImpactDecisionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionTaskEvent |
| dbo.FieldInspectionTaskEvents | INDEX | IX_FieldInspectionTaskEvents_LocationImpactId | {"name": "IX_FieldInspectionTaskEvents_LocationImpactId", "columns": ["LocationImpactId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionTaskEvent |
| dbo.FieldInspectionTaskEvents | INDEX | IX_FieldInspectionTaskEvents_ProjectId | {"name": "IX_FieldInspectionTaskEvents_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionTaskEvent |
| dbo.FieldInspectionTaskEvents | INDEX | IX_FieldInspectionTaskEvents_TaskId | {"name": "IX_FieldInspectionTaskEvents_TaskId", "columns": ["TaskId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionTaskEvent |
| dbo.FieldInspectionTasks | KEY | PK_FieldInspectionTasks | {"name": "PK_FieldInspectionTasks", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionTask |
| dbo.FieldInspectionTasks | INDEX | IX_FieldInspectionTasks_AssignedByUserId | {"name": "IX_FieldInspectionTasks_AssignedByUserId", "columns": ["AssignedByUserId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionTask |
| dbo.FieldInspectionTasks | INDEX | IX_FieldInspectionTasks_CrsProfileRevisionId | {"name": "IX_FieldInspectionTasks_CrsProfileRevisionId", "columns": ["CrsProfileRevisionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionTask |
| dbo.FieldInspectionTasks | INDEX | IX_FieldInspectionTasks_DefectId | {"name": "IX_FieldInspectionTasks_DefectId", "columns": ["DefectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionTask |
| dbo.FieldInspectionTasks | INDEX | IX_FieldInspectionTasks_LayoutRevisionId | {"name": "IX_FieldInspectionTasks_LayoutRevisionId", "columns": ["LayoutRevisionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionTask |
| dbo.FieldInspectionTasks | INDEX | IX_FieldInspectionTasks_MapPublicationId | {"name": "IX_FieldInspectionTasks_MapPublicationId", "columns": ["MapPublicationId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionTask |
| dbo.FieldInspectionTasks | INDEX | IX_FieldInspectionTasks_ProjectId | {"name": "IX_FieldInspectionTasks_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionTask |
| dbo.FieldInspectionTasks | INDEX | IX_FieldInspectionTasks_RepairItemId | {"name": "IX_FieldInspectionTasks_RepairItemId", "columns": ["RepairItemId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionTask |
| dbo.FieldInspectionTasks | INDEX | IX_FieldInspectionTasks_ReviewedByUserId | {"name": "IX_FieldInspectionTasks_ReviewedByUserId", "columns": ["ReviewedByUserId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionTask |
| dbo.FieldInspectionTasks | INDEX | IX_FieldInspectionTasks_RoadSectionVersionId | {"name": "IX_FieldInspectionTasks_RoadSectionVersionId", "columns": ["RoadSectionVersionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionTask |
| dbo.FieldInspectionTasks | INDEX | IX_FieldInspectionTasks_SegmentSetId | {"name": "IX_FieldInspectionTasks_SegmentSetId", "columns": ["SegmentSetId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionTask |
| dbo.FieldInspectionTasks | INDEX | IX_FieldInspectionTasks_SurveyId | {"name": "IX_FieldInspectionTasks_SurveyId", "columns": ["SurveyId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionTask |
| dbo.FieldInspectionTasks | INDEX | UX_FieldInspectionTasks_TaskCode | {"name": "UX_FieldInspectionTasks_TaskCode", "columns": ["TaskCode"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionTask |
| dbo.FieldInspectionTasks | CHECK | CK_FieldInspectionTasks_MeasurementScope_Json | {"name": "CK_FieldInspectionTasks_MeasurementScope_Json", "sql": "ISJSON([MeasurementScope]) = 1 AND LEFT(LTRIM([MeasurementScope]), 1) = '{'"} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionTask |
| dbo.FieldInspectionTasks | CHECK | CK_FieldInspectionTasks_RepairMode | {"name": "CK_FieldInspectionTasks_RepairMode", "sql": "([TaskMode]='MEASURE_ONLY' AND [RepairItemId] IS NULL) OR ([LifecycleVersion]=2 AND [TaskMode] IN('NORMAL','CONDITIONAL_FT') AND [RepairItemId] IS NOT NULL)"} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionTask |
| dbo.FieldInspectionTasks | CHECK | CK_FieldInspectionTasks_ReviewDecision | {"name": "CK_FieldInspectionTasks_ReviewDecision", "sql": "([ReviewDecision] IS NULL AND [ReviewedByUserId] IS NULL AND [ReviewedAt] IS NULL) OR ([ReviewDecision] IS NOT NULL AND [ReviewedByUserId] IS NOT NULL AND [ReviewedAt] IS NOT NULL AND [Status] = 7)"} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionTask |
| dbo.FieldInspectionTasks | CHECK | CK_FieldInspectionTasks_Source | {"name": "CK_FieldInspectionTasks_Source", "sql": "([LifecycleVersion]=1 AND [SourceKind]='SURVEY' AND [SurveyId] IS NOT NULL) OR ([LifecycleVersion]=2 AND [Purpose] IN(3,4,5) AND (([SourceKind]='SURVEY' AND [SurveyId] IS NOT NULL) OR ([SourceKind]='REPORTER' AND [SurveyId] IS NULL)))"} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionTask |
| dbo.FieldInspectionTasks | CHECK | CK_FieldInspectionTasks_Status | {"name": "CK_FieldInspectionTasks_Status", "sql": "[Status] IN (1, 2, 3, 4, 5, 6, 7, 8)"} | RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionTask |
| dbo.FieldTaskStartOrigins | KEY | PK_FieldTaskStartOrigins | {"name": "PK_FieldTaskStartOrigins", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Inspections.FieldTaskStartOrigin |
| dbo.FieldTaskStartOrigins | INDEX | IX_FieldTaskStartOrigins_AssignmentId | {"name": "IX_FieldTaskStartOrigins_AssignmentId", "columns": ["AssignmentId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldTaskStartOrigin |
| dbo.FieldTaskStartOrigins | INDEX | IX_FieldTaskStartOrigins_CrsProfileRevisionId | {"name": "IX_FieldTaskStartOrigins_CrsProfileRevisionId", "columns": ["CrsProfileRevisionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldTaskStartOrigin |
| dbo.FieldTaskStartOrigins | INDEX | IX_FieldTaskStartOrigins_LayoutRevisionId | {"name": "IX_FieldTaskStartOrigins_LayoutRevisionId", "columns": ["LayoutRevisionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldTaskStartOrigin |
| dbo.FieldTaskStartOrigins | INDEX | IX_FieldTaskStartOrigins_MapPublicationId | {"name": "IX_FieldTaskStartOrigins_MapPublicationId", "columns": ["MapPublicationId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldTaskStartOrigin |
| dbo.FieldTaskStartOrigins | INDEX | IX_FieldTaskStartOrigins_OperationOriginId | {"name": "IX_FieldTaskStartOrigins_OperationOriginId", "columns": ["OperationOriginId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldTaskStartOrigin |
| dbo.FieldTaskStartOrigins | INDEX | IX_FieldTaskStartOrigins_OriginalActorId | {"name": "IX_FieldTaskStartOrigins_OriginalActorId", "columns": ["OriginalActorId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldTaskStartOrigin |
| dbo.FieldTaskStartOrigins | INDEX | IX_FieldTaskStartOrigins_RouteVersionId | {"name": "IX_FieldTaskStartOrigins_RouteVersionId", "columns": ["RouteVersionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldTaskStartOrigin |
| dbo.FieldTaskStartOrigins | INDEX | IX_FieldTaskStartOrigins_SegmentSetId | {"name": "IX_FieldTaskStartOrigins_SegmentSetId", "columns": ["SegmentSetId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldTaskStartOrigin |
| dbo.FieldTaskStartOrigins | INDEX | IX_FieldTaskStartOrigins_TaskId | {"name": "IX_FieldTaskStartOrigins_TaskId", "columns": ["TaskId"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldTaskStartOrigin |
| dbo.FieldTaskStartOrigins | INDEX | IX_FieldTaskStartOrigins_ProjectId_OriginId | {"name": "IX_FieldTaskStartOrigins_ProjectId_OriginId", "columns": ["ProjectId", "OriginId"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.FieldTaskStartOrigin |
| dbo.Files | KEY | PK_Files | {"name": "PK_Files", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Files.StoredFile |
| dbo.Files | INDEX | UX_Files_StorageUri | {"name": "UX_Files_StorageUri", "columns": ["StorageUri"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Files.StoredFile |
| dbo.Files | INDEX | IX_Files_UploadedByUserId | {"name": "IX_Files_UploadedByUserId", "columns": ["UploadedByUserId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Files.StoredFile |
| dbo.Files | CHECK | CK_Files_Checksum_Sha256Lowercase | {"name": "CK_Files_Checksum_Sha256Lowercase", "sql": "LEN([Checksum]) = 64 AND [Checksum] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'"} | RoadGuardSystem.BusinessObjects.Files.StoredFile |
| dbo.Files | CHECK | CK_Files_SizeBytes_NonNegative | {"name": "CK_Files_SizeBytes_NonNegative", "sql": "[SizeBytes] >= 0"} | RoadGuardSystem.BusinessObjects.Files.StoredFile |
| dbo.FileScopes | KEY | PK_FileScopes | {"name": "PK_FileScopes", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Files.FileScope |
| dbo.FileScopes | INDEX | UX_FileScopes_FileId | {"name": "UX_FileScopes_FileId", "columns": ["FileId"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Files.FileScope |
| dbo.FileScopes | INDEX | IX_FileScopes_OwnerUserId | {"name": "IX_FileScopes_OwnerUserId", "columns": ["OwnerUserId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Files.FileScope |
| dbo.FileScopes | INDEX | IX_FileScopes_ProjectId_OwnerUserId | {"name": "IX_FileScopes_ProjectId_OwnerUserId", "columns": ["ProjectId", "OwnerUserId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Files.FileScope |
| dbo.FileScopes | CHECK | CK_FileScopes_PrivateShape | {"name": "CK_FileScopes_PrivateShape", "sql": "[ProjectId] IS NOT NULL OR ([Purpose] = 'REPORT_PHOTO' AND [TargetId] IS NULL)"} | RoadGuardSystem.BusinessObjects.Files.FileScope |
| dbo.Flights | KEY | PK_Flights | {"name": "PK_Flights", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Surveys.Flight |
| dbo.Flights | INDEX | IX_Flights_DroneDeviceId | {"name": "IX_Flights_DroneDeviceId", "columns": ["DroneDeviceId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Surveys.Flight |
| dbo.Flights | INDEX | IX_Flights_OperatorUserId | {"name": "IX_Flights_OperatorUserId", "columns": ["OperatorUserId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Surveys.Flight |
| dbo.Flights | INDEX | UX_Flights_SurveyFlightNo | {"name": "UX_Flights_SurveyFlightNo", "columns": ["SurveyId", "FlightNo"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Surveys.Flight |
| dbo.Flights | CHECK | CK_Flights_TimestampOrder | {"name": "CK_Flights_TimestampOrder", "sql": "[EndedAt] IS NULL OR [EndedAt] >= [StartedAt]"} | RoadGuardSystem.BusinessObjects.Surveys.Flight |
| dbo.GeometryLocationImpactDecisions | KEY | PK_GeometryLocationImpactDecisions | {"name": "PK_GeometryLocationImpactDecisions", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Projects.GeometryLocationImpactDecision |
| dbo.GeometryLocationImpactDecisions | INDEX | IX_GeometryLocationImpactDecisions_ActorId | {"name": "IX_GeometryLocationImpactDecisions_ActorId", "columns": ["ActorId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.GeometryLocationImpactDecision |
| dbo.GeometryLocationImpactDecisions | INDEX | IX_GeometryLocationImpactDecisions_ImpactId_TaskId | {"name": "IX_GeometryLocationImpactDecisions_ImpactId_TaskId", "columns": ["ImpactId", "TaskId"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.GeometryLocationImpactDecision |
| dbo.GeometryLocationImpactDecisions | CHECK | CK_GeometryLocationImpactDecisions_Action | {"name": "CK_GeometryLocationImpactDecisions_Action", "sql": "[Action] IN ('VERIFY','CONTINUE','STOP','REASSIGN')"} | RoadGuardSystem.BusinessObjects.Projects.GeometryLocationImpactDecision |
| dbo.GeometryLocationImpacts | KEY | PK_GeometryLocationImpacts | {"name": "PK_GeometryLocationImpacts", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Projects.GeometryLocationImpact |
| dbo.GeometryLocationImpacts | INDEX | IX_GeometryLocationImpacts_NewRouteVersionId | {"name": "IX_GeometryLocationImpacts_NewRouteVersionId", "columns": ["NewRouteVersionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.GeometryLocationImpact |
| dbo.GeometryLocationImpacts | INDEX | IX_GeometryLocationImpacts_ProjectId | {"name": "IX_GeometryLocationImpacts_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.GeometryLocationImpact |
| dbo.GeometryLocationImpacts | INDEX | IX_GeometryLocationImpacts_RecordedBy | {"name": "IX_GeometryLocationImpacts_RecordedBy", "columns": ["RecordedBy"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.GeometryLocationImpact |
| dbo.GeometryLocationImpacts | INDEX | IX_GeometryLocationImpacts_PreviousRouteVersionId_NewRouteVersionId | {"name": "IX_GeometryLocationImpacts_PreviousRouteVersionId_NewRouteVersionId", "columns": ["PreviousRouteVersionId", "NewRouteVersionId"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.GeometryLocationImpact |
| dbo.GeometryLocationImpacts | CHECK | CK_GeometryLocationImpacts_Json | {"name": "CK_GeometryLocationImpacts_Json", "sql": "ISJSON([AffectedReferencesJson])=1"} | RoadGuardSystem.BusinessObjects.Projects.GeometryLocationImpact |
| dbo.GeometryLocationImpacts | CHECK | CK_GeometryLocationImpacts_Versions | {"name": "CK_GeometryLocationImpacts_Versions", "sql": "[PreviousRouteVersionId]<>[NewRouteVersionId]"} | RoadGuardSystem.BusinessObjects.Projects.GeometryLocationImpact |
| dbo.GeometryMapPublications | KEY | PK_GeometryMapPublications | {"name": "PK_GeometryMapPublications", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Projects.GeometryMapPublication |
| dbo.GeometryMapPublications | INDEX | IX_GeometryMapPublications_LayoutRevisionId | {"name": "IX_GeometryMapPublications_LayoutRevisionId", "columns": ["LayoutRevisionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.GeometryMapPublication |
| dbo.GeometryMapPublications | INDEX | IX_GeometryMapPublications_PublishedBy | {"name": "IX_GeometryMapPublications_PublishedBy", "columns": ["PublishedBy"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.GeometryMapPublication |
| dbo.GeometryMapPublications | INDEX | IX_GeometryMapPublications_RouteVersionId | {"name": "IX_GeometryMapPublications_RouteVersionId", "columns": ["RouteVersionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.GeometryMapPublication |
| dbo.GeometryMapPublications | INDEX | IX_GeometryMapPublications_SegmentSetId | {"name": "IX_GeometryMapPublications_SegmentSetId", "columns": ["SegmentSetId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.GeometryMapPublication |
| dbo.GeometryMapPublications | INDEX | IX_GeometryMapPublications_CrsProfileRevisionId_ProjectId | {"name": "IX_GeometryMapPublications_CrsProfileRevisionId_ProjectId", "columns": ["CrsProfileRevisionId", "ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.GeometryMapPublication |
| dbo.GeometryMapPublications | INDEX | IX_GeometryMapPublications_ProjectId_PublishedAt_Id | {"name": "IX_GeometryMapPublications_ProjectId_PublishedAt_Id", "columns": ["ProjectId", "PublishedAt", "Id"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.GeometryMapPublication |
| dbo.GeometryMapPublications | CHECK | CK_GeometryMapPublications_Json | {"name": "CK_GeometryMapPublications_Json", "sql": "ISJSON([SnapshotJson])=1"} | RoadGuardSystem.BusinessObjects.Projects.GeometryMapPublication |
| dbo.GeometryMapPublications | CHECK | CK_GeometryMapPublications_Mode | {"name": "CK_GeometryMapPublications_Mode", "sql": "[PublicationMode] IN ('SAMPLE','OFFICIAL')"} | RoadGuardSystem.BusinessObjects.Projects.GeometryMapPublication |
| dbo.GroundTruthMeasurements | KEY | PK_GroundTruthMeasurements | {"name": "PK_GroundTruthMeasurements", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Inspections.GroundTruthMeasurement |
| dbo.GroundTruthMeasurements | INDEX | IX_GroundTruthMeasurements_DefectId | {"name": "IX_GroundTruthMeasurements_DefectId", "columns": ["DefectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.GroundTruthMeasurement |
| dbo.GroundTruthMeasurements | INDEX | IX_GroundTruthMeasurements_EvidenceFileId | {"name": "IX_GroundTruthMeasurements_EvidenceFileId", "columns": ["EvidenceFileId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.GroundTruthMeasurement |
| dbo.GroundTruthMeasurements | INDEX | IX_GroundTruthMeasurements_SurveyId | {"name": "IX_GroundTruthMeasurements_SurveyId", "columns": ["SurveyId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.GroundTruthMeasurement |
| dbo.GroundTruthMeasurements | INDEX | UX_GroundTruthMeasurements_SessionSample | {"name": "UX_GroundTruthMeasurements_SessionSample", "columns": ["FieldInspectionSessionId", "SampleId"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.GroundTruthMeasurement |
| dbo.GroundTruthMeasurements | INDEX | IX_GroundTruthMeasurements_RoadVersionType | {"name": "IX_GroundTruthMeasurements_RoadVersionType", "columns": ["RoadSectionVersionId", "MeasurementType"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Inspections.GroundTruthMeasurement |
| dbo.GroundTruthMeasurements | CHECK | CK_GroundTruthMeasurements_EvidenceOrReason | {"name": "CK_GroundTruthMeasurements_EvidenceOrReason", "sql": "[EvidenceFileId] IS NOT NULL OR [ValueState]='UNKNOWN' OR [LocationState]='UNKNOWN' OR ([Notes] IS NOT NULL AND LEN(LTRIM(RTRIM([Notes]))) > 0)"} | RoadGuardSystem.BusinessObjects.Inspections.GroundTruthMeasurement |
| dbo.GroundTruthMeasurements | CHECK | CK_GroundTruthMeasurements_Location | {"name": "CK_GroundTruthMeasurements_Location", "sql": "([Location] IS NOT NULL AND [LocationState]='CAPTURED' AND [Location].STSrid=4326 AND [Location].STIsEmpty()=0) OR ([Location] IS NULL AND [LocationState]='UNKNOWN' AND [LocationReason] IS NOT NULL AND LEN(LTRIM(RTRIM([LocationReason])))>0)"} | RoadGuardSystem.BusinessObjects.Inspections.GroundTruthMeasurement |
| dbo.GroundTruthMeasurements | CHECK | CK_GroundTruthMeasurements_MeasurementType | {"name": "CK_GroundTruthMeasurements_MeasurementType", "sql": "[MeasurementType] IN (1, 2, 3, 4)"} | RoadGuardSystem.BusinessObjects.Inspections.GroundTruthMeasurement |
| dbo.GroundTruthMeasurements | CHECK | CK_GroundTruthMeasurements_Unit | {"name": "CK_GroundTruthMeasurements_Unit", "sql": "([MeasurementType] IN (1,2,3) AND [Dimension]='LENGTH' AND LOWER([Unit]) IN ('mm','cm','m')) OR ([MeasurementType]=4 AND [Dimension]='AREA' AND [Unit]=N'm²')"} | RoadGuardSystem.BusinessObjects.Inspections.GroundTruthMeasurement |
| dbo.GroundTruthMeasurements | CHECK | CK_GroundTruthMeasurements_Value | {"name": "CK_GroundTruthMeasurements_Value", "sql": "([ValueState]='KNOWN' AND [Value] IS NOT NULL AND [Value]>=0) OR ([ValueState]='UNKNOWN' AND [Value] IS NULL AND [UnknownReason] IS NOT NULL AND LEN(LTRIM(RTRIM([UnknownReason])))>0)"} | RoadGuardSystem.BusinessObjects.Inspections.GroundTruthMeasurement |
| dbo.H6NotificationAudits | KEY | PK_H6NotificationAudits | {"name": "PK_H6NotificationAudits", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Messaging.H6NotificationAuditRow |
| dbo.H6NotificationAudits | INDEX | IX_H6NotificationAudits_DedupKey | {"name": "IX_H6NotificationAudits_DedupKey", "columns": ["DedupKey"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Messaging.H6NotificationAuditRow |
| dbo.H6NotificationAudits | INDEX | IX_H6NotificationAudits_NotificationId | {"name": "IX_H6NotificationAudits_NotificationId", "columns": ["NotificationId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Messaging.H6NotificationAuditRow |
| dbo.H6NotificationAudits | INDEX | IX_H6NotificationAudits_OutboxMessageId | {"name": "IX_H6NotificationAudits_OutboxMessageId", "columns": ["OutboxMessageId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Messaging.H6NotificationAuditRow |
| dbo.H6NotificationAudits | INDEX | IX_H6NotificationAudits_PreviousAuditId | {"name": "IX_H6NotificationAudits_PreviousAuditId", "columns": ["PreviousAuditId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Messaging.H6NotificationAuditRow |
| dbo.H6NotificationAudits | INDEX | IX_H6NotificationAudits_ProjectId | {"name": "IX_H6NotificationAudits_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Messaging.H6NotificationAuditRow |
| dbo.H6NotificationCalendar | KEY | PK_H6NotificationCalendar | {"name": "PK_H6NotificationCalendar", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Messaging.H6NotificationCalendarRow |
| dbo.H6NotificationCalendar | INDEX | IX_H6NotificationCalendar_OutboxMessageId | {"name": "IX_H6NotificationCalendar_OutboxMessageId", "columns": ["OutboxMessageId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Messaging.H6NotificationCalendarRow |
| dbo.H6NotificationCalendar | INDEX | IX_H6NotificationCalendar_ClockId_ScheduledAtUtc | {"name": "IX_H6NotificationCalendar_ClockId_ScheduledAtUtc", "columns": ["ClockId", "ScheduledAtUtc"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Messaging.H6NotificationCalendarRow |
| dbo.H6NotificationDeliveries | KEY | PK_H6NotificationDeliveries | {"name": "PK_H6NotificationDeliveries", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Messaging.H6NotificationDeliveryRow |
| dbo.H6NotificationDeliveries | INDEX | IX_H6NotificationDeliveries_NotificationId | {"name": "IX_H6NotificationDeliveries_NotificationId", "columns": ["NotificationId"], "unique": true, "filter": "[NotificationId] IS NOT NULL"} | RoadGuardSystem.BusinessObjects.Messaging.H6NotificationDeliveryRow |
| dbo.H6NotificationDeliveries | INDEX | IX_H6NotificationDeliveries_RecipientUserId | {"name": "IX_H6NotificationDeliveries_RecipientUserId", "columns": ["RecipientUserId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Messaging.H6NotificationDeliveryRow |
| dbo.H6NotificationDeliveries | INDEX | IX_H6NotificationDeliveries_OccurrenceId_RecipientKey | {"name": "IX_H6NotificationDeliveries_OccurrenceId_RecipientKey", "columns": ["OccurrenceId", "RecipientKey"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Messaging.H6NotificationDeliveryRow |
| dbo.H6NotificationDeliveries | CHECK | CK_H6NotificationDeliveries_State | {"name": "CK_H6NotificationDeliveries_State", "sql": "[Status] IN ('PENDING','UNRESOLVED','DELIVERED') AND (([Status]='DELIVERED' AND [RecipientUserId] IS NOT NULL AND [NotificationId] IS NOT NULL AND [DeliveredAtUtc] IS NOT NULL AND [ReasonCode] IS NULL) OR ([Status]<>'DELIVERED' AND [NotificationId] IS NULL AND [DeliveredAtUtc] IS NULL))"} | RoadGuardSystem.BusinessObjects.Messaging.H6NotificationDeliveryRow |
| dbo.H6NotificationDeliveryAttempts | KEY | PK_H6NotificationDeliveryAttempts | {"name": "PK_H6NotificationDeliveryAttempts", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Messaging.H6NotificationDeliveryAttempt |
| dbo.H6NotificationDeliveryAttempts | INDEX | IX_H6NotificationDeliveryAttempts_DeliveryId | {"name": "IX_H6NotificationDeliveryAttempts_DeliveryId", "columns": ["DeliveryId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Messaging.H6NotificationDeliveryAttempt |
| dbo.H6NotificationEventReceipts | KEY | PK_H6NotificationEventReceipts | {"name": "PK_H6NotificationEventReceipts", "columns": ["OutboxMessageId"], "primary": true} | RoadGuardSystem.BusinessObjects.Messaging.H6NotificationEventReceipt |
| dbo.H6NotificationEventReceipts | INDEX | IX_H6NotificationEventReceipts_OccurrenceId | {"name": "IX_H6NotificationEventReceipts_OccurrenceId", "columns": ["OccurrenceId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Messaging.H6NotificationEventReceipt |
| dbo.H6NotificationOccurrences | KEY | PK_H6NotificationOccurrences | {"name": "PK_H6NotificationOccurrences", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Messaging.H6NotificationOccurrenceRow |
| dbo.H6NotificationOccurrences | INDEX | IX_H6NotificationOccurrences_OccurrenceKey | {"name": "IX_H6NotificationOccurrences_OccurrenceKey", "columns": ["OccurrenceKey"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Messaging.H6NotificationOccurrenceRow |
| dbo.H6NotificationOccurrences | INDEX | IX_H6NotificationOccurrences_ProjectId | {"name": "IX_H6NotificationOccurrences_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Messaging.H6NotificationOccurrenceRow |
| dbo.H6NotificationOccurrences | INDEX | IX_H6NotificationOccurrences_SourceEventId | {"name": "IX_H6NotificationOccurrences_SourceEventId", "columns": ["SourceEventId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Messaging.H6NotificationOccurrenceRow |
| dbo.H6NotificationOccurrences | CHECK | CK_H6NotificationOccurrences_Hashes | {"name": "CK_H6NotificationOccurrences_Hashes", "sql": "LEN([OccurrenceKey])=64 AND LEN([ContentFingerprint])=64"} | RoadGuardSystem.BusinessObjects.Messaging.H6NotificationOccurrenceRow |
| dbo.H6NotificationOccurrences | CHECK | CK_H6NotificationOccurrences_Json | {"name": "CK_H6NotificationOccurrences_Json", "sql": "ISJSON([PayloadJson])=1"} | RoadGuardSystem.BusinessObjects.Messaging.H6NotificationOccurrenceRow |
| dbo.H6NotificationScopes | KEY | PK_H6NotificationScopes | {"name": "PK_H6NotificationScopes", "columns": ["NotificationId"], "primary": true} | RoadGuardSystem.BusinessObjects.Messaging.H6NotificationScopeRow |
| dbo.H6NotificationScopes | INDEX | IX_H6NotificationScopes_CurrentAuditId | {"name": "IX_H6NotificationScopes_CurrentAuditId", "columns": ["CurrentAuditId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Messaging.H6NotificationScopeRow |
| dbo.H6NotificationScopes | INDEX | IX_H6NotificationScopes_OccurrenceId | {"name": "IX_H6NotificationScopes_OccurrenceId", "columns": ["OccurrenceId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Messaging.H6NotificationScopeRow |
| dbo.H6NotificationScopes | INDEX | IX_H6NotificationScopes_ProjectId | {"name": "IX_H6NotificationScopes_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Messaging.H6NotificationScopeRow |
| dbo.HandoverDocuments | KEY | PK_HandoverDocuments | {"name": "PK_HandoverDocuments", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Projects.HandoverDocument |
| dbo.HandoverDocuments | INDEX | IX_HandoverDocuments_AcceptedByUserId | {"name": "IX_HandoverDocuments_AcceptedByUserId", "columns": ["AcceptedByUserId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.HandoverDocument |
| dbo.HandoverDocuments | INDEX | IX_HandoverDocuments_FileId | {"name": "IX_HandoverDocuments_FileId", "columns": ["FileId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.HandoverDocument |
| dbo.HandoverDocuments | INDEX | UX_HandoverDocuments_ProjectId_DocumentNo | {"name": "UX_HandoverDocuments_ProjectId_DocumentNo", "columns": ["ProjectId", "DocumentNo"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.HandoverDocument |
| dbo.IdempotencyRecords | KEY | PK_IdempotencyRecords | {"name": "PK_IdempotencyRecords", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Idempotency.IdempotencyRecord |
| dbo.IdempotencyRecords | INDEX | IX_IdempotencyRecords_OperationId | {"name": "IX_IdempotencyRecords_OperationId", "columns": ["OperationId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Idempotency.IdempotencyRecord |
| dbo.IdempotencyRecords | INDEX | UX_IdempotencyRecords_ScopeKey | {"name": "UX_IdempotencyRecords_ScopeKey", "columns": ["ActorUserId", "ProjectId", "Operation", "IdempotencyKey"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Idempotency.IdempotencyRecord |
| dbo.IdempotencyRecords | CHECK | CK_IdempotencyRecords_OutcomeJson_Json | {"name": "CK_IdempotencyRecords_OutcomeJson_Json", "sql": "ISJSON([OutcomeJson]) = 1"} | RoadGuardSystem.BusinessObjects.Idempotency.IdempotencyRecord |
| dbo.IncidentCases | KEY | PK_IncidentCases | {"name": "PK_IncidentCases", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Cases.IncidentCase |
| dbo.IncidentCases | INDEX | IX_IncidentCases_LinkedTargetCaseId | {"name": "IX_IncidentCases_LinkedTargetCaseId", "columns": ["LinkedTargetCaseId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Cases.IncidentCase |
| dbo.IncidentCases | INDEX | IX_IncidentCases_ProjectId | {"name": "IX_IncidentCases_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Cases.IncidentCase |
| dbo.IncidentCases | INDEX | IX_IncidentCases_GeometrySegmentSetId_GeometryRouteVersionId | {"name": "IX_IncidentCases_GeometrySegmentSetId_GeometryRouteVersionId", "columns": ["GeometrySegmentSetId", "GeometryRouteVersionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Cases.IncidentCase |
| dbo.IncidentCases | CHECK | CK_IncidentCases_GeometryPair | {"name": "CK_IncidentCases_GeometryPair", "sql": "([GeometryRouteVersionId] IS NULL AND [GeometrySegmentSetId] IS NULL) OR ([GeometryRouteVersionId] IS NOT NULL AND [GeometrySegmentSetId] IS NOT NULL)"} | RoadGuardSystem.BusinessObjects.Cases.IncidentCase |
| dbo.LD06ActionEvidence | KEY | PK_LD06ActionEvidence | {"name": "PK_LD06ActionEvidence", "columns": ["ActionId", "FileId"], "primary": true} | RoadGuardSystem.BusinessObjects.Projects.LD06ActionEvidence |
| dbo.LD06ActionEvidence | INDEX | IX_LD06ActionEvidence_FileId | {"name": "IX_LD06ActionEvidence_FileId", "columns": ["FileId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.LD06ActionEvidence |
| dbo.LD06LifecycleActions | KEY | PK_LD06LifecycleActions | {"name": "PK_LD06LifecycleActions", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Projects.LD06LifecycleAction |
| dbo.LD06LifecycleActions | INDEX | IX_LD06LifecycleActions_ActorId | {"name": "IX_LD06LifecycleActions_ActorId", "columns": ["ActorId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.LD06LifecycleAction |
| dbo.LD06LifecycleActions | INDEX | IX_LD06LifecycleActions_DefectId | {"name": "IX_LD06LifecycleActions_DefectId", "columns": ["DefectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.LD06LifecycleAction |
| dbo.LD06LifecycleActions | INDEX | IX_LD06LifecycleActions_LinkedDefectId | {"name": "IX_LD06LifecycleActions_LinkedDefectId", "columns": ["LinkedDefectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.LD06LifecycleAction |
| dbo.LD06LifecycleActions | INDEX | IX_LD06LifecycleActions_ObligationId | {"name": "IX_LD06LifecycleActions_ObligationId", "columns": ["ObligationId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.LD06LifecycleAction |
| dbo.LD06LifecycleActions | INDEX | IX_LD06LifecycleActions_PriorRepairDecisionId | {"name": "IX_LD06LifecycleActions_PriorRepairDecisionId", "columns": ["PriorRepairDecisionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.LD06LifecycleAction |
| dbo.LD06LifecycleActions | INDEX | IX_LD06LifecycleActions_ProjectId | {"name": "IX_LD06LifecycleActions_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.LD06LifecycleAction |
| dbo.LD06LifecycleActions | INDEX | IX_LD06LifecycleActions_ReceivingProjectId | {"name": "IX_LD06LifecycleActions_ReceivingProjectId", "columns": ["ReceivingProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.LD06LifecycleAction |
| dbo.LD06LifecycleActions | INDEX | IX_LD06LifecycleActions_SourceActionId_Kind | {"name": "IX_LD06LifecycleActions_SourceActionId_Kind", "columns": ["SourceActionId", "Kind"], "unique": true, "filter": "[SourceActionId] IS NOT NULL AND [Kind] IN (2,7)"} | RoadGuardSystem.BusinessObjects.Projects.LD06LifecycleAction |
| dbo.LD06LifecycleActions | CHECK | CK_LD06LifecycleActions_Kind | {"name": "CK_LD06LifecycleActions_Kind", "sql": "[Kind] BETWEEN 1 AND 7 AND ISJSON([FactsJson])=1"} | RoadGuardSystem.BusinessObjects.Projects.LD06LifecycleAction |
| dbo.MeasurementValidationSamples | KEY | PK_MeasurementValidationSamples | {"name": "PK_MeasurementValidationSamples", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Processing.MeasurementValidationSample |
| dbo.MeasurementValidationSamples | INDEX | IX_MeasurementValidationSamples_DerivedMeasurementId | {"name": "IX_MeasurementValidationSamples_DerivedMeasurementId", "columns": ["DerivedMeasurementId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Processing.MeasurementValidationSample |
| dbo.MeasurementValidationSamples | INDEX | IX_MeasurementValidationSamples_GroundTruthMeasurementId | {"name": "IX_MeasurementValidationSamples_GroundTruthMeasurementId", "columns": ["GroundTruthMeasurementId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Processing.MeasurementValidationSample |
| dbo.MeasurementValidationSamples | INDEX | UX_MeasurementValidationSamples_RunPair | {"name": "UX_MeasurementValidationSamples_RunPair", "columns": ["ValidationRunId", "GroundTruthMeasurementId", "DerivedMeasurementId"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Processing.MeasurementValidationSample |
| dbo.MeasurementValidationSamples | CHECK | CK_MeasurementValidationSamples_InclusionStatus | {"name": "CK_MeasurementValidationSamples_InclusionStatus", "sql": "[InclusionStatus] IN (1, 2, 3)"} | RoadGuardSystem.BusinessObjects.Processing.MeasurementValidationSample |
| dbo.MeasurementValidationSamples | CHECK | CK_MeasurementValidationSamples_Reason | {"name": "CK_MeasurementValidationSamples_Reason", "sql": "[InclusionStatus] = 1 OR LEN(LTRIM(RTRIM([ExclusionReason]))) > 0"} | RoadGuardSystem.BusinessObjects.Processing.MeasurementValidationSample |
| dbo.NativeRouteVersionFacts | KEY | PK_NativeRouteVersionFacts | {"name": "PK_NativeRouteVersionFacts", "columns": ["RoadSectionVersionId"], "primary": true} | RoadGuardSystem.BusinessObjects.Projects.NativeRouteVersionFacts |
| dbo.NativeRouteVersionFacts | INDEX | IX_NativeRouteVersionFacts_ParentRouteVersionId | {"name": "IX_NativeRouteVersionFacts_ParentRouteVersionId", "columns": ["ParentRouteVersionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.NativeRouteVersionFacts |
| dbo.NativeRouteVersionFacts | INDEX | IX_NativeRouteVersionFacts_CrsProfileRevisionId_ProjectId | {"name": "IX_NativeRouteVersionFacts_CrsProfileRevisionId_ProjectId", "columns": ["CrsProfileRevisionId", "ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.NativeRouteVersionFacts |
| dbo.NativeRouteVersionFacts | INDEX | IX_NativeRouteVersionFacts_RouteSystemId_ProjectId | {"name": "IX_NativeRouteVersionFacts_RouteSystemId_ProjectId", "columns": ["RouteSystemId", "ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.NativeRouteVersionFacts |
| dbo.NativeRouteVersionFacts | CHECK | CK_NativeRouteFacts | {"name": "CK_NativeRouteFacts", "sql": "[CanonicalLengthMeters]>0 AND ([DeclaredLengthMeters] IS NULL OR [DeclaredLengthMeters]>0) AND (([RouteKind]='MAIN' AND [ParentRouteVersionId] IS NULL AND [JunctionOffsetMeters] IS NULL) OR ([RouteKind]='BRANCH' AND [ParentRouteVersionId] IS NOT NULL AND [JunctionOffsetMeters] IS NOT NULL AND [JunctionOffsetMeters]>=0)) AND ([CalibrationJson] IS NULL OR ISJSON([CalibrationJson])=1)"} | RoadGuardSystem.BusinessObjects.Projects.NativeRouteVersionFacts |
| dbo.Notifications | KEY | PK_Notifications | {"name": "PK_Notifications", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Messaging.Notification |
| dbo.Notifications | INDEX | IX_Notifications_RecipientUserId_ReadAt | {"name": "IX_Notifications_RecipientUserId_ReadAt", "columns": ["RecipientUserId", "ReadAt"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Messaging.Notification |
| dbo.Notifications | INDEX | IX_Notifications_RecipientOccurredAtId | {"name": "IX_Notifications_RecipientOccurredAtId", "columns": ["RecipientUserId", "OccurredAtUtc", "Id"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Messaging.Notification |
| dbo.Notifications | INDEX | UX_Notifications_RecipientSourceEvent | {"name": "UX_Notifications_RecipientSourceEvent", "columns": ["RecipientUserId", "SourceEntityType", "SourceEntityId", "EventType"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Messaging.Notification |
| dbo.ObligationResponsibilities | KEY | PK_ObligationResponsibilities | {"name": "PK_ObligationResponsibilities", "columns": ["ObligationId"], "primary": true} | RoadGuardSystem.BusinessObjects.Projects.ObligationResponsibility |
| dbo.ObligationResponsibilities | INDEX | IX_ObligationResponsibilities_AcceptanceActionId | {"name": "IX_ObligationResponsibilities_AcceptanceActionId", "columns": ["AcceptanceActionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.ObligationResponsibility |
| dbo.ObligationResponsibilities | INDEX | IX_ObligationResponsibilities_CurrentProjectId | {"name": "IX_ObligationResponsibilities_CurrentProjectId", "columns": ["CurrentProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.ObligationResponsibility |
| dbo.ObligationResponsibilities | INDEX | IX_ObligationResponsibilities_OriginProjectId | {"name": "IX_ObligationResponsibilities_OriginProjectId", "columns": ["OriginProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.ObligationResponsibility |
| dbo.OfflineAdmittedFileReferences | KEY | PK_OfflineAdmittedFileReferences | {"name": "PK_OfflineAdmittedFileReferences", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Offline.OfflineAdmittedFileReference |
| dbo.OfflineAdmittedFileReferences | KEY | AK_OfflineAdmittedFileReferences_Id_ProjectId | {"name": "AK_OfflineAdmittedFileReferences_Id_ProjectId", "columns": ["Id", "ProjectId"], "primary": false} | RoadGuardSystem.BusinessObjects.Offline.OfflineAdmittedFileReference |
| dbo.OfflineAdmittedFileReferences | INDEX | IX_OfflineAdmittedFileReferences_ActualFileOwnerId | {"name": "IX_OfflineAdmittedFileReferences_ActualFileOwnerId", "columns": ["ActualFileOwnerId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineAdmittedFileReference |
| dbo.OfflineAdmittedFileReferences | INDEX | IX_OfflineAdmittedFileReferences_ActualUploadedById | {"name": "IX_OfflineAdmittedFileReferences_ActualUploadedById", "columns": ["ActualUploadedById"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineAdmittedFileReference |
| dbo.OfflineAdmittedFileReferences | INDEX | IX_OfflineAdmittedFileReferences_CurrentImporterId | {"name": "IX_OfflineAdmittedFileReferences_CurrentImporterId", "columns": ["CurrentImporterId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineAdmittedFileReference |
| dbo.OfflineAdmittedFileReferences | INDEX | IX_OfflineAdmittedFileReferences_FileId | {"name": "IX_OfflineAdmittedFileReferences_FileId", "columns": ["FileId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineAdmittedFileReference |
| dbo.OfflineAdmittedFileReferences | INDEX | IX_OfflineAdmittedFileReferences_OriginalActorId | {"name": "IX_OfflineAdmittedFileReferences_OriginalActorId", "columns": ["OriginalActorId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineAdmittedFileReference |
| dbo.OfflineAdmittedFileReferences | INDEX | IX_OfflineAdmittedFileReferences_ProjectId | {"name": "IX_OfflineAdmittedFileReferences_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineAdmittedFileReference |
| dbo.OfflineAdmittedFileReferences | INDEX | IX_OfflineAdmittedFileReferences_TaskId | {"name": "IX_OfflineAdmittedFileReferences_TaskId", "columns": ["TaskId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineAdmittedFileReference |
| dbo.OfflineAdmittedFileReferences | INDEX | IX_OfflineAdmittedFileReferences_AdmissionId_CaptureOriginId | {"name": "IX_OfflineAdmittedFileReferences_AdmissionId_CaptureOriginId", "columns": ["AdmissionId", "CaptureOriginId"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineAdmittedFileReference |
| dbo.OfflineAdmittedFileReferences | INDEX | IX_OfflineAdmittedFileReferences_AdmissionId_ProjectId | {"name": "IX_OfflineAdmittedFileReferences_AdmissionId_ProjectId", "columns": ["AdmissionId", "ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineAdmittedFileReference |
| dbo.OfflineAdmittedFileReferences | INDEX | IX_OfflineAdmittedFileReferences_BindingId_ProjectId | {"name": "IX_OfflineAdmittedFileReferences_BindingId_ProjectId", "columns": ["BindingId", "ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineAdmittedFileReference |
| dbo.OfflineAdmittedFileReferences | CHECK | CK_OfflineAdmittedFileReferences_CaptureFactsJson | {"name": "CK_OfflineAdmittedFileReferences_CaptureFactsJson", "sql": "ISJSON([CaptureFactsJson])=1"} | RoadGuardSystem.BusinessObjects.Offline.OfflineAdmittedFileReference |
| dbo.OfflineAdmittedFileReferences | CHECK | CK_OfflineAdmittedFileReferences_ContentChecksum | {"name": "CK_OfflineAdmittedFileReferences_ContentChecksum", "sql": "LEN([ContentChecksum])=64 AND [ContentChecksum] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'"} | RoadGuardSystem.BusinessObjects.Offline.OfflineAdmittedFileReference |
| dbo.OfflineDeviceRegistrations | KEY | PK_OfflineDeviceRegistrations | {"name": "PK_OfflineDeviceRegistrations", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Offline.OfflineDeviceRegistration |
| dbo.OfflineDeviceRegistrations | KEY | AK_OfflineDeviceRegistrations_Id_ProjectId | {"name": "AK_OfflineDeviceRegistrations_Id_ProjectId", "columns": ["Id", "ProjectId"], "primary": false} | RoadGuardSystem.BusinessObjects.Offline.OfflineDeviceRegistration |
| dbo.OfflineDeviceRegistrations | INDEX | IX_OfflineDeviceRegistrations_ActorId | {"name": "IX_OfflineDeviceRegistrations_ActorId", "columns": ["ActorId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineDeviceRegistration |
| dbo.OfflineDeviceRegistrations | INDEX | IX_OfflineDeviceRegistrations_ProjectId_KeyFingerprint | {"name": "IX_OfflineDeviceRegistrations_ProjectId_KeyFingerprint", "columns": ["ProjectId", "KeyFingerprint"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineDeviceRegistration |
| dbo.OfflineDeviceRegistrations | INDEX | IX_OfflineDeviceRegistrations_ProjectId_ActorId_DeviceId_Revision | {"name": "IX_OfflineDeviceRegistrations_ProjectId_ActorId_DeviceId_Revision", "columns": ["ProjectId", "ActorId", "DeviceId", "Revision"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineDeviceRegistration |
| dbo.OfflineDeviceRegistrations | CHECK | CK_OfflineDeviceRegistrations_KeyFingerprint | {"name": "CK_OfflineDeviceRegistrations_KeyFingerprint", "sql": "LEN([KeyFingerprint])=64 AND [KeyFingerprint] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'"} | RoadGuardSystem.BusinessObjects.Offline.OfflineDeviceRegistration |
| dbo.OfflineDeviceRegistrations | CHECK | CK_OfflineDeviceRegistrations_Revision | {"name": "CK_OfflineDeviceRegistrations_Revision", "sql": "[Revision]>0"} | RoadGuardSystem.BusinessObjects.Offline.OfflineDeviceRegistration |
| dbo.OfflineDeviceRevocations | KEY | PK_OfflineDeviceRevocations | {"name": "PK_OfflineDeviceRevocations", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Offline.OfflineDeviceRevocation |
| dbo.OfflineDeviceRevocations | KEY | AK_OfflineDeviceRevocations_Id_ProjectId | {"name": "AK_OfflineDeviceRevocations_Id_ProjectId", "columns": ["Id", "ProjectId"], "primary": false} | RoadGuardSystem.BusinessObjects.Offline.OfflineDeviceRevocation |
| dbo.OfflineDeviceRevocations | INDEX | IX_OfflineDeviceRevocations_DeviceRegistrationId | {"name": "IX_OfflineDeviceRevocations_DeviceRegistrationId", "columns": ["DeviceRegistrationId"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineDeviceRevocation |
| dbo.OfflineDeviceRevocations | INDEX | IX_OfflineDeviceRevocations_ProjectId | {"name": "IX_OfflineDeviceRevocations_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineDeviceRevocation |
| dbo.OfflineDeviceRevocations | INDEX | IX_OfflineDeviceRevocations_RevokedBy | {"name": "IX_OfflineDeviceRevocations_RevokedBy", "columns": ["RevokedBy"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineDeviceRevocation |
| dbo.OfflineDeviceRevocations | INDEX | IX_OfflineDeviceRevocations_DeviceRegistrationId_ProjectId | {"name": "IX_OfflineDeviceRevocations_DeviceRegistrationId_ProjectId", "columns": ["DeviceRegistrationId", "ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineDeviceRevocation |
| dbo.OfflineEncryptedCaptureArtifacts | KEY | PK_OfflineEncryptedCaptureArtifacts | {"name": "PK_OfflineEncryptedCaptureArtifacts", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Offline.OfflineEncryptedCaptureArtifact |
| dbo.OfflineEncryptedCaptureArtifacts | KEY | AK_OfflineEncryptedCaptureArtifacts_Id_ProjectId | {"name": "AK_OfflineEncryptedCaptureArtifacts_Id_ProjectId", "columns": ["Id", "ProjectId"], "primary": false} | RoadGuardSystem.BusinessObjects.Offline.OfflineEncryptedCaptureArtifact |
| dbo.OfflineEncryptedCaptureArtifacts | INDEX | IX_OfflineEncryptedCaptureArtifacts_OriginalActorId | {"name": "IX_OfflineEncryptedCaptureArtifacts_OriginalActorId", "columns": ["OriginalActorId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineEncryptedCaptureArtifact |
| dbo.OfflineEncryptedCaptureArtifacts | INDEX | IX_OfflineEncryptedCaptureArtifacts_ProjectId | {"name": "IX_OfflineEncryptedCaptureArtifacts_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineEncryptedCaptureArtifact |
| dbo.OfflineEncryptedCaptureArtifacts | INDEX | IX_OfflineEncryptedCaptureArtifacts_RegisteredBy | {"name": "IX_OfflineEncryptedCaptureArtifacts_RegisteredBy", "columns": ["RegisteredBy"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineEncryptedCaptureArtifact |
| dbo.OfflineEncryptedCaptureArtifacts | INDEX | IX_OfflineEncryptedCaptureArtifacts_TaskId | {"name": "IX_OfflineEncryptedCaptureArtifacts_TaskId", "columns": ["TaskId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineEncryptedCaptureArtifact |
| dbo.OfflineEncryptedCaptureArtifacts | INDEX | IX_OfflineEncryptedCaptureArtifacts_ParentPackageId_ProjectId | {"name": "IX_OfflineEncryptedCaptureArtifacts_ParentPackageId_ProjectId", "columns": ["ParentPackageId", "ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineEncryptedCaptureArtifact |
| dbo.OfflineEncryptedCaptureArtifacts | INDEX | IX_OfflineEncryptedCaptureArtifacts_SourceDeviceRegistrationId_ProjectId | {"name": "IX_OfflineEncryptedCaptureArtifacts_SourceDeviceRegistrationId_ProjectId", "columns": ["SourceDeviceRegistrationId", "ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineEncryptedCaptureArtifact |
| dbo.OfflineEncryptedCaptureArtifacts | INDEX | IX_OfflineEncryptedCaptureArtifacts_ParentPackageId_CaptureOriginId_ManifestHash_ChunkIndex | {"name": "IX_OfflineEncryptedCaptureArtifacts_ParentPackageId_CaptureOriginId_ManifestHash_ChunkIndex", "columns": ["ParentPackageId", "CaptureOriginId", "ManifestHash", "ChunkIndex"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineEncryptedCaptureArtifact |
| dbo.OfflineEncryptedCaptureArtifacts | CHECK | CK_OfflineEncryptedCaptureArtifacts_Chunk | {"name": "CK_OfflineEncryptedCaptureArtifacts_Chunk", "sql": "[ChunkIndex]>=0 AND [ChunkIndex]<32 AND [ChunkOffset]>=0 AND [ChunkLength]>0 AND [ChunkLength]<=16777216"} | RoadGuardSystem.BusinessObjects.Offline.OfflineEncryptedCaptureArtifact |
| dbo.OfflineEncryptedCaptureArtifacts | CHECK | CK_OfflineEncryptedCaptureArtifacts_CipherEnvelopeJson | {"name": "CK_OfflineEncryptedCaptureArtifacts_CipherEnvelopeJson", "sql": "ISJSON([CipherEnvelopeJson])=1"} | RoadGuardSystem.BusinessObjects.Offline.OfflineEncryptedCaptureArtifact |
| dbo.OfflineEncryptedCaptureArtifacts | CHECK | CK_OfflineEncryptedCaptureArtifacts_EnvelopeFingerprint | {"name": "CK_OfflineEncryptedCaptureArtifacts_EnvelopeFingerprint", "sql": "LEN([EnvelopeFingerprint])=64 AND [EnvelopeFingerprint] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'"} | RoadGuardSystem.BusinessObjects.Offline.OfflineEncryptedCaptureArtifact |
| dbo.OfflineEncryptedCaptureArtifacts | CHECK | CK_OfflineEncryptedCaptureArtifacts_ManifestHash | {"name": "CK_OfflineEncryptedCaptureArtifacts_ManifestHash", "sql": "LEN([ManifestHash])=64 AND [ManifestHash] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'"} | RoadGuardSystem.BusinessObjects.Offline.OfflineEncryptedCaptureArtifact |
| dbo.OfflineEncryptedCaptureArtifacts | CHECK | CK_OfflineEncryptedCaptureArtifacts_PlaintextChecksum | {"name": "CK_OfflineEncryptedCaptureArtifacts_PlaintextChecksum", "sql": "LEN([PlaintextChecksum])=64 AND [PlaintextChecksum] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'"} | RoadGuardSystem.BusinessObjects.Offline.OfflineEncryptedCaptureArtifact |
| dbo.OfflineEncryptedCaptureArtifacts | CHECK | CK_OfflineEncryptedCaptureArtifacts_SignedManifestJson | {"name": "CK_OfflineEncryptedCaptureArtifacts_SignedManifestJson", "sql": "ISJSON([SignedManifestJson])=1"} | RoadGuardSystem.BusinessObjects.Offline.OfflineEncryptedCaptureArtifact |
| dbo.OfflineEncryptedPackages | KEY | PK_OfflineEncryptedPackages | {"name": "PK_OfflineEncryptedPackages", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Offline.OfflineEncryptedPackageRecord |
| dbo.OfflineEncryptedPackages | KEY | AK_OfflineEncryptedPackages_Id_ProjectId | {"name": "AK_OfflineEncryptedPackages_Id_ProjectId", "columns": ["Id", "ProjectId"], "primary": false} | RoadGuardSystem.BusinessObjects.Offline.OfflineEncryptedPackageRecord |
| dbo.OfflineEncryptedPackages | INDEX | IX_OfflineEncryptedPackages_OriginalActorId | {"name": "IX_OfflineEncryptedPackages_OriginalActorId", "columns": ["OriginalActorId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineEncryptedPackageRecord |
| dbo.OfflineEncryptedPackages | INDEX | IX_OfflineEncryptedPackages_RegisteredBy | {"name": "IX_OfflineEncryptedPackages_RegisteredBy", "columns": ["RegisteredBy"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineEncryptedPackageRecord |
| dbo.OfflineEncryptedPackages | INDEX | IX_OfflineEncryptedPackages_SourceDeviceRegistrationId_ProjectId | {"name": "IX_OfflineEncryptedPackages_SourceDeviceRegistrationId_ProjectId", "columns": ["SourceDeviceRegistrationId", "ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineEncryptedPackageRecord |
| dbo.OfflineEncryptedPackages | INDEX | IX_OfflineEncryptedPackages_ProjectId_SourceDeviceRegistrationId_SourceBatchId | {"name": "IX_OfflineEncryptedPackages_ProjectId_SourceDeviceRegistrationId_SourceBatchId", "columns": ["ProjectId", "SourceDeviceRegistrationId", "SourceBatchId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineEncryptedPackageRecord |
| dbo.OfflineEncryptedPackages | CHECK | CK_OfflineEncryptedPackages_CipherPackageJson | {"name": "CK_OfflineEncryptedPackages_CipherPackageJson", "sql": "ISJSON([CipherPackageJson])=1"} | RoadGuardSystem.BusinessObjects.Offline.OfflineEncryptedPackageRecord |
| dbo.OfflineEncryptedPackages | CHECK | CK_OfflineEncryptedPackages_ManifestHash | {"name": "CK_OfflineEncryptedPackages_ManifestHash", "sql": "LEN([ManifestHash])=64 AND [ManifestHash] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'"} | RoadGuardSystem.BusinessObjects.Offline.OfflineEncryptedPackageRecord |
| dbo.OfflineEncryptedPackages | CHECK | CK_OfflineEncryptedPackages_PackageFingerprint | {"name": "CK_OfflineEncryptedPackages_PackageFingerprint", "sql": "LEN([PackageFingerprint])=64 AND [PackageFingerprint] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'"} | RoadGuardSystem.BusinessObjects.Offline.OfflineEncryptedPackageRecord |
| dbo.OfflineEncryptedPackages | CHECK | CK_OfflineEncryptedPackages_PayloadHash | {"name": "CK_OfflineEncryptedPackages_PayloadHash", "sql": "LEN([PayloadHash])=64 AND [PayloadHash] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'"} | RoadGuardSystem.BusinessObjects.Offline.OfflineEncryptedPackageRecord |
| dbo.OfflineEncryptedPackages | CHECK | CK_OfflineEncryptedPackages_SignedManifestJson | {"name": "CK_OfflineEncryptedPackages_SignedManifestJson", "sql": "ISJSON([SignedManifestJson])=1"} | RoadGuardSystem.BusinessObjects.Offline.OfflineEncryptedPackageRecord |
| dbo.OfflineEvidenceCaptureReferences | KEY | PK_OfflineEvidenceCaptureReferences | {"name": "PK_OfflineEvidenceCaptureReferences", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Offline.OfflineEvidenceCaptureReference |
| dbo.OfflineEvidenceCaptureReferences | KEY | AK_OfflineEvidenceCaptureReferences_Id_ProjectId | {"name": "AK_OfflineEvidenceCaptureReferences_Id_ProjectId", "columns": ["Id", "ProjectId"], "primary": false} | RoadGuardSystem.BusinessObjects.Offline.OfflineEvidenceCaptureReference |
| dbo.OfflineEvidenceCaptureReferences | INDEX | IX_OfflineEvidenceCaptureReferences_ActualUploaderId | {"name": "IX_OfflineEvidenceCaptureReferences_ActualUploaderId", "columns": ["ActualUploaderId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineEvidenceCaptureReference |
| dbo.OfflineEvidenceCaptureReferences | INDEX | IX_OfflineEvidenceCaptureReferences_FileId | {"name": "IX_OfflineEvidenceCaptureReferences_FileId", "columns": ["FileId"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineEvidenceCaptureReference |
| dbo.OfflineEvidenceCaptureReferences | INDEX | IX_OfflineEvidenceCaptureReferences_OriginalActorId | {"name": "IX_OfflineEvidenceCaptureReferences_OriginalActorId", "columns": ["OriginalActorId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineEvidenceCaptureReference |
| dbo.OfflineEvidenceCaptureReferences | INDEX | IX_OfflineEvidenceCaptureReferences_ProjectId | {"name": "IX_OfflineEvidenceCaptureReferences_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineEvidenceCaptureReference |
| dbo.OfflineEvidenceCaptureReferences | INDEX | IX_OfflineEvidenceCaptureReferences_TaskId | {"name": "IX_OfflineEvidenceCaptureReferences_TaskId", "columns": ["TaskId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineEvidenceCaptureReference |
| dbo.OfflineEvidenceCaptureReferences | INDEX | IX_OfflineEvidenceCaptureReferences_UploadSessionId | {"name": "IX_OfflineEvidenceCaptureReferences_UploadSessionId", "columns": ["UploadSessionId"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineEvidenceCaptureReference |
| dbo.OfflineEvidenceCaptureReferences | INDEX | IX_OfflineEvidenceCaptureReferences_AdmissionId_CaptureOriginId | {"name": "IX_OfflineEvidenceCaptureReferences_AdmissionId_CaptureOriginId", "columns": ["AdmissionId", "CaptureOriginId"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineEvidenceCaptureReference |
| dbo.OfflineEvidenceCaptureReferences | INDEX | IX_OfflineEvidenceCaptureReferences_AdmissionId_ProjectId | {"name": "IX_OfflineEvidenceCaptureReferences_AdmissionId_ProjectId", "columns": ["AdmissionId", "ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineEvidenceCaptureReference |
| dbo.OfflineEvidenceCaptureReferences | INDEX | IX_OfflineEvidenceCaptureReferences_BindingId_ProjectId | {"name": "IX_OfflineEvidenceCaptureReferences_BindingId_ProjectId", "columns": ["BindingId", "ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineEvidenceCaptureReference |
| dbo.OfflineEvidenceCaptureReferences | INDEX | IX_OfflineEvidenceCaptureReferences_GrantId_ProjectId | {"name": "IX_OfflineEvidenceCaptureReferences_GrantId_ProjectId", "columns": ["GrantId", "ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineEvidenceCaptureReference |
| dbo.OfflineEvidenceCaptureReferences | CHECK | CK_OfflineEvidenceCaptureReferences_CaptureFactsJson | {"name": "CK_OfflineEvidenceCaptureReferences_CaptureFactsJson", "sql": "ISJSON([CaptureFactsJson])=1"} | RoadGuardSystem.BusinessObjects.Offline.OfflineEvidenceCaptureReference |
| dbo.OfflineEvidenceCaptureReferences | CHECK | CK_OfflineEvidenceCaptureReferences_Checksum | {"name": "CK_OfflineEvidenceCaptureReferences_Checksum", "sql": "LEN([Checksum])=64 AND [Checksum] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'"} | RoadGuardSystem.BusinessObjects.Offline.OfflineEvidenceCaptureReference |
| dbo.OfflineHandoverGrantRevocations | KEY | PK_OfflineHandoverGrantRevocations | {"name": "PK_OfflineHandoverGrantRevocations", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Offline.OfflineHandoverGrantRevocation |
| dbo.OfflineHandoverGrantRevocations | KEY | AK_OfflineHandoverGrantRevocations_Id_ProjectId | {"name": "AK_OfflineHandoverGrantRevocations_Id_ProjectId", "columns": ["Id", "ProjectId"], "primary": false} | RoadGuardSystem.BusinessObjects.Offline.OfflineHandoverGrantRevocation |
| dbo.OfflineHandoverGrantRevocations | INDEX | IX_OfflineHandoverGrantRevocations_GrantId | {"name": "IX_OfflineHandoverGrantRevocations_GrantId", "columns": ["GrantId"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineHandoverGrantRevocation |
| dbo.OfflineHandoverGrantRevocations | INDEX | IX_OfflineHandoverGrantRevocations_ProjectId | {"name": "IX_OfflineHandoverGrantRevocations_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineHandoverGrantRevocation |
| dbo.OfflineHandoverGrantRevocations | INDEX | IX_OfflineHandoverGrantRevocations_RevokedBy | {"name": "IX_OfflineHandoverGrantRevocations_RevokedBy", "columns": ["RevokedBy"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineHandoverGrantRevocation |
| dbo.OfflineHandoverGrantRevocations | INDEX | IX_OfflineHandoverGrantRevocations_GrantId_ProjectId | {"name": "IX_OfflineHandoverGrantRevocations_GrantId_ProjectId", "columns": ["GrantId", "ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineHandoverGrantRevocation |
| dbo.OfflineHandoverGrants | KEY | PK_OfflineHandoverGrants | {"name": "PK_OfflineHandoverGrants", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Offline.OfflineHandoverGrant |
| dbo.OfflineHandoverGrants | KEY | AK_OfflineHandoverGrants_Id_ProjectId | {"name": "AK_OfflineHandoverGrants_Id_ProjectId", "columns": ["Id", "ProjectId"], "primary": false} | RoadGuardSystem.BusinessObjects.Offline.OfflineHandoverGrant |
| dbo.OfflineHandoverGrants | INDEX | IX_OfflineHandoverGrants_IssuedBy | {"name": "IX_OfflineHandoverGrants_IssuedBy", "columns": ["IssuedBy"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineHandoverGrant |
| dbo.OfflineHandoverGrants | INDEX | IX_OfflineHandoverGrants_RecipientActorId | {"name": "IX_OfflineHandoverGrants_RecipientActorId", "columns": ["RecipientActorId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineHandoverGrant |
| dbo.OfflineHandoverGrants | INDEX | IX_OfflineHandoverGrants_SourceActorId | {"name": "IX_OfflineHandoverGrants_SourceActorId", "columns": ["SourceActorId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineHandoverGrant |
| dbo.OfflineHandoverGrants | INDEX | IX_OfflineHandoverGrants_PackageId_ProjectId | {"name": "IX_OfflineHandoverGrants_PackageId_ProjectId", "columns": ["PackageId", "ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineHandoverGrant |
| dbo.OfflineHandoverGrants | INDEX | IX_OfflineHandoverGrants_RecipientDeviceRegistrationId_ProjectId | {"name": "IX_OfflineHandoverGrants_RecipientDeviceRegistrationId_ProjectId", "columns": ["RecipientDeviceRegistrationId", "ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineHandoverGrant |
| dbo.OfflineHandoverGrants | INDEX | IX_OfflineHandoverGrants_SourceDeviceRegistrationId_ProjectId | {"name": "IX_OfflineHandoverGrants_SourceDeviceRegistrationId_ProjectId", "columns": ["SourceDeviceRegistrationId", "ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineHandoverGrant |
| dbo.OfflineHandoverGrants | INDEX | IX_OfflineHandoverGrants_ProjectId_PackageId_RecipientActorId_ExpiresAt | {"name": "IX_OfflineHandoverGrants_ProjectId_PackageId_RecipientActorId_ExpiresAt", "columns": ["ProjectId", "PackageId", "RecipientActorId", "ExpiresAt"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineHandoverGrant |
| dbo.OfflineHandoverGrants | CHECK | CK_OfflineHandoverGrants_Expiry | {"name": "CK_OfflineHandoverGrants_Expiry", "sql": "[ExpiresAt]=DATEADD(hour,24,[IssuedAt])"} | RoadGuardSystem.BusinessObjects.Offline.OfflineHandoverGrant |
| dbo.OfflineHandoverGrants | CHECK | CK_OfflineHandoverGrants_ManifestHash | {"name": "CK_OfflineHandoverGrants_ManifestHash", "sql": "LEN([ManifestHash])=64 AND [ManifestHash] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'"} | RoadGuardSystem.BusinessObjects.Offline.OfflineHandoverGrant |
| dbo.OfflineHandoverGrants | CHECK | CK_OfflineHandoverGrants_ScopeJson | {"name": "CK_OfflineHandoverGrants_ScopeJson", "sql": "ISJSON([ScopeJson])=1"} | RoadGuardSystem.BusinessObjects.Offline.OfflineHandoverGrant |
| dbo.OfflineOperationAdmissions | KEY | PK_OfflineOperationAdmissions | {"name": "PK_OfflineOperationAdmissions", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Offline.OfflineOperationAdmission |
| dbo.OfflineOperationAdmissions | KEY | AK_OfflineOperationAdmissions_Id_ProjectId | {"name": "AK_OfflineOperationAdmissions_Id_ProjectId", "columns": ["Id", "ProjectId"], "primary": false} | RoadGuardSystem.BusinessObjects.Offline.OfflineOperationAdmission |
| dbo.OfflineOperationAdmissions | INDEX | IX_OfflineOperationAdmissions_CurrentImporterId | {"name": "IX_OfflineOperationAdmissions_CurrentImporterId", "columns": ["CurrentImporterId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineOperationAdmission |
| dbo.OfflineOperationAdmissions | INDEX | IX_OfflineOperationAdmissions_ProjectId | {"name": "IX_OfflineOperationAdmissions_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineOperationAdmission |
| dbo.OfflineOperationAdmissions | INDEX | IX_OfflineOperationAdmissions_BatchId_BindingId | {"name": "IX_OfflineOperationAdmissions_BatchId_BindingId", "columns": ["BatchId", "BindingId"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineOperationAdmission |
| dbo.OfflineOperationAdmissions | INDEX | IX_OfflineOperationAdmissions_BatchId_ProjectId | {"name": "IX_OfflineOperationAdmissions_BatchId_ProjectId", "columns": ["BatchId", "ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineOperationAdmission |
| dbo.OfflineOperationAdmissions | INDEX | IX_OfflineOperationAdmissions_BindingId_ProjectId | {"name": "IX_OfflineOperationAdmissions_BindingId_ProjectId", "columns": ["BindingId", "ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineOperationAdmission |
| dbo.OfflineOperationAdmissions | INDEX | IX_OfflineOperationAdmissions_GrantId_ProjectId | {"name": "IX_OfflineOperationAdmissions_GrantId_ProjectId", "columns": ["GrantId", "ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineOperationAdmission |
| dbo.OfflineOperationAdmissions | CHECK | CK_OfflineOperationAdmissions_Mode | {"name": "CK_OfflineOperationAdmissions_Mode", "sql": "([AdmissionMode]='DIRECT_SYNC' AND [GrantId] IS NULL) OR ([AdmissionMode]='HANDOVER' AND [GrantId] IS NOT NULL)"} | RoadGuardSystem.BusinessObjects.Offline.OfflineOperationAdmission |
| dbo.OfflineOperationAdmissions | CHECK | CK_OfflineOperationAdmissions_ScopeFactsJson | {"name": "CK_OfflineOperationAdmissions_ScopeFactsJson", "sql": "ISJSON([ScopeFactsJson])=1"} | RoadGuardSystem.BusinessObjects.Offline.OfflineOperationAdmission |
| dbo.OfflineOperationBindings | KEY | PK_OfflineOperationBindings | {"name": "PK_OfflineOperationBindings", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Offline.OfflineOperationBinding |
| dbo.OfflineOperationBindings | KEY | AK_OfflineOperationBindings_Id_ProjectId | {"name": "AK_OfflineOperationBindings_Id_ProjectId", "columns": ["Id", "ProjectId"], "primary": false} | RoadGuardSystem.BusinessObjects.Offline.OfflineOperationBinding |
| dbo.OfflineOperationBindings | INDEX | IX_OfflineOperationBindings_AssignmentId | {"name": "IX_OfflineOperationBindings_AssignmentId", "columns": ["AssignmentId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineOperationBinding |
| dbo.OfflineOperationBindings | INDEX | IX_OfflineOperationBindings_EffectId | {"name": "IX_OfflineOperationBindings_EffectId", "columns": ["EffectId"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineOperationBinding |
| dbo.OfflineOperationBindings | INDEX | IX_OfflineOperationBindings_OriginalActorId | {"name": "IX_OfflineOperationBindings_OriginalActorId", "columns": ["OriginalActorId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineOperationBinding |
| dbo.OfflineOperationBindings | INDEX | IX_OfflineOperationBindings_RepairResourceId | {"name": "IX_OfflineOperationBindings_RepairResourceId", "columns": ["RepairResourceId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineOperationBinding |
| dbo.OfflineOperationBindings | INDEX | IX_OfflineOperationBindings_TaskId | {"name": "IX_OfflineOperationBindings_TaskId", "columns": ["TaskId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineOperationBinding |
| dbo.OfflineOperationBindings | INDEX | IX_OfflineOperationBindings_ProjectId_OriginId | {"name": "IX_OfflineOperationBindings_ProjectId_OriginId", "columns": ["ProjectId", "OriginId"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineOperationBinding |
| dbo.OfflineOperationBindings | INDEX | IX_OfflineOperationBindings_SnapshotId_ProjectId | {"name": "IX_OfflineOperationBindings_SnapshotId_ProjectId", "columns": ["SnapshotId", "ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineOperationBinding |
| dbo.OfflineOperationBindings | INDEX | IX_OfflineOperationBindings_SourceDeviceRegistrationId_ProjectId | {"name": "IX_OfflineOperationBindings_SourceDeviceRegistrationId_ProjectId", "columns": ["SourceDeviceRegistrationId", "ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineOperationBinding |
| dbo.OfflineOperationBindings | CHECK | CK_OfflineOperationBindings_CorePayloadHash | {"name": "CK_OfflineOperationBindings_CorePayloadHash", "sql": "LEN([CorePayloadHash])=64 AND [CorePayloadHash] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'"} | RoadGuardSystem.BusinessObjects.Offline.OfflineOperationBinding |
| dbo.OfflineOperationBindings | CHECK | CK_OfflineOperationBindings_EnvelopeHash | {"name": "CK_OfflineOperationBindings_EnvelopeHash", "sql": "LEN([EnvelopeHash])=64 AND [EnvelopeHash] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'"} | RoadGuardSystem.BusinessObjects.Offline.OfflineOperationBinding |
| dbo.OfflineOperationBindings | CHECK | CK_OfflineOperationBindings_EnvelopeJson | {"name": "CK_OfflineOperationBindings_EnvelopeJson", "sql": "ISJSON([EnvelopeJson])=1"} | RoadGuardSystem.BusinessObjects.Offline.OfflineOperationBinding |
| dbo.OfflineOperationBindings | CHECK | CK_OfflineOperationBindings_RepairScope | {"name": "CK_OfflineOperationBindings_RepairScope", "sql": "([Kind] IN ('FIELD_ACCEPT','FIELD_START','FIELD_SUBMISSION') AND [RepairResourceId] IS NULL) OR ([Kind] IN ('REPAIR_ASSESSMENT','REPAIR_EXECUTION_START','REPAIR_EXECUTION_FINISH') AND [RepairResourceId] IS NOT NULL)"} | RoadGuardSystem.BusinessObjects.Offline.OfflineOperationBinding |
| dbo.OfflineOperationResults | KEY | PK_OfflineOperationResults | {"name": "PK_OfflineOperationResults", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Offline.OfflineOperationResult |
| dbo.OfflineOperationResults | KEY | AK_OfflineOperationResults_Id_ProjectId | {"name": "AK_OfflineOperationResults_Id_ProjectId", "columns": ["Id", "ProjectId"], "primary": false} | RoadGuardSystem.BusinessObjects.Offline.OfflineOperationResult |
| dbo.OfflineOperationResults | INDEX | IX_OfflineOperationResults_AdmissionId_ProjectId | {"name": "IX_OfflineOperationResults_AdmissionId_ProjectId", "columns": ["AdmissionId", "ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineOperationResult |
| dbo.OfflineOperationResults | INDEX | IX_OfflineOperationResults_BatchId_ProjectId | {"name": "IX_OfflineOperationResults_BatchId_ProjectId", "columns": ["BatchId", "ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineOperationResult |
| dbo.OfflineOperationResults | INDEX | IX_OfflineOperationResults_ProjectId_OriginId_RecordedAt | {"name": "IX_OfflineOperationResults_ProjectId_OriginId_RecordedAt", "columns": ["ProjectId", "OriginId", "RecordedAt"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineOperationResult |
| dbo.OfflineOperationResults | CHECK | CK_OfflineOperationResults_Acknowledgment | {"name": "CK_OfflineOperationResults_Acknowledgment", "sql": "([State] IN ('COMMITTED','REPLAYED') AND [DurableAck]=1 AND [EffectId] IS NOT NULL) OR ([State] IN ('CONFLICT','PENDING_DEPENDENCY','REJECTED','STALE_SNAPSHOT') AND [DurableAck]=0)"} | RoadGuardSystem.BusinessObjects.Offline.OfflineOperationResult |
| dbo.OfflineOperationResults | CHECK | CK_OfflineOperationResults_OutcomeJson | {"name": "CK_OfflineOperationResults_OutcomeJson", "sql": "ISJSON([OutcomeJson])=1"} | RoadGuardSystem.BusinessObjects.Offline.OfflineOperationResult |
| dbo.OfflineOperationResults | CHECK | CK_OfflineOperationResults_Time | {"name": "CK_OfflineOperationResults_Time", "sql": "[TimeProvenance] IN ('UNCERTAIN','VERIFIED_ORIGINAL') AND ([VerifiedFinishedAt] IS NOT NULL OR [SyncLateness]='UNKNOWN')"} | RoadGuardSystem.BusinessObjects.Offline.OfflineOperationResult |
| dbo.OfflineOriginTimeVerifications | KEY | PK_OfflineOriginTimeVerifications | {"name": "PK_OfflineOriginTimeVerifications", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Offline.OfflineOriginTimeVerification |
| dbo.OfflineOriginTimeVerifications | KEY | AK_OfflineOriginTimeVerifications_Id_ProjectId | {"name": "AK_OfflineOriginTimeVerifications_Id_ProjectId", "columns": ["Id", "ProjectId"], "primary": false} | RoadGuardSystem.BusinessObjects.Offline.OfflineOriginTimeVerification |
| dbo.OfflineOriginTimeVerifications | INDEX | IX_OfflineOriginTimeVerifications_CanonicalOriginId | {"name": "IX_OfflineOriginTimeVerifications_CanonicalOriginId", "columns": ["CanonicalOriginId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineOriginTimeVerification |
| dbo.OfflineOriginTimeVerifications | INDEX | IX_OfflineOriginTimeVerifications_ProjectId | {"name": "IX_OfflineOriginTimeVerifications_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineOriginTimeVerification |
| dbo.OfflineOriginTimeVerifications | INDEX | IX_OfflineOriginTimeVerifications_VerifiedBy | {"name": "IX_OfflineOriginTimeVerifications_VerifiedBy", "columns": ["VerifiedBy"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineOriginTimeVerification |
| dbo.OfflineOriginTimeVerifications | INDEX | IX_OfflineOriginTimeVerifications_BindingId_ProjectId | {"name": "IX_OfflineOriginTimeVerifications_BindingId_ProjectId", "columns": ["BindingId", "ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineOriginTimeVerification |
| dbo.OfflineOriginTimeVerifications | INDEX | IX_OfflineOriginTimeVerifications_BindingId_ProofSourceKind_ProofSourceId | {"name": "IX_OfflineOriginTimeVerifications_BindingId_ProofSourceKind_ProofSourceId", "columns": ["BindingId", "ProofSourceKind", "ProofSourceId"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineOriginTimeVerification |
| dbo.OfflineOriginTimeVerifications | CHECK | CK_OfflineOriginTimeVerifications_Source | {"name": "CK_OfflineOriginTimeVerifications_Source", "sql": "[ProofSourceKind] IN ('FIELD_START_SERVER_ORIGIN','REPAIR_FINISH_SERVER_ORIGIN') AND [OriginalOccurredAtUtc]<=[RecordedAtUtc]"} | RoadGuardSystem.BusinessObjects.Offline.OfflineOriginTimeVerification |
| dbo.OfflinePackageFileReferences | KEY | PK_OfflinePackageFileReferences | {"name": "PK_OfflinePackageFileReferences", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Offline.OfflinePackageFileReference |
| dbo.OfflinePackageFileReferences | KEY | AK_OfflinePackageFileReferences_Id_ProjectId | {"name": "AK_OfflinePackageFileReferences_Id_ProjectId", "columns": ["Id", "ProjectId"], "primary": false} | RoadGuardSystem.BusinessObjects.Offline.OfflinePackageFileReference |
| dbo.OfflinePackageFileReferences | INDEX | IX_OfflinePackageFileReferences_FileId | {"name": "IX_OfflinePackageFileReferences_FileId", "columns": ["FileId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflinePackageFileReference |
| dbo.OfflinePackageFileReferences | INDEX | IX_OfflinePackageFileReferences_ProjectId | {"name": "IX_OfflinePackageFileReferences_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflinePackageFileReference |
| dbo.OfflinePackageFileReferences | INDEX | IX_OfflinePackageFileReferences_PackageId_FileId | {"name": "IX_OfflinePackageFileReferences_PackageId_FileId", "columns": ["PackageId", "FileId"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflinePackageFileReference |
| dbo.OfflinePackageFileReferences | INDEX | IX_OfflinePackageFileReferences_PackageId_ProjectId | {"name": "IX_OfflinePackageFileReferences_PackageId_ProjectId", "columns": ["PackageId", "ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflinePackageFileReference |
| dbo.OfflinePackageFileReferences | CHECK | CK_OfflinePackageFileReferences_CaptureFactsJson | {"name": "CK_OfflinePackageFileReferences_CaptureFactsJson", "sql": "ISJSON([CaptureFactsJson])=1"} | RoadGuardSystem.BusinessObjects.Offline.OfflinePackageFileReference |
| dbo.OfflinePackageFileReferences | CHECK | CK_OfflinePackageFileReferences_ContentChecksum | {"name": "CK_OfflinePackageFileReferences_ContentChecksum", "sql": "LEN([ContentChecksum])=64 AND [ContentChecksum] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'"} | RoadGuardSystem.BusinessObjects.Offline.OfflinePackageFileReference |
| dbo.OfflineSyncBatches | KEY | PK_OfflineSyncBatches | {"name": "PK_OfflineSyncBatches", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Offline.OfflineSyncBatch |
| dbo.OfflineSyncBatches | KEY | AK_OfflineSyncBatches_Id_ProjectId | {"name": "AK_OfflineSyncBatches_Id_ProjectId", "columns": ["Id", "ProjectId"], "primary": false} | RoadGuardSystem.BusinessObjects.Offline.OfflineSyncBatch |
| dbo.OfflineSyncBatches | INDEX | IX_OfflineSyncBatches_CurrentImporterId | {"name": "IX_OfflineSyncBatches_CurrentImporterId", "columns": ["CurrentImporterId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineSyncBatch |
| dbo.OfflineSyncBatches | INDEX | IX_OfflineSyncBatches_GrantId_ProjectId | {"name": "IX_OfflineSyncBatches_GrantId_ProjectId", "columns": ["GrantId", "ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineSyncBatch |
| dbo.OfflineSyncBatches | INDEX | IX_OfflineSyncBatches_PackageId_ProjectId | {"name": "IX_OfflineSyncBatches_PackageId_ProjectId", "columns": ["PackageId", "ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineSyncBatch |
| dbo.OfflineSyncBatches | INDEX | IX_OfflineSyncBatches_RecipientDeviceRegistrationId_ProjectId | {"name": "IX_OfflineSyncBatches_RecipientDeviceRegistrationId_ProjectId", "columns": ["RecipientDeviceRegistrationId", "ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineSyncBatch |
| dbo.OfflineSyncBatches | INDEX | IX_OfflineSyncBatches_SourceDeviceRegistrationId_ProjectId | {"name": "IX_OfflineSyncBatches_SourceDeviceRegistrationId_ProjectId", "columns": ["SourceDeviceRegistrationId", "ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineSyncBatch |
| dbo.OfflineSyncBatches | INDEX | IX_OfflineSyncBatches_ProjectId_SourceDeviceRegistrationId_SourceBatchId_CurrentImporterId_ContentHash | {"name": "IX_OfflineSyncBatches_ProjectId_SourceDeviceRegistrationId_SourceBatchId_CurrentImporterId_ContentHash", "columns": ["ProjectId", "SourceDeviceRegistrationId", "SourceBatchId", "CurrentImporterId", "ContentHash"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineSyncBatch |
| dbo.OfflineSyncBatches | CHECK | CK_OfflineSyncBatches_AttachedPayload | {"name": "CK_OfflineSyncBatches_AttachedPayload", "sql": "([AttachedPayloadJson] IS NULL AND [AttachedPayloadHash] IS NULL) OR ([AttachedPayloadJson] IS NOT NULL AND [AttachedPayloadHash] IS NOT NULL AND ISJSON([AttachedPayloadJson])=1 AND LEN([AttachedPayloadHash])=64 AND [AttachedPayloadHash] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%' AND DATALENGTH(CONVERT(varchar(max),[AttachedPayloadJson] COLLATE Latin1_General_100_BIN2_UTF8))<=16777216)"} | RoadGuardSystem.BusinessObjects.Offline.OfflineSyncBatch |
| dbo.OfflineSyncBatches | CHECK | CK_OfflineSyncBatches_ContentHash | {"name": "CK_OfflineSyncBatches_ContentHash", "sql": "LEN([ContentHash])=64 AND [ContentHash] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'"} | RoadGuardSystem.BusinessObjects.Offline.OfflineSyncBatch |
| dbo.OfflineSyncBatches | CHECK | CK_OfflineSyncBatches_Recipient | {"name": "CK_OfflineSyncBatches_Recipient", "sql": "([PackageId] IS NULL AND [GrantId] IS NULL AND [RecipientDeviceRegistrationId] IS NULL AND [RecipientSignature] IS NULL) OR ([PackageId] IS NOT NULL AND [GrantId] IS NOT NULL AND [RecipientDeviceRegistrationId] IS NOT NULL AND [RecipientSignature] IS NOT NULL)"} | RoadGuardSystem.BusinessObjects.Offline.OfflineSyncBatch |
| dbo.OfflineSyncBatches | CHECK | CK_OfflineSyncBatches_SignedDescriptorJson | {"name": "CK_OfflineSyncBatches_SignedDescriptorJson", "sql": "ISJSON([SignedDescriptorJson])=1"} | RoadGuardSystem.BusinessObjects.Offline.OfflineSyncBatch |
| dbo.OfflineTaskSnapshots | KEY | PK_OfflineTaskSnapshots | {"name": "PK_OfflineTaskSnapshots", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Offline.OfflineTaskSnapshot |
| dbo.OfflineTaskSnapshots | KEY | AK_OfflineTaskSnapshots_Id_ProjectId | {"name": "AK_OfflineTaskSnapshots_Id_ProjectId", "columns": ["Id", "ProjectId"], "primary": false} | RoadGuardSystem.BusinessObjects.Offline.OfflineTaskSnapshot |
| dbo.OfflineTaskSnapshots | INDEX | IX_OfflineTaskSnapshots_AssignmentId | {"name": "IX_OfflineTaskSnapshots_AssignmentId", "columns": ["AssignmentId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineTaskSnapshot |
| dbo.OfflineTaskSnapshots | INDEX | IX_OfflineTaskSnapshots_OriginalActorId | {"name": "IX_OfflineTaskSnapshots_OriginalActorId", "columns": ["OriginalActorId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineTaskSnapshot |
| dbo.OfflineTaskSnapshots | INDEX | IX_OfflineTaskSnapshots_TaskId | {"name": "IX_OfflineTaskSnapshots_TaskId", "columns": ["TaskId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineTaskSnapshot |
| dbo.OfflineTaskSnapshots | INDEX | IX_OfflineTaskSnapshots_DeviceRegistrationId_ProjectId | {"name": "IX_OfflineTaskSnapshots_DeviceRegistrationId_ProjectId", "columns": ["DeviceRegistrationId", "ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineTaskSnapshot |
| dbo.OfflineTaskSnapshots | INDEX | IX_OfflineTaskSnapshots_ProjectId_TaskId_AssignmentId_DeviceRegistrationId | {"name": "IX_OfflineTaskSnapshots_ProjectId_TaskId_AssignmentId_DeviceRegistrationId", "columns": ["ProjectId", "TaskId", "AssignmentId", "DeviceRegistrationId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Offline.OfflineTaskSnapshot |
| dbo.OfflineTaskSnapshots | CHECK | CK_OfflineTaskSnapshots_AssignmentHash | {"name": "CK_OfflineTaskSnapshots_AssignmentHash", "sql": "LEN([AssignmentHash])=64 AND [AssignmentHash] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'"} | RoadGuardSystem.BusinessObjects.Offline.OfflineTaskSnapshot |
| dbo.OfflineTaskSnapshots | CHECK | CK_OfflineTaskSnapshots_ContentHash | {"name": "CK_OfflineTaskSnapshots_ContentHash", "sql": "LEN([ContentHash])=64 AND [ContentHash] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'"} | RoadGuardSystem.BusinessObjects.Offline.OfflineTaskSnapshot |
| dbo.OfflineTaskSnapshots | CHECK | CK_OfflineTaskSnapshots_SnapshotJson | {"name": "CK_OfflineTaskSnapshots_SnapshotJson", "sql": "ISJSON([SnapshotJson])=1"} | RoadGuardSystem.BusinessObjects.Offline.OfflineTaskSnapshot |
| dbo.OutboxMessages | KEY | PK_OutboxMessages | {"name": "PK_OutboxMessages", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Messaging.OutboxMessage |
| dbo.OutboxMessages | INDEX | IX_OutboxMessages_CorrelationId | {"name": "IX_OutboxMessages_CorrelationId", "columns": ["CorrelationId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Messaging.OutboxMessage |
| dbo.OutboxMessages | INDEX | IX_OutboxMessages_OccurredAtUtc | {"name": "IX_OutboxMessages_OccurredAtUtc", "columns": ["OccurredAtUtc"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Messaging.OutboxMessage |
| dbo.OutboxMessages | INDEX | IX_OutboxMessages_DeliveryStatus_NextAttempt | {"name": "IX_OutboxMessages_DeliveryStatus_NextAttempt", "columns": ["DeliveryStatus", "NextAttemptAtUtc"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Messaging.OutboxMessage |
| dbo.OutboxMessages | CHECK | CK_OutboxMessages_DeliveryAttemptCount | {"name": "CK_OutboxMessages_DeliveryAttemptCount", "sql": "[DeliveryAttemptCount] >= 0"} | RoadGuardSystem.BusinessObjects.Messaging.OutboxMessage |
| dbo.OutboxMessages | CHECK | CK_OutboxMessages_DeliveryStatus | {"name": "CK_OutboxMessages_DeliveryStatus", "sql": "[DeliveryStatus] IN (1, 2, 3, 4)"} | RoadGuardSystem.BusinessObjects.Messaging.OutboxMessage |
| dbo.OutboxMessages | CHECK | CK_OutboxMessages_PayloadJson_Json | {"name": "CK_OutboxMessages_PayloadJson_Json", "sql": "ISJSON([PayloadJson]) = 1"} | RoadGuardSystem.BusinessObjects.Messaging.OutboxMessage |
| dbo.PasswordRecoveryRequests | KEY | PK_PasswordRecoveryRequests | {"name": "PK_PasswordRecoveryRequests", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Identity.PasswordRecoveryRequest |
| dbo.PasswordRecoveryRequests | INDEX | IX_PasswordRecoveryRequests_TargetUserId_RequestedAtUtc | {"name": "IX_PasswordRecoveryRequests_TargetUserId_RequestedAtUtc", "columns": ["TargetUserId", "RequestedAtUtc"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Identity.PasswordRecoveryRequest |
| dbo.PasswordResetLogs | KEY | PK_PasswordResetLogs | {"name": "PK_PasswordResetLogs", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Identity.PasswordResetLog |
| dbo.PasswordResetLogs | INDEX | IX_PasswordResetLogs_OccurredAt | {"name": "IX_PasswordResetLogs_OccurredAt", "columns": ["OccurredAt"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Identity.PasswordResetLog |
| dbo.PasswordResetLogs | INDEX | IX_PasswordResetLogs_PerformedByUserId | {"name": "IX_PasswordResetLogs_PerformedByUserId", "columns": ["PerformedByUserId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Identity.PasswordResetLog |
| dbo.PasswordResetLogs | INDEX | IX_PasswordResetLogs_TargetUserId | {"name": "IX_PasswordResetLogs_TargetUserId", "columns": ["TargetUserId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Identity.PasswordResetLog |
| dbo.PasswordResetLogs | CHECK | CK_PasswordResetLogs_Reason_SafeCode | {"name": "CK_PasswordResetLogs_Reason_SafeCode", "sql": "[Reason] IS NULL OR [Reason] IN ('ADMINISTRATOR_INITIATED', 'SELF_SERVICE_ACCOUNT_RECOVERY', 'REGISTRATION_APPROVED', 'SAFETY_POLICY_VIOLATION', 'NO_STATUS_CHANGE', 'ADMINISTRATIVE_LOCK', 'SECURITY_INCIDENT', 'ACCOUNT_REACTIVATED')"} | RoadGuardSystem.BusinessObjects.Identity.PasswordResetLog |
| dbo.PasswordResetLogs | CHECK | CK_PasswordResetLogs_Result | {"name": "CK_PasswordResetLogs_Result", "sql": "[Result] IN (1, 2, 3)"} | RoadGuardSystem.BusinessObjects.Identity.PasswordResetLog |
| dbo.PasswordResetLogs | CHECK | CK_PasswordResetLogs_Source_SafeCode | {"name": "CK_PasswordResetLogs_Source_SafeCode", "sql": "[Source] IN ('ADMIN_API', 'SELF_SERVICE', 'IDENTITY_SERVICE', 'COMPLIANCE_REVIEW', 'SYSTEM')"} | RoadGuardSystem.BusinessObjects.Identity.PasswordResetLog |
| dbo.PavementLayoutRevisions | KEY | PK_PavementLayoutRevisions | {"name": "PK_PavementLayoutRevisions", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Projects.PavementLayoutRevision |
| dbo.PavementLayoutRevisions | INDEX | IX_PavementLayoutRevisions_CreatedBy | {"name": "IX_PavementLayoutRevisions_CreatedBy", "columns": ["CreatedBy"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.PavementLayoutRevision |
| dbo.PavementLayoutRevisions | INDEX | IX_PavementLayoutRevisions_ProjectId | {"name": "IX_PavementLayoutRevisions_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.PavementLayoutRevision |
| dbo.PavementLayoutRevisions | INDEX | IX_PavementLayoutRevisions_SegmentSetId | {"name": "IX_PavementLayoutRevisions_SegmentSetId", "columns": ["SegmentSetId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.PavementLayoutRevision |
| dbo.PavementLayoutRevisions | INDEX | IX_PavementLayoutRevisions_SourcePlanId | {"name": "IX_PavementLayoutRevisions_SourcePlanId", "columns": ["SourcePlanId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.PavementLayoutRevision |
| dbo.PavementLayoutRevisions | INDEX | IX_PavementLayoutRevisions_CrsProfileRevisionId_ProjectId | {"name": "IX_PavementLayoutRevisions_CrsProfileRevisionId_ProjectId", "columns": ["CrsProfileRevisionId", "ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.PavementLayoutRevision |
| dbo.PavementLayoutRevisions | INDEX | IX_PavementLayoutRevisions_RouteVersionId_CreatedAt_Id | {"name": "IX_PavementLayoutRevisions_RouteVersionId_CreatedAt_Id", "columns": ["RouteVersionId", "CreatedAt", "Id"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.PavementLayoutRevision |
| dbo.PavementLayoutRevisions | CHECK | CK_PavementLayoutRevisions_Json | {"name": "CK_PavementLayoutRevisions_Json", "sql": "ISJSON([DefinitionJson])=1 AND ISJSON([SnapshotJson])=1"} | RoadGuardSystem.BusinessObjects.Projects.PavementLayoutRevision |
| dbo.PavementLayoutRevisions | CHECK | CK_PavementLayoutRevisions_Kind | {"name": "CK_PavementLayoutRevisions_Kind", "sql": "[Kind] IN ('PLANNED','AS_BUILT') AND (([Kind]='PLANNED' AND [SourcePlanId] IS NULL) OR ([Kind]='AS_BUILT' AND [SourcePlanId] IS NOT NULL))"} | RoadGuardSystem.BusinessObjects.Projects.PavementLayoutRevision |
| dbo.PavementSourceFileReferences | KEY | PK_PavementSourceFileReferences | {"name": "PK_PavementSourceFileReferences", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Projects.PavementSourceFileReference |
| dbo.PavementSourceFileReferences | INDEX | IX_PavementSourceFileReferences_FileId | {"name": "IX_PavementSourceFileReferences_FileId", "columns": ["FileId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.PavementSourceFileReference |
| dbo.PavementSourceFileReferences | INDEX | IX_PavementSourceFileReferences_LayoutRevisionId_FileId | {"name": "IX_PavementSourceFileReferences_LayoutRevisionId_FileId", "columns": ["LayoutRevisionId", "FileId"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.PavementSourceFileReference |
| dbo.PavementSourceFileReferences | CHECK | CK_PavementSourceFileReferences_Facts | {"name": "CK_PavementSourceFileReferences_Facts", "sql": "LEN([ContentChecksum])=64 AND ISJSON([CaptureFactsJson])=1"} | RoadGuardSystem.BusinessObjects.Projects.PavementSourceFileReference |
| dbo.ProcessingAttempts | KEY | PK_ProcessingAttempts | {"name": "PK_ProcessingAttempts", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Processing.ProcessingAttempt |
| dbo.ProcessingAttempts | INDEX | IX_ProcessingAttempts_JobId | {"name": "IX_ProcessingAttempts_JobId", "columns": ["ProcessingJobId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Processing.ProcessingAttempt |
| dbo.ProcessingAttempts | INDEX | UX_ProcessingAttempts_JobAttemptNo | {"name": "UX_ProcessingAttempts_JobAttemptNo", "columns": ["ProcessingJobId", "AttemptNo"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Processing.ProcessingAttempt |
| dbo.ProcessingAttempts | CHECK | CK_ProcessingAttempts_AttemptNo | {"name": "CK_ProcessingAttempts_AttemptNo", "sql": "[AttemptNo] > 0"} | RoadGuardSystem.BusinessObjects.Processing.ProcessingAttempt |
| dbo.ProcessingAttempts | CHECK | CK_ProcessingAttempts_ErrorType | {"name": "CK_ProcessingAttempts_ErrorType", "sql": "[ErrorType] IS NULL OR [ErrorType] IN (1, 2, 3)"} | RoadGuardSystem.BusinessObjects.Processing.ProcessingAttempt |
| dbo.ProcessingAttempts | CHECK | CK_ProcessingAttempts_TimestampOrder | {"name": "CK_ProcessingAttempts_TimestampOrder", "sql": "[EndedAt] IS NULL OR [EndedAt] >= [StartedAt]"} | RoadGuardSystem.BusinessObjects.Processing.ProcessingAttempt |
| dbo.ProcessingBlocks | KEY | PK_ProcessingBlocks | {"name": "PK_ProcessingBlocks", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Processing.ProcessingBlock |
| dbo.ProcessingBlocks | INDEX | UX_ProcessingBlocks_DataVersionBlockNo | {"name": "UX_ProcessingBlocks_DataVersionBlockNo", "columns": ["SurveyDataVersionId", "BlockNo"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Processing.ProcessingBlock |
| dbo.ProcessingBlocks | CHECK | CK_ProcessingBlocks_BlockNo | {"name": "CK_ProcessingBlocks_BlockNo", "sql": "[BlockNo] > 0"} | RoadGuardSystem.BusinessObjects.Processing.ProcessingBlock |
| dbo.ProcessingBlocks | CHECK | CK_ProcessingBlocks_RangeMetadata_JsonObject | {"name": "CK_ProcessingBlocks_RangeMetadata_JsonObject", "sql": "ISJSON([RangeMetadata]) = 1 AND LEFT(LTRIM([RangeMetadata]), 1) = '{'"} | RoadGuardSystem.BusinessObjects.Processing.ProcessingBlock |
| dbo.ProcessingJobs | KEY | PK_ProcessingJobs | {"name": "PK_ProcessingJobs", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Processing.ProcessingJob |
| dbo.ProcessingJobs | INDEX | IX_ProcessingJobs_ModelVersionId | {"name": "IX_ProcessingJobs_ModelVersionId", "columns": ["ModelVersionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Processing.ProcessingJob |
| dbo.ProcessingJobs | INDEX | IX_ProcessingJobs_Block | {"name": "IX_ProcessingJobs_Block", "columns": ["ProcessingBlockId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Processing.ProcessingJob |
| dbo.ProcessingJobs | INDEX | IX_ProcessingJobs_ProjectStatus | {"name": "IX_ProcessingJobs_ProjectStatus", "columns": ["ProjectId", "Status"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Processing.ProcessingJob |
| dbo.ProcessingJobs | CHECK | CK_ProcessingJobs_Status | {"name": "CK_ProcessingJobs_Status", "sql": "[Status] IN (1, 2, 3, 4, 5, 6)"} | RoadGuardSystem.BusinessObjects.Processing.ProcessingJob |
| dbo.ProcessingJobs | CHECK | CK_ProcessingJobs_TimestampOrder | {"name": "CK_ProcessingJobs_TimestampOrder", "sql": "[CompletedAt] IS NULL OR [StartedAt] IS NULL OR [CompletedAt] >= [StartedAt]"} | RoadGuardSystem.BusinessObjects.Processing.ProcessingJob |
| dbo.ProjectLifecycleHistory | KEY | PK_ProjectLifecycleHistory | {"name": "PK_ProjectLifecycleHistory", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Projects.ProjectLifecycleHistoryRecord |
| dbo.ProjectLifecycleHistory | KEY | AK_ProjectLifecycleHistory_Id_ProjectId | {"name": "AK_ProjectLifecycleHistory_Id_ProjectId", "columns": ["Id", "ProjectId"], "primary": false} | RoadGuardSystem.BusinessObjects.Projects.ProjectLifecycleHistoryRecord |
| dbo.ProjectLifecycleHistory | INDEX | IX_ProjectLifecycleHistory_ActorId | {"name": "IX_ProjectLifecycleHistory_ActorId", "columns": ["ActorId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.ProjectLifecycleHistoryRecord |
| dbo.ProjectLifecycleHistory | INDEX | IX_ProjectLifecycleHistory_DefectId | {"name": "IX_ProjectLifecycleHistory_DefectId", "columns": ["DefectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.ProjectLifecycleHistoryRecord |
| dbo.ProjectLifecycleHistory | INDEX | IX_ProjectLifecycleHistory_LinkedDefectId | {"name": "IX_ProjectLifecycleHistory_LinkedDefectId", "columns": ["LinkedDefectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.ProjectLifecycleHistoryRecord |
| dbo.ProjectLifecycleHistory | INDEX | IX_ProjectLifecycleHistory_ObligationId | {"name": "IX_ProjectLifecycleHistory_ObligationId", "columns": ["ObligationId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.ProjectLifecycleHistoryRecord |
| dbo.ProjectLifecycleHistory | INDEX | IX_ProjectLifecycleHistory_ProjectId | {"name": "IX_ProjectLifecycleHistory_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.ProjectLifecycleHistoryRecord |
| dbo.ProjectLifecycleHistory | INDEX | IX_ProjectLifecycleHistory_ReceiverId | {"name": "IX_ProjectLifecycleHistory_ReceiverId", "columns": ["ReceiverId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.ProjectLifecycleHistoryRecord |
| dbo.ProjectLifecycleHistory | INDEX | IX_ProjectLifecycleHistory_OperationalClosureId_ProjectId | {"name": "IX_ProjectLifecycleHistory_OperationalClosureId_ProjectId", "columns": ["OperationalClosureId", "ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.ProjectLifecycleHistoryRecord |
| dbo.ProjectLifecycleHistory | CHECK | CK_ProjectLifecycleHistory_Facts | {"name": "CK_ProjectLifecycleHistory_Facts", "sql": "ISJSON([FactsJson])=1"} | RoadGuardSystem.BusinessObjects.Projects.ProjectLifecycleHistoryRecord |
| dbo.ProjectLifecycleHistory | CHECK | CK_ProjectLifecycleHistory_Source | {"name": "CK_ProjectLifecycleHistory_Source", "sql": "[Kind] IN (1,2,3,4,5,6,7,8) AND [SourceDisposition] IN ('CANDIDATE','TARGET_CONFIRMED') AND ([SourceDisposition]='CANDIDATE' OR LEN([AuthoritySourceReference])>0)"} | RoadGuardSystem.BusinessObjects.Projects.ProjectLifecycleHistoryRecord |
| dbo.ProjectMembers | KEY | PK_ProjectMembers | {"name": "PK_ProjectMembers", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Projects.ProjectMember |
| dbo.ProjectMembers | INDEX | UX_ProjectMembers_ActivePrimaryProjectManager | {"name": "UX_ProjectMembers_ActivePrimaryProjectManager", "columns": ["ProjectId"], "unique": true, "filter": "[IsPrimary] = 1 AND [Status] = 1"} | RoadGuardSystem.BusinessObjects.Projects.ProjectMember |
| dbo.ProjectMembers | INDEX | IX_ProjectMembers_RoleCode | {"name": "IX_ProjectMembers_RoleCode", "columns": ["RoleCode"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.ProjectMember |
| dbo.ProjectMembers | INDEX | IX_ProjectMembers_UserId | {"name": "IX_ProjectMembers_UserId", "columns": ["UserId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.ProjectMember |
| dbo.ProjectMembers | INDEX | IX_ProjectMembers_ProjectId_UserId | {"name": "IX_ProjectMembers_ProjectId_UserId", "columns": ["ProjectId", "UserId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.ProjectMember |
| dbo.ProjectMembers | CHECK | CK_ProjectMembers_EffectiveDateRange | {"name": "CK_ProjectMembers_EffectiveDateRange", "sql": "[ValidTo] IS NULL OR [ValidTo] >= [ValidFrom]"} | RoadGuardSystem.BusinessObjects.Projects.ProjectMember |
| dbo.ProjectMembers | CHECK | CK_ProjectMembers_PrimaryRole | {"name": "CK_ProjectMembers_PrimaryRole", "sql": "[IsPrimary] = 0 OR [RoleCode] = 'PM'"} | RoadGuardSystem.BusinessObjects.Projects.ProjectMember |
| dbo.ProjectMembers | CHECK | CK_ProjectMembers_Status | {"name": "CK_ProjectMembers_Status", "sql": "[Status] IN (1, 2)"} | RoadGuardSystem.BusinessObjects.Projects.ProjectMember |
| dbo.Projects | KEY | PK_Projects | {"name": "PK_Projects", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Projects.Project |
| dbo.Projects | INDEX | UX_Projects_ProjectCode | {"name": "UX_Projects_ProjectCode", "columns": ["ProjectCode"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.Project |
| dbo.Projects | CHECK | CK_Projects_DateRange | {"name": "CK_Projects_DateRange", "sql": "[EndDate] IS NULL OR [StartDate] IS NULL OR [EndDate] >= [StartDate]"} | RoadGuardSystem.BusinessObjects.Projects.Project |
| dbo.Projects | CHECK | CK_Projects_EngineeringUtmSrid | {"name": "CK_Projects_EngineeringUtmSrid", "sql": "[EngineeringUtmSrid] IS NULL OR [EngineeringUtmSrid] IN (32648, 32649)"} | RoadGuardSystem.BusinessObjects.Projects.Project |
| dbo.Projects | CHECK | CK_Projects_Status | {"name": "CK_Projects_Status", "sql": "[Status] IN (1, 2, 3)"} | RoadGuardSystem.BusinessObjects.Projects.Project |
| dbo.QualityChecks | KEY | PK_QualityChecks | {"name": "PK_QualityChecks", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Surveys.QualityCheck |
| dbo.QualityChecks | INDEX | IX_QualityChecks_InitiatedByUserId | {"name": "IX_QualityChecks_InitiatedByUserId", "columns": ["InitiatedByUserId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Surveys.QualityCheck |
| dbo.QualityChecks | INDEX | IX_QualityChecks_SurveyDataVersionId | {"name": "IX_QualityChecks_SurveyDataVersionId", "columns": ["SurveyDataVersionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Surveys.QualityCheck |
| dbo.QualityChecks | INDEX | IX_QualityChecks_SurveyFileId | {"name": "IX_QualityChecks_SurveyFileId", "columns": ["SurveyFileId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Surveys.QualityCheck |
| dbo.QualityChecks | CHECK | CK_QualityChecks_CheckType | {"name": "CK_QualityChecks_CheckType", "sql": "[CheckType] IN (1, 2, 3, 4, 5, 6, 7, 8, 9)"} | RoadGuardSystem.BusinessObjects.Surveys.QualityCheck |
| dbo.QualityChecks | CHECK | CK_QualityChecks_CheckedBy | {"name": "CK_QualityChecks_CheckedBy", "sql": "[CheckedBy] IN (1, 2)"} | RoadGuardSystem.BusinessObjects.Surveys.QualityCheck |
| dbo.QualityChecks | CHECK | CK_QualityChecks_ExactlyOneTarget | {"name": "CK_QualityChecks_ExactlyOneTarget", "sql": "([Scope] = 1 AND [SurveyFileId] IS NOT NULL AND [SurveyDataVersionId] IS NULL) OR ([Scope] = 2 AND [SurveyFileId] IS NULL AND [SurveyDataVersionId] IS NOT NULL)"} | RoadGuardSystem.BusinessObjects.Surveys.QualityCheck |
| dbo.QualityChecks | CHECK | CK_QualityChecks_ExecutionStage | {"name": "CK_QualityChecks_ExecutionStage", "sql": "[ExecutionStage] IN (1, 2)"} | RoadGuardSystem.BusinessObjects.Surveys.QualityCheck |
| dbo.QualityChecks | CHECK | CK_QualityChecks_MeasuredValue_Json | {"name": "CK_QualityChecks_MeasuredValue_Json", "sql": "[MeasuredValue] IS NULL OR ISJSON([MeasuredValue]) = 1"} | RoadGuardSystem.BusinessObjects.Surveys.QualityCheck |
| dbo.QualityChecks | CHECK | CK_QualityChecks_Scope | {"name": "CK_QualityChecks_Scope", "sql": "[Scope] IN (1, 2)"} | RoadGuardSystem.BusinessObjects.Surveys.QualityCheck |
| dbo.QualityChecks | CHECK | CK_QualityChecks_StageActor | {"name": "CK_QualityChecks_StageActor", "sql": "([ExecutionStage] = 1 AND [CheckedBy] = 1) OR ([ExecutionStage] = 2 AND [CheckedBy] = 2 AND [InitiatedByUserId] IS NULL)"} | RoadGuardSystem.BusinessObjects.Surveys.QualityCheck |
| dbo.QualityChecks | CHECK | CK_QualityChecks_Status | {"name": "CK_QualityChecks_Status", "sql": "[Status] IN (1, 2, 3, 4)"} | RoadGuardSystem.BusinessObjects.Surveys.QualityCheck |
| dbo.QualityChecks | CHECK | CK_QualityChecks_Threshold_Json | {"name": "CK_QualityChecks_Threshold_Json", "sql": "[Threshold] IS NULL OR ISJSON([Threshold]) = 1"} | RoadGuardSystem.BusinessObjects.Surveys.QualityCheck |
| dbo.RefreshTokens | KEY | PK_RefreshTokens | {"name": "PK_RefreshTokens", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Identity.RefreshToken |
| dbo.RefreshTokens | INDEX | IX_RefreshTokens_SessionId | {"name": "IX_RefreshTokens_SessionId", "columns": ["SessionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Identity.RefreshToken |
| dbo.RefreshTokens | INDEX | UX_RefreshTokens_TokenHash | {"name": "UX_RefreshTokens_TokenHash", "columns": ["TokenHash"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Identity.RefreshToken |
| dbo.RefreshTokens | CHECK | CK_RefreshTokens_TokenHash_NotEmpty | {"name": "CK_RefreshTokens_TokenHash_NotEmpty", "sql": "LEN(LTRIM(RTRIM([TokenHash]))) >= 32"} | RoadGuardSystem.BusinessObjects.Identity.RefreshToken |
| dbo.RepairActualScopes | KEY | PK_RepairActualScopes | {"name": "PK_RepairActualScopes", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Repairs.RepairActualScope |
| dbo.RepairActualScopes | INDEX | IX_RepairActualScopes_ObligationId | {"name": "IX_RepairActualScopes_ObligationId", "columns": ["ObligationId"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairActualScope |
| dbo.RepairActualScopes | INDEX | IX_RepairActualScopes_PhysicalRoadId | {"name": "IX_RepairActualScopes_PhysicalRoadId", "columns": ["PhysicalRoadId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairActualScope |
| dbo.RepairAssessmentEvidence | KEY | PK_RepairAssessmentEvidence | {"name": "PK_RepairAssessmentEvidence", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Repairs.RepairAssessmentEvidenceReference |
| dbo.RepairAssessmentEvidence | INDEX | IX_RepairAssessmentEvidence_ActualUploaderId | {"name": "IX_RepairAssessmentEvidence_ActualUploaderId", "columns": ["ActualUploaderId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairAssessmentEvidenceReference |
| dbo.RepairAssessmentEvidence | INDEX | IX_RepairAssessmentEvidence_FileId | {"name": "IX_RepairAssessmentEvidence_FileId", "columns": ["FileId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairAssessmentEvidenceReference |
| dbo.RepairAssessmentEvidence | INDEX | IX_RepairAssessmentEvidence_OriginalActorId | {"name": "IX_RepairAssessmentEvidence_OriginalActorId", "columns": ["OriginalActorId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairAssessmentEvidenceReference |
| dbo.RepairAssessmentEvidence | INDEX | IX_RepairAssessmentEvidence_ReuseDecisionId | {"name": "IX_RepairAssessmentEvidence_ReuseDecisionId", "columns": ["ReuseDecisionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairAssessmentEvidenceReference |
| dbo.RepairAssessmentEvidence | INDEX | IX_RepairAssessmentEvidence_AssessmentId_CaptureOriginId | {"name": "IX_RepairAssessmentEvidence_AssessmentId_CaptureOriginId", "columns": ["AssessmentId", "CaptureOriginId"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairAssessmentEvidenceReference |
| dbo.RepairAssessmentMeasurements | KEY | PK_RepairAssessmentMeasurements | {"name": "PK_RepairAssessmentMeasurements", "columns": ["AssessmentId", "MeasurementId"], "primary": true} | RoadGuardSystem.BusinessObjects.Repairs.RepairAssessmentMeasurementReference |
| dbo.RepairAssessmentMeasurements | INDEX | IX_RepairAssessmentMeasurements_MeasurementId | {"name": "IX_RepairAssessmentMeasurements_MeasurementId", "columns": ["MeasurementId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairAssessmentMeasurementReference |
| dbo.RepairAttemptEvidence | KEY | PK_RepairAttemptEvidence | {"name": "PK_RepairAttemptEvidence", "columns": ["AttemptId", "FileId", "Purpose"], "primary": true} | RoadGuardSystem.BusinessObjects.Repairs.RepairAttempt.Evidence#RepairEvidenceReference |
| dbo.RepairAttemptEvidence | INDEX | IX_RepairAttemptEvidence_FileId | {"name": "IX_RepairAttemptEvidence_FileId", "columns": ["FileId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairAttempt.Evidence#RepairEvidenceReference |
| dbo.RepairAttemptReviews | KEY | PK_RepairAttemptReviews | {"name": "PK_RepairAttemptReviews", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Repairs.RepairAttemptReview |
| dbo.RepairAttemptReviews | INDEX | IX_RepairAttemptReviews_ActorId | {"name": "IX_RepairAttemptReviews_ActorId", "columns": ["ActorId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairAttemptReview |
| dbo.RepairAttemptReviews | INDEX | IX_RepairAttemptReviews_AttemptId | {"name": "IX_RepairAttemptReviews_AttemptId", "columns": ["AttemptId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairAttemptReview |
| dbo.RepairAttemptReviews | INDEX | IX_RepairAttemptReviews_BindingId | {"name": "IX_RepairAttemptReviews_BindingId", "columns": ["BindingId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairAttemptReview |
| dbo.RepairAttemptReviews | INDEX | IX_RepairAttemptReviews_IntakeLinkId | {"name": "IX_RepairAttemptReviews_IntakeLinkId", "columns": ["IntakeLinkId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairAttemptReview |
| dbo.RepairAttemptReviews | INDEX | IX_RepairAttemptReviews_ItemId | {"name": "IX_RepairAttemptReviews_ItemId", "columns": ["ItemId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairAttemptReview |
| dbo.RepairAttemptReviews | INDEX | IX_RepairAttemptReviews_ProjectId | {"name": "IX_RepairAttemptReviews_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairAttemptReview |
| dbo.RepairAttemptReviews | INDEX | IX_RepairAttemptReviews_SubmissionId | {"name": "IX_RepairAttemptReviews_SubmissionId", "columns": ["SubmissionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairAttemptReview |
| dbo.RepairAttempts | KEY | PK_RepairAttempts | {"name": "PK_RepairAttempts", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Repairs.RepairAttempt |
| dbo.RepairAttempts | INDEX | IX_RepairAttempts_AssignmentId | {"name": "IX_RepairAttempts_AssignmentId", "columns": ["AssignmentId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairAttempt |
| dbo.RepairAttempts | INDEX | IX_RepairAttempts_CrewId | {"name": "IX_RepairAttempts_CrewId", "columns": ["CrewId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairAttempt |
| dbo.RepairAttempts | INDEX | IX_RepairAttempts_DefectId | {"name": "IX_RepairAttempts_DefectId", "columns": ["DefectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairAttempt |
| dbo.RepairAttempts | INDEX | IX_RepairAttempts_ItemId | {"name": "IX_RepairAttempts_ItemId", "columns": ["ItemId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairAttempt |
| dbo.RepairAttempts | INDEX | IX_RepairAttempts_ObligationId | {"name": "IX_RepairAttempts_ObligationId", "columns": ["ObligationId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairAttempt |
| dbo.RepairAttempts | INDEX | IX_RepairAttempts_TaskId | {"name": "IX_RepairAttempts_TaskId", "columns": ["TaskId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairAttempt |
| dbo.RepairAttempts | INDEX | IX_RepairAttempts_ProjectId_OriginId | {"name": "IX_RepairAttempts_ProjectId_OriginId", "columns": ["ProjectId", "OriginId"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairAttempt |
| dbo.RepairAttemptSubmissionLinks | KEY | PK_RepairAttemptSubmissionLinks | {"name": "PK_RepairAttemptSubmissionLinks", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Repairs.RepairAttemptSubmissionLink |
| dbo.RepairAttemptSubmissionLinks | INDEX | IX_RepairAttemptSubmissionLinks_AttemptId | {"name": "IX_RepairAttemptSubmissionLinks_AttemptId", "columns": ["AttemptId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairAttemptSubmissionLink |
| dbo.RepairAttemptSubmissionLinks | INDEX | IX_RepairAttemptSubmissionLinks_BindingId | {"name": "IX_RepairAttemptSubmissionLinks_BindingId", "columns": ["BindingId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairAttemptSubmissionLink |
| dbo.RepairAttemptSubmissionLinks | INDEX | IX_RepairAttemptSubmissionLinks_ExecutionFinishId | {"name": "IX_RepairAttemptSubmissionLinks_ExecutionFinishId", "columns": ["ExecutionFinishId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairAttemptSubmissionLink |
| dbo.RepairAttemptSubmissionLinks | INDEX | IX_RepairAttemptSubmissionLinks_FormalRootSubmissionId | {"name": "IX_RepairAttemptSubmissionLinks_FormalRootSubmissionId", "columns": ["FormalRootSubmissionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairAttemptSubmissionLink |
| dbo.RepairAttemptSubmissionLinks | INDEX | IX_RepairAttemptSubmissionLinks_ItemId | {"name": "IX_RepairAttemptSubmissionLinks_ItemId", "columns": ["ItemId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairAttemptSubmissionLink |
| dbo.RepairAttemptSubmissionLinks | INDEX | IX_RepairAttemptSubmissionLinks_PreviousLinkId | {"name": "IX_RepairAttemptSubmissionLinks_PreviousLinkId", "columns": ["PreviousLinkId"], "unique": true, "filter": "[PreviousLinkId] IS NOT NULL"} | RoadGuardSystem.BusinessObjects.Repairs.RepairAttemptSubmissionLink |
| dbo.RepairAttemptSubmissionLinks | INDEX | IX_RepairAttemptSubmissionLinks_ProjectId | {"name": "IX_RepairAttemptSubmissionLinks_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairAttemptSubmissionLink |
| dbo.RepairAttemptSubmissionLinks | INDEX | IX_RepairAttemptSubmissionLinks_ReviewClockId | {"name": "IX_RepairAttemptSubmissionLinks_ReviewClockId", "columns": ["ReviewClockId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairAttemptSubmissionLink |
| dbo.RepairAttemptSubmissionLinks | INDEX | IX_RepairAttemptSubmissionLinks_SubmissionId | {"name": "IX_RepairAttemptSubmissionLinks_SubmissionId", "columns": ["SubmissionId"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairAttemptSubmissionLink |
| dbo.RepairCorrectionEvidence | KEY | PK_RepairCorrectionEvidence | {"name": "PK_RepairCorrectionEvidence", "columns": ["DecisionId", "FileId"], "primary": true} | RoadGuardSystem.BusinessObjects.Repairs.RepairCorrectionBasis.Evidence#RepairEvidenceReference |
| dbo.RepairCorrectionEvidence | INDEX | IX_RepairCorrectionEvidence_FileId | {"name": "IX_RepairCorrectionEvidence_FileId", "columns": ["FileId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairCorrectionBasis.Evidence#RepairEvidenceReference |
| dbo.RepairDangerAcknowledgements | KEY | PK_RepairDangerAcknowledgements | {"name": "PK_RepairDangerAcknowledgements", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Repairs.RepairDangerAcknowledgement |
| dbo.RepairDangerAcknowledgements | INDEX | IX_RepairDangerAcknowledgements_ActorId | {"name": "IX_RepairDangerAcknowledgements_ActorId", "columns": ["ActorId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairDangerAcknowledgement |
| dbo.RepairDangerAcknowledgements | INDEX | IX_RepairDangerAcknowledgements_MonitoringId | {"name": "IX_RepairDangerAcknowledgements_MonitoringId", "columns": ["MonitoringId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairDangerAcknowledgement |
| dbo.RepairDangerAcknowledgements | INDEX | IX_RepairDangerAcknowledgements_WarningId | {"name": "IX_RepairDangerAcknowledgements_WarningId", "columns": ["WarningId"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairDangerAcknowledgement |
| dbo.RepairDangerWarnings | KEY | PK_RepairDangerWarnings | {"name": "PK_RepairDangerWarnings", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Repairs.RepairDangerWarning |
| dbo.RepairDangerWarnings | INDEX | IX_RepairDangerWarnings_ResponsibleActorId | {"name": "IX_RepairDangerWarnings_ResponsibleActorId", "columns": ["ResponsibleActorId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairDangerWarning |
| dbo.RepairDangerWarnings | INDEX | IX_RepairDangerWarnings_MonitoringId_SourceId | {"name": "IX_RepairDangerWarnings_MonitoringId_SourceId", "columns": ["MonitoringId", "SourceId"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairDangerWarning |
| dbo.RepairDecisions | KEY | PK_RepairDecisions | {"name": "PK_RepairDecisions", "columns": ["RepairDecisionId"], "primary": true} | RoadGuardSystem.BusinessObjects.Repairs.RepairCorrectionBasis |
| dbo.RepairDecisions | KEY | PK_RepairDecisions | {"name": "PK_RepairDecisions", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Repairs.RepairDecision |
| dbo.RepairDecisions | INDEX | IX_RepairDecisions_ActorId | {"name": "IX_RepairDecisions_ActorId", "columns": ["ActorId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairDecision |
| dbo.RepairDecisions | INDEX | IX_RepairDecisions_DefectId | {"name": "IX_RepairDecisions_DefectId", "columns": ["DefectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairDecision |
| dbo.RepairDecisions | INDEX | IX_RepairDecisions_ItemId | {"name": "IX_RepairDecisions_ItemId", "columns": ["ItemId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairDecision |
| dbo.RepairDecisions | INDEX | IX_RepairDecisions_ObligationId | {"name": "IX_RepairDecisions_ObligationId", "columns": ["ObligationId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairDecision |
| dbo.RepairDecisions | INDEX | IX_RepairDecisions_PreviousObligationHeadDecisionId | {"name": "IX_RepairDecisions_PreviousObligationHeadDecisionId", "columns": ["PreviousObligationHeadDecisionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairDecision |
| dbo.RepairDecisions | INDEX | IX_RepairDecisions_SupersedesDecisionId | {"name": "IX_RepairDecisions_SupersedesDecisionId", "columns": ["SupersedesDecisionId"], "unique": true, "filter": "[SupersedesDecisionId] IS NOT NULL"} | RoadGuardSystem.BusinessObjects.Repairs.RepairDecision |
| dbo.RepairEligibilityAssessments | KEY | PK_RepairEligibilityAssessments | {"name": "PK_RepairEligibilityAssessments", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Repairs.RepairEligibilityAssessment |
| dbo.RepairEligibilityAssessments | INDEX | IX_RepairEligibilityAssessments_AssessmentId | {"name": "IX_RepairEligibilityAssessments_AssessmentId", "columns": ["AssessmentId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairEligibilityAssessment |
| dbo.RepairEligibilityAssessments | INDEX | IX_RepairEligibilityAssessments_BindingId | {"name": "IX_RepairEligibilityAssessments_BindingId", "columns": ["BindingId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairEligibilityAssessment |
| dbo.RepairEligibilityAssessments | INDEX | IX_RepairEligibilityAssessments_ItemId | {"name": "IX_RepairEligibilityAssessments_ItemId", "columns": ["ItemId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairEligibilityAssessment |
| dbo.RepairEligibilityAssessments | INDEX | IX_RepairEligibilityAssessments_PolicyRevisionId | {"name": "IX_RepairEligibilityAssessments_PolicyRevisionId", "columns": ["PolicyRevisionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairEligibilityAssessment |
| dbo.RepairEligibilityAssessments | INDEX | IX_RepairEligibilityAssessments_ProjectId | {"name": "IX_RepairEligibilityAssessments_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairEligibilityAssessment |
| dbo.RepairEligibilityAssessments | INDEX | IX_RepairEligibilityAssessments_RoadSectionId | {"name": "IX_RepairEligibilityAssessments_RoadSectionId", "columns": ["RoadSectionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairEligibilityAssessment |
| dbo.RepairEligibilityHandoverSources | KEY | PK_RepairEligibilityHandoverSources | {"name": "PK_RepairEligibilityHandoverSources", "columns": ["EligibilityAssessmentId", "HandoverDocumentId"], "primary": true} | RoadGuardSystem.BusinessObjects.Repairs.RepairEligibilityHandoverSource |
| dbo.RepairEligibilityHandoverSources | INDEX | IX_RepairEligibilityHandoverSources_FileId | {"name": "IX_RepairEligibilityHandoverSources_FileId", "columns": ["FileId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairEligibilityHandoverSource |
| dbo.RepairEligibilityHandoverSources | INDEX | IX_RepairEligibilityHandoverSources_HandoverDocumentId | {"name": "IX_RepairEligibilityHandoverSources_HandoverDocumentId", "columns": ["HandoverDocumentId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairEligibilityHandoverSource |
| dbo.RepairEligibilityWarrantySources | KEY | PK_RepairEligibilityWarrantySources | {"name": "PK_RepairEligibilityWarrantySources", "columns": ["EligibilityAssessmentId", "WarrantyId"], "primary": true} | RoadGuardSystem.BusinessObjects.Repairs.RepairEligibilityWarrantySource |
| dbo.RepairEligibilityWarrantySources | INDEX | IX_RepairEligibilityWarrantySources_SourceDocumentId | {"name": "IX_RepairEligibilityWarrantySources_SourceDocumentId", "columns": ["SourceDocumentId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairEligibilityWarrantySource |
| dbo.RepairEligibilityWarrantySources | INDEX | IX_RepairEligibilityWarrantySources_WarrantyId | {"name": "IX_RepairEligibilityWarrantySources_WarrantyId", "columns": ["WarrantyId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairEligibilityWarrantySource |
| dbo.RepairExecutionAuthorizations | KEY | PK_RepairExecutionAuthorizations | {"name": "PK_RepairExecutionAuthorizations", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Repairs.RepairExecutionAuthorization |
| dbo.RepairExecutionAuthorizations | INDEX | IX_RepairExecutionAuthorizations_AssignmentId | {"name": "IX_RepairExecutionAuthorizations_AssignmentId", "columns": ["AssignmentId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairExecutionAuthorization |
| dbo.RepairExecutionAuthorizations | INDEX | IX_RepairExecutionAuthorizations_CrewId | {"name": "IX_RepairExecutionAuthorizations_CrewId", "columns": ["CrewId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairExecutionAuthorization |
| dbo.RepairExecutionAuthorizations | INDEX | IX_RepairExecutionAuthorizations_DefectId | {"name": "IX_RepairExecutionAuthorizations_DefectId", "columns": ["DefectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairExecutionAuthorization |
| dbo.RepairExecutionAuthorizations | INDEX | IX_RepairExecutionAuthorizations_IssuedBy | {"name": "IX_RepairExecutionAuthorizations_IssuedBy", "columns": ["IssuedBy"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairExecutionAuthorization |
| dbo.RepairExecutionAuthorizations | INDEX | IX_RepairExecutionAuthorizations_PolicyRevisionId | {"name": "IX_RepairExecutionAuthorizations_PolicyRevisionId", "columns": ["PolicyRevisionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairExecutionAuthorization |
| dbo.RepairExecutionAuthorizations | INDEX | IX_RepairExecutionAuthorizations_ProjectId | {"name": "IX_RepairExecutionAuthorizations_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairExecutionAuthorization |
| dbo.RepairExecutionAuthorizations | INDEX | IX_RepairExecutionAuthorizations_TaskId | {"name": "IX_RepairExecutionAuthorizations_TaskId", "columns": ["TaskId"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairExecutionAuthorization |
| dbo.RepairExecutionFinishes | KEY | PK_RepairExecutionFinishes | {"name": "PK_RepairExecutionFinishes", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Repairs.RepairExecutionFinish |
| dbo.RepairExecutionFinishes | INDEX | IX_RepairExecutionFinishes_BindingId | {"name": "IX_RepairExecutionFinishes_BindingId", "columns": ["BindingId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairExecutionFinish |
| dbo.RepairExecutionFinishes | INDEX | IX_RepairExecutionFinishes_ExecutionStartId | {"name": "IX_RepairExecutionFinishes_ExecutionStartId", "columns": ["ExecutionStartId"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairExecutionFinish |
| dbo.RepairExecutionFinishes | INDEX | IX_RepairExecutionFinishes_ItemId | {"name": "IX_RepairExecutionFinishes_ItemId", "columns": ["ItemId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairExecutionFinish |
| dbo.RepairExecutionFinishes | INDEX | IX_RepairExecutionFinishes_OperationOriginId | {"name": "IX_RepairExecutionFinishes_OperationOriginId", "columns": ["OperationOriginId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairExecutionFinish |
| dbo.RepairExecutionFinishes | INDEX | IX_RepairExecutionFinishes_OriginalActorId | {"name": "IX_RepairExecutionFinishes_OriginalActorId", "columns": ["OriginalActorId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairExecutionFinish |
| dbo.RepairExecutionFinishes | INDEX | IX_RepairExecutionFinishes_ProjectId_OriginId | {"name": "IX_RepairExecutionFinishes_ProjectId_OriginId", "columns": ["ProjectId", "OriginId"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairExecutionFinish |
| dbo.RepairExecutionStarts | KEY | PK_RepairExecutionStarts | {"name": "PK_RepairExecutionStarts", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Repairs.RepairExecutionStart |
| dbo.RepairExecutionStarts | INDEX | IX_RepairExecutionStarts_AssessmentId | {"name": "IX_RepairExecutionStarts_AssessmentId", "columns": ["AssessmentId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairExecutionStart |
| dbo.RepairExecutionStarts | INDEX | IX_RepairExecutionStarts_BindingId | {"name": "IX_RepairExecutionStarts_BindingId", "columns": ["BindingId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairExecutionStart |
| dbo.RepairExecutionStarts | INDEX | IX_RepairExecutionStarts_EligibilityAssessmentId | {"name": "IX_RepairExecutionStarts_EligibilityAssessmentId", "columns": ["EligibilityAssessmentId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairExecutionStart |
| dbo.RepairExecutionStarts | INDEX | IX_RepairExecutionStarts_FirstStartId | {"name": "IX_RepairExecutionStarts_FirstStartId", "columns": ["FirstStartId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairExecutionStart |
| dbo.RepairExecutionStarts | INDEX | IX_RepairExecutionStarts_ItemId | {"name": "IX_RepairExecutionStarts_ItemId", "columns": ["ItemId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairExecutionStart |
| dbo.RepairExecutionStarts | INDEX | IX_RepairExecutionStarts_OperationOriginId | {"name": "IX_RepairExecutionStarts_OperationOriginId", "columns": ["OperationOriginId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairExecutionStart |
| dbo.RepairExecutionStarts | INDEX | IX_RepairExecutionStarts_OriginalActorId | {"name": "IX_RepairExecutionStarts_OriginalActorId", "columns": ["OriginalActorId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairExecutionStart |
| dbo.RepairExecutionStarts | INDEX | IX_RepairExecutionStarts_ProjectId_OriginId | {"name": "IX_RepairExecutionStarts_ProjectId_OriginId", "columns": ["ProjectId", "OriginId"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairExecutionStart |
| dbo.RepairFieldTaskBindings | KEY | PK_RepairFieldTaskBindings | {"name": "PK_RepairFieldTaskBindings", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Repairs.RepairFieldTaskBinding |
| dbo.RepairFieldTaskBindings | INDEX | IX_RepairFieldTaskBindings_AssignedBy | {"name": "IX_RepairFieldTaskBindings_AssignedBy", "columns": ["AssignedBy"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairFieldTaskBinding |
| dbo.RepairFieldTaskBindings | INDEX | IX_RepairFieldTaskBindings_AssignmentId | {"name": "IX_RepairFieldTaskBindings_AssignmentId", "columns": ["AssignmentId"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairFieldTaskBinding |
| dbo.RepairFieldTaskBindings | INDEX | IX_RepairFieldTaskBindings_AuthorizationId | {"name": "IX_RepairFieldTaskBindings_AuthorizationId", "columns": ["AuthorizationId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairFieldTaskBinding |
| dbo.RepairFieldTaskBindings | INDEX | IX_RepairFieldTaskBindings_CrewId | {"name": "IX_RepairFieldTaskBindings_CrewId", "columns": ["CrewId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairFieldTaskBinding |
| dbo.RepairFieldTaskBindings | INDEX | IX_RepairFieldTaskBindings_CrsProfileRevisionId | {"name": "IX_RepairFieldTaskBindings_CrsProfileRevisionId", "columns": ["CrsProfileRevisionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairFieldTaskBinding |
| dbo.RepairFieldTaskBindings | INDEX | IX_RepairFieldTaskBindings_DefectId | {"name": "IX_RepairFieldTaskBindings_DefectId", "columns": ["DefectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairFieldTaskBinding |
| dbo.RepairFieldTaskBindings | INDEX | IX_RepairFieldTaskBindings_ItemId | {"name": "IX_RepairFieldTaskBindings_ItemId", "columns": ["ItemId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairFieldTaskBinding |
| dbo.RepairFieldTaskBindings | INDEX | IX_RepairFieldTaskBindings_LayoutRevisionId | {"name": "IX_RepairFieldTaskBindings_LayoutRevisionId", "columns": ["LayoutRevisionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairFieldTaskBinding |
| dbo.RepairFieldTaskBindings | INDEX | IX_RepairFieldTaskBindings_MapPublicationId | {"name": "IX_RepairFieldTaskBindings_MapPublicationId", "columns": ["MapPublicationId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairFieldTaskBinding |
| dbo.RepairFieldTaskBindings | INDEX | IX_RepairFieldTaskBindings_ObligationId | {"name": "IX_RepairFieldTaskBindings_ObligationId", "columns": ["ObligationId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairFieldTaskBinding |
| dbo.RepairFieldTaskBindings | INDEX | IX_RepairFieldTaskBindings_PolicyRevisionId | {"name": "IX_RepairFieldTaskBindings_PolicyRevisionId", "columns": ["PolicyRevisionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairFieldTaskBinding |
| dbo.RepairFieldTaskBindings | INDEX | IX_RepairFieldTaskBindings_ProjectId | {"name": "IX_RepairFieldTaskBindings_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairFieldTaskBinding |
| dbo.RepairFieldTaskBindings | INDEX | IX_RepairFieldTaskBindings_RouteVersionId | {"name": "IX_RepairFieldTaskBindings_RouteVersionId", "columns": ["RouteVersionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairFieldTaskBinding |
| dbo.RepairFieldTaskBindings | INDEX | IX_RepairFieldTaskBindings_SegmentSetId | {"name": "IX_RepairFieldTaskBindings_SegmentSetId", "columns": ["SegmentSetId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairFieldTaskBinding |
| dbo.RepairFieldTaskBindings | INDEX | IX_RepairFieldTaskBindings_TaskId | {"name": "IX_RepairFieldTaskBindings_TaskId", "columns": ["TaskId"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairFieldTaskBinding |
| dbo.RepairItemLifecycleEvents | KEY | PK_RepairItemLifecycleEvents | {"name": "PK_RepairItemLifecycleEvents", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Repairs.RepairItemLifecycleEvent |
| dbo.RepairItemLifecycleEvents | INDEX | IX_RepairItemLifecycleEvents_ActorId | {"name": "IX_RepairItemLifecycleEvents_ActorId", "columns": ["ActorId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairItemLifecycleEvent |
| dbo.RepairItemLifecycleEvents | INDEX | IX_RepairItemLifecycleEvents_AttemptId | {"name": "IX_RepairItemLifecycleEvents_AttemptId", "columns": ["AttemptId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairItemLifecycleEvent |
| dbo.RepairItemLifecycleEvents | INDEX | IX_RepairItemLifecycleEvents_BindingId | {"name": "IX_RepairItemLifecycleEvents_BindingId", "columns": ["BindingId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairItemLifecycleEvent |
| dbo.RepairItemLifecycleEvents | INDEX | IX_RepairItemLifecycleEvents_DecisionId | {"name": "IX_RepairItemLifecycleEvents_DecisionId", "columns": ["DecisionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairItemLifecycleEvent |
| dbo.RepairItemLifecycleEvents | INDEX | IX_RepairItemLifecycleEvents_DefectId | {"name": "IX_RepairItemLifecycleEvents_DefectId", "columns": ["DefectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairItemLifecycleEvent |
| dbo.RepairItemLifecycleEvents | INDEX | IX_RepairItemLifecycleEvents_ItemId | {"name": "IX_RepairItemLifecycleEvents_ItemId", "columns": ["ItemId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairItemLifecycleEvent |
| dbo.RepairItemLifecycleEvents | INDEX | IX_RepairItemLifecycleEvents_ObligationId | {"name": "IX_RepairItemLifecycleEvents_ObligationId", "columns": ["ObligationId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairItemLifecycleEvent |
| dbo.RepairItemLifecycleEvents | INDEX | IX_RepairItemLifecycleEvents_ProjectId | {"name": "IX_RepairItemLifecycleEvents_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairItemLifecycleEvent |
| dbo.RepairItemLifecycleEvents | INDEX | IX_RepairItemLifecycleEvents_ReviewId | {"name": "IX_RepairItemLifecycleEvents_ReviewId", "columns": ["ReviewId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairItemLifecycleEvent |
| dbo.RepairItemLifecycleEvents | INDEX | IX_RepairItemLifecycleEvents_SubmissionId | {"name": "IX_RepairItemLifecycleEvents_SubmissionId", "columns": ["SubmissionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairItemLifecycleEvent |
| dbo.RepairItems | KEY | PK_RepairItems | {"name": "PK_RepairItems", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Repairs.RepairItem |
| dbo.RepairItems | INDEX | IX_RepairItems_CrewId | {"name": "IX_RepairItems_CrewId", "columns": ["CrewId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairItem |
| dbo.RepairItems | INDEX | IX_RepairItems_CurrentAssessmentId | {"name": "IX_RepairItems_CurrentAssessmentId", "columns": ["CurrentAssessmentId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairItem |
| dbo.RepairItems | INDEX | IX_RepairItems_CurrentAttemptId | {"name": "IX_RepairItems_CurrentAttemptId", "columns": ["CurrentAttemptId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairItem |
| dbo.RepairItems | INDEX | IX_RepairItems_CurrentBindingId | {"name": "IX_RepairItems_CurrentBindingId", "columns": ["CurrentBindingId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairItem |
| dbo.RepairItems | INDEX | IX_RepairItems_CurrentExecutionFinishId | {"name": "IX_RepairItems_CurrentExecutionFinishId", "columns": ["CurrentExecutionFinishId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairItem |
| dbo.RepairItems | INDEX | IX_RepairItems_CurrentExecutionStartId | {"name": "IX_RepairItems_CurrentExecutionStartId", "columns": ["CurrentExecutionStartId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairItem |
| dbo.RepairItems | INDEX | IX_RepairItems_CurrentIntakeLinkId | {"name": "IX_RepairItems_CurrentIntakeLinkId", "columns": ["CurrentIntakeLinkId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairItem |
| dbo.RepairItems | INDEX | IX_RepairItems_CurrentReviewId | {"name": "IX_RepairItems_CurrentReviewId", "columns": ["CurrentReviewId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairItem |
| dbo.RepairItems | INDEX | IX_RepairItems_DefectId | {"name": "IX_RepairItems_DefectId", "columns": ["DefectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairItem |
| dbo.RepairItems | INDEX | IX_RepairItems_EffectiveDecisionId | {"name": "IX_RepairItems_EffectiveDecisionId", "columns": ["EffectiveDecisionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairItem |
| dbo.RepairItems | INDEX | IX_RepairItems_EffectiveIntakeSubmissionId | {"name": "IX_RepairItems_EffectiveIntakeSubmissionId", "columns": ["EffectiveIntakeSubmissionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairItem |
| dbo.RepairItems | INDEX | IX_RepairItems_ObligationId | {"name": "IX_RepairItems_ObligationId", "columns": ["ObligationId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairItem |
| dbo.RepairItems | INDEX | IX_RepairItems_PackageId | {"name": "IX_RepairItems_PackageId", "columns": ["PackageId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairItem |
| dbo.RepairItems | INDEX | IX_RepairItems_PredecessorItemId | {"name": "IX_RepairItems_PredecessorItemId", "columns": ["PredecessorItemId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairItem |
| dbo.RepairItems | INDEX | IX_RepairItems_ProjectId | {"name": "IX_RepairItems_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairItem |
| dbo.RepairItems | INDEX | IX_RepairItems_SupersededByItemId | {"name": "IX_RepairItems_SupersededByItemId", "columns": ["SupersededByItemId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairItem |
| dbo.RepairMeasurementAssessments | KEY | PK_RepairMeasurementAssessments | {"name": "PK_RepairMeasurementAssessments", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Repairs.RepairMeasurementAssessment |
| dbo.RepairMeasurementAssessments | INDEX | IX_RepairMeasurementAssessments_AssignmentId | {"name": "IX_RepairMeasurementAssessments_AssignmentId", "columns": ["AssignmentId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairMeasurementAssessment |
| dbo.RepairMeasurementAssessments | INDEX | IX_RepairMeasurementAssessments_BindingId | {"name": "IX_RepairMeasurementAssessments_BindingId", "columns": ["BindingId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairMeasurementAssessment |
| dbo.RepairMeasurementAssessments | INDEX | IX_RepairMeasurementAssessments_FirstStartId | {"name": "IX_RepairMeasurementAssessments_FirstStartId", "columns": ["FirstStartId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairMeasurementAssessment |
| dbo.RepairMeasurementAssessments | INDEX | IX_RepairMeasurementAssessments_FormalSourceSubmissionId | {"name": "IX_RepairMeasurementAssessments_FormalSourceSubmissionId", "columns": ["FormalSourceSubmissionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairMeasurementAssessment |
| dbo.RepairMeasurementAssessments | INDEX | IX_RepairMeasurementAssessments_ItemId | {"name": "IX_RepairMeasurementAssessments_ItemId", "columns": ["ItemId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairMeasurementAssessment |
| dbo.RepairMeasurementAssessments | INDEX | IX_RepairMeasurementAssessments_OperationOriginId | {"name": "IX_RepairMeasurementAssessments_OperationOriginId", "columns": ["OperationOriginId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairMeasurementAssessment |
| dbo.RepairMeasurementAssessments | INDEX | IX_RepairMeasurementAssessments_OriginalActorId | {"name": "IX_RepairMeasurementAssessments_OriginalActorId", "columns": ["OriginalActorId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairMeasurementAssessment |
| dbo.RepairMeasurementAssessments | INDEX | IX_RepairMeasurementAssessments_SessionId | {"name": "IX_RepairMeasurementAssessments_SessionId", "columns": ["SessionId"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairMeasurementAssessment |
| dbo.RepairMeasurementAssessments | INDEX | IX_RepairMeasurementAssessments_TaskId | {"name": "IX_RepairMeasurementAssessments_TaskId", "columns": ["TaskId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairMeasurementAssessment |
| dbo.RepairMeasurementAssessments | INDEX | IX_RepairMeasurementAssessments_ProjectId_OriginId | {"name": "IX_RepairMeasurementAssessments_ProjectId_OriginId", "columns": ["ProjectId", "OriginId"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairMeasurementAssessment |
| dbo.RepairNormalSuccessors | KEY | PK_RepairNormalSuccessors | {"name": "PK_RepairNormalSuccessors", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Repairs.RepairNormalSuccessor |
| dbo.RepairNormalSuccessors | INDEX | IX_RepairNormalSuccessors_ActorId | {"name": "IX_RepairNormalSuccessors_ActorId", "columns": ["ActorId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairNormalSuccessor |
| dbo.RepairNormalSuccessors | INDEX | IX_RepairNormalSuccessors_ObligationId | {"name": "IX_RepairNormalSuccessors_ObligationId", "columns": ["ObligationId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairNormalSuccessor |
| dbo.RepairNormalSuccessors | INDEX | IX_RepairNormalSuccessors_ProjectId | {"name": "IX_RepairNormalSuccessors_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairNormalSuccessor |
| dbo.RepairNormalSuccessors | INDEX | IX_RepairNormalSuccessors_SourceAssessmentId | {"name": "IX_RepairNormalSuccessors_SourceAssessmentId", "columns": ["SourceAssessmentId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairNormalSuccessor |
| dbo.RepairNormalSuccessors | INDEX | IX_RepairNormalSuccessors_SourceCancellationEventId | {"name": "IX_RepairNormalSuccessors_SourceCancellationEventId", "columns": ["SourceCancellationEventId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairNormalSuccessor |
| dbo.RepairNormalSuccessors | INDEX | IX_RepairNormalSuccessors_SourceDecisionId | {"name": "IX_RepairNormalSuccessors_SourceDecisionId", "columns": ["SourceDecisionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairNormalSuccessor |
| dbo.RepairNormalSuccessors | INDEX | IX_RepairNormalSuccessors_SourceHandoverEventId | {"name": "IX_RepairNormalSuccessors_SourceHandoverEventId", "columns": ["SourceHandoverEventId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairNormalSuccessor |
| dbo.RepairNormalSuccessors | INDEX | IX_RepairNormalSuccessors_SourceItemId | {"name": "IX_RepairNormalSuccessors_SourceItemId", "columns": ["SourceItemId"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairNormalSuccessor |
| dbo.RepairNormalSuccessors | INDEX | IX_RepairNormalSuccessors_TargetItemId | {"name": "IX_RepairNormalSuccessors_TargetItemId", "columns": ["TargetItemId"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairNormalSuccessor |
| dbo.RepairObligationResolutionEvents | KEY | PK_RepairObligationResolutionEvents | {"name": "PK_RepairObligationResolutionEvents", "columns": ["DecisionId"], "primary": true} | RoadGuardSystem.BusinessObjects.Repairs.RepairObligationResolutionEvent |
| dbo.RepairObligationResolutionEvents | INDEX | IX_RepairObligationResolutionEvents_ObligationId | {"name": "IX_RepairObligationResolutionEvents_ObligationId", "columns": ["ObligationId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairObligationResolutionEvent |
| dbo.RepairObligationResolutionEvents | INDEX | IX_RepairObligationResolutionEvents_PreviousHeadDecisionId | {"name": "IX_RepairObligationResolutionEvents_PreviousHeadDecisionId", "columns": ["PreviousHeadDecisionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairObligationResolutionEvent |
| dbo.RepairObligationResolutionEvents | INDEX | IX_RepairObligationResolutionEvents_SupersedesDecisionId | {"name": "IX_RepairObligationResolutionEvents_SupersedesDecisionId", "columns": ["SupersedesDecisionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairObligationResolutionEvent |
| dbo.RepairObligations | KEY | PK_RepairObligations | {"name": "PK_RepairObligations", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Repairs.RepairObligation |
| dbo.RepairObligations | INDEX | IX_RepairObligations_CurrentRepairItemId | {"name": "IX_RepairObligations_CurrentRepairItemId", "columns": ["CurrentRepairItemId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairObligation |
| dbo.RepairObligations | INDEX | IX_RepairObligations_DefectId | {"name": "IX_RepairObligations_DefectId", "columns": ["DefectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairObligation |
| dbo.RepairObligations | INDEX | IX_RepairObligations_EffectiveResolutionDecisionId | {"name": "IX_RepairObligations_EffectiveResolutionDecisionId", "columns": ["EffectiveResolutionDecisionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairObligation |
| dbo.RepairObligations | INDEX | IX_RepairObligations_EffectiveResolutionHeadDecisionId | {"name": "IX_RepairObligations_EffectiveResolutionHeadDecisionId", "columns": ["EffectiveResolutionHeadDecisionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairObligation |
| dbo.RepairObligations | INDEX | IX_RepairObligations_OriginalCrewFirstStartId | {"name": "IX_RepairObligations_OriginalCrewFirstStartId", "columns": ["OriginalCrewFirstStartId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairObligation |
| dbo.RepairObligations | INDEX | IX_RepairObligations_PackageId | {"name": "IX_RepairObligations_PackageId", "columns": ["PackageId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairObligation |
| dbo.RepairObligations | INDEX | IX_RepairObligations_ProjectId | {"name": "IX_RepairObligations_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairObligation |
| dbo.RepairPackages | KEY | PK_RepairPackages | {"name": "PK_RepairPackages", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Repairs.RepairPackage |
| dbo.RepairPackages | INDEX | IX_RepairPackages_DefectId | {"name": "IX_RepairPackages_DefectId", "columns": ["DefectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairPackage |
| dbo.RepairPackages | INDEX | IX_RepairPackages_ProjectId | {"name": "IX_RepairPackages_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairPackage |
| dbo.RepairPolicyDraftChanges | KEY | PK_RepairPolicyDraftChanges | {"name": "PK_RepairPolicyDraftChanges", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Repairs.RepairPolicyDraftChange |
| dbo.RepairPolicyDraftChanges | INDEX | IX_RepairPolicyDraftChanges_ActorId | {"name": "IX_RepairPolicyDraftChanges_ActorId", "columns": ["ActorId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairPolicyDraftChange |
| dbo.RepairPolicyDraftChanges | INDEX | IX_RepairPolicyDraftChanges_DraftId | {"name": "IX_RepairPolicyDraftChanges_DraftId", "columns": ["DraftId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairPolicyDraftChange |
| dbo.RepairPolicyDrafts | KEY | PK_RepairPolicyDrafts | {"name": "PK_RepairPolicyDrafts", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Repairs.RepairPolicyDraft |
| dbo.RepairPolicyDrafts | INDEX | IX_RepairPolicyDrafts_CurrentChangeId | {"name": "IX_RepairPolicyDrafts_CurrentChangeId", "columns": ["CurrentChangeId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairPolicyDraft |
| dbo.RepairPolicyDrafts | INDEX | IX_RepairPolicyDrafts_ProjectId | {"name": "IX_RepairPolicyDrafts_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairPolicyDraft |
| dbo.RepairPolicyDrafts | INDEX | IX_RepairPolicyDrafts_PublishedRevisionId | {"name": "IX_RepairPolicyDrafts_PublishedRevisionId", "columns": ["PublishedRevisionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairPolicyDraft |
| dbo.RepairPolicyMeasurementRules | KEY | PK_RepairPolicyMeasurementRules | {"name": "PK_RepairPolicyMeasurementRules", "columns": ["PolicyRevisionId", "Code"], "primary": true} | RoadGuardSystem.BusinessObjects.Repairs.RepairMeasurementRule |
| dbo.RepairPolicyRevisions | KEY | PK_RepairPolicyRevisions | {"name": "PK_RepairPolicyRevisions", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Repairs.RepairPolicyRevision |
| dbo.RepairPolicyRevisions | INDEX | IX_RepairPolicyRevisions_PublishedBy | {"name": "IX_RepairPolicyRevisions_PublishedBy", "columns": ["PublishedBy"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairPolicyRevision |
| dbo.RepairPolicyRevisions | INDEX | IX_RepairPolicyRevisions_ProjectId_Revision | {"name": "IX_RepairPolicyRevisions_ProjectId_Revision", "columns": ["ProjectId", "Revision"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairPolicyRevision |
| dbo.RepairPolicyRevocations | KEY | PK_RepairPolicyRevocations | {"name": "PK_RepairPolicyRevocations", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Repairs.RepairPolicyRevocation |
| dbo.RepairPolicyRevocations | INDEX | IX_RepairPolicyRevocations_ActorId | {"name": "IX_RepairPolicyRevocations_ActorId", "columns": ["ActorId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairPolicyRevocation |
| dbo.RepairPolicyRevocations | INDEX | IX_RepairPolicyRevocations_PolicyRevisionId | {"name": "IX_RepairPolicyRevocations_PolicyRevisionId", "columns": ["PolicyRevisionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairPolicyRevocation |
| dbo.RepairReviewRequests | KEY | PK_RepairReviewRequests | {"name": "PK_RepairReviewRequests", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Repairs.RepairReviewRequest |
| dbo.RepairReviewRequests | INDEX | IX_RepairReviewRequests_ActorId | {"name": "IX_RepairReviewRequests_ActorId", "columns": ["ActorId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairReviewRequest |
| dbo.RepairReviewRequests | INDEX | IX_RepairReviewRequests_DecisionId | {"name": "IX_RepairReviewRequests_DecisionId", "columns": ["DecisionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairReviewRequest |
| dbo.RepairReviewRequests | INDEX | IX_RepairReviewRequests_ItemId | {"name": "IX_RepairReviewRequests_ItemId", "columns": ["ItemId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairReviewRequest |
| dbo.RepairReviewRequests | INDEX | IX_RepairReviewRequests_ProjectId | {"name": "IX_RepairReviewRequests_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairReviewRequest |
| dbo.RepairSafetyActionSources | KEY | PK_RepairSafetyActionSources | {"name": "PK_RepairSafetyActionSources", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Repairs.RepairSafetyActionSource |
| dbo.RepairSafetyActionSources | INDEX | IX_RepairSafetyActionSources_ActorId | {"name": "IX_RepairSafetyActionSources_ActorId", "columns": ["ActorId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairSafetyActionSource |
| dbo.RepairSafetyActionSources | INDEX | IX_RepairSafetyActionSources_BindingId | {"name": "IX_RepairSafetyActionSources_BindingId", "columns": ["BindingId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairSafetyActionSource |
| dbo.RepairSafetyActionSources | INDEX | IX_RepairSafetyActionSources_ItemId | {"name": "IX_RepairSafetyActionSources_ItemId", "columns": ["ItemId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairSafetyActionSource |
| dbo.RepairSafetyActionSources | INDEX | IX_RepairSafetyActionSources_ProjectId | {"name": "IX_RepairSafetyActionSources_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairSafetyActionSource |
| dbo.RepairSafetyActionSources | INDEX | IX_RepairSafetyActionSources_MeasureId_Kind_OriginId | {"name": "IX_RepairSafetyActionSources_MeasureId_Kind_OriginId", "columns": ["MeasureId", "Kind", "OriginId"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairSafetyActionSource |
| dbo.RepairSafetyCheckEvidence | KEY | PK_RepairSafetyCheckEvidence | {"name": "PK_RepairSafetyCheckEvidence", "columns": ["CheckId", "FileId"], "primary": true} | RoadGuardSystem.BusinessObjects.Repairs.RepairSafetyEvidenceReference |
| dbo.RepairSafetyCheckEvidence | INDEX | IX_RepairSafetyCheckEvidence_ActualUploaderId | {"name": "IX_RepairSafetyCheckEvidence_ActualUploaderId", "columns": ["ActualUploaderId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairSafetyEvidenceReference |
| dbo.RepairSafetyCheckEvidence | INDEX | IX_RepairSafetyCheckEvidence_FileId | {"name": "IX_RepairSafetyCheckEvidence_FileId", "columns": ["FileId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairSafetyEvidenceReference |
| dbo.RepairSafetyChecks | KEY | PK_RepairSafetyChecks | {"name": "PK_RepairSafetyChecks", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Repairs.RepairSafetyCheck |
| dbo.RepairSafetyChecks | INDEX | IX_RepairSafetyChecks_ActorId | {"name": "IX_RepairSafetyChecks_ActorId", "columns": ["ActorId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairSafetyCheck |
| dbo.RepairSafetyChecks | INDEX | IX_RepairSafetyChecks_MeasureId | {"name": "IX_RepairSafetyChecks_MeasureId", "columns": ["MeasureId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairSafetyCheck |
| dbo.RepairSafetyMonitoring | KEY | PK_RepairSafetyMonitoring | {"name": "PK_RepairSafetyMonitoring", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Repairs.RepairSafetyMonitoring |
| dbo.RepairSafetyMonitoring | KEY | AK_RepairSafetyMonitoring_MeasureId | {"name": "AK_RepairSafetyMonitoring_MeasureId", "columns": ["MeasureId"], "primary": false} | RoadGuardSystem.BusinessObjects.Repairs.RepairSafetyMonitoring |
| dbo.RepairSafetyMonitoring | INDEX | IX_RepairSafetyMonitoring_CurrentCheckId | {"name": "IX_RepairSafetyMonitoring_CurrentCheckId", "columns": ["CurrentCheckId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairSafetyMonitoring |
| dbo.RepairSafetyMonitoring | INDEX | IX_RepairSafetyMonitoring_FormalObligationId | {"name": "IX_RepairSafetyMonitoring_FormalObligationId", "columns": ["FormalObligationId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairSafetyMonitoring |
| dbo.RepairSafetyMonitoring | INDEX | IX_RepairSafetyMonitoring_MeasureId | {"name": "IX_RepairSafetyMonitoring_MeasureId", "columns": ["MeasureId"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairSafetyMonitoring |
| dbo.RepairSafetyMonitoring | INDEX | IX_RepairSafetyMonitoring_SafetyObligationId | {"name": "IX_RepairSafetyMonitoring_SafetyObligationId", "columns": ["SafetyObligationId"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairSafetyMonitoring |
| dbo.RepairSafetyResponsibilityTransfers | KEY | PK_RepairSafetyResponsibilityTransfers | {"name": "PK_RepairSafetyResponsibilityTransfers", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Repairs.SafetyResponsibilityTransfer |
| dbo.RepairSafetyResponsibilityTransfers | KEY | AK_RepairSafetyResponsibilityTransfers_MeasureId_Id | {"name": "AK_RepairSafetyResponsibilityTransfers_MeasureId_Id", "columns": ["MeasureId", "Id"], "primary": false} | RoadGuardSystem.BusinessObjects.Repairs.SafetyResponsibilityTransfer |
| dbo.RepairSafetyResponsibilityTransfers | INDEX | IX_RepairSafetyResponsibilityTransfers_ChangedBy | {"name": "IX_RepairSafetyResponsibilityTransfers_ChangedBy", "columns": ["ChangedBy"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.SafetyResponsibilityTransfer |
| dbo.RepairSafetyResponsibilityTransfers | INDEX | IX_RepairSafetyResponsibilityTransfers_NextActorId | {"name": "IX_RepairSafetyResponsibilityTransfers_NextActorId", "columns": ["NextActorId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.SafetyResponsibilityTransfer |
| dbo.RepairSafetyResponsibilityTransfers | INDEX | IX_RepairSafetyResponsibilityTransfers_PreviousActorId | {"name": "IX_RepairSafetyResponsibilityTransfers_PreviousActorId", "columns": ["PreviousActorId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.SafetyResponsibilityTransfer |
| dbo.RepairTemporarySafetyMeasures | KEY | PK_RepairTemporarySafetyMeasures | {"name": "PK_RepairTemporarySafetyMeasures", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Repairs.TemporarySafetyMeasure |
| dbo.RepairTemporarySafetyMeasures | INDEX | IX_RepairTemporarySafetyMeasures_DefectId | {"name": "IX_RepairTemporarySafetyMeasures_DefectId", "columns": ["DefectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.TemporarySafetyMeasure |
| dbo.RepairTemporarySafetyMeasures | INDEX | IX_RepairTemporarySafetyMeasures_FormalRepairObligationId | {"name": "IX_RepairTemporarySafetyMeasures_FormalRepairObligationId", "columns": ["FormalRepairObligationId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.TemporarySafetyMeasure |
| dbo.RepairTemporarySafetyMeasures | INDEX | IX_RepairTemporarySafetyMeasures_InstalledBy | {"name": "IX_RepairTemporarySafetyMeasures_InstalledBy", "columns": ["InstalledBy"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.TemporarySafetyMeasure |
| dbo.RepairTemporarySafetyMeasures | INDEX | IX_RepairTemporarySafetyMeasures_ProjectId | {"name": "IX_RepairTemporarySafetyMeasures_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.TemporarySafetyMeasure |
| dbo.RepairTemporarySafetyMeasures | INDEX | IX_RepairTemporarySafetyMeasures_ResponsibleActorId | {"name": "IX_RepairTemporarySafetyMeasures_ResponsibleActorId", "columns": ["ResponsibleActorId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.TemporarySafetyMeasure |
| dbo.RepairTemporarySafetyMeasures | INDEX | IX_RepairTemporarySafetyMeasures_Id_CurrentResponsibilityTransferId | {"name": "IX_RepairTemporarySafetyMeasures_Id_CurrentResponsibilityTransferId", "columns": ["Id", "CurrentResponsibilityTransferId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.TemporarySafetyMeasure |
| dbo.RepairWorkHandovers | KEY | PK_RepairWorkHandovers | {"name": "PK_RepairWorkHandovers", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Repairs.RepairWorkHandover |
| dbo.RepairWorkHandovers | INDEX | IX_RepairWorkHandovers_FromActorId | {"name": "IX_RepairWorkHandovers_FromActorId", "columns": ["FromActorId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairWorkHandover |
| dbo.RepairWorkHandovers | INDEX | IX_RepairWorkHandovers_ItemId | {"name": "IX_RepairWorkHandovers_ItemId", "columns": ["ItemId"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairWorkHandover |
| dbo.RepairWorkHandovers | INDEX | IX_RepairWorkHandovers_ToActorId | {"name": "IX_RepairWorkHandovers_ToActorId", "columns": ["ToActorId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RepairWorkHandover |
| dbo.ReporterRegistrationIntents | KEY | PK_ReporterRegistrationIntents | {"name": "PK_ReporterRegistrationIntents", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Identity.ReporterRegistrationIntent |
| dbo.ReporterRegistrationIntents | INDEX | IX_ReporterRegistrationIntents_Email | {"name": "IX_ReporterRegistrationIntents_Email", "columns": ["NormalizedEmail"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Identity.ReporterRegistrationIntent |
| dbo.ReporterRegistrationIntents | INDEX | IX_ReporterRegistrationIntents_UserId | {"name": "IX_ReporterRegistrationIntents_UserId", "columns": ["UserId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Identity.ReporterRegistrationIntent |
| dbo.ReporterRegistrationIntents | CHECK | CK_ReporterRegistrationIntents_ReporterType | {"name": "CK_ReporterRegistrationIntents_ReporterType", "sql": "[ReporterType] IN (1, 2)"} | RoadGuardSystem.BusinessObjects.Identity.ReporterRegistrationIntent |
| dbo.ReportOriginalEvidence | KEY | PK_ReportOriginalEvidence | {"name": "PK_ReportOriginalEvidence", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Reports.Report.OriginalEvidence#ReportEvidence |
| dbo.ReportOriginalEvidence | INDEX | IX_ReportOriginalEvidence_FileId | {"name": "IX_ReportOriginalEvidence_FileId", "columns": ["FileId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Reports.Report.OriginalEvidence#ReportEvidence |
| dbo.ReportOriginalEvidence | INDEX | IX_ReportOriginalEvidence_OwnerUserId | {"name": "IX_ReportOriginalEvidence_OwnerUserId", "columns": ["OwnerUserId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Reports.Report.OriginalEvidence#ReportEvidence |
| dbo.ReportOriginalEvidence | INDEX | IX_ReportOriginalEvidence_Id_ReportId | {"name": "IX_ReportOriginalEvidence_Id_ReportId", "columns": ["Id", "ReportId"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Reports.Report.OriginalEvidence#ReportEvidence |
| dbo.ReportOriginalEvidence | INDEX | IX_ReportOriginalEvidence_ReportId_OwnerUserId | {"name": "IX_ReportOriginalEvidence_ReportId_OwnerUserId", "columns": ["ReportId", "OwnerUserId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Reports.Report.OriginalEvidence#ReportEvidence |
| dbo.ReportOriginalEvidence | CHECK | CK_ReportOriginalEvidence_Original | {"name": "CK_ReportOriginalEvidence_Original", "sql": "[SupplementId] IS NULL AND [VerificationState] = 1"} | RoadGuardSystem.BusinessObjects.Reports.Report.OriginalEvidence#ReportEvidence |
| dbo.ReportOriginalEvidence | KEY | PK_ReportOriginalEvidence | {"name": "PK_ReportOriginalEvidence", "columns": ["ReportEvidenceId"], "primary": true} | RoadGuardSystem.BusinessObjects.Reports.Report.OriginalEvidence#ReportEvidence.CaptureMetadata#EvidenceCaptureMetadata |
| dbo.Reports | KEY | PK_Reports | {"name": "PK_Reports", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Reports.Report |
| dbo.Reports | KEY | AK_Reports_Id_ReporterUserId | {"name": "AK_Reports_Id_ReporterUserId", "columns": ["Id", "ReporterUserId"], "primary": false} | RoadGuardSystem.BusinessObjects.Reports.Report |
| dbo.Reports | INDEX | IX_Reports_ReporterUserId | {"name": "IX_Reports_ReporterUserId", "columns": ["ReporterUserId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Reports.Report |
| dbo.ReportSupplementEvidence | KEY | PK_ReportSupplementEvidence | {"name": "PK_ReportSupplementEvidence", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Reports.ReportSupplement.Evidence#ReportEvidence |
| dbo.ReportSupplementEvidence | INDEX | IX_ReportSupplementEvidence_FileId | {"name": "IX_ReportSupplementEvidence_FileId", "columns": ["FileId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Reports.ReportSupplement.Evidence#ReportEvidence |
| dbo.ReportSupplementEvidence | INDEX | IX_ReportSupplementEvidence_OwnerUserId | {"name": "IX_ReportSupplementEvidence_OwnerUserId", "columns": ["OwnerUserId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Reports.ReportSupplement.Evidence#ReportEvidence |
| dbo.ReportSupplementEvidence | INDEX | IX_ReportSupplementEvidence_Id_ReportId | {"name": "IX_ReportSupplementEvidence_Id_ReportId", "columns": ["Id", "ReportId"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Reports.ReportSupplement.Evidence#ReportEvidence |
| dbo.ReportSupplementEvidence | INDEX | IX_ReportSupplementEvidence_ReportId_OwnerUserId | {"name": "IX_ReportSupplementEvidence_ReportId_OwnerUserId", "columns": ["ReportId", "OwnerUserId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Reports.ReportSupplement.Evidence#ReportEvidence |
| dbo.ReportSupplementEvidence | INDEX | IX_ReportSupplementEvidence_SupplementId_ReportId | {"name": "IX_ReportSupplementEvidence_SupplementId_ReportId", "columns": ["SupplementId", "ReportId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Reports.ReportSupplement.Evidence#ReportEvidence |
| dbo.ReportSupplementEvidence | CHECK | CK_ReportSupplementEvidence_Supplement | {"name": "CK_ReportSupplementEvidence_Supplement", "sql": "[SupplementId] IS NOT NULL AND [VerificationState] = 1"} | RoadGuardSystem.BusinessObjects.Reports.ReportSupplement.Evidence#ReportEvidence |
| dbo.ReportSupplementEvidence | KEY | PK_ReportSupplementEvidence | {"name": "PK_ReportSupplementEvidence", "columns": ["ReportEvidenceId"], "primary": true} | RoadGuardSystem.BusinessObjects.Reports.ReportSupplement.Evidence#ReportEvidence.CaptureMetadata#EvidenceCaptureMetadata |
| dbo.ReportSupplements | KEY | PK_ReportSupplements | {"name": "PK_ReportSupplements", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Reports.ReportSupplement |
| dbo.ReportSupplements | KEY | AK_ReportSupplements_Id_ReportId | {"name": "AK_ReportSupplements_Id_ReportId", "columns": ["Id", "ReportId"], "primary": false} | RoadGuardSystem.BusinessObjects.Reports.ReportSupplement |
| dbo.ReportSupplements | INDEX | IX_ReportSupplements_ReportId | {"name": "IX_ReportSupplements_ReportId", "columns": ["ReportId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Reports.ReportSupplement |
| dbo.RetentionBasisHeads | KEY | PK_RetentionBasisHeads | {"name": "PK_RetentionBasisHeads", "columns": ["FileId"], "primary": true} | RoadGuardSystem.BusinessObjects.Retention.RetentionBasisHead |
| dbo.RetentionBasisHeads | INDEX | IX_RetentionBasisHeads_RevisionId_FileId | {"name": "IX_RetentionBasisHeads_RevisionId_FileId", "columns": ["RevisionId", "FileId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Retention.RetentionBasisHead |
| dbo.RetentionBasisRevisions | KEY | PK_RetentionBasisRevisions | {"name": "PK_RetentionBasisRevisions", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Retention.RetentionBasisRevision |
| dbo.RetentionBasisRevisions | KEY | AK_RetentionBasisRevisions_Id_FileId | {"name": "AK_RetentionBasisRevisions_Id_FileId", "columns": ["Id", "FileId"], "primary": false} | RoadGuardSystem.BusinessObjects.Retention.RetentionBasisRevision |
| dbo.RetentionBasisRevisions | INDEX | IX_RetentionBasisRevisions_ConfirmedBy | {"name": "IX_RetentionBasisRevisions_ConfirmedBy", "columns": ["ConfirmedBy"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Retention.RetentionBasisRevision |
| dbo.RetentionBasisRevisions | INDEX | IX_RetentionBasisRevisions_FileId_Revision | {"name": "IX_RetentionBasisRevisions_FileId_Revision", "columns": ["FileId", "Revision"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Retention.RetentionBasisRevision |
| dbo.RetentionBasisRevisions | INDEX | IX_RetentionBasisRevisions_SupersedesId_FileId | {"name": "IX_RetentionBasisRevisions_SupersedesId_FileId", "columns": ["SupersedesId", "FileId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Retention.RetentionBasisRevision |
| dbo.RetentionBasisRevisions | CHECK | CK_RetentionBasis_Revision | {"name": "CK_RetentionBasis_Revision", "sql": "[Revision]>0 AND [PolicyVersion]='pr41a.v1' AND [Classification] IN ('EVIDENCE','TEMPORARY_EXPORT') AND ISJSON([WarrantyReferencesJson])=1"} | RoadGuardSystem.BusinessObjects.Retention.RetentionBasisRevision |
| dbo.RetentionEvaluationItems | KEY | PK_RetentionEvaluationItems | {"name": "PK_RetentionEvaluationItems", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Retention.RetentionEvaluationItem |
| dbo.RetentionEvaluationItems | INDEX | IX_RetentionEvaluationItems_FileId | {"name": "IX_RetentionEvaluationItems_FileId", "columns": ["FileId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Retention.RetentionEvaluationItem |
| dbo.RetentionEvaluationItems | INDEX | IX_RetentionEvaluationItems_EvaluationId_FileId | {"name": "IX_RetentionEvaluationItems_EvaluationId_FileId", "columns": ["EvaluationId", "FileId"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Retention.RetentionEvaluationItem |
| dbo.RetentionEvaluationItems | CHECK | CK_RetentionEvaluationItem_State | {"name": "CK_RetentionEvaluationItem_State", "sql": "[Eligibility] IN ('BLOCKED_HOLD','WAITING_RETENTION_BASIS','RETAIN_UNTIL','ELIGIBLE_FOR_REVIEW')"} | RoadGuardSystem.BusinessObjects.Retention.RetentionEvaluationItem |
| dbo.RetentionEvaluations | KEY | PK_RetentionEvaluations | {"name": "PK_RetentionEvaluations", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Retention.RetentionEvaluation |
| dbo.RetentionEvaluations | INDEX | IX_RetentionEvaluations_ProjectId | {"name": "IX_RetentionEvaluations_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Retention.RetentionEvaluation |
| dbo.RetentionEvaluations | INDEX | IX_RetentionEvaluations_RequestedBy | {"name": "IX_RetentionEvaluations_RequestedBy", "columns": ["RequestedBy"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Retention.RetentionEvaluation |
| dbo.RetentionEvaluations | INDEX | IX_RetentionEvaluations_Status_CreatedAt | {"name": "IX_RetentionEvaluations_Status_CreatedAt", "columns": ["Status", "CreatedAt"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Retention.RetentionEvaluation |
| dbo.RetentionEvaluations | CHECK | CK_RetentionEvaluation_State | {"name": "CK_RetentionEvaluation_State", "sql": "[Status] IN ('QUEUED','COMPLETE') AND (([Status]='QUEUED' AND [EvaluatedAt] IS NULL) OR ([Status]='COMPLETE' AND [EvaluatedAt] IS NOT NULL))"} | RoadGuardSystem.BusinessObjects.Retention.RetentionEvaluation |
| dbo.RetentionHoldHistories | KEY | PK_RetentionHoldHistories | {"name": "PK_RetentionHoldHistories", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Retention.RetentionHoldHistory |
| dbo.RetentionHoldHistories | INDEX | IX_RetentionHoldHistories_ActorId | {"name": "IX_RetentionHoldHistories_ActorId", "columns": ["ActorId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Retention.RetentionHoldHistory |
| dbo.RetentionHoldHistories | INDEX | IX_RetentionHoldHistories_HoldId_OccurredAt | {"name": "IX_RetentionHoldHistories_HoldId_OccurredAt", "columns": ["HoldId", "OccurredAt"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Retention.RetentionHoldHistory |
| dbo.RetentionHoldHistories | CHECK | CK_RetentionHoldHistory_State | {"name": "CK_RetentionHoldHistory_State", "sql": "[State] IN ('ACTIVE','RELEASED')"} | RoadGuardSystem.BusinessObjects.Retention.RetentionHoldHistory |
| dbo.RetentionHolds | KEY | PK_RetentionHolds | {"name": "PK_RetentionHolds", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Retention.RetentionHold |
| dbo.RetentionHolds | INDEX | IX_RetentionHolds_CreatedBy | {"name": "IX_RetentionHolds_CreatedBy", "columns": ["CreatedBy"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Retention.RetentionHold |
| dbo.RetentionHolds | INDEX | IX_RetentionHolds_ReleasedBy | {"name": "IX_RetentionHolds_ReleasedBy", "columns": ["ReleasedBy"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Retention.RetentionHold |
| dbo.RetentionHolds | INDEX | IX_RetentionHolds_ScopeType_ScopeId_State | {"name": "IX_RetentionHolds_ScopeType_ScopeId_State", "columns": ["ScopeType", "ScopeId", "State"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Retention.RetentionHold |
| dbo.RetentionHolds | CHECK | CK_RetentionHold_State | {"name": "CK_RetentionHold_State", "sql": "[ScopeType] IN ('PROJECT','FILE') AND [State] IN ('ACTIVE','RELEASED') AND (([State]='ACTIVE' AND [ReleasedBy] IS NULL AND [ReleasedAt] IS NULL) OR ([State]='RELEASED' AND [ReleasedBy] IS NOT NULL AND [ReleasedAt] IS NOT NULL))"} | RoadGuardSystem.BusinessObjects.Retention.RetentionHold |
| dbo.RoadCoverageMappings | KEY | PK_RoadCoverageMappings | {"name": "PK_RoadCoverageMappings", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Repairs.RoadCoverageMapping |
| dbo.RoadCoverageMappings | INDEX | IX_RoadCoverageMappings_ActorId | {"name": "IX_RoadCoverageMappings_ActorId", "columns": ["ActorId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RoadCoverageMapping |
| dbo.RoadCoverageMappings | INDEX | IX_RoadCoverageMappings_CoverageFileId | {"name": "IX_RoadCoverageMappings_CoverageFileId", "columns": ["CoverageFileId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RoadCoverageMapping |
| dbo.RoadCoverageMappings | INDEX | IX_RoadCoverageMappings_HandoverDocumentId | {"name": "IX_RoadCoverageMappings_HandoverDocumentId", "columns": ["HandoverDocumentId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RoadCoverageMapping |
| dbo.RoadCoverageMappings | INDEX | IX_RoadCoverageMappings_HandoverFileId | {"name": "IX_RoadCoverageMappings_HandoverFileId", "columns": ["HandoverFileId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RoadCoverageMapping |
| dbo.RoadCoverageMappings | INDEX | IX_RoadCoverageMappings_RoadSectionId | {"name": "IX_RoadCoverageMappings_RoadSectionId", "columns": ["RoadSectionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RoadCoverageMapping |
| dbo.RoadCoverageMappings | INDEX | IX_RoadCoverageMappings_ScopeObligationId | {"name": "IX_RoadCoverageMappings_ScopeObligationId", "columns": ["ScopeObligationId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RoadCoverageMapping |
| dbo.RoadCoverageMappings | INDEX | IX_RoadCoverageMappings_SupersedesId | {"name": "IX_RoadCoverageMappings_SupersedesId", "columns": ["SupersedesId"], "unique": true, "filter": "[SupersedesId] IS NOT NULL"} | RoadGuardSystem.BusinessObjects.Repairs.RoadCoverageMapping |
| dbo.RoadCoverageMappings | INDEX | IX_RoadCoverageMappings_ProjectId_RoadSectionId_LocationVersion | {"name": "IX_RoadCoverageMappings_ProjectId_RoadSectionId_LocationVersion", "columns": ["ProjectId", "RoadSectionId", "LocationVersion"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Repairs.RoadCoverageMapping |
| dbo.RoadCoverageMappings | CHECK | CK_RoadCoverageMappings_Bounds | {"name": "CK_RoadCoverageMappings_Bounds", "sql": "[From]>=0 AND [To]>[From] AND [OffsetTo]>[OffsetFrom] AND [ApplicableToUtc]>[ApplicableFromUtc]"} | RoadGuardSystem.BusinessObjects.Repairs.RoadCoverageMapping |
| dbo.RoadCoverageMappings | CHECK | CK_RoadCoverageMappings_Source | {"name": "CK_RoadCoverageMappings_Source", "sql": "[SourceKind] COLLATE Latin1_General_100_BIN2 IN ('WARRANTY','MAINTENANCE_BASIS') AND [Provenance] COLLATE Latin1_General_100_BIN2 IN ('REAL_SOURCE','TEST_ONLY') AND ISJSON([SourceFactsJson])=1"} | RoadGuardSystem.BusinessObjects.Repairs.RoadCoverageMapping |
| dbo.RoadGeometryDrafts | KEY | PK_RoadGeometryDrafts | {"name": "PK_RoadGeometryDrafts", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Projects.RoadGeometryDraft |
| dbo.RoadGeometryDrafts | INDEX | IX_RoadGeometryDrafts_ProjectId | {"name": "IX_RoadGeometryDrafts_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.RoadGeometryDraft |
| dbo.RoadGeometryDrafts | INDEX | IX_RoadGeometryDrafts_RoadSectionId | {"name": "IX_RoadGeometryDrafts_RoadSectionId", "columns": ["RoadSectionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.RoadGeometryDraft |
| dbo.RoadGeometryMetadata | KEY | PK_RoadGeometryMetadata | {"name": "PK_RoadGeometryMetadata", "columns": ["RoadSectionVersionId"], "primary": true} | RoadGuardSystem.BusinessObjects.Projects.RoadGeometryMetadata |
| dbo.RoadGeometryMetadata | INDEX | IX_RoadGeometryMetadata_SourceDraftId | {"name": "IX_RoadGeometryMetadata_SourceDraftId", "columns": ["SourceDraftId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.RoadGeometryMetadata |
| dbo.RoadRouteSystems | KEY | PK_RoadRouteSystems | {"name": "PK_RoadRouteSystems", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Projects.RoadRouteSystem |
| dbo.RoadRouteSystems | KEY | AK_RoadRouteSystems_Id_ProjectId | {"name": "AK_RoadRouteSystems_Id_ProjectId", "columns": ["Id", "ProjectId"], "primary": false} | RoadGuardSystem.BusinessObjects.Projects.RoadRouteSystem |
| dbo.RoadRouteSystems | INDEX | IX_RoadRouteSystems_ProjectId_Code | {"name": "IX_RoadRouteSystems_ProjectId_Code", "columns": ["ProjectId", "Code"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.RoadRouteSystem |
| dbo.RoadSections | KEY | PK_RoadSections | {"name": "PK_RoadSections", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Projects.RoadSection |
| dbo.RoadSections | INDEX | UX_RoadSections_ProjectId_Code | {"name": "UX_RoadSections_ProjectId_Code", "columns": ["ProjectId", "Code"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.RoadSection |
| dbo.RoadSectionVersions | KEY | PK_RoadSectionVersions | {"name": "PK_RoadSectionVersions", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Projects.RoadSectionVersion |
| dbo.RoadSectionVersions | INDEX | IX_RoadSectionVersions_CrsProfileRevisionId | {"name": "IX_RoadSectionVersions_CrsProfileRevisionId", "columns": ["CrsProfileRevisionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.RoadSectionVersion |
| dbo.RoadSectionVersions | INDEX | UX_RoadSectionVersions_Current | {"name": "UX_RoadSectionVersions_Current", "columns": ["RoadSectionId"], "unique": true, "filter": "[IsCurrent] = 1"} | RoadGuardSystem.BusinessObjects.Projects.RoadSectionVersion |
| dbo.RoadSectionVersions | INDEX | UX_RoadSectionVersions_RoadSectionId_VersionNo | {"name": "UX_RoadSectionVersions_RoadSectionId_VersionNo", "columns": ["RoadSectionId", "VersionNo"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.RoadSectionVersion |
| dbo.RoadSectionVersions | CHECK | CK_RoadSectionVersions_Geometry_AllowedSrid | {"name": "CK_RoadSectionVersions_Geometry_AllowedSrid", "sql": "([CrsProfileRevisionId] IS NULL AND [Geometry].STSrid IN (32648, 32649)) OR ([CrsProfileRevisionId] IS NOT NULL AND [Geometry].STSrid>=0)"} | RoadGuardSystem.BusinessObjects.Projects.RoadSectionVersion |
| dbo.RoadSectionVersions | CHECK | CK_RoadSectionVersions_Geometry_LineString | {"name": "CK_RoadSectionVersions_Geometry_LineString", "sql": "[Geometry].STGeometryType() = 'LineString'"} | RoadGuardSystem.BusinessObjects.Projects.RoadSectionVersion |
| dbo.RoadSectionVersions | CHECK | CK_RoadSectionVersions_VersionNo_Positive | {"name": "CK_RoadSectionVersions_VersionNo_Positive", "sql": "[VersionNo] > 0"} | RoadGuardSystem.BusinessObjects.Projects.RoadSectionVersion |
| dbo.RoadSegments | KEY | PK_RoadSegments | {"name": "PK_RoadSegments", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Projects.RoadSegment |
| dbo.RoadSegments | INDEX | IX_RoadSegments_RoadSectionVersionId | {"name": "IX_RoadSegments_RoadSectionVersionId", "columns": ["RoadSectionVersionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.RoadSegment |
| dbo.RoadSegments | INDEX | IX_RoadSegments_SegmentSetId_RoadSectionVersionId | {"name": "IX_RoadSegments_SegmentSetId_RoadSectionVersionId", "columns": ["SegmentSetId", "RoadSectionVersionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.RoadSegment |
| dbo.RoadSegments | INDEX | UX_RoadSegments_Set_Sequence | {"name": "UX_RoadSegments_Set_Sequence", "columns": ["SegmentSetId", "Sequence"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.RoadSegment |
| dbo.RoadSegments | CHECK | CK_RoadSegments_Sequence_Positive | {"name": "CK_RoadSegments_Sequence_Positive", "sql": "[Sequence] > 0"} | RoadGuardSystem.BusinessObjects.Projects.RoadSegment |
| dbo.RoadSegmentSets | KEY | PK_RoadSegmentSets | {"name": "PK_RoadSegmentSets", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Projects.RoadSegmentSet |
| dbo.RoadSegmentSets | KEY | AK_RoadSegmentSets_Id_RoadSectionVersionId | {"name": "AK_RoadSegmentSets_Id_RoadSectionVersionId", "columns": ["Id", "RoadSectionVersionId"], "primary": false} | RoadGuardSystem.BusinessObjects.Projects.RoadSegmentSet |
| dbo.RoadSegmentSets | INDEX | UX_RoadSegmentSets_CurrentPublished | {"name": "UX_RoadSegmentSets_CurrentPublished", "columns": ["RoadSectionVersionId"], "unique": true, "filter": "[Status] = 'PUBLISHED'"} | RoadGuardSystem.BusinessObjects.Projects.RoadSegmentSet |
| dbo.RoadSegmentSets | INDEX | IX_RoadSegmentSets_RouteVersion_Status | {"name": "IX_RoadSegmentSets_RouteVersion_Status", "columns": ["RoadSectionVersionId", "Status"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Projects.RoadSegmentSet |
| dbo.RoadSegmentSets | CHECK | CK_RoadSegmentSets_Status | {"name": "CK_RoadSegmentSets_Status", "sql": "[Status] IN ('DRAFT','PUBLISHED','SUPERSEDED')"} | RoadGuardSystem.BusinessObjects.Projects.RoadSegmentSet |
| dbo.Roles | KEY | PK_Roles | {"name": "PK_Roles", "columns": ["Code"], "primary": true} | RoadGuardSystem.BusinessObjects.Identity.ApplicationRole |
| dbo.Sessions | KEY | PK_Sessions | {"name": "PK_Sessions", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Identity.UserSession |
| dbo.Sessions | INDEX | IX_Sessions_UserId | {"name": "IX_Sessions_UserId", "columns": ["UserId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Identity.UserSession |
| dbo.Sessions | CHECK | CK_Sessions_DeviceMetadataJson_Json | {"name": "CK_Sessions_DeviceMetadataJson_Json", "sql": "[DeviceMetadataJson] IS NULL OR ISJSON([DeviceMetadataJson]) = 1"} | RoadGuardSystem.BusinessObjects.Identity.UserSession |
| dbo.Sessions | CHECK | CK_Sessions_ExpiresAt | {"name": "CK_Sessions_ExpiresAt", "sql": "([Lifecycle] = 0 AND [ExpiresAt] IS NOT NULL AND [ExpiresAt] > [IssuedAt]) OR ([Lifecycle] = 1 AND [ExpiresAt] IS NULL AND [IssuedRole] IS NOT NULL)"} | RoadGuardSystem.BusinessObjects.Identity.UserSession |
| dbo.Sessions | CHECK | CK_Sessions_RevokedAt | {"name": "CK_Sessions_RevokedAt", "sql": "[RevokedAt] IS NULL OR [RevokedAt] >= [IssuedAt]"} | RoadGuardSystem.BusinessObjects.Identity.UserSession |
| dbo.Sessions | CHECK | CK_Sessions_Transport | {"name": "CK_Sessions_Transport", "sql": "[Transport] IN (0, 1, 2)"} | RoadGuardSystem.BusinessObjects.Identity.UserSession |
| dbo.SeverityRuleVersions | KEY | PK_SeverityRuleVersions | {"name": "PK_SeverityRuleVersions", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Catalogs.SeverityRuleVersion |
| dbo.SeverityRuleVersions | INDEX | UX_SeverityRuleVersions_ScopeVersion | {"name": "UX_SeverityRuleVersions_ScopeVersion", "columns": ["StandardCode", "RoadTypeCode", "VersionNo"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Catalogs.SeverityRuleVersion |
| dbo.SeverityRuleVersions | CHECK | CK_SeverityRuleVersions_EffectiveDateRange | {"name": "CK_SeverityRuleVersions_EffectiveDateRange", "sql": "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]"} | RoadGuardSystem.BusinessObjects.Catalogs.SeverityRuleVersion |
| dbo.SeverityRuleVersions | CHECK | CK_SeverityRuleVersions_RuleDefinition_JsonObject | {"name": "CK_SeverityRuleVersions_RuleDefinition_JsonObject", "sql": "ISJSON([RuleDefinition]) = 1 AND LEFT(LTRIM([RuleDefinition]), 1) = '{'"} | RoadGuardSystem.BusinessObjects.Catalogs.SeverityRuleVersion |
| dbo.SeverityRuleVersions | CHECK | CK_SeverityRuleVersions_VersionNo_Positive | {"name": "CK_SeverityRuleVersions_VersionNo_Positive", "sql": "[VersionNo] > 0"} | RoadGuardSystem.BusinessObjects.Catalogs.SeverityRuleVersion |
| dbo.SourceDecisions | KEY | PK_SourceDecisions | {"name": "PK_SourceDecisions", "columns": ["CandidateDecisionId"], "primary": true} | RoadGuardSystem.BusinessObjects.Candidates.CandidateClassification |
| dbo.SourceDecisions | INDEX | IX_SourceDecisions_CauseCategoryCode | {"name": "IX_SourceDecisions_CauseCategoryCode", "columns": ["CauseCategoryCode"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Candidates.CandidateClassification |
| dbo.SourceDecisions | INDEX | IX_SourceDecisions_DefectTypeCode | {"name": "IX_SourceDecisions_DefectTypeCode", "columns": ["DefectTypeCode"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Candidates.CandidateClassification |
| dbo.SourceDecisions | INDEX | IX_SourceDecisions_RoadSectionVersionId | {"name": "IX_SourceDecisions_RoadSectionVersionId", "columns": ["RoadSectionVersionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Candidates.CandidateClassification |
| dbo.SourceDecisions | INDEX | IX_SourceDecisions_SegmentId | {"name": "IX_SourceDecisions_SegmentId", "columns": ["SegmentId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Candidates.CandidateClassification |
| dbo.SourceDecisions | KEY | PK_SourceDecisions | {"name": "PK_SourceDecisions", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Candidates.CandidateDecision |
| dbo.SourceDecisions | KEY | AK_SourceDecisions_Id_SourceKind_SourceId_ProjectId | {"name": "AK_SourceDecisions_Id_SourceKind_SourceId_ProjectId", "columns": ["Id", "SourceKind", "SourceId", "ProjectId"], "primary": false} | RoadGuardSystem.BusinessObjects.Candidates.CandidateDecision |
| dbo.SourceDecisions | INDEX | IX_SourceDecisions_AIDetectionSourceId | {"name": "IX_SourceDecisions_AIDetectionSourceId", "columns": ["AIDetectionSourceId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Candidates.CandidateDecision |
| dbo.SourceDecisions | INDEX | IX_SourceDecisions_DecidedByUserId | {"name": "IX_SourceDecisions_DecidedByUserId", "columns": ["DecidedByUserId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Candidates.CandidateDecision |
| dbo.SourceDecisions | INDEX | IX_SourceDecisions_ProjectId | {"name": "IX_SourceDecisions_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Candidates.CandidateDecision |
| dbo.SourceDecisions | INDEX | IX_SourceDecisions_ReportSourceId | {"name": "IX_SourceDecisions_ReportSourceId", "columns": ["ReportSourceId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Candidates.CandidateDecision |
| dbo.SourceDecisions | INDEX | IX_SourceDecisions_SupersedesDecisionId | {"name": "IX_SourceDecisions_SupersedesDecisionId", "columns": ["SupersedesDecisionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Candidates.CandidateDecision |
| dbo.SourceDecisions | INDEX | IX_SourceDecisions_TargetDefectId | {"name": "IX_SourceDecisions_TargetDefectId", "columns": ["TargetDefectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Candidates.CandidateDecision |
| dbo.SourceDecisions | CHECK | CK_SourceDecisions_TypedSource | {"name": "CK_SourceDecisions_TypedSource", "sql": "[SourceKind] = [Source_Kind] AND [SourceId] = [Source_Id] AND (([SourceKind]=1 AND [ReportSourceId]=[SourceId] AND [ReportSourceId] IS NOT NULL AND [AIDetectionSourceId] IS NULL) OR ([SourceKind]=2 AND [AIDetectionSourceId]=[SourceId] AND [AIDetectionSourceId] IS NOT NULL AND [ReportSourceId] IS NULL))"} | RoadGuardSystem.BusinessObjects.Candidates.CandidateDecision |
| dbo.SourceDecisions | KEY | PK_SourceDecisions | {"name": "PK_SourceDecisions", "columns": ["CandidateDecisionId"], "primary": true} | RoadGuardSystem.BusinessObjects.Candidates.CandidateSourceIdentity |
| dbo.StaffInvitationProjects | KEY | PK_StaffInvitationProjects | {"name": "PK_StaffInvitationProjects", "columns": ["InvitationId", "ProjectId"], "primary": true} | RoadGuardSystem.BusinessObjects.Identity.StaffInvitationProject |
| dbo.StaffInvitationProjects | INDEX | IX_StaffInvitationProjects_ProjectId | {"name": "IX_StaffInvitationProjects_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Identity.StaffInvitationProject |
| dbo.StaffInvitations | KEY | PK_StaffInvitations | {"name": "PK_StaffInvitations", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Identity.StaffInvitation |
| dbo.StaffInvitations | INDEX | IX_StaffInvitations_CreatedByUserId | {"name": "IX_StaffInvitations_CreatedByUserId", "columns": ["CreatedByUserId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Identity.StaffInvitation |
| dbo.StaffInvitations | INDEX | IX_StaffInvitations_Email | {"name": "IX_StaffInvitations_Email", "columns": ["NormalizedEmail"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Identity.StaffInvitation |
| dbo.StaffInvitations | INDEX | UX_StaffInvitations_TokenHash | {"name": "UX_StaffInvitations_TokenHash", "columns": ["TokenHash"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Identity.StaffInvitation |
| dbo.SupplementarySurveyRequests | KEY | PK_SupplementarySurveyRequests | {"name": "PK_SupplementarySurveyRequests", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Surveys.SupplementarySurveyRequest |
| dbo.SupplementarySurveyRequests | INDEX | IX_SupplementarySurveyRequests_ApprovedByUserId | {"name": "IX_SupplementarySurveyRequests_ApprovedByUserId", "columns": ["ApprovedByUserId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Surveys.SupplementarySurveyRequest |
| dbo.SupplementarySurveyRequests | INDEX | IX_SupplementarySurveyRequests_RequestedByUserId | {"name": "IX_SupplementarySurveyRequests_RequestedByUserId", "columns": ["RequestedByUserId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Surveys.SupplementarySurveyRequest |
| dbo.SupplementarySurveyRequests | INDEX | IX_SupplementarySurveyRequests_SurveyRequestId | {"name": "IX_SupplementarySurveyRequests_SurveyRequestId", "columns": ["SurveyRequestId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Surveys.SupplementarySurveyRequest |
| dbo.SupplementarySurveyRequests | INDEX | UX_SupplementarySurveyRequests_SurveyRound | {"name": "UX_SupplementarySurveyRequests_SurveyRound", "columns": ["SurveyId", "RoundNo"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Surveys.SupplementarySurveyRequest |
| dbo.SupplementarySurveyRequests | CHECK | CK_SupplementarySurveyRequests_Approval | {"name": "CK_SupplementarySurveyRequests_Approval", "sql": "([ApprovedByUserId] IS NULL AND [ApprovedAt] IS NULL) OR ([ApprovedByUserId] IS NOT NULL AND [ApprovedAt] IS NOT NULL)"} | RoadGuardSystem.BusinessObjects.Surveys.SupplementarySurveyRequest |
| dbo.SupplementarySurveyRequests | CHECK | CK_SupplementarySurveyRequests_RequestedScope_JsonObject | {"name": "CK_SupplementarySurveyRequests_RequestedScope_JsonObject", "sql": "ISJSON([RequestedScope]) = 1 AND LEFT(LTRIM([RequestedScope]), 1) = '{'"} | RoadGuardSystem.BusinessObjects.Surveys.SupplementarySurveyRequest |
| dbo.SupplementarySurveyRequests | CHECK | CK_SupplementarySurveyRequests_RoundNo | {"name": "CK_SupplementarySurveyRequests_RoundNo", "sql": "[RoundNo] > 0"} | RoadGuardSystem.BusinessObjects.Surveys.SupplementarySurveyRequest |
| dbo.SupplementarySurveyRequests | CHECK | CK_SupplementarySurveyRequests_Status | {"name": "CK_SupplementarySurveyRequests_Status", "sql": "[Status] IN (1, 2, 3, 4, 5, 6, 7)"} | RoadGuardSystem.BusinessObjects.Surveys.SupplementarySurveyRequest |
| dbo.SurveyAssignments | KEY | PK_SurveyAssignments | {"name": "PK_SurveyAssignments", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Surveys.SurveyAssignment |
| dbo.SurveyAssignments | INDEX | IX_SurveyAssignments_AssignedByUserId | {"name": "IX_SurveyAssignments_AssignedByUserId", "columns": ["AssignedByUserId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Surveys.SurveyAssignment |
| dbo.SurveyAssignments | INDEX | IX_SurveyAssignments_OperatorUserId | {"name": "IX_SurveyAssignments_OperatorUserId", "columns": ["OperatorUserId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Surveys.SurveyAssignment |
| dbo.SurveyAssignments | INDEX | UX_SurveyAssignments_ActiveRequest | {"name": "UX_SurveyAssignments_ActiveRequest", "columns": ["SurveyRequestId"], "unique": true, "filter": "[EndedAt] IS NULL"} | RoadGuardSystem.BusinessObjects.Surveys.SurveyAssignment |
| dbo.SurveyAssignments | INDEX | IX_SurveyAssignments_RequestEndedAt | {"name": "IX_SurveyAssignments_RequestEndedAt", "columns": ["SurveyRequestId", "EndedAt"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Surveys.SurveyAssignment |
| dbo.SurveyAssignments | CHECK | CK_SurveyAssignments_AcceptanceRejection | {"name": "CK_SurveyAssignments_AcceptanceRejection", "sql": "[AcceptedAt] IS NULL OR [RejectedAt] IS NULL"} | RoadGuardSystem.BusinessObjects.Surveys.SurveyAssignment |
| dbo.SurveyAssignments | CHECK | CK_SurveyAssignments_ActiveReassignmentReason | {"name": "CK_SurveyAssignments_ActiveReassignmentReason", "sql": "[EndedAt] IS NOT NULL OR [ReassignmentReason] IS NULL"} | RoadGuardSystem.BusinessObjects.Surveys.SurveyAssignment |
| dbo.SurveyAssignments | CHECK | CK_SurveyAssignments_ReassignmentNotRejected | {"name": "CK_SurveyAssignments_ReassignmentNotRejected", "sql": "[ReassignmentReason] IS NULL OR [RejectedAt] IS NULL"} | RoadGuardSystem.BusinessObjects.Surveys.SurveyAssignment |
| dbo.SurveyAssignments | CHECK | CK_SurveyAssignments_Rejection | {"name": "CK_SurveyAssignments_Rejection", "sql": "([RejectedAt] IS NULL AND [RejectionReason] IS NULL) OR ([RejectedAt] IS NOT NULL AND LEN(LTRIM(RTRIM([RejectionReason]))) > 0 AND [EndedAt] IS NOT NULL)"} | RoadGuardSystem.BusinessObjects.Surveys.SurveyAssignment |
| dbo.SurveyAssignments | CHECK | CK_SurveyAssignments_TimestampOrder | {"name": "CK_SurveyAssignments_TimestampOrder", "sql": "([AcceptedAt] IS NULL OR [AcceptedAt] >= [AssignedAt]) AND ([RejectedAt] IS NULL OR [RejectedAt] >= [AssignedAt]) AND ([EndedAt] IS NULL OR [EndedAt] >= [AssignedAt])"} | RoadGuardSystem.BusinessObjects.Surveys.SurveyAssignment |
| dbo.SurveyDataVersions | KEY | PK_SurveyDataVersions | {"name": "PK_SurveyDataVersions", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Surveys.SurveyDataVersion |
| dbo.SurveyDataVersions | INDEX | UX_SurveyDataVersions_SurveyVersion | {"name": "UX_SurveyDataVersions_SurveyVersion", "columns": ["SurveyId", "VersionNo"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Surveys.SurveyDataVersion |
| dbo.SurveyDataVersions | CHECK | CK_SurveyDataVersions_IntegrityStatus | {"name": "CK_SurveyDataVersions_IntegrityStatus", "sql": "[IntegrityStatus] IN (1, 2, 3)"} | RoadGuardSystem.BusinessObjects.Surveys.SurveyDataVersion |
| dbo.SurveyDataVersions | CHECK | CK_SurveyDataVersions_ServerConfirmation | {"name": "CK_SurveyDataVersions_ServerConfirmation", "sql": "([Status] = 3 AND [IntegrityStatus] = 2 AND [ConfirmedAt] IS NOT NULL AND [ConfirmedBy] = 1) OR ([Status] <> 3 AND [ConfirmedAt] IS NULL AND [ConfirmedBy] IS NULL)"} | RoadGuardSystem.BusinessObjects.Surveys.SurveyDataVersion |
| dbo.SurveyDataVersions | CHECK | CK_SurveyDataVersions_SourceManifest_JsonArray | {"name": "CK_SurveyDataVersions_SourceManifest_JsonArray", "sql": "ISJSON([SourceManifest]) = 1 AND LEFT(LTRIM([SourceManifest]), 1) = '['"} | RoadGuardSystem.BusinessObjects.Surveys.SurveyDataVersion |
| dbo.SurveyDataVersions | CHECK | CK_SurveyDataVersions_Status | {"name": "CK_SurveyDataVersions_Status", "sql": "[Status] IN (1, 2, 3, 4, 5)"} | RoadGuardSystem.BusinessObjects.Surveys.SurveyDataVersion |
| dbo.SurveyDataVersions | CHECK | CK_SurveyDataVersions_VersionNo | {"name": "CK_SurveyDataVersions_VersionNo", "sql": "[VersionNo] > 0"} | RoadGuardSystem.BusinessObjects.Surveys.SurveyDataVersion |
| dbo.SurveyFiles | KEY | PK_SurveyFiles | {"name": "PK_SurveyFiles", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Surveys.SurveyFile |
| dbo.SurveyFiles | INDEX | IX_SurveyFiles_FileId | {"name": "IX_SurveyFiles_FileId", "columns": ["FileId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Surveys.SurveyFile |
| dbo.SurveyFiles | INDEX | IX_SurveyFiles_FlightId | {"name": "IX_SurveyFiles_FlightId", "columns": ["FlightId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Surveys.SurveyFile |
| dbo.SurveyFiles | INDEX | IX_SurveyFiles_SurveyId | {"name": "IX_SurveyFiles_SurveyId", "columns": ["SurveyId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Surveys.SurveyFile |
| dbo.SurveyFiles | CHECK | CK_SurveyFiles_Checksum_Sha256Lowercase | {"name": "CK_SurveyFiles_Checksum_Sha256Lowercase", "sql": "LEN([Checksum]) = 64 AND [Checksum] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'"} | RoadGuardSystem.BusinessObjects.Surveys.SurveyFile |
| dbo.SurveyFiles | CHECK | CK_SurveyFiles_FileType | {"name": "CK_SurveyFiles_FileType", "sql": "[FileType] IN (1, 2, 3, 4)"} | RoadGuardSystem.BusinessObjects.Surveys.SurveyFile |
| dbo.SurveyFiles | CHECK | CK_SurveyFiles_SyncStatus | {"name": "CK_SurveyFiles_SyncStatus", "sql": "[SyncStatus] IN (1, 2, 3, 4, 5)"} | RoadGuardSystem.BusinessObjects.Surveys.SurveyFile |
| dbo.SurveyFiles | CHECK | CK_SurveyFiles_TimestampOrder | {"name": "CK_SurveyFiles_TimestampOrder", "sql": "[CaptureEndedAt] IS NULL OR [CaptureStartedAt] IS NULL OR [CaptureEndedAt] >= [CaptureStartedAt]"} | RoadGuardSystem.BusinessObjects.Surveys.SurveyFile |
| dbo.SurveyPlanPostponements | KEY | PK_SurveyPlanPostponements | {"name": "PK_SurveyPlanPostponements", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Surveys.SurveyPlanPostponement |
| dbo.SurveyPlanPostponements | INDEX | IX_SurveyPlanPostponements_PlanId_PostponedAt | {"name": "IX_SurveyPlanPostponements_PlanId_PostponedAt", "columns": ["SurveyPlanId", "PostponedAt"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Surveys.SurveyPlanPostponement |
| dbo.SurveyPlans | KEY | PK_SurveyPlans | {"name": "PK_SurveyPlans", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Surveys.SurveyPlan |
| dbo.SurveyPlans | INDEX | IX_SurveyPlans_RoadSectionId | {"name": "IX_SurveyPlans_RoadSectionId", "columns": ["RoadSectionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Surveys.SurveyPlan |
| dbo.SurveyPlans | INDEX | IX_SurveyPlans_RoadSectionVersionId | {"name": "IX_SurveyPlans_RoadSectionVersionId", "columns": ["RoadSectionVersionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Surveys.SurveyPlan |
| dbo.SurveyPlans | INDEX | IX_SurveyPlans_ProjectRoadStart | {"name": "IX_SurveyPlans_ProjectRoadStart", "columns": ["ProjectId", "RoadSectionId", "PlannedStartAt"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Surveys.SurveyPlan |
| dbo.SurveyPlans | INDEX | UX_SurveyPlans_ActiveScope | {"name": "UX_SurveyPlans_ActiveScope", "columns": ["ProjectId", "RoadSectionId", "SurveyType"], "unique": true, "filter": "[Status] IN (1, 2, 3)"} | RoadGuardSystem.BusinessObjects.Surveys.SurveyPlan |
| dbo.SurveyPlans | CHECK | CK_SurveyPlans_OutputRequirements_Json | {"name": "CK_SurveyPlans_OutputRequirements_Json", "sql": "ISJSON([OutputRequirements]) = 1"} | RoadGuardSystem.BusinessObjects.Surveys.SurveyPlan |
| dbo.SurveyPlans | CHECK | CK_SurveyPlans_PlannedDateRange | {"name": "CK_SurveyPlans_PlannedDateRange", "sql": "[PlannedEndAt] >= [PlannedStartAt]"} | RoadGuardSystem.BusinessObjects.Surveys.SurveyPlan |
| dbo.SurveyPlans | CHECK | CK_SurveyPlans_Status | {"name": "CK_SurveyPlans_Status", "sql": "[Status] IN (1, 2, 3, 4, 5)"} | RoadGuardSystem.BusinessObjects.Surveys.SurveyPlan |
| dbo.SurveyPlans | CHECK | CK_SurveyPlans_SurveyType | {"name": "CK_SurveyPlans_SurveyType", "sql": "[SurveyType] IN (1, 2, 3)"} | RoadGuardSystem.BusinessObjects.Surveys.SurveyPlan |
| dbo.SurveyPlanScopes | KEY | PK_SurveyPlanScopes | {"name": "PK_SurveyPlanScopes", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Surveys.SurveyPlanScope |
| dbo.SurveyPlanScopes | INDEX | IX_SurveyPlanScopes_RouteSectionVersionId | {"name": "IX_SurveyPlanScopes_RouteSectionVersionId", "columns": ["RouteSectionVersionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Surveys.SurveyPlanScope |
| dbo.SurveyPlanScopes | INDEX | UX_SurveyPlanScopes_UniqueBand | {"name": "UX_SurveyPlanScopes_UniqueBand", "columns": ["SurveyPlanId", "RouteSectionVersionId", "SegmentSetId", "TargetBand"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Surveys.SurveyPlanScope |
| dbo.SurveyPlanScopes | CHECK | CK_SurveyPlanScopes_SegmentIdsJson | {"name": "CK_SurveyPlanScopes_SegmentIdsJson", "sql": "ISJSON([SegmentIdsJson]) = 1"} | RoadGuardSystem.BusinessObjects.Surveys.SurveyPlanScope |
| dbo.SurveyRequests | KEY | PK_SurveyRequests | {"name": "PK_SurveyRequests", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Surveys.SurveyRequest |
| dbo.SurveyRequests | INDEX | IX_SurveyRequests_ParentTaskId | {"name": "IX_SurveyRequests_ParentTaskId", "columns": ["ParentTaskId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Surveys.SurveyRequest |
| dbo.SurveyRequests | INDEX | IX_SurveyRequests_RequestedByUserId | {"name": "IX_SurveyRequests_RequestedByUserId", "columns": ["RequestedByUserId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Surveys.SurveyRequest |
| dbo.SurveyRequests | INDEX | IX_SurveyRequests_RoadSectionId | {"name": "IX_SurveyRequests_RoadSectionId", "columns": ["RoadSectionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Surveys.SurveyRequest |
| dbo.SurveyRequests | INDEX | IX_SurveyRequests_RoadSectionVersionId | {"name": "IX_SurveyRequests_RoadSectionVersionId", "columns": ["RoadSectionVersionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Surveys.SurveyRequest |
| dbo.SurveyRequests | INDEX | IX_SurveyRequests_SupplementRequestId | {"name": "IX_SurveyRequests_SupplementRequestId", "columns": ["SupplementRequestId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Surveys.SurveyRequest |
| dbo.SurveyRequests | INDEX | UX_SurveyRequests_ActivePlan | {"name": "UX_SurveyRequests_ActivePlan", "columns": ["SurveyPlanId"], "unique": true, "filter": "[SurveyPlanId] IS NOT NULL AND [Status] IN (1, 2, 4, 5, 6, 7, 10)"} | RoadGuardSystem.BusinessObjects.Surveys.SurveyRequest |
| dbo.SurveyRequests | INDEX | IX_SurveyRequests_ProjectRoadStatus | {"name": "IX_SurveyRequests_ProjectRoadStatus", "columns": ["ProjectId", "RoadSectionId", "Status"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Surveys.SurveyRequest |
| dbo.SurveyRequests | CHECK | CK_SurveyRequests_Cancellation | {"name": "CK_SurveyRequests_Cancellation", "sql": "([Status] = 9 AND [CancelledAt] IS NOT NULL AND LEN(LTRIM(RTRIM([CancellationReason]))) > 0) OR ([Status] <> 9 AND [CancelledAt] IS NULL AND [CancellationReason] IS NULL)"} | RoadGuardSystem.BusinessObjects.Surveys.SurveyRequest |
| dbo.SurveyRequests | CHECK | CK_SurveyRequests_OutputRequirements_Json | {"name": "CK_SurveyRequests_OutputRequirements_Json", "sql": "ISJSON([OutputRequirements]) = 1"} | RoadGuardSystem.BusinessObjects.Surveys.SurveyRequest |
| dbo.SurveyRequests | CHECK | CK_SurveyRequests_Status | {"name": "CK_SurveyRequests_Status", "sql": "[Status] IN (1, 2, 3, 4, 5, 6, 7, 8, 9, 10)"} | RoadGuardSystem.BusinessObjects.Surveys.SurveyRequest |
| dbo.SurveyRequests | CHECK | CK_SurveyRequests_SurveyType | {"name": "CK_SurveyRequests_SurveyType", "sql": "[SurveyType] IN (1, 2, 3)"} | RoadGuardSystem.BusinessObjects.Surveys.SurveyRequest |
| dbo.SurveyRequestScopes | KEY | PK_SurveyRequestScopes | {"name": "PK_SurveyRequestScopes", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Surveys.SurveyRequestScope |
| dbo.SurveyRequestScopes | INDEX | IX_SurveyRequestScopes_RouteSectionVersionId | {"name": "IX_SurveyRequestScopes_RouteSectionVersionId", "columns": ["RouteSectionVersionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Surveys.SurveyRequestScope |
| dbo.SurveyRequestScopes | INDEX | UX_SurveyRequestScopes_UniqueBand | {"name": "UX_SurveyRequestScopes_UniqueBand", "columns": ["SurveyRequestId", "RouteSectionVersionId", "SegmentSetId", "TargetBand"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Surveys.SurveyRequestScope |
| dbo.SurveyRequestScopes | CHECK | CK_SurveyRequestScopes_SegmentIdsJson | {"name": "CK_SurveyRequestScopes_SegmentIdsJson", "sql": "ISJSON([SegmentIdsJson]) = 1"} | RoadGuardSystem.BusinessObjects.Surveys.SurveyRequestScope |
| dbo.Surveys | KEY | PK_Surveys | {"name": "PK_Surveys", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Surveys.Survey |
| dbo.Surveys | INDEX | IX_Surveys_BaselineConfirmedByUserId | {"name": "IX_Surveys_BaselineConfirmedByUserId", "columns": ["BaselineConfirmedByUserId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Surveys.Survey |
| dbo.Surveys | INDEX | IX_Surveys_RoadSectionVersionId | {"name": "IX_Surveys_RoadSectionVersionId", "columns": ["RoadSectionVersionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Surveys.Survey |
| dbo.Surveys | INDEX | IX_Surveys_SurveyRequestId | {"name": "IX_Surveys_SurveyRequestId", "columns": ["SurveyRequestId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Surveys.Survey |
| dbo.Surveys | INDEX | IX_Surveys_ProjectStatus | {"name": "IX_Surveys_ProjectStatus", "columns": ["ProjectId", "Status"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Surveys.Survey |
| dbo.Surveys | CHECK | CK_Surveys_BaselineConfirmation | {"name": "CK_Surveys_BaselineConfirmation", "sql": "([IsBaselineConfirmed] = 0 AND [BaselineConfirmedByUserId] IS NULL AND [BaselineConfirmedAt] IS NULL) OR ([IsBaselineConfirmed] = 1 AND [BaselineConfirmedByUserId] IS NOT NULL AND [BaselineConfirmedAt] IS NOT NULL)"} | RoadGuardSystem.BusinessObjects.Surveys.Survey |
| dbo.Surveys | CHECK | CK_Surveys_Status | {"name": "CK_Surveys_Status", "sql": "[Status] IN (1, 2, 3, 4, 5)"} | RoadGuardSystem.BusinessObjects.Surveys.Survey |
| dbo.Surveys | CHECK | CK_Surveys_SurveyType | {"name": "CK_Surveys_SurveyType", "sql": "[SurveyType] IN (1, 2, 3)"} | RoadGuardSystem.BusinessObjects.Surveys.Survey |
| dbo.TrainingLabelReviews | KEY | PK_TrainingLabelReviews | {"name": "PK_TrainingLabelReviews", "columns": ["Id"], "primary": true} | RoadGuardSystem.Repositories.Models.Huy01.HuyTrainingLabelReview |
| dbo.TrainingLabelReviews | INDEX | IX_TrainingLabelReviews_ActorUserId | {"name": "IX_TrainingLabelReviews_ActorUserId", "columns": ["ActorUserId"], "unique": false, "filter": null} | RoadGuardSystem.Repositories.Models.Huy01.HuyTrainingLabelReview |
| dbo.TrainingLabelReviews | INDEX | IX_TrainingLabelReviews_RevisionId | {"name": "IX_TrainingLabelReviews_RevisionId", "columns": ["RevisionId"], "unique": true, "filter": null} | RoadGuardSystem.Repositories.Models.Huy01.HuyTrainingLabelReview |
| dbo.TrainingLabelReviews | INDEX | IX_TrainingLabelReviews_RevisionId_LabelId | {"name": "IX_TrainingLabelReviews_RevisionId_LabelId", "columns": ["RevisionId", "LabelId"], "unique": false, "filter": null} | RoadGuardSystem.Repositories.Models.Huy01.HuyTrainingLabelReview |
| dbo.TrainingLabelReviews | CHECK | CK_TrainingLabelReviews_Decision | {"name": "CK_TrainingLabelReviews_Decision", "sql": "[Decision] IN ('APPROVED','REJECTED')"} | RoadGuardSystem.Repositories.Models.Huy01.HuyTrainingLabelReview |
| dbo.TrainingLabelRevisions | KEY | PK_TrainingLabelRevisions | {"name": "PK_TrainingLabelRevisions", "columns": ["Id"], "primary": true} | RoadGuardSystem.Repositories.Models.Huy01.HuyTrainingLabelRevision |
| dbo.TrainingLabelRevisions | KEY | AK_TrainingLabelRevisions_Id_LabelId | {"name": "AK_TrainingLabelRevisions_Id_LabelId", "columns": ["Id", "LabelId"], "primary": false} | RoadGuardSystem.Repositories.Models.Huy01.HuyTrainingLabelRevision |
| dbo.TrainingLabelRevisions | INDEX | IX_TrainingLabelRevisions_DefectTypeCode | {"name": "IX_TrainingLabelRevisions_DefectTypeCode", "columns": ["DefectTypeCode"], "unique": false, "filter": null} | RoadGuardSystem.Repositories.Models.Huy01.HuyTrainingLabelRevision |
| dbo.TrainingLabelRevisions | INDEX | IX_TrainingLabelRevisions_FileId | {"name": "IX_TrainingLabelRevisions_FileId", "columns": ["FileId"], "unique": false, "filter": null} | RoadGuardSystem.Repositories.Models.Huy01.HuyTrainingLabelRevision |
| dbo.TrainingLabelRevisions | INDEX | IX_TrainingLabelRevisions_LabelId_Revision | {"name": "IX_TrainingLabelRevisions_LabelId_Revision", "columns": ["LabelId", "Revision"], "unique": true, "filter": null} | RoadGuardSystem.Repositories.Models.Huy01.HuyTrainingLabelRevision |
| dbo.TrainingLabelRevisions | CHECK | CK_TrainingLabelRevisions_Bbox | {"name": "CK_TrainingLabelRevisions_Bbox", "sql": "[X]>=0 AND [Y]>=0 AND [Width]>0 AND [Height]>0 AND [X]+[Width]<=1 AND [Y]+[Height]<=1"} | RoadGuardSystem.Repositories.Models.Huy01.HuyTrainingLabelRevision |
| dbo.TrainingLabels | KEY | PK_TrainingLabels | {"name": "PK_TrainingLabels", "columns": ["Id"], "primary": true} | RoadGuardSystem.Repositories.Models.Huy01.HuyTrainingLabelHead |
| dbo.TrainingLabels | INDEX | IX_TrainingLabels_AIDetectionSourceId | {"name": "IX_TrainingLabels_AIDetectionSourceId", "columns": ["AIDetectionSourceId"], "unique": false, "filter": null} | RoadGuardSystem.Repositories.Models.Huy01.HuyTrainingLabelHead |
| dbo.TrainingLabels | INDEX | IX_TrainingLabels_ReportSourceId | {"name": "IX_TrainingLabels_ReportSourceId", "columns": ["ReportSourceId"], "unique": false, "filter": null} | RoadGuardSystem.Repositories.Models.Huy01.HuyTrainingLabelHead |
| dbo.TrainingLabels | INDEX | IX_TrainingLabels_ProjectId_SourceKind_SourceId | {"name": "IX_TrainingLabels_ProjectId_SourceKind_SourceId", "columns": ["ProjectId", "SourceKind", "SourceId"], "unique": false, "filter": null} | RoadGuardSystem.Repositories.Models.Huy01.HuyTrainingLabelHead |
| dbo.TrainingLabels | CHECK | CK_TrainingLabels_Source | {"name": "CK_TrainingLabels_Source", "sql": "([SourceKind]='REPORT' AND [ReportSourceId]=[SourceId] AND [AIDetectionSourceId] IS NULL) OR ([SourceKind]='AI_DETECTION' AND [AIDetectionSourceId]=[SourceId] AND [ReportSourceId] IS NULL)"} | RoadGuardSystem.Repositories.Models.Huy01.HuyTrainingLabelHead |
| dbo.UploadMultipartSweeps | KEY | PK_UploadMultipartSweeps | {"name": "PK_UploadMultipartSweeps", "columns": ["UploadSessionId"], "primary": true} | RoadGuardSystem.Repositories.Models.UploadMultipartSweep |
| dbo.UploadMultipartSweeps | INDEX | IX_UploadMultipartSweeps_NextCheckAt | {"name": "IX_UploadMultipartSweeps_NextCheckAt", "columns": ["NextCheckAt"], "unique": false, "filter": null} | RoadGuardSystem.Repositories.Models.UploadMultipartSweep |
| dbo.UploadParts | KEY | PK_UploadParts | {"name": "PK_UploadParts", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Files.UploadPart |
| dbo.UploadParts | INDEX | UX_UploadParts_Session_PartNumber | {"name": "UX_UploadParts_Session_PartNumber", "columns": ["UploadSessionId", "PartNumber"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Files.UploadPart |
| dbo.UploadSessions | KEY | PK_UploadSessions | {"name": "PK_UploadSessions", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Files.UploadSession |
| dbo.UploadSessions | INDEX | IX_UploadSessions_FileId | {"name": "IX_UploadSessions_FileId", "columns": ["FileId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Files.UploadSession |
| dbo.UploadSessions | INDEX | IX_UploadSessions_MultipartNextCheckAt | {"name": "IX_UploadSessions_MultipartNextCheckAt", "columns": ["MultipartNextCheckAt"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Files.UploadSession |
| dbo.UploadSessions | INDEX | UX_UploadSessions_ObjectKey | {"name": "UX_UploadSessions_ObjectKey", "columns": ["ObjectKey"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Files.UploadSession |
| dbo.UploadSessions | INDEX | IX_UploadSessions_OwnerUserId | {"name": "IX_UploadSessions_OwnerUserId", "columns": ["OwnerUserId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Files.UploadSession |
| dbo.UploadSessions | INDEX | IX_UploadSessions_Status_ExpiresAt | {"name": "IX_UploadSessions_Status_ExpiresAt", "columns": ["Status", "ExpiresAt"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Files.UploadSession |
| dbo.UploadSessions | CHECK | CK_UploadSessions_ExpectedSizeBytes_Positive | {"name": "CK_UploadSessions_ExpectedSizeBytes_Positive", "sql": "[ExpectedSizeBytes] > 0"} | RoadGuardSystem.BusinessObjects.Files.UploadSession |
| dbo.UploadSessions | CHECK | CK_UploadSessions_PartSizeBytes_Positive | {"name": "CK_UploadSessions_PartSizeBytes_Positive", "sql": "[PartSizeBytes] > 0"} | RoadGuardSystem.BusinessObjects.Files.UploadSession |
| dbo.Users | KEY | PK_Users | {"name": "PK_Users", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Identity.ApplicationUser |
| dbo.Users | INDEX | UX_Users_Email | {"name": "UX_Users_Email", "columns": ["Email"], "unique": true, "filter": "[Email] IS NOT NULL"} | RoadGuardSystem.BusinessObjects.Identity.ApplicationUser |
| dbo.Users | INDEX | UX_Users_NormalizedUserName | {"name": "UX_Users_NormalizedUserName", "columns": ["NormalizedUserName"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Identity.ApplicationUser |
| dbo.Users | INDEX | IX_Users_RoleCode | {"name": "IX_Users_RoleCode", "columns": ["RoleCode"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Identity.ApplicationUser |
| dbo.Users | INDEX | UX_Users_UserName | {"name": "UX_Users_UserName", "columns": ["UserName"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Identity.ApplicationUser |
| dbo.Users | CHECK | CK_Users_Status | {"name": "CK_Users_Status", "sql": "[Status] IN (1, 2, 3)"} | RoadGuardSystem.BusinessObjects.Identity.ApplicationUser |
| dbo.ValidationRuns | KEY | PK_ValidationRuns | {"name": "PK_ValidationRuns", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Processing.ValidationRun |
| dbo.ValidationRuns | INDEX | IX_ValidationRuns_ModelVersionId | {"name": "IX_ValidationRuns_ModelVersionId", "columns": ["ModelVersionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Processing.ValidationRun |
| dbo.ValidationRuns | INDEX | IX_ValidationRuns_ProjectStatus | {"name": "IX_ValidationRuns_ProjectStatus", "columns": ["ProjectId", "Status"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Processing.ValidationRun |
| dbo.ValidationRuns | CHECK | CK_ValidationRuns_ExclusionReasonsJson | {"name": "CK_ValidationRuns_ExclusionReasonsJson", "sql": "ISJSON([ExclusionReasonsJson]) = 1 AND LEFT(LTRIM([ExclusionReasonsJson]), 1) = '['"} | RoadGuardSystem.BusinessObjects.Processing.ValidationRun |
| dbo.ValidationRuns | CHECK | CK_ValidationRuns_PairsJson | {"name": "CK_ValidationRuns_PairsJson", "sql": "ISJSON([PairsJson]) = 1 AND LEFT(LTRIM([PairsJson]), 1) = '['"} | RoadGuardSystem.BusinessObjects.Processing.ValidationRun |
| dbo.ValidationRuns | CHECK | CK_ValidationRuns_Status | {"name": "CK_ValidationRuns_Status", "sql": "[Status] IN (1, 2, 3)"} | RoadGuardSystem.BusinessObjects.Processing.ValidationRun |
| dbo.Warranties | KEY | PK_Warranties | {"name": "PK_Warranties", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Warranties.Warranty |
| dbo.Warranties | INDEX | IX_Warranties_HandoverDocumentId | {"name": "IX_Warranties_HandoverDocumentId", "columns": ["HandoverDocumentId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Warranties.Warranty |
| dbo.Warranties | INDEX | IX_Warranties_ProjectId | {"name": "IX_Warranties_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Warranties.Warranty |
| dbo.Warranties | INDEX | IX_Warranties_RoadSectionId | {"name": "IX_Warranties_RoadSectionId", "columns": ["RoadSectionId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Warranties.Warranty |
| dbo.Warranties | INDEX | IX_Warranties_SourceDocumentId | {"name": "IX_Warranties_SourceDocumentId", "columns": ["SourceDocumentId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Warranties.Warranty |
| dbo.Warranties | CHECK | CK_Warranties_DateRange | {"name": "CK_Warranties_DateRange", "sql": "[WarrantyEndDate] >= [WarrantyStartDate]"} | RoadGuardSystem.BusinessObjects.Warranties.Warranty |
| dbo.Warranties | CHECK | CK_Warranties_RetainedValue_NonNegative | {"name": "CK_Warranties_RetainedValue_NonNegative", "sql": "[RetainedValue] IS NULL OR [RetainedValue] >= 0"} | RoadGuardSystem.BusinessObjects.Warranties.Warranty |
| dbo.Warranties | CHECK | CK_Warranties_Scope | {"name": "CK_Warranties_Scope", "sql": "[Scope] IN (1, 2, 3, 4)"} | RoadGuardSystem.BusinessObjects.Warranties.Warranty |
| dbo.Warranties | CHECK | CK_Warranties_ScopeRoadSection | {"name": "CK_Warranties_ScopeRoadSection", "sql": "([Scope] <> 1 OR [RoadSectionId] IS NULL) AND ([Scope] <> 2 OR [RoadSectionId] IS NOT NULL)"} | RoadGuardSystem.BusinessObjects.Warranties.Warranty |
| dbo.Warranties | CHECK | CK_Warranties_Status | {"name": "CK_Warranties_Status", "sql": "[Status] IN (1, 2, 3, 4)"} | RoadGuardSystem.BusinessObjects.Warranties.Warranty |
| dbo.WeeklyReviewDigestDuties | KEY | PK_WeeklyReviewDigestDuties | {"name": "PK_WeeklyReviewDigestDuties", "columns": ["DigestId", "ClockId"], "primary": true} | RoadGuardSystem.BusinessObjects.Messaging.WeeklyReviewDigestDuty |
| dbo.WeeklyReviewDigestDuties | INDEX | IX_WeeklyReviewDigestDuties_ClockId | {"name": "IX_WeeklyReviewDigestDuties_ClockId", "columns": ["ClockId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Messaging.WeeklyReviewDigestDuty |
| dbo.WeeklyReviewDigests | KEY | PK_WeeklyReviewDigests | {"name": "PK_WeeklyReviewDigests", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Messaging.WeeklyReviewDigest |
| dbo.WeeklyReviewDigests | INDEX | IX_WeeklyReviewDigests_RecipientId | {"name": "IX_WeeklyReviewDigests_RecipientId", "columns": ["RecipientId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Messaging.WeeklyReviewDigest |
| dbo.WeeklyReviewDigests | INDEX | IX_WeeklyReviewDigests_RecoveryPeriodId | {"name": "IX_WeeklyReviewDigests_RecoveryPeriodId", "columns": ["RecoveryPeriodId"], "unique": false, "filter": null} | RoadGuardSystem.BusinessObjects.Messaging.WeeklyReviewDigest |
| dbo.WeeklyReviewDigests | INDEX | IX_WeeklyReviewDigests_ProjectId_ScheduledAtUtc_RecipientKey | {"name": "IX_WeeklyReviewDigests_ProjectId_ScheduledAtUtc_RecipientKey", "columns": ["ProjectId", "ScheduledAtUtc", "RecipientKey"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Messaging.WeeklyReviewDigest |
| dbo.WeeklyReviewRecoveryPeriods | KEY | PK_WeeklyReviewRecoveryPeriods | {"name": "PK_WeeklyReviewRecoveryPeriods", "columns": ["Id"], "primary": true} | RoadGuardSystem.BusinessObjects.Messaging.WeeklyReviewRecoveryPeriod |
| dbo.WeeklyReviewRecoveryPeriods | INDEX | IX_WeeklyReviewRecoveryPeriods_ProjectId_ScheduledAtUtc | {"name": "IX_WeeklyReviewRecoveryPeriods_ProjectId_ScheduledAtUtc", "columns": ["ProjectId", "ScheduledAtUtc"], "unique": true, "filter": null} | RoadGuardSystem.BusinessObjects.Messaging.WeeklyReviewRecoveryPeriod |

## SQL-only/migration trigger evidence

### sqlOnlyForeignKeys

```json
[
  {
    "TableKey": "dbo.CasePublicationEvidence",
    "ForeignKey": {
      "Name": "FK_CasePublicationEvidence_OriginalEvidence",
      "Columns": [
        "OriginalEvidenceId",
        "SourceReportId"
      ],
      "PrincipalTable": "dbo.ReportOriginalEvidence",
      "PrincipalColumns": [
        "Id",
        "ReportId"
      ],
      "Required": false,
      "DeleteBehavior": "NoAction"
    },
    "MigrationId": "20261008171739_BaselineCurrentSchema",
    "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "Line": 11868,
    "MigrationSqlSha256Utf8Lf": "14c540531be4fd99f9873b9a4bf3a7ac656c270da2607f0586f50ebee3fc1190"
  },
  {
    "TableKey": "dbo.CasePublicationEvidence",
    "ForeignKey": {
      "Name": "FK_CasePublicationEvidence_SupplementEvidence",
      "Columns": [
        "SupplementEvidenceId",
        "SourceReportId"
      ],
      "PrincipalTable": "dbo.ReportSupplementEvidence",
      "PrincipalColumns": [
        "Id",
        "ReportId"
      ],
      "Required": false,
      "DeleteBehavior": "NoAction"
    },
    "MigrationId": "20261008171739_BaselineCurrentSchema",
    "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "Line": 11869,
    "MigrationSqlSha256Utf8Lf": "93b338d81d670ff387c1534333548c1eb3db98c5f93df0a348b0247940455337"
  },
  {
    "TableKey": "dbo.CaseConclusionEvidence",
    "ForeignKey": {
      "Name": "FK_CaseConclusionEvidence_OriginalEvidence",
      "Columns": [
        "OriginalEvidenceId",
        "SourceReportId"
      ],
      "PrincipalTable": "dbo.ReportOriginalEvidence",
      "PrincipalColumns": [
        "Id",
        "ReportId"
      ],
      "Required": false,
      "DeleteBehavior": "NoAction"
    },
    "MigrationId": "20261008171739_BaselineCurrentSchema",
    "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "Line": 11870,
    "MigrationSqlSha256Utf8Lf": "959b011059353ebc767e9202152e228ab321d32dcf279ebfb0a4eddf1ef1c4a5"
  },
  {
    "TableKey": "dbo.CaseConclusionEvidence",
    "ForeignKey": {
      "Name": "FK_CaseConclusionEvidence_SupplementEvidence",
      "Columns": [
        "SupplementEvidenceId",
        "SourceReportId"
      ],
      "PrincipalTable": "dbo.ReportSupplementEvidence",
      "PrincipalColumns": [
        "Id",
        "ReportId"
      ],
      "Required": false,
      "DeleteBehavior": "NoAction"
    },
    "MigrationId": "20261008171739_BaselineCurrentSchema",
    "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "Line": 11871,
    "MigrationSqlSha256Utf8Lf": "5984e8965e1a5a68446e039729420e67e5771bb09423c02b8b458922d4507f3e"
  },
  {
    "TableKey": "dbo.SourceDecisions",
    "ForeignKey": {
      "Name": "FK_SourceDecisions_CorrectionIdentity",
      "Columns": [
        "SupersedesDecisionId",
        "SourceKind",
        "SourceId",
        "ProjectId"
      ],
      "PrincipalTable": "dbo.SourceDecisions",
      "PrincipalColumns": [
        "Id",
        "SourceKind",
        "SourceId",
        "ProjectId"
      ],
      "Required": false,
      "DeleteBehavior": "NoAction"
    },
    "MigrationId": "20261008171739_BaselineCurrentSchema",
    "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "Line": 11872,
    "MigrationSqlSha256Utf8Lf": "6d4beee8f97783d33aa34332adf9b940f40de33765b9e47ece8f405db3064c3f"
  }
]
```

### sqlOnlyIndexes

```json
[
  {
    "TableKey": "dbo.RepairDecisions",
    "Name": "UX_RepairDecisions_Initial",
    "Columns": [
      "ItemId"
    ],
    "Filter": "[SupersedesDecisionId] IS NULL",
    "MigrationId": "20261008171739_BaselineCurrentSchema",
    "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "Line": 11873,
    "MigrationSqlSha256Utf8Lf": "0d25f2599a852bc8146718651c782be28135b3e4cb7fefebfd6b09628af9c107"
  }
]
```

### triggerEvidence

```json
[
  {
    "Schema": "dbo",
    "Table": "AccountStatusChangeLogs",
    "Name": "TR_AccountStatusChangeLogs_AppendOnly",
    "observedDefinitionSha256Utf8Lf": "5a7da5c98aad9f2e14224303b4e8a4bf6c79b7676b6f0e1ccfdac98c43df1185",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 11875,
    "migrationSqlSha256Utf8Lf": "dd321fe4361d013024e75444e94cc1bad4837fd1418b7cb9524b6cd7bd119ac1",
    "expectedCatalogDefinitionSha256Utf8Lf": "dd321fe4361d013024e75444e94cc1bad4837fd1418b7cb9524b6cd7bd119ac1",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 11875
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "AIDetections",
    "Name": "TR_AIDetections_Immutable",
    "observedDefinitionSha256Utf8Lf": "7b750929df2e6143f21c1ff192da667ccd8a9e0041a81bcc3e1e00c043153a6f",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 11885,
    "migrationSqlSha256Utf8Lf": "0db9f11c0d77baf5503ea87d8c9c532dab9681a9683a54f8db133017e0a8dde7",
    "expectedCatalogDefinitionSha256Utf8Lf": "0db9f11c0d77baf5503ea87d8c9c532dab9681a9683a54f8db133017e0a8dde7",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 11885
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "Anh02AiDetectionProvenance",
    "Name": "TR_Anh02AiDetectionProvenance_Immutable",
    "observedDefinitionSha256Utf8Lf": "f70e44b1e456907cc92ab19a4cc3a9fac16ef22bd803df8ec2238910e37b8200",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 11895,
    "migrationSqlSha256Utf8Lf": "72ea71b15e94f8c187e65ffa7f46306fbc8ea8c76e2edc582c82693e886701cb",
    "expectedCatalogDefinitionSha256Utf8Lf": "72ea71b15e94f8c187e65ffa7f46306fbc8ea8c76e2edc582c82693e886701cb",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 11895
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "Anh02AiManifestFiles",
    "Name": "TR_Anh02AiManifestFiles_Immutable",
    "observedDefinitionSha256Utf8Lf": "66cf8ee67dd892c86013380107da1e84f232b0e9ba48b9161815f2708a7cff47",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 11898,
    "migrationSqlSha256Utf8Lf": "59e709d6d058eb8b159e284f4c7ae28e63d5d937d0d55cc07f1155dda93fd972",
    "expectedCatalogDefinitionSha256Utf8Lf": "59e709d6d058eb8b159e284f4c7ae28e63d5d937d0d55cc07f1155dda93fd972",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 11898
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "Anh02AiMockRuns",
    "Name": "TR_Anh02AiMockRuns_Identity",
    "observedDefinitionSha256Utf8Lf": "e106e73a6ed7db261716eebb8f99b0fde8b10b85abfd8289c22fa6398467d332",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 11901,
    "migrationSqlSha256Utf8Lf": "a47aa0eef94fe48f50a23d2f7ef334ea85429b659470123ec355199acc78b4e3",
    "expectedCatalogDefinitionSha256Utf8Lf": "a47aa0eef94fe48f50a23d2f7ef334ea85429b659470123ec355199acc78b4e3",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 11901
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "Anh02AiResultProvenance",
    "Name": "TR_Anh02AiResultProvenance_Immutable",
    "observedDefinitionSha256Utf8Lf": "04208cf9e610c6cebec997ab484093b6b2ec10c57976f63ff9c26d23378433b2",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 11918,
    "migrationSqlSha256Utf8Lf": "62bd3f8131bc932acd0554dfc92ffff9934692395b67336582c095194efcfc4c",
    "expectedCatalogDefinitionSha256Utf8Lf": "62bd3f8131bc932acd0554dfc92ffff9934692395b67336582c095194efcfc4c",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 11918
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "Anh02ExportSnapshotFiles",
    "Name": "TR_Anh02ExportSnapshotFiles_Immutable",
    "observedDefinitionSha256Utf8Lf": "f95b7f0b57770a5ef9931378539a1a1493d01862917eef138f26870dd2dd8a85",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 11921,
    "migrationSqlSha256Utf8Lf": "95294d46f4f5efd6c93d552bb97b592b7369be8d0912dc81f3ec95307b403c63",
    "expectedCatalogDefinitionSha256Utf8Lf": "95294d46f4f5efd6c93d552bb97b592b7369be8d0912dc81f3ec95307b403c63",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 11921
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "Anh02ExportSnapshots",
    "Name": "TR_Anh02ExportSnapshots_Immutable",
    "observedDefinitionSha256Utf8Lf": "35ea834a5bd341c47863be72b6faf98f0d0123c5407536ad86b5139a0332a2ab",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 11924,
    "migrationSqlSha256Utf8Lf": "68a3338919e43178c645eef131fb6a116d799f5569aa7bde41fcc45373ec24d0",
    "expectedCatalogDefinitionSha256Utf8Lf": "68a3338919e43178c645eef131fb6a116d799f5569aa7bde41fcc45373ec24d0",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 11924
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "Anh02GeneratedArtifacts",
    "Name": "TR_Anh02GeneratedArtifacts_Immutable",
    "observedDefinitionSha256Utf8Lf": "4417679574129c5271dec19eb33c9eb7760e7181e73ceab881928c51ab7136c3",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 11927,
    "migrationSqlSha256Utf8Lf": "81f7f0c544f91886c0bf933536017f82a4773441df365a1a41ff20f400432da6",
    "expectedCatalogDefinitionSha256Utf8Lf": "81f7f0c544f91886c0bf933536017f82a4773441df365a1a41ff20f400432da6",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 11927
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "AuditLogs",
    "Name": "TR_AuditLogs_AppendOnly",
    "observedDefinitionSha256Utf8Lf": "4fc0ae0b4e8ed77eababdbd9edf99beb8dbb07afe91ba8913be77750317c005a",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 11930,
    "migrationSqlSha256Utf8Lf": "7a4c1ce0299560eb7a945d5bb066e52ac86b415cb848022ff28aecee7ec87f8c",
    "expectedCatalogDefinitionSha256Utf8Lf": "7a4c1ce0299560eb7a945d5bb066e52ac86b415cb848022ff28aecee7ec87f8c",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 11930
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "BaselineSelectionItems",
    "Name": "TR_BaselineSelectionItems_Immutable",
    "observedDefinitionSha256Utf8Lf": "239ee9861417d39a275045a97b17f960a5e3171ffbd987800fcbd7b237677b2e",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 11940,
    "migrationSqlSha256Utf8Lf": "8e9a11f0bc34763fb515977bb1e8850f0d134c06a56c7d9c2b1393697568b10a",
    "expectedCatalogDefinitionSha256Utf8Lf": "8e9a11f0bc34763fb515977bb1e8850f0d134c06a56c7d9c2b1393697568b10a",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 11940
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "BaselineSelections",
    "Name": "TR_BaselineSelections_Immutable",
    "observedDefinitionSha256Utf8Lf": "cbae15ff6640031cbe800c8b846e942a42cc7c2b741a6edf6a504ec97c04c846",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 11943,
    "migrationSqlSha256Utf8Lf": "4fd80f1211ca6c5d172647aaa4ad089a798e23aa984cb17fed747405724dd011",
    "expectedCatalogDefinitionSha256Utf8Lf": "4fd80f1211ca6c5d172647aaa4ad089a798e23aa984cb17fed747405724dd011",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 11943
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "BusinessDutyAppointments",
    "Name": "TR_BusinessDutyAppointments_Immutable",
    "observedDefinitionSha256Utf8Lf": "b119ffc2246e4b17a1e408d380c58acef9fc30502c900c93ce3cd4de4f921c00",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 11946,
    "migrationSqlSha256Utf8Lf": "5c963cbb1c49647213856d9b02c7308a2dc91ec9bda82b992f9557abe54dc3db",
    "expectedCatalogDefinitionSha256Utf8Lf": "5c963cbb1c49647213856d9b02c7308a2dc91ec9bda82b992f9557abe54dc3db",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 11946
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "BusinessReceivingRequests",
    "Name": "TR_BusinessReceivingRequests_SourceAck",
    "observedDefinitionSha256Utf8Lf": "00ff230928837a7ecbf925adecc6e9c6915093234ee5a1f913172c1025701de2",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 11950,
    "migrationSqlSha256Utf8Lf": "f9f45186df1a14d08401ef8b0ad512646bfec69d712be58aac3669f0d432bb63",
    "expectedCatalogDefinitionSha256Utf8Lf": "f9f45186df1a14d08401ef8b0ad512646bfec69d712be58aac3669f0d432bb63",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 11950
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "CaseConclusionDefects",
    "Name": "TR_CaseConclusionDefects_Immutable",
    "observedDefinitionSha256Utf8Lf": "96bdf2f02634d60af02ecadc44e793fc912fb72e51591bd8c6593fb73823e649",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 11966,
    "migrationSqlSha256Utf8Lf": "b72cd47345d8c02b4f56104000a24de28304afd5e432400d835f48538ef31003",
    "expectedCatalogDefinitionSha256Utf8Lf": "b72cd47345d8c02b4f56104000a24de28304afd5e432400d835f48538ef31003",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 11966
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "CaseConclusionEvidence",
    "Name": "TR_CaseConclusionEvidence_Immutable",
    "observedDefinitionSha256Utf8Lf": "f690b05b5ca2a0a23adb309e7d5493fcd024ba43d8bcf8887f9911189b6f42e3",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 11969,
    "migrationSqlSha256Utf8Lf": "26c0ac14466b80d6fbcc31b918705a30bcc620915f22a3350106d251629f5779",
    "expectedCatalogDefinitionSha256Utf8Lf": "26c0ac14466b80d6fbcc31b918705a30bcc620915f22a3350106d251629f5779",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 11969
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "CaseConclusions",
    "Name": "TR_CaseConclusions_Immutable",
    "observedDefinitionSha256Utf8Lf": "1af9783944b0a4fa97222ca42e3a0fb100350e0eafb4f22971bb32218697c722",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 11972,
    "migrationSqlSha256Utf8Lf": "a5e3f8f41da6a54f98133ef24cb160495aa110d8fd39f57441f578b47252c889",
    "expectedCatalogDefinitionSha256Utf8Lf": "a5e3f8f41da6a54f98133ef24cb160495aa110d8fd39f57441f578b47252c889",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 11972
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "CasePublicationDefects",
    "Name": "TR_CasePublicationDefects_Immutable",
    "observedDefinitionSha256Utf8Lf": "976f353843096688f3c4b3acb6b81d3a9cc4b657d81dea3fcba54f2ee7c0f0a8",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 11975,
    "migrationSqlSha256Utf8Lf": "a7d4fe3f0e50d086df4d70e23f21594117bbffec5b03b2c166a130356063bed1",
    "expectedCatalogDefinitionSha256Utf8Lf": "a7d4fe3f0e50d086df4d70e23f21594117bbffec5b03b2c166a130356063bed1",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 11975
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "CasePublicationEvidence",
    "Name": "TR_CasePublicationEvidence_Immutable",
    "observedDefinitionSha256Utf8Lf": "5beb41f45a5da34f8f11dc997059aadd4173da33383d04c18adaefe88179bd4b",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 11978,
    "migrationSqlSha256Utf8Lf": "93623948de25b32801f06fd951edb2787dced8f7a66f11d303a196b34c8e5c6c",
    "expectedCatalogDefinitionSha256Utf8Lf": "93623948de25b32801f06fd951edb2787dced8f7a66f11d303a196b34c8e5c6c",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 11978
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "CasePublicationRecipients",
    "Name": "TR_CasePublicationRecipients_Immutable",
    "observedDefinitionSha256Utf8Lf": "25c562896d57abd8cce6cef598f148f6bd8036f668a7f57ebe08b997948fb8dd",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 11981,
    "migrationSqlSha256Utf8Lf": "877924afaf99a8056f8d4a8df8fa42b11ea24c12e7781a7c6d46353cb6ae4aba",
    "expectedCatalogDefinitionSha256Utf8Lf": "877924afaf99a8056f8d4a8df8fa42b11ea24c12e7781a7c6d46353cb6ae4aba",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 11981
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "CasePublications",
    "Name": "TR_CasePublications_Immutable",
    "observedDefinitionSha256Utf8Lf": "da93c14d30c44fe7f9de55b67b016a98e62e2e94394c8e851750a02f5552c7f7",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 11984,
    "migrationSqlSha256Utf8Lf": "9d5ccf6f27489bce5bd08b9094ad620444eac1d4868558c3146616825abdfed1",
    "expectedCatalogDefinitionSha256Utf8Lf": "9d5ccf6f27489bce5bd08b9094ad620444eac1d4868558c3146616825abdfed1",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 11984
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "CaseReportLinkHistory",
    "Name": "TR_CaseReportLinkHistory_Immutable",
    "observedDefinitionSha256Utf8Lf": "08c86f5af6e88be5ac403b28215350454c03fde03702314b83fde2367f33466d",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 11987,
    "migrationSqlSha256Utf8Lf": "54c7428a583be9f96f4aa8201ee7a568cedf4a8b64bd49978ec6b3a75a3822a1",
    "expectedCatalogDefinitionSha256Utf8Lf": "54c7428a583be9f96f4aa8201ee7a568cedf4a8b64bd49978ec6b3a75a3822a1",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 11987
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "CaseReportLinkHistoryReports",
    "Name": "TR_CaseReportLinkHistoryReports_Immutable",
    "observedDefinitionSha256Utf8Lf": "d4964dbe34b56de7150faf65391f79797d589976f2c9ec766adcf0262c7a4f20",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 11990,
    "migrationSqlSha256Utf8Lf": "371131d9f31c5d7a13e3391e293167dcccecdcda0820fc73050489b2a1588c20",
    "expectedCatalogDefinitionSha256Utf8Lf": "371131d9f31c5d7a13e3391e293167dcccecdcda0820fc73050489b2a1588c20",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 11990
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "CaseReportLinks",
    "Name": "TR_CaseReportLinks_AppendOnly",
    "observedDefinitionSha256Utf8Lf": "6c99dae8d40d2fced994c9da5e54858cfd866e807cc22d418e8ad0be7289e397",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 11993,
    "migrationSqlSha256Utf8Lf": "36bcbbb4820d6032c2d7f34e46784ee092b86048502f69d9da26df723d0144fa",
    "expectedCatalogDefinitionSha256Utf8Lf": "36bcbbb4820d6032c2d7f34e46784ee092b86048502f69d9da26df723d0144fa",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 11993
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "CrsProfileRevisions",
    "Name": "TR_CrsProfileRevisions_Immutable",
    "observedDefinitionSha256Utf8Lf": "ea212a6c64bbc8342058e7615dbeba6327e644d6290ede40e26f05e15ef2e177",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 11996,
    "migrationSqlSha256Utf8Lf": "b3b6470691966d08e8cfecae0069d6226f4fc3f88717b6e42877a24529dfb4ac",
    "expectedCatalogDefinitionSha256Utf8Lf": "b3b6470691966d08e8cfecae0069d6226f4fc3f88717b6e42877a24529dfb4ac",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 11996
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "DatasetAssessmentItems",
    "Name": "TR_DatasetAssessmentItems_Immutable",
    "observedDefinitionSha256Utf8Lf": "1ddfc7ebd13e59cf65f8d5ce9fc04367e530cb6e464ac7df50dd2479a690adfd",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 11999,
    "migrationSqlSha256Utf8Lf": "b9c865984311784ac5af9d85da30712c9cf04206aaeeeb4f1aaa79340c3eb3d4",
    "expectedCatalogDefinitionSha256Utf8Lf": "b9c865984311784ac5af9d85da30712c9cf04206aaeeeb4f1aaa79340c3eb3d4",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 11999
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "DatasetAssessments",
    "Name": "TR_DatasetAssessments_Immutable",
    "observedDefinitionSha256Utf8Lf": "e4123afba0d28ac0d683a83dd901c02d58e7d1095742b1d710297769514a10f6",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12002,
    "migrationSqlSha256Utf8Lf": "0857c3a8f6bcd9fb261b58ad1341c56a6c90fcce0dc2a652119b558be00b0702",
    "expectedCatalogDefinitionSha256Utf8Lf": "0857c3a8f6bcd9fb261b58ad1341c56a6c90fcce0dc2a652119b558be00b0702",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12002
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "DeadlineBreaches",
    "Name": "TR_DeadlineBreaches_Immutable",
    "observedDefinitionSha256Utf8Lf": "a864dcbe35e0d756c45e7f998e6f75c2e3c6cce2416e19270e88bdf69729bcf3",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12005,
    "migrationSqlSha256Utf8Lf": "1e6ca0f3d56d6eb23ff60c4aa01baa5f012a13aeee535149ffd2102d4199424b",
    "expectedCatalogDefinitionSha256Utf8Lf": "1e6ca0f3d56d6eb23ff60c4aa01baa5f012a13aeee535149ffd2102d4199424b",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12005
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "DeadlineClocks",
    "Name": "TR_DeadlineClocks_ImmutableOrigin",
    "observedDefinitionSha256Utf8Lf": "18efbb55acc3df40576abd8b8e8ab79d6def3c7cff46006704442159c030939f",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12009,
    "migrationSqlSha256Utf8Lf": "b0970efd86d7a70dc194e7f851ddecd442d05d3b264a64effd235287a597876d",
    "expectedCatalogDefinitionSha256Utf8Lf": "b0970efd86d7a70dc194e7f851ddecd442d05d3b264a64effd235287a597876d",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12009
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "DeadlineDutyAppointments",
    "Name": "TR_DeadlineDutyAppointments_Immutable",
    "observedDefinitionSha256Utf8Lf": "d848c41aabb8af23e89e2a677997a2ac7d5ca7877f2c4114bdf9a3b29e7db7e5",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12025,
    "migrationSqlSha256Utf8Lf": "89be12c5879ddce9bb27e2a67297333ae325d978b1634e279b6d7ca0c1c624c6",
    "expectedCatalogDefinitionSha256Utf8Lf": "89be12c5879ddce9bb27e2a67297333ae325d978b1634e279b6d7ca0c1c624c6",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12025
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "DeadlineExtensions",
    "Name": "TR_DeadlineExtensions_Immutable",
    "observedDefinitionSha256Utf8Lf": "34ff8da7fb63652a02d7d8b6f3154c5456a88e7e25b0d78fbe97893a43fee344",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12029,
    "migrationSqlSha256Utf8Lf": "aa09d1060b4fb31d1d79981616efaceddb9842260d3af04e4444a3f740384d15",
    "expectedCatalogDefinitionSha256Utf8Lf": "aa09d1060b4fb31d1d79981616efaceddb9842260d3af04e4444a3f740384d15",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12029
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "DefectStatisticsSources",
    "Name": "TR_DefectStatisticsSources_Immutable",
    "observedDefinitionSha256Utf8Lf": "9e52fc22c20f744b0521691c46d5eef63d07f10930e87b6156ecfa4ae897c4a0",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12033,
    "migrationSqlSha256Utf8Lf": "90ed428be81bcfcbbf0c13414ada8d954822cc44e086351f0654dd05edc1e4e6",
    "expectedCatalogDefinitionSha256Utf8Lf": "90ed428be81bcfcbbf0c13414ada8d954822cc44e086351f0654dd05edc1e4e6",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12033
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "DefectStatisticsSources",
    "Name": "TR_DefectStatisticsSources_Scope",
    "observedDefinitionSha256Utf8Lf": "b99f5e0e060dab022cff3e9a00a84f291fd597016c22f2c0d69e99eac02c5824",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12039,
    "migrationSqlSha256Utf8Lf": "0dd782a06b7d69d999522aeaedbd17419cf2141a727a28d0087e47d98fdf4a83",
    "expectedCatalogDefinitionSha256Utf8Lf": "0dd782a06b7d69d999522aeaedbd17419cf2141a727a28d0087e47d98fdf4a83",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12039
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "DefectVerificationLogs",
    "Name": "TR_DefectVerificationLogs_AppendOnly",
    "observedDefinitionSha256Utf8Lf": "5ec1f57e4d4a758b049fa0871c92b533d4f6578f45e41c9d094ae87ad72b8569",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12068,
    "migrationSqlSha256Utf8Lf": "1975ea2cce64f09f4ccba123f2a57519a04ef0ea3cb8fcbc3247bde90cbb4bfc",
    "expectedCatalogDefinitionSha256Utf8Lf": "1975ea2cce64f09f4ccba123f2a57519a04ef0ea3cb8fcbc3247bde90cbb4bfc",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12068
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "FieldInspectionEvidenceLinks",
    "Name": "TR_FieldInspectionEvidenceLinks_Immutable",
    "observedDefinitionSha256Utf8Lf": "bee76ef4e7d1c1302acfae2d3d80b7954bc71e4185887fc8f4b74c718d9a6c3a",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12078,
    "migrationSqlSha256Utf8Lf": "aa010d890d018752368b7e4e901bb614e81d5bffee2f92b3b5c16cc3fb4282c0",
    "expectedCatalogDefinitionSha256Utf8Lf": "aa010d890d018752368b7e4e901bb614e81d5bffee2f92b3b5c16cc3fb4282c0",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12078
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "FieldInspectionEvidenceLinks",
    "Name": "TR_FieldInspectionEvidenceLinks_Scope",
    "observedDefinitionSha256Utf8Lf": "427b0965875736641f6b72c3bc80fd2ef95b13b101efd6e34dceeec6c4a6de53",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12087,
    "migrationSqlSha256Utf8Lf": "d89c79b6b7c627bed267f5c3e115fed8eefd5b31f26c3afb7ebf59fa7f5339d3",
    "expectedCatalogDefinitionSha256Utf8Lf": "d89c79b6b7c627bed267f5c3e115fed8eefd5b31f26c3afb7ebf59fa7f5339d3",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12087
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "FieldInspectionEvidenceReuseDecisions",
    "Name": "TR_FieldInspectionEvidenceReuseDecisions_Immutable",
    "observedDefinitionSha256Utf8Lf": "e4cb32ae8c82e6ca56e29d0f6ce84a21f4eb9719e1ab4b2628077bc586cf25fd",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12107,
    "migrationSqlSha256Utf8Lf": "7c87b2b1cb33ad8243e25e79b77c11a693a48b8a510ce58d7833f47a21ecc8df",
    "expectedCatalogDefinitionSha256Utf8Lf": "7c87b2b1cb33ad8243e25e79b77c11a693a48b8a510ce58d7833f47a21ecc8df",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12107
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "FieldInspectionEvidenceReuseDecisions",
    "Name": "TR_FieldInspectionEvidenceReuseDecisions_Scope",
    "observedDefinitionSha256Utf8Lf": "f7685beb767480b68226698eb6c7b093ab5e201a005fea1f86eab495eeac6a47",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12116,
    "migrationSqlSha256Utf8Lf": "b035606a334a8ba2b3be55cecb300e0380ca2825f9517048f34f7beb1e2a46e9",
    "expectedCatalogDefinitionSha256Utf8Lf": "b035606a334a8ba2b3be55cecb300e0380ca2825f9517048f34f7beb1e2a46e9",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12116
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "FieldInspectionLocationProofs",
    "Name": "TR_FieldInspectionLocationProofs_Immutable",
    "observedDefinitionSha256Utf8Lf": "b987b21bc9ab6ba553d0cc058f8055c5c920e5f138a63fc491526d4e4f1c3b55",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12140,
    "migrationSqlSha256Utf8Lf": "03023c4f12414d2ae919475f0f2c02f4a7a3a40ff2a4594b072913eb3a1febb0",
    "expectedCatalogDefinitionSha256Utf8Lf": "03023c4f12414d2ae919475f0f2c02f4a7a3a40ff2a4594b072913eb3a1febb0",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12140
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "FieldInspectionLocationProofs",
    "Name": "TR_FieldInspectionLocationProofs_Scope",
    "observedDefinitionSha256Utf8Lf": "9d88e2fc73c1b538ec4d1900a6c96fe815bf0d590dc73dbeae16fc3d9746290b",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12149,
    "migrationSqlSha256Utf8Lf": "de11b6aae0f3b5ba011aa7b43362043c3dc80b257f257907485271208661d768",
    "expectedCatalogDefinitionSha256Utf8Lf": "de11b6aae0f3b5ba011aa7b43362043c3dc80b257f257907485271208661d768",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12149
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "FieldInspectionOperationOrigins",
    "Name": "TR_FieldInspectionOperationOrigins_Immutable",
    "observedDefinitionSha256Utf8Lf": "2e62459c5a5c1bd20816d55f8be7f2f514f02e3de9604aa130c3374fd628d25c",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12161,
    "migrationSqlSha256Utf8Lf": "e4ed71f0a3be6f5382ad19435156d9c23ed679566d10e8eb7cc69a28277e2bd8",
    "expectedCatalogDefinitionSha256Utf8Lf": "e4ed71f0a3be6f5382ad19435156d9c23ed679566d10e8eb7cc69a28277e2bd8",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12161
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "FieldInspectionOperationOrigins",
    "Name": "TR_FieldInspectionOperationOrigins_Scope",
    "observedDefinitionSha256Utf8Lf": "13c571b67257f0ac22cc3e770400aca44fa57d6ddfd82facd4bfe020d24aa943",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12170,
    "migrationSqlSha256Utf8Lf": "ec72605ca84b5dcf8fbbe69266889db0ca465702bb980f4f0cd89e1879089df0",
    "expectedCatalogDefinitionSha256Utf8Lf": "ec72605ca84b5dcf8fbbe69266889db0ca465702bb980f4f0cd89e1879089df0",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12170
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "FieldInspectionReviews",
    "Name": "TR_FieldInspectionReviews_Immutable",
    "observedDefinitionSha256Utf8Lf": "97aeccad6f42e0280ccf34682f4fa758788f6b1c125819e2d46e28b60a686223",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12181,
    "migrationSqlSha256Utf8Lf": "c64ddd5d8d7a56b9d82f956a2e16f302f3e31391f22cbe576720a41083ab1d11",
    "expectedCatalogDefinitionSha256Utf8Lf": "c64ddd5d8d7a56b9d82f956a2e16f302f3e31391f22cbe576720a41083ab1d11",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12181
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "FieldInspectionReviews",
    "Name": "TR_FieldInspectionReviews_Scope",
    "observedDefinitionSha256Utf8Lf": "270a4117dbf56fec262897745b078d58a60d46b54f083f9aa5845df4712501f7",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12190,
    "migrationSqlSha256Utf8Lf": "4d6bc758acb3314b8321ae804b06227a647c1fb856e80fbe90df26a1fe318378",
    "expectedCatalogDefinitionSha256Utf8Lf": "4d6bc758acb3314b8321ae804b06227a647c1fb856e80fbe90df26a1fe318378",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12190
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "FieldInspectionSessions",
    "Name": "TR_FieldInspectionSessions_Immutable",
    "observedDefinitionSha256Utf8Lf": "dd35c9499d9157e3890f4e2bee4f39de2d5312af095dc3587dcefa4e1f6c261e",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12202,
    "migrationSqlSha256Utf8Lf": "ff5aef4010728e7baf44681f98dea065007a0345e31a72a80fe8b63d0a2e4077",
    "expectedCatalogDefinitionSha256Utf8Lf": "ff5aef4010728e7baf44681f98dea065007a0345e31a72a80fe8b63d0a2e4077",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12202
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "FieldInspectionSessions",
    "Name": "TR_FieldInspectionSessions_Integrity",
    "observedDefinitionSha256Utf8Lf": "4e7f52b5ca3c286118f0c6c6e7fab8a779a6437f46d634c13333af97e3d467bd",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12220,
    "migrationSqlSha256Utf8Lf": "e394028680307027046aa8995e76f579828f887aad825f74adc0d55bf911467f",
    "expectedCatalogDefinitionSha256Utf8Lf": "e394028680307027046aa8995e76f579828f887aad825f74adc0d55bf911467f",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12220
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "FieldInspectionSubmissions",
    "Name": "TR_FieldInspectionSubmissions_Immutable",
    "observedDefinitionSha256Utf8Lf": "f8579908201322895fbad939a534f5bd0ba65fdd8483b584d1d5367b81d16536",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12246,
    "migrationSqlSha256Utf8Lf": "dddc60161205a41643d109f319b2718985d501aca7f515e96d7063e92f9a499c",
    "expectedCatalogDefinitionSha256Utf8Lf": "dddc60161205a41643d109f319b2718985d501aca7f515e96d7063e92f9a499c",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12246
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "FieldInspectionSubmissions",
    "Name": "TR_FieldInspectionSubmissions_Scope",
    "observedDefinitionSha256Utf8Lf": "f057fb6db750b30e9bd4c90cc3d1befc8fb21f95e8715812daee9a51bfa230af",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12255,
    "migrationSqlSha256Utf8Lf": "10416925b5473d1fa04524279a7e5d720f5b1c3ba0174353a2510a029bcf9a6a",
    "expectedCatalogDefinitionSha256Utf8Lf": "10416925b5473d1fa04524279a7e5d720f5b1c3ba0174353a2510a029bcf9a6a",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12255
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "FieldInspectionTaskEvents",
    "Name": "TR_FieldInspectionTaskEvents_Immutable",
    "observedDefinitionSha256Utf8Lf": "dac9e1f340506a36627b81a7fae07b98144ae5a7975c513402308c052746209e",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12285,
    "migrationSqlSha256Utf8Lf": "ffb0bc505d6725c40d581b86a2aafe70df0a24419e975cd6186fad07bbd8b7dd",
    "expectedCatalogDefinitionSha256Utf8Lf": "ffb0bc505d6725c40d581b86a2aafe70df0a24419e975cd6186fad07bbd8b7dd",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12285
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "FieldInspectionTaskEvents",
    "Name": "TR_FieldInspectionTaskEvents_Scope",
    "observedDefinitionSha256Utf8Lf": "75cf688fd046c15a9a1c003d1a5a9d13b5bdd2917244693d4ae0ec8db82be27a",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12294,
    "migrationSqlSha256Utf8Lf": "71e2296560ec8af061eacd1e507f9f208d3465bde61bdade3d1028ec3c36d315",
    "expectedCatalogDefinitionSha256Utf8Lf": "71e2296560ec8af061eacd1e507f9f208d3465bde61bdade3d1028ec3c36d315",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12294
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "FieldInspectionTasks",
    "Name": "TR_FieldInspectionTasks_H3Scope",
    "observedDefinitionSha256Utf8Lf": "fc7b446275a6caecd017a27ca7e5446b72d448b245e0ab43d94f7a5a88b36244",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261009043010_AllowVerifiedDefectPostRepairTasks",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261009043010_AllowVerifiedDefectPostRepairTasks.cs",
    "sourceLine": 16,
    "migrationSqlSha256Utf8Lf": "33dca35002e493b48b3a450abfacffecdba2733bff3e7da99b49ea61b3605136",
    "expectedCatalogDefinitionSha256Utf8Lf": "468c0d30f80974ca3df1d821d3bcfb7beeeebb31262c1960368bf23eb3ad0c43",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12313
      },
      {
        "MigrationId": "20261009043010_AllowVerifiedDefectPostRepairTasks",
        "Action": "CREATE OR ALTER",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261009043010_AllowVerifiedDefectPostRepairTasks.cs",
        "Line": 16
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "FieldInspectionTasks",
    "Name": "TR_FieldInspectionTasks_H4RepairPin",
    "observedDefinitionSha256Utf8Lf": "f5c2485889ab0cc2e13ef295d7ff79cf263e9c50f0ec7ee2ba945427bcff23cc",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12364,
    "migrationSqlSha256Utf8Lf": "239522ce413c0dc2b5a5c362d2633089fcb88eadf00bf65e617421c52e70a262",
    "expectedCatalogDefinitionSha256Utf8Lf": "239522ce413c0dc2b5a5c362d2633089fcb88eadf00bf65e617421c52e70a262",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12364
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "FieldTaskStartOrigins",
    "Name": "TR_FieldTaskStartOrigins_Immutable",
    "observedDefinitionSha256Utf8Lf": "8bd4ef02669bd704d3cb78ce2875cde670eb6d073130cf802794544199ab0707",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12385,
    "migrationSqlSha256Utf8Lf": "8d8d410a3cfac8e07d1376602b9a4f1aaebfa0b4f4e708b0a41e7f3259c49372",
    "expectedCatalogDefinitionSha256Utf8Lf": "8d8d410a3cfac8e07d1376602b9a4f1aaebfa0b4f4e708b0a41e7f3259c49372",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12385
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "FieldTaskStartOrigins",
    "Name": "TR_FieldTaskStartOrigins_Scope",
    "observedDefinitionSha256Utf8Lf": "fa7b6e65c2d9c0fdcc30bffda181ddb04a57a18bdf0da4a088373864c6069e65",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12394,
    "migrationSqlSha256Utf8Lf": "31c9d13e5d4d835337209d731b19a249165d27f9ea6a64bc4d3be597331b77ad",
    "expectedCatalogDefinitionSha256Utf8Lf": "31c9d13e5d4d835337209d731b19a249165d27f9ea6a64bc4d3be597331b77ad",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12394
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "Files",
    "Name": "TR_Files_Immutable",
    "observedDefinitionSha256Utf8Lf": "515885d587d28f20870fb18f7dc0dcb9ca09d035aa2dea9c4da94b49387188db",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12418,
    "migrationSqlSha256Utf8Lf": "e94f15c3ee9719b77f68dcbdc9415b12024b7a112bb3b696d96a69ef51c35f08",
    "expectedCatalogDefinitionSha256Utf8Lf": "e94f15c3ee9719b77f68dcbdc9415b12024b7a112bb3b696d96a69ef51c35f08",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12418
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "FileScopes",
    "Name": "TR_FileScopes_Immutable",
    "observedDefinitionSha256Utf8Lf": "c092438f5590c9ca0d5f091d964f0b9493ec712e3c6f097de5a95e2a2ee7e618",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12428,
    "migrationSqlSha256Utf8Lf": "488404f0c29ea895d76b95e9d4e88bb30a80761807cd2652d20e28ead46eae46",
    "expectedCatalogDefinitionSha256Utf8Lf": "488404f0c29ea895d76b95e9d4e88bb30a80761807cd2652d20e28ead46eae46",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12428
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "Flights",
    "Name": "TR_Flights_ImmutableSurvey",
    "observedDefinitionSha256Utf8Lf": "2c9ae62bd2e38799b27b3b0097fb02fc9540104919d37db41fdae6c69211e1d8",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12431,
    "migrationSqlSha256Utf8Lf": "66db3d62a54923fca372a0d74a362c974133721066ebb73279c404d0ab8ccf05",
    "expectedCatalogDefinitionSha256Utf8Lf": "66db3d62a54923fca372a0d74a362c974133721066ebb73279c404d0ab8ccf05",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12431
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "GeometryLocationImpactDecisions",
    "Name": "TR_GeometryLocationImpactDecisions_Immutable",
    "observedDefinitionSha256Utf8Lf": "c06dabdcc4c0a78d8fda8c4bbc0d1d7cae76843d28476d4bd6ce0a0c26d6787e",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12445,
    "migrationSqlSha256Utf8Lf": "409d0653f85995a3210dfc1b1e79bc5d66e2f2a1587234b89995c5e8194217f4",
    "expectedCatalogDefinitionSha256Utf8Lf": "409d0653f85995a3210dfc1b1e79bc5d66e2f2a1587234b89995c5e8194217f4",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12445
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "GeometryLocationImpacts",
    "Name": "TR_GeometryLocationImpacts_Immutable",
    "observedDefinitionSha256Utf8Lf": "b0a8d897d8ced6edf025d49e0bc33dc92119446212f1baa41453eef906fa34dc",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12448,
    "migrationSqlSha256Utf8Lf": "44e806aaaaeeb79cdd686151697c0b27fe1d17c5ac692a0bb9685d3cbdc77237",
    "expectedCatalogDefinitionSha256Utf8Lf": "44e806aaaaeeb79cdd686151697c0b27fe1d17c5ac692a0bb9685d3cbdc77237",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12448
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "GeometryLocationImpacts",
    "Name": "TR_GeometryLocationImpacts_Scope",
    "observedDefinitionSha256Utf8Lf": "2f5db9b7e57911c358a05d368cc863cf7755c80ec18ac967a1005652a6f413cf",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12451,
    "migrationSqlSha256Utf8Lf": "7644f19c8d6800d57795b55068b7feaf7af564d289cec5478e147d0638f03a3d",
    "expectedCatalogDefinitionSha256Utf8Lf": "7644f19c8d6800d57795b55068b7feaf7af564d289cec5478e147d0638f03a3d",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12451
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "GeometryMapPublications",
    "Name": "TR_GeometryMapPublications_Immutable",
    "observedDefinitionSha256Utf8Lf": "95a86931caa10bfc6185141a240ef509749db7942cbe03397cbbb524378c3720",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12454,
    "migrationSqlSha256Utf8Lf": "c940776cc1e57a9734424c087c9073eeb521cd1e80ba414bf658f8467d9f78f5",
    "expectedCatalogDefinitionSha256Utf8Lf": "c940776cc1e57a9734424c087c9073eeb521cd1e80ba414bf658f8467d9f78f5",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12454
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "GeometryMapPublications",
    "Name": "TR_GeometryMapPublications_Scope",
    "observedDefinitionSha256Utf8Lf": "bab0910aaaab3a08c3779ba2ca3d8d1ce89ac2bd4c9cda1fe98436453d233c2e",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12457,
    "migrationSqlSha256Utf8Lf": "d93908681283692b587007b5402090a9bbfe0c25045b46a1f8363878be2a8d6e",
    "expectedCatalogDefinitionSha256Utf8Lf": "d93908681283692b587007b5402090a9bbfe0c25045b46a1f8363878be2a8d6e",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12457
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "GroundTruthMeasurements",
    "Name": "TR_GroundTruthMeasurements_Immutable",
    "observedDefinitionSha256Utf8Lf": "0e7e79167d25842833202fc03316e98239510f5e39298078877da686d659eab7",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12460,
    "migrationSqlSha256Utf8Lf": "37c71174615e7605563dc4265a5f997cb639d4336904c24911fce4f24d95e3fa",
    "expectedCatalogDefinitionSha256Utf8Lf": "37c71174615e7605563dc4265a5f997cb639d4336904c24911fce4f24d95e3fa",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12460
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "GroundTruthMeasurements",
    "Name": "TR_GroundTruthMeasurements_Integrity",
    "observedDefinitionSha256Utf8Lf": "b76a738188b470a91b5a623c1c864e35fec0d6c63348bff0d598c5d87d5e8421",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12480,
    "migrationSqlSha256Utf8Lf": "0dae63f425c3852eec7651d05d6eb07fac1cf95145ff0052f69504adc9ad60f9",
    "expectedCatalogDefinitionSha256Utf8Lf": "0dae63f425c3852eec7651d05d6eb07fac1cf95145ff0052f69504adc9ad60f9",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12480
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "H6NotificationAudits",
    "Name": "TR_H6NotificationAudits_Immutable",
    "observedDefinitionSha256Utf8Lf": "be0f26149cb9b800a497b9e2f3a896d0c3b639674b0218e8e5999369fdf58000",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12497,
    "migrationSqlSha256Utf8Lf": "6e7a3b6c6ad253b08a3ca5a0a13815ae5a0da4465fbbdd4c50769c038b778acf",
    "expectedCatalogDefinitionSha256Utf8Lf": "6e7a3b6c6ad253b08a3ca5a0a13815ae5a0da4465fbbdd4c50769c038b778acf",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12497
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "H6NotificationCalendar",
    "Name": "TR_H6NotificationCalendar_Identity",
    "observedDefinitionSha256Utf8Lf": "26f2d86973067fd94809724e38d6e2657b1a1b7f8af396dd4af25dba6b9c7c06",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12501,
    "migrationSqlSha256Utf8Lf": "50b7517bbef49db58ab0ed37f270bdd36b37ff1fb2d84e46df1a6b898660a94d",
    "expectedCatalogDefinitionSha256Utf8Lf": "50b7517bbef49db58ab0ed37f270bdd36b37ff1fb2d84e46df1a6b898660a94d",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12501
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "H6NotificationDeliveries",
    "Name": "TR_H6NotificationDeliveries_Identity",
    "observedDefinitionSha256Utf8Lf": "ec325f4db916bf98d9b6f4ac5a3aa2b6e48137861b4626e9030c91255183251e",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12516,
    "migrationSqlSha256Utf8Lf": "2dbc9344c1bab7e4222a8878cac6685dbd4f894a39db8d5f59e6accef158ae66",
    "expectedCatalogDefinitionSha256Utf8Lf": "2dbc9344c1bab7e4222a8878cac6685dbd4f894a39db8d5f59e6accef158ae66",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12516
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "H6NotificationDeliveries",
    "Name": "TR_H6NotificationDeliveries_Scope",
    "observedDefinitionSha256Utf8Lf": "5a14e62cfca2d341ac3f6043c5c9c623ed97a37b47d0424e16337bed3f55c6e0",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12531,
    "migrationSqlSha256Utf8Lf": "b8bb98bfb8ef84aba036a9c7c6073222a55f576babea6d37952478e93892c198",
    "expectedCatalogDefinitionSha256Utf8Lf": "b8bb98bfb8ef84aba036a9c7c6073222a55f576babea6d37952478e93892c198",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12531
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "H6NotificationDeliveryAttempts",
    "Name": "TR_H6NotificationDeliveryAttempts_Immutable",
    "observedDefinitionSha256Utf8Lf": "f9e37b8204ae1303a4bad7628f703b76995095b2cdcfaf5cace98d43e4ebae1f",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12545,
    "migrationSqlSha256Utf8Lf": "6c32b423800a8609f9a3f9c6f615044dd89c8c3933bb4faa24ef3d34712b5f6b",
    "expectedCatalogDefinitionSha256Utf8Lf": "6c32b423800a8609f9a3f9c6f615044dd89c8c3933bb4faa24ef3d34712b5f6b",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12545
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "H6NotificationEventReceipts",
    "Name": "TR_H6NotificationEventReceipts_Immutable",
    "observedDefinitionSha256Utf8Lf": "29fdf7a0b8aa5b7b04f49cbfeefc21d56891bc651a77f9f3482329421c10e31b",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12549,
    "migrationSqlSha256Utf8Lf": "c2aa01e55814bc6d1fdc09aa15eb9bb6b770040fe9ea5f8e4e5309f4acfd8d46",
    "expectedCatalogDefinitionSha256Utf8Lf": "c2aa01e55814bc6d1fdc09aa15eb9bb6b770040fe9ea5f8e4e5309f4acfd8d46",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12549
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "H6NotificationOccurrences",
    "Name": "TR_H6NotificationOccurrences_Immutable",
    "observedDefinitionSha256Utf8Lf": "69a751578c3207992929a0ca6ff17c8888ef611bbeb631b6e9e039174f2269ef",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12553,
    "migrationSqlSha256Utf8Lf": "77f632094ea919acd00fdd6b96d1fb8cd6603f72da7ca62ffe7acbc6c3f00711",
    "expectedCatalogDefinitionSha256Utf8Lf": "77f632094ea919acd00fdd6b96d1fb8cd6603f72da7ca62ffe7acbc6c3f00711",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12553
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "H6NotificationOccurrences",
    "Name": "TR_H6NotificationOccurrences_Scope",
    "observedDefinitionSha256Utf8Lf": "900e47a1e255f8348de88e71f1a666c97fae103c96b588d741e462baf5445fc1",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12557,
    "migrationSqlSha256Utf8Lf": "33aabe240b6fdec66b4bb54de2165bb2d969e3fe677e987bc21cc7b70494fa2a",
    "expectedCatalogDefinitionSha256Utf8Lf": "33aabe240b6fdec66b4bb54de2165bb2d969e3fe677e987bc21cc7b70494fa2a",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12557
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "H6NotificationScopes",
    "Name": "TR_H6NotificationScopes_Source",
    "observedDefinitionSha256Utf8Lf": "e3ac4a4f4ba8351e8bf0480fdd111791a2648c9e8d71880b29893094a475e33e",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12573,
    "migrationSqlSha256Utf8Lf": "fd43090d7a1196346a34a366cd96c449c4542314b19f2516a2269ec47965e169",
    "expectedCatalogDefinitionSha256Utf8Lf": "fd43090d7a1196346a34a366cd96c449c4542314b19f2516a2269ec47965e169",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12573
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "LD06ActionEvidence",
    "Name": "TR_LD06ActionEvidence_Immutable",
    "observedDefinitionSha256Utf8Lf": "66bfd65fe83a81cf17041d1cc3cd0d4562386f85496520646e7154429557c49d",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12610,
    "migrationSqlSha256Utf8Lf": "4c02584deb73248c90f191c37b13cadf4d652c0f58256d50736bba410a72fa6f",
    "expectedCatalogDefinitionSha256Utf8Lf": "4c02584deb73248c90f191c37b13cadf4d652c0f58256d50736bba410a72fa6f",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12610
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "LD06LifecycleActions",
    "Name": "TR_LD06LifecycleActions_Immutable",
    "observedDefinitionSha256Utf8Lf": "27c019d6b5b6130404a187acec90e8e677f994f1d6a167b3af973cd1c069e0ad",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12616,
    "migrationSqlSha256Utf8Lf": "6b62a95afffc024967e3a7a77f0c8951edb661f04c919c28aa85de984f0cbb91",
    "expectedCatalogDefinitionSha256Utf8Lf": "6b62a95afffc024967e3a7a77f0c8951edb661f04c919c28aa85de984f0cbb91",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12616
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "LD06LifecycleActions",
    "Name": "TR_LD06LifecycleActions_Scope",
    "observedDefinitionSha256Utf8Lf": "8e0ed3282e46d9b4abbc3a69b521a194d7b602bd7b005c3fa4d328d71479ef38",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12622,
    "migrationSqlSha256Utf8Lf": "9eca5d57e03ae3266cf7ec9006d98225552558498aa8b5b9a76f0cd1fa08bff6",
    "expectedCatalogDefinitionSha256Utf8Lf": "9eca5d57e03ae3266cf7ec9006d98225552558498aa8b5b9a76f0cd1fa08bff6",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12622
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "NativeRouteVersionFacts",
    "Name": "TR_NativeRouteVersionFacts_Immutable",
    "observedDefinitionSha256Utf8Lf": "ac5e2b1461c228a6f31fabbe4cdee8e00699ad1febb6c24b7275836e18abc1ea",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12643,
    "migrationSqlSha256Utf8Lf": "b961c63265ffaecf9463ab623bfd2f4c699f49f3453bfa58610255feaba3efad",
    "expectedCatalogDefinitionSha256Utf8Lf": "b961c63265ffaecf9463ab623bfd2f4c699f49f3453bfa58610255feaba3efad",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12643
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "NativeRouteVersionFacts",
    "Name": "TR_NativeRouteVersionFacts_Scope",
    "observedDefinitionSha256Utf8Lf": "c82024ad62741c94caa2e02d2cf647e5142e7a75f18ffd4c7c117d05ffb2f6bc",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12646,
    "migrationSqlSha256Utf8Lf": "59df302df4162dfb39e89fa191ef8f27afe4aace4b631a79dfe781951a66444f",
    "expectedCatalogDefinitionSha256Utf8Lf": "59df302df4162dfb39e89fa191ef8f27afe4aace4b631a79dfe781951a66444f",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12646
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "ObligationResponsibilities",
    "Name": "TR_ObligationResponsibilities_Scope",
    "observedDefinitionSha256Utf8Lf": "ad19174820045ea34aceab07fa108aab7b4a21ef26fc5406e42f850bbe39daa1",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12649,
    "migrationSqlSha256Utf8Lf": "0ec39995713e95e1e283dcfc63cf414849f416c6621eadfc34af554822885c9f",
    "expectedCatalogDefinitionSha256Utf8Lf": "0ec39995713e95e1e283dcfc63cf414849f416c6621eadfc34af554822885c9f",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12649
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "OfflineAdmittedFileReferences",
    "Name": "TR_OfflineAdmittedFileReferences_Immutable",
    "observedDefinitionSha256Utf8Lf": "0ede08ede26d41d8c2f94368373b2ed45184a0b3b7f1c4afeb6b43a68c7fd01e",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12668,
    "migrationSqlSha256Utf8Lf": "e4b2fc7bff1eb3dffebcaeded1b496cc5fca0103a0141642a7e52a1e915a1ccd",
    "expectedCatalogDefinitionSha256Utf8Lf": "e4b2fc7bff1eb3dffebcaeded1b496cc5fca0103a0141642a7e52a1e915a1ccd",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12668
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "OfflineAdmittedFileReferences",
    "Name": "TR_OfflineAdmittedFileReferences_Scope",
    "observedDefinitionSha256Utf8Lf": "8f10f832de0236dbcbac9387f774a3c76bb88e4f3ee60fb5b63cf3e1af4fae79",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12675,
    "migrationSqlSha256Utf8Lf": "1b8576b3ccfa00e091e6720d68e7feb360df7aa3d9f70c41dcdf5f67601663fe",
    "expectedCatalogDefinitionSha256Utf8Lf": "1b8576b3ccfa00e091e6720d68e7feb360df7aa3d9f70c41dcdf5f67601663fe",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12675
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "OfflineDeviceRegistrations",
    "Name": "TR_OfflineDeviceRegistrations_Immutable",
    "observedDefinitionSha256Utf8Lf": "1fb5882b4180dfb4b03449aadc5140f0026d6d270c07953e88ccbd9abb315cfb",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12695,
    "migrationSqlSha256Utf8Lf": "14b6b4f820c24770b06734fee8be72ef109ec4746a9cf0ec1afd3bd0d1a0322a",
    "expectedCatalogDefinitionSha256Utf8Lf": "14b6b4f820c24770b06734fee8be72ef109ec4746a9cf0ec1afd3bd0d1a0322a",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12695
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "OfflineDeviceRegistrations",
    "Name": "TR_OfflineDeviceRegistrations_Scope",
    "observedDefinitionSha256Utf8Lf": "276710322f650a069c9e619c6335225fdab3f5ef95c394f8f64b470601889469",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12702,
    "migrationSqlSha256Utf8Lf": "5c159e9dfa6f4e73da6ecf27c70d082b733f54955c39c2d1c9b388f3fd722fee",
    "expectedCatalogDefinitionSha256Utf8Lf": "5c159e9dfa6f4e73da6ecf27c70d082b733f54955c39c2d1c9b388f3fd722fee",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12702
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "OfflineDeviceRevocations",
    "Name": "TR_OfflineDeviceRevocations_Immutable",
    "observedDefinitionSha256Utf8Lf": "43d0aa081618ef7df31e1b847214d8ff4e3aebaa0f24e6f897c1b45d63d2d95c",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12710,
    "migrationSqlSha256Utf8Lf": "0e863974b2f34678ea82d72eeac56ec211805d044c85b6312816f95232b2ee9a",
    "expectedCatalogDefinitionSha256Utf8Lf": "0e863974b2f34678ea82d72eeac56ec211805d044c85b6312816f95232b2ee9a",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12710
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "OfflineDeviceRevocations",
    "Name": "TR_OfflineDeviceRevocations_Scope",
    "observedDefinitionSha256Utf8Lf": "5ed9c51bc9fe94a78bb472ae6eda2f90a0114d041415750f9acfdc7841dacfeb",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12717,
    "migrationSqlSha256Utf8Lf": "36e6ca20aaf1aa72e10e7198fdc1f2a9aac2cb9b6874fb4e513bab389f307233",
    "expectedCatalogDefinitionSha256Utf8Lf": "36e6ca20aaf1aa72e10e7198fdc1f2a9aac2cb9b6874fb4e513bab389f307233",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12717
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "OfflineEncryptedCaptureArtifacts",
    "Name": "TR_OfflineEncryptedCaptureArtifacts_Immutable",
    "observedDefinitionSha256Utf8Lf": "3271978a1be60480b8496407e3fc6b9795d18895b8081aa990525328703ba93e",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12725,
    "migrationSqlSha256Utf8Lf": "302dbc4d04b58b1fac772fb5aff97ae3fd83379d9f9a36fcb166359abc321828",
    "expectedCatalogDefinitionSha256Utf8Lf": "302dbc4d04b58b1fac772fb5aff97ae3fd83379d9f9a36fcb166359abc321828",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12725
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "OfflineEncryptedCaptureArtifacts",
    "Name": "TR_OfflineEncryptedCaptureArtifacts_Scope",
    "observedDefinitionSha256Utf8Lf": "6fedd2f79305164070eff1229b35484e7435776aee61af7ed4d7a24e77befb61",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12732,
    "migrationSqlSha256Utf8Lf": "70e94deb255f0f57b71d1b645885ac92652613e915847fc6906de5aaf9da7901",
    "expectedCatalogDefinitionSha256Utf8Lf": "70e94deb255f0f57b71d1b645885ac92652613e915847fc6906de5aaf9da7901",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12732
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "OfflineEncryptedPackages",
    "Name": "TR_OfflineEncryptedPackages_Immutable",
    "observedDefinitionSha256Utf8Lf": "a0fd0f9e5e587b7c02cd10f1deb4458c9f8427c74f389872e9d1d7c3d6d8421d",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12742,
    "migrationSqlSha256Utf8Lf": "1f15602212a04abb5dcbcd437c7292d8a3f6544347eac03b7cbb856910481e12",
    "expectedCatalogDefinitionSha256Utf8Lf": "1f15602212a04abb5dcbcd437c7292d8a3f6544347eac03b7cbb856910481e12",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12742
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "OfflineEncryptedPackages",
    "Name": "TR_OfflineEncryptedPackages_Scope",
    "observedDefinitionSha256Utf8Lf": "2f25878e2c8f77a8f04a6b7e63139b5badf304d69b92d0c3233832d344125ae5",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12749,
    "migrationSqlSha256Utf8Lf": "e89931a94c6e38fcbe33265956474c4616aab1a32e5755f572870661e23aac35",
    "expectedCatalogDefinitionSha256Utf8Lf": "e89931a94c6e38fcbe33265956474c4616aab1a32e5755f572870661e23aac35",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12749
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "OfflineEvidenceCaptureReferences",
    "Name": "TR_OfflineEvidenceCaptureReferences_Immutable",
    "observedDefinitionSha256Utf8Lf": "c3e01e55ff62d5206e8efc3531b183cc64114892cf17190cd5f2834dffbdb5ff",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12758,
    "migrationSqlSha256Utf8Lf": "2446e4cff7d39ddd33564d6362e178750c8b53e3410d029b75b2c2a3c86a61d2",
    "expectedCatalogDefinitionSha256Utf8Lf": "2446e4cff7d39ddd33564d6362e178750c8b53e3410d029b75b2c2a3c86a61d2",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12758
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "OfflineEvidenceCaptureReferences",
    "Name": "TR_OfflineEvidenceCaptureReferences_Scope",
    "observedDefinitionSha256Utf8Lf": "2c1244dd14d4760b83154304157296f6b8e77d7cbc2c0baebdd9a822854f2663",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12765,
    "migrationSqlSha256Utf8Lf": "8d22b5bbd74f382eb5070b427cedc7d928701ac5c899c5023eef700b231b9962",
    "expectedCatalogDefinitionSha256Utf8Lf": "8d22b5bbd74f382eb5070b427cedc7d928701ac5c899c5023eef700b231b9962",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12765
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "OfflineHandoverGrantRevocations",
    "Name": "TR_OfflineHandoverGrantRevocations_Immutable",
    "observedDefinitionSha256Utf8Lf": "31afe98ac3e7fc2e009528e6e72648006f7179029746e431b0773c6123f2cb58",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12776,
    "migrationSqlSha256Utf8Lf": "8038492095dc38a5f40fd43486c86e5d7438c434149924affa4e05f7ff3ba414",
    "expectedCatalogDefinitionSha256Utf8Lf": "8038492095dc38a5f40fd43486c86e5d7438c434149924affa4e05f7ff3ba414",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12776
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "OfflineHandoverGrantRevocations",
    "Name": "TR_OfflineHandoverGrantRevocations_Scope",
    "observedDefinitionSha256Utf8Lf": "8c30112cf6be5a662157b52e532b157de788c3835c9d175ebda38630e670ea29",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12783,
    "migrationSqlSha256Utf8Lf": "25e722182b6301df2bf3d6a29d17b3b92b4cf80eefd6ad1a56c0ef08881e3d97",
    "expectedCatalogDefinitionSha256Utf8Lf": "25e722182b6301df2bf3d6a29d17b3b92b4cf80eefd6ad1a56c0ef08881e3d97",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12783
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "OfflineHandoverGrants",
    "Name": "TR_OfflineHandoverGrants_Immutable",
    "observedDefinitionSha256Utf8Lf": "b4d9c0791a28f9604b4bf37e90461bd90838f56b4d0eba27964c9ff0b07d0b16",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12791,
    "migrationSqlSha256Utf8Lf": "9e123deb78f3009a26c4e5a61da8bcec82c4581a50b86a2acfd859924353aed7",
    "expectedCatalogDefinitionSha256Utf8Lf": "9e123deb78f3009a26c4e5a61da8bcec82c4581a50b86a2acfd859924353aed7",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12791
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "OfflineHandoverGrants",
    "Name": "TR_OfflineHandoverGrants_Scope",
    "observedDefinitionSha256Utf8Lf": "38bbf11761a5563ab4b4e4a1bf880a0b02e10863a9d218af0bd6b01633c9a00b",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12798,
    "migrationSqlSha256Utf8Lf": "b938c78a2ba971c24246419f5fbce5091046a33a6e24941a9086fea84bb48853",
    "expectedCatalogDefinitionSha256Utf8Lf": "b938c78a2ba971c24246419f5fbce5091046a33a6e24941a9086fea84bb48853",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12798
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "OfflineOperationAdmissions",
    "Name": "TR_OfflineOperationAdmissions_Immutable",
    "observedDefinitionSha256Utf8Lf": "bbf0b9c877f79a193bcd76e8367492b0e7f46fce3473f0d2fe3482aca130a553",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12812,
    "migrationSqlSha256Utf8Lf": "0f8cef3d448bc4e445579882ca6a8bd858df1d216986c3f22e4dc112be65755e",
    "expectedCatalogDefinitionSha256Utf8Lf": "0f8cef3d448bc4e445579882ca6a8bd858df1d216986c3f22e4dc112be65755e",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12812
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "OfflineOperationAdmissions",
    "Name": "TR_OfflineOperationAdmissions_Scope",
    "observedDefinitionSha256Utf8Lf": "8976a6bfae4c10fb10c6f05865bdfd4ba59ccde512e5e75476f67ae2a8ccee84",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12819,
    "migrationSqlSha256Utf8Lf": "43cbc01d2517ead8f869170264833b448fad2b2eabcfd3f975b8a992602b7e40",
    "expectedCatalogDefinitionSha256Utf8Lf": "43cbc01d2517ead8f869170264833b448fad2b2eabcfd3f975b8a992602b7e40",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12819
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "OfflineOperationBindings",
    "Name": "TR_OfflineOperationBindings_Immutable",
    "observedDefinitionSha256Utf8Lf": "d3db2383fcfb554cd17c4ad1cdbb8a4b5ef81b247fbfd51b1b5de7018730fbdb",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12830,
    "migrationSqlSha256Utf8Lf": "98f6c7d058de4c4dd7d3ecf03dbdce5c121cb74f6ad0f58688fba60eeb6d4f94",
    "expectedCatalogDefinitionSha256Utf8Lf": "98f6c7d058de4c4dd7d3ecf03dbdce5c121cb74f6ad0f58688fba60eeb6d4f94",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12830
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "OfflineOperationBindings",
    "Name": "TR_OfflineOperationBindings_Scope",
    "observedDefinitionSha256Utf8Lf": "9d80b9ac72ac091319a14932633be5a563f60d53edf13bb8c2dcef9eb3084381",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12837,
    "migrationSqlSha256Utf8Lf": "25e96475eb63985cf18ff65b2f07e63aed49ccfcea6c7693e131ce9cfb6294a2",
    "expectedCatalogDefinitionSha256Utf8Lf": "25e96475eb63985cf18ff65b2f07e63aed49ccfcea6c7693e131ce9cfb6294a2",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12837
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "OfflineOperationResults",
    "Name": "TR_OfflineOperationResults_Immutable",
    "observedDefinitionSha256Utf8Lf": "c8b1096fe01e64906ef7e4d185c1e19ba61856c7ada3a6f3079a57a1873808d8",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12850,
    "migrationSqlSha256Utf8Lf": "560f889cfad9f4cabf850d7ad45684907818057f19e680bdd1ea369e9eec98a6",
    "expectedCatalogDefinitionSha256Utf8Lf": "560f889cfad9f4cabf850d7ad45684907818057f19e680bdd1ea369e9eec98a6",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12850
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "OfflineOperationResults",
    "Name": "TR_OfflineOperationResults_Scope",
    "observedDefinitionSha256Utf8Lf": "12e0196d3bacc6e382943de8c438907109e66394b602a3ad90745596fd833a21",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12857,
    "migrationSqlSha256Utf8Lf": "7727a6512d08927cac53c6975ba29631ca2078cf0c44ff575aa939dec98cc47e",
    "expectedCatalogDefinitionSha256Utf8Lf": "7727a6512d08927cac53c6975ba29631ca2078cf0c44ff575aa939dec98cc47e",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12857
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "OfflineOriginTimeVerifications",
    "Name": "TR_OfflineOriginTimeVerifications_Immutable",
    "observedDefinitionSha256Utf8Lf": "c7ae74bba818a3dd24cd0473b06742272ed568a5ce803fded7c04e1b29aed5ac",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12887,
    "migrationSqlSha256Utf8Lf": "905b6d98281b7f180db358d07c9f8b506d5b196ad9ee6fb228e91cdac7de21f1",
    "expectedCatalogDefinitionSha256Utf8Lf": "905b6d98281b7f180db358d07c9f8b506d5b196ad9ee6fb228e91cdac7de21f1",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12887
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "OfflineOriginTimeVerifications",
    "Name": "TR_OfflineOriginTimeVerifications_Scope",
    "observedDefinitionSha256Utf8Lf": "bf2941caf9e398ee8f7e7abec3fbae244a0879a5d8e3352b1dc9d236e712d7bc",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12894,
    "migrationSqlSha256Utf8Lf": "19997b5c8d1f3e51a2176673a3b6702bad67b3478d4858603ee22381cd09784b",
    "expectedCatalogDefinitionSha256Utf8Lf": "19997b5c8d1f3e51a2176673a3b6702bad67b3478d4858603ee22381cd09784b",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12894
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "OfflinePackageFileReferences",
    "Name": "TR_OfflinePackageFileReferences_Immutable",
    "observedDefinitionSha256Utf8Lf": "15b03aa658d28b2b466b91c7fe10c896b204ff6b5b531465a9c5f3a4f5696a9a",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12911,
    "migrationSqlSha256Utf8Lf": "bd85f916a57ac320cb88eb3a52499577ae2192874156d68c3d43ea42a5c6f248",
    "expectedCatalogDefinitionSha256Utf8Lf": "bd85f916a57ac320cb88eb3a52499577ae2192874156d68c3d43ea42a5c6f248",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12911
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "OfflinePackageFileReferences",
    "Name": "TR_OfflinePackageFileReferences_Scope",
    "observedDefinitionSha256Utf8Lf": "c1b8a333c786ccbd275d14adcd9e3b4fa5dcd16f3f14d92d5d393e2c9c54ed48",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12918,
    "migrationSqlSha256Utf8Lf": "ec841ee71c7cd16c9d6018bcd90b24435c98e9e34e50590f3d999de9489b7a0b",
    "expectedCatalogDefinitionSha256Utf8Lf": "ec841ee71c7cd16c9d6018bcd90b24435c98e9e34e50590f3d999de9489b7a0b",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12918
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "OfflineSyncBatches",
    "Name": "TR_OfflineSyncBatches_Immutable",
    "observedDefinitionSha256Utf8Lf": "cb877515207467c60bf8db3504a457ac328089e4dec1c3080fc3c50615961733",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12926,
    "migrationSqlSha256Utf8Lf": "063355daf2f80aa84f0092e3f7aaa2eb7086cd41d26ec7276d074c135ffc6beb",
    "expectedCatalogDefinitionSha256Utf8Lf": "063355daf2f80aa84f0092e3f7aaa2eb7086cd41d26ec7276d074c135ffc6beb",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12926
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "OfflineSyncBatches",
    "Name": "TR_OfflineSyncBatches_Scope",
    "observedDefinitionSha256Utf8Lf": "25b3cd2d6cc62bc11d25a4c960cecc6870b220531e8941dba665237fb13a0a01",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12933,
    "migrationSqlSha256Utf8Lf": "5b0a0bdbb3ec69d0c01f316d34dabcb457f9b29d13ec46044f3410621889b694",
    "expectedCatalogDefinitionSha256Utf8Lf": "5b0a0bdbb3ec69d0c01f316d34dabcb457f9b29d13ec46044f3410621889b694",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12933
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "OfflineTaskSnapshots",
    "Name": "TR_OfflineTaskSnapshots_Immutable",
    "observedDefinitionSha256Utf8Lf": "06129293fba412a169897ddfa5024e485faad213780418709c9fcf4017e3be96",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12946,
    "migrationSqlSha256Utf8Lf": "e9392e83759c1a8de7feffda2c28bc02e07ceec8aa0362c7d8b994050f8d9ff6",
    "expectedCatalogDefinitionSha256Utf8Lf": "e9392e83759c1a8de7feffda2c28bc02e07ceec8aa0362c7d8b994050f8d9ff6",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12946
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "OfflineTaskSnapshots",
    "Name": "TR_OfflineTaskSnapshots_Scope",
    "observedDefinitionSha256Utf8Lf": "33bb280bee2f31cd332a98822f05b46d81b0af8ac02b8b7388e56b143dc3154f",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12953,
    "migrationSqlSha256Utf8Lf": "f7a03d69667462c671a29d4d15e5244339e8057ee0066ec987a1fbfebe82e0ea",
    "expectedCatalogDefinitionSha256Utf8Lf": "f7a03d69667462c671a29d4d15e5244339e8057ee0066ec987a1fbfebe82e0ea",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12953
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "PasswordResetLogs",
    "Name": "TR_PasswordResetLogs_AppendOnly",
    "observedDefinitionSha256Utf8Lf": "bea32a8d647d750db82958b2701cc4e3f36c58307fcd4876ebd4af1621d244fe",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12964,
    "migrationSqlSha256Utf8Lf": "ba93c39ed5a76f4c5e76083bab712eb6b06b38b99e9748c27bd78e00f1427a29",
    "expectedCatalogDefinitionSha256Utf8Lf": "ba93c39ed5a76f4c5e76083bab712eb6b06b38b99e9748c27bd78e00f1427a29",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12964
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "PavementLayoutRevisions",
    "Name": "TR_PavementLayoutRevisions_Immutable",
    "observedDefinitionSha256Utf8Lf": "0f4e9e822564fd917583da07c6cbf1a40f3faa38a83c5cb637c5f9113759f832",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12974,
    "migrationSqlSha256Utf8Lf": "91f82860909a6c3eea9d5d8e4c7064997aebf8a0ad18ba4e21e55a9fe15c6894",
    "expectedCatalogDefinitionSha256Utf8Lf": "91f82860909a6c3eea9d5d8e4c7064997aebf8a0ad18ba4e21e55a9fe15c6894",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12974
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "PavementLayoutRevisions",
    "Name": "TR_PavementLayoutRevisions_Scope",
    "observedDefinitionSha256Utf8Lf": "7d7a9d6c3fd68577d4d41a690bc456b69621fa541df0af0b2c7cc583c05b00ef",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12977,
    "migrationSqlSha256Utf8Lf": "bfd6b54ec08a50c5ac3daa327f349d63446df32be6037a082aea259c9f17eeb0",
    "expectedCatalogDefinitionSha256Utf8Lf": "bfd6b54ec08a50c5ac3daa327f349d63446df32be6037a082aea259c9f17eeb0",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12977
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "PavementSourceFileReferences",
    "Name": "TR_PavementSourceFileReferences_Immutable",
    "observedDefinitionSha256Utf8Lf": "0c116d25635d8096c7d80836e67f05277e06bd5827090c60958eec287ea2276c",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12980,
    "migrationSqlSha256Utf8Lf": "fc8eca875f00116ce09318054ed7477b35071f0832d6cfcd1c70972569eb11b7",
    "expectedCatalogDefinitionSha256Utf8Lf": "fc8eca875f00116ce09318054ed7477b35071f0832d6cfcd1c70972569eb11b7",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12980
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "PavementSourceFileReferences",
    "Name": "TR_PavementSourceFileReferences_Scope",
    "observedDefinitionSha256Utf8Lf": "41a8d6f2453f15c8da0b0138d649d40325af0937a59b7ee6c2aadfaf28c6e38e",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12983,
    "migrationSqlSha256Utf8Lf": "7b7fb9a5593ecc5da487299894a375bff7d31deb55f09ef6cca2b5947962ffdd",
    "expectedCatalogDefinitionSha256Utf8Lf": "7b7fb9a5593ecc5da487299894a375bff7d31deb55f09ef6cca2b5947962ffdd",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12983
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "ProcessingAttempts",
    "Name": "TR_ProcessingAttempts_AppendOnly",
    "observedDefinitionSha256Utf8Lf": "39bd79fa6eb99c82d77c12bb9a5a3515f4c733a426d06fe166bbe47ce0b4a421",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 12986,
    "migrationSqlSha256Utf8Lf": "19c0be9efd1627083f0de1ce9cf0d6107f58e4f95e022361dd23d778789f46fa",
    "expectedCatalogDefinitionSha256Utf8Lf": "19c0be9efd1627083f0de1ce9cf0d6107f58e4f95e022361dd23d778789f46fa",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 12986
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "ProcessingBlocks",
    "Name": "TR_ProcessingBlocks_Immutable",
    "observedDefinitionSha256Utf8Lf": "296cce851bf563a1a84a0aacad7303d34968ea521b78f46e506b4f072853d0b3",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13008,
    "migrationSqlSha256Utf8Lf": "ddc17e6f3623044807f06cbbd68d15f5de1953529539b42412a61abe25ca95e7",
    "expectedCatalogDefinitionSha256Utf8Lf": "ddc17e6f3623044807f06cbbd68d15f5de1953529539b42412a61abe25ca95e7",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13008
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "ProjectLifecycleHistory",
    "Name": "TR_ProjectLifecycleHistory_Immutable",
    "observedDefinitionSha256Utf8Lf": "65a1fb44ec1c65cc8e0b689f5baa4d66d980d4d82d525381a8ddd26392007e47",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13018,
    "migrationSqlSha256Utf8Lf": "dedbd214f67ea0d9903c4284079cc3f2abebeae3f274d5c7442cd2f91705cccb",
    "expectedCatalogDefinitionSha256Utf8Lf": "dedbd214f67ea0d9903c4284079cc3f2abebeae3f274d5c7442cd2f91705cccb",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13018
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "ProjectLifecycleHistory",
    "Name": "TR_ProjectLifecycleHistory_Production",
    "observedDefinitionSha256Utf8Lf": "37c2d385f37a39a754b9cf77fe3f1d4e2a9f1fa68915aea68862e3c9bdf4ab4b",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13025,
    "migrationSqlSha256Utf8Lf": "4151d668e8ff26e0a3311a2f954fad08fa8e4d7793c95f6fed79dc470792f02b",
    "expectedCatalogDefinitionSha256Utf8Lf": "4151d668e8ff26e0a3311a2f954fad08fa8e4d7793c95f6fed79dc470792f02b",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13025
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "ProjectLifecycleHistory",
    "Name": "TR_ProjectLifecycleHistory_Scope",
    "observedDefinitionSha256Utf8Lf": "6a10ca4638c6e19ed766003bc444ba8be1906f4dea0873078204157a1ba333d5",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13036,
    "migrationSqlSha256Utf8Lf": "919124ed355afb944c7f52c9cce0f8045cc0672343e8359acb4b60c4a4ad8814",
    "expectedCatalogDefinitionSha256Utf8Lf": "919124ed355afb944c7f52c9cce0f8045cc0672343e8359acb4b60c4a4ad8814",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13036
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "RefreshTokens",
    "Name": "TR_RefreshTokens_LifecycleExpiry",
    "observedDefinitionSha256Utf8Lf": "4589bddea10cb5522ca838a3a9d4677b29a1bfe94045a48b13ead345d5ac9afa",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13061,
    "migrationSqlSha256Utf8Lf": "254b2e8f606805d3da5066a6bbc9de403efb52ecd4e6e98c53d8e76d08aec2b5",
    "expectedCatalogDefinitionSha256Utf8Lf": "254b2e8f606805d3da5066a6bbc9de403efb52ecd4e6e98c53d8e76d08aec2b5",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13061
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "RepairActualScopes",
    "Name": "TR_RepairActualScopes_Scope",
    "observedDefinitionSha256Utf8Lf": "d1bb96f3c52247fbc7ed831cdb9283e2170c0e17f977a4f10c2522f949aa422f",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13068,
    "migrationSqlSha256Utf8Lf": "328d59271272079350a4c1e8f0c0991302ebff23d84946165aacb0bb57889e76",
    "expectedCatalogDefinitionSha256Utf8Lf": "328d59271272079350a4c1e8f0c0991302ebff23d84946165aacb0bb57889e76",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13068
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "RepairAssessmentEvidence",
    "Name": "TR_RepairAssessmentEvidence_Immutable",
    "observedDefinitionSha256Utf8Lf": "da4456a69184fc8b543e0d290f11fc3d22166906aab6136bda54abe45620b684",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13081,
    "migrationSqlSha256Utf8Lf": "75dfad393ef7e2eafaafcd4ab91b9b2d2bb2505372c08590cbbdb07f7194c9ba",
    "expectedCatalogDefinitionSha256Utf8Lf": "75dfad393ef7e2eafaafcd4ab91b9b2d2bb2505372c08590cbbdb07f7194c9ba",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13081
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "RepairAssessmentMeasurements",
    "Name": "TR_RepairAssessmentMeasurements_Immutable",
    "observedDefinitionSha256Utf8Lf": "b0ffff1bcc7ea7c80527fb2fc1376c6184e42e60b23d5a05e4b18e89c0fc042b",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13089,
    "migrationSqlSha256Utf8Lf": "90f4fecaa5908e3c5070279660433b8cd4bc4566296c35d37cbfe60703207c5c",
    "expectedCatalogDefinitionSha256Utf8Lf": "90f4fecaa5908e3c5070279660433b8cd4bc4566296c35d37cbfe60703207c5c",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13089
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "RepairAttemptEvidence",
    "Name": "TR_RepairAttemptEvidence_Immutable",
    "observedDefinitionSha256Utf8Lf": "8bf512d6c34efe5e0e8b44150a33f0db79b581dc1f8b420718d49dbadf4cba30",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13097,
    "migrationSqlSha256Utf8Lf": "ec4d9e599aa4bb270c2765ab493e80746fb2b95f166f4e52b28d64fa3e426c43",
    "expectedCatalogDefinitionSha256Utf8Lf": "ec4d9e599aa4bb270c2765ab493e80746fb2b95f166f4e52b28d64fa3e426c43",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13097
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "RepairAttemptReviews",
    "Name": "TR_RepairAttemptReviews_Immutable",
    "observedDefinitionSha256Utf8Lf": "94b4cff2620a9ac49584c71a2a237327748208b1d43e5010c1baeaf7a00fcc37",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13106,
    "migrationSqlSha256Utf8Lf": "9b1861d726f794582621fce5dc60f32a15fe133aa499526613aca2c4ab12f384",
    "expectedCatalogDefinitionSha256Utf8Lf": "9b1861d726f794582621fce5dc60f32a15fe133aa499526613aca2c4ab12f384",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13106
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "RepairAttempts",
    "Name": "TR_RepairAttempts_Immutable",
    "observedDefinitionSha256Utf8Lf": "206010e98df8062614327fd582c76bbf11ed02f38dad565c304234cda2502432",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13114,
    "migrationSqlSha256Utf8Lf": "3b9645346e68497f5ed8cce26f4a08a9cda01b83270265ccc4ab0c702bcba8e8",
    "expectedCatalogDefinitionSha256Utf8Lf": "3b9645346e68497f5ed8cce26f4a08a9cda01b83270265ccc4ab0c702bcba8e8",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13114
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "RepairAttemptSubmissionLinks",
    "Name": "TR_RepairAttemptSubmissionLinks_Immutable",
    "observedDefinitionSha256Utf8Lf": "d374276593e65e756df396d1f2335531f20dce732f6673c918364b8bc9aeb506",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13126,
    "migrationSqlSha256Utf8Lf": "04e44654b924b5dd54a73a3f18359616ad1b7db46b8d98b0d56fcc13c58fcbcf",
    "expectedCatalogDefinitionSha256Utf8Lf": "04e44654b924b5dd54a73a3f18359616ad1b7db46b8d98b0d56fcc13c58fcbcf",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13126
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "RepairCorrectionEvidence",
    "Name": "TR_RepairCorrectionEvidence_Immutable",
    "observedDefinitionSha256Utf8Lf": "4785e0b886dcea8a346291742551f29a76af3971a8aa923aa8ba7a527780d895",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13134,
    "migrationSqlSha256Utf8Lf": "cb13c85eb7c55599ad0b157502d8dcfb19b6cd432bcf5cdf28a7e49f6a5cc6bb",
    "expectedCatalogDefinitionSha256Utf8Lf": "cb13c85eb7c55599ad0b157502d8dcfb19b6cd432bcf5cdf28a7e49f6a5cc6bb",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13134
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "RepairDangerAcknowledgements",
    "Name": "TR_RepairDangerAcknowledgements_Immutable",
    "observedDefinitionSha256Utf8Lf": "7b204b189e4eaa6fabd8968329e90a6bca0146b730c8a92122cab6574cb509c6",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13148,
    "migrationSqlSha256Utf8Lf": "32bf4729663446b23becfc15a6b3badf9d8a21c6a8b407c77962d83b7831dbd3",
    "expectedCatalogDefinitionSha256Utf8Lf": "32bf4729663446b23becfc15a6b3badf9d8a21c6a8b407c77962d83b7831dbd3",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13148
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "RepairDangerWarnings",
    "Name": "TR_RepairDangerWarnings_Immutable",
    "observedDefinitionSha256Utf8Lf": "980131d6417562549be477454fdfa62ff283b2c04afecbb41763e768151adbc1",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13156,
    "migrationSqlSha256Utf8Lf": "13522fc888d54bba8c51bd1443ca7a057bbd0bc3e5110e6dfa45150b5d85cf3d",
    "expectedCatalogDefinitionSha256Utf8Lf": "13522fc888d54bba8c51bd1443ca7a057bbd0bc3e5110e6dfa45150b5d85cf3d",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13156
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "RepairDecisions",
    "Name": "TR_RepairDecisions_Immutable",
    "observedDefinitionSha256Utf8Lf": "6e6bdda0f642a53ebc87e712911d860f7ae0086a04e8b604135d3b6d07c6b38d",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13164,
    "migrationSqlSha256Utf8Lf": "afa5c4128cccce4fac28f80f1df33f26ee8696a4d6c027c57399a26a041c950a",
    "expectedCatalogDefinitionSha256Utf8Lf": "afa5c4128cccce4fac28f80f1df33f26ee8696a4d6c027c57399a26a041c950a",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13164
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "RepairEligibilityAssessments",
    "Name": "TR_RepairEligibilityAssessments_Immutable",
    "observedDefinitionSha256Utf8Lf": "6f6db78d84c1b1535c50ef968a1f0c888b9da015bed3a67fb34d727efae33c5c",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13181,
    "migrationSqlSha256Utf8Lf": "0ef4104ac238661193d3e11309d79a3d8b8531aa5c612d343c4e557311fa29e5",
    "expectedCatalogDefinitionSha256Utf8Lf": "0ef4104ac238661193d3e11309d79a3d8b8531aa5c612d343c4e557311fa29e5",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13181
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "RepairEligibilityHandoverSources",
    "Name": "TR_RepairEligibilityHandoverSources_Immutable",
    "observedDefinitionSha256Utf8Lf": "2e7a18b5f6574f1dc9a76a3c86f6b4c325afee0eded9072713508f1f35a4580d",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13189,
    "migrationSqlSha256Utf8Lf": "fc23b951f8d71aa15cdabb29ca6261c5f73d12f1d5eedabafb9df8180c83a561",
    "expectedCatalogDefinitionSha256Utf8Lf": "fc23b951f8d71aa15cdabb29ca6261c5f73d12f1d5eedabafb9df8180c83a561",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13189
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "RepairEligibilityWarrantySources",
    "Name": "TR_RepairEligibilityWarrantySources_Immutable",
    "observedDefinitionSha256Utf8Lf": "17b8204bbf8f5dee71b432cd9e892fea552e70d697dc016d94d312e2b8626947",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13197,
    "migrationSqlSha256Utf8Lf": "fdac48d5637221737e55663d685e7b7e7937f72dfad75369b1465da01f504eb0",
    "expectedCatalogDefinitionSha256Utf8Lf": "fdac48d5637221737e55663d685e7b7e7937f72dfad75369b1465da01f504eb0",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13197
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "RepairExecutionAuthorizations",
    "Name": "TR_RepairExecutionAuthorizations_Scope",
    "observedDefinitionSha256Utf8Lf": "5be411b6a31cf42dab1698be1a8b66684c9a8395860bf2201c847b80439bab46",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13205,
    "migrationSqlSha256Utf8Lf": "a3eb52fe9b65017e5371dfdf1685c740d25655ded42e7ecb82644a00df78f874",
    "expectedCatalogDefinitionSha256Utf8Lf": "a3eb52fe9b65017e5371dfdf1685c740d25655ded42e7ecb82644a00df78f874",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13205
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "RepairExecutionFinishes",
    "Name": "TR_RepairExecutionFinishes_Immutable",
    "observedDefinitionSha256Utf8Lf": "0abe90423c352e50e6c83a5ce42cfef7d8bd481da97151408be27e6566dea6d4",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13225,
    "migrationSqlSha256Utf8Lf": "bc5c0afd52d013de6b4ef3746b9cc463dddf2529d2e52e8a0cb0b784d89e943a",
    "expectedCatalogDefinitionSha256Utf8Lf": "bc5c0afd52d013de6b4ef3746b9cc463dddf2529d2e52e8a0cb0b784d89e943a",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13225
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "RepairExecutionStarts",
    "Name": "TR_RepairExecutionStarts_Immutable",
    "observedDefinitionSha256Utf8Lf": "1c36e989a0e3141bef067f3f2c1e1b4f828c2bf2fc6f0aa1f0582eceb4cf62ce",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13233,
    "migrationSqlSha256Utf8Lf": "c9e94f164aac5d073bd675022260e0977060475ba3bc5a73e2822d8743755c95",
    "expectedCatalogDefinitionSha256Utf8Lf": "c9e94f164aac5d073bd675022260e0977060475ba3bc5a73e2822d8743755c95",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13233
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "RepairFieldTaskBindings",
    "Name": "TR_RepairFieldTaskBindings_Immutable",
    "observedDefinitionSha256Utf8Lf": "b9b830f96cfd07d7acfc96c9ab632c9c4c4205adb253e3a77c8200223e5d81fb",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13241,
    "migrationSqlSha256Utf8Lf": "325811c4775c264e0af0e177d8907efb84e433262bd1561727ea3a04dff8a4d9",
    "expectedCatalogDefinitionSha256Utf8Lf": "325811c4775c264e0af0e177d8907efb84e433262bd1561727ea3a04dff8a4d9",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13241
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "RepairItemLifecycleEvents",
    "Name": "TR_RepairItemLifecycleEvents_Immutable",
    "observedDefinitionSha256Utf8Lf": "e5972210cc9728d6830b6809f214d0e7ce8a241311c7b5f62abcb7c562587043",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13249,
    "migrationSqlSha256Utf8Lf": "1f374385c1882d43d1a1377b0d00aedc404a590c361ca63cdc9a30c18079a05c",
    "expectedCatalogDefinitionSha256Utf8Lf": "1f374385c1882d43d1a1377b0d00aedc404a590c361ca63cdc9a30c18079a05c",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13249
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "RepairItems",
    "Name": "TR_RepairItems_Scope",
    "observedDefinitionSha256Utf8Lf": "f7581df3bcb1b9d68cda5980125e8c06c71ed9ef9392c3cadf4fff4cab083419",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13257,
    "migrationSqlSha256Utf8Lf": "17299b30102c972b6a4a07e743aed66315e12227cd24e2cdbcac5def67ec4f5e",
    "expectedCatalogDefinitionSha256Utf8Lf": "17299b30102c972b6a4a07e743aed66315e12227cd24e2cdbcac5def67ec4f5e",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13257
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "RepairMeasurementAssessments",
    "Name": "TR_RepairMeasurementAssessments_Immutable",
    "observedDefinitionSha256Utf8Lf": "bf4c8c5b7c6adde801a6d0583811799427dbd1bdf47bcf904f4d90afb4f26401",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13279,
    "migrationSqlSha256Utf8Lf": "221a5ec710c54c94c21ef1f596982ebd94d525e37adbf0eb37eb655cabf3fa04",
    "expectedCatalogDefinitionSha256Utf8Lf": "221a5ec710c54c94c21ef1f596982ebd94d525e37adbf0eb37eb655cabf3fa04",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13279
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "RepairNormalSuccessors",
    "Name": "TR_RepairNormalSuccessors_Immutable",
    "observedDefinitionSha256Utf8Lf": "b8512f229ed980dc648baf14bd3f4c3844d31227a4163c76e1201cff4030d47f",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13287,
    "migrationSqlSha256Utf8Lf": "bdc5df8ae5caa58acd1cf385c6f511f19e97cc1b1bbf42e636d4bae027b402ed",
    "expectedCatalogDefinitionSha256Utf8Lf": "bdc5df8ae5caa58acd1cf385c6f511f19e97cc1b1bbf42e636d4bae027b402ed",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13287
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "RepairObligationResolutionEvents",
    "Name": "TR_RepairObligationResolutionEvents_Immutable",
    "observedDefinitionSha256Utf8Lf": "f3888997a4c07a0e03642250846f8288d579590abb7352ec38a118003d43e0a1",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13295,
    "migrationSqlSha256Utf8Lf": "9ad5afcd2524dc00668da21c09830c8caa7ba12ff229719080ba30a34616b97e",
    "expectedCatalogDefinitionSha256Utf8Lf": "9ad5afcd2524dc00668da21c09830c8caa7ba12ff229719080ba30a34616b97e",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13295
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "RepairObligations",
    "Name": "TR_RepairObligations_ProducerHeads",
    "observedDefinitionSha256Utf8Lf": "3060f9e09d11ff4e9f4ceab59b5b70b160ab68a8100e4cb0ecc1fc2e910182dd",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13308,
    "migrationSqlSha256Utf8Lf": "8399d06f212e5a207d850219ea6a9cc3961dfb28ba377fadff2414f8bbbf62e2",
    "expectedCatalogDefinitionSha256Utf8Lf": "8399d06f212e5a207d850219ea6a9cc3961dfb28ba377fadff2414f8bbbf62e2",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13308
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "RepairObligations",
    "Name": "TR_RepairObligations_Scope",
    "observedDefinitionSha256Utf8Lf": "a4558946e616f417e00e10c46908b9f64b76e3205e52d1a78d4326ccf6657d5b",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13327,
    "migrationSqlSha256Utf8Lf": "a317ebeab50d442ad4d527178d8e994351a6ec6359e7067974e5ce8fac346452",
    "expectedCatalogDefinitionSha256Utf8Lf": "a317ebeab50d442ad4d527178d8e994351a6ec6359e7067974e5ce8fac346452",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13327
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "RepairPackages",
    "Name": "TR_RepairPackages_Scope",
    "observedDefinitionSha256Utf8Lf": "3f5930bf4f8b8cc048140c113e5c92ad7300a5b3392ee7e535fa62f592b6538d",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13349,
    "migrationSqlSha256Utf8Lf": "e88c58af653f123f977de011e0e9b452de1580093cd91c5e4187564dce30997f",
    "expectedCatalogDefinitionSha256Utf8Lf": "e88c58af653f123f977de011e0e9b452de1580093cd91c5e4187564dce30997f",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13349
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "RepairPolicyDraftChanges",
    "Name": "TR_RepairPolicyDraftChanges_Immutable",
    "observedDefinitionSha256Utf8Lf": "03ac6390e5de1670ffddd1302ab28e892248d24b7e2ea10458bcef7295292ad3",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13362,
    "migrationSqlSha256Utf8Lf": "de7b53e13225b4627c22ff4c3fddfc6c43ae5da4bad102d0b8b972c21c77d794",
    "expectedCatalogDefinitionSha256Utf8Lf": "de7b53e13225b4627c22ff4c3fddfc6c43ae5da4bad102d0b8b972c21c77d794",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13362
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "RepairPolicyDrafts",
    "Name": "TR_RepairPolicyDrafts_Scope",
    "observedDefinitionSha256Utf8Lf": "ec7f666faaee28b78f750b3ad21ba0e9e9b7c443bc85bdf066d77e9db898ba2e",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13371,
    "migrationSqlSha256Utf8Lf": "d3b2864b4abe83eaa9e5ace144a74791d53a502886d7900aa1fbb1fcf2b32257",
    "expectedCatalogDefinitionSha256Utf8Lf": "d3b2864b4abe83eaa9e5ace144a74791d53a502886d7900aa1fbb1fcf2b32257",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13371
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "RepairPolicyMeasurementRules",
    "Name": "TR_RepairPolicyMeasurementRules_Immutable",
    "observedDefinitionSha256Utf8Lf": "8fe6328498a890ad0c957c6c7c1dcedcd20853f05cfa83e86af2970106c4d5a9",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13402,
    "migrationSqlSha256Utf8Lf": "8504fd10cf86bbc6353a6908aa4ff5c28d38bed391160306107035f4d5659537",
    "expectedCatalogDefinitionSha256Utf8Lf": "8504fd10cf86bbc6353a6908aa4ff5c28d38bed391160306107035f4d5659537",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13402
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "RepairPolicyRevisions",
    "Name": "TR_RepairPolicyRevisions_Immutable",
    "observedDefinitionSha256Utf8Lf": "4d90e604defe77238bcf381c62f61a5d46fc11de6c539fd3e90a73e0d723b32f",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13415,
    "migrationSqlSha256Utf8Lf": "efa994329dd88360717e831b35aa32264406b83718e49f76aa0d06bcbfc4cb8a",
    "expectedCatalogDefinitionSha256Utf8Lf": "efa994329dd88360717e831b35aa32264406b83718e49f76aa0d06bcbfc4cb8a",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13415
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "RepairPolicyRevocations",
    "Name": "TR_RepairPolicyRevocations_Immutable",
    "observedDefinitionSha256Utf8Lf": "e5f9f1f34b6d1e3451bb79e656629902e24f59965deaf8569c7347fa2086e1d1",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13424,
    "migrationSqlSha256Utf8Lf": "32ec9221e01d3326732ba401dc820100b0f031e566c2c5842244c4a02cfe2f4f",
    "expectedCatalogDefinitionSha256Utf8Lf": "32ec9221e01d3326732ba401dc820100b0f031e566c2c5842244c4a02cfe2f4f",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13424
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "RepairReviewRequests",
    "Name": "TR_RepairReviewRequests_Immutable",
    "observedDefinitionSha256Utf8Lf": "a91f40d6137a72a37d965f1596c3918549f7f278d1b8f4045b3241302bb9a3f1",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13433,
    "migrationSqlSha256Utf8Lf": "304082febeac31fd287c8ce30f211a81a385155dca8ab7814a274622632478ae",
    "expectedCatalogDefinitionSha256Utf8Lf": "304082febeac31fd287c8ce30f211a81a385155dca8ab7814a274622632478ae",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13433
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "RepairSafetyActionSources",
    "Name": "TR_RepairSafetyActionSources_Immutable",
    "observedDefinitionSha256Utf8Lf": "d53ffe20ea7dd45c89148a89594dfc019dd35546a004a40c2f5159c4d0a1bb09",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13444,
    "migrationSqlSha256Utf8Lf": "7cf25e3332398208ea84cc1af6ff33d6e2f51e56d93c0c163776b82dab9429bb",
    "expectedCatalogDefinitionSha256Utf8Lf": "7cf25e3332398208ea84cc1af6ff33d6e2f51e56d93c0c163776b82dab9429bb",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13444
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "RepairSafetyCheckEvidence",
    "Name": "TR_RepairSafetyCheckEvidence_Immutable",
    "observedDefinitionSha256Utf8Lf": "e961265c76fb8807ed500cfff3d367cb2c836fc27c5b7fa95123defd767d32a0",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13462,
    "migrationSqlSha256Utf8Lf": "6706eae4f80eef4b6f3fdc3893ba640438ad54ff3d0d89da11537d51fa21f1f1",
    "expectedCatalogDefinitionSha256Utf8Lf": "6706eae4f80eef4b6f3fdc3893ba640438ad54ff3d0d89da11537d51fa21f1f1",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13462
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "RepairSafetyChecks",
    "Name": "TR_RepairSafetyChecks_Immutable",
    "observedDefinitionSha256Utf8Lf": "7086e84ecb0e1a2d9ad52cd86de90841220bb42855fb00ff391d37ea4fe98407",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13470,
    "migrationSqlSha256Utf8Lf": "2bdb5baae44ad032e6ad383de645daac8ccb74bd86e6f7f033256439e8f36632",
    "expectedCatalogDefinitionSha256Utf8Lf": "2bdb5baae44ad032e6ad383de645daac8ccb74bd86e6f7f033256439e8f36632",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13470
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "RepairSafetyMonitoring",
    "Name": "TR_RepairSafetyMonitoring_Scope",
    "observedDefinitionSha256Utf8Lf": "8ab45d669551d9fa14a2caf532093b5ce4d4f76488193c4bd8711e5f514ed4d9",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13478,
    "migrationSqlSha256Utf8Lf": "7a0957e2cb0aa21ca825261c4714a7fe38b4d761212caa523468fcbc92c933be",
    "expectedCatalogDefinitionSha256Utf8Lf": "7a0957e2cb0aa21ca825261c4714a7fe38b4d761212caa523468fcbc92c933be",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13478
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "RepairSafetyResponsibilityTransfers",
    "Name": "TR_RepairSafetyResponsibilityTransfers_Immutable",
    "observedDefinitionSha256Utf8Lf": "fbcaaccdc476ae7a9e5bcbc41dd909c8b468c2d96a0d169efa002c6304a1c79b",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13499,
    "migrationSqlSha256Utf8Lf": "e171fa7074bec00b4b9b647eff52b834d16e23d8bcfe619023b0c2ff1149d9ac",
    "expectedCatalogDefinitionSha256Utf8Lf": "e171fa7074bec00b4b9b647eff52b834d16e23d8bcfe619023b0c2ff1149d9ac",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13499
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "RepairTemporarySafetyMeasures",
    "Name": "TR_RepairTemporarySafetyMeasures_Scope",
    "observedDefinitionSha256Utf8Lf": "1a48abd380d19c9854dec027847a9b391fbede0a68ffc88b91922edef7220b46",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13508,
    "migrationSqlSha256Utf8Lf": "cdfee7c72cbd7fd52beb27e47aa5d5644175d2ed378aca337996f6bb722712f1",
    "expectedCatalogDefinitionSha256Utf8Lf": "cdfee7c72cbd7fd52beb27e47aa5d5644175d2ed378aca337996f6bb722712f1",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13508
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "RepairWorkHandovers",
    "Name": "TR_RepairWorkHandovers_Immutable",
    "observedDefinitionSha256Utf8Lf": "184135cd498e41fcf8db5b616f3fbb8e3e946ce44c76fd309402bc1feac8ab07",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13524,
    "migrationSqlSha256Utf8Lf": "e04b22fa1932edd60df1cc4612460f4abafcacf9f435ed39d23a32ab06a1afe1",
    "expectedCatalogDefinitionSha256Utf8Lf": "e04b22fa1932edd60df1cc4612460f4abafcacf9f435ed39d23a32ab06a1afe1",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13524
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "ReportOriginalEvidence",
    "Name": "TR_ReportOriginalEvidence_Identity",
    "observedDefinitionSha256Utf8Lf": "16fa1a6b7fd8abbccad51d3ecbb8f3c52e5bea035a8201b9ee9ee9ef899f9e36",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13533,
    "migrationSqlSha256Utf8Lf": "3fbcb1ed2ce76fc6a0d8974803787ae098bf71babbab0a01419acb69533d8393",
    "expectedCatalogDefinitionSha256Utf8Lf": "3fbcb1ed2ce76fc6a0d8974803787ae098bf71babbab0a01419acb69533d8393",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13533
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "ReportOriginalEvidence",
    "Name": "TR_ReportOriginalEvidence_Immutable",
    "observedDefinitionSha256Utf8Lf": "8bb4d2c78833d6186d5a70f8bb95de91055222a430bc2c77599a1f09c81d7ce8",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13536,
    "migrationSqlSha256Utf8Lf": "75cb8a8572ba56fdb2537247b0a4c988803717342112783192a64494b98117a4",
    "expectedCatalogDefinitionSha256Utf8Lf": "75cb8a8572ba56fdb2537247b0a4c988803717342112783192a64494b98117a4",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13536
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "Reports",
    "Name": "TR_Reports_OriginalImmutable",
    "observedDefinitionSha256Utf8Lf": "1725ba3935d8ce4a337c5fb6d333ab5c475dc81f4380658c66c062415e64ead7",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13539,
    "migrationSqlSha256Utf8Lf": "3e2b406d1c65d0a195ced94721a1795a55b580d05a43e6b39ea1dbd8936e7165",
    "expectedCatalogDefinitionSha256Utf8Lf": "3e2b406d1c65d0a195ced94721a1795a55b580d05a43e6b39ea1dbd8936e7165",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13539
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "ReportSupplementEvidence",
    "Name": "TR_ReportSupplementEvidence_Identity",
    "observedDefinitionSha256Utf8Lf": "a0695c0137a5b8bcccad7be35f4320188c33f51309149a589a7b2b5b7d33fd27",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13542,
    "migrationSqlSha256Utf8Lf": "951946a6363b3cc0f440a728f881db67c59f8dc71909bec2835a936f2e45fa33",
    "expectedCatalogDefinitionSha256Utf8Lf": "951946a6363b3cc0f440a728f881db67c59f8dc71909bec2835a936f2e45fa33",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13542
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "ReportSupplementEvidence",
    "Name": "TR_ReportSupplementEvidence_Immutable",
    "observedDefinitionSha256Utf8Lf": "971f9a2708c4752093092cae0a90f7f9334919a7e9686fc5400423260965081e",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13545,
    "migrationSqlSha256Utf8Lf": "1ec4b5ace1ea795fe9373ae79c0dd0cc238d83e4da41b58aff7f43579af443fa",
    "expectedCatalogDefinitionSha256Utf8Lf": "1ec4b5ace1ea795fe9373ae79c0dd0cc238d83e4da41b58aff7f43579af443fa",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13545
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "ReportSupplements",
    "Name": "TR_ReportSupplements_Immutable",
    "observedDefinitionSha256Utf8Lf": "c87693b42411b9391bbc9393566cd7c4571303c12bf874e0560b53e548568302",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13548,
    "migrationSqlSha256Utf8Lf": "9d3b9238ebf00425e135db4626ad2d6881c79a51006520929956cecc4480773e",
    "expectedCatalogDefinitionSha256Utf8Lf": "9d3b9238ebf00425e135db4626ad2d6881c79a51006520929956cecc4480773e",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13548
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "RetentionBasisRevisions",
    "Name": "TR_RetentionBasisRevisions_Immutable",
    "observedDefinitionSha256Utf8Lf": "98775243be91c2e25172218740fa7876f67762c833fe34674663b5813e5155a7",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13551,
    "migrationSqlSha256Utf8Lf": "ba80115c7d27860b39804909cf22519eb219309ebb15ba5bffb4aa3839d7ea79",
    "expectedCatalogDefinitionSha256Utf8Lf": "ba80115c7d27860b39804909cf22519eb219309ebb15ba5bffb4aa3839d7ea79",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13551
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "RetentionEvaluationItems",
    "Name": "TR_RetentionEvaluationItems_Immutable",
    "observedDefinitionSha256Utf8Lf": "9406620322ff6ec78f8c20da9232e40fd7c6d0f7d00b7453e3e445929d97083c",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13554,
    "migrationSqlSha256Utf8Lf": "108dc7ce592152311f4b822e927948f6be0492b837d8ceec7b3b0063eafcc590",
    "expectedCatalogDefinitionSha256Utf8Lf": "108dc7ce592152311f4b822e927948f6be0492b837d8ceec7b3b0063eafcc590",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13554
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "RetentionHoldHistories",
    "Name": "TR_RetentionHoldHistories_Immutable",
    "observedDefinitionSha256Utf8Lf": "6f5be2c7bf43c44499b9d852111c2d6bc274fcbe411842936ee4ee68e4c387ba",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13557,
    "migrationSqlSha256Utf8Lf": "25fde2866e1586521d194b92d930a13baa67385f14d92ac22b07655a6d2d85cb",
    "expectedCatalogDefinitionSha256Utf8Lf": "25fde2866e1586521d194b92d930a13baa67385f14d92ac22b07655a6d2d85cb",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13557
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "RoadCoverageMappings",
    "Name": "TR_RoadCoverageMappings_Immutable",
    "observedDefinitionSha256Utf8Lf": "0580492a7ce252ccd956c44f833ce1f119b832822377b74f8a1aa8ca62f7ffe8",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13560,
    "migrationSqlSha256Utf8Lf": "8b075d52dfe656e288233436100721add1106ada7b44a78c097153415fff3b91",
    "expectedCatalogDefinitionSha256Utf8Lf": "8b075d52dfe656e288233436100721add1106ada7b44a78c097153415fff3b91",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13560
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "RoadCoverageMappings",
    "Name": "TR_RoadCoverageMappings_Scope",
    "observedDefinitionSha256Utf8Lf": "de2f64b8ef888746b0d3fa9dd439f815a74fc53f5448501f4aeaddb8be6ef032",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13566,
    "migrationSqlSha256Utf8Lf": "9f4e72478e075de0dad5345c3be54a76f82203136e599fb29cdf0c0bbe9dba95",
    "expectedCatalogDefinitionSha256Utf8Lf": "9f4e72478e075de0dad5345c3be54a76f82203136e599fb29cdf0c0bbe9dba95",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13566
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "RoadGeometryMetadata",
    "Name": "TR_RoadGeometryMetadata_Immutable",
    "observedDefinitionSha256Utf8Lf": "489a920fbd78b7404be82594c758f87c49c35de5b0ec15e635f231c2ede0e347",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13596,
    "migrationSqlSha256Utf8Lf": "ba6ba83bd299f3e0be2105ce3cc2b2aaf4fcd1d89523ce4996e042de7f028e38",
    "expectedCatalogDefinitionSha256Utf8Lf": "ba6ba83bd299f3e0be2105ce3cc2b2aaf4fcd1d89523ce4996e042de7f028e38",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13596
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "RoadSectionVersions",
    "Name": "TR_RoadSectionVersions_Immutable",
    "observedDefinitionSha256Utf8Lf": "6df748afd19ce79705f2b83f3f5a9c96bcbc13fd8b14fad0644af1cbb7bd42d7",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13599,
    "migrationSqlSha256Utf8Lf": "dfb6d6eae97f08371109bc8d350693475cb006264baf508ff15b62ede621e548",
    "expectedCatalogDefinitionSha256Utf8Lf": "dfb6d6eae97f08371109bc8d350693475cb006264baf508ff15b62ede621e548",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13599
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "RoadSectionVersions",
    "Name": "TR_RoadSectionVersions_ProfileScope",
    "observedDefinitionSha256Utf8Lf": "a8f7ff97d28609cea6af40d784577c697283c5bedf0e3f009765a925b7f7a104",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13602,
    "migrationSqlSha256Utf8Lf": "94e5fa2d3d01552fb8f2cde3906c0b079d477e38218789ac12da4e5bd39c9477",
    "expectedCatalogDefinitionSha256Utf8Lf": "94e5fa2d3d01552fb8f2cde3906c0b079d477e38218789ac12da4e5bd39c9477",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13602
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "Sessions",
    "Name": "TR_Sessions_ImmutableLifecycle",
    "observedDefinitionSha256Utf8Lf": "c5c62cb7701695b004d493843022e71014c3806313e9212285d6e162e8b3069b",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13605,
    "migrationSqlSha256Utf8Lf": "0aec6942f9aebd7d05469ac7a285024c53de16120afa05c7666e3195fafc50b0",
    "expectedCatalogDefinitionSha256Utf8Lf": "0aec6942f9aebd7d05469ac7a285024c53de16120afa05c7666e3195fafc50b0",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13605
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "SourceDecisions",
    "Name": "TR_SourceDecisions_Immutable",
    "observedDefinitionSha256Utf8Lf": "223674981d264ef3da730746f22a50e68fcb304183c700b495a7df74f81bc144",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13612,
    "migrationSqlSha256Utf8Lf": "70935ae2dc6e67487d2f2054c8dfe1c6457080e95bdf136b5f52a5a871362961",
    "expectedCatalogDefinitionSha256Utf8Lf": "70935ae2dc6e67487d2f2054c8dfe1c6457080e95bdf136b5f52a5a871362961",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13612
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "SurveyDataVersions",
    "Name": "TR_SurveyDataVersions_Immutable",
    "observedDefinitionSha256Utf8Lf": "10bae429e5d7e39d89c67d77c3853765ac6043c7e0b6ca82d6d38387da34a9cd",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13615,
    "migrationSqlSha256Utf8Lf": "8e583cd38b917516cab938531dee602401bc13c746ccee6c21fdf786a97bba36",
    "expectedCatalogDefinitionSha256Utf8Lf": "8e583cd38b917516cab938531dee602401bc13c746ccee6c21fdf786a97bba36",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13615
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "SurveyFiles",
    "Name": "TR_SurveyFiles_ScopeIntegrity",
    "observedDefinitionSha256Utf8Lf": "8c325489901ef96bf87143137be6f7a294b506c6ce1b72e533623b9fcad691a3",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13654,
    "migrationSqlSha256Utf8Lf": "473c0b7afdf5f5c10374edc375823814322fe7c5e661f3ab84ffe7d6050878b4",
    "expectedCatalogDefinitionSha256Utf8Lf": "473c0b7afdf5f5c10374edc375823814322fe7c5e661f3ab84ffe7d6050878b4",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13654
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "SurveyPlanPostponements",
    "Name": "TR_SurveyPlanPostponements_AppendOnly",
    "observedDefinitionSha256Utf8Lf": "9b7ea56b10e62a11af28ee0e8a7b4b8964c0bdf5817189c77fa8e40dc0fbb21e",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13688,
    "migrationSqlSha256Utf8Lf": "c6105c2dd7fc49ac926a595f711f57ad9114e39ec481416e61eaeb638b12fd18",
    "expectedCatalogDefinitionSha256Utf8Lf": "c6105c2dd7fc49ac926a595f711f57ad9114e39ec481416e61eaeb638b12fd18",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13688
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "SurveyPlans",
    "Name": "TR_SurveyPlans_ScopeIntegrity",
    "observedDefinitionSha256Utf8Lf": "a1965d9a772699c7c5f7e6c2da73f05748d7091db9321cd9a6ca2c91400c08d0",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13698,
    "migrationSqlSha256Utf8Lf": "511d879a85839178547ecdfec7c87bf8bac8a9f40785e3018807535111156e0c",
    "expectedCatalogDefinitionSha256Utf8Lf": "511d879a85839178547ecdfec7c87bf8bac8a9f40785e3018807535111156e0c",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13698
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "SurveyRequests",
    "Name": "TR_SurveyRequests_ScopeIntegrity",
    "observedDefinitionSha256Utf8Lf": "793cbb78c41c984ebdedca3eeda16beee38495fb949e4fabb390e8f335b9b6cf",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13719,
    "migrationSqlSha256Utf8Lf": "de65d01df65a5e56cbd92d43eeb2bfdecb2371a1bc248357c117d17457eaf9b5",
    "expectedCatalogDefinitionSha256Utf8Lf": "de65d01df65a5e56cbd92d43eeb2bfdecb2371a1bc248357c117d17457eaf9b5",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13719
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "Surveys",
    "Name": "TR_Surveys_ScopeIntegrity",
    "observedDefinitionSha256Utf8Lf": "d3a7978f37340a7e58540886b621035a96e1404b603d18d60f20de49236c615b",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13754,
    "migrationSqlSha256Utf8Lf": "42acd40ab6eddb42cad691e56bf392ad0efcb04f4c36c73312c9d3c41af90dba",
    "expectedCatalogDefinitionSha256Utf8Lf": "42acd40ab6eddb42cad691e56bf392ad0efcb04f4c36c73312c9d3c41af90dba",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13754
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "TrainingLabelReviews",
    "Name": "TR_TrainingLabelReviews_AppendOnly",
    "observedDefinitionSha256Utf8Lf": "99325f0cfb3ad2f2969c92ddf224000c425b40dbeb28197c65082d04bbfcd0e1",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13793,
    "migrationSqlSha256Utf8Lf": "d618e1e5078f8f598e52f12f60b19be7e8c7f45d5e525d7d6688450ffdf34779",
    "expectedCatalogDefinitionSha256Utf8Lf": "d618e1e5078f8f598e52f12f60b19be7e8c7f45d5e525d7d6688450ffdf34779",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13793
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "TrainingLabelRevisions",
    "Name": "TR_TrainingLabelRevisions_AppendOnly",
    "observedDefinitionSha256Utf8Lf": "ad872c919309f689ba8ee746a2304b807ca2e2392685f37f908956b96060c102",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13796,
    "migrationSqlSha256Utf8Lf": "730dcaa44c20b88dee93778abb8ce0d3ec7f126339b92fa8bf65d2d60f239dcf",
    "expectedCatalogDefinitionSha256Utf8Lf": "730dcaa44c20b88dee93778abb8ce0d3ec7f126339b92fa8bf65d2d60f239dcf",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13796
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "TrainingLabels",
    "Name": "TR_TrainingLabels_Identity",
    "observedDefinitionSha256Utf8Lf": "cb01fc6b8110494152ec302182b1ccca440521b6da6c82fad74ddad61f28f372",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13799,
    "migrationSqlSha256Utf8Lf": "26346fdf48fb51c86a026ca7c917d6406e4fcaf1555026f9f3d2307249945823",
    "expectedCatalogDefinitionSha256Utf8Lf": "26346fdf48fb51c86a026ca7c917d6406e4fcaf1555026f9f3d2307249945823",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13799
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "WeeklyReviewDigestDuties",
    "Name": "TR_WeeklyReviewDigestDuties_Immutable",
    "observedDefinitionSha256Utf8Lf": "0b6c9bfeccdaa8ac7cd69e7fbfa3b71cee7123efc434ac58cd15ff46676401b4",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13802,
    "migrationSqlSha256Utf8Lf": "7d0a5c716a0970e42d7b8931a58c1891b6da0557aabc04e57753785bdcd671a9",
    "expectedCatalogDefinitionSha256Utf8Lf": "7d0a5c716a0970e42d7b8931a58c1891b6da0557aabc04e57753785bdcd671a9",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13802
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "WeeklyReviewDigestDuties",
    "Name": "TR_WeeklyReviewDigestDuties_Source",
    "observedDefinitionSha256Utf8Lf": "836f6622197d13224c74a3ff4700716bed899ea507c27ee495fdd28d2d548e81",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13805,
    "migrationSqlSha256Utf8Lf": "154546ceaf424ace756a1b2d3bc7478ffb11188a42aedf3c2d79bb49ab2701e7",
    "expectedCatalogDefinitionSha256Utf8Lf": "154546ceaf424ace756a1b2d3bc7478ffb11188a42aedf3c2d79bb49ab2701e7",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13805
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "WeeklyReviewDigests",
    "Name": "TR_WeeklyReviewDigests_Immutable",
    "observedDefinitionSha256Utf8Lf": "0b28cf12eaee65b39fa1b1682017f463c0b0d03a2758eb92ce50dfd0a36eb7e2",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13815,
    "migrationSqlSha256Utf8Lf": "03afadfd889e26636eadac60419f57e233d2f4c07ed9b6442e83e3f98440b374",
    "expectedCatalogDefinitionSha256Utf8Lf": "03afadfd889e26636eadac60419f57e233d2f4c07ed9b6442e83e3f98440b374",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13815
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "WeeklyReviewDigests",
    "Name": "TR_WeeklyReviewDigests_Source",
    "observedDefinitionSha256Utf8Lf": "8a7724c666244bd679ccec9324698b38bd3e4dfb94b5a98bec315bcc40210a4b",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13818,
    "migrationSqlSha256Utf8Lf": "157d5c304bbb720a96ba778519415975c927981f5ab8026b0151182813f541d3",
    "expectedCatalogDefinitionSha256Utf8Lf": "157d5c304bbb720a96ba778519415975c927981f5ab8026b0151182813f541d3",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13818
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  },
  {
    "Schema": "dbo",
    "Table": "WeeklyReviewRecoveryPeriods",
    "Name": "TR_WeeklyReviewRecoveryPeriods_Immutable",
    "observedDefinitionSha256Utf8Lf": "89c4e9ed80c2894ee7aa09661e99cfbd9840e85bd948c6e604542c8f310efc48",
    "createdMigration": "20261008171739_BaselineCurrentSchema",
    "lastDefinitionMigration": "20261008171739_BaselineCurrentSchema",
    "sourcePath": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
    "sourceLine": 13827,
    "migrationSqlSha256Utf8Lf": "eabef3d42e6d02117766dfc2036be234c43cbebbad031ec93a4b23815209e3f8",
    "expectedCatalogDefinitionSha256Utf8Lf": "eabef3d42e6d02117766dfc2036be234c43cbebbad031ec93a4b23815209e3f8",
    "textComparison": "OUTER_WHITESPACE_ONLY",
    "operationHistory": [
      {
        "MigrationId": "20261008171739_BaselineCurrentSchema",
        "Action": "CREATE",
        "Path": "RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs",
        "Line": 13827
      }
    ],
    "runtimeBehavior": "NOT_TESTED"
  }
]
```

## Fresh EF relational mapping (deduplicated physical definitions)

219 keys,692 indexes,229 checks,657 FKs. Current migration adds1SQL-only filtered index and5SQL-only FKs: migration-derived totals219keys/693indexes/229checks/662FK.189trigger definitions are in migration/historical source inventory; no current SQL execution. Owned/table-sharing CLR relationships/default constraint names above are logical diagnostics, not additional SQL constraints.

| Table | Relational kind | Name | Exact definition |
|---|---|---|---|
| dbo.AccountStatusChangeLogs | KEY | PK_AccountStatusChangeLogs | {"name": "PK_AccountStatusChangeLogs", "columns": ["Id"]} |
| dbo.AccountStatusChangeLogs | INDEX | IX_AccountStatusChangeLogs_ChangedByUserId | {"name": "IX_AccountStatusChangeLogs_ChangedByUserId", "columns": ["ChangedByUserId"], "unique": false, "filter": null} |
| dbo.AccountStatusChangeLogs | INDEX | IX_AccountStatusChangeLogs_OccurredAt | {"name": "IX_AccountStatusChangeLogs_OccurredAt", "columns": ["OccurredAt"], "unique": false, "filter": null} |
| dbo.AccountStatusChangeLogs | INDEX | IX_AccountStatusChangeLogs_TargetUserId | {"name": "IX_AccountStatusChangeLogs_TargetUserId", "columns": ["TargetUserId"], "unique": false, "filter": null} |
| dbo.AccountStatusChangeLogs | CHECK | CK_AccountStatusChangeLogs_FromStatus | {"name": "CK_AccountStatusChangeLogs_FromStatus", "sql": "[FromStatus] IN (1, 2, 3)"} |
| dbo.AccountStatusChangeLogs | CHECK | CK_AccountStatusChangeLogs_FromToStatus_Diff | {"name": "CK_AccountStatusChangeLogs_FromToStatus_Diff", "sql": "[FromStatus] <> [ToStatus]"} |
| dbo.AccountStatusChangeLogs | CHECK | CK_AccountStatusChangeLogs_Reason_SafeCode | {"name": "CK_AccountStatusChangeLogs_Reason_SafeCode", "sql": "[Reason] IN ('ADMINISTRATOR_INITIATED', 'SELF_SERVICE_ACCOUNT_RECOVERY', 'REGISTRATION_APPROVED', 'SAFETY_POLICY_VIOLATION', 'NO_STATUS_CHANGE', 'ADMINISTRATIVE_LOCK', 'SECURITY_INCIDENT', 'ACCOUNT_REACTIVATED')"} |
| dbo.AccountStatusChangeLogs | CHECK | CK_AccountStatusChangeLogs_Source_SafeCode | {"name": "CK_AccountStatusChangeLogs_Source_SafeCode", "sql": "[Source] IN ('ADMIN_API', 'SELF_SERVICE', 'IDENTITY_SERVICE', 'COMPLIANCE_REVIEW', 'SYSTEM')"} |
| dbo.AccountStatusChangeLogs | CHECK | CK_AccountStatusChangeLogs_ToStatus | {"name": "CK_AccountStatusChangeLogs_ToStatus", "sql": "[ToStatus] IN (1, 2, 3)"} |
| dbo.AIDetections | KEY | PK_AIDetections | {"name": "PK_AIDetections", "columns": ["Id"]} |
| dbo.AIDetections | INDEX | IX_AIDetections_DefectTypeCode | {"name": "IX_AIDetections_DefectTypeCode", "columns": ["DefectTypeCode"], "unique": false, "filter": null} |
| dbo.AIDetections | INDEX | IX_AIDetections_ModelVersionId | {"name": "IX_AIDetections_ModelVersionId", "columns": ["ModelVersionId"], "unique": false, "filter": null} |
| dbo.AIDetections | INDEX | IX_AIDetections_ProcessingJobId | {"name": "IX_AIDetections_ProcessingJobId", "columns": ["ProcessingJobId"], "unique": false, "filter": null} |
| dbo.AIDetections | INDEX | IX_AIDetections_RoadSectionVersionId | {"name": "IX_AIDetections_RoadSectionVersionId", "columns": ["RoadSectionVersionId"], "unique": false, "filter": null} |
| dbo.AIDetections | CHECK | CK_AIDetections_Confidence | {"name": "CK_AIDetections_Confidence", "sql": "[Confidence] >= 0 AND [Confidence] <= 1"} |
| dbo.AIDetections | CHECK | CK_AIDetections_EstimatedDimensions | {"name": "CK_AIDetections_EstimatedDimensions", "sql": "([EstimatedWidth] IS NULL OR [EstimatedWidth] >= 0) AND ([EstimatedLength] IS NULL OR [EstimatedLength] >= 0)"} |
| dbo.AIDetections | CHECK | CK_AIDetections_RawPayload_Json | {"name": "CK_AIDetections_RawPayload_Json", "sql": "ISJSON([RawPayload]) = 1"} |
| dbo.AIModelVersions | KEY | PK_AIModelVersions | {"name": "PK_AIModelVersions", "columns": ["Id"]} |
| dbo.AIModelVersions | INDEX | IX_AIModelVersions_ModelName | {"name": "IX_AIModelVersions_ModelName", "columns": ["ModelName"], "unique": false, "filter": null} |
| dbo.AIModelVersions | INDEX | IX_AIModelVersions_ReleasedByUserId | {"name": "IX_AIModelVersions_ReleasedByUserId", "columns": ["ReleasedByUserId"], "unique": false, "filter": null} |
| dbo.AIModelVersions | INDEX | UX_AIModelVersions_VersionLabel | {"name": "UX_AIModelVersions_VersionLabel", "columns": ["VersionLabel"], "unique": true, "filter": null} |
| dbo.AIModelVersions | CHECK | CK_AIModelVersions_Metrics_Json | {"name": "CK_AIModelVersions_Metrics_Json", "sql": "[Metrics] IS NULL OR ISJSON([Metrics]) = 1"} |
| dbo.AIModelVersions | CHECK | CK_AIModelVersions_OperatingThresholds_Json | {"name": "CK_AIModelVersions_OperatingThresholds_Json", "sql": "[OperatingThresholds] IS NULL OR ISJSON([OperatingThresholds]) = 1"} |
| dbo.AIModelVersions | CHECK | CK_AIModelVersions_ReleaseMetadata | {"name": "CK_AIModelVersions_ReleaseMetadata", "sql": "([ReleasedAt] IS NULL AND [ReleasedByUserId] IS NULL) OR ([ReleasedAt] IS NOT NULL AND [ReleasedByUserId] IS NOT NULL)"} |
| dbo.AIModelVersions | CHECK | CK_AIModelVersions_Status | {"name": "CK_AIModelVersions_Status", "sql": "[Status] IN (1, 2, 3)"} |
| dbo.Anh02AiDetectionProvenance | KEY | PK_Anh02AiDetectionProvenance | {"name": "PK_Anh02AiDetectionProvenance", "columns": ["DetectionId"]} |
| dbo.Anh02AiDetectionProvenance | INDEX | IX_Anh02AiDetectionProvenance_FrameFileId | {"name": "IX_Anh02AiDetectionProvenance_FrameFileId", "columns": ["FrameFileId"], "unique": false, "filter": null} |
| dbo.Anh02AiDetectionProvenance | INDEX | IX_Anh02AiDetectionProvenance_ResultId_RunId | {"name": "IX_Anh02AiDetectionProvenance_ResultId_RunId", "columns": ["ResultId", "RunId"], "unique": false, "filter": null} |
| dbo.Anh02AiDetectionProvenance | INDEX | IX_Anh02AiDetectionProvenance_RunId_SourceVideoFileId_SourceVideoFileVersion | {"name": "IX_Anh02AiDetectionProvenance_RunId_SourceVideoFileId_SourceVideoFileVersion", "columns": ["RunId", "SourceVideoFileId", "SourceVideoFileVersion"], "unique": false, "filter": null} |
| dbo.Anh02AiDetectionProvenance | INDEX | IX_Anh02AiDetectionProvenance_SegmentId | {"name": "IX_Anh02AiDetectionProvenance_SegmentId", "columns": ["SegmentId"], "unique": false, "filter": null} |
| dbo.Anh02AiDetectionProvenance | INDEX | IX_Anh02AiDetectionProvenance_SourceVideoFileId | {"name": "IX_Anh02AiDetectionProvenance_SourceVideoFileId", "columns": ["SourceVideoFileId"], "unique": false, "filter": null} |
| dbo.Anh02AiDetectionProvenance | CHECK | CK_Anh02AiDetection_Box | {"name": "CK_Anh02AiDetection_Box", "sql": "[BoxX]>=0 AND [BoxY]>=0 AND [BoxWidth]>0 AND [BoxHeight]>0 AND [BoxX]+[BoxWidth]<=1 AND [BoxY]+[BoxHeight]<=1"} |
| dbo.Anh02AiDetectionProvenance | CHECK | CK_Anh02AiDetection_Time | {"name": "CK_Anh02AiDetection_Time", "sql": "[TimestampMilliseconds]>=0 AND [SourceDurationMilliseconds]>0 AND [TimestampMilliseconds]<[SourceDurationMilliseconds]"} |
| dbo.Anh02AiManifestFiles | KEY | AK_Anh02AiManifestFiles_RunId_FileId_FileVersion | {"name": "AK_Anh02AiManifestFiles_RunId_FileId_FileVersion", "columns": ["RunId", "FileId", "FileVersion"]} |
| dbo.Anh02AiManifestFiles | KEY | PK_Anh02AiManifestFiles | {"name": "PK_Anh02AiManifestFiles", "columns": ["RunId", "FileId"]} |
| dbo.Anh02AiManifestFiles | INDEX | IX_Anh02AiManifestFiles_FileId | {"name": "IX_Anh02AiManifestFiles_FileId", "columns": ["FileId"], "unique": false, "filter": null} |
| dbo.Anh02AiMockRuns | KEY | PK_Anh02AiMockRuns | {"name": "PK_Anh02AiMockRuns", "columns": ["Id"]} |
| dbo.Anh02AiMockRuns | INDEX | IX_Anh02AiMockRuns_AnalysisRunId | {"name": "IX_Anh02AiMockRuns_AnalysisRunId", "columns": ["AnalysisRunId"], "unique": false, "filter": null} |
| dbo.Anh02AiMockRuns | INDEX | IX_Anh02AiMockRuns_AttemptId | {"name": "IX_Anh02AiMockRuns_AttemptId", "columns": ["AttemptId"], "unique": false, "filter": null} |
| dbo.Anh02AiMockRuns | INDEX | IX_Anh02AiMockRuns_CreatedBy | {"name": "IX_Anh02AiMockRuns_CreatedBy", "columns": ["CreatedBy"], "unique": false, "filter": null} |
| dbo.Anh02AiMockRuns | INDEX | IX_Anh02AiMockRuns_DatasetVersionId | {"name": "IX_Anh02AiMockRuns_DatasetVersionId", "columns": ["DatasetVersionId"], "unique": false, "filter": null} |
| dbo.Anh02AiMockRuns | INDEX | IX_Anh02AiMockRuns_ModelVersionId | {"name": "IX_Anh02AiMockRuns_ModelVersionId", "columns": ["ModelVersionId"], "unique": false, "filter": null} |
| dbo.Anh02AiMockRuns | INDEX | IX_Anh02AiMockRuns_ProcessingJobId | {"name": "IX_Anh02AiMockRuns_ProcessingJobId", "columns": ["ProcessingJobId"], "unique": true, "filter": "[Stage]='VIDEO_ANALYSIS'"} |
| dbo.Anh02AiMockRuns | INDEX | IX_Anh02AiMockRuns_ProjectId | {"name": "IX_Anh02AiMockRuns_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} |
| dbo.Anh02AiMockRuns | INDEX | IX_Anh02AiMockRuns_ResultId_Id | {"name": "IX_Anh02AiMockRuns_ResultId_Id", "columns": ["ResultId", "Id"], "unique": false, "filter": null} |
| dbo.Anh02AiMockRuns | INDEX | IX_Anh02AiMockRuns_SegmentSetId_RouteVersionId | {"name": "IX_Anh02AiMockRuns_SegmentSetId_RouteVersionId", "columns": ["SegmentSetId", "RouteVersionId"], "unique": false, "filter": null} |
| dbo.Anh02AiMockRuns | INDEX | IX_Anh02AiMockRuns_Status_LeaseUntil | {"name": "IX_Anh02AiMockRuns_Status_LeaseUntil", "columns": ["Status", "LeaseUntil"], "unique": false, "filter": null} |
| dbo.Anh02AiMockRuns | CHECK | CK_Anh02AiMockRuns_Completion | {"name": "CK_Anh02AiMockRuns_Completion", "sql": "([Status]='SUCCEEDED' AND [ResultId] IS NOT NULL AND [CompletedAt] IS NOT NULL) OR ([Status]<>'SUCCEEDED' AND [ResultId] IS NULL)"} |
| dbo.Anh02AiMockRuns | CHECK | CK_Anh02AiMockRuns_Manifest | {"name": "CK_Anh02AiMockRuns_Manifest", "sql": "ISJSON([CanonicalManifest])=1"} |
| dbo.Anh02AiMockRuns | CHECK | CK_Anh02AiMockRuns_Stage | {"name": "CK_Anh02AiMockRuns_Stage", "sql": "[Stage] IN ('VIDEO_ANALYSIS','DUPLICATE_MATCHING')"} |
| dbo.Anh02AiMockRuns | CHECK | CK_Anh02AiMockRuns_Status | {"name": "CK_Anh02AiMockRuns_Status", "sql": "[Status] IN ('QUEUED','RUNNING','SUCCEEDED','FAILED')"} |
| dbo.Anh02AiResultProvenance | KEY | AK_Anh02AiResultProvenance_Id_RunId | {"name": "AK_Anh02AiResultProvenance_Id_RunId", "columns": ["Id", "RunId"]} |
| dbo.Anh02AiResultProvenance | KEY | PK_Anh02AiResultProvenance | {"name": "PK_Anh02AiResultProvenance", "columns": ["Id"]} |
| dbo.Anh02AiResultProvenance | INDEX | IX_Anh02AiResultProvenance_AttemptId | {"name": "IX_Anh02AiResultProvenance_AttemptId", "columns": ["AttemptId"], "unique": false, "filter": null} |
| dbo.Anh02AiResultProvenance | INDEX | IX_Anh02AiResultProvenance_ProcessingJobId | {"name": "IX_Anh02AiResultProvenance_ProcessingJobId", "columns": ["ProcessingJobId"], "unique": false, "filter": null} |
| dbo.Anh02AiResultProvenance | INDEX | IX_Anh02AiResultProvenance_RunId | {"name": "IX_Anh02AiResultProvenance_RunId", "columns": ["RunId"], "unique": true, "filter": null} |
| dbo.Anh02AiResultProvenance | CHECK | CK_Anh02AiResult_Json | {"name": "CK_Anh02AiResult_Json", "sql": "ISJSON([CanonicalResult])=1"} |
| dbo.Anh02ExportJobs | KEY | PK_Anh02ExportJobs | {"name": "PK_Anh02ExportJobs", "columns": ["Id"]} |
| dbo.Anh02ExportJobs | INDEX | IX_Anh02ExportJobs_ArtifactId | {"name": "IX_Anh02ExportJobs_ArtifactId", "columns": ["ArtifactId"], "unique": false, "filter": null} |
| dbo.Anh02ExportJobs | INDEX | IX_Anh02ExportJobs_ProjectId | {"name": "IX_Anh02ExportJobs_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} |
| dbo.Anh02ExportJobs | INDEX | IX_Anh02ExportJobs_RequestedBy | {"name": "IX_Anh02ExportJobs_RequestedBy", "columns": ["RequestedBy"], "unique": false, "filter": null} |
| dbo.Anh02ExportJobs | INDEX | IX_Anh02ExportJobs_SnapshotId | {"name": "IX_Anh02ExportJobs_SnapshotId", "columns": ["SnapshotId"], "unique": true, "filter": null} |
| dbo.Anh02ExportJobs | INDEX | IX_Anh02ExportJobs_Status_NextAttemptAt_LeaseUntil | {"name": "IX_Anh02ExportJobs_Status_NextAttemptAt_LeaseUntil", "columns": ["Status", "NextAttemptAt", "LeaseUntil"], "unique": false, "filter": null} |
| dbo.Anh02ExportJobs | CHECK | CK_Anh02ExportJob_Expiry | {"name": "CK_Anh02ExportJob_Expiry", "sql": "([Status] <> 'SUCCEEDED' AND [ExpiresAt] IS NULL) OR ([Status]='SUCCEEDED' AND [ArtifactId] IS NOT NULL AND [CompletedAt] IS NOT NULL AND [ExpiresAt]=DATEADD(day,30,[CompletedAt]))"} |
| dbo.Anh02ExportJobs | CHECK | CK_Anh02ExportJob_Format | {"name": "CK_Anh02ExportJob_Format", "sql": "[Format] IN ('PDF','ZIP') AND ([Kind] <> 'TRAINING' OR [Format]='ZIP')"} |
| dbo.Anh02ExportJobs | CHECK | CK_Anh02ExportJob_Kind | {"name": "CK_Anh02ExportJob_Kind", "sql": "[Kind] IN ('DOSSIER','TRAINING')"} |
| dbo.Anh02ExportJobs | CHECK | CK_Anh02ExportJob_State | {"name": "CK_Anh02ExportJob_State", "sql": "[Status] IN ('QUEUED','RUNNING','SUCCEEDED','FAILED')"} |
| dbo.Anh02ExportSnapshotFiles | KEY | PK_Anh02ExportSnapshotFiles | {"name": "PK_Anh02ExportSnapshotFiles", "columns": ["Id"]} |
| dbo.Anh02ExportSnapshotFiles | INDEX | IX_Anh02ExportSnapshotFiles_FileId | {"name": "IX_Anh02ExportSnapshotFiles_FileId", "columns": ["FileId"], "unique": false, "filter": null} |
| dbo.Anh02ExportSnapshotFiles | INDEX | IX_Anh02ExportSnapshotFiles_SnapshotId_FileId | {"name": "IX_Anh02ExportSnapshotFiles_SnapshotId_FileId", "columns": ["SnapshotId", "FileId"], "unique": true, "filter": null} |
| dbo.Anh02ExportSnapshotFiles | CHECK | CK_Anh02ExportSnapshotFile_Size | {"name": "CK_Anh02ExportSnapshotFile_Size", "sql": "[SizeBytes]>0"} |
| dbo.Anh02ExportSnapshots | KEY | PK_Anh02ExportSnapshots | {"name": "PK_Anh02ExportSnapshots", "columns": ["Id"]} |
| dbo.Anh02ExportSnapshots | INDEX | IX_Anh02ExportSnapshots_ProjectId | {"name": "IX_Anh02ExportSnapshots_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} |
| dbo.Anh02GeneratedArtifacts | KEY | PK_Anh02GeneratedArtifacts | {"name": "PK_Anh02GeneratedArtifacts", "columns": ["Id"]} |
| dbo.Anh02GeneratedArtifacts | INDEX | IX_Anh02GeneratedArtifacts_ExportJobId | {"name": "IX_Anh02GeneratedArtifacts_ExportJobId", "columns": ["ExportJobId"], "unique": true, "filter": null} |
| dbo.Anh02GeneratedArtifacts | INDEX | IX_Anh02GeneratedArtifacts_FileId | {"name": "IX_Anh02GeneratedArtifacts_FileId", "columns": ["FileId"], "unique": true, "filter": null} |
| dbo.Anh02GeneratedArtifacts | INDEX | IX_Anh02GeneratedArtifacts_SnapshotId | {"name": "IX_Anh02GeneratedArtifacts_SnapshotId", "columns": ["SnapshotId"], "unique": false, "filter": null} |
| dbo.AuditLogs | KEY | PK_AuditLogs | {"name": "PK_AuditLogs", "columns": ["Id"]} |
| dbo.AuditLogs | INDEX | IX_AuditLogs_ActorUserId | {"name": "IX_AuditLogs_ActorUserId", "columns": ["ActorUserId"], "unique": false, "filter": null} |
| dbo.AuditLogs | INDEX | IX_AuditLogs_CorrelationId | {"name": "IX_AuditLogs_CorrelationId", "columns": ["CorrelationId"], "unique": false, "filter": null} |
| dbo.AuditLogs | INDEX | IX_AuditLogs_Entity | {"name": "IX_AuditLogs_Entity", "columns": ["EntityType", "EntityId"], "unique": false, "filter": null} |
| dbo.AuditLogs | INDEX | IX_AuditLogs_OccurredAtUtc | {"name": "IX_AuditLogs_OccurredAtUtc", "columns": ["OccurredAtUtc"], "unique": false, "filter": null} |
| dbo.AuditLogs | CHECK | CK_AuditLogs_AfterSnapshot_Json | {"name": "CK_AuditLogs_AfterSnapshot_Json", "sql": "[AfterSnapshot] IS NULL OR ISJSON([AfterSnapshot]) = 1"} |
| dbo.AuditLogs | CHECK | CK_AuditLogs_BeforeSnapshot_Json | {"name": "CK_AuditLogs_BeforeSnapshot_Json", "sql": "[BeforeSnapshot] IS NULL OR ISJSON([BeforeSnapshot]) = 1"} |
| dbo.BaselineCurrentPointers | KEY | PK_BaselineCurrentPointers | {"name": "PK_BaselineCurrentPointers", "columns": ["Id"]} |
| dbo.BaselineCurrentPointers | INDEX | IX_BaselineCurrentPointers_ProjectId_RouteVersionId_SegmentSetId_SegmentId_TargetBand | {"name": "IX_BaselineCurrentPointers_ProjectId_RouteVersionId_SegmentSetId_SegmentId_TargetBand", "columns": ["ProjectId", "RouteVersionId", "SegmentSetId", "SegmentId", "TargetBand"], "unique": true, "filter": null} |
| dbo.BaselineCurrentPointers | INDEX | IX_BaselineCurrentPointers_SelectionId | {"name": "IX_BaselineCurrentPointers_SelectionId", "columns": ["SelectionId"], "unique": false, "filter": null} |
| dbo.BaselineSelectionItems | KEY | PK_BaselineSelectionItems | {"name": "PK_BaselineSelectionItems", "columns": ["Id"]} |
| dbo.BaselineSelectionItems | INDEX | IX_BaselineSelectionItems_AssessmentId | {"name": "IX_BaselineSelectionItems_AssessmentId", "columns": ["AssessmentId"], "unique": false, "filter": null} |
| dbo.BaselineSelectionItems | INDEX | IX_BaselineSelectionItems_BaselineSelectionId_RouteVersionId_SegmentSetId_SegmentId_TargetBand | {"name": "IX_BaselineSelectionItems_BaselineSelectionId_RouteVersionId_SegmentSetId_SegmentId_TargetBand", "columns": ["BaselineSelectionId", "RouteVersionId", "SegmentSetId", "SegmentId", "TargetBand"], "unique": true, "filter": null} |
| dbo.BaselineSelectionItems | INDEX | IX_BaselineSelectionItems_DatasetId | {"name": "IX_BaselineSelectionItems_DatasetId", "columns": ["DatasetId"], "unique": false, "filter": null} |
| dbo.BaselineSelections | KEY | PK_BaselineSelections | {"name": "PK_BaselineSelections", "columns": ["Id"]} |
| dbo.BaselineSelections | INDEX | IX_BaselineSelections_ProjectId | {"name": "IX_BaselineSelections_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} |
| dbo.BaselineSelections | INDEX | IX_BaselineSelections_SelectedBy | {"name": "IX_BaselineSelections_SelectedBy", "columns": ["SelectedBy"], "unique": false, "filter": null} |
| dbo.BusinessDutyAppointments | KEY | PK_BusinessDutyAppointments | {"name": "PK_BusinessDutyAppointments", "columns": ["Id"]} |
| dbo.BusinessDutyAppointments | INDEX | IX_BusinessDutyAppointments_CurrentActorId | {"name": "IX_BusinessDutyAppointments_CurrentActorId", "columns": ["CurrentActorId"], "unique": false, "filter": null} |
| dbo.BusinessDutyAppointments | INDEX | IX_BusinessDutyAppointments_DecisionActorId | {"name": "IX_BusinessDutyAppointments_DecisionActorId", "columns": ["DecisionActorId"], "unique": false, "filter": null} |
| dbo.BusinessDutyAppointments | INDEX | IX_BusinessDutyAppointments_RequestId | {"name": "IX_BusinessDutyAppointments_RequestId", "columns": ["RequestId"], "unique": false, "filter": null} |
| dbo.BusinessReceivingRequests | KEY | PK_BusinessReceivingRequests | {"name": "PK_BusinessReceivingRequests", "columns": ["Id"]} |
| dbo.BusinessReceivingRequests | INDEX | IX_BusinessReceivingRequests_ClockId | {"name": "IX_BusinessReceivingRequests_ClockId", "columns": ["ClockId"], "unique": false, "filter": null} |
| dbo.BusinessReceivingRequests | INDEX | IX_BusinessReceivingRequests_ProjectId_ScopeId_CompletedAt | {"name": "IX_BusinessReceivingRequests_ProjectId_ScopeId_CompletedAt", "columns": ["ProjectId", "ScopeId", "CompletedAt"], "unique": false, "filter": null} |
| dbo.BusinessReceivingRequests | INDEX | IX_BusinessReceivingRequests_ResponsibleActorId | {"name": "IX_BusinessReceivingRequests_ResponsibleActorId", "columns": ["ResponsibleActorId"], "unique": false, "filter": null} |
| dbo.BusinessReceivingRequests | INDEX | IX_BusinessReceivingRequests_SourceKind_SourceId_Kind | {"name": "IX_BusinessReceivingRequests_SourceKind_SourceId_Kind", "columns": ["SourceKind", "SourceId", "Kind"], "unique": true, "filter": null} |
| dbo.BusinessReceivingRequests | CHECK | CK_BusinessReceivingRequests_Ack | {"name": "CK_BusinessReceivingRequests_Ack", "sql": "([AcknowledgedAt] IS NULL AND [AcknowledgmentId] IS NULL AND [AcknowledgedBy] IS NULL AND [ClockId] IS NULL AND [ClaimedDeviceAt] IS NULL) OR ([AcknowledgedAt] IS NOT NULL AND [AcknowledgmentId] IS NOT NULL AND [AcknowledgedBy] IS NOT NULL AND [ClockId] IS NOT NULL AND [AcknowledgedAt]>=[RequestedAt])"} |
| dbo.CandidateSourceHeads | KEY | PK_CandidateSourceHeads | {"name": "PK_CandidateSourceHeads", "columns": ["SourceKind", "SourceId"]} |
| dbo.CandidateSourceHeads | INDEX | IX_CandidateSourceHeads_DecisionId_SourceKind_SourceId_ProjectId | {"name": "IX_CandidateSourceHeads_DecisionId_SourceKind_SourceId_ProjectId", "columns": ["DecisionId", "SourceKind", "SourceId", "ProjectId"], "unique": false, "filter": null} |
| dbo.CandidateSourceHeads | INDEX | IX_CandidateSourceHeads_ProjectId | {"name": "IX_CandidateSourceHeads_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} |
| dbo.CaseConclusionDefects | KEY | PK_CaseConclusionDefects | {"name": "PK_CaseConclusionDefects", "columns": ["ConclusionId", "DefectId"]} |
| dbo.CaseConclusionDefects | INDEX | IX_CaseConclusionDefects_DefectId | {"name": "IX_CaseConclusionDefects_DefectId", "columns": ["DefectId"], "unique": false, "filter": null} |
| dbo.CaseConclusionEvidence | KEY | PK_CaseConclusionEvidence | {"name": "PK_CaseConclusionEvidence", "columns": ["ConclusionId", "EvidenceId"]} |
| dbo.CaseConclusionEvidence | INDEX | IX_CaseConclusionEvidence_SourceReportId | {"name": "IX_CaseConclusionEvidence_SourceReportId", "columns": ["SourceReportId"], "unique": false, "filter": null} |
| dbo.CaseConclusionEvidence | CHECK | CK_CaseConclusionEvidence_TypedEvidence | {"name": "CK_CaseConclusionEvidence_TypedEvidence", "sql": "([OriginalEvidenceId] IS NOT NULL AND [OriginalEvidenceId]=[EvidenceId] AND [SupplementEvidenceId] IS NULL) OR ([SupplementEvidenceId] IS NOT NULL AND [SupplementEvidenceId]=[EvidenceId] AND [OriginalEvidenceId] IS NULL)"} |
| dbo.CaseConclusions | KEY | PK_CaseConclusions | {"name": "PK_CaseConclusions", "columns": ["Id"]} |
| dbo.CaseConclusions | INDEX | IX_CaseConclusions_CaseId | {"name": "IX_CaseConclusions_CaseId", "columns": ["CaseId"], "unique": false, "filter": null} |
| dbo.CasePublicationDefects | KEY | PK_CasePublicationDefects | {"name": "PK_CasePublicationDefects", "columns": ["PublicationId", "DefectId"]} |
| dbo.CasePublicationDefects | INDEX | IX_CasePublicationDefects_DefectId | {"name": "IX_CasePublicationDefects_DefectId", "columns": ["DefectId"], "unique": false, "filter": null} |
| dbo.CasePublicationEvidence | KEY | PK_CasePublicationEvidence | {"name": "PK_CasePublicationEvidence", "columns": ["PublicationId", "RecipientReportId", "EvidenceId"]} |
| dbo.CasePublicationEvidence | INDEX | IX_CasePublicationEvidence_SourceReportId | {"name": "IX_CasePublicationEvidence_SourceReportId", "columns": ["SourceReportId"], "unique": false, "filter": null} |
| dbo.CasePublicationEvidence | CHECK | CK_CasePublicationEvidence_TypedEvidence | {"name": "CK_CasePublicationEvidence_TypedEvidence", "sql": "([OriginalEvidenceId] IS NOT NULL AND [OriginalEvidenceId]=[EvidenceId] AND [SupplementEvidenceId] IS NULL) OR ([SupplementEvidenceId] IS NOT NULL AND [SupplementEvidenceId]=[EvidenceId] AND [OriginalEvidenceId] IS NULL)"} |
| dbo.CasePublicationRecipients | KEY | PK_CasePublicationRecipients | {"name": "PK_CasePublicationRecipients", "columns": ["PublicationId", "ReportId"]} |
| dbo.CasePublicationRecipients | INDEX | IX_CasePublicationRecipients_ReportId | {"name": "IX_CasePublicationRecipients_ReportId", "columns": ["ReportId"], "unique": false, "filter": null} |
| dbo.CasePublications | KEY | PK_CasePublications | {"name": "PK_CasePublications", "columns": ["Id"]} |
| dbo.CasePublications | INDEX | IX_CasePublications_CaseId | {"name": "IX_CasePublications_CaseId", "columns": ["CaseId"], "unique": false, "filter": null} |
| dbo.CaseReportLinkHistory | KEY | PK_CaseReportLinkHistory | {"name": "PK_CaseReportLinkHistory", "columns": ["Id"]} |
| dbo.CaseReportLinkHistory | INDEX | IX_CaseReportLinkHistory_ActorUserId | {"name": "IX_CaseReportLinkHistory_ActorUserId", "columns": ["ActorUserId"], "unique": false, "filter": null} |
| dbo.CaseReportLinkHistory | INDEX | IX_CaseReportLinkHistory_FromCaseId | {"name": "IX_CaseReportLinkHistory_FromCaseId", "columns": ["FromCaseId"], "unique": false, "filter": null} |
| dbo.CaseReportLinkHistory | INDEX | IX_CaseReportLinkHistory_ToCaseId | {"name": "IX_CaseReportLinkHistory_ToCaseId", "columns": ["ToCaseId"], "unique": false, "filter": null} |
| dbo.CaseReportLinkHistoryReports | KEY | PK_CaseReportLinkHistoryReports | {"name": "PK_CaseReportLinkHistoryReports", "columns": ["HistoryId", "ReportId"]} |
| dbo.CaseReportLinkHistoryReports | INDEX | IX_CaseReportLinkHistoryReports_ReportId | {"name": "IX_CaseReportLinkHistoryReports_ReportId", "columns": ["ReportId"], "unique": false, "filter": null} |
| dbo.CaseReportLinks | KEY | PK_CaseReportLinks | {"name": "PK_CaseReportLinks", "columns": ["Id"]} |
| dbo.CaseReportLinks | INDEX | IX_CaseReportLinks_CaseId_ReportId | {"name": "IX_CaseReportLinks_CaseId_ReportId", "columns": ["CaseId", "ReportId"], "unique": false, "filter": null} |
| dbo.CaseReportLinks | INDEX | IX_CaseReportLinks_ReportId | {"name": "IX_CaseReportLinks_ReportId", "columns": ["ReportId"], "unique": true, "filter": "[EndedAt] IS NULL"} |
| dbo.CaseReportLinks | CHECK | CK_CaseReportLinks_Times | {"name": "CK_CaseReportLinks_Times", "sql": "[EndedAt] IS NULL OR [EndedAt] >= [StartedAt]"} |
| dbo.CauseCategories | KEY | PK_CauseCategories | {"name": "PK_CauseCategories", "columns": ["Code"]} |
| dbo.ConsumerEffectReceipts | KEY | PK_ConsumerEffectReceipts | {"name": "PK_ConsumerEffectReceipts", "columns": ["Id"]} |
| dbo.ConsumerEffectReceipts | INDEX | UX_ConsumerEffectReceipts_MessageConsumer | {"name": "UX_ConsumerEffectReceipts_MessageConsumer", "columns": ["MessageId", "ConsumerName"], "unique": true, "filter": null} |
| dbo.CrsProfileRevisions | KEY | AK_CrsProfileRevisions_Id_ProjectId | {"name": "AK_CrsProfileRevisions_Id_ProjectId", "columns": ["Id", "ProjectId"]} |
| dbo.CrsProfileRevisions | KEY | PK_CrsProfileRevisions | {"name": "PK_CrsProfileRevisions", "columns": ["Id"]} |
| dbo.CrsProfileRevisions | INDEX | IX_CrsProfileRevisions_CreatedBy | {"name": "IX_CrsProfileRevisions_CreatedBy", "columns": ["CreatedBy"], "unique": false, "filter": null} |
| dbo.CrsProfileRevisions | INDEX | IX_CrsProfileRevisions_ProjectId_Code_Revision | {"name": "IX_CrsProfileRevisions_ProjectId_Code_Revision", "columns": ["ProjectId", "Code", "Revision"], "unique": true, "filter": null} |
| dbo.CrsProfileRevisions | CHECK | CK_CrsProfileRevision | {"name": "CK_CrsProfileRevision", "sql": "[Revision]>0 AND [SourceSrid]>=0 AND [Status] IN ('CANDIDATE','VERIFIED') AND ISJSON([PayloadJson])=1"} |
| dbo.DatasetAssessmentItems | KEY | PK_DatasetAssessmentItems | {"name": "PK_DatasetAssessmentItems", "columns": ["Id"]} |
| dbo.DatasetAssessmentItems | INDEX | IX_DatasetAssessmentItems_AssessmentId_RouteVersionId_SegmentSetId_SegmentId_TargetBand | {"name": "IX_DatasetAssessmentItems_AssessmentId_RouteVersionId_SegmentSetId_SegmentId_TargetBand", "columns": ["AssessmentId", "RouteVersionId", "SegmentSetId", "SegmentId", "TargetBand"], "unique": true, "filter": null} |
| dbo.DatasetAssessmentItems | INDEX | IX_DatasetAssessmentItems_SegmentId | {"name": "IX_DatasetAssessmentItems_SegmentId", "columns": ["SegmentId"], "unique": false, "filter": null} |
| dbo.DatasetAssessmentItems | CHECK | CK_Assessment_Status | {"name": "CK_Assessment_Status", "sql": "[PositionStatus] IN ('PASS','FAIL','UNKNOWN') AND [QualityStatus] IN ('PASS','FAIL','UNKNOWN') AND [CoverageStatus] IN ('PASS','FAIL','UNKNOWN')"} |
| dbo.DatasetAssessments | KEY | PK_DatasetAssessments | {"name": "PK_DatasetAssessments", "columns": ["Id"]} |
| dbo.DatasetAssessments | INDEX | IX_DatasetAssessments_DatasetId_ReviewedAt | {"name": "IX_DatasetAssessments_DatasetId_ReviewedAt", "columns": ["DatasetId", "ReviewedAt"], "unique": false, "filter": null} |
| dbo.DatasetAssessments | INDEX | IX_DatasetAssessments_ReviewedBy | {"name": "IX_DatasetAssessments_ReviewedBy", "columns": ["ReviewedBy"], "unique": false, "filter": null} |
| dbo.DeadlineBreaches | KEY | PK_DeadlineBreaches | {"name": "PK_DeadlineBreaches", "columns": ["Id"]} |
| dbo.DeadlineBreaches | INDEX | IX_DeadlineBreaches_ClockId_DueAt | {"name": "IX_DeadlineBreaches_ClockId_DueAt", "columns": ["ClockId", "DueAt"], "unique": true, "filter": null} |
| dbo.DeadlineBreaches | CHECK | CK_DeadlineBreaches_Times | {"name": "CK_DeadlineBreaches_Times", "sql": "[ObservedAt]>=[DueAt]"} |
| dbo.DeadlineClocks | KEY | PK_DeadlineClocks | {"name": "PK_DeadlineClocks", "columns": ["Id"]} |
| dbo.DeadlineClocks | INDEX | IX_DeadlineClocks_AcknowledgedByUserId | {"name": "IX_DeadlineClocks_AcknowledgedByUserId", "columns": ["AcknowledgedByUserId"], "unique": false, "filter": null} |
| dbo.DeadlineClocks | INDEX | IX_DeadlineClocks_AppointedActorId | {"name": "IX_DeadlineClocks_AppointedActorId", "columns": ["AppointedActorId"], "unique": false, "filter": null} |
| dbo.DeadlineClocks | INDEX | IX_DeadlineClocks_Kind_TargetId | {"name": "IX_DeadlineClocks_Kind_TargetId", "columns": ["Kind", "TargetId"], "unique": true, "filter": null} |
| dbo.DeadlineClocks | INDEX | IX_DeadlineClocks_OriginEventId | {"name": "IX_DeadlineClocks_OriginEventId", "columns": ["OriginEventId"], "unique": false, "filter": null} |
| dbo.DeadlineClocks | INDEX | IX_DeadlineClocks_ProjectId_CompletedAt_CurrentDueAt | {"name": "IX_DeadlineClocks_ProjectId_CompletedAt_CurrentDueAt", "columns": ["ProjectId", "CompletedAt", "CurrentDueAt"], "unique": false, "filter": null} |
| dbo.DeadlineClocks | CHECK | CK_DeadlineClocks_Acknowledgment | {"name": "CK_DeadlineClocks_Acknowledgment", "sql": "([AcknowledgedAt] IS NULL AND [AcknowledgedByUserId] IS NULL AND [AcknowledgmentEventId] IS NULL) OR ([Kind]=9 AND [AcknowledgedAt] IS NOT NULL AND [AcknowledgedAt]>=[OriginAt] AND [AcknowledgedByUserId] IS NOT NULL AND [AcknowledgmentEventId] IS NOT NULL AND [CompletedAt] IS NOT NULL AND [CompletedAt]=[AcknowledgedAt])"} |
| dbo.DeadlineClocks | CHECK | CK_DeadlineClocks_Kind | {"name": "CK_DeadlineClocks_Kind", "sql": "[Kind] BETWEEN 1 AND 10"} |
| dbo.DeadlineClocks | CHECK | CK_DeadlineClocks_Times | {"name": "CK_DeadlineClocks_Times", "sql": "([OriginalDueAt]>[OriginAt] OR ([Kind]=10 AND [OriginalDueAt]=[OriginAt])) AND [CurrentDueAt]>=[OriginalDueAt] AND ([CompletedAt] IS NULL OR [CompletedAt]>=[OriginAt])"} |
| dbo.DeadlineDutyAppointments | KEY | PK_DeadlineDutyAppointments | {"name": "PK_DeadlineDutyAppointments", "columns": ["Id"]} |
| dbo.DeadlineDutyAppointments | INDEX | IX_DeadlineDutyAppointments_ClockId | {"name": "IX_DeadlineDutyAppointments_ClockId", "columns": ["ClockId"], "unique": false, "filter": null} |
| dbo.DeadlineDutyAppointments | INDEX | IX_DeadlineDutyAppointments_CurrentActorId | {"name": "IX_DeadlineDutyAppointments_CurrentActorId", "columns": ["CurrentActorId"], "unique": false, "filter": null} |
| dbo.DeadlineDutyAppointments | INDEX | IX_DeadlineDutyAppointments_DecisionActorId | {"name": "IX_DeadlineDutyAppointments_DecisionActorId", "columns": ["DecisionActorId"], "unique": false, "filter": null} |
| dbo.DeadlineExtensions | KEY | PK_DeadlineExtensions | {"name": "PK_DeadlineExtensions", "columns": ["Id"]} |
| dbo.DeadlineExtensions | INDEX | IX_DeadlineExtensions_ActorUserId | {"name": "IX_DeadlineExtensions_ActorUserId", "columns": ["ActorUserId"], "unique": false, "filter": null} |
| dbo.DeadlineExtensions | INDEX | IX_DeadlineExtensions_ClockId_NewDueAt | {"name": "IX_DeadlineExtensions_ClockId_NewDueAt", "columns": ["ClockId", "NewDueAt"], "unique": true, "filter": null} |
| dbo.DeadlineExtensions | CHECK | CK_DeadlineExtensions_Times | {"name": "CK_DeadlineExtensions_Times", "sql": "[NewDueAt]>[PreviousDueAt] AND [NewDueAt]>[OccurredAt]"} |
| dbo.Defects | KEY | PK_Defects | {"name": "PK_Defects", "columns": ["Id"]} |
| dbo.Defects | INDEX | IX_Defects_CauseCategoryCode | {"name": "IX_Defects_CauseCategoryCode", "columns": ["CauseCategoryCode"], "unique": false, "filter": null} |
| dbo.Defects | INDEX | IX_Defects_DefectTypeCode | {"name": "IX_Defects_DefectTypeCode", "columns": ["DefectTypeCode"], "unique": false, "filter": null} |
| dbo.Defects | INDEX | IX_Defects_ProjectId | {"name": "IX_Defects_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} |
| dbo.Defects | INDEX | IX_Defects_RoadSectionVersionId | {"name": "IX_Defects_RoadSectionVersionId", "columns": ["RoadSectionVersionId"], "unique": false, "filter": null} |
| dbo.Defects | INDEX | UX_Defects_SourceAIDetectionId | {"name": "UX_Defects_SourceAIDetectionId", "columns": ["SourceAIDetectionId"], "unique": true, "filter": "[SourceAIDetectionId] IS NOT NULL"} |
| dbo.Defects | CHECK | CK_Defects_Severity | {"name": "CK_Defects_Severity", "sql": "[Severity] IS NULL OR [Severity] IN (0, 1, 2, 3, 4)"} |
| dbo.Defects | CHECK | CK_Defects_Status | {"name": "CK_Defects_Status", "sql": "[Status] IS NULL OR [Status] IN (0, 1, 2, 3, 4, 5)"} |
| dbo.DefectSourceLinks | KEY | PK_DefectSourceLinks | {"name": "PK_DefectSourceLinks", "columns": ["Id"]} |
| dbo.DefectSourceLinks | INDEX | IX_DefectSourceLinks_AIDetectionSourceId | {"name": "IX_DefectSourceLinks_AIDetectionSourceId", "columns": ["AIDetectionSourceId"], "unique": false, "filter": null} |
| dbo.DefectSourceLinks | INDEX | IX_DefectSourceLinks_DecisionId | {"name": "IX_DefectSourceLinks_DecisionId", "columns": ["DecisionId"], "unique": true, "filter": null} |
| dbo.DefectSourceLinks | INDEX | IX_DefectSourceLinks_DecisionId_SourceKind_SourceId_ProjectId | {"name": "IX_DefectSourceLinks_DecisionId_SourceKind_SourceId_ProjectId", "columns": ["DecisionId", "SourceKind", "SourceId", "ProjectId"], "unique": false, "filter": null} |
| dbo.DefectSourceLinks | INDEX | IX_DefectSourceLinks_DefectId | {"name": "IX_DefectSourceLinks_DefectId", "columns": ["DefectId"], "unique": false, "filter": null} |
| dbo.DefectSourceLinks | INDEX | IX_DefectSourceLinks_ProjectId | {"name": "IX_DefectSourceLinks_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} |
| dbo.DefectSourceLinks | INDEX | IX_DefectSourceLinks_ReportSourceId | {"name": "IX_DefectSourceLinks_ReportSourceId", "columns": ["ReportSourceId"], "unique": false, "filter": null} |
| dbo.DefectSourceLinks | INDEX | IX_DefectSourceLinks_SourceKind_SourceId | {"name": "IX_DefectSourceLinks_SourceKind_SourceId", "columns": ["SourceKind", "SourceId"], "unique": true, "filter": "[EndedAt] IS NULL"} |
| dbo.DefectSourceLinks | CHECK | CK_DefectSourceLinks_TypedSource | {"name": "CK_DefectSourceLinks_TypedSource", "sql": "([SourceKind]=1 AND [ReportSourceId]=[SourceId] AND [AIDetectionSourceId] IS NULL) OR ([SourceKind]=2 AND [AIDetectionSourceId]=[SourceId] AND [ReportSourceId] IS NULL)"} |
| dbo.DefectStatisticsSources | KEY | PK_DefectStatisticsSources | {"name": "PK_DefectStatisticsSources", "columns": ["Id"]} |
| dbo.DefectStatisticsSources | INDEX | IX_DefectStatisticsSources_ActorId | {"name": "IX_DefectStatisticsSources_ActorId", "columns": ["ActorId"], "unique": false, "filter": null} |
| dbo.DefectStatisticsSources | INDEX | IX_DefectStatisticsSources_DefectId | {"name": "IX_DefectStatisticsSources_DefectId", "columns": ["DefectId"], "unique": false, "filter": null} |
| dbo.DefectStatisticsSources | INDEX | IX_DefectStatisticsSources_ObligationId | {"name": "IX_DefectStatisticsSources_ObligationId", "columns": ["ObligationId"], "unique": false, "filter": null} |
| dbo.DefectStatisticsSources | INDEX | IX_DefectStatisticsSources_ProjectId_SharedPartId | {"name": "IX_DefectStatisticsSources_ProjectId_SharedPartId", "columns": ["ProjectId", "SharedPartId"], "unique": false, "filter": null} |
| dbo.DefectStatisticsSources | INDEX | IX_DefectStatisticsSources_RoadSectionId | {"name": "IX_DefectStatisticsSources_RoadSectionId", "columns": ["RoadSectionId"], "unique": false, "filter": null} |
| dbo.DefectStatisticsSources | INDEX | IX_DefectStatisticsSources_RouteVersionId | {"name": "IX_DefectStatisticsSources_RouteVersionId", "columns": ["RouteVersionId"], "unique": false, "filter": null} |
| dbo.DefectStatisticsSources | INDEX | IX_DefectStatisticsSources_SupersedesId | {"name": "IX_DefectStatisticsSources_SupersedesId", "columns": ["SupersedesId"], "unique": true, "filter": "[SupersedesId] IS NOT NULL"} |
| dbo.DefectStatisticsSources | CHECK | CK_DefectStatisticsSources_Bounds | {"name": "CK_DefectStatisticsSources_Bounds", "sql": "[From]>=0 AND [To]>[From] AND [OffsetTo]>[OffsetFrom]"} |
| dbo.DefectStatisticsSources | CHECK | CK_DefectStatisticsSources_Facts | {"name": "CK_DefectStatisticsSources_Facts", "sql": "ISJSON([SegmentIdsJson])=1 AND ISJSON([QuantitiesJson])=1 AND ISJSON([SourceFactsJson])=1 AND [Provenance] COLLATE Latin1_General_100_BIN2 IN ('REAL_SOURCE','TEST_ONLY')"} |
| dbo.DefectTypes | KEY | PK_DefectTypes | {"name": "PK_DefectTypes", "columns": ["Code"]} |
| dbo.DefectVerificationLogs | KEY | PK_DefectVerificationLogs | {"name": "PK_DefectVerificationLogs", "columns": ["Id"]} |
| dbo.DefectVerificationLogs | INDEX | IX_DefectVerificationLogs_AIDetectionId | {"name": "IX_DefectVerificationLogs_AIDetectionId", "columns": ["AIDetectionId"], "unique": false, "filter": null} |
| dbo.DefectVerificationLogs | INDEX | IX_DefectVerificationLogs_DefectId | {"name": "IX_DefectVerificationLogs_DefectId", "columns": ["DefectId"], "unique": false, "filter": null} |
| dbo.DefectVerificationLogs | INDEX | IX_DefectVerificationLogs_FieldInspectionTaskId | {"name": "IX_DefectVerificationLogs_FieldInspectionTaskId", "columns": ["FieldInspectionTaskId"], "unique": false, "filter": null} |
| dbo.DefectVerificationLogs | INDEX | IX_DefectVerificationLogs_SeverityRuleVersionId | {"name": "IX_DefectVerificationLogs_SeverityRuleVersionId", "columns": ["SeverityRuleVersionId"], "unique": false, "filter": null} |
| dbo.DefectVerificationLogs | INDEX | IX_DefectVerificationLogs_VerifiedByUserId | {"name": "IX_DefectVerificationLogs_VerifiedByUserId", "columns": ["VerifiedByUserId"], "unique": false, "filter": null} |
| dbo.DefectVerificationLogs | CHECK | CK_DefectVerificationLogs_Action | {"name": "CK_DefectVerificationLogs_Action", "sql": "[Action] IN (1, 2, 3, 4, 5)"} |
| dbo.DefectVerificationLogs | CHECK | CK_DefectVerificationLogs_AfterSnapshot_Json | {"name": "CK_DefectVerificationLogs_AfterSnapshot_Json", "sql": "[AfterSnapshot] IS NULL OR ISJSON([AfterSnapshot]) = 1"} |
| dbo.DefectVerificationLogs | CHECK | CK_DefectVerificationLogs_BeforeSnapshot_Json | {"name": "CK_DefectVerificationLogs_BeforeSnapshot_Json", "sql": "[BeforeSnapshot] IS NULL OR ISJSON([BeforeSnapshot]) = 1"} |
| dbo.DefectVerificationLogs | CHECK | CK_DefectVerificationLogs_ExactlyOneTarget | {"name": "CK_DefectVerificationLogs_ExactlyOneTarget", "sql": "([DefectId] IS NOT NULL AND [AIDetectionId] IS NULL) OR ([DefectId] IS NULL AND [AIDetectionId] IS NOT NULL)"} |
| dbo.DerivedMeasurements | KEY | PK_DerivedMeasurements | {"name": "PK_DerivedMeasurements", "columns": ["Id"]} |
| dbo.DerivedMeasurements | INDEX | IX_DerivedMeasurements_DataSampleTypeStatus | {"name": "IX_DerivedMeasurements_DataSampleTypeStatus", "columns": ["SurveyDataVersionId", "SampleId", "MeasurementType", "Status"], "unique": false, "filter": null} |
| dbo.DerivedMeasurements | INDEX | IX_DerivedMeasurements_RoadSectionVersionId | {"name": "IX_DerivedMeasurements_RoadSectionVersionId", "columns": ["RoadSectionVersionId"], "unique": false, "filter": null} |
| dbo.DerivedMeasurements | CHECK | CK_DerivedMeasurements_MeasurementType | {"name": "CK_DerivedMeasurements_MeasurementType", "sql": "[MeasurementType] IN (1, 2, 3)"} |
| dbo.DerivedMeasurements | CHECK | CK_DerivedMeasurements_SourceType | {"name": "CK_DerivedMeasurements_SourceType", "sql": "[SourceType] IN (1, 2, 3, 4)"} |
| dbo.DerivedMeasurements | CHECK | CK_DerivedMeasurements_Status | {"name": "CK_DerivedMeasurements_Status", "sql": "[Status] IN (1, 2, 3)"} |
| dbo.DerivedMeasurements | CHECK | CK_DerivedMeasurements_UncertaintyEstimate | {"name": "CK_DerivedMeasurements_UncertaintyEstimate", "sql": "[UncertaintyEstimate] IS NULL OR [UncertaintyEstimate] >= 0"} |
| dbo.DerivedMeasurements | CHECK | CK_DerivedMeasurements_Unit | {"name": "CK_DerivedMeasurements_Unit", "sql": "LOWER([Unit]) IN ('mm', 'cm', 'm')"} |
| dbo.DerivedMeasurements | CHECK | CK_DerivedMeasurements_Value | {"name": "CK_DerivedMeasurements_Value", "sql": "[Value] >= 0"} |
| dbo.DroneDevices | KEY | PK_DroneDevices | {"name": "PK_DroneDevices", "columns": ["Id"]} |
| dbo.DroneDevices | INDEX | UX_DroneDevices_SerialNo | {"name": "UX_DroneDevices_SerialNo", "columns": ["SerialNo"], "unique": true, "filter": null} |
| dbo.DroneDevices | CHECK | CK_DroneDevices_Status | {"name": "CK_DroneDevices_Status", "sql": "[Status] IN (1, 2, 3)"} |
| dbo.FieldInspectionAssignments | KEY | PK_FieldInspectionAssignments | {"name": "PK_FieldInspectionAssignments", "columns": ["Id"]} |
| dbo.FieldInspectionAssignments | INDEX | IX_FieldInspectionAssignments_AssignedByUserId | {"name": "IX_FieldInspectionAssignments_AssignedByUserId", "columns": ["AssignedByUserId"], "unique": false, "filter": null} |
| dbo.FieldInspectionAssignments | INDEX | IX_FieldInspectionAssignments_AssignedToUserId | {"name": "IX_FieldInspectionAssignments_AssignedToUserId", "columns": ["AssignedToUserId"], "unique": false, "filter": null} |
| dbo.FieldInspectionAssignments | INDEX | UX_FieldInspectionAssignments_ActiveTask | {"name": "UX_FieldInspectionAssignments_ActiveTask", "columns": ["FieldInspectionTaskId"], "unique": true, "filter": "[Status] = 1"} |
| dbo.FieldInspectionAssignments | CHECK | CK_FieldInspectionAssignments_State | {"name": "CK_FieldInspectionAssignments_State", "sql": "([Status] = 1 AND [EndedAt] IS NULL AND [Reason] IS NULL) OR ([Status] IN (2, 3) AND [EndedAt] IS NOT NULL AND LEN(LTRIM(RTRIM([Reason]))) > 0)"} |
| dbo.FieldInspectionAssignments | CHECK | CK_FieldInspectionAssignments_Status | {"name": "CK_FieldInspectionAssignments_Status", "sql": "[Status] IN (1, 2, 3)"} |
| dbo.FieldInspectionAssignments | CHECK | CK_FieldInspectionAssignments_TimestampOrder | {"name": "CK_FieldInspectionAssignments_TimestampOrder", "sql": "[EndedAt] IS NULL OR [EndedAt] >= [AssignedAt]"} |
| dbo.FieldInspectionEvidenceLinks | KEY | PK_FieldInspectionEvidenceLinks | {"name": "PK_FieldInspectionEvidenceLinks", "columns": ["Id"]} |
| dbo.FieldInspectionEvidenceLinks | INDEX | IX_FieldInspectionEvidenceLinks_AssignmentId | {"name": "IX_FieldInspectionEvidenceLinks_AssignmentId", "columns": ["AssignmentId"], "unique": false, "filter": null} |
| dbo.FieldInspectionEvidenceLinks | INDEX | IX_FieldInspectionEvidenceLinks_FileId | {"name": "IX_FieldInspectionEvidenceLinks_FileId", "columns": ["FileId"], "unique": false, "filter": null} |
| dbo.FieldInspectionEvidenceLinks | INDEX | IX_FieldInspectionEvidenceLinks_ProjectId | {"name": "IX_FieldInspectionEvidenceLinks_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} |
| dbo.FieldInspectionEvidenceLinks | INDEX | IX_FieldInspectionEvidenceLinks_SubmissionId_CaptureOriginId | {"name": "IX_FieldInspectionEvidenceLinks_SubmissionId_CaptureOriginId", "columns": ["SubmissionId", "CaptureOriginId"], "unique": true, "filter": null} |
| dbo.FieldInspectionEvidenceLinks | INDEX | IX_FieldInspectionEvidenceLinks_TaskId | {"name": "IX_FieldInspectionEvidenceLinks_TaskId", "columns": ["TaskId"], "unique": false, "filter": null} |
| dbo.FieldInspectionEvidenceReuseDecisions | KEY | PK_FieldInspectionEvidenceReuseDecisions | {"name": "PK_FieldInspectionEvidenceReuseDecisions", "columns": ["Id"]} |
| dbo.FieldInspectionEvidenceReuseDecisions | INDEX | IX_FieldInspectionEvidenceReuseDecisions_ActorId | {"name": "IX_FieldInspectionEvidenceReuseDecisions_ActorId", "columns": ["ActorId"], "unique": false, "filter": null} |
| dbo.FieldInspectionEvidenceReuseDecisions | INDEX | IX_FieldInspectionEvidenceReuseDecisions_FileId | {"name": "IX_FieldInspectionEvidenceReuseDecisions_FileId", "columns": ["FileId"], "unique": false, "filter": null} |
| dbo.FieldInspectionEvidenceReuseDecisions | INDEX | IX_FieldInspectionEvidenceReuseDecisions_ProjectId | {"name": "IX_FieldInspectionEvidenceReuseDecisions_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} |
| dbo.FieldInspectionEvidenceReuseDecisions | INDEX | IX_FieldInspectionEvidenceReuseDecisions_TaskId | {"name": "IX_FieldInspectionEvidenceReuseDecisions_TaskId", "columns": ["TaskId"], "unique": false, "filter": null} |
| dbo.FieldInspectionLocationProofs | KEY | PK_FieldInspectionLocationProofs | {"name": "PK_FieldInspectionLocationProofs", "columns": ["Id"]} |
| dbo.FieldInspectionLocationProofs | INDEX | IX_FieldInspectionLocationProofs_ProjectId | {"name": "IX_FieldInspectionLocationProofs_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} |
| dbo.FieldInspectionLocationProofs | INDEX | IX_FieldInspectionLocationProofs_SubmissionId | {"name": "IX_FieldInspectionLocationProofs_SubmissionId", "columns": ["SubmissionId"], "unique": false, "filter": null} |
| dbo.FieldInspectionLocationProofs | INDEX | IX_FieldInspectionLocationProofs_TaskId | {"name": "IX_FieldInspectionLocationProofs_TaskId", "columns": ["TaskId"], "unique": false, "filter": null} |
| dbo.FieldInspectionOperationOrigins | KEY | PK_FieldInspectionOperationOrigins | {"name": "PK_FieldInspectionOperationOrigins", "columns": ["Id"]} |
| dbo.FieldInspectionOperationOrigins | INDEX | IX_FieldInspectionOperationOrigins_OriginalActorId | {"name": "IX_FieldInspectionOperationOrigins_OriginalActorId", "columns": ["OriginalActorId"], "unique": false, "filter": null} |
| dbo.FieldInspectionOperationOrigins | INDEX | IX_FieldInspectionOperationOrigins_ProjectId_OriginId | {"name": "IX_FieldInspectionOperationOrigins_ProjectId_OriginId", "columns": ["ProjectId", "OriginId"], "unique": true, "filter": null} |
| dbo.FieldInspectionOperationOrigins | INDEX | IX_FieldInspectionOperationOrigins_TaskId | {"name": "IX_FieldInspectionOperationOrigins_TaskId", "columns": ["TaskId"], "unique": false, "filter": null} |
| dbo.FieldInspectionOperationOrigins | CHECK | CK_FieldInspectionOperationOrigins_Kind | {"name": "CK_FieldInspectionOperationOrigins_Kind", "sql": "[Kind] IN ('FIELD_START','FIELD_SUBMISSION','FIELD_ACCEPT','REPAIR_ASSESSMENT','REPAIR_EXECUTION_START','REPAIR_EXECUTION_FINISH') AND [SchemaVersion]=1"} |
| dbo.FieldInspectionReviews | KEY | PK_FieldInspectionReviews | {"name": "PK_FieldInspectionReviews", "columns": ["Id"]} |
| dbo.FieldInspectionReviews | INDEX | IX_FieldInspectionReviews_ActorId | {"name": "IX_FieldInspectionReviews_ActorId", "columns": ["ActorId"], "unique": false, "filter": null} |
| dbo.FieldInspectionReviews | INDEX | IX_FieldInspectionReviews_ProjectId | {"name": "IX_FieldInspectionReviews_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} |
| dbo.FieldInspectionReviews | INDEX | IX_FieldInspectionReviews_SubmissionId | {"name": "IX_FieldInspectionReviews_SubmissionId", "columns": ["SubmissionId"], "unique": false, "filter": null} |
| dbo.FieldInspectionReviews | INDEX | IX_FieldInspectionReviews_TaskId | {"name": "IX_FieldInspectionReviews_TaskId", "columns": ["TaskId"], "unique": false, "filter": null} |
| dbo.FieldInspectionSessions | KEY | PK_FieldInspectionSessions | {"name": "PK_FieldInspectionSessions", "columns": ["Id"]} |
| dbo.FieldInspectionSessions | INDEX | IX_FieldInspectionSessions_EvidenceFileId | {"name": "IX_FieldInspectionSessions_EvidenceFileId", "columns": ["EvidenceFileId"], "unique": false, "filter": null} |
| dbo.FieldInspectionSessions | INDEX | IX_FieldInspectionSessions_FieldInspectionTaskId | {"name": "IX_FieldInspectionSessions_FieldInspectionTaskId", "columns": ["FieldInspectionTaskId"], "unique": false, "filter": null} |
| dbo.FieldInspectionSessions | INDEX | IX_FieldInspectionSessions_InspectorUserId | {"name": "IX_FieldInspectionSessions_InspectorUserId", "columns": ["InspectorUserId"], "unique": false, "filter": null} |
| dbo.FieldInspectionSessions | INDEX | IX_FieldInspectionSessions_ProjectVersion | {"name": "IX_FieldInspectionSessions_ProjectVersion", "columns": ["ProjectId", "RoadSectionVersionId"], "unique": false, "filter": null} |
| dbo.FieldInspectionSessions | INDEX | IX_FieldInspectionSessions_RoadSectionVersionId | {"name": "IX_FieldInspectionSessions_RoadSectionVersionId", "columns": ["RoadSectionVersionId"], "unique": false, "filter": null} |
| dbo.FieldInspectionSessions | INDEX | IX_FieldInspectionSessions_SurveyId | {"name": "IX_FieldInspectionSessions_SurveyId", "columns": ["SurveyId"], "unique": false, "filter": null} |
| dbo.FieldInspectionSessions | INDEX | UX_FieldInspectionSessions_SessionCode | {"name": "UX_FieldInspectionSessions_SessionCode", "columns": ["SessionCode"], "unique": true, "filter": null} |
| dbo.FieldInspectionSessions | CHECK | CK_FieldInspectionSessions_Purpose | {"name": "CK_FieldInspectionSessions_Purpose", "sql": "[Purpose] IN (1, 2, 3, 4, 5)"} |
| dbo.FieldInspectionSessions | CHECK | CK_FieldInspectionSessions_PurposeScope | {"name": "CK_FieldInspectionSessions_PurposeScope", "sql": "([Purpose] = 1 AND [FieldInspectionTaskId] IS NOT NULL AND [SurveyId] IS NOT NULL AND [InspectorUserId] IS NOT NULL) OR ([Purpose] = 2 AND [FieldInspectionTaskId] IS NULL) OR ([Purpose] IN (3,4,5) AND [FieldInspectionTaskId] IS NOT NULL AND [InspectorUserId] IS NOT NULL)"} |
| dbo.FieldInspectionSessions | CHECK | CK_FieldInspectionSessions_Status | {"name": "CK_FieldInspectionSessions_Status", "sql": "[Status] IN (1, 2, 3, 4)"} |
| dbo.FieldInspectionSubmissions | KEY | PK_FieldInspectionSubmissions | {"name": "PK_FieldInspectionSubmissions", "columns": ["Id"]} |
| dbo.FieldInspectionSubmissions | INDEX | IX_FieldInspectionSubmissions_AssignmentId | {"name": "IX_FieldInspectionSubmissions_AssignmentId", "columns": ["AssignmentId"], "unique": false, "filter": null} |
| dbo.FieldInspectionSubmissions | INDEX | IX_FieldInspectionSubmissions_OperationOriginId | {"name": "IX_FieldInspectionSubmissions_OperationOriginId", "columns": ["OperationOriginId"], "unique": false, "filter": null} |
| dbo.FieldInspectionSubmissions | INDEX | IX_FieldInspectionSubmissions_OriginalActorId | {"name": "IX_FieldInspectionSubmissions_OriginalActorId", "columns": ["OriginalActorId"], "unique": false, "filter": null} |
| dbo.FieldInspectionSubmissions | INDEX | IX_FieldInspectionSubmissions_ParentId | {"name": "IX_FieldInspectionSubmissions_ParentId", "columns": ["ParentId"], "unique": false, "filter": null} |
| dbo.FieldInspectionSubmissions | INDEX | IX_FieldInspectionSubmissions_ProjectId_OriginId | {"name": "IX_FieldInspectionSubmissions_ProjectId_OriginId", "columns": ["ProjectId", "OriginId"], "unique": true, "filter": null} |
| dbo.FieldInspectionSubmissions | INDEX | IX_FieldInspectionSubmissions_RootId_Revision | {"name": "IX_FieldInspectionSubmissions_RootId_Revision", "columns": ["RootId", "Revision"], "unique": true, "filter": null} |
| dbo.FieldInspectionSubmissions | INDEX | IX_FieldInspectionSubmissions_StartOriginId | {"name": "IX_FieldInspectionSubmissions_StartOriginId", "columns": ["StartOriginId"], "unique": false, "filter": null} |
| dbo.FieldInspectionSubmissions | INDEX | UX_FieldInspectionSubmissions_Session | {"name": "UX_FieldInspectionSubmissions_Session", "columns": ["SessionId"], "unique": true, "filter": null} |
| dbo.FieldInspectionSubmissions | INDEX | UX_FieldInspectionSubmissions_TaskRoot | {"name": "UX_FieldInspectionSubmissions_TaskRoot", "columns": ["TaskId"], "unique": true, "filter": "[Revision] = 1"} |
| dbo.FieldInspectionTaskEvents | KEY | PK_FieldInspectionTaskEvents | {"name": "PK_FieldInspectionTaskEvents", "columns": ["Id"]} |
| dbo.FieldInspectionTaskEvents | INDEX | IX_FieldInspectionTaskEvents_ActorId | {"name": "IX_FieldInspectionTaskEvents_ActorId", "columns": ["ActorId"], "unique": false, "filter": null} |
| dbo.FieldInspectionTaskEvents | INDEX | IX_FieldInspectionTaskEvents_AssignmentId | {"name": "IX_FieldInspectionTaskEvents_AssignmentId", "columns": ["AssignmentId"], "unique": false, "filter": null} |
| dbo.FieldInspectionTaskEvents | INDEX | IX_FieldInspectionTaskEvents_LocationImpactDecisionId | {"name": "IX_FieldInspectionTaskEvents_LocationImpactDecisionId", "columns": ["LocationImpactDecisionId"], "unique": false, "filter": null} |
| dbo.FieldInspectionTaskEvents | INDEX | IX_FieldInspectionTaskEvents_LocationImpactId | {"name": "IX_FieldInspectionTaskEvents_LocationImpactId", "columns": ["LocationImpactId"], "unique": false, "filter": null} |
| dbo.FieldInspectionTaskEvents | INDEX | IX_FieldInspectionTaskEvents_ProjectId | {"name": "IX_FieldInspectionTaskEvents_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} |
| dbo.FieldInspectionTaskEvents | INDEX | IX_FieldInspectionTaskEvents_TaskId | {"name": "IX_FieldInspectionTaskEvents_TaskId", "columns": ["TaskId"], "unique": false, "filter": null} |
| dbo.FieldInspectionTasks | KEY | PK_FieldInspectionTasks | {"name": "PK_FieldInspectionTasks", "columns": ["Id"]} |
| dbo.FieldInspectionTasks | INDEX | IX_FieldInspectionTasks_AssignedByUserId | {"name": "IX_FieldInspectionTasks_AssignedByUserId", "columns": ["AssignedByUserId"], "unique": false, "filter": null} |
| dbo.FieldInspectionTasks | INDEX | IX_FieldInspectionTasks_CrsProfileRevisionId | {"name": "IX_FieldInspectionTasks_CrsProfileRevisionId", "columns": ["CrsProfileRevisionId"], "unique": false, "filter": null} |
| dbo.FieldInspectionTasks | INDEX | IX_FieldInspectionTasks_DefectId | {"name": "IX_FieldInspectionTasks_DefectId", "columns": ["DefectId"], "unique": false, "filter": null} |
| dbo.FieldInspectionTasks | INDEX | IX_FieldInspectionTasks_LayoutRevisionId | {"name": "IX_FieldInspectionTasks_LayoutRevisionId", "columns": ["LayoutRevisionId"], "unique": false, "filter": null} |
| dbo.FieldInspectionTasks | INDEX | IX_FieldInspectionTasks_MapPublicationId | {"name": "IX_FieldInspectionTasks_MapPublicationId", "columns": ["MapPublicationId"], "unique": false, "filter": null} |
| dbo.FieldInspectionTasks | INDEX | IX_FieldInspectionTasks_ProjectId | {"name": "IX_FieldInspectionTasks_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} |
| dbo.FieldInspectionTasks | INDEX | IX_FieldInspectionTasks_RepairItemId | {"name": "IX_FieldInspectionTasks_RepairItemId", "columns": ["RepairItemId"], "unique": false, "filter": null} |
| dbo.FieldInspectionTasks | INDEX | IX_FieldInspectionTasks_ReviewedByUserId | {"name": "IX_FieldInspectionTasks_ReviewedByUserId", "columns": ["ReviewedByUserId"], "unique": false, "filter": null} |
| dbo.FieldInspectionTasks | INDEX | IX_FieldInspectionTasks_RoadSectionVersionId | {"name": "IX_FieldInspectionTasks_RoadSectionVersionId", "columns": ["RoadSectionVersionId"], "unique": false, "filter": null} |
| dbo.FieldInspectionTasks | INDEX | IX_FieldInspectionTasks_SegmentSetId | {"name": "IX_FieldInspectionTasks_SegmentSetId", "columns": ["SegmentSetId"], "unique": false, "filter": null} |
| dbo.FieldInspectionTasks | INDEX | IX_FieldInspectionTasks_SurveyId | {"name": "IX_FieldInspectionTasks_SurveyId", "columns": ["SurveyId"], "unique": false, "filter": null} |
| dbo.FieldInspectionTasks | INDEX | UX_FieldInspectionTasks_TaskCode | {"name": "UX_FieldInspectionTasks_TaskCode", "columns": ["TaskCode"], "unique": true, "filter": null} |
| dbo.FieldInspectionTasks | CHECK | CK_FieldInspectionTasks_MeasurementScope_Json | {"name": "CK_FieldInspectionTasks_MeasurementScope_Json", "sql": "ISJSON([MeasurementScope]) = 1 AND LEFT(LTRIM([MeasurementScope]), 1) = '{'"} |
| dbo.FieldInspectionTasks | CHECK | CK_FieldInspectionTasks_RepairMode | {"name": "CK_FieldInspectionTasks_RepairMode", "sql": "([TaskMode]='MEASURE_ONLY' AND [RepairItemId] IS NULL) OR ([LifecycleVersion]=2 AND [TaskMode] IN('NORMAL','CONDITIONAL_FT') AND [RepairItemId] IS NOT NULL)"} |
| dbo.FieldInspectionTasks | CHECK | CK_FieldInspectionTasks_ReviewDecision | {"name": "CK_FieldInspectionTasks_ReviewDecision", "sql": "([ReviewDecision] IS NULL AND [ReviewedByUserId] IS NULL AND [ReviewedAt] IS NULL) OR ([ReviewDecision] IS NOT NULL AND [ReviewedByUserId] IS NOT NULL AND [ReviewedAt] IS NOT NULL AND [Status] = 7)"} |
| dbo.FieldInspectionTasks | CHECK | CK_FieldInspectionTasks_Source | {"name": "CK_FieldInspectionTasks_Source", "sql": "([LifecycleVersion]=1 AND [SourceKind]='SURVEY' AND [SurveyId] IS NOT NULL) OR ([LifecycleVersion]=2 AND [Purpose] IN(3,4,5) AND (([SourceKind]='SURVEY' AND [SurveyId] IS NOT NULL) OR ([SourceKind]='REPORTER' AND [SurveyId] IS NULL)))"} |
| dbo.FieldInspectionTasks | CHECK | CK_FieldInspectionTasks_Status | {"name": "CK_FieldInspectionTasks_Status", "sql": "[Status] IN (1, 2, 3, 4, 5, 6, 7, 8)"} |
| dbo.FieldTaskStartOrigins | KEY | PK_FieldTaskStartOrigins | {"name": "PK_FieldTaskStartOrigins", "columns": ["Id"]} |
| dbo.FieldTaskStartOrigins | INDEX | IX_FieldTaskStartOrigins_AssignmentId | {"name": "IX_FieldTaskStartOrigins_AssignmentId", "columns": ["AssignmentId"], "unique": false, "filter": null} |
| dbo.FieldTaskStartOrigins | INDEX | IX_FieldTaskStartOrigins_CrsProfileRevisionId | {"name": "IX_FieldTaskStartOrigins_CrsProfileRevisionId", "columns": ["CrsProfileRevisionId"], "unique": false, "filter": null} |
| dbo.FieldTaskStartOrigins | INDEX | IX_FieldTaskStartOrigins_LayoutRevisionId | {"name": "IX_FieldTaskStartOrigins_LayoutRevisionId", "columns": ["LayoutRevisionId"], "unique": false, "filter": null} |
| dbo.FieldTaskStartOrigins | INDEX | IX_FieldTaskStartOrigins_MapPublicationId | {"name": "IX_FieldTaskStartOrigins_MapPublicationId", "columns": ["MapPublicationId"], "unique": false, "filter": null} |
| dbo.FieldTaskStartOrigins | INDEX | IX_FieldTaskStartOrigins_OperationOriginId | {"name": "IX_FieldTaskStartOrigins_OperationOriginId", "columns": ["OperationOriginId"], "unique": false, "filter": null} |
| dbo.FieldTaskStartOrigins | INDEX | IX_FieldTaskStartOrigins_OriginalActorId | {"name": "IX_FieldTaskStartOrigins_OriginalActorId", "columns": ["OriginalActorId"], "unique": false, "filter": null} |
| dbo.FieldTaskStartOrigins | INDEX | IX_FieldTaskStartOrigins_ProjectId_OriginId | {"name": "IX_FieldTaskStartOrigins_ProjectId_OriginId", "columns": ["ProjectId", "OriginId"], "unique": true, "filter": null} |
| dbo.FieldTaskStartOrigins | INDEX | IX_FieldTaskStartOrigins_RouteVersionId | {"name": "IX_FieldTaskStartOrigins_RouteVersionId", "columns": ["RouteVersionId"], "unique": false, "filter": null} |
| dbo.FieldTaskStartOrigins | INDEX | IX_FieldTaskStartOrigins_SegmentSetId | {"name": "IX_FieldTaskStartOrigins_SegmentSetId", "columns": ["SegmentSetId"], "unique": false, "filter": null} |
| dbo.FieldTaskStartOrigins | INDEX | IX_FieldTaskStartOrigins_TaskId | {"name": "IX_FieldTaskStartOrigins_TaskId", "columns": ["TaskId"], "unique": true, "filter": null} |
| dbo.Files | KEY | PK_Files | {"name": "PK_Files", "columns": ["Id"]} |
| dbo.Files | INDEX | IX_Files_UploadedByUserId | {"name": "IX_Files_UploadedByUserId", "columns": ["UploadedByUserId"], "unique": false, "filter": null} |
| dbo.Files | INDEX | UX_Files_StorageUri | {"name": "UX_Files_StorageUri", "columns": ["StorageUri"], "unique": true, "filter": null} |
| dbo.Files | CHECK | CK_Files_Checksum_Sha256Lowercase | {"name": "CK_Files_Checksum_Sha256Lowercase", "sql": "LEN([Checksum]) = 64 AND [Checksum] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'"} |
| dbo.Files | CHECK | CK_Files_SizeBytes_NonNegative | {"name": "CK_Files_SizeBytes_NonNegative", "sql": "[SizeBytes] >= 0"} |
| dbo.FileScopes | KEY | PK_FileScopes | {"name": "PK_FileScopes", "columns": ["Id"]} |
| dbo.FileScopes | INDEX | IX_FileScopes_OwnerUserId | {"name": "IX_FileScopes_OwnerUserId", "columns": ["OwnerUserId"], "unique": false, "filter": null} |
| dbo.FileScopes | INDEX | IX_FileScopes_ProjectId_OwnerUserId | {"name": "IX_FileScopes_ProjectId_OwnerUserId", "columns": ["ProjectId", "OwnerUserId"], "unique": false, "filter": null} |
| dbo.FileScopes | INDEX | UX_FileScopes_FileId | {"name": "UX_FileScopes_FileId", "columns": ["FileId"], "unique": true, "filter": null} |
| dbo.FileScopes | CHECK | CK_FileScopes_PrivateShape | {"name": "CK_FileScopes_PrivateShape", "sql": "[ProjectId] IS NOT NULL OR ([Purpose] = 'REPORT_PHOTO' AND [TargetId] IS NULL)"} |
| dbo.Flights | KEY | PK_Flights | {"name": "PK_Flights", "columns": ["Id"]} |
| dbo.Flights | INDEX | IX_Flights_DroneDeviceId | {"name": "IX_Flights_DroneDeviceId", "columns": ["DroneDeviceId"], "unique": false, "filter": null} |
| dbo.Flights | INDEX | IX_Flights_OperatorUserId | {"name": "IX_Flights_OperatorUserId", "columns": ["OperatorUserId"], "unique": false, "filter": null} |
| dbo.Flights | INDEX | UX_Flights_SurveyFlightNo | {"name": "UX_Flights_SurveyFlightNo", "columns": ["SurveyId", "FlightNo"], "unique": true, "filter": null} |
| dbo.Flights | CHECK | CK_Flights_TimestampOrder | {"name": "CK_Flights_TimestampOrder", "sql": "[EndedAt] IS NULL OR [EndedAt] >= [StartedAt]"} |
| dbo.GeometryLocationImpactDecisions | KEY | PK_GeometryLocationImpactDecisions | {"name": "PK_GeometryLocationImpactDecisions", "columns": ["Id"]} |
| dbo.GeometryLocationImpactDecisions | INDEX | IX_GeometryLocationImpactDecisions_ActorId | {"name": "IX_GeometryLocationImpactDecisions_ActorId", "columns": ["ActorId"], "unique": false, "filter": null} |
| dbo.GeometryLocationImpactDecisions | INDEX | IX_GeometryLocationImpactDecisions_ImpactId_TaskId | {"name": "IX_GeometryLocationImpactDecisions_ImpactId_TaskId", "columns": ["ImpactId", "TaskId"], "unique": true, "filter": null} |
| dbo.GeometryLocationImpactDecisions | CHECK | CK_GeometryLocationImpactDecisions_Action | {"name": "CK_GeometryLocationImpactDecisions_Action", "sql": "[Action] IN ('VERIFY','CONTINUE','STOP','REASSIGN')"} |
| dbo.GeometryLocationImpacts | KEY | PK_GeometryLocationImpacts | {"name": "PK_GeometryLocationImpacts", "columns": ["Id"]} |
| dbo.GeometryLocationImpacts | INDEX | IX_GeometryLocationImpacts_NewRouteVersionId | {"name": "IX_GeometryLocationImpacts_NewRouteVersionId", "columns": ["NewRouteVersionId"], "unique": false, "filter": null} |
| dbo.GeometryLocationImpacts | INDEX | IX_GeometryLocationImpacts_PreviousRouteVersionId_NewRouteVersionId | {"name": "IX_GeometryLocationImpacts_PreviousRouteVersionId_NewRouteVersionId", "columns": ["PreviousRouteVersionId", "NewRouteVersionId"], "unique": true, "filter": null} |
| dbo.GeometryLocationImpacts | INDEX | IX_GeometryLocationImpacts_ProjectId | {"name": "IX_GeometryLocationImpacts_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} |
| dbo.GeometryLocationImpacts | INDEX | IX_GeometryLocationImpacts_RecordedBy | {"name": "IX_GeometryLocationImpacts_RecordedBy", "columns": ["RecordedBy"], "unique": false, "filter": null} |
| dbo.GeometryLocationImpacts | CHECK | CK_GeometryLocationImpacts_Json | {"name": "CK_GeometryLocationImpacts_Json", "sql": "ISJSON([AffectedReferencesJson])=1"} |
| dbo.GeometryLocationImpacts | CHECK | CK_GeometryLocationImpacts_Versions | {"name": "CK_GeometryLocationImpacts_Versions", "sql": "[PreviousRouteVersionId]<>[NewRouteVersionId]"} |
| dbo.GeometryMapPublications | KEY | PK_GeometryMapPublications | {"name": "PK_GeometryMapPublications", "columns": ["Id"]} |
| dbo.GeometryMapPublications | INDEX | IX_GeometryMapPublications_CrsProfileRevisionId_ProjectId | {"name": "IX_GeometryMapPublications_CrsProfileRevisionId_ProjectId", "columns": ["CrsProfileRevisionId", "ProjectId"], "unique": false, "filter": null} |
| dbo.GeometryMapPublications | INDEX | IX_GeometryMapPublications_LayoutRevisionId | {"name": "IX_GeometryMapPublications_LayoutRevisionId", "columns": ["LayoutRevisionId"], "unique": false, "filter": null} |
| dbo.GeometryMapPublications | INDEX | IX_GeometryMapPublications_ProjectId_PublishedAt_Id | {"name": "IX_GeometryMapPublications_ProjectId_PublishedAt_Id", "columns": ["ProjectId", "PublishedAt", "Id"], "unique": false, "filter": null} |
| dbo.GeometryMapPublications | INDEX | IX_GeometryMapPublications_PublishedBy | {"name": "IX_GeometryMapPublications_PublishedBy", "columns": ["PublishedBy"], "unique": false, "filter": null} |
| dbo.GeometryMapPublications | INDEX | IX_GeometryMapPublications_RouteVersionId | {"name": "IX_GeometryMapPublications_RouteVersionId", "columns": ["RouteVersionId"], "unique": false, "filter": null} |
| dbo.GeometryMapPublications | INDEX | IX_GeometryMapPublications_SegmentSetId | {"name": "IX_GeometryMapPublications_SegmentSetId", "columns": ["SegmentSetId"], "unique": false, "filter": null} |
| dbo.GeometryMapPublications | CHECK | CK_GeometryMapPublications_Json | {"name": "CK_GeometryMapPublications_Json", "sql": "ISJSON([SnapshotJson])=1"} |
| dbo.GeometryMapPublications | CHECK | CK_GeometryMapPublications_Mode | {"name": "CK_GeometryMapPublications_Mode", "sql": "[PublicationMode] IN ('SAMPLE','OFFICIAL')"} |
| dbo.GroundTruthMeasurements | KEY | PK_GroundTruthMeasurements | {"name": "PK_GroundTruthMeasurements", "columns": ["Id"]} |
| dbo.GroundTruthMeasurements | INDEX | IX_GroundTruthMeasurements_DefectId | {"name": "IX_GroundTruthMeasurements_DefectId", "columns": ["DefectId"], "unique": false, "filter": null} |
| dbo.GroundTruthMeasurements | INDEX | IX_GroundTruthMeasurements_EvidenceFileId | {"name": "IX_GroundTruthMeasurements_EvidenceFileId", "columns": ["EvidenceFileId"], "unique": false, "filter": null} |
| dbo.GroundTruthMeasurements | INDEX | IX_GroundTruthMeasurements_RoadVersionType | {"name": "IX_GroundTruthMeasurements_RoadVersionType", "columns": ["RoadSectionVersionId", "MeasurementType"], "unique": false, "filter": null} |
| dbo.GroundTruthMeasurements | INDEX | IX_GroundTruthMeasurements_SurveyId | {"name": "IX_GroundTruthMeasurements_SurveyId", "columns": ["SurveyId"], "unique": false, "filter": null} |
| dbo.GroundTruthMeasurements | INDEX | UX_GroundTruthMeasurements_SessionSample | {"name": "UX_GroundTruthMeasurements_SessionSample", "columns": ["FieldInspectionSessionId", "SampleId"], "unique": true, "filter": null} |
| dbo.GroundTruthMeasurements | CHECK | CK_GroundTruthMeasurements_EvidenceOrReason | {"name": "CK_GroundTruthMeasurements_EvidenceOrReason", "sql": "[EvidenceFileId] IS NOT NULL OR [ValueState]='UNKNOWN' OR [LocationState]='UNKNOWN' OR ([Notes] IS NOT NULL AND LEN(LTRIM(RTRIM([Notes]))) > 0)"} |
| dbo.GroundTruthMeasurements | CHECK | CK_GroundTruthMeasurements_Location | {"name": "CK_GroundTruthMeasurements_Location", "sql": "([Location] IS NOT NULL AND [LocationState]='CAPTURED' AND [Location].STSrid=4326 AND [Location].STIsEmpty()=0) OR ([Location] IS NULL AND [LocationState]='UNKNOWN' AND [LocationReason] IS NOT NULL AND LEN(LTRIM(RTRIM([LocationReason])))>0)"} |
| dbo.GroundTruthMeasurements | CHECK | CK_GroundTruthMeasurements_MeasurementType | {"name": "CK_GroundTruthMeasurements_MeasurementType", "sql": "[MeasurementType] IN (1, 2, 3, 4)"} |
| dbo.GroundTruthMeasurements | CHECK | CK_GroundTruthMeasurements_Unit | {"name": "CK_GroundTruthMeasurements_Unit", "sql": "([MeasurementType] IN (1,2,3) AND [Dimension]='LENGTH' AND LOWER([Unit]) IN ('mm','cm','m')) OR ([MeasurementType]=4 AND [Dimension]='AREA' AND [Unit]=N'm²')"} |
| dbo.GroundTruthMeasurements | CHECK | CK_GroundTruthMeasurements_Value | {"name": "CK_GroundTruthMeasurements_Value", "sql": "([ValueState]='KNOWN' AND [Value] IS NOT NULL AND [Value]>=0) OR ([ValueState]='UNKNOWN' AND [Value] IS NULL AND [UnknownReason] IS NOT NULL AND LEN(LTRIM(RTRIM([UnknownReason])))>0)"} |
| dbo.H6NotificationAudits | KEY | PK_H6NotificationAudits | {"name": "PK_H6NotificationAudits", "columns": ["Id"]} |
| dbo.H6NotificationAudits | INDEX | IX_H6NotificationAudits_DedupKey | {"name": "IX_H6NotificationAudits_DedupKey", "columns": ["DedupKey"], "unique": true, "filter": null} |
| dbo.H6NotificationAudits | INDEX | IX_H6NotificationAudits_NotificationId | {"name": "IX_H6NotificationAudits_NotificationId", "columns": ["NotificationId"], "unique": false, "filter": null} |
| dbo.H6NotificationAudits | INDEX | IX_H6NotificationAudits_OutboxMessageId | {"name": "IX_H6NotificationAudits_OutboxMessageId", "columns": ["OutboxMessageId"], "unique": false, "filter": null} |
| dbo.H6NotificationAudits | INDEX | IX_H6NotificationAudits_PreviousAuditId | {"name": "IX_H6NotificationAudits_PreviousAuditId", "columns": ["PreviousAuditId"], "unique": false, "filter": null} |
| dbo.H6NotificationAudits | INDEX | IX_H6NotificationAudits_ProjectId | {"name": "IX_H6NotificationAudits_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} |
| dbo.H6NotificationCalendar | KEY | PK_H6NotificationCalendar | {"name": "PK_H6NotificationCalendar", "columns": ["Id"]} |
| dbo.H6NotificationCalendar | INDEX | IX_H6NotificationCalendar_ClockId_ScheduledAtUtc | {"name": "IX_H6NotificationCalendar_ClockId_ScheduledAtUtc", "columns": ["ClockId", "ScheduledAtUtc"], "unique": true, "filter": null} |
| dbo.H6NotificationCalendar | INDEX | IX_H6NotificationCalendar_OutboxMessageId | {"name": "IX_H6NotificationCalendar_OutboxMessageId", "columns": ["OutboxMessageId"], "unique": false, "filter": null} |
| dbo.H6NotificationDeliveries | KEY | PK_H6NotificationDeliveries | {"name": "PK_H6NotificationDeliveries", "columns": ["Id"]} |
| dbo.H6NotificationDeliveries | INDEX | IX_H6NotificationDeliveries_NotificationId | {"name": "IX_H6NotificationDeliveries_NotificationId", "columns": ["NotificationId"], "unique": true, "filter": "[NotificationId] IS NOT NULL"} |
| dbo.H6NotificationDeliveries | INDEX | IX_H6NotificationDeliveries_OccurrenceId_RecipientKey | {"name": "IX_H6NotificationDeliveries_OccurrenceId_RecipientKey", "columns": ["OccurrenceId", "RecipientKey"], "unique": true, "filter": null} |
| dbo.H6NotificationDeliveries | INDEX | IX_H6NotificationDeliveries_RecipientUserId | {"name": "IX_H6NotificationDeliveries_RecipientUserId", "columns": ["RecipientUserId"], "unique": false, "filter": null} |
| dbo.H6NotificationDeliveries | CHECK | CK_H6NotificationDeliveries_State | {"name": "CK_H6NotificationDeliveries_State", "sql": "[Status] IN ('PENDING','UNRESOLVED','DELIVERED') AND (([Status]='DELIVERED' AND [RecipientUserId] IS NOT NULL AND [NotificationId] IS NOT NULL AND [DeliveredAtUtc] IS NOT NULL AND [ReasonCode] IS NULL) OR ([Status]<>'DELIVERED' AND [NotificationId] IS NULL AND [DeliveredAtUtc] IS NULL))"} |
| dbo.H6NotificationDeliveryAttempts | KEY | PK_H6NotificationDeliveryAttempts | {"name": "PK_H6NotificationDeliveryAttempts", "columns": ["Id"]} |
| dbo.H6NotificationDeliveryAttempts | INDEX | IX_H6NotificationDeliveryAttempts_DeliveryId | {"name": "IX_H6NotificationDeliveryAttempts_DeliveryId", "columns": ["DeliveryId"], "unique": false, "filter": null} |
| dbo.H6NotificationEventReceipts | KEY | PK_H6NotificationEventReceipts | {"name": "PK_H6NotificationEventReceipts", "columns": ["OutboxMessageId"]} |
| dbo.H6NotificationEventReceipts | INDEX | IX_H6NotificationEventReceipts_OccurrenceId | {"name": "IX_H6NotificationEventReceipts_OccurrenceId", "columns": ["OccurrenceId"], "unique": false, "filter": null} |
| dbo.H6NotificationOccurrences | KEY | PK_H6NotificationOccurrences | {"name": "PK_H6NotificationOccurrences", "columns": ["Id"]} |
| dbo.H6NotificationOccurrences | INDEX | IX_H6NotificationOccurrences_OccurrenceKey | {"name": "IX_H6NotificationOccurrences_OccurrenceKey", "columns": ["OccurrenceKey"], "unique": true, "filter": null} |
| dbo.H6NotificationOccurrences | INDEX | IX_H6NotificationOccurrences_ProjectId | {"name": "IX_H6NotificationOccurrences_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} |
| dbo.H6NotificationOccurrences | INDEX | IX_H6NotificationOccurrences_SourceEventId | {"name": "IX_H6NotificationOccurrences_SourceEventId", "columns": ["SourceEventId"], "unique": false, "filter": null} |
| dbo.H6NotificationOccurrences | CHECK | CK_H6NotificationOccurrences_Hashes | {"name": "CK_H6NotificationOccurrences_Hashes", "sql": "LEN([OccurrenceKey])=64 AND LEN([ContentFingerprint])=64"} |
| dbo.H6NotificationOccurrences | CHECK | CK_H6NotificationOccurrences_Json | {"name": "CK_H6NotificationOccurrences_Json", "sql": "ISJSON([PayloadJson])=1"} |
| dbo.H6NotificationScopes | KEY | PK_H6NotificationScopes | {"name": "PK_H6NotificationScopes", "columns": ["NotificationId"]} |
| dbo.H6NotificationScopes | INDEX | IX_H6NotificationScopes_CurrentAuditId | {"name": "IX_H6NotificationScopes_CurrentAuditId", "columns": ["CurrentAuditId"], "unique": false, "filter": null} |
| dbo.H6NotificationScopes | INDEX | IX_H6NotificationScopes_OccurrenceId | {"name": "IX_H6NotificationScopes_OccurrenceId", "columns": ["OccurrenceId"], "unique": false, "filter": null} |
| dbo.H6NotificationScopes | INDEX | IX_H6NotificationScopes_ProjectId | {"name": "IX_H6NotificationScopes_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} |
| dbo.HandoverDocuments | KEY | PK_HandoverDocuments | {"name": "PK_HandoverDocuments", "columns": ["Id"]} |
| dbo.HandoverDocuments | INDEX | IX_HandoverDocuments_AcceptedByUserId | {"name": "IX_HandoverDocuments_AcceptedByUserId", "columns": ["AcceptedByUserId"], "unique": false, "filter": null} |
| dbo.HandoverDocuments | INDEX | IX_HandoverDocuments_FileId | {"name": "IX_HandoverDocuments_FileId", "columns": ["FileId"], "unique": false, "filter": null} |
| dbo.HandoverDocuments | INDEX | UX_HandoverDocuments_ProjectId_DocumentNo | {"name": "UX_HandoverDocuments_ProjectId_DocumentNo", "columns": ["ProjectId", "DocumentNo"], "unique": true, "filter": null} |
| dbo.IdempotencyRecords | KEY | PK_IdempotencyRecords | {"name": "PK_IdempotencyRecords", "columns": ["Id"]} |
| dbo.IdempotencyRecords | INDEX | IX_IdempotencyRecords_OperationId | {"name": "IX_IdempotencyRecords_OperationId", "columns": ["OperationId"], "unique": false, "filter": null} |
| dbo.IdempotencyRecords | INDEX | UX_IdempotencyRecords_ScopeKey | {"name": "UX_IdempotencyRecords_ScopeKey", "columns": ["ActorUserId", "ProjectId", "Operation", "IdempotencyKey"], "unique": true, "filter": null} |
| dbo.IdempotencyRecords | CHECK | CK_IdempotencyRecords_OutcomeJson_Json | {"name": "CK_IdempotencyRecords_OutcomeJson_Json", "sql": "ISJSON([OutcomeJson]) = 1"} |
| dbo.IncidentCases | KEY | PK_IncidentCases | {"name": "PK_IncidentCases", "columns": ["Id"]} |
| dbo.IncidentCases | INDEX | IX_IncidentCases_GeometrySegmentSetId_GeometryRouteVersionId | {"name": "IX_IncidentCases_GeometrySegmentSetId_GeometryRouteVersionId", "columns": ["GeometrySegmentSetId", "GeometryRouteVersionId"], "unique": false, "filter": null} |
| dbo.IncidentCases | INDEX | IX_IncidentCases_LinkedTargetCaseId | {"name": "IX_IncidentCases_LinkedTargetCaseId", "columns": ["LinkedTargetCaseId"], "unique": false, "filter": null} |
| dbo.IncidentCases | INDEX | IX_IncidentCases_ProjectId | {"name": "IX_IncidentCases_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} |
| dbo.IncidentCases | CHECK | CK_IncidentCases_GeometryPair | {"name": "CK_IncidentCases_GeometryPair", "sql": "([GeometryRouteVersionId] IS NULL AND [GeometrySegmentSetId] IS NULL) OR ([GeometryRouteVersionId] IS NOT NULL AND [GeometrySegmentSetId] IS NOT NULL)"} |
| dbo.LD06ActionEvidence | KEY | PK_LD06ActionEvidence | {"name": "PK_LD06ActionEvidence", "columns": ["ActionId", "FileId"]} |
| dbo.LD06ActionEvidence | INDEX | IX_LD06ActionEvidence_FileId | {"name": "IX_LD06ActionEvidence_FileId", "columns": ["FileId"], "unique": false, "filter": null} |
| dbo.LD06LifecycleActions | KEY | PK_LD06LifecycleActions | {"name": "PK_LD06LifecycleActions", "columns": ["Id"]} |
| dbo.LD06LifecycleActions | INDEX | IX_LD06LifecycleActions_ActorId | {"name": "IX_LD06LifecycleActions_ActorId", "columns": ["ActorId"], "unique": false, "filter": null} |
| dbo.LD06LifecycleActions | INDEX | IX_LD06LifecycleActions_DefectId | {"name": "IX_LD06LifecycleActions_DefectId", "columns": ["DefectId"], "unique": false, "filter": null} |
| dbo.LD06LifecycleActions | INDEX | IX_LD06LifecycleActions_LinkedDefectId | {"name": "IX_LD06LifecycleActions_LinkedDefectId", "columns": ["LinkedDefectId"], "unique": false, "filter": null} |
| dbo.LD06LifecycleActions | INDEX | IX_LD06LifecycleActions_ObligationId | {"name": "IX_LD06LifecycleActions_ObligationId", "columns": ["ObligationId"], "unique": false, "filter": null} |
| dbo.LD06LifecycleActions | INDEX | IX_LD06LifecycleActions_PriorRepairDecisionId | {"name": "IX_LD06LifecycleActions_PriorRepairDecisionId", "columns": ["PriorRepairDecisionId"], "unique": false, "filter": null} |
| dbo.LD06LifecycleActions | INDEX | IX_LD06LifecycleActions_ProjectId | {"name": "IX_LD06LifecycleActions_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} |
| dbo.LD06LifecycleActions | INDEX | IX_LD06LifecycleActions_ReceivingProjectId | {"name": "IX_LD06LifecycleActions_ReceivingProjectId", "columns": ["ReceivingProjectId"], "unique": false, "filter": null} |
| dbo.LD06LifecycleActions | INDEX | IX_LD06LifecycleActions_SourceActionId_Kind | {"name": "IX_LD06LifecycleActions_SourceActionId_Kind", "columns": ["SourceActionId", "Kind"], "unique": true, "filter": "[SourceActionId] IS NOT NULL AND [Kind] IN (2,7)"} |
| dbo.LD06LifecycleActions | CHECK | CK_LD06LifecycleActions_Kind | {"name": "CK_LD06LifecycleActions_Kind", "sql": "[Kind] BETWEEN 1 AND 7 AND ISJSON([FactsJson])=1"} |
| dbo.MeasurementValidationSamples | KEY | PK_MeasurementValidationSamples | {"name": "PK_MeasurementValidationSamples", "columns": ["Id"]} |
| dbo.MeasurementValidationSamples | INDEX | IX_MeasurementValidationSamples_DerivedMeasurementId | {"name": "IX_MeasurementValidationSamples_DerivedMeasurementId", "columns": ["DerivedMeasurementId"], "unique": false, "filter": null} |
| dbo.MeasurementValidationSamples | INDEX | IX_MeasurementValidationSamples_GroundTruthMeasurementId | {"name": "IX_MeasurementValidationSamples_GroundTruthMeasurementId", "columns": ["GroundTruthMeasurementId"], "unique": false, "filter": null} |
| dbo.MeasurementValidationSamples | INDEX | UX_MeasurementValidationSamples_RunPair | {"name": "UX_MeasurementValidationSamples_RunPair", "columns": ["ValidationRunId", "GroundTruthMeasurementId", "DerivedMeasurementId"], "unique": true, "filter": null} |
| dbo.MeasurementValidationSamples | CHECK | CK_MeasurementValidationSamples_InclusionStatus | {"name": "CK_MeasurementValidationSamples_InclusionStatus", "sql": "[InclusionStatus] IN (1, 2, 3)"} |
| dbo.MeasurementValidationSamples | CHECK | CK_MeasurementValidationSamples_Reason | {"name": "CK_MeasurementValidationSamples_Reason", "sql": "[InclusionStatus] = 1 OR LEN(LTRIM(RTRIM([ExclusionReason]))) > 0"} |
| dbo.NativeRouteVersionFacts | KEY | PK_NativeRouteVersionFacts | {"name": "PK_NativeRouteVersionFacts", "columns": ["RoadSectionVersionId"]} |
| dbo.NativeRouteVersionFacts | INDEX | IX_NativeRouteVersionFacts_CrsProfileRevisionId_ProjectId | {"name": "IX_NativeRouteVersionFacts_CrsProfileRevisionId_ProjectId", "columns": ["CrsProfileRevisionId", "ProjectId"], "unique": false, "filter": null} |
| dbo.NativeRouteVersionFacts | INDEX | IX_NativeRouteVersionFacts_ParentRouteVersionId | {"name": "IX_NativeRouteVersionFacts_ParentRouteVersionId", "columns": ["ParentRouteVersionId"], "unique": false, "filter": null} |
| dbo.NativeRouteVersionFacts | INDEX | IX_NativeRouteVersionFacts_RouteSystemId_ProjectId | {"name": "IX_NativeRouteVersionFacts_RouteSystemId_ProjectId", "columns": ["RouteSystemId", "ProjectId"], "unique": false, "filter": null} |
| dbo.NativeRouteVersionFacts | CHECK | CK_NativeRouteFacts | {"name": "CK_NativeRouteFacts", "sql": "[CanonicalLengthMeters]>0 AND ([DeclaredLengthMeters] IS NULL OR [DeclaredLengthMeters]>0) AND (([RouteKind]='MAIN' AND [ParentRouteVersionId] IS NULL AND [JunctionOffsetMeters] IS NULL) OR ([RouteKind]='BRANCH' AND [ParentRouteVersionId] IS NOT NULL AND [JunctionOffsetMeters] IS NOT NULL AND [JunctionOffsetMeters]>=0)) AND ([CalibrationJson] IS NULL OR ISJSON([CalibrationJson])=1)"} |
| dbo.Notifications | KEY | PK_Notifications | {"name": "PK_Notifications", "columns": ["Id"]} |
| dbo.Notifications | INDEX | IX_Notifications_RecipientOccurredAtId | {"name": "IX_Notifications_RecipientOccurredAtId", "columns": ["RecipientUserId", "OccurredAtUtc", "Id"], "unique": false, "filter": null} |
| dbo.Notifications | INDEX | IX_Notifications_RecipientUserId_ReadAt | {"name": "IX_Notifications_RecipientUserId_ReadAt", "columns": ["RecipientUserId", "ReadAt"], "unique": false, "filter": null} |
| dbo.Notifications | INDEX | UX_Notifications_RecipientSourceEvent | {"name": "UX_Notifications_RecipientSourceEvent", "columns": ["RecipientUserId", "SourceEntityType", "SourceEntityId", "EventType"], "unique": true, "filter": null} |
| dbo.ObligationResponsibilities | KEY | PK_ObligationResponsibilities | {"name": "PK_ObligationResponsibilities", "columns": ["ObligationId"]} |
| dbo.ObligationResponsibilities | INDEX | IX_ObligationResponsibilities_AcceptanceActionId | {"name": "IX_ObligationResponsibilities_AcceptanceActionId", "columns": ["AcceptanceActionId"], "unique": false, "filter": null} |
| dbo.ObligationResponsibilities | INDEX | IX_ObligationResponsibilities_CurrentProjectId | {"name": "IX_ObligationResponsibilities_CurrentProjectId", "columns": ["CurrentProjectId"], "unique": false, "filter": null} |
| dbo.ObligationResponsibilities | INDEX | IX_ObligationResponsibilities_OriginProjectId | {"name": "IX_ObligationResponsibilities_OriginProjectId", "columns": ["OriginProjectId"], "unique": false, "filter": null} |
| dbo.OfflineAdmittedFileReferences | KEY | AK_OfflineAdmittedFileReferences_Id_ProjectId | {"name": "AK_OfflineAdmittedFileReferences_Id_ProjectId", "columns": ["Id", "ProjectId"]} |
| dbo.OfflineAdmittedFileReferences | KEY | PK_OfflineAdmittedFileReferences | {"name": "PK_OfflineAdmittedFileReferences", "columns": ["Id"]} |
| dbo.OfflineAdmittedFileReferences | INDEX | IX_OfflineAdmittedFileReferences_ActualFileOwnerId | {"name": "IX_OfflineAdmittedFileReferences_ActualFileOwnerId", "columns": ["ActualFileOwnerId"], "unique": false, "filter": null} |
| dbo.OfflineAdmittedFileReferences | INDEX | IX_OfflineAdmittedFileReferences_ActualUploadedById | {"name": "IX_OfflineAdmittedFileReferences_ActualUploadedById", "columns": ["ActualUploadedById"], "unique": false, "filter": null} |
| dbo.OfflineAdmittedFileReferences | INDEX | IX_OfflineAdmittedFileReferences_AdmissionId_CaptureOriginId | {"name": "IX_OfflineAdmittedFileReferences_AdmissionId_CaptureOriginId", "columns": ["AdmissionId", "CaptureOriginId"], "unique": true, "filter": null} |
| dbo.OfflineAdmittedFileReferences | INDEX | IX_OfflineAdmittedFileReferences_AdmissionId_ProjectId | {"name": "IX_OfflineAdmittedFileReferences_AdmissionId_ProjectId", "columns": ["AdmissionId", "ProjectId"], "unique": false, "filter": null} |
| dbo.OfflineAdmittedFileReferences | INDEX | IX_OfflineAdmittedFileReferences_BindingId_ProjectId | {"name": "IX_OfflineAdmittedFileReferences_BindingId_ProjectId", "columns": ["BindingId", "ProjectId"], "unique": false, "filter": null} |
| dbo.OfflineAdmittedFileReferences | INDEX | IX_OfflineAdmittedFileReferences_CurrentImporterId | {"name": "IX_OfflineAdmittedFileReferences_CurrentImporterId", "columns": ["CurrentImporterId"], "unique": false, "filter": null} |
| dbo.OfflineAdmittedFileReferences | INDEX | IX_OfflineAdmittedFileReferences_FileId | {"name": "IX_OfflineAdmittedFileReferences_FileId", "columns": ["FileId"], "unique": false, "filter": null} |
| dbo.OfflineAdmittedFileReferences | INDEX | IX_OfflineAdmittedFileReferences_OriginalActorId | {"name": "IX_OfflineAdmittedFileReferences_OriginalActorId", "columns": ["OriginalActorId"], "unique": false, "filter": null} |
| dbo.OfflineAdmittedFileReferences | INDEX | IX_OfflineAdmittedFileReferences_ProjectId | {"name": "IX_OfflineAdmittedFileReferences_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} |
| dbo.OfflineAdmittedFileReferences | INDEX | IX_OfflineAdmittedFileReferences_TaskId | {"name": "IX_OfflineAdmittedFileReferences_TaskId", "columns": ["TaskId"], "unique": false, "filter": null} |
| dbo.OfflineAdmittedFileReferences | CHECK | CK_OfflineAdmittedFileReferences_CaptureFactsJson | {"name": "CK_OfflineAdmittedFileReferences_CaptureFactsJson", "sql": "ISJSON([CaptureFactsJson])=1"} |
| dbo.OfflineAdmittedFileReferences | CHECK | CK_OfflineAdmittedFileReferences_ContentChecksum | {"name": "CK_OfflineAdmittedFileReferences_ContentChecksum", "sql": "LEN([ContentChecksum])=64 AND [ContentChecksum] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'"} |
| dbo.OfflineDeviceRegistrations | KEY | AK_OfflineDeviceRegistrations_Id_ProjectId | {"name": "AK_OfflineDeviceRegistrations_Id_ProjectId", "columns": ["Id", "ProjectId"]} |
| dbo.OfflineDeviceRegistrations | KEY | PK_OfflineDeviceRegistrations | {"name": "PK_OfflineDeviceRegistrations", "columns": ["Id"]} |
| dbo.OfflineDeviceRegistrations | INDEX | IX_OfflineDeviceRegistrations_ActorId | {"name": "IX_OfflineDeviceRegistrations_ActorId", "columns": ["ActorId"], "unique": false, "filter": null} |
| dbo.OfflineDeviceRegistrations | INDEX | IX_OfflineDeviceRegistrations_ProjectId_ActorId_DeviceId_Revision | {"name": "IX_OfflineDeviceRegistrations_ProjectId_ActorId_DeviceId_Revision", "columns": ["ProjectId", "ActorId", "DeviceId", "Revision"], "unique": true, "filter": null} |
| dbo.OfflineDeviceRegistrations | INDEX | IX_OfflineDeviceRegistrations_ProjectId_KeyFingerprint | {"name": "IX_OfflineDeviceRegistrations_ProjectId_KeyFingerprint", "columns": ["ProjectId", "KeyFingerprint"], "unique": false, "filter": null} |
| dbo.OfflineDeviceRegistrations | CHECK | CK_OfflineDeviceRegistrations_KeyFingerprint | {"name": "CK_OfflineDeviceRegistrations_KeyFingerprint", "sql": "LEN([KeyFingerprint])=64 AND [KeyFingerprint] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'"} |
| dbo.OfflineDeviceRegistrations | CHECK | CK_OfflineDeviceRegistrations_Revision | {"name": "CK_OfflineDeviceRegistrations_Revision", "sql": "[Revision]>0"} |
| dbo.OfflineDeviceRevocations | KEY | AK_OfflineDeviceRevocations_Id_ProjectId | {"name": "AK_OfflineDeviceRevocations_Id_ProjectId", "columns": ["Id", "ProjectId"]} |
| dbo.OfflineDeviceRevocations | KEY | PK_OfflineDeviceRevocations | {"name": "PK_OfflineDeviceRevocations", "columns": ["Id"]} |
| dbo.OfflineDeviceRevocations | INDEX | IX_OfflineDeviceRevocations_DeviceRegistrationId | {"name": "IX_OfflineDeviceRevocations_DeviceRegistrationId", "columns": ["DeviceRegistrationId"], "unique": true, "filter": null} |
| dbo.OfflineDeviceRevocations | INDEX | IX_OfflineDeviceRevocations_DeviceRegistrationId_ProjectId | {"name": "IX_OfflineDeviceRevocations_DeviceRegistrationId_ProjectId", "columns": ["DeviceRegistrationId", "ProjectId"], "unique": false, "filter": null} |
| dbo.OfflineDeviceRevocations | INDEX | IX_OfflineDeviceRevocations_ProjectId | {"name": "IX_OfflineDeviceRevocations_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} |
| dbo.OfflineDeviceRevocations | INDEX | IX_OfflineDeviceRevocations_RevokedBy | {"name": "IX_OfflineDeviceRevocations_RevokedBy", "columns": ["RevokedBy"], "unique": false, "filter": null} |
| dbo.OfflineEncryptedCaptureArtifacts | KEY | AK_OfflineEncryptedCaptureArtifacts_Id_ProjectId | {"name": "AK_OfflineEncryptedCaptureArtifacts_Id_ProjectId", "columns": ["Id", "ProjectId"]} |
| dbo.OfflineEncryptedCaptureArtifacts | KEY | PK_OfflineEncryptedCaptureArtifacts | {"name": "PK_OfflineEncryptedCaptureArtifacts", "columns": ["Id"]} |
| dbo.OfflineEncryptedCaptureArtifacts | INDEX | IX_OfflineEncryptedCaptureArtifacts_OriginalActorId | {"name": "IX_OfflineEncryptedCaptureArtifacts_OriginalActorId", "columns": ["OriginalActorId"], "unique": false, "filter": null} |
| dbo.OfflineEncryptedCaptureArtifacts | INDEX | IX_OfflineEncryptedCaptureArtifacts_ParentPackageId_CaptureOriginId_ManifestHash_ChunkIndex | {"name": "IX_OfflineEncryptedCaptureArtifacts_ParentPackageId_CaptureOriginId_ManifestHash_ChunkIndex", "columns": ["ParentPackageId", "CaptureOriginId", "ManifestHash", "ChunkIndex"], "unique": true, "filter": null} |
| dbo.OfflineEncryptedCaptureArtifacts | INDEX | IX_OfflineEncryptedCaptureArtifacts_ParentPackageId_ProjectId | {"name": "IX_OfflineEncryptedCaptureArtifacts_ParentPackageId_ProjectId", "columns": ["ParentPackageId", "ProjectId"], "unique": false, "filter": null} |
| dbo.OfflineEncryptedCaptureArtifacts | INDEX | IX_OfflineEncryptedCaptureArtifacts_ProjectId | {"name": "IX_OfflineEncryptedCaptureArtifacts_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} |
| dbo.OfflineEncryptedCaptureArtifacts | INDEX | IX_OfflineEncryptedCaptureArtifacts_RegisteredBy | {"name": "IX_OfflineEncryptedCaptureArtifacts_RegisteredBy", "columns": ["RegisteredBy"], "unique": false, "filter": null} |
| dbo.OfflineEncryptedCaptureArtifacts | INDEX | IX_OfflineEncryptedCaptureArtifacts_SourceDeviceRegistrationId_ProjectId | {"name": "IX_OfflineEncryptedCaptureArtifacts_SourceDeviceRegistrationId_ProjectId", "columns": ["SourceDeviceRegistrationId", "ProjectId"], "unique": false, "filter": null} |
| dbo.OfflineEncryptedCaptureArtifacts | INDEX | IX_OfflineEncryptedCaptureArtifacts_TaskId | {"name": "IX_OfflineEncryptedCaptureArtifacts_TaskId", "columns": ["TaskId"], "unique": false, "filter": null} |
| dbo.OfflineEncryptedCaptureArtifacts | CHECK | CK_OfflineEncryptedCaptureArtifacts_Chunk | {"name": "CK_OfflineEncryptedCaptureArtifacts_Chunk", "sql": "[ChunkIndex]>=0 AND [ChunkIndex]<32 AND [ChunkOffset]>=0 AND [ChunkLength]>0 AND [ChunkLength]<=16777216"} |
| dbo.OfflineEncryptedCaptureArtifacts | CHECK | CK_OfflineEncryptedCaptureArtifacts_CipherEnvelopeJson | {"name": "CK_OfflineEncryptedCaptureArtifacts_CipherEnvelopeJson", "sql": "ISJSON([CipherEnvelopeJson])=1"} |
| dbo.OfflineEncryptedCaptureArtifacts | CHECK | CK_OfflineEncryptedCaptureArtifacts_EnvelopeFingerprint | {"name": "CK_OfflineEncryptedCaptureArtifacts_EnvelopeFingerprint", "sql": "LEN([EnvelopeFingerprint])=64 AND [EnvelopeFingerprint] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'"} |
| dbo.OfflineEncryptedCaptureArtifacts | CHECK | CK_OfflineEncryptedCaptureArtifacts_ManifestHash | {"name": "CK_OfflineEncryptedCaptureArtifacts_ManifestHash", "sql": "LEN([ManifestHash])=64 AND [ManifestHash] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'"} |
| dbo.OfflineEncryptedCaptureArtifacts | CHECK | CK_OfflineEncryptedCaptureArtifacts_PlaintextChecksum | {"name": "CK_OfflineEncryptedCaptureArtifacts_PlaintextChecksum", "sql": "LEN([PlaintextChecksum])=64 AND [PlaintextChecksum] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'"} |
| dbo.OfflineEncryptedCaptureArtifacts | CHECK | CK_OfflineEncryptedCaptureArtifacts_SignedManifestJson | {"name": "CK_OfflineEncryptedCaptureArtifacts_SignedManifestJson", "sql": "ISJSON([SignedManifestJson])=1"} |
| dbo.OfflineEncryptedPackages | KEY | AK_OfflineEncryptedPackages_Id_ProjectId | {"name": "AK_OfflineEncryptedPackages_Id_ProjectId", "columns": ["Id", "ProjectId"]} |
| dbo.OfflineEncryptedPackages | KEY | PK_OfflineEncryptedPackages | {"name": "PK_OfflineEncryptedPackages", "columns": ["Id"]} |
| dbo.OfflineEncryptedPackages | INDEX | IX_OfflineEncryptedPackages_OriginalActorId | {"name": "IX_OfflineEncryptedPackages_OriginalActorId", "columns": ["OriginalActorId"], "unique": false, "filter": null} |
| dbo.OfflineEncryptedPackages | INDEX | IX_OfflineEncryptedPackages_ProjectId_SourceDeviceRegistrationId_SourceBatchId | {"name": "IX_OfflineEncryptedPackages_ProjectId_SourceDeviceRegistrationId_SourceBatchId", "columns": ["ProjectId", "SourceDeviceRegistrationId", "SourceBatchId"], "unique": false, "filter": null} |
| dbo.OfflineEncryptedPackages | INDEX | IX_OfflineEncryptedPackages_RegisteredBy | {"name": "IX_OfflineEncryptedPackages_RegisteredBy", "columns": ["RegisteredBy"], "unique": false, "filter": null} |
| dbo.OfflineEncryptedPackages | INDEX | IX_OfflineEncryptedPackages_SourceDeviceRegistrationId_ProjectId | {"name": "IX_OfflineEncryptedPackages_SourceDeviceRegistrationId_ProjectId", "columns": ["SourceDeviceRegistrationId", "ProjectId"], "unique": false, "filter": null} |
| dbo.OfflineEncryptedPackages | CHECK | CK_OfflineEncryptedPackages_CipherPackageJson | {"name": "CK_OfflineEncryptedPackages_CipherPackageJson", "sql": "ISJSON([CipherPackageJson])=1"} |
| dbo.OfflineEncryptedPackages | CHECK | CK_OfflineEncryptedPackages_ManifestHash | {"name": "CK_OfflineEncryptedPackages_ManifestHash", "sql": "LEN([ManifestHash])=64 AND [ManifestHash] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'"} |
| dbo.OfflineEncryptedPackages | CHECK | CK_OfflineEncryptedPackages_PackageFingerprint | {"name": "CK_OfflineEncryptedPackages_PackageFingerprint", "sql": "LEN([PackageFingerprint])=64 AND [PackageFingerprint] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'"} |
| dbo.OfflineEncryptedPackages | CHECK | CK_OfflineEncryptedPackages_PayloadHash | {"name": "CK_OfflineEncryptedPackages_PayloadHash", "sql": "LEN([PayloadHash])=64 AND [PayloadHash] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'"} |
| dbo.OfflineEncryptedPackages | CHECK | CK_OfflineEncryptedPackages_SignedManifestJson | {"name": "CK_OfflineEncryptedPackages_SignedManifestJson", "sql": "ISJSON([SignedManifestJson])=1"} |
| dbo.OfflineEvidenceCaptureReferences | KEY | AK_OfflineEvidenceCaptureReferences_Id_ProjectId | {"name": "AK_OfflineEvidenceCaptureReferences_Id_ProjectId", "columns": ["Id", "ProjectId"]} |
| dbo.OfflineEvidenceCaptureReferences | KEY | PK_OfflineEvidenceCaptureReferences | {"name": "PK_OfflineEvidenceCaptureReferences", "columns": ["Id"]} |
| dbo.OfflineEvidenceCaptureReferences | INDEX | IX_OfflineEvidenceCaptureReferences_ActualUploaderId | {"name": "IX_OfflineEvidenceCaptureReferences_ActualUploaderId", "columns": ["ActualUploaderId"], "unique": false, "filter": null} |
| dbo.OfflineEvidenceCaptureReferences | INDEX | IX_OfflineEvidenceCaptureReferences_AdmissionId_CaptureOriginId | {"name": "IX_OfflineEvidenceCaptureReferences_AdmissionId_CaptureOriginId", "columns": ["AdmissionId", "CaptureOriginId"], "unique": true, "filter": null} |
| dbo.OfflineEvidenceCaptureReferences | INDEX | IX_OfflineEvidenceCaptureReferences_AdmissionId_ProjectId | {"name": "IX_OfflineEvidenceCaptureReferences_AdmissionId_ProjectId", "columns": ["AdmissionId", "ProjectId"], "unique": false, "filter": null} |
| dbo.OfflineEvidenceCaptureReferences | INDEX | IX_OfflineEvidenceCaptureReferences_BindingId_ProjectId | {"name": "IX_OfflineEvidenceCaptureReferences_BindingId_ProjectId", "columns": ["BindingId", "ProjectId"], "unique": false, "filter": null} |
| dbo.OfflineEvidenceCaptureReferences | INDEX | IX_OfflineEvidenceCaptureReferences_FileId | {"name": "IX_OfflineEvidenceCaptureReferences_FileId", "columns": ["FileId"], "unique": true, "filter": null} |
| dbo.OfflineEvidenceCaptureReferences | INDEX | IX_OfflineEvidenceCaptureReferences_GrantId_ProjectId | {"name": "IX_OfflineEvidenceCaptureReferences_GrantId_ProjectId", "columns": ["GrantId", "ProjectId"], "unique": false, "filter": null} |
| dbo.OfflineEvidenceCaptureReferences | INDEX | IX_OfflineEvidenceCaptureReferences_OriginalActorId | {"name": "IX_OfflineEvidenceCaptureReferences_OriginalActorId", "columns": ["OriginalActorId"], "unique": false, "filter": null} |
| dbo.OfflineEvidenceCaptureReferences | INDEX | IX_OfflineEvidenceCaptureReferences_ProjectId | {"name": "IX_OfflineEvidenceCaptureReferences_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} |
| dbo.OfflineEvidenceCaptureReferences | INDEX | IX_OfflineEvidenceCaptureReferences_TaskId | {"name": "IX_OfflineEvidenceCaptureReferences_TaskId", "columns": ["TaskId"], "unique": false, "filter": null} |
| dbo.OfflineEvidenceCaptureReferences | INDEX | IX_OfflineEvidenceCaptureReferences_UploadSessionId | {"name": "IX_OfflineEvidenceCaptureReferences_UploadSessionId", "columns": ["UploadSessionId"], "unique": true, "filter": null} |
| dbo.OfflineEvidenceCaptureReferences | CHECK | CK_OfflineEvidenceCaptureReferences_CaptureFactsJson | {"name": "CK_OfflineEvidenceCaptureReferences_CaptureFactsJson", "sql": "ISJSON([CaptureFactsJson])=1"} |
| dbo.OfflineEvidenceCaptureReferences | CHECK | CK_OfflineEvidenceCaptureReferences_Checksum | {"name": "CK_OfflineEvidenceCaptureReferences_Checksum", "sql": "LEN([Checksum])=64 AND [Checksum] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'"} |
| dbo.OfflineHandoverGrantRevocations | KEY | AK_OfflineHandoverGrantRevocations_Id_ProjectId | {"name": "AK_OfflineHandoverGrantRevocations_Id_ProjectId", "columns": ["Id", "ProjectId"]} |
| dbo.OfflineHandoverGrantRevocations | KEY | PK_OfflineHandoverGrantRevocations | {"name": "PK_OfflineHandoverGrantRevocations", "columns": ["Id"]} |
| dbo.OfflineHandoverGrantRevocations | INDEX | IX_OfflineHandoverGrantRevocations_GrantId | {"name": "IX_OfflineHandoverGrantRevocations_GrantId", "columns": ["GrantId"], "unique": true, "filter": null} |
| dbo.OfflineHandoverGrantRevocations | INDEX | IX_OfflineHandoverGrantRevocations_GrantId_ProjectId | {"name": "IX_OfflineHandoverGrantRevocations_GrantId_ProjectId", "columns": ["GrantId", "ProjectId"], "unique": false, "filter": null} |
| dbo.OfflineHandoverGrantRevocations | INDEX | IX_OfflineHandoverGrantRevocations_ProjectId | {"name": "IX_OfflineHandoverGrantRevocations_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} |
| dbo.OfflineHandoverGrantRevocations | INDEX | IX_OfflineHandoverGrantRevocations_RevokedBy | {"name": "IX_OfflineHandoverGrantRevocations_RevokedBy", "columns": ["RevokedBy"], "unique": false, "filter": null} |
| dbo.OfflineHandoverGrants | KEY | AK_OfflineHandoverGrants_Id_ProjectId | {"name": "AK_OfflineHandoverGrants_Id_ProjectId", "columns": ["Id", "ProjectId"]} |
| dbo.OfflineHandoverGrants | KEY | PK_OfflineHandoverGrants | {"name": "PK_OfflineHandoverGrants", "columns": ["Id"]} |
| dbo.OfflineHandoverGrants | INDEX | IX_OfflineHandoverGrants_IssuedBy | {"name": "IX_OfflineHandoverGrants_IssuedBy", "columns": ["IssuedBy"], "unique": false, "filter": null} |
| dbo.OfflineHandoverGrants | INDEX | IX_OfflineHandoverGrants_PackageId_ProjectId | {"name": "IX_OfflineHandoverGrants_PackageId_ProjectId", "columns": ["PackageId", "ProjectId"], "unique": false, "filter": null} |
| dbo.OfflineHandoverGrants | INDEX | IX_OfflineHandoverGrants_ProjectId_PackageId_RecipientActorId_ExpiresAt | {"name": "IX_OfflineHandoverGrants_ProjectId_PackageId_RecipientActorId_ExpiresAt", "columns": ["ProjectId", "PackageId", "RecipientActorId", "ExpiresAt"], "unique": false, "filter": null} |
| dbo.OfflineHandoverGrants | INDEX | IX_OfflineHandoverGrants_RecipientActorId | {"name": "IX_OfflineHandoverGrants_RecipientActorId", "columns": ["RecipientActorId"], "unique": false, "filter": null} |
| dbo.OfflineHandoverGrants | INDEX | IX_OfflineHandoverGrants_RecipientDeviceRegistrationId_ProjectId | {"name": "IX_OfflineHandoverGrants_RecipientDeviceRegistrationId_ProjectId", "columns": ["RecipientDeviceRegistrationId", "ProjectId"], "unique": false, "filter": null} |
| dbo.OfflineHandoverGrants | INDEX | IX_OfflineHandoverGrants_SourceActorId | {"name": "IX_OfflineHandoverGrants_SourceActorId", "columns": ["SourceActorId"], "unique": false, "filter": null} |
| dbo.OfflineHandoverGrants | INDEX | IX_OfflineHandoverGrants_SourceDeviceRegistrationId_ProjectId | {"name": "IX_OfflineHandoverGrants_SourceDeviceRegistrationId_ProjectId", "columns": ["SourceDeviceRegistrationId", "ProjectId"], "unique": false, "filter": null} |
| dbo.OfflineHandoverGrants | CHECK | CK_OfflineHandoverGrants_Expiry | {"name": "CK_OfflineHandoverGrants_Expiry", "sql": "[ExpiresAt]=DATEADD(hour,24,[IssuedAt])"} |
| dbo.OfflineHandoverGrants | CHECK | CK_OfflineHandoverGrants_ManifestHash | {"name": "CK_OfflineHandoverGrants_ManifestHash", "sql": "LEN([ManifestHash])=64 AND [ManifestHash] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'"} |
| dbo.OfflineHandoverGrants | CHECK | CK_OfflineHandoverGrants_ScopeJson | {"name": "CK_OfflineHandoverGrants_ScopeJson", "sql": "ISJSON([ScopeJson])=1"} |
| dbo.OfflineOperationAdmissions | KEY | AK_OfflineOperationAdmissions_Id_ProjectId | {"name": "AK_OfflineOperationAdmissions_Id_ProjectId", "columns": ["Id", "ProjectId"]} |
| dbo.OfflineOperationAdmissions | KEY | PK_OfflineOperationAdmissions | {"name": "PK_OfflineOperationAdmissions", "columns": ["Id"]} |
| dbo.OfflineOperationAdmissions | INDEX | IX_OfflineOperationAdmissions_BatchId_BindingId | {"name": "IX_OfflineOperationAdmissions_BatchId_BindingId", "columns": ["BatchId", "BindingId"], "unique": true, "filter": null} |
| dbo.OfflineOperationAdmissions | INDEX | IX_OfflineOperationAdmissions_BatchId_ProjectId | {"name": "IX_OfflineOperationAdmissions_BatchId_ProjectId", "columns": ["BatchId", "ProjectId"], "unique": false, "filter": null} |
| dbo.OfflineOperationAdmissions | INDEX | IX_OfflineOperationAdmissions_BindingId_ProjectId | {"name": "IX_OfflineOperationAdmissions_BindingId_ProjectId", "columns": ["BindingId", "ProjectId"], "unique": false, "filter": null} |
| dbo.OfflineOperationAdmissions | INDEX | IX_OfflineOperationAdmissions_CurrentImporterId | {"name": "IX_OfflineOperationAdmissions_CurrentImporterId", "columns": ["CurrentImporterId"], "unique": false, "filter": null} |
| dbo.OfflineOperationAdmissions | INDEX | IX_OfflineOperationAdmissions_GrantId_ProjectId | {"name": "IX_OfflineOperationAdmissions_GrantId_ProjectId", "columns": ["GrantId", "ProjectId"], "unique": false, "filter": null} |
| dbo.OfflineOperationAdmissions | INDEX | IX_OfflineOperationAdmissions_ProjectId | {"name": "IX_OfflineOperationAdmissions_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} |
| dbo.OfflineOperationAdmissions | CHECK | CK_OfflineOperationAdmissions_Mode | {"name": "CK_OfflineOperationAdmissions_Mode", "sql": "([AdmissionMode]='DIRECT_SYNC' AND [GrantId] IS NULL) OR ([AdmissionMode]='HANDOVER' AND [GrantId] IS NOT NULL)"} |
| dbo.OfflineOperationAdmissions | CHECK | CK_OfflineOperationAdmissions_ScopeFactsJson | {"name": "CK_OfflineOperationAdmissions_ScopeFactsJson", "sql": "ISJSON([ScopeFactsJson])=1"} |
| dbo.OfflineOperationBindings | KEY | AK_OfflineOperationBindings_Id_ProjectId | {"name": "AK_OfflineOperationBindings_Id_ProjectId", "columns": ["Id", "ProjectId"]} |
| dbo.OfflineOperationBindings | KEY | PK_OfflineOperationBindings | {"name": "PK_OfflineOperationBindings", "columns": ["Id"]} |
| dbo.OfflineOperationBindings | INDEX | IX_OfflineOperationBindings_AssignmentId | {"name": "IX_OfflineOperationBindings_AssignmentId", "columns": ["AssignmentId"], "unique": false, "filter": null} |
| dbo.OfflineOperationBindings | INDEX | IX_OfflineOperationBindings_EffectId | {"name": "IX_OfflineOperationBindings_EffectId", "columns": ["EffectId"], "unique": true, "filter": null} |
| dbo.OfflineOperationBindings | INDEX | IX_OfflineOperationBindings_OriginalActorId | {"name": "IX_OfflineOperationBindings_OriginalActorId", "columns": ["OriginalActorId"], "unique": false, "filter": null} |
| dbo.OfflineOperationBindings | INDEX | IX_OfflineOperationBindings_ProjectId_OriginId | {"name": "IX_OfflineOperationBindings_ProjectId_OriginId", "columns": ["ProjectId", "OriginId"], "unique": true, "filter": null} |
| dbo.OfflineOperationBindings | INDEX | IX_OfflineOperationBindings_RepairResourceId | {"name": "IX_OfflineOperationBindings_RepairResourceId", "columns": ["RepairResourceId"], "unique": false, "filter": null} |
| dbo.OfflineOperationBindings | INDEX | IX_OfflineOperationBindings_SnapshotId_ProjectId | {"name": "IX_OfflineOperationBindings_SnapshotId_ProjectId", "columns": ["SnapshotId", "ProjectId"], "unique": false, "filter": null} |
| dbo.OfflineOperationBindings | INDEX | IX_OfflineOperationBindings_SourceDeviceRegistrationId_ProjectId | {"name": "IX_OfflineOperationBindings_SourceDeviceRegistrationId_ProjectId", "columns": ["SourceDeviceRegistrationId", "ProjectId"], "unique": false, "filter": null} |
| dbo.OfflineOperationBindings | INDEX | IX_OfflineOperationBindings_TaskId | {"name": "IX_OfflineOperationBindings_TaskId", "columns": ["TaskId"], "unique": false, "filter": null} |
| dbo.OfflineOperationBindings | CHECK | CK_OfflineOperationBindings_CorePayloadHash | {"name": "CK_OfflineOperationBindings_CorePayloadHash", "sql": "LEN([CorePayloadHash])=64 AND [CorePayloadHash] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'"} |
| dbo.OfflineOperationBindings | CHECK | CK_OfflineOperationBindings_EnvelopeHash | {"name": "CK_OfflineOperationBindings_EnvelopeHash", "sql": "LEN([EnvelopeHash])=64 AND [EnvelopeHash] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'"} |
| dbo.OfflineOperationBindings | CHECK | CK_OfflineOperationBindings_EnvelopeJson | {"name": "CK_OfflineOperationBindings_EnvelopeJson", "sql": "ISJSON([EnvelopeJson])=1"} |
| dbo.OfflineOperationBindings | CHECK | CK_OfflineOperationBindings_RepairScope | {"name": "CK_OfflineOperationBindings_RepairScope", "sql": "([Kind] IN ('FIELD_ACCEPT','FIELD_START','FIELD_SUBMISSION') AND [RepairResourceId] IS NULL) OR ([Kind] IN ('REPAIR_ASSESSMENT','REPAIR_EXECUTION_START','REPAIR_EXECUTION_FINISH') AND [RepairResourceId] IS NOT NULL)"} |
| dbo.OfflineOperationResults | KEY | AK_OfflineOperationResults_Id_ProjectId | {"name": "AK_OfflineOperationResults_Id_ProjectId", "columns": ["Id", "ProjectId"]} |
| dbo.OfflineOperationResults | KEY | PK_OfflineOperationResults | {"name": "PK_OfflineOperationResults", "columns": ["Id"]} |
| dbo.OfflineOperationResults | INDEX | IX_OfflineOperationResults_AdmissionId_ProjectId | {"name": "IX_OfflineOperationResults_AdmissionId_ProjectId", "columns": ["AdmissionId", "ProjectId"], "unique": false, "filter": null} |
| dbo.OfflineOperationResults | INDEX | IX_OfflineOperationResults_BatchId_ProjectId | {"name": "IX_OfflineOperationResults_BatchId_ProjectId", "columns": ["BatchId", "ProjectId"], "unique": false, "filter": null} |
| dbo.OfflineOperationResults | INDEX | IX_OfflineOperationResults_ProjectId_OriginId_RecordedAt | {"name": "IX_OfflineOperationResults_ProjectId_OriginId_RecordedAt", "columns": ["ProjectId", "OriginId", "RecordedAt"], "unique": false, "filter": null} |
| dbo.OfflineOperationResults | CHECK | CK_OfflineOperationResults_Acknowledgment | {"name": "CK_OfflineOperationResults_Acknowledgment", "sql": "([State] IN ('COMMITTED','REPLAYED') AND [DurableAck]=1 AND [EffectId] IS NOT NULL) OR ([State] IN ('CONFLICT','PENDING_DEPENDENCY','REJECTED','STALE_SNAPSHOT') AND [DurableAck]=0)"} |
| dbo.OfflineOperationResults | CHECK | CK_OfflineOperationResults_OutcomeJson | {"name": "CK_OfflineOperationResults_OutcomeJson", "sql": "ISJSON([OutcomeJson])=1"} |
| dbo.OfflineOperationResults | CHECK | CK_OfflineOperationResults_Time | {"name": "CK_OfflineOperationResults_Time", "sql": "[TimeProvenance] IN ('UNCERTAIN','VERIFIED_ORIGINAL') AND ([VerifiedFinishedAt] IS NOT NULL OR [SyncLateness]='UNKNOWN')"} |
| dbo.OfflineOriginTimeVerifications | KEY | AK_OfflineOriginTimeVerifications_Id_ProjectId | {"name": "AK_OfflineOriginTimeVerifications_Id_ProjectId", "columns": ["Id", "ProjectId"]} |
| dbo.OfflineOriginTimeVerifications | KEY | PK_OfflineOriginTimeVerifications | {"name": "PK_OfflineOriginTimeVerifications", "columns": ["Id"]} |
| dbo.OfflineOriginTimeVerifications | INDEX | IX_OfflineOriginTimeVerifications_BindingId_ProjectId | {"name": "IX_OfflineOriginTimeVerifications_BindingId_ProjectId", "columns": ["BindingId", "ProjectId"], "unique": false, "filter": null} |
| dbo.OfflineOriginTimeVerifications | INDEX | IX_OfflineOriginTimeVerifications_BindingId_ProofSourceKind_ProofSourceId | {"name": "IX_OfflineOriginTimeVerifications_BindingId_ProofSourceKind_ProofSourceId", "columns": ["BindingId", "ProofSourceKind", "ProofSourceId"], "unique": true, "filter": null} |
| dbo.OfflineOriginTimeVerifications | INDEX | IX_OfflineOriginTimeVerifications_CanonicalOriginId | {"name": "IX_OfflineOriginTimeVerifications_CanonicalOriginId", "columns": ["CanonicalOriginId"], "unique": false, "filter": null} |
| dbo.OfflineOriginTimeVerifications | INDEX | IX_OfflineOriginTimeVerifications_ProjectId | {"name": "IX_OfflineOriginTimeVerifications_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} |
| dbo.OfflineOriginTimeVerifications | INDEX | IX_OfflineOriginTimeVerifications_VerifiedBy | {"name": "IX_OfflineOriginTimeVerifications_VerifiedBy", "columns": ["VerifiedBy"], "unique": false, "filter": null} |
| dbo.OfflineOriginTimeVerifications | CHECK | CK_OfflineOriginTimeVerifications_Source | {"name": "CK_OfflineOriginTimeVerifications_Source", "sql": "[ProofSourceKind] IN ('FIELD_START_SERVER_ORIGIN','REPAIR_FINISH_SERVER_ORIGIN') AND [OriginalOccurredAtUtc]<=[RecordedAtUtc]"} |
| dbo.OfflinePackageFileReferences | KEY | AK_OfflinePackageFileReferences_Id_ProjectId | {"name": "AK_OfflinePackageFileReferences_Id_ProjectId", "columns": ["Id", "ProjectId"]} |
| dbo.OfflinePackageFileReferences | KEY | PK_OfflinePackageFileReferences | {"name": "PK_OfflinePackageFileReferences", "columns": ["Id"]} |
| dbo.OfflinePackageFileReferences | INDEX | IX_OfflinePackageFileReferences_FileId | {"name": "IX_OfflinePackageFileReferences_FileId", "columns": ["FileId"], "unique": false, "filter": null} |
| dbo.OfflinePackageFileReferences | INDEX | IX_OfflinePackageFileReferences_PackageId_FileId | {"name": "IX_OfflinePackageFileReferences_PackageId_FileId", "columns": ["PackageId", "FileId"], "unique": true, "filter": null} |
| dbo.OfflinePackageFileReferences | INDEX | IX_OfflinePackageFileReferences_PackageId_ProjectId | {"name": "IX_OfflinePackageFileReferences_PackageId_ProjectId", "columns": ["PackageId", "ProjectId"], "unique": false, "filter": null} |
| dbo.OfflinePackageFileReferences | INDEX | IX_OfflinePackageFileReferences_ProjectId | {"name": "IX_OfflinePackageFileReferences_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} |
| dbo.OfflinePackageFileReferences | CHECK | CK_OfflinePackageFileReferences_CaptureFactsJson | {"name": "CK_OfflinePackageFileReferences_CaptureFactsJson", "sql": "ISJSON([CaptureFactsJson])=1"} |
| dbo.OfflinePackageFileReferences | CHECK | CK_OfflinePackageFileReferences_ContentChecksum | {"name": "CK_OfflinePackageFileReferences_ContentChecksum", "sql": "LEN([ContentChecksum])=64 AND [ContentChecksum] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'"} |
| dbo.OfflineSyncBatches | KEY | AK_OfflineSyncBatches_Id_ProjectId | {"name": "AK_OfflineSyncBatches_Id_ProjectId", "columns": ["Id", "ProjectId"]} |
| dbo.OfflineSyncBatches | KEY | PK_OfflineSyncBatches | {"name": "PK_OfflineSyncBatches", "columns": ["Id"]} |
| dbo.OfflineSyncBatches | INDEX | IX_OfflineSyncBatches_CurrentImporterId | {"name": "IX_OfflineSyncBatches_CurrentImporterId", "columns": ["CurrentImporterId"], "unique": false, "filter": null} |
| dbo.OfflineSyncBatches | INDEX | IX_OfflineSyncBatches_GrantId_ProjectId | {"name": "IX_OfflineSyncBatches_GrantId_ProjectId", "columns": ["GrantId", "ProjectId"], "unique": false, "filter": null} |
| dbo.OfflineSyncBatches | INDEX | IX_OfflineSyncBatches_PackageId_ProjectId | {"name": "IX_OfflineSyncBatches_PackageId_ProjectId", "columns": ["PackageId", "ProjectId"], "unique": false, "filter": null} |
| dbo.OfflineSyncBatches | INDEX | IX_OfflineSyncBatches_ProjectId_SourceDeviceRegistrationId_SourceBatchId_CurrentImporterId_ContentHash | {"name": "IX_OfflineSyncBatches_ProjectId_SourceDeviceRegistrationId_SourceBatchId_CurrentImporterId_ContentHash", "columns": ["ProjectId", "SourceDeviceRegistrationId", "SourceBatchId", "CurrentImporterId", "ContentHash"], "unique": true, "filter": null} |
| dbo.OfflineSyncBatches | INDEX | IX_OfflineSyncBatches_RecipientDeviceRegistrationId_ProjectId | {"name": "IX_OfflineSyncBatches_RecipientDeviceRegistrationId_ProjectId", "columns": ["RecipientDeviceRegistrationId", "ProjectId"], "unique": false, "filter": null} |
| dbo.OfflineSyncBatches | INDEX | IX_OfflineSyncBatches_SourceDeviceRegistrationId_ProjectId | {"name": "IX_OfflineSyncBatches_SourceDeviceRegistrationId_ProjectId", "columns": ["SourceDeviceRegistrationId", "ProjectId"], "unique": false, "filter": null} |
| dbo.OfflineSyncBatches | CHECK | CK_OfflineSyncBatches_AttachedPayload | {"name": "CK_OfflineSyncBatches_AttachedPayload", "sql": "([AttachedPayloadJson] IS NULL AND [AttachedPayloadHash] IS NULL) OR ([AttachedPayloadJson] IS NOT NULL AND [AttachedPayloadHash] IS NOT NULL AND ISJSON([AttachedPayloadJson])=1 AND LEN([AttachedPayloadHash])=64 AND [AttachedPayloadHash] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%' AND DATALENGTH(CONVERT(varchar(max),[AttachedPayloadJson] COLLATE Latin1_General_100_BIN2_UTF8))<=16777216)"} |
| dbo.OfflineSyncBatches | CHECK | CK_OfflineSyncBatches_ContentHash | {"name": "CK_OfflineSyncBatches_ContentHash", "sql": "LEN([ContentHash])=64 AND [ContentHash] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'"} |
| dbo.OfflineSyncBatches | CHECK | CK_OfflineSyncBatches_Recipient | {"name": "CK_OfflineSyncBatches_Recipient", "sql": "([PackageId] IS NULL AND [GrantId] IS NULL AND [RecipientDeviceRegistrationId] IS NULL AND [RecipientSignature] IS NULL) OR ([PackageId] IS NOT NULL AND [GrantId] IS NOT NULL AND [RecipientDeviceRegistrationId] IS NOT NULL AND [RecipientSignature] IS NOT NULL)"} |
| dbo.OfflineSyncBatches | CHECK | CK_OfflineSyncBatches_SignedDescriptorJson | {"name": "CK_OfflineSyncBatches_SignedDescriptorJson", "sql": "ISJSON([SignedDescriptorJson])=1"} |
| dbo.OfflineTaskSnapshots | KEY | AK_OfflineTaskSnapshots_Id_ProjectId | {"name": "AK_OfflineTaskSnapshots_Id_ProjectId", "columns": ["Id", "ProjectId"]} |
| dbo.OfflineTaskSnapshots | KEY | PK_OfflineTaskSnapshots | {"name": "PK_OfflineTaskSnapshots", "columns": ["Id"]} |
| dbo.OfflineTaskSnapshots | INDEX | IX_OfflineTaskSnapshots_AssignmentId | {"name": "IX_OfflineTaskSnapshots_AssignmentId", "columns": ["AssignmentId"], "unique": false, "filter": null} |
| dbo.OfflineTaskSnapshots | INDEX | IX_OfflineTaskSnapshots_DeviceRegistrationId_ProjectId | {"name": "IX_OfflineTaskSnapshots_DeviceRegistrationId_ProjectId", "columns": ["DeviceRegistrationId", "ProjectId"], "unique": false, "filter": null} |
| dbo.OfflineTaskSnapshots | INDEX | IX_OfflineTaskSnapshots_OriginalActorId | {"name": "IX_OfflineTaskSnapshots_OriginalActorId", "columns": ["OriginalActorId"], "unique": false, "filter": null} |
| dbo.OfflineTaskSnapshots | INDEX | IX_OfflineTaskSnapshots_ProjectId_TaskId_AssignmentId_DeviceRegistrationId | {"name": "IX_OfflineTaskSnapshots_ProjectId_TaskId_AssignmentId_DeviceRegistrationId", "columns": ["ProjectId", "TaskId", "AssignmentId", "DeviceRegistrationId"], "unique": false, "filter": null} |
| dbo.OfflineTaskSnapshots | INDEX | IX_OfflineTaskSnapshots_TaskId | {"name": "IX_OfflineTaskSnapshots_TaskId", "columns": ["TaskId"], "unique": false, "filter": null} |
| dbo.OfflineTaskSnapshots | CHECK | CK_OfflineTaskSnapshots_AssignmentHash | {"name": "CK_OfflineTaskSnapshots_AssignmentHash", "sql": "LEN([AssignmentHash])=64 AND [AssignmentHash] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'"} |
| dbo.OfflineTaskSnapshots | CHECK | CK_OfflineTaskSnapshots_ContentHash | {"name": "CK_OfflineTaskSnapshots_ContentHash", "sql": "LEN([ContentHash])=64 AND [ContentHash] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'"} |
| dbo.OfflineTaskSnapshots | CHECK | CK_OfflineTaskSnapshots_SnapshotJson | {"name": "CK_OfflineTaskSnapshots_SnapshotJson", "sql": "ISJSON([SnapshotJson])=1"} |
| dbo.OutboxMessages | KEY | PK_OutboxMessages | {"name": "PK_OutboxMessages", "columns": ["Id"]} |
| dbo.OutboxMessages | INDEX | IX_OutboxMessages_CorrelationId | {"name": "IX_OutboxMessages_CorrelationId", "columns": ["CorrelationId"], "unique": false, "filter": null} |
| dbo.OutboxMessages | INDEX | IX_OutboxMessages_DeliveryStatus_NextAttempt | {"name": "IX_OutboxMessages_DeliveryStatus_NextAttempt", "columns": ["DeliveryStatus", "NextAttemptAtUtc"], "unique": false, "filter": null} |
| dbo.OutboxMessages | INDEX | IX_OutboxMessages_OccurredAtUtc | {"name": "IX_OutboxMessages_OccurredAtUtc", "columns": ["OccurredAtUtc"], "unique": false, "filter": null} |
| dbo.OutboxMessages | CHECK | CK_OutboxMessages_DeliveryAttemptCount | {"name": "CK_OutboxMessages_DeliveryAttemptCount", "sql": "[DeliveryAttemptCount] >= 0"} |
| dbo.OutboxMessages | CHECK | CK_OutboxMessages_DeliveryStatus | {"name": "CK_OutboxMessages_DeliveryStatus", "sql": "[DeliveryStatus] IN (1, 2, 3, 4)"} |
| dbo.OutboxMessages | CHECK | CK_OutboxMessages_PayloadJson_Json | {"name": "CK_OutboxMessages_PayloadJson_Json", "sql": "ISJSON([PayloadJson]) = 1"} |
| dbo.PasswordRecoveryRequests | KEY | PK_PasswordRecoveryRequests | {"name": "PK_PasswordRecoveryRequests", "columns": ["Id"]} |
| dbo.PasswordRecoveryRequests | INDEX | IX_PasswordRecoveryRequests_TargetUserId_RequestedAtUtc | {"name": "IX_PasswordRecoveryRequests_TargetUserId_RequestedAtUtc", "columns": ["TargetUserId", "RequestedAtUtc"], "unique": false, "filter": null} |
| dbo.PasswordResetLogs | KEY | PK_PasswordResetLogs | {"name": "PK_PasswordResetLogs", "columns": ["Id"]} |
| dbo.PasswordResetLogs | INDEX | IX_PasswordResetLogs_OccurredAt | {"name": "IX_PasswordResetLogs_OccurredAt", "columns": ["OccurredAt"], "unique": false, "filter": null} |
| dbo.PasswordResetLogs | INDEX | IX_PasswordResetLogs_PerformedByUserId | {"name": "IX_PasswordResetLogs_PerformedByUserId", "columns": ["PerformedByUserId"], "unique": false, "filter": null} |
| dbo.PasswordResetLogs | INDEX | IX_PasswordResetLogs_TargetUserId | {"name": "IX_PasswordResetLogs_TargetUserId", "columns": ["TargetUserId"], "unique": false, "filter": null} |
| dbo.PasswordResetLogs | CHECK | CK_PasswordResetLogs_Reason_SafeCode | {"name": "CK_PasswordResetLogs_Reason_SafeCode", "sql": "[Reason] IS NULL OR [Reason] IN ('ADMINISTRATOR_INITIATED', 'SELF_SERVICE_ACCOUNT_RECOVERY', 'REGISTRATION_APPROVED', 'SAFETY_POLICY_VIOLATION', 'NO_STATUS_CHANGE', 'ADMINISTRATIVE_LOCK', 'SECURITY_INCIDENT', 'ACCOUNT_REACTIVATED')"} |
| dbo.PasswordResetLogs | CHECK | CK_PasswordResetLogs_Result | {"name": "CK_PasswordResetLogs_Result", "sql": "[Result] IN (1, 2, 3)"} |
| dbo.PasswordResetLogs | CHECK | CK_PasswordResetLogs_Source_SafeCode | {"name": "CK_PasswordResetLogs_Source_SafeCode", "sql": "[Source] IN ('ADMIN_API', 'SELF_SERVICE', 'IDENTITY_SERVICE', 'COMPLIANCE_REVIEW', 'SYSTEM')"} |
| dbo.PavementLayoutRevisions | KEY | PK_PavementLayoutRevisions | {"name": "PK_PavementLayoutRevisions", "columns": ["Id"]} |
| dbo.PavementLayoutRevisions | INDEX | IX_PavementLayoutRevisions_CreatedBy | {"name": "IX_PavementLayoutRevisions_CreatedBy", "columns": ["CreatedBy"], "unique": false, "filter": null} |
| dbo.PavementLayoutRevisions | INDEX | IX_PavementLayoutRevisions_CrsProfileRevisionId_ProjectId | {"name": "IX_PavementLayoutRevisions_CrsProfileRevisionId_ProjectId", "columns": ["CrsProfileRevisionId", "ProjectId"], "unique": false, "filter": null} |
| dbo.PavementLayoutRevisions | INDEX | IX_PavementLayoutRevisions_ProjectId | {"name": "IX_PavementLayoutRevisions_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} |
| dbo.PavementLayoutRevisions | INDEX | IX_PavementLayoutRevisions_RouteVersionId_CreatedAt_Id | {"name": "IX_PavementLayoutRevisions_RouteVersionId_CreatedAt_Id", "columns": ["RouteVersionId", "CreatedAt", "Id"], "unique": false, "filter": null} |
| dbo.PavementLayoutRevisions | INDEX | IX_PavementLayoutRevisions_SegmentSetId | {"name": "IX_PavementLayoutRevisions_SegmentSetId", "columns": ["SegmentSetId"], "unique": false, "filter": null} |
| dbo.PavementLayoutRevisions | INDEX | IX_PavementLayoutRevisions_SourcePlanId | {"name": "IX_PavementLayoutRevisions_SourcePlanId", "columns": ["SourcePlanId"], "unique": false, "filter": null} |
| dbo.PavementLayoutRevisions | CHECK | CK_PavementLayoutRevisions_Json | {"name": "CK_PavementLayoutRevisions_Json", "sql": "ISJSON([DefinitionJson])=1 AND ISJSON([SnapshotJson])=1"} |
| dbo.PavementLayoutRevisions | CHECK | CK_PavementLayoutRevisions_Kind | {"name": "CK_PavementLayoutRevisions_Kind", "sql": "[Kind] IN ('PLANNED','AS_BUILT') AND (([Kind]='PLANNED' AND [SourcePlanId] IS NULL) OR ([Kind]='AS_BUILT' AND [SourcePlanId] IS NOT NULL))"} |
| dbo.PavementSourceFileReferences | KEY | PK_PavementSourceFileReferences | {"name": "PK_PavementSourceFileReferences", "columns": ["Id"]} |
| dbo.PavementSourceFileReferences | INDEX | IX_PavementSourceFileReferences_FileId | {"name": "IX_PavementSourceFileReferences_FileId", "columns": ["FileId"], "unique": false, "filter": null} |
| dbo.PavementSourceFileReferences | INDEX | IX_PavementSourceFileReferences_LayoutRevisionId_FileId | {"name": "IX_PavementSourceFileReferences_LayoutRevisionId_FileId", "columns": ["LayoutRevisionId", "FileId"], "unique": true, "filter": null} |
| dbo.PavementSourceFileReferences | CHECK | CK_PavementSourceFileReferences_Facts | {"name": "CK_PavementSourceFileReferences_Facts", "sql": "LEN([ContentChecksum])=64 AND ISJSON([CaptureFactsJson])=1"} |
| dbo.ProcessingAttempts | KEY | PK_ProcessingAttempts | {"name": "PK_ProcessingAttempts", "columns": ["Id"]} |
| dbo.ProcessingAttempts | INDEX | IX_ProcessingAttempts_JobId | {"name": "IX_ProcessingAttempts_JobId", "columns": ["ProcessingJobId"], "unique": false, "filter": null} |
| dbo.ProcessingAttempts | INDEX | UX_ProcessingAttempts_JobAttemptNo | {"name": "UX_ProcessingAttempts_JobAttemptNo", "columns": ["ProcessingJobId", "AttemptNo"], "unique": true, "filter": null} |
| dbo.ProcessingAttempts | CHECK | CK_ProcessingAttempts_AttemptNo | {"name": "CK_ProcessingAttempts_AttemptNo", "sql": "[AttemptNo] > 0"} |
| dbo.ProcessingAttempts | CHECK | CK_ProcessingAttempts_ErrorType | {"name": "CK_ProcessingAttempts_ErrorType", "sql": "[ErrorType] IS NULL OR [ErrorType] IN (1, 2, 3)"} |
| dbo.ProcessingAttempts | CHECK | CK_ProcessingAttempts_TimestampOrder | {"name": "CK_ProcessingAttempts_TimestampOrder", "sql": "[EndedAt] IS NULL OR [EndedAt] >= [StartedAt]"} |
| dbo.ProcessingBlocks | KEY | PK_ProcessingBlocks | {"name": "PK_ProcessingBlocks", "columns": ["Id"]} |
| dbo.ProcessingBlocks | INDEX | UX_ProcessingBlocks_DataVersionBlockNo | {"name": "UX_ProcessingBlocks_DataVersionBlockNo", "columns": ["SurveyDataVersionId", "BlockNo"], "unique": true, "filter": null} |
| dbo.ProcessingBlocks | CHECK | CK_ProcessingBlocks_BlockNo | {"name": "CK_ProcessingBlocks_BlockNo", "sql": "[BlockNo] > 0"} |
| dbo.ProcessingBlocks | CHECK | CK_ProcessingBlocks_RangeMetadata_JsonObject | {"name": "CK_ProcessingBlocks_RangeMetadata_JsonObject", "sql": "ISJSON([RangeMetadata]) = 1 AND LEFT(LTRIM([RangeMetadata]), 1) = '{'"} |
| dbo.ProcessingJobs | KEY | PK_ProcessingJobs | {"name": "PK_ProcessingJobs", "columns": ["Id"]} |
| dbo.ProcessingJobs | INDEX | IX_ProcessingJobs_Block | {"name": "IX_ProcessingJobs_Block", "columns": ["ProcessingBlockId"], "unique": false, "filter": null} |
| dbo.ProcessingJobs | INDEX | IX_ProcessingJobs_ModelVersionId | {"name": "IX_ProcessingJobs_ModelVersionId", "columns": ["ModelVersionId"], "unique": false, "filter": null} |
| dbo.ProcessingJobs | INDEX | IX_ProcessingJobs_ProjectStatus | {"name": "IX_ProcessingJobs_ProjectStatus", "columns": ["ProjectId", "Status"], "unique": false, "filter": null} |
| dbo.ProcessingJobs | CHECK | CK_ProcessingJobs_Status | {"name": "CK_ProcessingJobs_Status", "sql": "[Status] IN (1, 2, 3, 4, 5, 6)"} |
| dbo.ProcessingJobs | CHECK | CK_ProcessingJobs_TimestampOrder | {"name": "CK_ProcessingJobs_TimestampOrder", "sql": "[CompletedAt] IS NULL OR [StartedAt] IS NULL OR [CompletedAt] >= [StartedAt]"} |
| dbo.ProjectLifecycleHistory | KEY | AK_ProjectLifecycleHistory_Id_ProjectId | {"name": "AK_ProjectLifecycleHistory_Id_ProjectId", "columns": ["Id", "ProjectId"]} |
| dbo.ProjectLifecycleHistory | KEY | PK_ProjectLifecycleHistory | {"name": "PK_ProjectLifecycleHistory", "columns": ["Id"]} |
| dbo.ProjectLifecycleHistory | INDEX | IX_ProjectLifecycleHistory_ActorId | {"name": "IX_ProjectLifecycleHistory_ActorId", "columns": ["ActorId"], "unique": false, "filter": null} |
| dbo.ProjectLifecycleHistory | INDEX | IX_ProjectLifecycleHistory_DefectId | {"name": "IX_ProjectLifecycleHistory_DefectId", "columns": ["DefectId"], "unique": false, "filter": null} |
| dbo.ProjectLifecycleHistory | INDEX | IX_ProjectLifecycleHistory_LinkedDefectId | {"name": "IX_ProjectLifecycleHistory_LinkedDefectId", "columns": ["LinkedDefectId"], "unique": false, "filter": null} |
| dbo.ProjectLifecycleHistory | INDEX | IX_ProjectLifecycleHistory_ObligationId | {"name": "IX_ProjectLifecycleHistory_ObligationId", "columns": ["ObligationId"], "unique": false, "filter": null} |
| dbo.ProjectLifecycleHistory | INDEX | IX_ProjectLifecycleHistory_OperationalClosureId_ProjectId | {"name": "IX_ProjectLifecycleHistory_OperationalClosureId_ProjectId", "columns": ["OperationalClosureId", "ProjectId"], "unique": false, "filter": null} |
| dbo.ProjectLifecycleHistory | INDEX | IX_ProjectLifecycleHistory_ProjectId | {"name": "IX_ProjectLifecycleHistory_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} |
| dbo.ProjectLifecycleHistory | INDEX | IX_ProjectLifecycleHistory_ReceiverId | {"name": "IX_ProjectLifecycleHistory_ReceiverId", "columns": ["ReceiverId"], "unique": false, "filter": null} |
| dbo.ProjectLifecycleHistory | CHECK | CK_ProjectLifecycleHistory_Facts | {"name": "CK_ProjectLifecycleHistory_Facts", "sql": "ISJSON([FactsJson])=1"} |
| dbo.ProjectLifecycleHistory | CHECK | CK_ProjectLifecycleHistory_Source | {"name": "CK_ProjectLifecycleHistory_Source", "sql": "[Kind] IN (1,2,3,4,5,6,7,8) AND [SourceDisposition] IN ('CANDIDATE','TARGET_CONFIRMED') AND ([SourceDisposition]='CANDIDATE' OR LEN([AuthoritySourceReference])>0)"} |
| dbo.ProjectMembers | KEY | PK_ProjectMembers | {"name": "PK_ProjectMembers", "columns": ["Id"]} |
| dbo.ProjectMembers | INDEX | IX_ProjectMembers_ProjectId_UserId | {"name": "IX_ProjectMembers_ProjectId_UserId", "columns": ["ProjectId", "UserId"], "unique": false, "filter": null} |
| dbo.ProjectMembers | INDEX | IX_ProjectMembers_RoleCode | {"name": "IX_ProjectMembers_RoleCode", "columns": ["RoleCode"], "unique": false, "filter": null} |
| dbo.ProjectMembers | INDEX | IX_ProjectMembers_UserId | {"name": "IX_ProjectMembers_UserId", "columns": ["UserId"], "unique": false, "filter": null} |
| dbo.ProjectMembers | INDEX | UX_ProjectMembers_ActivePrimaryProjectManager | {"name": "UX_ProjectMembers_ActivePrimaryProjectManager", "columns": ["ProjectId"], "unique": true, "filter": "[IsPrimary] = 1 AND [Status] = 1"} |
| dbo.ProjectMembers | CHECK | CK_ProjectMembers_EffectiveDateRange | {"name": "CK_ProjectMembers_EffectiveDateRange", "sql": "[ValidTo] IS NULL OR [ValidTo] >= [ValidFrom]"} |
| dbo.ProjectMembers | CHECK | CK_ProjectMembers_PrimaryRole | {"name": "CK_ProjectMembers_PrimaryRole", "sql": "[IsPrimary] = 0 OR [RoleCode] = 'PM'"} |
| dbo.ProjectMembers | CHECK | CK_ProjectMembers_Status | {"name": "CK_ProjectMembers_Status", "sql": "[Status] IN (1, 2)"} |
| dbo.Projects | KEY | PK_Projects | {"name": "PK_Projects", "columns": ["Id"]} |
| dbo.Projects | INDEX | UX_Projects_ProjectCode | {"name": "UX_Projects_ProjectCode", "columns": ["ProjectCode"], "unique": true, "filter": null} |
| dbo.Projects | CHECK | CK_Projects_DateRange | {"name": "CK_Projects_DateRange", "sql": "[EndDate] IS NULL OR [StartDate] IS NULL OR [EndDate] >= [StartDate]"} |
| dbo.Projects | CHECK | CK_Projects_EngineeringUtmSrid | {"name": "CK_Projects_EngineeringUtmSrid", "sql": "[EngineeringUtmSrid] IS NULL OR [EngineeringUtmSrid] IN (32648, 32649)"} |
| dbo.Projects | CHECK | CK_Projects_Status | {"name": "CK_Projects_Status", "sql": "[Status] IN (1, 2, 3)"} |
| dbo.QualityChecks | KEY | PK_QualityChecks | {"name": "PK_QualityChecks", "columns": ["Id"]} |
| dbo.QualityChecks | INDEX | IX_QualityChecks_InitiatedByUserId | {"name": "IX_QualityChecks_InitiatedByUserId", "columns": ["InitiatedByUserId"], "unique": false, "filter": null} |
| dbo.QualityChecks | INDEX | IX_QualityChecks_SurveyDataVersionId | {"name": "IX_QualityChecks_SurveyDataVersionId", "columns": ["SurveyDataVersionId"], "unique": false, "filter": null} |
| dbo.QualityChecks | INDEX | IX_QualityChecks_SurveyFileId | {"name": "IX_QualityChecks_SurveyFileId", "columns": ["SurveyFileId"], "unique": false, "filter": null} |
| dbo.QualityChecks | CHECK | CK_QualityChecks_CheckedBy | {"name": "CK_QualityChecks_CheckedBy", "sql": "[CheckedBy] IN (1, 2)"} |
| dbo.QualityChecks | CHECK | CK_QualityChecks_CheckType | {"name": "CK_QualityChecks_CheckType", "sql": "[CheckType] IN (1, 2, 3, 4, 5, 6, 7, 8, 9)"} |
| dbo.QualityChecks | CHECK | CK_QualityChecks_ExactlyOneTarget | {"name": "CK_QualityChecks_ExactlyOneTarget", "sql": "([Scope] = 1 AND [SurveyFileId] IS NOT NULL AND [SurveyDataVersionId] IS NULL) OR ([Scope] = 2 AND [SurveyFileId] IS NULL AND [SurveyDataVersionId] IS NOT NULL)"} |
| dbo.QualityChecks | CHECK | CK_QualityChecks_ExecutionStage | {"name": "CK_QualityChecks_ExecutionStage", "sql": "[ExecutionStage] IN (1, 2)"} |
| dbo.QualityChecks | CHECK | CK_QualityChecks_MeasuredValue_Json | {"name": "CK_QualityChecks_MeasuredValue_Json", "sql": "[MeasuredValue] IS NULL OR ISJSON([MeasuredValue]) = 1"} |
| dbo.QualityChecks | CHECK | CK_QualityChecks_Scope | {"name": "CK_QualityChecks_Scope", "sql": "[Scope] IN (1, 2)"} |
| dbo.QualityChecks | CHECK | CK_QualityChecks_StageActor | {"name": "CK_QualityChecks_StageActor", "sql": "([ExecutionStage] = 1 AND [CheckedBy] = 1) OR ([ExecutionStage] = 2 AND [CheckedBy] = 2 AND [InitiatedByUserId] IS NULL)"} |
| dbo.QualityChecks | CHECK | CK_QualityChecks_Status | {"name": "CK_QualityChecks_Status", "sql": "[Status] IN (1, 2, 3, 4)"} |
| dbo.QualityChecks | CHECK | CK_QualityChecks_Threshold_Json | {"name": "CK_QualityChecks_Threshold_Json", "sql": "[Threshold] IS NULL OR ISJSON([Threshold]) = 1"} |
| dbo.RefreshTokens | KEY | PK_RefreshTokens | {"name": "PK_RefreshTokens", "columns": ["Id"]} |
| dbo.RefreshTokens | INDEX | IX_RefreshTokens_SessionId | {"name": "IX_RefreshTokens_SessionId", "columns": ["SessionId"], "unique": false, "filter": null} |
| dbo.RefreshTokens | INDEX | UX_RefreshTokens_TokenHash | {"name": "UX_RefreshTokens_TokenHash", "columns": ["TokenHash"], "unique": true, "filter": null} |
| dbo.RefreshTokens | CHECK | CK_RefreshTokens_TokenHash_NotEmpty | {"name": "CK_RefreshTokens_TokenHash_NotEmpty", "sql": "LEN(LTRIM(RTRIM([TokenHash]))) >= 32"} |
| dbo.RepairActualScopes | KEY | PK_RepairActualScopes | {"name": "PK_RepairActualScopes", "columns": ["Id"]} |
| dbo.RepairActualScopes | INDEX | IX_RepairActualScopes_ObligationId | {"name": "IX_RepairActualScopes_ObligationId", "columns": ["ObligationId"], "unique": true, "filter": null} |
| dbo.RepairActualScopes | INDEX | IX_RepairActualScopes_PhysicalRoadId | {"name": "IX_RepairActualScopes_PhysicalRoadId", "columns": ["PhysicalRoadId"], "unique": false, "filter": null} |
| dbo.RepairAssessmentEvidence | KEY | PK_RepairAssessmentEvidence | {"name": "PK_RepairAssessmentEvidence", "columns": ["Id"]} |
| dbo.RepairAssessmentEvidence | INDEX | IX_RepairAssessmentEvidence_ActualUploaderId | {"name": "IX_RepairAssessmentEvidence_ActualUploaderId", "columns": ["ActualUploaderId"], "unique": false, "filter": null} |
| dbo.RepairAssessmentEvidence | INDEX | IX_RepairAssessmentEvidence_AssessmentId_CaptureOriginId | {"name": "IX_RepairAssessmentEvidence_AssessmentId_CaptureOriginId", "columns": ["AssessmentId", "CaptureOriginId"], "unique": true, "filter": null} |
| dbo.RepairAssessmentEvidence | INDEX | IX_RepairAssessmentEvidence_FileId | {"name": "IX_RepairAssessmentEvidence_FileId", "columns": ["FileId"], "unique": false, "filter": null} |
| dbo.RepairAssessmentEvidence | INDEX | IX_RepairAssessmentEvidence_OriginalActorId | {"name": "IX_RepairAssessmentEvidence_OriginalActorId", "columns": ["OriginalActorId"], "unique": false, "filter": null} |
| dbo.RepairAssessmentEvidence | INDEX | IX_RepairAssessmentEvidence_ReuseDecisionId | {"name": "IX_RepairAssessmentEvidence_ReuseDecisionId", "columns": ["ReuseDecisionId"], "unique": false, "filter": null} |
| dbo.RepairAssessmentMeasurements | KEY | PK_RepairAssessmentMeasurements | {"name": "PK_RepairAssessmentMeasurements", "columns": ["AssessmentId", "MeasurementId"]} |
| dbo.RepairAssessmentMeasurements | INDEX | IX_RepairAssessmentMeasurements_MeasurementId | {"name": "IX_RepairAssessmentMeasurements_MeasurementId", "columns": ["MeasurementId"], "unique": false, "filter": null} |
| dbo.RepairAttemptEvidence | KEY | PK_RepairAttemptEvidence | {"name": "PK_RepairAttemptEvidence", "columns": ["AttemptId", "FileId", "Purpose"]} |
| dbo.RepairAttemptEvidence | INDEX | IX_RepairAttemptEvidence_FileId | {"name": "IX_RepairAttemptEvidence_FileId", "columns": ["FileId"], "unique": false, "filter": null} |
| dbo.RepairAttemptReviews | KEY | PK_RepairAttemptReviews | {"name": "PK_RepairAttemptReviews", "columns": ["Id"]} |
| dbo.RepairAttemptReviews | INDEX | IX_RepairAttemptReviews_ActorId | {"name": "IX_RepairAttemptReviews_ActorId", "columns": ["ActorId"], "unique": false, "filter": null} |
| dbo.RepairAttemptReviews | INDEX | IX_RepairAttemptReviews_AttemptId | {"name": "IX_RepairAttemptReviews_AttemptId", "columns": ["AttemptId"], "unique": false, "filter": null} |
| dbo.RepairAttemptReviews | INDEX | IX_RepairAttemptReviews_BindingId | {"name": "IX_RepairAttemptReviews_BindingId", "columns": ["BindingId"], "unique": false, "filter": null} |
| dbo.RepairAttemptReviews | INDEX | IX_RepairAttemptReviews_IntakeLinkId | {"name": "IX_RepairAttemptReviews_IntakeLinkId", "columns": ["IntakeLinkId"], "unique": false, "filter": null} |
| dbo.RepairAttemptReviews | INDEX | IX_RepairAttemptReviews_ItemId | {"name": "IX_RepairAttemptReviews_ItemId", "columns": ["ItemId"], "unique": false, "filter": null} |
| dbo.RepairAttemptReviews | INDEX | IX_RepairAttemptReviews_ProjectId | {"name": "IX_RepairAttemptReviews_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} |
| dbo.RepairAttemptReviews | INDEX | IX_RepairAttemptReviews_SubmissionId | {"name": "IX_RepairAttemptReviews_SubmissionId", "columns": ["SubmissionId"], "unique": false, "filter": null} |
| dbo.RepairAttempts | KEY | PK_RepairAttempts | {"name": "PK_RepairAttempts", "columns": ["Id"]} |
| dbo.RepairAttempts | INDEX | IX_RepairAttempts_AssignmentId | {"name": "IX_RepairAttempts_AssignmentId", "columns": ["AssignmentId"], "unique": false, "filter": null} |
| dbo.RepairAttempts | INDEX | IX_RepairAttempts_CrewId | {"name": "IX_RepairAttempts_CrewId", "columns": ["CrewId"], "unique": false, "filter": null} |
| dbo.RepairAttempts | INDEX | IX_RepairAttempts_DefectId | {"name": "IX_RepairAttempts_DefectId", "columns": ["DefectId"], "unique": false, "filter": null} |
| dbo.RepairAttempts | INDEX | IX_RepairAttempts_ItemId | {"name": "IX_RepairAttempts_ItemId", "columns": ["ItemId"], "unique": false, "filter": null} |
| dbo.RepairAttempts | INDEX | IX_RepairAttempts_ObligationId | {"name": "IX_RepairAttempts_ObligationId", "columns": ["ObligationId"], "unique": false, "filter": null} |
| dbo.RepairAttempts | INDEX | IX_RepairAttempts_ProjectId_OriginId | {"name": "IX_RepairAttempts_ProjectId_OriginId", "columns": ["ProjectId", "OriginId"], "unique": true, "filter": null} |
| dbo.RepairAttempts | INDEX | IX_RepairAttempts_TaskId | {"name": "IX_RepairAttempts_TaskId", "columns": ["TaskId"], "unique": false, "filter": null} |
| dbo.RepairAttemptSubmissionLinks | KEY | PK_RepairAttemptSubmissionLinks | {"name": "PK_RepairAttemptSubmissionLinks", "columns": ["Id"]} |
| dbo.RepairAttemptSubmissionLinks | INDEX | IX_RepairAttemptSubmissionLinks_AttemptId | {"name": "IX_RepairAttemptSubmissionLinks_AttemptId", "columns": ["AttemptId"], "unique": false, "filter": null} |
| dbo.RepairAttemptSubmissionLinks | INDEX | IX_RepairAttemptSubmissionLinks_BindingId | {"name": "IX_RepairAttemptSubmissionLinks_BindingId", "columns": ["BindingId"], "unique": false, "filter": null} |
| dbo.RepairAttemptSubmissionLinks | INDEX | IX_RepairAttemptSubmissionLinks_ExecutionFinishId | {"name": "IX_RepairAttemptSubmissionLinks_ExecutionFinishId", "columns": ["ExecutionFinishId"], "unique": false, "filter": null} |
| dbo.RepairAttemptSubmissionLinks | INDEX | IX_RepairAttemptSubmissionLinks_FormalRootSubmissionId | {"name": "IX_RepairAttemptSubmissionLinks_FormalRootSubmissionId", "columns": ["FormalRootSubmissionId"], "unique": false, "filter": null} |
| dbo.RepairAttemptSubmissionLinks | INDEX | IX_RepairAttemptSubmissionLinks_ItemId | {"name": "IX_RepairAttemptSubmissionLinks_ItemId", "columns": ["ItemId"], "unique": false, "filter": null} |
| dbo.RepairAttemptSubmissionLinks | INDEX | IX_RepairAttemptSubmissionLinks_PreviousLinkId | {"name": "IX_RepairAttemptSubmissionLinks_PreviousLinkId", "columns": ["PreviousLinkId"], "unique": true, "filter": "[PreviousLinkId] IS NOT NULL"} |
| dbo.RepairAttemptSubmissionLinks | INDEX | IX_RepairAttemptSubmissionLinks_ProjectId | {"name": "IX_RepairAttemptSubmissionLinks_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} |
| dbo.RepairAttemptSubmissionLinks | INDEX | IX_RepairAttemptSubmissionLinks_ReviewClockId | {"name": "IX_RepairAttemptSubmissionLinks_ReviewClockId", "columns": ["ReviewClockId"], "unique": false, "filter": null} |
| dbo.RepairAttemptSubmissionLinks | INDEX | IX_RepairAttemptSubmissionLinks_SubmissionId | {"name": "IX_RepairAttemptSubmissionLinks_SubmissionId", "columns": ["SubmissionId"], "unique": true, "filter": null} |
| dbo.RepairCorrectionEvidence | KEY | PK_RepairCorrectionEvidence | {"name": "PK_RepairCorrectionEvidence", "columns": ["DecisionId", "FileId"]} |
| dbo.RepairCorrectionEvidence | INDEX | IX_RepairCorrectionEvidence_FileId | {"name": "IX_RepairCorrectionEvidence_FileId", "columns": ["FileId"], "unique": false, "filter": null} |
| dbo.RepairDangerAcknowledgements | KEY | PK_RepairDangerAcknowledgements | {"name": "PK_RepairDangerAcknowledgements", "columns": ["Id"]} |
| dbo.RepairDangerAcknowledgements | INDEX | IX_RepairDangerAcknowledgements_ActorId | {"name": "IX_RepairDangerAcknowledgements_ActorId", "columns": ["ActorId"], "unique": false, "filter": null} |
| dbo.RepairDangerAcknowledgements | INDEX | IX_RepairDangerAcknowledgements_MonitoringId | {"name": "IX_RepairDangerAcknowledgements_MonitoringId", "columns": ["MonitoringId"], "unique": false, "filter": null} |
| dbo.RepairDangerAcknowledgements | INDEX | IX_RepairDangerAcknowledgements_WarningId | {"name": "IX_RepairDangerAcknowledgements_WarningId", "columns": ["WarningId"], "unique": true, "filter": null} |
| dbo.RepairDangerWarnings | KEY | PK_RepairDangerWarnings | {"name": "PK_RepairDangerWarnings", "columns": ["Id"]} |
| dbo.RepairDangerWarnings | INDEX | IX_RepairDangerWarnings_MonitoringId_SourceId | {"name": "IX_RepairDangerWarnings_MonitoringId_SourceId", "columns": ["MonitoringId", "SourceId"], "unique": true, "filter": null} |
| dbo.RepairDangerWarnings | INDEX | IX_RepairDangerWarnings_ResponsibleActorId | {"name": "IX_RepairDangerWarnings_ResponsibleActorId", "columns": ["ResponsibleActorId"], "unique": false, "filter": null} |
| dbo.RepairDecisions | KEY | PK_RepairDecisions | {"name": "PK_RepairDecisions", "columns": ["Id"]} |
| dbo.RepairDecisions | INDEX | IX_RepairDecisions_ActorId | {"name": "IX_RepairDecisions_ActorId", "columns": ["ActorId"], "unique": false, "filter": null} |
| dbo.RepairDecisions | INDEX | IX_RepairDecisions_DefectId | {"name": "IX_RepairDecisions_DefectId", "columns": ["DefectId"], "unique": false, "filter": null} |
| dbo.RepairDecisions | INDEX | IX_RepairDecisions_ItemId | {"name": "IX_RepairDecisions_ItemId", "columns": ["ItemId"], "unique": false, "filter": null} |
| dbo.RepairDecisions | INDEX | IX_RepairDecisions_ObligationId | {"name": "IX_RepairDecisions_ObligationId", "columns": ["ObligationId"], "unique": false, "filter": null} |
| dbo.RepairDecisions | INDEX | IX_RepairDecisions_PreviousObligationHeadDecisionId | {"name": "IX_RepairDecisions_PreviousObligationHeadDecisionId", "columns": ["PreviousObligationHeadDecisionId"], "unique": false, "filter": null} |
| dbo.RepairDecisions | INDEX | IX_RepairDecisions_SupersedesDecisionId | {"name": "IX_RepairDecisions_SupersedesDecisionId", "columns": ["SupersedesDecisionId"], "unique": true, "filter": "[SupersedesDecisionId] IS NOT NULL"} |
| dbo.RepairEligibilityAssessments | KEY | PK_RepairEligibilityAssessments | {"name": "PK_RepairEligibilityAssessments", "columns": ["Id"]} |
| dbo.RepairEligibilityAssessments | INDEX | IX_RepairEligibilityAssessments_AssessmentId | {"name": "IX_RepairEligibilityAssessments_AssessmentId", "columns": ["AssessmentId"], "unique": false, "filter": null} |
| dbo.RepairEligibilityAssessments | INDEX | IX_RepairEligibilityAssessments_BindingId | {"name": "IX_RepairEligibilityAssessments_BindingId", "columns": ["BindingId"], "unique": false, "filter": null} |
| dbo.RepairEligibilityAssessments | INDEX | IX_RepairEligibilityAssessments_ItemId | {"name": "IX_RepairEligibilityAssessments_ItemId", "columns": ["ItemId"], "unique": false, "filter": null} |
| dbo.RepairEligibilityAssessments | INDEX | IX_RepairEligibilityAssessments_PolicyRevisionId | {"name": "IX_RepairEligibilityAssessments_PolicyRevisionId", "columns": ["PolicyRevisionId"], "unique": false, "filter": null} |
| dbo.RepairEligibilityAssessments | INDEX | IX_RepairEligibilityAssessments_ProjectId | {"name": "IX_RepairEligibilityAssessments_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} |
| dbo.RepairEligibilityAssessments | INDEX | IX_RepairEligibilityAssessments_RoadSectionId | {"name": "IX_RepairEligibilityAssessments_RoadSectionId", "columns": ["RoadSectionId"], "unique": false, "filter": null} |
| dbo.RepairEligibilityHandoverSources | KEY | PK_RepairEligibilityHandoverSources | {"name": "PK_RepairEligibilityHandoverSources", "columns": ["EligibilityAssessmentId", "HandoverDocumentId"]} |
| dbo.RepairEligibilityHandoverSources | INDEX | IX_RepairEligibilityHandoverSources_FileId | {"name": "IX_RepairEligibilityHandoverSources_FileId", "columns": ["FileId"], "unique": false, "filter": null} |
| dbo.RepairEligibilityHandoverSources | INDEX | IX_RepairEligibilityHandoverSources_HandoverDocumentId | {"name": "IX_RepairEligibilityHandoverSources_HandoverDocumentId", "columns": ["HandoverDocumentId"], "unique": false, "filter": null} |
| dbo.RepairEligibilityWarrantySources | KEY | PK_RepairEligibilityWarrantySources | {"name": "PK_RepairEligibilityWarrantySources", "columns": ["EligibilityAssessmentId", "WarrantyId"]} |
| dbo.RepairEligibilityWarrantySources | INDEX | IX_RepairEligibilityWarrantySources_SourceDocumentId | {"name": "IX_RepairEligibilityWarrantySources_SourceDocumentId", "columns": ["SourceDocumentId"], "unique": false, "filter": null} |
| dbo.RepairEligibilityWarrantySources | INDEX | IX_RepairEligibilityWarrantySources_WarrantyId | {"name": "IX_RepairEligibilityWarrantySources_WarrantyId", "columns": ["WarrantyId"], "unique": false, "filter": null} |
| dbo.RepairExecutionAuthorizations | KEY | PK_RepairExecutionAuthorizations | {"name": "PK_RepairExecutionAuthorizations", "columns": ["Id"]} |
| dbo.RepairExecutionAuthorizations | INDEX | IX_RepairExecutionAuthorizations_AssignmentId | {"name": "IX_RepairExecutionAuthorizations_AssignmentId", "columns": ["AssignmentId"], "unique": false, "filter": null} |
| dbo.RepairExecutionAuthorizations | INDEX | IX_RepairExecutionAuthorizations_CrewId | {"name": "IX_RepairExecutionAuthorizations_CrewId", "columns": ["CrewId"], "unique": false, "filter": null} |
| dbo.RepairExecutionAuthorizations | INDEX | IX_RepairExecutionAuthorizations_DefectId | {"name": "IX_RepairExecutionAuthorizations_DefectId", "columns": ["DefectId"], "unique": false, "filter": null} |
| dbo.RepairExecutionAuthorizations | INDEX | IX_RepairExecutionAuthorizations_IssuedBy | {"name": "IX_RepairExecutionAuthorizations_IssuedBy", "columns": ["IssuedBy"], "unique": false, "filter": null} |
| dbo.RepairExecutionAuthorizations | INDEX | IX_RepairExecutionAuthorizations_PolicyRevisionId | {"name": "IX_RepairExecutionAuthorizations_PolicyRevisionId", "columns": ["PolicyRevisionId"], "unique": false, "filter": null} |
| dbo.RepairExecutionAuthorizations | INDEX | IX_RepairExecutionAuthorizations_ProjectId | {"name": "IX_RepairExecutionAuthorizations_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} |
| dbo.RepairExecutionAuthorizations | INDEX | IX_RepairExecutionAuthorizations_TaskId | {"name": "IX_RepairExecutionAuthorizations_TaskId", "columns": ["TaskId"], "unique": true, "filter": null} |
| dbo.RepairExecutionFinishes | KEY | PK_RepairExecutionFinishes | {"name": "PK_RepairExecutionFinishes", "columns": ["Id"]} |
| dbo.RepairExecutionFinishes | INDEX | IX_RepairExecutionFinishes_BindingId | {"name": "IX_RepairExecutionFinishes_BindingId", "columns": ["BindingId"], "unique": false, "filter": null} |
| dbo.RepairExecutionFinishes | INDEX | IX_RepairExecutionFinishes_ExecutionStartId | {"name": "IX_RepairExecutionFinishes_ExecutionStartId", "columns": ["ExecutionStartId"], "unique": true, "filter": null} |
| dbo.RepairExecutionFinishes | INDEX | IX_RepairExecutionFinishes_ItemId | {"name": "IX_RepairExecutionFinishes_ItemId", "columns": ["ItemId"], "unique": false, "filter": null} |
| dbo.RepairExecutionFinishes | INDEX | IX_RepairExecutionFinishes_OperationOriginId | {"name": "IX_RepairExecutionFinishes_OperationOriginId", "columns": ["OperationOriginId"], "unique": false, "filter": null} |
| dbo.RepairExecutionFinishes | INDEX | IX_RepairExecutionFinishes_OriginalActorId | {"name": "IX_RepairExecutionFinishes_OriginalActorId", "columns": ["OriginalActorId"], "unique": false, "filter": null} |
| dbo.RepairExecutionFinishes | INDEX | IX_RepairExecutionFinishes_ProjectId_OriginId | {"name": "IX_RepairExecutionFinishes_ProjectId_OriginId", "columns": ["ProjectId", "OriginId"], "unique": true, "filter": null} |
| dbo.RepairExecutionStarts | KEY | PK_RepairExecutionStarts | {"name": "PK_RepairExecutionStarts", "columns": ["Id"]} |
| dbo.RepairExecutionStarts | INDEX | IX_RepairExecutionStarts_AssessmentId | {"name": "IX_RepairExecutionStarts_AssessmentId", "columns": ["AssessmentId"], "unique": false, "filter": null} |
| dbo.RepairExecutionStarts | INDEX | IX_RepairExecutionStarts_BindingId | {"name": "IX_RepairExecutionStarts_BindingId", "columns": ["BindingId"], "unique": false, "filter": null} |
| dbo.RepairExecutionStarts | INDEX | IX_RepairExecutionStarts_EligibilityAssessmentId | {"name": "IX_RepairExecutionStarts_EligibilityAssessmentId", "columns": ["EligibilityAssessmentId"], "unique": false, "filter": null} |
| dbo.RepairExecutionStarts | INDEX | IX_RepairExecutionStarts_FirstStartId | {"name": "IX_RepairExecutionStarts_FirstStartId", "columns": ["FirstStartId"], "unique": false, "filter": null} |
| dbo.RepairExecutionStarts | INDEX | IX_RepairExecutionStarts_ItemId | {"name": "IX_RepairExecutionStarts_ItemId", "columns": ["ItemId"], "unique": false, "filter": null} |
| dbo.RepairExecutionStarts | INDEX | IX_RepairExecutionStarts_OperationOriginId | {"name": "IX_RepairExecutionStarts_OperationOriginId", "columns": ["OperationOriginId"], "unique": false, "filter": null} |
| dbo.RepairExecutionStarts | INDEX | IX_RepairExecutionStarts_OriginalActorId | {"name": "IX_RepairExecutionStarts_OriginalActorId", "columns": ["OriginalActorId"], "unique": false, "filter": null} |
| dbo.RepairExecutionStarts | INDEX | IX_RepairExecutionStarts_ProjectId_OriginId | {"name": "IX_RepairExecutionStarts_ProjectId_OriginId", "columns": ["ProjectId", "OriginId"], "unique": true, "filter": null} |
| dbo.RepairFieldTaskBindings | KEY | PK_RepairFieldTaskBindings | {"name": "PK_RepairFieldTaskBindings", "columns": ["Id"]} |
| dbo.RepairFieldTaskBindings | INDEX | IX_RepairFieldTaskBindings_AssignedBy | {"name": "IX_RepairFieldTaskBindings_AssignedBy", "columns": ["AssignedBy"], "unique": false, "filter": null} |
| dbo.RepairFieldTaskBindings | INDEX | IX_RepairFieldTaskBindings_AssignmentId | {"name": "IX_RepairFieldTaskBindings_AssignmentId", "columns": ["AssignmentId"], "unique": true, "filter": null} |
| dbo.RepairFieldTaskBindings | INDEX | IX_RepairFieldTaskBindings_AuthorizationId | {"name": "IX_RepairFieldTaskBindings_AuthorizationId", "columns": ["AuthorizationId"], "unique": false, "filter": null} |
| dbo.RepairFieldTaskBindings | INDEX | IX_RepairFieldTaskBindings_CrewId | {"name": "IX_RepairFieldTaskBindings_CrewId", "columns": ["CrewId"], "unique": false, "filter": null} |
| dbo.RepairFieldTaskBindings | INDEX | IX_RepairFieldTaskBindings_CrsProfileRevisionId | {"name": "IX_RepairFieldTaskBindings_CrsProfileRevisionId", "columns": ["CrsProfileRevisionId"], "unique": false, "filter": null} |
| dbo.RepairFieldTaskBindings | INDEX | IX_RepairFieldTaskBindings_DefectId | {"name": "IX_RepairFieldTaskBindings_DefectId", "columns": ["DefectId"], "unique": false, "filter": null} |
| dbo.RepairFieldTaskBindings | INDEX | IX_RepairFieldTaskBindings_ItemId | {"name": "IX_RepairFieldTaskBindings_ItemId", "columns": ["ItemId"], "unique": false, "filter": null} |
| dbo.RepairFieldTaskBindings | INDEX | IX_RepairFieldTaskBindings_LayoutRevisionId | {"name": "IX_RepairFieldTaskBindings_LayoutRevisionId", "columns": ["LayoutRevisionId"], "unique": false, "filter": null} |
| dbo.RepairFieldTaskBindings | INDEX | IX_RepairFieldTaskBindings_MapPublicationId | {"name": "IX_RepairFieldTaskBindings_MapPublicationId", "columns": ["MapPublicationId"], "unique": false, "filter": null} |
| dbo.RepairFieldTaskBindings | INDEX | IX_RepairFieldTaskBindings_ObligationId | {"name": "IX_RepairFieldTaskBindings_ObligationId", "columns": ["ObligationId"], "unique": false, "filter": null} |
| dbo.RepairFieldTaskBindings | INDEX | IX_RepairFieldTaskBindings_PolicyRevisionId | {"name": "IX_RepairFieldTaskBindings_PolicyRevisionId", "columns": ["PolicyRevisionId"], "unique": false, "filter": null} |
| dbo.RepairFieldTaskBindings | INDEX | IX_RepairFieldTaskBindings_ProjectId | {"name": "IX_RepairFieldTaskBindings_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} |
| dbo.RepairFieldTaskBindings | INDEX | IX_RepairFieldTaskBindings_RouteVersionId | {"name": "IX_RepairFieldTaskBindings_RouteVersionId", "columns": ["RouteVersionId"], "unique": false, "filter": null} |
| dbo.RepairFieldTaskBindings | INDEX | IX_RepairFieldTaskBindings_SegmentSetId | {"name": "IX_RepairFieldTaskBindings_SegmentSetId", "columns": ["SegmentSetId"], "unique": false, "filter": null} |
| dbo.RepairFieldTaskBindings | INDEX | IX_RepairFieldTaskBindings_TaskId | {"name": "IX_RepairFieldTaskBindings_TaskId", "columns": ["TaskId"], "unique": true, "filter": null} |
| dbo.RepairItemLifecycleEvents | KEY | PK_RepairItemLifecycleEvents | {"name": "PK_RepairItemLifecycleEvents", "columns": ["Id"]} |
| dbo.RepairItemLifecycleEvents | INDEX | IX_RepairItemLifecycleEvents_ActorId | {"name": "IX_RepairItemLifecycleEvents_ActorId", "columns": ["ActorId"], "unique": false, "filter": null} |
| dbo.RepairItemLifecycleEvents | INDEX | IX_RepairItemLifecycleEvents_AttemptId | {"name": "IX_RepairItemLifecycleEvents_AttemptId", "columns": ["AttemptId"], "unique": false, "filter": null} |
| dbo.RepairItemLifecycleEvents | INDEX | IX_RepairItemLifecycleEvents_BindingId | {"name": "IX_RepairItemLifecycleEvents_BindingId", "columns": ["BindingId"], "unique": false, "filter": null} |
| dbo.RepairItemLifecycleEvents | INDEX | IX_RepairItemLifecycleEvents_DecisionId | {"name": "IX_RepairItemLifecycleEvents_DecisionId", "columns": ["DecisionId"], "unique": false, "filter": null} |
| dbo.RepairItemLifecycleEvents | INDEX | IX_RepairItemLifecycleEvents_DefectId | {"name": "IX_RepairItemLifecycleEvents_DefectId", "columns": ["DefectId"], "unique": false, "filter": null} |
| dbo.RepairItemLifecycleEvents | INDEX | IX_RepairItemLifecycleEvents_ItemId | {"name": "IX_RepairItemLifecycleEvents_ItemId", "columns": ["ItemId"], "unique": false, "filter": null} |
| dbo.RepairItemLifecycleEvents | INDEX | IX_RepairItemLifecycleEvents_ObligationId | {"name": "IX_RepairItemLifecycleEvents_ObligationId", "columns": ["ObligationId"], "unique": false, "filter": null} |
| dbo.RepairItemLifecycleEvents | INDEX | IX_RepairItemLifecycleEvents_ProjectId | {"name": "IX_RepairItemLifecycleEvents_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} |
| dbo.RepairItemLifecycleEvents | INDEX | IX_RepairItemLifecycleEvents_ReviewId | {"name": "IX_RepairItemLifecycleEvents_ReviewId", "columns": ["ReviewId"], "unique": false, "filter": null} |
| dbo.RepairItemLifecycleEvents | INDEX | IX_RepairItemLifecycleEvents_SubmissionId | {"name": "IX_RepairItemLifecycleEvents_SubmissionId", "columns": ["SubmissionId"], "unique": false, "filter": null} |
| dbo.RepairItems | KEY | PK_RepairItems | {"name": "PK_RepairItems", "columns": ["Id"]} |
| dbo.RepairItems | INDEX | IX_RepairItems_CrewId | {"name": "IX_RepairItems_CrewId", "columns": ["CrewId"], "unique": false, "filter": null} |
| dbo.RepairItems | INDEX | IX_RepairItems_CurrentAssessmentId | {"name": "IX_RepairItems_CurrentAssessmentId", "columns": ["CurrentAssessmentId"], "unique": false, "filter": null} |
| dbo.RepairItems | INDEX | IX_RepairItems_CurrentAttemptId | {"name": "IX_RepairItems_CurrentAttemptId", "columns": ["CurrentAttemptId"], "unique": false, "filter": null} |
| dbo.RepairItems | INDEX | IX_RepairItems_CurrentBindingId | {"name": "IX_RepairItems_CurrentBindingId", "columns": ["CurrentBindingId"], "unique": false, "filter": null} |
| dbo.RepairItems | INDEX | IX_RepairItems_CurrentExecutionFinishId | {"name": "IX_RepairItems_CurrentExecutionFinishId", "columns": ["CurrentExecutionFinishId"], "unique": false, "filter": null} |
| dbo.RepairItems | INDEX | IX_RepairItems_CurrentExecutionStartId | {"name": "IX_RepairItems_CurrentExecutionStartId", "columns": ["CurrentExecutionStartId"], "unique": false, "filter": null} |
| dbo.RepairItems | INDEX | IX_RepairItems_CurrentIntakeLinkId | {"name": "IX_RepairItems_CurrentIntakeLinkId", "columns": ["CurrentIntakeLinkId"], "unique": false, "filter": null} |
| dbo.RepairItems | INDEX | IX_RepairItems_CurrentReviewId | {"name": "IX_RepairItems_CurrentReviewId", "columns": ["CurrentReviewId"], "unique": false, "filter": null} |
| dbo.RepairItems | INDEX | IX_RepairItems_DefectId | {"name": "IX_RepairItems_DefectId", "columns": ["DefectId"], "unique": false, "filter": null} |
| dbo.RepairItems | INDEX | IX_RepairItems_EffectiveDecisionId | {"name": "IX_RepairItems_EffectiveDecisionId", "columns": ["EffectiveDecisionId"], "unique": false, "filter": null} |
| dbo.RepairItems | INDEX | IX_RepairItems_EffectiveIntakeSubmissionId | {"name": "IX_RepairItems_EffectiveIntakeSubmissionId", "columns": ["EffectiveIntakeSubmissionId"], "unique": false, "filter": null} |
| dbo.RepairItems | INDEX | IX_RepairItems_ObligationId | {"name": "IX_RepairItems_ObligationId", "columns": ["ObligationId"], "unique": false, "filter": null} |
| dbo.RepairItems | INDEX | IX_RepairItems_PackageId | {"name": "IX_RepairItems_PackageId", "columns": ["PackageId"], "unique": false, "filter": null} |
| dbo.RepairItems | INDEX | IX_RepairItems_PredecessorItemId | {"name": "IX_RepairItems_PredecessorItemId", "columns": ["PredecessorItemId"], "unique": false, "filter": null} |
| dbo.RepairItems | INDEX | IX_RepairItems_ProjectId | {"name": "IX_RepairItems_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} |
| dbo.RepairItems | INDEX | IX_RepairItems_SupersededByItemId | {"name": "IX_RepairItems_SupersededByItemId", "columns": ["SupersededByItemId"], "unique": false, "filter": null} |
| dbo.RepairMeasurementAssessments | KEY | PK_RepairMeasurementAssessments | {"name": "PK_RepairMeasurementAssessments", "columns": ["Id"]} |
| dbo.RepairMeasurementAssessments | INDEX | IX_RepairMeasurementAssessments_AssignmentId | {"name": "IX_RepairMeasurementAssessments_AssignmentId", "columns": ["AssignmentId"], "unique": false, "filter": null} |
| dbo.RepairMeasurementAssessments | INDEX | IX_RepairMeasurementAssessments_BindingId | {"name": "IX_RepairMeasurementAssessments_BindingId", "columns": ["BindingId"], "unique": false, "filter": null} |
| dbo.RepairMeasurementAssessments | INDEX | IX_RepairMeasurementAssessments_FirstStartId | {"name": "IX_RepairMeasurementAssessments_FirstStartId", "columns": ["FirstStartId"], "unique": false, "filter": null} |
| dbo.RepairMeasurementAssessments | INDEX | IX_RepairMeasurementAssessments_FormalSourceSubmissionId | {"name": "IX_RepairMeasurementAssessments_FormalSourceSubmissionId", "columns": ["FormalSourceSubmissionId"], "unique": false, "filter": null} |
| dbo.RepairMeasurementAssessments | INDEX | IX_RepairMeasurementAssessments_ItemId | {"name": "IX_RepairMeasurementAssessments_ItemId", "columns": ["ItemId"], "unique": false, "filter": null} |
| dbo.RepairMeasurementAssessments | INDEX | IX_RepairMeasurementAssessments_OperationOriginId | {"name": "IX_RepairMeasurementAssessments_OperationOriginId", "columns": ["OperationOriginId"], "unique": false, "filter": null} |
| dbo.RepairMeasurementAssessments | INDEX | IX_RepairMeasurementAssessments_OriginalActorId | {"name": "IX_RepairMeasurementAssessments_OriginalActorId", "columns": ["OriginalActorId"], "unique": false, "filter": null} |
| dbo.RepairMeasurementAssessments | INDEX | IX_RepairMeasurementAssessments_ProjectId_OriginId | {"name": "IX_RepairMeasurementAssessments_ProjectId_OriginId", "columns": ["ProjectId", "OriginId"], "unique": true, "filter": null} |
| dbo.RepairMeasurementAssessments | INDEX | IX_RepairMeasurementAssessments_SessionId | {"name": "IX_RepairMeasurementAssessments_SessionId", "columns": ["SessionId"], "unique": true, "filter": null} |
| dbo.RepairMeasurementAssessments | INDEX | IX_RepairMeasurementAssessments_TaskId | {"name": "IX_RepairMeasurementAssessments_TaskId", "columns": ["TaskId"], "unique": false, "filter": null} |
| dbo.RepairNormalSuccessors | KEY | PK_RepairNormalSuccessors | {"name": "PK_RepairNormalSuccessors", "columns": ["Id"]} |
| dbo.RepairNormalSuccessors | INDEX | IX_RepairNormalSuccessors_ActorId | {"name": "IX_RepairNormalSuccessors_ActorId", "columns": ["ActorId"], "unique": false, "filter": null} |
| dbo.RepairNormalSuccessors | INDEX | IX_RepairNormalSuccessors_ObligationId | {"name": "IX_RepairNormalSuccessors_ObligationId", "columns": ["ObligationId"], "unique": false, "filter": null} |
| dbo.RepairNormalSuccessors | INDEX | IX_RepairNormalSuccessors_ProjectId | {"name": "IX_RepairNormalSuccessors_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} |
| dbo.RepairNormalSuccessors | INDEX | IX_RepairNormalSuccessors_SourceAssessmentId | {"name": "IX_RepairNormalSuccessors_SourceAssessmentId", "columns": ["SourceAssessmentId"], "unique": false, "filter": null} |
| dbo.RepairNormalSuccessors | INDEX | IX_RepairNormalSuccessors_SourceCancellationEventId | {"name": "IX_RepairNormalSuccessors_SourceCancellationEventId", "columns": ["SourceCancellationEventId"], "unique": false, "filter": null} |
| dbo.RepairNormalSuccessors | INDEX | IX_RepairNormalSuccessors_SourceDecisionId | {"name": "IX_RepairNormalSuccessors_SourceDecisionId", "columns": ["SourceDecisionId"], "unique": false, "filter": null} |
| dbo.RepairNormalSuccessors | INDEX | IX_RepairNormalSuccessors_SourceHandoverEventId | {"name": "IX_RepairNormalSuccessors_SourceHandoverEventId", "columns": ["SourceHandoverEventId"], "unique": false, "filter": null} |
| dbo.RepairNormalSuccessors | INDEX | IX_RepairNormalSuccessors_SourceItemId | {"name": "IX_RepairNormalSuccessors_SourceItemId", "columns": ["SourceItemId"], "unique": true, "filter": null} |
| dbo.RepairNormalSuccessors | INDEX | IX_RepairNormalSuccessors_TargetItemId | {"name": "IX_RepairNormalSuccessors_TargetItemId", "columns": ["TargetItemId"], "unique": true, "filter": null} |
| dbo.RepairObligationResolutionEvents | KEY | PK_RepairObligationResolutionEvents | {"name": "PK_RepairObligationResolutionEvents", "columns": ["DecisionId"]} |
| dbo.RepairObligationResolutionEvents | INDEX | IX_RepairObligationResolutionEvents_ObligationId | {"name": "IX_RepairObligationResolutionEvents_ObligationId", "columns": ["ObligationId"], "unique": false, "filter": null} |
| dbo.RepairObligationResolutionEvents | INDEX | IX_RepairObligationResolutionEvents_PreviousHeadDecisionId | {"name": "IX_RepairObligationResolutionEvents_PreviousHeadDecisionId", "columns": ["PreviousHeadDecisionId"], "unique": false, "filter": null} |
| dbo.RepairObligationResolutionEvents | INDEX | IX_RepairObligationResolutionEvents_SupersedesDecisionId | {"name": "IX_RepairObligationResolutionEvents_SupersedesDecisionId", "columns": ["SupersedesDecisionId"], "unique": false, "filter": null} |
| dbo.RepairObligations | KEY | PK_RepairObligations | {"name": "PK_RepairObligations", "columns": ["Id"]} |
| dbo.RepairObligations | INDEX | IX_RepairObligations_CurrentRepairItemId | {"name": "IX_RepairObligations_CurrentRepairItemId", "columns": ["CurrentRepairItemId"], "unique": false, "filter": null} |
| dbo.RepairObligations | INDEX | IX_RepairObligations_DefectId | {"name": "IX_RepairObligations_DefectId", "columns": ["DefectId"], "unique": false, "filter": null} |
| dbo.RepairObligations | INDEX | IX_RepairObligations_EffectiveResolutionDecisionId | {"name": "IX_RepairObligations_EffectiveResolutionDecisionId", "columns": ["EffectiveResolutionDecisionId"], "unique": false, "filter": null} |
| dbo.RepairObligations | INDEX | IX_RepairObligations_EffectiveResolutionHeadDecisionId | {"name": "IX_RepairObligations_EffectiveResolutionHeadDecisionId", "columns": ["EffectiveResolutionHeadDecisionId"], "unique": false, "filter": null} |
| dbo.RepairObligations | INDEX | IX_RepairObligations_OriginalCrewFirstStartId | {"name": "IX_RepairObligations_OriginalCrewFirstStartId", "columns": ["OriginalCrewFirstStartId"], "unique": false, "filter": null} |
| dbo.RepairObligations | INDEX | IX_RepairObligations_PackageId | {"name": "IX_RepairObligations_PackageId", "columns": ["PackageId"], "unique": false, "filter": null} |
| dbo.RepairObligations | INDEX | IX_RepairObligations_ProjectId | {"name": "IX_RepairObligations_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} |
| dbo.RepairPackages | KEY | PK_RepairPackages | {"name": "PK_RepairPackages", "columns": ["Id"]} |
| dbo.RepairPackages | INDEX | IX_RepairPackages_DefectId | {"name": "IX_RepairPackages_DefectId", "columns": ["DefectId"], "unique": false, "filter": null} |
| dbo.RepairPackages | INDEX | IX_RepairPackages_ProjectId | {"name": "IX_RepairPackages_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} |
| dbo.RepairPolicyDraftChanges | KEY | PK_RepairPolicyDraftChanges | {"name": "PK_RepairPolicyDraftChanges", "columns": ["Id"]} |
| dbo.RepairPolicyDraftChanges | INDEX | IX_RepairPolicyDraftChanges_ActorId | {"name": "IX_RepairPolicyDraftChanges_ActorId", "columns": ["ActorId"], "unique": false, "filter": null} |
| dbo.RepairPolicyDraftChanges | INDEX | IX_RepairPolicyDraftChanges_DraftId | {"name": "IX_RepairPolicyDraftChanges_DraftId", "columns": ["DraftId"], "unique": false, "filter": null} |
| dbo.RepairPolicyDrafts | KEY | PK_RepairPolicyDrafts | {"name": "PK_RepairPolicyDrafts", "columns": ["Id"]} |
| dbo.RepairPolicyDrafts | INDEX | IX_RepairPolicyDrafts_CurrentChangeId | {"name": "IX_RepairPolicyDrafts_CurrentChangeId", "columns": ["CurrentChangeId"], "unique": false, "filter": null} |
| dbo.RepairPolicyDrafts | INDEX | IX_RepairPolicyDrafts_ProjectId | {"name": "IX_RepairPolicyDrafts_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} |
| dbo.RepairPolicyDrafts | INDEX | IX_RepairPolicyDrafts_PublishedRevisionId | {"name": "IX_RepairPolicyDrafts_PublishedRevisionId", "columns": ["PublishedRevisionId"], "unique": false, "filter": null} |
| dbo.RepairPolicyMeasurementRules | KEY | PK_RepairPolicyMeasurementRules | {"name": "PK_RepairPolicyMeasurementRules", "columns": ["PolicyRevisionId", "Code"]} |
| dbo.RepairPolicyRevisions | KEY | PK_RepairPolicyRevisions | {"name": "PK_RepairPolicyRevisions", "columns": ["Id"]} |
| dbo.RepairPolicyRevisions | INDEX | IX_RepairPolicyRevisions_ProjectId_Revision | {"name": "IX_RepairPolicyRevisions_ProjectId_Revision", "columns": ["ProjectId", "Revision"], "unique": true, "filter": null} |
| dbo.RepairPolicyRevisions | INDEX | IX_RepairPolicyRevisions_PublishedBy | {"name": "IX_RepairPolicyRevisions_PublishedBy", "columns": ["PublishedBy"], "unique": false, "filter": null} |
| dbo.RepairPolicyRevocations | KEY | PK_RepairPolicyRevocations | {"name": "PK_RepairPolicyRevocations", "columns": ["Id"]} |
| dbo.RepairPolicyRevocations | INDEX | IX_RepairPolicyRevocations_ActorId | {"name": "IX_RepairPolicyRevocations_ActorId", "columns": ["ActorId"], "unique": false, "filter": null} |
| dbo.RepairPolicyRevocations | INDEX | IX_RepairPolicyRevocations_PolicyRevisionId | {"name": "IX_RepairPolicyRevocations_PolicyRevisionId", "columns": ["PolicyRevisionId"], "unique": false, "filter": null} |
| dbo.RepairReviewRequests | KEY | PK_RepairReviewRequests | {"name": "PK_RepairReviewRequests", "columns": ["Id"]} |
| dbo.RepairReviewRequests | INDEX | IX_RepairReviewRequests_ActorId | {"name": "IX_RepairReviewRequests_ActorId", "columns": ["ActorId"], "unique": false, "filter": null} |
| dbo.RepairReviewRequests | INDEX | IX_RepairReviewRequests_DecisionId | {"name": "IX_RepairReviewRequests_DecisionId", "columns": ["DecisionId"], "unique": false, "filter": null} |
| dbo.RepairReviewRequests | INDEX | IX_RepairReviewRequests_ItemId | {"name": "IX_RepairReviewRequests_ItemId", "columns": ["ItemId"], "unique": false, "filter": null} |
| dbo.RepairReviewRequests | INDEX | IX_RepairReviewRequests_ProjectId | {"name": "IX_RepairReviewRequests_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} |
| dbo.RepairSafetyActionSources | KEY | PK_RepairSafetyActionSources | {"name": "PK_RepairSafetyActionSources", "columns": ["Id"]} |
| dbo.RepairSafetyActionSources | INDEX | IX_RepairSafetyActionSources_ActorId | {"name": "IX_RepairSafetyActionSources_ActorId", "columns": ["ActorId"], "unique": false, "filter": null} |
| dbo.RepairSafetyActionSources | INDEX | IX_RepairSafetyActionSources_BindingId | {"name": "IX_RepairSafetyActionSources_BindingId", "columns": ["BindingId"], "unique": false, "filter": null} |
| dbo.RepairSafetyActionSources | INDEX | IX_RepairSafetyActionSources_ItemId | {"name": "IX_RepairSafetyActionSources_ItemId", "columns": ["ItemId"], "unique": false, "filter": null} |
| dbo.RepairSafetyActionSources | INDEX | IX_RepairSafetyActionSources_MeasureId_Kind_OriginId | {"name": "IX_RepairSafetyActionSources_MeasureId_Kind_OriginId", "columns": ["MeasureId", "Kind", "OriginId"], "unique": true, "filter": null} |
| dbo.RepairSafetyActionSources | INDEX | IX_RepairSafetyActionSources_ProjectId | {"name": "IX_RepairSafetyActionSources_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} |
| dbo.RepairSafetyCheckEvidence | KEY | PK_RepairSafetyCheckEvidence | {"name": "PK_RepairSafetyCheckEvidence", "columns": ["CheckId", "FileId"]} |
| dbo.RepairSafetyCheckEvidence | INDEX | IX_RepairSafetyCheckEvidence_ActualUploaderId | {"name": "IX_RepairSafetyCheckEvidence_ActualUploaderId", "columns": ["ActualUploaderId"], "unique": false, "filter": null} |
| dbo.RepairSafetyCheckEvidence | INDEX | IX_RepairSafetyCheckEvidence_FileId | {"name": "IX_RepairSafetyCheckEvidence_FileId", "columns": ["FileId"], "unique": false, "filter": null} |
| dbo.RepairSafetyChecks | KEY | PK_RepairSafetyChecks | {"name": "PK_RepairSafetyChecks", "columns": ["Id"]} |
| dbo.RepairSafetyChecks | INDEX | IX_RepairSafetyChecks_ActorId | {"name": "IX_RepairSafetyChecks_ActorId", "columns": ["ActorId"], "unique": false, "filter": null} |
| dbo.RepairSafetyChecks | INDEX | IX_RepairSafetyChecks_MeasureId | {"name": "IX_RepairSafetyChecks_MeasureId", "columns": ["MeasureId"], "unique": false, "filter": null} |
| dbo.RepairSafetyMonitoring | KEY | AK_RepairSafetyMonitoring_MeasureId | {"name": "AK_RepairSafetyMonitoring_MeasureId", "columns": ["MeasureId"]} |
| dbo.RepairSafetyMonitoring | KEY | PK_RepairSafetyMonitoring | {"name": "PK_RepairSafetyMonitoring", "columns": ["Id"]} |
| dbo.RepairSafetyMonitoring | INDEX | IX_RepairSafetyMonitoring_CurrentCheckId | {"name": "IX_RepairSafetyMonitoring_CurrentCheckId", "columns": ["CurrentCheckId"], "unique": false, "filter": null} |
| dbo.RepairSafetyMonitoring | INDEX | IX_RepairSafetyMonitoring_FormalObligationId | {"name": "IX_RepairSafetyMonitoring_FormalObligationId", "columns": ["FormalObligationId"], "unique": false, "filter": null} |
| dbo.RepairSafetyMonitoring | INDEX | IX_RepairSafetyMonitoring_MeasureId | {"name": "IX_RepairSafetyMonitoring_MeasureId", "columns": ["MeasureId"], "unique": true, "filter": null} |
| dbo.RepairSafetyMonitoring | INDEX | IX_RepairSafetyMonitoring_SafetyObligationId | {"name": "IX_RepairSafetyMonitoring_SafetyObligationId", "columns": ["SafetyObligationId"], "unique": true, "filter": null} |
| dbo.RepairSafetyResponsibilityTransfers | KEY | AK_RepairSafetyResponsibilityTransfers_MeasureId_Id | {"name": "AK_RepairSafetyResponsibilityTransfers_MeasureId_Id", "columns": ["MeasureId", "Id"]} |
| dbo.RepairSafetyResponsibilityTransfers | KEY | PK_RepairSafetyResponsibilityTransfers | {"name": "PK_RepairSafetyResponsibilityTransfers", "columns": ["Id"]} |
| dbo.RepairSafetyResponsibilityTransfers | INDEX | IX_RepairSafetyResponsibilityTransfers_ChangedBy | {"name": "IX_RepairSafetyResponsibilityTransfers_ChangedBy", "columns": ["ChangedBy"], "unique": false, "filter": null} |
| dbo.RepairSafetyResponsibilityTransfers | INDEX | IX_RepairSafetyResponsibilityTransfers_NextActorId | {"name": "IX_RepairSafetyResponsibilityTransfers_NextActorId", "columns": ["NextActorId"], "unique": false, "filter": null} |
| dbo.RepairSafetyResponsibilityTransfers | INDEX | IX_RepairSafetyResponsibilityTransfers_PreviousActorId | {"name": "IX_RepairSafetyResponsibilityTransfers_PreviousActorId", "columns": ["PreviousActorId"], "unique": false, "filter": null} |
| dbo.RepairTemporarySafetyMeasures | KEY | PK_RepairTemporarySafetyMeasures | {"name": "PK_RepairTemporarySafetyMeasures", "columns": ["Id"]} |
| dbo.RepairTemporarySafetyMeasures | INDEX | IX_RepairTemporarySafetyMeasures_DefectId | {"name": "IX_RepairTemporarySafetyMeasures_DefectId", "columns": ["DefectId"], "unique": false, "filter": null} |
| dbo.RepairTemporarySafetyMeasures | INDEX | IX_RepairTemporarySafetyMeasures_FormalRepairObligationId | {"name": "IX_RepairTemporarySafetyMeasures_FormalRepairObligationId", "columns": ["FormalRepairObligationId"], "unique": false, "filter": null} |
| dbo.RepairTemporarySafetyMeasures | INDEX | IX_RepairTemporarySafetyMeasures_Id_CurrentResponsibilityTransferId | {"name": "IX_RepairTemporarySafetyMeasures_Id_CurrentResponsibilityTransferId", "columns": ["Id", "CurrentResponsibilityTransferId"], "unique": false, "filter": null} |
| dbo.RepairTemporarySafetyMeasures | INDEX | IX_RepairTemporarySafetyMeasures_InstalledBy | {"name": "IX_RepairTemporarySafetyMeasures_InstalledBy", "columns": ["InstalledBy"], "unique": false, "filter": null} |
| dbo.RepairTemporarySafetyMeasures | INDEX | IX_RepairTemporarySafetyMeasures_ProjectId | {"name": "IX_RepairTemporarySafetyMeasures_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} |
| dbo.RepairTemporarySafetyMeasures | INDEX | IX_RepairTemporarySafetyMeasures_ResponsibleActorId | {"name": "IX_RepairTemporarySafetyMeasures_ResponsibleActorId", "columns": ["ResponsibleActorId"], "unique": false, "filter": null} |
| dbo.RepairWorkHandovers | KEY | PK_RepairWorkHandovers | {"name": "PK_RepairWorkHandovers", "columns": ["Id"]} |
| dbo.RepairWorkHandovers | INDEX | IX_RepairWorkHandovers_FromActorId | {"name": "IX_RepairWorkHandovers_FromActorId", "columns": ["FromActorId"], "unique": false, "filter": null} |
| dbo.RepairWorkHandovers | INDEX | IX_RepairWorkHandovers_ItemId | {"name": "IX_RepairWorkHandovers_ItemId", "columns": ["ItemId"], "unique": true, "filter": null} |
| dbo.RepairWorkHandovers | INDEX | IX_RepairWorkHandovers_ToActorId | {"name": "IX_RepairWorkHandovers_ToActorId", "columns": ["ToActorId"], "unique": false, "filter": null} |
| dbo.ReporterRegistrationIntents | KEY | PK_ReporterRegistrationIntents | {"name": "PK_ReporterRegistrationIntents", "columns": ["Id"]} |
| dbo.ReporterRegistrationIntents | INDEX | IX_ReporterRegistrationIntents_Email | {"name": "IX_ReporterRegistrationIntents_Email", "columns": ["NormalizedEmail"], "unique": false, "filter": null} |
| dbo.ReporterRegistrationIntents | INDEX | IX_ReporterRegistrationIntents_UserId | {"name": "IX_ReporterRegistrationIntents_UserId", "columns": ["UserId"], "unique": false, "filter": null} |
| dbo.ReporterRegistrationIntents | CHECK | CK_ReporterRegistrationIntents_ReporterType | {"name": "CK_ReporterRegistrationIntents_ReporterType", "sql": "[ReporterType] IN (1, 2)"} |
| dbo.ReportOriginalEvidence | KEY | PK_ReportOriginalEvidence | {"name": "PK_ReportOriginalEvidence", "columns": ["Id"]} |
| dbo.ReportOriginalEvidence | INDEX | IX_ReportOriginalEvidence_FileId | {"name": "IX_ReportOriginalEvidence_FileId", "columns": ["FileId"], "unique": false, "filter": null} |
| dbo.ReportOriginalEvidence | INDEX | IX_ReportOriginalEvidence_Id_ReportId | {"name": "IX_ReportOriginalEvidence_Id_ReportId", "columns": ["Id", "ReportId"], "unique": true, "filter": null} |
| dbo.ReportOriginalEvidence | INDEX | IX_ReportOriginalEvidence_OwnerUserId | {"name": "IX_ReportOriginalEvidence_OwnerUserId", "columns": ["OwnerUserId"], "unique": false, "filter": null} |
| dbo.ReportOriginalEvidence | INDEX | IX_ReportOriginalEvidence_ReportId_OwnerUserId | {"name": "IX_ReportOriginalEvidence_ReportId_OwnerUserId", "columns": ["ReportId", "OwnerUserId"], "unique": false, "filter": null} |
| dbo.ReportOriginalEvidence | CHECK | CK_ReportOriginalEvidence_Original | {"name": "CK_ReportOriginalEvidence_Original", "sql": "[SupplementId] IS NULL AND [VerificationState] = 1"} |
| dbo.Reports | KEY | AK_Reports_Id_ReporterUserId | {"name": "AK_Reports_Id_ReporterUserId", "columns": ["Id", "ReporterUserId"]} |
| dbo.Reports | KEY | PK_Reports | {"name": "PK_Reports", "columns": ["Id"]} |
| dbo.Reports | INDEX | IX_Reports_ReporterUserId | {"name": "IX_Reports_ReporterUserId", "columns": ["ReporterUserId"], "unique": false, "filter": null} |
| dbo.ReportSupplementEvidence | KEY | PK_ReportSupplementEvidence | {"name": "PK_ReportSupplementEvidence", "columns": ["Id"]} |
| dbo.ReportSupplementEvidence | INDEX | IX_ReportSupplementEvidence_FileId | {"name": "IX_ReportSupplementEvidence_FileId", "columns": ["FileId"], "unique": false, "filter": null} |
| dbo.ReportSupplementEvidence | INDEX | IX_ReportSupplementEvidence_Id_ReportId | {"name": "IX_ReportSupplementEvidence_Id_ReportId", "columns": ["Id", "ReportId"], "unique": true, "filter": null} |
| dbo.ReportSupplementEvidence | INDEX | IX_ReportSupplementEvidence_OwnerUserId | {"name": "IX_ReportSupplementEvidence_OwnerUserId", "columns": ["OwnerUserId"], "unique": false, "filter": null} |
| dbo.ReportSupplementEvidence | INDEX | IX_ReportSupplementEvidence_ReportId_OwnerUserId | {"name": "IX_ReportSupplementEvidence_ReportId_OwnerUserId", "columns": ["ReportId", "OwnerUserId"], "unique": false, "filter": null} |
| dbo.ReportSupplementEvidence | INDEX | IX_ReportSupplementEvidence_SupplementId_ReportId | {"name": "IX_ReportSupplementEvidence_SupplementId_ReportId", "columns": ["SupplementId", "ReportId"], "unique": false, "filter": null} |
| dbo.ReportSupplementEvidence | CHECK | CK_ReportSupplementEvidence_Supplement | {"name": "CK_ReportSupplementEvidence_Supplement", "sql": "[SupplementId] IS NOT NULL AND [VerificationState] = 1"} |
| dbo.ReportSupplements | KEY | AK_ReportSupplements_Id_ReportId | {"name": "AK_ReportSupplements_Id_ReportId", "columns": ["Id", "ReportId"]} |
| dbo.ReportSupplements | KEY | PK_ReportSupplements | {"name": "PK_ReportSupplements", "columns": ["Id"]} |
| dbo.ReportSupplements | INDEX | IX_ReportSupplements_ReportId | {"name": "IX_ReportSupplements_ReportId", "columns": ["ReportId"], "unique": false, "filter": null} |
| dbo.RetentionBasisHeads | KEY | PK_RetentionBasisHeads | {"name": "PK_RetentionBasisHeads", "columns": ["FileId"]} |
| dbo.RetentionBasisHeads | INDEX | IX_RetentionBasisHeads_RevisionId_FileId | {"name": "IX_RetentionBasisHeads_RevisionId_FileId", "columns": ["RevisionId", "FileId"], "unique": false, "filter": null} |
| dbo.RetentionBasisRevisions | KEY | AK_RetentionBasisRevisions_Id_FileId | {"name": "AK_RetentionBasisRevisions_Id_FileId", "columns": ["Id", "FileId"]} |
| dbo.RetentionBasisRevisions | KEY | PK_RetentionBasisRevisions | {"name": "PK_RetentionBasisRevisions", "columns": ["Id"]} |
| dbo.RetentionBasisRevisions | INDEX | IX_RetentionBasisRevisions_ConfirmedBy | {"name": "IX_RetentionBasisRevisions_ConfirmedBy", "columns": ["ConfirmedBy"], "unique": false, "filter": null} |
| dbo.RetentionBasisRevisions | INDEX | IX_RetentionBasisRevisions_FileId_Revision | {"name": "IX_RetentionBasisRevisions_FileId_Revision", "columns": ["FileId", "Revision"], "unique": true, "filter": null} |
| dbo.RetentionBasisRevisions | INDEX | IX_RetentionBasisRevisions_SupersedesId_FileId | {"name": "IX_RetentionBasisRevisions_SupersedesId_FileId", "columns": ["SupersedesId", "FileId"], "unique": false, "filter": null} |
| dbo.RetentionBasisRevisions | CHECK | CK_RetentionBasis_Revision | {"name": "CK_RetentionBasis_Revision", "sql": "[Revision]>0 AND [PolicyVersion]='pr41a.v1' AND [Classification] IN ('EVIDENCE','TEMPORARY_EXPORT') AND ISJSON([WarrantyReferencesJson])=1"} |
| dbo.RetentionEvaluationItems | KEY | PK_RetentionEvaluationItems | {"name": "PK_RetentionEvaluationItems", "columns": ["Id"]} |
| dbo.RetentionEvaluationItems | INDEX | IX_RetentionEvaluationItems_EvaluationId_FileId | {"name": "IX_RetentionEvaluationItems_EvaluationId_FileId", "columns": ["EvaluationId", "FileId"], "unique": true, "filter": null} |
| dbo.RetentionEvaluationItems | INDEX | IX_RetentionEvaluationItems_FileId | {"name": "IX_RetentionEvaluationItems_FileId", "columns": ["FileId"], "unique": false, "filter": null} |
| dbo.RetentionEvaluationItems | CHECK | CK_RetentionEvaluationItem_State | {"name": "CK_RetentionEvaluationItem_State", "sql": "[Eligibility] IN ('BLOCKED_HOLD','WAITING_RETENTION_BASIS','RETAIN_UNTIL','ELIGIBLE_FOR_REVIEW')"} |
| dbo.RetentionEvaluations | KEY | PK_RetentionEvaluations | {"name": "PK_RetentionEvaluations", "columns": ["Id"]} |
| dbo.RetentionEvaluations | INDEX | IX_RetentionEvaluations_ProjectId | {"name": "IX_RetentionEvaluations_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} |
| dbo.RetentionEvaluations | INDEX | IX_RetentionEvaluations_RequestedBy | {"name": "IX_RetentionEvaluations_RequestedBy", "columns": ["RequestedBy"], "unique": false, "filter": null} |
| dbo.RetentionEvaluations | INDEX | IX_RetentionEvaluations_Status_CreatedAt | {"name": "IX_RetentionEvaluations_Status_CreatedAt", "columns": ["Status", "CreatedAt"], "unique": false, "filter": null} |
| dbo.RetentionEvaluations | CHECK | CK_RetentionEvaluation_State | {"name": "CK_RetentionEvaluation_State", "sql": "[Status] IN ('QUEUED','COMPLETE') AND (([Status]='QUEUED' AND [EvaluatedAt] IS NULL) OR ([Status]='COMPLETE' AND [EvaluatedAt] IS NOT NULL))"} |
| dbo.RetentionHoldHistories | KEY | PK_RetentionHoldHistories | {"name": "PK_RetentionHoldHistories", "columns": ["Id"]} |
| dbo.RetentionHoldHistories | INDEX | IX_RetentionHoldHistories_ActorId | {"name": "IX_RetentionHoldHistories_ActorId", "columns": ["ActorId"], "unique": false, "filter": null} |
| dbo.RetentionHoldHistories | INDEX | IX_RetentionHoldHistories_HoldId_OccurredAt | {"name": "IX_RetentionHoldHistories_HoldId_OccurredAt", "columns": ["HoldId", "OccurredAt"], "unique": false, "filter": null} |
| dbo.RetentionHoldHistories | CHECK | CK_RetentionHoldHistory_State | {"name": "CK_RetentionHoldHistory_State", "sql": "[State] IN ('ACTIVE','RELEASED')"} |
| dbo.RetentionHolds | KEY | PK_RetentionHolds | {"name": "PK_RetentionHolds", "columns": ["Id"]} |
| dbo.RetentionHolds | INDEX | IX_RetentionHolds_CreatedBy | {"name": "IX_RetentionHolds_CreatedBy", "columns": ["CreatedBy"], "unique": false, "filter": null} |
| dbo.RetentionHolds | INDEX | IX_RetentionHolds_ReleasedBy | {"name": "IX_RetentionHolds_ReleasedBy", "columns": ["ReleasedBy"], "unique": false, "filter": null} |
| dbo.RetentionHolds | INDEX | IX_RetentionHolds_ScopeType_ScopeId_State | {"name": "IX_RetentionHolds_ScopeType_ScopeId_State", "columns": ["ScopeType", "ScopeId", "State"], "unique": false, "filter": null} |
| dbo.RetentionHolds | CHECK | CK_RetentionHold_State | {"name": "CK_RetentionHold_State", "sql": "[ScopeType] IN ('PROJECT','FILE') AND [State] IN ('ACTIVE','RELEASED') AND (([State]='ACTIVE' AND [ReleasedBy] IS NULL AND [ReleasedAt] IS NULL) OR ([State]='RELEASED' AND [ReleasedBy] IS NOT NULL AND [ReleasedAt] IS NOT NULL))"} |
| dbo.RoadCoverageMappings | KEY | PK_RoadCoverageMappings | {"name": "PK_RoadCoverageMappings", "columns": ["Id"]} |
| dbo.RoadCoverageMappings | INDEX | IX_RoadCoverageMappings_ActorId | {"name": "IX_RoadCoverageMappings_ActorId", "columns": ["ActorId"], "unique": false, "filter": null} |
| dbo.RoadCoverageMappings | INDEX | IX_RoadCoverageMappings_CoverageFileId | {"name": "IX_RoadCoverageMappings_CoverageFileId", "columns": ["CoverageFileId"], "unique": false, "filter": null} |
| dbo.RoadCoverageMappings | INDEX | IX_RoadCoverageMappings_HandoverDocumentId | {"name": "IX_RoadCoverageMappings_HandoverDocumentId", "columns": ["HandoverDocumentId"], "unique": false, "filter": null} |
| dbo.RoadCoverageMappings | INDEX | IX_RoadCoverageMappings_HandoverFileId | {"name": "IX_RoadCoverageMappings_HandoverFileId", "columns": ["HandoverFileId"], "unique": false, "filter": null} |
| dbo.RoadCoverageMappings | INDEX | IX_RoadCoverageMappings_ProjectId_RoadSectionId_LocationVersion | {"name": "IX_RoadCoverageMappings_ProjectId_RoadSectionId_LocationVersion", "columns": ["ProjectId", "RoadSectionId", "LocationVersion"], "unique": false, "filter": null} |
| dbo.RoadCoverageMappings | INDEX | IX_RoadCoverageMappings_RoadSectionId | {"name": "IX_RoadCoverageMappings_RoadSectionId", "columns": ["RoadSectionId"], "unique": false, "filter": null} |
| dbo.RoadCoverageMappings | INDEX | IX_RoadCoverageMappings_ScopeObligationId | {"name": "IX_RoadCoverageMappings_ScopeObligationId", "columns": ["ScopeObligationId"], "unique": false, "filter": null} |
| dbo.RoadCoverageMappings | INDEX | IX_RoadCoverageMappings_SupersedesId | {"name": "IX_RoadCoverageMappings_SupersedesId", "columns": ["SupersedesId"], "unique": true, "filter": "[SupersedesId] IS NOT NULL"} |
| dbo.RoadCoverageMappings | CHECK | CK_RoadCoverageMappings_Bounds | {"name": "CK_RoadCoverageMappings_Bounds", "sql": "[From]>=0 AND [To]>[From] AND [OffsetTo]>[OffsetFrom] AND [ApplicableToUtc]>[ApplicableFromUtc]"} |
| dbo.RoadCoverageMappings | CHECK | CK_RoadCoverageMappings_Source | {"name": "CK_RoadCoverageMappings_Source", "sql": "[SourceKind] COLLATE Latin1_General_100_BIN2 IN ('WARRANTY','MAINTENANCE_BASIS') AND [Provenance] COLLATE Latin1_General_100_BIN2 IN ('REAL_SOURCE','TEST_ONLY') AND ISJSON([SourceFactsJson])=1"} |
| dbo.RoadGeometryDrafts | KEY | PK_RoadGeometryDrafts | {"name": "PK_RoadGeometryDrafts", "columns": ["Id"]} |
| dbo.RoadGeometryDrafts | INDEX | IX_RoadGeometryDrafts_ProjectId | {"name": "IX_RoadGeometryDrafts_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} |
| dbo.RoadGeometryDrafts | INDEX | IX_RoadGeometryDrafts_RoadSectionId | {"name": "IX_RoadGeometryDrafts_RoadSectionId", "columns": ["RoadSectionId"], "unique": false, "filter": null} |
| dbo.RoadGeometryMetadata | KEY | PK_RoadGeometryMetadata | {"name": "PK_RoadGeometryMetadata", "columns": ["RoadSectionVersionId"]} |
| dbo.RoadGeometryMetadata | INDEX | IX_RoadGeometryMetadata_SourceDraftId | {"name": "IX_RoadGeometryMetadata_SourceDraftId", "columns": ["SourceDraftId"], "unique": false, "filter": null} |
| dbo.RoadRouteSystems | KEY | AK_RoadRouteSystems_Id_ProjectId | {"name": "AK_RoadRouteSystems_Id_ProjectId", "columns": ["Id", "ProjectId"]} |
| dbo.RoadRouteSystems | KEY | PK_RoadRouteSystems | {"name": "PK_RoadRouteSystems", "columns": ["Id"]} |
| dbo.RoadRouteSystems | INDEX | IX_RoadRouteSystems_ProjectId_Code | {"name": "IX_RoadRouteSystems_ProjectId_Code", "columns": ["ProjectId", "Code"], "unique": true, "filter": null} |
| dbo.RoadSections | KEY | PK_RoadSections | {"name": "PK_RoadSections", "columns": ["Id"]} |
| dbo.RoadSections | INDEX | UX_RoadSections_ProjectId_Code | {"name": "UX_RoadSections_ProjectId_Code", "columns": ["ProjectId", "Code"], "unique": true, "filter": null} |
| dbo.RoadSectionVersions | KEY | PK_RoadSectionVersions | {"name": "PK_RoadSectionVersions", "columns": ["Id"]} |
| dbo.RoadSectionVersions | INDEX | IX_RoadSectionVersions_CrsProfileRevisionId | {"name": "IX_RoadSectionVersions_CrsProfileRevisionId", "columns": ["CrsProfileRevisionId"], "unique": false, "filter": null} |
| dbo.RoadSectionVersions | INDEX | UX_RoadSectionVersions_Current | {"name": "UX_RoadSectionVersions_Current", "columns": ["RoadSectionId"], "unique": true, "filter": "[IsCurrent] = 1"} |
| dbo.RoadSectionVersions | INDEX | UX_RoadSectionVersions_RoadSectionId_VersionNo | {"name": "UX_RoadSectionVersions_RoadSectionId_VersionNo", "columns": ["RoadSectionId", "VersionNo"], "unique": true, "filter": null} |
| dbo.RoadSectionVersions | CHECK | CK_RoadSectionVersions_Geometry_AllowedSrid | {"name": "CK_RoadSectionVersions_Geometry_AllowedSrid", "sql": "([CrsProfileRevisionId] IS NULL AND [Geometry].STSrid IN (32648, 32649)) OR ([CrsProfileRevisionId] IS NOT NULL AND [Geometry].STSrid>=0)"} |
| dbo.RoadSectionVersions | CHECK | CK_RoadSectionVersions_Geometry_LineString | {"name": "CK_RoadSectionVersions_Geometry_LineString", "sql": "[Geometry].STGeometryType() = 'LineString'"} |
| dbo.RoadSectionVersions | CHECK | CK_RoadSectionVersions_VersionNo_Positive | {"name": "CK_RoadSectionVersions_VersionNo_Positive", "sql": "[VersionNo] > 0"} |
| dbo.RoadSegments | KEY | PK_RoadSegments | {"name": "PK_RoadSegments", "columns": ["Id"]} |
| dbo.RoadSegments | INDEX | IX_RoadSegments_RoadSectionVersionId | {"name": "IX_RoadSegments_RoadSectionVersionId", "columns": ["RoadSectionVersionId"], "unique": false, "filter": null} |
| dbo.RoadSegments | INDEX | IX_RoadSegments_SegmentSetId_RoadSectionVersionId | {"name": "IX_RoadSegments_SegmentSetId_RoadSectionVersionId", "columns": ["SegmentSetId", "RoadSectionVersionId"], "unique": false, "filter": null} |
| dbo.RoadSegments | INDEX | UX_RoadSegments_Set_Sequence | {"name": "UX_RoadSegments_Set_Sequence", "columns": ["SegmentSetId", "Sequence"], "unique": true, "filter": null} |
| dbo.RoadSegments | CHECK | CK_RoadSegments_Sequence_Positive | {"name": "CK_RoadSegments_Sequence_Positive", "sql": "[Sequence] > 0"} |
| dbo.RoadSegmentSets | KEY | AK_RoadSegmentSets_Id_RoadSectionVersionId | {"name": "AK_RoadSegmentSets_Id_RoadSectionVersionId", "columns": ["Id", "RoadSectionVersionId"]} |
| dbo.RoadSegmentSets | KEY | PK_RoadSegmentSets | {"name": "PK_RoadSegmentSets", "columns": ["Id"]} |
| dbo.RoadSegmentSets | INDEX | IX_RoadSegmentSets_RouteVersion_Status | {"name": "IX_RoadSegmentSets_RouteVersion_Status", "columns": ["RoadSectionVersionId", "Status"], "unique": false, "filter": null} |
| dbo.RoadSegmentSets | INDEX | UX_RoadSegmentSets_CurrentPublished | {"name": "UX_RoadSegmentSets_CurrentPublished", "columns": ["RoadSectionVersionId"], "unique": true, "filter": "[Status] = 'PUBLISHED'"} |
| dbo.RoadSegmentSets | CHECK | CK_RoadSegmentSets_Status | {"name": "CK_RoadSegmentSets_Status", "sql": "[Status] IN ('DRAFT','PUBLISHED','SUPERSEDED')"} |
| dbo.Roles | KEY | PK_Roles | {"name": "PK_Roles", "columns": ["Code"]} |
| dbo.Sessions | KEY | PK_Sessions | {"name": "PK_Sessions", "columns": ["Id"]} |
| dbo.Sessions | INDEX | IX_Sessions_UserId | {"name": "IX_Sessions_UserId", "columns": ["UserId"], "unique": false, "filter": null} |
| dbo.Sessions | CHECK | CK_Sessions_DeviceMetadataJson_Json | {"name": "CK_Sessions_DeviceMetadataJson_Json", "sql": "[DeviceMetadataJson] IS NULL OR ISJSON([DeviceMetadataJson]) = 1"} |
| dbo.Sessions | CHECK | CK_Sessions_ExpiresAt | {"name": "CK_Sessions_ExpiresAt", "sql": "([Lifecycle] = 0 AND [ExpiresAt] IS NOT NULL AND [ExpiresAt] > [IssuedAt]) OR ([Lifecycle] = 1 AND [ExpiresAt] IS NULL AND [IssuedRole] IS NOT NULL)"} |
| dbo.Sessions | CHECK | CK_Sessions_RevokedAt | {"name": "CK_Sessions_RevokedAt", "sql": "[RevokedAt] IS NULL OR [RevokedAt] >= [IssuedAt]"} |
| dbo.Sessions | CHECK | CK_Sessions_Transport | {"name": "CK_Sessions_Transport", "sql": "[Transport] IN (0, 1, 2)"} |
| dbo.SeverityRuleVersions | KEY | PK_SeverityRuleVersions | {"name": "PK_SeverityRuleVersions", "columns": ["Id"]} |
| dbo.SeverityRuleVersions | INDEX | UX_SeverityRuleVersions_ScopeVersion | {"name": "UX_SeverityRuleVersions_ScopeVersion", "columns": ["StandardCode", "RoadTypeCode", "VersionNo"], "unique": true, "filter": null} |
| dbo.SeverityRuleVersions | CHECK | CK_SeverityRuleVersions_EffectiveDateRange | {"name": "CK_SeverityRuleVersions_EffectiveDateRange", "sql": "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]"} |
| dbo.SeverityRuleVersions | CHECK | CK_SeverityRuleVersions_RuleDefinition_JsonObject | {"name": "CK_SeverityRuleVersions_RuleDefinition_JsonObject", "sql": "ISJSON([RuleDefinition]) = 1 AND LEFT(LTRIM([RuleDefinition]), 1) = '{'"} |
| dbo.SeverityRuleVersions | CHECK | CK_SeverityRuleVersions_VersionNo_Positive | {"name": "CK_SeverityRuleVersions_VersionNo_Positive", "sql": "[VersionNo] > 0"} |
| dbo.SourceDecisions | KEY | AK_SourceDecisions_Id_SourceKind_SourceId_ProjectId | {"name": "AK_SourceDecisions_Id_SourceKind_SourceId_ProjectId", "columns": ["Id", "SourceKind", "SourceId", "ProjectId"]} |
| dbo.SourceDecisions | KEY | PK_SourceDecisions | {"name": "PK_SourceDecisions", "columns": ["Id"]} |
| dbo.SourceDecisions | INDEX | IX_SourceDecisions_AIDetectionSourceId | {"name": "IX_SourceDecisions_AIDetectionSourceId", "columns": ["AIDetectionSourceId"], "unique": false, "filter": null} |
| dbo.SourceDecisions | INDEX | IX_SourceDecisions_Classification_CauseCategoryCode | {"name": "IX_SourceDecisions_Classification_CauseCategoryCode", "columns": ["Classification_CauseCategoryCode"], "unique": false, "filter": null} |
| dbo.SourceDecisions | INDEX | IX_SourceDecisions_Classification_DefectTypeCode | {"name": "IX_SourceDecisions_Classification_DefectTypeCode", "columns": ["Classification_DefectTypeCode"], "unique": false, "filter": null} |
| dbo.SourceDecisions | INDEX | IX_SourceDecisions_Classification_RoadSectionVersionId | {"name": "IX_SourceDecisions_Classification_RoadSectionVersionId", "columns": ["Classification_RoadSectionVersionId"], "unique": false, "filter": null} |
| dbo.SourceDecisions | INDEX | IX_SourceDecisions_Classification_SegmentId | {"name": "IX_SourceDecisions_Classification_SegmentId", "columns": ["Classification_SegmentId"], "unique": false, "filter": null} |
| dbo.SourceDecisions | INDEX | IX_SourceDecisions_DecidedByUserId | {"name": "IX_SourceDecisions_DecidedByUserId", "columns": ["DecidedByUserId"], "unique": false, "filter": null} |
| dbo.SourceDecisions | INDEX | IX_SourceDecisions_ProjectId | {"name": "IX_SourceDecisions_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} |
| dbo.SourceDecisions | INDEX | IX_SourceDecisions_ReportSourceId | {"name": "IX_SourceDecisions_ReportSourceId", "columns": ["ReportSourceId"], "unique": false, "filter": null} |
| dbo.SourceDecisions | INDEX | IX_SourceDecisions_SupersedesDecisionId | {"name": "IX_SourceDecisions_SupersedesDecisionId", "columns": ["SupersedesDecisionId"], "unique": false, "filter": null} |
| dbo.SourceDecisions | INDEX | IX_SourceDecisions_TargetDefectId | {"name": "IX_SourceDecisions_TargetDefectId", "columns": ["TargetDefectId"], "unique": false, "filter": null} |
| dbo.SourceDecisions | CHECK | CK_SourceDecisions_TypedSource | {"name": "CK_SourceDecisions_TypedSource", "sql": "[SourceKind] = [Source_Kind] AND [SourceId] = [Source_Id] AND (([SourceKind]=1 AND [ReportSourceId]=[SourceId] AND [ReportSourceId] IS NOT NULL AND [AIDetectionSourceId] IS NULL) OR ([SourceKind]=2 AND [AIDetectionSourceId]=[SourceId] AND [AIDetectionSourceId] IS NOT NULL AND [ReportSourceId] IS NULL))"} |
| dbo.StaffInvitationProjects | KEY | PK_StaffInvitationProjects | {"name": "PK_StaffInvitationProjects", "columns": ["InvitationId", "ProjectId"]} |
| dbo.StaffInvitationProjects | INDEX | IX_StaffInvitationProjects_ProjectId | {"name": "IX_StaffInvitationProjects_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} |
| dbo.StaffInvitations | KEY | PK_StaffInvitations | {"name": "PK_StaffInvitations", "columns": ["Id"]} |
| dbo.StaffInvitations | INDEX | IX_StaffInvitations_CreatedByUserId | {"name": "IX_StaffInvitations_CreatedByUserId", "columns": ["CreatedByUserId"], "unique": false, "filter": null} |
| dbo.StaffInvitations | INDEX | IX_StaffInvitations_Email | {"name": "IX_StaffInvitations_Email", "columns": ["NormalizedEmail"], "unique": false, "filter": null} |
| dbo.StaffInvitations | INDEX | UX_StaffInvitations_TokenHash | {"name": "UX_StaffInvitations_TokenHash", "columns": ["TokenHash"], "unique": true, "filter": null} |
| dbo.SupplementarySurveyRequests | KEY | PK_SupplementarySurveyRequests | {"name": "PK_SupplementarySurveyRequests", "columns": ["Id"]} |
| dbo.SupplementarySurveyRequests | INDEX | IX_SupplementarySurveyRequests_ApprovedByUserId | {"name": "IX_SupplementarySurveyRequests_ApprovedByUserId", "columns": ["ApprovedByUserId"], "unique": false, "filter": null} |
| dbo.SupplementarySurveyRequests | INDEX | IX_SupplementarySurveyRequests_RequestedByUserId | {"name": "IX_SupplementarySurveyRequests_RequestedByUserId", "columns": ["RequestedByUserId"], "unique": false, "filter": null} |
| dbo.SupplementarySurveyRequests | INDEX | IX_SupplementarySurveyRequests_SurveyRequestId | {"name": "IX_SupplementarySurveyRequests_SurveyRequestId", "columns": ["SurveyRequestId"], "unique": false, "filter": null} |
| dbo.SupplementarySurveyRequests | INDEX | UX_SupplementarySurveyRequests_SurveyRound | {"name": "UX_SupplementarySurveyRequests_SurveyRound", "columns": ["SurveyId", "RoundNo"], "unique": true, "filter": null} |
| dbo.SupplementarySurveyRequests | CHECK | CK_SupplementarySurveyRequests_Approval | {"name": "CK_SupplementarySurveyRequests_Approval", "sql": "([ApprovedByUserId] IS NULL AND [ApprovedAt] IS NULL) OR ([ApprovedByUserId] IS NOT NULL AND [ApprovedAt] IS NOT NULL)"} |
| dbo.SupplementarySurveyRequests | CHECK | CK_SupplementarySurveyRequests_RequestedScope_JsonObject | {"name": "CK_SupplementarySurveyRequests_RequestedScope_JsonObject", "sql": "ISJSON([RequestedScope]) = 1 AND LEFT(LTRIM([RequestedScope]), 1) = '{'"} |
| dbo.SupplementarySurveyRequests | CHECK | CK_SupplementarySurveyRequests_RoundNo | {"name": "CK_SupplementarySurveyRequests_RoundNo", "sql": "[RoundNo] > 0"} |
| dbo.SupplementarySurveyRequests | CHECK | CK_SupplementarySurveyRequests_Status | {"name": "CK_SupplementarySurveyRequests_Status", "sql": "[Status] IN (1, 2, 3, 4, 5, 6, 7)"} |
| dbo.SurveyAssignments | KEY | PK_SurveyAssignments | {"name": "PK_SurveyAssignments", "columns": ["Id"]} |
| dbo.SurveyAssignments | INDEX | IX_SurveyAssignments_AssignedByUserId | {"name": "IX_SurveyAssignments_AssignedByUserId", "columns": ["AssignedByUserId"], "unique": false, "filter": null} |
| dbo.SurveyAssignments | INDEX | IX_SurveyAssignments_OperatorUserId | {"name": "IX_SurveyAssignments_OperatorUserId", "columns": ["OperatorUserId"], "unique": false, "filter": null} |
| dbo.SurveyAssignments | INDEX | IX_SurveyAssignments_RequestEndedAt | {"name": "IX_SurveyAssignments_RequestEndedAt", "columns": ["SurveyRequestId", "EndedAt"], "unique": false, "filter": null} |
| dbo.SurveyAssignments | INDEX | UX_SurveyAssignments_ActiveRequest | {"name": "UX_SurveyAssignments_ActiveRequest", "columns": ["SurveyRequestId"], "unique": true, "filter": "[EndedAt] IS NULL"} |
| dbo.SurveyAssignments | CHECK | CK_SurveyAssignments_AcceptanceRejection | {"name": "CK_SurveyAssignments_AcceptanceRejection", "sql": "[AcceptedAt] IS NULL OR [RejectedAt] IS NULL"} |
| dbo.SurveyAssignments | CHECK | CK_SurveyAssignments_ActiveReassignmentReason | {"name": "CK_SurveyAssignments_ActiveReassignmentReason", "sql": "[EndedAt] IS NOT NULL OR [ReassignmentReason] IS NULL"} |
| dbo.SurveyAssignments | CHECK | CK_SurveyAssignments_ReassignmentNotRejected | {"name": "CK_SurveyAssignments_ReassignmentNotRejected", "sql": "[ReassignmentReason] IS NULL OR [RejectedAt] IS NULL"} |
| dbo.SurveyAssignments | CHECK | CK_SurveyAssignments_Rejection | {"name": "CK_SurveyAssignments_Rejection", "sql": "([RejectedAt] IS NULL AND [RejectionReason] IS NULL) OR ([RejectedAt] IS NOT NULL AND LEN(LTRIM(RTRIM([RejectionReason]))) > 0 AND [EndedAt] IS NOT NULL)"} |
| dbo.SurveyAssignments | CHECK | CK_SurveyAssignments_TimestampOrder | {"name": "CK_SurveyAssignments_TimestampOrder", "sql": "([AcceptedAt] IS NULL OR [AcceptedAt] >= [AssignedAt]) AND ([RejectedAt] IS NULL OR [RejectedAt] >= [AssignedAt]) AND ([EndedAt] IS NULL OR [EndedAt] >= [AssignedAt])"} |
| dbo.SurveyDataVersions | KEY | PK_SurveyDataVersions | {"name": "PK_SurveyDataVersions", "columns": ["Id"]} |
| dbo.SurveyDataVersions | INDEX | UX_SurveyDataVersions_SurveyVersion | {"name": "UX_SurveyDataVersions_SurveyVersion", "columns": ["SurveyId", "VersionNo"], "unique": true, "filter": null} |
| dbo.SurveyDataVersions | CHECK | CK_SurveyDataVersions_IntegrityStatus | {"name": "CK_SurveyDataVersions_IntegrityStatus", "sql": "[IntegrityStatus] IN (1, 2, 3)"} |
| dbo.SurveyDataVersions | CHECK | CK_SurveyDataVersions_ServerConfirmation | {"name": "CK_SurveyDataVersions_ServerConfirmation", "sql": "([Status] = 3 AND [IntegrityStatus] = 2 AND [ConfirmedAt] IS NOT NULL AND [ConfirmedBy] = 1) OR ([Status] <> 3 AND [ConfirmedAt] IS NULL AND [ConfirmedBy] IS NULL)"} |
| dbo.SurveyDataVersions | CHECK | CK_SurveyDataVersions_SourceManifest_JsonArray | {"name": "CK_SurveyDataVersions_SourceManifest_JsonArray", "sql": "ISJSON([SourceManifest]) = 1 AND LEFT(LTRIM([SourceManifest]), 1) = '['"} |
| dbo.SurveyDataVersions | CHECK | CK_SurveyDataVersions_Status | {"name": "CK_SurveyDataVersions_Status", "sql": "[Status] IN (1, 2, 3, 4, 5)"} |
| dbo.SurveyDataVersions | CHECK | CK_SurveyDataVersions_VersionNo | {"name": "CK_SurveyDataVersions_VersionNo", "sql": "[VersionNo] > 0"} |
| dbo.SurveyFiles | KEY | PK_SurveyFiles | {"name": "PK_SurveyFiles", "columns": ["Id"]} |
| dbo.SurveyFiles | INDEX | IX_SurveyFiles_FileId | {"name": "IX_SurveyFiles_FileId", "columns": ["FileId"], "unique": false, "filter": null} |
| dbo.SurveyFiles | INDEX | IX_SurveyFiles_FlightId | {"name": "IX_SurveyFiles_FlightId", "columns": ["FlightId"], "unique": false, "filter": null} |
| dbo.SurveyFiles | INDEX | IX_SurveyFiles_SurveyId | {"name": "IX_SurveyFiles_SurveyId", "columns": ["SurveyId"], "unique": false, "filter": null} |
| dbo.SurveyFiles | CHECK | CK_SurveyFiles_Checksum_Sha256Lowercase | {"name": "CK_SurveyFiles_Checksum_Sha256Lowercase", "sql": "LEN([Checksum]) = 64 AND [Checksum] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'"} |
| dbo.SurveyFiles | CHECK | CK_SurveyFiles_FileType | {"name": "CK_SurveyFiles_FileType", "sql": "[FileType] IN (1, 2, 3, 4)"} |
| dbo.SurveyFiles | CHECK | CK_SurveyFiles_SyncStatus | {"name": "CK_SurveyFiles_SyncStatus", "sql": "[SyncStatus] IN (1, 2, 3, 4, 5)"} |
| dbo.SurveyFiles | CHECK | CK_SurveyFiles_TimestampOrder | {"name": "CK_SurveyFiles_TimestampOrder", "sql": "[CaptureEndedAt] IS NULL OR [CaptureStartedAt] IS NULL OR [CaptureEndedAt] >= [CaptureStartedAt]"} |
| dbo.SurveyPlanPostponements | KEY | PK_SurveyPlanPostponements | {"name": "PK_SurveyPlanPostponements", "columns": ["Id"]} |
| dbo.SurveyPlanPostponements | INDEX | IX_SurveyPlanPostponements_PlanId_PostponedAt | {"name": "IX_SurveyPlanPostponements_PlanId_PostponedAt", "columns": ["SurveyPlanId", "PostponedAt"], "unique": false, "filter": null} |
| dbo.SurveyPlans | KEY | PK_SurveyPlans | {"name": "PK_SurveyPlans", "columns": ["Id"]} |
| dbo.SurveyPlans | INDEX | IX_SurveyPlans_ProjectRoadStart | {"name": "IX_SurveyPlans_ProjectRoadStart", "columns": ["ProjectId", "RoadSectionId", "PlannedStartAt"], "unique": false, "filter": null} |
| dbo.SurveyPlans | INDEX | IX_SurveyPlans_RoadSectionId | {"name": "IX_SurveyPlans_RoadSectionId", "columns": ["RoadSectionId"], "unique": false, "filter": null} |
| dbo.SurveyPlans | INDEX | IX_SurveyPlans_RoadSectionVersionId | {"name": "IX_SurveyPlans_RoadSectionVersionId", "columns": ["RoadSectionVersionId"], "unique": false, "filter": null} |
| dbo.SurveyPlans | INDEX | UX_SurveyPlans_ActiveScope | {"name": "UX_SurveyPlans_ActiveScope", "columns": ["ProjectId", "RoadSectionId", "SurveyType"], "unique": true, "filter": "[Status] IN (1, 2, 3)"} |
| dbo.SurveyPlans | CHECK | CK_SurveyPlans_OutputRequirements_Json | {"name": "CK_SurveyPlans_OutputRequirements_Json", "sql": "ISJSON([OutputRequirements]) = 1"} |
| dbo.SurveyPlans | CHECK | CK_SurveyPlans_PlannedDateRange | {"name": "CK_SurveyPlans_PlannedDateRange", "sql": "[PlannedEndAt] >= [PlannedStartAt]"} |
| dbo.SurveyPlans | CHECK | CK_SurveyPlans_Status | {"name": "CK_SurveyPlans_Status", "sql": "[Status] IN (1, 2, 3, 4, 5)"} |
| dbo.SurveyPlans | CHECK | CK_SurveyPlans_SurveyType | {"name": "CK_SurveyPlans_SurveyType", "sql": "[SurveyType] IN (1, 2, 3)"} |
| dbo.SurveyPlanScopes | KEY | PK_SurveyPlanScopes | {"name": "PK_SurveyPlanScopes", "columns": ["Id"]} |
| dbo.SurveyPlanScopes | INDEX | IX_SurveyPlanScopes_RouteSectionVersionId | {"name": "IX_SurveyPlanScopes_RouteSectionVersionId", "columns": ["RouteSectionVersionId"], "unique": false, "filter": null} |
| dbo.SurveyPlanScopes | INDEX | UX_SurveyPlanScopes_UniqueBand | {"name": "UX_SurveyPlanScopes_UniqueBand", "columns": ["SurveyPlanId", "RouteSectionVersionId", "SegmentSetId", "TargetBand"], "unique": true, "filter": null} |
| dbo.SurveyPlanScopes | CHECK | CK_SurveyPlanScopes_SegmentIdsJson | {"name": "CK_SurveyPlanScopes_SegmentIdsJson", "sql": "ISJSON([SegmentIdsJson]) = 1"} |
| dbo.SurveyRequests | KEY | PK_SurveyRequests | {"name": "PK_SurveyRequests", "columns": ["Id"]} |
| dbo.SurveyRequests | INDEX | IX_SurveyRequests_ParentTaskId | {"name": "IX_SurveyRequests_ParentTaskId", "columns": ["ParentTaskId"], "unique": false, "filter": null} |
| dbo.SurveyRequests | INDEX | IX_SurveyRequests_ProjectRoadStatus | {"name": "IX_SurveyRequests_ProjectRoadStatus", "columns": ["ProjectId", "RoadSectionId", "Status"], "unique": false, "filter": null} |
| dbo.SurveyRequests | INDEX | IX_SurveyRequests_RequestedByUserId | {"name": "IX_SurveyRequests_RequestedByUserId", "columns": ["RequestedByUserId"], "unique": false, "filter": null} |
| dbo.SurveyRequests | INDEX | IX_SurveyRequests_RoadSectionId | {"name": "IX_SurveyRequests_RoadSectionId", "columns": ["RoadSectionId"], "unique": false, "filter": null} |
| dbo.SurveyRequests | INDEX | IX_SurveyRequests_RoadSectionVersionId | {"name": "IX_SurveyRequests_RoadSectionVersionId", "columns": ["RoadSectionVersionId"], "unique": false, "filter": null} |
| dbo.SurveyRequests | INDEX | IX_SurveyRequests_SupplementRequestId | {"name": "IX_SurveyRequests_SupplementRequestId", "columns": ["SupplementRequestId"], "unique": false, "filter": null} |
| dbo.SurveyRequests | INDEX | UX_SurveyRequests_ActivePlan | {"name": "UX_SurveyRequests_ActivePlan", "columns": ["SurveyPlanId"], "unique": true, "filter": "[SurveyPlanId] IS NOT NULL AND [Status] IN (1, 2, 4, 5, 6, 7, 10)"} |
| dbo.SurveyRequests | CHECK | CK_SurveyRequests_Cancellation | {"name": "CK_SurveyRequests_Cancellation", "sql": "([Status] = 9 AND [CancelledAt] IS NOT NULL AND LEN(LTRIM(RTRIM([CancellationReason]))) > 0) OR ([Status] <> 9 AND [CancelledAt] IS NULL AND [CancellationReason] IS NULL)"} |
| dbo.SurveyRequests | CHECK | CK_SurveyRequests_OutputRequirements_Json | {"name": "CK_SurveyRequests_OutputRequirements_Json", "sql": "ISJSON([OutputRequirements]) = 1"} |
| dbo.SurveyRequests | CHECK | CK_SurveyRequests_Status | {"name": "CK_SurveyRequests_Status", "sql": "[Status] IN (1, 2, 3, 4, 5, 6, 7, 8, 9, 10)"} |
| dbo.SurveyRequests | CHECK | CK_SurveyRequests_SurveyType | {"name": "CK_SurveyRequests_SurveyType", "sql": "[SurveyType] IN (1, 2, 3)"} |
| dbo.SurveyRequestScopes | KEY | PK_SurveyRequestScopes | {"name": "PK_SurveyRequestScopes", "columns": ["Id"]} |
| dbo.SurveyRequestScopes | INDEX | IX_SurveyRequestScopes_RouteSectionVersionId | {"name": "IX_SurveyRequestScopes_RouteSectionVersionId", "columns": ["RouteSectionVersionId"], "unique": false, "filter": null} |
| dbo.SurveyRequestScopes | INDEX | UX_SurveyRequestScopes_UniqueBand | {"name": "UX_SurveyRequestScopes_UniqueBand", "columns": ["SurveyRequestId", "RouteSectionVersionId", "SegmentSetId", "TargetBand"], "unique": true, "filter": null} |
| dbo.SurveyRequestScopes | CHECK | CK_SurveyRequestScopes_SegmentIdsJson | {"name": "CK_SurveyRequestScopes_SegmentIdsJson", "sql": "ISJSON([SegmentIdsJson]) = 1"} |
| dbo.Surveys | KEY | PK_Surveys | {"name": "PK_Surveys", "columns": ["Id"]} |
| dbo.Surveys | INDEX | IX_Surveys_BaselineConfirmedByUserId | {"name": "IX_Surveys_BaselineConfirmedByUserId", "columns": ["BaselineConfirmedByUserId"], "unique": false, "filter": null} |
| dbo.Surveys | INDEX | IX_Surveys_ProjectStatus | {"name": "IX_Surveys_ProjectStatus", "columns": ["ProjectId", "Status"], "unique": false, "filter": null} |
| dbo.Surveys | INDEX | IX_Surveys_RoadSectionVersionId | {"name": "IX_Surveys_RoadSectionVersionId", "columns": ["RoadSectionVersionId"], "unique": false, "filter": null} |
| dbo.Surveys | INDEX | IX_Surveys_SurveyRequestId | {"name": "IX_Surveys_SurveyRequestId", "columns": ["SurveyRequestId"], "unique": false, "filter": null} |
| dbo.Surveys | CHECK | CK_Surveys_BaselineConfirmation | {"name": "CK_Surveys_BaselineConfirmation", "sql": "([IsBaselineConfirmed] = 0 AND [BaselineConfirmedByUserId] IS NULL AND [BaselineConfirmedAt] IS NULL) OR ([IsBaselineConfirmed] = 1 AND [BaselineConfirmedByUserId] IS NOT NULL AND [BaselineConfirmedAt] IS NOT NULL)"} |
| dbo.Surveys | CHECK | CK_Surveys_Status | {"name": "CK_Surveys_Status", "sql": "[Status] IN (1, 2, 3, 4, 5)"} |
| dbo.Surveys | CHECK | CK_Surveys_SurveyType | {"name": "CK_Surveys_SurveyType", "sql": "[SurveyType] IN (1, 2, 3)"} |
| dbo.TrainingLabelReviews | KEY | PK_TrainingLabelReviews | {"name": "PK_TrainingLabelReviews", "columns": ["Id"]} |
| dbo.TrainingLabelReviews | INDEX | IX_TrainingLabelReviews_ActorUserId | {"name": "IX_TrainingLabelReviews_ActorUserId", "columns": ["ActorUserId"], "unique": false, "filter": null} |
| dbo.TrainingLabelReviews | INDEX | IX_TrainingLabelReviews_RevisionId | {"name": "IX_TrainingLabelReviews_RevisionId", "columns": ["RevisionId"], "unique": true, "filter": null} |
| dbo.TrainingLabelReviews | INDEX | IX_TrainingLabelReviews_RevisionId_LabelId | {"name": "IX_TrainingLabelReviews_RevisionId_LabelId", "columns": ["RevisionId", "LabelId"], "unique": false, "filter": null} |
| dbo.TrainingLabelReviews | CHECK | CK_TrainingLabelReviews_Decision | {"name": "CK_TrainingLabelReviews_Decision", "sql": "[Decision] IN ('APPROVED','REJECTED')"} |
| dbo.TrainingLabelRevisions | KEY | AK_TrainingLabelRevisions_Id_LabelId | {"name": "AK_TrainingLabelRevisions_Id_LabelId", "columns": ["Id", "LabelId"]} |
| dbo.TrainingLabelRevisions | KEY | PK_TrainingLabelRevisions | {"name": "PK_TrainingLabelRevisions", "columns": ["Id"]} |
| dbo.TrainingLabelRevisions | INDEX | IX_TrainingLabelRevisions_DefectTypeCode | {"name": "IX_TrainingLabelRevisions_DefectTypeCode", "columns": ["DefectTypeCode"], "unique": false, "filter": null} |
| dbo.TrainingLabelRevisions | INDEX | IX_TrainingLabelRevisions_FileId | {"name": "IX_TrainingLabelRevisions_FileId", "columns": ["FileId"], "unique": false, "filter": null} |
| dbo.TrainingLabelRevisions | INDEX | IX_TrainingLabelRevisions_LabelId_Revision | {"name": "IX_TrainingLabelRevisions_LabelId_Revision", "columns": ["LabelId", "Revision"], "unique": true, "filter": null} |
| dbo.TrainingLabelRevisions | CHECK | CK_TrainingLabelRevisions_Bbox | {"name": "CK_TrainingLabelRevisions_Bbox", "sql": "[X]>=0 AND [Y]>=0 AND [Width]>0 AND [Height]>0 AND [X]+[Width]<=1 AND [Y]+[Height]<=1"} |
| dbo.TrainingLabels | KEY | PK_TrainingLabels | {"name": "PK_TrainingLabels", "columns": ["Id"]} |
| dbo.TrainingLabels | INDEX | IX_TrainingLabels_AIDetectionSourceId | {"name": "IX_TrainingLabels_AIDetectionSourceId", "columns": ["AIDetectionSourceId"], "unique": false, "filter": null} |
| dbo.TrainingLabels | INDEX | IX_TrainingLabels_ProjectId_SourceKind_SourceId | {"name": "IX_TrainingLabels_ProjectId_SourceKind_SourceId", "columns": ["ProjectId", "SourceKind", "SourceId"], "unique": false, "filter": null} |
| dbo.TrainingLabels | INDEX | IX_TrainingLabels_ReportSourceId | {"name": "IX_TrainingLabels_ReportSourceId", "columns": ["ReportSourceId"], "unique": false, "filter": null} |
| dbo.TrainingLabels | CHECK | CK_TrainingLabels_Source | {"name": "CK_TrainingLabels_Source", "sql": "([SourceKind]='REPORT' AND [ReportSourceId]=[SourceId] AND [AIDetectionSourceId] IS NULL) OR ([SourceKind]='AI_DETECTION' AND [AIDetectionSourceId]=[SourceId] AND [ReportSourceId] IS NULL)"} |
| dbo.UploadMultipartSweeps | KEY | PK_UploadMultipartSweeps | {"name": "PK_UploadMultipartSweeps", "columns": ["UploadSessionId"]} |
| dbo.UploadMultipartSweeps | INDEX | IX_UploadMultipartSweeps_NextCheckAt | {"name": "IX_UploadMultipartSweeps_NextCheckAt", "columns": ["NextCheckAt"], "unique": false, "filter": null} |
| dbo.UploadParts | KEY | PK_UploadParts | {"name": "PK_UploadParts", "columns": ["Id"]} |
| dbo.UploadParts | INDEX | UX_UploadParts_Session_PartNumber | {"name": "UX_UploadParts_Session_PartNumber", "columns": ["UploadSessionId", "PartNumber"], "unique": true, "filter": null} |
| dbo.UploadSessions | KEY | PK_UploadSessions | {"name": "PK_UploadSessions", "columns": ["Id"]} |
| dbo.UploadSessions | INDEX | IX_UploadSessions_FileId | {"name": "IX_UploadSessions_FileId", "columns": ["FileId"], "unique": false, "filter": null} |
| dbo.UploadSessions | INDEX | IX_UploadSessions_MultipartNextCheckAt | {"name": "IX_UploadSessions_MultipartNextCheckAt", "columns": ["MultipartNextCheckAt"], "unique": false, "filter": null} |
| dbo.UploadSessions | INDEX | IX_UploadSessions_OwnerUserId | {"name": "IX_UploadSessions_OwnerUserId", "columns": ["OwnerUserId"], "unique": false, "filter": null} |
| dbo.UploadSessions | INDEX | IX_UploadSessions_Status_ExpiresAt | {"name": "IX_UploadSessions_Status_ExpiresAt", "columns": ["Status", "ExpiresAt"], "unique": false, "filter": null} |
| dbo.UploadSessions | INDEX | UX_UploadSessions_ObjectKey | {"name": "UX_UploadSessions_ObjectKey", "columns": ["ObjectKey"], "unique": true, "filter": null} |
| dbo.UploadSessions | CHECK | CK_UploadSessions_ExpectedSizeBytes_Positive | {"name": "CK_UploadSessions_ExpectedSizeBytes_Positive", "sql": "[ExpectedSizeBytes] > 0"} |
| dbo.UploadSessions | CHECK | CK_UploadSessions_PartSizeBytes_Positive | {"name": "CK_UploadSessions_PartSizeBytes_Positive", "sql": "[PartSizeBytes] > 0"} |
| dbo.Users | KEY | PK_Users | {"name": "PK_Users", "columns": ["Id"]} |
| dbo.Users | INDEX | IX_Users_RoleCode | {"name": "IX_Users_RoleCode", "columns": ["RoleCode"], "unique": false, "filter": null} |
| dbo.Users | INDEX | UX_Users_Email | {"name": "UX_Users_Email", "columns": ["Email"], "unique": true, "filter": "[Email] IS NOT NULL"} |
| dbo.Users | INDEX | UX_Users_NormalizedUserName | {"name": "UX_Users_NormalizedUserName", "columns": ["NormalizedUserName"], "unique": true, "filter": null} |
| dbo.Users | INDEX | UX_Users_UserName | {"name": "UX_Users_UserName", "columns": ["UserName"], "unique": true, "filter": null} |
| dbo.Users | CHECK | CK_Users_Status | {"name": "CK_Users_Status", "sql": "[Status] IN (1, 2, 3)"} |
| dbo.ValidationRuns | KEY | PK_ValidationRuns | {"name": "PK_ValidationRuns", "columns": ["Id"]} |
| dbo.ValidationRuns | INDEX | IX_ValidationRuns_ModelVersionId | {"name": "IX_ValidationRuns_ModelVersionId", "columns": ["ModelVersionId"], "unique": false, "filter": null} |
| dbo.ValidationRuns | INDEX | IX_ValidationRuns_ProjectStatus | {"name": "IX_ValidationRuns_ProjectStatus", "columns": ["ProjectId", "Status"], "unique": false, "filter": null} |
| dbo.ValidationRuns | CHECK | CK_ValidationRuns_ExclusionReasonsJson | {"name": "CK_ValidationRuns_ExclusionReasonsJson", "sql": "ISJSON([ExclusionReasonsJson]) = 1 AND LEFT(LTRIM([ExclusionReasonsJson]), 1) = '['"} |
| dbo.ValidationRuns | CHECK | CK_ValidationRuns_PairsJson | {"name": "CK_ValidationRuns_PairsJson", "sql": "ISJSON([PairsJson]) = 1 AND LEFT(LTRIM([PairsJson]), 1) = '['"} |
| dbo.ValidationRuns | CHECK | CK_ValidationRuns_Status | {"name": "CK_ValidationRuns_Status", "sql": "[Status] IN (1, 2, 3)"} |
| dbo.Warranties | KEY | PK_Warranties | {"name": "PK_Warranties", "columns": ["Id"]} |
| dbo.Warranties | INDEX | IX_Warranties_HandoverDocumentId | {"name": "IX_Warranties_HandoverDocumentId", "columns": ["HandoverDocumentId"], "unique": false, "filter": null} |
| dbo.Warranties | INDEX | IX_Warranties_ProjectId | {"name": "IX_Warranties_ProjectId", "columns": ["ProjectId"], "unique": false, "filter": null} |
| dbo.Warranties | INDEX | IX_Warranties_RoadSectionId | {"name": "IX_Warranties_RoadSectionId", "columns": ["RoadSectionId"], "unique": false, "filter": null} |
| dbo.Warranties | INDEX | IX_Warranties_SourceDocumentId | {"name": "IX_Warranties_SourceDocumentId", "columns": ["SourceDocumentId"], "unique": false, "filter": null} |
| dbo.Warranties | CHECK | CK_Warranties_DateRange | {"name": "CK_Warranties_DateRange", "sql": "[WarrantyEndDate] >= [WarrantyStartDate]"} |
| dbo.Warranties | CHECK | CK_Warranties_RetainedValue_NonNegative | {"name": "CK_Warranties_RetainedValue_NonNegative", "sql": "[RetainedValue] IS NULL OR [RetainedValue] >= 0"} |
| dbo.Warranties | CHECK | CK_Warranties_Scope | {"name": "CK_Warranties_Scope", "sql": "[Scope] IN (1, 2, 3, 4)"} |
| dbo.Warranties | CHECK | CK_Warranties_ScopeRoadSection | {"name": "CK_Warranties_ScopeRoadSection", "sql": "([Scope] <> 1 OR [RoadSectionId] IS NULL) AND ([Scope] <> 2 OR [RoadSectionId] IS NOT NULL)"} |
| dbo.Warranties | CHECK | CK_Warranties_Status | {"name": "CK_Warranties_Status", "sql": "[Status] IN (1, 2, 3, 4)"} |
| dbo.WeeklyReviewDigestDuties | KEY | PK_WeeklyReviewDigestDuties | {"name": "PK_WeeklyReviewDigestDuties", "columns": ["DigestId", "ClockId"]} |
| dbo.WeeklyReviewDigestDuties | INDEX | IX_WeeklyReviewDigestDuties_ClockId | {"name": "IX_WeeklyReviewDigestDuties_ClockId", "columns": ["ClockId"], "unique": false, "filter": null} |
| dbo.WeeklyReviewDigests | KEY | PK_WeeklyReviewDigests | {"name": "PK_WeeklyReviewDigests", "columns": ["Id"]} |
| dbo.WeeklyReviewDigests | INDEX | IX_WeeklyReviewDigests_ProjectId_ScheduledAtUtc_RecipientKey | {"name": "IX_WeeklyReviewDigests_ProjectId_ScheduledAtUtc_RecipientKey", "columns": ["ProjectId", "ScheduledAtUtc", "RecipientKey"], "unique": true, "filter": null} |
| dbo.WeeklyReviewDigests | INDEX | IX_WeeklyReviewDigests_RecipientId | {"name": "IX_WeeklyReviewDigests_RecipientId", "columns": ["RecipientId"], "unique": false, "filter": null} |
| dbo.WeeklyReviewDigests | INDEX | IX_WeeklyReviewDigests_RecoveryPeriodId | {"name": "IX_WeeklyReviewDigests_RecoveryPeriodId", "columns": ["RecoveryPeriodId"], "unique": false, "filter": null} |
| dbo.WeeklyReviewRecoveryPeriods | KEY | PK_WeeklyReviewRecoveryPeriods | {"name": "PK_WeeklyReviewRecoveryPeriods", "columns": ["Id"]} |
| dbo.WeeklyReviewRecoveryPeriods | INDEX | IX_WeeklyReviewRecoveryPeriods_ProjectId_ScheduledAtUtc | {"name": "IX_WeeklyReviewRecoveryPeriods_ProjectId_ScheduledAtUtc", "columns": ["ProjectId", "ScheduledAtUtc"], "unique": true, "filter": null} |
