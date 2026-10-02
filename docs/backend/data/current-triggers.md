# Current SQL trigger definitions

CURRENT_VERIFIED in an isolated migrated SQL Server only. Raw `sys.sql_modules.definition` is retained in [inventory](current-schema.inventory.json) under `sql.triggers`; this page presents it for reading. Definitions are source text, not runtime behavior tests or accepted business rules. SHA-256 uses UTF-8 after CRLF/CR to LF conversion, with no trimming or SQL rewriting. Comparison permits outer whitespace only as a separately labeled formatting difference; catalog and migration hashes remain distinct.

### dbo.TR_AccountStatusChangeLogs_AppendOnly

- Table: dbo.AccountStatusChangeLogs. Enabled: True. Timing: INSTEAD OF. Events: DELETE,UPDATE. Not-for-replication: False.
- Observed definition SHA-256 (UTF-8/LF): 5a7da5c98aad9f2e14224303b4e8a4bf6c79b7676b6f0e1ccfdac98c43df1185. Latest migration SQL SHA-256 (UTF-8/LF): dd321fe4361d013024e75444e94cc1bad4837fd1418b7cb9524b6cd7bd119ac1. Text comparison: OUTER_WHITESPACE_ONLY. Runtime behavior: NOT_TESTED.
- Created in 20260918152126_AddIdentitySessionSecurityLogs. Latest definition in 20260918152126_AddIdentitySessionSecurityLogs: [source](../../../RoadGuardSystem.Repositories/Migrations/20260918152126_AddIdentitySessionSecurityLogs.cs#L273). Ordered Up-operation history: 20260918152126_AddIdentitySessionSecurityLogs:CREATE.
- Technical interpretation from observed THROW messages: AccountStatusChangeLogs are append-only; updates and deletions are forbidden. This does not establish the full trigger policy.

```sql
CREATE TRIGGER [TR_AccountStatusChangeLogs_AppendOnly]
ON [AccountStatusChangeLogs]
INSTEAD OF UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    THROW 51000, 'AccountStatusChangeLogs are append-only; updates and deletions are forbidden.', 1;
END

```

### dbo.TR_AIDetections_Immutable

- Table: dbo.AIDetections. Enabled: True. Timing: AFTER. Events: DELETE,UPDATE. Not-for-replication: False.
- Observed definition SHA-256 (UTF-8/LF): 7b750929df2e6143f21c1ff192da667ccd8a9e0041a81bcc3e1e00c043153a6f. Latest migration SQL SHA-256 (UTF-8/LF): 0db9f11c0d77baf5503ea87d8c9c532dab9681a9683a54f8db133017e0a8dde7. Text comparison: OUTER_WHITESPACE_ONLY. Runtime behavior: NOT_TESTED.
- Created in 20260922034831_P232DetectionDefectTaskSchema. Latest definition in 20260922034831_P232DetectionDefectTaskSchema: [source](../../../RoadGuardSystem.Repositories/Migrations/20260922034831_P232DetectionDefectTaskSchema.cs#L360). Ordered Up-operation history: 20260922034831_P232DetectionDefectTaskSchema:CREATE.
- Technical interpretation from observed THROW messages: AIDetections are immutable. This does not establish the full trigger policy.

```sql
CREATE TRIGGER [TR_AIDetections_Immutable]
ON [dbo].[AIDetections]
AFTER UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    THROW 51033, 'AIDetections are immutable.', 1;
END

```

### dbo.TR_AuditLogs_AppendOnly

- Table: dbo.AuditLogs. Enabled: True. Timing: INSTEAD OF. Events: DELETE,UPDATE. Not-for-replication: False.
- Observed definition SHA-256 (UTF-8/LF): 4fc0ae0b4e8ed77eababdbd9edf99beb8dbb07afe91ba8913be77750317c005a. Latest migration SQL SHA-256 (UTF-8/LF): 7a4c1ce0299560eb7a945d5bb066e52ac86b415cb848022ff28aecee7ec87f8c. Text comparison: OUTER_WHITESPACE_ONLY. Runtime behavior: NOT_TESTED.
- Created in 20260918065914_AddAuditOutboxIdempotencyConcurrencyPrimitives. Latest definition in 20260918065914_AddAuditOutboxIdempotencyConcurrencyPrimitives: [source](../../../RoadGuardSystem.Repositories/Migrations/20260918065914_AddAuditOutboxIdempotencyConcurrencyPrimitives.cs#L138). Ordered Up-operation history: 20260918065914_AddAuditOutboxIdempotencyConcurrencyPrimitives:CREATE.
- Technical interpretation from observed THROW messages: AuditLogs are append-only; corrections require a new audit event. This does not establish the full trigger policy.

```sql
CREATE TRIGGER [TR_AuditLogs_AppendOnly]
ON [AuditLogs]
INSTEAD OF UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    THROW 51000, 'AuditLogs are append-only; corrections require a new audit event.', 1;
END

```

### dbo.TR_DefectVerificationLogs_AppendOnly

- Table: dbo.DefectVerificationLogs. Enabled: True. Timing: AFTER. Events: DELETE,UPDATE. Not-for-replication: False.
- Observed definition SHA-256 (UTF-8/LF): 5ec1f57e4d4a758b049fa0871c92b533d4f6578f45e41c9d094ae87ad72b8569. Latest migration SQL SHA-256 (UTF-8/LF): 1975ea2cce64f09f4ccba123f2a57519a04ef0ea3cb8fcbc3247bde90cbb4bfc. Text comparison: OUTER_WHITESPACE_ONLY. Runtime behavior: NOT_TESTED.
- Created in 20260922034831_P232DetectionDefectTaskSchema. Latest definition in 20260922034831_P232DetectionDefectTaskSchema: [source](../../../RoadGuardSystem.Repositories/Migrations/20260922034831_P232DetectionDefectTaskSchema.cs#L372). Ordered Up-operation history: 20260922034831_P232DetectionDefectTaskSchema:CREATE.
- Technical interpretation from observed THROW messages: DefectVerificationLogs are append-only. This does not establish the full trigger policy.

```sql
CREATE TRIGGER [TR_DefectVerificationLogs_AppendOnly]
ON [dbo].[DefectVerificationLogs]
AFTER UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    THROW 51034, 'DefectVerificationLogs are append-only.', 1;
END

```

### dbo.TR_FieldInspectionSessions_Immutable

- Table: dbo.FieldInspectionSessions. Enabled: True. Timing: AFTER. Events: DELETE,UPDATE. Not-for-replication: False.
- Observed definition SHA-256 (UTF-8/LF): dd35c9499d9157e3890f4e2bee4f39de2d5312af095dc3587dcefa4e1f6c261e. Latest migration SQL SHA-256 (UTF-8/LF): ff5aef4010728e7baf44681f98dea065007a0345e31a72a80fe8b63d0a2e4077. Text comparison: OUTER_WHITESPACE_ONLY. Runtime behavior: NOT_TESTED.
- Created in 20260922045838_P240FieldInspectionMeasurementSchema. Latest definition in 20260922045838_P240FieldInspectionMeasurementSchema: [source](../../../RoadGuardSystem.Repositories/Migrations/20260922045838_P240FieldInspectionMeasurementSchema.cs#L295). Ordered Up-operation history: 20260922045838_P240FieldInspectionMeasurementSchema:CREATE.
- Technical interpretation from observed THROW messages: Completed, imported, or locked field inspection sessions are immutable. This does not establish the full trigger policy.

```sql
CREATE TRIGGER [TR_FieldInspectionSessions_Immutable]
ON [dbo].[FieldInspectionSessions]
AFTER UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS
    (
        SELECT 1
        FROM deleted AS old_session
        WHERE old_session.[Status] IN (2, 3, 4)
    )
    BEGIN
        THROW 51041, 'Completed, imported, or locked field inspection sessions are immutable.', 1;
    END
END

```

### dbo.TR_FieldInspectionSessions_Integrity

- Table: dbo.FieldInspectionSessions. Enabled: True. Timing: AFTER. Events: INSERT,UPDATE. Not-for-replication: False.
- Observed definition SHA-256 (UTF-8/LF): 394ab3ab651ccce26c2720a6bf6965c86c48c30cd31200f4c4906e765abd29ba. Latest migration SQL SHA-256 (UTF-8/LF): 380a0194bda8c6cf349c7388ee73a2a342b244c3c8021664fc20577f453ca268. Text comparison: OUTER_WHITESPACE_ONLY. Runtime behavior: NOT_TESTED.
- Created in 20260922045838_P240FieldInspectionMeasurementSchema. Latest definition in 20260922045838_P240FieldInspectionMeasurementSchema: [source](../../../RoadGuardSystem.Repositories/Migrations/20260922045838_P240FieldInspectionMeasurementSchema.cs#L260). Ordered Up-operation history: 20260922045838_P240FieldInspectionMeasurementSchema:CREATE.
- Technical interpretation from observed THROW messages: Field inspection session scope or active assignment is invalid. This does not establish the full trigger policy.

```sql
CREATE TRIGGER [TR_FieldInspectionSessions_Integrity]
ON [dbo].[FieldInspectionSessions]
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS
    (
        SELECT 1
        FROM inserted AS session
        LEFT JOIN [dbo].[FieldInspectionTasks] AS task
            ON task.[Id] = session.[FieldInspectionTaskId]
        WHERE (session.[Purpose] = 1 AND
               (task.[Id] IS NULL OR
                task.[ProjectId] <> session.[ProjectId] OR
                task.[RoadSectionVersionId] <> session.[RoadSectionVersionId] OR
                task.[SurveyId] <> session.[SurveyId] OR
                NOT EXISTS
                (
                    SELECT 1
                    FROM [dbo].[FieldInspectionAssignments] AS assignment
                    WHERE assignment.[FieldInspectionTaskId] = task.[Id]
                      AND assignment.[AssignedToUserId] = session.[InspectorUserId]
                      AND assignment.[Status] = 1
                )))
           OR (session.[Purpose] = 2 AND session.[FieldInspectionTaskId] IS NOT NULL)
    )
    BEGIN
        THROW 51040, 'Field inspection session scope or active assignment is invalid.', 1;
    END
END

```

### dbo.TR_Files_Immutable

- Table: dbo.Files. Enabled: True. Timing: AFTER. Events: DELETE,UPDATE. Not-for-replication: False.
- Observed definition SHA-256 (UTF-8/LF): 515885d587d28f20870fb18f7dc0dcb9ca09d035aa2dea9c4da94b49387188db. Latest migration SQL SHA-256 (UTF-8/LF): e94f15c3ee9719b77f68dcbdc9415b12024b7a112bb3b696d96a69ef51c35f08. Text comparison: OUTER_WHITESPACE_ONLY. Runtime behavior: NOT_TESTED.
- Created in 20260919085118_AddImmutableFileStorageBoundary. Latest definition in 20260919085118_AddImmutableFileStorageBoundary: [source](../../../RoadGuardSystem.Repositories/Migrations/20260919085118_AddImmutableFileStorageBoundary.cs#L54). Ordered Up-operation history: 20260919085118_AddImmutableFileStorageBoundary:CREATE.
- Technical interpretation from observed THROW messages: Files are immutable; create a new file identity and use the retention workflow for deletion. This does not establish the full trigger policy.

```sql
CREATE TRIGGER [TR_Files_Immutable]
ON [Files]
AFTER UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    THROW 51020, 'Files are immutable; create a new file identity and use the retention workflow for deletion.', 1;
END

```

### dbo.TR_Flights_ImmutableSurvey

- Table: dbo.Flights. Enabled: True. Timing: AFTER. Events: UPDATE. Not-for-replication: False.
- Observed definition SHA-256 (UTF-8/LF): 2c9ae62bd2e38799b27b3b0097fb02fc9540104919d37db41fdae6c69211e1d8. Latest migration SQL SHA-256 (UTF-8/LF): 66db3d62a54923fca372a0d74a362c974133721066ebb73279c404d0ab8ccf05. Text comparison: OUTER_WHITESPACE_ONLY. Runtime behavior: NOT_TESTED.
- Created in 20260921134719_AddP230FlightSurveyIdentityImmutability. Latest definition in 20260921134719_AddP230FlightSurveyIdentityImmutability: [source](../../../RoadGuardSystem.Repositories/Migrations/20260921134719_AddP230FlightSurveyIdentityImmutability.cs#L15). Ordered Up-operation history: 20260921134719_AddP230FlightSurveyIdentityImmutability:CREATE.
- Technical interpretation from observed THROW messages: Flight survey identity is immutable. This does not establish the full trigger policy.

```sql
CREATE TRIGGER [TR_Flights_ImmutableSurvey]
ON [dbo].[Flights]
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF UPDATE([SurveyId])
    BEGIN
        THROW 51011, 'Flight survey identity is immutable.', 1;
    END
END

```

### dbo.TR_GroundTruthMeasurements_Immutable

- Table: dbo.GroundTruthMeasurements. Enabled: True. Timing: AFTER. Events: DELETE,UPDATE. Not-for-replication: False.
- Observed definition SHA-256 (UTF-8/LF): 0e7e79167d25842833202fc03316e98239510f5e39298078877da686d659eab7. Latest migration SQL SHA-256 (UTF-8/LF): 37c71174615e7605563dc4265a5f997cb639d4336904c24911fce4f24d95e3fa. Text comparison: OUTER_WHITESPACE_ONLY. Runtime behavior: NOT_TESTED.
- Created in 20260922045838_P240FieldInspectionMeasurementSchema. Latest definition in 20260922045838_P240FieldInspectionMeasurementSchema: [source](../../../RoadGuardSystem.Repositories/Migrations/20260922045838_P240FieldInspectionMeasurementSchema.cs#L351). Ordered Up-operation history: 20260922045838_P240FieldInspectionMeasurementSchema:CREATE.
- Technical interpretation from observed THROW messages: Submitted ground truth measurements are immutable. This does not establish the full trigger policy.

```sql
CREATE TRIGGER [TR_GroundTruthMeasurements_Immutable]
ON [dbo].[GroundTruthMeasurements]
AFTER UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS
    (
        SELECT 1
        FROM deleted AS old_measurement
        INNER JOIN [dbo].[FieldInspectionSessions] AS session
            ON session.[Id] = old_measurement.[FieldInspectionSessionId]
        WHERE session.[Status] IN (2, 3, 4)
    )
    BEGIN
        THROW 51043, 'Submitted ground truth measurements are immutable.', 1;
    END
END

```

### dbo.TR_GroundTruthMeasurements_Integrity

- Table: dbo.GroundTruthMeasurements. Enabled: True. Timing: AFTER. Events: INSERT,UPDATE. Not-for-replication: False.
- Observed definition SHA-256 (UTF-8/LF): bb7f8f02756f4c602935db3446759fbee85c20d9123ce986031c3273b2223b83. Latest migration SQL SHA-256 (UTF-8/LF): 119301314e1fd8905ff588bde70883ccb69026dfffa984c60d5021c0ab1d1572. Text comparison: OUTER_WHITESPACE_ONLY. Runtime behavior: NOT_TESTED.
- Created in 20260922045838_P240FieldInspectionMeasurementSchema. Latest definition in 20260922045838_P240FieldInspectionMeasurementSchema: [source](../../../RoadGuardSystem.Repositories/Migrations/20260922045838_P240FieldInspectionMeasurementSchema.cs#L315). Ordered Up-operation history: 20260922045838_P240FieldInspectionMeasurementSchema:CREATE.
- Technical interpretation from observed THROW messages: Ground truth measurement purpose or scope is invalid. This does not establish the full trigger policy.

```sql
CREATE TRIGGER [TR_GroundTruthMeasurements_Integrity]
ON [dbo].[GroundTruthMeasurements]
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS
    (
        SELECT 1
        FROM inserted AS measurement
        INNER JOIN [dbo].[FieldInspectionSessions] AS session
            ON session.[Id] = measurement.[FieldInspectionSessionId]
        LEFT JOIN [dbo].[FieldInspectionTasks] AS task
            ON task.[Id] = session.[FieldInspectionTaskId]
        LEFT JOIN [dbo].[Defects] AS defect
            ON defect.[Id] = measurement.[DefectId]
        WHERE measurement.[RoadSectionVersionId] <> session.[RoadSectionVersionId]
           OR (session.[Purpose] = 1 AND
               (measurement.[DefectId] IS NULL OR
                measurement.[SurveyId] IS NULL OR
                measurement.[SurveyId] <> session.[SurveyId] OR
                measurement.[DefectId] <> task.[DefectId] OR
                defect.[ProjectId] <> session.[ProjectId] OR
                defect.[RoadSectionVersionId] <> session.[RoadSectionVersionId] OR
                defect.[Status] <> 1))
           OR (session.[Purpose] = 2 AND
               (measurement.[DefectId] IS NOT NULL OR measurement.[SurveyId] IS NOT NULL))
    )
    BEGIN
        THROW 51042, 'Ground truth measurement purpose or scope is invalid.', 1;
    END
END

```

### dbo.TR_PasswordResetLogs_AppendOnly

- Table: dbo.PasswordResetLogs. Enabled: True. Timing: INSTEAD OF. Events: DELETE,UPDATE. Not-for-replication: False.
- Observed definition SHA-256 (UTF-8/LF): bea32a8d647d750db82958b2701cc4e3f36c58307fcd4876ebd4af1621d244fe. Latest migration SQL SHA-256 (UTF-8/LF): ba93c39ed5a76f4c5e76083bab712eb6b06b38b99e9748c27bd78e00f1427a29. Text comparison: OUTER_WHITESPACE_ONLY. Runtime behavior: NOT_TESTED.
- Created in 20260918152126_AddIdentitySessionSecurityLogs. Latest definition in 20260918152126_AddIdentitySessionSecurityLogs: [source](../../../RoadGuardSystem.Repositories/Migrations/20260918152126_AddIdentitySessionSecurityLogs.cs#L261). Ordered Up-operation history: 20260918152126_AddIdentitySessionSecurityLogs:CREATE.
- Technical interpretation from observed THROW messages: PasswordResetLogs are append-only; updates and deletions are forbidden. This does not establish the full trigger policy.

```sql
CREATE TRIGGER [TR_PasswordResetLogs_AppendOnly]
ON [PasswordResetLogs]
INSTEAD OF UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    THROW 51000, 'PasswordResetLogs are append-only; updates and deletions are forbidden.', 1;
END

```

### dbo.TR_ProcessingAttempts_AppendOnly

- Table: dbo.ProcessingAttempts. Enabled: True. Timing: AFTER. Events: DELETE,UPDATE. Not-for-replication: False.
- Observed definition SHA-256 (UTF-8/LF): 0373b5ff28d33a373e393e2d932947632659c54754ea198f0377c8ee334ab5ca. Latest migration SQL SHA-256 (UTF-8/LF): 16c6bf6d9b12787d8e6739e32daca1490b0b38563ef8467a667a9d47e96af7ac. Text comparison: OUTER_WHITESPACE_ONLY. Runtime behavior: NOT_TESTED.
- Created in 20260921182227_P231ProcessingAndOutboxDelivery. Latest definition in 20260921182227_P231ProcessingAndOutboxDelivery: [source](../../../RoadGuardSystem.Repositories/Migrations/20260921182227_P231ProcessingAndOutboxDelivery.cs#L253). Ordered Up-operation history: 20260921182227_P231ProcessingAndOutboxDelivery:CREATE.
- Technical interpretation from observed THROW messages: ProcessingAttempts are append-only. This does not establish the full trigger policy.

```sql
CREATE TRIGGER [TR_ProcessingAttempts_AppendOnly]
ON [dbo].[ProcessingAttempts]
AFTER UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    THROW 51032, 'ProcessingAttempts are append-only.', 1;
END

```

### dbo.TR_ProcessingBlocks_Immutable

- Table: dbo.ProcessingBlocks. Enabled: True. Timing: AFTER. Events: DELETE,UPDATE. Not-for-replication: False.
- Observed definition SHA-256 (UTF-8/LF): 296cce851bf563a1a84a0aacad7303d34968ea521b78f46e506b4f072853d0b3. Latest migration SQL SHA-256 (UTF-8/LF): ddc17e6f3623044807f06cbbd68d15f5de1953529539b42412a61abe25ca95e7. Text comparison: OUTER_WHITESPACE_ONLY. Runtime behavior: NOT_TESTED.
- Created in 20260921182227_P231ProcessingAndOutboxDelivery. Latest definition in 20260921182227_P231ProcessingAndOutboxDelivery: [source](../../../RoadGuardSystem.Repositories/Migrations/20260921182227_P231ProcessingAndOutboxDelivery.cs#L241). Ordered Up-operation history: 20260921182227_P231ProcessingAndOutboxDelivery:CREATE.
- Technical interpretation from observed THROW messages: ProcessingBlocks are immutable. This does not establish the full trigger policy.

```sql
CREATE TRIGGER [TR_ProcessingBlocks_Immutable]
ON [dbo].[ProcessingBlocks]
AFTER UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    THROW 51031, 'ProcessingBlocks are immutable.', 1;
END

```

### dbo.TR_RoadSectionVersions_Immutable

- Table: dbo.RoadSectionVersions. Enabled: True. Timing: AFTER. Events: DELETE,UPDATE. Not-for-replication: False.
- Observed definition SHA-256 (UTF-8/LF): 705aba8b7ac6cff4087d998e75a1ec25e8105b2938bbc8b0c4df6948cd0c5905. Latest migration SQL SHA-256 (UTF-8/LF): 201cf7dcc3ef5b30479367e9f2263165c8cc94df63058398c2eb31948a4d413a. Text comparison: OUTER_WHITESPACE_ONLY. Runtime behavior: NOT_TESTED.
- Created in 20260920154542_AddRoadSectionVersionAndWarrantySchema. Latest definition in 20260920154542_AddRoadSectionVersionAndWarrantySchema: [source](../../../RoadGuardSystem.Repositories/Migrations/20260920154542_AddRoadSectionVersionAndWarrantySchema.cs#L164). Ordered Up-operation history: 20260920154542_AddRoadSectionVersionAndWarrantySchema:CREATE.
- Technical interpretation from observed THROW messages: RoadSectionVersion records cannot be deleted.; RoadSectionVersion history is immutable; only IsCurrent may change. This does not establish the full trigger policy.

```sql
CREATE TRIGGER [dbo].[TR_RoadSectionVersions_Immutable]
ON [dbo].[RoadSectionVersions]
AFTER UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM inserted)
    BEGIN
        THROW 50000, 'RoadSectionVersion records cannot be deleted.', 1;
    END;

    IF UPDATE([RoadSectionId]) OR UPDATE([VersionNo]) OR UPDATE([Geometry]) OR
       UPDATE([EffectiveFrom]) OR UPDATE([ChangeReason])
    BEGIN
        THROW 50001, 'RoadSectionVersion history is immutable; only IsCurrent may change.', 1;
    END;
END

```

### dbo.TR_SurveyDataVersions_Immutable

- Table: dbo.SurveyDataVersions. Enabled: True. Timing: AFTER. Events: UPDATE. Not-for-replication: False.
- Observed definition SHA-256 (UTF-8/LF): 10bae429e5d7e39d89c67d77c3853765ac6043c7e0b6ca82d6d38387da34a9cd. Latest migration SQL SHA-256 (UTF-8/LF): 8e583cd38b917516cab938531dee602401bc13c746ccee6c21fdf786a97bba36. Text comparison: OUTER_WHITESPACE_ONLY. Runtime behavior: NOT_TESTED.
- Created in 20260921132735_AddP230ConfirmedDatasetImmutability. Latest definition in 20260921132735_AddP230ConfirmedDatasetImmutability: [source](../../../RoadGuardSystem.Repositories/Migrations/20260921132735_AddP230ConfirmedDatasetImmutability.cs#L15). Ordered Up-operation history: 20260921132735_AddP230ConfirmedDatasetImmutability:CREATE.
- Technical interpretation from observed THROW messages: Survey dataset identity is immutable.; Confirmed or superseded survey dataset cannot regress status.; Confirmed or superseded survey dataset manifest is immutable. This does not establish the full trigger policy.

```sql
CREATE TRIGGER [TR_SurveyDataVersions_Immutable]
ON [dbo].[SurveyDataVersions]
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF UPDATE([SurveyId]) OR UPDATE([VersionNo])
    BEGIN
        THROW 51008, 'Survey dataset identity is immutable.', 1;
    END

    IF UPDATE([Status]) AND EXISTS
    (
        SELECT 1
        FROM inserted AS [current]
        INNER JOIN deleted AS [previous]
            ON [previous].[Id] = [current].[Id]
        WHERE [previous].[Status] IN (3, 5)
            AND [current].[Status] NOT IN (3, 5)
    )
    BEGIN
        THROW 51010, 'Confirmed or superseded survey dataset cannot regress status.', 1;
    END

    IF UPDATE([SourceManifest]) AND EXISTS
    (
        SELECT 1
        FROM inserted AS [current]
        INNER JOIN deleted AS [previous]
            ON [previous].[Id] = [current].[Id]
        WHERE [previous].[Status] IN (3, 5)
    )
    BEGIN
        THROW 51009, 'Confirmed or superseded survey dataset manifest is immutable.', 1;
    END
END

```

### dbo.TR_SurveyFiles_ScopeIntegrity

- Table: dbo.SurveyFiles. Enabled: True. Timing: AFTER. Events: INSERT,UPDATE. Not-for-replication: False.
- Observed definition SHA-256 (UTF-8/LF): 8c325489901ef96bf87143137be6f7a294b506c6ce1b72e533623b9fcad691a3. Latest migration SQL SHA-256 (UTF-8/LF): 473c0b7afdf5f5c10374edc375823814322fe7c5e661f3ab84ffe7d6050878b4. Text comparison: OUTER_WHITESPACE_ONLY. Runtime behavior: NOT_TESTED.
- Created in 20260921125553_AddP230FlightSurveyFileSchema. Latest definition in 20260921125553_AddP230FlightSurveyFileSchema: [source](../../../RoadGuardSystem.Repositories/Migrations/20260921125553_AddP230FlightSurveyFileSchema.cs#L124). Ordered Up-operation history: 20260921125553_AddP230FlightSurveyFileSchema:CREATE.
- Technical interpretation from observed THROW messages: Survey file flight must belong to the same survey.; Survey file checksum must match the immutable stored file checksum. This does not establish the full trigger policy.

```sql
CREATE TRIGGER [TR_SurveyFiles_ScopeIntegrity]
ON [dbo].[SurveyFiles]
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS
    (
        SELECT 1
        FROM inserted AS [surveyFile]
        INNER JOIN [dbo].[Flights] AS [flight]
            ON [flight].[Id] = [surveyFile].[FlightId]
        WHERE [surveyFile].[FlightId] IS NOT NULL
            AND [flight].[SurveyId] <> [surveyFile].[SurveyId]
    )
    BEGIN
        THROW 51006, 'Survey file flight must belong to the same survey.', 1;
    END

    IF EXISTS
    (
        SELECT 1
        FROM inserted AS [surveyFile]
        INNER JOIN [dbo].[Files] AS [storedFile]
            ON [storedFile].[Id] = [surveyFile].[FileId]
        WHERE [surveyFile].[Checksum] <> [storedFile].[Checksum]
    )
    BEGIN
        THROW 51007, 'Survey file checksum must match the immutable stored file checksum.', 1;
    END
END

```

### dbo.TR_SurveyPlanPostponements_AppendOnly

- Table: dbo.SurveyPlanPostponements. Enabled: True. Timing: AFTER. Events: DELETE,UPDATE. Not-for-replication: False.
- Observed definition SHA-256 (UTF-8/LF): 9b7ea56b10e62a11af28ee0e8a7b4b8964c0bdf5817189c77fa8e40dc0fbb21e. Latest migration SQL SHA-256 (UTF-8/LF): c6105c2dd7fc49ac926a595f711f57ad9114e39ec481416e61eaeb638b12fd18. Text comparison: OUTER_WHITESPACE_ONLY. Runtime behavior: NOT_TESTED.
- Created in 20260920172407_AddSurveyPlanningSchema. Latest definition in 20260920172407_AddSurveyPlanningSchema: [source](../../../RoadGuardSystem.Repositories/Migrations/20260920172407_AddSurveyPlanningSchema.cs#L220). Ordered Up-operation history: 20260920172407_AddSurveyPlanningSchema:CREATE.
- Technical interpretation from observed THROW messages: SurveyPlanPostponements are append-only. This does not establish the full trigger policy.

```sql
CREATE TRIGGER [TR_SurveyPlanPostponements_AppendOnly]
ON [dbo].[SurveyPlanPostponements]
AFTER UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    THROW 51000, 'SurveyPlanPostponements are append-only.', 1;
END

```

### dbo.TR_SurveyPlans_ScopeIntegrity

- Table: dbo.SurveyPlans. Enabled: True. Timing: AFTER. Events: INSERT,UPDATE. Not-for-replication: False.
- Observed definition SHA-256 (UTF-8/LF): a1965d9a772699c7c5f7e6c2da73f05748d7091db9321cd9a6ca2c91400c08d0. Latest migration SQL SHA-256 (UTF-8/LF): 511d879a85839178547ecdfec7c87bf8bac8a9f40785e3018807535111156e0c. Text comparison: OUTER_WHITESPACE_ONLY. Runtime behavior: NOT_TESTED.
- Created in 20260920172407_AddSurveyPlanningSchema. Latest definition in 20260920172407_AddSurveyPlanningSchema: [source](../../../RoadGuardSystem.Repositories/Migrations/20260920172407_AddSurveyPlanningSchema.cs#L160). Ordered Up-operation history: 20260920172407_AddSurveyPlanningSchema:CREATE.
- Technical interpretation from observed THROW messages: SurveyPlan road section must belong to its project. This does not establish the full trigger policy.

```sql
CREATE TRIGGER [TR_SurveyPlans_ScopeIntegrity]
ON [dbo].[SurveyPlans]
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS
    (
        SELECT 1
        FROM inserted AS [plan]
        INNER JOIN [dbo].[RoadSections] AS [roadSection]
            ON [roadSection].[Id] = [plan].[RoadSectionId]
        WHERE [roadSection].[ProjectId] <> [plan].[ProjectId]
    )
    BEGIN
        THROW 51001, 'SurveyPlan road section must belong to its project.', 1;
    END
END

```

### dbo.TR_SurveyRequests_ScopeIntegrity

- Table: dbo.SurveyRequests. Enabled: True. Timing: AFTER. Events: INSERT,UPDATE. Not-for-replication: False.
- Observed definition SHA-256 (UTF-8/LF): 793cbb78c41c984ebdedca3eeda16beee38495fb949e4fabb390e8f335b9b6cf. Latest migration SQL SHA-256 (UTF-8/LF): de65d01df65a5e56cbd92d43eeb2bfdecb2371a1bc248357c117d17457eaf9b5. Text comparison: OUTER_WHITESPACE_ONLY. Runtime behavior: NOT_TESTED.
- Created in 20260920172407_AddSurveyPlanningSchema. Latest definition in 20260920172407_AddSurveyPlanningSchema: [source](../../../RoadGuardSystem.Repositories/Migrations/20260920172407_AddSurveyPlanningSchema.cs#L183). Ordered Up-operation history: 20260920172407_AddSurveyPlanningSchema:CREATE.
- Technical interpretation from observed THROW messages: SurveyRequest road section must belong to its project.; SurveyRequest source plan must match project, road section, and survey type. This does not establish the full trigger policy.

```sql
CREATE TRIGGER [TR_SurveyRequests_ScopeIntegrity]
ON [dbo].[SurveyRequests]
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS
    (
        SELECT 1
        FROM inserted AS [request]
        INNER JOIN [dbo].[RoadSections] AS [roadSection]
            ON [roadSection].[Id] = [request].[RoadSectionId]
        WHERE [roadSection].[ProjectId] <> [request].[ProjectId]
    )
    BEGIN
        THROW 51002, 'SurveyRequest road section must belong to its project.', 1;
    END

    IF EXISTS
    (
        SELECT 1
        FROM inserted AS [request]
        INNER JOIN [dbo].[SurveyPlans] AS [plan]
            ON [plan].[Id] = [request].[SurveyPlanId]
        WHERE [plan].[ProjectId] <> [request].[ProjectId]
            OR [plan].[RoadSectionId] <> [request].[RoadSectionId]
            OR [plan].[SurveyType] <> [request].[SurveyType]
    )
    BEGIN
        THROW 51003, 'SurveyRequest source plan must match project, road section, and survey type.', 1;
    END
END

```

### dbo.TR_Surveys_ScopeIntegrity

- Table: dbo.Surveys. Enabled: True. Timing: AFTER. Events: INSERT,UPDATE. Not-for-replication: False.
- Observed definition SHA-256 (UTF-8/LF): d3a7978f37340a7e58540886b621035a96e1404b603d18d60f20de49236c615b. Latest migration SQL SHA-256 (UTF-8/LF): 42acd40ab6eddb42cad691e56bf392ad0efcb04f4c36c73312c9d3c41af90dba. Text comparison: OUTER_WHITESPACE_ONLY. Runtime behavior: NOT_TESTED.
- Created in 20260920182623_AddSurveyAssignmentSchema. Latest definition in 20260920182623_AddSurveyAssignmentSchema: [source](../../../RoadGuardSystem.Repositories/Migrations/20260920182623_AddSurveyAssignmentSchema.cs#L147). Ordered Up-operation history: 20260920182623_AddSurveyAssignmentSchema:CREATE.
- Technical interpretation from observed THROW messages: Survey road section version must belong to its project.; Survey request must match project, road section version, and survey type. This does not establish the full trigger policy.

```sql
CREATE TRIGGER [TR_Surveys_ScopeIntegrity]
ON [dbo].[Surveys]
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS
    (
        SELECT 1
        FROM inserted AS [survey]
        INNER JOIN [dbo].[RoadSectionVersions] AS [roadSectionVersion]
            ON [roadSectionVersion].[Id] = [survey].[RoadSectionVersionId]
        INNER JOIN [dbo].[RoadSections] AS [roadSection]
            ON [roadSection].[Id] = [roadSectionVersion].[RoadSectionId]
        WHERE [roadSection].[ProjectId] <> [survey].[ProjectId]
    )
    BEGIN
        THROW 51004, 'Survey road section version must belong to its project.', 1;
    END

    IF EXISTS
    (
        SELECT 1
        FROM inserted AS [survey]
        INNER JOIN [dbo].[SurveyRequests] AS [request]
            ON [request].[Id] = [survey].[SurveyRequestId]
        INNER JOIN [dbo].[RoadSectionVersions] AS [roadSectionVersion]
            ON [roadSectionVersion].[Id] = [survey].[RoadSectionVersionId]
        WHERE [request].[ProjectId] <> [survey].[ProjectId]
            OR [request].[RoadSectionId] <> [roadSectionVersion].[RoadSectionId]
            OR [request].[SurveyType] <> [survey].[SurveyType]
    )
    BEGIN
        THROW 51005, 'Survey request must match project, road section version, and survey type.', 1;
    END
END

```

