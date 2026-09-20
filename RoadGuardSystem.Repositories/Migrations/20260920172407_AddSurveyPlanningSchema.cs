using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class AddSurveyPlanningSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SurveyPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoadSectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlannedStartAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    PlannedEndAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    SurveyType = table.Column<byte>(type: "tinyint", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyPlans", x => x.Id);
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
                        name: "FK_SurveyPlans_RoadSections_RoadSectionId",
                        column: x => x.RoadSectionId,
                        principalTable: "RoadSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                name: "SurveyRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoadSectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SurveyPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SurveyType = table.Column<byte>(type: "tinyint", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    CancelledAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyRequests", x => x.Id);
                    table.CheckConstraint("CK_SurveyRequests_Cancellation", "([Status] = 9 AND [CancelledAt] IS NOT NULL AND LEN(LTRIM(RTRIM([CancellationReason]))) > 0) OR ([Status] <> 9 AND [CancelledAt] IS NULL AND [CancellationReason] IS NULL)");
                    table.CheckConstraint("CK_SurveyRequests_Status", "[Status] IN (1, 2, 3, 4, 5, 6, 7, 8, 9, 10)");
                    table.CheckConstraint("CK_SurveyRequests_SurveyType", "[SurveyType] IN (1, 2, 3)");
                    table.ForeignKey(
                        name: "FK_SurveyRequests_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SurveyRequests_RoadSections_RoadSectionId",
                        column: x => x.RoadSectionId,
                        principalTable: "RoadSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SurveyRequests_SurveyPlans_SurveyPlanId",
                        column: x => x.SurveyPlanId,
                        principalTable: "SurveyPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SurveyRequests_Users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

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
                name: "UX_SurveyPlans_ActiveScope",
                table: "SurveyPlans",
                columns: new[] { "ProjectId", "RoadSectionId", "SurveyType" },
                unique: true,
                filter: "[Status] IN (1, 2, 3)");

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
                name: "UX_SurveyRequests_ActivePlan",
                table: "SurveyRequests",
                column: "SurveyPlanId",
                unique: true,
                filter: "[SurveyPlanId] IS NOT NULL AND [Status] IN (1, 2, 4, 5, 6, 7, 10)");

            migrationBuilder.Sql(
                """
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

            migrationBuilder.Sql(
                """
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

            migrationBuilder.Sql(
                """
                CREATE TRIGGER [TR_SurveyPlanPostponements_AppendOnly]
                ON [dbo].[SurveyPlanPostponements]
                AFTER UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    THROW 51000, 'SurveyPlanPostponements are append-only.', 1;
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [dbo].[TR_SurveyRequests_ScopeIntegrity];");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [dbo].[TR_SurveyPlans_ScopeIntegrity];");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [dbo].[TR_SurveyPlanPostponements_AppendOnly];");

            migrationBuilder.DropTable(
                name: "SurveyPlanPostponements");

            migrationBuilder.DropTable(
                name: "SurveyRequests");

            migrationBuilder.DropTable(
                name: "SurveyPlans");
        }
    }
}
