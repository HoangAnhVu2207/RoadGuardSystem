using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class AddP230SupplementarySurveyRequestSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SupplementarySurveyRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SurveyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SurveyRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", nullable: false),
                    RequestedScope = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RoundNo = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    SourcePreservationNote = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplementarySurveyRequests", x => x.Id);
                    table.CheckConstraint("CK_SupplementarySurveyRequests_Approval", "([ApprovedByUserId] IS NULL AND [ApprovedAt] IS NULL) OR ([ApprovedByUserId] IS NOT NULL AND [ApprovedAt] IS NOT NULL)");
                    table.CheckConstraint("CK_SupplementarySurveyRequests_RequestedScope_JsonObject", "ISJSON([RequestedScope]) = 1 AND LEFT(LTRIM([RequestedScope]), 1) = '{'");
                    table.CheckConstraint("CK_SupplementarySurveyRequests_RoundNo", "[RoundNo] > 0");
                    table.CheckConstraint("CK_SupplementarySurveyRequests_Status", "[Status] IN (1, 2, 3, 4, 5, 6, 7)");
                    table.ForeignKey(
                        name: "FK_SupplementarySurveyRequests_SurveyRequests_SurveyRequestId",
                        column: x => x.SurveyRequestId,
                        principalTable: "SurveyRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplementarySurveyRequests_Surveys_SurveyId",
                        column: x => x.SurveyId,
                        principalTable: "Surveys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplementarySurveyRequests_Users_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplementarySurveyRequests_Users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupplementarySurveyRequests_ApprovedByUserId",
                table: "SupplementarySurveyRequests",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplementarySurveyRequests_RequestedByUserId",
                table: "SupplementarySurveyRequests",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplementarySurveyRequests_SurveyRequestId",
                table: "SupplementarySurveyRequests",
                column: "SurveyRequestId");

            migrationBuilder.CreateIndex(
                name: "UX_SupplementarySurveyRequests_SurveyRound",
                table: "SupplementarySurveyRequests",
                columns: new[] { "SurveyId", "RoundNo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupplementarySurveyRequests");
        }
    }
}
