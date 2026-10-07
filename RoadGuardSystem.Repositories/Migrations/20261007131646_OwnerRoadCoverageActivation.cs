using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class OwnerRoadCoverageActivation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RoadCoverageMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScopeObligationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoadSectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationVersion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    From = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    To = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    OffsetFrom = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    OffsetTo = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    ApplicableFromUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ApplicableToUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    HandoverDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HandoverVersion = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    SourceKind = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceVersion = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    HandoverFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CoverageFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceFactsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Provenance = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConfirmedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    SupersedesId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoadCoverageMappings", x => x.Id);
                    table.CheckConstraint("CK_RoadCoverageMappings_Bounds", "[From]>=0 AND [To]>[From] AND [OffsetTo]>[OffsetFrom] AND [ApplicableToUtc]>[ApplicableFromUtc]");
                    table.CheckConstraint("CK_RoadCoverageMappings_Source", "[SourceKind] COLLATE Latin1_General_100_BIN2 IN ('WARRANTY','MAINTENANCE_BASIS') AND [Provenance] COLLATE Latin1_General_100_BIN2 IN ('REAL_SOURCE','TEST_ONLY') AND ISJSON([SourceFactsJson])=1");
                    table.ForeignKey(
                        name: "FK_RoadCoverageMappings_Files_CoverageFileId",
                        column: x => x.CoverageFileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoadCoverageMappings_Files_HandoverFileId",
                        column: x => x.HandoverFileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoadCoverageMappings_HandoverDocuments_HandoverDocumentId",
                        column: x => x.HandoverDocumentId,
                        principalTable: "HandoverDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoadCoverageMappings_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoadCoverageMappings_RepairObligations_ScopeObligationId",
                        column: x => x.ScopeObligationId,
                        principalTable: "RepairObligations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoadCoverageMappings_RoadCoverageMappings_SupersedesId",
                        column: x => x.SupersedesId,
                        principalTable: "RoadCoverageMappings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoadCoverageMappings_RoadSections_RoadSectionId",
                        column: x => x.RoadSectionId,
                        principalTable: "RoadSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoadCoverageMappings_Users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoadCoverageMappings_ActorId",
                table: "RoadCoverageMappings",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_RoadCoverageMappings_CoverageFileId",
                table: "RoadCoverageMappings",
                column: "CoverageFileId");

            migrationBuilder.CreateIndex(
                name: "IX_RoadCoverageMappings_HandoverDocumentId",
                table: "RoadCoverageMappings",
                column: "HandoverDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_RoadCoverageMappings_HandoverFileId",
                table: "RoadCoverageMappings",
                column: "HandoverFileId");

            migrationBuilder.CreateIndex(
                name: "IX_RoadCoverageMappings_ProjectId_RoadSectionId_LocationVersion",
                table: "RoadCoverageMappings",
                columns: new[] { "ProjectId", "RoadSectionId", "LocationVersion" });

            migrationBuilder.CreateIndex(
                name: "IX_RoadCoverageMappings_RoadSectionId",
                table: "RoadCoverageMappings",
                column: "RoadSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_RoadCoverageMappings_ScopeObligationId",
                table: "RoadCoverageMappings",
                column: "ScopeObligationId");

            migrationBuilder.CreateIndex(
                name: "IX_RoadCoverageMappings_SupersedesId",
                table: "RoadCoverageMappings",
                column: "SupersedesId",
                unique: true,
                filter: "[SupersedesId] IS NOT NULL");
            InstallRoadCoverageGuards(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS(SELECT 1 FROM RoadCoverageMappings) THROW 51790, 'Confirmed coverage history cannot be erased by downgrade.', 1;");
            migrationBuilder.DropTable(
                name: "RoadCoverageMappings");
        }
    }
}
