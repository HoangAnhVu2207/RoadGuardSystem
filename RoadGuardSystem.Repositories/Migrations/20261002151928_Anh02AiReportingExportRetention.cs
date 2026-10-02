using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class Anh02AiReportingExportRetention : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                        name: "FK_Anh02AiMockRuns_ProcessingAttempts_AttemptId",
                        column: x => x.AttemptId,
                        principalTable: "ProcessingAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Anh02AiMockRuns_ProcessingJobs_ProcessingJobId",
                        column: x => x.ProcessingJobId,
                        principalTable: "ProcessingJobs",
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
                        name: "FK_Anh02AiMockRuns_SurveyDataVersions_DatasetVersionId",
                        column: x => x.DatasetVersionId,
                        principalTable: "SurveyDataVersions",
                        principalColumn: "Id",
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
                    table.ForeignKey(
                        name: "FK_Anh02AiResultProvenance_ProcessingAttempts_AttemptId",
                        column: x => x.AttemptId,
                        principalTable: "ProcessingAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Anh02AiResultProvenance_ProcessingJobs_ProcessingJobId",
                        column: x => x.ProcessingJobId,
                        principalTable: "ProcessingJobs",
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
                name: "FK_Anh02ExportJobs_Anh02GeneratedArtifacts_ArtifactId",
                table: "Anh02ExportJobs",
                column: "ArtifactId",
                principalTable: "Anh02GeneratedArtifacts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
            migrationBuilder.Sql(@"CREATE TRIGGER [TR_Anh02AiResultProvenance_Immutable] ON [dbo].[Anh02AiResultProvenance] AFTER UPDATE, DELETE AS
BEGIN SET NOCOUNT ON; THROW 51220, 'ANH-02 evidence is immutable; append a new revision instead.', 1; END");

            migrationBuilder.Sql(@"CREATE TRIGGER [TR_Anh02AiDetectionProvenance_Immutable] ON [dbo].[Anh02AiDetectionProvenance] AFTER UPDATE, DELETE AS
BEGIN SET NOCOUNT ON; THROW 51220, 'ANH-02 evidence is immutable; append a new revision instead.', 1; END");

            migrationBuilder.Sql(@"CREATE TRIGGER [TR_Anh02AiManifestFiles_Immutable] ON [dbo].[Anh02AiManifestFiles] AFTER UPDATE, DELETE AS
BEGIN SET NOCOUNT ON; THROW 51220, 'ANH-02 evidence is immutable; append a new revision instead.', 1; END");

            migrationBuilder.Sql(@"CREATE TRIGGER [TR_Anh02ExportSnapshots_Immutable] ON [dbo].[Anh02ExportSnapshots] AFTER UPDATE, DELETE AS
BEGIN SET NOCOUNT ON; THROW 51220, 'ANH-02 evidence is immutable; append a new revision instead.', 1; END");

            migrationBuilder.Sql(@"CREATE TRIGGER [TR_Anh02ExportSnapshotFiles_Immutable] ON [dbo].[Anh02ExportSnapshotFiles] AFTER UPDATE, DELETE AS
BEGIN SET NOCOUNT ON; THROW 51220, 'ANH-02 evidence is immutable; append a new revision instead.', 1; END");

            migrationBuilder.Sql(@"CREATE TRIGGER [TR_Anh02GeneratedArtifacts_Immutable] ON [dbo].[Anh02GeneratedArtifacts] AFTER UPDATE, DELETE AS
BEGIN SET NOCOUNT ON; THROW 51220, 'ANH-02 evidence is immutable; append a new revision instead.', 1; END");

            migrationBuilder.Sql(@"CREATE TRIGGER [TR_RetentionBasisRevisions_Immutable] ON [dbo].[RetentionBasisRevisions] AFTER UPDATE, DELETE AS
BEGIN SET NOCOUNT ON; THROW 51220, 'ANH-02 evidence is immutable; append a new revision instead.', 1; END");

            migrationBuilder.Sql(@"CREATE TRIGGER [TR_RetentionHoldHistories_Immutable] ON [dbo].[RetentionHoldHistories] AFTER UPDATE, DELETE AS
BEGIN SET NOCOUNT ON; THROW 51220, 'ANH-02 evidence is immutable; append a new revision instead.', 1; END");

            migrationBuilder.Sql(@"CREATE TRIGGER [TR_RetentionEvaluationItems_Immutable] ON [dbo].[RetentionEvaluationItems] AFTER UPDATE, DELETE AS
BEGIN SET NOCOUNT ON; THROW 51220, 'ANH-02 evidence is immutable; append a new revision instead.', 1; END");

            migrationBuilder.Sql(@"CREATE TRIGGER [TR_Anh02AiMockRuns_Identity] ON [dbo].[Anh02AiMockRuns] AFTER UPDATE, DELETE AS
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
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"IF EXISTS (SELECT 1 FROM [dbo].[Anh02AiResultProvenance])
 OR EXISTS (SELECT 1 FROM [dbo].[Anh02AiDetectionProvenance])
 OR EXISTS (SELECT 1 FROM [dbo].[Anh02AiManifestFiles])
 OR EXISTS (SELECT 1 FROM [dbo].[Anh02ExportSnapshots])
 OR EXISTS (SELECT 1 FROM [dbo].[Anh02ExportSnapshotFiles])
 OR EXISTS (SELECT 1 FROM [dbo].[Anh02GeneratedArtifacts])
 OR EXISTS (SELECT 1 FROM [dbo].[RetentionBasisRevisions])
 OR EXISTS (SELECT 1 FROM [dbo].[RetentionHoldHistories])
 OR EXISTS (SELECT 1 FROM [dbo].[RetentionEvaluationItems])
 OR EXISTS (SELECT 1 FROM [dbo].[Anh02AiMockRuns])
 OR EXISTS (SELECT 1 FROM [dbo].[Anh02ExportJobs])
 OR EXISTS (SELECT 1 FROM [dbo].[RetentionBasisHeads])
 OR EXISTS (SELECT 1 FROM [dbo].[RetentionEvaluations])
 OR EXISTS (SELECT 1 FROM [dbo].[RetentionHolds])
 THROW 51222, 'Cannot downgrade populated ANH-02 evidence; preserve data and use a forward migration.', 1;");


            migrationBuilder.DropForeignKey(
                name: "FK_Anh02AiResultProvenance_Anh02AiMockRuns_RunId",
                table: "Anh02AiResultProvenance");

            migrationBuilder.DropForeignKey(
                name: "FK_Anh02ExportJobs_Anh02ExportSnapshots_SnapshotId",
                table: "Anh02ExportJobs");

            migrationBuilder.DropForeignKey(
                name: "FK_Anh02GeneratedArtifacts_Anh02ExportSnapshots_SnapshotId",
                table: "Anh02GeneratedArtifacts");

            migrationBuilder.DropForeignKey(
                name: "FK_Anh02ExportJobs_Anh02GeneratedArtifacts_ArtifactId",
                table: "Anh02ExportJobs");

            migrationBuilder.DropTable(
                name: "Anh02AiDetectionProvenance");

            migrationBuilder.DropTable(
                name: "Anh02ExportSnapshotFiles");

            migrationBuilder.DropTable(
                name: "RetentionBasisHeads");

            migrationBuilder.DropTable(
                name: "RetentionEvaluationItems");

            migrationBuilder.DropTable(
                name: "RetentionHoldHistories");

            migrationBuilder.DropTable(
                name: "Anh02AiManifestFiles");

            migrationBuilder.DropTable(
                name: "RetentionBasisRevisions");

            migrationBuilder.DropTable(
                name: "RetentionEvaluations");

            migrationBuilder.DropTable(
                name: "RetentionHolds");

            migrationBuilder.DropTable(
                name: "Anh02AiMockRuns");

            migrationBuilder.DropTable(
                name: "Anh02AiResultProvenance");

            migrationBuilder.DropTable(
                name: "Anh02ExportSnapshots");

            migrationBuilder.DropTable(
                name: "Anh02GeneratedArtifacts");

            migrationBuilder.DropTable(
                name: "Anh02ExportJobs");
        }
    }
}
