# Complete entity and table data dictionary

**CURRENT_VERIFIED — source baseline `cdeb1ca1037744d06995058af13d0582f6273c0a`, `huy-review`, 2026-10-13.** Source observation is not all-path runtime execution. Expected business behavior uses owner-confirmed decisions and assigned specs; implementation/test evidence has its own limits.

**EF_LOGICAL_MODEL** captured fresh from disconnected DesignTimeModel; **MIGRATION_DERIVED_PHYSICAL_MODEL** constrained by current baseline/additive source. Historical DOC-AUDIT-01 fixture SQL is input-hash revalidated; **LIVE_SQL_VERIFIED is NOT_RUN in BE-DOC-02**. Domain semantic meaning is not inferred merely from SQL type.

| Inventory | Count |
|---|---|
| dbSets | 75 |
| efEntityTypes | 196 |
| tables | 191 |
| uniqueMappedColumns | 1940 |

## Declared DbSets

| DbSet | Entity expression | Source line |
|---|---|---|
| Reports | `DbSet<RoadGuardSystem.BusinessObjects.Reports.Report>` | 47 |
| ReportSupplements | `DbSet<RoadGuardSystem.BusinessObjects.Reports.ReportSupplement>` | 48 |
| IncidentCases | `DbSet<RoadGuardSystem.BusinessObjects.Cases.IncidentCase>` | 49 |
| SourceDecisions | `DbSet<RoadGuardSystem.BusinessObjects.Candidates.CandidateDecision>` | 50 |
| AuditLogs | `DbSet<AuditLog>` | 52 |
| IdempotencyRecords | `DbSet<IdempotencyRecord>` | 54 |
| OutboxMessages | `DbSet<OutboxMessage>` | 56 |
| ConsumerEffectReceipts | `DbSet<ConsumerEffectReceipt>` | 58 |
| Notifications | `DbSet<Notification>` | 60 |
| Users | `DbSet<ApplicationUser>` | 63 |
| Roles | `DbSet<ApplicationRole>` | 65 |
| Sessions | `DbSet<UserSession>` | 67 |
| RefreshTokens | `DbSet<RefreshToken>` | 69 |
| PasswordResetLogs | `DbSet<PasswordResetLog>` | 71 |
| AccountStatusChangeLogs | `DbSet<AccountStatusChangeLog>` | 73 |
| PasswordRecoveryRequests | `DbSet<PasswordRecoveryRequest>` | 75 |
| ReporterRegistrationIntents | `DbSet<ReporterRegistrationIntent>` | 77 |
| StaffInvitations | `DbSet<StaffInvitation>` | 79 |
| StaffInvitationProjects | `DbSet<StaffInvitationProject>` | 81 |
| Files | `DbSet<StoredFile>` | 83 |
| FileScopes | `DbSet<FileScope>` | 85 |
| UploadSessions | `DbSet<UploadSession>` | 87 |
| UploadParts | `DbSet<UploadPart>` | 89 |
| DefectTypes | `DbSet<DefectType>` | 91 |
| CauseCategories | `DbSet<CauseCategory>` | 93 |
| SeverityRuleVersions | `DbSet<SeverityRuleVersion>` | 95 |
| Defects | `DbSet<Defect>` | 97 |
| AIDetections | `DbSet<AIDetection>` | 99 |
| DefectVerificationLogs | `DbSet<DefectVerificationLog>` | 101 |
| FieldInspectionTasks | `DbSet<FieldInspectionTask>` | 103 |
| FieldInspectionAssignments | `DbSet<FieldInspectionAssignment>` | 105 |
| FieldInspectionSessions | `DbSet<FieldInspectionSession>` | 107 |
| GroundTruthMeasurements | `DbSet<GroundTruthMeasurement>` | 109 |
| FieldTaskStartOrigins | `DbSet<FieldTaskStartOrigin>` | 111 |
| FieldInspectionSubmissions | `DbSet<FieldInspectionSubmission>` | 112 |
| FieldInspectionReviews | `DbSet<FieldInspectionReview>` | 113 |
| FieldInspectionEvidenceLinks | `DbSet<FieldInspectionEvidenceLink>` | 114 |
| FieldInspectionEvidenceReuseDecisions | `DbSet<FieldInspectionEvidenceReuseDecision>` | 115 |
| FieldInspectionTaskEvents | `DbSet<FieldInspectionTaskEvent>` | 116 |
| FieldInspectionLocationProofs | `DbSet<FieldInspectionLocationProof>` | 117 |
| FieldInspectionOperationOrigins | `DbSet<FieldInspectionOperationOrigin>` | 118 |
| RepairPackages | `DbSet<RepairPackage>` | 120 |
| RepairObligations | `DbSet<RepairObligation>` | 121 |
| RepairItems | `DbSet<RepairItem>` | 122 |
| RepairAttempts | `DbSet<RepairAttempt>` | 123 |
| RepairDecisions | `DbSet<RepairDecision>` | 124 |
| RepairReviewRequests | `DbSet<RepairReviewRequest>` | 125 |
| DroneDevices | `DbSet<DroneDevice>` | 127 |
| Projects | `DbSet<Project>` | 129 |
| RoadSections | `DbSet<RoadSection>` | 131 |
| RoadSectionVersions | `DbSet<RoadSectionVersion>` | 133 |
| RoadSegmentSets | `DbSet<RoadSegmentSet>` | 134 |
| RoadSegments | `DbSet<RoadSegment>` | 135 |
| Warranties | `DbSet<Warranty>` | 137 |
| SurveyPlans | `DbSet<SurveyPlan>` | 139 |
| SurveyPlanScopes | `DbSet<SurveyPlanScope>` | 141 |
| SurveyPlanPostponements | `DbSet<SurveyPlanPostponement>` | 143 |
| SurveyRequests | `DbSet<SurveyRequest>` | 145 |
| SurveyRequestScopes | `DbSet<SurveyRequestScope>` | 147 |
| Surveys | `DbSet<Survey>` | 149 |
| SurveyAssignments | `DbSet<SurveyAssignment>` | 151 |
| Flights | `DbSet<Flight>` | 153 |
| SurveyFiles | `DbSet<SurveyFile>` | 155 |
| SurveyDataVersions | `DbSet<SurveyDataVersion>` | 157 |
| QualityChecks | `DbSet<QualityCheck>` | 159 |
| SupplementarySurveyRequests | `DbSet<SupplementarySurveyRequest>` | 161 |
| ProcessingBlocks | `DbSet<ProcessingBlock>` | 163 |
| ProcessingJobs | `DbSet<ProcessingJob>` | 165 |
| ProcessingAttempts | `DbSet<ProcessingAttempt>` | 167 |
| AIModelVersions | `DbSet<AIModelVersion>` | 169 |
| ValidationRuns | `DbSet<ValidationRun>` | 171 |
| DerivedMeasurements | `DbSet<DerivedMeasurement>` | 173 |
| MeasurementValidationSamples | `DbSet<MeasurementValidationSample>` | 175 |
| ProjectMembers | `DbSet<ProjectMember>` | 177 |
| HandoverDocuments | `DbSet<HandoverDocument>` | 179 |

## dbo.AIDetections

