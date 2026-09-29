using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class P2ProcessingValidationFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ManifestHash",
                table: "ProcessingJobs",
                type: "char(64)",
                unicode: false,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Mode",
                table: "ProcessingJobs",
                type: "varchar(8)",
                unicode: false,
                maxLength: 8,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId",
                table: "ProcessingJobs",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ProcessingJobs",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: Array.Empty<byte>());

            migrationBuilder.CreateTable(
                name: "ValidationRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModelVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DatasetSplitId = table.Column<string>(type: "varchar(120)", unicode: false, maxLength: 120, nullable: false),
                    MeasurementType = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: false),
                    Unit = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    PairsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    UsedCount = table.Column<int>(type: "int", nullable: false),
                    ExcludedCount = table.Column<int>(type: "int", nullable: false),
                    Bias = table.Column<decimal>(type: "decimal(18,6)", nullable: true),
                    Mae = table.Column<decimal>(type: "decimal(18,6)", nullable: true),
                    Rmse = table.Column<decimal>(type: "decimal(18,6)", nullable: true),
                    ExclusionReasonsJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValidationRuns", x => x.Id);
                    table.CheckConstraint("CK_ValidationRuns_ExclusionReasonsJson", "ISJSON([ExclusionReasonsJson]) = 1 AND LEFT(LTRIM([ExclusionReasonsJson]), 1) = '['");
                    table.CheckConstraint("CK_ValidationRuns_PairsJson", "ISJSON([PairsJson]) = 1 AND LEFT(LTRIM([PairsJson]), 1) = '['");
                    table.CheckConstraint("CK_ValidationRuns_Status", "[Status] IN (1, 2, 3)");
                    table.ForeignKey(
                        name: "FK_ValidationRuns_AIModelVersions_ModelVersionId",
                        column: x => x.ModelVersionId,
                        principalTable: "AIModelVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingJobs_ProjectStatus",
                table: "ProcessingJobs",
                columns: new[] { "ProjectId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ValidationRuns_ModelVersionId",
                table: "ValidationRuns",
                column: "ModelVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ValidationRuns_ProjectStatus",
                table: "ValidationRuns",
                columns: new[] { "ProjectId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ValidationRuns");

            migrationBuilder.DropIndex(
                name: "IX_ProcessingJobs_ProjectStatus",
                table: "ProcessingJobs");

            migrationBuilder.DropColumn(
                name: "ManifestHash",
                table: "ProcessingJobs");

            migrationBuilder.DropColumn(
                name: "Mode",
                table: "ProcessingJobs");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "ProcessingJobs");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ProcessingJobs");
        }
    }
}
