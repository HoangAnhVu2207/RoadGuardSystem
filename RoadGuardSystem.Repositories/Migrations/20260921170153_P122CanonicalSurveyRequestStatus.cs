using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class P122CanonicalSurveyRequestStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_SurveyRequests_ActivePlan",
                table: "SurveyRequests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SurveyRequests_Status",
                table: "SurveyRequests");

            migrationBuilder.CreateIndex(
                name: "UX_SurveyRequests_ActivePlan",
                table: "SurveyRequests",
                column: "SurveyPlanId",
                unique: true,
                filter: "[SurveyPlanId] IS NOT NULL AND [Status] IN (1, 2, 4, 5, 6, 7, 10)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SurveyRequests_Status",
                table: "SurveyRequests",
                sql: "[Status] IN (1, 2, 3, 4, 5, 6, 7, 8, 9, 10)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_SurveyRequests_ActivePlan",
                table: "SurveyRequests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SurveyRequests_Status",
                table: "SurveyRequests");

            migrationBuilder.CreateIndex(
                name: "UX_SurveyRequests_ActivePlan",
                table: "SurveyRequests",
                column: "SurveyPlanId",
                unique: true,
                filter: "[SurveyPlanId] IS NOT NULL AND [Status] IN (1, 2, 4, 5, 6, 7, 10, 11)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SurveyRequests_Status",
                table: "SurveyRequests",
                sql: "[Status] IN (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11)");
        }
    }
}
