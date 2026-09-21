using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class P122SurveyPlanningContracts : Migration
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

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DueAt",
                table: "SurveyRequests",
                type: "datetimeoffset(7)",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<string>(
                name: "OutputRequirements",
                table: "SurveyRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<string>(
                name: "OutputRequirements",
                table: "SurveyPlans",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.CreateIndex(
                name: "UX_SurveyRequests_ActivePlan",
                table: "SurveyRequests",
                column: "SurveyPlanId",
                unique: true,
                filter: "[SurveyPlanId] IS NOT NULL AND [Status] IN (1, 2, 4, 5, 6, 7, 10, 11)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SurveyRequests_OutputRequirements_Json",
                table: "SurveyRequests",
                sql: "ISJSON([OutputRequirements]) = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SurveyRequests_Status",
                table: "SurveyRequests",
                sql: "[Status] IN (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SurveyPlans_OutputRequirements_Json",
                table: "SurveyPlans",
                sql: "ISJSON([OutputRequirements]) = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_SurveyRequests_ActivePlan",
                table: "SurveyRequests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SurveyRequests_OutputRequirements_Json",
                table: "SurveyRequests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SurveyRequests_Status",
                table: "SurveyRequests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SurveyPlans_OutputRequirements_Json",
                table: "SurveyPlans");

            migrationBuilder.DropColumn(
                name: "DueAt",
                table: "SurveyRequests");

            migrationBuilder.DropColumn(
                name: "OutputRequirements",
                table: "SurveyRequests");

            migrationBuilder.DropColumn(
                name: "OutputRequirements",
                table: "SurveyPlans");

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
    }
}
