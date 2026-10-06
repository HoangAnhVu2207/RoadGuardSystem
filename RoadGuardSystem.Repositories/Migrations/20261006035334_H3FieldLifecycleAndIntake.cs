using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class H3FieldLifecycleAndIntake : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_GroundTruthMeasurements_EvidenceOrReason",
                table: "GroundTruthMeasurements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_GroundTruthMeasurements_Location",
                table: "GroundTruthMeasurements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_GroundTruthMeasurements_MeasurementType",
                table: "GroundTruthMeasurements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_GroundTruthMeasurements_Unit",
                table: "GroundTruthMeasurements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_GroundTruthMeasurements_Value",
                table: "GroundTruthMeasurements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_FieldInspectionTasks_Status",
                table: "FieldInspectionTasks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_FieldInspectionSessions_Purpose",
                table: "FieldInspectionSessions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_FieldInspectionSessions_PurposeScope",
                table: "FieldInspectionSessions");

            migrationBuilder.AlterColumn<decimal>(
                name: "Value",
                table: "GroundTruthMeasurements",
                type: "decimal(19,6)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(19,6)");

            migrationBuilder.AlterColumn<Point>(
                name: "Location",
                table: "GroundTruthMeasurements",
                type: "geography",
                nullable: true,
                oldClrType: typeof(Point),
                oldType: "geography");

            migrationBuilder.AddColumn<string>(
                name: "Dimension",
                table: "GroundTruthMeasurements",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "LENGTH");

            migrationBuilder.AddColumn<string>(
                name: "LocationReason",
                table: "GroundTruthMeasurements",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocationState",
                table: "GroundTruthMeasurements",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "CAPTURED");

            migrationBuilder.AddColumn<string>(
                name: "UnknownReason",
                table: "GroundTruthMeasurements",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ValueState",
                table: "GroundTruthMeasurements",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "KNOWN");

            migrationBuilder.AlterColumn<Guid>(
                name: "SurveyId",
                table: "FieldInspectionTasks",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "CrsProfileRevisionId",
                table: "FieldInspectionTasks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LayoutRevisionId",
                table: "FieldInspectionTasks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LifecycleVersion",
                table: "FieldInspectionTasks",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<Guid>(
                name: "MapPublicationId",
                table: "FieldInspectionTasks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "Purpose",
                table: "FieldInspectionTasks",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)1);

            migrationBuilder.AddColumn<Guid>(
                name: "SegmentSetId",
                table: "FieldInspectionTasks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SlabId",
                table: "FieldInspectionTasks",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceKind",
                table: "FieldInspectionTasks",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "SURVEY");

            migrationBuilder.AddColumn<string>(
                name: "TaskMode",
                table: "FieldInspectionTasks",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "MEASURE_ONLY");

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
                        name: "FK_FieldInspectionEvidenceReuseDecisions_FieldInspectionTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "FieldInspectionTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                    table.CheckConstraint("CK_FieldInspectionOperationOrigins_Kind", "[Kind] IN ('FIELD_START','FIELD_SUBMISSION') AND [SchemaVersion]=1");
                    table.ForeignKey(
                        name: "FK_FieldInspectionOperationOrigins_FieldInspectionTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "FieldInspectionTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
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
                        name: "FK_FieldInspectionTaskEvents_FieldInspectionTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "FieldInspectionTasks",
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
                        name: "FK_FieldInspectionSubmissions_FieldInspectionTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "FieldInspectionTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldInspectionSubmissions_FieldTaskStartOrigins_StartOriginId",
                        column: x => x.StartOriginId,
                        principalTable: "FieldTaskStartOrigins",
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
                        name: "FK_FieldInspectionEvidenceLinks_FieldInspectionSubmissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "FieldInspectionSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldInspectionEvidenceLinks_FieldInspectionTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "FieldInspectionTasks",
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
                        name: "FK_FieldInspectionLocationProofs_FieldInspectionSubmissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "FieldInspectionSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldInspectionLocationProofs_FieldInspectionTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "FieldInspectionTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldInspectionLocationProofs_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
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
                        name: "FK_FieldInspectionReviews_FieldInspectionSubmissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "FieldInspectionSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldInspectionReviews_FieldInspectionTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "FieldInspectionTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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

            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [GroundTruthMeasurements] WHERE [EvidenceFileId] IS NULL
                    AND [ValueState]='KNOWN' AND [LocationState]='CAPTURED'
                    AND ([Notes] IS NULL OR LEN(LTRIM(RTRIM([Notes])))=0))
                    THROW 51136, 'Legacy measurement without evidence or sourced absence reason requires explicit remediation.', 1;
                """);
            migrationBuilder.AddCheckConstraint(
                name: "CK_GroundTruthMeasurements_EvidenceOrReason",
                table: "GroundTruthMeasurements",
                sql: "[EvidenceFileId] IS NOT NULL OR [ValueState]='UNKNOWN' OR [LocationState]='UNKNOWN' OR ([Notes] IS NOT NULL AND LEN(LTRIM(RTRIM([Notes]))) > 0)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_GroundTruthMeasurements_Location",
                table: "GroundTruthMeasurements",
                sql: "([Location] IS NOT NULL AND [LocationState]='CAPTURED' AND [Location].STSrid=4326 AND [Location].STIsEmpty()=0) OR ([Location] IS NULL AND [LocationState]='UNKNOWN' AND [LocationReason] IS NOT NULL AND LEN(LTRIM(RTRIM([LocationReason])))>0)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_GroundTruthMeasurements_MeasurementType",
                table: "GroundTruthMeasurements",
                sql: "[MeasurementType] IN (1, 2, 3, 4)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_GroundTruthMeasurements_Unit",
                table: "GroundTruthMeasurements",
                sql: "([MeasurementType] IN (1,2,3) AND [Dimension]='LENGTH' AND LOWER([Unit]) IN ('mm','cm','m')) OR ([MeasurementType]=4 AND [Dimension]='AREA' AND [Unit]=N'm²')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_GroundTruthMeasurements_Value",
                table: "GroundTruthMeasurements",
                sql: "([ValueState]='KNOWN' AND [Value] IS NOT NULL AND [Value]>=0) OR ([ValueState]='UNKNOWN' AND [Value] IS NULL AND [UnknownReason] IS NOT NULL AND LEN(LTRIM(RTRIM([UnknownReason])))>0)");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionTasks_CrsProfileRevisionId",
                table: "FieldInspectionTasks",
                column: "CrsProfileRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionTasks_LayoutRevisionId",
                table: "FieldInspectionTasks",
                column: "LayoutRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionTasks_MapPublicationId",
                table: "FieldInspectionTasks",
                column: "MapPublicationId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionTasks_SegmentSetId",
                table: "FieldInspectionTasks",
                column: "SegmentSetId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_FieldInspectionTasks_Source",
                table: "FieldInspectionTasks",
                sql: "([LifecycleVersion]=1 AND [SourceKind]='SURVEY' AND [SurveyId] IS NOT NULL) OR ([LifecycleVersion]=2 AND [Purpose] IN(3,4,5) AND (([SourceKind]='SURVEY' AND [SurveyId] IS NOT NULL) OR ([SourceKind]='REPORTER' AND [SurveyId] IS NULL)))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_FieldInspectionTasks_Status",
                table: "FieldInspectionTasks",
                sql: "[Status] IN (1, 2, 3, 4, 5, 6, 7, 8)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_FieldInspectionSessions_Purpose",
                table: "FieldInspectionSessions",
                sql: "[Purpose] IN (1, 2, 3, 4, 5)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_FieldInspectionSessions_PurposeScope",
                table: "FieldInspectionSessions",
                sql: "([Purpose] = 1 AND [FieldInspectionTaskId] IS NOT NULL AND [SurveyId] IS NOT NULL AND [InspectorUserId] IS NOT NULL) OR ([Purpose] = 2 AND [FieldInspectionTaskId] IS NULL) OR ([Purpose] IN (3,4,5) AND [FieldInspectionTaskId] IS NOT NULL AND [InspectorUserId] IS NOT NULL)");

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
                name: "UX_FieldInspectionSubmissions_Session",
                table: "FieldInspectionSubmissions",
                column: "SessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionSubmissions_StartOriginId",
                table: "FieldInspectionSubmissions",
                column: "StartOriginId");

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
                name: "IX_FieldInspectionTaskEvents_ProjectId",
                table: "FieldInspectionTaskEvents",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionTaskEvents_TaskId",
                table: "FieldInspectionTaskEvents",
                column: "TaskId");

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

            migrationBuilder.AddForeignKey(
                name: "FK_FieldInspectionTasks_CrsProfileRevisions_CrsProfileRevisionId",
                table: "FieldInspectionTasks",
                column: "CrsProfileRevisionId",
                principalTable: "CrsProfileRevisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FieldInspectionTasks_GeometryMapPublications_MapPublicationId",
                table: "FieldInspectionTasks",
                column: "MapPublicationId",
                principalTable: "GeometryMapPublications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FieldInspectionTasks_PavementLayoutRevisions_LayoutRevisionId",
                table: "FieldInspectionTasks",
                column: "LayoutRevisionId",
                principalTable: "PavementLayoutRevisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FieldInspectionTasks_RoadSegmentSets_SegmentSetId",
                table: "FieldInspectionTasks",
                column: "SegmentSetId",
                principalTable: "RoadSegmentSets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddColumn<string>(
                name: "FactsJson",
                table: "FieldInspectionTaskEvents",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<Guid>(
                name: "LocationImpactDecisionId",
                table: "FieldInspectionTaskEvents",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LocationImpactId",
                table: "FieldInspectionTaskEvents",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionTaskEvents_LocationImpactDecisionId",
                table: "FieldInspectionTaskEvents",
                column: "LocationImpactDecisionId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionTaskEvents_LocationImpactId",
                table: "FieldInspectionTaskEvents",
                column: "LocationImpactId");

            migrationBuilder.AddForeignKey(
                name: "FK_FieldInspectionTaskEvents_GeometryLocationImpactDecisions_LocationImpactDecisionId",
                table: "FieldInspectionTaskEvents",
                column: "LocationImpactDecisionId",
                principalTable: "GeometryLocationImpactDecisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FieldInspectionTaskEvents_GeometryLocationImpacts_LocationImpactId",
                table: "FieldInspectionTaskEvents",
                column: "LocationImpactId",
                principalTable: "GeometryLocationImpacts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
            InstallFieldScopeGuards(migrationBuilder);
            InstallHistoryGuards(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            RefuseLossAndRemoveGuards(migrationBuilder);
            migrationBuilder.DropForeignKey(
                name: "FK_FieldInspectionTasks_CrsProfileRevisions_CrsProfileRevisionId",
                table: "FieldInspectionTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_FieldInspectionTasks_GeometryMapPublications_MapPublicationId",
                table: "FieldInspectionTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_FieldInspectionTasks_PavementLayoutRevisions_LayoutRevisionId",
                table: "FieldInspectionTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_FieldInspectionTasks_RoadSegmentSets_SegmentSetId",
                table: "FieldInspectionTasks");

            migrationBuilder.DropTable(
                name: "FieldInspectionEvidenceLinks");

            migrationBuilder.DropTable(
                name: "FieldInspectionEvidenceReuseDecisions");

            migrationBuilder.DropTable(
                name: "FieldInspectionLocationProofs");

            migrationBuilder.DropTable(
                name: "FieldInspectionReviews");

            migrationBuilder.DropTable(
                name: "FieldInspectionTaskEvents");

            migrationBuilder.DropTable(
                name: "FieldInspectionSubmissions");

            migrationBuilder.DropTable(
                name: "FieldTaskStartOrigins");

            migrationBuilder.DropTable(
                name: "FieldInspectionOperationOrigins");

            migrationBuilder.DropCheckConstraint(
                name: "CK_GroundTruthMeasurements_EvidenceOrReason",
                table: "GroundTruthMeasurements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_GroundTruthMeasurements_Location",
                table: "GroundTruthMeasurements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_GroundTruthMeasurements_MeasurementType",
                table: "GroundTruthMeasurements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_GroundTruthMeasurements_Unit",
                table: "GroundTruthMeasurements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_GroundTruthMeasurements_Value",
                table: "GroundTruthMeasurements");

            migrationBuilder.DropIndex(
                name: "IX_FieldInspectionTasks_CrsProfileRevisionId",
                table: "FieldInspectionTasks");

            migrationBuilder.DropIndex(
                name: "IX_FieldInspectionTasks_LayoutRevisionId",
                table: "FieldInspectionTasks");

            migrationBuilder.DropIndex(
                name: "IX_FieldInspectionTasks_MapPublicationId",
                table: "FieldInspectionTasks");

            migrationBuilder.DropIndex(
                name: "IX_FieldInspectionTasks_SegmentSetId",
                table: "FieldInspectionTasks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_FieldInspectionTasks_Source",
                table: "FieldInspectionTasks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_FieldInspectionTasks_Status",
                table: "FieldInspectionTasks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_FieldInspectionSessions_Purpose",
                table: "FieldInspectionSessions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_FieldInspectionSessions_PurposeScope",
                table: "FieldInspectionSessions");

            migrationBuilder.DropColumn(
                name: "Dimension",
                table: "GroundTruthMeasurements");

            migrationBuilder.DropColumn(
                name: "LocationReason",
                table: "GroundTruthMeasurements");

            migrationBuilder.DropColumn(
                name: "LocationState",
                table: "GroundTruthMeasurements");

            migrationBuilder.DropColumn(
                name: "UnknownReason",
                table: "GroundTruthMeasurements");

            migrationBuilder.DropColumn(
                name: "ValueState",
                table: "GroundTruthMeasurements");

            migrationBuilder.DropColumn(
                name: "CrsProfileRevisionId",
                table: "FieldInspectionTasks");

            migrationBuilder.DropColumn(
                name: "LayoutRevisionId",
                table: "FieldInspectionTasks");

            migrationBuilder.DropColumn(
                name: "LifecycleVersion",
                table: "FieldInspectionTasks");

            migrationBuilder.DropColumn(
                name: "MapPublicationId",
                table: "FieldInspectionTasks");

            migrationBuilder.DropColumn(
                name: "Purpose",
                table: "FieldInspectionTasks");

            migrationBuilder.DropColumn(
                name: "SegmentSetId",
                table: "FieldInspectionTasks");

            migrationBuilder.DropColumn(
                name: "SlabId",
                table: "FieldInspectionTasks");

            migrationBuilder.DropColumn(
                name: "SourceKind",
                table: "FieldInspectionTasks");

            migrationBuilder.DropColumn(
                name: "TaskMode",
                table: "FieldInspectionTasks");

            migrationBuilder.AlterColumn<decimal>(
                name: "Value",
                table: "GroundTruthMeasurements",
                type: "decimal(19,6)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(19,6)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Point>(
                name: "Location",
                table: "GroundTruthMeasurements",
                type: "geography",
                nullable: false,
                oldClrType: typeof(Point),
                oldType: "geography",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "SurveyId",
                table: "FieldInspectionTasks",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_GroundTruthMeasurements_EvidenceOrReason",
                table: "GroundTruthMeasurements",
                sql: "[EvidenceFileId] IS NOT NULL OR LEN(LTRIM(RTRIM([Notes]))) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_GroundTruthMeasurements_Location",
                table: "GroundTruthMeasurements",
                sql: "[Location].STSrid = 4326 AND [Location].STIsEmpty() = 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_GroundTruthMeasurements_MeasurementType",
                table: "GroundTruthMeasurements",
                sql: "[MeasurementType] IN (1, 2, 3)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_GroundTruthMeasurements_Unit",
                table: "GroundTruthMeasurements",
                sql: "LOWER([Unit]) IN ('mm', 'cm', 'm')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_GroundTruthMeasurements_Value",
                table: "GroundTruthMeasurements",
                sql: "[Value] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_FieldInspectionTasks_Status",
                table: "FieldInspectionTasks",
                sql: "[Status] IN (1, 2, 3, 4, 5, 6, 7)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_FieldInspectionSessions_Purpose",
                table: "FieldInspectionSessions",
                sql: "[Purpose] IN (1, 2)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_FieldInspectionSessions_PurposeScope",
                table: "FieldInspectionSessions",
                sql: "([Purpose] = 1 AND [FieldInspectionTaskId] IS NOT NULL AND [SurveyId] IS NOT NULL AND [InspectorUserId] IS NOT NULL) OR ([Purpose] = 2 AND [FieldInspectionTaskId] IS NULL)");
            RestoreLegacyIntegrity(migrationBuilder);
        }
    }
}
