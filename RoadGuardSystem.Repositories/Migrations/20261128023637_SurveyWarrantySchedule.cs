using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class SurveyWarrantySchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "WarrantyId",
                table: "SurveyPlans",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SurveyPlanOccurrences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SeriesPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SurveyPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PlannedStartAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PlannedEndAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyPlanOccurrences", x => x.Id);
                    table.CheckConstraint("CK_SurveyPlanOccurrences_DateRange", "[PlannedEndAt] > [PlannedStartAt]");
                    table.ForeignKey(
                        name: "FK_SurveyPlanOccurrences_SurveyPlans_SeriesPlanId",
                        column: x => x.SeriesPlanId,
                        principalTable: "SurveyPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SurveyPlanOccurrences_SurveyPlans_SurveyPlanId",
                        column: x => x.SurveyPlanId,
                        principalTable: "SurveyPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SurveyPlans_WarrantyId",
                table: "SurveyPlans",
                column: "WarrantyId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyPlanOccurrences_SeriesPlanId_PlannedStartAt",
                table: "SurveyPlanOccurrences",
                columns: new[] { "SeriesPlanId", "PlannedStartAt" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SurveyPlanOccurrences_SurveyPlanId",
                table: "SurveyPlanOccurrences",
                column: "SurveyPlanId",
                unique: true,
                filter: "[SurveyPlanId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_SurveyPlans_Warranties_WarrantyId",
                table: "SurveyPlans",
                column: "WarrantyId",
                principalTable: "Warranties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SurveyPlans_Warranties_WarrantyId",
                table: "SurveyPlans");

            migrationBuilder.DropTable(
                name: "SurveyPlanOccurrences");

            migrationBuilder.DropIndex(
                name: "IX_SurveyPlans_WarrantyId",
                table: "SurveyPlans");

            migrationBuilder.DropColumn(
                name: "WarrantyId",
                table: "SurveyPlans");
        }
    }
}
