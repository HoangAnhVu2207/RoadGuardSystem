using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class AddSurveyAssignmentSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.Sql(
                """
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [dbo].[TR_Surveys_ScopeIntegrity];");

            migrationBuilder.DropTable(
                name: "SurveyAssignments");

            migrationBuilder.DropTable(
                name: "Surveys");
        }
    }
}
