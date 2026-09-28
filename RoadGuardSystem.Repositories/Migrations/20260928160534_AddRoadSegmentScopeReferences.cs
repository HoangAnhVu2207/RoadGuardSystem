using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class AddRoadSegmentScopeReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RoadSegmentSets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoadSectionVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoadSegmentSets", x => x.Id);
                    table.CheckConstraint("CK_RoadSegmentSets_Status", "[Status] IN ('DRAFT','PUBLISHED','SUPERSEDED')");
                    table.ForeignKey(
                        name: "FK_RoadSegmentSets_RoadSectionVersions_RoadSectionVersionId",
                        column: x => x.RoadSectionVersionId,
                        principalTable: "RoadSectionVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoadSegments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SegmentSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoadSectionVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoadSegments", x => x.Id);
                    table.CheckConstraint("CK_RoadSegments_Sequence_Positive", "[Sequence] > 0");
                    table.ForeignKey(
                        name: "FK_RoadSegments_RoadSectionVersions_RoadSectionVersionId",
                        column: x => x.RoadSectionVersionId,
                        principalTable: "RoadSectionVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoadSegments_RoadSegmentSets_SegmentSetId",
                        column: x => x.SegmentSetId,
                        principalTable: "RoadSegmentSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoadSegments_RoadSectionVersionId",
                table: "RoadSegments",
                column: "RoadSectionVersionId");

            migrationBuilder.CreateIndex(
                name: "UX_RoadSegments_Set_Sequence",
                table: "RoadSegments",
                columns: new[] { "SegmentSetId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoadSegmentSets_RouteVersion_Status",
                table: "RoadSegmentSets",
                columns: new[] { "RoadSectionVersionId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoadSegments");

            migrationBuilder.DropTable(
                name: "RoadSegmentSets");
        }
    }
}
