using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Offline;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Repairs;

namespace RoadGuardSystem.Repositories.Configurations;

// Explicit adoption by the shared schema writer is required. Assembly scanning cannot adopt this draft.
internal static class OfflinePersistenceConfigurations
{
    internal static void Configure(ModelBuilder builder)
    {
        Device(builder.Entity<OfflineDeviceRegistration>());
        DeviceRevocation(builder.Entity<OfflineDeviceRevocation>());
        Snapshot(builder.Entity<OfflineTaskSnapshot>());
        Binding(builder.Entity<OfflineOperationBinding>());
        Package(builder.Entity<OfflineEncryptedPackageRecord>());
        Grant(builder.Entity<OfflineHandoverGrant>());
        GrantRevocation(builder.Entity<OfflineHandoverGrantRevocation>());
        Batch(builder.Entity<OfflineSyncBatch>());
        Admission(builder.Entity<OfflineOperationAdmission>());
        Result(builder.Entity<OfflineOperationResult>());
        Capture(builder.Entity<OfflineEvidenceCaptureReference>());
        PackageFile(builder.Entity<OfflinePackageFileReference>());
        AdmittedFile(builder.Entity<OfflineAdmittedFileReference>());
        Artifact(builder.Entity<OfflineEncryptedCaptureArtifact>());
        TimeVerification(builder.Entity<OfflineOriginTimeVerification>());
    }

    private static void Root<T>(EntityTypeBuilder<T> builder, string table) where T : class
    {
        builder.ToTable(table, configuration =>
        {
            configuration.HasTrigger($"TR_{table}_Immutable");
            configuration.HasTrigger($"TR_{table}_Scope");
        });
        builder.HasKey("Id");
        builder.Property<Guid>("Id").ValueGeneratedNever();
        builder.HasAlternateKey("Id", "ProjectId");
        builder.HasOne<Project>().WithMany().HasForeignKey("ProjectId").OnDelete(DeleteBehavior.Restrict);
    }

    private static void User<T>(EntityTypeBuilder<T> builder, string property) where T : class
        => builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(property).OnDelete(DeleteBehavior.Restrict);

    private static void Scoped<T, TPrincipal>(EntityTypeBuilder<T> builder, string property)
        where T : class where TPrincipal : class
        => builder.HasOne<TPrincipal>().WithMany().HasForeignKey(property, "ProjectId")
            .HasPrincipalKey("Id", "ProjectId").OnDelete(DeleteBehavior.Restrict);

    private static void Task<T>(EntityTypeBuilder<T> builder) where T : class
        => builder.HasOne<FieldInspectionTask>().WithMany().HasForeignKey("TaskId").OnDelete(DeleteBehavior.Restrict);

    private static void Hash<T>(EntityTypeBuilder<T> builder, params string[] properties) where T : class
    {
        foreach (var property in properties)
        {
            builder.Property<string>(property).HasMaxLength(64).IsUnicode(false).IsRequired();
            var table = builder.Metadata.GetTableName()!;
            builder.ToTable(table, configuration => configuration.HasCheckConstraint($"CK_{table}_{property}",
                $"LEN([{property}])=64 AND [{property}] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'"));
        }
    }

    private static void Text<T>(EntityTypeBuilder<T> builder, string property, int length) where T : class
        => builder.Property<string>(property).HasMaxLength(length).IsRequired();

    private static void Json<T>(EntityTypeBuilder<T> builder, params string[] properties) where T : class
    {
        foreach (var property in properties)
        {
            builder.Property<string>(property).HasColumnType("nvarchar(max)").IsRequired();
            var table = builder.Metadata.GetTableName()!;
            builder.ToTable(table, configuration => configuration.HasCheckConstraint($"CK_{table}_{property}", $"ISJSON([{property}])=1"));
        }
    }

