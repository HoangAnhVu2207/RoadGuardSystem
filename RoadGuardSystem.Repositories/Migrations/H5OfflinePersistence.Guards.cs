using Microsoft.EntityFrameworkCore.Migrations;

namespace RoadGuardSystem.cRepositories.Migrations;

public partial class H5OfflinePersistence
{
    private static readonly string[] OfflineTables =
    ["OfflineDeviceRegistrations","OfflineDeviceRevocations","OfflineTaskSnapshots","OfflineOperationBindings",
     "OfflineEncryptedPackages","OfflineHandoverGrants","OfflineHandoverGrantRevocations","OfflineSyncBatches",
     "OfflineOperationAdmissions","OfflineOperationResults","OfflineEvidenceCaptureReferences",
     "OfflinePackageFileReferences","OfflineAdmittedFileReferences","OfflineEncryptedCaptureArtifacts",
     "OfflineOriginTimeVerifications"];

    private static void InstallOfflineGuards(MigrationBuilder migrationBuilder)
    {
        foreach(var table in OfflineTables)
        {
            migrationBuilder.Sql($"""
                CREATE TRIGGER [TR_{table}_Immutable] ON [{table}] AFTER UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS(SELECT 1 FROM deleted) THROW 51400, 'Offline source history is append-only.', 1;
                END
                """);
            var predicate=OfflineScopePredicate(table);
            migrationBuilder.Sql($"""
                CREATE TRIGGER [TR_{table}_Scope] ON [{table}] AFTER INSERT AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS(SELECT 1 FROM inserted i WHERE {predicate})
                        THROW 51401, 'Offline retained source scope does not match.', 1;
                END
                """);
        }
    }

