using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class BaselineCurrentSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CauseCategories",
                columns: table => new
                {
                    Code = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CauseCategories", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "DefectTypes",
                columns: table => new
                {
                    Code = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DefectTypes", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "DroneDevices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SerialNo = table.Column<string>(type: "varchar(120)", unicode: false, maxLength: 120, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    ChecklistVersion = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DroneDevices", x => x.Id);
                    table.CheckConstraint("CK_DroneDevices_Status", "[Status] IN (1, 2, 3)");
                });

            migrationBuilder.CreateTable(
                name: "IdempotencyRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Operation = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    RequestFingerprint = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    OperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OutcomeJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdempotencyRecords", x => x.Id);
                    table.CheckConstraint("CK_IdempotencyRecords_OutcomeJson_Json", "ISJSON([OutcomeJson]) = 1");
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MessageType = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeliveryStatus = table.Column<byte>(type: "tinyint", nullable: false),
                    DeliveryAttemptCount = table.Column<int>(type: "int", nullable: false),
                    NextAttemptAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    LeaseOwner = table.Column<string>(type: "varchar(120)", unicode: false, maxLength: 120, nullable: true),
                    LeaseExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    LastErrorCode = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: true),
                    LastErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                    table.CheckConstraint("CK_OutboxMessages_DeliveryAttemptCount", "[DeliveryAttemptCount] >= 0");
                    table.CheckConstraint("CK_OutboxMessages_DeliveryStatus", "[DeliveryStatus] IN (1, 2, 3, 4)");
                    table.CheckConstraint("CK_OutboxMessages_PayloadJson_Json", "ISJSON([PayloadJson]) = 1");
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectCode = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EngineeringUtmSrid = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                    table.CheckConstraint("CK_Projects_DateRange", "[EndDate] IS NULL OR [StartDate] IS NULL OR [EndDate] >= [StartDate]");
                    table.CheckConstraint("CK_Projects_EngineeringUtmSrid", "[EngineeringUtmSrid] IS NULL OR [EngineeringUtmSrid] IN (32648, 32649)");
                    table.CheckConstraint("CK_Projects_Status", "[Status] IN (1, 2, 3)");
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Code = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NormalizedName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "SeverityRuleVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StandardCode = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: false),
                    RoadTypeCode = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: false),
                    VersionNo = table.Column<int>(type: "int", nullable: false),
                    RuleDefinition = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeverityRuleVersions", x => x.Id);
                    table.CheckConstraint("CK_SeverityRuleVersions_EffectiveDateRange", "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]");
                    table.CheckConstraint("CK_SeverityRuleVersions_RuleDefinition_JsonObject", "ISJSON([RuleDefinition]) = 1 AND LEFT(LTRIM([RuleDefinition]), 1) = '{'");
                    table.CheckConstraint("CK_SeverityRuleVersions_VersionNo_Positive", "[VersionNo] > 0");
                });

            migrationBuilder.CreateTable(
                name: "ConsumerEffectReceipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConsumerName = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    EffectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcessedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsumerEffectReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsumerEffectReceipts_OutboxMessages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "OutboxMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Anh02ExportSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Hash = table.Column<string>(type: "char(64)", nullable: false),
                    CapturedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Anh02ExportSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Anh02ExportSnapshots_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "H6NotificationOccurrences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurrenceKey = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    ContentFingerprint = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    EventType = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    SourceKind = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ScheduledAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_H6NotificationOccurrences", x => x.Id);
                    table.CheckConstraint("CK_H6NotificationOccurrences_Hashes", "LEN([OccurrenceKey])=64 AND LEN([ContentFingerprint])=64");
                    table.CheckConstraint("CK_H6NotificationOccurrences_Json", "ISJSON([PayloadJson])=1");
                    table.ForeignKey(
                        name: "FK_H6NotificationOccurrences_OutboxMessages_SourceEventId",
                        column: x => x.SourceEventId,
                        principalTable: "OutboxMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_H6NotificationOccurrences_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoadRouteSystems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoadRouteSystems", x => x.Id);
                    table.UniqueConstraint("AK_RoadRouteSystems_Id_ProjectId", x => new { x.Id, x.ProjectId });
                    table.ForeignKey(
                        name: "FK_RoadRouteSystems_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoadSections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoadSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoadSections_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WeeklyReviewRecoveryPeriods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduledAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RecoveredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsLatestAtRecovery = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeeklyReviewRecoveryPeriods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeeklyReviewRecoveryPeriods_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RoleCode = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    MustChangePassword = table.Column<bool>(type: "bit", nullable: false),
                    LastLoginAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    SuspendedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SecurityStamp = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.CheckConstraint("CK_Users_Status", "[Status] IN (1, 2, 3)");
                    table.ForeignKey(
                        name: "FK_Users_Roles_RoleCode",
                        column: x => x.RoleCode,
                        principalTable: "Roles",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "H6NotificationEventReceipts",
                columns: table => new
                {
                    OutboxMessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompletionFence = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurrenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MessageType = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    PayloadHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "varchar(24)", unicode: false, maxLength: 24, nullable: false),
                    RecordedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_H6NotificationEventReceipts", x => x.OutboxMessageId);
                    table.ForeignKey(
                        name: "FK_H6NotificationEventReceipts_H6NotificationOccurrences_OccurrenceId",
                        column: x => x.OccurrenceId,
                        principalTable: "H6NotificationOccurrences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_H6NotificationEventReceipts_OutboxMessages_OutboxMessageId",
                        column: x => x.OutboxMessageId,
                        principalTable: "OutboxMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoadGeometryDrafts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoadSectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RoadCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    RoadName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    InputJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OriginalCoordinatesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourceChecksum = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoadGeometryDrafts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoadGeometryDrafts_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoadGeometryDrafts_RoadSections_RoadSectionId",
                        column: x => x.RoadSectionId,
                        principalTable: "RoadSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountStatusChangeLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    FromStatus = table.Column<byte>(type: "tinyint", nullable: false),
                    ToStatus = table.Column<byte>(type: "tinyint", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HandoverReference = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountStatusChangeLogs", x => x.Id);
                    table.CheckConstraint("CK_AccountStatusChangeLogs_FromStatus", "[FromStatus] IN (1, 2, 3)");
                    table.CheckConstraint("CK_AccountStatusChangeLogs_FromToStatus_Diff", "[FromStatus] <> [ToStatus]");
                    table.CheckConstraint("CK_AccountStatusChangeLogs_Reason_SafeCode", "[Reason] IN ('ADMINISTRATOR_INITIATED', 'SELF_SERVICE_ACCOUNT_RECOVERY', 'REGISTRATION_APPROVED', 'SAFETY_POLICY_VIOLATION', 'NO_STATUS_CHANGE', 'ADMINISTRATIVE_LOCK', 'SECURITY_INCIDENT', 'ACCOUNT_REACTIVATED')");
                    table.CheckConstraint("CK_AccountStatusChangeLogs_Source_SafeCode", "[Source] IN ('ADMIN_API', 'SELF_SERVICE', 'IDENTITY_SERVICE', 'COMPLIANCE_REVIEW', 'SYSTEM')");
                    table.CheckConstraint("CK_AccountStatusChangeLogs_ToStatus", "[ToStatus] IN (1, 2, 3)");
                    table.ForeignKey(
                        name: "FK_AccountStatusChangeLogs_Users_ChangedByUserId",
                        column: x => x.ChangedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountStatusChangeLogs_Users_TargetUserId",
                        column: x => x.TargetUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AIModelVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModelName = table.Column<string>(type: "varchar(120)", unicode: false, maxLength: 120, nullable: false),
                    VersionLabel = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: false),
                    ArtifactUri = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    Metrics = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OperatingThresholds = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    ReleasedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    ReleasedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIModelVersions", x => x.Id);
                    table.CheckConstraint("CK_AIModelVersions_Metrics_Json", "[Metrics] IS NULL OR ISJSON([Metrics]) = 1");
                    table.CheckConstraint("CK_AIModelVersions_OperatingThresholds_Json", "[OperatingThresholds] IS NULL OR ISJSON([OperatingThresholds]) = 1");
                    table.CheckConstraint("CK_AIModelVersions_ReleaseMetadata", "([ReleasedAt] IS NULL AND [ReleasedByUserId] IS NULL) OR ([ReleasedAt] IS NOT NULL AND [ReleasedByUserId] IS NOT NULL)");
                    table.CheckConstraint("CK_AIModelVersions_Status", "[Status] IN (1, 2, 3)");
                    table.ForeignKey(
                        name: "FK_AIModelVersions_Users_ReleasedByUserId",
                        column: x => x.ReleasedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    EventType = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BeforeSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AfterSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Source = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.CheckConstraint("CK_AuditLogs_AfterSnapshot_Json", "[AfterSnapshot] IS NULL OR ISJSON([AfterSnapshot]) = 1");
                    table.CheckConstraint("CK_AuditLogs_BeforeSnapshot_Json", "[BeforeSnapshot] IS NULL OR ISJSON([BeforeSnapshot]) = 1");
                    table.ForeignKey(
                        name: "FK_AuditLogs_Users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BaselineSelections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SelectedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SelectedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaselineSelections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BaselineSelections_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BaselineSelections_Users_SelectedBy",
                        column: x => x.SelectedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CrsProfileRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Revision = table.Column<int>(type: "int", nullable: false),
                    SourceSrid = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    SampleOnly = table.Column<bool>(type: "bit", nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrsProfileRevisions", x => x.Id);
                    table.UniqueConstraint("AK_CrsProfileRevisions_Id_ProjectId", x => new { x.Id, x.ProjectId });
                    table.CheckConstraint("CK_CrsProfileRevision", "[Revision]>0 AND [SourceSrid]>=0 AND [Status] IN ('CANDIDATE','VERIFIED') AND ISJSON([PayloadJson])=1");
                    table.ForeignKey(
                        name: "FK_CrsProfileRevisions_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CrsProfileRevisions_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeadlineClocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<byte>(type: "tinyint", nullable: false),
                    TargetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    OriginalDueAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CurrentDueAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AcknowledgedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AcknowledgedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AcknowledgmentEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    AppointedActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AppointedRole = table.Column<byte>(type: "tinyint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeadlineClocks", x => x.Id);
                    table.CheckConstraint("CK_DeadlineClocks_Acknowledgment", "([AcknowledgedAt] IS NULL AND [AcknowledgedByUserId] IS NULL AND [AcknowledgmentEventId] IS NULL) OR ([Kind]=9 AND [AcknowledgedAt] IS NOT NULL AND [AcknowledgedAt]>=[OriginAt] AND [AcknowledgedByUserId] IS NOT NULL AND [AcknowledgmentEventId] IS NOT NULL AND [CompletedAt] IS NOT NULL AND [CompletedAt]=[AcknowledgedAt])");
                    table.CheckConstraint("CK_DeadlineClocks_Kind", "[Kind] BETWEEN 1 AND 10");
                    table.CheckConstraint("CK_DeadlineClocks_Times", "([OriginalDueAt]>[OriginAt] OR ([Kind]=10 AND [OriginalDueAt]=[OriginAt])) AND [CurrentDueAt]>=[OriginalDueAt] AND ([CompletedAt] IS NULL OR [CompletedAt]>=[OriginAt])");
                    table.ForeignKey(
                        name: "FK_DeadlineClocks_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeadlineClocks_Users_AcknowledgedByUserId",
                        column: x => x.AcknowledgedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeadlineClocks_Users_AppointedActorId",
                        column: x => x.AppointedActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Files",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StorageUri = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    OriginalName = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    MimeType = table.Column<string>(type: "varchar(120)", unicode: false, maxLength: 120, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Checksum = table.Column<string>(type: "char(64)", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UploadedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    RetentionUntil = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Files", x => x.Id);
                    table.CheckConstraint("CK_Files_Checksum_Sha256Lowercase", "LEN([Checksum]) = 64 AND [Checksum] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'");
                    table.CheckConstraint("CK_Files_SizeBytes_NonNegative", "[SizeBytes] >= 0");
                    table.ForeignKey(
                        name: "FK_Files_Users_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecipientUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceEntityType = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: false),
                    SourceEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    ReadAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_RecipientUserId",
                        column: x => x.RecipientUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

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
                name: "PasswordRecoveryRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequestedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordRecoveryRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PasswordRecoveryRequests_Users_TargetUserId",
                        column: x => x.TargetUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PasswordResetLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PerformedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Result = table.Column<byte>(type: "tinyint", nullable: false),
                    Source = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetLogs", x => x.Id);
                    table.CheckConstraint("CK_PasswordResetLogs_Reason_SafeCode", "[Reason] IS NULL OR [Reason] IN ('ADMINISTRATOR_INITIATED', 'SELF_SERVICE_ACCOUNT_RECOVERY', 'REGISTRATION_APPROVED', 'SAFETY_POLICY_VIOLATION', 'NO_STATUS_CHANGE', 'ADMINISTRATIVE_LOCK', 'SECURITY_INCIDENT', 'ACCOUNT_REACTIVATED')");
                    table.CheckConstraint("CK_PasswordResetLogs_Result", "[Result] IN (1, 2, 3)");
                    table.CheckConstraint("CK_PasswordResetLogs_Source_SafeCode", "[Source] IN ('ADMIN_API', 'SELF_SERVICE', 'IDENTITY_SERVICE', 'COMPLIANCE_REVIEW', 'SYSTEM')");
                    table.ForeignKey(
                        name: "FK_PasswordResetLogs_Users_PerformedByUserId",
                        column: x => x.PerformedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PasswordResetLogs_Users_TargetUserId",
                        column: x => x.TargetUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleCode = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    ValidTo = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectMembers", x => x.Id);
                    table.CheckConstraint("CK_ProjectMembers_EffectiveDateRange", "[ValidTo] IS NULL OR [ValidTo] >= [ValidFrom]");
                    table.CheckConstraint("CK_ProjectMembers_PrimaryRole", "[IsPrimary] = 0 OR [RoleCode] = 'PM'");
                    table.CheckConstraint("CK_ProjectMembers_Status", "[Status] IN (1, 2)");
                    table.ForeignKey(
                        name: "FK_ProjectMembers_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectMembers_Roles_RoleCode",
                        column: x => x.RoleCode,
                        principalTable: "Roles",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectMembers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RepairPolicyRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Revision = table.Column<int>(type: "int", nullable: false),
                    PublishedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DefectTypeCode = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ChecklistVersion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StopConditions = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairPolicyRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepairPolicyRevisions_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairPolicyRevisions_Users_PublishedBy",
                        column: x => x.PublishedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReporterRegistrationIntents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    ReporterType = table.Column<byte>(type: "tinyint", nullable: false),
                    OtpHash = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    OtpGeneration = table.Column<int>(type: "int", nullable: false),
                    FailedAttempts = table.Column<int>(type: "int", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    ResendAvailableAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    ConsumedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    EmailConfirmedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReporterRegistrationIntents", x => x.Id);
                    table.CheckConstraint("CK_ReporterRegistrationIntents_ReporterType", "[ReporterType] IN (1, 2)");
                    table.ForeignKey(
                        name: "FK_ReporterRegistrationIntents_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Reports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReporterUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Revision = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reports", x => x.Id);
                    table.UniqueConstraint("AK_Reports_Id_ReporterUserId", x => new { x.Id, x.ReporterUserId });
                    table.ForeignKey(
                        name: "FK_Reports_Users_ReporterUserId",
                        column: x => x.ReporterUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RetentionEvaluations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EvaluatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PolicyVersion = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    SelectionJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetentionEvaluations", x => x.Id);
                    table.CheckConstraint("CK_RetentionEvaluation_State", "[Status] IN ('QUEUED','COMPLETE') AND (([Status]='QUEUED' AND [EvaluatedAt] IS NULL) OR ([Status]='COMPLETE' AND [EvaluatedAt] IS NOT NULL))");
                    table.ForeignKey(
                        name: "FK_RetentionEvaluations_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RetentionEvaluations_Users_RequestedBy",
                        column: x => x.RequestedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RetentionHolds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScopeType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ScopeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    State = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ReleasedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReleasedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetentionHolds", x => x.Id);
                    table.CheckConstraint("CK_RetentionHold_State", "[ScopeType] IN ('PROJECT','FILE') AND [State] IN ('ACTIVE','RELEASED') AND (([State]='ACTIVE' AND [ReleasedBy] IS NULL AND [ReleasedAt] IS NULL) OR ([State]='RELEASED' AND [ReleasedBy] IS NOT NULL AND [ReleasedAt] IS NOT NULL))");
                    table.ForeignKey(
                        name: "FK_RetentionHolds_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RetentionHolds_Users_ReleasedBy",
                        column: x => x.ReleasedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IssuedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    DeviceMetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    Lifecycle = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)0),
                    IssuedRole = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    Transport = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)0),
                    LastActivityAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.Id);
                    table.CheckConstraint("CK_Sessions_DeviceMetadataJson_Json", "[DeviceMetadataJson] IS NULL OR ISJSON([DeviceMetadataJson]) = 1");
                    table.CheckConstraint("CK_Sessions_ExpiresAt", "([Lifecycle] = 0 AND [ExpiresAt] IS NOT NULL AND [ExpiresAt] > [IssuedAt]) OR ([Lifecycle] = 1 AND [ExpiresAt] IS NULL AND [IssuedRole] IS NOT NULL)");
                    table.CheckConstraint("CK_Sessions_RevokedAt", "[RevokedAt] IS NULL OR [RevokedAt] >= [IssuedAt]");
                    table.CheckConstraint("CK_Sessions_Transport", "[Transport] IN (0, 1, 2)");
                    table.ForeignKey(
                        name: "FK_Sessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StaffInvitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    RoleCode = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    TokenHash = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    AcceptedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffInvitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaffInvitations_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WeeklyReviewDigests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecoveryPeriodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduledAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RecoveredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RecipientKey = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    RecipientId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RecipientRole = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeeklyReviewDigests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeeklyReviewDigests_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WeeklyReviewDigests_Users_RecipientId",
                        column: x => x.RecipientId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WeeklyReviewDigests_WeeklyReviewRecoveryPeriods_RecoveryPeriodId",
                        column: x => x.RecoveryPeriodId,
                        principalTable: "WeeklyReviewRecoveryPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ValidationRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModelVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DatasetSplitId = table.Column<string>(type: "varchar(120)", unicode: false, maxLength: 120, nullable: false),
                    MeasurementType = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: false),
                    Unit = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    PairsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    UsedCount = table.Column<int>(type: "int", nullable: false),
                    ExcludedCount = table.Column<int>(type: "int", nullable: false),
                    Bias = table.Column<decimal>(type: "decimal(18,6)", nullable: true),
                    Mae = table.Column<decimal>(type: "decimal(18,6)", nullable: true),
                    Rmse = table.Column<decimal>(type: "decimal(18,6)", nullable: true),
                    ExclusionReasonsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValidationRuns", x => x.Id);
                    table.CheckConstraint("CK_ValidationRuns_ExclusionReasonsJson", "ISJSON([ExclusionReasonsJson]) = 1 AND LEFT(LTRIM([ExclusionReasonsJson]), 1) = '['");
                    table.CheckConstraint("CK_ValidationRuns_PairsJson", "ISJSON([PairsJson]) = 1 AND LEFT(LTRIM([PairsJson]), 1) = '['");
                    table.CheckConstraint("CK_ValidationRuns_Status", "[Status] IN (1, 2, 3)");
                    table.ForeignKey(
                        name: "FK_ValidationRuns_AIModelVersions_ModelVersionId",
                        column: x => x.ModelVersionId,
                        principalTable: "AIModelVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoadSectionVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoadSectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNo = table.Column<int>(type: "int", nullable: false),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    Geometry = table.Column<LineString>(type: "geometry", nullable: false),
                    CrsProfileRevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EffectiveFrom = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    ChangeReason = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoadSectionVersions", x => x.Id);
                    table.CheckConstraint("CK_RoadSectionVersions_Geometry_AllowedSrid", "([CrsProfileRevisionId] IS NULL AND [Geometry].STSrid IN (32648, 32649)) OR ([CrsProfileRevisionId] IS NOT NULL AND [Geometry].STSrid>=0)");
                    table.CheckConstraint("CK_RoadSectionVersions_Geometry_LineString", "[Geometry].STGeometryType() = 'LineString'");
                    table.CheckConstraint("CK_RoadSectionVersions_VersionNo_Positive", "[VersionNo] > 0");
                    table.ForeignKey(
                        name: "FK_RoadSectionVersions_CrsProfileRevisions_CrsProfileRevisionId",
                        column: x => x.CrsProfileRevisionId,
                        principalTable: "CrsProfileRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoadSectionVersions_RoadSections_RoadSectionId",
                        column: x => x.RoadSectionId,
                        principalTable: "RoadSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BusinessReceivingRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<byte>(type: "tinyint", nullable: false),
                    SourceKind = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceVersion = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ScopeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResponsibleActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResponsibleRole = table.Column<byte>(type: "tinyint", nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AcknowledgmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AcknowledgedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AcknowledgedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ClaimedDeviceAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ClockId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessReceivingRequests", x => x.Id);
                    table.CheckConstraint("CK_BusinessReceivingRequests_Ack", "([AcknowledgedAt] IS NULL AND [AcknowledgmentId] IS NULL AND [AcknowledgedBy] IS NULL AND [ClockId] IS NULL AND [ClaimedDeviceAt] IS NULL) OR ([AcknowledgedAt] IS NOT NULL AND [AcknowledgmentId] IS NOT NULL AND [AcknowledgedBy] IS NOT NULL AND [ClockId] IS NOT NULL AND [AcknowledgedAt]>=[RequestedAt])");
                    table.ForeignKey(
                        name: "FK_BusinessReceivingRequests_DeadlineClocks_ClockId",
                        column: x => x.ClockId,
                        principalTable: "DeadlineClocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BusinessReceivingRequests_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BusinessReceivingRequests_Users_ResponsibleActorId",
                        column: x => x.ResponsibleActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeadlineBreaches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClockId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DueAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ObservedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeadlineBreaches", x => x.Id);
                    table.CheckConstraint("CK_DeadlineBreaches_Times", "[ObservedAt]>=[DueAt]");
                    table.ForeignKey(
                        name: "FK_DeadlineBreaches_DeadlineClocks_ClockId",
                        column: x => x.ClockId,
                        principalTable: "DeadlineClocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeadlineDutyAppointments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClockId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreviousActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CurrentActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<byte>(type: "tinyint", nullable: false),
                    DecisionActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    EffectiveAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeadlineDutyAppointments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeadlineDutyAppointments_DeadlineClocks_ClockId",
                        column: x => x.ClockId,
                        principalTable: "DeadlineClocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeadlineDutyAppointments_Users_CurrentActorId",
                        column: x => x.CurrentActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeadlineDutyAppointments_Users_DecisionActorId",
                        column: x => x.DecisionActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeadlineExtensions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClockId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PreviousDueAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    NewDueAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    PreviousDeadlineBreached = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeadlineExtensions", x => x.Id);
                    table.CheckConstraint("CK_DeadlineExtensions_Times", "[NewDueAt]>[PreviousDueAt] AND [NewDueAt]>[OccurredAt]");
                    table.ForeignKey(
                        name: "FK_DeadlineExtensions_DeadlineClocks_ClockId",
                        column: x => x.ClockId,
                        principalTable: "DeadlineClocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeadlineExtensions_Users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "H6NotificationCalendar",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClockId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlannedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ScheduledAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    SchedulerRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OutboxMessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ObservedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_H6NotificationCalendar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_H6NotificationCalendar_DeadlineClocks_ClockId",
                        column: x => x.ClockId,
                        principalTable: "DeadlineClocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_H6NotificationCalendar_OutboxMessages_OutboxMessageId",
                        column: x => x.OutboxMessageId,
                        principalTable: "OutboxMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Anh02ExportSnapshotFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileVersion = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Sha256 = table.Column<string>(type: "char(64)", nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    MediaType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Included = table.Column<bool>(type: "bit", nullable: false),
                    ArchivePath = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Anh02ExportSnapshotFiles", x => x.Id);
                    table.CheckConstraint("CK_Anh02ExportSnapshotFile_Size", "[SizeBytes]>0");
                    table.ForeignKey(
                        name: "FK_Anh02ExportSnapshotFiles_Anh02ExportSnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "Anh02ExportSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Anh02ExportSnapshotFiles_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FileScopes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TargetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Purpose = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileScopes", x => x.Id);
                    table.CheckConstraint("CK_FileScopes_PrivateShape", "[ProjectId] IS NOT NULL OR ([Purpose] = 'REPORT_PHOTO' AND [TargetId] IS NULL)");
                    table.ForeignKey(
                        name: "FK_FileScopes_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FileScopes_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FileScopes_Users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HandoverDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentNo = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: false),
                    HandoverDate = table.Column<DateOnly>(type: "date", nullable: false),
                    AcceptedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HandoverDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HandoverDocuments_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HandoverDocuments_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HandoverDocuments_Users_AcceptedByUserId",
                        column: x => x.AcceptedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RetentionBasisRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Revision = table.Column<int>(type: "int", nullable: false),
                    PolicyVersion = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Classification = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    InventoryVersion = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    InventoryComplete = table.Column<bool>(type: "bit", nullable: false),
                    WarrantyReferencesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConfirmedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConfirmedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    SupersedesId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetentionBasisRevisions", x => x.Id);
                    table.UniqueConstraint("AK_RetentionBasisRevisions_Id_FileId", x => new { x.Id, x.FileId });
                    table.CheckConstraint("CK_RetentionBasis_Revision", "[Revision]>0 AND [PolicyVersion]='pr41a.v1' AND [Classification] IN ('EVIDENCE','TEMPORARY_EXPORT') AND ISJSON([WarrantyReferencesJson])=1");
                    table.ForeignKey(
                        name: "FK_RetentionBasisRevisions_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RetentionBasisRevisions_RetentionBasisRevisions_SupersedesId_FileId",
                        columns: x => new { x.SupersedesId, x.FileId },
                        principalTable: "RetentionBasisRevisions",
                        principalColumns: new[] { "Id", "FileId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RetentionBasisRevisions_Users_ConfirmedBy",
                        column: x => x.ConfirmedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UploadSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObjectKey = table.Column<string>(type: "varchar(512)", unicode: false, maxLength: 512, nullable: false),
                    Purpose = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    MediaType = table.Column<string>(type: "varchar(120)", unicode: false, maxLength: 120, nullable: false),
                    ExpectedSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    ExpectedChecksumSha256 = table.Column<string>(type: "char(64)", nullable: false),
                    PartSizeBytes = table.Column<int>(type: "int", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    StorageUploadId = table.Column<string>(type: "varchar(1024)", unicode: false, maxLength: 1024, nullable: true),
                    FailureCode = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: true),
                    MultipartFence = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MultipartPhase = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    MultipartDeadline = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    MultipartNextCheckAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploadSessions", x => x.Id);
                    table.CheckConstraint("CK_UploadSessions_ExpectedSizeBytes_Positive", "[ExpectedSizeBytes] > 0");
                    table.CheckConstraint("CK_UploadSessions_PartSizeBytes_Positive", "[PartSizeBytes] > 0");
                    table.ForeignKey(
                        name: "FK_UploadSessions_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UploadSessions_Users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "H6NotificationAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NotificationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OutboxMessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PreviousAuditId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Classification = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    SourceKind = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReasonCode = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: false),
                    ResolverVersion = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    DedupKey = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    RecordedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_H6NotificationAudits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_H6NotificationAudits_H6NotificationAudits_PreviousAuditId",
                        column: x => x.PreviousAuditId,
                        principalTable: "H6NotificationAudits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_H6NotificationAudits_Notifications_NotificationId",
                        column: x => x.NotificationId,
                        principalTable: "Notifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_H6NotificationAudits_OutboxMessages_OutboxMessageId",
                        column: x => x.OutboxMessageId,
                        principalTable: "OutboxMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_H6NotificationAudits_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "H6NotificationDeliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurrenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecipientUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RecipientKey = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "varchar(24)", unicode: false, maxLength: 24, nullable: false),
                    NotificationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReasonCode = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: true),
                    DeliveredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    NextAttemptAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_H6NotificationDeliveries", x => x.Id);
                    table.CheckConstraint("CK_H6NotificationDeliveries_State", "[Status] IN ('PENDING','UNRESOLVED','DELIVERED') AND (([Status]='DELIVERED' AND [RecipientUserId] IS NOT NULL AND [NotificationId] IS NOT NULL AND [DeliveredAtUtc] IS NOT NULL AND [ReasonCode] IS NULL) OR ([Status]<>'DELIVERED' AND [NotificationId] IS NULL AND [DeliveredAtUtc] IS NULL))");
                    table.ForeignKey(
                        name: "FK_H6NotificationDeliveries_H6NotificationOccurrences_OccurrenceId",
                        column: x => x.OccurrenceId,
                        principalTable: "H6NotificationOccurrences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_H6NotificationDeliveries_Notifications_NotificationId",
                        column: x => x.NotificationId,
                        principalTable: "Notifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_H6NotificationDeliveries_Users_RecipientUserId",
                        column: x => x.RecipientUserId,
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
                name: "RepairPolicyMeasurementRules",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PolicyRevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Minimum = table.Column<decimal>(type: "decimal(20,6)", precision: 20, scale: 6, nullable: false),
                    Maximum = table.Column<decimal>(type: "decimal(20,6)", precision: 20, scale: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairPolicyMeasurementRules", x => new { x.PolicyRevisionId, x.Code });
                    table.ForeignKey(
                        name: "FK_RepairPolicyMeasurementRules_RepairPolicyRevisions_PolicyRevisionId",
                        column: x => x.PolicyRevisionId,
                        principalTable: "RepairPolicyRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RepairPolicyRevocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    At = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PolicyRevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairPolicyRevocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepairPolicyRevocations_RepairPolicyRevisions_PolicyRevisionId",
                        column: x => x.PolicyRevisionId,
                        principalTable: "RepairPolicyRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairPolicyRevocations_Users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReportOriginalEvidence",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplementId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileVersion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VerificationState = table.Column<int>(type: "int", nullable: false),
                    CaptureMetadata_CapturedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CaptureMetadata_Latitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    CaptureMetadata_Longitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    CaptureMetadata_AccuracyMeters = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    CaptureMetadata_LocationSource = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportOriginalEvidence", x => x.Id);
                    table.CheckConstraint("CK_ReportOriginalEvidence_Original", "[SupplementId] IS NULL AND [VerificationState] = 1");
                    table.ForeignKey(
                        name: "FK_ReportOriginalEvidence_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReportOriginalEvidence_Reports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "Reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReportOriginalEvidence_Reports_ReportId_OwnerUserId",
                        columns: x => new { x.ReportId, x.OwnerUserId },
                        principalTable: "Reports",
                        principalColumns: new[] { "Id", "ReporterUserId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReportOriginalEvidence_Users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReportSupplements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportSupplements", x => x.Id);
                    table.UniqueConstraint("AK_ReportSupplements_Id_ReportId", x => new { x.Id, x.ReportId });
                    table.ForeignKey(
                        name: "FK_ReportSupplements_Reports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "Reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RetentionEvaluationItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvaluationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Eligibility = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ReasonCodesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EligibleAfter = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    BasisVersion = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    InventoryVersion = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    HoldVersion = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ControlSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetentionEvaluationItems", x => x.Id);
                    table.CheckConstraint("CK_RetentionEvaluationItem_State", "[Eligibility] IN ('BLOCKED_HOLD','WAITING_RETENTION_BASIS','RETAIN_UNTIL','ELIGIBLE_FOR_REVIEW')");
                    table.ForeignKey(
                        name: "FK_RetentionEvaluationItems_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RetentionEvaluationItems_RetentionEvaluations_EvaluationId",
                        column: x => x.EvaluationId,
                        principalTable: "RetentionEvaluations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RetentionHoldHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HoldId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    State = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetentionHoldHistories", x => x.Id);
                    table.CheckConstraint("CK_RetentionHoldHistory_State", "[State] IN ('ACTIVE','RELEASED')");
                    table.ForeignKey(
                        name: "FK_RetentionHoldHistories_RetentionHolds_HoldId",
                        column: x => x.HoldId,
                        principalTable: "RetentionHolds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RetentionHoldHistories_Users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.CheckConstraint("CK_RefreshTokens_TokenHash_NotEmpty", "LEN(LTRIM(RTRIM([TokenHash]))) >= 32");
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StaffInvitationProjects",
                columns: table => new
                {
                    InvitationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffInvitationProjects", x => new { x.InvitationId, x.ProjectId });
                    table.ForeignKey(
                        name: "FK_StaffInvitationProjects_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StaffInvitationProjects_StaffInvitations_InvitationId",
                        column: x => x.InvitationId,
                        principalTable: "StaffInvitations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WeeklyReviewDigestDuties",
                columns: table => new
                {
                    DigestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClockId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    OriginAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DueAtRecoveryUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeeklyReviewDigestDuties", x => new { x.DigestId, x.ClockId });
                    table.ForeignKey(
                        name: "FK_WeeklyReviewDigestDuties_DeadlineClocks_ClockId",
                        column: x => x.ClockId,
                        principalTable: "DeadlineClocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WeeklyReviewDigestDuties_WeeklyReviewDigests_DigestId",
                        column: x => x.DigestId,
                        principalTable: "WeeklyReviewDigests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GeometryLocationImpacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreviousRouteVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NewRouteVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AffectedReferencesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RecordedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeometryLocationImpacts", x => x.Id);
                    table.CheckConstraint("CK_GeometryLocationImpacts_Json", "ISJSON([AffectedReferencesJson])=1");
                    table.CheckConstraint("CK_GeometryLocationImpacts_Versions", "[PreviousRouteVersionId]<>[NewRouteVersionId]");
                    table.ForeignKey(
                        name: "FK_GeometryLocationImpacts_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GeometryLocationImpacts_RoadSectionVersions_NewRouteVersionId",
                        column: x => x.NewRouteVersionId,
                        principalTable: "RoadSectionVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GeometryLocationImpacts_RoadSectionVersions_PreviousRouteVersionId",
                        column: x => x.PreviousRouteVersionId,
                        principalTable: "RoadSectionVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GeometryLocationImpacts_Users_RecordedBy",
                        column: x => x.RecordedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NativeRouteVersionFacts",
                columns: table => new
                {
                    RoadSectionVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CrsProfileRevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RouteSystemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RouteKind = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    ParentRouteVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    JunctionOffsetMeters = table.Column<double>(type: "float", nullable: true),
                    CanonicalLengthMeters = table.Column<double>(type: "float", nullable: false),
                    DeclaredLengthMeters = table.Column<double>(type: "float", nullable: true),
                    CalibrationJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SampleOnly = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NativeRouteVersionFacts", x => x.RoadSectionVersionId);
                    table.CheckConstraint("CK_NativeRouteFacts", "[CanonicalLengthMeters]>0 AND ([DeclaredLengthMeters] IS NULL OR [DeclaredLengthMeters]>0) AND (([RouteKind]='MAIN' AND [ParentRouteVersionId] IS NULL AND [JunctionOffsetMeters] IS NULL) OR ([RouteKind]='BRANCH' AND [ParentRouteVersionId] IS NOT NULL AND [JunctionOffsetMeters] IS NOT NULL AND [JunctionOffsetMeters]>=0)) AND ([CalibrationJson] IS NULL OR ISJSON([CalibrationJson])=1)");
                    table.ForeignKey(
                        name: "FK_NativeRouteVersionFacts_CrsProfileRevisions_CrsProfileRevisionId_ProjectId",
                        columns: x => new { x.CrsProfileRevisionId, x.ProjectId },
                        principalTable: "CrsProfileRevisions",
                        principalColumns: new[] { "Id", "ProjectId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NativeRouteVersionFacts_RoadRouteSystems_RouteSystemId_ProjectId",
                        columns: x => new { x.RouteSystemId, x.ProjectId },
                        principalTable: "RoadRouteSystems",
                        principalColumns: new[] { "Id", "ProjectId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NativeRouteVersionFacts_RoadSectionVersions_ParentRouteVersionId",
                        column: x => x.ParentRouteVersionId,
                        principalTable: "RoadSectionVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NativeRouteVersionFacts_RoadSectionVersions_RoadSectionVersionId",
                        column: x => x.RoadSectionVersionId,
                        principalTable: "RoadSectionVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoadGeometryMetadata",
                columns: table => new
                {
                    RoadSectionVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceDraftId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InputJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GeometryHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Wgs84GeometryJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoadGeometryMetadata", x => x.RoadSectionVersionId);
                    table.ForeignKey(
                        name: "FK_RoadGeometryMetadata_RoadGeometryDrafts_SourceDraftId",
                        column: x => x.SourceDraftId,
                        principalTable: "RoadGeometryDrafts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoadGeometryMetadata_RoadSectionVersions_RoadSectionVersionId",
                        column: x => x.RoadSectionVersionId,
                        principalTable: "RoadSectionVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoadSegmentSets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoadSectionVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    DefinitionJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GeometryHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    PublishedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoadSegmentSets", x => x.Id);
                    table.UniqueConstraint("AK_RoadSegmentSets_Id_RoadSectionVersionId", x => new { x.Id, x.RoadSectionVersionId });
                    table.CheckConstraint("CK_RoadSegmentSets_Status", "[Status] IN ('DRAFT','PUBLISHED','SUPERSEDED')");
                    table.ForeignKey(
                        name: "FK_RoadSegmentSets_RoadSectionVersions_RoadSectionVersionId",
                        column: x => x.RoadSectionVersionId,
                        principalTable: "RoadSectionVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SurveyPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoadSectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoadSectionVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ScopeFormatVersion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PlannedStartAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    PlannedEndAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    SurveyType = table.Column<byte>(type: "tinyint", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    OutputRequirements = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyPlans", x => x.Id);
                    table.CheckConstraint("CK_SurveyPlans_OutputRequirements_Json", "ISJSON([OutputRequirements]) = 1");
                    table.CheckConstraint("CK_SurveyPlans_PlannedDateRange", "[PlannedEndAt] >= [PlannedStartAt]");
                    table.CheckConstraint("CK_SurveyPlans_Status", "[Status] IN (1, 2, 3, 4, 5)");
                    table.CheckConstraint("CK_SurveyPlans_SurveyType", "[SurveyType] IN (1, 2, 3)");
                    table.ForeignKey(
                        name: "FK_SurveyPlans_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SurveyPlans_RoadSectionVersions_RoadSectionVersionId",
                        column: x => x.RoadSectionVersionId,
                        principalTable: "RoadSectionVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SurveyPlans_RoadSections_RoadSectionId",
                        column: x => x.RoadSectionId,
                        principalTable: "RoadSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BusinessDutyAppointments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreviousActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CurrentActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DecisionActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    EffectiveAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessDutyAppointments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessDutyAppointments_BusinessReceivingRequests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "BusinessReceivingRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BusinessDutyAppointments_Users_CurrentActorId",
                        column: x => x.CurrentActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BusinessDutyAppointments_Users_DecisionActorId",
                        column: x => x.DecisionActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Warranties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoadSectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HandoverDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HandoverDate = table.Column<DateOnly>(type: "date", nullable: false),
                    WarrantyStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    WarrantyEndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RetainedValue = table.Column<decimal>(type: "decimal(19,2)", precision: 19, scale: 2, nullable: true),
                    Scope = table.Column<byte>(type: "tinyint", nullable: false),
                    Terms = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SourceDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warranties", x => x.Id);
                    table.CheckConstraint("CK_Warranties_DateRange", "[WarrantyEndDate] >= [WarrantyStartDate]");
                    table.CheckConstraint("CK_Warranties_RetainedValue_NonNegative", "[RetainedValue] IS NULL OR [RetainedValue] >= 0");
                    table.CheckConstraint("CK_Warranties_Scope", "[Scope] IN (1, 2, 3, 4)");
                    table.CheckConstraint("CK_Warranties_ScopeRoadSection", "([Scope] <> 1 OR [RoadSectionId] IS NULL) AND ([Scope] <> 2 OR [RoadSectionId] IS NOT NULL)");
                    table.CheckConstraint("CK_Warranties_Status", "[Status] IN (1, 2, 3, 4)");
                    table.ForeignKey(
                        name: "FK_Warranties_Files_SourceDocumentId",
                        column: x => x.SourceDocumentId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Warranties_HandoverDocuments_HandoverDocumentId",
                        column: x => x.HandoverDocumentId,
                        principalTable: "HandoverDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Warranties_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Warranties_RoadSections_RoadSectionId",
                        column: x => x.RoadSectionId,
                        principalTable: "RoadSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RetentionBasisHeads",
                columns: table => new
                {
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Revision = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetentionBasisHeads", x => x.FileId);
                    table.ForeignKey(
                        name: "FK_RetentionBasisHeads_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RetentionBasisHeads_RetentionBasisRevisions_RevisionId_FileId",
                        columns: x => new { x.RevisionId, x.FileId },
                        principalTable: "RetentionBasisRevisions",
                        principalColumns: new[] { "Id", "FileId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UploadMultipartSweeps",
                columns: table => new
                {
                    UploadSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NextCheckAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploadMultipartSweeps", x => x.UploadSessionId);
                    table.ForeignKey(
                        name: "FK_UploadMultipartSweeps_UploadSessions_UploadSessionId",
                        column: x => x.UploadSessionId,
                        principalTable: "UploadSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UploadParts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UploadSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PartNumber = table.Column<int>(type: "int", nullable: false),
                    ETag = table.Column<string>(type: "varchar(512)", unicode: false, maxLength: 512, nullable: true),
                    UrlIssuedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    UrlExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploadParts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UploadParts_UploadSessions_UploadSessionId",
                        column: x => x.UploadSessionId,
                        principalTable: "UploadSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "H6NotificationScopes",
                columns: table => new
                {
                    NotificationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OccurrenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Classification = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    SourceKind = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    ResolverVersion = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    CurrentAuditId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_H6NotificationScopes", x => x.NotificationId);
                    table.ForeignKey(
                        name: "FK_H6NotificationScopes_H6NotificationAudits_CurrentAuditId",
                        column: x => x.CurrentAuditId,
                        principalTable: "H6NotificationAudits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_H6NotificationScopes_H6NotificationOccurrences_OccurrenceId",
                        column: x => x.OccurrenceId,
                        principalTable: "H6NotificationOccurrences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_H6NotificationScopes_Notifications_NotificationId",
                        column: x => x.NotificationId,
                        principalTable: "Notifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_H6NotificationScopes_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "H6NotificationDeliveryAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeliveryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObservedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<string>(type: "varchar(24)", unicode: false, maxLength: 24, nullable: false),
                    ReasonCode = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_H6NotificationDeliveryAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_H6NotificationDeliveryAttempts_H6NotificationDeliveries_DeliveryId",
                        column: x => x.DeliveryId,
                        principalTable: "H6NotificationDeliveries",
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
                name: "ReportSupplementEvidence",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileVersion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VerificationState = table.Column<int>(type: "int", nullable: false),
                    CaptureMetadata_CapturedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CaptureMetadata_Latitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    CaptureMetadata_Longitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    CaptureMetadata_AccuracyMeters = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    CaptureMetadata_LocationSource = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportSupplementEvidence", x => x.Id);
                    table.CheckConstraint("CK_ReportSupplementEvidence_Supplement", "[SupplementId] IS NOT NULL AND [VerificationState] = 1");
                    table.ForeignKey(
                        name: "FK_ReportSupplementEvidence_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReportSupplementEvidence_ReportSupplements_SupplementId_ReportId",
                        columns: x => new { x.SupplementId, x.ReportId },
                        principalTable: "ReportSupplements",
                        principalColumns: new[] { "Id", "ReportId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReportSupplementEvidence_Reports_ReportId_OwnerUserId",
                        columns: x => new { x.ReportId, x.OwnerUserId },
                        principalTable: "Reports",
                        principalColumns: new[] { "Id", "ReporterUserId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReportSupplementEvidence_Users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GeometryLocationImpactDecisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImpactId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeometryLocationImpactDecisions", x => x.Id);
                    table.CheckConstraint("CK_GeometryLocationImpactDecisions_Action", "[Action] IN ('VERIFY','CONTINUE','STOP','REASSIGN')");
                    table.ForeignKey(
                        name: "FK_GeometryLocationImpactDecisions_GeometryLocationImpacts_ImpactId",
                        column: x => x.ImpactId,
                        principalTable: "GeometryLocationImpacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GeometryLocationImpactDecisions_Users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IncidentCases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    VerificationMethod = table.Column<int>(type: "int", nullable: true),
                    TriageReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TriagedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LinkedTargetCaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    GeometryRouteVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GeometrySegmentSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Revision = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    ActiveReportIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentCases", x => x.Id);
                    table.CheckConstraint("CK_IncidentCases_GeometryPair", "([GeometryRouteVersionId] IS NULL AND [GeometrySegmentSetId] IS NULL) OR ([GeometryRouteVersionId] IS NOT NULL AND [GeometrySegmentSetId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_IncidentCases_IncidentCases_LinkedTargetCaseId",
                        column: x => x.LinkedTargetCaseId,
                        principalTable: "IncidentCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IncidentCases_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IncidentCases_RoadSegmentSets_GeometrySegmentSetId_GeometryRouteVersionId",
                        columns: x => new { x.GeometrySegmentSetId, x.GeometryRouteVersionId },
                        principalTable: "RoadSegmentSets",
                        principalColumns: new[] { "Id", "RoadSectionVersionId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PavementLayoutRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RouteVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SegmentSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourcePlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CrsProfileRevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Kind = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    DefinitionJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PavementLayoutRevisions", x => x.Id);
                    table.CheckConstraint("CK_PavementLayoutRevisions_Json", "ISJSON([DefinitionJson])=1 AND ISJSON([SnapshotJson])=1");
                    table.CheckConstraint("CK_PavementLayoutRevisions_Kind", "[Kind] IN ('PLANNED','AS_BUILT') AND (([Kind]='PLANNED' AND [SourcePlanId] IS NULL) OR ([Kind]='AS_BUILT' AND [SourcePlanId] IS NOT NULL))");
                    table.ForeignKey(
                        name: "FK_PavementLayoutRevisions_CrsProfileRevisions_CrsProfileRevisionId_ProjectId",
                        columns: x => new { x.CrsProfileRevisionId, x.ProjectId },
                        principalTable: "CrsProfileRevisions",
                        principalColumns: new[] { "Id", "ProjectId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PavementLayoutRevisions_PavementLayoutRevisions_SourcePlanId",
                        column: x => x.SourcePlanId,
                        principalTable: "PavementLayoutRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PavementLayoutRevisions_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PavementLayoutRevisions_RoadSectionVersions_RouteVersionId",
                        column: x => x.RouteVersionId,
                        principalTable: "RoadSectionVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PavementLayoutRevisions_RoadSegmentSets_SegmentSetId",
                        column: x => x.SegmentSetId,
                        principalTable: "RoadSegmentSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PavementLayoutRevisions_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoadSegments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SegmentSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoadSectionVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    FromOffsetMeters = table.Column<double>(type: "float", nullable: true),
                    ToOffsetMeters = table.Column<double>(type: "float", nullable: true),
                    StartStationMeters = table.Column<double>(type: "float", nullable: true),
                    EndStationMeters = table.Column<double>(type: "float", nullable: true),
                    Geometry = table.Column<LineString>(type: "geometry", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoadSegments", x => x.Id);
                    table.CheckConstraint("CK_RoadSegments_Sequence_Positive", "[Sequence] > 0");
                    table.ForeignKey(
                        name: "FK_RoadSegments_RoadSectionVersions_RoadSectionVersionId",
                        column: x => x.RoadSectionVersionId,
                        principalTable: "RoadSectionVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoadSegments_RoadSegmentSets_SegmentSetId_RoadSectionVersionId",
                        columns: x => new { x.SegmentSetId, x.RoadSectionVersionId },
                        principalTable: "RoadSegmentSets",
                        principalColumns: new[] { "Id", "RoadSectionVersionId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SurveyPlanPostponements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SurveyPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PostponedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NewPlannedStartAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyPlanPostponements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SurveyPlanPostponements_SurveyPlans_SurveyPlanId",
                        column: x => x.SurveyPlanId,
                        principalTable: "SurveyPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SurveyPlanScopes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SurveyPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RouteSectionVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SegmentSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SegmentIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetBand = table.Column<string>(type: "nvarchar(32)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyPlanScopes", x => x.Id);
                    table.CheckConstraint("CK_SurveyPlanScopes_SegmentIdsJson", "ISJSON([SegmentIdsJson]) = 1");
                    table.ForeignKey(
                        name: "FK_SurveyPlanScopes_RoadSectionVersions_RouteSectionVersionId",
                        column: x => x.RouteSectionVersionId,
                        principalTable: "RoadSectionVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SurveyPlanScopes_SurveyPlans_SurveyPlanId",
                        column: x => x.SurveyPlanId,
                        principalTable: "SurveyPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                    AttachedPayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AttachedPayloadHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: true),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfflineSyncBatches", x => x.Id);
                    table.UniqueConstraint("AK_OfflineSyncBatches_Id_ProjectId", x => new { x.Id, x.ProjectId });
                    table.CheckConstraint("CK_OfflineSyncBatches_AttachedPayload", "([AttachedPayloadJson] IS NULL AND [AttachedPayloadHash] IS NULL) OR ([AttachedPayloadJson] IS NOT NULL AND [AttachedPayloadHash] IS NOT NULL AND ISJSON([AttachedPayloadJson])=1 AND LEN([AttachedPayloadHash])=64 AND [AttachedPayloadHash] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%' AND DATALENGTH(CONVERT(varchar(max),[AttachedPayloadJson] COLLATE Latin1_General_100_BIN2_UTF8))<=16777216)");
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
                name: "CaseConclusions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Outcome = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ConcludedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefectIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EvidenceIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseConclusions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaseConclusions_IncidentCases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "IncidentCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CasePublications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DefectIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EvidenceIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RecipientReportIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CasePublications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CasePublications_IncidentCases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "IncidentCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CaseReportLinkHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromCaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToCaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReportIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseReportLinkHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaseReportLinkHistory_IncidentCases_FromCaseId",
                        column: x => x.FromCaseId,
                        principalTable: "IncidentCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CaseReportLinkHistory_IncidentCases_ToCaseId",
                        column: x => x.ToCaseId,
                        principalTable: "IncidentCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CaseReportLinkHistory_Users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CaseReportLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseReportLinks", x => x.Id);
                    table.CheckConstraint("CK_CaseReportLinks_Times", "[EndedAt] IS NULL OR [EndedAt] >= [StartedAt]");
                    table.ForeignKey(
                        name: "FK_CaseReportLinks_IncidentCases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "IncidentCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CaseReportLinks_Reports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "Reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GeometryMapPublications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RouteVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SegmentSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LayoutRevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CrsProfileRevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PublicationMode = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    PublishedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeometryMapPublications", x => x.Id);
                    table.CheckConstraint("CK_GeometryMapPublications_Json", "ISJSON([SnapshotJson])=1");
                    table.CheckConstraint("CK_GeometryMapPublications_Mode", "[PublicationMode] IN ('SAMPLE','OFFICIAL')");
                    table.ForeignKey(
                        name: "FK_GeometryMapPublications_CrsProfileRevisions_CrsProfileRevisionId_ProjectId",
                        columns: x => new { x.CrsProfileRevisionId, x.ProjectId },
                        principalTable: "CrsProfileRevisions",
                        principalColumns: new[] { "Id", "ProjectId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GeometryMapPublications_PavementLayoutRevisions_LayoutRevisionId",
                        column: x => x.LayoutRevisionId,
                        principalTable: "PavementLayoutRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GeometryMapPublications_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GeometryMapPublications_RoadSectionVersions_RouteVersionId",
                        column: x => x.RouteVersionId,
                        principalTable: "RoadSectionVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GeometryMapPublications_RoadSegmentSets_SegmentSetId",
                        column: x => x.SegmentSetId,
                        principalTable: "RoadSegmentSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GeometryMapPublications_Users_PublishedBy",
                        column: x => x.PublishedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PavementSourceFileReferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LayoutRevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContentChecksum = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CaptureFactsJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PavementSourceFileReferences", x => x.Id);
                    table.CheckConstraint("CK_PavementSourceFileReferences_Facts", "LEN([ContentChecksum])=64 AND ISJSON([CaptureFactsJson])=1");
                    table.ForeignKey(
                        name: "FK_PavementSourceFileReferences_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PavementSourceFileReferences_PavementLayoutRevisions_LayoutRevisionId",
                        column: x => x.LayoutRevisionId,
                        principalTable: "PavementLayoutRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CaseConclusionEvidence",
                columns: table => new
                {
                    ConclusionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvidenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalEvidenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SupplementEvidenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseConclusionEvidence", x => new { x.ConclusionId, x.EvidenceId });
                    table.CheckConstraint("CK_CaseConclusionEvidence_TypedEvidence", "([OriginalEvidenceId] IS NOT NULL AND [OriginalEvidenceId]=[EvidenceId] AND [SupplementEvidenceId] IS NULL) OR ([SupplementEvidenceId] IS NOT NULL AND [SupplementEvidenceId]=[EvidenceId] AND [OriginalEvidenceId] IS NULL)");
                    table.ForeignKey(
                        name: "FK_CaseConclusionEvidence_CaseConclusions_ConclusionId",
                        column: x => x.ConclusionId,
                        principalTable: "CaseConclusions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CaseConclusionEvidence_Reports_SourceReportId",
                        column: x => x.SourceReportId,
                        principalTable: "Reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CasePublicationRecipients",
                columns: table => new
                {
                    PublicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CasePublicationRecipients", x => new { x.PublicationId, x.ReportId });
                    table.ForeignKey(
                        name: "FK_CasePublicationRecipients_CasePublications_PublicationId",
                        column: x => x.PublicationId,
                        principalTable: "CasePublications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CasePublicationRecipients_Reports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "Reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CaseReportLinkHistoryReports",
                columns: table => new
                {
                    HistoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseReportLinkHistoryReports", x => new { x.HistoryId, x.ReportId });
                    table.ForeignKey(
                        name: "FK_CaseReportLinkHistoryReports_CaseReportLinkHistory_HistoryId",
                        column: x => x.HistoryId,
                        principalTable: "CaseReportLinkHistory",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CaseReportLinkHistoryReports_Reports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "Reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CasePublicationEvidence",
                columns: table => new
                {
                    PublicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecipientReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvidenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalEvidenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SupplementEvidenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CasePublicationEvidence", x => new { x.PublicationId, x.RecipientReportId, x.EvidenceId });
                    table.CheckConstraint("CK_CasePublicationEvidence_TypedEvidence", "([OriginalEvidenceId] IS NOT NULL AND [OriginalEvidenceId]=[EvidenceId] AND [SupplementEvidenceId] IS NULL) OR ([SupplementEvidenceId] IS NOT NULL AND [SupplementEvidenceId]=[EvidenceId] AND [OriginalEvidenceId] IS NULL)");
                    table.ForeignKey(
                        name: "FK_CasePublicationEvidence_CasePublicationRecipients_PublicationId_RecipientReportId",
                        columns: x => new { x.PublicationId, x.RecipientReportId },
                        principalTable: "CasePublicationRecipients",
                        principalColumns: new[] { "PublicationId", "ReportId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CasePublicationEvidence_Reports_SourceReportId",
                        column: x => x.SourceReportId,
                        principalTable: "Reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AIDetections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcessingJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModelVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoadSectionVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Geometry = table.Column<Geometry>(type: "geometry", nullable: true),
                    DefectTypeCode = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: true),
                    Confidence = table.Column<decimal>(type: "decimal(6,5)", nullable: false),
                    EstimatedWidth = table.Column<decimal>(type: "decimal(12,3)", nullable: true),
                    EstimatedLength = table.Column<decimal>(type: "decimal(12,3)", nullable: true),
                    RawPayload = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIDetections", x => x.Id);
                    table.CheckConstraint("CK_AIDetections_Confidence", "[Confidence] >= 0 AND [Confidence] <= 1");
                    table.CheckConstraint("CK_AIDetections_EstimatedDimensions", "([EstimatedWidth] IS NULL OR [EstimatedWidth] >= 0) AND ([EstimatedLength] IS NULL OR [EstimatedLength] >= 0)");
                    table.CheckConstraint("CK_AIDetections_RawPayload_Json", "ISJSON([RawPayload]) = 1");
                    table.ForeignKey(
                        name: "FK_AIDetections_AIModelVersions_ModelVersionId",
                        column: x => x.ModelVersionId,
                        principalTable: "AIModelVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AIDetections_DefectTypes_DefectTypeCode",
                        column: x => x.DefectTypeCode,
                        principalTable: "DefectTypes",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AIDetections_RoadSectionVersions_RoadSectionVersionId",
                        column: x => x.RoadSectionVersionId,
                        principalTable: "RoadSectionVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Defects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RoadSectionVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceAIDetectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DefectTypeCode = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: false),
                    CauseCategoryCode = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: true),
                    Severity = table.Column<byte>(type: "tinyint", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    Geometry = table.Column<Geometry>(type: "geometry", nullable: true),
                    ReportedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Defects", x => x.Id);
                    table.CheckConstraint("CK_Defects_Severity", "[Severity] IS NULL OR [Severity] IN (0, 1, 2, 3, 4)");
                    table.CheckConstraint("CK_Defects_Status", "[Status] IS NULL OR [Status] IN (0, 1, 2, 3, 4, 5)");
                    table.ForeignKey(
                        name: "FK_Defects_AIDetections_SourceAIDetectionId",
                        column: x => x.SourceAIDetectionId,
                        principalTable: "AIDetections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Defects_CauseCategories_CauseCategoryCode",
                        column: x => x.CauseCategoryCode,
                        principalTable: "CauseCategories",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Defects_DefectTypes_DefectTypeCode",
                        column: x => x.DefectTypeCode,
                        principalTable: "DefectTypes",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Defects_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Defects_RoadSectionVersions_RoadSectionVersionId",
                        column: x => x.RoadSectionVersionId,
                        principalTable: "RoadSectionVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TrainingLabels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceKind = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportSourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AIDetectionSourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CurrentRevision = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingLabels", x => x.Id);
                    table.CheckConstraint("CK_TrainingLabels_Source", "([SourceKind]='REPORT' AND [ReportSourceId]=[SourceId] AND [AIDetectionSourceId] IS NULL) OR ([SourceKind]='AI_DETECTION' AND [AIDetectionSourceId]=[SourceId] AND [ReportSourceId] IS NULL)");
                    table.ForeignKey(
                        name: "FK_TrainingLabels_AIDetections_AIDetectionSourceId",
                        column: x => x.AIDetectionSourceId,
                        principalTable: "AIDetections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrainingLabels_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrainingLabels_Reports_ReportSourceId",
                        column: x => x.ReportSourceId,
                        principalTable: "Reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CaseConclusionDefects",
                columns: table => new
                {
                    ConclusionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseConclusionDefects", x => new { x.ConclusionId, x.DefectId });
                    table.ForeignKey(
                        name: "FK_CaseConclusionDefects_CaseConclusions_ConclusionId",
                        column: x => x.ConclusionId,
                        principalTable: "CaseConclusions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CaseConclusionDefects_Defects_DefectId",
                        column: x => x.DefectId,
                        principalTable: "Defects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CasePublicationDefects",
                columns: table => new
                {
                    PublicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CasePublicationDefects", x => new { x.PublicationId, x.DefectId });
                    table.ForeignKey(
                        name: "FK_CasePublicationDefects_CasePublications_PublicationId",
                        column: x => x.PublicationId,
                        principalTable: "CasePublications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CasePublicationDefects_Defects_DefectId",
                        column: x => x.DefectId,
                        principalTable: "Defects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RepairPackages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefectStatusAtAnchor = table.Column<byte>(type: "tinyint", nullable: true),
                    MutationRevision = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairPackages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepairPackages_Defects_DefectId",
                        column: x => x.DefectId,
                        principalTable: "Defects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairPackages_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SourceDecisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Source_Kind = table.Column<int>(type: "int", nullable: false),
                    Source_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Source_Version = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GeometryVersion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Decision = table.Column<int>(type: "int", nullable: false),
                    Classification_RoadSectionVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Classification_SegmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Classification_DefectTypeCode = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: true),
                    Classification_CauseCategoryCode = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: true),
                    Classification_Severity = table.Column<byte>(type: "tinyint", nullable: true),
                    TargetDefectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TargetDefectVersion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SupersedesDecisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExpectedPreviousDecisionVersion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DecidedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DecidedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AIDetectionSourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReportSourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceKind = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SourceDecisions", x => x.Id);
                    table.UniqueConstraint("AK_SourceDecisions_Id_SourceKind_SourceId_ProjectId", x => new { x.Id, x.SourceKind, x.SourceId, x.ProjectId });
                    table.CheckConstraint("CK_SourceDecisions_TypedSource", "[SourceKind] = [Source_Kind] AND [SourceId] = [Source_Id] AND (([SourceKind]=1 AND [ReportSourceId]=[SourceId] AND [ReportSourceId] IS NOT NULL AND [AIDetectionSourceId] IS NULL) OR ([SourceKind]=2 AND [AIDetectionSourceId]=[SourceId] AND [AIDetectionSourceId] IS NOT NULL AND [ReportSourceId] IS NULL))");
                    table.ForeignKey(
                        name: "FK_SourceDecisions_AIDetections_AIDetectionSourceId",
                        column: x => x.AIDetectionSourceId,
                        principalTable: "AIDetections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SourceDecisions_CauseCategories_Classification_CauseCategoryCode",
                        column: x => x.Classification_CauseCategoryCode,
                        principalTable: "CauseCategories",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SourceDecisions_DefectTypes_Classification_DefectTypeCode",
                        column: x => x.Classification_DefectTypeCode,
                        principalTable: "DefectTypes",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SourceDecisions_Defects_TargetDefectId",
                        column: x => x.TargetDefectId,
                        principalTable: "Defects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SourceDecisions_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SourceDecisions_Reports_ReportSourceId",
                        column: x => x.ReportSourceId,
                        principalTable: "Reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SourceDecisions_RoadSectionVersions_Classification_RoadSectionVersionId",
                        column: x => x.Classification_RoadSectionVersionId,
                        principalTable: "RoadSectionVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SourceDecisions_RoadSegments_Classification_SegmentId",
                        column: x => x.Classification_SegmentId,
                        principalTable: "RoadSegments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SourceDecisions_SourceDecisions_SupersedesDecisionId",
                        column: x => x.SupersedesDecisionId,
                        principalTable: "SourceDecisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SourceDecisions_Users_DecidedByUserId",
                        column: x => x.DecidedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TrainingLabelRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LabelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Revision = table.Column<int>(type: "int", nullable: false),
                    SourceVersion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileVersion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    X = table.Column<decimal>(type: "decimal(9,7)", precision: 9, scale: 7, nullable: false),
                    Y = table.Column<decimal>(type: "decimal(9,7)", precision: 9, scale: 7, nullable: false),
                    Width = table.Column<decimal>(type: "decimal(9,7)", precision: 9, scale: 7, nullable: false),
                    Height = table.Column<decimal>(type: "decimal(9,7)", precision: 9, scale: 7, nullable: false),
                    DefectTypeCode = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingLabelRevisions", x => x.Id);
                    table.UniqueConstraint("AK_TrainingLabelRevisions_Id_LabelId", x => new { x.Id, x.LabelId });
                    table.CheckConstraint("CK_TrainingLabelRevisions_Bbox", "[X]>=0 AND [Y]>=0 AND [Width]>0 AND [Height]>0 AND [X]+[Width]<=1 AND [Y]+[Height]<=1");
                    table.ForeignKey(
                        name: "FK_TrainingLabelRevisions_DefectTypes_DefectTypeCode",
                        column: x => x.DefectTypeCode,
                        principalTable: "DefectTypes",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrainingLabelRevisions_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrainingLabelRevisions_TrainingLabels_LabelId",
                        column: x => x.LabelId,
                        principalTable: "TrainingLabels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CandidateSourceHeads",
                columns: table => new
                {
                    SourceKind = table.Column<int>(type: "int", nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DecisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateSourceHeads", x => new { x.SourceKind, x.SourceId });
                    table.ForeignKey(
                        name: "FK_CandidateSourceHeads_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CandidateSourceHeads_SourceDecisions_DecisionId_SourceKind_SourceId_ProjectId",
                        columns: x => new { x.DecisionId, x.SourceKind, x.SourceId, x.ProjectId },
                        principalTable: "SourceDecisions",
                        principalColumns: new[] { "Id", "SourceKind", "SourceId", "ProjectId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DefectSourceLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceKind = table.Column<int>(type: "int", nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportSourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AIDetectionSourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DecisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DefectSourceLinks", x => x.Id);
                    table.CheckConstraint("CK_DefectSourceLinks_TypedSource", "([SourceKind]=1 AND [ReportSourceId]=[SourceId] AND [AIDetectionSourceId] IS NULL) OR ([SourceKind]=2 AND [AIDetectionSourceId]=[SourceId] AND [ReportSourceId] IS NULL)");
                    table.ForeignKey(
                        name: "FK_DefectSourceLinks_AIDetections_AIDetectionSourceId",
                        column: x => x.AIDetectionSourceId,
                        principalTable: "AIDetections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DefectSourceLinks_Defects_DefectId",
                        column: x => x.DefectId,
                        principalTable: "Defects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DefectSourceLinks_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DefectSourceLinks_Reports_ReportSourceId",
                        column: x => x.ReportSourceId,
                        principalTable: "Reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DefectSourceLinks_SourceDecisions_DecisionId_SourceKind_SourceId_ProjectId",
                        columns: x => new { x.DecisionId, x.SourceKind, x.SourceId, x.ProjectId },
                        principalTable: "SourceDecisions",
                        principalColumns: new[] { "Id", "SourceKind", "SourceId", "ProjectId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TrainingLabelReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LabelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Decision = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingLabelReviews", x => x.Id);
                    table.CheckConstraint("CK_TrainingLabelReviews_Decision", "[Decision] IN ('APPROVED','REJECTED')");
                    table.ForeignKey(
                        name: "FK_TrainingLabelReviews_TrainingLabelRevisions_RevisionId_LabelId",
                        columns: x => new { x.RevisionId, x.LabelId },
                        principalTable: "TrainingLabelRevisions",
                        principalColumns: new[] { "Id", "LabelId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrainingLabelReviews_Users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Anh02AiDetectionProvenance",
                columns: table => new
                {
                    DetectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResultId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceVideoFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceVideoFileVersion = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: false),
                    FrameFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FrameFileVersion = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: false),
                    DerivationHash = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: false),
                    TimestampMilliseconds = table.Column<long>(type: "bigint", nullable: false),
                    SourceDurationMilliseconds = table.Column<long>(type: "bigint", nullable: false),
                    SegmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BoxX = table.Column<decimal>(type: "decimal(12,9)", precision: 12, scale: 9, nullable: false),
                    BoxY = table.Column<decimal>(type: "decimal(12,9)", precision: 12, scale: 9, nullable: false),
                    BoxWidth = table.Column<decimal>(type: "decimal(12,9)", precision: 12, scale: 9, nullable: false),
                    BoxHeight = table.Column<decimal>(type: "decimal(12,9)", precision: 12, scale: 9, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Anh02AiDetectionProvenance", x => x.DetectionId);
                    table.CheckConstraint("CK_Anh02AiDetection_Box", "[BoxX]>=0 AND [BoxY]>=0 AND [BoxWidth]>0 AND [BoxHeight]>0 AND [BoxX]+[BoxWidth]<=1 AND [BoxY]+[BoxHeight]<=1");
                    table.CheckConstraint("CK_Anh02AiDetection_Time", "[TimestampMilliseconds]>=0 AND [SourceDurationMilliseconds]>0 AND [TimestampMilliseconds]<[SourceDurationMilliseconds]");
                    table.ForeignKey(
                        name: "FK_Anh02AiDetectionProvenance_AIDetections_DetectionId",
                        column: x => x.DetectionId,
                        principalTable: "AIDetections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Anh02AiDetectionProvenance_Files_FrameFileId",
                        column: x => x.FrameFileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Anh02AiDetectionProvenance_Files_SourceVideoFileId",
                        column: x => x.SourceVideoFileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Anh02AiDetectionProvenance_RoadSegments_SegmentId",
                        column: x => x.SegmentId,
                        principalTable: "RoadSegments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Anh02AiManifestFiles",
                columns: table => new
                {
                    RunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileVersion = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Anh02AiManifestFiles", x => new { x.RunId, x.FileId });
                    table.UniqueConstraint("AK_Anh02AiManifestFiles_RunId_FileId_FileVersion", x => new { x.RunId, x.FileId, x.FileVersion });
                    table.ForeignKey(
                        name: "FK_Anh02AiManifestFiles_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Anh02AiMockRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DatasetVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcessingJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModelVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RouteVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SegmentSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Stage = table.Column<string>(type: "varchar(24)", unicode: false, maxLength: 24, nullable: false),
                    Status = table.Column<string>(type: "varchar(24)", unicode: false, maxLength: 24, nullable: false),
                    FixtureVersion = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: false),
                    GeometryVersion = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: false),
                    ManifestHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    CanonicalManifest = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AnalysisRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResultId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ErrorCode = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: true),
                    LeaseOwner = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LeaseUntil = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ResultTimestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Anh02AiMockRuns", x => x.Id);
                    table.CheckConstraint("CK_Anh02AiMockRuns_Completion", "([Status]='SUCCEEDED' AND [ResultId] IS NOT NULL AND [CompletedAt] IS NOT NULL) OR ([Status]<>'SUCCEEDED' AND [ResultId] IS NULL)");
                    table.CheckConstraint("CK_Anh02AiMockRuns_Manifest", "ISJSON([CanonicalManifest])=1");
                    table.CheckConstraint("CK_Anh02AiMockRuns_Stage", "[Stage] IN ('VIDEO_ANALYSIS','DUPLICATE_MATCHING')");
                    table.CheckConstraint("CK_Anh02AiMockRuns_Status", "[Status] IN ('QUEUED','RUNNING','SUCCEEDED','FAILED')");
                    table.ForeignKey(
                        name: "FK_Anh02AiMockRuns_AIModelVersions_ModelVersionId",
                        column: x => x.ModelVersionId,
                        principalTable: "AIModelVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Anh02AiMockRuns_Anh02AiMockRuns_AnalysisRunId",
                        column: x => x.AnalysisRunId,
                        principalTable: "Anh02AiMockRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Anh02AiMockRuns_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Anh02AiMockRuns_RoadSegmentSets_SegmentSetId_RouteVersionId",
                        columns: x => new { x.SegmentSetId, x.RouteVersionId },
                        principalTable: "RoadSegmentSets",
                        principalColumns: new[] { "Id", "RoadSectionVersionId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Anh02AiMockRuns_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Anh02AiResultProvenance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcessingJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ManifestHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    ResultHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    CanonicalResult = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Anh02AiResultProvenance", x => x.Id);
                    table.UniqueConstraint("AK_Anh02AiResultProvenance_Id_RunId", x => new { x.Id, x.RunId });
                    table.CheckConstraint("CK_Anh02AiResult_Json", "ISJSON([CanonicalResult])=1");
                    table.ForeignKey(
                        name: "FK_Anh02AiResultProvenance_Anh02AiMockRuns_RunId",
                        column: x => x.RunId,
                        principalTable: "Anh02AiMockRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Anh02ExportJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    Format = table.Column<string>(type: "varchar(8)", unicode: false, maxLength: 8, nullable: false),
                    Status = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ArtifactId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ErrorCode = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: true),
                    LeaseToken = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LeaseUntil = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    NextAttemptAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Anh02ExportJobs", x => x.Id);
                    table.CheckConstraint("CK_Anh02ExportJob_Expiry", "([Status] <> 'SUCCEEDED' AND [ExpiresAt] IS NULL) OR ([Status]='SUCCEEDED' AND [ArtifactId] IS NOT NULL AND [CompletedAt] IS NOT NULL AND [ExpiresAt]=DATEADD(day,30,[CompletedAt]))");
                    table.CheckConstraint("CK_Anh02ExportJob_Format", "[Format] IN ('PDF','ZIP') AND ([Kind] <> 'TRAINING' OR [Format]='ZIP')");
                    table.CheckConstraint("CK_Anh02ExportJob_Kind", "[Kind] IN ('DOSSIER','TRAINING')");
                    table.CheckConstraint("CK_Anh02ExportJob_State", "[Status] IN ('QUEUED','RUNNING','SUCCEEDED','FAILED')");
                    table.ForeignKey(
                        name: "FK_Anh02ExportJobs_Anh02ExportSnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "Anh02ExportSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Anh02ExportJobs_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Anh02ExportJobs_Users_RequestedBy",
                        column: x => x.RequestedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Anh02GeneratedArtifacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExportJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Anh02GeneratedArtifacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Anh02GeneratedArtifacts_Anh02ExportJobs_ExportJobId",
                        column: x => x.ExportJobId,
                        principalTable: "Anh02ExportJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Anh02GeneratedArtifacts_Anh02ExportSnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "Anh02ExportSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Anh02GeneratedArtifacts_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BaselineCurrentPointers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RouteVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SegmentSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SegmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetBand = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SelectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaselineCurrentPointers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BaselineCurrentPointers_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BaselineSelectionItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BaselineSelectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DatasetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssessmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RouteVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SegmentSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SegmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetBand = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaselineSelectionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BaselineSelectionItems_BaselineSelections_BaselineSelectionId",
                        column: x => x.BaselineSelectionId,
                        principalTable: "BaselineSelections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DatasetAssessmentItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssessmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RouteVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SegmentSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SegmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetBand = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PositionStatus = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    QualityStatus = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CoverageStatus = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EvidenceJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatasetAssessmentItems", x => x.Id);
                    table.CheckConstraint("CK_Assessment_Status", "[PositionStatus] IN ('PASS','FAIL','UNKNOWN') AND [QualityStatus] IN ('PASS','FAIL','UNKNOWN') AND [CoverageStatus] IN ('PASS','FAIL','UNKNOWN')");
                    table.ForeignKey(
                        name: "FK_DatasetAssessmentItems_RoadSegments_SegmentId",
                        column: x => x.SegmentId,
                        principalTable: "RoadSegments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DatasetAssessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DatasetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MethodVersion = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ReviewedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatasetAssessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DatasetAssessments_Users_ReviewedBy",
                        column: x => x.ReviewedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DefectStatisticsSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefectVersion = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    ObligationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScopeHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    RoadSectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RouteVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationVersion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    From = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    To = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    OffsetFrom = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    OffsetTo = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    SharedPartId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SegmentIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QuantitiesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourceFactsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Provenance = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConfirmedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    SupersedesId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DefectStatisticsSources", x => x.Id);
                    table.CheckConstraint("CK_DefectStatisticsSources_Bounds", "[From]>=0 AND [To]>[From] AND [OffsetTo]>[OffsetFrom]");
                    table.CheckConstraint("CK_DefectStatisticsSources_Facts", "ISJSON([SegmentIdsJson])=1 AND ISJSON([QuantitiesJson])=1 AND ISJSON([SourceFactsJson])=1 AND [Provenance] COLLATE Latin1_General_100_BIN2 IN ('REAL_SOURCE','TEST_ONLY')");
                    table.ForeignKey(
                        name: "FK_DefectStatisticsSources_DefectStatisticsSources_SupersedesId",
                        column: x => x.SupersedesId,
                        principalTable: "DefectStatisticsSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DefectStatisticsSources_Defects_DefectId",
                        column: x => x.DefectId,
                        principalTable: "Defects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DefectStatisticsSources_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DefectStatisticsSources_RoadSectionVersions_RouteVersionId",
                        column: x => x.RouteVersionId,
                        principalTable: "RoadSectionVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DefectStatisticsSources_RoadSections_RoadSectionId",
                        column: x => x.RoadSectionId,
                        principalTable: "RoadSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DefectStatisticsSources_Users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DefectVerificationLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AIDetectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Action = table.Column<byte>(type: "tinyint", nullable: false),
                    BeforeSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AfterSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SeverityRuleVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FieldInspectionTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VerifiedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DefectVerificationLogs", x => x.Id);
                    table.CheckConstraint("CK_DefectVerificationLogs_Action", "[Action] IN (1, 2, 3, 4, 5)");
                    table.CheckConstraint("CK_DefectVerificationLogs_AfterSnapshot_Json", "[AfterSnapshot] IS NULL OR ISJSON([AfterSnapshot]) = 1");
                    table.CheckConstraint("CK_DefectVerificationLogs_BeforeSnapshot_Json", "[BeforeSnapshot] IS NULL OR ISJSON([BeforeSnapshot]) = 1");
                    table.CheckConstraint("CK_DefectVerificationLogs_ExactlyOneTarget", "([DefectId] IS NOT NULL AND [AIDetectionId] IS NULL) OR ([DefectId] IS NULL AND [AIDetectionId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_DefectVerificationLogs_AIDetections_AIDetectionId",
                        column: x => x.AIDetectionId,
                        principalTable: "AIDetections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DefectVerificationLogs_Defects_DefectId",
                        column: x => x.DefectId,
                        principalTable: "Defects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DefectVerificationLogs_SeverityRuleVersions_SeverityRuleVersionId",
                        column: x => x.SeverityRuleVersionId,
                        principalTable: "SeverityRuleVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DefectVerificationLogs_Users_VerifiedByUserId",
                        column: x => x.VerifiedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DerivedMeasurements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SurveyDataVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoadSectionVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SampleId = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    MeasurementType = table.Column<byte>(type: "tinyint", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(19,6)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    UncertaintyEstimate = table.Column<decimal>(type: "decimal(19,6)", nullable: true),
                    SourceType = table.Column<byte>(type: "tinyint", nullable: false),
                    AlgorithmVersion = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    ComputedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DerivedMeasurements", x => x.Id);
                    table.CheckConstraint("CK_DerivedMeasurements_MeasurementType", "[MeasurementType] IN (1, 2, 3)");
                    table.CheckConstraint("CK_DerivedMeasurements_SourceType", "[SourceType] IN (1, 2, 3, 4)");
                    table.CheckConstraint("CK_DerivedMeasurements_Status", "[Status] IN (1, 2, 3)");
                    table.CheckConstraint("CK_DerivedMeasurements_UncertaintyEstimate", "[UncertaintyEstimate] IS NULL OR [UncertaintyEstimate] >= 0");
                    table.CheckConstraint("CK_DerivedMeasurements_Unit", "LOWER([Unit]) IN ('mm', 'cm', 'm')");
                    table.CheckConstraint("CK_DerivedMeasurements_Value", "[Value] >= 0");
                    table.ForeignKey(
                        name: "FK_DerivedMeasurements_RoadSectionVersions_RoadSectionVersionId",
                        column: x => x.RoadSectionVersionId,
                        principalTable: "RoadSectionVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FieldInspectionAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FieldInspectionTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedToUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    EndedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldInspectionAssignments", x => x.Id);
                    table.CheckConstraint("CK_FieldInspectionAssignments_State", "([Status] = 1 AND [EndedAt] IS NULL AND [Reason] IS NULL) OR ([Status] IN (2, 3) AND [EndedAt] IS NOT NULL AND LEN(LTRIM(RTRIM([Reason]))) > 0)");
                    table.CheckConstraint("CK_FieldInspectionAssignments_Status", "[Status] IN (1, 2, 3)");
                    table.CheckConstraint("CK_FieldInspectionAssignments_TimestampOrder", "[EndedAt] IS NULL OR [EndedAt] >= [AssignedAt]");
                    table.ForeignKey(
                        name: "FK_FieldInspectionAssignments_Users_AssignedByUserId",
                        column: x => x.AssignedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldInspectionAssignments_Users_AssignedToUserId",
                        column: x => x.AssignedToUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FieldInspectionEvidenceLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CaptureOriginId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DeclaredChecksum = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    MediaType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CaptureFactsJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldInspectionEvidenceLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FieldInspectionEvidenceLinks_FieldInspectionAssignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "FieldInspectionAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldInspectionEvidenceLinks_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldInspectionEvidenceLinks_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FieldInspectionEvidenceReuseDecisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceEvidenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceKind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FileChecksum = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProvenanceJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldInspectionEvidenceReuseDecisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FieldInspectionEvidenceReuseDecisions_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldInspectionEvidenceReuseDecisions_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldInspectionEvidenceReuseDecisions_Users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FieldInspectionLocationProofs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FactsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VerificationState = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldInspectionLocationProofs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FieldInspectionLocationProofs_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FieldInspectionOperationOrigins",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    SchemaVersion = table.Column<int>(type: "int", nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    OriginalActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EffectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServerReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldInspectionOperationOrigins", x => x.Id);
                    table.CheckConstraint("CK_FieldInspectionOperationOrigins_Kind", "[Kind] IN ('FIELD_START','FIELD_SUBMISSION','FIELD_ACCEPT','REPAIR_ASSESSMENT','REPAIR_EXECUTION_START','REPAIR_EXECUTION_FINISH') AND [SchemaVersion]=1");
                    table.ForeignKey(
                        name: "FK_FieldInspectionOperationOrigins_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldInspectionOperationOrigins_Users_OriginalActorId",
                        column: x => x.OriginalActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FieldInspectionReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Decision = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReceiptActivation = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldInspectionReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FieldInspectionReviews_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldInspectionReviews_Users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FieldInspectionSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Purpose = table.Column<byte>(type: "tinyint", nullable: false),
                    FieldInspectionTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoadSectionVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SurveyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SessionCode = table.Column<string>(type: "nvarchar(80)", nullable: false),
                    InspectorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InspectorName = table.Column<string>(type: "nvarchar(200)", nullable: false),
                    ConductedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    WeatherCondition = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    Method = table.Column<string>(type: "nvarchar(200)", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    EvidenceFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldInspectionSessions", x => x.Id);
                    table.CheckConstraint("CK_FieldInspectionSessions_Purpose", "[Purpose] IN (1, 2, 3, 4, 5)");
                    table.CheckConstraint("CK_FieldInspectionSessions_PurposeScope", "([Purpose] = 1 AND [FieldInspectionTaskId] IS NOT NULL AND [SurveyId] IS NOT NULL AND [InspectorUserId] IS NOT NULL) OR ([Purpose] = 2 AND [FieldInspectionTaskId] IS NULL) OR ([Purpose] IN (3,4,5) AND [FieldInspectionTaskId] IS NOT NULL AND [InspectorUserId] IS NOT NULL)");
                    table.CheckConstraint("CK_FieldInspectionSessions_Status", "[Status] IN (1, 2, 3, 4)");
                    table.ForeignKey(
                        name: "FK_FieldInspectionSessions_Files_EvidenceFileId",
                        column: x => x.EvidenceFileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldInspectionSessions_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldInspectionSessions_RoadSectionVersions_RoadSectionVersionId",
                        column: x => x.RoadSectionVersionId,
                        principalTable: "RoadSectionVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldInspectionSessions_Users_InspectorUserId",
                        column: x => x.InspectorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FieldInspectionSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RootId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Revision = table.Column<int>(type: "int", nullable: false),
                    AssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartOriginId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperationOriginId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    OriginalActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServerReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Readiness = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MissingReasonsJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldInspectionSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FieldInspectionSubmissions_FieldInspectionAssignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "FieldInspectionAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldInspectionSubmissions_FieldInspectionOperationOrigins_OperationOriginId",
                        column: x => x.OperationOriginId,
                        principalTable: "FieldInspectionOperationOrigins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldInspectionSubmissions_FieldInspectionSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "FieldInspectionSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldInspectionSubmissions_FieldInspectionSubmissions_ParentId",
                        column: x => x.ParentId,
                        principalTable: "FieldInspectionSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldInspectionSubmissions_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldInspectionSubmissions_Users_OriginalActorId",
                        column: x => x.OriginalActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FieldInspectionTaskEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FactsJson = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "{}"),
                    LocationImpactId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LocationImpactDecisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldInspectionTaskEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FieldInspectionTaskEvents_FieldInspectionAssignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "FieldInspectionAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldInspectionTaskEvents_GeometryLocationImpactDecisions_LocationImpactDecisionId",
                        column: x => x.LocationImpactDecisionId,
                        principalTable: "GeometryLocationImpactDecisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldInspectionTaskEvents_GeometryLocationImpacts_LocationImpactId",
                        column: x => x.LocationImpactId,
                        principalTable: "GeometryLocationImpacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldInspectionTaskEvents_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldInspectionTaskEvents_Users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FieldInspectionTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    TaskCode = table.Column<string>(type: "nvarchar(80)", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RepairItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SurveyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LifecycleVersion = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    TaskMode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false, defaultValue: "MEASURE_ONLY"),
                    SourceKind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "SURVEY"),
                    SegmentSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LayoutRevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SlabId = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    MapPublicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CrsProfileRevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Purpose = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)1),
                    RoadSectionVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequiredMeasurementType = table.Column<byte>(type: "tinyint", nullable: false),
                    MeasurementScope = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Instructions = table.Column<string>(type: "nvarchar(1000)", nullable: true),
                    MissingInformation = table.Column<string>(type: "nvarchar(1000)", nullable: true),
                    DueAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    AssignedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReviewDecision = table.Column<byte>(type: "tinyint", nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    ReviewReason = table.Column<string>(type: "nvarchar(1000)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldInspectionTasks", x => x.Id);
                    table.CheckConstraint("CK_FieldInspectionTasks_MeasurementScope_Json", "ISJSON([MeasurementScope]) = 1 AND LEFT(LTRIM([MeasurementScope]), 1) = '{'");
                    table.CheckConstraint("CK_FieldInspectionTasks_RepairMode", "([TaskMode]='MEASURE_ONLY' AND [RepairItemId] IS NULL) OR ([LifecycleVersion]=2 AND [TaskMode] IN('NORMAL','CONDITIONAL_FT') AND [RepairItemId] IS NOT NULL)");
                    table.CheckConstraint("CK_FieldInspectionTasks_ReviewDecision", "([ReviewDecision] IS NULL AND [ReviewedByUserId] IS NULL AND [ReviewedAt] IS NULL) OR ([ReviewDecision] IS NOT NULL AND [ReviewedByUserId] IS NOT NULL AND [ReviewedAt] IS NOT NULL AND [Status] = 7)");
                    table.CheckConstraint("CK_FieldInspectionTasks_Source", "([LifecycleVersion]=1 AND [SourceKind]='SURVEY' AND [SurveyId] IS NOT NULL) OR ([LifecycleVersion]=2 AND [Purpose] IN(3,4,5) AND (([SourceKind]='SURVEY' AND [SurveyId] IS NOT NULL) OR ([SourceKind]='REPORTER' AND [SurveyId] IS NULL)))");
                    table.CheckConstraint("CK_FieldInspectionTasks_Status", "[Status] IN (1, 2, 3, 4, 5, 6, 7, 8)");
                    table.ForeignKey(
                        name: "FK_FieldInspectionTasks_CrsProfileRevisions_CrsProfileRevisionId",
                        column: x => x.CrsProfileRevisionId,
                        principalTable: "CrsProfileRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldInspectionTasks_Defects_DefectId",
                        column: x => x.DefectId,
                        principalTable: "Defects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldInspectionTasks_GeometryMapPublications_MapPublicationId",
                        column: x => x.MapPublicationId,
                        principalTable: "GeometryMapPublications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldInspectionTasks_PavementLayoutRevisions_LayoutRevisionId",
                        column: x => x.LayoutRevisionId,
                        principalTable: "PavementLayoutRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldInspectionTasks_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldInspectionTasks_RoadSectionVersions_RoadSectionVersionId",
                        column: x => x.RoadSectionVersionId,
                        principalTable: "RoadSectionVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldInspectionTasks_RoadSegmentSets_SegmentSetId",
                        column: x => x.SegmentSetId,
                        principalTable: "RoadSegmentSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldInspectionTasks_Users_AssignedByUserId",
                        column: x => x.AssignedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldInspectionTasks_Users_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FieldTaskStartOrigins",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperationOriginId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperationKind = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    OriginalActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClaimedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    MonotonicMilliseconds = table.Column<long>(type: "bigint", nullable: true),
                    BootId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ServerReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    VerifiedOriginalAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TimeProvenance = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    RouteVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SegmentSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LayoutRevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MapPublicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CrsProfileRevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SlabId = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    LocationPolicyVersion = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    ClaimEvidenceJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldTaskStartOrigins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FieldTaskStartOrigins_CrsProfileRevisions_CrsProfileRevisionId",
                        column: x => x.CrsProfileRevisionId,
                        principalTable: "CrsProfileRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldTaskStartOrigins_FieldInspectionAssignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "FieldInspectionAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldTaskStartOrigins_FieldInspectionOperationOrigins_OperationOriginId",
                        column: x => x.OperationOriginId,
                        principalTable: "FieldInspectionOperationOrigins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldTaskStartOrigins_FieldInspectionTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "FieldInspectionTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldTaskStartOrigins_GeometryMapPublications_MapPublicationId",
                        column: x => x.MapPublicationId,
                        principalTable: "GeometryMapPublications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldTaskStartOrigins_PavementLayoutRevisions_LayoutRevisionId",
                        column: x => x.LayoutRevisionId,
                        principalTable: "PavementLayoutRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldTaskStartOrigins_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldTaskStartOrigins_RoadSectionVersions_RouteVersionId",
                        column: x => x.RouteVersionId,
                        principalTable: "RoadSectionVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldTaskStartOrigins_RoadSegmentSets_SegmentSetId",
                        column: x => x.SegmentSetId,
                        principalTable: "RoadSegmentSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldTaskStartOrigins_Users_OriginalActorId",
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
                name: "RepairExecutionAuthorizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CrewId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IssuedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IssuedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Permission = table.Column<byte>(type: "tinyint", nullable: false),
                    LocationVersion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PolicyRevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirstStartOriginId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FirstStartPayloadHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    FirstStartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    VerifiedStartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FirstServerReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TimeProvenance = table.Column<byte>(type: "tinyint", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairExecutionAuthorizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepairExecutionAuthorizations_Defects_DefectId",
                        column: x => x.DefectId,
                        principalTable: "Defects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairExecutionAuthorizations_FieldInspectionAssignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "FieldInspectionAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairExecutionAuthorizations_FieldInspectionTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "FieldInspectionTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairExecutionAuthorizations_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairExecutionAuthorizations_RepairPolicyRevisions_PolicyRevisionId",
                        column: x => x.PolicyRevisionId,
                        principalTable: "RepairPolicyRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairExecutionAuthorizations_Users_CrewId",
                        column: x => x.CrewId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairExecutionAuthorizations_Users_IssuedBy",
                        column: x => x.IssuedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Flights",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SurveyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DroneDeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OperatorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    EndedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    FlightNo = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Flights", x => x.Id);
                    table.CheckConstraint("CK_Flights_TimestampOrder", "[EndedAt] IS NULL OR [EndedAt] >= [StartedAt]");
                    table.ForeignKey(
                        name: "FK_Flights_DroneDevices_DroneDeviceId",
                        column: x => x.DroneDeviceId,
                        principalTable: "DroneDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Flights_Users_OperatorUserId",
                        column: x => x.OperatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GroundTruthMeasurements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FieldInspectionSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SampleId = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    RoadSectionVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SurveyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DefectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MeasurementType = table.Column<byte>(type: "tinyint", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(19,6)", nullable: true),
                    ValueState = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "KNOWN"),
                    Dimension = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "LENGTH"),
                    UnknownReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    LocationState = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "CAPTURED"),
                    LocationReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Unit = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    Location = table.Column<Point>(type: "geography", nullable: true),
                    InstrumentName = table.Column<string>(type: "nvarchar(150)", nullable: false),
                    InstrumentReference = table.Column<string>(type: "nvarchar(150)", nullable: true),
                    MeasurementMethod = table.Column<string>(type: "nvarchar(500)", nullable: false),
                    MeasuredBy = table.Column<string>(type: "nvarchar(200)", nullable: false),
                    MeasuredAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    EvidenceFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroundTruthMeasurements", x => x.Id);
                    table.CheckConstraint("CK_GroundTruthMeasurements_EvidenceOrReason", "[EvidenceFileId] IS NOT NULL OR [ValueState]='UNKNOWN' OR [LocationState]='UNKNOWN' OR ([Notes] IS NOT NULL AND LEN(LTRIM(RTRIM([Notes]))) > 0)");
                    table.CheckConstraint("CK_GroundTruthMeasurements_Location", "([Location] IS NOT NULL AND [LocationState]='CAPTURED' AND [Location].STSrid=4326 AND [Location].STIsEmpty()=0) OR ([Location] IS NULL AND [LocationState]='UNKNOWN' AND [LocationReason] IS NOT NULL AND LEN(LTRIM(RTRIM([LocationReason])))>0)");
                    table.CheckConstraint("CK_GroundTruthMeasurements_MeasurementType", "[MeasurementType] IN (1, 2, 3, 4)");
                    table.CheckConstraint("CK_GroundTruthMeasurements_Unit", "([MeasurementType] IN (1,2,3) AND [Dimension]='LENGTH' AND LOWER([Unit]) IN ('mm','cm','m')) OR ([MeasurementType]=4 AND [Dimension]='AREA' AND [Unit]=N'm²')");
                    table.CheckConstraint("CK_GroundTruthMeasurements_Value", "([ValueState]='KNOWN' AND [Value] IS NOT NULL AND [Value]>=0) OR ([ValueState]='UNKNOWN' AND [Value] IS NULL AND [UnknownReason] IS NOT NULL AND LEN(LTRIM(RTRIM([UnknownReason])))>0)");
                    table.ForeignKey(
                        name: "FK_GroundTruthMeasurements_Defects_DefectId",
                        column: x => x.DefectId,
                        principalTable: "Defects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GroundTruthMeasurements_FieldInspectionSessions_FieldInspectionSessionId",
                        column: x => x.FieldInspectionSessionId,
                        principalTable: "FieldInspectionSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GroundTruthMeasurements_Files_EvidenceFileId",
                        column: x => x.EvidenceFileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GroundTruthMeasurements_RoadSectionVersions_RoadSectionVersionId",
                        column: x => x.RoadSectionVersionId,
                        principalTable: "RoadSectionVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MeasurementValidationSamples",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ValidationRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroundTruthMeasurementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DerivedMeasurementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SignedError = table.Column<decimal>(type: "decimal(19,6)", nullable: false),
                    AbsoluteError = table.Column<decimal>(type: "decimal(19,6)", nullable: false),
                    InclusionStatus = table.Column<byte>(type: "tinyint", nullable: false),
                    ExclusionReason = table.Column<string>(type: "nvarchar(500)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeasurementValidationSamples", x => x.Id);
                    table.CheckConstraint("CK_MeasurementValidationSamples_InclusionStatus", "[InclusionStatus] IN (1, 2, 3)");
                    table.CheckConstraint("CK_MeasurementValidationSamples_Reason", "[InclusionStatus] = 1 OR LEN(LTRIM(RTRIM([ExclusionReason]))) > 0");
                    table.ForeignKey(
                        name: "FK_MeasurementValidationSamples_DerivedMeasurements_DerivedMeasurementId",
                        column: x => x.DerivedMeasurementId,
                        principalTable: "DerivedMeasurements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeasurementValidationSamples_GroundTruthMeasurements_GroundTruthMeasurementId",
                        column: x => x.GroundTruthMeasurementId,
                        principalTable: "GroundTruthMeasurements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeasurementValidationSamples_ValidationRuns_ValidationRunId",
                        column: x => x.ValidationRunId,
                        principalTable: "ValidationRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LD06ActionEvidence",
                columns: table => new
                {
                    ActionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Checksum = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LD06ActionEvidence", x => new { x.ActionId, x.FileId });
                    table.ForeignKey(
                        name: "FK_LD06ActionEvidence_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LD06LifecycleActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<byte>(type: "tinyint", nullable: false),
                    SourceActionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DefectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LinkedDefectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ObligationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReceivingProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PriorRepairDecisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    At = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ScopeHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    FactsJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LD06LifecycleActions", x => x.Id);
                    table.CheckConstraint("CK_LD06LifecycleActions_Kind", "[Kind] BETWEEN 1 AND 7 AND ISJSON([FactsJson])=1");
                    table.ForeignKey(
                        name: "FK_LD06LifecycleActions_Defects_DefectId",
                        column: x => x.DefectId,
                        principalTable: "Defects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LD06LifecycleActions_Defects_LinkedDefectId",
                        column: x => x.LinkedDefectId,
                        principalTable: "Defects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LD06LifecycleActions_LD06LifecycleActions_SourceActionId",
                        column: x => x.SourceActionId,
                        principalTable: "LD06LifecycleActions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LD06LifecycleActions_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LD06LifecycleActions_Projects_ReceivingProjectId",
                        column: x => x.ReceivingProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LD06LifecycleActions_Users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ObligationResponsibilities",
                columns: table => new
                {
                    ObligationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AcceptanceActionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObligationResponsibilities", x => x.ObligationId);
                    table.ForeignKey(
                        name: "FK_ObligationResponsibilities_LD06LifecycleActions_AcceptanceActionId",
                        column: x => x.AcceptanceActionId,
                        principalTable: "LD06LifecycleActions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ObligationResponsibilities_Projects_CurrentProjectId",
                        column: x => x.CurrentProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ObligationResponsibilities_Projects_OriginProjectId",
                        column: x => x.OriginProjectId,
                        principalTable: "Projects",
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
                        name: "FK_OfflineOperationBindings_Users_OriginalActorId",
                        column: x => x.OriginalActorId,
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
                name: "ProcessingAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcessingJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttemptNo = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    EndedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    ErrorType = table.Column<byte>(type: "tinyint", nullable: true),
                    WorkerReference = table.Column<string>(type: "varchar(120)", unicode: false, maxLength: 120, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessingAttempts", x => x.Id);
                    table.CheckConstraint("CK_ProcessingAttempts_AttemptNo", "[AttemptNo] > 0");
                    table.CheckConstraint("CK_ProcessingAttempts_ErrorType", "[ErrorType] IS NULL OR [ErrorType] IN (1, 2, 3)");
                    table.CheckConstraint("CK_ProcessingAttempts_TimestampOrder", "[EndedAt] IS NULL OR [EndedAt] >= [StartedAt]");
                });

            migrationBuilder.CreateTable(
                name: "ProcessingBlocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SurveyDataVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BlockNo = table.Column<int>(type: "int", nullable: false),
                    RangeMetadata = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessingBlocks", x => x.Id);
                    table.CheckConstraint("CK_ProcessingBlocks_BlockNo", "[BlockNo] > 0");
                    table.CheckConstraint("CK_ProcessingBlocks_RangeMetadata_JsonObject", "ISJSON([RangeMetadata]) = 1 AND LEFT(LTRIM([RangeMetadata]), 1) = '{'");
                });

            migrationBuilder.CreateTable(
                name: "ProcessingJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcessingBlockId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModelVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ManifestHash = table.Column<string>(type: "char(64)", unicode: false, nullable: false, defaultValue: ""),
                    ManifestJson = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "{}"),
                    Mode = table.Column<string>(type: "varchar(8)", unicode: false, maxLength: 8, nullable: false, defaultValue: ""),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    ErrorCode = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessingJobs", x => x.Id);
                    table.CheckConstraint("CK_ProcessingJobs_Status", "[Status] IN (1, 2, 3, 4, 5, 6)");
                    table.CheckConstraint("CK_ProcessingJobs_TimestampOrder", "[CompletedAt] IS NULL OR [StartedAt] IS NULL OR [CompletedAt] >= [StartedAt]");
                    table.ForeignKey(
                        name: "FK_ProcessingJobs_AIModelVersions_ModelVersionId",
                        column: x => x.ModelVersionId,
                        principalTable: "AIModelVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProcessingJobs_ProcessingBlocks_ProcessingBlockId",
                        column: x => x.ProcessingBlockId,
                        principalTable: "ProcessingBlocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectLifecycleHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<byte>(type: "tinyint", nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecordedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ObligationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GrantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReceiverId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OperationalClosureId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DefectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LinkedDefectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    BasisReference = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    AuthoritySourceReference = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    SourceDisposition = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    FactsJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectLifecycleHistory", x => x.Id);
                    table.UniqueConstraint("AK_ProjectLifecycleHistory_Id_ProjectId", x => new { x.Id, x.ProjectId });
                    table.CheckConstraint("CK_ProjectLifecycleHistory_Facts", "ISJSON([FactsJson])=1");
                    table.CheckConstraint("CK_ProjectLifecycleHistory_Source", "[Kind] IN (1,2,3,4,5,6,7,8) AND [SourceDisposition] IN ('CANDIDATE','TARGET_CONFIRMED') AND ([SourceDisposition]='CANDIDATE' OR LEN([AuthoritySourceReference])>0)");
                    table.ForeignKey(
                        name: "FK_ProjectLifecycleHistory_Defects_DefectId",
                        column: x => x.DefectId,
                        principalTable: "Defects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectLifecycleHistory_Defects_LinkedDefectId",
                        column: x => x.LinkedDefectId,
                        principalTable: "Defects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectLifecycleHistory_ProjectLifecycleHistory_OperationalClosureId_ProjectId",
                        columns: x => new { x.OperationalClosureId, x.ProjectId },
                        principalTable: "ProjectLifecycleHistory",
                        principalColumns: new[] { "Id", "ProjectId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectLifecycleHistory_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectLifecycleHistory_Users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectLifecycleHistory_Users_ReceiverId",
                        column: x => x.ReceiverId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QualityChecks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Scope = table.Column<byte>(type: "tinyint", nullable: false),
                    ExecutionStage = table.Column<byte>(type: "tinyint", nullable: false),
                    SurveyFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SurveyDataVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CheckType = table.Column<byte>(type: "tinyint", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    MeasuredValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Threshold = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CheckedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    CheckedBy = table.Column<byte>(type: "tinyint", nullable: false),
                    InitiatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualityChecks", x => x.Id);
                    table.CheckConstraint("CK_QualityChecks_CheckedBy", "[CheckedBy] IN (1, 2)");
                    table.CheckConstraint("CK_QualityChecks_CheckType", "[CheckType] IN (1, 2, 3, 4, 5, 6, 7, 8, 9)");
                    table.CheckConstraint("CK_QualityChecks_ExactlyOneTarget", "([Scope] = 1 AND [SurveyFileId] IS NOT NULL AND [SurveyDataVersionId] IS NULL) OR ([Scope] = 2 AND [SurveyFileId] IS NULL AND [SurveyDataVersionId] IS NOT NULL)");
                    table.CheckConstraint("CK_QualityChecks_ExecutionStage", "[ExecutionStage] IN (1, 2)");
                    table.CheckConstraint("CK_QualityChecks_MeasuredValue_Json", "[MeasuredValue] IS NULL OR ISJSON([MeasuredValue]) = 1");
                    table.CheckConstraint("CK_QualityChecks_Scope", "[Scope] IN (1, 2)");
                    table.CheckConstraint("CK_QualityChecks_StageActor", "([ExecutionStage] = 1 AND [CheckedBy] = 1) OR ([ExecutionStage] = 2 AND [CheckedBy] = 2 AND [InitiatedByUserId] IS NULL)");
                    table.CheckConstraint("CK_QualityChecks_Status", "[Status] IN (1, 2, 3, 4)");
                    table.CheckConstraint("CK_QualityChecks_Threshold_Json", "[Threshold] IS NULL OR ISJSON([Threshold]) = 1");
                    table.ForeignKey(
                        name: "FK_QualityChecks_Users_InitiatedByUserId",
                        column: x => x.InitiatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RepairActualScopes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PhysicalRoadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationVersion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RouteLabel = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    From = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    To = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    OffsetFrom = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    OffsetTo = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    ObligationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairActualScopes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepairActualScopes_RoadSections_PhysicalRoadId",
                        column: x => x.PhysicalRoadId,
                        principalTable: "RoadSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RepairAssessmentEvidence",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CaptureOriginId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Purpose = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ChecksumSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    MediaType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CapturedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    StateAtIntake = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    FileVersion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ActualUploaderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OriginalActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReuseDecisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CaptureFactsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AssessmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairAssessmentEvidence", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepairAssessmentEvidence_FieldInspectionEvidenceReuseDecisions_ReuseDecisionId",
                        column: x => x.ReuseDecisionId,
                        principalTable: "FieldInspectionEvidenceReuseDecisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairAssessmentEvidence_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairAssessmentEvidence_Users_ActualUploaderId",
                        column: x => x.ActualUploaderId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairAssessmentEvidence_Users_OriginalActorId",
                        column: x => x.OriginalActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RepairAssessmentMeasurements",
                columns: table => new
                {
                    MeasurementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssessmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairAssessmentMeasurements", x => new { x.AssessmentId, x.MeasurementId });
                    table.ForeignKey(
                        name: "FK_RepairAssessmentMeasurements_GroundTruthMeasurements_MeasurementId",
                        column: x => x.MeasurementId,
                        principalTable: "GroundTruthMeasurements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RepairAttemptEvidence",
                columns: table => new
                {
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Purpose = table.Column<byte>(type: "tinyint", nullable: false),
                    AttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileVersion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Hash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Verified = table.Column<bool>(type: "bit", nullable: false),
                    Related = table.Column<bool>(type: "bit", nullable: false),
                    SourceKind = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CapturedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReuseDecisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReusedSource = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairAttemptEvidence", x => new { x.AttemptId, x.FileId, x.Purpose });
                    table.ForeignKey(
                        name: "FK_RepairAttemptEvidence_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RepairAttemptReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BindingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IntakeLinkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmissionContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PlanHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ChecklistVersion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<byte>(type: "tinyint", nullable: false),
                    Decision = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    At = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EvidenceSufficient = table.Column<bool>(type: "bit", nullable: false),
                    ExecutionAuthority = table.Column<byte>(type: "tinyint", nullable: false),
                    AssessmentFactsJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairAttemptReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepairAttemptReviews_FieldInspectionSubmissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "FieldInspectionSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairAttemptReviews_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairAttemptReviews_Users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RepairAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayloadHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObligationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CrewId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LocationVersion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PolicyRevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Performed = table.Column<bool>(type: "bit", nullable: false),
                    UnperformedReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FinishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ServerReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TimeProvenance = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepairAttempts_Defects_DefectId",
                        column: x => x.DefectId,
                        principalTable: "Defects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairAttempts_FieldInspectionAssignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "FieldInspectionAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairAttempts_FieldInspectionTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "FieldInspectionTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairAttempts_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairAttempts_Users_CrewId",
                        column: x => x.CrewId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RepairAttemptSubmissionLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BindingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FormalRootSubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreviousLinkId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExecutionFinishId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SubmissionContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FormalRootServerReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReviewClockId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalReviewDueAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairAttemptSubmissionLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepairAttemptSubmissionLinks_DeadlineClocks_ReviewClockId",
                        column: x => x.ReviewClockId,
                        principalTable: "DeadlineClocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairAttemptSubmissionLinks_FieldInspectionSubmissions_FormalRootSubmissionId",
                        column: x => x.FormalRootSubmissionId,
                        principalTable: "FieldInspectionSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairAttemptSubmissionLinks_FieldInspectionSubmissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "FieldInspectionSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairAttemptSubmissionLinks_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairAttemptSubmissionLinks_RepairAttemptSubmissionLinks_PreviousLinkId",
                        column: x => x.PreviousLinkId,
                        principalTable: "RepairAttemptSubmissionLinks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairAttemptSubmissionLinks_RepairAttempts_AttemptId",
                        column: x => x.AttemptId,
                        principalTable: "RepairAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RepairCorrectionEvidence",
                columns: table => new
                {
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DecisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileVersion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Hash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Purpose = table.Column<byte>(type: "tinyint", nullable: false),
                    Verified = table.Column<bool>(type: "bit", nullable: false),
                    Related = table.Column<bool>(type: "bit", nullable: false),
                    SourceKind = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CapturedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReuseDecisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReusedSource = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairCorrectionEvidence", x => new { x.DecisionId, x.FileId });
                    table.ForeignKey(
                        name: "FK_RepairCorrectionEvidence_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RepairDangerAcknowledgements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarningId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    At = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    AfterOriginalDue = table.Column<bool>(type: "bit", nullable: false),
                    MonitoringId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairDangerAcknowledgements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepairDangerAcknowledgements_Users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RepairDangerWarnings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResponsibleActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServerReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    OriginalAcknowledgementDueAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    MonitoringId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairDangerWarnings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepairDangerWarnings_Users_ResponsibleActorId",
                        column: x => x.ResponsibleActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RepairDecisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObligationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Mode = table.Column<byte>(type: "tinyint", nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<byte>(type: "tinyint", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    At = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SupersedesDecisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Result = table.Column<byte>(type: "tinyint", nullable: false),
                    Basis_Text = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PreviousObligationHeadDecisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairDecisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepairDecisions_Defects_DefectId",
                        column: x => x.DefectId,
                        principalTable: "Defects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairDecisions_RepairDecisions_PreviousObligationHeadDecisionId",
                        column: x => x.PreviousObligationHeadDecisionId,
                        principalTable: "RepairDecisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairDecisions_RepairDecisions_SupersedesDecisionId",
                        column: x => x.SupersedesDecisionId,
                        principalTable: "RepairDecisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairDecisions_Users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RepairEligibilityAssessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BindingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssessmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoadSectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PolicyRevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PolicyContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RoadHandover = table.Column<byte>(type: "tinyint", nullable: false),
                    Coverage = table.Column<byte>(type: "tinyint", nullable: false),
                    SourceMapping = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SourceFactsHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EvaluatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    MissingReasonsJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairEligibilityAssessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepairEligibilityAssessments_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairEligibilityAssessments_RepairPolicyRevisions_PolicyRevisionId",
                        column: x => x.PolicyRevisionId,
                        principalTable: "RepairPolicyRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairEligibilityAssessments_RoadSections_RoadSectionId",
                        column: x => x.RoadSectionId,
                        principalTable: "RoadSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RepairEligibilityHandoverSources",
                columns: table => new
                {
                    HandoverDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EligibilityAssessmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FactsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairEligibilityHandoverSources", x => new { x.EligibilityAssessmentId, x.HandoverDocumentId });
                    table.ForeignKey(
                        name: "FK_RepairEligibilityHandoverSources_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairEligibilityHandoverSources_HandoverDocuments_HandoverDocumentId",
                        column: x => x.HandoverDocumentId,
                        principalTable: "HandoverDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairEligibilityHandoverSources_RepairEligibilityAssessments_EligibilityAssessmentId",
                        column: x => x.EligibilityAssessmentId,
                        principalTable: "RepairEligibilityAssessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RepairEligibilityWarrantySources",
                columns: table => new
                {
                    WarrantyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EligibilityAssessmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FactsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourceDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairEligibilityWarrantySources", x => new { x.EligibilityAssessmentId, x.WarrantyId });
                    table.ForeignKey(
                        name: "FK_RepairEligibilityWarrantySources_Files_SourceDocumentId",
                        column: x => x.SourceDocumentId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairEligibilityWarrantySources_RepairEligibilityAssessments_EligibilityAssessmentId",
                        column: x => x.EligibilityAssessmentId,
                        principalTable: "RepairEligibilityAssessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairEligibilityWarrantySources_Warranties_WarrantyId",
                        column: x => x.WarrantyId,
                        principalTable: "Warranties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RepairExecutionFinishes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BindingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExecutionStartId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperationOriginId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ClaimedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ServerReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    VerifiedOriginalAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TimeProvenance = table.Column<byte>(type: "tinyint", nullable: false),
                    TimeProofFactsJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairExecutionFinishes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepairExecutionFinishes_FieldInspectionOperationOrigins_OperationOriginId",
                        column: x => x.OperationOriginId,
                        principalTable: "FieldInspectionOperationOrigins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairExecutionFinishes_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairExecutionFinishes_Users_OriginalActorId",
                        column: x => x.OriginalActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RepairExecutionStarts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BindingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssessmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirstStartId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperationOriginId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ClaimedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ServerReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    VerifiedOriginalAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TimeProvenance = table.Column<byte>(type: "tinyint", nullable: false),
                    AssessmentContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ChecklistVersion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AuthorityFactsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EligibilityAssessmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairExecutionStarts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepairExecutionStarts_FieldInspectionOperationOrigins_OperationOriginId",
                        column: x => x.OperationOriginId,
                        principalTable: "FieldInspectionOperationOrigins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairExecutionStarts_FieldTaskStartOrigins_FirstStartId",
                        column: x => x.FirstStartId,
                        principalTable: "FieldTaskStartOrigins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairExecutionStarts_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairExecutionStarts_RepairEligibilityAssessments_EligibilityAssessmentId",
                        column: x => x.EligibilityAssessmentId,
                        principalTable: "RepairEligibilityAssessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairExecutionStarts_Users_OriginalActorId",
                        column: x => x.OriginalActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RepairFieldTaskBindings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObligationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CrewId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Mode = table.Column<byte>(type: "tinyint", nullable: false),
                    AuthorizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PolicyRevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PolicyContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    PlanHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ChecklistVersion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RouteVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SegmentSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LayoutRevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SlabId = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    MapPublicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CrsProfileRevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LocationVersion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AssignedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    TaskVersion = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    AssignmentVersion = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairFieldTaskBindings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepairFieldTaskBindings_CrsProfileRevisions_CrsProfileRevisionId",
                        column: x => x.CrsProfileRevisionId,
                        principalTable: "CrsProfileRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairFieldTaskBindings_Defects_DefectId",
                        column: x => x.DefectId,
                        principalTable: "Defects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairFieldTaskBindings_FieldInspectionAssignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "FieldInspectionAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairFieldTaskBindings_FieldInspectionTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "FieldInspectionTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairFieldTaskBindings_GeometryMapPublications_MapPublicationId",
                        column: x => x.MapPublicationId,
                        principalTable: "GeometryMapPublications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairFieldTaskBindings_PavementLayoutRevisions_LayoutRevisionId",
                        column: x => x.LayoutRevisionId,
                        principalTable: "PavementLayoutRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairFieldTaskBindings_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairFieldTaskBindings_RepairExecutionAuthorizations_AuthorizationId",
                        column: x => x.AuthorizationId,
                        principalTable: "RepairExecutionAuthorizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairFieldTaskBindings_RepairPolicyRevisions_PolicyRevisionId",
                        column: x => x.PolicyRevisionId,
                        principalTable: "RepairPolicyRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairFieldTaskBindings_RoadSectionVersions_RouteVersionId",
                        column: x => x.RouteVersionId,
                        principalTable: "RoadSectionVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairFieldTaskBindings_RoadSegmentSets_SegmentSetId",
                        column: x => x.SegmentSetId,
                        principalTable: "RoadSegmentSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairFieldTaskBindings_Users_AssignedBy",
                        column: x => x.AssignedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairFieldTaskBindings_Users_CrewId",
                        column: x => x.CrewId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RepairItemLifecycleEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObligationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Mode = table.Column<byte>(type: "tinyint", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<byte>(type: "tinyint", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    At = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    BindingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DecisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceVersion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairItemLifecycleEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepairItemLifecycleEvents_Defects_DefectId",
                        column: x => x.DefectId,
                        principalTable: "Defects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairItemLifecycleEvents_FieldInspectionSubmissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "FieldInspectionSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairItemLifecycleEvents_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairItemLifecycleEvents_RepairAttemptReviews_ReviewId",
                        column: x => x.ReviewId,
                        principalTable: "RepairAttemptReviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairItemLifecycleEvents_RepairAttempts_AttemptId",
                        column: x => x.AttemptId,
                        principalTable: "RepairAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairItemLifecycleEvents_RepairDecisions_DecisionId",
                        column: x => x.DecisionId,
                        principalTable: "RepairDecisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairItemLifecycleEvents_RepairFieldTaskBindings_BindingId",
                        column: x => x.BindingId,
                        principalTable: "RepairFieldTaskBindings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairItemLifecycleEvents_Users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RepairItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RepairPlan = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ChecklistVersion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ProposalPlanHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ApprovedPlanHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ObligationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Mode = table.Column<byte>(type: "tinyint", nullable: false),
                    State = table.Column<byte>(type: "tinyint", nullable: false),
                    CrewId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EffectiveDecisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CurrentBindingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CurrentAssessmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CurrentExecutionStartId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CurrentExecutionFinishId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CurrentAttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CurrentIntakeLinkId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EffectiveIntakeSubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CurrentReviewId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PredecessorItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SupersededByItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProposedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProposedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ApprovedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AssignedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssignedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReviewedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CancelledBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CancelledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepairItems_Defects_DefectId",
                        column: x => x.DefectId,
                        principalTable: "Defects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairItems_FieldInspectionSubmissions_EffectiveIntakeSubmissionId",
                        column: x => x.EffectiveIntakeSubmissionId,
                        principalTable: "FieldInspectionSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairItems_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairItems_RepairAttemptReviews_CurrentReviewId",
                        column: x => x.CurrentReviewId,
                        principalTable: "RepairAttemptReviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairItems_RepairAttemptSubmissionLinks_CurrentIntakeLinkId",
                        column: x => x.CurrentIntakeLinkId,
                        principalTable: "RepairAttemptSubmissionLinks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairItems_RepairAttempts_CurrentAttemptId",
                        column: x => x.CurrentAttemptId,
                        principalTable: "RepairAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairItems_RepairDecisions_EffectiveDecisionId",
                        column: x => x.EffectiveDecisionId,
                        principalTable: "RepairDecisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairItems_RepairExecutionFinishes_CurrentExecutionFinishId",
                        column: x => x.CurrentExecutionFinishId,
                        principalTable: "RepairExecutionFinishes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairItems_RepairExecutionStarts_CurrentExecutionStartId",
                        column: x => x.CurrentExecutionStartId,
                        principalTable: "RepairExecutionStarts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairItems_RepairFieldTaskBindings_CurrentBindingId",
                        column: x => x.CurrentBindingId,
                        principalTable: "RepairFieldTaskBindings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairItems_RepairItems_PredecessorItemId",
                        column: x => x.PredecessorItemId,
                        principalTable: "RepairItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairItems_RepairItems_SupersededByItemId",
                        column: x => x.SupersededByItemId,
                        principalTable: "RepairItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairItems_RepairPackages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "RepairPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairItems_Users_CrewId",
                        column: x => x.CrewId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RepairMeasurementAssessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BindingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirstStartId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperationOriginId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ServerReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Stage = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Readiness = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    MissingReasonsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LocationFactsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LocationState = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    FormalSourceSubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairMeasurementAssessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepairMeasurementAssessments_FieldInspectionAssignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "FieldInspectionAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairMeasurementAssessments_FieldInspectionOperationOrigins_OperationOriginId",
                        column: x => x.OperationOriginId,
                        principalTable: "FieldInspectionOperationOrigins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairMeasurementAssessments_FieldInspectionSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "FieldInspectionSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairMeasurementAssessments_FieldInspectionSubmissions_FormalSourceSubmissionId",
                        column: x => x.FormalSourceSubmissionId,
                        principalTable: "FieldInspectionSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairMeasurementAssessments_FieldInspectionTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "FieldInspectionTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairMeasurementAssessments_FieldTaskStartOrigins_FirstStartId",
                        column: x => x.FirstStartId,
                        principalTable: "FieldTaskStartOrigins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairMeasurementAssessments_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairMeasurementAssessments_RepairFieldTaskBindings_BindingId",
                        column: x => x.BindingId,
                        principalTable: "RepairFieldTaskBindings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairMeasurementAssessments_RepairItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "RepairItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairMeasurementAssessments_Users_OriginalActorId",
                        column: x => x.OriginalActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RepairObligations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<byte>(type: "tinyint", nullable: false),
                    Mandatory = table.Column<bool>(type: "bit", nullable: false),
                    EffectiveResolutionDecisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EffectiveResolutionHeadDecisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CurrentRepairItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OriginalCrewFirstStartId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairObligations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepairObligations_Defects_DefectId",
                        column: x => x.DefectId,
                        principalTable: "Defects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairObligations_FieldTaskStartOrigins_OriginalCrewFirstStartId",
                        column: x => x.OriginalCrewFirstStartId,
                        principalTable: "FieldTaskStartOrigins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairObligations_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairObligations_RepairDecisions_EffectiveResolutionDecisionId",
                        column: x => x.EffectiveResolutionDecisionId,
                        principalTable: "RepairDecisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairObligations_RepairDecisions_EffectiveResolutionHeadDecisionId",
                        column: x => x.EffectiveResolutionHeadDecisionId,
                        principalTable: "RepairDecisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairObligations_RepairItems_CurrentRepairItemId",
                        column: x => x.CurrentRepairItemId,
                        principalTable: "RepairItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairObligations_RepairPackages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "RepairPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RepairReviewRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DecisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<byte>(type: "tinyint", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    At = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairReviewRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepairReviewRequests_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairReviewRequests_RepairDecisions_DecisionId",
                        column: x => x.DecisionId,
                        principalTable: "RepairDecisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairReviewRequests_RepairItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "RepairItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairReviewRequests_Users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RepairWorkHandovers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PerformedScope = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    SafetyState = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    At = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairWorkHandovers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepairWorkHandovers_RepairItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "RepairItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairWorkHandovers_Users_FromActorId",
                        column: x => x.FromActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairWorkHandovers_Users_ToActorId",
                        column: x => x.ToActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RepairNormalSuccessors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObligationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceDecisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    At = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SourceAssessmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceCancellationEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceHandoverEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairNormalSuccessors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepairNormalSuccessors_FieldInspectionTaskEvents_SourceHandoverEventId",
                        column: x => x.SourceHandoverEventId,
                        principalTable: "FieldInspectionTaskEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairNormalSuccessors_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairNormalSuccessors_RepairDecisions_SourceDecisionId",
                        column: x => x.SourceDecisionId,
                        principalTable: "RepairDecisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairNormalSuccessors_RepairItemLifecycleEvents_SourceCancellationEventId",
                        column: x => x.SourceCancellationEventId,
                        principalTable: "RepairItemLifecycleEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairNormalSuccessors_RepairItems_SourceItemId",
                        column: x => x.SourceItemId,
                        principalTable: "RepairItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairNormalSuccessors_RepairItems_TargetItemId",
                        column: x => x.TargetItemId,
                        principalTable: "RepairItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairNormalSuccessors_RepairMeasurementAssessments_SourceAssessmentId",
                        column: x => x.SourceAssessmentId,
                        principalTable: "RepairMeasurementAssessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairNormalSuccessors_RepairObligations_ObligationId",
                        column: x => x.ObligationId,
                        principalTable: "RepairObligations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairNormalSuccessors_Users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RepairObligationResolutionEvents",
                columns: table => new
                {
                    DecisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Accepted = table.Column<bool>(type: "bit", nullable: false),
                    SupersedesDecisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    At = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PreviousHeadDecisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ObligationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairObligationResolutionEvents", x => x.DecisionId);
                    table.ForeignKey(
                        name: "FK_RepairObligationResolutionEvents_RepairDecisions_DecisionId",
                        column: x => x.DecisionId,
                        principalTable: "RepairDecisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairObligationResolutionEvents_RepairDecisions_PreviousHeadDecisionId",
                        column: x => x.PreviousHeadDecisionId,
                        principalTable: "RepairDecisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairObligationResolutionEvents_RepairDecisions_SupersedesDecisionId",
                        column: x => x.SupersedesDecisionId,
                        principalTable: "RepairDecisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairObligationResolutionEvents_RepairObligations_ObligationId",
                        column: x => x.ObligationId,
                        principalTable: "RepairObligations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoadCoverageMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScopeObligationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoadSectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationVersion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    From = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    To = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    OffsetFrom = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    OffsetTo = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    ApplicableFromUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ApplicableToUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    HandoverDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HandoverVersion = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    SourceKind = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceVersion = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    HandoverFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CoverageFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceFactsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Provenance = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConfirmedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    SupersedesId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoadCoverageMappings", x => x.Id);
                    table.CheckConstraint("CK_RoadCoverageMappings_Bounds", "[From]>=0 AND [To]>[From] AND [OffsetTo]>[OffsetFrom] AND [ApplicableToUtc]>[ApplicableFromUtc]");
                    table.CheckConstraint("CK_RoadCoverageMappings_Source", "[SourceKind] COLLATE Latin1_General_100_BIN2 IN ('WARRANTY','MAINTENANCE_BASIS') AND [Provenance] COLLATE Latin1_General_100_BIN2 IN ('REAL_SOURCE','TEST_ONLY') AND ISJSON([SourceFactsJson])=1");
                    table.ForeignKey(
                        name: "FK_RoadCoverageMappings_Files_CoverageFileId",
                        column: x => x.CoverageFileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoadCoverageMappings_Files_HandoverFileId",
                        column: x => x.HandoverFileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoadCoverageMappings_HandoverDocuments_HandoverDocumentId",
                        column: x => x.HandoverDocumentId,
                        principalTable: "HandoverDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoadCoverageMappings_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoadCoverageMappings_RepairObligations_ScopeObligationId",
                        column: x => x.ScopeObligationId,
                        principalTable: "RepairObligations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoadCoverageMappings_RoadCoverageMappings_SupersedesId",
                        column: x => x.SupersedesId,
                        principalTable: "RoadCoverageMappings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoadCoverageMappings_RoadSections_RoadSectionId",
                        column: x => x.RoadSectionId,
                        principalTable: "RoadSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoadCoverageMappings_Users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RepairPolicyDraftChanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    At = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DefectTypeCode = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ChecklistVersion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Measurements = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StopConditions = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DraftId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairPolicyDraftChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepairPolicyDraftChanges_Users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RepairPolicyDrafts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentChangeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PublishedRevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairPolicyDrafts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepairPolicyDrafts_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairPolicyDrafts_RepairPolicyDraftChanges_CurrentChangeId",
                        column: x => x.CurrentChangeId,
                        principalTable: "RepairPolicyDraftChanges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairPolicyDrafts_RepairPolicyRevisions_PublishedRevisionId",
                        column: x => x.PublishedRevisionId,
                        principalTable: "RepairPolicyRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RepairSafetyActionSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MeasureId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BindingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    At = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    OriginId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairSafetyActionSources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepairSafetyActionSources_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairSafetyActionSources_RepairFieldTaskBindings_BindingId",
                        column: x => x.BindingId,
                        principalTable: "RepairFieldTaskBindings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairSafetyActionSources_RepairItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "RepairItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairSafetyActionSources_Users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RepairSafetyCheckEvidence",
                columns: table => new
                {
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CheckId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileVersion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Hash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Purpose = table.Column<byte>(type: "tinyint", nullable: false),
                    Verified = table.Column<bool>(type: "bit", nullable: false),
                    Related = table.Column<bool>(type: "bit", nullable: false),
                    SourceKind = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CapturedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReuseDecisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReusedSource = table.Column<bool>(type: "bit", nullable: false),
                    ActualUploaderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairSafetyCheckEvidence", x => new { x.CheckId, x.FileId });
                    table.ForeignKey(
                        name: "FK_RepairSafetyCheckEvidence_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairSafetyCheckEvidence_Users_ActualUploaderId",
                        column: x => x.ActualUploaderId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RepairSafetyChecks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MeasureId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    At = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Result = table.Column<byte>(type: "tinyint", nullable: false),
                    Findings = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    EvidenceIds = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairSafetyChecks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepairSafetyChecks_Users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RepairSafetyMonitoring",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MeasureId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SafetyObligationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FormalObligationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentCheckId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairSafetyMonitoring", x => x.Id);
                    table.UniqueConstraint("AK_RepairSafetyMonitoring_MeasureId", x => x.MeasureId);
                    table.ForeignKey(
                        name: "FK_RepairSafetyMonitoring_RepairObligations_FormalObligationId",
                        column: x => x.FormalObligationId,
                        principalTable: "RepairObligations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairSafetyMonitoring_RepairObligations_SafetyObligationId",
                        column: x => x.SafetyObligationId,
                        principalTable: "RepairObligations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairSafetyMonitoring_RepairSafetyChecks_CurrentCheckId",
                        column: x => x.CurrentCheckId,
                        principalTable: "RepairSafetyChecks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RepairSafetyResponsibilityTransfers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreviousActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NextActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Handover = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    At = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    MeasureId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairSafetyResponsibilityTransfers", x => x.Id);
                    table.UniqueConstraint("AK_RepairSafetyResponsibilityTransfers_MeasureId_Id", x => new { x.MeasureId, x.Id });
                    table.ForeignKey(
                        name: "FK_RepairSafetyResponsibilityTransfers_Users_ChangedBy",
                        column: x => x.ChangedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairSafetyResponsibilityTransfers_Users_NextActorId",
                        column: x => x.NextActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairSafetyResponsibilityTransfers_Users_PreviousActorId",
                        column: x => x.PreviousActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RepairTemporarySafetyMeasures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FormalRepairObligationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResponsibleActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentResponsibilityTransferId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CheckSchedule = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ReplacementCondition = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    RemovalCondition = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    InstallationEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InstalledBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InstalledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FirstCheckDueAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairTemporarySafetyMeasures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepairTemporarySafetyMeasures_Defects_DefectId",
                        column: x => x.DefectId,
                        principalTable: "Defects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairTemporarySafetyMeasures_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairTemporarySafetyMeasures_RepairObligations_FormalRepairObligationId",
                        column: x => x.FormalRepairObligationId,
                        principalTable: "RepairObligations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairTemporarySafetyMeasures_RepairSafetyResponsibilityTransfers_Id_CurrentResponsibilityTransferId",
                        columns: x => new { x.Id, x.CurrentResponsibilityTransferId },
                        principalTable: "RepairSafetyResponsibilityTransfers",
                        principalColumns: new[] { "MeasureId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairTemporarySafetyMeasures_Users_InstalledBy",
                        column: x => x.InstalledBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairTemporarySafetyMeasures_Users_ResponsibleActorId",
                        column: x => x.ResponsibleActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplementarySurveyRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SurveyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SurveyRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", nullable: false),
                    RequestedScope = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RoundNo = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    SourcePreservationNote = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplementarySurveyRequests", x => x.Id);
                    table.CheckConstraint("CK_SupplementarySurveyRequests_Approval", "([ApprovedByUserId] IS NULL AND [ApprovedAt] IS NULL) OR ([ApprovedByUserId] IS NOT NULL AND [ApprovedAt] IS NOT NULL)");
                    table.CheckConstraint("CK_SupplementarySurveyRequests_RequestedScope_JsonObject", "ISJSON([RequestedScope]) = 1 AND LEFT(LTRIM([RequestedScope]), 1) = '{'");
                    table.CheckConstraint("CK_SupplementarySurveyRequests_RoundNo", "[RoundNo] > 0");
                    table.CheckConstraint("CK_SupplementarySurveyRequests_Status", "[Status] IN (1, 2, 3, 4, 5, 6, 7)");
                    table.ForeignKey(
                        name: "FK_SupplementarySurveyRequests_Users_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplementarySurveyRequests_Users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SurveyRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoadSectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoadSectionVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SurveyPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ScopeFormatVersion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ParentTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SupplementRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SurveyType = table.Column<byte>(type: "tinyint", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    DueAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    OutputRequirements = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CancelledAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyRequests", x => x.Id);
                    table.CheckConstraint("CK_SurveyRequests_Cancellation", "([Status] = 9 AND [CancelledAt] IS NOT NULL AND LEN(LTRIM(RTRIM([CancellationReason]))) > 0) OR ([Status] <> 9 AND [CancelledAt] IS NULL AND [CancellationReason] IS NULL)");
                    table.CheckConstraint("CK_SurveyRequests_OutputRequirements_Json", "ISJSON([OutputRequirements]) = 1");
                    table.CheckConstraint("CK_SurveyRequests_Status", "[Status] IN (1, 2, 3, 4, 5, 6, 7, 8, 9, 10)");
                    table.CheckConstraint("CK_SurveyRequests_SurveyType", "[SurveyType] IN (1, 2, 3)");
                    table.ForeignKey(
                        name: "FK_SurveyRequests_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SurveyRequests_RoadSectionVersions_RoadSectionVersionId",
                        column: x => x.RoadSectionVersionId,
                        principalTable: "RoadSectionVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SurveyRequests_RoadSections_RoadSectionId",
                        column: x => x.RoadSectionId,
                        principalTable: "RoadSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SurveyRequests_SupplementarySurveyRequests_SupplementRequestId",
                        column: x => x.SupplementRequestId,
                        principalTable: "SupplementarySurveyRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SurveyRequests_SurveyPlans_SurveyPlanId",
                        column: x => x.SurveyPlanId,
                        principalTable: "SurveyPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SurveyRequests_SurveyRequests_ParentTaskId",
                        column: x => x.ParentTaskId,
                        principalTable: "SurveyRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SurveyRequests_Users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SurveyAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SurveyRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperatorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    AcceptedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    RejectedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(1000)", nullable: true),
                    ReassignmentReason = table.Column<string>(type: "nvarchar(1000)", nullable: true),
                    EndedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyAssignments", x => x.Id);
                    table.CheckConstraint("CK_SurveyAssignments_AcceptanceRejection", "[AcceptedAt] IS NULL OR [RejectedAt] IS NULL");
                    table.CheckConstraint("CK_SurveyAssignments_ActiveReassignmentReason", "[EndedAt] IS NOT NULL OR [ReassignmentReason] IS NULL");
                    table.CheckConstraint("CK_SurveyAssignments_ReassignmentNotRejected", "[ReassignmentReason] IS NULL OR [RejectedAt] IS NULL");
                    table.CheckConstraint("CK_SurveyAssignments_Rejection", "([RejectedAt] IS NULL AND [RejectionReason] IS NULL) OR ([RejectedAt] IS NOT NULL AND LEN(LTRIM(RTRIM([RejectionReason]))) > 0 AND [EndedAt] IS NOT NULL)");
                    table.CheckConstraint("CK_SurveyAssignments_TimestampOrder", "([AcceptedAt] IS NULL OR [AcceptedAt] >= [AssignedAt]) AND ([RejectedAt] IS NULL OR [RejectedAt] >= [AssignedAt]) AND ([EndedAt] IS NULL OR [EndedAt] >= [AssignedAt])");
                    table.ForeignKey(
                        name: "FK_SurveyAssignments_SurveyRequests_SurveyRequestId",
                        column: x => x.SurveyRequestId,
                        principalTable: "SurveyRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SurveyAssignments_Users_AssignedByUserId",
                        column: x => x.AssignedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SurveyAssignments_Users_OperatorUserId",
                        column: x => x.OperatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SurveyRequestScopes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SurveyRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RouteSectionVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SegmentSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SegmentIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetBand = table.Column<string>(type: "nvarchar(32)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyRequestScopes", x => x.Id);
                    table.CheckConstraint("CK_SurveyRequestScopes_SegmentIdsJson", "ISJSON([SegmentIdsJson]) = 1");
                    table.ForeignKey(
                        name: "FK_SurveyRequestScopes_RoadSectionVersions_RouteSectionVersionId",
                        column: x => x.RouteSectionVersionId,
                        principalTable: "RoadSectionVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SurveyRequestScopes_SurveyRequests_SurveyRequestId",
                        column: x => x.SurveyRequestId,
                        principalTable: "SurveyRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Surveys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SurveyRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoadSectionVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SurveyType = table.Column<byte>(type: "tinyint", nullable: false),
                    IsBaselineConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    BaselineConfirmedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BaselineConfirmedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Surveys", x => x.Id);
                    table.CheckConstraint("CK_Surveys_BaselineConfirmation", "([IsBaselineConfirmed] = 0 AND [BaselineConfirmedByUserId] IS NULL AND [BaselineConfirmedAt] IS NULL) OR ([IsBaselineConfirmed] = 1 AND [BaselineConfirmedByUserId] IS NOT NULL AND [BaselineConfirmedAt] IS NOT NULL)");
                    table.CheckConstraint("CK_Surveys_Status", "[Status] IN (1, 2, 3, 4, 5)");
                    table.CheckConstraint("CK_Surveys_SurveyType", "[SurveyType] IN (1, 2, 3)");
                    table.ForeignKey(
                        name: "FK_Surveys_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Surveys_RoadSectionVersions_RoadSectionVersionId",
                        column: x => x.RoadSectionVersionId,
                        principalTable: "RoadSectionVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Surveys_SurveyRequests_SurveyRequestId",
                        column: x => x.SurveyRequestId,
                        principalTable: "SurveyRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Surveys_Users_BaselineConfirmedByUserId",
                        column: x => x.BaselineConfirmedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SurveyDataVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SurveyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNo = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    IntegrityStatus = table.Column<byte>(type: "tinyint", nullable: false),
                    ConfirmedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    ConfirmedBy = table.Column<byte>(type: "tinyint", nullable: true),
                    SourceManifest = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ScopeManifest = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SubmittedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PairsManifest = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyDataVersions", x => x.Id);
                    table.CheckConstraint("CK_SurveyDataVersions_IntegrityStatus", "[IntegrityStatus] IN (1, 2, 3)");
                    table.CheckConstraint("CK_SurveyDataVersions_ServerConfirmation", "([Status] = 3 AND [IntegrityStatus] = 2 AND [ConfirmedAt] IS NOT NULL AND [ConfirmedBy] = 1) OR ([Status] <> 3 AND [ConfirmedAt] IS NULL AND [ConfirmedBy] IS NULL)");
                    table.CheckConstraint("CK_SurveyDataVersions_SourceManifest_JsonArray", "ISJSON([SourceManifest]) = 1 AND LEFT(LTRIM([SourceManifest]), 1) = '['");
                    table.CheckConstraint("CK_SurveyDataVersions_Status", "[Status] IN (1, 2, 3, 4, 5)");
                    table.CheckConstraint("CK_SurveyDataVersions_VersionNo", "[VersionNo] > 0");
                    table.ForeignKey(
                        name: "FK_SurveyDataVersions_Surveys_SurveyId",
                        column: x => x.SurveyId,
                        principalTable: "Surveys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SurveyFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SurveyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FlightId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileType = table.Column<byte>(type: "tinyint", nullable: false),
                    CaptureStartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    CaptureEndedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    SyncStatus = table.Column<byte>(type: "tinyint", nullable: false),
                    Checksum = table.Column<string>(type: "char(64)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyFiles", x => x.Id);
                    table.CheckConstraint("CK_SurveyFiles_Checksum_Sha256Lowercase", "LEN([Checksum]) = 64 AND [Checksum] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'");
                    table.CheckConstraint("CK_SurveyFiles_FileType", "[FileType] IN (1, 2, 3, 4)");
                    table.CheckConstraint("CK_SurveyFiles_SyncStatus", "[SyncStatus] IN (1, 2, 3, 4, 5)");
                    table.CheckConstraint("CK_SurveyFiles_TimestampOrder", "[CaptureEndedAt] IS NULL OR [CaptureStartedAt] IS NULL OR [CaptureEndedAt] >= [CaptureStartedAt]");
                    table.ForeignKey(
                        name: "FK_SurveyFiles_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SurveyFiles_Flights_FlightId",
                        column: x => x.FlightId,
                        principalTable: "Flights",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SurveyFiles_Surveys_SurveyId",
                        column: x => x.SurveyId,
                        principalTable: "Surveys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountStatusChangeLogs_ChangedByUserId",
                table: "AccountStatusChangeLogs",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountStatusChangeLogs_OccurredAt",
                table: "AccountStatusChangeLogs",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_AccountStatusChangeLogs_TargetUserId",
                table: "AccountStatusChangeLogs",
                column: "TargetUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AIDetections_DefectTypeCode",
                table: "AIDetections",
                column: "DefectTypeCode");

            migrationBuilder.CreateIndex(
                name: "IX_AIDetections_ModelVersionId",
                table: "AIDetections",
                column: "ModelVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_AIDetections_ProcessingJobId",
                table: "AIDetections",
                column: "ProcessingJobId");

            migrationBuilder.CreateIndex(
                name: "IX_AIDetections_RoadSectionVersionId",
                table: "AIDetections",
                column: "RoadSectionVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_AIModelVersions_ModelName",
                table: "AIModelVersions",
                column: "ModelName");

            migrationBuilder.CreateIndex(
                name: "IX_AIModelVersions_ReleasedByUserId",
                table: "AIModelVersions",
                column: "ReleasedByUserId");

            migrationBuilder.CreateIndex(
                name: "UX_AIModelVersions_VersionLabel",
                table: "AIModelVersions",
                column: "VersionLabel",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Anh02AiDetectionProvenance_FrameFileId",
                table: "Anh02AiDetectionProvenance",
                column: "FrameFileId");

            migrationBuilder.CreateIndex(
                name: "IX_Anh02AiDetectionProvenance_ResultId_RunId",
                table: "Anh02AiDetectionProvenance",
                columns: new[] { "ResultId", "RunId" });

            migrationBuilder.CreateIndex(
                name: "IX_Anh02AiDetectionProvenance_RunId_SourceVideoFileId_SourceVideoFileVersion",
                table: "Anh02AiDetectionProvenance",
                columns: new[] { "RunId", "SourceVideoFileId", "SourceVideoFileVersion" });

            migrationBuilder.CreateIndex(
                name: "IX_Anh02AiDetectionProvenance_SegmentId",
                table: "Anh02AiDetectionProvenance",
                column: "SegmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Anh02AiDetectionProvenance_SourceVideoFileId",
                table: "Anh02AiDetectionProvenance",
                column: "SourceVideoFileId");

            migrationBuilder.CreateIndex(
                name: "IX_Anh02AiManifestFiles_FileId",
                table: "Anh02AiManifestFiles",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_Anh02AiMockRuns_AnalysisRunId",
                table: "Anh02AiMockRuns",
                column: "AnalysisRunId");

            migrationBuilder.CreateIndex(
                name: "IX_Anh02AiMockRuns_AttemptId",
                table: "Anh02AiMockRuns",
                column: "AttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_Anh02AiMockRuns_CreatedBy",
                table: "Anh02AiMockRuns",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Anh02AiMockRuns_DatasetVersionId",
                table: "Anh02AiMockRuns",
                column: "DatasetVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_Anh02AiMockRuns_ModelVersionId",
                table: "Anh02AiMockRuns",
                column: "ModelVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_Anh02AiMockRuns_ProcessingJobId",
                table: "Anh02AiMockRuns",
                column: "ProcessingJobId",
                unique: true,
                filter: "[Stage]='VIDEO_ANALYSIS'");

            migrationBuilder.CreateIndex(
                name: "IX_Anh02AiMockRuns_ProjectId",
                table: "Anh02AiMockRuns",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Anh02AiMockRuns_ResultId_Id",
                table: "Anh02AiMockRuns",
                columns: new[] { "ResultId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Anh02AiMockRuns_SegmentSetId_RouteVersionId",
                table: "Anh02AiMockRuns",
                columns: new[] { "SegmentSetId", "RouteVersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_Anh02AiMockRuns_Status_LeaseUntil",
                table: "Anh02AiMockRuns",
                columns: new[] { "Status", "LeaseUntil" });

            migrationBuilder.CreateIndex(
                name: "IX_Anh02AiResultProvenance_AttemptId",
                table: "Anh02AiResultProvenance",
                column: "AttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_Anh02AiResultProvenance_ProcessingJobId",
                table: "Anh02AiResultProvenance",
                column: "ProcessingJobId");

            migrationBuilder.CreateIndex(
                name: "IX_Anh02AiResultProvenance_RunId",
                table: "Anh02AiResultProvenance",
                column: "RunId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Anh02ExportJobs_ArtifactId",
                table: "Anh02ExportJobs",
                column: "ArtifactId");

            migrationBuilder.CreateIndex(
                name: "IX_Anh02ExportJobs_ProjectId",
                table: "Anh02ExportJobs",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Anh02ExportJobs_RequestedBy",
                table: "Anh02ExportJobs",
                column: "RequestedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Anh02ExportJobs_SnapshotId",
                table: "Anh02ExportJobs",
                column: "SnapshotId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Anh02ExportJobs_Status_NextAttemptAt_LeaseUntil",
                table: "Anh02ExportJobs",
                columns: new[] { "Status", "NextAttemptAt", "LeaseUntil" });

            migrationBuilder.CreateIndex(
                name: "IX_Anh02ExportSnapshotFiles_FileId",
                table: "Anh02ExportSnapshotFiles",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_Anh02ExportSnapshotFiles_SnapshotId_FileId",
                table: "Anh02ExportSnapshotFiles",
                columns: new[] { "SnapshotId", "FileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Anh02ExportSnapshots_ProjectId",
                table: "Anh02ExportSnapshots",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Anh02GeneratedArtifacts_ExportJobId",
                table: "Anh02GeneratedArtifacts",
                column: "ExportJobId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Anh02GeneratedArtifacts_FileId",
                table: "Anh02GeneratedArtifacts",
                column: "FileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Anh02GeneratedArtifacts_SnapshotId",
                table: "Anh02GeneratedArtifacts",
                column: "SnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ActorUserId",
                table: "AuditLogs",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CorrelationId",
                table: "AuditLogs",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Entity",
                table: "AuditLogs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_OccurredAtUtc",
                table: "AuditLogs",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_BaselineCurrentPointers_ProjectId_RouteVersionId_SegmentSetId_SegmentId_TargetBand",
                table: "BaselineCurrentPointers",
                columns: new[] { "ProjectId", "RouteVersionId", "SegmentSetId", "SegmentId", "TargetBand" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BaselineCurrentPointers_SelectionId",
                table: "BaselineCurrentPointers",
                column: "SelectionId");

            migrationBuilder.CreateIndex(
                name: "IX_BaselineSelectionItems_AssessmentId",
                table: "BaselineSelectionItems",
                column: "AssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_BaselineSelectionItems_BaselineSelectionId_RouteVersionId_SegmentSetId_SegmentId_TargetBand",
                table: "BaselineSelectionItems",
                columns: new[] { "BaselineSelectionId", "RouteVersionId", "SegmentSetId", "SegmentId", "TargetBand" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BaselineSelectionItems_DatasetId",
                table: "BaselineSelectionItems",
                column: "DatasetId");

            migrationBuilder.CreateIndex(
                name: "IX_BaselineSelections_ProjectId",
                table: "BaselineSelections",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_BaselineSelections_SelectedBy",
                table: "BaselineSelections",
                column: "SelectedBy");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessDutyAppointments_CurrentActorId",
                table: "BusinessDutyAppointments",
                column: "CurrentActorId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessDutyAppointments_DecisionActorId",
                table: "BusinessDutyAppointments",
                column: "DecisionActorId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessDutyAppointments_RequestId",
                table: "BusinessDutyAppointments",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessReceivingRequests_ClockId",
                table: "BusinessReceivingRequests",
                column: "ClockId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessReceivingRequests_ProjectId_ScopeId_CompletedAt",
                table: "BusinessReceivingRequests",
                columns: new[] { "ProjectId", "ScopeId", "CompletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessReceivingRequests_ResponsibleActorId",
                table: "BusinessReceivingRequests",
                column: "ResponsibleActorId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessReceivingRequests_SourceKind_SourceId_Kind",
                table: "BusinessReceivingRequests",
                columns: new[] { "SourceKind", "SourceId", "Kind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CandidateSourceHeads_DecisionId_SourceKind_SourceId_ProjectId",
                table: "CandidateSourceHeads",
                columns: new[] { "DecisionId", "SourceKind", "SourceId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_CandidateSourceHeads_ProjectId",
                table: "CandidateSourceHeads",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseConclusionDefects_DefectId",
                table: "CaseConclusionDefects",
                column: "DefectId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseConclusionEvidence_SourceReportId",
                table: "CaseConclusionEvidence",
                column: "SourceReportId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseConclusions_CaseId",
                table: "CaseConclusions",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_CasePublicationDefects_DefectId",
                table: "CasePublicationDefects",
                column: "DefectId");

            migrationBuilder.CreateIndex(
                name: "IX_CasePublicationEvidence_SourceReportId",
                table: "CasePublicationEvidence",
                column: "SourceReportId");

            migrationBuilder.CreateIndex(
                name: "IX_CasePublicationRecipients_ReportId",
                table: "CasePublicationRecipients",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_CasePublications_CaseId",
                table: "CasePublications",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseReportLinkHistory_ActorUserId",
                table: "CaseReportLinkHistory",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseReportLinkHistory_FromCaseId",
                table: "CaseReportLinkHistory",
                column: "FromCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseReportLinkHistory_ToCaseId",
                table: "CaseReportLinkHistory",
                column: "ToCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseReportLinkHistoryReports_ReportId",
                table: "CaseReportLinkHistoryReports",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseReportLinks_CaseId_ReportId",
                table: "CaseReportLinks",
                columns: new[] { "CaseId", "ReportId" });

            migrationBuilder.CreateIndex(
                name: "IX_CaseReportLinks_ReportId",
                table: "CaseReportLinks",
                column: "ReportId",
                unique: true,
                filter: "[EndedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "UX_ConsumerEffectReceipts_MessageConsumer",
                table: "ConsumerEffectReceipts",
                columns: new[] { "MessageId", "ConsumerName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CrsProfileRevisions_CreatedBy",
                table: "CrsProfileRevisions",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CrsProfileRevisions_ProjectId_Code_Revision",
                table: "CrsProfileRevisions",
                columns: new[] { "ProjectId", "Code", "Revision" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DatasetAssessmentItems_AssessmentId_RouteVersionId_SegmentSetId_SegmentId_TargetBand",
                table: "DatasetAssessmentItems",
                columns: new[] { "AssessmentId", "RouteVersionId", "SegmentSetId", "SegmentId", "TargetBand" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DatasetAssessmentItems_SegmentId",
                table: "DatasetAssessmentItems",
                column: "SegmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DatasetAssessments_DatasetId_ReviewedAt",
                table: "DatasetAssessments",
                columns: new[] { "DatasetId", "ReviewedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DatasetAssessments_ReviewedBy",
                table: "DatasetAssessments",
                column: "ReviewedBy");

            migrationBuilder.CreateIndex(
                name: "IX_DeadlineBreaches_ClockId_DueAt",
                table: "DeadlineBreaches",
                columns: new[] { "ClockId", "DueAt" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeadlineClocks_AcknowledgedByUserId",
                table: "DeadlineClocks",
                column: "AcknowledgedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DeadlineClocks_AppointedActorId",
                table: "DeadlineClocks",
                column: "AppointedActorId");

            migrationBuilder.CreateIndex(
                name: "IX_DeadlineClocks_Kind_TargetId",
                table: "DeadlineClocks",
                columns: new[] { "Kind", "TargetId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeadlineClocks_OriginEventId",
                table: "DeadlineClocks",
                column: "OriginEventId");

            migrationBuilder.CreateIndex(
                name: "IX_DeadlineClocks_ProjectId_CompletedAt_CurrentDueAt",
                table: "DeadlineClocks",
                columns: new[] { "ProjectId", "CompletedAt", "CurrentDueAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DeadlineDutyAppointments_ClockId",
                table: "DeadlineDutyAppointments",
                column: "ClockId");

            migrationBuilder.CreateIndex(
                name: "IX_DeadlineDutyAppointments_CurrentActorId",
                table: "DeadlineDutyAppointments",
                column: "CurrentActorId");

            migrationBuilder.CreateIndex(
                name: "IX_DeadlineDutyAppointments_DecisionActorId",
                table: "DeadlineDutyAppointments",
                column: "DecisionActorId");

            migrationBuilder.CreateIndex(
                name: "IX_DeadlineExtensions_ActorUserId",
                table: "DeadlineExtensions",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DeadlineExtensions_ClockId_NewDueAt",
                table: "DeadlineExtensions",
                columns: new[] { "ClockId", "NewDueAt" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Defects_CauseCategoryCode",
                table: "Defects",
                column: "CauseCategoryCode");

            migrationBuilder.CreateIndex(
                name: "IX_Defects_DefectTypeCode",
                table: "Defects",
                column: "DefectTypeCode");

            migrationBuilder.CreateIndex(
                name: "IX_Defects_ProjectId",
                table: "Defects",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Defects_RoadSectionVersionId",
                table: "Defects",
                column: "RoadSectionVersionId");

            migrationBuilder.CreateIndex(
                name: "UX_Defects_SourceAIDetectionId",
                table: "Defects",
                column: "SourceAIDetectionId",
                unique: true,
                filter: "[SourceAIDetectionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DefectSourceLinks_AIDetectionSourceId",
                table: "DefectSourceLinks",
                column: "AIDetectionSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_DefectSourceLinks_DecisionId",
                table: "DefectSourceLinks",
                column: "DecisionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DefectSourceLinks_DecisionId_SourceKind_SourceId_ProjectId",
                table: "DefectSourceLinks",
                columns: new[] { "DecisionId", "SourceKind", "SourceId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_DefectSourceLinks_DefectId",
                table: "DefectSourceLinks",
                column: "DefectId");

            migrationBuilder.CreateIndex(
                name: "IX_DefectSourceLinks_ProjectId",
                table: "DefectSourceLinks",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_DefectSourceLinks_ReportSourceId",
                table: "DefectSourceLinks",
                column: "ReportSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_DefectSourceLinks_SourceKind_SourceId",
                table: "DefectSourceLinks",
                columns: new[] { "SourceKind", "SourceId" },
                unique: true,
                filter: "[EndedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DefectStatisticsSources_ActorId",
                table: "DefectStatisticsSources",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_DefectStatisticsSources_DefectId",
                table: "DefectStatisticsSources",
                column: "DefectId");

            migrationBuilder.CreateIndex(
                name: "IX_DefectStatisticsSources_ObligationId",
                table: "DefectStatisticsSources",
                column: "ObligationId");

            migrationBuilder.CreateIndex(
                name: "IX_DefectStatisticsSources_ProjectId_SharedPartId",
                table: "DefectStatisticsSources",
                columns: new[] { "ProjectId", "SharedPartId" });

            migrationBuilder.CreateIndex(
                name: "IX_DefectStatisticsSources_RoadSectionId",
                table: "DefectStatisticsSources",
                column: "RoadSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_DefectStatisticsSources_RouteVersionId",
                table: "DefectStatisticsSources",
                column: "RouteVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_DefectStatisticsSources_SupersedesId",
                table: "DefectStatisticsSources",
                column: "SupersedesId",
                unique: true,
                filter: "[SupersedesId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DefectVerificationLogs_AIDetectionId",
                table: "DefectVerificationLogs",
                column: "AIDetectionId");

            migrationBuilder.CreateIndex(
                name: "IX_DefectVerificationLogs_DefectId",
                table: "DefectVerificationLogs",
                column: "DefectId");

            migrationBuilder.CreateIndex(
                name: "IX_DefectVerificationLogs_FieldInspectionTaskId",
                table: "DefectVerificationLogs",
                column: "FieldInspectionTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_DefectVerificationLogs_SeverityRuleVersionId",
                table: "DefectVerificationLogs",
                column: "SeverityRuleVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_DefectVerificationLogs_VerifiedByUserId",
                table: "DefectVerificationLogs",
                column: "VerifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DerivedMeasurements_DataSampleTypeStatus",
                table: "DerivedMeasurements",
                columns: new[] { "SurveyDataVersionId", "SampleId", "MeasurementType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_DerivedMeasurements_RoadSectionVersionId",
                table: "DerivedMeasurements",
                column: "RoadSectionVersionId");

            migrationBuilder.CreateIndex(
                name: "UX_DroneDevices_SerialNo",
                table: "DroneDevices",
                column: "SerialNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionAssignments_AssignedByUserId",
                table: "FieldInspectionAssignments",
                column: "AssignedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionAssignments_AssignedToUserId",
                table: "FieldInspectionAssignments",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "UX_FieldInspectionAssignments_ActiveTask",
                table: "FieldInspectionAssignments",
                column: "FieldInspectionTaskId",
                unique: true,
                filter: "[Status] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionEvidenceLinks_AssignmentId",
                table: "FieldInspectionEvidenceLinks",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionEvidenceLinks_FileId",
                table: "FieldInspectionEvidenceLinks",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionEvidenceLinks_ProjectId",
                table: "FieldInspectionEvidenceLinks",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionEvidenceLinks_SubmissionId_CaptureOriginId",
                table: "FieldInspectionEvidenceLinks",
                columns: new[] { "SubmissionId", "CaptureOriginId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionEvidenceLinks_TaskId",
                table: "FieldInspectionEvidenceLinks",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionEvidenceReuseDecisions_ActorId",
                table: "FieldInspectionEvidenceReuseDecisions",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionEvidenceReuseDecisions_FileId",
                table: "FieldInspectionEvidenceReuseDecisions",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionEvidenceReuseDecisions_ProjectId",
                table: "FieldInspectionEvidenceReuseDecisions",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionEvidenceReuseDecisions_TaskId",
                table: "FieldInspectionEvidenceReuseDecisions",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionLocationProofs_ProjectId",
                table: "FieldInspectionLocationProofs",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionLocationProofs_SubmissionId",
                table: "FieldInspectionLocationProofs",
                column: "SubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionLocationProofs_TaskId",
                table: "FieldInspectionLocationProofs",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionOperationOrigins_OriginalActorId",
                table: "FieldInspectionOperationOrigins",
                column: "OriginalActorId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionOperationOrigins_ProjectId_OriginId",
                table: "FieldInspectionOperationOrigins",
                columns: new[] { "ProjectId", "OriginId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionOperationOrigins_TaskId",
                table: "FieldInspectionOperationOrigins",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionReviews_ActorId",
                table: "FieldInspectionReviews",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionReviews_ProjectId",
                table: "FieldInspectionReviews",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionReviews_SubmissionId",
                table: "FieldInspectionReviews",
                column: "SubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionReviews_TaskId",
                table: "FieldInspectionReviews",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionSessions_EvidenceFileId",
                table: "FieldInspectionSessions",
                column: "EvidenceFileId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionSessions_FieldInspectionTaskId",
                table: "FieldInspectionSessions",
                column: "FieldInspectionTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionSessions_InspectorUserId",
                table: "FieldInspectionSessions",
                column: "InspectorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionSessions_ProjectVersion",
                table: "FieldInspectionSessions",
                columns: new[] { "ProjectId", "RoadSectionVersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionSessions_RoadSectionVersionId",
                table: "FieldInspectionSessions",
                column: "RoadSectionVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionSessions_SurveyId",
                table: "FieldInspectionSessions",
                column: "SurveyId");

            migrationBuilder.CreateIndex(
                name: "UX_FieldInspectionSessions_SessionCode",
                table: "FieldInspectionSessions",
                column: "SessionCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionSubmissions_AssignmentId",
                table: "FieldInspectionSubmissions",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionSubmissions_OperationOriginId",
                table: "FieldInspectionSubmissions",
                column: "OperationOriginId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionSubmissions_OriginalActorId",
                table: "FieldInspectionSubmissions",
                column: "OriginalActorId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionSubmissions_ParentId",
                table: "FieldInspectionSubmissions",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionSubmissions_ProjectId_OriginId",
                table: "FieldInspectionSubmissions",
                columns: new[] { "ProjectId", "OriginId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionSubmissions_RootId_Revision",
                table: "FieldInspectionSubmissions",
                columns: new[] { "RootId", "Revision" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionSubmissions_StartOriginId",
                table: "FieldInspectionSubmissions",
                column: "StartOriginId");

            migrationBuilder.CreateIndex(
                name: "UX_FieldInspectionSubmissions_Session",
                table: "FieldInspectionSubmissions",
                column: "SessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_FieldInspectionSubmissions_TaskRoot",
                table: "FieldInspectionSubmissions",
                column: "TaskId",
                unique: true,
                filter: "[Revision] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionTaskEvents_ActorId",
                table: "FieldInspectionTaskEvents",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionTaskEvents_AssignmentId",
                table: "FieldInspectionTaskEvents",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionTaskEvents_LocationImpactDecisionId",
                table: "FieldInspectionTaskEvents",
                column: "LocationImpactDecisionId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionTaskEvents_LocationImpactId",
                table: "FieldInspectionTaskEvents",
                column: "LocationImpactId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionTaskEvents_ProjectId",
                table: "FieldInspectionTaskEvents",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionTaskEvents_TaskId",
                table: "FieldInspectionTaskEvents",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionTasks_AssignedByUserId",
                table: "FieldInspectionTasks",
                column: "AssignedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionTasks_CrsProfileRevisionId",
                table: "FieldInspectionTasks",
                column: "CrsProfileRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionTasks_DefectId",
                table: "FieldInspectionTasks",
                column: "DefectId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionTasks_LayoutRevisionId",
                table: "FieldInspectionTasks",
                column: "LayoutRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionTasks_MapPublicationId",
                table: "FieldInspectionTasks",
                column: "MapPublicationId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionTasks_ProjectId",
                table: "FieldInspectionTasks",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionTasks_RepairItemId",
                table: "FieldInspectionTasks",
                column: "RepairItemId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionTasks_ReviewedByUserId",
                table: "FieldInspectionTasks",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionTasks_RoadSectionVersionId",
                table: "FieldInspectionTasks",
                column: "RoadSectionVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionTasks_SegmentSetId",
                table: "FieldInspectionTasks",
                column: "SegmentSetId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionTasks_SurveyId",
                table: "FieldInspectionTasks",
                column: "SurveyId");

            migrationBuilder.CreateIndex(
                name: "UX_FieldInspectionTasks_TaskCode",
                table: "FieldInspectionTasks",
                column: "TaskCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FieldTaskStartOrigins_AssignmentId",
                table: "FieldTaskStartOrigins",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldTaskStartOrigins_CrsProfileRevisionId",
                table: "FieldTaskStartOrigins",
                column: "CrsProfileRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldTaskStartOrigins_LayoutRevisionId",
                table: "FieldTaskStartOrigins",
                column: "LayoutRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldTaskStartOrigins_MapPublicationId",
                table: "FieldTaskStartOrigins",
                column: "MapPublicationId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldTaskStartOrigins_OperationOriginId",
                table: "FieldTaskStartOrigins",
                column: "OperationOriginId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldTaskStartOrigins_OriginalActorId",
                table: "FieldTaskStartOrigins",
                column: "OriginalActorId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldTaskStartOrigins_ProjectId_OriginId",
                table: "FieldTaskStartOrigins",
                columns: new[] { "ProjectId", "OriginId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FieldTaskStartOrigins_RouteVersionId",
                table: "FieldTaskStartOrigins",
                column: "RouteVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldTaskStartOrigins_SegmentSetId",
                table: "FieldTaskStartOrigins",
                column: "SegmentSetId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldTaskStartOrigins_TaskId",
                table: "FieldTaskStartOrigins",
                column: "TaskId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Files_UploadedByUserId",
                table: "Files",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "UX_Files_StorageUri",
                table: "Files",
                column: "StorageUri",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FileScopes_OwnerUserId",
                table: "FileScopes",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FileScopes_ProjectId_OwnerUserId",
                table: "FileScopes",
                columns: new[] { "ProjectId", "OwnerUserId" });

            migrationBuilder.CreateIndex(
                name: "UX_FileScopes_FileId",
                table: "FileScopes",
                column: "FileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Flights_DroneDeviceId",
                table: "Flights",
                column: "DroneDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_Flights_OperatorUserId",
                table: "Flights",
                column: "OperatorUserId");

            migrationBuilder.CreateIndex(
                name: "UX_Flights_SurveyFlightNo",
                table: "Flights",
                columns: new[] { "SurveyId", "FlightNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GeometryLocationImpactDecisions_ActorId",
                table: "GeometryLocationImpactDecisions",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_GeometryLocationImpactDecisions_ImpactId_TaskId",
                table: "GeometryLocationImpactDecisions",
                columns: new[] { "ImpactId", "TaskId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GeometryLocationImpacts_NewRouteVersionId",
                table: "GeometryLocationImpacts",
                column: "NewRouteVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_GeometryLocationImpacts_PreviousRouteVersionId_NewRouteVersionId",
                table: "GeometryLocationImpacts",
                columns: new[] { "PreviousRouteVersionId", "NewRouteVersionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GeometryLocationImpacts_ProjectId",
                table: "GeometryLocationImpacts",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_GeometryLocationImpacts_RecordedBy",
                table: "GeometryLocationImpacts",
                column: "RecordedBy");

            migrationBuilder.CreateIndex(
                name: "IX_GeometryMapPublications_CrsProfileRevisionId_ProjectId",
                table: "GeometryMapPublications",
                columns: new[] { "CrsProfileRevisionId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_GeometryMapPublications_LayoutRevisionId",
                table: "GeometryMapPublications",
                column: "LayoutRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_GeometryMapPublications_ProjectId_PublishedAt_Id",
                table: "GeometryMapPublications",
                columns: new[] { "ProjectId", "PublishedAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_GeometryMapPublications_PublishedBy",
                table: "GeometryMapPublications",
                column: "PublishedBy");

            migrationBuilder.CreateIndex(
                name: "IX_GeometryMapPublications_RouteVersionId",
                table: "GeometryMapPublications",
                column: "RouteVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_GeometryMapPublications_SegmentSetId",
                table: "GeometryMapPublications",
                column: "SegmentSetId");

            migrationBuilder.CreateIndex(
                name: "IX_GroundTruthMeasurements_DefectId",
                table: "GroundTruthMeasurements",
                column: "DefectId");

            migrationBuilder.CreateIndex(
                name: "IX_GroundTruthMeasurements_EvidenceFileId",
                table: "GroundTruthMeasurements",
                column: "EvidenceFileId");

            migrationBuilder.CreateIndex(
                name: "IX_GroundTruthMeasurements_RoadVersionType",
                table: "GroundTruthMeasurements",
                columns: new[] { "RoadSectionVersionId", "MeasurementType" });

            migrationBuilder.CreateIndex(
                name: "IX_GroundTruthMeasurements_SurveyId",
                table: "GroundTruthMeasurements",
                column: "SurveyId");

            migrationBuilder.CreateIndex(
                name: "UX_GroundTruthMeasurements_SessionSample",
                table: "GroundTruthMeasurements",
                columns: new[] { "FieldInspectionSessionId", "SampleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_H6NotificationAudits_DedupKey",
                table: "H6NotificationAudits",
                column: "DedupKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_H6NotificationAudits_NotificationId",
                table: "H6NotificationAudits",
                column: "NotificationId");

            migrationBuilder.CreateIndex(
                name: "IX_H6NotificationAudits_OutboxMessageId",
                table: "H6NotificationAudits",
                column: "OutboxMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_H6NotificationAudits_PreviousAuditId",
                table: "H6NotificationAudits",
                column: "PreviousAuditId");

            migrationBuilder.CreateIndex(
                name: "IX_H6NotificationAudits_ProjectId",
                table: "H6NotificationAudits",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_H6NotificationCalendar_ClockId_ScheduledAtUtc",
                table: "H6NotificationCalendar",
                columns: new[] { "ClockId", "ScheduledAtUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_H6NotificationCalendar_OutboxMessageId",
                table: "H6NotificationCalendar",
                column: "OutboxMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_H6NotificationDeliveries_NotificationId",
                table: "H6NotificationDeliveries",
                column: "NotificationId",
                unique: true,
                filter: "[NotificationId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_H6NotificationDeliveries_OccurrenceId_RecipientKey",
                table: "H6NotificationDeliveries",
                columns: new[] { "OccurrenceId", "RecipientKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_H6NotificationDeliveries_RecipientUserId",
                table: "H6NotificationDeliveries",
                column: "RecipientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_H6NotificationDeliveryAttempts_DeliveryId",
                table: "H6NotificationDeliveryAttempts",
                column: "DeliveryId");

            migrationBuilder.CreateIndex(
                name: "IX_H6NotificationEventReceipts_OccurrenceId",
                table: "H6NotificationEventReceipts",
                column: "OccurrenceId");

            migrationBuilder.CreateIndex(
                name: "IX_H6NotificationOccurrences_OccurrenceKey",
                table: "H6NotificationOccurrences",
                column: "OccurrenceKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_H6NotificationOccurrences_ProjectId",
                table: "H6NotificationOccurrences",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_H6NotificationOccurrences_SourceEventId",
                table: "H6NotificationOccurrences",
                column: "SourceEventId");

            migrationBuilder.CreateIndex(
                name: "IX_H6NotificationScopes_CurrentAuditId",
                table: "H6NotificationScopes",
                column: "CurrentAuditId");

            migrationBuilder.CreateIndex(
                name: "IX_H6NotificationScopes_OccurrenceId",
                table: "H6NotificationScopes",
                column: "OccurrenceId");

            migrationBuilder.CreateIndex(
                name: "IX_H6NotificationScopes_ProjectId",
                table: "H6NotificationScopes",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_HandoverDocuments_AcceptedByUserId",
                table: "HandoverDocuments",
                column: "AcceptedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_HandoverDocuments_FileId",
                table: "HandoverDocuments",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "UX_HandoverDocuments_ProjectId_DocumentNo",
                table: "HandoverDocuments",
                columns: new[] { "ProjectId", "DocumentNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyRecords_OperationId",
                table: "IdempotencyRecords",
                column: "OperationId");

            migrationBuilder.CreateIndex(
                name: "UX_IdempotencyRecords_ScopeKey",
                table: "IdempotencyRecords",
                columns: new[] { "ActorUserId", "ProjectId", "Operation", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IncidentCases_GeometrySegmentSetId_GeometryRouteVersionId",
                table: "IncidentCases",
                columns: new[] { "GeometrySegmentSetId", "GeometryRouteVersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_IncidentCases_LinkedTargetCaseId",
                table: "IncidentCases",
                column: "LinkedTargetCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentCases_ProjectId",
                table: "IncidentCases",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_LD06ActionEvidence_FileId",
                table: "LD06ActionEvidence",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_LD06LifecycleActions_ActorId",
                table: "LD06LifecycleActions",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_LD06LifecycleActions_DefectId",
                table: "LD06LifecycleActions",
                column: "DefectId");

            migrationBuilder.CreateIndex(
                name: "IX_LD06LifecycleActions_LinkedDefectId",
                table: "LD06LifecycleActions",
                column: "LinkedDefectId");

            migrationBuilder.CreateIndex(
                name: "IX_LD06LifecycleActions_ObligationId",
                table: "LD06LifecycleActions",
                column: "ObligationId");

            migrationBuilder.CreateIndex(
                name: "IX_LD06LifecycleActions_PriorRepairDecisionId",
                table: "LD06LifecycleActions",
                column: "PriorRepairDecisionId");

            migrationBuilder.CreateIndex(
                name: "IX_LD06LifecycleActions_ProjectId",
                table: "LD06LifecycleActions",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_LD06LifecycleActions_ReceivingProjectId",
                table: "LD06LifecycleActions",
                column: "ReceivingProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_LD06LifecycleActions_SourceActionId_Kind",
                table: "LD06LifecycleActions",
                columns: new[] { "SourceActionId", "Kind" },
                unique: true,
                filter: "[SourceActionId] IS NOT NULL AND [Kind] IN (2,7)");

            migrationBuilder.CreateIndex(
                name: "IX_MeasurementValidationSamples_DerivedMeasurementId",
                table: "MeasurementValidationSamples",
                column: "DerivedMeasurementId");

            migrationBuilder.CreateIndex(
                name: "IX_MeasurementValidationSamples_GroundTruthMeasurementId",
                table: "MeasurementValidationSamples",
                column: "GroundTruthMeasurementId");

            migrationBuilder.CreateIndex(
                name: "UX_MeasurementValidationSamples_RunPair",
                table: "MeasurementValidationSamples",
                columns: new[] { "ValidationRunId", "GroundTruthMeasurementId", "DerivedMeasurementId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NativeRouteVersionFacts_CrsProfileRevisionId_ProjectId",
                table: "NativeRouteVersionFacts",
                columns: new[] { "CrsProfileRevisionId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_NativeRouteVersionFacts_ParentRouteVersionId",
                table: "NativeRouteVersionFacts",
                column: "ParentRouteVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_NativeRouteVersionFacts_RouteSystemId_ProjectId",
                table: "NativeRouteVersionFacts",
                columns: new[] { "RouteSystemId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RecipientOccurredAtId",
                table: "Notifications",
                columns: new[] { "RecipientUserId", "OccurredAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RecipientUserId_ReadAt",
                table: "Notifications",
                columns: new[] { "RecipientUserId", "ReadAt" });

            migrationBuilder.CreateIndex(
                name: "UX_Notifications_RecipientSourceEvent",
                table: "Notifications",
                columns: new[] { "RecipientUserId", "SourceEntityType", "SourceEntityId", "EventType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ObligationResponsibilities_AcceptanceActionId",
                table: "ObligationResponsibilities",
                column: "AcceptanceActionId");

            migrationBuilder.CreateIndex(
                name: "IX_ObligationResponsibilities_CurrentProjectId",
                table: "ObligationResponsibilities",
                column: "CurrentProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ObligationResponsibilities_OriginProjectId",
                table: "ObligationResponsibilities",
                column: "OriginProjectId");

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

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_CorrelationId",
                table: "OutboxMessages",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_DeliveryStatus_NextAttempt",
                table: "OutboxMessages",
                columns: new[] { "DeliveryStatus", "NextAttemptAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_OccurredAtUtc",
                table: "OutboxMessages",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordRecoveryRequests_TargetUserId_RequestedAtUtc",
                table: "PasswordRecoveryRequests",
                columns: new[] { "TargetUserId", "RequestedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetLogs_OccurredAt",
                table: "PasswordResetLogs",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetLogs_PerformedByUserId",
                table: "PasswordResetLogs",
                column: "PerformedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetLogs_TargetUserId",
                table: "PasswordResetLogs",
                column: "TargetUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PavementLayoutRevisions_CreatedBy",
                table: "PavementLayoutRevisions",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_PavementLayoutRevisions_CrsProfileRevisionId_ProjectId",
                table: "PavementLayoutRevisions",
                columns: new[] { "CrsProfileRevisionId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_PavementLayoutRevisions_ProjectId",
                table: "PavementLayoutRevisions",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_PavementLayoutRevisions_RouteVersionId_CreatedAt_Id",
                table: "PavementLayoutRevisions",
                columns: new[] { "RouteVersionId", "CreatedAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_PavementLayoutRevisions_SegmentSetId",
                table: "PavementLayoutRevisions",
                column: "SegmentSetId");

            migrationBuilder.CreateIndex(
                name: "IX_PavementLayoutRevisions_SourcePlanId",
                table: "PavementLayoutRevisions",
                column: "SourcePlanId");

            migrationBuilder.CreateIndex(
                name: "IX_PavementSourceFileReferences_FileId",
                table: "PavementSourceFileReferences",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_PavementSourceFileReferences_LayoutRevisionId_FileId",
                table: "PavementSourceFileReferences",
                columns: new[] { "LayoutRevisionId", "FileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingAttempts_JobId",
                table: "ProcessingAttempts",
                column: "ProcessingJobId");

            migrationBuilder.CreateIndex(
                name: "UX_ProcessingAttempts_JobAttemptNo",
                table: "ProcessingAttempts",
                columns: new[] { "ProcessingJobId", "AttemptNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_ProcessingBlocks_DataVersionBlockNo",
                table: "ProcessingBlocks",
                columns: new[] { "SurveyDataVersionId", "BlockNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingJobs_Block",
                table: "ProcessingJobs",
                column: "ProcessingBlockId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingJobs_ModelVersionId",
                table: "ProcessingJobs",
                column: "ModelVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingJobs_ProjectStatus",
                table: "ProcessingJobs",
                columns: new[] { "ProjectId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectLifecycleHistory_ActorId",
                table: "ProjectLifecycleHistory",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectLifecycleHistory_DefectId",
                table: "ProjectLifecycleHistory",
                column: "DefectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectLifecycleHistory_LinkedDefectId",
                table: "ProjectLifecycleHistory",
                column: "LinkedDefectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectLifecycleHistory_ObligationId",
                table: "ProjectLifecycleHistory",
                column: "ObligationId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectLifecycleHistory_OperationalClosureId_ProjectId",
                table: "ProjectLifecycleHistory",
                columns: new[] { "OperationalClosureId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectLifecycleHistory_ProjectId",
                table: "ProjectLifecycleHistory",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectLifecycleHistory_ReceiverId",
                table: "ProjectLifecycleHistory",
                column: "ReceiverId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMembers_ProjectId_UserId",
                table: "ProjectMembers",
                columns: new[] { "ProjectId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMembers_RoleCode",
                table: "ProjectMembers",
                column: "RoleCode");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMembers_UserId",
                table: "ProjectMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "UX_ProjectMembers_ActivePrimaryProjectManager",
                table: "ProjectMembers",
                column: "ProjectId",
                unique: true,
                filter: "[IsPrimary] = 1 AND [Status] = 1");

            migrationBuilder.CreateIndex(
                name: "UX_Projects_ProjectCode",
                table: "Projects",
                column: "ProjectCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QualityChecks_InitiatedByUserId",
                table: "QualityChecks",
                column: "InitiatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityChecks_SurveyDataVersionId",
                table: "QualityChecks",
                column: "SurveyDataVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityChecks_SurveyFileId",
                table: "QualityChecks",
                column: "SurveyFileId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_SessionId",
                table: "RefreshTokens",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "UX_RefreshTokens_TokenHash",
                table: "RefreshTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RepairActualScopes_ObligationId",
                table: "RepairActualScopes",
                column: "ObligationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RepairActualScopes_PhysicalRoadId",
                table: "RepairActualScopes",
                column: "PhysicalRoadId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairAssessmentEvidence_ActualUploaderId",
                table: "RepairAssessmentEvidence",
                column: "ActualUploaderId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairAssessmentEvidence_AssessmentId_CaptureOriginId",
                table: "RepairAssessmentEvidence",
                columns: new[] { "AssessmentId", "CaptureOriginId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RepairAssessmentEvidence_FileId",
                table: "RepairAssessmentEvidence",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairAssessmentEvidence_OriginalActorId",
                table: "RepairAssessmentEvidence",
                column: "OriginalActorId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairAssessmentEvidence_ReuseDecisionId",
                table: "RepairAssessmentEvidence",
                column: "ReuseDecisionId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairAssessmentMeasurements_MeasurementId",
                table: "RepairAssessmentMeasurements",
                column: "MeasurementId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairAttemptEvidence_FileId",
                table: "RepairAttemptEvidence",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairAttemptReviews_ActorId",
                table: "RepairAttemptReviews",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairAttemptReviews_AttemptId",
                table: "RepairAttemptReviews",
                column: "AttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairAttemptReviews_BindingId",
                table: "RepairAttemptReviews",
                column: "BindingId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairAttemptReviews_IntakeLinkId",
                table: "RepairAttemptReviews",
                column: "IntakeLinkId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairAttemptReviews_ItemId",
                table: "RepairAttemptReviews",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairAttemptReviews_ProjectId",
                table: "RepairAttemptReviews",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairAttemptReviews_SubmissionId",
                table: "RepairAttemptReviews",
                column: "SubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairAttempts_AssignmentId",
                table: "RepairAttempts",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairAttempts_CrewId",
                table: "RepairAttempts",
                column: "CrewId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairAttempts_DefectId",
                table: "RepairAttempts",
                column: "DefectId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairAttempts_ItemId",
                table: "RepairAttempts",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairAttempts_ObligationId",
                table: "RepairAttempts",
                column: "ObligationId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairAttempts_ProjectId_OriginId",
                table: "RepairAttempts",
                columns: new[] { "ProjectId", "OriginId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RepairAttempts_TaskId",
                table: "RepairAttempts",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairAttemptSubmissionLinks_AttemptId",
                table: "RepairAttemptSubmissionLinks",
                column: "AttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairAttemptSubmissionLinks_BindingId",
                table: "RepairAttemptSubmissionLinks",
                column: "BindingId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairAttemptSubmissionLinks_ExecutionFinishId",
                table: "RepairAttemptSubmissionLinks",
                column: "ExecutionFinishId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairAttemptSubmissionLinks_FormalRootSubmissionId",
                table: "RepairAttemptSubmissionLinks",
                column: "FormalRootSubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairAttemptSubmissionLinks_ItemId",
                table: "RepairAttemptSubmissionLinks",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairAttemptSubmissionLinks_PreviousLinkId",
                table: "RepairAttemptSubmissionLinks",
                column: "PreviousLinkId",
                unique: true,
                filter: "[PreviousLinkId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RepairAttemptSubmissionLinks_ProjectId",
                table: "RepairAttemptSubmissionLinks",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairAttemptSubmissionLinks_ReviewClockId",
                table: "RepairAttemptSubmissionLinks",
                column: "ReviewClockId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairAttemptSubmissionLinks_SubmissionId",
                table: "RepairAttemptSubmissionLinks",
                column: "SubmissionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RepairCorrectionEvidence_FileId",
                table: "RepairCorrectionEvidence",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairDangerAcknowledgements_ActorId",
                table: "RepairDangerAcknowledgements",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairDangerAcknowledgements_MonitoringId",
                table: "RepairDangerAcknowledgements",
                column: "MonitoringId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairDangerAcknowledgements_WarningId",
                table: "RepairDangerAcknowledgements",
                column: "WarningId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RepairDangerWarnings_MonitoringId_SourceId",
                table: "RepairDangerWarnings",
                columns: new[] { "MonitoringId", "SourceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RepairDangerWarnings_ResponsibleActorId",
                table: "RepairDangerWarnings",
                column: "ResponsibleActorId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairDecisions_ActorId",
                table: "RepairDecisions",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairDecisions_DefectId",
                table: "RepairDecisions",
                column: "DefectId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairDecisions_ItemId",
                table: "RepairDecisions",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairDecisions_ObligationId",
                table: "RepairDecisions",
                column: "ObligationId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairDecisions_PreviousObligationHeadDecisionId",
                table: "RepairDecisions",
                column: "PreviousObligationHeadDecisionId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairDecisions_SupersedesDecisionId",
                table: "RepairDecisions",
                column: "SupersedesDecisionId",
                unique: true,
                filter: "[SupersedesDecisionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RepairEligibilityAssessments_AssessmentId",
                table: "RepairEligibilityAssessments",
                column: "AssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairEligibilityAssessments_BindingId",
                table: "RepairEligibilityAssessments",
                column: "BindingId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairEligibilityAssessments_ItemId",
                table: "RepairEligibilityAssessments",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairEligibilityAssessments_PolicyRevisionId",
                table: "RepairEligibilityAssessments",
                column: "PolicyRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairEligibilityAssessments_ProjectId",
                table: "RepairEligibilityAssessments",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairEligibilityAssessments_RoadSectionId",
                table: "RepairEligibilityAssessments",
                column: "RoadSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairEligibilityHandoverSources_FileId",
                table: "RepairEligibilityHandoverSources",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairEligibilityHandoverSources_HandoverDocumentId",
                table: "RepairEligibilityHandoverSources",
                column: "HandoverDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairEligibilityWarrantySources_SourceDocumentId",
                table: "RepairEligibilityWarrantySources",
                column: "SourceDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairEligibilityWarrantySources_WarrantyId",
                table: "RepairEligibilityWarrantySources",
                column: "WarrantyId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairExecutionAuthorizations_AssignmentId",
                table: "RepairExecutionAuthorizations",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairExecutionAuthorizations_CrewId",
                table: "RepairExecutionAuthorizations",
                column: "CrewId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairExecutionAuthorizations_DefectId",
                table: "RepairExecutionAuthorizations",
                column: "DefectId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairExecutionAuthorizations_IssuedBy",
                table: "RepairExecutionAuthorizations",
                column: "IssuedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RepairExecutionAuthorizations_PolicyRevisionId",
                table: "RepairExecutionAuthorizations",
                column: "PolicyRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairExecutionAuthorizations_ProjectId",
                table: "RepairExecutionAuthorizations",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairExecutionAuthorizations_TaskId",
                table: "RepairExecutionAuthorizations",
                column: "TaskId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RepairExecutionFinishes_BindingId",
                table: "RepairExecutionFinishes",
                column: "BindingId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairExecutionFinishes_ExecutionStartId",
                table: "RepairExecutionFinishes",
                column: "ExecutionStartId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RepairExecutionFinishes_ItemId",
                table: "RepairExecutionFinishes",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairExecutionFinishes_OperationOriginId",
                table: "RepairExecutionFinishes",
                column: "OperationOriginId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairExecutionFinishes_OriginalActorId",
                table: "RepairExecutionFinishes",
                column: "OriginalActorId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairExecutionFinishes_ProjectId_OriginId",
                table: "RepairExecutionFinishes",
                columns: new[] { "ProjectId", "OriginId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RepairExecutionStarts_AssessmentId",
                table: "RepairExecutionStarts",
                column: "AssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairExecutionStarts_BindingId",
                table: "RepairExecutionStarts",
                column: "BindingId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairExecutionStarts_EligibilityAssessmentId",
                table: "RepairExecutionStarts",
                column: "EligibilityAssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairExecutionStarts_FirstStartId",
                table: "RepairExecutionStarts",
                column: "FirstStartId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairExecutionStarts_ItemId",
                table: "RepairExecutionStarts",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairExecutionStarts_OperationOriginId",
                table: "RepairExecutionStarts",
                column: "OperationOriginId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairExecutionStarts_OriginalActorId",
                table: "RepairExecutionStarts",
                column: "OriginalActorId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairExecutionStarts_ProjectId_OriginId",
                table: "RepairExecutionStarts",
                columns: new[] { "ProjectId", "OriginId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RepairFieldTaskBindings_AssignedBy",
                table: "RepairFieldTaskBindings",
                column: "AssignedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RepairFieldTaskBindings_AssignmentId",
                table: "RepairFieldTaskBindings",
                column: "AssignmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RepairFieldTaskBindings_AuthorizationId",
                table: "RepairFieldTaskBindings",
                column: "AuthorizationId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairFieldTaskBindings_CrewId",
                table: "RepairFieldTaskBindings",
                column: "CrewId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairFieldTaskBindings_CrsProfileRevisionId",
                table: "RepairFieldTaskBindings",
                column: "CrsProfileRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairFieldTaskBindings_DefectId",
                table: "RepairFieldTaskBindings",
                column: "DefectId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairFieldTaskBindings_ItemId",
                table: "RepairFieldTaskBindings",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairFieldTaskBindings_LayoutRevisionId",
                table: "RepairFieldTaskBindings",
                column: "LayoutRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairFieldTaskBindings_MapPublicationId",
                table: "RepairFieldTaskBindings",
                column: "MapPublicationId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairFieldTaskBindings_ObligationId",
                table: "RepairFieldTaskBindings",
                column: "ObligationId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairFieldTaskBindings_PolicyRevisionId",
                table: "RepairFieldTaskBindings",
                column: "PolicyRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairFieldTaskBindings_ProjectId",
                table: "RepairFieldTaskBindings",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairFieldTaskBindings_RouteVersionId",
                table: "RepairFieldTaskBindings",
                column: "RouteVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairFieldTaskBindings_SegmentSetId",
                table: "RepairFieldTaskBindings",
                column: "SegmentSetId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairFieldTaskBindings_TaskId",
                table: "RepairFieldTaskBindings",
                column: "TaskId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RepairItemLifecycleEvents_ActorId",
                table: "RepairItemLifecycleEvents",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairItemLifecycleEvents_AttemptId",
                table: "RepairItemLifecycleEvents",
                column: "AttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairItemLifecycleEvents_BindingId",
                table: "RepairItemLifecycleEvents",
                column: "BindingId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairItemLifecycleEvents_DecisionId",
                table: "RepairItemLifecycleEvents",
                column: "DecisionId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairItemLifecycleEvents_DefectId",
                table: "RepairItemLifecycleEvents",
                column: "DefectId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairItemLifecycleEvents_ItemId",
                table: "RepairItemLifecycleEvents",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairItemLifecycleEvents_ObligationId",
                table: "RepairItemLifecycleEvents",
                column: "ObligationId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairItemLifecycleEvents_ProjectId",
                table: "RepairItemLifecycleEvents",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairItemLifecycleEvents_ReviewId",
                table: "RepairItemLifecycleEvents",
                column: "ReviewId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairItemLifecycleEvents_SubmissionId",
                table: "RepairItemLifecycleEvents",
                column: "SubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairItems_CrewId",
                table: "RepairItems",
                column: "CrewId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairItems_CurrentAssessmentId",
                table: "RepairItems",
                column: "CurrentAssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairItems_CurrentAttemptId",
                table: "RepairItems",
                column: "CurrentAttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairItems_CurrentBindingId",
                table: "RepairItems",
                column: "CurrentBindingId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairItems_CurrentExecutionFinishId",
                table: "RepairItems",
                column: "CurrentExecutionFinishId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairItems_CurrentExecutionStartId",
                table: "RepairItems",
                column: "CurrentExecutionStartId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairItems_CurrentIntakeLinkId",
                table: "RepairItems",
                column: "CurrentIntakeLinkId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairItems_CurrentReviewId",
                table: "RepairItems",
                column: "CurrentReviewId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairItems_DefectId",
                table: "RepairItems",
                column: "DefectId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairItems_EffectiveDecisionId",
                table: "RepairItems",
                column: "EffectiveDecisionId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairItems_EffectiveIntakeSubmissionId",
                table: "RepairItems",
                column: "EffectiveIntakeSubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairItems_ObligationId",
                table: "RepairItems",
                column: "ObligationId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairItems_PackageId",
                table: "RepairItems",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairItems_PredecessorItemId",
                table: "RepairItems",
                column: "PredecessorItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairItems_ProjectId",
                table: "RepairItems",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairItems_SupersededByItemId",
                table: "RepairItems",
                column: "SupersededByItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairMeasurementAssessments_AssignmentId",
                table: "RepairMeasurementAssessments",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairMeasurementAssessments_BindingId",
                table: "RepairMeasurementAssessments",
                column: "BindingId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairMeasurementAssessments_FirstStartId",
                table: "RepairMeasurementAssessments",
                column: "FirstStartId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairMeasurementAssessments_FormalSourceSubmissionId",
                table: "RepairMeasurementAssessments",
                column: "FormalSourceSubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairMeasurementAssessments_ItemId",
                table: "RepairMeasurementAssessments",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairMeasurementAssessments_OperationOriginId",
                table: "RepairMeasurementAssessments",
                column: "OperationOriginId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairMeasurementAssessments_OriginalActorId",
                table: "RepairMeasurementAssessments",
                column: "OriginalActorId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairMeasurementAssessments_ProjectId_OriginId",
                table: "RepairMeasurementAssessments",
                columns: new[] { "ProjectId", "OriginId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RepairMeasurementAssessments_SessionId",
                table: "RepairMeasurementAssessments",
                column: "SessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RepairMeasurementAssessments_TaskId",
                table: "RepairMeasurementAssessments",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairNormalSuccessors_ActorId",
                table: "RepairNormalSuccessors",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairNormalSuccessors_ObligationId",
                table: "RepairNormalSuccessors",
                column: "ObligationId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairNormalSuccessors_ProjectId",
                table: "RepairNormalSuccessors",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairNormalSuccessors_SourceAssessmentId",
                table: "RepairNormalSuccessors",
                column: "SourceAssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairNormalSuccessors_SourceCancellationEventId",
                table: "RepairNormalSuccessors",
                column: "SourceCancellationEventId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairNormalSuccessors_SourceDecisionId",
                table: "RepairNormalSuccessors",
                column: "SourceDecisionId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairNormalSuccessors_SourceHandoverEventId",
                table: "RepairNormalSuccessors",
                column: "SourceHandoverEventId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairNormalSuccessors_SourceItemId",
                table: "RepairNormalSuccessors",
                column: "SourceItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RepairNormalSuccessors_TargetItemId",
                table: "RepairNormalSuccessors",
                column: "TargetItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RepairObligationResolutionEvents_ObligationId",
                table: "RepairObligationResolutionEvents",
                column: "ObligationId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairObligationResolutionEvents_PreviousHeadDecisionId",
                table: "RepairObligationResolutionEvents",
                column: "PreviousHeadDecisionId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairObligationResolutionEvents_SupersedesDecisionId",
                table: "RepairObligationResolutionEvents",
                column: "SupersedesDecisionId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairObligations_CurrentRepairItemId",
                table: "RepairObligations",
                column: "CurrentRepairItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairObligations_DefectId",
                table: "RepairObligations",
                column: "DefectId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairObligations_EffectiveResolutionDecisionId",
                table: "RepairObligations",
                column: "EffectiveResolutionDecisionId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairObligations_EffectiveResolutionHeadDecisionId",
                table: "RepairObligations",
                column: "EffectiveResolutionHeadDecisionId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairObligations_OriginalCrewFirstStartId",
                table: "RepairObligations",
                column: "OriginalCrewFirstStartId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairObligations_PackageId",
                table: "RepairObligations",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairObligations_ProjectId",
                table: "RepairObligations",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairPackages_DefectId",
                table: "RepairPackages",
                column: "DefectId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairPackages_ProjectId",
                table: "RepairPackages",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairPolicyDraftChanges_ActorId",
                table: "RepairPolicyDraftChanges",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairPolicyDraftChanges_DraftId",
                table: "RepairPolicyDraftChanges",
                column: "DraftId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairPolicyDrafts_CurrentChangeId",
                table: "RepairPolicyDrafts",
                column: "CurrentChangeId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairPolicyDrafts_ProjectId",
                table: "RepairPolicyDrafts",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairPolicyDrafts_PublishedRevisionId",
                table: "RepairPolicyDrafts",
                column: "PublishedRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairPolicyRevisions_ProjectId_Revision",
                table: "RepairPolicyRevisions",
                columns: new[] { "ProjectId", "Revision" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RepairPolicyRevisions_PublishedBy",
                table: "RepairPolicyRevisions",
                column: "PublishedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RepairPolicyRevocations_ActorId",
                table: "RepairPolicyRevocations",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairPolicyRevocations_PolicyRevisionId",
                table: "RepairPolicyRevocations",
                column: "PolicyRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairReviewRequests_ActorId",
                table: "RepairReviewRequests",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairReviewRequests_DecisionId",
                table: "RepairReviewRequests",
                column: "DecisionId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairReviewRequests_ItemId",
                table: "RepairReviewRequests",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairReviewRequests_ProjectId",
                table: "RepairReviewRequests",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairSafetyActionSources_ActorId",
                table: "RepairSafetyActionSources",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairSafetyActionSources_BindingId",
                table: "RepairSafetyActionSources",
                column: "BindingId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairSafetyActionSources_ItemId",
                table: "RepairSafetyActionSources",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairSafetyActionSources_MeasureId_Kind_OriginId",
                table: "RepairSafetyActionSources",
                columns: new[] { "MeasureId", "Kind", "OriginId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RepairSafetyActionSources_ProjectId",
                table: "RepairSafetyActionSources",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairSafetyCheckEvidence_ActualUploaderId",
                table: "RepairSafetyCheckEvidence",
                column: "ActualUploaderId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairSafetyCheckEvidence_FileId",
                table: "RepairSafetyCheckEvidence",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairSafetyChecks_ActorId",
                table: "RepairSafetyChecks",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairSafetyChecks_MeasureId",
                table: "RepairSafetyChecks",
                column: "MeasureId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairSafetyMonitoring_CurrentCheckId",
                table: "RepairSafetyMonitoring",
                column: "CurrentCheckId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairSafetyMonitoring_FormalObligationId",
                table: "RepairSafetyMonitoring",
                column: "FormalObligationId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairSafetyMonitoring_MeasureId",
                table: "RepairSafetyMonitoring",
                column: "MeasureId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RepairSafetyMonitoring_SafetyObligationId",
                table: "RepairSafetyMonitoring",
                column: "SafetyObligationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RepairSafetyResponsibilityTransfers_ChangedBy",
                table: "RepairSafetyResponsibilityTransfers",
                column: "ChangedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RepairSafetyResponsibilityTransfers_NextActorId",
                table: "RepairSafetyResponsibilityTransfers",
                column: "NextActorId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairSafetyResponsibilityTransfers_PreviousActorId",
                table: "RepairSafetyResponsibilityTransfers",
                column: "PreviousActorId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairTemporarySafetyMeasures_DefectId",
                table: "RepairTemporarySafetyMeasures",
                column: "DefectId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairTemporarySafetyMeasures_FormalRepairObligationId",
                table: "RepairTemporarySafetyMeasures",
                column: "FormalRepairObligationId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairTemporarySafetyMeasures_Id_CurrentResponsibilityTransferId",
                table: "RepairTemporarySafetyMeasures",
                columns: new[] { "Id", "CurrentResponsibilityTransferId" });

            migrationBuilder.CreateIndex(
                name: "IX_RepairTemporarySafetyMeasures_InstalledBy",
                table: "RepairTemporarySafetyMeasures",
                column: "InstalledBy");

            migrationBuilder.CreateIndex(
                name: "IX_RepairTemporarySafetyMeasures_ProjectId",
                table: "RepairTemporarySafetyMeasures",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairTemporarySafetyMeasures_ResponsibleActorId",
                table: "RepairTemporarySafetyMeasures",
                column: "ResponsibleActorId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairWorkHandovers_FromActorId",
                table: "RepairWorkHandovers",
                column: "FromActorId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairWorkHandovers_ItemId",
                table: "RepairWorkHandovers",
                column: "ItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RepairWorkHandovers_ToActorId",
                table: "RepairWorkHandovers",
                column: "ToActorId");

            migrationBuilder.CreateIndex(
                name: "IX_ReporterRegistrationIntents_Email",
                table: "ReporterRegistrationIntents",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_ReporterRegistrationIntents_UserId",
                table: "ReporterRegistrationIntents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportOriginalEvidence_FileId",
                table: "ReportOriginalEvidence",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportOriginalEvidence_Id_ReportId",
                table: "ReportOriginalEvidence",
                columns: new[] { "Id", "ReportId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReportOriginalEvidence_OwnerUserId",
                table: "ReportOriginalEvidence",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportOriginalEvidence_ReportId_OwnerUserId",
                table: "ReportOriginalEvidence",
                columns: new[] { "ReportId", "OwnerUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ReporterUserId",
                table: "Reports",
                column: "ReporterUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportSupplementEvidence_FileId",
                table: "ReportSupplementEvidence",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportSupplementEvidence_Id_ReportId",
                table: "ReportSupplementEvidence",
                columns: new[] { "Id", "ReportId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReportSupplementEvidence_OwnerUserId",
                table: "ReportSupplementEvidence",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportSupplementEvidence_ReportId_OwnerUserId",
                table: "ReportSupplementEvidence",
                columns: new[] { "ReportId", "OwnerUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReportSupplementEvidence_SupplementId_ReportId",
                table: "ReportSupplementEvidence",
                columns: new[] { "SupplementId", "ReportId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReportSupplements_ReportId",
                table: "ReportSupplements",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_RetentionBasisHeads_RevisionId_FileId",
                table: "RetentionBasisHeads",
                columns: new[] { "RevisionId", "FileId" });

            migrationBuilder.CreateIndex(
                name: "IX_RetentionBasisRevisions_ConfirmedBy",
                table: "RetentionBasisRevisions",
                column: "ConfirmedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RetentionBasisRevisions_FileId_Revision",
                table: "RetentionBasisRevisions",
                columns: new[] { "FileId", "Revision" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RetentionBasisRevisions_SupersedesId_FileId",
                table: "RetentionBasisRevisions",
                columns: new[] { "SupersedesId", "FileId" });

            migrationBuilder.CreateIndex(
                name: "IX_RetentionEvaluationItems_EvaluationId_FileId",
                table: "RetentionEvaluationItems",
                columns: new[] { "EvaluationId", "FileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RetentionEvaluationItems_FileId",
                table: "RetentionEvaluationItems",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_RetentionEvaluations_ProjectId",
                table: "RetentionEvaluations",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_RetentionEvaluations_RequestedBy",
                table: "RetentionEvaluations",
                column: "RequestedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RetentionEvaluations_Status_CreatedAt",
                table: "RetentionEvaluations",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RetentionHoldHistories_ActorId",
                table: "RetentionHoldHistories",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_RetentionHoldHistories_HoldId_OccurredAt",
                table: "RetentionHoldHistories",
                columns: new[] { "HoldId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RetentionHolds_CreatedBy",
                table: "RetentionHolds",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RetentionHolds_ReleasedBy",
                table: "RetentionHolds",
                column: "ReleasedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RetentionHolds_ScopeType_ScopeId_State",
                table: "RetentionHolds",
                columns: new[] { "ScopeType", "ScopeId", "State" });

            migrationBuilder.CreateIndex(
                name: "IX_RoadCoverageMappings_ActorId",
                table: "RoadCoverageMappings",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_RoadCoverageMappings_CoverageFileId",
                table: "RoadCoverageMappings",
                column: "CoverageFileId");

            migrationBuilder.CreateIndex(
                name: "IX_RoadCoverageMappings_HandoverDocumentId",
                table: "RoadCoverageMappings",
                column: "HandoverDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_RoadCoverageMappings_HandoverFileId",
                table: "RoadCoverageMappings",
                column: "HandoverFileId");

            migrationBuilder.CreateIndex(
                name: "IX_RoadCoverageMappings_ProjectId_RoadSectionId_LocationVersion",
                table: "RoadCoverageMappings",
                columns: new[] { "ProjectId", "RoadSectionId", "LocationVersion" });

            migrationBuilder.CreateIndex(
                name: "IX_RoadCoverageMappings_RoadSectionId",
                table: "RoadCoverageMappings",
                column: "RoadSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_RoadCoverageMappings_ScopeObligationId",
                table: "RoadCoverageMappings",
                column: "ScopeObligationId");

            migrationBuilder.CreateIndex(
                name: "IX_RoadCoverageMappings_SupersedesId",
                table: "RoadCoverageMappings",
                column: "SupersedesId",
                unique: true,
                filter: "[SupersedesId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RoadGeometryDrafts_ProjectId",
                table: "RoadGeometryDrafts",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_RoadGeometryDrafts_RoadSectionId",
                table: "RoadGeometryDrafts",
                column: "RoadSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_RoadGeometryMetadata_SourceDraftId",
                table: "RoadGeometryMetadata",
                column: "SourceDraftId");

            migrationBuilder.CreateIndex(
                name: "IX_RoadRouteSystems_ProjectId_Code",
                table: "RoadRouteSystems",
                columns: new[] { "ProjectId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_RoadSections_ProjectId_Code",
                table: "RoadSections",
                columns: new[] { "ProjectId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoadSectionVersions_CrsProfileRevisionId",
                table: "RoadSectionVersions",
                column: "CrsProfileRevisionId");

            migrationBuilder.CreateIndex(
                name: "UX_RoadSectionVersions_Current",
                table: "RoadSectionVersions",
                column: "RoadSectionId",
                unique: true,
                filter: "[IsCurrent] = 1");

            migrationBuilder.CreateIndex(
                name: "UX_RoadSectionVersions_RoadSectionId_VersionNo",
                table: "RoadSectionVersions",
                columns: new[] { "RoadSectionId", "VersionNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoadSegments_RoadSectionVersionId",
                table: "RoadSegments",
                column: "RoadSectionVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_RoadSegments_SegmentSetId_RoadSectionVersionId",
                table: "RoadSegments",
                columns: new[] { "SegmentSetId", "RoadSectionVersionId" });

            migrationBuilder.CreateIndex(
                name: "UX_RoadSegments_Set_Sequence",
                table: "RoadSegments",
                columns: new[] { "SegmentSetId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoadSegmentSets_RouteVersion_Status",
                table: "RoadSegmentSets",
                columns: new[] { "RoadSectionVersionId", "Status" });

            migrationBuilder.CreateIndex(
                name: "UX_RoadSegmentSets_CurrentPublished",
                table: "RoadSegmentSets",
                column: "RoadSectionVersionId",
                unique: true,
                filter: "[Status] = 'PUBLISHED'");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_UserId",
                table: "Sessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "UX_SeverityRuleVersions_ScopeVersion",
                table: "SeverityRuleVersions",
                columns: new[] { "StandardCode", "RoadTypeCode", "VersionNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SourceDecisions_AIDetectionSourceId",
                table: "SourceDecisions",
                column: "AIDetectionSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_SourceDecisions_Classification_CauseCategoryCode",
                table: "SourceDecisions",
                column: "Classification_CauseCategoryCode");

            migrationBuilder.CreateIndex(
                name: "IX_SourceDecisions_Classification_DefectTypeCode",
                table: "SourceDecisions",
                column: "Classification_DefectTypeCode");

            migrationBuilder.CreateIndex(
                name: "IX_SourceDecisions_Classification_RoadSectionVersionId",
                table: "SourceDecisions",
                column: "Classification_RoadSectionVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_SourceDecisions_Classification_SegmentId",
                table: "SourceDecisions",
                column: "Classification_SegmentId");

            migrationBuilder.CreateIndex(
                name: "IX_SourceDecisions_DecidedByUserId",
                table: "SourceDecisions",
                column: "DecidedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SourceDecisions_ProjectId",
                table: "SourceDecisions",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_SourceDecisions_ReportSourceId",
                table: "SourceDecisions",
                column: "ReportSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_SourceDecisions_SupersedesDecisionId",
                table: "SourceDecisions",
                column: "SupersedesDecisionId");

            migrationBuilder.CreateIndex(
                name: "IX_SourceDecisions_TargetDefectId",
                table: "SourceDecisions",
                column: "TargetDefectId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffInvitationProjects_ProjectId",
                table: "StaffInvitationProjects",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffInvitations_CreatedByUserId",
                table: "StaffInvitations",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffInvitations_Email",
                table: "StaffInvitations",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UX_StaffInvitations_TokenHash",
                table: "StaffInvitations",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplementarySurveyRequests_ApprovedByUserId",
                table: "SupplementarySurveyRequests",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplementarySurveyRequests_RequestedByUserId",
                table: "SupplementarySurveyRequests",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplementarySurveyRequests_SurveyRequestId",
                table: "SupplementarySurveyRequests",
                column: "SurveyRequestId");

            migrationBuilder.CreateIndex(
                name: "UX_SupplementarySurveyRequests_SurveyRound",
                table: "SupplementarySurveyRequests",
                columns: new[] { "SurveyId", "RoundNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SurveyAssignments_AssignedByUserId",
                table: "SurveyAssignments",
                column: "AssignedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyAssignments_OperatorUserId",
                table: "SurveyAssignments",
                column: "OperatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyAssignments_RequestEndedAt",
                table: "SurveyAssignments",
                columns: new[] { "SurveyRequestId", "EndedAt" });

            migrationBuilder.CreateIndex(
                name: "UX_SurveyAssignments_ActiveRequest",
                table: "SurveyAssignments",
                column: "SurveyRequestId",
                unique: true,
                filter: "[EndedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "UX_SurveyDataVersions_SurveyVersion",
                table: "SurveyDataVersions",
                columns: new[] { "SurveyId", "VersionNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SurveyFiles_FileId",
                table: "SurveyFiles",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyFiles_FlightId",
                table: "SurveyFiles",
                column: "FlightId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyFiles_SurveyId",
                table: "SurveyFiles",
                column: "SurveyId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyPlanPostponements_PlanId_PostponedAt",
                table: "SurveyPlanPostponements",
                columns: new[] { "SurveyPlanId", "PostponedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SurveyPlans_ProjectRoadStart",
                table: "SurveyPlans",
                columns: new[] { "ProjectId", "RoadSectionId", "PlannedStartAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SurveyPlans_RoadSectionId",
                table: "SurveyPlans",
                column: "RoadSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyPlans_RoadSectionVersionId",
                table: "SurveyPlans",
                column: "RoadSectionVersionId");

            migrationBuilder.CreateIndex(
                name: "UX_SurveyPlans_ActiveScope",
                table: "SurveyPlans",
                columns: new[] { "ProjectId", "RoadSectionId", "SurveyType" },
                unique: true,
                filter: "[Status] IN (1, 2, 3)");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyPlanScopes_RouteSectionVersionId",
                table: "SurveyPlanScopes",
                column: "RouteSectionVersionId");

            migrationBuilder.CreateIndex(
                name: "UX_SurveyPlanScopes_UniqueBand",
                table: "SurveyPlanScopes",
                columns: new[] { "SurveyPlanId", "RouteSectionVersionId", "SegmentSetId", "TargetBand" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SurveyRequests_ParentTaskId",
                table: "SurveyRequests",
                column: "ParentTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyRequests_ProjectRoadStatus",
                table: "SurveyRequests",
                columns: new[] { "ProjectId", "RoadSectionId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SurveyRequests_RequestedByUserId",
                table: "SurveyRequests",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyRequests_RoadSectionId",
                table: "SurveyRequests",
                column: "RoadSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyRequests_RoadSectionVersionId",
                table: "SurveyRequests",
                column: "RoadSectionVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyRequests_SupplementRequestId",
                table: "SurveyRequests",
                column: "SupplementRequestId");

            migrationBuilder.CreateIndex(
                name: "UX_SurveyRequests_ActivePlan",
                table: "SurveyRequests",
                column: "SurveyPlanId",
                unique: true,
                filter: "[SurveyPlanId] IS NOT NULL AND [Status] IN (1, 2, 4, 5, 6, 7, 10)");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyRequestScopes_RouteSectionVersionId",
                table: "SurveyRequestScopes",
                column: "RouteSectionVersionId");

            migrationBuilder.CreateIndex(
                name: "UX_SurveyRequestScopes_UniqueBand",
                table: "SurveyRequestScopes",
                columns: new[] { "SurveyRequestId", "RouteSectionVersionId", "SegmentSetId", "TargetBand" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Surveys_BaselineConfirmedByUserId",
                table: "Surveys",
                column: "BaselineConfirmedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Surveys_ProjectStatus",
                table: "Surveys",
                columns: new[] { "ProjectId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Surveys_RoadSectionVersionId",
                table: "Surveys",
                column: "RoadSectionVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_Surveys_SurveyRequestId",
                table: "Surveys",
                column: "SurveyRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingLabelReviews_ActorUserId",
                table: "TrainingLabelReviews",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingLabelReviews_RevisionId",
                table: "TrainingLabelReviews",
                column: "RevisionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingLabelReviews_RevisionId_LabelId",
                table: "TrainingLabelReviews",
                columns: new[] { "RevisionId", "LabelId" });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingLabelRevisions_DefectTypeCode",
                table: "TrainingLabelRevisions",
                column: "DefectTypeCode");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingLabelRevisions_FileId",
                table: "TrainingLabelRevisions",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingLabelRevisions_LabelId_Revision",
                table: "TrainingLabelRevisions",
                columns: new[] { "LabelId", "Revision" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingLabels_AIDetectionSourceId",
                table: "TrainingLabels",
                column: "AIDetectionSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingLabels_ProjectId_SourceKind_SourceId",
                table: "TrainingLabels",
                columns: new[] { "ProjectId", "SourceKind", "SourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingLabels_ReportSourceId",
                table: "TrainingLabels",
                column: "ReportSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_UploadMultipartSweeps_NextCheckAt",
                table: "UploadMultipartSweeps",
                column: "NextCheckAt");

            migrationBuilder.CreateIndex(
                name: "UX_UploadParts_Session_PartNumber",
                table: "UploadParts",
                columns: new[] { "UploadSessionId", "PartNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UploadSessions_FileId",
                table: "UploadSessions",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_UploadSessions_MultipartNextCheckAt",
                table: "UploadSessions",
                column: "MultipartNextCheckAt");

            migrationBuilder.CreateIndex(
                name: "IX_UploadSessions_OwnerUserId",
                table: "UploadSessions",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UploadSessions_Status_ExpiresAt",
                table: "UploadSessions",
                columns: new[] { "Status", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "UX_UploadSessions_ObjectKey",
                table: "UploadSessions",
                column: "ObjectKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleCode",
                table: "Users",
                column: "RoleCode");

            migrationBuilder.CreateIndex(
                name: "UX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true,
                filter: "[Email] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_Users_NormalizedUserName",
                table: "Users",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Users_UserName",
                table: "Users",
                column: "UserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ValidationRuns_ModelVersionId",
                table: "ValidationRuns",
                column: "ModelVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ValidationRuns_ProjectStatus",
                table: "ValidationRuns",
                columns: new[] { "ProjectId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Warranties_HandoverDocumentId",
                table: "Warranties",
                column: "HandoverDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Warranties_ProjectId",
                table: "Warranties",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Warranties_RoadSectionId",
                table: "Warranties",
                column: "RoadSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Warranties_SourceDocumentId",
                table: "Warranties",
                column: "SourceDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyReviewDigestDuties_ClockId",
                table: "WeeklyReviewDigestDuties",
                column: "ClockId");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyReviewDigests_ProjectId_ScheduledAtUtc_RecipientKey",
                table: "WeeklyReviewDigests",
                columns: new[] { "ProjectId", "ScheduledAtUtc", "RecipientKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyReviewDigests_RecipientId",
                table: "WeeklyReviewDigests",
                column: "RecipientId");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyReviewDigests_RecoveryPeriodId",
                table: "WeeklyReviewDigests",
                column: "RecoveryPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyReviewRecoveryPeriods_ProjectId_ScheduledAtUtc",
                table: "WeeklyReviewRecoveryPeriods",
                columns: new[] { "ProjectId", "ScheduledAtUtc" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AIDetections_ProcessingJobs_ProcessingJobId",
                table: "AIDetections",
                column: "ProcessingJobId",
                principalTable: "ProcessingJobs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Anh02AiDetectionProvenance_Anh02AiManifestFiles_RunId_SourceVideoFileId_SourceVideoFileVersion",
                table: "Anh02AiDetectionProvenance",
                columns: new[] { "RunId", "SourceVideoFileId", "SourceVideoFileVersion" },
                principalTable: "Anh02AiManifestFiles",
                principalColumns: new[] { "RunId", "FileId", "FileVersion" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Anh02AiDetectionProvenance_Anh02AiMockRuns_RunId",
                table: "Anh02AiDetectionProvenance",
                column: "RunId",
                principalTable: "Anh02AiMockRuns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Anh02AiDetectionProvenance_Anh02AiResultProvenance_ResultId_RunId",
                table: "Anh02AiDetectionProvenance",
                columns: new[] { "ResultId", "RunId" },
                principalTable: "Anh02AiResultProvenance",
                principalColumns: new[] { "Id", "RunId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Anh02AiManifestFiles_Anh02AiMockRuns_RunId",
                table: "Anh02AiManifestFiles",
                column: "RunId",
                principalTable: "Anh02AiMockRuns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Anh02AiMockRuns_Anh02AiResultProvenance_ResultId_Id",
                table: "Anh02AiMockRuns",
                columns: new[] { "ResultId", "Id" },
                principalTable: "Anh02AiResultProvenance",
                principalColumns: new[] { "Id", "RunId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Anh02AiMockRuns_ProcessingAttempts_AttemptId",
                table: "Anh02AiMockRuns",
                column: "AttemptId",
                principalTable: "ProcessingAttempts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Anh02AiMockRuns_ProcessingJobs_ProcessingJobId",
                table: "Anh02AiMockRuns",
                column: "ProcessingJobId",
                principalTable: "ProcessingJobs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Anh02AiMockRuns_SurveyDataVersions_DatasetVersionId",
                table: "Anh02AiMockRuns",
                column: "DatasetVersionId",
                principalTable: "SurveyDataVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Anh02AiResultProvenance_ProcessingAttempts_AttemptId",
                table: "Anh02AiResultProvenance",
                column: "AttemptId",
                principalTable: "ProcessingAttempts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Anh02AiResultProvenance_ProcessingJobs_ProcessingJobId",
                table: "Anh02AiResultProvenance",
                column: "ProcessingJobId",
                principalTable: "ProcessingJobs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Anh02ExportJobs_Anh02GeneratedArtifacts_ArtifactId",
                table: "Anh02ExportJobs",
                column: "ArtifactId",
                principalTable: "Anh02GeneratedArtifacts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BaselineCurrentPointers_BaselineSelectionItems_SelectionId",
                table: "BaselineCurrentPointers",
                column: "SelectionId",
                principalTable: "BaselineSelectionItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BaselineSelectionItems_DatasetAssessments_AssessmentId",
                table: "BaselineSelectionItems",
                column: "AssessmentId",
                principalTable: "DatasetAssessments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BaselineSelectionItems_SurveyDataVersions_DatasetId",
                table: "BaselineSelectionItems",
                column: "DatasetId",
                principalTable: "SurveyDataVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DatasetAssessmentItems_DatasetAssessments_AssessmentId",
                table: "DatasetAssessmentItems",
                column: "AssessmentId",
                principalTable: "DatasetAssessments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DatasetAssessments_SurveyDataVersions_DatasetId",
                table: "DatasetAssessments",
                column: "DatasetId",
                principalTable: "SurveyDataVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DefectStatisticsSources_RepairObligations_ObligationId",
                table: "DefectStatisticsSources",
                column: "ObligationId",
                principalTable: "RepairObligations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DefectVerificationLogs_FieldInspectionTasks_FieldInspectionTaskId",
                table: "DefectVerificationLogs",
                column: "FieldInspectionTaskId",
                principalTable: "FieldInspectionTasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DerivedMeasurements_SurveyDataVersions_SurveyDataVersionId",
                table: "DerivedMeasurements",
                column: "SurveyDataVersionId",
                principalTable: "SurveyDataVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FieldInspectionAssignments_FieldInspectionTasks_FieldInspectionTaskId",
                table: "FieldInspectionAssignments",
                column: "FieldInspectionTaskId",
                principalTable: "FieldInspectionTasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FieldInspectionEvidenceLinks_FieldInspectionSubmissions_SubmissionId",
                table: "FieldInspectionEvidenceLinks",
                column: "SubmissionId",
                principalTable: "FieldInspectionSubmissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FieldInspectionEvidenceLinks_FieldInspectionTasks_TaskId",
                table: "FieldInspectionEvidenceLinks",
                column: "TaskId",
                principalTable: "FieldInspectionTasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FieldInspectionEvidenceReuseDecisions_FieldInspectionTasks_TaskId",
                table: "FieldInspectionEvidenceReuseDecisions",
                column: "TaskId",
                principalTable: "FieldInspectionTasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FieldInspectionLocationProofs_FieldInspectionSubmissions_SubmissionId",
                table: "FieldInspectionLocationProofs",
                column: "SubmissionId",
                principalTable: "FieldInspectionSubmissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FieldInspectionLocationProofs_FieldInspectionTasks_TaskId",
                table: "FieldInspectionLocationProofs",
                column: "TaskId",
                principalTable: "FieldInspectionTasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FieldInspectionOperationOrigins_FieldInspectionTasks_TaskId",
                table: "FieldInspectionOperationOrigins",
                column: "TaskId",
                principalTable: "FieldInspectionTasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FieldInspectionReviews_FieldInspectionSubmissions_SubmissionId",
                table: "FieldInspectionReviews",
                column: "SubmissionId",
                principalTable: "FieldInspectionSubmissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FieldInspectionReviews_FieldInspectionTasks_TaskId",
                table: "FieldInspectionReviews",
                column: "TaskId",
                principalTable: "FieldInspectionTasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FieldInspectionSessions_FieldInspectionTasks_FieldInspectionTaskId",
                table: "FieldInspectionSessions",
                column: "FieldInspectionTaskId",
                principalTable: "FieldInspectionTasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FieldInspectionSessions_Surveys_SurveyId",
                table: "FieldInspectionSessions",
                column: "SurveyId",
                principalTable: "Surveys",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FieldInspectionSubmissions_FieldInspectionTasks_TaskId",
                table: "FieldInspectionSubmissions",
                column: "TaskId",
                principalTable: "FieldInspectionTasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FieldInspectionSubmissions_FieldTaskStartOrigins_StartOriginId",
                table: "FieldInspectionSubmissions",
                column: "StartOriginId",
                principalTable: "FieldTaskStartOrigins",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FieldInspectionTaskEvents_FieldInspectionTasks_TaskId",
                table: "FieldInspectionTaskEvents",
                column: "TaskId",
                principalTable: "FieldInspectionTasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FieldInspectionTasks_RepairItems_RepairItemId",
                table: "FieldInspectionTasks",
                column: "RepairItemId",
                principalTable: "RepairItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FieldInspectionTasks_Surveys_SurveyId",
                table: "FieldInspectionTasks",
                column: "SurveyId",
                principalTable: "Surveys",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Flights_Surveys_SurveyId",
                table: "Flights",
                column: "SurveyId",
                principalTable: "Surveys",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GroundTruthMeasurements_Surveys_SurveyId",
                table: "GroundTruthMeasurements",
                column: "SurveyId",
                principalTable: "Surveys",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LD06ActionEvidence_LD06LifecycleActions_ActionId",
                table: "LD06ActionEvidence",
                column: "ActionId",
                principalTable: "LD06LifecycleActions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LD06LifecycleActions_RepairDecisions_PriorRepairDecisionId",
                table: "LD06LifecycleActions",
                column: "PriorRepairDecisionId",
                principalTable: "RepairDecisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LD06LifecycleActions_RepairObligations_ObligationId",
                table: "LD06LifecycleActions",
                column: "ObligationId",
                principalTable: "RepairObligations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ObligationResponsibilities_RepairObligations_ObligationId",
                table: "ObligationResponsibilities",
                column: "ObligationId",
                principalTable: "RepairObligations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OfflineAdmittedFileReferences_OfflineOperationAdmissions_AdmissionId_ProjectId",
                table: "OfflineAdmittedFileReferences",
                columns: new[] { "AdmissionId", "ProjectId" },
                principalTable: "OfflineOperationAdmissions",
                principalColumns: new[] { "Id", "ProjectId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OfflineAdmittedFileReferences_OfflineOperationBindings_BindingId_ProjectId",
                table: "OfflineAdmittedFileReferences",
                columns: new[] { "BindingId", "ProjectId" },
                principalTable: "OfflineOperationBindings",
                principalColumns: new[] { "Id", "ProjectId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OfflineEvidenceCaptureReferences_OfflineOperationAdmissions_AdmissionId_ProjectId",
                table: "OfflineEvidenceCaptureReferences",
                columns: new[] { "AdmissionId", "ProjectId" },
                principalTable: "OfflineOperationAdmissions",
                principalColumns: new[] { "Id", "ProjectId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OfflineEvidenceCaptureReferences_OfflineOperationBindings_BindingId_ProjectId",
                table: "OfflineEvidenceCaptureReferences",
                columns: new[] { "BindingId", "ProjectId" },
                principalTable: "OfflineOperationBindings",
                principalColumns: new[] { "Id", "ProjectId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OfflineOperationAdmissions_OfflineOperationBindings_BindingId_ProjectId",
                table: "OfflineOperationAdmissions",
                columns: new[] { "BindingId", "ProjectId" },
                principalTable: "OfflineOperationBindings",
                principalColumns: new[] { "Id", "ProjectId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OfflineOperationBindings_RepairItems_RepairResourceId",
                table: "OfflineOperationBindings",
                column: "RepairResourceId",
                principalTable: "RepairItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProcessingAttempts_ProcessingJobs_ProcessingJobId",
                table: "ProcessingAttempts",
                column: "ProcessingJobId",
                principalTable: "ProcessingJobs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProcessingBlocks_SurveyDataVersions_SurveyDataVersionId",
                table: "ProcessingBlocks",
                column: "SurveyDataVersionId",
                principalTable: "SurveyDataVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectLifecycleHistory_RepairObligations_ObligationId",
                table: "ProjectLifecycleHistory",
                column: "ObligationId",
                principalTable: "RepairObligations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QualityChecks_SurveyDataVersions_SurveyDataVersionId",
                table: "QualityChecks",
                column: "SurveyDataVersionId",
                principalTable: "SurveyDataVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QualityChecks_SurveyFiles_SurveyFileId",
                table: "QualityChecks",
                column: "SurveyFileId",
                principalTable: "SurveyFiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RepairActualScopes_RepairObligations_ObligationId",
                table: "RepairActualScopes",
                column: "ObligationId",
                principalTable: "RepairObligations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RepairAssessmentEvidence_RepairMeasurementAssessments_AssessmentId",
                table: "RepairAssessmentEvidence",
                column: "AssessmentId",
                principalTable: "RepairMeasurementAssessments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RepairAssessmentMeasurements_RepairMeasurementAssessments_AssessmentId",
                table: "RepairAssessmentMeasurements",
                column: "AssessmentId",
                principalTable: "RepairMeasurementAssessments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RepairAttemptEvidence_RepairAttempts_AttemptId",
                table: "RepairAttemptEvidence",
                column: "AttemptId",
                principalTable: "RepairAttempts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RepairAttemptReviews_RepairAttemptSubmissionLinks_IntakeLinkId",
                table: "RepairAttemptReviews",
                column: "IntakeLinkId",
                principalTable: "RepairAttemptSubmissionLinks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RepairAttemptReviews_RepairAttempts_AttemptId",
                table: "RepairAttemptReviews",
                column: "AttemptId",
                principalTable: "RepairAttempts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RepairAttemptReviews_RepairFieldTaskBindings_BindingId",
                table: "RepairAttemptReviews",
                column: "BindingId",
                principalTable: "RepairFieldTaskBindings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RepairAttemptReviews_RepairItems_ItemId",
                table: "RepairAttemptReviews",
                column: "ItemId",
                principalTable: "RepairItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RepairAttempts_RepairItems_ItemId",
                table: "RepairAttempts",
                column: "ItemId",
                principalTable: "RepairItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RepairAttempts_RepairObligations_ObligationId",
                table: "RepairAttempts",
                column: "ObligationId",
                principalTable: "RepairObligations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RepairAttemptSubmissionLinks_RepairExecutionFinishes_ExecutionFinishId",
                table: "RepairAttemptSubmissionLinks",
                column: "ExecutionFinishId",
                principalTable: "RepairExecutionFinishes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RepairAttemptSubmissionLinks_RepairFieldTaskBindings_BindingId",
                table: "RepairAttemptSubmissionLinks",
                column: "BindingId",
                principalTable: "RepairFieldTaskBindings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RepairAttemptSubmissionLinks_RepairItems_ItemId",
                table: "RepairAttemptSubmissionLinks",
                column: "ItemId",
                principalTable: "RepairItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RepairCorrectionEvidence_RepairDecisions_DecisionId",
                table: "RepairCorrectionEvidence",
                column: "DecisionId",
                principalTable: "RepairDecisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RepairDangerAcknowledgements_RepairDangerWarnings_WarningId",
                table: "RepairDangerAcknowledgements",
                column: "WarningId",
                principalTable: "RepairDangerWarnings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RepairDangerAcknowledgements_RepairSafetyMonitoring_MonitoringId",
                table: "RepairDangerAcknowledgements",
                column: "MonitoringId",
                principalTable: "RepairSafetyMonitoring",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RepairDangerWarnings_RepairSafetyMonitoring_MonitoringId",
                table: "RepairDangerWarnings",
                column: "MonitoringId",
                principalTable: "RepairSafetyMonitoring",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RepairDecisions_RepairItems_ItemId",
                table: "RepairDecisions",
                column: "ItemId",
                principalTable: "RepairItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RepairDecisions_RepairObligations_ObligationId",
                table: "RepairDecisions",
                column: "ObligationId",
                principalTable: "RepairObligations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RepairEligibilityAssessments_RepairFieldTaskBindings_BindingId",
                table: "RepairEligibilityAssessments",
                column: "BindingId",
                principalTable: "RepairFieldTaskBindings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RepairEligibilityAssessments_RepairItems_ItemId",
                table: "RepairEligibilityAssessments",
                column: "ItemId",
                principalTable: "RepairItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RepairEligibilityAssessments_RepairMeasurementAssessments_AssessmentId",
                table: "RepairEligibilityAssessments",
                column: "AssessmentId",
                principalTable: "RepairMeasurementAssessments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RepairExecutionFinishes_RepairExecutionStarts_ExecutionStartId",
                table: "RepairExecutionFinishes",
                column: "ExecutionStartId",
                principalTable: "RepairExecutionStarts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RepairExecutionFinishes_RepairFieldTaskBindings_BindingId",
                table: "RepairExecutionFinishes",
                column: "BindingId",
                principalTable: "RepairFieldTaskBindings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RepairExecutionFinishes_RepairItems_ItemId",
                table: "RepairExecutionFinishes",
                column: "ItemId",
                principalTable: "RepairItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RepairExecutionStarts_RepairFieldTaskBindings_BindingId",
                table: "RepairExecutionStarts",
                column: "BindingId",
                principalTable: "RepairFieldTaskBindings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RepairExecutionStarts_RepairItems_ItemId",
                table: "RepairExecutionStarts",
                column: "ItemId",
                principalTable: "RepairItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RepairExecutionStarts_RepairMeasurementAssessments_AssessmentId",
                table: "RepairExecutionStarts",
                column: "AssessmentId",
                principalTable: "RepairMeasurementAssessments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RepairFieldTaskBindings_RepairItems_ItemId",
                table: "RepairFieldTaskBindings",
                column: "ItemId",
                principalTable: "RepairItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RepairFieldTaskBindings_RepairObligations_ObligationId",
                table: "RepairFieldTaskBindings",
                column: "ObligationId",
                principalTable: "RepairObligations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RepairItemLifecycleEvents_RepairItems_ItemId",
                table: "RepairItemLifecycleEvents",
                column: "ItemId",
                principalTable: "RepairItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RepairItemLifecycleEvents_RepairObligations_ObligationId",
                table: "RepairItemLifecycleEvents",
                column: "ObligationId",
                principalTable: "RepairObligations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RepairItems_RepairMeasurementAssessments_CurrentAssessmentId",
                table: "RepairItems",
                column: "CurrentAssessmentId",
                principalTable: "RepairMeasurementAssessments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RepairItems_RepairObligations_ObligationId",
                table: "RepairItems",
                column: "ObligationId",
                principalTable: "RepairObligations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RepairPolicyDraftChanges_RepairPolicyDrafts_DraftId",
                table: "RepairPolicyDraftChanges",
                column: "DraftId",
                principalTable: "RepairPolicyDrafts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RepairSafetyActionSources_RepairTemporarySafetyMeasures_MeasureId",
                table: "RepairSafetyActionSources",
                column: "MeasureId",
                principalTable: "RepairTemporarySafetyMeasures",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RepairSafetyCheckEvidence_RepairSafetyChecks_CheckId",
                table: "RepairSafetyCheckEvidence",
                column: "CheckId",
                principalTable: "RepairSafetyChecks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RepairSafetyChecks_RepairSafetyMonitoring_MeasureId",
                table: "RepairSafetyChecks",
                column: "MeasureId",
                principalTable: "RepairSafetyMonitoring",
                principalColumn: "MeasureId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RepairSafetyChecks_RepairTemporarySafetyMeasures_MeasureId",
                table: "RepairSafetyChecks",
                column: "MeasureId",
                principalTable: "RepairTemporarySafetyMeasures",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RepairSafetyMonitoring_RepairTemporarySafetyMeasures_MeasureId",
                table: "RepairSafetyMonitoring",
                column: "MeasureId",
                principalTable: "RepairTemporarySafetyMeasures",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RepairSafetyResponsibilityTransfers_RepairTemporarySafetyMeasures_MeasureId",
                table: "RepairSafetyResponsibilityTransfers",
                column: "MeasureId",
                principalTable: "RepairTemporarySafetyMeasures",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplementarySurveyRequests_SurveyRequests_SurveyRequestId",
                table: "SupplementarySurveyRequests",
                column: "SurveyRequestId",
                principalTable: "SurveyRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplementarySurveyRequests_Surveys_SurveyId",
                table: "SupplementarySurveyRequests",
                column: "SurveyId",
                principalTable: "Surveys",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("ALTER TABLE [Defects] ADD DEFAULT (CONVERT([tinyint],(0))) FOR [Severity];");
            migrationBuilder.Sql("ALTER TABLE [Defects] ADD DEFAULT (CONVERT([tinyint],(0))) FOR [Status];");
            migrationBuilder.Sql("ALTER TABLE [H6NotificationCalendar] ADD DEFAULT ('0001-01-01T00:00:00.0000000+00:00') FOR [PlannedAtUtc];");
            migrationBuilder.Sql("ALTER TABLE [Notifications] ADD DEFAULT (sysutcdatetime()) FOR [OccurredAtUtc];");
            migrationBuilder.Sql("ALTER TABLE [OutboxMessages] ADD DEFAULT ((0)) FOR [DeliveryAttemptCount];");
            migrationBuilder.Sql("ALTER TABLE [OutboxMessages] ADD DEFAULT (CONVERT([tinyint],(1))) FOR [DeliveryStatus];");
            migrationBuilder.Sql("ALTER TABLE [OutboxMessages] ADD DEFAULT (sysutcdatetime()) FOR [NextAttemptAtUtc];");
            migrationBuilder.Sql("ALTER TABLE [ProcessingJobs] ADD DEFAULT ('00000000-0000-0000-0000-000000000000') FOR [ProjectId];");
            migrationBuilder.Sql("ALTER TABLE [SurveyPlans] ADD DEFAULT (N'{}') FOR [OutputRequirements];");
            migrationBuilder.Sql("ALTER TABLE [SurveyRequests] ADD DEFAULT (sysutcdatetime()) FOR [DueAt];");
            migrationBuilder.Sql("ALTER TABLE [SurveyRequests] ADD DEFAULT (N'{}') FOR [OutputRequirements];");
            // Final catalog SQL omitted by EF metadata; no historical upgrade transformations.
            migrationBuilder.Sql("ALTER TABLE [CasePublicationEvidence] ADD CONSTRAINT [FK_CasePublicationEvidence_OriginalEvidence] FOREIGN KEY ([OriginalEvidenceId], [SourceReportId]) REFERENCES [ReportOriginalEvidence] ([Id], [ReportId]);");
            migrationBuilder.Sql("ALTER TABLE [CasePublicationEvidence] ADD CONSTRAINT [FK_CasePublicationEvidence_SupplementEvidence] FOREIGN KEY ([SupplementEvidenceId], [SourceReportId]) REFERENCES [ReportSupplementEvidence] ([Id], [ReportId]);");
            migrationBuilder.Sql("ALTER TABLE [CaseConclusionEvidence] ADD CONSTRAINT [FK_CaseConclusionEvidence_OriginalEvidence] FOREIGN KEY ([OriginalEvidenceId], [SourceReportId]) REFERENCES [ReportOriginalEvidence] ([Id], [ReportId]);");
            migrationBuilder.Sql("ALTER TABLE [CaseConclusionEvidence] ADD CONSTRAINT [FK_CaseConclusionEvidence_SupplementEvidence] FOREIGN KEY ([SupplementEvidenceId], [SourceReportId]) REFERENCES [ReportSupplementEvidence] ([Id], [ReportId]);");
            migrationBuilder.Sql("ALTER TABLE [SourceDecisions] ADD CONSTRAINT [FK_SourceDecisions_CorrectionIdentity] FOREIGN KEY ([SupersedesDecisionId], [SourceKind], [SourceId], [ProjectId]) REFERENCES [SourceDecisions] ([Id], [SourceKind], [SourceId], [ProjectId]);");
            migrationBuilder.Sql("CREATE UNIQUE INDEX [UX_RepairDecisions_Initial] ON [RepairDecisions] ([ItemId]) WHERE [SupersedesDecisionId] IS NULL;");
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_AccountStatusChangeLogs_AppendOnly]
                ON [AccountStatusChangeLogs]
                INSTEAD OF UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    THROW 51000, 'AccountStatusChangeLogs are append-only; updates and deletions are forbidden.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_AIDetections_Immutable]
                ON [dbo].[AIDetections]
                AFTER UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    THROW 51033, 'AIDetections are immutable.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_Anh02AiDetectionProvenance_Immutable] ON [dbo].[Anh02AiDetectionProvenance] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51220, 'AI detection provenance is immutable.', 1; END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_Anh02AiManifestFiles_Immutable] ON [dbo].[Anh02AiManifestFiles] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51220, 'AI manifest file provenance is immutable.', 1; END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_Anh02AiMockRuns_Identity] ON [dbo].[Anh02AiMockRuns] AFTER UPDATE, DELETE AS
                BEGIN
                 SET NOCOUNT ON;
                 IF EXISTS (SELECT Id FROM deleted EXCEPT SELECT Id FROM inserted)
                    THROW 51221, 'AI run identity cannot be deleted.', 1;
                 IF EXISTS (SELECT Id, [ProjectId], [DatasetVersionId], [ProcessingJobId], [AttemptId], [ModelVersionId], [RouteVersionId], [SegmentSetId], [CreatedBy], [CreatedAt], CONVERT(varbinary(max), [Stage]), CONVERT(varbinary(max), [FixtureVersion]), CONVERT(varbinary(max), [GeometryVersion]), CONVERT(varbinary(max), [ManifestHash]), CONVERT(varbinary(max), [CanonicalManifest]), [AnalysisRunId] FROM deleted EXCEPT SELECT Id, [ProjectId], [DatasetVersionId], [ProcessingJobId], [AttemptId], [ModelVersionId], [RouteVersionId], [SegmentSetId], [CreatedBy], [CreatedAt], CONVERT(varbinary(max), [Stage]), CONVERT(varbinary(max), [FixtureVersion]), CONVERT(varbinary(max), [GeometryVersion]), CONVERT(varbinary(max), [ManifestHash]), CONVERT(varbinary(max), [CanonicalManifest]), [AnalysisRunId] FROM inserted)
                    THROW 51221, 'AI source identity is immutable.', 1;
                 IF EXISTS (SELECT 1 FROM deleted WHERE Status IN ('SUCCEEDED','FAILED'))
                    THROW 51221, 'AI terminal result is immutable.', 1;
                 IF EXISTS (SELECT 1 FROM deleted d JOIN inserted i ON d.Id=i.Id
                    WHERE (d.Status='QUEUED' AND i.Status NOT IN ('QUEUED','RUNNING','FAILED'))
                       OR (d.Status='RUNNING' AND i.Status NOT IN ('RUNNING','SUCCEEDED','FAILED'))
                       OR (d.ResultTimestamp IS NOT NULL AND (i.ResultTimestamp IS NULL OR i.ResultTimestamp<>d.ResultTimestamp)))
                    THROW 51221, 'AI stage transition or result timestamp is invalid.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_Anh02AiResultProvenance_Immutable] ON [dbo].[Anh02AiResultProvenance] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51220, 'AI result provenance is immutable.', 1; END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_Anh02ExportSnapshotFiles_Immutable] ON [dbo].[Anh02ExportSnapshotFiles] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51220, 'Export snapshot file is immutable.', 1; END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_Anh02ExportSnapshots_Immutable] ON [dbo].[Anh02ExportSnapshots] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51220, 'Export snapshot is immutable.', 1; END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_Anh02GeneratedArtifacts_Immutable] ON [dbo].[Anh02GeneratedArtifacts] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51220, 'Generated artifact is immutable.', 1; END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_AuditLogs_AppendOnly]
                ON [AuditLogs]
                INSTEAD OF UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    THROW 51000, 'AuditLogs are append-only; corrections require a new audit event.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_BaselineSelectionItems_Immutable] ON [BaselineSelectionItems] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51024, 'ANH-01 evidence and geometry metadata are immutable.', 1; END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_BaselineSelections_Immutable] ON [BaselineSelections] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51024, 'ANH-01 evidence and geometry metadata are immutable.', 1; END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_BusinessDutyAppointments_Immutable] ON [BusinessDutyAppointments] AFTER UPDATE,DELETE AS
                BEGIN SET NOCOUNT ON; THROW 51101, 'Business duty appointment history is immutable.', 1; END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_BusinessReceivingRequests_SourceAck] ON [BusinessReceivingRequests] AFTER UPDATE,DELETE AS
                BEGIN
                  SET NOCOUNT ON;
                  IF EXISTS (SELECT 1 FROM deleted d LEFT JOIN inserted i ON i.Id=d.Id WHERE i.Id IS NULL)
                    THROW 51102, 'Business receiving source history cannot be deleted.', 1;
                  IF EXISTS (SELECT 1 FROM deleted d JOIN inserted i ON i.Id=d.Id WHERE
                    i.ProjectId<>d.ProjectId OR i.Kind<>d.Kind OR i.SourceKind<>d.SourceKind OR i.SourceId<>d.SourceId OR
                    i.SourceVersion<>d.SourceVersion OR i.ScopeId<>d.ScopeId OR i.ResponsibleRole<>d.ResponsibleRole OR i.RequestedAt<>d.RequestedAt OR
                    (d.AcknowledgedAt IS NOT NULL AND (i.AcknowledgedAt IS NULL OR i.AcknowledgedAt<>d.AcknowledgedAt OR
                     i.AcknowledgmentId IS NULL OR i.AcknowledgmentId<>d.AcknowledgmentId OR i.AcknowledgedBy IS NULL OR i.AcknowledgedBy<>d.AcknowledgedBy OR
                     i.ClockId IS NULL OR i.ClockId<>d.ClockId OR ISNULL(CONVERT(nvarchar(64),i.ClaimedDeviceAt,127),'')<>ISNULL(CONVERT(nvarchar(64),d.ClaimedDeviceAt,127),''))) OR
                    (d.CompletedAt IS NOT NULL AND (i.CompletedAt IS NULL OR i.CompletedAt<>d.CompletedAt)))
                    THROW 51103, 'Business receiving source and first ACK are immutable.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_CaseConclusionDefects_Immutable] ON [CaseConclusionDefects] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51109, 'Immutable integration history cannot be rewritten.', 1; END;
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_CaseConclusionEvidence_Immutable] ON [CaseConclusionEvidence] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51111, 'Immutable integration history cannot be rewritten.', 1; END;
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_CaseConclusions_Immutable] ON [CaseConclusions] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51103, 'Immutable integration history cannot be rewritten.', 1; END;
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_CasePublicationDefects_Immutable] ON [CasePublicationDefects] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51110, 'Immutable integration history cannot be rewritten.', 1; END;
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_CasePublicationEvidence_Immutable] ON [CasePublicationEvidence] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51106, 'Immutable integration history cannot be rewritten.', 1; END;
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_CasePublicationRecipients_Immutable] ON [CasePublicationRecipients] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51105, 'Immutable integration history cannot be rewritten.', 1; END;
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_CasePublications_Immutable] ON [CasePublications] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51104, 'Immutable integration history cannot be rewritten.', 1; END;
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_CaseReportLinkHistory_Immutable] ON [CaseReportLinkHistory] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51107, 'Immutable integration history cannot be rewritten.', 1; END;
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_CaseReportLinkHistoryReports_Immutable] ON [CaseReportLinkHistoryReports] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51112, 'Immutable integration history cannot be rewritten.', 1; END;
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_CaseReportLinks_AppendOnly] ON [CaseReportLinks] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; IF EXISTS (SELECT Id,CaseId,ReportId,StartedAt FROM deleted EXCEPT SELECT Id,CaseId,ReportId,StartedAt FROM inserted) OR EXISTS (SELECT 1 FROM deleted d JOIN inserted i ON d.Id=i.Id WHERE d.EndedAt IS NOT NULL AND (i.EndedAt IS NULL OR i.EndedAt<>d.EndedAt)) THROW 51122, 'Link identity/history cannot be rewritten or reopened.', 1; END;
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_CrsProfileRevisions_Immutable] ON [dbo].[CrsProfileRevisions] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51230, 'H2 revision history is immutable.', 1; END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_DatasetAssessmentItems_Immutable] ON [DatasetAssessmentItems] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51024, 'ANH-01 evidence and geometry metadata are immutable.', 1; END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_DatasetAssessments_Immutable] ON [DatasetAssessments] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51024, 'ANH-01 evidence and geometry metadata are immutable.', 1; END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_DeadlineBreaches_Immutable] ON [DeadlineBreaches] AFTER UPDATE, DELETE AS
                BEGIN SET NOCOUNT ON; THROW 51002, 'Deadline breach history is immutable.', 1; END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_DeadlineClocks_ImmutableOrigin] ON [DeadlineClocks] AFTER UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT [Id] FROM deleted EXCEPT SELECT [Id] FROM inserted)
                      OR EXISTS (SELECT [Id],[ProjectId],[Kind],[TargetId],[OriginEventId],[OriginAt],[OriginalDueAt] FROM deleted
                        EXCEPT SELECT [Id],[ProjectId],[Kind],[TargetId],[OriginEventId],[OriginAt],[OriginalDueAt] FROM inserted)
                      OR EXISTS (SELECT 1 FROM deleted d JOIN inserted i ON d.Id=i.Id WHERE
                        i.CurrentDueAt<d.CurrentDueAt OR
                        (d.CompletedAt IS NOT NULL AND (i.CompletedAt IS NULL OR i.CompletedAt<>d.CompletedAt)) OR
                        (d.AcknowledgedAt IS NOT NULL AND (i.AcknowledgedAt IS NULL OR i.AcknowledgedAt<>d.AcknowledgedAt OR
                          i.AcknowledgedByUserId IS NULL OR i.AcknowledgedByUserId<>d.AcknowledgedByUserId OR
                          i.AcknowledgmentEventId IS NULL OR i.AcknowledgmentEventId<>d.AcknowledgmentEventId)))
                        THROW 51003, 'Deadline origin and acknowledged/completed history is immutable.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_DeadlineDutyAppointments_Immutable] ON [DeadlineDutyAppointments] AFTER UPDATE,DELETE AS
                BEGIN SET NOCOUNT ON; THROW 51105, 'Deadline duty appointment history is immutable.', 1; END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_DeadlineExtensions_Immutable] ON [DeadlineExtensions] AFTER UPDATE, DELETE AS
                BEGIN SET NOCOUNT ON; THROW 51001, 'Deadline extension history is immutable.', 1; END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER TR_DefectStatisticsSources_Immutable ON DefectStatisticsSources AFTER UPDATE,DELETE AS
                BEGIN SET NOCOUNT ON;
                  IF EXISTS(SELECT 1 FROM deleted) THROW 51800, 'Statistics source confirmations are append-only.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER TR_DefectStatisticsSources_Scope ON DefectStatisticsSources AFTER INSERT AS
                BEGIN SET NOCOUNT ON;
                  IF EXISTS(SELECT 1 FROM inserted i WHERE
                    NOT EXISTS(SELECT 1 FROM RepairObligations o JOIN RepairActualScopes s ON s.ObligationId=o.Id
                      JOIN Defects d ON d.Id=o.DefectId LEFT JOIN ObligationResponsibilities r ON r.ObligationId=o.Id
                      WHERE o.Id=i.ObligationId AND d.Id=i.DefectId AND d.ProjectId=i.ProjectId AND o.ProjectId=i.ProjectId
                        AND COALESCE(r.CurrentProjectId,o.ProjectId)=i.ProjectId AND d.RoadSectionVersionId=i.RouteVersionId
                        AND s.PhysicalRoadId=i.RoadSectionId AND s.LocationVersion=i.LocationVersion
                        AND s.[From]=i.[From] AND s.[To]=i.[To] AND s.OffsetFrom=i.OffsetFrom AND s.OffsetTo=i.OffsetTo)
                    OR NOT EXISTS(SELECT 1 FROM OPENJSON(i.SegmentIdsJson))
                    OR EXISTS(SELECT 1 FROM OPENJSON(i.SegmentIdsJson) j WHERE NOT EXISTS(SELECT 1 FROM RoadSegments s
                      JOIN RoadSegmentSets ss ON ss.Id=s.SegmentSetId WHERE s.Id=TRY_CONVERT(uniqueidentifier,j.[value])
                        AND s.RoadSectionVersionId=i.RouteVersionId AND ss.RoadSectionVersionId=i.RouteVersionId AND ss.Status='PUBLISHED'))
                    OR EXISTS(SELECT 1 FROM DefectStatisticsSources p WHERE p.Id<>i.Id AND p.SharedPartId=i.SharedPartId
                      AND (p.ProjectId<>i.ProjectId OR p.RoadSectionId<>i.RoadSectionId OR p.LocationVersion<>i.LocationVersion
                        OR p.[From]<>i.[From] OR p.[To]<>i.[To] OR p.OffsetFrom<>i.OffsetFrom OR p.OffsetTo<>i.OffsetTo OR p.Provenance<>i.Provenance))
                    OR (i.SupersedesId IS NOT NULL AND NOT EXISTS(SELECT 1 FROM DefectStatisticsSources p WHERE p.Id=i.SupersedesId
                      AND p.ProjectId=i.ProjectId AND p.ObligationId=i.ObligationId AND p.SharedPartId=i.SharedPartId AND p.Provenance=i.Provenance))
                    OR EXISTS(SELECT 1 FROM OPENJSON(i.QuantitiesJson) WITH(measurementId uniqueidentifier,segmentId uniqueidentifier) q
                      WHERE NOT EXISTS(SELECT 1 FROM GroundTruthMeasurements m WHERE m.Id=q.measurementId AND m.RoadSectionVersionId=i.RouteVersionId
                        AND (m.DefectId=i.DefectId OR EXISTS(SELECT 1 FROM DefectStatisticsSources p WHERE p.ProjectId=i.ProjectId
                          AND p.SharedPartId=i.SharedPartId AND p.DefectId=m.DefectId)))
                        OR (q.segmentId IS NOT NULL AND NOT EXISTS(SELECT 1 FROM OPENJSON(i.SegmentIdsJson) j WHERE TRY_CONVERT(uniqueidentifier,j.[value])=q.segmentId)))
                    OR (i.Provenance='REAL_SOURCE' AND EXISTS(SELECT 1 FROM OPENJSON(i.QuantitiesJson) WITH(measurementId uniqueidentifier) q
                      JOIN GroundTruthMeasurements m ON m.Id=q.measurementId WHERE m.Notes LIKE 'TEST_ONLY%')))
                    THROW 51801, 'Statistics require exact current scope, segments, measurement sources and immutable identity.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_DefectVerificationLogs_AppendOnly]
                ON [dbo].[DefectVerificationLogs]
                AFTER UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    THROW 51034, 'DefectVerificationLogs are append-only.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_FieldInspectionEvidenceLinks_Immutable] ON [dbo].[FieldInspectionEvidenceLinks]
                AFTER UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM deleted)
                        THROW 51130, 'FIELD original history is immutable.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_FieldInspectionEvidenceLinks_Scope] ON [dbo].[FieldInspectionEvidenceLinks] AFTER INSERT AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM inserted i LEFT JOIN [FieldInspectionTasks] t ON t.[Id]=i.[TaskId]
                        WHERE t.[Id] IS NULL OR t.[ProjectId]<>i.[ProjectId] OR t.[LifecycleVersion]<>2 OR NOT EXISTS (SELECT 1 FROM [FieldInspectionSubmissions] s
                    WHERE s.[Id]=i.[SubmissionId] AND s.[TaskId]=i.[TaskId] AND s.[ProjectId]=i.[ProjectId]
                      AND s.[AssignmentId]=i.[AssignmentId])
                OR i.[Purpose] NOT IN ('BEFORE','AFTER','MEASUREMENT')
                OR ISJSON(i.[CaptureFactsJson])<>1 OR LEFT(LTRIM(i.[CaptureFactsJson]),1)<>'{'
                OR (i.[FileId] IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [Files] f
                    WHERE f.[Id]=i.[FileId] AND f.[Checksum]=i.[DeclaredChecksum]
                      AND (EXISTS (SELECT 1 FROM [FileScopes] fs WHERE fs.[FileId]=f.[Id]
                          AND fs.[ProjectId]=i.[ProjectId] AND fs.[TargetId]=i.[TaskId] AND fs.[Purpose]=i.[Purpose])
                        OR (i.[Purpose]='BEFORE' AND EXISTS (SELECT 1 FROM [FieldInspectionEvidenceReuseDecisions] r
                            WHERE r.[TaskId]=i.[TaskId] AND r.[ProjectId]=i.[ProjectId]
                              AND r.[FileId]=i.[FileId] AND r.[FileChecksum]=i.[DeclaredChecksum]))))))
                        THROW 51131, 'FIELD history scope, lineage or provenance is invalid.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_FieldInspectionEvidenceReuseDecisions_Immutable] ON [dbo].[FieldInspectionEvidenceReuseDecisions]
                AFTER UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM deleted)
                        THROW 51130, 'FIELD original history is immutable.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_FieldInspectionEvidenceReuseDecisions_Scope] ON [dbo].[FieldInspectionEvidenceReuseDecisions] AFTER INSERT AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM inserted i LEFT JOIN [FieldInspectionTasks] t ON t.[Id]=i.[TaskId]
                        WHERE t.[Id] IS NULL OR t.[ProjectId]<>i.[ProjectId] OR t.[LifecycleVersion]<>2 OR i.[SourceKind] NOT IN ('REPORTER','DRONE') OR LEN(LTRIM(RTRIM(i.[Reason])))=0
                OR ISJSON(i.[ProvenanceJson])<>1
                OR NOT EXISTS (SELECT 1 FROM [Files] f WHERE f.[Id]=i.[FileId] AND f.[Checksum]=i.[FileChecksum])
                OR (i.[SourceKind]='REPORTER' AND NOT EXISTS
                    (SELECT 1 FROM [DefectSourceLinks] link JOIN [FileScopes] fs ON fs.[FileId]=i.[FileId]
                     WHERE link.[DefectId]=t.[DefectId] AND link.[ProjectId]=i.[ProjectId]
                       AND link.[SourceKind]=1 AND link.[EndedAt] IS NULL
                       AND fs.[ProjectId] IS NULL AND fs.[TargetId] IS NULL AND fs.[Purpose]='REPORT_PHOTO'
                       AND (EXISTS (SELECT 1 FROM [ReportOriginalEvidence] e WHERE e.[Id]=i.[SourceEvidenceId]
                            AND e.[ReportId]=link.[ReportSourceId] AND e.[FileId]=i.[FileId] AND e.[OwnerUserId]=fs.[OwnerUserId])
                        OR EXISTS (SELECT 1 FROM [ReportSupplementEvidence] e WHERE e.[Id]=i.[SourceEvidenceId]
                            AND e.[ReportId]=link.[ReportSourceId] AND e.[FileId]=i.[FileId] AND e.[OwnerUserId]=fs.[OwnerUserId]))))
                OR (i.[SourceKind]='DRONE' AND NOT EXISTS
                    (SELECT 1 FROM [SurveyFiles] sf JOIN [FileScopes] fs ON fs.[FileId]=sf.[FileId]
                     WHERE sf.[Id]=i.[SourceEvidenceId] AND sf.[FileId]=i.[FileId] AND sf.[SurveyId]=t.[SurveyId]
                       AND fs.[ProjectId]=i.[ProjectId] AND fs.[Purpose] IN ('SURVEY_VIDEO','TELEMETRY'))))
                        THROW 51131, 'FIELD history scope, lineage or provenance is invalid.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_FieldInspectionLocationProofs_Immutable] ON [dbo].[FieldInspectionLocationProofs]
                AFTER UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM deleted)
                        THROW 51130, 'FIELD original history is immutable.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_FieldInspectionLocationProofs_Scope] ON [dbo].[FieldInspectionLocationProofs] AFTER INSERT AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM inserted i LEFT JOIN [FieldInspectionTasks] t ON t.[Id]=i.[TaskId]
                        WHERE t.[Id] IS NULL OR t.[ProjectId]<>i.[ProjectId] OR t.[LifecycleVersion]<>2 OR NOT EXISTS (SELECT 1 FROM [FieldInspectionSubmissions] s
                    WHERE s.[Id]=i.[SubmissionId] AND s.[TaskId]=i.[TaskId] AND s.[ProjectId]=i.[ProjectId])
                OR i.[Kind] NOT IN ('GPS_CAPTURE','POSITION_CHECKLIST','UNKNOWN')
                OR ISJSON(i.[FactsJson])<>1 OR i.[VerificationState]<>'CLAIMED')
                        THROW 51131, 'FIELD history scope, lineage or provenance is invalid.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_FieldInspectionOperationOrigins_Immutable] ON [dbo].[FieldInspectionOperationOrigins]
                AFTER UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM deleted)
                        THROW 51130, 'FIELD original history is immutable.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_FieldInspectionOperationOrigins_Scope] ON [dbo].[FieldInspectionOperationOrigins] AFTER INSERT AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS(SELECT 1 FROM inserted i LEFT JOIN FieldInspectionTasks t ON t.Id=i.TaskId
                        WHERE t.Id IS NULL OR t.ProjectId<>i.ProjectId OR t.LifecycleVersion<>2
                            OR i.Id<>i.EffectId OR i.SchemaVersion<>1 OR i.Kind NOT IN ('FIELD_START','FIELD_SUBMISSION','FIELD_ACCEPT','REPAIR_ASSESSMENT','REPAIR_EXECUTION_START','REPAIR_EXECUTION_FINISH')
                            OR LEN(i.ContentHash)<>64 OR i.ContentHash COLLATE Latin1_General_100_BIN2 LIKE '%[^0-9a-f]%')
                        THROW 51131, 'FIELD history scope, lineage or provenance is invalid.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_FieldInspectionReviews_Immutable] ON [dbo].[FieldInspectionReviews]
                AFTER UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM deleted)
                        THROW 51130, 'FIELD original history is immutable.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_FieldInspectionReviews_Scope] ON [dbo].[FieldInspectionReviews] AFTER INSERT AS
                BEGIN
                  SET NOCOUNT ON;
                  IF EXISTS (SELECT 1 FROM inserted i LEFT JOIN FieldInspectionTasks t ON t.Id=i.TaskId
                    WHERE t.Id IS NULL OR t.ProjectId<>i.ProjectId OR t.LifecycleVersion<>2
                    OR NOT EXISTS (SELECT 1 FROM FieldInspectionSubmissions s WHERE s.Id=i.SubmissionId AND s.TaskId=i.TaskId AND s.ProjectId=i.ProjectId)
                    OR i.Decision NOT IN ('CONFIRM','NO_DEFECT','SUPPLEMENT') OR LEN(LTRIM(RTRIM(i.Reason)))=0
                    OR (i.Decision='SUPPLEMENT' AND i.ReceiptActivation NOT IN ('AWAITING_OWNER_RECEIPT_PROTOCOL','BUSINESS_ACK_REQUIRED')))
                    THROW 51131, 'FIELD history scope, lineage or provenance is invalid.', 1;
                END
                """);
            migrationBuilder.Sql("""
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
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_FieldInspectionSessions_Integrity] ON [dbo].[FieldInspectionSessions] AFTER INSERT, UPDATE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM inserted s LEFT JOIN [FieldInspectionTasks] t ON t.[Id]=s.[FieldInspectionTaskId]
                        WHERE (s.[Purpose] IN (1,3,4,5) AND
                            (t.[Id] IS NULL OR t.[ProjectId]<>s.[ProjectId] OR t.[RoadSectionVersionId]<>s.[RoadSectionVersionId]
                             OR EXISTS (SELECT t.[SurveyId] EXCEPT SELECT s.[SurveyId])
                             OR (s.[Purpose]=1 AND (t.[LifecycleVersion]<>1 OR t.[Purpose]<>1))
                             OR (s.[Purpose] IN (3,4,5) AND (t.[LifecycleVersion]<>2 OR (t.[Purpose]<>s.[Purpose] AND NOT (s.[Purpose]=3 AND t.[Purpose]=4
                    AND t.[TaskMode] IN('NORMAL','CONDITIONAL_FT') AND t.[RepairItemId] IS NOT NULL
                    AND (EXISTS (SELECT 1 FROM deleted priorSession WHERE priorSession.Id=s.Id) OR t.[Status] IN(2,4))
                    AND EXISTS (SELECT 1 FROM RepairItems item JOIN RepairFieldTaskBindings binding ON binding.Id=item.CurrentBindingId
                        JOIN FieldInspectionAssignments assignment ON assignment.Id=binding.AssignmentId
                        WHERE item.Id=t.[RepairItemId] AND binding.ItemId=item.Id AND binding.ProjectId=t.[ProjectId]
                            AND binding.DefectId=t.[DefectId] AND binding.TaskId=t.Id AND binding.CrewId=s.[InspectorUserId]
                            AND assignment.FieldInspectionTaskId=t.Id AND assignment.AssignedToUserId=binding.CrewId
                            AND (EXISTS (SELECT 1 FROM deleted priorSession WHERE priorSession.Id=s.Id)
                                OR (assignment.Status=1 AND assignment.EndedAt IS NULL)))))))
                             OR (NOT EXISTS (SELECT 1 FROM deleted old WHERE old.[Id]=s.[Id]) AND NOT EXISTS
                                (SELECT 1 FROM [FieldInspectionAssignments] a WHERE a.[FieldInspectionTaskId]=t.[Id]
                                    AND a.[AssignedToUserId]=s.[InspectorUserId] AND a.[Status]=1 AND a.[EndedAt] IS NULL))))
                           OR (s.[Purpose]=2 AND s.[FieldInspectionTaskId] IS NOT NULL))
                        THROW 51040, 'Field inspection session scope or active assignment is invalid.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_FieldInspectionSubmissions_Immutable] ON [dbo].[FieldInspectionSubmissions]
                AFTER UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM deleted)
                        THROW 51130, 'FIELD original history is immutable.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_FieldInspectionSubmissions_Scope] ON [dbo].[FieldInspectionSubmissions] AFTER INSERT AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM inserted i LEFT JOIN [FieldInspectionTasks] t ON t.[Id]=i.[TaskId]
                        WHERE t.[Id] IS NULL OR t.[ProjectId]<>i.[ProjectId] OR t.[LifecycleVersion]<>2 OR NOT EXISTS (SELECT 1 FROM [FieldInspectionAssignments] a
                    WHERE a.[Id]=i.[AssignmentId] AND a.[FieldInspectionTaskId]=i.[TaskId]
                      AND a.[AssignedToUserId]=i.[OriginalActorId])
                OR NOT EXISTS (SELECT 1 FROM [FieldTaskStartOrigins] s
                    WHERE s.[Id]=i.[StartOriginId] AND s.[TaskId]=i.[TaskId] AND s.[ProjectId]=i.[ProjectId])
                OR NOT EXISTS (SELECT 1 FROM [FieldInspectionSessions] s
                    WHERE s.[Id]=i.[SessionId] AND s.[FieldInspectionTaskId]=i.[TaskId]
                      AND s.[ProjectId]=i.[ProjectId] AND s.[RoadSectionVersionId]=t.[RoadSectionVersionId]
                      AND s.[InspectorUserId]=i.[OriginalActorId] AND s.[Purpose]=t.[Purpose])
                OR NOT EXISTS (SELECT 1 FROM [FieldInspectionOperationOrigins] o
                    WHERE o.[Id]=i.[OperationOriginId] AND o.[EffectId]=i.[Id]
                      AND o.[ProjectId]=i.[ProjectId] AND o.[TaskId]=i.[TaskId]
                      AND o.[OriginId]=i.[OriginId] AND o.[Kind]='FIELD_SUBMISSION'
                      AND o.[ContentHash]=i.[ContentHash] AND o.[OriginalActorId]=i.[OriginalActorId]
                      AND o.[ServerReceivedAt]=i.[ServerReceivedAt])
                OR NOT ((i.[Revision]=1 AND i.[ParentId] IS NULL AND i.[RootId]=i.[Id])
                    OR (i.[Revision]>1 AND EXISTS (SELECT 1 FROM [FieldInspectionSubmissions] p
                        WHERE p.[Id]=i.[ParentId] AND p.[TaskId]=i.[TaskId] AND p.[ProjectId]=i.[ProjectId]
                          AND p.[RootId]=i.[RootId] AND p.[Revision]+1=i.[Revision])))
                OR ISJSON(i.[PayloadJson])<>1 OR LEFT(LTRIM(i.[PayloadJson]),1)<>'{'
                OR ISJSON(i.[MissingReasonsJson])<>1 OR LEFT(LTRIM(i.[MissingReasonsJson]),1)<>'['
                OR i.[Readiness] NOT IN ('READY','INCOMPLETE'))
                        THROW 51131, 'FIELD history scope, lineage or provenance is invalid.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_FieldInspectionTaskEvents_Immutable] ON [dbo].[FieldInspectionTaskEvents]
                AFTER UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM deleted)
                        THROW 51130, 'FIELD original history is immutable.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_FieldInspectionTaskEvents_Scope] ON [dbo].[FieldInspectionTaskEvents] AFTER INSERT AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM inserted i LEFT JOIN [FieldInspectionTasks] t ON t.[Id]=i.[TaskId]
                        WHERE t.[Id] IS NULL OR t.[ProjectId]<>i.[ProjectId] OR t.[LifecycleVersion]<>2 OR (i.[AssignmentId] IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [FieldInspectionAssignments] a
                    WHERE a.[Id]=i.[AssignmentId] AND a.[FieldInspectionTaskId]=i.[TaskId]))
                OR ISJSON(i.[FactsJson])<>1 OR LEFT(LTRIM(i.[FactsJson]),1)<>'{'
                OR (i.[LocationImpactId] IS NULL AND i.[LocationImpactDecisionId] IS NOT NULL)
                OR (i.[LocationImpactId] IS NOT NULL AND (i.[LocationImpactDecisionId] IS NULL OR NOT EXISTS
                    (SELECT 1 FROM [GeometryLocationImpacts] impact JOIN [GeometryLocationImpactDecisions] decision
                        ON decision.[ImpactId]=impact.[Id]
                     WHERE impact.[Id]=i.[LocationImpactId] AND impact.[ProjectId]=i.[ProjectId]
                       AND impact.[PreviousRouteVersionId]=t.[RoadSectionVersionId]
                       AND decision.[Id]=i.[LocationImpactDecisionId] AND decision.[TaskId]=i.[TaskId]
                       AND decision.[ActorId]=i.[ActorId]))))
                        THROW 51131, 'FIELD history scope, lineage or provenance is invalid.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_FieldInspectionTasks_H3Scope] ON [dbo].[FieldInspectionTasks] AFTER INSERT, UPDATE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.[Id]=i.[Id]
                        WHERE EXISTS (SELECT i.[ProjectId],i.[DefectId],i.[SurveyId],i.[LifecycleVersion],i.[SourceKind],i.[TaskMode],i.[Purpose],
                            i.[RoadSectionVersionId],i.[SegmentSetId],i.[LayoutRevisionId],i.[MapPublicationId],i.[CrsProfileRevisionId],i.[SlabId],
                            i.[RequiredMeasurementType],i.[MeasurementScope]
                            EXCEPT SELECT d.[ProjectId],d.[DefectId],d.[SurveyId],d.[LifecycleVersion],d.[SourceKind],d.[TaskMode],d.[Purpose],
                            d.[RoadSectionVersionId],d.[SegmentSetId],d.[LayoutRevisionId],d.[MapPublicationId],d.[CrsProfileRevisionId],d.[SlabId],
                            d.[RequiredMeasurementType],d.[MeasurementScope]))
                        THROW 51132, 'FIELD task source, mode and location pins are write-once.', 1;
                    IF EXISTS (SELECT 1 FROM inserted i
                        LEFT JOIN [Defects] d ON d.[Id]=i.[DefectId]
                        LEFT JOIN [RoadSectionVersions] v ON v.[Id]=i.[RoadSectionVersionId]
                        LEFT JOIN [RoadSections] r ON r.[Id]=v.[RoadSectionId]
                        WHERE i.[LifecycleVersion]=2 AND
                            (i.[TaskMode] NOT IN ('MEASURE_ONLY','NORMAL','CONDITIONAL_FT') OR i.[RequiredMeasurementType] NOT IN (1,2,3,4)
                            OR d.[Id] IS NULL OR d.[ProjectId]<>i.[ProjectId] OR d.[RoadSectionVersionId]<>i.[RoadSectionVersionId]
                            OR r.[ProjectId]<>i.[ProjectId]
                            OR (NOT EXISTS (SELECT 1 FROM deleted old WHERE old.[Id]=i.[Id]) AND d.[Status]<>1)
                            OR (i.[SegmentSetId] IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [RoadSegmentSets] s
                                WHERE s.[Id]=i.[SegmentSetId] AND s.[RoadSectionVersionId]=i.[RoadSectionVersionId]))
                            OR (i.[LayoutRevisionId] IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [PavementLayoutRevisions] l
                                WHERE l.[Id]=i.[LayoutRevisionId] AND l.[ProjectId]=i.[ProjectId]
                                  AND l.[RouteVersionId]=i.[RoadSectionVersionId] AND l.[SegmentSetId]=i.[SegmentSetId]))
                            OR (i.[MapPublicationId] IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [GeometryMapPublications] m
                                WHERE m.[Id]=i.[MapPublicationId] AND m.[ProjectId]=i.[ProjectId]
                                  AND m.[RouteVersionId]=i.[RoadSectionVersionId] AND m.[SegmentSetId]=i.[SegmentSetId]
                                  AND m.[LayoutRevisionId]=i.[LayoutRevisionId]
                                  AND NOT EXISTS (SELECT m.[CrsProfileRevisionId] EXCEPT SELECT i.[CrsProfileRevisionId])))
                            OR EXISTS (SELECT v.[CrsProfileRevisionId] EXCEPT SELECT i.[CrsProfileRevisionId])
                            OR (i.[SlabId] IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [PavementLayoutRevisions] layout
                                CROSS APPLY OPENJSON(layout.[SnapshotJson],'$.slabs') WITH ([Key] nvarchar(160) '$.key') slab
                                WHERE layout.[Id]=i.[LayoutRevisionId] AND slab.[Key]=i.[SlabId]))
                            OR (NOT EXISTS (SELECT 1 FROM deleted old WHERE old.[Id]=i.[Id]) AND i.[SourceKind]='REPORTER'
                                AND NOT EXISTS (SELECT 1 FROM [DefectSourceLinks] link JOIN [SourceDecisions] decision ON decision.[Id]=link.[DecisionId]
                                    WHERE link.[DefectId]=i.[DefectId] AND link.[ProjectId]=i.[ProjectId]
                                      AND link.[SourceKind]=1 AND link.[EndedAt] IS NULL AND decision.[Decision]=1))
                            OR (NOT EXISTS (SELECT 1 FROM deleted old WHERE old.[Id]=i.[Id]) AND i.[SourceKind]='SURVEY'
                                AND (NOT EXISTS (SELECT 1 FROM [AIDetections] a JOIN [ProcessingJobs] j ON j.[Id]=a.[ProcessingJobId]
                                        JOIN [ProcessingBlocks] b ON b.[Id]=j.[ProcessingBlockId]
                                        JOIN [SurveyDataVersions] data ON data.[Id]=b.[SurveyDataVersionId]
                                        WHERE a.[Id]=d.[SourceAIDetectionId] AND data.[SurveyId]=i.[SurveyId])
                                    OR NOT EXISTS (SELECT 1 FROM [DefectSourceLinks] link JOIN [SourceDecisions] decision ON decision.[Id]=link.[DecisionId]
                                        WHERE link.[DefectId]=i.[DefectId] AND link.[ProjectId]=i.[ProjectId]
                                          AND link.[SourceKind]=2 AND link.[SourceId]=d.[SourceAIDetectionId]
                                          AND link.[EndedAt] IS NULL AND decision.[Decision]=1)))))
                        THROW 51133, 'FIELD task source or geometry scope is invalid.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_FieldInspectionTasks_H4RepairPin] ON [FieldInspectionTasks] AFTER INSERT, UPDATE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.Id=i.Id
                        WHERE EXISTS (SELECT i.RepairItemId EXCEPT SELECT d.RepairItemId))
                        THROW 51321, 'Native repair task item pin is immutable.', 1;
                    IF EXISTS (SELECT 1 FROM inserted t WHERE t.TaskMode IN('NORMAL','CONDITIONAL_FT') AND NOT EXISTS
                        (SELECT 1 FROM RepairItems i JOIN RepairObligations o ON o.Id=i.ObligationId
                            JOIN RepairActualScopes s ON s.ObligationId=o.Id
                            JOIN RoadSectionVersions v ON v.Id=t.RoadSectionVersionId
                            WHERE i.Id=t.RepairItemId AND i.ProjectId=t.ProjectId AND i.DefectId=t.DefectId
                                AND s.PhysicalRoadId=v.RoadSectionId AND t.LifecycleVersion=2 AND t.Purpose=4
                                AND ((t.TaskMode='NORMAL' AND i.Mode=1) OR (t.TaskMode='CONDITIONAL_FT' AND i.Mode=2))
                                AND (EXISTS (SELECT 1 FROM deleted old WHERE old.Id=t.Id) OR
                                    (i.State=3 AND i.CrewId IS NOT NULL AND i.AssignedBy=t.AssignedByUserId
                                    AND i.RepairPlan IS NOT NULL AND i.ChecklistVersion IS NOT NULL
                                    AND i.ProposalPlanHash IS NOT NULL AND (i.Mode=2 OR i.ApprovedPlanHash=i.ProposalPlanHash)))))
                        THROW 51321, 'Native repair task must retain the actual assigned item, plan and physical scope.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_FieldTaskStartOrigins_Immutable] ON [dbo].[FieldTaskStartOrigins]
                AFTER UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM deleted)
                        THROW 51130, 'FIELD original history is immutable.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_FieldTaskStartOrigins_Scope] ON [dbo].[FieldTaskStartOrigins] AFTER INSERT AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM inserted i LEFT JOIN [FieldInspectionTasks] t ON t.[Id]=i.[TaskId]
                        WHERE t.[Id] IS NULL OR t.[ProjectId]<>i.[ProjectId] OR t.[LifecycleVersion]<>2 OR NOT EXISTS (SELECT 1 FROM [FieldInspectionAssignments] a
                    WHERE a.[Id]=i.[AssignmentId] AND a.[FieldInspectionTaskId]=i.[TaskId]
                      AND a.[AssignedToUserId]=i.[OriginalActorId])
                OR EXISTS (SELECT i.[RouteVersionId],i.[SegmentSetId],i.[LayoutRevisionId],i.[MapPublicationId],i.[CrsProfileRevisionId],i.[SlabId]
                    EXCEPT SELECT t.[RoadSectionVersionId],t.[SegmentSetId],t.[LayoutRevisionId],t.[MapPublicationId],t.[CrsProfileRevisionId],t.[SlabId])
                OR NOT EXISTS (SELECT 1 FROM [FieldInspectionOperationOrigins] o
                    WHERE o.[Id]=i.[OperationOriginId] AND o.[EffectId]=i.[Id]
                      AND o.[ProjectId]=i.[ProjectId] AND o.[TaskId]=i.[TaskId]
                      AND o.[OriginId]=i.[OriginId] AND o.[Kind]='FIELD_START'
                      AND o.[ContentHash]=i.[ContentHash] AND o.[OriginalActorId]=i.[OriginalActorId]
                      AND NOT EXISTS (SELECT o.[DeviceId],o.[ServerReceivedAt] EXCEPT SELECT i.[DeviceId],i.[ServerReceivedAt]))
                OR ISJSON(i.[ClaimEvidenceJson])<>1 OR LEFT(LTRIM(i.[ClaimEvidenceJson]),1)<>'{'
                OR i.[OperationKind]<>'FIELD_START'
                OR i.[TimeProvenance] NOT IN ('SERVER_ONLINE','CLAIMED_OFFLINE')
                OR (i.[TimeProvenance]='SERVER_ONLINE' AND (i.[VerifiedOriginalAt] IS NULL OR i.[VerifiedOriginalAt]<>i.[ServerReceivedAt]))
                OR (i.[TimeProvenance]='CLAIMED_OFFLINE' AND i.[VerifiedOriginalAt] IS NOT NULL))
                        THROW 51131, 'FIELD history scope, lineage or provenance is invalid.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_Files_Immutable]
                ON [Files]
                AFTER UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    THROW 51020, 'Files are immutable; create a new file identity and use the retention workflow for deletion.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_FileScopes_Immutable] ON [FileScopes] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51113, 'Immutable integration history cannot be rewritten.', 1; END;
                """);
            migrationBuilder.Sql("""
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
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_GeometryLocationImpactDecisions_Immutable] ON [dbo].[GeometryLocationImpactDecisions] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51230, 'H2 revision history is immutable.', 1; END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_GeometryLocationImpacts_Immutable] ON [dbo].[GeometryLocationImpacts] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51230, 'H2 revision history is immutable.', 1; END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_GeometryLocationImpacts_Scope] ON [dbo].[GeometryLocationImpacts] AFTER INSERT AS BEGIN SET NOCOUNT ON; IF EXISTS(SELECT 1 FROM inserted i JOIN RoadSectionVersions p ON p.Id=i.PreviousRouteVersionId JOIN RoadSectionVersions n ON n.Id=i.NewRouteVersionId JOIN RoadSections r ON r.Id=p.RoadSectionId WHERE p.RoadSectionId<>n.RoadSectionId OR r.ProjectId<>i.ProjectId) THROW 51231, 'Impact route scope mismatch.', 1; END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_GeometryMapPublications_Immutable] ON [dbo].[GeometryMapPublications] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51230, 'H2 revision history is immutable.', 1; END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_GeometryMapPublications_Scope] ON [dbo].[GeometryMapPublications] AFTER INSERT AS BEGIN SET NOCOUNT ON; IF EXISTS(SELECT 1 FROM inserted i WHERE NOT EXISTS(SELECT 1 FROM PavementLayoutRevisions l WHERE l.Id=i.LayoutRevisionId AND l.ProjectId=i.ProjectId AND l.RouteVersionId=i.RouteVersionId AND l.SegmentSetId=i.SegmentSetId AND ((l.CrsProfileRevisionId=i.CrsProfileRevisionId) OR (l.CrsProfileRevisionId IS NULL AND i.CrsProfileRevisionId IS NULL)))) THROW 51231, 'Map source scope mismatch.', 1; IF EXISTS(SELECT 1 FROM inserted i WHERE i.PublicationMode='OFFICIAL' AND NOT EXISTS(SELECT 1 FROM CrsProfileRevisions p WHERE p.Id=i.CrsProfileRevisionId AND p.ProjectId=i.ProjectId AND p.Status='VERIFIED' AND p.SampleOnly=0)) THROW 51231, 'Official profile is not verified.', 1; END
                """);
            migrationBuilder.Sql("""
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
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_GroundTruthMeasurements_Integrity] ON [dbo].[GroundTruthMeasurements] AFTER INSERT, UPDATE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM inserted m JOIN [FieldInspectionSessions] s ON s.[Id]=m.[FieldInspectionSessionId]
                        LEFT JOIN [FieldInspectionTasks] t ON t.[Id]=s.[FieldInspectionTaskId]
                        LEFT JOIN [Defects] d ON d.[Id]=m.[DefectId]
                        WHERE m.[RoadSectionVersionId]<>s.[RoadSectionVersionId]
                            OR (s.[Purpose] IN (1,3,4,5) AND
                                (m.[DefectId] IS NULL OR m.[DefectId]<>t.[DefectId] OR d.[ProjectId]<>s.[ProjectId]
                                 OR d.[RoadSectionVersionId]<>s.[RoadSectionVersionId]
                                 OR EXISTS (SELECT m.[SurveyId] EXCEPT SELECT s.[SurveyId])
                                 OR (s.[Purpose]=1 AND (m.[SurveyId] IS NULL OR d.[Status]<>1))))
                            OR (s.[Purpose]=2 AND (m.[DefectId] IS NOT NULL OR m.[SurveyId] IS NOT NULL)))
                        THROW 51042, 'Ground truth measurement purpose or scope is invalid.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_H6NotificationAudits_Immutable] ON [H6NotificationAudits]
                AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; IF EXISTS(SELECT 1 FROM deleted) THROW 51182, 'Notification audit history is immutable.', 1; END;
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_H6NotificationCalendar_Identity] ON [H6NotificationCalendar]
                AFTER UPDATE, DELETE AS BEGIN
                 SET NOCOUNT ON;
                 IF EXISTS(SELECT 1 FROM deleted d WHERE NOT EXISTS(SELECT 1 FROM inserted i WHERE i.Id=d.Id))
                   THROW 51192, 'Calendar occurrence history cannot be deleted.', 1;
                 IF EXISTS(SELECT 1 FROM inserted i JOIN deleted d ON d.Id=i.Id WHERE
                   i.ClockId<>d.ClockId OR i.ScheduledAtUtc<>d.ScheduledAtUtc OR i.PlannedAtUtc<>d.PlannedAtUtc
                   OR i.SchedulerRunId<>d.SchedulerRunId
                   OR (d.OutboxMessageId IS NOT NULL AND NOT EXISTS(SELECT i.OutboxMessageId INTERSECT SELECT d.OutboxMessageId))
                   OR (d.Status IN ('COMMITTED','PENDING_POLICY','NO_REVIEW') AND EXISTS(
                      SELECT i.Status,i.OutboxMessageId,i.ObservedAtUtc EXCEPT SELECT d.Status,d.OutboxMessageId,d.ObservedAtUtc)))
                   THROW 51193, 'Calendar period, planning proof and final observation are immutable.', 1;
                END;
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_H6NotificationDeliveries_Identity] ON [H6NotificationDeliveries]
                AFTER UPDATE, DELETE AS BEGIN
                 SET NOCOUNT ON;
                 IF EXISTS(SELECT 1 FROM deleted d WHERE NOT EXISTS(SELECT 1 FROM inserted i WHERE i.Id=d.Id))
                   THROW 51185, 'Notification delivery history cannot be deleted.', 1;
                 IF EXISTS(SELECT 1 FROM inserted i JOIN deleted d ON d.Id=i.Id WHERE
                   i.OccurrenceId<>d.OccurrenceId OR i.RecipientKey COLLATE Latin1_General_100_BIN2<>d.RecipientKey COLLATE Latin1_General_100_BIN2
                   OR DATALENGTH(i.RecipientKey)<>DATALENGTH(d.RecipientKey)
                   OR (d.RecipientUserId IS NOT NULL AND NOT EXISTS(SELECT i.RecipientUserId INTERSECT SELECT d.RecipientUserId))
                   OR (d.Status='DELIVERED' AND EXISTS(SELECT i.Status,i.NotificationId,i.DeliveredAtUtc,i.ReasonCode
                                                    EXCEPT SELECT d.Status,d.NotificationId,d.DeliveredAtUtc,d.ReasonCode)))
                   THROW 51186, 'A pinned recipient or committed delivery cannot be retargeted.', 1;
                END;
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_H6NotificationDeliveries_Scope] ON [H6NotificationDeliveries]
                AFTER INSERT, UPDATE AS BEGIN
                 SET NOCOUNT ON;
                 IF EXISTS(SELECT 1 FROM inserted i WHERE i.NotificationId IS NOT NULL AND NOT EXISTS(
                   SELECT 1 FROM [Notifications] n JOIN [H6NotificationOccurrences] o ON o.Id=i.OccurrenceId
                   WHERE n.Id=i.NotificationId AND n.RecipientUserId=i.RecipientUserId
                   AND n.SourceEntityId=o.SourceId AND n.SourceEntityType COLLATE Latin1_General_100_BIN2=o.SourceKind COLLATE Latin1_General_100_BIN2
                   AND DATALENGTH(n.SourceEntityType)=DATALENGTH(o.SourceKind)
                   AND n.EventType COLLATE Latin1_General_100_BIN2=o.OccurrenceKey COLLATE Latin1_General_100_BIN2
                   AND DATALENGTH(n.EventType)=DATALENGTH(o.OccurrenceKey) AND n.OccurredAtUtc=o.OccurredAtUtc))
                   THROW 51187, 'Notification effect must match its occurrence and actual recipient.', 1;
                END;
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_H6NotificationDeliveryAttempts_Immutable] ON [H6NotificationDeliveryAttempts]
                AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; IF EXISTS(SELECT 1 FROM deleted) THROW 51181, 'Notification attempt facts are immutable.', 1; END;
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_H6NotificationEventReceipts_Immutable] ON [H6NotificationEventReceipts]
                AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; IF EXISTS(SELECT 1 FROM deleted) THROW 51183, 'Notification completion receipts are immutable.', 1; END;
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_H6NotificationOccurrences_Immutable] ON [H6NotificationOccurrences]
                AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; IF EXISTS(SELECT 1 FROM deleted) THROW 51180, 'Notification occurrence facts are immutable.', 1; END;
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_H6NotificationOccurrences_Scope] ON [H6NotificationOccurrences]
                AFTER INSERT AS BEGIN
                 SET NOCOUNT ON;
                 IF EXISTS(SELECT 1 FROM inserted i WHERE NOT EXISTS(
                   SELECT 1 FROM [OutboxMessages] o WHERE o.Id=i.SourceEventId
                   AND o.MessageType COLLATE Latin1_General_100_BIN2=i.EventType COLLATE Latin1_General_100_BIN2
                   AND DATALENGTH(o.MessageType)=DATALENGTH(i.EventType) AND o.OccurredAtUtc=i.OccurredAtUtc
                   AND TRY_CONVERT(uniqueidentifier,JSON_VALUE(o.PayloadJson,'$.projectId'))=i.ProjectId
                   AND TRY_CONVERT(uniqueidentifier,JSON_VALUE(o.PayloadJson,'$.sourceId'))=i.SourceId
                   AND TRY_CONVERT(uniqueidentifier,JSON_VALUE(o.PayloadJson,'$.originEventId'))=i.OriginEventId
                   AND JSON_VALUE(o.PayloadJson,'$.sourceKind') COLLATE Latin1_General_100_BIN2=i.SourceKind COLLATE Latin1_General_100_BIN2
                   AND DATALENGTH(CONVERT(varchar(80),JSON_VALUE(o.PayloadJson,'$.sourceKind')))=DATALENGTH(i.SourceKind)))
                   THROW 51184, 'Notification occurrence must mirror its actual admitted transport source.', 1;
                END;
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_H6NotificationScopes_Source] ON [H6NotificationScopes]
                AFTER INSERT, UPDATE, DELETE AS BEGIN
                 SET NOCOUNT ON;
                 IF EXISTS(SELECT 1 FROM deleted d WHERE NOT EXISTS(SELECT 1 FROM inserted i WHERE i.NotificationId=d.NotificationId))
                   THROW 51188, 'Notification source scope cannot be deleted.', 1;
                 IF EXISTS(SELECT 1 FROM inserted i JOIN deleted d ON d.NotificationId=i.NotificationId WHERE
                   i.SourceId<>d.SourceId OR i.SourceKind COLLATE Latin1_General_100_BIN2<>d.SourceKind COLLATE Latin1_General_100_BIN2
                   OR DATALENGTH(i.SourceKind)<>DATALENGTH(d.SourceKind)
                   OR i.EventType COLLATE Latin1_General_100_BIN2<>d.EventType COLLATE Latin1_General_100_BIN2
                   OR DATALENGTH(i.EventType)<>DATALENGTH(d.EventType)
                   OR (d.ProjectId IS NOT NULL AND NOT EXISTS(SELECT i.ProjectId INTERSECT SELECT d.ProjectId))
                   OR (d.OccurrenceId IS NOT NULL AND NOT EXISTS(SELECT i.OccurrenceId INTERSECT SELECT d.OccurrenceId)))
                   THROW 51189, 'Known notification source identities cannot be relabelled.', 1;
                 IF EXISTS(SELECT 1 FROM inserted i WHERE NOT EXISTS(
                   SELECT 1 FROM [Notifications] n JOIN [H6NotificationAudits] a ON a.Id=i.CurrentAuditId
                   WHERE n.Id=i.NotificationId AND n.SourceEntityId=i.SourceId
                   AND n.SourceEntityType COLLATE Latin1_General_100_BIN2=i.SourceKind COLLATE Latin1_General_100_BIN2
                   AND DATALENGTH(n.SourceEntityType)=DATALENGTH(i.SourceKind)
                   AND a.NotificationId=i.NotificationId AND a.SourceId=i.SourceId
                   AND a.SourceKind COLLATE Latin1_General_100_BIN2=i.SourceKind COLLATE Latin1_General_100_BIN2
                   AND DATALENGTH(a.SourceKind)=DATALENGTH(i.SourceKind)
                   AND a.Classification=i.Classification AND a.ResolverVersion=i.ResolverVersion
                   AND (a.ProjectId=i.ProjectId OR (a.ProjectId IS NULL AND i.ProjectId IS NULL))))
                   THROW 51190, 'Notification scope must have actual matching immutable audit facts.', 1;
                 IF EXISTS(SELECT 1 FROM inserted i WHERE i.OccurrenceId IS NOT NULL AND NOT EXISTS(
                   SELECT 1 FROM [H6NotificationOccurrences] o JOIN [Notifications] n ON n.Id=i.NotificationId
                   WHERE o.Id=i.OccurrenceId AND o.ProjectId=i.ProjectId AND o.SourceId=i.SourceId
                   AND o.SourceKind COLLATE Latin1_General_100_BIN2=i.SourceKind COLLATE Latin1_General_100_BIN2
                   AND DATALENGTH(o.SourceKind)=DATALENGTH(i.SourceKind)
                   AND o.EventType COLLATE Latin1_General_100_BIN2=i.EventType COLLATE Latin1_General_100_BIN2
                   AND DATALENGTH(o.EventType)=DATALENGTH(i.EventType)
                   AND n.EventType COLLATE Latin1_General_100_BIN2=o.OccurrenceKey COLLATE Latin1_General_100_BIN2
                   AND DATALENGTH(n.EventType)=DATALENGTH(o.OccurrenceKey)))
                   THROW 51191, 'Project notification scope must match its pinned occurrence.', 1;
                END;
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER TR_LD06ActionEvidence_Immutable ON LD06ActionEvidence AFTER UPDATE,DELETE AS
                BEGIN SET NOCOUNT ON;
                  IF EXISTS(SELECT 1 FROM deleted) THROW 51603, 'Lifecycle evidence pins are append-only.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER TR_LD06LifecycleActions_Immutable ON LD06LifecycleActions AFTER UPDATE,DELETE AS
                BEGIN SET NOCOUNT ON;
                  IF EXISTS(SELECT 1 FROM deleted) THROW 51600, 'Confirmed lifecycle actions are append-only.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER TR_LD06LifecycleActions_Scope ON LD06LifecycleActions AFTER INSERT AS
                BEGIN SET NOCOUNT ON;
                  IF EXISTS(SELECT 1 FROM inserted i WHERE
                    (i.Kind=1 AND i.SourceActionId IS NOT NULL)
                    OR (i.Kind=2 AND NOT EXISTS(SELECT 1 FROM LD06LifecycleActions s WHERE s.Id=i.SourceActionId
                        AND s.Kind=1 AND s.ProjectId=i.ProjectId AND s.At<=i.At))
                    OR (i.Kind IN (3,5) AND NOT EXISTS(SELECT 1 FROM Defects d WHERE d.Id=i.DefectId AND d.ProjectId=i.ProjectId))
                    OR (i.Kind=5 AND (i.LinkedDefectId=i.DefectId OR NOT EXISTS(SELECT 1 FROM Defects d WHERE d.Id=i.LinkedDefectId AND d.ProjectId=i.ProjectId)
                        OR NOT EXISTS(SELECT 1 FROM RepairDecisions d JOIN RepairObligations o ON o.Id=d.ObligationId
                            WHERE d.Id=i.PriorRepairDecisionId AND o.DefectId=i.DefectId AND o.ProjectId=i.ProjectId
                            AND o.EffectiveResolutionDecisionId=d.Id)))
                    OR (i.Kind=6 AND (i.ReceivingProjectId IS NULL OR i.ReceivingProjectId=i.ProjectId OR LEN(i.ScopeHash)<>64
                        OR NOT EXISTS(SELECT 1 FROM RepairObligations o LEFT JOIN ObligationResponsibilities r ON r.ObligationId=o.Id
                            WHERE o.Id=i.ObligationId AND COALESCE(r.CurrentProjectId,o.ProjectId)=i.ProjectId)))
                    OR (i.Kind=7 AND NOT EXISTS(SELECT 1 FROM LD06LifecycleActions s WHERE s.Id=i.SourceActionId AND s.Kind=6
                        AND s.ReceivingProjectId=i.ProjectId AND s.ObligationId=i.ObligationId AND s.ScopeHash=i.ScopeHash
                        AND s.At<=i.At)))
                    THROW 51601, 'Lifecycle action must retain its exact source, project and scope.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_NativeRouteVersionFacts_Immutable] ON [dbo].[NativeRouteVersionFacts] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51230, 'H2 revision history is immutable.', 1; END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_NativeRouteVersionFacts_Scope] ON [dbo].[NativeRouteVersionFacts] AFTER INSERT AS BEGIN SET NOCOUNT ON; IF EXISTS(SELECT 1 FROM inserted i JOIN RoadSectionVersions v ON v.Id=i.RoadSectionVersionId JOIN RoadSections r ON r.Id=v.RoadSectionId JOIN CrsProfileRevisions c ON c.Id=i.CrsProfileRevisionId WHERE r.ProjectId<>i.ProjectId OR v.CrsProfileRevisionId IS NULL OR v.CrsProfileRevisionId<>i.CrsProfileRevisionId OR c.SampleOnly<>i.SampleOnly) THROW 51231, 'Native route scope mismatch.', 1; IF EXISTS(SELECT 1 FROM inserted i WHERE i.RouteKind='BRANCH' AND NOT EXISTS(SELECT 1 FROM NativeRouteVersionFacts p JOIN RoadSectionVersions pv ON pv.Id=p.RoadSectionVersionId JOIN RoadSectionVersions cv ON cv.Id=i.RoadSectionVersionId WHERE p.RoadSectionVersionId=i.ParentRouteVersionId AND p.ProjectId=i.ProjectId AND p.RouteSystemId=i.RouteSystemId AND p.CrsProfileRevisionId=i.CrsProfileRevisionId AND pv.RoadSectionId<>cv.RoadSectionId AND pv.IsCurrent=1 AND i.JunctionOffsetMeters<=p.CanonicalLengthMeters)) THROW 51231, 'Native branch scope mismatch.', 1; DECLARE @bad bit=0; WITH walk AS (SELECT i.RoadSectionVersionId AS RootId,v.RoadSectionId AS RootSectionId,i.ParentRouteVersionId AS NextId,CAST('|' + CONVERT(varchar(36),i.RoadSectionVersionId) + '|' AS varchar(max)) AS Seen,CAST('|' + CONVERT(varchar(36),v.RoadSectionId) + '|' AS varchar(max)) AS SeenSections,CAST(0 AS bit) AS Bad,0 AS Depth FROM inserted i JOIN RoadSectionVersions v ON v.Id=i.RoadSectionVersionId UNION ALL SELECT w.RootId,w.RootSectionId,p.ParentRouteVersionId,CAST(w.Seen + CONVERT(varchar(36),p.RoadSectionVersionId) + '|' AS varchar(max)),CAST(w.SeenSections + CONVERT(varchar(36),v.RoadSectionId) + '|' AS varchar(max)),CAST(CASE WHEN CHARINDEX('|' + CONVERT(varchar(36),p.RoadSectionVersionId) + '|',w.Seen)>0 OR CHARINDEX('|' + CONVERT(varchar(36),v.RoadSectionId) + '|',w.SeenSections)>0 THEN 1 ELSE 0 END AS bit),w.Depth+1 FROM walk w JOIN NativeRouteVersionFacts p ON p.RoadSectionVersionId=w.NextId JOIN RoadSectionVersions v ON v.Id=p.RoadSectionVersionId WHERE w.Bad=0 AND w.Depth<100000) SELECT @bad=1 FROM walk WHERE Bad=1 OR Depth>=100000 OPTION(MAXRECURSION 0); IF @bad=1 THROW 51231, 'Native route topology cycle or depth bound exceeded.', 1; END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER TR_ObligationResponsibilities_Scope ON ObligationResponsibilities AFTER INSERT,UPDATE,DELETE AS
                BEGIN SET NOCOUNT ON;
                  IF EXISTS(SELECT 1 FROM deleted d LEFT JOIN inserted i ON i.ObligationId=d.ObligationId
                      WHERE i.ObligationId IS NULL OR i.OriginProjectId<>d.OriginProjectId)
                    THROW 51602, 'Responsibility history and original project cannot be erased.', 1;
                  IF EXISTS(SELECT 1 FROM inserted i WHERE NOT EXISTS(
                      SELECT 1 FROM RepairObligations o JOIN LD06LifecycleActions a ON a.ObligationId=o.Id
                      JOIN LD06LifecycleActions s ON s.Id=a.SourceActionId
                      WHERE o.Id=i.ObligationId AND o.ProjectId=i.OriginProjectId AND a.Id=i.AcceptanceActionId
                        AND a.Kind=7 AND s.Kind=6 AND a.ProjectId=i.CurrentProjectId
                        AND s.ReceivingProjectId=a.ProjectId AND a.ScopeHash=s.ScopeHash))
                    THROW 51602, 'Responsibility requires a committed exact receiving-project acceptance.', 1;
                  IF EXISTS(SELECT 1 FROM inserted i JOIN deleted d ON d.ObligationId=i.ObligationId
                      JOIN LD06LifecycleActions a ON a.Id=i.AcceptanceActionId JOIN LD06LifecycleActions s ON s.Id=a.SourceActionId
                      WHERE i.AcceptanceActionId=d.AcceptanceActionId OR s.ProjectId<>d.CurrentProjectId)
                    THROW 51602, 'Responsibility changes must continue the current accepted owner.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_OfflineAdmittedFileReferences_Immutable] ON [OfflineAdmittedFileReferences] AFTER UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS(SELECT 1 FROM deleted) THROW 51400, 'Offline source history is append-only.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_OfflineAdmittedFileReferences_Scope] ON [OfflineAdmittedFileReferences] AFTER INSERT AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS(SELECT 1 FROM inserted i WHERE NOT EXISTS(SELECT 1 FROM OfflineOperationAdmissions a JOIN OfflineOperationBindings b ON b.Id=a.BindingId
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
                            AND u.ExpectedChecksumSha256=i.ContentChecksum)))
                        THROW 51401, 'Offline retained source scope does not match.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_OfflineDeviceRegistrations_Immutable] ON [OfflineDeviceRegistrations] AFTER UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS(SELECT 1 FROM deleted) THROW 51400, 'Offline source history is append-only.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_OfflineDeviceRegistrations_Scope] ON [OfflineDeviceRegistrations] AFTER INSERT AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS(SELECT 1 FROM inserted i WHERE i.RoleSnapshot NOT IN (2,4))
                        THROW 51401, 'Offline retained source scope does not match.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_OfflineDeviceRevocations_Immutable] ON [OfflineDeviceRevocations] AFTER UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS(SELECT 1 FROM deleted) THROW 51400, 'Offline source history is append-only.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_OfflineDeviceRevocations_Scope] ON [OfflineDeviceRevocations] AFTER INSERT AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS(SELECT 1 FROM inserted i WHERE NOT EXISTS(SELECT 1 FROM OfflineDeviceRegistrations d WHERE d.Id=i.DeviceRegistrationId AND d.ProjectId=i.ProjectId AND d.RegisteredAt<=i.RevokedAt))
                        THROW 51401, 'Offline retained source scope does not match.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_OfflineEncryptedCaptureArtifacts_Immutable] ON [OfflineEncryptedCaptureArtifacts] AFTER UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS(SELECT 1 FROM deleted) THROW 51400, 'Offline source history is append-only.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_OfflineEncryptedCaptureArtifacts_Scope] ON [OfflineEncryptedCaptureArtifacts] AFTER INSERT AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS(SELECT 1 FROM inserted i WHERE NOT EXISTS(SELECT 1 FROM OfflineEncryptedPackages p JOIN FieldInspectionTasks t ON t.Id=i.TaskId
                    WHERE p.Id=i.ParentPackageId AND p.ProjectId=i.ProjectId AND t.ProjectId=i.ProjectId
                        AND p.OriginalActorId=i.OriginalActorId AND p.SourceDeviceRegistrationId=i.SourceDeviceRegistrationId))
                        THROW 51401, 'Offline retained source scope does not match.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_OfflineEncryptedPackages_Immutable] ON [OfflineEncryptedPackages] AFTER UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS(SELECT 1 FROM deleted) THROW 51400, 'Offline source history is append-only.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_OfflineEncryptedPackages_Scope] ON [OfflineEncryptedPackages] AFTER INSERT AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS(SELECT 1 FROM inserted i WHERE NOT EXISTS(SELECT 1 FROM OfflineDeviceRegistrations d WHERE d.Id=i.SourceDeviceRegistrationId
                    AND d.ProjectId=i.ProjectId AND d.ActorId=i.OriginalActorId))
                        THROW 51401, 'Offline retained source scope does not match.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_OfflineEvidenceCaptureReferences_Immutable] ON [OfflineEvidenceCaptureReferences] AFTER UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS(SELECT 1 FROM deleted) THROW 51400, 'Offline source history is append-only.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_OfflineEvidenceCaptureReferences_Scope] ON [OfflineEvidenceCaptureReferences] AFTER INSERT AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS(SELECT 1 FROM inserted i WHERE NOT EXISTS(SELECT 1 FROM OfflineOperationAdmissions a JOIN OfflineOperationBindings b ON b.Id=a.BindingId
                    WHERE a.Id=i.AdmissionId AND a.ProjectId=i.ProjectId AND b.Id=i.BindingId AND b.TaskId=i.TaskId
                        AND b.OriginalActorId=i.OriginalActorId AND a.CurrentImporterId=i.ActualUploaderId
                        AND (a.GrantId=i.GrantId OR (a.GrantId IS NULL AND i.GrantId IS NULL))))
                        THROW 51401, 'Offline retained source scope does not match.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_OfflineHandoverGrantRevocations_Immutable] ON [OfflineHandoverGrantRevocations] AFTER UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS(SELECT 1 FROM deleted) THROW 51400, 'Offline source history is append-only.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_OfflineHandoverGrantRevocations_Scope] ON [OfflineHandoverGrantRevocations] AFTER INSERT AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS(SELECT 1 FROM inserted i WHERE NOT EXISTS(SELECT 1 FROM OfflineHandoverGrants g WHERE g.Id=i.GrantId AND g.ProjectId=i.ProjectId AND g.IssuedAt<=i.RevokedAt))
                        THROW 51401, 'Offline retained source scope does not match.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_OfflineHandoverGrants_Immutable] ON [OfflineHandoverGrants] AFTER UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS(SELECT 1 FROM deleted) THROW 51400, 'Offline source history is append-only.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_OfflineHandoverGrants_Scope] ON [OfflineHandoverGrants] AFTER INSERT AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS(SELECT 1 FROM inserted i WHERE NOT EXISTS(SELECT 1 FROM OfflineEncryptedPackages p
                    JOIN OfflineDeviceRegistrations s ON s.Id=i.SourceDeviceRegistrationId
                    JOIN OfflineDeviceRegistrations r ON r.Id=i.RecipientDeviceRegistrationId
                    WHERE p.Id=i.PackageId AND p.ProjectId=i.ProjectId AND p.OriginalActorId=i.SourceActorId
                        AND p.SourceDeviceRegistrationId=s.Id AND p.ManifestHash=i.ManifestHash
                        AND s.ProjectId=i.ProjectId AND s.ActorId=i.SourceActorId
                        AND r.ProjectId=i.ProjectId AND r.ActorId=i.RecipientActorId AND r.RoleSnapshot=i.RecipientRole))
                        THROW 51401, 'Offline retained source scope does not match.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_OfflineOperationAdmissions_Immutable] ON [OfflineOperationAdmissions] AFTER UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS(SELECT 1 FROM deleted) THROW 51400, 'Offline source history is append-only.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_OfflineOperationAdmissions_Scope] ON [OfflineOperationAdmissions] AFTER INSERT AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS(SELECT 1 FROM inserted i WHERE NOT EXISTS(SELECT 1 FROM OfflineSyncBatches b JOIN OfflineOperationBindings o ON o.Id=i.BindingId
                    WHERE b.Id=i.BatchId AND b.ProjectId=i.ProjectId AND o.ProjectId=i.ProjectId
                        AND b.CurrentImporterId=i.CurrentImporterId AND b.SourceDeviceRegistrationId=o.SourceDeviceRegistrationId
                        AND (b.GrantId=i.GrantId OR (b.GrantId IS NULL AND i.GrantId IS NULL))))
                        THROW 51401, 'Offline retained source scope does not match.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_OfflineOperationBindings_Immutable] ON [OfflineOperationBindings] AFTER UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS(SELECT 1 FROM deleted) THROW 51400, 'Offline source history is append-only.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_OfflineOperationBindings_Scope] ON [OfflineOperationBindings] AFTER INSERT AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS(SELECT 1 FROM inserted i WHERE NOT EXISTS(SELECT 1 FROM OfflineTaskSnapshots s WHERE s.Id=i.SnapshotId AND s.ProjectId=i.ProjectId
                    AND s.TaskId=i.TaskId AND s.AssignmentId=i.AssignmentId AND s.OriginalActorId=i.OriginalActorId
                    AND s.DeviceRegistrationId=i.SourceDeviceRegistrationId)
                OR (i.RepairResourceId IS NOT NULL AND NOT EXISTS(SELECT 1 FROM RepairFieldTaskBindings b
                    WHERE b.ItemId=i.RepairResourceId AND b.TaskId=i.TaskId AND b.AssignmentId=i.AssignmentId
                        AND b.ProjectId=i.ProjectId AND b.CrewId=i.OriginalActorId)))
                        THROW 51401, 'Offline retained source scope does not match.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_OfflineOperationResults_Immutable] ON [OfflineOperationResults] AFTER UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS(SELECT 1 FROM deleted) THROW 51400, 'Offline source history is append-only.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_OfflineOperationResults_Scope] ON [OfflineOperationResults] AFTER INSERT AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS(SELECT 1 FROM inserted i WHERE NOT EXISTS(SELECT 1 FROM OfflineOperationAdmissions a JOIN OfflineOperationBindings b ON b.Id=a.BindingId
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
                            AND e.OperationOriginId=c.Id AND f.AssignmentId=b.AssignmentId AND e.ItemId=b.RepairResourceId))))))
                        THROW 51401, 'Offline retained source scope does not match.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_OfflineOriginTimeVerifications_Immutable] ON [OfflineOriginTimeVerifications] AFTER UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS(SELECT 1 FROM deleted) THROW 51400, 'Offline source history is append-only.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_OfflineOriginTimeVerifications_Scope] ON [OfflineOriginTimeVerifications] AFTER INSERT AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS(SELECT 1 FROM inserted i WHERE NOT EXISTS(SELECT 1 FROM OfflineOperationBindings b JOIN FieldInspectionOperationOrigins c ON c.Id=i.CanonicalOriginId
                    WHERE b.Id=i.BindingId AND b.ProjectId=i.ProjectId AND b.EffectId=i.TypedEffectId
                        AND c.ProjectId=i.ProjectId AND c.TaskId=b.TaskId AND c.OriginId=b.OriginId
                        AND c.EffectId=b.EffectId AND c.ContentHash=b.CorePayloadHash AND c.OriginalActorId=b.OriginalActorId)
                OR (i.ProofSourceKind='FIELD_START_SERVER_ORIGIN' AND NOT EXISTS(SELECT 1 FROM FieldTaskStartOrigins s
                    WHERE s.Id=i.ProofSourceId AND s.Id=i.TypedEffectId AND s.OperationOriginId=i.CanonicalOriginId
                        AND s.ProjectId=i.ProjectId AND s.VerifiedOriginalAt=i.OriginalOccurredAtUtc))
                OR (i.ProofSourceKind='REPAIR_FINISH_SERVER_ORIGIN' AND NOT EXISTS(SELECT 1 FROM RepairExecutionFinishes f
                    WHERE f.Id=i.ProofSourceId AND f.Id=i.TypedEffectId AND f.OperationOriginId=i.CanonicalOriginId
                        AND f.ProjectId=i.ProjectId AND f.VerifiedOriginalAt=i.OriginalOccurredAtUtc)))
                        THROW 51401, 'Offline retained source scope does not match.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_OfflinePackageFileReferences_Immutable] ON [OfflinePackageFileReferences] AFTER UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS(SELECT 1 FROM deleted) THROW 51400, 'Offline source history is append-only.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_OfflinePackageFileReferences_Scope] ON [OfflinePackageFileReferences] AFTER INSERT AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS(SELECT 1 FROM inserted i WHERE NOT EXISTS(SELECT 1 FROM Files f WHERE f.Id=i.FileId AND f.Checksum=i.ContentChecksum))
                        THROW 51401, 'Offline retained source scope does not match.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_OfflineSyncBatches_Immutable] ON [OfflineSyncBatches] AFTER UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS(SELECT 1 FROM deleted) THROW 51400, 'Offline source history is append-only.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_OfflineSyncBatches_Scope] ON [OfflineSyncBatches] AFTER INSERT AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS(SELECT 1 FROM inserted i WHERE (i.GrantId IS NULL AND NOT EXISTS(SELECT 1 FROM OfflineDeviceRegistrations s
                    WHERE s.Id=i.SourceDeviceRegistrationId AND s.ProjectId=i.ProjectId AND s.ActorId=i.CurrentImporterId))
                OR (i.GrantId IS NOT NULL AND NOT EXISTS(SELECT 1 FROM OfflineHandoverGrants g
                    WHERE g.Id=i.GrantId AND g.ProjectId=i.ProjectId AND g.PackageId=i.PackageId
                        AND g.SourceDeviceRegistrationId=i.SourceDeviceRegistrationId
                        AND g.RecipientDeviceRegistrationId=i.RecipientDeviceRegistrationId AND g.RecipientActorId=i.CurrentImporterId)))
                        THROW 51401, 'Offline retained source scope does not match.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_OfflineTaskSnapshots_Immutable] ON [OfflineTaskSnapshots] AFTER UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS(SELECT 1 FROM deleted) THROW 51400, 'Offline source history is append-only.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_OfflineTaskSnapshots_Scope] ON [OfflineTaskSnapshots] AFTER INSERT AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS(SELECT 1 FROM inserted i WHERE NOT EXISTS(SELECT 1 FROM FieldInspectionTasks t JOIN FieldInspectionAssignments a ON a.FieldInspectionTaskId=t.Id
                    JOIN OfflineDeviceRegistrations d ON d.Id=i.DeviceRegistrationId
                    WHERE t.Id=i.TaskId AND t.ProjectId=i.ProjectId AND a.Id=i.AssignmentId
                        AND a.AssignedToUserId=i.OriginalActorId AND d.ActorId=i.OriginalActorId AND d.ProjectId=i.ProjectId))
                        THROW 51401, 'Offline retained source scope does not match.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_PasswordResetLogs_AppendOnly]
                ON [PasswordResetLogs]
                INSTEAD OF UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    THROW 51000, 'PasswordResetLogs are append-only; updates and deletions are forbidden.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_PavementLayoutRevisions_Immutable] ON [dbo].[PavementLayoutRevisions] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51230, 'H2 revision history is immutable.', 1; END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_PavementLayoutRevisions_Scope] ON [dbo].[PavementLayoutRevisions] AFTER INSERT AS BEGIN SET NOCOUNT ON; IF EXISTS(SELECT 1 FROM inserted i JOIN RoadSectionVersions v ON v.Id=i.RouteVersionId JOIN RoadSections r ON r.Id=v.RoadSectionId JOIN RoadSegmentSets s ON s.Id=i.SegmentSetId WHERE r.ProjectId<>i.ProjectId OR s.RoadSectionVersionId<>i.RouteVersionId OR NOT EXISTS(SELECT 1 WHERE (v.CrsProfileRevisionId=i.CrsProfileRevisionId) OR (v.CrsProfileRevisionId IS NULL AND i.CrsProfileRevisionId IS NULL))) THROW 51231, 'Layout scope mismatch.', 1; IF EXISTS(SELECT 1 FROM inserted i WHERE i.Kind='AS_BUILT' AND NOT EXISTS(SELECT 1 FROM PavementLayoutRevisions p WHERE p.Id=i.SourcePlanId AND p.Kind='PLANNED' AND p.ProjectId=i.ProjectId AND p.RouteVersionId=i.RouteVersionId AND p.SegmentSetId=i.SegmentSetId AND ((p.CrsProfileRevisionId=i.CrsProfileRevisionId) OR (p.CrsProfileRevisionId IS NULL AND i.CrsProfileRevisionId IS NULL)))) THROW 51231, 'As-built source plan scope mismatch.', 1; END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_PavementSourceFileReferences_Immutable] ON [dbo].[PavementSourceFileReferences] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51230, 'H2 revision history is immutable.', 1; END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_PavementSourceFileReferences_Scope] ON [dbo].[PavementSourceFileReferences] AFTER INSERT AS BEGIN SET NOCOUNT ON; IF EXISTS(SELECT 1 FROM inserted i JOIN PavementLayoutRevisions l ON l.Id=i.LayoutRevisionId JOIN Files f ON f.Id=i.FileId WHERE f.Checksum<>i.ContentChecksum OR NOT EXISTS(SELECT 1 FROM FileScopes s WHERE s.FileId=i.FileId AND s.ProjectId=l.ProjectId AND s.Purpose<>'REPORT_PHOTO')) THROW 51231, 'Pavement source file scope or checksum mismatch.', 1; END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [dbo].[TR_ProcessingAttempts_AppendOnly]
                ON [dbo].[ProcessingAttempts] AFTER UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS(SELECT 1 FROM deleted d LEFT JOIN inserted i ON i.Id=d.Id WHERE i.Id IS NULL)
                        THROW 51032, 'ProcessingAttempts are append-only.', 1;
                    IF EXISTS (
                        SELECT 1 FROM inserted i JOIN deleted d ON d.Id=i.Id
                        WHERE d.EndedAt IS NOT NULL OR d.ErrorType IS NOT NULL OR i.EndedAt IS NULL
                           OR i.EndedAt < d.StartedAt OR i.ErrorType IS NULL OR i.ErrorType NOT IN (2,3)
                           OR EXISTS (SELECT i.ProcessingJobId,i.AttemptNo,i.StartedAt,i.WorkerReference
                                      EXCEPT SELECT d.ProcessingJobId,d.AttemptNo,d.StartedAt,d.WorkerReference)
                           OR NOT EXISTS (
                               SELECT 1 FROM dbo.Anh02AiMockRuns r JOIN dbo.ProcessingJobs j ON j.Id=r.ProcessingJobId
                               WHERE r.AttemptId=i.Id AND r.ProcessingJobId=i.ProcessingJobId
                                 AND r.Stage='VIDEO_ANALYSIS' AND j.Mode='MOCK' AND r.CompletedAt=i.EndedAt
                                 AND ((r.Status='FAILED' AND j.Status=4 AND i.ErrorType=2 AND j.CompletedAt=r.CompletedAt AND j.ErrorCode=r.ErrorCode)
                                   OR (r.Status='SUCCEEDED' AND j.Status=5 AND i.ErrorType=3 AND j.CompletedAt<=r.CompletedAt)))
                    ) THROW 51032, 'Only terminal ANH-02 analysis may close its pending attempt once.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_ProcessingBlocks_Immutable]
                ON [dbo].[ProcessingBlocks]
                AFTER UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    THROW 51031, 'ProcessingBlocks are immutable.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER TR_ProjectLifecycleHistory_Immutable ON ProjectLifecycleHistory AFTER UPDATE,DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS(SELECT 1 FROM deleted) THROW 51500, 'Lifecycle source history is append-only.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER TR_ProjectLifecycleHistory_Production ON ProjectLifecycleHistory AFTER INSERT AS
                BEGIN SET NOCOUNT ON;
                  IF EXISTS(SELECT 1 FROM inserted i WHERE i.AuthoritySourceReference LIKE 'LD06_PRODUCTION_ACTION:%'
                    AND NOT EXISTS(SELECT 1 FROM LD06LifecycleActions a WHERE a.Id=i.Id AND a.ProjectId=i.ProjectId
                      AND a.Kind=4 AND i.Kind=2 AND a.ActorId=i.ActorId AND a.At=i.RecordedAtUtc
                      AND i.SourceDisposition='TARGET_CONFIRMED'
                      AND i.AuthoritySourceReference='LD06_PRODUCTION_ACTION:'+CONVERT(nvarchar(36),a.Id)))
                    THROW 51604, 'Production closure history requires its exact committed action.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER TR_ProjectLifecycleHistory_Scope ON ProjectLifecycleHistory AFTER INSERT AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS(SELECT 1 FROM inserted i WHERE
                        (i.ObligationId IS NOT NULL AND NOT EXISTS(SELECT 1 FROM RepairObligations o WHERE o.Id=i.ObligationId AND o.ProjectId=i.ProjectId))
                        OR (i.DefectId IS NOT NULL AND NOT EXISTS(SELECT 1 FROM Defects d WHERE d.Id=i.DefectId AND d.ProjectId=i.ProjectId))
                        OR (i.LinkedDefectId IS NOT NULL AND NOT EXISTS(SELECT 1 FROM Defects d WHERE d.Id=i.LinkedDefectId AND d.ProjectId=i.ProjectId AND d.Id<>i.DefectId))
                        OR (i.Kind=3 AND (i.ObligationId IS NULL OR i.ReceiverId IS NULL OR i.GrantId IS NULL OR i.GrantId<>i.Id))
                        OR (i.Kind=4 AND NOT EXISTS(SELECT 1 FROM ProjectLifecycleHistory g WHERE g.Id=i.GrantId AND g.Kind=3
                            AND g.ProjectId=i.ProjectId AND g.ObligationId=i.ObligationId AND g.ReceiverId=i.ReceiverId
                            AND g.RecordedAtUtc<=i.RecordedAtUtc))
                        OR (i.Kind NOT IN (3,4) AND (i.ObligationId IS NOT NULL OR i.GrantId IS NOT NULL OR i.ReceiverId IS NOT NULL))
                        OR (i.Kind IN (7,8) AND i.DefectId IS NULL)
                        OR (i.Kind NOT IN (7,8) AND i.DefectId IS NOT NULL)
                        OR (i.Kind=8 AND i.LinkedDefectId IS NULL)
                        OR (i.Kind<>8 AND i.LinkedDefectId IS NOT NULL)
                        OR (i.Kind=5 AND i.SourceDisposition='TARGET_CONFIRMED' AND NOT EXISTS(
                            SELECT 1 FROM ProjectLifecycleHistory c WHERE c.Id=i.OperationalClosureId AND c.ProjectId=i.ProjectId
                            AND c.Kind=2 AND c.SourceDisposition='TARGET_CONFIRMED' AND LEN(c.AuthoritySourceReference)>0
                            AND c.RecordedAtUtc<=i.RecordedAtUtc))
                        OR (i.Kind<>5 AND i.OperationalClosureId IS NOT NULL))
                        THROW 51501, 'Lifecycle source pins must retain their exact project and history relation.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_RefreshTokens_LifecycleExpiry] ON [RefreshTokens] AFTER INSERT, UPDATE AS
                BEGIN SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM inserted i JOIN Sessions s ON i.SessionId=s.Id WHERE i.ExpiresAt IS NULL AND s.Lifecycle<>1)
                        THROW 51006, 'Non-expiring credentials require a persistent session.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_RepairActualScopes_Scope] ON [RepairActualScopes] AFTER INSERT, UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM deleted) AND NOT EXISTS (SELECT 1 FROM inserted)
                        THROW 51201, 'Repair business records cannot be deleted.', 1;
                    IF EXISTS (SELECT 1 FROM deleted) THROW 51218, 'Repair actual scope is immutable.', 1;
                IF EXISTS (SELECT 1 FROM inserted s JOIN RepairObligations o ON o.Id=s.ObligationId
                    JOIN Defects d ON d.Id=o.DefectId LEFT JOIN RoadSectionVersions r ON r.Id=d.RoadSectionVersionId
                    WHERE r.Id IS NULL OR s.PhysicalRoadId<>r.RoadSectionId OR s.[From]<0 OR s.[From]>=s.[To] OR s.OffsetFrom>=s.OffsetTo OR LEN(LTRIM(RTRIM(s.LocationVersion)))=0)
                    THROW 51219, 'Repair scope must use the defect physical road and valid bounds.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_RepairAssessmentEvidence_Immutable] ON [RepairAssessmentEvidence] AFTER INSERT, UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM deleted)
                        THROW 51300, 'Repair producer history is append-only.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_RepairAssessmentMeasurements_Immutable] ON [RepairAssessmentMeasurements] AFTER INSERT, UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM deleted)
                        THROW 51300, 'Repair producer history is append-only.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_RepairAttemptEvidence_Immutable] ON [RepairAttemptEvidence] AFTER INSERT, UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM deleted)
                        THROW 51200, 'Repair history is append-only.', 1;
                    
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_RepairAttemptReviews_Immutable] ON [RepairAttemptReviews] AFTER INSERT, UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM deleted)
                        THROW 51300, 'Repair producer history is append-only.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_RepairAttempts_Immutable] ON [RepairAttempts] AFTER INSERT, UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM deleted)
                        THROW 51200, 'Repair history is append-only.', 1;
                    IF EXISTS (SELECT 1 FROM inserted a JOIN RepairItems i ON i.Id=a.ItemId
                    WHERE a.ProjectId<>i.ProjectId OR a.DefectId<>i.DefectId OR a.ObligationId<>i.ObligationId
                    OR i.CrewId IS NULL OR a.CrewId<>i.CrewId OR LEN(a.PayloadHash)<>64)
                    THROW 51204, 'Repair attempt source scope is invalid.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_RepairAttemptSubmissionLinks_Immutable] ON [RepairAttemptSubmissionLinks] AFTER INSERT, UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM deleted)
                        THROW 51300, 'Repair producer history is append-only.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_RepairCorrectionEvidence_Immutable] ON [RepairCorrectionEvidence] AFTER INSERT, UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM deleted)
                        THROW 51200, 'Repair history is append-only.', 1;
                    IF EXISTS (SELECT 1 FROM inserted e JOIN RepairDecisions d ON d.Id=e.DecisionId
                    WHERE LEN(LTRIM(RTRIM(ISNULL(d.Basis_Text,''))))=0
                        OR EXISTS (SELECT 1 FROM RepairItems i WHERE i.EffectiveDecisionId=d.Id)
                        OR EXISTS (SELECT 1 FROM RepairObligationResolutionEvents h WHERE h.DecisionId=d.Id)
                        OR EXISTS (SELECT 1 FROM RepairDecisions c WHERE c.SupersedesDecisionId=d.Id))
                    THROW 51208, 'Repair decision evidence must be staged before publication and cannot alter published history.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_RepairDangerAcknowledgements_Immutable] ON [RepairDangerAcknowledgements] AFTER INSERT, UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM deleted)
                        THROW 51300, 'Repair producer history is append-only.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_RepairDangerWarnings_Immutable] ON [RepairDangerWarnings] AFTER INSERT, UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM deleted)
                        THROW 51300, 'Repair producer history is append-only.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_RepairDecisions_Immutable] ON [RepairDecisions] AFTER INSERT, UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM deleted)
                        THROW 51200, 'Repair history is append-only.', 1;
                    IF EXISTS (SELECT 1 FROM inserted d JOIN RepairItems i ON i.Id=d.ItemId
                    WHERE d.ObligationId<>i.ObligationId OR d.DefectId<>i.DefectId OR d.Mode<>i.Mode
                    OR d.Result NOT IN (1,2,3) OR (d.Mode=1 AND d.Role<>1) OR (d.Mode=2 AND d.Role<>2)
                    OR d.Mode NOT IN (1,2) OR LEN(LTRIM(RTRIM(d.Reason)))=0
                    OR (d.SupersedesDecisionId IS NOT NULL AND (i.EffectiveDecisionId IS NULL OR i.EffectiveDecisionId<>d.SupersedesDecisionId OR LEN(LTRIM(RTRIM(ISNULL(d.Basis_Text,''))))=0)))
                    THROW 51202, 'Repair decision scope, role or correction basis is invalid.', 1;
                IF EXISTS (SELECT 1 FROM inserted d JOIN RepairDecisions p ON p.Id=d.SupersedesDecisionId
                    WHERE p.ItemId<>d.ItemId OR p.ObligationId<>d.ObligationId OR p.DefectId<>d.DefectId OR p.Mode<>d.Mode OR d.At<p.At)
                    THROW 51203, 'Repair correction must supersede its own chronological decision head.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_RepairEligibilityAssessments_Immutable] ON [RepairEligibilityAssessments] AFTER INSERT, UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM deleted)
                        THROW 51300, 'Repair producer history is append-only.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_RepairEligibilityHandoverSources_Immutable] ON [RepairEligibilityHandoverSources] AFTER INSERT, UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM deleted)
                        THROW 51300, 'Repair producer history is append-only.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_RepairEligibilityWarrantySources_Immutable] ON [RepairEligibilityWarrantySources] AFTER INSERT, UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM deleted)
                        THROW 51300, 'Repair producer history is append-only.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_RepairExecutionAuthorizations_Scope] ON [RepairExecutionAuthorizations] AFTER INSERT, UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM deleted) AND NOT EXISTS (SELECT 1 FROM inserted)
                        THROW 51201, 'Repair business records cannot be deleted.', 1;
                    IF EXISTS (SELECT 1 FROM inserted i JOIN Defects d ON d.Id=i.DefectId JOIN RepairPolicyRevisions p ON p.Id=i.PolicyRevisionId
                    WHERE d.ProjectId IS NULL OR i.ProjectId<>d.ProjectId OR i.ProjectId<>p.ProjectId OR i.Permission NOT IN (1,2))
                    THROW 51223, 'Repair execution authorization source scope is invalid.', 1;
                IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.Id=i.Id
                    WHERE i.ProjectId<>d.ProjectId OR i.DefectId<>d.DefectId OR i.TaskId<>d.TaskId OR i.AssignmentId<>d.AssignmentId OR i.CrewId<>d.CrewId
                    OR i.IssuedBy<>d.IssuedBy OR i.IssuedAt<>d.IssuedAt OR i.PolicyRevisionId<>d.PolicyRevisionId OR i.Permission<>d.Permission OR i.LocationVersion<>d.LocationVersion
                    OR (d.FirstStartOriginId IS NOT NULL AND EXISTS (SELECT i.FirstStartOriginId,i.FirstStartPayloadHash,i.FirstStartedAt,i.FirstServerReceivedAt EXCEPT SELECT d.FirstStartOriginId,d.FirstStartPayloadHash,d.FirstStartedAt,d.FirstServerReceivedAt))
                    OR (d.VerifiedStartedAt IS NOT NULL AND EXISTS (SELECT i.VerifiedStartedAt,i.ExpiresAt EXCEPT SELECT d.VerifiedStartedAt,d.ExpiresAt)))
                    THROW 51224, 'Repair authorization identity and first clock origins are immutable.', 1;
                IF EXISTS (SELECT 1 FROM inserted WHERE (VerifiedStartedAt IS NULL AND ExpiresAt IS NOT NULL)
                    OR (VerifiedStartedAt IS NOT NULL AND (ExpiresAt IS NULL OR ExpiresAt<>DATEADD(hour,24,VerifiedStartedAt))))
                    THROW 51225, 'Repair execution due must remain first verified start plus 24 hours.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_RepairExecutionFinishes_Immutable] ON [RepairExecutionFinishes] AFTER INSERT, UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM deleted)
                        THROW 51300, 'Repair producer history is append-only.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_RepairExecutionStarts_Immutable] ON [RepairExecutionStarts] AFTER INSERT, UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM deleted)
                        THROW 51300, 'Repair producer history is append-only.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_RepairFieldTaskBindings_Immutable] ON [RepairFieldTaskBindings] AFTER INSERT, UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM deleted)
                        THROW 51300, 'Repair producer history is append-only.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_RepairItemLifecycleEvents_Immutable] ON [RepairItemLifecycleEvents] AFTER INSERT, UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM deleted)
                        THROW 51300, 'Repair producer history is append-only.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_RepairItems_Scope] ON [RepairItems] AFTER INSERT, UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM deleted) AND NOT EXISTS (SELECT 1 FROM inserted)
                        THROW 51201, 'Repair business records cannot be deleted.', 1;
                    IF EXISTS (SELECT 1 FROM inserted i JOIN RepairObligations o ON o.Id=i.ObligationId
                    WHERE i.ProjectId<>o.ProjectId OR i.DefectId<>o.DefectId OR i.Mode NOT IN (1,2)
                    OR EXISTS (SELECT i.PackageId EXCEPT SELECT o.PackageId))
                    THROW 51215, 'Repair item obligation scope is invalid.', 1;
                IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.Id=i.Id
                    WHERE i.ProjectId<>d.ProjectId OR i.DefectId<>d.DefectId OR i.ObligationId<>d.ObligationId OR i.Mode<>d.Mode
                    OR EXISTS (SELECT i.PackageId,i.RepairPlan,i.ChecklistVersion,i.ProposalPlanHash EXCEPT SELECT d.PackageId,d.RepairPlan,d.ChecklistVersion,d.ProposalPlanHash))
                    THROW 51216, 'Repair item identity and proposed plan are immutable.', 1;
                IF EXISTS (SELECT 1 FROM inserted i LEFT JOIN RepairDecisions h ON h.Id=i.EffectiveDecisionId
                    WHERE (i.EffectiveDecisionId IS NULL AND i.State IN (7,9)) OR
                    (h.Id IS NOT NULL AND (h.ItemId<>i.Id OR h.ObligationId<>i.ObligationId OR h.DefectId<>i.DefectId OR h.Mode<>i.Mode
                        OR (h.Result=3 AND i.State<>7) OR (h.Result<>3 AND i.State<>9)
                        OR EXISTS (SELECT 1 FROM RepairDecisions c WHERE c.SupersedesDecisionId=h.Id))))
                    THROW 51217, 'Repair item state must reflect its terminal effective decision.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_RepairMeasurementAssessments_Immutable] ON [RepairMeasurementAssessments] AFTER INSERT, UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM deleted)
                        THROW 51300, 'Repair producer history is append-only.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_RepairNormalSuccessors_Immutable] ON [RepairNormalSuccessors] AFTER INSERT, UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM deleted)
                        THROW 51300, 'Repair producer history is append-only.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_RepairObligationResolutionEvents_Immutable] ON [RepairObligationResolutionEvents] AFTER INSERT, UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM deleted)
                        THROW 51200, 'Repair history is append-only.', 1;
                    IF EXISTS (SELECT 1 FROM inserted e JOIN RepairDecisions d ON d.Id=e.DecisionId
                    WHERE e.ObligationId<>d.ObligationId OR e.At<>d.At
                    OR (e.Accepted=1 AND d.Result<>3) OR (e.Accepted=0 AND d.Result=3)
                    OR EXISTS (SELECT e.SupersedesDecisionId EXCEPT SELECT d.SupersedesDecisionId))
                    THROW 51206, 'Repair resolution history must match its exact decision.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_RepairObligations_ProducerHeads] ON [RepairObligations] AFTER INSERT, UPDATE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM inserted o WHERE o.CurrentRepairItemId IS NOT NULL AND NOT EXISTS
                        (SELECT 1 FROM RepairItems i WHERE i.Id=o.CurrentRepairItemId AND i.ObligationId=o.Id
                            AND i.ProjectId=o.ProjectId AND i.DefectId=o.DefectId))
                        THROW 51310, 'Current repair item must belong to the actual obligation.', 1;
                    IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.Id=i.Id
                        WHERE d.OriginalCrewFirstStartId IS NOT NULL AND EXISTS
                            (SELECT i.OriginalCrewFirstStartId EXCEPT SELECT d.OriginalCrewFirstStartId))
                        THROW 51311, 'Original Crew first-start cannot be reset.', 1;
                    IF EXISTS (SELECT 1 FROM inserted o WHERE o.OriginalCrewFirstStartId IS NOT NULL AND NOT EXISTS
                        (SELECT 1 FROM FieldTaskStartOrigins s JOIN RepairFieldTaskBindings b ON b.TaskId=s.TaskId
                            WHERE s.Id=o.OriginalCrewFirstStartId AND b.ObligationId=o.Id AND b.ProjectId=o.ProjectId
                                AND b.DefectId=o.DefectId AND b.AssignmentId=s.AssignmentId AND b.CrewId=s.OriginalActorId))
                        THROW 51311, 'Original Crew first-start must retain the actual task and obligation scope.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_RepairObligations_Scope] ON [RepairObligations] AFTER INSERT, UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM deleted) AND NOT EXISTS (SELECT 1 FROM inserted)
                        THROW 51201, 'Repair business records cannot be deleted.', 1;
                    IF EXISTS (SELECT 1 FROM inserted i JOIN Defects d ON d.Id=i.DefectId WHERE d.ProjectId IS NULL OR i.ProjectId<>d.ProjectId OR i.Kind NOT IN (1,2))
                    THROW 51212, 'Repair obligation defect scope is invalid.', 1;
                IF EXISTS (SELECT 1 FROM inserted i JOIN RepairPackages p ON p.Id=i.PackageId WHERE p.ProjectId<>i.ProjectId OR p.DefectId<>i.DefectId)
                    THROW 51212, 'Repair obligation package scope is invalid.', 1;
                IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.Id=i.Id WHERE i.ProjectId<>d.ProjectId OR i.DefectId<>d.DefectId OR i.Kind<>d.Kind OR i.Mandatory<>d.Mandatory
                    OR EXISTS (SELECT i.PackageId EXCEPT SELECT d.PackageId))
                    THROW 51213, 'Repair obligation identity is immutable.', 1;
                IF EXISTS (SELECT 1 FROM inserted i LEFT JOIN RepairDecisions h ON h.Id=i.EffectiveResolutionHeadDecisionId
                    WHERE (i.EffectiveResolutionHeadDecisionId IS NULL AND i.EffectiveResolutionDecisionId IS NOT NULL)
                    OR (h.Id IS NOT NULL AND (h.ObligationId<>i.Id OR h.DefectId<>i.DefectId
                        OR (h.Result=3 AND (i.EffectiveResolutionDecisionId IS NULL OR i.EffectiveResolutionDecisionId<>h.Id))
                        OR (h.Result<>3 AND i.EffectiveResolutionDecisionId IS NOT NULL)
                        OR EXISTS (SELECT 1 FROM RepairDecisions c WHERE c.SupersedesDecisionId=h.Id))))
                    THROW 51214, 'Repair obligation effective resolution must follow its terminal decision.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_RepairPackages_Scope] ON [RepairPackages] AFTER INSERT, UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM deleted) AND NOT EXISTS (SELECT 1 FROM inserted)
                        THROW 51201, 'Repair business records cannot be deleted.', 1;
                    IF EXISTS (SELECT 1 FROM inserted i JOIN Defects d ON d.Id=i.DefectId WHERE d.ProjectId IS NULL OR i.ProjectId<>d.ProjectId)
                    THROW 51210, 'Repair package project must match its defect.', 1;
                IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.Id=i.Id
                    WHERE i.ProjectId<>d.ProjectId OR i.DefectId<>d.DefectId OR i.MutationRevision<=d.MutationRevision)
                    THROW 51211, 'Repair package identity is immutable and mutation revision must advance.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_RepairPolicyDraftChanges_Immutable] ON [RepairPolicyDraftChanges] AFTER INSERT, UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM deleted)
                        THROW 51200, 'Repair history is append-only.', 1;
                    
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_RepairPolicyDrafts_Scope] ON [RepairPolicyDrafts] AFTER INSERT, UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM deleted) AND NOT EXISTS (SELECT 1 FROM inserted)
                        THROW 51201, 'Repair business records cannot be deleted.', 1;
                    IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.Id=i.Id WHERE i.ProjectId<>d.ProjectId
                    OR (d.PublishedRevisionId IS NOT NULL AND EXISTS (SELECT i.CurrentChangeId,i.PublishedRevisionId EXCEPT SELECT d.CurrentChangeId,d.PublishedRevisionId)))
                    THROW 51220, 'Published repair draft and project are immutable.', 1;
                IF EXISTS (SELECT 1 FROM inserted i JOIN RepairPolicyDraftChanges c ON c.Id=i.CurrentChangeId WHERE c.DraftId IS NULL OR c.DraftId<>i.Id)
                    THROW 51221, 'Repair draft head must reference its own change.', 1;
                IF EXISTS (SELECT 1 FROM inserted i JOIN RepairPolicyRevisions r ON r.Id=i.PublishedRevisionId
                    LEFT JOIN RepairPolicyDraftChanges c ON c.Id=i.CurrentChangeId
                    WHERE i.ProjectId<>r.ProjectId OR c.Id IS NULL OR c.DefectTypeCode<>r.DefectTypeCode OR c.ChecklistVersion<>r.ChecklistVersion OR c.StopConditions<>r.StopConditions)
                    THROW 51222, 'Repair publication must pin its own project and draft source.', 1;
                IF EXISTS (SELECT 1 FROM inserted i JOIN RepairPolicyDraftChanges c ON c.Id=i.CurrentChangeId
                    WHERE i.PublishedRevisionId IS NOT NULL AND (ISJSON(c.Measurements)<>1 OR LEFT(LTRIM(c.Measurements),1)<>'['))
                    THROW 51222, 'Repair publication measurement source must be a valid array.', 1;
                IF EXISTS (SELECT 1 FROM inserted i JOIN RepairPolicyDraftChanges c ON c.Id=i.CurrentChangeId
                    WHERE i.PublishedRevisionId IS NOT NULL AND (
                        EXISTS (SELECT j.Code COLLATE Latin1_General_100_BIN2,j.Unit COLLATE Latin1_General_100_BIN2,j.Minimum,j.Maximum
                            FROM OPENJSON(c.Measurements) WITH (Code nvarchar(200) '$.Code',Unit nvarchar(80) '$.Unit',Minimum decimal(20,6) '$.Minimum',Maximum decimal(20,6) '$.Maximum') j
                            EXCEPT SELECT r.Code COLLATE Latin1_General_100_BIN2,r.Unit COLLATE Latin1_General_100_BIN2,r.Minimum,r.Maximum
                            FROM RepairPolicyMeasurementRules r WHERE r.PolicyRevisionId=i.PublishedRevisionId)
                        OR EXISTS (SELECT r.Code COLLATE Latin1_General_100_BIN2,r.Unit COLLATE Latin1_General_100_BIN2,r.Minimum,r.Maximum
                            FROM RepairPolicyMeasurementRules r WHERE r.PolicyRevisionId=i.PublishedRevisionId
                            EXCEPT SELECT j.Code COLLATE Latin1_General_100_BIN2,j.Unit COLLATE Latin1_General_100_BIN2,j.Minimum,j.Maximum
                            FROM OPENJSON(c.Measurements) WITH (Code nvarchar(200) '$.Code',Unit nvarchar(80) '$.Unit',Minimum decimal(20,6) '$.Minimum',Maximum decimal(20,6) '$.Maximum') j)))
                    THROW 51222, 'Repair publication measurement rules must equal its immutable draft head.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_RepairPolicyMeasurementRules_Immutable] ON [RepairPolicyMeasurementRules] AFTER INSERT, UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM deleted)
                        THROW 51200, 'Repair history is append-only.', 1;
                    IF EXISTS (SELECT 1 FROM inserted WHERE Minimum>Maximum OR LEN(LTRIM(RTRIM(Code)))=0 OR LEN(LTRIM(RTRIM(Unit)))=0)
                    THROW 51207, 'Repair policy measurement rule is invalid.', 1;
                IF EXISTS (SELECT 1 FROM inserted r WHERE EXISTS (SELECT 1 FROM RepairPolicyDrafts d WHERE d.PublishedRevisionId=r.PolicyRevisionId)
                    OR EXISTS (SELECT 1 FROM RepairExecutionAuthorizations a WHERE a.PolicyRevisionId=r.PolicyRevisionId))
                    THROW 51209, 'Published or used repair policy measurement rules cannot be extended.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_RepairPolicyRevisions_Immutable] ON [RepairPolicyRevisions] AFTER INSERT, UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM deleted)
                        THROW 51200, 'Repair history is append-only.', 1;
                    
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_RepairPolicyRevocations_Immutable] ON [RepairPolicyRevocations] AFTER INSERT, UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM deleted)
                        THROW 51200, 'Repair history is append-only.', 1;
                    
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_RepairReviewRequests_Immutable] ON [RepairReviewRequests] AFTER INSERT, UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM deleted)
                        THROW 51200, 'Repair history is append-only.', 1;
                    IF EXISTS (SELECT 1 FROM inserted r JOIN RepairItems i ON i.Id=r.ItemId JOIN RepairDecisions d ON d.Id=r.DecisionId
                    WHERE r.ProjectId<>i.ProjectId OR d.ItemId<>i.Id OR r.Role NOT IN (2,4) OR LEN(LTRIM(RTRIM(r.Reason)))=0)
                    THROW 51205, 'Repair review request source scope is invalid.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_RepairSafetyActionSources_Immutable] ON [RepairSafetyActionSources]
                AFTER INSERT, UPDATE, DELETE AS BEGIN
                 SET NOCOUNT ON;
                 IF EXISTS(SELECT 1 FROM deleted) THROW 51271, 'Safety action provenance is append only.', 1;
                 IF EXISTS(SELECT 1 FROM inserted s WHERE NOT EXISTS(
                   SELECT 1 FROM RepairTemporarySafetyMeasures m
                   JOIN RepairItems i ON i.Id=s.ItemId AND i.ProjectId=s.ProjectId
                   JOIN RepairFieldTaskBindings b ON b.Id=s.BindingId AND b.ItemId=i.Id AND b.ProjectId=s.ProjectId
                   JOIN RepairSafetyMonitoring r ON r.MeasureId=m.Id
                   WHERE m.Id=s.MeasureId AND m.ProjectId=s.ProjectId AND r.FormalObligationId=i.ObligationId
                   AND ((s.Kind='ASSIGNED' AND s.OriginId=m.Id AND EXISTS(SELECT 1 FROM Users u WHERE u.Id=s.ActorId AND u.RoleCode='PM'))
                    OR (s.Kind='INSTALLED' AND s.OriginId=m.InstallationEventId AND s.ActorId=m.InstalledBy AND s.At=m.InstalledAt)
                    OR (s.Kind='WARNING' AND EXISTS(SELECT 1 FROM RepairDangerWarnings w
                        WHERE w.Id=s.OriginId AND w.MonitoringId=r.Id AND w.ServerReceivedAt=s.At)))))
                   THROW 51272, 'Safety action must retain its actual scoped source.', 1;
                END;
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_RepairSafetyCheckEvidence_Immutable] ON [RepairSafetyCheckEvidence] AFTER INSERT, UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM deleted)
                        THROW 51300, 'Repair producer history is append-only.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_RepairSafetyChecks_Immutable] ON [RepairSafetyChecks] AFTER INSERT, UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM deleted)
                        THROW 51300, 'Repair producer history is append-only.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_RepairSafetyMonitoring_Scope] ON [RepairSafetyMonitoring] AFTER INSERT, UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM deleted) AND NOT EXISTS (SELECT 1 FROM inserted)
                        THROW 51301, 'Repair monitoring cannot be deleted.', 1;
                    IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.Id=i.Id
                        WHERE EXISTS (SELECT i.MeasureId,i.SafetyObligationId,i.FormalObligationId
                            EXCEPT SELECT d.MeasureId,d.SafetyObligationId,d.FormalObligationId))
                        THROW 51301, 'Repair monitoring source pins are immutable.', 1;
                    IF EXISTS (SELECT 1 FROM inserted i LEFT JOIN RepairTemporarySafetyMeasures m ON m.Id=i.MeasureId
                        LEFT JOIN RepairObligations s ON s.Id=i.SafetyObligationId
                        LEFT JOIN RepairObligations f ON f.Id=i.FormalObligationId
                        WHERE i.Id<>i.MeasureId OR m.Id IS NULL OR s.Id IS NULL OR f.Id IS NULL
                            OR s.Kind<>2 OR f.Kind<>1 OR s.ProjectId<>m.ProjectId OR f.ProjectId<>m.ProjectId
                            OR s.DefectId<>m.DefectId OR f.DefectId<>m.DefectId OR m.FormalRepairObligationId<>f.Id
                            OR (i.CurrentCheckId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM RepairSafetyChecks c
                                WHERE c.Id=i.CurrentCheckId AND c.MeasureId=i.MeasureId)))
                        THROW 51301, 'Repair monitoring must retain the actual obligation and check scope.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_RepairSafetyResponsibilityTransfers_Immutable] ON [RepairSafetyResponsibilityTransfers] AFTER INSERT, UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM deleted)
                        THROW 51200, 'Repair history is append-only.', 1;
                    
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_RepairTemporarySafetyMeasures_Scope] ON [RepairTemporarySafetyMeasures] AFTER INSERT, UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM deleted) AND NOT EXISTS (SELECT 1 FROM inserted)
                        THROW 51201, 'Repair business records cannot be deleted.', 1;
                    IF EXISTS (SELECT 1 FROM inserted i JOIN RepairObligations o ON o.Id=i.FormalRepairObligationId
                    WHERE i.ProjectId<>o.ProjectId OR i.DefectId<>o.DefectId OR o.Kind<>1)
                    THROW 51226, 'Temporary safety must retain its own formal repair obligation.', 1;
                IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.Id=i.Id WHERE i.ProjectId<>d.ProjectId OR i.DefectId<>d.DefectId OR i.FormalRepairObligationId<>d.FormalRepairObligationId
                    OR (d.InstalledAt IS NOT NULL AND EXISTS (SELECT i.InstallationEventId,i.InstalledBy,i.InstalledAt,i.FirstCheckDueAt EXCEPT SELECT d.InstallationEventId,d.InstalledBy,d.InstalledAt,d.FirstCheckDueAt)))
                    THROW 51227, 'Temporary safety scope and original first-check clock are immutable.', 1;
                IF EXISTS (SELECT 1 FROM inserted WHERE InstalledAt IS NOT NULL AND (FirstCheckDueAt IS NULL OR FirstCheckDueAt<InstalledAt OR FirstCheckDueAt>DATEADD(hour,24,InstalledAt)))
                    THROW 51228, 'Temporary safety first check must be within 24 hours of installation.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_RepairWorkHandovers_Immutable] ON [RepairWorkHandovers] AFTER INSERT, UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM deleted)
                        THROW 51200, 'Repair history is append-only.', 1;
                    
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_ReportOriginalEvidence_Identity] ON [ReportOriginalEvidence] AFTER INSERT AS BEGIN SET NOCOUNT ON; IF EXISTS (SELECT 1 FROM inserted i JOIN [ReportSupplementEvidence] e WITH (UPDLOCK,HOLDLOCK) ON e.Id=i.Id) THROW 51121, 'Evidence identity already exists in another evidence namespace.', 1; END;
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_ReportOriginalEvidence_Immutable] ON [ReportOriginalEvidence] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51100, 'Immutable integration history cannot be rewritten.', 1; END;
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_Reports_OriginalImmutable] ON [Reports] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; IF EXISTS (SELECT Id,ReporterUserId,Description COLLATE Latin1_General_100_BIN2,DATALENGTH(Description),ReceivedAt FROM deleted EXCEPT SELECT Id,ReporterUserId,Description COLLATE Latin1_General_100_BIN2,DATALENGTH(Description),ReceivedAt FROM inserted) OR EXISTS (SELECT Id,ReporterUserId,Description COLLATE Latin1_General_100_BIN2,DATALENGTH(Description),ReceivedAt FROM inserted EXCEPT SELECT Id,ReporterUserId,Description COLLATE Latin1_General_100_BIN2,DATALENGTH(Description),ReceivedAt FROM deleted) THROW 51120, 'Original report is immutable.', 1; END;
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_ReportSupplementEvidence_Identity] ON [ReportSupplementEvidence] AFTER INSERT AS BEGIN SET NOCOUNT ON; IF EXISTS (SELECT 1 FROM inserted i JOIN [ReportOriginalEvidence] e WITH (UPDLOCK,HOLDLOCK) ON e.Id=i.Id) THROW 51121, 'Evidence identity already exists in another evidence namespace.', 1; END;
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_ReportSupplementEvidence_Immutable] ON [ReportSupplementEvidence] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51101, 'Immutable integration history cannot be rewritten.', 1; END;
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_ReportSupplements_Immutable] ON [ReportSupplements] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51102, 'Immutable integration history cannot be rewritten.', 1; END;
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_RetentionBasisRevisions_Immutable] ON [dbo].[RetentionBasisRevisions] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51220, 'Retention basis revisions are immutable.', 1; END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_RetentionEvaluationItems_Immutable] ON [dbo].[RetentionEvaluationItems] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51220, 'Retention evaluation snapshots are immutable.', 1; END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_RetentionHoldHistories_Immutable] ON [dbo].[RetentionHoldHistories] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51220, 'Retention hold history is immutable.', 1; END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER TR_RoadCoverageMappings_Immutable ON RoadCoverageMappings AFTER UPDATE,DELETE AS
                BEGIN SET NOCOUNT ON;
                  IF EXISTS(SELECT 1 FROM deleted) THROW 51700, 'Road coverage confirmations are append-only.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER TR_RoadCoverageMappings_Scope ON RoadCoverageMappings AFTER INSERT AS
                BEGIN SET NOCOUNT ON;
                  IF EXISTS(SELECT 1 FROM inserted i WHERE LEN(i.SourceVersion)<>64 OR LEN(i.HandoverVersion)<>64
                    OR NOT EXISTS(SELECT 1 FROM RepairObligations o JOIN RepairActualScopes s ON s.ObligationId=o.Id
                      LEFT JOIN ObligationResponsibilities r ON r.ObligationId=o.Id
                      WHERE o.Id=i.ScopeObligationId AND COALESCE(r.CurrentProjectId,o.ProjectId)=i.ProjectId
                        AND s.PhysicalRoadId=i.RoadSectionId AND s.LocationVersion=i.LocationVersion
                        AND s.[From]=i.[From] AND s.[To]=i.[To] AND s.OffsetFrom=i.OffsetFrom AND s.OffsetTo=i.OffsetTo)
                    OR NOT EXISTS(SELECT 1 FROM HandoverDocuments h WHERE h.Id=i.HandoverDocumentId
                      AND h.ProjectId=i.ProjectId AND h.FileId=i.HandoverFileId AND h.AcceptedByUserId IS NOT NULL)
                    OR (i.SourceKind='WARRANTY' AND NOT EXISTS(SELECT 1 FROM Warranties w WHERE w.Id=i.SourceId
                      AND w.ProjectId=i.ProjectId AND w.SourceDocumentId=i.CoverageFileId
                      AND (w.RoadSectionId IS NULL OR w.RoadSectionId=i.RoadSectionId)
                      AND (w.HandoverDocumentId IS NULL OR w.HandoverDocumentId=i.HandoverDocumentId)))
                    OR (i.SourceKind='MAINTENANCE_BASIS' AND i.SourceId<>i.CoverageFileId)
                    OR NOT EXISTS(SELECT 1 FROM FileScopes f WHERE f.FileId=i.HandoverFileId AND f.ProjectId=i.ProjectId)
                    OR NOT EXISTS(SELECT 1 FROM FileScopes f WHERE f.FileId=i.CoverageFileId AND f.ProjectId=i.ProjectId)
                    OR (i.Provenance='REAL_SOURCE' AND EXISTS(SELECT 1 FROM Files f JOIN FileScopes s ON s.FileId=f.Id
                      WHERE f.Id IN (i.HandoverFileId,i.CoverageFileId) AND s.ProjectId=i.ProjectId
                        AND (s.Purpose LIKE 'TEST_ONLY%' OR f.StorageUri LIKE 'TEST_ONLY/%')))
                    OR (i.SupersedesId IS NOT NULL AND NOT EXISTS(SELECT 1 FROM RoadCoverageMappings p
                      WHERE p.Id=i.SupersedesId AND p.ProjectId=i.ProjectId AND p.ScopeObligationId=i.ScopeObligationId
                        AND p.Provenance=i.Provenance AND p.ConfirmedAtUtc<=i.ConfirmedAtUtc))
                    OR (i.Provenance='REAL_SOURCE' AND EXISTS(SELECT 1 FROM RoadCoverageMappings p
                      WHERE p.Id<>i.Id AND p.ProjectId=i.ProjectId AND p.Provenance='TEST_ONLY'
                        AND ((p.SourceKind=i.SourceKind AND p.SourceId=i.SourceId) OR p.HandoverDocumentId=i.HandoverDocumentId))))
                    THROW 51701, 'Coverage requires its exact scoped sources and immutable provenance.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_RoadGeometryMetadata_Immutable] ON [RoadGeometryMetadata] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51024, 'ANH-01 evidence and geometry metadata are immutable.', 1; END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_RoadSectionVersions_Immutable] ON [dbo].[RoadSectionVersions] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; IF NOT EXISTS(SELECT 1 FROM inserted) THROW 50000, 'RoadSectionVersion records cannot be deleted.', 1; IF UPDATE(RoadSectionId) OR UPDATE(VersionNo) OR UPDATE(Geometry) OR UPDATE(EffectiveFrom) OR UPDATE(ChangeReason) OR UPDATE(CrsProfileRevisionId) THROW 50001, 'RoadSectionVersion history is immutable; only IsCurrent may change.', 1; END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_RoadSectionVersions_ProfileScope] ON [dbo].[RoadSectionVersions] AFTER INSERT AS BEGIN SET NOCOUNT ON; IF EXISTS (SELECT 1 FROM inserted i JOIN RoadSections r ON r.Id=i.RoadSectionId JOIN CrsProfileRevisions p ON p.Id=i.CrsProfileRevisionId WHERE p.ProjectId<>r.ProjectId OR p.SourceSrid<>i.Geometry.STSrid) THROW 51231, 'Native profile scope or SRID mismatch.', 1; END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_Sessions_ImmutableLifecycle] ON [Sessions] AFTER UPDATE AS
                BEGIN SET NOCOUNT ON;
                    IF EXISTS (SELECT Id,Lifecycle,IssuedRole FROM deleted EXCEPT SELECT Id,Lifecycle,IssuedRole FROM inserted)
                        THROW 51005, 'Session lifecycle and issued role are immutable; legacy sessions cannot be promoted.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_SourceDecisions_Immutable] ON [SourceDecisions] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51108, 'Immutable integration history cannot be rewritten.', 1; END;
                """);
            migrationBuilder.Sql("""
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
                """);
            migrationBuilder.Sql("""
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
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_SurveyPlanPostponements_AppendOnly]
                ON [dbo].[SurveyPlanPostponements]
                AFTER UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    THROW 51000, 'SurveyPlanPostponements are append-only.', 1;
                END
                """);
            migrationBuilder.Sql("""
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
                """);
            migrationBuilder.Sql("""
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
                """);
            migrationBuilder.Sql("""
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
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_TrainingLabelReviews_AppendOnly] ON [TrainingLabelReviews] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51001, 'Training label reviews are append-only.', 1; END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_TrainingLabelRevisions_AppendOnly] ON [TrainingLabelRevisions] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51001, 'Training label revisions are append-only.', 1; END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_TrainingLabels_Identity] ON [TrainingLabels] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; IF EXISTS (SELECT 1 FROM deleted d LEFT JOIN inserted i ON i.Id=d.Id WHERE i.Id IS NULL OR i.ProjectId<>d.ProjectId OR i.SourceKind<>d.SourceKind OR i.SourceId<>d.SourceId OR ISNULL(i.ReportSourceId,'00000000-0000-0000-0000-000000000000')<>ISNULL(d.ReportSourceId,'00000000-0000-0000-0000-000000000000') OR ISNULL(i.AIDetectionSourceId,'00000000-0000-0000-0000-000000000000')<>ISNULL(d.AIDetectionSourceId,'00000000-0000-0000-0000-000000000000') OR i.CurrentRevision<d.CurrentRevision OR i.CurrentRevision>d.CurrentRevision+1) THROW 51001, 'Training label identity/current head is immutable.', 1; END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_WeeklyReviewDigestDuties_Immutable] ON [WeeklyReviewDigestDuties] AFTER UPDATE,DELETE AS BEGIN SET NOCOUNT ON; THROW 51109, 'Weekly recovery facts are immutable.', 1; END;
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_WeeklyReviewDigestDuties_Source] ON [WeeklyReviewDigestDuties] AFTER INSERT AS
                BEGIN SET NOCOUNT ON;
                IF EXISTS(SELECT 1 FROM inserted d WHERE NOT EXISTS(SELECT 1 FROM [WeeklyReviewDigests] g
                  JOIN [DeadlineClocks] c ON c.Id=d.ClockId WHERE g.Id=d.DigestId AND c.ProjectId=g.ProjectId
                    AND c.TargetId=d.TargetId AND c.OriginEventId=d.OriginEventId AND c.OriginAt=d.OriginAtUtc AND c.CurrentDueAt=d.DueAtRecoveryUtc
                    AND c.OriginAt<=g.RecoveredAtUtc AND (c.CompletedAt IS NULL OR c.CompletedAt>g.RecoveredAtUtc)))
                  THROW 51111, 'Weekly duty must bind actual pending-at-recovery source facts.', 1;
                END;
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_WeeklyReviewDigests_Immutable] ON [WeeklyReviewDigests] AFTER UPDATE,DELETE AS BEGIN SET NOCOUNT ON; THROW 51109, 'Weekly recovery facts are immutable.', 1; END;
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_WeeklyReviewDigests_Source] ON [WeeklyReviewDigests] AFTER INSERT AS
                BEGIN SET NOCOUNT ON;
                IF EXISTS(SELECT 1 FROM inserted d WHERE NOT EXISTS(SELECT 1 FROM [WeeklyReviewRecoveryPeriods] p
                  WHERE p.Id=d.RecoveryPeriodId AND p.ProjectId=d.ProjectId AND p.ScheduledAtUtc=d.ScheduledAtUtc
                    AND p.RecoveredAtUtc=d.RecoveredAtUtc AND p.IsLatestAtRecovery=1))
                  THROW 51110, 'Weekly digest must bind the exact latest recovery period.', 1;
                END;
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_WeeklyReviewRecoveryPeriods_Immutable] ON [WeeklyReviewRecoveryPeriods] AFTER UPDATE,DELETE AS BEGIN SET NOCOUNT ON; THROW 51109, 'Weekly recovery facts are immutable.', 1; END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey("FK_CasePublicationEvidence_OriginalEvidence", "CasePublicationEvidence");
            migrationBuilder.DropForeignKey("FK_CasePublicationEvidence_SupplementEvidence", "CasePublicationEvidence");
            migrationBuilder.DropForeignKey("FK_CaseConclusionEvidence_OriginalEvidence", "CaseConclusionEvidence");
            migrationBuilder.DropForeignKey("FK_CaseConclusionEvidence_SupplementEvidence", "CaseConclusionEvidence");
            migrationBuilder.DropForeignKey("FK_SourceDecisions_CorrectionIdentity", "SourceDecisions");

            migrationBuilder.DropForeignKey(
                name: "FK_AIModelVersions_Users_ReleasedByUserId",
                table: "AIModelVersions");

            migrationBuilder.DropForeignKey(
                name: "FK_Anh02AiMockRuns_Users_CreatedBy",
                table: "Anh02AiMockRuns");

            migrationBuilder.DropForeignKey(
                name: "FK_Anh02ExportJobs_Users_RequestedBy",
                table: "Anh02ExportJobs");

            migrationBuilder.DropForeignKey(
                name: "FK_CrsProfileRevisions_Users_CreatedBy",
                table: "CrsProfileRevisions");

            migrationBuilder.DropForeignKey(
                name: "FK_DeadlineClocks_Users_AcknowledgedByUserId",
                table: "DeadlineClocks");

            migrationBuilder.DropForeignKey(
                name: "FK_DeadlineClocks_Users_AppointedActorId",
                table: "DeadlineClocks");

            migrationBuilder.DropForeignKey(
                name: "FK_FieldInspectionAssignments_Users_AssignedByUserId",
                table: "FieldInspectionAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_FieldInspectionAssignments_Users_AssignedToUserId",
                table: "FieldInspectionAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_FieldInspectionOperationOrigins_Users_OriginalActorId",
                table: "FieldInspectionOperationOrigins");

            migrationBuilder.DropForeignKey(
                name: "FK_FieldInspectionSessions_Users_InspectorUserId",
                table: "FieldInspectionSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_FieldInspectionSubmissions_Users_OriginalActorId",
                table: "FieldInspectionSubmissions");

            migrationBuilder.DropForeignKey(
                name: "FK_FieldInspectionTasks_Users_AssignedByUserId",
                table: "FieldInspectionTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_FieldInspectionTasks_Users_ReviewedByUserId",
                table: "FieldInspectionTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_FieldTaskStartOrigins_Users_OriginalActorId",
                table: "FieldTaskStartOrigins");

            migrationBuilder.DropForeignKey(
                name: "FK_Files_Users_UploadedByUserId",
                table: "Files");

            migrationBuilder.DropForeignKey(
                name: "FK_GeometryMapPublications_Users_PublishedBy",
                table: "GeometryMapPublications");

            migrationBuilder.DropForeignKey(
                name: "FK_PavementLayoutRevisions_Users_CreatedBy",
                table: "PavementLayoutRevisions");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairAttemptReviews_Users_ActorId",
                table: "RepairAttemptReviews");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairAttempts_Users_CrewId",
                table: "RepairAttempts");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairDecisions_Users_ActorId",
                table: "RepairDecisions");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairExecutionAuthorizations_Users_CrewId",
                table: "RepairExecutionAuthorizations");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairExecutionAuthorizations_Users_IssuedBy",
                table: "RepairExecutionAuthorizations");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairExecutionFinishes_Users_OriginalActorId",
                table: "RepairExecutionFinishes");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairExecutionStarts_Users_OriginalActorId",
                table: "RepairExecutionStarts");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairFieldTaskBindings_Users_AssignedBy",
                table: "RepairFieldTaskBindings");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairFieldTaskBindings_Users_CrewId",
                table: "RepairFieldTaskBindings");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairItems_Users_CrewId",
                table: "RepairItems");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairMeasurementAssessments_Users_OriginalActorId",
                table: "RepairMeasurementAssessments");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairPolicyDraftChanges_Users_ActorId",
                table: "RepairPolicyDraftChanges");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairPolicyRevisions_Users_PublishedBy",
                table: "RepairPolicyRevisions");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairSafetyChecks_Users_ActorId",
                table: "RepairSafetyChecks");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairSafetyResponsibilityTransfers_Users_ChangedBy",
                table: "RepairSafetyResponsibilityTransfers");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairSafetyResponsibilityTransfers_Users_NextActorId",
                table: "RepairSafetyResponsibilityTransfers");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairSafetyResponsibilityTransfers_Users_PreviousActorId",
                table: "RepairSafetyResponsibilityTransfers");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairTemporarySafetyMeasures_Users_InstalledBy",
                table: "RepairTemporarySafetyMeasures");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairTemporarySafetyMeasures_Users_ResponsibleActorId",
                table: "RepairTemporarySafetyMeasures");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplementarySurveyRequests_Users_ApprovedByUserId",
                table: "SupplementarySurveyRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplementarySurveyRequests_Users_RequestedByUserId",
                table: "SupplementarySurveyRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_SurveyRequests_Users_RequestedByUserId",
                table: "SurveyRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_Surveys_Users_BaselineConfirmedByUserId",
                table: "Surveys");

            migrationBuilder.DropForeignKey(
                name: "FK_AIDetections_AIModelVersions_ModelVersionId",
                table: "AIDetections");

            migrationBuilder.DropForeignKey(
                name: "FK_Anh02AiMockRuns_AIModelVersions_ModelVersionId",
                table: "Anh02AiMockRuns");

            migrationBuilder.DropForeignKey(
                name: "FK_ProcessingJobs_AIModelVersions_ModelVersionId",
                table: "ProcessingJobs");

            migrationBuilder.DropForeignKey(
                name: "FK_AIDetections_DefectTypes_DefectTypeCode",
                table: "AIDetections");

            migrationBuilder.DropForeignKey(
                name: "FK_Defects_DefectTypes_DefectTypeCode",
                table: "Defects");

            migrationBuilder.DropForeignKey(
                name: "FK_AIDetections_ProcessingJobs_ProcessingJobId",
                table: "AIDetections");

            migrationBuilder.DropForeignKey(
                name: "FK_Anh02AiMockRuns_ProcessingJobs_ProcessingJobId",
                table: "Anh02AiMockRuns");

            migrationBuilder.DropForeignKey(
                name: "FK_Anh02AiResultProvenance_ProcessingJobs_ProcessingJobId",
                table: "Anh02AiResultProvenance");

            migrationBuilder.DropForeignKey(
                name: "FK_ProcessingAttempts_ProcessingJobs_ProcessingJobId",
                table: "ProcessingAttempts");

            migrationBuilder.DropForeignKey(
                name: "FK_AIDetections_RoadSectionVersions_RoadSectionVersionId",
                table: "AIDetections");

            migrationBuilder.DropForeignKey(
                name: "FK_Defects_RoadSectionVersions_RoadSectionVersionId",
                table: "Defects");

            migrationBuilder.DropForeignKey(
                name: "FK_FieldInspectionSessions_RoadSectionVersions_RoadSectionVersionId",
                table: "FieldInspectionSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_FieldInspectionTasks_RoadSectionVersions_RoadSectionVersionId",
                table: "FieldInspectionTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_FieldTaskStartOrigins_RoadSectionVersions_RouteVersionId",
                table: "FieldTaskStartOrigins");

            migrationBuilder.DropForeignKey(
                name: "FK_GeometryMapPublications_RoadSectionVersions_RouteVersionId",
                table: "GeometryMapPublications");

            migrationBuilder.DropForeignKey(
                name: "FK_PavementLayoutRevisions_RoadSectionVersions_RouteVersionId",
                table: "PavementLayoutRevisions");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairFieldTaskBindings_RoadSectionVersions_RouteVersionId",
                table: "RepairFieldTaskBindings");

            migrationBuilder.DropForeignKey(
                name: "FK_RoadSegmentSets_RoadSectionVersions_RoadSectionVersionId",
                table: "RoadSegmentSets");

            migrationBuilder.DropForeignKey(
                name: "FK_SurveyPlans_RoadSectionVersions_RoadSectionVersionId",
                table: "SurveyPlans");

            migrationBuilder.DropForeignKey(
                name: "FK_SurveyRequests_RoadSectionVersions_RoadSectionVersionId",
                table: "SurveyRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_Surveys_RoadSectionVersions_RoadSectionVersionId",
                table: "Surveys");

            migrationBuilder.DropForeignKey(
                name: "FK_Defects_AIDetections_SourceAIDetectionId",
                table: "Defects");

            migrationBuilder.DropForeignKey(
                name: "FK_Anh02AiResultProvenance_Anh02AiMockRuns_RunId",
                table: "Anh02AiResultProvenance");

            migrationBuilder.DropForeignKey(
                name: "FK_Anh02GeneratedArtifacts_Files_FileId",
                table: "Anh02GeneratedArtifacts");

            migrationBuilder.DropForeignKey(
                name: "FK_FieldInspectionSessions_Files_EvidenceFileId",
                table: "FieldInspectionSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_Anh02ExportJobs_Projects_ProjectId",
                table: "Anh02ExportJobs");

            migrationBuilder.DropForeignKey(
                name: "FK_Anh02ExportSnapshots_Projects_ProjectId",
                table: "Anh02ExportSnapshots");

            migrationBuilder.DropForeignKey(
                name: "FK_CrsProfileRevisions_Projects_ProjectId",
                table: "CrsProfileRevisions");

            migrationBuilder.DropForeignKey(
                name: "FK_DeadlineClocks_Projects_ProjectId",
                table: "DeadlineClocks");

            migrationBuilder.DropForeignKey(
                name: "FK_Defects_Projects_ProjectId",
                table: "Defects");

            migrationBuilder.DropForeignKey(
                name: "FK_FieldInspectionOperationOrigins_Projects_ProjectId",
                table: "FieldInspectionOperationOrigins");

            migrationBuilder.DropForeignKey(
                name: "FK_FieldInspectionSessions_Projects_ProjectId",
                table: "FieldInspectionSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_FieldInspectionSubmissions_Projects_ProjectId",
                table: "FieldInspectionSubmissions");

            migrationBuilder.DropForeignKey(
                name: "FK_FieldInspectionTasks_Projects_ProjectId",
                table: "FieldInspectionTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_FieldTaskStartOrigins_Projects_ProjectId",
                table: "FieldTaskStartOrigins");

            migrationBuilder.DropForeignKey(
                name: "FK_GeometryMapPublications_Projects_ProjectId",
                table: "GeometryMapPublications");

            migrationBuilder.DropForeignKey(
                name: "FK_PavementLayoutRevisions_Projects_ProjectId",
                table: "PavementLayoutRevisions");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairAttemptReviews_Projects_ProjectId",
                table: "RepairAttemptReviews");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairAttempts_Projects_ProjectId",
                table: "RepairAttempts");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairAttemptSubmissionLinks_Projects_ProjectId",
                table: "RepairAttemptSubmissionLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairEligibilityAssessments_Projects_ProjectId",
                table: "RepairEligibilityAssessments");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairExecutionAuthorizations_Projects_ProjectId",
                table: "RepairExecutionAuthorizations");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairExecutionFinishes_Projects_ProjectId",
                table: "RepairExecutionFinishes");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairExecutionStarts_Projects_ProjectId",
                table: "RepairExecutionStarts");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairFieldTaskBindings_Projects_ProjectId",
                table: "RepairFieldTaskBindings");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairItems_Projects_ProjectId",
                table: "RepairItems");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairMeasurementAssessments_Projects_ProjectId",
                table: "RepairMeasurementAssessments");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairObligations_Projects_ProjectId",
                table: "RepairObligations");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairPackages_Projects_ProjectId",
                table: "RepairPackages");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairPolicyDrafts_Projects_ProjectId",
                table: "RepairPolicyDrafts");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairPolicyRevisions_Projects_ProjectId",
                table: "RepairPolicyRevisions");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairTemporarySafetyMeasures_Projects_ProjectId",
                table: "RepairTemporarySafetyMeasures");

            migrationBuilder.DropForeignKey(
                name: "FK_RoadSections_Projects_ProjectId",
                table: "RoadSections");

            migrationBuilder.DropForeignKey(
                name: "FK_SurveyPlans_Projects_ProjectId",
                table: "SurveyPlans");

            migrationBuilder.DropForeignKey(
                name: "FK_SurveyRequests_Projects_ProjectId",
                table: "SurveyRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_Surveys_Projects_ProjectId",
                table: "Surveys");

            migrationBuilder.DropForeignKey(
                name: "FK_FieldInspectionTasks_RoadSegmentSets_SegmentSetId",
                table: "FieldInspectionTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_FieldTaskStartOrigins_RoadSegmentSets_SegmentSetId",
                table: "FieldTaskStartOrigins");

            migrationBuilder.DropForeignKey(
                name: "FK_GeometryMapPublications_RoadSegmentSets_SegmentSetId",
                table: "GeometryMapPublications");

            migrationBuilder.DropForeignKey(
                name: "FK_PavementLayoutRevisions_RoadSegmentSets_SegmentSetId",
                table: "PavementLayoutRevisions");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairFieldTaskBindings_RoadSegmentSets_SegmentSetId",
                table: "RepairFieldTaskBindings");

            migrationBuilder.DropForeignKey(
                name: "FK_Anh02ExportJobs_Anh02ExportSnapshots_SnapshotId",
                table: "Anh02ExportJobs");

            migrationBuilder.DropForeignKey(
                name: "FK_Anh02GeneratedArtifacts_Anh02ExportSnapshots_SnapshotId",
                table: "Anh02GeneratedArtifacts");

            migrationBuilder.DropForeignKey(
                name: "FK_Anh02ExportJobs_Anh02GeneratedArtifacts_ArtifactId",
                table: "Anh02ExportJobs");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairAttemptSubmissionLinks_DeadlineClocks_ReviewClockId",
                table: "RepairAttemptSubmissionLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_FieldInspectionTasks_Defects_DefectId",
                table: "FieldInspectionTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairAttempts_Defects_DefectId",
                table: "RepairAttempts");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairDecisions_Defects_DefectId",
                table: "RepairDecisions");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairExecutionAuthorizations_Defects_DefectId",
                table: "RepairExecutionAuthorizations");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairFieldTaskBindings_Defects_DefectId",
                table: "RepairFieldTaskBindings");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairItems_Defects_DefectId",
                table: "RepairItems");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairObligations_Defects_DefectId",
                table: "RepairObligations");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairPackages_Defects_DefectId",
                table: "RepairPackages");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairTemporarySafetyMeasures_Defects_DefectId",
                table: "RepairTemporarySafetyMeasures");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairAttempts_RepairObligations_ObligationId",
                table: "RepairAttempts");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairDecisions_RepairObligations_ObligationId",
                table: "RepairDecisions");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairFieldTaskBindings_RepairObligations_ObligationId",
                table: "RepairFieldTaskBindings");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairItems_RepairObligations_ObligationId",
                table: "RepairItems");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairSafetyMonitoring_RepairObligations_FormalObligationId",
                table: "RepairSafetyMonitoring");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairSafetyMonitoring_RepairObligations_SafetyObligationId",
                table: "RepairSafetyMonitoring");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairTemporarySafetyMeasures_RepairObligations_FormalRepairObligationId",
                table: "RepairTemporarySafetyMeasures");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairEligibilityAssessments_RoadSections_RoadSectionId",
                table: "RepairEligibilityAssessments");

            migrationBuilder.DropForeignKey(
                name: "FK_SurveyPlans_RoadSections_RoadSectionId",
                table: "SurveyPlans");

            migrationBuilder.DropForeignKey(
                name: "FK_SurveyRequests_RoadSections_RoadSectionId",
                table: "SurveyRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_FieldInspectionAssignments_FieldInspectionTasks_FieldInspectionTaskId",
                table: "FieldInspectionAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_FieldInspectionOperationOrigins_FieldInspectionTasks_TaskId",
                table: "FieldInspectionOperationOrigins");

            migrationBuilder.DropForeignKey(
                name: "FK_FieldInspectionSessions_FieldInspectionTasks_FieldInspectionTaskId",
                table: "FieldInspectionSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_FieldInspectionSubmissions_FieldInspectionTasks_TaskId",
                table: "FieldInspectionSubmissions");

            migrationBuilder.DropForeignKey(
                name: "FK_FieldTaskStartOrigins_FieldInspectionTasks_TaskId",
                table: "FieldTaskStartOrigins");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairAttempts_FieldInspectionTasks_TaskId",
                table: "RepairAttempts");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairExecutionAuthorizations_FieldInspectionTasks_TaskId",
                table: "RepairExecutionAuthorizations");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairFieldTaskBindings_FieldInspectionTasks_TaskId",
                table: "RepairFieldTaskBindings");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairMeasurementAssessments_FieldInspectionTasks_TaskId",
                table: "RepairMeasurementAssessments");

            migrationBuilder.DropForeignKey(
                name: "FK_FieldInspectionSubmissions_FieldInspectionAssignments_AssignmentId",
                table: "FieldInspectionSubmissions");

            migrationBuilder.DropForeignKey(
                name: "FK_FieldTaskStartOrigins_FieldInspectionAssignments_AssignmentId",
                table: "FieldTaskStartOrigins");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairAttempts_FieldInspectionAssignments_AssignmentId",
                table: "RepairAttempts");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairExecutionAuthorizations_FieldInspectionAssignments_AssignmentId",
                table: "RepairExecutionAuthorizations");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairFieldTaskBindings_FieldInspectionAssignments_AssignmentId",
                table: "RepairFieldTaskBindings");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairMeasurementAssessments_FieldInspectionAssignments_AssignmentId",
                table: "RepairMeasurementAssessments");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairAttemptReviews_FieldInspectionSubmissions_SubmissionId",
                table: "RepairAttemptReviews");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairAttemptSubmissionLinks_FieldInspectionSubmissions_FormalRootSubmissionId",
                table: "RepairAttemptSubmissionLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairAttemptSubmissionLinks_FieldInspectionSubmissions_SubmissionId",
                table: "RepairAttemptSubmissionLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairItems_FieldInspectionSubmissions_EffectiveIntakeSubmissionId",
                table: "RepairItems");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairMeasurementAssessments_FieldInspectionSubmissions_FormalSourceSubmissionId",
                table: "RepairMeasurementAssessments");

            migrationBuilder.DropForeignKey(
                name: "FK_FieldInspectionSessions_Surveys_SurveyId",
                table: "FieldInspectionSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplementarySurveyRequests_Surveys_SurveyId",
                table: "SupplementarySurveyRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_FieldTaskStartOrigins_FieldInspectionOperationOrigins_OperationOriginId",
                table: "FieldTaskStartOrigins");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairExecutionFinishes_FieldInspectionOperationOrigins_OperationOriginId",
                table: "RepairExecutionFinishes");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairExecutionStarts_FieldInspectionOperationOrigins_OperationOriginId",
                table: "RepairExecutionStarts");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairMeasurementAssessments_FieldInspectionOperationOrigins_OperationOriginId",
                table: "RepairMeasurementAssessments");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairMeasurementAssessments_FieldInspectionSessions_SessionId",
                table: "RepairMeasurementAssessments");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairExecutionStarts_FieldTaskStartOrigins_FirstStartId",
                table: "RepairExecutionStarts");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairMeasurementAssessments_FieldTaskStartOrigins_FirstStartId",
                table: "RepairMeasurementAssessments");

            migrationBuilder.DropForeignKey(
                name: "FK_GeometryMapPublications_CrsProfileRevisions_CrsProfileRevisionId_ProjectId",
                table: "GeometryMapPublications");

            migrationBuilder.DropForeignKey(
                name: "FK_PavementLayoutRevisions_CrsProfileRevisions_CrsProfileRevisionId_ProjectId",
                table: "PavementLayoutRevisions");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairFieldTaskBindings_CrsProfileRevisions_CrsProfileRevisionId",
                table: "RepairFieldTaskBindings");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairFieldTaskBindings_GeometryMapPublications_MapPublicationId",
                table: "RepairFieldTaskBindings");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairFieldTaskBindings_PavementLayoutRevisions_LayoutRevisionId",
                table: "RepairFieldTaskBindings");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairAttemptReviews_RepairItems_ItemId",
                table: "RepairAttemptReviews");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairAttempts_RepairItems_ItemId",
                table: "RepairAttempts");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairAttemptSubmissionLinks_RepairItems_ItemId",
                table: "RepairAttemptSubmissionLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairDecisions_RepairItems_ItemId",
                table: "RepairDecisions");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairEligibilityAssessments_RepairItems_ItemId",
                table: "RepairEligibilityAssessments");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairExecutionFinishes_RepairItems_ItemId",
                table: "RepairExecutionFinishes");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairExecutionStarts_RepairItems_ItemId",
                table: "RepairExecutionStarts");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairFieldTaskBindings_RepairItems_ItemId",
                table: "RepairFieldTaskBindings");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairMeasurementAssessments_RepairItems_ItemId",
                table: "RepairMeasurementAssessments");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairSafetyChecks_RepairSafetyMonitoring_MeasureId",
                table: "RepairSafetyChecks");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairPolicyDrafts_RepairPolicyRevisions_PublishedRevisionId",
                table: "RepairPolicyDrafts");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairPolicyDraftChanges_RepairPolicyDrafts_DraftId",
                table: "RepairPolicyDraftChanges");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairSafetyResponsibilityTransfers_RepairTemporarySafetyMeasures_MeasureId",
                table: "RepairSafetyResponsibilityTransfers");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplementarySurveyRequests_SurveyRequests_SurveyRequestId",
                table: "SupplementarySurveyRequests");

            migrationBuilder.DropTable(
                name: "AccountStatusChangeLogs");

            migrationBuilder.DropTable(
                name: "Anh02AiDetectionProvenance");

            migrationBuilder.DropTable(
                name: "Anh02ExportSnapshotFiles");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "BaselineCurrentPointers");

            migrationBuilder.DropTable(
                name: "BusinessDutyAppointments");

            migrationBuilder.DropTable(
                name: "CandidateSourceHeads");

            migrationBuilder.DropTable(
                name: "CaseConclusionDefects");

            migrationBuilder.DropTable(
                name: "CaseConclusionEvidence");

            migrationBuilder.DropTable(
                name: "CasePublicationDefects");

            migrationBuilder.DropTable(
                name: "CasePublicationEvidence");

            migrationBuilder.DropTable(
                name: "CaseReportLinkHistoryReports");

            migrationBuilder.DropTable(
                name: "CaseReportLinks");

            migrationBuilder.DropTable(
                name: "ConsumerEffectReceipts");

            migrationBuilder.DropTable(
                name: "DatasetAssessmentItems");

            migrationBuilder.DropTable(
                name: "DeadlineBreaches");

            migrationBuilder.DropTable(
                name: "DeadlineDutyAppointments");

            migrationBuilder.DropTable(
                name: "DeadlineExtensions");

            migrationBuilder.DropTable(
                name: "DefectSourceLinks");

            migrationBuilder.DropTable(
                name: "DefectStatisticsSources");

            migrationBuilder.DropTable(
                name: "DefectVerificationLogs");

            migrationBuilder.DropTable(
                name: "FieldInspectionEvidenceLinks");

            migrationBuilder.DropTable(
                name: "FieldInspectionLocationProofs");

            migrationBuilder.DropTable(
                name: "FieldInspectionReviews");

            migrationBuilder.DropTable(
                name: "FileScopes");

            migrationBuilder.DropTable(
                name: "H6NotificationCalendar");

            migrationBuilder.DropTable(
                name: "H6NotificationDeliveryAttempts");

            migrationBuilder.DropTable(
                name: "H6NotificationEventReceipts");

            migrationBuilder.DropTable(
                name: "H6NotificationScopes");

            migrationBuilder.DropTable(
                name: "IdempotencyRecords");

            migrationBuilder.DropTable(
                name: "LD06ActionEvidence");

            migrationBuilder.DropTable(
                name: "MeasurementValidationSamples");

            migrationBuilder.DropTable(
                name: "NativeRouteVersionFacts");

            migrationBuilder.DropTable(
                name: "ObligationResponsibilities");

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
                name: "PasswordRecoveryRequests");

            migrationBuilder.DropTable(
                name: "PasswordResetLogs");

            migrationBuilder.DropTable(
                name: "PavementSourceFileReferences");

            migrationBuilder.DropTable(
                name: "ProjectLifecycleHistory");

            migrationBuilder.DropTable(
                name: "ProjectMembers");

            migrationBuilder.DropTable(
                name: "QualityChecks");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "RepairActualScopes");

            migrationBuilder.DropTable(
                name: "RepairAssessmentEvidence");

            migrationBuilder.DropTable(
                name: "RepairAssessmentMeasurements");

            migrationBuilder.DropTable(
                name: "RepairAttemptEvidence");

            migrationBuilder.DropTable(
                name: "RepairCorrectionEvidence");

            migrationBuilder.DropTable(
                name: "RepairDangerAcknowledgements");

            migrationBuilder.DropTable(
                name: "RepairEligibilityHandoverSources");

            migrationBuilder.DropTable(
                name: "RepairEligibilityWarrantySources");

            migrationBuilder.DropTable(
                name: "RepairNormalSuccessors");

            migrationBuilder.DropTable(
                name: "RepairObligationResolutionEvents");

            migrationBuilder.DropTable(
                name: "RepairPolicyMeasurementRules");

            migrationBuilder.DropTable(
                name: "RepairPolicyRevocations");

            migrationBuilder.DropTable(
                name: "RepairReviewRequests");

            migrationBuilder.DropTable(
                name: "RepairSafetyActionSources");

            migrationBuilder.DropTable(
                name: "RepairSafetyCheckEvidence");

            migrationBuilder.DropTable(
                name: "RepairWorkHandovers");

            migrationBuilder.DropTable(
                name: "ReporterRegistrationIntents");

            migrationBuilder.DropTable(
                name: "ReportOriginalEvidence");

            migrationBuilder.DropTable(
                name: "ReportSupplementEvidence");

            migrationBuilder.DropTable(
                name: "RetentionBasisHeads");

            migrationBuilder.DropTable(
                name: "RetentionEvaluationItems");

            migrationBuilder.DropTable(
                name: "RetentionHoldHistories");

            migrationBuilder.DropTable(
                name: "RoadCoverageMappings");

            migrationBuilder.DropTable(
                name: "RoadGeometryMetadata");

            migrationBuilder.DropTable(
                name: "StaffInvitationProjects");

            migrationBuilder.DropTable(
                name: "SurveyAssignments");

            migrationBuilder.DropTable(
                name: "SurveyPlanPostponements");

            migrationBuilder.DropTable(
                name: "SurveyPlanScopes");

            migrationBuilder.DropTable(
                name: "SurveyRequestScopes");

            migrationBuilder.DropTable(
                name: "TrainingLabelReviews");

            migrationBuilder.DropTable(
                name: "UploadMultipartSweeps");

            migrationBuilder.DropTable(
                name: "UploadParts");

            migrationBuilder.DropTable(
                name: "WeeklyReviewDigestDuties");

            migrationBuilder.DropTable(
                name: "Anh02AiManifestFiles");

            migrationBuilder.DropTable(
                name: "BaselineSelectionItems");

            migrationBuilder.DropTable(
                name: "BusinessReceivingRequests");

            migrationBuilder.DropTable(
                name: "CaseConclusions");

            migrationBuilder.DropTable(
                name: "CasePublicationRecipients");

            migrationBuilder.DropTable(
                name: "CaseReportLinkHistory");

            migrationBuilder.DropTable(
                name: "SourceDecisions");

            migrationBuilder.DropTable(
                name: "SeverityRuleVersions");

            migrationBuilder.DropTable(
                name: "H6NotificationDeliveries");

            migrationBuilder.DropTable(
                name: "H6NotificationAudits");

            migrationBuilder.DropTable(
                name: "DerivedMeasurements");

            migrationBuilder.DropTable(
                name: "ValidationRuns");

            migrationBuilder.DropTable(
                name: "RoadRouteSystems");

            migrationBuilder.DropTable(
                name: "LD06LifecycleActions");

            migrationBuilder.DropTable(
                name: "OfflineOperationAdmissions");

            migrationBuilder.DropTable(
                name: "SurveyFiles");

            migrationBuilder.DropTable(
                name: "Sessions");

            migrationBuilder.DropTable(
                name: "FieldInspectionEvidenceReuseDecisions");

            migrationBuilder.DropTable(
                name: "GroundTruthMeasurements");

            migrationBuilder.DropTable(
                name: "RepairDangerWarnings");

            migrationBuilder.DropTable(
                name: "Warranties");

            migrationBuilder.DropTable(
                name: "FieldInspectionTaskEvents");

            migrationBuilder.DropTable(
                name: "RepairItemLifecycleEvents");

            migrationBuilder.DropTable(
                name: "ReportSupplements");

            migrationBuilder.DropTable(
                name: "RetentionBasisRevisions");

            migrationBuilder.DropTable(
                name: "RetentionEvaluations");

            migrationBuilder.DropTable(
                name: "RetentionHolds");

            migrationBuilder.DropTable(
                name: "RoadGeometryDrafts");

            migrationBuilder.DropTable(
                name: "StaffInvitations");

            migrationBuilder.DropTable(
                name: "TrainingLabelRevisions");

            migrationBuilder.DropTable(
                name: "UploadSessions");

            migrationBuilder.DropTable(
                name: "WeeklyReviewDigests");

            migrationBuilder.DropTable(
                name: "BaselineSelections");

            migrationBuilder.DropTable(
                name: "DatasetAssessments");

            migrationBuilder.DropTable(
                name: "CasePublications");

            migrationBuilder.DropTable(
                name: "RoadSegments");

            migrationBuilder.DropTable(
                name: "H6NotificationOccurrences");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "OfflineOperationBindings");

            migrationBuilder.DropTable(
                name: "OfflineSyncBatches");

            migrationBuilder.DropTable(
                name: "Flights");

            migrationBuilder.DropTable(
                name: "HandoverDocuments");

            migrationBuilder.DropTable(
                name: "GeometryLocationImpactDecisions");

            migrationBuilder.DropTable(
                name: "TrainingLabels");

            migrationBuilder.DropTable(
                name: "WeeklyReviewRecoveryPeriods");

            migrationBuilder.DropTable(
                name: "IncidentCases");

            migrationBuilder.DropTable(
                name: "OutboxMessages");

            migrationBuilder.DropTable(
                name: "OfflineTaskSnapshots");

            migrationBuilder.DropTable(
                name: "OfflineHandoverGrants");

            migrationBuilder.DropTable(
                name: "DroneDevices");

            migrationBuilder.DropTable(
                name: "GeometryLocationImpacts");

            migrationBuilder.DropTable(
                name: "Reports");

            migrationBuilder.DropTable(
                name: "OfflineEncryptedPackages");

            migrationBuilder.DropTable(
                name: "OfflineDeviceRegistrations");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "AIModelVersions");

            migrationBuilder.DropTable(
                name: "DefectTypes");

            migrationBuilder.DropTable(
                name: "ProcessingJobs");

            migrationBuilder.DropTable(
                name: "ProcessingBlocks");

            migrationBuilder.DropTable(
                name: "RoadSectionVersions");

            migrationBuilder.DropTable(
                name: "AIDetections");

            migrationBuilder.DropTable(
                name: "Anh02AiMockRuns");

            migrationBuilder.DropTable(
                name: "Anh02AiResultProvenance");

            migrationBuilder.DropTable(
                name: "SurveyDataVersions");

            migrationBuilder.DropTable(
                name: "ProcessingAttempts");

            migrationBuilder.DropTable(
                name: "Files");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "RoadSegmentSets");

            migrationBuilder.DropTable(
                name: "Anh02ExportSnapshots");

            migrationBuilder.DropTable(
                name: "Anh02GeneratedArtifacts");

            migrationBuilder.DropTable(
                name: "Anh02ExportJobs");

            migrationBuilder.DropTable(
                name: "DeadlineClocks");

            migrationBuilder.DropTable(
                name: "Defects");

            migrationBuilder.DropTable(
                name: "CauseCategories");

            migrationBuilder.DropTable(
                name: "RepairObligations");

            migrationBuilder.DropTable(
                name: "RoadSections");

            migrationBuilder.DropTable(
                name: "FieldInspectionTasks");

            migrationBuilder.DropTable(
                name: "FieldInspectionAssignments");

            migrationBuilder.DropTable(
                name: "FieldInspectionSubmissions");

            migrationBuilder.DropTable(
                name: "Surveys");

            migrationBuilder.DropTable(
                name: "FieldInspectionOperationOrigins");

            migrationBuilder.DropTable(
                name: "FieldInspectionSessions");

            migrationBuilder.DropTable(
                name: "FieldTaskStartOrigins");

            migrationBuilder.DropTable(
                name: "CrsProfileRevisions");

            migrationBuilder.DropTable(
                name: "GeometryMapPublications");

            migrationBuilder.DropTable(
                name: "PavementLayoutRevisions");

            migrationBuilder.DropTable(
                name: "RepairItems");

            migrationBuilder.DropTable(
                name: "RepairAttemptReviews");

            migrationBuilder.DropTable(
                name: "RepairDecisions");

            migrationBuilder.DropTable(
                name: "RepairPackages");

            migrationBuilder.DropTable(
                name: "RepairAttemptSubmissionLinks");

            migrationBuilder.DropTable(
                name: "RepairAttempts");

            migrationBuilder.DropTable(
                name: "RepairExecutionFinishes");

            migrationBuilder.DropTable(
                name: "RepairExecutionStarts");

            migrationBuilder.DropTable(
                name: "RepairEligibilityAssessments");

            migrationBuilder.DropTable(
                name: "RepairMeasurementAssessments");

            migrationBuilder.DropTable(
                name: "RepairFieldTaskBindings");

            migrationBuilder.DropTable(
                name: "RepairExecutionAuthorizations");

            migrationBuilder.DropTable(
                name: "RepairSafetyMonitoring");

            migrationBuilder.DropTable(
                name: "RepairSafetyChecks");

            migrationBuilder.DropTable(
                name: "RepairPolicyRevisions");

            migrationBuilder.DropTable(
                name: "RepairPolicyDrafts");

            migrationBuilder.DropTable(
                name: "RepairPolicyDraftChanges");

            migrationBuilder.DropTable(
                name: "RepairTemporarySafetyMeasures");

            migrationBuilder.DropTable(
                name: "RepairSafetyResponsibilityTransfers");

            migrationBuilder.DropTable(
                name: "SurveyRequests");

            migrationBuilder.DropTable(
                name: "SupplementarySurveyRequests");

            migrationBuilder.DropTable(
                name: "SurveyPlans");
        }
    }
}
