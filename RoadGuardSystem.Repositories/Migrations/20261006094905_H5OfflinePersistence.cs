using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class H5OfflinePersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OfflineDeviceRegistrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Revision = table.Column<int>(type: "int", nullable: false),
                    RoleSnapshot = table.Column<byte>(type: "tinyint", nullable: false),
                    EncryptionPublicKey = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    SigningPublicKey = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    KeyFingerprint = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    RegisteredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfflineDeviceRegistrations", x => x.Id);
                    table.UniqueConstraint("AK_OfflineDeviceRegistrations_Id_ProjectId", x => new { x.Id, x.ProjectId });
                    table.CheckConstraint("CK_OfflineDeviceRegistrations_KeyFingerprint", "LEN([KeyFingerprint])=64 AND [KeyFingerprint] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'");
                    table.CheckConstraint("CK_OfflineDeviceRegistrations_Revision", "[Revision]>0");
                    table.ForeignKey(
                        name: "FK_OfflineDeviceRegistrations_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineDeviceRegistrations_Users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OfflineDeviceRevocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceRegistrationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RevokedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfflineDeviceRevocations", x => x.Id);
                    table.UniqueConstraint("AK_OfflineDeviceRevocations_Id_ProjectId", x => new { x.Id, x.ProjectId });
                    table.ForeignKey(
                        name: "FK_OfflineDeviceRevocations_OfflineDeviceRegistrations_DeviceRegistrationId_ProjectId",
                        columns: x => new { x.DeviceRegistrationId, x.ProjectId },
                        principalTable: "OfflineDeviceRegistrations",
                        principalColumns: new[] { "Id", "ProjectId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineDeviceRevocations_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineDeviceRevocations_Users_RevokedBy",
                        column: x => x.RevokedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OfflineEncryptedPackages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceDeviceRegistrationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CipherPackageJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SignedManifestJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourceSignature = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    ManifestHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    PayloadHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    PackageFingerprint = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    RegisteredBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RegisteredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RegistrationMode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfflineEncryptedPackages", x => x.Id);
                    table.UniqueConstraint("AK_OfflineEncryptedPackages_Id_ProjectId", x => new { x.Id, x.ProjectId });
                    table.CheckConstraint("CK_OfflineEncryptedPackages_CipherPackageJson", "ISJSON([CipherPackageJson])=1");
                    table.CheckConstraint("CK_OfflineEncryptedPackages_ManifestHash", "LEN([ManifestHash])=64 AND [ManifestHash] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'");
                    table.CheckConstraint("CK_OfflineEncryptedPackages_PackageFingerprint", "LEN([PackageFingerprint])=64 AND [PackageFingerprint] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'");
                    table.CheckConstraint("CK_OfflineEncryptedPackages_PayloadHash", "LEN([PayloadHash])=64 AND [PayloadHash] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'");
                    table.CheckConstraint("CK_OfflineEncryptedPackages_SignedManifestJson", "ISJSON([SignedManifestJson])=1");
                    table.ForeignKey(
                        name: "FK_OfflineEncryptedPackages_OfflineDeviceRegistrations_SourceDeviceRegistrationId_ProjectId",
                        columns: x => new { x.SourceDeviceRegistrationId, x.ProjectId },
                        principalTable: "OfflineDeviceRegistrations",
                        principalColumns: new[] { "Id", "ProjectId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineEncryptedPackages_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineEncryptedPackages_Users_OriginalActorId",
                        column: x => x.OriginalActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineEncryptedPackages_Users_RegisteredBy",
                        column: x => x.RegisteredBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OfflineTaskSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceRegistrationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskVersion = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    AssignmentHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    DownloadedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfflineTaskSnapshots", x => x.Id);
                    table.UniqueConstraint("AK_OfflineTaskSnapshots_Id_ProjectId", x => new { x.Id, x.ProjectId });
                    table.CheckConstraint("CK_OfflineTaskSnapshots_AssignmentHash", "LEN([AssignmentHash])=64 AND [AssignmentHash] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'");
                    table.CheckConstraint("CK_OfflineTaskSnapshots_ContentHash", "LEN([ContentHash])=64 AND [ContentHash] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'");
                    table.CheckConstraint("CK_OfflineTaskSnapshots_SnapshotJson", "ISJSON([SnapshotJson])=1");
                    table.ForeignKey(
                        name: "FK_OfflineTaskSnapshots_FieldInspectionAssignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "FieldInspectionAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineTaskSnapshots_FieldInspectionTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "FieldInspectionTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineTaskSnapshots_OfflineDeviceRegistrations_DeviceRegistrationId_ProjectId",
                        columns: x => new { x.DeviceRegistrationId, x.ProjectId },
                        principalTable: "OfflineDeviceRegistrations",
                        principalColumns: new[] { "Id", "ProjectId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineTaskSnapshots_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineTaskSnapshots_Users_OriginalActorId",
                        column: x => x.OriginalActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OfflineEncryptedCaptureArtifacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentPackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CaptureOriginId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceDeviceRegistrationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChunkIndex = table.Column<int>(type: "int", nullable: false),
                    ChunkOffset = table.Column<long>(type: "bigint", nullable: false),
                    ChunkLength = table.Column<int>(type: "int", nullable: false),
                    PlaintextChecksum = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    EnvelopeFingerprint = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    ManifestHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    SignedManifestJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ManifestSignature = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    CipherEnvelopeJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RegisteredBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RegisteredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfflineEncryptedCaptureArtifacts", x => x.Id);
                    table.UniqueConstraint("AK_OfflineEncryptedCaptureArtifacts_Id_ProjectId", x => new { x.Id, x.ProjectId });
                    table.CheckConstraint("CK_OfflineEncryptedCaptureArtifacts_Chunk", "[ChunkIndex]>=0 AND [ChunkIndex]<32 AND [ChunkOffset]>=0 AND [ChunkLength]>0 AND [ChunkLength]<=16777216");
                    table.CheckConstraint("CK_OfflineEncryptedCaptureArtifacts_CipherEnvelopeJson", "ISJSON([CipherEnvelopeJson])=1");
                    table.CheckConstraint("CK_OfflineEncryptedCaptureArtifacts_EnvelopeFingerprint", "LEN([EnvelopeFingerprint])=64 AND [EnvelopeFingerprint] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'");
                    table.CheckConstraint("CK_OfflineEncryptedCaptureArtifacts_ManifestHash", "LEN([ManifestHash])=64 AND [ManifestHash] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'");
                    table.CheckConstraint("CK_OfflineEncryptedCaptureArtifacts_PlaintextChecksum", "LEN([PlaintextChecksum])=64 AND [PlaintextChecksum] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'");
                    table.CheckConstraint("CK_OfflineEncryptedCaptureArtifacts_SignedManifestJson", "ISJSON([SignedManifestJson])=1");
                    table.ForeignKey(
                        name: "FK_OfflineEncryptedCaptureArtifacts_FieldInspectionTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "FieldInspectionTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineEncryptedCaptureArtifacts_OfflineDeviceRegistrations_SourceDeviceRegistrationId_ProjectId",
                        columns: x => new { x.SourceDeviceRegistrationId, x.ProjectId },
                        principalTable: "OfflineDeviceRegistrations",
                        principalColumns: new[] { "Id", "ProjectId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineEncryptedCaptureArtifacts_OfflineEncryptedPackages_ParentPackageId_ProjectId",
                        columns: x => new { x.ParentPackageId, x.ProjectId },
                        principalTable: "OfflineEncryptedPackages",
                        principalColumns: new[] { "Id", "ProjectId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineEncryptedCaptureArtifacts_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineEncryptedCaptureArtifacts_Users_OriginalActorId",
                        column: x => x.OriginalActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineEncryptedCaptureArtifacts_Users_RegisteredBy",
                        column: x => x.RegisteredBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OfflineHandoverGrants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceDeviceRegistrationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecipientActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecipientDeviceRegistrationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecipientRole = table.Column<byte>(type: "tinyint", nullable: false),
                    IssuedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IssuedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ManifestHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    ScopeJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfflineHandoverGrants", x => x.Id);
                    table.UniqueConstraint("AK_OfflineHandoverGrants_Id_ProjectId", x => new { x.Id, x.ProjectId });
                    table.CheckConstraint("CK_OfflineHandoverGrants_Expiry", "[ExpiresAt]=DATEADD(hour,24,[IssuedAt])");
                    table.CheckConstraint("CK_OfflineHandoverGrants_ManifestHash", "LEN([ManifestHash])=64 AND [ManifestHash] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'");
                    table.CheckConstraint("CK_OfflineHandoverGrants_ScopeJson", "ISJSON([ScopeJson])=1");
                    table.ForeignKey(
                        name: "FK_OfflineHandoverGrants_OfflineDeviceRegistrations_RecipientDeviceRegistrationId_ProjectId",
                        columns: x => new { x.RecipientDeviceRegistrationId, x.ProjectId },
                        principalTable: "OfflineDeviceRegistrations",
                        principalColumns: new[] { "Id", "ProjectId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineHandoverGrants_OfflineDeviceRegistrations_SourceDeviceRegistrationId_ProjectId",
                        columns: x => new { x.SourceDeviceRegistrationId, x.ProjectId },
                        principalTable: "OfflineDeviceRegistrations",
                        principalColumns: new[] { "Id", "ProjectId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineHandoverGrants_OfflineEncryptedPackages_PackageId_ProjectId",
                        columns: x => new { x.PackageId, x.ProjectId },
                        principalTable: "OfflineEncryptedPackages",
                        principalColumns: new[] { "Id", "ProjectId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineHandoverGrants_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineHandoverGrants_Users_IssuedBy",
                        column: x => x.IssuedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineHandoverGrants_Users_RecipientActorId",
                        column: x => x.RecipientActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineHandoverGrants_Users_SourceActorId",
                        column: x => x.SourceActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OfflinePackageFileReferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContentChecksum = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    CaptureFactsJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfflinePackageFileReferences", x => x.Id);
                    table.UniqueConstraint("AK_OfflinePackageFileReferences_Id_ProjectId", x => new { x.Id, x.ProjectId });
                    table.CheckConstraint("CK_OfflinePackageFileReferences_CaptureFactsJson", "ISJSON([CaptureFactsJson])=1");
                    table.CheckConstraint("CK_OfflinePackageFileReferences_ContentChecksum", "LEN([ContentChecksum])=64 AND [ContentChecksum] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'");
                    table.ForeignKey(
                        name: "FK_OfflinePackageFileReferences_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflinePackageFileReferences_OfflineEncryptedPackages_PackageId_ProjectId",
                        columns: x => new { x.PackageId, x.ProjectId },
                        principalTable: "OfflineEncryptedPackages",
                        principalColumns: new[] { "Id", "ProjectId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflinePackageFileReferences_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OfflineOperationBindings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EffectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CorePayloadHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    EnvelopeHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    OriginalActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceDeviceRegistrationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RepairResourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EnvelopeJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FirstServerReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfflineOperationBindings", x => x.Id);
                    table.UniqueConstraint("AK_OfflineOperationBindings_Id_ProjectId", x => new { x.Id, x.ProjectId });
                    table.CheckConstraint("CK_OfflineOperationBindings_CorePayloadHash", "LEN([CorePayloadHash])=64 AND [CorePayloadHash] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'");
                    table.CheckConstraint("CK_OfflineOperationBindings_EnvelopeHash", "LEN([EnvelopeHash])=64 AND [EnvelopeHash] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'");
                    table.CheckConstraint("CK_OfflineOperationBindings_EnvelopeJson", "ISJSON([EnvelopeJson])=1");
                    table.CheckConstraint("CK_OfflineOperationBindings_RepairScope", "([Kind] IN ('FIELD_ACCEPT','FIELD_START','FIELD_SUBMISSION') AND [RepairResourceId] IS NULL) OR ([Kind] IN ('REPAIR_ASSESSMENT','REPAIR_EXECUTION_START','REPAIR_EXECUTION_FINISH') AND [RepairResourceId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_OfflineOperationBindings_FieldInspectionAssignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "FieldInspectionAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineOperationBindings_FieldInspectionTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "FieldInspectionTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineOperationBindings_OfflineDeviceRegistrations_SourceDeviceRegistrationId_ProjectId",
                        columns: x => new { x.SourceDeviceRegistrationId, x.ProjectId },
                        principalTable: "OfflineDeviceRegistrations",
                        principalColumns: new[] { "Id", "ProjectId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineOperationBindings_OfflineTaskSnapshots_SnapshotId_ProjectId",
                        columns: x => new { x.SnapshotId, x.ProjectId },
                        principalTable: "OfflineTaskSnapshots",
                        principalColumns: new[] { "Id", "ProjectId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineOperationBindings_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineOperationBindings_RepairItems_RepairResourceId",
                        column: x => x.RepairResourceId,
                        principalTable: "RepairItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineOperationBindings_Users_OriginalActorId",
                        column: x => x.OriginalActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OfflineHandoverGrantRevocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GrantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RevokedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfflineHandoverGrantRevocations", x => x.Id);
                    table.UniqueConstraint("AK_OfflineHandoverGrantRevocations_Id_ProjectId", x => new { x.Id, x.ProjectId });
                    table.ForeignKey(
                        name: "FK_OfflineHandoverGrantRevocations_OfflineHandoverGrants_GrantId_ProjectId",
                        columns: x => new { x.GrantId, x.ProjectId },
                        principalTable: "OfflineHandoverGrants",
                        principalColumns: new[] { "Id", "ProjectId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineHandoverGrantRevocations_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineHandoverGrantRevocations_Users_RevokedBy",
                        column: x => x.RevokedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OfflineSyncBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceDeviceRegistrationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentImporterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GrantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RecipientDeviceRegistrationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RecipientSignature = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    SignedDescriptorJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourceSignature = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    ContentHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfflineSyncBatches", x => x.Id);
                    table.UniqueConstraint("AK_OfflineSyncBatches_Id_ProjectId", x => new { x.Id, x.ProjectId });
                    table.CheckConstraint("CK_OfflineSyncBatches_ContentHash", "LEN([ContentHash])=64 AND [ContentHash] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'");
                    table.CheckConstraint("CK_OfflineSyncBatches_Recipient", "([PackageId] IS NULL AND [GrantId] IS NULL AND [RecipientDeviceRegistrationId] IS NULL AND [RecipientSignature] IS NULL) OR ([PackageId] IS NOT NULL AND [GrantId] IS NOT NULL AND [RecipientDeviceRegistrationId] IS NOT NULL AND [RecipientSignature] IS NOT NULL)");
                    table.CheckConstraint("CK_OfflineSyncBatches_SignedDescriptorJson", "ISJSON([SignedDescriptorJson])=1");
                    table.ForeignKey(
                        name: "FK_OfflineSyncBatches_OfflineDeviceRegistrations_RecipientDeviceRegistrationId_ProjectId",
                        columns: x => new { x.RecipientDeviceRegistrationId, x.ProjectId },
                        principalTable: "OfflineDeviceRegistrations",
                        principalColumns: new[] { "Id", "ProjectId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineSyncBatches_OfflineDeviceRegistrations_SourceDeviceRegistrationId_ProjectId",
                        columns: x => new { x.SourceDeviceRegistrationId, x.ProjectId },
                        principalTable: "OfflineDeviceRegistrations",
                        principalColumns: new[] { "Id", "ProjectId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineSyncBatches_OfflineEncryptedPackages_PackageId_ProjectId",
                        columns: x => new { x.PackageId, x.ProjectId },
                        principalTable: "OfflineEncryptedPackages",
                        principalColumns: new[] { "Id", "ProjectId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineSyncBatches_OfflineHandoverGrants_GrantId_ProjectId",
                        columns: x => new { x.GrantId, x.ProjectId },
                        principalTable: "OfflineHandoverGrants",
                        principalColumns: new[] { "Id", "ProjectId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineSyncBatches_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineSyncBatches_Users_CurrentImporterId",
                        column: x => x.CurrentImporterId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OfflineOriginTimeVerifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BindingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CanonicalOriginId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TypedEffectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProofSourceKind = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ProofSourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalOccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    VerifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecordedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfflineOriginTimeVerifications", x => x.Id);
                    table.UniqueConstraint("AK_OfflineOriginTimeVerifications_Id_ProjectId", x => new { x.Id, x.ProjectId });
                    table.CheckConstraint("CK_OfflineOriginTimeVerifications_Source", "[ProofSourceKind] IN ('FIELD_START_SERVER_ORIGIN','REPAIR_FINISH_SERVER_ORIGIN') AND [OriginalOccurredAtUtc]<=[RecordedAtUtc]");
                    table.ForeignKey(
                        name: "FK_OfflineOriginTimeVerifications_FieldInspectionOperationOrigins_CanonicalOriginId",
                        column: x => x.CanonicalOriginId,
                        principalTable: "FieldInspectionOperationOrigins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineOriginTimeVerifications_OfflineOperationBindings_BindingId_ProjectId",
                        columns: x => new { x.BindingId, x.ProjectId },
                        principalTable: "OfflineOperationBindings",
                        principalColumns: new[] { "Id", "ProjectId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineOriginTimeVerifications_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineOriginTimeVerifications_Users_VerifiedBy",
                        column: x => x.VerifiedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OfflineOperationAdmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BindingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentImporterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImporterRole = table.Column<byte>(type: "tinyint", nullable: false),
                    GrantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AdmissionMode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ScopeFactsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AdmittedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfflineOperationAdmissions", x => x.Id);
                    table.UniqueConstraint("AK_OfflineOperationAdmissions_Id_ProjectId", x => new { x.Id, x.ProjectId });
                    table.CheckConstraint("CK_OfflineOperationAdmissions_Mode", "([AdmissionMode]='DIRECT_SYNC' AND [GrantId] IS NULL) OR ([AdmissionMode]='HANDOVER' AND [GrantId] IS NOT NULL)");
                    table.CheckConstraint("CK_OfflineOperationAdmissions_ScopeFactsJson", "ISJSON([ScopeFactsJson])=1");
                    table.ForeignKey(
                        name: "FK_OfflineOperationAdmissions_OfflineHandoverGrants_GrantId_ProjectId",
                        columns: x => new { x.GrantId, x.ProjectId },
                        principalTable: "OfflineHandoverGrants",
                        principalColumns: new[] { "Id", "ProjectId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineOperationAdmissions_OfflineOperationBindings_BindingId_ProjectId",
                        columns: x => new { x.BindingId, x.ProjectId },
                        principalTable: "OfflineOperationBindings",
                        principalColumns: new[] { "Id", "ProjectId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineOperationAdmissions_OfflineSyncBatches_BatchId_ProjectId",
                        columns: x => new { x.BatchId, x.ProjectId },
                        principalTable: "OfflineSyncBatches",
                        principalColumns: new[] { "Id", "ProjectId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineOperationAdmissions_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineOperationAdmissions_Users_CurrentImporterId",
                        column: x => x.CurrentImporterId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OfflineAdmittedFileReferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BindingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CaptureOriginId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentImporterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActualFileOwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActualUploadedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Purpose = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ContentChecksum = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    CaptureFactsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReferencedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfflineAdmittedFileReferences", x => x.Id);
                    table.UniqueConstraint("AK_OfflineAdmittedFileReferences_Id_ProjectId", x => new { x.Id, x.ProjectId });
                    table.CheckConstraint("CK_OfflineAdmittedFileReferences_CaptureFactsJson", "ISJSON([CaptureFactsJson])=1");
                    table.CheckConstraint("CK_OfflineAdmittedFileReferences_ContentChecksum", "LEN([ContentChecksum])=64 AND [ContentChecksum] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'");
                    table.ForeignKey(
                        name: "FK_OfflineAdmittedFileReferences_FieldInspectionTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "FieldInspectionTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineAdmittedFileReferences_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineAdmittedFileReferences_OfflineOperationAdmissions_AdmissionId_ProjectId",
                        columns: x => new { x.AdmissionId, x.ProjectId },
                        principalTable: "OfflineOperationAdmissions",
                        principalColumns: new[] { "Id", "ProjectId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineAdmittedFileReferences_OfflineOperationBindings_BindingId_ProjectId",
                        columns: x => new { x.BindingId, x.ProjectId },
                        principalTable: "OfflineOperationBindings",
                        principalColumns: new[] { "Id", "ProjectId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineAdmittedFileReferences_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineAdmittedFileReferences_Users_ActualFileOwnerId",
                        column: x => x.ActualFileOwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineAdmittedFileReferences_Users_ActualUploadedById",
                        column: x => x.ActualUploadedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineAdmittedFileReferences_Users_CurrentImporterId",
                        column: x => x.CurrentImporterId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineAdmittedFileReferences_Users_OriginalActorId",
                        column: x => x.OriginalActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OfflineEvidenceCaptureReferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BindingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CaptureOriginId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActualUploaderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GrantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UploadSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Checksum = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    MediaType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    DeclaredCapturedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CaptureFactsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AdmittedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfflineEvidenceCaptureReferences", x => x.Id);
                    table.UniqueConstraint("AK_OfflineEvidenceCaptureReferences_Id_ProjectId", x => new { x.Id, x.ProjectId });
                    table.CheckConstraint("CK_OfflineEvidenceCaptureReferences_CaptureFactsJson", "ISJSON([CaptureFactsJson])=1");
                    table.CheckConstraint("CK_OfflineEvidenceCaptureReferences_Checksum", "LEN([Checksum])=64 AND [Checksum] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'");
                    table.ForeignKey(
                        name: "FK_OfflineEvidenceCaptureReferences_FieldInspectionTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "FieldInspectionTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineEvidenceCaptureReferences_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineEvidenceCaptureReferences_OfflineHandoverGrants_GrantId_ProjectId",
                        columns: x => new { x.GrantId, x.ProjectId },
                        principalTable: "OfflineHandoverGrants",
                        principalColumns: new[] { "Id", "ProjectId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineEvidenceCaptureReferences_OfflineOperationAdmissions_AdmissionId_ProjectId",
                        columns: x => new { x.AdmissionId, x.ProjectId },
                        principalTable: "OfflineOperationAdmissions",
                        principalColumns: new[] { "Id", "ProjectId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineEvidenceCaptureReferences_OfflineOperationBindings_BindingId_ProjectId",
                        columns: x => new { x.BindingId, x.ProjectId },
                        principalTable: "OfflineOperationBindings",
                        principalColumns: new[] { "Id", "ProjectId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineEvidenceCaptureReferences_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineEvidenceCaptureReferences_UploadSessions_UploadSessionId",
                        column: x => x.UploadSessionId,
                        principalTable: "UploadSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineEvidenceCaptureReferences_Users_ActualUploaderId",
                        column: x => x.ActualUploaderId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineEvidenceCaptureReferences_Users_OriginalActorId",
                        column: x => x.OriginalActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OfflineOperationResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EffectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    State = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DurableAck = table.Column<bool>(type: "bit", nullable: false),
                    TimeProvenance = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    SyncLateness = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    ClaimedFinishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    VerifiedFinishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    OutcomeJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResourceVersion = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    RecordedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfflineOperationResults", x => x.Id);
                    table.UniqueConstraint("AK_OfflineOperationResults_Id_ProjectId", x => new { x.Id, x.ProjectId });
                    table.CheckConstraint("CK_OfflineOperationResults_Acknowledgment", "([State] IN ('COMMITTED','REPLAYED') AND [DurableAck]=1 AND [EffectId] IS NOT NULL) OR ([State] IN ('CONFLICT','PENDING_DEPENDENCY','REJECTED','STALE_SNAPSHOT') AND [DurableAck]=0)");
                    table.CheckConstraint("CK_OfflineOperationResults_OutcomeJson", "ISJSON([OutcomeJson])=1");
                    table.CheckConstraint("CK_OfflineOperationResults_Time", "[TimeProvenance] IN ('UNCERTAIN','VERIFIED_ORIGINAL') AND ([VerifiedFinishedAt] IS NOT NULL OR [SyncLateness]='UNKNOWN')");
                    table.ForeignKey(
                        name: "FK_OfflineOperationResults_OfflineOperationAdmissions_AdmissionId_ProjectId",
                        columns: x => new { x.AdmissionId, x.ProjectId },
                        principalTable: "OfflineOperationAdmissions",
                        principalColumns: new[] { "Id", "ProjectId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineOperationResults_OfflineSyncBatches_BatchId_ProjectId",
                        columns: x => new { x.BatchId, x.ProjectId },
                        principalTable: "OfflineSyncBatches",
                        principalColumns: new[] { "Id", "ProjectId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfflineOperationResults_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OfflineAdmittedFileReferences_ActualFileOwnerId",
                table: "OfflineAdmittedFileReferences",
                column: "ActualFileOwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_OfflineAdmittedFileReferences_ActualUploadedById",
                table: "OfflineAdmittedFileReferences",
                column: "ActualUploadedById");

            migrationBuilder.CreateIndex(
                name: "IX_OfflineAdmittedFileReferences_AdmissionId_CaptureOriginId",
                table: "OfflineAdmittedFileReferences",
                columns: new[] { "AdmissionId", "CaptureOriginId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OfflineAdmittedFileReferences_AdmissionId_ProjectId",
                table: "OfflineAdmittedFileReferences",
                columns: new[] { "AdmissionId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_OfflineAdmittedFileReferences_BindingId_ProjectId",
                table: "OfflineAdmittedFileReferences",
                columns: new[] { "BindingId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_OfflineAdmittedFileReferences_CurrentImporterId",
                table: "OfflineAdmittedFileReferences",
                column: "CurrentImporterId");

            migrationBuilder.CreateIndex(
                name: "IX_OfflineAdmittedFileReferences_FileId",
                table: "OfflineAdmittedFileReferences",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_OfflineAdmittedFileReferences_OriginalActorId",
                table: "OfflineAdmittedFileReferences",
                column: "OriginalActorId");

            migrationBuilder.CreateIndex(
                name: "IX_OfflineAdmittedFileReferences_ProjectId",
                table: "OfflineAdmittedFileReferences",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_OfflineAdmittedFileReferences_TaskId",
                table: "OfflineAdmittedFileReferences",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_OfflineDeviceRegistrations_ActorId",
                table: "OfflineDeviceRegistrations",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_OfflineDeviceRegistrations_ProjectId_ActorId_DeviceId_Revision",
                table: "OfflineDeviceRegistrations",
                columns: new[] { "ProjectId", "ActorId", "DeviceId", "Revision" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OfflineDeviceRegistrations_ProjectId_KeyFingerprint",
                table: "OfflineDeviceRegistrations",
                columns: new[] { "ProjectId", "KeyFingerprint" });

            migrationBuilder.CreateIndex(
                name: "IX_OfflineDeviceRevocations_DeviceRegistrationId",
                table: "OfflineDeviceRevocations",
                column: "DeviceRegistrationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OfflineDeviceRevocations_DeviceRegistrationId_ProjectId",
                table: "OfflineDeviceRevocations",
                columns: new[] { "DeviceRegistrationId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_OfflineDeviceRevocations_ProjectId",
                table: "OfflineDeviceRevocations",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_OfflineDeviceRevocations_RevokedBy",
                table: "OfflineDeviceRevocations",
                column: "RevokedBy");

            migrationBuilder.CreateIndex(
                name: "IX_OfflineEncryptedCaptureArtifacts_OriginalActorId",
                table: "OfflineEncryptedCaptureArtifacts",
                column: "OriginalActorId");

            migrationBuilder.CreateIndex(
                name: "IX_OfflineEncryptedCaptureArtifacts_ParentPackageId_CaptureOriginId_ManifestHash_ChunkIndex",
                table: "OfflineEncryptedCaptureArtifacts",
                columns: new[] { "ParentPackageId", "CaptureOriginId", "ManifestHash", "ChunkIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OfflineEncryptedCaptureArtifacts_ParentPackageId_ProjectId",
                table: "OfflineEncryptedCaptureArtifacts",
                columns: new[] { "ParentPackageId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_OfflineEncryptedCaptureArtifacts_ProjectId",
                table: "OfflineEncryptedCaptureArtifacts",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_OfflineEncryptedCaptureArtifacts_RegisteredBy",
                table: "OfflineEncryptedCaptureArtifacts",
                column: "RegisteredBy");

            migrationBuilder.CreateIndex(
                name: "IX_OfflineEncryptedCaptureArtifacts_SourceDeviceRegistrationId_ProjectId",
                table: "OfflineEncryptedCaptureArtifacts",
                columns: new[] { "SourceDeviceRegistrationId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_OfflineEncryptedCaptureArtifacts_TaskId",
                table: "OfflineEncryptedCaptureArtifacts",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_OfflineEncryptedPackages_OriginalActorId",
                table: "OfflineEncryptedPackages",
                column: "OriginalActorId");

            migrationBuilder.CreateIndex(
                name: "IX_OfflineEncryptedPackages_ProjectId_SourceDeviceRegistrationId_SourceBatchId",
                table: "OfflineEncryptedPackages",
                columns: new[] { "ProjectId", "SourceDeviceRegistrationId", "SourceBatchId" });

            migrationBuilder.CreateIndex(
                name: "IX_OfflineEncryptedPackages_RegisteredBy",
                table: "OfflineEncryptedPackages",
                column: "RegisteredBy");

            migrationBuilder.CreateIndex(
                name: "IX_OfflineEncryptedPackages_SourceDeviceRegistrationId_ProjectId",
                table: "OfflineEncryptedPackages",
                columns: new[] { "SourceDeviceRegistrationId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_OfflineEvidenceCaptureReferences_ActualUploaderId",
                table: "OfflineEvidenceCaptureReferences",
                column: "ActualUploaderId");

            migrationBuilder.CreateIndex(
                name: "IX_OfflineEvidenceCaptureReferences_AdmissionId_CaptureOriginId",
                table: "OfflineEvidenceCaptureReferences",
                columns: new[] { "AdmissionId", "CaptureOriginId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OfflineEvidenceCaptureReferences_AdmissionId_ProjectId",
                table: "OfflineEvidenceCaptureReferences",
                columns: new[] { "AdmissionId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_OfflineEvidenceCaptureReferences_BindingId_ProjectId",
                table: "OfflineEvidenceCaptureReferences",
                columns: new[] { "BindingId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_OfflineEvidenceCaptureReferences_FileId",
                table: "OfflineEvidenceCaptureReferences",
                column: "FileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OfflineEvidenceCaptureReferences_GrantId_ProjectId",
                table: "OfflineEvidenceCaptureReferences",
                columns: new[] { "GrantId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_OfflineEvidenceCaptureReferences_OriginalActorId",
                table: "OfflineEvidenceCaptureReferences",
                column: "OriginalActorId");

            migrationBuilder.CreateIndex(
                name: "IX_OfflineEvidenceCaptureReferences_ProjectId",
                table: "OfflineEvidenceCaptureReferences",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_OfflineEvidenceCaptureReferences_TaskId",
                table: "OfflineEvidenceCaptureReferences",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_OfflineEvidenceCaptureReferences_UploadSessionId",
                table: "OfflineEvidenceCaptureReferences",
                column: "UploadSessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OfflineHandoverGrantRevocations_GrantId",
                table: "OfflineHandoverGrantRevocations",
                column: "GrantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OfflineHandoverGrantRevocations_GrantId_ProjectId",
                table: "OfflineHandoverGrantRevocations",
                columns: new[] { "GrantId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_OfflineHandoverGrantRevocations_ProjectId",
                table: "OfflineHandoverGrantRevocations",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_OfflineHandoverGrantRevocations_RevokedBy",
                table: "OfflineHandoverGrantRevocations",
                column: "RevokedBy");

            migrationBuilder.CreateIndex(
                name: "IX_OfflineHandoverGrants_IssuedBy",
                table: "OfflineHandoverGrants",
                column: "IssuedBy");

            migrationBuilder.CreateIndex(
                name: "IX_OfflineHandoverGrants_PackageId_ProjectId",
                table: "OfflineHandoverGrants",
                columns: new[] { "PackageId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_OfflineHandoverGrants_ProjectId_PackageId_RecipientActorId_ExpiresAt",
                table: "OfflineHandoverGrants",
                columns: new[] { "ProjectId", "PackageId", "RecipientActorId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OfflineHandoverGrants_RecipientActorId",
                table: "OfflineHandoverGrants",
                column: "RecipientActorId");

            migrationBuilder.CreateIndex(
                name: "IX_OfflineHandoverGrants_RecipientDeviceRegistrationId_ProjectId",
                table: "OfflineHandoverGrants",
                columns: new[] { "RecipientDeviceRegistrationId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_OfflineHandoverGrants_SourceActorId",
                table: "OfflineHandoverGrants",
                column: "SourceActorId");

            migrationBuilder.CreateIndex(
                name: "IX_OfflineHandoverGrants_SourceDeviceRegistrationId_ProjectId",
                table: "OfflineHandoverGrants",
                columns: new[] { "SourceDeviceRegistrationId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_OfflineOperationAdmissions_BatchId_BindingId",
                table: "OfflineOperationAdmissions",
                columns: new[] { "BatchId", "BindingId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OfflineOperationAdmissions_BatchId_ProjectId",
                table: "OfflineOperationAdmissions",
                columns: new[] { "BatchId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_OfflineOperationAdmissions_BindingId_ProjectId",
                table: "OfflineOperationAdmissions",
                columns: new[] { "BindingId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_OfflineOperationAdmissions_CurrentImporterId",
                table: "OfflineOperationAdmissions",
                column: "CurrentImporterId");

            migrationBuilder.CreateIndex(
                name: "IX_OfflineOperationAdmissions_GrantId_ProjectId",
                table: "OfflineOperationAdmissions",
                columns: new[] { "GrantId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_OfflineOperationAdmissions_ProjectId",
                table: "OfflineOperationAdmissions",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_OfflineOperationBindings_AssignmentId",
                table: "OfflineOperationBindings",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_OfflineOperationBindings_EffectId",
                table: "OfflineOperationBindings",
                column: "EffectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OfflineOperationBindings_OriginalActorId",
                table: "OfflineOperationBindings",
                column: "OriginalActorId");

            migrationBuilder.CreateIndex(
                name: "IX_OfflineOperationBindings_ProjectId_OriginId",
                table: "OfflineOperationBindings",
                columns: new[] { "ProjectId", "OriginId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OfflineOperationBindings_RepairResourceId",
                table: "OfflineOperationBindings",
                column: "RepairResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_OfflineOperationBindings_SnapshotId_ProjectId",
                table: "OfflineOperationBindings",
                columns: new[] { "SnapshotId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_OfflineOperationBindings_SourceDeviceRegistrationId_ProjectId",
                table: "OfflineOperationBindings",
                columns: new[] { "SourceDeviceRegistrationId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_OfflineOperationBindings_TaskId",
                table: "OfflineOperationBindings",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_OfflineOperationResults_AdmissionId_ProjectId",
                table: "OfflineOperationResults",
                columns: new[] { "AdmissionId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_OfflineOperationResults_BatchId_ProjectId",
                table: "OfflineOperationResults",
                columns: new[] { "BatchId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_OfflineOperationResults_ProjectId_OriginId_RecordedAt",
                table: "OfflineOperationResults",
                columns: new[] { "ProjectId", "OriginId", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OfflineOriginTimeVerifications_BindingId_ProjectId",
                table: "OfflineOriginTimeVerifications",
                columns: new[] { "BindingId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_OfflineOriginTimeVerifications_BindingId_ProofSourceKind_ProofSourceId",
                table: "OfflineOriginTimeVerifications",
                columns: new[] { "BindingId", "ProofSourceKind", "ProofSourceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OfflineOriginTimeVerifications_CanonicalOriginId",
                table: "OfflineOriginTimeVerifications",
                column: "CanonicalOriginId");

            migrationBuilder.CreateIndex(
                name: "IX_OfflineOriginTimeVerifications_ProjectId",
                table: "OfflineOriginTimeVerifications",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_OfflineOriginTimeVerifications_VerifiedBy",
                table: "OfflineOriginTimeVerifications",
                column: "VerifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_OfflinePackageFileReferences_FileId",
                table: "OfflinePackageFileReferences",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_OfflinePackageFileReferences_PackageId_FileId",
                table: "OfflinePackageFileReferences",
                columns: new[] { "PackageId", "FileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OfflinePackageFileReferences_PackageId_ProjectId",
                table: "OfflinePackageFileReferences",
                columns: new[] { "PackageId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_OfflinePackageFileReferences_ProjectId",
                table: "OfflinePackageFileReferences",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_OfflineSyncBatches_CurrentImporterId",
                table: "OfflineSyncBatches",
                column: "CurrentImporterId");

            migrationBuilder.CreateIndex(
                name: "IX_OfflineSyncBatches_GrantId_ProjectId",
                table: "OfflineSyncBatches",
                columns: new[] { "GrantId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_OfflineSyncBatches_PackageId_ProjectId",
                table: "OfflineSyncBatches",
                columns: new[] { "PackageId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_OfflineSyncBatches_ProjectId_SourceDeviceRegistrationId_SourceBatchId_CurrentImporterId_ContentHash",
                table: "OfflineSyncBatches",
                columns: new[] { "ProjectId", "SourceDeviceRegistrationId", "SourceBatchId", "CurrentImporterId", "ContentHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OfflineSyncBatches_RecipientDeviceRegistrationId_ProjectId",
                table: "OfflineSyncBatches",
                columns: new[] { "RecipientDeviceRegistrationId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_OfflineSyncBatches_SourceDeviceRegistrationId_ProjectId",
                table: "OfflineSyncBatches",
                columns: new[] { "SourceDeviceRegistrationId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_OfflineTaskSnapshots_AssignmentId",
                table: "OfflineTaskSnapshots",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_OfflineTaskSnapshots_DeviceRegistrationId_ProjectId",
                table: "OfflineTaskSnapshots",
                columns: new[] { "DeviceRegistrationId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_OfflineTaskSnapshots_OriginalActorId",
                table: "OfflineTaskSnapshots",
                column: "OriginalActorId");

            migrationBuilder.CreateIndex(
                name: "IX_OfflineTaskSnapshots_ProjectId_TaskId_AssignmentId_DeviceRegistrationId",
                table: "OfflineTaskSnapshots",
                columns: new[] { "ProjectId", "TaskId", "AssignmentId", "DeviceRegistrationId" });

            migrationBuilder.CreateIndex(
                name: "IX_OfflineTaskSnapshots_TaskId",
                table: "OfflineTaskSnapshots",
                column: "TaskId");
            InstallOfflineGuards(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            RefusePopulatedOfflineDowngrade(migrationBuilder);
            migrationBuilder.DropTable(
                name: "OfflineAdmittedFileReferences");

            migrationBuilder.DropTable(
                name: "OfflineDeviceRevocations");

            migrationBuilder.DropTable(
                name: "OfflineEncryptedCaptureArtifacts");

            migrationBuilder.DropTable(
                name: "OfflineEvidenceCaptureReferences");

            migrationBuilder.DropTable(
                name: "OfflineHandoverGrantRevocations");

            migrationBuilder.DropTable(
                name: "OfflineOperationResults");

            migrationBuilder.DropTable(
                name: "OfflineOriginTimeVerifications");

            migrationBuilder.DropTable(
                name: "OfflinePackageFileReferences");

            migrationBuilder.DropTable(
                name: "OfflineOperationAdmissions");

            migrationBuilder.DropTable(
                name: "OfflineOperationBindings");

            migrationBuilder.DropTable(
                name: "OfflineSyncBatches");

            migrationBuilder.DropTable(
                name: "OfflineTaskSnapshots");

            migrationBuilder.DropTable(
                name: "OfflineHandoverGrants");

            migrationBuilder.DropTable(
                name: "OfflineEncryptedPackages");

            migrationBuilder.DropTable(
                name: "OfflineDeviceRegistrations");
        }
    }
}
