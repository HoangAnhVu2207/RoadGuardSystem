using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class H4RepairCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RepairPackages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    Basis_Text = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
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
                        name: "FK_RepairObligations_RepairPackages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "RepairPackages",
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
                        name: "FK_RepairItems_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairItems_RepairDecisions_EffectiveDecisionId",
                        column: x => x.EffectiveDecisionId,
                        principalTable: "RepairDecisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairItems_RepairObligations_ObligationId",
                        column: x => x.ObligationId,
                        principalTable: "RepairObligations",
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
                name: "RepairObligationResolutionEvents",
                columns: table => new
                {
                    DecisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Accepted = table.Column<bool>(type: "bit", nullable: false),
                    SupersedesDecisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    At = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
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
                name: "RepairTemporarySafetyMeasures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FormalRepairObligationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResponsibleActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.ForeignKey(
                        name: "FK_RepairSafetyResponsibilityTransfers_RepairTemporarySafetyMeasures_MeasureId",
                        column: x => x.MeasureId,
                        principalTable: "RepairTemporarySafetyMeasures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                name: "IX_RepairAttemptEvidence_FileId",
                table: "RepairAttemptEvidence",
                column: "FileId");

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
                name: "IX_RepairCorrectionEvidence_FileId",
                table: "RepairCorrectionEvidence",
                column: "FileId");

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
                name: "IX_RepairDecisions_SupersedesDecisionId",
                table: "RepairDecisions",
                column: "SupersedesDecisionId",
                unique: true,
                filter: "[SupersedesDecisionId] IS NOT NULL");

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
                name: "IX_RepairItems_CrewId",
                table: "RepairItems",
                column: "CrewId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairItems_DefectId",
                table: "RepairItems",
                column: "DefectId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairItems_EffectiveDecisionId",
                table: "RepairItems",
                column: "EffectiveDecisionId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairItems_ObligationId",
                table: "RepairItems",
                column: "ObligationId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairItems_PackageId",
                table: "RepairItems",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairItems_ProjectId",
                table: "RepairItems",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairObligationResolutionEvents_ObligationId",
                table: "RepairObligationResolutionEvents",
                column: "ObligationId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairObligationResolutionEvents_SupersedesDecisionId",
                table: "RepairObligationResolutionEvents",
                column: "SupersedesDecisionId");

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
                name: "IX_RepairSafetyResponsibilityTransfers_ChangedBy",
                table: "RepairSafetyResponsibilityTransfers",
                column: "ChangedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RepairSafetyResponsibilityTransfers_MeasureId",
                table: "RepairSafetyResponsibilityTransfers",
                column: "MeasureId");

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

            migrationBuilder.AddForeignKey(
                name: "FK_RepairActualScopes_RepairObligations_ObligationId",
                table: "RepairActualScopes",
                column: "ObligationId",
                principalTable: "RepairObligations",
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
                name: "FK_RepairCorrectionEvidence_RepairDecisions_DecisionId",
                table: "RepairCorrectionEvidence",
                column: "DecisionId",
                principalTable: "RepairDecisions",
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
                name: "FK_RepairPolicyDraftChanges_RepairPolicyDrafts_DraftId",
                table: "RepairPolicyDraftChanges",
                column: "DraftId",
                principalTable: "RepairPolicyDrafts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
            InstallRepairCoreGuards(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            RefuseRepairCoreLoss(migrationBuilder);
            migrationBuilder.DropForeignKey(
                name: "FK_RepairDecisions_RepairObligations_ObligationId",
                table: "RepairDecisions");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairItems_RepairObligations_ObligationId",
                table: "RepairItems");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairDecisions_RepairItems_ItemId",
                table: "RepairDecisions");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairPolicyDrafts_RepairPolicyRevisions_PublishedRevisionId",
                table: "RepairPolicyDrafts");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairPolicyDraftChanges_RepairPolicyDrafts_DraftId",
                table: "RepairPolicyDraftChanges");

            migrationBuilder.DropTable(
                name: "RepairActualScopes");

            migrationBuilder.DropTable(
                name: "RepairAttemptEvidence");

            migrationBuilder.DropTable(
                name: "RepairCorrectionEvidence");

            migrationBuilder.DropTable(
                name: "RepairExecutionAuthorizations");

            migrationBuilder.DropTable(
                name: "RepairObligationResolutionEvents");

            migrationBuilder.DropTable(
                name: "RepairPolicyMeasurementRules");

            migrationBuilder.DropTable(
                name: "RepairPolicyRevocations");

            migrationBuilder.DropTable(
                name: "RepairReviewRequests");

            migrationBuilder.DropTable(
                name: "RepairSafetyResponsibilityTransfers");

            migrationBuilder.DropTable(
                name: "RepairWorkHandovers");

            migrationBuilder.DropTable(
                name: "RepairAttempts");

            migrationBuilder.DropTable(
                name: "RepairTemporarySafetyMeasures");

            migrationBuilder.DropTable(
                name: "RepairObligations");

            migrationBuilder.DropTable(
                name: "RepairItems");

            migrationBuilder.DropTable(
                name: "RepairDecisions");

            migrationBuilder.DropTable(
                name: "RepairPackages");

            migrationBuilder.DropTable(
                name: "RepairPolicyRevisions");

            migrationBuilder.DropTable(
                name: "RepairPolicyDrafts");

            migrationBuilder.DropTable(
                name: "RepairPolicyDraftChanges");
        }
    }
}