Entity: `RoadGuardSystem.BusinessObjects.Processing.AIDetection`; [RoadGuardSystem.BusinessObjects/Processing/AIDetection.cs:6](../../../RoadGuardSystem.BusinessObjects/Processing/AIDetection.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Confidence / Confidence | `System.Decimal` / `decimal(6,5)` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| DefectTypeCode / DefectTypeCode | `System.String` / `varchar(80)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=80 precision=None scale=None |
| EstimatedLength / EstimatedLength | `System.Nullable1[[System.Decimal, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `decimal(12,3)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| EstimatedWidth / EstimatedWidth | `System.Nullable1[[System.Decimal, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `decimal(12,3)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Geometry / Geometry | `NetTopologySuite.Geometries.Geometry` / `geometry` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ModelVersionId / ModelVersionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProcessingJobId / ProcessingJobId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RawPayload / RawPayload | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RoadSectionVersionId / RoadSectionVersionId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.AIModelVersions

Entity: `RoadGuardSystem.BusinessObjects.Processing.AIModelVersion`; [RoadGuardSystem.BusinessObjects/Processing/AIModelVersion.cs:6](../../../RoadGuardSystem.BusinessObjects/Processing/AIModelVersion.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ArtifactUri / ArtifactUri | `System.String` / `nvarchar(2048)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=2048 precision=None scale=None |
| Metrics / Metrics | `System.String` / `nvarchar(max)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ModelName / ModelName | `System.String` / `varchar(120)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=120 precision=None scale=None |
| OperatingThresholds / OperatingThresholds | `System.String` / `nvarchar(max)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ReleasedAt / ReleasedAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset(7)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ReleasedByUserId / ReleasedByUserId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Status / Status | `RoadGuardSystem.aBusinessObjects.Commons.AIModelVersionStatus` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.aBusinessObjects.Commons.AIModelVersionStatus, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| VersionLabel / VersionLabel | `System.String` / `varchar(80)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=80 precision=None scale=None |

## dbo.AccountStatusChangeLogs

Entity: `RoadGuardSystem.BusinessObjects.Identity.AccountStatusChangeLog`; [RoadGuardSystem.BusinessObjects/Identity/AccountStatusChangeLog.cs:6](../../../RoadGuardSystem.BusinessObjects/Identity/AccountStatusChangeLog.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `OnAdd`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ChangedByUserId / ChangedByUserId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CorrelationId / CorrelationId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| FromStatus / FromStatus | `RoadGuardSystem.aBusinessObjects.Commons.UserStatus` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.aBusinessObjects.Commons.UserStatus, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| HandoverReference / HandoverReference | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| OccurredAt / OccurredAt | `System.DateTimeOffset` / `datetimeoffset(7)` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Reason / Reason | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Source / Source | `System.String` / `varchar(80)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=80 precision=None scale=None |
| TargetUserId / TargetUserId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ToStatus / ToStatus | `RoadGuardSystem.aBusinessObjects.Commons.UserStatus` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.aBusinessObjects.Commons.UserStatus, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |

## dbo.Anh02AiDetectionProvenance

Entity: `RoadGuardSystem.BusinessObjects.Processing.AiDetectionProvenance`; [RoadGuardSystem.BusinessObjects/Processing/Anh02AiProvenance.cs:45](../../../RoadGuardSystem.BusinessObjects/Processing/Anh02AiProvenance.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| DetectionId / DetectionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| BoxHeight / BoxHeight | `System.Decimal` / `decimal(12,9)` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=12 scale=9 |
| BoxWidth / BoxWidth | `System.Decimal` / `decimal(12,9)` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=12 scale=9 |
| BoxX / BoxX | `System.Decimal` / `decimal(12,9)` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=12 scale=9 |
| BoxY / BoxY | `System.Decimal` / `decimal(12,9)` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=12 scale=9 |
| DerivationHash / DerivationHash | `System.String` / `varchar(128)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=128 precision=None scale=None |
| FrameFileId / FrameFileId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| FrameFileVersion / FrameFileVersion | `System.String` / `varchar(128)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=128 precision=None scale=None |
| ResultId / ResultId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RunId / RunId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SegmentId / SegmentId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SourceDurationMilliseconds / SourceDurationMilliseconds | `System.Int64` / `bigint` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SourceVideoFileId / SourceVideoFileId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SourceVideoFileVersion / SourceVideoFileVersion | `System.String` / `varchar(128)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=128 precision=None scale=None |
| TimestampMilliseconds / TimestampMilliseconds | `System.Int64` / `bigint` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.Anh02AiManifestFiles

Entity: `RoadGuardSystem.BusinessObjects.Processing.AiManifestFileReference`; [RoadGuardSystem.BusinessObjects/Processing/Anh02AiProvenance.cs:64](../../../RoadGuardSystem.BusinessObjects/Processing/Anh02AiProvenance.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| RunId / RunId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| FileId / FileId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| FileVersion / FileVersion | `System.String` / `varchar(128)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=128 precision=None scale=None |

## dbo.Anh02AiMockRuns

Entity: `RoadGuardSystem.BusinessObjects.Processing.AiMockRun`; [RoadGuardSystem.BusinessObjects/Processing/Anh02AiProvenance.cs:5](../../../RoadGuardSystem.BusinessObjects/Processing/Anh02AiProvenance.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AnalysisRunId / AnalysisRunId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AttemptId / AttemptId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CanonicalManifest / CanonicalManifest | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CompletedAt / CompletedAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CreatedAt / CreatedAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CreatedBy / CreatedBy | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| DatasetVersionId / DatasetVersionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ErrorCode / ErrorCode | `System.String` / `varchar(80)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=80 precision=None scale=None |
| FixtureVersion / FixtureVersion | `System.String` / `varchar(128)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=128 precision=None scale=None |
| GeometryVersion / GeometryVersion | `System.String` / `varchar(128)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=128 precision=None scale=None |
| LeaseOwner / LeaseOwner | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| LeaseUntil / LeaseUntil | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ManifestHash / ManifestHash | `System.String` / `varchar(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| ModelVersionId / ModelVersionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProcessingJobId / ProcessingJobId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ResultId / ResultId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ResultTimestamp / ResultTimestamp | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RouteVersionId / RouteVersionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RowVersion / RowVersion | `System.Byte[]` / `rowversion` | False | True | `OnAddOrUpdate`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SegmentSetId / SegmentSetId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Stage / Stage | `System.String` / `varchar(24)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=24 precision=None scale=None |
| Status / Status | `System.String` / `varchar(24)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=24 precision=None scale=None |

## dbo.Anh02AiResultProvenance

Entity: `RoadGuardSystem.BusinessObjects.Processing.AiResultProvenance`; [RoadGuardSystem.BusinessObjects/Processing/Anh02AiProvenance.cs:33](../../../RoadGuardSystem.BusinessObjects/Processing/Anh02AiProvenance.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AttemptId / AttemptId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CanonicalResult / CanonicalResult | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CompletedAt / CompletedAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ManifestHash / ManifestHash | `System.String` / `varchar(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| ProcessingJobId / ProcessingJobId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ResultHash / ResultHash | `System.String` / `varchar(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| RunId / RunId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.Anh02ExportJobs

Entity: `RoadGuardSystem.BusinessObjects.Exports.ExportJob`; [RoadGuardSystem.BusinessObjects/Exports/ExportRecords.cs:3](../../../RoadGuardSystem.BusinessObjects/Exports/ExportRecords.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ArtifactId / ArtifactId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CompletedAt / CompletedAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CreatedAt / CreatedAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ErrorCode / ErrorCode | `System.String` / `varchar(80)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=80 precision=None scale=None |
| ExpiresAt / ExpiresAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Format / Format | `System.String` / `varchar(8)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=8 precision=None scale=None |
| Kind / Kind | `System.String` / `varchar(16)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=16 precision=None scale=None |
| LeaseToken / LeaseToken | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| LeaseUntil / LeaseUntil | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| NextAttemptAt / NextAttemptAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RequestedBy / RequestedBy | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RowVersion / RowVersion | `System.Byte[]` / `rowversion` | False | True | `OnAddOrUpdate`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SnapshotId / SnapshotId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Status / Status | `System.String` / `varchar(16)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=16 precision=None scale=None |

## dbo.Anh02ExportSnapshotFiles

Entity: `RoadGuardSystem.BusinessObjects.Exports.ExportSnapshotFile`; [RoadGuardSystem.BusinessObjects/Exports/ExportRecords.cs:63](../../../RoadGuardSystem.BusinessObjects/Exports/ExportRecords.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ArchivePath / ArchivePath | `System.String` / `nvarchar(255)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=255 precision=None scale=None |
| FileId / FileId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| FileVersion / FileVersion | `System.String` / `nvarchar(128)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=128 precision=None scale=None |
| Included / Included | `System.Boolean` / `bit` | False | False | `Never`; defaultSQL=`None`; default=`False`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| MediaType / MediaType | `System.String` / `nvarchar(120)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=120 precision=None scale=None |
| Sha256 / Sha256 | `System.String` / `char(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SizeBytes / SizeBytes | `System.Int64` / `bigint` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SnapshotId / SnapshotId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.Anh02ExportSnapshots

Entity: `RoadGuardSystem.BusinessObjects.Exports.ExportSnapshot`; [RoadGuardSystem.BusinessObjects/Exports/ExportRecords.cs:48](../../../RoadGuardSystem.BusinessObjects/Exports/ExportRecords.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CapturedAt / CapturedAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Hash / Hash | `System.String` / `char(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| PayloadJson / PayloadJson | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.Anh02GeneratedArtifacts

Entity: `RoadGuardSystem.BusinessObjects.Exports.GeneratedArtifact`; [RoadGuardSystem.BusinessObjects/Exports/ExportRecords.cs:79](../../../RoadGuardSystem.BusinessObjects/Exports/ExportRecords.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CreatedAt / CreatedAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ExportJobId / ExportJobId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| FileId / FileId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SnapshotId / SnapshotId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.AuditLogs

Entity: `RoadGuardSystem.BusinessObjects.Auditing.AuditLog`; [RoadGuardSystem.BusinessObjects/Auditing/AuditLog.cs:5](../../../RoadGuardSystem.BusinessObjects/Auditing/AuditLog.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `OnAdd`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ActorUserId / ActorUserId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AfterSnapshot / AfterSnapshot | `System.String` / `nvarchar(max)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| BeforeSnapshot / BeforeSnapshot | `System.String` / `nvarchar(max)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CorrelationId / CorrelationId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| EntityId / EntityId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| EntityType / EntityType | `System.String` / `varchar(100)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=100 precision=None scale=None |
| EventType / EventType | `System.String` / `varchar(100)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=100 precision=None scale=None |
| OccurredAtUtc / OccurredAtUtc | `System.DateTimeOffset` / `datetimeoffset(7)` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Reason / Reason | `System.String` / `nvarchar(max)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Source / Source | `System.String` / `varchar(80)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=80 precision=None scale=None |

## dbo.BaselineCurrentPointers

Entity: `RoadGuardSystem.BusinessObjects.Surveys.BaselineCurrentPointer`; [RoadGuardSystem.BusinessObjects/Surveys/DatasetAssessment.cs:84](../../../RoadGuardSystem.BusinessObjects/Surveys/DatasetAssessment.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RouteVersionId / RouteVersionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RowVersion / RowVersion | `System.Byte[]` / `rowversion` | False | True | `OnAddOrUpdate`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SegmentId / SegmentId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SegmentSetId / SegmentSetId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SelectionId / SelectionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| TargetBand / TargetBand | `System.String` / `nvarchar(20)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=20 precision=None scale=None |

## dbo.BaselineSelectionItems

Entity: `RoadGuardSystem.BusinessObjects.Surveys.BaselineSelectionItem`; [RoadGuardSystem.BusinessObjects/Surveys/DatasetAssessment.cs:60](../../../RoadGuardSystem.BusinessObjects/Surveys/DatasetAssessment.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AssessmentId / AssessmentId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| BaselineSelectionId / BaselineSelectionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| DatasetId / DatasetId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RouteVersionId / RouteVersionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SegmentId / SegmentId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SegmentSetId / SegmentSetId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| TargetBand / TargetBand | `System.String` / `nvarchar(20)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=20 precision=None scale=None |

## dbo.BaselineSelections

Entity: `RoadGuardSystem.BusinessObjects.Surveys.BaselineSelection`; [RoadGuardSystem.BusinessObjects/Surveys/DatasetAssessment.cs:47](../../../RoadGuardSystem.BusinessObjects/Surveys/DatasetAssessment.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Reason / Reason | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RowVersion / RowVersion | `System.Byte[]` / `rowversion` | False | True | `OnAddOrUpdate`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SelectedAt / SelectedAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SelectedBy / SelectedBy | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.BusinessDutyAppointments

Entity: `RoadGuardSystem.BusinessObjects.Clocks.BusinessDutyAppointment`; [RoadGuardSystem.BusinessObjects/Clocks/BusinessReceivingRequest.cs:84](../../../RoadGuardSystem.BusinessObjects/Clocks/BusinessReceivingRequest.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CurrentActorId / CurrentActorId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| DecisionActorId / DecisionActorId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| EffectiveAt / EffectiveAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| PreviousActorId / PreviousActorId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Reason / Reason | `System.String` / `nvarchar(2000)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=2000 precision=None scale=None |
| RequestId / RequestId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.BusinessReceivingRequests

Entity: `RoadGuardSystem.BusinessObjects.Clocks.BusinessReceivingRequest`; [RoadGuardSystem.BusinessObjects/Clocks/BusinessReceivingRequest.cs:5](../../../RoadGuardSystem.BusinessObjects/Clocks/BusinessReceivingRequest.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AcknowledgedAt / AcknowledgedAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AcknowledgedBy / AcknowledgedBy | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AcknowledgmentId / AcknowledgmentId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ClaimedDeviceAt / ClaimedDeviceAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ClockId / ClockId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CompletedAt / CompletedAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Kind / Kind | `RoadGuardSystem.BusinessObjects.Clocks.DeadlineClockKind` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.BusinessObjects.Clocks.DeadlineClockKind, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RequestedAt / RequestedAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ResponsibleActorId / ResponsibleActorId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ResponsibleRole / ResponsibleRole | `RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| RowVersion / RowVersion | `System.Byte[]` / `rowversion` | False | True | `OnAddOrUpdate`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ScopeId / ScopeId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SourceId / SourceId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SourceKind / SourceKind | `System.String` / `nvarchar(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| SourceVersion / SourceVersion | `System.String` / `nvarchar(256)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=256 precision=None scale=None |

## dbo.CandidateSourceHeads

Entity: `RoadGuardSystem.Repositories.Models.Huy01.HuyCandidateSourceHead`; [RoadGuardSystem.Repositories/Models/Huy01/HuyIntegrationRows.cs:33](../../../RoadGuardSystem.Repositories/Models/Huy01/HuyIntegrationRows.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| SourceKind / SourceKind | `RoadGuardSystem.BusinessObjects.Candidates.CandidateSourceKind` / `int` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.BusinessObjects.Candidates.CandidateSourceKind, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Int32, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Int32`; max=None precision=None scale=None |
| SourceId / SourceId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| DecisionId / DecisionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RowVersion / RowVersion | `System.Byte[]` / `rowversion` | False | True | `OnAddOrUpdate`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.CaseConclusionDefects

Entity: `RoadGuardSystem.Repositories.Models.Huy01.HuyConclusionDefect`; [RoadGuardSystem.Repositories/Models/Huy01/HuyIntegrationRows.cs:57](../../../RoadGuardSystem.Repositories/Models/Huy01/HuyIntegrationRows.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| ConclusionId / ConclusionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| DefectId / DefectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.CaseConclusionEvidence

Entity: `RoadGuardSystem.Repositories.Models.Huy01.HuyConclusionEvidence`; [RoadGuardSystem.Repositories/Models/Huy01/HuyIntegrationRows.cs:69](../../../RoadGuardSystem.Repositories/Models/Huy01/HuyIntegrationRows.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| ConclusionId / ConclusionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| EvidenceId / EvidenceId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| OriginalEvidenceId / OriginalEvidenceId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SourceReportId / SourceReportId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SupplementEvidenceId / SupplementEvidenceId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.CaseConclusions

Entity: `RoadGuardSystem.BusinessObjects.Cases.CaseConclusion`; [RoadGuardSystem.BusinessObjects/Cases/CaseConclusion.cs:3](../../../RoadGuardSystem.BusinessObjects/Cases/CaseConclusion.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CaseId / CaseId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ConcludedAt / ConcludedAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Outcome / Outcome | `RoadGuardSystem.BusinessObjects.Cases.CaseConclusionOutcome` / `int` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.BusinessObjects.Cases.CaseConclusionOutcome, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Int32, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Int32`; max=None precision=None scale=None |
| Reason / Reason | `System.String` / `nvarchar(1000)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=1000 precision=None scale=None |
| _defectIds / DefectIdsJson | `System.Collections.Generic.List1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter2[[System.Collections.Generic.List1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.String, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.String`; max=None precision=None scale=None |
| _evidenceIds / EvidenceIdsJson | `System.Collections.Generic.List1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter2[[System.Collections.Generic.List1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.String, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.String`; max=None precision=None scale=None |

## dbo.CasePublicationDefects

Entity: `RoadGuardSystem.Repositories.Models.Huy01.HuyPublicationDefect`; [RoadGuardSystem.Repositories/Models/Huy01/HuyIntegrationRows.cs:63](../../../RoadGuardSystem.Repositories/Models/Huy01/HuyIntegrationRows.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| PublicationId / PublicationId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| DefectId / DefectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.CasePublicationEvidence

Entity: `RoadGuardSystem.Repositories.Models.Huy01.HuyPublicationEvidence`; [RoadGuardSystem.Repositories/Models/Huy01/HuyIntegrationRows.cs:23](../../../RoadGuardSystem.Repositories/Models/Huy01/HuyIntegrationRows.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| PublicationId / PublicationId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RecipientReportId / RecipientReportId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| EvidenceId / EvidenceId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| OriginalEvidenceId / OriginalEvidenceId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SourceReportId / SourceReportId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SupplementEvidenceId / SupplementEvidenceId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.CasePublicationRecipients

Entity: `RoadGuardSystem.Repositories.Models.Huy01.HuyPublicationRecipient`; [RoadGuardSystem.Repositories/Models/Huy01/HuyIntegrationRows.cs:17](../../../RoadGuardSystem.Repositories/Models/Huy01/HuyIntegrationRows.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| PublicationId / PublicationId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ReportId / ReportId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.CasePublications

Entity: `RoadGuardSystem.BusinessObjects.Cases.CasePublication`; [RoadGuardSystem.BusinessObjects/Cases/CasePublication.cs:3](../../../RoadGuardSystem.BusinessObjects/Cases/CasePublication.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CaseId / CaseId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| PublishedAt / PublishedAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Summary / Summary | `System.String` / `nvarchar(1000)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=1000 precision=None scale=None |
| _defectIds / DefectIdsJson | `System.Collections.Generic.List1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter2[[System.Collections.Generic.List1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.String, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.String`; max=None precision=None scale=None |
| _evidenceIds / EvidenceIdsJson | `System.Collections.Generic.List1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter2[[System.Collections.Generic.List1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.String, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.String`; max=None precision=None scale=None |
| _recipientReportIds / RecipientReportIdsJson | `System.Collections.Generic.List1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter2[[System.Collections.Generic.List1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.String, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.String`; max=None precision=None scale=None |

## dbo.CaseReportLinkHistory

Entity: `RoadGuardSystem.BusinessObjects.Cases.CaseReportLinkHistory`; [RoadGuardSystem.BusinessObjects/Cases/CaseReportLinkHistory.cs:3](../../../RoadGuardSystem.BusinessObjects/Cases/CaseReportLinkHistory.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ActorUserId / ActorUserId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| FromCaseId / FromCaseId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| OccurredAt / OccurredAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Reason / Reason | `System.String` / `nvarchar(1000)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=1000 precision=None scale=None |
| ToCaseId / ToCaseId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| _reportIds / ReportIdsJson | `System.Collections.Generic.List1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter2[[System.Collections.Generic.List1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.String, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.String`; max=None precision=None scale=None |

## dbo.CaseReportLinkHistoryReports

Entity: `RoadGuardSystem.Repositories.Models.Huy01.HuyLinkHistoryReport`; [RoadGuardSystem.Repositories/Models/Huy01/HuyIntegrationRows.cs:78](../../../RoadGuardSystem.Repositories/Models/Huy01/HuyIntegrationRows.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| HistoryId / HistoryId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ReportId / ReportId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.CaseReportLinks

Entity: `RoadGuardSystem.Repositories.Models.Huy01.HuyCaseReportLink`; [RoadGuardSystem.Repositories/Models/Huy01/HuyIntegrationRows.cs:7](../../../RoadGuardSystem.Repositories/Models/Huy01/HuyIntegrationRows.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CaseId / CaseId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| EndedAt / EndedAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ReportId / ReportId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RowVersion / RowVersion | `System.Byte[]` / `rowversion` | False | True | `OnAddOrUpdate`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| StartedAt / StartedAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.CauseCategories

Entity: `RoadGuardSystem.BusinessObjects.Catalogs.CauseCategory`; [RoadGuardSystem.BusinessObjects/Catalogs/CauseCategory.cs:3](../../../RoadGuardSystem.BusinessObjects/Catalogs/CauseCategory.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Code / Code | `System.String` / `varchar(80)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=80 precision=None scale=None |
| IsActive / IsActive | `System.Boolean` / `bit` | False | False | `Never`; defaultSQL=`None`; default=`False`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Name / Name | `System.String` / `nvarchar(150)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=150 precision=None scale=None |

## dbo.ConsumerEffectReceipts

Entity: `RoadGuardSystem.BusinessObjects.Messaging.ConsumerEffectReceipt`; [RoadGuardSystem.BusinessObjects/Messaging/ConsumerEffectReceipt.cs:3](../../../RoadGuardSystem.BusinessObjects/Messaging/ConsumerEffectReceipt.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `OnAdd`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ConsumerName / ConsumerName | `System.String` / `varchar(100)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=100 precision=None scale=None |
| EffectId / EffectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| MessageId / MessageId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProcessedAtUtc / ProcessedAtUtc | `System.DateTimeOffset` / `datetimeoffset(7)` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.CrsProfileRevisions

Entity: `RoadGuardSystem.BusinessObjects.Projects.CrsProfileRevision`; [RoadGuardSystem.BusinessObjects/Projects/CrsProfileRevision.cs:3](../../../RoadGuardSystem.BusinessObjects/Projects/CrsProfileRevision.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Code / Code | `System.String` / `nvarchar(80)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=80 precision=None scale=None |
| CreatedAt / CreatedAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CreatedBy / CreatedBy | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| PayloadJson / PayloadJson | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Revision / Revision | `System.Int32` / `int` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SampleOnly / SampleOnly | `System.Boolean` / `bit` | False | False | `Never`; defaultSQL=`None`; default=`False`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SourceSrid / SourceSrid | `System.Int32` / `int` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Status / Status | `System.String` / `nvarchar(16)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=16 precision=None scale=None |

## dbo.DatasetAssessmentItems

Entity: `RoadGuardSystem.BusinessObjects.Surveys.DatasetAssessmentItem`; [RoadGuardSystem.BusinessObjects/Surveys/DatasetAssessment.cs:16](../../../RoadGuardSystem.BusinessObjects/Surveys/DatasetAssessment.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AssessmentId / AssessmentId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CoverageStatus / CoverageStatus | `System.String` / `nvarchar(10)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=10 precision=None scale=None |
| EvidenceJson / EvidenceJson | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| PositionStatus / PositionStatus | `System.String` / `nvarchar(10)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=10 precision=None scale=None |
| QualityStatus / QualityStatus | `System.String` / `nvarchar(10)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=10 precision=None scale=None |
| Reason / Reason | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RouteVersionId / RouteVersionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SegmentId / SegmentId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SegmentSetId / SegmentSetId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| TargetBand / TargetBand | `System.String` / `nvarchar(20)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=20 precision=None scale=None |

## dbo.DatasetAssessments

Entity: `RoadGuardSystem.BusinessObjects.Surveys.DatasetAssessment`; [RoadGuardSystem.BusinessObjects/Surveys/DatasetAssessment.cs:3](../../../RoadGuardSystem.BusinessObjects/Surveys/DatasetAssessment.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| DatasetId / DatasetId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| MethodVersion / MethodVersion | `System.String` / `nvarchar(80)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=80 precision=None scale=None |
| ReviewedAt / ReviewedAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ReviewedBy / ReviewedBy | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RowVersion / RowVersion | `System.Byte[]` / `rowversion` | False | True | `OnAddOrUpdate`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.DeadlineBreaches

Entity: `RoadGuardSystem.BusinessObjects.Clocks.DeadlineBreach`; [RoadGuardSystem.BusinessObjects/Clocks/DeadlineClock.cs:170](../../../RoadGuardSystem.BusinessObjects/Clocks/DeadlineClock.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ClockId / ClockId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| DueAt / DueAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ObservedAt / ObservedAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.DeadlineClocks

Entity: `RoadGuardSystem.BusinessObjects.Clocks.DeadlineClock`; [RoadGuardSystem.BusinessObjects/Clocks/DeadlineClock.cs:12](../../../RoadGuardSystem.BusinessObjects/Clocks/DeadlineClock.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AcknowledgedAt / AcknowledgedAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AcknowledgedByUserId / AcknowledgedByUserId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AcknowledgmentEventId / AcknowledgmentEventId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AppointedActorId / AppointedActorId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AppointedRole / AppointedRole | `System.Nullable1[[RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]` / `tinyint` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| CompletedAt / CompletedAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CurrentDueAt / CurrentDueAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Kind / Kind | `RoadGuardSystem.BusinessObjects.Clocks.DeadlineClockKind` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.BusinessObjects.Clocks.DeadlineClockKind, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| OriginAt / OriginAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| OriginEventId / OriginEventId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| OriginalDueAt / OriginalDueAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RowVersion / RowVersion | `System.Byte[]` / `rowversion` | False | True | `OnAddOrUpdate`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| TargetId / TargetId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.DeadlineDutyAppointments

Entity: `RoadGuardSystem.BusinessObjects.Clocks.DeadlineDutyAppointment`; [RoadGuardSystem.BusinessObjects/Clocks/DeadlineClock.cs:178](../../../RoadGuardSystem.BusinessObjects/Clocks/DeadlineClock.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ClockId / ClockId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CurrentActorId / CurrentActorId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| DecisionActorId / DecisionActorId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| EffectiveAt / EffectiveAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| PreviousActorId / PreviousActorId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Reason / Reason | `System.String` / `nvarchar(2000)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=2000 precision=None scale=None |
| Role / Role | `RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |

## dbo.DeadlineExtensions

Entity: `RoadGuardSystem.BusinessObjects.Clocks.DeadlineExtension`; [RoadGuardSystem.BusinessObjects/Clocks/DeadlineClock.cs:158](../../../RoadGuardSystem.BusinessObjects/Clocks/DeadlineClock.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ActorUserId / ActorUserId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ClockId / ClockId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| NewDueAt / NewDueAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| OccurredAt / OccurredAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| PreviousDeadlineBreached / PreviousDeadlineBreached | `System.Boolean` / `bit` | False | False | `Never`; defaultSQL=`None`; default=`False`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| PreviousDueAt / PreviousDueAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Reason / Reason | `System.String` / `nvarchar(2000)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=2000 precision=None scale=None |

## dbo.DefectSourceLinks

Entity: `RoadGuardSystem.Repositories.Models.Huy01.HuyDefectSourceLink`; [RoadGuardSystem.Repositories/Models/Huy01/HuyIntegrationRows.cs:42](../../../RoadGuardSystem.Repositories/Models/Huy01/HuyIntegrationRows.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AIDetectionSourceId / AIDetectionSourceId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CreatedAt / CreatedAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| DecisionId / DecisionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| DefectId / DefectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| EndedAt / EndedAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ReportSourceId / ReportSourceId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RowVersion / RowVersion | `System.Byte[]` / `rowversion` | False | True | `OnAddOrUpdate`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SourceId / SourceId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SourceKind / SourceKind | `RoadGuardSystem.BusinessObjects.Candidates.CandidateSourceKind` / `int` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.BusinessObjects.Candidates.CandidateSourceKind, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Int32, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Int32`; max=None precision=None scale=None |

## dbo.DefectStatisticsSources

Entity: `RoadGuardSystem.BusinessObjects.Reporting.DefectStatisticsSource`; [RoadGuardSystem.BusinessObjects/Reporting/DefectStatisticsSource.cs:4](../../../RoadGuardSystem.BusinessObjects/Reporting/DefectStatisticsSource.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ActorId / ActorId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ConfirmedAtUtc / ConfirmedAtUtc | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| DefectId / DefectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| DefectVersion / DefectVersion | `System.String` / `varchar(100)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=100 precision=None scale=None |
| From / From | `System.Decimal` / `decimal(18,3)` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=18 scale=3 |
| LocationVersion / LocationVersion | `System.String` / `nvarchar(200)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=200 precision=None scale=None |
| ObligationId / ObligationId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| OffsetFrom / OffsetFrom | `System.Decimal` / `decimal(18,3)` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=18 scale=3 |
| OffsetTo / OffsetTo | `System.Decimal` / `decimal(18,3)` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=18 scale=3 |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Provenance / Provenance | `System.String` / `varchar(20)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=20 precision=None scale=None |
| QuantitiesJson / QuantitiesJson | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Reason / Reason | `System.String` / `nvarchar(2000)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=2000 precision=None scale=None |
| RoadSectionId / RoadSectionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RouteVersionId / RouteVersionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ScopeHash / ScopeHash | `System.String` / `varchar(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| SegmentIdsJson / SegmentIdsJson | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SharedPartId / SharedPartId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SourceFactsJson / SourceFactsJson | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SupersedesId / SupersedesId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| To / To | `System.Decimal` / `decimal(18,3)` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=18 scale=3 |

## dbo.DefectTypes

Entity: `RoadGuardSystem.BusinessObjects.Catalogs.DefectType`; [RoadGuardSystem.BusinessObjects/Catalogs/DefectType.cs:3](../../../RoadGuardSystem.BusinessObjects/Catalogs/DefectType.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Code / Code | `System.String` / `varchar(80)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=80 precision=None scale=None |
| Description / Description | `System.String` / `nvarchar(max)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| IsActive / IsActive | `System.Boolean` / `bit` | False | False | `Never`; defaultSQL=`None`; default=`False`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Name / Name | `System.String` / `nvarchar(150)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=150 precision=None scale=None |

## dbo.DefectVerificationLogs

Entity: `RoadGuardSystem.BusinessObjects.Defects.DefectVerificationLog`; [RoadGuardSystem.BusinessObjects/Defects/DefectVerificationLog.cs:6](../../../RoadGuardSystem.BusinessObjects/Defects/DefectVerificationLog.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AIDetectionId / AIDetectionId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Action / Action | `RoadGuardSystem.aBusinessObjects.Commons.DefectVerificationAction` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.aBusinessObjects.Commons.DefectVerificationAction, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| AfterSnapshot / AfterSnapshot | `System.String` / `nvarchar(max)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| BeforeSnapshot / BeforeSnapshot | `System.String` / `nvarchar(max)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| DefectId / DefectId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| FieldInspectionTaskId / FieldInspectionTaskId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Reason / Reason | `System.String` / `nvarchar(1000)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SeverityRuleVersionId / SeverityRuleVersionId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| VerifiedByUserId / VerifiedByUserId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.Defects

Entity: `RoadGuardSystem.BusinessObjects.Defects.Defect`; [RoadGuardSystem.BusinessObjects/Defects/Defect.cs:8](../../../RoadGuardSystem.BusinessObjects/Defects/Defect.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CauseCategoryCode / CauseCategoryCode | `System.String` / `varchar(80)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=80 precision=None scale=None |
| DefectTypeCode / DefectTypeCode | `System.String` / `varchar(80)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=80 precision=None scale=None |
| Geometry / Geometry | `NetTopologySuite.Geometries.Geometry` / `geometry` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ReportedAt / ReportedAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset(7)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RoadSectionVersionId / RoadSectionVersionId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RowVersion / RowVersion | `System.Byte[]` / `rowversion` | True | True | `OnAddOrUpdate`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Severity / Severity | `RoadGuardSystem.aBusinessObjects.Commons.DefectSeverity` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.aBusinessObjects.Commons.DefectSeverity, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| SourceAIDetectionId / SourceAIDetectionId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Status / Status | `RoadGuardSystem.aBusinessObjects.Commons.DefectStatus` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.aBusinessObjects.Commons.DefectStatus, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |

## dbo.DerivedMeasurements

Entity: `RoadGuardSystem.BusinessObjects.Processing.DerivedMeasurement`; [RoadGuardSystem.BusinessObjects/Processing/DerivedMeasurement.cs:5](../../../RoadGuardSystem.BusinessObjects/Processing/DerivedMeasurement.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AlgorithmVersion / AlgorithmVersion | `System.String` / `nvarchar(100)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ComputedAt / ComputedAt | `System.DateTimeOffset` / `datetimeoffset(7)` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| MeasurementType / MeasurementType | `RoadGuardSystem.aBusinessObjects.Commons.MeasurementType` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.aBusinessObjects.Commons.MeasurementType, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| RoadSectionVersionId / RoadSectionVersionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SampleId / SampleId | `System.String` / `nvarchar(100)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SourceType / SourceType | `RoadGuardSystem.aBusinessObjects.Commons.DerivedMeasurementSourceType` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.aBusinessObjects.Commons.DerivedMeasurementSourceType, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| Status / Status | `RoadGuardSystem.aBusinessObjects.Commons.DerivedMeasurementStatus` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.aBusinessObjects.Commons.DerivedMeasurementStatus, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| SurveyDataVersionId / SurveyDataVersionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| UncertaintyEstimate / UncertaintyEstimate | `System.Nullable1[[System.Decimal, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `decimal(19,6)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Unit / Unit | `System.String` / `nvarchar(20)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Value / Value | `System.Decimal` / `decimal(19,6)` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.DroneDevices

Entity: `RoadGuardSystem.BusinessObjects.Devices.DroneDevice`; [RoadGuardSystem.BusinessObjects/Devices/DroneDevice.cs:5](../../../RoadGuardSystem.BusinessObjects/Devices/DroneDevice.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ChecklistVersion / ChecklistVersion | `System.String` / `varchar(50)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=50 precision=None scale=None |
| Model / Model | `System.String` / `nvarchar(120)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=120 precision=None scale=None |
| SerialNo / SerialNo | `System.String` / `varchar(120)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=120 precision=None scale=None |
| Status / Status | `RoadGuardSystem.aBusinessObjects.Commons.DroneDeviceStatus` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.aBusinessObjects.Commons.DroneDeviceStatus, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |

## dbo.FieldInspectionAssignments

Entity: `RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionAssignment`; [RoadGuardSystem.BusinessObjects/Inspections/FieldInspectionAssignment.cs:5](../../../RoadGuardSystem.BusinessObjects/Inspections/FieldInspectionAssignment.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AssignedAt / AssignedAt | `System.DateTimeOffset` / `datetimeoffset(7)` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AssignedByUserId / AssignedByUserId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AssignedToUserId / AssignedToUserId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| EndedAt / EndedAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset(7)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| FieldInspectionTaskId / FieldInspectionTaskId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Reason / Reason | `System.String` / `nvarchar(1000)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Status / Status | `RoadGuardSystem.aBusinessObjects.Commons.FieldInspectionAssignmentStatus` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.aBusinessObjects.Commons.FieldInspectionAssignmentStatus, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |

## dbo.FieldInspectionEvidenceLinks

Entity: `RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionEvidenceLink`; [RoadGuardSystem.BusinessObjects/Inspections/FieldInspectionEvidenceLink.cs:3](../../../RoadGuardSystem.BusinessObjects/Inspections/FieldInspectionEvidenceLink.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AssignmentId / AssignmentId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CaptureFactsJson / CaptureFactsJson | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CaptureOriginId / CaptureOriginId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| DeclaredChecksum / DeclaredChecksum | `System.String` / `nvarchar(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| FileId / FileId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| MediaType / MediaType | `System.String` / `nvarchar(120)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=120 precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Purpose / Purpose | `System.String` / `nvarchar(20)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=20 precision=None scale=None |
| SubmissionId / SubmissionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| TaskId / TaskId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.FieldInspectionEvidenceReuseDecisions

Entity: `RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionEvidenceReuseDecision`; [RoadGuardSystem.BusinessObjects/Inspections/FieldInspectionEvidenceReuseDecision.cs:3](../../../RoadGuardSystem.BusinessObjects/Inspections/FieldInspectionEvidenceReuseDecision.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ActorId / ActorId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| FileChecksum / FileChecksum | `System.String` / `nvarchar(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| FileId / FileId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| OccurredAt / OccurredAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProvenanceJson / ProvenanceJson | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Reason / Reason | `System.String` / `nvarchar(2000)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=2000 precision=None scale=None |
| SourceEvidenceId / SourceEvidenceId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SourceKind / SourceKind | `System.String` / `nvarchar(20)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=20 precision=None scale=None |
| TaskId / TaskId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.FieldInspectionLocationProofs

Entity: `RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionLocationProof`; [RoadGuardSystem.BusinessObjects/Inspections/FieldInspectionLocationProof.cs:3](../../../RoadGuardSystem.BusinessObjects/Inspections/FieldInspectionLocationProof.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| FactsJson / FactsJson | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Kind / Kind | `System.String` / `nvarchar(30)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=30 precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SubmissionId / SubmissionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| TaskId / TaskId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| VerificationState / VerificationState | `System.String` / `nvarchar(20)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=20 precision=None scale=None |

## dbo.FieldInspectionOperationOrigins

Entity: `RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionOperationOrigin`; [RoadGuardSystem.BusinessObjects/Inspections/FieldInspectionOperationOrigin.cs:3](../../../RoadGuardSystem.BusinessObjects/Inspections/FieldInspectionOperationOrigin.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ContentHash / ContentHash | `System.String` / `nvarchar(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| DeviceId / DeviceId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| EffectId / EffectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Kind / Kind | `System.String` / `nvarchar(40)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=40 precision=None scale=None |
| OriginId / OriginId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| OriginalActorId / OriginalActorId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SchemaVersion / SchemaVersion | `System.Int32` / `int` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ServerReceivedAt / ServerReceivedAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| TaskId / TaskId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.FieldInspectionReviews

Entity: `RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionReview`; [RoadGuardSystem.BusinessObjects/Inspections/FieldInspectionReview.cs:3](../../../RoadGuardSystem.BusinessObjects/Inspections/FieldInspectionReview.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ActorId / ActorId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Decision / Decision | `System.String` / `nvarchar(20)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=20 precision=None scale=None |
| OccurredAt / OccurredAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Reason / Reason | `System.String` / `nvarchar(2000)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=2000 precision=None scale=None |
| ReceiptActivation / ReceiptActivation | `System.String` / `nvarchar(60)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=60 precision=None scale=None |
| SubmissionId / SubmissionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| TaskId / TaskId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.FieldInspectionSessions

Entity: `RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionSession`; [RoadGuardSystem.BusinessObjects/Inspections/FieldInspectionSession.cs:5](../../../RoadGuardSystem.BusinessObjects/Inspections/FieldInspectionSession.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ConductedAt / ConductedAt | `System.DateTimeOffset` / `datetimeoffset(7)` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| EvidenceFileId / EvidenceFileId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| FieldInspectionTaskId / FieldInspectionTaskId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| InspectorName / InspectorName | `System.String` / `nvarchar(200)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| InspectorUserId / InspectorUserId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Method / Method | `System.String` / `nvarchar(200)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Purpose / Purpose | `RoadGuardSystem.aBusinessObjects.Commons.FieldInspectionPurpose` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.aBusinessObjects.Commons.FieldInspectionPurpose, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| RoadSectionVersionId / RoadSectionVersionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SessionCode / SessionCode | `System.String` / `nvarchar(80)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Status / Status | `RoadGuardSystem.aBusinessObjects.Commons.FieldInspectionSessionStatus` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.aBusinessObjects.Commons.FieldInspectionSessionStatus, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| SurveyId / SurveyId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| WeatherCondition / WeatherCondition | `System.String` / `nvarchar(100)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.FieldInspectionSubmissions

Entity: `RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionSubmission`; [RoadGuardSystem.BusinessObjects/Inspections/FieldInspectionSubmission.cs:3](../../../RoadGuardSystem.BusinessObjects/Inspections/FieldInspectionSubmission.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AssignmentId / AssignmentId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ContentHash / ContentHash | `System.String` / `nvarchar(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| MissingReasonsJson / MissingReasonsJson | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| OperationOriginId / OperationOriginId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| OriginId / OriginId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| OriginalActorId / OriginalActorId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ParentId / ParentId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| PayloadJson / PayloadJson | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Readiness / Readiness | `System.String` / `nvarchar(20)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=20 precision=None scale=None |
| Revision / Revision | `System.Int32` / `int` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RootId / RootId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ServerReceivedAt / ServerReceivedAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SessionId / SessionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| StartOriginId / StartOriginId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| TaskId / TaskId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.FieldInspectionTaskEvents

Entity: `RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionTaskEvent`; [RoadGuardSystem.BusinessObjects/Inspections/FieldInspectionTaskEvent.cs:3](../../../RoadGuardSystem.BusinessObjects/Inspections/FieldInspectionTaskEvent.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ActorId / ActorId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AssignmentId / AssignmentId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| FactsJson / FactsJson | `System.String` / `nvarchar(max)` | False | False | `OnAdd`; defaultSQL=`None`; default=`{}`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Kind / Kind | `System.String` / `nvarchar(20)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=20 precision=None scale=None |
| LocationImpactDecisionId / LocationImpactDecisionId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| LocationImpactId / LocationImpactId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| OccurredAt / OccurredAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Reason / Reason | `System.String` / `nvarchar(2000)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=2000 precision=None scale=None |
| TaskId / TaskId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.FieldInspectionTasks

Entity: `RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionTask`; [RoadGuardSystem.BusinessObjects/Inspections/FieldInspectionTask.cs:6](../../../RoadGuardSystem.BusinessObjects/Inspections/FieldInspectionTask.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AssignedByUserId / AssignedByUserId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CrsProfileRevisionId / CrsProfileRevisionId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| DefectId / DefectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| DueAt / DueAt | `System.DateTimeOffset` / `datetimeoffset(7)` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Instructions / Instructions | `System.String` / `nvarchar(1000)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| LayoutRevisionId / LayoutRevisionId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| LifecycleVersion / LifecycleVersion | `System.Int32` / `int` | False | False | `OnAdd`; defaultSQL=`None`; default=`1`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| MapPublicationId / MapPublicationId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| MeasurementScope / MeasurementScope | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| MissingInformation / MissingInformation | `System.String` / `nvarchar(1000)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Purpose / Purpose | `RoadGuardSystem.aBusinessObjects.Commons.FieldInspectionPurpose` / `tinyint` | False | False | `OnAdd`; defaultSQL=`None`; default=`DefectVerification`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.aBusinessObjects.Commons.FieldInspectionPurpose, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| RepairItemId / RepairItemId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RequiredMeasurementType / RequiredMeasurementType | `System.Byte` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ReviewDecision / ReviewDecision | `System.Nullable1[[RoadGuardSystem.aBusinessObjects.Commons.FieldInspectionReviewDecision, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]` / `tinyint` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.aBusinessObjects.Commons.FieldInspectionReviewDecision, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| ReviewReason / ReviewReason | `System.String` / `nvarchar(1000)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ReviewedAt / ReviewedAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset(7)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ReviewedByUserId / ReviewedByUserId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RoadSectionVersionId / RoadSectionVersionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RowVersion / RowVersion | `System.Byte[]` / `rowversion` | False | True | `OnAddOrUpdate`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SegmentSetId / SegmentSetId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SlabId / SlabId | `System.String` / `nvarchar(160)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=160 precision=None scale=None |
| SourceKind / SourceKind | `System.String` / `nvarchar(20)` | False | False | `OnAdd`; defaultSQL=`None`; default=`SURVEY`; computed=`None`; collation=`None`; `None`; provider=`None`; max=20 precision=None scale=None |
| Status / Status | `RoadGuardSystem.aBusinessObjects.Commons.FieldInspectionTaskStatus` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.aBusinessObjects.Commons.FieldInspectionTaskStatus, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| SurveyId / SurveyId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| TaskCode / TaskCode | `System.String` / `nvarchar(80)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| TaskMode / TaskMode | `System.String` / `nvarchar(40)` | False | False | `OnAdd`; defaultSQL=`None`; default=`MEASURE_ONLY`; computed=`None`; collation=`None`; `None`; provider=`None`; max=40 precision=None scale=None |

## dbo.FieldTaskStartOrigins

Entity: `RoadGuardSystem.BusinessObjects.Inspections.FieldTaskStartOrigin`; [RoadGuardSystem.BusinessObjects/Inspections/FieldTaskStartOrigin.cs:3](../../../RoadGuardSystem.BusinessObjects/Inspections/FieldTaskStartOrigin.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AssignmentId / AssignmentId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| BootId / BootId | `System.String` / `nvarchar(200)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=200 precision=None scale=None |
| ClaimEvidenceJson / ClaimEvidenceJson | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ClaimedAt / ClaimedAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ContentHash / ContentHash | `System.String` / `nvarchar(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| CrsProfileRevisionId / CrsProfileRevisionId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| DeviceId / DeviceId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| LayoutRevisionId / LayoutRevisionId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| LocationPolicyVersion / LocationPolicyVersion | `System.String` / `nvarchar(60)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=60 precision=None scale=None |
| MapPublicationId / MapPublicationId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| MonotonicMilliseconds / MonotonicMilliseconds | `System.Nullable1[[System.Int64, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `bigint` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| OperationKind / OperationKind | `System.String` / `nvarchar(40)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=40 precision=None scale=None |
| OperationOriginId / OperationOriginId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| OriginId / OriginId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| OriginalActorId / OriginalActorId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RouteVersionId / RouteVersionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SegmentSetId / SegmentSetId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ServerReceivedAt / ServerReceivedAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SlabId / SlabId | `System.String` / `nvarchar(160)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=160 precision=None scale=None |
| TaskId / TaskId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| TimeProvenance / TimeProvenance | `System.String` / `nvarchar(40)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=40 precision=None scale=None |
| VerifiedOriginalAt / VerifiedOriginalAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.FileScopes

Entity: `RoadGuardSystem.BusinessObjects.Files.FileScope`; [RoadGuardSystem.BusinessObjects/Files/FileScope.cs:3](../../../RoadGuardSystem.BusinessObjects/Files/FileScope.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CreatedAt / CreatedAt | `System.DateTimeOffset` / `datetimeoffset(7)` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| FileId / FileId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| OwnerUserId / OwnerUserId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Purpose / Purpose | `System.String` / `varchar(40)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=40 precision=None scale=None |
| TargetId / TargetId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.Files

Entity: `RoadGuardSystem.BusinessObjects.Files.StoredFile`; [RoadGuardSystem.BusinessObjects/Files/StoredFile.cs:3](../../../RoadGuardSystem.BusinessObjects/Files/StoredFile.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Checksum / Checksum | `System.String` / `char(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| MimeType / MimeType | `System.String` / `varchar(120)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=120 precision=None scale=None |
| OriginalName / OriginalName | `System.String` / `varchar(255)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=255 precision=None scale=None |
| RetentionUntil / RetentionUntil | `System.Nullable1[[System.DateOnly, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `date` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SizeBytes / SizeBytes | `System.Int64` / `bigint` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| StorageUri / StorageUri | `System.String` / `nvarchar(2048)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=2048 precision=None scale=None |
| UploadedAt / UploadedAt | `System.DateTimeOffset` / `datetimeoffset(7)` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| UploadedByUserId / UploadedByUserId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.Flights

Entity: `RoadGuardSystem.BusinessObjects.Surveys.Flight`; [RoadGuardSystem.BusinessObjects/Surveys/Flight.cs:3](../../../RoadGuardSystem.BusinessObjects/Surveys/Flight.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| DroneDeviceId / DroneDeviceId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| EndedAt / EndedAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset(7)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| FlightNo / FlightNo | `System.String` / `varchar(80)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=80 precision=None scale=None |
| OperatorUserId / OperatorUserId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| StartedAt / StartedAt | `System.DateTimeOffset` / `datetimeoffset(7)` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SurveyId / SurveyId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.GeometryLocationImpactDecisions

Entity: `RoadGuardSystem.BusinessObjects.Projects.GeometryLocationImpactDecision`; [RoadGuardSystem.BusinessObjects/Projects/PavementPublication.cs:45](../../../RoadGuardSystem.BusinessObjects/Projects/PavementPublication.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Action / Action | `System.String` / `nvarchar(16)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=16 precision=None scale=None |
| ActorId / ActorId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ImpactId / ImpactId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| OccurredAt / OccurredAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Reason / Reason | `System.String` / `nvarchar(2000)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=2000 precision=None scale=None |
| TaskId / TaskId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.GeometryLocationImpacts

Entity: `RoadGuardSystem.BusinessObjects.Projects.GeometryLocationImpact`; [RoadGuardSystem.BusinessObjects/Projects/PavementPublication.cs:34](../../../RoadGuardSystem.BusinessObjects/Projects/PavementPublication.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AffectedReferencesJson / AffectedReferencesJson | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| NewRouteVersionId / NewRouteVersionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| PreviousRouteVersionId / PreviousRouteVersionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RecordedAt / RecordedAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RecordedBy / RecordedBy | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.GeometryMapPublications

Entity: `RoadGuardSystem.BusinessObjects.Projects.GeometryMapPublication`; [RoadGuardSystem.BusinessObjects/Projects/PavementPublication.cs:19](../../../RoadGuardSystem.BusinessObjects/Projects/PavementPublication.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ContentHash / ContentHash | `System.String` / `nvarchar(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| CrsProfileRevisionId / CrsProfileRevisionId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| LayoutRevisionId / LayoutRevisionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| PublicationMode / PublicationMode | `System.String` / `nvarchar(16)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=16 precision=None scale=None |
| PublishedAt / PublishedAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| PublishedBy / PublishedBy | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RouteVersionId / RouteVersionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SegmentSetId / SegmentSetId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SnapshotJson / SnapshotJson | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.GroundTruthMeasurements

Entity: `RoadGuardSystem.BusinessObjects.Inspections.GroundTruthMeasurement`; [RoadGuardSystem.BusinessObjects/Inspections/GroundTruthMeasurement.cs:6](../../../RoadGuardSystem.BusinessObjects/Inspections/GroundTruthMeasurement.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| DefectId / DefectId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Dimension / Dimension | `System.String` / `nvarchar(20)` | False | False | `OnAdd`; defaultSQL=`None`; default=`LENGTH`; computed=`None`; collation=`None`; `None`; provider=`None`; max=20 precision=None scale=None |
| EvidenceFileId / EvidenceFileId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| FieldInspectionSessionId / FieldInspectionSessionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| InstrumentName / InstrumentName | `System.String` / `nvarchar(150)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| InstrumentReference / InstrumentReference | `System.String` / `nvarchar(150)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Location / Location | `NetTopologySuite.Geometries.Point` / `geography` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| LocationReason / LocationReason | `System.String` / `nvarchar(1000)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=1000 precision=None scale=None |
| LocationState / LocationState | `System.String` / `nvarchar(20)` | False | False | `OnAdd`; defaultSQL=`None`; default=`CAPTURED`; computed=`None`; collation=`None`; `None`; provider=`None`; max=20 precision=None scale=None |
| MeasuredAt / MeasuredAt | `System.DateTimeOffset` / `datetimeoffset(7)` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| MeasuredBy / MeasuredBy | `System.String` / `nvarchar(200)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| MeasurementMethod / MeasurementMethod | `System.String` / `nvarchar(500)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| MeasurementType / MeasurementType | `RoadGuardSystem.aBusinessObjects.Commons.MeasurementType` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.aBusinessObjects.Commons.MeasurementType, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| Notes / Notes | `System.String` / `nvarchar(max)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RoadSectionVersionId / RoadSectionVersionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SampleId / SampleId | `System.String` / `nvarchar(100)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SurveyId / SurveyId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Unit / Unit | `System.String` / `nvarchar(20)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| UnknownReason / UnknownReason | `System.String` / `nvarchar(1000)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=1000 precision=None scale=None |
| Value / Value | `System.Nullable1[[System.Decimal, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `decimal(19,6)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ValueState / ValueState | `System.String` / `nvarchar(20)` | False | False | `OnAdd`; defaultSQL=`None`; default=`KNOWN`; computed=`None`; collation=`None`; `None`; provider=`None`; max=20 precision=None scale=None |

## dbo.H6NotificationAudits

Entity: `RoadGuardSystem.BusinessObjects.Messaging.H6NotificationAuditRow`; [RoadGuardSystem.BusinessObjects/Messaging/H6NotificationPersistenceRecords.cs:54](../../../RoadGuardSystem.BusinessObjects/Messaging/H6NotificationPersistenceRecords.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `OnAdd`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Classification / Classification | `System.String` / `varchar(40)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=40 precision=None scale=None |
| DedupKey / DedupKey | `System.String` / `varchar(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| NotificationId / NotificationId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| OutboxMessageId / OutboxMessageId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| PreviousAuditId / PreviousAuditId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ReasonCode / ReasonCode | `System.String` / `varchar(80)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=80 precision=None scale=None |
| RecordedAtUtc / RecordedAtUtc | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ResolverVersion / ResolverVersion | `System.String` / `varchar(100)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=100 precision=None scale=None |
| SourceId / SourceId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SourceKind / SourceKind | `System.String` / `varchar(80)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=80 precision=None scale=None |

## dbo.H6NotificationCalendar

Entity: `RoadGuardSystem.BusinessObjects.Messaging.H6NotificationCalendarRow`; [RoadGuardSystem.BusinessObjects/Messaging/H6NotificationPersistenceRecords.cs:79](../../../RoadGuardSystem.BusinessObjects/Messaging/H6NotificationPersistenceRecords.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `OnAdd`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ClockId / ClockId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ObservedAtUtc / ObservedAtUtc | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| OutboxMessageId / OutboxMessageId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| PlannedAtUtc / PlannedAtUtc | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RowVersion / RowVersion | `System.Byte[]` / `rowversion` | False | True | `OnAddOrUpdate`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ScheduledAtUtc / ScheduledAtUtc | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SchedulerRunId / SchedulerRunId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Status / Status | `System.String` / `varchar(40)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=40 precision=None scale=None |

## dbo.H6NotificationDeliveries

Entity: `RoadGuardSystem.BusinessObjects.Messaging.H6NotificationDeliveryRow`; [RoadGuardSystem.BusinessObjects/Messaging/H6NotificationPersistenceRecords.cs:20](../../../RoadGuardSystem.BusinessObjects/Messaging/H6NotificationPersistenceRecords.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `OnAdd`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| DeliveredAtUtc / DeliveredAtUtc | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| NextAttemptAtUtc / NextAttemptAtUtc | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| NotificationId / NotificationId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| OccurrenceId / OccurrenceId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ReasonCode / ReasonCode | `System.String` / `varchar(80)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=80 precision=None scale=None |
| RecipientKey / RecipientKey | `System.String` / `varchar(100)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=100 precision=None scale=None |
| RecipientUserId / RecipientUserId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RowVersion / RowVersion | `System.Byte[]` / `rowversion` | False | True | `OnAddOrUpdate`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Status / Status | `System.String` / `varchar(24)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=24 precision=None scale=None |

## dbo.H6NotificationDeliveryAttempts

Entity: `RoadGuardSystem.BusinessObjects.Messaging.H6NotificationDeliveryAttempt`; [RoadGuardSystem.BusinessObjects/Messaging/H6NotificationPersistenceRecords.cs:33](../../../RoadGuardSystem.BusinessObjects/Messaging/H6NotificationPersistenceRecords.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `OnAdd`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| DeliveryId / DeliveryId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ObservedAtUtc / ObservedAtUtc | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ReasonCode / ReasonCode | `System.String` / `varchar(80)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=80 precision=None scale=None |
| Status / Status | `System.String` / `varchar(24)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=24 precision=None scale=None |

## dbo.H6NotificationEventReceipts

Entity: `RoadGuardSystem.BusinessObjects.Messaging.H6NotificationEventReceipt`; [RoadGuardSystem.BusinessObjects/Messaging/H6NotificationPersistenceRecords.cs:69](../../../RoadGuardSystem.BusinessObjects/Messaging/H6NotificationPersistenceRecords.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| OutboxMessageId / OutboxMessageId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CompletionFence / CompletionFence | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| MessageType / MessageType | `System.String` / `varchar(200)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=200 precision=None scale=None |
| OccurrenceId / OccurrenceId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| PayloadHash / PayloadHash | `System.String` / `varchar(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| RecordedAtUtc / RecordedAtUtc | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Status / Status | `System.String` / `varchar(24)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=24 precision=None scale=None |

## dbo.H6NotificationOccurrences

Entity: `RoadGuardSystem.BusinessObjects.Messaging.H6NotificationOccurrenceRow`; [RoadGuardSystem.BusinessObjects/Messaging/H6NotificationPersistenceRecords.cs:5](../../../RoadGuardSystem.BusinessObjects/Messaging/H6NotificationPersistenceRecords.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `OnAdd`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ContentFingerprint / ContentFingerprint | `System.String` / `varchar(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| EventType / EventType | `System.String` / `varchar(200)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=200 precision=None scale=None |
| OccurredAtUtc / OccurredAtUtc | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| OccurrenceKey / OccurrenceKey | `System.String` / `varchar(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| OriginEventId / OriginEventId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| PayloadJson / PayloadJson | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ScheduledAtUtc / ScheduledAtUtc | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SourceEventId / SourceEventId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SourceId / SourceId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SourceKind / SourceKind | `System.String` / `varchar(80)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=80 precision=None scale=None |

## dbo.H6NotificationScopes

Entity: `RoadGuardSystem.BusinessObjects.Messaging.H6NotificationScopeRow`; [RoadGuardSystem.BusinessObjects/Messaging/H6NotificationPersistenceRecords.cs:41](../../../RoadGuardSystem.BusinessObjects/Messaging/H6NotificationPersistenceRecords.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| NotificationId / NotificationId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Classification / Classification | `System.String` / `varchar(40)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=40 precision=None scale=None |
| CurrentAuditId / CurrentAuditId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| EventType / EventType | `System.String` / `varchar(200)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=200 precision=None scale=None |
| OccurrenceId / OccurrenceId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ResolverVersion / ResolverVersion | `System.String` / `varchar(100)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=100 precision=None scale=None |
| RowVersion / RowVersion | `System.Byte[]` / `rowversion` | False | True | `OnAddOrUpdate`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SourceId / SourceId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SourceKind / SourceKind | `System.String` / `varchar(80)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=80 precision=None scale=None |

## dbo.HandoverDocuments

Entity: `RoadGuardSystem.BusinessObjects.Projects.HandoverDocument`; [RoadGuardSystem.BusinessObjects/Projects/HandoverDocument.cs:4](../../../RoadGuardSystem.BusinessObjects/Projects/HandoverDocument.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AcceptedByUserId / AcceptedByUserId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| DocumentNo / DocumentNo | `System.String` / `varchar(80)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=80 precision=None scale=None |
| FileId / FileId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| HandoverDate / HandoverDate | `System.DateOnly` / `date` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Notes / Notes | `System.String` / `nvarchar(max)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RowVersion / RowVersion | `System.Byte[]` / `rowversion` | False | True | `OnAddOrUpdate`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.IdempotencyRecords

Entity: `RoadGuardSystem.BusinessObjects.Idempotency.IdempotencyRecord`; [RoadGuardSystem.BusinessObjects/Idempotency/IdempotencyRecord.cs:6](../../../RoadGuardSystem.BusinessObjects/Idempotency/IdempotencyRecord.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `OnAdd`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ActorUserId / ActorUserId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CreatedAtUtc / CreatedAtUtc | `System.DateTimeOffset` / `datetimeoffset(7)` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| IdempotencyKey / IdempotencyKey | `System.String` / `varchar(200)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=200 precision=None scale=None |
| Operation / Operation | `System.String` / `varchar(100)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=100 precision=None scale=None |
| OperationId / OperationId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| OutcomeJson / OutcomeJson | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RequestFingerprint / RequestFingerprint | `System.String` / `char(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |

## dbo.IncidentCases

Entity: `RoadGuardSystem.BusinessObjects.Cases.IncidentCase`; [RoadGuardSystem.BusinessObjects/Cases/IncidentCase.cs:3](../../../RoadGuardSystem.BusinessObjects/Cases/IncidentCase.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CreatedAt / CreatedAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| GeometryRouteVersionId / GeometryRouteVersionId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| GeometrySegmentSetId / GeometrySegmentSetId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| LinkedTargetCaseId / LinkedTargetCaseId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Revision / Revision | `System.Int64` / `bigint` | False | False | `OnAdd`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RowVersion / RowVersion | `System.Byte[]` / `rowversion` | True | True | `OnAddOrUpdate`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Status / Status | `RoadGuardSystem.BusinessObjects.Cases.IncidentCaseStatus` / `int` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.BusinessObjects.Cases.IncidentCaseStatus, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Int32, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Int32`; max=None precision=None scale=None |
| TriageReason / TriageReason | `System.String` / `nvarchar(1000)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=1000 precision=None scale=None |
| TriagedAt / TriagedAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| VerificationMethod / VerificationMethod | `System.Nullable1[[RoadGuardSystem.BusinessObjects.Cases.CaseVerificationMethod, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]` / `int` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.BusinessObjects.Cases.CaseVerificationMethod, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Int32, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Int32`; max=None precision=None scale=None |
| _activeReportIds / ActiveReportIdsJson | `System.Collections.Generic.List1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter2[[System.Collections.Generic.List1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.String, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.String`; max=None precision=None scale=None |

## dbo.LD06ActionEvidence

Entity: `RoadGuardSystem.BusinessObjects.Projects.LD06ActionEvidence`; [RoadGuardSystem.BusinessObjects/Projects/LD06LifecycleAction.cs:91](../../../RoadGuardSystem.BusinessObjects/Projects/LD06LifecycleAction.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| ActionId / ActionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| FileId / FileId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Checksum / Checksum | `System.String` / `varchar(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |

## dbo.LD06LifecycleActions

Entity: `RoadGuardSystem.BusinessObjects.Projects.LD06LifecycleAction`; [RoadGuardSystem.BusinessObjects/Projects/LD06LifecycleAction.cs:15](../../../RoadGuardSystem.BusinessObjects/Projects/LD06LifecycleAction.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ActorId / ActorId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| At / At | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| DefectId / DefectId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| FactsJson / FactsJson | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Kind / Kind | `RoadGuardSystem.BusinessObjects.Projects.LD06ActionKind` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.BusinessObjects.Projects.LD06ActionKind, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| LinkedDefectId / LinkedDefectId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ObligationId / ObligationId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| PriorRepairDecisionId / PriorRepairDecisionId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Reason / Reason | `System.String` / `nvarchar(2000)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=2000 precision=None scale=None |
| ReceivingProjectId / ReceivingProjectId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ScopeHash / ScopeHash | `System.String` / `varchar(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| SourceActionId / SourceActionId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.MeasurementValidationSamples

Entity: `RoadGuardSystem.BusinessObjects.Processing.MeasurementValidationSample`; [RoadGuardSystem.BusinessObjects/Processing/MeasurementValidationSample.cs:5](../../../RoadGuardSystem.BusinessObjects/Processing/MeasurementValidationSample.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AbsoluteError / AbsoluteError | `System.Decimal` / `decimal(19,6)` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| DerivedMeasurementId / DerivedMeasurementId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ExclusionReason / ExclusionReason | `System.String` / `nvarchar(500)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| GroundTruthMeasurementId / GroundTruthMeasurementId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| InclusionStatus / InclusionStatus | `RoadGuardSystem.aBusinessObjects.Commons.ValidationSampleInclusionStatus` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.aBusinessObjects.Commons.ValidationSampleInclusionStatus, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| SignedError / SignedError | `System.Decimal` / `decimal(19,6)` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ValidationRunId / ValidationRunId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.NativeRouteVersionFacts

Entity: `RoadGuardSystem.BusinessObjects.Projects.NativeRouteVersionFacts`; [RoadGuardSystem.BusinessObjects/Projects/CrsProfileRevision.cs:25](../../../RoadGuardSystem.BusinessObjects/Projects/CrsProfileRevision.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| RoadSectionVersionId / RoadSectionVersionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CalibrationJson / CalibrationJson | `System.String` / `nvarchar(max)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CanonicalLengthMeters / CanonicalLengthMeters | `System.Double` / `float` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CrsProfileRevisionId / CrsProfileRevisionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| DeclaredLengthMeters / DeclaredLengthMeters | `System.Nullable1[[System.Double, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `float` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| JunctionOffsetMeters / JunctionOffsetMeters | `System.Nullable1[[System.Double, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `float` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ParentRouteVersionId / ParentRouteVersionId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RouteKind / RouteKind | `System.String` / `nvarchar(16)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=16 precision=None scale=None |
| RouteSystemId / RouteSystemId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SampleOnly / SampleOnly | `System.Boolean` / `bit` | False | False | `Never`; defaultSQL=`None`; default=`False`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.Notifications

Entity: `RoadGuardSystem.BusinessObjects.Messaging.Notification`; [RoadGuardSystem.BusinessObjects/Messaging/Notification.cs:3](../../../RoadGuardSystem.BusinessObjects/Messaging/Notification.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Body / Body | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| EventType / EventType | `System.String` / `varchar(80)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=80 precision=None scale=None |
| OccurredAtUtc / OccurredAtUtc | `System.DateTimeOffset` / `datetimeoffset(7)` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ReadAt / ReadAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset(7)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RecipientUserId / RecipientUserId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RowVersion / RowVersion | `System.Byte[]` / `rowversion` | False | True | `OnAddOrUpdate`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SourceEntityId / SourceEntityId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SourceEntityType / SourceEntityType | `System.String` / `varchar(80)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=80 precision=None scale=None |
| Title / Title | `System.String` / `nvarchar(200)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=200 precision=None scale=None |

## dbo.ObligationResponsibilities

Entity: `RoadGuardSystem.BusinessObjects.Projects.ObligationResponsibility`; [RoadGuardSystem.BusinessObjects/Projects/LD06LifecycleAction.cs:78](../../../RoadGuardSystem.BusinessObjects/Projects/LD06LifecycleAction.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| ObligationId / ObligationId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AcceptanceActionId / AcceptanceActionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CurrentProjectId / CurrentProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| OriginProjectId / OriginProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RowVersion / RowVersion | `System.Byte[]` / `rowversion` | False | True | `OnAddOrUpdate`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.OfflineAdmittedFileReferences

Entity: `RoadGuardSystem.BusinessObjects.Offline.OfflineAdmittedFileReference`; [RoadGuardSystem.BusinessObjects/Offline/OfflineAdmittedFileReference.cs:4](../../../RoadGuardSystem.BusinessObjects/Offline/OfflineAdmittedFileReference.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ActualFileOwnerId / ActualFileOwnerId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ActualUploadedById / ActualUploadedById | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AdmissionId / AdmissionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| BindingId / BindingId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CaptureFactsJson / CaptureFactsJson | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CaptureOriginId / CaptureOriginId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ContentChecksum / ContentChecksum | `System.String` / `varchar(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| CurrentImporterId / CurrentImporterId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| FileId / FileId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| OriginalActorId / OriginalActorId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Purpose / Purpose | `System.String` / `nvarchar(40)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=40 precision=None scale=None |
| ReferencedAt / ReferencedAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| TaskId / TaskId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.OfflineDeviceRegistrations

Entity: `RoadGuardSystem.BusinessObjects.Offline.OfflineDeviceRegistration`; [RoadGuardSystem.BusinessObjects/Offline/OfflineDeviceRegistration.cs:5](../../../RoadGuardSystem.BusinessObjects/Offline/OfflineDeviceRegistration.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ActorId / ActorId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| DeviceId / DeviceId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| EncryptionPublicKey / EncryptionPublicKey | `System.String` / `nvarchar(512)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=512 precision=None scale=None |
| KeyFingerprint / KeyFingerprint | `System.String` / `varchar(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RegisteredAt / RegisteredAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Revision / Revision | `System.Int32` / `int` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RoleSnapshot / RoleSnapshot | `RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| SigningPublicKey / SigningPublicKey | `System.String` / `nvarchar(512)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=512 precision=None scale=None |

## dbo.OfflineDeviceRevocations

Entity: `RoadGuardSystem.BusinessObjects.Offline.OfflineDeviceRevocation`; [RoadGuardSystem.BusinessObjects/Offline/OfflineDeviceRegistration.cs:40](../../../RoadGuardSystem.BusinessObjects/Offline/OfflineDeviceRegistration.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| DeviceRegistrationId / DeviceRegistrationId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Reason / Reason | `System.String` / `nvarchar(2000)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=2000 precision=None scale=None |
| RevokedAt / RevokedAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RevokedBy / RevokedBy | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.OfflineEncryptedCaptureArtifacts

Entity: `RoadGuardSystem.BusinessObjects.Offline.OfflineEncryptedCaptureArtifact`; [RoadGuardSystem.BusinessObjects/Offline/OfflineEncryptedCaptureArtifact.cs:8](../../../RoadGuardSystem.BusinessObjects/Offline/OfflineEncryptedCaptureArtifact.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CaptureOriginId / CaptureOriginId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ChunkIndex / ChunkIndex | `System.Int32` / `int` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ChunkLength / ChunkLength | `System.Int32` / `int` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ChunkOffset / ChunkOffset | `System.Int64` / `bigint` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CipherEnvelopeJson / CipherEnvelopeJson | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| EnvelopeFingerprint / EnvelopeFingerprint | `System.String` / `varchar(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| ManifestHash / ManifestHash | `System.String` / `varchar(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| ManifestSignature / ManifestSignature | `System.String` / `nvarchar(512)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=512 precision=None scale=None |
| OriginalActorId / OriginalActorId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ParentPackageId / ParentPackageId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| PlaintextChecksum / PlaintextChecksum | `System.String` / `varchar(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RegisteredAt / RegisteredAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RegisteredBy / RegisteredBy | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SignedManifestJson / SignedManifestJson | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SourceDeviceRegistrationId / SourceDeviceRegistrationId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| TaskId / TaskId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.OfflineEncryptedPackages

Entity: `RoadGuardSystem.BusinessObjects.Offline.OfflineEncryptedPackageRecord`; [RoadGuardSystem.BusinessObjects/Offline/OfflineEncryptedPackageRecord.cs:5](../../../RoadGuardSystem.BusinessObjects/Offline/OfflineEncryptedPackageRecord.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CipherPackageJson / CipherPackageJson | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ManifestHash / ManifestHash | `System.String` / `varchar(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| OriginalActorId / OriginalActorId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| PackageFingerprint / PackageFingerprint | `System.String` / `varchar(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| PayloadHash / PayloadHash | `System.String` / `varchar(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RegisteredAt / RegisteredAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RegisteredBy / RegisteredBy | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RegistrationMode / RegistrationMode | `System.String` / `nvarchar(32)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=32 precision=None scale=None |
| SignedManifestJson / SignedManifestJson | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SourceBatchId / SourceBatchId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SourceDeviceRegistrationId / SourceDeviceRegistrationId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SourceSignature / SourceSignature | `System.String` / `nvarchar(512)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=512 precision=None scale=None |

## dbo.OfflineEvidenceCaptureReferences

Entity: `RoadGuardSystem.BusinessObjects.Offline.OfflineEvidenceCaptureReference`; [RoadGuardSystem.BusinessObjects/Offline/OfflineEvidenceCaptureReference.cs:4](../../../RoadGuardSystem.BusinessObjects/Offline/OfflineEvidenceCaptureReference.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ActualUploaderId / ActualUploaderId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AdmissionId / AdmissionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AdmittedAt / AdmittedAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| BindingId / BindingId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CaptureFactsJson / CaptureFactsJson | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CaptureOriginId / CaptureOriginId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Checksum / Checksum | `System.String` / `varchar(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| DeclaredCapturedAt / DeclaredCapturedAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| FileId / FileId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| GrantId / GrantId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| MediaType / MediaType | `System.String` / `nvarchar(120)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=120 precision=None scale=None |
| OriginalActorId / OriginalActorId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Purpose / Purpose | `System.String` / `nvarchar(40)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=40 precision=None scale=None |
| TaskId / TaskId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| UploadSessionId / UploadSessionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.OfflineHandoverGrantRevocations

Entity: `RoadGuardSystem.BusinessObjects.Offline.OfflineHandoverGrantRevocation`; [RoadGuardSystem.BusinessObjects/Offline/OfflineHandoverGrantRevocation.cs:3](../../../RoadGuardSystem.BusinessObjects/Offline/OfflineHandoverGrantRevocation.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| GrantId / GrantId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Reason / Reason | `System.String` / `nvarchar(2000)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=2000 precision=None scale=None |
| RevokedAt / RevokedAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RevokedBy / RevokedBy | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.OfflineHandoverGrants

Entity: `RoadGuardSystem.BusinessObjects.Offline.OfflineHandoverGrant`; [RoadGuardSystem.BusinessObjects/Offline/OfflineHandoverGrant.cs:6](../../../RoadGuardSystem.BusinessObjects/Offline/OfflineHandoverGrant.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ExpiresAt / ExpiresAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| IssuedAt / IssuedAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| IssuedBy / IssuedBy | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ManifestHash / ManifestHash | `System.String` / `varchar(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| PackageId / PackageId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Reason / Reason | `System.String` / `nvarchar(2000)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=2000 precision=None scale=None |
| RecipientActorId / RecipientActorId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RecipientDeviceRegistrationId / RecipientDeviceRegistrationId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RecipientRole / RecipientRole | `RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| ScopeJson / ScopeJson | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SourceActorId / SourceActorId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SourceDeviceRegistrationId / SourceDeviceRegistrationId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.OfflineOperationAdmissions

Entity: `RoadGuardSystem.BusinessObjects.Offline.OfflineOperationAdmission`; [RoadGuardSystem.BusinessObjects/Offline/OfflineOperationAdmission.cs:6](../../../RoadGuardSystem.BusinessObjects/Offline/OfflineOperationAdmission.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AdmissionMode / AdmissionMode | `System.String` / `nvarchar(32)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=32 precision=None scale=None |
| AdmittedAt / AdmittedAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| BatchId / BatchId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| BindingId / BindingId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CurrentImporterId / CurrentImporterId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| GrantId / GrantId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ImporterRole / ImporterRole | `RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ScopeFactsJson / ScopeFactsJson | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.OfflineOperationBindings

Entity: `RoadGuardSystem.BusinessObjects.Offline.OfflineOperationBinding`; [RoadGuardSystem.BusinessObjects/Offline/OfflineOperationBinding.cs:7](../../../RoadGuardSystem.BusinessObjects/Offline/OfflineOperationBinding.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AssignmentId / AssignmentId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CorePayloadHash / CorePayloadHash | `System.String` / `varchar(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| EffectId / EffectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| EnvelopeHash / EnvelopeHash | `System.String` / `varchar(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| EnvelopeJson / EnvelopeJson | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| FirstServerReceivedAt / FirstServerReceivedAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Kind / Kind | `System.String` / `nvarchar(40)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=40 precision=None scale=None |
| OriginId / OriginId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| OriginalActorId / OriginalActorId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RepairResourceId / RepairResourceId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SnapshotId / SnapshotId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SourceDeviceRegistrationId / SourceDeviceRegistrationId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| TaskId / TaskId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.OfflineOperationResults

Entity: `RoadGuardSystem.BusinessObjects.Offline.OfflineOperationResult`; [RoadGuardSystem.BusinessObjects/Offline/OfflineOperationResult.cs:4](../../../RoadGuardSystem.BusinessObjects/Offline/OfflineOperationResult.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AdmissionId / AdmissionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| BatchId / BatchId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ClaimedFinishedAt / ClaimedFinishedAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Code / Code | `System.String` / `nvarchar(100)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=100 precision=None scale=None |
| DurableAck / DurableAck | `System.Boolean` / `bit` | False | False | `Never`; defaultSQL=`None`; default=`False`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| EffectId / EffectId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| OriginId / OriginId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| OutcomeJson / OutcomeJson | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RecordedAt / RecordedAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ResourceVersion / ResourceVersion | `System.String` / `nvarchar(32)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=32 precision=None scale=None |
| State / State | `System.String` / `nvarchar(32)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=32 precision=None scale=None |
| SyncLateness / SyncLateness | `System.String` / `nvarchar(24)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=24 precision=None scale=None |
| TimeProvenance / TimeProvenance | `System.String` / `nvarchar(32)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=32 precision=None scale=None |
| VerifiedFinishedAt / VerifiedFinishedAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.OfflineOriginTimeVerifications

Entity: `RoadGuardSystem.BusinessObjects.Offline.OfflineOriginTimeVerification`; [RoadGuardSystem.BusinessObjects/Offline/OfflineOriginTimeVerification.cs:4](../../../RoadGuardSystem.BusinessObjects/Offline/OfflineOriginTimeVerification.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| BindingId / BindingId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CanonicalOriginId / CanonicalOriginId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| OriginalOccurredAtUtc / OriginalOccurredAtUtc | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProofSourceId / ProofSourceId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProofSourceKind / ProofSourceKind | `System.String` / `nvarchar(40)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=40 precision=None scale=None |
| RecordedAtUtc / RecordedAtUtc | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| TypedEffectId / TypedEffectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| VerifiedBy / VerifiedBy | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.OfflinePackageFileReferences

Entity: `RoadGuardSystem.BusinessObjects.Offline.OfflinePackageFileReference`; [RoadGuardSystem.BusinessObjects/Offline/OfflinePackageFileReference.cs:3](../../../RoadGuardSystem.BusinessObjects/Offline/OfflinePackageFileReference.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CaptureFactsJson / CaptureFactsJson | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ContentChecksum / ContentChecksum | `System.String` / `varchar(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| FileId / FileId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| PackageId / PackageId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.OfflineSyncBatches

Entity: `RoadGuardSystem.BusinessObjects.Offline.OfflineSyncBatch`; [RoadGuardSystem.BusinessObjects/Offline/OfflineSyncBatch.cs:3](../../../RoadGuardSystem.BusinessObjects/Offline/OfflineSyncBatch.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AttachedPayloadHash / AttachedPayloadHash | `System.String` / `varchar(64)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| AttachedPayloadJson / AttachedPayloadJson | `System.String` / `nvarchar(max)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ContentHash / ContentHash | `System.String` / `varchar(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| CurrentImporterId / CurrentImporterId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| GrantId / GrantId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| PackageId / PackageId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ReceivedAt / ReceivedAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RecipientDeviceRegistrationId / RecipientDeviceRegistrationId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RecipientSignature / RecipientSignature | `System.String` / `nvarchar(512)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=512 precision=None scale=None |
| SignedDescriptorJson / SignedDescriptorJson | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SourceBatchId / SourceBatchId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SourceDeviceRegistrationId / SourceDeviceRegistrationId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SourceSignature / SourceSignature | `System.String` / `nvarchar(512)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=512 precision=None scale=None |

## dbo.OfflineTaskSnapshots

Entity: `RoadGuardSystem.BusinessObjects.Offline.OfflineTaskSnapshot`; [RoadGuardSystem.BusinessObjects/Offline/OfflineTaskSnapshot.cs:3](../../../RoadGuardSystem.BusinessObjects/Offline/OfflineTaskSnapshot.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AssignmentHash / AssignmentHash | `System.String` / `varchar(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| AssignmentId / AssignmentId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ContentHash / ContentHash | `System.String` / `varchar(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| DeviceRegistrationId / DeviceRegistrationId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| DownloadedAt / DownloadedAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| OriginalActorId / OriginalActorId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SnapshotJson / SnapshotJson | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| TaskId / TaskId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| TaskVersion / TaskVersion | `System.String` / `nvarchar(32)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=32 precision=None scale=None |

## dbo.OutboxMessages

Entity: `RoadGuardSystem.BusinessObjects.Messaging.OutboxMessage`; [RoadGuardSystem.BusinessObjects/Messaging/OutboxMessage.cs:7](../../../RoadGuardSystem.BusinessObjects/Messaging/OutboxMessage.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `OnAdd`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CorrelationId / CorrelationId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| DeliveryAttemptCount / DeliveryAttemptCount | `System.Int32` / `int` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| DeliveryStatus / DeliveryStatus | `RoadGuardSystem.aBusinessObjects.Commons.OutboxDeliveryStatus` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.aBusinessObjects.Commons.OutboxDeliveryStatus, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| LastErrorCode / LastErrorCode | `System.String` / `varchar(80)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=80 precision=None scale=None |
| LastErrorMessage / LastErrorMessage | `System.String` / `nvarchar(max)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| LeaseExpiresAtUtc / LeaseExpiresAtUtc | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset(7)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| LeaseOwner / LeaseOwner | `System.String` / `varchar(120)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=120 precision=None scale=None |
| MessageType / MessageType | `System.String` / `varchar(200)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=200 precision=None scale=None |
| NextAttemptAtUtc / NextAttemptAtUtc | `System.DateTimeOffset` / `datetimeoffset(7)` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| OccurredAtUtc / OccurredAtUtc | `System.DateTimeOffset` / `datetimeoffset(7)` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| PayloadJson / PayloadJson | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RowVersion / RowVersion | `System.Byte[]` / `rowversion` | False | True | `OnAddOrUpdate`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.PasswordRecoveryRequests

Entity: `RoadGuardSystem.BusinessObjects.Identity.PasswordRecoveryRequest`; [RoadGuardSystem.BusinessObjects/Identity/PasswordRecoveryRequest.cs:3](../../../RoadGuardSystem.BusinessObjects/Identity/PasswordRecoveryRequest.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CorrelationId / CorrelationId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RequestedAtUtc / RequestedAtUtc | `System.DateTimeOffset` / `datetimeoffset(7)` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| TargetUserId / TargetUserId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.PasswordResetLogs

Entity: `RoadGuardSystem.BusinessObjects.Identity.PasswordResetLog`; [RoadGuardSystem.BusinessObjects/Identity/PasswordResetLog.cs:6](../../../RoadGuardSystem.BusinessObjects/Identity/PasswordResetLog.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `OnAdd`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CorrelationId / CorrelationId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| OccurredAt / OccurredAt | `System.DateTimeOffset` / `datetimeoffset(7)` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| PerformedByUserId / PerformedByUserId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Reason / Reason | `System.String` / `nvarchar(max)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Result / Result | `RoadGuardSystem.aBusinessObjects.Commons.PasswordResetResult` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.aBusinessObjects.Commons.PasswordResetResult, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| Source / Source | `System.String` / `varchar(80)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=80 precision=None scale=None |
| TargetUserId / TargetUserId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.PavementLayoutRevisions

Entity: `RoadGuardSystem.BusinessObjects.Projects.PavementLayoutRevision`; [RoadGuardSystem.BusinessObjects/Projects/PavementPublication.cs:3](../../../RoadGuardSystem.BusinessObjects/Projects/PavementPublication.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ContentHash / ContentHash | `System.String` / `nvarchar(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| CreatedAt / CreatedAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CreatedBy / CreatedBy | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CrsProfileRevisionId / CrsProfileRevisionId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| DefinitionJson / DefinitionJson | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Kind / Kind | `System.String` / `nvarchar(16)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=16 precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RouteVersionId / RouteVersionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SegmentSetId / SegmentSetId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SnapshotJson / SnapshotJson | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SourcePlanId / SourcePlanId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.PavementSourceFileReferences

Entity: `RoadGuardSystem.BusinessObjects.Projects.PavementSourceFileReference`; [RoadGuardSystem.BusinessObjects/Projects/PavementPublication.cs:56](../../../RoadGuardSystem.BusinessObjects/Projects/PavementPublication.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CaptureFactsJson / CaptureFactsJson | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ContentChecksum / ContentChecksum | `System.String` / `nvarchar(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| FileId / FileId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| LayoutRevisionId / LayoutRevisionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.ProcessingAttempts

Entity: `RoadGuardSystem.BusinessObjects.Processing.ProcessingAttempt`; [RoadGuardSystem.BusinessObjects/Processing/ProcessingAttempt.cs:5](../../../RoadGuardSystem.BusinessObjects/Processing/ProcessingAttempt.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AttemptNo / AttemptNo | `System.Int32` / `int` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| EndedAt / EndedAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset(7)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ErrorType / ErrorType | `System.Nullable1[[RoadGuardSystem.aBusinessObjects.Commons.ProcessingAttemptErrorType, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]` / `tinyint` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.aBusinessObjects.Commons.ProcessingAttemptErrorType, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| ProcessingJobId / ProcessingJobId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| StartedAt / StartedAt | `System.DateTimeOffset` / `datetimeoffset(7)` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| WorkerReference / WorkerReference | `System.String` / `varchar(120)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=120 precision=None scale=None |

## dbo.ProcessingBlocks

Entity: `RoadGuardSystem.BusinessObjects.Processing.ProcessingBlock`; [RoadGuardSystem.BusinessObjects/Processing/ProcessingBlock.cs:5](../../../RoadGuardSystem.BusinessObjects/Processing/ProcessingBlock.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| BlockNo / BlockNo | `System.Int32` / `int` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RangeMetadata / RangeMetadata | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SurveyDataVersionId / SurveyDataVersionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.ProcessingJobs

Entity: `RoadGuardSystem.BusinessObjects.Processing.ProcessingJob`; [RoadGuardSystem.BusinessObjects/Processing/ProcessingJob.cs:6](../../../RoadGuardSystem.BusinessObjects/Processing/ProcessingJob.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CompletedAt / CompletedAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset(7)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ErrorCode / ErrorCode | `System.String` / `varchar(80)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=80 precision=None scale=None |
| ErrorMessage / ErrorMessage | `System.String` / `nvarchar(max)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ManifestHash / ManifestHash | `System.String` / `char(64)` | False | False | `OnAdd`; defaultSQL=`None`; default=``; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ManifestJson / ManifestJson | `System.String` / `nvarchar(max)` | False | False | `OnAdd`; defaultSQL=`None`; default=`{}`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Mode / Mode | `System.String` / `varchar(8)` | False | False | `OnAdd`; defaultSQL=`None`; default=``; computed=`None`; collation=`None`; `None`; provider=`None`; max=8 precision=None scale=None |
| ModelVersionId / ModelVersionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProcessingBlockId / ProcessingBlockId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RowVersion / RowVersion | `System.Byte[]` / `rowversion` | False | True | `OnAddOrUpdate`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| StartedAt / StartedAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset(7)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Status / Status | `RoadGuardSystem.aBusinessObjects.Commons.ProcessingJobStatus` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.aBusinessObjects.Commons.ProcessingJobStatus, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |

## dbo.ProjectLifecycleHistory

Entity: `RoadGuardSystem.BusinessObjects.Projects.ProjectLifecycleHistoryRecord`; [RoadGuardSystem.BusinessObjects/Projects/ProjectLifecycleHistoryRecord.cs:12](../../../RoadGuardSystem.BusinessObjects/Projects/ProjectLifecycleHistoryRecord.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ActorId / ActorId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AuthoritySourceReference / AuthoritySourceReference | `System.String` / `nvarchar(2000)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=2000 precision=None scale=None |
| BasisReference / BasisReference | `System.String` / `nvarchar(2000)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=2000 precision=None scale=None |
| DefectId / DefectId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| FactsJson / FactsJson | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| GrantId / GrantId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Kind / Kind | `RoadGuardSystem.BusinessObjects.Projects.ProjectLifecycleFactKind` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.BusinessObjects.Projects.ProjectLifecycleFactKind, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| LinkedDefectId / LinkedDefectId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ObligationId / ObligationId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| OperationalClosureId / OperationalClosureId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Reason / Reason | `System.String` / `nvarchar(2000)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=2000 precision=None scale=None |
| ReceiverId / ReceiverId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RecordedAtUtc / RecordedAtUtc | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SourceDisposition / SourceDisposition | `System.String` / `varchar(32)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=32 precision=None scale=None |

## dbo.ProjectMembers

Entity: `RoadGuardSystem.BusinessObjects.Projects.ProjectMember`; [RoadGuardSystem.BusinessObjects/Projects/ProjectMember.cs:5](../../../RoadGuardSystem.BusinessObjects/Projects/ProjectMember.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| IsPrimary / IsPrimary | `System.Boolean` / `bit` | False | False | `Never`; defaultSQL=`None`; default=`False`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RoleCode / RoleCode | `RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode` / `varchar(40)` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter2[[RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.String, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.String`; max=40 precision=None scale=None |
| RowVersion / RowVersion | `System.Byte[]` / `rowversion` | False | True | `OnAddOrUpdate`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Status / Status | `RoadGuardSystem.aBusinessObjects.Commons.ProjectMemberStatus` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.aBusinessObjects.Commons.ProjectMemberStatus, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| UserId / UserId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ValidFrom / ValidFrom | `System.DateOnly` / `date` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ValidTo / ValidTo | `System.Nullable1[[System.DateOnly, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `date` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.Projects

Entity: `RoadGuardSystem.BusinessObjects.Projects.Project`; [RoadGuardSystem.BusinessObjects/Projects/Project.cs:5](../../../RoadGuardSystem.BusinessObjects/Projects/Project.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CreatedAt / CreatedAt | `System.DateTimeOffset` / `datetimeoffset(7)` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Description / Description | `System.String` / `nvarchar(max)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| EndDate / EndDate | `System.Nullable1[[System.DateOnly, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `date` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| EngineeringUtmSrid / EngineeringUtmSrid | `System.Nullable1[[System.Int32, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `int` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Name / Name | `System.String` / `nvarchar(255)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=255 precision=None scale=None |
| ProjectCode / ProjectCode | `System.String` / `varchar(50)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=50 precision=None scale=None |
| RowVersion / RowVersion | `System.Byte[]` / `rowversion` | False | True | `OnAddOrUpdate`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| StartDate / StartDate | `System.Nullable1[[System.DateOnly, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `date` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Status / Status | `RoadGuardSystem.aBusinessObjects.Commons.ProjectStatus` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.aBusinessObjects.Commons.ProjectStatus, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |

## dbo.QualityChecks

Entity: `RoadGuardSystem.BusinessObjects.Surveys.QualityCheck`; [RoadGuardSystem.BusinessObjects/Surveys/QualityCheck.cs:6](../../../RoadGuardSystem.BusinessObjects/Surveys/QualityCheck.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CheckType / CheckType | `RoadGuardSystem.aBusinessObjects.Commons.QualityCheckType` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.aBusinessObjects.Commons.QualityCheckType, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| CheckedAt / CheckedAt | `System.DateTimeOffset` / `datetimeoffset(7)` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CheckedBy / CheckedBy | `RoadGuardSystem.aBusinessObjects.Commons.QualityCheckActor` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.aBusinessObjects.Commons.QualityCheckActor, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| ExecutionStage / ExecutionStage | `RoadGuardSystem.aBusinessObjects.Commons.QualityCheckExecutionStage` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.aBusinessObjects.Commons.QualityCheckExecutionStage, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| InitiatedByUserId / InitiatedByUserId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| MeasuredValue / MeasuredValue | `System.String` / `nvarchar(max)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Message / Message | `System.String` / `nvarchar(max)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Scope / Scope | `RoadGuardSystem.aBusinessObjects.Commons.QualityCheckScope` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.aBusinessObjects.Commons.QualityCheckScope, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| Status / Status | `RoadGuardSystem.aBusinessObjects.Commons.QualityCheckStatus` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.aBusinessObjects.Commons.QualityCheckStatus, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| SurveyDataVersionId / SurveyDataVersionId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SurveyFileId / SurveyFileId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Threshold / Threshold | `System.String` / `nvarchar(max)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.RefreshTokens

Entity: `RoadGuardSystem.BusinessObjects.Identity.RefreshToken`; [RoadGuardSystem.BusinessObjects/Identity/RefreshToken.cs:5](../../../RoadGuardSystem.BusinessObjects/Identity/RefreshToken.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `OnAdd`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ExpiresAt / ExpiresAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset(7)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RevokedAt / RevokedAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset(7)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RowVersion / RowVersion | `System.Byte[]` / `rowversion` | False | True | `OnAddOrUpdate`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SessionId / SessionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| TokenHash / TokenHash | `System.String` / `varchar(128)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=128 precision=None scale=None |

## dbo.RepairActualScopes

Entity: `RoadGuardSystem.BusinessObjects.Repairs.RepairActualScope`; [RoadGuardSystem.BusinessObjects/Repairs/RepairWork.cs:18](../../../RoadGuardSystem.BusinessObjects/Repairs/RepairWork.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| From / From | `System.Decimal` / `decimal(18,3)` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=18 scale=3 |
| LocationVersion / LocationVersion | `System.String` / `nvarchar(200)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=200 precision=None scale=None |
| ObligationId / ObligationId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| OffsetFrom / OffsetFrom | `System.Decimal` / `decimal(18,3)` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=18 scale=3 |
| OffsetTo / OffsetTo | `System.Decimal` / `decimal(18,3)` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=18 scale=3 |
| PhysicalRoadId / PhysicalRoadId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RouteLabel / RouteLabel | `System.String` / `nvarchar(200)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=200 precision=None scale=None |
| To / To | `System.Decimal` / `decimal(18,3)` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=18 scale=3 |

## dbo.RepairAssessmentEvidence

Entity: `RoadGuardSystem.BusinessObjects.Repairs.RepairAssessmentEvidenceReference`; [RoadGuardSystem.BusinessObjects/Repairs/RepairProducerSources.cs:29](../../../RoadGuardSystem.BusinessObjects/Repairs/RepairProducerSources.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ActualUploaderId / ActualUploaderId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AssessmentId / AssessmentId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CaptureFactsJson / CaptureFactsJson | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CaptureOriginId / CaptureOriginId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CapturedAt / CapturedAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ChecksumSha256 / ChecksumSha256 | `System.String` / `nvarchar(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| FileId / FileId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| FileVersion / FileVersion | `System.String` / `nvarchar(200)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=200 precision=None scale=None |
| MediaType / MediaType | `System.String` / `nvarchar(120)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=120 precision=None scale=None |
| OriginalActorId / OriginalActorId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Purpose / Purpose | `System.String` / `nvarchar(40)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=40 precision=None scale=None |
| ReuseDecisionId / ReuseDecisionId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| StateAtIntake / StateAtIntake | `System.String` / `nvarchar(40)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=40 precision=None scale=None |

## dbo.RepairAssessmentMeasurements

Entity: `RoadGuardSystem.BusinessObjects.Repairs.RepairAssessmentMeasurementReference`; [RoadGuardSystem.BusinessObjects/Repairs/RepairProducerSources.cs:28](../../../RoadGuardSystem.BusinessObjects/Repairs/RepairProducerSources.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| AssessmentId / AssessmentId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| MeasurementId / MeasurementId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.RepairAttemptEvidence

Entity: `RoadGuardSystem.BusinessObjects.Repairs.RepairAttempt.Evidence#RepairEvidenceReference`; [RoadGuardSystem.BusinessObjects/Repairs/RepairWork.cs:127](../../../RoadGuardSystem.BusinessObjects/Repairs/RepairWork.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| AttemptId / AttemptId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| FileId / FileId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Purpose / Purpose | `RoadGuardSystem.BusinessObjects.Repairs.RepairEvidencePurpose` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.BusinessObjects.Repairs.RepairEvidencePurpose, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| CapturedAt / CapturedAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| FileVersion / FileVersion | `System.String` / `nvarchar(200)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=200 precision=None scale=None |
| Hash / Hash | `System.String` / `nvarchar(64)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| Related / Related | `System.Boolean` / `bit` | False | False | `Never`; defaultSQL=`None`; default=`False`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ReuseDecisionId / ReuseDecisionId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ReusedSource / ReusedSource | `System.Boolean` / `bit` | False | False | `Never`; defaultSQL=`None`; default=`False`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SourceId / SourceId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SourceKind / SourceKind | `System.String` / `nvarchar(60)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=60 precision=None scale=None |
| Verified / Verified | `System.Boolean` / `bit` | False | False | `Never`; defaultSQL=`None`; default=`False`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.RepairAttemptReviews

Entity: `RoadGuardSystem.BusinessObjects.Repairs.RepairAttemptReview`; [RoadGuardSystem.BusinessObjects/Repairs/RepairProducerSources.cs:49](../../../RoadGuardSystem.BusinessObjects/Repairs/RepairProducerSources.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ActorId / ActorId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AssessmentFactsJson / AssessmentFactsJson | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| At / At | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AttemptId / AttemptId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| BindingId / BindingId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ChecklistVersion / ChecklistVersion | `System.String` / `nvarchar(200)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=200 precision=None scale=None |
| Decision / Decision | `System.String` / `nvarchar(40)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=40 precision=None scale=None |
| EvidenceSufficient / EvidenceSufficient | `System.Boolean` / `bit` | False | False | `Never`; defaultSQL=`None`; default=`False`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ExecutionAuthority / ExecutionAuthority | `RoadGuardSystem.BusinessObjects.Repairs.RepairFactState` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.BusinessObjects.Repairs.RepairFactState, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| IntakeLinkId / IntakeLinkId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ItemId / ItemId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| PlanHash / PlanHash | `System.String` / `nvarchar(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Reason / Reason | `System.String` / `nvarchar(2000)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=2000 precision=None scale=None |
| Role / Role | `RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| SubmissionContentHash / SubmissionContentHash | `System.String` / `nvarchar(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| SubmissionId / SubmissionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.RepairAttemptSubmissionLinks

Entity: `RoadGuardSystem.BusinessObjects.Repairs.RepairAttemptSubmissionLink`; [RoadGuardSystem.BusinessObjects/Repairs/RepairProducerSources.cs:45](../../../RoadGuardSystem.BusinessObjects/Repairs/RepairProducerSources.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AttemptId / AttemptId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| BindingId / BindingId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ExecutionFinishId / ExecutionFinishId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| FormalRootServerReceivedAt / FormalRootServerReceivedAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| FormalRootSubmissionId / FormalRootSubmissionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ItemId / ItemId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| OriginalReviewDueAt / OriginalReviewDueAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| PreviousLinkId / PreviousLinkId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ReviewClockId / ReviewClockId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SubmissionContentHash / SubmissionContentHash | `System.String` / `nvarchar(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| SubmissionId / SubmissionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.RepairAttempts

Entity: `RoadGuardSystem.BusinessObjects.Repairs.RepairAttempt`; [RoadGuardSystem.BusinessObjects/Repairs/RepairWork.cs:143](../../../RoadGuardSystem.BusinessObjects/Repairs/RepairWork.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AssignmentId / AssignmentId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AuthorizationId / AuthorizationId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CrewId / CrewId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| DefectId / DefectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| FinishedAt / FinishedAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ItemId / ItemId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| LocationVersion / LocationVersion | `System.String` / `nvarchar(200)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=200 precision=None scale=None |
| ObligationId / ObligationId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| OriginId / OriginId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| PayloadHash / PayloadHash | `System.String` / `nvarchar(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| Performed / Performed | `System.Boolean` / `bit` | False | False | `Never`; defaultSQL=`None`; default=`False`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| PolicyRevisionId / PolicyRevisionId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ServerReceivedAt / ServerReceivedAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| StartedAt / StartedAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| TaskId / TaskId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| TimeProvenance / TimeProvenance | `RoadGuardSystem.BusinessObjects.Repairs.RepairTimeProvenance` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`Uncertain`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.BusinessObjects.Repairs.RepairTimeProvenance, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| UnperformedReason / UnperformedReason | `System.String` / `nvarchar(2000)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=2000 precision=None scale=None |

## dbo.RepairCorrectionEvidence

Entity: `RoadGuardSystem.BusinessObjects.Repairs.RepairCorrectionBasis.Evidence#RepairEvidenceReference`; [RoadGuardSystem.BusinessObjects/Repairs/RepairWork.cs:127](../../../RoadGuardSystem.BusinessObjects/Repairs/RepairWork.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| DecisionId / DecisionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| FileId / FileId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CapturedAt / CapturedAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| FileVersion / FileVersion | `System.String` / `nvarchar(200)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=200 precision=None scale=None |
| Hash / Hash | `System.String` / `nvarchar(64)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| Purpose / Purpose | `RoadGuardSystem.BusinessObjects.Repairs.RepairEvidencePurpose` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.BusinessObjects.Repairs.RepairEvidencePurpose, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| Related / Related | `System.Boolean` / `bit` | False | False | `Never`; defaultSQL=`None`; default=`False`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ReuseDecisionId / ReuseDecisionId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ReusedSource / ReusedSource | `System.Boolean` / `bit` | False | False | `Never`; defaultSQL=`None`; default=`False`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SourceId / SourceId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SourceKind / SourceKind | `System.String` / `nvarchar(60)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=60 precision=None scale=None |
| Verified / Verified | `System.Boolean` / `bit` | False | False | `Never`; defaultSQL=`None`; default=`False`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.RepairDangerAcknowledgements

Entity: `RoadGuardSystem.BusinessObjects.Repairs.RepairDangerAcknowledgement`; [RoadGuardSystem.BusinessObjects/Repairs/RepairSafetyMonitoring.cs:19](../../../RoadGuardSystem.BusinessObjects/Repairs/RepairSafetyMonitoring.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ActorId / ActorId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AfterOriginalDue / AfterOriginalDue | `System.Boolean` / `bit` | False | False | `Never`; defaultSQL=`None`; default=`False`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| At / At | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| MonitoringId / MonitoringId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Reason / Reason | `System.String` / `nvarchar(2000)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=2000 precision=None scale=None |
| WarningId / WarningId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.RepairDangerWarnings

Entity: `RoadGuardSystem.BusinessObjects.Repairs.RepairDangerWarning`; [RoadGuardSystem.BusinessObjects/Repairs/RepairSafetyMonitoring.cs:14](../../../RoadGuardSystem.BusinessObjects/Repairs/RepairSafetyMonitoring.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| MonitoringId / MonitoringId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| OriginalAcknowledgementDueAt / OriginalAcknowledgementDueAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Reason / Reason | `System.String` / `nvarchar(2000)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=2000 precision=None scale=None |
| ResponsibleActorId / ResponsibleActorId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ServerReceivedAt / ServerReceivedAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SourceId / SourceId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.RepairDecisions

Entity: `RoadGuardSystem.BusinessObjects.Repairs.RepairCorrectionBasis`; [RoadGuardSystem.BusinessObjects/Repairs/RepairCorrectionBehavior.cs:5](../../../RoadGuardSystem.BusinessObjects/Repairs/RepairCorrectionBehavior.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| RepairDecisionId / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Text / Basis_Text | `System.String` / `nvarchar(2000)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=2000 precision=None scale=None |

Entity: `RoadGuardSystem.BusinessObjects.Repairs.RepairDecision`; [RoadGuardSystem.BusinessObjects/Repairs/RepairWork.cs:130](../../../RoadGuardSystem.BusinessObjects/Repairs/RepairWork.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ActorId / ActorId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| At / At | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| DefectId / DefectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ItemId / ItemId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Mode / Mode | `RoadGuardSystem.BusinessObjects.Repairs.RepairMode` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.BusinessObjects.Repairs.RepairMode, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| ObligationId / ObligationId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| PreviousObligationHeadDecisionId / PreviousObligationHeadDecisionId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Reason / Reason | `System.String` / `nvarchar(2000)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=2000 precision=None scale=None |
| Result / Result | `RoadGuardSystem.BusinessObjects.Repairs.RepairPresentationState` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.BusinessObjects.Repairs.RepairPresentationState, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| Role / Role | `RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| SupersedesDecisionId / SupersedesDecisionId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.RepairEligibilityAssessments

Entity: `RoadGuardSystem.BusinessObjects.Repairs.RepairEligibilityAssessment`; [RoadGuardSystem.BusinessObjects/Repairs/RepairProducerSources.cs:67](../../../RoadGuardSystem.BusinessObjects/Repairs/RepairProducerSources.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AssessmentId / AssessmentId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| BindingId / BindingId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Coverage / Coverage | `RoadGuardSystem.BusinessObjects.Repairs.RepairFactState` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.BusinessObjects.Repairs.RepairFactState, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| EvaluatedAt / EvaluatedAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ItemId / ItemId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| MissingReasonsJson / MissingReasonsJson | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| PolicyContentHash / PolicyContentHash | `System.String` / `nvarchar(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| PolicyRevisionId / PolicyRevisionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RoadHandover / RoadHandover | `RoadGuardSystem.BusinessObjects.Repairs.RepairFactState` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.BusinessObjects.Repairs.RepairFactState, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| RoadSectionId / RoadSectionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SourceFactsHash / SourceFactsHash | `System.String` / `nvarchar(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| SourceMapping / SourceMapping | `System.String` / `nvarchar(200)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=200 precision=None scale=None |

## dbo.RepairEligibilityHandoverSources

Entity: `RoadGuardSystem.BusinessObjects.Repairs.RepairEligibilityHandoverSource`; [RoadGuardSystem.BusinessObjects/Repairs/RepairProducerSources.cs:81](../../../RoadGuardSystem.BusinessObjects/Repairs/RepairProducerSources.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| EligibilityAssessmentId / EligibilityAssessmentId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| HandoverDocumentId / HandoverDocumentId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ContentHash / ContentHash | `System.String` / `nvarchar(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| FactsJson / FactsJson | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| FileId / FileId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RowVersion / RowVersion | `System.String` / `nvarchar(24)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=24 precision=None scale=None |

## dbo.RepairEligibilityWarrantySources

Entity: `RoadGuardSystem.BusinessObjects.Repairs.RepairEligibilityWarrantySource`; [RoadGuardSystem.BusinessObjects/Repairs/RepairProducerSources.cs:79](../../../RoadGuardSystem.BusinessObjects/Repairs/RepairProducerSources.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| EligibilityAssessmentId / EligibilityAssessmentId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| WarrantyId / WarrantyId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ContentHash / ContentHash | `System.String` / `nvarchar(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| FactsJson / FactsJson | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RowVersion / RowVersion | `System.String` / `nvarchar(24)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=24 precision=None scale=None |
| SourceDocumentId / SourceDocumentId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.RepairExecutionAuthorizations

Entity: `RoadGuardSystem.BusinessObjects.Repairs.RepairExecutionAuthorization`; [RoadGuardSystem.BusinessObjects/Repairs/RepairPolicyAndAuthorization.cs:108](../../../RoadGuardSystem.BusinessObjects/Repairs/RepairPolicyAndAuthorization.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AssignmentId / AssignmentId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CrewId / CrewId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| DefectId / DefectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ExpiresAt / ExpiresAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| FirstServerReceivedAt / FirstServerReceivedAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| FirstStartOriginId / FirstStartOriginId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| FirstStartPayloadHash / FirstStartPayloadHash | `System.String` / `nvarchar(64)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| FirstStartedAt / FirstStartedAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| IssuedAt / IssuedAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| IssuedBy / IssuedBy | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| LocationVersion / LocationVersion | `System.String` / `nvarchar(200)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=200 precision=None scale=None |
| Permission / Permission | `RoadGuardSystem.BusinessObjects.Repairs.RepairTaskMode` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.BusinessObjects.Repairs.RepairTaskMode, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| PolicyRevisionId / PolicyRevisionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Reason / Reason | `System.String` / `nvarchar(2000)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=2000 precision=None scale=None |
| RowVersion / RowVersion | `System.Byte[]` / `rowversion` | True | True | `OnAddOrUpdate`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| TaskId / TaskId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| TimeProvenance / TimeProvenance | `RoadGuardSystem.BusinessObjects.Repairs.RepairTimeProvenance` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`Uncertain`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.BusinessObjects.Repairs.RepairTimeProvenance, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| VerifiedStartedAt / VerifiedStartedAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.RepairExecutionFinishes

Entity: `RoadGuardSystem.BusinessObjects.Repairs.RepairExecutionFinish`; [RoadGuardSystem.BusinessObjects/Repairs/RepairProducerSources.cs:39](../../../RoadGuardSystem.BusinessObjects/Repairs/RepairProducerSources.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| BindingId / BindingId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ClaimedAt / ClaimedAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ContentHash / ContentHash | `System.String` / `nvarchar(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| ExecutionStartId / ExecutionStartId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ItemId / ItemId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| OperationOriginId / OperationOriginId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| OriginId / OriginId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| OriginalActorId / OriginalActorId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ServerReceivedAt / ServerReceivedAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| TimeProofFactsJson / TimeProofFactsJson | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| TimeProvenance / TimeProvenance | `RoadGuardSystem.BusinessObjects.Repairs.RepairTimeProvenance` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`Uncertain`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.BusinessObjects.Repairs.RepairTimeProvenance, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| VerifiedOriginalAt / VerifiedOriginalAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.RepairExecutionStarts

Entity: `RoadGuardSystem.BusinessObjects.Repairs.RepairExecutionStart`; [RoadGuardSystem.BusinessObjects/Repairs/RepairProducerSources.cs:34](../../../RoadGuardSystem.BusinessObjects/Repairs/RepairProducerSources.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AssessmentContentHash / AssessmentContentHash | `System.String` / `nvarchar(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| AssessmentId / AssessmentId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AuthorityFactsJson / AuthorityFactsJson | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| BindingId / BindingId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ChecklistVersion / ChecklistVersion | `System.String` / `nvarchar(200)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=200 precision=None scale=None |
| ClaimedAt / ClaimedAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ContentHash / ContentHash | `System.String` / `nvarchar(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| EligibilityAssessmentId / EligibilityAssessmentId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| FirstStartId / FirstStartId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ItemId / ItemId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| OperationOriginId / OperationOriginId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| OriginId / OriginId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| OriginalActorId / OriginalActorId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ServerReceivedAt / ServerReceivedAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| TimeProvenance / TimeProvenance | `RoadGuardSystem.BusinessObjects.Repairs.RepairTimeProvenance` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`Uncertain`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.BusinessObjects.Repairs.RepairTimeProvenance, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| VerifiedOriginalAt / VerifiedOriginalAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.RepairFieldTaskBindings

Entity: `RoadGuardSystem.BusinessObjects.Repairs.RepairFieldTaskBinding`; [RoadGuardSystem.BusinessObjects/Repairs/RepairProducerSources.cs:7](../../../RoadGuardSystem.BusinessObjects/Repairs/RepairProducerSources.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AssignedAt / AssignedAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AssignedBy / AssignedBy | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AssignmentId / AssignmentId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AssignmentVersion / AssignmentVersion | `System.String` / `nvarchar(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| AuthorizationId / AuthorizationId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ChecklistVersion / ChecklistVersion | `System.String` / `nvarchar(200)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=200 precision=None scale=None |
| CrewId / CrewId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CrsProfileRevisionId / CrsProfileRevisionId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| DefectId / DefectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ItemId / ItemId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| LayoutRevisionId / LayoutRevisionId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| LocationVersion / LocationVersion | `System.String` / `nvarchar(200)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=200 precision=None scale=None |
| MapPublicationId / MapPublicationId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Mode / Mode | `RoadGuardSystem.BusinessObjects.Repairs.RepairMode` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.BusinessObjects.Repairs.RepairMode, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| ObligationId / ObligationId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| PlanHash / PlanHash | `System.String` / `nvarchar(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| PolicyContentHash / PolicyContentHash | `System.String` / `nvarchar(64)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| PolicyRevisionId / PolicyRevisionId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Reason / Reason | `System.String` / `nvarchar(2000)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=2000 precision=None scale=None |
| RouteVersionId / RouteVersionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SegmentSetId / SegmentSetId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SlabId / SlabId | `System.String` / `nvarchar(160)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=160 precision=None scale=None |
| TaskId / TaskId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| TaskVersion / TaskVersion | `System.String` / `nvarchar(24)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=24 precision=None scale=None |

## dbo.RepairItemLifecycleEvents

Entity: `RoadGuardSystem.BusinessObjects.Repairs.RepairItemLifecycleEvent`; [RoadGuardSystem.BusinessObjects/Repairs/RepairProducerSources.cs:55](../../../RoadGuardSystem.BusinessObjects/Repairs/RepairProducerSources.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ActorId / ActorId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| At / At | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AttemptId / AttemptId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| BindingId / BindingId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| DecisionId / DecisionId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| DefectId / DefectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ItemId / ItemId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Kind / Kind | `System.String` / `nvarchar(40)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=40 precision=None scale=None |
| Mode / Mode | `RoadGuardSystem.BusinessObjects.Repairs.RepairMode` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.BusinessObjects.Repairs.RepairMode, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| ObligationId / ObligationId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Reason / Reason | `System.String` / `nvarchar(2000)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=2000 precision=None scale=None |
| ReviewId / ReviewId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Role / Role | `RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| SourceVersion / SourceVersion | `System.String` / `nvarchar(200)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=200 precision=None scale=None |
| SubmissionId / SubmissionId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.RepairItems

Entity: `RoadGuardSystem.BusinessObjects.Repairs.RepairItem`; [RoadGuardSystem.BusinessObjects/Repairs/RepairWork.cs:229](../../../RoadGuardSystem.BusinessObjects/Repairs/RepairWork.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ApprovedAt / ApprovedAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ApprovedBy / ApprovedBy | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ApprovedPlanHash / ApprovedPlanHash | `System.String` / `nvarchar(64)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| AssignedAt / AssignedAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AssignedBy / AssignedBy | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CancellationReason / CancellationReason | `System.String` / `nvarchar(2000)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=2000 precision=None scale=None |
| CancelledAt / CancelledAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CancelledBy / CancelledBy | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ChecklistVersion / ChecklistVersion | `System.String` / `nvarchar(200)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=200 precision=None scale=None |
| CrewId / CrewId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CurrentAssessmentId / CurrentAssessmentId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CurrentAttemptId / CurrentAttemptId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CurrentBindingId / CurrentBindingId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CurrentExecutionFinishId / CurrentExecutionFinishId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CurrentExecutionStartId / CurrentExecutionStartId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CurrentIntakeLinkId / CurrentIntakeLinkId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CurrentReviewId / CurrentReviewId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| DefectId / DefectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| EffectiveDecisionId / EffectiveDecisionId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| EffectiveIntakeSubmissionId / EffectiveIntakeSubmissionId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Mode / Mode | `RoadGuardSystem.BusinessObjects.Repairs.RepairMode` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.BusinessObjects.Repairs.RepairMode, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| ObligationId / ObligationId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| PackageId / PackageId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| PredecessorItemId / PredecessorItemId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProposalPlanHash / ProposalPlanHash | `System.String` / `nvarchar(64)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| ProposedAt / ProposedAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProposedBy / ProposedBy | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RepairPlan / RepairPlan | `System.String` / `nvarchar(2000)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=2000 precision=None scale=None |
| ReviewedAt / ReviewedAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ReviewedBy / ReviewedBy | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RowVersion / RowVersion | `System.Byte[]` / `rowversion` | True | True | `OnAddOrUpdate`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| StartedAt / StartedAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| State / State | `RoadGuardSystem.BusinessObjects.Repairs.RepairItemState` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.BusinessObjects.Repairs.RepairItemState, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| SupersededByItemId / SupersededByItemId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.RepairMeasurementAssessments

Entity: `RoadGuardSystem.BusinessObjects.Repairs.RepairMeasurementAssessment`; [RoadGuardSystem.BusinessObjects/Repairs/RepairProducerSources.cs:15](../../../RoadGuardSystem.BusinessObjects/Repairs/RepairProducerSources.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AssignmentId / AssignmentId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| BindingId / BindingId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ContentHash / ContentHash | `System.String` / `nvarchar(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| FirstStartId / FirstStartId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| FormalSourceSubmissionId / FormalSourceSubmissionId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ItemId / ItemId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| LocationFactsJson / LocationFactsJson | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| LocationState / LocationState | `System.String` / `nvarchar(40)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=40 precision=None scale=None |
| MissingReasonsJson / MissingReasonsJson | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| OperationOriginId / OperationOriginId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| OriginId / OriginId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| OriginalActorId / OriginalActorId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| PayloadJson / PayloadJson | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Readiness / Readiness | `System.String` / `nvarchar(40)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=40 precision=None scale=None |
| ServerReceivedAt / ServerReceivedAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SessionId / SessionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Stage / Stage | `System.String` / `nvarchar(40)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=40 precision=None scale=None |
| TaskId / TaskId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.RepairNormalSuccessors

Entity: `RoadGuardSystem.BusinessObjects.Repairs.RepairNormalSuccessor`; [RoadGuardSystem.BusinessObjects/Repairs/RepairProducerSources.cs:61](../../../RoadGuardSystem.BusinessObjects/Repairs/RepairProducerSources.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ActorId / ActorId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| At / At | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ObligationId / ObligationId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Reason / Reason | `System.String` / `nvarchar(2000)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=2000 precision=None scale=None |
| SourceAssessmentId / SourceAssessmentId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SourceCancellationEventId / SourceCancellationEventId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SourceDecisionId / SourceDecisionId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SourceHandoverEventId / SourceHandoverEventId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SourceItemId / SourceItemId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| TargetItemId / TargetItemId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.RepairObligationResolutionEvents

Entity: `RoadGuardSystem.BusinessObjects.Repairs.RepairObligationResolutionEvent`; [RoadGuardSystem.BusinessObjects/Repairs/RepairWork.cs:12](../../../RoadGuardSystem.BusinessObjects/Repairs/RepairWork.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| DecisionId / DecisionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Accepted / Accepted | `System.Boolean` / `bit` | False | False | `Never`; defaultSQL=`None`; default=`False`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| At / At | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ObligationId / ObligationId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| PreviousHeadDecisionId / PreviousHeadDecisionId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SupersedesDecisionId / SupersedesDecisionId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.RepairObligations

Entity: `RoadGuardSystem.BusinessObjects.Repairs.RepairObligation`; [RoadGuardSystem.BusinessObjects/Repairs/RepairWork.cs:68](../../../RoadGuardSystem.BusinessObjects/Repairs/RepairWork.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CurrentRepairItemId / CurrentRepairItemId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| DefectId / DefectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| EffectiveResolutionDecisionId / EffectiveResolutionDecisionId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| EffectiveResolutionHeadDecisionId / EffectiveResolutionHeadDecisionId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Kind / Kind | `RoadGuardSystem.BusinessObjects.Repairs.RepairObligationKind` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.BusinessObjects.Repairs.RepairObligationKind, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| Mandatory / Mandatory | `System.Boolean` / `bit` | False | False | `Never`; defaultSQL=`None`; default=`False`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| OriginalCrewFirstStartId / OriginalCrewFirstStartId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| PackageId / PackageId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RowVersion / RowVersion | `System.Byte[]` / `rowversion` | True | True | `OnAddOrUpdate`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.RepairPackages

Entity: `RoadGuardSystem.BusinessObjects.Repairs.RepairPackage`; [RoadGuardSystem.BusinessObjects/Repairs/RepairPackage.cs:4](../../../RoadGuardSystem.BusinessObjects/Repairs/RepairPackage.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| DefectId / DefectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| DefectStatusAtAnchor / DefectStatusAtAnchor | `System.Nullable1[[RoadGuardSystem.aBusinessObjects.Commons.DefectStatus, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]` / `tinyint` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.aBusinessObjects.Commons.DefectStatus, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| MutationRevision / MutationRevision | `System.Int64` / `bigint` | False | False | `OnAdd`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RowVersion / RowVersion | `System.Byte[]` / `rowversion` | True | True | `OnAddOrUpdate`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.RepairPolicyDraftChanges

Entity: `RoadGuardSystem.BusinessObjects.Repairs.RepairPolicyDraftChange`; [RoadGuardSystem.BusinessObjects/Repairs/RepairPolicyDraft.cs:5](../../../RoadGuardSystem.BusinessObjects/Repairs/RepairPolicyDraft.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ActorId / ActorId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| At / At | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ChecklistVersion / ChecklistVersion | `System.String` / `nvarchar(200)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=200 precision=None scale=None |
| DefectTypeCode / DefectTypeCode | `System.String` / `nvarchar(200)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=200 precision=None scale=None |
| DraftId / DraftId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Measurements / Measurements | `System.Collections.Generic.IReadOnlyList1[[RoadGuardSystem.BusinessObjects.Repairs.RepairMeasurementRule, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter2[[System.Collections.Generic.IReadOnlyList1[[RoadGuardSystem.BusinessObjects.Repairs.RepairMeasurementRule, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.String, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.String`; max=None precision=None scale=None |
| Reason / Reason | `System.String` / `nvarchar(2000)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=2000 precision=None scale=None |
| StopConditions / StopConditions | `System.Collections.Generic.IReadOnlyList1[[System.String, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter2[[System.Collections.Generic.IReadOnlyList1[[System.String, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.String, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.String`; max=None precision=None scale=None |

## dbo.RepairPolicyDrafts

Entity: `RoadGuardSystem.BusinessObjects.Repairs.RepairPolicyDraft`; [RoadGuardSystem.BusinessObjects/Repairs/RepairPolicyDraft.cs:9](../../../RoadGuardSystem.BusinessObjects/Repairs/RepairPolicyDraft.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CurrentChangeId / CurrentChangeId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| PublishedRevisionId / PublishedRevisionId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RowVersion / RowVersion | `System.Byte[]` / `rowversion` | True | True | `OnAddOrUpdate`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.RepairPolicyMeasurementRules

Entity: `RoadGuardSystem.BusinessObjects.Repairs.RepairMeasurementRule`; [RoadGuardSystem.BusinessObjects/Repairs/RepairPolicyAndAuthorization.cs:16](../../../RoadGuardSystem.BusinessObjects/Repairs/RepairPolicyAndAuthorization.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| PolicyRevisionId / PolicyRevisionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Code / Code | `System.String` / `nvarchar(200)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=200 precision=None scale=None |
| Maximum / Maximum | `System.Decimal` / `decimal(20,6)` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=20 scale=6 |
| Minimum / Minimum | `System.Decimal` / `decimal(20,6)` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=20 scale=6 |
| Unit / Unit | `System.String` / `nvarchar(80)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=80 precision=None scale=None |

## dbo.RepairPolicyRevisions

Entity: `RoadGuardSystem.BusinessObjects.Repairs.RepairPolicyRevision`; [RoadGuardSystem.BusinessObjects/Repairs/RepairPolicyAndAuthorization.cs:25](../../../RoadGuardSystem.BusinessObjects/Repairs/RepairPolicyAndAuthorization.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ChecklistVersion / ChecklistVersion | `System.String` / `nvarchar(200)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=200 precision=None scale=None |
| DefectTypeCode / DefectTypeCode | `System.String` / `nvarchar(200)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=200 precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| PublishedAt / PublishedAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| PublishedBy / PublishedBy | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Revision / Revision | `System.Int32` / `int` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| StopConditions / StopConditions | `System.Collections.Generic.IReadOnlyList1[[System.String, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter2[[System.Collections.Generic.IReadOnlyList1[[System.String, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.String, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.String`; max=None precision=None scale=None |

## dbo.RepairPolicyRevocations

Entity: `RoadGuardSystem.BusinessObjects.Repairs.RepairPolicyRevocation`; [RoadGuardSystem.BusinessObjects/Repairs/RepairPolicyAndAuthorization.cs:18](../../../RoadGuardSystem.BusinessObjects/Repairs/RepairPolicyAndAuthorization.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ActorId / ActorId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| At / At | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| PolicyRevisionId / PolicyRevisionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Reason / Reason | `System.String` / `nvarchar(2000)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=2000 precision=None scale=None |

## dbo.RepairReviewRequests

Entity: `RoadGuardSystem.BusinessObjects.Repairs.RepairReviewRequest`; [RoadGuardSystem.BusinessObjects/Repairs/RepairCorrectionBehavior.cs:29](../../../RoadGuardSystem.BusinessObjects/Repairs/RepairCorrectionBehavior.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ActorId / ActorId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| At / At | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| DecisionId / DecisionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ItemId / ItemId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Reason / Reason | `System.String` / `nvarchar(2000)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=2000 precision=None scale=None |
| Role / Role | `RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |

## dbo.RepairSafetyActionSources

Entity: `RoadGuardSystem.BusinessObjects.Repairs.RepairSafetyActionSource`; [RoadGuardSystem.BusinessObjects/Repairs/RepairSafetyActionSource.cs:3](../../../RoadGuardSystem.BusinessObjects/Repairs/RepairSafetyActionSource.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ActorId / ActorId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| At / At | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| BindingId / BindingId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ItemId / ItemId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Kind / Kind | `System.String` / `nvarchar(32)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=32 precision=None scale=None |
| MeasureId / MeasureId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| OriginId / OriginId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Reason / Reason | `System.String` / `nvarchar(2000)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=2000 precision=None scale=None |

## dbo.RepairSafetyCheckEvidence

Entity: `RoadGuardSystem.BusinessObjects.Repairs.RepairSafetyEvidenceReference`; [RoadGuardSystem.BusinessObjects/Repairs/RepairSafetyMonitoring.cs:4](../../../RoadGuardSystem.BusinessObjects/Repairs/RepairSafetyMonitoring.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| CheckId / CheckId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| FileId / FileId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ActualUploaderId / ActualUploaderId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CapturedAt / CapturedAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| FileVersion / FileVersion | `System.String` / `nvarchar(200)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=200 precision=None scale=None |
| Hash / Hash | `System.String` / `nvarchar(64)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| Purpose / Purpose | `RoadGuardSystem.BusinessObjects.Repairs.RepairEvidencePurpose` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.BusinessObjects.Repairs.RepairEvidencePurpose, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| Related / Related | `System.Boolean` / `bit` | False | False | `Never`; defaultSQL=`None`; default=`False`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ReuseDecisionId / ReuseDecisionId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ReusedSource / ReusedSource | `System.Boolean` / `bit` | False | False | `Never`; defaultSQL=`None`; default=`False`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SourceId / SourceId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SourceKind / SourceKind | `System.String` / `nvarchar(60)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=60 precision=None scale=None |
| Verified / Verified | `System.Boolean` / `bit` | False | False | `Never`; defaultSQL=`None`; default=`False`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.RepairSafetyChecks

Entity: `RoadGuardSystem.BusinessObjects.Repairs.RepairSafetyCheck`; [RoadGuardSystem.BusinessObjects/Repairs/RepairSafetyMonitoring.cs:7](../../../RoadGuardSystem.BusinessObjects/Repairs/RepairSafetyMonitoring.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ActorId / ActorId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| At / At | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| EvidenceIds / EvidenceIds | `System.Collections.Generic.IReadOnlyList1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter2[[System.Collections.Generic.IReadOnlyList1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.String, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.String`; max=None precision=None scale=None |
| Findings / Findings | `System.String` / `nvarchar(2000)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=2000 precision=None scale=None |
| MeasureId / MeasureId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Result / Result | `RoadGuardSystem.BusinessObjects.Repairs.RepairSafetyCheckResult` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.BusinessObjects.Repairs.RepairSafetyCheckResult, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |

## dbo.RepairSafetyMonitoring

Entity: `RoadGuardSystem.BusinessObjects.Repairs.RepairSafetyMonitoring`; [RoadGuardSystem.BusinessObjects/Repairs/RepairSafetyMonitoring.cs:26](../../../RoadGuardSystem.BusinessObjects/Repairs/RepairSafetyMonitoring.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CurrentCheckId / CurrentCheckId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| FormalObligationId / FormalObligationId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| MeasureId / MeasureId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RowVersion / RowVersion | `System.Byte[]` / `rowversion` | True | True | `OnAddOrUpdate`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SafetyObligationId / SafetyObligationId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.RepairSafetyResponsibilityTransfers

Entity: `RoadGuardSystem.BusinessObjects.Repairs.SafetyResponsibilityTransfer`; [RoadGuardSystem.BusinessObjects/Repairs/TemporarySafetyMeasure.cs:3](../../../RoadGuardSystem.BusinessObjects/Repairs/TemporarySafetyMeasure.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| At / At | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ChangedBy / ChangedBy | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Handover / Handover | `System.String` / `nvarchar(2000)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=2000 precision=None scale=None |
| MeasureId / MeasureId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| NextActorId / NextActorId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| PreviousActorId / PreviousActorId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Reason / Reason | `System.String` / `nvarchar(2000)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=2000 precision=None scale=None |

## dbo.RepairTemporarySafetyMeasures

Entity: `RoadGuardSystem.BusinessObjects.Repairs.TemporarySafetyMeasure`; [RoadGuardSystem.BusinessObjects/Repairs/TemporarySafetyMeasure.cs:8](../../../RoadGuardSystem.BusinessObjects/Repairs/TemporarySafetyMeasure.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CheckSchedule / CheckSchedule | `System.String` / `nvarchar(2000)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=2000 precision=None scale=None |
| CurrentResponsibilityTransferId / CurrentResponsibilityTransferId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| DefectId / DefectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| FirstCheckDueAt / FirstCheckDueAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| FormalRepairObligationId / FormalRepairObligationId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| InstallationEventId / InstallationEventId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| InstalledAt / InstalledAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| InstalledBy / InstalledBy | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RemovalCondition / RemovalCondition | `System.String` / `nvarchar(2000)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=2000 precision=None scale=None |
| ReplacementCondition / ReplacementCondition | `System.String` / `nvarchar(2000)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=2000 precision=None scale=None |
| ResponsibleActorId / ResponsibleActorId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RowVersion / RowVersion | `System.Byte[]` / `rowversion` | True | True | `OnAddOrUpdate`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.RepairWorkHandovers

Entity: `RoadGuardSystem.BusinessObjects.Repairs.RepairWorkHandover`; [RoadGuardSystem.BusinessObjects/Repairs/RepairWork.cs:129](../../../RoadGuardSystem.BusinessObjects/Repairs/RepairWork.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| At / At | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| FromActorId / FromActorId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ItemId / ItemId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| PerformedScope / PerformedScope | `System.String` / `nvarchar(2000)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=2000 precision=None scale=None |
| SafetyState / SafetyState | `System.String` / `nvarchar(2000)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=2000 precision=None scale=None |
| ToActorId / ToActorId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.ReportOriginalEvidence

Entity: `RoadGuardSystem.BusinessObjects.Reports.Report.OriginalEvidence#ReportEvidence`; [RoadGuardSystem.BusinessObjects/Reports/ReportEvidence.cs:3](../../../RoadGuardSystem.BusinessObjects/Reports/ReportEvidence.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| FileId / FileId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| FileVersion / FileVersion | `System.String` / `nvarchar(200)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=200 precision=None scale=None |
| OwnerUserId / OwnerUserId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ReportId / ReportId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SupplementId / SupplementId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| VerificationState / VerificationState | `RoadGuardSystem.BusinessObjects.Reports.EvidenceVerificationState` / `int` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.BusinessObjects.Reports.EvidenceVerificationState, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Int32, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Int32`; max=None precision=None scale=None |

Entity: `RoadGuardSystem.BusinessObjects.Reports.Report.OriginalEvidence#ReportEvidence.CaptureMetadata#EvidenceCaptureMetadata`; [RoadGuardSystem.BusinessObjects/Reports/EvidenceCaptureMetadata.cs:3](../../../RoadGuardSystem.BusinessObjects/Reports/EvidenceCaptureMetadata.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| ReportEvidenceId / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AccuracyMeters / CaptureMetadata_AccuracyMeters | `System.Nullable1[[System.Decimal, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `decimal(18,3)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=18 scale=3 |
| CapturedAt / CaptureMetadata_CapturedAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Latitude / CaptureMetadata_Latitude | `System.Nullable1[[System.Decimal, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `decimal(10,7)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=10 scale=7 |
| LocationSource / CaptureMetadata_LocationSource | `RoadGuardSystem.BusinessObjects.Reports.EvidenceLocationSource` / `int` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.BusinessObjects.Reports.EvidenceLocationSource, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Int32, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Int32`; max=None precision=None scale=None |
| Longitude / CaptureMetadata_Longitude | `System.Nullable1[[System.Decimal, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `decimal(10,7)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=10 scale=7 |

## dbo.ReportSupplementEvidence

Entity: `RoadGuardSystem.BusinessObjects.Reports.ReportSupplement.Evidence#ReportEvidence`; [RoadGuardSystem.BusinessObjects/Reports/ReportEvidence.cs:3](../../../RoadGuardSystem.BusinessObjects/Reports/ReportEvidence.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| FileId / FileId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| FileVersion / FileVersion | `System.String` / `nvarchar(200)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=200 precision=None scale=None |
| OwnerUserId / OwnerUserId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ReportId / ReportId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SupplementId / SupplementId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| VerificationState / VerificationState | `RoadGuardSystem.BusinessObjects.Reports.EvidenceVerificationState` / `int` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.BusinessObjects.Reports.EvidenceVerificationState, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Int32, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Int32`; max=None precision=None scale=None |

Entity: `RoadGuardSystem.BusinessObjects.Reports.ReportSupplement.Evidence#ReportEvidence.CaptureMetadata#EvidenceCaptureMetadata`; [RoadGuardSystem.BusinessObjects/Reports/EvidenceCaptureMetadata.cs:3](../../../RoadGuardSystem.BusinessObjects/Reports/EvidenceCaptureMetadata.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| ReportEvidenceId / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AccuracyMeters / CaptureMetadata_AccuracyMeters | `System.Nullable1[[System.Decimal, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `decimal(18,3)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=18 scale=3 |
| CapturedAt / CaptureMetadata_CapturedAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Latitude / CaptureMetadata_Latitude | `System.Nullable1[[System.Decimal, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `decimal(10,7)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=10 scale=7 |
| LocationSource / CaptureMetadata_LocationSource | `RoadGuardSystem.BusinessObjects.Reports.EvidenceLocationSource` / `int` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.BusinessObjects.Reports.EvidenceLocationSource, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Int32, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Int32`; max=None precision=None scale=None |
| Longitude / CaptureMetadata_Longitude | `System.Nullable1[[System.Decimal, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `decimal(10,7)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=10 scale=7 |

## dbo.ReportSupplements

Entity: `RoadGuardSystem.BusinessObjects.Reports.ReportSupplement`; [RoadGuardSystem.BusinessObjects/Reports/ReportSupplement.cs:3](../../../RoadGuardSystem.BusinessObjects/Reports/ReportSupplement.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Description / Description | `System.String` / `nvarchar(1000)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=1000 precision=None scale=None |
| ReceivedAt / ReceivedAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ReportId / ReportId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.ReporterRegistrationIntents

Entity: `RoadGuardSystem.BusinessObjects.Identity.ReporterRegistrationIntent`; [RoadGuardSystem.BusinessObjects/Identity/ReporterRegistrationIntent.cs:5](../../../RoadGuardSystem.BusinessObjects/Identity/ReporterRegistrationIntent.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `OnAdd`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ConsumedAt / ConsumedAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset(7)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CreatedAt / CreatedAt | `System.DateTimeOffset` / `datetimeoffset(7)` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| EmailConfirmedAt / EmailConfirmedAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset(7)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ExpiresAt / ExpiresAt | `System.DateTimeOffset` / `datetimeoffset(7)` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| FailedAttempts / FailedAttempts | `System.Int32` / `int` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| NormalizedEmail / NormalizedEmail | `System.String` / `nvarchar(254)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=254 precision=None scale=None |
| OtpGeneration / OtpGeneration | `System.Int32` / `int` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| OtpHash / OtpHash | `System.String` / `char(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| ReporterType / ReporterType | `RoadGuardSystem.aBusinessObjects.Commons.ReporterType` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.aBusinessObjects.Commons.ReporterType, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| ResendAvailableAt / ResendAvailableAt | `System.DateTimeOffset` / `datetimeoffset(7)` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RowVersion / RowVersion | `System.Byte[]` / `rowversion` | False | True | `OnAddOrUpdate`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| UserId / UserId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.Reports

Entity: `RoadGuardSystem.BusinessObjects.Reports.Report`; [RoadGuardSystem.BusinessObjects/Reports/Report.cs:3](../../../RoadGuardSystem.BusinessObjects/Reports/Report.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Description / Description | `System.String` / `nvarchar(1000)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=1000 precision=None scale=None |
| ReceivedAt / ReceivedAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ReporterUserId / ReporterUserId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Revision / Revision | `System.Int64` / `bigint` | False | False | `OnAdd`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RowVersion / RowVersion | `System.Byte[]` / `rowversion` | True | True | `OnAddOrUpdate`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.RetentionBasisHeads

Entity: `RoadGuardSystem.BusinessObjects.Retention.RetentionBasisHead`; [RoadGuardSystem.BusinessObjects/Retention/RetentionRecords.cs:3](../../../RoadGuardSystem.BusinessObjects/Retention/RetentionRecords.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| FileId / FileId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Revision / Revision | `System.Int32` / `int` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RevisionId / RevisionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RowVersion / RowVersion | `System.Byte[]` / `rowversion` | False | True | `OnAddOrUpdate`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.RetentionBasisRevisions

Entity: `RoadGuardSystem.BusinessObjects.Retention.RetentionBasisRevision`; [RoadGuardSystem.BusinessObjects/Retention/RetentionRecords.cs:10](../../../RoadGuardSystem.BusinessObjects/Retention/RetentionRecords.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Classification / Classification | `System.String` / `nvarchar(40)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=40 precision=None scale=None |
| ConfirmedAt / ConfirmedAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ConfirmedBy / ConfirmedBy | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| FileId / FileId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| InventoryComplete / InventoryComplete | `System.Boolean` / `bit` | False | False | `Never`; defaultSQL=`None`; default=`False`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| InventoryVersion / InventoryVersion | `System.String` / `nvarchar(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| PolicyVersion / PolicyVersion | `System.String` / `nvarchar(40)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=40 precision=None scale=None |
| Reason / Reason | `System.String` / `nvarchar(2000)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=2000 precision=None scale=None |
| Revision / Revision | `System.Int32` / `int` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SupersedesId / SupersedesId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| WarrantyReferencesJson / WarrantyReferencesJson | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.RetentionEvaluationItems

Entity: `RoadGuardSystem.BusinessObjects.Retention.RetentionEvaluationItem`; [RoadGuardSystem.BusinessObjects/Retention/RetentionRecords.cs:60](../../../RoadGuardSystem.BusinessObjects/Retention/RetentionRecords.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| BasisVersion / BasisVersion | `System.String` / `nvarchar(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| ControlSnapshotJson / ControlSnapshotJson | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Eligibility / Eligibility | `System.String` / `nvarchar(40)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=40 precision=None scale=None |
| EligibleAfter / EligibleAfter | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| EvaluationId / EvaluationId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| FileId / FileId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| HoldVersion / HoldVersion | `System.String` / `nvarchar(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| InventoryVersion / InventoryVersion | `System.String` / `nvarchar(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| ReasonCodesJson / ReasonCodesJson | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.RetentionEvaluations

Entity: `RoadGuardSystem.BusinessObjects.Retention.RetentionEvaluation`; [RoadGuardSystem.BusinessObjects/Retention/RetentionRecords.cs:47](../../../RoadGuardSystem.BusinessObjects/Retention/RetentionRecords.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CreatedAt / CreatedAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| EvaluatedAt / EvaluatedAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| PolicyVersion / PolicyVersion | `System.String` / `nvarchar(40)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=40 precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RequestedBy / RequestedBy | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RowVersion / RowVersion | `System.Byte[]` / `rowversion` | False | True | `OnAddOrUpdate`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SelectionJson / SelectionJson | `System.String` / `nvarchar(max)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Status / Status | `System.String` / `nvarchar(20)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=20 precision=None scale=None |

## dbo.RetentionHoldHistories

Entity: `RoadGuardSystem.BusinessObjects.Retention.RetentionHoldHistory`; [RoadGuardSystem.BusinessObjects/Retention/RetentionRecords.cs:38](../../../RoadGuardSystem.BusinessObjects/Retention/RetentionRecords.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ActorId / ActorId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| HoldId / HoldId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| OccurredAt / OccurredAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Reason / Reason | `System.String` / `nvarchar(2000)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=2000 precision=None scale=None |
| State / State | `System.String` / `nvarchar(20)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=20 precision=None scale=None |

## dbo.RetentionHolds

Entity: `RoadGuardSystem.BusinessObjects.Retention.RetentionHold`; [RoadGuardSystem.BusinessObjects/Retention/RetentionRecords.cs:25](../../../RoadGuardSystem.BusinessObjects/Retention/RetentionRecords.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CreatedAt / CreatedAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CreatedBy / CreatedBy | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Reason / Reason | `System.String` / `nvarchar(2000)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=2000 precision=None scale=None |
| ReleasedAt / ReleasedAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ReleasedBy / ReleasedBy | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RowVersion / RowVersion | `System.Byte[]` / `rowversion` | False | True | `OnAddOrUpdate`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ScopeId / ScopeId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ScopeType / ScopeType | `System.String` / `nvarchar(20)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=20 precision=None scale=None |
| State / State | `System.String` / `nvarchar(20)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=20 precision=None scale=None |

## dbo.RoadCoverageMappings

Entity: `RoadGuardSystem.BusinessObjects.Repairs.RoadCoverageMapping`; [RoadGuardSystem.BusinessObjects/Repairs/RoadCoverageMapping.cs:5](../../../RoadGuardSystem.BusinessObjects/Repairs/RoadCoverageMapping.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ActorId / ActorId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ApplicableFromUtc / ApplicableFromUtc | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ApplicableToUtc / ApplicableToUtc | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ConfirmedAtUtc / ConfirmedAtUtc | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CoverageFileId / CoverageFileId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| From / From | `System.Decimal` / `decimal(18,3)` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=18 scale=3 |
| HandoverDocumentId / HandoverDocumentId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| HandoverFileId / HandoverFileId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| HandoverVersion / HandoverVersion | `System.String` / `varchar(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| LocationVersion / LocationVersion | `System.String` / `nvarchar(200)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=200 precision=None scale=None |
| OffsetFrom / OffsetFrom | `System.Decimal` / `decimal(18,3)` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=18 scale=3 |
| OffsetTo / OffsetTo | `System.Decimal` / `decimal(18,3)` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=18 scale=3 |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Provenance / Provenance | `System.String` / `varchar(20)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=20 precision=None scale=None |
| Reason / Reason | `System.String` / `nvarchar(2000)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=2000 precision=None scale=None |
| RoadSectionId / RoadSectionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ScopeObligationId / ScopeObligationId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SourceFactsJson / SourceFactsJson | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SourceId / SourceId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SourceKind / SourceKind | `System.String` / `varchar(30)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=30 precision=None scale=None |
| SourceVersion / SourceVersion | `System.String` / `varchar(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| SupersedesId / SupersedesId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| To / To | `System.Decimal` / `decimal(18,3)` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=18 scale=3 |

## dbo.RoadGeometryDrafts

Entity: `RoadGuardSystem.BusinessObjects.Projects.RoadGeometryDraft`; [RoadGuardSystem.BusinessObjects/Projects/RoadGeometryDraft.cs:3](../../../RoadGuardSystem.BusinessObjects/Projects/RoadGeometryDraft.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CreatedAt / CreatedAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CreatedBy / CreatedBy | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| InputJson / InputJson | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| OriginalCoordinatesJson / OriginalCoordinatesJson | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RoadCode / RoadCode | `System.String` / `nvarchar(80)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=80 precision=None scale=None |
| RoadName / RoadName | `System.String` / `nvarchar(255)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=255 precision=None scale=None |
| RoadSectionId / RoadSectionId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RowVersion / RowVersion | `System.Byte[]` / `rowversion` | False | True | `OnAddOrUpdate`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SourceChecksum / SourceChecksum | `System.String` / `nvarchar(64)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| Status / Status | `System.String` / `nvarchar(16)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=16 precision=None scale=None |
| UpdatedAt / UpdatedAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| UpdatedBy / UpdatedBy | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.RoadGeometryMetadata

Entity: `RoadGuardSystem.BusinessObjects.Projects.RoadGeometryMetadata`; [RoadGuardSystem.BusinessObjects/Projects/RoadGeometryMetadata.cs:3](../../../RoadGuardSystem.BusinessObjects/Projects/RoadGeometryMetadata.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| RoadSectionVersionId / RoadSectionVersionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ApprovedAt / ApprovedAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ApprovedBy / ApprovedBy | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| GeometryHash / GeometryHash | `System.String` / `nvarchar(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| InputJson / InputJson | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SourceDraftId / SourceDraftId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Wgs84GeometryJson / Wgs84GeometryJson | `System.String` / `nvarchar(max)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.RoadRouteSystems

Entity: `RoadGuardSystem.BusinessObjects.Projects.RoadRouteSystem`; [RoadGuardSystem.BusinessObjects/Projects/CrsProfileRevision.cs:17](../../../RoadGuardSystem.BusinessObjects/Projects/CrsProfileRevision.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Code / Code | `System.String` / `nvarchar(80)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=80 precision=None scale=None |
| Name / Name | `System.String` / `nvarchar(255)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=255 precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.RoadSectionVersions

Entity: `RoadGuardSystem.BusinessObjects.Projects.RoadSectionVersion`; [RoadGuardSystem.BusinessObjects/Projects/RoadSectionVersion.cs:5](../../../RoadGuardSystem.BusinessObjects/Projects/RoadSectionVersion.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ChangeReason / ChangeReason | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CrsProfileRevisionId / CrsProfileRevisionId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| EffectiveFrom / EffectiveFrom | `System.DateTimeOffset` / `datetimeoffset(7)` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Geometry / Geometry | `NetTopologySuite.Geometries.LineString` / `geometry` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| IsCurrent / IsCurrent | `System.Boolean` / `bit` | False | False | `Never`; defaultSQL=`None`; default=`False`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RoadSectionId / RoadSectionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| VersionNo / VersionNo | `System.Int32` / `int` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.RoadSections

Entity: `RoadGuardSystem.BusinessObjects.Projects.RoadSection`; [RoadGuardSystem.BusinessObjects/Projects/RoadSection.cs:3](../../../RoadGuardSystem.BusinessObjects/Projects/RoadSection.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Code / Code | `System.String` / `varchar(80)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=80 precision=None scale=None |
| Name / Name | `System.String` / `nvarchar(255)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=255 precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.RoadSegmentSets

Entity: `RoadGuardSystem.BusinessObjects.Projects.RoadSegmentSet`; [RoadGuardSystem.BusinessObjects/Projects/RoadSegmentSet.cs:3](../../../RoadGuardSystem.BusinessObjects/Projects/RoadSegmentSet.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| DefinitionJson / DefinitionJson | `System.String` / `nvarchar(max)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| GeometryHash / GeometryHash | `System.String` / `nvarchar(64)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| PublishedAt / PublishedAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| PublishedBy / PublishedBy | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RoadSectionVersionId / RoadSectionVersionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RowVersion / RowVersion | `System.Byte[]` / `rowversion` | False | True | `OnAddOrUpdate`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Status / Status | `System.String` / `varchar(16)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=16 precision=None scale=None |

## dbo.RoadSegments

Entity: `RoadGuardSystem.BusinessObjects.Projects.RoadSegment`; [RoadGuardSystem.BusinessObjects/Projects/RoadSegment.cs:3](../../../RoadGuardSystem.BusinessObjects/Projects/RoadSegment.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| EndStationMeters / EndStationMeters | `System.Nullable1[[System.Double, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `float` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| FromOffsetMeters / FromOffsetMeters | `System.Nullable1[[System.Double, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `float` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Geometry / Geometry | `NetTopologySuite.Geometries.LineString` / `geometry` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RoadSectionVersionId / RoadSectionVersionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SegmentSetId / SegmentSetId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Sequence / Sequence | `System.Int32` / `int` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| StartStationMeters / StartStationMeters | `System.Nullable1[[System.Double, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `float` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ToOffsetMeters / ToOffsetMeters | `System.Nullable1[[System.Double, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `float` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.Roles

Entity: `RoadGuardSystem.BusinessObjects.Identity.ApplicationRole`; [RoadGuardSystem.BusinessObjects/Identity/ApplicationRole.cs:11](../../../RoadGuardSystem.BusinessObjects/Identity/ApplicationRole.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Code / Code | `RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode` / `varchar(40)` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter2[[RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.String, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.String`; max=40 precision=None scale=None |
| ConcurrencyStamp / ConcurrencyStamp | `System.String` / `nvarchar(36)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=36 precision=None scale=None |
| IsActive / IsActive | `System.Boolean` / `bit` | False | False | `Never`; defaultSQL=`None`; default=`False`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Name / Name | `System.String` / `nvarchar(100)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=100 precision=None scale=None |
| NormalizedName / NormalizedName | `System.String` / `nvarchar(100)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=100 precision=None scale=None |

## dbo.Sessions

Entity: `RoadGuardSystem.BusinessObjects.Identity.UserSession`; [RoadGuardSystem.BusinessObjects/Identity/UserSession.cs:6](../../../RoadGuardSystem.BusinessObjects/Identity/UserSession.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `OnAdd`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| DeviceMetadataJson / DeviceMetadataJson | `System.String` / `nvarchar(max)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ExpiresAt / ExpiresAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset(7)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| IssuedAt / IssuedAt | `System.DateTimeOffset` / `datetimeoffset(7)` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| IssuedRole / IssuedRole | `System.Nullable1[[RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]` / `nvarchar(40)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToStringConverter1[[RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]`; provider=`System.String`; max=40 precision=None scale=None |
| LastActivityAt / LastActivityAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset(7)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Lifecycle / Lifecycle | `RoadGuardSystem.BusinessObjects.Identity.SessionLifecycle` / `tinyint` | False | False | `OnAdd`; defaultSQL=`None`; default=`LegacyBounded`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.BusinessObjects.Identity.SessionLifecycle, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| RevokedAt / RevokedAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset(7)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RowVersion / RowVersion | `System.Byte[]` / `rowversion` | False | True | `OnAddOrUpdate`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Transport / Transport | `RoadGuardSystem.BusinessObjects.Identity.SessionTransport` / `tinyint` | False | False | `OnAdd`; defaultSQL=`None`; default=`LegacyBearer`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.BusinessObjects.Identity.SessionTransport, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| UserId / UserId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.SeverityRuleVersions

Entity: `RoadGuardSystem.BusinessObjects.Catalogs.SeverityRuleVersion`; [RoadGuardSystem.BusinessObjects/Catalogs/SeverityRuleVersion.cs:5](../../../RoadGuardSystem.BusinessObjects/Catalogs/SeverityRuleVersion.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| EffectiveFrom / EffectiveFrom | `System.DateOnly` / `date` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| EffectiveTo / EffectiveTo | `System.Nullable1[[System.DateOnly, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `date` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RoadTypeCode / RoadTypeCode | `System.String` / `varchar(80)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=80 precision=None scale=None |
| RuleDefinition / RuleDefinition | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| StandardCode / StandardCode | `System.String` / `varchar(80)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=80 precision=None scale=None |
| VersionNo / VersionNo | `System.Int32` / `int` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.SourceDecisions

Entity: `RoadGuardSystem.BusinessObjects.Candidates.CandidateClassification`; [RoadGuardSystem.BusinessObjects/Candidates/CandidateClassification.cs:5](../../../RoadGuardSystem.BusinessObjects/Candidates/CandidateClassification.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| CandidateDecisionId / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CauseCategoryCode / Classification_CauseCategoryCode | `System.String` / `varchar(80)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=80 precision=None scale=None |
| DefectTypeCode / Classification_DefectTypeCode | `System.String` / `varchar(80)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=80 precision=None scale=None |
| RoadSectionVersionId / Classification_RoadSectionVersionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SegmentId / Classification_SegmentId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Severity / Classification_Severity | `RoadGuardSystem.aBusinessObjects.Commons.DefectSeverity` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.aBusinessObjects.Commons.DefectSeverity, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |

Entity: `RoadGuardSystem.BusinessObjects.Candidates.CandidateDecision`; [RoadGuardSystem.BusinessObjects/Candidates/CandidateDecision.cs:3](../../../RoadGuardSystem.BusinessObjects/Candidates/CandidateDecision.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AIDetectionSourceId / AIDetectionSourceId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| DecidedAt / DecidedAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| DecidedByUserId / DecidedByUserId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Decision / Decision | `RoadGuardSystem.BusinessObjects.Candidates.CandidateDecisionKind` / `int` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.BusinessObjects.Candidates.CandidateDecisionKind, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Int32, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Int32`; max=None precision=None scale=None |
| ExpectedPreviousDecisionVersion / ExpectedPreviousDecisionVersion | `System.String` / `nvarchar(200)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=200 precision=None scale=None |
| GeometryVersion / GeometryVersion | `System.String` / `nvarchar(200)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=200 precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Reason / Reason | `System.String` / `nvarchar(1000)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=1000 precision=None scale=None |
| ReportSourceId / ReportSourceId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RowVersion / RowVersion | `System.Byte[]` / `rowversion` | True | True | `OnAddOrUpdate`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SourceId / SourceId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SourceKind / SourceKind | `RoadGuardSystem.BusinessObjects.Candidates.CandidateSourceKind` / `int` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.BusinessObjects.Candidates.CandidateSourceKind, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Int32, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Int32`; max=None precision=None scale=None |
| SupersedesDecisionId / SupersedesDecisionId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| TargetDefectId / TargetDefectId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| TargetDefectVersion / TargetDefectVersion | `System.String` / `nvarchar(200)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=200 precision=None scale=None |

Entity: `RoadGuardSystem.BusinessObjects.Candidates.CandidateSourceIdentity`; [RoadGuardSystem.BusinessObjects/Candidates/CandidateSourceIdentity.cs:3](../../../RoadGuardSystem.BusinessObjects/Candidates/CandidateSourceIdentity.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| CandidateDecisionId / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Id / Source_Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Kind / Source_Kind | `RoadGuardSystem.BusinessObjects.Candidates.CandidateSourceKind` / `int` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.BusinessObjects.Candidates.CandidateSourceKind, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Int32, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Int32`; max=None precision=None scale=None |
| SourceVersion / Source_Version | `System.String` / `nvarchar(200)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=200 precision=None scale=None |

## dbo.StaffInvitationProjects

Entity: `RoadGuardSystem.BusinessObjects.Identity.StaffInvitationProject`; [RoadGuardSystem.BusinessObjects/Identity/StaffInvitationProject.cs:3](../../../RoadGuardSystem.BusinessObjects/Identity/StaffInvitationProject.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| InvitationId / InvitationId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.StaffInvitations

Entity: `RoadGuardSystem.BusinessObjects.Identity.StaffInvitation`; [RoadGuardSystem.BusinessObjects/Identity/StaffInvitation.cs:5](../../../RoadGuardSystem.BusinessObjects/Identity/StaffInvitation.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `OnAdd`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AcceptedAt / AcceptedAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset(7)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CreatedAt / CreatedAt | `System.DateTimeOffset` / `datetimeoffset(7)` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CreatedByUserId / CreatedByUserId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| DisplayName / DisplayName | `System.String` / `nvarchar(200)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=200 precision=None scale=None |
| Email / Email | `System.String` / `nvarchar(254)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=254 precision=None scale=None |
| ExpiresAt / ExpiresAt | `System.DateTimeOffset` / `datetimeoffset(7)` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| NormalizedEmail / NormalizedEmail | `System.String` / `nvarchar(254)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=254 precision=None scale=None |
| RevokedAt / RevokedAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset(7)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RoleCode / RoleCode | `RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode` / `varchar(40)` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter2[[RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.String, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.String`; max=40 precision=None scale=None |
| RowVersion / RowVersion | `System.Byte[]` / `rowversion` | False | True | `OnAddOrUpdate`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| TokenHash / TokenHash | `System.String` / `char(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |

## dbo.SupplementarySurveyRequests

Entity: `RoadGuardSystem.BusinessObjects.Surveys.SupplementarySurveyRequest`; [RoadGuardSystem.BusinessObjects/Surveys/SupplementarySurveyRequest.cs:6](../../../RoadGuardSystem.BusinessObjects/Surveys/SupplementarySurveyRequest.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ApprovedAt / ApprovedAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset(7)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ApprovedByUserId / ApprovedByUserId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Reason / Reason | `System.String` / `nvarchar(1000)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RequestedByUserId / RequestedByUserId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RequestedScope / RequestedScope | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RoundNo / RoundNo | `System.Int32` / `int` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SourcePreservationNote / SourcePreservationNote | `System.String` / `nvarchar(max)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Status / Status | `RoadGuardSystem.aBusinessObjects.Commons.SupplementarySurveyRequestStatus` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.aBusinessObjects.Commons.SupplementarySurveyRequestStatus, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| SurveyId / SurveyId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SurveyRequestId / SurveyRequestId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.SurveyAssignments

Entity: `RoadGuardSystem.BusinessObjects.Surveys.SurveyAssignment`; [RoadGuardSystem.BusinessObjects/Surveys/SurveyAssignment.cs:3](../../../RoadGuardSystem.BusinessObjects/Surveys/SurveyAssignment.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AcceptedAt / AcceptedAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset(7)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AssignedAt / AssignedAt | `System.DateTimeOffset` / `datetimeoffset(7)` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AssignedByUserId / AssignedByUserId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| EndedAt / EndedAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset(7)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| OperatorUserId / OperatorUserId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ReassignmentReason / ReassignmentReason | `System.String` / `nvarchar(1000)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RejectedAt / RejectedAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset(7)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RejectionReason / RejectionReason | `System.String` / `nvarchar(1000)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SurveyRequestId / SurveyRequestId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.SurveyDataVersions

Entity: `RoadGuardSystem.BusinessObjects.Surveys.SurveyDataVersion`; [RoadGuardSystem.BusinessObjects/Surveys/SurveyDataVersion.cs:6](../../../RoadGuardSystem.BusinessObjects/Surveys/SurveyDataVersion.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ConfirmedAt / ConfirmedAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset(7)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ConfirmedBy / ConfirmedBy | `System.Nullable1[[RoadGuardSystem.aBusinessObjects.Commons.SurveyDataConfirmationActor, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]` / `tinyint` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.aBusinessObjects.Commons.SurveyDataConfirmationActor, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| DeviceId / DeviceId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| IntegrityStatus / IntegrityStatus | `RoadGuardSystem.aBusinessObjects.Commons.SurveyDataIntegrityStatus` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.aBusinessObjects.Commons.SurveyDataIntegrityStatus, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| PairsManifest / PairsManifest | `System.String` / `nvarchar(max)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RecordedAt / RecordedAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset(7)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RowVersion / RowVersion | `System.Byte[]` / `rowversion` | False | True | `OnAddOrUpdate`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ScopeManifest / ScopeManifest | `System.String` / `nvarchar(max)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SourceManifest / SourceManifest | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Status / Status | `RoadGuardSystem.aBusinessObjects.Commons.SurveyDataVersionStatus` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.aBusinessObjects.Commons.SurveyDataVersionStatus, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| SubmittedBy / SubmittedBy | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SurveyId / SurveyId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| VersionNo / VersionNo | `System.Int32` / `int` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.SurveyFiles

Entity: `RoadGuardSystem.BusinessObjects.Surveys.SurveyFile`; [RoadGuardSystem.BusinessObjects/Surveys/SurveyFile.cs:5](../../../RoadGuardSystem.BusinessObjects/Surveys/SurveyFile.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CaptureEndedAt / CaptureEndedAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset(7)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CaptureStartedAt / CaptureStartedAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset(7)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Checksum / Checksum | `System.String` / `char(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| FileId / FileId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| FileType / FileType | `RoadGuardSystem.aBusinessObjects.Commons.SurveyFileType` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.aBusinessObjects.Commons.SurveyFileType, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| FlightId / FlightId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SurveyId / SurveyId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SyncStatus / SyncStatus | `RoadGuardSystem.aBusinessObjects.Commons.SurveyFileSyncStatus` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.aBusinessObjects.Commons.SurveyFileSyncStatus, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |

## dbo.SurveyPlanPostponements

Entity: `RoadGuardSystem.BusinessObjects.Surveys.SurveyPlanPostponement`; [RoadGuardSystem.BusinessObjects/Surveys/SurveyPlanPostponement.cs:3](../../../RoadGuardSystem.BusinessObjects/Surveys/SurveyPlanPostponement.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| NewPlannedStartAt / NewPlannedStartAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset(7)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| PostponedAt / PostponedAt | `System.DateTimeOffset` / `datetimeoffset(7)` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Reason / Reason | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SurveyPlanId / SurveyPlanId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.SurveyPlanScopes

Entity: `RoadGuardSystem.BusinessObjects.Surveys.SurveyPlanScope`; [RoadGuardSystem.BusinessObjects/Surveys/SurveyPlanScope.cs:5](../../../RoadGuardSystem.BusinessObjects/Surveys/SurveyPlanScope.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RouteSectionVersionId / RouteSectionVersionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SegmentIdsJson / SegmentIdsJson | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SegmentSetId / SegmentSetId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SurveyPlanId / SurveyPlanId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| TargetBand / TargetBand | `System.String` / `nvarchar(32)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.SurveyPlans

Entity: `RoadGuardSystem.BusinessObjects.Surveys.SurveyPlan`; [RoadGuardSystem.BusinessObjects/Surveys/SurveyPlan.cs:6](../../../RoadGuardSystem.BusinessObjects/Surveys/SurveyPlan.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| OutputRequirements / OutputRequirements | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| PlannedEndAt / PlannedEndAt | `System.DateTimeOffset` / `datetimeoffset(7)` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| PlannedStartAt / PlannedStartAt | `System.DateTimeOffset` / `datetimeoffset(7)` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RoadSectionId / RoadSectionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RoadSectionVersionId / RoadSectionVersionId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RowVersion / RowVersion | `System.Byte[]` / `rowversion` | False | True | `OnAddOrUpdate`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ScopeFormatVersion / ScopeFormatVersion | `System.String` / `nvarchar(20)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=20 precision=None scale=None |
| Status / Status | `RoadGuardSystem.aBusinessObjects.Commons.SurveyPlanStatus` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.aBusinessObjects.Commons.SurveyPlanStatus, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| SurveyType / SurveyType | `RoadGuardSystem.aBusinessObjects.Commons.SurveyType` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.aBusinessObjects.Commons.SurveyType, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |

## dbo.SurveyRequestScopes

Entity: `RoadGuardSystem.BusinessObjects.Surveys.SurveyRequestScope`; [RoadGuardSystem.BusinessObjects/Surveys/SurveyRequestScope.cs:5](../../../RoadGuardSystem.BusinessObjects/Surveys/SurveyRequestScope.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RouteSectionVersionId / RouteSectionVersionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SegmentIdsJson / SegmentIdsJson | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SegmentSetId / SegmentSetId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SurveyRequestId / SurveyRequestId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| TargetBand / TargetBand | `System.String` / `nvarchar(32)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.SurveyRequests

Entity: `RoadGuardSystem.BusinessObjects.Surveys.SurveyRequest`; [RoadGuardSystem.BusinessObjects/Surveys/SurveyRequest.cs:6](../../../RoadGuardSystem.BusinessObjects/Surveys/SurveyRequest.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CancellationReason / CancellationReason | `System.String` / `nvarchar(max)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CancelledAt / CancelledAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset(7)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| DueAt / DueAt | `System.DateTimeOffset` / `datetimeoffset(7)` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| OutputRequirements / OutputRequirements | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ParentTaskId / ParentTaskId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RequestedAt / RequestedAt | `System.DateTimeOffset` / `datetimeoffset(7)` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RequestedByUserId / RequestedByUserId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RoadSectionId / RoadSectionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RoadSectionVersionId / RoadSectionVersionId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RowVersion / RowVersion | `System.Byte[]` / `rowversion` | False | True | `OnAddOrUpdate`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ScopeFormatVersion / ScopeFormatVersion | `System.String` / `nvarchar(20)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=20 precision=None scale=None |
| Status / Status | `RoadGuardSystem.aBusinessObjects.Commons.SurveyRequestStatus` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.aBusinessObjects.Commons.SurveyRequestStatus, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| SupplementRequestId / SupplementRequestId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SurveyPlanId / SurveyPlanId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SurveyType / SurveyType | `RoadGuardSystem.aBusinessObjects.Commons.SurveyType` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.aBusinessObjects.Commons.SurveyType, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |

## dbo.Surveys

Entity: `RoadGuardSystem.BusinessObjects.Surveys.Survey`; [RoadGuardSystem.BusinessObjects/Surveys/Survey.cs:5](../../../RoadGuardSystem.BusinessObjects/Surveys/Survey.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| BaselineConfirmedAt / BaselineConfirmedAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset(7)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| BaselineConfirmedByUserId / BaselineConfirmedByUserId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| IsBaselineConfirmed / IsBaselineConfirmed | `System.Boolean` / `bit` | False | False | `Never`; defaultSQL=`None`; default=`False`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RoadSectionVersionId / RoadSectionVersionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Status / Status | `RoadGuardSystem.aBusinessObjects.Commons.SurveyStatus` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.aBusinessObjects.Commons.SurveyStatus, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| SurveyRequestId / SurveyRequestId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SurveyType / SurveyType | `RoadGuardSystem.aBusinessObjects.Commons.SurveyType` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.aBusinessObjects.Commons.SurveyType, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |

## dbo.TrainingLabelReviews

Entity: `RoadGuardSystem.Repositories.Models.Huy01.HuyTrainingLabelReview`; [RoadGuardSystem.Repositories/Models/Huy01/HuyTrainingLabelRows.cs:33](../../../RoadGuardSystem.Repositories/Models/Huy01/HuyTrainingLabelRows.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ActorUserId / ActorUserId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Decision / Decision | `System.String` / `varchar(10)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=10 precision=None scale=None |
| LabelId / LabelId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Reason / Reason | `System.String` / `nvarchar(1000)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=1000 precision=None scale=None |
| ReviewedAt / ReviewedAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RevisionId / RevisionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.TrainingLabelRevisions

Entity: `RoadGuardSystem.Repositories.Models.Huy01.HuyTrainingLabelRevision`; [RoadGuardSystem.Repositories/Models/Huy01/HuyTrainingLabelRows.cs:16](../../../RoadGuardSystem.Repositories/Models/Huy01/HuyTrainingLabelRows.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CreatedAt / CreatedAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| DefectTypeCode / DefectTypeCode | `System.String` / `varchar(80)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=80 precision=None scale=None |
| FileId / FileId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| FileVersion / FileVersion | `System.String` / `nvarchar(200)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=200 precision=None scale=None |
| Height / Height | `System.Decimal` / `decimal(9,7)` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=9 scale=7 |
| LabelId / LabelId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Reason / Reason | `System.String` / `nvarchar(1000)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=1000 precision=None scale=None |
| Revision / Revision | `System.Int32` / `int` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SourceVersion / SourceVersion | `System.String` / `nvarchar(200)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=200 precision=None scale=None |
| Width / Width | `System.Decimal` / `decimal(9,7)` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=9 scale=7 |
| X / X | `System.Decimal` / `decimal(9,7)` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=9 scale=7 |
| Y / Y | `System.Decimal` / `decimal(9,7)` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=9 scale=7 |

## dbo.TrainingLabels

Entity: `RoadGuardSystem.Repositories.Models.Huy01.HuyTrainingLabelHead`; [RoadGuardSystem.Repositories/Models/Huy01/HuyTrainingLabelRows.cs:3](../../../RoadGuardSystem.Repositories/Models/Huy01/HuyTrainingLabelRows.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AIDetectionSourceId / AIDetectionSourceId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| CurrentRevision / CurrentRevision | `System.Int32` / `int` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ReportSourceId / ReportSourceId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RowVersion / RowVersion | `System.Byte[]` / `rowversion` | False | True | `OnAddOrUpdate`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SourceId / SourceId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SourceKind / SourceKind | `System.String` / `varchar(20)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=20 precision=None scale=None |
| UpdatedAt / UpdatedAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.UploadMultipartSweeps

Entity: `RoadGuardSystem.Repositories.Models.UploadMultipartSweep`; [RoadGuardSystem.Repositories/Models/UploadMultipartSweep.cs:7](../../../RoadGuardSystem.Repositories/Models/UploadMultipartSweep.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| UploadSessionId / UploadSessionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| NextCheckAt / NextCheckAt | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.UploadParts

Entity: `RoadGuardSystem.BusinessObjects.Files.UploadPart`; [RoadGuardSystem.BusinessObjects/Files/UploadPart.cs:3](../../../RoadGuardSystem.BusinessObjects/Files/UploadPart.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ETag / ETag | `System.String` / `varchar(512)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=512 precision=None scale=None |
| PartNumber / PartNumber | `System.Int32` / `int` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| UploadSessionId / UploadSessionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| UrlExpiresAt / UrlExpiresAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset(7)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| UrlIssuedAt / UrlIssuedAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset(7)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.UploadSessions

Entity: `RoadGuardSystem.BusinessObjects.Files.UploadSession`; [RoadGuardSystem.BusinessObjects/Files/UploadSession.cs:5](../../../RoadGuardSystem.BusinessObjects/Files/UploadSession.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ExpectedChecksumSha256 / ExpectedChecksumSha256 | `System.String` / `char(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ExpectedSizeBytes / ExpectedSizeBytes | `System.Int64` / `bigint` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ExpiresAt / ExpiresAt | `System.DateTimeOffset` / `datetimeoffset(7)` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| FailureCode / FailureCode | `System.String` / `varchar(80)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=80 precision=None scale=None |
| FileId / FileId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| MediaType / MediaType | `System.String` / `varchar(120)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=120 precision=None scale=None |
| MultipartDeadline / MultipartDeadline | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| MultipartFence / MultipartFence | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| MultipartNextCheckAt / MultipartNextCheckAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| MultipartPhase / MultipartPhase | `System.String` / `varchar(20)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=20 precision=None scale=None |
| ObjectKey / ObjectKey | `System.String` / `varchar(512)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=512 precision=None scale=None |
| OwnerUserId / OwnerUserId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| PartSizeBytes / PartSizeBytes | `System.Int32` / `int` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Purpose / Purpose | `System.String` / `varchar(40)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=40 precision=None scale=None |
| RowVersion / RowVersion | `System.Byte[]` / `rowversion` | False | True | `OnAddOrUpdate`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Status / Status | `RoadGuardSystem.aBusinessObjects.Commons.UploadSessionStatus` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.aBusinessObjects.Commons.UploadSessionStatus, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| StorageUploadId / StorageUploadId | `System.String` / `varchar(1024)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=1024 precision=None scale=None |

## dbo.Users

Entity: `RoadGuardSystem.BusinessObjects.Identity.ApplicationUser`; [RoadGuardSystem.BusinessObjects/Identity/ApplicationUser.cs:12](../../../RoadGuardSystem.BusinessObjects/Identity/ApplicationUser.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `OnAdd`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| AccessFailedCount / AccessFailedCount | `System.Int32` / `int` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ConcurrencyStamp / ConcurrencyStamp | `System.String` / `nvarchar(36)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=36 precision=None scale=None |
| CreatedAt / CreatedAt | `System.DateTimeOffset` / `datetimeoffset(7)` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| DisplayName / DisplayName | `System.String` / `nvarchar(200)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=200 precision=None scale=None |
| Email / Email | `System.String` / `nvarchar(254)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=254 precision=None scale=None |
| EmailConfirmed / EmailConfirmed | `System.Boolean` / `bit` | False | False | `Never`; defaultSQL=`None`; default=`False`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| LastLoginAt / LastLoginAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset(7)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| LockoutEnabled / LockoutEnabled | `System.Boolean` / `bit` | False | False | `Never`; defaultSQL=`None`; default=`False`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| LockoutEnd / LockoutEnd | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| MustChangePassword / MustChangePassword | `System.Boolean` / `bit` | False | False | `Never`; defaultSQL=`None`; default=`False`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| NormalizedEmail / NormalizedEmail | `System.String` / `nvarchar(254)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=254 precision=None scale=None |
| NormalizedUserName / NormalizedUserName | `System.String` / `nvarchar(100)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=100 precision=None scale=None |
| PasswordHash / PasswordHash | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| PhoneNumber / PhoneNumber | `System.String` / `nvarchar(50)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=50 precision=None scale=None |
| PhoneNumberConfirmed / PhoneNumberConfirmed | `System.Boolean` / `bit` | False | False | `Never`; defaultSQL=`None`; default=`False`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RoleCode / RoleCode | `RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode` / `varchar(40)` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter2[[RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.String, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.String`; max=40 precision=None scale=None |
| RowVersion / RowVersion | `System.Byte[]` / `rowversion` | False | True | `OnAddOrUpdate`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| SecurityStamp / SecurityStamp | `System.String` / `nvarchar(36)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=36 precision=None scale=None |
| Status / Status | `RoadGuardSystem.aBusinessObjects.Commons.UserStatus` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.aBusinessObjects.Commons.UserStatus, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| SuspendedAt / SuspendedAt | `System.Nullable1[[System.DateTimeOffset, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `datetimeoffset(7)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| TwoFactorEnabled / TwoFactorEnabled | `System.Boolean` / `bit` | False | False | `Never`; defaultSQL=`None`; default=`False`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| UserName / UserName | `System.String` / `nvarchar(100)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=100 precision=None scale=None |

## dbo.ValidationRuns

Entity: `RoadGuardSystem.BusinessObjects.Processing.ValidationRun`; [RoadGuardSystem.BusinessObjects/Processing/ValidationRun.cs:6](../../../RoadGuardSystem.BusinessObjects/Processing/ValidationRun.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Bias / Bias | `System.Nullable1[[System.Decimal, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `decimal(18,6)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| DatasetSplitId / DatasetSplitId | `System.String` / `varchar(120)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=120 precision=None scale=None |
| ExcludedCount / ExcludedCount | `System.Int32` / `int` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ExclusionReasonsJson / ExclusionReasonsJson | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Mae / Mae | `System.Nullable1[[System.Decimal, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `decimal(18,6)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| MeasurementType / MeasurementType | `System.String` / `varchar(80)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=80 precision=None scale=None |
| ModelVersionId / ModelVersionId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| PairsJson / PairsJson | `System.String` / `nvarchar(max)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Rmse / Rmse | `System.Nullable1[[System.Decimal, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `decimal(18,6)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RowVersion / RowVersion | `System.Byte[]` / `rowversion` | False | True | `OnAddOrUpdate`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Status / Status | `RoadGuardSystem.aBusinessObjects.Commons.ValidationRunStatus` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.aBusinessObjects.Commons.ValidationRunStatus, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| Unit / Unit | `System.String` / `varchar(32)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=32 precision=None scale=None |
| UsedCount / UsedCount | `System.Int32` / `int` | False | False | `Never`; defaultSQL=`None`; default=`0`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.Warranties

Entity: `RoadGuardSystem.BusinessObjects.Warranties.Warranty`; [RoadGuardSystem.BusinessObjects/Warranties/Warranty.cs:5](../../../RoadGuardSystem.BusinessObjects/Warranties/Warranty.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| HandoverDate / HandoverDate | `System.DateOnly` / `date` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| HandoverDocumentId / HandoverDocumentId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RetainedValue / RetainedValue | `System.Nullable1[[System.Decimal, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `decimal(19,2)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=19 scale=2 |
| RoadSectionId / RoadSectionId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Scope / Scope | `RoadGuardSystem.aBusinessObjects.Commons.WarrantyScope` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.aBusinessObjects.Commons.WarrantyScope, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| SourceDocumentId / SourceDocumentId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Status / Status | `RoadGuardSystem.aBusinessObjects.Commons.WarrantyStatus` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.aBusinessObjects.Commons.WarrantyStatus, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| Terms / Terms | `System.String` / `nvarchar(max)` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| WarrantyEndDate / WarrantyEndDate | `System.DateOnly` / `date` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| WarrantyStartDate / WarrantyStartDate | `System.DateOnly` / `date` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.WeeklyReviewDigestDuties

Entity: `RoadGuardSystem.BusinessObjects.Messaging.WeeklyReviewDigestDuty`; [RoadGuardSystem.BusinessObjects/Messaging/WeeklyReviewDigest.cs:26](../../../RoadGuardSystem.BusinessObjects/Messaging/WeeklyReviewDigest.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| DigestId / DigestId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ClockId / ClockId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| DueAtRecoveryUtc / DueAtRecoveryUtc | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| Kind / Kind | `System.String` / `varchar(64)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=64 precision=None scale=None |
| OriginAtUtc / OriginAtUtc | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| OriginEventId / OriginEventId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| TargetId / TargetId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.WeeklyReviewDigests

Entity: `RoadGuardSystem.BusinessObjects.Messaging.WeeklyReviewDigest`; [RoadGuardSystem.BusinessObjects/Messaging/WeeklyReviewDigest.cs:14](../../../RoadGuardSystem.BusinessObjects/Messaging/WeeklyReviewDigest.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RecipientId / RecipientId | `System.Nullable1[[System.Guid, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]` / `uniqueidentifier` | True | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RecipientKey / RecipientKey | `System.String` / `varchar(100)` | False | False | `Never`; defaultSQL=`None`; default=`None`; computed=`None`; collation=`None`; `None`; provider=`None`; max=100 precision=None scale=None |
| RecipientRole / RecipientRole | `RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode` / `tinyint` | False | False | `Never`; defaultSQL=`None`; default=`Unknown`; computed=`None`; collation=`None`; `Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToNumberConverter2[[RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode, RoadGuardSystem.aBusinessObjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null],[System.Byte, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]`; provider=`System.Byte`; max=None precision=None scale=None |
| RecoveredAtUtc / RecoveredAtUtc | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RecoveryPeriodId / RecoveryPeriodId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ScheduledAtUtc / ScheduledAtUtc | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

## dbo.WeeklyReviewRecoveryPeriods

Entity: `RoadGuardSystem.BusinessObjects.Messaging.WeeklyReviewRecoveryPeriod`; [RoadGuardSystem.BusinessObjects/Messaging/WeeklyReviewDigest.cs:6](../../../RoadGuardSystem.BusinessObjects/Messaging/WeeklyReviewDigest.cs)

| Property / column | CLR / SQL type | Nullable | Concurrency | Generation / converter / facets |
|---|---|---|---|---|
| Id / Id | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| IsLatestAtRecovery / IsLatestAtRecovery | `System.Boolean` / `bit` | False | False | `Never`; defaultSQL=`None`; default=`False`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ProjectId / ProjectId | `System.Guid` / `uniqueidentifier` | False | False | `Never`; defaultSQL=`None`; default=`00000000-0000-0000-0000-000000000000`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| RecoveredAtUtc / RecoveredAtUtc | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |
| ScheduledAtUtc / ScheduledAtUtc | `System.DateTimeOffset` / `datetimeoffset` | False | False | `Never`; defaultSQL=`None`; default=`1/1/0001 12:00:00 AM +00:00`; computed=`None`; collation=`None`; `None`; provider=`None`; max=None precision=None scale=None |

Relational-count clarification:662CLR FK declaration rows are not the physical constraint count. Fresh EF relational mapping is657FK/692indexes/219keys/229checks; migration source adds5FK/1index, yielding662FK/693indexes. Historical fixture catalog verifies the latter at its source baseline; live SQL remains NOT_RUN.
