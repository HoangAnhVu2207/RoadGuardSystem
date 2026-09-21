using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class AddP230DataVersionQualityCheckSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SurveyDataVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SurveyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNo = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    IntegrityStatus = table.Column<byte>(type: "tinyint", nullable: false),
                    ConfirmedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    ConfirmedBy = table.Column<byte>(type: "tinyint", nullable: true),
                    SourceManifest = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyDataVersions", x => x.Id);
                    table.CheckConstraint("CK_SurveyDataVersions_IntegrityStatus", "[IntegrityStatus] IN (1, 2, 3)");
                    table.CheckConstraint("CK_SurveyDataVersions_ServerConfirmation", "([Status] = 3 AND [IntegrityStatus] = 2 AND [ConfirmedAt] IS NOT NULL AND [ConfirmedBy] = 1) OR ([Status] <> 3 AND [ConfirmedAt] IS NULL AND [ConfirmedBy] IS NULL)");
                    table.CheckConstraint("CK_SurveyDataVersions_SourceManifest_JsonArray", "ISJSON([SourceManifest]) = 1 AND LEFT(LTRIM([SourceManifest]), 1) = '['");
                    table.CheckConstraint("CK_SurveyDataVersions_Status", "[Status] IN (1, 2, 3, 4, 5)");
                    table.CheckConstraint("CK_SurveyDataVersions_VersionNo", "[VersionNo] > 0");
                    table.ForeignKey(
                        name: "FK_SurveyDataVersions_Surveys_SurveyId",
                        column: x => x.SurveyId,
                        principalTable: "Surveys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QualityChecks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Scope = table.Column<byte>(type: "tinyint", nullable: false),
                    ExecutionStage = table.Column<byte>(type: "tinyint", nullable: false),
                    SurveyFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SurveyDataVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CheckType = table.Column<byte>(type: "tinyint", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    MeasuredValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Threshold = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CheckedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    CheckedBy = table.Column<byte>(type: "tinyint", nullable: false),
                    InitiatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualityChecks", x => x.Id);
                    table.CheckConstraint("CK_QualityChecks_CheckedBy", "[CheckedBy] IN (1, 2)");
                    table.CheckConstraint("CK_QualityChecks_CheckType", "[CheckType] IN (1, 2, 3, 4, 5, 6, 7, 8, 9)");
                    table.CheckConstraint("CK_QualityChecks_ExactlyOneTarget", "([Scope] = 1 AND [SurveyFileId] IS NOT NULL AND [SurveyDataVersionId] IS NULL) OR ([Scope] = 2 AND [SurveyFileId] IS NULL AND [SurveyDataVersionId] IS NOT NULL)");
                    table.CheckConstraint("CK_QualityChecks_ExecutionStage", "[ExecutionStage] IN (1, 2)");
                    table.CheckConstraint("CK_QualityChecks_MeasuredValue_Json", "[MeasuredValue] IS NULL OR ISJSON([MeasuredValue]) = 1");
                    table.CheckConstraint("CK_QualityChecks_Scope", "[Scope] IN (1, 2)");
                    table.CheckConstraint("CK_QualityChecks_StageActor", "([ExecutionStage] = 1 AND [CheckedBy] = 1) OR ([ExecutionStage] = 2 AND [CheckedBy] = 2 AND [InitiatedByUserId] IS NULL)");
                    table.CheckConstraint("CK_QualityChecks_Status", "[Status] IN (1, 2, 3, 4)");
                    table.CheckConstraint("CK_QualityChecks_Threshold_Json", "[Threshold] IS NULL OR ISJSON([Threshold]) = 1");
                    table.ForeignKey(
                        name: "FK_QualityChecks_SurveyDataVersions_SurveyDataVersionId",
                        column: x => x.SurveyDataVersionId,
                        principalTable: "SurveyDataVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QualityChecks_SurveyFiles_SurveyFileId",
                        column: x => x.SurveyFileId,
                        principalTable: "SurveyFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QualityChecks_Users_InitiatedByUserId",
                        column: x => x.InitiatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QualityChecks_InitiatedByUserId",
                table: "QualityChecks",
                column: "InitiatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityChecks_SurveyDataVersionId",
                table: "QualityChecks",
                column: "SurveyDataVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityChecks_SurveyFileId",
                table: "QualityChecks",
                column: "SurveyFileId");

            migrationBuilder.CreateIndex(
                name: "UX_SurveyDataVersions_SurveyVersion",
                table: "SurveyDataVersions",
                columns: new[] { "SurveyId", "VersionNo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QualityChecks");

            migrationBuilder.DropTable(
                name: "SurveyDataVersions");
        }
    }
}