    // These backstops bind retained facts. Current admission authority and cryptographic
    // validation remain production repository duties, not inferred from a historical role.
    private static string OfflineScopePredicate(string table)=>table switch
    {
        "OfflineDeviceRegistrations"=>"i.RoleSnapshot NOT IN (2,4)",
        "OfflineDeviceRevocations"=>"NOT EXISTS(SELECT 1 FROM OfflineDeviceRegistrations d WHERE d.Id=i.DeviceRegistrationId AND d.ProjectId=i.ProjectId AND d.RegisteredAt<=i.RevokedAt)",
        "OfflineTaskSnapshots"=>"""
            NOT EXISTS(SELECT 1 FROM FieldInspectionTasks t JOIN FieldInspectionAssignments a ON a.FieldInspectionTaskId=t.Id
                JOIN OfflineDeviceRegistrations d ON d.Id=i.DeviceRegistrationId
                WHERE t.Id=i.TaskId AND t.ProjectId=i.ProjectId AND a.Id=i.AssignmentId
                    AND a.AssignedToUserId=i.OriginalActorId AND d.ActorId=i.OriginalActorId AND d.ProjectId=i.ProjectId)
            """,
        "OfflineOperationBindings"=>"""
            NOT EXISTS(SELECT 1 FROM OfflineTaskSnapshots s WHERE s.Id=i.SnapshotId AND s.ProjectId=i.ProjectId
                AND s.TaskId=i.TaskId AND s.AssignmentId=i.AssignmentId AND s.OriginalActorId=i.OriginalActorId
                AND s.DeviceRegistrationId=i.SourceDeviceRegistrationId)
            OR (i.RepairResourceId IS NOT NULL AND NOT EXISTS(SELECT 1 FROM RepairFieldTaskBindings b
                WHERE b.ItemId=i.RepairResourceId AND b.TaskId=i.TaskId AND b.AssignmentId=i.AssignmentId
                    AND b.ProjectId=i.ProjectId AND b.CrewId=i.OriginalActorId))
            """,
        "OfflineEncryptedPackages"=>"""
            NOT EXISTS(SELECT 1 FROM OfflineDeviceRegistrations d WHERE d.Id=i.SourceDeviceRegistrationId
                AND d.ProjectId=i.ProjectId AND d.ActorId=i.OriginalActorId)
            """,
        "OfflineHandoverGrants"=>"""
            NOT EXISTS(SELECT 1 FROM OfflineEncryptedPackages p
                JOIN OfflineDeviceRegistrations s ON s.Id=i.SourceDeviceRegistrationId
                JOIN OfflineDeviceRegistrations r ON r.Id=i.RecipientDeviceRegistrationId
                WHERE p.Id=i.PackageId AND p.ProjectId=i.ProjectId AND p.OriginalActorId=i.SourceActorId
                    AND p.SourceDeviceRegistrationId=s.Id AND p.ManifestHash=i.ManifestHash
                    AND s.ProjectId=i.ProjectId AND s.ActorId=i.SourceActorId
                    AND r.ProjectId=i.ProjectId AND r.ActorId=i.RecipientActorId AND r.RoleSnapshot=i.RecipientRole)
            """,
        "OfflineHandoverGrantRevocations"=>"NOT EXISTS(SELECT 1 FROM OfflineHandoverGrants g WHERE g.Id=i.GrantId AND g.ProjectId=i.ProjectId AND g.IssuedAt<=i.RevokedAt)",
        "OfflineSyncBatches"=>"""
            (i.GrantId IS NULL AND NOT EXISTS(SELECT 1 FROM OfflineDeviceRegistrations s
                WHERE s.Id=i.SourceDeviceRegistrationId AND s.ProjectId=i.ProjectId AND s.ActorId=i.CurrentImporterId))
            OR (i.GrantId IS NOT NULL AND NOT EXISTS(SELECT 1 FROM OfflineHandoverGrants g
                WHERE g.Id=i.GrantId AND g.ProjectId=i.ProjectId AND g.PackageId=i.PackageId
                    AND g.SourceDeviceRegistrationId=i.SourceDeviceRegistrationId
                    AND g.RecipientDeviceRegistrationId=i.RecipientDeviceRegistrationId AND g.RecipientActorId=i.CurrentImporterId))
            """,
        "OfflineOperationAdmissions"=>"""
            NOT EXISTS(SELECT 1 FROM OfflineSyncBatches b JOIN OfflineOperationBindings o ON o.Id=i.BindingId
                WHERE b.Id=i.BatchId AND b.ProjectId=i.ProjectId AND o.ProjectId=i.ProjectId
                    AND b.CurrentImporterId=i.CurrentImporterId AND b.SourceDeviceRegistrationId=o.SourceDeviceRegistrationId
                    AND (b.GrantId=i.GrantId OR (b.GrantId IS NULL AND i.GrantId IS NULL)))
            """,
        "OfflineOperationResults"=>"""
            NOT EXISTS(SELECT 1 FROM OfflineOperationAdmissions a JOIN OfflineOperationBindings b ON b.Id=a.BindingId
                WHERE a.Id=i.AdmissionId AND a.ProjectId=i.ProjectId AND a.BatchId=i.BatchId AND b.OriginId=i.OriginId
                    AND (i.EffectId IS NULL OR i.EffectId=b.EffectId))
            OR (i.DurableAck=1 AND NOT EXISTS(SELECT 1 FROM OfflineOperationAdmissions a
                JOIN OfflineOperationBindings b ON b.Id=a.BindingId
                JOIN FieldInspectionOperationOrigins c ON c.ProjectId=b.ProjectId AND c.OriginId=b.OriginId
                WHERE a.Id=i.AdmissionId AND c.Kind=b.Kind AND c.TaskId=b.TaskId AND c.EffectId=i.EffectId
                    AND c.ContentHash=b.CorePayloadHash AND c.OriginalActorId=b.OriginalActorId
                    AND ((b.Kind='FIELD_START' AND EXISTS(SELECT 1 FROM FieldTaskStartOrigins e WHERE e.Id=c.EffectId
                        AND e.OperationOriginId=c.Id AND e.AssignmentId=b.AssignmentId))
                    OR (b.Kind='FIELD_SUBMISSION' AND EXISTS(SELECT 1 FROM FieldInspectionSubmissions e WHERE e.Id=c.EffectId
                        AND e.OperationOriginId=c.Id AND e.AssignmentId=b.AssignmentId))
                    OR (b.Kind='FIELD_ACCEPT' AND EXISTS(SELECT 1 FROM FieldInspectionTaskEvents e WHERE e.Id=c.EffectId
                        AND e.ProjectId=b.ProjectId AND e.TaskId=b.TaskId AND e.AssignmentId=b.AssignmentId
                        AND e.ActorId=b.OriginalActorId AND e.Kind='ACCEPTED'))
                    OR (b.Kind='REPAIR_ASSESSMENT' AND EXISTS(SELECT 1 FROM RepairMeasurementAssessments e WHERE e.Id=c.EffectId
                        AND e.OperationOriginId=c.Id AND e.AssignmentId=b.AssignmentId AND e.ItemId=b.RepairResourceId))
                    OR (b.Kind='REPAIR_EXECUTION_START' AND EXISTS(SELECT 1 FROM RepairExecutionStarts e
                        JOIN RepairFieldTaskBindings f ON f.Id=e.BindingId WHERE e.Id=c.EffectId
                        AND e.OperationOriginId=c.Id AND f.AssignmentId=b.AssignmentId AND e.ItemId=b.RepairResourceId))
                    OR (b.Kind='REPAIR_EXECUTION_FINISH' AND EXISTS(SELECT 1 FROM RepairExecutionFinishes e
                        JOIN RepairFieldTaskBindings f ON f.Id=e.BindingId WHERE e.Id=c.EffectId
                        AND e.OperationOriginId=c.Id AND f.AssignmentId=b.AssignmentId AND e.ItemId=b.RepairResourceId)))))
            """,
        "OfflineEvidenceCaptureReferences"=>"""
            NOT EXISTS(SELECT 1 FROM OfflineOperationAdmissions a JOIN OfflineOperationBindings b ON b.Id=a.BindingId
                WHERE a.Id=i.AdmissionId AND a.ProjectId=i.ProjectId AND b.Id=i.BindingId AND b.TaskId=i.TaskId
                    AND b.OriginalActorId=i.OriginalActorId AND a.CurrentImporterId=i.ActualUploaderId
                    AND (a.GrantId=i.GrantId OR (a.GrantId IS NULL AND i.GrantId IS NULL)))
            """,
        "OfflineAdmittedFileReferences"=>"""
            NOT EXISTS(SELECT 1 FROM OfflineOperationAdmissions a JOIN OfflineOperationBindings b ON b.Id=a.BindingId
                JOIN Files f ON f.Id=i.FileId JOIN FileScopes s ON s.FileId=f.Id
                WHERE a.Id=i.AdmissionId AND a.ProjectId=i.ProjectId
                    AND b.Id=i.BindingId AND b.TaskId=i.TaskId AND b.OriginalActorId=i.OriginalActorId
                    AND a.CurrentImporterId=i.CurrentImporterId AND f.Checksum=i.ContentChecksum
                    AND s.OwnerUserId=i.ActualFileOwnerId
                    AND NOT EXISTS(SELECT f.UploadedByUserId EXCEPT SELECT i.ActualUploadedById)
                    AND ((s.ProjectId=i.ProjectId AND s.TargetId=i.TaskId AND s.Purpose=i.Purpose)
                        OR (i.Purpose='BEFORE' AND EXISTS(SELECT 1 FROM FieldInspectionEvidenceReuseDecisions r
                            WHERE r.ProjectId=i.ProjectId AND r.TaskId=i.TaskId AND r.FileId=f.Id
                                AND r.FileChecksum=i.ContentChecksum AND r.OccurredAt<=i.ReferencedAt)))
                    AND EXISTS(SELECT 1 FROM UploadSessions u WHERE u.FileId=f.Id AND u.Status=4
                        AND u.ExpectedChecksumSha256=i.ContentChecksum))
            """,
        "OfflinePackageFileReferences"=>"NOT EXISTS(SELECT 1 FROM Files f WHERE f.Id=i.FileId AND f.Checksum=i.ContentChecksum)",
        "OfflineEncryptedCaptureArtifacts"=>"""
            NOT EXISTS(SELECT 1 FROM OfflineEncryptedPackages p JOIN FieldInspectionTasks t ON t.Id=i.TaskId
                WHERE p.Id=i.ParentPackageId AND p.ProjectId=i.ProjectId AND t.ProjectId=i.ProjectId
                    AND p.OriginalActorId=i.OriginalActorId AND p.SourceDeviceRegistrationId=i.SourceDeviceRegistrationId)
            """,
        "OfflineOriginTimeVerifications"=>"""
            NOT EXISTS(SELECT 1 FROM OfflineOperationBindings b JOIN FieldInspectionOperationOrigins c ON c.Id=i.CanonicalOriginId
                WHERE b.Id=i.BindingId AND b.ProjectId=i.ProjectId AND b.EffectId=i.TypedEffectId
                    AND c.ProjectId=i.ProjectId AND c.TaskId=b.TaskId AND c.OriginId=b.OriginId
                    AND c.EffectId=b.EffectId AND c.ContentHash=b.CorePayloadHash AND c.OriginalActorId=b.OriginalActorId)
            OR (i.ProofSourceKind='FIELD_START_SERVER_ORIGIN' AND NOT EXISTS(SELECT 1 FROM FieldTaskStartOrigins s
                WHERE s.Id=i.ProofSourceId AND s.Id=i.TypedEffectId AND s.OperationOriginId=i.CanonicalOriginId
                    AND s.ProjectId=i.ProjectId AND s.VerifiedOriginalAt=i.OriginalOccurredAtUtc))
            OR (i.ProofSourceKind='REPAIR_FINISH_SERVER_ORIGIN' AND NOT EXISTS(SELECT 1 FROM RepairExecutionFinishes f
                WHERE f.Id=i.ProofSourceId AND f.Id=i.TypedEffectId AND f.OperationOriginId=i.CanonicalOriginId
                    AND f.ProjectId=i.ProjectId AND f.VerifiedOriginalAt=i.OriginalOccurredAtUtc))
            """,
        _=>throw new InvalidOperationException("Unregistered offline table.")
    };

    private static void RefusePopulatedOfflineDowngrade(MigrationBuilder migrationBuilder)
    {
        var populated=string.Join(" OR ",OfflineTables.Select(table=>$"EXISTS(SELECT 1 FROM [{table}])"));
        migrationBuilder.Sql($"IF {populated} THROW 51490, 'Populated offline history requires explicit data-preserving migration.', 1;");
    }
}