    private static void Device(EntityTypeBuilder<OfflineDeviceRegistration> builder)
    {
        Root(builder, "OfflineDeviceRegistrations"); User(builder, "ActorId");
        builder.HasIndex(row => new { row.ProjectId, row.ActorId, row.DeviceId, row.Revision }).IsUnique();
        builder.HasIndex(row => new { row.ProjectId, row.KeyFingerprint });
        builder.Property(row => row.RoleSnapshot).HasConversion<byte>();
        builder.ToTable("OfflineDeviceRegistrations", table => table.HasCheckConstraint("CK_OfflineDeviceRegistrations_Revision", "[Revision]>0"));
        Text(builder, "EncryptionPublicKey", 512); Text(builder, "SigningPublicKey", 512);
        Hash(builder, "KeyFingerprint");
    }

    private static void DeviceRevocation(EntityTypeBuilder<OfflineDeviceRevocation> builder)
    {
        Root(builder, "OfflineDeviceRevocations"); User(builder, "RevokedBy");
        Scoped<OfflineDeviceRevocation, OfflineDeviceRegistration>(builder, "DeviceRegistrationId");
        builder.HasIndex(row => row.DeviceRegistrationId).IsUnique(); Text(builder, "Reason", 2000);
    }

    private static void Snapshot(EntityTypeBuilder<OfflineTaskSnapshot> builder)
    {
        Root(builder, "OfflineTaskSnapshots"); Task(builder); User(builder, "OriginalActorId");
        builder.HasOne<FieldInspectionAssignment>().WithMany().HasForeignKey(row => row.AssignmentId).OnDelete(DeleteBehavior.Restrict);
        Scoped<OfflineTaskSnapshot, OfflineDeviceRegistration>(builder, "DeviceRegistrationId");
        builder.HasIndex(row => new { row.ProjectId, row.TaskId, row.AssignmentId, row.DeviceRegistrationId });
        Text(builder, "TaskVersion", 32); Hash(builder, "AssignmentHash", "ContentHash"); Json(builder, "SnapshotJson");
    }

