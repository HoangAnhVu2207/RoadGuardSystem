using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class OwnerDefectStatisticsActivation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DefectStatisticsSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefectVersion = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    ObligationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScopeHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    RoadSectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RouteVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationVersion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    From = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    To = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    OffsetFrom = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    OffsetTo = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    SharedPartId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SegmentIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QuantitiesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourceFactsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Provenance = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConfirmedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    SupersedesId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DefectStatisticsSources", x => x.Id);
                    table.CheckConstraint("CK_DefectStatisticsSources_Bounds", "[From]>=0 AND [To]>[From] AND [OffsetTo]>[OffsetFrom]");
                    table.CheckConstraint("CK_DefectStatisticsSources_Facts", "ISJSON([SegmentIdsJson])=1 AND ISJSON([QuantitiesJson])=1 AND ISJSON([SourceFactsJson])=1 AND [Provenance] COLLATE Latin1_General_100_BIN2 IN ('REAL_SOURCE','TEST_ONLY')");
                    table.ForeignKey(
                        name: "FK_DefectStatisticsSources_DefectStatisticsSources_SupersedesId",
                        column: x => x.SupersedesId,
                        principalTable: "DefectStatisticsSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DefectStatisticsSources_Defects_DefectId",
                        column: x => x.DefectId,
                        principalTable: "Defects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DefectStatisticsSources_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DefectStatisticsSources_RepairObligations_ObligationId",
                        column: x => x.ObligationId,
                        principalTable: "RepairObligations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DefectStatisticsSources_RoadSectionVersions_RouteVersionId",
                        column: x => x.RouteVersionId,
                        principalTable: "RoadSectionVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DefectStatisticsSources_RoadSections_RoadSectionId",
                        column: x => x.RoadSectionId,
                        principalTable: "RoadSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DefectStatisticsSources_Users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DefectStatisticsSources_ActorId",
                table: "DefectStatisticsSources",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_DefectStatisticsSources_DefectId",
                table: "DefectStatisticsSources",
                column: "DefectId");

            migrationBuilder.CreateIndex(
                name: "IX_DefectStatisticsSources_ObligationId",
                table: "DefectStatisticsSources",
                column: "ObligationId");

            migrationBuilder.CreateIndex(
                name: "IX_DefectStatisticsSources_ProjectId_SharedPartId",
                table: "DefectStatisticsSources",
                columns: new[] { "ProjectId", "SharedPartId" });

            migrationBuilder.CreateIndex(
                name: "IX_DefectStatisticsSources_RoadSectionId",
                table: "DefectStatisticsSources",
                column: "RoadSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_DefectStatisticsSources_RouteVersionId",
                table: "DefectStatisticsSources",
                column: "RouteVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_DefectStatisticsSources_SupersedesId",
                table: "DefectStatisticsSources",
                column: "SupersedesId",
                unique: true,
                filter: "[SupersedesId] IS NOT NULL");
            InstallStatisticsGuards(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS(SELECT 1 FROM DefectStatisticsSources) THROW 51890, 'Cannot erase immutable statistics sources.', 1;");
            migrationBuilder.DropTable(
                name: "DefectStatisticsSources");
        }
    }
}
