using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class ReporterRoutingMap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CaseRoutingEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Outcome = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    At = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvidenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CaseSourceVersion = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EvaluationVersion = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    RegistryVersion = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TemporalBasis = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    SupersedesId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TriageSourceJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseRoutingEvents", x => x.Id);
                    table.CheckConstraint("CK_CaseRoutingEvents_Kind", "([Kind] COLLATE Latin1_General_100_BIN2='DECISION' AND [Outcome] IS NOT NULL AND [Outcome] COLLATE Latin1_General_100_BIN2 IN ('NEEDS_LOCATION_EVIDENCE','OUTSIDE_COMPANY_SCOPE')) OR ([Kind] COLLATE Latin1_General_100_BIN2='TRIAGE' AND [Outcome] IS NULL)");
                    table.ForeignKey(
                        name: "FK_CaseRoutingEvents_CaseRoutingEvents_SupersedesId",
                        column: x => x.SupersedesId,
                        principalTable: "CaseRoutingEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CaseRoutingEvents_IncidentCases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "IncidentCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CaseRoutingEvents_Users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CaseRoutingEvents_ActorId",
                table: "CaseRoutingEvents",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseRoutingEvents_CaseId_Sequence",
                table: "CaseRoutingEvents",
                columns: new[] { "CaseId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CaseRoutingEvents_SupersedesId",
                table: "CaseRoutingEvents",
                column: "SupersedesId");
            migrationBuilder.Sql("""
                CREATE OR ALTER TRIGGER [dbo].[TR_CaseRoutingEvents_Immutable]
                ON [dbo].[CaseRoutingEvents] AFTER UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS(SELECT 1 FROM deleted) THROW 51560, 'Routing history is immutable.', 1;
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CaseRoutingEvents");
        }
    }
}
