using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class AddSurveyV2ScopesAndConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "SurveyPlans",
                type: "rowversion",
                rowVersion: true,
                nullable: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "SurveyRequests",
                type: "rowversion",
                rowVersion: true,
                nullable: false);

            migrationBuilder.CreateTable(
                name: "SurveyPlanScopes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SurveyPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RouteSectionVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SegmentSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SegmentIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetBand = table.Column<string>(type: "nvarchar(32)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyPlanScopes", x => x.Id);
                    table.CheckConstraint("CK_SurveyPlanScopes_SegmentIdsJson", "ISJSON([SegmentIdsJson]) = 1");
                    table.ForeignKey(
                        name: "FK_SurveyPlanScopes_RoadSectionVersions_RouteSectionVersionId",
                        column: x => x.RouteSectionVersionId,
                        principalTable: "RoadSectionVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SurveyPlanScopes_SurveyPlans_SurveyPlanId",
                        column: x => x.SurveyPlanId,
                        principalTable: "SurveyPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SurveyRequestScopes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SurveyRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RouteSectionVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SegmentSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SegmentIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetBand = table.Column<string>(type: "nvarchar(32)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyRequestScopes", x => x.Id);
                    table.CheckConstraint("CK_SurveyRequestScopes_SegmentIdsJson", "ISJSON([SegmentIdsJson]) = 1");
                    table.ForeignKey(
                        name: "FK_SurveyRequestScopes_RoadSectionVersions_RouteSectionVersionId",
                        column: x => x.RouteSectionVersionId,
                        principalTable: "RoadSectionVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SurveyRequestScopes_SurveyRequests_SurveyRequestId",
                        column: x => x.SurveyRequestId,
                        principalTable: "SurveyRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SurveyPlanScopes_RouteSectionVersionId",
                table: "SurveyPlanScopes",
                column: "RouteSectionVersionId");

            migrationBuilder.CreateIndex(
                name: "UX_SurveyPlanScopes_UniqueBand",
                table: "SurveyPlanScopes",
                columns: new[] { "SurveyPlanId", "RouteSectionVersionId", "SegmentSetId", "TargetBand" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SurveyRequestScopes_RouteSectionVersionId",
                table: "SurveyRequestScopes",
                column: "RouteSectionVersionId");

            migrationBuilder.CreateIndex(
                name: "UX_SurveyRequestScopes_UniqueBand",
                table: "SurveyRequestScopes",
                columns: new[] { "SurveyRequestId", "RouteSectionVersionId", "SegmentSetId", "TargetBand" },
                unique: true);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SurveyPlanScopes");

            migrationBuilder.DropTable(
                name: "SurveyRequestScopes");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "SurveyRequests");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "SurveyPlans");

        }
    }
}
