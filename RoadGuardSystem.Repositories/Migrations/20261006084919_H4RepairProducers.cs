using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class H4RepairProducers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RepairSafetyResponsibilityTransfers_MeasureId",
                table: "RepairSafetyResponsibilityTransfers");

            migrationBuilder.AddColumn<Guid>(
                name: "CurrentResponsibilityTransferId",
                table: "RepairTemporarySafetyMeasures",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "DefectStatusAtAnchor",
                table: "RepairPackages",
                type: "tinyint",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CurrentRepairItemId",
                table: "RepairObligations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OriginalCrewFirstStartId",
                table: "RepairObligations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PreviousHeadDecisionId",
                table: "RepairObligationResolutionEvents",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CurrentAssessmentId",
                table: "RepairItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CurrentAttemptId",
                table: "RepairItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CurrentBindingId",
                table: "RepairItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CurrentExecutionFinishId",
                table: "RepairItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CurrentExecutionStartId",
                table: "RepairItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CurrentIntakeLinkId",
                table: "RepairItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CurrentReviewId",
                table: "RepairItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EffectiveIntakeSubmissionId",
                table: "RepairItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PredecessorItemId",
                table: "RepairItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SupersededByItemId",
                table: "RepairItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PreviousObligationHeadDecisionId",
                table: "RepairDecisions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RepairItemId",
                table: "FieldInspectionTasks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_RepairSafetyResponsibilityTransfers_MeasureId_Id",
                table: "RepairSafetyResponsibilityTransfers",
                columns: new[] { "MeasureId", "Id" });

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
                        name: "FK_RepairFieldTaskBindings_RepairItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "RepairItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairFieldTaskBindings_RepairObligations_ObligationId",
                        column: x => x.ObligationId,
                        principalTable: "RepairObligations",
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
                        name: "FK_RepairAssessmentEvidence_RepairMeasurementAssessments_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "RepairMeasurementAssessments",
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
                    table.ForeignKey(
                        name: "FK_RepairAssessmentMeasurements_RepairMeasurementAssessments_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "RepairMeasurementAssessments",
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
                        name: "FK_RepairEligibilityAssessments_RepairFieldTaskBindings_BindingId",
                        column: x => x.BindingId,
                        principalTable: "RepairFieldTaskBindings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairEligibilityAssessments_RepairItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "RepairItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairEligibilityAssessments_RepairMeasurementAssessments_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "RepairMeasurementAssessments",
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
                    SourceAssessmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairNormalSuccessors", x => x.Id);
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
                        name: "FK_RepairExecutionStarts_RepairFieldTaskBindings_BindingId",
                        column: x => x.BindingId,
                        principalTable: "RepairFieldTaskBindings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairExecutionStarts_RepairItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "RepairItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairExecutionStarts_RepairMeasurementAssessments_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "RepairMeasurementAssessments",
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
                        name: "FK_RepairExecutionFinishes_RepairExecutionStarts_ExecutionStartId",
                        column: x => x.ExecutionStartId,
                        principalTable: "RepairExecutionStarts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairExecutionFinishes_RepairFieldTaskBindings_BindingId",
                        column: x => x.BindingId,
                        principalTable: "RepairFieldTaskBindings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairExecutionFinishes_RepairItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "RepairItems",
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
                    table.ForeignKey(
                        name: "FK_RepairAttemptSubmissionLinks_RepairExecutionFinishes_ExecutionFinishId",
                        column: x => x.ExecutionFinishId,
                        principalTable: "RepairExecutionFinishes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairAttemptSubmissionLinks_RepairFieldTaskBindings_BindingId",
                        column: x => x.BindingId,
                        principalTable: "RepairFieldTaskBindings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairAttemptSubmissionLinks_RepairItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "RepairItems",
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
                        name: "FK_RepairAttemptReviews_RepairAttemptSubmissionLinks_IntakeLinkId",
                        column: x => x.IntakeLinkId,
                        principalTable: "RepairAttemptSubmissionLinks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairAttemptReviews_RepairAttempts_AttemptId",
                        column: x => x.AttemptId,
                        principalTable: "RepairAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairAttemptReviews_RepairFieldTaskBindings_BindingId",
                        column: x => x.BindingId,
                        principalTable: "RepairFieldTaskBindings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairAttemptReviews_RepairItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "RepairItems",
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
                        name: "FK_RepairItemLifecycleEvents_RepairItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "RepairItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairItemLifecycleEvents_RepairObligations_ObligationId",
                        column: x => x.ObligationId,
                        principalTable: "RepairObligations",
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
                    ReusedSource = table.Column<bool>(type: "bit", nullable: false)
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
                        name: "FK_RepairSafetyChecks_RepairTemporarySafetyMeasures_MeasureId",
                        column: x => x.MeasureId,
                        principalTable: "RepairTemporarySafetyMeasures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                    table.ForeignKey(
                        name: "FK_RepairSafetyMonitoring_RepairTemporarySafetyMeasures_MeasureId",
                        column: x => x.MeasureId,
                        principalTable: "RepairTemporarySafetyMeasures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RepairTemporarySafetyMeasures_Id_CurrentResponsibilityTransferId",
                table: "RepairTemporarySafetyMeasures",
                columns: new[] { "Id", "CurrentResponsibilityTransferId" });

            migrationBuilder.CreateIndex(
                name: "IX_RepairObligations_CurrentRepairItemId",
                table: "RepairObligations",
                column: "CurrentRepairItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairObligations_OriginalCrewFirstStartId",
                table: "RepairObligations",
                column: "OriginalCrewFirstStartId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairObligationResolutionEvents_PreviousHeadDecisionId",
                table: "RepairObligationResolutionEvents",
                column: "PreviousHeadDecisionId");

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
                name: "IX_RepairItems_EffectiveIntakeSubmissionId",
                table: "RepairItems",
                column: "EffectiveIntakeSubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairItems_PredecessorItemId",
                table: "RepairItems",
                column: "PredecessorItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairItems_SupersededByItemId",
                table: "RepairItems",
                column: "SupersededByItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairDecisions_PreviousObligationHeadDecisionId",
                table: "RepairDecisions",
                column: "PreviousObligationHeadDecisionId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionTasks_RepairItemId",
                table: "FieldInspectionTasks",
                column: "RepairItemId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_FieldInspectionTasks_RepairMode",
                table: "FieldInspectionTasks",
                sql: "([TaskMode]='MEASURE_ONLY' AND [RepairItemId] IS NULL) OR ([LifecycleVersion]=2 AND [TaskMode] IN('NORMAL','CONDITIONAL_FT') AND [RepairItemId] IS NOT NULL)");

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
                name: "IX_RepairNormalSuccessors_SourceDecisionId",
                table: "RepairNormalSuccessors",
                column: "SourceDecisionId");

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
                column: "SafetyObligationId");

            migrationBuilder.AddForeignKey(
                name: "FK_FieldInspectionTasks_RepairItems_RepairItemId",
                table: "FieldInspectionTasks",
                column: "RepairItemId",
                principalTable: "RepairItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RepairDecisions_RepairDecisions_PreviousObligationHeadDecisionId",
                table: "RepairDecisions",
                column: "PreviousObligationHeadDecisionId",
                principalTable: "RepairDecisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RepairItems_FieldInspectionSubmissions_EffectiveIntakeSubmissionId",
                table: "RepairItems",
                column: "EffectiveIntakeSubmissionId",
                principalTable: "FieldInspectionSubmissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RepairItems_RepairAttemptReviews_CurrentReviewId",
                table: "RepairItems",
                column: "CurrentReviewId",
                principalTable: "RepairAttemptReviews",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RepairItems_RepairAttemptSubmissionLinks_CurrentIntakeLinkId",
                table: "RepairItems",
                column: "CurrentIntakeLinkId",
                principalTable: "RepairAttemptSubmissionLinks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RepairItems_RepairAttempts_CurrentAttemptId",
                table: "RepairItems",
                column: "CurrentAttemptId",
                principalTable: "RepairAttempts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RepairItems_RepairExecutionFinishes_CurrentExecutionFinishId",
                table: "RepairItems",
                column: "CurrentExecutionFinishId",
                principalTable: "RepairExecutionFinishes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RepairItems_RepairExecutionStarts_CurrentExecutionStartId",
                table: "RepairItems",
                column: "CurrentExecutionStartId",
                principalTable: "RepairExecutionStarts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RepairItems_RepairFieldTaskBindings_CurrentBindingId",
                table: "RepairItems",
                column: "CurrentBindingId",
                principalTable: "RepairFieldTaskBindings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RepairItems_RepairItems_PredecessorItemId",
                table: "RepairItems",
                column: "PredecessorItemId",
                principalTable: "RepairItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RepairItems_RepairItems_SupersededByItemId",
                table: "RepairItems",
                column: "SupersededByItemId",
                principalTable: "RepairItems",
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
                name: "FK_RepairObligationResolutionEvents_RepairDecisions_PreviousHeadDecisionId",
                table: "RepairObligationResolutionEvents",
                column: "PreviousHeadDecisionId",
                principalTable: "RepairDecisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RepairObligations_FieldTaskStartOrigins_OriginalCrewFirstStartId",
                table: "RepairObligations",
                column: "OriginalCrewFirstStartId",
                principalTable: "FieldTaskStartOrigins",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RepairObligations_RepairItems_CurrentRepairItemId",
                table: "RepairObligations",
                column: "CurrentRepairItemId",
                principalTable: "RepairItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RepairTemporarySafetyMeasures_RepairSafetyResponsibilityTransfers_Id_CurrentResponsibilityTransferId",
                table: "RepairTemporarySafetyMeasures",
                columns: new[] { "Id", "CurrentResponsibilityTransferId" },
                principalTable: "RepairSafetyResponsibilityTransfers",
                principalColumns: new[] { "MeasureId", "Id" },
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
            InstallRepairProducerGuards(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            RefuseAndRemoveRepairProducerGuards(migrationBuilder);
            migrationBuilder.DropForeignKey(
                name: "FK_FieldInspectionTasks_RepairItems_RepairItemId",
                table: "FieldInspectionTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairDecisions_RepairDecisions_PreviousObligationHeadDecisionId",
                table: "RepairDecisions");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairItems_FieldInspectionSubmissions_EffectiveIntakeSubmissionId",
                table: "RepairItems");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairItems_RepairAttemptReviews_CurrentReviewId",
                table: "RepairItems");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairItems_RepairAttemptSubmissionLinks_CurrentIntakeLinkId",
                table: "RepairItems");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairItems_RepairAttempts_CurrentAttemptId",
                table: "RepairItems");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairItems_RepairExecutionFinishes_CurrentExecutionFinishId",
                table: "RepairItems");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairItems_RepairExecutionStarts_CurrentExecutionStartId",
                table: "RepairItems");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairItems_RepairFieldTaskBindings_CurrentBindingId",
                table: "RepairItems");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairItems_RepairItems_PredecessorItemId",
                table: "RepairItems");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairItems_RepairItems_SupersededByItemId",
                table: "RepairItems");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairItems_RepairMeasurementAssessments_CurrentAssessmentId",
                table: "RepairItems");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairObligationResolutionEvents_RepairDecisions_PreviousHeadDecisionId",
                table: "RepairObligationResolutionEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairObligations_FieldTaskStartOrigins_OriginalCrewFirstStartId",
                table: "RepairObligations");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairObligations_RepairItems_CurrentRepairItemId",
                table: "RepairObligations");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairTemporarySafetyMeasures_RepairSafetyResponsibilityTransfers_Id_CurrentResponsibilityTransferId",
                table: "RepairTemporarySafetyMeasures");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairSafetyChecks_RepairSafetyMonitoring_MeasureId",
                table: "RepairSafetyChecks");

            migrationBuilder.DropTable(
                name: "RepairAssessmentEvidence");

            migrationBuilder.DropTable(
                name: "RepairAssessmentMeasurements");

            migrationBuilder.DropTable(
                name: "RepairDangerAcknowledgements");

            migrationBuilder.DropTable(
                name: "RepairEligibilityHandoverSources");

            migrationBuilder.DropTable(
                name: "RepairEligibilityWarrantySources");

            migrationBuilder.DropTable(
                name: "RepairItemLifecycleEvents");

            migrationBuilder.DropTable(
                name: "RepairNormalSuccessors");

            migrationBuilder.DropTable(
                name: "RepairSafetyCheckEvidence");

            migrationBuilder.DropTable(
                name: "RepairDangerWarnings");

            migrationBuilder.DropTable(
                name: "RepairAttemptReviews");

            migrationBuilder.DropTable(
                name: "RepairAttemptSubmissionLinks");

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
                name: "RepairSafetyMonitoring");

            migrationBuilder.DropTable(
                name: "RepairSafetyChecks");

            migrationBuilder.DropIndex(
                name: "IX_RepairTemporarySafetyMeasures_Id_CurrentResponsibilityTransferId",
                table: "RepairTemporarySafetyMeasures");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_RepairSafetyResponsibilityTransfers_MeasureId_Id",
                table: "RepairSafetyResponsibilityTransfers");

            migrationBuilder.DropIndex(
                name: "IX_RepairObligations_CurrentRepairItemId",
                table: "RepairObligations");

            migrationBuilder.DropIndex(
                name: "IX_RepairObligations_OriginalCrewFirstStartId",
                table: "RepairObligations");

            migrationBuilder.DropIndex(
                name: "IX_RepairObligationResolutionEvents_PreviousHeadDecisionId",
                table: "RepairObligationResolutionEvents");

            migrationBuilder.DropIndex(
                name: "IX_RepairItems_CurrentAssessmentId",
                table: "RepairItems");

            migrationBuilder.DropIndex(
                name: "IX_RepairItems_CurrentAttemptId",
                table: "RepairItems");

            migrationBuilder.DropIndex(
                name: "IX_RepairItems_CurrentBindingId",
                table: "RepairItems");

            migrationBuilder.DropIndex(
                name: "IX_RepairItems_CurrentExecutionFinishId",
                table: "RepairItems");

            migrationBuilder.DropIndex(
                name: "IX_RepairItems_CurrentExecutionStartId",
                table: "RepairItems");

            migrationBuilder.DropIndex(
                name: "IX_RepairItems_CurrentIntakeLinkId",
                table: "RepairItems");

            migrationBuilder.DropIndex(
                name: "IX_RepairItems_CurrentReviewId",
                table: "RepairItems");

            migrationBuilder.DropIndex(
                name: "IX_RepairItems_EffectiveIntakeSubmissionId",
                table: "RepairItems");

            migrationBuilder.DropIndex(
                name: "IX_RepairItems_PredecessorItemId",
                table: "RepairItems");

            migrationBuilder.DropIndex(
                name: "IX_RepairItems_SupersededByItemId",
                table: "RepairItems");

            migrationBuilder.DropIndex(
                name: "IX_RepairDecisions_PreviousObligationHeadDecisionId",
                table: "RepairDecisions");

            migrationBuilder.DropIndex(
                name: "IX_FieldInspectionTasks_RepairItemId",
                table: "FieldInspectionTasks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_FieldInspectionTasks_RepairMode",
                table: "FieldInspectionTasks");

            migrationBuilder.DropColumn(
                name: "CurrentResponsibilityTransferId",
                table: "RepairTemporarySafetyMeasures");

            migrationBuilder.DropColumn(
                name: "DefectStatusAtAnchor",
                table: "RepairPackages");

            migrationBuilder.DropColumn(
                name: "CurrentRepairItemId",
                table: "RepairObligations");

            migrationBuilder.DropColumn(
                name: "OriginalCrewFirstStartId",
                table: "RepairObligations");

            migrationBuilder.DropColumn(
                name: "PreviousHeadDecisionId",
                table: "RepairObligationResolutionEvents");

            migrationBuilder.DropColumn(
                name: "CurrentAssessmentId",
                table: "RepairItems");

            migrationBuilder.DropColumn(
                name: "CurrentAttemptId",
                table: "RepairItems");

            migrationBuilder.DropColumn(
                name: "CurrentBindingId",
                table: "RepairItems");

            migrationBuilder.DropColumn(
                name: "CurrentExecutionFinishId",
                table: "RepairItems");

            migrationBuilder.DropColumn(
                name: "CurrentExecutionStartId",
                table: "RepairItems");

            migrationBuilder.DropColumn(
                name: "CurrentIntakeLinkId",
                table: "RepairItems");

            migrationBuilder.DropColumn(
                name: "CurrentReviewId",
                table: "RepairItems");

            migrationBuilder.DropColumn(
                name: "EffectiveIntakeSubmissionId",
                table: "RepairItems");

            migrationBuilder.DropColumn(
                name: "PredecessorItemId",
                table: "RepairItems");

            migrationBuilder.DropColumn(
                name: "SupersededByItemId",
                table: "RepairItems");

            migrationBuilder.DropColumn(
                name: "PreviousObligationHeadDecisionId",
                table: "RepairDecisions");

            migrationBuilder.DropColumn(
                name: "RepairItemId",
                table: "FieldInspectionTasks");

            migrationBuilder.CreateIndex(
                name: "IX_RepairSafetyResponsibilityTransfers_MeasureId",
                table: "RepairSafetyResponsibilityTransfers",
                column: "MeasureId");

        }
    }
}