    private static void Binding(EntityTypeBuilder<OfflineOperationBinding> builder)
    {
        Root(builder, "OfflineOperationBindings"); Task(builder); User(builder, "OriginalActorId");
        builder.HasIndex(row => new { row.ProjectId, row.OriginId }).IsUnique();
        builder.HasIndex(row => row.EffectId).IsUnique();
        builder.HasOne<FieldInspectionAssignment>().WithMany().HasForeignKey(row => row.AssignmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RepairItem>().WithMany().HasForeignKey(row => row.RepairResourceId).OnDelete(DeleteBehavior.Restrict);
        Scoped<OfflineOperationBinding, OfflineDeviceRegistration>(builder, "SourceDeviceRegistrationId");
        Scoped<OfflineOperationBinding, OfflineTaskSnapshot>(builder, "SnapshotId");
        Text(builder, "Kind", 40); Hash(builder, "CorePayloadHash", "EnvelopeHash"); Json(builder, "EnvelopeJson");
        builder.ToTable("OfflineOperationBindings", table => table.HasCheckConstraint("CK_OfflineOperationBindings_RepairScope",
            "([Kind] IN ('FIELD_ACCEPT','FIELD_START','FIELD_SUBMISSION') AND [RepairResourceId] IS NULL) OR ([Kind] IN ('REPAIR_ASSESSMENT','REPAIR_EXECUTION_START','REPAIR_EXECUTION_FINISH') AND [RepairResourceId] IS NOT NULL)"));
    }

    private static void Package(EntityTypeBuilder<OfflineEncryptedPackageRecord> builder)
    {
        Root(builder, "OfflineEncryptedPackages"); User(builder, "OriginalActorId"); User(builder, "RegisteredBy");
        Scoped<OfflineEncryptedPackageRecord, OfflineDeviceRegistration>(builder, "SourceDeviceRegistrationId");
        builder.HasIndex(row => new { row.ProjectId, row.SourceDeviceRegistrationId, row.SourceBatchId });
        Hash(builder, "ManifestHash", "PayloadHash", "PackageFingerprint");
        Json(builder, "CipherPackageJson", "SignedManifestJson");
        Text(builder, "SourceSignature", 512); Text(builder, "RegistrationMode", 32);
    }

    private static void Grant(EntityTypeBuilder<OfflineHandoverGrant> builder)
    {
        Root(builder, "OfflineHandoverGrants"); User(builder, "SourceActorId"); User(builder, "RecipientActorId"); User(builder, "IssuedBy");
        Scoped<OfflineHandoverGrant, OfflineEncryptedPackageRecord>(builder, "PackageId");
        Scoped<OfflineHandoverGrant, OfflineDeviceRegistration>(builder, "SourceDeviceRegistrationId");
        Scoped<OfflineHandoverGrant, OfflineDeviceRegistration>(builder, "RecipientDeviceRegistrationId");
        builder.Property(row => row.RecipientRole).HasConversion<byte>();
        builder.HasIndex(row => new { row.ProjectId, row.PackageId, row.RecipientActorId, row.ExpiresAt });
        Hash(builder, "ManifestHash"); Json(builder, "ScopeJson"); Text(builder, "Reason", 2000);
        builder.ToTable("OfflineHandoverGrants", table => table.HasCheckConstraint("CK_OfflineHandoverGrants_Expiry", "[ExpiresAt]=DATEADD(hour,24,[IssuedAt])"));
    }

    private static void GrantRevocation(EntityTypeBuilder<OfflineHandoverGrantRevocation> builder)
    {
        Root(builder, "OfflineHandoverGrantRevocations"); User(builder, "RevokedBy");
        Scoped<OfflineHandoverGrantRevocation, OfflineHandoverGrant>(builder, "GrantId");
        builder.HasIndex(row => row.GrantId).IsUnique(); Text(builder, "Reason", 2000);
    }

    private static void Batch(EntityTypeBuilder<OfflineSyncBatch> builder)
    {
        Root(builder, "OfflineSyncBatches"); User(builder, "CurrentImporterId");
        Scoped<OfflineSyncBatch, OfflineDeviceRegistration>(builder, "SourceDeviceRegistrationId");
        Scoped<OfflineSyncBatch, OfflineDeviceRegistration>(builder, "RecipientDeviceRegistrationId");
        Scoped<OfflineSyncBatch, OfflineEncryptedPackageRecord>(builder, "PackageId");
        Scoped<OfflineSyncBatch, OfflineHandoverGrant>(builder, "GrantId");
        builder.HasIndex(row => new { row.ProjectId, row.SourceDeviceRegistrationId, row.SourceBatchId, row.CurrentImporterId, row.ContentHash }).IsUnique();
        Text(builder, "SourceSignature", 512); builder.Property(row => row.RecipientSignature).HasMaxLength(512);
        Hash(builder, "ContentHash"); Json(builder, "SignedDescriptorJson");
        builder.Property(row => row.AttachedPayloadJson).HasColumnType("nvarchar(max)");
        builder.Property(row => row.AttachedPayloadHash).HasMaxLength(64).IsUnicode(false);
        builder.ToTable("OfflineSyncBatches", table => table.HasCheckConstraint("CK_OfflineSyncBatches_AttachedPayload",
            "([AttachedPayloadJson] IS NULL AND [AttachedPayloadHash] IS NULL) OR ([AttachedPayloadJson] IS NOT NULL AND [AttachedPayloadHash] IS NOT NULL AND ISJSON([AttachedPayloadJson])=1 AND LEN([AttachedPayloadHash])=64 AND [AttachedPayloadHash] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%' AND DATALENGTH(CONVERT(varchar(max),[AttachedPayloadJson] COLLATE Latin1_General_100_BIN2_UTF8))<=16777216)"));
        builder.ToTable("OfflineSyncBatches", table => table.HasCheckConstraint("CK_OfflineSyncBatches_Recipient",
            "([PackageId] IS NULL AND [GrantId] IS NULL AND [RecipientDeviceRegistrationId] IS NULL AND [RecipientSignature] IS NULL) OR ([PackageId] IS NOT NULL AND [GrantId] IS NOT NULL AND [RecipientDeviceRegistrationId] IS NOT NULL AND [RecipientSignature] IS NOT NULL)"));
    }

    private static void Admission(EntityTypeBuilder<OfflineOperationAdmission> builder)
    {
        Root(builder, "OfflineOperationAdmissions"); User(builder, "CurrentImporterId");
        Scoped<OfflineOperationAdmission, OfflineSyncBatch>(builder, "BatchId");
        Scoped<OfflineOperationAdmission, OfflineOperationBinding>(builder, "BindingId");
        Scoped<OfflineOperationAdmission, OfflineHandoverGrant>(builder, "GrantId");
        builder.HasIndex(row => new { row.BatchId, row.BindingId }).IsUnique();
        builder.Property(row => row.ImporterRole).HasConversion<byte>();
        Text(builder, "AdmissionMode", 32); Json(builder, "ScopeFactsJson");
        builder.ToTable("OfflineOperationAdmissions", table => table.HasCheckConstraint("CK_OfflineOperationAdmissions_Mode",
            "([AdmissionMode]='DIRECT_SYNC' AND [GrantId] IS NULL) OR ([AdmissionMode]='HANDOVER' AND [GrantId] IS NOT NULL)"));
    }

    private static void Result(EntityTypeBuilder<OfflineOperationResult> builder)
    {
        Root(builder, "OfflineOperationResults");
        Scoped<OfflineOperationResult, OfflineSyncBatch>(builder, "BatchId");
        Scoped<OfflineOperationResult, OfflineOperationAdmission>(builder, "AdmissionId");
        builder.HasIndex(row => new { row.ProjectId, row.OriginId, row.RecordedAt });
        Text(builder, "State", 32); Text(builder, "TimeProvenance", 32); Text(builder, "SyncLateness", 24);
        builder.Property(row => row.Code).HasMaxLength(100); builder.Property(row => row.ResourceVersion).HasMaxLength(32);
        Json(builder, "OutcomeJson");
        builder.ToTable("OfflineOperationResults", table => table.HasCheckConstraint("CK_OfflineOperationResults_Acknowledgment",
            "([State] IN ('COMMITTED','REPLAYED') AND [DurableAck]=1 AND [EffectId] IS NOT NULL) OR ([State] IN ('CONFLICT','PENDING_DEPENDENCY','REJECTED','STALE_SNAPSHOT') AND [DurableAck]=0)"));
        builder.ToTable("OfflineOperationResults", table => table.HasCheckConstraint("CK_OfflineOperationResults_Time",
            "[TimeProvenance] IN ('UNCERTAIN','VERIFIED_ORIGINAL') AND ([VerifiedFinishedAt] IS NOT NULL OR [SyncLateness]='UNKNOWN')"));
    }

    private static void Capture(EntityTypeBuilder<OfflineEvidenceCaptureReference> builder)
    {
        Root(builder, "OfflineEvidenceCaptureReferences"); Task(builder); User(builder, "OriginalActorId"); User(builder, "ActualUploaderId");
        Scoped<OfflineEvidenceCaptureReference, OfflineOperationAdmission>(builder, "AdmissionId");
        Scoped<OfflineEvidenceCaptureReference, OfflineOperationBinding>(builder, "BindingId");
        Scoped<OfflineEvidenceCaptureReference, OfflineHandoverGrant>(builder, "GrantId");
        builder.HasOne<StoredFile>().WithMany().HasForeignKey(row => row.FileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UploadSession>().WithMany().HasForeignKey(row => row.UploadSessionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(row => new { row.AdmissionId, row.CaptureOriginId }).IsUnique();
        builder.HasIndex(row => row.FileId).IsUnique(); builder.HasIndex(row => row.UploadSessionId).IsUnique();
        Text(builder, "Purpose", 40); Text(builder, "MediaType", 120); Hash(builder, "Checksum"); Json(builder, "CaptureFactsJson");
    }

    private static void PackageFile(EntityTypeBuilder<OfflinePackageFileReference> builder)
    {
        Root(builder, "OfflinePackageFileReferences");
        Scoped<OfflinePackageFileReference, OfflineEncryptedPackageRecord>(builder, "PackageId");
        builder.HasOne<StoredFile>().WithMany().HasForeignKey(row => row.FileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(row => new { row.PackageId, row.FileId }).IsUnique();
        Hash(builder, "ContentChecksum"); Json(builder, "CaptureFactsJson");
    }

    private static void AdmittedFile(EntityTypeBuilder<OfflineAdmittedFileReference> builder)
    {
        Root(builder, "OfflineAdmittedFileReferences"); Task(builder); User(builder, "OriginalActorId");
        User(builder, "CurrentImporterId"); User(builder, "ActualFileOwnerId"); User(builder, "ActualUploadedById");
        Scoped<OfflineAdmittedFileReference, OfflineOperationAdmission>(builder, "AdmissionId");
        Scoped<OfflineAdmittedFileReference, OfflineOperationBinding>(builder, "BindingId");
        builder.HasOne<StoredFile>().WithMany().HasForeignKey(row => row.FileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(row => new { row.AdmissionId, row.CaptureOriginId }).IsUnique();
        Text(builder, "Purpose", 40); Hash(builder, "ContentChecksum"); Json(builder, "CaptureFactsJson");
    }

    private static void Artifact(EntityTypeBuilder<OfflineEncryptedCaptureArtifact> builder)
    {
        Root(builder, "OfflineEncryptedCaptureArtifacts"); Task(builder); User(builder, "OriginalActorId"); User(builder, "RegisteredBy");
        Scoped<OfflineEncryptedCaptureArtifact, OfflineEncryptedPackageRecord>(builder, "ParentPackageId");
        Scoped<OfflineEncryptedCaptureArtifact, OfflineDeviceRegistration>(builder, "SourceDeviceRegistrationId");
        builder.HasIndex(row => new { row.ParentPackageId, row.CaptureOriginId, row.ManifestHash, row.ChunkIndex }).IsUnique();
        Hash(builder, "PlaintextChecksum", "EnvelopeFingerprint", "ManifestHash");
        Json(builder, "SignedManifestJson", "CipherEnvelopeJson"); Text(builder, "ManifestSignature", 512);
        builder.ToTable("OfflineEncryptedCaptureArtifacts", table => table.HasCheckConstraint("CK_OfflineEncryptedCaptureArtifacts_Chunk", "[ChunkIndex]>=0 AND [ChunkIndex]<32 AND [ChunkOffset]>=0 AND [ChunkLength]>0 AND [ChunkLength]<=16777216"));
    }

    private static void TimeVerification(EntityTypeBuilder<OfflineOriginTimeVerification> builder)
    {
        Root(builder, "OfflineOriginTimeVerifications"); User(builder, "VerifiedBy");
        Scoped<OfflineOriginTimeVerification, OfflineOperationBinding>(builder, "BindingId");
        builder.HasOne<FieldInspectionOperationOrigin>().WithMany().HasForeignKey(row => row.CanonicalOriginId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(row => new { row.BindingId, row.ProofSourceKind, row.ProofSourceId }).IsUnique();
        Text(builder, "ProofSourceKind", 40);
        builder.ToTable("OfflineOriginTimeVerifications", table => table.HasCheckConstraint("CK_OfflineOriginTimeVerifications_Source",
            "[ProofSourceKind] IN ('FIELD_START_SERVER_ORIGIN','REPAIR_FINISH_SERVER_ORIGIN') AND [OriginalOccurredAtUtc]<=[RecordedAtUtc]"));
    }
}
