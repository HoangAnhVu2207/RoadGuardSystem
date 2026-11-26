using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class ReportMatchingLocationConfirmation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReportMatchingLocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConfirmedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    RouteVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SegmentSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GeometryVersion = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ReportVersion = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CaseVersion = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    EvidenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SegmentIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(18,10)", precision: 18, scale: 10, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(18,10)", precision: 18, scale: 10, nullable: true),
                    AccuracyMeters = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportMatchingLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportMatchingLocations_IncidentCases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "IncidentCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReportMatchingLocations_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReportMatchingLocations_Reports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "Reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReportMatchingLocations_RoadSectionVersions_RouteVersionId",
                        column: x => x.RouteVersionId,
                        principalTable: "RoadSectionVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReportMatchingLocations_RoadSegmentSets_SegmentSetId",
                        column: x => x.SegmentSetId,
                        principalTable: "RoadSegmentSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReportMatchingLocations_Users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReportMatchingLocations_ActorId",
                table: "ReportMatchingLocations",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportMatchingLocations_CaseId",
                table: "ReportMatchingLocations",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportMatchingLocations_ProjectId",
                table: "ReportMatchingLocations",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportMatchingLocations_ReportId_Sequence",
                table: "ReportMatchingLocations",
                columns: new[] { "ReportId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReportMatchingLocations_RouteVersionId",
                table: "ReportMatchingLocations",
                column: "RouteVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportMatchingLocations_SegmentSetId",
                table: "ReportMatchingLocations",
                column: "SegmentSetId");
            migrationBuilder.Sql("""
                CREATE OR ALTER TRIGGER TR_ReportMatchingLocations_Immutable
                ON ReportMatchingLocations AFTER UPDATE, DELETE AS
                BEGIN SET NOCOUNT ON;
                    IF EXISTS(SELECT 1 FROM deleted)
                        THROW 51141, 'Report matching confirmations are immutable.', 1;
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS(SELECT 1 FROM ReportMatchingLocations) THROW 51142, 'Cannot downgrade populated matching confirmation history.', 1;");
            migrationBuilder.DropTable(
                name: "ReportMatchingLocations");
        }
    }
}
