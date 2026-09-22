using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class P232DetectionDefectTaskSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Geometry>(
                name: "Geometry",
                table: "Defects",
                type: "geometry",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId",
                table: "Defects",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReportedAt",
                table: "Defects",
                type: "datetimeoffset(7)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RoadSectionVersionId",
                table: "Defects",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "Severity",
                table: "Defects",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceAIDetectionId",
                table: "Defects",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "Status",
                table: "Defects",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

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
                        name: "FK_AIDetections_ProcessingJobs_ProcessingJobId",
                        column: x => x.ProcessingJobId,
                        principalTable: "ProcessingJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AIDetections_RoadSectionVersions_RoadSectionVersionId",
                        column: x => x.RoadSectionVersionId,
                        principalTable: "RoadSectionVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FieldInspectionTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskCode = table.Column<string>(type: "nvarchar(80)", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SurveyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.CheckConstraint("CK_FieldInspectionTasks_ReviewDecision", "([ReviewDecision] IS NULL AND [ReviewedByUserId] IS NULL AND [ReviewedAt] IS NULL) OR ([ReviewDecision] IS NOT NULL AND [ReviewedByUserId] IS NOT NULL AND [ReviewedAt] IS NOT NULL AND [Status] = 7)");
                    table.CheckConstraint("CK_FieldInspectionTasks_Status", "[Status] IN (1, 2, 3, 4, 5, 6, 7)");
                    table.ForeignKey(
                        name: "FK_FieldInspectionTasks_Defects_DefectId",
                        column: x => x.DefectId,
                        principalTable: "Defects",
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
                        name: "FK_FieldInspectionTasks_Surveys_SurveyId",
                        column: x => x.SurveyId,
                        principalTable: "Surveys",
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
                        name: "FK_DefectVerificationLogs_FieldInspectionTasks_FieldInspectionTaskId",
                        column: x => x.FieldInspectionTaskId,
                        principalTable: "FieldInspectionTasks",
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

            migrationBuilder.AddCheckConstraint(
                name: "CK_Defects_Severity",
                table: "Defects",
                sql: "[Severity] IS NULL OR [Severity] IN (0, 1, 2, 3, 4)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Defects_Status",
                table: "Defects",
                sql: "[Status] IS NULL OR [Status] IN (0, 1, 2, 3, 4)");

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
                name: "IX_FieldInspectionTasks_AssignedByUserId",
                table: "FieldInspectionTasks",
                column: "AssignedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionTasks_DefectId",
                table: "FieldInspectionTasks",
                column: "DefectId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionTasks_ProjectId",
                table: "FieldInspectionTasks",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionTasks_ReviewedByUserId",
                table: "FieldInspectionTasks",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionTasks_RoadSectionVersionId",
                table: "FieldInspectionTasks",
                column: "RoadSectionVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionTasks_SurveyId",
                table: "FieldInspectionTasks",
                column: "SurveyId");

            migrationBuilder.CreateIndex(
                name: "UX_FieldInspectionTasks_TaskCode",
                table: "FieldInspectionTasks",
                column: "TaskCode",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Defects_AIDetections_SourceAIDetectionId",
                table: "Defects",
                column: "SourceAIDetectionId",
                principalTable: "AIDetections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Defects_Projects_ProjectId",
                table: "Defects",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Defects_RoadSectionVersions_RoadSectionVersionId",
                table: "Defects",
                column: "RoadSectionVersionId",
                principalTable: "RoadSectionVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                CREATE TRIGGER [TR_AIDetections_Immutable]
                ON [dbo].[AIDetections]
                AFTER UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    THROW 51033, 'AIDetections are immutable.', 1;
                END
                """);

            migrationBuilder.Sql(
                """
                CREATE TRIGGER [TR_DefectVerificationLogs_AppendOnly]
                ON [dbo].[DefectVerificationLogs]
                AFTER UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    THROW 51034, 'DefectVerificationLogs are append-only.', 1;
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [dbo].[TR_DefectVerificationLogs_AppendOnly];");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [dbo].[TR_AIDetections_Immutable];");

            migrationBuilder.DropForeignKey(
                name: "FK_Defects_AIDetections_SourceAIDetectionId",
                table: "Defects");

            migrationBuilder.DropForeignKey(
                name: "FK_Defects_Projects_ProjectId",
                table: "Defects");

            migrationBuilder.DropForeignKey(
                name: "FK_Defects_RoadSectionVersions_RoadSectionVersionId",
                table: "Defects");

            migrationBuilder.DropTable(
                name: "DefectVerificationLogs");

            migrationBuilder.DropTable(
                name: "AIDetections");

            migrationBuilder.DropTable(
                name: "FieldInspectionTasks");

            migrationBuilder.DropIndex(
                name: "IX_Defects_ProjectId",
                table: "Defects");

            migrationBuilder.DropIndex(
                name: "IX_Defects_RoadSectionVersionId",
                table: "Defects");

            migrationBuilder.DropIndex(
                name: "UX_Defects_SourceAIDetectionId",
                table: "Defects");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Defects_Severity",
                table: "Defects");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Defects_Status",
                table: "Defects");

            migrationBuilder.DropColumn(
                name: "Geometry",
                table: "Defects");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "Defects");

            migrationBuilder.DropColumn(
                name: "ReportedAt",
                table: "Defects");

            migrationBuilder.DropColumn(
                name: "RoadSectionVersionId",
                table: "Defects");

            migrationBuilder.DropColumn(
                name: "Severity",
                table: "Defects");

            migrationBuilder.DropColumn(
                name: "SourceAIDetectionId",
                table: "Defects");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Defects");
        }
    }
}
