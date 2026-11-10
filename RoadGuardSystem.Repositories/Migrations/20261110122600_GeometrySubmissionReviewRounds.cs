using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class GeometrySubmissionReviewRounds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_RoadGeometryDrafts_Submission",
                table: "RoadGeometryDrafts");

            migrationBuilder.CreateTable(
                name: "GeometrySubmissionRounds",
                columns: table => new
                {
                    DraftId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false),
                    ContentVersion = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    ContentFingerprint = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    InputJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OriginalCoordinatesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubmittedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Outcome = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    DecidedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DecidedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeometrySubmissionRounds", x => new { x.DraftId, x.Number });
                    table.ForeignKey(
                        name: "FK_GeometrySubmissionRounds_RoadGeometryDrafts_DraftId",
                        column: x => x.DraftId,
                        principalTable: "RoadGeometryDrafts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql("""
                INSERT INTO GeometrySubmissionRounds
                    (DraftId, Number, ContentVersion, InputJson, OriginalCoordinatesJson, SubmittedBy, SubmittedAt, Outcome, DecidedBy, DecidedAt)
                SELECT draft.Id, 1, draft.SubmittedContentVersion, draft.InputJson, draft.OriginalCoordinatesJson,
                    draft.SubmittedBy, draft.SubmittedAt, draft.Status, metadata.ApprovedBy, metadata.ApprovedAt
                FROM RoadGeometryDrafts AS draft
                LEFT JOIN RoadGeometryMetadata AS metadata ON draft.Status = 'CONFIRMED' AND metadata.SourceDraftId = draft.Id
                WHERE draft.SubmittedAt IS NOT NULL;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_RoadGeometryDrafts_Submission",
                table: "RoadGeometryDrafts",
                sql: "([SubmittedAt] IS NULL AND [SubmittedBy] IS NULL AND [SubmittedContentVersion] IS NULL AND [Status] NOT IN ('SUBMITTED', 'REJECTED')) OR ([SubmittedAt] IS NOT NULL AND [SubmittedBy] IS NOT NULL AND [SubmittedContentVersion] IS NOT NULL AND [Status] IN ('SUBMITTED', 'CONFIRMED', 'REJECTED'))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM GeometrySubmissionRounds WHERE Number > 1 OR Outcome = 'REJECTED')
                    THROW 51000, 'Cannot downgrade with geometry rejection or resubmission history.', 1;
                """);

            migrationBuilder.DropTable(
                name: "GeometrySubmissionRounds");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RoadGeometryDrafts_Submission",
                table: "RoadGeometryDrafts");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RoadGeometryDrafts_Submission",
                table: "RoadGeometryDrafts",
                sql: "([SubmittedAt] IS NULL AND [SubmittedBy] IS NULL AND [SubmittedContentVersion] IS NULL AND [Status] <> 'SUBMITTED') OR ([SubmittedAt] IS NOT NULL AND [SubmittedBy] IS NOT NULL AND [SubmittedContentVersion] IS NOT NULL AND [Status] IN ('SUBMITTED', 'CONFIRMED'))");
        }
    }
}
