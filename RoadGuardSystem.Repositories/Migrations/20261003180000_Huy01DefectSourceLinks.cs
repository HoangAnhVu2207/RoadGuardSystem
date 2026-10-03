using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class Huy01DefectSourceLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DefectSourceLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceKind = table.Column<int>(type: "int", nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportSourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AIDetectionSourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DecisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DefectSourceLinks", x => x.Id);
                    table.CheckConstraint("CK_DefectSourceLinks_TypedSource", "([SourceKind]=1 AND [ReportSourceId]=[SourceId] AND [AIDetectionSourceId] IS NULL) OR ([SourceKind]=2 AND [AIDetectionSourceId]=[SourceId] AND [ReportSourceId] IS NULL)");
                    table.ForeignKey(
                        name: "FK_DefectSourceLinks_AIDetections_AIDetectionSourceId",
                        column: x => x.AIDetectionSourceId,
                        principalTable: "AIDetections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DefectSourceLinks_Defects_DefectId",
                        column: x => x.DefectId,
                        principalTable: "Defects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DefectSourceLinks_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DefectSourceLinks_Reports_ReportSourceId",
                        column: x => x.ReportSourceId,
                        principalTable: "Reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DefectSourceLinks_SourceDecisions_DecisionId_SourceKind_SourceId_ProjectId",
                        columns: x => new { x.DecisionId, x.SourceKind, x.SourceId, x.ProjectId },
                        principalTable: "SourceDecisions",
                        principalColumns: new[] { "Id", "SourceKind", "SourceId", "ProjectId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DefectSourceLinks_AIDetectionSourceId",
                table: "DefectSourceLinks",
                column: "AIDetectionSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_DefectSourceLinks_DecisionId",
                table: "DefectSourceLinks",
                column: "DecisionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DefectSourceLinks_DecisionId_SourceKind_SourceId_ProjectId",
                table: "DefectSourceLinks",
                columns: new[] { "DecisionId", "SourceKind", "SourceId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_DefectSourceLinks_DefectId",
                table: "DefectSourceLinks",
                column: "DefectId");

            migrationBuilder.CreateIndex(
                name: "IX_DefectSourceLinks_ProjectId",
                table: "DefectSourceLinks",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_DefectSourceLinks_ReportSourceId",
                table: "DefectSourceLinks",
                column: "ReportSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_DefectSourceLinks_SourceKind_SourceId",
                table: "DefectSourceLinks",
                columns: new[] { "SourceKind", "SourceId" },
                unique: true,
                filter: "[EndedAt] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM [DefectSourceLinks]) THROW 51000, 'Defect source-link downgrade requires an empty disposable graph.', 1;");
            migrationBuilder.DropTable(
                name: "DefectSourceLinks");
        }
    }
}
