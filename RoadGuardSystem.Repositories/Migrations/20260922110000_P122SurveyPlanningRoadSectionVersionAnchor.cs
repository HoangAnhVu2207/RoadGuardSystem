using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RoadGuardSystem.Repositories;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations;

[DbContext(typeof(RoadGuardDbContext))]
[Migration("20260922110000_P122SurveyPlanningRoadSectionVersionAnchor")]
public partial class P122SurveyPlanningRoadSectionVersionAnchor : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "RoadSectionVersionId",
            table: "SurveyPlans",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "RoadSectionVersionId",
            table: "SurveyRequests",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_SurveyPlans_RoadSectionVersionId",
            table: "SurveyPlans",
            column: "RoadSectionVersionId");

        migrationBuilder.CreateIndex(
            name: "IX_SurveyRequests_RoadSectionVersionId",
            table: "SurveyRequests",
            column: "RoadSectionVersionId");

        migrationBuilder.AddForeignKey(
            name: "FK_SurveyPlans_RoadSectionVersions_RoadSectionVersionId",
            table: "SurveyPlans",
            column: "RoadSectionVersionId",
            principalTable: "RoadSectionVersions",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_SurveyRequests_RoadSectionVersions_RoadSectionVersionId",
            table: "SurveyRequests",
            column: "RoadSectionVersionId",
            principalTable: "RoadSectionVersions",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_SurveyPlans_RoadSectionVersions_RoadSectionVersionId",
            table: "SurveyPlans");
        migrationBuilder.DropForeignKey(
            name: "FK_SurveyRequests_RoadSectionVersions_RoadSectionVersionId",
            table: "SurveyRequests");
        migrationBuilder.DropIndex(
            name: "IX_SurveyPlans_RoadSectionVersionId",
            table: "SurveyPlans");
        migrationBuilder.DropIndex(
            name: "IX_SurveyRequests_RoadSectionVersionId",
            table: "SurveyRequests");
        migrationBuilder.DropColumn(name: "RoadSectionVersionId", table: "SurveyPlans");
        migrationBuilder.DropColumn(name: "RoadSectionVersionId", table: "SurveyRequests");
    }
}
